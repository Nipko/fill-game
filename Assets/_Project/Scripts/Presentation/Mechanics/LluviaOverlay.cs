#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M6 Lluvia (02 §9.1 M6; propuesta de D-007 b.2 M6) [P]: una nube gris perla sobre la boca, dentro de los 32 dp táctiles
    /// sobre la cuia (02 §3.3), con una fila de <c>k</c> puntos (llenos los vertidos útiles ya contados) y una gotita del agua
    /// del mundo por cada gota pendiente. Son los mismos campos que el contador del HUD (<c>LevelCounter.Of</c>: ritmo,
    /// transcurridos y gotas pendientes). Se va con la última gota. En vuelo se oculta (la nube no se inclina con la cuia).
    /// </summary>
    /// <remarks>
    /// La nube queda por debajo de los 32 dp del área táctil, así que nunca tapa la fila de arriba (entre filas hay
    /// elevación + 24 dp, 02 §5).
    /// </remarks>
    public sealed class LluviaOverlay : CuiaOverlay
    {
        /// <summary>Alto máximo de la nube (u).</summary>
        public const float CloudMaxU = 0.85f;

        /// <summary>Hueco entre la boca y la panza de la nube (u).</summary>
        public const float CloudGapU = 0.15f;

        private readonly Image[] _drops = new Image[PipRow.MaxPips];
        private Image _cloud = null!;
        private PipRow _pips = null!;
        private bool _on;
        private bool _inFlight;
        private int _dropCount;
        private int _key;
        private float _u = 24f;
        private float _dropY;
        private float _dropSize = 0.15f;
        private float _dropStep = 0.24f;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Lluvia;

        /// <summary>Puntos del contador: el ritmo <c>k</c>.</summary>
        public int PipCount => _on ? _pips.Count : 0;

        /// <summary>Puntos llenos: vertidos útiles contados desde la última gota.</summary>
        public int FilledPips => _on ? _pips.Filled : 0;

        /// <summary>Gotitas: gotas pendientes.</summary>
        public int DropCount => _on ? _dropCount : 0;

        /// <summary>Lo alto de la nube sobre la boca (dp; ≤ 32, dentro del área táctil).</summary>
        public float CloudTopAboveMouthDp
        {
            get
            {
                RectTransform rt = _cloud.rectTransform;
                return rt.anchoredPosition.y + (0.5f * rt.sizeDelta.y) - (Host.HeightU * _u);
            }
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            _on = cuia.Mechanic == MechanicType.Lluvia;
            Visible = _on;
            if (_on)
            {
                _pips.Set(cuia.MechanicK, cuia.MechanicCounter);
                int key = cuia.MechanicKey;
                if (key != _key)
                {
                    _key = key;
                    Color water = Palette.Tint(key); // el agua del mundo, a propósito
                    for (int i = 0; i < _drops.Length; i++)
                    {
                        OverlayParts.SetColor(_drops[i], water);
                    }
                }

                int drops = Mathf.Clamp(cuia.MechanicN, 0, _drops.Length);
                if (drops != _dropCount)
                {
                    _dropCount = drops;
                    PlaceDrops();
                }
            }

            Apply();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            float cloudH = Mathf.Min(CloudMaxU, ((CuiaView.TouchHeadroomDp - 1f) / u) - CloudGapU);
            float scale = cloudH / CloudMaxU;
            float cloudY = heightU + CloudGapU + (0.5f * cloudH);
            OverlayParts.Place(_cloud, 0f, cloudY, 2.35f * cloudH, cloudH, u);
            _pips.Layout(0f, cloudY + (0.04f * scale), 0.17f * scale, 0.27f * scale, u);
            _dropSize = 0.15f * scale;
            _dropStep = 0.24f * scale;
            _dropY = heightU + CloudGapU - (0.02f * scale);
            PlaceDrops();
        }

        /// <inheritdoc />
        public override void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            Apply();
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(OverlayParts.Pearl); // nube
            into.Add(OverlayParts.Slate); // puntos del contador (las gotitas llevan el agua del mundo a propósito)
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _cloud = OverlayParts.NewImage("LluviaCloud", Host.BodyFront, Host.Art.RainCloud, OverlayParts.Pearl);
            _pips = new PipRow("LluviaPip", Host.BodyFront, Host.Art.CounterPip, Host.Art.CounterPipEmpty, OverlayParts.Slate);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = OverlayParts.NewImage("LluviaDrop" + i, Host.BodyFront, Host.Art.DropIcon, Color.white);
            }
        }

        private void PlaceDrops()
        {
            float start = -0.5f * _dropStep * (_dropCount - 1);
            for (int i = 0; i < _drops.Length; i++)
            {
                OverlayParts.Place(_drops[i], start + (i * _dropStep), _dropY, _dropSize, _dropSize * 1.25f, _u);
            }
        }

        private void Apply()
        {
            bool shown = _on && !_inFlight;
            OverlayParts.SetVisible(_cloud, shown);
            _pips.SetVisible(shown);
            for (int i = 0; i < _drops.Length; i++)
            {
                OverlayParts.SetVisible(_drops[i], shown && i < _dropCount);
            }
        }
    }
}
