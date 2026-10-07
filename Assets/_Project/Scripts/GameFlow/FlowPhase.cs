#nullable enable

namespace AF.GameFlow
{
    /// <summary>
    /// Estados de la pantalla de nivel (GDD 02 §2) que cubre el prototipo v0. FAIL_RESOLVE, SKIP_RESOLVE, AD_GATE y
    /// SUSPENDED quedan fuera (docs/dev/prototipo-v0.md: sin canoas, anuncios ni Salto del Boto).
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
                default: return "UNKNOWN";
            }
        }
    }
}
