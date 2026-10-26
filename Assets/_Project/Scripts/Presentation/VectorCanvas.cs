#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Lienzo vectorial en CPU para el arte de la marca (añadido de D-051): una ventana del lienzo de la UI (dp) a una densidad
    /// dada, donde se componen polígonos rellenos (regla no nula, <see cref="VectorFill"/>) con color liso o degradado vertical,
    /// opacidad y recorte opcional, en orden de pintado, y que acaba en una textura. Solo en la construcción, nunca por fotograma.
    /// </summary>
    internal sealed class VectorCanvas
    {
        private readonly Rect _window;
        private readonly float _ppd;
        private readonly float[] _r;
        private readonly float[] _g;
        private readonly float[] _b;
        private readonly float[] _a;
        private readonly float[] _cover;
        private readonly float[] _clip;

        /// <summary>Lienzo para la ventana <paramref name="windowDp"/> (dp) a <paramref name="pxPerDp"/> px por dp.</summary>
        public VectorCanvas(Rect windowDp, float pxPerDp)
        {
            _window = windowDp;
            _ppd = pxPerDp;
            WidthPx = Mathf.Max(1, Mathf.CeilToInt(windowDp.width * pxPerDp));
            HeightPx = Mathf.Max(1, Mathf.CeilToInt(windowDp.height * pxPerDp));
            int n = WidthPx * HeightPx;
            _r = Pooled(ref s_r, n, true);
            _g = Pooled(ref s_g, n, true);
            _b = Pooled(ref s_b, n, true);
            _a = Pooled(ref s_a, n, true);
            _cover = Pooled(ref s_cover, n, false);
            _clip = Pooled(ref s_clip, n, false);
        }

        // Búferes compartidos entre lienzos (uno a la vez, en el hilo principal): sin basura por pieza, que en el arranque cuesta.
        private static float[]? s_r;
        private static float[]? s_g;
        private static float[]? s_b;
        private static float[]? s_a;
        private static float[]? s_cover;
        private static float[]? s_clip;

        private static float[] Pooled(ref float[]? pool, int n, bool clear)
        {
            if (pool == null || pool.Length < n)
            {
                pool = new float[n];
                return pool;
            }

            if (clear)
            {
                System.Array.Clear(pool, 0, n);
            }

            return pool;
        }

        /// <summary>Suelta los búferes compartidos (tras hacer la escena: no se quedan en memoria).</summary>
        public static void ReleasePool()
        {
            s_r = s_g = s_b = s_a = s_cover = s_clip = null;
        }

        /// <summary>Ancho en píxeles.</summary>
        public int WidthPx { get; }

        /// <summary>Alto en píxeles.</summary>
        public int HeightPx { get; }

        /// <summary>
        /// El rectángulo (dp, desde arriba a la izquierda) que ocupa la textura de una ventana <paramref name="windowDp"/> a
        /// <paramref name="pxPerDp"/>: el de la ventana, redondeado a píxeles enteros.
        /// </summary>
        public static Rect PlacedRect(Rect windowDp, float pxPerDp)
        {
            float w = Mathf.Max(1, Mathf.CeilToInt(windowDp.width * pxPerDp)) / pxPerDp;
            float h = Mathf.Max(1, Mathf.CeilToInt(windowDp.height * pxPerDp)) / pxPerDp;
            return new Rect(windowDp.x, windowDp.y, w, h);
        }

        /// <summary>
        /// Rellena los contornos <paramref name="first"/> … + <paramref name="count"/> de <paramref name="pts"/> (x, y; con
        /// <paramref name="ends"/>), llevados a dp con x · <paramref name="s"/> + <paramref name="tx"/> e y · s + <paramref name="ty"/>,
        /// con <paramref name="paint"/>. Con <paramref name="clipPts"/>, solo dentro de esos otros contornos (misma transformación).
        /// </summary>
        public void Fill(float[] pts, int[] ends, int first, int count, float s, float tx, float ty, in Paint paint,
            float[]? clipPts = null, int[]? clipEnds = null, int clipFirst = 0, int clipCount = 0)
        {
            if (count <= 0)
            {
                return;
            }

            int start = first == 0 ? 0 : ends[first - 1];
            int end = ends[first + count - 1];
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            for (int i = start; i < end; i++)
            {
                float x = (pts[2 * i] * s) + tx;
                float y = (pts[(2 * i) + 1] * s) + ty;
                xMin = Mathf.Min(xMin, x);
                yMin = Mathf.Min(yMin, y);
                xMax = Mathf.Max(xMax, x);
                yMax = Mathf.Max(yMax, y);
            }

            int x0 = Mathf.Max(0, Mathf.FloorToInt((xMin - _window.xMin) * _ppd) - 1);
            int y0 = Mathf.Max(0, Mathf.FloorToInt((yMin - _window.yMin) * _ppd) - 1);
            int x1 = Mathf.Min(WidthPx, Mathf.CeilToInt((xMax - _window.xMin) * _ppd) + 1);
            int y1 = Mathf.Min(HeightPx, Mathf.CeilToInt((yMax - _window.yMin) * _ppd) + 1);
            if (x1 <= x0 || y1 <= y0)
            {
                return;
            }

            int rw = x1 - x0;
            int rh = y1 - y0;
            float sp = s * _ppd;
            float ox = ((tx - _window.xMin) * _ppd) - x0;
            float oy = ((ty - _window.yMin) * _ppd) - y0;
            float[] cov = VectorFill.Coverage(pts, ends, first, count, sp, ox, oy, rw, rh, _cover);
            float[]? clip = clipPts != null && clipEnds != null
                ? VectorFill.Coverage(clipPts, clipEnds, clipFirst, clipCount, sp, ox, oy, rw, rh, _clip)
                : null;
            float span = Mathf.Max(1e-3f, paint.GradientY1 - paint.GradientY0);
            for (int row = 0; row < rh; row++)
            {
                float yDp = _window.yMin + ((y0 + row + 0.5f) / _ppd);
                Color c = paint.At((yDp - paint.GradientY0) / span);
                float ca = c.a * paint.Opacity;
                int src = row * rw;
                int dst = ((y0 + row) * WidthPx) + x0;
                for (int x = 0; x < rw; x++)
                {
                    float k = cov[src + x];
                    if (clip != null)
                    {
                        k *= clip[src + x];
                    }

                    k *= ca;
                    if (k <= 0f)
                    {
                        continue;
                    }

                    Blend(dst + x, c, k);
                }
            }
        }

        /// <summary>Rellena las filas de <paramref name="yTopDp"/> a <paramref name="yBottomDp"/>, de lado a lado, con el degradado de <paramref name="paint"/>.</summary>
        public void FillBand(float yTopDp, float yBottomDp, in Paint paint)
        {
            int y0 = Mathf.Max(0, Mathf.FloorToInt((yTopDp - _window.yMin) * _ppd));
            int y1 = Mathf.Min(HeightPx, Mathf.CeilToInt((yBottomDp - _window.yMin) * _ppd));
            float span = Mathf.Max(1e-3f, paint.GradientY1 - paint.GradientY0);
            for (int row = y0; row < y1; row++)
            {
                float yDp = _window.yMin + ((row + 0.5f) / _ppd);
                Color c = paint.At((yDp - paint.GradientY0) / span);
                float top = Mathf.Clamp01((yDp - yTopDp) * _ppd + 0.5f);
                float k = c.a * paint.Opacity * top;
                int dst = row * WidthPx;
                for (int x = 0; x < WidthPx; x++)
                {
                    Blend(dst + x, c, k);
                }
            }
        }

        /// <summary>
        /// La textura (sin mipmaps, bilineal). Con <paramref name="fadeRightPx"/> &gt; 0, el borde derecho se funde en ese ancho.
        /// </summary>
        public Texture2D ToTexture(string name, float fadeRightPx = 0f)
        {
            int w = WidthPx;
            int h = HeightPx;
            var px = new Color32[w * h];
            for (int row = 0; row < h; row++)
            {
                int src = row * w;
                int dst = (h - 1 - row) * w; // la textura va de abajo arriba
                for (int x = 0; x < w; x++)
                {
                    float al = _a[src + x];
                    if (al <= 0.001f)
                    {
                        continue; // ya es (0, 0, 0, 0)
                    }

                    // Directo a bytes (sin Color intermedio): del premultiplicado al color, con su alfa.
                    float inv = 255f / al;
                    float fa = al;
                    if (fadeRightPx > 0f)
                    {
                        fa *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((w - x - 0.5f) / fadeRightPx));
                    }

                    px[dst + x] = new Color32(ToByte(_r[src + x] * inv), ToByte(_g[src + x] * inv), ToByte(_b[src + x] * inv), ToByte(fa * 255f));
                }
            }

            BleedEdges(px, w, h);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static byte ToByte(float v)
        {
            return v >= 255f ? (byte)255 : (v <= 0f ? (byte)0 : (byte)(v + 0.5f));
        }

        // Los píxeles transparentes junto a la figura toman el color de su vecino opaco (con alfa 0): el filtrado bilineal no mezcla
        // el borde con negro y no salen líneas oscuras en los bordes suavizados (entre tramos del açaí, por ejemplo). Una pasada:
        // basta con un píxel.
        private static void BleedEdges(Color32[] px, int w, int h)
        {
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x;
                    Color32 p = px[i];
                    if (p.a != 0 || (p.r | p.g | p.b) != 0)
                    {
                        continue;
                    }

                    int j = -1;
                    if (x > 0 && px[i - 1].a > 0)
                    {
                        j = i - 1;
                    }
                    else if (x < w - 1 && px[i + 1].a > 0)
                    {
                        j = i + 1;
                    }
                    else if (y > 0 && px[i - w].a > 0)
                    {
                        j = i - w;
                    }
                    else if (y < h - 1 && px[i + w].a > 0)
                    {
                        j = i + w;
                    }

                    if (j >= 0)
                    {
                        Color32 n = px[j];
                        px[i] = new Color32(n.r, n.g, n.b, 0);
                    }
                }
            }
        }

        private void Blend(int i, Color c, float k)
        {
            float keep = 1f - k;
            _r[i] = (c.r * k) + (_r[i] * keep);
            _g[i] = (c.g * k) + (_g[i] * keep);
            _b[i] = (c.b * k) + (_b[i] * keep);
            _a[i] = k + (_a[i] * keep);
        }

        /// <summary>Pintura: degradado vertical de hasta 3 paradas (en dp: de <see cref="GradientY0"/> a <see cref="GradientY1"/>) y opacidad.</summary>
        internal readonly struct Paint
        {
            public readonly Color C0;
            public readonly Color C1;
            public readonly Color C2;
            public readonly float O0;
            public readonly float O1;
            public readonly float O2;
            public readonly float GradientY0;
            public readonly float GradientY1;
            public readonly float Opacity;

            public Paint(Color c0, Color c1, Color c2, float o0, float o1, float o2, float gy0, float gy1, float opacity)
            {
                C0 = c0;
                C1 = c1;
                C2 = c2;
                O0 = o0;
                O1 = o1;
                O2 = o2;
                GradientY0 = gy0;
                GradientY1 = gy1;
                Opacity = opacity;
            }

            /// <summary>Color liso.</summary>
            public static Paint Solid(Color c, float opacity = 1f)
            {
                return new Paint(c, c, c, 0f, 1f, 1f, 0f, 1f, opacity);
            }

            /// <summary>Degradado de dos colores entre y0 e y1 (dp).</summary>
            public static Paint Vertical(Color top, Color bottom, float y0, float y1, float opacity = 1f)
            {
                return new Paint(top, bottom, bottom, 0f, 1f, 1f, y0, y1, opacity);
            }

            /// <summary>El color en t (0 arriba, 1 abajo).</summary>
            public Color At(float t)
            {
                t = Mathf.Clamp01(t);
                if (t <= O0)
                {
                    return C0;
                }

                if (t <= O1)
                {
                    return Color.Lerp(C0, C1, (t - O0) / Mathf.Max(1e-4f, O1 - O0));
                }

                if (t <= O2)
                {
                    return Color.Lerp(C1, C2, (t - O1) / Mathf.Max(1e-4f, O2 - O1));
                }

                return C2;
            }
        }
    }
}
