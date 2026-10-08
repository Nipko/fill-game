#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Estados de la pantalla de nivel (GDD 02 §2) con el embudo de D-008 (tareas B2-2 y B2-3): GIVE_UP_CONFIRM,
    /// LEVEL_FAILED y NO_CANOES. FAIL_RESOLVE es instantáneo (<see cref="FailResolve"/>); SKIP_RESOLVE (Salto del Boto),
    /// AD_GATE (B2-4) y SUSPENDED quedan fuera.
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

        /// <summary>VICTORY_SCREEN: pantalla de victoria con Siguiente; sin auto-avance.</summary>
        VictoryScreen = 4,

        /// <summary>STUCK_MODAL: modal Aguas estancadas (02 §7).</summary>
        StuckModal = 5,

        /// <summary>GIVE_UP_CONFIRM: ¿Seguro? [P] tras «Rendirse», desde el 31 y con canoa en juego (D-008, paso 2).</summary>
        GiveUpConfirm = 6,

        /// <summary>LEVEL_FAILED: Nivel fallido [P] tras el fallo duro desde el 31 (D-008, paso 3).</summary>
        LevelFailed = 7,

        /// <summary>NO_CANOES: Sin canoas (04 §7.3) al reintentar o reiniciar con 0 canoas desde el 31 (D-008, paso 4).</summary>
        NoCanoes = 8,
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
                default: return "UNKNOWN";
            }
        }
    }
}
