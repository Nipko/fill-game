#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using AF.Boosters;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Modo Reto del río en <see cref="LevelFlow"/> (tarea R8; <c>LevelFlow.Daily.cs</c>; nota <c>docs/dev/reto-del-rio.md</c> §9;
    /// 06 §4.1; 04 §7.1; 04 §2.6 e; 06 §4.3; R-Q4, R-Q8, R-Q9 y R-Q13): victoria, bloqueo y reinicio sin efectos de campaña
    /// (canoas, Crecida —aún no existe en el cliente—, <c>fails_level</c>, <c>max_level</c>, progreso, cartera), comodines con
    /// el stock y el nivel efectivo, reintentos ilimitados, reanudación con un almacén propio y el resultado entregado al llamador.
    /// </summary>
    public sealed class DailyLevelFlowTests
    {
        private static DailyPool? _pool;

        /// <summary>Pool de prueba (banda 1; el tablero del nivel 1 de 02 §13 en cada día).</summary>
        private static DailyPool Pool => _pool ??= DailyPoolReader.Read(DailyPoolTestPacks.Pool(DailyPoolTestPacks.Boards(1)), DailyPoolCoverage.PresentBands);

        private static LevelDefinition DailyBoard => Pool.Find(1, 0)!;

        /// <summary>Tablero 6 del tutorial: 3 → 4 deja Aguas estancadas (TutorialPackTests).</summary>
        private static LevelDefinition StuckBoard => TestPacks.Tutorial.Find(6)!;

        [Test]
        public void Win_DeliversTheResult_WithoutProgressHarvestCanoesOrCampaignLevelState()
        {
            var rig = new DailyRig(DailyBoard, maxLevel: 40);
            CampaignSnapshot before = rig.Snapshot();
            rig.Flow.StartDaily();
            Assert.That(rig.Flow.IsDaily, Is.True);
            Assert.That(rig.Flow.LevelNumber, Is.EqualTo(41), "nivel efectivo max_level + 1 [P]");
            Assert.That(rig.Flow.Level.Id, Is.EqualTo("d1_n0000"));
            Assert.That(rig.Flow.DailyBoardId, Is.EqualTo("d1_n0000"));
            Assert.That(rig.Flow.DailyBoard, Is.SameAs(DailyBoard));
            AssertSameBoard(rig.Flow.Level, DailyBoard);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "sin tarjeta previa ni banner de mundo, y sin Sin canoas");
            Assert.That(rig.Flow.CanoesLevel, Is.False, "el Reto no pide canoas (04 §7.1)");

            rig.Flow.TapAnywhere();
            rig.Solve();
            Assert.That(rig.Flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(rig.Results.Count, Is.EqualTo(1));
            Assert.That(rig.Results[0].Outcome, Is.EqualTo(DailyPlayOutcome.Win));
            Assert.That(rig.Results[0].BoardId, Is.EqualTo("d1_n0000"));
            Assert.That(rig.Results[0].Attempt, Is.EqualTo(1));
            Assert.That(rig.Results[0].Pours, Is.EqualTo(rig.Flow.Session.AppliedPours));
            Assert.That(rig.Listener.Saw("win"), Is.True, "la vista también recibe la victoria");

            Assert.That(rig.Flow.HarvestSeeds, Is.EqualTo(0), "sin Cosecha de campaña: el premio de 60 lo cobra quien llama");
            rig.Flow.Tick(16, true);
            rig.Flow.Tick(GameConfig.AnimCelebrationMs, true);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(rig.Flow.DoubleHarvestOffered, Is.False, "la Cosecha doble del Reto necesita E9");
            Assert.That(rig.Flow.Next(), Is.False, "no hay siguiente nivel");
            Assert.That(rig.Flow.ExitDaily(), Is.False, "tras la victoria, salir no es otro resultado");
            Assert.That(rig.Results.Count, Is.EqualTo(1));
            Assert.That(rig.DailyStore.Load(), Is.Null, "la victoria borra la partida guardada del Reto");
            rig.AssertCampaignUntouched(before);
        }

        [Test]
        public void Stuck_OffersOnlyMareaAndRestart_NoRescueNoCanoeNoGiveUp()
        {
            var rig = new DailyRig(StuckBoard, maxLevel: 40);
            CampaignSnapshot before = rig.Snapshot();
            rig.StartPlaying();
            Assert.That(rig.Flow.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(rig.Flow.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(rig.Flow.Session.Status, Is.EqualTo(LevelStatus.Stuck));
            rig.Flow.Tick(16, true);
            rig.Flow.Tick(GameConfig.StuckModalDelayMs, true);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));

            Assert.That(rig.Flow.RescueOffered, Is.False, "sin Cuia de rescate (06 §4.1)");
            Assert.That(rig.Flow.FrozenRescueQuote, Is.Null);
            Assert.That(rig.Flow.BuyRescue(), Is.Null);
            Assert.That(rig.Flow.ApplyPaidRescue(), Is.False, "tampoco la de la Balsa");
            Assert.That(rig.Flow.CanoeAtStake, Is.False);
            Assert.That(rig.Flow.PressGiveUp(), Is.False, "sin embudo de D-008");
            Assert.That(rig.Flow.QuitCostsCanoe, Is.False);
            Assert.That(rig.Flow.ConfirmQuit(), Is.False, "salir no es fallo duro");

            StuckModalView view = StuckModalModel.Build(rig.Flow.BuildStuckContext(true, true, GeoTier.T1), rig.Flow.Boosters);
            Assert.That(view.Rescue, Is.EqualTo(StuckRescueKind.None));
            Assert.That(view.Offers.Count, Is.EqualTo(0), "ni Balsa ni Puñado");
            Assert.That(view.CanoeLine, Is.EqualTo(StuckCanoeLine.None));
            Assert.That(view.Footer, Is.EqualTo(StuckFooter.RestartFree), "el pie es Volver a la orilla; no hay Salto del Boto");
            Assert.That(view.Undo, Is.EqualTo(StuckUndoKind.Undo));
            Assert.That(view.UndoUnlimited, Is.False);

            Assert.That(rig.Flow.CanRestart, Is.True);
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "mismo tablero, intro corta; nunca Sin canoas");
            Assert.That(rig.Results.Count, Is.EqualTo(1));
            Assert.That(rig.Results[0].Outcome, Is.EqualTo(DailyPlayOutcome.Restart));
            Assert.That(rig.Results[0].FromStuckModal, Is.True);
            Assert.That(rig.Results[0].Pours, Is.EqualTo(1));
            Assert.That(rig.Flow.DailyAttempt, Is.EqualTo(2));
            Assert.That(rig.Flow.Session.AppliedPours, Is.EqualTo(0));
            AssertSameBoard(rig.Flow.Level, StuckBoard);
            rig.AssertCampaignUntouched(before);
        }

        [Test]
        public void Restart_IsUnlimited_WithTheSameBoard_EvenWithZeroCanoes()
        {
            var rig = new DailyRig(DailyBoard, maxLevel: 40);
            rig.SpendCanoesTo(0);
            CampaignSnapshot before = rig.Snapshot();
            rig.Flow.StartDaily();
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "con 0 canoas el Reto se juega (04 §7.1)");
            const int restarts = 25;
            for (int i = 0; i < restarts; i++)
            {
                rig.Flow.Tick(rig.Flow.IntroDurationMs, true);
                Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "intento " + (i + 1));
                Assert.That(rig.Flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(rig.Flow.TapCuia(2).Kind, Is.EqualTo(TapKind.Poured));
                Assert.That(rig.Flow.PressRestart(), Is.True, "intento " + (i + 1));
                Assert.That(rig.Flow.Phase, Is.Not.EqualTo(FlowPhase.NoCanoes));
                Assert.That(rig.Flow.Session.AppliedPours, Is.EqualTo(0));
                AssertSameBoard(rig.Flow.Level, DailyBoard);
            }

            Assert.That(rig.Results.Count, Is.EqualTo(restarts));
            for (int i = 0; i < restarts; i++)
            {
                Assert.That(rig.Results[i].Outcome, Is.EqualTo(DailyPlayOutcome.Restart));
                Assert.That(rig.Results[i].Attempt, Is.EqualTo(i + 1));
                Assert.That(rig.Results[i].FromStuckModal, Is.False);
                Assert.That(rig.Results[i].Pours, Is.EqualTo(1));
            }

            Assert.That(rig.Flow.DailyAttempt, Is.EqualTo(restarts + 1));
            Assert.That(rig.Flow.UndoStatus.FreeLeft, Is.EqualTo(BoosterConfig.Defaults.UndoFreePerLevel), "cada intento, las Mareas gratis enteras");
            rig.AssertCampaignUntouched(before);
        }

        [Test]
        public void Exit_DeliversFail_WithoutCanoe_AndClearsTheDailySave()
        {
            var rig = new DailyRig(DailyBoard, maxLevel: 40);
            CampaignSnapshot before = rig.Snapshot();
            rig.StartPlaying();
            rig.Flow.TapCuia(0);
            rig.Flow.TapCuia(2);
            Assert.That(rig.DailyStore.Load(), Is.Not.Null, "el Reto guarda la partida en su almacén (R-Q4)");
            Assert.That(rig.Flow.ExitDaily(), Is.True);
            Assert.That(rig.Results.Count, Is.EqualTo(1));
            Assert.That(rig.Results[0].Outcome, Is.EqualTo(DailyPlayOutcome.Exit), "result = fail (R-Q9)");
            Assert.That(rig.Results[0].Pours, Is.EqualTo(1));
            Assert.That(rig.DailyStore.Load(), Is.Null, "salir descarta la partida del Reto [P]");
            Assert.That(rig.Flow.ExitDaily(), Is.False, "una sola salida");
            Assert.That(rig.Results.Count, Is.EqualTo(1));
            rig.AssertCampaignUntouched(before);
        }

        [Test]
        public void Boosters_SpendTheSameStock_AndUnlockWithTheEffectiveLevel()
        {
            var rig = new DailyRig(DailyBoard, maxLevel: 12);
            rig.Grant(BoosterKind.Undo, 2);
            rig.Grant(BoosterKind.ExtraCuia, 1);
            CampaignSnapshot before = rig.Snapshot();
            rig.StartPlaying();
            Assert.That(rig.Flow.LevelNumber, Is.EqualTo(13));

            // Marea: las 3 gratis del tablero y luego el stock, nunca ilimitada (04 §2.1 [P]).
            UndoStatus undo = rig.Flow.UndoStatus;
            Assert.That(undo.Unlimited, Is.False);
            Assert.That(undo.FreeLeft, Is.EqualTo(3));
            Assert.That(undo.Stock, Is.EqualTo(2));
            for (int i = 0; i < 4; i++)
            {
                rig.Flow.TapCuia(0);
                Assert.That(rig.Flow.TapCuia(2).Kind, Is.EqualTo(TapKind.Poured));
                Assert.That(rig.Flow.Undo(), Is.True, "Marea " + (i + 1));
            }

            Assert.That(rig.Profile.Stock.Undo, Is.EqualTo(1), "la cuarta Marea sale del stock (R-Q8)");
            Assert.That(rig.CampaignWallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(1), "el mismo stock que la campaña");

            // Cuia extra: con el stock, como en la campaña.
            Assert.That(rig.Flow.ExtraCuia.State, Is.EqualTo(ExtraCuiaState.Ready));
            TransactionResult? extra = rig.Flow.UseExtraCuia();
            Assert.That(extra != null && extra.IsApplied, Is.True);
            Assert.That(rig.Profile.Stock.ExtraCuia, Is.EqualTo(0));
            Assert.That(rig.Flow.Session.Current.Count, Is.EqualTo(DailyBoard.CuiaCount + 1));

            // Morpho desde el 12: con el nivel efectivo 13 la ranura existe.
            Assert.That(rig.Flow.Hint.State, Is.Not.EqualTo(HintState.Hidden));
            Assert.That(rig.Profile.Wallet.Seeds, Is.EqualTo(before.Seeds), "con stock no se gastan semillas");
            Assert.That(rig.Profile.Progress.MaxLevel, Is.EqualTo(before.MaxLevel));
            Assert.That(rig.Profile.LevelState, Is.SameAs(before.LevelState));
        }

        [TestCase(6, false, false)]
        [TestCase(7, true, false)]
        [TestCase(10, true, false)]
        [TestCase(11, true, true)]
        public void Unlocks_UseMaxLevelPlusOne_AndTutorialGiftsNeverAppear(int maxLevel, bool extraCuia, bool morpho)
        {
            var rig = new DailyRig(DailyBoard, maxLevel);
            rig.StartPlaying();
            Assert.That(rig.Flow.LevelNumber, Is.EqualTo(maxLevel + 1));
            Assert.That(rig.Flow.ExtraCuia.State != ExtraCuiaState.Hidden, Is.EqualTo(extraCuia), "Cuia extra desde el 8");
            Assert.That(rig.Flow.Hint.State != HintState.Hidden, Is.EqualTo(morpho), "Morpho desde el 12");
            Assert.That(rig.Flow.UndoStatus.Unlimited, Is.False, "Marea no ilimitada en el Reto [P]");
            Assert.That(rig.Profile.Stock.ExtraCuia, Is.EqualTo(0), "sin el regalo de Capi del 8");
            Assert.That(rig.Profile.Stock.Hint, Is.EqualTo(0), "sin los 3 Morphos del 12");
            Assert.That(rig.Listener.Saw("gift:1"), Is.False);
        }

        [Test]
        public void Resume_WithItsOwnStore_SameBoardAndFrozenEffectiveLevel()
        {
            var rig = new DailyRig(DailyBoard, maxLevel: 12);
            CampaignSnapshot before = rig.Snapshot();
            rig.StartPlaying();
            rig.Flow.TapCuia(0);
            rig.Flow.TapCuia(2);
            LevelStateData? saved = rig.DailyStore.Load();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.Level, Is.EqualTo(13));
            Assert.That(rig.Profile.LevelState, Is.SameAs(before.LevelState), "el level_state de campaña no se toca");

            // Al volver a abrir la app el mismo día, la campaña ya avanzó: el nivel efectivo sigue siendo el de la partida.
            rig.Profile.Progress.MaxLevel = 20;
            int effective = LevelFlow.DailyEffectiveLevel(rig.Profile.Progress.MaxLevel, saved, DailyBoard);
            Assert.That(effective, Is.EqualTo(13));
            Assert.That(LevelFlow.DailyEffectiveLevel(20, saved, Pool.Find(1, 1)!), Is.EqualTo(21), "otro tablero: no es esa partida");

            var listener = new RecordingListener();
            LevelFlow reopened = LevelFlow.CreateDaily(DailyBoard, effective, rig.Progress, listener, rig.DailyWallet);
            reopened.StateStore = rig.DailyStore;
            Assert.That(reopened.ResumeDaily(saved), Is.True);
            Assert.That(reopened.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(reopened.Session.AppliedPours, Is.EqualTo(1));
            Assert.That(reopened.DailyAttempt, Is.EqualTo(1));

            LevelFlow other = LevelFlow.CreateDaily(Pool.Find(1, 1)!, 13, rig.Progress, new RecordingListener(), rig.DailyWallet);
            Assert.That(other.ResumeDaily(saved), Is.False, "la partida de otro tablero no se reabre");
            LevelFlow later = LevelFlow.CreateDaily(DailyBoard, 21, rig.Progress, new RecordingListener(), rig.DailyWallet);
            Assert.That(later.ResumeDaily(saved), Is.False, "con otro nivel efectivo tampoco");
        }

        [Test]
        public void CreateDaily_RejectsTheCampaignWallet_AndCampaignFlowsHaveNoDailyMode()
        {
            var rig = new DailyRig(DailyBoard, maxLevel: 12);
            Assert.That(
                () => LevelFlow.CreateDaily(DailyBoard, 13, rig.Progress, new RecordingListener(), rig.CampaignWallet),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => LevelFlow.CreateDaily(DailyBoard, 0, rig.Progress, new RecordingListener(), rig.DailyWallet),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => LevelFlow.CreateDaily(DailyBoard, 41, rig.Progress, new RecordingListener(), null!),
                Throws.TypeOf<ArgumentNullException>(),
                "sin cartera del Reto, rescue_min_level volvería al 31 y con él el embudo de D-008");

            var campaign = new LevelFlow(TestPacks.Tutorial, new MemoryProgressStore(), new RecordingListener());
            Assert.That(campaign.IsDaily, Is.False);
            Assert.That(campaign.DailyBoardId, Is.Null);
            Assert.That(() => campaign.StartDaily(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => campaign.ExitDaily(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(LevelFlow.DailyEffectiveLevel(-3), Is.EqualTo(1));
        }

        [Test]
        public void StartAndResume_RejectAnAdGate_AndTheCampaignLevelState()
        {
            var rig = new DailyRig(DailyBoard, maxLevel: 12);
            rig.Flow.AdGate = new NoAdGate();
            Assert.That(() => rig.Flow.StartDaily(), Throws.TypeOf<InvalidOperationException>(), "sin intersticial (R-Q13 [P])");
            Assert.That(() => rig.Flow.ResumeDaily(new LevelStateData()), Throws.TypeOf<InvalidOperationException>());
            rig.Flow.AdGate = null;
            rig.Flow.StateStore = new ProfileLevelStateStore(rig.Profile, rig.Store);
            Assert.That(() => rig.Flow.StartDaily(), Throws.TypeOf<InvalidOperationException>(), "nunca el level_state de campaña (R-Q4)");
            Assert.That(rig.Flow.HasLevel, Is.False);
            rig.Flow.StateStore = rig.DailyStore;
            rig.Flow.StartDaily();
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [Test]
        public void RestartFromTheModal_ResolvesAHardFailOfTheDailyAttemptOnly()
        {
            // Hoy el pie del modal pasa por Fail(Hard) → FailResolve y la vista recibe OnFailResolved (lo anota en su sonda); no
            // hay canoa, ni stats.fails_hard, ni fails_level de campaña. Cuando exista la Crecida hará falta la guarda del
            // informe de R8 (06 §4.3: el Reto no la toca).
            var rig = new DailyRig(StuckBoard, maxLevel: 40);
            CampaignSnapshot before = rig.Snapshot();
            rig.StartPlaying();
            rig.GetStuck();
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Listener.Saw("fail:Hard:1"), Is.True, "OnFailResolved(Hard) del intento del Reto, sin canoa");
            Assert.That(rig.Listener.Events.Exists(e => e.EndsWith(":canoe", StringComparison.Ordinal)), Is.False);
            Assert.That(rig.Flow.Attempt.FailsLevel, Is.EqualTo(1), "el fails_level es el del intento del Reto");
            Assert.That(rig.Profile.Stats.FailsHard, Is.EqualTo(before.FailsHard), "stats.fails_hard");
            Assert.That(rig.DailyStore.Load()!.FailsLevel, Is.EqualTo(1), "se guarda en el almacén del Reto");
            rig.AssertCampaignUntouched(before);
        }

        [Test]
        public void RestartFromTheHeaderAfterManyPours_AsksConfirm_AndIsASoftFailWithoutEffects()
        {
            var rig = new DailyRig(LongBoard, maxLevel: 40);
            CampaignSnapshot before = rig.Snapshot();
            rig.StartPlaying();
            SolveResult solution = new AStarSolver().Solve(rig.Flow.Session.Current, SolverOptions.ForPipeline(rig.Flow.Session.PourMode));
            int pours = BoosterConfig.Defaults.RestartConfirmMinPours;
            for (int i = 0; i < pours; i++)
            {
                rig.Flow.TapCuia(solution.GetMove(i).Source);
                Assert.That(rig.Flow.TapCuia(solution.GetMove(i).Target).Kind, Is.EqualTo(TapKind.Poured), "vertido " + (i + 1));
            }

            Assert.That(rig.Flow.Session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(rig.Flow.RestartNeedsConfirm, Is.True, "≥ restart_confirm_min_pours: la vista confirma (04 §2.7)");
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Listener.Saw("fail:Soft:1"), Is.True);
            Assert.That(rig.Results.Count, Is.EqualTo(1));
            Assert.That(rig.Results[0].Outcome, Is.EqualTo(DailyPlayOutcome.Restart));
            Assert.That(rig.Results[0].Pours, Is.EqualTo(pours));
            Assert.That(rig.Results[0].FromStuckModal, Is.False);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            rig.AssertCampaignUntouched(before);
        }

        [Test]
        public void ExitFromTheStuckModal_DeliversFail_WithoutCanoe()
        {
            var rig = new DailyRig(StuckBoard, maxLevel: 40);
            CampaignSnapshot before = rig.Snapshot();
            rig.StartPlaying();
            rig.GetStuck();
            Assert.That(rig.Flow.CanQuit, Is.True);
            Assert.That(rig.Flow.ExitDaily(), Is.True);
            Assert.That(rig.Results.Count, Is.EqualTo(1));
            Assert.That(rig.Results[0].Outcome, Is.EqualTo(DailyPlayOutcome.Exit));
            Assert.That(rig.Results[0].Pours, Is.EqualTo(1));
            Assert.That(rig.Listener.Events.Exists(e => e.StartsWith("fail:", StringComparison.Ordinal)), Is.False, "salir no es fallo");
            Assert.That(rig.DailyStore.Load(), Is.Null);
            rig.AssertCampaignUntouched(before);
        }

        [Test]
        public void Morpho_SpendsTheStock_WhenItGivesAPour()
        {
            var rig = new DailyRig(DailyBoard, maxLevel: 12);
            rig.Grant(BoosterKind.Hint, 1);
            CampaignSnapshot before = rig.Snapshot();
            rig.StartPlaying();
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Ready));
            Assert.That(rig.Flow.PressHint(), Is.True);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (rig.Flow.HintThinking)
            {
                Assert.That(watch.ElapsedMilliseconds, Is.LessThan(10000), "Morpho no respondió");
                System.Threading.Thread.Sleep(1);
                rig.Flow.Tick(16, true);
            }

            HintResult? hint = rig.Flow.LastHint;
            Assert.That(hint.HasValue, Is.True);
            Assert.That(hint!.Value.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(hint.Value.Charged, Is.True);
            Assert.That(rig.Profile.Stock.Hint, Is.EqualTo(0), "el Morpho sale del stock (R-Q8)");
            Assert.That(rig.Profile.Wallet.Seeds, Is.EqualTo(before.Seeds), "con stock no se cobran semillas");
        }

        private static LevelDefinition? _longBoard;

        /// <summary>
        /// Primer nivel de la campaña sin mecánicas, ocultas, alivio ni estreno cuya solución pasa de
        /// <c>restart_confirm_min_pours</c> vertidos: sirve de tablero del Reto para un reinicio con confirmación.
        /// </summary>
        private static LevelDefinition LongBoard
        {
            get
            {
                if (_longBoard != null)
                {
                    return _longBoard;
                }

                int needed = BoosterConfig.Defaults.RestartConfirmMinPours + 1;
                IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
                for (int p = 0; p < packs.Count; p++)
                {
                    for (int i = 0; i < packs[p].Count; i++)
                    {
                        LevelDefinition def = packs[p].Levels[i];
                        if (def.HiddenCount > 0 || def.Relief.Count > 0 || def.IntroMechanic != null || HasAttrs(def))
                        {
                            continue;
                        }

                        SolveResult result = new AStarSolver().Solve(def.CreateBoard(), SolverOptions.ForPipeline(PourModes.Default));
                        if (result.MoveCount >= needed)
                        {
                            _longBoard = def;
                            return def;
                        }
                    }
                }

                Assert.Fail("No hay un tablero de campaña largo y sin mecánicas para la prueba.");
                return null!;
            }
        }

        private static bool HasAttrs(LevelDefinition def)
        {
            for (int i = 0; i < def.Attrs.Count; i++)
            {
                if (!def.Attrs[i].IsNone)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class NoAdGate : IAdGate
        {
            public void RecordWin(int level)
            {
            }

            public void Run(int wonLevel, Action onDone)
            {
                onDone();
            }
        }

        [Test]
        public void DailyEconomy_CopiesEveryKey_ExceptHarvestAndRescueMinLevel()
        {
            var campaign = new EconomyConfig();
            Perturb(campaign);
            campaign.Validate();
            EconomyConfig daily = LevelFlow.DailyEconomy(campaign);
            var changed = new Dictionary<string, object>
            {
                { "eco_reward_normal", 0 },
                { "eco_reward_hard", 0 },
                { "eco_reward_very_hard", 0 },
                { "rescue_min_level", int.MaxValue },
            };
            AssertCopied(campaign, daily, changed);
            Assert.That(LevelFlow.IsDailyEconomy(daily), Is.True);
            Assert.That(LevelFlow.IsDailyEconomy(campaign), Is.False);
        }

        [Test]
        public void DailyBoosters_CopiesEveryKey_ExceptUnlimitedUndoAndRescue()
        {
            var campaign = new BoosterConfig();
            Perturb(campaign);
            campaign.Validate();
            BoosterConfig daily = LevelFlow.DailyBoosters(campaign);
            var changed = new Dictionary<string, object>
            {
                { "undo_unlimited_until_level", 0 },
                { "rescue_max_per_level", 0 },
            };
            AssertCopied(campaign, daily, changed);
        }

        private static void AssertSameBoard(LevelDefinition actual, LevelDefinition expected)
        {
            Assert.That(actual.CuiaCount, Is.EqualTo(expected.CuiaCount));
            for (int i = 0; i < expected.CuiaCount; i++)
            {
                Assert.That(actual.CapacityOf(i), Is.EqualTo(expected.CapacityOf(i)), "cuia " + i);
                Assert.That(actual.CopyDoses(i), Is.EqualTo(expected.CopyDoses(i)), "cuia " + i);
            }
        }

        // Cambia cada clave de Remote Config a otro valor válido, para que una copia olvidada se note.
        private static void Perturb(object config)
        {
            foreach (PropertyInfo p in Keys(config))
            {
                object? value = p.GetValue(config);
                if (p.PropertyType == typeof(int))
                {
                    p.SetValue(config, (int)value! + 1);
                }
                else if (p.PropertyType == typeof(bool))
                {
                    p.SetValue(config, !(bool)value!);
                }
                else if (p.PropertyType == typeof(double))
                {
                    p.SetValue(config, p.Name == "HintWeight" ? 3.0 : 0.25);
                }
                else if (p.PropertyType == typeof(string))
                {
                    p.SetValue(config, (string)value! + ",x");
                }
                else if (p.PropertyType == typeof(IReadOnlyList<PriceBand>))
                {
                    p.SetValue(config, new[] { new PriceBand(32, 700) });
                }
                else
                {
                    Assert.Fail("Tipo sin perturbar en el test: " + p.PropertyType + " (" + p.Name + ")");
                }
            }
        }

        private static void AssertCopied(object campaign, object daily, Dictionary<string, object> changed)
        {
            int seen = 0;
            foreach (PropertyInfo p in Keys(campaign))
            {
                string key = p.GetCustomAttribute<RemoteConfigKeyAttribute>()!.Key;
                object? expected = changed.TryGetValue(key, out object? forced) ? forced : p.GetValue(campaign);
                Assert.That(p.GetValue(daily), Is.EqualTo(expected), key);
                if (changed.ContainsKey(key))
                {
                    seen++;
                }
            }

            Assert.That(seen, Is.EqualTo(changed.Count), "las claves cambiadas existen");
        }

        private static IEnumerable<PropertyInfo> Keys(object config)
        {
            PropertyInfo[] all = config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].GetCustomAttribute<RemoteConfigKeyAttribute>() != null && all[i].CanWrite)
                {
                    yield return all[i];
                }
            }
        }

        /// <summary>Lo que el Reto no puede cambiar de la campaña.</summary>
        private sealed class CampaignSnapshot
        {
            public int MaxLevel;
            public int NextLevel;
            public int MapSeenLevel;
            public int LevelsWon;
            public int FailsHard;
            public int Rescues;
            public int Seeds;
            public int Canoes;
            public DateTime? NextRegenAt;
            public int LevelsSinceInterstitial;
            public int RescuesToday;
            public int SinceLastSkip;
            public int Skipped;
            public LevelStateData? LevelState;
            public int CampaignFailsLevel;
            public string CampaignHistory = "";
        }

        /// <summary>
        /// Perfil con la campaña en <c>max_level</c>, su cartera, su progreso y un <c>level_state</c> de campaña (nivel 35 con
        /// <c>fails_level</c> 2) que el Reto no debe tocar; el flujo del Reto con su cartera y su almacén propio.
        /// </summary>
        private sealed class DailyRig
        {
            public DailyRig(LevelDefinition board, int maxLevel)
            {
                Clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
                Profile = Wallet.NewProfile(Clock);
                Profile.Wallet.Seeds = 500;
                Profile.Progress.MaxLevel = maxLevel;
                Profile.Progress.NextLevel = maxLevel + 1;
                Profile.Stats.LevelsWon = maxLevel;
                Profile.LevelState = new LevelStateData { Level = 35, Seed = "amazonfill:v1:m04:v1:n0035", FailsLevel = 2, History = "x" };
                Store = new MemoryProfileStore();
                CampaignWallet = new Wallet(Profile, Store, Clock);
                Progress = new ProfileProgressStore(Profile, Store);
                DailyWallet = LevelFlow.CreateDailyWallet(Profile, Store, Clock);
                Listener = new RecordingListener();
                DailyStore = new MemoryLevelStateStore();
                Flow = LevelFlow.CreateDaily(board, LevelFlow.DailyEffectiveLevel(maxLevel), Progress, Listener, DailyWallet);
                Flow.StateStore = DailyStore;
                Flow.Clock = Clock;
                Flow.DailyResult += r => Results.Add(r);
            }

            public ManualClock Clock { get; }

            public Profile Profile { get; }

            public MemoryProfileStore Store { get; }

            public Wallet CampaignWallet { get; }

            public Wallet DailyWallet { get; }

            public ProfileProgressStore Progress { get; }

            public RecordingListener Listener { get; }

            public MemoryLevelStateStore DailyStore { get; }

            public LevelFlow Flow { get; }

            public List<DailyPlayResult> Results { get; } = new List<DailyPlayResult>();

            public void StartPlaying()
            {
                Flow.StartDaily();
                Flow.TapAnywhere();
                Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            }

            public void GetStuck()
            {
                Assert.That(Flow.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(Flow.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
                Assert.That(Flow.Session.Status, Is.EqualTo(LevelStatus.Stuck));
                Flow.Tick(16, true);
                Flow.Tick(GameConfig.StuckModalDelayMs, true);
                Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            }

            public void Solve()
            {
                SolveResult result = new AStarSolver().Solve(Flow.Session.Current, SolverOptions.ForPipeline(Flow.Session.PourMode));
                for (int i = 0; i < result.MoveCount; i++)
                {
                    Flow.TapCuia(result.GetMove(i).Source);
                    Flow.TapCuia(result.GetMove(i).Target);
                }
            }

            public void Grant(BoosterKind kind, int units)
            {
                Assert.That(CampaignWallet.Apply(Transaction.GrantStock(kind, units)).IsApplied, Is.True);
            }

            public void SpendCanoesTo(int canoes)
            {
                while (CampaignWallet.Canoes.Count > canoes)
                {
                    Assert.That(CampaignWallet.Apply(Transaction.HardFail(31)).IsApplied, Is.True);
                }
            }

            public CampaignSnapshot Snapshot()
            {
                return new CampaignSnapshot
                {
                    MaxLevel = Profile.Progress.MaxLevel,
                    NextLevel = Profile.Progress.NextLevel,
                    MapSeenLevel = Profile.Progress.MapSeenLevel,
                    LevelsWon = Profile.Stats.LevelsWon,
                    FailsHard = Profile.Stats.FailsHard,
                    Rescues = Profile.Stats.Rescues,
                    Seeds = Profile.Wallet.Seeds,
                    Canoes = CampaignWallet.Canoes.Count,
                    NextRegenAt = Profile.Wallet.Canoes.NextRegenAt,
                    LevelsSinceInterstitial = Profile.Ads.LevelsSinceInterstitial,
                    RescuesToday = Profile.Daily.RescuesToday,
                    SinceLastSkip = Profile.Progress.SinceLastSkip,
                    Skipped = Profile.Progress.SkippedLevels.Count,
                    LevelState = Profile.LevelState,
                    CampaignFailsLevel = Profile.LevelState?.FailsLevel ?? -1,
                    CampaignHistory = Profile.LevelState?.History ?? "",
                };
            }

            public void AssertCampaignUntouched(CampaignSnapshot before)
            {
                CampaignSnapshot now = Snapshot();
                Assert.That(now.MaxLevel, Is.EqualTo(before.MaxLevel), "max_level");
                Assert.That(now.NextLevel, Is.EqualTo(before.NextLevel), "next_level");
                Assert.That(now.MapSeenLevel, Is.EqualTo(before.MapSeenLevel), "map_seen_level");
                Assert.That(now.LevelsWon, Is.EqualTo(before.LevelsWon), "stats.levels_won");
                Assert.That(now.FailsHard, Is.EqualTo(before.FailsHard), "stats.fails_hard");
                Assert.That(now.Rescues, Is.EqualTo(before.Rescues), "stats.rescues");
                Assert.That(now.Seeds, Is.EqualTo(before.Seeds), "semillas: ni Cosecha ni cobros de campaña");
                Assert.That(now.Canoes, Is.EqualTo(before.Canoes), "canoas");
                Assert.That(now.NextRegenAt, Is.EqualTo(before.NextRegenAt), "regeneración de canoas");
                Assert.That(now.LevelsSinceInterstitial, Is.EqualTo(before.LevelsSinceInterstitial), "cadencia de intersticiales (R-Q13)");
                Assert.That(now.RescuesToday, Is.EqualTo(before.RescuesToday), "rescates de hoy");
                Assert.That(now.SinceLastSkip, Is.EqualTo(before.SinceLastSkip), "Salto del Boto");
                Assert.That(now.Skipped, Is.EqualTo(before.Skipped), "niveles saltados");
                Assert.That(now.LevelState, Is.SameAs(before.LevelState), "level_state de campaña");
                Assert.That(now.CampaignFailsLevel, Is.EqualTo(before.CampaignFailsLevel), "fails_level de campaña");
                Assert.That(now.CampaignHistory, Is.EqualTo(before.CampaignHistory), "historial de campaña");
            }
        }
    }
}
