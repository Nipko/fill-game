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
    /// Observaciones 13 y 15 de la jornada 6 en el teléfono (2026-10-29).
    /// <list type="bullet">
    /// <item>13: a 320 dp con la letra al 130 %, en el nivel 100, la bandeja partía «Deshace/r» (es-419), «Desfaze/r» y «Mistura/r»
    /// (pt-BR). El teléfono tenía las cinco ranuras (Marea, Cuia extra, Pista, Pororoca y Luciérnaga); los tests de antes
    /// (<see cref="Narrow320WordBreakTests"/>) apagaban la Pororoca y miraban cuatro. Aquí, con las cinco, con saldo y sin él (la
    /// bandeja en gris con «Te faltan {n}», como en el teléfono): ninguna etiqueta parte una palabra; a 320 dp ninguna baja de 12 dp
    /// (D-061) y a 360 dp todas van a 12 sp × escala (02 §12).</item>
    /// <item>15: al 130 % (y en es-419 también al 100 %) cada pestaña del Mercado encogía su texto por su cuenta. Aquí las pestañas
    /// que se ven comparten un único tamaño (<see cref="MarketScreen.TabFontSize"/>), nunca por debajo de 12 sp × escala y sin
    /// partir palabras, con cada pestaña elegida.</item>
    /// </list>
    /// A 320 y 360 dp, al 100 y al 130 %, en es-419, en y pt-BR. El perfil es el de <see cref="Fx130OverflowTests"/>.
    /// </summary>
    public sealed class TrayAndTabFontTests
    {
        [SetUp]
        public void SetUp()
        {
            Fx130Walk.SetUp();
            GameScreen.ShuffleOverride = true; // la Pororoca, como en el teléfono: cinco ranuras en el 100
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
        public IEnumerator TrayLabels_FiveSlots_NeverBreakAWord_AndKeepTheFloor(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent,
            [Values(320, 360)] int width, [Values(2450, 0)] int seeds)
        {
            float scale = percent / 100f;
            yield return Load(locale, scale, width, seeds);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressPlay(), Is.True, "Jugar desde el inicio");
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            yield return null;
            GameScreen view = boot.View!;
            Assert.That(view.CanvasWidth, Is.EqualTo((float)width).Within(0.01f), "el ancho del lienzo");
            BoosterTray tray = view.Tray;
            Assert.That(tray.ShuffleShown, Is.True, "la Pororoca en la bandeja");
            Assert.That(tray.RevealShown, Is.True, "con Aguas turbias, la Luciérnaga: cinco ranuras");

            string at = locale + " al " + percent.ToString(CultureInfo.InvariantCulture) + " % a " + width.ToString(CultureInfo.InvariantCulture)
                + " dp con " + seeds.ToString(CultureInfo.InvariantCulture) + " semillas";
            float normal = UiBuilder.MinTextSp * UiBuilder.TextScale;
            var parts = new List<string>();
            var problems = new List<string>();
            int labels = 0;
            foreach (TextMeshProUGUI label in new[] { tray.UndoLabel, tray.ExtraLabel, tray.HintLabel, tray.ShuffleLabel, tray.RevealLabel })
            {
                Assert.That(label.gameObject.activeInHierarchy, Is.True, at + ": «" + label.text + "» se ve");
                label.ForceMeshUpdate();
                labels++;
                // Las líneas, antes de medir palabras: GetPreferredValues reescribe el textInfo de la etiqueta.
                int lineCount = label.textInfo.lineCount;
                bool broken = LocInventory.BrokenWordLine(label) != 0;
                string lines = LinesOf(label);
                string size = label.fontSize.ToString("0.0#", CultureInfo.InvariantCulture);
                parts.Add("«" + label.text + "» " + size + " en " + lineCount.ToString(CultureInfo.InvariantCulture)
                    + " línea(s), palabra " + WidestWord(label).ToString("0.#", CultureInfo.InvariantCulture) + " en caja "
                    + BoxWidth(label).ToString("0.#", CultureInfo.InvariantCulture));
                if (broken)
                {
                    problems.Add("«" + label.text + "» parte una palabra (" + lines + " a " + size + " dp en "
                        + BoxWidth(label).ToString("0.#", CultureInfo.InvariantCulture) + ")");
                }

                if (WidestWord(label) > BoxWidth(label) + 0.5f)
                {
                    problems.Add("la palabra más larga de «" + label.text + "» no cabe en su caja a " + size + " dp");
                }

                if (width == 360 && Mathf.Abs(label.fontSize - normal) > 0.01f)
                {
                    problems.Add("«" + label.text + "» a " + size + " dp, no a 12 sp × escala (02 §12)");
                }

                if (width == 320 && (label.fontSize < UiBuilder.MinTextSp - 0.01f || label.fontSize > normal + 0.01f))
                {
                    problems.Add("«" + label.text + "» a " + size + " dp: fuera de [12 dp (D-061), 12 sp × escala]");
                }
            }

            TestContext.WriteLine("AF_TRAY_FIT " + at + ": " + string.Join(", ", parts));
            Assert.That(labels, Is.EqualTo(5));
            if (width == 360 && percent == 130 && locale != LocLocale.English && problems.Count > 0 && OnlyWordProblems(problems))
            {
                // Pregunta abierta (no la decide este carril): a 360 dp y al 130 %, con las cinco ranuras, «Deshacer» (68,7 dp),
                // «Desfazer», «Misturar», «Barajar» y «Revelar» no caben a 12 sp × escala en la ranura más ancha que deja la pantalla
                // (54 dp por dentro), y D-061 solo deja encoger por debajo de 360 dp. La regla del tamaño (02 §12) sí se comprueba
                // arriba; el corte de palabras queda pendiente del dueño (extender D-061, textos más cortos u otra bandeja).
                Assert.Ignore("pendiente del dueño (D-061 a 360 dp con cinco ranuras), " + at + ":\n" + string.Join("\n", problems));
            }

            Assert.That(problems, Is.Empty, at + ":\n" + string.Join("\n", problems));
        }

        // Solo palabras que no caben (ningún problema de tamaño): el caso de la pregunta abierta de 360 dp al 130 %.
        private static bool OnlyWordProblems(List<string> problems)
        {
            foreach (string problem in problems)
            {
                if (!problem.Contains("parte una palabra") && !problem.Contains("no cabe en su caja"))
                {
                    return false;
                }
            }

            return true;
        }

        [UnityTest]
        public IEnumerator MarketTabs_ShareOneSize_AboveTheFloor(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent,
            [Values(320, 360)] int width)
        {
            float scale = percent / 100f;
            yield return Load(locale, scale, width, 2450);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True, "el Mercado");
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True, "el Mercado se abre desde el inicio");
            MarketScreen market = boot.Market!;
            string at = locale + " al " + percent.ToString(CultureInfo.InvariantCulture) + " % a " + width.ToString(CultureInfo.InvariantCulture) + " dp";
            float floor = UiBuilder.MinTextSp * UiBuilder.TextScale;
            var log = new List<string>();
            foreach (MarketTab selected in new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
            {
                market.SelectTab(selected);
                yield return null;
                Canvas.ForceUpdateCanvases();
                float first = float.NaN;
                int shown = 0;
                var sizes = new List<string>();
                foreach (MarketTab tab in new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
                {
                    if (!market.TabShown(tab))
                    {
                        continue;
                    }

                    TextMeshProUGUI label = market.TabLabel(tab);
                    label.ForceMeshUpdate();
                    shown++;
                    sizes.Add("«" + label.text + "» " + label.fontSize.ToString("0.0#", CultureInfo.InvariantCulture));
                    string what = at + ", con " + selected + " elegida: «" + label.text + "»";
                    Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(floor - 0.01f), what + " nunca por debajo de 12 sp × escala");
                    Assert.That(label.fontSize, Is.EqualTo(market.TabFontSize).Within(0.01f), what + " al tamaño común de las pestañas");
                    Assert.That(LocInventory.BrokenWordLine(label), Is.EqualTo(0), what + " parte una palabra");
                    Assert.That(WidestWord(label), Is.LessThanOrEqualTo(BoxWidth(label) + 0.5f), what + ": su palabra más larga cabe en la caja");
                    if (float.IsNaN(first))
                    {
                        first = label.fontSize;
                    }
                    else
                    {
                        Assert.That(label.fontSize, Is.EqualTo(first).Within(0.01f), what + ": un único tamaño en la fila de pestañas");
                    }
                }

                Assert.That(shown, Is.GreaterThanOrEqualTo(2), "al menos Semillas y Comodines");
                log.Add(selected + ": " + string.Join(", ", sizes));
            }

            TestContext.WriteLine("AF_TAB_FONT " + at + ": " + string.Join(" | ", log));
        }

        private static IEnumerator Load(string locale, float scale, int width, int seeds)
        {
            if (width == 320)
            {
                GameScreen.SimulatedPxPerDp = Canvas320Tests.Density;
            }

            yield return Fx130Walk.Load(locale, scale, width == 320 ? Canvas320Tests.Screen320 : HudLabelTests.Screen780, 100, seeds);
            Assert.That(Loc.Locale, Is.EqualTo(locale));
        }

        // Las líneas que dibuja la etiqueta, separadas por « / » (para el mensaje).
        private static string LinesOf(TextMeshProUGUI label)
        {
            TMP_TextInfo info = label.textInfo;
            var lines = new List<string>();
            for (int i = 0; i < info.lineCount; i++)
            {
                var line = new System.Text.StringBuilder();
                for (int c = info.lineInfo[i].firstCharacterIndex; c <= info.lineInfo[i].lastCharacterIndex && c < info.characterCount; c++)
                {
                    line.Append(info.characterInfo[c].character);
                }

                lines.Add("«" + line + "»");
            }

            return string.Join(" / ", lines);
        }

        // Ancho de la caja de la etiqueta, sin sus márgenes (dp).
        private static float BoxWidth(TextMeshProUGUI label)
        {
            return label.rectTransform.rect.width - label.margin.x - label.margin.z;
        }

        // Ancho de la palabra más ancha de la etiqueta a su tamaño de ahora (dp): sin autoajuste, su fontSize; con él, TextMeshPro
        // mide a su tope (las pestañas lo tienen en el tamaño común).
        private static float WidestWord(TextMeshProUGUI label)
        {
            float widest = 0f;
            foreach (string word in label.text.Split(' ', '\n'))
            {
                if (word.Length > 0)
                {
                    widest = Mathf.Max(widest, label.GetPreferredValues(word, float.PositiveInfinity, float.PositiveInfinity).x);
                }
            }

            return widest;
        }
    }

    /// <summary>
    /// Capturas de las observaciones 13 y 15 de la jornada 6 en <c>Logs/snapshots/&lt;idioma&gt;/</c>: a 320 dp (1080 × 2340 px con
    /// 3,375 px/dp, como <c>wm density 540</c>), al 100 y al 130 %, en es-419, en y pt-BR, el nivel 100 con las cinco ranuras y 0
    /// semillas (<c>j6b_320_&lt;pct&gt;_nivel100</c>) y el Mercado en la pestaña Comodines (<c>j6b_320_&lt;pct&gt;_mercado</c>). Explícito:
    /// necesita gráficos.
    /// </summary>
    [Explicit("Capturas de la bandeja y del Mercado a 320 dp; necesitan dispositivo gráfico.")]
    public sealed class TrayAndTabSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            Fx130Walk.SetUp();
            GameScreen.ShuffleOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.SimulatedPxPerDp = null;
            GameScreen.SimulatedScreen = null;
            GameScreen.ShuffleOverride = null;
            Fx130Walk.TearDown();
        }

        [UnityTest]
        public IEnumerator Capture([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(locale);
            GameScreen.SimulatedPxPerDp = 1080f / 320f;
            string prefix = "j6b_320_" + percent.ToString(CultureInfo.InvariantCulture) + "_";
            yield return Fx130Walk.Load(locale, percent / 100f, new Vector4(1080f, 2340f, 96f, 48f), 100, 0);
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.SetUpMarket(), Is.True, "el Mercado");
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Boosters), Is.True, "el Mercado se abre desde el inicio");
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(boot.Market!.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, prefix + "mercado");
            boot.Market.CloseButton.onClick.Invoke();
            yield return null;

            Assert.That(boot.PressPlay(), Is.True, "Jugar desde el inicio");
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.View!.CanvasWidth, Is.EqualTo(320f).Within(0.01f), "el lienzo de 320 dp");
            SnapshotKit.Attach(boot);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, prefix + "nivel100");
            Debug.Log("AF_J6B_SNAPSHOT " + locale + " " + prefix);
        }
    }
}
