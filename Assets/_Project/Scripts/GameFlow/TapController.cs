#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;

namespace AF.GameFlow
{
    /// <summary>
    /// Control tap-tap (GDD 02 §3.1, C1-C8). La selección es la del modelo (02 §3.2): los toques siempre se evalúan
    /// contra <see cref="LevelSession.Current"/>, el vertido lo decide el núcleo (<see cref="LevelSession.TryPour"/>) y
    /// quién puede ser origen también (<see cref="PourRules.CanBeSource"/>): aquí no se repite ninguna regla de vertido.
    /// </summary>
    /// <remarks>C9 (mantener pulsado) y C10 (arrastre) quedan fuera del prototipo v0 (docs/dev/prototipo-v0.md).</remarks>
    public sealed class TapController
    {
        /// <summary>Sin selección.</summary>
        public const int NoSelection = -1;

        private readonly bool _switchSelection;

        /// <summary>Crea el controlador.</summary>
        /// <param name="switchSelection">Valor de <c>tap_switch_selection</c> (08 §9; C5).</param>
        public TapController(bool switchSelection)
        {
            _switchSelection = switchSelection;
            Selected = NoSelection;
        }

        /// <summary>Cuia seleccionada como origen, o <see cref="NoSelection"/>.</summary>
        public int Selected { get; private set; }

        /// <summary>Procesa un toque sobre la cuia <paramref name="index"/> (C1-C6).</summary>
        /// <exception cref="ArgumentOutOfRangeException">El índice está fuera del tablero.</exception>
        public TapResult OnCuiaTap(LevelSession session, int index)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            BoardState state = session.Current;
            if ((uint)index >= (uint)state.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice de cuia fuera del tablero.");
            }

            int previous = Selected;
            if (session.Status != LevelStatus.Playing)
            {
                return TapResult.Ignored(index, previous);
            }

            if (previous == NoSelection)
            {
                if (PourRules.CanBeSource(state, index))
                {
                    Selected = index; // C1
                    return new TapResult(TapKind.Selected, index, previous, index, default);
                }

                return new TapResult(TapKind.Rejected, index, previous, NoSelection, default); // C2
            }

            if (previous == index)
            {
                Selected = NoSelection; // C3
                return new TapResult(TapKind.Deselected, index, previous, NoSelection, default);
            }

            if (session.TryPour(previous, index, out PourResult pour))
            {
                Selected = NoSelection; // C4: el origen no sigue seleccionado aunque le queden dosis del mismo color (en run, tras un vertido parcial; en single, de pruebas, lo habitual; P4 cerrada por D-005)
                return new TapResult(TapKind.Poured, index, previous, NoSelection, pour);
            }

            if (_switchSelection && PourRules.CanBeSource(state, index))
            {
                Selected = index; // C5 con tap_switch_selection = true
                return new TapResult(TapKind.Selected, index, previous, index, default);
            }

            Selected = NoSelection; // C5 con false y C6: el origen baja y la cuia tocada se sacude
            return new TapResult(TapKind.Rejected, index, previous, NoSelection, default);
        }

        /// <summary>C7: toque en el fondo o en zonas vacías del tablero. Devuelve el resultado (Ignored si no había selección).</summary>
        public TapResult OnBackgroundTap()
        {
            int previous = Selected;
            if (previous == NoSelection)
            {
                return TapResult.Ignored(-1, NoSelection);
            }

            Selected = NoSelection;
            return new TapResult(TapKind.Deselected, -1, previous, NoSelection, default);
        }

        /// <summary>C8 y cambios de nivel: quita la selección sin feedback.</summary>
        public void Clear()
        {
            Selected = NoSelection;
        }
    }
}
