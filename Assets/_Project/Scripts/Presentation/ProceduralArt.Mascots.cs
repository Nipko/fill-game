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
    /// pequeño, sin rasgos humanos. Sin ropa ni accesorios. Todo se genera la primera vez que se pide (un nivel sin banner no
    /// crea ninguna textura) y lo sustituye el arte del artista; las mascotas y los lugares quedan pendientes de revisión del
    /// asesor cultural (07 §8.2).
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>Lado del sprite del cuerpo de Capi (dp): el animal sentado ocupa ≈ 0,92 del alto.</summary>
        public const float CapiPresentDp = 100f;

        /// <summary>Hombro de la pata que presenta, en fracción del sprite del cuerpo (x desde la izquierda, y desde abajo).</summary>
        public static readonly Vector2 CapiShoulder = new Vector2(0.55f, 0.32f);

        /// <summary>Largo de la pata que presenta (dp), del hombro a la punta de la mano.</summary>
        public const float CapiPawLengthDp = 44f;

        /// <summary>Alto del sprite de la pata (dp).</summary>
        public const float CapiPawThickDp = 13f;

        /// <summary>Ancho del sprite de Rosa (dp); el alto es <see cref="RosaHeightDp"/>.</summary>
        public const float RosaWidthDp = 104f;

        /// <summary>Alto del sprite de Rosa (dp).</summary>
        public const float RosaHeightDp = 66f;

        /// <summary>Respiradero de Rosa (fracción del sprite: x desde la izquierda, y desde abajo): de ahí sale el soplo.</summary>
        public static readonly Vector2 RosaBlowhole = new Vector2(0.43f, 0.75f);

        /// <summary>Lado del soplo de la exhalación (dp).</summary>
        public const float RosaSprayDp = 36f;

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

        private readonly Dictionary<string, Sprite> _bannerScenes = new Dictionary<string, Sprite>();
        private Sprite? _capiPresent;
        private Sprite? _capiPaw;
        private Sprite? _rosa;
        private Sprite? _rosaSpray;
        private Sprite? _rosaRipple;

        /// <summary>
        /// Capi sentado de perfil, mirando a la derecha, sin la pata que presenta (<see cref="CapiPaw"/>, con el hombro en
        /// <see cref="CapiShoulder"/>); <see cref="CapiPresentDp"/> de lado, con el pivote abajo en el centro.
        /// </summary>
        public Sprite CapiPresent => _capiPresent != null ? _capiPresent : (_capiPresent = BuildCapiPresent());

        /// <summary>
        /// La pata delantera que presenta (<c>capi_present</c>): <see cref="CapiPawLengthDp"/> × <see cref="CapiPawThickDp"/>,
        /// horizontal hacia la derecha con la mano abierta en la punta y el pivote en el hombro (a la izquierda).
        /// </summary>
        public Sprite CapiPaw => _capiPaw != null ? _capiPaw : (_capiPaw = BuildCapiPaw());

        /// <summary>
        /// Rosa asomando de perfil, mirando a la izquierda: la cabeza con la frente abombada y el hocico largo y el lomo arqueado
        /// con la aleta baja; lo de abajo del sprite queda bajo el agua (la vista lo recorta en la línea del agua).
        /// </summary>
        public Sprite Rosa => _rosa != null ? _rosa : (_rosa = BuildRosa());

        /// <summary>Soplo de la exhalación de Rosa (<see cref="RosaSprayDp"/>): gotitas y vaho claros, pivote abajo.</summary>
        public Sprite RosaSpray => _rosaSpray != null ? _rosaSpray : (_rosaSpray = BuildRosaSpray());

        /// <summary>
        /// Onda en el agua donde Rosa asoma y se sumerge (<see cref="RosaRippleWidthDp"/> × <see cref="RosaRippleHeightDp"/>):
        /// un anillo aplanado claro; la vista lo agranda y lo apaga.
        /// </summary>
        public Sprite RosaRipple => _rosaRipple != null ? _rosaRipple : (_rosaRipple = BuildRosaRipple());

        /// <summary>Ancho de la onda de Rosa (dp).</summary>
        public const float RosaRippleWidthDp = 72f;

        /// <summary>Alto de la onda de Rosa (dp).</summary>
        public const float RosaRippleHeightDp = 14f;

        /// <summary>
        /// Viñeta de río del banner de mundo, de <paramref name="widthDp"/> × <paramref name="heightDp"/> dp con las esquinas
        /// redondeadas: cielo, orilla de selva en el horizonte (<see cref="BannerSceneHorizon"/>), el agua del mundo
        /// (<paramref name="water"/>, <c>Palette.WaterBase</c>) con reflejos y, abajo a la izquierda, el banco de arena de Capi
        /// (su lo alto en <see cref="BannerSceneBankTop"/>). Una por tamaño y agua.
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

            Sprite sprite = MakeSprite(DrawBannerScene(w, h, water), "banner_scene_" + key, new Vector2(0.5f, 0.5f), Vector4.zero, BannerScenePxPerDp);
            _bannerScenes[key] = sprite;
            return sprite;
        }

        // ---- Capi ----

        // Distancia con signo (en unidades del lado del sprite) a la silueta de Capi sentado de perfil: grupa grande en el suelo,
        // cuerpo de barril inclinado hacia delante, cabeza en bloque, larga, con el hocico ancho y romo (como el animal real),
        // oreja chica arriba y atrás, la pata delantera de apoyo y la trasera, unidas con suavidad.
        private static float CapiSdf(float x, float y)
        {
            float haunch = SdEllipse(x, y, 0.29f, 0.25f, 0.245f, 0.215f, 0f);
            float chest = SdEllipse(x, y, 0.47f, 0.34f, 0.25f, 0.19f, 28f);
            float body = Smin(haunch, chest, 0.07f);
            float neck = SdCapsule(x, y, 0.50f, 0.42f, 0.59f, 0.55f, 0.115f);
            float head = SdRoundBox(x, y, 0.655f, 0.60f, 0.20f, 0.115f, 0.085f, -10f);
            float snout = SdRoundBox(x, y, 0.80f, 0.567f, 0.088f, 0.104f, 0.07f, -10f);
            head = Smin(head, snout, 0.03f);
            float d = Smin(body, Smin(head, neck, 0.05f), 0.05f);
            float ear = SdEllipse(x, y, 0.505f, 0.772f, 0.045f, 0.037f, 25f);
            d = Smin(d, ear, 0.012f);
            float leg = SdCapsule(x, y, 0.575f, 0.27f, 0.60f, 0.075f, 0.046f);
            float foot = SdEllipse(x, y, 0.625f, 0.052f, 0.072f, 0.034f, 0f);
            d = Smin(d, Smin(leg, foot, 0.02f), 0.025f);
            float hind = SdEllipse(x, y, 0.24f, 0.058f, 0.15f, 0.046f, 0f);
            return Smin(d, hind, 0.04f);
        }

        private Sprite BuildCapiPresent()
        {
            int s = Mathf.RoundToInt(CapiPresentDp * MascotPxPerDp);
            var pc = new PixelCanvas(s, s);
            float inv = 1f / s;
            float outline = MascotOutlineDp * MascotPxPerDp;
            Color furDark = ColorMath.Darken(CapiFur, 0.22f);
            Color furLight = ColorMath.Lighten(CapiFur, 0.14f);
            Color muzzle = ColorMath.Darken(CapiFur, 0.10f);
            for (int py = 0; py < s; py++)
            {
                for (int px = 0; px < s; px++)
                {
                    float x = (px + 0.5f) * inv;
                    float y = (py + 0.5f) * inv;
                    float d = CapiSdf(x, y) * s; // px, negativa dentro
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

                    // Pelaje: más oscuro abajo y en la grupa, luz de juego arriba (el canto de arriba de cada forma), y una veta
                    // muy leve de pelo.
                    float shade = Mathf.Clamp01((y - 0.05f) / 0.85f);
                    Color fur = Color.Lerp(furDark, CapiFur, Mathf.SmoothStep(0f, 1f, shade));
                    float rim = Mathf.Clamp01(1f - ((-d) / (4f * outline)));
                    float up = Mathf.Clamp01((CapiSdf(x, y - (2f * inv)) - CapiSdf(x, y)) * s * 0.8f);
                    fur = Color.Lerp(fur, furLight, 0.55f * rim * up);
                    float grain = (ArtNoise.Value2(x * 95f, y * 95f, 71) - 0.5f) * 0.035f;
                    fur = ColorMath.Lighten(fur, grain);

                    // Panza clara en el pecho y la barriga.
                    float belly = SdEllipse(x, y, 0.53f, 0.285f, 0.16f, 0.115f, 28f) * s;
                    if (belly < 2f)
                    {
                        fur = Color.Lerp(fur, CapiBelly, PixelCanvas.SoftCoverage(belly, 3f * MascotPxPerDp));
                    }

                    // Hocico algo más oscuro; oreja con su interior.
                    float snout = SdRoundBox(x, y, 0.815f, 0.565f, 0.07f, 0.09f, 0.06f, -10f) * s;
                    if (snout < 2f)
                    {
                        fur = Color.Lerp(fur, muzzle, 0.6f * PixelCanvas.SoftCoverage(snout, 2f * MascotPxPerDp));
                    }

                    float earIn = SdEllipse(x, y, 0.51f, 0.768f, 0.024f, 0.019f, 25f) * s;
                    if (earIn < 1f)
                    {
                        fur = Color.Lerp(fur, CapiNose, 0.7f * PixelCanvas.Coverage(earIn));
                    }

                    fur.a = 1f;
                    pc.Blend(px, py, fur, cov);
                }
            }

            // La línea de la grupa sobre el muslo y la que separa la pata de apoyo del pecho (trazos finos de tinta).
            StrokeArcUnits(pc, s, 0.28f, 0.20f, 0.14f, 25f, 120f, 0.9f * MascotOutlineDp);
            StrokeArcUnits(pc, s, 0.555f, 0.245f, 0.06f, 190f, 290f, 0.8f * MascotOutlineDp);

            // Ojo pequeño y oscuro con un brillo, alto en la cabeza; la nariz arriba en la punta del hocico; la boca.
            float k = s;
            pc.FillCircle(0.645f * k, 0.665f * k, 0.024f * k, Palette.Ink);
            pc.FillCircle(0.652f * k, 0.674f * k, 0.0085f * k, new Color(1f, 1f, 1f, 0.95f));
            pc.FillEllipseSoft(0.868f * k, 0.618f * k, 0.03f * k, 0.019f * k, 1.2f, CapiNose);
            pc.FillEllipseSoft(0.879f * k, 0.622f * k, 0.008f * k, 0.006f * k, 1f, new Color(0f, 0f, 0f, 0.65f));
            StrokeArcUnits(pc, s, 0.855f, 0.535f, 0.03f, 200f, 300f, 0.8f * MascotOutlineDp);
            return MakeSprite(pc, "capi_present_body", new Vector2(0.5f, 0f), Vector4.zero, MascotPxPerDp);
        }

        private Sprite BuildCapiPaw()
        {
            int w = Mathf.RoundToInt(CapiPawLengthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(CapiPawThickDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float outline = MascotOutlineDp * MascotPxPerDp;
            float cy = h * 0.5f;
            float r = (h * 0.5f) - outline - 1f;
            float handR = r * 1.12f;
            float ax = r + outline + 1f;
            float bx = w - handR - outline - 2f;
            Color furDark = ColorMath.Darken(CapiFur, 0.12f);
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    float x = px + 0.5f;
                    float y = py + 0.5f;
                    float arm = SdCapsulePx(x, y, ax, cy, bx, cy, r * 0.9f);
                    float hand = Mathf.Sqrt(((x - bx) * (x - bx)) + ((y - cy) * (y - cy))) - handR;
                    float d = Smin(arm, hand, 2f);
                    pc.Blend(px, py, MascotInk, PixelCanvas.Coverage(d - outline));
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color fur = Color.Lerp(furDark, CapiFur, Mathf.Clamp01(y / h + 0.25f));
                    if (y > cy + (0.35f * r))
                    {
                        fur = Color.Lerp(fur, ColorMath.Lighten(CapiFur, 0.14f), 0.4f);
                    }

                    fur.a = 1f;
                    pc.Blend(px, py, fur, cov);
                }
            }

            // Dedos: dos trazos cortos en la mano abierta.
            for (int f = -1; f <= 1; f += 2)
            {
                float fy = cy + (f * handR * 0.38f);
                pc.FillCapsule(bx + (handR * 0.25f), fy, bx + (handR * 0.85f), fy, Mathf.Max(0.8f, 0.4f * MascotPxPerDp), MascotInk);
            }

            return MakeSprite(pc, "capi_present_paw", new Vector2(ax / w, 0.5f), Vector4.zero, MascotPxPerDp);
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

        private Sprite BuildRosaSpray()
        {
            int s = Mathf.RoundToInt(RosaSprayDp * MascotPxPerDp);
            var pc = new PixelCanvas(s, s);
            var mist = new Color(1f, 1f, 1f, 0.55f);
            pc.FillEllipseSoft(s * 0.5f, s * 0.58f, s * 0.30f, s * 0.22f, s * 0.12f, mist);
            var drop = new Color(0.94f, 0.98f, 1f, 0.95f);
            pc.FillCircle(s * 0.30f, s * 0.70f, s * 0.07f, drop);
            pc.FillCircle(s * 0.52f, s * 0.84f, s * 0.08f, drop);
            pc.FillCircle(s * 0.72f, s * 0.66f, s * 0.065f, drop);
            pc.FillCircle(s * 0.46f, s * 0.48f, s * 0.05f, drop);
            pc.FillCapsule(s * 0.5f, s * 0.1f, s * 0.5f, s * 0.4f, s * 0.05f, new Color(1f, 1f, 1f, 0.5f));
            return MakeSprite(pc, "rosa_spray", new Vector2(0.5f, 0f), Vector4.zero, MascotPxPerDp);
        }

        private Sprite BuildRosaRipple()
        {
            int w = Mathf.RoundToInt(RosaRippleWidthDp * MascotPxPerDp);
            int h = Mathf.RoundToInt(RosaRippleHeightDp * MascotPxPerDp);
            var pc = new PixelCanvas(w, h);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float rx = (w * 0.5f) - 2f;
            float ry = (h * 0.5f) - 2f;
            float thick = 1.6f * MascotPxPerDp;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = ((x + 0.5f) - cx) / rx;
                    float py = ((y + 0.5f) - cy) / ry;
                    float d = (Mathf.Sqrt((px * px) + (py * py)) - 1f) * ry;
                    float ring = PixelCanvas.Coverage(Mathf.Abs(d) - (thick * 0.5f));
                    // Más luz en el canto de delante (abajo) que en el de detrás.
                    float front = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(((y + 0.5f) - cy) / ry * 0.5f + 0.5f));
                    pc.Blend(x, y, new Color(1f, 1f, 1f, 0.85f * front), ring);
                }
            }

            return MakeSprite(pc, "rosa_ripple", new Vector2(0.5f, 0.5f), Vector4.zero, MascotPxPerDp);
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

                        // Orilla de selva: copas irregulares sobre el horizonte, la lejana más clara (bruma).
                        float far = horizon + ((6f + (10f * ArtNoise.Fbm2(fx * 9f, 0.5f, 31))) * k);
                        float near = horizon + ((3f + (13f * ArtNoise.Fbm2(fx * 6f, 3.5f, 47)) - (5f * Mathf.Clamp01(fx * 2.2f - 0.4f))) * k);
                        if (y < far)
                        {
                            c = Color.Lerp(Palette.FarShore, Palette.FarPalm, Mathf.Clamp01((far - y) / (8f * k)));
                        }

                        if (y < near)
                        {
                            c = Color.Lerp(Palette.JungleMid, Palette.JungleShadow, Mathf.Clamp01((near - y) / (10f * k)));
                        }
                    }
                    else
                    {
                        // Agua del mundo: refleja el cielo junto al horizonte y se oscurece hacia delante, con rizos de luz.
                        float t = Mathf.Clamp01((horizon - y) / horizon);
                        c = Color.Lerp(waterTop, water, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 2.2f)));
                        c = Color.Lerp(c, waterLow, Mathf.Clamp01((t - 0.5f) * 1.6f));
                        float rowNoise = ArtNoise.Value2(fx * 18f * (1f + t), y / (2.2f * k), 53);
                        float stripe = Mathf.Abs(Mathf.Repeat(y / (5.5f * k * (0.6f + t)), 1f) - 0.5f);
                        if (rowNoise > 0.62f && stripe < 0.12f)
                        {
                            c = Color.Lerp(c, Palette.WaterLine, 0.45f * (1f - t * 0.6f));
                        }
                        else if (rowNoise < 0.28f && stripe > 0.40f)
                        {
                            c = Color.Lerp(c, waterLow, 0.55f); // senos de las olas: el agua clara también se lee como agua
                        }
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
                        float blade = ArtNoise.Value1(fx * 140f, 17);
                        if (bank > -(1.5f + (3f * blade)) * k)
                        {
                            sand = Color.Lerp(Palette.LeafMid, Palette.LeafLight, blade);
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
