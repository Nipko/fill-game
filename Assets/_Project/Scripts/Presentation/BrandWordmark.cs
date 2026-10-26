#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// El nombre del logo v2 (D-045, «El río que se tiñe»: Baloo 2 ExtraBold, convertida en trazados) para la pantalla de arranque
    /// (añadido de D-051, 2026-10-25). Los contornos los genera <c>docs/art/marca-oficial/v2/exportar.py --unity-wordmark</c> desde
    /// la misma fuente y con las mismas cifras que el logo (<see cref="Points"/>, en <c>BrandWordmark.Generated.cs</c>), y aquí se
    /// rasterizan al tamaño real en píxeles de la pantalla (<c>VectorFill</c>): nítido a cualquier densidad, sin PNG ni catálogo
    /// de arte. Es un dibujo, no un texto (06 §2.1, regla 3).
    /// </summary>
    public static partial class BrandWordmark
    {
        /// <summary>Alto / ancho de la caja de los contornos.</summary>
        public const float Aspect = Height / Width;

        /// <summary>
        /// Rasteriza el nombre con <paramref name="widthPx"/> px de ancho de caja: relleno con degradado vertical de
        /// <paramref name="top"/> a <paramref name="bottom"/> y sombra <paramref name="shadow"/> desplazada
        /// <paramref name="shadowOffsetPx"/> hacia abajo y difuminada <paramref name="shadowBlurPx"/>. La textura lleva margen para la
        /// sombra: <paramref name="padPx"/> = (izquierda, arriba, derecha, abajo) en px, fuera de la caja de los contornos.
        /// </summary>
        public static Texture2D Render(int widthPx, Color top, Color bottom, Color shadow, float shadowOffsetPx, float shadowBlurPx, out Vector4 padPx)
        {
            widthPx = Mathf.Max(8, widthPx);
            float s = widthPx / Width;
            int boxH = Mathf.CeilToInt(Height * s);
            int pad = Mathf.CeilToInt((shadowBlurPx * 2f) + 2f);
            int padBottom = pad + Mathf.CeilToInt(shadowOffsetPx);
            int w = widthPx + (2 * pad);
            int h = boxH + pad + padBottom;
            padPx = new Vector4(pad, pad, pad, padBottom);

            float[] mask = Coverage(w, h, s, pad, pad);
            float[] shade = Shifted(mask, w, h, Mathf.RoundToInt(shadowOffsetPx));
            int r = Mathf.Max(0, Mathf.RoundToInt(shadowBlurPx));
            if (r > 0)
            {
                BoxBlur(shade, w, h, r);
                BoxBlur(shade, w, h, r);
            }

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                // Las filas de la textura van de abajo arriba; las de la máscara, de arriba abajo.
                int row = h - 1 - y;
                float t = Mathf.Clamp01((row - pad) / (float)Mathf.Max(1, boxH));
                Color fill = Color.Lerp(top, bottom, t);
                for (int x = 0; x < w; x++)
                {
                    int i = (row * w) + x;
                    float a = mask[i];
                    float sa = shade[i] * shadow.a;
                    // El relleno sobre la sombra (premultiplicado y vuelto a dividir).
                    float outA = a + (sa * (1f - a));
                    if (outA <= 0f)
                    {
                        px[(y * w) + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float cr = ((fill.r * a) + (shadow.r * sa * (1f - a))) / outA;
                    float cg = ((fill.g * a) + (shadow.g * sa * (1f - a))) / outA;
                    float cb = ((fill.b * a) + (shadow.b * sa * (1f - a))) / outA;
                    px[(y * w) + x] = new Color(cr, cg, cb, outA);
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "AF_BrandWordmark",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Cobertura (0-1) de los contornos escalados por <paramref name="s"/> y desplazados (<paramref name="ox"/>,
        /// <paramref name="oy"/>) px, en una máscara de <paramref name="w"/> × <paramref name="h"/> con las filas de arriba abajo.
        /// </summary>
        internal static float[] Coverage(int w, int h, float s, float ox, float oy)
        {
            return VectorFill.Coverage(Points, ContourEnds, 0, ContourEnds.Length, s, ox, oy, w, h);
        }

        // La máscara desplazada dy filas hacia abajo.
        private static float[] Shifted(float[] mask, int w, int h, int dy)
        {
            var result = new float[mask.Length];
            for (int row = 0; row < h; row++)
            {
                int src = row - dy;
                if (src < 0 || src >= h)
                {
                    continue;
                }

                Array.Copy(mask, src * w, result, row * w, w);
            }

            return result;
        }

        // Difuminado de caja separable, de radio r, en el sitio.
        private static void BoxBlur(float[] m, int w, int h, int r)
        {
            var line = new float[Mathf.Max(w, h)];
            float norm = 1f / ((2 * r) + 1);
            for (int y = 0; y < h; y++)
            {
                int o = y * w;
                float sum = 0f;
                for (int x = -r; x <= r; x++)
                {
                    sum += m[o + Mathf.Clamp(x, 0, w - 1)];
                }

                for (int x = 0; x < w; x++)
                {
                    line[x] = sum * norm;
                    sum += m[o + Mathf.Min(x + r + 1, w - 1)] - m[o + Mathf.Max(x - r, 0)];
                }

                Array.Copy(line, 0, m, o, w);
            }

            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int y = -r; y <= r; y++)
                {
                    sum += m[(Mathf.Clamp(y, 0, h - 1) * w) + x];
                }

                for (int y = 0; y < h; y++)
                {
                    line[y] = sum * norm;
                    sum += m[(Mathf.Min(y + r + 1, h - 1) * w) + x] - m[(Mathf.Max(y - r, 0) * w) + x];
                }

                for (int y = 0; y < h; y++)
                {
                    m[(y * w) + x] = line[y];
                }
            }
        }
    }
}
