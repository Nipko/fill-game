#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Búferes de los lienzos grandes del arte por código (<see cref="PixelCanvas"/>; memoria, 2026-10-26, informe
    /// <c>docs/dev/memoria-2026-10-26.md</c>). Cada lienzo guarda 16 bytes por píxel (<see cref="Color"/>) y el arranque pinta
    /// unos 110 MB de lienzos que se tiran en cuanto suben a la GPU; con IL2CPP el heap gestionado crece hasta el pico y no se
    /// devuelve al sistema. Aquí los lienzos de al menos <see cref="MinPooledPixels"/> píxeles alquilan un búfer que, al
    /// soltarse (<see cref="PixelCanvas.Release"/>, que hacen <see cref="PixelCanvas.ToTexture"/> y <c>MakeSprite</c>), sirve al
    /// siguiente: el pico baja a lo que miden a la vez los lienzos vivos. Seguro entre hilos (los miquitos se pintan en otro).
    /// </summary>
    public static class PixelBuffers
    {
        /// <summary>Lienzos más pequeños (64 × 64 px... 128 × 128) se crean y se tiran como siempre: no compensa guardarlos.</summary>
        public const int MinPooledPixels = 128 * 128;

        /// <summary>Un búfer libre sirve a un lienzo que mida al menos 1/<see cref="MaxWaste"/> de su largo.</summary>
        public const int MaxWaste = 8;

        /// <summary>Búferes libres como mucho; al pasar, se suelta el más pequeño.</summary>
        public const int MaxFree = 4;

        private static readonly object s_lock = new object();
        private static readonly List<Color[]> s_free = new List<Color[]>(MaxFree + 1);
        private static long s_canvasBytes;
        private static long s_newBytes;
        private static int s_reused;

        /// <summary>Bytes de píxeles que han pedido todos los lienzos desde el arranque (16 por píxel; sonda <c>AF_PERF mem</c>).</summary>
        public static long CanvasBytes => Interlocked.Read(ref s_canvasBytes);

        /// <summary>Bytes de búferes nuevos creados (lo que de verdad se asignó; el resto salió de búferes reutilizados).</summary>
        public static long NewBytes => Interlocked.Read(ref s_newBytes);

        /// <summary>Lienzos que reutilizaron un búfer libre.</summary>
        public static int Reused => Volatile.Read(ref s_reused);

        /// <summary>Bytes de los búferes libres guardados ahora.</summary>
        public static long FreeBytes
        {
            get
            {
                lock (s_lock)
                {
                    long bytes = 0;
                    for (int i = 0; i < s_free.Count; i++)
                    {
                        bytes += (long)s_free[i].Length * 16;
                    }

                    return bytes;
                }
            }
        }

        /// <summary>Un búfer de al menos <paramref name="count"/> píxeles, con los <paramref name="count"/> primeros a cero (transparente).</summary>
        internal static Color[] Rent(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Interlocked.Add(ref s_canvasBytes, (long)count * 16);
            if (count >= MinPooledPixels)
            {
                Color[]? best = null;
                lock (s_lock)
                {
                    int bestIndex = -1;
                    for (int i = 0; i < s_free.Count; i++)
                    {
                        int length = s_free[i].Length;
                        // El más pequeño que sirva (uno de la misma medida, si lo hay: la textura sube sin conversión en C#).
                        if (length >= count && length / MaxWaste <= count && (bestIndex < 0 || length < s_free[bestIndex].Length))
                        {
                            bestIndex = i;
                        }
                    }

                    if (bestIndex >= 0)
                    {
                        best = s_free[bestIndex];
                        s_free.RemoveAt(bestIndex);
                    }
                }

                if (best != null)
                {
                    Interlocked.Increment(ref s_reused);
                    Array.Clear(best, 0, count);
                    return best;
                }
            }

            Interlocked.Add(ref s_newBytes, (long)count * 16);
            return new Color[count];
        }

        /// <summary>Guarda <paramref name="buffer"/> para otro lienzo (los pequeños no se guardan).</summary>
        internal static void Return(Color[] buffer)
        {
            if (buffer == null || buffer.Length < MinPooledPixels)
            {
                return;
            }

            lock (s_lock)
            {
                for (int i = 0; i < s_free.Count; i++)
                {
                    if (ReferenceEquals(s_free[i], buffer))
                    {
                        return;
                    }
                }

                s_free.Add(buffer);
                if (s_free.Count > MaxFree)
                {
                    int smallest = 0;
                    for (int i = 1; i < s_free.Count; i++)
                    {
                        if (s_free[i].Length < s_free[smallest].Length)
                        {
                            smallest = i;
                        }
                    }

                    s_free.RemoveAt(smallest);
                }
            }
        }

        private static Color32[]? s_staging;

        /// <summary>
        /// Búfer de <see cref="Color32"/> de al menos <paramref name="count"/> píxeles para pasar un lienzo a su textura (se copia
        /// a la memoria de la textura con <c>NativeArray.Copy</c>): uno solo, compartido, que crece con el lienzo más grande. Solo en
        /// el hilo principal (las texturas solo se crean ahí). Su contenido no se limpia.
        /// </summary>
        internal static Color32[] Staging(int count)
        {
            Color32[]? staging = s_staging;
            if (staging == null || staging.Length < count)
            {
                staging = new Color32[count];
                s_staging = staging;
            }

            return staging;
        }

        /// <summary>
        /// Suelta los búferes libres (al acabar el arranque, y al quitar la escena de la marca): su memoria queda para el resto del
        /// juego en lugar de esperar a un lienzo que quizá no llegue.
        /// </summary>
        public static void Trim()
        {
            lock (s_lock)
            {
                s_free.Clear();
            }

            s_staging = null;
        }

        /// <summary>Pone a cero las cuentas y suelta los libres (tests).</summary>
        internal static void ResetForTests()
        {
            Trim();
            Interlocked.Exchange(ref s_canvasBytes, 0);
            Interlocked.Exchange(ref s_newBytes, 0);
            Interlocked.Exchange(ref s_reused, 0);
        }
    }
}
