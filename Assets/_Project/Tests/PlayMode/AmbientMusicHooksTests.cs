#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Audio;
using AF.Audio.Unity;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Ambiente por mundo y música adaptativa en el juego (tarea AU2; 07 §9.1, §9.3 y §9.4), con el servicio falso
    /// (<see cref="FakeAudioService"/>): el inicio pide el tema de menú sin río; un nivel, el lecho de su mundo (nocturno en Muy
    /// difícil) y su ambiente con la lluvia que le toca; las capas siguen a las cuias selladas hasta
    /// <c>audio_music_percussion_layers</c>; la toada y el modal atenúan; el anuncio calla y vuelve en 0,5 s. Con el servicio
    /// real: el ajuste Música apaga lecho, capas y ambiente en el acto, y su fotograma no asigna memoria; el inicio para el lecho y
    /// el agua sin soltarlos (volver al mismo mundo no genera nada), otro mundo los suelta y con Música apagada no se genera nada.
    /// </summary>
    public sealed class AmbientMusicHooksTests
    {
        private FakeAudioService _audio = null!;
        private Recorder _probe = null!;
        private UnityAudioService? _real;

        [SetUp]
        public void SetUp()
        {
            _audio = new FakeAudioService();
            _probe = new Recorder();
            _audio.Probe = _probe;
            AudioCues.Override = _audio;
            AudioConfig.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            AudioCues.Override = null;
            AudioCues.Install(null);
            AudioConfig.Reset();
            if (_real != null)
            {
                Object.Destroy(_real.gameObject);
                _real = null;
            }
        }

        [UnityTest]
        public IEnumerator Home_PlaysTheMenuTheme_ThenLevel58_ItsWorldBedAndAmbient()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(_audio.MusicMenu, Is.True, "07 §9.3: el inicio, con el tema ligero");
            Assert.That(_audio.AmbientWorld, Is.EqualTo(0), "sin río en el inicio");
            Assert.That(_probe.Music.Count > 0 && _probe.Music[_probe.Music.Count - 1].Menu, Is.True, "AF_STATE music con menu");

            Assert.That(boot.PressPlay(), Is.True);
            yield return PlayModeHelpers.WaitFor(() => boot.Flow != null && boot.Flow.LevelNumber == 58, "nivel 58");
            Assert.That(_audio.MusicMenu, Is.False);
            Assert.That(_audio.MusicWorld, Is.EqualTo(3), "el lecho de Manaos");
            Assert.That(_audio.MusicNight, Is.False, "el 58 no es Muy difícil");
            Assert.That(_audio.MusicLayers, Is.EqualTo(0), "sin cuias selladas, sin percusión");
            Assert.That(_audio.AmbientWorld, Is.EqualTo(3), "el agua de Manaos (07 §9.1)");
            Assert.That(_audio.AmbientLevel, Is.EqualTo(58));
            Assert.That(_audio.AmbientRain, Is.EqualTo(AmbientPlan.HasRain(58, 3, 1.0)), "la lluvia del 58, siempre la misma");
            Assert.That(_probe.LastAmbientWorld, Is.EqualTo(3), "AF_STATE ambient al cargar el nivel");
        }

        [UnityTest]
        public IEnumerator VeryHard_Level60_PlaysTheNightBed()
        {
            yield return PlayModeHelpers.LoadGame(60, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Level.DifficultyLabel, Is.EqualTo(AF.Levels.LevelDefinition.LabelVeryHard), "el 60 es Muy difícil");
            Assert.That(_audio.MusicWorld, Is.EqualTo(3));
            Assert.That(_audio.MusicNight, Is.True, "07 §9.3: Muy difícil, la variante nocturna del lecho");
            Assert.That(_probe.Music[_probe.Music.Count - 1].Night, Is.True, "AF_STATE music con night");
            Assert.That(_audio.AmbientWorld, Is.EqualTo(3), "el ambiente no cambia por la dificultad");
        }

        [UnityTest]
        public IEnumerator Level1_LayersFollowTheSeals_UpToTheRemoteConfigKey_AndTheToadaDucks()
        {
            AudioConfig.MusicPercussionLayers = 3;
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            int max = SolveTrackingLayers(boot);
            Assert.That(max, Is.EqualTo(3), "con audio_music_percussion_layers = 3, las tres capas");

            PlayModeHelpers.Step(boot, 3000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(_audio.MusicLayers, Is.EqualTo(3), "todo sellado: todas las capas");
            Assert.That(_audio.Mix.Ducked, Is.True, "07 §9.4: el lecho baja durante la toada");
            Assert.That(_probe.Music[_probe.Music.Count - 1].Ducked, Is.True, "AF_STATE music con ducked");
            _audio.Tick(AudioMix.ToadaDuckMs + 1f);
            Assert.That(_audio.Mix.Ducked, Is.False, "y vuelve al terminar");
            Assert.That(_probe.Music[_probe.Music.Count - 1].Ducked, Is.False, "la sonda lo cuenta al cambiar");

            Assert.That(boot.PressHome(), Is.True);
            Assert.That(_audio.MusicMenu && _audio.AmbientWorld == 0, Is.True, "Inicio: el tema de menú");
        }

        [UnityTest]
        public IEnumerator Level1_WithZeroLayers_TheBedPlaysAlone()
        {
            AudioConfig.MusicPercussionLayers = 0;
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(SolveTrackingLayers(boot), Is.EqualTo(0), "audio_music_percussion_layers = 0: solo el lecho");
            Assert.That(_audio.MusicWorld, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StuckModal_DucksMusicAndAmbient_AndFaunaKeepsItsClock()
        {
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.Playing, "fin de la intro");
            Assert.That(_audio.AmbientWorld, Is.EqualTo(1));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.StuckModal, "modal Aguas estancadas");
            Assert.That(_audio.Mix.MusicGain, Is.EqualTo(AudioMix.DuckGain).Within(1e-6f), "07 §9.4: −6 dB con el modal");
            Assert.That(_audio.Mix.AmbientGain, Is.EqualTo(AudioMix.DuckGain).Within(1e-6f), "el ambiente también");
            Assert.That(_probe.Music[_probe.Music.Count - 1].Ducked, Is.True);

            // Fauna (07 §9.1): un animal de los del Estuario en 45 s como mucho, atenuado como el resto del ambiente.
            _audio.Clear();
            _audio.Tick(AmbientPlan.FaunaMaxMs + 1f);
            Assert.That(_audio.FaunaCount(true), Is.EqualTo(1), "un animal cada 20-45 s");
            Assert.That(_audio.Requested(SfxId.WorldBanner), Is.False, "nunca la piha");

            Assert.That(boot.PressUndo(), Is.True);
            Assert.That(_audio.Mix.Ducked, Is.False, "el modal se cerró");
            Assert.That(_probe.Music[_probe.Music.Count - 1].Ducked, Is.False);
        }

        [UnityTest]
        public IEnumerator SimulatedAd_SilencesTheMenuTheme_AndItComesBackIn500Ms()
        {
            LaunchOptions.ClockOverride = new ManualClock(new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            Assert.That(_audio.MusicMenu, Is.True);
            AdOverlay ad = boot.AdOverlayView!;
            AdResult? result = null;
            boot.Ads!.ShowRewarded(AdPlacement.Hint, r => result = r);
            Assert.That(_audio.Mix.MusicGain + _audio.Mix.AmbientGain, Is.EqualTo(0f), "07 §9.4: la música se calla durante el anuncio");
            ad.Tick((float)FakeAdService.RewardedDuration.TotalMilliseconds);
            ad.CloseButton.onClick.Invoke();
            Assert.That(result, Is.Not.Null);
            Assert.That(_audio.Mix.MusicGain, Is.LessThan(0.05f), "vuelve desde 0");
            _audio.Tick(AudioMix.AdFadeMs);
            Assert.That(_audio.Mix.MusicGain, Is.EqualTo(1f), "0,5 s después, a su nivel");
        }

        [UnityTest]
        public IEnumerator RealService_MusicOff_SilencesBedLayersAndAmbientAtOnce_AndItsFrameDoesNotAllocate()
        {
            AudioCues.Override = null;
            _real = UnityAudioService.Ensure();
            _real.Probe = null;
            _real.Configure(SoundSettings.Defaults);
            _real.SetModalDuck(false);
            _real.SetAdActive(false);
            _real.SetMusic(1, 2, false);
            _real.SetAmbient(1, true, 1);
            UnityAudioService s = _real;
            yield return WaitLong(() => s.WorldBedKey == 2 && s.WaterWorld == 1 && s.RainReady, "lecho, agua y lluvia del Estuario", 30f);
            yield return WaitLong(() => s.MusicVolume(0) > 0.5f && s.MusicVolume(2) > 0.3f && s.WaterVolume > 0.5f && s.RainVolume > 0.3f, "fundidos de entrada", 10f);
            Assert.That(s.MusicVolume(3), Is.EqualTo(0f), "con 2 capas, la tercera calla");

            s.Configure(new SoundSettings(true, false, true));
            for (int v = 0; v < UnityAudioService.MusicVoices; v++)
            {
                Assert.That(s.MusicVolume(v), Is.EqualTo(0f), "Música apagada: el lecho y sus capas, al momento (fuente " + v + ")");
            }

            Assert.That(s.WaterVolume + s.RainVolume, Is.EqualTo(0f), "y el ambiente");
            yield return null;
            yield return null;
            Assert.That(s.MusicVolume(0) + s.WaterVolume, Is.EqualTo(0f), "y siguen callados");
            s.Configure(SoundSettings.Defaults);
            yield return WaitLong(() => s.MusicVolume(0) > 0.5f && s.WaterVolume > 0.5f, "vuelven al encender Música", 10f);

            // El fotograma del servicio, con capas que entran y salen, la toada, un modal y la fauna: sin asignaciones.
            for (int i = 0; i < 30; i++)
            {
                s.Tick(16f);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 3000; i++)
            {
                if (i % 500 == 0)
                {
                    s.SetMusic(1, (i / 500) % 3, false);
                    s.SetModalDuck((i / 500) % 2 == 1);
                }

                if (i == 1000)
                {
                    s.Play(SfxId.WinToada, 0);
                }

                s.Tick(16f);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L), "sin coste por fotograma que asigne memoria (07 §9.4; 08 §5.4)");

            // La variante nocturna y el inicio cambian de pieza con un fundido.
            s.SetModalDuck(false);
            s.SetMusic(1, 0, true);
            yield return WaitLong(() => s.WorldBedKey == 3, "lecho nocturno", 30f);
            s.SetMenuMusic();
            s.SetAmbient(0, false, 0);
            yield return WaitLong(() => s.MenuReady && s.MenuVolume > 0.3f && s.MusicVolume(0) == 0f && s.WaterVolume == 0f, "tema de menú sin río", 30f);
            Assert.That(s.WorldBedKey == 3 && s.WaterWorld == 1, Is.True, "el lecho nocturno y el agua, parados y guardados para la vuelta");
        }

        [UnityTest]
        public IEnumerator RealService_HomeAndBackToTheSameWorld_KeepsBedAndWater_AnotherWorldReleasesThem_AndMusicOffGeneratesNothing()
        {
            AudioCues.Override = null;
            _real = UnityAudioService.Ensure();
            _real.Probe = null;
            _real.Configure(SoundSettings.Defaults);
            _real.SetModalDuck(false);
            _real.SetAdActive(false);
            UnityAudioService s = _real;
            s.SetMusic(1, 0, false);
            s.SetAmbient(1, false, 1);
            yield return WaitLong(() => s.WorldBedKey == 2 && s.WaterWorld == 1 && s.MusicVolume(0) > 0.5f && s.WaterVolume > 0.5f, "lecho y agua del Estuario", 30f);
            int beds = s.WorldBedRenders;
            int waters = s.WaterRenders;

            // Al inicio (lo que pide AudioCues.Home): se funden y se paran, pero se quedan.
            s.SetMenuMusic();
            s.SetAmbient(0, false, 0);
            yield return WaitLong(() => s.MenuReady && s.MenuVolume > 0.3f && s.MusicVolume(0) == 0f && s.WaterVolume == 0f, "tema de menú", 30f);
            Assert.That(s.WorldBedKey, Is.EqualTo(2), "el lecho del mundo 1 se guarda en el inicio");
            Assert.That(s.WaterWorld, Is.EqualTo(1), "y el agua también");

            // Vuelta a un nivel del mismo mundo: suenan desde el primer fotograma sin generar nada.
            s.SetMusic(1, 0, false);
            s.SetAmbient(1, false, 2);
            yield return null;
            Assert.That(s.WorldBedKey == 2 && s.WaterWorld == 1, Is.True, "los mismos clips");
            yield return WaitLong(() => s.MusicVolume(0) > 0.5f && s.WaterVolume > 0.5f, "vuelven con su fundido", 5f);
            Assert.That(s.WorldBedRenders, Is.EqualTo(beds), "volver al mismo mundo no genera otro lecho");
            Assert.That(s.WaterRenders, Is.EqualTo(waters), "ni otra agua");

            // Con Música apagada no se genera nada: otro mundo suelta lo que había y espera a que se encienda.
            s.Configure(new SoundSettings(true, false, true));
            s.SetMusic(2, 0, false);
            s.SetAmbient(2, false, 101);
            yield return WaitLong(() => s.WorldBedKey == 0 && s.WaterWorld == 0, "el mundo 1 se suelta al pedir el 2", 10f);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.That(s.WorldBedRenders == beds && s.WaterRenders == waters, Is.True, "Música apagada: nada se genera");
            s.Configure(SoundSettings.Defaults);
            yield return WaitLong(() => s.WorldBedKey == 4 && s.WaterWorld == 2 && s.MusicVolume(0) > 0.5f, "al encenderla, el lecho y el agua del mundo 2", 30f);
            Assert.That(s.WorldBedRenders, Is.EqualTo(beds + 1));
            Assert.That(s.WaterRenders, Is.EqualTo(waters + 1));
        }

        // Como PlayModeHelpers.WaitFor, con un tope propio: generar un lecho o el agua lleva un hilo de trabajo.
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

        // Resuelve el nivel con el solucionador y devuelve las capas más altas que se pidieron al sellar.
        private int SolveTrackingLayers(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            int max = 0;
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
                PlayModeHelpers.StepUntilIdle(boot, 4000);
                max = Math.Max(max, _audio.MusicLayers);
            }

            return max;
        }

        private sealed class Recorder : IAudioProbe
        {
            public List<MusicReport> Music { get; } = new List<MusicReport>();

            public int LastAmbientWorld { get; private set; } = -1;

            public void Sfx(string id, float gainDb, int tint)
            {
            }

            public void Haptic(string kind)
            {
            }

            public void Build(double workerMs, double mainMs, int clips, long pcmKiB)
            {
            }

            void IAudioProbe.Music(in MusicReport report)
            {
                Music.Add(report);
            }

            public void Ambient(int world, bool rain, int level)
            {
                LastAmbientWorld = world;
            }
        }
    }
}
