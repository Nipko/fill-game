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
    /// oscuro en la base de cada cuia de la pareja, en la ranura (no viaja con la cuia al verter), y un canal de agua que sale
    /// de los dos remansos y los une por debajo del tablero. Nunca turquesa: ni el turquesa del icono de Marea (#3BB8D9) ni
    /// Agua del Tapajós.
    /// </summary>
    /// <remarks>
    /// <para><b>Ficha de la pareja</b> (opción C del dueño, 2026-10-11: el canal y la MISMA marca en las dos cuias, también
    /// con un solo par): un disco del agua del remanso con aro de espuma y contorno de pizarra
    /// (<see cref="ProceduralArt.RemolinoPairTag"/>) clavado en el centro del pie de fibra, sobre el ojo de la espiral, con
    /// <c>id + 1</c> puntos de espuma (1 con id 0); con más puntos se alarga en píldora. Las dos cuias de una pareja llevan la
    /// misma ficha y dos parejas, fichas distintas. Va en la ranura (<c>SlotFront</c>), así que no viaja con la cuia ni cambia
    /// al seleccionar (D-002): con la cuia elevada queda sobre el remanso. Colores a ΔE00 ≥ 10 de los 14 tintes y del
    /// turquesa de Marea (<see cref="CollectSurfaceColors"/>).</para>
    /// <para><b>Canal.</b> Lo dibuja una sola vez la cuia de menor índice de la pareja (la pareja se busca por
    /// <see cref="Cuia.MechanicPairId"/> en <see cref="ICuiaBoard.ShownCuia"/>, la misma consulta que
    /// <c>BoardDiff.RemolinoPartner</c>, sin construir un <c>BoardState</c>), en la capa trasera del tablero, y se recoloca en
    /// <see cref="OnBoardLayout"/> (también tras añadir una cuia). Trazado [P] (T16: «baja del pie de cada cuia hasta el borde
    /// inferior de su fila»): de cada remanso baja por el frente de su repisa (la <b>bajada</b>, en la columna del remanso)
    /// hasta el canto inferior, y lo unen tramos horizontales pegados por debajo a las repisas. Si la pareja está en filas
    /// distintas, en cada fila que cruza baja por el hueco entre dos cuias (el que acorta el camino; a igualdad, el más
    /// cercano: una escalera hacia la pareja), así que no pasa por detrás de ninguna cuia; si ningún hueco deja 1 dp de
    /// margen, por el que más deja. Cada tramo es una cápsula, así que los codos son redondos y no dejan costuras.</para>
    /// <para><b>La repisa va delante del canal</b> (direccion-visual-v2 §4.5: repisa continua por fila), salvo en las bajadas,
    /// que son lo único del canal sobre una repisa: así se ve que el canal sale del remanso. La capa trasera se pinta sobre
    /// las repisas, así que donde un tramo cruza la repisa de otra fila lo tapa una franja de su madera
    /// (<see cref="ProceduralArt.RemolinoShelfCover"/>, los mismos píxeles de la repisa): el tramo se corta a ras de la cara
    /// superior y vuelve a asomar a ras del canto inferior, sin extremos redondos sueltos ni codos a la vista detrás de la
    /// madera. Las franjas siguen a los pies, como el canal: durante el relayout de «+1 cuia» (300 ms) GameScreen pone las
    /// repisas en su sitio final al empezar mientras las cuias se deslizan, así que si el ancho de la cuia cambia (10 → 11 o
    /// 15 → 16 cuias) las franjas van con las cuias, desfasadas de su repisa, hasta que el relayout termina.</para>
    /// <para><b>Medidas.</b> La repisa empieza en pie + <see cref="ShelfTopAboveFootDp"/> (GameScreen) y su madera mide 20 dp
    /// (ProceduralArt.BuildShelf: cara superior, bisel, frente y un canto inferior de 2 dp), así que acaba en pie −
    /// <see cref="ShelfBottomBelowFootDp"/>, 2 dp por debajo de <see cref="ICuiaBoard.SlotRect"/>. Los tramos horizontales miden
    /// <see cref="ChannelWidthDp"/> dp y tocan ese canto: bajo la última fila quedan 2 dp hasta el canto interior del marco
    /// (15 dp de repisa y 22 de margen del panel menos 13,5 de marco: 23,5 dp bajo el pie) y entre filas, 2,5 dp hasta una cuia
    /// elevada de la fila de abajo (02 §5: elevación de 16 dp y hueco de 24).</para>
    /// <para><b>Agua</b> (D-003): un lecho de <see cref="ProceduralArt.RemolinoChannelEdge"/> y encima un centro con degradado
    /// transversal hasta <see cref="ProceduralArt.RemolinoChannelWater"/>, con una línea de espuma discontinua en el eje
    /// (<see cref="DashThickDp"/> dp, 6 dp de trazo y 6 de hueco) fuera de las bajadas.</para>
    /// <para><b>Recepción</b> (paso <see cref="MechanicStepKind.Receive"/>, despachado a la pareja; OtherIndex = la cuia
    /// tocada): <see cref="ReceiveMs"/> (500 ms [P b.2]) × <c>ctx.SpeedFactor</c>. La racha viaja con su tinte por el canal: sale
    /// del remanso de la cuia tocada (donde desapareció el chorro), baja por su bajada, pasa por debajo (y por detrás) de las
    /// repisas y sube por la bajada de la pareja hasta su remanso. Es la propia fruta en tránsito, no una superficie fija: como
    /// la fruta llave de la Liana, no entra en la regla de ΔE00 (direccion-visual-v2 §2.4). Después la espiral de la pareja
    /// gira y la racha sube por su fondo con burbujas (<see cref="IBoardFx.Bubbles"/>; con Reducir movimiento la capa de
    /// efectos no emite): las dosis nuevas crecen desde abajo y las que ya había suben con ellas, pintadas con
    /// <see cref="ICuiaBoard.SetDose"/> en el orden de <c>ctx.After</c>. Al empezar, la pareja ya queda en el tablero con su
    /// estado de después (<see cref="ICuiaBoard.ShownCuia"/> = <c>ctx.After</c>: salta al llegar el caudal, antes de que vuelva
    /// el origen, como en el vertido de T04) y los fotogramas se pintan encima. Al terminar, la cola pinta
    /// <c>ShowContent(ctx.After)</c>. La racha va en la raíz de la cuia que recibe; si no es la que dibuja el canal, su raíz
    /// pasa al final de la capa y lleva sus propias franjas de madera, idénticas, para que la racha también pase por detrás de
    /// las repisas.</para>
    /// <para>Sin asignaciones tras <see cref="CuiaOverlay.Attach"/>: piezas, puntos del trazado y buffers fijos; los sprites
    /// se piden en <see cref="OnAttach"/>.</para>
    /// </remarks>
    public sealed class RemolinoOverlay : CuiaOverlay
    {
        /// <summary>
        /// Opacidad de la espuma marfil (<see cref="Palette.UiPaper"/>) de la espiral sobre el remanso: 0,7 deja la espuma
        /// vista a ΔE00 10,7 del tinte más cercano (Copoazú); con 0,8 bajaría de 10.
        /// </summary>
        public const float FoamAlpha = 0.7f;

        /// <summary>Recepción por el fondo de la pareja: 500 ms [P] (propuesta de D-007 b.2 M4).</summary>
        public const float ReceiveMs = 500f;

        /// <summary>
        /// Ancho del canal (dp) [P], el mismo en todos los tramos: bajo la última fila caben 6,5 dp entre el canto inferior
        /// de la repisa y el canto interior del marco, menos 2 dp de aire hasta el marco.
        /// </summary>
        public const float ChannelWidthDp = 4.5f;

        /// <summary>Cara superior de la repisa sobre la línea de pie (dp; la repisa de <c>GameScreen</c> empieza en pie + 3).</summary>
        public const float ShelfTopAboveFootDp = 3f;

        /// <summary>
        /// Canto inferior de la madera de la repisa bajo la línea de pie (dp): 20 dp de madera desde pie + 3
        /// (<c>ProceduralArt.BuildShelf</c>); debajo empieza su sombra de 6 dp.
        /// </summary>
        public const float ShelfBottomBelowFootDp = 17f;

        /// <summary>
        /// Arranque de la bajada bajo la línea de pie (dp): bajo el remanso, que la tapa, y con su extremo redondo 0,5 dp por
        /// debajo del pie, fuera de la cuia.
        /// </summary>
        public const float DropTopBelowFootDp = (0.5f * ChannelWidthDp) + 0.5f;

        /// <summary>Grosor de la línea de espuma discontinua del eje del canal (dp).</summary>
        public const float DashThickDp = 1f;

        /// <summary>Opacidad de la línea de espuma del eje (<see cref="Palette.UiPaper"/>).</summary>
        public const float DashAlpha = 0.5f;

        /// <summary>Ancho de la racha que viaja por el canal (dp): por dentro del agua, que deja 0,75 dp de lecho a cada lado.</summary>
        public const float RachaWidthDp = ChannelWidthDp - 1.5f;

        /// <summary>Largo de la racha que viaja por el canal (dp) [P]; como mucho el 60 % del trazado.</summary>
        public const float RachaLengthDp = 60f;

        /// <summary>Puntos del trazado como mucho: dos por fila cruzada (3 filas) y los dos pies.</summary>
        public const int MaxChannelPoints = 8;

        /// <summary>
        /// Centro del remanso y de la espiral (u sobre el pie): por debajo, para que se vea ≈ 60 % de la espiral bajo el pie sin
        /// pasar del frente de la repisa.
        /// </summary>
        public const float PoolCenterU = -0.10f;

        /// <summary>Vueltas de la espiral durante la recepción [P].</summary>
        public const float SpinTurns = 1.5f;

        /// <summary>Fracción del paso en que la cola de la racha entra en el remanso de la pareja [P].</summary>
        public const float PulseEnd = 0.5f;

        /// <summary>Fracción del paso en que la racha empieza a entrar por el fondo de la pareja (llega su cabeza) [P].</summary>
        public const float FillStart = 0.4f;

        /// <summary>Burbujas al entrar la racha (≤ 7 de las 44 del tablero entre las dos tandas) [P].</summary>
        public const int BubblesFirst = 4;

        /// <summary>Burbujas de la segunda tanda, a mitad de la subida [P].</summary>
        public const int BubblesSecond = 3;

        /// <summary>
        /// Alto de la ficha de la pareja (u) [P]: 15 dp con la cuia de 48 dp y 12 dp con la de 39 dp (3 filas de 6); con un
        /// punto es un disco.
        /// </summary>
        public const float PairTagHeightU = 0.62f;

        /// <summary>
        /// Centro de la ficha de la pareja sobre el pie (u) [P]: en el pie de fibra (0-0,30 u), sin llegar al fondo de la
        /// cavidad (0,40 u en el eje) ni a la bajada del canal (bajo el pie).
        /// </summary>
        public const float PairTagCenterU = 0.085f;

        /// <summary>Lado de cada punto de la ficha (u): 6,2 dp con la cuia de 48 dp.</summary>
        public const float PairDotU = 0.26f;

        /// <summary>Paso entre los centros de los puntos de la ficha (u); con muchos puntos se juntan.</summary>
        public const float PairDotStepU = 0.3f;

        /// <summary>Ancho máximo de la ficha (u): el del pie de fibra (1,60 u).</summary>
        public const float PairTagMaxWidthU = 2f * CuiaGeometry.FootHalfWidthU;

        /// <summary>Puntos de la ficha como mucho: <c>id + 1</c> con el id de 3 bits (0..7).</summary>
        public const int MaxPairDots = Cuia.MaxMechanicField + 1;

        private const int MaxSegments = MaxChannelPoints - 1;
        private const int MaxRows = 4;
        private const int MaxBoard = 32;
        private const float RowTolerance = 1f;
        private const float SecondBubbles = 0.7f;
        private const float OuterGapPenaltyDp = 2f;
        private const float CoverPadDp = 1f;
        private const float MinRachaDp = 0.25f;

        // Lo que un cruce por la punta de una fila corta deja hasta el borde de las ranuras (dp): la repisa empieza ≤ 15,5 dp
        // dentro (marco de 13,5 + 6 + 8 − 12) y sus 16 dp de punta, con la hoja tallada, no son la madera lisa que copia la
        // franja (más la mitad de la franja).
        private const float ShelfEndClearDp = 35f;

        private readonly Image[] _bed = new Image[MaxSegments];
        private readonly Image[] _core = new Image[MaxSegments];
        private readonly Image[] _dash = new Image[MaxSegments];
        private readonly Image[] _racha = new Image[MaxSegments];
        private readonly Image[] _cover = new Image[MaxRows];
        private readonly Vector2[] _pts = new Vector2[MaxChannelPoints];
        private readonly Vector2[] _pieceA = new Vector2[MaxSegments];
        private readonly Vector2[] _pieceB = new Vector2[MaxSegments];
        private readonly bool[] _pieceDrop = new bool[MaxSegments];
        private readonly Vector2[] _coverAt = new Vector2[MaxRows];
        private readonly float[] _rowFoot = new float[MaxRows];
        private readonly Vector2[] _pulsePts = new Vector2[MaxChannelPoints];
        private readonly float[] _pulseAt = new float[MaxChannelPoints];
        private readonly float[] _rowX = new float[MaxBoard];
        private readonly Image[] _pairDots = new Image[MaxPairDots];

        private Image _pool = null!;
        private Image _spiral = null!;
        private int _spinFrame;
        private Image _pairTag = null!;
        private int _pairDotCount = -1;
        private float _pairU = -1f;
        private RectTransform _layerRoot = null!;
        private bool _on;
        private int _pairId;
        private int _partner = -1;
        private int _ptCount;
        private int _pieceCount;
        private int _coverCount;
        private int _rowCount;
        private bool _channelShown;
        private int _laidVersion = int.MinValue;
        private int _laidPartner = -2;
        private float _laidU = -1f;
        private int _pulseCount;
        private bool _pulseReverse;
        private float _rachaHead = -1f;
        private bool _animating;
        private float _elapsed;
        private float _duration;
        private bool _finalPainted;
        private int _bubbles;
        private Cuia _stepAfter;
        private int _stepAmount;
        private int _stepTint;
        private Color _rachaColor;
        private ICuiaBoard? _stepBoard;
        private IBoardFx? _stepFx;
        private bool _stepReduce;
        private CuiaView? _view;

        /// <inheritdoc />
        public override MechanicType Type => MechanicType.Remolino;

        /// <summary>Puntos de la ficha de la pareja a la vista: <c>id + 1</c> (1 con id 0); 0 sin ficha.</summary>
        public int PairDots => PairMarkVisible ? _pairDotCount : 0;

        /// <summary>La ficha de la pareja está a la vista (siempre que la cuia tenga el Remolino, también con id 0).</summary>
        public bool PairMarkVisible => _pairTag != null && _pairTag.gameObject.activeSelf;

        /// <summary>
        /// Rectángulo de la ficha de la pareja en el espacio de las anclas de la cuia (dp, origen en el centro del pie y hacia
        /// arriba).
        /// </summary>
        public Rect PairMarkRect
        {
            get
            {
                RectTransform rt = _pairTag.rectTransform;
                Vector2 size = rt.sizeDelta;
                return new Rect(rt.anchoredPosition - (0.5f * size), size);
            }
        }

        /// <summary>Rectángulo del punto <paramref name="i"/> de la ficha (dp, como <see cref="PairMarkRect"/>).</summary>
        public Rect PairDotRect(int i)
        {
            RectTransform rt = _pairDots[Mathf.Clamp(i, 0, MaxPairDots - 1)].rectTransform;
            Vector2 size = rt.sizeDelta;
            return new Rect(rt.anchoredPosition - (0.5f * size), size);
        }

        /// <summary>Color con el que se ve la espiral: la espuma compuesta en lineal sobre el remanso, como la GPU.</summary>
        public static Color SpiralSeenColor => ColorMath.CompositeLinear(Palette.UiPaper, FoamAlpha, ProceduralArt.WhirlPoolColor);

        /// <summary>Color con el que se ve la línea de espuma del eje del canal.</summary>
        public static Color DashSeenColor => ColorMath.CompositeLinear(Palette.UiPaper, DashAlpha, ProceduralArt.RemolinoChannelWater);

        /// <summary>La otra cuia de la pareja en el tablero mostrado, o −1.</summary>
        public int PartnerIndex => _partner;

        /// <summary>Esta cuia dibuja el canal: es la de menor índice de la pareja.</summary>
        public bool DrawsChannel => _on && _partner > IndexOrMinus;

        /// <summary>El canal de esta cuia está a la vista.</summary>
        public bool ChannelVisible => _channelShown;

        /// <summary>Puntos del trazado del canal (de esta cuia a su pareja), 0 si no lo dibuja.</summary>
        public int ChannelPointCount => _channelShown ? _ptCount : 0;

        /// <summary>Tramos del canal (uno por tramo del trazado, de esta cuia a su pareja), 0 si no lo dibuja.</summary>
        public int ChannelSegmentCount => _channelShown ? _pieceCount : 0;

        /// <summary>Franjas de madera a la vista que tapan el canal donde cruza una repisa (las de esta cuia).</summary>
        public int CoverCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxRows; i++)
                {
                    if (_cover[i] != null && _cover[i].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>La recepción por el fondo está en curso.</summary>
        public override bool IsAnimating => _animating;

        /// <summary>Avance de la recepción en curso (0..1; 0 sin paso).</summary>
        public float ReceiveProgress => _animating && _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 0f;

        /// <summary>Giro actual de la espiral (grados; 0 en reposo).</summary>
        public float SpiralAngle => -_spinFrame * (360f / ProceduralArt.RemolinoSpiralFrames);

        /// <summary>La racha viaja por el canal a la vista.</summary>
        public bool PulseVisible => RachaPieceCount > 0;

        /// <summary>Trozos a la vista de la racha que viaja por el canal (uno por tramo que ocupa).</summary>
        public int RachaPieceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxSegments; i++)
                {
                    if (_racha[i] != null && _racha[i].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Color de la racha que viaja por el canal: el tinte de lo vertido.</summary>
        public Color RachaColor => _rachaColor;

        /// <summary>
        /// Avance de la cabeza de la racha por el trazado, desde el pie de la cuia tocada (0) hasta el de la pareja (1); −1 sin
        /// racha.
        /// </summary>
        public float RachaHeadProgress
        {
            get
            {
                if (_rachaHead < 0f || _pulseCount < 2)
                {
                    return -1f;
                }

                float length = _pulseAt[_pulseCount - 1];
                return length > 0f ? Mathf.Clamp01(_rachaHead / length) : -1f;
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
        /// Rectángulo (espacio de las capas de tablero) del tramo <paramref name="i"/> del canal, con sus extremos redondos: lo
        /// que pinta su lecho.
        /// </summary>
        public Rect ChannelSegmentRect(int i)
        {
            if ((uint)i >= (uint)MaxSegments || _bed[i] == null)
            {
                return Rect.zero;
            }

            return RectOf(_bed[i].rectTransform);
        }

        /// <summary>El tramo <paramref name="i"/> es una bajada: el que sale del remanso por el frente de su repisa.</summary>
        public bool ChannelSegmentIsDrop(int i)
        {
            return (uint)i < (uint)_pieceCount && _pieceDrop[i];
        }

        /// <summary>Rectángulo (espacio de las capas de tablero) de la franja de madera <paramref name="i"/>.</summary>
        public Rect CoverRect(int i)
        {
            if ((uint)i >= (uint)MaxRows || _cover[i] == null)
            {
                return Rect.zero;
            }

            return RectOf(_cover[i].rectTransform);
        }

        /// <summary>Rectángulo (espacio de las capas de tablero) del trozo <paramref name="i"/> de la racha.</summary>
        public Rect RachaRect(int i)
        {
            if ((uint)i >= (uint)MaxSegments || _racha[i] == null || !_racha[i].gameObject.activeSelf)
            {
                return Rect.zero;
            }

            return RectOf(_racha[i].rectTransform);
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
            SetPairMark(on ? Mathf.Min(pair + 1, MaxPairDots) : 0, Host.U);
            RefreshChannel(false);
        }

        /// <inheritdoc />
        public override void Layout(float u, float heightU)
        {
            OverlayParts.Place(_pool, 0f, PoolCenterU, 2.6f, 0.85f, u);
            OverlayParts.Place(_spiral, 0f, PoolCenterU, ProceduralArt.RemolinoSpiralWidthU, ProceduralArt.RemolinoSpiralHeightU, u);
            SetPairMark(_pairDotCount, u);
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
            _rachaColor = ctx.Tint == Cuia.MudColor ? Palette.Mud : Palette.Tint(ctx.Tint);
            for (int i = 0; i < MaxSegments; i++)
            {
                OverlayParts.SetColor(_racha[i], _rachaColor);
            }

            _duration = ms;
            _elapsed = 0f;
            _bubbles = 0;
            _finalPainted = false;
            _animating = true;
            PreparePulse(ctx.OtherIndex);
            _layerRoot.SetAsLastSibling(); // la racha, sobre el canal que dibuja la otra cuia de la pareja

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
            HideRacha();
            SetSpin(0);

            // Las franjas de esta cuia solo siguen si dibuja el canal (las de la racha de la otra cuia ya no hacen falta).
            if (_channelShown)
            {
                ShowCovers(_pts, _ptCount, true);
            }
            else
            {
                HideCovers();
            }
        }

        /// <inheritdoc />
        public override void CollectSurfaceColors(List<Color> into)
        {
            into.Add(ProceduralArt.WhirlPoolColor); // remanso y agua de la ficha de la pareja
            into.Add(SpiralSeenColor); // espiral de espuma
            into.Add(ProceduralArt.Pearl); // aro y puntos de la ficha de la pareja
            into.Add(ProceduralArt.Slate); // contorno de la ficha de la pareja
            into.Add(ProceduralArt.RemolinoChannelWater); // centro del agua del canal
            into.Add(ProceduralArt.RemolinoChannelEdge); // bordes del agua del canal
            into.Add(DashSeenColor); // línea de espuma del eje (1 dp, pero también lejos de los tintes)
        }

        /// <inheritdoc />
        protected override void OnAttach()
        {
            _pool = OverlayParts.NewImage("RemolinoPool", Host.SlotBack, Host.Art.WhirlPool, ProceduralArt.WhirlPoolColor);
            _spiral = OverlayParts.NewImage("RemolinoSpiral", Host.SlotBack, Host.Art.RemolinoSpiralFrame(0), OverlayParts.Fade(Palette.UiPaper, FoamAlpha));
            _pairTag = OverlayParts.NewImage("RemolinoPairTag", Host.SlotFront, Host.Art.RemolinoPairTag, Color.white);
            _pairTag.type = Image.Type.Sliced;
            for (int i = 0; i < _pairDots.Length; i++)
            {
                _pairDots[i] = OverlayParts.NewImage("RemolinoPairDot" + i, Host.SlotFront, Host.Art.CounterPip, ProceduralArt.Pearl);
            }

            // Solo para pintar mate el Barro que reciba (puede no haberla). Con includeInactive, como MurkyMarks: si la cuia se
            // monta con su jerarquía inactiva (una pantalla oculta), sin él la búsqueda daría null y el Barro perdería el mate sin
            // avisar.
            _view = Host.SlotBack.GetComponentInParent<CuiaView>(true);

            // Canal y racha en la capa trasera del tablero, en una raíz propia anclada a la esquina superior izquierda de la
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

            // Orden: todos los lechos, todos los centros y todas las líneas de espuma (los codos no dejan costuras), la racha
            // y, delante, las franjas de madera que tapan el canal y la racha donde pasan por detrás de una repisa.
            ProceduralArt art = Host.Art;
            NewPieces(_bed, "ChannelBed", art.RemolinoCapsule, ProceduralArt.RemolinoChannelEdge, Image.Type.Sliced);
            NewPieces(_core, "ChannelWater", art.RemolinoCapsuleSoft, ProceduralArt.RemolinoChannelWater, Image.Type.Sliced);
            NewPieces(_dash, "ChannelFoamLine", art.RemolinoDash, OverlayParts.Fade(Palette.UiPaper, DashAlpha), Image.Type.Tiled);
            NewPieces(_racha, "ChannelRacha", art.RemolinoCapsule, Color.white, Image.Type.Sliced);
            for (int i = 0; i < _racha.Length; i++)
            {
                PreRegister(_racha[i]);
            }

            Sprite cover = art.RemolinoShelfCover;
            for (int i = 0; i < _cover.Length; i++)
            {
                _cover[i] = OverlayParts.NewImage("ChannelShelfCover" + i, _layerRoot, cover, Color.white);
                PreRegister(_cover[i]); // la de la racha de la otra cuia se ve a mitad de un paso
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

        private Rect RectOf(RectTransform rt)
        {
            Vector2 size = rt.sizeDelta;
            if (Mathf.Abs(rt.localEulerAngles.z - 90f) < 1f)
            {
                size = new Vector2(size.y, size.x);
            }

            Vector2 center = rt.anchoredPosition + LayerOrigin;
            return new Rect(center - (0.5f * size), size);
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
            _ptCount = Route(board, Host.Index, partner, _pts);
            ToRoot(_pts, _ptCount, LayerOrigin);
            BuildPieces();
            PlacePieces();
            ShowCovers(_pts, _ptCount, true);
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
        // abajo y, si a está debajo, se invierte al final: el mismo trazado para las dos cuias de la pareja. Deja en _rowFoot
        // la línea de pie de cada fila del tablero.
        private int Route(ICuiaBoard board, int a, int b, Vector2[] pts)
        {
            CollectRows(board);
            Vector2 fa = board.Foot(a);
            Vector2 fb = board.Foot(b);
            bool swap = fb.y > fa.y + RowTolerance;
            Vector2 ft = swap ? fb : fa;
            Vector2 fl = swap ? fa : fb;
            const float half = 0.5f * ChannelWidthDp;
            float y = UnderShelf(ft.y);
            int n = 0;
            pts[n++] = ft;
            pts[n++] = new Vector2(ft.x, y);
            float x = ft.x;
            float rowY = ft.y;
            while (n < MaxChannelPoints - 3)
            {
                if (!TryNextRow(board, rowY, fl.y, out float nextRow))
                {
                    break;
                }

                float gx = GapColumn(board, nextRow, x, fl.x, half);
                if (Mathf.Abs(gx - x) > 0.01f)
                {
                    pts[n++] = new Vector2(gx, y); // bajo la repisa de la fila de arriba, hasta el hueco
                }

                y = UnderShelf(nextRow);
                pts[n++] = new Vector2(gx, y); // por el hueco de la fila, por detrás de su repisa, hasta debajo de ella
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

        // Eje de un tramo horizontal pegado por debajo a la repisa de la fila de pie footY.
        private static float UnderShelf(float footY)
        {
            return footY - ShelfBottomBelowFootDp - (0.5f * ChannelWidthDp);
        }

        // Línea de pie de cada fila del tablero (como mucho MaxRows), sin asignaciones.
        private void CollectRows(ICuiaBoard board)
        {
            _rowCount = 0;
            int n = Mathf.Min(board.Count, MaxBoard);
            for (int c = 0; c < n; c++)
            {
                float fy = board.Foot(c).y;
                bool known = false;
                for (int k = 0; k < _rowCount; k++)
                {
                    if (Mathf.Abs(_rowFoot[k] - fy) <= RowTolerance)
                    {
                        known = true;
                        break;
                    }
                }

                if (!known && _rowCount < MaxRows)
                {
                    _rowFoot[_rowCount++] = fy;
                }
            }
        }

        // Fila siguiente bajo la de pie rowY sin pasar de la de pie lastY: su línea de pie.
        private static bool TryNextRow(ICuiaBoard board, float rowY, float lastY, out float next)
        {
            next = float.NegativeInfinity;
            bool found = false;
            int n = Mathf.Min(board.Count, MaxBoard);
            for (int c = 0; c < n; c++)
            {
                float fy = board.Foot(c).y;
                if (fy < rowY - RowTolerance && fy >= lastY - RowTolerance && fy > next)
                {
                    next = fy;
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

                if (outer && (gx < minX + ShelfEndClearDp || gx > maxX - ShelfEndClearDp))
                {
                    continue; // la punta tallada de la repisa (hoja y esquina redonda): la franja de madera no la copia
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
        private static void ToRoot(Vector2[] pts, int count, Vector2 origin)
        {
            for (int i = 0; i < count; i++)
            {
                pts[i] -= origin;
            }
        }

        // Tramos del trazado (espacio de la raíz): uno por tramo; el primero y el último son las bajadas, que arrancan bajo el
        // remanso (DropTopBelowFootDp bajo el pie) y no en el pie, para que su extremo redondo no entre en la cuia.
        private void BuildPieces()
        {
            _pieceCount = 0;
            for (int i = 0; i < _ptCount - 1 && _pieceCount < MaxSegments; i++)
            {
                Vector2 p = _pts[i];
                Vector2 q = _pts[i + 1];
                bool drop = i == 0 || i == _ptCount - 2;
                if (i == 0)
                {
                    p.y -= DropTopBelowFootDp;
                }

                if (i == _ptCount - 2)
                {
                    q.y -= DropTopBelowFootDp;
                }

                if ((q - p).sqrMagnitude < 1e-4f)
                {
                    continue;
                }

                _pieceA[_pieceCount] = p;
                _pieceB[_pieceCount] = q;
                _pieceDrop[_pieceCount] = drop;
                _pieceCount++;
            }
        }

        // Coloca lechos, centros y líneas de espuma de los tramos, y oculta lo que sobra.
        private void PlacePieces()
        {
            const float width = ChannelWidthDp;
            for (int i = 0; i < MaxSegments; i++)
            {
                bool on = i < _pieceCount;
                OverlayParts.SetVisible(_bed[i], on);
                OverlayParts.SetVisible(_core[i], on);
                if (!on)
                {
                    OverlayParts.SetVisible(_dash[i], false);
                    continue;
                }

                Vector2 a = _pieceA[i];
                Vector2 b = _pieceB[i];
                bool horizontal = Mathf.Abs(b.x - a.x) >= Mathf.Abs(b.y - a.y);
                Vector2 mid = 0.5f * (a + b);
                float length = horizontal ? Mathf.Abs(b.x - a.x) : Mathf.Abs(b.y - a.y);
                PlaceCapsule(_bed[i], mid, length, width, horizontal);
                PlaceCapsule(_core[i], mid, length, width, horizontal);

                // Línea de espuma en el eje, sin entrar en los extremos redondos ni en las bajadas.
                float dash = length - width;
                bool showDash = dash > 1f && !_pieceDrop[i];
                OverlayParts.SetVisible(_dash[i], showDash);
                if (showDash)
                {
                    RectTransform rt = _dash[i].rectTransform;
                    var size = new Vector2(dash, DashThickDp);
                    if (rt.sizeDelta != size)
                    {
                        rt.sizeDelta = size;
                    }

                    if (rt.anchoredPosition != mid)
                    {
                        rt.anchoredPosition = mid;
                    }

                    SetRotation(rt, horizontal ? 0f : 90f);
                }
            }
        }

        // Franjas de madera sobre los cruces del trazado pts (espacio de la raíz) con las repisas: cada tramo vertical que no es
        // una bajada, en cada fila cuya repisa atraviesa; la bajada solo pasa por delante de la repisa de su propia fila.
        private void ShowCovers(Vector2[] pts, int count, bool show)
        {
            _coverCount = 0;
            float originY = LayerOrigin.y;
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 p = pts[i];
                Vector2 q = pts[i + 1];
                if (Mathf.Abs(q.x - p.x) > 0.01f || Mathf.Abs(q.y - p.y) < 0.01f)
                {
                    continue; // horizontal o vacío
                }

                bool drop = i == 0 || i == count - 2;
                float footEnd = i == 0 ? p.y : q.y; // el pie de la bajada
                float lo = Mathf.Min(p.y, q.y);
                float hi = Mathf.Max(p.y, q.y);
                for (int r = 0; r < _rowCount; r++)
                {
                    float foot = _rowFoot[r] - originY;
                    float bandLo = foot - ShelfBottomBelowFootDp;
                    float bandHi = foot + ShelfTopAboveFootDp;
                    if (hi <= bandLo + 0.01f || lo >= bandHi - 0.01f)
                    {
                        continue;
                    }

                    if (drop && Mathf.Abs(footEnd - foot) <= RowTolerance)
                    {
                        continue; // la bajada, por delante de su repisa
                    }

                    if (_coverCount < MaxRows)
                    {
                        _coverAt[_coverCount++] = new Vector2(p.x, 0.5f * (bandLo + bandHi));
                    }
                }
            }

            const float coverW = ChannelWidthDp + (2f * CoverPadDp);
            const float coverH = ShelfTopAboveFootDp + ShelfBottomBelowFootDp;
            for (int i = 0; i < MaxRows; i++)
            {
                bool on = show && i < _coverCount;
                OverlayParts.SetVisible(_cover[i], on);
                if (on)
                {
                    OverlayParts.Place(_cover[i], _coverAt[i].x, _coverAt[i].y, coverW, coverH, 1f);
                }
            }
        }

        private void HideCovers()
        {
            for (int i = 0; i < MaxRows; i++)
            {
                OverlayParts.SetVisible(_cover[i], false);
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

        private static void SetRotation(RectTransform rt, float degrees)
        {
            Quaternion q = Quaternion.Euler(0f, 0f, degrees);
            if (rt.localRotation != q)
            {
                rt.localRotation = q;
            }
        }

        private void HideChannel()
        {
            if (!_animating)
            {
                HideCovers(); // durante la recepción, las franjas tapan la racha de esta cuia
            }

            if (!_channelShown)
            {
                return;
            }

            _channelShown = false;
            for (int i = 0; i < MaxSegments; i++)
            {
                OverlayParts.SetVisible(_bed[i], false);
                OverlayParts.SetVisible(_core[i], false);
                OverlayParts.SetVisible(_dash[i], false);
            }
        }

        private void NewPieces(Image[] into, string name, Sprite sprite, Color color, Image.Type type)
        {
            for (int i = 0; i < into.Length; i++)
            {
                Image image = OverlayParts.NewImage(name + i, _layerRoot, sprite, color);
                image.type = type;
                into[i] = image;
            }
        }

        // Trazado de la racha: el canal de la pareja (siempre de la de menor índice a la de mayor), recorrido desde la cuia
        // tocada hasta esta, con sus franjas de madera (las mismas que las del canal) delante.
        private void PreparePulse(int touched)
        {
            ICuiaBoard? board = _stepBoard;
            _pulseCount = 0;
            _rachaHead = -1f;
            if (board == null || touched < 0 || touched >= board.Count)
            {
                return;
            }

            int self = Host.Index;
            int a = Mathf.Min(self, touched);
            int b = Mathf.Max(self, touched);
            _pulseCount = Route(board, a, b, _pulsePts);
            ToRoot(_pulsePts, _pulseCount, LayerOrigin);
            ShowCovers(_pulsePts, _pulseCount, true);
            _pulseReverse = self == a; // el trazado va de a a b; la racha, de la tocada a esta
            _pulseAt[0] = 0f;
            for (int i = 1; i < _pulseCount; i++)
            {
                _pulseAt[i] = _pulseAt[i - 1] + Vector2.Distance(_pulsePts[i - 1], _pulsePts[i]);
            }
        }

        // Un fotograma de la recepción en t (0..1): la racha por el canal, giro de la espiral, la racha por el fondo y burbujas.
        private void PaintReceive(float t)
        {
            PaintRacha(t);
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
                        // Rama defensiva (LevelValidation no deja Barro con mecánica): mate, como lo pinta MudMarks.PaintMud
                        // (T20). SetDose lleva la cuenta de la vista y SetMatteDose le quita el brillo.
                        board.SetDose(self, k, Palette.Mud, fill, 0);
                        if (_view != null)
                        {
                            _view.Liquid.SetMatteDose(k, Palette.Mud, fill);
                        }
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

        // La racha: un tramo de RachaLengthDp del tinte vertido que recorre el trazado desde la cuia tocada hasta esta, con
        // un trozo por tramo del trazado que ocupa (cápsulas, así que los codos son redondos). Entra y sale bajo los
        // remansos, que la tapan, y las franjas de madera la tapan donde pasa por detrás de una repisa.
        private void PaintRacha(float t)
        {
            if (_pulseCount < 2 || t >= PulseEnd)
            {
                HideRacha();
                return;
            }

            // Sinusoidal (pico de velocidad 1,57 veces la media, como la vuelta del vertido): con la cúbica, a mitad del canal
            // la racha avanzaría más que su largo en un fotograma y se vería a saltos.
            float length = _pulseAt[_pulseCount - 1];
            float band = Mathf.Min(RachaLengthDp, 0.6f * length);
            float head = Ease.InOutSine(t / PulseEnd) * (length + band);
            _rachaHead = Mathf.Min(head, length);
            float tail = head - band;

            // En el sentido del trazado (de la de menor índice a la de mayor): al revés si la tocada es la de mayor índice. Sin
            // salir de las bajadas, que arrancan DropTopBelowFootDp bajo cada pie.
            float lo = Mathf.Max(DropTopBelowFootDp, _pulseReverse ? length - head : tail);
            float hi = Mathf.Min(length - DropTopBelowFootDp, _pulseReverse ? length - tail : head);
            for (int i = 1; i < MaxChannelPoints; i++)
            {
                Image piece = _racha[i - 1];
                bool on = i < _pulseCount;
                float s0 = 0f;
                float s1 = 0f;
                if (on)
                {
                    s0 = Mathf.Max(lo, _pulseAt[i - 1]);
                    s1 = Mathf.Min(hi, _pulseAt[i]);
                    on = s1 - s0 > MinRachaDp;
                }

                OverlayParts.SetVisible(piece, on);
                if (!on)
                {
                    continue;
                }

                Vector2 p0 = _pulsePts[i - 1];
                Vector2 p1 = _pulsePts[i];
                float len = Mathf.Max(1e-4f, _pulseAt[i] - _pulseAt[i - 1]);
                Vector2 a = Vector2.Lerp(p0, p1, (s0 - _pulseAt[i - 1]) / len);
                Vector2 b = Vector2.Lerp(p0, p1, (s1 - _pulseAt[i - 1]) / len);
                bool horizontal = Mathf.Abs(p1.x - p0.x) >= Mathf.Abs(p1.y - p0.y);
                PlaceCapsule(piece, 0.5f * (a + b), s1 - s0, RachaWidthDp, horizontal);
            }
        }

        private void HideRacha()
        {
            _rachaHead = -1f;
            for (int i = 0; i < MaxSegments; i++)
            {
                if (_racha[i] != null)
                {
                    OverlayParts.SetVisible(_racha[i], false);
                }
            }
        }

        // Ficha de la pareja con count puntos (0 o menos: oculta) a la escala u (dp por u). Solo escribe si cambia el número de
        // puntos o u (sin asignaciones): una píldora 9-slice cuyo borde es la mitad de su alto, con los puntos centrados.
        private void SetPairMark(int count, float u)
        {
            if (_pairTag == null || (count == _pairDotCount && Mathf.Approximately(u, _pairU)))
            {
                return;
            }

            _pairDotCount = count;
            _pairU = u;
            bool on = count > 0 && u > 0f;
            OverlayParts.SetVisible(_pairTag, on);
            for (int i = 0; i < _pairDots.Length; i++)
            {
                OverlayParts.SetVisible(_pairDots[i], on && i < count);
            }

            if (!on)
            {
                return;
            }

            float step = PairDotStepU;
            if (count > 1)
            {
                step = Mathf.Min(step, (PairTagMaxWidthU - PairTagHeightU) / (count - 1));
            }

            float dot = Mathf.Min(PairDotU, 0.8f * step);
            float width = PairTagHeightU + ((count - 1) * step);
            OverlayParts.Place(_pairTag, 0f, PairTagCenterU, width, PairTagHeightU, u);
            float multiplier = ProceduralArt.RemolinoPairTagRadiusPx / Mathf.Max(0.25f, 0.5f * PairTagHeightU * u);
            if (!Mathf.Approximately(_pairTag.pixelsPerUnitMultiplier, multiplier))
            {
                _pairTag.pixelsPerUnitMultiplier = multiplier;
            }

            float start = -0.5f * step * (count - 1);
            for (int i = 0; i < count && i < _pairDots.Length; i++)
            {
                OverlayParts.Place(_pairDots[i], start + (i * step), PairTagCenterU, dot, dot, u);
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
