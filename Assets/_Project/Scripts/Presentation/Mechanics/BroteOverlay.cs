#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M2 Brote (02 §9.1 M2; propuesta de D-007 b.2 M2) [P]: un tapón-brote encajado en la boca, del color de la cima
    /// (<c>Palette.Tint(TopColor)</c>; a propósito, es el color que lo hace germinar), con dos hojitas verdes plegadas que
    /// asoman por detrás. El contenido se ve (Q17 A). Si la cima estuviera oculta (Aguas turbias), el tapón va en el pardo de las turbias
    /// y nunca enseña el tinte (07 §4.3).
    /// </summary>
    /// <remarks>
    /// <para><b>Desbloqueo</b> (paso Open de la etapa (b), <see cref="OpenStepMs"/> × <c>ctx.SpeedFactor</c>; 02 §4.1: «brote
    /// germina en 2 hojas», 350 ms; b.2 no cambia esta duración): el tapón se hincha y se disuelve con unos destellos pequeños
    /// de su color (<see cref="IBoardFx.Sparkles"/>), y las dos hojitas germinan: se abren hacia fuera y crecen (hasta
    /// <see cref="OpenEnd"/>), se quedan abiertas en V y opacas (hasta <see cref="HoldEnd"/>) y en el último cuarto se
    /// desvanecen. El paso va en la cuia receptora y no espera a que el origen vuelva (contrato de T07): el brote abierto
    /// sigue a la vista cuando el origen aterriza.
    /// Mientras anima (<see cref="IsAnimating"/>), la vista no llama a <see cref="Show"/>; la cola cierra el paso con
    /// <see cref="SnapStep"/> y <c>ShowContent(ctx.After)</c>, que lo oculta. Marea lo vuelve a pintar cerrado.</para>
    /// <para>Sin asignaciones en <see cref="Tick"/>, <see cref="PlayStep"/> ni <see cref="Show"/> (08 §5.4); las partículas
    /// salen del pool común de <see cref="FxLayer"/> (tope de 44) y con Reducir movimiento no se emiten.</para>
    /// </remarks>
    public sealed class BroteOverlay : CuiaOverlay
    {
        /// <summary>Duración nominal de la germinación (ms) [P]: 02 §4.1 (Hoja, Brote y Liana se abren en 350 ms).</summary>
        public const float OpenStepMs = 350f;

        /// <summary>Pie del tapón bajo el labio (u): encaja en el cuello.</summary>
        public const float CorkBelowLipU = 0.2f;

        /// <summary>Giro hacia fuera de cada hojita plegada, que asoma por detrás del tapón (grados desde la vertical).</summary>
        public const float ClosedLeafDeg = 30f;

        /// <summary>Giro hacia fuera de cada hojita abierta (grados desde la vertical): el brote en V.</summary>
        public const float OpenLeafDeg = 48f;

        /// <summary>Destellos del tapón al disolverse.</summary>
        public const int SparkleCount = 4;

        /// <summary>Fin de la anticipación (el tapón se hincha; fracción del paso).</summary>
        public const float SwellEnd = 0.18f;

        /// <summary>Fin de la apertura de las hojitas y de la disolución del tapón (fracción del paso).</summary>
        public const float OpenEnd = 0.45f;

        /// <summary>
        /// Hasta aquí el brote abierto en V se queda opaco (fracción del paso); después se desvanece hasta el final. Así sigue a
        /// la vista cuando la cuia de origen, que vuelve en vuelo durante la germinación, aterriza en su ranura.
        /// </summary>
        public const float HoldEnd = 0.75f;

        /// <summary>Instante de los destellos (fracción del paso).</summary>
        public const float SparkleAt = 0.12f;

        // Margen tras la duración con el que el overlay cierra solo un paso que la cola no cerró (no debería pasar).
        private const float StuckGuardMs = 1000f;

        // Hojitas: lado (−1 izquierda, 1 derecha), pie (u desde el eje y bajo el labio).
        private const float LeafFootXU = 0.16f;
        private const float LeafFootBelowLipU = 0.04f;

        private Image _cork = null!;
        private Image _gloss = null!;
        private Image _leafL = null!;
        private Image _leafR = null!;
        private RectTransform _slot = null!;
        private IBoardFx? _fx;
        private Color _budColor = Color.white;
        private float _u = 24f;
        private float _heightU = 5.6f;
        private float _elapsed = -1f;
        private float _duration;
        private bool _sparkled;
        private bool _posed;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Brote;

        /// <summary>
        /// Color pintado del tapón (el RGB de su <see cref="Image"/>, con alfa 1): el de la cima, o el pardo de las turbias si
        /// la cima está oculta.
        /// </summary>
        public Color BudColor
        {
            get
            {
                Color c = _cork.color;
                c.a = 1f;
                return c;
            }
        }

        /// <inheritdoc />
        public override bool IsAnimating => _elapsed >= 0f;

        /// <summary>Fracción de la germinación en curso (0..1), o −1 sin paso.</summary>
        public float StepProgress => _elapsed < 0f ? -1f : Mathf.Clamp01(_elapsed / Mathf.Max(1f, _duration));

        /// <summary>El tapón está a la vista.</summary>
        public bool CorkVisible => _cork.gameObject.activeSelf && _cork.color.a > 0.001f;

        /// <summary>Opacidad del tapón.</summary>
        public float CorkAlpha => _cork.gameObject.activeSelf ? _cork.color.a : 0f;

        /// <summary>Las hojitas están a la vista.</summary>
        public bool LeavesVisible => _leafL.gameObject.activeSelf && _leafL.color.a > 0.001f;

        /// <summary>Opacidad de las hojitas (0 ocultas).</summary>
        public float LeafAlpha => _leafL.gameObject.activeSelf ? _leafL.color.a : 0f;

        /// <summary>Apertura de las hojitas (grados hacia fuera desde la vertical; <see cref="ClosedLeafDeg"/> plegadas).</summary>
        public float LeafOpenDeg => Mathf.DeltaAngle(0f, _leafL.rectTransform.localEulerAngles.z);

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Brote;
            Visible = on;
            if (on)
            {
                int top = cuia.Count - 1;
                Color bud = top < 0 ? Palette.Murky : (cuia.IsHiddenAt(top) ? Palette.Murky : Palette.Tint(cuia.TopColor));
                if (bud != _budColor)
                {
                    _budColor = bud;
                    Color c = bud;
                    c.a = _cork.color.a;
                    _cork.color = c;
                }
            }

            if (_posed)
            {
                PlaceClosed();
            }

            OverlayParts.SetVisible(_cork, on);
            OverlayParts.SetVisible(_leafL, on);
            OverlayParts.SetVisible(_leafR, on);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            if (_elapsed >= 0f)
            {
                Apply(StepProgress);
            }
            else
            {
                PlaceClosed();
            }
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Open ? OpenStepMs : 0f;
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            float ms = StepDurationMs(ctx.Kind) * ctx.SpeedFactor;
            if (ms <= 0f)
            {
                return 0f;
            }

            _fx = ctx.Fx ?? Host.Fx;
            _duration = ms;
            _elapsed = 0f;
            _sparkled = false;
            Visible = true;
            OverlayParts.SetVisible(_cork, true);
            OverlayParts.SetVisible(_leafL, true);
            OverlayParts.SetVisible(_leafR, true);
            Apply(0f);
            return ms;
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed += dtMs;
            if (_elapsed > _duration + StuckGuardMs)
            {
                SnapStep();
                ICuiaBoard? board = Host.Board;
                if (board != null)
                {
                    Show(board.ShownCuia(Host.Index), false);
                }

                return;
            }

            // Pasada la duración se queda en el último fotograma (todo transparente) hasta que la cola llame a SnapStep.
            Apply(Mathf.Clamp01(_elapsed / Mathf.Max(1f, _duration)));
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed = -1f;
            PlaceClosed();
            OverlayParts.SetVisible(_cork, false);
            OverlayParts.SetVisible(_leafL, false);
            OverlayParts.SetVisible(_leafR, false);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.BroteLeafLight); // hojitas (el tapón lleva el tinte de la cima a propósito)
            into.Add(ProceduralArt.BroteLeafDark);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _slot = (RectTransform)Host.SlotBack.parent;
            // Las hojitas, detrás del tapón: asoman a los lados y el color de la cima queda entero a la vista.
            _leafL = OverlayParts.NewImage("BroteLeafL", Host.BodyFront, Host.Art.BroteSprout, Color.white);
            _leafR = OverlayParts.NewImage("BroteLeafR", Host.BodyFront, Host.Art.BroteSprout, Color.white);
            _cork = OverlayParts.NewImage("BroteCork", Host.BodyFront, Host.Art.BroteCork, Color.white);
            _cork.rectTransform.pivot = new Vector2(0.5f, 0f);
            _gloss = OverlayParts.NewImage("BroteGloss", _cork.rectTransform, Host.Art.BroteGloss, new Color(1f, 1f, 1f, 0.6f));
            _gloss.gameObject.SetActive(true); // cuelga del tapón: se ve y se oculta con él
            _leafL.rectTransform.pivot = new Vector2(0.5f, 0f);
            _leafR.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        // Pose de reposo: el tapón encajado en la boca y las hojitas plegadas sobre él, opacos y sin escala.
        private void PlaceClosed()
        {
            _posed = false;
            float footY = _heightU - CorkBelowLipU;
            PoseCork(footY, 1f, 1f);
            PoseLeaf(_leafL, -1f, ClosedLeafDeg, 1f, 0f, 1f);
            PoseLeaf(_leafR, 1f, ClosedLeafDeg, 1f, 0f, 1f);
        }

        // Fotograma t (0..1) de la germinación; los destellos salen una sola vez al cruzar su instante.
        private void Apply(float t)
        {
            _posed = true;
            float footY = _heightU - CorkBelowLipU;
            if (!_sparkled && t >= SparkleAt)
            {
                _sparkled = true;
                _fx?.Sparkles(_slot.anchoredPosition + new Vector2(0f, (footY + (0.5f * ProceduralArt.BroteCorkHeightU)) * _u), ColorMath.Lighten(_budColor, 0.3f), SparkleCount, _u);
            }

            // Tapón: se hincha y después se encoge y se disuelve hacia arriba.
            if (t < SwellEnd)
            {
                float p = Ease.OutCubic(t / SwellEnd);
                PoseCork(footY, 1f + (0.12f * p), 1f);
            }
            else
            {
                float p = Mathf.Clamp01((t - SwellEnd) / (OpenEnd - SwellEnd));
                PoseCork(footY + (0.12f * p), Mathf.Lerp(1.12f, 0.55f, Ease.In(p)), 1f - Mathf.SmoothStep(0f, 1f, p));
            }

            // Hojitas: se abren hacia fuera (de 30° a 48° desde la vertical) y crecen, se quedan abiertas y opacas en V hasta
            // HoldEnd (apenas se mecen) y en el último cuarto se desvanecen.
            float open;
            float grow;
            float rise;
            float alpha;
            if (t < SwellEnd)
            {
                float p = t / SwellEnd;
                open = ClosedLeafDeg - (6f * Ease.OutCubic(p)); // se recogen un poco, como tomando impulso
                grow = 1f;
                rise = 0f;
                alpha = 1f;
            }
            else if (t < OpenEnd)
            {
                float p = Ease.OutCubic((t - SwellEnd) / (OpenEnd - SwellEnd));
                open = Mathf.Lerp(ClosedLeafDeg - 6f, OpenLeafDeg, p);
                grow = 1f + (0.3f * p);
                rise = 0.08f * p;
                alpha = 1f;
            }
            else if (t < HoldEnd)
            {
                float p = (t - OpenEnd) / (HoldEnd - OpenEnd);
                open = OpenLeafDeg + (3f * Mathf.Sin(p * Mathf.PI)); // un leve vaivén, abiertas
                grow = 1.3f + (0.04f * p);
                rise = 0.08f + (0.04f * p);
                alpha = 1f;
            }
            else
            {
                float p = (t - HoldEnd) / (1f - HoldEnd);
                open = OpenLeafDeg + (5f * p);
                grow = 1.34f + (0.06f * p);
                rise = 0.12f + (0.08f * p);
                alpha = 1f - Mathf.SmoothStep(0f, 1f, p);
            }

            PoseLeaf(_leafL, -1f, open, grow, rise, alpha);
            PoseLeaf(_leafR, 1f, open, grow, rise, alpha);
        }

        private void PoseCork(float footY, float scale, float alpha)
        {
            OverlayParts.Place(_cork, 0f, footY, ProceduralArt.BroteCorkWidthU, ProceduralArt.BroteCorkHeightU, _u);
            OverlayParts.Place(_gloss, -0.17f, 0.62f, 0.13f, 0.3f, _u); // dentro del tapón, arriba a la izquierda
            RectTransform rt = _cork.rectTransform;
            var s = new Vector3(scale, scale, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }

            Color c = _cork.color;
            if (!Mathf.Approximately(c.a, alpha))
            {
                c.a = alpha;
                _cork.color = c;
                _gloss.color = new Color(1f, 1f, 1f, 0.6f * alpha);
            }
        }

        // Hojita del lado side (−1 izquierda, 1 derecha): pie detrás del tapón, giro open (grados hacia fuera desde la
        // vertical), escala, subida (u) y opacidad. La derecha es la izquierda en espejo.
        private void PoseLeaf(Image leaf, float side, float open, float scale, float rise, float alpha)
        {
            float footX = side * (LeafFootXU + (0.06f * Mathf.Clamp01((open - ClosedLeafDeg) / (OpenLeafDeg - ClosedLeafDeg))));
            float footY = _heightU - LeafFootBelowLipU + rise;
            OverlayParts.Place(leaf, footX, footY, ProceduralArt.BroteSproutWidthU, ProceduralArt.BroteSproutHeightU, _u);
            RectTransform rt = leaf.rectTransform;
            Quaternion rotation = Quaternion.Euler(0f, 0f, -side * open); // la izquierda gira a la izquierda (z positiva) al abrirse
            if (rt.localRotation != rotation)
            {
                rt.localRotation = rotation;
            }

            var s = new Vector3(side * scale, scale, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }

            Color c = leaf.color;
            if (!Mathf.Approximately(c.a, alpha))
            {
                c.a = alpha;
                leaf.color = c;
            }
        }
    }
}
