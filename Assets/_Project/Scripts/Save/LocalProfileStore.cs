#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Save.Json;

namespace AF.Save
{
    /// <summary>
    /// Perfil en el dispositivo (GDD 08 §11 "Local"): <c>profile.json</c> en la carpeta que inyecta <c>AF.UI</c>
    /// (<c>Application.persistentDataPath</c>; este ensamblado no usa el motor), UTF-8 sin BOM.
    /// <list type="bullet">
    /// <item><b>Escritura atómica:</b> <c>profile.json.tmp</c> → <c>Flush(true)</c> → <c>File.Replace</c> sobre
    /// <c>profile.json</c>, que deja el estado anterior en <c>profile.bak</c>. Un cierre a mitad deja el principal
    /// anterior intacto o, como mucho, la copia.</item>
    /// <item><b>Carga:</b> si el principal falta o está corrupto se usa <c>profile.bak</c>; un <c>.tmp</c> huérfano se
    /// ignora (el siguiente guardado lo pisa).</item>
    /// <item><b>Migraciones</b> (08 §4.4): un esquema anterior se migra en memoria con <see cref="ProfileMigrations"/>
    /// tras copiar el principal a <c>profile.bak</c>; si falla, o si el archivo es de un esquema más nuevo, no se toca
    /// nada y <see cref="Save"/> queda bloqueado.</item>
    /// </list>
    /// Solo datos locales: sin red ni identificadores. Se usa desde un solo hilo.
    /// </summary>
    public sealed class LocalProfileStore : IProfileStore
    {
        /// <summary>Archivo principal.</summary>
        public const string FileName = "profile.json";

        /// <summary>Copia del estado anterior.</summary>
        public const string BackupFileName = "profile.bak";

        /// <summary>Temporal de la escritura atómica.</summary>
        public const string TempFileName = "profile.json.tmp";

        private static readonly UTF8Encoding WriteEncoding = new UTF8Encoding(false);
        private static readonly UTF8Encoding ReadEncoding = new UTF8Encoding(false, true);

        private readonly ProfileMigrations _migrations;

        // El último Load cargó la copia porque el principal estaba corrupto: el siguiente Save borra el principal antes
        // de escribir, para que File.Replace no convierta el archivo corrupto en la nueva copia y pierda la buena.
        private bool _discardMainOnSave;

        /// <summary>Crea el almacén en <paramref name="directory"/> con las migraciones de esta build.</summary>
        /// <param name="directory">Carpeta absoluta del perfil (se crea al guardar si no existe).</param>
        public LocalProfileStore(string directory)
            : this(directory, ProfileMigrations.Current)
        {
        }

        /// <summary>Crea el almacén con un juego de migraciones concreto (tests).</summary>
        /// <exception cref="ArgumentException">La carpeta no es absoluta o las migraciones no llegan al esquema actual.</exception>
        public LocalProfileStore(string directory, ProfileMigrations migrations)
        {
            if (string.IsNullOrEmpty(directory) || !Path.IsPathRooted(directory))
            {
                throw new ArgumentException("La carpeta del perfil debe ser una ruta absoluta.", nameof(directory));
            }

            _migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
            if (migrations.TargetSchema != Profile.CurrentSchema)
            {
                throw new ArgumentException("Las migraciones deben llegar a Profile.CurrentSchema.", nameof(migrations));
            }

            DirectoryPath = directory;
            MainPath = Path.Combine(directory, FileName);
            BackupPath = Path.Combine(directory, BackupFileName);
            TempPath = Path.Combine(directory, TempFileName);
        }

        /// <summary>Carpeta del perfil.</summary>
        public string DirectoryPath { get; }

        /// <summary>Ruta de <c>profile.json</c>.</summary>
        public string MainPath { get; }

        /// <summary>Ruta de <c>profile.bak</c>.</summary>
        public string BackupPath { get; }

        /// <summary>Ruta de <c>profile.json.tmp</c>.</summary>
        public string TempPath { get; }

        /// <summary>
        /// <see cref="Save"/> está bloqueado porque el último <see cref="Load"/> encontró un esquema más nuevo o una
        /// migración fallida: así no se pisa el archivo que se quiere conservar. <see cref="Delete"/> lo levanta.
        /// </summary>
        public bool WriteBlocked { get; private set; }

        /// <summary>Bytes del último guardado (para vigilar el tope de 100 KB y el coste por vertido).</summary>
        public int LastSavedBytes { get; private set; }

