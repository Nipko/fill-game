#nullable enable
using System;
using System.Collections;
using System.IO;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El tooltip de Marea, una sola vez en la vida (D-030, pregunta 15 (b); tarea DC23; 02 §13 y 06 §2.2, fila del 3), en la escena
    /// real y con el perfil:
    /// <list type="bullet">
    /// <item>en el 3 sale tras el vertido que no acerca (D-029, 56 a) y se apunta en <c>progress.undo_tip_seen</c> al mostrarse,
    /// con un guardado (en segundo plano con el perfil en disco: D-012); no vuelve en el reintento;</item>
    /// <item>visto en el 3, no sale en el 5 aunque se cierre la app (el perfil, otra vez desde su almacén); ni en el 3 otra
    /// vez;</item>
    /// <item>sin verlo en el 3 (ganado con la solución, sin un vertido que no acerque), sale al inicio del 5, tras la intro,
    /// en el mismo sitio y con la misma cola que en el 3; una vez: ni en el reintento del 5.</item>
    /// </list>
    /// </summary>
    public sealed class UndoTipTests
    {
        // Tope de espera al escritor de segundo plano en el test con el perfil en disco.
        private const int WaitMs = 10000;

        private string? _dir;

        [SetUp]
        public void SetUp()
        {
            TutorialSlothTests.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap? boot = GameBootstrap.Current;
            ProfileSaveBatch? batch = boot != null ? boot.SaveBatch : null;
            batch?.FlushSync(WaitMs);
            GameScreen.TutorialSlothOverride = null;
            GameScreen.TutorialBandOverrideDp = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            if (_dir != null && Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }

            _dir = null;
        }

        [UnityTest]
        public IEnumerator Level3_TheTipIsRecordedWhenShown_AndNotOnTheRetry()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return Load(3, store);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Profile!.Progress.UndoTipSeen, Is.False, "condición del test: perfil sin el tooltip visto");
            Assert.That(view.UndoTutorialDone, Is.False);

            // Tras la intro, nada: espera al vertido que no acerca (D-029, 56 a). Sin mostrarse, no se apunta.
            PlayModeHelpers.Step(boot, 6000);
            Assert.That(view.TutorialWaitsForPour, Is.True);
            Assert.That(boot.Profile!.Progress.UndoTipSeen, Is.False, "se apunta al mostrarse, no antes");
            int saves = store.SaveCount;

            TutorialSlothTests.PourIfUndoLevel(boot, 3);
            SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, "el 3");
            Assert.That(card.Text, Is.EqualTo(Loc.T(TutorialInstruction.UndoKey)));
            Assert.That(view.TutorialPointsAtUndo, Is.True, "la cola señala Marea");
            Assert.That(boot.Profile!.Progress.UndoTipSeen, Is.True, "apuntado al mostrarse");
            Assert.That(boot.UndoTipRecordedCount, Is.EqualTo(1));
            Assert.That(store.SaveCount, Is.GreaterThan(saves), "guardado en el perfil");
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.UndoTipSeen, Is.True, "en el almacén: sobrevive a cerrar la app");

            // Volver a la orilla: el mismo vertido tentador ya no lo trae.
            Assert.That(boot.PressRestart(), Is.True);
            StepUntilPlaying(boot);
            Assert.That(view.TutorialWaitsForPour, Is.False, "ya no espera ningún vertido");
            TutorialSlothTests.PourIfUndoLevel(boot, 3);
            for (int t = 0; t < 6000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialSlothVisible, Is.False, "no vuelve en el reintento (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialShownCount, Is.EqualTo(1), "una sola vez");
            Assert.That(boot.UndoTipRecordedCount, Is.EqualTo(1), "ni otro apunte");
        }

        [UnityTest]
        public IEnumerator SeenIn3_NotIn5_NorIn3Again_AfterClosingTheApp()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return Load(3, store);
            GameBootstrap boot = GameBootstrap.Current!;
            TutorialSlothTests.PourIfUndoLevel(boot, 3);
            TutorialSlothTests.WaitForInstruction(boot, "el 3");
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.UndoTipSeen, Is.True);

            // «Se cierra la app»: la escena otra vez, con el perfil leído de su almacén.
            yield return Load(5, store);
            boot = GameBootstrap.Current!;
            Assert.That(boot.Profile!.Progress.UndoTipSeen, Is.True, "leído del perfil");
            AssertNoTip(boot, "el 5 con el tooltip ya visto");

            yield return Load(3, store);
            boot = GameBootstrap.Current!;
            Assert.That(boot.View!.TutorialWaitsForPour, Is.False, "el 3 ya no espera el vertido");
            TutorialSlothTests.PourIfUndoLevel(boot, 3);
            AssertNoTip(boot, "el 3 con el tooltip ya visto");
        }

        [UnityTest]
        public IEnumerator NotSeenIn3_ShowsAtTheStartOf5_Once_WithTheSamePlaceAndTail()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));

            // El 3, ganado con la solución óptima: cada vertido acerca, así que el tooltip no sale (y no se apunta).
            yield return Load(3, store);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 6000);
            TapTheSolution(boot);
            for (int t = 0; t < 4000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(boot.View!.TutorialSlothVisible, Is.False, "ganado sin un vertido que no acerque (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.View!.TutorialShownCount, Is.EqualTo(0));
            Assert.That(boot.Profile!.Progress.UndoTipSeen, Is.False);
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.UndoTipSeen, Is.False);

            // El 5: tras la intro, con la tropa ya ida, TiTino señala Marea (02 §13: «o al inicio del nivel 5 si no ocurrió»).
            yield return Load(5, store);
            boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.TutorialWaitsForPour, Is.False, "en el 5 no espera ningún vertido: sale tras la intro");
            SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, "el 5");
            Assert.That(boot.Flow!.Session.AppliedPours, Is.EqualTo(0), "sin vertidos: al inicio del nivel");
            Assert.That(card.Text, Is.EqualTo(Loc.T(TutorialInstruction.UndoKey)), "el tooltip de Marea");
            Assert.That(card.Pose, Is.EqualTo(SlothPoseId.Present));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"level\":5"));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"key\":\"" + TutorialInstruction.UndoKey + "\""));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"undo\":true"));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"over_undo\":true"));
            Assert.That(view.TutorialSlothOnDock, Is.True, "el mismo sitio que en el 3: sobre el muelle, junto a la bandeja");
            Assert.That(view.TutorialPointsAtUndo, Is.True, "la misma cola, en el eje de Marea");
            Assert.That(card.TailVisible, Is.True);
            TutorialSlothTests.AssertDockAir(view, "el 5");
            TutorialSlothTests.AssertClear(view, "el 5");
            TutorialSlothTests.AssertTail(view, "el 5");
            Assert.That(boot.Profile!.Progress.UndoTipSeen, Is.True, "apuntado al mostrarse");
            Assert.That(ProfileJson.Deserialize(store.Json!).Progress.UndoTipSeen, Is.True);
            Assert.That(view.TutorialShownCount, Is.EqualTo(1));

            // Volver a la orilla en el 5: no vuelve.
            PlayModeHelpers.Step(boot, (int)card.DurationMs + 100);
            Assert.That(boot.PressRestart(), Is.True);
            StepUntilPlaying(boot);
            for (int t = 0; t < 6000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialSlothVisible, Is.False, "no vuelve en el reintento del 5 (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialShownCount, Is.EqualTo(1), "una sola vez");
            Assert.That(boot.UndoTipRecordedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Level5_WithTheProfileOnDisk_TheTipIsSavedInTheBackground()
        {
            // D-012: nada nuevo en el hilo principal; el cierre del lote lo entrega al escritor de segundo plano.
            _dir = Path.Combine(Application.temporaryCachePath, "af_undo_tip_" + Guid.NewGuid().ToString("N"));
            var local = new LocalProfileStore(_dir);
            local.Save(FunnelSetup.Profile(0));
            yield return Load(5, local);
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            Assert.That(batch.Writer, Is.Not.Null, "con el perfil en disco, el escritor de segundo plano está en marcha");
            Assert.That(batch.Writer!.WaitIdle(WaitMs), Is.True);
            int writes = batch.WriteCount;

            TutorialSlothTests.WaitForInstruction(boot, "el 5 con el perfil en disco");

            Assert.That(boot.UndoTipRecordedCount, Is.EqualTo(1));
            Assert.That(batch.WriteCount, Is.EqualTo(writes + 1), "el tooltip visto, en una escritura");
            Assert.That(batch.LastWriteInBackground, Is.True, "entregado al escritor: nada síncrono en el hilo principal");
            Assert.That(batch.FlushSync(WaitMs), Is.True);
            ProfileLoadResult loaded = new LocalProfileStore(_dir).Load();
            Assert.That(loaded.HasProfile, Is.True, "perfil en disco: " + loaded.Status);
            Assert.That(loaded.Profile!.Progress.UndoTipSeen, Is.True);
        }

        // El nivel en su primer intento, con el perfil de store, sin reloj y con la intro saltada (como TutorialSlothTests.Load).
        private static IEnumerator Load(int level, IProfileStore store)
        {
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        private static void StepUntilPlaying(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            for (int t = 0; t < 9000 && (flow.Phase != FlowPhase.Playing || view.IntroTroopVisible); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "condición del test: el intento nuevo, en juego");
        }

        private static void AssertNoTip(GameBootstrap boot, string at)
        {
            GameScreen view = boot.View!;
            for (int t = 0; t < 6000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialSlothVisible, Is.False, at + " (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialShownCount, Is.EqualTo(0), at);
            Assert.That(view.LastTutorialProbe, Is.Null, at);
            Assert.That(boot.UndoTipRecordedCount, Is.EqualTo(0), at);
        }

        // Juega la solución óptima del solver con la vista al día: cada vertido acerca a la victoria.
        private static void TapTheSolution(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
                PlayModeHelpers.StepUntilIdle(boot, 4000);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
        }
    }
}
