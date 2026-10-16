#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Audio
{
    /// <summary>
    /// Bucles de una pieza de música provisional: el lecho y sus capas de percusión, cada una con su frecuencia de muestreo. Las
    /// capas duran un divisor exacto del lecho y suenan desde el mismo instante, así que siguen juntas.
    /// </summary>
    public sealed class MusicLoops
    {
        /// <summary>Crea los bucles.</summary>
        public MusicLoops(float[] bed, int bedSampleRate, float[][] percussion, int percussionSampleRate)
        {
            Bed = bed;
            BedSampleRate = bedSampleRate;
            Percussion = percussion;
            PercussionSampleRate = percussionSampleRate;
        }

        /// <summary>El lecho (o el tema de menú).</summary>
        public float[] Bed { get; }

        /// <summary>Frecuencia de muestreo del lecho (Hz).</summary>
        public int BedSampleRate { get; }

        /// <summary>Las capas de percusión, en el orden en que entran (vacío en el tema de menú).</summary>
        public float[][] Percussion { get; }

        /// <summary>Frecuencia de muestreo de las capas (Hz).</summary>
        public int PercussionSampleRate { get; }

        /// <summary>Muestras en total (memoria PCM = 4 bytes cada una en float).</summary>
        public long Samples
        {
            get
            {
                long s = Bed.Length;
                for (int i = 0; i < Percussion.Length; i++)
                {
                    s += Percussion[i].Length;
                }

                return s;
            }
        }
    }

    /// <summary>
    /// Música adaptativa provisional por código de cada mundo (tarea AU2; 07 §9.3), del simulador de ambiente y música del
    /// laboratorio (sección 7) con los arreglos que pide la tarea:
    /// <list type="bullet">
    /// <item><description>el **lecho** es un pad sostenido, sin ataques ni pulso: los acordes de la escala del mundo del laboratorio
    /// (<c>worldNotes</c>), uno cada <see cref="ChordSeconds"/> s, que se funden despacio (<see cref="ChordFadeSeconds"/> s) en un
    /// bucle de <see cref="BedSeconds"/> s (07 §9.3: 60-90 s). Cada nota es una suma de armónicos 1, 2 y 4 con un número entero de
    /// ciclos por bucle: sin costura, y sin batidos entre notas (solo coinciden octavas exactas). Nada de notas de marimba cada
    /// 1,05 s, que marcaban pulso;</description></item>
    /// <item><description>las **capas de percusión suave** del laboratorio (maracá, madera y tambor grave; las del 25, 50 y 75 %),
    /// hasta <see cref="AudioConfig.MusicPercussionLayersMax"/>, en bucles de <see cref="PercussionSeconds"/> s que dividen el del
    /// lecho: entran con el volumen, nunca con el tempo;</description></item>
    /// <item><description>la **variante nocturna** (Muy difícil): el mismo lecho una octava más grave, como el <c>pitchMult</c> 0,5
    /// del laboratorio, y sin agudos por encima de 1,3 kHz («menos agudos, más graves», 07 §9.3). El peso pasa al segundo
    /// armónico para que el altavoz del teléfono la siga dando: al menos una cuarta parte de su energía queda por encima de
    /// <see cref="AudioAnalysis.SpeakerCutHz"/> (−6 dB como mucho; de día, más de la mitad). Las capas de percusión no cambian:
    /// 07 §9.3 pide la variante del lecho, y las maderas a la mitad (145-170 Hz) ya no salen por ese altavoz.</description></item>
    /// </list>
    /// El tema de menú está en <see cref="MenuThemeSynth"/>. Determinista; se genera en un hilo de trabajo, nunca por fotograma.
    /// </summary>
    public static class MusicSynth
    {
        /// <summary>Pasos de la percusión por bucle (el <c>setInterval</c> de 1,05 s del laboratorio).</summary>
        public const int Steps = 10;

        /// <summary>Paso de la percusión (s).</summary>
        public const double StepSeconds = 1.05;

        /// <summary>Largo de cada golpe de percusión antes de su cola (s).</summary>
        public const double SliceSeconds = 1.25;

        /// <summary>Largo del bucle de cada capa de percusión (s).</summary>
        public const double PercussionSeconds = Steps * StepSeconds;

        /// <summary>Acordes del lecho por bucle.</summary>
        public const int Chords = 6;

        /// <summary>Lo que dura cada acorde (s): uno por vuelta de la percusión.</summary>
        public const double ChordSeconds = PercussionSeconds;

        /// <summary>Largo del bucle del lecho (s): 63 s, dentro de los 60-90 s de 07 §9.3.</summary>
        public const double BedSeconds = Chords * ChordSeconds;

        /// <summary>Fundido entre dos acordes (s): sin ataques.</summary>
        public const double ChordFadeSeconds = 4.0;

        /// <summary>Frecuencia de muestreo del lecho (Hz): el pad no pasa de 2,7 kHz.</summary>
        public const int BedSampleRate = Dsp.SampleRate / 4;

        /// <summary>Frecuencia de muestreo de la percusión (Hz).</summary>
        public const int PercussionSampleRate = Dsp.SampleRate / 2;

        /// <summary>Capas de percusión que se generan (todas las que admite <c>audio_music_percussion_layers</c>).</summary>
        public const int PercussionLayers = AudioConfig.MusicPercussionLayersMax;

        /// <summary>
        /// Pico de la mezcla con todas las capas (dBFS) [P]: 6 dB por debajo de la toada y de los premios (−14 dBFS), para que la
        /// música quede por debajo de los efectos (07 §9.4).
        /// </summary>
        public const double MixPeakDb = -20.0;

        /// <summary>RMS del lecho solo (dBFS) [P], como mucho: si la mezcla pasara de <see cref="MixPeakDb"/>, todo baja a la vez.</summary>
        public const double BedRmsDb = -30.0;

        /// <summary>Pico de cada capa de percusión (dBFS) [P], como mucho.</summary>
        public const double PercussionPeakDb = -24.0;

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

        // Raíz de cada acorde (índice en la escala de 5 notas): recorre las cinco y vuelve sin repetir la primera.
        private static readonly int[] ChordRoots = { 0, 3, 1, 4, 2, 3 };

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
        /// Las tres notas (Hz) del acorde <paramref name="chord"/> (0-5) del mundo <paramref name="world"/>, de día: la raíz y las
        /// notas dos y cuatro pasos más arriba en su escala de cinco (las parejas del laboratorio, <c>notes[step]</c> y
        /// <c>notes[step + 2]</c>, más la siguiente).
        /// </summary>
        public static double[] ChordOf(int world, int chord)
        {
            double[] notes = NotesOf(world);
            int r = ChordRoots[((chord % Chords) + Chords) % Chords];
            return new[] { notes[r], notes[(r + 2) % 5], notes[(r + 4) % 5] };
        }

        /// <summary>El lecho y las capas del mundo <paramref name="world"/> (con <paramref name="night"/>, la variante nocturna).</summary>
        public static MusicLoops Render(int world, bool night)
        {
            float[] bed = RenderBed(world, night);
            var percussion = new float[PercussionLayers][];
            for (int l = 0; l < PercussionLayers; l++)
            {
                percussion[l] = RenderPercussion(l);
            }

            // Si la mezcla completa pasara del pico de la música, todo baja lo mismo (el equilibrio no cambia).
            double peak = MixPeak(bed, percussion);
            double limit = AudioAnalysis.FromDb(MixPeakDb);
            if (peak > limit)
            {
                double g = limit / peak;
                LoopDsp.Scale(bed, g);
                for (int l = 0; l < percussion.Length; l++)
                {
                    LoopDsp.Scale(percussion[l], g);
                }
            }

            return new MusicLoops(bed, BedSampleRate, percussion, PercussionSampleRate);
        }

        /// <summary>
        /// El lecho: pad sostenido de <see cref="BedSeconds"/> s a <see cref="BedSampleRate"/> Hz, con su RMS en
        /// <see cref="BedRmsDb"/>.
        /// </summary>
        public static float[] RenderBed(int world, bool night)
        {
            int n = (int)Math.Round(BedSeconds * BedSampleRate);
            double pitch = night ? 0.5 : 1.0;

            // Las notas distintas (por ciclos enteros en el bucle) y el peso de cada una en cada acorde: la raíz, las otras dos y
            // el bajo, una octava bajo la raíz. Una nota que siguen dos acordes seguidos no se mueve en el fundido.
            var cycles = new List<long>();
            var weights = new List<double[]>();
            for (int k = 0; k < Chords; k++)
            {
                double[] chord = ChordOf(world, k);
                AddVoice(cycles, weights, k, chord[0] * pitch, 1.0);
                AddVoice(cycles, weights, k, chord[1] * pitch, 0.85);
                AddVoice(cycles, weights, k, chord[2] * pitch, 0.7);
                AddVoice(cycles, weights, k, chord[0] * pitch * 0.5, 0.6);
            }

            // Timbre: armónicos 1, 2 y 4 (solo coinciden en octavas exactas: sin batidos). De noche, menos brillo y más cuerpo.
            double h1 = night ? 0.6 : 1.0;
            double h2 = night ? 1.0 : 0.42;
            double h4 = night ? 0.22 : 0.16;
            int voices = cycles.Count;
            var amp = new double[voices];
            var env = new double[Chords];
            var bed = new float[n];
            double chordSamples = n / (double)Chords;
            double fadeSamples = ChordFadeSeconds * BedSampleRate;
            for (int i = 0; i < n; i++)
            {
                ChordEnvelopes(i, chordSamples, fadeSamples, env);
                double s = 0.0;
                for (int v = 0; v < voices; v++)
                {
                    double[] w = weights[v];
                    double e = 0.0;
                    for (int k = 0; k < Chords; k++)
                    {
                        e += env[k] * w[k] * w[k];
                    }

                    amp[v] = Math.Sqrt(e); // potencia constante en el fundido
                    if (amp[v] <= 0.0)
                    {
                        continue;
                    }

                    long c = cycles[v];
                    double phase = 2.0 * Math.PI * (((c * i) % n) / (double)n);
                    s += amp[v] * ((h1 * Math.Sin(phase)) + (h2 * Math.Sin(2.0 * phase)) + (h4 * Math.Sin(4.0 * phase)));
                }

                bed[i] = (float)s;
            }

            LoopDsp.NormalizeRms(bed, BedRmsDb);
            return bed;
        }

        /// <summary>
        /// Capa de percusión <paramref name="layer"/> (0 = maracá, la del 25 % del laboratorio; 1 = madera, la del 50 %; 2 = tambor
        /// grave, la del 75 %): un golpe doble cada 1,05 s con la cola de dosel, en un bucle de <see cref="PercussionSeconds"/> s a
        /// <see cref="PercussionSampleRate"/> Hz y con su pico en <see cref="PercussionPeakDb"/>.
        /// </summary>
        public static float[] RenderPercussion(int layer)
        {
            int n = (int)Math.Round(PercussionSeconds * Dsp.SampleRate);
            var loop = new float[n];
            for (int step = 0; step < Steps; step++)
            {
                int offset = (int)Math.Floor(step * StepSeconds * Dsp.SampleRate);
                float[] slice = Dsp.Buffer(SliceSeconds);
                switch (layer)
                {
                    case 0:
                        Dsp.MaracaShake(slice, 0.05, 0.07, 0.12, 1.0, 12);
                        Dsp.MaracaShake(slice, 0.60, 0.07, 0.10, 1.05, 10);
                        break;
                    case 1:
                        Dsp.WoodTap(slice, 0.32, 340, 0.06, 0.16, false);
                        Dsp.WoodTap(slice, 0.88, 290, 0.06, 0.14, true);
                        break;
                    default:
                        Dsp.WoodTap(slice, 0.05, 135, 0.11, 0.24, true);
                        Dsp.WoodTap(slice, 0.60, 155, 0.10, 0.20, true);
                        break;
                }

                float[] wet = Dsp.CanopyReverb(slice, SfxMaster.CanopyMix);
                for (int i = 0; i < wet.Length; i++)
                {
                    int k = (offset + i) % n;
                    loop[k] = (float)(loop[k] + (double)wet[i]);
                }
            }

            float[] y = LoopDsp.Decimate2(loop, 1.0);
            float peak = AudioAnalysis.Peak(y);
            if (peak > 0f)
            {
                LoopDsp.Scale(y, AudioAnalysis.FromDb(PercussionPeakDb) / peak);
            }

            return y;
        }

        /// <summary>
        /// Pico de la mezcla del lecho con todas las capas, sumadas como suenan: cada muestra del lecho con las de las capas que
        /// caen en su instante (las capas, a otra frecuencia y repetidas <see cref="Chords"/> veces en el bucle del lecho).
        /// </summary>
        public static double MixPeak(float[] bed, float[][] percussion)
        {
            double peak = 0.0;
            int ratio = PercussionSampleRate / BedSampleRate;
            for (int l = 0; l < percussion.Length; l++)
            {
                if (percussion[l].Length * Chords != bed.Length * ratio)
                {
                    throw new ArgumentException("Las capas no dividen el lecho.", nameof(percussion));
                }
            }

            int perLoop = percussion.Length > 0 ? percussion[0].Length : 1;
            for (int i = 0; i < bed.Length * ratio; i++)
            {
                double s = bed[i / ratio];
                for (int l = 0; l < percussion.Length; l++)
                {
                    s += percussion[l][i % perLoop];
                }

                peak = Math.Max(peak, Math.Abs(s));
            }

            return peak;
        }

        // Pesos de los acordes en la muestra i: partición de la unidad con fundidos de coseno alzado centrados en cada cambio de
        // acorde (el del final con el del principio, también).
        private static void ChordEnvelopes(int i, double chordSamples, double fadeSamples, double[] env)
        {
            for (int k = 0; k < env.Length; k++)
            {
                env[k] = 0.0;
            }

            double pos = i / chordSamples;
            int k0 = (int)Math.Floor(pos);
            double into = (pos - k0) * chordSamples; // muestras desde el cambio al acorde k0
            double half = fadeSamples * 0.5;
            if (into < half)
            {
                // Fundido con el acorde anterior: va de 0,5 en el cambio a 1 a media anchura.
                double w = 0.5 + (0.5 * Math.Sin(Math.PI * 0.5 * (into / half)));
                env[k0 % Chords] = w;
                env[(k0 - 1 + Chords) % Chords] = 1.0 - w;
            }
            else if (chordSamples - into < half)
            {
                double w = 0.5 + (0.5 * Math.Sin(Math.PI * 0.5 * ((chordSamples - into) / half)));
                env[k0 % Chords] = w;
                env[(k0 + 1) % Chords] = 1.0 - w;
            }
            else
            {
                env[k0 % Chords] = 1.0;
            }
        }

        private static void AddVoice(List<long> cycles, List<double[]> weights, int chord, double hz, double weight)
        {
            long c = Math.Max(1L, (long)Math.Round(hz * BedSeconds));
            int v = cycles.IndexOf(c);
            if (v < 0)
            {
                cycles.Add(c);
                weights.Add(new double[Chords]);
                v = cycles.Count - 1;
            }

            weights[v][chord] = Math.Max(weights[v][chord], weight);
        }
    }

    /// <summary>
    /// Tema ligero provisional del inicio (y del Mercado cuando exista; tarea AU2): 07 §9.3 pide una cumbia amazónica ligera a
    /// 90-100 BPM para los menús. Aquí, a <see cref="Bpm"/> BPM y en ocho compases: un pad cálido (Do, Sol, La menor y Fa, dos
    /// compases cada uno, sin ataques), el bajo de la cumbia en el 1 y el 3, la maracá en corcheas con el acento a contratiempo y
    /// una frase de marimba de chonta en la pentatónica de Do. Sin voces. Pico en <see cref="MusicSynth.MixPeakDb"/>. Lo sustituye
    /// el tema del compositor (07 §9.5). Determinista.
    /// </summary>
    public static class MenuThemeSynth
    {
        /// <summary>Tempo (07 §9.3: 90-100 BPM).</summary>
        public const double Bpm = 94.0;

        /// <summary>Compases del bucle (de cuatro tiempos).</summary>
        public const int Bars = 8;

        /// <summary>Un tiempo (s).</summary>
        public const double BeatSeconds = 60.0 / Bpm;

        /// <summary>Largo del bucle (s).</summary>
        public const double LoopSeconds = Bars * 4 * BeatSeconds;

        /// <summary>Frecuencia de muestreo (Hz).</summary>
        public const int SampleRate = Dsp.SampleRate / 2;

        // Acordes de dos compases: Do, Sol, La menor, Fa (Hz, en la octava del pad).
        private static readonly double[][] PadChords =
        {
            new[] { 261.63, 329.63, 392.00 },
            new[] { 246.94, 293.66, 392.00 },
            new[] { 220.00, 261.63, 329.63 },
            new[] { 220.00, 261.63, 349.23 },
        };

        // Bajo: raíz y quinta de cada acorde (Hz).
        private static readonly double[][] BassNotes =
        {
            new[] { 130.81, 196.00 },
            new[] { 98.00, 146.83 },
            new[] { 110.00, 164.81 },
            new[] { 87.31, 130.81 },
        };

        // Frase de marimba: (tiempo dentro del bucle, Hz), en la pentatónica de Do.
        private static readonly double[] MelodyBeats = { 0, 1.5, 3, 4, 6, 8, 9.5, 11, 12, 14, 16, 17.5, 19, 20, 22, 24, 25.5, 27, 28, 30 };

        private static readonly double[] MelodyHz =
        {
            659.25, 783.99, 880.00, 783.99, 659.25,
            587.33, 783.99, 659.25, 587.33, 523.25,
            659.25, 783.99, 880.00, 1046.50, 880.00,
            783.99, 659.25, 587.33, 523.25, 587.33,
        };

        /// <summary>El tema (un solo bucle, sin capas de percusión aparte).</summary>
        public static MusicLoops Render()
        {
            int n = (int)Math.Round(LoopSeconds * Dsp.SampleRate);
            int tail = (int)Math.Round(2.0 * Dsp.SampleRate);
            var ext = new float[n + tail];

            // Pad sostenido con un número entero de ciclos por bucle y fundidos de un tiempo entre acordes.
            double chordSamples = n / (double)PadChords.Length;
            double fade = BeatSeconds * Dsp.SampleRate;
            for (int c = 0; c < PadChords.Length; c++)
            {
                for (int v = 0; v < PadChords[c].Length; v++)
                {
                    long cyc = (long)Math.Round(PadChords[c][v] * (n / (double)Dsp.SampleRate));
                    for (int i = 0; i < n; i++)
                    {
                        double e = ChordWeight(i, c, chordSamples, fade, PadChords.Length);
                        if (e <= 0.0)
                        {
                            continue;
                        }

                        double phase = 2.0 * Math.PI * (((cyc * i) % n) / (double)n);
                        ext[i] = (float)(ext[i] + (0.05 * e * (Math.Sin(phase) + (0.4 * Math.Sin(2.0 * phase)) + (0.12 * Math.Sin(4.0 * phase)))));
                    }
                }
            }

            for (int bar = 0; bar < Bars; bar++)
            {
                int chord = (bar / 2) % PadChords.Length;
                double barStart = bar * 4 * BeatSeconds;

                // Bajo de cumbia: raíz en el 1 y quinta en el 3 (con su segundo armónico, que el altavoz sí da).
                Pluck(ext, barStart, BassNotes[chord][0], 0.42, 0.075);
                Pluck(ext, barStart + (2 * BeatSeconds), BassNotes[chord][1], 0.42, 0.06);

                // Maracá en corcheas, más fuerte a contratiempo.
                for (int e8 = 0; e8 < 8; e8++)
                {
                    bool off = (e8 % 2) == 1;
                    Dsp.MaracaShake(ext, barStart + (e8 * BeatSeconds * 0.5), off ? 0.07 : 0.05, off ? 0.05 : 0.03, 1.0, off ? 10 : 7);
                }
            }

            for (int m = 0; m < MelodyBeats.Length; m++)
            {
                Dsp.Marimba(ext, MelodyBeats[m] * BeatSeconds, MelodyHz[m], 0.6, 0.11, 0.4);
            }

            // Lo que suena pasado el final vuelve al principio.
            var loop = new float[n];
            Array.Copy(ext, loop, n);
            for (int i = 0; i < tail; i++)
            {
                loop[i] = (float)(loop[i] + (double)ext[n + i]);
            }

            float[] y = LoopDsp.Decimate2(loop, 1.0);
            float peak = AudioAnalysis.Peak(y);
            if (peak > 0f)
            {
                LoopDsp.Scale(y, AudioAnalysis.FromDb(MusicSynth.MixPeakDb) / peak);
            }

            return new MusicLoops(y, SampleRate, Array.Empty<float[]>(), SampleRate);
        }

        // Peso del acorde c en la muestra i: 1 en su tramo, con medio fundido de seno a cada lado (potencia constante).
        private static double ChordWeight(int i, int c, double chordSamples, double fade, int count)
        {
            double n = chordSamples * count;
            double start = c * chordSamples;
            double d = i - start;
            if (d < 0)
            {
                d += n;
            }

            double half = fade * 0.5;
            if (d >= half && d <= chordSamples - half)
            {
                return 1.0;
            }

            if (d < half)
            {
                return Math.Sin(Math.PI * 0.25 * (1.0 + (d / half)));
            }

            if (d <= chordSamples + half)
            {
                double out_ = d - (chordSamples - half);
                return Math.Cos(Math.PI * 0.25 * (out_ / half));
            }

            double before = n - d; // muestras hasta el comienzo del tramo (fundido de entrada al final del bucle)
            if (before <= half)
            {
                return Math.Sin(Math.PI * 0.25 * (1.0 - (before / half)));
            }

            return 0.0;
        }

        // Nota pulsada del bajo: seno con su segundo armónico, ataque de 8 ms y caída exponencial.
        private static void Pluck(float[] buf, double startSec, double hz, double durSec, double amp)
        {
            double sr = Dsp.SampleRate;
            int i0 = (int)Math.Floor(startSec * sr);
            int len = (int)Math.Floor(durSec * sr);
            for (int i = 0; i < len && (i0 + i) < buf.Length; i++)
            {
                double t = i / sr;
                double attack = Math.Min(1.0, i / (0.008 * sr));
                double decay = Math.Exp(-t / (durSec * 0.35));
                double s = Math.Sin(2.0 * Math.PI * hz * t) + (0.8 * Math.Sin(2.0 * Math.PI * 2.0 * hz * t)) + (0.25 * Math.Sin(2.0 * Math.PI * 3.0 * hz * t));
                buf[i0 + i] = (float)(buf[i0 + i] + (s * attack * decay * amp));
            }
        }
    }
}
