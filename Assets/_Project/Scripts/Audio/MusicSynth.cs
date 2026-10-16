#nullable enable
using System;

namespace AF.Audio
{
    /// <summary>
    /// Lecho de música provisional por código de cada mundo (07 §9.3), del simulador de ambiente y música adaptativa del
    /// laboratorio (sección 7, <c>playAmbienceSlice</c>), sin sus capas de agua y lluvia (son ambiente, 07 §9.1, fuera de esta
    /// tarea): notas largas de marimba en la escala del mundo y las capas de percusión suave que entran al sellar cuias, cada una
    /// en su propio bucle del mismo largo para sonar juntas y entrar o salir con un fundido (07 §9.3: «gana una capa», sube el
    /// volumen, nunca el tempo). Con <c>audio_music_percussion_layers</c> = 2 (08 §9) hay dos capas: maracá (la del 25 % del
    /// laboratorio) y madera (la del 50 %). Cada trozo de 1,25 s se repite cada 1,05 s, como allí, y lleva la cola de dosel; el
    /// bucle tiene <see cref="Steps"/> trozos (dos vueltas a las 5 notas) y se escribe a 22,05 kHz para ahorrar memoria. La
    /// variante nocturna (Muy difícil) y el tema de menú quedan para el compositor [P]. Determinista.
    /// </summary>
    public static class MusicSynth
    {
        /// <summary>Trozos del bucle.</summary>
        public const int Steps = 10;

        /// <summary>Paso entre trozos (s), el <c>setInterval</c> del laboratorio.</summary>
        public const double StepSeconds = 1.05;

        /// <summary>Largo de cada trozo (s).</summary>
        public const double SliceSeconds = 1.25;

        /// <summary>Largo del bucle (s).</summary>
        public const double LoopSeconds = Steps * StepSeconds;

        /// <summary>Frecuencia de muestreo de la música (Hz): la mitad de la de los efectos.</summary>
        public const int SampleRate = Dsp.SampleRate / 2;

        /// <summary>Capas: 0 = lecho, 1 = maracá, 2 = madera.</summary>
        public const int LayerCount = 3;

        /// <summary>
        /// Pico de la mezcla con todas las capas (dBFS) [P]: 6 dB por debajo de la toada y de los premios (−14 dBFS), para que la
        /// música quede por debajo de los efectos (07 §9.4).
        /// </summary>
        public const double MixPeakDb = -20.0;

        private const double SR = Dsp.SampleRate;

        // Escalas por mundo del laboratorio (worldNotes): 1-9 y 13; el resto usa la del 1.
        private static readonly double[][] WorldNotes =
        {
            new[] { 261.63, 329.63, 392.00, 440.00, 523.25 }, // 1 Estuario
            new[] { 293.66, 369.99, 440.00, 554.37, 587.33 }, // 2 Tapajós
            new[] { 220.00, 261.63, 329.63, 392.00, 440.00 }, // 3 Manaos
            new[] { 329.63, 392.00, 440.00, 523.25, 659.25 }, // 4 Trapecio
            new[] { 261.63, 329.63, 392.00, 523.25, 659.25 }, // 5 Loreto
            new[] { 196.00, 261.63, 293.66, 392.00, 440.00 }, // 6 Oriente
            new[] { 293.66, 392.00, 440.00, 523.25, 587.33 }, // 7 Madre de Dios
            new[] { 220.00, 293.66, 329.63, 440.00, 523.25 }, // 8 Madidi
            new[] { 261.63, 329.63, 392.00, 440.00, 587.33 }, // 9 Ucayali
        };

        private static readonly double[] OriginNotes = { 220.00, 261.63, 293.66, 392.00, 440.00 }; // 13 El origen

        /// <summary>Escala del mundo <paramref name="world"/> (la del 1 si el laboratorio no tiene la suya).</summary>
        public static double[] NotesOf(int world)
        {
            if (world == 13)
            {
                return OriginNotes;
            }

            return world >= 1 && world <= WorldNotes.Length ? WorldNotes[world - 1] : WorldNotes[0];
        }

        /// <summary>
        /// Las <see cref="LayerCount"/> capas del mundo <paramref name="world"/>, cada una un bucle de <see cref="LoopSeconds"/> s a
        /// <see cref="SampleRate"/> Hz, con una ganancia común que deja la mezcla completa en <see cref="MixPeakDb"/>.
        /// </summary>
        public static float[][] RenderLayers(int world)
        {
            double[] notes = NotesOf(world);
            int n = (int)Math.Floor(LoopSeconds * SR);
            var layers = new float[LayerCount][];
            for (int l = 0; l < LayerCount; l++)
            {
                layers[l] = new float[n];
            }

            for (int step = 0; step < Steps; step++)
            {
                int offset = (int)Math.Floor(step * StepSeconds * SR);
                for (int l = 0; l < LayerCount; l++)
                {
                    float[] slice = Dsp.Buffer(SliceSeconds);
                    switch (l)
                    {
                        case 0:
                            Dsp.Marimba(slice, 0.05, notes[step % notes.Length], 0.95, 0.18, 0.45);
                            Dsp.Marimba(slice, 0.55, notes[(step + 2) % notes.Length], 0.65, 0.15, 0.45);
                            break;
                        case 1:
                            Dsp.MaracaShake(slice, 0.05, 0.07, 0.12, 1.0, 12);
                            Dsp.MaracaShake(slice, 0.60, 0.07, 0.10, 1.05, 10);
                            break;
                        default:
                            Dsp.WoodTap(slice, 0.32, 340, 0.06, 0.16, false);
                            Dsp.WoodTap(slice, 0.88, 290, 0.06, 0.14, true);
                            break;
                    }

                    float[] wet = Dsp.CanopyReverb(slice, SfxMaster.CanopyMix);
                    float[] dst = layers[l];
                    for (int i = 0; i < wet.Length; i++)
                    {
                        int k = (offset + i) % n;
                        dst[k] = (float)(dst[k] + (double)wet[i]);
                    }
                }
            }

            var mix = new float[n];
            for (int i = 0; i < n; i++)
            {
                double s = 0.0;
                for (int l = 0; l < LayerCount; l++)
                {
                    s += layers[l][i];
                }

                mix[i] = (float)s;
            }

            float peak = AudioAnalysis.Peak(mix);
            double gain = peak > 0f ? AudioAnalysis.FromDb(MixPeakDb) / peak : 1.0;
            var output = new float[LayerCount][];
            for (int l = 0; l < LayerCount; l++)
            {
                output[l] = Decimate(layers[l], gain);
            }

            return output;
        }

        // A la mitad de muestras, con un paso bajo triangular circular (el bucle sigue sin costura) y la ganancia común.
        private static float[] Decimate(float[] x, double gain)
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
    }
}
