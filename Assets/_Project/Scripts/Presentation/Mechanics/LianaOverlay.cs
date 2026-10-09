#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M3 Liana atada (02 §9.1 M3; propuesta de D-007 b.2 M3) [P]: 2-3 vueltas en diagonal de liana verde hoja oscuro
    /// (<see cref="Palette.LeafMid"/>, a ΔE00 16,3 del Lulo; nunca <c>LeafLight</c> ni <c>JungleLight</c>, a 7,8 y 6,4) que
    /// dejan ver el contenido (Q17 A), un nudo en el cuello y, con fruta (<c>key</c> &gt; 0), la fruta llave colgando del nudo;
    /// de cuenta (<c>n</c> &gt; 0), <c>n</c> puntos en el nudo. Se oculta en cuanto la Liana se suelta.
    /// </summary>
    public sealed class LianaOverlay : CuiaOverlay
    {
        /// <summary>Vueltas como mucho.</summary>
        public const int MaxWraps = 3;

        private readonly Image[] _wraps = new Image[MaxWraps];
        private Image _knot = null!;
        private Image _fruit = null!;
        private PipRow _dots = null!;
        private int _key;
        private int _n;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Liana;

        /// <summary>Vueltas de liana que se dibujan: 2 en las cuias de 1-2 dosis y 3 en las demás.</summary>
        public int WrapCount => Host.Capacity <= 2 ? 2 : MaxWraps;

        /// <summary>Fruta llave que cuelga del nudo (0 en la Liana de cuenta).</summary>
        public int ShownKey => _key;

        /// <summary>Puntos del nudo a la vista (Liana de cuenta: <c>n</c>).</summary>
        public int CountDots => _dots.Visible ? _dots.Count : 0;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Liana;
            Visible = on;
            if (on)
            {
                int key = cuia.MechanicKey;
                if (key != _key)
                {
                    _key = key;
                    _fruit.sprite = key > 0 ? Host.Art.FruitFlight(key) : null;
                }

                _n = cuia.MechanicN;
            }

            int wraps = WrapCount;
            for (int i = 0; i < MaxWraps; i++)
            {
                OverlayParts.SetVisible(_wraps[i], on && i < wraps);
            }

            OverlayParts.SetVisible(_knot, on);
            OverlayParts.SetVisible(_fruit, on && _key > 0);
            int dots = on && _key == 0 ? _n : 0;
            _dots.Set(dots, dots);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            int cap = Host.Capacity;
            float cavityTop = CuiaGeometry.CavityBottomU + cap;
            int wraps = WrapCount;
            float band = cap / (float)wraps;
            for (int i = 0; i < MaxWraps; i++)
            {
                float y = CuiaGeometry.CavityBottomU + (band * (i + 0.5f));
                OverlayParts.Place(_wraps[i], 0f, y, 2.1f, Mathf.Min(1f, band + 0.2f), u);
            }

            float knotY = cavityTop + 0.42f;
            OverlayParts.Place(_knot, 0.38f, knotY, 0.5f, 0.42f, u);
            OverlayParts.Place(_fruit, 0.62f, cavityTop + 0.02f, 0.72f, 0.72f, u);
            _dots.Layout(0.38f, knotY, 0.13f, 0.18f, u);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(Palette.LeafMid); // tallo de la liana
            into.Add(Palette.LeafDark); // hojitas y nudo
            into.Add(OverlayParts.Pearl); // puntos de la Liana de cuenta
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            for (int i = 0; i < MaxWraps; i++)
            {
                _wraps[i] = OverlayParts.NewImage("LianaWrap" + i, Host.BodyFront, Host.Art.LianaWrap, Color.white);
                if ((i % 2) == 1)
                {
                    _wraps[i].rectTransform.localScale = new Vector3(-1f, 1f, 1f); // vueltas alternas, en zigzag
                }
            }

            _knot = OverlayParts.NewImage("LianaKnot", Host.BodyFront, Host.Art.LianaKnot, Color.white);
            _fruit = OverlayParts.NewImage("LianaFruit", Host.BodyFront, null, Color.white);
            _dots = new PipRow("LianaDot", Host.BodyFront, Host.Art.CounterPip, Host.Art.CounterPipEmpty, OverlayParts.Pearl);
        }
    }
}
