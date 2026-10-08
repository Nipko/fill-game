#nullable enable
using System;

namespace AF.Save
{
    /// <summary>
    /// Almacén del perfil (GDD 08 §2.2 <c>AF.Save</c>; §11): <see cref="LocalProfileStore"/> en el dispositivo y
    /// <see cref="MemoryProfileStore"/> en los tests. La nube (Play Games Saved Games, Firebase) llegará detrás de esta
    /// misma interfaz cuando el dueño apruebe sus SDK. Se usa desde un solo hilo (el principal).
    /// </summary>
    public interface IProfileStore
    {
        /// <summary>
        /// Carga el perfil (08 §2.3: <c>profile = Save.LoadLocal() ?? Profile.New()</c>). Si no hay ninguno
        /// (<see cref="ProfileLoadStatus.NotFound"/>) o no se puede leer (<see cref="ProfileLoadStatus.Unreadable"/>),
        /// quien llama crea uno con <see cref="Profile.CreateNew"/>.
        /// </summary>
        ProfileLoadResult Load();

        /// <summary>Guarda el perfil y lo deja persistido (sobrevive a un cierre o un <c>force-stop</c>).</summary>
        /// <exception cref="InvalidOperationException">
        /// El último <see cref="Load"/> devolvió <see cref="ProfileLoadStatus.TooNew"/> o
        /// <see cref="ProfileLoadStatus.MigrationFailed"/>: se protege el archivo existente.
        /// </exception>
        /// <exception cref="System.IO.IOException">No se pudo escribir.</exception>
        void Save(Profile profile);

        /// <summary>
        /// Borra el perfil y su copia (eliminación de cuenta, 08 §11: <c>profile.json</c> y <c>profile.bak</c>) y
        /// levanta la protección de escritura.
        /// </summary>
        void Delete();
    }

    /// <summary>Operaciones comunes sobre un <see cref="IProfileStore"/>.</summary>
    public static class ProfileStoreExtensions
    {
        /// <summary>
        /// Guarda <paramref name="profile"/> y devuelve si quedó guardado. Si la escritura falla (<see cref="System.IO.IOException"/>,
        /// <see cref="UnauthorizedAccessException"/>) o el almacén protege un perfil más nuevo o una migración fallida
        /// (<see cref="InvalidOperationException"/>), devuelve false y el cambio sigue en memoria para reintentarlo, como hace
        /// <c>AF.Economy.Wallet</c>.
        /// </summary>
        public static bool TrySave(this IProfileStore store, Profile profile)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            try
            {
                store.Save(profile);
                return true;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>Resultado de <see cref="IProfileStore.Load"/>.</summary>
    public enum ProfileLoadStatus
    {
        /// <summary>No hay perfil: primera ejecución.</summary>
        NotFound = 0,

        /// <summary>Se cargó el archivo principal.</summary>
        Loaded = 1,

        /// <summary>El principal falta o está corrupto y se cargó <c>profile.bak</c>.</summary>
        LoadedFromBackup = 2,

        /// <summary>Hay archivos, pero ninguno se puede leer (corruptos). No hay perfil.</summary>
        Unreadable = 3,

        /// <summary>
        /// El archivo es de un esquema más nuevo que esta build (08 §4.4: el esquema solo avanza). No hay perfil y el
        /// almacén no deja guardar, para no pisarlo con uno más viejo.
        /// </summary>
        TooNew = 4,

        /// <summary>
        /// Una migración falló (08 §4.4: se conserva el perfil anterior y se registra <c>af_error{module=save}</c>). No hay
        /// perfil y el almacén no deja guardar.
        /// </summary>
        MigrationFailed = 5,
    }

    /// <summary>Lo que devuelve <see cref="IProfileStore.Load"/>.</summary>
    public sealed class ProfileLoadResult
    {
        /// <summary>Crea un resultado.</summary>
        public ProfileLoadResult(ProfileLoadStatus status, Profile? profile, bool migrated = false, string? error = null)
        {
            bool hasProfile = status == ProfileLoadStatus.Loaded || status == ProfileLoadStatus.LoadedFromBackup;
            if (hasProfile != (profile != null))
            {
                throw new ArgumentException("Solo Loaded y LoadedFromBackup llevan perfil.", nameof(profile));
            }

            Status = status;
            Profile = profile;
            Migrated = migrated;
            Error = error;
        }

        /// <summary>Qué pasó.</summary>
        public ProfileLoadStatus Status { get; }

        /// <summary>El perfil (con <see cref="ProfileLoadStatus.Loaded"/> y <see cref="ProfileLoadStatus.LoadedFromBackup"/>).</summary>
        public Profile? Profile { get; }

        /// <summary>El archivo era de un esquema anterior y se migró en memoria: conviene guardarlo.</summary>
        public bool Migrated { get; }

        /// <summary>Descripción del problema encontrado (archivo corrupto, migración…), para el registro; null si no hubo.</summary>
        public string? Error { get; }

        /// <summary>Hay perfil.</summary>
        public bool HasProfile => Profile != null;
    }

    /// <summary>
    /// Almacén en memoria para tests y para el Editor: guarda el JSON (no la referencia), así que cada
    /// <see cref="Load"/> devuelve una copia independiente, como el disco.
    /// </summary>
    public sealed class MemoryProfileStore : IProfileStore
    {
        /// <summary>Crea el almacén vacío o con <paramref name="initial"/> ya guardado.</summary>
        public MemoryProfileStore(Profile? initial = null)
        {
            Json = initial == null ? null : ProfileJson.Serialize(initial);
        }

        /// <summary>Último JSON guardado, o null.</summary>
        public string? Json { get; private set; }

        /// <summary>Veces que se ha guardado.</summary>
        public int SaveCount { get; private set; }

        /// <inheritdoc />
        public ProfileLoadResult Load()
        {
            return Json == null
                ? new ProfileLoadResult(ProfileLoadStatus.NotFound, null)
                : new ProfileLoadResult(ProfileLoadStatus.Loaded, ProfileJson.Deserialize(Json));
        }

        /// <inheritdoc />
        public void Save(Profile profile)
        {
            Json = ProfileJson.Serialize(profile);
            SaveCount++;
        }

        /// <inheritdoc />
        public void Delete()
        {
            Json = null;
        }
    }
}
