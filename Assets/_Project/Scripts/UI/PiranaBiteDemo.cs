#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation;
using AF.Presentation.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Mordisco animado del tutorial de la Piraña en su tarjeta de estreno (nivel 183; D-012, punto 1: opción C + D; 02 §9.1
    /// M7): un tablero en miniatura, con su panel, su repisa y dos cuias de verdad (<see cref="CuiaView"/>). La de la izquierda,
    /// una cuia de 4 como las del 183, recibe una dosis con un chorro que cae desde arriba: es el cuarto vertido, el que completa
    /// el ritmo (la cueva tenía 3 de sus 4 puntos encendidos y se queda a 0), y la Piraña sale de la cueva de la derecha y se
    /// lleva la dosis de la cima. La cueva es la del 183 y la de los 8 niveles con Piraña (183, 184, 185, 187, 189, 192, 198 y
    /// 200: <c>{"type":"pirana","k":4}</c> en una Cuia chica de 2 que empieza vacía; 02 §9.1 M7, «normalmente Cuia chica de 2»):
    /// vacía al empezar y con la dosis mordida en el fondo al acabar, así que al cerrar la tarjeta el jugador la reconoce en el
    /// tablero. El mordisco es el mismo que en el juego: lo pinta <see cref="PiranaOverlay"/> con
    /// <see cref="MechanicStepKind.Bite"/> sobre un <see cref="ICuiaBoard"/> propio de la tarjeta, así que se ve igual que en el
    /// tablero, solo que más despacio (<see cref="BiteSpeed"/>). Las reglas no se tocan: la tarjeta no juega, pinta un caso fijo
    /// (02 §9.1 M7: el mordisco del cuarto vertido a la cuia que lo recibe).
    /// </summary>
    /// <remarks>
    /// <para>Cronología [P]: espera a que la tarjeta entre, la cabeza del chorro baja (<see cref="StreamLeadMs"/>), la cuia
    /// recibe la dosis en <see cref="ReceiveMs"/>, el chorro se corta (<see cref="StreamTailMs"/>), una pausa y el mordisco
    /// (<see cref="PiranaOverlay.BiteStepMs"/> × <see cref="BiteSpeed"/>); acaba en <see cref="TotalMs"/> con la tarjeta
    /// abierta (no se repite): la cuia sin la dosis, la cueva con ella y el cardumen de vuelta a su ronda. Un toque en cualquier
    /// punto cierra la tarjeta (el escenario no recibe toques: <see cref="CanvasGroup.blocksRaycasts"/> = false).</para>
    /// <para>Con Reducir movimiento (07 §7.8), una versión quieta: el instante del mordisco (<see cref="StillFraction"/>), con
    /// el bocado ya fuera de la cuia, el cardumen quieto y sin chorro. Sin asignaciones por fotograma (08 §5.4): las vistas, el
    /// chorro (<see cref="PourStream"/>, la cinta del tablero), el tablero y los estados se crean una vez; el mordisco usa el
    /// paso del overlay, que no asigna.</para>
    /// <para>Las cuias del escenario miden 44 dp (<see cref="UnitDp"/>) y las del tablero, 48-64: como el grosor de 1 dp del
    /// borde del líquido va en el material compartido (<c>LiquidGraphic.SetUnit</c>), GameScreen vuelve a poner la unidad del
    /// tablero cada vez que coloca el escenario (GameScreen.Cards.cs, <c>RestoreBoardUnit</c>).</para>
    /// </remarks>
    internal sealed class PiranaBiteDemo
    {
        /// <summary>u del escenario (dp): cuias de 44 dp de ancho y 123,2 de alto (las del tablero miden 48-64).</summary>
        public const float UnitDp = 22f;

        /// <summary>Capacidad de la cuia que recibe la dosis: 4, como las demás cuias del 183.</summary>
        public const int VictimCapacity = 4;

        /// <summary>
        /// Capacidad de la cueva: la Cuia chica de 2 de los niveles con Piraña (183: <c>caps[8] = 2</c>, vacía al empezar), en la
        /// que caben 2 bocados.
        /// </summary>
        public const int CaveCapacity = 2;

        /// <summary>Distancia entre los ejes de las dos cuias (u).</summary>
        public const float CuiaGapU = 4.2f;

        /// <summary>Aire sobre las bocas para el arco de piedras y el cardumen (u).</summary>
        public const float HeadroomU = 1.95f;

        /// <summary>Espera antes de la dosis: la tarjeta termina de entrar (ms) [P].</summary>
        public const float ReceiveStartMs = 350f;

        /// <summary>La cuia recibe la dosis (ms) [P].</summary>
        public const float ReceiveMs = 550f;

        /// <summary>
        /// El chorro aparece este tiempo antes de la dosis: su cabeza baja hasta la superficie y entonces empieza a llenar, como en
        /// el tablero (<see cref="BoardAnimator.StreamGrowMs"/>).
        /// </summary>
        public const float StreamLeadMs = BoardAnimator.StreamGrowMs;

        /// <summary>Cola del chorro tras la dosis: se corta desde arriba (ms) [P].</summary>
        public const float StreamTailMs = 120f;

        /// <summary>Empieza el mordisco (ms desde que se abre la tarjeta) [P].</summary>
        public const float BiteStartMs = 1150f;

        /// <summary>El mordisco va a esta fracción de la velocidad del juego (× la duración de 500 ms) [P].</summary>
        public const float BiteSpeed = 2.4f;

        /// <summary>Fin de la demostración (ms desde que se abre la tarjeta).</summary>
        public const float TotalMs = BiteStartMs + (PiranaOverlay.BiteStepMs * BiteSpeed);

        /// <summary>Con Reducir movimiento, el instante del mordisco que se queda quieto (fracción del paso).</summary>
        public const float StillFraction = 0.62f;

        /// <summary>Cuia que recibe la dosis y pierde la de la cima.</summary>
        public const int VictimIndex = 0;

        /// <summary>Cuia con la cueva de la Piraña.</summary>
        public const int CaveIndex = 1;

        /// <summary>Ritmo de la cueva: cada 4 vertidos (02 §9.1 M7; el 183).</summary>
        public const int CaveK = 4;

        /// <summary>Tinte de la dosis que llega y se lleva la Piraña: taperebá (3), que se lee sobre el panel.</summary>
        public const int BittenTint = 3;

        // Grosor del marco del panel en miniatura: el del tablero a la mitad (9-slice con el doble de px por unidad).
        private const float FrameMultiplier = 2f;
        private const float ShelfTopAboveFootDp = 3f;
        private const float ShelfDp = 26f;

        private static readonly int[] VictimBase = { 11, 6 }; // lulo y camu camu
        private static readonly int[] VictimFull = { 11, 6, BittenTint };
        private static readonly int[] CaveBase = System.Array.Empty<int>(); // la cueva empieza vacía, como en el 183
        private static readonly int[] CaveFull = { BittenTint }; // la dosis mordida, en el fondo

        private readonly ProceduralArt _art;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _fill;
        private readonly Image _inner;
        private readonly Image _frame;
        private readonly Image _shelf;
        private readonly RectTransform _back;
        private readonly RectTransform _front;
        private readonly CuiaView[] _views = new CuiaView[2];
        private readonly PourStream _stream;
        private readonly StageBoard _board;
        private readonly StageFx _fx = new StageFx();
        private readonly Cuia _victimBefore;
        private readonly Cuia _victimReceived;
        private readonly Cuia _victimAfter;
        private readonly Cuia _caveBefore;
        private readonly Cuia _caveAfter;
        private float _ms = -1f;
        private float _biteMs;
        private float _biteDur;
        private bool _streamOn;
        private Stage _stage = Stage.Hidden;
        private bool _reduceMotion;
        private float _width;
        private float _height;

        /// <summary>Crea el escenario, oculto, bajo <paramref name="parent"/> (el cuerpo de la tarjeta).</summary>
        public PiranaBiteDemo(RectTransform parent, ProceduralArt art)
        {
            _art = art;
            _root = UiBuilder.Rect("PiranaBite", parent);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false; // el toque es del velo de la tarjeta, en cualquier punto
            _group.interactable = false;
            _fill = UiBuilder.Image("PanelFill", _root, art.PanelFill, Color.white);
            _inner = UiBuilder.Sliced("PanelInnerShadow", _root, art.PanelInnerShadow, Color.white);
            _frame = UiBuilder.Sliced("PanelFrame", _root, art.PanelFrame, Color.white);
            _frame.pixelsPerUnitMultiplier = FrameMultiplier;
            _shelf = UiBuilder.Sliced("Shelf", _root, art.Shelf, Color.white);
            _back = UiBuilder.Rect("MechanicBack", _root);
            _board = new StageBoard(_views, _back);
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i] = CuiaView.Create(_root, i, i == CaveIndex ? CaveCapacity : VictimCapacity, art, NoDown, NoUp, _fx);
            }

            _front = UiBuilder.Rect("MechanicFront", _root);
            _stream = new PourStream(_front, 0); // delante de las cuias, como en la capa de vertido del tablero
            _victimBefore = Cuia.Create(VictimCapacity, VictimBase);
            _victimReceived = Cuia.Create(VictimCapacity, VictimFull);
            _victimAfter = _victimBefore;
            _caveBefore = Cuia.Create(CaveCapacity, CaveBase).WithMechanic(MechanicType.Pirana, k: CaveK, counter: CaveK - 1);
            _caveAfter = Cuia.Create(CaveCapacity, CaveFull).WithMechanic(MechanicType.Pirana, k: CaveK, counter: 0);
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].BindBoard(_board, _back, _front);
            }

            _root.gameObject.SetActive(false);
        }

        private enum Stage
        {
            Hidden,
            Waiting,
            Receiving,
            Pause,
            Biting,
            Done,
            Still,
        }

        /// <summary>Raíz del escenario (tests de solapes y AF_LAYOUT).</summary>
        public RectTransform Root => _root;

        /// <summary>Se ve (en la tarjeta del estreno de la Piraña).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        /// <summary>La demostración avanza (aún no ha terminado; nunca con Reducir movimiento).</summary>
        public bool Playing => _stage == Stage.Waiting || _stage == Stage.Receiving || _stage == Stage.Pause || _stage == Stage.Biting;

        /// <summary>Terminó (o es la versión quieta de Reducir movimiento): queda así hasta que se cierra la tarjeta.</summary>
        public bool Finished => _stage == Stage.Done || _stage == Stage.Still;

        /// <summary>La versión quieta de Reducir movimiento.</summary>
        public bool Still => _stage == Stage.Still;

        /// <summary>El mordisco está en marcha (o quieto en su instante, con Reducir movimiento).</summary>
        public bool Biting => _stage == Stage.Biting || _stage == Stage.Still;

        /// <summary>Se ve el chorro que trae la dosis (tests y capturas).</summary>
        public bool StreamVisible => _streamOn;

        /// <summary>Cinta del chorro (tests y capturas).</summary>
        public PourStream Stream => _stream;

        /// <summary>Tiempo desde que se abrió la tarjeta (ms; −1 oculta).</summary>
        public float ElapsedMs => _ms;

        /// <summary>Alto del escenario (dp) para el ancho dado en <see cref="Layout"/>.</summary>
        public float Height => _height;

        /// <summary>La cuia que recibe la dosis y pierde la de la cima.</summary>
        public CuiaView Victim => _views[VictimIndex];

        /// <summary>La cuia de la cueva.</summary>
        public CuiaView Cave => _views[CaveIndex];

        /// <summary>Overlay de la Piraña de la cueva (null hasta que se pinta la cueva por primera vez).</summary>
        public CuiaOverlay? CaveOverlay => _views[CaveIndex].OverlayOf(MechanicType.Pirana);

        /// <summary>Gráficos del escenario que reciben toques (tests: ninguno, también los que crea el overlay).</summary>
        public int RaycastTargetCount()
        {
            int n = 0;
            Graphic[] all = _root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].raycastTarget)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>
        /// Alto (dp) que ocupa el escenario con <see cref="UnitDp"/>, sin colocarlo: el aire sobre la boca más alta (la de la cuia
        /// de 4; la cueva, de 2, queda más baja en la misma repisa), la cuia más alta y la repisa.
        /// </summary>
        public float MeasureHeight()
        {
            float frame = FrameDp;
            return Mathf.Ceil(FootY(frame) + (ShelfDp - ShelfTopAboveFootDp) + frame + 2f);
        }

        /// <summary>
        /// Coloca el escenario en (<paramref name="x"/>, <paramref name="y"/> desde arriba) del cuerpo de la tarjeta, con
        /// <paramref name="width"/> dp de ancho; el alto sale de <see cref="MeasureHeight"/>.
        /// </summary>
        public void Layout(float x, float y, float width)
        {
            float frame = FrameDp;
            float h = MeasureHeight();
            _width = width;
            _height = h;
            UiBuilder.Place(_root, x, y, width, h);
            UiBuilder.Place(_fill.rectTransform, frame - 1f, frame - 1f, width - (2f * frame) + 2f, h - (2f * frame) + 2f);
            UiBuilder.Place(_inner.rectTransform, frame, frame, width - (2f * frame), h - (2f * frame));
            UiBuilder.Place(_frame.rectTransform, 0f, 0f, width, h);
            float footY = FootY(frame); // las dos en la misma repisa; la cueva, de 2, más baja
            UiBuilder.Place(_shelf.rectTransform, frame + 6f, footY - ShelfTopAboveFootDp, width - (2f * frame) - 12f, ShelfDp);
            float gap = CuiaGapU * UnitDp;
            float cuiaW = CuiaGeometry.WidthU * UnitDp;
            for (int i = 0; i < _views.Length; i++)
            {
                float cx = (width * 0.5f) + ((i == VictimIndex ? -0.5f : 0.5f) * gap);
                _views[i].SetGeometry(new Vector2(cx, -footY), cuiaW, gap);
            }

            _board.Relaid();
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].NotifyBoardLayout();
            }
        }

        /// <summary>
        /// Abre la demostración desde el principio (la cuia aún sin la dosis y la cueva vacía, con 3 de sus 4 puntos); con
        /// <paramref name="reduceMotion"/>, la versión quieta en el instante del mordisco.
        /// </summary>
        public void Show(bool reduceMotion)
        {
            _root.gameObject.SetActive(true);
            _reduceMotion = reduceMotion;
            _fx.ReduceMotion = reduceMotion;
            CuiaOverlay? overlay = CaveOverlay;
            if (overlay != null && overlay.IsAnimating)
            {
                overlay.SnapStep();
            }

            HideStream();

            _board.ShowContent(VictimIndex, _victimBefore, false);
            _board.ShowContent(CaveIndex, _caveBefore, false); // aquí se une el overlay de la Piraña y crea sus piezas
            MakeStageInert(); // también las del overlay: ninguna recibe toques (asigna, pero solo al abrir la tarjeta)
            _ms = 0f;
            _biteMs = 0f;
            _biteDur = 0f;
            if (!reduceMotion)
            {
                _stage = Stage.Waiting;
                return;
            }

            // Reducir movimiento: la cuia ya tiene la dosis y el mordisco se queda quieto en su instante (el bocado fuera de la
            // cuia, que la está perdiendo, y la que va delante en su boca).
            _board.ShowContent(VictimIndex, _victimReceived, false);
            _biteDur = PlayBite();
            _stage = Stage.Still;
            Victim.Tick(0f);
            Cave.Tick(0f); // el primer fotograma del paso no avanza
            Cave.Tick(_biteDur * StillFraction);
        }

        /// <summary>La oculta en el acto (la tarjeta se cierra o enseña otra cosa) y suelta el paso a medias.</summary>
        public void Hide()
        {
            CuiaOverlay? overlay = CaveOverlay;
            if (overlay != null && overlay.IsAnimating)
            {
                overlay.SnapStep();
            }

            HideStream();
            _stage = Stage.Hidden;
            _ms = -1f;
            if (_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <summary>Avanza la demostración y las cuias (el cardumen ronda la cueva; quieto con Reducir movimiento).</summary>
        public void Tick(float dtMs)
        {
            if (_stage == Stage.Hidden || _stage == Stage.Still)
            {
                return;
            }

            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].Tick(dtMs);
            }

            _ms += dtMs;
            switch (_stage)
            {
                case Stage.Waiting:
                    if (_ms >= ReceiveStartMs)
                    {
                        _stage = Stage.Receiving;
                        PaintReceive(0f);
                    }

                    PaintStream();
                    break;

                case Stage.Receiving:
                    float f = Mathf.Clamp01((_ms - ReceiveStartMs) / ReceiveMs);
                    PaintReceive(Ease.Out(f));
                    if (f >= 1f)
                    {
                        _board.ShowContent(VictimIndex, _victimReceived, false);
                        Victim.Ripple();
                        _stage = Stage.Pause;
                    }

                    PaintStream();
                    break;

                case Stage.Pause:
                    PaintStream(); // la cola del chorro
                    if (_ms >= BiteStartMs)
                    {
                        _biteDur = PlayBite();
                        _biteMs = 0f;
                        _stage = Stage.Biting;
                    }

                    break;

                case Stage.Biting:
                    _biteMs += dtMs;
                    if (_biteMs >= _biteDur)
                    {
                        CuiaOverlay? overlay = CaveOverlay;
                        overlay?.SnapStep(); // pinta el final: la cuia sin la dosis y la cueva con ella
                        _stage = Stage.Done;
                    }

                    break;
            }
        }

        // La dosis que llega sube de 0 a su nivel en la cuia que la recibe.
        private void PaintReceive(float fill)
        {
            _board.SetDose(VictimIndex, VictimBase.Length, Palette.Tint(BittenTint), fill, BittenTint);
        }

        // El chorro que trae la dosis: cae recto desde lo alto del panel, por el eje de la cuia, hasta su superficie (que sube
        // mientras se llena). La cabeza baja StreamLeadMs antes de la dosis y la cola se corta desde arriba en StreamTailMs
        // después; la misma cinta, los mismos grosores y el mismo tinte que el chorro del tablero (guía 6.5). Sin asignaciones.
        private void PaintStream()
        {
            float start = ReceiveStartMs - StreamLeadMs;
            float flowEnd = ReceiveStartMs + ReceiveMs;
            if (_ms < start || _ms >= flowEnd + StreamTailMs)
            {
                HideStream();
                return;
            }

            float head = Mathf.Clamp01((_ms - start) / StreamLeadMs);
            float tail = _ms > flowEnd ? Mathf.Clamp01((_ms - flowEnd) / StreamTailMs) : 0f;
            CuiaView victim = Victim;
            float u = victim.Unit;
            Vector2 end = victim.SurfaceInContainer();
            var top = new Vector2(end.x, -(FrameDp + 1f));
            _stream.Draw(top, (top + end) * 0.5f, end, tail, head, Palette.Tint(BittenTint), BoardAnimator.StreamWidthStartU * u, BoardAnimator.StreamWidthEndU * u);
            _streamOn = true;
        }

        private void HideStream()
        {
            if (_streamOn)
            {
                _stream.Hide();
                _streamOn = false;
            }
        }

        // Pie de las dos cuias (dp desde lo alto del escenario): el aire sobre la boca más alta y la cuia más alta.
        private float FootY(float frame)
        {
            float tallest = Mathf.Max(CuiaGeometry.HeightU(VictimCapacity), CuiaGeometry.HeightU(CaveCapacity));
            return frame + ((HeadroomU + tallest) * UnitDp);
        }

        // El mordisco del juego: el paso Bite del overlay de la cueva sobre el tablero de la tarjeta, más despacio.
        private float PlayBite()
        {
            CuiaOverlay? overlay = CaveOverlay;
            if (overlay == null)
            {
                return 0f;
            }

            var ctx = new MechanicStepContext(
                MechanicStepKind.Bite,
                CaveIndex,
                VictimIndex,
                _caveBefore,
                _caveAfter,
                false,
                _victimReceived,
                _victimAfter,
                false,
                _board.Foot(CaveIndex),
                _board.Foot(VictimIndex),
                _board,
                _back,
                _front,
                _fx,
                _art,
                BiteSpeed,
                _reduceMotion,
                BittenTint,
                0);
            return overlay.PlayStep(in ctx); // sin asignar: el overlay creó sus piezas al unirse (Show las dejó sin toques)
        }

        // Ningún gráfico del escenario recibe toques (el área táctil de las cuias tampoco): el toque es del velo de la tarjeta.
        private void MakeStageInert()
        {
            Graphic[] all = _root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].raycastTarget)
                {
                    all[i].raycastTarget = false;
                }
            }
        }

        private float FrameDp => _art.PanelFrameDp / FrameMultiplier;

        private static void NoDown(int index)
        {
        }

        private static void NoUp(int index, bool inside)
        {
        }

        /// <summary>Tablero de dos cuias de la tarjeta para el overlay de la Piraña (posiciones en el espacio de sus capas).</summary>
        private sealed class StageBoard : ICuiaBoard
        {
            private readonly CuiaView[] _views;
            private readonly RectTransform _layer;
            private readonly Cuia[] _shown = new Cuia[2];
            private int _version;

            public StageBoard(CuiaView[] views, RectTransform layer)
            {
                _views = views;
                _layer = layer;
            }

            public int Count => _views.Length;

            public int LayoutVersion => _version;

            public void Relaid()
            {
                _version++;
            }

            public Cuia ShownCuia(int i)
            {
                return _shown[i];
            }

            public Vector2 BodyCenter(int i)
            {
                CuiaView view = _views[i];
                return ToLayer(view.Root.TransformPoint(new Vector3(0f, 0.5f * view.HeightU * view.Unit, 0f)));
            }

            public Vector2 Foot(int i)
            {
                return ToLayer(_views[i].Root.position);
            }

            public Rect SlotRect(int i)
            {
                CuiaView view = _views[i];
                Rect r = view.Root.rect;
                float shelfBottom = r.yMin - (ShelfDp - ShelfTopAboveFootDp);
                Vector2 a = ToLayer(view.Root.TransformPoint(new Vector3(r.xMin, shelfBottom, 0f)));
                Vector2 b = ToLayer(view.Root.TransformPoint(new Vector3(r.xMax, r.yMax, 0f)));
                return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            }

            public void SetDose(int i, int dose, Color color, float fill, int pattern)
            {
                if ((uint)i < (uint)_views.Length)
                {
                    _views[i].SetDose(dose, color, fill, pattern);
                }
            }

            public void ShowContent(int i, Cuia cuia, bool sealedNow)
            {
                if ((uint)i < (uint)_views.Length)
                {
                    _shown[i] = cuia;
                    _views[i].ShowContent(cuia, sealedNow);
                }
            }

            private Vector2 ToLayer(Vector3 world)
            {
                Vector3 local = _layer.InverseTransformPoint(world);
                return new Vector2(local.x, local.y);
            }
        }

        /// <summary>Efectos de la tarjeta: ninguno (sin capa de partículas propia); solo lleva Reducir movimiento al cardumen.</summary>
        private sealed class StageFx : IBoardFx
        {
            public bool ReduceMotion { get; set; }

            public void Splash(Vector2 surface, Color tint, float unitDp)
            {
            }

            public void SealDrops(Vector2 rim, Color tint, float unitDp)
            {
            }

            public void Sparkles(Vector2 pos, Color color, int count, float unitDp)
            {
            }

            public void Shards(Vector2 pos, Color color, int count, float unitDp)
            {
            }

            public void Bubbles(Vector2 pos, int count, float unitDp)
            {
            }
        }
    }
}
