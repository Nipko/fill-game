#nullable enable
using System;
using AF.Boosters;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El Reto del río no admite Salto del Boto (bloque B3; 04 §2.6 e; 06 §4.1): aunque el flujo del Reto tenga almacén, nivel
    /// efectivo ≥ 31 y cinco fallos, la opción sale oculta, <see cref="LevelFlow.Skip"/> no hace nada y no escribe.
    /// </summary>
    public sealed class SkipDailyTests
    {
        [Test]
        public void TheDailyChallenge_NeverOffersTheSkip()
        {
            var clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            profile.Wallet.Seeds = 1000;
            profile.Progress.MaxLevel = 40;
            profile.Progress.NextLevel = 41;
            var disk = new MemoryProfileStore();
            var progress = new ProfileProgressStore(profile, disk);
            Wallet dailyWallet = LevelFlow.CreateDailyWallet(profile, disk, clock);
            LevelFlow flow = LevelFlow.CreateDaily(
                TestPacks.Tutorial.Find(6)!, LevelFlow.DailyEffectiveLevel(40), progress, new RecordingListener(), dailyWallet);
            flow.Clock = clock;
            flow.UseSkip(new ProfileSkipStore(profile, disk));
            flow.StartDaily();
            flow.TapAnywhere();
            Assert.That(flow.IsDaily, Is.True);
            Assert.That(flow.LevelNumber, Is.GreaterThanOrEqualTo(31), "nivel efectivo del Reto");

            for (int fail = 0; fail < 5; fail++)
            {
                LevelStateTests.GetStuck(flow);
                Assert.That(flow.PressRestart(), Is.True, "el pie del Reto es Volver a la orilla (fallo duro sin canoa)");
                flow.TapAnywhere();
            }

            LevelStateTests.GetStuck(flow);
            Assert.That(flow.Attempt.FailsLevel, Is.GreaterThanOrEqualTo(5));
            int savesBefore = disk.SaveCount;
            Assert.That(flow.SkipOffer.State, Is.EqualTo(SkipState.Hidden), "04 §2.6 e");
            Assert.That(flow.Skip(), Is.Null);
            Assert.That(disk.SaveCount, Is.EqualTo(savesBefore), "ni cobra ni guarda");
            Assert.That(profile.Progress.SkippedLevels, Is.Empty);
            Assert.That(profile.Wallet.Seeds, Is.EqualTo(1000));
        }
    }
}
