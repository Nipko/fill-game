#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.Audio;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            GameScreen.SimulatedScreen = null;
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
            MapArtRule.ForceProvisional = null;
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
                Cuia target = flow.Session.Current[move.Target];
                float full = PourLoopSynth.LevelForDoses(target.Count, target.Capacity);
                Assert.That(_audio.LastPourLevel, Is.InRange(0f, full + 1e-3f), "vertido " + i + ": el tono sigue al nivel del destino, sin pasarse");
                if (target.Count >= 2)
                {
                    Assert.That(_audio.LastPourLevel, Is.GreaterThan(0f), "vertido " + i + ": con 2 dosis o más, por encima de los 380 Hz");
                }

                // 07 §9.3: la primera capa de percusión del lecho entra al sellar la primera cuia.
                bool anySealed = false;
                for (int c = 0; c < flow.Session.Current.Count; c++)
                {
                    anySealed |= flow.Session.Current.IsSealed(c);
                }

                if (anySealed)
                {
                    Assert.That(_audio.MusicLayers, Is.GreaterThanOrEqualTo(1), "vertido " + i + ": con una cuia sellada, percusión en el lecho");
                }
                else
                {
                    Assert.That(_audio.MusicLayers, Is.EqualTo(0), "vertido " + i + ": sin cuias selladas, solo el lecho");
                }
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
        public IEnumerator DevReject_AnInvalidDestination_IsARejection_WithTheWoodAndNoHaptic()
        {
            // Observación 4 de la prueba de la ola 4 (tarea AU3): sin el extra, un destino que no puede recibir el vertido pero
            // puede ser origen cambia la selección (C5); con af_dev_reject (DevRejectOverride en el test) es un rechazo.
            LaunchOptions.DevRejectOverride = true;
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            BoardState board = boot.Flow!.Session.Current;
            int source = -1;
            int target = -1;
            for (int i = 0; i < board.Count && target < 0; i++)
            {
                for (int j = 0; j < board.Count && PourRules.CanBeSource(board, i); j++)
                {
                    if (j != i && PourRules.CanBeSource(board, j) && !PourRules.IsLegal(board, i, j))
                    {
                        source = i;
                        target = j;
                        break;
                    }
                }
            }

            Assert.That(target, Is.GreaterThanOrEqualTo(0), "el nivel 1 tiene un destino que no recibe y puede ser origen");
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            _audio.Clear();
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Rejected), "C5 con tap_switch_selection = false: rechazo");
            Assert.That(_audio.Heard(SfxId.Illegal), Is.True, "se oye sfx_illegal");
            Assert.That(_audio.HapticTotal(), Is.EqualTo(0), "02 §4: nunca háptico en un rechazo");
            Assert.That(LaunchOptions.ResolveTapSwitchSelection(), Is.False);
            LaunchOptions.DevRejectOverride = null;
            Assert.That(LaunchOptions.ResolveTapSwitchSelection(), Is.EqualTo(GameConfig.TapSwitchSelection), "sin el extra, el de GameConfig");
        }

        [UnityTest]
        public IEnumerator Deselect_TappingTheLiftedCuiaAgain_PlaysNothing()
        {
            // 02 §4.1: «Deselección | Baja a su sitio | 100 ms | ease-in | — | —», sin audio ni háptico (revisión de AU1). Se
            // comprueba en el juego, no solo la falta de una API: ningún gancho, con el nombre que sea, suena al bajar la cuia.
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            BoardState board = boot.Flow!.Session.Current;
            int full = -1;
            for (int i = 0; i < board.Count; i++)
            {
                if (!board[i].IsEmpty)
                {
                    full = i;
                    break;
                }
            }

            Assert.That(full, Is.GreaterThanOrEqualTo(0), "el nivel 1 tiene cuias con fruta");
            _audio.Clear();
            Assert.That(boot.TapCuia(full).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(_audio.Heard(SfxId.Select), Is.True, "subirla suena con la nota de su fruta");

            _audio.Clear();
            Assert.That(boot.TapCuia(full).Kind, Is.EqualTo(TapKind.Deselected));
            Assert.That(_audio.Events.Count, Is.EqualTo(0), "bajar la selección no suena ni vibra");
            PlayModeHelpers.Step(boot, 200);
            Assert.That(_audio.Events.Count, Is.EqualTo(0), "tampoco al terminar de bajar (100 ms)");
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
            Assert.That(boot.BackBlocked(), Is.False, "jugando, Atrás abre la pausa: no vibra (02 §3.5)");
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

        [UnityTest]
        public IEnumerator Luciernaga_RevealsWithItsSound()
        {
            FunnelSetup.Hooks();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            _audio.Clear();
            Assert.That(boot.PressReveal(), Is.True, "el regalo del 100 (04 §2.5)");
            Assert.That(_audio.Requested(SfxId.Reveal), Is.False, "elegir la cuia aún no revela");
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored), "el toque elige la cuia turbia");
            Assert.That(boot.Flow!.Session.Current[0].HiddenMask, Is.EqualTo(0), "revelada");
            Assert.That(_audio.Count(SfxId.Reveal, true), Is.EqualTo(1), "07 §9.2: la Luciérnaga suena al revelar");
            Assert.That(_audio.Requested(SfxId.Select), Is.False, "y no como una selección");
        }

        [UnityTest]
        public IEnumerator RiverLowered_PlaysTheRiverDrop()
        {
            // El flujo avisa a la vista con OnRiverLowered antes del teñido corto de la variante aliviada (LevelFlow.BeginAttempt;
            // esa llamada la prueban los tests de EditMode del flujo); aquí, el gancho de la vista, como en NoticeTests.
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            _audio.Clear();
            boot.View!.OnRiverLowered("plus_empty", 2000);
            Assert.That(boot.View.BannerText, Is.EqualTo(UiText.RiverLoweredCuia), "el cartel «El río bajó»");
            Assert.That(_audio.Count(SfxId.RiverDrop, true), Is.EqualTo(1), "07 §9.2: el agua que baja");
        }

        [UnityTest]
        public IEnumerator GivingUp_CostsACanoe_WithTheCanoeLost_AndNoTapOnTop()
        {
            FunnelSetup.Hooks();
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            view.Stuck.GiveUpButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            Assert.That(_audio.Requested(SfxId.CanoeLost), Is.False, "¿Seguro? aún no cobra la canoa");

            _audio.Clear();
            view.GiveUp.GiveUpButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(4), "se pierde una canoa");
            Assert.That(_audio.Count(SfxId.CanoeLost, true), Is.EqualTo(1), "07 §9.2: la canoa que vuelca");
            Assert.That(_audio.StingerCount(StingerId.LevelFailed, true), Is.EqualTo(1), "AU3: el remate de Nivel fallido, al abrirse");
            Assert.That(_audio.Count(SfxId.UiTap, false), Is.EqualTo(0), "el botón suena con lo que hace, sin el toque encima");
            Assert.That(_audio.HapticTotal(), Is.EqualTo(0), "sin háptico");
        }

        [UnityTest]
        public IEnumerator LevelFailed_Resumed_DoesNotPlayItsStingerAgain()
        {
            // Revisión de AU3: el remate suena en la transición real a Nivel fallido, no al reabrir la pantalla desde el estado
            // guardado (en el teléfono la reanudación llega desde el inicio: sonaría el remate de algo que no acaba de pasar).
            FunnelSetup.Hooks();
            var store = new MemoryProfileStore(FunnelSetup.Profile(500));
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            FunnelSetup.GetStuck(boot);
            boot.View!.Stuck.GiveUpButton.onClick.Invoke();
            boot.View.GiveUp.GiveUpButton.onClick.Invoke();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(_audio.StingerCount(StingerId.LevelFailed, true), Is.EqualTo(1), "la transición real: su remate");

            _audio.Clear();
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap again = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(again);
            PlayModeHelpers.Step(again, 300);
            Assert.That(again.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(again.View!.Failed.Visible, Is.True, "Nivel fallido reaparece");
            Assert.That(_audio.StingerCount(StingerId.LevelFailed, false), Is.EqualTo(0), "al reanudar, sin el remate");
        }

        [UnityTest]
        public IEnumerator Lobby_ACanoeComesBack_WithItsSound_Once()
        {
            var clock = new ManualClock(LobbyT0, TimeSpan.FromHours(-5));
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesAt31(clock)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            yield return null;
            Assert.That(lobby.CanoesCountShown, Is.EqualTo(0));
            Assert.That(_audio.Requested(SfxId.CanoeBack), Is.False, "al abrir el inicio, el contador no suena");

            _audio.Clear();
            clock.Advance(TimeSpan.FromMinutes(19));
            yield return null;
            yield return null;
            Assert.That(lobby.CanoesCountShown, Is.EqualTo(1), "vuelve una canoa");
            Assert.That(_audio.Count(SfxId.CanoeBack, true), Is.EqualTo(1), "07 §9.2: la canoa que vuelve");
            yield return null;
            yield return null;
            Assert.That(_audio.Count(SfxId.CanoeBack, false), Is.EqualTo(1), "una sola vez");
        }

        [UnityTest]
        public IEnumerator WorldBanner_Level41_PlaysThePihaOnce()
        {
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(boot.View!.WorldBannerView.Visible, Is.True);
            Assert.That(_audio.Count(SfxId.WorldBanner, true), Is.EqualTo(1), "07 §9.2: la piha, al entrar la tarjeta del banner");
            Assert.That(_audio.StingerCount(StingerId.WorldBanner, true), Is.EqualTo(1), "AU3: su remate, con la piha");
            Assert.That(_audio.Requested(SfxId.MechCard), Is.False, "el 41 no tiene tarjeta previa");
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(_audio.Count(SfxId.WorldBanner, false), Is.EqualTo(1), "mientras espera el toque, no se repite");
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(flow.Phase, Is.Not.EqualTo(FlowPhase.LevelCard), "cerrado: la intro");
            Assert.That(_audio.Count(SfxId.WorldBanner, false), Is.EqualTo(1), "una sola vez, también al cerrarse");
            Assert.That(_audio.StingerCount(StingerId.WorldBanner, false), Is.EqualTo(1), "y su remate, una vez");
        }

        [UnityTest]
        public IEnumerator LevelCard_Level43_PlaysTheMechanicCard()
        {
            yield return CardSetup.Load(43);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(boot.Flow.LevelCard, Is.Not.Null, "el estreno de la Hoja");
            Assert.That(_audio.Count(SfxId.MechCard, true), Is.EqualTo(1), "07 §9.2: la tarjeta previa");
            Assert.That(_audio.StingerCount(StingerId.MechCard, true), Is.EqualTo(1), "AU3: su remate");
            Assert.That(_audio.StingerCount(StingerId.WorldBanner, false), Is.EqualTo(0), "sin el remate del banner");
            Assert.That(_audio.Requested(SfxId.WorldBanner), Is.False, "sin banner de mundo");
            Assert.That(boot.TapAnywhere(), Is.True, "un toque la cierra");
            PlayModeHelpers.Step(boot, 500);
            Assert.That(_audio.Count(SfxId.MechCard, false), Is.EqualTo(1), "una vez");
        }

        [UnityTest]
        public IEnumerator LevelCard_Level60_DifficultyOnly_PlaysTheCardWithoutTheMechanicStinger()
        {
            // Revisión de AU3: la tarjeta que solo anuncia la dificultad (el 60, Muy difícil sin intro_mechanic) no presenta
            // ninguna mecánica: suena sfx_mech_card (AU1), pero no sting_mech_card, que es de los estrenos.
            yield return CardSetup.Load(60);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(boot.View!.LevelCard.Card!.Kind, Is.EqualTo(LevelCardKind.Difficulty), "el 60 no estrena nada");
            Assert.That(_audio.Count(SfxId.MechCard, true), Is.EqualTo(1), "07 §9.2: la tarjeta previa");
            Assert.That(_audio.StingerCount(StingerId.MechCard, false), Is.EqualTo(0), "sin el remate de mecánica");
        }

        [UnityTest]
        public IEnumerator FruitNew_Level4_PlaysTheCardSoundAndItsStingerOnce()
        {
            // Pendiente de AU3, cerrado en FX6: sting_fruit_new suena con la tarjeta «Fruta nueva» (F5) como los otros remates
            // (el servicio lo deja para después de la toada, va con el ajuste Música y a −8 dB), junto a sfx_fruit_new afinado
            // a su fruta, una vez al salir la tarjeta y nunca antes.
            FunnelSetup.Hooks();
            GameScreen.FruitNewOverride = true;
            try
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
                yield return PlayModeHelpers.LoadGame(4, new MemoryProgressStore(4, 3));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                FruitNewCard card = boot.View!.FruitNewView;
                for (int t = 0; t < 14000 && !card.Visible; t += PlayModeHelpers.FrameMs)
                {
                    Assert.That(_audio.StingerCount(StingerId.FruitNew, false), Is.EqualTo(0), "el remate no sale antes que la tarjeta (" + t + " ms)");
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                Assert.That(card.Visible, Is.True, "la tarjeta de la guanábana sale tras la intro del 4");
                Assert.That(_audio.Count(SfxId.FruitNew, true), Is.EqualTo(1), "07 §9.2: sfx_fruit_new con la tarjeta");
                Assert.That(_audio.StingerCount(StingerId.FruitNew, true), Is.EqualTo(1), "AU3: su remate, con la tarjeta");
                PlayModeHelpers.Step(boot, 8000);
                Assert.That(card.Visible, Is.False, "la tarjeta ya se fue");
                Assert.That(_audio.StingerCount(StingerId.FruitNew, false), Is.EqualTo(1), "una sola vez");
                Assert.That(_audio.Count(SfxId.FruitNew, false), Is.EqualTo(1), "y su efecto, una vez");
            }
            finally
            {
                GameScreen.FruitNewOverride = null;
            }
        }

        [UnityTest]
        public IEnumerator Hoja43_Opening_PlaysItsMechanicSound()
        {
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => (ev.HojaOpenedMask & (1 << hoja)) != 0);
            Assert.That(path, Is.Not.Null, "vertidos que abren la Hoja del 43");
            MechanicTestKit.PlayPrefix(boot, path!);
            Assert.That(_audio.Requested(SfxId.MechHoja), Is.False, "aún cerrada");
            _audio.Clear();
            MechanicTestKit.TapPour(boot, path![path.Length - 1], out _);
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(boot.Flow!.Session.Current[hoja].Mechanic, Is.EqualTo(MechanicType.None), "abierta");
            Assert.That(_audio.Count(SfxId.MechHoja, true), Is.EqualTo(1), "la lista, §3: la Hoja que se abre, por el paso de MechanicStepQueue");
        }

        [UnityTest]
        public IEnumerator Arrival_AtAStop_PlaysTheFact()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = true;
            MapArtRule.ForceProvisional = true;
            yield return VictoryArrivalTests.Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.Arrival!.Stop, Is.Not.Null, "el 20 llega a Alter do Chão");
            Assert.That(view.Arrival.Started, Is.True);
            Assert.That(_audio.Count(SfxId.Fact, true), Is.EqualTo(1), "07 §9.2: la llegada a la parada");
            Assert.That(_audio.StingerCount(StingerId.Arrival, true), Is.EqualTo(1), "AU3: el remate de la llegada (el servicio lo deja para después de la toada)");
        }

        [UnityTest]
        public IEnumerator StoreSheet_OpensWithItsSound_BuysOrCancelsWithOneSound()
        {
            var clock = new ManualClock(LobbyT0, TimeSpan.FromHours(-5));
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(NoCanoesAt31(clock)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            _audio.Clear();
            lobby.CanoesButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            Assert.That(_audio.Count(SfxId.UiTap, true), Is.EqualTo(1), "un botón sin sonido propio suena con el toque");
            yield return null;

            StoreSheet sheet = boot.StoreSheetView!;
            _audio.Clear();
            lobby.NoCanoes.InfiniteButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.True, "la hoja «Compra simulada»");
            Assert.That(_audio.Count(SfxId.StoreOpen, true), Is.EqualTo(1), "07 §9.2: abrir la tienda");
            Assert.That(_audio.Count(SfxId.UiTap, false), Is.EqualTo(0), "sin el toque encima");

            _audio.Clear();
            sheet.CancelButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.False);
            Assert.That(_audio.Count(SfxId.UiBack, true), Is.EqualTo(1), "cancelar: un solo sfx_ui_back");
            Assert.That(_audio.Count(SfxId.UiTap, false) + _audio.Count(SfxId.UiBuy, false), Is.EqualTo(0));

            lobby.NoCanoes.InfiniteButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.True);
            _audio.Clear();
            sheet.BuyButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.False);
            Assert.That(_audio.Count(SfxId.UiBuy, true), Is.EqualTo(1), "comprar: sfx_ui_buy");
            Assert.That(_audio.Count(SfxId.UiBack, false) + _audio.Count(SfxId.UiTap, false), Is.EqualTo(0), "y nada más");
        }

        [UnityTest]
        public IEnumerator SettingsButton_Taps_AndTheModalX_SoundsOneBack()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            _audio.Clear();
            boot.Lobby!.SettingsButton.onClick.Invoke();
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.Visible, Is.True);
            Assert.That(_audio.Count(SfxId.UiTap, true), Is.EqualTo(1), "el engranaje no tiene sonido propio: el toque");

            _audio.Clear();
            settings.CloseButton.onClick.Invoke();
            Assert.That(settings.Visible, Is.False);
            Assert.That(_audio.Count(SfxId.UiBack, true), Is.EqualTo(1), "la X de un modal: un solo sfx_ui_back");
            Assert.That(_audio.Count(SfxId.UiTap, false), Is.EqualTo(0), "sin el toque encima");
        }

        [UnityTest]
        public IEnumerator Settings_At130In16x9_TheScrollViewportStaysInsideTheCream()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;
            SettingsScreen settings = boot.SettingsView!;
            foreach (float scale in new[] { 1f, 1.3f })
            {
                settings.FontScale = scale;
                Canvas.ForceUpdateCanvases();
                RectTransform panel = settings.Panel;
                var corners = new Vector3[4];
                settings.Viewport.GetWorldCorners(corners);
                float bottom = panel.InverseTransformPoint(corners[0]).y;
                float top = panel.InverseTransformPoint(corners[1]).y;
                string where = "al " + Mathf.RoundToInt(scale * 100f) + " %";
                Assert.That(bottom - panel.rect.yMin, Is.GreaterThanOrEqualTo(SettingsScreen.PanelInsetDp - 0.5f), "el visor acaba 16 dp por encima del borde de la tarjeta: " + where);
                Assert.That(top, Is.LessThanOrEqualTo(panel.rect.yMax + 0.5f), where);
                if (scale > 1f)
                {
                    Assert.That(settings.Scrolls, Is.True, "en 16:9 al 130 % no cabe: se desplaza");
                }

                if (!settings.Scrolls)
                {
                    Assert.That(settings.FadeTopVisible || settings.FadeBottomVisible, Is.False, "sin desplazamiento, sin fundidos: " + where);
                    continue;
                }

                // Revisión de AU1: con el contenido arriba, la última fila cortada se ve entre el 35 y el 60 % (no una franja de
                // unos dp que parece una línea suelta), con el fundido de abajo y sin el de arriba.
                int cut = 0;
                float shown = 0f;
                foreach (Button row in Rows(settings))
                {
                    var rc = new Vector3[4];
                    ((RectTransform)row.transform).GetWorldCorners(rc);
                    float rowBottom = settings.Viewport.InverseTransformPoint(rc[0]).y;
                    float rowTop = settings.Viewport.InverseTransformPoint(rc[1]).y;
                    float viewBottom = settings.Viewport.rect.yMin;
                    if (rowTop > viewBottom + 0.5f && rowBottom < viewBottom - 0.5f)
                    {
                        cut++;
                        shown = (rowTop - viewBottom) / (rowTop - rowBottom);
                    }
                }

                Assert.That(cut, Is.EqualTo(1), "una fila cortada por el borde de abajo del visor: " + where);
                Assert.That(shown, Is.InRange(SettingsScreen.CutRowMin - 0.01f, SettingsScreen.CutRowMax + 0.01f), "se ve a medias: " + where);
                Assert.That(settings.FadeBottomVisible, Is.True, "fundido de abajo: la lista sigue");
                Assert.That(settings.FadeTopVisible, Is.False, "arriba del todo, sin fundido de arriba");

                settings.ScrollToEnd();
                Assert.That(settings.FadeTopVisible, Is.True, "desplazada hasta el final: fundido de arriba");
                Assert.That(settings.FadeBottomVisible, Is.False, "y sin el de abajo: no queda nada debajo");
            }
        }

        // Las filas de Ajustes, de arriba abajo: Sonido, Música y Vibración, los idiomas, los patrones, Mensajes de TiTino, Pista
        // por inactividad y, desde F4, las frutas de «Sobre la Amazonía» (una por fila, de una o de dos columnas: las dos de una
        // fila se cortan a la vez).
        private static System.Collections.Generic.List<Button> Rows(SettingsScreen settings)
        {
            var rows = new System.Collections.Generic.List<Button>
            {
                settings.AudioButton(SettingsScreen.SoundId),
                settings.AudioButton(SettingsScreen.MusicId),
                settings.AudioButton(SettingsScreen.HapticsId),
            };
            foreach (string id in settings.LanguageOptions)
            {
                rows.Add(settings.LanguageButton(id)!);
            }

            rows.Add(settings.PatternsButton(PatternsSetting.Auto));
            rows.Add(settings.PatternsButton(PatternsSetting.On));
            rows.Add(settings.PatternsButton(PatternsSetting.Off));
            rows.Add(settings.SlothMessagesButton);
            rows.Add(settings.IdleHintButton); // MO3
            for (int i = 0; i < settings.FruitCount; i += settings.FruitColumns)
            {
                rows.Add(settings.FruitButton(i));
            }

            return rows;
        }

        // Como LuciernagaTests: carga, salta la intro y espera a la vista quieta en juego.
        private static IEnumerator LoadPlaying(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + level + " en juego");
            PlayModeHelpers.Step(boot, 50);
        }

        private static readonly DateTime LobbyT0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        // Inicio en el 31 sin canoas (como SettingsBackColdStartTests): la próxima vuelve a los 18 min.
        internal static Profile NoCanoesAt31(ManualClock clock)
        {
            LaunchOptions.ClockOverride = clock;
            LaunchOptions.LevelOffsetOverride = 30;
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.NextLevel = 31;
            profile.Progress.MaxLevel = 30;
            profile.Wallet.Canoes.Count = 0;
            profile.Wallet.Canoes.NextRegenAt = LobbyT0 + TimeSpan.FromMinutes(18);
            return profile;
        }
    }

    /// <summary>
    /// El Atrás de Android y el audio (tarea AU1; la lista, §7: «Atrás no puede cerrar un aviso obligatorio»): con Aguas
    /// estancadas delante, Atrás no cierra nada y vibra corto (<c>haptic_back_blocked</c>) sin <c>sfx_ui_back</c>; con Ajustes
    /// abierta, la cierra con un solo <c>sfx_ui_back</c> y sin háptico. Escape se inyecta con <see cref="InputTestFixture"/>, por
    /// el camino del juego (<c>GameBootstrap.Update</c>), como en <see cref="BackButtonTests"/>.
    /// </summary>
    public sealed class AudioBackTests : UiInputTestFixture
    {
        private const int FramesPressed = 3;
        private FakeAudioService _audio = null!;

        /// <inheritdoc />
        public override void Setup()
        {
            base.Setup();
            FunnelSetup.Hooks();
            _audio = new FakeAudioService();
            AudioCues.Override = _audio;
        }

        /// <inheritdoc />
        public override void TearDown()
        {
            LaunchOptions.Reset();
            AudioCues.Override = null;
            AudioCues.Install(null);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator AndroidBack_OnTheStuckModal_VibratesShort_WithoutTheBackSound()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.BackBlocked(), Is.True);

            _audio.Clear();
            yield return PressBack(boot, keyboard);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal), "Atrás no cierra Aguas estancadas (04 §6.6)");
            Assert.That(_audio.HapticCount(HapticKind.BackBlocked), Is.EqualTo(1), "02 §4: el háptico corto de Atrás bloqueado");
            Assert.That(_audio.HapticTotal(), Is.EqualTo(1), "solo ese");
            Assert.That(_audio.Requested(SfxId.UiBack), Is.False, "sin sfx_ui_back: no se volvió a ningún sitio");
        }

        [UnityTest]
        public IEnumerator AndroidBack_ClosesSettings_WithOneBackSound_AndNoHaptic()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;

            _audio.Clear();
            yield return PressBack(boot, keyboard);
            Assert.That(boot.SettingsView!.Visible, Is.False, "Atrás cierra Ajustes");
            Assert.That(_audio.Count(SfxId.UiBack, true), Is.EqualTo(1), "un solo sfx_ui_back");
            Assert.That(_audio.HapticTotal(), Is.EqualTo(0), "sin háptico");
        }

        [UnityTest]
        public IEnumerator AndroidBack_OnTheBareLobby_GoesToTheBackground_WithoutSoundOrHaptic()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            var task = new FakeAppTaskPort();
            GameBootstrap.AppTaskPortOverride = task;
            GameBootstrap.DailyPoolDisabledOverride = true;
            try
            {
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(20)));
                GameBootstrap boot = GameBootstrap.Current!;
                Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
                Assert.That(boot.Lobby!.OverlayVisible, Is.False);
                yield return null;

                _audio.Clear();
                yield return PressBack(boot, keyboard);
                Assert.That(boot.LastBackScreen, Is.EqualTo("lobby"));
                Assert.That(boot.LastBackAction, Is.EqualTo("background"), "02 §3.5: la app a segundo plano");
                Assert.That(task.Calls, Is.EqualTo(1));
                Assert.That(_audio.Requested(SfxId.UiBack), Is.False, "sin sfx_ui_back: la app se va");
                Assert.That(_audio.HapticTotal(), Is.EqualTo(0), "sin háptico");
            }
            finally
            {
                GameBootstrap.AppTaskPortOverride = null;
                GameBootstrap.ResetDailyOverrides();
            }
        }

        // Pulsa y suelta Escape con el bucle del juego en marcha unos fotogramas (como BackButtonTests).
        private IEnumerator PressBack(GameBootstrap boot, Keyboard keyboard)
        {
            boot.enabled = true;
            Press(keyboard.escapeKey);
            for (int i = 0; i < FramesPressed; i++)
            {
                yield return null;
            }

            Release(keyboard.escapeKey);
            for (int i = 0; i < FramesPressed; i++)
            {
                yield return null;
            }

            PlayModeHelpers.TakeClock(boot);
        }
    }
}
