#nullable enable
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Parcial del Atrás de Android en el nivel, la pausa y el inicio (02 §3.5; observación 21 de la jornada 6, decisión del dueño
    /// del 2026-10-30): el nivel y el Reto pasan por <see cref="GameScreen.AndroidBack"/>; en el inicio sin nada encima, Atrás manda
    /// la app a segundo plano (<see cref="IAppTaskPort"/>), sin diálogo. Cada pulsación escribe
    /// <c>AF_STATE {"event":"back","t":…,"screen":"pause","action":"resume","reto":false}</c> (<see cref="DevProbe.Back"/>).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>
        /// Entre dos peticiones de mandar la app al fondo, como mínimo (ms de tiempo real): un doble Atrás rápido en el inicio pide
        /// una sola vez; el segundo se consume sin volver a llamar a Android.
        /// </summary>
        internal const float MoveToBackRepeatMs = 1000f;

        private static readonly FakeAppTaskPort s_editorAppTask = new FakeAppTaskPort();

        // Lo que hizo la última pulsación de Atrás, para la sonda y el audio (null: lo decide el resultado).
        private string? _backActionNote;
        private bool _backBlockedNote;
        private bool _backReto;
        private float _movedToBackAt = -1f;

        /// <summary>Tests: el puerto que manda la app al fondo (null = Android en el teléfono; en el Editor, uno falso).</summary>
        internal static IAppTaskPort? AppTaskPortOverride { get; set; }

        /// <summary>El puerto falso del Editor cuando ningún test pone el suyo.</summary>
        internal static FakeAppTaskPort EditorAppTask => s_editorAppTask;

        /// <summary>Pantalla de la última pulsación de Atrás (la de la sonda; null antes de la primera).</summary>
        internal string? LastBackScreen { get; private set; }

        /// <summary>Lo que hizo la última pulsación de Atrás (la de la sonda; null antes de la primera).</summary>
        internal string? LastBackAction { get; private set; }

        /// <summary>
        /// Atrás de Android, el único camino (la tecla en <c>Update</c> y los tests): decide con <see cref="PressAndroidBackCore"/> y
        /// escribe la sonda <c>AF_STATE back</c> con la pantalla de antes y lo que hizo (<c>pause</c>, <c>resume</c>,
        /// <c>cancel_to_pause</c>, <c>cancel</c>, <c>close</c>, <c>background</c>, <c>blocked</c> o <c>none</c>).
        /// </summary>
        public bool PressAndroidBack()
        {
            _backActionNote = null;
            _backBlockedNote = false;
            _backReto = false;
            string screen = BackScreenNow();
            bool done = PressAndroidBackCore();
            string action = _backActionNote ?? (done ? "close" : (_backBlockedNote || BackBlocked() ? "blocked" : "none"));
            LastBackScreen = screen;
            LastBackAction = action;
            DevProbe.Back(screen, action, _backReto);
            return done;
        }

        // El nivel de campaña o el Reto: lo que diga su pantalla (null: nada).
        private bool LevelBack(GameScreen? view, bool reto)
        {
            _backReto = reto;
            LevelBackAction action = view != null ? view.AndroidBack() : LevelBackAction.None;
            _backActionNote = GameScreen.BackActionName(action);
            _backBlockedNote = action == LevelBackAction.Blocked;
            return action != LevelBackAction.None && action != LevelBackAction.Blocked;
        }

        // El inicio sin nada encima: la app al fondo, como Android (sin «¿Salir?»). Un segundo Atrás antes de MoveToBackRepeatMs
        // se consume sin pedirlo otra vez.
        private bool MoveAppToBack()
        {
            _backActionNote = "background";
            float now = Time.realtimeSinceStartup * 1000f;
            if (_movedToBackAt >= 0f && now - _movedToBackAt < MoveToBackRepeatMs)
            {
                return true;
            }

            IAppTaskPort port = AppTaskPortOverride ?? (Application.isEditor ? s_editorAppTask : (IAppTaskPort)new AndroidAppTaskPort());
            if (!port.MoveToBack())
            {
                _backActionNote = "none";
                return false;
            }

            _movedToBackAt = now;
            return true;
        }

        // Al volver de segundo plano, el siguiente Atrás en el inicio vuelve a mandar la app al fondo.
        private void BackResumed()
        {
            _movedToBackAt = -1f;
        }

        // Lo que se ve, para la sonda: la hoja de encima o la pantalla (antes de que Atrás la cambie). Solo al pulsar Atrás.
        private string BackScreenNow()
        {
            AppFlow? app = _app;
            if (app == null || app.Screen == AppScreen.Boot)
            {
                return "boot";
            }

            if (_consentScreen != null && _consentScreen.Visible)
            {
                return "consent";
            }

            if (PrePromptCardVisible)
            {
                return "push_prompt";
            }

            if ((_storeSheet != null && _storeSheet.Visible) || (_adOverlay != null && _adOverlay.Visible) || (_lockedNotice != null && _lockedNotice.Visible))
            {
                return "blocking_sheet";
            }

            if (FruitSheetOpen)
            {
                return "fruit_sheet";
            }

            if (AvatarScreenOpen)
            {
                return "avatar";
            }

            if (_mapScreen != null && _mapScreen.Visible)
            {
                return "map";
            }

            if (_logbookScreen != null && _logbookScreen.Visible)
            {
                return "logbook";
            }

            if (_dailyScreen != null && _dailyScreen.IsOpen)
            {
                return _dailyScreen.Calendar.Visible || _dailyScreen.View == null ? "daily_calendar" : _dailyScreen.View.BackScreenName();
            }

            if (MarketCommerceOpen)
            {
                return "market";
            }

            if (LeagueOpen)
            {
                return "league";
            }

            if (app.Screen == AppScreen.Level)
            {
                return _skipConfirm != null && _skipConfirm.Visible ? "skip_confirm" : _screen != null ? _screen.BackScreenName() : "level";
            }

            if (DeleteAccountOpen)
            {
                return "delete_account";
            }

            if (_settingsScreen != null && _settingsScreen.Visible)
            {
                return "settings";
            }

            return app.Screen == AppScreen.Lobby ? "lobby" : AppScreenNames.Of(app.Screen);
        }
    }
}
