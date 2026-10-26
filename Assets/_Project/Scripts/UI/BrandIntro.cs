#nullable enable
using AF.Presentation;
using UnityEngine;
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
    /// (<see cref="AnimMs"/>, unos 2,5 s: el chorro cae, salen los aros, el açaí tiñe el río hasta <see cref="MotionMs"/> y entra
    /// el nombre «Tintalú», <see cref="WordmarkAlpha"/>, que se sostiene), y el arranque la espera aunque la carga acabe antes
    /// (<see cref="Done"/>). Si la carga dura más, se queda en su último fotograma y la pantalla de arranque enseña «Cargando…»
    /// bajo el nombre hasta que acabe. Con Reducir movimiento sale quieta, entera, y se sostiene <see cref="ReduceMotionHoldMs"/>
    /// (o lo que dure la carga). <see cref="End"/> la quita.</item>
    /// <item>No recibe toques (el fondo de la pantalla de arranque ya los traga: aún no hay nada que tocar) y no asigna memoria por
    /// fotograma. No tiene texturas propias: las de la escena son de la pantalla de arranque.</item>
    /// </list>
    /// </summary>
    public sealed class BrandIntro
    {
        /// <summary>
        /// Duración entera de la intro (ms; <c>anim_ms</c>): anima hasta <see cref="MotionMs"/>, entra el nombre y sostiene el
        /// emblema con «Tintalú» hasta aquí. Añadido de D-051 del 2026-10-26 (el dueño: «es la marca, debe durar unos segundos»):
        /// unos 2,5 s [P], y nunca se corta antes porque la carga acabe.
        /// </summary>
        public const int AnimMs = 2500;

        /// <summary>Fin del movimiento: el açaí llega al primer plano (ms) [P].</summary>
        public const int MotionMs = 1800;

        /// <summary>Fin del chorro: llega de la boca de la cuia al río (ms) [P].</summary>
        public const int FallMs = 520;

        /// <summary>Fin de los aros del impacto (ms) [P].</summary>
        public const int RippleEndMs = 1250;

        /// <summary>El nombre «Tintalú» entra bajo la cuia desde aquí (ms) [P].</summary>
        public const int WordmarkFromMs = 1650;

        /// <summary>Lo que tarda el nombre en entrar (ms) [P].</summary>
        public const int WordmarkFadeMs = 350;

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
        private readonly double _createdAt;
        private double _origin = -1.0;
        private float _tintFill;
        private bool _begun;
        private bool _reduceMotion;
        private bool _ended;
        private bool _seeked;
        private float _clockMs;
        private float _wordmarkAlpha;
        private int _frames;

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
            _images = BrandEmblemImages.Create(_root, halo);
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
        public long FirstFrameMs => _origin < 0.0 ? -1L : (long)((_origin - _createdAt) * 1000.0);

        /// <summary>
        /// La animación está corriendo y aún no llegó a su último fotograma: cada fotograma pintado enseña algo nuevo (false antes de
        /// <see cref="Begin"/>, con Reducir movimiento, al acabar, al quitarse y en las capturas con <see cref="Seek"/>).
        /// </summary>
        public bool Animating => !_ended && _begun && !_reduceMotion && !_seeked && _clockMs < AnimMs;

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
                    return _origin >= 0.0 && (Time.realtimeSinceStartupAsDouble - _origin) * 1000.0 >= ReduceMotionHoldMs;
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

        /// <summary>
        /// Arranca la animación, con lo que se sabe ya del perfil: con <paramref name="reduceMotion"/>, salta a su último fotograma y
        /// se queda quieta. El reloj cuenta desde que se creó la intro, no desde aquí: no se alarga nada.
        /// </summary>
        public void Begin(bool reduceMotion)
        {
            if (_ended || _begun)
            {
                return;
            }

            _begun = true;
            _reduceMotion = reduceMotion;
            Apply(reduceMotion ? AnimMs : CurrentMs());
        }

        /// <summary>
        /// Lleva la animación al instante de ahora sin contar un fotograma: GameBootstrap la llama justo antes de dejar pintar uno
        /// entre dos bloques de la carga (el <see cref="Tick"/> de ese fotograma fue antes del bloque). Sin asignaciones.
        /// </summary>
        public void Refresh()
        {
            if (Animating)
            {
                Apply(CurrentMs());
            }
        }

        /// <summary>Un fotograma: avanza el reloj con el tiempo real (sin asignaciones). Quieta antes de <see cref="Begin"/>, con Reducir movimiento y tras el final.</summary>
        public void Tick()
        {
            if (_ended)
            {
                return;
            }

            _frames++;
            if (_origin < 0.0)
            {
                _origin = Time.realtimeSinceStartupAsDouble; // su primer fotograma a la vista: aquí empieza su reloj
            }

            if (!_begun || _reduceMotion || _seeked || _clockMs >= AnimMs)
            {
                return;
            }

            Apply(CurrentMs());
        }

        /// <summary>Lleva la animación al instante <paramref name="ms"/> y la deja ahí (capturas y tests deterministas).</summary>
        public void Seek(float ms)
        {
            _seeked = true;
            _begun = true;
            Apply(ms);
        }

        /// <summary>Rehace la composición con la escena <paramref name="art"/> (otra pantalla: la pantalla de arranque la rehízo).</summary>
        internal void Layout(BrandEmblemArt art)
        {
            _images.Place(art);
            Apply(_clockMs);
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
            };
            _ended = true;
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }

            return report;
        }

        /// <summary>Nada que liberar: las texturas del emblema son de la pantalla de arranque.</summary>
        internal void Dispose()
        {
        }

        /// <summary>
        /// Estado de la animación en <paramref name="ms"/> (sin asignaciones): el chorro baja de la boca de la cuia hasta el agua
        /// (<see cref="FallMs"/>); entonces salen los aros (hasta <see cref="RippleEndMs"/>) y el açaí avanza por el cauce, tramo a
        /// tramo y frenando, hasta el primer plano (<see cref="AnimMs"/>). Después, quieta: la escena entera.
        /// </summary>
        private void Apply(float ms)
        {
            float t = Mathf.Clamp(ms, 0f, AnimMs);
            float w = Mathf.Clamp01((t - WordmarkFromMs) / WordmarkFadeMs);
            _wordmarkAlpha = w * w * (3f - (2f * w));
            _clockMs = t;
            float pour = Mathf.Clamp01(t / FallMs);
            _images.Stream.fillAmount = 1f - ((1f - pour) * (1f - pour));

            float ripple = t <= FallMs ? -1f : Mathf.Clamp01((t - FallMs) / (float)(RippleEndMs - FallMs));
            float rs = ripple < 0f ? 0.3f : 0.3f + (0.7f * (1f - ((1f - ripple) * (1f - ripple))));
            _images.Impact.rectTransform.localScale = new Vector3(rs, rs, 1f);
            _images.Impact.color = new Color(1f, 1f, 1f, ripple < 0f ? 0f : Mathf.Clamp01(ripple * 2.5f));

            // El frente del açaí recorre los tramos río abajo: cada tramo, opaco y con su frente en lengua, se enciende entero al
            // llegar el frente (nunca a medias: ni velos ni cortes rectos).
            float spread = t <= FallMs ? 0f : Mathf.Clamp01((t - FallMs) / (MotionMs - FallMs));
            float eased = 1f - Mathf.Pow(1f - spread, 1.6f);
            _tintFill = eased;
            Image[] acai = _images.Acai;
            float front = eased * acai.Length;
            for (int k = 0; k < acai.Length; k++)
            {
                bool lit = spread > 0f && (front > k || eased >= 1f);
                acai[k].color = new Color(1f, 1f, 1f, lit ? 1f : 0f);
            }
        }

        // Del primer fotograma a ahora (ms); si aún no se pintó ninguno (Begin antes del primer Tick), el reloj empieza aquí.
        private float CurrentMs()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (_origin < 0.0)
            {
                _origin = now;
            }

            return (float)((now - _origin) * 1000.0);
        }
    }
}
