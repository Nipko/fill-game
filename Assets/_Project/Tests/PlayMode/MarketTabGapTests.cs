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
    /// Observación 5 de la jornada 6 en el teléfono (2026-10-28): al 130 % en es-419, las pestañas «Semillas» y «Comodines» del
    /// Mercado casi se tocaban. Mide, con el texto ya dibujado, el hueco entre los glifos de cada dos pestañas vecinas con la
    /// escala de letra de <c>af_font_scale</c> (<see cref="LaunchOptions.FontScaleOverride"/>; al 200 % se queda en el tope de
    /// 02 §12) en es-419, en y pt-BR, a 360 × 640 dp y con cada pestaña elegida (la elegida va subrayada): nunca menos de
    /// <see cref="MarketScreen.TabGapDp"/>. El perfil y el arranque son los de <see cref="Fx130OverflowTests"/>.
    /// </summary>
    public sealed class MarketTabGapTests
    {
        [SetUp]
        public void SetUp()
        {
            Fx130Walk.SetUp();
        }

        [TearDown]
        public void TearDown()
        {
            Fx130Walk.TearDown();
        }

        [UnityTest]
        public IEnumerator Tabs_KeepAMinimumGap_AtAnyFontScale(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130, 200)] int percent)
        {
            yield return Fx130Walk.Load(locale, percent / 100f);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Loc.Locale, Is.EqualTo(locale));
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            MarketScreen market = boot.Market!;
            RectTransform panel = (RectTransform)market.Canvas.transform.Find("Panel");
            Assert.That(panel, Is.Not.Null);

            var problems = new List<string>();
            float narrowest = float.PositiveInfinity;
            foreach (MarketTab selected in new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
            {
                market.SelectTab(selected);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var spans = new List<(MarketTab tab, float min, float max)>();
                foreach (MarketTab tab in new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
                {
                    if (!market.TabShown(tab))
                    {
                        continue;
                    }

                    TextMeshProUGUI label = market.TabLabel(tab);
                    Assert.That(Ink(label, panel, out float min, out float max), Is.True, tab + ": la etiqueta tiene texto dibujado");
                    spans.Add((tab, min, max));
                }

                Assert.That(spans.Count, Is.GreaterThanOrEqualTo(2), "al menos Semillas y Comodines");
                spans.Sort((a, b) => a.min.CompareTo(b.min));
                for (int i = 1; i < spans.Count; i++)
                {
                    float gap = spans[i].min - spans[i - 1].max;
                    narrowest = Mathf.Min(narrowest, gap);
                    if (gap < MarketScreen.TabGapDp - 0.5f)
                    {
                        problems.Add("con " + selected + " elegida, entre " + spans[i - 1].tab + " y " + spans[i].tab + ": "
                            + gap.ToString("0.#", CultureInfo.InvariantCulture) + " dp");
                    }
                }
            }

            TestContext.WriteLine("AF_TAB_GAP " + locale + " al " + percent.ToString(CultureInfo.InvariantCulture) + " %: hueco mínimo "
                + narrowest.ToString("0.#", CultureInfo.InvariantCulture) + " dp");
            Assert.That(problems, Is.Empty, "pestañas demasiado juntas en " + locale + " al " + percent + " %:\n" + string.Join("\n", problems));
        }

        // Extremos horizontales de los glifos visibles de la etiqueta, en dp de la tarjeta.
        private static bool Ink(TextMeshProUGUI label, RectTransform panel, out float min, out float max)
        {
            label.ForceMeshUpdate();
            TMP_TextInfo info = label.textInfo;
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible)
                {
                    continue;
                }

                float a = panel.InverseTransformPoint(label.rectTransform.TransformPoint(c.bottomLeft)).x;
                float b = panel.InverseTransformPoint(label.rectTransform.TransformPoint(c.topRight)).x;
                min = Mathf.Min(min, Mathf.Min(a, b));
                max = Mathf.Max(max, Mathf.Max(a, b));
            }

            return max > min;
        }
    }
}
