#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La lista del Mercado que se desplaza (D-033, puntos 11 y 12), con la fuente al 130 % en la pantalla del batchmode (el lienzo
    /// mínimo de 640 dp de alto), donde las filas de Semillas no caben: TalkBack llega a todas, también a las de debajo del corte,
    /// y el foco desplaza el visor (Q-B9-17); un <see cref="MarketScreen.Refresh"/> sin cambio de pestaña deja la lista donde
    /// estaba, y abrir o cambiar de pestaña la lleva arriba; la sonda <c>AF_LAYOUT</c> no da los botones de las filas que el visor
    /// recorta del todo.
    /// </summary>
    public sealed class MarketScrollTests
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
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator SeedsAt130_EveryRowReachesTheReader_TheListKeepsItsPlace_AndTheProbeSkipsHiddenRows()
        {
            yield return Fx130Walk.Load(LocLocale.SourceLocale, 1.3f);
            GameBootstrap boot = GameBootstrap.Current!;

            // El Mercado se mide con la pantalla de verdad (en batchmode, 640 × 480 px; el lienzo, 640 dp de alto, el mínimo), para
            // que los marcos que mira el lector (píxeles de la pantalla) caigan dentro de ella.
            GameScreen.SimulatedScreen = null;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            yield return null;
            MarketScreen market = boot.Market!;
            Canvas.ForceUpdateCanvases();
            Assert.That(market.RowsScroll, Is.True, "en una pantalla baja y al 130 % las filas de Semillas no caben: se desplazan");
            ScrollRect list = market.Canvas.GetComponentInChildren<ScrollRect>();
            RectTransform content = list.content;
            Assert.That(list.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.01f), "al abrir, arriba del todo");

            // La sonda: los botones que el visor recorta del todo no salen (no se pueden tocar).
            AssertProbeMatchesTheViewport(market, "arriba");

            // El lector: todas las filas, también las de debajo del corte, con su lista; el foco en la última la enseña.
            var host = new UnityScreenReaderHost();
            try
            {
                var service = new A11yService(host);
                service.Refresh();
                IReadOnlyList<A11yItem> items = new List<A11yItem>(service.Published);
                int last = -1;
                for (int i = 0; i < market.Rows.Count; i++)
                {
                    if (!market.RowButtonShown(i))
                    {
                        continue;
                    }

                    int index = IndexOfId(items, "btn_" + market.RowButton(i).GetInstanceID());
                    Assert.That(index, Is.GreaterThanOrEqualTo(0), "la fila " + i + " llega al lector; publicado: " + Describe(items));
                    Assert.That(items[index].Scroll, Is.SameAs(list), "con su lista");
                    Assert.That(index, Is.GreaterThan(last), "en el orden de la lista");
                    last = index;
                }

                Assert.That(host.NodeReveal(last)(), Is.True, "el foco en la última fila desplaza el visor");
                Assert.That(A11yScroll.FullyShown(list, items[last].Rect!), Is.True, "y la enseña entera");
                int pages = 0;
                while (host.NodeScroller(last)(AccessibilityScrollDirection.Down) && pages < 20)
                {
                    pages++;
                }

                Assert.That(list.verticalNormalizedPosition, Is.EqualTo(0f).Within(0.01f), "la acción de desplazar llega abajo del todo y ahí se para");
                Assert.That(host.NodeScroller(last)(AccessibilityScrollDirection.Up), Is.True, "hacia arriba, sí");
            }
            finally
            {
                host.Clear();
            }

            // Abajo del todo: la sonda da ahora las de abajo; un Refresh (una compra, una nota, la tienda) no la sube.
            list.verticalNormalizedPosition = 0f;
            float bottom = content.anchoredPosition.y;
            Assert.That(bottom, Is.GreaterThan(1f));
            AssertProbeMatchesTheViewport(market, "abajo");
            market.Refresh();
            Assert.That(content.anchoredPosition.y, Is.EqualTo(bottom).Within(0.5f), "un Refresh sin cambio de pestaña conserva la posición");
            market.SelectTab(MarketTab.Seeds);
            Assert.That(content.anchoredPosition.y, Is.EqualTo(bottom).Within(0.5f), "tocar la pestaña que ya se ve, también");

            // Cambiar de pestaña y volver, o cerrar y abrir: la lista empieza arriba.
            MarketTab other = market.TabShown(MarketTab.Boosters) ? MarketTab.Boosters : MarketTab.Cuias;
            Assert.That(market.TabShown(other), Is.True);
            market.SelectTab(other);
            market.SelectTab(MarketTab.Seeds);
            Assert.That(content.anchoredPosition.y, Is.EqualTo(0f).Within(0.5f), "otra pestaña: arriba");

            // Con un visor de una fila y media (como en una pantalla aún más baja), las filas de debajo quedan enteras bajo el
            // corte: la sonda no las da, y el lector sí, con su lista.
            list.viewport.sizeDelta = new Vector2(list.viewport.sizeDelta.x, 90f);
            Canvas.ForceUpdateCanvases();
            Assert.That(AssertProbeMatchesTheViewport(market, "visor bajo"), Is.GreaterThan(0), "filas enteras bajo el corte");
            var fake = new FakeScreenReaderHost(true);
            new A11yService(fake).Refresh();
            for (int i = 0; i < market.Rows.Count; i++)
            {
                if (market.RowButtonShown(i))
                {
                    int index = IndexOfId(fake.Items, "btn_" + market.RowButton(i).GetInstanceID());
                    Assert.That(index, Is.GreaterThanOrEqualTo(0), "visor bajo: la fila " + i + " llega al lector");
                    Assert.That(fake.Items[index].Scroll, Is.SameAs(list));
                }
            }

            market.Refresh(); // vuelve a colocar el visor
            list.verticalNormalizedPosition = 0f;
            market.Close();
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            Assert.That(content.anchoredPosition.y, Is.EqualTo(0f).Within(0.5f), "al volver a abrir: arriba");
            market.Close();
        }

        // La sonda da el botón de cada fila que se ve y ninguno de los que el visor recorta del todo; devuelve cuántos recorta.
        private static int AssertProbeMatchesTheViewport(MarketScreen market, string where)
        {
            Canvas.ForceUpdateCanvases();
            var ids = new List<string>();
            var rects = new List<RectTransform>();
            market.VisibleButtons(ids, rects);
            Assert.That(ids, Does.Contain("market_close"));
            int hidden = 0;
            for (int i = 0; i < market.Rows.Count; i++)
            {
                if (!market.RowButtonShown(i))
                {
                    continue;
                }

                bool clipped = A11yScroll.ClippedOut((RectTransform)market.RowButton(i).transform);
                hidden += clipped ? 1 : 0;
                Assert.That(ids.Contains("market_row_" + market.Rows[i].Id), Is.EqualTo(!clipped), where + ": la fila " + i + (clipped ? ", bajo el corte, no sale" : ", a la vista, sale"));
            }

            return hidden;
        }

        private static string Describe(IReadOnlyList<A11yItem> items)
        {
            var sb = new System.Text.StringBuilder();
            foreach (A11yItem item in items)
            {
                sb.Append(item.Id).Append('=').Append(item.Label).Append(item.Scroll != null ? " [lista]" : string.Empty).Append(" | ");
            }

            return sb.ToString();
        }

        private static int IndexOfId(IReadOnlyList<A11yItem> items, string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
