#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Observación 8 de la jornada 6 en el teléfono (2026-10-28): a 320 dp (<c>wm density 540</c>) y con la letra del sistema al
    /// 130 %, en es-419, se partían palabras por la mitad: «Reinicia/r» bajo el botón del HUD, «Deshace/r» en el comodín y
    /// «Comodine/s» en la pestaña del Mercado. Con el lienzo de 320 dp de <see cref="Canvas320Tests"/> (1 dp físico por dp) y la
    /// escala de <c>af_font_scale</c> (<see cref="LaunchOptions.FontScaleOverride"/>), en es-419, en y pt-BR: el HUD del nivel
    /// (la píldora de Reiniciar, la bandeja con sus etiquetas, el saldo) y las cuatro pestañas del Mercado; falla si algún
    /// texto visible se parte a media palabra (<see cref="LocInventory.BrokenWordLine"/>): o encoge con su autoajuste, sin bajar
    /// de 12 sp, o pasa a dos líneas por los espacios. El perfil es el de <see cref="Fx130OverflowTests"/> (nivel 46).
    /// </summary>
    public sealed class Narrow320WordBreakTests
    {
        [SetUp]
        public void SetUp()
        {
            Fx130Walk.SetUp();
            GameScreen.SimulatedPxPerDp = Canvas320Tests.Density;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedPxPerDp = null;
            GameScreen.SimulatedScreen = null;
            Fx130Walk.TearDown();
        }

        [UnityTest]
        public IEnumerator HudAndMarket_At320And130_NoWordIsBroken(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent, [Values(46, 100)] int level)
        {
            float scale = percent / 100f;
            yield return Fx130Walk.Load(locale, scale, Canvas320Tests.Screen320, level);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Loc.Locale, Is.EqualTo(locale));
            var inventory = new LocInventory(locale, scale);
            string tag = locale + " " + percent.ToString(CultureInfo.InvariantCulture) + " % 320 dp nivel " + level.ToString(CultureInfo.InvariantCulture) + " ";

            // El Mercado (se abre desde el inicio), pestaña por pestaña.
            Assert.That(boot.SetUpMarket(), Is.True, "el Mercado");
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True, "el Mercado se abre desde el inicio");
            foreach (MarketTab tab in new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
            {
                boot.Market!.SelectTab(tab);
                yield return null;
                inventory.Scan(tag + "mercado_" + tab.ToString().ToLowerInvariant());
            }

            boot.Market!.CloseButton.onClick.Invoke();
            yield return null;

            // El HUD del nivel (46: Marea, Cuia extra, Pista y Pororoca; 100, con Aguas turbias, también la Luciérnaga: cinco ranuras), en juego.
            Assert.That(boot.PressPlay(), Is.True, "Jugar desde el inicio");
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            GameScreen view = boot.View!;
            Assert.That(view.CanvasWidth, Is.EqualTo(320f).Within(0.01f), "el lienzo de 320 dp");
            if (level == 100)
            {
                Assert.That(view.Tray.RevealShown, Is.True, "con Aguas turbias, la Luciérnaga: la bandeja con sus cinco ranuras");
            }

            yield return null;
            inventory.Scan(tag + "hud");

            var broken = new List<string>();
            foreach (string line in inventory.Lines)
            {
                if (line.Contains("se parte a media palabra"))
                {
                    broken.Add(line);
                }
            }

            TestContext.WriteLine("AF_WORDBREAK " + tag + ": " + inventory.Scanned.ToString(CultureInfo.InvariantCulture) + " textos mirados, "
                + broken.Count.ToString(CultureInfo.InvariantCulture) + " partidos; otros avisos:\n" + string.Join("\n", inventory.Lines));
            Assert.That(inventory.Scanned, Is.GreaterThan(20), "textos mirados");
            Assert.That(broken, Is.Empty, "palabras partidas en " + tag + ":\n" + string.Join("\n", broken));
        }
    }
}
