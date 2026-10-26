#nullable enable
using System;
using AF.Levels;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// El pool del Reto se lee al pedirlo (memoria, 2026-10-26; <see cref="DailyPoolSource"/>): la tarjeta no lo lee, la partida sí,
    /// soltarlo lo vuelve a leer igual y el tablero de cada día es el mismo que con el pool leído al arrancar (D-031: el sorteo no
    /// cambia).
    /// </summary>
    public sealed class DailyPoolSourceTests
    {
        [Test]
        public void Lazy_ReadsOnFirstGet_OnlyOnce_AndReleaseReadsAgain()
        {
            int reads = 0;
            var source = new DailyPoolSource(() =>
            {
                reads++;
                return DailyTestPool.Full;
            });

            Assert.That(source.InBuild, Is.True, "con lector, en el build sin leer nada");
            Assert.That(source.IsLoaded, Is.False);
            Assert.That(reads, Is.EqualTo(0));

            Assert.That(source.Get(), Is.SameAs(DailyTestPool.Full));
            Assert.That(source.Get(), Is.SameAs(DailyTestPool.Full));
            Assert.That(reads, Is.EqualTo(1));
            Assert.That(source.Loads, Is.EqualTo(1));
            Assert.That(source.IsLoaded, Is.True);

            source.Release();
            Assert.That(source.IsLoaded, Is.False);
            Assert.That(source.InBuild, Is.True);
            Assert.That(source.Get(), Is.Not.Null);
            Assert.That(reads, Is.EqualTo(2));
        }

        [Test]
        public void Lazy_InvalidPool_LeavesTheBuild_AndIsNotReadAgain()
        {
            int reads = 0;
            var source = new DailyPoolSource(() =>
            {
                reads++;
                return null;
            });

            Assert.That(source.InBuild, Is.True);
            Assert.That(source.Get(), Is.Null);
            Assert.That(source.InBuild, Is.False, "un pool no válido oculta la tarjeta (D-008)");
            Assert.That(source.Get(), Is.Null);
            Assert.That(reads, Is.EqualTo(1));
        }

        [Test]
        public void Eager_IsNotReleased_AndNullMeansNoDaily()
        {
            var source = new DailyPoolSource(DailyTestPool.Full);
            source.Release();
            Assert.That(source.Get(), Is.SameAs(DailyTestPool.Full));
            Assert.That(source.Loads, Is.EqualTo(0));

            var none = new DailyPoolSource((DailyPool?)null);
            Assert.That(none.InBuild, Is.False);
            Assert.That(none.Get(), Is.Null);
            Assert.Throws<ArgumentNullException>(() => new DailyPoolSource((Func<DailyPool?>)null!));
        }

        [Test]
        public void Service_CardStateAndTodaySlot_DoNotReadThePool_StartDoes()
        {
            var f = new DailyFixture();
            int reads = 0;
            var source = new DailyPoolSource(() =>
            {
                reads++;
                return DailyTestPool.Full;
            });
            DailyChallengeService lazy = DailyChallengeService.WithPoolSource(f.Store, f.Rules, f.Clock, f.Config, source);

            Assert.That(lazy.HasPool, Is.True);
            Assert.That(lazy.IsAvailable(DailyFixture.MaxLevel), Is.True);
            Assert.That(lazy.CardState(DailyFixture.MaxLevel), Is.EqualTo(DailyCardState.Play));
            Assert.That(lazy.TodaySlot(DailyFixture.MaxLevel), Is.EqualTo(f.Service.TodaySlot(DailyFixture.MaxLevel)));
            Assert.That(reads, Is.EqualTo(0), "la tarjeta del inicio no lee el pool");
            Assert.That(lazy.PoolLoaded, Is.False);

            DailyPlayInProgress play = lazy.Start(DailyFixture.MaxLevel);
            Assert.That(reads, Is.EqualTo(1), "empezar la partida sí");
            Assert.That(play.BoardId, Is.EqualTo(f.Service.TodayBoard(DailyFixture.MaxLevel)!.Id));
            Assert.That(play.State.Seed, Is.EqualTo(DailyPool.SeedOf(play.Band, f.Service.TodaySlot(DailyFixture.MaxLevel).Day)));

            lazy.ReleasePool();
            Assert.That(lazy.PoolLoaded, Is.False);
            Assert.That(lazy.TodayBoard(DailyFixture.MaxLevel)!.Id, Is.EqualTo(play.BoardId), "leído otra vez, el mismo tablero (reanudar, R-Q4)");
            Assert.That(reads, Is.EqualTo(2));
        }

        [Test]
        public void Service_WithAnOverrideToday_ReadsThePoolToCheckTheId()
        {
            DailyChallengeConfig config = DailyChallengeConfig.Defaults;
            config.DailyOverrides = DailyOverrides.Parse("{\"2026-10-19\":{\"1\":\"d1_n0007\"}}");
            var f = new DailyFixture(config: config);
            int reads = 0;
            DailyChallengeService lazy = DailyChallengeService.WithPoolSource(f.Store, f.Rules, f.Clock, config, new DailyPoolSource(() =>
            {
                reads++;
                return DailyTestPool.Full;
            }));

            DailySlot slot = lazy.TodaySlot(DailyFixture.MaxLevel);
            Assert.That(reads, Is.EqualTo(1));
            Assert.That(slot, Is.EqualTo(f.Service.TodaySlot(DailyFixture.MaxLevel)));
            Assert.That(slot.OverrideId, Is.EqualTo("d1_n0007"));
        }

        [Test]
        public void SameBoardAsTheEagerPool_EveryDayOfTwoYears_InEachBand([Values(1, 41, 201)] int maxLevel)
        {
            var f = new DailyFixture();
            var source = new DailyPoolSource(() => DailyTestPool.Full);
            DailyChallengeService lazy = DailyChallengeService.WithPoolSource(f.Store, f.Rules, f.Clock, f.Config, source);
            var day = new DateTime(2026, 1, 1, 12, 0, 0);
            for (int i = 0; i < 2 * DailyPool.Days; i++)
            {
                f.SetLocal(day.AddDays(i));
                Assert.That(lazy.TodaySlot(maxLevel), Is.EqualTo(f.Service.TodaySlot(maxLevel)), day.AddDays(i).ToString("yyyy-MM-dd"));
                if (i % 97 == 0)
                {
                    Assert.That(lazy.TodayBoard(maxLevel), Is.SameAs(f.Service.TodayBoard(maxLevel)));
                    lazy.ReleasePool();
                }
            }
        }
    }
}
