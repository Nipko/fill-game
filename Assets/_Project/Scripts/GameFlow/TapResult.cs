#nullable enable
using AF.Core.Session;

namespace AF.GameFlow
{
    /// <summary>Qué produjo un toque sobre una cuia (GDD 02 §3.1).</summary>
    public enum TapKind
    {
        /// <summary>El toque no se procesa (input bloqueado: intro, bloqueo, victoria…).</summary>
        Ignored = 0,

        /// <summary>C1 o C5: la cuia tocada queda seleccionada (y se eleva).</summary>
        Selected = 1,

        /// <summary>C3 o C7: se quita la selección.</summary>
        Deselected = 2,

        /// <summary>C4: vertido legal ejecutado en el modelo.</summary>
        Poured = 3,

        /// <summary>C2, C6 o C5 con <c>tap_switch_selection</c> = false: rechazo suave (sacudida de la cuia tocada).</summary>
        Rejected = 4,
    }

    /// <summary>Resultado de un toque procesado por <see cref="TapController"/>.</summary>
    public readonly struct TapResult
    {
        /// <summary>Crea un resultado.</summary>
        public TapResult(TapKind kind, int tapped, int previousSelection, int selection, PourResult pour)
        {
            Kind = kind;
            Tapped = tapped;
            PreviousSelection = previousSelection;
            Selection = selection;
            Pour = pour;
        }

        /// <summary>Qué pasó.</summary>
        public TapKind Kind { get; }

        /// <summary>Cuia tocada (−1 si fue el fondo).</summary>
        public int Tapped { get; }

        /// <summary>Selección antes del toque (−1 = ninguna).</summary>
        public int PreviousSelection { get; }

        /// <summary>Selección después del toque (−1 = ninguna).</summary>
        public int Selection { get; }

        /// <summary>Vertido ejecutado; solo válido si <see cref="Kind"/> es <see cref="TapKind.Poured"/>.</summary>
        public PourResult Pour { get; }

        /// <summary>Resultado de un toque ignorado.</summary>
        public static TapResult Ignored(int tapped, int selection)
        {
            return new TapResult(TapKind.Ignored, tapped, selection, selection, default);
        }
    }
}
