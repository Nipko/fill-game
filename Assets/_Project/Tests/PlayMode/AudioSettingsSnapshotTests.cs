#nullable enable
using System.Collections;
using System.IO;
using AF.Audio;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de Ajustes con las filas Sonido, Música y Vibración (tarea AU1), con la pantalla simulada de
    /// <see cref="SnapshotKit"/> (360 × 780 dp), en es-419 (<c>Logs/snapshots/es-419/</c>) y en en-XA (<c>Logs/snapshots/en-XA/</c>):
    /// <c>au1_01_settings</c> (al 100 %, todo marcado), <c>au1_02_settings_130</c> (al 130 %, con Música desmarcada) y
    /// <c>au1_03_settings_130_end</c> (al 130 %, desplazada hasta el final: lo que no cabe se ve desplazando, con el fundido de arriba) y
    /// <c>au1_04_settings_130_16x9</c> (al 130 % en un teléfono 16:9, 1080 × 1920 px: el caso del visor; lo cortado se corta
    /// dentro de la crema, sin pisar el marco, con la última fila a medias y el fundido de abajo). Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.AudioSettingsSnapshotTests</c> sin <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class AudioSettingsSnapshotTests
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
        public IEnumerator CaptureAudioSettings_Es419()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir);
        }

        [UnityTest]
        public IEnumerator CaptureAudioSettings_EnXA()
        {
            string dir = SnapshotKit.Prepare(LocLocale.Pseudo);
            yield return Capture(dir);
        }

        private static IEnumerator Capture(string dir)
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            SnapshotKit.AttachOverlay(settings.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "au1_01_settings");

            settings.AudioButton(SettingsScreen.MusicId).onClick.Invoke();
            settings.FontScale = 1.3f;
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "au1_02_settings_130");

            settings.ScrollToEnd();
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "au1_03_settings_130_end");
            Assert.That(File.Exists(Path.Combine(dir, "au1_03_settings_130_end.png")), Is.True);
            settings.FontScale = 1f;

            // 16:9 al 130 % (revisión de AU1): la tarjeta recién abierta, con el visor dentro de la crema.
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressSettings(), Is.True);
            settings = boot.SettingsView!;
            SnapshotKit.AttachOverlay(settings.Canvas);
            settings.FontScale = 1.3f;
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(settings.Scrolls, Is.True, "en 16:9 al 130 % se desplaza");
            SnapshotKit.SaveTop(dir, "au1_04_settings_130_16x9", (int)HudLabelTests.Screen640.y);
            settings.FontScale = 1f;
        }
    }
}
