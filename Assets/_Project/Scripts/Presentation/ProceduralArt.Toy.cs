#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Los cuatro tonos de un material de juguete (D-058; guía <c>docs/art/estilo-personajes-3d.md</c> §2): sombra fría, medio,
    /// luz cálida y el contorno fino del mismo color. Se calculan una vez por pieza (<see cref="ToyShading.Tones"/>), nunca por
    /// píxel.
    /// </summary>
    public struct ToyTones
    {
        /// <summary>Tono de la sombra propia: más oscuro, más saturado y algo frío.</summary>
        public Color Shadow;

        /// <summary>El color base del material.</summary>
        public Color Mid;

        /// <summary>Tono de la luz: más claro y algo cálido.</summary>
        public Color Light;

        /// <summary>El contorno fino coloreado (el color base muy oscuro y saturado), con su alfa.</summary>
        public Color Outline;

        /// <summary>Mezcla de dos juegos de tonos (para los parches de color dentro de una pieza, sin pasar a OKLab por píxel).</summary>
        public static ToyTones Lerp(in ToyTones a, in ToyTones b, float t)
        {
            return new ToyTones
            {
                Shadow = Color.Lerp(a.Shadow, b.Shadow, t),
                Mid = Color.Lerp(a.Mid, b.Mid, t),
                Light = Color.Lerp(a.Light, b.Light, t),
                Outline = Color.Lerp(a.Outline, b.Outline, t),
            };
        }
    }

    /// <summary>
    /// Sombreado de «juguete brillante» en pseudo-3D (D-058): funciones puras sobre las SDF del arte por código. La pieza da una
    /// normal aparente (de cúpula, de cilindro o de borde) y aquí salen la luz difusa en tres tonos, el barniz con su doble
    /// brillo (nítido y ancho), la luz de borde cálida desde atrás, el rebote frío de la selva, la oclusión ambiental de las
    /// uniones, la sombra de contacto, los mechones de felpa y el iris en degradado. La luz es la misma para todos los
    /// personajes: la principal arriba a la izquierda y algo de frente (la luz de juego de D-003).
    /// </summary>
    /// <remarks>
    /// Sin estado, sin asignaciones y deterministas (mismo resultado en cualquier hilo): las usan los atlas que se pintan en
    /// paralelo. Las normales van en el espacio de la vista: x a la derecha, y arriba y z hacia quien mira.
    /// </remarks>
    public static class ToyShading
    {
        /// <summary>Luz principal: arriba a la izquierda y algo de frente (unitaria).</summary>
        public static readonly Vector3 KeyLight = new Vector3(-0.48f, 0.66f, 0.58f).normalized;

        /// <summary>De dónde viene la luz de borde, en el plano (unitaria): por detrás, arriba a la derecha.</summary>
        public static readonly Vector2 RimFrom = new Vector2(0.72f, 0.69f).normalized;

        /// <summary>Luz de borde cálida.</summary>
        public static readonly Color RimWarm = Palette.Hex(0xFFE3B3);

        /// <summary>Rebote frío de la selva y el río en el lado de la sombra.</summary>
        public static readonly Color BounceCool = Palette.Hex(0x8DB6C4);

        /// <summary>Hacia dónde tira la luz (mezcla de tono en OKLab).</summary>
        public static readonly Color WarmTint = Palette.Hex(0xFFE9C2);

        /// <summary>Hacia dónde tira la sombra (mezcla de tono en OKLab).</summary>
        public static readonly Color CoolTint = Palette.Hex(0x3A3D6E);

        /// <summary>Alfa del contorno fino coloreado.</summary>
        public const float OutlineAlpha = 0.92f;

        // Luz envolvente: la sombra no llega al negro en el terminador (vinilo, no metal).
        private const float Wrap = 0.3f;

        // Semivector de la luz principal y la vista (0, 0, 1), para el brillo especular.
        private static readonly Vector3 Half = (KeyLight + new Vector3(0f, 0f, 1f)).normalized;

        /// <summary>
        /// Los tonos de un material de base <paramref name="c"/>: la sombra al 66 % de luminosidad OKLab con un 20 % más de croma
        /// y tirando al frío; la luz al 116 % con un 12 % más de croma y tirando al cálido; el contorno al 42 %, más saturado
        /// (<paramref name="contrast"/> escala la distancia de la sombra y la luz al medio).
        /// </summary>
        public static ToyTones Tones(Color c, float contrast = 1f)
        {
            c.a = 1f;
            Color light = ColorMath.OkLabLerp(ColorMath.ScaleOkLightnessChroma(c, 1f + (0.16f * contrast), 1.12f), WarmTint, 0.1f);
            Color shadow = ColorMath.OkLabLerp(ColorMath.ScaleOkLightnessChroma(c, 1f - (0.34f * contrast), 1.2f), CoolTint, 0.14f);
            Color outline = ColorMath.ScaleOkLightnessChroma(c, 0.42f, 1.25f);
            outline.a = OutlineAlpha;
            return new ToyTones { Shadow = shadow, Mid = c, Light = light, Outline = outline };
        }

        /// <summary>Normal de una cúpula a partir de su normal aparente (nx, ny en −1..1; fuera del disco, en el borde).</summary>
        public static Vector3 Normal(float nx, float ny)
        {
            float nn = (nx * nx) + (ny * ny);
            if (nn >= 1f)
            {
                float inv = 1f / Mathf.Sqrt(nn);
                return new Vector3(nx * inv, ny * inv, 0f);
            }

            return new Vector3(nx, ny, Mathf.Sqrt(1f - nn));
        }

        /// <summary>
        /// Normal de una forma cualquiera redondeada como una cúpula de radio <paramref name="radius"/> desde su borde: a
        /// <paramref name="inside"/> dentro del borde (mismas unidades), con la normal del borde <paramref name="edge"/> (hacia
        /// fuera, unitaria). En el borde mira de lado; a un radio o más dentro, de frente.
        /// </summary>
        public static Vector3 EdgeDome(Vector2 edge, float inside, float radius)
        {
            float t = Mathf.Clamp01(inside / Mathf.Max(1e-5f, radius));
            float m = 1f - t;
            return new Vector3(edge.x * m, edge.y * m, Mathf.Sqrt(1f - (m * m)));
        }

        /// <summary>
        /// Normal de un cilindro que gira con su pieza (las extremidades del rig): <paramref name="across"/> (−1..1) a lo ancho, con
        /// el centro mirando a la luz principal, así que la luz no tiene lado y el cilindro se lee igual en cualquier pose.
        /// </summary>
        public static Vector3 FacingLight(float across)
        {
            across = Mathf.Clamp(across, -1f, 1f);
            float z = Mathf.Sqrt(1f - (across * across));
            Vector3 side = new Vector3(KeyLight.z, 0f, -KeyLight.x).normalized;
            return (KeyLight * z) + (side * across);
        }

        /// <summary>Inclina la normal con la pendiente (sx, sy) de un relieve (mechones, pliegues) por <paramref name="amount"/>.</summary>
        public static Vector3 Tilt(Vector3 n, float sx, float sy, float amount)
        {
            var t = new Vector3(n.x - (sx * amount), n.y - (sy * amount), n.z);
            float len = Mathf.Sqrt((t.x * t.x) + (t.y * t.y) + (t.z * t.z));
            return len > 1e-6f ? t / len : n;
        }

        /// <summary>Luz difusa envolvente de la luz principal (0 de espaldas a la luz, 1 de frente).</summary>
        public static float Diffuse(Vector3 n)
        {
            float l = (n.x * KeyLight.x) + (n.y * KeyLight.y) + (n.z * KeyLight.z);
            return Mathf.Clamp01((l + Wrap) / (1f + Wrap));
        }

        /// <summary>
        /// Los tres tonos según la luz difusa: sombra hasta 0,12, medio de 0,45 a 0,62 y luz desde 0,95, con transiciones
        /// suaves (el «terminador» marcado del juguete, sin bandas duras).
        /// </summary>
        public static Color ThreeTone(in ToyTones t, float diffuse)
        {
            if (diffuse < 0.45f)
            {
                return Color.Lerp(t.Shadow, t.Mid, Smooth01((diffuse - 0.12f) / 0.33f));
            }

            return Color.Lerp(t.Mid, t.Light, Smooth01((diffuse - 0.62f) / 0.33f));
        }

        /// <summary>Brillo especular de Blinn-Phong de la luz principal (vista de frente) con el exponente <paramref name="power"/>.</summary>
        public static float Specular(Vector3 n, float power)
        {
            float d = (n.x * Half.x) + (n.y * Half.y) + (n.z * Half.z);
            return d <= 0f ? 0f : Mathf.Pow(d, power);
        }

        /// <summary>El brillo nítido y pequeño del barniz (0..1, con el canto definido).</summary>
        public static float VarnishSharp(Vector3 n)
        {
            float d6 = HalfPow6(n);
            float d12 = d6 * d6;
            float d24 = d12 * d12;
            return Smooth01(((d24 * d24) - 0.55f) / 0.25f); // exponente 48, sin Pow
        }

        /// <summary>El brillo ancho y suave del barniz (0..0,5).</summary>
        public static float VarnishBroad(Vector3 n)
        {
            return 0.5f * HalfPow6(n); // exponente 6, sin Pow
        }

        /// <summary>Fresnel: 0 de frente, 1 de canto.</summary>
        public static float Fresnel(Vector3 n, float power)
        {
            return Mathf.Pow(1f - Mathf.Clamp01(n.z), power);
        }

        /// <summary>Luz de borde (0..1): en los cantos, más en los que miran hacia <see cref="RimFrom"/>.</summary>
        public static float Rim(Vector3 n)
        {
            float len = Mathf.Sqrt((n.x * n.x) + (n.y * n.y));
            if (len < 1e-5f)
            {
                return 0f;
            }

            float facing = ((n.x * RimFrom.x) + (n.y * RimFrom.y)) / len;
            float f = 1f - Mathf.Clamp01(n.z);
            return f * f * f * Mathf.Clamp01(0.3f + (0.7f * facing)); // Fresnel(n, 3), sin Pow
        }

        /// <summary>
        /// Oclusión ambiental de la unión de dos piezas de distancias <paramref name="a"/> y <paramref name="b"/> (negativas
        /// dentro) que se funden con radio <paramref name="k"/>: 1 donde las dos superficies se cruzan, 0 lejos de la unión o
        /// lejos de los bordes. Es la diferencia normalizada entre min y smin (la «muesca» que rellena la unión suave).
        /// </summary>
        public static float CreaseAO(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + (0.5f * (b - a) / k));
            float w = 4f * h * (1f - h);
            return w * Mathf.Clamp01(1f - (Mathf.Abs(Mathf.Max(a, b)) / k));
        }

        /// <summary>Sombra de contacto (0..1) de una elipse de semiejes (rx, ry) a (dx, dy) de su centro: núcleo y caída suave.</summary>
        public static float ContactShadow(float dx, float dy, float rx, float ry)
        {
            float q = Mathf.Sqrt(((dx * dx) / (rx * rx)) + ((dy * dy) / (ry * ry)));
            return 1f - Smooth01((q - 0.15f) / 0.85f);
        }

        /// <summary>
        /// El color final de un punto del material <paramref name="t"/> con normal <paramref name="n"/>: los tres tonos, el
        /// rebote frío abajo en la sombra, la luz de borde cálida, la oclusión <paramref name="ao"/> (0..1) y el barniz con su
        /// doble brillo, de intensidad <paramref name="gloss"/> (0 mate, 1 lacado). Opaco.
        /// </summary>
        public static Color Shade(in ToyTones t, Vector3 n, float ao, float gloss)
        {
            return Shade(t, n, ao, gloss, n);
        }

        /// <summary>
        /// Como <see cref="Shade(in ToyTones, Vector3, float, float)"/>, con el barniz calculado sobre otra normal,
        /// <paramref name="glossNormal"/>: la de la forma sin el relieve de los mechones, para que el brillo de la capa de barniz
        /// sea una mancha limpia y no se rompa en chispas sobre la felpa.
        /// </summary>
        public static Color Shade(in ToyTones t, Vector3 n, float ao, float gloss, Vector3 glossNormal)
        {
            float d = Diffuse(n) * (1f - (0.35f * ao));
            Color c = ThreeTone(t, d);
            float bounce = Mathf.Clamp01(-n.y) * (1f - d) * 0.2f;
            c = Color.Lerp(c, BounceCool, bounce);
            float rim = Rim(n) * 0.55f * (1f - ao);
            c = Color.Lerp(c, Color.Lerp(t.Light, RimWarm, 0.55f), rim);
            float dark = 1f - (0.5f * ao);
            c.r *= dark;
            c.g *= dark;
            c.b *= dark;
            float s = Mathf.Clamp01(gloss * ((0.85f * VarnishSharp(glossNormal)) + (0.6f * VarnishBroad(glossNormal))) * (1f - ao));
            c.r += (1f - c.r) * s;
            c.g += (1f - c.g) * s;
            c.b += (1f - c.b) * s;
            c.a = 1f;
            return c;
        }

        /// <summary>
        /// Iris en degradado del ojo de juguete en (u, v) (en radios del ojo, y arriba): la pupila en el centro, el iris más
        /// oscuro arriba (la sombra del párpado) y más claro abajo a la derecha (la luz que entra), y el anillo oscuro del borde.
        /// </summary>
        public static Color Iris(float u, float v, Color iris, float pupil = 0.48f)
        {
            float r = Mathf.Sqrt((u * u) + (v * v));
            Color dark = ColorMath.Darken(iris, 0.55f);
            Color lit = ColorMath.Lighten(iris, 0.28f);
            Color c = Color.Lerp(dark, lit, Mathf.Clamp01(0.5f - (0.55f * v) + (0.2f * u)));
            c = Color.Lerp(c, ColorMath.Darken(iris, 0.65f), Smooth01((r - 0.8f) / 0.2f));
            c = Color.Lerp(c, ColorMath.Darken(iris, 0.85f), Smooth01((pupil + 0.04f - r) / 0.08f));
            c.a = 1f;
            return c;
        }

        /// <summary>
        /// Mechones de felpa estilizados en (x, y) (en unidades de <paramref name="size"/>, el ancho de un mechón): bultos redondos
        /// y suaves en una rejilla al tresbolillo, cada uno con su tamaño, que se suman donde se solapan. Devuelve la altura (0 en
        /// el surco, 1 en lo alto de un mechón) y su pendiente (sx, sy) por unidad de <paramref name="size"/>, para inclinar la
        /// normal.
        /// </summary>
        public static float PlushClumps(float x, float y, float size, int seed, out float sx, out float sy)
        {
            float px = x / size;
            float py = y / size;
            int row0 = Mathf.FloorToInt(py);
            float best = 0f;
            sx = 0f;
            sy = 0f;
            for (int dr = -1; dr <= 1; dr++)
            {
                int row = row0 + dr;
                float shift = (row & 1) == 0 ? 0f : 0.5f;
                int col0 = Mathf.FloorToInt(px - shift);
                for (int dc = -1; dc <= 1; dc++)
                {
                    int col = col0 + dc;
                    float cx = col + shift + 0.5f + (0.3f * (ArtNoise.Hash(col, row, seed) - 0.5f));
                    float cy = row + 0.5f + (0.3f * (ArtNoise.Hash(col, row, seed + 7) - 0.5f));
                    float rad = 0.62f + (0.22f * ArtNoise.Hash(col, row, seed + 13));
                    float dx = (px - cx) / rad;
                    float dy = (py - cy) / rad;
                    float q = 1f - ((dx * dx) + (dy * dy));
                    if (q <= 0f)
                    {
                        continue;
                    }

                    // Bulto suave (1 − r²)²: los vecinos se suman y se funden, sin aristas entre mechones (felpa, no escamas).
                    best += q * q;
                    sx += -4f * q * dx / rad;
                    sy += -4f * q * dy / rad;
                }
            }

            return Mathf.Clamp01(best);
        }

        // (n · semivector)⁶, el brillo especular de exponente 6 por multiplicaciones (se llama por píxel).
        private static float HalfPow6(Vector3 n)
        {
            float d = (n.x * Half.x) + (n.y * Half.y) + (n.z * Half.z);
            if (d <= 0f)
            {
                return 0f;
            }

            float d2 = d * d;
            return d2 * d2 * d2;
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - (2f * t));
        }
    }

    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Ojo de juguete (D-058) de radio <paramref name="r"/> px centrado en (cx, cy): iris en degradado
        /// (<see cref="ToyShading.Iris"/>) con un contorno fino algo más oscuro, el reflejo grande arriba a la izquierda (hacia la
        /// luz principal) y el pequeño abajo a la derecha.
        /// </summary>
        internal static void DrawToyEye(PixelCanvas pc, float cx, float cy, float r, Color iris)
        {
            int x0 = Mathf.FloorToInt(cx - r - 2f);
            int x1 = Mathf.CeilToInt(cx + r + 2f);
            int y0 = Mathf.FloorToInt(cy - r - 2f);
            int y1 = Mathf.CeilToInt(cy + r + 2f);
            Color rim = ColorMath.Darken(iris, 0.8f);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - r;
                    float cov = PixelCanvas.Coverage(d);
                    if (cov <= 0f)
                    {
                        continue;
                    }

                    Color c = ToyShading.Iris(dx / r, dy / r, iris);
                    c = Color.Lerp(c, rim, Mathf.Clamp01((d + Mathf.Max(0.8f, 0.1f * r)) / Mathf.Max(0.8f, 0.1f * r)));
                    c.a = 1f;
                    pc.Blend(x, y, c, cov);
                }
            }

            pc.FillEllipseSoft(cx - (0.36f * r), cy + (0.36f * r), 0.3f * r, 0.27f * r, Mathf.Max(0.6f, 0.06f * r), new Color(1f, 1f, 1f, 0.97f));
            pc.FillEllipseSoft(cx + (0.38f * r), cy - (0.34f * r), 0.13f * r, 0.12f * r, Mathf.Max(0.5f, 0.05f * r), new Color(1f, 1f, 1f, 0.8f));
        }
    }
}
