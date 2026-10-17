#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Utilidades de los bucles largos del ambiente y la música (tarea AU2), a cualquier frecuencia de muestreo: filtros RBJ,
    /// filtrado circular (la salida de un bucle sigue siendo un bucle), cierre de un ruido largo con fundido cruzado de potencia
    /// constante y nivel por RMS. Deterministas y sin motor; asignan sus búferes: solo al generar, nunca por fotograma.
    /// </summary>
    public static class LoopDsp
    {
        private const double TwoPi = 2.0 * Math.PI;

        /// <summary>Filtro biquad RBJ de <paramref name="buf"/> sobre una copia, a <paramref name="sampleRate"/> Hz.</summary>
        public static float[] Biquad(float[] buf, BiquadType type, double freq, double q, int sampleRate)
        {
            var output = new float[buf.Length];
            Coefficients(type, freq, q, sampleRate, out double b0, out double b1, out double b2, out double a1, out double a2);
            double x1 = 0.0, x2 = 0.0, y1 = 0.0, y2 = 0.0;
            for (int i = 0; i < buf.Length; i++)
            {
                double x0 = buf[i];
                double y0 = (b0 * x0) + (b1 * x1) + (b2 * x2) - (a1 * y1) - (a2 * y2);
                output[i] = (float)y0;
                x2 = x1;
                x1 = x0;
                y2 = y1;
                y1 = y0;
            }

            return output;
        }

        /// <summary>
        /// Filtro biquad de un bucle, circular: da dos vueltas y se queda con la segunda, que empieza con el estado del final de
        /// la primera; así la salida enlaza su última muestra con la primera como la entrada (sin costura nueva).
        /// </summary>
        public static float[] BiquadCircular(float[] loop, BiquadType type, double freq, double q, int sampleRate)
        {
            int n = loop.Length;
            var output = new float[n];
            Coefficients(type, freq, q, sampleRate, out double b0, out double b1, out double b2, out double a1, out double a2);
            double x1 = 0.0, x2 = 0.0, y1 = 0.0, y2 = 0.0;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    double x0 = loop[i];
                    double y0 = (b0 * x0) + (b1 * x1) + (b2 * x2) - (a1 * y1) - (a2 * y2);
                    if (pass == 1)
                    {
                        output[i] = (float)y0;
                    }

                    x2 = x1;
                    x1 = x0;
                    y2 = y1;
                    y1 = y0;
                }
            }

            return output;
        }

        /// <summary>
        /// Cierra en bucle de <paramref name="n"/> muestras un sonido estacionario más largo (<paramref name="longBuf"/>, al menos
        /// <paramref name="skip"/> + n + <paramref name="fade"/>): descarta las <paramref name="skip"/> primeras (el arranque de los
        /// filtros) y funde lo que sigue al final sobre el principio con potencia constante (seno y coseno, que con ruido sin
        /// correlación dejan el nivel igual). La última muestra enlaza con la primera: sin clic ni bache de nivel.
        /// </summary>
        public static float[] CrossfadeLoop(float[] longBuf, int skip, int n, int fade)
        {
            if (longBuf.Length < skip + n + fade)
            {
                throw new ArgumentException("El búfer no alcanza para el bucle y su fundido.", nameof(longBuf));
            }

            var loop = new float[n];
            for (int i = 0; i < n; i++)
            {
                double v;
                if (i < fade)
                {
                    double w = (double)i / fade;
                    v = (longBuf[skip + n + i] * Math.Cos(w * Math.PI * 0.5)) + (longBuf[skip + i] * Math.Sin(w * Math.PI * 0.5));
                }
                else
                {
                    v = longBuf[skip + i];
                }

                loop[i] = (float)v;
            }

            return loop;
        }

        /// <summary>RMS de <paramref name="buf"/> (lineal).</summary>
        public static double Rms(float[] buf)
        {
            if (buf.Length == 0)
            {
                return 0.0;
            }

            double s = 0.0;
            for (int i = 0; i < buf.Length; i++)
            {
                s += (double)buf[i] * buf[i];
            }

            return Math.Sqrt(s / buf.Length);
        }

        /// <summary>Multiplica <paramref name="buf"/> para dejar su RMS en <paramref name="rmsDb"/> dBFS.</summary>
        public static void NormalizeRms(float[] buf, double rmsDb)
        {
            double rms = Rms(buf);
            if (rms <= 0.0)
            {
                return;
            }

            Scale(buf, AudioAnalysis.FromDb(rmsDb) / rms);
        }

        /// <summary>Multiplica <paramref name="buf"/> por <paramref name="gain"/>.</summary>
        public static void Scale(float[] buf, double gain)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (float)(buf[i] * gain);
            }
        }

        /// <summary>
        /// Burbuja de Minnaert (la de <see cref="Dsp.Bubble"/>) escrita en un bucle de <paramref name="sampleRate"/> Hz: lo que pasa
        /// del final vuelve al principio.
        /// </summary>
        public static void BubbleWrapped(float[] buf, int sampleRate, double startSec, double f0, double durSec, double amp, double chirpFactor)
        {
            int n = buf.Length;
            double sr = sampleRate;
            int i0 = Math.Max(0, (int)Math.Floor(startSec * sr));
            int len = (int)Math.Floor(durSec * sr);
            double phase = 0.0;
            for (int i = 0; i < len; i++)
            {
                double u = (double)i / len;
                double attack = Math.Min(1.0, i / (0.0025 * sr));
                double decay = Math.Exp(-u * 5.2);
                double f = f0 * (1.0 + ((chirpFactor - 1.0) * Math.Pow(u, 1.85)));
                phase += TwoPi * f / sr;
                int k = (i0 + i) % n;
                buf[k] = (float)(buf[k] + (Math.Sin(phase) * attack * decay * amp));
            }
        }

        /// <summary>
        /// Baja un bucle a la mitad de muestras con un paso bajo triangular circular (sigue sin costura) y la ganancia
        /// <paramref name="gain"/>.
        /// </summary>
        public static float[] Decimate2(float[] x, double gain)
        {
            int n = x.Length;
            int m = n / 2;
            var y = new float[m];
            for (int j = 0; j < m; j++)
            {
                int c = 2 * j;
                double a = x[(c - 1 + n) % n];
                double b = x[c];
                double d = x[(c + 1) % n];
                y[j] = (float)(((0.25 * a) + (0.5 * b) + (0.25 * d)) * gain);
            }

            return y;
        }

        private static void Coefficients(BiquadType type, double freq, double q, int sampleRate, out double b0, out double b1, out double b2, out double a1, out double a2)
        {
            double sr = sampleRate;
            double w0 = TwoPi * Math.Min(Math.Max(freq, 20.0), sr * 0.45) / sr;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double alpha = sinw0 / (2.0 * Math.Max(q, 0.1));
            if (type == BiquadType.Lowpass)
            {
                b0 = (1.0 - cosw0) / 2.0;
                b1 = 1.0 - cosw0;
                b2 = (1.0 - cosw0) / 2.0;
            }
            else if (type == BiquadType.Highpass)
            {
                b0 = (1.0 + cosw0) / 2.0;
                b1 = -(1.0 + cosw0);
                b2 = (1.0 + cosw0) / 2.0;
            }
            else
            {
                b0 = alpha;
                b1 = 0.0;
                b2 = -alpha;
            }

            double a0 = 1.0 + alpha;
            a1 = (-2.0 * cosw0) / a0;
            a2 = (1.0 - alpha) / a0;
            b0 /= a0;
            b1 /= a0;
            b2 /= a0;
        }
    }
}
