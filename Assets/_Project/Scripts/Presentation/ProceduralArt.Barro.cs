#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] del lavado del Barro (02 §9.1 M5; propuesta de D-007 b.2 M5: «el barro se enturbia y se disuelve
    /// en la fruta nueva (~400 ms), con un chapoteo»; D-003): la nube de barro que enturbia la dosis lavada mientras se
    /// disuelve. Los grumos son los de T03 (<see cref="MudLumps"/>).
    /// </summary>
    /// <remarks>
    /// Perezoso: solo lo pide el primer lavado (<c>MudMarks.PlayWash</c>), así que un nivel sin Barro no crea la textura.
    /// Colores horneados en los pardos del Barro (<see cref="Palette.Mud"/> y <see cref="MudLumpColor"/>), con los bordes
    /// difuminados para que la nube no marque el rectángulo de la banda; <c>Image.color</c> solo cambia su alfa.
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        // Volutas de la nube: (x, y) relativos a la banda, radios (fracción del alto) y tono (0 = Barro, 1 = grumo oscuro,
        // 2 = sedimento claro).
        private static readonly float[] BarroCloudPuffs =
        {
            0.30f, 0.46f, 0.30f, 0.22f, 0f,
            0.55f, 0.58f, 0.34f, 0.20f, 0f,
            0.74f, 0.40f, 0.26f, 0.21f, 0f,
            0.44f, 0.30f, 0.22f, 0.15f, 1f,
            0.66f, 0.66f, 0.20f, 0.12f, 1f,
            0.22f, 0.64f, 0.16f, 0.10f, 2f,
            0.82f, 0.62f, 0.14f, 0.09f, 2f,
            0.50f, 0.46f, 0.18f, 0.08f, 2f,
        };

        private Sprite? _barroCloud;

        /// <summary>
        /// Nube de barro de una banda de dosis (1,6 × 1 u, el ancho de la cavidad): volutas pardas con grumos oscuros y
        /// sedimento claro, y una voluta en espiral que da el remolino del barro al disolverse. Bordes difuminados.
        /// </summary>
        public Sprite BarroCloud => _barroCloud != null ? _barroCloud : (_barroCloud = BuildBarroCloud());

        private Sprite BuildBarroCloud()
        {
            int w = Mathf.RoundToInt(2f * CuiaGeometry.CavityHalfWidthU * MechanicPxPerU);
            int h = MechanicPxPerU;
            var pc = new PixelCanvas(w, h);
            Color mud = Palette.Mud;
            Color dark = MudLumpColor;
            Color silt = ColorMath.Lighten(Palette.Mud, 0.28f);

            // Velo de fondo y volutas.
            pc.FillRoundedRectSoft(6f, 10f, w - 6f, h - 10f, 26f, 12f, ColorMath.WithAlpha(mud, 0.55f));
            float[] puffs = BarroCloudPuffs;
            for (int i = 0; i < puffs.Length; i += 5)
            {
                float tone = puffs[i + 4];
                Color c = tone < 0.5f ? ColorMath.WithAlpha(mud, 0.85f) : (tone < 1.5f ? ColorMath.WithAlpha(dark, 0.7f) : ColorMath.WithAlpha(silt, 0.6f));
                pc.FillEllipseSoft(puffs[i] * w, puffs[i + 1] * h, puffs[i + 2] * h * 1.6f, puffs[i + 3] * h * 1.6f, 6f, c);
            }

            // Voluta en espiral de sedimento claro (el barro que se deshace girando).
            Vector2 prev = new Vector2(w * 0.5f, h * 0.5f);
            const int steps = 60;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = t * 1.6f * Mathf.PI * 2f;
                var p = new Vector2((w * 0.5f) + (Mathf.Cos(angle) * t * w * 0.34f), (h * 0.5f) + (Mathf.Sin(angle) * t * h * 0.3f));
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, 1.2f + (2.2f * t), ColorMath.WithAlpha(silt, 0.55f * (1f - (0.5f * t))));
                prev = p;
            }

            // Bordes difuminados: la nube se apaga en los últimos 14 px de cada lado.
            const float feather = 14f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x + 0.5f, w - (x + 0.5f)), Mathf.Min(y + 0.5f, h - (y + 0.5f)));
                    float k = Mathf.Clamp01(edge / feather);
                    pc.MaskAlpha(x, y, k * k * (3f - (2f * k)));
                }
            }

            return MakeSprite(pc, "barro_cloud", new Vector2(0.5f, 0.5f), Vector4.zero);
        }
    }
}
