#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M2 Brote cerrado (02 §9.1 M2; propuesta de D-007 b.2 M2) [P]: un capullo de dos hojitas en la boca, del color de la cima
    /// (<c>Palette.Tint(TopColor)</c>; a propósito, es el color que lo hace germinar), sobre un cáliz verde. El contenido se
    /// ve (Q17 A). Se oculta en cuanto germina (la cuia pasa a no tener mecánica).
    /// </summary>
    public sealed class BroteOverlay : CuiaOverlay
    {
        private Image _bud = null!;
        private Image _calyx = null!;
        private int _tint;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Brote;

        /// <summary>Color del capullo (el de la cima).</summary>
        public Color BudColor => _bud.color;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Brote;
            Visible = on;
            if (on)
            {
                int tint = cuia.TopColor;
                if (tint != _tint)
                {
                    _tint = tint;
                    OverlayParts.SetColor(_bud, Palette.Tint(tint));
                }
            }

            OverlayParts.SetVisible(_bud, on);
            OverlayParts.SetVisible(_calyx, on);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            OverlayParts.Place(_calyx, 0f, heightU - 0.02f, 0.8f, 0.3f, u);
            OverlayParts.Place(_bud, 0f, heightU + 0.28f, 0.95f, 0.86f, u);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(Palette.LeafDark); // cáliz (el capullo lleva el tinte a propósito)
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _calyx = OverlayParts.NewImage("BroteCalyx", Host.BodyFront, Host.Art.BroteCalyx, Color.white);
            _bud = OverlayParts.NewImage("BroteBud", Host.BodyFront, Host.Art.BroteBud, Color.white);
        }
    }
}
