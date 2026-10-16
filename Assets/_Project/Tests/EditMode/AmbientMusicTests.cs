#nullable enable
using System;
using System.Collections.Generic;
using AF.Audio;
using AF.Core.Board;
using AF.Presentation;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Ambiente por mundo y música adaptativa provisional (tarea AU2; 07 §9.1, §9.3 y §9.4; 08 §9): bucles sin clic en la
    /// costura y sin «respiración», lecho sin ataques ni pulso, variante nocturna más grave, capas según las cuias selladas y
    /// <c>audio_music_percussion_layers</c>, lluvia por nivel con <c>audio_ambient_rain_scale</c>, fauna cada 20-45 s sin la
    /// piha, atenuación y silencio en anuncios para música y ambiente, la sonda de la música y ninguna asignación por fotograma.
    /// </summary>
    public sealed class AmbientMusicTests
    {
        // Lo que el nivel puede subir y bajar más deprisa que 1,5 s en un ruido estacionario (dB): su propio grano.
        private const double MaxWobbleDb = 1.0;

        // Parte mínima de la energía del lecho nocturno por encima de AudioAnalysis.SpeakerCutHz: 10·log10(0,25) ≈ −6 dB.
        private const double NightSpeakerFraction = 0.25;

        [TearDown]
        public void TearDown()
        {
            AudioConfig.Reset();
            AudioCues.Install(null);
        }

        [Test]
        public void Water_EveryWorld_LoopsWithoutASeam_WithoutBreathing_AndBelowTheBed()
        {
            var hashes = new HashSet<ulong>();
            for (int world = 1; world <= 9; world++)
            {
                float[] water = AmbientSynth.RenderWater(world);
                int sr = AmbientSynth.SampleRate;
                Assert.That(water.Length / (double)sr, Is.EqualTo(AmbientSynth.WaterSeconds).Within(0.001), "mundo " + world + ": un bucle largo");
                Assert.That(AudioAnalysis.SeamRatio(water, sr), Is.LessThanOrEqualTo(1.0), "mundo " + world + ": sin clic en la costura");
                Assert.That(AudioAnalysis.FastWobbleDb(water, sr, 0.05, 1.5), Is.LessThan(MaxWobbleDb), "mundo " + world + ": sin respirar a ~1 Hz");
                Assert.That(AudioAnalysis.ToDb(LoopDsp.Rms(water)), Is.EqualTo((float)AmbientSynth.WaterRmsDb).Within(1.5f), "mundo " + world + ": a su nivel");
                Assert.That(AudioAnalysis.EnergyFractionAbove(water, AudioAnalysis.SpeakerCutHz, sr), Is.GreaterThanOrEqualTo(0.5), "mundo " + world + ": lo da el altavoz del teléfono");
                Assert.That(hashes.Add(AudioAnalysis.Hash(water)), Is.True, "mundo " + world + ": su propia agua");
            }

            Assert.That(AudioAnalysis.Hash(AmbientSynth.RenderWater(3)), Is.EqualTo(AudioAnalysis.Hash(AmbientSynth.RenderWater(3))), "determinista");
            Assert.That(AudioAnalysis.Hash(AmbientSynth.RenderWater(12)), Is.EqualTo(AudioAnalysis.Hash(AmbientSynth.RenderWater(1))), "sin perfil propio, el del Estuario");

            // 07 §9.4: el lecho de ambiente, unos 6 dB por debajo de la música (−26 frente a −20 LUFS).
            MusicLoops music = MusicSynth.Render(1, false);
            double bedRms = AudioAnalysis.ToDb(LoopDsp.Rms(music.Bed));
            Assert.That(AmbientSynth.WaterRmsDb, Is.LessThanOrEqualTo(bedRms - 5.0), "el agua, por debajo del lecho de música");
        }

        [Test]
        public void FastWobble_CatchesTheLabsBreathingWater()
        {
            // El simulador del laboratorio: un trozo de ruido nuevo cada 1,05 s con una envolvente seno de 1,25 s.
            int sr = AmbientSynth.SampleRate;
            var lab = new float[(int)(10.5 * sr)];
            var rng = new XorShift32(1000);
            int slice = (int)(1.25 * sr);
            for (int step = 0; step < 10; step++)
            {
                int offset = (int)(step * 1.05 * sr);
                for (int i = 0; i < slice; i++)
                {
                    double fade = Math.Sin(Math.PI * i / slice);
                    int k = (offset + i) % lab.Length;
                    lab[k] = (float)(lab[k] + (((rng.Next() * 2.0) - 1.0) * fade * 0.1));
                }
            }

            Assert.That(AudioAnalysis.FastWobbleDb(lab, sr, 0.05, 1.5), Is.GreaterThan(2.0 * MaxWobbleDb), "el agua del laboratorio respira y la medida lo ve");
        }

        [Test]
        public void Rain_LoopsWithoutASeam_WithoutBreathing_AndUnderTheWater()
        {
            float[] rain = AmbientSynth.RenderRain();
            int sr = AmbientSynth.SampleRate;
            Assert.That(rain.Length / (double)sr, Is.EqualTo(AmbientSynth.RainSeconds).Within(0.001));
            Assert.That(AudioAnalysis.SeamRatio(rain, sr), Is.LessThanOrEqualTo(1.0), "sin clic en la costura");
            Assert.That(AudioAnalysis.FastWobbleDb(rain, sr, 0.05, 1.5), Is.LessThan(MaxWobbleDb), "sin pulsaciones");
            Assert.That(AudioAnalysis.ToDb(LoopDsp.Rms(rain)), Is.EqualTo((float)AmbientSynth.RainRmsDb).Within(0.5f));
            Assert.That(AmbientSynth.RainRmsDb, Is.LessThan(AmbientSynth.WaterRmsDb), "la lluvia se suma al agua, por debajo");
        }

        [Test]
        public void Rain_ProbabilityPerWorld_IsTheTableOf07_DeterministicPerLevel_AndScaled()
        {
            int[] percent = { 20, 10, 25, 50, 30, 40, 20, 30, 20, 15, 10, 10, 10 };
            for (int world = 1; world <= 13; world++)
            {
                Assert.That(AmbientPlan.RainProbability(world), Is.EqualTo(percent[world - 1] / 100.0).Within(1e-9), "07 §9.1, mundo " + world);
            }

            Assert.That(AmbientPlan.RainProbability(0), Is.EqualTo(0.0));
            Assert.That(AmbientConfigDefaultsOf08(), Is.True);
            for (int world = 1; world <= 9; world++)
            {
                int rainy = 0;
                const int Levels = 20000;
                for (int level = 1; level <= Levels; level++)
                {
                    bool rain = AmbientPlan.HasRain(level, world, 1.0);
                    Assert.That(AmbientPlan.HasRain(level, world, 1.0), Is.EqualTo(rain), "siempre igual para el mismo nivel");
                    if (rain)
                    {
                        rainy++;
                    }
                }

                Assert.That(rainy / (double)Levels, Is.EqualTo(AmbientPlan.RainProbability(world)).Within(0.02), "mundo " + world + ": su probabilidad por nivel");
            }

            int doubled = 0;
            for (int level = 81; level <= 140; level++)
            {
                Assert.That(AmbientPlan.HasRain(level, 4, 0.0), Is.False, "audio_ambient_rain_scale = 0: nunca llueve");
                doubled += AmbientPlan.HasRain(level, 4, 2.0) ? 1 : 0;
                if (AmbientPlan.HasRain(level, 4, 1.0))
                {
                    Assert.That(AmbientPlan.HasRain(level, 4, 1.5), Is.True, "subir la escala nunca quita la lluvia de un nivel");
                }
            }

            Assert.That(doubled, Is.EqualTo(60), "mundo 4 (50 %) con la escala al doble: llueve en todos");
            Assert.That(AmbientPlan.HasRain(95, 4, 9.0), Is.EqualTo(AmbientPlan.HasRain(95, 4, 2.0)), "la escala se recorta a 2 (08 §9)");
        }

        [Test]
        public void Fauna_Every20To45Seconds_WithTheSoundsThatFit_NeverThePiha_AndTheSameForTheSameLevel()
        {
            for (int world = 1; world <= 13; world++)
            {
                foreach (SfxId id in AmbientPlan.FaunaOf(world))
                {
                    Assert.That(id, Is.Not.EqualTo(SfxId.WorldBanner), "la piha solo va en el banner (la lista, §7)");
                }
            }

            Assert.That(AmbientPlan.FaunaOf(6), Is.Empty, "Oriente: ranas, grillos y búho, sin efecto que encaje [P]");
            Assert.That(AmbientPlan.FaunaOf(0), Is.Empty);

            var clock = new FaunaClock();
            clock.Reset(3, 58);
            var gaps = new List<float>();
            var ids = new List<SfxId>();
            float since = 0f;
            for (int frame = 0; frame < 60 * 60 * 10; frame++)
            {
                since += 100f;
                if (clock.Advance(100f, out SfxId id))
                {
                    gaps.Add(since);
                    ids.Add(id);
                    since = 0f;
                }
            }

            Assert.That(gaps.Count, Is.GreaterThanOrEqualTo(60000 * 60 / 45000 - 1), "una hora: al menos uno cada 45 s");
            foreach (float gap in gaps)
            {
                Assert.That(gap, Is.InRange(AmbientPlan.FaunaMinMs, AmbientPlan.FaunaMaxMs + 100f), "07 §9.1: cada 20-45 s");
            }

            Assert.That(ids.Contains(SfxId.MapRosa) && ids.Contains(SfxId.Confetti), Is.True, "Manaos: botos y guacamayos");

            var again = new FaunaClock();
            again.Reset(3, 58);
            var other = new FaunaClock();
            other.Reset(3, 59);
            var same = new FaunaClock();
            same.Reset(3, 58);
            Assert.That(again.LeftMs, Is.EqualTo(same.LeftMs), "el mismo nivel, la misma espera");
            Assert.That(other.LeftMs, Is.Not.EqualTo(again.LeftMs), "otro nivel, otra");

            var quiet = new FaunaClock();
            quiet.Reset(6, 230);
            Assert.That(quiet.Advance(3600000f, out _), Is.False, "sin animales en el mundo, nunca suena");
        }

        [Test]
        public void Bed_EveryWorld_DayAndNight_LoopsWithoutASeam_WithoutAttacksOrPulse()
        {
            int sr = MusicSynth.BedSampleRate;
            for (int world = 1; world <= 9; world++)
            {
                float[] day = MusicSynth.RenderBed(world, false);
                float[] night = MusicSynth.RenderBed(world, true);
                Assert.That(day.Length / (double)sr, Is.EqualTo(MusicSynth.BedSeconds).Within(0.001));
                Assert.That(MusicSynth.BedSeconds, Is.InRange(60.0, 90.0), "07 §9.3: lecho de 60-90 s");
                foreach (float[] bed in new[] { day, night })
                {
                    string what = "mundo " + world + (bed == night ? " (noche)" : string.Empty);
                    Assert.That(AudioAnalysis.SeamRatio(bed, sr), Is.LessThanOrEqualTo(1.0 + 1e-3), what + ": sin clic en la costura");
                    Assert.That(AudioAnalysis.MaxRiseDb(bed, sr, 0.05), Is.LessThan(2.0), what + ": sin ataques (ninguna nota pulsada)");
                    Assert.That(AudioAnalysis.FastWobbleDb(bed, sr, 0.05, 1.5), Is.LessThan(0.6), what + ": sin pulso");
                }

                // 07 §9.3: la variante nocturna, «menos agudos, más graves».
                Assert.That(AudioAnalysis.EnergyFractionAbove(night, 500, sr), Is.LessThan(AudioAnalysis.EnergyFractionAbove(day, 500, sr)), "mundo " + world + ": más grave de noche");
                Assert.That(AudioAnalysis.EnergyFractionAbove(night, 1300, sr), Is.LessThan(0.005), "mundo " + world + ": sin agudos de noche");

                // Y el altavoz del teléfono la sigue dando: lo que pasa de SpeakerCutHz queda, como mucho, 6 dB por debajo del
                // lecho entero (una cuarta parte de su energía). Hoy va del 27 % (Oriente) al 65 % (Trapecio); de día, 54-91 %.
                Assert.That(AudioAnalysis.EnergyFractionAbove(night, AudioAnalysis.SpeakerCutHz, sr), Is.GreaterThanOrEqualTo(NightSpeakerFraction), "mundo " + world + ": el altavoz da la noche");
                Assert.That(AudioAnalysis.Hash(night), Is.Not.EqualTo(AudioAnalysis.Hash(day)));
            }

            Assert.That(MusicSynth.NotesOf(13)[0], Is.EqualTo(220.0), "El origen");
            Assert.That(MusicSynth.NotesOf(11), Is.SameAs(MusicSynth.NotesOf(1)), "sin escala propia, la del Estuario");
            Assert.That(MusicSynth.ChordOf(1, 0), Is.EqualTo(new[] { 261.63, 392.00, 523.25 }), "los acordes salen de la escala del laboratorio");
        }

        [Test]
        public void MaxRise_CatchesTheLabsMarimbaBed()
        {
            // El lecho del laboratorio (y de AU1): dos notas de marimba cada 1,05 s, que marcan pulso.
            var bed = new float[(int)(10.5 * Dsp.SampleRate)];
            double[] notes = MusicSynth.NotesOf(1);
            for (int step = 0; step < 10; step++)
            {
                Dsp.Marimba(bed, (step * 1.05) + 0.05, notes[step % 5], 0.95, 0.18, 0.45);
                Dsp.Marimba(bed, (step * 1.05) + 0.55, notes[(step + 2) % 5], 0.65, 0.15, 0.45);
            }

            Assert.That(AudioAnalysis.MaxRiseDb(bed, Dsp.SampleRate, 0.05), Is.GreaterThan(6.0), "cada nota es un ataque y la medida lo ve");
        }

        [Test]
        public void Percussion_LayersLoopWithTheBed_AndTheMixSitsUnderTheEffects()
        {
            MusicLoops loops = MusicSynth.Render(4, false);
            Assert.That(loops.Percussion.Length, Is.EqualTo(AudioConfig.MusicPercussionLayersMax), "las 3 capas que admite 08 §9");
            for (int l = 0; l < loops.Percussion.Length; l++)
            {
                float[] layer = loops.Percussion[l];
                Assert.That(layer.Length / (double)loops.PercussionSampleRate, Is.EqualTo(MusicSynth.PercussionSeconds).Within(0.001));
                Assert.That(AudioAnalysis.SeamRatio(layer, loops.PercussionSampleRate), Is.LessThanOrEqualTo(1.0), "capa " + (l + 1) + ": sin clic");
            }

            double bedSeconds = loops.Bed.Length / (double)loops.BedSampleRate;
            Assert.That(bedSeconds / MusicSynth.PercussionSeconds, Is.EqualTo(MusicSynth.Chords).Within(1e-6), "las capas dividen el lecho: siguen juntas");
            Assert.That(AudioAnalysis.ToDb(MusicSynth.MixPeak(loops.Bed, loops.Percussion)), Is.LessThanOrEqualTo((float)MusicSynth.MixPeakDb + 0.01f), "toda la música, por debajo de los premios (07 §9.4)");

            MusicLoops night = MusicSynth.Render(4, true);
            Assert.That(night.Percussion.Length, Is.EqualTo(loops.Percussion.Length), "de noche, las mismas capas (07 §9.3 cambia el lecho)");
            Assert.That(AudioAnalysis.ToDb(MusicSynth.MixPeak(night.Bed, night.Percussion)), Is.LessThanOrEqualTo((float)MusicSynth.MixPeakDb + 0.01f));
            Assert.That(AudioAnalysis.Hash(MusicSynth.Render(4, false).Bed), Is.EqualTo(AudioAnalysis.Hash(loops.Bed)), "determinista");
        }

        [Test]
        public void MenuTheme_IsALightLoopAt90To100Bpm_WithoutASeam()
        {
            MusicLoops theme = MenuThemeSynth.Render();
            Assert.That(MenuThemeSynth.Bpm, Is.InRange(90.0, 100.0), "07 §9.3: cumbia ligera a 90-100 BPM");
            Assert.That(theme.Percussion, Is.Empty);
            Assert.That(theme.Bed.Length / (double)theme.BedSampleRate, Is.EqualTo(MenuThemeSynth.LoopSeconds).Within(0.001));
            Assert.That(AudioAnalysis.SeamRatio(theme.Bed, theme.BedSampleRate), Is.LessThanOrEqualTo(1.0), "sin clic en la costura");
            Assert.That(AudioAnalysis.PeakDb(theme.Bed), Is.EqualTo((float)MusicSynth.MixPeakDb).Within(0.01f), "al nivel de la música");
            Assert.That(AudioAnalysis.EnergyFractionAbove(theme.Bed, AudioAnalysis.SpeakerCutHz, theme.BedSampleRate), Is.GreaterThanOrEqualTo(0.5), "lo da el altavoz");
        }

        [Test]
        public void PercussionLayers_FollowTheSealedShare_AndTheRemoteConfigKey()
        {
            Assert.That(AudioConfig.MusicPercussionLayers, Is.EqualTo(2), "audio_music_percussion_layers = 2 (08 §9)");
            Assert.That(AudioConfig.AmbientRainScale, Is.EqualTo(1.0), "audio_ambient_rain_scale = 1,0 (08 §9)");

            // Cuatro frutas en cuias de 4: ninguna, una y dos selladas.
            BoardState none = Board(0);
            BoardState one = Board(1);
            BoardState two = Board(2);
            BoardState all = Board(4);
            Assert.That(AudioBoard.PercussionLayers(none, 2), Is.EqualTo(0), "sin sellar, solo el lecho");
            Assert.That(AudioBoard.PercussionLayers(one, 2), Is.EqualTo(2), "la primera al sellar la primera cuia y otra al 25 %");
            Assert.That(AudioBoard.PercussionLayers(two, 3), Is.EqualTo(3), "50 %: la tercera si la clave lo deja");
            Assert.That(AudioBoard.PercussionLayers(all, 3), Is.EqualTo(3), "nunca más de la clave");
            Assert.That(AudioBoard.PercussionLayers(all, 0), Is.EqualTo(0), "con 0 capas, solo el lecho");

            AudioConfig.MusicPercussionLayers = 9;
            Assert.That(AudioConfig.MusicPercussionLayers, Is.EqualTo(3), "rango 0-3 (08 §9)");
            AudioConfig.MusicPercussionLayers = -1;
            Assert.That(AudioConfig.MusicPercussionLayers, Is.EqualTo(0));
            AudioConfig.AmbientRainScale = 5.0;
            Assert.That(AudioConfig.AmbientRainScale, Is.EqualTo(2.0), "rango 0-2 (08 §9)");
            AudioConfig.Reset();
            Assert.That(AudioConfig.MusicPercussionLayers, Is.EqualTo(2));

            var mix = new AudioMix();
            mix.SetMusic(2, 7, false);
            Assert.That(mix.MusicLayers, Is.EqualTo(3), "el servicio también recorta");
            mix.SetMusic(0, 2, true);
            Assert.That(mix.MusicLayers + (mix.MusicNight ? 1 : 0), Is.EqualTo(0), "sin mundo, sin capas ni noche");
        }

        [Test]
        public void Mix_MusicAndAmbient_DuckWithTheToadaAndModals_GoQuietWithMusicOffAndAds_AndComeBackIn500Ms()
        {
            var mix = new AudioMix();
            mix.SetMusic(1, 1, false);
            mix.SetAmbient(1, true, 3);
            Assert.That(mix.MusicGain, Is.EqualTo(1f));
            Assert.That(mix.AmbientGain, Is.EqualTo(1f));

            mix.ToadaStarted();
            Assert.That(mix.MusicGain, Is.EqualTo(AudioMix.DuckGain).Within(1e-6f), "07 §9.4: −6 dB con la toada");
            Assert.That(mix.AmbientGain, Is.EqualTo(AudioMix.DuckGain).Within(1e-6f), "y el ambiente con ella [P]");
            Assert.That(AudioAnalysis.ToDb(AudioMix.DuckGain), Is.EqualTo(-6f).Within(0.01f));
            mix.Tick(AudioMix.ToadaDuckMs);
            Assert.That(mix.Ducked, Is.False, "vuelve al terminar la toada");
            mix.ModalDuck = true;
            Assert.That(mix.AmbientGain, Is.EqualTo(AudioMix.DuckGain).Within(1e-6f), "y con un modal");
            mix.ModalDuck = false;

            mix.Settings = new SoundSettings(true, false, true);
            Assert.That(mix.MusicGain + mix.AmbientGain + mix.FaunaGain, Is.EqualTo(0f), "Música apagada: lecho, capas y ambiente callados");
            Assert.That(mix.SfxAudible, Is.True, "los efectos siguen");
            mix.Settings = SoundSettings.Defaults;

            mix.Ringer = RingerMode.Vibrate;
            Assert.That(mix.AmbientGain, Is.EqualTo(0f), "modo silencio del teléfono");
            mix.Ringer = RingerMode.Normal;

            mix.SetAdActive(true);
            Assert.That(mix.MusicGain + mix.AmbientGain, Is.EqualTo(0f), "silencio en anuncios");
            float left = mix.FaunaLeftMs;
            Assert.That(mix.AdvanceFauna(left + 1f, out _), Is.False, "la fauna espera durante el anuncio");
            Assert.That(mix.FaunaLeftMs, Is.EqualTo(left));
            mix.SetAdActive(false);
            Assert.That(mix.AmbientGain, Is.EqualTo(0f), "vuelve desde 0");
            mix.Tick(AudioMix.AdFadeMs / 2f);
            Assert.That(mix.AmbientGain, Is.EqualTo(0.5f).Within(1e-4f));
            mix.Tick(AudioMix.AdFadeMs / 2f);
            Assert.That(mix.MusicGain, Is.EqualTo(1f), "0,5 s después, a su nivel");
            Assert.That(mix.FaunaGain, Is.EqualTo((float)AudioAnalysis.FromDb(AmbientPlan.FaunaGainDb)).Within(1e-6f), "la fauna, por debajo de su efecto");
        }

        [Test]
        public void MusicReport_ChangesOnlyWhenTheStateChanges()
        {
            var mix = new AudioMix();
            Assert.That(mix.TakeMusicReport(out MusicReport first), Is.True, "la primera vez, siempre");
            Assert.That(first.World + first.Layers, Is.EqualTo(0));
            Assert.That(mix.TakeMusicReport(out _), Is.False, "sin cambios, nada");

            mix.SetMusic(3, 0, true);
            Assert.That(mix.TakeMusicReport(out MusicReport level), Is.True);
            Assert.That(level.World == 3 && level.Night && !level.Menu && !level.Ducked, Is.True);
            mix.SetMusic(3, 0, true);
            Assert.That(mix.TakeMusicReport(out _), Is.False, "lo mismo otra vez no cuenta");

            mix.SetMusic(3, 1, true);
            Assert.That(mix.TakeMusicReport(out MusicReport layer), Is.True);
            Assert.That(layer.Layers, Is.EqualTo(1));
            mix.ToadaStarted();
            Assert.That(mix.TakeMusicReport(out MusicReport ducked), Is.True);
            Assert.That(ducked.Ducked, Is.True);
            mix.Tick(AudioMix.ToadaDuckMs);
            Assert.That(mix.TakeMusicReport(out MusicReport back), Is.True, "también cuando la atenuación termina sola");
            Assert.That(back.Ducked, Is.False);

            mix.SetMenuMusic();
            Assert.That(mix.TakeMusicReport(out MusicReport menu), Is.True);
            Assert.That(menu.Menu && menu.World == 0 && !menu.Night, Is.True);
        }

        [Test]
        public void FakeService_ReportsMusicAndAmbient_AndPlaysFaunaWhileTheAmbientIsHeard()
        {
            var fake = new FakeAudioService();
            var probe = new RecordingProbe();
            fake.Probe = probe;
            AudioCues.Install(fake);
            AudioCues.SetMusic(4, 1, false);
            AudioCues.LevelAmbient(4, 95);
            Assert.That(fake.AmbientWorld, Is.EqualTo(4));
            Assert.That(fake.AmbientRain, Is.EqualTo(AmbientPlan.HasRain(95, 4, AudioConfig.AmbientRainScale)));
            Assert.That(probe.Ambients, Is.EqualTo(1));
            Assert.That(probe.LastMusic.World, Is.EqualTo(4));

            fake.Tick(AmbientPlan.FaunaMaxMs + 1f);
            Assert.That(fake.FaunaCount(true), Is.EqualTo(1), "un animal en los primeros 45 s");
            fake.Configure(new SoundSettings(true, false, true));
            fake.Tick(AmbientPlan.FaunaMaxMs + 1f);
            Assert.That(fake.FaunaCount(false), Is.EqualTo(2));
            Assert.That(fake.FaunaCount(true), Is.EqualTo(1), "con Música apagada no se oye");

            AudioCues.Home();
            Assert.That(fake.MusicMenu && fake.AmbientWorld == 0, Is.True, "el inicio: tema de menú, sin río");
            Assert.That(probe.LastMusic.Menu, Is.True);
            Assert.That(probe.Ambients, Is.EqualTo(2));
            fake.Tick(3600000f);
            Assert.That(fake.FaunaCount(false), Is.EqualTo(2), "sin ambiente, sin animales");
        }

        [Test]
        public void Probe_WritesTheMusicLineOnlyOutsideTheEditorUnlessAsked_AndTheAmbientLine()
        {
            var probe = new AF.UI.DevProbe();
            probe.Music(new MusicReport(3, 1, true, false, false));
            Assert.That(probe.LastMusicLine, Is.Null, "como sfx: en el Editor no escribe al sellar (sin asignaciones en el bucle)");
            try
            {
                AF.UI.DevProbe.AudioLinesInEditor = true;
                probe.Music(new MusicReport(3, 1, true, true, false));
            }
            finally
            {
                AF.UI.DevProbe.AudioLinesInEditor = false;
            }

            StringAssert.StartsWith("AF_STATE {\"event\":\"music\",\"t\":", probe.LastMusicLine);
            StringAssert.EndsWith(",\"world\":3,\"layers\":1,\"night\":true,\"ducked\":true,\"menu\":false}", probe.LastMusicLine);

            probe.Ambient(4, true, 95);
            StringAssert.StartsWith("AF_STATE {\"event\":\"ambient\",\"t\":", probe.LastAmbientLine);
            StringAssert.EndsWith(",\"world\":4,\"rain\":true,\"level\":95}", probe.LastAmbientLine, "al cargar el nivel, también en el Editor");
        }

        [Test]
        public void Mix_TickFaunaAndReport_DoNotAllocate()
        {
            var mix = new AudioMix();
            mix.SetMusic(3, 1, false);
            mix.SetAmbient(3, true, 58);
            for (int i = 0; i < 100; i++)
            {
                mix.Tick(16f);
                mix.AdvanceFauna(16f, out _);
                mix.TakeMusicReport(out _);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 20000; i++)
            {
                mix.Tick(16f);
                mix.AdvanceFauna(16f, out _);
                if (i == 5000)
                {
                    mix.ToadaStarted();
                }

                mix.TakeMusicReport(out _);
                _ = mix.MusicGain + mix.AmbientGain + mix.FaunaGain;
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0L), "sin coste por fotograma que asigne memoria (07 §9.4; 08 §5.4)");
        }

        // Tablero de cuatro frutas en cuias de 4 con las primeras sealedCount selladas (llenas de una fruta) y el resto mezclado
        // (sealedCount ≠ 3: con una sola fruta suelta, su cuia también saldría sellada).
        private static BoardState Board(int sealedCount)
        {
            var cuias = new Cuia[6];
            var rest = new List<int>();
            for (int f = 1; f <= 4; f++)
            {
                if (f <= sealedCount)
                {
                    cuias[f - 1] = Cuia.Create(4, new[] { f, f, f, f });
                }
                else
                {
                    rest.Add(f);
                }
            }

            int index = sealedCount;
            for (int r = 0; r < rest.Count; r++)
            {
                int a = rest[r];
                int b = rest[(r + 1) % rest.Count];
                cuias[index++] = Cuia.Create(4, new[] { a, b, a, b });
            }

            while (index < cuias.Length)
            {
                cuias[index++] = Cuia.Empty(4);
            }

            return new BoardState(cuias);
        }

        private static bool AmbientConfigDefaultsOf08()
        {
            return AudioConfig.AmbientRainScaleDefault == 1.0 && AudioConfig.MusicPercussionLayersDefault == 2;
        }

        private sealed class RecordingProbe : IAudioProbe
        {
            public int Ambients { get; private set; }

            public MusicReport LastMusic { get; private set; }

            public void Sfx(string id, float gainDb, int tint)
            {
            }

            public void Haptic(string kind)
            {
            }

            public void Build(double workerMs, double mainMs, int clips, long pcmKiB)
            {
            }

            public void Music(in MusicReport report)
            {
                LastMusic = report;
            }

            public void Ambient(int world, bool rain, int level)
            {
                Ambients++;
            }
        }
    }
}
