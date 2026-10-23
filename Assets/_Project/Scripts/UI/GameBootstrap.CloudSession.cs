#nullable enable
using System.Collections;
using AF.Events;
using AF.GameFlow;
using AF.IAP;
using AF.Platform;
using AF.Save;
#if UNITY_ANDROID && !UNITY_EDITOR
using AF.Save.PlayGames;
#endif
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La nube de la sesión con Play Games (D-042; 08 §2.3 y §11; <c>docs/dev/play-games.md</c> §4). Parcial nuevo:
    /// <list type="bullet">
    /// <item><b>Qué nube:</b> la de los tests (<see cref="CloudSaveOverride"/>); en el teléfono con el <c>APP_ID</c> de Play Console,
    /// <see cref="PlayGamesCloudSave"/> sobre el plugin 2.3.0; si no (editor, o el build sin configurar mientras espera el dictamen
    /// de marca, D-034), la falsa sin sesión, que no hace nada.</item>
    /// <item><b>Cuándo:</b> nunca con la banda &lt;13 (D-033, añadido del 2026-10-23; 08 §7); con la banda 13-17 o 18+ ya
    /// guardada, en el arranque (<see cref="SyncProfileAtBoot"/>), esperando hasta <see cref="CloudBootWaitMs"/> con la pantalla
    /// de arranque a la vista (<see cref="WaitCloudAtBoot"/>); en el primer arranque, tras declarar la edad
    /// (<see cref="StartCloudAfterConsent"/>).</item>
    /// <item><b>Si la nube llega tarde</b> y gana (o aporta campos), el perfil fusionado se guarda y la escena se recarga al
    /// inicio con él, como al cambiar de idioma (<see cref="TickCloud"/>), en cuanto no hay una compra ni un borrado en vuelo.</item>
    /// <item><b>Subidas</b> (08 §11): cada <c>save_cloud_interval_levels</c> victorias, tras cada compra
    /// (<see cref="CloudUploadStoreService"/>) y al pasar a segundo plano; al volver, se reintenta la fusión que no llegó o la
    /// subida pendiente de #3379 (<see cref="RetryCloud"/>). Solo tras fusionar en esta sesión.</item>
    /// <item><b>Borrado:</b> <c>GameBootstrap.Account.cs</c> llama a <see cref="CloudSession.EraseForAccountDelete"/>; la marca
    /// de borrado pendiente (<see cref="ICloudSaveFlags"/>) sobrevive al borrado de <c>PlayerPrefs</c> y se comprueba en el
    /// arranque siguiente (<see cref="CloudErase"/>).</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Lo que el arranque espera a la nube con la pantalla de arranque a la vista (ms) [P]; después sigue y la fusión llega tarde.</summary>
        public const float CloudBootWaitMs = 2500f;

        private static CloudSession? s_rebuildCloud;
        private static ICloudSaveFlags? s_editorCloudFlags;
        private CloudSession? _cloudSession;
        private bool _cloudBootWindow;
        private Profile? _cloudBootProfile;
        private IProfileStore? _cloudBootStore;
        private Profile? _cloudLateRemote;
        private bool _cloudLateRemoteCoherent;

        /// <summary>Marcas de la nube para los tests (por defecto: en memoria en el editor, <c>PlayerPrefs</c> en el teléfono).</summary>
        internal static ICloudSaveFlags? CloudFlagsOverride { get; set; }

        /// <summary>Sustituye <see cref="CloudBootWaitMs"/> en los tests.</summary>
        internal static float? CloudBootWaitMsOverride { get; set; }

        /// <summary>La nube de la sesión (null antes del arranque).</summary>
        public CloudSession? CloudSessionNow => _cloudSession;

        /// <summary>
        /// Llegó tarde un perfil de la nube que cambia el de la sesión: espera a <see cref="TickCloud"/>, que lo vuelve a fusionar con
        /// el perfil vivo. Mientras, no se sube nada (sería subir el local sin fusionar).
        /// </summary>
        public bool CloudReplacePending => _cloudLateRemote != null;

        // La firma del perfil de la sesión para fusionar: la que tenía al leerse del disco (lo de después lo escribe el propio juego).
        private IntegrityStatus SessionIntegrity => BootIntegrity ?? IntegrityStatus.Unsigned;

        private static ICloudSaveFlags CloudFlags
        {
            get
            {
                if (CloudFlagsOverride != null)
                {
                    return CloudFlagsOverride;
                }
#if UNITY_EDITOR
                return s_editorCloudFlags ??= new MemoryCloudSaveFlags();
#else
                return PlayerPrefsCloudFlags.Instance;
#endif
            }
        }

        /// <summary>Limpia los ganchos de test de la nube de la sesión.</summary>
        internal static void ResetCloudSessionOverrides()
        {
            CloudFlagsOverride = null;
            CloudBootWaitMsOverride = null;
            s_editorCloudFlags = null;
            s_rebuildCloud = null;
        }

        // La de la sesión: la que viene de una recarga (RebuildUi) o una nueva.
        private CloudSession EnsureCloudSession()
        {
            if (_cloudSession == null)
            {
                ICloudSave cloud = CloudSaveOverride ?? CreatePlatformCloud() ?? FakeCloudSave.WithoutSession();
                _cloudSession = new CloudSession(cloud, CloudFlags, DeviceIdentity);
            }

            _cloudSave = _cloudSession.Cloud;
            return _cloudSession;
        }

        // Play Games solo en el teléfono y con el APP_ID de Play Console; sin él, null (y la falsa sin sesión).
        private static ICloudSave? CreatePlatformCloud()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ISavedGamesPort? port = PlayGamesSavedGamesPort.TryCreate();
            if (port != null)
            {
                return new PlayGamesCloudSave(port, CloudFlags, () => Application.internetReachability != NetworkReachability.NotReachable, DeviceIdentity);
            }
