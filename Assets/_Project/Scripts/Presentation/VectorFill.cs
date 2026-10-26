#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Relleno de polígonos en CPU para el arte vectorial de la marca (el nombre y el emblema v2; añadido de D-051): regla no nula,
    /// 4 submuestras por fila y cobertura exacta en horizontal. Solo en la construcción de texturas, nunca por fotograma.
    /// </summary>
    internal static class VectorFill
    {
        private const int SubRows = 4;

        /// <summary>
        /// Cobertura (0-1) de los contornos <paramref name="firstContour"/> … + <paramref name="contourCount"/> de
        /// <paramref name="points"/> (pares x, y; <paramref name="ends"/>: índice, en puntos, donde termina cada contorno), con
        /// px = <paramref name="ox"/> + x · <paramref name="s"/> y py = <paramref name="oy"/> + y · <paramref name="s"/>, en una
        /// máscara de <paramref name="w"/> × <paramref name="h"/> con las filas de arriba abajo. Con <paramref name="buffer"/> de sitio
        /// suficiente, la máscara va en él (se limpia antes): sin basura por relleno.
        /// </summary>
        public static float[] Coverage(float[] points, int[] ends, int firstContour, int contourCount, float s, float ox, float oy, int w, int h, float[]? buffer = null)
        {
            int start = firstContour == 0 ? 0 : ends[firstContour - 1];
            int last = ends[firstContour + contourCount - 1];
            int n = last - start;
            var ex0 = new float[n];
            var ey0 = new float[n];
            var ex1 = new float[n];
            var ey1 = new float[n];
            var dir = new int[n];
            int count = 0;
            for (int c = firstContour; c < firstContour + contourCount; c++)
            {
                int end = ends[c];
                for (int i = start; i < end; i++)
                {
                    int j = i + 1 < end ? i + 1 : start;
                    float ax = ox + (points[2 * i] * s);
                    float ay = oy + (points[(2 * i) + 1] * s);
                    float bx = ox + (points[2 * j] * s);
                    float by = oy + (points[(2 * j) + 1] * s);
                    if (ay == by)
                    {
                        continue;
                    }

                    bool down = by > ay;
                    ex0[count] = down ? ax : bx;
                    ey0[count] = down ? ay : by;
                    ex1[count] = down ? bx : ax;
                    ey1[count] = down ? by : ay;
                    dir[count] = down ? 1 : -1;
                    count++;
                }

                start = end;
            }

            var order = new int[count];
            var keys = new float[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
                keys[i] = ey0[i];
            }

            Array.Sort(keys, order);
            float[] mask;
            if (buffer != null && buffer.Length >= w * h)
            {
                mask = buffer;
                Array.Clear(mask, 0, w * h);
            }
            else
            {
                mask = new float[w * h];
            }

            var acc = new float[w + 1];
            var active = new int[Mathf.Max(1, count)];
            var xs = new float[Mathf.Max(1, count)];
            var ds = new int[Mathf.Max(1, count)];
            int activeCount = 0;
            int next = 0;
            const float weight = 1f / SubRows;
            for (int row = 0; row < h; row++)
            {
                bool any = false;
                for (int sub = 0; sub < SubRows; sub++)
                {
                    float y = row + ((sub + 0.5f) / SubRows);
                    while (next < count && ey0[order[next]] <= y)
                    {
                        active[activeCount++] = order[next++];
                    }

                    int hits = 0;
                    for (int a = 0; a < activeCount; a++)
                    {
                        int e = active[a];
                        if (ey1[e] <= y)
                        {
                            active[a--] = active[--activeCount];
                            continue;
                        }

                        xs[hits] = ex0[e] + ((y - ey0[e]) * (ex1[e] - ex0[e]) / (ey1[e] - ey0[e]));
                        ds[hits] = dir[e];
                        hits++;
                    }

                    // Por x: inserción con pocos cortes y ordenación rápida con muchos (las copas de la selva dan cientos por fila).
                    if (hits > 24)
                    {
                        Array.Sort(xs, ds, 0, hits);
                    }
                    else
                    {
                        for (int q = 1; q < hits; q++)
                        {
                            float xq = xs[q];
                            int dq = ds[q];
                            int k = q;
                            while (k > 0 && xs[k - 1] > xq)
                            {
                                xs[k] = xs[k - 1];
                                ds[k] = ds[k - 1];
                                k--;
                            }

                            xs[k] = xq;
                            ds[k] = dq;
                        }
                    }

                    int winding = 0;
                    float spanStart = 0f;
                    for (int k = 0; k < hits; k++)
                    {
                        int before = winding;
                        winding += ds[k];
                        if (before == 0 && winding != 0)
                        {
                            spanStart = xs[k];
                        }
                        else if (before != 0 && winding == 0)
                        {
                            if (!any)
                            {
                                Array.Clear(acc, 0, acc.Length);
                                any = true;
                            }

                            AddSpan(acc, w, spanStart, xs[k], weight);
                        }
                    }
                }

                if (!any)
                {
                    continue;
                }

                int baseIndex = row * w;
                for (int x = 0; x < w; x++)
                {
                    mask[baseIndex + x] = Mathf.Clamp01(acc[x]);
                }
            }

            return mask;
        }

        // Suma weight × (lo que cubre [a, b) de cada píxel) a acc.
        private static void AddSpan(float[] acc, int w, float a, float b, float weight)
        {
            a = Mathf.Clamp(a, 0f, w);
            b = Mathf.Clamp(b, 0f, w);
            if (b <= a)
            {
                return;
            }

            int ia = (int)a;
            int ib = (int)b;
            if (ia == ib)
            {
                acc[ia] += (b - a) * weight;
                return;
            }

            acc[ia] += (ia + 1 - a) * weight;
            for (int i = ia + 1; i < ib; i++)
            {
                acc[i] += weight;
            }

            if (ib < w)
            {
                acc[ib] += (b - ib) * weight;
            }
        }
    }
}
