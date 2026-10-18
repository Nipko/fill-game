#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Solver.Murky;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Morpho · Pista en el cliente (tarea MO1; 04 §2.3; 03 §5 y §8; 08 §9 <c>hint_budget_nodes</c> = 40.000,
    /// <c>hint_weight</c> = 2, <c>hint_timeout_ms</c> = 150 y <c>hint_unlock_level</c> = 12; D-019, Consecuencias; K2): las
    /// cuatro respuestas en su orden con tableros de prueba (también los de la Tinaja de la propuesta b.2), la consulta en el hilo
    /// de trabajo sin bloquear el hilo principal, el corte a los 150 ms del <see cref="ManualClock"/>, y en el flujo las reglas de
    /// uso, el cobro (stock primero, 100 semillas; nunca sin vertido), el enfriamiento de 5 s [P], <c>hint_supported</c>, la
    /// pista que se apaga al verter y el recálculo tras la Cuia extra o la de rescate.
    /// </summary>
    public sealed class MorphoBoosterTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 17, 15, 0, 0, DateTimeKind.Utc);
        private const int WaitLimitMs = 20000;
        private const int CampaignLevel = 15;

        // a = 1, b = 2, c = 3 (propuesta de la tinaja, b.2; los mismos tableros que TinajaSolverTests del núcleo).
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia T(int hiddenMask, params int[] colorsBottomToTop)
        {
            return Cuia.Create(LevelValidation.TinajaCapacity, colorsBottomToTop, hiddenMask).WithMechanic(MechanicType.Tinaja);
        }

        private static Cuia Ch(params int[] colorsBottomToTop)
        {
            return Cuia.Create(LevelValidation.CucharonCapacity, colorsBottomToTop).WithMechanic(MechanicType.Cucharon);
        }

        /// <summary>S0 de b.2: N[c,c,a] N[a,b,a] N[b,b,b] T[c(o),a(o),c] Ch[]: tiene ruta segura.</summary>
        private static BoardState TinajaDebutS0()
        {
            return new BoardState(new[] { C(4, 3, 3, 1), C(4, 1, 2, 1), C(4, 2, 2, 2), T(0x3, 3, 1, 3), Ch() });
        }

        /// <summary>Tablero perdido de b.2: N[c,c,c] N[a,b,a,a] N[b,b,b] T[c(o),a] Ch[]: una realidad, sin salida.</summary>
        private static BoardState TinajaLost()
        {
            return new BoardState(new[] { C(4, 3, 3, 3), C(4, 1, 2, 1, 1), C(4, 2, 2, 2), T(0x1, 3, 1), Ch() });
        }

        /// <summary>
        /// Segundo tablero perdido de b.2, el vaivén tras la suelta (<c>TinajaSolverTests.SwingAfterTheRelease</c>):
        /// N[c,c,c,c] N[a,b,a,a] N[b,b,b] T[] (inerte) ch[a] (ya una Cuia chica normal). Una realidad, sin salida.
        /// </summary>
        private static BoardState TinajaSwingAfterTheRelease()
        {
            return new BoardState(new[] { C(4, 3, 3, 3, 3), C(4, 1, 2, 1, 1), C(4, 2, 2, 2), Cuia.Empty(LevelValidation.TinajaCapacity).WithMechanic(MechanicType.Tinaja), C(2, 1) });
        }

        /// <summary>Aguas turbias de MurkyTests: dos realidades, la verdadera con solución, y ningún vertido seguro en las dos.</summary>
        private static BoardState MurkyNoSafePour()
        {
            return new BoardState(new[] { Cuia.Create(3, new[] { 3, 3, 1 }, 0x2), Cuia.Create(3, new[] { 2, 1 }, 0x1), C(3, 3, 2, 2), C(3, 1) });
        }

        /// <summary>
        /// <see cref="MurkyNoSafePour"/> con una tinaja vacía e inerte al lado (no vierte ni recibe): un tablero con tinaja y
        /// R = 2 en el que alguna realidad tiene solución y ningún vertido es seguro en todas (respuesta 3, «… la tinaja»). No es
        /// un nivel de campaña (<see cref="LevelValidation"/> lo rechaza: la tinaja es la única fuente de ocultas de su nivel).
        /// </summary>
        private static BoardState TinajaNoSafePour()
        {
            return new BoardState(new[]
            {
                Cuia.Create(3, new[] { 3, 3, 1 }, 0x2), Cuia.Create(3, new[] { 2, 1 }, 0x1), C(3, 3, 2, 2), C(3, 1),
                Cuia.Empty(LevelValidation.TinajaCapacity).WithMechanic(MechanicType.Tinaja),
            });
        }

        /// <summary>Sin ocultas y con solución: A*.</summary>
        private static BoardState PlainSolvable()
        {
            return new BoardState(new[] { C(4, 1, 1, 1, 2), C(4, 2, 2, 2, 1), Cuia.Empty(4) });
        }

        /// <summary>Sin ocultas y sin ningún vertido: A* lo demuestra irresoluble.</summary>
        private static BoardState PlainNoExit()
        {
            return new BoardState(new[] { C(2, 1, 2), C(2, 2, 1) });
        }

        private static MorphoBooster NewMorpho(BoosterConfig? config = null, Action? hook = null)
        {
            return new MorphoBooster(config ?? BoosterConfig.Defaults, new ManualClock(T0), hook);
        }

        [Test]
        public void Config_HintKeys_AreThe08_9Defaults()
        {
            BoosterConfig config = BoosterConfig.Defaults;
            Assert.That(config.HintBudgetNodes, Is.EqualTo(40000));
            Assert.That(config.SolverWeight, Is.EqualTo(2));
            Assert.That(config.HintTimeoutMs, Is.EqualTo(150));
            Assert.That(config.HintUnlockLevel, Is.EqualTo(12));
            Assert.That(config.HintTutorialUnits, Is.EqualTo(3));
            Assert.That(BoosterConfig.HintCooldownMs, Is.EqualTo(5000), "04 §2.3: «Cooldown 5 s entre pistas» [P]");
            Assert.That(EconomyConfig.Defaults.HintSeedPrice, Is.EqualTo(100));

            config.HintTimeoutMs = 0;
            Assert.Throws<InvalidOperationException>(() => config.Validate());

            BoosterConfig noGift = BoosterConfig.Defaults;
            noGift.HintTutorialUnits = 0;
            Assert.Throws<InvalidOperationException>(() => noGift.Validate(), "hint_tutorial_units >= 1");
        }

        [Test]
        public void ShouldGift_OnlyAtTheUnlockLevel_NotYetBeaten_AndOnce()
        {
            // Tarea TU1b (04 §2.3): 3 Morphos al cargar el 12 por primera vez; nunca otro nivel, ni el 12 ya superado, ni dos veces.
            BoosterConfig config = BoosterConfig.Defaults;
            var economy = new EconomyFixture(0);
            Wallet wallet = economy.Wallet;
            Assert.That(config.HintTutorialUnits, Is.EqualTo(3), "hint_tutorial_units; 04 §2.3: «se acreditan 3 Morphos al stock»");
            Assert.That(MorphoBooster.ShouldGift(12, 11, false, wallet, config), Is.True);
            Assert.That(MorphoBooster.ShouldGift(12, 0, false, wallet, config), Is.True, "también con un perfil nuevo (af_level)");
            Assert.That(MorphoBooster.ShouldGift(12, 11, true, wallet, config), Is.False, "ya regalado en este nivel");
            Assert.That(MorphoBooster.ShouldGift(12, 12, false, wallet, config), Is.False, "el 12 ya superado");
            Assert.That(MorphoBooster.ShouldGift(11, 10, false, wallet, config), Is.False);
            Assert.That(MorphoBooster.ShouldGift(13, 12, false, wallet, config), Is.False);
            Assert.That(MorphoBooster.ShouldGift(12, 11, false, null, config), Is.False, "sin cartera");
            Assert.That(wallet.Apply(Transaction.GrantStock(BoosterKind.Hint, 1)).IsApplied, Is.True);
            Assert.That(MorphoBooster.ShouldGift(12, 11, false, wallet, config), Is.True, "con stock previo también: son 3 regalados [P]");

            config.HintUnlockLevel = 14; // hint_unlock_level por Remote Config: el regalo va con la ranura
            Assert.That(MorphoBooster.ShouldGift(12, 11, false, wallet, config), Is.False);
            Assert.That(MorphoBooster.ShouldGift(14, 13, false, wallet, config), Is.True);
        }

        [Test]
        public void Flow_Level12_TellsTheGiftOnce_WithItsUnits_NotOnRestartNorOnLoad()
        {
            // TU1b (04 §2.3): el flujo avisa a la vista (IHintListener.OnHintGifted) una sola vez, con los 3 Morphos, al cargar el 12
            // aún no superado; ni Volver a la orilla ni volver a cargar el nivel lo repiten.
            Rig rig = NewRig(500, maxLevel: 11);
            rig.Start(12);
            Assert.That(rig.Hints.GiftCalls, Is.EqualTo(1), "un aviso");
            Assert.That(rig.Hints.Gifted, Is.EqualTo(3), "con 3 Morphos (hint_tutorial_units)");
            Assert.That(rig.Hints.Gifted, Is.EqualTo(rig.Flow.Boosters.HintTutorialUnits));
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(3), "los mismos que entran al stock");

            Assert.That(rig.Flow.Restart(), Is.True, "Volver a la orilla");
            Assert.That(rig.Hints.GiftCalls, Is.EqualTo(1), "Volver a la orilla no avisa otra vez");
            rig.Start(12);
            Assert.That(rig.Hints.GiftCalls, Is.EqualTo(1), "volver a cargar el 12 no avisa otra vez");
            Assert.That(rig.Hints.Gifted, Is.EqualTo(3));
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(3), "ni regala otra vez");

            // Otro nivel no avisa.
            Rig other = NewRig(500, maxLevel: 12);
            other.Start(13);
            Assert.That(other.Hints.GiftCalls, Is.EqualTo(0));
            Assert.That(other.Hints.Gifted, Is.EqualTo(0));
        }

        [Test]
        public void Answer1_Pour_AStarWithoutHidden_AndSafeMovesWithTheTinaja()
        {
            MorphoBooster morpho = NewMorpho();
            BoardState plain = PlainSolvable();
            HintResult a = morpho.Compute(plain, null, PourMode.Run, true);
            Assert.That(a.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(a.Solver, Is.EqualTo(HintSolver.AStar));
            Assert.That(a.Tinaja, Is.False);
            Assert.That(a.Realities, Is.EqualTo(1));
            Assert.That(a.Charged, Is.False, "la clase no cobra: cobra el flujo al entregarla");
            Assert.That(PourRules.IsLegal(plain, a.Move.Source, a.Move.Target), Is.True);
            BoardState after = PourRules.Apply(plain, a.Move.Source, a.Move.Target, PourMode.Run);
            Assert.That(new AStarSolver().Solve(after, SolverOptions.ForPipeline(PourMode.Run)).Status, Is.EqualTo(SolveStatus.Solved),
                "el vertido propuesto deja el tablero resoluble");

            BoardState s0 = TinajaDebutS0();
            HintResult t = morpho.Compute(s0, null, PourMode.Run, true);
            Assert.That(t.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(t.Solver, Is.EqualTo(HintSolver.SafeMoves), "con la tinaja, SafeMoves (D-019)");
            Assert.That(t.Tinaja, Is.True);
            Assert.That(PourRules.IsLegal(s0, t.Move.Source, t.Move.Target), Is.True);
            Assert.That(MorphoBooster.UsesSafeMoves(s0), Is.True);
            Assert.That(MorphoBooster.UsesSafeMoves(plain), Is.False);
            Assert.That(MorphoBooster.UsesSafeMoves(MurkyNoSafePour()), Is.True, "Aguas turbias con ocultas");
        }

        [Test]
        public void Answer2_NoExit_AlsoTheLostTinajaBoard_WithOneReality_NeverTheFirefly()
        {
            MorphoBooster morpho = NewMorpho();
            HintResult plain = morpho.Compute(PlainNoExit(), null, PourMode.Run, true);
            Assert.That(plain.Answer, Is.EqualTo(HintAnswer.NoExit), "«Desde aquí no hay salida: la Marea o la orilla»");
            Assert.That(plain.Solver, Is.EqualTo(HintSolver.AStar));

            HintResult lost = morpho.Compute(TinajaLost(), null, PourMode.Run, true);
            Assert.That(lost.Answer, Is.EqualTo(HintAnswer.NoExit), "b.2: el tablero perdido de la tinaja, sin salida");
            Assert.That(lost.Realities, Is.EqualTo(1));
            Assert.That(lost.Tinaja, Is.True);
            Assert.That(lost.Answer, Is.Not.EqualTo(HintAnswer.NoSafePour), "nunca la luciérnaga con R = 1");
            Assert.That(lost.Charged, Is.False);

            // El segundo tablero perdido de b.2: tras la suelta, con la tinaja inerte (sigue buscando con SafeMoves).
            HintResult swing = morpho.Compute(TinajaSwingAfterTheRelease(), null, PourMode.Run, true);
            Assert.That(swing.Answer, Is.EqualTo(HintAnswer.NoExit), "b.2: el vaivén tras la suelta, sin salida");
            Assert.That(swing.Solver, Is.EqualTo(HintSolver.SafeMoves), "una tinaja, también vacía, busca con SafeMoves (M13)");
            Assert.That(swing.Realities, Is.EqualTo(1));
            Assert.That(swing.Tinaja, Is.True);
            Assert.That(swing.Answer, Is.Not.EqualTo(HintAnswer.NoSafePour), "nunca la luciérnaga con R = 1");
            Assert.That(swing.HasMove, Is.False);
            Assert.That(swing.Charged, Is.False);
        }

        [Test]
        public void Answer3_NoSafePour_SomeRealitySolvable_WithTwoRealities()
        {
            MorphoBooster morpho = NewMorpho();
            HintResult r = morpho.Compute(MurkyNoSafePour(), null, PourMode.Run, true);
            Assert.That(r.Answer, Is.EqualTo(HintAnswer.NoSafePour), "«No veo un vertido seguro: la luciérnaga ilumina una cuia»");
            Assert.That(r.Realities, Is.EqualTo(2));
            Assert.That(r.Tinaja, Is.False, "sin tinaja: «… una cuia»");
            Assert.That(r.HasMove, Is.False);
            Assert.That(r.Charged, Is.False);
        }

        [Test]
        public void Answer3_NoSafePour_WithTheTinaja_SaysTheTinaja_AndNeverCharges()
        {
            // D-019 y 04 §2.3 a: «No veo un vertido seguro: … la tinaja» en un tablero con tinaja. Con la tinaja como única
            // fuente de ocultas (lo que admite LevelValidation) no se encontró ningún estado así: una búsqueda de más de dos
            // millones de estados alcanzables desde tableros con tinaja de 3-5 frutas solo dio «vertido» o «sin salida» (la
            // resolubilidad no cambió entre realidades). El tablero de prueba junta las dos realidades de Aguas turbias con una
            // tinaja inerte, así que no vale como nivel de campaña y el flujo no se puede probar con él.
            BoardState board = TinajaNoSafePour();
            Assert.That(LevelValidation.Validate(board), Is.Not.Null, "no es un nivel de campaña");

            var safe = new SafeMoves();
            Assert.That(safe.Find(board, NewMorpho().OptionsFor(board, PourMode.Run, true), board), Is.EqualTo(RealityStatus.Ok));
            Assert.That(safe.RealityCount, Is.EqualTo(2));
            Assert.That(safe.Count, Is.EqualTo(0), "ningún vertido seguro en las dos realidades");
            Assert.That(safe.Outcome, Is.EqualTo(SafeMovesOutcome.SomeRealitySolvable), "alguna realidad tiene solución");

            HintResult r = NewMorpho().Compute(board, null, PourMode.Run, true);
            Assert.That(r.Answer, Is.EqualTo(HintAnswer.NoSafePour), "«No veo un vertido seguro: la luciérnaga ilumina la tinaja»");
            Assert.That(r.Tinaja, Is.True, "con tinaja: «… la tinaja»");
            Assert.That(r.Solver, Is.EqualTo(HintSolver.SafeMoves));
            Assert.That(r.Realities, Is.EqualTo(2), "nunca con R = 1");
            Assert.That(r.HasMove, Is.False);
            Assert.That(r.Charged, Is.False, "no cobra");
        }

        [Test]
        public void Answer4_NotFound_OutOfBudget_OrCutBeforeStarting()
        {
            BoosterConfig tight = BoosterConfig.Defaults;
            tight.HintBudgetNodes = 1;
            HintResult budget = NewMorpho(tight).Compute(MurkyNoSafePour(), null, PourMode.Run, true);
            Assert.That(budget.Answer, Is.EqualTo(HintAnswer.NotFound), "«No encuentro un camino desde aquí»");
            Assert.That(budget.Cancelled, Is.False);

            var cut = new SolverCancellation();
            cut.Request();
            HintResult plain = NewMorpho().Compute(PlainSolvable(), null, PourMode.Run, true, 0, cut);
            Assert.That(plain.Answer, Is.EqualTo(HintAnswer.NotFound));
            Assert.That(plain.Cancelled, Is.True);
            var cut2 = new SolverCancellation();
            cut2.Request();
            HintResult safe = NewMorpho().Compute(TinajaDebutS0(), null, PourMode.Run, true, 0, cut2);
            Assert.That(safe.Answer, Is.EqualTo(HintAnswer.NotFound));
            Assert.That(safe.Cancelled, Is.True);
        }

        [Test]
        public void AnswerOf_FollowsTheOrderOfD019()
        {
            Assert.That(MorphoBooster.AnswerOf(SafeMovesOutcome.Found), Is.EqualTo(HintAnswer.Pour));
            Assert.That(MorphoBooster.AnswerOf(SafeMovesOutcome.NoExit), Is.EqualTo(HintAnswer.NoExit));
            Assert.That(MorphoBooster.AnswerOf(SafeMovesOutcome.SomeRealitySolvable), Is.EqualTo(HintAnswer.NoSafePour));
            Assert.That(MorphoBooster.AnswerOf(SafeMovesOutcome.BudgetExceeded), Is.EqualTo(HintAnswer.NotFound));
            Assert.That(MorphoBooster.AnswerOf(SafeMovesOutcome.Won), Is.EqualTo(HintAnswer.NotFound));
            Assert.That(MorphoBooster.AnswerOf(SafeMovesOutcome.RealitiesRejected), Is.EqualTo(HintAnswer.NotFound));
            Assert.That(MorphoBooster.AnswerOf(SolveStatus.Solved, 3), Is.EqualTo(HintAnswer.Pour));
            Assert.That(MorphoBooster.AnswerOf(SolveStatus.Solved, 0), Is.EqualTo(HintAnswer.NotFound), "ya ganado: nada que proponer");
            Assert.That(MorphoBooster.AnswerOf(SolveStatus.Unsolvable, 0), Is.EqualTo(HintAnswer.NoExit));
            Assert.That(MorphoBooster.AnswerOf(SolveStatus.BudgetExceeded, 0), Is.EqualTo(HintAnswer.NotFound));
        }

        [Test]
        public void Worker_SameAnswerAsOnTheCallingThread()
        {
            MorphoBooster morpho = NewMorpho();
            BoardState[] boards = { PlainSolvable(), PlainNoExit(), TinajaDebutS0(), TinajaLost(), TinajaSwingAfterTheRelease(), MurkyNoSafePour(), TinajaNoSafePour() };
            HintAnswer[] expected = { HintAnswer.Pour, HintAnswer.NoExit, HintAnswer.Pour, HintAnswer.NoExit, HintAnswer.NoExit, HintAnswer.NoSafePour, HintAnswer.NoSafePour };
            for (int i = 0; i < boards.Length; i++)
            {
                BoardState board = boards[i];
                HintResult direct = NewMorpho().Compute(board, null, PourMode.Run, true);
                Assert.That(direct.Answer, Is.EqualTo(expected[i]), board.ToString());
                Assert.That(morpho.Start(board, null, PourMode.Run, true, 1), Is.True);
                HintResult threaded = WaitDone(morpho);
                Assert.That(threaded.Answer, Is.EqualTo(direct.Answer), board.ToString());
                Assert.That(threaded.Move, Is.EqualTo(direct.Move), board.ToString());
                Assert.That(threaded.Realities, Is.EqualTo(direct.Realities));
                Assert.That(threaded.Tinaja, Is.EqualTo(direct.Tinaja), board.ToString());
                Assert.That(morpho.LastError, Is.Null);
            }
        }

        [Test]
        public void Worker_DoesNotBlockTheMainThread_AndTheCutAt150msSaysNotFound()
        {
            using (var gate = new ManualResetEventSlim(false))
            {
                var clock = new ManualClock(T0);
                var morpho = new MorphoBooster(BoosterConfig.Defaults, clock, () => gate.Wait(WaitLimitMs));
                var watch = Stopwatch.StartNew();
                Assert.That(morpho.Start(TinajaDebutS0(), null, PourMode.Run, true, 1), Is.True);
                Assert.That(morpho.IsBusy, Is.True);
                Assert.That(morpho.Start(PlainSolvable(), null, PourMode.Run, true, 1), Is.False, "una consulta a la vez");
                Assert.Throws<InvalidOperationException>(() => morpho.Compute(PlainSolvable(), null, PourMode.Run, true));

                // El hilo de trabajo está retenido: el principal sondea sin esperar.
                for (int i = 0; i < 20; i++)
                {
                    Assert.That(morpho.Poll(out _), Is.EqualTo(MorphoPoll.Running));
                }

                Assert.That(watch.ElapsedMilliseconds, Is.LessThan(1000), "Start y Poll vuelven enseguida");
                clock.Advance(TimeSpan.FromMilliseconds(149));
                Assert.That(morpho.Poll(out _), Is.EqualTo(MorphoPoll.Running));
                Assert.That(morpho.TimeoutRequested, Is.False, "a los 149 ms sigue");
                clock.Advance(TimeSpan.FromMilliseconds(1));
                Assert.That(morpho.Poll(out _), Is.EqualTo(MorphoPoll.Running));
                Assert.That(morpho.TimeoutRequested, Is.True, "a los 150 ms (hint_timeout_ms) el hilo principal marca el corte");
                gate.Set();
                HintResult r = WaitDone(morpho);
                Assert.That(r.Answer, Is.EqualTo(HintAnswer.NotFound), "«No encuentro un camino desde aquí»");
                Assert.That(r.Cancelled, Is.True);
                Assert.That(r.Charged, Is.False);
            }
        }

        [Test]
        public void Worker_ClockGoesBack_CutsAtOnce()
        {
            using (var gate = new ManualResetEventSlim(false))
            {
                var clock = new ManualClock(T0);
                var morpho = new MorphoBooster(BoosterConfig.Defaults, clock, () => gate.Wait(WaitLimitMs));
                Assert.That(morpho.Start(TinajaDebutS0(), null, PourMode.Run, true, 1), Is.True);
                Assert.That(morpho.Poll(out _), Is.EqualTo(MorphoPoll.Running));
                Assert.That(morpho.TimeoutRequested, Is.False);
                clock.Advance(TimeSpan.FromHours(-1));
                Assert.That(morpho.Poll(out _), Is.EqualTo(MorphoPoll.Running));
                Assert.That(morpho.TimeoutRequested, Is.True, "con el reloj hacia atrás no espera a que vuelva a alcanzar hint_timeout_ms");
                gate.Set();
                HintResult r = WaitDone(morpho);
                Assert.That(r.Answer, Is.EqualTo(HintAnswer.NotFound));
                Assert.That(r.Cancelled, Is.True);
                Assert.That(r.Charged, Is.False);
            }
        }

        [Test]
        public void Worker_Abandoned_DiscardsTheAnswer_AndTheNextQueryRuns()
        {
            using (var gate = new ManualResetEventSlim(false))
            {
                var morpho = new MorphoBooster(BoosterConfig.Defaults, new ManualClock(T0), () => gate.Wait(WaitLimitMs));
                Assert.That(morpho.Start(PlainSolvable(), null, PourMode.Run, true, 1), Is.True);
                morpho.Abandon();
                Assert.That(morpho.Poll(out _), Is.EqualTo(MorphoPoll.Idle), "abandonada: no hay respuesta");
                gate.Set();
                var watch = Stopwatch.StartNew();
                while (morpho.IsBusy && watch.ElapsedMilliseconds < WaitLimitMs)
                {
                    Thread.Sleep(1);
                }

                Assert.That(morpho.IsBusy, Is.False);
                Assert.That(morpho.Poll(out _), Is.EqualTo(MorphoPoll.Idle));
                Assert.That(morpho.Start(PlainSolvable(), null, PourMode.Run, true, 1), Is.True);
                Assert.That(WaitDone(morpho).Answer, Is.EqualTo(HintAnswer.Pour));
            }
        }

        [Test]
        public void Offer_States_AndPayment_StockFirst()
        {
            BoosterConfig config = BoosterConfig.Defaults;
            var economy = new EconomyFixture(150);
            Wallet wallet = economy.Wallet;
            Assert.That(MorphoBooster.Evaluate(11, true, true, false, 0, wallet, config).State, Is.EqualTo(HintState.Hidden));
            Assert.That(MorphoBooster.Evaluate(12, false, true, false, 0, wallet, config).State, Is.EqualTo(HintState.Unsupported));
            Assert.That(MorphoBooster.Evaluate(12, true, true, true, 0, wallet, config).State, Is.EqualTo(HintState.Thinking));
            Assert.That(MorphoBooster.Evaluate(12, true, false, false, 0, wallet, config).State, Is.EqualTo(HintState.Disabled));
            Assert.That(MorphoBooster.Evaluate(12, true, true, false, 10, wallet, config).State, Is.EqualTo(HintState.Cooldown));
            HintOffer ready = MorphoBooster.Evaluate(12, true, true, false, 0, wallet, config);
            Assert.That(ready.State, Is.EqualTo(HintState.Ready));
            Assert.That(ready.Price, Is.EqualTo(100));
            Assert.That(ready.Missing, Is.EqualTo(0));
            Assert.That(ready.CanUse, Is.True);

            economy.Profile.Stock.Hint = 1;
            Assert.That(MorphoBooster.Pay(wallet).IsApplied, Is.True);
            Assert.That(wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(0), "primero el stock");
            Assert.That(wallet.Seeds, Is.EqualTo(150));
            Assert.That(MorphoBooster.Pay(wallet).IsApplied, Is.True);
            Assert.That(wallet.Seeds, Is.EqualTo(50), "después, 100 semillas");
            HintOffer poor = MorphoBooster.Evaluate(12, true, true, false, 0, wallet, config);
            Assert.That(poor.Missing, Is.EqualTo(50), "«Te faltan 50»");
            Assert.That(poor.CanUse, Is.False);
            Assert.That(MorphoBooster.CanPay(wallet), Is.False);
            Assert.That(MorphoBooster.Pay(wallet).IsApplied, Is.False);
            Assert.That(wallet.Seeds, Is.EqualTo(50), "un cobro rechazado no cambia nada");
        }

        // --- En el flujo ---

        [Test]
        public void Flow_HiddenBefore12_ReadyWithSeeds_AnswerCharges100_ThenCooldown5s()
        {
            Rig rig = NewRig(500);
            rig.Flow.Load(11);
            rig.Flow.TapAnywhere();
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Hidden), "la ranura sale en el 12");
            Assert.That(rig.Flow.PressHint(), Is.False);

            rig.Start(CampaignLevel);
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Ready));
            BoardState before = rig.Flow.Session.Current;
            Assert.That(rig.Flow.PressHint(), Is.True);
            Assert.That(rig.Flow.HintThinking, Is.True);
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Thinking));
            Assert.That(rig.Hints.Thinking, Is.EqualTo(new[] { true }));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(500), "pedirla no cobra");

            HintResult r = WaitAnswer(rig);
            Assert.That(r.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(r.Charged, Is.True);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(400), "100 semillas al dar el vertido");
            Assert.That(rig.Flow.HintsUsed, Is.EqualTo(1));
            Assert.That(rig.Flow.HasActiveHint, Is.True);
            Assert.That(rig.Flow.ActiveHint, Is.EqualTo(r.Move));
            Assert.That(PourRules.IsLegal(before, r.Move.Source, r.Move.Target), Is.True);
            Assert.That(rig.Hints.Answers.Count, Is.EqualTo(1));
            Assert.That(rig.Hints.Thinking, Is.EqualTo(new[] { true, false }));
            Assert.That(rig.Probe.Lines, Is.EqualTo(1), "una línea AF_PERF morpho por respuesta");
            Assert.That(rig.Probe.TimeoutMs, Is.EqualTo(150));

            // Enfriamiento de 5 s [P] con el reloj del flujo.
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Cooldown));
            Assert.That(rig.Flow.Hint.CooldownMs, Is.EqualTo(5000));
            Assert.That(rig.Flow.PressHint(), Is.False);
            rig.Economy.Clock.Advance(TimeSpan.FromMilliseconds(4999));
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Cooldown));
            rig.Economy.Clock.Advance(TimeSpan.FromMilliseconds(1));
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Ready));

            // 04 §2.3 d: verter otra cosa apaga la pista (sin reembolso).
            Pour other = FindOtherPour(rig.Flow.Session.Current, r.Move);
            rig.Flow.TapCuia(other.Source);
            rig.Flow.TapCuia(other.Target);
            rig.Flow.Tick(16, true);
            Assert.That(rig.Flow.HasActiveHint, Is.False);
            Assert.That(rig.Hints.Cleared, Is.EqualTo(1));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(400));
        }

        [Test]
        public void Flow_StockFirst_ThenSeeds_AndRvHintGivesAUnit()
        {
            Rig rig = NewRig(0);
            rig.Start(CampaignLevel);
            Assert.That(rig.Flow.Hint.CanUse, Is.False, "sin stock ni saldo: la vista ofrece el video o «Te faltan»");
            Assert.That(rig.Flow.Hint.Missing, Is.EqualTo(100));
            Assert.That(rig.Flow.PressHint(), Is.False);
            Assert.That(rig.Flow.GrantHintVideoUnit(), Is.True, "rv_hint: 1 pista al stock");
            Assert.That(rig.Flow.Hint.FromStock, Is.True);
            Assert.That(rig.Flow.PressHint(), Is.True);
            HintResult r = WaitAnswer(rig);
            Assert.That(r.Charged, Is.True);
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(0));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(0));
        }

        [Test]
        public void Flow_NeverChargesWithoutAPour_OutOfBudget()
        {
            BoosterConfig tight = BoosterConfig.Defaults;
            tight.HintBudgetNodes = 1;
            Rig rig = NewRig(500, tight);
            rig.Start(CampaignLevel);
            Assert.That(rig.Flow.PressHint(), Is.True);
            HintResult r = WaitAnswer(rig);
            Assert.That(r.Answer, Is.EqualTo(HintAnswer.NotFound));
            Assert.That(r.Charged, Is.False);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(500), "no cobra");
            Assert.That(rig.Flow.HasActiveHint, Is.False);
            Assert.That(rig.Flow.HintsUsed, Is.EqualTo(0));
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Cooldown), "el enfriamiento cuenta desde cualquier respuesta [P]");
        }

        [Test]
        public void Flow_NoSafePour_InAMurkyBoard_DoesNotCharge()
        {
            Rig rig = NewRig(500, null, LevelPackReader.Read(MurkyPack()));
            rig.Start(CampaignLevel);
            Assert.That(rig.Flow.Session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(rig.Flow.PressHint(), Is.True);
            HintResult r = WaitAnswer(rig);
            Assert.That(r.Answer, Is.EqualTo(HintAnswer.NoSafePour));
            Assert.That(r.Charged, Is.False);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(500));
        }

        [Test]
        public void Flow_CutAt150ms_SaysNotFound_WithoutCharging_AndTheMainThreadKeepsTicking()
        {
            using (var gate = new ManualResetEventSlim(false))
            {
                Rig rig = NewRig(500);
                rig.Start(CampaignLevel);
                rig.Flow.Morpho = new MorphoBooster(rig.Flow.Boosters, rig.Economy.Clock, () => gate.Wait(WaitLimitMs));
                var watch = Stopwatch.StartNew();
                Assert.That(rig.Flow.PressHint(), Is.True);
                for (int i = 0; i < 30; i++)
                {
                    rig.Flow.Tick(16, true); // el hilo principal sigue: 30 fotogramas con el hilo de trabajo retenido
                }

                Assert.That(watch.ElapsedMilliseconds, Is.LessThan(1000));
                Assert.That(rig.Flow.HintThinking, Is.True);
                Assert.That(rig.Flow.AcceptsBoardInput, Is.True, "el tablero sigue aceptando toques");
                rig.Economy.Clock.Advance(TimeSpan.FromMilliseconds(150));
                rig.Flow.Tick(16, true);
                Assert.That(rig.Flow.Morpho.TimeoutRequested, Is.True);
                gate.Set();
                HintResult r = WaitAnswer(rig);
                Assert.That(r.Answer, Is.EqualTo(HintAnswer.NotFound));
                Assert.That(r.Cancelled, Is.True);
                Assert.That(r.Charged, Is.False);
                Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(500), "el corte no cobra");
            }
        }

        [Test]
        public void Flow_ExtraCuiaWhileThinking_Abandons_ThenRecalculatesWithTheAddedCuia()
        {
            using (var gate = new ManualResetEventSlim(false))
            {
                Rig rig = NewRig(500);
                rig.Start(CampaignLevel);
                int cuias = rig.Flow.Session.Current.Count;
                rig.Flow.Morpho = new MorphoBooster(rig.Flow.Boosters, rig.Economy.Clock, () => gate.Wait(WaitLimitMs));
                Assert.That(rig.Flow.PressHint(), Is.True);
                Assert.That(rig.Flow.UseExtraCuia()!.IsApplied, Is.True);
                rig.Flow.Tick(16, true);
                Assert.That(rig.Flow.HintThinking, Is.False, "el tablero cambió: la consulta se abandona");
                Assert.That(rig.Hints.Answers.Count, Is.EqualTo(0), "sin respuesta");
                Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(350), "solo la Cuia extra (150); Morpho no cobra");
                gate.Set();
                rig.Flow.Morpho = new MorphoBooster(rig.Flow.Boosters, rig.Economy.Clock);

                Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Ready), "lo abandonado no pone enfriamiento");
                Assert.That(rig.Flow.PressHint(), Is.True);
                HintResult r = WaitAnswer(rig);
                Assert.That(rig.Flow.Session.Current.Count, Is.EqualTo(cuias + 1));
                Assert.That(r.Answer, Is.EqualTo(HintAnswer.Pour), "04 §2.3 c: con la cuia añadida");
                Assert.That(PourRules.IsLegal(rig.Flow.Session.Current, r.Move.Source, r.Move.Target), Is.True);
                Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(250));
            }
        }

        [Test]
        public void Flow_AfterTheRescueCuia_TheHintIsCalculatedWithTheAddedCuia_AndChargesOnlyWithAPour()
        {
            // 04 §2.3 c: «Tras Cuia extra o Cuia de rescate la pista se calcula con la cuia añadida». Nivel 36 = tablero 6 del
            // tutorial (FunnelFixture): 3 → 4 deja Aguas estancadas y la compra cumple (04 §6.4).
            Rig rig = NewRig(1000, offset: 30, maxLevel: 35, pack: TestPacks.Tutorial);
            rig.Start(36);
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Ready));
            Assert.That(rig.Flow.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(rig.Flow.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(rig.Flow.Session.Status, Is.EqualTo(LevelStatus.Stuck));
            rig.Flow.Tick(16, true);
            rig.Flow.Tick(GameConfig.StuckModalDelayMs, true);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Disabled), "en Aguas estancadas Morpho no está (04 §2.3)");
            Assert.That(rig.Flow.PressHint(), Is.False);

            int cuias = rig.Flow.Session.Current.Count;
            Assert.That(rig.Flow.BuyRescue()!.IsApplied, Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "la partida sigue");
            Assert.That(rig.Flow.Session.Status, Is.EqualTo(LevelStatus.Playing));
            BoardState rescued = rig.Flow.Session.Current;
            Assert.That(rescued.Count, Is.EqualTo(cuias + 1));
            int seeds = rig.Economy.Wallet.Seeds;
            Assert.That(seeds, Is.EqualTo(700), "solo la Cuia de rescate (300)");

            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Ready));
            Assert.That(rig.Flow.PressHint(), Is.True);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(seeds), "pedirla no cobra");
            HintResult r = WaitAnswer(rig);
            Assert.That(rig.Flow.Session.Current.Equals(rescued), Is.True, "la consulta fue sobre el tablero con la cuia de rescate");
            Assert.That(r.Answer, Is.EqualTo(HintAnswer.Pour), "04 §2.3 c: con la cuia añadida");
            Assert.That(PourRules.IsLegal(rescued, r.Move.Source, r.Move.Target), Is.True);
            BoardState after = PourRules.Apply(rescued, r.Move.Source, r.Move.Target, PourMode.Run);
            Assert.That(new AStarSolver().Solve(after, SolverOptions.ForPipeline(PourMode.Run, after)).Status, Is.EqualTo(SolveStatus.Solved),
                "el vertido propuesto deja resoluble el tablero con la cuia añadida");
            Assert.That(r.Charged, Is.True, "cobra solo al dar el vertido");
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(seeds - 100));
        }

        [Test]
        public void Flow_HintSupportedFalse_IsUnsupported()
        {
            Rig rig = NewRig(500, null, LevelPackReader.Read(PlainPack(false)));
            rig.Start(CampaignLevel);
            Assert.That(rig.Flow.Level.HintSupported, Is.False);
            Assert.That(rig.Flow.Hint.State, Is.EqualTo(HintState.Unsupported), "«La morpho no conoce este tramo todavía»");
            Assert.That(rig.Flow.PressHint(), Is.False);

            Rig ok = NewRig(500, null, LevelPackReader.Read(PlainPack(true)));
            ok.Start(CampaignLevel);
            Assert.That(ok.Flow.Hint.State, Is.EqualTo(HintState.Ready));
        }

        // --- Ayudas ---

        private static HintResult WaitDone(MorphoBooster morpho)
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                MorphoPoll poll = morpho.Poll(out HintResult result);
                if (poll == MorphoPoll.Done)
                {
                    return result;
                }

                Assert.That(poll, Is.EqualTo(MorphoPoll.Running));
                Assert.That(watch.ElapsedMilliseconds, Is.LessThan(WaitLimitMs), "Morpho no respondió");
                Thread.Sleep(1);
            }
        }

        private static HintResult WaitAnswer(Rig rig)
        {
            var watch = Stopwatch.StartNew();
            while (rig.Flow.HintThinking)
            {
                Assert.That(watch.ElapsedMilliseconds, Is.LessThan(WaitLimitMs), "Morpho no respondió");
                Thread.Sleep(1);
                rig.Flow.Tick(16, true);
            }

            Assert.That(rig.Flow.LastHint, Is.Not.Null);
            Assert.That(rig.Hints.Answers.Count, Is.GreaterThan(0));
            return rig.Hints.Answers[rig.Hints.Answers.Count - 1];
        }

        // Un vertido legal distinto de la pista.
        private static Pour FindOtherPour(BoardState state, Pour hint)
        {
            for (int a = 0; a < state.Count; a++)
            {
                for (int b = 0; b < state.Count; b++)
                {
                    if (a != b && !(a == hint.Source && b == hint.Target) && PourRules.IsLegal(state, a, b))
                    {
                        return new Pour(a, b, 1);
                    }
                }
            }

            Assert.Fail("sin otro vertido legal");
            return default;
        }

        private static Rig NewRig(int seeds, BoosterConfig? config = null, LevelPack? pack = null, int offset = 0, int maxLevel = 0)
        {
            return new Rig(seeds, config, pack, offset, maxLevel);
        }

        // Nivel 15 de Aguas turbias válido en campaña (U_c = 4, T3: la turbia con 3 dosis y la de arriba a la vista): dos
        // realidades, alguna con solución y ningún vertido seguro en las dos (buscado con SafeMoves y las opciones de Morpho).
        private static string MurkyPack()
        {
            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_morpho_murky\", \"kind\": \"campaign\", \"world\": 1, "
                + "\"world_id\": \"w01_estuario\", \"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m01_v1_n0015\", "
                + "\"level\": 15, \"world\": 1, \"lap\": 1, \"water_base\": \"white\", \"palette\": [\"guanabana\", \"cupuacu\", \"tapereba\"], "
                + "\"units_per_color\": 4, \"caps\": [4, 4, 4, 4], \"cuias\": [[1], [3, 2, 2, 3], [3, 2, 1, 1], [2, 3, 1]], "
                + "\"attrs\": [null, null, null, null], \"murky\": true, \"hidden\": [[3, 0], [3, 1]], \"difficulty\": { \"par\": 4 }, "
                + "\"relief\": [] } ] }";
        }

        // Nivel 15 sin ocultas, con hint_supported explícito.
        private static string PlainPack(bool hintSupported)
        {
            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_morpho_plain\", \"kind\": \"campaign\", \"world\": 1, "
                + "\"world_id\": \"w01_estuario\", \"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m01_v1_n0015\", "
                + "\"level\": 15, \"world\": 1, \"lap\": 1, \"water_base\": \"white\", \"palette\": [\"guanabana\", \"cupuacu\"], "
                + "\"units_per_color\": 4, \"caps\": [4, 4, 4], \"cuias\": [[1, 1, 1, 2], [2, 2, 2, 1], []], "
                + "\"attrs\": [null, null, null], \"murky\": false, \"hidden\": [], \"hint_supported\": " + (hintSupported ? "true" : "false")
                + ", \"difficulty\": { \"par\": 2 }, \"relief\": [] } ] }";
        }

        private sealed class Rig
        {
            public Rig(int seeds, BoosterConfig? config, LevelPack? pack, int offset, int maxLevel)
            {
                Economy = new EconomyFixture(seeds);
                Hints = new HintRecorder();
                Probe = new ProbeRecorder();
                IReadOnlyList<LevelPack> packs = pack == null ? TestPacks.Campaign : new[] { pack };
                Flow = new LevelFlow(LevelCatalog.Create(packs, offset), new MemoryProgressStore(0, maxLevel), Hints, GameConfig.TapSwitchSelection, PourModes.Default, Economy.Wallet, config)
                {
                    Clock = Economy.Clock,
                    HintProbe = Probe,
                };
            }

            public EconomyFixture Economy { get; }

            public HintRecorder Hints { get; }

            public ProbeRecorder Probe { get; }

            public LevelFlow Flow { get; }

            public void Start(int level)
            {
                Flow.Load(level);
                Assert.That(Flow.TapAnywhere(), Is.True);
                Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            }
        }

        private sealed class ProbeRecorder : IHintProbe
        {
            public int Lines { get; private set; }

            public int TimeoutMs { get; private set; }

            public void Morpho(in HintResult result, int level, int waitMs, int timeoutMs)
            {
                Lines++;
                TimeoutMs = timeoutMs;
            }
        }

        private sealed class HintRecorder : ILevelFlowListener, IHintListener
        {
            public readonly List<bool> Thinking = new List<bool>();

            public readonly List<HintResult> Answers = new List<HintResult>();

            public int Cleared { get; private set; }

            public void OnHintThinking(bool thinking)
            {
                Thinking.Add(thinking);
            }

            public void OnHint(in HintResult result)
            {
                Answers.Add(result);
            }

            public void OnHintCleared()
            {
                Cleared++;
            }

            public int Gifted { get; private set; }

            public int GiftCalls { get; private set; }

            public void OnHintGifted(int units)
            {
                Gifted += units;
                GiftCalls++;
            }

            public void OnLevelLoaded(int level)
            {
            }

            public void OnIntroStarted(int durationMs, bool isShort)
            {
            }

            public void OnIntroFinished(bool skipped)
            {
            }

            public void OnTap(TapResult result)
            {
            }

            public void OnUndo(Pour undone, bool fromStuckModal)
            {
            }

            public void OnRestart(bool fromStuckModal)
            {
            }

            public void OnWin()
            {
            }

            public void OnStuck()
            {
            }

            public void OnCelebrationStarted()
            {
            }

            public void OnVictoryScreen()
            {
            }

            public void OnStuckModalOpened()
            {
            }

            public void OnCuiaAdded(int index, AddedCuiaKind kind)
            {
            }

            public void OnExtraCuiaGifted(int units)
            {
            }

            public void OnFunnelPhase(FlowPhase phase)
            {
            }

            public void OnFailResolved(FailOutcome outcome)
            {
            }

            public void OnRiverLowered(string reliefType, int bannerMs)
            {
            }

            public void OnHarvestDoubled(int totalSeeds)
            {
            }

            public void OnAdGate()
            {
            }

            public void OnResumed(FlowPhase phase)
            {
            }
        }
    }
}
