#nullable enable
using System.Collections;
using System.IO;
using AF.IAP;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de Ajustes (tarea L3) con la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp), en es-419
    /// (<c>Logs/snapshots/es-419/</c>) y en en-XA (<c>Logs/snapshots/en-XA/</c>): <c>l3_01_settings</c> (Ajustes sobre el inicio,
    /// con Automático marcado), <c>l3_02_settings_on_130</c> (Patrones «Siempre» marcado y la fuente al 130 %),
    /// <c>l3_03_store_sheet</c> (la hoja «Compra simulada» del Puñado con su nombre de la tabla) y, solo en es-419,
    /// <c>l3_04_lobby_rebuilt_en-XA</c> (el inicio reconstruido tras elegir en-XA en Ajustes: todo entre corchetes). Necesitan
    /// gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.SettingsSnapshotTests</c> sin
    /// <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class SettingsSnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureSettings_Es419()
        {
            string dir = SnapshotKit.Prepare();
            yield return Capture(dir, true);
        }

        [UnityTest]
        public IEnumerator CaptureSettings_EnXA()
        {
            string dir = SnapshotKit.Prepare(LocLocale.Pseudo);
            yield return Capture(dir, false);
        }

        private static IEnumerator Capture(string dir, bool rebuild)
        {
            var store = new MemoryProfileStore(SettingsScreenTests.ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            SnapshotKit.AttachOverlay(settings.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "l3_01_settings");

            settings.PatternsButton(PatternsSetting.On).onClick.Invoke();
            settings.FontScale = 1.3f;
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "l3_02_settings_on_130");
            settings.FontScale = 1f;

            Assert.That(boot.CloseSettings(), Is.True);
            boot.Store!.Purchase(new PurchaseRequest(StoreCatalog.Handful, PurchaseContext.Shop, 4), _ => { });
            StoreSheet sheet = boot.StoreSheetView!;
            Assert.That(sheet.Visible, Is.True);
            SnapshotKit.AttachOverlay(sheet.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "l3_03_store_sheet");
            sheet.Close(false);

            if (rebuild)
            {
                boot.PressSettings();
                boot.SettingsView!.LanguageButton(LocLocale.Pseudo)!.onClick.Invoke();
                yield return PlayModeHelpers.WaitFor(
                    () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AF.GameFlow.AppScreen.Boot,
                    "reconstrucción en en-XA");
                SnapshotKit.AttachLobby(GameBootstrap.Current!);
                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, "l3_04_lobby_rebuilt_en-XA");
                Assert.That(File.Exists(Path.Combine(dir, "l3_04_lobby_rebuilt_en-XA.png")), Is.True);
            }
        }
    }
}
