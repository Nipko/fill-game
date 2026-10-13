#nullable enable
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural provisional [P] del banner de entrada a un mundo (07 §2.2, regla 1; 07 §6.1 y §6.2; decisión del dueño
    /// del 2026-10-11): la viñeta de río del banner (cielo, orilla de selva, el agua del tipo del mundo con sus olas y el banco
    /// de arena mojada) con Capi ya dibujado encima, sentado en el banco (<c>capi_present</c>: el cuerpo va en la viñeta y la
    /// pata delantera en una pieza aparte, que la vista levanta), Rosa asomando del río (<c>rosa_surface</c>, y la vista la
    /// sumerge al cerrar: <c>rosa_dive</c>), el soplo de su exhalación, la onda y la espuma. Hojas de modelo de 07: Capi con
    /// pelaje #8B6A3E, panza #C8A872, nariz #4A3222 y contorno de tinta #2B2118 al 80 %, cuerpo redondo de perfil cuadrado,
    /// hocico ancho, ojos pequeños con un brillo y orejas chicas; Rosa rosa #E8A0B4 con dorso #C98A98 y vientre #F3D3DB,
    /// hocico largo, frente abombada, aleta dorsal baja y ojo pequeño, sin rasgos humanos. Sin ropa ni accesorios. Todo se
    /// genera la primera vez que se pide (la vista lo pide al abrir el banner: construir la pantalla o jugar un nivel sin banner
    /// no crea ninguna textura, <see cref="MascotsBuilt"/>) y se libera al cerrarlo (<see cref="ReleaseBannerArt"/>: cada banner
    /// sale una vez por mundo, así que no se queda en memoria toda la sesión); lo sustituye el arte del artista. Las mascotas y
    /// los lugares quedan pendientes de revisión del asesor cultural (07 §8.2).
    /// </summary>
    /// <remarks>
    /// <para>Coste (08 §5.4: carga de nivel &lt; 300 ms e hilo principal ≤ 8 ms por fotograma; guía 8): el banner se abre en el
    /// fotograma en que se carga el nivel (11, 41, 81 y 141), y pintar sus píxeles cuesta unos 230 ms de CPU en el Editor
    /// (revisión 2 de A4; la viñeta, a 3 px/dp con Capi dentro, se lleva casi todo). Por eso la vista lo pide con
    /// <see cref="RequestBannerArt"/>: los píxeles se pintan en un hilo del grupo y el hilo principal solo crea las texturas
    /// (<see cref="TryTakeBannerArt"/>); el banner entra cuando están. Se mide (<see cref="MascotsMs"/>, lo pintado, y
    /// <see cref="MascotsMainMs"/>, lo que tocó al hilo principal; la vista los lleva al <c>AF_STATE world_banner_art</c>) y se
    /// acota en EditMode (<c>WorldBannerArtTests</c>). Además: los senos y cosenos de los giros fijos de las elipses y las cajas
    /// se calculan una sola vez (<see cref="Rot"/>); Capi, Rosa y la pata solo evalúan su silueta en las celdas de 6 px que
    /// pueden tocarla (las distancias son 1-lipschitzianas, con margen, así que el resultado es el mismo que evaluando todos los
    /// píxeles); el lomo de Rosa se busca en una tabla de 97 puntos; y la viñeta calcula por columna las siluetas de la orilla y
    /// por fila el agua, el reflejo y las filas de olas.</para>
    /// <para>Capi va dibujado en la viñeta, no en una imagen aparte (revisión de la captura 69e): el lienzo funde cada gráfico
    /// por separado, así que con la tarjeta a medio fundir el agua se veía a través de su cuerpo.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado del cuadrado de Capi en la viñeta (dp): el animal sentado ocupa ≈ 0,92 del alto; sus patas, abajo.</summary>
        public const float CapiPresentDp = 100f;

        /// <summary>
        /// Hombro de la pata que presenta, en fracción del cuadrado de Capi (x desde la izquierda, y desde abajo): arriba del
        /// pecho, junto al cuello (no en la panza).
        /// </summary>
        public static readonly Vector2 CapiShoulder = new Vector2(0.50f, 0.405f);

        /// <summary>Ancho del sprite de la pata (dp): del hombro, por el codo, a la punta de los dedos.</summary>
        public const float CapiPawLengthDp = 48f;

        /// <summary>Alto del sprite de la pata (dp): el codo baja y la mano vuelve a subir.</summary>
        public const float CapiPawThickDp = 24f;

        /// <summary>
        /// Pivote de la pata, el centro del hombro (fracción de su sprite: x desde la izquierda, y desde abajo); la vista lo pone
        /// en <see cref="CapiShoulder"/> y gira la pata alrededor.
        /// </summary>
        public static readonly Vector2 CapiPawPivot = new Vector2(7.5f / CapiPawLengthDp, 15f / CapiPawThickDp);

        /// <summary>Ancho del sprite de Rosa (dp); el alto es <see cref="RosaHeightDp"/>.</summary>
        public const float RosaWidthDp = 104f;

        /// <summary>Alto del sprite de Rosa (dp).</summary>
        public const float RosaHeightDp = 66f;

        /// <summary>
        /// Respiradero de Rosa (fracción del sprite: x desde la izquierda, y desde abajo): en lo alto de la cabeza, justo detrás
        /// del melón (a un tercio de la cabeza desde la frente), sobre el contorno; de ahí sale el soplo.
        /// </summary>
        public static readonly Vector2 RosaBlowhole = new Vector2(0.42f, 0.765f);

        /// <summary>Lado del soplo de la exhalación (dp): el abanico de gotas nace en el centro de su canto de abajo.</summary>
        public const float RosaSprayDp = 30f;

        /// <summary>
        /// Centro del corte del cuerpo de Rosa con el agua (fracción del ancho del sprite) cuando asoma el 74 % de su alto
        /// (<c>WorldBannerView.RosaAbove</c>): el lomo entra en el agua entre 0,70 y 0,925 del ancho. Ahí va la espuma.
        /// </summary>
        public const float RosaCutCenter = 0.81f;

        /// <summary>Ancho del corte del cuerpo con el agua (fracción del ancho del sprite), con <see cref="RosaCutCenter"/>.</summary>
        public const float RosaCutWidth = 0.225f;

        /// <summary>Ancho de la espuma que tapa el corte del cuerpo de Rosa con el agua (dp).</summary>
        public const float RosaFoamWidthDp = 40f;

        /// <summary>Alto de la espuma (dp): una franja de unos 4 dp con burbujas, centrada en la línea del agua.</summary>
        public const float RosaFoamHeightDp = 9f;

        /// <summary>Radio de las esquinas de la viñeta del banner (dp).</summary>
        public const float BannerSceneRadiusDp = 10f;

        /// <summary>Línea del horizonte de la viñeta (fracción del alto desde arriba).</summary>
        public const float BannerSceneHorizon = 0.46f;

        /// <summary>Lo alto del banco de arena de Capi (fracción del alto desde arriba): ahí apoya las patas.</summary>
        public const float BannerSceneBankTop = 0.86f;

        /// <summary>Eje de Capi en la viñeta (fracción del ancho), en lo alto del banco.</summary>
        public const float BannerSceneCapiX = 0.17f;

        /// <summary>Las patas de Capi, este tanto (dp) por debajo de lo alto del banco: apoyadas en la arena, no en su canto.</summary>
        public const float BannerSceneCapiFootDp = 3f;

        /// <summary>El sol de la viñeta y su camino de destellos en el agua (fracción del ancho), detrás de Rosa.</summary>
        public const float BannerSceneSunX = 0.78f;

        /// <summary>Densidad de la viñeta (px por dp): la de las mascotas, porque Capi va dibujado en ella.</summary>
        public const float BannerScenePxPerDp = UiPxPerDp;

        /// <summary>Pelaje de Capi #8B6A3E (07 §6.1).</summary>
        public static readonly Color CapiFur = Palette.Hex(0x8B6A3E);

        /// <summary>Panza de Capi #C8A872 (07 §6.1).</summary>
        public static readonly Color CapiBelly = Palette.Hex(0xC8A872);

        /// <summary>Nariz de Capi #4A3222 (07 §6.1).</summary>
        public static readonly Color CapiNose = Palette.Hex(0x4A3222);

        /// <summary>Rosa #E8A0B4 (07 §6.2).</summary>
        public static readonly Color RosaPink = Palette.Hex(0xE8A0B4);

        /// <summary>Dorso de Rosa #C98A98 (07 §6.2).</summary>
        public static readonly Color RosaBack = Palette.Hex(0xC98A98);

        /// <summary>Vientre de Rosa #F3D3DB (07 §6.2).</summary>
        public static readonly Color RosaBelly = Palette.Hex(0xF3D3DB);

        /// <summary>Ancho de la onda de Rosa (dp).</summary>
        public const float RosaRippleWidthDp = 72f;

        /// <summary>Alto de la onda de Rosa (dp).</summary>
        public const float RosaRippleHeightDp = 14f;

        private const float MascotPxPerDp = UiPxPerDp;

        // Celdas de la poda de las siluetas (px): solo se evalúan las que pueden tocar la silueta o su contorno.
        private const int SilhouetteCell = 6;

        // Contorno de tinta de las mascotas: 2 px del arte a 1,5 px/dp ≈ 1,35 dp (07 §6.1: «trazo de contorno 2 px #2B2118 al 80 %»).
        private const float MascotOutlineDp = 1.35f;
        private static readonly Color MascotInk = new Color(0x2B / 255f, 0x21 / 255f, 0x18 / 255f, 0.8f);

        // Luz de borde de Capi: ancho (dp) dentro del contorno y dirección de la luz de juego (arriba a la izquierda).
        private const float MascotRimDp = 1.8f;
        private static readonly Vector2 MascotLight = new Vector2(-0.42f, 0.91f).normalized;

        // Contorno de Capi: la misma tinta, al 93 %. El lienzo mezcla en lineal, donde la tinta al 80 % sobre el cielo durazno y
        // el agua blanca se ve como un gris marrón a medias (un halo, no un trazo); al 93 % se ve como el 80 % de 07 §6.1 mezclado
        // en sRGB (≈ el contorno de Rosa, que va sobre el agua, más oscura).
        private static readonly Color CapiInk = new Color(MascotInk.r, MascotInk.g, MascotInk.b, 0.93f);

        // Azul claro de las gotas del soplo y su contorno fino: se leen sobre el cielo durazno y sobre el agua.
        private static readonly Color SprayDrop = new Color(0.80f, 0.92f, 0.99f, 0.97f);
        private static readonly Color SprayEdge = new Color(0.25f, 0.45f, 0.58f, 0.85f);

        // Banco de arena mojada [P] (revisión de la captura 69b: con los colores del muelle se leía como la tarima y en 81 y 141
        // se fundía con el agua canela): arena seca y clara arriba, más tostada abajo, mojada y oscura junto al agua, con el canto
        // oscuro donde la toca.
        private static readonly Color SandDry = Palette.Hex(0xEAD09A);
        private static readonly Color SandLow = Palette.Hex(0xCFAA70);
        private static readonly Color SandWet = Palette.Hex(0x93714A);
        private static readonly Color SandEdge = Palette.Hex(0x553D25);

        // Senos y cosenos de los giros fijos (grados) de las elipses y las cajas de las siluetas: se calculan una vez, no por
        // píxel ni por primitiva.
        private static readonly Vector2 Rot36 = Rot(36f);
        private static readonly Vector2 Rot34 = Rot(34f);
        private static readonly Vector2 Rot15 = Rot(15f);
        private static readonly Vector2 Rot12 = Rot(12f);
        private static readonly Vector2 Rot10 = Rot(10f);
        private static readonly Vector2 RotM12 = Rot(-12f);
        private static readonly Vector2 RotM20 = Rot(-20f);
        private static readonly Vector2 RotM30 = Rot(-30f);

        // Lomo de Rosa muestreado en t = i / 96 (la búsqueda gruesa toma uno de cada 4) y la aleta dorsal, fija.
        private const int RosaSpineSteps = 96;
        private static readonly Vector2[] RosaSpineTable = BuildRosaSpineTable();
        private static readonly Vector2 RosaFinAt = RosaSpine(0.42f) + (new Vector2(0.03f, 1f).normalized * (RosaRadius(0.42f) * 0.92f));

        private readonly Dictionary<string, Sprite> _bannerScenes = new Dictionary<string, Sprite>();
        private BannerArtJob? _bannerJob;
        private string? _bannerSceneKey;
        private Sprite? _capiPaw;
        private Sprite? _rosa;
        private Sprite? _rosaSpray;
        private Sprite? _rosaRipple;
        private Sprite? _rosaFoam;

        /// <summary>
        /// La pata delantera que presenta (<c>capi_present</c>): <see cref="CapiPawLengthDp"/> × <see cref="CapiPawThickDp"/>,
        /// del hombro (el pivote, <see cref="CapiPawPivot"/>) baja hacia delante hasta el codo y el antebrazo sube hasta la mano
        /// abierta con la palma hacia arriba, hacia la derecha. Si no está generada (<see cref="RequestBannerArt"/>), se genera
        /// aquí, en el hilo principal.
        /// </summary>
        public Sprite CapiPaw => _capiPaw != null ? _capiPaw : (_capiPaw = TimedMascot(DrawCapiPaw, PawSprite));

        /// <summary>
        /// Rosa asomando de perfil, mirando a la izquierda: la cabeza con la frente abombada y el hocico largo y el lomo arqueado
        /// con la aleta baja; lo de abajo del sprite queda bajo el agua (la vista lo recorta en la línea del agua).
        /// </summary>
        public Sprite Rosa => _rosa != null ? _rosa : (_rosa = TimedMascot(DrawRosa, RosaSprite));

        /// <summary>
        /// Soplo de la exhalación de Rosa (<see cref="RosaSprayDp"/>) [P, pendiente del asesor cultural]: una bruma breve y baja,
        /// más ancha que alta, que nace del respiradero (<see cref="RosaBlowhole"/>, el centro del canto de abajo y su pivote) y
        /// toca la cabeza, con 3 gotas pequeñas justo encima; no un surtidor de ballena (revisión de la captura 69d).
        /// </summary>
        public Sprite RosaSpray => _rosaSpray != null ? _rosaSpray : (_rosaSpray = TimedMascot(DrawRosaSpray, SpraySprite));

        /// <summary>
        /// Onda en el agua donde Rosa asoma y se sumerge (<see cref="RosaRippleWidthDp"/> × <see cref="RosaRippleHeightDp"/>):
        /// dos arcos concéntricos aplanados y rotos, blancos al 60 %; la vista los agranda y los apaga.
        /// </summary>
        public Sprite RosaRipple => _rosaRipple != null ? _rosaRipple : (_rosaRipple = TimedMascot(DrawRosaRipple, RippleSprite));

        /// <summary>
        /// Espuma de la línea del agua (<see cref="RosaFoamWidthDp"/> × <see cref="RosaFoamHeightDp"/>): una franja clara y
        /// ondulada con burbujas que tapa el corte recto del cuerpo de Rosa con el agua (<see cref="RosaCutCenter"/>).
        /// </summary>
        public Sprite RosaFoam => _rosaFoam != null ? _rosaFoam : (_rosaFoam = TimedMascot(DrawRosaFoam, FoamSprite));

        /// <summary>
        /// Alguna pieza del banner (la pata de Capi, Rosa, el soplo, la onda, la espuma o la viñeta con Capi) está generada:
        /// false al construir la pantalla y en cualquier nivel sin banner, mientras su arte se genera fuera del hilo principal
        /// (<see cref="RequestBannerArt"/>) y otra vez false cuando el banner se cierra.
        /// </summary>
        public bool MascotsBuilt => _capiPaw != null || _rosa != null || _rosaSpray != null || _rosaRipple != null
            || _rosaFoam != null || _bannerScenes.Count > 0;

        /// <summary>Viñetas del banner en memoria (como mucho 1: se guarda solo la última).</summary>
        public int BannerSceneCount => _bannerScenes.Count;

        /// <summary>
        /// Tiempo de CPU (ms) de pintar las piezas del banner desde la última liberación (las mascotas y la viñeta con Capi, en
        /// píxeles): en un hilo del grupo si se pidieron con <see cref="RequestBannerArt"/> en asíncrono, o en el hilo principal.
        /// Mide lo que espera el banner antes de entrar; la vista lo lleva al <c>AF_STATE world_banner_art</c> como
        /// <c>mascots_ms</c> para medirlo en el teléfono.
        /// </summary>
        public double MascotsMs { get; private set; }

        /// <summary>
        /// Tiempo (ms) que el arte del banner ocupó el hilo principal desde la última liberación: crear sus texturas (y pintarlas,
        /// si se generaron en el hilo principal). Es lo que cae en un fotograma (08 §5.4: hilo principal ≤ 8 ms y carga de nivel
        /// &lt; 300 ms); la vista lo lleva al <c>AF_STATE world_banner_art</c> como <c>main_ms</c>.
        /// </summary>
        public double MascotsMainMs { get; private set; }

        /// <summary>El arte pedido con <see cref="RequestBannerArt"/> aún se está pintando en otro hilo.</summary>
        public bool BannerArtPending => _bannerJob != null;

        /// <summary>
        /// Destruye las texturas y los sprites del banner (las mascotas y la viñeta): la vista los suelta al cerrarse el banner y
        /// se vuelven a generar en el siguiente (el de otro mundo). Si el arte pedido aún se estaba pintando en otro hilo, su
        /// resultado se descarta. Sin efecto si no hay ninguno.
        /// </summary>
        public void ReleaseBannerArt()
        {
            _bannerJob = null; // el hilo termina por su cuenta y nadie recoge lo que pintó
            ReleaseBannerSprite(ref _capiPaw);
            ReleaseBannerSprite(ref _rosa);
            ReleaseBannerSprite(ref _rosaSpray);
            ReleaseBannerSprite(ref _rosaRipple);
            ReleaseBannerSprite(ref _rosaFoam);
            ReleaseBannerScenes();
            _bannerSceneKey = null;
            MascotsMs = 0d;
            MascotsMainMs = 0d;
        }

        /// <summary>
        /// Pide todo el arte de un banner: la viñeta de <paramref name="widthDp"/> × <paramref name="heightDp"/> dp (como
        /// <see cref="BannerScene"/>) y las mascotas que falten. Con <paramref name="offMainThread"/>, sus píxeles se pintan en un
        /// hilo del grupo (<see cref="ThreadPool"/>: solo cuentas con <see cref="Color"/> y <see cref="Mathf"/>, sin API de
        /// Unity) y <see cref="TryTakeBannerArt"/> crea las texturas en el hilo principal cuando acaban; así abrir el banner no
        /// para el fotograma de la carga del nivel (08 §5.4; revisión 2 de A4: unos 230 ms de CPU en el Editor). Sin él, en el
        /// acto. Si ya está pedido o hecho con las mismas medidas, no hace nada.
        /// </summary>
        public void RequestBannerArt(float widthDp, float heightDp, Color water, Color? meetWater, int seed, bool offMainThread)
        {
            int w = Mathf.RoundToInt(widthDp * BannerScenePxPerDp);
            int h = Mathf.RoundToInt(heightDp * BannerScenePxPerDp);
            string key = BannerSceneKey(w, h, water, meetWater, seed);
            BannerArtJob? pending = _bannerJob;
            if (pending != null && pending.Key == key)
            {
                if (!offMainThread)
                {
                    WaitForBannerArt(int.MaxValue);
                    TryTakeBannerArt();
                }

                return;
            }

            if (BannerArtReadyFor(key))
            {
                _bannerJob = null;
                return;
            }

            var job = new BannerArtJob(key, w, h, water, meetWater, seed, _capiPaw == null, _rosa == null, _rosaSpray == null,
                _rosaRipple == null, _rosaFoam == null);
            _bannerJob = job;
            if (offMainThread)
            {
                ThreadPool.QueueUserWorkItem(RunBannerJob, job);
                return;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            job.Run();
            watch.Stop();
            MascotsMainMs += watch.Elapsed.TotalMilliseconds; // pintado en el hilo principal
            TryTakeBannerArt();
        }

        /// <summary>
        /// En el hilo principal: si el arte pedido con <see cref="RequestBannerArt"/> ya está pintado, crea sus texturas y sus
        /// sprites (<see cref="BannerSceneSprite"/> y las mascotas) y devuelve true; true también si ya estaba listo, y false
        /// mientras otro hilo lo sigue pintando o si no se pidió nada.
        /// </summary>
        public bool TryTakeBannerArt()
        {
            BannerArtJob? job = _bannerJob;
            if (job == null)
            {
                return _bannerSceneKey != null && BannerArtReadyFor(_bannerSceneKey);
            }

            if (!job.Done)
            {
                return false;
            }

            _bannerJob = null;
            if (job.Error != null)
            {
                throw new System.InvalidOperationException("No se pudo pintar el arte del banner de mundo.", job.Error);
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            ReleaseBannerScenes(); // solo la última
            _bannerScenes[job.Key] = SceneSprite(job.Scene!, job.Key);
            _bannerSceneKey = job.Key;
            _capiPaw = _capiPaw != null ? _capiPaw : PawSprite(job.Paw!);
            _rosa = _rosa != null ? _rosa : RosaSprite(job.Rosa!);
            _rosaSpray = _rosaSpray != null ? _rosaSpray : SpraySprite(job.Spray!);
            _rosaRipple = _rosaRipple != null ? _rosaRipple : RippleSprite(job.Ripple!);
            _rosaFoam = _rosaFoam != null ? _rosaFoam : FoamSprite(job.Foam!);
            watch.Stop();
            MascotsMs += job.RasterMs;
            MascotsMainMs += watch.Elapsed.TotalMilliseconds;
            return true;
        }

        /// <summary>
        /// Espera (como mucho <paramref name="timeoutMs"/> ms) a que otro hilo termine de pintar el arte pedido; true si ya no
        /// queda nada pendiente. Solo para los tests y para pedirlo en el acto: el juego nunca bloquea el hilo principal.
        /// </summary>
        public bool WaitForBannerArt(int timeoutMs)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            BannerArtJob? job = _bannerJob;
            while (job != null && !job.Done)
            {
                if (watch.ElapsedMilliseconds >= timeoutMs)
                {
                    return false;
                }

                Thread.Sleep(1);
            }

            return true;
        }

        /// <summary>
        /// Viñeta del banner de la última petición ya recogida (<see cref="RequestBannerArt"/> y <see cref="TryTakeBannerArt"/>),
        /// o null.
        /// </summary>
        public Sprite? BannerSceneSprite =>
            _bannerSceneKey != null && _bannerScenes.TryGetValue(_bannerSceneKey, out Sprite? s) ? s : null;

        private static void RunBannerJob(object? state)
        {
            ((BannerArtJob)state!).Run();
        }

        private static string BannerSceneKey(int w, int h, Color water, Color? meetWater, int seed)
        {
            return w.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + h.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "_" + HexOf(water) + "_" + (meetWater.HasValue ? HexOf(meetWater.Value) : "-") + "_" + seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private bool BannerArtReadyFor(string key)
        {
            return _bannerScenes.ContainsKey(key) && _capiPaw != null && _rosa != null && _rosaSpray != null && _rosaRipple != null
                && _rosaFoam != null;
        }

        private Sprite SceneSprite(PixelCanvas pc, string key)
        {
            return MakeSprite(pc, "banner_scene_" + key, new Vector2(0.5f, 0.5f), Vector4.zero, BannerScenePxPerDp);
        }

        private Sprite PawSprite(PixelCanvas pc)
        {
            return MakeSprite(pc, "capi_present_paw", CapiPawPivot, Vector4.zero, MascotPxPerDp);
        }

        private Sprite RosaSprite(PixelCanvas pc)
        {
            return MakeSprite(pc, "rosa_surface", new Vector2(0.5f, 0f), Vector4.zero, MascotPxPerDp);
        }

        private Sprite SpraySprite(PixelCanvas pc)
        {
            return MakeSprite(pc, "rosa_spray", new Vector2(0.5f, 0f), Vector4.zero, MascotPxPerDp);
        }

        private Sprite RippleSprite(PixelCanvas pc)
        {
            return MakeSprite(pc, "rosa_ripple", new Vector2(0.5f, 0.5f), Vector4.zero, MascotPxPerDp);
        }

        private Sprite FoamSprite(PixelCanvas pc)
        {
            return MakeSprite(pc, "rosa_foam", new Vector2(0.5f, 0.5f), Vector4.zero, MascotPxPerDp);
        }

        /// <summary>
        /// Viñeta de río del banner de mundo, de <paramref name="widthDp"/> × <paramref name="heightDp"/> dp con las esquinas
        /// redondeadas: cielo con el sol a la derecha, orilla de selva en el horizonte (<see cref="BannerSceneHorizon"/>), el agua
        /// del mundo (<paramref name="water"/>, <c>Palette.WaterBase</c>) con el cielo reflejado junto al horizonte, el camino de
        /// destellos del sol (<see cref="BannerSceneSunX"/>) y olas cortas y arqueadas, más pequeñas y apretadas hacia el
        /// horizonte; con <paramref name="meetWater"/>, el agua se parte en dos con una costura suave (el Encuentro de las Aguas
        /// del mundo 3: la del mundo a la izquierda y esta a la derecha) [P]. Abajo a la izquierda, el banco de arena mojada (lo
        /// alto en <see cref="BannerSceneBankTop"/>) con Capi sentado encima (<see cref="CapiPresentDp"/>, su eje en
        /// <see cref="BannerSceneCapiX"/>, sin la pata, <see cref="CapiPaw"/>). <paramref name="seed"/> (el mundo) cambia la orilla
        /// y las olas. Se guarda solo la última: pedir otra libera la anterior (la vista la sustituye en el acto).
        /// </summary>
        public Sprite BannerScene(float widthDp, float heightDp, Color water, Color? meetWater = null, int seed = 0)
        {
            int w = Mathf.RoundToInt(widthDp * BannerScenePxPerDp);
            int h = Mathf.RoundToInt(heightDp * BannerScenePxPerDp);
            string key = BannerSceneKey(w, h, water, meetWater, seed);
            if (_bannerScenes.TryGetValue(key, out Sprite? cached) && cached != null)
            {
                return cached;
            }

            ReleaseBannerScenes(); // solo la última
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Sprite sprite = SceneSprite(DrawBannerScene(w, h, water, meetWater, seed), key);
            watch.Stop();
            MascotsMs += watch.Elapsed.TotalMilliseconds;
            MascotsMainMs += watch.Elapsed.TotalMilliseconds;
            _bannerScenes[key] = sprite;
            _bannerSceneKey = key;
            return sprite;
        }

        private static string HexOf(Color color)
        {
            Color32 c = color;
            return c.r.ToString("X2", System.Globalization.CultureInfo.InvariantCulture) + c.g.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)
                + c.b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
        }

        // Una pieza pintada y convertida en sprite en el hilo principal (las propiedades, si no se pidió antes con
        // RequestBannerArt).
        private Sprite TimedMascot(System.Func<PixelCanvas> draw, System.Func<PixelCanvas, Sprite> sprite)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Sprite s = sprite(draw());
            watch.Stop();
            MascotsMs += watch.Elapsed.TotalMilliseconds;
            MascotsMainMs += watch.Elapsed.TotalMilliseconds;
            return s;
        }

        // Saca el sprite y su textura de lo generado (_owned) y los destruye.
        private void ReleaseBannerSprite(ref Sprite? sprite)
        {
            Sprite? s = sprite;
            sprite = null;
            if (s == null)
            {
                return;
            }

            Texture2D tex = s.texture;
            _owned.Remove(s);
            DestroyBannerObject(s);
            if (tex != null)
            {
                _owned.Remove(tex);
                DestroyBannerObject(tex);
            }
        }

        private void ReleaseBannerScenes()
        {
            if (_bannerScenes.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, Sprite> pair in _bannerScenes)
            {
                Sprite? s = pair.Value;
                ReleaseBannerSprite(ref s);
            }

            _bannerScenes.Clear();
        }

        private static void DestroyBannerObject(Object o)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(o);
            }
            else
            {
                Object.DestroyImmediate(o); // tests EditMode
            }
        }

        // ---- Capi ----

        // Capi (revisión de las capturas de A4, 69_banner_l11): una capibara, con la cabeza grande en bloque, la frente plana, el
        // hocico largo y romo y las orejas chicas; el contorno nítido de 2 px del arte, como el de Rosa (la distancia se corrige
        // con el gradiente, CapiDistance); sombreado suave con luz de borde arriba a la izquierda y sin grano; y la pata, en su
        // pieza, nace del hombro, dobla el codo y presenta con la palma hacia arriba.

        // Distancia con signo (en unidades del lado del cuadrado) a la silueta de Capi sentado de perfil, mirando a la derecha:
        // grupa grande en el suelo, torso de barril inclinado hacia delante, cuello ancho, cabeza grande en bloque (inclinada
        // 12° hacia el hocico) con el hocico largo y romo, oreja chica atrás y arriba, la pata delantera de apoyo y la trasera,
        // unidas con suavidad. 1-lipschitziana: elipses (por debajo de la distancia real), cajas y cápsulas unidas con Smin.
        private static float CapiSdf(float x, float y)
        {
            float haunch = SdEllipse(x, y, 0.285f, 0.235f, 0.225f, 0.205f);
            float torso = SdEllipse(x, y, 0.445f, 0.355f, 0.235f, 0.175f, Rot36);
            float body = Smin(haunch, torso, 0.06f);
            float neck = SdCapsule(x, y, 0.47f, 0.44f, 0.56f, 0.56f, 0.125f);
            float head = SdRoundBox(x, y, 0.655f, 0.635f, 0.215f, 0.135f, 0.06f, RotM12);
            float muzzle = SdRoundBox(x, y, 0.815f, 0.598f, 0.08f, 0.118f, 0.05f, RotM12);
            head = Smin(head, muzzle, 0.02f);
            float d = Smin(body, Smin(head, neck, 0.05f), 0.045f);
            float ear = SdEllipse(x, y, 0.535f, 0.795f, 0.033f, 0.028f, Rot15);
            d = Smin(d, ear, 0.01f);
            float leg = SdCapsule(x, y, 0.555f, 0.27f, 0.585f, 0.075f, 0.047f);
            float foot = SdEllipse(x, y, 0.618f, 0.05f, 0.068f, 0.032f);
            d = Smin(d, Smin(leg, foot, 0.02f), 0.022f);
            float hind = SdEllipse(x, y, 0.235f, 0.052f, 0.15f, 0.044f);
            return Smin(d, hind, 0.035f);
        }

        // Distancia (px) a la silueta de Capi en (x, y) (unidades del lado s), corregida cerca del borde con el gradiente
        // (f / |∇f|): la silueta une elipses y cajas con Smin y su gradiente baja de 1, así que sin corregir el contorno se
        // ensancha y se emborrona. En normal, la dirección hacia fuera (cero lejos del borde, más allá de band px).
        private static float CapiDistance(float x, float y, float s, float band, out Vector2 normal)
        {
            normal = Vector2.zero;
            float f = CapiSdf(x, y);
            if (Mathf.Abs(f) * s > band)
            {
                return f * s;
            }

            float e = 0.5f / s;
            float gx = CapiSdf(x + e, y) - CapiSdf(x - e, y);
            float gy = CapiSdf(x, y + e) - CapiSdf(x, y - e);
            float len = Mathf.Sqrt((gx * gx) + (gy * gy));
            if (len < 1e-7f)
            {
                return f * s;
            }

            normal = new Vector2(gx / len, gy / len);
            return f / (len / (2f * e)) * s;
        }

        // Capi sentado de perfil, mirando a la derecha, sin la pata que presenta (CapiPaw), en el cuadrado de s px cuya esquina
        // de abajo a la izquierda es el píxel (ox, oy) del lienzo (las patas, en su canto de abajo). Solo evalúa la silueta en
        // las celdas que pueden tocarla o tocar su contorno: CapiSdf es 1-lipschitziana, así que si en el centro de la celda la
        // distancia pasa del contorno más media diagonal, ningún píxel de la celda se pinta (el resultado es el mismo).
        private static void DrawCapi(PixelCanvas pc, int ox, int oy, int s)
        {
            float inv = 1f / s;
            float outline = MascotOutlineDp * MascotPxPerDp;
            float rimW = MascotRimDp * MascotPxPerDp;
            float band = outline + rimW + 2f;
            float reach = outline + 1f + (0.5f * 1.4143f * SilhouetteCell) + 1f;
            Color furDark = ColorMath.Darken(CapiFur, 0.24f);
            Color furTop = ColorMath.Lighten(CapiFur, 0.08f);
            Color furLight = ColorMath.Lighten(CapiFur, 0.22f);
            Color furShadow = ColorMath.Darken(CapiFur, 0.32f);
            Color muzzle = ColorMath.Darken(CapiFur, 0.12f);
            Color cheekLight = ColorMath.Lighten(CapiFur, 0.08f);
            for (int cy = 0; cy < s; cy += SilhouetteCell)
            {
                int cyEnd = Mathf.Min(s, cy + SilhouetteCell);
                for (int cx = 0; cx < s; cx += SilhouetteCell)
                {
                    int cxEnd = Mathf.Min(s, cx + SilhouetteCell);
                    if (CapiSdf((cx + cxEnd) * 0.5f * inv, (cy + cyEnd) * 0.5f * inv) * s > reach)
                    {
                        continue; // lejos de la silueta y de su contorno
                    }

                    for (int py = cy; py < cyEnd; py++)
                    {
                        for (int px = cx; px < cxEnd; px++)
                        {
                            float x = (px + 0.5f) * inv;
                            float y = (py + 0.5f) * inv;
                            float d = CapiDistance(x, y, s, band, out Vector2 normal); // px, negativa dentro
                            if (d > outline + 1f)
                            {
                                continue;
                            }

                            pc.Blend(ox + px, oy + py, CapiInk, PixelCanvas.Coverage(d - outline));
                            float cov = PixelCanvas.Coverage(d);
                            if (cov <= 0f)
                            {
                                continue;
                            }

                            pc.Blend(ox + px, oy + py, CapiFurAt(x, y, d, normal, s, rimW, furDark, furTop, furLight, furShadow, muzzle, cheekLight), cov);
                        }
                    }
                }
            }

            // La línea del muslo sobre la grupa y la que separa la pata de apoyo del pecho (trazos finos de tinta).
            float thin = 0.9f * MascotOutlineDp;
            StrokeArcUnits(pc, s, ox, oy, 0.285f, 0.20f, 0.135f, 30f, 118f, thin);
            StrokeArcUnits(pc, s, ox, oy, 0.545f, 0.235f, 0.05f, 195f, 285f, 0.85f * thin);

            // La nariz arriba en el frente del hocico (como la de la capibara), el ojo pequeño y alto con su brillo, y la boca.
            float k = s;
            pc.FillEllipseSoft(ox + (0.868f * k), oy + (0.662f * k), 0.026f * k, 0.017f * k, 1.2f, CapiNose);
            pc.FillEllipseSoft(ox + (0.876f * k), oy + (0.664f * k), 0.008f * k, 0.006f * k, 1f, new Color(0f, 0f, 0f, 0.6f));
            pc.FillCircle(ox + (0.665f * k), oy + (0.70f * k), 0.021f * k, Palette.Ink);
            pc.FillCircle(ox + (0.671f * k), oy + (0.708f * k), 0.0075f * k, new Color(1f, 1f, 1f, 0.95f));
            StrokeArcUnits(pc, s, ox, oy, 0.845f, 0.548f, 0.026f, 205f, 300f, 0.8f * thin);
        }

        // Pelaje de Capi en (x, y) (unidades del lado s), a d px del borde (negativa dentro) con la normal del borde: más oscuro
        // abajo y algo más claro arriba, luz de borde donde el canto mira arriba a la izquierda (MascotLight), sombra en los
        // cantos de abajo, la panza clara, el hocico algo más oscuro, una luz suave en la mejilla y el interior de la oreja.
        // Sin grano (D-003: vidrio, madera y luz de juego, no fieltro).
        private static Color CapiFurAt(float x, float y, float d, Vector2 normal, float s, float rimW, Color furDark, Color furTop,
            Color furLight, Color furShadow, Color muzzle, Color cheekLight)
        {
            Color fur = Color.Lerp(furDark, CapiFur, SmoothRange(0.04f, 0.62f, y));
            fur = Color.Lerp(fur, furTop, SmoothRange(0.55f, 0.85f, y));
            fur = RimShade(fur, -d, normal, rimW, furLight, furShadow);

            float belly = SdEllipse(x, y, 0.52f, 0.285f, 0.135f, 0.10f, Rot34) * s;
            if (belly < 6f)
            {
                fur = Color.Lerp(fur, CapiBelly, 0.85f * (1f - SmoothRange(0f, 1f, (belly + 4.5f) / 9f)));
            }

            float snout = SdRoundBox(x, y, 0.83f, 0.598f, 0.065f, 0.10f, 0.045f, RotM12) * s;
            if (snout < 6f)
            {
                fur = Color.Lerp(fur, muzzle, 0.55f * (1f - SmoothRange(0f, 1f, (snout + 4.5f) / 9f)));
            }

            float cheek = SdEllipse(x, y, 0.68f, 0.585f, 0.09f, 0.06f, RotM12) * s;
            if (cheek < 8f)
            {
                fur = Color.Lerp(fur, cheekLight, 0.35f * (1f - SmoothRange(0f, 1f, (cheek + 6f) / 12f)));
            }

            float earIn = SdEllipse(x, y, 0.538f, 0.79f, 0.016f, 0.013f, Rot15) * s;
            if (earIn < 1f)
            {
                fur = Color.Lerp(fur, CapiNose, 0.75f * PixelCanvas.Coverage(earIn));
            }

            fur.a = 1f;
            return fur;
        }

        // Luz de borde y sombra de un canto: a inside px dentro del borde (hasta rimW), según lo que mira la normal: hacia la luz
        // (MascotLight, arriba a la izquierda) se aclara y hacia abajo se oscurece.
        private static Color RimShade(Color c, float inside, Vector2 normal, float rimW, Color light, Color shadow)
        {
            if (normal.x == 0f && normal.y == 0f)
            {
                return c;
            }

            float edge = 1f - SmoothRange(0f, rimW, inside);
            if (edge <= 0f)
            {
                return c;
            }

            float lit = Mathf.Max(0f, (normal.x * MascotLight.x) + (normal.y * MascotLight.y));
            c = Color.Lerp(c, light, 0.65f * edge * SmoothRange(0.25f, 0.9f, lit));
            return Color.Lerp(c, shadow, 0.45f * edge * SmoothRange(0.2f, 0.9f, Mathf.Max(0f, -normal.y)));
        }

        private static float SmoothRange(float from, float to, float x)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x - from) / (to - from)));
        }

        // Pata que presenta, en dp desde abajo a la izquierda de su sprite: el hombro redondo (el pivote, CapiPawPivot), el brazo
        // que baja hacia delante hasta el codo, el antebrazo que sube hacia delante y la mano abierta con los dedos hacia
        // arriba.
        private static readonly Vector2 PawShoulder = new Vector2(7.5f, 15f);
        private static readonly Vector2 PawElbow = new Vector2(19f, 6.5f);
        private static readonly Vector2 PawWrist = new Vector2(34f, 9f);
        private static readonly Vector2 PawHand = new Vector2(38.6f, 10.4f);

        private static float CapiPawSdf(float x, float y)
        {
            float shoulder = SdEllipse(x, y, PawShoulder.x, PawShoulder.y, 7f, 6.3f, RotM30);
            float upper = SdTaper(x, y, PawShoulder, 5.6f, PawElbow, 4.4f);
            float arm = Smin(shoulder, upper, 1.5f);
            arm = Smin(arm, SdTaper(x, y, PawElbow, 4.4f, PawWrist, 3.6f), 1.2f);
            float d = Smin(arm, SdEllipse(x, y, PawHand.x, PawHand.y, 5.4f, 2.9f, Rot10), 1f);
            d = Smin(d, SdCircle(x, y, 43f, 12.6f, 1.55f), 0.6f);
            d = Smin(d, SdCircle(x, y, 44.4f, 11.2f, 1.5f), 0.6f);
            return Smin(d, SdCircle(x, y, 44.8f, 9.6f, 1.4f), 0.6f);
        }

        private static PixelCanvas DrawCapiPaw()
        {
            int w = Mathf.RoundToInt(CapiPawLengthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(CapiPawThickDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float outline = MascotOutlineDp * MascotPxPerDp;
            float rimW = MascotRimDp * MascotPxPerDp;
            float e = 0.5f / MascotPxPerDp;
            float near = (2f * outline) + 2f;

            // Poda por celdas, con el margen de las cápsulas de radio variable (algo más que 1-lipschitzianas).
            float reach = near + (1.25f * 0.5f * 1.4143f * SilhouetteCell) + 1f;
            Color furDark = ColorMath.Darken(CapiFur, 0.18f);
            Color furLight = ColorMath.Lighten(CapiFur, 0.22f);
            Color furShadow = ColorMath.Darken(CapiFur, 0.3f);
            Color palm = ColorMath.Lighten(CapiBelly, 0.08f);
            for (int cy = 0; cy < h; cy += SilhouetteCell)
            {
                int cyEnd = Mathf.Min(h, cy + SilhouetteCell);
                for (int cx = 0; cx < w; cx += SilhouetteCell)
                {
                    int cxEnd = Mathf.Min(w, cx + SilhouetteCell);
                    if (CapiPawSdf((cx + cxEnd) * 0.5f / MascotPxPerDp, (cy + cyEnd) * 0.5f / MascotPxPerDp) * MascotPxPerDp > reach)
                    {
                        continue;
                    }

                    for (int py = cy; py < cyEnd; py++)
                    {
                        for (int px = cx; px < cxEnd; px++)
                        {
                            float x = (px + 0.5f) / MascotPxPerDp;
                            float y = (py + 0.5f) / MascotPxPerDp;
                            float f = CapiPawSdf(x, y);
                            if (f * MascotPxPerDp > near)
                            {
                                continue; // f no pasa de la distancia real (Smin y las elipses la acotan por debajo): lejos de la pata
                            }

                            float gx = CapiPawSdf(x + e, y) - CapiPawSdf(x - e, y);
                            float gy = CapiPawSdf(x, y + e) - CapiPawSdf(x, y - e);
                            float len = Mathf.Max(1e-7f, Mathf.Sqrt((gx * gx) + (gy * gy)));
                            var normal = new Vector2(gx / len, gy / len);
                            float d = f / (len / (2f * e)) * MascotPxPerDp; // px, corregida como la del cuerpo
                            if (d > outline + 1f)
                            {
                                continue;
                            }

                            pc.Blend(px, py, CapiInk, PixelCanvas.Coverage(d - outline));
                            float cov = PixelCanvas.Coverage(d);
                            if (cov <= 0f)
                            {
                                continue;
                            }

                            Color fur = Color.Lerp(furDark, CapiFur, SmoothRange(2f, 20f, y));
                            fur = RimShade(fur, -d, normal, rimW, furLight, furShadow);

                            // La palma hacia arriba: la cara de arriba de la mano, clara.
                            float pad = SdEllipse(x, y, PawHand.x + 0.8f, PawHand.y + 1f, 4.4f, 1.6f, Rot10) * MascotPxPerDp;
                            if (pad < 4f)
                            {
                                fur = Color.Lerp(fur, palm, 0.9f * (1f - SmoothRange(0f, 1f, (pad + 3f) / 6f)));
                            }

                            fur.a = 1f;
                            pc.Blend(px, py, fur, cov);
                        }
                    }
                }
            }

            // El pliegue del codo y la separación de los dedos.
            float thin = 0.75f * MascotOutlineDp * MascotPxPerDp;
            float k = MascotPxPerDp;
            pc.StrokeArc(PawElbow.x * k, (PawElbow.y + 5f) * k, 2.4f * k, thin, 235f, 305f, MascotInk);
            pc.FillCapsule(41.6f * k, 11.6f * k, 42.6f * k, 11.1f * k, 0.35f * k, MascotInk);
            pc.FillCapsule(42.4f * k, 10.2f * k, 43.4f * k, 9.8f * k, 0.35f * k, MascotInk);
            return pc;
        }

        // Cápsula de radio variable (de ra en a a rb en b; el radio, interpolado en la proyección sobre el segmento).
        private static float SdTaper(float x, float y, Vector2 a, float ra, Vector2 b, float rb)
        {
            float vx = b.x - a.x;
            float vy = b.y - a.y;
            float px = x - a.x;
            float py = y - a.y;
            float t = Mathf.Clamp01(((px * vx) + (py * vy)) / Mathf.Max(1e-9f, (vx * vx) + (vy * vy)));
            float dx = px - (vx * t);
            float dy = py - (vy * t);
            return Mathf.Sqrt((dx * dx) + (dy * dy)) - Mathf.Lerp(ra, rb, t);
        }

        private static float SdCircle(float x, float y, float cx, float cy, float r)
        {
            return Mathf.Sqrt(((x - cx) * (x - cx)) + ((y - cy) * (y - cy))) - r;
        }

        // ---- Rosa ----

        // Lomo de Rosa: curva de Bézier cuadrática desde la cola sumergida (derecha, abajo) hasta la cabeza (izquierda), en
        // unidades del alto del sprite (x hasta W/H).
        private static Vector2 RosaSpine(float t)
        {
            var p0 = new Vector2(1.42f, -0.06f);
            var p1 = new Vector2(1.10f, 0.86f);
            var p2 = new Vector2(0.60f, 0.56f);
            float u = 1f - t;
            return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
        }

        private static Vector2[] BuildRosaSpineTable()
        {
            var table = new Vector2[RosaSpineSteps + 1];
            for (int i = 0; i <= RosaSpineSteps; i++)
            {
                table[i] = RosaSpine(i / (float)RosaSpineSteps);
            }

            return table;
        }

        private static float RosaRadius(float t)
        {
            return t < 0.55f ? Mathf.Lerp(0.15f, 0.175f, t / 0.55f) : Mathf.Lerp(0.175f, 0.14f, (t - 0.55f) / 0.45f);
        }

        // Distancia (unidades del alto) al cuerpo de Rosa y, en side, el lado respecto del lomo (+1 arriba, −1 abajo, según la
        // normal de la curva) en la parte del cuerpo; 0 en la cabeza. El punto más cercano del lomo se busca en la tabla: uno de
        // cada 4 puntos (t = i / 24) y después los 4 vecinos de cada lado (t ± i / 96).
        private static float RosaSdf(float x, float y, out float side)
        {
            Vector2[] spine = RosaSpineTable;
            float best = float.MaxValue;
            int bestI = 0;
            for (int i = 0; i <= RosaSpineSteps; i += 4)
            {
                Vector2 c = spine[i];
                float dd = ((x - c.x) * (x - c.x)) + ((y - c.y) * (y - c.y));
                if (dd < best)
                {
                    best = dd;
                    bestI = i;
                }
            }

            // Afinado del punto más cercano.
            int from = Mathf.Max(0, bestI - 4);
            int to = Mathf.Min(RosaSpineSteps, bestI + 4);
            int coarse = bestI;
            for (int i = from; i <= to; i++)
            {
                if (i == coarse)
                {
                    continue;
                }

                Vector2 c = spine[i];
                float dd = ((x - c.x) * (x - c.x)) + ((y - c.y) * (y - c.y));
                if (dd < best)
                {
                    best = dd;
                    bestI = i;
                }
            }

            Vector2 at = spine[bestI];
            Vector2 tangent = spine[Mathf.Min(RosaSpineSteps, bestI + 1)] - spine[Mathf.Max(0, bestI - 1)];
            var normal = new Vector2(-tangent.y, tangent.x); // hacia arriba al ir hacia la cabeza (a la izquierda)
            if (normal.y < 0f)
            {
                normal = -normal;
            }

            normal.Normalize();
            float r = RosaRadius(bestI / (float)RosaSpineSteps);
            float body = Mathf.Sqrt(best) - r;
            side = Mathf.Clamp(Vector2.Dot(new Vector2(x - at.x, y - at.y), normal) / r, -1f, 1f);

            // Aleta dorsal baja sobre el lomo y frente abombada; el hocico largo y fino.
            float fin = SdEllipse(x, y, RosaFinAt.x, RosaFinAt.y, 0.11f, 0.045f, Rot12);
            float d = Smin(body, fin, 0.04f);
            float melon = SdEllipse(x, y, 0.56f, 0.64f, 0.145f, 0.135f);
            float snout = SdCapsule(x, y, 0.46f, 0.57f, 0.12f, 0.47f, 0.042f);
            float head = Smin(melon, snout, 0.05f);
            if (head < d - 0.01f)
            {
                side = 0f;
            }

            return Smin(d, head, 0.045f);
        }

        private static PixelCanvas DrawRosa()
        {
            int w = Mathf.RoundToInt(RosaWidthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(RosaHeightDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float inv = 1f / h;
            float outline = MascotOutlineDp * MascotPxPerDp;

            // Poda por celdas: el lomo muestreado y el radio variable dejan la distancia algo por encima de la real y algo más
            // que 1-lipschitziana; el margen lo cubre de sobra (unos 4 px).
            float reach = outline + 1f + (1.25f * 0.5f * 1.4143f * SilhouetteCell) + 4f;
            Color light = ColorMath.Lighten(RosaPink, 0.12f);
            for (int cy = 0; cy < h; cy += SilhouetteCell)
            {
                int cyEnd = Mathf.Min(h, cy + SilhouetteCell);
                for (int cx = 0; cx < w; cx += SilhouetteCell)
                {
                    int cxEnd = Mathf.Min(w, cx + SilhouetteCell);
                    if (RosaSdf((cx + cxEnd) * 0.5f * inv, (cy + cyEnd) * 0.5f * inv, out _) * h > reach)
                    {
                        continue;
                    }

                    for (int py = cy; py < cyEnd; py++)
                    {
                        for (int px = cx; px < cxEnd; px++)
                        {
                            float x = (px + 0.5f) * inv;
                            float y = (py + 0.5f) * inv;
                            float d = RosaSdf(x, y, out float side) * h;
                            if (d > outline + 1f)
                            {
                                continue;
                            }

                            pc.Blend(px, py, MascotInk, PixelCanvas.Coverage(d - outline));
                            float cov = PixelCanvas.Coverage(d);
                            if (cov <= 0f)
                            {
                                continue;
                            }

                            pc.Blend(px, py, RosaColorAt(x, y, side, h, light), cov);
                        }
                    }
                }
            }

            // Boca a lo largo del hocico y el ojo pequeño, sin pestañas.
            pc.FillCapsule(0.16f * h, 0.475f * h, 0.47f * h, 0.53f * h, Mathf.Max(0.8f, 0.35f * MascotPxPerDp), new Color(MascotInk.r, MascotInk.g, MascotInk.b, 0.5f));
            pc.FillCircle(0.545f * h, 0.60f * h, 0.022f * h, Palette.Ink);
            pc.FillCircle(0.55f * h, 0.607f * h, 0.008f * h, new Color(1f, 1f, 1f, 0.9f));
            return pc;
        }

        // Rosa de base; dorso más oscuro arriba y vientre claro abajo (por el lado del lomo); en la cabeza, el hocico se aclara
        // por debajo y la frente recibe la luz de juego.
        private static Color RosaColorAt(float x, float y, float side, int h, Color light)
        {
            Color c = RosaPink;
            if (side > 0.25f)
            {
                c = Color.Lerp(c, RosaBack, Mathf.SmoothStep(0f, 1f, (side - 0.25f) / 0.55f));
            }
            else if (side < -0.2f)
            {
                c = Color.Lerp(c, RosaBelly, Mathf.SmoothStep(0f, 1f, (-side - 0.2f) / 0.5f));
            }

            if (side == 0f)
            {
                float under = Mathf.Clamp01((0.56f - y) / 0.12f);
                c = Color.Lerp(c, RosaBelly, 0.55f * under * Mathf.Clamp01((0.5f - x) / 0.2f));
            }

            float forehead = SdEllipse(x, y, 0.53f, 0.71f, 0.07f, 0.045f, RotM20) * h;
            if (forehead < 3f)
            {
                c = Color.Lerp(c, light, 0.7f * PixelCanvas.SoftCoverage(forehead, 4f * MascotPxPerDp));
            }

            c.a = 1f;
            return c;
        }

        // Soplo [P, pendiente del asesor cultural] (revisión 2 de A4, captura 69d): el boto exhala con un soplo corto y bajo, no
        // con un surtidor de ballena de dibujos. Una bruma breve, más ancha que alta, de 3 bocanadas suaves pegadas al
        // respiradero (el pivote, en el canto de abajo) y, justo encima, 3 gotas pequeñas con su contorno fino y su brillo.
        private static PixelCanvas DrawRosaSpray()
        {
            int s = Mathf.RoundToInt(RosaSprayDp * MascotPxPerDp);
            var pc = new PixelCanvas(s, s);
            float ox = s * 0.5f;
            float edge = 0.5f * MascotPxPerDp;
            float soft = 0.06f * s;

            // La bruma: un borde azulado muy tenue (se lee sobre el cielo durazno) y encima las 3 bocanadas, casi blancas.
            for (int pass = 0; pass < 2; pass++)
            {
                float grow = pass == 0 ? edge : 0f;
                Color c = pass == 0 ? new Color(SprayEdge.r, SprayEdge.g, SprayEdge.b, 0.22f) : new Color(0.94f, 0.97f, 1f, 0.72f);
                pc.FillEllipseSoft(ox - (0.17f * s), 0.12f * s, (0.15f * s) + grow, (0.085f * s) + grow, soft, c);
                pc.FillEllipseSoft(ox + (0.18f * s), 0.12f * s, (0.15f * s) + grow, (0.08f * s) + grow, soft, c);
                pc.FillEllipseSoft(ox, 0.16f * s, (0.21f * s) + grow, (0.115f * s) + grow, soft, c);
            }

            // 3 gotas pequeñas (≈ 2,4-3 dp) sobre la bruma, abiertas apenas.
            for (int i = 0; i < 3; i++)
            {
                float dx = i == 0 ? -0.14f : (i == 1 ? 0.02f : 0.16f);
                float dy = i == 0 ? 0.41f : (i == 1 ? 0.50f : 0.39f);
                float r = (i == 1 ? 0.05f : 0.042f) * s;
                float px = ox + (dx * s);
                float py = dy * s;
                pc.FillCircle(px, py, r + edge, SprayEdge);
                pc.FillCircle(px, py, r, SprayDrop);
                pc.FillCircle(px - (0.3f * r), py + (0.3f * r), 0.32f * r, new Color(1f, 1f, 1f, 0.9f));
            }

            return pc;
        }

        // Onda (revisión de las capturas de A4, 69c): dos arcos concéntricos aplanados, rotos en trazos, blancos al 60 % (grises
        // se leían como un arañazo sobre el agua oscura). La vista los agranda y los apaga.
        private static PixelCanvas DrawRosaRipple()
        {
            int w = Mathf.RoundToInt(RosaRippleWidthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(RosaRippleHeightDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float thick = 1.4f * MascotPxPerDp;
            for (int ring = 0; ring < 2; ring++)
            {
                float k = ring == 0 ? 1f : 0.6f;
                float rx = ((w * 0.5f) - 2f) * k;
                float ry = ((h * 0.5f) - 2f) * k;
                float phase = ring == 0 ? 0.35f : 1.4f;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float px = ((x + 0.5f) - cx) / rx;
                        float py = ((y + 0.5f) - cy) / ry;
                        float d = (Mathf.Sqrt((px * px) + (py * py)) - 1f) * ry;
                        float cov = PixelCanvas.Coverage(Mathf.Abs(d) - (thick * 0.5f));
                        if (cov <= 0f)
                        {
                            continue;
                        }

                        // Rota en 5 trazos con los extremos suaves; el canto de delante (abajo), entero, y el de detrás, algo menos.
                        float ang = Mathf.Atan2(py, px);
                        float dash = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Mathf.Cos((ang * 5f) + phase) + 0.25f) * 2.2f));
                        float front = Mathf.Lerp(1f, 0.75f, Mathf.Clamp01((((y + 0.5f) - cy) / ry * 0.5f) + 0.5f));
                        pc.Blend(x, y, new Color(1f, 1f, 1f, 0.6f * front), cov * dash);
                    }
                }
            }

            return pc;
        }

        // Espuma (revisión de las capturas de A4, 69c): una franja clara y ondulada, más ancha en el centro, con burbujas, que
        // tapa el corte recto del cuerpo de Rosa con el agua (el borde del recorte RosaClip).
        private static PixelCanvas DrawRosaFoam()
        {
            int w = Mathf.RoundToInt(RosaFoamWidthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(RosaFoamHeightDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float cy = h * 0.5f;
            float half = 2.2f * MascotPxPerDp;
            var foam = new Color(0.98f, 0.99f, 1f, 0.92f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float fx = (x + 0.5f) / w;
                    float taper = Mathf.Sin(Mathf.PI * fx); // lente: fina en las puntas
                    float top = cy + (half * taper * (0.85f + (0.3f * Mathf.Sin((fx * 26f) + 0.6f))));
                    float bottom = cy - (half * 0.75f * taper);
                    float py = y + 0.5f;
                    float d = Mathf.Max(py - top, bottom - py);
                    float cov = PixelCanvas.Coverage(d) * Mathf.Clamp01(taper * 3f);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    // Un poco más clara arriba (la cresta) que abajo.
                    float t = Mathf.Clamp01((py - bottom) / Mathf.Max(1f, top - bottom));
                    pc.Blend(x, y, new Color(foam.r, foam.g, foam.b, foam.a * Mathf.Lerp(0.75f, 1f, t)), cov);
                }
            }

            // Burbujas por encima y por debajo de la franja.
            float b = MascotPxPerDp;
            pc.FillCircle(w * 0.22f, cy + (2.6f * b), 0.9f * b, foam);
            pc.FillCircle(w * 0.47f, cy + (3.1f * b), 0.7f * b, foam);
            pc.FillCircle(w * 0.70f, cy + (2.8f * b), 1.0f * b, foam);
            pc.FillCircle(w * 0.36f, cy - (2.4f * b), 0.6f * b, new Color(foam.r, foam.g, foam.b, 0.7f));
            pc.FillCircle(w * 0.80f, cy - (2.2f * b), 0.7f * b, new Color(foam.r, foam.g, foam.b, 0.7f));
            return pc;
        }

        // ---- Viñeta ----

        // Filas de olas sobre todo el agua: más juntas hacia el horizonte (r = filas · √t, con t = 0 en el horizonte).
        private const float WaveRows = 11f;

        // Arco de cada ola (fracción de su fila): el centro de la cresta sube tanto sobre sus puntas.
        private const float WaveArch = 0.22f;

        // Revisión de las capturas de A4 (69b, 69f y 69g): el agua era un color plano cruzado por rayas largas y rectas, que a
        // tamaño de teléfono se leían como veta de madera o arena. Ahora: el cielo reflejado con fuerza en el 25 % de arriba del
        // agua, el camino de destellos del sol, olas cortas (2-5 % del ancho) y algo arqueadas, cada una con su cresta clara y su
        // seno oscuro, más pequeñas y apretadas hacia el horizonte; el banco, de arena mojada con el canto oscuro donde toca el
        // agua; y en el mundo 3, las dos aguas con una costura suave.
        private static PixelCanvas DrawBannerScene(int w, int h, Color water, Color? meetWater, int seed)
        {
            var pc = new PixelCanvas(w, h);
            float k = BannerScenePxPerDp;
            float radius = BannerSceneRadiusDp * k;
            float horizon = h * (1f - BannerSceneHorizon); // y desde abajo
            int sd = seed * 7919;
            bool meet = meetWater.HasValue;
            Color waterB = meet ? meetWater.GetValueOrDefault() : water;

            // Por columna (solo dependen de x): las dos siluetas de la orilla de selva, la lejana más clara (bruma).
            var far = new float[w];
            var near = new float[w];
            for (int x = 0; x < w; x++)
            {
                float fx = (x + 0.5f) / w;
                far[x] = horizon + ((6f + (10f * ArtNoise.Fbm2(fx * 9f, 0.5f, 31 + sd))) * k);
                near[x] = horizon + ((3f + (13f * ArtNoise.Fbm2(fx * 6f, 3.5f, 47 + sd)) - (5f * Mathf.Clamp01((fx * 2.2f) - 0.4f))) * k);
            }

            // Banco de arena de Capi: elipse con lo alto en BankTop, abajo a la izquierda.
            float bankTop = h * (1f - BannerSceneBankTop);
            float brx = 0.25f * w;
            float bry = 0.30f * h;
            float bx0 = BannerSceneCapiX * w;
            float by0 = bankTop - bry;
            float bankRight = bx0 + brx + (3f * k);
            float bankUp = bankTop + (3f * k);

            for (int y = 0; y < h; y++)
            {
                float yc = y + 0.5f;
                bool sky = y >= horizon;

                // Por fila: el cielo, o el agua (sus dos tonos con el reflejo, la costura, el camino del sol y la fila de olas).
                Color rowSky = default;
                float sunY2 = 0f;
                Color rowA = default;
                Color rowB = default;
                float t = 0f;
                float seamX = 0f;
                float seamW = 1f;
                float pathW = 1f;
                float pathGlow = 0f;
                bool waves = false;
                int ri = 0;
                float fr = 0f;
                float rowPx = 1f;
                float cellW = 1f;
                float crestTh = 1f;
                float troughTh = 1f;
                float crestA = 0f;
                float rowShift = 0f;
                if (sky)
                {
                    // Cielo: azul arriba, durazno y crema hacia el horizonte, con el resplandor del sol a la derecha.
                    float ts = Mathf.Clamp01((y - horizon) / (h - horizon));
                    rowSky = ts > 0.45f ? Color.Lerp(Palette.SkyMid, Palette.SkyTop, Mathf.SmoothStep(0f, 1f, (ts - 0.45f) / 0.55f))
                        : Color.Lerp(Palette.SkyHorizon, Palette.SkyMid, ts / 0.45f);
                    float sy = ((y - horizon) / h) - 0.10f;
                    sunY2 = sy * sy * 9f;
                }
                else
                {
                    t = Mathf.Clamp01((horizon - yc) / horizon);
                    rowA = WaterRow(water, t);
                    rowB = meet ? WaterRow(waterB, t) : rowA;
                    seamX = Mathf.Lerp(0.60f, 0.40f, t) + (0.014f * Mathf.Sin((t * 19f) + 1.1f)) + (0.03f * (ArtNoise.Value1(t * 6f, 83 + sd) - 0.5f));
                    seamW = Mathf.Lerp(0.004f, 0.022f, t);
                    pathW = 0.03f + (0.10f * t);
                    pathGlow = 0.32f * (1f - (0.75f * t));
                    float r = WaveRows * Mathf.Sqrt(t);
                    ri = (int)r;
                    fr = r - ri;
                    rowPx = 2f * Mathf.Sqrt(Mathf.Max(t, 1e-4f)) * horizon / WaveRows;
                    waves = rowPx >= 2.4f * k;
                    cellW = Mathf.Lerp(0.02f, 0.05f, t) * w;
                    crestTh = Mathf.Lerp(0.7f, 1.3f, t) * k;
                    troughTh = Mathf.Lerp(0.8f, 2.0f, t) * k;
                    crestA = Mathf.Lerp(0.55f, 0.85f, t);
                    rowShift = ArtNoise.Hash(ri, 3, 71 + sd) * 7.31f;
                }

                int lastCell = int.MinValue;
                bool cellOn = false;
                float cellPath = 0f;
                float cellLen = 1f;
                float cellC0 = 0.5f;
                float cellX0 = 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float xc = x + 0.5f;
                    float cov = PixelCanvas.Coverage(PixelCanvas.RoundedRectDistance(xc, yc, 0f, 0f, w, h, radius));
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float fx = xc / w;
                    Color c;
                    if (sky)
                    {
                        float sx = fx - BannerSceneSunX;
                        float glow = Mathf.Clamp01(1f - (Mathf.Sqrt((sx * sx * 4f) + sunY2) / 0.5f));
                        c = Color.Lerp(rowSky, Palette.Sun, 0.6f * glow * glow);

                        // Orilla de selva: copas irregulares sobre el horizonte; el canto de cada silueta con antialias (la cobertura
                        // del píxel bajo la curva): sin escalones a la densidad del teléfono.
                        float farCov = Mathf.Clamp01(far[x] - y);
                        if (farCov > 0f)
                        {
                            c = Color.Lerp(c, Color.Lerp(Palette.FarShore, Palette.FarPalm, Mathf.Clamp01((far[x] - y) / (8f * k))), farCov);
                        }

                        float nearCov = Mathf.Clamp01(near[x] - y);
                        if (nearCov > 0f)
                        {
                            c = Color.Lerp(c, Color.Lerp(Palette.JungleMid, Palette.JungleShadow, Mathf.Clamp01((near[x] - y) / (10f * k))), nearCov);
                        }
                    }
                    else
                    {
                        // Agua del mundo (o las dos, con la costura suave), con el camino de destellos del sol detrás de Rosa.
                        c = rowA;
                        if (meet)
                        {
                            c = Color.Lerp(rowA, rowB, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fx - seamX + seamW) / (2f * seamW))));
                        }

                        float path = 1f - (Mathf.Abs(fx - BannerSceneSunX) / pathW);
                        if (path > 0f)
                        {
                            c = Color.Lerp(c, Palette.Sun, pathGlow * path * path * (3f - (2f * path)));
                        }

                        if (waves)
                        {
                            float cxf = (xc / cellW) + rowShift;
                            int cell = Mathf.FloorToInt(cxf);
                            float fc = cxf - cell;
                            if (cell != lastCell)
                            {
                                // Cada celda: si lleva ola (más en el camino del sol), su largo, su altura en la fila y su centro.
                                lastCell = cell;
                                float cellFx = ((cell + 0.5f - rowShift) * cellW) / w;
                                cellPath = Mathf.Clamp01(1f - (Mathf.Abs(cellFx - BannerSceneSunX) / (pathW * 1.2f)));
                                cellOn = ArtNoise.Hash(cell, ri, 61 + sd) < 0.55f + (0.35f * cellPath);
                                cellLen = 0.30f + (0.14f * ArtNoise.Hash(cell, ri, 62 + sd));
                                cellC0 = 0.40f + (0.28f * ArtNoise.Hash(cell, ri, 63 + sd));
                                cellX0 = 0.5f + (0.16f * (ArtNoise.Hash(cell, ri, 64 + sd) - 0.5f));
                            }

                            if (cellOn)
                            {
                                c = Wave(c, fc, cellX0, cellLen, cellC0, fr, rowPx, crestTh, troughTh, crestA, cellPath);
                            }
                        }
                    }

                    // Banco de arena mojada, abajo a la izquierda.
                    if (xc < bankRight && yc < bankUp)
                    {
                        c = Bank(c, xc, yc, bx0, by0, brx, bry, bankTop, h, k);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            // Capi sentado en lo alto del banco, con su sombra de contacto en la arena.
            int s = Mathf.RoundToInt(CapiPresentDp * k);
            int ox = Mathf.RoundToInt((BannerSceneCapiX * w) - (s * 0.5f));
            int oy = Mathf.RoundToInt(bankTop - (BannerSceneCapiFootDp * k));
            pc.FillEllipseSoft(ox + (0.42f * s), oy + (0.012f * s), 0.30f * s, 2.2f * k, 2.4f * k, new Color(0.30f, 0.20f, 0.09f, 0.32f));
            DrawCapi(pc, ox, oy, s);
            return pc;
        }

        // Color del agua w en la fila t (0 en el horizonte, 1 delante): refleja con fuerza el cielo en el 25 % de arriba y se
        // oscurece hacia delante.
        private static Color WaterRow(Color w, float t)
        {
            Color top = Color.Lerp(w, Palette.SkyHorizon, 0.6f);
            Color c = Color.Lerp(top, w, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.25f)));
            return Color.Lerp(c, ColorMath.Darken(w, 0.18f), Mathf.Clamp01((t - 0.5f) * 1.6f));
        }

        // Una ola corta en su celda: un arco (convexo hacia arriba) con la cresta clara y, justo debajo, el seno oscuro; las
        // puntas se afinan. fc: posición en la celda; fr: en la fila (0 arriba); rowPx: alto de la fila; en el camino del sol, la
        // cresta es un destello.
        private static Color Wave(Color c, float fc, float x0, float len, float c0, float fr, float rowPx, float crestTh, float troughTh,
            float crestA, float sunPath)
        {
            float u = (fc - x0) / len;
            float au = Mathf.Abs(u);
            if (au >= 1f)
            {
                return c;
            }

            float taper = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((au - 0.45f) / 0.55f));
            float d = (fr - (c0 - (WaveArch * (1f - (u * u))))) * rowPx; // px; > 0 bajo la cresta
            float th = crestTh * (0.45f + (0.55f * taper));
            float tr = troughTh * (0.4f + (0.6f * taper));
            float trough = Mathf.Clamp01(0.5f - Mathf.Max(-d, d - tr)) * (1f - (0.6f * Mathf.Clamp01(d / tr)));
            if (trough > 0f)
            {
                c = ColorMath.Darken(c, 0.30f * trough * taper);
            }

            float crest = Mathf.Clamp01(0.5f - Mathf.Max(-th - d, d));
            if (crest > 0f)
            {
                Color light = Color.Lerp(Color.Lerp(c, Palette.WaterLine, 0.75f), Palette.Sun, sunPath);
                c = Color.Lerp(c, light, Mathf.Lerp(crestA, 1f, sunPath) * crest * (0.4f + (0.6f * taper)));
            }

            return c;
        }

        // El banco de arena mojada sobre el color c del píxel (xc, yc): arena seca y clara arriba, tostada abajo, mojada junto al
        // agua (más ancha bajando por el flanco) y el canto oscuro donde la toca; fuera, junto al flanco, la línea clara del agua
        // que lame la arena.
        private static Color Bank(Color c, float xc, float yc, float bx0, float by0, float brx, float bry, float bankTop, int h, float k)
        {
            float ex = (xc - bx0) / brx;
            float ey = (yc - by0) / bry;
            float q = Mathf.Sqrt((ex * ex) + (ey * ey));
            float gq = Mathf.Sqrt(((ex / brx) * (ex / brx)) + ((ey / bry) * (ey / bry))) / Mathf.Max(q, 1e-5f);
            float bd = (q - 1f) / Mathf.Max(gq, 1e-6f); // px, negativa dentro (corregida con el gradiente)
            if (bd > 2.6f * k)
            {
                return c;
            }

            float low = Mathf.Clamp01((bankTop - yc) / (0.09f * h)); // 0 en lo alto del banco, 1 bajando por el flanco
            if (bd > 0f)
            {
                float lap = Mathf.Clamp01(1f - (Mathf.Abs(bd - (1.3f * k)) / (0.9f * k)));
                return Color.Lerp(c, Palette.WaterLine, 0.55f * lap * low);
            }

            float depth = -bd;
            Color sand = Color.Lerp(SandDry, SandLow, Mathf.Clamp01((bankTop - yc) / (0.14f * h)));
            float wetW = Mathf.Lerp(1.2f, 3.4f, low) * k;
            sand = Color.Lerp(sand, SandWet, (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(depth / wetW))) * Mathf.Lerp(0.35f, 1f, low));
            sand = Color.Lerp(sand, SandEdge, 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(depth / k)));
            return Color.Lerp(c, sand, PixelCanvas.Coverage(bd));
        }

        // ---- Distancias con signo ----

        // Giro de deg grados (antihorario) como (coseno, seno), para precalcularlo.
        private static Vector2 Rot(float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        // Elipse de centro (cx, cy) y radios (rx, ry), sin giro: aproximación suficiente para el antialias (por debajo de la
        // distancia real: 1-lipschitziana).
        private static float SdEllipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float u = (x - cx) / rx;
            float v = (y - cy) / ry;
            return (Mathf.Sqrt((u * u) + (v * v)) - 1f) * Mathf.Min(rx, ry);
        }

        // Elipse girada rot = (coseno, seno) del giro (antihorario; Rot).
        private static float SdEllipse(float x, float y, float cx, float cy, float rx, float ry, Vector2 rot)
        {
            float dx = x - cx;
            float dy = y - cy;
            float u = ((dx * rot.x) + (dy * rot.y)) / rx;
            float v = ((-dx * rot.y) + (dy * rot.x)) / ry;
            return (Mathf.Sqrt((u * u) + (v * v)) - 1f) * Mathf.Min(rx, ry);
        }

        // Caja de semilados (hx, hy) con las esquinas de radio r, girada rot = (coseno, seno) (Rot).
        private static float SdRoundBox(float x, float y, float cx, float cy, float hx, float hy, float r, Vector2 rot)
        {
            float dx = x - cx;
            float dy = y - cy;
            float u = Mathf.Abs((dx * rot.x) + (dy * rot.y)) - (hx - r);
            float v = Mathf.Abs((-dx * rot.y) + (dy * rot.x)) - (hy - r);
            float ox = Mathf.Max(u, 0f);
            float oy = Mathf.Max(v, 0f);
            return Mathf.Sqrt((ox * ox) + (oy * oy)) + Mathf.Min(Mathf.Max(u, v), 0f) - r;
        }

        private static float SdCapsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float vx = bx - ax;
            float vy = by - ay;
            float px = x - ax;
            float py = y - ay;
            float t = Mathf.Clamp01(((px * vx) + (py * vy)) / Mathf.Max(1e-9f, (vx * vx) + (vy * vy)));
            float dx = px - (vx * t);
            float dy = py - (vy * t);
            return Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
        }

        // Unión suave (polinómica) de dos distancias con radio de mezcla k: conserva la constante de Lipschitz (su gradiente es
        // una media de los dos).
        private static float Smin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + (0.5f * (b - a) / k));
            return Mathf.Lerp(b, a, h) - (k * h * (1f - h));
        }

        // Arco de tinta fino en unidades del lado s del cuadrado con la esquina en el píxel (ox, oy) (centro, radio y ángulos en
        // grados, y grosor en dp).
        private static void StrokeArcUnits(PixelCanvas pc, int s, int ox, int oy, float cx, float cy, float r, float fromDeg, float toDeg, float thickDp)
        {
            pc.StrokeArc(ox + (cx * s), oy + (cy * s), r * s, thickDp * MascotPxPerDp, fromDeg, toDeg, MascotInk);
        }

        /// <summary>
        /// Píxeles de un banner pintados de una vez (<see cref="RequestBannerArt"/>), en un hilo del grupo o en el hilo principal:
        /// solo funciones estáticas de este parcial sobre <see cref="PixelCanvas"/> propios, con <see cref="Color"/>,
        /// <see cref="Vector2"/> y <see cref="Mathf"/> (ninguna API de Unity que exija el hilo principal) y tablas estáticas de
        /// solo lectura ya iniciadas. El hilo principal solo lee los resultados cuando <see cref="Done"/> es true.
        /// </summary>
        private sealed class BannerArtJob
        {
            private readonly int _w;
            private readonly int _h;
            private readonly Color _water;
            private readonly Color? _meet;
            private readonly int _seed;
            private readonly bool _paw;
            private readonly bool _rosa;
            private readonly bool _spray;
            private readonly bool _ripple;
            private readonly bool _foam;
            private volatile bool _done;

            public BannerArtJob(string key, int w, int h, Color water, Color? meet, int seed, bool paw, bool rosa, bool spray,
                bool ripple, bool foam)
            {
                Key = key;
                _w = w;
                _h = h;
                _water = water;
                _meet = meet;
                _seed = seed;
                _paw = paw;
                _rosa = rosa;
                _spray = spray;
                _ripple = ripple;
                _foam = foam;
            }

            public string Key { get; }

            public bool Done => _done;

            public PixelCanvas? Scene { get; private set; }

            public PixelCanvas? Paw { get; private set; }

            public PixelCanvas? Rosa { get; private set; }

            public PixelCanvas? Spray { get; private set; }

            public PixelCanvas? Ripple { get; private set; }

            public PixelCanvas? Foam { get; private set; }

            public double RasterMs { get; private set; }

            public System.Exception? Error { get; private set; }

            // Pinta lo pedido; la escritura volátil de _done publica los resultados para el hilo principal.
            public void Run()
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    Scene = DrawBannerScene(_w, _h, _water, _meet, _seed);
                    Paw = _paw ? DrawCapiPaw() : null;
                    Rosa = _rosa ? DrawRosa() : null;
                    Spray = _spray ? DrawRosaSpray() : null;
                    Ripple = _ripple ? DrawRosaRipple() : null;
                    Foam = _foam ? DrawRosaFoam() : null;
                }
                catch (System.Exception e)
                {
                    Error = e;
                }
                finally
                {
                    watch.Stop();
                    RasterMs = watch.Elapsed.TotalMilliseconds;
                    _done = true;
                }
            }
        }
    }
}
