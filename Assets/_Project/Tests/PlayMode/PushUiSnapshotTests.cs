#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de referencia de la UI de los avisos (<c>.claude/rules/localizacion.md</c>: cada cadena nueva con su captura), con la
    /// pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp) y la de 16:9 (<see cref="HudLabelTests.Screen640"/>), al 100 % y
    /// al 130 %, en es-419 (<c>Logs/snapshots/es-419/</c>) y en en-XA: <c>push_01_card_&lt;pantalla&gt;_&lt;escala&gt;</c> (la tarjeta
    /// del pre-prompt encima de la victoria del 12) y <c>push_02_settings_&lt;estado&gt;_&lt;pantalla&gt;_&lt;escala&gt;</c> (Ajustes
    /// desplazado hasta «Avisos», apagados, encendidos y bloqueados en el teléfono). Comprueba además que ningún texto baja de 12 sp.
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.PushUiSnapshotTests</c> sin
    /// <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class PushUiSnapshotTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        private FakeNotificationScheduler _scheduler = null!;

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetSessionOverrides();
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture_Es419()
        {
            string dir = SnapshotKit.Prepare();
            yield return CaptureAll(dir);
        }

        [UnityTest]
        public IEnumerator Capture_EnXA()
        {
            string dir = SnapshotKit.Prepare(LocLocale.Pseudo);
            yield return CaptureAll(dir);
        }

        private IEnumerator CaptureAll(string dir)
        {
            foreach (bool wide in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string tag = (wide ? "16x9" : "360x780") + "_" + (scale > 1f ? "130" : "100");
                    yield return CaptureCard(dir, tag, wide, scale);
                    yield return CaptureSettings(dir, tag, wide, scale);
                }
            }
        }

        private void Prime(bool wide, float scale)
        {
            var clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = clock;
            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = wide ? HudLabelTests.Screen640 : new UnityEngine.Vector4(SnapshotKit.Width, SnapshotKit.Height, 96f, 48f); // la de SnapshotKit.Prepare
            _scheduler = new FakeNotificationScheduler(clock);
            GameBootstrap.NotificationSchedulerOverride = _scheduler;
            GameBootstrap.PushSettingsStoreOverride = new InMemoryPushSettingsStore();
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1, LastSeenAt = T0.AddHours(-20) });
            GameBootstrap.PrePromptCardOverride = true;
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        private static void Save(string dir, string name, bool wide)
        {
            if (wide)
            {
                SnapshotKit.SaveTop(dir, name, (int)HudLabelTests.Screen640.y);
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }
        }

        private static void AssertMinText(System.Collections.Generic.IEnumerable<TextMeshProUGUI> texts, string where)
        {
            foreach (TextMeshProUGUI t in texts)
            {
                if (t.gameObject.activeInHierarchy && t.text.Length > 0)
                {
                    Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), where + ": «" + t.text + "» ≥ 12 sp");
                    Assert.That(t.isTextOverflowing, Is.False, where + ": «" + t.text + "» sin recortes");
                }
            }
        }

        private IEnumerator CaptureCard(string dir, string tag, bool wide, float scale)
        {
            Prime(wide, scale);
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(ProfileAt(12));
            yield return PlayModeHelpers.LoadGame(12, new MemoryProgressStore(12, 11));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.GetComponent<SessionRunner>().enabled = false;
            boot.GetComponent<MarketRunner>().enabled = false;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 2500); // la victoria entera (Siguiente a la vista)
            boot.TickSessions();
            SnapshotKit.Attach(boot);
            Assert.That(boot.PressNext(), Is.True);
            PushPrePromptCard card = boot.PrePromptCard!;
            Assert.That(card.Visible, Is.True);
            SnapshotKit.AttachOverlay(card.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            AssertMinText(card.Texts(), "tarjeta " + tag);
            Assert.That(card.Panel.rect.height, Is.LessThanOrEqualTo(wide ? 640f : 780f), "la tarjeta cabe en la pantalla");
            Save(dir, "push_01_card_" + tag, wide);
            card.Close(false);
        }

        private IEnumerator CaptureSettings(string dir, string tag, bool wide, float scale)
        {
            Prime(wide, scale);
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(12)));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            SnapshotKit.AttachOverlay(settings.Canvas);
            settings.FontScale = scale;

            foreach (PushRowState state in new[] { PushRowState.Off, PushRowState.On, PushRowState.Blocked })
            {
                _scheduler.Permission = state == PushRowState.Off ? PushPermission.NotRequested
                    : state == PushRowState.On ? PushPermission.Allowed : PushPermission.Blocked;
                if (state == PushRowState.On)
                {
                    boot.Push!.SetFromSettings(true);
                }

                boot.RefreshPushRow();
                Assert.That(settings.PushState, Is.EqualTo(state));
                settings.ScrollToPush();
                yield return SnapshotKit.Wait(0.3f);
                AssertMinText(settings.PushTexts(), "Ajustes " + state + " " + tag);
                string name = state == PushRowState.Off ? "off" : state == PushRowState.On ? "on" : "blocked";
                Save(dir, "push_02_settings_" + name + "_" + tag, wide);
            }

            settings.FontScale = 1f;
        }
    }
}
