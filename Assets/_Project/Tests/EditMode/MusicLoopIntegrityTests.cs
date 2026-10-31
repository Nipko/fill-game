#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Audio;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La música sin tropiezos (observación de la prueba de la 10001, 2026-10-31: «la música … parece como si se tropezara o
    /// fuera un ruido debajo … le bajamos un poquito»). Los 18 lechos de la entrega 1 llevaban un fundido a silencio en cada
    /// unión de sus tramos (a 0,2, 0,5 y 0,8 del bucle: hasta 270 ms a −65/−80 dBFS en un pad a −25), que dejaba oír el agua
    /// debajo. Se leen los WAV del proyecto tal cual (PCM de 16 bits) y se mide: sin huecos en los lechos, costura sin salto,
    /// pico por debajo de −1 dBFS (también el lecho con sus 3 capas sumadas), sin NaN y sin continua; y el nivel de la música,
    /// 3 dB por debajo de sus clips.
    /// </summary>
    public sealed class MusicLoopIntegrityTests
    {
        private const double PeakMaxDb = -1.0; // especificación, «global»: true_peak_max_dbfs
        private const double DcMax = 0.002; // especificación, «global»: dc_offset_max
        private const double SeamMaxJump = 0.02; // especificación, «global»: loop_seam_max_jump

        private static string AudioPath(string rel)
        {
            return Path.Combine(Application.dataPath, "_Project", "Audio", rel);
        }

        [Test]
        public void Dropouts_CatchAHoleInASustainedPad_AndNotThePadItself()
        {
            const int sr = 48000;
            float[] pad = Pad(4.0, sr);
            Assert.That(AudioAnalysis.Dropouts(pad, sr, out _, out _), Is.EqualTo(0), "un pad sostenido no tiene huecos");

            float[] gap = Pad(4.0, sr);
            int a = 2 * sr;
            int fade = (int)(0.04 * sr);
            int hole = (int)(0.25 * sr);
            for (int i = a - fade; i < a + hole + fade; i++)
            {
                double g = i < a ? (a - i) / (double)fade : i < a + hole ? 0.0 : (i - a - hole) / (double)fade;
                gap[i] = (float)(gap[i] * g);
            }

            Assert.That(AudioAnalysis.Dropouts(gap, sr, out double longest, out double first), Is.EqualTo(1), "el hueco de 250 ms");
            Assert.That(longest, Is.InRange(200.0, 320.0));
            Assert.That(first, Is.InRange(1.9, 2.05));

            // Un hueco corto (20 ms) no cuenta: es el grano de cualquier grabación.
            float[] blip = Pad(4.0, sr);
            for (int i = a; i < a + (int)(0.02 * sr); i++)
            {
                blip[i] = 0f;
            }

            Assert.That(AudioAnalysis.Dropouts(blip, sr, out _, out _), Is.EqualTo(0));
        }

        [Test]
        public void RecordedBeds_EveryWorld_DayAndNight_HaveNoDropouts()
        {
            var bad = new List<string>();
            for (int w = 1; w <= AudioClipIds.Worlds; w++)
            {
                foreach (string suffix in new[] { "_bed.wav", "_bed_night.wav" })
                {
                    string rel = "music/w" + w.ToString("00") + suffix;
                    float[] x = ReadWav16(AudioPath(rel), out int sr);
                    int holes = AudioAnalysis.Dropouts(x, sr, out double longest, out double first);
                    if (holes > 0)
                    {
                        bad.Add(rel + ": " + holes + " hueco(s), el primero a " + first.ToString("0.00") + " s, el más largo de " + longest + " ms");
                    }
                }
            }

            Assert.That(bad, Is.Empty, "lechos con huecos (se oyen como un tropiezo; tools/audio-check/fill_dropouts.py los rellena)");
        }

        [Test]
        public void RecordedLoops_SeamWithoutJump_PeakUnderOneDbfs_NoNaN_NoDc()
        {
            var files = new List<string> { "music/menu.wav", "amb/rain_light.wav", "amb/rain_medium.wav", "amb/rain_heavy.wav" };
            for (int w = 1; w <= AudioClipIds.Worlds; w++)
            {
                string p = "w" + w.ToString("00");
                files.Add("music/" + p + "_bed.wav");
                files.Add("music/" + p + "_bed_night.wav");
                for (int l = 1; l <= AudioClipIds.PercussionLayers; l++)
                {
                    files.Add("music/" + p + "_perc" + l + ".wav");
                }

                files.Add("amb/" + p + "_water.wav");
            }

            foreach (string rel in files)
            {
                float[] x = ReadWav16(AudioPath(rel), out int sr);
                Check(rel, x, sr);
            }
        }

        [Test]
        public void RecordedBed_WithItsThreeLayers_PeaksUnderOneDbfs()
        {
            for (int w = 1; w <= AudioClipIds.Worlds; w++)
            {
                string p = "music/w" + w.ToString("00");
                foreach (string bedName in new[] { "_bed.wav", "_bed_night.wav" })
                {
                    float[] mix = ReadWav16(AudioPath(p + bedName), out int sr);
                    for (int l = 1; l <= AudioClipIds.PercussionLayers; l++)
                    {
                        float[] layer = ReadWav16(AudioPath(p + "_perc" + l + ".wav"), out _);
                        Assert.That(layer.Length, Is.EqualTo(mix.Length), p + ": las capas miden lo mismo que el lecho");
                        for (int i = 0; i < mix.Length; i++)
                        {
                            mix[i] += layer[i];
                        }
                    }

                    Check(p + bedName + " + 3 capas", mix, sr);
                }
            }
        }

        [Test]
        public void SynthFallback_BedAndWater_HaveNoDropouts()
        {
            foreach (bool night in new[] { false, true })
            {
                float[] bed = MusicSynth.RenderBed(1, night);
                Assert.That(AudioAnalysis.Dropouts(bed, MusicSynth.BedSampleRate, out _, out _), Is.EqualTo(0), "lecho por código" + (night ? " (noche)" : string.Empty));
                Check("lecho por código", bed, MusicSynth.BedSampleRate, false);
            }

            float[] water = AmbientSynth.RenderWater(1);
            Assert.That(AudioAnalysis.Dropouts(water, AmbientSynth.SampleRate, out _, out _), Is.EqualTo(0), "agua por código");
        }

        [Test]
        public void MusicLevel_IsThreeDecibelsUnderTheClips_ForMusicAndAmbient_NotForStingers()
        {
            Assert.That(AudioMix.MusicLevelDb, Is.EqualTo(-3f));
            Assert.That(AudioMix.MusicLevel, Is.EqualTo(0.708f).Within(0.001f));
            var mix = new AudioMix();
            Assert.That(mix.MusicGain, Is.EqualTo(1f), "la ganancia de control (la que cuenta la sonda) sigue en 1");
            Assert.That(mix.MusicVolume, Is.EqualTo(AudioMix.MusicLevel).Within(1e-6f), "el lecho, el menú y sus capas");
            Assert.That(mix.AmbientVolume, Is.EqualTo(AudioMix.MusicLevel).Within(1e-6f), "el agua y la lluvia, igual: misma distancia al lecho");
            Assert.That(mix.FaunaVolume, Is.EqualTo(mix.FaunaGain * AudioMix.MusicLevel).Within(1e-6f));
            Assert.That(mix.StingerGain, Is.EqualTo((float)AudioAnalysis.FromDb(AudioMix.StingerGainDb)).Within(1e-6f), "los remates no cambian");

            mix.ModalDuck = true;
            Assert.That(mix.MusicVolume, Is.EqualTo(AudioMix.DuckGain * AudioMix.MusicLevel).Within(1e-6f), "la atenuación, encima");
            mix.Settings = new SoundSettings(true, false, true);
            Assert.That(mix.MusicVolume + mix.AmbientVolume + mix.FaunaVolume, Is.EqualTo(0f), "Música apagada");
        }

        private static void Check(string what, float[] x, int sampleRate, bool recorded = true)
        {
            Assert.That(x.Length, Is.GreaterThan(0), what);
            double sum = 0.0;
            float peak = 0f;
            int bad = 0;
            for (int i = 0; i < x.Length; i++)
            {
                if (float.IsNaN(x[i]) || float.IsInfinity(x[i]))
                {
                    bad++;
                    continue;
                }

                sum += x[i];
                peak = Math.Max(peak, Math.Abs(x[i]));
            }

            Assert.That(bad, Is.EqualTo(0), what + ": sin NaN ni infinitos");
            Assert.That(AudioAnalysis.ToDb(peak), Is.LessThanOrEqualTo((float)PeakMaxDb), what + ": pico ≤ −1 dBFS");
            Assert.That(Math.Abs(sum / x.Length), Is.LessThanOrEqualTo(DcMax), what + ": sin continua");
            Assert.That(AudioAnalysis.SeamRatio(x, sampleRate), Is.LessThanOrEqualTo(1.0 + 1e-3), what + ": costura sin clic (no salta más que la onda que la rodea)");
            if (recorded)
            {
                Assert.That(Math.Abs(x[x.Length - 1] - x[0]), Is.LessThanOrEqualTo(SeamMaxJump), what + ": costura sin salto (especificación)");
            }
        }

        private static float[] Pad(double seconds, int sr)
        {
            var x = new float[(int)(seconds * sr)];
            for (int i = 0; i < x.Length; i++)
            {
                double t = i / (double)sr;
                x[i] = (float)(0.08 * (Math.Sin(2 * Math.PI * 220 * t) + (0.6 * Math.Sin(2 * Math.PI * 330 * t)) + (0.4 * Math.Sin(2 * Math.PI * 440 * t))));
            }

            return x;
        }

        // WAV PCM mono de 16 bits (los de la entrega 1): busca «fmt » y «data» entre los trozos RIFF.
        private static float[] ReadWav16(string path, out int sampleRate)
        {
            Assert.That(File.Exists(path), Is.True, path);
            byte[] b = File.ReadAllBytes(path);
            Assert.That(b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F', Is.True, path + ": RIFF (¿puntero de Git LFS sin descargar?)");
            sampleRate = 0;
            int channels = 0;
            int bits = 0;
            int pos = 12;
            while (pos + 8 <= b.Length)
            {
                string id = new string(new[] { (char)b[pos], (char)b[pos + 1], (char)b[pos + 2], (char)b[pos + 3] });
                int size = BitConverter.ToInt32(b, pos + 4);
                int body = pos + 8;
                if (id == "fmt ")
                {
                    channels = BitConverter.ToInt16(b, body + 2);
                    sampleRate = BitConverter.ToInt32(b, body + 4);
                    bits = BitConverter.ToInt16(b, body + 14);
                }
                else if (id == "data")
                {
                    Assert.That(channels, Is.EqualTo(1), path + ": mono");
                    Assert.That(bits, Is.EqualTo(16), path + ": 16 bits");
                    int n = Math.Min(size, b.Length - body) / 2;
                    var x = new float[n];
                    for (int i = 0; i < n; i++)
                    {
                        x[i] = BitConverter.ToInt16(b, body + (2 * i)) / 32768f;
                    }

                    return x;
                }

                pos = body + size + (size & 1);
            }

            Assert.Fail(path + ": sin trozo «data»");
            return Array.Empty<float>();
        }
    }
}
