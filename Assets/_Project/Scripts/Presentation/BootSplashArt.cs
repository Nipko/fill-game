#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas por código de la pantalla de arranque con la marca v2 (añadido de D-051, 2026-10-25; D-045 «El río que se tiñe»;
    /// D-003 «Selva brillante premium»): el mundo del logo alrededor del emblema de la intro (<c>BrandIntro</c>), con la paleta del
    /// icono v2 (<c>docs/art/marca-oficial/v2/generar.py</c>): la selva de la orilla de enfrente que sigue a lo ancho, las palmeras
    /// de açaí en silueta, el río blanco y el teñido a sangre, la orilla de acá y su follaje en silueta. Las siluetas salen blancas
    /// (se tiñen con el color de la <c>Image</c>); las demás, con su color. Arte final por código (D-050, D-052): sale en todos los
    /// builds. Cada llamada crea una textura nueva, que es de quien la pide.
    /// </summary>
    public static class BootSplashArt
    {
        /// <summary>Selva de la orilla de enfrente (#0E4232, la del icono y la intro).</summary>
        public static readonly Color Jungle = Palette.Hex(0x0E4232);

        /// <summary>Línea clara de la orilla de acá (#26603A, el trazo del icono).</summary>
        public static readonly Color BankLine = Palette.Hex(0x26603A);

        /// <summary>La orilla de acá, arriba (la misma selva oscura del icono).</summary>
        public static readonly Color GroundTop = Palette.Hex(0x0E4232);

        /// <summary>La orilla de acá, abajo: el petróleo hondo del icono (#0A2A2E).</summary>
        public static readonly Color GroundBottom = Palette.Hex(0x0A2A2E);

        /// <summary>
        /// Línea de copas a lo ancho (blanca), sin fundido en los extremos y sin palmeras: sigue la selva del emblema hasta los
        /// bordes. Copas redondeadas de dos tamaños, con 1 px de suavizado.
        /// </summary>
        public static Texture2D Treeline(int w, int h)
        {
            Texture2D tex = NewTexture(w, h, "AF_BootTreeline");
            var px = new Color32[w * h];
            for (int x = 0; x < w; x++)
            {
                float top = 0.42f + (0.20f * Mathf.Abs(Mathf.Sin((x * 0.061f) + 0.4f))) + (0.11f * Mathf.Abs(Mathf.Sin((x * 0.153f) + 1.3f)));
                float edge = Mathf.Clamp01(top) * (h - 1);
                for (int y = 0; y < h; y++)
                {
                    float a = Mathf.Clamp01(edge - y + 0.5f);
                    px[(y * w) + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Palmera de açaí en silueta (blanca), como las del icono v2: tronco fino y algo inclinado, y la corona de hojas
        /// pinnadas que se arquean, con sus foliolos colgando. El pie, abajo en el centro.
        /// </summary>
        public static Texture2D Palm(int w, int h)
        {
            var m = new float[w * h];
            float bx = w * 0.5f;
            float lean = w * 0.10f;
            var top = new Vector2(bx + lean, h * 0.70f);
            var ctrl = new Vector2(bx + (lean * 0.15f), h * 0.36f);
            float trunkR = Mathf.Max(1.6f, w * 0.022f);
            Vector2 prev = new Vector2(bx, 0f);
            for (int i = 1; i <= 24; i++)
            {
                float t = i / 24f;
                Vector2 p = Quad(new Vector2(bx, 0f), ctrl, top, t);
                Capsule(m, w, h, prev, p, Mathf.Lerp(trunkR * 1.25f, trunkR * 0.8f, t));
                prev = p;
            }

            // Las hojas: de la copa hacia fuera, subiendo y cayendo en arco; los foliolos cuelgan a lo largo.
            float[] angles = { 12f, 38f, 64f, 88f, 112f, 140f, 166f };
            float[] lengths = { 0.46f, 0.42f, 0.34f, 0.28f, 0.34f, 0.42f, 0.46f };
            float reach = w * 0.98f;
            for (int f = 0; f < angles.Length; f++)
            {
                float a = angles[f] * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float len = lengths[f] * reach;
                Vector2 c = top + (d * (0.55f * len)) + new Vector2(0f, 0.22f * len);
                Vector2 end = top + (d * len) + new Vector2(0f, -0.50f * len * (1f - Mathf.Abs(d.y)) - (0.12f * len));
                Vector2 p0 = top;
                for (int i = 1; i <= 18; i++)
                {
                    float t = i / 18f;
                    Vector2 p = Quad(top, c, end, t);
                    float r = Mathf.Lerp(trunkR * 1.05f, trunkR * 0.35f, t);
                    Capsule(m, w, h, p0, p, r);
                    if (i >= 3 && i % 2 == 1)
                    {
                        // Dos foliolos, uno a cada lado, colgando hacia abajo y hacia fuera.
                        Vector2 tangent = (p - p0).normalized;
                        float leaf = Mathf.Lerp(len * 0.20f, len * 0.09f, t);
                        var side = new Vector2(-tangent.y, tangent.x);
                        for (int sgn = -1; sgn <= 1; sgn += 2)
                        {
                            Vector2 dir = ((side * sgn * 0.45f) + new Vector2(tangent.x * 0.35f, -1f)).normalized;
                            Capsule(m, w, h, p, p + (dir * leaf), Mathf.Max(0.7f, r * 0.42f));
                        }
                    }

                    p0 = p;
                }
            }

            return MaskTexture(m, w, h, "AF_BootPalm");
        }

        /// <summary>
        /// Banda del río a sangre, sin fundidos en los extremos: degradado vertical (arriba <paramref name="top"/>, en medio
        /// <paramref name="mid"/>, abajo <paramref name="low"/>) con ondas claras (<paramref name="wave"/>) a tramos, como las de la
        /// intro. Con <paramref name="front"/>, el borde izquierdo es el frente ondulado y suave del tinte que entra en el agua.
        /// </summary>
        public static Texture2D RiverBand(int w, int h, Color top, Color mid, Color low, Color wave, bool front, int seed)
        {
            Texture2D tex = NewTexture(w, h, front ? "AF_BootRiverTint" : "AF_BootRiver");
            var px = new Color32[w * h];
            float phase = seed * 1.37f;
            for (int y = 0; y < h; y++)
            {
                float v = 1f - (y / (float)(h - 1)); // 0 arriba, 1 abajo
                Color body = v < 0.25f ? Color.Lerp(top, mid, v / 0.25f) : Color.Lerp(mid, low, (v - 0.25f) / 0.75f);
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    float a = 1f;
                    if (front)
                    {
                        float f = 0.03f + (0.03f * Mathf.Sin(v * 9f));
                        a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - f) / (24f / w)));
                    }

                    float waves = 0f;
                    for (int k = 0; k < 2; k++)
                    {
                        float v0 = k == 0 ? 0.42f : 0.72f;
                        float line = v0 + (0.06f * Mathf.Sin((x * 0.040f) + (k * 1.7f) + phase));
                        float stroke = Mathf.Clamp01(1f - (Mathf.Abs(v - line) * h / 1.6f));
                        float segment = Mathf.Clamp01((Mathf.Sin((x * 0.026f) + (k * 2.3f) + phase) * 3f) - 1f);
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

        /// <summary>La orilla de acá: degradado vertical de <see cref="GroundTop"/> (arriba) a <see cref="GroundBottom"/> (abajo).</summary>
        public static Texture2D Ground(int h)
        {
            Texture2D tex = NewTexture(1, h, "AF_BootGround");
            var px = new Color32[h];
            for (int y = 0; y < h; y++)
            {
                float t = 1f - (y / (float)(h - 1)); // 0 arriba, 1 abajo
                px[y] = Color.Lerp(GroundTop, GroundBottom, Mathf.SmoothStep(0f, 1f, t));
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Follaje en silueta de la esquina de abajo a la izquierda (la de la derecha es la misma volteada): hojas anchas y
        /// lanceoladas que suben desde la esquina, en verdes de noche apenas encendidos por arriba, sin especie reconocible.
        /// <paramref name="k"/> = px por dp de la textura (<paramref name="sizeDp"/> de lado).
        /// </summary>
        public static Texture2D CornerFoliage(float sizeDp, float k)
        {
            int n = Mathf.RoundToInt(sizeDp * k);
            var pc = new PixelCanvas(n, n);
            Color backDark = Palette.Hex(0x072720);
            Color backLight = Palette.Hex(0x0F3D2E);
            Color frontDark = Palette.Hex(0x0B3427);
            Color frontLight = Palette.Hex(0x1D6343);
            float s = sizeDp;

            // Detrás: hojas largas y más oscuras.
            Leaf(pc, k, 0.02f * s, 0f, 80f, 0.95f * s, 0.20f * s, 0.10f, LeafShape.Lanceolate, backDark, backLight, 901);
            Leaf(pc, k, 0.10f * s, 0f, 52f, 0.86f * s, 0.24f * s, -0.12f, LeafShape.Ovate, backDark, backLight, 902);
            Leaf(pc, k, 0.04f * s, 0f, 24f, 0.80f * s, 0.20f * s, 0.10f, LeafShape.Lanceolate, backDark, backLight, 903);

            // Delante: tres hojas anchas.
            Leaf(pc, k, 0.00f, 0f, 66f, 0.70f * s, 0.30f * s, -0.10f, LeafShape.Ovate, frontDark, frontLight, 911);
            Leaf(pc, k, 0.06f * s, 0f, 38f, 0.66f * s, 0.30f * s, 0.12f, LeafShape.Ovate, frontDark, frontLight, 912);
            Leaf(pc, k, 0.00f, 0.02f * s, 12f, 0.56f * s, 0.24f * s, -0.08f, LeafShape.Ovate, frontDark, frontLight, 913);
            return pc.ToTexture("AF_BootFoliage");
        }

        /// <summary>
        /// Matorral en silueta a lo ancho del pie de la pantalla (<paramref name="widthDp"/> × <paramref name="heightDp"/> dp, a
        /// <paramref name="k"/> px por dp): puntas de hojas que suben del borde de abajo, casi negras de verde, detrás del follaje
        /// de las esquinas. Da suelo a la orilla de acá.
        /// </summary>
        public static Texture2D Hedge(float widthDp, float heightDp, float k)
        {
            int w = Mathf.RoundToInt(widthDp * k);
            int h = Mathf.RoundToInt(heightDp * k);
            var pc = new PixelCanvas(w, h);
            Color dark = Palette.Hex(0x05201A);
            Color light = Palette.Hex(0x0A2C22);
            int n = Mathf.Max(6, Mathf.RoundToInt(widthDp / 15f));
            for (int i = 0; i < n; i++)
            {
                float u = (i + 0.5f) / n;
                float jitter = ArtNoise.Hash(i, 3, 917) - 0.5f;
                float deg = 90f + (jitter * 70f) + ((u - 0.5f) * 30f);
                float len = heightDp * (0.62f + (0.38f * ArtNoise.Hash(i, 5, 919)));
                float wid = len * (0.30f + (0.10f * ArtNoise.Hash(i, 7, 921)));
                LeafShape shape = (i % 3) == 1 ? LeafShape.Ovate : LeafShape.Lanceolate;
                Leaf(pc, k, (u * widthDp) + (jitter * 8f), -2f, deg, len, wid, jitter * 0.3f, shape, dark, light, 930 + i);
            }

            return pc.ToTexture("AF_BootHedge");
        }

        // Una hoja desde (x, y) dp, con el ángulo deg (0 = derecha, 90 = arriba), largo y ancho en dp.
        private static void Leaf(PixelCanvas pc, float k, float x, float y, float deg, float length, float width, float bend, LeafShape shape, Color dark, Color light, int seed)
        {
            float a = deg * Mathf.Deg2Rad;
            ProceduralArt.DrawLeaf(pc, new Vector2(x * k, y * k), new Vector2(Mathf.Cos(a), Mathf.Sin(a)), length * k, width * k, bend, shape, dark, light, k, seed);
        }

        private static Vector2 Quad(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return (u * u * a) + (2f * u * t * b) + (t * t * c);
        }

        // Cápsula de a a b y radio r en la máscara (unión: el máximo de las coberturas), con 1 px de suavizado.
        private static void Capsule(float[] m, int w, int h, Vector2 a, Vector2 b, float r)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r - 1f));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r + 1f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r - 1f));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r + 1f));
            Vector2 v = b - a;
            float len2 = Mathf.Max(1e-6f, v.sqrMagnitude);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f - a.x, y + 0.5f - a.y);
                    float t = Mathf.Clamp01(Vector2.Dot(p, v) / len2);
                    float d = (p - (v * t)).magnitude - r;
                    float cov = Mathf.Clamp01(0.5f - d);
                    int i = (y * w) + x;
                    if (cov > m[i])
                    {
                        m[i] = cov;
                    }
                }
            }
        }

        private static Texture2D MaskTexture(float[] m, int w, int h, string name)
        {
            Texture2D tex = NewTexture(w, h, name);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(m[i]) * 255f));
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
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
    }
}
