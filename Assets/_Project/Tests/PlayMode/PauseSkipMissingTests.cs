#nullable enable
using System;
using System.Collections;
using System.Text.RegularExpressions;
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
    /// Observación 2 de la jornada 6 en el teléfono (2026-10-28): en el 46, con 0 semillas y sin tienda, «Salto del Boto» en gris en
    /// la pausa no respondía al toque (ni latido, ni <c>missing_tap</c>, ni aviso); el arreglo de la jornada 5 (observación 6) no
    /// cubría <see cref="PauseMenu"/>. Ahora responde como el del modal de atasco: late su etiqueta (no tiene línea «Te faltan»;
    /// con Reducir movimiento, solo se atenúa), lo anuncia con TalkBack, avisa si la tienda no vende y escribe
    /// <c>AF_STATE missing_tap</c> con <c>pause_skip</c>; sin abrir la confirmación ni cobrar. Lo mismo el banner del Salto en la
    /// entrada del nivel (<see cref="SkipBanner"/>, <c>banner_skip</c>), que también era un toque mudo. En el 31 con el tablero 6
    /// del tutorial, como <see cref="SkipPlayModeTests"/>.
    /// </summary>
    public sealed class PauseSkipMissingTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameBootstrap.ResetSkipOverrides();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.A11yHostOverride = null;
            GameBootstrap.ResetSkipOverrides();
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Pause_TheGraySkip_PulsesItsLabel_AndOpensNothing([Values(true, false)] bool storeOn)
        {
            GameBootstrap boot = null!;
            yield return LoadSkipEligible(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            boot.Services!.StoreLedger.Config.BizIapEnabled = storeOn;
            Assert.That(boot.Flow!.SkipOffer.State, Is.EqualTo(SkipState.Disabled), "condición: elegible sin saldo");

            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.True);
            Assert.That(view.Pause.SkipButton.gameObject.activeSelf, Is.True, "04 §2.6: Rosa también en la pausa");
            Assert.That(view.Pause.SkipButton.GetComponent<ReliefButton>().Interactable, Is.False, "en gris");
            Assert.That(view.Pause.SkipButton.IsInteractable(), Is.True, "pero el toque llega");
            Assert.That(view.Pause.SkipMissingSeeds, Is.EqualTo(300));

            LogAssert.Expect(LogType.Log, new Regex(@"^AF_STATE \{""event"":""missing_tap"",.*""option"":""pause_skip"",""pulsed"":true,""store"":"
                + (storeOn ? "true" : "false") + @",""level"":31\}$"));
            view.Pause.SkipButton.onClick.Invoke();
            Assert.That(view.Pause.SkipPulse.Running, Is.True, "sin línea «Te faltan», late su etiqueta");
            Assert.That(view.Pause.Visible, Is.True, "la pausa sigue abierta");
            Assert.That(boot.SkipConfirmView == null || !boot.SkipConfirmView.Visible, Is.True, "no abre la confirmación");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(31));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(FunnelSetup.Purchases.Presented, Is.EqualTo(0), "ninguna compra");
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(storeOn ? 0 : 1), "el aviso solo si la tienda no vende");

            float max = 1f;
            for (int ms = 0; ms < NotePulse.DurationMs; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                max = Mathf.Max(max, view.Pause.SkipPulse.ScaleNow);
            }

            Assert.That(max, Is.GreaterThan(1.04f).And.LessThanOrEqualTo(NotePulse.MaxScale + 1e-4f), "el latido de la burbuja de un comodín");
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Pause.SkipPulse.Running, Is.False);
            Assert.That(view.Pause.SkipPulse.ScaleNow, Is.EqualTo(1f), "vuelve a reposo");
        }

        [UnityTest]
        public IEnumerator Pause_TheGraySkip_WithReduceMotion_Fades_AndTalkBackAnnouncesTheMissingSeeds()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            Profile profile = FunnelSetup.Profile(0);
            profile.Settings.ReduceMotion = ReduceMotionSetting.On;
            GameBootstrap boot = null!;
            yield return LoadSkipEligible(profile, b => boot = b);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.ReduceMotion, Is.True);

            view.PauseButton.onClick.Invoke();
            float rest = view.Pause.SkipPulse.AlphaNow;
            view.Pause.SkipButton.onClick.Invoke();
            Assert.That(host.Announcements[host.Announcements.Count - 1], Is.EqualTo(UiText.MissingSeeds(300)), "«Te faltan 300 semillas»");
            Assert.That(view.Pause.SkipPulse.Running, Is.True);
            float minAlpha = 1f;
            for (int ms = 0; ms < NotePulse.DurationMs; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.Pause.SkipPulse.ScaleNow, Is.EqualTo(1f), "con Reducir movimiento, sin latido (02 §12)");
                minAlpha = Mathf.Min(minAlpha, view.Pause.SkipPulse.AlphaNow);
            }

            Assert.That(minAlpha, Is.LessThan(rest * 0.6f).And.GreaterThanOrEqualTo((rest * NotePulse.ReducedAlpha) - 1e-3f), "se atenúa");
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Pause.SkipPulse.AlphaNow, Is.EqualTo(rest), "y vuelve");
        }

        [UnityTest]
        public IEnumerator Pause_ClosingMidPulse_LeavesTheLabelAtRest_AndWithSeedsTheSkipStillOpensTheConfirmation()
        {
            GameBootstrap boot = null!;
            yield return LoadSkipEligible(FunnelSetup.Profile(0), b => boot = b);
            GameScreen view = boot.View!;
            view.PauseButton.onClick.Invoke();
            view.Pause.SkipButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs * 4);
            Assert.That(view.Pause.SkipPulse.ScaleNow, Is.GreaterThan(1f), "a medio latido");
            view.Pause.ResumeButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.False);
            Assert.That(view.Pause.SkipPulse.Running, Is.False, "Hide deja la etiqueta en reposo");
            Assert.That(view.Pause.SkipPulse.ScaleNow, Is.EqualTo(1f));

            // Llegan las semillas: la pausa vuelve a abrir el Salto listo, que abre la confirmación y no late.
            Assert.That(boot.Wallet!.Apply(AF.Economy.Transaction.Earn(AF.Economy.SeedSource.Calendar, 1000)).IsApplied, Is.True);
            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.SkipButton.GetComponent<ReliefButton>().Interactable, Is.True, "con saldo, listo");
            Assert.That(view.Pause.SkipMissingSeeds, Is.Zero);
            int starts = view.Pause.SkipPulse.Starts;
            view.Pause.SkipButton.onClick.Invoke();
            Assert.That(view.Pause.SkipPulse.Starts, Is.EqualTo(starts), "no late");
            Assert.That(view.Pause.Visible, Is.False);
            Assert.That(boot.SkipConfirmView!.Visible, Is.True, "abre la confirmación de siempre");
        }

        [UnityTest]
        public IEnumerator Banner_TheGraySkip_PulsesTheMissingLine_AndOpensNothing([Values(true, false)] bool storeOn)
        {
            GameBootstrap boot = null!;
            yield return LoadSkipEligible(FunnelSetup.Profile(0), b => boot = b, false);
            boot.Services!.StoreLedger.Config.BizIapEnabled = storeOn;
            boot.TickSkipBanner();
            SkipBanner banner = boot.SkipBannerView!;
            Assert.That(banner.Visible, Is.True, "elegible sin saldo: en gris");
            Assert.That(banner.Interactable, Is.False, "no está lista");
            Assert.That(banner.Button.IsInteractable(), Is.True, "pero el toque llega");
            Assert.That(banner.MissingText, Is.EqualTo(UiText.MissingSeeds(300)));

            LogAssert.Expect(LogType.Log, new Regex(@"^AF_STATE \{""event"":""missing_tap"",.*""option"":""banner_skip"",""pulsed"":true,""store"":"
                + (storeOn ? "true" : "false") + @",""level"":31\}$"));
            banner.Button.onClick.Invoke();
            Assert.That(banner.Pulse.Running, Is.True, "late «Te faltan {n} semillas»");
            Assert.That(boot.SkipConfirmView!.Visible, Is.False, "no abre la confirmación");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(0));
            Assert.That(boot.StoreUnavailableNoticeCount, Is.EqualTo(storeOn ? 0 : 1), "el aviso solo si la tienda no vende");

            float max = 1f;
            for (int ms = 0; ms < NotePulse.DurationMs; ms += PlayModeHelpers.FrameMs)
            {
                boot.TickSkipBanner(); // el banner se recoloca cada fotograma: el latido no se pierde
                banner.Tick(PlayModeHelpers.FrameMs);
                max = Mathf.Max(max, banner.Pulse.ScaleNow);
            }

            Assert.That(max, Is.GreaterThan(1.04f));
            banner.Tick(100f);
            Assert.That(banner.Pulse.Running, Is.False);
            Assert.That(banner.Pulse.ScaleNow, Is.EqualTo(1f), "vuelve a reposo");
        }

        // El 31 con el tablero 6 del tutorial y el Salto elegible tras cinco fallos (04 §2.6), con Canoas infinitas para que
        // «Rendirse» no pregunte. Con playing, el tablero en juego; si no, en la tarjeta previa (el banner).
        private static IEnumerator LoadSkipEligible(Profile profile, Action<GameBootstrap> done, bool playing = true)
        {
            profile.Wallet.Canoes.InfiniteUntil = FunnelSetup.T0 + TimeSpan.FromHours(2);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            for (int fail = 0; fail < 5; fail++)
            {
                FunnelSetup.GetStuck(boot);
                Assert.That(boot.Flow!.PressGiveUp(), Is.True);
                Assert.That(boot.Flow.PressRetry(), Is.True);
                if (fail < 4 || playing)
                {
                    boot.TapAnywhere();
                    PlayModeHelpers.Step(boot, 50);
                }
            }

            Assert.That(boot.Flow!.SkipOffer.Offered, Is.True, "el Salto, elegible al quinto fallo");
            done(boot);
        }
    }
}
