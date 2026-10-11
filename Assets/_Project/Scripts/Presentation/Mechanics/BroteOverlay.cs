#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M2 Brote (02 §9.1 M2; propuesta de D-007 b.2 M2) [P]: un capullo del color de la cima (<c>Palette.Tint(TopColor)</c>;
    /// a propósito, es el color que lo hace germinar) con contorno oscuro, sujeto por un cáliz verde que tapa la boca entera, y
    /// dos hojitas verdes plegadas que asoman por detrás a los lados. El contenido se ve (Q17 A). Si la cima estuviera oculta
    /// (Aguas turbias), el capullo va en el pardo de las turbias y nunca enseña el tinte (07 §4.3).
    /// </summary>
    /// <remarks>
    /// <para><b>Tamaño</b> (prueba del dueño del 2026-10-09; tanda B2): el cáliz mide <see cref="ProceduralArt.BroteSepalsWidthU"/>
    /// = 1,32 u (la boca, el borde de fibra, 1,10 u) y baja hasta el labio de vertido; el capullo llega a
    /// <see cref="TopAboveRimU"/> = 0,90 u sobre lo alto de la cuia, 1,15 u sobre el labio (<c>CuiaGeometry.LipU</c>). Más no
    /// cabe: entre lo alto de una cuia y la madera de la repisa de la fila de arriba quedan 16 + 24 − 17 = 23 dp, y hasta el
    /// marco del panel en la fila de arriba, unos 24 dp (<c>BoardLayout</c>); con la cuia más ancha de los tableros de dos o
    /// más filas (48 dp, u = 24), 0,90 u son 21,6 dp. En todo el paso Open nada pasa de ahí: el capullo se hincha a lo ancho y
    /// después encoge más deprisa de lo que sube, y las hojitas abiertas no pasan de <see cref="TopAboveRimU"/>.</para>
    /// <para><b>Desbloqueo</b> (paso Open de la etapa (b), <see cref="OpenStepMs"/> × <c>ctx.SpeedFactor</c>; 02 §4.1: «brote
    /// germina en 2 hojas», 350 ms; b.2 no cambia esta duración): el capullo se hincha y se disuelve con unos destellos pequeños
    /// de su color (<see cref="IBoardFx.Sparkles"/>) mientras el cáliz se abre y se desvanece, y las dos hojitas germinan: se
    /// abren hacia fuera y crecen (hasta <see cref="OpenEnd"/>), se quedan abiertas en V y opacas (hasta
    /// <see cref="HoldEnd"/>) y en el último cuarto se desvanecen. El paso va en la cuia receptora y no espera a que el origen
    /// vuelva (contrato de T07): el brote abierto sigue a la vista cuando el origen aterriza.
    /// Mientras anima (<see cref="IsAnimating"/>), la vista no llama a <see cref="Show"/>; la cola cierra el paso con
    /// <see cref="SnapStep"/> y <c>ShowContent(ctx.After)</c>, que lo oculta. Marea lo vuelve a pintar cerrado.</para>
    /// <para>Sin asignaciones en <see cref="Tick"/>, <see cref="PlayStep"/> ni <see cref="Show"/> (08 §5.4); las partículas
    /// salen del pool común de <see cref="FxLayer"/> (tope de 44) y con Reducir movimiento no se emiten.</para>
    /// </remarks>
    public sealed class BroteOverlay : CuiaOverlay
    {
        /// <summary>Duración nominal de la germinación (ms) [P]: 02 §4.1 (Hoja, Brote y Liana se abren en 350 ms).</summary>
        public const float OpenStepMs = 350f;

        /// <summary>Pie del capullo bajo lo alto de la cuia (u): encaja en el cuello, por dentro del borde de fibra.</summary>
        public const float CorkBelowLipU = 0.22f;

        /// <summary>Lo más alto del brote sobre lo alto de la cuia (u), en reposo y en todo el paso: la punta del capullo.</summary>
        public const float TopAboveRimU = ProceduralArt.BroteCorkHeightU - CorkBelowLipU;

        /// <summary>Pie del cáliz bajo lo alto de la cuia (u): cubre el borde de fibra entero (0,25 u) hasta el labio.</summary>
        public const float SepalsBelowLipU = 0.28f;

        /// <summary>Giro hacia fuera de cada hojita plegada, que asoma por detrás del capullo (grados desde la vertical).</summary>
        public const float ClosedLeafDeg = 34f;

        /// <summary>Giro hacia fuera de cada hojita abierta (grados desde la vertical): el brote en V.</summary>
        public const float OpenLeafDeg = 50f;

        /// <summary>Escala de las hojitas abiertas (crecen al germinar, sin pasar de <see cref="TopAboveRimU"/>).</summary>
        public const float OpenLeafScale = 1.3f;

        /// <summary>Destellos del capullo al disolverse.</summary>
        public const int SparkleCount = 4;

        /// <summary>Fin de la anticipación (el capullo se hincha; fracción del paso).</summary>
        public const float SwellEnd = 0.18f;

        /// <summary>Fin de la apertura de las hojitas y de la disolución del capullo (fracción del paso).</summary>
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

        // Hojitas: pie (u desde el eje y sobre lo alto de la cuia) y lo que sube al abrirse, como mucho.
        private const float LeafFootXU = 0.18f;
        private const float LeafFootAboveLipU = 0f;
        private const float LeafRiseU = 0.1f;

        private Image _cork = null!;
        private Image _gloss = null!;
        private Image _sepals = null!;
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
        /// Color pintado del capullo (el RGB de su <see cref="Image"/>, con alfa 1): el de la cima, o el pardo de las turbias si
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

        /// <summary>El capullo está a la vista.</summary>
        public bool CorkVisible => _cork.gameObject.activeSelf && _cork.color.a > 0.001f;

        /// <summary>Opacidad del capullo.</summary>
        public float CorkAlpha => _cork.gameObject.activeSelf ? _cork.color.a : 0f;

        /// <summary>El cáliz verde que tapa la boca está a la vista.</summary>
        public bool SepalsVisible => _sepals.gameObject.activeSelf && _sepals.color.a > 0.001f;

        /// <summary>Las hojitas están a la vista.</summary>
        public bool LeavesVisible => _leafL.gameObject.activeSelf && _leafL.color.a > 0.001f;

        /// <summary>Opacidad de las hojitas (0 ocultas).</summary>
        public float LeafAlpha => _leafL.gameObject.activeSelf ? _leafL.color.a : 0f;

        /// <summary>Apertura de las hojitas (grados hacia fuera desde la vertical; <see cref="ClosedLeafDeg"/> plegadas).</summary>
        public float LeafOpenDeg => Mathf.DeltaAngle(0f, _leafL.rectTransform.localEulerAngles.z);

        /// <summary>Rectángulo del capullo (u desde el centro del pie de la cuia, hacia arriba), con su escala.</summary>
        public Rect CorkRectU => RectU(_cork.rectTransform);

        /// <summary>Rectángulo del cáliz (u desde el centro del pie de la cuia), con su escala: el ancho que tapa la boca.</summary>
        public Rect SepalsRectU => RectU(_sepals.rectTransform);

        /// <summary>
        /// Lo más alto de lo que se ve del brote (u desde el pie): la punta del capullo, lo alto del cáliz y la punta de cada
        /// hojita (girada, con su escala), solo de las piezas a la vista.
        /// </summary>
        public float VisibleTopU
        {
            get
            {
                float top = float.NegativeInfinity;
                if (CorkVisible)
                {
                    top = Mathf.Max(top, CorkRectU.yMax);
                }

                if (SepalsVisible)
                {
                    top = Mathf.Max(top, SepalsRectU.yMax);
                }

                if (LeavesVisible)
                {
                    top = Mathf.Max(top, Mathf.Max(LeafTipU(_leafL).y, LeafTipU(_leafR).y));
                }

                return top;
            }
        }

        /// <summary>Punta de la hojita izquierda (−1) o derecha (1), en u desde el pie (tests: el verde asoma a los lados).</summary>
        public Vector2 LeafTip(int side)
        {
            return LeafTipU(side < 0 ? _leafL : _leafR);
        }

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
            OverlayParts.SetVisible(_sepals, on);
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
            OverlayParts.SetVisible(_sepals, true);
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
            OverlayParts.SetVisible(_sepals, false);
            OverlayParts.SetVisible(_leafL, false);
            OverlayParts.SetVisible(_leafR, false);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.BroteLeafLight); // cáliz y hojitas (el capullo lleva el tinte de la cima a propósito)
            into.Add(ProceduralArt.BroteLeafDark);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _slot = (RectTransform)Host.SlotBack.parent;
            // Las hojitas, detrás del capullo; delante, el cáliz que tapa la boca y el pie del capullo.
            _leafL = OverlayParts.NewImage("BroteLeafL", Host.BodyFront, Host.Art.BroteSprout, Color.white);
            _leafR = OverlayParts.NewImage("BroteLeafR", Host.BodyFront, Host.Art.BroteSprout, Color.white);
            _cork = OverlayParts.NewImage("BroteCork", Host.BodyFront, Host.Art.BroteCork, Color.white);
            _cork.rectTransform.pivot = new Vector2(0.5f, 0f);
            _gloss = OverlayParts.NewImage("BroteGloss", _cork.rectTransform, Host.Art.BroteGloss, new Color(1f, 1f, 1f, 0.6f));
            _gloss.gameObject.SetActive(true); // cuelga del capullo: se ve y se oculta con él
            _sepals = OverlayParts.NewImage("BroteSepals", Host.BodyFront, Host.Art.BroteSepals, Color.white);
            _sepals.rectTransform.pivot = new Vector2(0.5f, 0f);
            _leafL.rectTransform.pivot = new Vector2(0.5f, 0f);
            _leafR.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        // Pose de reposo: el capullo en la boca sobre su cáliz y las hojitas plegadas, opacos y sin escala.
        private void PlaceClosed()
        {
            _posed = false;
            PoseCork(_heightU - CorkBelowLipU, 1f, 1f, 1f);
            PoseSepals(1f, 1f, 1f);
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
                _fx?.Sparkles(_slot.anchoredPosition + new Vector2(0f, (footY + (0.55f * ProceduralArt.BroteCorkHeightU)) * _u), ColorMath.Lighten(_budColor, 0.3f), SparkleCount, _u);
            }

            // Capullo: se hincha a lo ancho (lo alto no se mueve) y después encoge más deprisa de lo que sube y se disuelve;
            // el cáliz se abre y se desvanece con él.
            if (t < SwellEnd)
            {
                float p = Ease.OutCubic(t / SwellEnd);
                PoseCork(footY, 1f + (0.12f * p), 1f, 1f);
                PoseSepals(1f + (0.06f * p), 1f, 1f);
            }
            else
            {
                float p = Mathf.Clamp01((t - SwellEnd) / (OpenEnd - SwellEnd));
                float fade = 1f - Mathf.SmoothStep(0f, 1f, p);
                float shrink = Mathf.Lerp(1f, 0.55f, Ease.In(p));
                PoseCork(footY + (0.08f * p), Mathf.Lerp(1.12f, 0.55f, Ease.In(p)), shrink, fade);
                PoseSepals(1.06f + (0.2f * Ease.Out(p)), 1f - (0.3f * Ease.Out(p)), fade);
            }

            // Hojitas: se abren hacia fuera (de 34° a 50° desde la vertical) y crecen, se quedan abiertas y opacas en V hasta
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
                grow = Mathf.Lerp(1f, OpenLeafScale, p);
                rise = 0.6f * LeafRiseU * p;
                alpha = 1f;
            }
            else if (t < HoldEnd)
            {
                float p = (t - OpenEnd) / (HoldEnd - OpenEnd);
                open = OpenLeafDeg + (3f * Mathf.Sin(p * Mathf.PI)); // un leve vaivén, abiertas
                grow = OpenLeafScale;
                rise = LeafRiseU * (0.6f + (0.4f * p));
                alpha = 1f;
            }
            else
            {
                float p = (t - HoldEnd) / (1f - HoldEnd);
                open = OpenLeafDeg + (5f * p);
                grow = OpenLeafScale;
                rise = LeafRiseU;
                alpha = 1f - Mathf.SmoothStep(0f, 1f, p);
            }

            PoseLeaf(_leafL, -1f, open, grow, rise, alpha);
            PoseLeaf(_leafR, 1f, open, grow, rise, alpha);
        }

        // Capullo con pie en footY (u), escala (sx, sy) desde el pie y opacidad; su brillo va con él.
        private void PoseCork(float footY, float sx, float sy, float alpha)
        {
            OverlayParts.Place(_cork, 0f, footY, ProceduralArt.BroteCorkWidthU, ProceduralArt.BroteCorkHeightU, _u);
            OverlayParts.Place(_gloss, -0.2f, 0.66f, 0.14f, 0.32f, _u); // dentro del capullo, arriba a la izquierda
            SetScale(_cork.rectTransform, sx, sy);
            Color c = _cork.color;
            if (!Mathf.Approximately(c.a, alpha))
            {
                c.a = alpha;
                _cork.color = c;
                _gloss.color = new Color(1f, 1f, 1f, 0.6f * alpha);
            }
        }

        // Cáliz con escala (sx, sy) desde su pie y opacidad.
        private void PoseSepals(float sx, float sy, float alpha)
        {
            OverlayParts.Place(_sepals, 0f, _heightU - SepalsBelowLipU, ProceduralArt.BroteSepalsWidthU, ProceduralArt.BroteSepalsHeightU, _u);
            SetScale(_sepals.rectTransform, sx, sy);
            OverlayParts.SetColor(_sepals, new Color(1f, 1f, 1f, alpha));
        }

        // Hojita del lado side (−1 izquierda, 1 derecha): pie detrás del capullo, giro open (grados hacia fuera desde la
        // vertical), escala, subida (u) y opacidad. La derecha es la izquierda en espejo.
        private void PoseLeaf(Image leaf, float side, float open, float scale, float rise, float alpha)
        {
            float footX = side * (LeafFootXU + (0.06f * Mathf.Clamp01((open - ClosedLeafDeg) / (OpenLeafDeg - ClosedLeafDeg))));
            float footY = _heightU + LeafFootAboveLipU + rise;
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

        // Rectángulo (u desde el pie) de una pieza sin girar con su pivote y su escala.
        private Rect RectU(RectTransform rt)
        {
            Vector2 size = rt.sizeDelta;
            Vector3 scale = rt.localScale;
            float w = size.x * Mathf.Abs(scale.x);
            float h = size.y * Mathf.Abs(scale.y);
            Vector2 pos = rt.anchoredPosition;
            Vector2 pivot = rt.pivot;
            var r = new Rect(pos.x - (pivot.x * w), pos.y - (pivot.y * h), w, h);
            float u = Mathf.Max(0.01f, _u);
            return new Rect(r.x / u, r.y / u, r.width / u, r.height / u);
        }

        // Punta de una hojita (u desde el pie): su pie más el alto del sprite, con la escala y el giro.
        private Vector2 LeafTipU(Image leaf)
        {
            RectTransform rt = leaf.rectTransform;
            float len = rt.sizeDelta.y * Mathf.Abs(rt.localScale.y);
            Vector2 tip = rt.anchoredPosition + (Vector2)(rt.localRotation * new Vector3(0f, len, 0f));
            float u = Mathf.Max(0.01f, _u);
            return tip / u;
        }

        private static void SetScale(RectTransform rt, float sx, float sy)
        {
            var s = new Vector3(sx, sy, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }
        }
    }
}
