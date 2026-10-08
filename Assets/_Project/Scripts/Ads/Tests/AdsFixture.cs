#nullable enable
using System;
using System.IO;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Ads.Tests
{
    /// <summary>
    /// Perfil nuevo, almacén en memoria, reloj manual (Bogotá, UTC−5) y anuncios simulados que se cierran al instante.
    /// Por defecto no es la sesión de instalación y el calentamiento de 120 s ya pasó.
    /// </summary>
    public sealed class AdsFixture
    {
        /// <summary>2026-10-08 15:00 UTC = 10:00 en Bogotá.</summary>
        public static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        /// <summary>Crea el entorno.</summary>
        public AdsFixture(bool installSession = false, AdsConfig? config = null, FakeAdFill fill = FakeAdFill.Fill, bool warmedUp = true)
        {
            Clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            Profile = Profile.CreateNew(Clock, 5);
            Store = new MemoryProfileStore();
            Presenter = new ImmediateAdPresenter();
            Service = new FakeAdService(Presenter, fill);
            Config = config ?? AdsConfig.Defaults;
            Controller = AdController.Create(Service, Profile, Store, Clock, installSession, Config);
            if (warmedUp)
            {
                Clock.Advance(Config.SessionWarmup);
            }
        }

        /// <summary>Reloj.</summary>
        public ManualClock Clock { get; }

        /// <summary>Perfil.</summary>
        public Profile Profile { get; }

        /// <summary>Almacén.</summary>
        public MemoryProfileStore Store { get; }

        /// <summary>Pantalla simulada.</summary>
        public ImmediateAdPresenter Presenter { get; }

        /// <summary>Proveedor simulado.</summary>
        public FakeAdService Service { get; }

        /// <summary>Cifras.</summary>
        public AdsConfig Config { get; }

        /// <summary>Controlador.</summary>
        public AdController Controller { get; private set; }

        /// <summary>Reglas.</summary>
        public AdPolicy Policy => Controller.Policy;

        /// <summary>Abre otra sesión (nuevo arranque) sobre el mismo perfil, ya calentada.</summary>
        public void NewSession(bool installSession = false)
        {
            Controller = AdController.Create(Service, Profile, Store, Clock, installSession, Config);
            Clock.Advance(Config.SessionWarmup);
        }

        /// <summary>Gana <paramref name="levels"/> niveles seguidos.</summary>
        public void Win(int levels)
        {
            for (int i = 0; i < levels; i++)
            {
                Controller.RecordLevelCompleted();
            }
        }

        /// <summary>Pide el intersticial tras la victoria de <paramref name="level"/> y devuelve el resultado (síncrono).</summary>
        public AdResult Interstitial(int level, AdContext context = AdContext.VictoryScreen)
        {
            AdResult? result = null;
            Controller.ShowInterstitial(level, context, r => result = r);
            Assert.That(result, Is.Not.Null, "onDone siempre se llama");
            return result!;
        }

        /// <summary>Pide el recompensado de <paramref name="placement"/> (síncrono).</summary>
        public AdResult Rewarded(AdPlacement placement)
        {
            AdResult? result = null;
            Controller.ShowRewarded(placement, r => result = r);
            Assert.That(result, Is.Not.Null, "onDone siempre se llama");
            return result!;
        }

        /// <summary>Muestra un intersticial que sí toca: 3 victorias más, el hueco mínimo y la pantalla de victoria del <paramref name="level"/>.</summary>
        public AdResult NextDueInterstitial(int level)
        {
            Win(Config.InterstitialEvery(level) ?? 0);
            Clock.Advance(Config.InterstitialMinGap);
            return Interstitial(level);
        }

        /// <summary>El perfil tal como quedó guardado.</summary>
        public Profile Saved()
        {
            ProfileLoadResult load = Store.Load();
            Assert.That(load.HasProfile, Is.True, "el perfil se guardó");
            return load.Profile!;
        }
    }

    /// <summary>Almacén que falla al escribir mientras <see cref="Broken"/> sea true.</summary>
    public sealed class FlakyStore : IProfileStore
    {
        private readonly MemoryProfileStore _inner = new MemoryProfileStore();

        /// <summary>Las escrituras fallan.</summary>
        public bool Broken { get; set; }

        /// <summary>Guardados que llegaron al almacén.</summary>
        public int SaveCount => _inner.SaveCount;

        /// <inheritdoc />
        public ProfileLoadResult Load()
        {
            return _inner.Load();
        }

        /// <inheritdoc />
        public void Save(Profile profile)
        {
            if (Broken)
            {
                throw new IOException("disco lleno");
            }

            _inner.Save(profile);
        }

        /// <inheritdoc />
        public void Delete()
        {
            _inner.Delete();
        }
    }
}
