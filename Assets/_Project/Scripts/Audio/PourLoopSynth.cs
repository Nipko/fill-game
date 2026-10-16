#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// <c>sfx_pour_loop</c> como bucle de verdad (revisión del laboratorio, 2026-10-16: allí era un sonido de 0,72 s que subía con
    /// el tiempo): la misma corriente de microburbujas de Minnaert y el mismo murmullo laminar filtrado a 720 Hz, pero con
    /// densidad y tono constantes en un búfer circular de <see cref="LoopSeconds"/> s que se repite sin costura (las burbujas que
    /// pasan del final vuelven al principio y el murmullo se funde consigo mismo). El tono lo pone quien lo reproduce según el
    /// **nivel de la cuia de destino** (02 §4: «el loop sube de tono con el llenado»; la lista, §1: de 380 Hz con 1 dosis a 920 Hz
    /// con 4), con <see cref="LevelForDoses"/> y <see cref="PitchForLevel"/>. Determinista.
    /// </summary>
    public static class PourLoopSynth
    {
        /// <summary>Largo del bucle (s).</summary>
        public const double LoopSeconds = 1.2;

        /// <summary>Frecuencia base de las burbujas con 1 dosis en el destino (Hz; la lista: 380 Hz), y también con 0.</summary>
        public const double BaseHz = 380.0;

        /// <summary>Frecuencia con el destino lleno (Hz; la lista: 920 Hz con 4 dosis en una cuia de 4).</summary>
        public const double FullHz = 920.0;

        /// <summary>Burbujas por segundo: las 34 del laboratorio en sus 0,65 s útiles.</summary>
        public const double BubblesPerSecond = 34.0 / 0.65;

        /// <summary>Semilla del laboratorio para el chorro.</summary>
        public const uint Seed = 20261015;

        private const double SR = Dsp.SampleRate;

        /// <summary>
        /// Nivel del destino para el tono (la lista, §1: «de 380 Hz con 1 dosis a 920 Hz con 4 dosis»): (dosis − 1) / (capacidad − 1),
        /// entre 0 y 1, con <paramref name="doses"/> las dosis que ya se ven dentro (con decimales mientras entra el chorro). Con 0
        /// o 1 dosis, 0 (380 Hz); con la cuia llena, 1 (920 Hz). Una cuia de capacidad 1 llena da 1.
        /// </summary>
        public static float LevelForDoses(float doses, int capacity)
        {
            if (capacity <= 1)
            {
                return doses >= 1f ? 1f : 0f;
            }

            float l = (doses - 1f) / (capacity - 1);
            return l < 0f ? 0f : l > 1f ? 1f : l;
        }

        /// <summary>
        /// Velocidad de reproducción del bucle para un destino en el nivel <paramref name="level"/> (<see cref="LevelForDoses"/>:
        /// 0 con 1 dosis … 1 lleno): 1 a 380 Hz y <see cref="FullHz"/> / <see cref="BaseHz"/> ≈ 2,42 lleno, lineal en la frecuencia.
        /// </summary>
        public static float PitchForLevel(float level)
        {
            double l = level < 0f ? 0.0 : level > 1f ? 1.0 : level;
            return (float)((BaseHz + ((FullHz - BaseHz) * l)) / BaseHz);
        }

        /// <summary>El bucle crudo (sin nivel): <see cref="LoopSeconds"/> s, y el último valor enlaza con el primero.</summary>
        public static float[] RenderRaw()
        {
            int n = (int)Math.Floor(LoopSeconds * SR);
            var buf = new float[n];
            var rng = new XorShift32(Seed);
            int count = (int)Math.Round(BubblesPerSecond * LoopSeconds);
            for (int k = 0; k < count; k++)
            {
                double u = (double)k / count;
                double t = (u * LoopSeconds) + (rng.Next() * 0.012);
                double f = BaseHz + ((rng.Next() - 0.5) * 65.0) + (rng.Next() * 40.0);
                double dDur = 0.032 + (rng.Next() * 0.025);
                Dsp.BubbleWrapped(buf, t, f, dDur, 0.24, 1.42);
            }

            // Murmullo: ruido filtrado más largo que el bucle (n + 2x); lo que sigue al final se funde en el principio (potencia
            // constante), así la última muestra enlaza con la primera.
            int x = (int)Math.Floor(0.1 * SR);
            var noise = new float[n + (2 * x)];
            for (int i = 0; i < noise.Length; i++)
            {
                noise[i] = (float)(((rng.Next() * 2.0) - 1.0) * 0.06);
            }

            float[] m = Dsp.Biquad(noise, BiquadType.Bandpass, 720, 2.2);

            // Se descartan los primeros x del filtro (su arranque) y la ventana [x, x + n) enlaza su final con su principio.
            for (int i = 0; i < n; i++)
            {
                double v;
                if (i < x)
                {
                    double w = (double)i / x;
                    v = (m[x + n + i] * Math.Cos(w * Math.PI * 0.5)) + (m[x + i] * Math.Sin(w * Math.PI * 0.5));
                }
                else
                {
                    v = m[x + i];
                }

                buf[i] = (float)(buf[i] + v);
            }

            return buf;
        }

        /// <summary>El bucle listo para sonar: con su nivel objetivo (<c>sfx_pour_loop</c>, −29 dBFS) y sin graves sobrantes.</summary>
        public static float[] Render()
        {
            float[] buf = RenderRaw();
            buf = SfxMaster.EnsureEnergyAbove(buf, AudioAnalysis.SpeakerCutHz, SfxMaster.EnergyAbove300Target);
            SfxMaster.Normalize(buf, SfxCatalog.Get(SfxId.PourLoop).TargetPeakDb);
            SfxMaster.SoftLimit(buf, SfxMaster.LimiterDb);
            return buf;
        }
    }
}
