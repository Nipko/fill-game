#nullable enable
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>Lo que hizo el Atrás de Android en la pantalla del nivel (sonda <c>AF_STATE back</c>; 02 §3.5).</summary>
    internal enum LevelBackAction
    {
        /// <summary>Nada que hacer aquí (la intro de la victoria, la tarjeta previa, la ola de Barajar, un vertido que resuelve).</summary>
        None = 0,

        /// <summary>Abre la pausa, como su botón (jugando o en la intro, sin modal).</summary>
        Pause = 1,

        /// <summary>Cierra la pausa, como «Continuar».</summary>
        Resume = 2,

        /// <summary>«¿Salir del nivel?» (o ¿Reiniciar? abierto desde la pausa): cancela sin gastar la canoa y vuelve a la pausa.</summary>
        CancelToPause = 3,

        /// <summary>Cierra sin coste lo que está delante (¿Reiniciar?, la tarjeta de Morpho, la hoja o la tarjeta de Barajar).</summary>
        Cancel = 4,

        /// <summary>Nivel fallido y Sin canoas: lo mismo que su X.</summary>
        Close = 5,

        /// <summary>Un modal del embudo que Atrás no cierra (Aguas estancadas, ¿Seguro?): vibración corta (04 §6.6).</summary>
        Blocked = 6,
    }

    /// <summary>
    /// Parcial del Atrás de Android en el nivel (observación 21 de la jornada 6, decisión del dueño del 2026-10-30; 02 §3.5). Lo
    /// llama <c>GameBootstrap.PressAndroidBack</c> con la pantalla del nivel de campaña o la del Reto, que se comporta como un
    /// nivel. Usa los mismos caminos que los botones: la pausa (<see cref="OnPause"/>), «Continuar» (<see cref="OnResume"/>), la X
    /// de «¿Salir del nivel?» (<see cref="OnConfirmCancel"/>) y la de Nivel fallido (<c>PressLevelFailedClose</c>).
    /// </summary>
    public sealed partial class GameScreen
    {
        // ¿Reiniciar? se abrió desde la pausa (OnPauseRestart): Atrás vuelve a ella, como con «¿Salir del nivel?».
        private bool _restartFromPause;

        /// <summary>
        /// Nombre de lo que se ve para la sonda <c>AF_STATE back</c>: la pausa y sus confirmaciones, las hojas del nivel, el embudo
        /// (con los nombres de su <c>AF_LAYOUT</c>) o la fase (<c>level</c> jugando).
        /// </summary>
        internal string BackScreenName()
        {
            LevelFlow? flow = _flow;
            if (flow == null)
            {
                return "level";
            }

            if (_confirm.Visible)
            {
                return _confirm.Kind == ConfirmKind.Quit ? "quit_confirm" : "restart_confirm";
            }

            if (_pause.Visible)
            {
                return "pause";
            }

            switch (flow.Phase)
            {
                case FlowPhase.StuckModal: return "stuck";
                case FlowPhase.GiveUpConfirm: return GiveUpConfirmLayout;
                case FlowPhase.LevelFailed: return LevelFailedLayout;
                case FlowPhase.NoCanoes: return NoCanoesLayout;
                case FlowPhase.Celebration: return "celebration";
                case FlowPhase.VictoryScreen:
                case FlowPhase.AdGate: return "victory";
                case FlowPhase.LevelCard: return "level_card";
                case FlowPhase.Loading: return "loading";
            }

            if (_hintCard.Visible)
            {
                return "hint_answer";
            }

            if (_shuffleConfirm.Visible)
            {
                return "shuffle_confirm";
            }

            if (_shuffleCard.Visible)
            {
                return "shuffle_card";
            }

            return flow.Phase == FlowPhase.Intro ? "intro" : "level";
        }

        /// <summary>
        /// Atrás de Android en el nivel (02 §3.5; D-063): con «¿Salir del nivel?», la cancela y vuelve a la pausa (sin gastar la
        /// canoa); con ¿Reiniciar?, la cancela (y vuelve a la pausa si se abrió desde ella); con la pausa, «Continuar»; en Nivel
        /// fallido y en Sin canoas, su X; con la tarjeta de Morpho o la hoja o la
        /// tarjeta de Barajar, las cierra sin coste; jugando (o en la intro) sin nada delante, abre la pausa como su botón. Aguas
        /// estancadas y ¿Seguro? no se cierran (<see cref="LevelBackAction.Blocked"/>: vibración corta, 04 §6.6). Mientras
        /// un vertido resuelve el nivel (victoria o bloqueo a la espera de la vista), durante la ola de Barajar, la celebración, la
        /// victoria y la tarjeta previa no hace nada: así nunca se abre una pausa que la victoria o el modal cerrarían en el
        /// fotograma siguiente. Sin asignaciones.
        /// </summary>
        internal LevelBackAction AndroidBack()
        {
            LevelFlow? flow = _flow;
            if (flow == null || !Visible || !flow.HasLevel)
            {
                return LevelBackAction.None;
            }

            if (_confirm.Visible)
            {
                // «¿Salir del nivel?» siempre sale de la pausa; ¿Reiniciar?, solo si se abrió desde ella (no desde la cabecera ni
                // desde la tarjeta de Morpho): en los dos casos, Atrás vuelve a la pausa.
                bool toPause = _confirm.Kind == ConfirmKind.Quit || _restartFromPause;
                OnConfirmCancel();
                if (!toPause)
                {
                    return LevelBackAction.Cancel;
                }

                OnPause();
                return _pause.Visible ? LevelBackAction.CancelToPause : LevelBackAction.Cancel;
            }

            if (_pause.Visible)
            {
                OnResume();
                return LevelBackAction.Resume;
            }

            switch (flow.Phase)
            {
                case FlowPhase.LevelFailed:
                    return _failed.Visible && _actions.PressLevelFailedClose() ? LevelBackAction.Close : LevelBackAction.None;
                case FlowPhase.NoCanoes:
                    // Sin canoas en el nivel: como su X («esperar»), igual que Nivel fallido y que Sin canoas en el inicio (D-063).
                    return _noCanoes.Visible && _actions.PressNoCanoesClose() ? LevelBackAction.Close : LevelBackAction.None;
                case FlowPhase.StuckModal:
                case FlowPhase.GiveUpConfirm:
                    return LevelBackAction.Blocked;
                case FlowPhase.Playing:
                case FlowPhase.Intro:
                    break;
                default:
                    return LevelBackAction.None;
            }

            if (_hintCard.Visible)
            {
                OnHintCardClose();
                return LevelBackAction.Cancel;
            }

            if (_shuffleConfirm.Visible)
            {
                OnShuffleCancel();
                return LevelBackAction.Cancel;
            }

            if (_shuffleCard.Visible)
            {
                OnShuffleCardDismissed();
                return LevelBackAction.Cancel;
            }

            if (flow.IsResolving || _shuffleWave.Playing || !_pauseButton.Button.gameObject.activeInHierarchy)
            {
                return LevelBackAction.None;
            }

            OnPause();
            return _pause.Visible ? LevelBackAction.Pause : LevelBackAction.None;
        }

        /// <summary>Nombre de <paramref name="action"/> en la sonda <c>AF_STATE back</c>.</summary>
        internal static string BackActionName(LevelBackAction action)
        {
            switch (action)
            {
                case LevelBackAction.Pause: return "pause";
                case LevelBackAction.Resume: return "resume";
                case LevelBackAction.CancelToPause: return "cancel_to_pause";
                case LevelBackAction.Cancel: return "cancel";
                case LevelBackAction.Close: return "close";
                case LevelBackAction.Blocked: return "blocked";
                default: return "none";
            }
        }
    }
}
