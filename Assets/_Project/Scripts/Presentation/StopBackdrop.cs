#nullable enable
using System;
using AF.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Fondo de la parada del Mapa del río en el tablero (D-013, punto 9, Q5-A; 07 §2.1; propuesta del Mapa §10.1-§10.6;
    /// direccion-visual-v2 §3.4; tarea M1.4): sus capas van en la escena delante del panorama y detrás de la garza, en un Canvas
    /// anidado propio (guía 8, «Reconstrucción de Canvas»), y ninguna recibe toques (VA-08). De atrás adelante:
    /// <list type="number">
    /// <item><b>0a, cielo</b> con la hora del mundo de 07 §2.1 (o la luz de la parada: atardecer en la 9, alba en la 19), un
    /// degradado por vértice con alfa ≤ <see cref="SkyMaxAlpha"/> sobre el cielo del panorama [P]; en el atardecer y el alba,
    /// hasta <see cref="SkyLowMaxAlpha"/> en el tercio de abajo, para que la hora se lea a tamaño de teléfono. El mundo 1 va
    /// sin tinte: el panorama ya es su alba dorada.</item>
    /// <item><b>0e, banda de agua del primer plano</b> con el <c>water_base</c> del mundo (07 §2.1; VA-23: ΔE00 ≤ 5), opaca bajo
    /// los <see cref="BandFadeDp"/> dp de junto al horizonte, donde deja ver el reflejo del cielo del panorama, con ondas finas
    /// del mismo tono. En el alba, bruma baja sobre ella, que entra con un degradado de <see cref="MistFadeDp"/> dp.</item>
    /// <item><b>0d, piezas por código</b>: la línea del Tapajós (#2E8B8B delante) contra el Amazonas (#B08A5A detrás) en
    /// Santarém, una franja de agua de ≥ 28 dp con las ondas de la banda, y la diagonal negra y beige del Encuentro (#4A2C14 y
    /// #B08A5A), que empieza en el borde de la pantalla. Las dos aguas se juntan en una costura recta de <see cref="SeamDp"/>
    /// dp de #E8F0E8 al 60 % con <see cref="SeamBlendDp"/> dp de mezcla a cada lado. Ya son definitivas: salen también en
    /// release.</item>
    /// <item><b>0c, héroe</b>: el PNG de su ranura (<c>bg_hero_&lt;sitio&gt;</c>, <see cref="ArtSlots"/>) de
    /// <see cref="HeroWidthDp"/> × <see cref="HeroHeightDp"/> dp con el pie en el horizonte (≤ 48 dp por encima y ≤ 32 por debajo)
    /// y su reflejo volteado al <see cref="ReflectionAlpha"/> en la línea del agua, que se apaga con la profundidad
    /// (<see cref="StopReflectionFade"/>, VA-21) y va recortado a <see cref="ReflectionDp"/> dp; sin PNG, la silueta
    /// de <see cref="StopShapes"/>, solo con <see cref="MapArtRule.ShowProvisional"/> (en release, sin héroe). Las piezas del
    /// kit (<c>kit:&lt;pieza&gt;</c>: la baliza y la orilla de Nauta) van igual, como silueta provisional.</item>
    /// </list>
    /// <see cref="LutColor"/> es la luz de la parada (solo <c>dusk</c> y <c>dawn</c>): quien la usa la multiplica sobre el panorama;
    /// aquí tiñe el héroe y no la banda (VA-23). La LUT de temporada la añade SE2 y el velo de Muy difícil no se toca.
    /// <para>Sin parada (sin catálogo, o del 201 en adelante mientras no haya paradas) no pinta nada: el aspecto de hoy. Al
    /// cambiar de composición puede fundir en <c>fadeMs</c> (<see cref="Apply"/>): la nueva entra encima con dos juegos de capas
    /// que se alternan; la vieja se queda entera debajo, así que su banda opaca nunca deja ver el panorama a mitad del fundido,
    /// y solo lo que la nueva no tapa (cielo, piezas y héroe) se va con ella. Solo mallas por vértice, sin texturas en tiempo
    /// de ejecución; construir una composición cuesta décimas de milisegundo (<see cref="BuildMs"/>) y <see cref="Tick"/> no
    /// asigna memoria.</para>
    /// </summary>
    public sealed class StopBackdrop
    {
        /// <summary>Ancho del héroe (dp; 07 §2.1).</summary>
        public const float HeroWidthDp = 240f;

        /// <summary>Alto del héroe (dp; 07 §2.1).</summary>
        public const float HeroHeightDp = 80f;

        /// <summary>Silueta del héroe sobre el horizonte, como mucho (dp; 07 §2.1).</summary>
        public const float HeroAboveDp = 48f;

        /// <summary>Orilla y reflejo del PNG del héroe bajo el horizonte, como mucho (dp; 07 §2.1).</summary>
        public const float HeroBelowDp = 32f;

        /// <summary>Alto del reflejo que alarga el código, hasta la franja de 44 dp (dp; 07 §2.1; propuesta del Mapa §10.2).</summary>
        public const float ReflectionDp = 44f;

        /// <summary>Opacidad del reflejo volteado en la línea del agua [P] (plan del Mapa, M1.4).</summary>
        public const float ReflectionAlpha = 0.35f;

        /// <summary>Tope de opacidad del tinte del cielo (plan del Mapa, M1.4) [P].</summary>
        public const float SkyMaxAlpha = 0.5f;

        /// <summary>Tope de opacidad del tinte en el tercio de abajo del cielo, solo con la luz <c>dusk</c> o <c>dawn</c> [P].</summary>
        public const float SkyLowMaxAlpha = 0.7f;

        /// <summary>Grosor de la costura entre dos aguas (dp) [P].</summary>
        public const float SeamDp = 1.5f;

        /// <summary>Mezcla de las dos aguas a cada lado de la costura (dp) [P].</summary>
        public const float SeamBlendDp = 4f;

        /// <summary>Degradado del borde de arriba de la bruma del alba (dp) [P].</summary>
        public const float MistFadeDp = 8f;

        /// <summary>Franja junto al horizonte en la que la banda de agua pasa de transparente a opaca (dp) [P].</summary>
        public const float BandFadeDp = 16f;

        /// <summary>Pieza por código de Santarém: el Tapajós claro delante y el Amazonas blanco detrás (07 §2.1).</summary>
        public const string CodeClearWhite = "codigo:dos_aguas_clara_blanca";

        /// <summary>Pieza por código del Encuentro de las Aguas: la diagonal negra y beige (07 §2.1).</summary>
        public const string CodeBlackWhite = "codigo:dos_aguas_negra_blanca";

        /// <summary>Luz de la parada 9 (Leticia al atardecer).</summary>
        public const string LutDusk = "dusk";

        /// <summary>Luz de la parada 19 (Pacaya-Samiria al alba).</summary>
        public const string LutDawn = "dawn";

        /// <summary>Origen del héroe: el PNG de su ranura.</summary>
        public const string HeroPng = "png";

        /// <summary>Origen del héroe: la silueta provisional de <see cref="StopShapes"/>.</summary>
        public const string HeroShape = "shape";

        /// <summary>Sin héroe (sin parada, pieza por código o provisional no permitido).</summary>
        public const string HeroNone = "none";

        /// <summary>Multiplicador de la luz de atardecer sobre el panorama y el héroe [P].</summary>
        public static readonly Color DuskLut = new Color(1f, 0.88f, 0.78f, 1f);

        /// <summary>Multiplicador de la luz del alba sobre el panorama y el héroe [P].</summary>
        public static readonly Color DawnLut = new Color(0.86f, 0.84f, 0.97f, 1f);

        // Cielo por mundo (07 §2.1, columna «Hora del día» [P]): arriba, en el horizonte, y su opacidad arriba y abajo.
        private static readonly Color NoonTop = Palette.Hex(0x3FA7E0);
        private static readonly Color NoonHorizon = Palette.Hex(0xBDE8F2);
        private static readonly Color AfternoonTop = Palette.Hex(0x5C9BD1);
        private static readonly Color AfternoonHorizon = Palette.Hex(0xF4C37E);
        private static readonly Color CloudyTop = Palette.Hex(0x7FB2D4);
        private static readonly Color CloudyHorizon = Palette.Hex(0xDCEBE4);
        private static readonly Color SunsetTop = Palette.Hex(0x6E86C9);
        private static readonly Color SunsetHorizon = Palette.Hex(0xF7A766);
        private static readonly Color DuskTop = Palette.Hex(0x6574C0);
        private static readonly Color DuskHorizon = Palette.Hex(0xF59A5C);
        private static readonly Color DawnTop = Palette.Hex(0xA7A3D6);
        private static readonly Color DawnHorizon = Palette.Hex(0xF6A99B);
        private static readonly Color DawnMist = Palette.Hex(0xEEF0F2);

        // Costura entre dos aguas (07 §2.1) [P].
        private static readonly Color SeamColor = Palette.Hex(0xE8F0E8);
        private const float SeamAlpha = 0.6f;

        private const int RippleRows = 9;
        private const float MistDp = 14f;

        // Calma sobre el panel (VA-21: 16 dp) más el medio grosor de la costura y un margen: la diagonal del Encuentro, cuya
        // costura sobre el agua negra pasaría de ΔL* 30, queda entera por encima.
        private const float CalmDp = 18f;

        // La costura de Santarém sí puede entrar en esos 16 dp (sobre el agua clara se queda en ΔL* < 30): su franja de agua
        // blanca llega hasta 16 dp sobre el panel y su mezcla, hasta 12.
        private const float CalmClearDp = 16f;

        private static readonly System.Diagnostics.Stopwatch BuildWatch = new System.Diagnostics.Stopwatch();

        private readonly RectTransform _root;
        private readonly Layer _a;
        private readonly Layer _b;
        private Layer _front;
        private Layer _back;
        private RiverStop? _stop;
        private string _water = string.Empty;
        private Color _waterColor = Palette.WaterWhite;
        private Color _lut = Color.white;
        private Color _lutFrom = Color.white;
        private string _heroSource = HeroNone;
        private string? _shape;
        private Sprite? _heroSprite;
        private float _fadeMs = -1f;
        private float _fadeTotal;
        private float _w = 360f;
        private float _horizon = ProceduralArt.PanoramaHorizonDp;
        private float _panelTop;
        private bool _hasBoard;
        private float _bandBottom;
        private bool _laidOut;
        private Rect _heroRect;

        /// <summary>
        /// Crea las capas bajo <paramref name="scene"/> (sin parada: nada se ve). Quien la llama la pone donde toca en la pila
        /// (GameScreen: tras el panorama y antes de la garza).
        /// </summary>
        public StopBackdrop(RectTransform scene, ProceduralArt? art)
        {
            Art = art;
            var go = new GameObject("StopBackdrop", typeof(RectTransform));
            _root = (RectTransform)go.transform;
            _root.SetParent(scene, false);
            Stretch(_root);
            go.AddComponent<Canvas>(); // anidado: sus fundidos y sus mallas no reconstruyen el lienzo de la pantalla (guía 8)
            _a = new Layer(_root, "A");
            _b = new Layer(_root, "B");
            _front = _a;
            _back = _b;
            go.SetActive(false);
        }

        /// <summary>
        /// Busca el PNG de un héroe por su ranura (tests: el PNG final sin tocar el catálogo de ranuras). Null =
        /// <see cref="ArtSlots.Find"/>.
        /// </summary>
        public static Func<string, Sprite?>? HeroSpriteOverride { get; set; }

        /// <summary>Arte compartido de la pantalla (los kits pintados lo usarán cuando lleguen).</summary>
        public ProceduralArt? Art { get; }

        /// <summary>Raíz de las capas (en la escena).</summary>
        public RectTransform Root => _root;

        /// <summary>Parada que se pinta ahora (null = ninguna: el aspecto de hoy).</summary>
        public RiverStop? Stop => _stop;

        /// <summary>Agua base del mundo con la que se pinta la banda (<c>white</c>, <c>clear</c> o <c>black</c>).</summary>
        public string Water => _water;

        /// <summary>Color de la banda del primer plano: el preset del agua base del mundo (VA-23).</summary>
        public Color BandColor => _waterColor;

        /// <summary>
        /// Luz de la parada: <see cref="DuskLut"/> en <c>dusk</c>, <see cref="DawnLut"/> en <c>dawn</c> y blanco en las demás y sin
        /// parada. Es la de destino: durante el fundido, la que se ve es <see cref="DisplayedLutColor"/>.
        /// </summary>
        public Color LutColor => _lut;

        /// <summary>La luz que se ve ahora: la de destino, o entre la anterior y ella durante el fundido.</summary>
        public Color DisplayedLutColor => Fading ? Color.Lerp(_lutFrom, _lut, FadeProgress) : _lut;

        /// <summary>Origen del héroe: <see cref="HeroPng"/>, <see cref="HeroShape"/> o <see cref="HeroNone"/>.</summary>
        public string HeroSource => _heroSource;

        /// <summary>Forma de <see cref="StopShapes"/> que se pinta como héroe, o null.</summary>
        public string? Shape => _heroSource == HeroShape ? _shape : null;

        /// <summary>Pieza por código o del kit del fondo de la parada (<c>codigo:</c> o <c>kit:</c>), o null.</summary>
        public string? Code => _stop?.Backdrop.Code;

        /// <summary>
        /// Rectángulo del héroe en dp del lienzo (x desde la izquierda, y desde arriba; 240 × 80 a escala del ancho, con el pie
        /// en el horizonte): lo que TiTino no debe tapar (TI6a). Vacío sin héroe.
        /// </summary>
        public Rect HeroRect => _heroSource == HeroNone ? new Rect(0f, 0f, 0f, 0f) : _heroRect;

        /// <summary>Hay un fundido en curso.</summary>
        public bool Fading => _fadeMs >= 0f;

        /// <summary>Avance del fundido, de 0 a 1 (1 sin fundido).</summary>
        public float FadeProgress => _fadeMs < 0f ? 1f : Mathf.Clamp01(_fadeMs / Mathf.Max(1f, _fadeTotal));

        /// <summary>Opacidad de las capas de la parada que entra o que se ve (tests).</summary>
        public float FrontAlpha => _front.Group.alpha;

        /// <summary>Opacidad de las capas de la parada que se va (1 durante el fundido: se queda entera debajo; 0 sin él; tests).</summary>
        public float BackAlpha => _back.Root.gameObject.activeSelf ? _back.Group.alpha : 0f;

        /// <summary>Opacidad del cielo, las piezas y el héroe de la parada que se va (0 sin fundido; tests).</summary>
        public float BackDetailAlpha => _back.Root.gameObject.activeSelf ? _back.Group.alpha * _back.Sky.canvasRenderer.GetAlpha() : 0f;

        /// <summary>Opacidad de la banda de agua de la parada que se va (0 sin fundido; tests).</summary>
        public float BackBandAlpha => _back.Root.gameObject.activeSelf ? _back.Group.alpha * _back.Band.canvasRenderer.GetAlpha() : 0f;

        /// <summary>Lo que tardó la última composición (<see cref="Apply"/> más su maquetación), en ms.</summary>
        public double BuildMs { get; private set; }

        /// <summary>Cielo de la parada que se ve (tests).</summary>
        public StopShapeGraphic SkyGraphic => _front.Sky;

        /// <summary>Banda de agua de la parada que se ve (tests).</summary>
        public StopShapeGraphic BandGraphic => _front.Band;

        /// <summary>Piezas por código de la parada que se ve (tests).</summary>
        public StopShapeGraphic CodeGraphic => _front.Code;

        /// <summary>Silueta del héroe de la parada que se ve (tests).</summary>
        public StopShapeGraphic ShapeGraphic => _front.Shape;

        /// <summary>PNG del héroe de la parada que se ve (tests).</summary>
        public Image HeroImage => _front.Hero;

        /// <summary>Reflejo del PNG del héroe de la parada que se ve (tests).</summary>
        public Image HeroReflection => _front.Reflection;

        /// <summary>Recorte del reflejo del PNG (tests).</summary>
        public RectTransform HeroReflectionClip => _front.ReflectionClip;

        /// <summary>Atenuación con la profundidad del reflejo del PNG (tests).</summary>
        public StopReflectionFade HeroReflectionFade => _front.ReflectionFade;

        /// <summary>Luz de la parada <paramref name="stop"/> (blanco salvo <c>dusk</c> y <c>dawn</c>).</summary>
        public static Color LutOf(RiverStop? stop)
        {
            string? lut = stop?.Backdrop.Lut;
            if (lut == LutDusk)
            {
                return DuskLut;
            }

            return lut == LutDawn ? DawnLut : Color.white;
        }

        /// <summary>
        /// Cielo de la parada (07 §2.1, hora del mundo; atardecer y alba por su luz) [P]: arriba, en el horizonte y su opacidad
        /// arriba (≤ <see cref="SkyMaxAlpha"/>) y en el horizonte (≤ <see cref="SkyMaxAlpha"/>, o ≤ <see cref="SkyLowMaxAlpha"/>
        /// con <c>dusk</c> o <c>dawn</c>). Sin tinte (alfa 0) en el mundo 1 y sin parada.
        /// </summary>
        public static void SkyOf(RiverStop? stop, out Color top, out Color horizon, out float alphaTop, out float alphaHorizon)
        {
            top = Color.white;
            horizon = Color.white;
            alphaTop = 0f;
            alphaHorizon = 0f;
            if (stop == null)
            {
                return;
            }

            string? lut = stop.Backdrop.Lut;
            if (lut == LutDusk)
            {
                Set(DuskTop, DuskHorizon, 0.45f, 0.4f, out top, out horizon, out alphaTop, out alphaHorizon);
                return;
            }

            if (lut == LutDawn)
            {
                Set(DawnTop, DawnHorizon, 0.45f, 0.7f, out top, out horizon, out alphaTop, out alphaHorizon);
                return;
            }

            switch (stop.World)
            {
                case 2: // mediodía luminoso, cielo alto
                    Set(NoonTop, NoonHorizon, 0.4f, 0.25f, out top, out horizon, out alphaTop, out alphaHorizon);
                    break;
                case 3: // tarde, luz rasante
                    Set(AfternoonTop, AfternoonHorizon, 0.4f, 0.3f, out top, out horizon, out alphaTop, out alphaHorizon);
                    break;
                case 4: // mañana nublada tras lluvia
                    Set(CloudyTop, CloudyHorizon, 0.35f, 0.25f, out top, out horizon, out alphaTop, out alphaHorizon);
                    break;
                case 5: // atardecer reflejado en agua quieta
                    Set(SunsetTop, SunsetHorizon, 0.45f, 0.35f, out top, out horizon, out alphaTop, out alphaHorizon);
                    break;
            }
        }

        /// <summary>
        /// Compone el fondo de la parada <paramref name="stop"/> con el agua base del mundo <paramref name="worldWater"/>
        /// (<c>white</c>, <c>clear</c> o <c>black</c>). Con <paramref name="crossfade"/> y una parada ya a la vista, funde en
        /// <paramref name="fadeMs"/> ms (la nueva entra mientras la vieja se va); si no, corte. Null = sin parada: el aspecto de
        /// hoy (con fundido, se va). Si ya se ve esa composición, no hace nada.
        /// </summary>
        public void Apply(RiverStop? stop, string worldWater, bool crossfade, float fadeMs)
        {
            string water = worldWater ?? string.Empty;
            if (ReferenceEquals(stop, _stop) && string.Equals(water, _water, StringComparison.Ordinal))
            {
                BuildMs = 0d;
                return;
            }

            BuildWatch.Restart();
            Color displayed = DisplayedLutColor;
            FinishFade(); // un cambio durante un fundido parte de lo que ya se ve entero
            bool showing = _stop != null;
            _stop = stop;
            _water = water;
            _waterColor = Palette.WaterBase(water);
            _lut = LutOf(stop);
            ResolveHero(stop);

            if (crossfade && fadeMs > 0f && showing)
            {
                // La que se veía queda detrás y se va; la nueva se construye en el otro juego de capas y entra encima.
                Layer old = _front;
                _front = _back;
                _back = old;
                _back.Group.alpha = 1f;
                _front.Group.alpha = 0f;
                _front.SetAlphas(1f, 1f);
                _front.Root.gameObject.SetActive(stop != null);
                _front.Root.SetAsLastSibling();
                _lutFrom = displayed;
                _fadeMs = 0f;
                _fadeTotal = fadeMs;
                _root.gameObject.SetActive(true);
            }
            else
            {
                _back.Root.gameObject.SetActive(false);
                _back.Group.alpha = 0f;
                _back.SetAlphas(1f, 1f);
                _front.Group.alpha = 1f;
                _front.SetAlphas(1f, 1f);
                _front.Root.gameObject.SetActive(stop != null);
                _lutFrom = _lut;
                _fadeMs = -1f;
                _root.gameObject.SetActive(stop != null);
            }

            if (_laidOut && stop != null)
            {
                Rebuild(_front);
            }

            BuildWatch.Stop();
            BuildMs = BuildWatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        /// Maqueta las capas para un lienzo de <paramref name="w"/> dp de ancho con el horizonte en <paramref name="horizon"/>
        /// (dp desde arriba; <c>GameScreen.HorizonFor</c>) y el panel en <paramref name="panelTop"/>. La banda de agua llega hasta
        /// <paramref name="bandBottom"/> (la viga del muelle; ≤ 0 = 96 dp bajo el panel). Solo reconstruye si algo cambió.
        /// </summary>
        public void Layout(float w, float horizon, float panelTop, bool hasBoard, float bandBottom = -1f)
        {
            if (bandBottom <= 0f)
            {
                bandBottom = Mathf.Max(panelTop, horizon) + 96f;
            }

            bool same = _laidOut && Mathf.Approximately(w, _w) && Mathf.Approximately(horizon, _horizon) && Mathf.Approximately(panelTop, _panelTop)
                && hasBoard == _hasBoard && Mathf.Approximately(bandBottom, _bandBottom);
            _w = w;
            _horizon = horizon;
            _panelTop = panelTop;
            _hasBoard = hasBoard;
            _bandBottom = bandBottom;
            _laidOut = true;
            if (same)
            {
                return;
            }

            FinishFade(); // la parada que se va no se vuelve a maquetar: el fundido termina
            Place(_a.Root, w);
            Place(_b.Root, w);
            if (_stop != null)
            {
                Rebuild(_front);
            }
        }

        /// <summary>
        /// Avanza el fundido <paramref name="dtMs"/> ms (sin asignar memoria). True mientras la luz que se ve cambia (quien
        /// tiñe el panorama con <see cref="DisplayedLutColor"/> la vuelve a aplicar), también en el fotograma en que termina.
        /// </summary>
        public bool Tick(float dtMs)
        {
            if (_fadeMs < 0f)
            {
                return false;
            }

            _fadeMs += Mathf.Max(0f, dtMs);
            float t = FadeProgress;

            // La nueva entra encima; la vieja se queda entera debajo, así que su banda opaca no deja ver el panorama a mitad del
            // fundido. Lo que la nueva no tapa (cielo, piezas y héroe) se va con 1 − t, y sin parada nueva se va también la banda.
            _front.Group.alpha = t;
            _back.SetAlphas(1f - t, _stop == null ? 1f - t : 1f);
            if (_fadeMs >= _fadeTotal)
            {
                _fadeMs = -1f;
                _front.Group.alpha = 1f;
                _back.Group.alpha = 0f;
                _back.SetAlphas(1f, 1f);
                _back.Root.gameObject.SetActive(false);
                if (_stop == null)
                {
                    _root.gameObject.SetActive(false);
                }
            }

            return true;
        }

        /// <summary>Termina en el acto el fundido en curso (sin él, nada).</summary>
        public void FinishFade()
        {
            if (_fadeMs >= 0f)
            {
                Tick(_fadeTotal + 1f);
            }
        }

        private void ResolveHero(RiverStop? stop)
        {
            _heroSource = HeroNone;
            _shape = null;
            _heroSprite = null;
            if (stop == null)
            {
                return;
            }

            string? hero = stop.Backdrop.Hero;
            if (hero != null)
            {
                Func<string, Sprite?>? find = HeroSpriteOverride;
                _heroSprite = find != null ? find(hero) : ArtSlots.Find(hero);
                if (_heroSprite != null)
                {
                    _heroSource = HeroPng; // el PNG final manda siempre, también en release (MapArtRule.CanShow)
                    return;
                }
            }

            _shape = StopShapes.ShapeOf(stop.Backdrop);
            if (_shape != null && MapArtRule.CanShow(MapArtRule.ShowProvisional, false))
            {
                _heroSource = HeroShape;
            }
        }

        // Vuelve a construir las mallas del juego de capas con la composición de ahora.
        private void Rebuild(Layer layer)
        {
            float w = _w;
            float k = w / 360f;
            float horizon = _horizon;
            float visible = _hasBoard ? Mathf.Max(24f * k, _panelTop - horizon) : 64f * k;
            BuildSky(layer.Sky, w, horizon);
            BuildBand(layer.Band, w, horizon, _bandBottom, k);
            BuildCode(layer.Code, w, horizon, visible, k);
            BuildHero(layer, w, horizon, k);
        }

        private void BuildSky(StopShapeGraphic g, float w, float horizon)
        {
            g.Clear();
            SkyOf(_stop, out Color top, out Color bottom, out float aTop, out float aBottom);
            if (aTop <= 0f && aBottom <= 0f)
            {
                g.Commit();
                return;
            }

            // Arriba, ≤ SkyMaxAlpha; en el tercio de abajo puede subir hasta SkyLowMaxAlpha con el atardecer y el alba.
            string? lut = _stop?.Backdrop.Lut;
            bool twilight = lut == LutDusk || lut == LutDawn;
            top.a = Mathf.Min(aTop, SkyMaxAlpha);
            bottom.a = Mathf.Min(aBottom, twilight ? SkyLowMaxAlpha : SkyMaxAlpha);
            const float lowThird = 2f / 3f;
            Color low = Color.Lerp(top, bottom, lowThird);
            low.a = Mathf.Min(low.a, SkyMaxAlpha);
            float yLow = horizon * lowThird;
            g.Rect(0f, 0f, w, yLow, top, low);
            g.Rect(0f, yLow, w, horizon, low, bottom);
            g.Commit();
        }

        // Banda del primer plano (VA-23): del horizonte (transparente) a BandFadeDp (opaca) y opaca hasta la viga del muelle, con
        // ondas en perspectiva de 1 dp, 3 tramos por fila, a ΔE00 ≤ 5 del preset. En el alba, bruma baja.
        private void BuildBand(StopShapeGraphic g, float w, float horizon, float bottom, float k)
        {
            g.Clear();
            Color water = _waterColor;
            Color clear = water;
            clear.a = 0f;
            float fadeEnd = horizon + (BandFadeDp * k);
            g.Rect(0f, horizon, w, fadeEnd, clear, water);
            g.Rect(0f, fadeEnd, w, Mathf.Max(fadeEnd, bottom), water, water);

            Ripples(g, w, fadeEnd, bottom, RippleColor(water), k);

            if (_stop != null && _stop.Backdrop.Lut == LutDawn)
            {
                // Bruma baja: entra con un degradado (sin arista arriba), a 0,45 junto al horizonte, y se va hacia el primer plano.
                Color mist = DawnMist;
                mist.a = 0.45f;
                Color none = DawnMist;
                none.a = 0f;
                float mistTop = horizon - (4f * k);
                g.Rect(0f, mistTop - (MistFadeDp * k), w, mistTop, none, mist);
                g.Rect(0f, mistTop, w, horizon + (MistDp * k), mist, none);
            }

            g.Commit();
        }

        // Ondas en perspectiva de 1 dp, 3 tramos por fila, desde start y sin pasar de limit (la banda y la franja de Santarém).
        private static void Ripples(StopShapeGraphic g, float w, float start, float limit, Color ripple, float k)
        {
            for (int i = 1; i <= RippleRows; i++)
            {
                float y = start + (k * 2.4f * Mathf.Pow(i, 1.5f));
                if (y >= limit - 1f)
                {
                    break;
                }

                float th = Mathf.Lerp(0.8f, 1.6f, i / (float)RippleRows) * k;
                for (int s = 0; s < 3; s++)
                {
                    float u = Frac((i * 0.618f) + (s * 0.37f));
                    float len = (24f + (36f * Frac((i * 0.27f) + (s * 0.53f)))) * k;
                    float x = (u * w) - (len * 0.5f);
                    g.Rect(x, y, x + len, y + th, ripple, ripple);
                }
            }
        }

        /// <summary>Color de las ondas de la banda: el preset con +4 L* (a ΔE00 ≤ 5, VA-23).</summary>
        public static Color RippleColor(Color water)
        {
            return ColorMath.AddLabLightness(water, 4f);
        }

        // Líneas entre aguas (07 §2.1), siempre por encima del primer plano: la del Tapajós, recta, con el Amazonas en una franja
        // de agua de hasta 30 dp (28 con 2 filas) con las ondas de la banda, y la diagonal del Encuentro desde el borde izquierdo
        // de la pantalla hasta el 70 % de la franja visible sobre el panel. VA-21: en los 16 dp de encima del panel, ningún
        // detalle con ΔL* > 30 (la costura clara sobre el agua clara se queda en ≈ 29; la diagonal, sobre el agua negra, queda
        // fuera de esa franja).
        private void BuildCode(StopShapeGraphic g, float w, float horizon, float visible, float k)
        {
            g.Clear();
            string? code = _stop?.Backdrop.Code;
            Color far = Palette.WaterWhite;
            Color farTop = far;
            farTop.a = 0f;
            if (code == CodeClearWhite)
            {
                // El Amazonas café detrás, del horizonte a la costura recta, con las ondas de la banda.
                float line = Mathf.Clamp(visible - (CalmClearDp * k), 12f * k, 30f * k);
                float top = horizon + (2f * k);
                float mid = top + (6f * k);
                float y = horizon + line;
                g.Rect(0f, top, w, mid, farTop, far);
                g.Rect(0f, mid, w, y, far, far);
                Ripples(g, w, mid, y - (SeamBlendDp * k), RippleColor(far), k);
                Seam(g, new Vector2(0f, y), new Vector2(w, y), far, _waterColor, horizon, k);
            }
            else if (code == CodeBlackWhite)
            {
                // La cuña beige del Amazonas sobre el Negro: del borde izquierdo, junto al horizonte, a (w, horizonte + 70 % de la
                // franja visible), sin arista en medio de la orilla.
                float depth = Mathf.Clamp(Mathf.Min(0.7f * visible, visible - (CalmDp * k)), 12f * k, 40f * k);
                float top = horizon + (3f * k);
                g.Rect(0f, horizon, w, top, farTop, far);
                Vector2 a = new Vector2(0f, top);
                Vector2 b = new Vector2(w, top);
                Vector2 c = new Vector2(w, horizon + depth);
                g.Triangle(a, far, b, far, c, far);
                Seam(g, a, c, far, _waterColor, horizon, k);
            }

            g.Commit();
        }

        // Costura de a a b entre el agua de detrás (far, por encima) y la del primer plano (near, por debajo): SeamBlendDp dp de
        // mezcla a cada lado (en la costura, mitad y mitad), nunca por encima del horizonte, y la línea clara de SeamDp dp.
        private static void Seam(StopShapeGraphic g, Vector2 a, Vector2 b, Color far, Color near, float horizon, float k)
        {
            Vector2 d = b - a;
            Vector2 n = new Vector2(-d.y, d.x).normalized; // hacia el primer plano (la y crece hacia abajo)
            Vector2 blend = n * (SeamBlendDp * k);
            Color nearNone = near;
            nearNone.a = 0f;
            Color nearHalf = near;
            nearHalf.a = 0.5f;
            Color farHalf = far;
            farHalf.a = 0.5f;
            Color farNone = far;
            farNone.a = 0f;
            g.Quad(NotAbove(a - blend, horizon), NotAbove(b - blend, horizon), b, a, nearNone, nearHalf);
            g.Quad(a, b, b + blend, a + blend, farHalf, farNone);
            Color seam = SeamColor;
            seam.a = SeamAlpha;
            Vector2 h = n * (SeamDp * k * 0.5f);
            g.Quad(NotAbove(a - h, horizon), NotAbove(b - h, horizon), b + h, a + h, seam, seam);
        }

        private static Vector2 NotAbove(Vector2 p, float horizon)
        {
            return new Vector2(p.x, Mathf.Max(p.y, horizon));
        }

        private void BuildHero(Layer layer, float w, float horizon, float k)
        {
            float hw = HeroWidthDp * k;
            float hh = HeroHeightDp * k;
            float footX = w * 0.5f;
            _heroRect = new Rect(footX - (hw * 0.5f), horizon - (HeroAboveDp * k), hw, hh);
            layer.Shape.Clear();
            bool png = _heroSource == HeroPng && _heroSprite != null;
            layer.Hero.gameObject.SetActive(png);
            layer.ReflectionClip.gameObject.SetActive(png);
            layer.Reflection.gameObject.SetActive(png);
            if (png)
            {
                // El PNG con el pie en el horizonte (48 dp encima, 32 debajo) y su reflejo volteado en el recorte de 44 dp, al
                // ReflectionAlpha en la línea del agua y apagándose hasta 0 a los 44 dp, como el de las siluetas (VA-21).
                layer.Hero.sprite = _heroSprite;
                layer.Hero.color = new Color(_lut.r, _lut.g, _lut.b, 1f);
                PlaceTopLeft((RectTransform)layer.Hero.transform, _heroRect.x, _heroRect.y, hw, hh);
                PlaceTopLeft(layer.ReflectionClip, _heroRect.x, horizon, hw, ReflectionDp * k);
                layer.Reflection.sprite = _heroSprite;
                layer.Reflection.color = new Color(_lut.r, _lut.g, _lut.b, ReflectionAlpha);
                RectTransform rt = (RectTransform)layer.Reflection.transform;
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, HeroBelowDp / HeroHeightDp); // el horizonte del PNG
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(hw, hh);
                rt.localScale = new Vector3(1f, -1f, 1f);
                layer.ReflectionFade.FadeDp = ReflectionDp * k;
            }
            else if (_heroSource == HeroShape && _shape != null)
            {
                StopShapes.Draw(layer.Shape, _shape, footX, horizon, k, _lut, _waterColor, ReflectionAlpha, ReflectionDp);
            }

            layer.Shape.Commit();
        }

        private static void Set(Color top, Color horizon, float aTop, float aHorizon, out Color outTop, out Color outHorizon,
            out float outATop, out float outAHorizon)
        {
            outTop = top;
            outHorizon = horizon;
            outATop = aTop;
            outAHorizon = aHorizon;
        }

        private static float Frac(float v)
        {
            return v - Mathf.Floor(v);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 1f);
        }

        // Juego de capas a lo ancho del lienzo, con el pivote arriba a la izquierda: las mallas van en dp con la y hacia abajo.
        private static void Place(RectTransform rt, float w)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, 0f);
        }

        private static void PlaceTopLeft(RectTransform rt, float x, float yTop, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yTop);
            rt.sizeDelta = new Vector2(w, h);
        }

        // Un juego de capas: cielo, banda, piezas por código y héroe (PNG con su reflejo, o silueta), bajo un CanvasGroup.
        private sealed class Layer
        {
            public Layer(RectTransform parent, string name)
            {
                var go = new GameObject("Stop" + name, typeof(RectTransform), typeof(CanvasGroup));
                Root = (RectTransform)go.transform;
                Root.SetParent(parent, false);
                Group = go.GetComponent<CanvasGroup>();
                Group.interactable = false;
                Group.blocksRaycasts = false;
                Sky = Graphic("Sky");
                Band = Graphic("Band");
                Code = Graphic("Code");
                var clip = new GameObject("HeroReflectionClip", typeof(RectTransform), typeof(RectMask2D));
                ReflectionClip = (RectTransform)clip.transform;
                ReflectionClip.SetParent(Root, false);
                Reflection = NewImage("HeroReflection", ReflectionClip);
                ReflectionFade = Reflection.gameObject.AddComponent<StopReflectionFade>();
                Hero = NewImage("Hero", Root);
                Shape = Graphic("HeroShape");
                go.SetActive(false);
            }

            public RectTransform Root { get; }

            public CanvasGroup Group { get; }

            public StopShapeGraphic Sky { get; }

            public StopShapeGraphic Band { get; }

            public StopShapeGraphic Code { get; }

            public StopShapeGraphic Shape { get; }

            public Image Hero { get; }

            public RectTransform ReflectionClip { get; }

            public Image Reflection { get; }

            public StopReflectionFade ReflectionFade { get; }

            // Opacidad del cielo, las piezas y el héroe (detail) y de la banda (band), sin reconstruir mallas ni asignar memoria.
            public void SetAlphas(float detail, float band)
            {
                Band.canvasRenderer.SetAlpha(band);
                Sky.canvasRenderer.SetAlpha(detail);
                Code.canvasRenderer.SetAlpha(detail);
                Shape.canvasRenderer.SetAlpha(detail);
                Hero.canvasRenderer.SetAlpha(detail);
                Reflection.canvasRenderer.SetAlpha(detail);
            }

            private StopShapeGraphic Graphic(string name)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(StopShapeGraphic));
                var rt = (RectTransform)go.transform;
                rt.SetParent(Root, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                StopShapeGraphic g = go.GetComponent<StopShapeGraphic>();
                g.Setup();
                return g;
            }

            private static Image NewImage(string name, Transform parent)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                Image image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = false;
                go.SetActive(false);
                return image;
            }
        }
    }
}
