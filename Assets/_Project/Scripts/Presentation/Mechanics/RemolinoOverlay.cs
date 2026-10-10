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
    /// oscuro en la base de cada cuia de la pareja, en la ranura (no viaja con la cuia al verter), y un canal de agua bajo el
    /// tablero que une las dos. Nunca turquesa: ni el turquesa del icono de Marea (#3BB8D9) ni Agua del Tapajós. Si la pareja
    /// tiene id &gt; 0, <c>id + 1</c> puntos junto a la boca del canal distinguen las parejas.
    /// </summary>
    /// <remarks>
    /// <para><b>Canal.</b> Lo dibuja una sola vez la cuia de menor índice de la pareja (la pareja se busca por
    /// <see cref="Cuia.MechanicPairId"/> en <see cref="ICuiaBoard.ShownCuia"/>, la misma consulta que
    /// <c>BoardDiff.RemolinoPartner</c>, sin construir un <c>BoardState</c>), en la capa trasera del tablero, y se recoloca en
    /// <see cref="OnBoardLayout"/> (también tras añadir una cuia). Trazado [P]: baja del pie de cada cuia hasta el canto
    /// inferior de su fila (la repisa) y se une con tramos horizontales bajo las repisas, en su sombra. Si la pareja está en
    /// filas distintas, en cada fila que cruza baja por el hueco entre dos cuias (el que acorta el camino; a igualdad, el más
    /// cercano: una escalera hacia la pareja), así que no pasa por detrás de ninguna cuia; si ningún hueco deja 1 dp de
    /// margen, por el que más deja.</para>
    /// <para><b>La repisa va delante del canal</b> (direccion-visual-v2 §4.5: repisa continua por fila). La capa trasera se
    /// pinta sobre las repisas, así que el canal no se dibuja en la franja de ninguna repisa: los tramos verticales se cortan
    /// desde <see cref="ShelfClearDp"/> dp sobre su cara superior (pie + <see cref="ShelfTopAboveFootDp"/>) hasta donde
    /// asoman bajo ella, con extremos redondos, y el canal se lee como agua que pasa por debajo. Bajo el remanso de cada cuia
    /// de la pareja, el canal sale de una boca elíptica (<see cref="MouthWidthDp"/> × <see cref="MouthHeightDp"/> dp) en el
    /// canto inferior de la repisa. Los tramos horizontales van <see cref="ChannelGapDp"/> dp bajo el canto de la repisa
    /// (pie − 15) y miden <see cref="ChannelWidthDp"/> dp, así que bajo la última fila quedan ≥ 2 dp por dentro del canto
    /// interior del marco (margen del panel de 22 dp menos el marco de 13,5) y entre filas, a ≥ 2 dp de una cuia elevada de
    /// la fila de abajo (02 §5: elevación de 16 dp y hueco de 24).</para>
    /// <para><b>Agua</b> (D-003): un lecho de <see cref="ProceduralArt.RemolinoChannelEdge"/> y encima un centro con degradado
    /// transversal hasta <see cref="ProceduralArt.RemolinoChannelWater"/>, con una línea de espuma discontinua en el eje
    /// (<see cref="DashThickDp"/> dp, 6 dp de trazo y 6 de hueco). Cada tramo es una cápsula, así que los codos son redondos.</para>
    /// <para><b>Recepción</b> (paso <see cref="MechanicStepKind.Receive"/>, despachado a la pareja; OtherIndex = la cuia
    /// tocada): <see cref="ReceiveMs"/> (500 ms [P b.2]) × <c>ctx.SpeedFactor</c>. La espuma (cápsulas suaves translúcidas
    /// del ancho del agua) recorre el canal desde la cuia tocada (donde desapareció el chorro), pasando por detrás de las
    /// repisas; la espiral de la pareja gira y la racha sube por su fondo con burbujas (<see cref="IBoardFx.Bubbles"/>; con
    /// Reducir movimiento la capa de efectos no emite): las dosis nuevas crecen desde abajo y las que ya había suben con
    /// ellas, pintadas con <see cref="ICuiaBoard.SetDose"/> en el orden de <c>ctx.After</c>. Al empezar, la pareja ya queda en
    /// el tablero con su estado de después (<see cref="ICuiaBoard.ShownCuia"/> = <c>ctx.After</c>: salta al llegar el caudal,
    /// antes de que vuelva el origen, como en el vertido de T04) y los fotogramas se pintan encima. Al terminar, la cola pinta
    /// <c>ShowContent(ctx.After)</c>.</para>
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
        /// Ancho del canal (dp) [P], el mismo en todos los tramos: bajo la última fila caben 8,5 dp entre el canto de la
        /// repisa y el del marco, menos <see cref="ChannelGapDp"/> y 2 dp de aire hasta el marco.
        /// </summary>
        public const float ChannelWidthDp = 5f;

        /// <summary>Hueco entre el canto inferior de la repisa (pie − 15) y el canal (dp).</summary>
        public const float ChannelGapDp = 1.5f;

        /// <summary>Cara superior de la repisa sobre la línea de pie (dp; la repisa de <c>GameScreen</c> empieza en pie + 3).</summary>
        public const float ShelfTopAboveFootDp = 3f;

        /// <summary>Aire (dp) entre el extremo redondo de un tramo vertical cortado y la cara superior de la repisa.</summary>
        public const float ShelfClearDp = 1f;

        /// <summary>Ancho de la boca por la que el canal sale de debajo de la repisa de la pareja (dp).</summary>
        public const float MouthWidthDp = 8f;

        /// <summary>Alto de la boca (dp).</summary>
        public const float MouthHeightDp = 3f;

        /// <summary>Opacidad de la boca (<see cref="ProceduralArt.RemolinoChannelRim"/>).</summary>
        public const float MouthAlpha = 0.8f;

        /// <summary>Centro de la boca bajo el canto de <c>SlotRect</c> (pie − 15): en el canto de madera de 2 dp de la repisa.</summary>
        public const float MouthDropDp = 1f;

        /// <summary>Grosor de la línea de espuma discontinua del eje del canal (dp).</summary>
        public const float DashThickDp = 1f;

        /// <summary>Opacidad de la línea de espuma del eje (<see cref="Palette.UiPaper"/>).</summary>
        public const float DashAlpha = 0.5f;

        /// <summary>
        /// Opacidad de la espuma que recorre el canal al recibir (<see cref="Palette.UiPaper"/>): 0,5 la deja a ΔE00 10,6 de
        /// Guanábana sobre el centro del agua; con 0,55 bajaría a 9,8.
        /// </summary>
        public const float FoamBlobAlpha = 0.5f;

        /// <summary>Ancho de cada gota de espuma (dp): menos que el agua, nunca toca el marco ni las repisas.</summary>
        public const float FoamBlobWidthDp = 4.5f;

        /// <summary>Alargamiento de las gotas de espuma en la dirección del tramo.</summary>
        public const float FoamBlobAspect = 1.8f;

        /// <summary>Puntos del trazado como mucho: dos por fila cruzada (3 filas) y los dos pies.</summary>
        public const int MaxChannelPoints = 8;

        /// <summary>
        /// Centro del remanso y de la espiral (u sobre el pie): por debajo, para que se vea ≈ 60 % de la espiral bajo el pie sin
        /// pasar del frente de la repisa.
        /// </summary>
        public const float PoolCenterU = -0.10f;

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
        private const int MaxPieces = MaxSegments + 2;
        private const int MaxRows = 4;
        private const int PulseBlobs = 4;
        private const int MaxBoard = 32;
        private const float RowTolerance = 1f;
        private const float MinPieceDp = 0.5f;
        private const float SecondBubbles = 0.62f;
        private const float OuterGapPenaltyDp = 2f;
        private const float PulseFade = 0.15f;

        private readonly Image[] _bed = new Image[MaxPieces];
        private readonly Image[] _core = new Image[MaxPieces];
        private readonly Image[] _dash = new Image[MaxPieces];
        private readonly Image[] _mouth = new Image[2];
        private readonly Image[] _pulse = new Image[PulseBlobs];
        private readonly Vector2[] _pts = new Vector2[MaxChannelPoints];
        private readonly Vector2[] _pieceA = new Vector2[MaxPieces];
        private readonly Vector2[] _pieceB = new Vector2[MaxPieces];
        private readonly Vector2[] _mouthAt = new Vector2[2];
        private readonly float[] _bandLo = new float[MaxRows];
        private readonly float[] _bandHi = new float[MaxRows];
        private readonly float[] _pulseLo = new float[MaxRows];
        private readonly float[] _pulseHi = new float[MaxRows];
        private readonly float[] _cutS = new float[MaxRows + 1];
        private readonly float[] _cutE = new float[MaxRows + 1];
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
        private int _pieceCount;
        private int _bandCount;
        private bool _channelShown;
        private int _laidVersion = int.MinValue;
        private int _laidPartner = -2;
        private float _laidU = -1f;
        private int _pulseCount;
        private int _pulseBands;
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

        /// <summary>Color con el que se ve la espuma que recorre el canal, en el centro del agua.</summary>
        public static Color FoamSeenColor => ColorMath.CompositeLinear(Palette.UiPaper, FoamBlobAlpha, ProceduralArt.RemolinoChannelWater);

        /// <summary>Color con el que se ve la línea de espuma del eje del canal.</summary>
        public static Color DashSeenColor => ColorMath.CompositeLinear(Palette.UiPaper, DashAlpha, ProceduralArt.RemolinoChannelWater);

        /// <summary>Color con el que se ve la boca del canal sobre el frente de la repisa.</summary>
        public static Color MouthSeenColor => ColorMath.CompositeLinear(ProceduralArt.RemolinoChannelRim, MouthAlpha, Palette.ShelfFrontLow);

        /// <summary>La otra cuia de la pareja en el tablero mostrado, o −1.</summary>
        public int PartnerIndex => _partner;

        /// <summary>Esta cuia dibuja el canal: es la de menor índice de la pareja.</summary>
        public bool DrawsChannel => _on && _partner > IndexOrMinus;

        /// <summary>El canal de esta cuia está a la vista.</summary>
        public bool ChannelVisible => _channelShown;

        /// <summary>Puntos del trazado del canal (de esta cuia a su pareja), 0 si no lo dibuja.</summary>
        public int ChannelPointCount => _channelShown ? _ptCount : 0;

        /// <summary>Tramos del canal a la vista (los trozos del trazado fuera de las repisas).</summary>
        public int ChannelSegmentCount => _channelShown ? _pieceCount : 0;

        /// <summary>Bocas del canal a la vista (una bajo el remanso de cada cuia de la pareja).</summary>
        public int MouthCount => _channelShown ? 2 : 0;

        /// <summary>La recepción por el fondo está en curso.</summary>
        public override bool IsAnimating => _animating;

        /// <summary>Avance de la recepción en curso (0..1; 0 sin paso).</summary>
        public float ReceiveProgress => _animating && _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 0f;

        /// <summary>Giro actual de la espiral (grados; 0 en reposo).</summary>
        public float SpiralAngle => -_spinFrame * (360f / ProceduralArt.RemolinoSpiralFrames);

        /// <summary>Alguna gota de espuma recorre el canal a la vista.</summary>
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
        /// Rectángulo (espacio de las capas de tablero) del tramo a la vista <paramref name="i"/> del canal, con sus extremos
        /// redondos: lo que pinta su lecho.
        /// </summary>
        public Rect ChannelSegmentRect(int i)
        {
            if ((uint)i >= (uint)MaxPieces || _bed[i] == null)
            {
                return Rect.zero;
            }

            return RectOf(_bed[i].rectTransform);
        }

        /// <summary>Rectángulo (espacio de las capas de tablero) de la boca <paramref name="i"/> (0: esta cuia; 1: la pareja).</summary>
        public Rect MouthRect(int i)
        {
            if ((uint)i >= 2u || _mouth[i] == null)
            {
                return Rect.zero;
            }

            return RectOf(_mouth[i].rectTransform);
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

            // A la derecha de la boca del canal, en el frente de la repisa, bajo el borde del remanso.
            _dots.Layout(0.62f, -0.55f, 0.13f, 0.2f, u);
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
            into.Add(ProceduralArt.Pearl); // puntos de la pareja
            into.Add(ProceduralArt.RemolinoChannelWater); // centro del agua del canal
            into.Add(ProceduralArt.RemolinoChannelEdge); // bordes del agua del canal
            into.Add(FoamSeenColor); // espuma que recorre el canal
            into.Add(DashSeenColor); // línea de espuma del eje (1 dp, pero también lejos de los tintes)
            into.Add(ProceduralArt.RemolinoChannelRim); // boca del canal
            into.Add(MouthSeenColor); // boca, vista sobre el frente de la repisa
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

            // Orden: todos los lechos, todos los centros y todas las líneas de espuma (los codos no dejan costuras), las bocas
            // y la espuma que recorre el canal.
            ProceduralArt art = Host.Art;
            NewPieces(_bed, "ChannelBed", art.RemolinoCapsule, ProceduralArt.RemolinoChannelEdge, Image.Type.Sliced);
            NewPieces(_core, "ChannelWater", art.RemolinoCapsuleSoft, ProceduralArt.RemolinoChannelWater, Image.Type.Sliced);
            NewPieces(_dash, "ChannelFoamLine", art.RemolinoDash, OverlayParts.Fade(Palette.UiPaper, DashAlpha), Image.Type.Tiled);
            for (int i = 0; i < _mouth.Length; i++)
            {
                _mouth[i] = OverlayParts.NewImage("ChannelMouth" + i, _layerRoot, art.RemolinoCapsule, OverlayParts.Fade(ProceduralArt.RemolinoChannelRim, MouthAlpha));
            }

            Sprite foam = art.RemolinoFoam;
            for (int i = 0; i < PulseBlobs; i++)
            {
                _pulse[i] = OverlayParts.NewImage("ChannelFoam" + i, _layerRoot, foam, OverlayParts.Fade(Palette.UiPaper, FoamBlobAlpha));
                _pulse[i].rectTransform.sizeDelta = new Vector2(FoamBlobAspect * FoamBlobWidthDp, FoamBlobWidthDp);
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
            Vector2 origin = LayerOrigin;
            ToRoot(_pts, _ptCount, origin);
            _mouthAt[0] = MouthCenter(board, Host.Index) - origin;
            _mouthAt[1] = MouthCenter(board, partner) - origin;
            BuildPieces(origin.y);
            PlacePieces();
            _channelShown = true;
        }

        private static Vector2 MouthCenter(ICuiaBoard board, int i)
        {
            return new Vector2(board.Foot(i).x, board.SlotRect(i).yMin - MouthDropDp);
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
        // abajo y, si a está debajo, se invierte al final: el mismo trazado para las dos cuias de la pareja. Deja en _bandLo y
        // _bandHi la franja de cada repisa en la que no se pintan los tramos verticales.
        private int Route(ICuiaBoard board, int a, int b, Vector2[] pts)
        {
            CollectShelfBands(board);
            Vector2 fa = board.Foot(a);
            Vector2 fb = board.Foot(b);
            bool swap = fb.y > fa.y + RowTolerance;
            int top = swap ? b : a;
            Vector2 ft = swap ? fb : fa;
            Vector2 fl = swap ? fa : fb;
            const float half = 0.5f * ChannelWidthDp;
            float y = UnderShelf(board.SlotRect(top).yMin);
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

                y = UnderShelf(underside);
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

        // Eje de un tramo horizontal bajo la repisa cuyo canto inferior está en underside.
        private static float UnderShelf(float underside)
        {
            return underside - ChannelGapDp - (0.5f * ChannelWidthDp);
        }

        // Franja de cada repisa (una por fila) donde no va el eje de un tramo vertical: desde el eje de los tramos horizontales
        // bajo ella hasta la cara superior, más el aire y el extremo redondo. Sin asignaciones.
        private void CollectShelfBands(ICuiaBoard board)
        {
            _bandCount = 0;
            int n = Mathf.Min(board.Count, MaxBoard);
            for (int c = 0; c < n; c++)
            {
                float fy = board.Foot(c).y;
                bool known = false;
                for (int k = 0; k < _bandCount; k++)
                {
                    if (Mathf.Abs(_bandHi[k] - (fy + ShelfTopAboveFootDp + ShelfClearDp + (0.5f * ChannelWidthDp))) <= RowTolerance)
                    {
                        known = true;
                        break;
                    }
                }

                if (known || _bandCount >= MaxRows)
                {
                    continue;
                }

                _bandLo[_bandCount] = UnderShelf(board.SlotRect(c).yMin);
                _bandHi[_bandCount] = fy + ShelfTopAboveFootDp + ShelfClearDp + (0.5f * ChannelWidthDp);
                _bandCount++;
            }
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
        private static void ToRoot(Vector2[] pts, int count, Vector2 origin)
        {
            for (int i = 0; i < count; i++)
            {
                pts[i] -= origin;
            }
        }

        // Trozos a la vista del trazado (espacio de la raíz): los tramos horizontales enteros y los verticales sin la franja de
        // ninguna repisa (originY pasa las franjas, en espacio de la capa, a la raíz).
        private void BuildPieces(float originY)
        {
            _pieceCount = 0;
            for (int i = 0; i < _ptCount - 1; i++)
            {
                Vector2 p = _pts[i];
                Vector2 q = _pts[i + 1];
                if ((q - p).sqrMagnitude < 1e-4f)
                {
                    continue;
                }

                if (Mathf.Abs(q.y - p.y) < 0.01f)
                {
                    AddPiece(p, q);
                    continue;
                }

                // Vertical: el intervalo [lo, hi] del eje menos las franjas de las repisas.
                int cuts = 1;
                _cutS[0] = Mathf.Min(p.y, q.y);
                _cutE[0] = Mathf.Max(p.y, q.y);
                for (int k = 0; k < _bandCount; k++)
                {
                    float lo = _bandLo[k] - originY;
                    float hi = _bandHi[k] - originY;
                    int kept = 0;
                    for (int c = 0; c < cuts; c++)
                    {
                        float s = _cutS[c];
                        float e = _cutE[c];
                        if (e <= lo || s >= hi)
                        {
                            Keep(ref kept, s, e);
                            continue;
                        }

                        if (s < lo)
                        {
                            Keep(ref kept, s, lo);
                        }

                        if (e > hi)
                        {
                            Keep(ref kept, hi, e);
                        }
                    }

                    cuts = kept;
                }

                for (int c = 0; c < cuts; c++)
                {
                    if (_cutE[c] - _cutS[c] >= MinPieceDp)
                    {
                        AddPiece(new Vector2(p.x, _cutS[c]), new Vector2(p.x, _cutE[c]));
                    }
                }
            }
        }

        // Guarda el intervalo [s, e] en la posición kept de los cortes (≤ MaxRows + 1; los que no caben se pierden).
        private void Keep(ref int kept, float s, float e)
        {
            if (kept < _cutS.Length)
            {
                _cutS[kept] = s;
                _cutE[kept] = e;
                kept++;
            }
        }

        private void AddPiece(Vector2 a, Vector2 b)
        {
            if (_pieceCount < MaxPieces)
            {
                _pieceA[_pieceCount] = a;
                _pieceB[_pieceCount] = b;
                _pieceCount++;
            }
        }

        // Coloca lechos, centros y líneas de espuma de los trozos y las dos bocas, y oculta lo que sobra.
        private void PlacePieces()
        {
            const float width = ChannelWidthDp;
            for (int i = 0; i < MaxPieces; i++)
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

                // Línea de espuma en el eje, sin entrar en los extremos redondos.
                float dash = length - width;
                OverlayParts.SetVisible(_dash[i], dash > 1f);
                if (dash > 1f)
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

            for (int m = 0; m < _mouth.Length; m++)
            {
                OverlayParts.SetVisible(_mouth[m], true);
                OverlayParts.Place(_mouth[m], _mouthAt[m].x, _mouthAt[m].y, MouthWidthDp, MouthHeightDp, 1f);
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
            if (!_channelShown)
            {
                return;
            }

            _channelShown = false;
            for (int i = 0; i < MaxPieces; i++)
            {
                OverlayParts.SetVisible(_bed[i], false);
                OverlayParts.SetVisible(_core[i], false);
                OverlayParts.SetVisible(_dash[i], false);
            }

            for (int m = 0; m < _mouth.Length; m++)
            {
                OverlayParts.SetVisible(_mouth[m], false);
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

        // Trazado de la espuma: el canal de la pareja (siempre de la de menor índice a la de mayor), recorrido desde la cuia
        // tocada hasta esta; las franjas de las repisas, en espacio de la raíz, para ocultar la espuma que pasa por detrás.
        private void PreparePulse(int touched)
        {
            ICuiaBoard? board = _stepBoard;
            _pulseCount = 0;
            _pulseBands = 0;
            if (board == null || touched < 0 || touched >= board.Count)
            {
                return;
            }

            int self = Host.Index;
            int a = Mathf.Min(self, touched);
            int b = Mathf.Max(self, touched);
            _pulseCount = Route(board, a, b, _pulsePts);
            Vector2 origin = LayerOrigin;
            ToRoot(_pulsePts, _pulseCount, origin);
            _pulseBands = _bandCount;
            for (int k = 0; k < _bandCount; k++)
            {
                _pulseLo[k] = _bandLo[k] - origin.y;
                _pulseHi[k] = _bandHi[k] - origin.y;
            }

            _pulseReverse = self == a; // el trazado va de a a b; la espuma, de la tocada a esta
            _pulseAt[0] = 0f;
            for (int i = 1; i < _pulseCount; i++)
            {
                _pulseAt[i] = _pulseAt[i - 1] + Vector2.Distance(_pulsePts[i - 1], _pulsePts[i]);
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

        // La espuma: 4 cápsulas suaves en fila por el canal, que entran y salen con un fundido y se ocultan por detrás de las
        // repisas, giradas en la dirección del tramo.
        private void PaintPulse(float t)
        {
            if (_pulseCount < 2 || t >= PulseEnd)
            {
                HidePulse();
                return;
            }

            float k = t / PulseEnd;
            float length = _pulseAt[_pulseCount - 1];
            float head = Ease.InOut(k) * length;
            float spacing = 1.15f * FoamBlobAspect * FoamBlobWidthDp;
            float envelope = Mathf.Clamp01(k / PulseFade) * Mathf.Clamp01((1f - k) / PulseFade);
            for (int i = 0; i < PulseBlobs; i++)
            {
                float s = head - (i * spacing);
                bool on = s >= 0f && s <= length;
                int seg = 0;
                Vector2 at = Vector2.zero;
                if (on)
                {
                    at = PointAt(_pulseReverse ? length - s : s, out seg);
                    on = !BehindShelf(seg, at);
                }

                OverlayParts.SetVisible(_pulse[i], on);
                if (!on)
                {
                    continue;
                }

                RectTransform rt = _pulse[i].rectTransform;
                if (rt.anchoredPosition != at)
                {
                    rt.anchoredPosition = at;
                }

                Vector2 d = _pulsePts[seg] - _pulsePts[seg - 1];
                SetRotation(rt, Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? 0f : 90f);
                OverlayParts.SetColor(_pulse[i], OverlayParts.Fade(Palette.UiPaper, FoamBlobAlpha * envelope * (1f - (0.15f * i))));
            }
        }

        // La gota de espuma en at, sobre el tramo seg (vertical), queda en la franja de una repisa: pasa por detrás.
        private bool BehindShelf(int seg, Vector2 at)
        {
            Vector2 d = _pulsePts[seg] - _pulsePts[seg - 1];
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
            {
                return false;
            }

            float reach = (0.5f * FoamBlobAspect * FoamBlobWidthDp) - (0.5f * ChannelWidthDp); // la gota es más larga que el extremo redondo
            for (int k = 0; k < _pulseBands; k++)
            {
                if (at.y > _pulseLo[k] + 0.01f && at.y < _pulseHi[k] + reach)
                {
                    return true;
                }
            }

            return false;
        }

        // Punto del trazado de la espuma a la distancia s de su primer punto, y el tramo (índice de su punto final).
        private Vector2 PointAt(float s, out int seg)
        {
            for (int i = 1; i < _pulseCount; i++)
            {
                if (s <= _pulseAt[i] || i == _pulseCount - 1)
                {
                    seg = i;
                    float len = Mathf.Max(1e-4f, _pulseAt[i] - _pulseAt[i - 1]);
                    return Vector2.Lerp(_pulsePts[i - 1], _pulsePts[i], Mathf.Clamp01((s - _pulseAt[i - 1]) / len));
                }
            }

            seg = 1;
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
