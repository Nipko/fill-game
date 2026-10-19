#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la tarea FNT1 (fuentes recortadas sin perder ningún glifo), para comparar antes y después: las pantallas con
    /// más texto en es-419 a 360 × 780 dp y al 100 % (<c>Logs/snapshots/es-419/</c>) y en en-XA a 360 × 640 dp y al 130 %, el
    /// peor caso (<c>Logs/snapshots/en-XA/</c>):
    /// <list type="bullet">
    /// <item><c>fnt1_01_inicio</c>: el inicio con 1.240 semillas y el 25 por jugar;</item>
    /// <item><c>fnt1_02_ajustes_frutas</c>: Ajustes desplazada hasta «Sobre la Amazonía», con las frutas;</item>
    /// <item><c>fnt1_03_tarjeta_parada</c>: la tarjeta de Manaos en el Mapa del río (max 58);</item>
    /// <item><c>fnt1_04_victoria_fruta</c>: la victoria del 3 con la línea de fruta («Açaí · 3/20»).</item>
    /// </list>
    /// En cada captura comprueba que ningún carácter visible de un texto con una fuente del juego sale de la reserva de TMP ni
    /// como □ (<see cref="FontGlyphSnapshotTests.Scan"/>). Las de 360 × 640 se recortan a los 1.920 px de arriba.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.FontTrimSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de las fuentes recortadas (FNT1); necesitan dispositivo gráfico.")]
    public sealed class FontTrimSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator Capture_Es419_780_100()
        {
            yield return Walk(LocLocale.SpanishLatinAmerica, false);
        }

        [UnityTest]
        public IEnumerator Capture_EnXA_640_130()
        {
            yield return Walk(LocLocale.Pseudo, true);
        }

        private static IEnumerator Walk(string locale, bool worst)
        {
            FunnelSetup.Hooks(); // antes de Prepare: limpia los ganchos y fijaría es-419
            string dir = SnapshotKit.Prepare(locale);
            if (worst)
            {
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            }

            float scale = worst ? 1.3f : 1f;
            var problems = new List<string>();

            // Inicio.
            LaunchOptions.FontScaleOverride = scale;
            Profile home = SettingsScreenTests.ProfileAt(25);
            home.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(home));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.AttachLobby(boot);
            yield return SnapshotKit.Wait(0.3f);
            Save(dir, "fnt1_01_inicio", worst);
            FontGlyphSnapshotTests.Scan("fnt1_01_inicio", problems);

            // Ajustes › Sobre la Amazonía, con las frutas.
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            settings.FontScale = scale;
            Assert.That(settings.AboutShown, Is.True);
            SnapshotKit.AttachOverlay(settings.Canvas);
            settings.ScrollToAbout();
            yield return SnapshotKit.Wait(0.3f);
            Save(dir, "fnt1_02_ajustes_frutas", worst);
            FontGlyphSnapshotTests.Scan("fnt1_02_ajustes_frutas", problems);

            // Tarjeta de la parada de Manaos en el Mapa.
            LaunchOptions.FontScaleOverride = scale;
            yield return MapMarkersTests.OpenApp(59);
            boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Assert.That(map.OpenStopCard(5), Is.True, "la tarjeta de Manaos");
            map.Tick(16f);
            yield return SnapshotKit.Wait(0.3f);
            Save(dir, "fnt1_03_tarjeta_parada", worst);
            FontGlyphSnapshotTests.Scan("fnt1_03_tarjeta_parada", problems);

            // Victoria del 3 con la línea de fruta.
            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(3, new MemoryProgressStore(3, 2));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.TickExact(boot, 1500);
            SnapshotKit.Attach(boot);
            Assert.That(boot.View!.FruitLine, Is.Not.Null, "la línea de fruta del 3");
            yield return SnapshotKit.Wait(0.3f);
            Save(dir, "fnt1_04_victoria_fruta", worst);
            FontGlyphSnapshotTests.Scan("fnt1_04_victoria_fruta", problems);

            Debug.Log("AF_FNT1 " + locale + " " + scale.ToString(CultureInfo.InvariantCulture) + ": " + problems.Count.ToString(CultureInfo.InvariantCulture)
                + " caracteres fuera del atlas.");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static void Save(string dir, string name, bool worst)
        {
            if (worst)
            {
                SnapshotKit.SaveTop(dir, name, (int)HudLabelTests.Screen640.y);
            }
            else
            {
                SnapshotKit.Save(dir, name);
            }
        }
    }
}
