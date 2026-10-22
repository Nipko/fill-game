#nullable enable
using System;
using System.Collections.Generic;
using AnalyticsEvent = AF.Analytics.AnalyticsEvent;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// La UI de los avisos que le faltaba a B6 (nota <c>docs/dev/sesiones-notificaciones.md</c> §6; 06 §8.1): la tarjeta del pre-prompt
    /// (<see cref="PushPrePromptCard"/>) y la fila «Avisos» de Ajustes (<c>SettingsScreen.Push.cs</c>), sobre el
    /// <see cref="PushPermissionService"/> y el programador de <c>GameBootstrap.Sessions.cs</c>.
    /// <list type="bullet">
    /// <item><b>Cuándo sale la tarjeta</b> (B6-Q16 (a)): en la victoria (de campaña o del Reto) en que tocan las reglas de B6
    /// (<see cref="PushPrePromptRules"/>: la 1.ª victoria de la sesión <c>push_preprompt_session</c> con el nivel
    /// <c>push_preprompt_min_level</c> ya superado; el segundo, al alcanzar el hito 3 de la Crecida o a los 7 días) se apunta y sale
    /// al pulsar Siguiente o Inicio (también el avance solo tras la Cosecha doble), como pantalla aparte encima de la victoria y
    /// antes de lo que venga después; al contestar (y tras el diálogo nativo, si «Avísame» lo pidió) sigue el camino que se
    /// pulsó. Nunca en el arranque, ni sobre la edad o el consentimiento (B12: <see cref="ConsentHoldsSession"/>), ni con la banda
    /// &lt;13, y como mucho una por sesión.</item>
    /// <item><b>Con la cola de takeovers</b> (B4; D-033, añadido del 2026-10-20, punto 11: el pre-prompt es una pantalla aparte, no
    /// un takeover): no gasta el cupo de la sesión, pero tampoco se apila con otra ventana: en la victoria en que toca, esa
    /// victoria no enseña tarjeta de oferta (06 §8.1: «la Mochila pasa a la siguiente victoria»), y si se sale al inicio, esa
    /// entrada no abre la libreta sola (sale en la siguiente entrada de la sesión) [P].</item>
    /// <item><b>Fila «Avisos»</b>: activados, desactivados o bloqueados en el teléfono (<see cref="PushUiRules.RowState"/>). Encender
    /// pide el permiso si hace falta; con el teléfono bloqueándolos, abre los ajustes de la app en Android y, al volver con el
    /// permiso dado, los enciende; apagar cancela lo programado y guarda <c>settings.push</c>. TalkBack lee el estado y lo anuncia
    /// al cambiar (B9).</item>
    /// <item><b>Analítica</b> (08 §8.2): <c>af_push_prompt</c> con <c>step</c> y <c>result</c> en cada paso que informa
    /// <see cref="PushPermissionService.Reported"/> (pre-prompt y diálogo nativo; el <c>origin</c> [P] de B6 solo va a la sonda: el
    /// catálogo de 08 §8.2 no lo tiene).</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>
        /// Tras «Avísame», cuánto se espera como mucho (ms, con la app delante) a la respuesta del diálogo nativo antes de seguir el
        /// camino de la victoria igualmente [P]: un diálogo que nunca contesta no deja al jugador en la victoria.
        /// </summary>
        public const float PrePromptNativeWaitMs = 5000f;

        private PushPrePromptCard? _prePromptCard;
        private PrePromptKind _prePromptDue;
        private PrePromptMoment _prePromptMoment;
        private bool _prePromptDueDaily;
        private Action? _prePromptLeave;
        private bool _prePromptLeaveToLobby;
        private float _prePromptWaitMs;
        private bool _pushOpenedPhone;

        // Sesión (stats.sessions) en que salió el último pre-prompt: como mucho uno por sesión. Sigue con la reconstrucción por
        // idioma (el mismo perfil y la misma sesión, como el contador de GameBootstrap.Sessions.cs).
        private static int s_prePromptSession;

        /// <summary>
        /// Tests: false = sin tarjeta del pre-prompt (los tests que no tratan de ella ganan y pulsan Siguiente como antes; los pone
        /// <c>PlayModeHelpers.NoPrePromptUnlessSet</c>); null o true = con ella (el juego). <see cref="PrePromptOverride"/> gana.
        /// </summary>
        internal static bool? PrePromptCardOverride { get; set; }

        /// <summary>Tests: lo que hace «Abrir ajustes del teléfono» (por defecto, <see cref="AndroidAppSettings.OpenNotificationSettings"/>).</summary>
        internal static Func<bool>? OpenPhoneSettingsOverride { get; set; }

        /// <summary>La tarjeta del pre-prompt (null sin sesiones o con <see cref="PrePromptCardOverride"/> = false).</summary>
        internal PushPrePromptCard? PrePromptCard => _prePromptCard;

        /// <summary>La tarjeta del pre-prompt está a la vista.</summary>
        internal bool PrePromptCardVisible => _prePromptCard != null && _prePromptCard.Visible;

        /// <summary>El pre-prompt que saldrá al dejar la victoria a la vista (<see cref="PrePromptKind.None"/> si ninguno).</summary>
        public PrePromptKind PrePromptPending => _prePromptDue;

        /// <summary>Hay un pre-prompt en marcha al dejar una victoria (la tarjeta o el diálogo nativo): el camino espera.</summary>
        internal bool PrePromptHoldsLeave => _prePromptLeave != null;

        /// <summary>Estado de la fila «Avisos» de Ajustes ahora.</summary>
        internal PushRowState PushRowNow
        {
            get
            {
                PushPermissionService? push = _push;
                INotificationScheduler? scheduler = _notificationScheduler;
                return PushUiRules.RowState(push != null && scheduler != null, PlayerUnder13(), push?.Settings.Granted ?? false,
                    scheduler?.Permission ?? PushPermission.NotRequested);
            }
        }

        // Al crear las sesiones (SetUpSessions): la tarjeta por defecto, salvo la de los tests, y los toques de la fila de Ajustes.
        private IPushPrePromptPresenter? CreatePrePromptPresenter(bool rebuilt)
        {
            if (!rebuilt)
            {
                s_prePromptSession = 0;
            }

            if (_settingsScreen != null)
            {
                _settingsScreen.PushPressed = () => PressPushRow();
                _settingsScreen.PushOpenPhonePressed = () => PressPushOpenPhone();
            }

            if (PrePromptOverride != null)
            {
                return PrePromptOverride;
            }

            if (!(PrePromptCardOverride ?? true) || _art == null)
            {
                return null;
            }

            _prePromptCard = new PushPrePromptCard(transform, _art);
            return _prePromptCard;
        }

        /// <summary>
        /// El pre-prompt que tocaría en una victoria con <paramref name="maxLevelBeforeVictory"/> (sin enseñarlo ni apuntarlo): las
        /// reglas de B6 con las guardas de la UI (<see cref="PushUiRules.CanOffer"/>).
        /// </summary>
        internal PrePromptKind PrePromptDueNow(int maxLevelBeforeVictory, out PrePromptMoment moment)
        {
            moment = default;
            SessionTracker? sessions = _sessions;
            PushPermissionService? push = _push;
            if (sessions == null || push == null || _sessionClock == null)
            {
                return PrePromptKind.None;
            }

            if (!PushUiRules.CanOffer(_prePrompt != null, PlayerUnder13(), ConsentHoldsSession, push.Asking || _prePromptLeave != null,
                    sessions.Sessions, s_prePromptSession))
            {
                return PrePromptKind.None;
            }

            moment = new PrePromptMoment(sessions.Sessions, maxLevelBeforeVictory, _profile?.Streak?.Wins ?? 0, _sessionClock.UtcNow);
            return push.Due(in moment);
        }

        /// <summary>
        /// La victoria de campaña de <paramref name="wonLevel"/> lleva el pre-prompt al dejarla (para el Mercado: esa victoria no
        /// enseña tarjeta de oferta; 06 §8.1).
        /// </summary>
        internal bool PrePromptDueAtCampaignVictory(int wonLevel)
        {
            return PrePromptDueNow(Math.Max(0, wonLevel - 1), out _) != PrePromptKind.None;
        }

        // La victoria (de campaña o del Reto, <paramref name="daily"/>) se dejó por otro camino (TickSessions): lo apuntado se olvida.
        private void ForgetPrePromptAtVictory(bool daily)
        {
            if (_prePromptDue != PrePromptKind.None && _prePromptDueDaily == daily)
            {
                _prePromptDue = PrePromptKind.None;
            }
        }

        // Al entrar en la pantalla de victoria (TickSessions): apunta el pre-prompt que toca, que saldrá al dejarla.
        private void NotePrePromptAtVictory(int maxLevelBeforeVictory, bool daily)
        {
            _prePromptDue = PrePromptDueNow(maxLevelBeforeVictory, out _prePromptMoment);
            _prePromptDueDaily = daily;
        }

        /// <summary>
        /// Siguiente o Inicio de la victoria (<paramref name="daily"/>: la del Reto): si esta victoria lleva el pre-prompt, enseña la
        /// tarjeta y devuelve true; <paramref name="leave"/> sigue el camino al contestar (tras el diálogo nativo, si «Avísame» lo
        /// pidió). Si no, false y no hace nada.
        /// </summary>
        internal bool TryPrePromptOnLeave(bool daily, bool toLobby, Action leave)
        {
            PrePromptKind kind = _prePromptDue;
            if (kind == PrePromptKind.None || _prePromptDueDaily != daily)
            {
                return false;
            }

            _prePromptDue = PrePromptKind.None;
            PushPermissionService? push = _push;
            IPushPrePromptPresenter? presenter = _prePrompt;
            SessionTracker? sessions = _sessions;
            PrePromptMoment moment = _prePromptMoment;
            if (push == null || presenter == null || sessions == null
                || !PushUiRules.CanOffer(true, PlayerUnder13(), ConsentHoldsSession, push.Asking || _prePromptLeave != null, sessions.Sessions, s_prePromptSession)
                || push.Due(in moment) != kind)
            {
                return false; // algo cambió desde la victoria (otra sesión, el bloqueo de <13…): el camino sigue sin tarjeta
            }

            s_prePromptSession = sessions.Sessions;
            _prePromptLeave = leave ?? throw new ArgumentNullException(nameof(leave));
            _prePromptLeaveToLobby = toLobby;
            _prePromptWaitMs = 0f;
            push.OnVictory(in moment, presenter, _ => FinishPrePromptLeave());
            _a11y?.Invalidate();
            if (DevProbe.Enabled && _prePromptCard != null && _prePromptCard.Visible && _probe != null)
            {
                var ids = new List<string>(2);
                var rects = new List<RectTransform>(2);
                _prePromptCard.VisibleButtons(ids, rects);
                _probe.Layout("push_preprompt", 0, null, ids.ToArray(), rects.ToArray());
            }

            return true;
        }

        /// <summary>Atrás de Android con la tarjeta delante: «Ahora no» (B6: cerrarla cuenta como «Ahora no»). False si no se ve.</summary>
        public bool PressPrePromptBack()
        {
            if (!PrePromptCardVisible)
            {
                return false;
            }

            _prePromptCard!.Close(false);
            return true;
        }

        // Cada fotograma (TickSessions): la tarjeta a la pantalla y, tras «Avísame», la espera del diálogo nativo con su tope.
        internal void TickPushUi(float dtMs)
        {
            _prePromptCard?.Tick();
            if (_prePromptLeave == null || PrePromptCardVisible)
            {
                return;
            }

            _prePromptWaitMs += Mathf.Min(dtMs, 100f); // un fotograma largo (vuelta de segundo plano) no se come la espera
            if (_prePromptWaitMs >= PrePromptNativeWaitMs)
            {
                Debug.LogWarning(LogPrefix + "el diálogo nativo de los avisos no contestó: la victoria sigue su camino.");
                FinishPrePromptLeave();
            }
        }

        // El pre-prompt acabó (respuesta del jugador y, si la hubo, del diálogo nativo): sigue el camino que se pulsó, una vez.
        private void FinishPrePromptLeave()
        {
            Action? leave = _prePromptLeave;
            if (leave == null)
            {
                return;
            }

            _prePromptLeave = null;
            if (_prePromptLeaveToLobby)
            {
                SkipNextLobbyTakeover(); // la entrada al inicio que sigue no apila la libreta sobre el pre-prompt [P]
            }

            leave();
            RefreshPushRow();
        }

        // af_push_prompt (08 §8.2): step y result; el origin [P] de B6 no está en el catálogo y solo va a la sonda.
        private void LogPushPrompt(PushPromptEvent e)
        {
            _analytics?.Log(AnalyticsEvent.Create("af_push_prompt").Add("step", e.Step).Add("result", e.Result));
        }

        /// <summary>Pone la fila «Avisos» de Ajustes en su estado de ahora (al abrir Ajustes y tras cada cambio).</summary>
        internal void RefreshPushRow()
        {
            _settingsScreen?.ShowPush(PushRowNow);
        }

        /// <summary>
        /// Toque en la fila «Avisos» (Ajustes abierta): con los avisos encendidos, los apaga (cancela lo programado y guarda
        /// <c>settings.push</c>); apagados, los enciende (con el diálogo nativo si hace falta); bloqueados en el teléfono, abre sus
        /// ajustes. False si no hay fila.
        /// </summary>
        public bool PressPushRow()
        {
            SettingsScreen? settings = _settingsScreen;
            PushPermissionService? push = _push;
            if (settings == null || !settings.Visible || push == null)
            {
                return false;
            }

            switch (PushUiRules.ActionFor(PushRowNow))
            {
                case PushRowAction.TurnOff:
                    push.SetFromSettings(false, _ => AfterPushChange());
                    return true;
                case PushRowAction.TurnOn:
                    push.SetFromSettings(true, _ => AfterPushChange());
                    return true;
                case PushRowAction.OpenPhoneSettings:
                    return PressPushOpenPhone();
                default:
                    return false;
            }
        }

        /// <summary>
        /// «Abrir ajustes del teléfono» (y el toque en la fila con los avisos bloqueados): abre los ajustes de notificaciones de la app
        /// en Android. Al volver con el permiso dado, los avisos se encienden (<see cref="PushUiResumed"/>). False si no se abrió.
        /// </summary>
        public bool PressPushOpenPhone()
        {
            SettingsScreen? settings = _settingsScreen;
            if (settings == null || !settings.Visible || PushRowNow != PushRowState.Blocked)
            {
                return false;
            }

            Func<bool>? open = OpenPhoneSettingsOverride;
            bool opened = open != null ? open() : AndroidAppSettings.OpenNotificationSettings();
            _pushOpenedPhone = opened;
            _probe?.SettingsChange(SettingsScreen.PushOpenPhoneId, opened ? "true" : "false", null);
            return opened;
        }

        /// <summary>
        /// La app vuelve de segundo plano (<c>OnApplicationPause(false)</c>): si el jugador fue a los ajustes del teléfono desde la
        /// fila y dio el permiso, los avisos se encienden como desde Ajustes; con Ajustes abierta, la fila se pone al día.
        /// </summary>
        internal void PushUiResumed()
        {
            PushPermissionService? push = _push;
            bool fromPhone = _pushOpenedPhone;
            _pushOpenedPhone = false;
            if (fromPhone && push != null && _notificationScheduler != null && _notificationScheduler.Permission == PushPermission.Allowed
                && !push.Settings.Granted)
            {
                push.SetFromSettings(true, _ => AfterPushChange());
                return;
            }

            if (_settingsScreen != null && _settingsScreen.Visible)
            {
                RefreshPushRow();
            }
        }

        // Tras encender o apagar desde Ajustes: la fila, el anuncio de TalkBack (B9) y la sonda (settings_change con la clave push [P]).
        private void AfterPushChange()
        {
            PushRowState state = PushRowNow;
            RefreshPushRow();
            A11yService? a11y = _a11y;
            if (a11y != null)
            {
                a11y.Announce(PushText.A11yLabel(state));
                a11y.Invalidate();
            }

            _probe?.SettingsChange(SettingsScreen.PushId, state == PushRowState.On ? "true" : "false", null);
        }
    }
}
