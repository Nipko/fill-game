#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// M4 Remolino (02 §9.1 M4; propuesta de D-007 b.2 M4) [P]: una espiral de espuma marfil translúcida sobre un remanso
    /// oscuro en la base de cada cuia de la pareja, en la ranura (no viaja con la cuia al verter), y un canal bajo el tablero
    /// que une las dos. Nunca turquesa: ni el turquesa del icono de Marea (#3BB8D9) ni Agua del Tapajós. Si la pareja tiene
    /// id &gt; 0, <c>id + 1</c> puntos bajo el remanso distinguen las parejas.
    /// </summary>
    /// <remarks>
    /// <para><b>Canal.</b> Lo dibuja una sola vez la cuia de menor índice de la pareja (la pareja se busca por
    /// <see cref="Cuia.MechanicPairId"/> en <see cref="ICuiaBoard.ShownCuia"/>, la misma consulta que
    /// <c>BoardDiff.RemolinoPartner</c>, sin construir un <c>BoardState</c>), en la capa trasera del tablero, y se recoloca en
    /// <see cref="OnBoardLayout"/> (también tras añadir una cuia). Trazado [P]: baja del pie de cada cuia hasta el canto
    /// inferior de su fila (la repisa) y se une con tramos horizontales bajo las repisas. Si la pareja está en filas distintas,
    /// en cada fila que cruza baja por el hueco entre dos cuias (el que acorta el camino; a igualdad, el más cercano: una
    /// escalera hacia la pareja), así que no pasa por detrás de ninguna cuia; si ningún hueco deja 1 dp de margen, por el que
    /// más deja. Los tramos horizontales van bajo la repisa, en su sombra, sin tocar su cara, y por encima del hueco que deja
    /// una cuia elevada de la fila de abajo. Los verticales (bajo el pie de la pareja y por los huecos) cruzan la cara de la
    /// repisa como una ranura de agua de <see cref="ChannelWidthU"/> u (como mucho <see cref="ChannelMaxDp"/> dp). Cada tramo
    /// es una cápsula (canto oscuro, agua y un filo de espuma), así que los codos son redondos.</para>
    /// <para><b>Recepción</b> (paso <see cref="MechanicStepKind.Receive"/>, despachado a la pareja; OtherIndex = la cuia
    /// tocada): <see cref="ReceiveMs"/> (500 ms [P b.2]) × <c>ctx.SpeedFactor</c>. La espuma recorre el canal desde la cuia
    /// tocada (donde desapareció el chorro), la espiral de la pareja gira y la racha sube por su fondo con burbujas
    /// (<see cref="IBoardFx.Bubbles"/>; con Reducir movimiento la capa de efectos no emite): las dosis nuevas crecen desde
    /// abajo y las que ya había suben con ellas, pintadas con <see cref="ICuiaBoard.SetDose"/> en el orden de
    /// <c>ctx.After</c>. Al empezar, la pareja ya queda en el tablero con su estado de después
    /// (<see cref="ICuiaBoard.ShownCuia"/> = <c>ctx.After</c>: salta al llegar el caudal, antes de que vuelva el origen, como
    /// en el vertido de T04) y los fotogramas se pintan encima. Al terminar, la cola pinta <c>ShowContent(ctx.After)</c>.</para>
    /// <para>Sin asignaciones tras <see cref="CuiaOverlay.Attach"/>: piezas, puntos del trazado y buffers fijos.</para>
    /// </remarks>
    public sealed class RemolinoOverlay : CuiaOverlay
    {
        /// <summary>
        /// Opacidad de la espuma marfil (<see cref="Palette.UiPaper"/>) sobre el remanso: 0,7 deja la espuma vista a ΔE00 10,7
        /// del tinte más cercano (Copoazú); con 0,8 bajaría de 10.
        /// </summary>
        public const float FoamAlpha = 0.7f;

        /// <summary>Recepción por el fondo de la pareja: 500 ms [P] (propuesta de D-007 b.2 M4).</summary>
        public const float ReceiveMs = 500f;

        /// <summary>Ancho del canal: 0,36 u [P], entre <see cref="ChannelMinDp"/> y <see cref="ChannelMaxDp"/>.</summary>
        public const float ChannelWidthU = 0.36f;

        /// <summary>Ancho mínimo del canal (dp).</summary>
        public const float ChannelMinDp = 5f;

        /// <summary>
        /// Ancho máximo del canal (dp): con el hueco de <see cref="ChannelGapDp"/> bajo la repisa queda a ≤ 8,5 dp de ella,
        /// dentro de los 9 dp que deja libres una cuia elevada de la fila de abajo (02 §5: elevación de 16 dp y hueco de 24) y
        /// del canto interior del marco bajo la última fila (margen del panel de 22 dp menos el marco de 13,5).
        /// </summary>
        public const float ChannelMaxDp = 8f;

        /// <summary>Hueco entre el canto inferior de la repisa y el canal (dp).</summary>
        public const float ChannelGapDp = 0.5f;

        /// <summary>Canto oscuro de cada lado del canal (dp).</summary>
        public const float ChannelRimDp = 1.25f;

        /// <summary>Puntos del trazado como mucho: dos por fila cruzada (3 filas) y los dos pies.</summary>
        public const int MaxChannelPoints = 8;

        /// <summary>Centro del remanso y de la espiral (u sobre el pie): un poco por debajo, para que se vean delante del pie.</summary>
        public const float PoolCenterU = -0.06f;

        /// <summary>Vueltas de la espiral durante la recepción [P].</summary>
        public const float SpinTurns = 1.5f;

        /// <summary>Fracción del paso en que la espuma llega al final del canal [P].</summary>
        public const float PulseEnd = 0.4f;

        /// <summary>Fracción del paso en que la racha empieza a entrar por el fondo de la pareja [P].</summary>
        public const float FillStart = 0.3f;

        /// <summary>Burbujas al entrar la racha (≤ 7 de las 44 del tablero entre las dos tandas) [P].</summary>
        public const int BubblesFirst = 4;

        /// <summary>Burbujas de la segunda tanda, a mitad de la subida [P].</summary>
        public const int BubblesSecond = 3;

        private const int MaxSegments = MaxChannelPoints - 1;
        private const int PulseBlobs = 4;
        private const int MaxBoard = 32;
        private const float RowTolerance = 1f;
        private const float ShineAlpha = 0.85f;
        private const float SecondBubbles = 0.62f;
        private const float OuterGapPenaltyDp = 2f;

        private readonly Image[] _rim = new Image[MaxSegments];
        private readonly Image[] _water = new Image[MaxSegments];
        private readonly Image[] _shine = new Image[MaxSegments];
        private readonly Image[] _pulse = new Image[PulseBlobs];
        private readonly Vector2[] _pts = new Vector2[MaxChannelPoints];
        private readonly Vector2[] _pulsePts = new Vector2[MaxChannelPoints];
        private readonly float[] _pulseAt = new float[MaxChannelPoints];
        private readonly float[] _rowX = new float[MaxBoard];

        private Image _pool = null!;
        private Image _spiral = null!;
        private int _spinFrame;
        private PipRow _dots = null!;
        private RectTransform _layerRoot = null!;
        private bool _on;
        private int _pairId;
        private int _partner = -1;
        private int _ptCount;
        private float _width;
        private bool _channelShown;
        private int _laidVersion = int.MinValue;
        private int _laidPartner = -2;
        private float _laidU = -1f;
        private int _pulseCount;
        private bool _pulseReverse;
        private bool _animating;
        private float _elapsed;
        private float _duration;
        private bool _finalPainted;
        private int _bubbles;
        private Cuia _stepAfter;
        private int _stepAmount;
        private int _stepTint;
        private ICuiaBoard? _stepBoard;
        private IBoardFx? _stepFx;
        private bool _stepReduce;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Remolino;

        /// <summary>Puntos de la pareja a la vista (0 con id 0).</summary>
        public int PairDots => _dots.Visible ? _dots.Count : 0;

        /// <summary>Color con el que se ve la espiral: la espuma compuesta en lineal sobre el remanso, como la GPU.</summary>
        public static Color SpiralSeenColor => ColorMath.CompositeLinear(Palette.UiPaper, FoamAlpha, ProceduralArt.WhirlPoolColor);

        /// <summary>La otra cuia de la pareja en el tablero mostrado, o −1.</summary>
        public int PartnerIndex => _partner;

        /// <summary>Esta cuia dibuja el canal: es la de menor índice de la pareja.</summary>
        public bool DrawsChannel => _on && _partner > IndexOrMinus;

        /// <summary>El canal de esta cuia está a la vista.</summary>
        public bool ChannelVisible => _channelShown;

        /// <summary>Ancho del canal (dp).</summary>
        public float ChannelWidthDp => _width;

        /// <summary>Puntos del trazado del canal (de esta cuia a su pareja), 0 si no lo dibuja.</summary>
        public int ChannelPointCount => _channelShown ? _ptCount : 0;

        /// <summary>Tramos del canal a la vista.</summary>
        public int ChannelSegmentCount => _channelShown ? Mathf.Max(0, _ptCount - 1) : 0;

        /// <summary>La recepción por el fondo está en curso.</summary>
        public override bool IsAnimating => _animating;

        /// <summary>Avance de la recepción en curso (0..1; 0 sin paso).</summary>
        public float ReceiveProgress => _animating && _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 0f;

        /// <summary>Giro actual de la espiral (grados; 0 en reposo).</summary>
        public float SpiralAngle => -_spinFrame * (360f / ProceduralArt.RemolinoSpiralFrames);

        /// <summary>Alguna gota de espuma recorre el canal.</summary>
        public bool PulseVisible
        {
            get
            {
                for (int i = 0; i < PulseBlobs; i++)
                {
                    if (_pulse[i] != null && _pulse[i].gameObject.activeSelf)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private int IndexOrMinus => AttachedHost != null ? AttachedHost.Index : int.MaxValue;

        // Esquina superior izquierda de la capa trasera en su espacio local (el de ICuiaBoard), donde está la raíz del canal.
        private Vector2 LayerOrigin => _layerRoot != null ? (Vector2)_layerRoot.localPosition : Vector2.zero;

        /// <summary>Punto <paramref name="i"/> del trazado, en el espacio de las capas de tablero.</summary>
        public Vector2 ChannelPoint(int i)
        {
            return _pts[Mathf.Clamp(i, 0, MaxChannelPoints - 1)] + LayerOrigin;
        }

        /// <summary>
        /// Rectángulo (espacio de las capas de tablero) del tramo <paramref name="i"/> del canal, con sus extremos redondos:
        /// lo que pinta su canto oscuro.
        /// </summary>
        public Rect ChannelSegmentRect(int i)
        {
            if ((uint)i >= (uint)MaxSegments || _rim[i] == null)
            {
                return Rect.zero;
            }

            RectTransform rt = _rim[i].rectTransform;
            Vector2 size = rt.sizeDelta;
            Vector2 center = rt.anchoredPosition + LayerOrigin;
            return new Rect(center - (0.5f * size), size);
        }

        /// <inheritdoc />
        public override void Show(Cuia cuia, bool sealedNow)
        {
            bool on = cuia.Mechanic == MechanicType.Remolino;
            Visible = on;
            _on = on;
            OverlayParts.SetVisible(_pool, on);
            OverlayParts.SetVisible(_spiral, on);
            int pair = on ? cuia.MechanicPairId : 0;
            _pairId = pair;
            int dots = pair > 0 ? pair + 1 : 0;
            _dots.Set(dots, dots);
            RefreshChannel(false);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            OverlayParts.Place(_pool, 0f, PoolCenterU, 2.6f, 0.85f, u);
            OverlayParts.Place(_spiral, 0f, PoolCenterU, ProceduralArt.RemolinoSpiralWidthU, ProceduralArt.RemolinoSpiralHeightU, u);
            _dots.Layout(0f, -0.62f, 0.13f, 0.2f, u);
        }

        /// <inheritdoc />
        public override void OnBoardLayout()
        {
            RefreshChannel(true);
        }

        /// <inheritdoc />
        public override float StepDurationMs(MechanicStepKind kind)
        {
            return kind == MechanicStepKind.Receive ? ReceiveMs : 0f;
        }

        /// <inheritdoc />
        public override float PlayStep(in MechanicStepContext ctx)
        {
            if (ctx.Kind != MechanicStepKind.Receive)
            {
                return 0f;
            }

            float ms = ReceiveMs * ctx.SpeedFactor;
            if (ms <= 0f)
            {
                return 0f;
            }

            _stepBoard = ctx.Board ?? Host.Board;
            _stepFx = ctx.Fx;
            _stepReduce = ctx.ReduceMotion;
            _stepAfter = ctx.After;
            _stepAmount = Mathf.Max(0, ctx.After.Count - ctx.Before.Count);
            _stepTint = ctx.Tint;
            _duration = ms;
            _elapsed = 0f;
            _bubbles = 0;
            _finalPainted = false;
            _animating = true;
            PreparePulse(ctx.OtherIndex);
            _layerRoot.SetAsLastSibling(); // la espuma, sobre el canal que dibuja la otra cuia de la pareja

            // La pareja llega al estado de después al llegar el caudal (ShownCuia, como el salto de T04; mientras anima,
            // ShowContent no llama a Show) y cada fotograma pinta encima, con SetDose, cómo sube la racha por el fondo.
            _stepBoard?.ShowContent(Host.Index, ctx.After, ctx.AfterSealed);
            PaintReceive(0f);
            return ms;
        }

        /// <inheritdoc />
        public override void Tick(float dtMs)
        {
            if (!_animating || _finalPainted)
            {
                return;
            }

            _elapsed += dtMs;
            float t = Mathf.Clamp01(_elapsed / _duration);
            PaintReceive(t);
            if (t >= 1f)
            {
                _finalPainted = true; // la cola cierra con SnapStep y ShowContent(After)
            }
        }

        /// <inheritdoc />
        public override void SnapStep()
        {
            if (!_animating)
            {
                return;
            }

            if (!_finalPainted)
            {
                PaintReceive(1f);
            }

            _animating = false;
            _finalPainted = false;
            _stepBoard = null;
            _stepFx = null;
            HidePulse();
            SetSpin(0);
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.WhirlPoolColor); // remanso
            into.Add(SpiralSeenColor); // espiral de espuma
            into.Add(ProceduralArt.Pearl); // puntos de la pareja y espuma que recorre el canal
            into.Add(ProceduralArt.RemolinoChannelWater); // agua del canal
            into.Add(ProceduralArt.RemolinoChannelRim); // canto del canal (≤ 2 dp, pero también lejos de los tintes)
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _pool = OverlayParts.NewImage("RemolinoPool", Host.SlotBack, Host.Art.WhirlPool, ProceduralArt.WhirlPoolColor);
            _spiral = OverlayParts.NewImage("RemolinoSpiral", Host.SlotBack, Host.Art.RemolinoSpiralFrame(0), OverlayParts.Fade(Palette.UiPaper, FoamAlpha));
            _dots = new PipRow("RemolinoPair", Host.SlotFront, Host.Art.CounterPip, Host.Art.CounterPipEmpty, ProceduralArt.Pearl);

            // Canal y espuma en la capa trasera del tablero, en una raíz propia anclada a la esquina superior izquierda de la
            // capa, como las ranuras de las cuias: si el tablero cambia de tamaño sin recolocarse (el CanvasScaler al cambiar
            // de pantalla), canal y cuias se mueven juntos. Las posiciones del tablero (ICuiaBoard, relativas al pivote de la
            // capa) se pasan a la raíz restando LayerOrigin. La raíz se va con la cuia (LayerLink).
            RectTransform layer = Host.BoardBackLayer ?? Host.SlotBack;
            var go = new GameObject("RemolinoChannel" + Host.Index, typeof(RectTransform));
            go.transform.SetParent(layer, false);
            _layerRoot = (RectTransform)go.transform;
            _layerRoot.anchorMin = new Vector2(0f, 1f);
            _layerRoot.anchorMax = new Vector2(0f, 1f);
            _layerRoot.pivot = new Vector2(0.5f, 0.5f);
            _layerRoot.sizeDelta = Vector2.zero;
            _layerRoot.anchoredPosition = Vector2.zero;
            go.AddComponent<LayerLink>().Owner = Host.SlotBack;

            Sprite capsule = Host.Art.RemolinoCapsule;
            NewStroke(_rim, "ChannelRim", capsule, ProceduralArt.RemolinoChannelRim);
            NewStroke(_water, "ChannelWater", capsule, ProceduralArt.RemolinoChannelWater);
            NewStroke(_shine, "ChannelShine", capsule, OverlayParts.Fade(Palette.UiPaper, ShineAlpha));
            for (int i = 0; i < PulseBlobs; i++)
            {
                _pulse[i] = OverlayParts.NewImage("ChannelFoam" + i, _layerRoot, Host.Art.CounterPip, ProceduralArt.Pearl);
                PreRegister(_pulse[i]);
            }
        }

        /// <summary>
        /// Registra en su Canvas, oculta, una pieza que solo se ve durante un paso: UGUI guarda en el registro los gráficos que
        /// se ocultan, así que mostrarla a mitad del paso no hace crecer sus colecciones (sin asignaciones; ver
        /// <c>GraphicPool</c>). Sin efecto si la cuia aún no está en una jerarquía activa.
        /// </summary>
        private static void PreRegister(Component piece)
        {
            GameObject go = piece.gameObject;
            Transform? parent = go.transform.parent;
            if (go.activeSelf || parent == null || !parent.gameObject.activeInHierarchy)
            {
                return;
            }

            go.SetActive(true);
            go.SetActive(false);
        }

        // Pareja y canal: la pareja se busca en lo que muestra el tablero; el canal se recalcula si cambia la pareja, el
        // tablero se recolocó (LayoutVersion) o cambió u (force: siempre, tras cada recolocación).
        private void RefreshChannel(bool force)
        {
            ICuiaBoard? board = Host.Board;
            int partner = _on && board != null ? FindPartner(board) : -1;
            _partner = partner;
            if (board == null || partner < 0 || partner < Host.Index)
            {
                HideChannel();
                _laidPartner = partner;
                return;
            }

            if (_layerRoot.anchoredPosition != Vector2.zero)
            {
                _layerRoot.anchoredPosition = Vector2.zero;
            }

            float u = Host.U;
            int version = board.LayoutVersion;
            if (!force && _channelShown && partner == _laidPartner && version == _laidVersion && u == _laidU)
            {
                return;
            }

            _laidPartner = partner;
            _laidVersion = version;
            _laidU = u;
            _width = Mathf.Clamp(ChannelWidthU * u, ChannelMinDp, ChannelMaxDp);
            _ptCount = Route(board, Host.Index, partner, _width, _pts);
            ToRoot(_pts, _ptCount);
            PlaceStrokes(_pts, _ptCount, _width);
            _channelShown = true;
        }

        private int FindPartner(ICuiaBoard board)
        {
            int self = Host.Index;
            int n = Mathf.Min(board.Count, MaxBoard);
            for (int j = 0; j < n; j++)
            {
                if (j == self)
                {
                    continue;
                }

                Cuia other = board.ShownCuia(j);
                if (other.Mechanic == MechanicType.Remolino && other.MechanicPairId == _pairId)
                {
                    return j;
                }
            }

            return -1;
        }

        // Trazado del canal de la cuia a a la b (ver la clase) en pts; devuelve los puntos. Va de la fila de arriba a la de
        // abajo y, si a está debajo, se invierte al final: el mismo trazado para las dos cuias de la pareja.
        private int Route(ICuiaBoard board, int a, int b, float width, Vector2[] pts)
        {
            Vector2 fa = board.Foot(a);
            Vector2 fb = board.Foot(b);
            bool swap = fb.y > fa.y + RowTolerance;
            int top = swap ? b : a;
            Vector2 ft = swap ? fb : fa;
            Vector2 fl = swap ? fa : fb;
            float half = 0.5f * width;
            float y = board.SlotRect(top).yMin - ChannelGapDp - half;
            int n = 0;
            pts[n++] = ft;
            pts[n++] = new Vector2(ft.x, y);
            float x = ft.x;
            float rowY = ft.y;
            while (n < MaxChannelPoints - 3)
            {
                if (!TryNextRow(board, rowY, fl.y, out float nextRow, out float underside))
                {
                    break;
                }

                float gx = GapColumn(board, nextRow, x, fl.x, half);
                if (Mathf.Abs(gx - x) > 0.01f)
                {
                    pts[n++] = new Vector2(gx, y); // bajo la repisa de la fila de arriba, hasta el hueco
                }

                y = underside - ChannelGapDp - half;
                pts[n++] = new Vector2(gx, y); // por el hueco de la fila, hasta bajo su repisa
                x = gx;
                rowY = nextRow;
            }

            if (Mathf.Abs(fl.x - x) > 0.01f)
            {
                pts[n++] = new Vector2(fl.x, y);
            }

            pts[n++] = fl;
            if (swap)
            {
                for (int i = 0, j = n - 1; i < j; i++, j--)
                {
                    Vector2 tmp = pts[i];
                    pts[i] = pts[j];
                    pts[j] = tmp;
                }
            }

            return n;
        }

        // Fila siguiente bajo la de pie rowY sin pasar de la de pie lastY: su línea de pie y el canto inferior de su repisa.
        private static bool TryNextRow(ICuiaBoard board, float rowY, float lastY, out float next, out float underside)
        {
            next = float.NegativeInfinity;
            underside = 0f;
            bool found = false;
            int n = Mathf.Min(board.Count, MaxBoard);
            for (int c = 0; c < n; c++)
            {
                float fy = board.Foot(c).y;
                if (fy < rowY - RowTolerance && fy >= lastY - RowTolerance && fy > next)
                {
                    next = fy;
                    underside = board.SlotRect(c).yMin;
                    found = true;
                }
            }

            return found;
        }

        // Columna por la que el canal cruza la fila de pie rowY: el hueco entre dos cuias vecinas (o junto a una punta de una
        // fila más corta que la más ancha) que deja el canal a ≥ 1 dp de toda cuia de la fila y que hace más corto el camino
        // de from a to. Si ninguno deja ese margen, el de más margen. Sin asignaciones.
        private float GapColumn(ICuiaBoard board, float rowY, float from, float to, float half)
        {
            int n = Mathf.Min(board.Count, MaxBoard);
            int count = 0;
            float slot = 0f;
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            for (int c = 0; c < n; c++)
            {
                Rect r = board.SlotRect(c);
                minX = Mathf.Min(minX, r.xMin);
                maxX = Mathf.Max(maxX, r.xMax);
                Vector2 f = board.Foot(c);
                if (Mathf.Abs(f.y - rowY) <= RowTolerance)
                {
                    _rowX[count++] = f.x;
                    slot = r.width;
                }
            }

            // Orden por inserción (≤ 16 cuias por fila).
            for (int i = 1; i < count; i++)
            {
                float v = _rowX[i];
                int j = i - 1;
                while (j >= 0 && _rowX[j] > v)
                {
                    _rowX[j + 1] = _rowX[j];
                    j--;
                }

                _rowX[j + 1] = v;
            }

            if (count == 0)
            {
                return to;
            }

            float bodyHalf = CuiaGeometry.BodyOuterHalfWidthU * Host.U;
            float best = to;
            float bestCost = float.PositiveInfinity;
            float bestFrom = float.PositiveInfinity;
            float fallback = to;
            float fallbackClear = float.NegativeInfinity;
            for (int k = -1; k < count; k++)
            {
                float gx;
                if (k < 0)
                {
                    gx = _rowX[0] - (0.5f * slot); // junto a la punta izquierda
                }
                else if (k == count - 1)
                {
                    gx = _rowX[count - 1] + (0.5f * slot); // junto a la punta derecha
                }
                else
                {
                    gx = 0.5f * (_rowX[k] + _rowX[k + 1]);
                }

                bool outer = k < 0 || k == count - 1;
                if (outer && (gx < minX + (0.5f * slot) - 0.5f || gx > maxX - (0.5f * slot) + 0.5f))
                {
                    continue; // la punta de la fila más ancha da al marco del panel
                }

                float clear = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    clear = Mathf.Min(clear, Mathf.Abs(gx - _rowX[i]) - bodyHalf - half);
                }

                if (clear > fallbackClear)
                {
                    fallbackClear = clear;
                    fallback = gx;
                }

                if (clear < 1f)
                {
                    continue;
                }

                // El camino más corto; a igualdad, el hueco más cerca de donde viene el canal (escalera hacia la pareja, sin
                // rodeos) y, antes que la punta de una fila, un hueco entre cuias.
                float near = Mathf.Abs(gx - from);
                float cost = near + Mathf.Abs(to - gx) + (outer ? OuterGapPenaltyDp : 0f);
                if (cost < bestCost - 0.01f || (cost < bestCost + 0.01f && near < bestFrom - 0.01f))
                {
                    bestCost = cost;
                    bestFrom = near;
                    best = gx;
                }
            }

            return float.IsPositiveInfinity(bestCost) ? fallback : best;
        }

        // Pasa los puntos del espacio de la capa (ICuiaBoard) al de la raíz del canal.
        private void ToRoot(Vector2[] pts, int count)
        {
            Vector2 origin = LayerOrigin;
            for (int i = 0; i < count; i++)
            {
                pts[i] -= origin;
            }
        }

        // Coloca las cápsulas de los tramos (canto, agua y filo de espuma) y oculta las que sobran.
        private void PlaceStrokes(Vector2[] pts, int count, float width)
        {
            float water = Mathf.Max(1f, width - (2f * ChannelRimDp));
            float shine = Mathf.Min(1.8f, 0.34f * water);
            float shineOffset = (0.5f * water) - (0.5f * shine) - 0.4f;
            for (int i = 0; i < MaxSegments; i++)
            {
                bool on = i < count - 1 && (pts[i + 1] - pts[i]).sqrMagnitude > 1e-4f;
                OverlayParts.SetVisible(_rim[i], on);
                OverlayParts.SetVisible(_water[i], on);
                OverlayParts.SetVisible(_shine[i], on);
                if (!on)
                {
                    continue;
                }

                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                bool horizontal = Mathf.Abs(b.x - a.x) >= Mathf.Abs(b.y - a.y);
                Vector2 mid = 0.5f * (a + b);
                float length = horizontal ? Mathf.Abs(b.x - a.x) : Mathf.Abs(b.y - a.y);
                PlaceCapsule(_rim[i], mid, length, width, horizontal);
                PlaceCapsule(_water[i], mid, length, water, horizontal);
                Vector2 lit = horizontal ? new Vector2(0f, shineOffset) : new Vector2(-shineOffset, 0f); // luz de arriba a la izquierda
                PlaceCapsule(_shine[i], mid + lit, Mathf.Max(0f, length - (0.5f * width)), shine, horizontal);
            }
        }

        // Cápsula de grosor thick centrada en mid con largo length entre los centros de sus extremos redondos.
        private static void PlaceCapsule(Image image, Vector2 mid, float length, float thick, bool horizontal)
        {
            float w = horizontal ? length + thick : thick;
            float h = horizontal ? thick : length + thick;
            OverlayParts.Place(image, mid.x, mid.y, w, h, 1f);
            float multiplier = ProceduralArt.RemolinoCapsuleRadiusPx / Mathf.Max(0.25f, 0.5f * thick);
            if (!Mathf.Approximately(image.pixelsPerUnitMultiplier, multiplier))
            {
                image.pixelsPerUnitMultiplier = multiplier;
            }
        }

        private void HideChannel()
        {
            if (!_channelShown)
            {
                return;
            }

            _channelShown = false;
            for (int i = 0; i < MaxSegments; i++)
            {
                OverlayParts.SetVisible(_rim[i], false);
                OverlayParts.SetVisible(_water[i], false);
                OverlayParts.SetVisible(_shine[i], false);
            }
        }

        private void NewStroke(Image[] into, string name, Sprite capsule, Color color)
        {
            for (int i = 0; i < into.Length; i++)
            {
                Image image = OverlayParts.NewImage(name + i, _layerRoot, capsule, color);
                image.type = Image.Type.Sliced;
                into[i] = image;
            }
        }

        // Trazado de la espuma: el canal de la pareja (siempre de la de menor índice a la de mayor), recorrido desde la cuia
        // tocada hasta esta.
        private void PreparePulse(int touched)
        {
            ICuiaBoard? board = _stepBoard;
            _pulseCount = 0;
            if (board == null || touched < 0 || touched >= board.Count)
            {
                return;
            }

            int self = Host.Index;
            int a = Mathf.Min(self, touched);
            int b = Mathf.Max(self, touched);
            float width = Mathf.Clamp(ChannelWidthU * Host.U, ChannelMinDp, ChannelMaxDp);
            _pulseCount = Route(board, a, b, width, _pulsePts);
            ToRoot(_pulsePts, _pulseCount);
            _pulseReverse = self == a; // el trazado va de a a b; la espuma, de la tocada a esta
            _pulseAt[0] = 0f;
            for (int i = 1; i < _pulseCount; i++)
            {
                _pulseAt[i] = _pulseAt[i - 1] + Vector2.Distance(_pulsePts[i - 1], _pulsePts[i]);
            }

            // Espuma algo más ancha que el canal: asoma sobre el panel y se ve de un vistazo.
            float blob = 1.35f * width;
            for (int i = 0; i < PulseBlobs; i++)
            {
                float side = blob * (1f - (0.15f * i));
                RectTransform rt = _pulse[i].rectTransform;
                rt.sizeDelta = new Vector2(side, side);
            }
        }

        // Un fotograma de la recepción en t (0..1): espuma por el canal, giro de la espiral, la racha por el fondo y burbujas.
        private void PaintReceive(float t)
        {
            PaintPulse(t);
            SetSpin(Mathf.FloorToInt(SpinTurns * ProceduralArt.RemolinoSpiralFrames * Ease.Out(t)));
            float rise = Mathf.Clamp01((t - FillStart) / (1f - FillStart));
            float p = _stepAmount * Ease.Out(rise);
            ICuiaBoard? board = _stepBoard;
            if (board != null)
            {
                int self = Host.Index;
                Cuia after = _stepAfter;
                int capacity = Host.Capacity;
                for (int k = 0; k < capacity; k++)
                {
                    if (k >= after.Count)
                    {
                        board.SetDose(self, k, Color.clear, 0f, 0);
                        continue;
                    }

                    int tint = k < _stepAmount ? _stepTint : after.ColorAt(k);
                    float fill = k < _stepAmount ? Mathf.Clamp01(p - k) : 1f;
                    if (tint == Cuia.MudColor)
                    {
                        board.SetDose(self, k, Palette.Mud, fill, 0);
                    }
                    else
                    {
                        board.SetDose(self, k, Palette.Tint(tint), fill, tint);
                    }
                }
            }

            EmitBubbles(t, p);
        }

        private void EmitBubbles(float t, float level)
        {
            IBoardFx? fx = _stepFx;
            if (fx == null || _stepReduce || fx.ReduceMotion)
            {
                return;
            }

            float u = Host.U;
            var root = (RectTransform)Host.SlotBack.parent;
            Vector2 foot = root.anchoredPosition; // espacio del contenedor del tablero: el de la capa de efectos
            if (_bubbles == 0 && t >= FillStart)
            {
                _bubbles = 1;
                fx.Bubbles(foot + new Vector2(0f, (CuiaGeometry.CavityBottomU + 0.15f) * u), BubblesFirst, u);
            }

            if (_bubbles == 1 && t >= SecondBubbles)
            {
                _bubbles = 2;
                fx.Bubbles(foot + new Vector2(0f, (CuiaGeometry.CavityBottomU + (0.5f * level)) * u), BubblesSecond, u);
            }
        }

        private void PaintPulse(float t)
        {
            if (_pulseCount < 2 || t >= PulseEnd)
            {
                HidePulse();
                return;
            }

            float length = _pulseAt[_pulseCount - 1];
            float head = Ease.InOut(t / PulseEnd) * length;
            float spacing = 1.1f * Mathf.Clamp(ChannelWidthU * Host.U, ChannelMinDp, ChannelMaxDp);
            for (int i = 0; i < PulseBlobs; i++)
            {
                float s = head - (i * spacing);
                bool on = s >= 0f && s <= length;
                OverlayParts.SetVisible(_pulse[i], on);
                if (!on)
                {
                    continue;
                }

                Vector2 at = PointAt(_pulseReverse ? length - s : s);
                RectTransform rt = _pulse[i].rectTransform;
                if (rt.anchoredPosition != at)
                {
                    rt.anchoredPosition = at;
                }
            }
        }

        // Punto del trazado de la espuma a la distancia s de su primer punto.
        private Vector2 PointAt(float s)
        {
            for (int i = 1; i < _pulseCount; i++)
            {
                if (s <= _pulseAt[i] || i == _pulseCount - 1)
                {
                    float seg = Mathf.Max(1e-4f, _pulseAt[i] - _pulseAt[i - 1]);
                    return Vector2.Lerp(_pulsePts[i - 1], _pulsePts[i], Mathf.Clamp01((s - _pulseAt[i - 1]) / seg));
                }
            }

            return _pulseCount > 0 ? _pulsePts[0] : Vector2.zero;
        }

        private void HidePulse()
        {
            for (int i = 0; i < PulseBlobs; i++)
            {
                if (_pulse[i] != null)
                {
                    OverlayParts.SetVisible(_pulse[i], false);
                }
            }
        }

        // Giro de la espiral: la fase frame (30° cada una, en el sentido de las agujas del reloj); 0 en reposo.
        private void SetSpin(int frame)
        {
            if (frame == _spinFrame)
            {
                return;
            }

            _spinFrame = frame;
            _spiral.sprite = Host.Art.RemolinoSpiralFrame(frame);
        }

        /// <summary>
        /// Une la raíz del canal (en la capa del tablero, fuera de la cuia) a la vida de la cuia: cuando la cuia se destruye
        /// (cambio de nivel), el canal se va con ella en el fotograma siguiente, antes de pintarse.
        /// </summary>
        private sealed class LayerLink : MonoBehaviour
        {
            public Transform? Owner;

            private void LateUpdate()
            {
                if (Owner == null)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
