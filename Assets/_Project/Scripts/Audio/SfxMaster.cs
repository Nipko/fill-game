#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Masterización de los efectos con los arreglos de la revisión del laboratorio (medidos el 2026-10-16 renderizando sus 61
    /// efectos; tarea AU1), que sustituyen a su <c>finalizeBuffer</c>:
    /// <list type="number">
    /// <item><b>Cola de dosel solo donde va</b> (<see cref="SfxEntry.Canopy"/>: toada, Boto, boca de la Tinaja y piha); el
    /// laboratorio la añadía a todo y un toque de 0,12 s duraba 0,44 s.</item>
    /// <item><b>Graves que el altavoz del teléfono no reproduce:</b> al menos el 50 % de la energía por encima de 300 Hz
    /// (<see cref="MinEnergyAbove300"/>, con margen <see cref="EnergyAbove300Target"/>), subiendo los armónicos 2.º y 3.º del propio
    /// sonido y, si no basta, bajando un poco su parte grave: la madera grave y el barro siguen siéndolo.</item>
    /// <item><b>Gestos repetidos ≤ 0,2 s</b> (<see cref="SfxEntry.Gesture"/>), con un fundido de salida.</item>
    /// <item><b>Cada efecto a su pico objetivo</b> (<see cref="SfxEntry.TargetPeakDb"/>, la columna dBFS del catálogo), sin el
    /// <c>tanh</c> global que igualaba los volúmenes (<c>sfx_select</c> salía a −7 dBFS y <c>sfx_confetti</c>, ~20 dB por debajo
    /// del suyo); solo un limitador suave por encima de −1 dBFS (<see cref="LimiterDb"/>), que con los objetivos de la tabla
    /// (≤ −14 dBFS; 07 §9.4, picos ≤ −6 dBTP) no actúa.</item>
    /// </list>
    /// Determinista. Asigna: solo al construir los clips.
    /// </summary>
    public static class SfxMaster
    {
        /// <summary>Mezcla de la cola de dosel: la del laboratorio con sus controles por defecto (<c>max(0,32; reverb)</c>).</summary>
        public const double CanopyMix = 0.32;

        /// <summary>Regla de la revisión: fracción mínima de energía por encima de 300 Hz.</summary>
        public const double MinEnergyAbove300 = 0.50;

        /// <summary>Lo que se busca al subir los armónicos (deja margen sobre <see cref="MinEnergyAbove300"/>).</summary>
        public const double EnergyAbove300Target = 0.55;

        /// <summary>Umbral del limitador suave (dBFS).</summary>
        public const double LimiterDb = -1.0;

        /// <summary>Fundido de salida al recortar un gesto a 0,2 s (s).</summary>
        public const double GestureFadeSeconds = 0.015;

        /// <summary>Umbral de la cola que se conserva al recortar el silencio final (dB bajo el pico).</summary>
        public const double TailFloorDb = -70.0;

        private const double SR = Dsp.SampleRate;

        // Rejilla de la búsqueda de graves: primero solo armónicos (a = 1), luego también menos grave (a < 1).
        private static readonly double[] HarmonicSteps = { 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 3.0, 4.0 };
        private static readonly double[] LowSteps = { 0.8, 0.65, 0.5, 0.4, 0.3, 0.2, 0.15, 0.1 };
        private static readonly double[] LowHarmonicSteps = { 1.0, 2.0, 3.0, 4.0, 6.0 };

        /// <summary>El efecto <paramref name="id"/> listo para sonar (con la nota de la fruta <paramref name="tint"/> si va afinado).</summary>
        public static float[] Render(SfxId id, int tint = 0)
        {
            SfxEntry e = SfxCatalog.Get(id);
            double hz = e.FruitTuned ? SfxCatalog.FruitFrequency(tint) : 0.0;
            float[] raw = SfxSynth.RenderRaw(id, hz);
            SfxSynth.AddOctaveLayer(id, raw);
            return Master(raw, e.TargetPeakDb, e.Canopy, e.Gesture);
        }

        /// <summary>
        /// Variante de <c>sfx_select</c> al bajar la selección (la lista, §1: «una variante apagada una quinta más grave»,
        /// −35 dBFS) [P]: la misma síntesis una quinta justa por debajo (× 2/3).
        /// </summary>
        public static float[] RenderSelectDown(int tint = 0)
        {
            double hz = SfxCatalog.FruitFrequency(tint);
            double f = (hz > 0.0 ? hz : SfxSynth.SelectDefaultHz) * (2.0 / 3.0);
            SfxEntry e = SfxCatalog.Get(SfxId.Select);
            return Master(SfxSynth.RenderRaw(SfxId.Select, f), SfxCatalog.SelectDownPeakDb, false, e.Gesture);
        }

        /// <summary>Pasa <paramref name="raw"/> por los cuatro arreglos (cola, graves, largo y nivel) hasta <paramref name="targetPeakDb"/>.</summary>
        public static float[] Master(float[] raw, double targetPeakDb, bool canopy, bool gesture)
        {
            float[] buf = canopy ? Dsp.CanopyReverb(raw, CanopyMix) : raw;
            buf = EnsureEnergyAbove(buf, AudioAnalysis.SpeakerCutHz, EnergyAbove300Target);
            buf = Trim(buf, gesture ? SfxCatalog.GestureMaxSeconds : 0.0);
            Normalize(buf, targetPeakDb);
            SoftLimit(buf, LimiterDb);
            return buf;
        }

        /// <summary>
        /// Si menos de <paramref name="target"/> de la energía queda por encima de <paramref name="hz"/>, la sube: suma los
        /// armónicos 2.º y 3.º del propio sonido (x² y x³ filtrados por encima del corte) y, si no basta, atenúa su parte grave.
        /// Devuelve el mismo búfer si ya cumple.
        /// </summary>
        public static float[] EnsureEnergyAbove(float[] buf, double hz, double target)
        {
            AudioAnalysis.Spectrum(buf, out double[] bre, out double[] bim);
            if (AudioAnalysis.FractionAbove(bre, bim, hz) >= target)
            {
                return buf;
            }

            float peak = AudioAnalysis.Peak(buf);
            if (peak <= 0f)
            {
                return buf;
            }

            int n = buf.Length;
            float[] low = Dsp.Biquad(buf, BiquadType.Lowpass, hz, 0.7071);
            var high = new float[n];
            var harm = new float[n];
            for (int i = 0; i < n; i++)
            {
                high[i] = buf[i] - low[i];
                double x = buf[i] / (double)peak;
                harm[i] = (float)(((0.7 * x * x) + (0.5 * x * x * x)) * peak);
            }

            harm = Dsp.Biquad(Dsp.Biquad(harm, BiquadType.Highpass, hz, 0.7071), BiquadType.Highpass, hz, 0.7071);

            AudioAnalysis.Spectrum(low, out double[] lre, out double[] lim);
            AudioAnalysis.Spectrum(high, out double[] hre, out double[] him);
            AudioAnalysis.Spectrum(harm, out double[] gre, out double[] gim);

            double bestA = LowSteps[LowSteps.Length - 1];
            double bestG = LowHarmonicSteps[LowHarmonicSteps.Length - 1];
            bool found = false;
            for (int s = 0; s < HarmonicSteps.Length && !found; s++)
            {
                if (Fraction(lre, lim, hre, him, gre, gim, 1.0, HarmonicSteps[s], hz) >= target)
                {
                    bestA = 1.0;
                    bestG = HarmonicSteps[s];
                    found = true;
                }
            }

            for (int a = 0; a < LowSteps.Length && !found; a++)
            {
                for (int s = 0; s < LowHarmonicSteps.Length && !found; s++)
                {
                    if (Fraction(lre, lim, hre, him, gre, gim, LowSteps[a], LowHarmonicSteps[s], hz) >= target)
                    {
                        bestA = LowSteps[a];
                        bestG = LowHarmonicSteps[s];
                        found = true;
                    }
                }
            }

            var y = new float[n];
            for (int i = 0; i < n; i++)
            {
                y[i] = (float)((bestA * low[i]) + high[i] + (bestG * harm[i]));
            }

            return y;
        }

        // Fracción por encima de hz de a·L + H + g·G, desde los espectros (la FFT es lineal).
        private static double Fraction(double[] lre, double[] lim, double[] hre, double[] him, double[] gre, double[] gim, double a, double g, double hz)
        {
            int n = lre.Length;
            double total = 0.0;
            double above = 0.0;
            for (int k = 0; k <= n / 2; k++)
            {
                double r = (a * lre[k]) + hre[k] + (g * gre[k]);
                double im = (a * lim[k]) + him[k] + (g * gim[k]);
                double w = k == 0 || k == n / 2 ? 1.0 : 2.0;
                double p = ((r * r) + (im * im)) * w;
                total += p;
                if (k * SR / n >= hz)
                {
                    above += p;
                }
            }

            return total <= 0.0 ? 0.0 : above / total;
        }

        /// <summary>
        /// Recorta el silencio final (bajo <see cref="TailFloorDb"/>) y, con <paramref name="maxSeconds"/> &gt; 0, el largo, con un
        /// fundido de salida de <see cref="GestureFadeSeconds"/> si corta sonido. Siempre deja un fundido de 2 ms al final.
        /// </summary>
        public static float[] Trim(float[] buf, double maxSeconds)
        {
            float peak = AudioAnalysis.Peak(buf);
            int end = buf.Length;
            if (peak > 0f)
            {
                double floor = peak * AudioAnalysis.FromDb(TailFloorDb);
                int last = buf.Length - 1;
                while (last > 0 && Math.Abs(buf[last]) < floor)
                {
                    last--;
                }

                end = Math.Min(buf.Length, last + 1 + (int)(0.005 * SR));
            }

            int fade = (int)(0.002 * SR);
            if (maxSeconds > 0.0)
            {
                int maxN = (int)Math.Floor(maxSeconds * SR);
                if (end > maxN)
                {
                    end = maxN;
                    fade = (int)(GestureFadeSeconds * SR);
                }
            }

            var output = new float[end];
            Array.Copy(buf, output, end);
            fade = Math.Min(fade, end / 2);
            for (int i = 0; i < fade; i++)
            {
                int k = end - fade + i;
                double w = 0.5 * (1.0 + Math.Cos(Math.PI * (i + 1) / fade));
                output[k] = (float)(output[k] * w);
            }

            return output;
        }

        /// <summary>Lleva el pico de <paramref name="buf"/> a <paramref name="targetPeakDb"/> dBFS.</summary>
        public static void Normalize(float[] buf, double targetPeakDb)
        {
            float peak = AudioAnalysis.Peak(buf);
            if (peak <= 0f)
            {
                return;
            }

            double gain = AudioAnalysis.FromDb(targetPeakDb) / peak;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (float)(buf[i] * gain);
            }
        }

        /// <summary>Limitador suave: por encima de <paramref name="thresholdDb"/>, una rodilla en tanh hasta 0 dBFS.</summary>
        public static void SoftLimit(float[] buf, double thresholdDb)
        {
            double t = AudioAnalysis.FromDb(thresholdDb);
            double room = 1.0 - t;
            for (int i = 0; i < buf.Length; i++)
            {
                double x = buf[i];
                double a = Math.Abs(x);
                if (a > t)
                {
                    double y = t + (room * Math.Tanh((a - t) / room));
                    buf[i] = (float)(x < 0.0 ? -y : y);
                }
            }
        }
    }
}
