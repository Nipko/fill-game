#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Ambiente provisional por código de cada mundo (tarea AU2; 07 §9.1): la capa de agua con el carácter de su tabla y la capa
    /// de lluvia, cada una un bucle largo que se cierra con un fundido cruzado de potencia constante (<see cref="LoopDsp.CrossfadeLoop"/>),
    /// sin costura y sin pulsaciones. El simulador del laboratorio regeneraba el ruido cada 1,05 s con una envolvente seno y el
    /// agua «respiraba» a ~1 Hz: aquí el ruido es estacionario y lo único que varía despacio es el oleaje o la marea de los
    /// mundos que lo llevan (periodos de 6,7 s o más, un número entero de veces por bucle). Lo que pide cada nivel (lluvia y
    /// fauna) está en <see cref="AmbientPlan"/>. Determinista; se genera en un hilo de trabajo, nunca por fotograma.
    /// </summary>
    public static class AmbientSynth
    {
        /// <summary>Frecuencia de muestreo del ambiente (Hz).</summary>
        public const int SampleRate = Dsp.SampleRate / 2;

        /// <summary>Largo del bucle de agua (s).</summary>
        public const double WaterSeconds = 20.0;

        /// <summary>Largo del bucle de lluvia (s).</summary>
        public const double RainSeconds = 10.0;

        /// <summary>Fundido cruzado con el que se cierra cada bucle (s).</summary>
        public const double FadeSeconds = 2.0;

        /// <summary>
        /// RMS del agua (dBFS) [P]: unos 6 dB por debajo del lecho de música (07 §9.4: música a −20 LUFS y lecho de ambiente a
        /// −26 LUFS; el lecho provisional queda en <see cref="MusicSynth.BedRmsDb"/>).
        /// </summary>
        public const double WaterRmsDb = -39.0;

        /// <summary>RMS de la lluvia, que se suma al agua (dBFS) [P].</summary>
        public const double RainRmsDb = -43.0;

        private const double SR = SampleRate;

        // Carácter del agua de cada mundo (07 §9.1; la lista, §5). Índice 0 = mundo 1; del 10 en adelante, el del 1 [P].
        private static readonly WaterProfile[] Profiles =
        {
            // 1 Estuario: río ancho y lento con marea (vaivén de 10 s).
            new WaterProfile(390, 0.9, 1.00, 1300, 1.2, 0.30, 0.00, 2, 0.22, 0.0, 0, 0, 0.0, 0),
            // 2 Tapajós: olas suaves de playa (oleaje de 6,7 s) con su lavado agudo.
            new WaterProfile(620, 0.8, 0.85, 2600, 1.0, 0.45, 0.00, 3, 0.40, 0.0, 0, 0, 0.0, 0),
            // 3 Manaos: dos corrientes, una grave y una aguda.
            new WaterProfile(330, 1.2, 1.00, 1700, 1.8, 0.55, 0.00, 1, 0.12, 0.0, 0, 0, 0.0, 0),
            // 4 Trapecio: río tras la lluvia, con goteo.
            new WaterProfile(420, 1.1, 0.80, 1800, 1.4, 0.22, 0.00, 0, 0.00, 2.4, 700, 1300, 0.10, 0),
            // 5 Loreto: agua quieta de espejo, con alguna onda.
            new WaterProfile(360, 1.6, 0.55, 950, 2.0, 0.14, 0.00, 0, 0.00, 0.35, 420, 620, 0.07, 0),
            // 6 Oriente: laguna negra de noche.
            new WaterProfile(330, 1.4, 0.60, 760, 1.6, 0.16, 0.00, 1, 0.10, 0.5, 360, 520, 0.06, 0),
            // 7 Madre de Dios: río medio.
            new WaterProfile(430, 1.1, 1.00, 1450, 1.2, 0.35, 0.00, 2, 0.14, 0.0, 0, 0, 0.0, 0),
            // 8 Madidi: cascada lejana (rumor ancho, sin agudos de cerca).
            new WaterProfile(650, 0.5, 0.90, 2100, 0.6, 0.60, 0.00, 0, 0.00, 0.0, 0, 0, 0.0, 2600),
            // 9 Ucayali: vapor que sisea y río.
            new WaterProfile(430, 1.1, 0.90, 1400, 1.2, 0.25, 0.22, 1, 0.10, 0.0, 0, 0, 0.0, 0),
        };

        /// <summary>
        /// El bucle de agua del mundo <paramref name="world"/> (<see cref="WaterSeconds"/> s a <see cref="SampleRate"/> Hz). Los
        /// mundos sin perfil propio (del 10 en adelante, fuera de la 1.0) suenan como el Estuario [P].
        /// </summary>
        public static float[] RenderWater(int world)
        {
            int profile = world >= 1 && world <= Profiles.Length ? world : 1;
            WaterProfile p = Profiles[profile - 1];
            int n = (int)Math.Floor(WaterSeconds * SR);
            int skip = (int)Math.Floor(0.5 * SR);
            int fade = (int)Math.Floor(FadeSeconds * SR);
            var noise = new float[skip + n + fade];
            var rng = new XorShift32(77003.0 + (profile * 131.0));
            for (int i = 0; i < noise.Length; i++)
            {
                noise[i] = (float)((rng.Next() * 2.0) - 1.0);
            }

            float[] low = LoopDsp.Biquad(noise, BiquadType.Bandpass, p.LowHz, p.LowQ, SampleRate);
            float[] high = LoopDsp.Biquad(noise, BiquadType.Bandpass, p.HighHz, p.HighQ, SampleRate);
            float[]? hiss = p.HissGain > 0.0 ? LoopDsp.Biquad(noise, BiquadType.Highpass, 4500, 0.7, SampleRate) : null;
            var sum = new float[noise.Length];
            for (int i = 0; i < sum.Length; i++)
            {
                double v = (low[i] * p.LowGain) + (high[i] * p.HighGain);
                if (hiss != null)
                {
                    v += hiss[i] * p.HissGain;
                }

                sum[i] = (float)v;
            }

            float[] loop = LoopDsp.CrossfadeLoop(sum, skip, n, fade);
            if (p.SwellCycles > 0 && p.SwellDepth > 0.0)
            {
                // Oleaje o marea: un número entero de vaivenes por bucle, así la envolvente también enlaza.
                for (int i = 0; i < n; i++)
                {
                    double swell = 1.0 + (p.SwellDepth * Math.Sin(2.0 * Math.PI * p.SwellCycles * i / n));
                    loop[i] = (float)(loop[i] * swell);
                }
            }

            if (p.LowpassHz > 0)
            {
                loop = LoopDsp.BiquadCircular(loop, BiquadType.Lowpass, p.LowpassHz, 0.7, SampleRate);
            }

            LoopDsp.NormalizeRms(loop, WaterRmsDb);
            if (p.DripsPerSecond > 0.0)
            {
                // Gotas sueltas (goteo de hojas, ondas): instantes al azar, sin ritmo; las que pasan del final vuelven al principio.
                double rms = LoopDsp.Rms(loop);
                int drips = (int)Math.Round(p.DripsPerSecond * WaterSeconds);
                for (int k = 0; k < drips; k++)
                {
                    double t = rng.Next() * WaterSeconds;
                    double f = p.DripLowHz + (rng.Next() * (p.DripHighHz - p.DripLowHz));
                    double amp = rms * p.DripAmp * 10.0 * (0.5 + (0.5 * rng.Next()));
                    LoopDsp.BubbleWrapped(loop, SampleRate, t, f, 0.05 + (rng.Next() * 0.04), amp, 1.35);
                }
            }

            return loop;
        }

        /// <summary>
        /// El bucle de lluvia (<see cref="RainSeconds"/> s a <see cref="SampleRate"/> Hz), el mismo en todos los mundos: el siseo
        /// del laboratorio (ruido en banda a 2,4 kHz), una llovizna de gotas finas y alguna gota gruesa en el agua, al azar.
        /// </summary>
        public static float[] RenderRain()
        {
            int n = (int)Math.Floor(RainSeconds * SR);
            int skip = (int)Math.Floor(0.5 * SR);
            int fade = (int)Math.Floor(FadeSeconds * SR);
            var noise = new float[skip + n + fade];
            var rng = new XorShift32(240003.0);
            for (int i = 0; i < noise.Length; i++)
            {
                noise[i] = (float)((rng.Next() * 2.0) - 1.0);
            }

            float[] hissBand = LoopDsp.Biquad(noise, BiquadType.Bandpass, 2400, 1.4, SampleRate);
            float[] airBand = LoopDsp.Biquad(noise, BiquadType.Bandpass, 5200, 0.9, SampleRate);
            for (int i = 0; i < hissBand.Length; i++)
            {
                hissBand[i] = (float)(hissBand[i] + (airBand[i] * 0.35));
            }

            float[] loop = LoopDsp.CrossfadeLoop(hissBand, skip, n, fade);
            LoopDsp.NormalizeRms(loop, RainRmsDb - 2.0);
            double rms = LoopDsp.Rms(loop);

            // Llovizna: gotas finas y agudas, muy suaves (unas 40 por segundo).
            int fine = (int)Math.Round(40.0 * RainSeconds);
            for (int k = 0; k < fine; k++)
            {
                double t = rng.Next() * RainSeconds;
                double f = 2600.0 + (rng.Next() * 2200.0);
                LoopDsp.BubbleWrapped(loop, SampleRate, t, f, 0.010 + (rng.Next() * 0.010), rms * (0.6 + rng.Next()), 1.3);
            }

            // Gotas gruesas en el agua (las «burbujas» del laboratorio, 520-800 Hz), alguna por segundo.
            int heavy = (int)Math.Round(1.2 * RainSeconds);
            for (int k = 0; k < heavy; k++)
            {
                double t = rng.Next() * RainSeconds;
                double f = 520.0 + (rng.Next() * 280.0);
                LoopDsp.BubbleWrapped(loop, SampleRate, t, f, 0.05, rms * (1.5 + rng.Next()), 1.45);
            }

            LoopDsp.NormalizeRms(loop, RainRmsDb);
            return loop;
        }

        private readonly struct WaterProfile
        {
            public WaterProfile(double lowHz, double lowQ, double lowGain, double highHz, double highQ, double highGain, double hissGain,
                int swellCycles, double swellDepth, double dripsPerSecond, double dripLowHz, double dripHighHz, double dripAmp, double lowpassHz)
            {
                LowHz = lowHz;
                LowQ = lowQ;
                LowGain = lowGain;
                HighHz = highHz;
                HighQ = highQ;
                HighGain = highGain;
                HissGain = hissGain;
                SwellCycles = swellCycles;
                SwellDepth = swellDepth;
                DripsPerSecond = dripsPerSecond;
                DripLowHz = dripLowHz;
                DripHighHz = dripHighHz;
                DripAmp = dripAmp;
                LowpassHz = lowpassHz;
            }

            public double LowHz { get; }

            public double LowQ { get; }

            public double LowGain { get; }

            public double HighHz { get; }

            public double HighQ { get; }

            public double HighGain { get; }

            public double HissGain { get; }

            public int SwellCycles { get; }

            public double SwellDepth { get; }

            public double DripsPerSecond { get; }

            public double DripLowHz { get; }

            public double DripHighHz { get; }

            public double DripAmp { get; }

            public double LowpassHz { get; }
        }
    }
}
