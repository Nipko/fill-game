#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas por código de la pantalla de arranque con la marca v2 (añadido de D-051, 2026-10-25; D-045 «El río que se tiñe»;
    /// D-003 «Selva brillante premium»): lo que hay alrededor del emblema (<c>BrandEmblem</c>), con la paleta del icono v2
    /// (<c>docs/art/marca-oficial/v2/generar.py</c>): el suelo de la orilla de acá, que sigue al del icono, y su follaje y su
    /// matorral en silueta. Arte final por código (D-050, D-052): sale en todos los builds. Cada llamada crea una textura nueva, que
    /// es de quien la pide.
    /// </summary>
    public static class BootSplashArt
    {
        /// <summary>La orilla de acá, arriba (la misma selva oscura del icono).</summary>
        public static readonly Color GroundTop = Palette.Hex(0x0E4232);

        /// <summary>La orilla de acá, abajo: el petróleo hondo del icono (#0A2A2E).</summary>
        public static readonly Color GroundBottom = Palette.Hex(0x0A2A2E);

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
