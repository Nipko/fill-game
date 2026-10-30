#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Audio;
using AF.Audio.Unity;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El juego suena con el audio definitivo de la entrega 1 (tarea AU3), con el servicio real: la escena cuelga el registro de
    /// los 173 clips y el servicio lo usa sin generar el audio por código; cada nota de fruta es su clip, sin cambio de tono; un id
    /// sin clip suena con el respaldo por código; el chorro funde los 4 bucles grabados sin pasar de ± 2 semitonos; el lecho y sus
    /// capas se programan en el mismo instante del reloj de audio y miden lo mismo; el agua, la lluvia y la fauna son las del
    /// mundo (el 11, de respaldo); y un remate espera a que termine la toada. La sonda dice de dónde sale cada cosa.
    /// </summary>
    public sealed class AudioClipsPlayTests
    {
        private Recorder _probe = null!;
        private UnityAudioService? _service;

        [SetUp]
        public void SetUp()
        {
            _probe = new Recorder();
            AudioCues.Override = null;
            AudioConfig.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            AudioCues.Override = null;
            AudioCues.Install(null);
            AudioConfig.Reset();
            if (_service != null)
            {
                UnityEngine.Object.Destroy(_service.gameObject); // el siguiente arranque crea otro con el registro de su escena
                _service = null;
            }
        }

        [UnityTest]
        public IEnumerator Scene_HangsTheRegistry_TheServiceUsesIt_AndEachFruitNoteIsItsClip()
        {
            // Un servicio nuevo: el que quedara de otro test pudo generar ya el respaldo por código.
            UnityEngine.Object.Destroy(UnityAudioService.Ensure().gameObject);
            yield return null;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            AudioClipRegistry? registry = boot.AudioClipsAsset;
            Assert.That(registry, Is.Not.Null, "GameSceneBuilder cuelga el registro de la escena");
            Assert.That(registry!.Count, Is.EqualTo(AudioClipIds.Count));
            UnityAudioService s = Take();
            Assert.That(s.Clips, Is.SameAs(registry), "el arranque da el registro al servicio");
            Assert.That(s.EffectsRecorded && s.Ready, Is.True);
            yield return null;
            yield return null;
            Assert.That(s.SynthRequested, Is.False, "con todo grabado no se genera el audio por código");

            for (int tint = 1; tint <= SfxCatalog.FruitCount; tint++)
            {
                s.Play(SfxId.Select, tint);
                AssertLastSfx("sfx_select", tint, true);
                AssertSounding(s, registry.Get(AudioBank.Index(SfxId.Select, tint))!);
                s.Play(SfxId.Tint, tint);
                AssertLastSfx("sfx_tint", tint, true);
                AssertSounding(s, registry.Get(AudioBank.Index(SfxId.Tint, tint))!);
            }

            Assert.That(registry.Get(AudioBank.Index(SfxId.Select, 9))!.name, Is.EqualTo("sfx_select_jenipapo"), "el 9 es el Huito (C4)");
            s.Play(SfxId.Select, 0);
            AssertSounding(s, registry.Get((int)SfxId.Select)!);
            Assert.That(_probe.LastSfxGain, Is.EqualTo(-24f), "el pico de su ficha (observación 23 de la jornada 6: antes, −36)");
        }

        [UnityTest]
        public IEnumerator AnIdWithoutClip_SoundsWithTheSynthFallback()
        {
            AudioClipRegistry full = LoadRegistry();
            var ids = new List<string>();
            var clips = new List<AudioClip>();
            for (int slot = 0; slot < AudioClipIds.Count; slot++)
            {
                if (slot != (int)SfxId.Seal)
                {
                    ids.Add(AudioClipIds.Id(slot));
                    clips.Add(full.Get(slot)!);
                }
            }

            AudioClipRegistry partial = AudioClipRegistry.Create(ids.ToArray(), clips.ToArray());
            UnityAudioService s = Take();
            s.UseClips(partial);
            Assert.That(s.EffectsRecorded, Is.False);
            yield return WaitLong(() => s.SynthReady, "el respaldo por código", 30f);
            s.Play(SfxId.Seal, 0);
            AssertLastSfx("sfx_seal", 0, false);
            s.Play(SfxId.Undo, 0);
            AssertLastSfx("sfx_undo", 0, true);
            AssertSounding(s, full.Get((int)SfxId.Undo)!);
            s.UseClips(full);
            Assert.That(s.EffectsRecorded, Is.True);
            s.Play(SfxId.Seal, 0);
            AssertLastSfx("sfx_seal", 0, true);
            UnityEngine.Object.Destroy(partial);
        }

        [UnityTest]
        public IEnumerator Pour_BlendsTheFourRecordedLoops_ByTheTargetLevel_WithinTwoSemitones()
        {
            UnityAudioService s = Take();
            s.UseClips(LoadRegistry());
            yield return null;
            float min = PourBlend.Pitch(-PourBlend.MaxSemitones) - 1e-4f;
            float max = PourBlend.Pitch(PourBlend.MaxSemitones) + 1e-4f;
            s.PourBegin(7, 0.5f);
            int slot = -1;
            for (int i = 0; i < UnityAudioService.PourVoices; i++)
            {
                if (s.PourUsesClips(i))
                {
                    slot = i;
                }
            }

            Assert.That(slot, Is.GreaterThanOrEqualTo(0), "el chorro suena con los bucles grabados");
            for (int f = 0; f < 6; f++)
            {
                s.Tick(16f);
            }

            Assert.That(s.PourPosition(slot), Is.EqualTo(1.5f).Within(1e-4f), "a medio llenar, entre l2 y l3");
            Assert.That(s.PourLayerVolume(slot, 0) + s.PourLayerVolume(slot, 3), Is.EqualTo(0f), "l1 y l4 callan");
            Assert.That(s.PourLayerVolume(slot, 1), Is.GreaterThan(0.3f));
            Assert.That(s.PourLayerVolume(slot, 2), Is.GreaterThan(0.3f));

            // El destino se llena: la posición sube sin saltos y ningún bucle que suena sale de ± 2 semitonos.
            float last = s.PourPosition(slot);
            for (int f = 0; f <= 60; f++)
            {
                s.PourLevel(7, 0.5f + (0.5f * f / 60f));
                s.Tick(16f);
                Assert.That(s.PourPosition(slot) - last, Is.InRange(0f, (16f * UnityAudioService.PourBlendPerMs) + 1e-4f), "sin saltos");
                last = s.PourPosition(slot);
                for (int k = 0; k < PourBlend.Loops; k++)
                {
                    if (s.PourLayerVolume(slot, k) > 0f)
                    {
                        Assert.That(s.PourLayerPitch(slot, k), Is.InRange(min, max), "bucle " + (k + 1) + ": como mucho ± 2 semitonos");
                    }
                }
            }

            for (int f = 0; f < 60; f++)
            {
                s.Tick(16f);
            }

            Assert.That(s.PourPosition(slot), Is.EqualTo(3f).Within(1e-4f), "llena: l4");
            Assert.That(s.PourLayerVolume(slot, 3), Is.GreaterThan(0.5f));
            Assert.That(s.PourLayerPitch(slot, 3), Is.EqualTo(1f).Within(1e-4f), "en su punto, sin afinar");
            s.PourEnd(7);
            for (int f = 0; f < 6; f++)
            {
                s.Tick(16f);
            }

            Assert.That(s.PourUsesClips(slot), Is.False, "el chorro se cortó");
            for (int k = 0; k < PourBlend.Loops; k++)
            {
                Assert.That(s.PourLayerVolume(slot, k), Is.EqualTo(0f));
            }
        }

        [UnityTest]
        public IEnumerator Music_BedAndLayers_AreScheduledAtTheSameDspTime_AndMeasureTheSame_DayAndNight()
        {
            AudioClipRegistry registry = LoadRegistry();
            UnityAudioService s = Take();
            s.UseClips(registry);
            s.Configure(SoundSettings.Defaults);
            s.SetAdActive(false);
            s.SetModalDuck(false);
            s.SetMusic(3, 2, false);
            yield return WaitLong(() => s.WorldBedKey == 6 && s.WorldBedFromClips && s.MusicVolume(0) > 0.3f, "el lecho grabado de Manaos", 30f);
            Assert.That(_probe.LastMusicClip, Is.EqualTo("mus_w03_bed"), "AF_STATE music con el clip del lecho");
            AssertBed(s, registry, 3, false);
            yield return WaitLong(() => s.MusicVolume(2) > 0.3f, "dos capas", 10f);
            Assert.That(s.MusicVolume(3), Is.EqualTo(0f), "con 2 capas, la tercera calla");

            s.SetMusic(3, 2, true);
            yield return WaitLong(() => s.WorldBedKey == 7 && s.WorldBedFromClips && s.MusicVolume(0) > 0.3f, "la variante nocturna", 30f);
            Assert.That(_probe.LastMusicClip, Is.EqualTo("mus_w03_bed_night"));
            AssertBed(s, registry, 3, true);

            s.SetMusic(11, 1, false);
            yield return WaitLong(() => s.WorldBedKey == 22 && !s.WorldBedFromClips, "el mundo 11, de respaldo por código", 30f);
            Assert.That(_probe.LastMusicClip, Is.EqualTo(AudioProbeIds.Synth));

            s.SetMenuMusic();
            yield return WaitLong(() => s.MenuReady && s.MenuVolume > 0.3f, "el tema de menú grabado", 30f);
            Assert.That(s.MenuClip, Is.SameAs(registry.Get(AudioClipIds.MenuSlot)));
            Assert.That(_probe.LastMusicClip, Is.EqualTo("mus_menu"));
        }

        [UnityTest]
        public IEnumerator Ambient_RecordedWaterRainAndFauna_OfTheWorld_AndWorld11FromTheFallback()
        {
            AudioClipRegistry registry = LoadRegistry();
            UnityAudioService s = Take();
            s.UseClips(registry);
            s.Configure(SoundSettings.Defaults);
            s.SetAdActive(false);
            int level = RainyLevel(4);
            RainIntensity intensity = AmbientPlan.RainIntensityOf(level);
            s.SetAmbient(4, true, level);
            Assert.That(_probe.LastWaterClip, Is.EqualTo("amb_w04_water"));
            Assert.That(_probe.LastRainClip, Is.EqualTo(AudioClipIds.Id(AudioClipIds.RainSlot(intensity))));
            yield return WaitLong(() => s.WaterFromClip && s.WaterVolume > 0.3f && s.RainVolume > 0.3f, "agua y lluvia grabadas del Trapecio", 10f);
            Assert.That(s.WaterClip, Is.SameAs(registry.Get(AudioClipIds.WaterSlot(4))));
            Assert.That(s.RainClip, Is.SameAs(registry.Get(AudioClipIds.RainSlot(intensity))), "la intensidad del nivel");

            // Un animal grabado del mundo en 20-45 s.
            for (int i = 0; i < 50; i++)
            {
                s.Tick(1000f);
            }

            AudioClip? fauna = s.FaunaClip;
            Assert.That(fauna, Is.Not.Null, "en 50 s suena algún animal");
            bool ofWorld = false;
            for (int k = 0; k < AudioClipIds.FaunaCount(4); k++)
            {
                ofWorld |= fauna == registry.Get(AudioClipIds.FaunaSlot(4, k));
            }

            Assert.That(ofWorld, Is.True, "de los grabados del Trapecio");

            int renders = s.WaterRenders;
            s.SetAmbient(11, false, 650);
            Assert.That(_probe.LastWaterClip, Is.EqualTo(AudioProbeIds.Synth), "el mundo 11 no tiene WAV");
            yield return WaitLong(() => s.WaterWorld == 11 && !s.WaterFromClip, "el agua del 11 por código", 30f);
            Assert.That(s.WaterRenders, Is.EqualTo(renders + 1));
        }

        [UnityTest]
        public IEnumerator Stinger_WaitsForTheToada_UnderTheEffects_AndFollowsTheMusicSetting()
        {
            UnityAudioService s = Take();
            AudioClipRegistry registry = LoadRegistry();
            s.UseClips(registry);
            s.Configure(SoundSettings.Defaults);
            s.SetAdActive(false);
            yield return null;
            float toadaMs = registry.Get(AudioBank.Index(SfxId.WinToada, 0))!.length * 1000f;
            Assert.That(toadaMs, Is.GreaterThan(AudioMix.ToadaDuckMs + 200f), "el WAV de la toada dura más que el largo fijo (2,55 s)");
            int started = s.StingersStarted;
            s.PlayStinger(StingerId.MechCard);
            Assert.That(s.StingersStarted, Is.EqualTo(started + 1), "sin toada, suena ya");
            Assert.That(_probe.LastSfxId, Is.EqualTo("sting_mech_card"));
            Assert.That(_probe.LastSfxGain, Is.EqualTo(UnityAudioService.StingerPeakDb + AudioMix.StingerGainDb), "bajo los efectos");

            s.Play(SfxId.WinToada, 0);
            Assert.That(s.Mix.ToadaLeftMs, Is.EqualTo(toadaMs), "tarea FX7: lo que dura el WAV, no el largo fijo");
            s.PlayStinger(StingerId.Arrival);
            Assert.That(s.StingerPending, Is.True, "la toada suena: espera");
            Assert.That(s.StingersStarted, Is.EqualTo(started + 1));
            s.Tick(AudioMix.ToadaDuckMs + 50f);
            Assert.That(s.StingerPending, Is.True, "pasado el largo fijo, la toada aún suena: sigue esperando");
            s.Tick(toadaMs - AudioMix.ToadaDuckMs - 150f);
            Assert.That(s.StingerPending, Is.True, "nunca encima de la toada");
            s.Tick(300f);
            Assert.That(s.StingersStarted, Is.EqualTo(started + 2), "al terminar la toada");
            Assert.That(s.LastStinger, Is.EqualTo(StingerId.Arrival));

            s.Configure(new SoundSettings(true, false, true));
            s.PlayStinger(StingerId.WorldBanner);
            Assert.That(s.StingersStarted, Is.EqualTo(started + 2), "con Música apagada no suena [P]");
            Assert.That(s.StingerPlaying, Is.False, "y el que sonaba se calla");
            s.Configure(SoundSettings.Defaults);
        }

        [UnityTest]
        public IEnumerator Toada_LastsItsClip_OfAnyLength_OrTheSynthFallbackClip()
        {
            // Tarea FX7: la espera de los remates y la atenuación del lecho duran lo que el clip de la toada que suena. Un WAV de
            // prueba de 3,2 s (distinto del largo fijo de 2,25 s y del WAV de 2,55 s) …
            AudioClipRegistry full = LoadRegistry();
            int toadaSlot = AudioBank.Index(SfxId.WinToada, 0);
            const int rate = 44100;
            AudioClip custom = AudioClip.Create("sfx_win_toada_test", (int)(3.2f * rate), 1, rate, false);
            var ids = new List<string>();
            var clips = new List<AudioClip>();
            for (int slot = 0; slot < AudioClipIds.Count; slot++)
            {
                ids.Add(AudioClipIds.Id(slot));
                clips.Add(slot == toadaSlot ? custom : full.Get(slot)!);
            }

            AudioClipRegistry withCustom = AudioClipRegistry.Create(ids.ToArray(), clips.ToArray());
            UnityAudioService s = Take();
            s.UseClips(withCustom);
            s.Configure(SoundSettings.Defaults);
            s.SetAdActive(false);
            yield return null;
            int started = s.StingersStarted;
            s.Play(SfxId.WinToada, 0);
            Assert.That(_probe.LastSfxClip, Is.True, "suena el WAV de prueba");
            Assert.That(s.Mix.ToadaLeftMs, Is.EqualTo(3200f).Within(0.5f), "lo que dura el clip");
            s.PlayStinger(StingerId.Arrival);
            s.Tick(3000f);
            Assert.That(s.StingerPending && s.Mix.Ducked, Is.True, "a los 3 s, la toada de 3,2 s aún suena");
            s.Tick(300f);
            Assert.That(s.StingersStarted, Is.EqualTo(started + 1), "al terminar el clip");
            Assert.That(s.Mix.Ducked, Is.False);

            // … y, sin WAV de la toada, la del respaldo por código (su clip sintetizado, de unos 2,27 s).
            ids.RemoveAt(toadaSlot);
            clips.RemoveAt(toadaSlot);
            AudioClipRegistry withoutToada = AudioClipRegistry.Create(ids.ToArray(), clips.ToArray());
            s.UseClips(withoutToada);
            yield return WaitLong(() => s.SynthReady, "el respaldo por código", 30f);
            s.Play(SfxId.WinToada, 0);
            Assert.That(_probe.LastSfxClip, Is.False, "suena el respaldo por código");
            AudioClip? synth = null;
            foreach (AudioSource source in s.GetComponentsInChildren<AudioSource>())
            {
                if (source.isPlaying && source.clip != null && source.clip.name.StartsWith("sfx_win_toada_", StringComparison.Ordinal))
                {
                    synth = source.clip;
                }
            }

            Assert.That(synth, Is.Not.Null, "suena el clip sintetizado de la toada");
            Assert.That(synth, Is.Not.SameAs(custom));
            float synthMs = synth!.length * 1000f;
            Assert.That(s.Mix.ToadaLeftMs, Is.EqualTo(synthMs).Within(0.5f), "lo que dura el clip sintetizado (" + synthMs + " ms)");
            s.Tick(synthMs + 1f);
            Assert.That(s.Mix.ToadaLeftMs, Is.EqualTo(0f));

            s.UseClips(full);
            UnityEngine.Object.Destroy(withCustom);
            UnityEngine.Object.Destroy(withoutToada);
            UnityEngine.Object.Destroy(custom);
        }

        private UnityAudioService Take()
        {
            _service = UnityAudioService.Ensure();
            _service.Configure(SoundSettings.Defaults);
            _service.SetAdActive(false);
            _service.SetModalDuck(false);
            _service.Probe = _probe;
            return _service;
        }

        private static AudioClipRegistry LoadRegistry()
        {
#if UNITY_EDITOR
            var registry = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClipRegistry>(AudioClipRegistry.AssetPath);
            Assert.That(registry, Is.Not.Null, "AudioAssetSetup.Apply genera el registro");
            return registry!;
#else
            Assert.Ignore("Solo en el Editor.");
            return null!;
#endif
        }

        // Un nivel del mundo world que tiene lluvia con los defaults de 08 §9.
        private static int RainyLevel(int world)
        {
            for (int level = 81; level < 10000; level++)
            {
                if (AmbientPlan.HasRain(level, world, AudioConfig.AmbientRainScaleDefault))
                {
                    return level;
                }
            }

            throw new InvalidOperationException("ningún nivel con lluvia");
        }

        private static void AssertBed(UnityAudioService s, AudioClipRegistry registry, int world, bool night)
        {
            Assert.That(s.MusicClip(0), Is.SameAs(registry.Get(AudioClipIds.BedSlot(world, night))), night ? "la nocturna" : "el lecho");
            double at = s.MusicScheduledDsp(0);
            Assert.That(at, Is.GreaterThan(0.0));
            int samples = s.MusicClip(0)!.samples;
            for (int v = 1; v < UnityAudioService.MusicVoices; v++)
            {
                Assert.That(s.MusicClip(v), Is.SameAs(registry.Get(AudioClipIds.PercussionSlot(world, v))), "capa " + v);
                Assert.That(s.MusicScheduledDsp(v), Is.EqualTo(at), "capa " + v + ": el mismo instante del reloj de audio");
                Assert.That(s.MusicClip(v)!.samples, Is.EqualTo(samples), "capa " + v + ": mide lo mismo que el lecho");
            }
        }

        private void AssertLastSfx(string id, int tint, bool clip)
        {
            Assert.That(_probe.LastSfxId, Is.EqualTo(id));
            Assert.That(_probe.LastSfxTint, Is.EqualTo(tint));
            Assert.That(_probe.LastSfxClip, Is.EqualTo(clip), id + (clip ? ": del clip grabado" : ": del respaldo por código"));
        }

        private static void AssertSounding(UnityAudioService s, AudioClip clip)
        {
            AudioSource[] sources = s.GetComponentsInChildren<AudioSource>();
            bool found = false;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].clip == clip)
                {
                    found = true;
                    Assert.That(sources[i].pitch, Is.EqualTo(1f), clip.name + ": sin cambio de tono por código");
                }
            }

            Assert.That(found, Is.True, clip.name + " en una fuente");
        }

        private static IEnumerator WaitLong(Func<bool> condition, string what, float seconds)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > seconds)
                {
                    Assert.Fail("Tiempo agotado esperando: " + what);
                }

                yield return null;
            }
        }

        private sealed class Recorder : IAudioProbe
        {
            public string LastSfxId { get; private set; } = string.Empty;

            public float LastSfxGain { get; private set; }

            public int LastSfxTint { get; private set; }

            public bool LastSfxClip { get; private set; }

            public string LastMusicClip { get; private set; } = string.Empty;

            public string LastWaterClip { get; private set; } = string.Empty;

            public string LastRainClip { get; private set; } = string.Empty;

            public void Sfx(string id, float gainDb, int tint, bool clip)
            {
                LastSfxId = id;
                LastSfxGain = gainDb;
                LastSfxTint = tint;
                LastSfxClip = clip;
            }

            public void Haptic(string kind)
            {
            }

            public void Build(double workerMs, double mainMs, int clips, long pcmKiB)
            {
            }

            public void Music(in MusicReport report, string clip)
            {
                LastMusicClip = clip;
            }

            public void Ambient(int world, bool rain, int level, string waterClip, string rainClip)
            {
                LastWaterClip = waterClip;
                LastRainClip = rainClip;
            }
        }
    }
}
