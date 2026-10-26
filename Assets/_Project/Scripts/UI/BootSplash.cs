#nullable enable
using System;
using AF.GameFlow;
using AF.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Lo que pasó en el arranque (tarea TI6d; sonda <c>AF_STATE boot</c>, <see cref="DevProbe.Boot"/>): para medir en el
    /// teléfono el primer toque en el nivel 1 (&lt; 20 s, 09 §6) y si cabe el segundo de <c>sloth_wake</c> (06 §2.2).
    /// </summary>
    public sealed class BootReport
    {
        /// <summary>Primera apertura: no había perfil (<c>first_open</c>).</summary>
        public bool FirstOpen { get; internal set; }

        /// <summary>Pose de TiTino en la pantalla de arranque (<c>sloth_wake</c> o <c>sloth_idle</c>); null si no salió.</summary>
        public string? SlothPose { get; internal set; }

        /// <summary>Desde que se creó la pantalla hasta que terminó la carga (ms; <c>load_ms</c>).</summary>
        public long LoadMs { get; internal set; }

        /// <summary>Tiempo a la vista de la pantalla de arranque, de su creación a su corte (ms; <c>splash_ms</c>).</summary>
        public long SplashMs { get; internal set; }

        /// <summary>Lo que se vio del gesto de TiTino (reloj de su pose al cortar, ms; <c>sloth_ms</c>; 0 sin TiTino).</summary>
        public long SlothMs { get; internal set; }

        /// <summary>Espera añadida tras la carga (ms; <c>hold_ms</c>; 0 salvo en la primera apertura con TiTino).</summary>
        public long HoldMs { get; internal set; }

        /// <summary>
        /// El segundo de <c>sloth_wake</c> cupo en el objetivo de 20 s (<c>wake_fits</c>; <see cref="BootSplashTiming.Fits"/>):
        /// false si hacía falta esperar y no cabía (se anota para el dueño). True si no hacía falta esperar.
        /// </summary>
        public bool WakeFits { get; internal set; } = true;

        /// <summary>
        /// Desde el arranque hasta el corte de la pantalla (ms; <c>splash_end_ms</c>): en el teléfono, desde que arranca Unity
        /// (<see cref="Time.realtimeSinceStartupAsDouble"/>), sin el lanzamiento del proceso ni el de la Activity, que van antes
        /// (para el arranque en frío de 09 §6.1 se suma el <c>TotalTime</c> de <c>adb shell am start -W</c>); en el Editor, desde el
        /// Awake de la escena.
        /// </summary>
        public long SplashEndMs { get; internal set; }

        /// <summary>
        /// Desde el arranque hasta el primer toque del jugador en el juego (ms; <c>first_touch_ms</c>; el mismo origen que
        /// <see cref="SplashEndMs"/>: desde que arranca Unity), o −1 si aún no ha tocado. Cuenta el primer toque en una cuia, en el
        /// fondo del nivel o durante la intro, o «Jugar» en el inicio.
        /// </summary>
        public long FirstTouchMs { get; internal set; } = -1;

        /// <summary>Pantalla del primer toque (<c>level</c> o <c>lobby</c>; null si aún no ha tocado).</summary>
        public string? FirstTouchScreen { get; internal set; }

        /// <summary>Nivel del primer toque (el de la pantalla de nivel, o el del botón Jugar; 0 si aún no ha tocado).</summary>
        public int FirstTouchLevel { get; internal set; }
    }

    /// <summary>
    /// Pantalla de arranque (06 §2.2: «Splash de 1 pantalla (río + logotipo) con TiTino colgado de su embaúba»; D-016, T2-A;
    /// tarea TI6d), rehecha con la marca v2 a petición del dueño (añadido de D-051, 2026-10-25; D-045 «El río que se tiñe»), en su
    /// propio lienzo, por encima de todo, y <b>sin texto</b> (06 §2.1, regla 3: ni un componente de texto; el nombre es un dibujo).
    /// Es el último fotograma de la intro de la marca (<see cref="BrandIntro"/>) con su mundo alrededor:
    /// <list type="bullet">
    /// <item>El <b>emblema v2</b> (<see cref="BrandEmblemArt"/>, generado desde el SVG del icono): la selva con sus palmeras de açaí,
    /// el río blanco con sus orillas en curva, la cuia de vidrio con Taperebá, Aguaje y Açaí inclinada según D-006, el chorro en
    /// parábola, los aros del impacto y el açaí que se abre río abajo desde él. Son las mismas piezas de la intro, en el mismo sitio
    /// (<see cref="BrandIntro.PlaceEmblem"/>): al quitarse la intro no se mueve ni cambia de color nada de eso.</item>
    /// <item>El <b>mundo</b> alrededor, que aparece al desvanecerse un velo con el petróleo y el halo de la intro
    /// (<see cref="RevealMs"/>; de golpe con Reducir movimiento): la orilla de acá del icono, que sigue hacia abajo en el suelo; el
    /// <b>nombre del logo</b> pegado al emblema, como en el logo vertical (<see cref="BrandWordmark"/>: Baloo 2 ExtraBold en marfil,
    /// como el logo «oscuro» de la v2), nítido a cualquier densidad; el follaje en silueta de las esquinas de abajo y unas
    /// luciérnagas.</item>
    /// <item><b>TiTino</b> (<see cref="SlothRig"/>; propuesta c.4: 140-180 dp en el arranque) colgado de su rama del inicio
    /// (<see cref="ProceduralArt.HomeBranch"/>, D-050), que entra desde la derecha, por encima de todo el emblema (nunca tapa la cuia
    /// ni el chorro): <c>sloth_wake</c> en la primera apertura y <c>sloth_idle</c> en las demás (<see cref="ShowSloth"/>). Su tamaño
    /// sale del aire entre la zona segura y lo más alto del emblema (<see cref="SlothFitDp"/>). Sale en todos los builds (su dibujo
    /// por código o sus PNG: <see cref="MapArtRule"/>, D-052), y nunca con <c>ui_sloth_enabled</c> apagado
    /// (<see cref="SlothEnabled"/>): entonces la pantalla sale igual, sin TiTino ni su rama.</item>
    /// <item>Pantallas bajas (alto &lt; <see cref="TallAspect"/> × ancho, como 16:9): el emblema encoge un poco y baja para que TiTino
    /// quepa encima, y el nombre se estrecha.</item>
    /// <item>Cuánto dura lo decide <see cref="BootSplashTiming"/> (GameBootstrap): se corta al terminar la carga, salvo el
    /// segundo de <c>sloth_wake</c> de la primera apertura si cabe en el objetivo de 20 s.</item>
    /// </list>
    /// Con la intro (<c>withIntro</c>, lo que hace GameBootstrap), la intro va encima de todo mientras el juego carga y se quita al
    /// terminar la carga (<see cref="EndIntro"/>); mientras se ve, el gesto de TiTino no avanza (su reloj se queda en 0), para que
    /// su segundo de <c>sloth_wake</c> se vea entero después, y el mundo no se pinta: sus texturas se hacen en el primer fotograma
    /// tras quitarse la intro (en las aperturas que no esperan, la pantalla se corta antes y nunca se hacen). Las del emblema se
    /// hacen al crear la pantalla, porque la intro las enseña desde su primer fotograma.
    /// El fondo traga los toques mientras se ve (aún no hay nada que tocar). Las texturas son suyas y se destruyen con
    /// <see cref="Dispose"/>; las piezas de TiTino y su rama son de <see cref="ProceduralArt"/> y no se tocan. Nada asigna memoria
    /// por fotograma.
    /// </summary>
    public sealed class BootSplash : IDisposable
    {
        /// <summary>Orden del lienzo: por encima de todos los demás (Ajustes 40, tienda 50, anuncio 60, aviso del perfil 70).</summary>
        public const int SortingOrder = 90;

        /// <summary>Tamaño S máximo de TiTino en el arranque (propuesta de TiTino c.4: 140-180 dp) [P].</summary>
        public const float SlothSizeDp = 160f;

        /// <summary>Tamaño S mínimo de TiTino, aunque no quepa entero sobre la selva (pantallas muy bajas) [P].</summary>
        public const float SlothMinDp = 120f;

        /// <summary>
        /// Caída del cuerpo bajo la rama (S): el borde inferior de la caja del torso colgado (0,60 + 0,56 × 1,02 / 2 ≈ 0,886 S en
        /// lo más hondo de la respiración, <c>SlothRig.Hang</c>), con un poco de margen [P].
        /// </summary>
        public const float SlothDropS = 0.90f;

        /// <summary>Aire mínimo entre el fondo de TiTino y lo más alto del emblema (dp) [P].</summary>
        public const float SlothClearDp = 16f;

        /// <summary>Proporción alto/ancho por debajo de la cual la pantalla es baja (16:9 = 1,78; 20:9 = 2,22) [P].</summary>
        public const float TallAspect = 2.0f;

        /// <summary>Lo que tarda el mundo en entrar al quitarse la intro (ms; 0 con Reducir movimiento) [P].</summary>
        public const float RevealMs = 300f;

        /// <summary>Escala de la rama del inicio en el arranque, donde TiTino es mayor (inicio: 76-92 dp; aquí, 140-160) [P].</summary>
        public const float BranchScale = 1.3f;

        /// <summary>Ancho máximo de la caja del nombre (dp) [P].</summary>
        public const float WordmarkMaxDp = 296f;

        /// <summary>Ancho máximo de la caja del nombre en las pantallas bajas (dp) [P].</summary>
        public const float WordmarkMaxShortDp = 264f;

        // Medidas en dp [P]: margen lateral del nombre, su distancia a la orilla del emblema (pantalla alta y baja), lo alto de la
        // rama sobre la zona segura, el aire entre TiTino y el emblema cuando sobra sitio, el follaje de las esquinas (lado máximo y
        // aire con el nombre), el matorral y las luciérnagas.
        private const float WordmarkMarginDp = 32f;
        private const float WordmarkGapDp = 10f;
        private const float WordmarkGapShortDp = 6f;
        private const float BranchTopDp = 36f;
        private const float SlothGapDp = 20f;
        private const float FoliageDp = 232f;
        private const float FoliageGapDp = 4f;
        private const float HedgeDp = 96f;
        private const float FireflyGlowDp = 26f;
        private const float FireflyCoreDp = 5f;

        // Las luciérnagas: x (fracción del ancho) e y (fracción del suelo, del nombre al borde de abajo; las negativas, dp sobre lo
        // más alto del emblema), con su fase [P].
        private static readonly float[] FireflyX = { 0.14f, 0.86f, 0.40f, 0.66f, 0.10f, 0.52f };
        private static readonly float[] FireflyY = { 0.10f, 0.22f, 0.40f, 0.30f, -26f, 0.70f };
        private static readonly float[] FireflyPhase = { 0f, 1.9f, 3.7f, 0.8f, 2.6f, 4.4f };

        // El nombre: marfil del logo «oscuro» de la v2 (generar.py, MARFIL_TXT #F4EEDF), con un poco de luz arriba, y su sombra.
        private static readonly Color WordmarkTop = Palette.Hex(0xFFF8EA);
        private static readonly Color WordmarkBottom = Palette.Hex(0xEBDDC2);
        private static readonly Color WordmarkShadow = new Color(0x04 / 255f, 0x18 / 255f, 0x18 / 255f, 0.55f);

        private readonly OverlayCanvas _canvas;
        private readonly Texture2D?[] _textures = new Texture2D?[8];
        private readonly Sprite?[] _sprites = new Sprite?[8];
        private readonly Image _sky;
        private readonly Image _halo;
        private readonly Image _base;
        private readonly Image _acai;
        private readonly Image _bank;
        private readonly Image _impact;
        private readonly Image _stream;
        private readonly Image _cuia;
        private readonly RectTransform _front;
        private readonly CanvasGroup _veil;
        private readonly Image _veilHalo;
        private readonly double _createdAt;
        private readonly BrandIntro? _intro;
        private BrandEmblemArt _emblem;
        private Image? _ground;
        private Image? _hedge;
        private Image? _foliageLeft;
        private Image? _foliageRight;
        private Image? _wordmark;
        private Image[]? _fireflyGlow;
        private Image[]? _fireflyCore;
        private Image? _branch;
        private Vector4 _wordmarkPadPx;
        private float _wordmarkScale;
        private SlothRig? _sloth;
        private bool _withSloth;
        private bool _sceneBuilt;
        private bool _reduceMotion;
        private float _revealMs = -1f;
        private float _clockMs;
        private bool _disposed;

        /// <summary>
        /// Interruptor de los tests (null = lo de siempre: la pantalla sale en cada arranque en frío). Los tests PlayMode que no
        /// tratan de ella la apagan (<c>PlayModeHelpers</c>, al cargar la escena) y los que la prueban la encienden y la vuelven
        /// a null en su TearDown.
        /// </summary>
        public static bool? EnabledOverride { get; set; }

        /// <summary>La pantalla de arranque sale en los arranques en frío (<see cref="EnabledOverride"/> o, sin él, siempre).</summary>
        public static bool Enabled => EnabledOverride ?? true;

        /// <summary>Interruptor de los tests para TiTino en el arranque (null = <see cref="GameConfig.UiSlothEnabled"/>).</summary>
        public static bool? SlothEnabledOverride { get; set; }

        /// <summary>
        /// TiTino sale en la pantalla de arranque: <c>ui_sloth_enabled</c> (08 §9; D-016: el interruptor de cada gancho, como el de
        /// la victoria), salvo que un test lo fuerce (<see cref="SlothEnabledOverride"/>).
        /// </summary>
        public static bool SlothEnabled => SlothEnabledOverride ?? GameConfig.UiSlothEnabled;

        /// <summary>
        /// Se espera a TiTino en la pantalla (<see cref="SlothEnabled"/> y su arte: el de código, en todos los builds (D-052), o
        /// los dos PNG finales, las piezas y la rama; la regla de <see cref="ProceduralArt.SlothRigArt()"/>).
        /// </summary>
        public static bool SlothExpected =>
            SlothEnabled
            && (MapArtRule.ShowProvisional
                || (ArtSlots.Find(ProceduralArt.SlothPartsSlot) != null && ArtSlots.Find(ProceduralArt.SlothBranchSlot) != null));

        /// <summary>
        /// Solo para los tests: ms que se suman al reloj del arranque (<c>GameBootstrap.SinceStartupMs</c>), para recorrer el caso
        /// «el segundo de <c>sloth_wake</c> no cabe en los 20 s». Siempre 0 fuera de ellos (y lo vuelven a 0 en su TearDown).
        /// </summary>
        public static long TestStartupOffsetMs { get; set; }

        /// <summary>
        /// Crea la pantalla, ya a la vista, en un lienzo propio dentro de <paramref name="host"/>, esperando a TiTino
        /// (<paramref name="withSloth"/>) o sin él. Con <paramref name="withIntro"/>, la intro de la marca (<see cref="BrandIntro"/>)
        /// encima de todo, en su primer fotograma hasta <see cref="BeginIntro"/>; el mundo, sin pintar hasta que se quite. Sin
        /// intro, la pantalla nace entera.
        /// </summary>
        public BootSplash(Transform host, bool withSloth = true, bool withIntro = false)
        {
            _createdAt = Time.realtimeSinceStartupAsDouble;
            _withSloth = withSloth;
            _canvas = new OverlayCanvas("BootSplashCanvas", host, SortingOrder);
            RectTransform root = _canvas.Root;

            // El emblema: sus piezas, a la escala de la pantalla, las comparte la intro (su último fotograma es este emblema).
            Sprite halo = Own(0, SoftDisc(128));
            _emblem = BrandEmblemArt.Create(_canvas.Width, _canvas.Height, _canvas.Scale);

            UiBuilder.Stretch(UiBuilder.Image("TouchBlocker", root, null, Color.clear, raycast: true).rectTransform); // traga los toques: aún no hay nada que tocar
            _sky = UiBuilder.Image("Sky", root, null, BrandIntro.Petroleum);
            UiBuilder.Stretch(_sky.rectTransform);
            _halo = UiBuilder.Image("Halo", root, halo, BrandIntro.HaloColor);
            _front = Group("WorldFront", root);

            // El velo: el petróleo y el halo de la intro, por encima del mundo y por debajo del emblema. Mientras está opaco, la
            // pantalla es el último fotograma de la intro; al quitarse la intro se desvanece y deja ver el mundo, sin tocar el
            // emblema ni mezclar piezas que se solapan (TiTino, el follaje) a medio fundido.
            RectTransform veil = Group("Veil", root);
            _veil = veil.gameObject.AddComponent<CanvasGroup>();
            _veil.blocksRaycasts = false;
            _veil.interactable = false;
            UiBuilder.Stretch(UiBuilder.Image("VeilSky", veil, null, BrandIntro.Petroleum).rectTransform);
            _veilHalo = UiBuilder.Image("VeilHalo", veil, halo, BrandIntro.HaloColor);

            // El emblema, en el orden del icono: la base, el açaí, la orilla de acá (que entra con el mundo: su alfa), el impacto,
            // el chorro y la cuia.
            _base = UiBuilder.Image("EmblemBase", root, null, Color.white);
            _acai = UiBuilder.Image("EmblemAcai", root, null, Color.white);
            _bank = UiBuilder.Image("EmblemBank", root, null, Color.white);
            _bank.enabled = false;
            _impact = UiBuilder.Image("EmblemImpact", root, null, Color.white);
            _stream = UiBuilder.Image("EmblemStream", root, null, Color.white);
            _cuia = UiBuilder.Image("EmblemCuia", root, null, Color.white);
            if (withIntro)
            {
                _intro = new BrandIntro(root, _createdAt, halo, _emblem); // la última: por encima de todo; el mismo reloj
                _front.gameObject.SetActive(false);
                SetReveal(0f);
            }
            else
            {
                EnsureScene();
                SetReveal(1f);
            }

            Layout();
        }

        /// <summary>Lienzo propio (capturas de prueba).</summary>
        public Canvas Canvas => _canvas.Canvas;

        /// <summary>La pantalla está a la vista.</summary>
        public bool Visible => !_disposed && _canvas.Canvas.gameObject.activeSelf;

        /// <summary>La intro de la marca (null sin ella); sigue aquí tras <see cref="EndIntro"/>, ya fuera de la vista.</summary>
        public BrandIntro? Intro => _intro;

        /// <summary>La intro de la marca está a la vista (tapa el resto de la pantalla).</summary>
        public bool IntroVisible => _intro != null && _intro.Visible;

        /// <summary>TiTino en la pantalla (null hasta <see cref="ShowSloth"/>, o sin su arte).</summary>
        public SlothRig? Sloth => _sloth;

        /// <summary>TiTino está a la vista.</summary>
        public bool SlothVisible => _sloth != null && _sloth.Visible;

        /// <summary>Lo que lleva su pose a la vista (reloj de la pose, ms; 0 sin TiTino).</summary>
        public float SlothClockMs => SlothVisible ? _sloth!.ClockMs : 0f;

        /// <summary>Desde que se creó la pantalla (ms de tiempo real).</summary>
        public long AgeMs => (long)((Time.realtimeSinceStartupAsDouble - _createdAt) * 1000.0);

        /// <summary>La pantalla espera a TiTino o lo tiene; false: sin TiTino ni su rama (el resto, igual).</summary>
        public bool ComposedForSloth => _withSloth;

        /// <summary>La pantalla es baja (alto &lt; <see cref="TallAspect"/> × ancho, como 16:9).</summary>
        public bool ShortScreen => _canvas.Height < TallAspect * _canvas.Width;

        /// <summary>El mundo alrededor del emblema ya está hecho (sus texturas).</summary>
        public bool SceneBuilt => _sceneBuilt;

        /// <summary>Cuánto ha entrado el mundo (0 bajo la intro, 1 entero).</summary>
        public float Reveal => 1f - _veil.alpha;

        /// <summary>Lo más alto del emblema (dp desde arriba: las palmeras, la cuia y el chorro): TiTino cuelga por encima.</summary>
        public float CanopyDp => _emblem.TopDp;

        /// <summary>Donde acaba la orilla de acá del emblema (dp desde arriba); debajo, el suelo y el nombre.</summary>
        public float EmblemBottomDp => _emblem.BankBottomDp;

        /// <summary>El rectángulo de la cuia (dp, desde arriba a la izquierda; con su sombra, su pie y su boca).</summary>
        public Rect CuiaRect => _emblem.DpRect(BrandEmblem.Cuia);

        /// <summary>El rectángulo del chorro (dp, desde arriba a la izquierda).</summary>
        public Rect StreamRect => _emblem.DpRect(BrandEmblem.Stream);

        /// <summary>La caja de los contornos del nombre (dp, desde arriba a la izquierda), sin el margen de su sombra.</summary>
        public Rect WordmarkBox
        {
            get
            {
                float w = Mathf.Min(_canvas.Width - (2f * WordmarkMarginDp), ShortScreen ? WordmarkMaxShortDp : WordmarkMaxDp);
                float h = w * BrandWordmark.Aspect;
                float top = EmblemBottomDp + (ShortScreen ? WordmarkGapShortDp : WordmarkGapDp);
                return new Rect((_canvas.Width - w) * 0.5f, top, w, h);
            }
        }

        /// <summary>El nombre, si ya está hecho (tests y capturas).</summary>
        public Image? Wordmark => _wordmark;

        /// <summary>La rama del inicio de la que cuelga TiTino (null sin TiTino o sin rama).</summary>
        public Image? Branch => _branch;

        /// <summary>
        /// Tamaño S de TiTino en esta pantalla (dp): el que deja <see cref="SlothClearDp"/> de aire entre su fondo
        /// (<see cref="SlothDropS"/> bajo la rama) y la copa de la selva con la rama lo más alta que deja la zona segura, entre
        /// <see cref="SlothMinDp"/> y <see cref="SlothSizeDp"/>.
        /// </summary>
        public float SlothFitDp
        {
            get
            {
                float room = CanopyDp - SlothClearDp - BranchMinDp;
                return Mathf.Clamp(room / SlothDropS, SlothMinDp, SlothSizeDp);
            }
        }

        /// <summary>
        /// Dónde se agarra TiTino a la rama (dp, x desde la izquierda e y desde arriba): a la derecha, sobre el açaí y lejos de la
        /// cuia, y con <see cref="SlothGapDp"/> de aire sobre el emblema cuando sobra sitio.
        /// </summary>
        public Vector2 BranchAnchor
        {
            get
            {
                float s = _sloth?.Size ?? SlothFitDp;
                float y = Mathf.Max(BranchMinDp, CanopyDp - SlothGapDp - (SlothDropS * s));
                float x = _canvas.Width - Mathf.Round(Mathf.Max((0.5f * s) + 14f, 0.30f * _canvas.Width));
                return new Vector2(x, Mathf.Round(y));
            }
        }

        /// <summary>Todo lo que se pinta (tests: ningún componente de texto, ningún raycast salvo el fondo).</summary>
        public RectTransform Root => _canvas.Root;

        private float BranchMinDp => _canvas.SafeTop + BranchTopDp;

        /// <summary>
        /// Pone a TiTino colgado de <paramref name="branch"/> (la rama del inicio, <see cref="ProceduralArt.HomeBranch"/>, que entra
        /// volteada desde el borde derecho; sin ella, la de su rig) con <c>sloth_wake</c> (<paramref name="firstOpen"/>) o <c>sloth_idle</c>, al tamaño que cabe
        /// (<see cref="SlothFitDp"/>). Sin piezas (<paramref name="art"/> null: <see cref="ProceduralArt.SlothRigArt()"/>) no crea
        /// nada, queda sin TiTino y devuelve false.
        /// </summary>
        public bool ShowSloth(SlothArt? art, Sprite? branch, bool firstOpen, bool reduceMotion)
        {
            if (_disposed || _sloth != null)
            {
                return _sloth != null;
            }

            _withSloth = true;
            _reduceMotion = reduceMotion;
            _sloth = SlothRig.Create(_front, art, SlothFitDp);
            if (_sloth == null)
            {
                _withSloth = false;
                Layout();
                return false;
            }

            if (branch != null)
            {
                _branch = UiBuilder.Image("TitinoBranch", _front, branch, Color.white);
                _branch.transform.SetSiblingIndex(_sloth.Root.GetSiblingIndex()); // detrás de TiTino
                _sloth.BranchImage.enabled = false; // la rama es la del inicio (D-050), no la de su rig
            }

            Layout();
            _sloth.Play(firstOpen ? SlothPoseId.Wake : SlothPoseId.Idle, reduceMotion);
            if (branch != null)
            {
                _sloth.BranchImage.enabled = false;
            }

            return true;
        }

        /// <summary>
        /// Un fotograma: con la intro a la vista, la intro (con el tiempo real) y TiTino quieto; sin ella, el mundo entra (la
        /// primera vez se hacen sus texturas), la pose de TiTino avanza <paramref name="dtMs"/> ms y las luciérnagas laten. Rehace
        /// la composición si cambió la pantalla. Sin asignaciones salvo la primera vez tras la intro.
        /// </summary>
        public void Tick(float dtMs)
        {
            if (_disposed)
            {
                return;
            }

            if (_canvas.Measure())
            {
                Layout();
            }

            if (_intro != null && _intro.Visible)
            {
                _intro.Tick();
                return;
            }

            if (!_sceneBuilt)
            {
                PrepareWorld();
            }
            else if (_revealMs >= 0f && _revealMs < RevealMs)
            {
                _revealMs = Mathf.Min(RevealMs, _revealMs + dtMs);
                SetReveal(Ease(_revealMs / RevealMs));
            }

            _clockMs += dtMs;
            Pulse();
            _sloth?.Tick(dtMs);
        }

        /// <summary>
        /// Hace el mundo ya quitada la intro, con el velo aún opaco (la pantalla sigue siendo el último fotograma de la intro), para
        /// que su coste no se coma el segundo de <c>sloth_wake</c>: GameBootstrap lo llama cuando va a esperar, antes de medir la
        /// espera. Si no se llama, lo hace el primer <see cref="Tick"/> tras la intro. No hace nada con la intro a la vista ni dos veces.
        /// </summary>
        public void PrepareWorld()
        {
            if (_disposed || _sceneBuilt || IntroVisible)
            {
                return;
            }

            EnsureScene();
            Layout();
            _revealMs = _reduceMotion ? RevealMs : 0f;
            SetReveal(_reduceMotion ? 1f : 0f);
        }

        /// <summary>Arranca la animación de la intro, con Reducir movimiento ya resuelto (no hace nada sin intro).</summary>
        public void BeginIntro(bool reduceMotion)
        {
            if (!_disposed)
            {
                _reduceMotion = reduceMotion;
                _intro?.Begin(reduceMotion);
            }
        }

        /// <summary>Quita la intro al terminar la carga y devuelve lo que se vio (null sin intro o si ya se quitó).</summary>
        public BrandIntroReport? EndIntro()
        {
            return _intro?.End();
        }

        /// <summary>Lleva la pose de TiTino al instante <paramref name="ms"/> (capturas deterministas).</summary>
        public void Seek(float ms)
        {
            _sloth?.Seek(ms);
        }

        /// <summary>
        /// Deja el mundo a los <paramref name="ms"/> ms de su entrada (capturas deterministas): lo hace si aún no está, sin
        /// tocar la intro.
        /// </summary>
        public void SeekReveal(float ms)
        {
            if (_disposed)
            {
                return;
            }

            if (!_sceneBuilt)
            {
                EnsureScene();
                Layout();
            }

            _revealMs = Mathf.Clamp(ms, 0f, RevealMs);
            SetReveal(Ease(_revealMs / RevealMs));
        }

        /// <summary>Corta la pantalla y libera lo suyo (el lienzo y sus texturas).</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _sloth = null;
            _intro?.Dispose();
            if (_canvas.Canvas != null)
            {
                UnityEngine.Object.Destroy(_canvas.Canvas.gameObject);
            }

            for (int i = 0; i < _textures.Length; i++)
            {
                DestroyOwned(i);
            }

            _emblem.Dispose();
        }

        // Hace el mundo alrededor del emblema: sus texturas y sus imágenes (el suelo, las luciérnagas, el matorral, el follaje y el
        // nombre, bajo el velo) y la orilla de acá del emblema. TiTino y su rama van también bajo el velo, al final.
        private void EnsureScene()
        {
            if (_sceneBuilt)
            {
                return;
            }

            _sceneBuilt = true;
            _front.gameObject.SetActive(true);
            _bank.enabled = true;
            _emblem.EnsureBank();
            Sprite ground = Own(1, BootSplashArt.Ground(128));
            Sprite foliage = Own(2, BootSplashArt.CornerFoliage(FoliageDp, ProceduralArt.UiPxPerDp));
            Sprite hedge = Own(3, BootSplashArt.Hedge(360f, HedgeDp, 2.5f));
            Sprite glow = _sprites[0]!;

            _ground = UiBuilder.Image("Ground", _front, ground, Color.white);
            _fireflyGlow = new Image[FireflyX.Length];
            _fireflyCore = new Image[FireflyX.Length];
            for (int i = 0; i < FireflyX.Length; i++)
            {
                _fireflyGlow[i] = UiBuilder.Image("FireflyGlow" + i, _front, glow, ProceduralArt.FireflyLight);
                _fireflyCore[i] = UiBuilder.Image("Firefly" + i, _front, glow, ProceduralArt.FireflyCore);
            }

            _hedge = UiBuilder.Image("Hedge", _front, hedge, Color.white);
            _foliageLeft = UiBuilder.Image("FoliageLeft", _front, foliage, Color.white);
            _foliageRight = UiBuilder.Image("FoliageRight", _front, foliage, Color.white);
            _wordmark = UiBuilder.Image("Wordmark", _front, null, Color.white);
            if (_sloth != null)
            {
                // TiTino (y su rama) se crearon antes que el mundo: por encima de todo lo del mundo.
                _branch?.transform.SetAsLastSibling();
                _sloth.Root.SetAsLastSibling();
            }

            Pulse();
        }

        // Rasteriza el nombre a la escala de la pantalla (otra vez solo si cambia).
        private void RenderWordmark(Rect box)
        {
            if (_wordmark == null)
            {
                return;
            }

            float scale = Mathf.Max(1f, _canvas.Scale);
            if (_wordmark.sprite == null || Mathf.Abs(scale - _wordmarkScale) > 0.01f)
            {
                _wordmarkScale = scale;
                DestroyOwned(4);
                int widthPx = Mathf.RoundToInt(box.width * scale);
                Texture2D tex = BrandWordmark.Render(widthPx, WordmarkTop, WordmarkBottom, WordmarkShadow, 2.5f * scale, 1.6f * scale, out _wordmarkPadPx);
                _wordmark.sprite = Own(4, tex);
            }

            float k = 1f / _wordmarkScale;
            UiBuilder.Place(
                _wordmark.rectTransform,
                box.x - (_wordmarkPadPx.x * k),
                box.y - (_wordmarkPadPx.y * k),
                box.width + ((_wordmarkPadPx.x + _wordmarkPadPx.z) * k),
                box.height + ((_wordmarkPadPx.y + _wordmarkPadPx.w) * k));
        }

        // Composición en dp (origen arriba a la izquierda): el emblema de la intro en su sitio (rehecho si cambió la pantalla); bajo
        // él, el suelo con el nombre pegado a la orilla, el matorral, el follaje de las esquinas y las luciérnagas; y TiTino colgado
        // de la rama del inicio a la derecha, por encima del emblema.
        private void Layout()
        {
            float w = _canvas.Width;
            float h = _canvas.Height;
            if (!_emblem.Fits(w, h, _canvas.Scale))
            {
                BrandEmblemArt old = _emblem;
                _emblem = BrandEmblemArt.Create(w, h, _canvas.Scale);
                if (_sceneBuilt)
                {
                    _emblem.EnsureBank();
                }

                _intro?.Layout(_emblem);
                old.Dispose();
            }

            BrandIntro.PlaceEmblem(_halo, _base, _acai, _impact, _stream, _cuia, _emblem);
            RectTransform halo = _halo.rectTransform;
            UiBuilder.Place(_veilHalo.rectTransform, halo.anchoredPosition.x, -halo.anchoredPosition.y, halo.sizeDelta.x, halo.sizeDelta.y);
            _intro?.Layout(_emblem);

            if (_sceneBuilt)
            {
                _emblem.Place(_bank, BrandEmblem.Bank);
                float groundTop = EmblemBottomDp - 1f;
                UiBuilder.Place(_ground!.rectTransform, 0f, groundTop, w, h - groundTop);
                Rect box = WordmarkBox;
                RenderWordmark(box);

                // El follaje de las esquinas, tan grande como deja el nombre, con el matorral a lo ancho del pie.
                float fol = Mathf.Clamp(h - box.yMax - FoliageGapDp, 120f, Mathf.Min(FoliageDp, w * 0.64f));
                float hedge = Mathf.Min(HedgeDp, fol * 0.5f);
                UiBuilder.Place(_hedge!.rectTransform, 0f, h - hedge, w, hedge);
                UiBuilder.Place(_foliageLeft!.rectTransform, -6f, h - fol + 4f, fol, fol);
                RectTransform right = _foliageRight!.rectTransform; // la misma, volteada desde su centro
                UiBuilder.Place(right, w + 6f - fol, h - fol + 4f, fol, fol);
                right.pivot = new Vector2(0.5f, 1f);
                right.anchoredPosition = new Vector2(w + 6f - (fol * 0.5f), -(h - fol + 4f));
                right.localScale = new Vector3(-1f, 1f, 1f);
                for (int i = 0; i < _fireflyGlow!.Length; i++)
                {
                    float fx = FireflyX[i] * w;
                    float fy = FireflyY[i] < 0f ? CanopyDp - FireflyY[i] : box.yMax + (FireflyY[i] * (h - box.yMax));
                    UiBuilder.Place(_fireflyGlow[i].rectTransform, fx - (FireflyGlowDp * 0.5f), fy - (FireflyGlowDp * 0.5f), FireflyGlowDp, FireflyGlowDp);
                    UiBuilder.Place(_fireflyCore![i].rectTransform, fx - (FireflyCoreDp * 0.5f), fy - (FireflyCoreDp * 0.5f), FireflyCoreDp, FireflyCoreDp);
                }
            }

            if (_sloth != null)
            {
                float s = SlothFitDp;
                if (Mathf.Abs(_sloth.Size - s) > 0.01f)
                {
                    _sloth.SetArt(_sloth.Art, s); // otra pantalla, otro aire: el mismo gesto a otro tamaño
                    _sloth.Seek(_sloth.ClockMs);
                }

                Vector2 anchor = BranchAnchor;
                _sloth.Place(anchor.x, anchor.y);
                if (_branch != null)
                {
                    // La rama del inicio, volteada, desde el borde derecho, con su línea media en el agarre de TiTino.
                    float bw = ProceduralArt.HomeBranchWidthDp * BranchScale;
                    float bh = ProceduralArt.HomeBranchHeightDp * BranchScale;
                    float line = ProceduralArt.HomeBranchLineAt((w - anchor.x) / BranchScale) * BranchScale;
                    RectTransform rt = _branch.rectTransform;
                    UiBuilder.Place(rt, w, anchor.y - line, bw, bh);
                    rt.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
        }

        // Las luciérnagas laten despacio (quietas con Reducir movimiento). Sin asignaciones.
        private void Pulse()
        {
            if (_fireflyGlow == null)
            {
                return;
            }

            for (int i = 0; i < _fireflyGlow.Length; i++)
            {
                float beat = _reduceMotion ? 0.7f : 0.55f + (0.45f * Mathf.Sin((_clockMs * 0.0021f) + FireflyPhase[i]));
                Color g = ProceduralArt.FireflyLight;
                _fireflyGlow[i].color = new Color(g.r, g.g, g.b, 0.42f * beat);
                Color c = ProceduralArt.FireflyCore;
                _fireflyCore![i].color = new Color(c.r, c.g, c.b, 0.55f + (0.45f * beat));
            }
        }

        private void SetReveal(float alpha)
        {
            _veil.alpha = 1f - alpha;
            _veil.gameObject.SetActive(alpha < 1f);
            _bank.color = new Color(1f, 1f, 1f, alpha);
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - (2f * t));
        }

        private static RectTransform Group(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            UiBuilder.Stretch(rt);
            return rt;
        }

        private Sprite Own(int slot, Texture2D tex)
        {
            return Own(slot, tex, new Vector2(0.5f, 0.5f));
        }

        private Sprite Own(int slot, Texture2D tex, Vector2 pivot)
        {
            _textures[slot] = tex;
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivot, 100f);
            sprite.name = tex.name;
            _sprites[slot] = sprite;
            return sprite;
        }

        private void DestroyOwned(int slot)
        {
            if (_sprites[slot] != null)
            {
                UnityEngine.Object.Destroy(_sprites[slot]);
                _sprites[slot] = null;
            }

            if (_textures[slot] != null)
            {
                UnityEngine.Object.Destroy(_textures[slot]);
                _textures[slot] = null;
            }
        }

        // Disco blanco que se apaga hacia el borde (el halo del emblema y las luciérnagas).
        private static Texture2D SoftDisc(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AF_BootGlow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude / r;
                    float a = Mathf.Clamp01(1f - d);
                    px[(y * size) + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
