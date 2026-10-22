#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// La Bitácora en el juego (B4; 06 §4.2; D-033, añadido del 2026-10-20, puntos 4-7) con la cartera real de <c>AF.Economy</c>:
    /// se sella solo tras terminar un nivel hoy, una página por día, en orden y sin reinicio; el avance se guarda antes de pagar;
    /// lo fijo de los hitos (Canastas, Canasta grande y marco) entra con la casilla; <c>af_calendar_claim</c> lleva sus campos.
    /// </summary>
    public sealed class LoginCalendarServiceTests
    {
        private const int MaxLevel = 25;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        private ManualClock _clock = null!;
        private Profile _profile = null!;
        private Wallet _wallet = null!;
        private InMemoryLoginCalendarStore _store = null!;
        private LoginCalendarService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(Utc(new DateTime(2026, 10, 20, 10, 0, 0)), Bogota);
            _profile = Wallet.NewProfile(_clock);
            _wallet = new Wallet(_profile, new MemoryProfileStore(), _clock);
            _store = new InMemoryLoginCalendarStore();
            _service = new LoginCalendarService(_store, _wallet, _clock, DailyChallengeConfig.Defaults.LiveUnlockLevelDaily);
        }

        [Test]
        public void TheLogbook_ExistsFromLiveUnlockLevelDaily_WithItsSwitch()
        {
            Assert.That(_service.UnlockLevel, Is.EqualTo(10), "live_unlock_level_daily (08 §9)");
            Assert.That(_service.StateFor(9), Is.EqualTo(LoginCalendarState.Hidden));
            Assert.That(_service.StateFor(10), Is.EqualTo(LoginCalendarState.Waiting));

            var config = LoginCalendarConfig.Defaults;
            config.BizFeatureLoginCalendar = false;
            var off = new LoginCalendarService(_store, _wallet, _clock, 10, config);
            Assert.That(off.StateFor(MaxLevel), Is.EqualTo(LoginCalendarState.Hidden));
            _service.MarkPlayed();
            Assert.That(off.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 1), Is.Null, "apagada: no se sella");
        }

        [Test]
        public void ClaimNeedsALevelFinishedToday_AndSealsOnePagePerDay()
        {
            Assert.That(_service.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 1), Is.Null, "sin jugar hoy no se sella");
            Assert.That(_service.MarkPlayed(), Is.True);
            Assert.That(_service.MarkPlayed(), Is.False, "solo la primera vez del día escribe");
            Assert.That(_store.SaveCount, Is.EqualTo(1));
            Assert.That(_service.StateFor(MaxLevel), Is.EqualTo(LoginCalendarState.Ready));

            int seeds = _wallet.Seeds;
            LoginCalendarClaim? claim = _service.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 1);
            Assert.That(claim, Is.Not.Null);
            Assert.That(claim!.Day, Is.EqualTo(1));
            Assert.That(claim.Cycle, Is.EqualTo(1));
            Assert.That(claim.SeedsPaid, Is.EqualTo(60));
            Assert.That(claim.AllApplied, Is.True);
            Assert.That(_wallet.Seeds, Is.EqualTo(seeds + 60));
            Assert.That(_service.StateFor(MaxLevel), Is.EqualTo(LoginCalendarState.Sealed));
            Assert.That(_service.Record.NextDay, Is.EqualTo(2));
            Assert.That(_service.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 1), Is.Null, "una página por día");

            NextDay();
            Assert.That(_service.StateFor(MaxLevel), Is.EqualTo(LoginCalendarState.Waiting), "al día siguiente hay que volver a jugar");
            _service.MarkPlayed();
            LoginCalendarClaim? second = _service.Claim(LoginCalendarChoice.Booster, MaxLevel, LoginCalendarUnlocks.All, 1);
            Assert.That(second!.Day, Is.EqualTo(2));
            Assert.That(second.Reward.ToJson(), Is.EqualTo("{\"undo\":2}"));
            Assert.That(second.SeedsPaid, Is.EqualTo(0));
        }

        [Test]
        public void MissingDays_DoesNotResetTheLogbook_AndReportsDaysMissedBefore()
        {
            PlayAndClaim(LoginCalendarChoice.Seeds);
            NextDay(4);
            _service.MarkPlayed();
            LoginCalendarClaim? claim = _service.Claim(LoginCalendarChoice.Mix, MaxLevel, LoginCalendarUnlocks.All, 1);
            Assert.That(claim!.Day, Is.EqualTo(2), "secuencial sin reinicio (06 §4.2)");
            Assert.That(claim.DaysMissedBefore, Is.EqualTo(3));
            Assert.That(claim.RewardType, Is.EqualTo("mix"));
            Assert.That(claim.RewardValue, Is.EqualTo(60), "reward_value: el valor equivalente (D-033)");
        }

        [Test]
        public void TheChoice_PaysTheSubstitutedReward_ForLockedBoosters()
        {
            for (int day = 1; day <= 3; day++)
            {
                PlayAndClaim(LoginCalendarChoice.Seeds);
                NextDay();
            }

            int cuias = _wallet.Stock.Get(BoosterKind.ExtraCuia);
            int hints = _wallet.Stock.Get(BoosterKind.Hint);
            _service.MarkPlayed();
            LoginCalendarUnlocks atTen = LoginCalendarUnlocks.AtLevel(11, 12, 100, 8);
            LoginCalendarSnapshot snap = _service.Peek(10, atTen, 1);
            Assert.That(snap.NextDay, Is.EqualTo(4));
            Assert.That(snap.Options[1].ToJson(), Is.EqualTo("{\"extra_cuia\":1}"), "Morpho bloqueado → Cuia extra");
            LoginCalendarClaim? claim = _service.Claim(LoginCalendarChoice.Booster, 10, atTen, 1);
            Assert.That(claim!.Reward, Is.EqualTo(snap.Options[1]));
            Assert.That(_wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(cuias + 1));
            Assert.That(_wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(hints), "nunca un comodín bloqueado");
            Assert.That(claim.RewardValue, Is.EqualTo(100));
        }

        [Test]
        public void Day7_PaysTheBasketWithTheOption_AndDay30TheGrandBasketAndTheFrame()
        {
            for (int day = 1; day <= 6; day++)
            {
                PlayAndClaim(LoginCalendarChoice.Seeds);
                NextDay();
            }

            _service.MarkPlayed();
            LoginCalendarSnapshot snap = _service.Peek(MaxLevel, LoginCalendarUnlocks.All, 1);
            Assert.That(snap.Basket.Kind, Is.EqualTo(BasketKind.Normal));
            BasketContents expected = BasketRules.HitSix(MaxLevel, BasketConfig.Defaults);
            Assert.That(snap.Basket.Seeds, Is.EqualTo(expected.Seeds).And.EqualTo(50), "eco_basket_seeds (Q-B7)");
            Assert.That(snap.Basket.Stock, Is.EqualTo(expected.Stock), "el comodín de la próxima Canasta de nivel");
            int seeds = _wallet.Seeds;
            LoginCalendarClaim? claim = _service.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 1);
            Assert.That(claim!.Day, Is.EqualTo(7));
            Assert.That(claim.SeedsPaid, Is.EqualTo(250 + 50));
            Assert.That(_wallet.Seeds, Is.EqualTo(seeds + 300));
            Assert.That(claim.RewardValue, Is.EqualTo(250), "sin lo fijo [P]");
            Assert.That(claim.FrameId, Is.Null);

            for (int day = 8; day <= 29; day++)
            {
                NextDay();
                PlayAndClaim(LoginCalendarChoice.Seeds);
            }

            NextDay();
            _service.MarkPlayed();
            int hints = _wallet.Stock.Get(BoosterKind.Hint);
            int cuias = _wallet.Stock.Get(BoosterKind.ExtraCuia);
            int undos = _wallet.Stock.Get(BoosterKind.Undo);
            seeds = _wallet.Seeds;
            LoginCalendarClaim? last = _service.Claim(LoginCalendarChoice.Mix, MaxLevel, LoginCalendarUnlocks.All, 3);
            Assert.That(last!.Day, Is.EqualTo(30));
            Assert.That(last.Basket.Kind, Is.EqualTo(BasketKind.Grand));
            Assert.That(last.FrameId, Is.EqualTo("frame_w03"), "el marco del mundo donde está el jugador (06 §4.2, regla 4)");
            Assert.That(_service.Record.Frames, Is.EqualTo(new[] { "frame_w03" }), "se guarda como derecho (Q-M6)");
            // Mezcla: 3 Morpho + 300; Canasta grande: 150 + 1 Cuia extra + 1 Morpho + 3 Marea.
            Assert.That(_wallet.Seeds, Is.EqualTo(seeds + 300 + 150));
            Assert.That(_wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(hints + 3 + 1));
            Assert.That(_wallet.Stock.Get(BoosterKind.ExtraCuia), Is.EqualTo(cuias + 1));
            Assert.That(_wallet.Stock.Get(BoosterKind.Undo), Is.EqualTo(undos + 3));
            Assert.That(_service.Record.NextDay, Is.EqualTo(1), "al sellar la 30 empieza otro ciclo");
            Assert.That(_service.Record.Cycle, Is.EqualTo(2));

            NextDay();
            _service.MarkPlayed();
            LoginCalendarClaim? again = _service.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 3);
            Assert.That(again!.Day, Is.EqualTo(1));
            Assert.That(again.Cycle, Is.EqualTo(2));
            Assert.That(_service.Record.Frames.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheAdvance_IsSavedBeforeThePayment_AndAFailedWriteStaysPending()
        {
            _service.MarkPlayed();
            LoginCalendarClaim? seen = null;
            int seedsAtEvent = -1;
            _service.Claimed += c =>
            {
                seen = c;
                seedsAtEvent = _wallet.Seeds;
            };

            int savesBefore = _store.SaveCount;
            _store.FailSaves = true;
            LoginCalendarClaim? claim = _service.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 1);
            Assert.That(_store.SaveCount, Is.EqualTo(savesBefore + 1));
            Assert.That(_store.PendingSave, Is.True, "el avance sigue en memoria y sale con el siguiente guardado");
            Assert.That(_service.Record.NextDay, Is.EqualTo(2));
            Assert.That(_service.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 1), Is.Null, "nunca dos veces");
            Assert.That(seen, Is.SameAs(claim));
            Assert.That(_service.LastClaim, Is.SameAs(claim));
            Assert.That(seedsAtEvent, Is.EqualTo(60), "el aviso sale después de pagar");
            _store.FailSaves = false;
            Assert.That(_store.Flush(), Is.True);
            Assert.That(_store.Persisted!.NextDay, Is.EqualTo(2));
        }

        [Test]
        public void BackwardsClock_NeitherSealsNorMarks()
        {
            PlayAndClaim(LoginCalendarChoice.Seeds);
            _clock.Advance(TimeSpan.FromDays(-3));
            Assert.That(_service.MarkPlayed(), Is.False);
            Assert.That(_service.StateFor(MaxLevel), Is.EqualTo(LoginCalendarState.Sealed));
            Assert.That(_service.Claim(LoginCalendarChoice.Seeds, MaxLevel, LoginCalendarUnlocks.All, 1), Is.Null);
        }

        [Test]
        public void TheService_RejectsAnInvalidConfig()
        {
            var config = LoginCalendarConfig.Defaults;
            config.EcoCalendarLockedBoosterSeeds = new LoginCalendarValueRate(0, 100);
            Assert.Throws<InvalidOperationException>(() => new LoginCalendarService(_store, _wallet, _clock, 10, config));
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.Claim((LoginCalendarChoice)3, MaxLevel, LoginCalendarUnlocks.All, 1));
        }

        private void PlayAndClaim(LoginCalendarChoice choice)
        {
            _service.MarkPlayed();
            Assert.That(_service.Claim(choice, MaxLevel, LoginCalendarUnlocks.All, 1), Is.Not.Null);
        }

        private void NextDay(int days = 1)
        {
            _clock.Advance(TimeSpan.FromDays(days));
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }
    }
}
