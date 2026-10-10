#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.Boosters;
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
    /// es una salida gratis del embudo); y cada vertido y la victoria escriben el perfil una sola vez, con su coste en el log
    /// (el objetivo [P] de 5 ms por vertido está pendiente del dueño).
    /// </summary>
    public sealed class ResumeTests
    {
        // Plan B2-5 [P]: «guardar cuesta < 5 ms por vertido en el Editor»; pendiente de que el dueño lo confirme o lo ajuste.
        private const double PourSaveTargetMs = 5.0;

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
        public IEnumerator WithoutSeeds_TheReopenedModal_KeepsTheBalsa_WithoutCountingItAgain()
        {
            // 02 §14 y D-008: el modal se reabre tal cual. Sin saldo (rama B) enseña Puñado y Balsa; la impresión de la Balsa
            // cuenta para af_rescue_daily_cap (= 1) y queda anotada en level_state (marca b), así que al volver a la app la
            // Balsa sigue ahí y no cuenta otra impresión. Antes, el modal reabierto la perdía: CanOfferBalsa ya daba false.
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.View!.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Disabled), "saldo insuficiente");
            Assert.That(boot.View.Stuck.HandfulButton.gameObject.activeSelf, Is.True);
            Assert.That(boot.View.Stuck.BalsaButton.gameObject.activeSelf, Is.True);
            Assert.That(boot.Services!.StoreLedger.BalsaShownToday, Is.EqualTo(1), "la impresión cuenta para el tope diario");
            Assert.That(boot.Services.StoreLedger.CanOfferBalsa(31), Is.False, "af_rescue_daily_cap = 1: ya no se ofrecería en otro bloqueo");
            StringAssert.StartsWith("h2|S||b|", ProfileJson.Deserialize(store.Json!).LevelState!.History, "la Balsa enseñada queda en level_state");

            yield return PlayModeHelpers.LoadApp(store); // como cerrar y abrir la app
            GameBootstrap again = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(again);
            PlayModeHelpers.Step(again, 300);
            Assert.That(again.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(again.View!.StuckVisible, Is.True);
            Assert.That(again.View.Stuck.View!.Rescue, Is.EqualTo(StuckRescueKind.Disabled));
            Assert.That(again.View.Stuck.RescueNoteText, Is.EqualTo(UiText.MissingSeeds(300)), "el mismo precio (300, primero del día)");
            Assert.That(again.View.Stuck.HandfulButton.gameObject.activeSelf, Is.True);
            Assert.That(again.View.Stuck.BalsaButton.gameObject.activeSelf, Is.True, "el modal reabierto trae la Balsa, tal cual");
            Assert.That(again.Services!.StoreLedger.BalsaShownToday, Is.EqualTo(1), "sin contar otra impresión");
            Assert.That(again.Flow.BalsaShownThisBlock, Is.True);
            Assert.That(again.Wallet!.Canoes.Count, Is.EqualTo(5));

            // Al salir del bloqueo (Marea) la marca se olvida: el tope diario vuelve a mandar en el siguiente bloqueo.
            Assert.That(again.PressUndo(), Is.True);
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(again.Flow.BalsaShownThisBlock, Is.False);
            StringAssert.StartsWith("h2|P|||", ProfileJson.Deserialize(store.Json!).LevelState!.History);
        }

        [UnityTest]
        public IEnumerator Resume_MurkyLevel_KeepsRevealedLayers()
        {
            // Tarea T06 (02 R8, §11 T6 y §14): nivel 100 del catálogo real de la escena, estreno de Aguas turbias (cuia 0 =
            // [10*, 8*, 6]). 0 → 1 revela la capa 1 (R7) y Marea lo deshace sin volver a ocultarla (I5). Al cerrar y abrir la
            // app, el nivel se reabre con esa capa revelada (sección R de history h2) y la vista la enseña; antes se volvía a
            // ocultar, porque ningún vertido del historial la revela al rehacerlo.
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Level.IsMurky, Is.True);
            Assert.That(flow.Session.Current[0].HiddenMask, Is.EqualTo(0b11));
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(flow.Session.RevealedLayers(0), Is.EqualTo(0b10), "R7: la capa 1 queda arriba y se revela");
            Assert.That(boot.PressUndo(), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            BoardState board = flow.Session.Current;
            Assert.That(board[0].HiddenMask, Is.EqualTo(0b01), "I5: Marea no la vuelve a ocultar");
            Assert.That(boot.View!.Views[0].ShownCuia.HiddenMask, Is.EqualTo(0b01));
            LevelStateData saved = ProfileJson.Deserialize(store.Json!).LevelState!;
            Assert.That(saved.History, Is.EqualTo("h2|P||||002"), "R: capa 1 (0b10) de la cuia 0");
            Assert.That(saved.RevealedCuias, Is.EqualTo(new[] { 0 }), "revealed_cuias sigue igual, por compatibilidad");

            yield return PlayModeHelpers.LoadApp(store); // como cerrar y abrir la app
            GameBootstrap again = GameBootstrap.Current!;
            Assert.That(again, Is.Not.SameAs(boot));
            PlayModeHelpers.TakeClock(again);
            PlayModeHelpers.Step(again, 50);
            Assert.That(again.App!.Screen, Is.EqualTo(AppScreen.Level), "reabre el nivel");
            LevelFlow resumed = again.Flow!;
            Assert.That(resumed.LevelNumber, Is.EqualTo(100));
            Assert.That(resumed.Phase, Is.EqualTo(FlowPhase.Playing), "sin intro (02 §14)");
            Assert.That(resumed.Session.RevealedLayers(0), Is.EqualTo(0b10), "02 T6: lo revelado sigue revelado");
            Assert.That(resumed.Session.Current, Is.EqualTo(board), "el mismo tablero, con la capa 1 a la vista");
            Assert.That(again.View!.Views[0].ShownCuia.HiddenMask, Is.EqualTo(0b01), "la vista enseña la capa revelada y oculta la otra");
        }

        [UnityTest]
        public IEnumerator Saving_IsOneWritePerPour_AndOneForTheVictory_AndReportsItsCost()
        {
            // Revisión de B2-5: cada acción del jugador escribe el perfil una sola vez (ProfileSaveBatch): un vertido, una
            // escritura; la victoria (level_state, progreso, Cosecha y cadencia de intersticiales), una, no cuatro. Perfil real en
            // disco (LocalProfileStore de AF.Save: .tmp, Flush(true) y File.Replace con copia) en una carpeta temporal.
            // El criterio [P] del plan («< 5 ms por vertido en el Editor») no se cumple con una escritura síncrona con fsync en el
            // hilo principal: lo que pone la pista B (estado y JSON) es < 1 ms y el resto es el disco (AF.Save). Aquí se exige que
            // la escritura quepa en un fotograma de 60 fps (08 §5.4) y se informa de la cifra (AF_PERF) para que el dueño confirme
            // o ajuste el umbral; si pasa de 5 ms queda un aviso en el log, no un fallo.
            _dir = Path.Combine(Application.temporaryCachePath, "af_resume_" + Guid.NewGuid().ToString("N"));
            var store = new LocalProfileStore(_dir);
            store.Save(FunnelSetup.Profile(0));
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            ProfileSaveBatch batch = boot.SaveBatch!;
            Assert.That(batch.Inner, Is.SameAs(store));
            SolveResult solution = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            var saves = new List<double>();
            var json = new List<double>();
            int last = solution.MoveCount - 1;
            for (int i = 0; i <= last; i++)
            {
                int before = batch.WriteCount;
                boot.TapCuia(solution.GetMove(i).Source);
                boot.TapCuia(solution.GetMove(i).Target);
                Assert.That(batch.WriteCount, Is.EqualTo(before + 1), i == last ? "la victoria: una escritura (antes, cuatro)" : "vertido " + (i + 1) + ": una escritura");
                saves.Add(batch.LastWriteMs);
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                ProfileJson.Serialize(boot.Profile!);
                json.Add((System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
                if (i < last)
                {
                    PlayModeHelpers.StepUntilIdle(boot, 4000);
                }
            }

            ProfileLoadResult onDisk = new LocalProfileStore(_dir).Load();
            // (El progreso de este test va en un MemoryProgressStore; en el juego, ProfileProgressStore escribe por el mismo lote.)
            Assert.That(onDisk.Profile!.LevelState, Is.Null, "la victoria borró level_state…");
            Assert.That(onDisk.Profile.Wallet.Seeds, Is.EqualTo(boot.Flow.HarvestSeeds), "…cobró la Cosecha…");
            Assert.That(onDisk.Profile.Ads.LevelsSinceInterstitial, Is.EqualTo(1), "…y contó para los intersticiales, en la misma escritura");

            Assert.That(saves.Count, Is.GreaterThanOrEqualTo(3));
            double saveMedian = Median(saves);
            double saveMax = saves[saves.Count - 1];
            double jsonMedian = Median(json);
            Debug.Log("AF_PERF level_state: " + saves.Count + " acciones (vertidos y victoria), una escritura cada una; escritura mediana "
                + Ms(saveMedian) + " ms, máximo " + Ms(saveMax) + " ms; JSON del perfil mediana " + Ms(jsonMedian) + " ms; "
                + store.LastSavedBytes + " bytes");
            if (saveMedian >= PourSaveTargetMs)
            {
                Debug.LogWarning("AF_PERF level_state: la escritura por vertido (" + Ms(saveMedian) + " ms) supera el objetivo [P] de "
                    + Ms(PourSaveTargetMs) + " ms del plan B2-5; pendiente de que el dueño confirme o ajuste el umbral.");
            }

            Assert.That(batch.PendingSave, Is.False);
            Assert.That(jsonMedian, Is.LessThan(PourSaveTargetMs), "lo que pone la pista B por vertido (estado y JSON)");
            Assert.That(saveMedian, Is.LessThan(1000.0 / 60.0), "la escritura cabe en un fotograma de 60 fps (08 §5.4)");
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
