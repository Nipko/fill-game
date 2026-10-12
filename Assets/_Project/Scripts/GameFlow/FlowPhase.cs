#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Estados de la pantalla de nivel (GDD 02 §2) con el embudo de D-008 (tareas B2-2 y B2-3): GIVE_UP_CONFIRM,
    /// LEVEL_FAILED y NO_CANOES; y AD_GATE tras «Siguiente» (tarea B2-4). FAIL_RESOLVE es instantáneo
    /// (<see cref="FailResolve"/>); SKIP_RESOLVE (Salto del Boto) y SUSPENDED quedan fuera (el estado ya está guardado tras
    /// cada vertido: <see cref="LevelStateSnapshot"/>).
    /// </summary>
    public enum FlowPhase
    {
        /// <summary>LOADING: se construye el estado del nivel.</summary>
        Loading = 0,

        /// <summary>INTRO: Sumergir y teñir (02 §4.2), saltable con un toque.</summary>
        Intro = 1,

        /// <summary>PLAYING: input activo.</summary>
        Playing = 2,

        /// <summary>CELEBRATION: victoria (R12), 900 ms o toque.</summary>
        Celebration = 3,

        /// <summary>
        /// VICTORY_SCREEN: pantalla de victoria con Siguiente; sin auto-avance por tiempo. Tras cobrar la Cosecha doble, la vista
        /// avanza sola por el camino de Siguiente (D-011, punto 4).
        /// </summary>
        VictoryScreen = 4,

        /// <summary>STUCK_MODAL: modal Aguas estancadas (02 §7).</summary>
        StuckModal = 5,

        /// <summary>GIVE_UP_CONFIRM: ¿Seguro? [P] tras «Rendirse», desde el 31 y con canoa en juego (D-008, paso 2).</summary>
        GiveUpConfirm = 6,

        /// <summary>LEVEL_FAILED: Nivel fallido [P] tras el fallo duro desde el 31 (D-008, paso 3).</summary>
        LevelFailed = 7,

        /// <summary>NO_CANOES: Sin canoas (04 §7.3) al reintentar o reiniciar con 0 canoas desde el 31 (D-008, paso 4).</summary>
        NoCanoes = 8,

        /// <summary>
        /// AD_GATE: tras tocar «Siguiente» en VICTORY_SCREEN (02 §2; 05 §6.1 y §6.3; D-008 punto 4): el intersticial simulado,
        /// si las reglas lo permiten, y después LOADING(N + 1). Nunca por temporizador (<c>victory_autonext_s</c> = 0): solo tras
        /// tocar Siguiente o, tras la Cosecha doble, por el avance de la vista (D-011, punto 4), cuando <c>AdPolicy</c> bloquea el
        /// intersticial (<c>AfterRewarded</c>, 60 s tras un recompensado).
        /// </summary>
        AdGate = 9,

        /// <summary>
        /// LEVEL_CARD: tarjeta previa de 1 toque entre LOADING e INTRO (02 §10; propuesta de D-009 §c.1), solo con
        /// <see cref="LevelFlow.LevelCardsEnabled"/> y en un nivel Difícil o Muy difícil o en un estreno (<c>intro_mechanic</c>,
        /// donde el tutorial hace de tarjeta). Un toque en cualquier punto la cierra y arranca la intro
        /// (<see cref="LevelFlow.DismissLevelCard"/>). No sale al reanudar (02 §14) ni, por defecto, al reintentar [P]. También la
        /// usa el banner de entrada a un mundo (<see cref="LevelFlow.WorldBanner"/>; 07 §2.2, regla 1), que va antes de la tarjeta.
        /// </summary>
        LevelCard = 10,
    }

    /// <summary>Nombres de los estados tal como los escribe 02 §2 (los usa la línea <c>AF_STATE</c>).</summary>
    public static class FlowPhaseNames
    {
        /// <summary>Nombre canónico del estado (p. ej. <c>VICTORY_SCREEN</c>).</summary>
        public static string Of(FlowPhase phase)
        {
            switch (phase)
            {
                case FlowPhase.Loading: return "LOADING";
                case FlowPhase.Intro: return "INTRO";
                case FlowPhase.Playing: return "PLAYING";
                case FlowPhase.Celebration: return "CELEBRATION";
                case FlowPhase.VictoryScreen: return "VICTORY_SCREEN";
                case FlowPhase.StuckModal: return "STUCK_MODAL";
                case FlowPhase.GiveUpConfirm: return "GIVE_UP_CONFIRM";
                case FlowPhase.LevelFailed: return "LEVEL_FAILED";
                case FlowPhase.NoCanoes: return "NO_CANOES";
                case FlowPhase.AdGate: return "AD_GATE";
                case FlowPhase.LevelCard: return "LEVEL_CARD"; // level_card, con la convención de esta tabla (02 §2 no lo nombra)
                default: return "UNKNOWN";
            }
        }
    }
}
