#nullable enable
using System;
using System.Collections.Generic;
using AF.IAP;
using AF.Platform;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Las pantallas de la edad y el consentimiento solas, sin la escena (bloque B12; 08 §2.3 y §7; 05 §7; Q-T4 (a)): la edad neutral
    /// con tres bandas iguales y ninguna marcada; el consentimiento con las finalidades apagadas al abrir, «Rechazar todo» al mismo
    /// nivel que «Aceptar todo» y «Guardar mi elección»; la línea de los anuncios en UMP y con 13-17; el bloqueo sin botones; y la
    /// doble confirmación de compras. El arranque con <c>GameBootstrap</c> y Ajustes van en el parche <c>B12-ui.diff</c>
    /// (<c>ConsentBootTests</c>).
    /// </summary>
    public sealed class ConsentScreenTests
    {
        // El arte procedural cuesta unos 3 s por instancia: uno para todo el fixture (T6, carril A); las pantallas solo leen
        // sus sprites.
        private static ProceduralArt? s_art;
        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        private GameObject? _host;
        private ConsentScreen _screen = null!;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("ConsentHost", typeof(RectTransform));
            _screen = new ConsentScreen(_host.transform, Art);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose(); // la recarga de dominio está apagada: el arte del fixture no sobrevive a la sesión de Play
            s_art = null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
            }
        }

        [Test]
        public void Age_ThreeEqualBands_InOrder_WithoutClose_AtBoot()
        {
            var chosen = new List<AgeBand>();
            _screen.ShowAge(chosen.Add);

            Assert.That(_screen.Visible, Is.True);
            Assert.That(_screen.View, Is.EqualTo(ConsentView.Age));
            Assert.That(_screen.Closable, Is.False, "en el arranque no se puede saltar (06 §2.1, regla 3)");
            Assert.That(_screen.CloseButton.gameObject.activeSelf, Is.False);
            RectTransform a = (RectTransform)_screen.AgeButton(AgeBand.Under13).transform;
            RectTransform b = (RectTransform)_screen.AgeButton(AgeBand.Teen).transform;
            RectTransform c = (RectTransform)_screen.AgeButton(AgeBand.Adult).transform;
            Assert.That(b.rect.width, Is.EqualTo(a.rect.width).Within(0.5f), "iguales: ninguna destaca");
            Assert.That(c.rect.width, Is.EqualTo(a.rect.width).Within(0.5f));
            Assert.That(a.anchoredPosition.y, Is.GreaterThan(b.anchoredPosition.y), "<13 arriba, luego 13-17 y 18+ (en orden)");
            Assert.That(b.anchoredPosition.y, Is.GreaterThan(c.anchoredPosition.y));
            Assert.That(_screen.RejectAllButton.gameObject.activeSelf, Is.False);

            _screen.AgeButton(AgeBand.Teen).onClick.Invoke();
            Assert.That(chosen, Is.EqualTo(new[] { AgeBand.Teen }));
            Assert.That(_screen.PressBack(), Is.True, "Atrás se consume…");
            Assert.That(_screen.Visible, Is.True, "…y no la cierra");
        }

        [Test]
        public void Age_FromSettings_HasTheClose()
        {
            int closed = 0;
            _screen.ShowAge(_ => { }, () => closed++);

            Assert.That(_screen.CloseButton.gameObject.activeSelf, Is.True);
            Assert.That(_screen.PressBack(), Is.True);
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(_screen.Visible, Is.False);
        }

        [Test]
        public void Choices_StartOff_AndRejectIsAtTheSameLevelAsAccept()
        {
            var answers = new List<(ConsentAnswer, bool, bool)>();
            _screen.ShowChoices(ConsentAdsLine.Toggle, false, false, (a, an, ad) => answers.Add((a, an, ad)));

            Assert.That(_screen.View, Is.EqualTo(ConsentView.Choices));
            Assert.That(_screen.AdsToggleShown, Is.True);
            Assert.That(_screen.AdsSelected, Is.False, "consentimiento expreso: nada marcado");
            Assert.That(_screen.AnalyticsSelected, Is.False);
            Assert.That(_screen.AdsLineText, Is.Null);
            RectTransform reject = _screen.RejectRect;
            RectTransform accept = _screen.AcceptRect;
            Assert.That(reject.rect.width, Is.EqualTo(accept.rect.width).Within(0.5f), "05 §7: «Rechazar» al mismo nivel que «Aceptar»");
            Assert.That(reject.rect.height, Is.EqualTo(accept.rect.height).Within(0.5f));
            Assert.That(reject.anchoredPosition.y, Is.EqualTo(accept.anchoredPosition.y).Within(0.5f), "en la misma fila");

            _screen.RejectAllButton.onClick.Invoke();
            _screen.AcceptAllButton.onClick.Invoke();
            Assert.That(answers[0].Item1, Is.EqualTo(ConsentAnswer.RejectAll));
            Assert.That(answers[1].Item1, Is.EqualTo(ConsentAnswer.AcceptAll));
        }

        [Test]
        public void Choices_SaveMyChoice_SendsWhatIsMarked()
        {
            (ConsentAnswer, bool, bool)? answer = null;
            _screen.ShowChoices(ConsentAdsLine.Toggle, false, false, (a, an, ad) => answer = (a, an, ad));

            _screen.AdsToggleButton.onClick.Invoke();
            Assert.That(_screen.AdsSelected, Is.True);
            _screen.SaveButton.onClick.Invoke();

            Assert.That(answer, Is.EqualTo((ConsentAnswer.Save, false, true)));
        }

        [Test]
        public void Choices_FromSettings_ShowWhatWasSaved_AndCloseWithoutDeciding()
        {
            int closed = 0;
            int answered = 0;
            _screen.ShowChoices(ConsentAdsLine.Toggle, true, false, (a, an, ad) => answered++, () => closed++);

            Assert.That(_screen.AnalyticsSelected, Is.True);
            Assert.That(_screen.AdsSelected, Is.False);
            Assert.That(_screen.CloseButton.gameObject.activeSelf, Is.True);
            _screen.CloseButton.onClick.Invoke();
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(answered, Is.EqualTo(0));
            Assert.That(_screen.Visible, Is.False);
        }

        [TestCase(ConsentAdsLine.Ump, "consent_ads_ump")]
        [TestCase(ConsentAdsLine.Teen, "consent_ads_teen")]
        [TestCase(ConsentAdsLine.Strict, "consent_ads_strict")]
        public void Choices_WithoutTheAdsToggle_ExplainWhy_AndNeverSendAds(ConsentAdsLine line, string key)
        {
            (ConsentAnswer, bool, bool)? answer = null;
            _screen.ShowChoices(line, false, true, (a, an, ad) => answer = (a, an, ad));

            Assert.That(_screen.AdsToggleShown, Is.False);
            Assert.That(_screen.AdsLineText, Is.EqualTo(ConsentText.T(key)));
            _screen.SaveButton.onClick.Invoke();
            Assert.That(answer!.Value.Item3, Is.False, "sin interruptor no hay anuncios personalizados");
        }

        [Test]
        public void Ump_FromSettings_TheLineReopensTheUmpMessage()
        {
            int options = 0;
            _screen.ShowChoices(ConsentAdsLine.Ump, true, false, (a, an, ad) => { }, () => { }, () => options++);

            Assert.That(_screen.AdsOptionsShown, Is.True);
            Assert.That(_screen.AdsLineText, Is.Null, "el enlace sustituye a la línea");
            _screen.AdsOptionsButton.onClick.Invoke();
            Assert.That(options, Is.EqualTo(1));

            _screen.ShowChoices(ConsentAdsLine.Ump, true, false, (a, an, ad) => { });
            Assert.That(_screen.AdsOptionsShown, Is.False, "en el arranque, la línea: UMP sale al inicializar MAX");
        }

        [Test]
        public void AConfirmationCoveredByAnotherView_IsAnsweredNo()
        {
            StoreProduct product = StoreCatalog.Default.Get("af_coins_2");
            var answers = new List<bool>();
            _screen.Confirm(product, product.PriceIn("USD"), answers.Add);

            _screen.ShowBlocked();
            _screen.Hide();

            Assert.That(answers, Is.EqualTo(new[] { false }), "la compra nunca se queda colgada");
        }

        [Test]
        public void Blocked_HasNoButtons_AndBackDoesNotCloseIt()
        {
            _screen.ShowBlocked();

            Assert.That(_screen.View, Is.EqualTo(ConsentView.Blocked));
            Assert.That(_screen.BodyText, Is.EqualTo(ConsentText.T(ConsentText.Keys.Blocked)));
            Assert.That(_screen.CloseButton.gameObject.activeSelf, Is.False);
            Assert.That(_screen.RejectAllButton.gameObject.activeSelf, Is.False);
            Assert.That(_screen.AcceptAllButton.gameObject.activeSelf, Is.False);
            Assert.That(_screen.SaveButton.gameObject.activeSelf, Is.False);
            Assert.That(_screen.AgeButton(AgeBand.Adult).gameObject.activeSelf, Is.False);
            Assert.That(_screen.PressBack(), Is.True);
            Assert.That(_screen.Visible, Is.True);
            var ids = new List<string>();
            _screen.VisibleButtons(ids, new List<RectTransform>());
            Assert.That(ids, Is.Empty);
        }

        [Test]
        public void Confirm_BuyAndCancel_AnswerOnce()
        {
            StoreProduct product = StoreCatalog.Default.Get("af_coins_2");
            var answers = new List<bool>();
            _screen.Confirm(product, product.PriceIn("USD"), answers.Add);
            Assert.That(_screen.View, Is.EqualTo(ConsentView.Confirm));
            Assert.That(_screen.SaveButton.gameObject.activeSelf, Is.False);
            _screen.ConfirmBuyButton.onClick.Invoke();
            Assert.That(_screen.Visible, Is.False);

            _screen.Confirm(product, product.PriceIn("USD"), answers.Add);
            _screen.ConfirmCancelButton.onClick.Invoke();

            _screen.Confirm(product, product.PriceIn("USD"), answers.Add);
            Assert.That(_screen.PressBack(), Is.True);

            Assert.That(answers, Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void TheProbeSeesEveryButton()
        {
            var ids = new List<string>();
            var rects = new List<RectTransform>();
            _screen.ShowChoices(ConsentAdsLine.Toggle, false, false, (a, an, ad) => { });
            _screen.VisibleButtons(ids, rects);
            Assert.That(ids, Is.EqualTo(new[] { "consent_ads", "consent_analytics", "consent_reject_all", "consent_accept_all", "consent_save" }));
            Assert.That(rects.Count, Is.EqualTo(ids.Count));

            ids.Clear();
            _screen.ShowAge(_ => { });
            _screen.VisibleButtons(ids, new List<RectTransform>());
            Assert.That(ids, Is.EqualTo(new[] { "age_under_13", "age_13_17", "age_18_plus" }));
        }

        [Test]
        public void TheProbeLine_HasOnlyIdsAndBooleans()
        {
            ConsentFlags flags = ConsentPolicy.Flags(
                new ConsentRecord(LegalProfiles.Co1581, 1, new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc), true, false, AgeBand.Teen),
                ConsentConfig.Defaults);
            string line = DevProbe.FormatConsentState(1234, "done", flags, LegalProfiles.Co1581, "CO", CountrySource.Sim);
            Assert.That(line, Is.EqualTo(
                "AF_STATE {\"event\":\"consent\",\"t\":1234,\"step\":\"done\",\"profile\":\"co_1581\",\"country\":\"CO\",\"country_source\":\"sim\","
                + "\"age_band\":\"13-17\",\"decided\":true,\"blocked\":false,\"analytics\":true,\"npa\":true,\"ump\":false,\"tfcd\":false,"
                + "\"rating\":\"G\",\"purchases\":true,\"double_confirm\":true}"));

            ConsentFlags ump = ConsentPolicy.Flags(
                new ConsentRecord(LegalProfiles.Ump, 1, new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc), true, false, AgeBand.Adult),
                ConsentConfig.Defaults);
            StringAssert.Contains("\"npa\":false,\"ump\":true", DevProbe.FormatConsentState(1, "done", ump, LegalProfiles.Ump, "DE", CountrySource.Sim),
                "la sonda dice lo mismo que recibe MAX: con UMP no se fuerza npa");
        }
    }
}
