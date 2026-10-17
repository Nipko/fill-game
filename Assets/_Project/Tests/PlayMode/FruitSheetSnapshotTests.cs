#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la ficha de una fruta y de «Sobre la Amazonía» en Ajustes (D-017 Q4-A; tarea F4), en es-419 a 360 × 780 dp y
    /// al 100 % (<c>Logs/snapshots/es-419/</c>) y en en-XA a 360 × 640 dp y al 130 %, el peor caso (<c>Logs/snapshots/en-XA/</c>):
    /// <list type="bullet">
    /// <item><c>f4_01_sheet_from_victory</c>: la ficha del açaí abierta con un toque en la línea de la victoria del 3, encima de la
    /// victoria; <c>f4_02_victory_after_sheet</c>: la victoria al cerrarla, igual que antes;</item>
    /// <item><c>f4_03_sheet_boto</c>: el agua del Tapajós con 21-24 superados y el 27 saltado con el Salto del Boto (revelado,
    /// numerado) y el resto en silueta con «Nivel {n}»;</item>
    /// <item><c>f4_04_sheet_sealed</c> y <c>f4_05_sheet_sealed_sources</c>: el açaí completo (sello y nombre científico) y, al final
    /// del desplazamiento, «Fuentes»;</item>
    /// <item><c>f4_06_sheet_unstarted</c>: el achiote sin empezar (su frase de «Fruta nueva» y «Su serie empieza en el nivel 41»);</item>
    /// <item><c>f4_07_settings_about</c>: Ajustes desplazada hasta «Sobre la Amazonía» con las frutas (en silueta las que no
    /// empezaron) y <c>f4_08_settings_sheet</c>: la ficha abierta desde ahí, encima de Ajustes.</item>
    /// </list>
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.FruitSheetSnapshotTests</c>
    /// sin <c>-nographics</c>). Falla si un texto de la ficha baja de 12 sp por la escala o se recorta
    /// (<see cref="FruitSheetTests.AssertFits"/>).
    /// </summary>
    [Explicit("Capturas de la ficha de Frutas del río (F4); necesitan dispositivo gráfico.")]
    public sealed class FruitSheetSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureFruitSheet_Es419_780_100()
        {
            yield return Capture(LocLocale.SpanishLatinAmerica, false);
        }

        [UnityTest]
        public IEnumerator CaptureFruitSheet_EnXA_640_130()
        {
            yield return Capture(LocLocale.Pseudo, true);
        }

        private static IEnumerator Capture(string locale, bool worst)
        {
            string suffix = worst ? "_640_130" : "_780_100";
            float scale = worst ? 1.3f : 1f;

            // Desde la victoria del 3: un toque en la línea, la ficha encima, y al cerrarla la victoria como estaba.
            FunnelSetup.Hooks();
            string dir = SnapshotKit.Prepare(locale);
            if (worst)
            {
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            }

            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(3, new MemoryProgressStore(3, 2));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.TickExact(boot, 1500);
            SnapshotKit.Attach(boot);
            FactLine line = boot.View!.FruitLine!;
            Assert.That(line.Touchable, Is.True);
            line.TapArea!.GetComponent<Button>().onClick.Invoke();
            FruitSheet sheet = boot.FruitSheetView!;
            Assert.That(sheet.Visible, Is.True);
            SnapshotKit.AttachOverlay(sheet.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            FruitSheetTests.AssertFits(sheet, "ficha del açaí desde la victoria" + suffix);
            SnapshotKit.Save(dir, "f4_01_sheet_from_victory" + suffix);
            Assert.That(boot.PressAndroidBack(), Is.True);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "f4_02_victory_after_sheet" + suffix);

            FactCatalog catalog = boot.FruitFacts!.Catalog;
            var boto = new MemoryProgressStore(28, 24);
            boto.MarkSkipped(27);
            yield return Sheet(dir, sheet, catalog.SeriesOf("agua_tapajos")!, catalog, boto, "f4_03_sheet_boto" + suffix, false);
            var done = new MemoryProgressStore(25, 24);
            yield return Sheet(dir, sheet, catalog.SeriesOf("acai")!, catalog, done, "f4_04_sheet_sealed" + suffix, false);
            yield return Sheet(dir, sheet, catalog.SeriesOf("acai")!, catalog, done, "f4_05_sheet_sealed_sources" + suffix, true);
            yield return Sheet(dir, sheet, catalog.SeriesOf("urucum")!, catalog, done, "f4_06_sheet_unstarted" + suffix, false);

            // Ajustes › Sobre la Amazonía, y la ficha desde ahí.
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(25)));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.AttachLobby(boot);
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            settings.FontScale = scale;
            Assert.That(settings.AboutShown, Is.True);
            SnapshotKit.AttachOverlay(settings.Canvas);
            settings.ScrollToEnd();
            yield return SnapshotKit.Wait(0.3f);
            SettingsScreenTests.AssertFits(settings, "Ajustes con Sobre la Amazonía" + suffix);
            SnapshotKit.Save(dir, "f4_07_settings_about" + suffix);
            settings.FruitButton("agua_tapajos")!.onClick.Invoke();
            sheet = boot.FruitSheetView!;
            Assert.That(sheet.Visible, Is.True);
            SnapshotKit.AttachOverlay(sheet.Canvas);
            sheet.Canvas.planeDistance = 4f; // delante de Ajustes
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, "f4_08_settings_sheet" + suffix);
            Debug.Log("AF_F4 {\"locale\":\"" + locale + "\",\"scale\":" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ",\"settings_fruits\":" + settings.FruitCount + ",\"settings_scrolls\":" + (settings.Scrolls ? "true" : "false") + "}");
        }

        private static IEnumerator Sheet(string dir, FruitSheet sheet, FactSeries series, FactCatalog catalog, MemoryProgressStore progress, string name, bool end)
        {
            sheet.Hide();
            Assert.That(sheet.Show(series, catalog, progress, FruitSheetSource.Settings), Is.True, name);
            SnapshotKit.AttachOverlay(sheet.Canvas);
            if (end)
            {
                yield return null;
                sheet.ScrollToEnd();
            }

            yield return SnapshotKit.Wait(0.3f);
            FruitSheetTests.AssertFits(sheet, name);
            SnapshotKit.Save(dir, name);
            sheet.Hide();
        }
    }
}
