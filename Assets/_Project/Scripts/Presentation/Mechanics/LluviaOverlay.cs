#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M6 Lluvia (02 §9.1 M6; propuesta de D-007 b.2 M6; Q18 B) [P]: una nube gris perla con volumen sobre la boca, dentro de
    /// los 32 dp táctiles sobre la cuia (02 §3.3), con una fila de <c>k</c> puntos en su base maciza (llenos los vertidos útiles
    /// ya contados: solo cambian con vertidos útiles) y, colgando de la panza, una gotita del agua del mundo por cada gota
    /// pendiente. Son los mismos campos que el contador del HUD (<c>LevelCounter.Of</c>). Con la cuia llena (la gota esperaría)
    /// la nube se carga de tormenta (un gris algo más oscuro y la panza en sombra) y, si la gota ya tocaba, queda colgando sobre
    /// la boca. En vuelo se oculta (la nube no se inclina).
    /// </summary>
    /// <remarks>
    /// <para>Sitio (02 §5; revisión de T17): lo alto de la nube queda como mucho a <see cref="CloudTopMaxDp"/> de la boca, así
    /// que no pisa la madera del marco del panel en la fila de arriba ni tapa nunca la fila de arriba. Al irse con la última
    /// gota sube y encoge a la vez sin pasar de <see cref="CloudTopLimitDp"/>: el pie de la fila de arriba (elevación + 24 dp =
    /// 40 dp, menos 1; como mucho <see cref="RiseTopMaxDp"/>) o, en la fila de arriba, el marco (sube poco y se encoge). Es una
    /// consulta de las posiciones del tablero (<see cref="ICuiaBoard.Foot"/>), no una regla.</para>
    /// <para>Pasos de la etapa (e) (arquitectura del paso 1 §4; <see cref="MechanicStepQueue"/>), cada uno desde
    /// <c>ctx.Before</c> hasta <c>ctx.After</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="MechanicStepKind.RainDrop"/> (<see cref="DropStepMs"/> × <c>ctx.SpeedFactor</c>): la nube
    /// se encoge y suelta una gota del agua del mundo (<c>Palette.Tint(MechanicKey)</c>), que cae por la boca y, al tocar la
    /// superficie (el 70 % del paso), forma la dosis de arriba y salpica en la superficie que sube (<c>IBoardFx.Splash</c>; con
    /// Reducir movimiento el pool no emite). La dosis se pinta con <c>ctx.Board.SetDose</c>: <c>ctx.Before</c> no la tiene y
    /// <c>ctx.After</c> sí. Los puntos se llenan todos al empezar (el vertido que completa el ritmo) y vuelven a los de
    /// <c>After</c> al caer la gota. Si la gota sella la cuia, la tapa es el paso Seal de la etapa (c′), después: aquí no se
    /// dibuja.</description></item>
    /// <item><description><see cref="MechanicStepKind.RainEnd"/> (la última gota, un único paso con la caída; misma duración):
    /// como la gota y, mientras cae, la nube sube y se desvanece con sus puntos.</description></item>
    /// <item><description><see cref="MechanicStepKind.RainWait"/> (<see cref="WaitStepMs"/>; la cuia está llena): la tormenta
    /// se carga, la gota que ya colgaba intenta caer (baja hacia la boca llena con un pulso más oscuro de la nube), no cabe y
    /// vuelve a colgar con un temblor; los puntos vuelven a <c>k − 1</c>. Si aún no colgaba, asoma primero.</description></item>
    /// </list>
    /// <para>Al acabar (o con <see cref="SnapStep"/>) la dosis queda entera y se pinta <c>ShowContent(ctx.After)</c>. Las
    /// duraciones son [P] (la de la gota no está en ningún documento; la revisa el dueño). Sin asignaciones en los pasos ni en
    /// <see cref="CuiaOverlay.Tick"/>; en reposo no se mueve nada (D-002 y 08 §5.4).</para>
    /// </remarks>
    public sealed class LluviaOverlay : CuiaOverlay
    {
        /// <summary>Alto máximo de la nube (u).</summary>
        public const float CloudMaxU = 1f;

        /// <summary>Hueco entre la boca y la panza de la nube (u).</summary>
        public const float CloudGapU = 0.18f;

        /// <summary>
        /// Lo alto de la nube sobre la boca, como mucho (dp): el canto interior del marco del panel queda a unos 24,5 dp de la
        /// boca de la fila de arriba (<c>BoardLayout</c>: margen del panel 22 + elevación 16 − marco 13,5).
        /// </summary>
        public const float CloudTopMaxDp = 24f;

        /// <summary>
        /// Al irse con la última gota, lo alto de la nube no pasa de aquí (dp): el pie de la fila de arriba está a elevación 16
        /// + separación 24 = 40 dp de la boca (<c>BoardLayout</c>; 02 §5).
        /// </summary>
        public const float RiseTopMaxDp = 39f;

        /// <summary>Gota (RainDrop) y última gota (RainEnd): 500 ms [P, ≤ 600; no está en ningún documento].</summary>
        public const float DropStepMs = 500f;

        /// <summary>La gota espera con la cuia llena (RainWait): 400 ms [P].</summary>
        public const float WaitStepMs = 400f;

        /// <summary>Lo que sube la nube al irse con la última gota, como mucho (u) [P]; lo recorta <see cref="CloudTopLimitDp"/>.</summary>
        public const float RiseU = 0.7f;

        /// <summary>Lo que encoge la nube al irse con la última gota (fracción de su tamaño) [P].</summary>
        public const float EndShrink = 0.3f;

        /// <summary>Pulso de tormenta mientras la gota intenta caer (fracción del paso de perla a tormenta) [P].</summary>
        public const float StormPulse = 0.12f;

        /// <summary>
        /// Opacidad de los puntos vacíos (el aro) frente a los llenos, en la nube: el vertido que falta se distingue por el
        /// contraste, no por un agujero de menos de 2 dp (revisión de T17; 02 §5, cuias de 48 dp). Pizarra sobre perla: con la
        /// mezcla en lineal del proyecto, el aro queda tenue sobre la nube.
        /// </summary>
        public const float EmptyPipAlpha = 0.4f;

        // Fila de puntos en la base maciza de la nube (fracciones de su alto): centro, lado y paso entre centros.
        private const float PipRowBelowCenter = 0.20f;
        private const float PipSizeOfCloud = 0.375f;
        private const float PipStepOfCloud = 0.49f;

        // Mitad del ancho útil de la base para los centros de los puntos (fracción del alto de la nube).
        private const float PipRowHalfWidthOfCloud = 0.72f;

        // Gotitas pendientes y gota colgando (fracciones del alto de la nube): lado, paso y lo alto de su punta, que asoma bajo
        // la panza justo por debajo de los puntos.
        private const float DropSizeOfCloud = 0.37f;
        private const float DropStepOfCloud = 0.46f;
        private const float HangSizeOfCloud = 0.42f;
        private const float DropTipAboveBottom = 0.10f;

        // Fases de la gota (fracción del paso): se forma bajo la nube, cae y, al tocar la superficie, llena la dosis.
        private const float FormEnd = 0.3f;
        private const float FallEnd = 0.7f;

        // Fases de la espera: la gota asoma (si no colgaba), intenta caer y vuelve a colgar.
        private const float WaitGrowEnd = 0.35f;
        private const float WaitTryEnd = 0.7f;
        private const float WaitPipsAt = 0.6f;

        // Gota que cae (u): ancho, y su alto con el estirón de la caída.
        private const float FallDropU = 0.4f;

        private readonly Image[] _drops = new Image[PipRow.MaxPips];
        private Image _cloud = null!;
        private Image _hang = null!;
        private Image _fall = null!;
        private PipRow _pips = null!;
        private bool _on;
        private bool _inFlight;
        private bool _dark;
        private bool _hangOn;
        private bool _rowAroundHang;
        private int _rowCount;
        private int _pipK = -1;
        private int _key = -1;
        private float _u = 24f;
        private float _heightU = CuiaGeometry.HeightU(4);
        private float _cloudY;
        private float _cloudH = CloudMaxU;
        private float _cloudW = ProceduralArt.LluviaCloudWidthU;
        private float _topLimitDp = CloudTopMaxDp;
        private float _dropY;
        private float _dropSize = 0.3f;
        private float _dropStep = 0.38f;
        private float _hangY;
        private float _hangSize = 0.34f;

        // Paso en curso.
        private bool _animating;
        private bool _fresh;
        private MechanicStepKind _kind;
        private float _t;
        private float _dur;
        private int _dose = -1;
        private int _tint;
        private Color _color;
        private float _startDark;
        private bool _hangAtStart;
        private ICuiaBoard? _board;
        private int _index;
        private Cuia _after;
        private bool _afterSealed;
        private int _afterK;
        private int _afterCounter;
        private bool _impact;
        private bool _splashed;
        private bool _pipsReset;
        private Vector2 _surface;
        private IBoardFx? _fx;
        private bool _reduceMotion;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Lluvia;

        /// <inheritdoc />
        public override bool IsAnimating => _animating;

        /// <summary>Puntos del contador: el ritmo <c>k</c>.</summary>
        public int PipCount => _on ? _pips.Count : 0;

        /// <summary>Puntos llenos: vertidos útiles contados desde la última gota.</summary>
        public int FilledPips => _on ? _pips.Filled : 0;

        /// <summary>Fila de puntos del contador (tests: tamaño y opacidad de cada punto).</summary>
        public PipRow Pips => _pips;

        /// <summary>Gotas pendientes a la vista: las gotitas de la fila y la que cuelga.</summary>
        public int DropCount => _on ? _rowCount + (_hangOn ? 1 : 0) : 0;

        /// <summary>La nube está cargada de tormenta: la cuia está llena y la gota esperaría.</summary>
        public bool Stormy => _on && _dark;

        /// <summary>La gota que tocaba cuelga sobre la boca (cuia llena con el ritmo cumplido).</summary>
        public bool HangingDrop => _on && _hang.color.a > 0.01f && _hang.gameObject.activeSelf;

        /// <summary>Lo que ha bajado la gota colgando desde su sitio (u; 0 en reposo, más al intentar caer).</summary>
        public float HangingDropDipU => _hangY - (_hang.rectTransform.anchoredPosition.y / _u);

        /// <summary>Escala de la gota colgando (1 en reposo; la que ya colgaba no se encoge al empezar la espera).</summary>
        public float HangingDropScale
        {
            get
            {
                Vector3 s = _hang.rectTransform.localScale;
                return Mathf.Min(s.x, s.y);
            }
        }

        /// <summary>Hay una gota cayendo (o formándose) a la vista.</summary>
        public bool FallingDropVisible => _fall.color.a > 0.01f && _fall.gameObject.activeSelf;

        /// <summary>Altura del centro de la gota que cae sobre el pie (u).</summary>
        public float FallingDropHeightU => _fall.rectTransform.anchoredPosition.y / _u;

        /// <summary>Opacidad de la nube (baja al irse con la última gota).</summary>
        public float CloudAlpha => _cloud.color.a;

        /// <summary>Lo que ha subido la nube sobre su sitio (u).</summary>
        public float CloudRiseU => (_cloud.rectTransform.anchoredPosition.y / _u) - _cloudY;

        /// <summary>
        /// Oscuridad de la nube: 0 perla, 1 tormenta y algo más (hasta 1 + <see cref="StormPulse"/>) mientras la gota intenta
        /// caer.
        /// </summary>
        public float CloudDarkness
        {
            get
            {
                Color c = _cloud.color;
                return Mathf.Max(0f, (1f - c.g) / (1f - ProceduralArt.LluviaStormTint.g));
            }
        }

        /// <summary>La nube lleva la panza en sombra (1 con la tormenta cargada; 0 perla).</summary>
        public float BellyShade => _cloud.sprite == Host.Art.LluviaCloudStormSprite ? 1f : 0f;

        /// <summary>
        /// Lo alto de la nube sobre la boca, con su escala (dp; ≤ <see cref="CloudTopMaxDp"/> en reposo, dentro del área
        /// táctil, y ≤ <see cref="CloudTopLimitDp"/> mientras se va).
        /// </summary>
        public float CloudTopAboveMouthDp
        {
            get
            {
                RectTransform rt = _cloud.rectTransform;
                return rt.anchoredPosition.y + (0.5f * rt.sizeDelta.y * rt.localScale.y) - (Host.HeightU * _u);
            }
        }

        /// <summary>
        /// Lo más alto que llega la nube en su sitio del tablero (dp sobre la boca): el pie de la fila de arriba menos 1 dp
        /// (como mucho <see cref="RiseTopMaxDp"/>) o, en la fila de arriba, el marco del panel (<see cref="CloudTopMaxDp"/>).
        /// </summary>
        public float CloudTopLimitDp => _topLimitDp;

        /// <summary>
        /// Pinta los puntos de <paramref name="row"/> del color <paramref name="color"/> con opacidad <paramref name="alpha"/>:
        /// los llenos enteros y los vacíos (el aro) a <paramref name="emptyAlpha"/> de esa opacidad (sin asignaciones; solo
        /// cambia lo que cambia). También la usa la cueva de la Piraña, con su propia opacidad de los vacíos.
        /// </summary>
        internal static void PaintPips(PipRow row, Color color, float alpha, float emptyAlpha)
        {
            Color full = OverlayParts.Fade(color, alpha);
            Color empty = OverlayParts.Fade(color, alpha * emptyAlpha);
            for (int i = 0; i < PipRow.MaxPips; i++)
            {
                OverlayParts.SetColor(row.Pip(i), i < row.Filled ? full : empty);
            }
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            _on = cuia.Mechanic == MechanicType.Lluvia;
            Visible = _on;
            if (_on)
            {
                SetPips(cuia.MechanicK, cuia.MechanicCounter);
                SetKey(cuia.MechanicKey);
                int pending = Mathf.Clamp(cuia.MechanicN, 0, _drops.Length);
                _dark = cuia.IsFull;
                _hangOn = _dark && pending > 0 && cuia.MechanicCounter >= cuia.MechanicK - 1;
                SetRow(_hangOn ? pending - 1 : pending, _hangOn);
            }

            Apply();
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            _u = u;
            _heightU = heightU;
            _cloudH = Mathf.Min(CloudMaxU, (CloudTopMaxDp / u) - CloudGapU);
            _cloudW = _cloudH * ProceduralArt.LluviaCloudWidthU / ProceduralArt.LluviaCloudHeightU;
            _cloudY = heightU + CloudGapU + (0.5f * _cloudH);
            UpdateHeadroom();
            LayoutPips();
            float bottom = heightU + CloudGapU; // canto inferior del sprite de la nube
            float tip = bottom + (DropTipAboveBottom * _cloudH);
            _dropSize = DropSizeOfCloud * _cloudH;
            _dropStep = DropStepOfCloud * _cloudH;
            _dropY = tip - (0.5f * _dropSize * ProceduralArt.LluviaDropAspect);
            _hangSize = HangSizeOfCloud * _cloudH;
            _hangY = tip - (0.5f * _hangSize * ProceduralArt.LluviaDropAspect);
            PlaceRow();
            if (!_animating)
            {
                PlaceAtRest();
            }
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            UpdateHeadroom();
        }

        /// <inheritdoc />
        public override void SetInFlight(bool inFlight)
        {
            _inFlight = inFlight;
            Apply();
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            switch (kind)
            {
                case MechanicStepKind.RainDrop:
                case MechanicStepKind.RainEnd:
                    return DropStepMs;
                case MechanicStepKind.RainWait:
                    return WaitStepMs;
                default:
                    return 0f;
            }
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            float nominal = StepDurationMs(ctx.Kind);
            if (nominal <= 0f)
            {
                return 0f;
            }

            if (_animating)
            {
                FinishStep(false);
            }

            Cuia before = ctx.Before;
            _hangAtStart = HangingDrop; // la gota que ya colgaba no desaparece al empezar la espera
            _kind = ctx.Kind;
            _board = ctx.Board;
            _index = ctx.Index;
            _after = ctx.After;
            _afterSealed = ctx.AfterSealed;
            _afterK = ctx.After.MechanicK;
            _afterCounter = ctx.After.MechanicCounter;
            _fx = ctx.Fx;
            _reduceMotion = ctx.ReduceMotion;
            _dur = nominal * Mathf.Max(0.01f, ctx.SpeedFactor);
            _t = 0f;
            _fresh = true;
            _animating = true;
            _impact = false;
            _splashed = false;
            _pipsReset = false;
            _startDark = Mathf.Min(1f, CloudDarkness);
            _tint = ctx.Tint > 0 ? ctx.Tint : before.MechanicKey;
            _color = Palette.Tint(_tint);
            SetKey(before.MechanicKey);

            // El vertido que completa el ritmo llena todos los puntos.
            int k = before.MechanicK;
            SetPips(k, k);
            int pending = Mathf.Clamp(before.MechanicN, 0, _drops.Length);
            bool wait = _kind == MechanicStepKind.RainWait;
            SetRow(pending > 0 ? pending - 1 : 0, wait); // la gota que cae (o la que cuelga) sale de la fila
            if (wait)
            {
                _dose = -1;
            }
            else
            {
                _dose = before.Count;
                if (_board != null && _dose >= 0 && _dose < Host.Capacity)
                {
                    _board.SetDose(_index, _dose, _color, 0f, _tint);
                }

                Vector2 surface = ctx.SelfPos + new Vector2(0f, (CuiaGeometry.CavityBottomU + _dose) * _u);
                RectTransform? space = ctx.BoardFrontLayer;
                Rect r = space != null ? space.rect : default;
                _surface = new Vector2(surface.x - r.xMin, surface.y - r.yMax);
            }

            Apply();
            Pose(0f);
            return _dur;
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (!_animating)
            {
                return;
            }

            // El primer fotograma del paso no avanza: la cola ya llevaba parte de él al llamar a PlayStep, así que la
            // animación nunca va por delante del paso de la cola.
            if (_fresh)
            {
                _fresh = false;
            }
            else
            {
                _t += dtMs;
            }

            Pose(_dur > 0f ? Mathf.Clamp01(_t / _dur) : 1f);
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            FinishStep(true);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            // Nube (también cargada de tormenta, con el pulso y con la panza en sombra) y puntos; las gotitas llevan el agua
            // del mundo a propósito.
            Color storm = ProceduralArt.LluviaStormTint;
            Color pulse = StormColor(1f + StormPulse, 1f);
            Color belly = new Color(ProceduralArt.LluviaBellyShade, ProceduralArt.LluviaBellyShade, ProceduralArt.LluviaBellyShade, 1f);
            into.Add(ProceduralArt.Pearl);
            into.Add(ProceduralArt.LluviaCloudLit);
            into.Add(ProceduralArt.LluviaCloudBelly);
            into.Add(ProceduralArt.Pearl * storm);
            into.Add(ProceduralArt.LluviaCloudLit * storm);
            into.Add(ProceduralArt.LluviaCloudBelly * storm);
            into.Add(ProceduralArt.Pearl * pulse);
            into.Add(ProceduralArt.LluviaCloudBelly * pulse);
            into.Add(ProceduralArt.Pearl * storm * belly);
            into.Add(ProceduralArt.LluviaCloudBelly * storm * belly);
            into.Add(ProceduralArt.LluviaCloudBelly * pulse * belly);
            into.Add(ProceduralArt.Slate);
            // El punto vacío sobre la nube, perla y de tormenta (mezcla en sRGB y en lineal, como la pinta la UI).
            into.Add(Color.Lerp(ProceduralArt.Pearl, ProceduralArt.Slate, EmptyPipAlpha));
            into.Add(Color.Lerp(ProceduralArt.Pearl.linear, ProceduralArt.Slate.linear, EmptyPipAlpha).gamma);
            into.Add(Color.Lerp(ProceduralArt.Pearl * storm, ProceduralArt.Slate, EmptyPipAlpha));
            into.Add(Color.Lerp((ProceduralArt.Pearl * storm).linear, ProceduralArt.Slate.linear, EmptyPipAlpha).gamma);
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            ProceduralArt art = Host.Art;
            _fall = NewStepImage("LluviaFallingDrop", Host.BodyBack, art.LluviaDropSprite(10)); // detrás del líquido y del vidrio
            _cloud = OverlayParts.NewImage("LluviaCloud", Host.BodyFront, art.LluviaCloudSprite, Color.white);
            _ = art.LluviaCloudStormSprite; // la nube de tormenta se genera ahora: el paso no asigna
            _pips = new PipRow("LluviaPip", Host.BodyFront, art.CounterPip, art.CounterPipEmpty, ProceduralArt.Slate);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = OverlayParts.NewImage("LluviaDrop" + i, Host.BodyFront, art.LluviaDropSprite(10), Color.white);
            }

            _hang = NewStepImage("LluviaHangingDrop", Host.BodyFront, art.LluviaDropSprite(10));
            PaintCloudPips(1f);
        }

        // Imagen siempre activa y registrada en su Canvas que se muestra con el alfa: los pasos no la activan (sin asignar).
        private static Image NewStepImage(string name, Transform parent, Sprite sprite)
        {
            Image img = OverlayParts.NewImage(name, parent, sprite, new Color(1f, 1f, 1f, 0f));
            img.canvasRenderer.cullTransparentMesh = true;
            img.gameObject.SetActive(true);
            return img;
        }

        private void SetPips(int k, int filled)
        {
            if (k != _pipK)
            {
                _pipK = k;
                LayoutPips();
            }

            _pips.Set(k, filled);
            PaintCloudPips(1f); // en un paso, Pose la vuelve a pintar con la opacidad de la nube
        }

        // Puntos de 0,30-0,32 u con la nube de 0,82 u (≈ 7 dp con la cuia de 48 dp) en la base maciza, lejos de las muescas
        // entre lóbulos; con k ≥ 5 se juntan (y encogen) para seguir dentro de la base.
        private void LayoutPips()
        {
            LayoutPips(0f, 1f);
        }

        // La fila de puntos con la nube subida rise u y a escala scale (al irse con la última gota, los puntos van con ella).
        private void LayoutPips(float rise, float scale)
        {
            int k = Mathf.Max(1, _pipK);
            float step = PipStepOfCloud * _cloudH;
            if (k > 1)
            {
                step = Mathf.Min(step, 2f * PipRowHalfWidthOfCloud * _cloudH / (k - 1));
            }

            float size = Mathf.Min(PipSizeOfCloud * _cloudH, 0.9f * step);
            _pips.Layout(0f, _cloudY + rise - (PipRowBelowCenter * _cloudH * scale), size * scale, step * scale, _u);
        }

        // Los puntos de la nube: pizarra, los vacíos (el aro) más tenues.
        private void PaintCloudPips(float alpha)
        {
            PaintPips(_pips, ProceduralArt.Slate, alpha, EmptyPipAlpha);
        }

        // Lo más alto que puede llegar la nube en su sitio (dp sobre la boca): el pie de la cuia más baja de las que quedan por
        // encima de la boca (la fila de arriba) menos 1 dp, entre CloudTopMaxDp y RiseTopMaxDp; sin fila encima, el marco del
        // panel (CloudTopMaxDp). Se recalcula con cada recolocación del tablero (sin asignaciones).
        private void UpdateHeadroom()
        {
            float limit = CloudTopMaxDp;
            ICuiaBoard? board = Host.Board;
            int index = Host.Index;
            if (board != null && (uint)index < (uint)board.Count)
            {
                float mouth = board.Foot(index).y + (_heightU * _u);
                float above = float.MaxValue;
                for (int j = 0; j < board.Count; j++)
                {
                    float foot = board.Foot(j).y;
                    if (foot > mouth + 1f && foot < above)
                    {
                        above = foot;
                    }
                }

                if (above < float.MaxValue)
                {
                    limit = Mathf.Clamp(above - mouth - 1f, CloudTopMaxDp, RiseTopMaxDp);
                }
            }

            _topLimitDp = limit;
        }

        private void SetKey(int key)
        {
            if (key == _key)
            {
                return;
            }

            _key = key;
            Sprite drop = Host.Art.LluviaDropSprite(key);
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i].sprite = drop;
            }

            _hang.sprite = drop;
            _fall.sprite = drop;
        }

        // La fila de gotitas; con la gota colgando sobre la boca (en el eje), las demás se apartan a los lados.
        private void SetRow(int count, bool aroundHang)
        {
            count = Mathf.Clamp(count, 0, _drops.Length);
            if (count != _rowCount || aroundHang != _rowAroundHang)
            {
                _rowCount = count;
                _rowAroundHang = aroundHang;
                PlaceRow();
            }
        }

        private void PlaceRow()
        {
            float start = -0.5f * _dropStep * (_rowCount - 1);
            float sideStep = 0.5f * (_dropSize + _hangSize) + (0.12f * _dropSize);
            for (int i = 0; i < _drops.Length; i++)
            {
                float x = start + (i * _dropStep);
                if (_rowAroundHang)
                {
                    int slot = (i / 2) + 1;
                    x = (i % 2 == 0 ? 1f : -1f) * (sideStep + ((slot - 1) * _dropStep));
                }

                OverlayParts.Place(_drops[i], x, _dropY, _dropSize, _dropSize * ProceduralArt.LluviaDropAspect, _u);
            }
        }

        // Reposo: la nube en su sitio (perla o tormenta), la gota colgando si toca y ninguna cayendo.
        private void PlaceAtRest()
        {
            bool shown = _on && !_inFlight;
            PlaceCloud(0f, 1f, 1f);
            ShadeCloud(_dark ? 1f : 0f, 1f);
            LayoutPips(); // en su sitio, también tras irse con la última gota (Marea la puede traer de vuelta)
            PaintCloudPips(1f);
            SetRowAlpha(1f);
            OverlayParts.Place(_hang, 0f, _hangY, _hangSize, _hangSize * ProceduralArt.LluviaDropAspect, _u);
            SetScale(_hang.rectTransform, 1f, 1f);
            OverlayParts.SetColor(_hang, new Color(1f, 1f, 1f, shown && _hangOn ? 1f : 0f));
            OverlayParts.SetColor(_fall, new Color(1f, 1f, 1f, 0f));
        }

        private void Apply()
        {
            bool shown = _on && !_inFlight;
            OverlayParts.SetVisible(_cloud, shown);
            _pips.SetVisible(shown);
            for (int i = 0; i < _drops.Length; i++)
            {
                OverlayParts.SetVisible(_drops[i], shown && i < _rowCount);
            }

            if (!_animating)
            {
                PlaceAtRest();
            }
        }

        // La nube: rise u sobre su sitio y escala (sx, sy).
        private void PlaceCloud(float rise, float sx, float sy)
        {
            OverlayParts.Place(_cloud, 0f, _cloudY + rise, _cloudW, _cloudH, _u);
            SetScale(_cloud.rectTransform, sx, sy);
        }

        // Tormenta: el gris de toda la nube (dark de 0 a 1 + el pulso) y, cargada, la panza en sombra (otro sprite: la
        // tormenta es de reposo, se carga en Show cuando la cuia se llena, así que el cambio no se ve a medias).
        private void ShadeCloud(float dark, float alpha)
        {
            OverlayParts.SetColor(_cloud, StormColor(dark, alpha));
            Sprite sprite = dark >= 0.5f ? Host.Art.LluviaCloudStormSprite : Host.Art.LluviaCloudSprite;
            if (_cloud.sprite != sprite)
            {
                _cloud.sprite = sprite;
            }
        }

        // Fotograma del paso en la fracción p (sin asignaciones).
        private void Pose(float p)
        {
            if (_kind == MechanicStepKind.RainWait)
            {
                PoseWait(p);
            }
            else
            {
                PoseDrop(p);
            }
        }

        private void PoseDrop(float p)
        {
            // Nube: se encoge al soltar la gota; con la última, sube y encoge a la vez sin que lo alto pase de CloudTopLimitDp
            // (el pie de la fila de arriba o, en la fila de arriba, el marco: allí casi no sube) y se desvanece deprisa con sus
            // puntos, que van con ella.
            float squash = p < FormEnd ? Mathf.Sin(Mathf.PI * p / FormEnd) : 0f;
            float rise = 0f;
            float shrink = 1f;
            float alpha = 1f;
            if (_kind == MechanicStepKind.RainEnd && p > FormEnd)
            {
                float q = (p - FormEnd) / (1f - FormEnd);
                float e = Ease.InOut(q);
                shrink = 1f - (EndShrink * e);
                float room = (_topLimitDp / _u) - (CloudGapU + (0.5f * _cloudH) + (0.5f * _cloudH * shrink));
                rise = Mathf.Min(RiseU * e, Mathf.Max(0f, room));
                alpha = 1f - Ease.Out(q);
                LayoutPips(rise, shrink);
            }

            PlaceCloud(rise, (1f + (0.05f * squash)) * shrink, (1f - (0.08f * squash)) * shrink);
            float dark = Mathf.Lerp(_startDark, 0f, Mathf.Clamp01(p / FormEnd)); // con hueco, la tormenta se aclara
            ShadeCloud(dark, alpha);
            PaintCloudPips(alpha);
            SetRowAlpha(alpha);
            OverlayParts.SetColor(_hang, new Color(1f, 1f, 1f, 0f)); // con hueco no cuelga ninguna: la que cae es la de la fila

            // Gota: se forma bajo la panza, cae con gravedad y, al tocar la superficie, llena la dosis y salpica.
            float surfaceU = CuiaGeometry.CavityBottomU + Mathf.Max(0, _dose);
            if (p < FormEnd)
            {
                float grow = 0.55f + (0.45f * Ease.Out(p / FormEnd));
                PlaceFall(_dropY, grow, 1f, 1f);
            }
            else if (p < FallEnd)
            {
                float q = (p - FormEnd) / (FallEnd - FormEnd);
                float y = Mathf.Lerp(_dropY, surfaceU + (0.5f * FallDropU), q * q);
                PlaceFall(y, 1f, 1f + (0.25f * q), 1f);
            }
            else
            {
                OverlayParts.SetColor(_fall, new Color(1f, 1f, 1f, 0f));
                if (!_impact)
                {
                    _impact = true;
                    if (_kind == MechanicStepKind.RainDrop)
                    {
                        _pips.Set(_afterK, _afterCounter);
                        PaintCloudPips(alpha);
                    }
                }

                float fill = Ease.Out((p - FallEnd) / (1f - FallEnd));
                if (_board != null && _dose >= 0 && _dose < Host.Capacity)
                {
                    _board.SetDose(_index, _dose, _color, fill, _tint);
                }

                // La salpicadura, en la superficie que sube (no dentro de la dosis ya formada).
                if (!_splashed && fill >= 0.7f)
                {
                    _splashed = true;
                    Splash(fill);
                }
            }
        }

        private void PoseWait(float p)
        {
            // La tormenta se carga (con un pulso más oscuro mientras la gota intenta caer).
            float dark = Mathf.Lerp(_startDark, 1f, Ease.Out(Mathf.Clamp01(p / WaitGrowEnd)));
            if (p > WaitGrowEnd && p < WaitTryEnd + 0.05f)
            {
                dark += StormPulse * Mathf.Sin(Mathf.PI * (p - WaitGrowEnd) / (WaitTryEnd + 0.05f - WaitGrowEnd));
            }

            PlaceCloud(0f, 1f, 1f);
            ShadeCloud(dark, 1f);

            // La gota (que ya colgaba, o que asoma) baja hasta la boca llena, no cabe y vuelve a colgar con un temblor.
            float y = _hangY;
            float sx = 1f;
            float sy = 1f;
            if (p < WaitGrowEnd)
            {
                if (!_hangAtStart)
                {
                    float g = Ease.OutBack(p / WaitGrowEnd);
                    sx = g;
                    sy = g;
                }
            }
            else if (p < WaitTryEnd)
            {
                float dip = Mathf.Sin(Mathf.PI * (p - WaitGrowEnd) / (WaitTryEnd - WaitGrowEnd));
                y -= 0.14f * dip;
                sy = 1f + (0.3f * dip);
                sx = 1f - (0.1f * dip);
            }
            else
            {
                float q = (p - WaitTryEnd) / (1f - WaitTryEnd);
                sy = 1f + (0.08f * Mathf.Sin(3f * Mathf.PI * q) * (1f - q));
            }

            OverlayParts.Place(_hang, 0f, y, _hangSize, _hangSize * ProceduralArt.LluviaDropAspect, _u);
            SetScale(_hang.rectTransform, sx, sy);
            OverlayParts.SetColor(_hang, new Color(1f, 1f, 1f, _on && !_inFlight ? 1f : 0f));
            if (!_pipsReset && p >= WaitPipsAt)
            {
                _pipsReset = true;
                _pips.Set(_afterK, _afterCounter); // la gota no cayó: el contador se queda en k − 1
                PaintCloudPips(1f);
            }
        }

        private void PlaceFall(float yU, float grow, float stretch, float alpha)
        {
            OverlayParts.Place(_fall, 0f, yU, FallDropU, FallDropU * ProceduralArt.LluviaDropAspect, _u);
            SetScale(_fall.rectTransform, grow, grow * stretch);
            OverlayParts.SetColor(_fall, new Color(1f, 1f, 1f, _on && !_inFlight ? alpha : 0f));
        }

        // Salpicadura a la altura fill de la dosis nueva. Los efectos van arriba a la izquierda del tablero, así que la posición
        // de la capa (centrada) se pasa a ese espacio con el tamaño que tenía la capa al leerla (al empezar el paso).
        private void Splash(float fill)
        {
            if (_fx == null || _reduceMotion)
            {
                return; // Reducir movimiento: el pool no emite (02 §4)
            }

            _fx.Splash(_surface + new Vector2(0f, fill * _u), _color, _u);
        }

        // Termina el paso: la dosis entera, la nube en reposo y, si cierra, el estado de después (la cola lo vuelve a pintar).
        private void FinishStep(bool paint)
        {
            if (!_animating)
            {
                return;
            }

            _animating = false;
            if (_board != null && _dose >= 0 && _dose < Host.Capacity)
            {
                _board.SetDose(_index, _dose, _color, 1f, _tint);
            }

            ICuiaBoard? board = _board;
            _board = null;
            _fx = null;
            _dose = -1;
            if (paint && board != null)
            {
                board.ShowContent(_index, _after, _afterSealed);
            }
            else
            {
                PlaceAtRest();
            }
        }

        private void SetRowAlpha(float alpha)
        {
            var c = new Color(1f, 1f, 1f, alpha);
            for (int i = 0; i < _drops.Length; i++)
            {
                OverlayParts.SetColor(_drops[i], c);
            }
        }

        private static Color StormColor(float dark, float alpha)
        {
            Color storm = ProceduralArt.LluviaStormTint;
            return new Color(
                Mathf.Max(0f, Mathf.LerpUnclamped(1f, storm.r, dark)),
                Mathf.Max(0f, Mathf.LerpUnclamped(1f, storm.g, dark)),
                Mathf.Max(0f, Mathf.LerpUnclamped(1f, storm.b, dark)),
                alpha);
        }

        private static void SetScale(Transform t, float x, float y)
        {
            var s = new Vector3(x, y, 1f);
            if (t.localScale != s)
            {
                t.localScale = s;
            }
        }
    }
}
