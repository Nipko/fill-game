#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M4 Remolino (02 §9.1 M4; propuesta de D-007 b.2 M4) [P]: una espiral de espuma marfil translúcida sobre un remanso
    /// oscuro bajo el pie, en la ranura (no viaja con la cuia al verter). Nunca turquesa: ni el turquesa del icono de Marea
    /// (#3BB8D9) ni Agua del Tapajós. Si la pareja tiene id &gt; 0, <c>id + 1</c> puntos bajo el remanso distinguen las
    /// parejas. El canal entre las dos cuias del par llega con su arte, en la capa del tablero.
    /// </summary>
    public sealed class RemolinoOverlay : CuiaOverlay
    {
        /// <summary>Remanso oscuro #1A2E33 [P]: ΔE00 17,7 del Huito, lejos del turquesa.</summary>
        public static readonly Color PoolColor = Palette.Hex(0x1A2E33);

        /// <summary>Opacidad de la espuma marfil (<see cref="Palette.UiPaper"/>) sobre el remanso.</summary>
        public const float FoamAlpha = 0.5f;

        private Image _pool = null!;
        private Image _spiral = null!;
        private PipRow _dots = null!;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Remolino;

        /// <summary>Puntos de la pareja a la vista (0 con id 0).</summary>
        public int PairDots => _dots.Visible ? _dots.Count : 0;

        /// <summary>Color con el que se ve la espiral: la espuma compuesta en lineal sobre el remanso, como la GPU.</summary>
        public static Color SpiralSeenColor => ColorMath.CompositeLinear(Palette.UiPaper, FoamAlpha, PoolColor);

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Remolino;
            Visible = on;
            OverlayParts.SetVisible(_pool, on);
            OverlayParts.SetVisible(_spiral, on);
            int pair = on ? cuia.MechanicPairId : 0;
            int dots = pair > 0 ? pair + 1 : 0;
            _dots.Set(dots, dots);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            OverlayParts.Place(_pool, 0f, 0.02f, 2.4f, 0.7f, u);
            OverlayParts.Place(_spiral, 0f, 0.02f, 2.1f, 0.55f, u);
            _dots.Layout(0f, -0.5f, 0.13f, 0.2f, u);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(PoolColor); // remanso
            into.Add(SpiralSeenColor); // espiral de espuma
            into.Add(OverlayParts.Pearl); // puntos de la pareja
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _pool = OverlayParts.NewImage("RemolinoPool", Host.SlotBack, Host.Art.WhirlPool, PoolColor);
            _spiral = OverlayParts.NewImage("RemolinoSpiral", Host.SlotBack, Host.Art.WhirlSpiral, OverlayParts.Fade(Palette.UiPaper, FoamAlpha));
            _dots = new PipRow("RemolinoPair", Host.SlotFront, Host.Art.CounterPip, Host.Art.CounterPipEmpty, OverlayParts.Pearl);
        }
    }
}
