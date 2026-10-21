#nullable enable
using System;
using System.Collections.Generic;
using AF.Boosters;
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
    /// El Salto del Boto en el flujo con el perfil de verdad (bloque B3; 04 §2.6, §5.2 y §6.2 <c>SKIP</c>; 02 §2 SKIP_RESOLVE;
    /// E34 y K9 de <c>docs/dev/crecida-canasta.md</c>; nota <c>docs/dev/salto-del-boto.md</c>): la rama D aparece al quinto
    /// fallo, el salto cobra 300, marca el nivel como saltado y superado (<c>max_level</c> lo cuenta: lo que pidió el revisor de
    /// B1a), no cuesta canoa, no toca la Crecida, no es una victoria y carga el siguiente con <c>fails_level</c> = 0; en un
    /// <c>10k</c> paga la Canasta, y el cobro, la Canasta y el salto salen en <b>una sola escritura</b>.
    /// </summary>
    public sealed class SkipFlowTests
    {
        [Test]
        public void FifthFail_OffersTheSkip_AndTheSkipLoadsTheNextLevelWithoutCanoeOrStreak()
        {
            var rig = new ProfileRig(new[] { TestPacks.Tutorial }, 30, seeds: 1000);
            rig.Wallet.Apply(Transaction.InfiniteCanoesFor(TimeSpan.FromHours(1))); // «Rendirse» va directo a Nivel fallido
            LevelFlow flow = rig.Flow;
            flow.Load(36); // tablero 6 del tutorial: 3 → 4 lo bloquea
            Assert.That(flow.TapAnywhere(), Is.True);
            for (int fail = 0; fail < 5; fail++)
            {
                LevelStateTests.GetStuck(flow);
                Assert.That(flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden), "con " + fail + " fallos aún no hay Salto");
                Assert.That(flow.PressGiveUp(), Is.True);
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
                Assert.That(flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden), "Nivel fallido no lo ofrece");
                Assert.That(flow.PressRetry(), Is.True);
                Assert.That(flow.TapAnywhere(), Is.True);
            }

            Assert.That(flow.Attempt.FailsLevel, Is.EqualTo(5));
            Assert.That(flow.SkipOffer.State, Is.EqualTo(SkipState.Ready), "también en juego: la pausa");
            LevelStateTests.GetStuck(flow);
            SkipOffer offer = flow.SkipOffer;
            Assert.That(offer.State, Is.EqualTo(SkipState.Ready), "rama D: fails_level ≥ 5, sin salto anterior, skippable, ≥ 31");
            Assert.That(offer.Price, Is.EqualTo(300));

            CanoesStatus canoesBefore = rig.Wallet.Canoes;
            int wonBefore = rig.Progress.LevelsWon;
            int streakWinsBefore = rig.StreakStore.Load().Wins;
            SkipResult? result = flow.Skip();

            Assert.That(result.HasValue && result.Value.Applied, Is.True);
            Assert.That(result!.Value.Level, Is.EqualTo(36));
            Assert.That(result.Value.FailsLevel, Is.EqualTo(5));
            Assert.That(result.Value.SinceLastSkip, Is.EqualTo(36));
            Assert.That(result.Value.Streak, Is.Not.Null, "con servicio, OnSkipStreak responde (E34)");
            Assert.That(result.Value.Streak!.Has(StreakServiceEvents.BasketOpened), Is.False, "36 no es un 10k: sin Canasta");
            Assert.That(result.Value.Streak.Streak.Counted, Is.False, "la Crecida no cuenta el salto (C11)");
            Assert.That(rig.Wallet.Seeds, Is.EqualTo(700), "−300 semillas (04 §6.2)");
            Assert.That(rig.Wallet.Canoes.Count, Is.EqualTo(canoesBefore.Count), "no cuesta canoa (04 §2.6 a)");
            Assert.That(rig.StreakStore.Load().Wins, Is.EqualTo(streakWinsBefore), "no toca la Crecida (C11)");
            Assert.That(rig.Progress.LevelsWon, Is.EqualTo(wonBefore), "no es una victoria (stats.levels_won)");
            Assert.That(rig.Profile.Progress.SkippedLevels, Is.EqualTo(new[] { 36 }));
            Assert.That(rig.Progress.MaxLevel, Is.EqualTo(36), "el nivel saltado cuenta como superado (04 §2.6)");
            Assert.That(rig.Progress.IsSkipped(36), Is.True, "el Mapa pone a Rosa en su boya");
            Assert.That(rig.Progress.NextLevel, Is.EqualTo(37));
            Assert.That(rig.Profile.Progress.SinceLastSkip, Is.Zero);
            Assert.That(flow.LevelNumber, Is.EqualTo(37), "LOADING(N+1)");
            Assert.That(flow.Attempt.FailsLevel, Is.Zero);
            Assert.That(flow.LastSkip!.Value.Applied, Is.True);
        }

        [Test]
        public void SkippingA10k_PaysItsBasket_AndEverythingGoesOutInOneWrite()
        {
            var rig = new ProfileRig(TestPacks.Campaign, 0, seeds: 1000, maxLevel: 39);
            LevelFlow flow = rig.Flow;
            rig.ForceFails(40, 5);
            Assert.That(flow.Level.Skippable, Is.True, "precondición: el 40 se puede saltar");
            Assert.That(flow.SkipOffer.State, Is.EqualTo(SkipState.Ready));

            BasketContents basket = BasketRules.For(40, 10, BasketConfig.Defaults);
            int writes = rig.Batch.WriteCount;
            SkipResult result = flow.Skip()!.Value;

            Assert.That(rig.Batch.WriteCount, Is.EqualTo(writes + 1), "el cobro, la Canasta, el salto y el nivel 41: una escritura");
            Assert.That(result.Streak, Is.Not.Null);
            Assert.That(result.Streak!.Has(StreakServiceEvents.BasketOpened), Is.True, "K9: el salto del 10k paga la Canasta");
            Assert.That(result.Streak.LevelBasket, Is.EqualTo(basket));
            Profile saved = rig.Disk.Load().Profile!;
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(1000 - 300 + basket.Seeds));
            Assert.That(saved.Progress.MaxLevel, Is.EqualTo(40));
            Assert.That(saved.Progress.SkippedLevels, Is.EqualTo(new[] { 40 }));
            Assert.That(saved.Progress.NextLevel, Is.EqualTo(41));
            Assert.That(saved.LevelState, Is.Not.Null);
            Assert.That(saved.LevelState!.Level, Is.EqualTo(41), "el level_state ya es el del 41");

            // La Canasta no se repaga: el 40 ya está superado y el Salto no vuelve a ofrecerse en él.
            rig.ForceFails(40, 5);
            Assert.That(flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden), "nivel ya superado");
        }

        [Test]
        public void SkippingTheWorldClose_GivesItsSkin_InTheSameWrite()
        {
            // Nota de B14 en D-033: el Salto supera el nivel (cuenta para max_level), así que el 40 saltado da la skin del mundo 2.
            var rig = new ProfileRig(TestPacks.Campaign, 0, seeds: 1000, maxLevel: 39);
            var skins = new SkinService(rig.Profile, rig.Batch.Durable);
            rig.Flow.UseSkins(skins);
            rig.ForceFails(40, 5);
            int serial = rig.Flow.SkinUnlockSerial;
            int writes = rig.Batch.WriteCount;

            Assert.That(rig.Flow.Skip()!.Value.Applied, Is.True);

            Assert.That(rig.Batch.WriteCount, Is.EqualTo(writes + 1), "el salto y la skin: una escritura");
            Assert.That(rig.Flow.SkinUnlockSerial, Is.EqualTo(serial + 1));
            Assert.That(rig.Flow.LatestSkinUnlock!.SkinId, Is.EqualTo("af_skin_m02"));
            Assert.That(rig.Flow.SkinUnlockLoad, Is.LessThan(rig.Flow.LoadSerial), "el aviso sale en el nivel siguiente, ya cargado");
            Profile saved = rig.Disk.Load().Profile!;
            Assert.That(saved.Entitlements.Skins, Does.Contain("af_skin_m02"));
            Assert.That(saved.Entitlements.SkinEquipped, Is.EqualTo("af_skin_m02"), "Q-M8 (a): se pone sola");
        }

        [Test]
        public void TheGap_IsTenLevelsFromTheLastSkip()
        {
            var rig = new ProfileRig(TestPacks.Campaign, 0, seeds: 2000, maxLevel: 40);
            rig.Profile.Progress.SkippedLevels.Add(40);
            rig.ForceFails(49, 5);
            Assert.That(rig.Flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden), "49 − 40 = 9 < skip_min_gap_levels");
            rig.ForceFails(50, 5);
            Assert.That(rig.Flow.SkipOffer.State, Is.EqualTo(SkipState.Ready), "50 − 40 = 10");
            Assert.That(rig.Flow.SkipOffer.LevelsSinceLastSkip, Is.EqualTo(10));
        }

        [Test]
        public void ADebut_IsNotSkippable()
        {
            var rig = new ProfileRig(TestPacks.Campaign, 0, seeds: 2000, maxLevel: 42);
            rig.ForceFails(43, 9);
            Assert.That(rig.Flow.Level.Skippable, Is.False, "43: estreno de la Hoja (04 §2.6)");
            Assert.That(rig.Flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden));
            Assert.That(rig.Flow.Skip(), Is.Null);
        }

        [Test]
        public void WithoutSeeds_TheOfferIsGrey_AndSkipDoesNothing()
        {
            var rig = new ProfileRig(TestPacks.Campaign, 0, seeds: 100, maxLevel: 39);
            rig.ForceFails(40, 5);
            Assert.That(rig.Flow.SkipOffer.State, Is.EqualTo(SkipState.Disabled));
            Assert.That(rig.Flow.SkipOffer.Missing, Is.EqualTo(200));
            int writes = rig.Batch.WriteCount;
            Assert.That(rig.Flow.Skip(), Is.Null);
            Assert.That(rig.Batch.WriteCount, Is.EqualTo(writes));
            Assert.That(rig.Profile.Progress.SkippedLevels, Is.Empty);
            Assert.That(rig.Flow.LevelNumber, Is.EqualTo(40));
        }

        [Test]
        public void WithoutStore_TheSkipIsNeverOffered()
        {
            var rig = new ProfileRig(TestPacks.Campaign, 0, seeds: 1000, maxLevel: 39);
            rig.Flow.UseSkip(null);
            rig.ForceFails(40, 5);
            Assert.That(rig.Flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden));
        }

        [Test]
        public void AnAppliedExtraCuia_IsLostWithoutRefund_AndTheConfirmationKnows()
        {
            var rig = new ProfileRig(TestPacks.Campaign, 0, seeds: 1000, maxLevel: 40);
            rig.ForceFails(41, 5);
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(rig.Flow.SkipLosesExtraCuia, Is.False);
            TransactionResult? extra = rig.Flow.UseExtraCuia();
            Assert.That(extra != null && extra.IsApplied, Is.True, "«+1 cuia» aplicada");
            Assert.That(rig.Flow.SkipLosesExtraCuia, Is.True, "04 §2.6 c: aviso en la confirmación");
            int stock = rig.Wallet.Stock.Get(BoosterKind.ExtraCuia);
            int seeds = rig.Wallet.Seeds;
            Assert.That(rig.Flow.Skip()!.Value.Applied, Is.True);
            Assert.That(rig.Wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(stock), "sin reembolso");
            Assert.That(rig.Wallet.Seeds, Is.EqualTo(seeds - 300), "sin reembolso");
            Assert.That(rig.Flow.Attempt.ExtraCuias, Is.Zero, "el 42 empieza sin la cuia");
            Assert.That(rig.Flow.SkipLosesExtraCuia, Is.False);
        }

        [Test]
        public void WithoutANextLevel_OrCanoesToStartIt_TheSkipIsHidden()
        {
            // El 40 es el último del catálogo (tutorial con desplazamiento 30): no hay adónde llevar al jugador (Q-S4).
            var rig = new ProfileRig(new[] { TestPacks.Tutorial }, 30, seeds: 1000, maxLevel: 39);
            rig.ForceFails(40, 5);
            Assert.That(rig.Flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden), "sin nivel siguiente");
            rig.ForceFails(39, 5); // ya superado (max_level = 39): tampoco
            Assert.That(rig.Flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden));

            // Sin canoas para empezar el siguiente (las infinitas caducaron a mitad): tampoco (Q-S4).
            var poor = new ProfileRig(TestPacks.Campaign, 0, seeds: 1000, maxLevel: 39);
            poor.ForceFails(40, 5);
            Assert.That(poor.Flow.SkipOffer.State, Is.EqualTo(SkipState.Ready));
            poor.Profile.Wallet.Canoes.Count = 0;
            Assert.That(poor.Wallet.CanStartLevel(41), Is.False);
            Assert.That(poor.Flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden));
        }

        [Test]
        public void MemorySkipStore_MarksTheMapProgress()
        {
            var progress = new MemoryProgressStore(nextLevel: 36, maxLevel: 35);
            var store = new MemorySkipStore(progress);
            store.SaveSkip(36, 37);
            store.SaveSkip(36, 37);
            Assert.That(store.SkippedLevels, Is.EqualTo(new[] { 36 }));
            Assert.That(store.SinceLastSkip, Is.Zero);
            Assert.That(progress.IsSkipped(36), Is.True);
            Assert.That(progress.NextLevel, Is.EqualTo(37));
        }

        /// <summary>Flujo sobre un perfil de verdad con el lote, la cartera, el progreso, el salto y la Crecida.</summary>
        internal sealed class ProfileRig
        {
            public ProfileRig(IReadOnlyList<LevelPack> packs, int offset, int seeds, int maxLevel = 0)
            {
                Clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
                Profile = Wallet.NewProfile(Clock);
                Profile.Wallet.Seeds = seeds;
                Profile.Progress.MaxLevel = maxLevel;
                Profile.Progress.NextLevel = maxLevel + 1;
                Disk = new MemoryProfileStore();
                Batch = new ProfileSaveBatch(Disk);
                Wallet = new Wallet(Profile, Batch, Clock);
                Progress = new ProfileProgressStore(Profile, Batch);
                StreakStore = new ProfileStreakStore(Profile, Batch, Clock);
                Flow = new LevelFlow(LevelCatalog.Create(packs, offset), Progress, new RecordingListener(), GameConfig.TapSwitchSelection, PourModes.Default, Wallet)
                {
                    SaveBatch = Batch,
                    StateStore = new ProfileLevelStateStore(Profile, Batch),
                };
                Flow.UseStreak(new StreakService(StreakStore, Wallet, Clock, 10));
                Flow.UseSkip(new ProfileSkipStore(Profile, Batch));
            }

            public ManualClock Clock { get; }

            public Profile Profile { get; }

            public MemoryProfileStore Disk { get; }

            public ProfileSaveBatch Batch { get; }

            public Wallet Wallet { get; }

            public ProfileProgressStore Progress { get; }

            public ProfileStreakStore StreakStore { get; }

            public LevelFlow Flow { get; }

            /// <summary>
            /// Entra en <paramref name="level"/> con <paramref name="fails"/> fallos ya contados, como al volver desde el inicio:
            /// sale del nivel (<c>level_state</c> «salido»), le pone los fallos y lo vuelve a cargar.
            /// </summary>
            public void ForceFails(int level, int fails)
            {
                Flow.Load(level);
                Flow.LeaveLevel();
                LevelStateData saved = Profile.LevelState!;
                saved.FailsLevel = fails;
                Assert.That(Flow.RestoreExited(saved), Is.True);
                Flow.Load(level);
                Assert.That(Flow.Attempt.FailsLevel, Is.EqualTo(fails));
            }
        }
    }
}
