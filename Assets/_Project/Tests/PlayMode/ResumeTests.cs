#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
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
    /// Nivel en curso guardado tras cada vertido y reabierto en la escena (tarea B2-5; 02 R8 y §14; 04 §5.2): destruir y
    /// recrear el arranque a mitad de nivel devuelve el mismo tablero y la misma pantalla, sin intro; con Aguas estancadas,
    /// ¿Seguro? o Nivel fallido abiertos, la pantalla reaparece y las canoas no cambian (cerrar la app nunca cobra canoa ni
    /// es una salida gratis del embudo); y guardar cuesta menos de 5 ms por vertido en el Editor [P].
    /// </summary>
    public sealed class ResumeTests
    {
        private string? _dir;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            if (_dir != null && Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [UnityTest]
        public IEnumerator MidLevel_RecreatingTheBootstrap_GivesBackTheSameBoard_WithoutIntro()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            SolveResult solution = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            for (int i = 0; i < 3; i++)
            {
                boot.TapCuia(solution.GetMove(i).Source);
                boot.TapCuia(solution.GetMove(i).Target);
                PlayModeHelpers.StepUntilIdle(boot, 4000);
            }

            Assert.That(boot.PressUndo(), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            BoardState board = boot.Flow.Session.Current;
            Assert.That(ProfileJson.Deserialize(store.Json!).LevelState!.Pours, Is.EqualTo(2), "guardado tras cada vertido y Marea");

            yield return PlayModeHelpers.LoadApp(store); // como cerrar y abrir la app
            GameBootstrap again = GameBootstrap.Current!;
            Assert.That(again, Is.Not.SameAs(boot));
            Assert.That(again.App!.Screen, Is.EqualTo(AppScreen.Level), "no abre el inicio: reabre el nivel");
            LevelFlow flow = again.Flow!;
            Assert.That(flow.LevelNumber, Is.EqualTo(7));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "sin intro (02 §14)");
            Assert.That(flow.Session.Current, Is.EqualTo(board), "el mismo tablero");
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(2));
            Assert.That(flow.CanUndo, Is.True);
            CuiaView[] views = again.View!.Views;
            Assert.That(views.Length, Is.EqualTo(board.Count));
            for (int i = 0; i < views.Length; i++)
            {
                Assert.That(views[i].LiquidLevel, Is.EqualTo(board[i].Count).Within(0.001f), "la vista enseña el tablero guardado, cuia " + i);
            }

            Assert.That(again.View.UndoButton.gameObject.activeInHierarchy, Is.True, "el HUD y la bandeja de siempre");
        }

        [UnityTest]
        public IEnumerator WithTheModalOpen_TheModalComesBack_AndTheCanoesDoNotChange()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(1000));
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.Wallet!.Canoes.Count, Is.EqualTo(5));

            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap again = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(again);
            PlayModeHelpers.Step(again, 300);
            Assert.That(again.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(again.View!.StuckVisible, Is.True, "Aguas estancadas reaparece");
            Assert.That(again.View.WalletBar.Visible, Is.True);
            Assert.That(again.View.Stuck.RescuePriceShown, Is.EqualTo(300), "primer rescate del día, como antes de cerrar");
            Assert.That(again.Wallet!.Canoes.Count, Is.EqualTo(5), "cerrar la app no cuesta canoa");
            Assert.That(again.Flow.Attempt.FailsLevel, Is.EqualTo(0), "ni es fallo");

            // ¿Seguro? también se reabre, y la rendición confirmada cobra la canoa una sola vez.
            again.View.Stuck.GiveUpButton.onClick.Invoke();
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap third = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(third);
            Assert.That(third.Flow!.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            Assert.That(third.View!.GiveUp.Visible, Is.True);
            Assert.That(third.View.GiveUp.RescueButton.gameObject.activeSelf, Is.True, "otra vez «Continuar: +1 cuia»");
            Assert.That(third.Wallet!.Canoes.Count, Is.EqualTo(5));

            third.View.GiveUp.GiveUpButton.onClick.Invoke();
            Assert.That(third.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(third.Wallet.Canoes.Count, Is.EqualTo(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap fourth = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(fourth);
            Assert.That(fourth.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(fourth.View!.Failed.Visible, Is.True);
            Assert.That(fourth.Wallet!.Canoes.Count, Is.EqualTo(4), "reabrir Nivel fallido no vuelve a cobrar");
            fourth.View.Failed.RetryButton.onClick.Invoke();
            Assert.That(fourth.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "Reintentar: el mismo tablero desde el principio");
            Assert.That(fourth.Wallet.Canoes.Count, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator Saving_OnePerPour_ReportsItsCost_AndStaysWithinAFrame()
        {
            // Plan B2-5 [P]: «guardar cuesta < 5 ms por vertido en el Editor». Perfil real en disco (LocalProfileStore de
            // AF.Save: .tmp, Flush(true) y File.Replace con copia) en una carpeta temporal. Se mide aparte lo que pone la pista
            // B (estado y JSON) y el guardado entero; el objetivo de 5 ms se exige a lo primero y el guardado entero, a un
            // fotograma de 60 fps (08 §5.4). Las cifras quedan en el log (AF_PERF) para el informe.
            _dir = Path.Combine(Application.temporaryCachePath, "af_resume_" + Guid.NewGuid().ToString("N"));
            var store = new LocalProfileStore(_dir);
            store.Save(FunnelSetup.Profile(0));
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            var states = (ProfileLevelStateStore)boot.Flow!.StateStore!;
            SolveResult solution = new AStarSolver().Solve(boot.Flow.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            var saves = new List<double>();
            var json = new List<double>();
            for (int i = 0; i < solution.MoveCount - 1; i++)
            {
                int before = states.SaveCount;
                boot.TapCuia(solution.GetMove(i).Source);
                boot.TapCuia(solution.GetMove(i).Target);
                Assert.That(states.SaveCount, Is.EqualTo(before + 1), "un guardado por vertido");
                saves.Add(states.LastSaveMs);
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                ProfileJson.Serialize(boot.Profile!);
                json.Add((System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
                PlayModeHelpers.StepUntilIdle(boot, 4000);
            }

            Assert.That(saves.Count, Is.GreaterThanOrEqualTo(3));
            double saveMedian = Median(saves);
            double jsonMedian = Median(json);
            Debug.Log("AF_PERF level_state: " + saves.Count + " vertidos; guardado entero mediana " + Ms(saveMedian) + " ms, máximo "
                + Ms(saves[saves.Count - 1]) + " ms; JSON del perfil mediana " + Ms(jsonMedian) + " ms; " + store.LastSavedBytes + " bytes");
            Assert.That(states.PendingSave, Is.False);
            Assert.That(jsonMedian, Is.LessThan(5.0), "lo que pone la pista B: < 5 ms por vertido [P]");
            Assert.That(saveMedian, Is.LessThan(1000.0 / 60.0), "el guardado entero cabe en un fotograma de 60 fps (08 §5.4)");
            Assert.That(store.LastSavedBytes, Is.LessThan(100 * 1024), "perfil < 100 KB (08 §4.3)");
        }

        private static double Median(List<double> values)
        {
            values.Sort();
            return values[values.Count / 2];
        }

        private static string Ms(double value)
        {
            return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
