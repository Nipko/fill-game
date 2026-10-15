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
    /// Pantalla de inicio (Lobby, GDD 06 §4.7; D-008 punto 6; tarea B1-6) construida por código, con la composición de
    /// Magic Sort (ficha §7.1: saldos arriba, botón central grande, pestañas abajo) y sin su trade dress (07 §1.3; D-003:
    /// madera, hoja y agua; ni oro biselado, ni gemas, ni gatos):
    /// <list type="bullet">
    /// <item><description>arriba, Semillas, Canoas «n/5» con el cronómetro mm:ss de la próxima (solo desde el 31,
    /// <c>rescue_min_level</c>; con Canoas infinitas, ∞ y el tiempo que queda) y Ajustes, que abre su pantalla
    /// (<see cref="SettingsScreen"/>; tarea L3);</description></item>
    /// <item><description>en el centro, el Estuario (panorama, muelle y follaje del nivel) y el botón grande «Jugar · Nivel {n}»
    /// con su centro al 60 % del alto (06 §4.7), con la etiqueta de D-009 encima cuando el nivel la lleva (propuesta §c.1;
    /// 07 §7.7; <see cref="DifficultyChip"/>): Difícil, chip ámbar con la rana dardo y la cinta «×3»; Muy difícil, chip índigo
    /// con el jaguar, «×4» y el botón en variante nocturna [P];</description></item>
    /// <item><description>abajo, la barra de pestañas con «Inicio» [P]: Mercado, Mapa del río, Reto del río y Bitácora quedan
    /// ocultos hasta que existan (D-008).</description></item>
    /// </list>
    /// Encima, cuando toca, «El río sigue creciendo» (06 §9) o Sin canoas (04 §7.3, con sus opciones desde B2-3:
    /// <see cref="NoCanoesPanel"/>). Lienzo de 360 dp de ancho y al menos 640 de alto. Los contadores se actualizan solo al cambiar
    /// y sin asignar memoria (08 §5.4).
    /// <para>Mapa del río en el inicio (D-013, punto 6, Q17-A; 06 §4.7; tarea M2.1), con el catálogo de paradas que le pasa
    /// <see cref="GameBootstrap"/> (<see cref="RiverStops"/>; sin él, todo como antes):</para>
    /// <list type="bullet">
    /// <item><description><b>el sitio</b>: su propio <see cref="AF.Presentation.StopBackdrop"/> con la parada de «Jugar»
    /// (<see cref="RiverStopCatalog.StopOf"/> de <see cref="AppFlow.PlayLevel"/>; el mismo fondo que el tablero de ese nivel,
    /// <see cref="GameScreen.StopForLevel"/>), con su luz sobre el panorama y corte directo al cambiar de nivel;</description></item>
    /// <item><description><b>«Faltan {n} niveles para {lugar}»</b> (<c>map_stop_remaining</c>, con el plural de la tabla) bajo
    /// «Jugar», en Nunito Bold de <see cref="StopRemainingSp"/> sp sobre madera, hacia la parada siguiente; se arma solo al
    /// cambiar de nivel y no sale con el fin de contenido, con Sin canoas ni si esa parada no está en el catálogo;</description></item>
    /// <item><description><b>la ventanita de casa</b> (<see cref="HomeWindow"/>): el único sitio de TiTino en el inicio, a la
    /// izquierda bajo los saldos [P], sin tapar Ajustes, los saldos ni «Jugar»; se oculta con Sin canoas y con la hoja de compra o
    /// una oferta delante (<see cref="SetCommerceOpen"/>; propuesta del personaje g.2 y d.11).</description></item>
    /// </list>
    /// </summary>
    public sealed class LobbyScreen
    {
        /// <summary>Ancho del lienzo (dp).</summary>
        public const float ReferenceWidth = 360f;

        /// <summary>Alto mínimo del lienzo (dp): se comprueba sin solapes a 360 × 640 y a 360 × 800.</summary>
        public const float MinHeight = 640f;

        /// <summary>Centro del botón Jugar sobre el alto del lienzo (06 §4.7: «60 % del alto visual»).</summary>
        public const float PlayCenterFraction = 0.60f;

        /// <summary>Lado mínimo de un control (07 §1.2 y §7.8).</summary>
        public const float MinTouchDp = 48f;

        private const float TopRowHeight = 48f;
        private const float TabBarHeight = 64f;
        private const float PlayTouchHeight = 72f;
        private const float PlayMaxWidth = 296f;
        private const float ChipGapDp = 6f;
        private const float HomeTabWidth = 120f;
        private const float OverlayPanelHeight = 236f;
        private const float MaxHorizonDp = 200f;
        private const float RiverPropsMinBandDp = 56f;
        private const float RemainingGapDp = 8f;
        private const float RemainingPadDp = 12f;
        private const float RemainingLineDp = 28f;
        private const float HomeWindowGapDp = 8f;
        private const float SideGutterDp = 16f;

        /// <summary>Cuerpo (sp) de «Faltan {n} niveles para {lugar}» bajo «Jugar» (M2.1), en Nunito Bold.</summary>
        public const float StopRemainingSp = 14f;

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
        private readonly Image _heron;
        private readonly Image[] _lilyPads = new Image[3];
        private readonly Image _dock;
        private readonly Image[] _foliage = new Image[4];
        private readonly Image _scrim;
        private readonly StopBackdrop _stopBackdrop;

        private readonly RectTransform _seeds;
        private readonly Image _seedsIcon;
        private readonly TextMeshProUGUI _seedsText;
        private readonly Button _canoes;
        private readonly Image _canoesPlate;
        private readonly Image _canoesIcon;
        private readonly Image _canoesInfinite;
        private readonly TextMeshProUGUI _canoesCount;
        private readonly TextMeshProUGUI _canoesTimer;
        private readonly ReliefParts _settings;

        private readonly ReliefParts _play;
        private readonly TextMeshProUGUI _playLabel;
        private readonly DifficultyChip _chip;
        private bool _playNight;

        private readonly Image _tabBar;
        private readonly RectTransform _homeTab;
        private readonly TextMeshProUGUI _homeTabLabel;

        private readonly RectTransform _overlay;
        private readonly RectTransform _overlayPanel;
        private readonly Image _overlaySurface;
        private readonly Image _overlayHeader;
        private readonly TextMeshProUGUI _overlayTitle;
        private readonly TextMeshProUGUI _overlayBody;
        private readonly ReliefParts _back;
        private readonly NoCanoesPanel _noCanoes;
        private readonly RectTransform _remaining;
        private readonly TextMeshProUGUI _remainingLabel;
        private readonly HomeWindow _home;
        private GameServices? _services;
        private RiverStopCatalog? _riverStops;
        private RiverStop? _currentStop;
        private RiverStop? _nextStop;
        private bool _remainingReady;
        private bool _commerceOpen;
        private int _stopLevelShown = -1;

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

            // ---- Escena: el Estuario del nivel (panorama, garza, vitórias-régias, muelle y follaje; D-003). ----
            RectTransform scene = UiBuilder.Rect("Scene", _root);
            Image baseFill = UiBuilder.Image("BaseFill", scene, null, Palette.RiverFar);
            UiBuilder.Stretch(baseFill.rectTransform);
            _skyFill = UiBuilder.Image("SkyFill", scene, null, Palette.SkyTop);
            _panorama = UiBuilder.Image("Panorama", scene, art.Panorama, Color.white);

            // Fondo de la parada de «Jugar» (M2.1): como en el tablero, delante del panorama y detrás de la garza.
            _stopBackdrop = new StopBackdrop(scene, art);
            _heron = UiBuilder.Image("Heron", scene, art.Heron, Color.white);
            for (int i = 0; i < _lilyPads.Length; i++)
            {
                _lilyPads[i] = UiBuilder.Image("LilyPad" + i, scene, art.LilyPad, Color.white);
            }

            _dock = UiBuilder.Image("Dock", scene, art.Dock, Color.white);
            _foliage[0] = UiBuilder.Image("FoliageTL", scene, art.FoliageTopLeft, Color.white);
            _foliage[1] = UiBuilder.Image("FoliageTR", scene, art.FoliageTopRight, Color.white);
            _foliage[2] = UiBuilder.Image("FoliageBL", scene, art.FoliageBottomLeft, Color.white);
            _foliage[3] = UiBuilder.Image("FoliageBR", scene, art.FoliageBottomRight, Color.white);

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
            _canoesTimer = UiBuilder.Label("Timer", _canoes.transform, TextStyle.PillWood, string.Empty, 13f, Palette.Cream, TextAlignmentOptions.Center);

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

            // ---- Pestañas abajo: solo «Inicio» [P] mientras Mercado, Mapa del río, Reto del río y Bitácora no existan. ----
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

            // ---- Botón central «Jugar · Nivel {n}» (familia Hoja, el primario de 07 §7.2; nocturno en Muy difícil) y el chip de
            // la etiqueta de D-009 encima (propuesta §c.1). ----
            _chip = DifficultyChip.Create(_root, art);

            _play = UiBuilder.Relief("PlayButton", _root, art.CtaLeafLarge, () => _onPlay());
            _playLabel = UiBuilder.Label("Label", _play.Face, TextStyle.CtaLeaf, UiText.PlayLevel(1), 30f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_playLabel.rectTransform, art.CtaLeafLarge);
            UiBuilder.AutoSize(_playLabel, 22f, 30f);
            _play.Relief.Bind(_play.Face, _play.Base, _play.FaceImage, art.CtaLeafLarge, null, null, _playLabel);

            // ---- «Faltan {n} niveles para {lugar}» bajo «Jugar» y la ventanita de casa de TiTino (M2.1), sin toques. ----
            _remaining = UiBuilder.Rect("StopRemaining", _root);
            Image remainingPlate = UiBuilder.Sliced("Plate", _remaining, art.WoodPlate, Color.white);
            UiBuilder.Stretch(remainingPlate.rectTransform);
            _remainingLabel = UiBuilder.Label("Label", _remaining, TextStyle.PlainBold, string.Empty, StopRemainingSp, Palette.Cream, TextAlignmentOptions.Center);
            _remainingLabel.textWrappingMode = TextWrappingModes.Normal;
            _remaining.gameObject.SetActive(false);
            _home = new HomeWindow(_root, art);

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
                () => actions.PressInfiniteCanoes(), () => actions.PressNoCanoesHome(), () => actions.PressNoCanoesClose());

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
        /// Paradas del Mapa del río (<c>af-river-stops/1</c>; M2.1). Null = sin el archivo o inválido: el inicio de siempre, sin
        /// fondo de parada, sin «Faltan…» y sin ventanita. Al cambiarlo, el inicio se rehace en el siguiente fotograma.
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

        /// <summary>Fondo de la parada del inicio (tests y capturas).</summary>
        internal StopBackdrop StopBackdrop => _stopBackdrop;

        /// <summary>Tinte del panorama: la luz de la parada (blanco = como antes; tests).</summary>
        internal Color PanoramaTint => _panorama.color;

        /// <summary>Ventanita de casa de TiTino (tests y capturas).</summary>
        internal HomeWindow HomeWindow => _home;

        /// <summary>«Faltan {n} niveles para {lugar}» se ve.</summary>
        public bool StopRemainingShown => _remaining.gameObject.activeSelf;

        /// <summary>Texto de «Faltan {n} niveles para {lugar}» (el último armado; vacío sin parada siguiente).</summary>
        public string StopRemainingText => _remainingLabel.text;

        /// <summary>Caja de «Faltan {n} niveles para {lugar}».</summary>
        public RectTransform StopRemainingRect => _remaining;

        /// <summary>Texto de «Faltan…» (tests: tamaño y fuente).</summary>
        internal TextMeshProUGUI StopRemainingLabel => _remainingLabel;

        /// <summary>
        /// Hay una pantalla de pago delante del inicio (la hoja de compra, un anuncio o una oferta; propuesta del personaje g.2,
        /// regla 9, y d.11): la ventanita de casa se oculta mientras dure. Lo avisa <see cref="GameBootstrap"/>.
        /// </summary>
        public bool CommerceOpen => _commerceOpen;

        /// <summary>Ancho del lienzo en dp.</summary>
        public float CanvasWidth => _canvasW;

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

        /// <summary>Contador de canoas (botón: con 0/5 abre Sin canoas).</summary>
        public Button CanoesButton => _canoes;

        /// <summary>Botón de Ajustes: abre <see cref="SettingsScreen"/> (tarea L3).</summary>
        public Button SettingsButton => _settings.Button;

        /// <summary>Botón Volver de la superposición («Volver al inicio» en Sin canoas).</summary>
        public Button BackButton => _noCanoes.Visible ? _noCanoes.HomeButton : _back.Button;

        /// <summary>Contador de semillas.</summary>
        public RectTransform SeedsRect => _seeds;

        /// <summary>Pestaña «Inicio».</summary>
        public RectTransform HomeTabRect => _homeTab;

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

        /// <summary>Canoas que muestra el contador («n» de «n/5»; −1 con Canoas infinitas o si no se ve).</summary>
        public int CanoesCountShown => _canoesModeShown == 2 ? -1 : _canoesCountShown;

        /// <summary>Segundos del cronómetro de canoas que se muestran (−1 si no corre).</summary>
        public long CanoesTimerShown => _timerShown;

        /// <summary>
        /// Pares texto / fondo de la pantalla de inicio y su superposición, para la comprobación de contraste ≥ 4,5:1
        /// (07 §1.2 y §7.8): contadores sobre madera oscura, botón Jugar y Volver sobre hoja, Jugar nocturno (marfil sobre
        /// índigo), pestaña sobre madera, el texto de la superposición sobre la madera clara y, al final, el chip y su cinta
        /// (<see cref="DifficultyChip.ContrastPairs"/>: tinta sobre ámbar, marfil sobre índigo y crema sobre el agua de la cinta).
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
                ("faltan N (stop_remaining)", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("ventanita: casa a unos N km", (Color)Palette.Cream, (Color)Palette.WoodDark),
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
        /// Avisa de que hay (o ya no hay) una pantalla de pago delante del inicio: la hoja de compra, un anuncio o una oferta. La
        /// ventanita de casa no sale en ellas (D-013, punto 6; propuesta del personaje g.2, regla 9, y d.11). Sin asignar memoria.
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
            if (_app != null)
            {
                _probe.App(AppScreenNames.Of(screen), _app);
            }

            EmitLayout();
        }

        /// <summary>Avanza la pantalla <paramref name="dtMs"/> milisegundos: métricas de pantalla, contadores y cronómetros.</summary>
        public void Tick(float dtMs)
        {
            if (!_canvas.gameObject.activeSelf)
            {
                return;
            }

            GameScreen.ReadScreen(out int sw, out int sh, out Rect safe);
            if (sw != _screenW || sh != _screenH || safe != _safeArea)
            {
                ApplyScreenMetrics();
                EmitLayout();
            }

            Refresh();
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
            if (level != _playLevelShown)
            {
                _playLevelShown = level;
                _playLabel.text = UiText.PlayLevel(level);
            }

            if (level != _stopLevelShown)
            {
                _stopLevelShown = level;
                ApplyStop(app, level);
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
            if (_shown == AppScreen.NoCanoes)
            {
                _noCanoes.Refresh(_services);
            }
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

            // 360 dp de ancho; si la pantalla es tan ancha que quedarían menos de 640 dp de alto, manda el alto.
            float scale = _screenW / ReferenceWidth;
            if (_screenH / scale < MinHeight)
            {
                scale = _screenH / MinHeight;
            }

            scale = Mathf.Max(0.01f, scale);
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
            bool props = beam - horizon >= RiverPropsMinBandDp;
            _heron.gameObject.SetActive(props);
            if (props)
            {
                PlaceAt(_heron.rectTransform, 34f * k, horizon + (5.4f * k), 22f * k, 31f * k, new Vector2(0.5f, 0f));
            }

            PlaceLilyPad(0, props, 54f * k, horizon + (30f * k), 34f * k, beam);
            PlaceLilyPad(1, props, 314f * k, horizon + (22f * k), 26f * k, beam);
            PlaceLilyPad(2, props, 86f * k, horizon + (47f * k), 24f * k, beam);

            // Fondo de la parada (M2.1): sin tablero, con la banda de agua hasta la viga del muelle, como en el tablero.
            _stopBackdrop.Layout(w, horizon, _playTop, false, beam + 8f);
            PlaceAt(_foliage[0].rectTransform, -20f, 0f, 160f, 160f, new Vector2(0f, 1f));
            PlaceAt(_foliage[1].rectTransform, w + 20f, 0f, 160f, 160f, new Vector2(1f, 1f));
            float bottom = ProceduralArt.BottomFoliageDp;
            PlaceAt(_foliage[2].rectTransform, 0f, tabTop + 24f, bottom, bottom, new Vector2(0f, 0f));
            PlaceAt(_foliage[3].rectTransform, w, tabTop + 24f, bottom, bottom, new Vector2(1f, 0f));
            UiBuilder.Place(_scrim.rectTransform, 0f, 0f, w, _safeTop + 48f);

            // Cabecera (ficha §7.1): Semillas a la izquierda, Canoas a su derecha y Ajustes en la esquina.
            UiBuilder.Place(_seeds, 16f, topY + 4f, 112f, 40f);
            UiBuilder.Place(_seedsIcon.rectTransform, -8f, 1f, 38f, 38f);
            UiBuilder.Place(_seedsText.rectTransform, 30f, 0f, 74f, 40f);
            UiBuilder.Place((RectTransform)_canoes.transform, 140f, topY, 128f, TopRowHeight);
            UiBuilder.Place(_canoesPlate.rectTransform, 0f, 4f, 128f, 40f);
            UiBuilder.Place(_canoesIcon.rectTransform, -8f, 11f, 48f, 28f);
            LayoutCanoesContent();
            UiBuilder.Place((RectTransform)_settings.Button.transform, w - 8f - MinTouchDp, topY, MinTouchDp, MinTouchDp);

            // Botón central y chip.
            float playW = PlayWidth(w);
            UiBuilder.Place((RectTransform)_play.Button.transform, (w - playW) * 0.5f, _playTop, playW, PlayTouchHeight);
            LayoutChip();
            LayoutRemaining();

            // Ventanita de casa (M2.1): a la izquierda, bajo los saldos [P]; la píldora de la distancia, a su derecha.
            _home.Place(SideGutterDp, topY + TopRowHeight + HomeWindowGapDp, w - SideGutterDp);

            // Pestañas: la barra cubre hasta el borde inferior (y la zona de gestos); «Inicio» sobresale 12 dp.
            UiBuilder.Place(_tabBar.rectTransform, -8f, tabTop, w + 16f, h - tabTop + 12f);
            UiBuilder.Place(_homeTab, (w - HomeTabWidth) * 0.5f, tabTop - 12f, HomeTabWidth, _art.CtaWood.HeightDp);

            // Superposición: tarjeta centrada en vertical, que tapa el botón Jugar atenuado (Magic Sort cubre lo de detrás con
            // la tarjeta y un fondo oscurecido, ficha §4.4 y §4.8), con su botón en la mitad inferior.
            float panelW = Mathf.Min(328f, w - 32f);
            float inner = panelW - 32f;
            UiBuilder.Place(_overlayPanel, (w - panelW) * 0.5f, Mathf.Round((h - OverlayPanelHeight) * 0.5f), panelW, OverlayPanelHeight);
            _overlaySurface.sprite = _art.ModalSurfaceFor(panelW - 16f, OverlayPanelHeight - 16f);
            UiBuilder.Place(_overlayHeader.rectTransform, 16f, 16f, inner, 40f);
            UiBuilder.Place(_overlayBody.rectTransform, 16f, 66f, inner, 66f);
            UiBuilder.Place((RectTransform)_back.Button.transform, 16f, 148f, inner, 64f);
        }

        // Ancho de «Jugar» en un lienzo de w dp: también el tope de la placa de «Faltan…».
        private static float PlayWidth(float w)
        {
            return Mathf.Min(PlayMaxWidth, w - 64f);
        }

        private void LayoutCanoesContent()
        {
            // «n/5» arriba y el cronómetro debajo mientras corre; con las canoas llenas, «5/5» centrado (ficha §4.5).
            bool timer = _canoesTimer.gameObject.activeSelf;
            if (timer)
            {
                UiBuilder.Place(_canoesCount.rectTransform, 40f, 5f, 82f, 21f);
                UiBuilder.Place(_canoesTimer.rectTransform, 40f, 25f, 82f, 17f);
                UiBuilder.Place(_canoesInfinite.rectTransform, 40f + ((82f - 26f) * 0.5f), 9f, 26f, 14f);
            }
            else
            {
                UiBuilder.Place(_canoesCount.rectTransform, 40f, 4f, 82f, 40f);
                UiBuilder.Place(_canoesInfinite.rectTransform, 40f + ((82f - 26f) * 0.5f), 17f, 26f, 14f);
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

        // La parada de «Jugar» (M2.1), solo al cambiar de nivel o de catálogo: el fondo (corte directo; con ui_bg_per_stop =
        // false, la gran parada del mundo, como el tablero), la luz sobre el panorama, «Faltan…» hacia la parada siguiente y la
        // ventanita con la parada actual. Fuera de 1..1.000 o sin catálogo, nada: el inicio de siempre.
        private void ApplyStop(AppFlow app, int level)
        {
            RiverStopCatalog? stops = _riverStops;
            bool inRange = stops != null && level >= 1 && level <= WorldTable.LastLevel;
            _currentStop = inRange ? stops!.StopOf(level) : null;
            _nextStop = _currentStop != null ? stops!.At(_currentStop.N + 1) : null;
            RiverStop? backdrop = inRange ? GameScreen.StopForLevel(stops, level, MapConfig.UiBgPerStop) : null;
            LevelDefinition? def = app.Catalog.Find(level);
            _stopBackdrop.Apply(backdrop, def != null ? def.WaterBase : app.Catalog.WaterBase, false, 0f);
            ApplyStopLut();

            // «Faltan N»: solo con la parada siguiente en el catálogo y con su nombre en el idioma (en release, un nombre sin el
            // visto bueno del asesor es «» y la línea no sale).
            string place = _nextStop != null ? UiText.StopName(_nextStop) : string.Empty;
            _remainingReady = _nextStop != null && place.Length > 0;
            _remainingLabel.text = _remainingReady ? UiText.MapStopRemaining(RiverJourney.LevelsLeft(level), place) : string.Empty;
            LayoutRemaining();
            ApplyStopVisibility();
        }

        // Qué se ve del Mapa en el inicio según la pantalla: «Faltan…» no sale con el fin de contenido ni con Sin canoas; la
        // ventanita, tampoco con Sin canoas ni con una pantalla de pago delante (SetCommerceOpen).
        private void ApplyStopVisibility()
        {
            bool lobby = _shown == AppScreen.Lobby;
            bool available = _app == null || _app.PlayLevelAvailable;
            bool remaining = lobby && available && _remainingReady;
            if (_remaining.gameObject.activeSelf != remaining)
            {
                _remaining.gameObject.SetActive(remaining);
            }

            bool hideHome = _shown == AppScreen.NoCanoes || _commerceOpen;
            _home.Apply(_currentStop, hideHome);
        }

        // «Faltan…» centrado bajo «Jugar», en una placa de madera a la medida del texto y nunca más ancha que el botón (la columna
        // que marca «Jugar»); si no cabe en una línea, en dos (su caja crece; 02 §12). Se mide al colocar, nunca por fotograma.
        private void LayoutRemaining()
        {
            float w = _canvasW;
            float maxW = PlayWidth(w);
            float textW = string.IsNullOrEmpty(_remainingLabel.text) ? 0f : Mathf.Ceil(UiBuilder.MinTextWidth(_remainingLabel));
            float boxW = Mathf.Min(maxW, textW + (2f * RemainingPadDp));
            float innerW = boxW - (2f * RemainingPadDp);
            float boxH = RemainingLineDp;
            if (textW > innerW + 0.5f)
            {
                boxH = Mathf.Max(boxH, Mathf.Ceil(UiBuilder.MinTextHeight(_remainingLabel, innerW)) + 8f);
            }

            float top = _playTop + PlayTouchHeight + RemainingGapDp;
            UiBuilder.Place(_remaining, (w - boxW) * 0.5f, top, boxW, boxH);
            UiBuilder.Place(_remainingLabel.rectTransform, RemainingPadDp, 0f, innerW, boxH);
        }

        // La luz de la parada sobre el panorama y el cielo que lo completa (blanco = como antes), como en el tablero.
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

        private void EmitLayout()
        {
            if (!DevProbe.Enabled || _app == null || !_canvas.gameObject.activeSelf)
            {
                return;
            }

            int level = _app.PlayLevel;
            if (_shown == AppScreen.Lobby)
            {
                // Los botones y, si se ven, «Faltan…» (stop_remaining) y la ventanita de casa (home_window) de M2.1, sin toques.
                var ids = new System.Collections.Generic.List<string>(6) { "play" };
                var rects = new System.Collections.Generic.List<RectTransform>(6) { (RectTransform)_play.Button.transform };
                if (_canoes.gameObject.activeSelf)
                {
                    ids.Add("canoes");
                    rects.Add((RectTransform)_canoes.transform);
                }

                ids.Add("settings");
                rects.Add((RectTransform)_settings.Button.transform);
                if (_remaining.gameObject.activeSelf)
                {
                    ids.Add("stop_remaining");
                    rects.Add(_remaining);
                }

                if (_home.Visible)
                {
                    ids.Add("home_window");
                    rects.Add(_home.Root);
                }

                _probe.Layout("lobby", level, null, ids.ToArray(), rects.ToArray());
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

        private void PlaceLilyPad(int i, bool props, float x, float y, float width, float limit)
        {
            float height = width / 3f;
            bool on = props && y + (height * 0.5f) <= limit - 8f;
            _lilyPads[i].gameObject.SetActive(on);
            if (on)
            {
                PlaceAt(_lilyPads[i].rectTransform, x, y, width, height, new Vector2(0.5f, 0.5f));
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
}
