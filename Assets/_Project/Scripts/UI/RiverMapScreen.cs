#nullable enable
using System;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Vista de la pantalla del Mapa del río (D-013, punto 5; propuesta del Mapa §3.2).</summary>
    public enum MapView
    {
        /// <summary>Mundo (por defecto): la región del mundo de «Jugar», entre <see cref="RiverMapScreen.WorldZoomMin"/>× y <see cref="RiverMapScreen.WorldZoomMax"/>×.</summary>
        World = 0,

        /// <summary>Cuenca: toda la cuenca a 1× (<see cref="MapSpace.BasinWidthDp"/>: el ancho útil de 328 dp (360 − 2 × 16), 13,1 dp por grado).</summary>
        Basin = 1,
    }

    /// <summary>Lo que dejó el Mapa al cerrarse, para la sonda <c>map_close</c> (<c>af_map_close</c>, 08 §8.2).</summary>
    public readonly struct MapCloseStats
    {
        /// <summary>Crea las cifras.</summary>
        public MapCloseStats(MapOpenSource source, int dwellMs, int stopsTapped, int viewChanges, bool northUp)
        {
            Source = source;
            DwellMs = dwellMs;
            StopsTapped = stopsTapped;
            ViewChanges = viewChanges;
            NorthUp = northUp;
        }

        /// <summary>Desde dónde se abrió.</summary>
        public MapOpenSource Source { get; }

        /// <summary>Milisegundos de reloj real con el Mapa abierto.</summary>
        public int DwellMs { get; }

        /// <summary>Paradas tocadas (las mini cuias llegan con M3.5 y M4.1: hoy, 0).</summary>
        public int StopsTapped { get; }

        /// <summary>Cambios de vista (Mundo ↔ Cuenca).</summary>
        public int ViewChanges { get; }

        /// <summary>Se usó «Norte arriba» (M4.4: lo fija su parcial, <c>RiverMapScreen.NorthUp.cs</c>; hasta entonces, false).</summary>
        public bool NorthUp { get; }
    }

    /// <summary>
    /// Pantalla del Mapa del río (D-013, puntos 4, 5 y 7; 06 §4.5 y §4.7; 08 §2.2 y §5.4; propuesta del Mapa §3.1-§3.5, §8.4 y
    /// §11.4; tarea M3.3: el armazón), construida por código como <see cref="LobbyScreen"/>, sin escena ni prefab, encima de
    /// donde se abre (hoy, la pestaña del inicio):
    /// <list type="bullet">
    /// <item><description><b>Tres lienzos</b>, por debajo de la hoja de compra (50), del anuncio (60) y del aviso del perfil
    /// bloqueado (70): el de la UI (<see cref="SortingOrder"/>, con la cabecera, el marco y el pie) y, dentro de la zona del mapa,
    /// uno <b>estático</b> anidado (tierra y vidrio; <see cref="StaticCanvas"/>) y uno <b>dinámico</b> anidado (canoa y lo que se
    /// anima; <see cref="DynamicCanvas"/>), sin <c>overrideSorting</c> para que el <see cref="RectMask2D"/> los recorte
    /// (propuesta del Mapa §11.4).</description></item>
    /// <item><description><b>Cabecera</b> de madera con ← (cierra, como el Atrás de Android) y la pregunta del mundo
    /// (<c>map_world_question</c>); si ya se respondió (la llegada a la parada con <c>answers_world_question</c>), un
    /// <b>icono ✓ procedural</b>: ninguna de las dos familias tiene ese glifo (B1; P-M2).</description></item>
    /// <item><description><b>Zona del mapa</b> en su marco de madera, con <see cref="RectMask2D"/>: el mar, la tierra y la cuenca
    /// en vidrio (una cinta por línea; <see cref="RiverRibbonGraphic"/>), la canoa y «Volver a la canoa»
    /// (<c>map_back_to_canoe</c>) cuando la canoa queda fuera de la vista.</description></item>
    /// <item><description><b>Pie</b>: el conmutador Mundo / Cuenca (<c>map_view_world</c> y <c>map_view_basin</c>), «Faltan N»
    /// (<c>map_stop_remaining</c>) y «Jugar · Nivel {n}», siempre a la vista; el hueco del Muestrario (M4.1), oculto.</description></item>
    /// </list>
    /// <para><b>Vistas</b> (<see cref="MapView"/>): Mundo, la caja de las paradas del mundo de «Jugar» y la canoa al
    /// <see cref="WorldFill"/> de la zona, entre <see cref="WorldZoomMin"/>× y <see cref="WorldZoomMax"/>× [P] (propuesta del
    /// Mapa §3.2: unos 4× en Manaos y 15× en el Trapecio); Cuenca, 1×. Se arrastra con un dedo, siempre dentro del mapa.</para>
    /// <para><b>Rendimiento</b> (08 §5.4; propuesta del Mapa §11.4): las mallas se construyen al abrir y al cambiar de vista,
    /// nunca por fotograma; con el Mapa quieto, <see cref="Tick"/> no asigna memoria ni reconstruye nada, y
    /// <see cref="OnDemandRendering"/> pinta a <see cref="PerfTargetFpsMenus"/> fps (<c>perf_target_fps_menus</c>, 08 §9) y a 60
    /// mientras se arrastra o algo se anima. Al cerrar, vuelve a 60.</para>
    /// <para>Los ganchos parciales (<c>Progress*</c>, <c>Markers*</c>, <c>CatchUp*</c>, <c>Samples*</c>, <c>Sequence*</c>,
    /// <c>Modes*</c> y <c>NorthUp*</c>) son de las tareas siguientes del Mapa: el río que se llena (M3.4), los marcadores y
    /// «ponerse al día» (M3.5), el Muestrario (M4.1), la secuencia del Mapa del 10 (M4.2), las entradas de la pausa y la
    /// victoria (M4.3) y «Norte arriba» (M4.4). Cada tarea los implementa en su propio archivo parcial
    /// (<c>RiverMapScreen.Sequence.cs</c>, <c>RiverMapScreen.NorthUp.cs</c>…) sin tocar este. Sin cuerpo no cuestan nada.</para>
    /// </summary>
    public sealed partial class RiverMapScreen
    {
        /// <summary>Orden del lienzo de la UI del Mapa: sobre Ajustes (40) y por debajo de la hoja de compra (50).</summary>
        public const int SortingOrder = 45;

        /// <summary>
        /// <c>max_level</c> desde el que sale la pestaña del Mapa en el inicio: el 10, la primera llegada (06 §2.3 y §4.7; D-008:
        /// oculto hasta que existe).
        /// </summary>
        public const int TabFromMaxLevel = RiverStopCatalog.LevelsPerStop;

        /// <summary>Zoom mínimo de la vista Mundo [P] (propuesta del Mapa §3.2: unos 4× en Manaos).</summary>
        public const float WorldZoomMin = 4f;

        /// <summary>Zoom máximo de la vista Mundo [P] (propuesta del Mapa §3.2: unos 15× en el Trapecio).</summary>
        public const float WorldZoomMax = 15f;

        /// <summary>Fracción de la zona del mapa que ocupa la caja de las paradas del mundo en la vista Mundo [P].</summary>
        public const float WorldFill = 0.6f;

        /// <summary><c>perf_target_fps_menus</c> (08 §9: 30, de 30 a 60): fotogramas por segundo del Mapa quieto (08 §5.4).</summary>
        public const int PerfTargetFpsMenus = 30;

        /// <summary>Tras soltar el arrastre, el Mapa sigue a 60 fps estos milisegundos [P] antes de volver al reposo.</summary>
        public const int ActiveLingerMs = 300;

        /// <summary>
        /// Alto de la cabecera bajo la zona segura (dp): dos líneas holgadas de la pregunta a 17 sp (unos 2 × 26,5 dp con la
        /// interlínea de la familia) con 4 dp de margen arriba y abajo.
        /// </summary>
        public const float HeaderDp = 64f;

        /// <summary>Lado mínimo de un control (07 §1.2 y §7.8).</summary>
        public const float MinTouchDp = 48f;

        /// <summary>Margen que deja la canoa con el borde de la vista antes de ofrecer «Volver a la canoa» (dp) [P].</summary>
        public const float CanoeMarginDp = 24f;

        /// <summary>
        /// Radio de las esquinas de fuera del marco de madera (dp): el de <see cref="ProceduralArt.RoundedRect"/> (20 px a 100 px
        /// por unidad, es decir, 20 dp con <c>pixelsPerUnitMultiplier</c> = 1).
        /// </summary>
        public const float FrameRadiusDp = ProceduralArt.RoundedRectRadiusPx;

        /// <summary>
        /// Radio de las esquinas de la zona del mapa (dp): concéntricas con las del marco, <see cref="FramePadDp"/> por dentro
        /// (D-003: el vidrio y la tierra siguen el filete de la madera; <see cref="RectMask2D"/> recorta en ángulo recto).
        /// </summary>
        public const float ViewportRadiusDp = FrameRadiusDp - FramePadDp;

        /// <summary>Relleno de madera entre el borde del marco y la zona del mapa (dp).</summary>
        public const float FramePadDp = 8f;

        /// <summary>
        /// Mar del Mapa (<c>mar_a</c> de la muestra, <c>generar_mapa.py</c>) [P]: solo para el polígono del mar o la esquina
        /// atlántica. <c>river_map_v1.json</c> aún no los trae, así que hoy no se pinta en ningún sitio (hasta M3.4): fuera de la
        /// cuenca no hay mar, sino tierra (<see cref="GroundColor"/>).
        /// </summary>
        public static readonly Color SeaColor = Palette.Hex(0x0F6275);

        /// <summary>
        /// Tierra fuera de la cuenca: el fondo del visor, la imagen que recibe el arrastre (la mezcla de la selva #2A7A4B [P], a un
        /// paso del dosel #2C7A4B de <see cref="LandGraphic.CanopyColor"/>), para que el borde de los datos no se lea como una
        /// costa: la cuenca no es una isla y al sur y al oeste no hay mar (D-003).
        /// </summary>
        public static readonly Color GroundColor = Palette.Hex(0x2A7A4B);

        /// <summary>Ancho del cauce y del ramal del Pará (clase 1) en la vista Mundo, en dp [P] (en Cuenca, el de la cinta).</summary>
        public const float WorldWidthClass1Dp = 8f;

        /// <summary>Ancho de los afluentes (clase 2) en la vista Mundo, en dp [P].</summary>
        public const float WorldWidthClass2Dp = 5f;

        /// <summary>Ancho de los ríos locales (clase 3) en la vista Mundo, en dp [P].</summary>
        public const float WorldWidthClass3Dp = 3f;

        /// <summary>Fondo de la pantalla detrás del marco (<c>fondo_a</c> de la muestra) [P].</summary>
        public static readonly Color BackgroundColor = Palette.Hex(0x0E2B23);

        /// <summary>Cápsula del conmutador Mundo / Cuenca: la madera del marco del mapa (<see cref="Palette.Frame"/>).</summary>
        public static readonly Color SegmentSwitchColor = Palette.Frame;

        /// <summary>Placa de la pestaña elegida (crema): el verde queda para «Jugar».</summary>
        public static readonly Color SegmentSelectedPlate = Palette.Cream;

        /// <summary>Texto de la pestaña elegida (madera oscura sobre crema).</summary>
        public static readonly Color SegmentSelectedText = Palette.WoodDark;

        /// <summary>Texto de la pestaña no elegida (crema sobre la cápsula).</summary>
        public static readonly Color SegmentText = Palette.Cream;

        private const float PlayDp = 72f;
        private const float PlayMaxWidth = 296f;
        private const float RowDp = 48f;
        private const float GapDp = 8f;
        private const float FrameInsetDp = 8f;
        private const float SegmentDp = 82f;
        private const float SwitchPadDp = 4f;
        private const float RowPadDp = 10f;
        private const float TwoLineRowDp = 56f;
        private const float RemainingLabelXDp = 14f;
        private const float CheckDp = 24f;
        private const float TitleSp = 17f;
        private const float TitleMinSp = 13f;
        private const float RowSp = 15f;
        private const float RemainingSp = 14f;
        private const float MinZoomExtent = 1e-4f;

        private readonly ProceduralArt _art;
        private readonly DevProbe _probe;
        private readonly OverlayCanvas _canvas;
        private readonly RectTransform _root;

        private readonly Image _background;
        private readonly Image _headerPlate;
        private readonly ReliefParts _back;
        private readonly TextMeshProUGUI _title;
        private readonly GlyphGraphic _check;

        private readonly Image _frame;
        private readonly FrameCornersGraphic _frameCorners;
        private readonly Image _ground;
        private readonly RectTransform _viewport;
        private readonly RectTransform _content;
        private readonly Canvas _staticCanvas;
        private readonly RectTransform _staticLayer;
        private readonly Canvas _dynamicCanvas;
        private readonly RectTransform _dynamicLayer;
        private readonly LandGraphic _land;
        private readonly RectTransform _glassLayer;
        private readonly List<RiverRibbonGraphic> _glass = new List<RiverRibbonGraphic>();
        private readonly Image _canoe;
        private readonly ReliefParts _backToCanoe;
        private readonly TextMeshProUGUI _backToCanoeLabel;

        private readonly Image _viewSwitch;
        private readonly Segment _viewWorld;
        private readonly Segment _viewBasin;
        private readonly RectTransform _remaining;
        private readonly TextMeshProUGUI _remainingLabel;
        private readonly RectTransform _samplesSlot;
        private readonly ReliefParts _play;
        private readonly TextMeshProUGUI _playLabel;

        private RiverMap? _map;
        private RiverMap? _builtFor;
        private RiverRoutes? _routes;
        private RiverStopCatalog? _stops;
        private IMapProgress? _progress;
        private MapOpenSource _source;
        private MapView _view = MapView.World;
        private MapSpace _space = new MapSpace(MapSpace.BasinWidthDp, Vector2.zero);
        private float _zoom = 1f;
        private float _contentW;
        private float _contentH;
        private Vector2 _offset;
        private Vector2 _canoeMap;
        private bool _hasCanoe;
        private int _world = 1;
        private int _playLevel = 1;
        private bool _answered;
        private bool _remainingReady;
        private bool _dragging;
        private float _activeMs;
        private float _openedAt;
        private int _viewChanges;
        private int _stopsTapped;
        private bool _northUp;
        private bool _layoutCanoeButton;

        private int _screenW;
        private int _screenH;
        private Rect _safeArea;
        private float _canvasW = LobbyScreen.ReferenceWidth;
        private float _canvasH = 780f;
        private float _safeTop = GameScreen.MinSafeTop;
        private float _safeBottom = GameScreen.MinSafeBottom;
        private float _scale = 1f;
        private float _viewW = MapSpace.BasinWidthDp;
        private float _viewH = 400f;

        /// <summary>
        /// Construye la pantalla bajo <paramref name="host"/>, oculta hasta <see cref="Open"/>. <paramref name="onClose"/> es el
        /// ← de la cabecera (cierra, como el Atrás de Android) y <paramref name="onPlay"/>, «Jugar»: los dos los resuelve
        /// <see cref="GameBootstrap"/> (<see cref="IGameActions.CloseRiverMap"/>).
        /// </summary>
        public RiverMapScreen(Transform host, ProceduralArt art, DevProbe probe, Action onClose, Action onPlay)
        {
            _art = art ?? throw new ArgumentNullException(nameof(art));
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            if (onClose == null || onPlay == null)
            {
                throw new ArgumentNullException(onClose == null ? nameof(onClose) : nameof(onPlay));
            }

            _canvas = new OverlayCanvas("RiverMapCanvas", host, SortingOrder);
            _root = _canvas.Root;

            // Fondo: tapa lo de detrás y se queda con sus toques.
            _background = UiBuilder.Image("Background", _root, null, BackgroundColor, true);
            UiBuilder.Stretch(_background.rectTransform);

            // ---- Zona del mapa: marco de madera, máscara, tierra de fondo, contenido (dos lienzos anidados) y «Volver a la canoa». ----
            _frame = UiBuilder.Sliced("Frame", _root, art.RoundedRect, Palette.Frame);
            _viewport = UiBuilder.Rect("Viewport", _frame.transform);
            _viewport.gameObject.AddComponent<RectMask2D>();
            _viewport.gameObject.AddComponent<DragArea>().Owner = this;
            _ground = UiBuilder.Image("Ground", _viewport, null, GroundColor, true); // recibe el arrastre fuera de la cuenca
            UiBuilder.Stretch(_ground.rectTransform);
            _content = UiBuilder.Rect("Content", _viewport);
            UiBuilder.Place(_content, 0f, 0f, MapSpace.BasinWidthDp, MapSpace.BasinWidthDp);

            // Las esquinas redondeadas de la zona del mapa (el RectMask2D recorta en ángulo recto): cuatro rincones de madera encima
            // del contenido, en el lienzo de la UI, que no reciben toques.
            _frameCorners = FrameCornersGraphic.Create("ViewportCorners", _frame.transform, ViewportRadiusDp, _frame.color);

            _staticCanvas = NestedCanvas("StaticLayer", _content, false, out _staticLayer);
            var landGo = new GameObject("Land", typeof(RectTransform), typeof(CanvasRenderer), typeof(LandGraphic));
            landGo.transform.SetParent(_staticLayer, false);
            _land = landGo.GetComponent<LandGraphic>();
            _land.Setup();
            FillTopLeft((RectTransform)landGo.transform);
            _glassLayer = UiBuilder.Rect("Glass", _staticLayer);
            FillTopLeft(_glassLayer);

            _dynamicCanvas = NestedCanvas("DynamicLayer", _content, true, out _dynamicLayer);
            _canoe = UiBuilder.Image("Canoe", _dynamicLayer, null, Color.white);
            _canoe.preserveAspect = true;
            _canoe.gameObject.SetActive(false);

            _backToCanoe = UiBuilder.Relief("BackToCanoeButton", _frame.transform, art.CtaWood, BackToCanoe);
            _backToCanoeLabel = UiBuilder.Label("Label", _backToCanoe.Face, TextStyle.PillWood, UiText.MapBackToCanoe, RowSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_backToCanoeLabel.rectTransform, art.CtaWood);
            UiBuilder.AutoSize(_backToCanoeLabel, UiBuilder.MinTextSp, RowSp);
            _backToCanoe.Relief.Bind(_backToCanoe.Face, _backToCanoe.Base, _backToCanoe.FaceImage, art.CtaWood, null, null, _backToCanoeLabel);
            _backToCanoe.Button.gameObject.SetActive(false);

            // ---- Cabecera de madera: ← y la pregunta del mundo, con su ✓ al responderse. ----
            _headerPlate = UiBuilder.Sliced("HeaderPlate", _root, art.WoodPlate, Color.white);
            _back = UiBuilder.Relief("BackButton", _headerPlate.transform, art.DiscWood, () => onClose());
            RectTransform backVisual = (RectTransform)_back.Face.parent;
            backVisual.anchorMin = new Vector2(0.5f, 1f);
            backVisual.anchorMax = new Vector2(0.5f, 1f);
            backVisual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            backVisual.anchoredPosition = new Vector2(0f, -Mathf.Max(0f, (MinTouchDp - art.DiscWood.HeightDp) * 0.5f)); // centrado en sus 48 dp
            GlyphGraphic arrow = GlyphGraphic.Create("Arrow", _back.Face, GlyphKind.BackArrow);
            UiBuilder.Place(arrow.rectTransform, 9f, 9f, 22f, 22f);
            _back.Relief.Bind(_back.Face, _back.Base, _back.FaceImage, art.DiscWood, null, null, null);
            _title = UiBuilder.Label("Title", _headerPlate.transform, TextStyle.ModalTitle, string.Empty, TitleSp, Palette.Cream, TextAlignmentOptions.Left);
            _title.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_title, TitleMinSp, TitleSp);
            _check = GlyphGraphic.Create("AnsweredCheck", _headerPlate.transform, GlyphKind.Check);
            _check.gameObject.SetActive(false);

            // ---- Pie: Mundo / Cuenca (una sola cápsula de madera: un interruptor de dos posiciones), «Faltan N», el hueco del
            // Muestrario y «Jugar · Nivel {n}» (el único verde del pie). ----
            _viewSwitch = UiBuilder.Sliced("ViewSwitch", _root, art.RoundedRect, Palette.Frame);
            _viewWorld = new Segment(_root, art, "ViewWorld", UiText.MapViewWorld, () => SetView(MapView.World));
            _viewBasin = new Segment(_root, art, "ViewBasin", UiText.MapViewBasin, () => SetView(MapView.Basin));
            _remaining = UiBuilder.Rect("StopRemaining", _root);
            Image remainingPlate = UiBuilder.Sliced("Plate", _remaining, art.WoodPlate, Color.white);
            UiBuilder.Stretch(remainingPlate.rectTransform);
            _remainingLabel = UiBuilder.Label("Label", _remaining, TextStyle.PlainBold, string.Empty, RemainingSp, Palette.Cream, TextAlignmentOptions.Center);
            _remainingLabel.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_remainingLabel, UiBuilder.MinTextSp, RemainingSp);
            _remaining.gameObject.SetActive(false);
            _samplesSlot = UiBuilder.Rect("SamplesSlot", _root);
            _samplesSlot.gameObject.SetActive(false);
            SamplesBuild(_samplesSlot);

            _play = UiBuilder.Relief("PlayButton", _root, art.CtaLeafLarge, () => onPlay());
            _playLabel = UiBuilder.Label("Label", _play.Face, TextStyle.CtaLeaf, UiText.PlayLevel(1), 30f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_playLabel.rectTransform, art.CtaLeafLarge);
            UiBuilder.AutoSize(_playLabel, 22f, 30f);
            _play.Relief.Bind(_play.Face, _play.Base, _play.FaceImage, art.CtaLeafLarge, null, null, _playLabel);

            SequenceBuild(_root, _dynamicLayer);
            NorthUpBuild((RectTransform)_frame.transform);
            ApplyScreenMetrics();
            _canvas.Canvas.gameObject.SetActive(false);
        }

        /// <summary>El Mapa está abierto.</summary>
        public bool Visible => _canvas.Canvas.gameObject.activeSelf;

        /// <summary>Lienzo de la UI (capturas: <c>SnapshotKit.AttachOverlay</c>).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>Lienzo estático anidado: tierra y vidrio.</summary>
        public Canvas StaticCanvas => _staticCanvas;

        /// <summary>Lienzo dinámico anidado: la canoa y lo que se anima.</summary>
        public Canvas DynamicCanvas => _dynamicCanvas;

        /// <summary>Capa del lienzo estático (M3.4 añade aquí el lleno).</summary>
        internal RectTransform StaticLayer => _staticLayer;

        /// <summary>Capa del lienzo dinámico (M3.4 y M3.5 añaden aquí el frente y los marcadores).</summary>
        internal RectTransform DynamicLayer => _dynamicLayer;

        /// <summary>Desde dónde se abrió (el último <see cref="Open"/>).</summary>
        public MapOpenSource Source => _source;

        /// <summary>Vista actual.</summary>
        public MapView View => _view;

        /// <summary>Zoom de la vista actual: 1 en Cuenca; entre <see cref="WorldZoomMin"/> y <see cref="WorldZoomMax"/> en Mundo.</summary>
        public float Zoom => _zoom;

        /// <summary>Espacio del Mapa de la vista actual (dp por unidad del marco y origen arriba a la izquierda del contenido).</summary>
        public MapSpace Space => _space;

        /// <summary>Mapa que se pinta (null antes de abrirlo).</summary>
        public RiverMap? Map => _map;

        /// <summary>Zona visible del mapa (con la máscara).</summary>
        public RectTransform ViewportRect => _viewport;

        /// <summary>Rincones de madera de la zona del mapa: sus esquinas redondeadas (tests y capturas).</summary>
        internal FrameCornersGraphic ViewportCorners => _frameCorners;

        /// <summary>Contenido que se arrastra (tierra, vidrio y canoa).</summary>
        public RectTransform ContentRect => _content;

        /// <summary>Esquina de arriba a la izquierda del contenido dentro de la zona visible, en dp (y hacia abajo).</summary>
        public Vector2 ContentOffset => _offset;

        /// <summary>Tamaño del contenido en dp.</summary>
        public Vector2 ContentSize => new Vector2(_contentW, _contentH);

        /// <summary>Tamaño de la zona visible en dp.</summary>
        public Vector2 ViewportSize => new Vector2(_viewW, _viewH);

        /// <summary>Tierra.</summary>
        public LandGraphic Land => _land;

        /// <summary>Fondo del visor: la tierra fuera de la cuenca (<see cref="GroundColor"/>), que recibe el arrastre.</summary>
        public Image Ground => _ground;

        /// <summary>Cintas de vidrio activas, una por línea del Mapa.</summary>
        public int GlassCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _glass.Count; i++)
                {
                    if (_glass[i].gameObject.activeSelf)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        /// <summary>Cinta de vidrio <paramref name="i"/> (en el orden de las líneas del Mapa).</summary>
        public RiverRibbonGraphic Glass(int i)
        {
            return _glass[i];
        }

        /// <summary>Canoa (M3.5 la viste con sus banderas y el guía).</summary>
        internal Image Canoe => _canoe;

        /// <summary>Hay canoa: el Mapa tiene rutas y sabe dónde está.</summary>
        public bool HasCanoe => _hasCanoe;

        /// <summary>Dónde está la canoa, en unidades del marco (W = 1, y hacia abajo).</summary>
        public Vector2 CanoeMapPoint => _canoeMap;

        /// <summary>Centro de la canoa dentro de la zona visible, en dp (y hacia abajo).</summary>
        public Vector2 CanoeInViewport
        {
            get
            {
                Vector2 local = _space.ToLocal(_canoeMap);
                return new Vector2(_offset.x + local.x, _offset.y - local.y);
            }
        }

        /// <summary>La canoa se ve con su margen dentro de la zona del mapa.</summary>
        public bool CanoeInView
        {
            get
            {
                if (!_hasCanoe)
                {
                    return true;
                }

                Vector2 p = CanoeInViewport;
                return p.x >= CanoeMarginDp && p.x <= _viewW - CanoeMarginDp && p.y >= CanoeMarginDp && p.y <= _viewH - CanoeMarginDp;
            }
        }

        /// <summary>Mundo cuya región enseña la vista Mundo (el de «Jugar»).</summary>
        public int World => _world;

        /// <summary>Nivel de «Jugar».</summary>
        public int PlayLevel => _playLevel;

        /// <summary>Texto de la cabecera: la pregunta del mundo (o, sin ella, el nombre del mundo).</summary>
        public string Title => _title.text;

        /// <summary>Texto de la cabecera (tests: tamaño y caja).</summary>
        internal TextMeshProUGUI TitleLabel => _title;

        /// <summary>La pregunta del mundo ya se respondió: sale el ✓.</summary>
        public bool QuestionAnswered => _answered;

        /// <summary>El ✓ de la cabecera se ve.</summary>
        public bool CheckShown => _check.gameObject.activeSelf;

        /// <summary>Caja del ✓.</summary>
        public RectTransform CheckRect => _check.rectTransform;

        /// <summary>Cabecera.</summary>
        public RectTransform HeaderRect => _headerPlate.rectTransform;

        /// <summary>← de la cabecera.</summary>
        public Button BackButton => _back.Button;

        /// <summary>«Jugar · Nivel {n}».</summary>
        public Button PlayButton => _play.Button;

        /// <summary>Texto de «Jugar».</summary>
        public string PlayLabel => _playLabel.text;

        /// <summary>Pestaña Mundo del conmutador.</summary>
        public Button ViewWorldButton => _viewWorld.Button;

        /// <summary>Pestaña Cuenca del conmutador.</summary>
        public Button ViewBasinButton => _viewBasin.Button;

        /// <summary>Texto de la pestaña Mundo.</summary>
        public string ViewWorldLabel => _viewWorld.Label.text;

        /// <summary>Texto de la pestaña Cuenca.</summary>
        public string ViewBasinLabel => _viewBasin.Label.text;

        /// <summary>Cápsula del conmutador Mundo / Cuenca.</summary>
        internal Image ViewSwitch => _viewSwitch;

        /// <summary>Placa de la pestaña de <paramref name="view"/> (activa solo en la elegida).</summary>
        internal Image ViewPlate(MapView view)
        {
            return view == MapView.World ? _viewWorld.Plate : _viewBasin.Plate;
        }

        /// <summary>Texto de la pestaña de <paramref name="view"/>.</summary>
        internal TextMeshProUGUI ViewLabel(MapView view)
        {
            return view == MapView.World ? _viewWorld.Label : _viewBasin.Label;
        }

        /// <summary>«Volver a la canoa».</summary>
        public Button BackToCanoeButton => _backToCanoe.Button;

        /// <summary>«Volver a la canoa» se ve.</summary>
        public bool BackToCanoeShown => _backToCanoe.Button.gameObject.activeSelf;

        /// <summary>Texto de «Volver a la canoa».</summary>
        public string BackToCanoeLabel => _backToCanoeLabel.text;

        /// <summary>«Faltan {n} niveles para {lugar}» se ve.</summary>
        public bool StopRemainingShown => _remaining.gameObject.activeSelf;

        /// <summary>Texto de «Faltan…» (vacío sin parada siguiente).</summary>
        public string StopRemainingText => _remainingLabel.text;

        /// <summary>Caja de «Faltan…».</summary>
        public RectTransform StopRemainingRect => _remaining;

        /// <summary>Texto de «Faltan…» (en coordenadas de su placa, <see cref="StopRemainingRect"/>).</summary>
        public TextMeshProUGUI StopRemainingLabel => _remainingLabel;

        /// <summary>Hueco del Muestrario (M4.1), oculto hasta que exista.</summary>
        public RectTransform SamplesSlot => _samplesSlot;

        /// <summary>Ancho del lienzo en dp.</summary>
        public float CanvasWidth => _canvasW;

        /// <summary>Alto del lienzo en dp.</summary>
        public float CanvasHeight => _canvasH;

        /// <summary>Zona segura superior aplicada (dp).</summary>
        public float SafeTop => _safeTop;

        /// <summary>Zona segura inferior aplicada (dp).</summary>
        public float SafeBottom => _safeBottom;

        /// <summary>Se está arrastrando.</summary>
        public bool Dragging => _dragging;

        /// <summary>
        /// Intervalo de <see cref="OnDemandRendering.renderFrameInterval"/> en reposo: el objetivo de fotogramas del juego
        /// (<see cref="Application.targetFrameRate"/>, 60) entre <see cref="PerfTargetFpsMenus"/> (2: 30 fps).
        /// </summary>
        public static int RestRenderInterval
        {
            get
            {
                int target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
                return Mathf.Max(1, Mathf.RoundToInt(target / (float)PerfTargetFpsMenus));
            }
        }

        /// <summary>Todos los textos de la pantalla (tests: ≥ 12 sp y sin recortes).</summary>
        internal IEnumerable<TextMeshProUGUI> Texts()
        {
            yield return _title;
            yield return _viewWorld.Label;
            yield return _viewBasin.Label;
            if (_remaining.gameObject.activeSelf)
            {
                yield return _remainingLabel;
            }

            if (_backToCanoe.Button.gameObject.activeSelf)
            {
                yield return _backToCanoeLabel;
            }

            yield return _playLabel;
        }

        /// <summary>
        /// Pares texto / fondo de la pantalla, para la comprobación de contraste ≥ 4,5:1 (07 §1.2 y §7.8): la cabecera y «Faltan…»
        /// sobre madera oscura, las pestañas del conmutador (la elegida, madera oscura sobre crema; la otra, crema sobre la cápsula de
        /// madera), «Volver a la canoa» sobre madera y «Jugar» sobre hoja.
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            return new[]
            {
                ("cabecera: pregunta del mundo", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("pestaña elegida", SegmentSelectedText, SegmentSelectedPlate),
                ("pestaña", SegmentText, SegmentSwitchColor),
                ("faltan N", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("volver a la canoa", (Color)Palette.Cream, Palette.FamilyWood.Base),
                ("volver a la canoa (fondo de la cara)", (Color)Palette.Cream, Palette.FamilyWood.Deep),
                ("jugar", (Color)Palette.Cream, Palette.FamilyLeaf.Base),
                ("jugar (fondo de la cara)", (Color)Palette.Cream, Palette.FamilyLeaf.Deep),
            };
        }

        /// <summary>
        /// Abre el Mapa desde <paramref name="source"/> en la vista Mundo, con el estado de <paramref name="progress"/>
        /// (<c>max_level</c> deja la canoa: en la parada recién alcanzada o a la décima del tramo siguiente) y «Jugar» en
        /// <paramref name="playLevel"/> (<paramref name="playAvailable"/> = el nivel está publicado; si no, sin «Faltan…»). Las
        /// mallas se construyen aquí (una vez por Mapa) y al cambiar de vista; nunca por fotograma.
        /// </summary>
        public void Open(MapOpenSource source, RiverMap map, RiverRoutes routes, RiverStopCatalog stops, IMapProgress progress, int playLevel, bool playAvailable)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _routes = routes ?? throw new ArgumentNullException(nameof(routes));
            _stops = stops ?? throw new ArgumentNullException(nameof(stops));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _source = source;
            _playLevel = Mathf.Clamp(playLevel, 1, WorldTable.LastLevel);
            _world = WorldTable.WorldOf(_playLevel);
            _viewChanges = 0;
            _stopsTapped = 0;
            _northUp = false;
            _dragging = false;
            _activeMs = 0f;
            _openedAt = Time.realtimeSinceStartup;

            _canvas.Canvas.gameObject.SetActive(true);
            EnsureGeometry(map);
            _hasCanoe = TryCanoePoint(map, routes, progress.MaxLevel, out _canoeMap);
            _canoe.sprite = _art.MapCanoe;
            _canoe.gameObject.SetActive(_hasCanoe);
            ApplyTexts(playAvailable);
            MapView view = MapView.World;
            ModesOpen(source);
            SequenceOpen(source, ref view);
            NorthUpOpen();
            ProgressOpen(progress);
            MarkersOpen(progress);
            CatchUpOpen(progress);
            ApplyScreenMetrics();
            ApplyView(view);
            EmitLayout();
        }

        /// <summary>
        /// Cierra el Mapa: lo oculta, devuelve el render a 60 fps y da las cifras de la sonda <c>map_close</c>. Sin efecto si no
        /// estaba abierto (cifras a cero).
        /// </summary>
        public MapCloseStats Close()
        {
            if (!Visible)
            {
                return new MapCloseStats(_source, 0, 0, 0, false);
            }

            if (_progress != null)
            {
                CatchUpClose(_progress);
            }

            SequenceClose();
            NorthUpClose();

            _dragging = false;
            _activeMs = 0f;
            int dwell = Mathf.Max(0, Mathf.RoundToInt((Time.realtimeSinceStartup - _openedAt) * 1000f));
            _canvas.Canvas.gameObject.SetActive(false);
            SetRenderInterval(1);
            return new MapCloseStats(_source, dwell, _stopsTapped, _viewChanges, _northUp);
        }

        /// <summary>
        /// Cambia a la vista <paramref name="view"/> (las pestañas del pie): reconstruye las mallas a la escala nueva y centra en
        /// la región del mundo (Mundo) o en la cuenca (Cuenca). Cuenta el cambio para <c>map_close</c>. Sin efecto si ya está.
        /// </summary>
        public void SetView(MapView view)
        {
            if (!Visible || view == _view)
            {
                return;
            }

            _viewChanges++;
            ApplyView(view);
            EmitLayout();
        }

        /// <summary>
        /// Arrastra el contenido <paramref name="deltaDp"/> dp (y hacia arriba, como el puntero), sin salirse del mapa: en el eje
        /// en que el mapa cabe entero, se queda centrado. Sin asignaciones.
        /// </summary>
        public void Drag(Vector2 deltaDp)
        {
            if (!Visible)
            {
                return;
            }

            SetOffset(new Vector2(_offset.x + deltaDp.x, _offset.y - deltaDp.y));
            _activeMs = ActiveLingerMs;
        }

        /// <summary>«Volver a la canoa»: centra la vista en la canoa, sin cambiar de vista.</summary>
        public void BackToCanoe()
        {
            if (!Visible || !_hasCanoe)
            {
                return;
            }

            SetOffset(CanoeCenteredOffset());
            _activeMs = ActiveLingerMs;
            EmitLayoutIfCanoeButtonChanged(); // «Volver a la canoa» se fue: AF_LAYOUT deja de publicarlo
        }

        /// <summary>
        /// Avanza el Mapa <paramref name="dtMs"/> milisegundos: métricas de pantalla (solo si cambian), los ganchos de lo que se
        /// anima y la frecuencia de pintado (60 fps arrastrando o animando, <see cref="PerfTargetFpsMenus"/> en reposo). Con el
        /// Mapa quieto no asigna memoria ni toca ninguna malla.
        /// </summary>
        public void Tick(float dtMs)
        {
            if (!Visible)
            {
                return;
            }

            GameScreen.ReadScreen(out int sw, out int sh, out Rect safe);
            if (sw != _screenW || sh != _screenH || safe != _safeArea)
            {
                ApplyScreenMetrics();
                ApplyView(_view);
                EmitLayout();
            }

            bool animating = false;
            ProgressTick(dtMs, ref animating);
            MarkersTick(dtMs, ref animating);
            CatchUpTick(dtMs, ref animating);
            SequenceTick(dtMs, ref animating);
            NorthUpTick(dtMs, ref animating);
            if (_activeMs > 0f)
            {
                _activeMs -= dtMs;
            }

            SetRenderInterval(_dragging || animating || _activeMs > 0f ? 1 : RestRenderInterval);
        }

        // ============================================================================================ geometría

        /// <summary>
        /// Dónde está la canoa con <paramref name="maxLevel"/> niveles ganados (D-013, punto 3; propuesta del Mapa §3.4): en un
        /// <c>10k</c>, en la parada k recién alcanzada; si no, a (max mod 10) / 10 de la ruta de la parada siguiente, por el agua.
        /// Más allá del catálogo, en su última parada. False sin rutas. Sin asignaciones.
        /// </summary>
        public static bool TryCanoePoint(RiverMap map, RiverRoutes routes, int maxLevel, out Vector2 point)
        {
            if (map == null || routes == null || routes.Count == 0)
            {
                point = default;
                return false;
            }

            int max = Mathf.Max(0, maxLevel);
            int arrived = max / RiverStopCatalog.LevelsPerStop;
            int rest = max % RiverStopCatalog.LevelsPerStop;
            RiverStopRoute? next = rest > 0 ? routes.At(arrived + 1) : null;
            if (next != null)
            {
                point = RoutePoint(map, next, rest / (float)RiverStopCatalog.LevelsPerStop);
                return true;
            }

            RiverStopRoute at = routes.At(Mathf.Min(arrived, routes.Count - 1))!;
            point = PlacePoint(map, at.At);
            return true;
        }

        /// <summary>Punto de <paramref name="place"/> en unidades del marco. Sin asignaciones.</summary>
        public static Vector2 PlacePoint(RiverMap map, RiverMapPlace place)
        {
            map.Lines[place.Line].PointAt(place.S, out float x, out float y);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Punto a la fracción <paramref name="fraction"/> (0-1, por longitud de arco) de la ruta <paramref name="route"/>, por sus
        /// tramos de agua en orden (también los que remontan). Sin largo, la parada. Sin asignaciones.
        /// </summary>
        public static Vector2 RoutePoint(RiverMap map, RiverStopRoute route, float fraction)
        {
            IReadOnlyList<RiverPathPiece> path = route.Path;
            float total = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                total += Mathf.Abs(piece.S1 - piece.S0) * map.Lines[piece.Line].Length;
            }

            if (total <= 0f)
            {
                return PlacePoint(map, route.At);
            }

            float target = Mathf.Clamp01(fraction) * total;
            for (int i = 0; i < path.Count; i++)
            {
                RiverPathPiece piece = path[i];
                RiverLine line = map.Lines[piece.Line];
                float length = Mathf.Abs(piece.S1 - piece.S0) * line.Length;
                if (target <= length || i == path.Count - 1)
                {
                    float t = length > 0f ? Mathf.Clamp01(target / length) : 1f;
                    line.PointAt(piece.S0 + ((piece.S1 - piece.S0) * t), out float x, out float y);
                    return new Vector2(x, y);
                }

                target -= length;
            }

            return PlacePoint(map, route.At);
        }

        /// <summary>
        /// Zoom de la vista Mundo [P] para una zona de <paramref name="viewW"/> × <paramref name="viewH"/> dp: la caja de las
        /// paradas del mundo <paramref name="world"/> (y de la canoa, si la hay) ocupa <see cref="WorldFill"/> de la zona, entre
        /// <see cref="WorldZoomMin"/> y <see cref="WorldZoomMax"/>; a 1× la cuenca mide <see cref="MapSpace.BasinWidthDp"/> dp de
        /// ancho (13,1 dp por grado; propuesta del Mapa §3.1).
        /// <paramref name="center"/> es el centro de esa caja, en unidades del marco.
        /// </summary>
        public static float WorldZoom(RiverMap map, RiverRoutes routes, RiverStopCatalog stops, int world, bool hasCanoe, Vector2 canoe,
            float viewW, float viewH, out Vector2 center)
        {
            float x0 = float.MaxValue;
            float y0 = float.MaxValue;
            float x1 = float.MinValue;
            float y1 = float.MinValue;
            for (int n = 0; n < routes.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                RiverStopRoute? route = routes.At(n);
                if (stop == null || route == null || stop.World != world)
                {
                    continue;
                }

                Vector2 p = PlacePoint(map, route.At);
                x0 = Mathf.Min(x0, p.x);
                y0 = Mathf.Min(y0, p.y);
                x1 = Mathf.Max(x1, p.x);
                y1 = Mathf.Max(y1, p.y);
            }

            if (hasCanoe)
            {
                x0 = Mathf.Min(x0, canoe.x);
                y0 = Mathf.Min(y0, canoe.y);
                x1 = Mathf.Max(x1, canoe.x);
                y1 = Mathf.Max(y1, canoe.y);
            }

            if (x0 > x1)
            {
                center = new Vector2(map.Width * 0.5f, map.Height * 0.5f);
                return WorldZoomMin;
            }

            center = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            float bw = Mathf.Max(x1 - x0, MinZoomExtent) * MapSpace.BasinWidthDp;
            float bh = Mathf.Max(y1 - y0, MinZoomExtent) * MapSpace.BasinWidthDp;
            float zoom = Mathf.Min(viewW * WorldFill / bw, viewH * WorldFill / bh);
            return Mathf.Clamp(zoom, WorldZoomMin, WorldZoomMax);
        }

        // Una cinta de vidrio por línea del Mapa (toda la cuenca, una vez por Mapa) y la tierra. Asigna: solo al abrir.
        private void EnsureGeometry(RiverMap map)
        {
            if (ReferenceEquals(map, _builtFor))
            {
                return;
            }

            _builtFor = map;
            IReadOnlyList<RiverLine> lines = map.Lines;
            while (_glass.Count < lines.Count)
            {
                var go = new GameObject("Glass_" + _glass.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(RiverRibbonGraphic));
                go.transform.SetParent(_glassLayer, false);
                FillTopLeft((RectTransform)go.transform);
                RiverRibbonGraphic ribbon = go.GetComponent<RiverRibbonGraphic>();
                ribbon.Setup();
                _glass.Add(ribbon);
            }

            for (int i = 0; i < _glass.Count; i++)
            {
                RiverRibbonGraphic ribbon = _glass[i];
                bool used = i < lines.Count;
                ribbon.gameObject.SetActive(used);
                ribbon.SetLine(used ? lines[i] : null);
                if (used)
                {
                    ribbon.gameObject.name = "Glass_" + lines[i].Id;
                    ribbon.SetLook(RibbonLook.Glass);
                    ribbon.SetFill(0f, 1f);
                }
            }

            ProgressBuild(_staticLayer, _dynamicLayer);
            MarkersBuild(_dynamicLayer);
        }

        /// <summary>
        /// Ancho de la cinta de una línea de clase <paramref name="lineClass"/> en la vista <paramref name="view"/>: en Mundo, a
        /// 4×-15×, el río es el protagonista y la jerarquía cauce / afluente / río local tiene que leerse a tamaño de teléfono
        /// (<see cref="WorldWidthClass1Dp"/>, <see cref="WorldWidthClass2Dp"/> y <see cref="WorldWidthClass3Dp"/>); en Cuenca, a
        /// 1×, el de la clase (<see cref="RiverRibbonGraphic.WidthForClass"/>).
        /// </summary>
        public static float RibbonWidthDp(MapView view, int lineClass)
        {
            if (view != MapView.World)
            {
                return RiverRibbonGraphic.WidthForClass(lineClass);
            }

            switch (lineClass)
            {
                case 1:
                    return WorldWidthClass1Dp;
                case 2:
                    return WorldWidthClass2Dp;
                default:
                    return WorldWidthClass3Dp;
            }
        }

        // La vista: escala, mallas a esa escala (solo si cambia), la canoa y el encuadre.
        private void ApplyView(MapView view)
        {
            _view = view;
            _viewWorld.SetSelected(view == MapView.World);
            _viewBasin.SetSelected(view == MapView.Basin);
            NorthUpView(view);
            RiverMap? map = _map;
            if (map == null || _routes == null || _stops == null)
            {
                return;
            }

            Vector2 center;
            if (view == MapView.World)
            {
                _zoom = WorldZoom(map, _routes, _stops, _world, _hasCanoe, _canoeMap, _viewW, _viewH, out center);
            }
            else
            {
                _zoom = 1f;
                center = new Vector2(map.Width * 0.5f, map.Height * 0.5f);
            }

            var space = new MapSpace(MapSpace.BasinWidthDp * _zoom, Vector2.zero);
            _space = space;
            _contentW = space.DpPerUnit * map.Width;
            _contentH = space.DpPerUnit * map.Height;
            UiBuilder.Place(_content, 0f, 0f, _contentW, _contentH);
            _land.SetLand(map.Land, space);
            for (int i = 0; i < _glass.Count; i++)
            {
                RiverRibbonGraphic ribbon = _glass[i];
                if (ribbon.gameObject.activeSelf && ribbon.Line != null)
                {
                    ribbon.SetSpace(space);
                    ribbon.SetWidth(RibbonWidthDp(view, ribbon.Line.Class));
                }
            }

            if (_hasCanoe)
            {
                Vector2 local = space.ToLocal(_canoeMap);
                RectTransform rt = _canoe.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.35f);
                rt.anchoredPosition = local;
                rt.sizeDelta = new Vector2(ProceduralArt.MapCanoeWidthDp, ProceduralArt.MapCanoeHeightDp);
            }

            ProgressSpace(space);
            MarkersSpace(space);
            SequenceSpace(space);
            NorthUpSpace(space);
            Vector2 c = space.ToLocal(center);
            SetOffset(new Vector2((_viewW * 0.5f) - c.x, (_viewH * 0.5f) + c.y));
        }

        // Coloca el contenido con su esquina en offset (dp, y hacia abajo), sin salirse del mapa, y decide «Volver a la canoa»: en
        // cualquier vista (también en la Cuenca, que en un teléfono de 640 dp no cabe entera), si la canoa queda fuera y el botón
        // movería la vista (con la canoa pegada al borde del mapa, la vista ya está lo más cerca que puede: ningún botón que no
        // hace nada). Sin asignaciones.
        private void SetOffset(Vector2 offset)
        {
            _offset = new Vector2(ClampAxis(offset.x, _contentW, _viewW), ClampAxis(offset.y, _contentH, _viewH));
            _content.anchoredPosition = new Vector2(_offset.x, -_offset.y);
            bool show = false;
            if (_hasCanoe && !CanoeInView)
            {
                Vector2 target = CanoeCenteredOffset();
                show = Mathf.Abs(target.x - _offset.x) > 0.5f || Mathf.Abs(target.y - _offset.y) > 0.5f;
            }

            if (_backToCanoe.Button.gameObject.activeSelf != show)
            {
                _backToCanoe.Button.gameObject.SetActive(show);
            }
        }

        // Esquina del contenido que deja la canoa en el centro de la vista (o lo más cerca que deja el borde del mapa).
        private Vector2 CanoeCenteredOffset()
        {
            Vector2 local = _space.ToLocal(_canoeMap);
            return new Vector2(ClampAxis((_viewW * 0.5f) - local.x, _contentW, _viewW), ClampAxis((_viewH * 0.5f) + local.y, _contentH, _viewH));
        }

        // Un eje: el contenido cubre la vista (de vista − contenido a 0) o, si cabe, queda centrado.
        private static float ClampAxis(float value, float content, float view)
        {
            if (content <= view)
            {
                return (view - content) * 0.5f;
            }

            return Mathf.Clamp(value, view - content, 0f);
        }

        // ============================================================================================ textos

        // Cabecera, «Faltan…» y «Jugar» al abrir (los textos con argumentos asignan su cadena: solo aquí).
        private void ApplyTexts(bool playAvailable)
        {
            RiverStopCatalog stops = _stops!;
            RiverStop? first = stops.FirstStopOfWorld(_world);
            string? questionKey = first?.WorldQuestion;
            string question = questionKey != null ? Loc.Fact(questionKey) : string.Empty;
            _title.text = question.Length > 0 ? UiText.MapWorldQuestion(_world, question) : UiText.WorldName(_world);
            _answered = question.Length > 0 && WorldQuestionAnswered(stops, _world, _progress!.MaxLevel);
            _check.gameObject.SetActive(_answered);

            // «Faltan N» hacia la parada siguiente, con su nombre en el idioma (en release, sin el visto bueno del asesor es «»).
            RiverStop? current = stops.StopOf(_playLevel);
            RiverStop? next = current != null ? stops.At(current.N + 1) : null;
            string place = next != null ? UiText.StopName(next) : string.Empty;
            _remainingReady = playAvailable && next != null && place.Length > 0;
            _remainingLabel.text = _remainingReady ? UiText.MapStopRemaining(RiverJourney.LevelsLeft(_playLevel), place) : string.Empty;
            _remaining.gameObject.SetActive(_remainingReady);
            _playLabel.text = UiText.PlayLevel(_playLevel);
        }

        /// <summary>
        /// La pregunta del mundo <paramref name="world"/> ya se respondió con <paramref name="maxLevel"/> niveles ganados: se llegó
        /// (se ganó su <c>10k</c>) a una parada del mundo con <c>answers_world_question</c> (D-013, punto 1). Sin asignaciones.
        /// </summary>
        public static bool WorldQuestionAnswered(RiverStopCatalog stops, int world, int maxLevel)
        {
            for (int n = 1; n < stops.Count; n++)
            {
                RiverStop? stop = stops.At(n);
                if (stop != null && stop.World == world && stop.AnswersWorldQuestion && maxLevel >= n * RiverStopCatalog.LevelsPerStop)
                {
                    return true;
                }
            }

            return false;
        }

        // ============================================================================================ maquetación

        private void ApplyScreenMetrics()
        {
            GameScreen.ReadScreen(out _screenW, out _screenH, out _safeArea);
            _canvas.Measure();
            _canvasW = _canvas.Width;
            _canvasH = _canvas.Height;
            _scale = Mathf.Max(0.01f, _screenW / _canvasW);
            _safeTop = Mathf.Max(GameScreen.MinSafeTop, (_screenH - _safeArea.yMax) / _scale);
            _safeBottom = Mathf.Max(GameScreen.MinSafeBottom, _safeArea.yMin / _scale);
            Layout();
        }

        private void Layout()
        {
            float w = _canvasW;
            float h = _canvasH;

            // Cabecera: la placa cubre también la barra de estado y acaba en la zona segura + HeaderDp. Sus hijos van en coordenadas
            // de la placa, que empieza 8 dp por encima y a la izquierda del lienzo, así que la banda de la cabecera empieza en
            // bandTop. El ← y el ✓ van centrados en la banda y el título deja 4 dp arriba y abajo: todo queda dentro de la placa.
            UiBuilder.Place(_headerPlate.rectTransform, -8f, -8f, w + 16f, _safeTop + HeaderDp + 8f);
            float bandTop = _safeTop + 8f;
            UiBuilder.Place((RectTransform)_back.Button.transform, 16f, bandTop + ((HeaderDp - MinTouchDp) * 0.5f), MinTouchDp, MinTouchDp);
            float checkSpace = _check.gameObject.activeSelf ? CheckDp + 8f : 0f;
            float titleX = 16f + MinTouchDp + 8f;
            float titleW = Mathf.Max(48f, w + 16f - titleX - 16f - checkSpace);
            UiBuilder.Place(_title.rectTransform, titleX, bandTop + 4f, titleW, HeaderDp - 8f);
            UiBuilder.Place(_check.rectTransform, titleX + titleW + 8f, bandTop + ((HeaderDp - CheckDp) * 0.5f), CheckDp, CheckDp);

            // Pie: «Jugar» abajo y, encima, el conmutador y «Faltan…».
            float playW = Mathf.Min(PlayMaxWidth, w - 64f);
            float playTop = h - _safeBottom - GapDp - PlayDp;
            UiBuilder.Place((RectTransform)_play.Button.transform, (w - playW) * 0.5f, playTop, playW, PlayDp);
            // La fila es una sola banda de alto rowH: la cápsula del conmutador (las pestañas a todo su alto, con 4 dp de relleno) y
            // «Faltan…», que va en dos líneas si hace falta (y entonces la fila mide TwoLineRowDp) y, si ni así cabe a su mínimo
            // (texto grande), hace crecer la fila (02 §12). El aire se mide desde el filete de la placa, no desde su borde:
            // RemainingLabelXDp a cada lado y RowPadDp arriba y abajo.
            float switchW = (2f * SegmentDp) + (2f * SwitchPadDp);
            float remainingX = 16f + switchW + GapDp;
            float remainingW = Mathf.Max(48f, w - 16f - remainingX);
            float labelW = Mathf.Max(24f, remainingW - (2f * RemainingLabelXDp));
            float rowH = RowDp;
            if (!string.IsNullOrEmpty(_remainingLabel.text))
            {
                if (_remainingLabel.GetPreferredValues(_remainingLabel.text, float.PositiveInfinity, float.PositiveInfinity).x > labelW)
                {
                    rowH = TwoLineRowDp; // a su tamaño de siempre no cabe en una línea: va en dos
                }

                rowH = Mathf.Max(rowH, Mathf.Ceil(UiBuilder.MinTextHeight(_remainingLabel, labelW)) + (2f * RowPadDp));
            }

            float rowTop = playTop - GapDp - rowH;
            UiBuilder.Place(_viewSwitch.rectTransform, 16f, rowTop, switchW, rowH);
            _viewWorld.Place(16f + SwitchPadDp, rowTop, SegmentDp, rowH);
            _viewBasin.Place(16f + SwitchPadDp + SegmentDp, rowTop, SegmentDp, rowH);
            UiBuilder.Place(_remaining, remainingX, rowTop, remainingW, rowH);
            UiBuilder.Place(_remainingLabel.rectTransform, RemainingLabelXDp, RowPadDp, labelW, rowH - (2f * RowPadDp));
            UiBuilder.Place(_samplesSlot, remainingX, rowTop, remainingW, rowH);
            SamplesLayout(_samplesSlot);

            // Zona del mapa: entre la cabecera y el pie, en su marco.
            float frameTop = _safeTop + HeaderDp + 8f;
            float frameBottom = rowTop - 6f;
            float frameW = w - (2f * FrameInsetDp);
            float frameH = Mathf.Max(2f * FramePadDp + 48f, frameBottom - frameTop);
            UiBuilder.Place(_frame.rectTransform, FrameInsetDp, frameTop, frameW, frameH);
            _viewW = frameW - (2f * FramePadDp);
            _viewH = frameH - (2f * FramePadDp);
            UiBuilder.Place(_viewport, FramePadDp, FramePadDp, _viewW, _viewH);
            UiBuilder.Place(_frameCorners.rectTransform, FramePadDp, FramePadDp, _viewW, _viewH);

            // «Volver a la canoa», abajo a la derecha de la zona del mapa, a la medida de su texto.
            float canoeLabelW = Mathf.Ceil(UiBuilder.MinTextWidth(_backToCanoeLabel));
            float buttonW = Mathf.Min(_viewW - 16f, Mathf.Max(MinTouchDp * 2f, canoeLabelW + 32f));
            UiBuilder.Place((RectTransform)_backToCanoe.Button.transform, FramePadDp + _viewW - buttonW - 8f, FramePadDp + _viewH - MinTouchDp - 8f, buttonW, MinTouchDp);
            SequenceLayout();
            NorthUpLayout();
        }

        private void EmitLayout()
        {
            _layoutCanoeButton = _backToCanoe.Button.gameObject.activeSelf;
            if (!DevProbe.Enabled || !Visible)
            {
                return;
            }

            var ids = new List<string>(8) { "back", "view_world", "view_basin", "play" };
            var rects = new List<RectTransform>(8)
            {
                (RectTransform)_back.Button.transform,
                (RectTransform)_viewWorld.Button.transform,
                (RectTransform)_viewBasin.Button.transform,
                (RectTransform)_play.Button.transform,
            };
            if (_backToCanoe.Button.gameObject.activeSelf)
            {
                ids.Add("back_to_canoe");
                rects.Add((RectTransform)_backToCanoe.Button.transform);
            }

            if (_remaining.gameObject.activeSelf)
            {
                ids.Add("stop_remaining");
                rects.Add(_remaining);
            }

            MarkersLayoutIds(ids, rects);
            SamplesLayoutIds(ids, rects);
            SequenceLayoutIds(ids, rects);
            NorthUpLayoutIds(ids, rects);
            _probe.Layout("river_map", _playLevel, null, ids.ToArray(), rects.ToArray());
        }

        // Arrastre del puntero (DragArea), en píxeles de pantalla: a dp con la escala del lienzo.
        private void BeginDrag()
        {
            _dragging = true;
            SetRenderInterval(1);
        }

        private void DragPixels(Vector2 pixels)
        {
            Drag(pixels / _scale);
        }

        private void EndDrag()
        {
            _dragging = false;
            _activeMs = ActiveLingerMs;
            EmitLayoutIfCanoeButtonChanged();
        }

        // AF_LAYOUT solo se reescribe (al soltar el arrastre o tras «Volver a la canoa») si «Volver a la canoa» apareció o
        // desapareció desde el último que se escribió (nunca por fotograma).
        private void EmitLayoutIfCanoeButtonChanged()
        {
            if (_backToCanoe.Button.gameObject.activeSelf != _layoutCanoeButton)
            {
                EmitLayout();
            }
        }

        /// <summary>
        /// Devuelve el pintado a todos los fotogramas (60 fps), para quien se va con el Mapa quizá abierto sin pasar por
        /// <see cref="Close"/>: <see cref="GameBootstrap"/> al destruirse la escena (una recarga con el Mapa delante), para que el
        /// nivel siguiente no pinte a 30 fps.
        /// </summary>
        public static void RestoreFullRenderRate()
        {
            SetRenderInterval(1);
        }

        private static void SetRenderInterval(int interval)
        {
            if (OnDemandRendering.renderFrameInterval != interval)
            {
                OnDemandRendering.renderFrameInterval = interval;
            }
        }

        // Lienzo anidado (sin overrideSorting, para que lo recorte la máscara de la zona) estirado al contenido.
        private static Canvas NestedCanvas(string name, RectTransform parent, bool raycaster, out RectTransform layer)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);
            layer = (RectTransform)go.transform;
            FillTopLeft(layer);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = false;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            if (raycaster)
            {
                go.AddComponent<GraphicRaycaster>(); // los marcadores tocables (M3.5) viven en este lienzo
            }

            return canvas;
        }

        // Estira rt a su padre con el pivote arriba a la izquierda: el origen de MapSpace.
        private static void FillTopLeft(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ============================================================================================ ganchos (tareas siguientes)

        /// <summary>M3.4: al construir la geometría de un Mapa (una vez), sus capas del lleno y del frente.</summary>
        partial void ProgressBuild(RectTransform staticLayer, RectTransform dynamicLayer);

        /// <summary>M3.4: al abrir, el lleno hasta <c>max_level</c>.</summary>
        partial void ProgressOpen(IMapProgress progress);

        /// <summary>M3.4: la vista cambió de escala.</summary>
        partial void ProgressSpace(MapSpace space);

        /// <summary>M3.4: cada fotograma con el Mapa abierto; <paramref name="animating"/> = true pide 60 fps.</summary>
        partial void ProgressTick(float dtMs, ref bool animating);

        /// <summary>M3.5: al construir la geometría, los marcadores (canoa con banderas, guía, mini cuias, baliza y casa).</summary>
        partial void MarkersBuild(RectTransform dynamicLayer);

        /// <summary>M3.5: al abrir.</summary>
        partial void MarkersOpen(IMapProgress progress);

        /// <summary>M3.5: la vista cambió de escala.</summary>
        partial void MarkersSpace(MapSpace space);

        /// <summary>M3.5: cada fotograma con el Mapa abierto.</summary>
        partial void MarkersTick(float dtMs, ref bool animating);

        /// <summary>M3.5: los marcadores tocables en <c>AF_LAYOUT</c>.</summary>
        partial void MarkersLayoutIds(List<string> ids, List<RectTransform> rects);

        /// <summary>M3.5: al abrir, «ponerse al día» entre <c>map_seen_level</c> y <c>max_level</c>.</summary>
        partial void CatchUpOpen(IMapProgress progress);

        /// <summary>M3.5: cada fotograma de «ponerse al día».</summary>
        partial void CatchUpTick(float dtMs, ref bool animating);

        /// <summary>M3.5: al cerrar.</summary>
        partial void CatchUpClose(IMapProgress progress);

        /// <summary>M4.1: el Muestrario en su hueco del pie (una vez, al construir la pantalla).</summary>
        partial void SamplesBuild(RectTransform slot);

        /// <summary>M4.1: el hueco del Muestrario se recolocó.</summary>
        partial void SamplesLayout(RectTransform slot);

        /// <summary>M4.1: el Muestrario en <c>AF_LAYOUT</c>.</summary>
        partial void SamplesLayoutIds(List<string> ids, List<RectTransform> rects);

        /// <summary>M4.3: la entrada (pausa, victoria, fin de contenido) y el modo <c>ui_map_mode</c> al abrir.</summary>
        partial void ModesOpen(MapOpenSource source);

        /// <summary>
        /// M4.2: la secuencia del Mapa del 10, al construir la pantalla (una vez): sus piezas sobre la UI (<paramref name="root"/>:
        /// el bocadillo y saltar) y sobre el mapa (<paramref name="dynamicLayer"/>: TiTino en su casa).
        /// </summary>
        partial void SequenceBuild(RectTransform root, RectTransform dynamicLayer);

        /// <summary>
        /// M4.2: al abrir, tras <see cref="ModesOpen"/>. Si la secuencia arranca (<paramref name="source"/> del Mapa del 10), elige
        /// la vista con que se abre (<paramref name="view"/>: Mundo por defecto; la secuencia, Cuenca).
        /// </summary>
        partial void SequenceOpen(MapOpenSource source, ref MapView view);

        /// <summary>M4.2: la vista cambió de escala (también al abrir y al cambiar las métricas de la pantalla).</summary>
        partial void SequenceSpace(MapSpace space);

        /// <summary>M4.2: cada fotograma con el Mapa abierto (la canoa que sube a Santarém); <paramref name="animating"/> = true pide 60 fps.</summary>
        partial void SequenceTick(float dtMs, ref bool animating);

        /// <summary>M4.2: la pantalla se recolocó (al final de la maquetación).</summary>
        partial void SequenceLayout();

        /// <summary>M4.2: los controles de la secuencia (saltar) en <c>AF_LAYOUT</c>.</summary>
        partial void SequenceLayoutIds(List<string> ids, List<RectTransform> rects);

        /// <summary>M4.2: al cerrar, antes de dar las cifras de <c>map_close</c>.</summary>
        partial void SequenceClose();

        /// <summary>M4.4: «Norte arriba» al construir la pantalla (una vez): el botón <c>map_north_up</c> en el marco de la zona del mapa.</summary>
        partial void NorthUpBuild(RectTransform frame);

        /// <summary>M4.4: al abrir, sin giro (<see cref="Open"/> ya puso <c>_northUp</c> a false).</summary>
        partial void NorthUpOpen();

        /// <summary>
        /// M4.4: en cada cambio de vista (también al abrir y al cambiar las métricas de la pantalla, con la misma vista): el botón
        /// solo en la Cuenca y, fuera de ella, sin giro.
        /// </summary>
        partial void NorthUpView(MapView view);

        /// <summary>M4.4: la vista cambió de escala.</summary>
        partial void NorthUpSpace(MapSpace space);

        /// <summary>M4.4: cada fotograma con el Mapa abierto (el giro de −90° con su escala); <paramref name="animating"/> = true pide 60 fps.</summary>
        partial void NorthUpTick(float dtMs, ref bool animating);

        /// <summary>M4.4: la pantalla se recolocó (al final de la maquetación: el botón en la zona del mapa).</summary>
        partial void NorthUpLayout();

        /// <summary>M4.4: el botón <c>map_north_up</c> en <c>AF_LAYOUT</c>.</summary>
        partial void NorthUpLayoutIds(List<string> ids, List<RectTransform> rects);

        /// <summary>M4.4: al cerrar, antes de dar las cifras de <c>map_close</c> (pone <c>_northUp</c> a true si se usó).</summary>
        partial void NorthUpClose();

        // ============================================================================================ piezas

        // Pestaña del conmutador Mundo / Cuenca, dentro de su cápsula de madera (Palette.Frame): la elegida con su placa crema y el
        // texto en madera oscura; la otra, sin placa, con el texto crema sobre la cápsula. El verde queda para «Jugar». Área ≥ 48 dp.
        private sealed class Segment
        {
            private readonly Image _plate;

            public Segment(RectTransform parent, ProceduralArt art, string name, string text, Action onClick)
            {
                Button = UiBuilder.BareButton(name, parent, onClick);
                _plate = UiBuilder.Sliced("Plate", Button.transform, art.RoundedRect, Palette.Cream);
                UiBuilder.Stretch(_plate.rectTransform);
                _plate.rectTransform.offsetMin = new Vector2(0f, 4f);
                _plate.rectTransform.offsetMax = new Vector2(0f, -4f);
                Label = UiBuilder.Label("Label", Button.transform, TextStyle.PlainBold, text, RowSp, Palette.Cream, TextAlignmentOptions.Center);
                UiBuilder.Stretch(Label.rectTransform);
                Label.rectTransform.offsetMin = new Vector2(6f, 4f);
                Label.rectTransform.offsetMax = new Vector2(-6f, -4f);
                UiBuilder.AutoSize(Label, UiBuilder.MinTextSp, RowSp);
            }

            public Button Button { get; }

            public TextMeshProUGUI Label { get; }

            public bool Selected { get; private set; }

            public Image Plate => _plate;

            public void SetSelected(bool selected)
            {
                Selected = selected;
                if (_plate.enabled != selected)
                {
                    _plate.enabled = selected;
                }

                Color text = selected ? SegmentSelectedText : SegmentText;
                if (Label.color != text)
                {
                    Label.color = text;
                }
            }

            public void Place(float x, float y, float w, float h)
            {
                UiBuilder.Place((RectTransform)Button.transform, x, y, w, h);
            }
        }

        // Recibe el arrastre de la zona del mapa (el mar y lo que haya encima que no lo use) y se lo pasa a la pantalla.
        private sealed class DragArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            internal RiverMapScreen? Owner;

            public void OnBeginDrag(PointerEventData eventData)
            {
                Owner?.BeginDrag();
            }

            public void OnDrag(PointerEventData eventData)
            {
                Owner?.DragPixels(eventData.delta);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                Owner?.EndDrag();
            }
        }

        /// <summary>Glifo procedural de la pantalla del Mapa (ninguna de las dos familias tiene ✓ ni una flecha de volver; B1).</summary>
        internal enum GlyphKind
        {
            /// <summary>✓ de la pregunta del mundo respondida: verde con contorno oscuro y halo crema.</summary>
            Check = 0,

            /// <summary>← de la cabecera: crema con contorno de madera.</summary>
            BackArrow = 1,
        }

        /// <summary>
        /// Glifo dibujado con trazos (quads), sin textura ni fuente: el ✓ y el ← de la cabecera del Mapa (P-M2: el ✓ es un icono, nunca
        /// texto). Las coordenadas de los trazos van de 0 a 1 en su caja, con y hacia arriba. Sin raycast.
        /// </summary>
        [RequireComponent(typeof(CanvasRenderer))]
        internal sealed class GlyphGraphic : MaskableGraphic
        {
            /// <summary>Verde del ✓ (<c>hoja_media</c>).</summary>
            public static readonly Color CheckGreen = Palette.Hex(0x23824A);

            private GlyphKind _kind;

            /// <summary>Qué glifo dibuja.</summary>
            public GlyphKind Kind => _kind;

            /// <summary>Crea el glifo bajo <paramref name="parent"/>.</summary>
            public static GlyphGraphic Create(string name, Transform parent, GlyphKind kind)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(GlyphGraphic));
                go.transform.SetParent(parent, false);
                GlyphGraphic glyph = go.GetComponent<GlyphGraphic>();
                glyph._kind = kind;
                glyph.raycastTarget = false;
                glyph.color = Color.white;
                glyph.SetVerticesDirty();
                return glyph;
            }

            /// <inheritdoc />
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                Rect r = rectTransform.rect;
                float s = Mathf.Min(r.width, r.height);
                if (s <= 0f)
                {
                    return;
                }

                if (_kind == GlyphKind.Check)
                {
                    var a = new Vector2(0.16f, 0.50f);
                    var b = new Vector2(0.40f, 0.24f);
                    var c = new Vector2(0.86f, 0.78f);
                    Stroke(vh, r, a, b, 0.17f * s, Palette.Cream);
                    Stroke(vh, r, b, c, 0.17f * s, Palette.Cream);
                    Stroke(vh, r, a, b, 0.12f * s, Palette.Vein);
                    Stroke(vh, r, b, c, 0.12f * s, Palette.Vein);
                    Stroke(vh, r, a, b, 0.075f * s, CheckGreen);
                    Stroke(vh, r, b, c, 0.075f * s, CheckGreen);
                    return;
                }

                var tip = new Vector2(0.18f, 0.50f);
                var tail = new Vector2(0.84f, 0.50f);
                var up = new Vector2(0.48f, 0.80f);
                var down = new Vector2(0.48f, 0.20f);
                Color rim = Palette.FamilyWood.Deep;
                Stroke(vh, r, tip, tail, 0.13f * s, rim);
                Stroke(vh, r, tip, up, 0.13f * s, rim);
                Stroke(vh, r, tip, down, 0.13f * s, rim);
                Stroke(vh, r, tip, tail, 0.085f * s, Palette.Cream);
                Stroke(vh, r, tip, up, 0.085f * s, Palette.Cream);
                Stroke(vh, r, tip, down, 0.085f * s, Palette.Cream);
            }

            // Un trazo de a a b (0-1 en la caja r) con medio ancho half (dp) y extremos cuadrados que se solapan en las uniones.
            private void Stroke(VertexHelper vh, Rect r, Vector2 a, Vector2 b, float half, Color tint)
            {
                Vector2 p = new Vector2(r.xMin + (a.x * r.width), r.yMin + (a.y * r.height));
                Vector2 q = new Vector2(r.xMin + (b.x * r.width), r.yMin + (b.y * r.height));
                Vector2 dir = q - p;
                float length = dir.magnitude;
                if (length <= 0f)
                {
                    return;
                }

                dir /= length;
                Vector2 n = new Vector2(-dir.y, dir.x) * half;
                Vector2 e = dir * (half * 0.9f);
                Color32 c = tint * color;
                int i = vh.currentVertCount;
                vh.AddVert(p - e - n, c, Vector2.zero);
                vh.AddVert(p - e + n, c, Vector2.zero);
                vh.AddVert(q + e + n, c, Vector2.zero);
                vh.AddVert(q + e - n, c, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i, i + 2, i + 3);
            }
        }

        /// <summary>
        /// Los cuatro rincones de madera de la zona del mapa (D-003): en cada esquina de su caja, lo que queda fuera de un cuarto de
        /// círculo de radio <see cref="Radius"/>, con el color del marco, para que la tierra y el vidrio recortados por el
        /// <see cref="RectMask2D"/> (en ángulo recto) sigan el filete de la madera, concéntricos con sus esquinas de fuera. El arco
        /// lleva un borde de un píxel que se desvanece (sin dientes). Se dibuja con su malla (sin textura), solo al cambiar de tamaño;
        /// sin raycast, así que el arrastre pasa al mapa.
        /// </summary>
        [RequireComponent(typeof(CanvasRenderer))]
        internal sealed class FrameCornersGraphic : MaskableGraphic
        {
            /// <summary>Tramos de cada cuarto de círculo.</summary>
            public const int ArcSegments = 12;

            private float _radius;

            /// <summary>Radio de las esquinas (dp).</summary>
            public float Radius => _radius;

            /// <summary>Crea los rincones bajo <paramref name="parent"/>, con radio <paramref name="radius"/> dp y el color <paramref name="tint"/>.</summary>
            public static FrameCornersGraphic Create(string name, Transform parent, float radius, Color tint)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(FrameCornersGraphic));
                go.transform.SetParent(parent, false);
                FrameCornersGraphic corners = go.GetComponent<FrameCornersGraphic>();
                corners._radius = Mathf.Max(0f, radius);
                corners.raycastTarget = false;
                corners.color = tint;
                corners.SetVerticesDirty();
                return corners;
            }

            /// <inheritdoc />
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                Rect r = rectTransform.rect;
                float radius = Mathf.Min(_radius, Mathf.Min(r.width, r.height) * 0.5f);
                if (radius <= 0f)
                {
                    return;
                }

                Canvas? root = canvas;
                float px = root != null && root.scaleFactor > 0f ? 1f / root.scaleFactor : 1f / ProceduralArt.UiPxPerDp;
                float half = Mathf.Min(0.5f * px, radius * 0.5f);
                Corner(vh, new Vector2(r.xMin, r.yMin), new Vector2(1f, 1f), radius, half);
                Corner(vh, new Vector2(r.xMax, r.yMin), new Vector2(-1f, 1f), radius, half);
                Corner(vh, new Vector2(r.xMax, r.yMax), new Vector2(-1f, -1f), radius, half);
                Corner(vh, new Vector2(r.xMin, r.yMax), new Vector2(1f, -1f), radius, half);
            }

            // Un rincón: abanico desde la esquina c hasta el arco (radio + half, opaco) y una franja del arco (de radio + half,
            // opaca, a radio − half, transparente). inward apunta hacia dentro de la caja en los dos ejes.
            private void Corner(VertexHelper vh, Vector2 c, Vector2 inward, float radius, float half)
            {
                Color32 solid = color;
                Color32 clear = solid;
                clear.a = 0;
                var center = new Vector2(c.x + (inward.x * radius), c.y + (inward.y * radius));
                int start = vh.currentVertCount;
                vh.AddVert(c, solid, Vector2.zero);
                for (int i = 0; i <= ArcSegments; i++)
                {
                    float a = (i / (float)ArcSegments) * (Mathf.PI * 0.5f);
                    var dir = new Vector2(-inward.x * Mathf.Cos(a), -inward.y * Mathf.Sin(a));
                    vh.AddVert(center + (dir * (radius + half)), solid, Vector2.zero);
                    vh.AddVert(center + (dir * (radius - half)), clear, Vector2.zero);
                }

                for (int i = 0; i < ArcSegments; i++)
                {
                    int outer = start + 1 + (2 * i);
                    int inner = outer + 1;
                    int nextOuter = outer + 2;
                    int nextInner = outer + 3;
                    vh.AddTriangle(start, outer, nextOuter);
                    vh.AddTriangle(outer, inner, nextInner);
                    vh.AddTriangle(outer, nextInner, nextOuter);
                }
            }
        }
    }
}
