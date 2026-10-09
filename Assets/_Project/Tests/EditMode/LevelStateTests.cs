#nullable enable
using System.Collections.Generic;
using System.Text;
using AF.Boosters;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Nivel en curso guardado tras cada vertido y reabierto al volver a la app (tarea B2-5; 02 R8 y §14; 04 §5.2 y §10.1;
    /// D-008 «Persistencia»): el formato de <c>level_state</c>, el mismo tablero y la misma pantalla al reabrir (tablero,
    /// Aguas estancadas, ¿Seguro? o Nivel fallido), sin cobrar canoa ni contar fallo por cerrar la app; la victoria lo borra;
    /// salir al inicio conserva <c>fails_level</c> y la variante, no las cuias añadidas; un estado de otro tablero se descarta.
    /// </summary>
    public sealed class LevelStateTests
    {
        [Test]
        public void History_RoundTrips_PhaseAddedCuiasGiftAndPours()
        {
            var rig = new Rig(offset: 10, seeds: 150);
            rig.Start(11);
            Assert.That(rig.Flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(rig.Flow.TapCuia(2).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(rig.Flow.UseExtraCuia()!.IsApplied, Is.True);

            var sb = new StringBuilder();
            LevelStateSnapshot.WriteHistory(sb, SavedPhase.GiveUpConfirm, rig.Flow.Attempt.Added, true, false, rig.Flow.Session);
            Assert.That(sb.ToString(), Is.EqualTo("h1|G|E|g|02"));
            Assert.That(LevelStateSnapshot.TryParseHistory(sb.ToString(), out SavedHistory parsed), Is.True);
            Assert.That(parsed.Phase, Is.EqualTo(SavedPhase.GiveUpConfirm));
            Assert.That(parsed.Added, Is.EqualTo(new[] { AddedCuiaKind.Extra }));
            Assert.That(parsed.UndoGiftUsed, Is.True);
            Assert.That(parsed.ExtraCuiaGifted, Is.False);
            Assert.That(parsed.Sources, Is.EqualTo(new[] { 0 }));
            Assert.That(parsed.Targets, Is.EqualTo(new[] { 2 }));

            // Marca «c»: Capi ya regaló la Cuia extra del nivel (04 §2.2), también en un estado de salida al inicio.
            LevelStateSnapshot.WriteHistory(sb, SavedPhase.Exited, new AddedCuiaKind[0], false, true, null);
            Assert.That(sb.ToString(), Is.EqualTo("h1|X||c|"));
            Assert.That(LevelStateSnapshot.TryParseHistory("h1|X||c|", out SavedHistory gifted), Is.True);
            Assert.That(gifted.ExtraCuiaGifted, Is.True);
            Assert.That(gifted.UndoGiftUsed, Is.False);
            Assert.That(LevelStateSnapshot.TryParseHistory("h1|P||cg|", out SavedHistory both), Is.True);
            Assert.That(both.ExtraCuiaGifted && both.UndoGiftUsed, Is.True, "las marcas en cualquier orden");

            Assert.That(LevelStateSnapshot.TryParseHistory("h1|X|||", out SavedHistory exited), Is.True);
            Assert.That(exited.Phase, Is.EqualTo(SavedPhase.Exited));
            Assert.That(LevelStateSnapshot.TryParseHistory("h1|P|RE||0f1e", out SavedHistory two), Is.True);
            Assert.That(two.Added, Is.EqualTo(new[] { AddedCuiaKind.Rescue, AddedCuiaKind.Extra }));
            Assert.That(two.Sources, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(two.Targets, Is.EqualTo(new[] { 15, 14 }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("h2|P|||")]
        [TestCase("h1|Q|||")]
        [TestCase("h1|P|Z||")]
        [TestCase("h1|P||x|")]
        [TestCase("h1|P||gg|")]
        [TestCase("h1|P||cc|")]
        [TestCase("h1|P|||0")]
        [TestCase("h1|P|||00")]
        [TestCase("h1|P|||0g")]
        [TestCase("h1|P||")]
        public void History_RejectsBrokenText(string? history)
        {
            Assert.That(LevelStateSnapshot.TryParseHistory(history, out _), Is.False);
        }

        [Test]
        public void Seed_IsTheCanonicalOneOfTheBoard_AndVariantIsBase()
        {
            LevelDefinition level1 = TestPacks.Tutorial.Levels[0];
            Assert.That(LevelStateSnapshot.SeedOf(level1), Is.EqualTo("amazonfill:v1:m01:v1:n0001"), "08 §4.1");
            Assert.That(LevelStateSnapshot.VariantOf(level1, -1), Is.EqualTo("base"));
            Assert.That(LevelStateSnapshot.ReliefIndexOf(level1, "base"), Is.EqualTo(-1));
            Assert.That(LevelStateSnapshot.ReliefIndexOf(level1, "plus_empty"), Is.EqualTo(-2), "el tutorial no tiene relief[]");
        }

        [Test]
        public void EveryPour_IsSaved_AndReopeningRebuildsTheSameBoard_WithoutIntro()
        {
            var rig = new Rig();
            rig.Start(7);
            Assert.That(rig.Store.Load()!.Pours, Is.EqualTo(0), "al cargar ya hay level_state");
            SolveResult solution = Solve(rig.Flow);
            for (int i = 0; i < 2; i++)
            {
                Pour move = solution.GetMove(i);
                rig.Flow.TapCuia(move.Source);
                rig.Flow.TapCuia(move.Target);
                LevelStateData saved = rig.Store.Load()!;
                Assert.That(saved.Pours, Is.EqualTo(i + 1), "02 R8: tras cada vertido");
                Assert.That(saved.Level, Is.EqualTo(7));
                Assert.That(saved.Seed, Is.EqualTo("amazonfill:v1:m01:v1:n0007"));
                Assert.That(saved.Stuck, Is.False);
            }

            Assert.That(rig.Flow.Undo(), Is.True);
            Assert.That(rig.Store.Load()!.Pours, Is.EqualTo(1), "Marea también se guarda");

            Rig again = rig.Reopen();
            Assert.That(again.Flow.CheckResume(rig.Store.Load()), Is.EqualTo(ResumeCheck.Resumable));
            Assert.That(again.Flow.Resume(rig.Store.Load()!), Is.True);
            Assert.That(again.Flow.LevelNumber, Is.EqualTo(7));
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "sin intro (02 §14)");
            Assert.That(again.Flow.AcceptsBoardInput, Is.True);
            Assert.That(again.Flow.Session.Current, Is.EqualTo(rig.Flow.Session.Current));
            Assert.That(again.Flow.Session.AppliedPours, Is.EqualTo(1));
            Assert.That(again.Flow.CanUndo, Is.True, "el historial de Marea vuelve");
            Assert.That(again.Listener.Events, Is.EqualTo(new[] { "loaded:7", "resumed:Playing" }));
        }

        [Test]
        public void TheVictory_ClearsTheSavedLevel()
        {
            var rig = new Rig();
            rig.Start(1);
            Win(rig.Flow);
            Assert.That(rig.Store.Load(), Is.Null);
            rig.Flow.Tick(16, true);
            Assert.That(rig.Store.Load(), Is.Null, "tras la victoria ya no hay nivel en curso");
        }

        [Test]
        public void ClosingTheAppWithTheModalOpen_ReopensIt_WithoutChargingACanoe()
        {
            var rig = new Rig(offset: 25, seeds: 1000);
            rig.Start(31);
            GetStuck(rig.Flow);
            LevelStateData saved = rig.Store.Load()!;
            Assert.That(saved.Stuck, Is.True);
            StringAssert.StartsWith("h1|S|", saved.History);

            Rig again = rig.Reopen();
            Assert.That(again.Flow.Resume(saved), Is.True);
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal), "el modal se reabre al instante");
            Assert.That(again.Listener.Saw("modal"), Is.True);
            Assert.That(again.Listener.Saw("intro:" + GameConfig.AnimTintDurationMsLate), Is.False);
            Assert.That(again.Flow.RescueOffered, Is.True, "con el rescate verificado otra vez (04 §6.4)");
            Assert.That(again.Economy.Wallet.Canoes.Count, Is.EqualTo(5), "cerrar la app no cuesta canoa");
            Assert.That(again.Flow.Attempt.FailsLevel, Is.EqualTo(0), "ni cuenta como fallo");
        }

        [Test]
        public void AreYouSure_AndLevelFailed_Reopen_AndTheCanoeIsChargedOnlyOnce()
        {
            var rig = new Rig(offset: 25, seeds: 1000);
            rig.Start(31);
            GetStuck(rig.Flow);
            Assert.That(rig.Flow.PressGiveUp(), Is.True);
            StringAssert.StartsWith("h1|G|", rig.Store.Load()!.History);

            Rig second = rig.Reopen();
            Assert.That(second.Flow.Resume(rig.Store.Load()!), Is.True);
            Assert.That(second.Flow.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            Assert.That(second.Flow.RescueOffered, Is.True, "¿Seguro? vuelve a ofrecer «Continuar: +1 cuia»");
            Assert.That(second.Economy.Wallet.Canoes.Count, Is.EqualTo(5));

            Assert.That(second.Flow.ConfirmGiveUp(), Is.True);
            Assert.That(second.Economy.Wallet.Canoes.Count, Is.EqualTo(4));
            Assert.That(second.Flow.Attempt.FailsLevel, Is.EqualTo(1));
            StringAssert.StartsWith("h1|F|", second.Store.Load()!.History);

            Rig third = second.Reopen();
            Assert.That(third.Flow.Resume(second.Store.Load()!), Is.True);
            Assert.That(third.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(third.Economy.Wallet.Canoes.Count, Is.EqualTo(4), "reabrir Nivel fallido no vuelve a cobrar");
            Assert.That(third.Flow.Attempt.FailsLevel, Is.EqualTo(1));
            Assert.That(third.Flow.PressRetry(), Is.True);
            Assert.That(third.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(third.Flow.Session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void AddedCuias_MareaQuotaAndHistory_Persist()
        {
            var rig = new Rig(offset: 10, seeds: 150);
            rig.Start(11);
            rig.Flow.TapCuia(0);
            rig.Flow.TapCuia(2);
            Assert.That(rig.Flow.Undo(), Is.True);
            Assert.That(rig.Flow.UndoStatus.FreeLeft, Is.EqualTo(2));
            Assert.That(rig.Flow.UseExtraCuia()!.IsApplied, Is.True);
            rig.Flow.TapCuia(0);
            rig.Flow.TapCuia(3);
            LevelStateData saved = rig.Store.Load()!;
            Assert.That(saved.ExtraCuias, Is.EqualTo(1));
            Assert.That(saved.UndoFree, Is.EqualTo(2));
            Assert.That(saved.Pours, Is.EqualTo(1));

            Rig again = rig.Reopen();
            Assert.That(again.Flow.Resume(saved), Is.True);
            Assert.That(again.Flow.Session.Current.Count, Is.EqualTo(4), "la Cuia extra sigue (04 §2.2)");
            Assert.That(again.Flow.GeneratedCuias, Is.EqualTo(3), "las filas las fijan las generadas (D-006)");
            Assert.That(again.Flow.Attempt.ExtraCuias, Is.EqualTo(1));
            Assert.That(again.Flow.ExtraCuia.State, Is.Not.EqualTo(ExtraCuiaState.Ready), "1 por nivel, también al reabrir");
            Assert.That(again.Flow.UndoStatus.FreeLeft, Is.EqualTo(2), "la Marea usada no se devuelve");
            Assert.That(again.Flow.Session.Current, Is.EqualTo(rig.Flow.Session.Current));
        }

        [Test]
        public void LeavingToTheLobby_RemembersFailsLevel_ButNotTheAddedCuias()
        {
            // Nivel 6 (1-30): Aguas estancadas → «Volver a la orilla (gratis)» es fallo duro sin canoa (rama C): fails_level = 1.
            var rig = new Rig();
            rig.Start(6);
            GetStuck(rig.Flow);
            Assert.That(rig.Flow.PressRestart(), Is.True);
            Assert.That(rig.Flow.Attempt.FailsLevel, Is.EqualTo(1));
            rig.Flow.LeaveLevel();
            LevelStateData saved = rig.Store.Load()!;
            Assert.That(saved.History, Is.EqualTo("h1|X|||"));
            Assert.That(saved.FailsLevel, Is.EqualTo(1));

            Rig again = rig.Reopen();
            Assert.That(again.Flow.CheckResume(saved), Is.EqualTo(ResumeCheck.Exited));
            Assert.That(again.Flow.Resume(saved), Is.False, "no se reabre el tablero");
            Assert.That(again.Flow.RestoreExited(saved), Is.True);
            again.Flow.Load(6);
            Assert.That(again.Flow.Attempt.FailsLevel, Is.EqualTo(1), "04 §2.7: los fallos cuentan hasta ganar");

            // Nivel 8 con una Cuia extra: al salir, la cuia añadida no se guarda (04 §2.2: desaparece al abandonar el nivel); el
            // regalo de Capi sí («c»), para no repetirlo al volver.
            var withCuia = new Rig(seeds: 150);
            withCuia.Start(8);
            Assert.That(withCuia.Flow.UseExtraCuia()!.IsApplied, Is.True);
            Assert.That(withCuia.Store.Load()!.ExtraCuias, Is.EqualTo(1));
            withCuia.Flow.LeaveLevel();
            Assert.That(withCuia.Store.Load()!.ExtraCuias, Is.EqualTo(0));
            Assert.That(withCuia.Store.Load()!.History, Is.EqualTo("h1|X||c|"));
        }

        [Test]
        public void CapisGift_IsGivenOnce_EvenLeavingFreeIn1To30_AndReopeningTheApp()
        {
            // Revisión de B2-1: salir en 1-30 es gratis y volver a entrar llamaba a Load, que regalaba otra vez la Cuia extra.
            var rig = new Rig();
            rig.Start(8);
            Assert.That(rig.Listener.Saw("gift:1"), Is.True, "04 §2.2: Capi regala 1 en el 8");
            Assert.That(rig.Flow.UseExtraCuia()!.IsApplied, Is.True, "se gasta la regalada");
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(0));

            // Misma sesión: salir al inicio y volver a jugar el 8.
            rig.Flow.LeaveLevel();
            rig.Listener.Events.Clear();
            rig.Start(8);
            Assert.That(rig.Listener.Saw("gift:1"), Is.False, "salir y volver no repite el regalo");
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(0));

            // Otra sesión: salir, cerrar la app y volver a jugar el 8 desde el inicio.
            rig.Flow.LeaveLevel();
            LevelStateData exited = rig.Store.Load()!;
            Rig again = rig.Reopen();
            var app = new AppFlow(again.Flow, again.Progress, again.Economy.Wallet, new NullApp());
            app.Start(false, exited);
            Assert.That(app.Screen, Is.EqualTo(AppScreen.Lobby));
            again.Flow.Load(8);
            Assert.That(again.Listener.Saw("gift:1"), Is.False, "el regalo queda en level_state");
            Assert.That(again.Economy.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(0));
            StringAssert.StartsWith("h1|P||c|", again.Store.Load()!.History);
        }

        [Test]
        public void AStateOfAnotherBoard_OrBroken_IsDiscarded()
        {
            var rig = new Rig(offset: 25);
            rig.Start(31);
            rig.Flow.TapCuia(3);
            rig.Flow.TapCuia(4);
            LevelStateData saved = rig.Store.Load()!;

            Rig otherOffset = new Rig(offset: 30, store: rig.Store, economy: rig.Economy);
            Assert.That(otherOffset.Flow.CheckResume(saved), Is.EqualTo(ResumeCheck.Invalid), "el 31 es otro tablero con otro desplazamiento");

            LevelStateData wrongSeed = LevelStateSnapshot.Copy(saved);
            wrongSeed.Seed = "amazonfill:v1:m01:v1:n0099";
            Assert.That(rig.Reopen().Flow.CheckResume(wrongSeed), Is.EqualTo(ResumeCheck.Invalid));

            LevelStateData missing = LevelStateSnapshot.Copy(saved);
            missing.Level = 99;
            Assert.That(rig.Reopen().Flow.CheckResume(missing), Is.EqualTo(ResumeCheck.Invalid));

            LevelStateData garbage = LevelStateSnapshot.Copy(saved);
            garbage.History = "h1|P|||e0"; // la cuia 14 no existe en el tablero 6
            Rig again = rig.Reopen();
            Assert.That(again.Flow.CheckResume(garbage), Is.EqualTo(ResumeCheck.Resumable), "el formato vale");
            Assert.That(again.Flow.Resume(garbage), Is.False, "un vertido que no se puede rehacer: quien llama carga el nivel de nuevo");
        }

        [Test]
        public void AppFlow_StartsOnTheSavedLevel_OrTheLobbyAfterLeaving_AndDiscardsAnInvalidState()
        {
            var rig = new Rig(offset: 25, seeds: 1000);
            rig.Start(31);
            GetStuck(rig.Flow);
            LevelStateData saved = rig.Store.Load()!;

            Rig resumed = rig.Reopen();
            var app = new AppFlow(resumed.Flow, resumed.Progress, resumed.Economy.Wallet, new NullApp());
            app.Start(false, saved);
            Assert.That(app.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(resumed.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));

            Assert.That(resumed.Flow.PressGiveUp(), Is.True);
            Assert.That(resumed.Flow.ConfirmGiveUp(), Is.True);
            Assert.That(app.ExitToLobby(), Is.True, "X de Nivel fallido");
            LevelStateData exited = resumed.Store.Load()!;
            StringAssert.StartsWith("h1|X|", exited.History);

            Rig later = resumed.Reopen();
            var lobby = new AppFlow(later.Flow, later.Progress, later.Economy.Wallet, new NullApp());
            lobby.Start(false, exited);
            Assert.That(lobby.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(later.Flow.Attempt.FailsLevel, Is.EqualTo(1));

            LevelStateData broken = LevelStateSnapshot.Copy(exited);
            broken.History = "roto";
            Rig last = later.Reopen();
            var discard = new AppFlow(last.Flow, last.Progress, last.Economy.Wallet, new NullApp());
            discard.Start(false, broken);
            Assert.That(discard.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(last.Store.Load(), Is.Null, "un estado que no sirve se descarta");
        }

        [Test]
        public void ProfileStore_WritesLevelStateIntoTheProfile()
        {
            var economy = new EconomyFixture();
            var store = new ProfileLevelStateStore(economy.Profile, economy.Store);
            var state = new LevelStateData { Level = 7, Seed = "amazonfill:v1:m01:v1:n0007", History = "h1|P|||02", Pours = 1 };
            store.Save(state);
            Profile saved = ProfileJson.Deserialize(economy.Store.Json!);
            Assert.That(saved.LevelState, Is.Not.Null);
            Assert.That(saved.LevelState!.History, Is.EqualTo("h1|P|||02"));
            Assert.That(store.LastSaveMs, Is.GreaterThanOrEqualTo(0.0));
            store.Clear();
            Assert.That(ProfileJson.Deserialize(economy.Store.Json!).LevelState, Is.Null);
        }

        internal static SolveResult Solve(LevelFlow flow)
        {
            SolveResult result = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(result.IsSolved, Is.True);
            return result;
        }

        // Juega la solución del solver; el nivel queda ganado (input bloqueado hasta que la vista termine).
        internal static void Win(LevelFlow flow)
        {
            SolveResult result = Solve(flow);
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour move = result.GetMove(i);
                flow.TapCuia(move.Source);
                flow.TapCuia(move.Target);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
        }

        // Tablero 6 del tutorial: 3 → 4 deja Aguas estancadas; el modal se abre 400 ms después de vaciarse la cola (R15).
        internal static void GetStuck(LevelFlow flow)
        {
            Assert.That(flow.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(flow.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            for (int i = 0; i < 100 && flow.Phase != FlowPhase.StuckModal; i++)
            {
                flow.Tick(16, true);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
        }

        /// <summary>Flujo con cartera y nivel en curso en memoria; <see cref="Reopen"/> es «cerrar y abrir la app».</summary>
        internal sealed class Rig
        {
            private readonly int _offset;

            public Rig(int offset = 0, int seeds = 0, MemoryLevelStateStore? store = null, EconomyFixture? economy = null)
            {
                _offset = offset;
                Economy = economy ?? new EconomyFixture(seeds);
                Store = store ?? new MemoryLevelStateStore();
                Listener = new RecordingListener();
                Progress = new MemoryProgressStore();
                Flow = new LevelFlow(LevelCatalog.Create(new[] { TestPacks.Tutorial }, offset), Progress, Listener, GameConfig.TapSwitchSelection, PourModes.Default, Economy.Wallet)
                {
                    StateStore = Store,
                };
            }

            public EconomyFixture Economy { get; }

            public MemoryLevelStateStore Store { get; }

            public RecordingListener Listener { get; }

            public MemoryProgressStore Progress { get; }

            public LevelFlow Flow { get; }

            public Rig Reopen()
            {
                return new Rig(_offset, store: Store, economy: Economy);
            }

            public void Start(int level)
            {
                Flow.Load(level);
                Assert.That(Flow.TapAnywhere(), Is.True);
                Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            }
        }

        private sealed class NullApp : IAppFlowListener
        {
            public void OnAppScreenChanged(AppScreen screen)
            {
            }
        }
    }
}
