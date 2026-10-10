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
    /// Perezoso: lo pide <c>MudMarks</c> la primera vez que una cuia tiene Barro (al cargar el nivel, fuera de los pasos), así
    /// que un nivel sin Barro no crea la textura y el lavado nunca la genera a mitad del paso.
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

        /// <summary>Barrido máximo de cada arco de sedimento de la nube (grados): abiertos, nunca una espiral.</summary>
        public const float BarroArcMaxSweepDeg = 120f;

        // Arcos de sedimento: (x, y) del centro relativos a la banda, radio vertical (fracción del alto; el horizontal es 1,6
        // veces), inicio y barrido (grados) y trazo (px). Repartidos entre las volutas, cada uno con su sentido.
        private static readonly float[] BarroCloudArcs =
        {
            0.36f, 0.50f, 0.17f, 200f, 110f, 4.5f,
            0.64f, 0.44f, 0.15f, 10f, 100f, 4f,
            0.52f, 0.66f, 0.12f, 120f, 90f, 3.5f,
        };

        private Sprite? _barroCloud;

        /// <summary>
        /// Nube de barro de una banda de dosis (1,6 × 1 u, el ancho de la cavidad): volutas pardas con grumos oscuros y
        /// sedimento claro, y unos arcos abiertos de sedimento (≤ <see cref="BarroArcMaxSweepDeg"/>°) que dan el remolino suelto
        /// del barro al disolverse, sin la espiral del Remolino. Bordes difuminados.
        /// </summary>
        public Sprite BarroCloud => _barroCloud != null ? _barroCloud : (_barroCloud = BuildBarroCloud());

        /// <summary>La nube de barro ya está generada (tests: un nivel sin Barro no la crea).</summary>
        public bool HasBarroCloud => _barroCloud != null;

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

            // Arcos abiertos de sedimento claro entre las volutas (el barro que se deshace en remolinos sueltos). Nunca una
            // espiral cerrada: es la seña del Remolino, y en un nivel con las dos mecánicas la cuia lavada no debe parecerlo.
            float[] arcs = BarroCloudArcs;
            for (int i = 0; i < arcs.Length; i += 6)
            {
                DrawBarroArc(pc, arcs[i] * w, arcs[i + 1] * h, arcs[i + 2] * h * 1.6f, arcs[i + 2] * h, arcs[i + 3], arcs[i + 4], arcs[i + 5], silt);
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

        // Arco abierto de sedimento: elipse de radios rx × ry centrada en (cx, cy), de startDeg a startDeg + sweepDeg (≤ 120°),
        // con trazo de stroke px que se afila en los dos extremos.
        private static void DrawBarroArc(PixelCanvas pc, float cx, float cy, float rx, float ry, float startDeg, float sweepDeg, float stroke, Color silt)
        {
            const int steps = 24;
            float a0 = startDeg * Mathf.Deg2Rad;
            float sweep = Mathf.Min(sweepDeg, BarroArcMaxSweepDeg) * Mathf.Deg2Rad;
            var prev = new Vector2(cx + (Mathf.Cos(a0) * rx), cy + (Mathf.Sin(a0) * ry));
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = a0 + (t * sweep);
                var p = new Vector2(cx + (Mathf.Cos(angle) * rx), cy + (Mathf.Sin(angle) * ry));
                float taper = Mathf.Sin(t * Mathf.PI); // fino en los extremos, como una estela
                float radius = 0.5f * stroke * (0.35f + (0.65f * taper));
                pc.FillCapsule(prev.x, prev.y, p.x, p.y, radius, ColorMath.WithAlpha(silt, 0.45f * (0.4f + (0.6f * taper))));
                prev = p;
            }
        }
    }
}
