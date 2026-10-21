#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AF.Boosters;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Reglas del Salto del Boto sin flujo (bloque B3; 04 §2.6 y §6.1 <c>skip_eligible</c>; 08 §9; nota
    /// <c>docs/dev/salto-del-boto.md</c>): las tres condiciones de la dirección 9 más <c>skippable</c>, el borde de cada una,
    /// <c>last_skip_level</c> deducido de <c>skipped_levels</c>, el estado en gris sin saldo y las cifras contra 08 §9.
    /// </summary>
    public sealed class SkipBoosterTests
    {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static SkipInput Eligible(int level = 40, int seeds = 1000)
        {
            return new SkipInput
            {
                Level = level,
                IsCampaign = true,
                Skippable = true,
                FailsLevel = 5,
                LastSkipLevel = 0,
                HasNextLevel = true,
                Seeds = seeds,
                Price = 300,
            };
        }

        [Test]
        public void AllConditions_GiveAReadyOfferAt300()
        {
            SkipOffer offer = SkipBooster.Evaluate(Eligible(), SkipConfig.Defaults);
            Assert.That(offer.State, Is.EqualTo(SkipState.Ready));
            Assert.That(offer.Price, Is.EqualTo(300));
            Assert.That(offer.Missing, Is.Zero);
            Assert.That(offer.CanUse, Is.True);
            Assert.That(offer.LevelsSinceLastSkip, Is.EqualTo(40), "sin salto anterior: el propio nivel [P] (Q-S1)");
        }

        [Test]
        public void WithoutEnoughSeeds_TheOfferIsGreyWithWhatIsMissing()
        {
            SkipOffer offer = SkipBooster.Evaluate(Eligible(seeds: 120), SkipConfig.Defaults);
            Assert.That(offer.State, Is.EqualTo(SkipState.Disabled), "04 §1: el precio en gris y «Te faltan N»");
            Assert.That(offer.Missing, Is.EqualTo(180));
            Assert.That(offer.Offered, Is.True);
            Assert.That(offer.CanUse, Is.False);
        }

        [TestCase(4, false, TestName = "Fails_4_NotEligible")]
        [TestCase(5, true, TestName = "Fails_5_Eligible")]
        public void FailsLevel_NeedsSkipMinFails(int fails, bool eligible)
        {
            SkipInput input = Eligible();
            input.FailsLevel = fails;
            Assert.That(SkipBooster.IsEligible(input, SkipConfig.Defaults), Is.EqualTo(eligible));
        }

        [TestCase(30, false, TestName = "Level_30_NotEligible")]
        [TestCase(31, true, TestName = "Level_31_Eligible")]
        public void Level_NeedsSkipMinLevel(int level, bool eligible)
        {
            Assert.That(SkipBooster.IsEligible(Eligible(level), SkipConfig.Defaults), Is.EqualTo(eligible));
        }

        [TestCase(49, false, TestName = "Gap_9_NotEligible")]
        [TestCase(50, true, TestName = "Gap_10_Eligible")]
        public void Gap_NeedsTenLevelsSinceTheLastSkip(int level, bool eligible)
        {
            SkipInput input = Eligible(level);
            input.LastSkipLevel = 40;
            Assert.That(SkipBooster.IsEligible(input, SkipConfig.Defaults), Is.EqualTo(eligible), "«máximo 1 cada 10 niveles» (glosario §5.1)");
        }

        [Test]
        public void NotSkippable_DailyOrNoNextLevel_HideIt()
        {
            SkipInput input = Eligible();
            input.Skippable = false;
            Assert.That(SkipBooster.Evaluate(input, SkipConfig.Defaults).State, Is.EqualTo(SkipState.Hidden), "estreno: skippable = false");
            input = Eligible();
            input.IsCampaign = false;
            Assert.That(SkipBooster.Evaluate(input, SkipConfig.Defaults).State, Is.EqualTo(SkipState.Hidden), "04 §2.6 e: el Reto no admite salto");
            input = Eligible();
            input.HasNextLevel = false;
            Assert.That(SkipBooster.Evaluate(input, SkipConfig.Defaults).State, Is.EqualTo(SkipState.Hidden), "sin nivel siguiente [P]");
        }

        [Test]
        public void LastSkipLevel_IsTheHighestSkippedBelowTheLevel()
        {
            Assert.That(SkipBooster.LastSkipLevel(null, 50), Is.Zero);
            Assert.That(SkipBooster.LastSkipLevel(new List<int>(), 50), Is.Zero);
            Assert.That(SkipBooster.LastSkipLevel(new List<int> { 63, 41, 33 }, 50), Is.EqualTo(41));
            Assert.That(SkipBooster.LastSkipLevel(new List<int> { 50 }, 50), Is.Zero, "el propio nivel no cuenta");
            Assert.That(SkipBooster.LevelsSince(50, 41), Is.EqualTo(9));
            Assert.That(SkipBooster.LevelsSince(50, 0), Is.EqualTo(50));
        }

        [Test]
        public void RemoteConfig_ChangesTheThresholds()
        {
            var config = new SkipConfig { SkipMinFails = 4, SkipMinGapLevels = 8, SkipMinLevel = 20 };
            SkipInput input = Eligible(28);
            input.FailsLevel = 4;
            input.LastSkipLevel = 20;
            Assert.That(SkipBooster.IsEligible(input, config), Is.True);
            Assert.That(() => new SkipConfig { SkipMinFails = 0 }.Validate(), Throws.InvalidOperationException);
        }

        [Test]
        public void Pay_Charges300SeedsWithTheSkipSink()
        {
            var economy = new EconomyFixture(1000);
            TransactionResult paid = SkipBooster.Pay(economy.Wallet);
            Assert.That(paid.IsApplied, Is.True);
            Assert.That(paid.SeedsDelta, Is.EqualTo(-300), "skip_seed_price (08 §9)");
            Assert.That(economy.Wallet.Seeds, Is.EqualTo(700));
            var poor = new EconomyFixture(299);
            Assert.That(SkipBooster.Pay(poor.Wallet).IsApplied, Is.False);
            Assert.That(poor.Wallet.Seeds, Is.EqualTo(299), "sin saldo no cobra nada");
        }

        [Test]
        public void EveryDefault_IsTheOneOfThe08_9Table()
        {
            string text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "gdd", "08-arquitectura-tecnica-publicacion.md")).Replace("\r\n", "\n");
            Match row = Regex.Match(
                text, @"\n\| `skip_seed_price` / `skip_min_fails` / `skip_min_gap_levels` / `skip_min_level` \| int \| ([0-9]+) / ([0-9]+) / ([0-9]+) / ([0-9]+) \|");
            Assert.That(row.Success, Is.True, "no encuentro la fila del Salto del Boto en 08 §9");
            Assert.That(EconomyConfig.Defaults.SkipSeedPrice.ToString(), Is.EqualTo(row.Groups[1].Value), "skip_seed_price");
            var expected = new Dictionary<string, string>
            {
                { "skip_min_fails", row.Groups[2].Value },
                { "skip_min_gap_levels", row.Groups[3].Value },
                { "skip_min_level", row.Groups[4].Value },
            };
            var seen = new List<string>();
            SkipConfig config = SkipConfig.Defaults;
            foreach (PropertyInfo property in typeof(SkipConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                Assert.That(expected.ContainsKey(attribute.Key), Is.True, attribute.Key + " no es de la fila del Salto en 08 §9");
                Assert.That(property.GetValue(config)!.ToString(), Is.EqualTo(expected[attribute.Key]), attribute.Key);
                seen.Add(attribute.Key);
            }

            seen.Sort(StringComparer.Ordinal);
            Assert.That(seen, Is.EqualTo(new[] { "skip_min_fails", "skip_min_gap_levels", "skip_min_level" }));
        }
    }
}
