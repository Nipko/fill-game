#nullable enable
using System;
using System.Collections;
using System.Text.RegularExpressions;
using AF.Ads;
using AF.Boosters;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// «+1 cuia» de la bandeja sin stock y sin saldo (el precio en rojo y «Te faltan {n}» en la ranura; 04 §2.2) era un toque mudo
    /// (petición del 2026-10-29 tras la observación 2 de la jornada 6: ningún botón con «Te faltan» sin respuesta). Ahora late esa
    /// burbuja como la de Pista (D-022; con Reducir movimiento, solo el color), lo dice TalkBack, avisa si la tienda no vende y
    /// escribe <c>AF_STATE missing_tap</c> con <c>tray_add_cuia</c>, sin cobrar ni añadir cuias. En el 106 con 50 semillas y sin
    /// video, como <see cref="BoosterTrayTests"/>.
    /// </summary>
    public sealed class TrayExtraMissingTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.A11yHostOverride = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator TheRedAddCuia_PulsesItsMissingBubble_AndChargesNothing([Values(true, false)] bool storeOn)
        {
            GameBootstrap boot = null!;
            yield return Load106(FunnelSetup.Profile(50), b => boot = b);
            boot.Services!.StoreLedger.Config.BizIapEnabled = storeOn;
            GameScreen view = boot.View!;
            BoosterTray tray = view.Tray;
            ExtraCuiaOffer offer = boot.Flow!.ExtraCuia;
            Assert.That(offer.State, Is.EqualTo(ExtraCuiaState.Ready), "condición: lista, pero sin saldo");
            Assert.That(offer.Missing, Is.EqualTo(100));
            Assert.That(tray.ExtraShowsMissing, Is.True, "«Te faltan 100» en la ranura");
            int cuias = view.Views.Length;

            LogAssert.Expect(LogType.Log, new Regex(@"^AF_STATE \{""event"":""missing_tap"",.*""option"":""tray_add_cuia"",""pulsed"":true,""store"":"
                + (storeOn ? "true" : "false") + @",""level"":106\}$"));
            tray.ExtraCuiaButton.onClick.Invoke();
            Assert.That(tray.ExtraMissingPulsing, Is.True, "late «Te faltan {n}»");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(50), "no cobra nada");
            Assert.That(view.Views.Length, Is.EqualTo(cuias), "ninguna cuia añadida");
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(0));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(storeOn ? 0 : 1), "el aviso solo si la tienda no vende");

            float max = 1f;
            for (int ms = 0; ms < 2f * BoosterTray.MissingPulseBeatMs; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                max = Mathf.Max(max, tray.ExtraMissingScaleNow);
            }

            Assert.That(max, Is.GreaterThan(1.04f).And.LessThanOrEqualTo(BoosterTray.MissingPulseScale + 1e-4f), "el latido de Pista");
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(tray.ExtraMissingPulsing, Is.False);
            Assert.That(tray.ExtraMissingScaleNow, Is.EqualTo(1f), "vuelve a reposo");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_OnlyTheColor_AndTalkBackAnnouncesTheMissingSeeds()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            Profile profile = FunnelSetup.Profile(50);
            profile.Settings.ReduceMotion = ReduceMotionSetting.On;
            GameBootstrap boot = null!;
            yield return Load106(profile, b => boot = b);
            BoosterTray tray = boot.View!.Tray;
            Assert.That(boot.Flow!.ReduceMotion, Is.True);

            tray.ExtraCuiaButton.onClick.Invoke();
            Assert.That(host.Announcements[host.Announcements.Count - 1], Is.EqualTo(UiText.MissingSeeds(100)), "«Te faltan 100 semillas»");
            Assert.That(tray.ExtraMissingPulsing, Is.True);
            float color = 0f;
            for (int i = 0; i < 20; i++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(tray.ExtraMissingScaleNow, Is.EqualTo(1f), "con Reducir movimiento, sin latido (02 §12)");
                color = Mathf.Max(color, tray.ExtraMissingColorNow);
            }

            Assert.That(color, Is.GreaterThan(0.9f), "solo el color");
        }

        [UnityTest]
        public IEnumerator WithSeeds_TheAddCuiaStillAddsACuia()
        {
            GameBootstrap boot = null!;
            yield return Load106(FunnelSetup.Profile(1000), b => boot = b);
            GameScreen view = boot.View!;
            int cuias = view.Views.Length;
            view.Tray.ExtraCuiaButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Tray.ExtraMissingPulsing, Is.False, "no late");
            Assert.That(boot.Wallet!.Seeds, Is.LessThan(1000), "la paga");
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1), "y añade la cuia");
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(0));
        }

        /// <summary>
        /// TalkBack en la bandeja (02 §12: etiqueta + estado): cada botón con su burbuja «Te faltan {n}» a la vista se lee con
        /// «Te faltan {n} semillas» unido a su etiqueta (la Cuia extra y la Luciérnaga en el 106 con 50 semillas), y la burbuja no se
        /// lee suelta (no dos veces).
        /// </summary>
        [UnityTest]
        public IEnumerator TalkBack_ReadsEachTrayButton_WithItsMissingSeeds_AndNotTheBubbleAgain()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            GameBootstrap boot = null!;
            yield return Load106(FunnelSetup.Profile(50), b => boot = b);
            BoosterTray tray = boot.View!.Tray;
            Assert.That(tray.ExtraShowsMissing, Is.True);
            Assert.That(tray.RevealShowsMissing, Is.True);
            Assert.That(tray.RevealMissingSeeds, Is.EqualTo(70));
            boot.A11y!.Refresh();
            var items = new System.Collections.Generic.List<AF.UI.A11yItem>(host.Items);

            string extra = LabelOf(items, tray.ExtraCuiaButton);
            Assert.That(extra, Is.EqualTo(A11yText.Append(tray.ExtraLabel.text, UiText.MissingSeeds(100))), "«+1 cuia, Te faltan 100 semillas»");
            string reveal = LabelOf(items, tray.RevealButton);
            Assert.That(reveal, Is.EqualTo(A11yText.Append(tray.RevealLabel.text, UiText.MissingSeeds(70))), "la Luciérnaga");
            foreach (string seeds in new[] { tray.ExtraMissingText, tray.RevealMissingText })
            {
                foreach (AF.UI.A11yItem item in items)
                {
                    Assert.That(item.Label, Is.Not.EqualTo(seeds), "la burbuja «" + seeds + "» no se lee suelta");
                }
            }

            // Pista y Pororoca, con su burbuja, igual (sin saldo y sin video: BoosterTray la enseña si su precio no alcanza).
            if (tray.HintShowsMissing)
            {
                Assert.That(LabelOf(items, tray.HintButton), Does.EndWith(UiText.MissingSeeds(tray.HintMissingSeeds)), "Pista");
            }

            if (tray.ShuffleShowsMissing)
            {
                Assert.That(LabelOf(items, tray.ShuffleButton), Does.EndWith(UiText.MissingSeeds(tray.ShuffleMissingSeeds)), "la Pororoca");
            }
        }

        private static string LabelOf(System.Collections.Generic.List<AF.UI.A11yItem> items, UnityEngine.UI.Button button)
        {
            string id = "btn_" + button.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (AF.UI.A11yItem item in items)
            {
                if (item.Id == id)
                {
                    return item.Label;
                }
            }

            Assert.Fail("sin elemento para «" + button.name + "»");
            return string.Empty;
        }

        // El 106 con el tablero en juego y sin video (el precio de la Cuia extra en la ranura).
        private static IEnumerator Load106(Profile profile, Action<GameBootstrap> done)
        {
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(106, new MemoryProgressStore(106, 105));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1000);
            done(boot);
        }
    }
}