        /// <inheritdoc />
        public ProfileLoadResult Load()
        {
            WriteBlocked = false;
            _discardMainOnSave = false;

            Attempt main = TryLoad(MainPath, isBackup: false);
            if (main.Kind == AttemptKind.Ok)
            {
                return new ProfileLoadResult(ProfileLoadStatus.Loaded, main.Profile, main.Migrated);
            }

            if (main.Kind == AttemptKind.TooNew || main.Kind == AttemptKind.MigrationFailed)
            {
                return Blocked(main);
            }

            Attempt backup = TryLoad(BackupPath, isBackup: true);
            if (backup.Kind == AttemptKind.Ok)
            {
                _discardMainOnSave = main.Kind == AttemptKind.Corrupt;
                string reason = main.Kind == AttemptKind.Corrupt ? main.Error! : FileName + " no existe";
                return new ProfileLoadResult(ProfileLoadStatus.LoadedFromBackup, backup.Profile, backup.Migrated, reason);
            }

            if (backup.Kind == AttemptKind.TooNew || backup.Kind == AttemptKind.MigrationFailed)
            {
                return Blocked(backup);
            }

            if (main.Kind == AttemptKind.Missing && backup.Kind == AttemptKind.Missing)
            {
                return new ProfileLoadResult(ProfileLoadStatus.NotFound, null);
            }

            string error = (main.Error ?? FileName + " no existe") + " | " + (backup.Error ?? BackupFileName + " no existe");
            return new ProfileLoadResult(ProfileLoadStatus.Unreadable, null, false, error);
        }

        /// <inheritdoc />
        public void Save(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (WriteBlocked)
            {
                throw new InvalidOperationException("El perfil en disco es de un esquema más nuevo o su migración falló: no se sobrescribe.");
            }

            byte[] bytes = WriteEncoding.GetBytes(ProfileJson.Serialize(profile));
            Directory.CreateDirectory(DirectoryPath);
            using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (_discardMainOnSave)
            {
                DeleteIfExists(MainPath);
                _discardMainOnSave = false;
            }

            Commit();
            LastSavedBytes = bytes.Length;
        }

        /// <inheritdoc />
        public void Delete()
        {
            DeleteIfExists(TempPath);
            DeleteIfExists(MainPath);
            DeleteIfExists(BackupPath);
            WriteBlocked = false;
            _discardMainOnSave = false;
        }

        private void Commit()
        {
            if (!File.Exists(MainPath))
            {
                File.Move(TempPath, MainPath);
                return;
            }

            try
            {
                File.Replace(TempPath, MainPath, BackupPath, true);
            }
            catch (Exception e) when (e is NotSupportedException || e is NotImplementedException)
            {
                // Plataforma sin ReplaceFile: el mismo orden que File.Replace en Unix (principal → copia,
                // temporal → principal). Entre los dos pasos solo falta el principal, y Load usa la copia.
                DeleteIfExists(BackupPath);
                File.Move(MainPath, BackupPath);
                File.Move(TempPath, MainPath);
            }
        }

        private Attempt TryLoad(string path, bool isBackup)
        {
            if (!File.Exists(path))
            {
                return new Attempt(AttemptKind.Missing, null, false, null);
            }

            string name = Path.GetFileName(path);
            JsonNode root;
            int schema;
            try
            {
                string text = ReadEncoding.GetString(File.ReadAllBytes(path));
                root = JsonText.Parse(text);
                schema = ProfileJson.ReadSchema(root);
            }
            catch (Exception e) when (e is FormatException || e is IOException || e is UnauthorizedAccessException || e is DecoderFallbackException)
            {
                return new Attempt(AttemptKind.Corrupt, null, false, name + ": " + e.Message);
            }

            if (schema > _migrations.TargetSchema)
            {
                return new Attempt(AttemptKind.TooNew, null, false, name + ": profile_schema " + Invariant(schema) + " es más nuevo que esta build (" + Invariant(_migrations.TargetSchema) + ")");
            }

            bool migrated = false;
            if (schema < _migrations.TargetSchema)
            {
                try
                {
                    // 08 §4.4: antes de migrar se escribe profile.bak (si se migra la propia copia, ya lo es).
                    if (!isBackup)
                    {
                        File.Copy(path, BackupPath, true);
                    }

                    _migrations.Migrate(root, schema);
                }
                catch (Exception e)
                {
                    return new Attempt(AttemptKind.MigrationFailed, null, false, name + ": migración desde profile_schema " + Invariant(schema) + " fallida: " + e.Message);
                }

                migrated = true;
            }

            try
            {
                return new Attempt(AttemptKind.Ok, ProfileJson.FromNode(root), migrated, null);
            }
            catch (FormatException e)
            {
                return new Attempt(AttemptKind.Corrupt, null, false, name + ": " + e.Message);
            }
        }

        private ProfileLoadResult Blocked(Attempt attempt)
        {
            WriteBlocked = true;
            ProfileLoadStatus status = attempt.Kind == AttemptKind.TooNew ? ProfileLoadStatus.TooNew : ProfileLoadStatus.MigrationFailed;
            return new ProfileLoadResult(status, null, false, attempt.Error);
        }

        private static string Invariant(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private enum AttemptKind
        {
            Missing,
            Corrupt,
            TooNew,
            MigrationFailed,
            Ok,
        }

        private readonly struct Attempt
        {
            public Attempt(AttemptKind kind, Profile? profile, bool migrated, string? error)
            {
                Kind = kind;
                Profile = profile;
                Migrated = migrated;
                Error = error;
            }

            public AttemptKind Kind { get; }

            public Profile? Profile { get; }

            public bool Migrated { get; }

            public string? Error { get; }
        }
    }
}
