#nullable enable
using AF.LiveOps;
using AF.Platform;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Reglas de la UI de los avisos (06 §8.1; nota de B6 §3 y §6): el estado y la acción de la fila «Avisos» de Ajustes, cuándo la
    /// tarjeta del pre-prompt puede salir (banda &lt;13, edad o consentimiento pendientes, una por sesión) y sus textos.
    /// </summary>
    public sealed class PushUiRulesTests
    {
        [Test]
        public void RowState_FollowsTheServiceTheBandGrantedAndTheSystem()
        {
            Assert.That(PushUiRules.RowState(false, false, true, PushPermission.Allowed), Is.EqualTo(PushRowState.Hidden), "sin servicio de avisos, sin fila");
            Assert.That(PushUiRules.RowState(true, true, true, PushPermission.Allowed), Is.EqualTo(PushRowState.Hidden), "B12: con <13, sin fila");
            Assert.That(PushUiRules.RowState(true, false, true, PushPermission.Allowed), Is.EqualTo(PushRowState.On));
            Assert.That(PushUiRules.RowState(true, false, false, PushPermission.Allowed), Is.EqualTo(PushRowState.Off), "el sistema los deja pero el jugador no dijo que sí");
            Assert.That(PushUiRules.RowState(true, false, true, PushPermission.Denied), Is.EqualTo(PushRowState.Off), "granted sin permiso no es encendido");
            Assert.That(PushUiRules.RowState(true, false, false, PushPermission.NotRequested), Is.EqualTo(PushRowState.Off));
            Assert.That(PushUiRules.RowState(true, false, false, PushPermission.Blocked), Is.EqualTo(PushRowState.Blocked));
            Assert.That(PushUiRules.RowState(true, false, true, PushPermission.Blocked), Is.EqualTo(PushRowState.Blocked));
        }

        [Test]
        public void ActionFor_EachState()
        {
            Assert.That(PushUiRules.ActionFor(PushRowState.On), Is.EqualTo(PushRowAction.TurnOff));
            Assert.That(PushUiRules.ActionFor(PushRowState.Off), Is.EqualTo(PushRowAction.TurnOn));
            Assert.That(PushUiRules.ActionFor(PushRowState.Blocked), Is.EqualTo(PushRowAction.OpenPhoneSettings));
            Assert.That(PushUiRules.ActionFor(PushRowState.Hidden), Is.EqualTo(PushRowAction.None));
        }

        [Test]
        public void CanOffer_NeedsAPresenter_NoUnder13_NoPendingConsent_AndOnePerSession()
        {
            Assert.That(PushUiRules.CanOffer(true, false, false, false, 2, 0), Is.True);
            Assert.That(PushUiRules.CanOffer(false, false, false, false, 2, 0), Is.False, "sin tarjeta, nada");
            Assert.That(PushUiRules.CanOffer(true, true, false, false, 2, 0), Is.False, "B12: con <13 nunca sale");
            Assert.That(PushUiRules.CanOffer(true, false, true, false, 2, 0), Is.False, "nunca sobre la edad o el consentimiento");
            Assert.That(PushUiRules.CanOffer(true, false, false, true, 2, 0), Is.False, "con otro pre-prompt o el diálogo nativo abiertos");
            Assert.That(PushUiRules.CanOffer(true, false, false, false, 2, 2), Is.False, "como mucho uno por sesión");
            Assert.That(PushUiRules.CanOffer(true, false, false, false, 3, 2), Is.True, "en la sesión siguiente, sí");
            Assert.That(PushUiRules.CanOffer(true, false, false, false, 0, 0), Is.False, "sin sesión contada");
        }

        [Test]
        public void Texts_HaveTheirKeys_AndOneFullPhrasePerState()
        {
            Assert.That(PushText.AllKeys, Does.Contain("settings_notifications").And.Contain("settings_notifications_blocked"));
            Assert.That(PushText.AllKeys, Is.Unique);
            foreach (string key in PushText.AllKeys)
            {
                Assert.That(LocSetUpFixture.Catalog.Strings.Contains(key), Is.True, key + " en strings.csv");
            }

            Assert.That(PushText.State(PushRowState.Hidden), Is.Empty);
            Assert.That(PushText.A11yLabel(PushRowState.On), Is.Not.EqualTo(PushText.A11yLabel(PushRowState.Off)));
            Assert.That(PushText.A11yLabel(PushRowState.Blocked), Is.Not.EqualTo(PushText.A11yLabel(PushRowState.Off)));
            Assert.That(PrePromptRequest.BodyKey, Is.EqualTo("push_preprompt_body"));
        }
    }
}
