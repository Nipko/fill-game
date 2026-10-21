#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using AF.Boosters;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Pororoca · Barajar en <c>AF.Boosters</c> (bloque B2; GDD 04 §2.4, §3 y §11; 08 §9 <c>shuffle_*</c>): cifras contra la tabla
    /// única de 08 §9, la oferta del botón (desde el 30; 1 por nivel y 3 al día también con el stock; fuera del juego y del modal;
    /// sin Lluvia ni Piraña; poca agua), el cobro (stock primero; 300 semillas), la hoja de confirmación con ≥ 200 semillas [P] y
    /// los motivos de <c>af_booster_denied</c>. El barajado está en <c>AF.Solver</c> (<c>ShufflerTests</c>, parche B2-solver).
    /// </summary>
    public sealed class ShuffleBoosterTests
    {
        private static ShuffleOffer Evaluate(
            Wallet? wallet, int level = 30, int usedLevel = 0, int usedToday = 0, bool playing = true, ShuffleBoardBlock board = ShuffleBoardBlock.None)
        {
            return ShuffleBooster.Evaluate(level, usedLevel, usedToday, playing, board, wallet, BoosterConfig.Defaults, ShuffleConfig.Defaults);
        }

        [Test]
        public void EveryShuffleDefault_IsTheOneOfThe08_9Table()
        {
            Dictionary<string, EconomyConfigTests.RcRow> table = EconomyConfigTests.ReadRemoteConfigTable();
            ShuffleConfig config = ShuffleConfig.Defaults;
            int checkedKeys = 0;
            foreach (PropertyInfo property in typeof(ShuffleConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                Assert.That(attribute, Is.Not.Null, property.Name + ": toda cifra configurable sale de 08 §9");
                Assert.That(table.ContainsKey(attribute!.Key), Is.True, attribute.Key + " no está en 08 §9");
                EconomyConfigTests.RcRow row = table[attribute.Key];
                object? value = property.GetValue(config);
                if (value is int i)
                {
                    Assert.That(row.Type, Is.EqualTo("int"), attribute.Key);
                    Assert.That(i.ToString(CultureInfo.InvariantCulture), Is.EqualTo(row.Default), attribute.Key);
                }
                else if (value is bool b)
                {
                    Assert.That(row.Type, Is.EqualTo("bool"), attribute.Key);
                    Assert.That(b ? "true" : "false", Is.EqualTo(row.Default), attribute.Key);
                }
                else
                {
                    Assert.Fail(attribute.Key + ": tipo no previsto");
                }

                checkedKeys++;
            }

            Assert.That(checkedKeys, Is.EqualTo(5));
            Assert.That(table["shuffle_unlock_level"].Default, Is.EqualTo(BoosterConfig.Defaults.ShuffleUnlockLevel.ToString(CultureInfo.InvariantCulture)));
            Assert.That(table["shuffle_seed_price"].Default, Is.EqualTo(EconomyConfig.Defaults.ShuffleSeedPrice.ToString(CultureInfo.InvariantCulture)));
        }

        [Test]
        public void Slot_AppearsAtLevel30_04_2_4()
        {
            Assert.That(ShuffleBooster.IsUnlocked(29, BoosterConfig.Defaults), Is.False);
            Assert.That(ShuffleBooster.IsUnlocked(30, BoosterConfig.Defaults), Is.True);
            Assert.That(Evaluate(null, level: 29).State, Is.EqualTo(ShuffleState.Hidden));
            Assert.That(Evaluate(null, level: 29).Shown, Is.False);
            Assert.That(Evaluate(null, level: 30).Shown, Is.True);
        }

        [Test]
        public void Caps_OnePerLevel_ThreePerDay_AlsoWithStock_04_2_4()
        {
            var economy = new EconomyFixture(seeds: 1000);
            economy.Wallet.Apply(Transaction.GrantStock(BoosterKind.Shuffle, 2));

            Assert.That(Evaluate(economy.Wallet).State, Is.EqualTo(ShuffleState.Ready));
            Assert.That(Evaluate(economy.Wallet, usedLevel: 1).State, Is.EqualTo(ShuffleState.UsedLevel));
            Assert.That(Evaluate(economy.Wallet, usedToday: 2).State, Is.EqualTo(ShuffleState.Ready));
            Assert.That(Evaluate(economy.Wallet, usedToday: 3).State, Is.EqualTo(ShuffleState.UsedToday));
            Assert.That(Evaluate(economy.Wallet, usedLevel: 1).DeniedReason, Is.EqualTo("cap_level"));
            Assert.That(Evaluate(economy.Wallet, usedToday: 3).DeniedReason, Is.EqualTo("cap_day"));
        }

        [Test]
        public void NotInTheStuckModal_NorOutsidePlaying_04_2_4()
        {
            var economy = new EconomyFixture(seeds: 1000);

            ShuffleOffer offer = Evaluate(economy.Wallet, playing: false);

            Assert.That(offer.State, Is.EqualTo(ShuffleState.Disabled));
            Assert.That(offer.CanUse, Is.False);
            Assert.That(offer.DeniedReason, Is.EqualTo("not_eligible"));
        }

        [Test]
        public void LluviaOrPirana_DisableIt_AndLowWater_Too_04_2_4_b_d()
        {
            var economy = new EconomyFixture(seeds: 1000);

            Assert.That(Evaluate(economy.Wallet, board: ShuffleBoardBlock.Unsupported).State, Is.EqualTo(ShuffleState.Unsupported));
            Assert.That(Evaluate(economy.Wallet, board: ShuffleBoardBlock.Unsupported).DeniedReason, Is.EqualTo("unsupported"));
            Assert.That(Evaluate(economy.Wallet, board: ShuffleBoardBlock.LowWater).State, Is.EqualTo(ShuffleState.LowWater));
            Assert.That(Evaluate(economy.Wallet, board: ShuffleBoardBlock.LowWater, playing: false).State, Is.EqualTo(ShuffleState.LowWater));
            Assert.That(Evaluate(economy.Wallet, board: ShuffleBoardBlock.Unsupported, usedLevel: 1).State, Is.EqualTo(ShuffleState.Unsupported));
        }

        [Test]
        public void Stock_IsSpentFirst_ThenSeeds_04_1_1()
        {
            var economy = new EconomyFixture(seeds: 1000);
            economy.Wallet.Apply(Transaction.GrantStock(BoosterKind.Shuffle, 1));

            ShuffleOffer withStock = Evaluate(economy.Wallet);
            Assert.That(withStock.FromStock, Is.True);
            Assert.That(withStock.Payment, Is.EqualTo(ShufflePayment.Stock));
            Assert.That(ShuffleBooster.Pay(economy.Wallet, withStock.Payment).IsApplied, Is.True);
            Assert.That(economy.Wallet.Stock.Get(BoosterKind.Shuffle), Is.EqualTo(0));
            Assert.That(economy.Wallet.Seeds, Is.EqualTo(1000));

            ShuffleOffer withSeeds = Evaluate(economy.Wallet);
            Assert.That(withSeeds.Payment, Is.EqualTo(ShufflePayment.Seeds));
            Assert.That(withSeeds.Price, Is.EqualTo(300));
            Assert.That(ShuffleBooster.Pay(economy.Wallet, withSeeds.Payment).IsApplied, Is.True);
            Assert.That(economy.Wallet.Seeds, Is.EqualTo(700));
            Assert.That(ShuffleBooster.SourceId(ShufflePayment.Seeds), Is.EqualTo("seeds"));
            Assert.That(ShuffleBooster.SourceId(ShufflePayment.Stock), Is.EqualTo("stock"));
        }

        [Test]
        public void WithoutBalance_ShowsTheMissingSeeds_AndDoesNotCharge_04_1_1()
        {
            var economy = new EconomyFixture(seeds: 120);

            ShuffleOffer offer = Evaluate(economy.Wallet);

            Assert.That(offer.State, Is.EqualTo(ShuffleState.Ready));
            Assert.That(offer.Missing, Is.EqualTo(180));
            Assert.That(offer.CanUse, Is.False);
            Assert.That(offer.DeniedReason, Is.EqualTo("no_balance"));
            Assert.That(ShuffleBooster.CanPay(economy.Wallet, ShufflePayment.Seeds), Is.False);
            Assert.That(ShuffleBooster.CanPay(economy.Wallet, ShufflePayment.Stock), Is.False);
            Assert.That(ShuffleBooster.Pay(economy.Wallet, ShufflePayment.Seeds).IsApplied, Is.False);
            Assert.That(economy.Wallet.Seeds, Is.EqualTo(120));
        }

        [Test]
        public void ConfirmationSheet_OnlyWhenPayingAtLeast200Seeds_04_2_4()
        {
            var economy = new EconomyFixture(seeds: 1000);
            Assert.That(ShuffleBooster.ConfirmMinSeeds, Is.EqualTo(200));
            Assert.That(Evaluate(economy.Wallet).NeedsConfirmation, Is.True);

            economy.Wallet.Apply(Transaction.GrantStock(BoosterKind.Shuffle, 1));
            Assert.That(Evaluate(economy.Wallet).NeedsConfirmation, Is.False, "con el stock no se gastan semillas [P]");

            var cheap = new EconomyFixture(seeds: 1000, config: new EconomyConfig { ShuffleSeedPrice = 199 });
            ShuffleOffer offer = ShuffleBooster.Evaluate(30, 0, 0, true, ShuffleBoardBlock.None, cheap.Wallet, BoosterConfig.Defaults, ShuffleConfig.Defaults);
            Assert.That(offer.NeedsConfirmation, Is.False);
        }

        [Test]
        public void Validate_RejectsNegativeCaps()
        {
            Assert.That(() => new ShuffleConfig { ShuffleMaxPerDay = -1 }.Validate(), Throws.InvalidOperationException);
            Assert.That(() => new ShuffleConfig { ShuffleBudgetNodes = 0 }.Validate(), Throws.InvalidOperationException);
            Assert.That(() => ShuffleConfig.Defaults.Validate(), Throws.Nothing);
        }
    }
}
