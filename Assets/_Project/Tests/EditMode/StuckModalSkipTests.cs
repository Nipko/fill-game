#nullable enable
using AF.Boosters;
using AF.GameFlow;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Rama D de Aguas estancadas (bloque B3; 04 §6.2 <c>if ctx.skip_eligible: opts.add(SKIP(price=300))</c> y §6.3): el modelo
    /// del modal lleva el Salto del Boto del contexto, solo desde el 31, también en las ramas A' y F («Salto si elegible»), y el
    /// flujo lo pone en el contexto al quinto fallo. Necesita <c>B3-model.diff</c>.
    /// </summary>
    public sealed class StuckModalSkipTests
    {
        private static readonly BoosterConfig Config = BoosterConfig.Defaults;

        private static StuckContext Ctx(int level, SkipOffer skip, RescueCheck check = RescueCheck.Solvable, int rescuesLevel = 0)
        {
            return new StuckContext
            {
                Level = level,
                RescueMinLevel = 31,
                UndoFree = 3,
                Seeds = 1000,
                UndoRefillPrice = 30,
                UndoRefillAmount = 3,
                Canoes = 5,
                RescueMaxPerLevel = 1,
                RescuesLevel = rescuesLevel,
                RescueCheck = check,
                RescuePrice = 300,
                RescueBasePrice = 600,
                Geo = GeoTier.T3,
                Skip = skip,
            };
        }

        [Test]
        public void BranchD_CarriesTheSkipOfTheContext()
        {
            StuckModalView view = StuckModalModel.Build(Ctx(40, new SkipOffer(SkipState.Ready, 300, 0, 40)), Config);
            Assert.That(view.Skip.State, Is.EqualTo(SkipState.Ready));
            Assert.That(view.Skip.Price, Is.EqualTo(300));
            Assert.That(view.Rescue, Is.EqualTo(StuckRescueKind.Available), "el rescate sigue antes (04 §6.2)");
            Assert.That(view.Footer, Is.EqualTo(StuckFooter.GiveUp));
        }

        [Test]
        public void WithoutSeeds_TheSkipIsGrey()
        {
            StuckModalView view = StuckModalModel.Build(Ctx(40, new SkipOffer(SkipState.Disabled, 300, 180, 40)), Config);
            Assert.That(view.Skip.State, Is.EqualTo(SkipState.Disabled));
            Assert.That(view.Skip.Missing, Is.EqualTo(180));
        }

        [Test]
        public void BranchesAPrimeAndF_StillOfferTheSkip()
        {
            var ready = new SkipOffer(SkipState.Ready, 300, 0, 40);
            StuckModalView second = StuckModalModel.Build(Ctx(40, ready, rescuesLevel: 1), Config);
            Assert.That(second.Rescue, Is.EqualTo(StuckRescueKind.None), "rama A'");
            Assert.That(second.Skip.CanUse, Is.True, "«Salto si elegible»");
            StuckModalView f = StuckModalModel.Build(Ctx(40, ready, RescueCheck.Unsolvable), Config);
            Assert.That(f.Unsolvable, Is.True, "rama F");
            Assert.That(f.Skip.CanUse, Is.True, "«Salto si elegible»");
        }

        [Test]
        public void Before31_ThereIsNoSkip()
        {
            StuckModalView view = StuckModalModel.Build(Ctx(30, new SkipOffer(SkipState.Ready, 300, 0, 30)), Config);
            Assert.That(view.Footer, Is.EqualTo(StuckFooter.RestartFree), "rama C");
            Assert.That(view.Skip.State, Is.EqualTo(SkipState.Hidden));
        }

        [Test]
        public void TheFlow_PutsTheSkipInTheContext_OnTheFifthFail()
        {
            var rig = new SkipFlowTests.ProfileRig(new[] { TestPacks.Tutorial }, 30, seeds: 1000);
            rig.Wallet.Apply(AF.Economy.Transaction.InfiniteCanoesFor(System.TimeSpan.FromHours(1)));
            LevelFlow flow = rig.Flow;
            flow.Load(36);
            Assert.That(flow.TapAnywhere(), Is.True);
            for (int fail = 0; fail < 5; fail++)
            {
                LevelStateTests.GetStuck(flow);
                Assert.That(StuckModalModel.Build(flow.BuildStuckContext(false, true, GeoTier.T3), flow.Boosters).Skip.Offered, Is.False);
                Assert.That(flow.PressGiveUp(), Is.True);
                Assert.That(flow.PressRetry(), Is.True);
                Assert.That(flow.TapAnywhere(), Is.True);
            }

            LevelStateTests.GetStuck(flow);
            StuckModalView view = StuckModalModel.Build(flow.BuildStuckContext(false, true, GeoTier.T3), flow.Boosters);
            Assert.That(view.Skip.State, Is.EqualTo(SkipState.Ready), "rama D al quinto fallo");
        }
    }
}
