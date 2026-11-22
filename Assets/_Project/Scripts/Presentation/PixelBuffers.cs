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
    /// siguiente: el pico baja a lo que miden a la vez los lienzos vivos. Solo se guardan durante el arranque
    /// (<see cref="BeginPooling"/>-<see cref="EndPooling()"/>, en <c>GameBootstrap</c>), al abrir el Mapa y al repintar el inicio:
    /// después solo queda vivo, si se abrió el Mapa, un búfer de reserva (el más grande de su apertura, <see cref="ReserveBytes"/>;
    /// <c>docs/dev/memoria.md</c> §10), que la apertura siguiente reutiliza en vez de pedir otro al heap. Seguro entre hilos (los
    /// miquitos se pintan en otro).
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
        private static volatile bool s_pooling = true;

        /// <summary>
        /// Se guardan búferes al soltarlos: entre <see cref="BeginPooling"/> y <see cref="EndPooling()"/>, durante el arranque, al abrir
        /// el Mapa (<c>GameBootstrap.OpenMapPooled</c>) y al repintar la escena del inicio (<c>LobbyScreen</c>, <c>docs/dev/memoria.md</c>
        /// §9.2). Fuera de esas ventanas, <see cref="Return"/> no guarda nada: los lienzos grandes de después (modales, TiTino, el
        /// banner del mundo) se tiran como siempre y su memoria vuelve al heap; solo queda vivo, si se abrió el Mapa, el búfer de
        /// reserva (<see cref="ReserveBytes"/>, §10).
        /// </summary>
        public static bool Pooling => s_pooling;

        /// <summary>
        /// Empieza a guardar búferes (al empezar el arranque, antes de la escena de la marca, al abrir el Mapa y al repintar el
        /// inicio). Si hay reserva (<see cref="ReserveBytes"/>), se presta: el primer lienzo que quepa en ella la usa.
        /// </summary>
        public static void BeginPooling()
        {
            s_pooling = true;
            lock (s_lock)
            {
                Color[]? reserve = s_reserve;
                s_reserve = null;
                if (reserve != null)
                {
                    s_reserveLent = true;
                    s_free.Add(reserve);
                }
            }
        }

        /// <summary>Deja de guardar búferes y suelta los libres (al acabar el arranque).</summary>
        public static void EndPooling()
        {
            EndPooling(false);
        }

        /// <summary>
        /// Deja de guardar búferes y suelta los libres, salvo la reserva: con <paramref name="keepLargest"/> (al acabar de abrir el
        /// Mapa: observación 2 de la prueba r2b de la 1.1, <c>docs/dev/memoria.md</c> §10), el búfer libre más grande se queda como
        /// reserva para la apertura siguiente; si la reserva estaba prestada, vuelve (el búfer libre más grande que no pase de su
        /// medida). Así cada apertura del Mapa reutiliza el mismo búfer grande en vez de pedir otro al heap gestionado, que con
        /// IL2CPP crece y no se devuelve.
        /// </summary>
        public static void EndPooling(bool keepLargest)
        {
            s_pooling = false;
            lock (s_lock)
            {
                if (keepLargest && s_droppedForLowMemory)
                {
                    // Tras un aviso de memoria baja, esta apertura no vuelve a guardar la reserva: la guarda la siguiente.
                    keepLargest = false;
                    s_droppedForLowMemory = false;
                }

                if (keepLargest || s_reserveLent)
                {
                    int cap = keepLargest ? int.MaxValue : s_reserveLength;
                    int best = -1;
                    for (int i = 0; i < s_free.Count; i++)
                    {
                        int length = s_free[i].Length;
                        if (length <= cap && (best < 0 || length > s_free[best].Length))
                        {
                            best = i;
                        }
                    }

                    if (best >= 0)
                    {
                        s_reserve = s_free[best];
                        s_reserveLength = s_reserve.Length;
                        s_free.RemoveAt(best);
                    }
                }

                s_reserveLent = false;
            }

            Trim();
        }

        /// <summary>Bytes de la reserva guardada ahora (0 si no hay o si está prestada).</summary>
        public static long ReserveBytes
        {
            get
            {
                Color[]? reserve = s_reserve;
                return reserve != null ? (long)reserve.Length * 16 : 0L;
            }
        }

        /// <summary>Suelta la reserva (tests), también si está prestada: la apertura siguiente vuelve a pedir su búfer y la guarda.</summary>
        public static void ReleaseReserve()
        {
            lock (s_lock)
            {
                s_reserve = null;
                s_reserveLength = 0;
                s_reserveLent = false;
                s_droppedForLowMemory = false;
            }
        }

        /// <summary>
        /// Suelta la reserva por un aviso de memoria baja del sistema (<c>Application.lowMemory</c>), también si está prestada: el
        /// <see cref="EndPooling(bool)"/> que sigue (el de la apertura en curso o el de la siguiente) no la vuelve a guardar; la
        /// guarda la apertura normal posterior.
        /// </summary>
        public static void ReleaseReserveForLowMemory()
        {
            lock (s_lock)
            {
                s_reserve = null;
                s_reserveLength = 0;
                s_reserveLent = false;
                s_droppedForLowMemory = true;
            }
        }

        /// <summary>La reserva se soltó por memoria baja y la apertura siguiente no la guardará (tests).</summary>
        public static bool ReserveDroppedForLowMemory => s_droppedForLowMemory;

        private static readonly List<Color[]> s_free = new List<Color[]>(MaxFree + 2);
        private static Color[]? s_reserve;
        private static int s_reserveLength;
        private static bool s_reserveLent;
        private static bool s_droppedForLowMemory;
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
            if (buffer == null || buffer.Length < MinPooledPixels || !s_pooling)
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
                if (s_free.Count > MaxFree + (s_reserveLent ? 1 : 0))
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
        /// Suelta los búferes libres y el de subida: su memoria queda para el resto del juego en lugar de esperar a un lienzo que quizá
        /// no llegue (lo hace <see cref="EndPooling"/> al acabar el arranque).
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
            s_pooling = true;
            Trim();
            ReleaseReserve();
            Interlocked.Exchange(ref s_canvasBytes, 0);
            Interlocked.Exchange(ref s_newBytes, 0);
            Interlocked.Exchange(ref s_reused, 0);
        }
    }
}