#endif
            return null;
        }

        // 08 §2.3 y §7: con la banda 13-17 o 18+ ya guardada; sin banda, tras la pantalla de edad; con <13, nunca. Sin el
        // consentimiento en marcha (los tests de siempre), en el arranque.
        private static bool CloudAllowedAtBoot(Profile profile)
        {
            if (!ConsentEnabled)
            {
                return true;
            }

            ConsentRecord? record = ConsentStoreOverride != null ? ConsentStoreOverride.Load() : profile.Consent;
            return record != null && (record.AgeBand == AgeBand.Teen || record.AgeBand == AgeBand.Adult);
        }

        /// <summary>El arranque espera a la nube (solo si no respondió en el acto) hasta <see cref="CloudBootWaitMs"/>.</summary>
        private bool CloudBootPending => _cloudBootWindow && _cloudSession != null && _cloudSession.State == CloudSessionState.Starting;

        private IEnumerator WaitCloudAtBoot()
        {
            float waited = 0f;
            float limit = CloudBootWaitMsOverride ?? CloudBootWaitMs;
            while (CloudBootPending && waited < limit)
            {
                yield return null;
                waited += Time.unscaledDeltaTime * 1000f;
            }
        }

        // Cierra la ventana del arranque: desde aquí, lo que llegue de la nube llega tarde (TickCloud).
        private Profile EndCloudBootWindow(Profile profile)
        {
            Profile result = _cloudBootProfile ?? profile;
            _cloudBootWindow = false;
            _cloudBootProfile = null;
            _cloudBootStore = null;
            return result;
        }

        /// <summary>
        /// Primer arranque con el consentimiento: con la edad ya declarada (13-17 o 18+; <c>playGamesAllowed</c>), la nube arranca
        /// ahora, en segundo plano (08 §2.3: «Auth…Then(Save.CloudSyncAsync)»). Lo que traiga llega tarde.
        /// </summary>
        private void StartCloudAfterConsent(bool playGamesAllowed)
        {
            Profile? profile = _profile;
            if (!playGamesAllowed || profile == null || ProfileLocked || AccountDeleted)
            {
                return;
            }

            CloudSession session = EnsureCloudSession();
            if (session.State == CloudSessionState.Idle)
            {
                session.Start(() => _profile ?? profile, RouteCloudStart, SessionIntegrity);
            }
        }

        // La respuesta puede llegar con otra escena (una recarga por idioma): la recibe el arranque vivo.
        private static void RouteCloudStart(CloudSessionStart start)
        {
            GameBootstrap? boot = Current;
            if (boot != null && boot.isActiveAndEnabled)
            {
                boot.OnCloudStarted(start);
            }
        }

        private void OnCloudStarted(CloudSessionStart start)
        {
            CloudSyncReport? report = start.Report;
            if (report == null || start.State == CloudSessionState.Closed || ConsentBlocked || AccountDeleted)
            {
                return; // <13 declarado o cuenta borrada mientras la nube respondía: nada de lo que traiga se usa
            }

            bool late = !_cloudBootWindow;
            BootCloudSync = report;
            if (!report.Ran)
            {
                return;
            }

            if (report.Error != null)
            {
                Debug.LogWarning(LogPrefix + "nube: " + report.Error);
            }

            if (late)
            {
                _probe?.SaveSync(CloudMerge.ToId(report.Result), report.Reason.HasValue ? ReasonId(report.Reason.Value) : "none", report.Bytes);
            }

            if (!report.Replaced)
            {
                return;
            }

            if (!late)
            {
                Profile merged = report.Profile;
                ProfileIntegrity.Sign(merged, DeviceIdentity);
                _cloudBootStore?.TrySave(merged);
                _cloudBootProfile = merged;
                return;
            }

            if (report.Remote != null)
            {
                // TickCloud la vuelve a fusionar con el perfil vivo en cuanto se pueda (lo jugado o comprado mientras no se pierde).
                _cloudLateRemote = report.Remote;
                _cloudLateRemoteCoherent = !report.CloudIntegrity.HasValue || ProfileIntegrity.IsCoherent(report.CloudIntegrity.Value);
            }
        }

        /// <summary>
        /// Cada fotograma: el perfil de la nube que llegó tarde se vuelve a fusionar con el perfil vivo (<see cref="CloudMerge.Resolve"/>,
        /// las reglas de 08 §11: lo jugado o comprado mientras esperaba entra en la fusión, no se pisa con la fusión de cuando llegó).
        /// Si la fusión cambia el perfil de la sesión, se guarda firmado, se sube y la escena se recarga al inicio con él; si no, solo se
        /// sube si la nube quedó atrás. Espera con una compra, un borrado o la pantalla de consentimiento en curso. True si recargó.
        /// </summary>
        internal bool TickCloud()
        {
            Profile? remote = _cloudLateRemote;
            if (remote == null)
            {
                return false;
            }

            if (AccountDeleted || ConsentBlocked)
            {
                _cloudLateRemote = null;
                return false;
            }

            Profile? live = _profile;
            if (live == null || (_store != null && _store.IsPurchasing) || DeleteAccountBusy || AccountDeleteInFlight || ConsentHoldsSession)
            {
                return false;
            }

            _cloudLateRemote = null;
            PersistNow("nube"); // lo de esta sesión, en disco antes de fusionar
            CloudMergeResult merge = CloudMerge.Resolve(live, remote, ProfileIntegrity.IsCoherent(SessionIntegrity), _cloudLateRemoteCoherent);
            if (!merge.LocalChanged)
            {
                if (merge.CloudChanged)
                {
                    _cloudSession?.Upload(live); // gana el vivo: la nube se pone al día con él
                }

                return false;
            }

            Profile merged = merge.Merged;
            ProfileSaveBatch? batch = _saveBatch;
            if (batch != null)
            {
                batch.Save(merged); // firma (SigningProfileStore) y escribe
                batch.FlushSync(batch.SyncTimeoutMs);
                _saveBatch = null; // el cierre de esta escena (OnDestroy) ya no vuelve a escribir el perfil anterior
            }
            else
            {
                ProfileIntegrity.Sign(merged, DeviceIdentity);
                _profileStore?.TrySave(merged);
            }

            if (merge.CloudChanged)
            {
                _cloudSession?.Upload(merged); // la sesión de nube sigue tras la recarga
            }

            _profile = merged;
            RebuildUi(persist: false);
            return true;
        }

        // Las victorias, al bus (cada save_cloud_interval_levels, una subida).
        private void ConnectCloud()
        {
            _events?.Subscribe<LevelWon>(OnCloudLevelWon);
        }

        private void OnCloudLevelWon(LevelWon won)
        {
            CloudSession? session = _cloudSession;
            Profile? profile = _profile;
            if (session == null || profile == null || AccountDeleted || ProfileLocked || ConsentBlocked || CloudReplacePending)
            {
                return; // con una fusión tardía pendiente no se sube el local sin fusionar (TickCloud sube lo fusionado)
            }

            if (_rc != null && _rc.TryGet("save_cloud_interval_levels", out long interval))
            {
                session.IntervalLevels = (int)System.Math.Max(1L, System.Math.Min(5L, interval)); // 08 §9: 1-5
            }

            session.OnLevelWon(profile);
        }

        /// <summary>Sube el perfil de la sesión (tras una compra o al pasar a segundo plano). False si no sube.</summary>
        internal bool UploadCloud()
        {
            CloudSession? session = _cloudSession;
            Profile? profile = _profile;
            if (session == null || profile == null || AccountDeleted || ProfileLocked || ConsentBlocked || CloudReplacePending)
            {
                return false; // con una fusión tardía pendiente no se sube el local sin fusionar (TickCloud sube lo fusionado)
            }

            return session.Upload(profile);
        }

        /// <summary>Al volver de segundo plano: la fusión que no llegó (sin red al arrancar) o la subida pendiente de #3379.</summary>
        internal bool RetryCloud()
        {
            CloudSession? session = _cloudSession;
            if (session == null || _profile == null || AccountDeleted || ProfileLocked || ConsentBlocked || CloudReplacePending)
            {
                return false;
            }

            Profile current = _profile;
            return session.Retry(() => _profile ?? current, RouteCloudStart, SessionIntegrity);
        }

        // <13 declarado (al corregir la edad en Ajustes): la nube no vuelve a usarse en esta sesión.
        private void StopCloudForBlock()
        {
            _cloudLateRemote = null;
            _cloudSession?.Stop(); // lo que esté en vuelo ya no fusiona ni escribe (Closed, sin marca de borrado)
            _cloudSession = null;
            s_rebuildCloud = null;
        }

        // Tienda: tras cada compra acreditada, una subida (08 §11).
        private IStoreService WrapCloudStore(IStoreService inner)
        {
            return new CloudUploadStoreService(inner, () => UploadCloud());
        }
    }
}
