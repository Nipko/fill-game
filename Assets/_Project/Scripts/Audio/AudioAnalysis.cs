#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Medidas de un búfer de PCM mono a 44,1 kHz (tests y masterización): pico, energía por encima de una frecuencia (por FFT,
    /// con el búfer entero), cola audible, salto en la costura de un bucle y huella determinista. Sin motor. Asigna sus búferes
    /// de trabajo: solo al construir los clips y en los tests, nunca por fotograma.
    /// </summary>
    public static class AudioAnalysis
    {
        /// <summary>Corte de los graves que el altavoz de un teléfono no reproduce (Hz; revisión del laboratorio, 2026-10-16).</summary>
        public const double SpeakerCutHz = 300.0;

        /// <summary>Pico absoluto (lineal).</summary>
        public static float Peak(float[] buf)
        {
            float peak = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float a = Math.Abs(buf[i]);
                if (a > peak)
                {
                    peak = a;
                }
            }

            return peak;
        }

        /// <summary>Pico en dBFS (−∞ con silencio: <see cref="float.NegativeInfinity"/>).</summary>
        public static float PeakDb(float[] buf)
        {
            return ToDb(Peak(buf));
        }

        /// <summary>Lineal → dB.</summary>
        public static float ToDb(double linear)
        {
            return linear <= 0.0 ? float.NegativeInfinity : (float)(20.0 * Math.Log10(linear));
        }

        /// <summary>dB → lineal.</summary>
        public static double FromDb(double db)
        {
            return Math.Pow(10.0, db / 20.0);
        }

        /// <summary>
        /// Fracción de la energía de <paramref name="buf"/> en las frecuencias ≥ <paramref name="hz"/> (0-1), con el espectro de
        /// potencia de una FFT de todo el búfer (rellenado con ceros hasta la potencia de 2). 0 con silencio.
        /// </summary>
        public static double EnergyFractionAbove(float[] buf, double hz)
        {
            Spectrum(buf, out double[] re, out double[] im);
            return FractionAbove(re, im, hz);
        }

        /// <summary><see cref="EnergyFractionAbove(float[], double)"/> de un búfer a <paramref name="sampleRate"/> Hz.</summary>
        public static double EnergyFractionAbove(float[] buf, double hz, int sampleRate)
        {
            Spectrum(buf, out double[] re, out double[] im);
            return FractionAbove(re, im, hz, sampleRate);
        }

        /// <summary>Fracción de energía por encima de <paramref name="hz"/> de un espectro ya calculado (<see cref="Spectrum"/>).</summary>
        public static double FractionAbove(double[] re, double[] im, double hz)
        {
            return FractionAbove(re, im, hz, Dsp.SampleRate);
        }

        /// <summary><see cref="FractionAbove(double[], double[], double)"/> de un búfer a <paramref name="sampleRate"/> Hz.</summary>
        public static double FractionAbove(double[] re, double[] im, double hz, int sampleRate)
        {
            int n = re.Length;
            double total = 0.0;
            double above = 0.0;
            for (int k = 0; k <= n / 2; k++)
            {
                // Las bandas 1…n/2−1 tienen su gemela negativa (doble energía); la 0 y la n/2, no.
                double w = k == 0 || k == n / 2 ? 1.0 : 2.0;
                double p = ((re[k] * re[k]) + (im[k] * im[k])) * w;
                total += p;
                if (k * (double)sampleRate / n >= hz)
                {
                    above += p;
                }
            }

            return total <= 0.0 ? 0.0 : above / total;
        }

        /// <summary>FFT de todo el búfer (radix 2, rellenada con ceros): parte real e imaginaria.</summary>
        public static void Spectrum(float[] buf, out double[] re, out double[] im)
        {
            int n = 1;
            while (n < buf.Length)
            {
                n <<= 1;
            }

            re = new double[n];
            im = new double[n];
            for (int i = 0; i < buf.Length; i++)
            {
                re[i] = buf[i];
            }

            Fft(re, im);
        }

        /// <summary>FFT compleja iterativa en el sitio (n potencia de 2).</summary>
        public static void Fft(double[] re, double[] im)
        {
            int n = re.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1)
                {
                    j ^= bit;
                }

                j ^= bit;
                if (i < j)
                {
                    double tr = re[i];
                    re[i] = re[j];
                    re[j] = tr;
                    double ti = im[i];
                    im[i] = im[j];
                    im[j] = ti;
                }
            }

            for (int len = 2; len <= n; len <<= 1)
            {
                double ang = -2.0 * Math.PI / len;
                double wr = Math.Cos(ang);
                double wi = Math.Sin(ang);
                for (int i = 0; i < n; i += len)
                {
                    double cr = 1.0;
                    double ci = 0.0;
                    int half = len >> 1;
                    for (int k = 0; k < half; k++)
                    {
                        int a = i + k;
                        int b = a + half;
                        double xr = (re[b] * cr) - (im[b] * ci);
                        double xi = (re[b] * ci) + (im[b] * cr);
                        re[b] = re[a] - xr;
                        im[b] = im[a] - xi;
                        re[a] += xr;
                        im[a] += xi;
                        double ncr = (cr * wr) - (ci * wi);
                        ci = (cr * wi) + (ci * wr);
                        cr = ncr;
                    }
                }
            }
        }

        /// <summary>
        /// Segundos hasta la última muestra que supera <paramref name="relativeDb"/> respecto al pico (la cola audible; −60 dB por
        /// defecto). 0 con silencio.
        /// </summary>
        public static double AudibleSeconds(float[] buf, double relativeDb = -60.0)
        {
            float peak = Peak(buf);
            if (peak <= 0f)
            {
                return 0.0;
            }

            double floor = peak * FromDb(relativeDb);
            for (int i = buf.Length - 1; i >= 0; i--)
            {
                if (Math.Abs(buf[i]) >= floor)
                {
                    return (i + 1) / (double)Dsp.SampleRate;
                }
            }

            return 0.0;
        }

        /// <summary>Ventana a cada lado de la costura con la que se compara su salto (s): unos 5 ms.</summary>
        public const double SeamWindowSeconds = 0.005;

        /// <summary>
        /// Salto de la costura de un bucle (|primera − última|) dividido por el mayor salto entre dos muestras seguidas de la
        /// ventana de <see cref="SeamWindowSeconds"/> a cada lado (el final y el principio del bucle): ≤ 1 es una costura sin clic,
        /// porque no salta más que la onda que la rodea. Se compara con lo de alrededor y no con todo el bucle, para que un salto
        /// en un tramo flojo no quede tapado por la burbuja más fuerte. <paramref name="sampleRate"/>: la del bucle.
        /// </summary>
        public static double SeamRatio(float[] loop, int sampleRate = Dsp.SampleRate)
        {
            int n = loop.Length;
            if (n < 3)
            {
                return 0.0;
            }

            int w = Math.Max(2, (int)Math.Round(SeamWindowSeconds * sampleRate));
            w = Math.Min(w, (n - 1) / 2);
            double localStep = 0.0;
            for (int i = 1; i <= w; i++)
            {
                double head = Math.Abs(loop[i] - loop[i - 1]);
                double tail = Math.Abs(loop[n - i] - loop[n - i - 1]);
                localStep = Math.Max(localStep, Math.Max(head, tail));
            }

            double seam = Math.Abs(loop[0] - loop[n - 1]);
            if (localStep <= 0.0)
            {
                return seam <= 0.0 ? 0.0 : double.PositiveInfinity;
            }

            return seam / localStep;
        }

        /// <summary>
        /// Envolvente de un bucle: el RMS de cada ventana de <paramref name="windowSec"/> s, en dB (−200 con silencio). Las
        /// ventanas parten el bucle entero (la última, más corta si no cabe).
        /// </summary>
        public static double[] EnvelopeDb(float[] loop, int sampleRate, double windowSec)
        {
            int w = Math.Max(1, (int)Math.Round(windowSec * sampleRate));
            int count = (loop.Length + w - 1) / w;
            var env = new double[count];
            for (int k = 0; k < count; k++)
            {
                double s = 0.0;
                int end = Math.Min(loop.Length, (k + 1) * w);
                for (int i = k * w; i < end; i++)
                {
                    s += (double)loop[i] * loop[i];
                }

                double rms = Math.Sqrt(s / Math.Max(1, end - (k * w)));
                env[k] = rms > 0.0 ? 20.0 * Math.Log10(rms) : -200.0;
            }

            return env;
        }

        /// <summary>
        /// «Respiración» de un bucle (dB): la desviación típica de su envolvente (<see cref="EnvelopeDb"/>, ventanas de
        /// <paramref name="windowSec"/> s) menos su media móvil circular de <paramref name="smoothSec"/> s, es decir, lo que el
        /// nivel sube y baja más deprisa que <paramref name="smoothSec"/>. Un ruido estacionario da poco (su propio grano); el agua
        /// del laboratorio, que se regeneraba cada 1,05 s con una envolvente seno, mucho. El oleaje lento no cuenta.
        /// </summary>
        public static double FastWobbleDb(float[] loop, int sampleRate, double windowSec, double smoothSec)
        {
            double[] env = EnvelopeDb(loop, sampleRate, windowSec);
            int count = env.Length;
            int half = Math.Max(1, (int)Math.Round(smoothSec / windowSec / 2.0));
            double sum = 0.0;
            double sum2 = 0.0;
            for (int k = 0; k < count; k++)
            {
                double avg = 0.0;
                for (int j = -half; j <= half; j++)
                {
                    avg += env[((k + j) % count + count) % count];
                }

                double d = env[k] - (avg / ((2 * half) + 1));
                sum += d;
                sum2 += d * d;
            }

            double mean = sum / count;
            return Math.Sqrt(Math.Max(0.0, (sum2 / count) - (mean * mean)));
        }

        /// <summary>
        /// Mayor subida de nivel entre dos ventanas seguidas de <paramref name="windowSec"/> s (dB), con la costura incluida: un
        /// ataque (una nota pulsada, un golpe) sube muchos dB de una ventana a la siguiente; un pad que se funde, casi nada.
        /// </summary>
        public static double MaxRiseDb(float[] loop, int sampleRate, double windowSec)
        {
            double[] env = EnvelopeDb(loop, sampleRate, windowSec);
            double rise = 0.0;
            for (int k = 0; k < env.Length; k++)
            {
                double prev = env[(k - 1 + env.Length) % env.Length];
                rise = Math.Max(rise, env[k] - prev);
            }

            return rise;
        }

        /// <summary>Hondura mínima de un hueco (dB por debajo de la mediana de su entorno).</summary>
        public const double DropoutDepthDb = 15.0;

        /// <summary>Lo que dura un hueco como mínimo (ms).</summary>
        public const double DropoutMinMs = 30.0;

        /// <summary>Ventana de la envolvente con la que se buscan los huecos (s).</summary>
        public const double DropoutWindowSeconds = 0.01;

        /// <summary>Entorno con el que se compara cada ventana (s a cada lado, circular: el bucle sigue).</summary>
        public const double DropoutContextSeconds = 1.0;

        /// <summary>Entornos más callados que esto (dBFS) no cuentan: colas y silencios de una capa de percusión.</summary>
        public const double DropoutFloorDb = -60.0;

        /// <summary>
        /// Huecos de un sonido sostenido (un lecho, el agua): tramos de <see cref="DropoutMinMs"/> ms o más en que el nivel (RMS en
        /// ventanas de <see cref="DropoutWindowSeconds"/> s) queda <see cref="DropoutDepthDb"/> dB o más por debajo de la mediana
        /// de su entorno de ±<see cref="DropoutContextSeconds"/> s. En un lecho sin pulso se oyen como un tropiezo de la música
        /// (observación de la prueba de la 10001, 2026-10-31: los lechos de la entrega 1 llevaban hasta 270 ms a −70 dBFS en cada
        /// unión de sus tramos). El mismo detector que <c>tools/audio-check/check_audio.py</c>. Devuelve cuántos hay, el más largo
        /// (ms) y dónde empieza el primero (s; −1 sin huecos).
        /// </summary>
        public static int Dropouts(float[] loop, int sampleRate, out double longestMs, out double firstAtSeconds)
        {
            longestMs = 0.0;
            firstAtSeconds = -1.0;
            double[] env = EnvelopeDb(loop, sampleRate, DropoutWindowSeconds);
            int k = env.Length;
            if (k == 0)
            {
                return 0;
            }

            int half = (int)Math.Round(DropoutContextSeconds / DropoutWindowSeconds);
            int span = Math.Min(k, (2 * half) + 1);
            var scratch = new double[span];
            var ctx = new double[k];
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < span; j++)
                {
                    scratch[j] = env[(((i - half + j) % k) + k) % k];
                }

                Array.Sort(scratch);
                ctx[i] = scratch[span / 2];
            }

            int need = Math.Max(1, (int)Math.Ceiling((DropoutMinMs / (DropoutWindowSeconds * 1000.0)) - 1e-9));
            int count = 0;
            int run = 0;
            for (int i = 0; i <= k; i++)
            {
                bool low = i < k && ctx[i] > DropoutFloorDb && ctx[i] - env[i] >= DropoutDepthDb;
                if (low)
                {
                    run++;
                    continue;
                }

                if (run >= need)
                {
                    count++;
                    longestMs = Math.Max(longestMs, run * DropoutWindowSeconds * 1000.0);
                    if (firstAtSeconds < 0.0)
                    {
                        firstAtSeconds = (i - run) * DropoutWindowSeconds;
                    }
                }

                run = 0;
            }

            return count;
        }

        /// <summary>Huella FNV-1a de 64 bits de las muestras (sus bits exactos): la misma síntesis da la misma huella.</summary>
        public static ulong Hash(float[] buf)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < buf.Length; i++)
            {
                uint bits = (uint)BitConverter.SingleToInt32Bits(buf[i]);
                for (int b = 0; b < 4; b++)
                {
                    h ^= (bits >> (8 * b)) & 0xFFu;
                    h *= 1099511628211UL;
                }
            }

            return h;
        }
    }
}
