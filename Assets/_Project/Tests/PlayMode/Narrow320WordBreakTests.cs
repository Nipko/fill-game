#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using TMPro;
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
    /// de 12 sp × escala (o, como último recurso si la palabra no cabe, de 12 dp en cualquier ancho: añadido de D-061), o pasa a dos líneas por los espacios. Deja
    /// en el log el tamaño final de «Reiniciar» y de la bandeja. El perfil es el de <see cref="Fx130OverflowTests"/>.
    /// </summary>
    public sealed class Narrow320WordBreakTests
    {
        [SetUp]
        public void SetUp()
        {
            Fx130Walk.SetUp();
            GameScreen.SimulatedPxPerDp = Canvas320Tests.Density;
            GameScreen.ShuffleOverride = true; // la Pororoca, como en el teléfono (observación 13 de la jornada 6): cinco ranuras en el 100
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedPxPerDp = null;
            GameScreen.SimulatedScreen = null;
            GameScreen.ShuffleOverride = null;
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
            // Hasta la observación 13 de la jornada 6 la Pororoca iba apagada (PlayModeHelpers.NoShuffleUnlessSet) y se miraban cuatro.
            yield return EnterLevel(boot, level);
            GameScreen view = boot.View!;
            Assert.That(view.CanvasWidth, Is.EqualTo(320f).Within(0.01f), "el lienzo de 320 dp");
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
                + broken.Count.ToString(CultureInfo.InvariantCulture) + " partidos; tamaños finales: " + Sizes(view) + "; otros avisos:\n" + string.Join("\n", inventory.Lines));
            Assert.That(inventory.Scanned, Is.GreaterThan(20), "textos mirados");
            Assert.That(broken, Is.Empty, "palabras partidas en " + tag + ":\n" + string.Join("\n", broken));
        }

        /// <summary>
        /// D-061 y su añadido del 2026-10-29 (el suelo de 12 dp en cualquier ancho): a 320 y a 360 dp, «Reiniciar» y las etiquetas
        /// de la bandeja van a 12 sp × escala, el mínimo de 02 §12, y solo encoge la que a ese tamaño no cabe (a 360 dp y al 130 %,
        /// con las cinco ranuras, «Deshacer» o «Desfazer»), nunca por debajo de 12 dp. Al 100 % no encoge ninguna (12 sp ya son
        /// 12 dp). En es-419, en y pt-BR, con las cinco ranuras (el 100).
        /// </summary>
        [UnityTest]
        public IEnumerator LastResortShrink_OnlyWhenTheWordDoesNotFit_AndNeverBelow12(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent,
            [Values(360, 320)] int width)
        {
            float scale = percent / 100f;
            if (width == 360)
            {
                GameScreen.SimulatedPxPerDp = null;
            }

            yield return Fx130Walk.Load(locale, scale, width == 320 ? Canvas320Tests.Screen320 : HudLabelTests.Screen780, 100);
            GameBootstrap boot = GameBootstrap.Current!;
            yield return EnterLevel(boot, 100);
            GameScreen view = boot.View!;
            Assert.That(view.CanvasWidth, Is.EqualTo((float)width).Within(0.01f));
            string at = locale + " al " + percent + " % a " + width + " dp: " + Sizes(view);
            TestContext.WriteLine("AF_SHRINK " + at);
            float normal = UiBuilder.MinTextSp * UiBuilder.TextScale;
            foreach (TextMeshProUGUI label in Labels(view))
            {
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), at + ": «" + label.text + "» nunca por debajo de 12 dp (D-061)");
                Assert.That(label.fontSize, Is.LessThanOrEqualTo(normal + 0.01f), at + ": «" + label.text + "» nunca crece");
                if (percent == 100)
                {
                    Assert.That(label.fontSize, Is.EqualTo(normal).Within(0.01f), at + ": «" + label.text + "» al 100 %, a 12 sp");
                }
                else if (label != view.RestartLabel && label.fontSize < normal - 0.01f)
                {
                    // Encoge solo si su palabra, a 12 sp × escala, no cabía en la caja.
                    float wordAtNormal = UiBuilder.LongestWordWidth(label) * (normal / label.fontSize);
                    Assert.That(wordAtNormal, Is.GreaterThan(label.rectTransform.rect.width - 0.5f), at + ": «" + label.text + "» encoge aunque cabía");
                }
            }
        }

        // Juega el nivel desde el inicio y deja el tablero en juego; con el 100 (Aguas turbias), la bandeja con sus cinco ranuras.
        private static IEnumerator EnterLevel(GameBootstrap boot, int level)
        {
            Assert.That(boot.PressPlay(), Is.True, "Jugar desde el inicio");
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            if (level == 100)
            {
                Assert.That(boot.View!.Tray.RevealShown, Is.True, "con Aguas turbias, la Luciérnaga: la bandeja con sus cinco ranuras");
            }

            yield return null;
        }

        // «Reiniciar» y las etiquetas de la bandeja que se ven.
        private static List<TextMeshProUGUI> Labels(GameScreen view)
        {
            var labels = new List<TextMeshProUGUI> { view.RestartLabel };
            BoosterTray tray = view.Tray;
            foreach (TextMeshProUGUI label in new[] { tray.UndoLabel, tray.ExtraLabel, tray.HintLabel, tray.ShuffleLabel, tray.RevealLabel })
            {
                if (label.gameObject.activeInHierarchy)
                {
                    labels.Add(label);
                }
            }

            return labels;
        }

        // Los tamaños finales (en dp de lienzo) de «Reiniciar» y de las etiquetas de la bandeja, para el log.
        private static string Sizes(GameScreen view)
        {
            var parts = new List<string>();
            foreach (TextMeshProUGUI label in Labels(view))
            {
                parts.Add("«" + label.text + "» " + label.fontSize.ToString("0.0#", CultureInfo.InvariantCulture));
            }

            return string.Join(", ", parts);
        }
    }
}
