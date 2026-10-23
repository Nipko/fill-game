#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AF.UI
{
    /// <summary>
    /// «Eliminar cuenta y datos» en local y la nube sin SDK (bloque B8; 08 §11, filas «Eliminación de cuenta» y «Nube Android
    /// 1.0»; nota <c>docs/dev/nube-integridad.md</c>). Parcial nuevo: <c>GameBootstrap.cs</c> solo gana la llamada a
    /// <see cref="SetUpAccount"/> (tras crear Ajustes) y el caso del Atrás de Android (<see cref="CancelDeleteAccount"/>), con el
    /// parche <c>B8-ui.diff</c>.
    /// <list type="bullet">
    /// <item><see cref="SetUpAccount"/> crea el diálogo de dos pasos (<see cref="DeleteAccountDialog"/>) encima de Ajustes y le
    /// da la fila «Eliminar cuenta y datos» (<see cref="SettingsScreen.DeleteAccountPressed"/>).</item>
    /// <item>«Eliminar» (<see cref="ConfirmDeleteAccount"/>) pide primero el borrado en el servidor (F3-5; D-035: <c>POST
    /// /v1/account/delete</c>, <see cref="ServerAccountDelete"/>, que conecta <c>GameBootstrap.Backend.cs</c> con
    /// <c>AF.Backend.IBackendClient.DeleteAccount</c>) y, solo si el servidor lo confirma, hace la parte del cliente de 08 §11: borra <c>profile.json</c> y
    /// <c>profile.bak</c> (<see cref="IProfileStore.Delete"/>, por el lote, que antes cancela el escritor de segundo plano para que
    /// nada vuelva a escribir el perfil), <c>PlayerPrefs</c> y, si eso salió bien, el snapshot <c>af_main</c> de la nube
    /// (<see cref="ICloudSave"/>; sin Play Games, la falsa sin sesión, que no hace nada), y vuelve al estado de primera ejecución
    /// recargando la escena sin el perfil de la sesión (el arranque siguiente no encuentra perfil: entra al nivel 1, 08 §2.3).
    /// Si el servidor no lo confirma (sin red, <c>biz_backend_enabled</c> apagado), no se borra nada y el paso 2 dice «No se pudo
    /// eliminar» [P]. Sin borrado remoto (banda &lt; 13, o un build sin backend: nada se mandó a ningún servidor) solo se borra lo
    /// local. Ajustes no enseña el UID: enseña el código de borrado para la web de <c>IBackendClient.GetAccountCode</c> (Q-B13-21:
    /// aleatorio, distinto del UID y rotatorio; <c>GameBootstrap.Backend.cs</c>).</item>
    /// <item>La espera (prueba del dueño del 2026-10-23: «sin mostrar nada si cargando o procesando»): mientras el servidor
    /// borra, el diálogo dice «Eliminando tu cuenta y tus datos…» con los puntos de espera (<see cref="WaitDots"/>, quietos con
    /// Reducir movimiento), los botones desactivados y el Atrás de Android bloqueado (<see cref="DeleteAccountBusy"/>); TalkBack lo
    /// anuncia. Al terminar, «Tu cuenta y tus datos se eliminaron.» durante <see cref="DeleteDoneHoldMs"/> y un fundido de
    /// <see cref="DeleteFadeMs"/> (× <c>anim_reduce_motion_factor</c> con Reducir movimiento) antes de la recarga
    /// (<see cref="TickAccount"/>). Tras un fallo, «No se pudo eliminar» con «Reintentar», sin cerrar el diálogo.</item>
    /// <item>Sonda (solo en desarrollo): <c>AF_STATE af_account_delete</c> con <c>requested</c>, <c>confirmed</c>, <c>done</c>
    /// o <c>failed</c> (08 §8.2), y el <c>AF_LAYOUT delete_account</c> del diálogo.</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private DeleteAccountDialog? _deleteAccountDialog;
        private ICloudSave? _cloudSave;
        private float _deleteDoneMs = -1f;
        private bool _accountWiped;
        private bool _restartAsked;

        /// <summary>Lo que se ve «Tu cuenta y tus datos se eliminaron.» antes del fundido (ms) [P]: lo justo para leer una línea.</summary>
        public const float DeleteDoneHoldMs = 1800f;

        /// <summary>Fundido de la confirmación al velo antes de la recarga (ms) [P].</summary>
        public const float DeleteFadeMs = 300f;

        /// <summary>El fundido final en vigor (ms): <see cref="DeleteFadeMs"/>, × <c>anim_reduce_motion_factor</c> con Reducir movimiento.</summary>
        internal float DeleteFadeMsNow => ReduceMotionNow ? DeleteFadeMs * (float)(_rcConfigs?.A11y ?? A11yConfig.Defaults).AnimReduceMotionFactor : DeleteFadeMs;

        /// <summary>Sustituye <see cref="DeleteDoneHoldMs"/> en los tests (capturas); <see cref="ResetAccountOverrides"/> lo limpia.</summary>
        internal static float? DeleteDoneHoldMsOverride { get; set; }

        /// <summary>Nube para los tests (gana a la falsa sin sesión); <see cref="ResetAccountOverrides"/> la limpia.</summary>
        internal static ICloudSave? CloudSaveOverride { get; set; }

        /// <summary>Hace fallar el borrado local en los tests (simula un error de disco); <see cref="ResetAccountOverrides"/> lo limpia.</summary>
        internal static bool FailAccountDeleteForTests { get; set; }

        /// <summary>
        /// Sustituye el borrado de <c>PlayerPrefs</c> en los tests (los del Editor son los del proyecto entero: un test no los
        /// borra); <see cref="ResetAccountOverrides"/> lo limpia.
        /// </summary>
        internal static Action? WipePlayerPrefsForTests { get; set; }

        /// <summary>La banda de edad para los tests: true = menor de 13 (sin borrado remoto ni código); null: la del jugador.</summary>
        internal static bool? AgeUnder13Override { get; set; }

        /// <summary>Limpia los ganchos de test de la cuenta.</summary>
        internal static void ResetAccountOverrides()
        {
            CloudSaveOverride = null;
            FailAccountDeleteForTests = false;
            WipePlayerPrefsForTests = null;
            DeleteDoneHoldMsOverride = null;
            AnalyticsResetOverride = null;
            ResetBackendOverrides();
            ResetCloudSessionOverrides();
        }

        /// <summary>
        /// Borrado en el servidor (D-035; F3-5): <c>GameBootstrap.Backend.cs</c> le da una llamada a
        /// <c>IBackendClient.DeleteAccount</c>, que recibe una función para avisar si salió bien. Va <b>antes</b> del borrado local (plan del
        /// backend §9, F3-5: «el borrado llama al backend y luego al local»): si no sale bien, no se borra nada y se puede reintentar (el
        /// borrado del servidor es idempotente). Null (un build sin backend): solo el borrado local.
        /// </summary>
        public Action<Action<bool>>? ServerAccountDelete { get; set; }

        /// <summary>Hay un borrado en el servidor en vuelo (el paso 2 no admite otro toque).</summary>
        public bool AccountDeleteInFlight { get; private set; }

        /// <summary>
        /// Hay borrado remoto y código para la web (plan del backend §7, B12: «con la banda &lt; 13 no hay IAP ni borrado remoto»).
        /// </summary>
        public bool RemoteAccountAllowed => !(AgeUnder13Override ?? PlayerUnder13());

        // Plan del backend §7 («B12: … con la banda < 13 no hay IAP ni borrado remoto»): la banda guardada de la pantalla neutral de
        // 08 §2.3 (B12; GameBootstrap.Consent.cs), o el bloqueo del arranque. Sin el flujo (apagado en los tests) no hay banda: 13 o más.
        private bool PlayerUnder13() => ConsentBlocked || _consentFlow?.Record?.AgeBand == AgeBand.Under13;

        /// <summary>La nube del guardado (null antes de <see cref="SetUpAccount"/>).</summary>
        public ICloudSave? CloudSave => _cloudSave;

        /// <summary>Diálogo de dos pasos (tests; null antes de <see cref="SetUpAccount"/>).</summary>
        internal DeleteAccountDialog? DeleteAccountView => _deleteAccountDialog;

        /// <summary>El diálogo «Eliminar cuenta y datos» está abierto.</summary>
        public bool DeleteAccountOpen => _deleteAccountDialog != null && _deleteAccountDialog.Visible;

        /// <summary>
        /// El borrado espera al servidor o ya terminó (la confirmación antes de la recarga): ni la X ni el Atrás de Android cierran
        /// el diálogo y sus botones no responden.
        /// </summary>
        public bool DeleteAccountBusy => AccountDeleteInFlight || _accountWiped || (_deleteAccountDialog != null && _deleteAccountDialog.Busy);

        /// <summary>
        /// Los datos ya se borraron (desde el borrado local hasta que la escena se va; no vuelve a false): se ve la confirmación y la
        /// escena se recarga al acabar (<see cref="TickAccount"/>). Mientras, nada de esta escena escribe el perfil, llama al
        /// servidor ni avanza: cortan <c>OnApplicationPause</c> y <c>OnApplicationQuit</c>, <c>SessionsPaused</c>,
        /// <c>LogbookPause</c>, <c>ResumeBackend</c>, <c>RestoreLedgerNow</c>, <c>PushUiResumed</c> y los <c>Tick*</c> de los
        /// runners (sesiones, Bitácora, Mercado, meta, Reto, Salto y Crecida); y el lote del perfil queda cerrado
        /// (<see cref="ProfileSaveBatch.Closed"/>), que descarta cualquier guardado que aún llegue.
        /// </summary>
        public bool AccountDeleted => _accountWiped;

        /// <summary>
        /// Crea el diálogo encima de Ajustes y conecta su fila. False, sin crear nada, si Ajustes aún no existe; una segunda llamada
        /// no hace nada y devuelve true.
        /// </summary>
        public bool SetUpAccount()
        {
            if (_deleteAccountDialog != null)
            {
                return true;
            }

            SettingsScreen? settings = _settingsScreen;
            if (settings == null || _art == null)
            {
                Debug.LogError(LogPrefix + "SetUpAccount antes de crear Ajustes: «Eliminar cuenta y datos» no se crea.");
                return false;
            }

            EnsureCloudSession(); // D-042 (GameBootstrap.CloudSession.cs)
            _deleteAccountDialog = new DeleteAccountDialog(settings.OverlayRoot, _art, () => ContinueDeleteAccount(), () => ConfirmDeleteAccount(), () => CancelDeleteAccount());
            settings.DeleteAccountPressed = () => OpenDeleteAccount();
            ReportBootIntegrity();
            return true;
        }

        /// <summary>Sonda de la firma y de la nube del arranque (la implementa <c>GameBootstrap.Cloud.cs</c>, del parche de B8).</summary>
        partial void ReportBootIntegrity();

        /// <summary>Ajustes › «Eliminar cuenta y datos»: abre el paso 1. False si Ajustes no está abierta o el diálogo ya lo está.</summary>
        public bool OpenDeleteAccount()
        {
            SettingsScreen? settings = _settingsScreen;
            DeleteAccountDialog? dialog = _deleteAccountDialog;
            if (settings == null || dialog == null || !settings.Visible || dialog.Visible)
            {
                return false;
            }

            dialog.ShowWarn(settings.CanvasWidth, settings.CanvasHeight);
            _probe?.AccountDelete("requested");
            EmitDeleteAccountLayout();
            return true;
        }

        /// <summary>«Continuar» del paso 1: pasa al paso 2.</summary>
        public bool ContinueDeleteAccount()
        {
            DeleteAccountDialog? dialog = _deleteAccountDialog;
            if (dialog == null || dialog.Step != DeleteAccountStep.Warn)
            {
                return false;
            }

            dialog.ShowConfirm();
            EmitDeleteAccountLayout();
            return true;
        }

        /// <summary>
        /// X, «Conservar mis datos» o Atrás de Android: cierra el diálogo sin borrar y deja Ajustes como estaba. False, sin cerrar
        /// nada, mientras el borrado espera al servidor o ya terminó (<see cref="DeleteAccountBusy"/>).
        /// </summary>
        public bool CancelDeleteAccount()
        {
            DeleteAccountDialog? dialog = _deleteAccountDialog;
            if (dialog == null || !dialog.Visible || DeleteAccountBusy)
            {
                return false;
            }

            dialog.Hide();
            return true;
        }

        /// <summary>
        /// «Eliminar» (o «Reintentar») del paso 2: pide el borrado al servidor con «Eliminando…» a la vista, borra los datos del
        /// cliente, enseña la confirmación y vuelve al estado de primera ejecución (recarga la escena). Si algo falla, sigue en el
        /// paso 2 con «No se pudo eliminar» y «Reintentar», y no recarga. False si el paso 2 no está abierto.
        /// </summary>
        public bool ConfirmDeleteAccount()
        {
            DeleteAccountDialog? dialog = _deleteAccountDialog;
            if (dialog == null)
            {
                return false;
            }

            if (AccountDeleteInFlight)
            {
                return true; // ya se pidió al servidor: se espera su respuesta (un segundo toque no pide otro)
            }

            if (dialog.Step != DeleteAccountStep.Confirm)
            {
                return false;
            }

            _probe?.AccountDelete("confirmed");
            Action<Action<bool>>? server = RemoteAccountAllowed ? ServerAccountDelete : null;
            if (server == null)
            {
                FinishDeleteAccount(dialog);
                return true;
            }

            AccountDeleteInFlight = true;
            dialog.ShowDeleting(ReduceMotionNow);
            AnnounceAccount(AccountText.Deleting);
            EmitDeleteAccountLayout();
            server(ok =>
            {
                AccountDeleteInFlight = false;
                if (this == null || !enabled || _deleteAccountDialog != dialog)
                {
                    return; // la escena ya se fue
                }

                if (!ok)
                {
                    // El servidor no lo confirmó: no se borra nada aquí, para poder reintentar (idempotente) [P].
                    Debug.LogWarning(LogPrefix + "el servidor no confirmó el borrado de la cuenta (deleteAccount): no se borra nada");
                    _probe?.AccountDelete("failed");
                    if (dialog.Visible)
                    {
                        dialog.ShowFailure();
                        AnnounceAccount(AccountText.Failed);
                        EmitDeleteAccountLayout();
                    }

                    return;
                }

                FinishDeleteAccount(dialog); // la cuenta ya no existe en el servidor: lo local se borra aunque se cerrara el diálogo
            });
            return true;
        }

        // Parte del cliente de 08 §11, tras el servidor (o sin él): si falla, sigue en el paso 2 con «No se pudo eliminar».
        private void FinishDeleteAccount(DeleteAccountDialog dialog)
        {
            if (!DeleteAccountData(out string? error))
            {
                Debug.LogWarning(LogPrefix + "no se pudieron borrar los datos de la cuenta: " + error);
                _probe?.AccountDelete("failed");
                if (dialog.Visible)
                {
                    dialog.ShowFailure();
                    AnnounceAccount(AccountText.Failed);
                    EmitDeleteAccountLayout();
                }

                return;
            }

            _accountWiped = true; // desde aquí nada de esta escena escribe ni llama al servidor (AccountDeleted)
            _probe?.AccountDelete("done");
            if (!dialog.Visible)
            {
                RestartAsFirstRun(false); // el diálogo ya no se ve: nada que confirmar
                return;
            }

            // La confirmación breve, y la recarga cuando acaba (TickAccount): no un salto directo a la primera ejecución.
            dialog.ShowDone();
            AnnounceAccount(AccountText.Done);
            EmitDeleteAccountLayout();
            _deleteDoneMs = 0f;
        }

        /// <summary>
        /// Cada fotograma: los puntos de espera y, con la cuenta ya borrada, la confirmación (<see cref="DeleteDoneHoldMs"/>), el
        /// fundido y la recarga. True mientras se ve la confirmación: el resto del arranque no avanza (el perfil ya no existe y
        /// nada debe volver a escribirlo ni llamar al servidor).
        /// </summary>
        internal bool TickAccount(float dtMs)
        {
            DeleteAccountDialog? dialog = _deleteAccountDialog;
            if (dialog == null)
            {
                return false;
            }

            dialog.Tick(dtMs);
            if (!_accountWiped)
            {
                return false;
            }

            if (_restartAsked || _deleteDoneMs < 0f)
            {
                return true; // la recarga ya se pidió (llega el fotograma siguiente): nada más de esta escena
            }

            _deleteDoneMs += Mathf.Max(0f, dtMs);
            float hold = DeleteDoneHoldMsOverride ?? DeleteDoneHoldMs;
            float fade = DeleteFadeMsNow;
            if (_deleteDoneMs < hold)
            {
                return true;
            }

            float t = fade > 0f ? (_deleteDoneMs - hold) / fade : 1f;
            dialog.SetFade(t);
            if (t >= 1f)
            {
                _restartAsked = true;
                RestartAsFirstRun(true);
            }

            return true;
        }

        // TalkBack: el estado del borrado («Eliminando…», la confirmación o el fallo) y un recorrido nuevo de la pantalla.
        private void AnnounceAccount(string text)
        {
            A11yService? a11y = _a11y;
            if (a11y != null)
            {
                a11y.Announce(text);
                a11y.Invalidate();
            }
        }

        // Parte del cliente de 08 §11: perfil, PlayerPrefs y nube. Primero lo local: si falla, no se borra nada (tampoco la nube).
        // El resultado de la nube no bloquea: sin red, el snapshot se queda (y lo borrará deleteAccount, o el marcador de borrado
        // pendiente que llega con Play Games: nota B8 §8) [P].
        private bool DeleteAccountData(out string? error)
        {
            error = null;
            try
            {
                if (FailAccountDeleteForTests)
                {
                    throw new IOException("fallo simulado del borrado");
                }

                ProfileSaveBatch? batch = _saveBatch;
                if (batch != null)
                {
                    batch.Delete(); // cancela el escritor de segundo plano y borra profile.json y profile.bak
                }
                else
                {
                    _profileStore?.Delete();
                }
            }
            catch (IOException e)
            {
                error = e.Message;
                return false;
            }
            catch (UnauthorizedAccessException e)
            {
                error = e.Message;
                return false;
            }

            // Desde aquí nada vuelve a escribir el perfil: el cierre de la escena (PersistNow) no encuentra lote.
            _saveBatch = null;
            if (WipePlayerPrefsForTests != null)
            {
                WipePlayerPrefsForTests();
            }
            else
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            }

            ResetAnalyticsData(); // F3-6 (08 §13.3): resetAnalyticsData() de Firebase, con un ID de instancia nuevo

            // D-042: la marca de borrado pendiente (tras borrar PlayerPrefs, para que sobreviva), el borrado de af_main si hay sesión y
            // la sesión de nube cerrada: nada más se sube. El arranque siguiente comprueba que el snapshot quedó vacío (CloudErase).
            _cloudLateProfile = null;
            EnsureCloudSession().EraseForAccountDelete(result =>
            {
                if (result != CloudWriteResult.Ok && result != CloudWriteResult.NoSession)
                {
                    Debug.LogWarning(LogPrefix + "no se pudo borrar el snapshot " + CloudSaveNames.Snapshot + " de la nube: " + result + " (se reintenta al arrancar)");
                }
            });

            return true;
        }

        // Como RebuildUi, pero sin llevar el perfil de la sesión: el arranque siguiente es un arranque en frío sin perfil. Con
        // covered (la confirmación ya fundida en el velo), Ajustes no se cierra antes: se iría el velo y el inicio asomaría un
        // fotograma antes de la recarga; la escena entera se va igual.
        private void RestartAsFirstRun(bool covered)
        {
            s_rebuildToLobby = false;
            s_rebuildProfile = null;
            s_rebuildStore = null;
            s_rebuildReopenSettings = false;
            s_rebuildCloud = null; // D-042: la primera ejecución empieza con una nube nueva (la marca de borrado pendiente sigue)
            if (!covered)
            {
                _settingsScreen?.Hide();
            }

            enabled = false; // nada más de este arranque: la escena se va
            Scene scene = gameObject.scene;
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
#endif
        }

        private void EmitDeleteAccountLayout()
        {
            DeleteAccountDialog? dialog = _deleteAccountDialog;
            if (!DevProbe.Enabled || _probe == null || dialog == null || !dialog.Visible)
            {
                return;
            }

            var ids = new List<string>();
            var rects = new List<RectTransform>();
            dialog.VisibleButtons(ids, rects);
            _probe.Layout("delete_account", 0, null, ids.ToArray(), rects.ToArray());
        }
    }
}
