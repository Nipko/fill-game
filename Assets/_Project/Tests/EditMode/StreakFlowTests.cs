#nullable enable
using System;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La Crecida, la Hoja escudo y la Canasta en el flujo del nivel sin el parche <c>S2-flow.diff</c> (B1a, A6;
    /// <c>LevelFlow.Streak.cs</c>; nota <c>docs/dev/crecida-canasta.md</c> E32 y E34; 04 §2.6; K8-K9): el punto de inyección
    /// <see cref="LevelFlow.UseStreak"/>, el flujo sin servicio, el Salto del Boto en un <c>10k</c> (paga la Canasta y no toca la
    /// Crecida) y el Reto del río, que no cuenta. Los tests de la victoria y del fallo duro necesitan las llamadas del parche y van
    /// dentro de él (<c>StreakFlowHookTests.cs</c>).
    /// </summary>
    public sealed class StreakFlowTests
    {
        [Test]
        public void WithoutService_NothingHappens_AndNothingBreaks()
        {
            var rig = new StreakFlowRig(offset: 10, inject: false);
            rig.Start(20);
            Assert.That(rig.Flow.Streak, Is.Null);
            Assert.That(rig.Flow.PeekStreak(), Is.Null);
            Assert.That(rig.Flow.OnSkipStreak(), Is.Null);
            int seeds = rig.Economy.Wallet.Seeds;
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.LastStreakResult, Is.Null);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(seeds + rig.Flow.HarvestBase), "solo la Cosecha");
        }

        [Test]
        public void UseStreak_InjectsAndRemovesTheService()
        {
            var rig = new StreakFlowRig(offset: 22);
            Assert.That(rig.Flow.Streak, Is.SameAs(rig.Service));
            rig.Start(25);
            StreakSnapshot? peek = rig.Flow.PeekStreak();
            Assert.That(peek.HasValue, Is.True);
            Assert.That(peek!.Value.Active, Is.True, "campaña desde streak_unlock_level (25): hay barra");
            Assert.That(peek.Value.Wins, Is.EqualTo(0));

            rig.Flow.UseStreak(null);
            Assert.That(rig.Flow.Streak, Is.Null);
            Assert.That(rig.Flow.PeekStreak(), Is.Null);
        }

        [Test]
        public void PeekStreak_BeforeUnlock_IsNotActive()
        {
            var rig = new StreakFlowRig(offset: 10);
            rig.Start(20);
            Assert.That(rig.Flow.PeekStreak()!.Value.Active, Is.False, "antes del 25 no hay barra (C1)");
        }

        [Test]
        public void Skip_On10k_PaysTheBasket_AndLeavesTheStreakAlone()
        {
            // K9, E34: el Salto del Boto en el 20 paga su Canasta (50 semillas + 3 Mareas: eco_basket_cycle[0] = undo3) con
            // source = basket; la Crecida ni avanza ni se rompe (C11) y el almacén no se toca.
            var rig = new StreakFlowRig(offset: 10, seeds: 7);
            rig.Start(20);
            int undo = rig.Economy.Wallet.Stock.Get(BoosterKind.Undo);
            StreakServiceResult? result = rig.Flow.OnSkipStreak();
            Assert.That(result, Is.Not.Null);
            Assert.That(rig.Flow.LastStreakResult, Is.SameAs(result));
            Assert.That(result!.Has(StreakServiceEvents.BasketOpened), Is.True);
            Assert.That(result.LevelBasket.Level, Is.EqualTo(20));
            Assert.That(result.BasketSeeds, Is.EqualTo(50));
            Assert.That(result.Streak.Counted, Is.False, "el Salto no toca la Crecida");
            Assert.That(result.AllApplied, Is.True);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(57));
            Assert.That(rig.Economy.Wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(undo + 3));
            Assert.That(rig.Store.WriteCount, Is.EqualTo(0), "no lee ni guarda la Crecida");
        }

        [Test]
        public void Skip_OnALevelThatIsNot10k_PaysNothing()
        {
            var rig = new StreakFlowRig(offset: 10, seeds: 7);
            rig.Start(15);
            StreakServiceResult? result = rig.Flow.OnSkipStreak();
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.LevelBasket.IsNone, Is.True);
            Assert.That(result.Transactions.Count, Is.EqualTo(0));
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(7));
        }

        [Test]
        public void Skip_OnA10kAlreadyPassed_DoesNotPayTwice()
        {
            // K8: la Canasta del 20 ya se pagó al superarlo; volver a él (vía de desarrollo) y saltarlo no la repite.
            var rig = new StreakFlowRig(offset: 10, maxLevel: 20, seeds: 7);
            rig.Start(20);
            Assert.That(rig.Flow.OnSkipStreak(), Is.Null);
            Assert.That(rig.Flow.LastStreakResult, Is.Null);
            Assert.That(rig.Economy.Wallet.Seeds, Is.EqualTo(7));
        }

        [Test]
        public void Skip_InsideTheSkipBatch_PaysTheBasket_AndTheProgressInOneWrite()
        {
            // Contrato de OnSkipStreak para B3: dentro del mismo using (Saves()) en el que se guarda el salto y antes de
            // guardarlo. La Canasta (cartera) y el progreso van al mismo perfil por el mismo ProfileSaveBatch: una escritura.
            // El guardado del salto aún no existe (B3): aquí lo representa SaveWin(20, 21).
            var clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            profile.Wallet.Seeds = 7;
            profile.Progress.MaxLevel = 19;
            profile.Progress.NextLevel = 20;
            var disk = new MemoryProfileStore();
            var batch = new ProfileSaveBatch(disk);
            var wallet = new Wallet(profile, batch, clock);
            var progress = new ProfileProgressStore(profile, batch);
            var flow = new LevelFlow(
                LevelCatalog.Create(new[] { TestPacks.Tutorial }, 10), progress, new RecordingListener(), GameConfig.TapSwitchSelection,
                PourModes.Default, wallet)
            {
                SaveBatch = batch,
            };
            flow.UseStreak(new StreakService(new InMemoryStreakStore(clock), wallet, clock, 10));
            flow.Load(20);
            flow.TapAnywhere();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));

            int writes = batch.WriteCount;
            StreakServiceResult? result;
            using (batch.Begin())
            {
                result = flow.OnSkipStreak();
                progress.SaveWin(20, 21);
                Assert.That(batch.WriteCount, Is.EqualTo(writes), "nada sale antes de cerrar el lote del salto");
            }

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.BasketSeeds, Is.EqualTo(50));
            Assert.That(batch.WriteCount, Is.EqualTo(writes + 1), "la Canasta y el progreso, en una sola escritura");
            Profile saved = disk.Load().Profile!;
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(57));
            Assert.That(saved.Progress.MaxLevel, Is.EqualTo(20));
        }

        [Test]
        public void TheDaily_HasNoBarAndNoSkip()
        {
            // 06 §4.3 («si not nivel.es_campana: return»), Q-C2 (a), 04 §2.6 e: en el Reto ni barra ni Salto ni Canasta, también
            // con el nivel efectivo en un 10k desde el 25 (max_level 29 → 30).
            var daily = new StreakDailyRig(maxLevel: 29);
            daily.Flow.StartDaily();
            Assert.That(daily.Flow.LevelNumber, Is.EqualTo(30));
            StreakSnapshot? peek = daily.Flow.PeekStreak();
            Assert.That(peek.HasValue, Is.True);
            Assert.That(peek!.Value.Active, Is.False, "sin barra en el Reto (E25)");
            Assert.That(daily.Flow.OnSkipStreak(), Is.Null);
            Assert.That(daily.Store.WriteCount, Is.EqualTo(0));
        }
    }

    /// <summary>Flujo de campaña con el tutorial desplazado (<c>af_level_offset</c>), la cartera y un <see cref="StreakService"/> en memoria.</summary>
    internal sealed class StreakFlowRig
    {
        public StreakFlowRig(
            int offset, int maxLevel = 0, StreakRecord? initial = null, int seeds = 0, bool inject = true, LevelPack? pack = null)
        {
            Economy = new EconomyFixture(seeds);
            Store = new InMemoryStreakStore(Economy.Clock, initial);
            Service = new StreakService(Store, Economy.Wallet, Economy.Clock, 10);
            Progress = new MemoryProgressStore(maxLevel + 1, maxLevel);
            Listener = new RecordingListener();
            Flow = new LevelFlow(
                LevelCatalog.Create(new[] { pack ?? TestPacks.Tutorial }, offset), Progress, Listener, GameConfig.TapSwitchSelection, PourModes.Default,
                Economy.Wallet);
            if (inject)
            {
                Flow.UseStreak(Service);
            }
        }

        public EconomyFixture Economy { get; }

        public InMemoryStreakStore Store { get; }

        public StreakService Service { get; }

        public MemoryProgressStore Progress { get; }

        public RecordingListener Listener { get; }

        public LevelFlow Flow { get; }

        /// <summary>Día local del reloj de la cartera (<c>yyyy-MM-dd</c>).</summary>
        public string Today => Economy.Clock.LocalDateKey();

        public void Start(int level)
        {
            Flow.Load(level);
            Assert.That(Flow.TapAnywhere(), Is.True);
            Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        public void WinAndShowVictory()
        {
            LevelStateTests.Win(Flow);
            Flow.Tick(16, true);
            Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            Assert.That(Flow.TapAnywhere(), Is.True);
            Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }
    }

    /// <summary>Partida del Reto del río con la cartera de campaña y la del Reto sobre el mismo perfil y un <see cref="StreakService"/>.</summary>
    internal sealed class StreakDailyRig
    {
        public StreakDailyRig(int maxLevel, LevelDefinition? board = null, StreakRecord? initial = null)
        {
            Clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
            Profile = Wallet.NewProfile(Clock);
            Profile.Wallet.Seeds = 500;
            Profile.Progress.MaxLevel = maxLevel;
            Profile.Progress.NextLevel = maxLevel + 1;
            ProfileStore = new MemoryProfileStore();
            CampaignWallet = new Wallet(Profile, ProfileStore, Clock);
            Progress = new ProfileProgressStore(Profile, ProfileStore);
            DailyWallet = LevelFlow.CreateDailyWallet(Profile, ProfileStore, Clock);
            Store = new InMemoryStreakStore(Clock, initial);
            Service = new StreakService(Store, CampaignWallet, Clock, 10);
            LevelDefinition b = board ?? DailyPoolReader.Read(DailyPoolTestPacks.Pool(DailyPoolTestPacks.Boards(1)), DailyPoolCoverage.PresentBands).Find(1, 0)!;
            Flow = LevelFlow.CreateDaily(b, LevelFlow.DailyEffectiveLevel(maxLevel), Progress, new RecordingListener(), DailyWallet);
            Flow.StateStore = new MemoryLevelStateStore();
            Flow.Clock = Clock;
            Flow.UseStreak(Service);
        }

        public ManualClock Clock { get; }

        public Profile Profile { get; }

        public MemoryProfileStore ProfileStore { get; }

        public Wallet CampaignWallet { get; }

        public Wallet DailyWallet { get; }

        public ProfileProgressStore Progress { get; }

        public InMemoryStreakStore Store { get; }

        public StreakService Service { get; }

        public LevelFlow Flow { get; }
    }
}
