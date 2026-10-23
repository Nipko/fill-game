#nullable enable
using System;

namespace AF.Save
{
    /// <summary>
    /// La costura con el SDK de Play Games (D-042; 08 §7 #5 y §11; <c>docs/dev/play-games.md</c>): lo mínimo de Saved Games que
    /// necesita <see cref="PlayGamesCloudSave"/>, sin tipos del plugin, para que la lógica (sesión, tope del blob, conflicto,
    /// reintento de #3379 y borrado) se pruebe en el editor con un puerto falso. La implementación real
    /// (<c>AF.Save.PlayGames.PlayGamesSavedGamesPort</c>) solo se crea en el teléfono. Todas las llamadas terminan en el hilo
    /// principal.
    /// </summary>
    public interface ISavedGamesPort
    {
        /// <summary>
        /// Play Games está configurado en este build (hay <c>APP_ID</c>: el dueño pegó los recursos de Play Console). Sin él no se
        /// llama al SDK y el juego sigue sin nube (D-034).
        /// </summary>
        bool Configured { get; }

        /// <summary>Inicio de sesión silencioso (sin interfaz: <c>isAuthenticated</c> de PGS v2); <paramref name="done"/> dice si hay sesión.</summary>
        void SignInSilently(Action<bool> done);

        /// <summary>
        /// Abre (o crea vacío) el snapshot <paramref name="name"/> con resolución manual de conflictos: si Play guarda dos versiones,
        /// <paramref name="resolve"/> recibe los dos blobs y devuelve el que queda (08 §11: nunca <c>UseLongestPlaytime</c>).
        /// </summary>
        void Open(string name, SnapshotConflictResolver resolve, Action<SavedGamesStatus, ISavedGamesSnapshot?> done);

        /// <summary>Lee los bytes del snapshot abierto (vacío si se acaba de crear).</summary>
        void Read(ISavedGamesSnapshot snapshot, Action<SavedGamesStatus, byte[]?> done);

        /// <summary>Sustituye los bytes del snapshot abierto y lo cierra.</summary>
        void Commit(ISavedGamesSnapshot snapshot, byte[] data, Action<SavedGamesStatus> done);

        /// <summary>Borra el snapshot abierto. El plugin no avisa del resultado: se comprueba en el arranque siguiente (<see cref="CloudErase"/>).</summary>
        void Delete(ISavedGamesSnapshot snapshot);
    }

    /// <summary>Un snapshot abierto (opaco: lo crea y lo entiende solo el puerto).</summary>
    public interface ISavedGamesSnapshot
    {
    }

    /// <summary>Elige el blob que queda en un conflicto de Saved Games: devuelve los bytes que se escriben.</summary>
    public delegate byte[] SnapshotConflictResolver(byte[] original, byte[] unmerged);

    /// <summary>Resultado de una llamada del puerto (el <c>SavedGameRequestStatus</c> del plugin, sin sus tipos).</summary>
    public enum SavedGamesStatus
    {
        /// <summary>Hecho.</summary>
        Ok = 0,

        /// <summary>Se agotó el tiempo (sin red, o Play no respondió).</summary>
        Timeout = 1,

        /// <summary>Sin sesión (la perdió o nunca la tuvo).</summary>
        AuthError = 2,

        /// <summary>Error interno de Play o del plugin.</summary>
        Failed = 3,

        /// <summary>Argumentos que Play no acepta (nombre del snapshot, tamaño).</summary>
        BadInput = 4,
    }
}
