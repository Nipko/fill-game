#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Arte procedural [P] de la Lluvia (M6; 02 §9.1 M6; propuesta de D-007 b.2 M6; D-003 «Selva brillante premium»): la nube
    /// gris perla con volumen (lóbulos con luz de arriba, panza en sombra, filo de luz y contorno pizarra) y la gota del agua
    /// del mundo, de vidrio, con brillo y sombra horneados en su tinte. Lo usa <c>LluviaOverlay</c>.
    /// </summary>
    /// <remarks>
    /// <para>Color (direccion-visual-v2 §2.4, regla 1): las superficies de la nube quedan a ΔE00 ≥ 10 de los 14 tintes
    /// (<see cref="Pearl"/> 14,9; <see cref="LluviaCloudLit"/> 11,8; <see cref="LluviaCloudBelly"/> 19,0; con la tormenta,
    /// multiplicadas por <see cref="LluviaStormTint"/>, aún más lejos). El filo de luz mide menos de 2 dp. La gota lleva el agua
    /// del mundo a propósito (excepción de la regla).</para>
    /// <para>Perezosos: se generan la primera vez que se piden; la gota, una por tinte de agua (como mucho dos en la campaña:
    /// Tapajós y Río Negro). Tras <see cref="Dispose"/> se vuelven a generar.</para>
    /// </remarks>
    public sealed partial class ProceduralArt
    {
        /// <summary>Ancho del sprite de la nube en u (a su alto de <see cref="LluviaCloudHeightU"/>).</summary>
        public const float LluviaCloudWidthU = 2.35f;

        /// <summary>Alto del sprite de la nube en u.</summary>
        public const float LluviaCloudHeightU = 1f;

        /// <summary>Proporción alto / ancho del sprite de la gota.</summary>
        public const float LluviaDropAspect = 1.25f;

        /// <summary>Cara iluminada de los lóbulos de la nube #C4CCD3 [P]: ΔE00 11,8 de Guanábana.</summary>
        public static readonly Color LluviaCloudLit = Palette.Hex(0xC4CCD3);

        /// <summary>Panza en sombra de la nube #8C96A3 [P]: ΔE00 19,0 de Agua del Tapajós.</summary>
        public static readonly Color LluviaCloudBelly = Palette.Hex(0x8C96A3);

        /// <summary>
        /// Multiplicador de la nube cargada (la gota espera: la cuia está llena) [P]: perla #B8BEC6 → gris tormenta ≈ #7A8794.
        /// </summary>
        public static readonly Color LluviaStormTint = new Color(0.66f, 0.71f, 0.75f, 1f);

        // Lóbulos de la nube: (x en fracción del ancho, y y radio en fracción del alto), más una base redondeada.
        private static readonly float[] LluviaCloudLobes =
        {
            0.25f, 0.44f, 0.27f,
            0.47f, 0.57f, 0.36f,
            0.71f, 0.47f, 0.29f,
            0.36f, 0.36f, 0.24f,
            0.83f, 0.30f, 0.19f,
        };

        private Sprite? _lluviaCloud;
        private Sprite?[]? _lluviaDrops;

        /// <summary>
        /// Nube con volumen (2,35 × 1 u a 96 px/u), con sus colores horneados: se usa con <c>Image.color</c> blanco y, cargada,
        /// con <see cref="LluviaStormTint"/>.
        /// </summary>
        internal Sprite LluviaCloudSprite => _lluviaCloud != null ? _lluviaCloud : (_lluviaCloud = BuildLluviaCloud());

        /// <summary>
        /// Gota de vidrio del tinte <paramref name="key"/> (agua del mundo; 0,4 × 0,5 u), con contorno, sombra y brillo blancos
        /// horneados: se usa con <c>Image.color</c> blanco. Una por tinte, la primera vez que se pide.
        /// </summary>
        internal Sprite LluviaDropSprite(int key)
        {
            if (key < 1 || key > Cuia.MaxTintColor)
            {
                key = 10;
            }

            _lluviaDrops ??= new Sprite?[Cuia.MaxTintColor + 1];
            Sprite? drop = _lluviaDrops[key];
            if (drop == null)
            {
                drop = BuildLluviaDrop(key);
                _lluviaDrops[key] = drop;
            }

            return drop;
        }

        private Sprite BuildLluviaCloud()
        {
            int w = Mathf.RoundToInt(LluviaCloudWidthU * MechanicPxPerU);
            int h = Mathf.RoundToInt(LluviaCloudHeightU * MechanicPxPerU);
            const float k = MechanicPxPerU / 96f;
            float outline = 5f * k; // ≈ 1,2 dp con la cuia de 48 dp
            float rim = 5f * k; // filo de luz (< 2 dp)
            float bottom = h * 0.07f;
            float top = h * 0.93f;
            Color rimLight = ColorMath.Lighten(LluviaCloudLit, 0.55f);
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float d = LluviaCloudDistance(px, py, w, h);
                    if (d > 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, Slate, PixelCanvas.Coverage(d));
                    float inside = PixelCanvas.Coverage(d + outline);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    // Degradado vertical: panza en sombra (30 % de abajo), cuerpo perla y cara iluminada arriba.
                    float t = Mathf.Clamp01((py - bottom) / (top - bottom));
                    Color c = t < 0.32f
                        ? Color.Lerp(LluviaCloudBelly, Pearl, Mathf.SmoothStep(0f, 1f, t / 0.32f))
                        : Color.Lerp(Pearl, LluviaCloudLit, Mathf.SmoothStep(0f, 1f, (t - 0.32f) / 0.68f));

                    // Sombra entre lóbulos: lo que está justo debajo del borde de otro lóbulo se oscurece un poco.
                    float under = LluviaCloudDistance(px, py + (7f * k), w, h);
                    if (under + outline > 0f && t > 0.25f)
                    {
                        c = Color.Lerp(c, LluviaCloudBelly, 0.18f);
                    }

                    // Filo de luz en lo alto de cada lóbulo: subir rim px saca el punto de la nube.
                    float above = LluviaCloudDistance(px - (2f * k), py + rim, w, h);
                    float edge = Mathf.Clamp01((above + outline) / rim);
                    if (edge > 0f && t > 0.45f)
                    {
                        c = Color.Lerp(c, rimLight, 0.75f * edge);
                    }

                    pc.Blend(x, y, c, inside);
                }
            }

            return MakeSprite(pc, "mech_lluvia_cloud", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Distancia con signo (px; negativa dentro) a la silueta de la nube: la base redondeada y los lóbulos.
        private static float LluviaCloudDistance(float px, float py, float w, float h)
        {
            float d = PixelCanvas.RoundedRectDistance(px, py, w * 0.07f, h * 0.07f, w * 0.93f, h * 0.46f, h * 0.2f);
            float[] lobes = LluviaCloudLobes;
            for (int i = 0; i < lobes.Length; i += 3)
            {
                float dx = px - (lobes[i] * w);
                float dy = py - (lobes[i + 1] * h);
                d = Mathf.Min(d, Mathf.Sqrt((dx * dx) + (dy * dy)) - (lobes[i + 2] * h));
            }

            return d;
        }

        private Sprite BuildLluviaDrop(int key)
        {
            const int w = 48;
            const int h = 60;
            Color tint = Palette.Tint(key);
            Color edge = ColorMath.Darken(tint, 0.45f);
            Color deep = ColorMath.Darken(tint, 0.18f);
            Color light = ColorMath.Lighten(tint, 0.35f);
            float cx = w * 0.5f;
            const float cy = 21f;
            const float r = 16f;
            const float tip = 57f;
            const float outline = 2.6f;
            var pc = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float d = LluviaDropDistance(px, py, cx, cy, r, tip);
                    if (d > 1f)
                    {
                        continue;
                    }

                    pc.Blend(x, y, edge, PixelCanvas.Coverage(d));
                    float inside = PixelCanvas.Coverage(d + outline);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    // Volumen: luz arriba a la izquierda y fondo más hondo abajo a la derecha.
                    float sx = (px - cx) / r;
                    float sy = (py - cy) / r;
                    float s = Mathf.Clamp((0.55f * sx) - (0.45f * sy), -1f, 1f);
                    Color c = s >= 0f ? Color.Lerp(tint, deep, s) : Color.Lerp(tint, light, -0.6f * s);
                    pc.Blend(x, y, c, inside);
                }
            }

            // Brillo de vidrio arriba a la izquierda, chispa y reflejo bajo.
            pc.FillEllipseSoft(cx - 6.5f, cy + 5f, 4.2f, 6.8f, 1.2f, new Color(1f, 1f, 1f, 0.92f));
            pc.FillCircle(cx - 3.5f, cy + 15f, 1.8f, new Color(1f, 1f, 1f, 0.85f));
            pc.StrokeArc(cx, cy, r - 5f, 2.2f, 285f, 345f, new Color(1f, 1f, 1f, 0.38f));
            return MakeSprite(pc, "mech_lluvia_drop", new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        // Distancia con signo (px) a una gota con el círculo en (cx, cy) de radio r y la punta hacia arriba en tip: el círculo
        // unido al cono de sus tangentes desde la punta.
        private static float LluviaDropDistance(float px, float py, float cx, float cy, float r, float tip)
        {
            float dx = px - cx;
            float dy = py - cy;
            float circle = Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
            float height = tip - cy;
            float sin = r / height; // seno del semiángulo del cono
            float cos = Mathf.Sqrt(Mathf.Max(0f, 1f - (sin * sin)));
            float ax = Mathf.Abs(dx);
            float ay = tip - py; // distancia bajo la punta
            float cone = (ax * cos) - (ay * sin); // distancia al lado del cono (positiva fuera)
            float tangentY = cy + (r * sin); // altura de las tangentes: por debajo manda el círculo
            if (py < tangentY || ay < 0f)
            {
                return ay < 0f ? Mathf.Min(circle, Mathf.Sqrt((ax * ax) + (ay * ay))) : circle;
            }

            return Mathf.Min(circle, cone);
        }
    }
}
