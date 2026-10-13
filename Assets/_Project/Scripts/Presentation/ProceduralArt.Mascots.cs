#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural provisional [P] del banner de entrada a un mundo (07 §2.2, regla 1; 07 §6.1 y §6.2; decisión del dueño
    /// del 2026-10-11): Capi presentando con la pata (<c>capi_present</c>: el cuerpo sentado de perfil y la pata delantera en
    /// una pieza aparte, que la vista levanta), Rosa asomando del río (<c>rosa_surface</c>, y la vista la sumerge al cerrar:
    /// <c>rosa_dive</c>), el soplo de su exhalación y la viñeta de río del banner (cielo, orilla de selva y agua del tipo del
    /// mundo, con un banco de arena para Capi). Hojas de modelo de 07: Capi con pelaje #8B6A3E, panza #C8A872, nariz #4A3222 y
    /// contorno de tinta #2B2118 al 80 %, cuerpo redondo de perfil cuadrado, hocico ancho, ojos pequeños con un brillo y orejas
    /// chicas; Rosa rosa #E8A0B4 con dorso #C98A98 y vientre #F3D3DB, hocico largo, frente abombada, aleta dorsal baja y ojo
    /// pequeño, sin rasgos humanos. Sin ropa ni accesorios. Todo se genera la primera vez que se pide (la vista lo pide al abrir
    /// el banner: construir la pantalla o jugar un nivel sin banner no crea ninguna textura, <see cref="MascotsBuilt"/>) y se
    /// libera al cerrarlo (<see cref="ReleaseBannerArt"/>: cada banner sale una vez por mundo, así que no se queda en memoria
    /// toda la sesión); lo sustituye el arte del artista. Las mascotas y los lugares quedan pendientes de revisión del asesor
    /// cultural (07 §8.2).
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado del sprite del cuerpo de Capi (dp): el animal sentado ocupa ≈ 0,92 del alto.</summary>
        public const float CapiPresentDp = 100f;

        /// <summary>
        /// Hombro de la pata que presenta, en fracción del sprite del cuerpo (x desde la izquierda, y desde abajo): arriba del
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

        private const float MascotPxPerDp = UiPxPerDp;
        private const float BannerScenePxPerDp = 2f;

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

        private readonly Dictionary<string, Sprite> _bannerScenes = new Dictionary<string, Sprite>();
        private Sprite? _capiPresent;
        private Sprite? _capiPaw;
        private Sprite? _rosa;
        private Sprite? _rosaSpray;
        private Sprite? _rosaRipple;
        private Sprite? _rosaFoam;

        /// <summary>
        /// Capi sentado de perfil, mirando a la derecha, sin la pata que presenta (<see cref="CapiPaw"/>, con el hombro en
        /// <see cref="CapiShoulder"/>); <see cref="CapiPresentDp"/> de lado, con el pivote abajo en el centro.
        /// </summary>
        public Sprite CapiPresent => _capiPresent != null ? _capiPresent : (_capiPresent = TimedMascot(BuildCapiPresent));

        /// <summary>
        /// La pata delantera que presenta (<c>capi_present</c>): <see cref="CapiPawLengthDp"/> × <see cref="CapiPawThickDp"/>,
        /// del hombro (el pivote, <see cref="CapiPawPivot"/>) baja hacia delante hasta el codo y el antebrazo sube hasta la mano
        /// abierta con la palma hacia arriba, hacia la derecha.
        /// </summary>
        public Sprite CapiPaw => _capiPaw != null ? _capiPaw : (_capiPaw = TimedMascot(BuildCapiPaw));

        /// <summary>
        /// Rosa asomando de perfil, mirando a la izquierda: la cabeza con la frente abombada y el hocico largo y el lomo arqueado
        /// con la aleta baja; lo de abajo del sprite queda bajo el agua (la vista lo recorta en la línea del agua).
        /// </summary>
        public Sprite Rosa => _rosa != null ? _rosa : (_rosa = TimedMascot(BuildRosa));

        /// <summary>
        /// Soplo de la exhalación de Rosa (<see cref="RosaSprayDp"/>): un abanico de 5 gotas azul claro con un contorno fino, que
        /// nace del centro del canto de abajo (el pivote), sobre el respiradero (<see cref="RosaBlowhole"/>).
        /// </summary>
        public Sprite RosaSpray => _rosaSpray != null ? _rosaSpray : (_rosaSpray = TimedMascot(BuildRosaSpray));

        /// <summary>
        /// Onda en el agua donde Rosa asoma y se sumerge (<see cref="RosaRippleWidthDp"/> × <see cref="RosaRippleHeightDp"/>):
        /// dos arcos concéntricos aplanados, rotos y más claros que el agua; la vista los agranda y los apaga.
        /// </summary>
        public Sprite RosaRipple => _rosaRipple != null ? _rosaRipple : (_rosaRipple = TimedMascot(BuildRosaRipple));

        /// <summary>
        /// Espuma de la línea del agua (<see cref="RosaFoamWidthDp"/> × <see cref="RosaFoamHeightDp"/>): una franja clara y
        /// ondulada con burbujas que tapa el corte recto del cuerpo de Rosa con el agua (<see cref="RosaCutCenter"/>).
        /// </summary>
        public Sprite RosaFoam => _rosaFoam != null ? _rosaFoam : (_rosaFoam = TimedMascot(BuildRosaFoam));

        /// <summary>
        /// Alguna pieza del banner (Capi, su pata, Rosa, el soplo, la onda, la espuma o la viñeta) está generada: false al
        /// construir la pantalla y en cualquier nivel sin banner, y otra vez false cuando el banner se cierra.
        /// </summary>
        public bool MascotsBuilt => _capiPresent != null || _capiPaw != null || _rosa != null || _rosaSpray != null
            || _rosaRipple != null || _rosaFoam != null || _bannerScenes.Count > 0;

        /// <summary>Viñetas del banner en memoria (como mucho 1: se guarda solo la última).</summary>
        public int BannerSceneCount => _bannerScenes.Count;

        /// <summary>
        /// Tiempo de CPU (ms) de generar las piezas de las mascotas desde la última liberación (cada una, la primera vez que se
        /// pide). Ya no lo paga el arranque (guía 8: ≤ 300 ms, <see cref="GenerationMs"/>), sino la apertura del banner.
        /// </summary>
        public double MascotsMs { get; private set; }

        /// <summary>
        /// Destruye las texturas y los sprites del banner (las mascotas y la viñeta): la vista los suelta al cerrarse el banner y
        /// se vuelven a generar en el siguiente (el de otro mundo). Sin efecto si no hay ninguno.
        /// </summary>
        public void ReleaseBannerArt()
        {
            ReleaseBannerSprite(ref _capiPresent);
            ReleaseBannerSprite(ref _capiPaw);
            ReleaseBannerSprite(ref _rosa);
            ReleaseBannerSprite(ref _rosaSpray);
            ReleaseBannerSprite(ref _rosaRipple);
            ReleaseBannerSprite(ref _rosaFoam);
            ReleaseBannerScenes();
            MascotsMs = 0d;
        }

        private Sprite TimedMascot(System.Func<Sprite> build)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Sprite sprite = build();
            watch.Stop();
            MascotsMs += watch.Elapsed.TotalMilliseconds;
            return sprite;
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

        /// <summary>Ancho de la onda de Rosa (dp).</summary>
        public const float RosaRippleWidthDp = 72f;

        /// <summary>Alto de la onda de Rosa (dp).</summary>
        public const float RosaRippleHeightDp = 14f;

        /// <summary>
        /// Viñeta de río del banner de mundo, de <paramref name="widthDp"/> × <paramref name="heightDp"/> dp con las esquinas
        /// redondeadas: cielo, orilla de selva en el horizonte (<see cref="BannerSceneHorizon"/>), el agua del mundo
        /// (<paramref name="water"/>, <c>Palette.WaterBase</c>) con reflejos y, abajo a la izquierda, el banco de arena de Capi
        /// (su lo alto en <see cref="BannerSceneBankTop"/>). Las siluetas (las copas, la orilla y la hierba del banco) llevan
        /// antialias. Se guarda solo la última: pedir otro tamaño u otra agua libera la anterior (la vista la sustituye en el acto).
        /// </summary>
        public Sprite BannerScene(float widthDp, float heightDp, Color water)
        {
            int w = Mathf.RoundToInt(widthDp * BannerScenePxPerDp);
            int h = Mathf.RoundToInt(heightDp * BannerScenePxPerDp);
            Color32 c = water;
            string key = w.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + h.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "_" + c.r.ToString("X2", System.Globalization.CultureInfo.InvariantCulture) + c.g.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)
                + c.b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
            if (_bannerScenes.TryGetValue(key, out Sprite? cached) && cached != null)
            {
                return cached;
            }

            ReleaseBannerScenes(); // solo la última
            Sprite sprite = MakeSprite(DrawBannerScene(w, h, water), "banner_scene_" + key, new Vector2(0.5f, 0.5f), Vector4.zero, BannerScenePxPerDp);
            _bannerScenes[key] = sprite;
            return sprite;
        }

        // ---- Capi ----

        // Capi (revisión de las capturas de A4, 69_banner_l11): ya no es un animalito de fieltro con la cabeza pequeña y redonda
        // y la oreja alta (se leía como una marmota o un oso) y una pata recta que salía de la panza, sino una capibara: la
        // cabeza grande en bloque, con la frente plana, el hocico largo y romo y las orejas chicas; el contorno nítido de 2 px
        // del arte, como el de Rosa (la distancia se corrige con el gradiente, CapiDistance); sombreado suave con luz de borde
        // arriba a la izquierda y sin grano; y la pata, en su pieza, nace del hombro, dobla el codo y presenta con la palma
        // hacia arriba.

        // Distancia con signo (en unidades del lado del sprite) a la silueta de Capi sentado de perfil, mirando a la derecha:
        // grupa grande en el suelo, torso de barril inclinado hacia delante, cuello ancho, cabeza grande en bloque (inclinada
        // 12° hacia el hocico) con el hocico largo y romo, oreja chica atrás y arriba, la pata delantera de apoyo y la trasera,
        // unidas con suavidad.
        private static float CapiSdf(float x, float y)
        {
            float haunch = SdEllipse(x, y, 0.285f, 0.235f, 0.225f, 0.205f, 0f);
            float torso = SdEllipse(x, y, 0.445f, 0.355f, 0.235f, 0.175f, 36f);
            float body = Smin(haunch, torso, 0.06f);
            float neck = SdCapsule(x, y, 0.47f, 0.44f, 0.56f, 0.56f, 0.125f);
            float head = SdRoundBox(x, y, 0.655f, 0.635f, 0.215f, 0.135f, 0.06f, -12f);
            float muzzle = SdRoundBox(x, y, 0.815f, 0.598f, 0.08f, 0.118f, 0.05f, -12f);
            head = Smin(head, muzzle, 0.02f);
            float d = Smin(body, Smin(head, neck, 0.05f), 0.045f);
            float ear = SdEllipse(x, y, 0.535f, 0.795f, 0.033f, 0.028f, 15f);
            d = Smin(d, ear, 0.01f);
            float leg = SdCapsule(x, y, 0.555f, 0.27f, 0.585f, 0.075f, 0.047f);
            float foot = SdEllipse(x, y, 0.618f, 0.05f, 0.068f, 0.032f, 0f);
            d = Smin(d, Smin(leg, foot, 0.02f), 0.022f);
            float hind = SdEllipse(x, y, 0.235f, 0.052f, 0.15f, 0.044f, 0f);
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

        private Sprite BuildCapiPresent()
        {
            int s = Mathf.RoundToInt(CapiPresentDp * MascotPxPerDp);
            var pc = new PixelCanvas(s, s);
            float inv = 1f / s;
            float outline = MascotOutlineDp * MascotPxPerDp;
            float rimW = MascotRimDp * MascotPxPerDp;
            float band = outline + rimW + 2f;
            Color furDark = ColorMath.Darken(CapiFur, 0.24f);
            Color furTop = ColorMath.Lighten(CapiFur, 0.08f);
            Color furLight = ColorMath.Lighten(CapiFur, 0.22f);
            Color furShadow = ColorMath.Darken(CapiFur, 0.32f);
            Color muzzle = ColorMath.Darken(CapiFur, 0.12f);
            Color cheekLight = ColorMath.Lighten(CapiFur, 0.08f);
            for (int py = 0; py < s; py++)
            {
                for (int px = 0; px < s; px++)
                {
                    float x = (px + 0.5f) * inv;
                    float y = (py + 0.5f) * inv;
                    float d = CapiDistance(x, y, s, band, out Vector2 normal); // px, negativa dentro
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

                    pc.Blend(px, py, CapiFurAt(x, y, d, normal, s, rimW, furDark, furTop, furLight, furShadow, muzzle, cheekLight), cov);
                }
            }

            // La línea del muslo sobre la grupa y la que separa la pata de apoyo del pecho (trazos finos de tinta).
            float thin = 0.9f * MascotOutlineDp;
            StrokeArcUnits(pc, s, 0.285f, 0.20f, 0.135f, 30f, 118f, thin);
            StrokeArcUnits(pc, s, 0.545f, 0.235f, 0.05f, 195f, 285f, 0.85f * thin);

            // La nariz arriba en el frente del hocico (como la de la capibara), el ojo pequeño y alto con su brillo, y la boca.
            float k = s;
            pc.FillEllipseSoft(0.868f * k, 0.662f * k, 0.026f * k, 0.017f * k, 1.2f, CapiNose);
            pc.FillEllipseSoft(0.876f * k, 0.664f * k, 0.008f * k, 0.006f * k, 1f, new Color(0f, 0f, 0f, 0.6f));
            pc.FillCircle(0.665f * k, 0.70f * k, 0.021f * k, Palette.Ink);
            pc.FillCircle(0.671f * k, 0.708f * k, 0.0075f * k, new Color(1f, 1f, 1f, 0.95f));
            StrokeArcUnits(pc, s, 0.845f, 0.548f, 0.026f, 205f, 300f, 0.8f * thin);
            return MakeSprite(pc, "capi_present_body", new Vector2(0.5f, 0f), Vector4.zero, MascotPxPerDp);
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

            float belly = SdEllipse(x, y, 0.52f, 0.285f, 0.135f, 0.10f, 34f) * s;
            if (belly < 6f)
            {
                fur = Color.Lerp(fur, CapiBelly, 0.85f * (1f - SmoothRange(0f, 1f, (belly + 4.5f) / 9f)));
            }

            float snout = SdRoundBox(x, y, 0.83f, 0.598f, 0.065f, 0.10f, 0.045f, -12f) * s;
            if (snout < 6f)
            {
                fur = Color.Lerp(fur, muzzle, 0.55f * (1f - SmoothRange(0f, 1f, (snout + 4.5f) / 9f)));
            }

            float cheek = SdEllipse(x, y, 0.68f, 0.585f, 0.09f, 0.06f, -12f) * s;
            if (cheek < 8f)
            {
                fur = Color.Lerp(fur, cheekLight, 0.35f * (1f - SmoothRange(0f, 1f, (cheek + 6f) / 12f)));
            }

            float earIn = SdEllipse(x, y, 0.538f, 0.79f, 0.016f, 0.013f, 15f) * s;
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
            float shoulder = SdEllipse(x, y, PawShoulder.x, PawShoulder.y, 7f, 6.3f, -30f);
            float upper = SdTaper(x, y, PawShoulder, 5.6f, PawElbow, 4.4f);
            float arm = Smin(shoulder, upper, 1.5f);
            arm = Smin(arm, SdTaper(x, y, PawElbow, 4.4f, PawWrist, 3.6f), 1.2f);
            float d = Smin(arm, SdEllipse(x, y, PawHand.x, PawHand.y, 5.4f, 2.9f, 10f), 1f);
            d = Smin(d, SdCircle(x, y, 43f, 12.6f, 1.55f), 0.6f);
            d = Smin(d, SdCircle(x, y, 44.4f, 11.2f, 1.5f), 0.6f);
            return Smin(d, SdCircle(x, y, 44.8f, 9.6f, 1.4f), 0.6f);
        }

        private Sprite BuildCapiPaw()
        {
            int w = Mathf.RoundToInt(CapiPawLengthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(CapiPawThickDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float outline = MascotOutlineDp * MascotPxPerDp;
            float rimW = MascotRimDp * MascotPxPerDp;
            float e = 0.5f / MascotPxPerDp;
            Color furDark = ColorMath.Darken(CapiFur, 0.18f);
            Color furLight = ColorMath.Lighten(CapiFur, 0.22f);
            Color furShadow = ColorMath.Darken(CapiFur, 0.3f);
            Color palm = ColorMath.Lighten(CapiBelly, 0.08f);
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    float x = (px + 0.5f) / MascotPxPerDp;
                    float y = (py + 0.5f) / MascotPxPerDp;
                    float f = CapiPawSdf(x, y);
                    if (f * MascotPxPerDp > (2f * outline) + 2f)
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
                    float pad = SdEllipse(x, y, PawHand.x + 0.8f, PawHand.y + 1f, 4.4f, 1.6f, 10f) * MascotPxPerDp;
                    if (pad < 4f)
                    {
                        fur = Color.Lerp(fur, palm, 0.9f * (1f - SmoothRange(0f, 1f, (pad + 3f) / 6f)));
                    }

                    fur.a = 1f;
                    pc.Blend(px, py, fur, cov);
                }
            }

            // El pliegue del codo y la separación de los dedos.
            float thin = 0.75f * MascotOutlineDp * MascotPxPerDp;
            float k = MascotPxPerDp;
            pc.StrokeArc(PawElbow.x * k, (PawElbow.y + 5f) * k, 2.4f * k, thin, 235f, 305f, MascotInk);
            pc.FillCapsule(41.6f * k, 11.6f * k, 42.6f * k, 11.1f * k, 0.35f * k, MascotInk);
            pc.FillCapsule(42.4f * k, 10.2f * k, 43.4f * k, 9.8f * k, 0.35f * k, MascotInk);
            return MakeSprite(pc, "capi_present_paw", CapiPawPivot, Vector4.zero, MascotPxPerDp);
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

        private static float RosaRadius(float t)
        {
            return t < 0.55f ? Mathf.Lerp(0.15f, 0.175f, t / 0.55f) : Mathf.Lerp(0.175f, 0.14f, (t - 0.55f) / 0.45f);
        }

        // Distancia (unidades del alto) al cuerpo de Rosa y, en side, el lado respecto del lomo (+1 arriba, −1 abajo, según la
        // normal de la curva) en la parte del cuerpo; 0 en la cabeza.
        private static float RosaSdf(float x, float y, out float side)
        {
            float best = float.MaxValue;
            float bestT = 0f;
            for (int i = 0; i <= 24; i++)
            {
                float t = i / 24f;
                Vector2 c = RosaSpine(t);
                float dd = ((x - c.x) * (x - c.x)) + ((y - c.y) * (y - c.y));
                if (dd < best)
                {
                    best = dd;
                    bestT = t;
                }
            }

            // Afinado del punto más cercano.
            for (int i = -4; i <= 4; i++)
            {
                float t = Mathf.Clamp01(bestT + (i / 96f));
                Vector2 c = RosaSpine(t);
                float dd = ((x - c.x) * (x - c.x)) + ((y - c.y) * (y - c.y));
                if (dd < best)
                {
                    best = dd;
                    bestT = t;
                }
            }

            Vector2 at = RosaSpine(bestT);
            Vector2 tangent = RosaSpine(Mathf.Min(1f, bestT + 0.01f)) - RosaSpine(Mathf.Max(0f, bestT - 0.01f));
            var normal = new Vector2(-tangent.y, tangent.x); // hacia arriba al ir hacia la cabeza (a la izquierda)
            if (normal.y < 0f)
            {
                normal = -normal;
            }

            normal.Normalize();
            float r = RosaRadius(bestT);
            float body = Mathf.Sqrt(best) - r;
            side = Mathf.Clamp(Vector2.Dot(new Vector2(x - at.x, y - at.y), normal) / r, -1f, 1f);

            // Aleta dorsal baja sobre el lomo y frente abombada; el hocico largo y fino.
            Vector2 finAt = RosaSpine(0.42f) + (new Vector2(0.03f, 1f).normalized * (RosaRadius(0.42f) * 0.92f));
            float fin = SdEllipse(x, y, finAt.x, finAt.y, 0.11f, 0.045f, 12f);
            float d = Smin(body, fin, 0.04f);
            float melon = SdEllipse(x, y, 0.56f, 0.64f, 0.145f, 0.135f, 0f);
            float snout = SdCapsule(x, y, 0.46f, 0.57f, 0.12f, 0.47f, 0.042f);
            float head = Smin(melon, snout, 0.05f);
            if (head < d - 0.01f)
            {
                side = 0f;
            }

            return Smin(d, head, 0.045f);
        }

        private Sprite BuildRosa()
        {
            int w = Mathf.RoundToInt(RosaWidthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(RosaHeightDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float inv = 1f / h;
            float outline = MascotOutlineDp * MascotPxPerDp;
            Color light = ColorMath.Lighten(RosaPink, 0.12f);
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
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

                    // Rosa de base; dorso más oscuro arriba y vientre claro abajo (por el lado del lomo); en la cabeza, el
                    // hocico se aclara por debajo y la frente recibe la luz de juego.
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

                    float forehead = SdEllipse(x, y, 0.53f, 0.71f, 0.07f, 0.045f, -20f) * h;
                    if (forehead < 3f)
                    {
                        c = Color.Lerp(c, light, 0.7f * PixelCanvas.SoftCoverage(forehead, 4f * MascotPxPerDp));
                    }

                    c.a = 1f;
                    pc.Blend(px, py, c, cov);
                }
            }

            // Boca a lo largo del hocico y el ojo pequeño, sin pestañas.
            pc.FillCapsule(0.16f * h, 0.475f * h, 0.47f * h, 0.53f * h, Mathf.Max(0.8f, 0.35f * MascotPxPerDp), new Color(MascotInk.r, MascotInk.g, MascotInk.b, 0.5f));
            pc.FillCircle(0.545f * h, 0.60f * h, 0.022f * h, Palette.Ink);
            pc.FillCircle(0.55f * h, 0.607f * h, 0.008f * h, new Color(1f, 1f, 1f, 0.9f));
            return MakeSprite(pc, "rosa_surface", new Vector2(0.5f, 0f), Vector4.zero, MascotPxPerDp);
        }

        // Soplo (revisión de las capturas de A4, 69d): ya no es un penacho blanco con tallo, que sobre el cielo durazno se leía
        // como un diente de león detrás de la cabeza, sino un abanico de 5 gotas azul claro con contorno fino que nace del
        // pivote, sobre el respiradero, y la vista lo abre desde ahí.
        private Sprite BuildRosaSpray()
        {
            int s = Mathf.RoundToInt(RosaSprayDp * MascotPxPerDp);
            var pc = new PixelCanvas(s, s);
            float ox = s * 0.5f;
            float oy = s * 0.03f;
            float edge = 0.5f * MascotPxPerDp;

            // Vaho leve sobre el respiradero.
            pc.FillEllipseSoft(ox, oy + (s * 0.16f), s * 0.17f, s * 0.11f, s * 0.08f, new Color(SprayDrop.r, SprayDrop.g, SprayDrop.b, 0.35f));

            // Abanico: ángulo desde la vertical (grados), distancia de la punta y radio de la gota (fracciones del lado).
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < 5; i++)
                {
                    float deg = -46f + (23f * i);
                    float reach = i == 2 ? 0.84f : (i == 1 || i == 3 ? 0.74f : 0.58f);
                    float r = i == 2 ? 0.085f : (i == 1 || i == 3 ? 0.075f : 0.062f);
                    float a = deg * Mathf.Deg2Rad;
                    float dx = Mathf.Sin(a);
                    float dy = Mathf.Cos(a);
                    float tipX = ox + (dx * (reach - r) * s);
                    float tipY = oy + (dy * (reach - r) * s);
                    float tailX = ox + (dx * (reach - (3.2f * r)) * s);
                    float tailY = oy + (dy * (reach - (3.2f * r)) * s);
                    float grow = pass == 0 ? edge : 0f;
                    Color c = pass == 0 ? SprayEdge : SprayDrop;
                    pc.FillCapsule(tailX, tailY, tipX, tipY, (0.38f * r * s) + grow, c); // la estela de la gota
                    pc.FillCircle(tipX, tipY, (r * s) + grow, c);
                }
            }

            // Brillo en cada gota grande y dos gotitas sueltas entre las del abanico.
            for (int i = 1; i <= 3; i++)
            {
                float a = (-46f + (23f * i)) * Mathf.Deg2Rad;
                float reach = i == 2 ? 0.84f : 0.74f;
                float r = i == 2 ? 0.085f : 0.075f;
                pc.FillCircle(ox + (Mathf.Sin(a) * (reach - r) * s) - (0.3f * r * s), oy + (Mathf.Cos(a) * (reach - r) * s) + (0.3f * r * s), 0.3f * r * s, new Color(1f, 1f, 1f, 0.9f));
            }

            for (int f = -1; f <= 1; f += 2)
            {
                float a = f * 34f * Mathf.Deg2Rad;
                float px = ox + (Mathf.Sin(a) * 0.44f * s);
                float py = oy + (Mathf.Cos(a) * 0.44f * s);
                pc.FillCircle(px, py, (0.032f * s) + edge, SprayEdge);
                pc.FillCircle(px, py, 0.032f * s, SprayDrop);
            }

            return MakeSprite(pc, "rosa_spray", new Vector2(0.5f, 0f), Vector4.zero, MascotPxPerDp);
        }

        // Onda (revisión de las capturas de A4, 69c): dos arcos concéntricos aplanados, rotos en trazos y más claros que el agua,
        // en lugar del aro gris continuo que se leía como un plato flotando. La vista los agranda y los apaga.
        private Sprite BuildRosaRipple()
        {
            int w = Mathf.RoundToInt(RosaRippleWidthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(RosaRippleHeightDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float thick = 1.2f * MascotPxPerDp;
            for (int ring = 0; ring < 2; ring++)
            {
                float k = ring == 0 ? 1f : 0.6f;
                float rx = ((w * 0.5f) - 2f) * k;
                float ry = ((h * 0.5f) - 2f) * k;
                float alpha = ring == 0 ? 0.8f : 0.65f;
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

                        // Rota en 5 trazos con los extremos suaves; el canto de delante (abajo) con más luz que el de detrás.
                        float ang = Mathf.Atan2(py, px);
                        float dash = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Mathf.Cos((ang * 5f) + phase) + 0.25f) * 2.2f));
                        float front = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((((y + 0.5f) - cy) / ry * 0.5f) + 0.5f));
                        pc.Blend(x, y, new Color(0.97f, 0.99f, 1f, alpha * front), cov * dash);
                    }
                }
            }

            return MakeSprite(pc, "rosa_ripple", new Vector2(0.5f, 0.5f), Vector4.zero, MascotPxPerDp);
        }

        // Espuma (revisión de las capturas de A4, 69c): una franja clara y ondulada, más ancha en el centro, con burbujas, que
        // tapa el corte recto del cuerpo de Rosa con el agua (el borde del recorte RosaClip).
        private Sprite BuildRosaFoam()
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
            return MakeSprite(pc, "rosa_foam", new Vector2(0.5f, 0.5f), Vector4.zero, MascotPxPerDp);
        }

        // ---- Viñeta ----

        private static PixelCanvas DrawBannerScene(int w, int h, Color water)
        {
            var pc = new PixelCanvas(w, h);
            float k = BannerScenePxPerDp;
            float radius = BannerSceneRadiusDp * k;
            float horizon = h * (1f - BannerSceneHorizon); // y desde abajo
            Color waterTop = Color.Lerp(water, Palette.SkyHorizon, 0.35f);
            Color waterLow = ColorMath.Darken(water, 0.18f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = PixelCanvas.RoundedRectDistance(x + 0.5f, y + 0.5f, 0f, 0f, w, h, radius);
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    float fx = (x + 0.5f) / w;
                    Color c;
                    if (y >= horizon)
                    {
                        // Cielo: azul arriba, durazno y crema hacia el horizonte, con un resplandor de sol a la derecha.
                        float t = Mathf.Clamp01((y - horizon) / (h - horizon));
                        c = t > 0.45f ? Color.Lerp(Palette.SkyMid, Palette.SkyTop, Mathf.SmoothStep(0f, 1f, (t - 0.45f) / 0.55f))
                            : Color.Lerp(Palette.SkyHorizon, Palette.SkyMid, t / 0.45f);
                        float sx = fx - 0.78f;
                        float sy = (y - horizon) / h - 0.10f;
                        float glow = Mathf.Clamp01(1f - (Mathf.Sqrt((sx * sx * 4f) + (sy * sy * 9f)) / 0.5f));
                        c = Color.Lerp(c, Palette.Sun, 0.6f * glow * glow);

                        // Orilla de selva: copas irregulares sobre el horizonte, la lejana más clara (bruma). El canto de cada
                        // silueta con antialias (la cobertura del píxel bajo la curva; revisión de las capturas de A4, 69): sin
                        // escalones a la densidad del teléfono.
                        float far = horizon + ((6f + (10f * ArtNoise.Fbm2(fx * 9f, 0.5f, 31))) * k);
                        float near = horizon + ((3f + (13f * ArtNoise.Fbm2(fx * 6f, 3.5f, 47)) - (5f * Mathf.Clamp01(fx * 2.2f - 0.4f))) * k);
                        float farCov = Mathf.Clamp01(far - y);
                        if (farCov > 0f)
                        {
                            c = Color.Lerp(c, Color.Lerp(Palette.FarShore, Palette.FarPalm, Mathf.Clamp01((far - y) / (8f * k))), farCov);
                        }

                        float nearCov = Mathf.Clamp01(near - y);
                        if (nearCov > 0f)
                        {
                            c = Color.Lerp(c, Color.Lerp(Palette.JungleMid, Palette.JungleShadow, Mathf.Clamp01((near - y) / (10f * k))), nearCov);
                        }
                    }
                    else
                    {
                        // Agua del mundo: refleja el cielo junto al horizonte y se oscurece hacia delante, con rizos de luz.
                        float t = Mathf.Clamp01((horizon - y) / horizon);
                        c = Color.Lerp(waterTop, water, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 2.2f)));
                        c = Color.Lerp(c, waterLow, Mathf.Clamp01((t - 0.5f) * 1.6f));
                        // Rizos de luz y senos de las olas con los cantos suaves (sin trazos de bordes duros).
                        float rowNoise = ArtNoise.Value2(fx * 18f * (1f + t), y / (2.2f * k), 53);
                        float stripe = Mathf.Abs(Mathf.Repeat(y / (5.5f * k * (0.6f + t)), 1f) - 0.5f);
                        float crest = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((rowNoise - 0.58f) / 0.08f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.15f - stripe) / 0.06f));
                        float trough = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.32f - rowNoise) / 0.08f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((stripe - 0.37f) / 0.06f));
                        c = Color.Lerp(c, Palette.WaterLine, 0.45f * (1f - t * 0.6f) * crest);
                        c = Color.Lerp(c, waterLow, 0.55f * trough); // senos de las olas: el agua clara también se lee como agua
                    }

                    // Banco de arena de Capi, abajo a la izquierda, con hierba en el canto.
                    float bankTop = h * (1f - BannerSceneBankTop);
                    float bx = (x + 0.5f) - (0.17f * w);
                    float by = (y + 0.5f) - (bankTop - (0.30f * h));
                    float bank = (Mathf.Sqrt((bx * bx / (0.25f * w * 0.25f * w)) + (by * by / (0.30f * h * 0.30f * h))) - 1f) * 0.30f * h;
                    if (bank < 1f)
                    {
                        float bc = PixelCanvas.Coverage(bank);
                        Color sand = Color.Lerp(Palette.DockSunlit, Palette.DockBase, Mathf.Clamp01(-bank / (10f * k)));

                        // Hierba en el canto: su borde de abajo, con antialias (la cobertura bajo la línea de las briznas).
                        float blade = ArtNoise.Value1(fx * 70f, 17); // briznas de unos 4 dp: el canto sube y baja sin escalones
                        float grass = Mathf.Clamp01(bank + ((1.5f + (3f * blade)) * k) + 0.5f);
                        if (grass > 0f)
                        {
                            sand = Color.Lerp(sand, Color.Lerp(Palette.LeafMid, Palette.LeafLight, blade), grass);
                        }

                        c = Color.Lerp(c, sand, bc);
                    }

                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            return pc;
        }

        // ---- Distancias con signo ----

        // Elipse de centro (cx, cy), radios (rx, ry) y giro deg (antihorario): aproximación suficiente para el antialias.
        private static float SdEllipse(float x, float y, float cx, float cy, float rx, float ry, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);
            float dx = x - cx;
            float dy = y - cy;
            float u = (dx * cos) + (dy * sin);
            float v = (-dx * sin) + (dy * cos);
            float q = Mathf.Sqrt(((u / rx) * (u / rx)) + ((v / ry) * (v / ry)));
            return (q - 1f) * Mathf.Min(rx, ry);
        }

        private static float SdRoundBox(float x, float y, float cx, float cy, float hx, float hy, float r, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);
            float dx = x - cx;
            float dy = y - cy;
            float u = Mathf.Abs((dx * cos) + (dy * sin)) - (hx - r);
            float v = Mathf.Abs((-dx * sin) + (dy * cos)) - (hy - r);
            float ox = Mathf.Max(u, 0f);
            float oy = Mathf.Max(v, 0f);
            return Mathf.Sqrt((ox * ox) + (oy * oy)) + Mathf.Min(Mathf.Max(u, v), 0f) - r;
        }

        private static float SdCapsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            return SdCapsulePx(x, y, ax, ay, bx, by, r);
        }

        private static float SdCapsulePx(float x, float y, float ax, float ay, float bx, float by, float r)
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

        // Unión suave (polinómica) de dos distancias con radio de mezcla k.
        private static float Smin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + (0.5f * (b - a) / k));
            return Mathf.Lerp(b, a, h) - (k * h * (1f - h));
        }

        // Arco de tinta fino en unidades del lado s del sprite (centro, radio y ángulos en grados, y grosor en dp).
        private static void StrokeArcUnits(PixelCanvas pc, int s, float cx, float cy, float r, float fromDeg, float toDeg, float thickDp)
        {
            pc.StrokeArc(cx * s, cy * s, r * s, thickDp * MascotPxPerDp, fromDeg, toDeg, MascotInk);
        }
    }
}
