#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M7 Piraña: la cueva (02 §9.1 M7; propuesta de D-007 b.2 M7) [P]. Un arco de piedras grises sobre la boca, una piraña
    /// pequeña de perfil asomando por el hombro (sin dientes exagerados ni sangre) y, dentro del arco, una fila de <c>k</c>
    /// puntos con los vertidos útiles ya contados: los mismos campos que el contador del HUD (<c>LevelCounter.Of</c>). Con la
    /// cueva llena (el próximo mordisco se perdería, respuesta Q-C) todo se atenúa. En vuelo se oculta.
    /// </summary>
    public sealed class PiranaOverlay : CuiaOverlay
    {
        /// <summary>Opacidad con la cueva llena (<c>LevelCounter.Blocked</c>).</summary>
        public const float DimAlpha = 0.45f;

        private Image _stones = null!;
        private Image _fish = null!;
        private PipRow _pips = null!;
        private bool _on;
        private bool _inFlight;
        private bool _dimmed;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Pirana;

        /// <summary>Puntos del contador: el ritmo <c>k</c>.</summary>
        public int PipCount => _on ? _pips.Count : 0;

        /// <summary>Puntos llenos: vertidos útiles contados desde el último mordisco.</summary>
        public int FilledPips => _on ? _pips.Filled : 0;

        /// <summary>Atenuada: la cueva está llena.</summary>
        public bool Dimmed => _on && _dimmed;

        /// <summary>Opacidad actual del arco de piedras.</summary>
        public float StonesAlpha => _stones.color.a;

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            _on = cuia.Mechanic == MechanicType.Pirana;
            Visible = _on;
            if (_on)
            {
                _pips.Set(cuia.MechanicK, cuia.MechanicCounter);
                bool dim = cuia.IsFull;
                if (dim != _dimmed)
                {
                    _dimmed = dim;
                    float alpha = dim ? DimAlpha : 1f;
                    OverlayParts.SetColor(_stones, new Color(1f, 1f, 1f, alpha));
                    OverlayParts.SetColor(_fish, new Color(1f, 1f, 1f, alpha));
                    _pips.SetAlpha(alpha);
                }
            }

            Apply();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            OverlayParts.Place(_stones, 0f, heightU + 0.25f, 1.7f, 0.75f, u);
            OverlayParts.Place(_fish, 0.72f, heightU - 0.62f, 0.75f, 0.46f, u);
            _pips.Layout(0f, heightU + 0.15f, 0.14f, 0.21f, u);
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
            into.Add(ProceduralArt.StoneLight); // piedras
            into.Add(ProceduralArt.StoneDark);
            into.Add(ProceduralArt.FishSide); // piraña
            into.Add(ProceduralArt.FishBack);
            into.Add(ProceduralArt.FishBelly);
            into.Add(ProceduralArt.Pearl); // puntos del contador
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _stones = OverlayParts.NewImage("PiranaStones", Host.BodyFront, Host.Art.PiranaStones, Color.white);
            _fish = OverlayParts.NewImage("PiranaFish", Host.BodyFront, Host.Art.Piranha, Color.white);
            _pips = new PipRow("PiranaPip", Host.BodyFront, Host.Art.CounterPip, Host.Art.CounterPipEmpty, ProceduralArt.Pearl);
        }

        private void Apply()
        {
            bool shown = _on && !_inFlight;
            OverlayParts.SetVisible(_stones, shown);
            OverlayParts.SetVisible(_fish, shown);
            _pips.SetVisible(shown);
        }
    }
}
