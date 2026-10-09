#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M1 Hoja cerrada (02 §9.1 M1; propuesta de D-007 b.2 M1) [P]: una hoja de palma alargada, plegada sobre la boca, con la
    /// fruta llave grande en el centro (<see cref="ProceduralArt.FruitFlight"/>). Su silueta es alargada para no confundirse
    /// con la hoja redonda de la Hoja escudo (02 §9.1; glosario §6.3). Es un estado, no un resaltado: no se ilumina al
    /// seleccionar (D-002). Se oculta en cuanto la Hoja se abre (la cuia pasa a no tener mecánica).
    /// </summary>
    public sealed class HojaOverlay : CuiaOverlay
    {
        /// <summary>Ancho de la hoja (u).</summary>
        public const float LeafWidthU = 2.3f;

        /// <summary>Alto de la hoja (u).</summary>
        public const float LeafHeightU = 1f;

        /// <summary>Lado de la fruta llave (u).</summary>
        public const float FruitU = 1f;

        private Image _leaf = null!;
        private Image _fruit = null!;
        private int _key;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Hoja;

        /// <summary>Fruta llave que muestra (0 si aún ninguna).</summary>
        public int ShownKey => _key;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Hoja;
            Visible = on;
            if (on)
            {
                int key = cuia.MechanicKey;
                if (key != _key)
                {
                    _key = key;
                    _fruit.sprite = key > 0 ? Host.Art.FruitFlight(key) : null;
                }
            }

            OverlayParts.SetVisible(_leaf, on);
            OverlayParts.SetVisible(_fruit, on && _key > 0);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            OverlayParts.Place(_leaf, 0f, heightU + 0.12f, LeafWidthU, LeafHeightU, u);
            OverlayParts.Place(_fruit, 0f, heightU + 0.30f, FruitU, FruitU, u);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(Palette.JungleMid); // cara de la hoja
            into.Add(Palette.LeafDark); // mitad plegada
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _leaf = OverlayParts.NewImage("HojaLeaf", Host.BodyFront, Host.Art.HojaLeaf, Color.white);
            _fruit = OverlayParts.NewImage("HojaFruit", Host.BodyFront, null, Color.white);
        }
    }
}
