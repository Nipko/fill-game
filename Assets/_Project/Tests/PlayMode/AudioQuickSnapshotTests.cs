#nullable enable
using System.Collections;
using AF.Audio;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de Música y Sonido con un toque (D-046, punto 2), con la pantalla simulada de <see cref="SnapshotKit"/>
    /// (360 × 780 dp), en es-419 (<c>Logs/snapshots/es-419/</c>) y en en-XA (<c>Logs/snapshots/en-XA/</c>): <c>d046_01_home</c> (el
    /// inicio del 58 con los dos botones bajo el engranaje, el Sonido apagado), <c>d046_02_pause</c> (la pausa del 4 con la Música
    /// apagada) y <c>d046_03_pause_130</c> (la misma pausa con el texto al 130 %). Necesitan gráficos:
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
