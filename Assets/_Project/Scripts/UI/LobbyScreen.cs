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
    /// <c>rescue_min_level</c>; con Canoas infinitas, ∞ y el tiempo que queda) y Ajustes (deshabilitado hasta que exista su
    /// pantalla);</description></item>
    /// <item><description>en el centro, el Estuario (panorama, muelle y follaje del nivel) y el botón grande «Jugar · Nivel {n}»
    /// con su centro al 60 % del alto (06 §4.7), con el chip Difícil o Muy difícil encima cuando el nivel lo lleva
    /// [P];</description></item>
    /// <item><description>abajo, la barra de pestañas con «Inicio» [P]: Mercado, Mapa del río, Reto del río y Bitácora quedan
    /// ocultos hasta que existan (D-008).</description></item>
    /// </list>
    /// Encima, cuando toca, «El río sigue creciendo» (06 §9) o Sin canoas (04 §7.3, con sus opciones desde B2-3:
    /// <see cref="NoCanoesPanel"/>). Lienzo de 360 dp de ancho y al menos 640 de alto. Los contadores se actualizan solo al cambiar
    /// y sin asignar memoria (08 §5.4).
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
        private const float ChipHeight = 26f;
        private const float HomeTabWidth = 120f;
        private const float OverlayPanelHeight = 236f;
        private const float MaxHorizonDp = 200f;
        private const float RiverPropsMinBandDp = 56f;

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
        private readonly Image _chip;
        private readonly TextMeshProUGUI _chipLabel;

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
        private GameServices? _services;

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

            // Ajustes (07 §7.6: engranaje de madera, disco de 40 dp en un área de 48). Su pantalla aún no existe: deshabilitado.
            _settings = UiBuilder.Relief("SettingsButton", top, art.DiscWood, () => _onSettings());
            RectTransform settingsVisual = (RectTransform)_settings.Face.parent;
            settingsVisual.anchorMin = new Vector2(0.5f, 1f);
            settingsVisual.anchorMax = new Vector2(0.5f, 1f);
            settingsVisual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            settingsVisual.anchoredPosition = new Vector2(0f, -4f);
            Image gear = UiBuilder.Image("Icon", _settings.Face, art.IconGear, Color.white);
            UiBuilder.Place(gear.rectTransform, 7f, 7f, 26f, 26f);
            _settings.Relief.Bind(_settings.Face, _settings.Base, _settings.FaceImage, art.DiscWood, gear, art.IconGearDisabled, null);
            UiBuilder.SetEnabled(_settings.Button, false);

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

            // ---- Botón central «Jugar · Nivel {n}» (familia Hoja, el primario de 07 §7.2) y chip de dificultad [P]. ----
            _chip = UiBuilder.Sliced("DifficultyChip", _root, art.RoundedRect, Palette.UiAmber);
            _chip.pixelsPerUnitMultiplier = 24f / (ChipHeight * 0.5f); // borde 9-slice de 24 px → media altura: píldora
            _chipLabel = UiBuilder.Label("Label", _chip.transform, TextStyle.PlainBold, string.Empty, 14f, Palette.Ink, TextAlignmentOptions.Center);
            UiBuilder.Stretch(_chipLabel.rectTransform);
            _chip.gameObject.SetActive(false);

            _play = UiBuilder.Relief("PlayButton", _root, art.CtaLeafLarge, () => _onPlay());
            _playLabel = UiBuilder.Label("Label", _play.Face, TextStyle.CtaLeaf, UiText.PlayLevel(1), 30f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(_playLabel.rectTransform, art.CtaLeafLarge);
            UiBuilder.AutoSize(_playLabel, 22f, 30f);
            _play.Relief.Bind(_play.Face, _play.Base, _play.FaceImage, art.CtaLeafLarge, null, null, _playLabel);

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

        /// <summary>Botón de Ajustes (deshabilitado hasta que exista su pantalla).</summary>
        public Button SettingsButton => _settings.Button;

        /// <summary>Botón Volver de la superposición («Volver al inicio» en Sin canoas).</summary>
        public Button BackButton => _noCanoes.Visible ? _noCanoes.HomeButton : _back.Button;

        /// <summary>Contador de semillas.</summary>
        public RectTransform SeedsRect => _seeds;

        /// <summary>Pestaña «Inicio».</summary>
        public RectTransform HomeTabRect => _homeTab;

        /// <summary>Chip de dificultad.</summary>
        public RectTransform ChipRect => _chip.rectTransform;

        /// <summary>El contador de canoas se ve (desde el 31).</summary>
        public bool CanoesShown => _canoes.gameObject.activeSelf;

        /// <summary>El chip de dificultad se ve.</summary>
        public bool ChipShown => _chip.gameObject.activeSelf;

        /// <summary>Texto del chip de dificultad.</summary>
        public string ChipText => _chipLabel.text;

        /// <summary>Semillas que muestra el contador.</summary>
        public int SeedsShown => _seedsShown;

        /// <summary>Canoas que muestra el contador («n» de «n/5»; −1 con Canoas infinitas o si no se ve).</summary>
        public int CanoesCountShown => _canoesModeShown == 2 ? -1 : _canoesCountShown;

        /// <summary>Segundos del cronómetro de canoas que se muestran (−1 si no corre).</summary>
        public long CanoesTimerShown => _timerShown;

        /// <summary>
        /// Pares texto / fondo de la pantalla de inicio y su superposición, para la comprobación de contraste ≥ 4,5:1
        /// (07 §1.2 y §7.8): contadores sobre madera oscura, botón Jugar y Volver sobre hoja, chip Difícil (tinta sobre ámbar)
        /// y Muy difícil (crema sobre índigo), pestaña sobre madera y el texto de la superposición sobre la madera clara.
        /// </summary>
        public static (string Name, Color Text, Color Background)[] ContrastPairs()
        {
            return new[]
            {
                ("contadores", (Color)Palette.Cream, (Color)Palette.WoodDark),
                ("jugar", (Color)Palette.Cream, Palette.FamilyLeaf.Base),
                ("jugar (fondo de la cara)", (Color)Palette.Cream, Palette.FamilyLeaf.Deep),
                ("chip Difícil", (Color)Palette.Ink, (Color)Palette.UiAmber),
                ("chip Muy difícil", (Color)Palette.Cream, (Color)Palette.UiIndigo),
                ("pestaña Inicio", (Color)Palette.Cream, Palette.FamilyWood.Base),
                ("pestaña Inicio (fondo de la cara)", (Color)Palette.Cream, Palette.FamilyWood.Deep),
                ("texto de la superposición", Palette.FamilyWood.Base, (Color)Palette.PanelLightLow),
            };
        }

        /// <summary>Asocia la navegación y los servicios de Sin canoas (antes de la primera pantalla).</summary>
        public void Bind(AppFlow app, GameServices? services = null)
        {
            _app = app;
            _services = services;
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

            string label = app.PlayLevelLabel;
            if (!string.Equals(label, _labelShown, StringComparison.Ordinal))
            {
                _labelShown = label;
                ApplyChip(label);
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

        private void ApplyChip(string label)
        {
            string? text = UiText.DifficultyChip(label);
            _chip.gameObject.SetActive(text != null);
            if (text == null)
            {
                return;
            }

            // 07 §7.7: Difícil en ámbar con tinta; Muy difícil en índigo con crema (sin calavera).
            bool veryHard = label == LevelDefinition.LabelVeryHard;
            _chip.color = veryHard ? (Color)Palette.UiIndigo : (Color)Palette.UiAmber;
            _chipLabel.color = veryHard ? (Color)Palette.Cream : (Color)Palette.Ink;
            _chipLabel.text = text;
            LayoutChip();
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
            float playW = Mathf.Min(PlayMaxWidth, w - 64f);
            UiBuilder.Place((RectTransform)_play.Button.transform, (w - playW) * 0.5f, _playTop, playW, PlayTouchHeight);
            LayoutChip();

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
            if (!_chip.gameObject.activeSelf)
            {
                return;
            }

            float width = Mathf.Max(84f, Mathf.Ceil(_chipLabel.preferredWidth) + 28f);
            UiBuilder.Place(_chip.rectTransform, (_canvasW - width) * 0.5f, _playTop - ChipHeight - 6f, width, ChipHeight);
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
                if (_canoes.gameObject.activeSelf)
                {
                    _probe.Layout("lobby", level, null, new[] { "play", "canoes", "settings" }, new[] { (RectTransform)_play.Button.transform, (RectTransform)_canoes.transform, (RectTransform)_settings.Button.transform });
                }
                else
                {
                    _probe.Layout("lobby", level, null, new[] { "play", "settings" }, new[] { (RectTransform)_play.Button.transform, (RectTransform)_settings.Button.transform });
                }
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
