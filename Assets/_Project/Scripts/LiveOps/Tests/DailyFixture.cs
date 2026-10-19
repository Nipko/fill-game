#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Reto del río de prueba (R7b): reloj manual en Bogotá (UTC−5), cartera real de <c>AF.Economy</c> sobre un perfil nuevo y un
    /// <see cref="MemoryProfileStore"/>, <see cref="InMemoryDailyChallengeStore"/>, <see cref="FakeServerDate"/> (sin red por
    /// defecto), reglas y servicio con el pool de prueba completo.
    /// </summary>
    internal sealed class DailyFixture
    {
        /// <summary>Desfase de Bogotá.</summary>
        public static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);

        /// <summary>Nivel máximo de un jugador de B1 con el Reto desbloqueado.</summary>
        public const int MaxLevel = 25;

        public DailyFixture(
            FakeServerDate? server = null,
            DailyReconcileMode mode = DailyReconcileMode.KeepOfflineClaims,
            DailyChallengeConfig? config = null,
            DailyChallengeRecord? initial = null,
            DateTime? localStart = null)
        {
            Clock = new ManualClock(Utc(localStart ?? new DateTime(2026, 10, 19, 10, 0, 0)), Bogota);
            Profile = Wallet.NewProfile(Clock);
            ProfileStore = new MemoryProfileStore();
            Wallet = new Wallet(Profile, ProfileStore, Clock);
            Store = new InMemoryDailyChallengeStore(initial);
            Server = server ?? FakeServerDate.Offline();
            Config = config ?? DailyChallengeConfig.Defaults;
            Rules = new DailyChallengeRules(Store, Wallet, Clock, Server, Config, mode);
            Service = new DailyChallengeService(Store, Rules, Clock, Config, DailyTestPool.Full);
            ShuffleAtStart = Shuffle;
        }

        public ManualClock Clock { get; }

        public Profile Profile { get; }

        public MemoryProfileStore ProfileStore { get; }

        public Wallet Wallet { get; }

        public InMemoryDailyChallengeStore Store { get; }

        public FakeServerDate Server { get; }

        public DailyChallengeConfig Config { get; }

        public DailyChallengeRules Rules { get; }

        public DailyChallengeService Service { get; }

        public DailyChallengeRecord Record => Store.Load();

        public int Seeds => Wallet.Seeds;

        public int Shuffle => Wallet.Stock.Get(BoosterKind.Shuffle);

        /// <summary>Pororocas ganadas desde que se creó el fixture.</summary>
        public int ShuffleGained => Shuffle - ShuffleAtStart;

        public int ShuffleAtStart { get; }

        public string Today => Clock.LocalDateKey();

        /// <summary>Hora local de Bogotá → UTC.</summary>
        public static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }

        /// <summary>Pone el reloj del dispositivo en la hora local <paramref name="bogota"/>.</summary>
        public void SetLocal(DateTime bogota)
        {
            Clock.Set(Utc(bogota));
        }

        public void NextDay(int days = 1)
        {
            Clock.Advance(TimeSpan.FromDays(days));
        }

        /// <summary>Gana el Reto de hoy de principio a fin con el servicio (empezar y reclamar).</summary>
        public ClaimOutcome WinToday()
        {
            Service.Start(MaxLevel);
            return Service.ClaimWin();
        }
    }
}
