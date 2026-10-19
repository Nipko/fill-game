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
    /// <item><b>Hilos</b> (T-D): <see cref="Serialize"/> se llama en el hilo principal y no toca el disco;
    /// <see cref="WriteBytes"/> puede correr en el escritor de segundo plano (<see cref="ProfileWriter"/>). Las operaciones
    /// sobre el disco (<see cref="Load"/>, <see cref="Save"/>, <see cref="WriteBytes"/> y <see cref="Delete"/>) se excluyen con
    /// un cerrojo, y <see cref="WriteBlocked"/> y <see cref="LastSavedBytes"/> se leen desde cualquier hilo.</item>
    /// </list>
    /// Solo datos locales: sin red ni identificadores.
    /// </summary>
    public sealed class LocalProfileStore : IProfileByteStore
    {
        /// <summary>Archivo principal.</summary>
        public const string FileName = "profile.json";

        /// <summary>Copia del estado anterior.</summary>
        public const string BackupFileName = "profile.bak";

        /// <summary>Temporal de la escritura atómica.</summary>
        public const string TempFileName = "profile.json.tmp";

        private const string BlockedMessage = "El perfil en disco es de un esquema más nuevo o su migración falló: no se sobrescribe.";

        private static readonly UTF8Encoding WriteEncoding = new UTF8Encoding(false);
        private static readonly UTF8Encoding ReadEncoding = new UTF8Encoding(false, true);

        private readonly ProfileMigrations _migrations;

        // Una sola operación sobre el disco a la vez (el escritor de segundo plano y el hilo principal).
        private readonly object _io = new object();

        // El último Load cargó la copia porque el principal estaba corrupto: el siguiente guardado borra el principal antes
        // de escribir, para que File.Replace no convierta el archivo corrupto en la nueva copia y pierda la buena. Bajo _io.
        private bool _discardMainOnSave;

        private volatile bool _writeBlocked;
        private volatile int _lastSavedBytes;

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

        /// <summary>Momentos de una escritura en los que un test puede «cortar el proceso» (<see cref="WriteHook"/>).</summary>
        internal enum WriteStage
        {
            /// <summary>El <c>.tmp</c> tiene la mitad de los bytes y aún no se ha volcado.</summary>
            TempHalfWritten,

            /// <summary>El <c>.tmp</c> está completo, volcado (<c>Flush(true)</c>) y cerrado; el principal no se ha tocado.</summary>
            TempWritten,

            /// <summary>Se borró el principal corrupto (tras cargar la copia) y el <c>.tmp</c> aún no ha ocupado su sitio.</summary>
            CorruptMainDeleted,

            /// <summary>Sin <c>File.Replace</c>: el principal ya pasó a la copia y el <c>.tmp</c> aún no es el principal.</summary>
            MainMovedToBackup,
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
        public bool WriteBlocked => _writeBlocked;

        /// <summary>Bytes del último guardado (para vigilar el tope de 100 KB y el coste por vertido).</summary>
        public int LastSavedBytes => _lastSavedBytes;

        /// <summary>
        /// Tests: se llama en cada <see cref="WriteStage"/> de una escritura, en el hilo que escribe y con el cerrojo del disco
        /// tomado. Si lanza, la escritura se queda tal cual en ese punto, como si el proceso muriera ahí.
        /// </summary>
        internal Action<WriteStage>? WriteHook { get; set; }

        /// <summary>Tests: usa el reemplazo en dos <c>File.Move</c> (plataformas sin <c>File.Replace</c>) en vez de <c>File.Replace</c>.</summary>
        internal bool ForceMoveFallback { get; set; }

        /// <inheritdoc />
        public ProfileLoadResult Load()
        {
            lock (_io)
            {
                return LoadLocked();
            }
        }

        /// <inheritdoc />
        public void Save(Profile profile)
        {
            WriteBytes(Serialize(profile));
        }

        /// <inheritdoc />
        public byte[] Serialize(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (_writeBlocked)
            {
                throw new InvalidOperationException(BlockedMessage);
            }

            return WriteEncoding.GetBytes(ProfileJson.Serialize(profile));
        }

        /// <inheritdoc />
        public void WriteBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            lock (_io)
            {
                if (_writeBlocked)
                {
                    throw new InvalidOperationException(BlockedMessage);
                }

                Directory.CreateDirectory(DirectoryPath);
                using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    int half = bytes.Length / 2;
                    stream.Write(bytes, 0, half);
                    Reach(WriteStage.TempHalfWritten);
                    stream.Write(bytes, half, bytes.Length - half);
                    stream.Flush(true);
                }

                Reach(WriteStage.TempWritten);
                if (_discardMainOnSave)
                {
                    DeleteIfExists(MainPath);
                    _discardMainOnSave = false;
                    Reach(WriteStage.CorruptMainDeleted);
                }

                Commit();
                _lastSavedBytes = bytes.Length;
            }
        }

        /// <inheritdoc />
        public void Delete()
        {
            lock (_io)
            {
                DeleteIfExists(TempPath);
                DeleteIfExists(MainPath);
                DeleteIfExists(BackupPath);
                _writeBlocked = false;
                _discardMainOnSave = false;
            }
        }

        private ProfileLoadResult LoadLocked()
        {
            _writeBlocked = false;
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

        private void Commit()
        {
            if (!File.Exists(MainPath))
            {
                File.Move(TempPath, MainPath);
                return;
            }

            if (!ForceMoveFallback)
            {
                try
                {
                    File.Replace(TempPath, MainPath, BackupPath, true);
                    return;
                }
                catch (Exception e) when (e is NotSupportedException || e is NotImplementedException)
                {
                    // Plataforma sin ReplaceFile: abajo.
                }
            }

            // El mismo orden que File.Replace en Unix (principal → copia, temporal → principal). Entre los dos pasos solo
            // falta el principal, y Load usa la copia.
            DeleteIfExists(BackupPath);
            File.Move(MainPath, BackupPath);
            Reach(WriteStage.MainMovedToBackup);
            File.Move(TempPath, MainPath);
        }

        private void Reach(WriteStage stage)
        {
            WriteHook?.Invoke(stage);
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

            if (schema == _migrations.TargetSchema)
            {
                try
                {
                    return new Attempt(AttemptKind.Ok, ProfileJson.FromNode(root), false, null);
                }
                catch (FormatException e)
                {
                    return new Attempt(AttemptKind.Corrupt, null, false, name + ": " + e.Message);
                }
            }

            // 08 §4.4: antes de migrar, profile.bak (si se migra la propia copia, ya lo es). La migración va en memoria y el
            // archivo no se toca hasta el siguiente Save, así que la copia se escribe cuando el documento ya migró y se leyó
            // (DC15): un principal antiguo con un tipo equivocado es corrupto y no pisa la copia buena, que es lo que se carga
            // entonces. Si el paso lanza, la copia también se escribe (el principal no se toca y la escritura queda bloqueada).
            Profile profile;
            try
            {
                _migrations.Migrate(root, schema);
            }
            catch (Exception e)
            {
                string? copyError = TryBackUp(path, isBackup);
                return new Attempt(AttemptKind.MigrationFailed, null, false, name + ": migración desde profile_schema " + Invariant(schema) + " fallida: " + e.Message
                    + (copyError == null ? string.Empty : " (" + copyError + ")"));
            }

            try
            {
                profile = ProfileJson.FromNode(root);
            }
            catch (FormatException e)
            {
                return new Attempt(AttemptKind.Corrupt, null, false, name + ": " + e.Message);
            }

            string? backupError = TryBackUp(path, isBackup);
            if (backupError != null)
            {
                return new Attempt(AttemptKind.MigrationFailed, null, false, name + ": migración desde profile_schema " + Invariant(schema) + " fallida: " + backupError);
            }

            return new Attempt(AttemptKind.Ok, profile, true, null);
        }

        // Copia el archivo a migrar en profile.bak (nada si es la propia copia); el mensaje del error, o null.
        private string? TryBackUp(string path, bool isBackup)
        {
            if (isBackup)
            {
                return null;
            }

            try
            {
                File.Copy(path, BackupPath, true);
                return null;
            }
            catch (Exception e)
            {
                return "no se pudo escribir " + BackupFileName + ": " + e.Message;
            }
        }

        private ProfileLoadResult Blocked(Attempt attempt)
        {
            _writeBlocked = true;
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
