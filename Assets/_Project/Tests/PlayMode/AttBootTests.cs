#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El aviso de rastreo de Apple (ATT; fase M3 de <c>mac-ios.md</c>; regla aprobada por el dueño el 2026-11-04) en la escena
    /// <c>Game</c> real, con el aviso del sistema falso (<see cref="FakeTrackingPort"/>): tras la edad y el consentimiento, la tarjeta
    /// previa solo con 18+ y anuncios personalizados y mientras iOS no haya preguntado; un solo botón, «Continuar», que pide el aviso
    /// del sistema una vez; nunca con 13-17, &lt;13, <c>npa</c>, con la respuesta ya dada ni fuera del iPhone (sin puerto). Textos de
    /// <c>strings.csv</c>, ≥ 12 sp y sin recortes al 130 %.
    /// </summary>
    public sealed class AttBootTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 4, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
            LaunchOptions.ClockOverride = new ManualClock(T0, TimeSpan.FromHours(-5));
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            GameBootstrap.ResetConsentOverrides(); // también TrackingPortOverride
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetCloudOverrides();
        }

        private static MemoryProfileStore StoreWith(AgeBand band, bool adsPersonalized)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 4;
            profile.Progress.MaxLevel = 3;
            profile.Consent = new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, adsPersonalized, band);
            return new MemoryProfileStore(profile);
        }

        private static IEnumerator Boot(MemoryProfileStore store, FakeTrackingPort? port)
        {
            GameBootstrap.TrackingPortOverride = port;
            yield return PlayModeHelpers.LoadAppWithConsent(store);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Adult_WithPersonalizedAds_SeesTheCard_ThenTheSystemPromptOnce()
        {
            var port = new FakeTrackingPort(TrackingStatus.NotDetermined, TrackingStatus.Denied);
            MemoryProfileStore store = StoreWith(AgeBand.Adult, true);
            yield return Boot(store, port);
            GameBootstrap boot = GameBootstrap.Current!;
            AttPrePromptCard card = boot.AttCard!;
            Assert.That(card, Is.Not.Null);
            Assert.That(card.Visible, Is.True);
            Assert.That(port.Requests, Is.EqualTo(0), "el aviso del sistema, solo tras «Continuar»");
            Assert.That(card.TitleText, Is.EqualTo(Loc.T(AttPrePromptCard.TitleKey)));
            Assert.That(card.BodyText, Is.EqualTo(Loc.T(AttPrePromptCard.BodyKey)));
            Assert.That(card.ContinueText, Is.EqualTo(Loc.T(AttPrePromptCard.ContinueKey)));
            var ids = new List<string>();
            var rects = new List<RectTransform>();
            card.VisibleButtons(ids, rects);
            Assert.That(ids, Is.EqualTo(new[] { "att_continue" }), "directriz 5.1.1: un solo botón, sin «No» ni X");

            card.ContinueButton.onClick.Invoke();
            Assert.That(card.Visible, Is.False);
            Assert.That(port.Requests, Is.EqualTo(1));
            Assert.That(boot.TrackingAnswer, Is.EqualTo(TrackingStatus.Denied));
            card.ContinueButton.onClick.Invoke();
            Assert.That(port.Requests, Is.EqualTo(1), "un segundo toque no vuelve a pedirlo");

            yield return Boot(store, port); // el arranque siguiente: iOS ya respondió
            Assert.That(GameBootstrap.Current!.AttCard, Is.Null);
            Assert.That(port.Requests, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NeverAsked_WithNpa_Teens_AnAnswerGiven_OrOffTheIphone()
        {
            var cases = new (string Why, MemoryProfileStore Store, FakeTrackingPort? Port)[]
            {
                ("npa", StoreWith(AgeBand.Adult, false), new FakeTrackingPort()),
                ("13-17", StoreWith(AgeBand.Teen, true), new FakeTrackingPort()),
                ("ya denegado", StoreWith(AgeBand.Adult, true), new FakeTrackingPort(TrackingStatus.Denied)),
                ("restringido", StoreWith(AgeBand.Adult, true), new FakeTrackingPort(TrackingStatus.Restricted)),
                ("ya autorizado", StoreWith(AgeBand.Adult, true), new FakeTrackingPort(TrackingStatus.Authorized)),
                ("sin puerto (Android, editor)", StoreWith(AgeBand.Adult, true), null),
            };
            foreach ((string why, MemoryProfileStore store, FakeTrackingPort? port) in cases)
            {
                yield return Boot(store, port);
                Assert.That(GameBootstrap.Current!.AttCard, Is.Null, why);
                Assert.That(port?.Requests ?? 0, Is.EqualTo(0), why);
            }
        }

        [UnityTest]
        public IEnumerator Under13_Blocked_NeverAsked()
        {
            var port = new FakeTrackingPort();
            yield return Boot(StoreWith(AgeBand.Under13, false), port);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentBlocked, Is.True);
            Assert.That(boot.AttCard, Is.Null);
            Assert.That(port.Requests, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FirstRun_AdultAcceptsAll_ThenTheCard()
        {
            var port = new FakeTrackingPort();
            GameBootstrap.TrackingPortOverride = port;
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.AttCard, Is.Null, "nunca antes de la edad y el consentimiento");
            boot.ConsentView!.AgeButton(AgeBand.Adult).onClick.Invoke();
            yield return null;
            Assert.That(boot.AttCard, Is.Null);
            boot.ConsentView!.AcceptAllButton.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(() => boot.AttCard != null && boot.AttCard.Visible, "la tarjeta tras aceptar");
            Assert.That(boot.ConsentView!.Visible, Is.False, "nunca a la vez que el consentimiento");
        }

        [UnityTest]
        public IEnumerator Texts_AtLeast12sp_WithoutClipping_At130Percent()
        {
            LaunchOptions.FontScaleOverride = 1.3f;
            yield return Boot(StoreWith(AgeBand.Adult, true), new FakeTrackingPort());
            AttPrePromptCard card = GameBootstrap.Current!.AttCard!;
            Assert.That(card.Visible, Is.True);
            yield return null;
            foreach (TextMeshProUGUI t in card.Texts())
            {
                t.ForceMeshUpdate();
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), "«" + t.text + "» ≥ 12 sp");
                Assert.That(t.isTextOverflowing, Is.False, "«" + t.text + "» sin recortes");
            }
        }
    }
}
