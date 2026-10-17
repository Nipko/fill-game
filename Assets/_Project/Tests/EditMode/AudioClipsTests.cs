#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using AF.Audio;
using AF.Audio.Unity;
using AF.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Audio definitivo de la entrega 1 (tarea AU3): los 173 ids de la especificación con su ranura y su archivo, el registro de
    /// clips completo y fuera de <c>Resources</c>, los ajustes de importación por carpeta, las capas de cada lecho del mismo largo
    /// que él, el fundido de los 4 bucles del chorro sin pasar de ± 2 semitonos, la intensidad de la lluvia por nivel, la fauna
    /// grabada de cada mundo y los remates (bajo los efectos y nunca encima de la toada).
    /// </summary>
    public sealed class AudioClipsTests
    {
        private static readonly string SpecPath = Path.Combine(Application.dataPath, "..", "docs", "audio", "especificacion-sonidos.json");

        [Test]
        public void Ids_AreThe173OfTheSpecification_WithTheirFiles()
        {
            string json = File.ReadAllText(SpecPath);
            MatchCollection pairs = Regex.Matches(json, "\"id\":\\s*\"([^\"]+)\",\\s*\"file\":\\s*\"([^\"]+)\"");
            Assert.That(pairs.Count, Is.EqualTo(AudioClipIds.Count), "173 piezas en la especificación");
            var seen = new HashSet<int>();
            foreach (Match m in pairs)
            {
                string id = m.Groups[1].Value;
                int slot = AudioClipIds.SlotOf(id);
                Assert.That(slot, Is.GreaterThanOrEqualTo(0), id + " tiene ranura");
                Assert.That(AudioClipIds.File(slot), Is.EqualTo(m.Groups[2].Value), id + ": su archivo");
                Assert.That(seen.Add(slot), Is.True, id + ": una ranura por id");
            }

            Assert.That(AudioClipIds.SlotOf("sfx_inventado"), Is.EqualTo(-1));
        }

        [Test]
        public void EffectSlots_AreTheAudioBankIndices_AndTheFruitNotesTheirFruit()
        {
            for (int i = 0; i < SfxCatalog.Count; i++)
            {
                Assert.That(AudioClipIds.Id(AudioBank.Index((SfxId)i, 0)), Is.EqualTo(SfxCatalog.Name((SfxId)i)));
            }

            // Cada nota de fruta grabada (fruit_notes) suena a la frecuencia de su fruta en el catálogo: la ranura del índice de
            // motor es la de su fruta.
            string json = File.ReadAllText(SpecPath);
            MatchCollection notes = Regex.Matches(json, "\"id\":\\s*\"(sfx_(?:select|tint)_[a-z_]+)\"[^}]*?\"f0_hz\":\\s*([0-9.]+)");
            Assert.That(notes.Count, Is.EqualTo(2 * SfxCatalog.FruitCount), "28 notas de fruta");
            foreach (Match m in notes)
            {
                int slot = AudioClipIds.SlotOf(m.Groups[1].Value);
                int tint = AudioBank.TintOf(slot);
                Assert.That(tint, Is.InRange(1, SfxCatalog.FruitCount), m.Groups[1].Value);
                Assert.That(AudioBank.IdOf(slot), Is.EqualTo(m.Groups[1].Value.StartsWith("sfx_select_", StringComparison.Ordinal) ? SfxId.Select : SfxId.Tint));
                double hz = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                Assert.That(SfxCatalog.FruitFrequency(tint), Is.EqualTo(hz).Within(0.01), m.Groups[1].Value + ": la nota de su fruta");
            }
        }

        [Test]
        public void Registry_Has173Clips_OutsideResources_AndEveryWavIsImportedAsItsFolderSays()
        {
            List<string> problems = AudioAssetSetup.Problems();
            Assert.That(problems, Is.Empty, "AudioAssetSetup.Apply: " + string.Join("; ", problems));
            var registry = AssetDatabase.LoadAssetAtPath<AudioClipRegistry>(AudioClipRegistry.AssetPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry!.Count, Is.EqualTo(AudioClipIds.Count), "ningún id sin clip");
            Assert.That(AudioClipRegistry.AssetPath.Contains("/Resources/"), Is.False, "sin Resources");
            for (int slot = 0; slot < AudioClipIds.Count; slot++)
            {
                string path = AudioClipIds.AssetPath(slot);
                Assert.That(AssetDatabase.GetAssetPath(registry.Get(slot)), Is.EqualTo(path), AudioClipIds.Id(slot));
                Assert.That(path.Contains("/Resources/"), Is.False);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                AudioImporterSampleSettings s = importer.defaultSampleSettings;
                Assert.That(importer.loadInBackground, Is.True, path + ": Load In Background");
                Assert.That(AudioAssetSetup.Normalizes(importer), Is.False, path + ": sin Normalize (conserva el pico de su ficha)");
                Assert.That(s.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), path);
                string rel = path.Substring(AudioClipIds.Folder.Length);
                if (rel.StartsWith("sfx/", StringComparison.Ordinal) || rel.StartsWith("amb/fauna/", StringComparison.Ordinal))
                {
                    Assert.That(s.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad), path + ": efectos, notas y fauna");
                    Assert.That(importer.forceToMono, Is.True, path + ": mono");
                    Assert.That(s.quality, Is.GreaterThanOrEqualTo(0.7f), path + ": Vorbis de calidad alta");
                    Assert.That(s.preloadAudioData, Is.True, path);
                }
                else if (rel.StartsWith("amb/", StringComparison.Ordinal) || rel == "music/menu.wav")
                {
                    Assert.That(s.loadType, Is.EqualTo(AudioClipLoadType.Streaming), path + ": agua, lluvia y menú en streaming");
                    Assert.That(s.quality, Is.InRange(0.4f, 0.5f), path + ": 08 §6, Vorbis q0,4");
                    Assert.That(importer.forceToMono, Is.False, path + ": estéreo donde el WAV lo sea");
                }
                else if (rel.StartsWith("music/stingers/", StringComparison.Ordinal))
                {
                    Assert.That(s.loadType, Is.EqualTo(AudioClipLoadType.CompressedInMemory), path + ": remates");
                    Assert.That(s.preloadAudioData, Is.True, path + ": suenan sin espera");
                }
                else
                {
                    Assert.That(rel.StartsWith("music/", StringComparison.Ordinal), Is.True, path);
                    Assert.That(s.loadType, Is.EqualTo(AudioClipLoadType.CompressedInMemory), path + ": capas a la muestra, no en streaming");
                    Assert.That(s.quality, Is.InRange(0.4f, 0.5f), path);
                    Assert.That(s.preloadAudioData, Is.False, path + ": se cargan al pedir el mundo");
                }
            }
        }

        [Test]
        public void EachWorld_BedNightAndLayers_MeasureTheSameSampleBySample()
        {
            var registry = AssetDatabase.LoadAssetAtPath<AudioClipRegistry>(AudioClipRegistry.AssetPath);
            Assert.That(registry, Is.Not.Null);
            for (int w = 1; w <= AudioClipIds.Worlds; w++)
            {
                AudioClip bed = registry!.Get(AudioClipIds.BedSlot(w, false))!;
                AudioClip night = registry.Get(AudioClipIds.BedSlot(w, true))!;
                Assert.That(night.samples, Is.EqualTo(bed.samples), "mundo " + w + ": la nocturna lleva las mismas capas");
                Assert.That(night.frequency, Is.EqualTo(bed.frequency));
                for (int l = 1; l <= AudioClipIds.PercussionLayers; l++)
                {
                    AudioClip layer = registry.Get(AudioClipIds.PercussionSlot(w, l))!;
                    Assert.That(layer.samples, Is.EqualTo(bed.samples), "mundo " + w + ", capa " + l + ": mismo largo que el lecho");
                    Assert.That(layer.frequency, Is.EqualTo(bed.frequency));
                }
            }
        }

        [Test]
        public void PourBlend_FourLoops_FadeWithoutJumps_AndNeverMoreThanTwoSemitones()
        {
            Assert.That(PourBlend.Weight(PourBlend.Position(0f), 0), Is.EqualTo(1f), "1 dosis: l1");
            Assert.That(PourBlend.Weight(PourBlend.Position(1f), 3), Is.EqualTo(1f), "llena: l4");
            Assert.That(PourBlend.Semitones(PourBlend.Position(1f / 3f), 1), Is.EqualTo(0f).Within(1e-4f), "un tercio: l2 en su tono");
            float min = PourBlend.Pitch(-PourBlend.MaxSemitones);
            float max = PourBlend.Pitch(PourBlend.MaxSemitones);
            Assert.That(max, Is.LessThan(1.13f), "nunca la aceleración de 2,4 veces del bucle por código");
            var previous = new float[PourBlend.Loops];
            var previousPitch = new float[PourBlend.Loops];
            for (int step = 0; step <= 1000; step++)
            {
                float pos = PourBlend.Position(step / 1000f);
                float power = 0f;
                int audible = 0;
                for (int k = 0; k < PourBlend.Loops; k++)
                {
                    float w = PourBlend.Weight(pos, k);
                    float pitch = PourBlend.Pitch(PourBlend.Semitones(pos, k));
                    power += w * w;
                    if (w > 0f)
                    {
                        audible++;
                        Assert.That(Math.Abs(PourBlend.Semitones(pos, k)), Is.LessThanOrEqualTo(PourBlend.MaxSemitones), "± 2 semitonos");
                        Assert.That(pitch, Is.InRange(min, max));
                    }

                    if (step > 0)
                    {
                        Assert.That(Math.Abs(w - previous[k]), Is.LessThan(0.01f), "sin saltos de volumen");
                        Assert.That(Math.Abs(pitch - previousPitch[k]), Is.LessThan(0.001f), "sin saltos de tono");
                    }

                    previous[k] = w;
                    previousPitch[k] = pitch;
                }

                Assert.That(power, Is.EqualTo(1f).Within(1e-4f), "potencia constante");
                Assert.That(audible, Is.InRange(1, 2), "como mucho dos bucles a la vez");
            }
        }

        [Test]
        public void RainIntensity_IsFixedPerLevel_AndUsesTheThreeRecordings()
        {
            var counts = new int[3];
            for (int level = 1; level <= 2000; level++)
            {
                RainIntensity a = AmbientPlan.RainIntensityOf(level);
                Assert.That(AmbientPlan.RainIntensityOf(level), Is.EqualTo(a), "determinista");
                counts[(int)a]++;
            }

            Assert.That(counts[0] / 2000.0, Is.EqualTo(0.5).Within(0.05), "ligera ~50 %");
            Assert.That(counts[1] / 2000.0, Is.EqualTo(0.35).Within(0.05), "media ~35 %");
            Assert.That(counts[2] / 2000.0, Is.EqualTo(0.15).Within(0.05), "fuerte ~15 %");
            Assert.That(AudioClipIds.Id(AudioClipIds.RainSlot(RainIntensity.Medium)), Is.EqualTo("amb_rain_medium"));
        }

        [Test]
        public void Fauna_PicksAmongTheRecordedAnimalsOfTheWorld_Every20To45Seconds()
        {
            int[] expected = { 3, 2, 2, 2, 1, 3, 2, 1, 1 };
            for (int w = 1; w <= AudioClipIds.Worlds; w++)
            {
                Assert.That(AudioClipIds.FaunaCount(w), Is.EqualTo(expected[w - 1]), "mundo " + w);
            }

            Assert.That(AudioClipIds.FaunaCount(11), Is.EqualTo(0), "los mundos 10-13, de respaldo");
            var mix = new AudioMix();
            mix.SetAmbient(6, false, 205);
            var picks = new int[3];
            float sinceLast = 0f;
            int fired = 0;
            for (int i = 0; i < 3600 * 10; i++)
            {
                sinceLast += 100f;
                if (mix.AdvanceFauna(100f, AudioClipIds.FaunaCount(6), out int pick))
                {
                    Assert.That(sinceLast, Is.InRange(AmbientPlan.FaunaMinMs, AmbientPlan.FaunaMaxMs + 100f), "cada 20-45 s");
                    picks[pick]++;
                    sinceLast = 0f;
                    fired++;
                }
            }

            Assert.That(fired, Is.GreaterThan(50));
            Assert.That(picks[0] > 0 && picks[1] > 0 && picks[2] > 0, Is.True, "el mundo 6 suena con sus 3 animales (antes, ninguno)");
            Assert.That(AudioClipIds.Id(AudioClipIds.FaunaSlot(6, 2)), Is.EqualTo("amb_w06_fauna_03"));
        }

        [Test]
        public void Stingers_WaitForTheToada_AndSitUnderTheEffects()
        {
            var mix = new AudioMix();
            var queue = new StingerQueue();
            Assert.That(queue.Request(StingerId.MechCard, mix.ToadaLeftMs), Is.True, "sin toada, suena ya");
            mix.ToadaStarted();
            Assert.That(queue.Request(StingerId.Arrival, mix.ToadaLeftMs), Is.False, "con la toada, espera");
            Assert.That(queue.Pending && queue.PendingId == StingerId.Arrival, Is.True);
            mix.Tick(AudioMix.ToadaDuckMs - 100f);
            Assert.That(queue.Advance(mix.ToadaLeftMs, out _), Is.False, "no se solapa con la toada");
            mix.Tick(200f);
            Assert.That(queue.Advance(mix.ToadaLeftMs, out StingerId id), Is.True, "al terminar la toada");
            Assert.That(id, Is.EqualTo(StingerId.Arrival));
            Assert.That(queue.Pending, Is.False);

            Assert.That(AudioMix.StingerGainDb + UnityAudioService.StingerPeakDb, Is.LessThan(-21f), "por debajo de la llegada, la tarjeta y la canoa");
            Assert.That(mix.StingerGain, Is.EqualTo((float)AudioAnalysis.FromDb(AudioMix.StingerGainDb)).Within(1e-5f));
            mix.Settings = new SoundSettings(true, false, true);
            Assert.That(mix.StingerGain, Is.EqualTo(0f), "con el ajuste Música [P]");
            mix.Settings = SoundSettings.Defaults;
            mix.SetAdActive(true);
            Assert.That(mix.StingerGain, Is.EqualTo(0f), "callado durante un anuncio");

            var fake = new FakeAudioService();
            AudioCues.Install(fake);
            try
            {
                int serial = AudioCues.Serial;
                AudioCues.Stinger(StingerId.LevelFailed);
                Assert.That(fake.StingerCount(StingerId.LevelFailed, true), Is.EqualTo(1));
                Assert.That(AudioCues.Serial, Is.EqualTo(serial), "no cuenta como el sonido de un botón");
            }
            finally
            {
                AudioCues.Install(null);
            }

            Assert.That(AudioClipIds.Id(AudioClipIds.StingerSlot(StingerId.FruitNew)), Is.EqualTo("sting_fruit_new"), "en el registro, sin gancho");
        }

        [Test]
        public void RegistryInMemory_MissingIdsAreNull_AndUnknownIdsAreIgnored()
        {
            AudioClip clip = AudioClip.Create("prueba", 480, 1, 48000, false);
            AudioClipRegistry registry = AudioClipRegistry.Create(new[] { "sfx_seal", "no_existe" }, new[] { clip, clip });
            try
            {
                Assert.That(registry.Count, Is.EqualTo(1));
                Assert.That(registry.Get(AudioClipIds.SlotOf("sfx_seal")), Is.SameAs(clip));
                Assert.That(registry.Has((int)SfxId.Select), Is.False, "sin clip: el respaldo por código");
                Assert.That(registry.Get(-1), Is.Null);
                Assert.That(registry.Get(AudioClipIds.Count), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(registry);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}
