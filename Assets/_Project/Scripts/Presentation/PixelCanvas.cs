#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Lienzo de píxeles en CPU para los gráficos provisionales generados por código (sin assets binarios): primitivas
    /// con antialias por distancia con signo y composición "over" con alfa directo. Coordenadas en píxeles con el
    /// origen abajo a la izquierda (como <see cref="Texture2D"/>).
    /// </summary>
    internal sealed class PixelCanvas
    {
        // Sin píxeles tras Release: cualquier uso posterior falla en el acto (IndexOutOfRange) en vez de pintar sobre otro lienzo.
        private static readonly Color[] Released = new Color[0];

        // Búfer del lienzo: alquilado a PixelBuffers si es grande (memoria, 2026-10-26), así que puede ser más largo que el lienzo;
        // solo cuentan los _count primeros.
        private Color[] _px;
        private readonly int _count;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _count = width * height;
            _px = PixelBuffers.Rent(_count);
        }

        public int Width { get; }

        public int Height { get; }

        public Color Get(int x, int y)
        {
            return _px[(y * Width) + x];
        }

        public void Set(int x, int y, Color c)
        {
            _px[(y * Width) + x] = c;
        }

        /// <summary>Compone <paramref name="c"/> sobre el píxel con cobertura <paramref name="coverage"/> (0..1).</summary>
        public void Blend(int x, int y, Color c, float coverage)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            float a = c.a * Mathf.Clamp01(coverage);
            if (a <= 0f)
            {
                return;
            }

            int i = (y * Width) + x;
            Color d = _px[i];
            float outA = a + (d.a * (1f - a));
            if (outA <= 0f)
            {
                _px[i] = new Color(0f, 0f, 0f, 0f);
                return;
            }

            float k = d.a * (1f - a);
            _px[i] = new Color(
                ((c.r * a) + (d.r * k)) / outA,
                ((c.g * a) + (d.g * k)) / outA,
                ((c.b * a) + (d.b * k)) / outA,
                outA);
        }

        /// <summary>Cobertura de antialias a partir de una distancia con signo en píxeles (negativa dentro).</summary>
        public static float Coverage(float signedDistance)
        {
            return Mathf.Clamp01(0.5f - signedDistance);
        }

        public void FillCircle(float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1f);
            int x1 = Mathf.CeilToInt(cx + r + 1f);
            int y0 = Mathf.FloorToInt(cy - r - 1f);
            int y1 = Mathf.CeilToInt(cy + r + 1f);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    Blend(x, y, c, Coverage(Mathf.Sqrt((dx * dx) + (dy * dy)) - r));
                }
            }
        }

        /// <summary>Segmento con extremos redondeados (cápsula) de radio <paramref name="r"/>.</summary>
        public void FillCapsule(float ax, float ay, float bx, float by, float r, Color c)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(ax, bx) - r - 1f);
            int x1 = Mathf.CeilToInt(Mathf.Max(ax, bx) + r + 1f);
            int y0 = Mathf.FloorToInt(Mathf.Min(ay, by) - r - 1f);
            int y1 = Mathf.CeilToInt(Mathf.Max(ay, by) + r + 1f);
            float vx = bx - ax;
            float vy = by - ay;
            float len2 = Mathf.Max(1e-6f, (vx * vx) + (vy * vy));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) - ax;
                    float py = (y + 0.5f) - ay;
                    float t = Mathf.Clamp01(((px * vx) + (py * vy)) / len2);
                    float dx = px - (vx * t);
                    float dy = py - (vy * t);
                    Blend(x, y, c, Coverage(Mathf.Sqrt((dx * dx) + (dy * dy)) - r));
                }
            }
        }

        /// <summary>Rectángulo redondeado [x0, x1] × [y0, y1] con radio <paramref name="r"/>.</summary>
        public void FillRoundedRect(float x0, float y0, float x1, float y1, float r, Color c)
        {
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float hx = ((x1 - x0) * 0.5f) - r;
            float hy = ((y1 - y0) * 0.5f) - r;
            for (int y = Mathf.FloorToInt(y0) - 1; y <= Mathf.CeilToInt(y1); y++)
            {
                for (int x = Mathf.FloorToInt(x0) - 1; x <= Mathf.CeilToInt(x1); x++)
                {
                    float qx = Mathf.Abs((x + 0.5f) - cx) - hx;
                    float qy = Mathf.Abs((y + 0.5f) - cy) - hy;
                    float ox = Mathf.Max(qx, 0f);
                    float oy = Mathf.Max(qy, 0f);
                    float d = Mathf.Sqrt((ox * ox) + (oy * oy)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                    Blend(x, y, c, Coverage(d));
                }
            }
        }

        /// <summary>
        /// Arco de anillo centrado en (cx, cy), radio <paramref name="radius"/>, grosor <paramref name="thickness"/>,
        /// desde <paramref name="fromDeg"/> hasta <paramref name="toDeg"/> en sentido antihorario, con extremos redondeados.
        /// </summary>
        public void StrokeArc(float cx, float cy, float radius, float thickness, float fromDeg, float toDeg, Color c)
        {
            float half = thickness * 0.5f;
            float a0 = fromDeg * Mathf.Deg2Rad;
            float sweep = (toDeg - fromDeg) * Mathf.Deg2Rad;
            float ex0 = cx + (Mathf.Cos(a0) * radius);
            float ey0 = cy + (Mathf.Sin(a0) * radius);
            float ex1 = cx + (Mathf.Cos(a0 + sweep) * radius);
            float ey1 = cy + (Mathf.Sin(a0 + sweep) * radius);
            int r = Mathf.CeilToInt(radius + half + 1f);
            for (int y = Mathf.FloorToInt(cy) - r; y <= Mathf.CeilToInt(cy) + r; y++)
            {
                for (int x = Mathf.FloorToInt(cx) - r; x <= Mathf.CeilToInt(cx) + r; x++)
                {
                    float px = (x + 0.5f) - cx;
                    float py = (y + 0.5f) - cy;
                    float ang = Mathf.Atan2(py, px) - a0;
                    while (ang < 0f)
                    {
                        ang += Mathf.PI * 2f;
                    }

                    float d;
                    if (ang <= sweep)
                    {
                        d = Mathf.Abs(Mathf.Sqrt((px * px) + (py * py)) - radius) - half;
                    }
                    else
                    {
                        float d0 = Dist((x + 0.5f) - ex0, (y + 0.5f) - ey0);
                        float d1 = Dist((x + 0.5f) - ex1, (y + 0.5f) - ey1);
                        d = Mathf.Min(d0, d1) - half;
                    }

                    Blend(x, y, c, Coverage(d));
                }
            }
        }

        /// <summary>Triángulo relleno (vértices en cualquier orden).</summary>
        public void FillTriangle(Vector2 p0, Vector2 p1, Vector2 p2, Color c)
        {
            float cross = ((p1.x - p0.x) * (p2.y - p0.y)) - ((p1.y - p0.y) * (p2.x - p0.x));
            if (cross < 0f)
            {
                Vector2 t = p1;
                p1 = p2;
                p2 = t;
            }

            int x0 = Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x))) - 1;
            int x1 = Mathf.CeilToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x))) + 1;
            int y0 = Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y))) - 1;
            int y1 = Mathf.CeilToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y))) + 1;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Max(EdgeDistance(p0, p1, p), Mathf.Max(EdgeDistance(p1, p2, p), EdgeDistance(p2, p0, p)));
                    Blend(x, y, c, Coverage(d));
                }
            }
        }

        /// <summary>Rellena todo el lienzo con <paramref name="c"/>.</summary>
        public void Fill(Color c)
        {
            Color[] px = _px;
            for (int i = 0; i < _count; i++)
            {
                px[i] = c;
            }
        }

        /// <summary>
        /// Como <see cref="Blend"/>, pero mezcla en espacio lineal (guía visual v2, 2.5: "BlendLinear"): para lo horneado
        /// que deba igualar a lo que compone la GPU con el proyecto en Linear (sombras de contacto, charcos de luz).
        /// </summary>
        public void BlendLinear(int x, int y, Color c, float coverage)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            float a = c.a * Mathf.Clamp01(coverage);
            if (a <= 0f)
            {
                return;
            }

            int i = (y * Width) + x;
            Color d = _px[i];
            float outA = a + (d.a * (1f - a));
            if (outA <= 0f)
            {
                return;
            }

            float k = d.a * (1f - a);
            _px[i] = new Color(
                ColorMath.ToSrgb(((ColorMath.ToLinear(c.r) * a) + (ColorMath.ToLinear(d.r) * k)) / outA),
                ColorMath.ToSrgb(((ColorMath.ToLinear(c.g) * a) + (ColorMath.ToLinear(d.g) * k)) / outA),
                ColorMath.ToSrgb(((ColorMath.ToLinear(c.b) * a) + (ColorMath.ToLinear(d.b) * k)) / outA),
                outA);
        }

        /// <summary>
        /// Suma luz al píxel como una capa aditiva de la GPU (en lineal, guía 2.5): <paramref name="c"/> por
        /// <paramref name="amount"/> (destellos del río, haces de sol). El alfa del píxel no cambia.
        /// </summary>
        public void AddLight(int x, int y, Color c, float amount)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height || amount <= 0f)
            {
                return;
            }

            int i = (y * Width) + x;
            Color d = _px[i];
            _px[i] = new Color(
                ColorMath.ToSrgb(Mathf.Min(1f, ColorMath.ToLinear(d.r) + (ColorMath.ToLinear(c.r) * amount))),
                ColorMath.ToSrgb(Mathf.Min(1f, ColorMath.ToLinear(d.g) + (ColorMath.ToLinear(c.g) * amount))),
                ColorMath.ToSrgb(Mathf.Min(1f, ColorMath.ToLinear(d.b) + (ColorMath.ToLinear(c.b) * amount))),
                d.a);
        }

        /// <summary>Multiplica el RGB del píxel por <paramref name="factor"/> con cobertura <paramref name="coverage"/>.</summary>
        public void Multiply(int x, int y, float factor, float coverage)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            float k = Mathf.Lerp(1f, factor, Mathf.Clamp01(coverage));
            int i = (y * Width) + x;
            Color d = _px[i];
            _px[i] = new Color(d.r * k, d.g * k, d.b * k, d.a);
        }

        /// <summary>Recorta el alfa del píxel por <paramref name="coverage"/> (máscaras).</summary>
        public void MaskAlpha(int x, int y, float coverage)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            int i = (y * Width) + x;
            Color d = _px[i];
            d.a *= Mathf.Clamp01(coverage);
            _px[i] = d;
        }

        /// <summary>Elipse con borde suave de <paramref name="soft"/> px (sombras y halos sin desenfoque por píxel).</summary>
        public void FillEllipseSoft(float cx, float cy, float rx, float ry, float soft, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - rx - soft - 1f);
            int x1 = Mathf.CeilToInt(cx + rx + soft + 1f);
            int y0 = Mathf.FloorToInt(cy - ry - soft - 1f);
            int y1 = Mathf.CeilToInt(cy + ry + soft + 1f);
            float r = Mathf.Min(rx, ry);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = ((x + 0.5f) - cx) / rx;
                    float py = ((y + 0.5f) - cy) / ry;
                    float d = (Mathf.Sqrt((px * px) + (py * py)) - 1f) * r;
                    Blend(x, y, c, SoftCoverage(d, soft));
                }
            }
        }

        /// <summary>Rectángulo redondeado con borde suave de <paramref name="soft"/> px.</summary>
        public void FillRoundedRectSoft(float x0, float y0, float x1, float y1, float r, float soft, Color c)
        {
            for (int y = Mathf.FloorToInt(y0 - soft) - 1; y <= Mathf.CeilToInt(y1 + soft); y++)
            {
                for (int x = Mathf.FloorToInt(x0 - soft) - 1; x <= Mathf.CeilToInt(x1 + soft); x++)
                {
                    float d = RoundedRectDistance(x + 0.5f, y + 0.5f, x0, y0, x1, y1, r);
                    Blend(x, y, c, SoftCoverage(d, soft));
                }
            }
        }

        /// <summary>Distancia con signo (px, negativa dentro) al rectángulo redondeado [x0, x1] × [y0, y1] de radio r.</summary>
        public static float RoundedRectDistance(float px, float py, float x0, float y0, float x1, float y1, float r)
        {
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float hx = ((x1 - x0) * 0.5f) - r;
            float hy = ((y1 - y0) * 0.5f) - r;
            float qx = Mathf.Abs(px - cx) - hx;
            float qy = Mathf.Abs(py - cy) - hy;
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt((ox * ox) + (oy * oy)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        /// <summary>Cobertura con borde suave: 1 dentro, 0 a <paramref name="soft"/> px fuera (smoothstep).</summary>
        public static float SoftCoverage(float signedDistance, float soft)
        {
            if (soft <= 0.5f)
            {
                return Coverage(signedDistance);
            }

            float t = Mathf.Clamp01((signedDistance + (soft * 0.5f)) / soft);
            return 1f - (t * t * (3f - (2f * t)));
        }

        /// <summary>Píxeles como <see cref="Color32"/> (caché en disco de las texturas generadas). El lienzo sigue valiendo.</summary>
        public Color32[] ToColor32()
        {
            var result = new Color32[_count];
            Color[] px = _px;
            for (int i = 0; i < _count; i++)
            {
                result[i] = px[i];
            }

            return result;
        }

        /// <summary>Como <see cref="ToColor32"/>, y suelta el búfer (<see cref="Release"/>): para el lienzo que ya no se usa.</summary>
        public Color32[] TakeColor32()
        {
            Color32[] result = ToColor32();
            Release();
            return result;
        }

        /// <summary>Píxeles en RGBA32, 4 bytes por píxel (caché en disco), sin el arreglo de <see cref="Color32"/> intermedio.</summary>
        public byte[] ToRgba32Bytes()
        {
            var bytes = new byte[_count * 4];
            Color[] px = _px;
            for (int i = 0; i < _count; i++)
            {
                Color32 c = px[i];
                int k = i * 4;
                bytes[k] = c.r;
                bytes[k + 1] = c.g;
                bytes[k + 2] = c.b;
                bytes[k + 3] = c.a;
            }

            return bytes;
        }

        /// <summary>
        /// Devuelve el búfer a <see cref="PixelBuffers"/> (memoria, 2026-10-26: los lienzos grandes del arranque reutilizan el
        /// mismo en vez de dejar decenas de MB de basura que el heap de IL2CPP no devuelve). Después el lienzo no se puede usar.
        /// Idempotente.
        /// </summary>
        public void Release()
        {
            Color[] px = _px;
            _px = Released;
            if (px.Length > 0)
            {
                PixelBuffers.Return(px);
            }
        }

        /// <summary>
        /// La textura del lienzo; con <paramref name="mipmaps"/>, con su cadena de mipmaps (trilineal), para las piezas que se
        /// pintan a tamaños muy distintos (el rig del perezoso, de ≈ 48 a ≈ 160 dp) sin parpadeo al reducir. Sube los píxeles sin
        /// copia en CPU (<see cref="Texture2D.Apply(bool, bool)"/> con <c>makeNoLongerReadable</c>) y suelta el búfer
        /// (<see cref="Release"/>): el lienzo ya no se puede usar después.
        /// </summary>
        public Texture2D ToTexture(string name, bool repeatX = false, bool repeatY = false, bool mipmaps = false)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, mipmaps)
            {
                name = name,
                filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear,
                wrapModeU = repeatX ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                wrapModeV = repeatY ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            if (mipmaps)
            {
                tex.mipMapBias = -0.5f; // algo más nítido que el mip exacto: el contorno de tinta no se emborrona
            }

            // El búfer alquilado puede ser más largo que el lienzo: SetPixels por bloque solo lee Width × Height (la conversión nativa
            // de siempre, sin ningún Color32[] intermedio).
            tex.SetPixels(0, 0, Width, Height, _px);

            tex.Apply(mipmaps, true);
            Release();
            return tex;
        }

        private static float Dist(float x, float y)
        {
            return Mathf.Sqrt((x * x) + (y * y));
        }

        // Distancia con signo al borde a→b (positiva fuera, con el triángulo en sentido antihorario).
        private static float EdgeDistance(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 e = b - a;
            float len = Mathf.Max(1e-6f, e.magnitude);
            return -(((e.x * (p.y - a.y)) - (e.y * (p.x - a.x))) / len);
        }
    }

    /// <summary>
    /// Ruido determinista para las texturas procedurales (veta de madera, nubes, follaje): hash entero y ruido de valor
    /// suavizado. Solo presentación: no interviene en reglas ni en el núcleo.
    /// </summary>
    internal static class ArtNoise
    {
        /// <summary>Valor pseudoaleatorio en [0, 1) para (x, y, semilla).</summary>
        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)((x * 374761393) + (y * 668265263) + (seed * 1442695041));
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFF) / 16777216f;
            }
        }

        /// <summary>Ruido de valor 1D suavizado en [0, 1).</summary>
        public static float Value1(float x, int seed)
        {
            int i = Mathf.FloorToInt(x);
            float f = x - i;
            float u = f * f * (3f - (2f * f));
            return Mathf.Lerp(Hash(i, 0, seed), Hash(i + 1, 0, seed), u);
        }

        /// <summary>Ruido de valor 2D suavizado en [0, 1).</summary>
        public static float Value2(float x, float y, int seed)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float fx = x - ix;
            float fy = y - iy;
            float ux = fx * fx * (3f - (2f * fx));
            float uy = fy * fy * (3f - (2f * fy));
            float a = Hash(ix, iy, seed);
            float b = Hash(ix + 1, iy, seed);
            float c = Hash(ix, iy + 1, seed);
            float d = Hash(ix + 1, iy + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        /// <summary>Suma de 3 octavas de <see cref="Value2"/> en [0, 1).</summary>
        public static float Fbm2(float x, float y, int seed)
        {
            return (0.57f * Value2(x, y, seed)) + (0.29f * Value2(x * 2.03f, y * 2.03f, seed + 17)) + (0.14f * Value2(x * 4.11f, y * 4.11f, seed + 31));
        }
    }
}
