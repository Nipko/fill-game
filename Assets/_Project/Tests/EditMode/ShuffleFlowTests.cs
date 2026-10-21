#nullable enable
using System.Collections.Generic;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Pororoca · Barajar en <see cref="LevelFlow"/> (bloque B2; 04 §2.4; 02 §14; 02 R8): la ola en el nivel 30 de la campaña,
    /// aplicar → guardar → cobrar (stock primero, 300 semillas), los topes de 1 por nivel y 3 al día (<c>daily.shuffles_today</c>),
    /// el historial de Marea vacío, Volver a la orilla al tablero del nivel [P], la ola que falla sin cobrar, Lluvia y Piraña,
    /// la tarjeta del 30 y el guardado con la marca de la ola, que al reanudar da el mismo tablero.
    /// </summary>
    public sealed class ShuffleFlowTests
    {
        private static LevelCatalog? _catalog;

        private static LevelCatalog Catalog => _catalog ??= LevelCatalog.Create(TestPacks.Campaign);

        private sealed class Fixture
        {
            public Fixture(int seeds = 1000, int maxLevel = 29, EconomyFixture? economy = null)
            {
                Economy = economy ?? new EconomyFixture(seeds);
                Listener = new ShuffleListener();
                Progress = new MemoryProgressStore(maxLevel + 1, maxLevel);
                States = new MemoryLevelStateStore();
                Flow = new LevelFlow(Catalog, Progress, Listener, GameConfig.TapSwitchSelection, PourModes.Default, Economy.Wallet)
                {
                    StateStore = States,
                };
            }

            public EconomyFixture Economy { get; }

            public ShuffleListener Listener { get; }

            public MemoryProgressStore Progress { get; }

            public MemoryLevelStateStore States { get; }

            public LevelFlow Flow { get; }

            public void Play(int level)
            {
                Flow.Load(level);
                Flow.TapAnywhere();
                Flow.Tick(Flow.IntroDurationMs, true);
                Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            }

            public void PourFirstUseful()
            {
                var buffer = new Pour[PourRules.MaxPours];
                int n = PourRules.CollectUsefulMoves(Flow.Session.Current, true, buffer);
                Assert.That(n, Is.GreaterThan(0));
                Assert.That(Flow.TapCuia(buffer[0].Source).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(Flow.TapCuia(buffer[0].Target).Kind, Is.EqualTo(TapKind.Poured));
                Flow.Tick(16, true);
            }
        }

        private sealed class ShuffleListener : ILevelFlowListener, IShuffleListener
        {
            public readonly List<string> Events = new List<string>();

            public void OnShuffled(ShuffleOutcome outcome, ShufflePayment payment) => Events.Add("shuffled:" + outcome.MethodId + ":" + ShuffleBooster.SourceId(payment));

            public void OnShuffleFailed(ShuffleOutcome outcome) => Events.Add("shuffle_failed");

            public void OnLevelLoaded(int level) => Events.Add("loaded:" + level);

            public void OnIntroStarted(int durationMs, bool isShort) => Events.Add("intro");

            public void OnIntroFinished(bool skipped) => Events.Add("intro_end");

            public void OnTap(TapResult result)
            {
            }

            public void OnUndo(Pour undone, bool fromStuckModal) => Events.Add("undo");

            public void OnRestart(bool fromStuckModal) => Events.Add("restart");

            public void OnWin() => Events.Add("win");

            public void OnStuck() => Events.Add("stuck");

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

            public void OnResumed(FlowPhase phase) => Events.Add("resumed");
        }

        [Test]
        public void Level30_ShufflesTheBoard_PaysWithSeeds_AndEmptiesTheUndoHistory_04_2_4()
        {
            var f = new Fixture();
            f.Play(30);
            f.PourFirstUseful();
            BoardState before = f.Flow.Session.Current;
            Assert.That(f.Flow.Shuffle.State, Is.EqualTo(ShuffleState.Ready));
            Assert.That(f.Flow.Shuffle.NeedsConfirmation, Is.True);

            ShuffleOutcome? outcome = f.Flow.UseShuffle();

            Assert.That(outcome.HasValue && outcome.Value.Succeeded, Is.True);
            Assert.That(f.Flow.Session.Current.Equals(before), Is.False);
            Assert.That(f.Flow.Session.Current.Equals(outcome!.Value.Board), Is.True);
            Assert.That(f.Flow.Session.CanUndo, Is.False, "02 §14: vacía el historial de Marea");
            Assert.That(f.Economy.Wallet.Seeds, Is.EqualTo(700));
            Assert.That(f.Economy.Wallet.ShufflesToday, Is.EqualTo(1));
            Assert.That(f.Economy.Profile.Daily.ShufflesToday, Is.EqualTo(1));
            Assert.That(f.Flow.ShufflesUsed, Is.EqualTo(1));
            Assert.That(f.Flow.Shuffle.State, Is.EqualTo(ShuffleState.UsedLevel));
            Assert.That(f.Listener.Events, Does.Contain("shuffled:" + outcome.Value.MethodId + ":seeds"));
            Assert.That(f.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        [Test]
        public void Stock_IsSpentFirst_AndCountsForTheDay_04_2_4()
        {
            var f = new Fixture();
            f.Economy.Wallet.Apply(Transaction.GrantStock(BoosterKind.Shuffle, 1));
            f.Play(30);

            Assert.That(f.Flow.Shuffle.NeedsConfirmation, Is.False);
            Assert.That(f.Flow.UseShuffle()!.Value.Succeeded, Is.True);

            Assert.That(f.Economy.Wallet.Stock.Get(BoosterKind.Shuffle), Is.EqualTo(0));
            Assert.That(f.Economy.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(f.Economy.Wallet.ShufflesToday, Is.EqualTo(1));
            Assert.That(f.Listener.Events[f.Listener.Events.Count - 1], Does.EndWith(":stock"));
        }

        [Test]
        public void ThreePerDay_AlsoAcrossLevels_04_2_4()
        {
            var f = new Fixture(seeds: 5000);
            f.Economy.Profile.Daily.DateLocal = f.Economy.Clock.LocalDateKey();
            f.Economy.Profile.Daily.ShufflesToday = 3;
            f.Play(30);

            Assert.That(f.Flow.Shuffle.State, Is.EqualTo(ShuffleState.UsedToday));
            Assert.That(f.Flow.UseShuffle(), Is.Null);
            Assert.That(f.Economy.Wallet.Seeds, Is.EqualTo(5000));
        }

        [Test]
        public void BeforeLevel30_TheSlotDoesNotExist_AndDuringTheIntroItIsGrey_04_2_4()
        {
            var f = new Fixture(maxLevel: 28);
            f.Play(29);
            Assert.That(f.Flow.Shuffle.State, Is.EqualTo(ShuffleState.Hidden));

            f.Flow.Load(30);
            Assert.That(f.Flow.Shuffle.State, Is.EqualTo(ShuffleState.Disabled), "fuera de PLAYING");
            Assert.That(f.Flow.UseShuffle(), Is.Null);
        }

        [Test]
        public void Restart_GoesBackToTheLevelBoard_AndTheLevelCapStaysUsed_P5()
        {
            var f = new Fixture();
            f.Play(30);
            BoardState level = f.Flow.Session.Initial;
            Assert.That(f.Flow.UseShuffle()!.Value.Succeeded, Is.True);

            Assert.That(f.Flow.PressRestart(), Is.True);
            f.Flow.Tick(f.Flow.IntroDurationMs, true);

            Assert.That(f.Flow.Session.Current.Equals(level), Is.True);
            Assert.That(f.Flow.Shuffle.State, Is.EqualTo(ShuffleState.UsedLevel));
        }

        [Test]
        public void WhenTheWaveFails_NothingIsCharged_AndTheBoardStays_04_2_4()
        {
            var f = new Fixture();
            f.Flow.ShuffleConfig = new ShuffleConfig { ShuffleBudgetNodes = 1 };
            f.Play(30);
            BoardState before = f.Flow.Session.Current;

            ShuffleOutcome? outcome = f.Flow.UseShuffle();

            Assert.That(outcome!.Value.Method, Is.EqualTo(ShuffleWaveMethod.Failed));
            Assert.That(outcome.Value.MethodId, Is.EqualTo("failed"));
            Assert.That(f.Flow.Session.Current, Is.SameAs(before));
            Assert.That(f.Economy.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(f.Economy.Wallet.ShufflesToday, Is.EqualTo(0));
            Assert.That(f.Flow.ShufflesUsed, Is.EqualTo(0));
            Assert.That(f.Listener.Events, Does.Contain("shuffle_failed"));
        }

        [Test]
        public void LluviaLevel_DisablesTheWave_04_2_4_d()
        {
            var f = new Fixture(maxLevel: 142);
            f.Play(143); // estreno de la Lluvia (02 §9.1)

            Assert.That(ShuffleWave.HasTemporalMechanic(f.Flow.Session.Initial), Is.True);
            Assert.That(f.Flow.Shuffle.State, Is.EqualTo(ShuffleState.Unsupported));
            Assert.That(f.Flow.Shuffle.DeniedReason, Is.EqualTo("unsupported"));
        }

        [Test]
        public void Card_IsDueOnTheFirstLevel30_Once_04_4()
        {
            var f = new Fixture();
            f.Play(30);
            Assert.That(f.Flow.ShuffleCardDue, Is.True);

            f.Flow.MarkShuffleCardShown();
            Assert.That(f.Flow.ShuffleCardDue, Is.False);
            Assert.That(f.States.Load()!.History, Does.Contain("p"));

            var replay = new Fixture(maxLevel: 30);
            replay.Play(30);
            Assert.That(replay.Flow.ShuffleCardDue, Is.False, "ya superado");
        }

        [Test]
        public void SavedLevel_WithAShuffle_ResumesToTheSameBoard_02_R8()
        {
            var f = new Fixture();
            f.Play(30);
            f.PourFirstUseful();
            Assert.That(f.Flow.UseShuffle()!.Value.Succeeded, Is.True);
            f.PourFirstUseful();
            BoardState expected = f.Flow.Session.Current;
            LevelStateData saved = f.States.Load()!;
            Assert.That(saved.History, Does.Contain("*"));
            Assert.That(LevelStateSnapshot.TryParseHistory(saved.History, out SavedHistory history), Is.True);
            Assert.That(history.ShufflesUsed, Is.EqualTo(1));
            Assert.That(history.ShuffleAt.Count, Is.EqualTo(1));
            Assert.That(history.ShuffleAt[0], Is.EqualTo(1));
            Assert.That(history.Sources.Count, Is.EqualTo(2));

            var again = new Fixture(economy: f.Economy);
            Assert.That(again.Flow.Resume(saved), Is.True);

            Assert.That(again.Flow.Session.Current.Equals(expected), Is.True);
            Assert.That(again.Flow.ShufflesUsed, Is.EqualTo(1));
            Assert.That(again.Flow.Shuffle.State, Is.EqualTo(ShuffleState.UsedLevel));
            Assert.That(again.Flow.Session.AppliedPours, Is.EqualTo(1), "Marea solo llega a lo de después de la ola");

            // Lo que se vuelve a guardar es lo mismo: los vertidos de antes de la ola, su marca y los de después.
            again.PourFirstUseful();
            Assert.That(again.States.Load()!.History, Does.StartWith(saved.History.Substring(0, saved.History.IndexOf('*') + 1)));
        }

        [Test]
        public void MurkyLevel_Firefly_Shuffle_Resume_KeepsTheBoardAndTheRevealed_02_R8_T6()
        {
            // Nivel 100 (estreno de Aguas turbias, con la Luciérnaga regalada): Luciérnaga → vertido → Pororoca (revela el
            // resto) → vertido; al reanudar, el mismo tablero, sin ocultas, y la sección R de h2 se restaura sin romperse.
            var f = new Fixture(maxLevel: 99);
            f.Play(100);
            BoardState start = f.Flow.Session.Current;
            int murky = -1;
            for (int i = 0; i < start.Count && murky < 0; i++)
            {
                murky = start[i].HiddenMask != 0 ? i : -1;
            }

            Assert.That(murky, Is.GreaterThanOrEqualTo(0), "el 100 lleva una cuia turbia");
            Assert.That(f.Flow.BeginRevealSelection(RevealPayment.Stock), Is.True);
            f.Flow.TapCuia(murky);
            Assert.That(f.Flow.Session.RevealedLayers(murky), Is.Not.EqualTo(0));
            f.PourFirstUseful();
            Assert.That(f.Flow.UseShuffle()!.Value.Succeeded, Is.True);
            Assert.That(f.Flow.Session.HiddenCount, Is.EqualTo(0), "04 §2.4 a: la ola revela todo");
            f.PourFirstUseful();
            BoardState expected = f.Flow.Session.Current;
            LevelStateData saved = f.States.Load()!;

            var again = new Fixture(maxLevel: 99, economy: f.Economy);
            Assert.That(again.Flow.Resume(saved), Is.True);

            Assert.That(again.Flow.Session.Current.Equals(expected), Is.True);
            Assert.That(again.Flow.Session.HiddenCount, Is.EqualTo(0));
            Assert.That(again.Flow.Session.RevealedLayers(murky), Is.EqualTo(f.Flow.Session.RevealedLayers(murky)));
        }

        [Test]
        public void ExitedLevel_KeepsTheLevelCap_P4()
        {
            var f = new Fixture();
            f.Play(30);
            Assert.That(f.Flow.UseShuffle()!.Value.Succeeded, Is.True);
            f.Flow.LeaveLevel();
            LevelStateData saved = f.States.Load()!;

            var again = new Fixture(economy: f.Economy);
            Assert.That(again.Flow.RestoreExited(saved), Is.True);
            again.Play(30);

            Assert.That(again.Flow.ShufflesUsed, Is.EqualTo(1));
            Assert.That(again.Flow.Shuffle.State, Is.EqualTo(ShuffleState.UsedLevel));
        }

        [Test]
        public void History_WithoutShuffleMarks_ReadsAsBefore()
        {
            Assert.That(LevelStateSnapshot.TryParseHistory("h2|P||c|0102|", out SavedHistory plain), Is.True);
            Assert.That(plain.ShufflesUsed, Is.EqualTo(0));
            Assert.That(plain.ShuffleAt.Count, Is.EqualTo(0));
            Assert.That(LevelStateSnapshot.TryParseHistory("h2|P||oo|01*02|", out SavedHistory two), Is.True);
            Assert.That(two.ShufflesUsed, Is.EqualTo(2));
            Assert.That(two.ShuffleAt[0], Is.EqualTo(1));
            Assert.That(LevelStateSnapshot.TryParseHistory("h2|P|||01*02|", out _), Is.False, "una ola sin su marca o");
            Assert.That(LevelStateSnapshot.TryParseHistory("h2|P||o|0*102|", out _), Is.False, "la marca parte un par");
        }
    }
}
