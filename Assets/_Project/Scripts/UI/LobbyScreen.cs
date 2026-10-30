#nullable enable
using System;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Pantalla de inicio (Lobby, GDD 06 §4.7; D-008 punto 6; tarea B1-6) construida por código con la composición «el muelle del
    /// río» (D-050), que conserva la de Magic Sort (ficha §7.1: saldos arriba, botón central grande, pestañas abajo) sin su trade
    /// dress (07 §1.3; D-003: madera, hoja, agua y luz de juego; ni oro biselado, ni gemas, ni gatos). De arriba abajo:
    /// <list type="number">
    /// <item><description><b>Cielo y vegetación</b> con la barra de arriba: Semillas, Canoas «n/5» con el cronómetro mm:ss de la
    /// próxima (solo desde el 31, <c>rescue_min_level</c>; con Canoas infinitas, ∞ y el tiempo que queda) y Ajustes, que abre su
    /// pantalla (<see cref="SettingsScreen"/>; tarea L3), con Música y Sonido de un toque en columna debajo (D-046, punto 2;
    /// <see cref="AudioToggles"/>).</description></item>
    /// <item><description><b>TiTino grande</b> colgado de su rama, que entra desde la vegetación de arriba a la izquierda
    /// (<see cref="ProceduralArt.HomeBranch"/>; <see cref="HomeWindow"/> en su modo del inicio y <see cref="HomeSloth"/>), con su
    /// pose por la hora y el bocadillo que rota (D-050, punto 1).</description></item>
    /// <item><description><b>El río vivo</b> (<see cref="HomeRiver"/>) sobre la banda de agua del mundo de la parada de «Jugar»
    /// (<see cref="StopBackdrop"/>): reflejos, ondas, nenúfares, un pez y un ave (punto 2).</description></item>
    /// <item><description><b>La travesía</b> (<see cref="HomeVoyage"/>): la canoa de Capi con Rosa por la ruta punteada hacia el
    /// poste de la próxima parada, con su nombre y «Faltan {n} niveles»; tocarla abre el Mapa del río (punto 3).</description></item>
    /// <item><description><b>La Crecida</b> como poste de nivel clavado en el agua a la derecha (<see cref="StreakWidget"/>; 25+;
    /// punto 4).</description></item>
    /// <item><description><b>El muelle</b>: el chip de D-009 encima de «Jugar · Nivel {n}» (centro al 60 % del alto, 06 §4.7; sin
    /// cambiar de tamaño ni de función: D-008), el Cántaro como tinaja de barro apoyada en el muelle a la izquierda (el sitio que
    /// le guarda <see cref="PiggyAnchorRect"/>; <see cref="MarketLobbyIcons"/>) y la fila de tarjetas del día bajo «Jugar»
    /// (<see cref="DayCardSlot"/>: Reto del río, Bitácora y Liga; las coloca <see cref="GameBootstrap"/>). Sin tarjetas (antes del
    /// 10), la caja de cuias de Capi en su sitio (punto 6).</description></item>
    /// <item><description>Abajo, la barra de pestañas con «Inicio» [P] y, desde <c>max_level</c> ≥ 10, el icono del Mapa del río a
    /// su derecha (06 §4.7; D-008; tarea M3.3), con un punto si hay una llegada sin ver (<c>map_seen_level</c>).</description></item>
    /// </list>
    /// Encima, cuando toca, «El río sigue creciendo» (06 §9) o Sin canoas (04 §7.3, con sus opciones desde B2-3:
    /// <see cref="NoCanoesPanel"/>). Lienzo de 360 dp de ancho (320 en los teléfonos de ese ancho) y al menos 640 de alto. Los
    /// contadores se actualizan solo al cambiar y nada asigna memoria por fotograma (08 §5.4). Sin el catálogo de paradas, el inicio
    /// sin fondo de parada, sin travesía y sin TiTino.
    /// </summary>
    public sealed partial class LobbyScreen
    {
        /// <summary>Ancho del lienzo (dp).</summary>
        public const float ReferenceWidth = 360f;

        /// <summary>Alto mínimo del lienzo (dp): se comprueba sin solapes a 360 × 640 y a 360 × 800.</summary>
        public const float MinHeight = 640f;

        /// <summary>Centro del botón Jugar sobre el alto del lienzo (06 §4.7: «60 % del alto visual»).</summary>
        public const float PlayCenterFraction = 0.60f;

        /// <summary>Lado mínimo de un control (07 §1.2 y §7.8).</summary>
        public const float MinTouchDp = 48f;

        /// <summary>Cuerpo (sp) del nombre de la próxima parada en su letrero (M2.1; D-050), en Nunito Bold.</summary>
        public const float StopRemainingSp = HomeVoyage.NameSp;

        /// <summary>Borde izquierdo de la caja de TiTino (dp).</summary>
        public const float TitinoLeftDp = 12f;

        /// <summary>Margen de la fila de tarjetas del día a cada lado (dp).</summary>
        public const float CardGutterDp = 16f;

        /// <summary>Hueco entre dos tarjetas del día (dp).</summary>
        public const float CardGapDp = 10f;

        /// <summary>Ancho de una tarjeta del día cuando hay dos, como mucho (dp): centradas.</summary>
        public const float CardMaxWidthDp = 150f;

        /// <summary>Hueco entre «Jugar» y el disco del icono de las tarjetas, que asoma por encima de ellas (dp).</summary>
        public const float CardTopGapDp = 4f;

        private const float TopRowHeight = 48f;

        // Tamaño del cronómetro de la próxima canoa (sp).
        private const float CanoesTimerSp = 13f;
        private const float TabBarHeight = 64f;
        private const float PlayTouchHeight = 72f;
        private const float PlayMaxWidth = 296f;
        private const float ChipGapDp = 6f;
        private const float HomeTabWidth = 120f;
        private const float MapTabWidth = 64f;
        private const float MapTabGapDp = 12f;
        private const float MapTabDotDp = 14f;
        private const float OverlayPanelHeight = 236f;
        private const float MaxHorizonDp = 200f;

        private readonly ProceduralArt _art;
        private readonly DevProbe _probe;
        private readonly Action _onPlay;
        private readonly Action _onCanoes;
        private readonly Action _onSettings;
        private readonly Action _onBack;

        private readonly Canvas _canvas;
        private readonly CanvasScaler _scaler;
        private readonly RectTransform _root;

        private readonly Image _skyFill;
        private readonly Image _panorama;
        private readonly Image _dock;
        private readonly Image _crate;
        private readonly Image _branch;
        private readonly Image[] _foliage = new Image[4];
        private readonly Image _scrim;
        private readonly StopBackdrop _stopBackdrop;
        private readonly HomeRiver _river;
        private readonly HomeVoyage _voyage;

        private readonly RectTransform _seeds;
        private readonly Image _seedsIcon;
        private readonly TextMeshProUGUI _seedsText;
        private readonly Button _canoes;
        private readonly Image _canoesPlate;
        private readonly Image _canoesIcon;
        private readonly Image _canoesInfinite;
        private readonly TextMeshProUGUI _canoesCount;
        private readonly TextMeshProUGUI _canoesTimer;

        // Escala de texto con la que se crearon «n/5» y el cronómetro (UiBuilder.TextScale; tarea L4): la placa de Canoas crece
        // con ella (LayoutCanoesContent).
        private readonly float _canoesTextScale;
        private readonly ReliefParts _settings;
        private readonly AudioToggles _audio;
        private readonly RectTransform _piggySlot;

        private readonly ReliefParts _play;
        private readonly TextMeshProUGUI _playLabel;
        private readonly DifficultyChip _chip;
        private bool _playNight;

        private readonly Image _tabBar;
        private readonly RectTransform _homeTab;
        private readonly TextMeshProUGUI _homeTabLabel;
        private readonly ReliefParts _mapTab;
        private readonly RectTransform _mapTabDot;
        private bool _riverMapAvailable;
        private int _mapTabShown = -1;
        private int _mapDotShown = -1;

        private readonly RectTransform _overlay;
        private readonly RectTransform _overlayPanel;
        private readonly Image _overlaySurface;
        private readonly Image _overlayHeader;
        private readonly TextMeshProUGUI _overlayTitle;
        private readonly TextMeshProUGUI _overlayBody;
        private readonly ReliefParts _back;
        private readonly NoCanoesPanel _noCanoes;
        private readonly HomeWindow _home;
        private readonly StreakWidget _streakWidget;
        private GameServices? _services;
        private RiverStopCatalog? _riverStops;
        private SeasonTable? _seasons;
        private RiverStop? _currentStop;
        private string _remainingText = string.Empty;
        private bool _remainingReady;
        private bool _voyageReady;
        private bool _commerceOpen;
        private bool _reduceMotion;
        private int _stopLevelShown = -1;
        private int _dayCardCount;
        private int _layoutVersion;
        private bool _signCovered;
        private bool _canoeCovered;

        private readonly char[] _buf = new char[24];

        private AppFlow? _app;
        private AppScreen _shown = AppScreen.Boot;
        private int _screenW;
        private int _screenH;
        private Rect _safeArea;
        private float _canvasW = ReferenceWidth;
        private float _canvasH = 780f;
        private float _safeTop = GameScreen.MinSafeTop;
        private float _safeBottom = GameScreen.MinSafeBottom;
        private float _playTop;

        private int _playLevelShown = -1;
        private PlayButtonMode _playModeShown = PlayButtonMode.Level;
        private string _labelShown = string.Empty;
        private int _seedsShown = -1;
        private int _canoesVisibleShown = -1;
        private int _canoesCountShown = -1;
        private int _canoesMaxShown = -1;
        private int _canoesModeShown = -1;
        private long _timerShown = long.MinValue;

        /// <summary>
        /// Construye la pantalla bajo <paramref name="host"/>, oculta hasta <see cref="Show"/>. Las acciones son los puntos
        /// de entrada del input (los mismos que usan los tests PlayMode).
        /// </summary>
        public LobbyScreen(Transform host, ProceduralArt art, DevProbe probe, Action onPlay, Action onCanoes, Action onSettings, Action onBack, IGameActions actions)
        {
            _art = art;
            _probe = probe;
            _onPlay = onPlay;
            _onCanoes = onCanoes;
            _onSettings = onSettings;
            _onBack = onBack;

            var canvasGo = new GameObject("LobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(host, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = false;
            _canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            _scaler = canvasGo.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.referencePixelsPerUnit = 100f;
            _root = (RectTransform)canvasGo.transform;

            // ---- Escena: el panorama, la parada de «Jugar», el río vivo, el muelle, la rama de TiTino y el follaje (D-003; D-050). ----
            RectTransform scene = UiBuilder.Rect("Scene", _root);
            Image baseFill = UiBuilder.Image("BaseFill", scene, null, Palette.RiverFar);
            UiBuilder.Stretch(baseFill.rectTransform);
            _skyFill = UiBuilder.Image("SkyFill", scene, null, Palette.SkyTop);
            _panorama = UiBuilder.Image("Panorama", scene, art.Panorama, Color.white);

            // Fondo de la parada de «Jugar» (M2.1): como en el tablero, delante del panorama; encima, el río vivo (D-050, punto 2).
            _stopBackdrop = new StopBackdrop(scene, art);
            _river = new HomeRiver(scene, art);

            _dock = UiBuilder.Image("Dock", scene, art.Dock, Color.white);
            _crate = UiBuilder.Image("CuiaCrate", scene, art.DockCrate, Color.white);
            _crate.preserveAspect = true;
            _crate.gameObject.SetActive(false);

            // La rama de TiTino sale de debajo del follaje de arriba a la izquierda (D-050, punto 1).
            _branch = UiBuilder.Image("TitinoBranch", scene, art.HomeBranch, Color.white);
            _foliage[0] = UiBuilder.Image("FoliageTL", scene, art.FoliageTopLeft, Color.white);
            _foliage[1] = UiBuilder.Image("FoliageTR", scene, art.FoliageTopRight, Color.white);
            _foliage[2] = UiBuilder.Image("FoliageBL", scene, art.FoliageBottomLeft, Color.white);
            _foliage[3] = UiBuilder.Image("FoliageBR", scene, art.FoliageBottomRight, Color.white);

            // ---- La travesía (D-050, punto 3) y la Crecida en el agua (punto 4): delante del decorado, debajo de TiTino. ----
            _voyage = new HomeVoyage(_root, art, () => actions.OpenRiverMap(MapOpenSource.Lobby));
            _streakWidget = new StreakWidget(_root, art);

            // ---- Cabecera: Semillas, Canoas (31+) y Ajustes (ficha §7.1: saldos arriba). ----
            RectTransform top = UiBuilder.Rect("Top", _root);
            _scrim = UiBuilder.Image("StatusScrim", top, art.StatusScrim, Color.white);
            _seeds = UiBuilder.Rect("Seeds", top);
            Image seedsPlate = UiBuilder.Sliced("Plate", _seeds, art.WoodPlate, Color.white);
            UiBuilder.Stretch(seedsPlate.rectTransform);
            _seedsIcon = UiBuilder.Image("Icon", _seeds, art.IconSeed, Color.white);
            _seedsIcon.preserveAspect = true;
            _seedsText = UiBuilder.Label("Value", _seeds, TextStyle.PillWood, "0", 20f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.AutoSize(_seedsText, 14f, 20f);

            _canoes = UiBuilder.BareButton("CanoesButton", top, () => _onCanoes());
            _canoesPlate = UiBuilder.Sliced("Plate", _canoes.transform, art.WoodPlate, Color.white);
            _canoesIcon = UiBuilder.Image("Icon", _canoes.transform, art.IconCanoe, Color.white);
            _canoesIcon.preserveAspect = true;
            _canoesInfinite = UiBuilder.Image("Infinite", _canoes.transform, art.IconInfinite, Color.white);
            _canoesInfinite.preserveAspect = true;
            _canoesCount = UiBuilder.Label("Count", _canoes.transform, TextStyle.PillWood, "5/5", 19f, Palette.Cream, TextAlignmentOptions.Center);
            _canoesTimer = UiBuilder.Label("Timer", _canoes.transform, TextStyle.PillWood, string.Empty, CanoesTimerSp, Palette.Cream, TextAlignmentOptions.Center);
            _canoesTextScale = Mathf.Max(1f, UiBuilder.TextScale);

            // Ajustes (07 §7.6: engranaje de madera, disco de 40 dp en un área de 48): abre su pantalla (tarea L3).
            _settings = UiBuilder.Relief("SettingsButton", top, art.DiscWood, () => _onSettings());
            RectTransform settingsVisual = (RectTransform)_settings.Face.parent;
            settingsVisual.anchorMin = new Vector2(0.5f, 1f);
            settingsVisual.anchorMax = new Vector2(0.5f, 1f);
            settingsVisual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            settingsVisual.anchoredPosition = new Vector2(0f, -4f);
            Image gear = UiBuilder.Image("Icon", _settings.Face, art.IconGear, Color.white);
            UiBuilder.Place(gear.rectTransform, 7f, 7f, 26f, 26f);
            _settings.Relief.Bind(_settings.Face, _settings.Base, _settings.FaceImage, art.DiscWood, gear, art.IconGearDisabled, null);

            BuildAvatar(top, art); // D-056: el retrato del jugador arriba a la izquierda, desde el 10 (LobbyScreen.Avatar.cs)

            // Música y Sonido con un toque (D-046, punto 2; 07 §9.4): en columna bajo el engranaje, junto a él.
            _audio = new AudioToggles(top, art);
            _piggySlot = UiBuilder.Rect("PiggySlot", _root); // sin gráficos: solo el sitio del Cántaro en el muelle (MarketLobbyIcons)

            // ---- Pestañas abajo: «Inicio» [P] y, desde el 10, el Mapa del río a su derecha. ----
            _tabBar = UiBuilder.Sliced("TabBar", _root, art.WoodPlate, Color.white);
            _homeTab = UiBuilder.Rect("HomeTab", _root);
            if (art.CtaWood.Base != null)
            {
                Image tabBase = art.CtaWood.Sliced
                    ? UiBuilder.Sliced("Base", _homeTab, art.CtaWood.Base, Color.white)
                    : UiBuilder.Image("Base", _homeTab, art.CtaWood.Base, Color.white);
                UiBuilder.Stretch(tabBase.rectTransform);
            }

            Image tabFace = art.CtaWood.Sliced
                ? UiBuilder.Sliced("Face", _homeTab, art.CtaWood.Face, Color.white)
                : UiBuilder.Image("Face", _homeTab, art.CtaWood.Face, Color.white);
            UiBuilder.Stretch(tabFace.rectTransform);
            _homeTabLabel = UiBuilder.Label("Label", tabFace.transform, TextStyle.PillWood, UiText.Home, 17f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_homeTabLabel.rectTransform, art.CtaWood);

            // Icono del Mapa del río (06 §4.7: los iconos Mercado / Mapa del río, sin texto; tarea M3.3): la canoa de Capi sobre la
            // madera de la pestaña y, si hay una llegada sin ver, un punto de heliconia con canto crema. Desde max_level ≥ 10.
            _mapTab = UiBuilder.Relief("MapTab", _root, art.CtaWood, () => actions.OpenRiverMap(MapOpenSource.Lobby));
            Image mapIcon = UiBuilder.Image("Icon", _mapTab.Face, art.MapCanoe, Color.white);
            mapIcon.preserveAspect = true;
            UiBuilder.Stretch(mapIcon.rectTransform);
            mapIcon.rectTransform.offsetMin = new Vector2(10f, 5f);
            mapIcon.rectTransform.offsetMax = new Vector2(-10f, -5f);
            _mapTab.Relief.Bind(_mapTab.Face, _mapTab.Base, _mapTab.FaceImage, art.CtaWood, null, null, null);
            _mapTabDot = UiBuilder.Rect("UnseenDot", _mapTab.Face);
            Image dotRim = UiBuilder.Image("Rim", _mapTabDot, art.Disc, Palette.Cream);
            UiBuilder.Stretch(dotRim.rectTransform);
            Image dot = UiBuilder.Image("Dot", _mapTabDot, art.Disc, Palette.Heliconia);
            UiBuilder.Stretch(dot.rectTransform);
            dot.rectTransform.offsetMin = new Vector2(2f, 2f);
            dot.rectTransform.offsetMax = new Vector2(-2f, -2f);
            _mapTabDot.gameObject.SetActive(false);
            _mapTab.Button.gameObject.SetActive(false);

            // ---- Botón central «Jugar · Nivel {n}» (familia Hoja, el primario de 07 §7.2; nocturno en Muy difícil) y el chip de
            // la etiqueta de D-009 encima (propuesta §c.1). ----
            _chip = DifficultyChip.Create(_root, art);

            _play = UiBuilder.Relief("PlayButton", _root, art.CtaLeafLarge, () => _onPlay());
            _playLabel = UiBuilder.Label("Label", _play.Face, TextStyle.CtaLeaf, UiText.PlayLevel(1), 30f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_playLabel.rectTransform, art.CtaLeafLarge);
            UiBuilder.AutoSize(_playLabel, 22f, 30f);
            _play.Relief.Bind(_play.Face, _play.Base, _play.FaceImage, art.CtaLeafLarge, null, null, _playLabel);

            // ---- TiTino grande en su rama, con su bocadillo encima de todo lo de la escena (D-050, punto 1). ----
            _home = new HomeWindow(_root, art);
            if (_home.Sloth != null)
            {
                _home.Sloth.AsleepChanged = OnSlothAsleepChanged; // sondas: titino_sleep y titino_asleep de AF_LAYOUT lobby
            }

            // ---- Superposición: «El río sigue creciendo» (06 §9) o Sin canoas (04 §7.3), con el inicio atenuado detrás. ----
            _overlay = UiBuilder.Rect("Overlay", _root);
            Image dim = UiBuilder.Image("Dim", _overlay, null, new Color(0.04f, 0.06f, 0.05f, 0.62f), true);
            UiBuilder.Stretch(dim.rectTransform);
            _overlayPanel = UiBuilder.Rect("Panel", _overlay);
            _overlaySurface = UiBuilder.ModalBody(_overlayPanel, art);
            _overlayHeader = UiBuilder.Image("HeaderPlate", _overlayPanel, art.HeaderPlate, Color.white);
            _overlayTitle = UiBuilder.Label("Title", _overlayHeader.transform, TextStyle.ModalTitle, string.Empty, 22f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_overlayTitle.rectTransform);
            UiBuilder.AutoSize(_overlayTitle, 16f, 22f);
            _overlayBody = UiBuilder.Label("Body", _overlayPanel, TextStyle.PlainBold, string.Empty, 16f, Palette.FamilyWood.Base, TextAlignmentOptions.Center);
            _overlayBody.textWrappingMode = TextWrappingModes.Normal;
            UiBuilder.AutoSize(_overlayBody, 13f, 16f);
            _back = UiBuilder.Relief("BackButton", _overlayPanel, art.CtaLeaf, () => _onBack());
            TextMeshProUGUI backLabel = UiBuilder.Label("Label", _back.Face, TextStyle.CtaLeaf, UiText.Back, 22f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(backLabel.rectTransform, art.CtaLeaf);
            _back.Relief.Bind(_back.Face, _back.Base, _back.FaceImage, art.CtaLeaf, null, null, backLabel);
            _overlay.gameObject.SetActive(false);

            // Sin canoas con sus opciones (04 §7.3; B2-3): la misma tarjeta que en la pantalla de nivel.
            _noCanoes = new NoCanoesPanel(_root, art, () => actions.PressExtraCanoeVideo(), () => actions.PressRefillCanoes(),
                () => actions.PressInfiniteCanoes(), () => actions.PressNoCanoesMap(), () => actions.PressNoCanoesClose());
            // «Llenar canoas» en gris (sin saldo): late «Te faltan {n} semillas» y, si la tienda no vende, su aviso (como en la
            // pantalla de nivel; observación 6 de la jornada 5). Desde el inicio no hay nivel en juego: level 0 en la sonda.
            _noCanoes.RefillMissing = () => GameScreen.RespondToMissing("refill_canoes", _noCanoes.PulseRefillMissing(_reduceMotion),
                _noCanoes.RefillMissingSeeds, _services, actions, A11y, _probe, 0);

            ApplyScreenMetrics();
            _canvas.gameObject.SetActive(false);
        }

        /// <summary>Lienzo (capturas de prueba).</summary>
        public Canvas Canvas => _canvas;

        /// <summary>La pantalla de inicio (o una superposición suya) está visible.</summary>
        public bool Visible => _canvas.gameObject.activeSelf;

        /// <summary>Hay una superposición abierta («El río sigue creciendo» o Sin canoas).</summary>
        public bool OverlayVisible => _overlay.gameObject.activeSelf || _noCanoes.Visible;

        /// <summary>Título de la superposición abierta.</summary>
        public string OverlayTitle => _noCanoes.Visible ? UiText.NoCanoesTitle : _overlayTitle.text;

        /// <summary>Sin canoas con sus opciones (tests).</summary>
        internal NoCanoesPanel NoCanoes => _noCanoes;

        /// <summary>
        /// Paradas del Mapa del río (<c>af-river-stops/1</c>; M2.1). Null = sin el archivo o inválido: el inicio sin fondo de parada,
        /// sin travesía y sin TiTino. Al cambiarlo, el inicio se rehace en el siguiente fotograma.
        /// </summary>
        public RiverStopCatalog? RiverStops
        {
            get => _riverStops;
            set
            {
                _riverStops = value;
                _stopLevelShown = -1;
            }
        }

        /// <summary>
        /// Tabla de temporada del cliente (<c>af-seasons/1</c>; SE2): el fondo del inicio lleva la LUT de aguas altas o bajas del
        /// ciclo de «Jugar», como el tablero (07 §2.1). Null = sin temporada (como antes). Al cambiarla, el inicio se rehace en el
        /// siguiente fotograma.
        /// </summary>
        public SeasonTable? Seasons
        {
            get => _seasons;
            set
            {
                _seasons = value;
                _stopLevelShown = -1;
            }
        }

        /// <summary>Fondo de la parada del inicio (tests y capturas).</summary>
        internal StopBackdrop StopBackdrop => _stopBackdrop;

        /// <summary>El río vivo (tests y capturas).</summary>
        internal HomeRiver River => _river;

        /// <summary>La travesía de la canoa de Capi hacia la próxima parada (tests y capturas).</summary>
        internal HomeVoyage Voyage => _voyage;

        /// <summary>La rama de TiTino (tests y capturas).</summary>
        internal Image Branch => _branch;

        /// <summary>La caja de cuias del muelle sin tarjetas (tests y capturas).</summary>
        internal Image Crate => _crate;

        /// <summary>Tinte del panorama: la luz de la parada (blanco = como antes; tests).</summary>
        internal Color PanoramaTint => _panorama.color;

        /// <summary>TiTino en el inicio, en su rama (tests y capturas): <see cref="HomeWindow"/> en su modo del inicio.</summary>
        internal HomeWindow HomeWindow => _home;

        /// <summary>El letrero de la próxima parada (nombre y «Faltan {n} niveles») se ve (D-050, punto 3).</summary>
        public bool StopRemainingShown => _voyage.Visible && _voyage.SignShown;

        /// <summary>«Faltan {n} niveles para {lugar}»: la línea entera de la parada (la que lee TalkBack en la canoa y el poste).</summary>
        public string StopRemainingText => _remainingText;

        /// <summary>Caja del letrero de la próxima parada.</summary>
        public RectTransform StopRemainingRect => _voyage.SignRect;

        /// <summary>Nombre de la próxima parada en su letrero (tests: tamaño y fuente).</summary>
        internal TextMeshProUGUI StopRemainingLabel => _voyage.NameLabel;

        /// <summary>
        /// Hay una pantalla de pago delante del inicio (la hoja de compra, un anuncio o una oferta; propuesta del personaje g.2,
        /// regla 9, y d.11): TiTino se oculta mientras dure. Lo avisa <see cref="GameBootstrap"/>.
        /// </summary>
        public bool CommerceOpen => _commerceOpen;

        /// <summary>
        /// Reducir movimiento (07 §7.8; lo fija <see cref="GameBootstrap"/>): el río, la canoa y Rosa, quietos (D-050). TiTino lo lee
        /// él mismo.
        /// </summary>
        public bool ReduceMotion
        {
            get => _reduceMotion;
            set
            {
                _reduceMotion = value;
                _river.SetReduceMotion(value);
                _voyage.SetReduceMotion(value);
            }
        }

        /// <summary>El lector de pantalla (lo pone <c>GameBootstrap.A11y</c>; null sin él): anuncia «Te faltan {n} semillas».</summary>
        internal A11yService? A11y { get; set; }

        /// <summary>Ancho del lienzo en dp.</summary>
        public float CanvasWidth => _canvasW;

        /// <summary>Raíz del lienzo del inicio (tests de layout).</summary>
        internal RectTransform Root => _root;

        /// <summary>Alto del lienzo en dp.</summary>
        public float CanvasHeight => _canvasH;

        /// <summary>Píxeles de pantalla por dp.</summary>
        public float ScaleFactor => _scaler.scaleFactor;

        /// <summary>Zona segura superior aplicada (dp).</summary>
        public float SafeTop => _safeTop;

        /// <summary>Zona segura inferior aplicada (dp).</summary>
        public float SafeBottom => _safeBottom;

        /// <summary>Botón «Jugar · Nivel {n}».</summary>
        public Button PlayButton => _play.Button;

        /// <summary>Texto del botón Jugar.</summary>
        public string PlayLabel => _playLabel.text;

        /// <summary>
        /// Cuántas veces se maquetó el inicio (sube al cambiar la pantalla): quien coloca algo en sus sitios (las tarjetas del día)
        /// sabe así que tiene que volver a colocarlo.
        /// </summary>
        public int LayoutVersion => _layoutVersion;

        /// <summary>
        /// Tarjetas del día que hay en el muelle (D-050, punto 6; las coloca <see cref="GameBootstrap"/>): sin ninguna (antes del
        /// 10), la caja de cuias de Capi ocupa el muelle bajo «Jugar».
        /// </summary>
        public int DayCardCount
        {
            get => _dayCardCount;
            set
            {
                int n = Mathf.Clamp(value, 0, 3);
                if (n == _dayCardCount)
                {
                    return;
                }

                _dayCardCount = n;
                PlaceCrate();
            }
        }

        /// <summary>
        /// Ancho de cada tarjeta del día con <paramref name="count"/> en la fila (dp): con 3, a partes iguales entre los márgenes de
        /// <see cref="CardGutterDp"/>; con 1 o 2, como mucho <see cref="CardMaxWidthDp"/>, centradas.
        /// </summary>
        public float DayCardWidth(int count)
        {
            int n = Mathf.Clamp(count, 1, 3);
            float even = (_canvasW - (2f * CardGutter(n)) - ((n - 1) * CardGap(n))) / n;
            return n >= 3 ? Mathf.Floor(even) : Mathf.Min(CardMaxWidthDp, Mathf.Floor(even));
        }

        /// <summary>
        /// Sitio de la tarjeta del día número <paramref name="index"/> de <paramref name="count"/> (dp del lienzo, y hacia abajo),
        /// de <paramref name="height"/> dp de alto: la fila centrada bajo «Jugar», con el disco del icono que asoma por encima a
        /// <see cref="CardTopGapDp"/> de él.
        /// </summary>
        public Rect DayCardSlot(int index, int count, float height)
        {
            int n = Mathf.Clamp(count, 1, 3);
            float w = DayCardWidth(n);
            float gap = CardGap(n);
            float total = (n * w) + ((n - 1) * gap);
            float left = Mathf.Round((_canvasW - total) * 0.5f);
            float top = _playTop + PlayTouchHeight + CardTopGapDp + DockCard.IconOverhangDp;
            return new Rect(left + (Mathf.Clamp(index, 0, n - 1) * (w + gap)), top, w, height);
        }

        // Con 3 tarjetas, margen y hueco más justos para que una palabra larga del título («Challenge» al 130 % en 320 dp) quepa.
        private static float CardGutter(int n) => n >= 3 ? CardGutterTightDp : CardGutterDp;

        /// <summary>Margen a cada lado de la fila con 3 tarjetas (dp).</summary>
        public const float CardGutterTightDp = 12f;

        private static float CardGap(int n) => n >= 3 ? 8f : CardGapDp;

        /// <summary>
        /// Alto que tiene la fila de tarjetas del día (dp): de su borde de arriba hasta 4 dp por encima de las pestañas (de «Inicio» y
        /// los iconos, que sobresalen 12 dp de la barra). Si las tarjetas piden más, <see cref="GameBootstrap"/> las compacta.
        /// </summary>
        public float DayCardMaxHeight
        {
            get
            {
                float top = _playTop + PlayTouchHeight + CardTopGapDp + DockCard.IconOverhangDp;
                float tabs = _canvasH - _safeBottom - TabBarHeight - 12f;
                return Mathf.Max(LobbyScreen.MinTouchDp, tabs - 4f - top);
            }
        }

        /// <summary>
        /// El Reto del río está desbloqueado (lo da <c>GameBootstrap</c> al crearlo: en el build, con su interruptor y desde
        /// <c>live_unlock_level_daily</c>); null o false = sin Reto. Decide el botón Jugar tras acabar el contenido (D-032,
        /// punto 6; <see cref="PlayModeFor"/>).
        /// </summary>
        public Func<bool>? DailyUnlocked { get; set; }

        /// <summary>
        /// Estado de la Crecida para el nivel de «Jugar» (lo da <c>GameBootstrap.SetUpStreak</c> con <c>StreakService.Peek</c>);
        /// null o sin <see cref="StreakSnapshot.Active"/> = sin poste (06 §4.7: «widget de Crecida (25+)»). Lee el almacén: se llama
        /// al mostrar el inicio y al cambiar el nivel de «Jugar», nunca por fotograma (tarea B1b).
        /// </summary>
        public Func<int, StreakSnapshot?>? StreakPeek { get; set; }

        /// <summary>El poste de la Crecida (tests y capturas).</summary>
        internal StreakWidget StreakWidget => _streakWidget;

        /// <summary>Lo que dice y hace el botón Jugar ahora (<see cref="PlayModeFor"/>).</summary>
        public PlayButtonMode PlayMode => _playModeShown;

        /// <summary>
        /// Qué es el botón Jugar del inicio (D-032, punto 6): con el nivel de <c>next_level</c> publicado, «Jugar · Nivel {n}»; tras
        /// acabar el contenido (el siguiente nivel aún no está publicado), «Jugar · Reto del río» si el Reto está desbloqueado
        /// (y al tocarlo, el Reto) y, si no, «Jugar» a secas (y al tocarlo, «El río sigue creciendo», como antes).
        /// </summary>
        public static PlayButtonMode PlayModeFor(bool levelAvailable, bool dailyUnlocked)
        {
            if (levelAvailable)
            {
                return PlayButtonMode.Level;
            }

            return dailyUnlocked ? PlayButtonMode.Daily : PlayButtonMode.ContentEnd;
        }

        /// <summary>El texto del botón Jugar para <paramref name="mode"/> y el nivel <paramref name="level"/> (claves de la tabla).</summary>
        public static string PlayLabelFor(PlayButtonMode mode, int level)
        {
            switch (mode)
            {
                case PlayButtonMode.Daily:
                    return UiText.PlayDaily;
                case PlayButtonMode.ContentEnd:
                    return UiText.Play;
                default:
                    return UiText.PlayLevel(level);
            }
        }

        /// <summary>Contador de canoas (botón: con 0/5 abre Sin canoas).</summary>
        public Button CanoesButton => _canoes;

        /// <summary>Botón de Ajustes: abre <see cref="SettingsScreen"/> (tarea L3).</summary>
        public Button SettingsButton => _settings.Button;

        /// <summary>Botones de un toque Música y Sonido, en columna bajo el engranaje (D-046, punto 2).</summary>
        internal AudioToggles Audio => _audio;

        /// <summary>
        /// Toque en Música o Sonido del inicio: la clave y el valor pedido. Lo fija <see cref="GameBootstrap"/>, que lo guarda por el
        /// mismo camino que Ajustes y devuelve lo guardado con <see cref="MarkAudio"/>.
        /// </summary>
        public Action<string, bool>? AudioToggled
        {
            get => _audio.Toggled;
            set => _audio.Toggled = value;
        }

        /// <summary>Lo guardado en <c>settings.music</c> y <c>settings.sound</c>, a los botones del inicio.</summary>
        public void MarkAudio(bool music, bool sound)
        {
            _audio.Mark(music, sound);
        }

        /// <summary>
        /// El sitio del Cántaro del Mercado (<see cref="MarketLobbyIcons"/>, que se pone encima): desde D-050, la tinaja de barro
        /// apoyada en el muelle a la izquierda, entre el agua y «Jugar», sin pisar el chip de la etiqueta.
        /// </summary>
        public RectTransform PiggyAnchorRect => _piggySlot;

        /// <summary>
        /// Botón Volver de la superposición: en Sin canoas, su X, que vuelve al inicio (desde B11 su enlace de abajo es «Volver al
        /// Mapa del río», <c>NoCanoes.HomeButton</c>, que abre el Mapa encima).
        /// </summary>
        public Button BackButton => _noCanoes.Visible ? _noCanoes.CloseButton : _back.Button;

        /// <summary>Contador de semillas.</summary>
        public RectTransform SeedsRect => _seeds;

        /// <summary>Pestaña «Inicio».</summary>
        public RectTransform HomeTabRect => _homeTab;

        /// <summary>
        /// La escena trae el Mapa del río (su geometría y las paradas; tarea M3.3): sin él, la pestaña del Mapa no sale nunca y la
        /// canoa no se toca. Lo fija <see cref="GameBootstrap"/>.
        /// </summary>
        public bool RiverMapAvailable
        {
            get => _riverMapAvailable;
            set
            {
                _riverMapAvailable = value;
                if (_app != null && Visible)
                {
                    bool shown = MapTabShown;
                    RefreshMapTab();
                    if (MapTabShown != shown)
                    {
                        EmitLayout(); // AF_LAYOUT deja de publicar (o publica) map_tab al momento, no en el siguiente cambio
                    }
                }
            }
        }

        /// <summary>Pestaña del Mapa del río (abre el Mapa en la vista Mundo).</summary>
        public Button MapTabButton => _mapTab.Button;

        /// <summary>Caja de la pestaña del Mapa del río.</summary>
        public RectTransform MapTabRect => (RectTransform)_mapTab.Button.transform;

        /// <summary>La pestaña del Mapa del río se ve (desde <c>max_level</c> ≥ 10, con el Mapa en la escena).</summary>
        public bool MapTabShown => _mapTab.Button.gameObject.activeSelf;

        /// <summary>La pestaña del Mapa lleva el punto: hay una llegada a parada sin ver (<see cref="RiverJourney.HasUnseenArrival"/>).</summary>
        public bool MapTabDotShown => _mapTabDot.gameObject.activeSelf;

        /// <summary>Chip de dificultad entero (sello, píldora y cinta).</summary>
        public RectTransform ChipRect => _chip.Root;

        /// <summary>El contador de canoas se ve (desde el 31).</summary>
        public bool CanoesShown => _canoes.gameObject.activeSelf;

        /// <summary>El chip de dificultad se ve.</summary>
        public bool ChipShown => _chip.Visible;

        /// <summary>Texto del chip de dificultad.</summary>
        public string ChipText => _chip.Text;

        /// <summary>Texto de la cinta del chip («×3» o «×4»; vacío si no se ve).</summary>
        public string ChipRibbonText => _chip.RibbonText;

        /// <summary>
        /// «Jugar · Nivel {n}» va en su variante nocturna [P] (Muy difícil: índigo oscuro con texto marfil; 07 §7.7;
        /// propuesta §c.1).
        /// </summary>
        public bool PlayNight => _playNight;

        /// <summary>Color del texto del botón Jugar.</summary>
        public Color PlayLabelColor => _playLabel.color;

        /// <summary>Sprite de la cara del botón Jugar (hoja o noche).</summary>
        public Sprite? PlayFaceSprite => _play.FaceImage.sprite;

        /// <summary>Material del texto del botón Jugar (contorno de hoja, o de agua profunda en la variante nocturna; tests).</summary>
        internal Material? PlayLabelMaterial => _playLabel.fontSharedMaterial;

        /// <summary>Chip de dificultad (tests y capturas).</summary>
        internal DifficultyChip Chip => _chip;

        /// <summary>Semillas que muestra el contador.</summary>
        public int SeedsShown => _seedsShown;

        /// <summary>El texto del contador de semillas tal como está en la etiqueta (tests; observación 22 de la jornada 6).</summary>
        public string SeedsText => _seedsText.text;

        /// <summary>Canoas que muestra el contador («n» de «n/5»; −1 con Canoas infinitas o si no se ve).</summary>
        public int CanoesCountShown => _canoesModeShown == 2 ? -1 : _canoesCountShown;

        /// <summary>Segundos del cronómetro de canoas que se muestran (−1 si no corre).</summary>
        public long CanoesTimerShown => _timerShown;

        /// <summary>
        /// Pares texto / fondo de la pantalla de inicio y su superposición, para la comprobación de contraste ≥ 4,5:1
        /// (07 §1.2 y §7.8): contadores sobre madera oscura, botón Jugar y Volver sobre hoja, Jugar nocturno (marfil sobre
        /// índigo), pestaña sobre madera, el texto de la superposición sobre la madera clara, el letrero de la parada y las tarjetas
        /// del día del muelle (D-050) y, al final, el chip y su cinta (<see cref="DifficultyChip.ContrastPairs"/>).
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            (string Name, Color Text, Color Background)[] own =
            {
                ("contadores", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("jugar", (Color)Palette.Cream, Palette.FamilyLeaf.Base),
                ("jugar (fondo de la cara)", (Color)Palette.Cream, Palette.FamilyLeaf.Deep),
                ("jugar nocturno", (Color)Palette.UiPaper, ProceduralArt.FamilyNight.Base),
                ("jugar nocturno (fondo de la cara)", (Color)Palette.UiPaper, ProceduralArt.FamilyNight.Deep),
                ("pestaña Inicio", (Color)Palette.Cream, Palette.FamilyWood.Base),
                ("pestaña Inicio (fondo de la cara)", (Color)Palette.Cream, Palette.FamilyWood.Deep),
                ("texto de la superposición", Palette.FamilyWood.Base, (Color)Palette.PanelLightLow),
                ("letrero de la parada: nombre", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("letrero de la parada: faltan N niveles", (Color)Palette.UiInk, (Color)Palette.PanelLight),
                ("tarjeta del día: título", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("tarjeta del día: estado verde", (Color)Palette.LeafShine, (Color)Palette.WoodDark),
                ("tarjeta del día: estado fibra", (Color)Palette.FiberLight, (Color)Palette.WoodDark),
                ("tarjeta del día: cifra", (Color)Palette.UiInk, (Color)Palette.PanelLight),
            };
            (string Name, Color Text, Color Background)[] chip = DifficultyChip.ContrastPairs();
            var all = new (string Name, Color Text, Color Background)[own.Length + chip.Length];
            own.CopyTo(all, 0);
            chip.CopyTo(all, own.Length);
            return all;
        }

        /// <summary>Asocia la navegación y los servicios de Sin canoas (antes de la primera pantalla).</summary>
        public void Bind(AppFlow app, GameServices? services = null)
        {
            _app = app;
            _services = services;
        }

        /// <summary>
        /// Avisa de que hay (o ya no hay) una pantalla de pago delante del inicio: la hoja de compra, un anuncio o una oferta. TiTino
        /// no sale en ellas (D-013, punto 6; propuesta del personaje g.2, regla 9, y d.11). Sin asignar memoria.
        /// </summary>
        public void SetCommerceOpen(bool open)
        {
            if (open == _commerceOpen)
            {
                return;
            }

            _commerceOpen = open;
            ApplyStopVisibility();
        }

        /// <summary>
        /// Muestra la pantalla de <paramref name="screen"/>: el inicio, con «El río sigue creciendo» o Sin canoas encima si
        /// toca; en <see cref="AppScreen.Level"/> se oculta.
        /// </summary>
        public void Show(AppScreen screen)
        {
            _shown = screen;
            bool visible = screen == AppScreen.Lobby || screen == AppScreen.ContentEnd || screen == AppScreen.NoCanoes;
            if (_canvas.gameObject.activeSelf != visible)
            {
                _canvas.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            _overlay.gameObject.SetActive(screen == AppScreen.ContentEnd);
            if (screen == AppScreen.ContentEnd)
            {
                _overlayTitle.text = UiText.ContentEndTitle;
                _overlayBody.text = UiText.ContentEnd(_app != null ? _app.ContentEndEtaDays : GameConfig.LiveContentEndNextBatchEtaDays);
            }

            ApplyScreenMetrics();
            if (screen == AppScreen.NoCanoes)
            {
                _noCanoes.Show(_services, _canvasW, _canvasH);
            }
            else
            {
                _noCanoes.Hide();
            }
            ForceRefresh();
            ApplyStopVisibility();
            RefreshStreak();
            if (_app != null)
            {
                _probe.App(AppScreenNames.Of(screen), _app);
            }

            if (screen == AppScreen.Lobby)
            {
                DevProbe.Mem("lobby"); // AF_PERF mem (DevProbe.Memory.cs): cada vez que se ve el inicio
            }

            EmitLayout();
        }

        /// <summary>
        /// Avanza la pantalla <paramref name="dtMs"/> milisegundos: métricas de pantalla, contadores, cronómetros, el río vivo y la
        /// canoa (sin asignaciones).
        /// </summary>
        public void Tick(float dtMs)
        {
            if (!_canvas.gameObject.activeSelf)
            {
                return;
            }

            _noCanoes.Tick(dtMs);

            GameScreen.ReadScreen(out int sw, out int sh, out Rect safe);
            if (sw != _screenW || sh != _screenH || safe != _safeArea)
            {
                ApplyScreenMetrics();
                EmitLayout();
            }

            Refresh();
            _river.Tick(dtMs);
            _voyage.Tick(dtMs);
            UpdateVoyageCover();
        }

        // Mientras el bocadillo de TiTino crece hasta la travesía (un mensaje largo, al 130 % o en una pantalla baja), lo que tapa
        // (el letrero o la canoa) se aparta y vuelve cuando el bocadillo se va: nada se pisa (como hacía la tarjeta del Reto con la
        // nota de TiTino, revisión de D-046). Sin asignaciones: lee las cajas tal como se colocaron.
        private void UpdateVoyageCover()
        {
            HomeSloth? sloth = _home.Sloth;
            bool sign = false;
            bool canoe = false;
            if (sloth != null && sloth.BubbleShown && _voyageReady)
            {
                Rect bubble = TopLeftRect(sloth.BubbleRect);
                bubble.xMin -= HomeSloth.BubbleRimDp;
                bubble.xMax += HomeSloth.BubbleRimDp;
                bubble.yMax += HomeSloth.BubbleRimDp + 2f;
                sign = bubble.Overlaps(TopLeftRect(_voyage.SignRect));
                canoe = bubble.Overlaps(_voyage.CanoeBounds);
            }

            if (sign != _signCovered || canoe != _canoeCovered)
            {
                _signCovered = sign;
                _canoeCovered = canoe;
                _voyage.SetCovered(sign, canoe);
            }
        }

        // Caja de un hijo del lienzo colocado con UiBuilder.Place (ancla y pivote arriba a la izquierda), en dp y con la y hacia abajo.
        private static Rect TopLeftRect(RectTransform rt)
        {
            Vector2 pos = rt.anchoredPosition;
            Vector2 size = rt.sizeDelta;
            return new Rect(pos.x - (rt.pivot.x * size.x), -pos.y - ((1f - rt.pivot.y) * size.y), size.x, size.y);
        }

        private void ForceRefresh()
        {
            _playLevelShown = -1;
            _labelShown = string.Empty;
            _seedsShown = -1;
            _canoesVisibleShown = -1;
            _canoesCountShown = -1;
            _canoesMaxShown = -1;
            _canoesModeShown = -1;
            _timerShown = long.MinValue;
            Refresh();
        }

        private void Refresh()
        {
            AppFlow? app = _app;
            if (app == null)
            {
                return;
            }

            int level = app.PlayLevel;
            Func<bool>? daily = DailyUnlocked;
            bool available = app.PlayLevelAvailable;
            PlayButtonMode mode = PlayModeFor(available, !available && daily != null && daily());
            if (level != _playLevelShown || mode != _playModeShown)
            {
                _playLevelShown = level;
                _playModeShown = mode;
                _playLabel.text = PlayLabelFor(mode, level);
            }

            if (level != _stopLevelShown)
            {
                _stopLevelShown = level;
                ApplyStop(app, level);
                RefreshStreak(); // otro nivel de «Jugar»: el poste puede aparecer (el 25) o cambiar
            }

            string label = app.PlayLevelLabel;
            if (!string.Equals(label, _labelShown, StringComparison.Ordinal))
            {
                _labelShown = label;
                ApplyChip(label, app.Wallet.Config);
            }

            int seeds = app.Wallet.Seeds;
            if (seeds != _seedsShown)
            {
                _seedsShown = seeds;
                int n = UiText.WriteNumber(_buf, 0, seeds, true);
                _seedsText.SetCharArray(_buf, 0, n);
            }

            RefreshCanoes(app);
            RefreshMapTab();
            if (_shown == AppScreen.NoCanoes)
            {
                _noCanoes.Refresh(_services);
            }
        }

        // Pestaña del Mapa del río (M3.3): solo en el inicio sin superposición, con el Mapa en la escena y desde max_level ≥ 10; el
        // punto, si hay una llegada sin ver. La canoa y la parada de la travesía se tocan con la misma regla (D-050, punto 3).
        // Enteros del progreso, sin asignar memoria; solo cambia la UI cuando cambia algo.
        private void RefreshMapTab()
        {
            IMapProgress? progress = _services?.MapProgress;
            int max = progress != null ? Mathf.Clamp(progress.MaxLevel, 0, WorldTable.LastLevel) : 0;
            bool show = _riverMapAvailable && progress != null && _shown == AppScreen.Lobby && max >= RiverMapScreen.TabFromMaxLevel;
            bool dot = show && RiverJourney.HasUnseenArrival(Mathf.Max(0, progress!.MapSeenLevel), max);
            int showFlag = show ? 1 : 0;
            if (showFlag != _mapTabShown)
            {
                _mapTabShown = showFlag;
                _mapTab.Button.gameObject.SetActive(show);
                _voyage.SetInteractive(show);
            }

            int dotFlag = dot ? 1 : 0;
            if (dotFlag != _mapDotShown)
            {
                _mapDotShown = dotFlag;
                _mapTabDot.gameObject.SetActive(dot);
            }
        }

        // Poste de la Crecida (B1b; D-050, punto 4): solo en el inicio sin superposición y con la Crecida activa en el nivel de «Jugar».
        private void RefreshStreak()
        {
            AppFlow? app = _app;
            Func<int, StreakSnapshot?>? peek = StreakPeek;
            StreakSnapshot? s = app != null && peek != null && _shown == AppScreen.Lobby ? peek(app.PlayLevel) : null;
            bool show = s.HasValue && s.Value.Active;
            if (show)
            {
                _streakWidget.Set(s!.Value.Wins, s.Value.ShieldAvailable);
            }

            _streakWidget.SetVisible(show);
        }

        private void RefreshCanoes(AppFlow app)
        {
            bool visible = app.CanoesVisible;
            int visibleFlag = visible ? 1 : 0;
            if (visibleFlag != _canoesVisibleShown)
            {
                _canoesVisibleShown = visibleFlag;
                _canoes.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            CanoesStatus canoes = app.Wallet.Canoes;
            int mode = canoes.IsInfinite ? 2 : (canoes.Count <= 0 ? 0 : 1); // 0 volcada, 1 con canoas, 2 infinitas
            long seconds = canoes.IsInfinite
                ? CeilSeconds(canoes.InfiniteRemaining)
                : (canoes.IsFull ? -1L : CeilSeconds(canoes.TimeToNext));
            bool layoutChanged = mode != _canoesModeShown || (seconds < 0) != (_timerShown < 0) || _timerShown == long.MinValue;
            if (mode != _canoesModeShown || canoes.Count != _canoesCountShown || canoes.Max != _canoesMaxShown)
            {
                if (mode != 2 && _canoesModeShown != 2 && _canoesCountShown >= 0 && canoes.Count > _canoesCountShown)
                {
                    AF.Audio.AudioCues.Play(AF.Audio.SfxId.CanoeBack); // AU1: se recupera una canoa
                }

                _canoesModeShown = mode;
                _canoesCountShown = canoes.Count;
                _canoesMaxShown = canoes.Max;
                _canoesIcon.sprite = mode == 0 ? _art.IconCanoeEmpty : _art.IconCanoe;
                _canoesInfinite.gameObject.SetActive(mode == 2);
                _canoesCount.gameObject.SetActive(mode != 2);
                if (mode != 2)
                {
                    int n = UiText.WriteNumber(_buf, 0, canoes.Count, false);
                    _buf[n++] = '/';
                    n = UiText.WriteNumber(_buf, n, canoes.Max, false);
                    _canoesCount.SetCharArray(_buf, 0, n);
                }
            }

            if (seconds != _timerShown)
            {
                _timerShown = seconds;
                _canoesTimer.gameObject.SetActive(seconds >= 0);
                if (seconds >= 0)
                {
                    int n = UiText.WriteClock(_buf, 0, seconds);
                    _canoesTimer.SetCharArray(_buf, 0, n);
                }
            }

            if (layoutChanged)
            {
                LayoutCanoesContent();
            }
        }

        private void ApplyChip(string label, EconomyConfig config)
        {
            // Propuesta §c.1 y 07 §7.7: Difícil, chip ámbar con la rana y «×3»; Muy difícil, chip índigo con el jaguar, «×4» y el
            // botón en variante nocturna; normal, botón verde sin chip. Sin calavera. La cinta sale de eco_reward_* (§c.2).
            _chip.Set(label, DifficultyChip.MultiplierFor(config, label));
            ApplyPlayVariant(label == LevelDefinition.LabelVeryHard);
            LayoutChip();
            PlacePiggySlot(); // el Cántaro no pisa el chip, que cambia de ancho con la etiqueta
        }

        // Variante nocturna [P] de «Jugar · Nivel {n}» (07 §7.7; propuesta §c.1): la cara y el canto índigo de
        // ProceduralArt.CtaNightLarge y el texto marfil (10,7:1); en los demás niveles, la hoja de siempre.
        private void ApplyPlayVariant(bool night)
        {
            if (night == _playNight)
            {
                return;
            }

            _playNight = night;
            ReliefSprites sprites = night ? _art.CtaNightLarge : _art.CtaLeafLarge;
            _play.FaceImage.sprite = sprites.Face;
            if (_play.Base != null && sprites.Base != null)
            {
                _play.Base.sprite = sprites.Base;
            }

            // De noche, el contorno de agua profunda (CtaWater: la misma Baloo 2 Bold) en lugar del verde de hoja.
            TextStyle style = night ? TextStyle.CtaWater : TextStyle.CtaLeaf;
            TMPro.TMP_FontAsset? font = UiFonts.Font(style);
            Material? material = UiFonts.Material(style);
            if (font != null && material != null)
            {
                _playLabel.font = font;
                _playLabel.fontSharedMaterial = material;
            }

            _playLabel.color = night ? (Color)Palette.UiPaper : (Color)Palette.Cream;
            _play.Relief.Bind(_play.Face, _play.Base, _play.FaceImage, sprites, null, null, _playLabel);
            _play.Relief.SetInteractable(_play.Relief.Interactable);
        }

        private void ApplyScreenMetrics()
        {
            GameScreen.ReadScreen(out _screenW, out _screenH, out _safeArea);

            // La escala de la pantalla de nivel (GameScreen.CanvasScale): 360 dp de ancho (de 320 a 360 en los teléfonos de ese ancho,
            // Q-B11-5 (a)); si la pantalla es tan ancha que quedarían menos de 640 dp de alto, manda el alto.
            float scale = GameScreen.CurrentCanvasScale(_screenW, _screenH);
            _scaler.scaleFactor = scale;
            _canvasW = _screenW / scale;
            _canvasH = _screenH / scale;
            _safeTop = Mathf.Max(GameScreen.MinSafeTop, (_screenH - _safeArea.yMax) / scale);
            _safeBottom = Mathf.Max(GameScreen.MinSafeBottom, _safeArea.yMin / scale);
            Layout();
        }

        private void Layout()
        {
            float w = _canvasW;
            float h = _canvasH;
            float k = w / ReferenceWidth;
            float topY = _safeTop + 8f;
            float playCenter = Mathf.Round(h * PlayCenterFraction);
            _playTop = playCenter - (PlayTouchHeight * 0.5f);
            float tabTop = h - _safeBottom - TabBarHeight;

            // Escena: horizonte entre 118 y 200 dp (a 360 de ancho) y la viga del muelle bajo el botón Jugar, que se apoya en él.
            float baseHorizon = ProceduralArt.PanoramaHorizonDp * k;
            float horizon = Mathf.Clamp(h * 0.26f, baseHorizon, MaxHorizonDp * k);
            float shift = horizon - baseHorizon;
            UiBuilder.Place(_panorama.rectTransform, 0f, shift, w, w * (ProceduralArt.PanoramaHeightDp / 360f));
            _skyFill.enabled = shift > 0.01f;
            UiBuilder.Place(_skyFill.rectTransform, 0f, 0f, w, shift + 1f);
            float beam = _playTop - 36f;
            float dockTop = beam - (ProceduralArt.DockBeamTopDp * k);
            Sprite? dock = _dock.sprite;
            float aspectH = dock != null ? w * (dock.rect.height / dock.rect.width) : w;
            UiBuilder.Place(_dock.rectTransform, 0f, dockTop, w, Mathf.Max(aspectH, h - dockTop));

            // Fondo de la parada (M2.1): sin tablero, con la banda de agua hasta la viga del muelle, como en el tablero; encima, el río
            // vivo con la columna del sol del panorama (D-050, punto 2).
            _stopBackdrop.Layout(w, horizon, _playTop, false, beam + 8f);
            _river.Layout(w, horizon, beam, ProceduralArt.PanoramaSunXDp * k);
            _river.SetVisible(true);
            PlaceAt(_foliage[0].rectTransform, -20f, 0f, 160f, 160f, new Vector2(0f, 1f));
            PlaceAt(_foliage[1].rectTransform, w + 20f, 0f, 160f, 160f, new Vector2(1f, 1f));
            float bottom = ProceduralArt.BottomFoliageDp;
            PlaceAt(_foliage[2].rectTransform, 0f, tabTop + 24f, bottom, bottom, new Vector2(0f, 0f));
            PlaceAt(_foliage[3].rectTransform, w, tabTop + 24f, bottom, bottom, new Vector2(1f, 0f));
            UiBuilder.Place(_scrim.rectTransform, 0f, 0f, w, _safeTop + 48f);

            // Cabecera (ficha §7.1): Semillas a la izquierda, Canoas a su derecha y Ajustes en la esquina. Con el avatar (D-056,
            // avatares.md §3.1, desde el 10), el retrato va en x = 8 y Semillas pasa a x = 60; Canoas, 4 dp antes del engranaje y, si no
            // cabe, más estrecha (104 dp a 320 dp, con el icono 8 dp más estrecho), y Semillas llega hasta 4 dp antes de Canoas.
            TopRowMetrics(w, _avatarShown, out float seedsX, out float seedsW, out float canoesX, out float canoesW);
            _canoesW = canoesW;
            PlaceAvatar(topY);
            UiBuilder.Place(_seeds, seedsX, topY + 4f, seedsW, 40f);
            UiBuilder.Place(_seedsIcon.rectTransform, -8f, 1f, 38f, 38f);
            UiBuilder.Place(_seedsText.rectTransform, 30f, 0f, seedsW - 38f, 40f);
            // A 320 dp el engranaje (w − 56) pisaba 4 dp de Canoas: Canoas se arrima a Semillas lo justo, 4 dp antes del engranaje.
            UiBuilder.Place((RectTransform)_canoes.transform, canoesX, topY, canoesW, TopRowHeight);
            UiBuilder.Place(_canoesIcon.rectTransform, -8f, 11f, 48f - (128f - canoesW) * (8f / 24f), 28f);
            LayoutCanoesContent();
            UiBuilder.Place((RectTransform)_settings.Button.transform, w - 8f - MinTouchDp, topY, MinTouchDp, MinTouchDp);

            // Música y Sonido (D-046) [P]: en columna bajo el engranaje, alineados con él.
            float audioX = w - 8f - MinTouchDp;
            float audioTop = topY + TopRowHeight + AudioToggles.GapDp;
            _audio.PlaceColumn(audioX, audioTop);
            float audioBottom = audioTop + AudioToggles.ColumnHeightDp;

            // Botón central y chip.
            float playW = PlayWidth(w);
            UiBuilder.Place((RectTransform)_play.Button.transform, (w - playW) * 0.5f, _playTop, playW, PlayTouchHeight);
            LayoutChip();

            // TiTino (D-050, punto 1): su caja bajo los saldos, a la izquierda, con la rama que sale del follaje; tamaño S de 76 a 92 dp
            // según el alto (en 16:9, más pequeño). El bocadillo, a su derecha, sin pasar de la columna de Música y Sonido ni del
            // muelle (si llega a la travesía, la travesía se aparta mientras dure: UpdateVoyageCover).
            float rig = Mathf.Clamp(Mathf.Round(h * 0.118f), HomeWindow.SceneRigMinDp, HomeWindow.SceneRigMaxDp);
            float boxTop = topY + TopRowHeight + 4f;
            float branchY = boxTop + HomeWindow.SceneAboveDp;
            float grabX = TitinoLeftDp + (HomeWindow.SceneWidthDp * 0.5f);
            UiBuilder.Place(_branch.rectTransform, 0f, branchY - ProceduralArt.HomeBranchLineAt(grabX), ProceduralArt.HomeBranchWidthDp, ProceduralArt.HomeBranchHeightDp);
            _home.PlaceScene(TitinoLeftDp, branchY, rig, audioX - 8f, beam - 8f);
            float titinoBottom = branchY + rig + HomeWindow.SceneBelowDp;

            // La Crecida (D-050, punto 4): el poste a la derecha, en el agua, bajo la columna de Música y Sonido y hasta la viga.
            float crecX = w - 16f - StreakWidget.WidthDp;
            float crecBottom = beam - StreakWidget.FootDp - 6f;
            float crecTop = Mathf.Max(Mathf.Max(audioBottom + 12f, horizon + 8f), crecBottom - StreakWidget.MaxHeightDp);
            _streakWidget.Place(crecX, crecTop, Mathf.Max(52f, crecBottom - crecTop));

            // La travesía (D-050, punto 3): la ruta sale de la izquierda, más lejos (bajo TiTino), y acaba delante, en el poste de la
            // parada, a 16 dp de la viga; el letrero, en lo alto del poste, sin llegar a TiTino, a Música y Sonido ni a la Crecida.
            float depth = beam - horizon;
            var foot = new Vector2(Mathf.Round(0.62f * w), beam - 16f);
            float startY = Mathf.Clamp(foot.y - (0.42f * depth), titinoBottom + 20f, foot.y - 20f);
            var start = new Vector2(Mathf.Round(0.12f * w), startY);
            float signMin = TitinoLeftDp + HomeWindow.SceneWidthDp + 6f;
            float signMax = Mathf.Min(crecX - 8f, audioX - 6f);
            _voyage.Layout(start, foot, signMin, signMax, k);

            // El muelle: el Cántaro a la izquierda y, sin tarjetas, la caja de cuias.
            PlacePiggySlot();
            PlaceCrate();

            // Pestañas: la barra cubre hasta el borde inferior (y la zona de gestos); «Inicio» sobresale 12 dp.
            UiBuilder.Place(_tabBar.rectTransform, -8f, tabTop, w + 16f, h - tabTop + 12f);
            UiBuilder.Place(_homeTab, (w - HomeTabWidth) * 0.5f, tabTop - 12f, HomeTabWidth, _art.CtaWood.HeightDp);

            // El icono del Mapa, a la derecha de «Inicio», a su altura; su área táctil baja hasta tener 48 dp (M3.3).
            float mapTabX = ((w + HomeTabWidth) * 0.5f) + MapTabGapDp;
            UiBuilder.Place(MapTabRect, mapTabX, tabTop - 12f, MapTabWidth, Mathf.Max(MinTouchDp, _art.CtaWood.HeightDp));
            UiBuilder.Place(_mapTabDot, MapTabWidth - MapTabDotDp + 2f, -4f, MapTabDotDp, MapTabDotDp);

            // Superposición: tarjeta centrada en vertical, que tapa el botón Jugar atenuado (Magic Sort cubre lo de detrás con
            // la tarjeta y un fondo oscurecido, ficha §4.4 y §4.8), con su botón en la mitad inferior.
            float panelW = Mathf.Min(328f, w - 32f);
            float inner = panelW - 32f;
            UiBuilder.Place(_overlayPanel, (w - panelW) * 0.5f, Mathf.Round((h - OverlayPanelHeight) * 0.5f), panelW, OverlayPanelHeight);
            _overlaySurface.sprite = _art.ModalSurfaceFor(panelW - 16f, OverlayPanelHeight - 16f);
            UiBuilder.Place(_overlayHeader.rectTransform, 16f, 16f, inner, 40f);
            UiBuilder.Place(_overlayBody.rectTransform, 16f, 66f, inner, 66f);
            UiBuilder.Place((RectTransform)_back.Button.transform, 16f, 148f, inner, 64f);
            _layoutVersion++;
        }

        // Ancho de «Jugar» en un lienzo de w dp.
        private static float PlayWidth(float w)
        {
            return Mathf.Min(PlayMaxWidth, w - 64f);
        }

        private void LayoutCanoesContent()
        {
            // «n/5» arriba y el cronómetro debajo mientras corre; con las canoas llenas, «5/5» centrado (ficha §4.5). Con la
            // escala de texto (tarea L4; 02 §12: los contenedores crecen), las dos líneas crecen con ella y la placa de 40 dp
            // crece lo que haga falta a lo alto, por igual arriba y abajo de su centro (al 130 %, ceil(37 × 1,3 + 3) = 52 dp:
            // crece 6 dp por cada lado y asoma 2 dp por encima y por debajo de la fila de 48, dentro del hueco de 4 dp hasta TiTino).
            // La escala es la que se guardó al crear los textos, no se deduce de su tamaño.
            bool timer = _canoesTimer.gameObject.activeSelf;
            float s = _canoesTextScale;
            float plateH = timer ? Mathf.Max(40f, Mathf.Ceil((37f * s) + 3f)) : 40f;
            float plateTop = 4f - ((plateH - 40f) * 0.5f);
            float cw = _canoesW; // 128 dp, o menos con el avatar a 320 dp (D-056)
            float textX = 40f - (128f - cw) * (8f / 24f);
            float textW = cw - textX - 6f;
            UiBuilder.Place(_canoesPlate.rectTransform, 0f, plateTop, cw, plateH);
            if (timer)
            {
                float countTop = plateTop + 1f;
                UiBuilder.Place(_canoesCount.rectTransform, textX, countTop, textW, 21f * s);
                UiBuilder.Place(_canoesTimer.rectTransform, textX, countTop + (20f * s), textW, 17f * s);
                UiBuilder.Place(_canoesInfinite.rectTransform, textX + ((textW - 26f) * 0.5f), 9f, 26f, 14f);
            }
            else
            {
                UiBuilder.Place(_canoesCount.rectTransform, textX, 4f, textW, 40f);
                UiBuilder.Place(_canoesInfinite.rectTransform, textX + ((textW - 26f) * 0.5f), 17f, 26f, 14f);
            }
        }

        private void LayoutChip()
        {
            if (!_chip.Visible)
            {
                return;
            }

            // La píldora, 6 dp por encima de Jugar; el sello sobresale 2 dp por debajo y no llega al botón.
            _chip.PlaceCentered(_canvasW * 0.5f, _playTop - ChipGapDp - DifficultyChip.PillBottomDp);
        }

        // El sitio del Cántaro (D-050, punto 6): la tinaja de pie en el muelle, a la izquierda, con la base 2 dp por encima de «Jugar»;
        // si el chip de la etiqueta llega hasta ahí, se arrima al borde y, si ni así cabe, sube por encima del chip.
        private void PlacePiggySlot()
        {
            float pw = MarketLobbyIcons.PiggyWidthDp;
            float ph = MarketLobbyIcons.PiggyHeightDp;
            float x = 14f;
            float y = _playTop - 2f - ph;
            if (_chip.Visible)
            {
                float chipLeft = (_canvasW - _chip.Width) * 0.5f;
                x = Mathf.Max(6f, Mathf.Min(x, chipLeft - 6f - pw));
                float chipTop = _playTop - ChipGapDp - DifficultyChip.PillBottomDp - 4f;
                if (x + pw > chipLeft - 2f)
                {
                    y = Mathf.Min(y, chipTop - ph);
                }
            }

            UiBuilder.Place(_piggySlot, x, y, pw, ph);
        }

        // La caja de cuias de Capi (D-050, punto 6): solo con el muelle sin tarjetas (antes del 10), centrada bajo «Jugar», sin
        // llegar a las pestañas.
        private void PlaceCrate()
        {
            bool on = _dayCardCount == 0;
            if (_crate.gameObject.activeSelf != on)
            {
                _crate.gameObject.SetActive(on);
            }

            if (!on)
            {
                return;
            }

            float k = Mathf.Min(1f, _canvasW / ReferenceWidth);
            float cw = ProceduralArt.DockCrateWidthDp * k;
            float ch = ProceduralArt.DockCrateHeightDp * k;
            float tabTop = _canvasH - _safeBottom - TabBarHeight - 12f;
            float top = _playTop + PlayTouchHeight + 18f;
            float room = tabTop - 8f - top;
            if (room < ch)
            {
                float s = Mathf.Max(0.6f, room / ch);
                cw *= s;
                ch *= s;
            }

            UiBuilder.Place(_crate.rectTransform, (_canvasW - cw) * 0.5f, top, cw, ch);
        }

        // La parada de «Jugar» (M2.1), solo al cambiar de nivel o de catálogo: el fondo (corte directo; con ui_bg_per_stop =
        // false, la gran parada del mundo, como el tablero), la luz sobre el panorama, el agua del río vivo, la travesía hacia la
        // parada siguiente y TiTino con la parada actual. Fuera de 1..1.000 o sin catálogo, nada: el inicio sin parada. En el fin de
        // contenido, si el nivel de «Jugar» no tiene parada en los datos del Mapa (el 701 con las paradas 0-69), la del último
        // nivel publicado (06 §4: fondo de la parada actual; StopLevel).
        private void ApplyStop(AppFlow app, int level)
        {
            RiverStopCatalog? stops = _riverStops;
            int stopLevel = StopLevel(stops, level, app.Catalog.LastLevel);
            bool inRange = stops != null && stopLevel >= 1 && stopLevel <= WorldTable.LastLevel;
            _currentStop = inRange ? stops!.StopOf(stopLevel) : null;
            RiverStop? backdrop = inRange ? GameScreen.StopForLevel(stops, stopLevel, MapConfig.UiBgPerStop) : null;
            LevelDefinition? def = app.Catalog.Find(stopLevel);
            Season season = stopLevel >= 1 && stopLevel <= WorldTable.LastLevel ? GameScreen.SeasonFor(_seasons, stopLevel) : Season.None;
            _stopBackdrop.Apply(backdrop, def != null ? def.WaterBase : app.Catalog.WaterBase, false, 0f, season);
            ApplyStopLut();
            _river.SetWater(_stopBackdrop.Stop != null ? _stopBackdrop.SeasonWaterColor : Palette.RiverDeep);

            // «Faltan N niveles» y el nombre de la parada siguiente del catálogo o, tras la última, de la naciente (FX13), en el idioma
            // (en release, un nombre sin el visto bueno del asesor es «» y el letrero no sale). Con la parada prestada del último
            // nivel publicado (el 701 en el fin de contenido), sin letrero.
            string place = _currentStop != null && stopLevel == level ? UiText.NextPlaceName(stops!, _currentStop) : string.Empty;
            _remainingReady = place.Length > 0;
            int journeyLevel = Mathf.Clamp(level, 1, WorldTable.LastLevel); // el fin de contenido puede ir más allá del 1.000
            int left = RiverJourney.LevelsLeft(journeyLevel);
            _remainingText = _remainingReady ? UiText.MapStopRemaining(left, place) : string.Empty;
            string guide = RiverJourney.GuideFor(WorldTable.WorldOf(Mathf.Clamp(level, 1, WorldTable.LastLevel)));
            _voyage.Apply(place, left, _remainingText, HomeVoyage.ProgressOf(journeyLevel), guide);
            _voyageReady = _currentStop != null;
            ApplyStopVisibility();
        }

        /// <summary>
        /// Nivel cuya parada se enseña en el inicio con <paramref name="level"/> en «Jugar»: el propio nivel, salvo en el fin de
        /// contenido (más allá de <paramref name="lastPublished"/>) cuando ese nivel no tiene parada en los datos del Mapa; entonces,
        /// el último nivel publicado (el 701 con las paradas 0-69 enseña la parada del 700). Con parada, el de siempre.
        /// </summary>
        internal static int StopLevel(RiverStopCatalog? stops, int level, int lastPublished)
        {
            if (stops == null || level <= lastPublished || lastPublished < 1 || stops.StopOf(level) != null)
            {
                return level;
            }

            return lastPublished;
        }

        // Qué se ve del Mapa en el inicio según la pantalla: la travesía, solo en el inicio, con un nivel publicado y sin el bocadillo
        // de TiTino encima; TiTino, tampoco con Sin canoas ni con una pantalla de pago delante (SetCommerceOpen).
        private void ApplyStopVisibility()
        {
            bool lobby = _shown == AppScreen.Lobby;
            bool available = _app == null || _app.PlayLevelAvailable;
            _voyage.SetVisible(lobby && available && _voyageReady);
            bool hideHome = _shown == AppScreen.NoCanoes || _commerceOpen;
            _home.Apply(_currentStop, hideHome);
        }

        // La luz de la parada y de la temporada (SE2) sobre el panorama y el cielo que lo completa (blanco = como antes), como en
        // el tablero.
        private void ApplyStopLut()
        {
            Color lut = _stopBackdrop.LutColor;
            if (_panorama.color != lut)
            {
                _panorama.color = lut;
            }

            Color sky = Palette.SkyTop * lut;
            sky.a = 1f;
            if (_skyFill.color != sky)
            {
                _skyFill.color = sky;
            }
        }

        // TiTino se duerme o se despierta (añadido de D-050 del 2026-10-26): AF_STATE titino_sleep con la causa y lo que dice al
        // despertarse, y otra línea AF_LAYOUT lobby con titino_asleep. Solo al cambiar, nunca por fotograma.
        private void OnSlothAsleepChanged(bool asleep, string cause)
        {
            if (!DevProbe.Enabled || !_canvas.gameObject.activeSelf || _shown != AppScreen.Lobby)
            {
                return;
            }

            HomeSloth? sloth = _home.Sloth;
            string note = sloth != null ? HomeSloth.NoteId(sloth.Note) : "none";
            _probe.Event("titino_sleep", "\"asleep\":" + (asleep ? "true" : "false") + ",\"cause\":\"" + cause + "\",\"note\":\"" + note + "\"");
            EmitLayout();
        }

        private void EmitLayout()
        {
            if (!DevProbe.Enabled || _app == null || !_canvas.gameObject.activeSelf)
            {
                return;
            }

            int level = _app.PlayLevel;
            if (_shown == AppScreen.Lobby)
            {
                // Los botones y lo que se ve del inicio: el letrero de la parada (stop_remaining), TiTino (home_window, y su toque,
                // titino), la canoa y la parada que abren el Mapa (voyage_canoe y voyage_stop) y la Crecida (crecida).
                var ids = new System.Collections.Generic.List<string>(12) { "play" };
                var rects = new System.Collections.Generic.List<RectTransform>(12) { (RectTransform)_play.Button.transform };
                if (_canoes.gameObject.activeSelf)
                {
                    ids.Add("canoes");
                    rects.Add((RectTransform)_canoes.transform);
                }

                ids.Add("settings");
                rects.Add((RectTransform)_settings.Button.transform);
                if (_avatarShown)
                {
                    ids.Add("avatar"); // D-056 (avatares.md §3.6)
                    rects.Add(AvatarRect);
                }

                _audio.VisibleButtons(ids, rects, string.Empty);
                if (_mapTab.Button.gameObject.activeSelf)
                {
                    ids.Add("map_tab");
                    rects.Add(MapTabRect);
                }

                if (StopRemainingShown)
                {
                    ids.Add("stop_remaining");
                    rects.Add(_voyage.SignRect);
                }

                if (_voyage.Interactive)
                {
                    if (_voyage.CanoeButton.gameObject.activeSelf)
                    {
                        ids.Add("voyage_canoe");
                        rects.Add((RectTransform)_voyage.CanoeButton.transform);
                    }

                    if (_voyage.StopButton.gameObject.activeSelf)
                    {
                        ids.Add("voyage_stop");
                        rects.Add((RectTransform)_voyage.StopButton.transform);
                    }
                }

                string? extra = null;
                if (_home.Visible)
                {
                    ids.Add("home_window");
                    rects.Add(_home.Root);
                    HomeSloth? sloth = _home.Sloth;
                    if (sloth != null && sloth.TapButton.gameObject.activeSelf)
                    {
                        ids.Add("titino");
                        rects.Add(sloth.TapRect);
                        extra = sloth.Asleep ? ",\"titino_asleep\":true" : ",\"titino_asleep\":false";
                    }
                }

                if (_streakWidget.Visible)
                {
                    ids.Add("crecida");
                    rects.Add(_streakWidget.Root);
                }

                _probe.Layout("lobby", level, null, ids.ToArray(), rects.ToArray(), extra);
            }
            else if (_shown == AppScreen.NoCanoes)
            {
                var ids = new System.Collections.Generic.List<string>(6);
                var rects = new System.Collections.Generic.List<RectTransform>(6);
                _noCanoes.VisibleButtons(ids, rects);
                _probe.Layout(AppScreenNames.Of(_shown), level, null, ids.ToArray(), rects.ToArray());
            }
            else
            {
                _probe.Layout(AppScreenNames.Of(_shown), level, null, new[] { "back" }, new[] { (RectTransform)_back.Button.transform });
            }
        }

        private static long CeilSeconds(TimeSpan span)
        {
            if (span <= TimeSpan.Zero)
            {
                return 0L;
            }

            return (span.Ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
        }

        // Coloca rt con su pivote en (x, y desde arriba) del padre, con tamaño w × h.
        private static void PlaceAt(RectTransform rt, float x, float yTop, float w, float h, Vector2 pivot)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(x, -yTop);
            rt.sizeDelta = new Vector2(w, h);
        }
    }

    /// <summary>Qué dice y hace el botón Jugar del inicio (D-032, punto 6; <see cref="LobbyScreen.PlayModeFor"/>).</summary>
    public enum PlayButtonMode
    {
        /// <summary>«Jugar · Nivel {n}»: abre ese nivel (o Sin canoas).</summary>
        Level = 0,

        /// <summary>«Jugar» tras acabar el contenido, sin el Reto: abre «El río sigue creciendo».</summary>
        ContentEnd = 1,

        /// <summary>«Jugar · Reto del río» tras acabar el contenido, con el Reto desbloqueado: abre el Reto.</summary>
        Daily = 2,
    }
}
