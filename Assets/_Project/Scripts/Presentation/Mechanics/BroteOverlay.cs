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
    /// <para><b>Tamaño</b> (prueba del dueño del 2026-10-09: «sobresalir ≥ 1 u», medido como la auditoría de la tanda B2,
    /// desde lo alto de la cuia, <c>HeightU</c>): el cáliz mide <see cref="ProceduralArt.BroteSepalsWidthU"/> = 1,32 u (la
    /// boca, el borde de fibra, 1,10 u) y baja hasta el labio de vertido; el capullo llega a <see cref="TopAboveRimU"/> = 1,0 u
    /// sobre lo alto de la cuia (1,25 u sobre el labio, <c>CuiaGeometry.LipU</c>). No tapa la fila de arriba, con el mismo
    /// límite que la nube de la Lluvia (<see cref="LluviaOverlay.RiseTopMaxDp"/>): el pie de la fila de arriba queda 16 + 24 =
    /// 40 dp sobre lo alto de la cuia (<c>BoardLayout</c>) y, con la cuia más ancha de los tableros de dos o más filas (48 dp,
    /// u = 24), 1,0 u son 24 dp. Ese peor caso roza 1 dp la madera de la repisa de la fila de arriba, que acaba 17 dp bajo su
    /// pie (16 + 24 − 17 = 23 dp) [P, pendiente del dueño: 1,0 u rozando la repisa o 0,9 u sin rozarla]. En la fila de arriba
    /// no pasa del canto interior del marco del panel (<see cref="LluviaOverlay.CloudTopMaxDp"/>, 24 dp). En todo el paso Open
    /// nada sube más: el capullo se hincha a lo ancho y después encoge más deprisa de lo que sube, y las hojitas abiertas no
    /// pasan de <see cref="TopAboveRimU"/>.</para>
    /// <para><b>Desbloqueo</b> (paso Open de la etapa (b), <see cref="OpenStepMs"/> × <c>ctx.SpeedFactor</c>; 02 §4.1: «brote
    /// germina en 2 hojas», 350 ms; b.2 no cambia esta duración): el capullo se hincha y se disuelve con unos destellos pequeños
    /// de su color (<see cref="IBoardFx.Sparkles"/>) mientras el cáliz se abre y se desvanece, y las dos hojitas germinan: se
    /// abren hacia fuera y crecen (hasta <see cref="OpenEnd"/>), se quedan abiertas en V y opacas (hasta
    /// <see cref="HoldEnd"/>) y en el último cuarto se desvanecen. El paso va en la cuia receptora y no espera a que el origen
    /// vuelva (contrato de T07): el brote abierto sigue a la vista cuando el origen aterriza.
    /// Mientras anima (<see cref="IsAnimating"/>), la vista no llama a <see cref="Show"/>; la cola cierra el paso con
    /// <see cref="SnapStep"/> y <c>ShowContent(ctx.After)</c>, que lo oculta. Marea lo vuelve a pintar cerrado.</para>
    /// <para><b>Brote de dos hojas</b> (<c>n</c> = 2; variante de la 1.0, estreno en el 343; 02 §9.1 «Variantes» y R10 b;
    /// propuesta de D-007 b.3: «Brote con dos hojas cerradas; cada dosis abre una»; tarea VA1b) [P]: en lugar de las dos hojitas
    /// que asoman por detrás, dos hojas grandes cerradas por delante, una a cada lado del capullo, inclinadas hacia el eje
    /// (<see cref="WrapClosedDeg"/>) y algo más oscuras (<see cref="WrapClosedShade"/>): el capullo se ve entre las dos y por
    /// encima, con su color. <see cref="Cuia.MechanicN"/> dice cuántas siguen cerradas: con 2, las dos; con 1 (ya recibió una
    /// dosis), la izquierda está abierta hacia fuera (<see cref="WrapOpenDeg"/>) y a plena luz. Cada dosis de su color abre una
    /// en el paso Open de la etapa (b) (<see cref="LeafStepMs"/>; el núcleo lo marca en <c>BroteLeafOpenedMask</c> y la cola lo
    /// despacha aquí, con <c>ctx.After</c> aún con el Brote): la hoja gira hacia fuera con un pequeño rebote y suelta unos
    /// destellos del color del capullo. La que germina hace el paso Open del Brote, con las dos hojas grandes en el papel de
    /// las hojitas: se abren del todo y se desvanecen. Un tramo de 2 en <c>run</c> lo hace germinar de una vez (D-005). Marea
    /// vuelve a cerrar la hoja (02 §1.2).</para>
    /// <para>Sin asignaciones en <see cref="Tick"/>, <see cref="PlayStep"/> ni <see cref="Show"/> (08 §5.4); las partículas
    /// salen del pool común de <see cref="FxLayer"/> (tope de 44) y con Reducir movimiento no se emiten.</para>
    /// </remarks>
    public sealed class BroteOverlay : CuiaOverlay
    {
        /// <summary>Duración nominal de la germinación (ms) [P]: 02 §4.1 (Hoja, Brote y Liana se abren en 350 ms).</summary>
        public const float OpenStepMs = 350f;

        /// <summary>
        /// Pie del capullo bajo lo alto de la cuia (u; desde <c>HeightU</c>, no desde el labio de vertido): encaja en el cuello,
        /// por dentro del borde de fibra.
        /// </summary>
        public const float CorkBelowTopU = 0.22f;

        /// <summary>
        /// Lo más alto del brote sobre lo alto de la cuia (u; desde <c>HeightU</c>), en reposo y en todo el paso: la punta del
        /// capullo, 1,0 u (prueba del dueño: ≥ 1 u).
        /// </summary>
        public const float TopAboveRimU = ProceduralArt.BroteCorkHeightU - CorkBelowTopU;

        /// <summary>
        /// Pie del cáliz bajo lo alto de la cuia (u; desde <c>HeightU</c>): cubre el borde de fibra entero (0,25 u) hasta el
        /// labio.
        /// </summary>
        public const float SepalsBelowTopU = 0.28f;

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

        /// <summary>Apertura de una hoja del Brote de dos hojas sin germinar (ms) [P]: la misma duración que la germinación.</summary>
        public const float LeafStepMs = OpenStepMs;

        /// <summary>Pie de cada hoja grande del Brote de dos hojas (u desde el eje), dentro del cáliz.</summary>
        public const float WrapFootXU = 0.32f;

        /// <summary>Pie de las hojas grandes sobre lo alto de la cuia (u; desde <c>HeightU</c>), dentro del cáliz.</summary>
        public const float WrapFootAboveTopU = 0.02f;

        /// <summary>
        /// Giro hacia fuera de una hoja grande cerrada (grados desde la vertical; negativo = hacia el eje): se cierra sobre el
        /// costado del capullo.
        /// </summary>
        public const float WrapClosedDeg = -10f;

        /// <summary>Giro hacia fuera de una hoja grande abierta (grados desde la vertical).</summary>
        public const float WrapOpenDeg = 48f;

        /// <summary>Escala a lo ancho de las hojas grandes (sobre la hojita del Brote): más estrechas, para que el capullo se vea.</summary>
        public const float WrapScaleX = 0.68f;

        /// <summary>Escala a lo largo de las hojas grandes (sobre la hojita del Brote).</summary>
        public const float WrapScaleY = 1.02f;

        /// <summary>Tono de una hoja grande cerrada (multiplica el verde de la hojita): plegada, en sombra; la abierta, a plena luz.</summary>
        public const float WrapClosedShade = 0.80f;

        /// <summary>Destellos al abrirse una hoja sin germinar.</summary>
        public const int LeafSparkleCount = 3;

        // Margen tras la duración con el que el overlay cierra solo un paso que la cola no cerró (no debería pasar).
        private const float StuckGuardMs = 1000f;

        // n del Brote de dos hojas al empezar (BroteMechanic.TwoLeaves, interno del núcleo; firma de pack_schema 3).
        private const int TwoLeavesN = 2;

        // Hojitas: pie (u desde el eje y sobre lo alto de la cuia, HeightU) y lo que sube al abrirse, como mucho.
        private const float LeafFootXU = 0.18f;
        private const float LeafFootAboveTopU = 0f;
        private const float LeafRiseU = 0.1f;

        private Image _cork = null!;
        private Image _gloss = null!;
        private Image _sepals = null!;
        private Image _leafL = null!;
        private Image _leafR = null!;
        private Image _wrapL = null!;
        private Image _wrapR = null!;
        private RectTransform _slot = null!;
        private IBoardFx? _fx;
        private Color _budColor = Color.white;
        private float _u = 24f;
        private float _heightU = 5.6f;
        private float _elapsed = -1f;
        private float _duration;
        private bool _sparkled;
        private bool _posed;
        private bool _twoLeaves;
        private int _closedLeaves;
        private bool _leafStep;
        private int _leafIndex;

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

        /// <summary>Muestra el Brote de dos hojas (<c>n</c> &gt; 0; VA1b): las dos hojas grandes por delante en lugar de las hojitas.</summary>
        public bool TwoLeaves => _twoLeaves;

        /// <summary>Hojas grandes que siguen cerradas (2 o 1) en el Brote de dos hojas; 0 en el Brote normal.</summary>
        public int ClosedLeaves => _twoLeaves ? _closedLeaves : 0;

        /// <summary>El paso en curso abre una hoja del Brote de dos hojas sin germinar.</summary>
        public bool IsLeafStep => _elapsed >= 0f && _leafStep;

        /// <summary>Las hojas grandes del Brote de dos hojas están a la vista.</summary>
        public bool WrapsVisible => _wrapL.gameObject.activeSelf && _wrapL.color.a > 0.001f;

        /// <summary>Giro hacia fuera (grados desde la vertical; negativo = hacia el eje) de la hoja grande izquierda (−1) o derecha (1).</summary>
        public float WrapOutDeg(int side)
        {
            RectTransform rt = (side < 0 ? _wrapL : _wrapR).rectTransform;
            float z = Mathf.DeltaAngle(0f, rt.localEulerAngles.z);
            return side < 0 ? z : -z;
        }

        /// <summary>Color pintado de la hoja grande izquierda (−1) o derecha (1), con su alfa.</summary>
        public Color WrapColor(int side)
        {
            return (side < 0 ? _wrapL : _wrapR).color;
        }

        /// <summary>Punta de la hoja grande izquierda (−1) o derecha (1), en u desde el pie.</summary>
        public Vector2 WrapTip(int side)
        {
            return LeafTipU(side < 0 ? _wrapL : _wrapR);
        }

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

                if (WrapsVisible)
                {
                    top = Mathf.Max(top, Mathf.Max(LeafTipU(_wrapL).y, LeafTipU(_wrapR).y));
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
            _twoLeaves = on && cuia.MechanicN > 0;
            _closedLeaves = _twoLeaves ? cuia.MechanicN : 0;
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
            else if (_twoLeaves)
            {
                PoseWrapsAtRest(1f); // las hojas grandes según las que siguen cerradas (VA1b)
            }

            OverlayParts.SetVisible(_cork, on);
            OverlayParts.SetVisible(_sepals, on);
            OverlayParts.SetVisible(_leafL, on && !_twoLeaves);
            OverlayParts.SetVisible(_leafR, on && !_twoLeaves);
            OverlayParts.SetVisible(_wrapL, _twoLeaves);
            OverlayParts.SetVisible(_wrapR, _twoLeaves);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            if (_elapsed >= 0f)
            {
                ApplyStep(StepProgress);
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

            // Brote de dos hojas (VA1b): las hojas que le faltaban antes del paso; si después sigue siendo Brote, solo se abre
            // una hoja (la primera cerrada: la izquierda con 2, la derecha con 1) y no germina.
            int before = ctx.Before.Mechanic == MechanicType.Brote ? ctx.Before.MechanicN : 0;
            _twoLeaves = before > 0;
            _closedLeaves = before;
            _leafStep = _twoLeaves && ctx.After.Mechanic == MechanicType.Brote;
            _leafIndex = Mathf.Clamp(TwoLeavesN - before, 0, 1);
            OverlayParts.SetVisible(_cork, true);
            OverlayParts.SetVisible(_sepals, true);
            OverlayParts.SetVisible(_leafL, !_twoLeaves);
            OverlayParts.SetVisible(_leafR, !_twoLeaves);
            OverlayParts.SetVisible(_wrapL, _twoLeaves);
            OverlayParts.SetVisible(_wrapR, _twoLeaves);
            ApplyStep(0f);
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
            ApplyStep(Mathf.Clamp01(_elapsed / Mathf.Max(1f, _duration)));
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed = -1f;
            if (_leafStep)
            {
                // La hoja abierta se queda: la cola pinta ShowContent(ctx.After) con el Brote de una hoja cerrada.
                _leafStep = false;
                _closedLeaves = Mathf.Max(0, _closedLeaves - 1);
                PlaceClosed();
                return;
            }

            PlaceClosed();
            OverlayParts.SetVisible(_cork, false);
            OverlayParts.SetVisible(_sepals, false);
            OverlayParts.SetVisible(_leafL, false);
            OverlayParts.SetVisible(_leafR, false);
            OverlayParts.SetVisible(_wrapL, false);
            OverlayParts.SetVisible(_wrapR, false);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.BroteLeafLight); // cáliz y hojitas (el capullo lleva el tinte de la cima a propósito)
            into.Add(ProceduralArt.BroteLeafDark);
            into.Add(Shade(ProceduralArt.BroteLeafLight, WrapClosedShade)); // las hojas grandes cerradas del Brote de dos hojas (VA1b)
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

            // Brote de dos hojas (VA1b): las dos hojas grandes, delante del capullo y del cáliz.
            _wrapL = OverlayParts.NewImage("BroteWrapL", Host.BodyFront, Host.Art.BroteSprout, Color.white);
            _wrapR = OverlayParts.NewImage("BroteWrapR", Host.BodyFront, Host.Art.BroteSprout, Color.white);
            _wrapL.rectTransform.pivot = new Vector2(0.5f, 0f);
            _wrapR.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        // Pose de reposo: el capullo en la boca sobre su cáliz y las hojitas plegadas, opacos y sin escala.
        private void PlaceClosed()
        {
            _posed = false;
            PoseCork(_heightU - CorkBelowTopU, 1f, 1f, 1f);
            PoseSepals(1f, 1f, 1f);
            PoseLeaf(_leafL, -1f, ClosedLeafDeg, 1f, 0f, 1f);
            PoseLeaf(_leafR, 1f, ClosedLeafDeg, 1f, 0f, 1f);
            PoseWrapsAtRest(1f);
        }

        // Hojas grandes del Brote de dos hojas en reposo: abiertas las primeras (2 − cerradas) desde la izquierda, con su alfa.
        private void PoseWrapsAtRest(float alpha)
        {
            int open = TwoLeavesN - _closedLeaves;
            PoseWrap(_wrapL, -1f, open > 0 ? 1f : 0f, alpha);
            PoseWrap(_wrapR, 1f, open > 1 ? 1f : 0f, alpha);
        }

        // Hoja grande del lado side con apertura a (0 cerrada sobre el capullo, 1 abierta; pasa de 1 en el rebote y al
        // germinar) y alfa.
        private void PoseWrap(Image wrap, float side, float a, float alpha)
        {
            float deg = Mathf.LerpUnclamped(WrapClosedDeg, WrapOpenDeg, a);
            OverlayParts.Place(wrap, side * WrapFootXU, _heightU + WrapFootAboveTopU, ProceduralArt.BroteSproutWidthU, ProceduralArt.BroteSproutHeightU, _u);
            RectTransform rt = wrap.rectTransform;
            // La izquierda gira a la izquierda (z positiva) al abrirse. Se compara componente a componente: el == de Quaternion
            // da por iguales giros de menos de ≈ 0,1°, y el último fotograma del rebote no volvería a la pose de reposo.
            Quaternion rotation = Quaternion.Euler(0f, 0f, -side * deg);
            Quaternion current = rt.localRotation;
            if (current.z != rotation.z || current.w != rotation.w || current.x != rotation.x || current.y != rotation.y)
            {
                rt.localRotation = rotation;
            }

            var s = new Vector3(side * WrapScaleX, WrapScaleY, 1f);
            if (rt.localScale != s)
            {
                rt.localScale = s;
            }

            float shade = Mathf.Lerp(WrapClosedShade, 1f, Mathf.Clamp01(a));
            OverlayParts.SetColor(wrap, new Color(shade, shade, shade, alpha));
        }

        // Paso en curso: abrir una hoja del Brote de dos hojas o germinar.
        private void ApplyStep(float t)
        {
            if (_leafStep)
            {
                ApplyLeaf(t);
            }
            else
            {
                Apply(t);
            }
        }

        // Fotograma t (0..1) de la apertura de una hoja sin germinar: el capullo no cambia; la hoja _leafIndex gira hacia fuera
        // con un pequeño rebote y se aclara, y a mitad de camino suelta unos destellos del color del capullo.
        private void ApplyLeaf(float t)
        {
            _posed = true;
            PoseCork(_heightU - CorkBelowTopU, 1f, 1f, 1f);
            PoseSepals(1f, 1f, 1f);
            int open = TwoLeavesN - _closedLeaves; // las que ya estaban abiertas antes del paso
            float a = Ease.OutBack(Mathf.Clamp01(t / 0.8f));
            PoseWrap(_wrapL, -1f, _leafIndex == 0 ? a : (open > 0 ? 1f : 0f), 1f);
            PoseWrap(_wrapR, 1f, _leafIndex == 1 ? a : (open > 1 ? 1f : 0f), 1f);
            if (!_sparkled && t >= 0.35f)
            {
                _sparkled = true;
                Vector2 tip = LeafTipU(_leafIndex == 0 ? _wrapL : _wrapR);
                _fx?.Sparkles(_slot.anchoredPosition + (tip * _u), ColorMath.Lighten(_budColor, 0.3f), LeafSparkleCount, _u);
            }
        }

        // Fotograma t (0..1) de la germinación; los destellos salen una sola vez al cruzar su instante.
        private void Apply(float t)
        {
            _posed = true;
            float footY = _heightU - CorkBelowTopU;
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

                // El pie sube, pero nunca más de lo que encoge el capullo: la punta no pasa de TopAboveRimU en ningún
                // fotograma (con el pie subiendo 0,08 p sin tope, la punta subía ≈ 0,003 u al empezar a encoger).
                float lift = Mathf.Min(0.08f * p, ProceduralArt.BroteCorkHeightU * (1f - shrink));
                PoseCork(footY + lift, Mathf.Lerp(1.12f, 0.55f, Ease.In(p)), shrink, fade);
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
            if (_twoLeaves)
            {
                // Brote de dos hojas (VA1b): las hojas grandes hacen de hojitas: las cerradas se abren con ellas, las ya abiertas
                // siguen el vaivén, y todas se desvanecen con ellas.
                float spread = (open - ClosedLeafDeg) / (OpenLeafDeg - ClosedLeafDeg);
                int alreadyOpen = TwoLeavesN - _closedLeaves;
                float aL = alreadyOpen > 0 ? Mathf.Max(1f, spread) : spread;
                float aR = alreadyOpen > 1 ? Mathf.Max(1f, spread) : spread;
                PoseWrap(_wrapL, -1f, Mathf.Max(0f, aL), alpha);
                PoseWrap(_wrapR, 1f, Mathf.Max(0f, aR), alpha);
            }
        }

        // Capullo con pie en footY (u), escala (sx, sy) desde el pie y opacidad; su brillo va con él.
        private void PoseCork(float footY, float sx, float sy, float alpha)
        {
            OverlayParts.Place(_cork, 0f, footY, ProceduralArt.BroteCorkWidthU, ProceduralArt.BroteCorkHeightU, _u);
            OverlayParts.Place(_gloss, -0.2f, 0.72f, 0.14f, 0.32f, _u); // dentro del capullo, arriba a la izquierda (a 0,59 de su alto)
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
            OverlayParts.Place(_sepals, 0f, _heightU - SepalsBelowTopU, ProceduralArt.BroteSepalsWidthU, ProceduralArt.BroteSepalsHeightU, _u);
            SetScale(_sepals.rectTransform, sx, sy);
            OverlayParts.SetColor(_sepals, new Color(1f, 1f, 1f, alpha));
        }

        // Hojita del lado side (−1 izquierda, 1 derecha): pie detrás del capullo, giro open (grados hacia fuera desde la
        // vertical), escala, subida (u) y opacidad. La derecha es la izquierda en espejo.
        private void PoseLeaf(Image leaf, float side, float open, float scale, float rise, float alpha)
        {
            float footX = side * (LeafFootXU + (0.06f * Mathf.Clamp01((open - ClosedLeafDeg) / (OpenLeafDeg - ClosedLeafDeg))));
            float footY = _heightU + LeafFootAboveTopU + rise;
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

        private static Color Shade(Color c, float shade)
        {
            return new Color(c.r * shade, c.g * shade, c.b * shade, c.a);
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
