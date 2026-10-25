#nullable enable
using System.Collections;
using AF.Audio;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de Música y Sonido con un toque (D-046, punto 2), con la pantalla simulada de <see cref="SnapshotKit"/>
    /// (360 × 780 dp), en es-419 (<c>Logs/snapshots/es-419/</c>) y en en-XA (<c>Logs/snapshots/en-XA/</c>): <c>d046_01_home</c> (el
    /// inicio del 58 con los dos botones bajo el engranaje, el Sonido apagado), <c>d046_02_pause</c> (la pausa del 4 con la Música
    /// apagada) y <c>d046_03_pause_130</c> (la misma pausa con el texto al 130 %); y <c>t6p_home320_100</c> y <c>t6p_home320_130</c>
    /// (T6, carril B, punto 1: el inicio del 58 en un lienzo de 320 dp, 1080 px a 3,375 px/dp, con la tarjeta del Reto de 272 dp
    /// junto a la Crecida, el Cántaro y Canoas, al 100 % y al 130 %). Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.AudioQuickSnapshotTests</c> sin <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class AudioQuickSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            AudioCues.Override = new FakeAudioService();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            GameScreen.SimulatedScreen = null;
            SnapshotKit.Release();
            AudioCues.Override = null;
            AudioCues.Install(null);
        }

        [UnityTest]
        public IEnumerator CaptureAudioQuick_Es419()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir);
        }

        [UnityTest]
        public IEnumerator CaptureAudioQuick_EnXA()
        {
            string dir = SnapshotKit.Prepare(LocLocale.Pseudo);
            yield return Capture(dir);
        }

        [UnityTest]
        public IEnumerator CaptureHome320([Values("es-419", "en-XA")] string locale, [Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(locale);
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            GameScreen.SimulatedPxPerDp = 3.375f; // 320 dp de ancho con los 1080 px de la captura
            LaunchOptions.FontScaleOverride = percent / 100f;
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            try
            {
                yield return AudioQuickTogglesTests.LoadRichLobby();
                GameBootstrap boot = GameBootstrap.Current!;
                HomeSloth sloth = boot.Lobby!.HomeWindow.Sloth!;
                AudioQuickTogglesTests.Tick(sloth, 400);
                AudioQuickTogglesTests.Tick(sloth, (int)sloth.BubbleDurationMs + 300);
                boot.TickDaily(16f);
                SnapshotKit.AttachLobby(boot);
                SnapshotKit.AttachOverlay(boot.DailyCard!.Root.GetComponentInParent<Canvas>().rootCanvas);
                if (boot.MarketIcons != null)
                {
                    SnapshotKit.AttachOverlay(boot.MarketIcons.Canvas);
                }

                if (boot.LogbookCard != null)
                {
                    SnapshotKit.AttachOverlay(boot.LogbookCard.Root.GetComponentInParent<Canvas>().rootCanvas);
                }

                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, "t6p_home320_" + percent);
            }
            finally
            {
                GameScreen.SimulatedPxPerDp = null;
            }
        }

        private static IEnumerator Capture(string dir)
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(58)));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            boot.Lobby!.Audio.SoundButton.onClick.Invoke();
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "d046_01_home");

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return Canvas16x9LayoutTests.LoadPlaying(4);
            boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.PauseButton.onClick.Invoke();
            view.Pause.Audio.MusicButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "d046_02_pause");

            view.Pause.ResumeButton.onClick.Invoke();
            LaunchOptions.FontScaleOverride = 1.3f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return Canvas16x9LayoutTests.LoadPlaying(4);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            view.PauseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "d046_03_pause_130");
        }
    }
}
