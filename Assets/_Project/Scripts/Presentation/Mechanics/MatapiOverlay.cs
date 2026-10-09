#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M10 Matapi (D-007; propuesta b.2 M10) [P]: un cilindro de 7 varillas de fibra semitransparentes (tonos
    /// <c>fibra_*</c>) sobre el cuerpo, con dos aros y un embudo hacia dentro en la boca: la forma ya dice «entra». El
    /// contenido se ve entre las varillas. No tiene estado: nunca se abre.
    /// </summary>
    public sealed class MatapiOverlay : CuiaOverlay
    {
        /// <summary>Varillas del cilindro.</summary>
        public const int RodCount = 7;

        /// <summary>Opacidad de las varillas (dejan ver el contenido).</summary>
        public const float RodAlpha = 0.6f;

        private readonly Image[] _rods = new Image[RodCount];
        private readonly Image[] _hoops = new Image[2];
        private Image _funnel = null!;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Matapi;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Matapi;
            Visible = on;
            for (int i = 0; i < RodCount; i++)
            {
                OverlayParts.SetVisible(_rods[i], on);
            }

            OverlayParts.SetVisible(_hoops[0], on);
            OverlayParts.SetVisible(_hoops[1], on);
            OverlayParts.SetVisible(_funnel, on);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            float bottom = CuiaGeometry.FootTopU;
            float top = CuiaGeometry.CavityBottomU + Host.Capacity + 0.1f;
            for (int i = 0; i < RodCount; i++)
            {
                float x = -0.84f + (i * (1.68f / (RodCount - 1)));
                OverlayParts.Place(_rods[i], x, (top + bottom) * 0.5f, 0.09f, top - bottom, u);
            }

            OverlayParts.Place(_hoops[0], 0f, 0.42f, 2.1f, 0.35f, u);
            OverlayParts.Place(_hoops[1], 0f, top - 0.08f, 2.1f, 0.35f, u);
            OverlayParts.Place(_funnel, 0f, heightU - 0.2f, 1.4f, 0.65f, u);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            // Varillas, aros y embudo son de fibra (fibra_*): excepción de la regla 1 de direccion-visual-v2 §2.4.
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            for (int i = 0; i < RodCount; i++)
            {
                Color fiber = (i % 3) == 0 ? Palette.FiberLight : ((i % 3) == 1 ? Palette.FiberBase : Palette.FiberShadow);
                _rods[i] = OverlayParts.NewImage("MatapiRod" + i, Host.BodyFront, Host.Art.MatapiRod, OverlayParts.Fade(fiber, RodAlpha));
            }

            for (int i = 0; i < 2; i++)
            {
                _hoops[i] = OverlayParts.NewImage("MatapiHoop" + i, Host.BodyFront, Host.Art.MatapiHoop, OverlayParts.Fade(Palette.FiberBase, 0.9f));
            }

            _funnel = OverlayParts.NewImage("MatapiFunnel", Host.BodyFront, Host.Art.MatapiFunnel, Color.white);
        }
    }
}
