#nullable enable
using System;
using AF.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>Lo que se vio de la intro de la marca (D-051, punto 3; sonda <c>AF_STATE brand_intro</c>, <see cref="DevProbe.BrandIntro"/>).</summary>
    public sealed class BrandIntroReport
    {
        /// <summary>Tiempo a la vista, desde que se creó hasta que terminó la carga (ms reales; <c>intro_ms</c>).</summary>
        public long IntroMs { get; internal set; }

        /// <summary>Hasta dónde llegó la animación (ms de su reloj, como mucho <see cref="BrandIntro.AnimMs"/>; <c>shown_ms</c>).</summary>
        public long ShownMs { get; internal set; }

        /// <summary>
        /// Se quitó antes de verse entera (<c>cut</c>): desde el añadido de D-051 del 2026-10-26 solo pasa si el arranque se para antes
        /// (catálogo roto, banda &lt;13); en un arranque normal, siempre false (false también con Reducir movimiento).
        /// </summary>
        public bool Cut { get; internal set; }

        /// <summary>Lo que se vio «Cargando…» bajo el nombre, porque la carga duró más que la intro (ms; <c>loading_ms</c>; 0 si no salió).</summary>
        public long LoadingMs { get; internal set; }

        /// <summary>La espera a la intro tras la carga llegó a su tope de seguridad y se quitó antes (<c>wait_capped</c>; no debería).</summary>
        public bool WaitCapped { get; internal set; }

        /// <summary>Fotogramas que llegaron a pintarse con la intro a la vista (<c>frames</c>).</summary>
        public int Frames { get; internal set; }

        /// <summary>Con Reducir movimiento: la intro salió quieta, en su último fotograma (<c>reduce_motion</c>).</summary>
        public bool ReduceMotion { get; internal set; }

        /// <summary>La animación llegó a empezar (se supo si había Reducir movimiento); false si el arranque se paró antes.</summary>
        public bool Begun { get; internal set; }

        /// <summary>De la creación de la pantalla a su primer fotograma a la vista (ms reales; -1 si no llegó; <c>first_frame_ms</c>).</summary>
        public long FirstFrameMs { get; internal set; } = -1;

        /// <summary>
        /// Desde que arrancó Unity hasta que se creó la pantalla de arranque (ms; <c>start_ms</c>): en el teléfono, el arranque del
        /// motor que no depende del juego (salvo lo poco del Awake). Lo pone GameBootstrap; -1 sin él.
        /// </summary>
        public long StartMs { get; internal set; } = -1;

        /// <summary>Lo que costó tener la escena de la marca (ms; dibujarla o leerla de la caché; <c>emblem_ms</c>).</summary>
        public long EmblemMs { get; internal set; }

        /// <summary>De dónde salió la escena (<c>emblem_cache</c>: <c>off</c>, <c>miss</c> o <c>hit</c>).</summary>
        public BrandEmblemCacheResult EmblemCache { get; internal set; }

        /// <summary>Fotogramas que GameBootstrap dejó pintar entre bloques de la carga para que la intro se viera moverse (<c>yields</c>).</summary>
        public int Yields { get; internal set; }

        /// <summary>
        /// Jornada 4 (2026-10-28): de la creación de la pantalla a cuando se supo que su primer fotograma ya estaba en la pantalla
        /// (la GPU lo terminó: <see cref="BrandIntro.VisibleBy"/>; ms reales; -1 si no llegó; <c>visible_ms</c>). Es una cota por
        /// arriba: se mira en cada fotograma y en cada hueco de la carga.
        /// </summary>
        public long VisibleMs { get; internal set; } = -1;

        /// <summary>Cómo se supo (<c>visible_by</c>: <c>gpu</c>, <c>cap</c>, <c>no_readback</c> o <c>none</c>).</summary>
        public BrandIntroVisibleBy VisibleBy { get; internal set; }

        /// <summary>De la creación de la pantalla a cuando empezó a moverse (el chorro sale de la cuia; ms reales; -1 si no empezó; <c>play_ms</c>).</summary>
        public long PlayMs { get; internal set; } = -1;

        /// <summary>Fotogramas pintados mientras se movía, del primer movimiento al nombre entero (<c>motion_frames</c>).</summary>
        public int MotionFrames { get; internal set; }

        /// <summary>Percentil 95 del tiempo entre fotogramas mientras se movía (ms; -1 sin datos; <c>frame_p95_ms</c>).</summary>
        public float FrameP95Ms { get; internal set; } = -1f;

        /// <summary>El mayor tiempo entre fotogramas mientras se movía (ms; -1 sin datos; <c>frame_max_ms</c>), sin las pausas.</summary>
        public float FrameMaxMs { get; internal set; } = -1f;

        /// <summary>
        /// Huecos entre fotogramas de más de <see cref="BrandIntro.MotionPauseMs"/> mientras se movía (la app a segundo plano, un
        /// bloqueo): no cuentan en <see cref="FrameP95Ms"/> ni en <see cref="FrameMaxMs"/> (<c>motion_pauses</c>).
        /// </summary>
        public int MotionPauses { get; internal set; }

        /// <summary>
        /// Añadido de D-051 del 2026-10-28 («la intro nace del logo»): de que empezó a moverse a que el círculo del icono cubrió la
        /// pantalla (ms reales; -1 si no se abrió: Reducir movimiento, capturas o cortada antes; <c>morph_ms</c>).
        /// </summary>
        public long MorphMs { get; internal set; } = -1;

        /// <summary>
        /// Diámetro del círculo del icono en el primer fotograma de la intro (dp de Android; <c>morph_start_diam_dp</c>): 160 con la
        /// caja de la guía; con la caja medida, la del sistema (~185 en el SM-S938B).
        /// </summary>
        public float MorphStartDiamDp { get; internal set; } = -1f;

        /// <summary>
        /// Jornada 6, observación 11: diámetro del círculo del primer fotograma de la intro en px de pantalla
        /// (<c>morph_start_diam_px</c>); se compara con <c>os_splash_icon_px</c> de <c>boot_steps</c>. -1 si no hubo primer fotograma.
        /// </summary>
        public float MorphStartDiamPx { get; internal set; } = -1f;

        /// <summary>Centro de ese círculo en px de pantalla, desde arriba a la izquierda (<c>morph_start_xy_px</c>).</summary>
        public Vector2 MorphStartCenterPx { get; internal set; } = new Vector2(-1f, -1f);

        /// <summary>
        /// De dónde salió la caja del primer fotograma (<c>morph_geometry</c>): <c>measured</c> (la que midió Java: la vista del
        /// icono del sistema o el círculo de <c>af_launch_window</c>) o <c>guide</c> (240/160 dp centrados: el Editor o sin medida).
        /// </summary>
        public string MorphGeometry { get; internal set; } = "guide";

        /// <summary>
        /// Jornada 6, observación 12: la forma del agujero del primer fotograma (<c>morph_shape</c>): <c>squircle</c> (la superelipse
        /// que calca la máscara del sistema, en One UI su cuadrado redondeado) o <c>circle</c> (sin máscara: el Editor, Android 8-11,
        /// el velo propio o una máscara ilegible; o una máscara redonda).
        /// </summary>
        public string MorphShape { get; internal set; } = "circle";

        /// <summary>Exponente de esa superelipse (<c>morph_shape_n</c>; 2 el círculo).</summary>
        public float MorphShapeN { get; internal set; } = 2f;

        /// <summary>
        /// Diámetros de ese agujero en px de pantalla en los ejes y en las diagonales (<c>morph_shape_px</c>): se comparan con la
        /// forma del último fotograma del sistema en la ráfaga. Con el círculo, los dos son <see cref="MorphStartDiamPx"/>.
        /// </summary>
        public Vector2 MorphShapePx { get; internal set; } = new Vector2(-1f, -1f);

        /// <summary>
        /// Lo que se separa la superelipse de la máscara medida a 22,5 grados, en px de radio (<c>morph_shape_fit_px</c>; positivo,
        /// la superelipse por fuera); 0 sin máscara.
        /// </summary>
        public float MorphShapeFitPx { get; internal set; }

        /// <summary>Fotogramas pintados mientras el icono se abría (<c>morph_frames</c>).</summary>
        public int MorphFrames { get; internal set; }

        /// <summary>Cómo salió del icono (<c>morph_mode</c>): <c>open</c> (se abrió), <c>fade</c> (Reducir movimiento) o <c>none</c>.</summary>
        public string MorphMode { get; internal set; } = "none";

        /// <summary>Unidades del lienzo por dp de Android con que se midió el círculo (1 si no se supo la densidad; <c>morph_units_per_dp</c>).</summary>
        public float MorphUnitsPerDp { get; internal set; } = 1f;

        /// <summary>Lo que costó crear las piezas del icono, con su vista decodificada (ms; <c>morph_build_ms</c>).</summary>
        public long MorphBuildMs { get; internal set; }
    }

    /// <summary>Cómo supo la intro que su primer fotograma ya estaba en la pantalla (<see cref="BrandIntro.VisibleBy"/>; sonda <c>visible_by</c>).</summary>
    public enum BrandIntroVisibleBy
    {
        /// <summary>Aún no se sabe (o la intro no llegó a pintarse).</summary>
        None,

        /// <summary>La GPU terminó una lectura pedida tras el primer fotograma: ese fotograma ya se pintó.</summary>
        Gpu,

        /// <summary>La GPU no respondió a tiempo y se dio por visto al llegar al tope (<see cref="BrandIntro.VisibleWaitCapMs"/>).</summary>
        Cap,

        /// <summary>
        /// Sin lecturas de la GPU (el Editor sin gráficos, los tests) o la lectura volvió con error: se da por visto en cuanto el
        /// primer fotograma está enviado (<see cref="BrandIntro.FenceTick"/>).
        /// </summary>
        NoReadback,
    }

    /// <summary>
    /// Intro de la marca (D-051, punto 3): «El río que se tiñe» de la identidad v2 (D-045), dibujado por código sobre el petróleo de
    /// la marca, el mismo fondo de la pantalla del sistema de Android 12+ (D-051, punto 2), para que empalmen. Con su añadido
    /// (2026-10-25, a petición del dueño): el Amazonas con meandros, del horizonte al primer plano, entre la selva
    /// (<see cref="BrandRiverScene"/>); la cuia de vidrio del icono (Taperebá, Aguaje y Açaí, inclinada según D-006) vierte el
    /// chorro en parábola sobre un punto del río; al llegar, salen los aros del impacto y el açaí tiñe el cauce río abajo, siguiendo
    /// los meandros, hasta el primer plano (sin texto: 06 §2.1, regla 3, en el arranque). Las piezas son las de la pantalla de
    /// arranque (<see cref="BrandEmblemArt"/>, colocadas igual: <see cref="BrandEmblemImages"/>): su último fotograma es esa
    /// pantalla sin el mundo de alrededor.
    /// <list type="bullet">
    /// <item>Va en la pantalla de arranque (<see cref="BootSplash"/>), por encima de todo lo suyo, en cada arranque en frío
    /// (añadido de D-051 del 2026-10-26, que sustituye a «nunca alarga la carga»): se ve <b>entera y a su ritmo</b>
    /// (<see cref="AnimMs"/>, unos 3,5 s también con «la intro nace del logo», añadido del 2026-10-28: el icono del sistema se abre en
    /// la escena (<see cref="MorphMs"/>), el chorro cae, salen los aros, el açaí tiñe el río hasta <see cref="MotionMs"/> y entra
    /// el nombre «Tintalú», <see cref="WordmarkAlpha"/>, que se sostiene), y el arranque la espera aunque la carga acabe antes
    /// (<see cref="Done"/>). Si la carga dura más, se queda en su último fotograma y la pantalla de arranque enseña «Cargando…»
    /// bajo el nombre hasta que acabe. Con Reducir movimiento sale quieta, entera, bajo el icono, que se funde en
    /// <see cref="ReduceMotionFadeMs"/>, y se sostiene <see cref="ReduceMotionHoldMs"/> (o lo que dure la carga). <see cref="End"/>
    /// la quita.</item>
    /// <item>No recibe toques (el fondo de la pantalla de arranque ya los traga: aún no hay nada que tocar) y no asigna memoria por
    /// fotograma. Las texturas de la escena son de la pantalla de arranque; las del icono, su círculo y su aro, de
    /// <see cref="BrandIntroMorph"/>.</item>
    /// </list>
    /// </summary>
    public sealed class BrandIntro
    {
        /// <summary>
        /// Duración entera de la intro (ms; <c>anim_ms</c>): anima hasta <see cref="MotionMs"/>, entra el nombre y sostiene el
        /// emblema con «Tintalú» hasta aquí. Añadido de D-051 del 2026-10-26 (el dueño: «es la marca, debe durar unos segundos»)
        /// y del 2026-10-28 (el dueño: «podemos dejarla un segundo más para apreciarla mejor»): unos 3,5 s [P], con el segundo de
        /// más repartido en el vertido, el tinte y la entrada del nombre (no como pausa al final); sigue en 3,5 s [P] con «la intro nace
        /// del logo» (añadido del 2026-10-28: 0,55 s de apertura antes del vertido, con el vertido, el tinte y el nombre algo más
        /// rápidos; <see cref="SettledMs"/> sigue en 2.900). Nunca se corta antes porque la
        /// carga acabe; cuenta desde que empieza a moverse (<see cref="Play"/>).
        /// </summary>
        public const int AnimMs = 3500;

        /// <summary>
        /// Añadido de D-051 del 2026-10-28 («la intro nace del logo»): el círculo del icono se abre hasta cubrir la pantalla y la
        /// cuia viaja del encuadre del icono al de la escena, con un ease-in-out cúbico (ms) [P].
        /// </summary>
        public const int MorphMs = 700;

        /// <summary>
        /// El corte sobre la cuia, primero: desde aquí el resto del icono (río, selva, cielo) se funde hacia el petróleo y la cuia
        /// de la escena queda encima de la del icono (ms) [P]. Hasta aquí, el icono tal cual.
        /// </summary>
        public const int IconFadeFromMs = 40;

        /// <summary>Lo que tarda el resto del icono en irse al petróleo (ms) [P].</summary>
        public const int IconFadeMs = 180;

        /// <summary>
        /// El corte sobre la cuia, después: desde aquí el fondo de la escena entra desde el petróleo alrededor de la cuia (ms) [P].
        /// Empieza cuando el icono ya se fue entero: los dos ríos nunca se ven a la vez.
        /// </summary>
        public const int CoverFadeFromMs = IconFadeFromMs + IconFadeMs;

        /// <summary>Lo que tarda el fondo de la escena en salir del petróleo (ms) [P].</summary>
        public const int CoverFadeMs = 260;

        /// <summary>El açaí del río se recoge hacia la cuia (los 10 tramos al revés) desde aquí (ms) [P].</summary>
        public const int AcaiRetractFromMs = 200;

        /// <summary>Lo que tarda el açaí en recogerse (ms) [P].</summary>
        public const int AcaiRetractMs = 350;

        /// <summary>Los aros del icono se deshacen desde aquí (ms) [P].</summary>
        public const int ImpactFadeFromMs = 150;

        /// <summary>Lo que tardan en deshacerse (ms) [P].</summary>
        public const int ImpactFadeMs = 250;

        /// <summary>El chorro del icono vuelve a la boca de la cuia desde aquí hasta <see cref="PourFromMs"/> (ms) [P].</summary>
        public const int StreamRetractFromMs = 350;

        /// <summary>Empieza el vertido de siempre: el chorro sale otra vez de la boca de la cuia (ms) [P].</summary>
        public const int PourFromMs = 550;

        /// <summary>Con Reducir movimiento, el icono se funde en la escena quieta en esto (ms) [P].</summary>
        public const int ReduceMotionFadeMs = 200;

        /// <summary>Fin del movimiento: el açaí llega al primer plano (ms) [P].</summary>
        public const int MotionMs = 2600;

        /// <summary>Fin del chorro: llega de la boca de la cuia al río (ms; 700 ms de caída) [P].</summary>
        public const int FallMs = 1250;

        /// <summary>Fin de los aros del impacto (ms; 850 ms) [P].</summary>
        public const int RippleEndMs = 2100;

        /// <summary>El nombre «Tintalú» entra bajo la cuia desde aquí (ms) [P].</summary>
        public const int WordmarkFromMs = 2450;

        /// <summary>Lo que tarda el nombre en entrar (ms) [P].</summary>
        public const int WordmarkFadeMs = 450;

        /// <summary>
        /// Desde aquí no se mueve nada (ms): el nombre ya entró entero y solo se sostiene el emblema hasta <see cref="AnimMs"/>. Hasta
        /// aquí GameBootstrap no carga nada entre fotogramas (jornada 4, 2026-10-28: el vertido, fluido); en lo que queda, sí.
        /// </summary>
        public const int SettledMs = WordmarkFromMs + WordmarkFadeMs;

        /// <summary>
        /// Tope de la espera a saber que el primer fotograma ya está en la pantalla (ms desde ese fotograma) [P]: si la GPU no responde
        /// antes, la intro empieza a moverse igual (<see cref="BrandIntroVisibleBy.Cap"/>).
        /// </summary>
        public const int VisibleWaitCapMs = 1200;

        /// <summary>
        /// Jornada 6, observación 11: cada cuántos fotogramas se mira, mientras se espera a ver el fotograma 0, si cambió la caja del
        /// icono que mide Java (una llamada JNI; la caja solo se copia si cambió) [P].
        /// </summary>
        public const int IconPollFrames = 3;

        /// <summary>Cuántas veces como mucho se rehace el fotograma 0 por una caja nueva del icono [P].</summary>
        public const int MaxIconRelayouts = 2;

        /// <summary>
        /// El <see cref="Tick"/> en que se pide la lectura a la GPU (y desde el que se mira si ya volvió): el segundo. El primero va
        /// en el mismo fotograma en que se crea la intro, que aún no se ha enviado a la GPU; una lectura pedida entonces se encolaría
        /// antes de los dibujos de la intro y solo probaría que acabó el fotograma anterior.
        /// </summary>
        public const int FenceTick = 2;

        /// <summary>
        /// Un hueco entre fotogramas mayor que esto (ms) mientras se mueve es una pausa (segundo plano, bloqueo), no un fotograma
        /// lento: se cuenta aparte (<see cref="BrandIntroReport.MotionPauses"/>) [P].
        /// </summary>
        public const int MotionPauseMs = 500;

        /// <summary>Con Reducir movimiento, la escena entera sale quieta y se sostiene esto (ms; o lo que dure la carga) [P].</summary>
        public const int ReduceMotionHoldMs = 1000;

        /// <summary>
        /// Petróleo de la marca (D-045; D-051, punto 2): #14444A, el tono medio del cielo del icono v2. Lo repiten
        /// <c>ProjectSetup.BrandPetroleum</c> y <c>af_brand_petroleum</c> de <c>AfIcon.androidlib</c>; un test EditMode los compara.
        /// </summary>
        public static readonly Color32 Petroleum = Palette.Hex(0x14444A);

        /// <summary>Halo del icono v2 (#5FB3A6 al 45 %); la pantalla de arranque usa el mismo.</summary>
        internal static readonly Color HaloColor = new Color(0x5F / 255f, 0xB3 / 255f, 0xA6 / 255f, 0.45f);

        private readonly RectTransform _root;
        private readonly Image _background;
        private readonly BrandEmblemImages _images;
        private readonly RectTransform _scene;
        private readonly BrandIntroMorph _morph;
        private readonly double _createdAt;
        private readonly float[] _motionDeltas = new float[MotionDeltaCapacity];
        private double _firstFrameAt = -1.0;
        private double _visibleAt = -1.0;
        private double _playAt = -1.0;
        private double _lastTickAt = -1.0;
        private BrandIntroVisibleBy _visibleBy;
        private AsyncGPUReadbackRequest _fence;
        private bool _fenceArmed;
        private bool _playing;
        private int _motionCount;
        private int _motionFrames;
        private int _motionPauses;
        private int _fenceAtTick = -1;
        private float _motionMax;
        private float _tintFill;
        private bool _begun;
        private bool _reduceMotion;
        private bool _ended;
        private bool _seeked;
        private float _clockMs;
        private float _wordmarkAlpha;
        private int _frames;
        private double _fadeFrom = -1.0;
        private double _morphDoneAt = -1.0;
        private int _morphFrames;
        private float _morphStartDiamDp = -1f;
        private float _morphStartDiamPx = -1f;
        private Vector2 _morphStartCenterPx = new Vector2(-1f, -1f);
        private BrandEmblemArt _art;
        private int _fenceNotBefore;
        private int _iconRelayouts;

        // Tiempos entre fotogramas que se guardan mientras se mueve (unos 3 s a 120 Hz caben).
        private const int MotionDeltaCapacity = 512;

        /// <summary>
        /// Solo para los tests: sustituye a la lectura de la GPU que dice si el primer fotograma ya está en la pantalla (null = la de
        /// siempre). El tope <see cref="VisibleWaitCapMs"/> sigue valiendo. Los tests que lo cambian lo vuelven a null en su TearDown.
        /// </summary>
        public static Func<bool>? FrameVisibleOverrideForTests { get; set; }

        /// <summary>
        /// Solo para los tests: el reloj de la intro no avanza aunque pasen fotogramas (fotogramas parados, para el tope de
        /// <c>GameBootstrap.IntroGap</c>). Los tests que lo cambian lo vuelven a false en su TearDown.
        /// </summary>
        public static bool ClockFrozenForTests { get; set; }

        /// <summary>El <see cref="Tick"/> en que se pidió la lectura a la GPU (o se empezó a mirar si se ve); -1 si aún no (tests).</summary>
        public int FenceRequestedAtTick => _fenceAtTick;

        /// <summary>
        /// Interruptor de los tests (null = lo de siempre: sale con la pantalla de arranque). Los tests que lo cambian lo vuelven a
        /// null en su TearDown.
        /// </summary>
        public static bool? EnabledOverride { get; set; }

        /// <summary>La intro sale en la pantalla de arranque (<see cref="EnabledOverride"/> o, sin él, siempre).</summary>
        public static bool Enabled => EnabledOverride ?? true;

        /// <summary>
        /// Crea la intro, ya a la vista en su primer fotograma, dentro de <paramref name="parent"/> (la pantalla de arranque).
        /// <paramref name="createdAt"/> (<see cref="Time.realtimeSinceStartupAsDouble"/> de cuando se creó la pantalla: el mismo
        /// origen que <c>load_ms</c>) es el de <c>intro_ms</c>; el reloj de la animación cuenta desde su primer fotograma a la vista.
        /// El halo y las piezas del emblema (<paramref name="art"/>) son de la pantalla de arranque.
        /// </summary>
        internal BrandIntro(Transform parent, double createdAt, Sprite halo, BrandEmblemArt art)
        {
            _createdAt = createdAt;
            var go = new GameObject("BrandIntro", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _root = (RectTransform)go.transform;
            UiBuilder.Stretch(_root);

            _background = UiBuilder.Image("Petroleum", _root, null, Petroleum);
            UiBuilder.Stretch(_background.rectTransform);
            var sceneGo = new GameObject("Scene", typeof(RectTransform));
            sceneGo.transform.SetParent(_root, false);
            _scene = (RectTransform)sceneGo.transform;
            _images = BrandEmblemImages.Create(_scene, halo);
            _morph = new BrandIntroMorph(_root, _scene, _images.Impact, _images.Stream, _images.Cuia);
            _background.sprite = _morph.PetroleumSprite; // el petróleo exacto, de la textura (jornada 6, observación 11)
            _background.color = Color.white;
            _art = art;
            Image stream = _images.Stream;
            stream.type = Image.Type.Filled;
            stream.fillMethod = Image.FillMethod.Vertical;
            stream.fillOrigin = (int)Image.OriginVertical.Top;
            Layout(art);
        }

        /// <summary>Todo lo de la intro (tests y capturas).</summary>
        public RectTransform Root => _root;

        /// <summary>La intro está a la vista.</summary>
        public bool Visible => !_ended && _root != null && _root.gameObject.activeSelf;

        /// <summary>
        /// Empieza a moverse sola en cuanto puede (<see cref="ReadyToPlay"/>; lo de siempre). GameBootstrap la apaga para elegir él el
        /// momento, en un hueco de la carga, y no cargar nada mientras se mueve (jornada 4, 2026-10-28).
        /// </summary>
        public bool AutoPlay { get; set; } = true;

        /// <summary>Su primer fotograma ya está en la pantalla (o se dio por visto: <see cref="VisibleBy"/>).</summary>
        public bool OnScreen => _visibleAt >= 0.0;

        /// <summary>Cómo se supo que su primer fotograma ya estaba en la pantalla.</summary>
        public BrandIntroVisibleBy VisibleBy => _visibleBy;

        /// <summary>Ya empezó a moverse (su reloj corre desde <see cref="Play"/>).</summary>
        public bool Playing => _playing;

        /// <summary>
        /// Puede empezar a moverse: se sabe que no hay Reducir movimiento, su primer fotograma ya está en la pantalla y aún no empezó
        /// (false con Reducir movimiento, en las capturas con <see cref="Seek"/> y tras quitarse).
        /// </summary>
        public bool ReadyToPlay => !_ended && _begun && !_reduceMotion && !_seeked && !_playing && _visibleAt >= 0.0;

        /// <summary>Se mueve: empezó y aún no llegó a <see cref="SettledMs"/> (el nombre entero). Mientras, GameBootstrap no carga nada.</summary>
        public bool Moving => Animating && _clockMs < SettledMs;

        /// <summary>Se sabe si hay Reducir movimiento y la animación corre (o, con él, está en su último fotograma).</summary>
        public bool Begun => _begun;

        /// <summary>Sale quieta, en su último fotograma (Reducir movimiento).</summary>
        public bool ReduceMotion => _reduceMotion;

        /// <summary>Reloj de la animación (ms, entre 0 y <see cref="AnimMs"/>).</summary>
        public float ClockMs => _clockMs;

        /// <summary>La animación llegó a su último fotograma, el estable.</summary>
        public bool Finished => _clockMs >= AnimMs;

        /// <summary>Fotogramas pintados con la intro a la vista.</summary>
        public int Frames => _frames;

        /// <summary>Desde que se creó (ms reales).</summary>
        public long AgeMs => (long)((Time.realtimeSinceStartupAsDouble - _createdAt) * 1000.0);

        /// <summary>De la creación a su primer fotograma a la vista (ms reales; -1 si aún no llegó; <c>first_frame_ms</c>).</summary>
        public long FirstFrameMs => _firstFrameAt < 0.0 ? -1L : (long)((_firstFrameAt - _createdAt) * 1000.0);

        /// <summary>De la creación a cuando se supo que su primer fotograma ya estaba en la pantalla (ms reales; -1 si no; <c>visible_ms</c>).</summary>
        public long VisibleMs => _visibleAt < 0.0 ? -1L : (long)((_visibleAt - _createdAt) * 1000.0);

        /// <summary>De la creación a cuando empezó a moverse (ms reales; -1 si no empezó; <c>play_ms</c>).</summary>
        public long PlayMs => _playAt < 0.0 ? -1L : (long)((_playAt - _createdAt) * 1000.0);

        /// <summary>
        /// La animación está corriendo y aún no llegó a su último fotograma: cada fotograma pintado enseña algo nuevo (false antes de
        /// <see cref="Begin"/>, con Reducir movimiento, al acabar, al quitarse y en las capturas con <see cref="Seek"/>).
        /// </summary>
        public bool Animating => !_ended && _begun && !_reduceMotion && !_seeked && _playing && _clockMs < AnimMs;

        /// <summary>
        /// La intro ya se vio entera (añadido de D-051 del 2026-10-26): su reloj llegó a <see cref="AnimMs"/> o, con Reducir
        /// movimiento, lleva <see cref="ReduceMotionHoldMs"/> quieta desde su primer fotograma. Hasta entonces el arranque la espera
        /// aunque la carga haya acabado; false antes de <see cref="Begin"/>.
        /// </summary>
        public bool Done
        {
            get
            {
                if (!_begun)
                {
                    return false;
                }

                if (_reduceMotion)
                {
                    PollVisible(Time.realtimeSinceStartupAsDouble);
                    return _visibleAt >= 0.0 && (Time.realtimeSinceStartupAsDouble - _visibleAt) * 1000.0 >= ReduceMotionHoldMs;
                }

                return _clockMs >= AnimMs;
            }
        }

        /// <summary>Lo que se ve del nombre «Tintalú» (0 a 1): entra al final del movimiento; entero con Reducir movimiento.</summary>
        public float WordmarkAlpha => _wordmarkAlpha;

        /// <summary>Lo teñido del río: por dónde va el açaí río abajo desde el impacto (0 sin teñir, 1 hasta el primer plano).</summary>
        public float TintFill => _tintFill;

        /// <summary>Lo que lleva el chorro de la boca de la cuia al río (0 aún dentro, 1 en el agua).</summary>
        public float StreamFill => _images.Stream.fillAmount;

        /// <summary>Lo que se ve de los aros del impacto (0 a 1).</summary>
        public float ImpactAlpha => _images.Impact.color.a;

        /// <summary>El fondo de la escena (el halo, la selva, el río, el açaí y la isla; tests).</summary>
        public RectTransform Scene => _scene;

        /// <summary>Una pieza de la escena por su nombre, en el fondo o en el grupo de la cuia (tests); null si no está.</summary>
        public Transform? Piece(string name)
        {
            Transform? t = _scene.Find(name);
            return t != null ? t : _morph.Top.Find(name);
        }

        /// <summary>Lo de «la intro nace del logo»: el icono, el círculo y el aro (tests y capturas).</summary>
        internal BrandIntroMorph Morph => _morph;

        /// <summary>Diámetro del círculo del icono ahora (unidades del lienzo; ya cubre la pantalla desde <see cref="MorphMs"/>).</summary>
        public float MorphDiameter => _morph.Diameter;

        /// <summary>Diámetro del círculo ahora en dp de Android.</summary>
        public float MorphDiameterDp => _morph.Diameter / _morph.UnitsPerDp;

        /// <summary>Centro del círculo del icono (unidades del lienzo, y desde arriba).</summary>
        public Vector2 MorphCenter => _morph.Center;

        /// <summary>Unidades del lienzo por dp de Android (1 si no se sabe la densidad).</summary>
        public float MorphUnitsPerDp => _morph.UnitsPerDp;

        /// <summary>El círculo del icono, con su petróleo alrededor, sigue a la vista.</summary>
        public bool MorphIrisVisible => _morph.IrisVisible;

        /// <summary>Lo que se ve de la vista del icono (0 a 1).</summary>
        public float MorphIconAlpha => _morph.IconAlpha;

        /// <summary>Escala del grupo de la cuia ahora (1 = su encuadre; menor mientras viene del icono).</summary>
        public float MorphSceneScale => _morph.CuiaScale;

        /// <summary>Escala del fondo de la escena ahora (≥ 1: nunca enseña sus bordes).</summary>
        public float MorphBackScale => _morph.BackScale;

        /// <summary>Lo que tapa el petróleo al fondo de la escena (0 a 1).</summary>
        public float MorphCoverAlpha => _morph.CoverAlpha;

        /// <summary>
        /// Con lo que se sabe ya del perfil: con <paramref name="reduceMotion"/>, salta a su último fotograma y se queda quieta; si
        /// no, sigue quieta en el primero (la cuia sobre el río) hasta <see cref="Play"/> o, con <see cref="AutoPlay"/>, hasta que
        /// pueda empezar.
        /// </summary>
        public void Begin(bool reduceMotion)
        {
            if (_ended || _begun)
            {
                return;
            }

            _begun = true;
            _reduceMotion = reduceMotion;
            Apply(reduceMotion ? AnimMs : _clockMs);
            if (reduceMotion)
            {
                UpdateFade(Time.realtimeSinceStartupAsDouble); // el icono sigue encima hasta verse en la pantalla
            }
        }

        /// <summary>
        /// Empieza a moverse ahora, si puede (<see cref="ReadyToPlay"/>): su reloj cuenta desde aquí, así que nada de lo que pasó
        /// antes (la escena, la carga, la espera a la GPU) se come el vertido. True si empezó.
        /// </summary>
        public bool Play()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            PollVisible(now);
            if (!ReadyToPlay)
            {
                return false;
            }

            _playing = true;
            _playAt = now;
            _lastTickAt = now;
            Apply(0f);
            _morphFrames = 1; // este fotograma enseña el icono entero: el instante 0 de la apertura
            return true;
        }

        /// <summary>
        /// Lleva la animación al instante de ahora sin contar un fotograma: GameBootstrap la llama justo antes de dejar pintar uno
        /// entre dos bloques de la carga (el <see cref="Tick"/> de ese fotograma fue antes del bloque). Sin asignaciones.
        /// </summary>
        public void Refresh()
        {
            PollVisible(Time.realtimeSinceStartupAsDouble);
            if (AutoPlay && ReadyToPlay)
            {
                Play();
            }

            if (Animating && !ClockFrozenForTests)
            {
                Apply(CurrentMs());
                NoteMorphDone(Time.realtimeSinceStartupAsDouble);
            }
            else if (_reduceMotion && _begun)
            {
                UpdateFade(Time.realtimeSinceStartupAsDouble);
            }
        }

        /// <summary>
        /// Un fotograma: mira si su primer fotograma ya está en la pantalla, empieza a moverse si toca (<see cref="AutoPlay"/>) y
        /// avanza el reloj con el tiempo real (sin asignaciones). Quieta antes de empezar, con Reducir movimiento y tras el final.
        /// </summary>
        public void Tick()
        {
            if (_ended)
            {
                return;
            }

            _frames++;
            double now = Time.realtimeSinceStartupAsDouble;
            if (_firstFrameAt < 0.0)
            {
                _firstFrameAt = now; // su primer fotograma: desde aquí se espera a verlo en la pantalla
                NoteMorphStart();
            }
            else if (_visibleAt < 0.0 && !_playing && _iconRelayouts < MaxIconRelayouts && _frames % IconPollFrames == 0
                && AndroidOsSplash.ReadIcon().DiffersFrom(_morph.Box, 1))
            {
                // La caja del icono cambió después del primer fotograma: en Android 12+ el velo propio publica primero la suya (160 dp)
                // y la del sistema llega al colocarse su vista. Se rehace el fotograma 0 con la nueva y se vuelve a esperar a verlo en
                // la pantalla, para que el velo no se suelte sobre el anterior.
                _iconRelayouts++;
                Layout(_art);
                NoteMorphStart();
                _firstFrameAt = now;
                _fenceAtTick = -1;
                _fenceArmed = false;
                _fenceNotBefore = _frames + 1;
            }

            if (_fenceAtTick < 0 && _frames >= FenceTick && _frames >= _fenceNotBefore)
            {
                ArmFence(now); // el primer fotograma ya está enviado a la GPU: la lectura va detrás de sus dibujos
            }

            PollVisible(now);
            if (AutoPlay && ReadyToPlay)
            {
                Play(); // este fotograma enseña su instante 0
                return;
            }

            if (!Animating)
            {
                if (_reduceMotion && _begun)
                {
                    UpdateFade(now);
                }

                _lastTickAt = now;
                return;
            }

            if (_clockMs < SettledMs && _lastTickAt >= _playAt)
            {
                NoteMotionFrame((float)((now - _lastTickAt) * 1000.0));
            }

            _lastTickAt = now;
            if (!ClockFrozenForTests)
            {
                Apply(CurrentMs());
            }

            if (_morphDoneAt < 0.0)
            {
                _morphFrames++;
                NoteMorphDone(now);
            }
        }

        /// <summary>Lleva la animación al instante <paramref name="ms"/> y la deja ahí (capturas y tests deterministas).</summary>
        public void Seek(float ms)
        {
            _seeked = true;
            _playing = true;
            _begun = true;
            Apply(ms);
            if (_reduceMotion)
            {
                UpdateFade(Time.realtimeSinceStartupAsDouble); // quieta y sin el icono encima
            }
        }

        /// <summary>Rehace la composición con la escena <paramref name="art"/> (otra pantalla: la pantalla de arranque la rehízo).</summary>
        internal void Layout(BrandEmblemArt art)
        {
            _art = art;
            _images.Place(art);
            _morph.Layout(art, UnitsPerDp(art), AndroidOsSplash.ReadIcon(), ScreenPxPerUnit(art));
            Apply(_clockMs);
            if (_reduceMotion && _begun)
            {
                UpdateFade(Time.realtimeSinceStartupAsDouble);
            }
        }

        /// <summary>
        /// Termina la intro al acabar la carga: la quita de la vista y devuelve lo que se vio. Una sola vez (null después).
        /// </summary>
        public BrandIntroReport? End()
        {
            if (_ended)
            {
                return null;
            }

            var report = new BrandIntroReport
            {
                IntroMs = AgeMs,
                ShownMs = (long)_clockMs,
                Cut = _begun && !_reduceMotion && _clockMs < AnimMs,
                Frames = _frames,
                FirstFrameMs = FirstFrameMs,
                ReduceMotion = _reduceMotion,
                Begun = _begun,
                VisibleMs = VisibleMs,
                VisibleBy = _visibleBy,
                PlayMs = _seeked ? -1L : PlayMs,
                MotionFrames = _motionFrames,
                FrameP95Ms = DevProbe.Enabled ? MotionP95() : -1f,
                FrameMaxMs = _motionCount > 0 ? _motionMax : -1f,
                MotionPauses = _motionPauses,
                MorphMs = _morphDoneAt < 0.0 || _seeked ? -1L : (long)((_morphDoneAt - _playAt) * 1000.0),
                MorphStartDiamDp = _morphStartDiamDp,
                MorphStartDiamPx = _morphStartDiamPx,
                MorphStartCenterPx = _morphStartCenterPx,
                MorphGeometry = _morph.Measured ? "measured" : "guide",
                MorphShape = _morph.Shape.Name,
                MorphShapeN = _morph.Shape.Exponent,
                MorphShapePx = new Vector2(_morphStartDiamPx * _morph.Shape.Axis, _morphStartDiamPx * _morph.Shape.Diag),
                MorphShapeFitPx = _morph.Shape.FromMask && _morph.Shape.Mid > 0f
                    ? (IconMaskShape.Radius(_morph.Shape.Axis, _morph.Shape.Exponent, Mathf.PI / 8f) - _morph.Shape.Mid) * _morphStartDiamPx * 0.5f
                    : 0f,
                MorphFrames = _seeked ? 0 : _morphFrames,
                MorphMode = _reduceMotion ? "fade" : (_playing && !_seeked ? "open" : "none"),
                MorphUnitsPerDp = _morph.UnitsPerDp,
                MorphBuildMs = _morph.BuildMs,
            };
            _ended = true;
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }

            return report;
        }

        /// <summary>Libera lo del icono (su vista, el círculo y el aro); las texturas del emblema son de la pantalla de arranque.</summary>
        internal void Dispose()
        {
            _morph.Dispose();
        }

        /// <summary>
        /// Estado de la animación en <paramref name="ms"/> (sin asignaciones). Añadido de D-051 del 2026-10-28 («la intro nace del
        /// logo»): en 0 es el icono de la pantalla del sistema (la vista del icono en su círculo sobre el petróleo); hasta
        /// <see cref="MorphMs"/> el círculo se abre a toda la pantalla mientras la escena pasa del encuadre del icono al suyo y la
        /// vista del icono se funde en ella; a la vez el açaí del río se recoge hacia la cuia, los aros se deshacen y el chorro vuelve
        /// a la boca (hasta <see cref="PourFromMs"/>). Desde ahí, lo de siempre: el chorro baja hasta el agua (<see cref="FallMs"/>),
        /// salen los aros (hasta <see cref="RippleEndMs"/>) y el açaí avanza por el cauce, tramo a tramo y frenando, hasta el primer
        /// plano (<see cref="MotionMs"/>). Después, quieta: la escena entera.
        /// </summary>
        private void Apply(float ms)
        {
            float t = Mathf.Clamp(ms, 0f, AnimMs);
            float w = Mathf.Clamp01((t - WordmarkFromMs) / WordmarkFadeMs);
            _wordmarkAlpha = w * w * (3f - (2f * w));
            _clockMs = t;

            float open = EaseInOutCubic(t / MorphMs);
            _morph.Apply(open, 1f - Smooth((t - IconFadeFromMs) / IconFadeMs), 1f - Smooth((t - CoverFadeFromMs) / CoverFadeMs));

            // Hasta el corte, el icono tal cual (su propia cuia); desde ahí, la cuia, el chorro y los aros de la escena encima, en el
            // mismo sitio y a la misma escala.
            float shared = t < IconFadeFromMs ? 0f : 1f;
            _images.Cuia.color = new Color(1f, 1f, 1f, shared);
            Image[] acai = _images.Acai;
            if (t < PourFromMs)
            {
                // El icono coge aire: el açaí se recoge hacia la cuia, los aros se deshacen y el chorro vuelve a la boca.
                float back = Smooth((t - StreamRetractFromMs) / (PourFromMs - StreamRetractFromMs));
                _images.Stream.fillAmount = 1f - back;
                _images.Stream.color = new Color(1f, 1f, 1f, shared);
                float fade = Smooth((t - ImpactFadeFromMs) / ImpactFadeMs);
                float grow = 1f + (0.15f * fade);
                _images.Impact.rectTransform.localScale = new Vector3(grow, grow, 1f);
                _images.Impact.color = new Color(1f, 1f, 1f, (1f - fade) * shared);
                float retreat = acai.Length * (1f - Smooth((t - AcaiRetractFromMs) / AcaiRetractMs));
                _tintFill = retreat / acai.Length;
                for (int k = 0; k < acai.Length; k++)
                {
                    acai[k].color = new Color(1f, 1f, 1f, FrontAlpha(retreat, k));
                }

                return;
            }

            _images.Stream.color = Color.white;
            float pour = Mathf.Clamp01((t - PourFromMs) / (FallMs - PourFromMs));
            _images.Stream.fillAmount = 1f - ((1f - pour) * (1f - pour));

            float ripple = t <= FallMs ? -1f : Mathf.Clamp01((t - FallMs) / (float)(RippleEndMs - FallMs));
            float rs = ripple < 0f ? 0.3f : 0.3f + (0.7f * (1f - ((1f - ripple) * (1f - ripple))));
            _images.Impact.rectTransform.localScale = new Vector3(rs, rs, 1f);
            _images.Impact.color = new Color(1f, 1f, 1f, ripple < 0f ? 0f : Mathf.Clamp01(ripple * 2.5f));

            // El frente del açaí recorre los tramos río abajo: cada tramo, con su frente en lengua, entra mientras el frente lo cruza
            // (jornada 4, 2026-10-28: con el avance, sin saltos de un tramo entero de golpe) y queda opaco detrás; en cada instante
            // solo va a medias el tramo del frente.
            float spread = t <= FallMs ? 0f : Mathf.Clamp01((t - FallMs) / (MotionMs - FallMs));
            float eased = 1f - Mathf.Pow(1f - spread, 1.6f);
            _tintFill = eased;
            float front = eased * acai.Length;
            for (int k = 0; k < acai.Length; k++)
            {
                acai[k].color = new Color(1f, 1f, 1f, AcaiAlpha(front, k, spread, eased));
            }
        }

        /// <summary>Ease-in-out cúbico de <paramref name="x"/> (recortado a 0-1): arranca y llega con suavidad, sin rebote.</summary>
        internal static float EaseInOutCubic(float x)
        {
            x = Mathf.Clamp01(x);
            if (x < 0.5f)
            {
                return 4f * x * x * x;
            }

            float u = (-2f * x) + 2f;
            return 1f - (u * u * u * 0.5f);
        }

        // Smoothstep de x recortado a 0-1.
        private static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - (2f * x));
        }

        // Lo que se ve del tramo k con el frente del açaí en front (tramos): 0 delante, 1 detrás y suave mientras lo cruza.
        private static float FrontAlpha(float front, int k)
        {
            float a = Mathf.Clamp01(front - k);
            return a * a * (3f - (2f * a));
        }

        // Reducir movimiento: el icono del fotograma 0 se funde en la escena quieta en ReduceMotionFadeMs desde que se sabe que está
        // en la pantalla (antes, el icono entero: empalma con la pantalla del sistema). El nombre entra con el mismo fundido.
        private void UpdateFade(double now)
        {
            PollVisible(now);
            if (_visibleAt >= 0.0 && _fadeFrom < 0.0)
            {
                _fadeFrom = now;
            }

            float fade = _seeked ? 1f : _fadeFrom < 0.0 ? 0f : Smooth((float)((now - _fadeFrom) * 1000.0 / ReduceMotionFadeMs));
            if (fade > 0.999f)
            {
                fade = 1f; // entero: el nombre de la pantalla de arranque solo se repinta con saltos de más de 0,0001
            }

            _morph.ApplyFade(fade);
            _wordmarkAlpha = fade;
        }

        // El círculo ya cubre la pantalla: anota cuándo (morph_ms).
        private void NoteMorphDone(double now)
        {
            if (_morphDoneAt < 0.0 && _clockMs >= MorphMs)
            {
                _morphDoneAt = now;
            }
        }

        // El círculo con que empieza (sondas morph_start_*).
        private void NoteMorphStart()
        {
            _morphStartDiamDp = MorphDiameterDp;
            _morphStartDiamPx = _morph.StartDiameterPx;
            _morphStartCenterPx = _morph.StartCenterPx;
        }

        // Px de pantalla por unidad del lienzo (1 si no se sabe).
        private static float ScreenPxPerUnit(BrandEmblemArt art)
        {
            if (art.Width <= 0f)
            {
                return 1f;
            }

            GameScreen.ReadScreen(out int sw, out _, out _);
            return sw / art.Width;
        }

        // Unidades del lienzo por dp de Android: la densidad del teléfono entre la escala del lienzo (1 si no se sabe: el Editor).
        private static float UnitsPerDp(BrandEmblemArt art)
        {
            float density = GameScreen.ReadPxPerDp();
            if (density <= 0f || art.Width <= 0f)
            {
                return 1f;
            }

            GameScreen.ReadScreen(out int sw, out _, out _);
            float canvasScale = sw / art.Width;
            return canvasScale > 0f ? density / canvasScale : 1f;
        }

        /// <summary>
        /// Lo que se ve del tramo <paramref name="k"/> del açaí con el frente en <paramref name="front"/> (tramos): 0 delante del
        /// frente, 1 detrás y, mientras el frente lo cruza, de 0 a 1 con suavidad (jornada 4, 2026-10-28).
        /// </summary>
        internal static float AcaiAlpha(float front, int k, float spread, float eased)
        {
            if (spread <= 0f)
            {
                return 0f;
            }

            if (eased >= 1f)
            {
                return 1f;
            }

            float a = Mathf.Clamp01(front - k);
            return a * a * (3f - (2f * a));
        }

        // Desde que empezó a moverse hasta ahora (ms; 0 si aún no empezó).
        private float CurrentMs()
        {
            return _playAt < 0.0 ? 0f : (float)((Time.realtimeSinceStartupAsDouble - _playAt) * 1000.0);
        }

        // Pide a la GPU una lectura mínima tras el primer fotograma: cuando la termina, ese fotograma ya se pintó (jornada 4: en la
        // primera apertura la pantalla seguía lisa ~1 s aunque el juego ya hubiera mandado la intro). Sin lecturas, se da por visto.
        private void ArmFence(double now)
        {
            _fenceAtTick = _frames;
            if (FrameVisibleOverrideForTests != null)
            {
                return;
            }

            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                MarkVisible(now, BrandIntroVisibleBy.NoReadback);
                return;
            }

            try
            {
                _fence = AsyncGPUReadback.Request(Texture2D.whiteTexture);
                _fenceArmed = true;
            }
            catch (Exception)
            {
                MarkVisible(now, BrandIntroVisibleBy.NoReadback);
            }
        }

        // ¿Ya está el primer fotograma en la pantalla? Con el tope VisibleWaitCapMs desde ese fotograma (sin asignaciones).
        private void PollVisible(double now)
        {
            if (_visibleAt >= 0.0 || _fenceAtTick < 0)
            {
                return; // ya se sabe, o el primer fotograma aún no está enviado
            }

            Func<bool>? overrideVisible = FrameVisibleOverrideForTests;
            if (overrideVisible != null)
            {
                if (overrideVisible())
                {
                    MarkVisible(now, BrandIntroVisibleBy.Gpu);
                    return;
                }
            }
            else if (_fenceArmed)
            {
                if (!_fence.done)
                {
                    _fence.Update();
                }

                BrandIntroVisibleBy by = FenceResult(_fence.done, _fence.done && _fence.hasError);
                if (by != BrandIntroVisibleBy.None)
                {
                    _fenceArmed = false;
                    MarkVisible(now, by);
                    return;
                }
            }

            if ((now - _firstFrameAt) * 1000.0 >= VisibleWaitCapMs)
            {
                _fenceArmed = false;
                MarkVisible(now, BrandIntroVisibleBy.Cap);
            }
        }

        /// <summary>
        /// Lo que dice una lectura de la GPU: aún nada (<see cref="BrandIntroVisibleBy.None"/>), visto (<see cref="BrandIntroVisibleBy.Gpu"/>)
        /// o, si volvió con error, que no se pudo saber (<see cref="BrandIntroVisibleBy.NoReadback"/>: un error no prueba que se pintara).
        /// </summary>
        internal static BrandIntroVisibleBy FenceResult(bool done, bool hasError)
        {
            if (!done)
            {
                return BrandIntroVisibleBy.None;
            }

            return hasError ? BrandIntroVisibleBy.NoReadback : BrandIntroVisibleBy.Gpu;
        }

        private void MarkVisible(double now, BrandIntroVisibleBy by)
        {
            _visibleAt = now;
            _visibleBy = by;
        }

        private void NoteMotionFrame(float deltaMs)
        {
            _motionFrames++;
            if (deltaMs > MotionPauseMs)
            {
                _motionPauses++; // una pausa (segundo plano), no un fotograma lento
                return;
            }

            if (deltaMs > _motionMax)
            {
                _motionMax = deltaMs;
            }

            if (_motionCount < _motionDeltas.Length)
            {
                _motionDeltas[_motionCount++] = deltaMs;
            }
        }

        // Percentil 95 de los tiempos entre fotogramas mientras se movía (-1 sin datos); solo al quitarse (asigna una copia).
        private float MotionP95()
        {
            if (_motionCount == 0)
            {
                return -1f;
            }

            var sorted = new float[_motionCount];
            Array.Copy(_motionDeltas, sorted, _motionCount);
            Array.Sort(sorted);
            int index = Mathf.Clamp(Mathf.CeilToInt(0.95f * _motionCount) - 1, 0, _motionCount - 1);
            return sorted[index];
        }
    }
}
