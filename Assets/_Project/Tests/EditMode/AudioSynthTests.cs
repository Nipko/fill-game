#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AF.Audio;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Audio provisional por código (tarea AU1; D-026): el port 1:1 del laboratorio (<c>docs/audio/laboratorio-sonidos.html</c>),
    /// el determinismo y los arreglos de su revisión del 2026-10-16 (pico de cada efecto a su objetivo ±1,5 dB, ≥ 50 % de la
    /// energía por encima de 300 Hz, colas de los gestos repetidos ≤ 0,2 s, cola de dosel solo donde va y el bucle del vertido
    /// sin clic en la costura), con el catálogo de la lista (<c>docs/audio/lista-de-sonidos.md</c>).
    /// </summary>
    public sealed class AudioSynthTests
    {
        private static AudioBank? s_bank;

        // Lo que da el laboratorio para cada efecto antes de su finalizeBuffer (largo en muestras, pico y suma de cuadrados), con
        // sus controles por defecto: se sacó ejecutando su synthesizeSfx en Node 22 con finalizeBuffer como identidad
        // (2026-10-16). El port de SfxSynth.RenderRaw debe dar lo mismo.
        private static readonly (string Id, int Length, double Peak, double Energy)[] LabRaw =
        {
            ("sfx_select", 7056, 0.495587438, 84.4340048),
            ("sfx_pour_start", 9702, 0.501104951, 65.3281709),
            ("sfx_pour_loop", 31752, 0.190197513, 57.5386472),
            ("sfx_pour_end", 9702, 0.275033206, 27.3708336),
            ("sfx_illegal", 8820, 0.593683839, 153.123390),
            ("sfx_seal", 24255, 0.900052011, 665.262039),
            ("sfx_river_rise", 19845, 0.187368691, 16.5236326),
            ("sfx_fill", 18522, 0.215221301, 46.9390704),
            ("sfx_fruit_plip", 4410, 0.326870143, 16.1504950),
            ("sfx_tint", 6174, 0.409045219, 75.6924672),
            ("sfx_win_toada", 99225, 1.32162046, 3710.14969),
            ("sfx_confetti", 46305, 0.0685725734, 3.73275238),
            ("sfx_stuck", 68355, 0.460068822, 1238.34411),
            ("sfx_undo", 18522, 0.332864910, 112.457458),
            ("sfx_capi_paddle", 37485, 0.545589447, 301.351950),
            ("sfx_hint", 22932, 0.590564728, 423.786533),
            ("sfx_shuffle", 40572, 0.487941325, 697.829677),
            ("sfx_reveal", 22932, 0.613291323, 419.321766),
            ("sfx_skip", 53802, 0.444493830, 314.595047),
            ("sfx_restart", 22932, 0.612313092, 176.614235),
            ("sfx_river_drop", 44982, 0.457136482, 369.309278),
            ("sfx_seeds_tick", 2866, 0.639724374, 59.0869876),
            ("sfx_basket_open", 36162, 0.806362748, 888.181588),
            ("sfx_streak_up", 36162, 0.562980711, 435.530961),
            ("sfx_streak_shield", 27342, 0.661546290, 242.649032),
            ("sfx_canoe_lost", 27342, 0.497639745, 358.281911),
            ("sfx_canoe_back", 27342, 0.477056861, 397.606401),
            ("sfx_ui_tap", 3969, 0.585383892, 53.8959899),
            ("sfx_ui_back", 4851, 0.548955083, 75.0150600),
            ("sfx_ui_buy", 19404, 0.740956843, 498.998677),
            ("sfx_mech_hoja", 22932, 0.288254738, 72.9078210),
            ("sfx_mech_matapi", 36162, 0.554573357, 271.722619),
            ("sfx_mech_liana", 27342, 0.575199842, 389.119278),
            ("sfx_mech_liana_cuenta", 7056, 0.632389963, 102.618628),
            ("sfx_mech_remolino", 22932, 0.352352768, 108.463291),
            ("sfx_mech_barro", 22932, 0.406251729, 176.954730),
            ("sfx_mech_lluvia", 18522, 0.415270746, 48.9158555),
            ("sfx_mech_pirana", 18522, 0.731434107, 234.372009),
            ("sfx_mech_brote", 20286, 0.426782221, 254.366809),
            ("sfx_mech_flor", 29106, 0.673580110, 550.270102),
            ("sfx_mech_hoja_atada", 22932, 0.577577114, 195.234290),
            ("sfx_tinaja_select", 6615, 0.705411136, 127.744054),
            ("sfx_tinaja_scoop", 14112, 0.377312332, 108.423968),
            ("sfx_tinaja_reveal", 7938, 0.467221290, 46.3918699),
            ("sfx_tinaja_release", 36162, 0.651766121, 269.719783),
            ("sfx_sloth_nod", 16758, 0.334952861, 261.456409),
            ("sfx_sloth_happy", 21168, 0.138775796, 28.9981947),
            ("sfx_sloth_branch", 16758, 0.386583418, 39.7610031),
            ("sfx_sloth_yawn", 27342, 0.134106398, 16.7581422),
            ("sfx_rosa_surface", 44982, 0.322289169, 59.1873805),
            ("sfx_monkey_chirp", 7938, 0.0907809287, 12.0119270),
            ("sfx_fact", 14994, 0.571418405, 356.279126),
            ("sfx_world_banner", 72765, 0.372780174, 1070.93978),
            ("sfx_map_paddle", 36162, 0.426342219, 162.596056),
            ("sfx_map_sample", 21168, 0.717946827, 570.836073),
            ("sfx_map_rosa", 44982, 0.241716877, 33.2929015),
            ("sfx_map_pequepeque", 40572, 0.450227797, 143.342622),
            ("sfx_fruit_new", 31752, 0.657880366, 900.565846),
            ("sfx_mech_card", 16758, 0.547002375, 201.644822),
            ("sfx_morpho_idle", 16758, 0.230192602, 47.5962609),
            ("sfx_store_open", 25578, 0.527327299, 357.160892),
        };

        private static AudioBank Bank => s_bank ??= AudioBank.Generate();

        [Test]
        public void Catalog_Has61Effects_InEnumOrder_WithTheIdsOfTheList()
        {
            Assert.That(Enum.GetValues(typeof(SfxId)).Length, Is.EqualTo(SfxCatalog.Count));
            Assert.That(SfxCatalog.Count, Is.EqualTo(61), "lista-de-sonidos.md: 13 + 17 + 21 + 10");
            string list = File.ReadAllText(LocTables.ProjectPath("docs/audio/lista-de-sonidos.md"), Encoding.UTF8);
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < SfxCatalog.Count; i++)
            {
                SfxEntry e = SfxCatalog.Get((SfxId)i);
                Assert.That((int)e.Id, Is.EqualTo(i), "fila " + i);
                Assert.That(names.Add(e.Name), Is.True, "ID repetido: " + e.Name);
                StringAssert.Contains("`" + e.Name + "`", list, "el ID está en la lista");
                Assert.That(SfxCatalog.TryParse(e.Name, out SfxId back) && back == e.Id, Is.True);
                Assert.That(e.TargetPeakDb, Is.LessThanOrEqualTo(-14f), "07 §9.4: picos ≤ −6 dBTP; ninguno pasa de −14 dBFS");
            }
        }

        [Test]
        public void FruitScale_IsThe14PentatonicNotes_C4ToG6()
        {
            var notes = new List<double>();
            for (int t = 1; t <= SfxCatalog.FruitCount; t++)
            {
                notes.Add(SfxCatalog.FruitFrequency(t));
            }

            notes.Sort();
            Assert.That(notes[0], Is.EqualTo(261.63).Within(0.01), "Huito, C4");
            Assert.That(notes[13], Is.EqualTo(1567.98).Within(0.01), "Guanábana, G6");
            Assert.That(SfxCatalog.FruitFrequency(9), Is.EqualTo(261.63).Within(0.01), "jenipapo, motor 9");
            Assert.That(SfxCatalog.FruitFrequency(4), Is.EqualTo(587.33).Within(0.01), "aguaje, motor 4");
            Assert.That(SfxCatalog.FruitFrequency(0), Is.EqualTo(0.0));
            Assert.That(SfxCatalog.FruitFrequency(15), Is.EqualTo(0.0), "el Barro no es una fruta");
            for (int i = 1; i < notes.Count; i++)
            {
                Assert.That(notes[i], Is.GreaterThan(notes[i - 1]), "14 notas distintas");
            }
        }

        [Test]
        public void RawSynthesis_IsThePortOfTheLab_ForThe61Effects()
        {
            Assert.That(LabRaw.Length, Is.EqualTo(SfxCatalog.Count));
            foreach ((string id, int length, double peak, double energy) in LabRaw)
            {
                Assert.That(SfxCatalog.TryParse(id, out SfxId sfx), Is.True, id);
                float[] raw = SfxSynth.RenderRaw(sfx);
                double p = 0.0;
                double e = 0.0;
                for (int i = 0; i < raw.Length; i++)
                {
                    p = Math.Max(p, Math.Abs((double)raw[i]));
                    e += (double)raw[i] * raw[i];
                }

                Assert.That(raw.Length, Is.EqualTo(length), id + ": largo");
                Assert.That(p, Is.EqualTo(peak).Within(peak * 1e-5), id + ": pico");
                Assert.That(e, Is.EqualTo(energy).Within(energy * 1e-5), id + ": energía");
            }
        }

        [Test]
        public void TheReview_LowEndEffects_WereMostlyBelow300Hz_InTheLab()
        {
            // La revisión del 2026-10-16: entre el 78 % y el 99 % de la energía bajo 300 Hz en estos siete.
            SfxId[] low = { SfxId.Illegal, SfxId.Stuck, SfxId.CanoeLost, SfxId.TinajaSelect, SfxId.UiBack, SfxId.MapPequepeque, SfxId.SlothNod };
            foreach (SfxId id in low)
            {
                double above = AudioAnalysis.EnergyFractionAbove(SfxSynth.RenderRaw(id), 300.0);
                Assert.That(1.0 - above, Is.InRange(0.78, 0.995), SfxCatalog.Name(id) + " en crudo");
            }
        }

        [Test]
        public void Bank_IsDeterministic_SameHashPerClip_AlsoOnAWorkerThread()
        {
            AudioBank a = Bank;
            AudioBank b = Task.Run(() => AudioBank.Generate()).Result;
            Assert.That(AudioBank.ClipCount, Is.EqualTo(89), "61 + 14 notas de select + 14 de tint (bajar la selección no suena, 02 §4.1)");
            for (int i = 0; i < AudioBank.ClipCount; i++)
            {
                Assert.That(AudioAnalysis.Hash(b.Pcm(i)), Is.EqualTo(AudioAnalysis.Hash(a.Pcm(i))), "clip " + i + " (" + SfxCatalog.Name(AudioBank.IdOf(i)) + ")");
            }

            Assert.That(AudioAnalysis.Hash(PourLoopSynth.Render()), Is.EqualTo(AudioAnalysis.Hash(a.Pcm((int)SfxId.PourLoop))));
        }

        [Test]
        public void BankIndices_RoundTrip()
        {
            Assert.That(AudioBank.Index(SfxId.Select, 0), Is.EqualTo((int)SfxId.Select));
            Assert.That(AudioBank.Index(SfxId.Seal, 4), Is.EqualTo((int)SfxId.Seal), "los no afinados ignoran la fruta");
            for (int t = 1; t <= SfxCatalog.FruitCount; t++)
            {
                Assert.That(AudioBank.IdOf(AudioBank.Index(SfxId.Select, t)), Is.EqualTo(SfxId.Select));
                Assert.That(AudioBank.TintOf(AudioBank.Index(SfxId.Select, t)), Is.EqualTo(t));
                Assert.That(AudioBank.IdOf(AudioBank.Index(SfxId.Tint, t)), Is.EqualTo(SfxId.Tint));
                Assert.That(AudioBank.TintOf(AudioBank.Index(SfxId.Tint, t)), Is.EqualTo(t));
            }

            Assert.That(AudioBank.ClipCount, Is.EqualTo(SfxCatalog.Count + (2 * SfxCatalog.FruitCount)), "61 efectos y las 14 frutas de select y de tint; bajar la selección no suena (02 §4.1)");
        }

        [Test]
        public void EveryClip_PeaksAtItsTarget_Within15Db()
        {
            for (int i = 0; i < AudioBank.ClipCount; i++)
            {
                SfxId id = AudioBank.IdOf(i);
                float target = SfxCatalog.Get(id).TargetPeakDb;
                float peak = AudioAnalysis.PeakDb(Bank.Pcm(i));
                Assert.That(peak, Is.EqualTo(target).Within(1.5f), "clip " + i + " " + SfxCatalog.Name(id) + " fruta " + AudioBank.TintOf(i));
                Assert.That(Bank.GainDb(i), Is.EqualTo(peak).Within(0.06f), "gain_db de la sonda");
            }

            // La jerarquía de la lista: el toque, 22 dB por debajo de la toada (en el laboratorio quedaba en unos 4 dB).
            Assert.That(AudioAnalysis.PeakDb(Bank.Pcm((int)SfxId.WinToada)) - AudioAnalysis.PeakDb(Bank.Pcm((int)SfxId.Select)), Is.EqualTo(22f).Within(0.5f));
            Assert.That(AudioAnalysis.PeakDb(Bank.Pcm((int)SfxId.Confetti)), Is.EqualTo(-20f).Within(1.5f), "el confeti sube a su objetivo");
        }

        [Test]
        public void EveryClip_HasHalfItsEnergyAbove300Hz_TheSpeakerOfAPhone()
        {
            for (int i = 0; i < AudioBank.ClipCount; i++)
            {
                double above = AudioAnalysis.EnergyFractionAbove(Bank.Pcm(i), AudioAnalysis.SpeakerCutHz);
                Assert.That(above, Is.GreaterThanOrEqualTo(SfxMaster.MinEnergyAbove300),
                    "clip " + i + " " + SfxCatalog.Name(AudioBank.IdOf(i)) + " fruta " + AudioBank.TintOf(i));
            }
        }

        [Test]
        public void EnergyMeasure_SeesTheSpectrum()
        {
            Assert.That(AudioAnalysis.EnergyFractionAbove(Sine(200.0, 0.5), 300.0), Is.LessThan(0.02), "200 Hz");
            Assert.That(AudioAnalysis.EnergyFractionAbove(Sine(1000.0, 0.5), 300.0), Is.GreaterThan(0.98), "1 kHz");
            float[] mix = Sine(200.0, 0.5);
            float[] high = Sine(1000.0, 0.5);
            for (int i = 0; i < mix.Length; i++)
            {
                mix[i] += high[i];
            }

            Assert.That(AudioAnalysis.EnergyFractionAbove(mix, 300.0), Is.EqualTo(0.5).Within(0.03), "mitad y mitad");
        }

        [Test]
        public void RepeatedGestures_EndWithin02Seconds()
        {
            SfxId[] gestures = { SfxId.Select, SfxId.Tint, SfxId.FruitPlip, SfxId.UiTap, SfxId.UiBack, SfxId.SeedsTick, SfxId.PourStart, SfxId.PourEnd };
            foreach (SfxId id in gestures)
            {
                Assert.That(SfxCatalog.Get(id).Gesture, Is.True, SfxCatalog.Name(id));
            }

            for (int i = 0; i < AudioBank.ClipCount; i++)
            {
                SfxId id = AudioBank.IdOf(i);
                if (!SfxCatalog.Get(id).Gesture)
                {
                    continue;
                }

                float[] pcm = Bank.Pcm(i);
                Assert.That(pcm.Length / (double)Dsp.SampleRate, Is.LessThanOrEqualTo(SfxCatalog.GestureMaxSeconds + 1e-6), SfxCatalog.Name(id) + " " + i);
                Assert.That(Math.Abs(pcm[pcm.Length - 1]), Is.LessThan(1e-4f), "termina en silencio, sin clic: " + i);
            }
        }

        [Test]
        public void OnlyCanopyEffects_GetAReverbTail()
        {
            var canopy = new List<string>();
            for (int i = 0; i < SfxCatalog.Count; i++)
            {
                SfxEntry e = SfxCatalog.Get((SfxId)i);
                if (e.Canopy)
                {
                    canopy.Add(e.Name);
                }
                else if (e.Id != SfxId.PourLoop)
                {
                    float[] raw = SfxSynth.RenderRaw(e.Id);
                    SfxSynth.AddOctaveLayer(e.Id, raw);
                    Assert.That(Bank.Pcm(i).Length, Is.LessThanOrEqualTo(raw.Length), e.Name + ": sin cola añadida");
                }
            }

            Assert.That(canopy, Is.EqualTo(new[] { "sfx_win_toada", "sfx_skip", "sfx_tinaja_reveal", "sfx_world_banner" }), "useCanopyReverb del laboratorio");
        }

        [Test]
        public void PourLoop_IsContinuous_WithoutAClickAtTheSeam_AndFollowsTheLevel()
        {
            float[] loop = Bank.Pcm((int)SfxId.PourLoop);
            Assert.That(loop.Length / (double)Dsp.SampleRate, Is.EqualTo(PourLoopSynth.LoopSeconds).Within(0.001), "un bucle, no un sonido de 0,72 s");
            Assert.That(AudioAnalysis.SeamRatio(loop), Is.LessThanOrEqualTo(1.0), "la costura no salta más que la onda de los 5 ms de cada lado");

            // Sin silencios: el chorro suena entero mientras dura.
            int window = Dsp.SampleRate / 50;
            for (int start = 0; start + window <= loop.Length; start += window)
            {
                double e = 0.0;
                for (int i = start; i < start + window; i++)
                {
                    e += (double)loop[i] * loop[i];
                }

                Assert.That(Math.Sqrt(e / window), Is.GreaterThan(AudioAnalysis.FromDb(-60.0)), "20 ms sin sonido en " + start);
            }

            // Una sola regla (revisión de AU1; la lista, §1 y §9): 380 Hz con 1 dosis y 920 Hz con la cuia llena.
            Assert.That(PourLoopSynth.LevelForDoses(1f, 4), Is.EqualTo(0f), "1 dosis de 4");
            Assert.That(PourLoopSynth.LevelForDoses(0f, 4), Is.EqualTo(0f), "vacía, igual que con 1");
            Assert.That(PourLoopSynth.LevelForDoses(4f, 4), Is.EqualTo(1f), "4 dosis de 4");
            Assert.That(PourLoopSynth.LevelForDoses(2.5f, 4), Is.EqualTo(0.5f).Within(1e-6f), "a mitad del chorro, entre las dos");
            Assert.That(PourLoopSynth.LevelForDoses(6f, 6), Is.EqualTo(1f), "llena con cualquier capacidad");
            Assert.That(PourLoopSynth.LevelForDoses(1f, 1), Is.EqualTo(1f), "una cuia de 1, llena");
            Assert.That(PourLoopSynth.BaseHz * PourLoopSynth.PitchForLevel(PourLoopSynth.LevelForDoses(1f, 4)), Is.EqualTo(380.0).Within(0.01), "380 Hz con 1 dosis");
            Assert.That(PourLoopSynth.BaseHz * PourLoopSynth.PitchForLevel(PourLoopSynth.LevelForDoses(4f, 4)), Is.EqualTo(920.0).Within(0.05), "920 Hz con 4 dosis");
            Assert.That(PourLoopSynth.PitchForLevel(0f), Is.EqualTo(1f).Within(1e-6f), "380 Hz en el nivel 0");
            Assert.That(PourLoopSynth.PitchForLevel(1f), Is.EqualTo(920f / 380f).Within(1e-4f), "920 Hz lleno");
            Assert.That(PourLoopSynth.PitchForLevel(0.5f), Is.GreaterThan(PourLoopSynth.PitchForLevel(0.25f)), "sube con el nivel");
            Assert.That(PourLoopSynth.PitchForLevel(2f), Is.EqualTo(PourLoopSynth.PitchForLevel(1f)));
        }

        [Test]
        public void SeamRatio_CatchesAClickInAQuietStretch_EvenWithALoudBubbleElsewhere()
        {
            // Revisión de AU1: un bucle con una burbuja fuerte en medio y un final flojo que salta al volver al principio. Contra el
            // mayor salto de todo el bucle (1,0, la burbuja), el clic pasaría (≈ 0,05); contra los 5 ms que lo rodean, no.
            int n = Dsp.SampleRate / 2;
            var loop = new float[n];
            for (int i = 0; i < n; i++)
            {
                loop[i] = 0.01f * (float)Math.Sin(2.0 * Math.PI * 440.0 * i / Dsp.SampleRate);
            }

            for (int i = n / 2; i < (n / 2) + 200; i++)
            {
                loop[i] += (i % 2 == 0) ? 0.5f : -0.5f; // la burbuja fuerte
            }

            // El final se va despacio a +0,05 (en 50 ms, antes de la ventana): al volver al principio salta 0,05 de golpe.
            int ramp = Dsp.SampleRate / 20;
            int from = (3 * n) / 4;
            for (int i = from; i < n; i++)
            {
                float k = Math.Min(1f, (i - from) / (float)ramp);
                loop[i] += 0.05f * k * k * (3f - (2f * k));
            }

            Assert.That(Math.Abs(loop[n - 1] - loop[n - 2]), Is.LessThan(0.001f), "dentro del final, sin saltos");
            Assert.That(AudioAnalysis.SeamRatio(loop), Is.GreaterThan(1.0), "el clic se detecta");

            // La misma onda sin el clic: una sinusoide entera de 440 Hz no salta más en la costura que a su lado.
            var clean = new float[Dsp.SampleRate / 10]; // 44 ciclos enteros: el principio sigue al final
            for (int i = 0; i < clean.Length; i++)
            {
                clean[i] = 0.01f * (float)Math.Sin(2.0 * Math.PI * 440.0 * i / Dsp.SampleRate);
            }

            Assert.That(AudioAnalysis.SeamRatio(clean), Is.LessThanOrEqualTo(1.0 + 1e-3), "sin clic, pasa (el salto de la costura es el de un paso de la onda)");
        }

        [Test]
        public void MusicBed_LoopsWithoutASeam_AndSitsUnderTheEffects()
        {
            float[][] layers = MusicSynth.RenderLayers(1);
            Assert.That(layers.Length, Is.EqualTo(MusicSynth.LayerCount));
            int n = layers[0].Length;
            Assert.That(n / (double)MusicSynth.SampleRate, Is.EqualTo(MusicSynth.LoopSeconds).Within(0.001));
            var mix = new float[n];
            for (int l = 0; l < layers.Length; l++)
            {
                Assert.That(layers[l].Length, Is.EqualTo(n), "las capas, del mismo largo: suenan juntas");
                Assert.That(AudioAnalysis.SeamRatio(layers[l], MusicSynth.SampleRate), Is.LessThanOrEqualTo(1.0), "capa " + l + ": la costura no salta más que lo que la rodea");
                for (int i = 0; i < n; i++)
                {
                    mix[i] += layers[l][i];
                }
            }

            Assert.That(AudioAnalysis.PeakDb(mix), Is.LessThanOrEqualTo((float)MusicSynth.MixPeakDb + 0.5f), "la música, por debajo de los premios (07 §9.4)");
            Assert.That(MusicSynth.NotesOf(13)[0], Is.EqualTo(220.0), "El origen");
            Assert.That(MusicSynth.NotesOf(11), Is.SameAs(MusicSynth.NotesOf(1)), "sin escala propia, la del Estuario");
        }

        [Test]
        public void SoundSettings_Default_AreTheDefaultsOf08Section9()
        {
            SoundSettings d = SoundSettings.Defaults;
            Assert.That(d.Sound && d.Music && d.Haptics, Is.True, "audio_sfx_default_on, audio_music_default_on y ui_haptics_default_on = true");
            var profileSettings = new AF.Save.SettingsData();
            Assert.That(profileSettings.Sound, Is.EqualTo(d.Sound));
            Assert.That(profileSettings.Music, Is.EqualTo(d.Music));
            Assert.That(profileSettings.Haptics, Is.EqualTo(d.Haptics));
        }

        private static float[] Sine(double hz, double seconds)
        {
            var buf = new float[(int)(seconds * Dsp.SampleRate)];
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (float)(0.5 * Math.Sin(2.0 * Math.PI * hz * i / Dsp.SampleRate));
            }

            return buf;
        }
    }
}
