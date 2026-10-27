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

        // Las mismas luces y colores como constantes para ShadeCore, que se llama por píxel (sin leer campos estáticos):
        // KeyLight, el semivector Half, RimFrom, RimWarm y BounceCool (los comprueba ToyShadingTests).
        internal const float KeyX = -0.47942504f;
        internal const float KeyY = 0.6592095f;
        internal const float KeyZ = 0.57930523f;
        internal const float HalfX = -0.26975697f;
        internal const float HalfY = 0.37091586f;
        internal const float HalfZ = 0.88862395f;
        internal const float RimX = 0.72198826f;
        internal const float RimY = 0.6919054f;
        internal const float RimWarmR = 1f;
        internal const float RimWarmG = 0.8901961f;
        internal const float RimWarmB = 0.7019608f;
        internal const float BounceR = 0.5529412f;
        internal const float BounceG = 0.7137255f;
        internal const float BounceB = 0.76862746f;

        // Semivector de la luz principal y la vista (0, 0, 1), para el brillo especular.
        internal static readonly Vector3 Half = (KeyLight + new Vector3(0f, 0f, 1f)).normalized;

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
            k = Mathf.Max(1e-5f, k);
            float h = Mathf.Clamp01(0.5f + (0.5f * (b - a) / k));
            float w = 4f * h * (1f - h);
            return w * Mathf.Clamp01(1f - (Mathf.Abs(Mathf.Max(a, b)) / k));
        }

        /// <summary>Sombra de contacto (0..1) de una elipse de semiejes (rx, ry) a (dx, dy) de su centro: núcleo y caída suave.</summary>
        public static float ContactShadow(float dx, float dy, float rx, float ry)
        {
            rx = Mathf.Max(1e-5f, rx);
            ry = Mathf.Max(1e-5f, ry);
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
            ShadeCore(t, n.x, n.y, n.z, ao, gloss, glossNormal.x, glossNormal.y, glossNormal.z, out float r, out float g, out float b);
            return new Color(r, g, b, 1f);
        }

        /// <summary>
        /// El cálculo de <see cref="Shade(in ToyTones, Vector3, float, float, Vector3)"/> en floats, sin llamadas por píxel (se
        /// llama decenas de miles de veces por atlas; en el Editor cada llamada pequeña cuenta): los mismos tres tonos, rebote,
        /// luz de borde, oclusión y barniz, con el mismo resultado.
        /// </summary>
        public static void ShadeCore(in ToyTones t, float nx, float ny, float nz, float ao, float gloss, float gx, float gy, float gz, out float r, out float g, out float b)
        {
            // Difusa envolvente (Diffuse), apagada por la oclusión.
            float l = (nx * KeyX) + (ny * KeyY) + (nz * KeyZ);
            float d = (l + Wrap) / (1f + Wrap);
            d = d < 0f ? 0f : (d > 1f ? 1f : d);
            d *= 1f - (0.35f * ao);

            // Los tres tonos (ThreeTone).
            if (d < 0.45f)
            {
                float u = (d - 0.12f) / 0.33f;
                u = u < 0f ? 0f : (u > 1f ? 1f : u);
                u = u * u * (3f - (2f * u));
                r = t.Shadow.r + ((t.Mid.r - t.Shadow.r) * u);
                g = t.Shadow.g + ((t.Mid.g - t.Shadow.g) * u);
                b = t.Shadow.b + ((t.Mid.b - t.Shadow.b) * u);
            }
            else
            {
                float u = (d - 0.62f) / 0.33f;
                u = u < 0f ? 0f : (u > 1f ? 1f : u);
                u = u * u * (3f - (2f * u));
                r = t.Mid.r + ((t.Light.r - t.Mid.r) * u);
                g = t.Mid.g + ((t.Light.g - t.Mid.g) * u);
                b = t.Mid.b + ((t.Light.b - t.Mid.b) * u);
            }

            // Rebote frío desde abajo, en el lado de la sombra.
            float down = -ny;
            down = down < 0f ? 0f : (down > 1f ? 1f : down);
            float bounce = down * (1f - d) * 0.2f;
            if (bounce > 0f)
            {
                r += (BounceR - r) * bounce;
                g += (BounceG - g) * bounce;
                b += (BounceB - b) * bounce;
            }

            // Luz de borde cálida (Rim), hacia el tono de luz mezclado con RimWarm.
            float len2 = (nx * nx) + (ny * ny);
            if (len2 > 1e-10f && ao < 1f)
            {
                float len = Mathf.Sqrt(len2);
                float facing = ((nx * RimX) + (ny * RimY)) / len;
                float fz = nz < 0f ? 0f : (nz > 1f ? 1f : nz);
                float f = 1f - fz;
                float side = 0.3f + (0.7f * facing);
                side = side < 0f ? 0f : (side > 1f ? 1f : side);
                float rim = f * f * f * side * 0.55f * (1f - ao);
                if (rim > 0f)
                {
                    float wr = t.Light.r + ((RimWarmR - t.Light.r) * 0.55f);
                    float wg = t.Light.g + ((RimWarmG - t.Light.g) * 0.55f);
                    float wb = t.Light.b + ((RimWarmB - t.Light.b) * 0.55f);
                    r += (wr - r) * rim;
                    g += (wg - g) * rim;
                    b += (wb - b) * rim;
                }
            }

            // Oclusión.
            if (ao > 0f)
            {
                float dark = 1f - (0.5f * ao);
                r *= dark;
                g *= dark;
                b *= dark;
            }

            // Barniz: el brillo nítido (exponente 48, con el canto definido) y el ancho (exponente 6), hacia el blanco.
            if (gloss > 0f && ao < 1f)
            {
                float h = (gx * HalfX) + (gy * HalfY) + (gz * HalfZ);
                if (h > 0f)
                {
                    float h2 = h * h;
                    float d6 = h2 * h2 * h2;
                    float d12 = d6 * d6;
                    float d24 = d12 * d12;
                    float sharp = ((d24 * d24) - 0.55f) / 0.25f;
                    sharp = sharp < 0f ? 0f : (sharp > 1f ? 1f : sharp);
                    sharp = sharp * sharp * (3f - (2f * sharp));
                    float sp = gloss * ((0.85f * sharp) + (0.6f * 0.5f * d6)) * (1f - ao);
                    sp = sp < 0f ? 0f : (sp > 1f ? 1f : sp);
                    r += (1f - r) * sp;
                    g += (1f - g) * sp;
                    b += (1f - b) * sp;
                }
            }
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

    /// <summary>
    /// Tabla del sombreado de juguete de un material (D-058): <see cref="ToyShading.ShadeCore"/> sin oclusión, precalculado en una
    /// rejilla de (<see cref="Size"/> + 1)² normales aparentes (nx, ny en −<see cref="Reach"/>..<see cref="Reach"/>) e interpolado.
    /// Las piezas de juguete del arte por código (los miquitos, los avatares) lo leen por píxel en lugar de recalcularlo: en el
    /// Editor, la cuenta completa cuesta unos 120 ns por píxel y la tabla, una fracción. Se cachea por hilo (<see cref="For"/>).
    /// </summary>
    internal sealed class ToyLut
    {
        /// <summary>Celdas por eje.</summary>
        public const int Size = 32;

        /// <summary>Alcance de la normal aparente en cada eje (fuera, se recorta: el canto).</summary>
        public const float Reach = 1.1f;

        private const int Slots = 24;
        private const float Step = 2f * Reach / Size;
        private const float InvStep = Size / (2f * Reach);

        [System.ThreadStatic]
        private static ToyLut[]? s_cache;

        [System.ThreadStatic]
        private static int s_next;

        private readonly float[] _rgb = new float[(Size + 1) * (Size + 1) * 3];
        private ToyTones _tones;
        private float _gloss = -1f;

        /// <summary>Píxeles a partir de los que compensa crear la tabla (crearla cuesta unas 1.100 cuentas completas).</summary>
        public const int MinPixels = 1800;

        /// <summary>
        /// La tabla del material <paramref name="t"/> con barniz <paramref name="gloss"/> para una pieza de unos
        /// <paramref name="pixels"/> píxeles: la de la caché de este hilo si ya está; si no, la crea solo si la pieza es grande (si
        /// no, null: la pieza se sombrea píxel a píxel con <see cref="ToyShading.ShadeCore"/>).
        /// </summary>
        public static ToyLut? For(in ToyTones t, float gloss, float pixels)
        {
            ToyLut[] cache = s_cache ?? NewCache();
            for (int i = 0; i < cache.Length; i++)
            {
                ToyLut l = cache[i];
                if (l.Matches(t, gloss))
                {
                    return l;
                }
            }

            if (pixels < MinPixels)
            {
                return null;
            }

            ToyLut lut = cache[s_next];
            s_next = (s_next + 1) % Slots;
            lut.Build(t, gloss);
            return lut;
        }

        // La caché de este hilo, entera de una vez: después, pintar no asigna memoria (el atlas de avatares lo comprueba).
        private static ToyLut[] NewCache()
        {
            var cache = new ToyLut[Slots];
            for (int i = 0; i < Slots; i++)
            {
                cache[i] = new ToyLut();
            }

            s_cache = cache;
            return cache;
        }

        /// <summary>El color (opaco) de la normal aparente (nx, ny), interpolado en la tabla.</summary>
        public void Sample(float nx, float ny, out float r, out float g, out float b)
        {
            float fx = (nx + Reach) * InvStep;
            float fy = (ny + Reach) * InvStep;
            fx = fx < 0f ? 0f : (fx > Size - 0.0001f ? Size - 0.0001f : fx);
            fy = fy < 0f ? 0f : (fy > Size - 0.0001f ? Size - 0.0001f : fy);
            int ix = (int)fx;
            int iy = (int)fy;
            float tx = fx - ix;
            float ty = fy - iy;
            int i00 = ((iy * (Size + 1)) + ix) * 3;
            int i10 = i00 + 3;
            int i01 = i00 + ((Size + 1) * 3);
            int i11 = i01 + 3;
            float[] v = _rgb;
            float w00 = (1f - tx) * (1f - ty);
            float w10 = tx * (1f - ty);
            float w01 = (1f - tx) * ty;
            float w11 = tx * ty;
            r = (v[i00] * w00) + (v[i10] * w10) + (v[i01] * w01) + (v[i11] * w11);
            g = (v[i00 + 1] * w00) + (v[i10 + 1] * w10) + (v[i01 + 1] * w01) + (v[i11 + 1] * w11);
            b = (v[i00 + 2] * w00) + (v[i10 + 2] * w10) + (v[i01 + 2] * w01) + (v[i11 + 2] * w11);
        }

        private bool Matches(in ToyTones t, float gloss)
        {
            return gloss == _gloss && t.Mid == _tones.Mid && t.Shadow == _tones.Shadow && t.Light == _tones.Light;
        }

        private void Build(in ToyTones t, float gloss)
        {
            _tones = t;
            _gloss = gloss;
            int k = 0;
            for (int j = 0; j <= Size; j++)
            {
                float ny = -Reach + (j * Step);
                for (int i = 0; i <= Size; i++)
                {
                    Vector3 n = ToyShading.Normal(-Reach + (i * Step), ny);
                    ToyShading.ShadeCore(t, n.x, n.y, n.z, 0f, gloss, n.x, n.y, n.z, out float r, out float g, out float b);
                    _rgb[k] = r;
                    _rgb[k + 1] = g;
                    _rgb[k + 2] = b;
                    k += 3;
                }
            }
        }
    }

    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Elipse de juguete (D-058; guía §2-§3) de semiejes (<paramref name="rx"/>, <paramref name="ry"/>) px centrada en (cx, cy):
        /// relleno con el volumen de una cúpula (<see cref="ToyShading.Shade(in ToyTones, Vector3, float, float)"/>), el barniz de
        /// intensidad <paramref name="gloss"/>, el borde fino coloreado de <paramref name="outlinePx"/> px (0, sin borde) y, con
        /// <paramref name="aoPx"/> &gt; 0, la oclusión que la pieza deja sobre lo que ya estaba pintado a su alrededor (las
        /// uniones). Una sola pasada: el borde, la oclusión y el relleno salen del mismo recorrido.
        /// </summary>
        internal static void ToyEllipse(PixelCanvas pc, float cx, float cy, float rx, float ry, in ToyTones t, float gloss, float outlinePx, float aoPx = 0f)
        {
            ToyEllipseOn(pc, cx, cy, rx, ry, t, gloss, outlinePx, aoPx, cx, cy, rx * ToyDomeSpread, ry * ToyDomeSpread, 0f);
        }

        /// <summary>
        /// Como <see cref="ToyEllipse"/>, con la normal de otra cúpula, la de la pieza sobre la que va (centro (domeCx, domeCy) y
        /// semiejes (domeRx, domeRy)): los parches de color (la cara, el hocico, la panza, un babero) siguen el volumen de la
        /// cabeza o del cuerpo en lugar de abombarse aparte. <paramref name="soft"/> &gt; 0 difumina el canto del parche (px).
        /// </summary>
        internal static void ToyEllipseOn(PixelCanvas pc, float cx, float cy, float rx, float ry, in ToyTones t, float gloss, float outlinePx, float aoPx, float domeCx, float domeCy, float domeRx, float domeRy, float soft)
        {
            rx = Mathf.Max(0.25f, rx);
            ry = Mathf.Max(0.25f, ry);
            float reach = outlinePx + aoPx + soft + 2f;
            int x0 = Mathf.FloorToInt(cx - rx - reach);
            int x1 = Mathf.CeilToInt(cx + rx + reach);
            int y0 = Mathf.FloorToInt(cy - ry - reach);
            int y1 = Mathf.CeilToInt(cy + ry + reach);
            x0 = Mathf.Max(0, x0);
            y0 = Mathf.Max(0, y0);
            x1 = Mathf.Min(pc.Width - 1, x1);
            y1 = Mathf.Min(pc.Height - 1, y1);
            float r = Mathf.Min(rx, ry);
            float invRx = 1f / rx;
            float invRy = 1f / ry;
            ToyLut? lut = ToyLut.For(t, gloss, 4f * rx * ry);
            float invDx = 1f / Mathf.Max(1e-3f, domeRx);
            float invDy = 1f / Mathf.Max(1e-3f, domeRy);
            for (int y = y0; y <= y1; y++)
            {
                float py = (y + 0.5f) - cy;
                float qy = py * invRy;
                float ny = ((y + 0.5f) - domeCy) * invDy;
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - cx;
                    float qx = px * invRx;
                    float d = (Mathf.Sqrt((qx * qx) + (qy * qy)) - 1f) * r;
                    if (d > outlinePx + aoPx + 1f)
                    {
                        continue;
                    }

                    float nx = ((x + 0.5f) - domeCx) * invDx;
                    ToyPixel(pc, x, y, d, t, gloss, lut, nx, ny, outlinePx, aoPx, soft);
                }
            }
        }

        /// <summary>
        /// Cápsula de juguete de (ax, ay) a (bx, by) y radio <paramref name="radius"/> px: un cilindro con las puntas en cúpula, con
        /// el borde fino coloreado y la oclusión de alrededor como en <see cref="ToyEllipse"/>.
        /// </summary>
        internal static void ToyCapsule(PixelCanvas pc, float ax, float ay, float bx, float by, float radius, in ToyTones t, float gloss, float outlinePx, float aoPx = 0f)
        {
            radius = Mathf.Max(0.25f, radius);
            float reach = radius + outlinePx + aoPx + 2f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ax, bx) - reach));
            int x1 = Mathf.Min(pc.Width - 1, Mathf.CeilToInt(Mathf.Max(ax, bx) + reach));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ay, by) - reach));
            int y1 = Mathf.Min(pc.Height - 1, Mathf.CeilToInt(Mathf.Max(ay, by) + reach));
            float vx = bx - ax;
            float vy = by - ay;
            float invLen2 = 1f / Mathf.Max(1e-6f, (vx * vx) + (vy * vy));
            float invDome = 1f / (radius * ToyDomeSpread);
            ToyLut? lut = ToyLut.For(t, gloss, 2f * radius * (Mathf.Sqrt((vx * vx) + (vy * vy)) + (2f * radius)));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - ax;
                    float py = (y + 0.5f) - ay;
                    float s = Mathf.Clamp01(((px * vx) + (py * vy)) * invLen2);
                    float dx = px - (vx * s);
                    float dy = py - (vy * s);
                    float d = Mathf.Sqrt((dx * dx) + (dy * dy)) - radius;
                    if (d > outlinePx + aoPx + 1f)
                    {
                        continue;
                    }

                    ToyPixel(pc, x, y, d, t, gloss, lut, dx * invDome, dy * invDome, outlinePx, aoPx, 0f);
                }
            }
        }

        // La cúpula de una pieza llega algo más allá de su canto: el borde mira casi de lado, sin llegar al negro.
        private const float ToyDomeSpread = 1.06f;

        // El color de juguete de la normal aparente (nx, ny): de la tabla si la hay; si no, la cuenta completa.
        internal static Color ToyColor(in ToyTones t, float gloss, ToyLut? lut, float nx, float ny)
        {
            float r;
            float g;
            float b;
            if (lut != null)
            {
                lut.Sample(nx, ny, out r, out g, out b);
            }
            else
            {
                Vector3 n = ToyShading.Normal(nx, ny);
                ToyShading.ShadeCore(t, n.x, n.y, n.z, 0f, gloss, n.x, n.y, n.z, out r, out g, out b);
            }

            return new Color(r, g, b, 1f);
        }

        // Un píxel a la distancia d (px, negativa dentro) de una pieza de juguete con la normal aparente (nx, ny) de su cúpula: la
        // oclusión de alrededor, el borde y el relleno (la normal se completa solo si hay relleno).
        private static void ToyPixel(PixelCanvas pc, int x, int y, float d, in ToyTones t, float gloss, ToyLut? lut, float nx, float ny, float outlinePx, float aoPx, float soft)
        {
            if (aoPx > 0f && d > outlinePx)
            {
                float u = 1f - Mathf.Clamp01((d - outlinePx) / aoPx);
                pc.Multiply(x, y, 1f - (0.32f * u * u), 1f);
                return;
            }

            if (outlinePx > 0f)
            {
                float co = PixelCanvas.Coverage(d - outlinePx);
                if (co > 0f && d > -0.5f)
                {
                    pc.Blend(x, y, t.Outline, co);
                }
            }

            float cov = soft > 0f ? PixelCanvas.SoftCoverage(d, soft) : PixelCanvas.Coverage(d);
            if (cov <= 0f)
            {
                return;
            }

            Color c = ToyColor(t, gloss, lut, nx, ny);
            if (cov >= 1f)
            {
                pc.Set(x, y, c);
            }
            else
            {
                pc.Blend(x, y, c, cov);
            }
        }

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
