#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Piezas por código de la pantalla de arranque con la marca v2 (añadido de D-051, 2026-10-25; D-045 «El río que se tiñe»;
    /// D-003 «Selva brillante premium»): lo que hay alrededor del emblema (<c>BrandEmblem</c>), con la paleta del icono v2
    /// (<c>docs/art/marca-oficial/v2/generar.py</c>): el follaje y el matorral en silueta del primer plano. Arte final por código
    /// (D-050, D-052): sale en todos los builds. Cada llamada crea una textura nueva, que es de quien la pide.
    /// </summary>
    public static class BootSplashArt
    {
        /// <summary>
        /// El primer plano entero en una sola textura (<paramref name="widthDp"/> × <paramref name="heightDp"/> dp, a
        /// <paramref name="k"/> px por dp): el matorral en silueta a lo ancho del pie (<paramref name="hedgeDp"/> de alto) y, delante,
        /// el follaje de las dos esquinas de abajo (el de la derecha, el mismo volteado), de lado <paramref name="heightDp"/>. Al ser
        /// una sola imagen, se funde sin que unas hojas dejen ver a otras.
        /// </summary>
        public static Texture2D Foreground(float widthDp, float heightDp, float hedgeDp, float k)
        {
            int w = Mathf.RoundToInt(widthDp * k);
            int h = Mathf.RoundToInt(heightDp * k);
            var pc = new PixelCanvas(w, h);
            DrawHedge(pc, k, widthDp, hedgeDp);
            DrawCorner(pc, k, widthDp, heightDp, false);
            DrawCorner(pc, k, widthDp, heightDp, true);
            return pc.ToTexture("AF_BootForeground");
        }

        // El follaje de una esquina de abajo (la derecha, volteada): hojas anchas y lanceoladas que suben desde la esquina, en verdes
        // de noche apenas encendidos por arriba, sin especie reconocible.
        private static void DrawCorner(PixelCanvas pc, float k, float widthDp, float s, bool mirror)
        {
            Color backDark = Palette.Hex(0x072720);
            Color backLight = Palette.Hex(0x0F3D2E);
            Color frontDark = Palette.Hex(0x0B3427);
            Color frontLight = Palette.Hex(0x1D6343);
            CornerLeaf(pc, k, widthDp, mirror, 0.02f * s, 0f, 80f, 0.95f * s, 0.16f * s, 0.10f, LeafShape.Ovate, backDark, backLight, 901);
            CornerLeaf(pc, k, widthDp, mirror, 0.10f * s, 0f, 52f, 0.86f * s, 0.24f * s, -0.12f, LeafShape.Ovate, backDark, backLight, 902);
            CornerLeaf(pc, k, widthDp, mirror, 0.04f * s, 0f, 24f, 0.80f * s, 0.16f * s, 0.10f, LeafShape.Ovate, backDark, backLight, 903);
            CornerLeaf(pc, k, widthDp, mirror, 0.00f, 0f, 66f, 0.70f * s, 0.30f * s, -0.10f, LeafShape.Ovate, frontDark, frontLight, 911);
            CornerLeaf(pc, k, widthDp, mirror, 0.06f * s, 0f, 38f, 0.66f * s, 0.30f * s, 0.12f, LeafShape.Ovate, frontDark, frontLight, 912);
            CornerLeaf(pc, k, widthDp, mirror, 0.00f, 0.02f * s, 12f, 0.56f * s, 0.24f * s, -0.08f, LeafShape.Ovate, frontDark, frontLight, 913);
        }

        private static void CornerLeaf(PixelCanvas pc, float k, float widthDp, bool mirror, float x, float y, float deg, float length, float width, float bend, LeafShape shape, Color dark, Color light, int seed)
        {
            if (mirror)
            {
                Leaf(pc, k, widthDp - x, y, 180f - deg, length, width, -bend, shape, dark, light, seed);
            }
            else
            {
                Leaf(pc, k, x, y, deg, length, width, bend, shape, dark, light, seed);
            }
        }

        // El matorral: puntas de hojas que suben del borde de abajo, casi negras de verde.
        private static void DrawHedge(PixelCanvas pc, float k, float widthDp, float heightDp)
        {
            Color dark = Palette.Hex(0x05201A);
            Color light = Palette.Hex(0x0A2C22);
            int n = Mathf.Max(6, Mathf.RoundToInt(widthDp / 15f));
            for (int i = 0; i < n; i++)
            {
                float u = (i + 0.5f) / n;
                float jitter = ArtNoise.Hash(i, 3, 917) - 0.5f;
                float deg = 90f + (jitter * 70f) + ((u - 0.5f) * 30f);
                float len = heightDp * (0.62f + (0.38f * ArtNoise.Hash(i, 5, 919)));
                float wid = len * ((i % 3) == 1 ? 0.30f : 0.22f) * (1f + (0.33f * ArtNoise.Hash(i, 7, 921)));
                Leaf(pc, k, (u * widthDp) + (jitter * 8f), -2f, deg, len, wid, jitter * 0.3f, LeafShape.Ovate, dark, light, 930 + i);
            }
        }

        // Una hoja desde (x, y) dp, con el ángulo deg (0 = derecha, 90 = arriba), largo y ancho en dp. Solo ovadas: se eligieron
        // cuando las lanceoladas de ProceduralArt.DrawLeaf daban NaN en la punta y borraban una línea de lo pintado antes
        // (corregido en el añadido de D-051; LeafDrawingTests). Se quedan ovadas: el dibujo del arranque ya está aprobado así.
        private static void Leaf(PixelCanvas pc, float k, float x, float y, float deg, float length, float width, float bend, LeafShape shape, Color dark, Color light, int seed)
        {
            float a = deg * Mathf.Deg2Rad;
            ProceduralArt.DrawLeaf(pc, new Vector2(x * k, y * k), new Vector2(Mathf.Cos(a), Mathf.Sin(a)), length * k, width * k, bend, shape, dark, light, k, seed);
        }
    }
}
