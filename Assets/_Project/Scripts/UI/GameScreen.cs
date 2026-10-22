#nullable enable
using System;
using System.Collections.Generic;
using AF.Ads;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Pantalla de nivel construida por código sobre un Canvas de 360 dp de ancho de referencia (GDD 02 §5; 07 §7.4) con
    /// la dirección visual v2 «Selva brillante premium» (D-003; docs/art/direccion-visual-v2.md): panorama del Estuario,
    /// muelle y follaje; panel azul petróleo con marco tallado y una repisa por fila; HUD con pausa, insignia y Reiniciar en
    /// las posiciones de 02 §5 y 07 §7.4; bandeja de comodines (<see cref="BoosterTray"/>: Marea y Cuia extra, B2-1); el
    /// embudo de bloqueo de D-008 (B2-2 y B2-3): Aguas estancadas (<see cref="StuckModal"/>), ¿Seguro?, Nivel fallido y Sin
    /// canoas, con los saldos arriba; pausa con «Volver a la orilla» y «Salir» y sus confirmaciones; el cartel «El río
    /// bajó»; y la victoria con cinta-hoja y estallido (02 §6; D-008 punto 3; tarea B2-4): la Cosecha que cuenta hacia arriba
    /// en 400 ms, desde el 11 «Cosecha doble: x2 por video» (sin anuncio no aparece), Siguiente (→ AD_GATE) y el secundario
    /// «Inicio» [P] (06 §4.7), con el saldo arriba como en el embudo. Reabre el nivel guardado sin intro, con la pantalla que
    /// estaba abierta (02 §14; tarea B2-5). Escucha a <see cref="LevelFlow"/> y hace que la vista persiga al modelo (02 §3.2).
    /// Se oculta mientras se ve la pantalla de inicio (<see cref="SetVisible"/>).
    /// </summary>
    /// <remarks>
    /// Capas de atrás adelante (guía 3.4): panorama, ambiente lejano (con la capa de crecida del río), muelle, follaje, velo
    /// de escena; captura de fondo; tablero (sombra, relleno, sombra interior y marco del panel, repisas, cuias, la capa de
    /// «Llenar y teñir» con su agua, chorritos y frutas, chorros y efectos); la tropa de miquitos y su rama; el poste del muelle;
    /// velo de estado, HUD y bandeja; la cuia elevada cuyo cielo entra en la fila del HUD (tableros de 3 filas, [P]); polen;
    /// avisos del tablero (el aviso y la tarjeta «¡Miquito a la vista!», D-011, punto 2; GameScreen.Notices.cs); capa de vertido
    /// (el origen y su chorro mientras vierte, delante de todo, D-006, también de los avisos); estallido; modales; captura de
    /// saltos. El cartel («El río bajó» y el rótulo del intento) se pone encima de todo al salir.
    /// Las capas que no son botones ni áreas de cuia no reciben toques (V7). Dentro del tablero, las cuias van entre dos capas
    /// de mecánicas (detrás y delante; arquitectura del paso 1, §6), y la delantera, detrás de la intro, los chorros y los
    /// efectos.
    /// <para>Clase parcial (arquitectura del paso 1, §7): los métodos parciales <c>Labels*</c>, <c>Cards*</c>,
    /// <c>Reveal*</c> y <c>FunnelModalShowing</c> son los ganchos que implementan otros archivos (GameScreen.Labels.cs,
    /// GameScreen.Cards.cs, GameScreen.Reveal.cs y GameScreen.Funnel.cs) sin editar este. Sin implementación no hacen
    /// nada. Los del Mapa del río y de la victoria (arquitectura de la tanda 4, §2; tarea M1.3b): <c>Stop*</c> (fondo por
    /// parada, GameScreen.Stop.cs), <c>Arrival*</c> (llegada, GameScreen.Arrival.cs), <c>Slot*</c> (ranura del Dato del río) y
    /// <c>Sloth*</c> (TiTino en la victoria), estos dos en GameScreen.Victory.cs. Los de Morpho · Pista (tarea MO2):
    /// <c>Hint*</c> (GameScreen.Morpho.cs).</para>
    /// </remarks>
    public sealed partial class GameScreen : ILevelFlowListener
    {
        /// <summary>Ancho de referencia del lienzo (dp).</summary>
        public const float ReferenceWidth = 360f;

        /// <summary>
        /// Ancho mínimo del lienzo (dp; 02 §5, «Dispositivos»: «a 320 dp de ancho se admiten 6 cuias por fila»). Entre 320 y 360 dp
        /// físicos el lienzo mide lo que el teléfono, con 1 dp físico por dp de lienzo (Q-B11-5 (a), D-033, añadido de B11; cierre
        /// de B11); por debajo de 320, mide 320 y se escala (<see cref="CanvasScale(int, int, float)"/>).
        /// </summary>
        public const float MinWidth = 320f;

        /// <summary>
        /// Alto mínimo del lienzo (dp; 16:9 de 02 §5). El lienzo mide 360 dp de ancho y, si así quedaran menos de 640 dp de alto,
        /// manda el alto (<see cref="CanvasScale"/>): en 16:9 mide 360 × 640 dp y 1 dp de lienzo es 1 dp físico, así que el texto
        /// no baja de 12 sp físicos (02 §12; pregunta 19 de GDD.md §6.3, (a); D-028, tarea DC3). En 19,5:9 y 20:9, 360 × 780 y
        /// 360 × 800, como antes.
        /// </summary>
        public const float MinHeight = 640f;

        /// <summary>Alto del lienzo en la pantalla de referencia (dp; 19,5:9: 1080 × 2340 px a 3 px/dp).</summary>
        public const float ReferenceHeight = 780f;

        /// <summary>Zona segura superior mínima (02 §5).</summary>
        public const float MinSafeTop = 32f;

        /// <summary>Zona segura inferior mínima (02 §5).</summary>
        public const float MinSafeBottom = 24f;

        /// <summary>
        /// Banda mínima del muelle bajo el panel para enseñar el poste de amarre (guía 3.7 y 3.8). Capi ya no se queda en el muelle
        /// durante la partida (D-016, T5-B; 02 §5, «Mascotas durante la partida»); el nombre se conserva.
        /// </summary>
        public const float CapiMinBandDp = 64f;

        private const float PanelSlideMs = 250f;
        private const float OverlayFadeMs = 200f;

        /// <summary>Borde derecho de la insignia del nivel (placa de 136 dp en x = 56, con sus hojas; guía 3.5).</summary>
        private const float HudBadgeRight = 56f + 136f;

        /// <summary>
        /// El rótulo «Nivel {n} · intento {k}» del reintento (02 §2, INTRO) se ve al menos este tiempo, aunque la intro corta
        /// dure 800 ms o se salte, como el rótulo «Level N» de Magic Sort al reintentar (ficha §4.1, t = 340-342) [P].
        /// </summary>
        private const float AttemptBannerMs = 1400f;

        /// <summary>Relayout animado al añadir una cuia (02 §4.1: «600 ms + 300 ms re-layout»).</summary>
        private const float RelayoutMs = 300f;

        /// <summary>Entrada de la cuia que trae Capi (02 §4.1: 600 ms).</summary>
        private const float AddedCuiaMs = 600f;

        /// <summary>
        /// Panel de victoria (guía 5.3, ajustado al contenido en la revisión visual del 2026-10-07; guía 12: la fila de 44 dp de
        /// «Semillas +N» que llega con la tarea B2-4): 36 dp de cinta dentro, la fila de la Cosecha
        /// (<see cref="VictoryHarvestDp"/>), desde el 11 la Cosecha doble (CTA de agua, 66 dp), Siguiente (66 dp), el secundario
        /// «Inicio» [P] (07 §7.3: 40 dp de cara, área táctil de 48) y 18 dp de marco: 218 dp sin Cosecha doble y 286 con ella.
        /// </summary>
        private const float VictoryHarvestTopDp = 36f;

        /// <summary>Alto de la fila de la Cosecha (guía 12: 44 dp).</summary>
        private const float VictoryHarvestDp = 44f;

        /// <summary>Del borde superior del CTA al del secundario «Inicio» (68 dp, como desde B1-6).</summary>
        private const float VictoryHomeBelowCtaDp = 68f;

        /// <summary>Ancho del secundario «Inicio» (dp).</summary>
        private const float VictoryHomeWidthDp = 168f;

        /// <summary>La Cosecha empieza a contar cuando el panel ya subió (250 ms) y cuenta en 400 ms (02 §6).</summary>
        private const float HarvestCountDelayMs = 250f;

        /// <summary>
        /// Tras cobrar la Cosecha doble, la fila cuenta de +N a +2N (<see cref="GameConfig.HarvestCountMs"/>, 400 ms) y se ve
        /// este tiempo más antes de pasar sola al nivel siguiente por el camino de Siguiente (D-011, punto 4) [P].
        /// </summary>
        internal const float DoubleHarvestAdvanceMs = 600f;

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

        // Título de la cinta de victoria (guía 5.5, con la revisión visual del 2026-10-07): Baloo 2 de 30 sp, mínimo 22 (× la
        // escala de texto). Si a 22 no cabe en una línea (otro idioma, en-XA o la fuente al 130 %; tarea L4), va en dos líneas a
        // 22 sp con el interlineado apretado [P] y la hoja crece a lo alto hasta RibbonTallDp (guía 5.5: «la hoja crece hasta
        // 88 dp de alto. Nunca se recorta»).
        private const float RibbonTitleSp = 30f;
        private const float RibbonTitleMinSp = 22f;
        private const float RibbonTitleLineSpacing = -45f;

        /// <summary>Alto máximo de la cinta-hoja de victoria con el título en dos líneas (guía 5.5).</summary>
        private const float RibbonTallDp = 88f;

        // Margen de la cinta de 72 dp alrededor de la franja del título (72 − 42), que se conserva al crecer.
        private const float RibbonTitleMarginDp = 30f;

        // Píldora «Reiniciar» que crece (tarea L4) [P]: aire a cada lado de su etiqueta, lo que puede salir del área de 60 dp
        // por la derecha (queda a 4 dp del borde) y el aire hasta la columna del saldo.
        private const float RestartPillPadDp = 4f;

        // Aire más corto a cada lado de la etiqueta de «Reiniciar» cuando una palabra no cabe con RestartPillPadDp ni con la
        // columna del saldo a su ancho mínimo (tarea FX5) [P]. Nunca menos: con 2 dp, en en-XA al 130 % a 360 dp, el «[» y la
        // «r» quedaban pegados a los cantos (revisión de FX5; D-003). La etiqueta ya va a 12 sp × escala, el mínimo de 02 §12,
        // así que no puede encoger para ganar aire.
        internal const float RestartPillTightPadDp = 3f;
        private const float RestartPillOutDp = 4f;
        internal const float RestartPillGapDp = 4f;

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
        private readonly IGameActions _actions;

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
        private readonly RectTransform _mechanicBackLayer;
        private readonly RectTransform _mechanicFrontLayer;
        private readonly RectTransform _streamLayer;
        private readonly FxLayer _fx;

        private readonly Image _post;

        private readonly Image _scrim;
        private readonly RectTransform _hud;
        private readonly Image _badge;
        private readonly TextMeshProUGUI _levelText;
        private readonly ReliefParts _restart;
        private readonly Image _restartPill;
        private readonly TextMeshProUGUI _restartLabel;
        private float _restartPillLeft = float.NaN; // LayoutRestartPill (tarea FX5)
        private readonly BoosterTray _tray;
        private readonly RectTransform _liftFront;
        private readonly RectTransform _ambientNearParent;
        private readonly AmbientLayer _ambient;
        private readonly RectTransform _pourFront;

        private readonly RectTransform _confettiLayer;
        private readonly RectTransform _skipCatcher;

        private readonly RectTransform _stuckOverlay;
        private readonly Image _stuckDim;
        private readonly StuckModal _stuck;
        private readonly GiveUpConfirm _giveUp;
        private readonly LevelFailedScreen _failed;
        private readonly NoCanoesPanel _noCanoes;
        private DailyTeaserCard? _dailyTeaser; // Reto del río tras ganar el 7 (E6): se crea en la primera victoria del 7
        private readonly RectTransform _topLayer;
        private readonly ReliefParts _pauseButton;
        private readonly WalletBar _walletBar;
        private readonly PauseMenu _pause;
        private readonly QuitConfirm _confirm;
        private readonly Toast _banner;
        private readonly Toast _toast;
        private readonly SightingCard _sighting;
        private readonly List<string> _layoutIds = new List<string>(12);
        private readonly List<RectTransform> _layoutRects = new List<RectTransform>(12);

        private readonly RectTransform _victoryOverlay;
        private readonly Image _victoryDim;
        private readonly Image _victoryRays;
        private readonly RectTransform _victoryPanel;
        private readonly Image _victorySurface;
        private readonly Image _ribbon;
        private readonly TextMeshProUGUI _ribbonTitle;
        private readonly float _ribbonTitleMinDp;
        private readonly float _ribbonTitleMaxDp;
        private float _ribbonHeightDp = RibbonHeightDp;
        private readonly ReliefParts _next;
        private readonly CanvasGroup _nextGroup;
        private readonly ReliefParts _home;
        private readonly CanvasGroup _homeGroup;
        private readonly RectTransform _harvestRow;
        private readonly TextMeshProUGUI _harvestLabel;
        private readonly Image _harvestSeed;
        private readonly TextMeshProUGUI _harvestValue;
        private readonly ReliefParts _double;
        private readonly CanvasGroup _doubleGroup;
        private readonly Image _doubleIcon;
        private readonly TextMeshProUGUI _doubleLabel;
        private readonly TextMeshProUGUI _doubleSub;
        private readonly char[] _harvestBuf = new char[16];

        private readonly BoardAnimator _animator;
        private readonly IntroAnimation _intro;
        private readonly TroopLayer _troop;
        private readonly ConfettiLayer _confetti;
        private readonly Action<int> _cuiaDown;
        private readonly Action<int, bool> _cuiaUp;

        private LevelFlow? _flow;
        private GameServices? _services;
        private int _pressPreview = -1;
        private CuiaView[] _views = Array.Empty<CuiaView>();
        private int _generatedCount;
        private FaunaSpecies? _introSpecies;
        private bool _introRetry;
        private int _animSkipped;
        private int _lastSkipAtMs = -1;
        private int _lastSkipCfgMs = -1;
        private bool _introAnimStarted;
        private int _introCfgMs;
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
        private AF.Audio.SeedTicker _seedTicker; // AU1: sfx_seeds_tick de la Cosecha
        private bool _restartProbePending;
        private float _horizonY = 118f;
        private PourTimings _timings;
        private int _stuckSeedsShown = -1;
        private int _stuckUndoShown = -1;
        private int _stuckNotesShown = -1; // F3-5: la versión de las notas de compra con la que se armó el modal
        private bool _stuckBalsaOk;
        private bool _attemptBanner;
        private FlowPhase _chromePhase = FlowPhase.Loading;
        private float _introDelayMs = -1f;
        private int _introPendingMs;
        private float _relayoutMs = -1f;
        private float _victoryPanelDp = 218f;
        private float _victoryCtaTopDp = 84f;
        private float _victoryHomeTopDp = 152f;
        private readonly TextMeshProUGUI _nextLabel;
        private readonly TextMeshProUGUI _homeLabel;
        private readonly float _nextLabelDp;
        private bool _doubleShown;
        private int _harvestFrom;
        private int _harvestTo = -1;
        private int _harvestShown = -1;
        private float _harvestCountMs = -1f;
        private bool _harvestCounting;
        private float _autoNextMs = -1f; // espera tras la Cosecha doble (D-011, punto 4): −1 = sin espera; 0 = vencida (TakeAutoNext)
        private Vector2[] _relayoutFrom = Array.Empty<Vector2>();
        private float[] _relayoutFromWidth = Array.Empty<float>();
        private readonly Vector3[] _relayoutShelfFrom = new Vector3[3]; // (x, y desde arriba, ancho) de cada repisa al empezar
        private int _addedView = -1;

        // La intro está retenida en su fotograma 0 (detrás de la tarjeta previa, GameScreen.Cards.cs): Tick no la avanza.
        // StartIntro la suelta; quien la quiera retenida lo marca después de arrancarla.
        private bool _introHeld;

        // Pantallas de AF_LAYOUT de los modales del embudo del nivel (EmitFunnelLayout).
        private const string GiveUpConfirmLayout = "giveup_confirm";
        private const string LevelFailedLayout = "level_failed";
        private const string NoCanoesLayout = "no_canoes";

        // OnLevelLoaded está avisando a los ganchos *LevelLoaded: la AF_LAYOUT level se aplaza a su final (una sola, ya con las
        // ranuras de la bandeja de este nivel), aunque un gancho la pida antes (Revelar, al aparecer su ranura).
        private bool _levelLayoutDeferred;

        /// <summary>
        /// Construye la pantalla bajo <paramref name="host"/>. Las acciones son los puntos de entrada del input
        /// (los mismos que usan los tests PlayMode).
        /// </summary>
        public GameScreen(Transform host, ProceduralArt art, DevProbe probe, IGameActions actions)
        {
            _art = art;
            _probe = probe;
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
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

            // Fondo de la parada del Mapa del río (M1.4): sus capas van delante del panorama y detrás de la garza.
            StopBuild(scene);

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
            bgCatcher.gameObject.AddComponent<PointerTapRelay>().Bind(() => _actions.TapBackground());

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

            // Capas de mecánicas (arquitectura del paso 1, §6): lo que une cuias o ronda el tablero va detrás o delante de las
            // cuias, nunca en su cuerpo. Sin Graphic ni raycast; BuildBoard las coloca a los dos lados de las cuias.
            _mechanicBackLayer = UiBuilder.Rect("MechanicBack", _board);
            _mechanicFrontLayer = UiBuilder.Rect("MechanicFront", _board);

            // «Llenar y teñir» (guion de la intro): su Canvas anidado va delante de las cuias (BuildBoard lo reordena).
            _intro = new IntroAnimation(_board, art);
            _streamLayer = UiBuilder.Rect("Streams", _board);
            _fx = new FxLayer(_board, art);

            // ---- Tropa de miquitos (guion de la intro §4.4): Canvas anidado en la escena, justo por detrás del follaje de las
            // esquinas (la rama sale de él; la tropa entra y sale trepando por él) y del tablero; el labio del río que desborda
            // va por detrás de la rama (agua < rama < tropa, guion §4.6.5). ----
            _troop = new TroopLayer(scene, art);
            _troop.Layer.SetSiblingIndex(_foliage[0].transform.GetSiblingIndex());
            _intro.Troop = _troop;
            _intro.AttachFront(_troop.WaterSlot);
            _troop.PlaceSideLayer(_board); // tropa lateral (D-021): delante del panel y del marco, detrás de las cuias (OrderBoardLayers)
            _intro.AttachFrameSides(_troop.SideWaterSlot); // el agua de los costados del marco, por detrás de la liana lateral (TL1)

            // ---- Accesorio del muelle (capa 8): el poste, fuera de las zonas táctiles. Ninguna mascota fija en el muelle durante
            // la partida (D-016, T5-B; 02 §5): Capi ya no se dibuja aquí; solo entra con la Cuia extra, la de rescate y el banner
            // de mundo. ----
            RectTransform decor = UiBuilder.Rect("Decor", _root);
            _post = UiBuilder.Image("RopePost", decor, art.RopePost, Color.white);

            // ---- HUD (capa 9): velo de estado, insignia, Reiniciar y bandeja con Deshacer · Marea. ----
            _hud = UiBuilder.Rect("Hud", _root);
            _scrim = UiBuilder.Image("StatusScrim", _hud, art.StatusScrim, Color.white);
            _badge = UiBuilder.Image("LevelBadge", _hud, art.BadgePlate, Color.white);
            _levelText = UiBuilder.Label("LevelTitle", _badge.transform, TextStyle.Badge, UiText.Level(1), 26f, Palette.Cream, TextAlignmentOptions.Center);
            // Guía 3.5: 26 dp, mínimo 20, nunca se recorta; fuera del español (en-XA, «[Ñívél 100 ··]») no cabe en la placa ni a
            // 20 sp, y la placa no crece (la columna del saldo va pegada; la fila del HUD no se rediseña). Decidido el 2026-10-19
            // (D-030, pregunta 8 b): fuera del español, solo cuando no cabe a 20 sp baja hasta 12 sp (02 §12); en español sigue
            // a 20 sp o más, sin cambio visual (DC19; FitLevelBadge).
            UiBuilder.AutoSize(_levelText, BadgeMinSp, BadgeMaxSp);

            // Volver a la orilla: disco de 40 dp (familia Madera) con píldora «Reiniciar» en un área de 60 × 48 (guía 3.5).
            _restart = UiBuilder.Relief("RestartButton", _hud, art.DiscWood, OnHudRestart);
            RectTransform restartVisual = (RectTransform)_restart.Face.parent;
            restartVisual.anchorMin = new Vector2(0.5f, 1f);
            restartVisual.anchorMax = new Vector2(0.5f, 1f);
            restartVisual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            restartVisual.anchoredPosition = Vector2.zero;
            Image restartIcon = UiBuilder.Image("Icon", _restart.Face, art.IconRestart, Color.white);
            UiBuilder.Place(restartIcon.rectTransform, 7f, 7f, 26f, 26f);
            _restartPill = UiBuilder.Image("Pill", _restart.Button.transform, art.Pill, Color.white);
            _restartPill.type = Image.Type.Sliced; // crece si su etiqueta no cabe (LayoutRestartPill; tarea L4)
            TextMeshProUGUI restartLabel = UiBuilder.Label("Label", _restartPill.transform, TextStyle.PillWood, UiText.Restart, 12f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Stretch(restartLabel.rectTransform);
            restartLabel.rectTransform.offsetMin = new Vector2(0f, 1f);
            _restartLabel = restartLabel;
            _restart.Relief.Bind(_restart.Face, _restart.Base, _restart.FaceImage, art.DiscWood, restartIcon, null, null);

            // Bandeja de comodines (B2-1; 07 §7.4): Marea, desde el 8 Cuia extra y desde el 12 Pista (MO2).
            _tray = new BoosterTray(_hud, art, OnTrayUndo, () => _actions.PressExtraCuia());

            // ---- Cuia elevada cuyo cielo entra en la fila del HUD (revisión de la captura 48c): en los tableros que pasan bajo el
            // HUD (3 filas a 360 × 780), los 32 dp sobre la boca de una cuia de la fila 1 (la nube de la Lluvia, la cueva de la
            // Piraña) suben con ella 16 dp al elevarla y la insignia «Nivel {n}» los tapaba. Mientras sube, está elevada o baja,
            // la cuia entera se dibuja aquí: delante del HUD y de la bandeja, detrás del polen, de la capa de vertido y de la
            // pausa (capa superior) [P], como el origen que vierte (D-006). Sin Graphic propio, alineada con el tablero (sigue a
            // ShiftToClear) y vacía casi siempre (SyncLiftFront). ----
            _liftFront = UiBuilder.Rect("LiftFront", _root);

            _ambientNearParent = UiBuilder.Rect("AmbientNearHost", _root);
            _ambient = new AmbientLayer(_ambientFarParent, _ambientNearParent, art);
            _ambient.SetFoliage(_foliage[0].rectTransform, _foliage[1].rectTransform, _foliage[2].rectTransform, _foliage[3].rectTransform);
            _intro.Ambient = _ambient;

            // ---- Avisos del tablero (D-011, punto 2; GameScreen.Notices.cs): el aviso y la tarjeta «¡Miquito a la vista!»,
            // delante del tablero, del HUD y del polen, pero justo detrás de la capa de vertido: el origen que vierte va delante
            // de todo (D-006), y la tarjeta, que dura ~5 s y sale ya en juego, lo alcanza en un vertido de la fila de arriba (en
            // el 5, la boca del origen que se vacía entra ~13 dp en el canto inferior de la placa; revisión de A2, captura
            // a2_04). Los modales y la pausa también quedan delante. Sin Graphic ni raycast; no sigue a ShiftToClear (los avisos
            // se colocan en el lienzo). ----
            _noticeLayer = UiBuilder.Rect("Notices", _root);

            // ---- Capa de vertido (D-006): mientras vierte, el cuerpo del origen y su chorro se dibujan aquí, delante de las
            // vecinas, del marco del panel, del HUD, del polen y de los avisos, como Magic Sort (t = 250: el origen tapa el
            // rótulo del nivel). Sin Graphic ni raycast; alineada con el tablero (sigue a ShiftToClear) y vacía en reposo. ----
            _pourFront = UiBuilder.Rect("PourFront", _root);

            _confettiLayer = UiBuilder.Rect("Burst", _root);
            _confetti = new ConfettiLayer(_confettiLayer, art, GameConfig.AnimConfettiMaxParticles);

            // ---- Aguas estancadas (02 §7; 07 §7.5; guía 5.3): tablero atenuado detrás; panel de madera clara abajo. ----
            _stuckOverlay = UiBuilder.Rect("StuckOverlay", _root);
            _stuckDim = UiBuilder.Image("Dim", _stuckOverlay, null, new Color(0f, 0f, 0f, 0f), true);
            UiBuilder.Stretch(_stuckDim.rectTransform);
            _stuck = new StuckModal(_stuckOverlay, art, actions);
            _stuckOverlay.gameObject.SetActive(false);

            // ---- Victoria (02 §6; 07 §5.4; guía 5.3, 5.5 y 6.9): panel abajo, cinta-hoja y Siguiente; sin auto-avance por
            // tiempo, salvo tras la Cosecha doble, que avanza sola por el camino de Siguiente (D-011, punto 4; TickAutoNext). ----
            _victoryOverlay = UiBuilder.Rect("VictoryOverlay", _root);
            _victoryDim = UiBuilder.Image("Dim", _victoryOverlay, null, new Color(0f, 0f, 0f, 0f), true);
            UiBuilder.Stretch(_victoryDim.rectTransform);

            _victoryPanel = UiBuilder.Rect("Panel", _victoryOverlay);
            _victorySurface = UiBuilder.ModalBody(_victoryPanel, art);
            _next = UiBuilder.Relief("NextButton", _victoryPanel, art.CtaLeaf, OnNextPressed); // con las barras pendientes, las completa (B1b)
            _nextGroup = _next.Face.parent.gameObject.AddComponent<CanvasGroup>();
            _nextGroup.blocksRaycasts = false;
            // Con la escala de texto, Siguiente y la Cosecha doble crecen en alto con ella, como los CTA de los modales (07 §7.8;
            // tarea L4): al 130 %, «Siguiente» a 28,6 dp no cabía en la cara de siempre.
            ModalKit.StretchForTextScale(_next);
            TextMeshProUGUI nextLabel = UiBuilder.Label("Label", _next.Face, TextStyle.CtaLeaf, UiText.Next, 22f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(nextLabel.rectTransform, ModalKit.FaceHeightOf(_next));
            _nextLabel = nextLabel;
            _nextLabelDp = nextLabel.fontSize;
            _next.Relief.Bind(_next.Face, _next.Base, _next.FaceImage, art.CtaLeaf, null, null, nextLabel);

            // «Inicio» [P] (06 §4.7; D-008 punto 3): secundario de madera (07 §7.3) bajo Siguiente; vuelve a la pantalla de inicio.
            _home = UiBuilder.Relief("HomeButton", _victoryPanel, art.CtaWood, OnVictoryHome);
            _homeGroup = _home.Face.parent.gameObject.AddComponent<CanvasGroup>();
            _homeGroup.blocksRaycasts = false;
            TextMeshProUGUI homeLabel = UiBuilder.Label("Label", _home.Face, TextStyle.PillWood, UiText.Home, 18f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.PlaceCtaLabel(homeLabel.rectTransform, art.CtaWood);
            UiBuilder.AutoSize(homeLabel, 14f, 18f);
            _homeLabel = homeLabel;
            _home.Relief.Bind(_home.Face, _home.Base, _home.FaceImage, art.CtaWood, null, null, homeLabel);

            // Cosecha (02 §6; D-008 punto 3; guía 12): «Cosecha» y la semilla con +20, +60 u +80, que cuenta hacia arriba en
            // 400 ms. Hace de «Rewards:» con el montón de monedas de Magic Sort (ficha §4.6 y §4.8), sin su trade dress.
            _harvestRow = UiBuilder.Rect("Harvest", _victoryPanel);
            _harvestLabel = UiBuilder.Label("Label", _harvestRow, TextStyle.PlainBold, UiText.Harvest, 20f, Palette.FamilyWood.Base, TextAlignmentOptions.MidlineRight);
            _harvestSeed = UiBuilder.Image("Seed", _harvestRow, art.IconSeed, Color.white);
            _harvestSeed.preserveAspect = true;
            _harvestValue = UiBuilder.Label("Value", _harvestRow, TextStyle.PlainBold, "+0", 30f, Palette.FamilyWood.Deep, TextAlignmentOptions.MidlineLeft);

            // Cosecha doble (04 §2.8; rv_double_harvest, desde el 11): CTA de agua con el icono de video, como «Canoa extra · Ver
            // video» (el agua es la familia de los videos); «+20 → +40» bajo la etiqueta (02 §6).
            _double = UiBuilder.Relief("DoubleHarvestButton", _victoryPanel, art.CtaWater, () => _actions.PressDoubleHarvest());
            ModalKit.StretchForTextScale(_double);
            _doubleGroup = _double.Face.parent.gameObject.AddComponent<CanvasGroup>();
            _doubleGroup.blocksRaycasts = false;
            _doubleIcon = UiBuilder.Image("Video", _double.Face, art.IconVideo, Color.white);
            _doubleIcon.preserveAspect = true;
            _doubleLabel = UiBuilder.Label("Label", _double.Face, TextStyle.CtaWater, UiText.DoubleHarvest, 17f, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.AutoSize(_doubleLabel, 13f, 17f);
            _doubleSub = UiBuilder.Label("Gain", _double.Face, TextStyle.CtaWater, string.Empty, 14f, Palette.Cream, TextAlignmentOptions.Center);
            _double.Relief.Bind(_double.Face, _double.Base, _double.FaceImage, art.CtaWater, null, null, _doubleLabel);
            _double.Button.gameObject.SetActive(false);
            _ribbon = UiBuilder.Image("Ribbon", _victoryPanel, art.Ribbon, Color.white);
            // Baloo 2 de 30 dp, mínimo 22 (revisión visual del 2026-10-07; guía 5.5), en la franja de texto de la cinta de
            // 344 × 72 (≈ 262 × 45 dp de hoja_base → hoja_fondo). En español cabe siempre en una línea a 22 o más; si no cabe a
            // 22 (× la escala de texto), dos líneas y la hoja más alta (LayoutRibbonTitle; guía 5.5; tarea L4).
            _ribbonTitle = UiBuilder.Label("Title", _ribbon.transform, TextStyle.Ribbon, UiText.LevelComplete, RibbonTitleSp, Palette.Cream, TextAlignmentOptions.Center);
            UiBuilder.Place(_ribbonTitle.rectTransform, 44f, 15f, RibbonWidthDp - 88f, 42f);
            UiBuilder.AutoSize(_ribbonTitle, RibbonTitleMinSp, RibbonTitleSp);
            _ribbonTitleMinDp = _ribbonTitle.fontSizeMin;
            _ribbonTitleMaxDp = _ribbonTitle.fontSizeMax;
            _victoryOverlay.gameObject.SetActive(false);

            // Caja de TiTino en la victoria (TI6a; propuesta de TiTino d.4): lo que cree ahora queda delante de la victoria y
            // detrás del embudo, la pausa y la capa superior. Sin raycast.
            SlothBoxBuild();

            // ---- Embudo de D-008 (B2-2 y B2-3): ¿Seguro?, Nivel fallido y Sin canoas, con el tablero atenuado detrás. ----
            _giveUp = new GiveUpConfirm(_root, art, actions);
            _failed = new LevelFailedScreen(_root, art, actions);
            _noCanoes = new NoCanoesPanel(_root, art, () => _actions.PressExtraCanoeVideo(), () => _actions.PressRefillCanoes(),
                () => _actions.PressInfiniteCanoes(), () => _actions.PressNoCanoesMap(), () => _actions.PressNoCanoesClose());

            // ---- Capa superior: pausa (02 §5: izquierda de la fila superior; disco de 40 dp) y saldos del embudo (D-008). ----
            _topLayer = UiBuilder.Rect("TopLayer", _root);
            _pauseButton = UiBuilder.Relief("PauseButton", _topLayer, art.DiscWood, OnPause);
            RectTransform pauseVisual = (RectTransform)_pauseButton.Face.parent;
            pauseVisual.anchorMin = new Vector2(0.5f, 1f);
            pauseVisual.anchorMax = new Vector2(0.5f, 1f);
            pauseVisual.sizeDelta = new Vector2(art.DiscWood.Painted ? art.DiscWood.HeightDp : 40f, art.DiscWood.HeightDp);
            pauseVisual.anchoredPosition = new Vector2(0f, -4f);
            Image pauseIcon = UiBuilder.Image("Icon", _pauseButton.Face, art.IconPause, Color.white);
            UiBuilder.Place(pauseIcon.rectTransform, 7f, 7f, 26f, 26f);
            _pauseButton.Relief.Bind(_pauseButton.Face, _pauseButton.Base, _pauseButton.FaceImage, art.DiscWood, pauseIcon, null, null);
            _walletBar = new WalletBar(_topLayer, art);
            _pause = new PauseMenu(_root, art, OnResume, OnPauseMap, OnPauseRestart, OnPauseQuit, OnPauseSkip);
            _confirm = new QuitConfirm(_root, art, OnConfirm, OnConfirmCancel, OnConfirmUndo);
            _banner = new Toast("RiverLowered", _root, art, 18f);
            _toast = new Toast("Toast", _noticeLayer, art, 15f);
            _sighting = new SightingCard(_noticeLayer, art); // «¡Miquito a la vista!» (guion de la intro §4.5)
            _fruitNew = new FruitNewCard(_noticeLayer, art); // «Fruta nueva» (06 §4.5; F5; GameScreen.Notices.cs)
            _stopLabel = new StopLabel(_noticeLayer, art); // rótulo del lugar (M2.3; GameScreen.Notices.cs)

            // Captura de toques para saltar Sumergir y teñir o la celebración (02 §4.2, §2): por encima de todo.
            _skipCatcher = HitLayer("SkipCatcher", _root);
            _skipCatcher.gameObject.AddComponent<PointerDownRelay>().Bind(() => _actions.TapAnywhere());
            _skipCatcher.gameObject.SetActive(false);

            _animator = new BoardAnimator(_streamLayer, _pourFront) { Fx = _fx, DevEvent = _probe.Event };

            // Ganchos de construcción de los parciales: al final, con todo creado (también la bandeja) y antes de la primera
            // maquetación, para que sus Layout* ya encuentren lo que crean.
            LabelsBuild();
            CardsBuild();
            RevealBuild();
            HintBuild();
            ShuffleBuild();
            ArrivalBuild();
            StreakBuild(); // la barra de la Crecida del HUD y las barras de la victoria (GameScreen.Streak.cs; B1b)
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

        /// <summary>
        /// Capa de la cuia elevada cuyo cielo entra en la fila del HUD (tableros de 3 filas): delante del HUD y detrás de la capa
        /// de vertido y de la pausa (tests de orden de capas).
        /// </summary>
        internal RectTransform LiftFront => _liftFront;

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

        /// <summary>«Llenar y teñir»: la intro de carga (tests y capturas).</summary>
        public IntroAnimation Intro => _intro;

        /// <summary>
        /// Agua base con la que arrancó la última intro: la del mundo del nivel (<c>water_base</c>; 07 §2.1; glosario §11),
        /// no la del primer paquete del catálogo (tests).
        /// </summary>
        internal Color IntroWaterColor { get; private set; }

        /// <summary>Capa de mecánicas detrás de las cuias (arquitectura del paso 1, §6; tests de orden de capas).</summary>
        internal RectTransform MechanicBackLayer => _mechanicBackLayer;

        /// <summary>Capa de mecánicas delante de las cuias y detrás de la intro, los chorros y los efectos (tests).</summary>
        internal RectTransform MechanicFrontLayer => _mechanicFrontLayer;

        /// <summary>Tropa de miquitos de la intro (tests y capturas).</summary>
        public TroopLayer Troop => _troop;

        /// <summary>
        /// Catálogo de miquitos (<c>af-fauna/0</c>; null = siempre dosel). La especie de la intro la resuelve
        /// <see cref="IntroCast.Resolve"/> con el mundo, el nivel y el intento (guion de la intro §4.3).
        /// </summary>
        public FaunaCatalog? Fauna { get; set; }

        /// <summary>Especie de la intro en curso o de la última (null = dosel).</summary>
        public FaunaSpecies? IntroSpecies => _introSpecies;

        /// <summary>Veces que se saltó la intro desde que existe la pantalla (<c>af_anim_skipped</c> con <c>anim = tint</c>).</summary>
        public int AnimSkippedCount => _animSkipped;

        /// <summary><c>at_ms</c> del último salto de la intro (−1 si no hubo).</summary>
        public int LastAnimSkipAtMs => _lastSkipAtMs;

        /// <summary>
        /// <c>duration_cfg_ms</c> del último <c>af_anim_skipped</c>: la duración configurada de la animación (1.400, 800 o 250),
        /// sin el cartel «El río bajó».
        /// </summary>
        public int LastAnimSkipCfgMs => _lastSkipCfgMs;

        /// <summary>Ambiente de la escena (presupuesto de partículas).</summary>
        public AmbientLayer Ambient => _ambient;

        /// <summary>Estallido de la victoria (presupuesto de partículas, VA-13).</summary>
        public ConfettiLayer Burst => _confetti;

        /// <summary>
        /// Capi está en el muelle durante la partida: nunca (D-016, T5-B; 02 §5, «Mascotas durante la partida»: ninguna mascota
        /// fija en el muelle ni en la orilla).
        /// </summary>
        public bool CapiVisible => false;

        /// <summary>Rectángulos decorativos del muelle (el poste), para comprobar que no tocan zonas táctiles (VA-08).</summary>
        public RectTransform[] DecorRects => new[] { _post.rectTransform };

        /// <summary>Botón de Deshacer de la bandeja.</summary>
        public Button UndoButton => _tray.UndoButton;

        /// <summary>Botón «+1 cuia» de la bandeja (desde el nivel 8).</summary>
        public Button ExtraCuiaButton => _tray.ExtraCuiaButton;

        /// <summary>Bandeja de comodines (tests).</summary>
        internal BoosterTray Tray => _tray;

        /// <summary>Modal Aguas estancadas (tests y capturas).</summary>
        internal StuckModal Stuck => _stuck;

        /// <summary>¿Seguro? (tests).</summary>
        internal GiveUpConfirm GiveUp => _giveUp;

        /// <summary>Nivel fallido (tests).</summary>
        internal LevelFailedScreen Failed => _failed;

        /// <summary>Sin canoas (tests).</summary>
        internal NoCanoesPanel NoCanoes => _noCanoes;

        /// <summary>Tarjeta del Reto del río de la victoria del 7 (E6; null hasta esa victoria; tests).</summary>
        internal DailyTeaserCard? DailyTeaser => _dailyTeaser;

        /// <summary>Menú de pausa (tests).</summary>
        internal PauseMenu Pause => _pause;

        /// <summary>Diálogo de confirmación (tests).</summary>
        internal QuitConfirm Confirm => _confirm;

        /// <summary>Saldos arriba durante el embudo (tests).</summary>
        internal WalletBar WalletBar => _walletBar;

        /// <summary>Botón de pausa.</summary>
        public Button PauseButton => _pauseButton.Button;

        /// <summary>El modal Aguas estancadas está abierto.</summary>
        public bool StuckVisible => _stuckOverlay.gameObject.activeSelf && _stuck.Panel.gameObject.activeSelf;

        /// <summary>Texto del cartel «El río bajó» si se ve (null si no).</summary>
        public string? BannerText => _banner.Visible ? _banner.Text : null;

        /// <summary>Tarjeta «¡Miquito a la vista!» (tests).</summary>
        internal SightingCard Sighting => _sighting;

        /// <summary>Texto del aviso en pantalla (regalo de Capi, rescate, «+1 Luciérnaga», «Te faltan»…), o null.</summary>
        public string? ToastText => _toast.Visible ? _toast.Text : null;

        /// <summary>Hay un relayout de cuia añadida en curso.</summary>
        public bool Relayouting => _relayoutMs >= 0f;

        /// <summary>Botón de Reiniciar del HUD.</summary>
        public Button RestartButton => _restart.Button;

        /// <summary>Botón Siguiente de la victoria.</summary>
        public Button NextButton => _next.Button;

        /// <summary>Botón «Inicio» de la victoria [P].</summary>
        public Button HomeButton => _home.Button;

        /// <summary>Botón «Cosecha doble: x2 por video» de la victoria (desde el 11).</summary>
        public Button DoubleHarvestButton => _double.Button;

        /// <summary>La victoria ofrece la Cosecha doble (hay video y aún no se usó).</summary>
        public bool DoubleHarvestShown => _victoryOverlay.gameObject.activeSelf && _double.Button.gameObject.activeSelf;

        /// <summary>Cosecha que se ve en la fila de la victoria (cuenta hacia arriba; −1 si aún no se pintó).</summary>
        public int HarvestShown => _harvestShown;

        /// <summary>
        /// La victoria espera, tras la Cosecha doble, a pasar sola al nivel siguiente (D-011, punto 4): Inicio y la Cosecha doble
        /// están inactivos y Siguiente puede adelantarlo. Sigue en true con la espera vencida, hasta que
        /// <see cref="TakeAutoNext"/> la consume.
        /// </summary>
        internal bool AutoNextPending => _autoNextMs >= 0f;

        /// <summary>Texto de la fila de la Cosecha (p. ej. «+20»).</summary>
        public string HarvestText => _harvestValue.text;

        /// <summary>Texto «+20 → +40» del botón de la Cosecha doble.</summary>
        public string DoubleHarvestGainText => _doubleSub.text;

        /// <summary>La pantalla de victoria está a la vista.</summary>
        public bool VictoryVisible => _victoryOverlay.gameObject.activeSelf;

        /// <summary>Alto del panel de victoria (dp; tests).</summary>
        internal float VictoryPanelHeight => _victoryPanelDp;

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

            if (!visible)
            {
                SlothLeave(); // Inicio o «El río sigue creciendo»: TiTino no se queda colgado de una victoria que ya no se ve
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

        /// <summary>Asocia el flujo y los servicios del embudo (antes de cargar el primer nivel).</summary>
        public void Bind(LevelFlow flow, GameServices? services = null)
        {
            _flow = flow;
            _services = services;
            _tray.Bind(flow, services);
        }

        /// <summary>Avanza la vista <paramref name="dtMs"/> milisegundos.</summary>
        public void Tick(float dtMs)
        {
            ReadScreen(out int sw, out int sh, out Rect safe);
            if (sw != _screenW || sh != _screenH || safe != _safeArea)
            {
                ApplyScreenMetrics(false);
            }

            if (_intro.Active && !_introHeld)
            {
                _intro.Tick(dtMs); // retenida detrás de la tarjeta previa, se queda quieta en t = 0
            }

            _animator.Tick(dtMs);
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].Tick(dtMs);
            }

            SyncLiftFront();
            if (_confettiDelay >= 0f)
            {
                _confettiDelay -= dtMs;
                if (_confettiDelay < 0f)
                {
                    _confettiDelay = -1f;
                    // Tras el estallido, ≤ 12 hojas y pétalos caen despacio hasta el CTA de la victoria.
                    _confetti.Play(GameConfig.AnimConfettiMaxParticles, PanelRect(), _canvasW, _victoryPanelY + _victoryCtaTopDp);
                    AF.Audio.AudioCues.Play(AF.Audio.SfxId.Confetti); // AU1
                }
            }

            _confetti.Tick(dtMs);
            _fx.Tick(dtMs);
            _ambient.Tick(dtMs);
            TickBoardShift(dtMs);
            TickPanels(dtMs);
            TickIntroDelay(dtMs);
            TickRelayout(dtMs);
            _banner.Tick(dtMs);
            _toast.Tick(dtMs);
            _dailyTeaser?.Tick(dtMs);
            _sighting.Tick(dtMs);
            _fruitNew.Tick(dtMs);
            _stopLabel.Tick(dtMs);
            _slothNotice?.Tick(dtMs); // aviso de TiTino tras Volver a la orilla (TI6b; GameScreen.Notices.cs)
            _failed.Tick(dtMs); // TiTino en Nivel fallido (TI6b)
            TickNotices(); // el siguiente aviso, si el anterior se acaba de ocultar (GameScreen.Notices.cs)
            TickHarvest(dtMs);
            TickAutoNext(dtMs);
            StreakTick(dtMs); // las barras de la victoria y la espera de 250 ms tras Siguiente (B1b)
            RefreshHud();
            LabelsTick(dtMs);
            CardsTick(dtMs);
            RevealTick(dtMs);
            HintTick(dtMs);
            ShuffleTick(dtMs);
            StopTick(dtMs);
            ArrivalTick(dtMs);
            SlotTick(dtMs);
            SlothTick(dtMs);
            PlaceToast(false); // con la bandeja ya repintada: el aviso no tapa sus burbujas «Te faltan {n}» (GameScreen.Notices.cs)
        }

        /// <summary>Vuelta de segundo plano (02 §14): la vista salta al modelo sin animar.</summary>
        public void SnapToModel()
        {
            _animator.SnapAll();
            _intro.Clear();
        }

        /// <inheritdoc />
        public void OnLevelLoaded(int level)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            _intro.Clear();
            _confetti.Stop();
            _fx.Clear();
            _confettiDelay = -1f;
            HideOverlays();
            _banner.Hide();
            ClearNotices(); // ningún aviso ni tarjeta del nivel anterior pasa a este (GameScreen.Notices.cs)
            StopLabelLevelLoaded(level);
            _attemptBanner = false;
            _introDelayMs = -1f;
            StreakLevelLoaded(level); // fuera la barra del HUD (sale en T) y las de la victoria anterior (B1b)
            BuildBoard(flow);
            _levelText.text = UiText.Level(level);
            FitLevelBadge();
            AudioBoard.LevelLoaded(flow.Level, flow.Session.Current); // AU1-AU2: el lecho del mundo (nocturno en Muy difícil) y su ambiente
            _pressPreview = -1;
            _shownBoard = flow.Session.Current;
            _probe.State("load", flow, int.MinValue, null);

            // AF_LAYOUT level, después de los ganchos (T-A de la prueba del 2026-10-09): antes salía antes que ellos, sin
            // «+1 cuia» (la bandeja aún no se había refrescado) y con Marea en el centro de una bandeja de 1 ranura; la
            // corregía otra un fotograma después. Ahora sale una sola, con la bandeja refrescada (EmitLevelLayout).
            _levelLayoutDeferred = true;
            LabelsLevelLoaded(flow);
            CardsLevelLoaded(flow);
            RevealLevelLoaded(flow);
            HintLevelLoaded(flow);
            ShuffleLevelLoaded(flow);
            StopLevelLoaded(flow);
            ArrivalLevelLoaded(flow);
            _levelLayoutDeferred = false;
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
            _animator.AdoptBoard(_shownBoard); // la intro pinta el tablero nuevo (también tras Volver a la orilla)
            _introRetry = isShort;
            _introAnimStarted = false;
            _streakBar.SetVisible(false); // la barra de la Crecida no está durante la intro (07 §7.4; D-030, pregunta 4 (c))
            if (_introDelayMs > 0f)
            {
                // Variante aliviada (04 §2.7): el cartel «El río bajó» va antes del teñido; la intro arranca al terminar.
                _introPendingMs = Mathf.Max(0, durationMs - (int)_introDelayMs);
            }
            else
            {
                StartIntro(flow, durationMs, isShort);
                if (isShort && !flow.IsDaily) // E21: el Reto no enseña «Nivel N» (06 §4.1: cabecera «Reto del río»)
                {
                    // 02 §2 (INTRO): «En reintento muestra Nivel N · intento k», con k = fails_level + 1, como el rótulo
                    // «Level N» que baja al reintentar en Magic Sort (ficha §4.1). Reintentar desde Nivel fallido, Volver a la
                    // orilla (cabecera, pausa o pie del modal en 1-30); la variante aliviada lleva su cartel «El río bajó».
                    _attemptBanner = true;
                    _banner.Place(_canvasW, TopRowY + 64f);
                    _banner.Show(UiText.LevelAttempt(flow.LevelNumber, flow.Attempt.FailsLevel + 1), Mathf.Max(durationMs, AttemptBannerMs));
                }
            }

            _skipCatcher.gameObject.SetActive(true);
            if (_restartProbePending)
            {
                _restartProbePending = false;
                _probe.State("restart", flow, int.MinValue, null);
            }

            // El tablero del intento ya es el de la sesión (OnRestart avisa antes de reiniciarla: ahí aún es el viejo).
            LabelsBoardChanged(flow);
            RevealBoardChanged(flow);
            ShuffleBoardChanged(flow);
        }

        /// <inheritdoc />
        public void OnIntroFinished(bool skipped)
        {
            _introDelayMs = -1f;
            if (!_attemptBanner)
            {
                _banner.Hide(); // el rótulo del intento se apaga solo (AttemptBannerMs)
            }

            if (skipped)
            {
                // af_anim_skipped (08 §8.2) con anim = tint, at_ms y duration_cfg_ms; lo enviará AF.Analytics cuando exista. Si el
                // salto llega durante el cartel «El río bajó», la intro aún no ha empezado: at_ms = 0 (no el tiempo de la intro
                // anterior); duration_cfg_ms es siempre la duración configurada de la animación, sin el cartel.
                _animSkipped++;
                _lastSkipAtMs = _introAnimStarted ? Mathf.RoundToInt(_intro.TimeMs) : 0;
                _lastSkipCfgMs = _introAnimStarted ? _introCfgMs : _introPendingMs;
                LevelFlow f = RequireFlow();
                _probe.AnimSkipped("tint", f, _lastSkipAtMs, _lastSkipCfgMs);
            }

            _intro.Complete(skipped);
            // Con el toque habilitado salen los avisos que esperaban (D-011, punto 2): el regalo de Capi, «+1 Luciérnaga»…; la
            // tarjeta del miquito queda pendiente hasta que la tropa se vaya (GameScreen.Notices.cs).
            LevelFlow ready = RequireFlow();
            MaybeShowTutorial(ready); // la instrucción del tutorial con TiTino en 1, 2, 3 y 6 (TU1a), la primera de la cola
            MaybeShowTinajaTip(ready); // el consejo del segundo tablero del estreno de la Tinaja (324 [P]; TJ4)
            MaybeShowFruitNew(ready); // «Fruta nueva» en el estreno de un tinte (F5), antes que el miquito y el rótulo
            MaybeShowSighting(ready);
            ShowRevealGiftIfReady(ready);
            MaybeShowStopLabel(ready); // el rótulo del lugar, si el nivel no trae otra tarjeta (M2.3)
            MaybeShowSlothRestart(ready); // tras Volver a la orilla en 1-30, el aviso de TiTino (TI6b)
            StreakTouchEnabled(ready); // la barra de la Crecida sale en T y, tras un fallo cubierto, el aviso de la hoja (B1b)
            PumpNotices();
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
                    AudioBoard.Select(flow.Session.Current, result.Selection); // AU1: la nota de la fruta de arriba
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

                    AF.Audio.AudioCues.Play(AF.Audio.SfxId.Illegal); // AU1: madera neutra, nunca háptico en un rechazo (02 §4)
                    _probe.State("reject", flow, result.Tapped, null);
                    break;

                case TapKind.Poured:
                    SetLift(result.PreviousSelection, false);
                    EnqueuePour(flow, result.Pour);
                    AudioBoard.Music(flow.Level, flow.Session.Current); // AU1-AU2: una capa de percusión más al sellar
                    _probe.State("pour", flow, result.Tapped, result.Pour);
                    LabelsBoardChanged(flow);
                    RevealBoardChanged(flow);
                    ShuffleBoardChanged(flow);
                    break;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Marea (04 §2.1; 02 §14): la cola salta a su final y las dosis vuelven de la cuia tocada al origen en 250 ms con la
        /// tapa que salta. Si la cuia tocada no cambió (vertido por un Remolino), no hay traslado: el origen y la pareja saltan
        /// al estado nuevo [P] (Magic Sort no se observó; paso 2). Después, las demás cuias que difieren del tablero que se
        /// veía (la gota, el mordisco, la Liana que se vuelve a atar…) saltan a su estado.
        /// </remarks>
        public void OnUndo(Pour undone, bool fromStuckModal)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            ClearLifts();
            HideOverlays(true);
            BoardState before = _shownBoard ?? flow.Session.Current;
            BoardState after = flow.Session.Current;
            int from = undone.Target;
            int to = undone.Source;
            int moved = 0;
            if (before.Count == after.Count && (uint)from < (uint)after.Count && (uint)to < (uint)after.Count
                && (before[from].Packed != after[from].Packed || before.IsSealed(from) != after.IsSealed(from)))
            {
                _animator.EnqueueUndo(
                    from,
                    to,
                    undone.Amount,
                    before[from].TopColor,
                    before[from],
                    after[from],
                    after.IsSealed(from),
                    before[to],
                    after[to],
                    after.IsSealed(to));
                moved = (1 << from) | (1 << to);
            }

            int others = BoardDiff.ChangedOthers(before, after, -1, -1) & ~moved;
            for (int i = 0; i < _views.Length && i < after.Count; i++)
            {
                if (((others >> i) & 1) != 0)
                {
                    _animator.ShowContent(i, after[i], after.IsSealed(i));
                }
            }

            _shownBoard = after;
            _probe.State("undo", flow, int.MinValue, undone);
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.Undo); // AU1: Marea
            AudioBoard.Music(flow.Level, after); // AU1-AU2: la Marea puede quitar una capa
            LabelsBoardChanged(flow);
            RevealBoardChanged(flow);
            ShuffleBoardChanged(flow);
        }

        /// <inheritdoc />
        public void OnRestart(bool fromStuckModal)
        {
            _animator.SnapAll();
            ClearLifts();
            HideOverlays();
            ClearNotices(); // otro intento: los avisos del anterior ya no valen
            _shownBoard = RequireFlow().Session.Current;
            _restartProbePending = true;
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.Restart); // AU1: Volver a la orilla
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
            _probe.Funnel("rescue_check", flow);
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
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.WinToada); // AU1: la toada y el háptico medio de la victoria
            AF.Audio.AudioCues.Haptic(AF.Audio.HapticKind.Win);
            _skipCatcher.gameObject.SetActive(true);
            // Ningún aviso encima de la celebración ni de la victoria (D-011, punto 2): fuera el que se vea (el regalo de Capi
            // dura 5,5 s; la tarjeta del miquito, unos 5) y lo que esperaba turno (GameScreen.Notices.cs).
            ClearNotices();
        }

        /// <inheritdoc />
        public void OnVictoryScreen()
        {
            LevelFlow flow = RequireFlow();
            _skipCatcher.gameObject.SetActive(false);
            _ambient.SetSuspended(true);
            _victoryOverlay.gameObject.SetActive(true);
            SetVictoryLights(true);
            StreakVictoryPlan(flow); // las barras del panel único (D-033, punto 10; GameScreen.Streak.cs): antes de colocar la victoria

            // 02 §6: la Cosecha (ya acreditada en el instante de la victoria) cuenta desde 0; desde el 11, Cosecha doble si hay
            // video (rv_double_harvest: reglas, tope, cooldown y fill; sin anuncio no aparece).
            _doubleShown = flow.DoubleHarvestOffered && _services != null && _services.RewardedReady(AdPlacement.DoubleHarvest);
            if (_doubleShown)
            {
                _doubleSub.text = UiText.DoubleHarvestGain(flow.HarvestBase);
            }

            VictoryTitleShown(flow); // «¡Perfecto!» o el de siempre (DC14), antes de que LayoutVictory mida el título
            LayoutVictory();
            StartHarvestCount(0, flow.HarvestSeeds, HarvestCountDelayMs);
            ShiftBoard(ShiftToClear(_victoryPanelY - RibbonRiseDp, 8f), true);
            _victoryAnim = 0f;
            ApplyVictoryAnim(0f);
            SyncChrome();
            StreakVictoryStarted(); // las barras empiezan a contar y saben adónde vuela su premio (B1b)
            _probe.Victory("victory", flow, _doubleShown);
            LabelsVictoryShown(flow);

            // Victoria del Mapa del río y de TiTino (arquitectura de la tanda 4, §2), en este orden: TiTino mide su caja (la franja
            // libre entre la cabecera y la cima de las cuias tras ShiftToClear) antes de que la ranura pida su línea al árbitro,
            // que necesita SlothCanSpeak; después, el fondo de la parada y la llegada.
            VictoryFreeBand(out float bandTop, out float bandBottom);
            SlothVictoryShown(flow, bandTop, bandBottom);
            StreakYieldToSloth(flow, ref bandTop, ref bandBottom); // en su hito, sin barras si con ellas no cabe su frase (B1b-Q10 (c))
            SlotVictoryShown(flow);
            StopVictoryShown(flow);
            ArrivalVictoryShown(flow);

            // Reto del río (E6; 06 §2.2, nivel 7; 02 §13): la tarjeta de 2 s «Desde el nivel 10, un tablero nuevo cada día», sobre
            // las cuias y bajo la cinta, sin toques; no cambia nada de esta victoria. Solo con el Reto en el build (D-008).
            if (flow.LevelNumber == DailyTeaserCard.TeaserLevel || _dailyTeaser != null)
            {
                _dailyTeaser ??= new DailyTeaserCard(_victoryOverlay, _art);
                bool inBuild = GameBootstrap.Current != null && GameBootstrap.Current.DailyInBuild;
                // La cima de las cuias ya desplazadas, no bandBottom: con la victoria con vista (DC2), bandBottom es el pie de la
                // caja de TiTino, pegado a la cinta, y la tarjeta la pisaría.
                float cuiasTop = Mathf.Max(bandTop, CuiasTopDp() - _boardShiftTarget);
                _dailyTeaser.Show(flow.LevelNumber, inBuild, _canvasW, bandTop, cuiasTop, _victoryPanelY - RibbonRiseDp);
            }
        }

        /// <inheritdoc />
        public void OnHarvestDoubled(int totalSeeds)
        {
            LevelFlow flow = RequireFlow();
            int from = _harvestShown < 0 ? flow.HarvestBase : _harvestShown;
            _doubleShown = false;

            // D-011, punto 4: la fila cuenta de +N a +2N, se ve un momento más y la victoria pasa sola al nivel siguiente por el
            // camino de Siguiente (TickAutoNext y TakeAutoNext). Mientras espera, Inicio y la Cosecha doble quedan inactivos
            // (LayoutVictory); Siguiente puede adelantarlo y entonces AD_GATE cancela la espera (OnAdGate).
            _autoNextMs = GameConfig.HarvestCountMs + DoubleHarvestAdvanceMs;
            LayoutVictory();
            ShiftBoard(ShiftToClear(_victoryPanelY - RibbonRiseDp, 8f), true);
            if (_victoryAnim >= 0f)
            {
                ApplyVictoryAnim(_victoryAnim);
            }
            else
            {
                ApplyVictoryAnim(PanelSlideMs + 1000f);
            }

            StartHarvestCount(from, totalSeeds, 0f);
            _probe.Victory("double_harvest", flow, false);
        }

        /// <inheritdoc />
        public void OnAdGate()
        {
            // AD_GATE (02 §2): Siguiente, Inicio y la Cosecha doble dejan de responder; el intersticial, si toca, va encima. Si
            // Siguiente adelantó el avance tras la Cosecha doble, la espera se cancela: N + 1 se carga una sola vez (D-011).
            _autoNextMs = -1f;
            _nextHoldMs = -1f;
            _victoryBars.Complete(); // nada de las barras queda a medias tras Siguiente (D-033, punto 9)
            UiBuilder.SetEnabled(_next.Button, false);
            UiBuilder.SetEnabled(_home.Button, false);
            UiBuilder.SetEnabled(_double.Button, false);
            LevelFlow flow = RequireFlow();
            _probe.Victory("ad_gate", flow, false);
            SlotAdGate(flow);
            ArrivalAdGate(flow);
        }

        /// <inheritdoc />
        public void OnResumed(FlowPhase phase)
        {
            // 02 §14: LOADING del mismo nivel desde el estado guardado, sin intro; el tablero ya se construyó con el contenido
            // de ahora (OnLevelLoaded) y se reabre la pantalla que estaba abierta.
            LevelFlow flow = RequireFlow();
            _intro.Clear();
            _skipCatcher.gameObject.SetActive(false);
            if (IsFunnelModal(phase))
            {
                HideNoticesForModal();
            }

            if (phase == FlowPhase.GiveUpConfirm)
            {
                ShowFunnelBackdrop();
                FitBoardToStuck(false);
                FunnelModalShowing(phase, flow);
                _giveUp.Show(StuckModalModel.BuildGiveUpConfirm(StuckContext(flow)), _canvasW, _canvasH);
                EmitFunnelLayout(GiveUpConfirmLayout);
            }
            else if (phase == FlowPhase.LevelFailed)
            {
                ShowFunnelPhase(FlowPhase.LevelFailed, true); // también publica su AF_LAYOUT; sin el remate (no acaba de pasar)
            }

            SyncChrome();
            _probe.Funnel("resume", flow);
            if (phase == FlowPhase.Playing)
            {
                _probe.State("ready", flow, int.MinValue, null);
            }

            StreakResumed(flow, phase); // reabierto sin intro: la barra de la Crecida ya está (B1b)
            LabelsBoardChanged(flow);
            RevealBoardChanged(flow);
            ShuffleBoardChanged(flow);
        }

        /// <inheritdoc />
        public void OnStuckModalOpened()
        {
            HideNoticesForModal(); // pregunta 42 (b): el aviso que se veía se oculta y no vuelve (GameScreen.Notices.cs)
            _animator.SnapAll();
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.Stuck); // AU1: Aguas estancadas, con el lecho atenuado
            AF.Audio.AudioCues.SetModalDuck(true);
            _ambient.SetSuspended(true);
            _stuckOverlay.gameObject.SetActive(true);
            _stuck.Panel.gameObject.SetActive(true);
            // balsa_ok se congela al abrir (04 §6.6: lo mostrado no cambia con el modal abierto); la impresión cuenta para el tope.
            // Si la Balsa ya se enseñó en este bloqueo (el modal reabierto al volver a la app: 02 §14 y D-008, «se reabre tal
            // cual»), se sigue ofreciendo sin contar otra impresión. Con el perfil en solo lectura no se ofrecen compras.
            LevelFlow flow = RequireFlow();
            _stuckBalsaOk = flow.BalsaShownThisBlock
                || (_services != null && !_services.PurchasesBlocked && _services.StoreLedger.CanOfferBalsa(flow.LevelNumber));
            ShowStuckView(true);
            FitBoardToStuck(true); // 07 §5.5: las cuias siguen visibles sobre el modal
            _stuckAnim = 0f;
            ApplyStuckAnim(0f);
            SyncChrome();
            _probe.Funnel("stuck_modal", RequireFlow(), _services?.StoreLedger, _stuckBalsaOk);
        }

        /// <inheritdoc />
        public void OnCuiaAdded(int index, AddedCuiaKind kind)
        {
            LevelFlow flow = RequireFlow();
            if (kind == AddedCuiaKind.Rescue)
            {
                HideOverlays(true);
                ShowToast(UiText.RescueApplied);
            }

            AddCuiaView(flow, index);
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.CapiPaddle); // AU1: llega Capi con la cuia (extra o de rescate)
            AF.Audio.AudioCues.Haptic(AF.Audio.HapticKind.Capi);
            SyncChrome();
            _probe.Funnel(kind == AddedCuiaKind.Rescue ? "rescue" : "extra_cuia", flow);
            LabelsBoardChanged(flow);
            RevealBoardChanged(flow);
            ShuffleBoardChanged(flow);
        }

        /// <inheritdoc />
        public void OnExtraCuiaGifted(int units)
        {
            // Llega al cargar el 8 (LOADING): el aviso de Capi espera en la cola y sale al habilitarse el toque (D-011, punto 2).
            ShowToast(UiText.ExtraCuiaGift);
            _probe.Funnel("extra_cuia_gift", RequireFlow());
        }

        /// <inheritdoc />
        public void OnFunnelPhase(FlowPhase phase)
        {
            ShowFunnelPhase(phase, false);
        }

        // resumed: la pantalla se reabre desde el estado guardado (OnResumed), sin el remate de Nivel fallido, que solo suena
        // en la transición real (lista-de-sonidos §9: una vez al abrirse).
        private void ShowFunnelPhase(FlowPhase phase, bool resumed)
        {
            LevelFlow flow = RequireFlow();
            _pause.Hide();
            _confirm.Hide();
            if (IsFunnelModal(phase))
            {
                HideNoticesForModal(); // pregunta 42 (b): nada del aviso que se veía detrás del modal (GameScreen.Notices.cs)
            }

            switch (phase)
            {
                case FlowPhase.GiveUpConfirm:
                    _stuck.Panel.gameObject.SetActive(false);
                    FunnelModalShowing(phase, flow);
                    _giveUp.Show(StuckModalModel.BuildGiveUpConfirm(StuckContext(flow)), _canvasW, _canvasH);
                    EmitFunnelLayout(GiveUpConfirmLayout);
                    _probe.Funnel("giveup_confirm", flow);
                    break;

                case FlowPhase.LevelFailed:
                    if (!resumed && !_failed.Visible && !_noCanoes.Visible)
                    {
                        AF.Audio.AudioCues.Stinger(AF.Audio.StingerId.LevelFailed); // AU3: el remate, una vez al abrirse (no al volver de Sin canoas)
                    }

                    ShowFunnelBackdrop();
                    _giveUp.Hide();
                    _noCanoes.Hide();
                    FunnelModalShowing(phase, flow);
                    _failed.Show(flow.LevelNumber, LevelReward(flow), _canvasW, _canvasH);
                    EmitFunnelLayout(LevelFailedLayout);
                    _probe.Funnel("level_failed", flow);
                    break;

                case FlowPhase.NoCanoes:
                    _failed.Hide();
                    _noCanoes.Show(_services, _canvasW, _canvasH);
                    EmitFunnelLayout(NoCanoesLayout);
                    _probe.Funnel("no_canoes", flow);
                    break;

                default:
                    _noCanoes.Hide();
                    if (phase == FlowPhase.Playing)
                    {
                        HideOverlays(true);
                    }

                    break;
            }

            SyncChrome();
        }

        /// <inheritdoc />
        public void OnFailResolved(FailOutcome outcome)
        {
            if (outcome.CanoeLost)
            {
                AF.Audio.AudioCues.Play(AF.Audio.SfxId.CanoeLost); // AU1
            }

            _probe.Funnel(outcome.Kind == FailKind.Hard ? (outcome.CanoeLost ? "fail_hard_canoe" : "fail_hard") : "fail_soft", RequireFlow());
            StreakFailResolved(RequireFlow()); // la Hoja escudo cubrió el fallo: su aviso, al volver a jugar (B1b)
        }

        /// <inheritdoc />
        public void OnRiverLowered(string reliefType, int bannerMs)
        {
            _introDelayMs = bannerMs;
            _attemptBanner = false;
            AF.Audio.AudioCues.Play(AF.Audio.SfxId.RiverDrop); // AU1: El río bajó
            _banner.Place(_canvasW, TopRowY + 64f);
            _banner.Show(reliefType == AF.Levels.ReliefBoard.TypeMinusColor ? UiText.RiverLoweredColor : UiText.RiverLoweredCuia, bannerMs);
            _probe.Funnel("river_lowered", RequireFlow());
        }

        /// <summary>
        /// Rearma el modal Aguas estancadas con el contexto de ahora (tras una compra o una recarga: mismo precio congelado,
        /// saldo nuevo; 04 §6.2 <c>PUÑADO_199</c>: «re-render modal with new saldo»).
        /// </summary>
        internal void ShowStuckView(bool layout)
        {
            LevelFlow flow = RequireFlow();
            if (layout)
            {
                _services?.PurchaseNotes.ClearFailures(); // F3-5: «No se pudo confirmar» es de la compra de antes, no de este bloqueo
            }

            StuckModalView view = StuckModalModel.Build(StuckContext(flow), flow.Boosters);
            _stuck.Show(view, _services, _canvasW, StuckRoomDp);
            _stuckSeedsShown = _services?.Wallet.Seeds ?? 0;
            _stuckNotesShown = _services?.PurchaseNotes.Version ?? 0;
            _stuckUndoShown = flow.UndoStatus.Stock + (flow.UndoStatus.FreeLeft * 1000);
            if (_services != null && _stuckBalsaOk && !flow.BalsaShownThisBlock)
            {
                for (int i = 0; i < view.Offers.Count; i++)
                {
                    if (view.Offers[i] == StuckOffer.Balsa)
                    {
                        // af_rescue_daily_cap: la impresión cuenta una vez por bloqueo; balsa_ok sigue congelado mientras esté
                        // abierto. La impresión y la marca de level_state van en la misma escritura del perfil.
                        using (flow.SaveBatch == null ? default : flow.SaveBatch.Begin())
                        {
                            _services.StoreLedger.RecordBalsaShown();
                            flow.MarkBalsaShown();
                        }

                        break;
                    }
                }
            }

            UiBuilder.SetEnabled(_stuck.UndoButton, view.Undo != StuckUndoKind.Undo || flow.CanUndo);
            _stuckPanelY = _canvasH - _safeBottom - _stuck.BottomGapDp - _stuck.Height;
            if (layout || _stuckAnim < 0f)
            {
                _stuck.Place(_canvasW, _stuckPanelY);
            }

            if (!layout)
            {
                // Rearmado con el modal abierto (tras una compra o una recarga): el alto del panel pudo cambiar (FX16a).
                FitBoardToStuck(true);
            }
        }

        /// <summary>
        /// Rearma Aguas estancadas con <paramref name="view"/> (otra rama del mismo bloqueo; tests de legibilidad), lo coloca en
        /// su sitio y ajusta el tablero a él como al abrirlo (<see cref="FitBoardToStuck"/>).
        /// </summary>
        internal void ShowStuckVariant(StuckModalView view)
        {
            _stuck.Show(view, _services, _canvasW, StuckRoomDp);
            _stuckPanelY = _canvasH - _safeBottom - _stuck.BottomGapDp - _stuck.Height;
            _stuck.Place(_canvasW, _stuckPanelY);
            FitBoardToStuck(false);
        }

        // Tablero atenuado detrás del embudo (Nivel fallido, y ¿Seguro? reabierto sin pasar por el modal).
        private void ShowFunnelBackdrop()
        {
            _animator.SnapAll();
            ClearLifts();
            _ambient.SetSuspended(true);
            _stuckOverlay.gameObject.SetActive(true);
            _stuck.Panel.gameObject.SetActive(false);
            _stuckDim.color = new Color(0.04f, 0.06f, 0.05f, 0.30f);
            _sceneVeil.color = new Color(0x6B / 255f, 0x6B / 255f, 0x6B / 255f, 0.20f);
        }

        private StuckContext StuckContext(LevelFlow flow)
        {
            bool undoAd = _services != null && _services.RewardedReady(AdPlacement.UndoRefill);
            StuckContext ctx = flow.BuildStuckContext(undoAd, _stuckBalsaOk, _services?.Geo ?? GeoTier.T3);
            // Sin compras con el perfil en solo lectura (08 §4.4) ni con la tienda que no vende (biz_iap_enabled, o el backend apagado o
            // ausente en este build: F3-5, que no las podría acreditar).
            ctx.PurchasesBlocked = _services != null && (_services.PurchasesBlocked || !_services.Store.IsAvailable);
            return ctx;
        }

        private int LevelReward(LevelFlow flow)
        {
            EconomyConfig config = _services?.Wallet.Config ?? EconomyConfig.Defaults;
            RewardTiers.TryParse(flow.Level.DifficultyLabel, out RewardTier tier);
            return config.WinReward(tier);
        }

        // 02 §3.1: la elevación empieza en el touch-down (0 ms percibidos) si, sin selección, la cuia puede ser origen
        // (lo decide el núcleo); el toque se decide en el touch-up. En el modo de selección de la Luciérnaga (04 §2.5) el toque
        // elige la cuia que se revela, no un origen: nada sube.
        private void OnCuiaDown(int index)
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.AcceptsBoardInput || flow.Selected != TapController.NoSelection || flow.RevealSelecting
                || _pressPreview >= 0)
            {
                return;
            }

            if ((uint)index < (uint)_views.Length && PourRules.CanBeSource(flow.Session.Current, index))
            {
                _pressPreview = index;
                _views[index].SetLifted(true);
            }
        }

        // Soltar dentro del área táctil es un toque (C1-C6); soltar fuera, sobre otra cuia y tras ≥ 24 dp, es el arrastre (C10;
        // B11, GameScreen.Drag.cs); si no, cancela.
        private void OnCuiaUp(int index, bool inside)
        {
            if (inside)
            {
                _actions.TapCuia(index);
            }
            else if ((uint)index < (uint)_views.Length)
            {
                CuiaView view = _views[index];
                TryDragPour(index, _pressPreview == index, view.PressScreenPosition, view.ReleaseScreenPosition, view.PointerCamera);
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

        /// <summary>
        /// Vuelve a aplicar los Patrones de fruta al nivel cargado con el ajuste de ahora (Ajustes › Patrones de fruta; 02 §12;
        /// tarea L3: se aplica al momento, sin recargar el nivel). Sin nivel cargado no hace nada: el siguiente lo toma al cargar.
        /// Devuelve si había nivel.
        /// </summary>
        internal bool RefreshFruitPatterns()
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || _views.Length == 0)
            {
                return false;
            }

            _animator.ApplyPatterns(PatternsOn(flow.LevelNumber));
            return true;
        }

        /// <summary>Patrones de fruta en curso (el interruptor que lleva cada cuia; tests).</summary>
        internal bool FruitPatternsOn => _animator.FruitPatterns;

        // Patrones de fruta del nivel con el ajuste del jugador (GameServices.PatternsOnFor); sin servicios (tests), el automático.
        private bool PatternsOn(int level)
        {
            return _services != null ? _services.PatternsOnFor(level) : Palette.PatternsOnFor(level);
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
            }

            _views = views;
            _generatedCount = Mathf.Max(1, flow.GeneratedCuias); // cuias generadas: fijan las filas (D-006); las añadidas van a la más corta
            _tinajaPair = BindTinajaPair(flow.Session.Initial); // la tinaja y su cuia, en la misma fila (02 §5; GameScreen.Tinaja.cs)
            OrderBoardLayers();
            float f = GameConfig.SpeedFactor(flow.LevelNumber);
            _timings = new PourTimings(
                GameConfig.AnimLiftMs * f,
                GameConfig.AnimTravelMs * f,
                GameConfig.AnimPourDoseMs * f,
                GameConfig.AnimPourTailMs * f,
                GameConfig.AnimReturnMs * f);
            _animator.Bind(_views, _timings, GameConfig.QueueMaxLagPours, _mechanicBackLayer, _mechanicFrontLayer);
            _animator.SpeedFactor = f;
            _animator.ReduceMotion = flow.ReduceMotion;
            _fx.ReduceMotion = flow.ReduceMotion;
            _animator.ApplyPatterns(PatternsOn(flow.LevelNumber)); // Patrones de fruta: el ajuste; en auto, 20 % e icono desde el 41 (07 §3; T-E; L3)
            for (int i = 0; i < board.Count; i++)
            {
                _animator.ShowContent(i, board[i], board.IsSealed(i));
            }

            LayoutBoard();
        }

        // Orden dentro del tablero (arquitectura del paso 1, §6): panel y repisas; capa de mecánicas trasera; las cuias en
        // orden; capa de mecánicas delantera; la intro (delante de las cuias); los chorros; los efectos.
        private void OrderBoardLayers()
        {
            _mechanicBackLayer.SetAsLastSibling();
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].Root.SetAsLastSibling();
            }

            _mechanicFrontLayer.SetAsLastSibling();
            _intro.Layer.SetAsLastSibling();
            _streamLayer.SetAsLastSibling();
            _fx.Root.SetAsLastSibling();
        }

        // La vista persigue al modelo (02 §3.2): el vertido con sus etapas de R10 y los tableros que se veía y del modelo.
        private void EnqueuePour(LevelFlow flow, in PourResult result)
        {
            BoardState after = flow.Session.Current;
            BoardState before = _shownBoard ?? after;
            _animator.EnqueuePour(new PourVisual(result.Pour, result.Events, before, after));
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
            AF.Audio.AudioCues.SetModalDuck(false); // AU1: sin modal, el lecho vuelve a su nivel
            _victoryOverlay.gameObject.SetActive(false);
            _giveUp.Hide();
            _failed.Hide();
            _noCanoes.Hide();
            _pause.Hide();
            _confirm.Hide();
            _sceneVeil.color = new Color(_sceneVeil.color.r, _sceneVeil.color.g, _sceneVeil.color.b, 0f);
            SetVictoryLights(false);
            for (int i = 0; i < _views.Length; i++)
            {
                _views[i].SetVictoryAura(0f);
            }

            _stuckAnim = -1f;
            _victoryAnim = -1f;
            _harvestCounting = false;
            _harvestShown = -1;
            _doubleShown = false;
            _autoNextMs = -1f;
            _nextHoldMs = -1f;
            _victoryBars.Hide();
            _ambient.SetSuspended(false);
            SlothLeave();
        }

        private void RefreshHud()
        {
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel)
            {
                return;
            }

            if (flow.Phase != _chromePhase)
            {
                _chromePhase = flow.Phase;
                SyncChrome();
            }

            // La bandeja, con el mismo saldo que el de arriba: la Cosecha que aún no se ve tampoco cuenta en sus precios ni en sus
            // avisos «Te faltan {n}» (D-011, punto 1). Solo importa donde se ve con la Cosecha sin enseñar, en la celebración (con
            // la victoria pendiente, Refresh no repinta). En VICTORY_SCREEN y AD_GATE la bandeja está oculta (SyncChrome) y lo
            // pendiente baja en cada fotograma mientras la fila cuenta: si entrara en su clave, la bandeja se repintaría entera
            // en cada uno, con cadenas nuevas (08 §5.4). Allí recibe el saldo entero, que no cambia durante la cuenta, y al volver
            // a verse (SetVisible) se repinta de todos modos.
            int pending = PendingHarvest(flow) + PendingBarSeeds(flow);
            _tray.Refresh(flow.Phase == FlowPhase.Celebration ? pending : 0);
            _walletBar.Refresh(_services?.Wallet, flow.CanoesActive, pending); // biz_feature_lives (08 §9): sin canoas, sin su contador
            if (flow.Phase == FlowPhase.StuckModal && _stuck.Panel.gameObject.activeSelf)
            {
                // Tras comprar un Puñado o recargar Marea, el modal se rearma con el saldo nuevo y el mismo precio.
                int seeds = _services?.Wallet.Seeds ?? 0;
                int undo = flow.UndoStatus.Stock + (flow.UndoStatus.FreeLeft * 1000);
                int notes = _services?.PurchaseNotes.Version ?? 0;
                if (seeds != _stuckSeedsShown || undo != _stuckUndoShown || notes != _stuckNotesShown)
                {
                    ShowStuckView(false); // también con una nota de compra nueva (F3-5: pendiente, rechazada o acreditada)
                }
            }
            else if (flow.Phase == FlowPhase.NoCanoes && _noCanoes.Refresh(_services))
            {
                // Sin canoas rehízo el layout (cambió el video, el saldo o lo que se ofrece): sus botones cambiaron de sitio.
                EmitFunnelLayout(NoCanoesLayout);
            }
        }

        // Qué se ve arriba (D-008 paso 1: «el HUD de partida y la bandeja se ocultan» con el embudo abierto; arriba, semillas y
        // canoas): en juego, la cabecera (con las píldoras de Semillas y, desde el 31, Canoas: 07 §7.4 y 02 §5) y la bandeja;
        // en el embudo, el saldo grande (y la pausa en Aguas estancadas, que es la única salida sin elegir, 04 §6.6). «¿Salir
        // del nivel?» cuenta como embudo: como «Are You Sure?» de Magic Sort (ficha §4.3, t = 5), saldo arriba y HUD de
        // partida, bandeja y pausa ocultos.
        private void SyncChrome()
        {
            LevelFlow? flow = _flow;
            FlowPhase phase = flow?.Phase ?? FlowPhase.Loading;
            bool quitAsk = _confirm.QuitVisible;
            bool funnel = quitAsk || phase == FlowPhase.StuckModal || phase == FlowPhase.GiveUpConfirm || phase == FlowPhase.LevelFailed
                || phase == FlowPhase.NoCanoes;
            bool victoryScreen = phase == FlowPhase.VictoryScreen || phase == FlowPhase.AdGate;
            bool victory = victoryScreen || phase == FlowPhase.Celebration;

            // En la pantalla de victoria (y en AD_GATE) arriba va el saldo, como en el embudo (ficha §4.8: «el saldo sale
            // siempre arriba a la izquierda»), y la Cosecha llega a él mientras cuenta (02 §6). Durante la celebración el HUD
            // sigue a la vista, como Magic Sort (ficha §4.6: «tablero y HUD oscurecidos, no ocultos»).
            bool balance = funnel || victoryScreen;
            _badge.gameObject.SetActive(!balance);
            _restart.Button.gameObject.SetActive(!balance);
            _tray.SetVisible(!funnel && !victoryScreen);

            // En LEVEL_CARD la pausa ya está, bajo el velo de la tarjeta previa, que se lleva todos los toques (y sin poder salir
            // ni reiniciar, OnPause no hace nada): así no aparece de golpe al cerrarla, como tampoco la bandeja [P].
            bool pause = !victory && !quitAsk && (phase == FlowPhase.Playing || phase == FlowPhase.Intro || phase == FlowPhase.LevelCard
                || phase == FlowPhase.StuckModal);
            _pauseButton.Button.gameObject.SetActive(pause);

            // 07 §7.4 (dueño del HUD) y 02 §5: en INTRO, PLAYING y la celebración, las píldoras de la cabecera, en la capa del
            // HUD (el origen que vierte las tapa, como al rótulo del nivel: D-006); en el embudo y la victoria, el saldo grande
            // en la capa superior. Nada se oculta salvo donde D-008 lo pide (el HUD de partida en el embudo).
            _walletBar.SetCompact(!balance);
            Transform layer = balance ? _topLayer : _hud;
            if (_walletBar.Root.parent != layer)
            {
                _walletBar.Root.SetParent(layer, false);
                _walletBar.Root.SetAsLastSibling();
            }

            _walletBar.SetVisible(true);
            LayoutWalletBar(pause && phase == FlowPhase.StuckModal);
            _walletBar.Refresh(_services?.Wallet, flow != null && flow.CanoesActive, flow == null ? 0 : PendingHarvest(flow) + PendingBarSeeds(flow));
        }

        // Saldo grande del embudo a la izquierda (tras la pausa en Aguas estancadas); píldoras de la cabecera de partida entre
        // la insignia del nivel (x 56-192) y Reiniciar (x W − 68), con 4 dp a cada lado (≈ 92 dp a 360 de ancho). La columna
        // Semillas/Canoas (desde el 31) va a su tamaño, de TopRowY + 1 a TopRowY + 49, salvo que el parcial de las etiquetas la
        // compacte dentro de la fila (LabelsWalletColumn: el tablero pasa bajo el HUD).
        private void LayoutWalletBar(bool afterPause)
        {
            if (_walletBar.Compact)
            {
                float left = HudBadgeRight + 4f;
                float right = RestartLimitDp;
                float pill = WalletBar.CompactPillDp;
                float gap = WalletBar.CompactGapDp;
                float columnTop = TopRowY + 1f;
                LabelsWalletColumn(ref pill, ref gap, ref columnTop);
                _walletBar.Layout(left, TopRowY, Mathf.Min(120f, right - left), pill, gap, columnTop);
                return;
            }

            _walletBar.Layout(afterPause ? 64f : 16f, TopRowY);
        }

        // 02 §3.1 C8 y 04 §1.1: Marea con cupo deshace; sin cupo ni stock, el botón recarga (video si hay; si no, semillas).
        private void OnTrayUndo()
        {
            LevelFlow? flow = _flow;
            if (flow == null)
            {
                return;
            }

            // D-011, punto 1: con la victoria (o el bloqueo) pendiente, la bandeja se queda como estaba, pero el input ya está
            // bloqueado (R15): Marea no deshace, no recarga con semillas ni abre el video de rv_undo_refill.
            if (flow.IsResolving)
            {
                return;
            }

            if (flow.UndoStatus.HasQuota)
            {
                _actions.PressUndo();
            }
            else if (_services != null && _services.RewardedReady(AdPlacement.UndoRefill))
            {
                _actions.PressUndoRefillVideo();
            }
            else
            {
                _actions.PressUndoRefillSeeds();
            }
        }

        // 04 §2.7: con ≥ restart_confirm_min_pours vertidos, confirmación de una línea con atajo a Deshacer.
        private void OnHudRestart()
        {
            LevelFlow? flow = _flow;
            if (flow != null && flow.RestartNeedsConfirm)
            {
                _confirm.Show(ConfirmKind.Restart, flow.CanUndo, false, _canvasW, _canvasH);
                EmitConfirmLayout();
                return;
            }

            _actions.PressRestart();
        }

        private void OnPause()
        {
            LevelFlow? flow = _flow;
            if (flow == null || !(flow.CanQuit || flow.CanRestart))
            {
                return;
            }

            flow.ClearSelection();
            _pause.Show(flow.CanRestart, PauseMapOffered, _canvasW, _canvasH, flow.SkipOffer);
            EmitPauseLayout();
        }

        /// <summary>
        /// El Mapa del río se puede abrir desde esta pantalla (la geometría y las paradas en la escena; lo fija
        /// <see cref="GameBootstrap"/> y lo apaga si la geometría no valida): sin él, ni «Mapa del río» en la pausa ni «Ver mapa» en
        /// la victoria (tarea M4.3). False por defecto (tests sin Mapa).
        /// </summary>
        internal bool RiverMapOpenable { get; set; }

        /// <summary>
        /// La pausa ofrece «Mapa del río» (02 §5; D-013, punto 7; tarea M4.3): con el Mapa (<see cref="RiverMapOpenable"/>) y desde
        /// <c>max_level</c> ≥ 10, como la pestaña del inicio (D-008: oculto hasta el 10) [P].
        /// </summary>
        internal bool PauseMapOffered
        {
            get
            {
                IMapProgress? progress = _services?.MapProgress;
                return RiverMapOpenable && progress != null && progress.MaxLevel >= RiverMapScreen.TabFromMaxLevel;
            }
        }

        // Pausa › «Mapa del río» (D-013, punto 7; 04 §5.2; tarea M4.3): el Mapa encima del nivel en pausa, con «Volver al nivel»; no
        // es salir del nivel (ni canoa, ni Crecida, ni fails_level, ni cambio de fase). Al cerrarlo se vuelve al nivel, sin la
        // pausa. Si no se puede abrir, la pausa se queda como estaba.
        private void OnPauseMap()
        {
            if (_actions.OpenRiverMap(MapOpenSource.Pause))
            {
                _pause.Hide();
            }
        }

        private void EmitPauseLayout()
        {
            _layoutIds.Clear();
            _layoutRects.Clear();
            _pause.VisibleButtons(_layoutIds, _layoutRects);
            _probe.Layout("pause", RequireFlow().LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        private void OnResume()
        {
            _pause.Hide();
        }

        // Pausa › «Salto del Boto» (04 §2.6; bloque B3): la confirmación de GameBootstrap.Skip.cs, que cierra la pausa.
        private void OnPauseSkip()
        {
            if (_actions is ISkipActions skip)
            {
                skip.PressSkip();
            }
        }

        private void OnPauseRestart()
        {
            _pause.Hide();
            OnHudRestart();
        }

        // D-008: desde el 31 «Salir» pregunta «¿Salir del nivel? Perderás 1 canoa» [P] (sin la línea de canoa si no hay canoa
        // en juego: Canoas infinitas o 0 canoas); en 1-30, y desde el 31 con biz_feature_lives apagado (D-038), sale sin coste.
        private void OnPauseQuit()
        {
            _pause.Hide();
            LevelFlow? flow = _flow;
            if (flow != null && flow.QuitIsHardFail)
            {
                _confirm.Show(ConfirmKind.Quit, false, flow.CanoeAtStake, _canvasW, _canvasH);
                SyncChrome();
                EmitConfirmLayout();
                return;
            }

            _actions.PressQuit();
        }

        private void OnConfirm()
        {
            ConfirmKind kind = _confirm.Kind;
            _confirm.Hide();
            SyncChrome();
            if (kind == ConfirmKind.Quit)
            {
                _actions.PressQuit();
            }
            else if (kind == ConfirmKind.Restart)
            {
                _actions.PressRestart();
            }
        }

        private void OnConfirmCancel()
        {
            _confirm.Hide();
            SyncChrome();
        }

        private void OnConfirmUndo()
        {
            _confirm.Hide();
            SyncChrome();
            _actions.PressUndo();
        }

        private void EmitConfirmLayout()
        {
            _layoutIds.Clear();
            _layoutRects.Clear();
            _confirm.VisibleButtons(_layoutIds, _layoutRects);
            _probe.Layout(_confirm.Kind == ConfirmKind.Quit ? "quit_confirm" : "restart_confirm", RequireFlow().LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        // AF_LAYOUT de un modal del embudo del nivel (D-008; T-B de la prueba del 2026-10-09): ¿Seguro? (giveup_confirm: close,
        // rescue si se ofrece y give_up), Nivel fallido (level_failed: retry y close) y Sin canoas (no_canoes: close, y
        // extra_canoe_video, refill_canoes e infinite_canoes si se ofrecen, y home). Sale al abrirse (¿Seguro? y Nivel fallido,
        // también al reabrir la app en ellos; Sin canoas no se guarda: se reabre como Nivel fallido o como el nivel) y, en Sin
        // canoas, cada vez que rehace su layout (RefreshHud). Fuera de desarrollo no hace nada.
        private void EmitFunnelLayout(string screen)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            _layoutIds.Clear();
            _layoutRects.Clear();
            switch (screen)
            {
                case GiveUpConfirmLayout:
                    _giveUp.VisibleButtons(_layoutIds, _layoutRects);
                    break;
                case LevelFailedLayout:
                    _failed.VisibleButtons(_layoutIds, _layoutRects);
                    break;
                default:
                    _noCanoes.VisibleButtons(_layoutIds, _layoutRects);
                    break;
            }

            _probe.Layout(screen, RequireFlow().LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        // AF_LAYOUT de Aguas estancadas, al terminar de subir el panel (TickPanels).
        private void EmitStuckLayout()
        {
            _layoutIds.Clear();
            _layoutRects.Clear();
            _stuck.VisibleButtons(_layoutIds, _layoutRects);
            _probe.Layout("stuck", RequireFlow().LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        // Vuelve a publicar la AF_LAYOUT del modal del nivel que está abierto sobre el tablero, si ya publicó la suya; sin modal,
        // nada. La llama el final del re-layout de una cuia añadida justo después de su línea level, para que la última línea
        // siga siendo la de lo que se ve: ¿Salir? o ¿Reiniciar?, la Pausa, el embudo (¿Seguro?, Nivel fallido y Sin canoas),
        // Aguas estancadas ya arriba y la victoria ya arriba. La tarjeta previa no hace falta: la línea level ya la trae.
        private void EmitOpenModalLayout()
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            if (_confirm.Visible)
            {
                EmitConfirmLayout();
            }
            else if (_pause.Visible)
            {
                EmitPauseLayout();
            }
            else if (_giveUp.Visible)
            {
                EmitFunnelLayout(GiveUpConfirmLayout);
            }
            else if (_failed.Visible)
            {
                EmitFunnelLayout(LevelFailedLayout);
            }
            else if (_noCanoes.Visible)
            {
                EmitFunnelLayout(NoCanoesLayout);
            }
            else if (RequireFlow().Phase == FlowPhase.StuckModal && _stuck.Panel.gameObject.activeSelf && _stuckAnim < 0f)
            {
                EmitStuckLayout();
            }
            else if (_victoryAnim >= PanelSlideMs)
            {
                EmitVictoryLayout();
            }
        }

        // «Llenar y teñir» (guion de la intro §7): geometría del panel, del marco, de las repisas y del HUD, y la especie del tramo
        // (o el dosel: reintento, mundo 13, sin arte en release, franja < 44 dp). Reducir movimiento lo decide el flujo.
        private void StartIntro(LevelFlow flow, int durationMs, bool isShort)
        {
            StartIntro(flow, durationMs, isShort, !isShort && !flow.ReduceMotion && flow.LevelNumber < GameConfig.AnimTintFastFromLevel);
        }

        // longVersion: la versión la decide la clave que dio la duración (anim_tint_duration_ms_early en el primer intento de
        // 1-20; _late desde el 21 y en los reintentos), nunca T (guion §7.2-§7.3). Arrancarla la suelta (_introHeld).
        private void StartIntro(LevelFlow flow, int durationMs, bool isShort, bool longVersion)
        {
            _introHeld = false;
            _introAnimStarted = true;
            _introCfgMs = durationMs;
            IntroInput input = _intro.Input;
            input.Clear();
            input.Version = longVersion ? IntroVersion.Long : IntroVersion.Short;
            BoardLayoutResult? l = _layout;
            if (l != null)
            {
                input.Rows = l.Rows;
                for (int r = 0; r < l.Rows && r < IntroInput.MaxRows; r++)
                {
                    input.RowFootY[r] = l.RowFootY[r];
                }

                input.PanelX = l.PanelX;
                input.PanelTop = l.PanelY;
                input.PanelWidth = l.PanelWidth;
                input.PanelBottom = l.PanelBottom;
            }

            input.FrameDp = PanelFrameDp;
            input.HudBottom = BoardTop;
            input.TrayTop = TrayTop;
            input.CanvasWidth = _canvasW;
            input.Seed = flow.LevelNumber;
            // En release, sin su arte final: dosel, nunca la especie de otro mundo (guion §4.3); la regla, en FaunaArtAvailable.
            FaunaSpecies? candidate = Fauna?.ForLevel(flow.LevelNumber);
            bool art = candidate != null && FaunaArtAvailable(candidate, false);
            FaunaSpecies? species = IntroCast.Resolve(Fauna, flow.Level.World, flow.LevelNumber, isShort ? 2 : 1, art);
            _introSpecies = species;
            // Con 3 filas, la tropa lateral en los niveles que le tocan (D-021; IntroSideSchedule); si no cabe, el planificador
            // vuelve a la regla de D-010 (rama con franja ≥ 44 dp; si no, dosel).
            input.Perch = species == null ? IntroPerch.Canopy : (SideTroopFor(flow, isShort) ? IntroPerch.Side : IntroPerch.Branch);
            if (species != null)
            {
                input.Model = species.Model == FaunaModel.Hang ? TroopModel.Hang : (species.Model == FaunaModel.Shake ? TroopModel.Shake : TroopModel.Row);
                input.Troop = species.Troop;
                input.MonkeyDp = species.SizeDp;
                input.Pair = species.HasTrait("colas_entrelazadas"); // la pareja se sienta junta con las colas trenzadas
            }

            _troop.Prepare(species);
            _intro.ReduceMotion = flow.ReduceMotion;

            // El agua del mundo del nivel (07 §2.1; mundo 3, agua negra), no la del primer paquete del catálogo.
            Color water = Palette.WaterBase(flow.Level.WaterBase);
            IntroWaterColor = water;
            _intro.Start(_views, flow.Session.Current, water, durationMs);
        }

        // D-021: ¿va este intento con la tropa lateral? Solo en primer intento, con el tablero en 3 filas y según el ordinal del
        // nivel entre los de 3 filas de su tramo (IntroSideSchedule), con la misma regla de filas del tablero en este lienzo.
        private bool SideTroopFor(LevelFlow flow, bool retry)
        {
            BoardLayoutResult? l = _layout;
            if (retry || l == null || l.Rows != 3)
            {
                return false;
            }

            int ordinal = ThreeRowOrdinal(flow.Catalog, Fauna, flow.LevelNumber, _canvasW, BoardTop, BoardBottom);
            return IntroSideSchedule.IsSide(ordinal, retry);
        }

        /// <summary>
        /// El nivel <paramref name="def"/> va en 3 filas en un lienzo de <paramref name="canvasWidth"/> dp con el tablero entre
        /// <paramref name="boardTop"/> y <paramref name="boardBottom"/> (la regla de <see cref="BoardLayout.RowsFor"/> con sus
        /// cuias generadas y la cuia más alta). Sin asignaciones.
        /// </summary>
        internal static bool HasThreeRows(LevelDefinition def, float canvasWidth, float boardTop, float boardBottom)
        {
            int count = def.CuiaCount;
            if (count < 1 || count > 16)
            {
                return false;
            }

            float heightU = 0f;
            for (int i = 0; i < count; i++)
            {
                heightU = Mathf.Max(heightU, CuiaGeometry.HeightU(def.CapacityOf(i)));
            }

            return BoardLayout.RowsFor(count, heightU, canvasWidth, boardTop, boardBottom) == 3;
        }

        /// <summary>
        /// Ordinal del nivel <paramref name="level"/> entre los niveles de 3 filas de su tramo (D-021; el mundo y, dentro de él,
        /// la especie del nivel si empieza más tarde: <see cref="IntroSideSchedule.SegmentStart"/>), contando desde 0; −1 si no
        /// está en el catálogo o no va en 3 filas. Determinista por el número de nivel y el lienzo. Sin asignaciones.
        /// </summary>
        internal static int ThreeRowOrdinal(LevelCatalog catalog, FaunaCatalog? fauna, int level, float canvasWidth, float boardTop, float boardBottom)
        {
            LevelDefinition? def = catalog.Find(level);
            if (def == null || !HasThreeRows(def, canvasWidth, boardTop, boardBottom))
            {
                return -1;
            }

            int world = def.World;
            int worldFirst = world >= 1 && world <= WorldTable.WorldCount ? WorldTable.FirstLevel(world) : catalog.FirstLevel;
            FaunaSpecies? species = fauna?.ForLevel(level);
            int start = IntroSideSchedule.SegmentStart(level, worldFirst, species != null ? species.FromLevel : worldFirst);
            int ordinal = 0;
            for (int n = start; n < level; n++)
            {
                LevelDefinition? other = catalog.Find(n);
                if (other != null && other.World == world && HasThreeRows(other, canvasWidth, boardTop, boardBottom))
                {
                    ordinal++;
                }
            }

            return ordinal;
        }

        /// <summary>
        /// Compuerta del arte de una especie (guion de la intro §4.3 y §11), la misma para la intro y para la tarjeta
        /// (<see cref="FaunaArtRule"/>): en un build de desarrollo vale el miquito provisional por código; fuera de él, la tropa
        /// exige el PNG <c>fauna_&lt;id&gt;_partes</c> y la tarjeta (<paramref name="forCard"/>), además, <c>fauna_&lt;id&gt;_retrato</c>.
        /// El build es <c>Debug.isDebugBuild</c> salvo <see cref="LaunchOptions.DevelopmentBuildOverride"/> (tests).
        /// </summary>
        internal static bool FaunaArtAvailable(FaunaSpecies species, bool forCard)
        {
            bool development = LaunchOptions.DevelopmentBuildOverride ?? Debug.isDebugBuild;
            bool parts = ArtSlots.Find(species.PartsSlot) != null;
            return forCard
                ? FaunaArtRule.CardArt(development, parts, ArtSlots.Find(species.PortraitSlot) != null)
                : FaunaArtRule.TroopArt(development, parts);
        }

        /// <summary>
        /// Enganche de depuración (capturas y pruebas, guion de la intro A14): vuelve a lanzar «Llenar y teñir» del nivel
        /// cargado desde t = 0 con <paramref name="durationMs"/> y como primer intento o como reintento (dosel), sin tocar el
        /// flujo. Con <see cref="IntroAnimation.Seek"/> fija el instante de cada captura.
        /// </summary>
        internal void DebugReplayIntro(int durationMs, bool retry)
        {
            LevelFlow flow = RequireFlow();
            _animator.SnapAll();
            ClearLifts();
            HideOverlays();
            StartIntro(flow, durationMs, retry, !retry && durationMs > GameConfig.AnimTintDurationMsLate);
        }

        // Variante aliviada: cuando termina el cartel arranca el teñido corto (02 §2 INTRO; 07 §5.5).
        private void TickIntroDelay(float dtMs)
        {
            if (_introDelayMs < 0f)
            {
                return;
            }

            _introDelayMs -= dtMs;
            if (_introDelayMs <= 0f)
            {
                _introDelayMs = -1f;
                LevelFlow? flow = _flow;
                if (flow != null && flow.HasLevel && flow.Phase == FlowPhase.Intro)
                {
                    StartIntro(flow, _introPendingMs, true);
                }
            }
        }

        // Cuia extra o Cuia de rescate (02 §4.1; D-006 punto 4): la cuia nueva va a la fila más corta y solo cambia el ancho;
        // las demás se reacomodan en 300 ms y la nueva entra en 600 ms (Capi la trae cuando exista su arte, 04 §2.2). Las
        // repisas se deslizan con las cuias (TickRelayout): si cambia el ancho de la cuia (de 10 a 11 cuias o de 15 a 16), el
        // pie de cada fila se mueve y lo que se dibuja sobre la repisa (el canal del Remolino) no se desfasa de ella.
        private void AddCuiaView(LevelFlow flow, int index)
        {
            _animator.SnapAll();
            ClearLifts();
            int old = _views.Length;
            var from = new Vector2[old];
            var fromWidth = new float[old];
            for (int i = 0; i < old; i++)
            {
                from[i] = _views[i].FootPosition;
                fromWidth[i] = _views[i].Unit * CuiaGeometry.WidthU;
            }

            for (int r = 0; r < _shelves.Length; r++)
            {
                RectTransform shelf = _shelves[r].rectTransform;
                _relayoutShelfFrom[r] = new Vector3(shelf.anchoredPosition.x, -shelf.anchoredPosition.y, shelf.sizeDelta.x);
            }

            BoardState board = flow.Session.Current;
            var views = new CuiaView[board.Count];
            for (int i = 0; i < old && i < views.Length; i++)
            {
                views[i] = _views[i];
            }

            float liftMs = GameConfig.AnimLiftMs * GameConfig.SpeedFactor(flow.LevelNumber);
            for (int i = old; i < views.Length; i++)
            {
                views[i] = CuiaView.Create(_board, i, board[i].Capacity, _art, _cuiaDown, _cuiaUp, _fx);
                views[i].SetLiftDuration(liftMs);
            }

            _views = views;
            OrderBoardLayers();
            _animator.Bind(_views, _timings, GameConfig.QueueMaxLagPours, _mechanicBackLayer, _mechanicFrontLayer);
            for (int i = old; i < views.Length; i++)
            {
                _animator.ShowContent(i, board[i], board.IsSealed(i));
            }

            LayoutBoard();
            _shownBoard = board;
            _relayoutFrom = from;
            _relayoutFromWidth = fromWidth;
            _addedView = index;
            _relayoutMs = 0f;

            // La AF_LAYOUT level sale al acabar el re-layout (TickRelayout), no aquí: en t = 0 las cuias estaban en su sitio
            // viejo y la nueva medía 0 × 0.
            TickRelayout(0f);
        }

        private void TickRelayout(float dtMs)
        {
            if (_relayoutMs < 0f || _layout == null)
            {
                return;
            }

            _relayoutMs += dtMs;
            float t = Ease.OutCubic(Mathf.Clamp01(_relayoutMs / RelayoutMs));
            BoardLayoutResult l = _layout;
            for (int i = 0; i < _relayoutFrom.Length && i < _views.Length; i++)
            {
                var to = new Vector2(l.CenterX[i], -l.FootY[i]);
                _views[i].SetGeometry(Vector2.LerpUnclamped(_relayoutFrom[i], to, t), Mathf.LerpUnclamped(_relayoutFromWidth[i], l.CuiaWidth, t), l.SlotWidth);
            }

            // Las repisas, con el mismo deslizamiento que los pies de su fila (las filas no cambian: D-006).
            for (int r = 0; r < l.Rows && r < _shelves.Length; r++)
            {
                PlaceShelf(r, Vector3.LerpUnclamped(_relayoutShelfFrom[r], ShelfRect(l, r), t));
            }

            if ((uint)_addedView < (uint)_views.Length)
            {
                float a = Ease.OutBack(Mathf.Clamp01(_relayoutMs / AddedCuiaMs), 1.6f);
                _views[_addedView].Root.localScale = new Vector3(a, a, 1f);
            }

            if (_relayoutMs >= Mathf.Max(RelayoutMs, AddedCuiaMs))
            {
                _relayoutMs = -1f;
                if ((uint)_addedView < (uint)_views.Length)
                {
                    _views[_addedView].Root.localScale = Vector3.one;
                }

                _addedView = -1;

                // LayoutBoard ya avisa de la recolocación (BoardLaidOut y LabelsLayoutBoard): un solo aviso por fotograma.
                LayoutBoard();

                // Una sola AF_LAYOUT level por cuia añadida (Cuia extra, rescate o Balsa), con las N + 1 cuias en su sitio final.
                // El re-layout no bloquea la entrada: si en esos 600 ms se abrió un modal del nivel (la Pausa, ¿Salir? o
                // ¿Reiniciar?), lo que se ve es el modal y su línea se repite detrás de esta (la level sigue valiendo para el
                // tablero cuando se cierre).
                EmitLevelLayout();
                EmitOpenModalLayout();
                return;
            }

            _animator.BoardLaidOut();
            LabelsLayoutBoard();
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
                    EmitStuckLayout();
                }
            }

            if (_victoryAnim >= 0f)
            {
                float before = _victoryAnim;
                _victoryAnim += dtMs;
                ApplyVictoryAnim(_victoryAnim);
                if (before < PanelSlideMs && _victoryAnim >= PanelSlideMs)
                {
                    EmitVictoryLayout();
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
            _stuck.Panel.anchoredPosition = new Vector2(_stuck.Panel.anchoredPosition.x, -(_stuckPanelY + (slide * (_stuck.Height + 40f))));
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
            float panelTop = _victoryPanelY + (slide * (_victoryPanelDp + RibbonRiseDp + 40f));
            _victoryPanel.anchoredPosition = new Vector2(_victoryPanel.anchoredPosition.x, -panelTop);
            float rt = Mathf.Clamp01((ms - 100f) / 300f);
            float ribbon = ms < 100f ? 0.6f : Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(rt, 2.3f));
            _ribbon.rectTransform.localScale = Vector3.one * ribbon;
            var ribbonCenter = new Vector2(_canvasW * 0.5f, -(panelTop - RibbonRiseDp + (_ribbonHeightDp * 0.5f)));
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
            float nt = Mathf.Clamp01((ms - VictoryButtonsInMs) / 150f);
            _nextGroup.alpha = nt;
            _homeGroup.alpha = nt;
            _doubleGroup.alpha = nt;
            float breathe = ms > 750f ? 1f + (0.015f * (1f - Mathf.Cos(((ms - 750f) / 1400f) * Mathf.PI * 2f))) : 1f;
            ((RectTransform)_next.Face.parent).localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, Ease.Out(nt)) * breathe;
        }

        // AF_LAYOUT level: al cargar el nivel (al final de OnLevelLoaded, tras sus ganchos), al acabar el re-layout de una cuia
        // añadida, al cambiar la pantalla y cuando aparece o se va una ranura de la bandeja (Cuia extra o Revelar; los parciales
        // la vuelven a pedir). Primero se refresca la bandeja (una vez enlazada, Refresh es segura en cualquier momento): así
        // «+1 cuia» existe ya en OnLevelLoaded, sale en esta línea con Marea en su sitio, y RevealLayoutIds anota esas ranuras
        // (SlotsVersion), así que el primer fotograma no vuelve a emitir.
        private void EmitLevelLayout()
        {
            if (_levelLayoutDeferred)
            {
                return; // la emite OnLevelLoaded al acabar sus ganchos
            }

            _tray.Refresh();
            _layoutIds.Clear();
            _layoutRects.Clear();
            _layoutIds.Add("undo");
            _layoutRects.Add((RectTransform)_tray.UndoButton.transform);
            if (_tray.ExtraCuiaShown)
            {
                _layoutIds.Add("extra_cuia");
                _layoutRects.Add((RectTransform)_tray.ExtraCuiaButton.transform);
            }

            if (_tray.HintShown)
            {
                _layoutIds.Add("hint");
                _layoutRects.Add((RectTransform)_tray.HintButton.transform);
            }

            _layoutIds.Add("restart");
            _layoutRects.Add((RectTransform)_restart.Button.transform);
            _layoutIds.Add("pause");
            _layoutRects.Add((RectTransform)_pauseButton.Button.transform);
            CardsLayoutIds(_layoutIds, _layoutRects);
            RevealLayoutIds(_layoutIds, _layoutRects);
            ShuffleLayoutIds(_layoutIds, _layoutRects);
            _probe.Layout("level", RequireFlow().LevelNumber, _views, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        private void ApplyScreenMetrics(bool initial)
        {
            ReadScreen(out _screenW, out _screenH, out _safeArea);

            float scale = CurrentCanvasScale(_screenW, _screenH); // de 320 a 360 dp de ancho con 1 dp físico por dp (Q-B11-5 (a))
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

            // Con Aguas estancadas abierto (o ¿Seguro? delante), el panel se rearma con el lienzo y la zona segura nuevos (la
            // disposición compacta y su alto dependen de ellos: StuckRoomDp) y el tablero se vuelve a ajustar (FX16a).
            if (!initial && _flow != null && _flow.HasLevel)
            {
                if (_flow.Phase == FlowPhase.StuckModal && _stuck.Panel.gameObject.activeSelf)
                {
                    ShowStuckView(true);
                    FitBoardToStuck(false);
                }
                else if (_flow.Phase == FlowPhase.GiveUpConfirm)
                {
                    FitBoardToStuck(false);
                }
            }

            if (!initial && _flow != null && _flow.HasLevel)
            {
                EmitLevelLayout();
            }
        }

        // Posiciones de 02 §5 y 07 §7.4 (dp desde arriba): fila superior en safe + 8 (alto 48); bandeja de 80 dp que
        // termina 16 dp por encima de la zona segura inferior; 40 dp entre tablero y bandeja (guía 3.2).
        private float TopRowY => _safeTop + 8f;

        /// <summary>Borde inferior de la fila del HUD (hudBottom de la intro): safeTop + 56.</summary>
        private float BoardTop => TopRowY + 48f;

        private float TrayBottom => _canvasH - _safeBottom - 16f;

        private float TrayTop => TrayBottom - 80f;

        // Píldora «Reiniciar» al pie del disco (guía 3.5: 60 × 16 dp). Si su etiqueta (12 sp, el mínimo de 02 §12) no cabe en
        // una línea (otro idioma, en-XA o la fuente al 130 %; tarea L4), la píldora crece hacia la izquierda, sin pasar de la
        // columna del saldo (a RestartPillGapDp), y hasta RestartPillOutDp más allá del área por la derecha; si ni así cabe, la
        // etiqueta va en dos líneas y la píldora crece hacia abajo, bajo la fila del HUD. Nunca parte una palabra (prueba de la
        // ola 3 en el teléfono: «Reinici / ar» al 130 %; tarea FX5): si una palabra de la etiqueta no cabe ni en ese ancho, la
        // píldora crece hacia la izquierda lo que le falte a esa palabra y la columna del saldo se estrecha otro tanto
        // (_restartPillLeft; LayoutWalletBar), sin bajar de WalletBar.CompactMinWidthDp, y si ni así cabe, con menos aire
        // (RestartPillTightPadDp); la etiqueta, en una línea si así cabe entera y, si no, en dos, cortadas entre palabras. Si
        // una palabra no cupiera ni así, se partiría: no pasa con es-419 ni con en-XA (Fx5Tests). Mide al colocar.
        private void LayoutRestartPill(float w)
        {
            _restartPillLeft = float.NaN;
            float labelW = Mathf.Ceil(UiBuilder.MinTextWidth(_restartLabel)) + (2f * RestartPillPadDp);
            if (labelW <= 60f)
            {
                SetRestartPillLines(false);
                UiBuilder.Place(_restartPill.rectTransform, 0f, 32f, 60f, 16f);
                return;
            }

            float areaX = w - 8f - 60f;
            float walletLeft = HudBadgeRight + 4f;
            float walletRight = walletLeft + Mathf.Min(120f, areaX - 4f - walletLeft);
            float maxW = Mathf.Max(60f, 60f + RestartPillOutDp - (walletRight + RestartPillGapDp - areaX));
            float right = 60f + RestartPillOutDp;
            if (labelW <= maxW)
            {
                SetRestartPillLines(false);
                UiBuilder.Place(_restartPill.rectTransform, right - labelW, 32f, labelW, 16f);
                return;
            }

            float width = maxW;
            float pad = RestartPillPadDp;
            float wordInk = Mathf.Ceil(UiBuilder.LongestWordWidth(_restartLabel));
            float wordW = wordInk + (2f * pad);
            if (wordW > maxW)
            {
                // Lo más ancha que puede ir: hasta la columna del saldo a su ancho mínimo (y a RestartPillGapDp de ella). Si ni así
                // cabe la palabra con su aire, el aire baja a RestartPillTightPadDp (en-XA al 130 % a 360 dp), y nunca más.
                float capW = areaX + right - (walletLeft + _walletBar.CompactMinWidthDp + RestartPillGapDp);
                if (wordW > capW)
                {
                    pad = RestartPillTightPadDp;
                    wordW = wordInk + (2f * pad);
                    labelW = Mathf.Ceil(UiBuilder.MinTextWidth(_restartLabel)) + (2f * pad);
                }

                width = Mathf.Max(maxW, Mathf.Min(wordW, capW));
                _restartPillLeft = areaX + right - Mathf.Min(width, labelW);
                if (labelW <= width)
                {
                    SetRestartPillLines(false, pad);
                    UiBuilder.Place(_restartPill.rectTransform, right - labelW, 32f, labelW, 16f);
                    return;
                }
            }

            SetRestartPillLines(true, pad);
            float inner = width - (2f * pad);
            float h = Mathf.Max(16f, Mathf.Ceil(_restartLabel.GetPreferredValues(_restartLabel.text, inner, float.PositiveInfinity).y) + 1f);
            UiBuilder.Place(_restartPill.rectTransform, right - width, 32f, width, h);
        }

        /// <summary>
        /// Borde izquierdo de la píldora «Reiniciar» (dp desde la izquierda del lienzo) cuando estrecha la columna del saldo
        /// (una palabra de su etiqueta no cabe en su área; tarea FX5); NaN si la columna va a su ancho de siempre.
        /// </summary>
        internal float RestartPillLeftDp => _restartPillLeft;

        // Hasta dónde llega lo de la fila del HUD a la izquierda de Reiniciar (dp desde la izquierda del lienzo): 4 dp antes de
        // su área (x = W − 68) o, si la píldora la estrecha, RestartPillGapDp antes de la píldora (tarea FX5).
        private float RestartLimitDp => float.IsNaN(_restartPillLeft)
            ? _canvasW - 8f - 60f - 4f
            : Mathf.Min(_canvasW - 8f - 60f - 4f, _restartPillLeft - RestartPillGapDp);

        /// <summary>Píldora «Reiniciar» (tests de desbordes; tarea L4).</summary>
        internal RectTransform RestartPillRect => _restartPill.rectTransform;

        /// <summary>Etiqueta de la píldora «Reiniciar» (tests de desbordes; tarea L4).</summary>
        internal TextMeshProUGUI RestartLabel => _restartLabel;

        /// <summary>
        /// Vuelve a colocar toda la pantalla como si hubiera cambiado su tamaño (tests: tras cambiar un texto a mano para forzar
        /// una etiqueta larga; tarea L4).
        /// </summary>
        internal void RelayoutForTests()
        {
            ApplyScreenMetrics(false);
        }

        private void SetRestartPillLines(bool two, float pad = RestartPillPadDp)
        {
            _restartLabel.textWrappingMode = two ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            _restartLabel.lineSpacing = two ? -20f : 0f;
            _restartLabel.rectTransform.offsetMin = new Vector2(two ? pad : 0f, 1f);
            _restartLabel.rectTransform.offsetMax = new Vector2(two ? -pad : 0f, 0f);
        }

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

        /// <summary>Tamaño de la insignia del nivel (guía 3.5): 26 sp.</summary>
        internal const float BadgeMaxSp = 26f;

        /// <summary>Mínimo de la insignia del nivel en español (guía 3.5): 20 sp.</summary>
        internal const float BadgeMinSp = 20f;

        /// <summary>
        /// Mínimo de la insignia del nivel cuando su texto no cabe a <see cref="BadgeMinSp"/> (fuera del español; D-030, pregunta
        /// 8 b): 12 sp, el mínimo de 02 §12.
        /// </summary>
        internal const float BadgeFallbackMinSp = 12f;

        // La insignia del nivel con su texto de ahora (D-030, pregunta 8 b): entre 20 y 26 sp si cabe a 20 o más; fuera del
        // español, si su tinta se sale de la caja incluso a 20 sp, entre 12 y 26 sp; nunca se recorta. En español (es-419) no baja
        // nunca de 20 sp (DC19: «sin cambio visual»; D-030 habla solo de otros idiomas), aunque al 130 %, del 100 en adelante,
        // la tinta llena la caja y en algunos números («Nivel 700») se sale unos dp (pendiente del dueño). Mide la tinta
        // (textBounds, con 1 dp de margen, como la compuerta de L4) al cargar el nivel, nunca por fotograma.
        private void FitLevelBadge()
        {
            UiBuilder.AutoSize(_levelText, BadgeMinSp, BadgeMaxSp);
            string locale = Loc.IsConfigured ? Loc.Locale : LocLocale.SourceLocale;
            if (string.Equals(locale, LocLocale.SpanishLatinAmerica, StringComparison.Ordinal))
            {
                return;
            }

            float width = _levelText.rectTransform.rect.width;
            if (width <= 0f)
            {
                return;
            }

            _levelText.ForceMeshUpdate();
            float ink = _levelText.textBounds.size.x;
            if (_levelText.fontSize <= _levelText.fontSizeMin + 0.01f && ink > width + BadgeInkToleranceDp)
            {
                UiBuilder.AutoSize(_levelText, BadgeFallbackMinSp, BadgeMaxSp);
                _levelText.ForceMeshUpdate();
            }
        }

        // Margen de la tinta de la insignia sobre su caja antes de bajar de 20 sp (dp; el de LocInventory.Tolerance).
        private const float BadgeInkToleranceDp = 1f;

        private void LayoutHud()
        {
            float w = _canvasW;

            // Insignia en la zona centro-izquierda (x 56-192, guía 3.5): placa de 120 × 44 y hojas; texto en 106 dp.
            UiBuilder.Place(_badge.rectTransform, 56f, TopRowY - 2f, 136f, 52f);
            UiBuilder.Place(_levelText.rectTransform, 15f, 3f, 106f, 42f);

            // Reiniciar a la derecha: área de 60 × 48 en x = W − 8 − 60; disco de 40 y píldora de 60 × 16 al pie.
            UiBuilder.Place((RectTransform)_restart.Button.transform, w - 8f - 60f, TopRowY, 60f, 48f);
            LayoutRestartPill(w);

            // Bandeja ajustada al contenido (guía 3.6): 16 + n·64 + (n − 1)·8 + 16 (1 ranura en 1-7; 2 desde el 8).
            _tray.Layout(w, TrayTop);
            _streakBar.Layout(w, BoardTop + StreakBar.BelowHeaderDp, BoardBottom - StreakBar.AboveBoardBottomDp); // 07 §7.4 (B1b)

            // Pausa a la izquierda de la fila superior (02 §5; 07 §7.4: 40 dp en un área de 48).
            UiBuilder.Place((RectTransform)_pauseButton.Button.transform, 8f, TopRowY, 48f, 48f);
            LevelFlow? flow = _flow;
            LayoutWalletBar(flow != null && flow.Phase == FlowPhase.StuckModal);

            // Paneles anclados abajo (07 §5.4, §5.5, §7.5), con los CTA en la zona del pulgar (02 §3.3).
            float panelW = Mathf.Min(328f, w - 32f);
            _stuckPanelY = _canvasH - _safeBottom - _stuck.BottomGapDp - _stuck.Height;
            _stuck.Place(w, _stuckPanelY);

            LayoutVictory();
            LabelsLayoutHud();
            CardsLayout();
            RevealLayout();
            HintLayout();
            ShuffleLayout();
        }

        // Panel de victoria de arriba abajo según lo que ofrece (con o sin Cosecha doble), con la cinta, los rayos y el velo
        // centrados en la cinta (02 §6; guía 5.3 y 12).
        private void LayoutVictory()
        {
            float w = _canvasW;
            float panelW = Mathf.Min(328f, w - 32f);
            float nextW = Mathf.Min(w - 48f, Mathf.Min(312f, panelW - 16f));

            // Victoria compacta (D-032, punto 2; GameScreen.Victory.cs): el escalón más bajo con el que la victoria más alta posible
            // de esta pantalla, idioma y escala cabe bajo la cabecera del HUD. En 360 × 780, y en 16:9 con sitio, el 0 (la de siempre).
            int compact = ChooseVictoryCompact();

            // Con el título en dos líneas la cinta crece hacia abajo (su borde de arriba sigue a RibbonRiseDp sobre el panel) y
            // todo lo del panel (la Cosecha, Siguiente, Inicio y la ranura del Dato del río) baja lo mismo; el panel crece.
            _ribbonHeightDp = LayoutRibbonTitle(compact >= VictoryCompactMargins);
            float y = VictoryHarvestTopDp + (_ribbonHeightDp - RibbonHeightDp);
            UiBuilder.Place(_harvestRow, 0f, y, panelW, VictoryHarvestDp);
            float mid = panelW * 0.5f;
            UiBuilder.Place(_harvestLabel.rectTransform, 16f, 0f, mid - 22f, VictoryHarvestDp);
            UiBuilder.Place(_harvestSeed.rectTransform, mid - 2f, 6f, 32f, 32f);
            UiBuilder.Place(_harvestValue.rectTransform, mid + 34f, 0f, mid - 50f, VictoryHarvestDp);
            y += VictoryHarvestDp + VictoryHarvestGapDp(compact);

            // Las barras del panel único (D-033, punto 10): Crecida y Canasta, entre la Cosecha y la Cosecha doble (GameScreen.Streak.cs).
            _victoryBars.Barless = _victoryBarsYield; // en el hito de TiTino que las cede, sin filas (B1b-Q10 (c))
            y += _victoryBarsDropped ? _victoryBars.Collapse() : _victoryBars.Layout(VictoryBarsSideDp, y, panelW - (2f * VictoryBarsSideDp), compact);

            // Tras la Cosecha doble, mientras espera a avanzar solo (D-011, punto 4), Inicio y la Cosecha doble quedan inactivos,
            // también al recolocar la pantalla (LayoutHud); Siguiente sigue activo y puede adelantar el avance.
            bool autoNext = _autoNextMs >= 0f || _nextHoldMs >= 0f; // también en la espera de 250 ms de Siguiente (B1b)
            _double.Button.gameObject.SetActive(_doubleShown);
            UiBuilder.SetEnabled(_double.Button, !autoNext);
            if (_doubleShown)
            {
                UiBuilder.Place((RectTransform)_double.Button.transform, (panelW - nextW) / 2f, y, nextW, ModalKit.HeightOf(_double));
                float face = ModalKit.FaceHeightOf(_double);
                UiBuilder.Place(_doubleIcon.rectTransform, 12f, (face - 22f) * 0.5f, 24f, 22f);
                UiBuilder.Place(_doubleLabel.rectTransform, 40f, 3f, nextW - 52f, face * 0.52f);
                UiBuilder.Place(_doubleSub.rectTransform, 40f, (face * 0.52f) + 1f, nextW - 52f, face * 0.36f);
                y += ModalKit.HeightOf(_double) + 2f;
            }

            LabelsVictoryReserve(ref y); // el chip del nivel siguiente colgado de Siguiente: Siguiente e Inicio bajan, el panel crece
            _victoryCtaTopDp = y;
            ApplyNextStretch(compact >= VictoryCompactRow); // en la fila del escalón 3, Siguiente con el alto que pide su etiqueta
            float nextH = ModalKit.HeightOf(_next);
            UiBuilder.SetEnabled(_next.Button, true);
            UiBuilder.SetEnabled(_home.Button, !autoNext);
            float bottom;
            float rowX = (panelW - nextW) / 2f;
            if (compact >= VictoryCompactRow)
            {
                // Victoria compacta, escalón 3 (D-032, punto 2) [P]: «Inicio» (y «Ver mapa», con la llegada) a la izquierda de
                // «Siguiente», en su misma fila (GameScreen.Victory.cs, PlaceVictoryRow).
                bottom = PlaceVictoryRow(rowX, y, nextW, nextH);
            }
            else
            {
                SetNextLabelFit(false);
                UiBuilder.Place((RectTransform)_next.Button.transform, rowX, y, nextW, nextH);
                _victoryNextCenterX = panelW * 0.5f;
                float homeW = Mathf.Min(VictoryHomeWidthDp, nextW);
                // Con la escala de texto, Siguiente crece en alto (ModalKit.StretchForTextScale; tarea L4) e Inicio baja lo mismo.
                _victoryHomeTopDp = y + VictoryHomeBelowCtaDp + VictoryNextGrowDp;
                UiBuilder.Place((RectTransform)_home.Button.transform, (panelW - homeW) / 2f, _victoryHomeTopDp, homeW, 48f);
                bottom = _victoryHomeTopDp + 48f;
            }

            SlotReserveBelowHome(ref bottom); // la ranura del Dato del río bajo Inicio: Siguiente sube una vez y el panel crece
            _victoryPanelDp = bottom + VictoryFrameDp(compact);

            _victoryPanelY = _canvasH - _safeBottom - 8f - _victoryPanelDp;
            UiBuilder.Place(_victoryPanel, (w - panelW) / 2f, _victoryPanelY, panelW, _victoryPanelDp);
            PlaceAt(_ribbon.rectTransform, panelW / 2f, -RibbonRiseDp + (_ribbonHeightDp * 0.5f), RibbonWidthDp, _ribbonHeightDp, new Vector2(0.5f, 0.5f));
            float ribbonY = _victoryPanelY - RibbonRiseDp + (_ribbonHeightDp * 0.5f);
            PlaceAt(_victoryRays.rectTransform, w * 0.5f, ribbonY, 2f * VictoryRaysRadiusDp, 2f * VictoryRaysRadiusDp, new Vector2(0.5f, 0.5f));
            PlaceAt(_victoryGlow.rectTransform, w * 0.5f, ribbonY, 2f * VictoryGlowRadiusDp, 2f * VictoryGlowRadiusDp, new Vector2(0.5f, 0.5f));
            PlaceAt(_victoryVeil.rectTransform, w * 0.5f, ribbonY, 2f * ProceduralArt.VeilHoleHalfDp, 2f * ProceduralArt.VeilHoleHalfDp, new Vector2(0.5f, 0.5f));
            _victorySurface.sprite = _art.ModalSurfaceFor(panelW - 16f, _victoryPanelDp - 16f);
            LabelsLayoutVictory();
            SlotLayout();
            ArrivalLayoutVictory();
        }

        // Título de la cinta de victoria (guía 5.5; tarea L4): en una línea, de 30 a 22 sp (× la escala de texto), en la franja
        // de 256 × 42 dp de la cinta de 72; si ni a 22 cabe en una línea, en dos a 22 sp (nunca menos) con el interlineado
        // apretado, y la cinta crece a lo alto lo que haga falta, hasta RibbonTallDp. Devuelve el alto de la cinta. Mide al
        // colocar, nunca por fotograma.
        private float LayoutRibbonTitle(bool compact)
        {
            const float boxW = RibbonWidthDp - 88f;
            TextMeshProUGUI title = _ribbonTitle;
            float min = RibbonTitleMinDp(compact);
            title.enableAutoSizing = true;
            title.fontSizeMin = min;
            title.fontSizeMax = _ribbonTitleMaxDp;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.lineSpacing = 0f;
            if (UiBuilder.MinTextWidth(title) <= boxW)
            {
                UiBuilder.Place(title.rectTransform, 44f, 15f, boxW, RibbonHeightDp - RibbonTitleMarginDp);
                return RibbonHeightDp;
            }

            title.enableAutoSizing = false;
            title.fontSize = min;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.lineSpacing = RibbonTitleLineSpacing;
            float need = Mathf.Ceil(title.GetPreferredValues(title.text, boxW, float.PositiveInfinity).y) + 1f;
            float h = Mathf.Clamp(need + RibbonTitleMarginDp, RibbonHeightDp, RibbonTallDp);
            float boxH = Mathf.Max(h - RibbonTitleMarginDp, need);
            UiBuilder.Place(title.rectTransform, 44f, (h - boxH) * 0.5f, boxW, boxH);
            return h;
        }

        /// <summary>Título de la cinta de victoria (tests de desbordes; tarea L4).</summary>
        internal TextMeshProUGUI VictoryRibbonTitle => _ribbonTitle;

        /// <summary>Cinta-hoja de victoria (tests de desbordes; tarea L4).</summary>
        internal RectTransform VictoryRibbonRect => _ribbon.rectTransform;

        /// <summary>Alto de la cinta-hoja de victoria ahora (dp): 72, o hasta 88 con el título en dos líneas (guía 5.5).</summary>
        internal float VictoryRibbonHeight => _ribbonHeightDp;

        /// <summary>Fila de la Cosecha del panel de victoria (tests de desbordes; tarea L4).</summary>
        internal RectTransform VictoryHarvestRect => _harvestRow;

        private void EmitVictoryLayout()
        {
            _layoutIds.Clear();
            _layoutRects.Clear();
            if (_double.Button.gameObject.activeSelf)
            {
                _layoutIds.Add("double_harvest");
                _layoutRects.Add((RectTransform)_double.Button.transform);
            }

            _layoutIds.Add("next");
            _layoutRects.Add((RectTransform)_next.Button.transform);
            _layoutIds.Add("home");
            _layoutRects.Add((RectTransform)_home.Button.transform);
            StreakLayoutIds(_layoutIds, _layoutRects); // las barras de la Crecida y la Canasta (B1b)
            SlotLayoutIds(_layoutIds, _layoutRects);
            ArrivalLayoutIds(_layoutIds, _layoutRects);
            _probe.Layout("victory", RequireFlow().LevelNumber, null, _layoutIds.ToArray(), _layoutRects.ToArray());
        }

        // La Cosecha cuenta de from a to en 400 ms (02 §6) tras delayMs; sin asignaciones (SetCharArray).
        private void StartHarvestCount(int from, int to, float delayMs)
        {
            _harvestFrom = Mathf.Max(0, from);
            _harvestTo = Mathf.Max(0, to);
            _harvestCountMs = -delayMs;
            _harvestCounting = true;
            _seedTicker.Begin(_harvestFrom, _harvestTo); // AU1
            _harvestShown = -1;
            WriteHarvest(_harvestFrom);
        }

        private void TickHarvest(float dtMs)
        {
            if (!_harvestCounting)
            {
                return;
            }

            _harvestCountMs += dtMs;
            float t = _harvestCountMs <= 0f ? 0f : Mathf.Clamp01(_harvestCountMs / GameConfig.HarvestCountMs);
            WriteHarvest(_harvestFrom + Mathf.RoundToInt((_harvestTo - _harvestFrom) * t));
            if (t >= 1f)
            {
                _harvestCounting = false;
            }
        }

        private void WriteHarvest(int value)
        {
            if (value == _harvestShown)
            {
                return;
            }

            _harvestShown = value;
            if (_seedTicker.Advance(value) > 0)
            {
                AF.Audio.AudioCues.Play(AF.Audio.SfxId.SeedsTick); // AU1: una semilla cada 10 (máx. 20)
            }

            _harvestBuf[0] = '+';
            int n = UiText.WriteNumber(_harvestBuf, 1, value, true);
            _harvestValue.SetCharArray(_harvestBuf, 0, n);
        }

        // Espera tras la Cosecha doble (D-011, punto 4): se descuenta con la vista y, al acabar, queda vencida (0) sin cargar
        // nada dentro de este fotograma; la consume TakeAutoNext después de LevelFlow.Tick. Sin asignaciones.
        private void TickAutoNext(float dtMs)
        {
            if (_autoNextMs <= 0f)
            {
                return; // sin espera (−1) o ya vencida (0)
            }

            _autoNextMs = Mathf.Max(0f, _autoNextMs - dtMs);
        }

        /// <summary>
        /// Avance tras la Cosecha doble (D-011, punto 4): true, una sola vez, si la espera venció y la pantalla sigue en
        /// VICTORY_SCREEN (si Siguiente se adelantó, <see cref="OnAdGate"/> ya la canceló); quien llama pulsa entonces
        /// Siguiente. Lo llama <c>GameBootstrap.Update</c> después de <see cref="LevelFlow.Tick"/>, no la vista en su
        /// <see cref="Tick"/>: así N + 1 se carga entre dos fotogramas, como con un toque en Siguiente, y su vista y su flujo
        /// empiezan juntos (el resto del fotograma de N no corre sobre N + 1). Sin asignaciones.
        /// </summary>
        internal bool TakeAutoNext()
        {
            if (TakeNextHold())
            {
                return true; // Siguiente con las barras pendientes, tras sus 250 ms (D-033, punto 9; GameScreen.Streak.cs)
            }

            if (_autoNextMs != 0f)
            {
                return false;
            }

            _autoNextMs = -1f;
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || flow.Phase != FlowPhase.VictoryScreen)
            {
                return false;
            }

            _probe.Victory("auto_next", flow, false);
            return true;
        }

        // «Inicio» de la victoria: nada mientras espera a avanzar solo tras la Cosecha doble (D-011, punto 4), aunque el botón ya
        // esté inactivo (LayoutVictory).
        private void OnVictoryHome()
        {
            if (_autoNextMs < 0f && _nextHoldMs < 0f)
            {
                LevelFlow flow = RequireFlow();
                _victoryBars.Complete(); // lo de las barras llega al saldo antes de salir (D-033, punto 9)
                SlotHomePressed(flow);
                ArrivalHomePressed(flow);
                _actions.PressHome();
            }
        }

        // Semillas de la Cosecha que el saldo de arriba aún no enseña. D-011, punto 1: la Cosecha se cobra en el instante lógico de
        // la victoria (02 §14), pero no se da por ganada antes de verse: mientras termina el último vertido (la victoria
        // pendiente) y durante la celebración, el saldo no la enseña. En la pantalla de victoria la recibe mientras la fila cuenta.
        private int PendingHarvest(LevelFlow flow)
        {
            if (flow.IsResolvingWin || flow.Phase == FlowPhase.Celebration)
            {
                return Mathf.Max(0, flow.HarvestSeeds);
            }

            bool victory = flow.Phase == FlowPhase.VictoryScreen || flow.Phase == FlowPhase.AdGate;
            if (!victory || _harvestShown < 0)
            {
                return 0;
            }

            return Mathf.Max(0, flow.HarvestSeeds - _harvestShown);
        }

        // Maquetación del tablero y, al final, los avisos de recolocación: la cola (y desde la ola 2, los overlays) y las
        // etiquetas del tablero.
        private void LayoutBoard()
        {
            LayoutBoardPanel();
            _animator.BoardLaidOut();
            LabelsLayoutBoard();
        }

        private void LayoutBoardPanel()
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
                _post.gameObject.SetActive(false);
                LayoutBackdrop(0f, 0f, false);
                return;
            }

            float heightU = 0f;
            for (int i = 0; i < _views.Length; i++)
            {
                heightU = Mathf.Max(heightU, _views[i].HeightU);
            }

            // D-006: el número de filas lo fijan las cuias generadas al cargar el nivel y no cambia a mitad de nivel; cada vista
            // que añaden Cuia extra, Cuia de rescate o Balsa va a la derecha de la fila más corta (a igualdad, la de más abajo
            // [P]), ninguna cuia cambia de fila (T-C) y solo se recalcula el ancho con el T en pantalla (re-layout de 300 ms,
            // 02 §4.1; tope de D-004).
            int generated = _generatedCount > 0 ? Mathf.Min(_generatedCount, _views.Length) : _views.Length;
            _layout = BoardLayout.Compute(_views.Length, generated, heightU, w, BoardTop, BoardBottom, _tinajaPair, LayoutTinajaIndex);
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
            float leftEdge = l.ContentLeft; // el vidrio y, con la Tinaja, su vasija y el poste de su cuia (GameScreen.Tinaja.cs)
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

            // Una repisa por fila (guía 4.5).
            for (int r = 0; r < l.Rows && r < _shelves.Length; r++)
            {
                _shelves[r].gameObject.SetActive(true);
                PlaceShelf(r, ShelfRect(l, r));
            }

            // El poste en el muelle, si la banda libre bajo el panel es ≥ 64 dp (guía 3.7 y 3.8). Capi ya no (D-016, T5-B).
            float band = TrayTop - l.PanelBottom;
            bool decor = band >= CapiMinBandDp;
            _post.gameObject.SetActive(decor);
            if (decor)
            {
                UiBuilder.Place(_post.rectTransform, w - 56f, TrayTop + 4f - 48f, 40f, 48f);
            }

            LayoutBackdrop(py, l.PanelBottom, true);
            _animator.BoardWidth = w; // lado del cuerpo al verter (D-006): el lienzo del layout
            UpdateScoopCeiling();
            _ambient.SetLayout(new Vector2(w, _canvasH), PanelRect(), new Rect(0f, _horizonY + 4f, w, Mathf.Max(0f, py - _horizonY - 8f)));
        }

        // Repisa de la fila r (guía 4.5) con el marco tal como se dibuja (PanelFrameDp): x de innerLeft + 6, cara superior en
        // pie − 3 (y desde arriba) y ancho hasta innerRight − 6.
        private Vector3 ShelfRect(BoardLayoutResult l, int r)
        {
            float frame = PanelFrameDp;
            return new Vector3(l.PanelX + frame + 6f, l.RowFootY[r] - 3f, l.PanelWidth - (2f * frame) - 12f);
        }

        // 26 dp de alto con su sombra (guía 4.5).
        private void PlaceShelf(int r, Vector3 rect)
        {
            UiBuilder.Place(_shelves[r].rectTransform, rect.x, rect.y, rect.z, 26f);
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
            StopLayoutBackdrop(horizon, panelTop, hasBoard);
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

        /// <summary>
        /// Alto que le queda a Aguas estancadas, con su aire de abajo, para no tapar ninguna cuia con el tablero subido hasta su
        /// tope (<see cref="ShiftToClear"/>): del pie más bajo de las cuias, más <see cref="StuckCuiaGapDp"/>, a la zona segura
        /// de abajo. Solo en los lienzos más bajos que <see cref="StuckModal.CompactCanvasBelowDp"/> (el 16:9; tarea FX16a): en
        /// los demás, sin límite (el panel no cambia).
        /// </summary>
        internal float StuckRoomDp
        {
            get
            {
                if (_layout == null || _views.Length == 0 || _canvasH >= StuckModal.CompactCanvasBelowDp - 0.5f)
                {
                    return float.PositiveInfinity;
                }

                float highestTop = float.MaxValue;
                float lowestFoot = float.MinValue;
                for (int i = 0; i < _views.Length; i++)
                {
                    highestTop = Mathf.Min(highestTop, _layout.FootY[i] - (_views[i].HeightU * _views[i].Unit) - CuiaView.LiftDp);
                    lowestFoot = Mathf.Max(lowestFoot, _layout.FootY[i]);
                }

                float lift = Mathf.Max(0f, highestTop - BoardTop);
                return _canvasH - _safeBottom - (lowestFoot - lift + StuckCuiaGapDp);
            }
        }

        /// <summary>Aire mínimo entre el pie de las cuias y el panel de Aguas estancadas compacto (dp) [P] (tarea FX16a).</summary>
        internal const float StuckCuiaGapDp = 2f;

        /// <summary>
        /// Con Aguas estancadas abierto (o ¿Seguro? delante), el tablero sube lo justo para que el panel no lo tape, sin pasar
        /// del borde de abajo del HUD (<see cref="ShiftToClear"/>; 04 §6.6: «sube lo justo para que el panel no lo tape»; 07
        /// §5.5). El tablero no cambia de tamaño: en 16:9, lo que el panel compacto aún tapa en los tableros que no caben (el 640,
        /// de 3 filas, y la rama B del 31 al 130 %) es una pregunta del dueño (tarea FX16a; <c>StuckCompactTests</c>).
        /// </summary>
        private void FitBoardToStuck(bool animate)
        {
            ShiftBoard(ShiftToClear(_stuckPanelY, 12f), animate);
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
            _liftFront.anchoredPosition = new Vector2(0f, shift); // y la de la cuia elevada
            UpdateScoopCeiling();
        }

        // Techo del arco de sacar de la Tinaja (TJ3): su cuia no sube hasta la cabecera (Nivel, canoas), 8 dp bajo lo más bajo
        // del HUD, en coordenadas del contenedor del tablero (y hacia arriba; sigue al desplazamiento del tablero).
        private void UpdateScoopCeiling()
        {
            _animator.ScoopCeilingY = -(HudBadgeBottom + ScoopHudGapDp + _boardShift);
        }

        /// <summary>Hueco entre el HUD y lo más alto que sube la cuia de la Tinaja al sacar (dp) [P] (TJ3).</summary>
        internal const float ScoopHudGapDp = 8f;

        // Pie de la insignia «Nivel {n}» (placa de 52 dp desde TopRowY − 2; guía 3.5): lo más bajo del HUD sobre el tablero.
        private float HudBadgeBottom => TopRowY + 50f;

        // La cuia cuyo cielo (los 32 dp táctiles sobre la boca, con los 16 de la elevación) entra en la fila del HUD se dibuja
        // en _liftFront mientras sube, está elevada o baja; si no (en reposo, en vuelo hacia la capa de vertido o con su cielo
        // bajo el HUD, que es lo normal con 1 y 2 filas), en el tablero, en su sitio entre las capas de mecánicas
        // (OrderBoardLayers). Solo cambia de padre cuando hace falta; sin asignaciones.
        private void SyncLiftFront()
        {
            float hudBottom = HudBadgeBottom;
            for (int i = 0; i < _views.Length; i++)
            {
                CuiaView v = _views[i];
                RectTransform root = v.Root;
                bool inFront = root.parent == _liftFront;
                bool raised = v.IsLifted || (inFront && v.BodyOffset.y > 0.01f);
                bool front = false;
                if (raised && !v.BodyInFront)
                {
                    float mouth = -v.FootPosition.y - _boardShift - (v.HeightU * v.Unit);
                    front = mouth - CuiaView.LiftDp - CuiaView.TouchHeadroomDp < hudBottom;
                }

                if (front && !inFront)
                {
                    root.SetParent(_liftFront, false);
                }
                else if (!front && inFront)
                {
                    root.SetParent(_board, false);
                    root.SetSiblingIndex(BoardSiblingOf(i));
                }
            }
        }

        // Índice de la cuia i entre las capas de mecánicas del tablero (OrderBoardLayers: la trasera, las cuias en orden y la
        // delantera), contando solo las cuias que están en el tablero.
        private int BoardSiblingOf(int i)
        {
            int index = _mechanicBackLayer.GetSiblingIndex() + 1;
            for (int j = 0; j < i; j++)
            {
                if (_views[j].Root.parent == _board)
                {
                    index++;
                }
            }

            return index;
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

        /// <summary>
        /// Escala del lienzo del juego (px por dp) para una pantalla de <paramref name="width"/> × <paramref name="height"/> px sin
        /// saber su densidad: 360 dp de ancho y, si así quedaran menos de <see cref="MinHeight"/> dp de alto, manda el alto (D-028,
        /// tarea DC3). Antes era min(ancho / 360, alto / 780): en 16:9 el lienzo medía 438,75 × 780 dp y el texto de 12 sp salía a
        /// 9,8 sp físicos.
        /// </summary>
        public static float CanvasScale(int width, int height)
        {
            return CanvasScale(width, height, 0f);
        }

        /// <summary>
        /// Escala del lienzo (px por dp) con la densidad del teléfono, <paramref name="pxPerDp"/> (px por dp físico; ≤ 0 = no se sabe):
        /// el lienzo mide 360 dp de ancho, salvo en los teléfonos de 320 a 360 dp físicos, donde mide su ancho con 1 dp físico por
        /// dp de lienzo (así el texto de 12 sp sale a 12 sp físicos, 02 §12, y el tablero usa el layout de 320 dp de 02 §5; Q-B11-5
        /// (a)), y en los de menos de 320, donde mide 320 dp. Después, la regla del alto mínimo de 640 dp (D-028; D-032, punto 5),
        /// que manda en las pantallas anchas. Todas las pantallas del juego usan esta misma escala (el inicio, la tienda y las capas
        /// de encima).
        /// </summary>
        public static float CanvasScale(int width, int height, float pxPerDp)
        {
            float canvasWidth = ReferenceWidth;
            if (pxPerDp > 0f)
            {
                float physical = width / pxPerDp;
                if (physical < ReferenceWidth)
                {
                    canvasWidth = Mathf.Max(MinWidth, physical);
                }
            }

            float scale = width / canvasWidth;
            if (height / scale < MinHeight)
            {
                scale = height / MinHeight;
            }

            return Mathf.Max(0.01f, scale);
        }

        /// <summary>Solo tests: la densidad de <see cref="SimulatedScreen"/> (px por dp físico; null = no se sabe, el lienzo de 360 dp).</summary>
        internal static float? SimulatedPxPerDp { get; set; }

        private static float s_devicePxPerDp = -1f;

        /// <summary>
        /// La densidad de la pantalla (px por dp físico): la de <see cref="SimulatedPxPerDp"/> con una pantalla simulada; en Android, la
        /// de <c>DisplayMetrics.density</c> (se lee una vez); en el Editor y si no se puede leer, 0 (no se sabe: lienzo de 360 dp).
        /// </summary>
        internal static float ReadPxPerDp()
        {
            if (SimulatedScreen.HasValue)
            {
                return SimulatedPxPerDp ?? 0f;
            }

            if (s_devicePxPerDp < 0f)
            {
                s_devicePxPerDp = QueryDevicePxPerDp();
            }

            return s_devicePxPerDp;
        }

        /// <summary>Escala del lienzo para la pantalla de ahora (<see cref="ReadScreen"/> y <see cref="ReadPxPerDp"/>).</summary>
        internal static float CurrentCanvasScale(int width, int height)
        {
            return CanvasScale(width, height, ReadPxPerDp());
        }

        private static float QueryDevicePxPerDp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject resources = activity.Call<AndroidJavaObject>("getResources"))
                using (AndroidJavaObject metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
                {
                    float density = metrics.Get<float>("density");
                    return density > 0f ? density : 0f;
                }
            }
            catch (System.Exception)
            {
                return 0f; // sin densidad, el lienzo de 360 dp de siempre
            }
#else
            return 0f;
#endif
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

        // ---- Ganchos de los parciales (arquitectura del paso 1, §7). Métodos parciales clásicos: void, sin modificador y
        // opcionales; sin implementación, el compilador quita sus llamadas. Comparten los campos privados, pero cada parcial
        // crea sus propios Image (no reutiliza _sceneVeil ni _panelFrame). Si hace falta otro gancho, lo añade la sesión
        // principal entre olas. ----

        // Etiquetas (GameScreen.Labels.cs): al final del constructor, con todo creado y antes de la primera maquetación.
        partial void LabelsBuild();

        // Etiquetas: al final de LayoutHud.
        partial void LabelsLayoutHud();

        // Etiquetas: al final de LayoutBoard y de cada fotograma de TickRelayout (cuia añadida); al añadir la cuia avisan los dos
        // en el mismo fotograma (LayoutBoard y después TickRelayout(0), que devuelve las vistas a la posición de partida).
        partial void LabelsLayoutBoard();

        // Etiquetas: al final de OnLevelLoaded.
        partial void LabelsLevelLoaded(LevelFlow flow);

        // Etiquetas: el tablero del modelo cambió; tras un vertido, Marea, OnResumed, OnCuiaAdded y al final de
        // OnIntroStarted. No en OnRestart: LevelFlow lo avisa antes de reiniciar la sesión y ahí el tablero aún es el viejo.
        partial void LabelsBoardChanged(LevelFlow flow);

        // Etiquetas: en OnVictoryScreen, con la pantalla de victoria ya colocada.
        partial void LabelsVictoryShown(LevelFlow flow);

        // Etiquetas: en LayoutVictory, justo antes de colocar Siguiente en y (dp desde el canto de arriba del panel de victoria):
        // lo que sume a y lo bajan Siguiente e Inicio, y el panel crece lo mismo hacia arriba (sigue anclado abajo).
        partial void LabelsVictoryReserve(ref float y);

        // Etiquetas: al final de LayoutVictory.
        partial void LabelsLayoutVictory();

        // Etiquetas: en LayoutWalletBar, con la cabecera de partida, antes de colocarla: alto de las píldoras de la columna
        // Semillas/Canoas (desde el 31), hueco entre ellas y borde de arriba de la columna (dp desde arriba). Llegan a su
        // tamaño (22 y 4 dp desde TopRowY + 1); el parcial puede compactarla dentro de la fila (WalletBar las dibuja a escala).
        partial void LabelsWalletColumn(ref float pillDp, ref float gapDp, ref float columnTop);

        // Etiquetas: en cada Tick, tras el resto de la vista.
        partial void LabelsTick(float dtMs);

        // Tarjeta previa y tutoriales (GameScreen.Cards.cs): al final del constructor.
        partial void CardsBuild();

        // Tarjetas: al final de LayoutHud.
        partial void CardsLayout();

        // Tarjetas: en cada Tick.
        partial void CardsTick(float dtMs);

        // Tarjetas: al final de OnLevelLoaded.
        partial void CardsLevelLoaded(LevelFlow flow);

        // Tarjetas: botones visibles para AF_LAYOUT (EmitLevelLayout, antes de emitir).
        partial void CardsLayoutIds(List<string> ids, List<RectTransform> rects);

        // Luciérnaga en la bandeja (GameScreen.Reveal.cs): al final del constructor, después de crear _tray.
        partial void RevealBuild();

        // Luciérnaga: al final de LayoutHud.
        partial void RevealLayout();

        // Luciérnaga: en cada Tick.
        partial void RevealTick(float dtMs);

        // Luciérnaga: al final de OnLevelLoaded.
        partial void RevealLevelLoaded(LevelFlow flow);

        // Luciérnaga: en los mismos puntos que LabelsBoardChanged.
        partial void RevealBoardChanged(LevelFlow flow);

        // Luciérnaga: botones visibles para AF_LAYOUT (EmitLevelLayout, antes de emitir).
        partial void RevealLayoutIds(List<string> ids, List<RectTransform> rects);

        // Morpho · Pista (GameScreen.Morpho.cs; tarea MO2): al final del constructor, después de RevealBuild.
        partial void HintBuild();

        // Morpho: al final de LayoutHud, tras RevealLayout.
        partial void HintLayout();

        // Morpho: en cada Tick, tras RevealTick.
        partial void HintTick(float dtMs);

        // Morpho: al final de OnLevelLoaded, tras RevealLevelLoaded.
        partial void HintLevelLoaded(LevelFlow flow);

        // Pororoca · Barajar (GameScreen.Shuffle.cs; bloque B2): al final del constructor, después de HintBuild.
        partial void ShuffleBuild();

        // Pororoca: al final de LayoutHud, tras HintLayout.
        partial void ShuffleLayout();

        // Pororoca: en cada Tick, tras HintTick.
        partial void ShuffleTick(float dtMs);

        // Pororoca: al final de OnLevelLoaded, tras HintLevelLoaded.
        partial void ShuffleLevelLoaded(LevelFlow flow);

        // Pororoca: en los mismos puntos que RevealBoardChanged, justo después.
        partial void ShuffleBoardChanged(LevelFlow flow);

        // Pororoca: botones visibles para AF_LAYOUT (EmitLevelLayout, tras RevealLayoutIds).
        partial void ShuffleLayoutIds(List<string> ids, List<RectTransform> rects);

        // Embudo (GameScreen.Funnel.cs): justo antes de abrir ¿Seguro? (GiveUpConfirm) o Nivel fallido (LevelFailed), para
        // colorear sus cabeceras.
        partial void FunnelModalShowing(FlowPhase phase, LevelFlow flow);

        // ---- Ganchos del Mapa del río y de la victoria (arquitectura de la tanda 4, §2; plan del Mapa, §4; tarea M1.3b). Solo
        // los declara la dueña de GameScreen.cs de cada ola. ----

        // Fondo de la parada (GameScreen.Stop.cs, M1.4): en el constructor, tras crear el panorama y antes de la garza, con la
        // escena como padre; lo que cree queda delante del panorama y detrás de la garza.
        partial void StopBuild(RectTransform scene);

        // Fondo de la parada: al final de LayoutBackdrop, con el horizonte (dp desde arriba), el canto de arriba del panel y si hay
        // tablero (sin él, la escena de antes del primer nivel).
        partial void StopLayoutBackdrop(float horizon, float panelTop, bool hasBoard);

        // Fondo de la parada: al final de OnLevelLoaded, junto a los demás *LevelLoaded (antes de la AF_LAYOUT level).
        partial void StopLevelLoaded(LevelFlow flow);

        // Fondo de la parada: al final de OnVictoryScreen, tras la ranura (la victoria con vista, M2.2).
        partial void StopVictoryShown(LevelFlow flow);

        // Fondo de la parada: en cada Tick, tras RevealTick.
        partial void StopTick(float dtMs);

        // Llegada a la parada (GameScreen.Arrival.cs, M1.6): al final del constructor, junto a LabelsBuild, CardsBuild y
        // RevealBuild.
        partial void ArrivalBuild();

        // Llegada: al final de OnLevelLoaded, tras StopLevelLoaded.
        partial void ArrivalLevelLoaded(LevelFlow flow);

        // Llegada: al final de OnVictoryScreen, tras la ranura (ya con la línea del árbitro) y el fondo.
        partial void ArrivalVictoryShown(LevelFlow flow);

        // Llegada: en cada Tick, tras StopTick.
        partial void ArrivalTick(float dtMs);

        // Llegada: al final de LayoutVictory, tras SlotLayout (la ranura ya colocada).
        partial void ArrivalLayoutVictory();

        // Llegada: botones visibles para AF_LAYOUT victory (EmitVictoryLayout, antes de emitir; «Ver mapa» desde M4.3).
        partial void ArrivalLayoutIds(List<string> ids, List<RectTransform> rects);

        // Llegada: en OnAdGate, tras SlotAdGate (Siguiente o el avance tras la Cosecha doble cierran la victoria).
        partial void ArrivalAdGate(LevelFlow flow);

        // Llegada: en OnVictoryHome, tras SlotHomePressed y antes de PressHome.
        partial void ArrivalHomePressed(LevelFlow flow);

        // Ranura del Dato del río (GameScreen.Victory.cs; 02 §6, Q7-A de D-017): en LayoutVictory, con Inicio ya colocado, antes
        // de fijar el alto del panel. y es el pie del área táctil de Inicio (dp desde el canto de arriba del panel): lo que sume
        // a y queda bajo Inicio, y el panel crece lo mismo hacia arriba (sigue anclado abajo), así que Siguiente sube.
        partial void SlotReserveBelowHome(ref float y);

        // Ranura: al final de LayoutVictory, tras LabelsLayoutVictory.
        partial void SlotLayout();

        // Ranura: al final de OnVictoryScreen, tras SlothVictoryShown (con SlothCanSpeak) y antes del fondo y la llegada: pide la
        // línea al árbitro (VictoryLineArbiter) y la reparte a sus proveedores.
        partial void SlotVictoryShown(LevelFlow flow);

        // Ranura: en cada Tick, tras ArrivalTick.
        partial void SlotTick(float dtMs);

        // Ranura: botones visibles para AF_LAYOUT victory (EmitVictoryLayout, antes de emitir).
        partial void SlotLayoutIds(List<string> ids, List<RectTransform> rects);

        // Ranura: en OnAdGate, antes de ArrivalAdGate: la victoria se cierra con Siguiente (punto común de river_fact_shown).
        partial void SlotAdGate(LevelFlow flow);

        // Ranura: en OnVictoryHome, antes de ArrivalHomePressed y de PressHome: la victoria se cierra con Inicio.
        partial void SlotHomePressed(LevelFlow flow);

        // TiTino en la victoria (TI6a): en el constructor, justo tras crear la victoria y antes del embudo, la pausa y la capa
        // superior; lo que cree queda delante de la victoria y detrás de ellos.
        partial void SlothBoxBuild();

        // TiTino: en OnVictoryScreen, antes de SlotVictoryShown, con la franja libre entre el pie de la cabecera (top) y la cima
        // de las cuias con su elevación (bottom), en dp desde arriba y con el tablero ya desplazado por ShiftToClear (propuesta
        // de TiTino d.4). De ahí sale SlothCanSpeak para el árbitro.
        partial void SlothVictoryShown(LevelFlow flow, float top, float bottom);

        // TiTino: en cada Tick, tras SlotTick.
        partial void SlothTick(float dtMs);

        // TiTino: al cerrar la victoria por cualquier camino (HideOverlays: carga de otro nivel, intro, reinicio; y la pantalla
        // de nivel que se oculta: Inicio o «El río sigue creciendo» tras AD_GATE), TiTino y su bocadillo se van.
        partial void SlothLeave();
    }
}
