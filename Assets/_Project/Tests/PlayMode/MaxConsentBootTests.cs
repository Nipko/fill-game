#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// AppLovin MAX en la escena <c>Game</c> real con el SDK falso (D-042; 08 §2.3 y §7; D-033 B12): el arranque crea el proveedor de
    /// MAX, pero no lo toca hasta la decisión de edad y consentimiento; los flags van antes de <c>InitializeSdk</c>; con &lt;13 nunca se
    /// inicia (tampoco en el arranque siguiente, que se para antes de crear los anuncios); con 13-17, no personalizado.
    /// </summary>
    public sealed class MaxConsentBootTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

        private FakeMaxSdkPort _port = new FakeMaxSdkPort();
        private int _created;

        [SetUp]
        public void SetUp()
        {
            _port = new FakeMaxSdkPort { AutoFill = true };
            _created = 0;
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
            LaunchOptions.ClockOverride = new ManualClock(T0, TimeSpan.FromHours(-5));
            GameBootstrap.AdProviderOverride = (profile, clock) =>
            {
                _created++;
                return new MaxAdService(_port, clock, () => null, () => profile.Progress.NextLevel);
            };
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            GameBootstrap.ResetConsentOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetCloudOverrides();
        }

        private static Profile ProfileWith(ConsentRecord consent)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 4;
            profile.Progress.MaxLevel = 3;
            profile.Consent = consent;
            return profile;
        }

        [UnityTest]
        public IEnumerator FirstRun_MaxStartsOnlyAfterConsent_WithTheFlagsFirst()
        {
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(_created, Is.EqualTo(1), "el arranque crea el proveedor de MAX");
            Assert.That(_port.Calls, Is.Empty, "nada antes de consentir (08 §2.3)");

            boot.ConsentView!.AgeButton(AgeBand.Adult).onClick.Invoke();
            yield return null;
            Assert.That(_port.Calls, Is.Empty, "la edad sola no basta: falta el consentimiento");
            boot.ConsentView.AcceptAllButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => boot.App!.Screen != AppScreen.Boot, "la primera pantalla tras el consentimiento");

            Assert.That(_port.Initializations, Is.EqualTo(1));
            int init = _port.Calls.IndexOf("init");
            Assert.That(_port.Calls.IndexOf("consent:true"), Is.InRange(0, init - 1));
            Assert.That(_port.Calls.IndexOf("dns:false"), Is.InRange(0, init - 1));
        }

        [UnityTest]
        public IEnumerator Under13_NeverInitializesMax_NorOnTheNextBoot()
        {
            var store = new MemoryProfileStore();
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            GameBootstrap first = GameBootstrap.Current!;
            first.ConsentView!.AgeButton(AgeBand.Under13).onClick.Invoke();
            yield return null;
            Assert.That(first.ConsentBlocked, Is.True);
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.ConsentBlocked,
                "el arranque bloqueado tras elegir <13");

            Assert.That(_port.Initializations, Is.EqualTo(0), "con <13, MAX nunca se inicia (D-042)");
            Assert.That(_port.Calls, Is.Empty);
            Assert.That(_created, Is.EqualTo(1), "el arranque bloqueado ni siquiera crea los anuncios");
        }

        [UnityTest]
        public IEnumerator Teen_WithASavedDecision_IsNonPersonalizedBeforeInit()
        {
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Teen)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);

            Assert.That(_port.Initializations, Is.EqualTo(1));
            int init = _port.Calls.IndexOf("init");
            Assert.That(_port.Calls.IndexOf("consent:false"), Is.InRange(0, init - 1), "13-17: npa (08 §2.3)");
            Assert.That(_port.Calls.IndexOf("dns:true"), Is.InRange(0, init - 1));
            Assert.That(GameBootstrap.Current!.ConsentAds!.MaxAdContentRating, Is.EqualTo("G"));
        }
    }
}
