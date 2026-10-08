#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Pantalla de nivel construida por código sobre un Canvas de 360 dp de ancho de referencia (GDD 02 §5; 07 §7.4) con
    /// la dirección visual v2 «Selva brillante premium» (D-003; docs/art/direccion-visual-v2.md): panorama del Estuario,
    /// muelle y follaje; panel azul petróleo con marco tallado y una repisa por fila; HUD con insignia y Reiniciar en las
    /// posiciones de 07 §7.4; bandeja con Deshacer · Marea; modal Aguas estancadas (02 §7; 07 §7.5) y victoria con
    /// cinta-hoja y estallido (02 §6), con Siguiente y el secundario «Inicio» [P] (06 §4.7; D-008 punto 3). Escucha a
    /// <see cref="LevelFlow"/> y hace que la vista persiga al modelo (02 §3.2). Se oculta mientras se ve la pantalla de
    /// inicio (<see cref="SetVisible"/>).
    /// </summary>
    /// <remarks>
    /// Capas de atrás adelante (guía 3.4): panorama, ambiente lejano, muelle, follaje, velo de escena; captura de fondo;
    /// tablero (sombra, relleno, sombra interior y marco del panel, repisas, cuias, agua de inmersión, chorros y efectos);
    /// Capi y el poste; velo de estado, HUD y bandeja; polen; capa de vertido (el origen y su chorro mientras vierte, delante
    /// de todo, D-006); estallido; modales; captura de saltos. Las capas que no son botones ni áreas de cuia no reciben
    /// toques (V7).
    /// </remarks>
    public sealed class GameScreen : ILevelFlowListener
    {
        /// <summary>Ancho de referencia del lienzo (dp).</summary>
        public const float ReferenceWidth = 360f;

        /// <summary>Alto de referencia del lienzo (dp; 19,5:9).</summary>
        public const float ReferenceHeight = 780f;

        /// <summary>Zona segura superior mínima (02 §5).</summary>
        public const float MinSafeTop = 32f;

        /// <summary>Zona segura inferior mínima (02 §5).</summary>
        public const float MinSafeBottom = 24f;

        /// <summary>Banda mínima del muelle bajo el panel para enseñar a Capi (guía 3.7).</summary>
        public const float CapiMinBandDp = 64f;

        private const float PanelSlideMs = 250f;
        private const float OverlayFadeMs = 200f;

        /// <summary>Panel Aguas estancadas (guía 5.3): 328 × 232 en v0 (07 §7.5 fija 300 contando el rescate, desde el 31).</summary>
        private const float StuckPanelDp = 232f;

        /// <summary>
        /// Panel de victoria (guía 5.3, ajustado al contenido en la revisión visual del 2026-10-07): 30 dp de cinta dentro +
        /// 12 + 56 (CTA y canto) + 12 (eran 118 dp hasta el marco) y, desde la tarea B1-6, el secundario «Inicio» [P] (07 §7.3:
        /// 40 dp de cara, área táctil de 48) en <see cref="VictoryHomeTopDp"/>, más 12 + 8 de marco: 176 dp. La fila de 44 dp
        /// de «Semillas +N» llega con la victoria de B2-4 (guía 12).
        /// </summary>
        private const float VictoryPanelDp = 176f;

        /// <summary>Borde superior del secundario «Inicio» dentro del panel de victoria (bajo el CTA y su sombra).</summary>
        private const float VictoryHomeTopDp = 110f;

        /// <summary>Ancho del secundario «Inicio» (dp).</summary>
        private const float VictoryHomeWidthDp = 168f;

        /// <summary>Borde superior del CTA dentro del panel de victoria (centrado entre la cinta y el marco inferior).</summary>
        private const float VictoryCtaTopDp = 42f;

        /// <summary>
        /// El horizonte del panorama baja hasta quedar 64 dp por encima del panel, entre 118 y 200 dp (revisión visual del
        /// 2026-10-07): en el nivel 1 la franja de río entre el HUD y el panel ya no es un desierto de 150 dp.
        /// </summary>
        public const float HorizonAbovePanelDp = 64f;

        /// <summary>Horizonte más bajo del panorama (dp sobre un ancho de 360).</summary>
        public const float MaxHorizonDp = 200f;

        /// <summary>Franja mínima de río visible para enseñar la garza y las vitórias-régias (dp).</summary>
        public const float RiverPropsMinBandDp = 56f;

        /// <summary>
        /// La cinta-hoja asoma 38 dp sobre el panel de victoria (guía 5.3 decía 34 con la cinta de 64 dp; revisión visual del
        /// 2026-10-07: cinta de 344 × 72 dp centrada 2 dp por encima del borde del panel).
        /// </summary>
        private const float RibbonRiseDp = 38f;

        /// <summary>Cinta-hoja de victoria (revisión visual del 2026-10-07; la guía 5.5 decía 336 × 64 dp).</summary>
        private const float RibbonWidthDp = 344f;

        /// <summary>Alto de la cinta-hoja de victoria.</summary>
        private const float RibbonHeightDp = 72f;

        /// <summary>Rayos de la victoria: radio de 260 dp y giro de 8°/s (revisión visual del 2026-10-07).</summary>
        private const float VictoryRaysRadiusDp = 260f;

        private const float VictoryRaysDegPerSec = 8f;

        /// <summary>Brillo radial de la victoria bajo la cinta: #FFE4A8 de 0,5 a 0, radio de 180 dp.</summary>
        private const float VictoryGlowRadiusDp = 180f;

        /// <summary>Atenuado de la escena en la victoria (0,30), con un hueco bajo los rayos.</summary>
        private const float VictoryVeilAlpha = 0.30f;

        private readonly ProceduralArt _art;
        private readonly DevProbe _probe;
        private readonly Action<int> _onCuia;
        private readonly Action _onBackground;
        private readonly Action _onAnywhere;
        private readonly Action _onUndo;
        private readonly Action _onRestart;
        private readonly Action _onNext;
        private readonly Action _onHome;

        private readonly Canvas _canvas;
        private readonly CanvasScaler _scaler;
        private readonly RectTransform _root;

        private readonly Image _baseFill;
        private readonly Image _skyFill;
        private readonly Image _panorama;
        private readonly Image _heron;
        private readonly Image[] _lilyPads = new Image[3];
        private readonly RectTransform _ambientFarParent;
        private readonly Image _dock;
        private readonly Image[] _foliage = new Image[4];
        private readonly Image _sceneVeil;
        private readonly Image _victoryVeil;
        private readonly Image _victoryGlow;

        private readonly RectTransform _board;
        private readonly Image _panelShadow;
        private readonly Image _panelFill;
        private readonly Image _panelInner;
        private readonly Image _panelFrame;
        private readonly Image[] _shelves = new Image[3];
        private readonly ImmersionWater[] _rowWater = new ImmersionWater[3];
        private readonly RectTransform _streamLayer;
        private readonly FxLayer _fx;

        private readonly Image _capi;
        private readonly Image _post;

        private readonly Image _scrim;
        private readonly RectTransform _hud;
        private readonly Image _badge;
        private readonly TextMeshProUGUI _levelText;
        private readonly ReliefParts _restart;
        private readonly Image _restartPill;
        private readonly Image _tray;
        private readonly ReliefParts _undo;
        private readonly RectTransform _ambientNearParent;
        private readonly AmbientLayer _ambient;
        private readonly RectTransform _pourFront;

        private readonly RectTransform _confettiLayer;
        private readonly RectTransform _skipCatcher;

        private readonly RectTransform _stuckOverlay;
        private readonly Image _stuckDim;
        private readonly RectTransform _stuckPanel;
        private readonly Image _stuckSurface;
        private readonly ReliefParts _stuckUndo;
        private readonly Button _stuckRestart;
        private readonly TextMeshProUGUI _stuckRestartLabel;

        private readonly RectTransform _victoryOverlay;
        private readonly Image _victoryDim;
        private readonly Image _victoryRays;
        private readonly RectTransform _victoryPanel;
        private readonly Image _victorySurface;
        private readonly Image _ribbon;
        private readonly ReliefParts _next;
        private readonly CanvasGroup _nextGroup;
        private readonly ReliefParts _home;
        private readonly CanvasGroup _homeGroup;

        private readonly BoardAnimator _animator;
        private readonly IntroAnimation _intro = new IntroAnimation();
        private readonly ConfettiLayer _confetti;
        private readonly Action<int> _cuiaDown;
        private readonly Action<int, bool> _cuiaUp;

        private LevelFlow? _flow;
        private int _pressPreview = -1;
        private CuiaView[] _views = Array.Empty<CuiaView>();
        private int _generatedCount;
        private RectTransform[] _rowWaterActive = Array.Empty<RectTransform>();
        private BoardState? _shownBoard;
        private BoardLayoutResult? _layout;
        private float _canvasW = ReferenceWidth;
        private float _canvasH = ReferenceHeight;
        private float _safeTop = MinSafeTop;
        private float _safeBottom = MinSafeBottom;
        private float _stuckPanelY;
        private float _victoryPanelY;
        private int _screenW;
        private int _screenH;
        private Rect _safeArea;
        private float _stuckAnim = -1f;
        private float _victoryAnim = -1f;
        private float _boardShift;
        private float _boardShiftFrom;
        private float _boardShiftTarget;
        private float _boardShiftAnim = -1f;
        private float _confettiDelay = -1f;
        private bool _restartProbePending;
        private bool _undoEnabledShown = true;
        private float _capiTime;
        private float _trayWidth = 96f;
        private float _horizonY = 118f;
        private PourTimings _timings;

        /// <summary>
        /// Construye la pantalla bajo <paramref name="host"/>. Las acciones son los puntos de entrada del input
        /// (los mismos que usan los tests PlayMode).
        /// </summary>
        public GameScreen(
            Transform host,
            ProceduralArt art,
            DevProbe probe,
            Action<int> onCuia,
            Action onBackground,
            Action onAnywhere,
            Action onUndo,
            Action onRestart,
            Action onNext,
            Action onHome)
        {
            _art = art;
            _probe = probe;
            _onCuia = onCuia;
            _onBackground = onBackground;
            _onAnywhere = onAnywhere;
            _onUndo = onUndo;
            _onRestart = onRestart;
            _onNext = onNext;
            _onHome = onHome;
            _cuiaDown = OnCuiaDown;
            _cuiaUp = OnCuiaUp;

            var canvasGo = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(host, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = false;
            // Canales que usan el shader del líquido (datos por dosis en los vértices) y TextMeshPro.
            _canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2
                | AdditionalCanvasShaderChannels.TexCoord3 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            // (Normal y Tangent son de TextMeshPro; el líquido solo usa canales UV, que el Canvas no gira.)
            _scaler = canvasGo.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.referencePixelsPerUnit = 100f;
            _root = (RectTransform)canvasGo.transform;

            // ---- Escena (capas 0-3): panorama, ambiente lejano, muelle, follaje y velo de escena. ----
            RectTransform scene = UiBuilder.Rect("Scene", _root);
            _baseFill = UiBuilder.Image("BaseFill", scene, null, Palette.RiverFar);
            UiBuilder.Stretch(_baseFill.rectTransform);
            _skyFill = UiBuilder.Image("SkyFill", scene, null, Palette.SkyTop); // cielo sobre el panorama si el horizonte baja
            _panorama = UiBuilder.Image("Panorama", scene, art.Panorama, Color.white);

            // Escena del río con el horizonte bajo: garza en la orilla izquierda y vitórias-régias junto a las orillas,
            // fuera del panel y sin nada ligado al juego (D-002). Pendientes de revisión cultural.
            _heron = UiBuilder.Image("Heron", scene, art.Heron, Color.white);
            for (int i = 0; i < _lilyPads.Length; i++)
            {
                _lilyPads[i] = UiBuilder.Image("LilyPad" + i, scene, art.LilyPad, Color.white);
            }

            _ambientFarParent = UiBuilder.Rect("AmbientFarHost", scene);
            _dock = UiBuilder.Image("Dock", scene, art.Dock, Color.white);
            _foliage[0] = UiBuilder.Image("FoliageTL", scene, art.FoliageTopLeft, Color.white);
            _foliage[1] = UiBuilder.Image("FoliageTR", scene, art.FoliageTopRight, Color.white);
            _foliage[2] = UiBuilder.Image("FoliageBL", scene, art.FoliageBottomLeft, Color.white);
            _foliage[3] = UiBuilder.Image("FoliageBR", scene, art.FoliageBottomRight, Color.white);
            for (int i = 0; i < _foliage.Length; i++)
            {
                // El follaje se balancea cada fotograma (guía 6.12): con su propio Canvas anidado, el lienzo principal no
                // se reconstruye por ello (guía 8, "Reconstrucción de Canvas").
                _foliage[i].gameObject.AddComponent<Canvas>();
            }
            _sceneVeil = UiBuilder.Image("SceneVeil", scene, null, new Color(0x6B / 255f, 0x6B / 255f, 0x6B / 255f, 0f));
            UiBuilder.Stretch(_sceneVeil.rectTransform);

            // Victoria (revisión visual del 2026-10-07): la escena se atenúa al 30 % salvo bajo los rayos (velo con un hueco
            // centrado en la cinta) y, detrás del tablero, un brillo radial cálido y 12 rayos aditivos de destello que
            // iluminan la banda del muelle en lugar de oscurecerla. Luz, no oro (guía 2.4, regla 5).
            _victoryVeil = UiBuilder.Image("VictoryVeil", scene, art.VeilHole, new Color(0f, 0f, 0f, 0f));
            _victoryGlow = UiBuilder.Image("VictoryGlow", scene, art.Glow, new Color(1f, 0xE4 / 255f, 0xA8 / 255f, 0f));
            _victoryRays = UiBuilder.Image("VictoryRays", scene, art.LightRays, new Color(1f, 0xF4 / 255f, 0xD2 / 255f, 0f));
            _victoryRays.gameObject.AddComponent<Canvas>(); // gira cada fotograma: Canvas anidado propio (guía 8)
            Material? victoryAdditive = UiMaterials.Additive;
            if (victoryAdditive != null)
            {
                _victoryGlow.material = victoryAdditive;
                _victoryRays.material = victoryAdditive;
            }

            SetVictoryLights(false);

            // C7 (02 §3.1): el toque en el fondo deselecciona al soltar, como el resto de decisiones.
            RectTransform bgCatcher = HitLayer("BackgroundCatcher", _root);
            bgCatcher.gameObject.AddComponent<PointerTapRelay>().Bind(() => _onBackground());

            // ---- Tablero (capas 4-7): se desplaza entero con ShiftToClear. ----
            _board = UiBuilder.Rect("Board", _root);
            _panelShadow = UiBuilder.Sliced("PanelShadow", _board, art.SoftShadow, new Color(1f, 1f, 1f, 0.35f));
            _panelFill = UiBuilder.Image("PanelFill", _board, art.PanelFill, Color.white);
            _panelInner = UiBuilder.Sliced("PanelInnerShadow", _board, art.PanelInnerShadow, Color.white);
            _panelFrame = UiBuilder.Sliced("PanelFrame", _board, art.PanelFrame, Color.white);
            for (int i = 0; i < _shelves.Length; i++)
            {
                _shelves[i] = UiBuilder.Sliced("Shelf" + i, _board, art.Shelf, Color.white);
                _shelves[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < _rowWater.Length; i++)
            {
                var waterGo = new GameObject("IntroWater" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(ImmersionWater));
                waterGo.transform.SetParent(_board, false);
                _rowWater[i] = waterGo.GetComponent<ImmersionWater>();
                _rowWater[i].Setup();
                RectTransform rt = _rowWater[i].rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0f);
                _rowWater[i].gameObject.SetActive(false);
            }

            _streamLayer = UiBuilder.Rect("Streams", _board);
            _fx = new FxLayer(_board, art);

            // ---- Capi y accesorio del muelle (capa 8): fuera de las zonas táctiles, nunca miran una cuia. Capi solo con
            // capi_idle.png (sin respaldo procedural; revisión visual del 2026-10-07 y guía 10.2 F7). ----
            RectTransform decor = UiBuilder.Rect("Decor", _root);
            _capi = UiBuilder.Image("Capi", decor, art.Capi, Color.white);
            _capi.preserveAspect = true;
            _capi.gameObject.AddComponent<Canvas>(); // respira: Canvas anidado propio (guía 8)
            _capi.gameObject.SetActive(false);
            _post = UiBuilder.Image("RopePost", decor, art.RopePost, Color.white);

            // ---- HUD (capa 9): velo de estado, insignia, Reiniciar y bandeja con Deshacer · Marea. ----
            _hud = UiBuilder.Rect("Hud", _root);
            _scrim = UiBuilder.Image("StatusScrim", _hud, art.StatusScrim, Color.white);
            _badge = UiBuilder.Image("LevelBadge", _hud, art.BadgePlate, Color.white);
            _levelText = UiBuilder.Label("LevelTitle", _badge.transform, TextStyle.Badge, UiText.Level(1), 26f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.AutoSize(_levelText, 20f, 26f); // guía 3.5: 26 dp, mínimo 20, nunca se recorta

            // Volver a la orilla: disco de 40 dp (familia Madera) con píldora «Reiniciar» en un área de 60 × 48 (guía 3.5).
            _restart = UiBuilder.Relief("RestartButton", _hud, art.DiscWood, () => _onRestart());
            RectTransform restartVisual = (RectTransform)_restart.Face.parent;
            restartVisual.anchorMin = new Vector2(0.5f, 1f);
            restartVisual.anchorMax = new Vector2(0.5f, 1f);
            restartVisual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            restartVisual.anchoredPosition = Vector2.zero;
            Image restartIcon = UiBuilder.Image("Icon", _restart.Face, art.IconRestart, Color.white);
            UiBuilder.Place(restartIcon.rectTransform, 7f, 7f, 26f, 26f);
            _restartPill = UiBuilder.Image("Pill", _restart.Button.transform, art.Pill, Color.white);
            TextMeshProUGUI restartLabel = UiBuilder.Label("Label", _restartPill.transform, TextStyle.PillWood, UiText.Restart, 12f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(restartLabel.rectTransform);
            restartLabel.rectTransform.offsetMin = new Vector2(0f, 1f);
            _restart.Relief.Bind(_restart.Face, _restart.Base, _restart.FaceImage, art.DiscWood, restartIcon, null, null);

            _tray = UiBuilder.Image("Tray", _hud, art.Tray(_trayWidth), Color.white);
            _undo = UiBuilder.Relief("UndoButton", _hud, art.SlotWater, () => _onUndo());
            Image undoIcon = UiBuilder.Image("Icon", _undo.Face, art.IconUndo, Color.white);
            UiBuilder.Place(undoIcon.rectTransform, 16f, 6f, 32f, 32f);
            TextMeshProUGUI undoLabel = UiBuilder.Label("Label", _undo.Face, TextStyle.SlotWater, UiText.Undo, 12f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Place(undoLabel.rectTransform, 0f, 41f, 64f, 16f); // franja del 65-87 % de la cara (guía 3.6)
            _undo.Relief.Bind(_undo.Face, _undo.Base, _undo.FaceImage, art.SlotWater, undoIcon, art.IconUndoDisabled, undoLabel);

            _ambientNearParent = UiBuilder.Rect("AmbientNearHost", _root);
            _ambient = new AmbientLayer(_ambientFarParent, _ambientNearParent, art);
            _ambient.SetFoliage(_foliage[0].rectTransform, _foliage[1].rectTransform, _foliage[2].rectTransform, _foliage[3].rectTransform);

            // ---- Capa de vertido (D-006): mientras vierte, el cuerpo del origen y su chorro se dibujan aquí, delante de las
            // vecinas, del marco del panel, del HUD y del polen, como Magic Sort (t = 250: el origen tapa el rótulo del
            // nivel). Sin Graphic ni raycast; alineada con el tablero (sigue a ShiftToClear) y vacía en reposo. ----
            _pourFront = UiBuilder.Rect("PourFront", _root);

            _confettiLayer = UiBuilder.Rect("Burst", _root);
            _confetti = new ConfettiLayer(_confettiLayer, art, GameConfig.AnimConfettiMaxParticles);

            // ---- Aguas estancadas (02 §7; 07 §7.5; guía 5.3): tablero atenuado detrás; panel de madera clara abajo. ----
            _stuckOverlay = UiBuilder.Rect("StuckOverlay", _root);
            _stuckDim = UiBuilder.Image("Dim", _stuckOverlay, null, new Color(0f, 0f, 0f, 0f), true);
            UiBuilder.Stretch(_stuckDim.rectTransform);
            _stuckPanel = UiBuilder.Rect("Panel", _stuckOverlay);
            _stuckSurface = UiBuilder.ModalBody(_stuckPanel, art);
            Image header = UiBuilder.Image("HeaderPlate", _stuckPanel, art.HeaderPlate, Color.white);
            UiBuilder.Place(header.rectTransform, 16f, 16f, 296f, 40f);
            TextMeshProUGUI stuckTitle = UiBuilder.Label("Title", header.transform, TextStyle.ModalTitle, UiText.StuckTitle, 22f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(stuckTitle.rectTransform);
            UiBuilder.AutoSize(stuckTitle, 18f, 22f);
            // Subtítulo en madera_base sobre panel_claro (5,1:1). Nunca alfa en textos.
            TextMeshProUGUI stuckSubtitle = UiBuilder.Label("Subtitle", _stuckPanel, TextStyle.PlainBold, UiText.StuckSubtitle, 14f, Palette.FamilyWood.Base, TextAlignmentOptions.Center);
            UiBuilder.Place(stuckSubtitle.rectTransform, 16f, 60f, 296f, 20f);
            Image still = UiBuilder.Image("StillWater", _stuckPanel, art.StillWater, Color.white);
            UiBuilder.Place(still.rectTransform, 16f, 86f, 296f, 20f);
            _stuckUndo = UiBuilder.Relief("UndoModalButton", _stuckPanel, art.CtaWater, () => _onUndo());
            TextMeshProUGUI stuckUndoLabel = UiBuilder.Label("Label", _stuckUndo.Face, TextStyle.CtaWater, UiText.StuckUndo, 22f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(stuckUndoLabel.rectTransform, art.CtaWater);
            UiBuilder.AutoSize(stuckUndoLabel, 18f, 22f);
            _stuckUndo.Relief.Bind(_stuckUndo.Face, _stuckUndo.Base, _stuckUndo.FaceImage, art.CtaWater, null, null, stuckUndoLabel);
            _stuckRestart = UiBuilder.BareButton("RestartModalButton", _stuckPanel, () => _onRestart());
            _stuckRestartLabel = UiBuilder.Label("Label", _stuckRestart.transform, TextStyle.PlainBold, UiText.StuckRestart, 16f, Palette.FamilyWood.Base, TextAlignmentOptions.Center);
            _stuckRestartLabel.fontStyle = FontStyles.Underline;
            UiBuilder.Stretch(_stuckRestartLabel.rectTransform);
            _stuckOverlay.gameObject.SetActive(false);

            // ---- Victoria (02 §6; 07 §5.4; guía 5.3, 5.5 y 6.9): panel abajo, cinta-hoja y Siguiente; sin auto-avance. ----
            _victoryOverlay = UiBuilder.Rect("VictoryOverlay", _root);
            _victoryDim = UiBuilder.Image("Dim", _victoryOverlay, null, new Color(0f, 0f, 0f, 0f), true);
            UiBuilder.Stretch(_victoryDim.rectTransform);

            _victoryPanel = UiBuilder.Rect("Panel", _victoryOverlay);
            _victorySurface = UiBuilder.ModalBody(_victoryPanel, art);
            _next = UiBuilder.Relief("NextButton", _victoryPanel, art.CtaLeaf, () => _onNext());
            _nextGroup = _next.Face.parent.gameObject.AddComponent<CanvasGroup>();
            _nextGroup.blocksRaycasts = false;
            TextMeshProUGUI nextLabel = UiBuilder.Label("Label", _next.Face, TextStyle.CtaLeaf, UiText.Next, 22f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(nextLabel.rectTransform, art.CtaLeaf);
            _next.Relief.Bind(_next.Face, _next.Base, _next.FaceImage, art.CtaLeaf, null, null, nextLabel);

            // «Inicio» [P] (06 §4.7; D-008 punto 3): secundario de madera (07 §7.3) bajo Siguiente; vuelve a la pantalla de inicio.
            _home = UiBuilder.Relief("HomeButton", _victoryPanel, art.CtaWood, () => _onHome());
            _homeGroup = _home.Face.parent.gameObject.AddComponent<CanvasGroup>();
            _homeGroup.blocksRaycasts = false;
            TextMeshProUGUI homeLabel = UiBuilder.Label("Label", _home.Face, TextStyle.PillWood, UiText.Home, 18f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(homeLabel.rectTransform, art.CtaWood);
            UiBuilder.AutoSize(homeLabel, 14f, 18f);
            _home.Relief.Bind(_home.Face, _home.Base, _home.FaceImage, art.CtaWood, null, null, homeLabel);
            _ribbon = UiBuilder.Image("Ribbon", _victoryPanel, art.Ribbon, Color.white);
            // Baloo 2 de 30 dp, mínimo 22 (revisión visual del 2026-10-07; guía 5.5), en la franja de texto de la cinta de
            // 344 × 72 (≈ 262 × 45 dp de hoja_base → hoja_fondo).
            TextMeshProUGUI ribbonLabel = UiBuilder.Label("Title", _ribbon.transform, TextStyle.Ribbon, UiText.LevelComplete, 30f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Place(ribbonLabel.rectTransform, 44f, 15f, RibbonWidthDp - 88f, 42f);
            UiBuilder.AutoSize(ribbonLabel, 22f, 30f);
            _victoryOverlay.gameObject.SetActive(false);

            // Captura de toques para saltar Sumergir y teñir o la celebración (02 §4.2, §2): por encima de todo.
            _skipCatcher = HitLayer("SkipCatcher", _root);
            _skipCatcher.gameObject.AddComponent<PointerDownRelay>().Bind(() => _onAnywhere());
            _skipCatcher.gameObject.SetActive(false);

            _animator = new BoardAnimator(_streamLayer, _pourFront) { Fx = _fx };
            ApplyScreenMetrics(true);
        }

        /// <summary>
        /// Pantalla simulada en píxeles (ancho, alto, inserto superior e inferior de la zona segura) para las capturas
        /// visuales de prueba en el Editor; null = <see cref="Screen"/> real. No se usa en el juego.
        /// </summary>
        public static Vector4? SimulatedScreen { get; set; }

        /// <summary>La cola visual está vacía (02 §3.2).</summary>
        public bool IsViewIdle => _animator.IsIdle;

        /// <summary>Cola visual de vertidos (diagnóstico y tests: saltos y encadenados).</summary>
        public BoardAnimator Animator => _animator;

        /// <summary>Duraciones del vertido del nivel cargado (claves <c>anim_*</c> de 08 §9 por el factor de velocidad).</summary>
        public PourTimings Timings => _timings;

        /// <summary>Lienzo de la pantalla (capturas de prueba).</summary>
        public Canvas Canvas => _canvas;

        /// <summary>Ancho del lienzo en dp (360 en el teléfono; el de la pantalla simulada en los tests).</summary>
        public float CanvasWidth => _canvasW;

        /// <summary>Capa de vertido (D-006): el origen y su chorro mientras vierte, delante del HUD (tests).</summary>
        public RectTransform PourFront => _pourFront;

        /// <summary>Contenedor del tablero, en cuyas coordenadas viven las ranuras y la cola de animación (tests).</summary>
        public RectTransform BoardContainer => _board;

        /// <summary>Capa del HUD (tests de orden de capas).</summary>
        public RectTransform Hud => _hud;

        /// <summary>Capa del ambiente cercano (polen), delante del HUD (tests de orden de capas).</summary>
        public RectTransform AmbientNear => _ambientNearParent;

        /// <summary>Vistas de las cuias del nivel actual.</summary>
        public CuiaView[] Views => _views;

        /// <summary>Layout del tablero del nivel actual (null antes del primer nivel).</summary>
        public BoardLayoutResult? Layout => _layout;

        /// <summary>
        /// Grosor del marco del panel tal como se dibuja (dp), tras adelgazarlo con el 9-slice si las cuias quedarían a
        /// menos de 8 dp de él (guía 3.2). El canto interior del marco está en <c>Layout.PanelY + PanelFrameDp</c>.
        /// </summary>
        public float PanelFrameDp { get; private set; }

        /// <summary>Capa de efectos del tablero (presupuesto de partículas).</summary>
        public FxLayer Fx => _fx;

        /// <summary>Ambiente de la escena (presupuesto de partículas).</summary>
        public AmbientLayer Ambient => _ambient;

        /// <summary>Estallido de la victoria (presupuesto de partículas, VA-13).</summary>
        public ConfettiLayer Burst => _confetti;

        /// <summary>Capi está visible en el muelle (guía 3.7: solo si la banda libre es ≥ 64 dp).</summary>
        public bool CapiVisible => _capi.gameObject.activeSelf;

        /// <summary>Rectángulos decorativos del muelle (Capi y el poste), para comprobar que no tocan zonas táctiles (VA-08).</summary>
        public RectTransform[] DecorRects => new[] { _capi.rectTransform, _post.rectTransform };

        /// <summary>Botón de Deshacer de la bandeja.</summary>
        public Button UndoButton => _undo.Button;

        /// <summary>Botón de Reiniciar del HUD.</summary>
        public Button RestartButton => _restart.Button;

        /// <summary>Botón Siguiente de la victoria.</summary>
        public Button NextButton => _next.Button;

        /// <summary>Botón «Inicio» de la victoria [P].</summary>
        public Button HomeButton => _home.Button;

        /// <summary>La pantalla de nivel está visible (oculta mientras se ve la de inicio).</summary>
        public bool Visible => _canvas.gameObject.activeSelf;

        /// <summary>
        /// Muestra u oculta la pantalla de nivel (tarea B1-6: la pantalla de inicio la sustituye). Oculta no recibe toques ni
        /// se dibuja; el estado del nivel se conserva hasta la siguiente carga.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_canvas.gameObject.activeSelf != visible)
            {
                _canvas.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Horizonte del panorama (dp desde arriba) para un panel que empieza en <paramref name="panelTop"/> en un lienzo de
        /// <paramref name="canvasWidth"/> dp: clamp(panel.top − 64, 118, 200), a escala del ancho (revisión visual del
        /// 2026-10-07). A 360 × 780 dp, con las cuias de 5,60 u (D-006): nivel 7 (panel en 162,1) → 118; nivel 1 (panel en
        /// 226,9) → 162,9.
        /// </summary>
        public static float HorizonFor(float panelTop, float canvasWidth)
        {
            float k = canvasWidth / 360f;
            return Mathf.Clamp(panelTop - HorizonAbovePanelDp, ProceduralArt.PanoramaHorizonDp * k, MaxHorizonDp * k);
        }

        /// <summary>Horizonte del panorama en el nivel cargado (dp desde arriba).</summary>
        public float HorizonY => _horizonY;

        /// <summary>Asocia el flujo (antes de cargar el primer nivel).</summary>
        public void Bind(LevelFlow flow)
        {
            _flow = flow;
        }

        /// <summary>Avanza la vista <paramref name="dtMs"/> milisegundos.</summary>
        public void Tick(float dtMs)
        {
            ReadScreen(out int sw, out int sh, out Rect safe);
            if (sw != _screenW || sh != _screenH || safe != _safeArea)
            {
                ApplyScreenMetrics(false);
            }

            if (_intro.Running)
            {
                _intro.Tick(dtMs);
            }

            _animator.Tick(dtMs);
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].Tick(dtMs);
            }

            if (_confettiDelay >= 0f)
            {
                _confettiDelay -= dtMs;
                if (_confettiDelay < 0f)
                {
                    _confettiDelay = -1f;
                    // Tras el estallido, ≤ 12 hojas y pétalos caen despacio hasta el CTA de la victoria.
                    _confetti.Play(GameConfig.AnimConfettiMaxParticles, PanelRect(), _canvasW, _victoryPanelY + VictoryCtaTopDp);
                }
            }

            _confetti.Tick(dtMs);
            _fx.Tick(dtMs);
            _ambient.Tick(dtMs);
            TickCapi(dtMs);
            TickBoardShift(dtMs);
            TickPanels(dtMs);
            RefreshHud();
        }

        /// <summary>Vuelta de segundo plano (02 §14): la vista salta al modelo sin animar.</summary>
        public void SnapToModel()
        {
            _animator.SnapAll();
            _intro.Complete();
        }

        /// <inheritdoc />
        public void OnLevelLoaded(int level)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            _intro.Complete();
            _confetti.Stop();
            _fx.Clear();
            _confettiDelay = -1f;
            HideOverlays();
            BuildBoard(flow);
            _levelText.text = UiText.Level(level);
            _pressPreview = -1;
            _shownBoard = flow.Session.Current;
            _probe.State("load", flow, int.MinValue, null);
            EmitLevelLayout();
        }

        /// <inheritdoc />
        public void OnIntroStarted(int durationMs, bool isShort)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            ClearLifts();
            HideOverlays();
            _shownBoard = flow.Session.Current;
            _intro.Start(_views, flow.Session.Current, _art.Water, _rowWaterActive, durationMs, _fx);
            _skipCatcher.gameObject.SetActive(true);
            if (_restartProbePending)
            {
                _restartProbePending = false;
                _probe.State("restart", flow, int.MinValue, null);
            }
        }

        /// <inheritdoc />
        public void OnIntroFinished(bool skipped)
        {
            _intro.Complete();
            _skipCatcher.gameObject.SetActive(false);
            _probe.State("ready", RequireFlow(), int.MinValue, null);
        }

        /// <inheritdoc />
        public void OnTap(TapResult result)
        {
            LevelFlow flow = RequireFlow();
            switch (result.Kind)
            {
                case TapKind.Selected:
                    SetLift(result.PreviousSelection, false);
                    SetLift(result.Selection, true); // solo el origen sube: ningún destino se resalta (D-002; 02 §3.1 C1)
                    _probe.State("select", flow, result.Tapped, null);
                    break;

                case TapKind.Deselected:
                    SetLift(result.PreviousSelection, false);
                    _probe.State("deselect", flow, result.Tapped, null);
                    break;

                case TapKind.Rejected:
                    SetLift(result.PreviousSelection, false);
                    if ((uint)result.Tapped < (uint)_views.Length)
                    {
                        _views[result.Tapped].Shake();
                    }

                    _probe.State("reject", flow, result.Tapped, null);
                    break;

                case TapKind.Poured:
                    SetLift(result.PreviousSelection, false);
                    EnqueuePour(flow, result.Pour.Pour);
                    _probe.State("pour", flow, result.Tapped, result.Pour.Pour);
                    break;
            }
        }

        /// <inheritdoc />
        public void OnUndo(Pour undone, bool fromStuckModal)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            ClearLifts();
            HideOverlays(true);
            BoardState before = _shownBoard ?? flow.Session.Current;
            BoardState after = flow.Session.Current;
            _animator.EnqueueUndo(
                undone.Target,
                undone.Source,
                undone.Amount,
                before[undone.Target].TopColor,
                before[undone.Target],
                after[undone.Target],
                after.IsSealed(undone.Target),
                before[undone.Source],
                after[undone.Source],
                after.IsSealed(undone.Source));
            _shownBoard = after;
            _probe.State("undo", flow, int.MinValue, undone);
        }

        /// <inheritdoc />
        public void OnRestart(bool fromStuckModal)
        {
            _animator.SnapAll();
            ClearLifts();
            HideOverlays();
            _shownBoard = RequireFlow().Session.Current;
            _restartProbePending = true;
        }

        /// <inheritdoc />
        public void OnWin()
        {
            LevelFlow flow = RequireFlow();
            ClearLifts();
            _probe.State("win", flow, int.MinValue, null);
        }

        /// <inheritdoc />
        public void OnStuck()
        {
            LevelFlow flow = RequireFlow();
            ClearLifts();
            _probe.State("stuck", flow, int.MinValue, null);
        }

        /// <inheritdoc />
        public void OnCelebrationStarted()
        {
            // 07 §5.4; guía 6.9: ola de saltos (escalón de 40 ms) y estallido a los 0,5 s. El ambiente se suspende para
            // que el estallido disponga de las 60 partículas.
            _animator.SnapAll();
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].PlayCelebration(300f + (i * 40f));
            }

            _ambient.SetSuspended(true);
            _confettiDelay = 500f;
            _skipCatcher.gameObject.SetActive(true);
        }

        /// <inheritdoc />
        public void OnVictoryScreen()
        {
            _skipCatcher.gameObject.SetActive(false);
            _ambient.SetSuspended(true);
            _victoryOverlay.gameObject.SetActive(true);
            SetVictoryLights(true);
            ShiftBoard(ShiftToClear(_victoryPanelY - RibbonRiseDp, 8f), true);
            _victoryAnim = 0f;
            ApplyVictoryAnim(0f);
        }

        /// <inheritdoc />
        public void OnStuckModalOpened()
        {
            _animator.SnapAll();
            _ambient.SetSuspended(true);
            _stuckOverlay.gameObject.SetActive(true);
            ShiftBoard(ShiftToClear(_stuckPanelY, 12f), true); // 07 §5.5: las cuias siguen visibles sobre el modal
            _stuckAnim = 0f;
            ApplyStuckAnim(0f);
        }

        // 02 §3.1: la elevación empieza en el touch-down (0 ms percibidos) si, sin selección, la cuia puede ser origen
        // (lo decide el núcleo); el toque se decide en el touch-up.
        private void OnCuiaDown(int index)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.AcceptsBoardInput || flow.Selected != TapController.NoSelection || _pressPreview >= 0)
            {
                return;
            }

            if ((uint)index < (uint)_views.Length && PourRules.CanBeSource(flow.Session.Current, index))
            {
                _pressPreview = index;
                _views[index].SetLifted(true);
            }
        }

        // Soltar dentro del área táctil es un toque (C1-C6); soltar fuera cancela (C10, el arrastre, queda fuera de v0).
        private void OnCuiaUp(int index, bool inside)
        {
            if (inside)
            {
                _onCuia(index);
            }

            if (_pressPreview == index)
            {
                _pressPreview = -1;
                if ((uint)index < (uint)_views.Length && (_flow == null || _flow.Selected != index))
                {
                    _views[index].SetLifted(false);
                }
            }
        }

        private LevelFlow RequireFlow()
        {
            return _flow ?? throw new InvalidOperationException("GameScreen sin LevelFlow: llama a Bind antes de cargar.");
        }

        private void BuildBoard(LevelFlow flow)
        {
            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i] != null)
                {
                    UnityEngine.Object.Destroy(_views[i].gameObject);
                }
            }

            BoardState board = flow.Session.Current;
            var views = new CuiaView[board.Count];
            float liftMs = GameConfig.AnimLiftMs * GameConfig.SpeedFactor(flow.LevelNumber);
            for (int i = 0; i < board.Count; i++)
            {
                views[i] = CuiaView.Create(_board, i, board[i].Capacity, _art, _cuiaDown, _cuiaUp, _fx);
                views[i].SetLiftDuration(liftMs);
                views[i].ShowContent(board[i], board.IsSealed(i));
            }

            _views = views;
            _generatedCount = views.Length; // cuias generadas al cargar el nivel: fijan las filas (D-006); en v0, todas
            for (int i = 0; i < _rowWater.Length; i++)
            {
                _rowWater[i].rectTransform.SetAsLastSibling();
            }

            _streamLayer.SetAsLastSibling();
            _fx.Root.SetAsLastSibling();
            float f = GameConfig.SpeedFactor(flow.LevelNumber);
            _timings = new PourTimings(
                GameConfig.AnimLiftMs * f,
                GameConfig.AnimTravelMs * f,
                GameConfig.AnimPourDoseMs * f,
                GameConfig.AnimPourTailMs * f,
                GameConfig.AnimReturnMs * f);
            _animator.Bind(_views, _timings, GameConfig.QueueMaxLagPours);
            LayoutBoard();
        }

        private void EnqueuePour(LevelFlow flow, Pour pour)
        {
            BoardState after = flow.Session.Current;
            BoardState before = _shownBoard ?? after;
            _animator.EnqueuePour(
                pour.Source,
                pour.Target,
                pour.Amount,
                before[pour.Source].TopColor,
                before[pour.Source],
                after[pour.Source],
                after.IsSealed(pour.Source),
                before[pour.Target],
                after[pour.Target],
                after.IsSealed(pour.Target));
            _shownBoard = after;
        }

        private void SetLift(int index, bool lifted)
        {
            if ((uint)index < (uint)_views.Length)
            {
                _views[index].SetLifted(lifted);
            }
        }

        private void ClearLifts()
        {
            _pressPreview = -1;
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].SetLifted(false);
            }
        }

        // animateBoard: el tablero vuelve a su sitio con la misma curva que usó para subir (Marea desde el modal); si no,
        // vuelve al instante (carga, intro y reinicio, que recolocan la vista de todos modos).
        private void HideOverlays(bool animateBoard = false)
        {
            ShiftBoard(0f, animateBoard);
            _stuckOverlay.gameObject.SetActive(false);
            _victoryOverlay.gameObject.SetActive(false);
            _sceneVeil.color = new Color(_sceneVeil.color.r, _sceneVeil.color.g, _sceneVeil.color.b, 0f);
            SetVictoryLights(false);
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].SetVictoryAura(0f);
            }

            _stuckAnim = -1f;
            _victoryAnim = -1f;
            _ambient.SetSuspended(false);
        }

        private void RefreshHud()
        {
            bool undo = _flow != null && _flow.CanUndo;
            if (undo != _undoEnabledShown)
            {
                _undoEnabledShown = undo;
                UiBuilder.SetEnabled(_undo.Button, undo);
                UiBuilder.SetEnabled(_stuckUndo.Button, undo);
            }
        }

        private void TickPanels(float dtMs)
        {
            if (_stuckAnim >= 0f)
            {
                _stuckAnim += dtMs;
                ApplyStuckAnim(_stuckAnim);
                if (_stuckAnim >= PanelSlideMs)
                {
                    _stuckAnim = -1f;
                    _probe.Layout("stuck", RequireFlow().LevelNumber, null, new[] { "undo_modal", "restart_modal" }, new[] { (RectTransform)_stuckUndo.Button.transform, (RectTransform)_stuckRestart.transform });
                }
            }

            if (_victoryAnim >= 0f)
            {
                float before = _victoryAnim;
                _victoryAnim += dtMs;
                ApplyVictoryAnim(_victoryAnim);
                if (before < PanelSlideMs && _victoryAnim >= PanelSlideMs)
                {
                    _probe.Layout("victory", RequireFlow().LevelNumber, null, new[] { "next", "home" }, new[] { (RectTransform)_next.Button.transform, (RectTransform)_home.Button.transform });
                }
            }
        }

        private void ApplyStuckAnim(float ms)
        {
            // 07 §5.5; guía 6.10: el tablero queda visible y atenuado al 70 %; la escena pierde saturación (velo gris al
            // 20 %); el modal sube desde abajo en 0,25 s.
            float fade = Mathf.Clamp01(ms / OverlayFadeMs);
            _stuckDim.color = new Color(0.04f, 0.06f, 0.05f, 0.30f * fade);
            _sceneVeil.color = new Color(0x6B / 255f, 0x6B / 255f, 0x6B / 255f, 0.20f * fade);
            float slide = 1f - Ease.OutCubic(ms / PanelSlideMs);
            _stuckPanel.anchoredPosition = new Vector2(_stuckPanel.anchoredPosition.x, -(_stuckPanelY + (slide * (StuckPanelDp + 40f))));
        }

        private void ApplyVictoryAnim(float ms)
        {
            // Atenuado suave: las cuias selladas son la recompensa del nivel y siguen a la vista (02 §6; 07 §5.4). Guía 6.9:
            // el panel sube en 250 ms (easeOutCubic); la cinta pasa de 0,6 a 1,08 y a 1,0 en 300 ms (easeOutBack) a los
            // 100 ms; Siguiente aparece a los 600 ms (0,9 → 1,0 en 150 ms) y respira (1,00 ↔ 1,03, 1,4 s).
            // Revisiones visuales del 2026-10-07: la escena (no el tablero, que es la recompensa) se atenúa al 30 % salvo
            // bajo los rayos; detrás del tablero, centrados en la cinta, un brillo radial (#FFE4A8, 0,5 → 0, radio de
            // 180 dp) y 12 rayos aditivos de destello (#FFF4D2 al 35 %, radio de 260 dp, 8°/s) que entran con la cinta; y
            // un brillo cálido de 0,25 detrás de cada cuia sellada.
            float fade = Mathf.Clamp01(ms / OverlayFadeMs);
            _victoryDim.color = new Color(0f, 0f, 0f, 0f);
            float slide = 1f - Ease.OutCubic(ms / PanelSlideMs);
            float panelTop = _victoryPanelY + (slide * (VictoryPanelDp + RibbonRiseDp + 40f));
            _victoryPanel.anchoredPosition = new Vector2(_victoryPanel.anchoredPosition.x, -panelTop);
            float rt = Mathf.Clamp01((ms - 100f) / 300f);
            float ribbon = ms < 100f ? 0.6f : Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(rt, 2.3f));
            _ribbon.rectTransform.localScale = Vector3.one * ribbon;
            var ribbonCenter = new Vector2(_canvasW * 0.5f, -(panelTop - RibbonRiseDp + (RibbonHeightDp * 0.5f)));
            _victoryVeil.color = new Color(0f, 0f, 0f, VictoryVeilAlpha * fade);
            _victoryVeil.rectTransform.anchoredPosition = ribbonCenter;
            _victoryGlow.color = new Color(1f, 0xE4 / 255f, 0xA8 / 255f, 0.5f * rt);
            _victoryGlow.rectTransform.anchoredPosition = ribbonCenter;
            _victoryRays.color = new Color(1f, 0xF4 / 255f, 0xD2 / 255f, 0.35f * rt);
            _victoryRays.rectTransform.anchoredPosition = ribbonCenter;
            _victoryRays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -VictoryRaysDegPerSec * (ms / 1000f));
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].SetVictoryAura(fade);
            }
            float nt = Mathf.Clamp01((ms - 600f) / 150f);
            _nextGroup.alpha = nt;
            _homeGroup.alpha = nt;
            float breathe = ms > 750f ? 1f + (0.015f * (1f - Mathf.Cos(((ms - 750f) / 1400f) * Mathf.PI * 2f))) : 1f;
            ((RectTransform)_next.Face.parent).localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, Ease.Out(nt)) * breathe;
        }

        private void EmitLevelLayout()
        {
            _probe.Layout("level", RequireFlow().LevelNumber, _views, new[] { "undo", "restart" }, new[] { (RectTransform)_undo.Button.transform, (RectTransform)_restart.Button.transform });
        }

        private void ApplyScreenMetrics(bool initial)
        {
            ReadScreen(out _screenW, out _screenH, out _safeArea);

            // Lienzo de al menos 360 × 780 dp (02 §5): 360 de ancho en 19,5:9 y 20:9 (360 × 800).
            float scale = Mathf.Min(_screenW / ReferenceWidth, _screenH / ReferenceHeight);
            scale = Mathf.Max(0.01f, scale);
            _scaler.scaleFactor = scale;
            _art.ScreenScale = scale;
            _canvasW = _screenW / scale;
            _canvasH = _screenH / scale;
            float insetTop = (_screenH - _safeArea.yMax) / scale;
            float insetBottom = _safeArea.yMin / scale;
            _safeTop = Mathf.Max(MinSafeTop, insetTop);
            _safeBottom = Mathf.Max(MinSafeBottom, insetBottom);

            if (!initial)
            {
                _animator.SnapAll(); // las poses de vertido en curso se calcularon con el layout anterior
            }

            LayoutScene();
            LayoutHud();
            LayoutBoard();

            if (!initial && _flow != null && _flow.HasLevel)
            {
                EmitLevelLayout();
            }
        }

        // Posiciones de 02 §5 y 07 §7.4 (dp desde arriba): fila superior en safe + 8 (alto 48); bandeja de 80 dp que
        // termina 16 dp por encima de la zona segura inferior; 40 dp entre tablero y bandeja (guía 3.2).
        private float TopRowY => _safeTop + 8f;

        private float BoardTop => TopRowY + 48f;

        private float TrayBottom => _canvasH - _safeBottom - 16f;

        private float TrayTop => TrayBottom - 80f;

        private float BoardBottom => TrayTop - 40f;

        private void LayoutScene()
        {
            float w = _canvasW;
            float h = _canvasH;
            PlaceAt(_foliage[0].rectTransform, -20f, 0f, 160f, 160f, new Vector2(0f, 1f));
            PlaceAt(_foliage[1].rectTransform, w + 20f, 0f, 160f, 160f, new Vector2(1f, 1f));
            float bottom = ProceduralArt.BottomFoliageDp; // 170 dp (revisión visual del 2026-10-07; antes 140)
            PlaceAt(_foliage[2].rectTransform, 0f, h, bottom, bottom, new Vector2(0f, 0f));
            PlaceAt(_foliage[3].rectTransform, w, h, bottom, bottom, new Vector2(1f, 0f));
            UiBuilder.Place(_scrim.rectTransform, 0f, 0f, w, _safeTop + 40f);
            LayoutBackdrop(0f, 0f, false);
        }

        private void LayoutHud()
        {
            float w = _canvasW;

            // Insignia en la zona centro-izquierda (x 56-192, guía 3.5): placa de 120 × 44 y hojas; texto en 106 dp.
            UiBuilder.Place(_badge.rectTransform, 56f, TopRowY - 2f, 136f, 52f);
            UiBuilder.Place(_levelText.rectTransform, 15f, 3f, 106f, 42f);

            // Reiniciar a la derecha: área de 60 × 48 en x = W − 8 − 60; disco de 40 y píldora de 60 × 16 al pie.
            UiBuilder.Place((RectTransform)_restart.Button.transform, w - 8f - 60f, TopRowY, 60f, 48f);
            UiBuilder.Place(_restartPill.rectTransform, 0f, 32f, 60f, 16f);

            // Bandeja ajustada al contenido (guía 3.6): 16 + n·64 + (n − 1)·8 + 16 con n = 1 en v0 → 96 dp.
            _trayWidth = 96f;
            _tray.sprite = _art.Tray(_trayWidth);
            float trayX = (w - _trayWidth) / 2f;
            if (_art.TrayIsPainted)
            {
                _tray.type = Image.Type.Sliced; // PNG de ui_bandeja: 9-slice horizontal a su alto nativo
                UiBuilder.Place(_tray.rectTransform, trayX, TrayTop, _trayWidth, 80f);
            }
            else
            {
                UiBuilder.Place(_tray.rectTransform, trayX - 8f, TrayTop - 2f, _trayWidth + 16f, 94f);
            }

            UiBuilder.Place((RectTransform)_undo.Button.transform, trayX + 16f, TrayTop + 4f, 64f, 72f);

            // Paneles anclados abajo (07 §5.4, §5.5, §7.5), con los CTA en la zona del pulgar (02 §3.3).
            float panelW = Mathf.Min(328f, w - 32f);
            _stuckPanelY = _canvasH - _safeBottom - 8f - StuckPanelDp;
            UiBuilder.Place(_stuckPanel, (w - panelW) / 2f, _stuckPanelY, panelW, StuckPanelDp);
            float ctaW = Mathf.Min(panelW - 32f, 296f);
            UiBuilder.Place((RectTransform)_stuckUndo.Button.transform, (panelW - ctaW) / 2f, 114f, ctaW, 64f);
            UiBuilder.Place((RectTransform)_stuckRestart.transform, (panelW - 280f) / 2f, StuckPanelDp - 4f - 48f, 280f, 48f); // enlace al pie (07 §7.5)

            _victoryPanelY = _canvasH - _safeBottom - 8f - VictoryPanelDp;
            UiBuilder.Place(_victoryPanel, (w - panelW) / 2f, _victoryPanelY, panelW, VictoryPanelDp);
            PlaceAt(_ribbon.rectTransform, panelW / 2f, -RibbonRiseDp + (RibbonHeightDp * 0.5f), RibbonWidthDp, RibbonHeightDp, new Vector2(0.5f, 0.5f));
            float nextW = Mathf.Min(w - 48f, Mathf.Min(312f, panelW - 16f));
            UiBuilder.Place((RectTransform)_next.Button.transform, (panelW - nextW) / 2f, VictoryCtaTopDp, nextW, 64f);
            float homeW = Mathf.Min(VictoryHomeWidthDp, nextW);
            UiBuilder.Place((RectTransform)_home.Button.transform, (panelW - homeW) / 2f, VictoryHomeTopDp, homeW, 48f);
            float ribbonY = _victoryPanelY - RibbonRiseDp + (RibbonHeightDp * 0.5f);
            PlaceAt(_victoryRays.rectTransform, w * 0.5f, ribbonY, 2f * VictoryRaysRadiusDp, 2f * VictoryRaysRadiusDp, new Vector2(0.5f, 0.5f));
            PlaceAt(_victoryGlow.rectTransform, w * 0.5f, ribbonY, 2f * VictoryGlowRadiusDp, 2f * VictoryGlowRadiusDp, new Vector2(0.5f, 0.5f));
            PlaceAt(_victoryVeil.rectTransform, w * 0.5f, ribbonY, 2f * ProceduralArt.VeilHoleHalfDp, 2f * ProceduralArt.VeilHoleHalfDp, new Vector2(0.5f, 0.5f));
            _stuckSurface.sprite = _art.ModalSurfaceFor(panelW - 16f, StuckPanelDp - 16f);
            _victorySurface.sprite = _art.ModalSurfaceFor(panelW - 16f, VictoryPanelDp - 16f);
        }

        private void LayoutBoard()
        {
            float w = _canvasW;
            for (int i = 0; i < _shelves.Length; i++)
            {
                _shelves[i].gameObject.SetActive(false);
            }

            if (_views.Length == 0)
            {
                _panelShadow.enabled = false;
                _panelFill.enabled = false;
                _panelInner.enabled = false;
                _panelFrame.enabled = false;
                _capi.gameObject.SetActive(false);
                _post.gameObject.SetActive(false);
                LayoutBackdrop(0f, 0f, false);
                return;
            }

            float heightU = 0f;
            for (int i = 0; i < _views.Length; i++)
            {
                heightU = Mathf.Max(heightU, _views[i].HeightU);
            }

            // D-006: el número de filas lo fijan las cuias generadas al cargar el nivel y no cambia a mitad de nivel; cuando
            // Cuia extra o Cuia de rescate añadan una vista (fuera de v0), va a la fila más corta y solo se recalcula el ancho
            // con el T en pantalla (re-layout de 300 ms, 02 §4.1; tope de D-004).
            int generated = _generatedCount > 0 ? Mathf.Min(_generatedCount, _views.Length) : _views.Length;
            _layout = BoardLayout.Compute(_views.Length, generated, heightU, w, BoardTop, BoardBottom);
            BoardLayoutResult l = _layout;
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].SetGeometry(new Vector2(l.CenterX[i], -l.FootY[i]), l.CuiaWidth, l.SlotWidth);
            }

            // Panel (guía 4.6): sombra proyectada (y + 6, desenfoque ≈ 10), relleno, sombra interior y marco. El marco tallado
            // mide 12 dp; si las cuias quedarían a menos de 8 dp de él (14 cuias en 2 filas), se adelgaza con el
            // multiplicador del 9-slice, nunca encogiendo una cuia (guía 3.2).
            float px = l.PanelX;
            float py = l.PanelY;
            float pw = l.PanelWidth;
            float ph = l.PanelHeight;
            float leftEdge = float.MaxValue;
            for (int i = 0; i < _views.Length; i++)
            {
                leftEdge = Mathf.Min(leftEdge, l.CenterX[i] - (l.CuiaWidth * 0.5f));
            }

            float frameScale = Mathf.Clamp((leftEdge - px - 8f) / _art.PanelFrameDp, 0.45f, 1f);
            _panelFrame.pixelsPerUnitMultiplier = 1f / frameScale;
            float frame = _art.PanelFrameDp * frameScale;
            PanelFrameDp = frame;
            _panelShadow.enabled = true;
            _panelFill.enabled = true;
            _panelInner.enabled = true;
            _panelFrame.enabled = true;
            UiBuilder.Place(_panelShadow.rectTransform, px - 14f, py - 8f, pw + 28f, ph + 28f);
            UiBuilder.Place(_panelFill.rectTransform, px + frame - 1f, py + frame - 1f, pw - (2f * frame) + 2f, ph - (2f * frame) + 2f);
            UiBuilder.Place(_panelInner.rectTransform, px + frame, py + frame, pw - (2f * frame), ph - (2f * frame));
            UiBuilder.Place(_panelFrame.rectTransform, px, py, pw, ph);

            // Una repisa por fila (guía 4.5): de innerLeft + 6 a innerRight − 6, cara superior en pie − 3, 26 dp con sombra.
            float shelfX = px + frame + 6f;
            float shelfW = pw - (2f * frame) - 12f;
            var active = new RectTransform[l.Rows];
            for (int r = 0; r < l.Rows && r < _shelves.Length; r++)
            {
                float foot = l.RowFootY[r];
                _shelves[r].gameObject.SetActive(true);
                UiBuilder.Place(_shelves[r].rectTransform, shelfX, foot - 3f, shelfW, 26f);
                RectTransform water = _rowWater[r].rectTransform;
                water.anchoredPosition = new Vector2(px + (pw * 0.5f), -(foot - 3f));
                water.sizeDelta = new Vector2(shelfW, 0f);
                active[r] = water;
            }

            _rowWaterActive = active;

            // Capi (solo con su PNG) y el poste en el muelle, si la banda libre bajo el panel es ≥ 64 dp (guía 3.7 y 3.8).
            float band = TrayTop - l.PanelBottom;
            bool decor = band >= CapiMinBandDp;
            _capi.gameObject.SetActive(decor && _art.Capi != null);
            _post.gameObject.SetActive(decor);
            if (decor)
            {
                UiBuilder.Place(_capi.rectTransform, 12f, TrayTop - 48f, 64f, 56f);
                _capi.rectTransform.pivot = new Vector2(0f, 0f);
                _capi.rectTransform.anchoredPosition = new Vector2(12f, -(TrayTop + 8f));
                UiBuilder.Place(_post.rectTransform, w - 56f, TrayTop + 4f - 48f, 40f, 48f);
            }

            LayoutBackdrop(py, l.PanelBottom, true);
            _animator.BoardWidth = w; // lado del cuerpo al verter (D-006): el lienzo del layout
            _ambient.SetLayout(new Vector2(w, _canvasH), PanelRect(), new Rect(0f, _horizonY + 4f, w, Mathf.Max(0f, py - _horizonY - 8f)));
        }

        // Panorama, cielo, muelle y escena del río según el panel (revisión visual del 2026-10-07):
        //  - el horizonte queda en clamp(panel.top − 64, 118, 200) dp (a 360 de ancho) y el hueco de arriba se rellena con
        //    cielo_alto: nivel 7 (panel en 162,1) en 118; nivel 1 (panel en 226,9) en 162,9 (D-006);
        //  - la viga del muelle va bajo el panel (panel.bottom − 4): el tablero se apoya en el muelle, sin postes;
        //  - con ≥ 56 dp de río visible, una garza en la orilla izquierda y 3 vitórias-régias junto a las orillas.
        private void LayoutBackdrop(float panelTop, float panelBottom, bool hasBoard)
        {
            float w = _canvasW;
            float h = _canvasH;
            float k = w / 360f;
            float baseHorizon = ProceduralArt.PanoramaHorizonDp * k;
            float horizon = hasBoard ? HorizonFor(panelTop, w) : baseHorizon;
            float shift = horizon - baseHorizon;
            _horizonY = horizon;
            UiBuilder.Place(_panorama.rectTransform, 0f, shift, w, w * (ProceduralArt.PanoramaHeightDp / 360f));
            _skyFill.enabled = shift > 0.01f;
            UiBuilder.Place(_skyFill.rectTransform, 0f, 0f, w, shift + 1f);

            float beam = hasBoard ? panelBottom - 4f : h - 288f;
            beam = Mathf.Min(beam, TrayTop - 24f);
            float dockTop = beam - (ProceduralArt.DockBeamTopDp * k);
            Sprite? dock = _dock.sprite;
            float aspectH = dock != null ? w * (dock.rect.height / dock.rect.width) : w;
            UiBuilder.Place(_dock.rectTransform, 0f, dockTop, w, Mathf.Max(aspectH, h - dockTop));

            bool props = hasBoard && panelTop - horizon >= RiverPropsMinBandDp;
            _heron.gameObject.SetActive(props);
            if (props)
            {
                // Sobre la línea de agua de la orilla izquierda (en x = 34 dp la orilla acaba 5,4 dp bajo el horizonte).
                PlaceAt(_heron.rectTransform, 34f * k, horizon + (5.4f * k), 22f * k, 31f * k, new Vector2(0.5f, 0f));
            }

            PlaceLilyPad(0, props, 54f * k, horizon + (30f * k), 34f * k, panelTop);
            PlaceLilyPad(1, props, 314f * k, horizon + (22f * k), 26f * k, panelTop);
            PlaceLilyPad(2, props, 86f * k, horizon + (47f * k), 24f * k, panelTop);
        }

        private void PlaceLilyPad(int i, bool props, float x, float y, float width, float panelTop)
        {
            float height = width / 3f;
            bool on = props && y + (height * 0.5f) <= panelTop - 8f;
            _lilyPads[i].gameObject.SetActive(on);
            if (on)
            {
                PlaceAt(_lilyPads[i].rectTransform, x, y, width, height, new Vector2(0.5f, 0.5f));
            }
        }

        // Velo con hueco, brillo y rayos de la victoria (en la escena, detrás del tablero).
        private void SetVictoryLights(bool on)
        {
            if (_victoryVeil.gameObject.activeSelf != on)
            {
                _victoryVeil.gameObject.SetActive(on);
                _victoryGlow.gameObject.SetActive(on);
                _victoryRays.gameObject.SetActive(on);
            }
        }

        private Rect PanelRect()
        {
            if (_layout == null)
            {
                return new Rect(0f, 0f, 0f, 0f);
            }

            return new Rect(_layout.PanelX, _layout.PanelY - _boardShift, _layout.PanelWidth, _layout.PanelHeight);
        }

        // Con el panel de victoria o el modal anclados abajo, el tablero sube lo justo para que el panel del tablero quede
        // por encima (07 §5.5: "las cuias siguen visibles"; 02 §6), sin que las cuias pasen de la cabecera. La cinta de
        // victoria cuenta como borde (panel.top − 38; la guía 5.3 decía 34). Desplazar el contenedor no cambia las
        // coordenadas internas que usa la cola de animación.
        private float ShiftToClear(float panelTop, float gap)
        {
            if (_layout == null || _views.Length == 0)
            {
                return 0f;
            }

            float highestTop = float.MaxValue;
            for (int i = 0; i < _views.Length; i++)
            {
                highestTop = Mathf.Min(highestTop, _layout.FootY[i] - (_views[i].HeightU * _views[i].Unit) - CuiaView.LiftDp);
            }

            float needed = _layout.PanelBottom - (panelTop - gap);
            float room = highestTop - BoardTop;
            return Mathf.Clamp(needed, 0f, Mathf.Max(0f, room));
        }

        private void ShiftBoard(float target, bool animate)
        {
            _boardShiftTarget = target;
            if (!animate || Mathf.Approximately(_boardShift, target))
            {
                _boardShiftAnim = -1f;
                SetBoardShift(target);
                return;
            }

            _boardShiftFrom = _boardShift;
            _boardShiftAnim = 0f;
        }

        private void TickBoardShift(float dtMs)
        {
            if (_boardShiftAnim < 0f)
            {
                return;
            }

            _boardShiftAnim += dtMs;
            float t = Mathf.Clamp01(_boardShiftAnim / PanelSlideMs);
            SetBoardShift(Mathf.Lerp(_boardShiftFrom, _boardShiftTarget, Ease.Out(t)));
            if (t >= 1f)
            {
                _boardShiftAnim = -1f;
            }
        }

        private void SetBoardShift(float shift)
        {
            _boardShift = shift;
            _board.anchoredPosition = new Vector2(0f, shift);
            _pourFront.anchoredPosition = new Vector2(0f, shift); // la capa de vertido sigue al tablero
        }

        // Capi (guía 3.7; solo con capi_idle.png): respiración (escala Y 1,00 ↔ 1,02, periodo 3 s). El parpadeo llega con su
        // arte en la fase de jugabilidad. Nada en él depende de la selección ni de la legalidad (D-002).
        private void TickCapi(float dtMs)
        {
            if (AmbientLayer.Frozen || !_capi.gameObject.activeSelf)
            {
                return;
            }

            _capiTime += dtMs;
            float breathe = 1f + (0.01f * (1f - Mathf.Cos((_capiTime / 3000f) * Mathf.PI * 2f)));
            _capi.rectTransform.localScale = new Vector3(1f, breathe, 1f);
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

        internal static void ReadScreen(out int width, out int height, out Rect safeArea)
        {
            if (SimulatedScreen.HasValue)
            {
                Vector4 sim = SimulatedScreen.Value;
                width = Mathf.Max(1, (int)sim.x);
                height = Mathf.Max(1, (int)sim.y);
                safeArea = new Rect(0f, sim.w, width, height - sim.z - sim.w);
                return;
            }

            width = Mathf.Max(1, Screen.width);
            height = Mathf.Max(1, Screen.height);
            safeArea = Screen.safeArea;
        }

        private static RectTransform HitLayer(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(HitArea));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            UiBuilder.Stretch(rt);
            go.GetComponent<HitArea>().raycastTarget = true;
            return rt;
        }
    }
}
