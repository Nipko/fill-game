#nullable enable
using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

namespace AF.Save.PlayGames
{
    /// <summary>
    /// <see cref="ISavedGamesPort"/> real: Play Games plugin for Unity 2.3.0 (PGS v2; D-042; 08 §7 #5 y §11;
    /// <c>docs/dev/play-games.md</c>). Solo en el teléfono (<c>UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c>) y solo si el build lleva el
    /// <c>APP_ID</c> de Play Console (<see cref="IsConfigured"/>; el dueño lo pega con <c>Window › Google Play Games › Setup</c>
    /// cuando exista la app, D-034). En el editor y sin configurar, <see cref="TryCreate"/> devuelve null y el juego sigue sin nube.
    /// <list type="bullet">
    /// <item>Sesión: <c>PlayGamesPlatform.Instance.Authenticate</c>, la silenciosa de PGS v2 (<c>isAuthenticated</c>); nunca
    /// <c>ManuallyAuthenticate</c> (sin botón: 08 §2.3).</item>
    /// <item>Snapshot: <c>OpenWithManualConflictResolution</c> con <c>ReadCacheOrNetwork</c> y los datos de las dos versiones en un
    /// conflicto; sin portada ni descripción (08 §11).</item>
    /// <item>Nada de Nearby, logros, marcadores ni eventos.</item>
    /// </list>
    /// </summary>
    public sealed class PlayGamesSavedGamesPort : ISavedGamesPort
    {
        private PlayGamesSavedGamesPort()
        {
        }

        /// <summary>El build lleva el <c>APP_ID</c> de Play Games (los recursos de Play Console). Fuera del teléfono, false.</summary>
        public static bool IsConfigured
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                PlayGamesSettings settings = PlayGamesSettings.LoadInstance();
                return settings != null && !string.IsNullOrEmpty(settings.AppId);
#else
                return false;
#endif
            }
        }

        /// <summary>El puerto real, o null fuera del teléfono o sin configurar (sin <c>APP_ID</c>: no se toca el SDK).</summary>
        public static ISavedGamesPort? TryCreate()
        {
            return IsConfigured ? new PlayGamesSavedGamesPort() : null;
        }

        /// <inheritdoc />
        public bool Configured => IsConfigured;

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <inheritdoc />
        public void SignInSilently(Action<bool> done)
        {
            PlayGamesPlatform.Instance.Authenticate(status => done(status == SignInStatus.Success));
        }

        /// <inheritdoc />
        public void Open(string name, SnapshotConflictResolver resolve, Action<SavedGamesStatus, ISavedGamesSnapshot?> done)
        {
            PlayGamesPlatform.Instance.SavedGame.OpenWithManualConflictResolution(
                name,
                DataSource.ReadCacheOrNetwork,
                true,
                (resolver, original, originalData, unmerged, unmergedData) =>
                {
                    byte[] chosen = resolve(originalData ?? Array.Empty<byte>(), unmergedData ?? Array.Empty<byte>());
                    resolver.ResolveConflict(original, new SavedGameMetadataUpdate.Builder().Build(), chosen);
                },
                (status, metadata) => done(Map(status), status == SavedGameRequestStatus.Success && metadata != null ? new Snapshot(metadata) : null));
        }

        /// <inheritdoc />
        public void Read(ISavedGamesSnapshot snapshot, Action<SavedGamesStatus, byte[]?> done)
        {
            PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(((Snapshot)snapshot).Metadata, (status, data) => done(Map(status), data));
        }

        /// <inheritdoc />
        public void Commit(ISavedGamesSnapshot snapshot, byte[] data, Action<SavedGamesStatus> done)
        {
            PlayGamesPlatform.Instance.SavedGame.CommitUpdate(
                ((Snapshot)snapshot).Metadata, new SavedGameMetadataUpdate.Builder().Build(), data, (status, _) => done(Map(status)));
        }

        /// <inheritdoc />
        public void Delete(ISavedGamesSnapshot snapshot)
        {
            PlayGamesPlatform.Instance.SavedGame.Delete(((Snapshot)snapshot).Metadata);
        }

        private static SavedGamesStatus Map(SavedGameRequestStatus status)
        {
            switch (status)
            {
                case SavedGameRequestStatus.Success: return SavedGamesStatus.Ok;
                case SavedGameRequestStatus.TimeoutError: return SavedGamesStatus.Timeout;
                case SavedGameRequestStatus.AuthenticationError: return SavedGamesStatus.AuthError;
                case SavedGameRequestStatus.BadInputError: return SavedGamesStatus.BadInput;
                default: return SavedGamesStatus.Failed;
            }
        }

        private sealed class Snapshot : ISavedGamesSnapshot
        {
            public Snapshot(ISavedGameMetadata metadata)
            {
                Metadata = metadata;
            }

            public ISavedGameMetadata Metadata { get; }
        }
#else
        /// <inheritdoc />
        public void SignInSilently(Action<bool> done) => done(false);

        /// <inheritdoc />
        public void Open(string name, SnapshotConflictResolver resolve, Action<SavedGamesStatus, ISavedGamesSnapshot?> done) => done(SavedGamesStatus.AuthError, null);

        /// <inheritdoc />
        public void Read(ISavedGamesSnapshot snapshot, Action<SavedGamesStatus, byte[]?> done) => done(SavedGamesStatus.AuthError, null);

        /// <inheritdoc />
        public void Commit(ISavedGamesSnapshot snapshot, byte[] data, Action<SavedGamesStatus> done) => done(SavedGamesStatus.AuthError);

        /// <inheritdoc />
        public void Delete(ISavedGamesSnapshot snapshot)
        {
        }
#endif
    }
}
