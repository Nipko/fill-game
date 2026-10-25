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
    /// AdMob directo (Google Mobile Ads) en la escena <c>Game</c> real con el SDK falso (D-049; 08 §2.3 y §7; D-033 B12): el arranque
    /// crea el proveedor de Google pero no lo toca hasta la edad y el consentimiento; después, <c>SetRequestConfiguration</c>, UMP y
    /// solo entonces <c>MobileAds.Initialize</c>; con &lt;13 ninguna llamada (tampoco en el arranque siguiente); con 13-17,
    /// <c>TagForUnderAgeOfConsent</c> y <c>npa</c>.
    /// </summary>
    public sealed class GoogleConsentBootTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 25, 15, 0, 0, DateTimeKind.Utc);

        private FakeGmaPort _port = new FakeGmaPort();
        private int _created;

        [SetUp]
        public void SetUp()
        {
            _port = new FakeGmaPort { AutoConsent = true, AutoInitialize = true, AutoFill = true };
            _created = 0;
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
            LaunchOptions.ClockOverride = new ManualClock(T0, TimeSpan.FromHours(-5));
            GameBootstrap.AdProviderOverride = (profile, clock) =>
            {
                _created++;
                return new GoogleAdService(_port, clock, () => null, () => profile.Progress.NextLevel);
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
        public IEnumerator FirstRun_GoogleStartsOnlyAfterConsent_UmpBeforeInitialize()
        {
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(_created, Is.EqualTo(1), "el arranque crea el proveedor de Google");
            Assert.That(_port.Calls, Is.Empty, "nada antes de consentir (08 §2.3)");

            boot.ConsentView!.AgeButton(AgeBand.Adult).onClick.Invoke();
            yield return null;
            Assert.That(_port.Calls, Is.Empty, "la edad sola no basta: falta el consentimiento");
            boot.ConsentView.AcceptAllButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => boot.App!.Screen != AppScreen.Boot, "la primera pantalla tras el consentimiento");

            Assert.That(_port.Initializations, Is.EqualTo(1));
            int config = _port.Calls.IndexOf("config:tfua=false");
            int ump = _port.Calls.IndexOf("ump:tfua=false");
            int init = _port.Calls.IndexOf("init");
            Assert.That(config, Is.EqualTo(0));
            Assert.That(ump, Is.InRange(config + 1, init - 1), "UMP antes de MobileAds.Initialize");
            Assert.That(_port.Pumps, Is.GreaterThan(0), "el arranque entrega los avisos del SDK cada fotograma");
        }

        [UnityTest]
        public IEnumerator Under13_NeverTouchesGoogle_NorOnTheNextBoot()
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

            Assert.That(_port.Calls, Is.Empty, "con <13 ninguna línea de Google Ads (D-049)");
            Assert.That(_port.ConsentRequests, Is.EqualTo(0));
            Assert.That(_created, Is.EqualTo(1), "el arranque bloqueado ni siquiera crea los anuncios");
        }

        [UnityTest]
        public IEnumerator Teen_WithASavedDecision_IsUnderAgeAndNonPersonalized()
        {
            var store = new MemoryProfileStore(ProfileWith(new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Teen)));
            yield return PlayModeHelpers.LoadAppWithConsent(store);

            Assert.That(_port.Initializations, Is.EqualTo(1));
            int init = _port.Calls.IndexOf("init");
            Assert.That(_port.Calls.IndexOf("config:tfua=true"), Is.InRange(0, init - 1), "13-17: TagForUnderAgeOfConsent (08 §2.3)");
            Assert.That(_port.Calls.IndexOf("ump:tfua=true"), Is.InRange(0, init - 1));
            Assert.That(_port.Calls, Does.Contain("load:rewarded:npa=true"), "13-17: npa");
            Assert.That(GameBootstrap.Current!.ConsentAds!.MaxAdContentRating, Is.EqualTo("G"));
        }
    }
}
