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

        /// <summary>La carga terminó antes que la animación y se cortó a medias (<c>cut</c>; false con Reducir movimiento).</summary>
        public bool Cut { get; internal set; }

        /// <summary>Fotogramas que llegaron a pintarse con la intro a la vista (<c>frames</c>).</summary>
        public int Frames { get; internal set; }

        /// <summary>Con Reducir movimiento: la intro salió quieta, en su último fotograma (<c>reduce_motion</c>).</summary>
        public bool ReduceMotion { get; internal set; }

        /// <summary>La animación llegó a empezar (se supo si había Reducir movimiento); false si el arranque se paró antes.</summary>
        public bool Begun { get; internal set; }
    }

    /// <summary>
    /// Intro de la marca (D-051, punto 3): el logo de Tintalú de la identidad v2 (D-045, «El río que se tiñe»), dibujado por código
    /// sobre el petróleo de la marca, el mismo fondo de la pantalla del sistema de Android 12+ (D-051, punto 2), para que empalmen.
    /// Una gota de açaí cae sobre el agua blanca del río y el río se tiñe desde el impacto, río abajo (la composición del icono, sin
    /// texto: 06 §2.1, regla 3, en el arranque).
    /// <list type="bullet">
    /// <item>Va en la pantalla de arranque (<see cref="BootSplash"/>), por encima de todo lo suyo, y solo mientras el juego carga:
    /// nunca añade espera. Al terminar la carga se corta (<see cref="End"/>) aunque la animación vaya a medias; si la carga dura más
    /// que la animación (<see cref="AnimMs"/>, menos de 1 s), se queda en su último fotograma, quieta.</item>
    /// <item>Su reloj es el tiempo real desde que se creó, sin tope por fotograma: la carga va en bloques síncronos largos y cada
    /// fotograma que llega a pintarse enseña el instante que toca. Hasta <see cref="Begin"/> (cuando se sabe si hay Reducir
    /// movimiento, con el perfil leído) sale en su primer fotograma; con Reducir movimiento, en el último, sin moverse.</item>
    /// <item>No recibe toques (el fondo de la pantalla de arranque ya los traga: aún no hay nada que tocar) y no asigna memoria por
    /// fotograma. Las texturas son suyas y se destruyen con la pantalla.</item>
    /// </list>
    /// </summary>
    public sealed class BrandIntro
    {
        /// <summary>Duración de la animación (ms): la caída, el impacto y el tinte río abajo (D-051: 1 s como mucho) [P].</summary>
        public const int AnimMs = 650;

        /// <summary>Fin de la caída de la gota (ms) [P].</summary>
        public const int FallMs = 260;

        /// <summary>Fin de las ondas del impacto (ms) [P].</summary>
        public const int RippleEndMs = 560;

        /// <summary>
        /// Petróleo de la marca (D-045; D-051, punto 2): #14444A, el tono medio del cielo del icono v2. Lo repiten
        /// <c>ProjectSetup.BrandPetroleum</c> y <c>af_brand_petroleum</c> de <c>AfIcon.androidlib</c>; un test EditMode los compara.
        /// </summary>
        public static readonly Color32 Petroleum = Palette.Hex(0x14444A);

        /// <summary>Punto del impacto a lo largo del río (fracción de su ancho, desde la izquierda): río abajo se tiñe [P].</summary>
        public const float ImpactU = 0.40f;

        // Colores de la identidad v2 (docs/art/marca-oficial/v2/generar.py): halo, selva de la orilla, agua blanca del Amazonas
        // (#B08A5A, glosario §9) y açaí (#6A2C91), con los extremos de sus degradados, y las ondas del impacto.
        // La pantalla de arranque usa los mismos (añadido de D-051): su emblema es el último fotograma de la intro.
        internal static readonly Color HaloColor = new Color(0x5F / 255f, 0xB3 / 255f, 0xA6 / 255f, 0.45f);
        internal static readonly Color Jungle = Palette.Hex(0x0E4232);
        internal static readonly Color WaterTop = Palette.Hex(0x8F6B3F);
        internal static readonly Color WaterMid = Palette.Hex(0xB08A5A);
        internal static readonly Color WaterLow = Palette.Hex(0xC29C6A);
        internal static readonly Color AcaiTop = Palette.Hex(0x4E1F6E);
        internal static readonly Color AcaiMid = Palette.Hex(0x6A2C91);
        internal static readonly Color AcaiLow = Palette.Hex(0x7E3AAE);
        private static readonly Color RippleColor = Palette.Hex(0xEBD5F8);
        internal static readonly Color WaterWave = Palette.Hex(0xE6CB9E);
        internal static readonly Color AcaiWave = Palette.Hex(0xCDA0F0);

        // La gota de açaí (la de la intro; antes, también la del logotipo provisional de la pantalla de arranque).
        private static readonly Color DropLight = Palette.Hex(0x9B5CC4);
        private static readonly Color DropDeep = Palette.Hex(0x4A1C68);
        private static readonly Color DropInk = Palette.Hex(0x2E0F44);

        // Medidas, en fracciones del ancho E del emblema [P]: el río, la selva sobre él, el halo, la gota y las ondas.
        private const float EmblemMaxDp = 300f;
        private const float EmblemMarginDp = 40f;
        private const float CenterY = 0.42f;
        internal const float RiverH = 0.16f;
        internal const float JungleH = 0.20f;
        private const float HaloSize = 1.45f;
        private const float DropSize = 0.24f;
        private const float DropStartAbove = 0.42f;
        private const float RippleW = 0.36f;
        private const float RippleH = 0.10f;

        private readonly Texture2D?[] _textures = new Texture2D?[2];
        private readonly Sprite?[] _sprites = new Sprite?[2];
        private readonly RectTransform _root;
        private readonly Image _background;
        private readonly Image _halo;
        private readonly Image _jungle;
        private readonly Image _river;
        private readonly Image _tint;
        private readonly Image _ripple;
        private readonly Image _drop;
        private readonly double _origin;
        private float _width = 360f;
        private float _height = 780f;
        private float _emblem;
        private float _dropX;
        private float _dropStartY;
        private float _dropEndY;
        private float _riverH;
        private float _tintFullW;
        private float _tintFill;
        private bool _begun;
        private bool _reduceMotion;
        private bool _ended;
        private bool _seeked;
        private float _clockMs;
        private int _frames;

        /// <summary>
        /// Interruptor de los tests (null = lo de siempre: sale con la pantalla de arranque). Los tests que lo cambian lo vuelven a
        /// null en su TearDown.
        /// </summary>
        public static bool? EnabledOverride { get; set; }

        /// <summary>La intro sale en la pantalla de arranque (<see cref="EnabledOverride"/> o, sin él, siempre).</summary>
        public static bool Enabled => EnabledOverride ?? true;

        /// <summary>
        /// Crea la intro, ya a la vista en su primer fotograma, dentro de <paramref name="parent"/> (la pantalla de arranque). Su reloj
        /// cuenta desde <paramref name="origin"/> (<see cref="Time.realtimeSinceStartupAsDouble"/> de cuando se creó la pantalla: el
        /// mismo origen que <c>load_ms</c>). El halo, la selva, el río y el açaí son los del emblema de la pantalla de arranque
        /// (suyos: los destruye ella; añadido de D-051), que es el último fotograma de esta intro: así empalman sin salto y no se
        /// pintan dos veces las mismas texturas antes del primer fotograma. La gota y las ondas son de la intro.
        /// </summary>
        internal BrandIntro(Transform parent, double origin, Sprite halo, Sprite jungle, Sprite river, Sprite tint)
        {
            _origin = origin;
            var go = new GameObject("BrandIntro", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _root = (RectTransform)go.transform;
            UiBuilder.Stretch(_root);

            _background = UiBuilder.Image("Petroleum", _root, null, Petroleum);
            UiBuilder.Stretch(_background.rectTransform);
            _halo = UiBuilder.Image("Halo", _root, halo, HaloColor);
            _jungle = UiBuilder.Image("Jungle", _root, jungle, Jungle);
            _river = UiBuilder.Image("River", _root, river, Color.white);
            _tint = UiBuilder.Image("Tint", _root, tint, Color.white);
            _ripple = UiBuilder.Image("Ripple", _root, Own(0, Ring(128, 40)), RippleColor);
            _drop = UiBuilder.Image("Drop", _root, Own(1, Drop(256)), Color.white);
            Layout(_width, _height);
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
        public long AgeMs => (long)((Time.realtimeSinceStartupAsDouble - _origin) * 1000.0);

        /// <summary>Lo teñido del río (relleno de la capa de açaí, de <see cref="ImpactU"/> a 1).</summary>
        public float TintFill => _tintFill;

        /// <summary>Opacidad de la gota (1 mientras cae; 0 tras el impacto).</summary>
        public float DropAlpha => _drop.color.a;

        /// <summary>Centro de la gota (dp desde arriba a la izquierda).</summary>
        public Vector2 DropCenter => new Vector2(_dropX, -_drop.rectTransform.anchoredPosition.y + (_emblem * DropSize * 0.5f));

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

        /// <summary>Un fotograma: avanza el reloj con el tiempo real (sin asignaciones). Quieta antes de <see cref="Begin"/>, con Reducir movimiento y tras el final.</summary>
        public void Tick()
        {
            if (_ended)
            {
                return;
            }

            _frames++;
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

        /// <summary>Rehace la composición para un lienzo de <paramref name="width"/> × <paramref name="height"/> dp.</summary>
        public void Layout(float width, float height)
        {
            _width = width;
            _height = height;
            EmblemGeometry(width, height, out float left, out float e, out float riverTop);
            _emblem = e;
            float riverH = e * RiverH;
            PlaceEmblem(_halo.rectTransform, _jungle.rectTransform, _river.rectTransform, _tint.rectTransform, width, height);
            // El açaí, desde el impacto hasta el final del río: crece en ancho río abajo (Apply), con su frente suave.
            _riverH = riverH;
            _tintFullW = (1f - ImpactU) * e;

            // La gota cae sobre el agua en ImpactU; las ondas, centradas en el impacto (un poco por debajo de la orilla de arriba).
            float d = e * DropSize;
            float impactX = left + (ImpactU * e);
            float impactY = riverTop + (riverH * 0.35f);
            _dropX = impactX;
            _dropEndY = impactY - d;
            _dropStartY = _dropEndY - (e * DropStartAbove);
            UiBuilder.Place(_drop.rectTransform, impactX - (d * 0.5f), _dropStartY, d, d);
            float rw = e * RippleW;
            float rh = e * RippleH;
            UiBuilder.Place(_ripple.rectTransform, impactX - (rw * 0.5f), impactY - (rh * 0.5f), rw, rh);
            _ripple.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _ripple.rectTransform.anchoredPosition = new Vector2(impactX, -impactY);
            Apply(_clockMs);
        }

        /// <summary>
        /// Medidas del emblema en un lienzo de <paramref name="width"/> × <paramref name="height"/> dp: su borde izquierdo
        /// (<paramref name="left"/>), su ancho E (<paramref name="e"/>) y la orilla de arriba del río (<paramref name="riverTop"/>,
        /// dp desde arriba). Las comparten la intro y la pantalla de arranque (añadido de D-051), para que empalmen.
        /// </summary>
        internal static void EmblemGeometry(float width, float height, out float left, out float e, out float riverTop)
        {
            e = Mathf.Min(width - (2f * EmblemMarginDp), EmblemMaxDp);
            left = (width - e) * 0.5f;
            riverTop = Mathf.Round(CenterY * height);
        }

        /// <summary>
        /// Coloca el halo, la selva, el río y el açaí del emblema (este, teñido entero: el último fotograma) para un lienzo de
        /// <paramref name="width"/> × <paramref name="height"/> dp. Lo usan la intro y la pantalla de arranque.
        /// </summary>
        internal static void PlaceEmblem(RectTransform halo, RectTransform jungle, RectTransform river, RectTransform tint, float width, float height)
        {
            EmblemGeometry(width, height, out float left, out float e, out float riverTop);
            float riverH = e * RiverH;
            float h = e * HaloSize;
            UiBuilder.Place(halo, (width - h) * 0.5f, riverTop - (h * 0.55f), h, h);
            UiBuilder.Place(jungle, left, riverTop - (e * JungleH) + 2f, e, e * JungleH);
            UiBuilder.Place(river, left, riverTop, e, riverH);
            UiBuilder.Place(tint, left + (ImpactU * e), riverTop, (1f - ImpactU) * e, riverH);
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

        /// <summary>Destruye las texturas y los sprites propios (el lienzo lo destruye la pantalla de arranque).</summary>
        internal void Dispose()
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                if (_sprites[i] != null)
                {
                    UnityEngine.Object.Destroy(_sprites[i]);
                    _sprites[i] = null;
                }

                if (_textures[i] != null)
                {
                    UnityEngine.Object.Destroy(_textures[i]);
                    _textures[i] = null;
                }
            }
        }

        /// <summary>
        /// Estado de la animación en <paramref name="ms"/> (sin asignaciones): la gota cae acelerando hasta el agua
        /// (<see cref="FallMs"/>); entonces desaparece, salen las ondas (hasta <see cref="RippleEndMs"/>) y el açaí avanza río abajo,
        /// frenando, hasta el final del río (<see cref="AnimMs"/>). Después, quieta.
        /// </summary>
        private void Apply(float ms)
        {
            float t = Mathf.Clamp(ms, 0f, AnimMs);
            _clockMs = t;
            float fall = Mathf.Clamp01(t / FallMs);
            float y = Mathf.Lerp(_dropStartY, _dropEndY, fall * fall);
            RectTransform drop = _drop.rectTransform;
            drop.anchoredPosition = new Vector2(drop.anchoredPosition.x, -y);
            _drop.color = new Color(1f, 1f, 1f, t < FallMs ? 1f : 0f);

            float ripple = t <= FallMs ? -1f : Mathf.Clamp01((t - FallMs) / (RippleEndMs - FallMs));
            float scale = ripple < 0f ? 0.4f : 0.4f + (0.8f * ripple);
            _ripple.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _ripple.color = new Color(RippleColor.r, RippleColor.g, RippleColor.b, ripple < 0f ? 0f : 1f - (ripple * ripple));

            float spread = t <= FallMs ? 0f : Mathf.Clamp01((t - FallMs) / (AnimMs - FallMs));
            float eased = 1f - ((1f - spread) * (1f - spread));
            _tintFill = spread <= 0f ? 0f : ImpactU + ((1f - ImpactU) * eased);
            _tint.rectTransform.sizeDelta = new Vector2(_tintFullW * eased, _riverH);
            _tint.color = new Color(1f, 1f, 1f, spread <= 0f ? 0f : 1f);
        }

        private float CurrentMs()
        {
            return (float)((Time.realtimeSinceStartupAsDouble - _origin) * 1000.0);
        }

        private Sprite Own(int slot, Texture2D tex)
        {
            _textures[slot] = tex;
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = tex.name;
            _sprites[slot] = sprite;
            return sprite;
        }

        private static Texture2D NewTexture(int w, int h, string name)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        // Fundido de los extremos de la selva: entra y sale del fondo, de izquierda a derecha.
        private static float EndFade(float u)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.12f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / 0.12f));
        }

        // Banda del río: degradado vertical (arriba top, en medio mid, abajo low), con dos ondas claras (wave, como las del icono) y
        // los extremos fundidos. La del açaí (tint) cubre del impacto al final del río: su frente izquierdo es suave y ondulado (el
        // tinte entra en el agua blanca) y su extremo derecho se funde como el del río.
        internal static Texture2D Band(int w, int h, Color top, Color mid, Color low, Color wave, bool tint)
        {
            Texture2D tex = NewTexture(w, h, tint ? "AF_IntroTint" : "AF_IntroRiver");
            var px = new Color32[w * h];
            float rightFade = tint ? 0.12f / (1f - ImpactU) : 0.12f;
            for (int y = 0; y < h; y++)
            {
                float v = 1f - (y / (float)(h - 1)); // 0 arriba, 1 abajo
                Color body = v < 0.25f ? Color.Lerp(top, mid, v / 0.25f) : Color.Lerp(mid, low, (v - 0.25f) / 0.75f);
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / rightFade));
                    if (tint)
                    {
                        float front = 0.03f + (0.03f * Mathf.Sin(v * 9f)); // frente ondulado
                        a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - front) / 0.10f));
                    }
                    else
                    {
                        a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.12f));
                    }

                    // Dos ondas: trazos finos que ondulan a lo largo del río, a tramos.
                    float waves = 0f;
                    for (int k = 0; k < 2; k++)
                    {
                        float v0 = k == 0 ? 0.42f : 0.72f;
                        float line = v0 + (0.06f * Mathf.Sin((u * 6.283f * (tint ? 2.5f : 4f)) + (k * 1.7f)));
                        float stroke = Mathf.Clamp01(1f - (Mathf.Abs(v - line) * h / 1.6f));
                        float segment = Mathf.Clamp01((Mathf.Sin((u * 6.283f * (tint ? 1.5f : 2.5f)) + (k * 2.3f)) * 3f) - 1f);
                        waves = Mathf.Max(waves, stroke * segment);
                    }

                    Color c = Color.Lerp(body, wave, 0.65f * waves);
                    px[(y * w) + x] = new Color(c.r, c.g, c.b, a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // Selva de la orilla de enfrente (blanca, se tiñe con el color de la Image): copas redondeadas y dos palmeras de açaí más
        // altas, con 1 px de suavizado y los extremos fundidos.
        internal static Texture2D Treeline(int w, int h)
        {
            Texture2D tex = NewTexture(w, h, "AF_IntroJungle");
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w;
                float top = 0.45f + (0.18f * Mathf.Abs(Mathf.Sin(x * 0.07f))) + (0.10f * Mathf.Abs(Mathf.Sin((x * 0.17f) + 0.9f)));
                top = Mathf.Max(top, Palm(u, 0.16f), Palm(u, 0.84f));
                float edge = Mathf.Clamp01(top) * (h - 1);
                float fade = EndFade(u);
                for (int y = 0; y < h; y++)
                {
                    float a = Mathf.Clamp01(edge - y + 0.5f) * fade;
                    px[(y * w) + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // Copa de una palmera de açaí en u0: un penacho estrecho que sube por encima de la selva.
        private static float Palm(float u, float u0)
        {
            float d = Mathf.Abs(u - u0) / 0.035f;
            return d >= 1f ? 0f : 0.98f * (1f - (d * d * 0.35f));
        }

        // Anillo elíptico blanco de borde suave (las ondas del impacto).
        private static Texture2D Ring(int w, int h)
        {
            Texture2D tex = NewTexture(w, h, "AF_IntroRipple");
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = ((x + 0.5f) / w) - 0.5f;
                    float dy = ((y + 0.5f) / h) - 0.5f;
                    float r = Mathf.Sqrt((dx * dx) + (dy * dy * (w / (float)h) * (w / (float)h))) * 2f; // 1 en el borde de la caja
                    float a = Mathf.Clamp01(1f - (Mathf.Abs(r - 0.82f) / 0.16f));
                    px[(y * w) + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // La gota de açaí (u, v en 0..1, v hacia arriba): un círculo abajo y su punta arriba, con un brillo arriba a la izquierda y
        // un contorno oscuro. Suavizado con 2 × 2 muestras por píxel.
        private static Texture2D Drop(int size)
        {
            Texture2D tex = NewTexture(size, size, "AF_IntroDrop");
            var px = new Color32[size * size];
            var c = new Vector2(0.5f, 0.38f);
            const float r = 0.30f;
            var tip = new Vector2(0.5f, 0.97f);
            Vector2 axis = (tip - c).normalized;
            float beta = Mathf.Acos(r / (tip - c).magnitude);
            Vector2 left = c + (r * Rotate(axis, beta));
            Vector2 right = c + (r * Rotate(axis, -beta));
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color sum = Color.clear;
                    for (int s = 0; s < 4; s++)
                    {
                        var p = new Vector2((x + 0.25f + (0.5f * (s & 1))) / size, (y + 0.25f + (0.5f * (s >> 1))) / size);

                        // Distancia al borde de la gota (> 0 dentro): la mayor de la del círculo y la del triángulo de la punta.
                        float inCircle = r - (p - c).magnitude;
                        float inTip = InTriangle(p, tip, left, right) ? EdgeDistance(p, tip, left, right) : -1f;
                        float inside = Mathf.Max(inCircle, inTip);
                        if (inside < 0f)
                        {
                            continue;
                        }

                        float shade = Mathf.Clamp01(((p.x - 0.30f) * 0.9f) + ((0.75f - p.y) * 0.6f));
                        Color col = Color.Lerp(DropLight, DropDeep, shade);
                        float shine = Mathf.Clamp01(1f - new Vector2((p.x - 0.38f) / 0.06f, (p.y - 0.47f) / 0.10f).magnitude);
                        col = Color.Lerp(col, Color.white, 0.75f * Mathf.SmoothStep(0f, 1f, shine));
                        if (inside < 0.018f)
                        {
                            col = Color.Lerp(col, DropInk, 0.85f);
                        }

                        sum += new Color(col.r, col.g, col.b, 1f);
                    }

                    Color avg = sum * 0.25f;
                    px[(y * size) + x] = avg.a <= 0f ? new Color32(0, 0, 0, 0) : (Color32)new Color(avg.r / avg.a, avg.g / avg.a, avg.b / avg.a, avg.a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2((cos * v.x) - (sin * v.y), (sin * v.x) + (cos * v.y));
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p, a, b);
            float d2 = Cross(p, b, c);
            float d3 = Cross(p, c, a);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(neg && pos);
        }

        // Distancia a los dos lados largos del triángulo de la punta (tip-left y tip-right).
        private static float EdgeDistance(Vector2 p, Vector2 tip, Vector2 left, Vector2 right)
        {
            return Mathf.Min(SegmentDistance(p, tip, left), SegmentDistance(p, tip, right));
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + (t * ab))).magnitude;
        }

        private static float Cross(Vector2 p, Vector2 a, Vector2 b)
        {
            return ((p.x - b.x) * (a.y - b.y)) - ((a.x - b.x) * (p.y - b.y));
        }
    }
}
