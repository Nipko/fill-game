#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.Audio;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Ganchos del audio provisional en el juego (tarea AU1; 07 §9.2-§9.4; 02 §4), con el servicio falso
    /// (<see cref="FakeAudioService"/>, instalado con <see cref="AudioCues.Override"/>): cada evento dispara su ID (intro, seleccionar,
    /// verter, sellar, ganar, comodines, Aguas estancadas…), los ajustes apagan al momento y se guardan, ningún háptico en el
    /// rechazo y silencio durante el anuncio simulado.
    /// </summary>
    public sealed class AudioHooksTests
    {
        private FakeAudioService _audio = null!;

        [SetUp]
        public void SetUp()
        {
            _audio = new FakeAudioService();
            AudioCues.Override = _audio;
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            AudioCues.Override = null;
            AudioCues.Install(null);
        }

        [UnityTest]
        public IEnumerator Level1_IntroSelectPourSealAndVictory_PlayTheirSounds()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            Assert.That(AudioCues.Service, Is.SameAs(_audio), "el arranque instala el servicio de los tests");
            Assert.That(_audio.MusicWorld, Is.EqualTo(1), "el lecho del Estuario en el nivel 1");
            Assert.That(_audio.MusicLayers, Is.EqualTo(0), "sin cuias selladas, sin percusión");

            // Llenar y teñir (07 §9.2 con D-010): el río, un chorro por fila, un plic por fruta y una nota por racha.
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(_audio.Count(SfxId.RiverRise, true), Is.EqualTo(1));
            Assert.That(_audio.Count(SfxId.Fill, true), Is.GreaterThanOrEqualTo(1));
            Assert.That(_audio.Count(SfxId.FruitPlip, true), Is.GreaterThanOrEqualTo(1));
            Assert.That(_audio.Count(SfxId.Tint, true), Is.GreaterThanOrEqualTo(2));
            Assert.That(_audio.Count(SfxId.MonkeyChirp, false), Is.LessThanOrEqualTo(1), "como mucho un chillido de miquito");

            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                _audio.Clear();
                Cuia source = flow.Session.Current[move.Source];
                Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(_audio.Events.Count, Is.GreaterThanOrEqualTo(1));
                Assert.That(_audio.Events[0].Sfx, Is.EqualTo(SfxId.Select), "seleccionar " + i);
                Assert.That(_audio.Events[0].Tint, Is.EqualTo(source.TopColor), "afinado a la fruta de arriba (07 §9.2)");
                Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
                PlayModeHelpers.StepUntilIdle(boot, 4000);
                Assert.That(_audio.Heard(SfxId.PourStart), Is.True, "vertido " + i + ": empieza el chorro");
                Assert.That(_audio.Heard(SfxId.PourEnd), Is.True, "vertido " + i + ": se corta");
                Assert.That(_audio.ActivePours, Is.EqualTo(0), "el bucle no se queda sonando");
                Assert.That(_audio.LastPourLevel, Is.GreaterThan(0f), "el tono sigue al nivel del destino");
            }

            Assert.That(_audio.Count(SfxId.Seal, true), Is.GreaterThanOrEqualTo(1), "la última cuia se tapa");
            Assert.That(_audio.HapticCount(HapticKind.Seal), Is.EqualTo(_audio.Count(SfxId.Seal, true)), "cada sellado con su háptico ligero");

            PlayModeHelpers.Step(boot, 3000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(_audio.Count(SfxId.WinToada, true), Is.EqualTo(1), "la toada");
            Assert.That(_audio.HapticCount(HapticKind.Win), Is.EqualTo(1), "y el háptico medio de la victoria");
            Assert.That(_audio.Count(SfxId.Confetti, true), Is.EqualTo(1), "el confeti de plumas");
            int ticks = Math.Min(SeedTicker.MaxTicks, flow.HarvestSeeds / SeedTicker.SeedsPerTick);
            Assert.That(_audio.Count(SfxId.SeedsTick, true), Is.InRange(ticks == 0 ? 0 : 1, ticks), "las semillas de la Cosecha: 1 por 10, máx. 20");
            Assert.That(_audio.Count(SfxId.Fact, false), Is.EqualTo(0), "la línea de fruta va sin sonido (D-017)");
            Assert.That(_audio.Mix.Ducked && !_audio.Mix.ModalDuck, Is.True, "07 §9.4: el lecho baja durante la toada, sin modal");
        }

        [UnityTest]
        public IEnumerator Rejection_SoundsTheNeutralWood_WithoutHaptic()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            BoardState board = boot.Flow!.Session.Current;
            int empty = -1;
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].IsEmpty)
                {
                    empty = i;
                }
            }

            Assert.That(empty, Is.GreaterThanOrEqualTo(0), "el nivel 1 tiene una cuia vacía");
            _audio.Clear();
            Assert.That(boot.TapCuia(empty).Kind, Is.EqualTo(TapKind.Rejected), "C2: elegir una vacía");
            Assert.That(_audio.Heard(SfxId.Illegal), Is.True, "07 §9.2: madera neutra");
            Assert.That(_audio.HapticTotal(), Is.EqualTo(0), "02 §4: nunca háptico en un rechazo");
            Assert.That(_audio.Requested(SfxId.Select), Is.False);
        }

        [UnityTest]
        public IEnumerator Stuck_Undo_Restart_PlayTheirSounds_AndTheModalDucksTheBed()
        {
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.Playing, "fin de la intro");
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.StuckModal, "modal Aguas estancadas");
            Assert.That(_audio.Heard(SfxId.Stuck), Is.True, "Aguas estancadas");
            Assert.That(_audio.Mix.ModalDuck, Is.True, "07 §9.4: el lecho baja 6 dB con el modal");
            Assert.That(boot.BackBlocked(), Is.True, "Atrás no cierra el modal: háptico corto");

            _audio.Clear();
            Assert.That(boot.PressUndo(), Is.True);
            Assert.That(_audio.Heard(SfxId.Undo), Is.True, "Marea");
            Assert.That(_audio.Mix.ModalDuck, Is.False, "el modal se cerró");
            Assert.That(boot.BackBlocked(), Is.False, "jugando, Atrás simplemente no hace nada");
            yield return PlayModeHelpers.WaitFor(() => boot.View!.IsViewIdle, "cola visual tras Marea");

            _audio.Clear();
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(_audio.Heard(SfxId.Restart), Is.True, "Volver a la orilla");
        }

        [UnityTest]
        public IEnumerator ExtraCuia_BringsCapi_WithItsSoundAndHaptic()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            _audio.Clear();
            Assert.That(boot.PressExtraCuia(), Is.True);
            Assert.That(_audio.Heard(SfxId.CapiPaddle), Is.True, "07 §9.2: Capi rema");
            Assert.That(_audio.HapticCount(HapticKind.Capi), Is.EqualTo(1), "con el háptico medio de Capi");
            Assert.That(_audio.Count(SfxId.UiTap, false), Is.EqualTo(0), "el comodín suena con su sonido, sin el toque encima");
        }

        [UnityTest]
        public IEnumerator Settings_SoundMusicAndHaptics_Rows_SwitchOffAtOnce_AndAreSaved()
        {
            var store = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.AudioLabel(SettingsScreen.SoundId), Is.EqualTo("Sonido"), "glosario §10");
            Assert.That(settings.AudioLabel(SettingsScreen.MusicId), Is.EqualTo("Música"));
            Assert.That(settings.AudioLabel(SettingsScreen.HapticsId), Is.EqualTo("Vibración"));
            foreach (string id in new[] { SettingsScreen.SoundId, SettingsScreen.MusicId, SettingsScreen.HapticsId })
            {
                Assert.That(settings.AudioMarked(id), Is.True, id + ": activado por defecto (08 §9)");
                Assert.That(((UnityEngine.RectTransform)settings.AudioButton(id).transform).rect.height, Is.GreaterThanOrEqualTo(48f), id);
            }

            // Sonido: se apaga en el acto y se guarda.
            settings.AudioButton(SettingsScreen.SoundId).onClick.Invoke();
            Assert.That(settings.AudioMarked(SettingsScreen.SoundId), Is.False);
            Assert.That(boot.Profile!.Settings.Sound, Is.False, "settings.sound (08 §4.3)");
            Assert.That(store.Load().Profile!.Settings.Sound, Is.False, "guardado");
            Assert.That(_audio.Mix.Settings.Sound, Is.False, "aplicado al momento");
            _audio.Clear();
            AudioCues.Play(SfxId.Seal);
            Assert.That(_audio.Heard(SfxId.Seal), Is.False, "ya no suena nada");

            // Vibración: el háptico tampoco.
            settings.AudioButton(SettingsScreen.HapticsId).onClick.Invoke();
            Assert.That(boot.Profile.Settings.Haptics, Is.False);
            AudioCues.Haptic(HapticKind.Seal);
            Assert.That(_audio.HapticTotal(), Is.EqualTo(0));

            // Música, por separado.
            settings.AudioButton(SettingsScreen.MusicId).onClick.Invoke();
            Assert.That(boot.Profile.Settings.Music, Is.False);
            Assert.That(_audio.Mix.MusicAudible, Is.False);

            // Se vuelven a encender.
            settings.AudioButton(SettingsScreen.SoundId).onClick.Invoke();
            settings.AudioButton(SettingsScreen.MusicId).onClick.Invoke();
            settings.AudioButton(SettingsScreen.HapticsId).onClick.Invoke();
            Assert.That(boot.Profile.Settings.Sound && boot.Profile.Settings.Music && boot.Profile.Settings.Haptics, Is.True);
            _audio.Clear();
            AudioCues.Seal();
            Assert.That(_audio.Heard(SfxId.Seal) && _audio.HapticCount(HapticKind.Seal) == 1, Is.True);

            // Lo guardado sale marcado al volver a abrir.
            Assert.That(boot.CloseSettings(), Is.True);
            boot.Profile.Settings.Music = false;
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(settings.AudioMarked(SettingsScreen.MusicId), Is.False);
            Assert.That(settings.AudioMarked(SettingsScreen.SoundId), Is.True);
        }

        [UnityTest]
        public IEnumerator SimulatedAd_SilencesEverything_AndTheSoundComesBackAfter()
        {
            LaunchOptions.ClockOverride = new ManualClock(new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc));
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            AdOverlay ad = boot.AdOverlayView!;
            Assert.That(boot.Ads!.CheckRewarded(AdPlacement.Hint).IsAllowed, Is.True);
            AdResult? result = null;
            boot.Ads.ShowRewarded(AdPlacement.Hint, r => result = r);
            Assert.That(ad.Visible, Is.True);
            Assert.That(_audio.Mix.AdActive, Is.True, "07 §9.4: todo se detiene durante el anuncio");
            _audio.Clear();
            AudioCues.Play(SfxId.UiTap);
            AudioCues.Haptic(HapticKind.Win);
            Assert.That(_audio.Heard(SfxId.UiTap), Is.False);
            Assert.That(_audio.HapticTotal(), Is.EqualTo(0));

            ad.Tick((float)FakeAdService.RewardedDuration.TotalMilliseconds);
            ad.CloseButton.onClick.Invoke();
            Assert.That(result, Is.Not.Null);
            Assert.That(_audio.Mix.AdActive, Is.False, "vuelve al cerrarse");
            Assert.That(_audio.Mix.SfxGain, Is.LessThan(0.05f), "con un fundido desde 0");
            _audio.Tick(AudioMix.AdFadeMs);
            Assert.That(_audio.Mix.SfxGain, Is.EqualTo(1f), "0,5 s después, a su nivel");
        }
    }
}
