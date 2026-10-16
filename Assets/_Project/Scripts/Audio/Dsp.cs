#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Generador pseudoaleatorio xorshift32 del laboratorio (<c>makeRng</c>): la misma semilla da la misma secuencia aquí y en
    /// el navegador. Una semilla 0 pasa a 0x12345678, como allí. Valor (no clase): sin asignaciones.
    /// </summary>
    public struct XorShift32
    {
        private uint _s;

        /// <summary>Semilla como la da el laboratorio: un número que se convierte a uint32 (<c>seed &gt;&gt;&gt; 0</c>).</summary>
        public XorShift32(double seed)
        {
            uint v = ToUint32(seed);
            _s = v == 0u ? 0x12345678u : v;
        }

        /// <summary>Siguiente valor en [0, 1).</summary>
        public double Next()
        {
            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;
            return _s / 4294967296.0;
        }

        // ToUint32 de JavaScript: trunca hacia cero y reduce módulo 2³².
        private static uint ToUint32(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x))
            {
                return 0u;
            }

            double t = x < 0.0 ? Math.Ceiling(x) : Math.Floor(x);
            double m = t - (4294967296.0 * Math.Floor(t / 4294967296.0));
            return (uint)m;
        }
    }

    /// <summary>Tipo de filtro RBJ del laboratorio (<c>biquadFilter</c>).</summary>
    public enum BiquadType
    {
        /// <summary>Paso bajo.</summary>
        Lowpass = 0,

        /// <summary>Paso alto.</summary>
        Highpass = 1,

        /// <summary>Paso banda (ganancia de pico constante 0 dB).</summary>
        Bandpass = 2,
    }

    /// <summary>
    /// Primitivas de síntesis del laboratorio (<c>docs/audio/laboratorio-sonidos.html</c>, sección 3), portadas 1:1 a C# 9 sin
    /// motor: <c>biquadFilter</c>, <c>addBubble</c>, <c>addMarimbaNote</c>, <c>addWoodTap</c>, <c>addMaracaShake</c> y
    /// <c>addLeafRustle</c>, con los controles del laboratorio en su valor por defecto (agua y madera al 100 %). Aritmética en
    /// double, como JavaScript, y muestras en float, como su <c>Float32Array</c>: cada suma se redondea a float al guardarla.
    /// Deterministas (sin <c>System.Random</c>, solo <see cref="XorShift32"/> con las semillas de allí). Solo se usan al construir
    /// los clips, nunca por fotograma.
    /// </summary>
    public static class Dsp
    {
        /// <summary>Frecuencia de muestreo (Hz): 44,1 kHz, mono.</summary>
        public const int SampleRate = 44100;

        private const double SR = SampleRate;
        private const double TwoPi = 2.0 * Math.PI;

        /// <summary>Filtro biquad RBJ sobre una copia (<c>biquadFilter</c>): el estado va en double, como en JavaScript.</summary>
        public static float[] Biquad(float[] buf, BiquadType type, double freq, double q)
        {
            var output = new float[buf.Length];
            BiquadInto(buf, output, type, freq, q);
            return output;
        }

        /// <summary><see cref="Biquad"/> escribiendo en <paramref name="output"/> (del mismo largo).</summary>
        public static void BiquadInto(float[] buf, float[] output, BiquadType type, double freq, double q)
        {
            double w0 = TwoPi * Math.Min(Math.Max(freq, 20.0), SR * 0.45) / SR;
            double cosw0 = Math.Cos(w0);
            double sinw0 = Math.Sin(w0);
            double alpha = sinw0 / (2.0 * Math.Max(q, 0.1));
            double b0, b1, b2, a0, a1, a2;
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

            a0 = 1.0 + alpha;
            a1 = -2.0 * cosw0;
            a2 = 1.0 - alpha;
            b0 /= a0;
            b1 /= a0;
            b2 /= a0;
            a1 /= a0;
            a2 /= a0;
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
        }

        /// <summary>Burbuja de Minnaert (<c>addBubble</c>): gota o menisco que revienta, con el tono que sube al final.</summary>
        public static void Bubble(float[] buf, double startSec, double f0, double durSec, double amp, double chirpFactor = 1.45)
        {
            int i0 = Math.Max(0, (int)Math.Floor(startSec * SR));
            int len = (int)Math.Floor(durSec * SR);
            double phase = 0.0;
            double effAmp = amp;
            for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
            {
                double u = (double)i / len;
                double attack = Math.Min(1.0, i / (0.0025 * SR));
                double decay = Math.Exp(-u * 5.2);
                double f = f0 * (1.0 + ((chirpFactor - 1.0) * Math.Pow(u, 1.85)));
                phase += TwoPi * f / SR;
                buf[i0 + i] = (float)(buf[i0 + i] + (Math.Sin(phase) * attack * decay * effAmp));
            }
        }

        /// <summary>
        /// <see cref="Bubble"/> sobre un búfer circular (el bucle del vertido): lo que pasa del final vuelve al principio, así la
        /// costura no corta ninguna burbuja.
        /// </summary>
        public static void BubbleWrapped(float[] buf, double startSec, double f0, double durSec, double amp, double chirpFactor)
        {
            int n = buf.Length;
            int i0 = Math.Max(0, (int)Math.Floor(startSec * SR));
            int len = (int)Math.Floor(durSec * SR);
            double phase = 0.0;
            for (int i = 0; i < len; i++)
            {
                double u = (double)i / len;
                double attack = Math.Min(1.0, i / (0.0025 * SR));
                double decay = Math.Exp(-u * 5.2);
                double f = f0 * (1.0 + ((chirpFactor - 1.0) * Math.Pow(u, 1.85)));
                phase += TwoPi * f / SR;
                int k = (i0 + i) % n;
                buf[k] = (float)(buf[k] + (Math.Sin(phase) * attack * decay * amp));
            }
        }

        /// <summary>Nota de marimba de chonta / kalimba de calabaza (<c>addMarimbaNote</c>).</summary>
        public static void Marimba(float[] buf, double startSec, double freq, double durSec, double amp, double glassiness = 0.35)
        {
            int i0 = Math.Max(0, (int)Math.Floor(startSec * SR));
            int len = (int)Math.Floor(durSec * SR);
            double effAmp = amp * ((0.65 * 1.0) + (0.35 * 1.0));
            for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
            {
                double t = i / SR;
                double attack = Math.Min(1.0, i / (0.002 * SR));
                double env1 = Math.Exp(-t / (durSec * 0.42));
                double env2 = Math.Exp(-t / (durSec * 0.11));
                double env3 = Math.Exp(-t / 0.007);
                double m1 = Math.Sin(TwoPi * freq * t) * env1;
                double m2 = Math.Sin(TwoPi * (freq * 2.0) * t) * env2 * 0.22 * glassiness;
                double m3 = Math.Sin(TwoPi * (freq * 3.98) * t) * env2 * 0.28 * glassiness;
                double mallet = Math.Sin(TwoPi * (freq * 6.7) * t) * env3 * 0.18;
                buf[i0 + i] = (float)(buf[i0 + i] + ((m1 + m2 + m3 + mallet) * attack * effAmp));
            }
        }

        private static readonly double[] HollowRatios = { 1.0, 1.58, 2.42, 3.85 };
        private static readonly double[] HollowDecays = { 0.032, 0.018, 0.009, 0.004 };
        private static readonly double[] HollowWeights = { 1.0, 0.55, 0.28, 0.12 };
        private static readonly double[] DryRatios = { 1.0, 2.76, 5.40 };
        private static readonly double[] DryDecays = { 0.024, 0.011, 0.005 };
        private static readonly double[] DryWeights = { 1.0, 0.42, 0.18 };

        /// <summary>Toque de madera seca o de calabaza hueca (<c>addWoodTap</c>): resonador modal con un grano de impacto.</summary>
        public static void WoodTap(float[] buf, double startSec, double baseFreq, double durSec, double amp, bool hollow = true)
        {
            int i0 = Math.Max(0, (int)Math.Floor(startSec * SR));
            int len = (int)Math.Floor(durSec * SR);
            var rng = new XorShift32(Math.Floor((baseFreq * 100.0) + (startSec * 1000.0)));
            double[] ratios = hollow ? HollowRatios : DryRatios;
            double[] decays = hollow ? HollowDecays : DryDecays;
            double[] weights = hollow ? HollowWeights : DryWeights;
            for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
            {
                double t = i / SR;
                double attack = Math.Min(1.0, i / (0.0012 * SR));
                double sum = 0.0;
                for (int m = 0; m < ratios.Length; m++)
                {
                    double fDrop = baseFreq * ratios[m] * (1.0 + (0.08 * Math.Exp(-t / 0.004)));
                    sum += Math.Sin(TwoPi * fDrop * t) * Math.Exp(-t / decays[m]) * weights[m];
                }

                double click = ((rng.Next() * 2.0) - 1.0) * Math.Exp(-t / 0.0025) * 0.22;
                buf[i0 + i] = (float)(buf[i0 + i] + ((sum + click) * attack * amp));
            }
        }

        /// <summary>Sacudida de maracá (<c>addMaracaShake</c>): semillas en una calabaza, con su resonancia y el golpe del cuerpo.</summary>
        public static void MaracaShake(float[] buf, double startSec, double durSec, double amp, double pitchMult = 1.0, int seedCount = 26)
        {
            var rng = new XorShift32(Math.Floor((startSec * 9999.0) + seedCount));
            var temp = new float[buf.Length];
            for (int s = 0; s < seedCount; s++)
            {
                double u = (rng.Next() + rng.Next()) * 0.5;
                double tSeed = startSec + (u * durSec * 0.85);
                double fSeed = (2400.0 + (rng.Next() * 3400.0)) * pitchMult;
                double seedDur = 0.008 + (rng.Next() * 0.009);
                int i0 = (int)Math.Floor(tSeed * SR);
                int len = (int)Math.Floor(seedDur * SR);
                double sAmp = (0.4 + (0.6 * Math.Sin(Math.PI * u))) * amp;
                for (int i = 0; i < len && (i0 + i) < temp.Length; i++)
                {
                    double t = i / SR;
                    double env = Math.Exp(-t / 0.0022);
                    double sine = Math.Sin(TwoPi * fSeed * t) * 0.65;
                    double grain = ((rng.Next() * 2.0) - 1.0) * 0.35;
                    temp[i0 + i] = (float)(temp[i0 + i] + ((sine + grain) * env * sAmp));
                }
            }

            float[] shell = Biquad(temp, BiquadType.Bandpass, 3400.0 * pitchMult, 1.8);
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (float)(buf[i] + (double)shell[i]);
            }

            WoodTap(buf, startSec + (durSec * 0.22), 390.0 * pitchMult, 0.04, amp * 0.25, true);
        }

        /// <summary>Roce de hojas, plumas o fibra (<c>addLeafRustle</c>): ráfaga de grano filtrada en banda.</summary>
        public static void LeafRustle(float[] buf, double startSec, double durSec, double amp, double centerFreq = 1800.0, double bandwidthQ = 1.4)
        {
            var rng = new XorShift32(Math.Floor((startSec * 777.0) + centerFreq));
            int i0 = Math.Max(0, (int)Math.Floor(startSec * SR));
            int len = (int)Math.Floor(durSec * SR);
            var raw = new float[len];
            for (int i = 0; i < len; i++)
            {
                double u = (double)i / len;
                double env = Math.Sin(Math.PI * u) * Math.Exp(-u * 1.6);
                double flutter = 0.55 + (0.45 * Math.Sin(TwoPi * (28.0 + (14.0 * u)) * (i / SR)));
                raw[i] = (float)(((rng.Next() * 2.0) - 1.0) * env * flutter * amp);
            }

            float[] filt = Biquad(raw, BiquadType.Bandpass, centerFreq, bandwidthQ);
            for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
            {
                buf[i0 + i] = (float)(buf[i0 + i] + (double)filt[i]);
            }
        }

        /// <summary>
        /// Reverberación de dosel del laboratorio (<c>finalizeBuffer</c> con <c>applyCanopyReverb</c>): cuatro ecos realimentados
        /// a 31, 53, 89 y 137 ms y 0,28 s de cola, con la mezcla <paramref name="mix"/> (0,32 en el laboratorio por defecto). Sin el
        /// <c>tanh</c> final: el nivel lo pone <see cref="SfxMaster"/>.
        /// </summary>
        public static float[] CanopyReverb(float[] buf, double mix)
        {
            int tail = (int)Math.Floor(0.28 * SR);
            var ext = new float[buf.Length + tail];
            Array.Copy(buf, ext, buf.Length);
            int[] delays = { (int)Math.Floor(0.031 * SR), (int)Math.Floor(0.053 * SR), (int)Math.Floor(0.089 * SR), (int)Math.Floor(0.137 * SR) };
            double[] gains = { 0.34 * mix, 0.24 * mix, 0.16 * mix, 0.10 * mix };
            for (int d = 0; d < delays.Length; d++)
            {
                int dl = delays[d];
                double g = gains[d];
                for (int i = dl; i < ext.Length; i++)
                {
                    ext[i] = (float)(ext[i] + (ext[i - dl] * g * 0.65));
                }
            }

            return ext;
        }

        /// <summary>Búfer de <paramref name="seconds"/> s (<c>new Float32Array(Math.floor(s * SR))</c>).</summary>
        public static float[] Buffer(double seconds)
        {
            return new float[(int)Math.Floor(seconds * SR)];
        }
    }
}
