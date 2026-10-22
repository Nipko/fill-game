#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Economy;
using AF.IAP;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los desbordes al 130 % del punto 11 de D-033 (añadido del 2026-10-22; los detectó la traducción T2b y pasaban también en
    /// español): las filas del Mercado, la pestaña «Sin anuncios» y la línea del código de borrado de Ajustes (26 caracteres de
    /// Crockford). A 360 × 640 dp (02 §5), en es-419, en y pt-BR, al 100 % y al 130 % (02 §12: hasta el 130 % sin desbordes ni
    /// recortes; ≥ 12 sp): falla si un texto de esas zonas se sale de su caja, no cabe ni al mínimo del autoajuste, baja de 12 sp
    /// (× escala) o se parte a media palabra (<see cref="LocInventory.ProblemsOf"/>), si una fila del Mercado pisa a la siguiente o
    /// la tarjeta se sale de la pantalla, o si el código no se lee entero (sus 26 caracteres en orden, solo con guiones de
    /// Crockford entre los grupos). No depende de ninguna cadena concreta: mide lo que haya en las tablas (otro carril cambia
    /// nombres de en y pt-BR). No necesita gráficos; las capturas, en <see cref="Fx130SnapshotTests"/>.
    /// </summary>
    public sealed class Fx130OverflowTests
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
        public IEnumerator MarketRows_NoAdsTab_AccountCode_NoOverflow(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent)
        {
            var problems = new List<string>();
            var counts = new int[1];
            yield return Fx130Walk.Run(locale, percent / 100f, null, string.Empty, problems, counts);
            TestContext.WriteLine("AF_FX130 " + locale + " al " + percent.ToString(CultureInfo.InvariantCulture) + " %: "
                + counts[0].ToString(CultureInfo.InvariantCulture) + " textos mirados, " + problems.Count.ToString(CultureInfo.InvariantCulture) + " problemas");
            Assert.That(counts[0], Is.GreaterThan(30), "textos mirados en el Mercado y en el código de Ajustes");
            Assert.That(problems, Is.Empty, "desbordes en " + locale + " al " + percent + " %:\n" + string.Join("\n", problems));
        }

        [Test]
        public void CodeGroups_KeepEveryCharacterInOrder()
        {
            const string code = "2PE092D8MZVHN1ZM3X18WS47FH";
            string shown = AccountText.CodeGroups(code);
            Assert.That(shown.Replace("-", string.Empty), Is.EqualTo(code), "los 26 caracteres, en orden");
            foreach (string group in shown.Split('-'))
            {
                Assert.That(group.Length, Is.InRange(3, AccountText.CodeGroupSize), "grupos de " + AccountText.CodeGroupSize);
            }

            Assert.That(AccountText.CodeGroups(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(AccountText.CodeGroups("ABCD"), Is.EqualTo("ABCD"));
        }
    }

    /// <summary>
    /// Capturas de <see cref="Fx130OverflowTests"/> (Ajustes con el código y las cuatro pestañas del Mercado, a 360 × 640 dp, en
    /// es-419, en y pt-BR, al 100 % y al 130 %), en <c>Logs/snapshots/&lt;idioma&gt;/fx130_&lt;escala&gt;_*.png</c>, con los
    /// problemas que queden en <c>fx130_&lt;escala&gt;_problemas.txt</c>. Explícito: necesita gráficos (<c>unity test &lt;repo&gt;
    /// --mode PlayMode --filter AF.Tests.PlayMode.Fx130SnapshotTests</c> sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de los desbordes al 130 % (D-033, punto 11); necesitan dispositivo gráfico.")]
    public sealed class Fx130SnapshotTests
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
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(locale);
            string prefix = "fx130_" + percent.ToString(CultureInfo.InvariantCulture) + "_";
            var problems = new List<string>();
            yield return Fx130Walk.Run(locale, percent / 100f, dir, prefix, problems, new int[1]);
            var sb = new StringBuilder();
            sb.Append("# FX-130 ").Append(locale).Append(" al ").Append(percent.ToString(CultureInfo.InvariantCulture)).Append(" %: ")
                .Append(problems.Count.ToString(CultureInfo.InvariantCulture)).Append(" problemas\n");
            foreach (string line in problems)
            {
                sb.Append(line).Append('\n');
            }

            string report = System.IO.Path.Combine(dir, prefix + "problemas.txt");
            System.IO.File.WriteAllText(report, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("AF_FX130 " + locale + " al " + percent + " %: " + problems.Count.ToString(CultureInfo.InvariantCulture) + " problemas; " + report);
        }
    }

    /// <summary>El recorrido de <see cref="Fx130OverflowTests"/> y <see cref="Fx130SnapshotTests"/>.</summary>
    internal static class Fx130Walk
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc);

        public static void SetUp()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
        }

        public static void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetConsentOverrides();
            PseudoLocWalk.Release();
        }

        /// <summary>
        /// Arranca el juego en <paramref name="locale"/> con la fuente a <paramref name="scale"/>, a 360 × 640 dp (o en
        /// <paramref name="screen"/>), con el perfil de las capturas: nivel 46, 2.450 semillas, la Cuia de Santarém puesta y el
        /// Cántaro a 1.230.
        /// </summary>
        public static IEnumerator Load(string locale, float scale, Vector4? screen = null)
        {
            FunnelSetup.Hooks();
            LaunchOptions.LocTablesOverride = locale == LocLocale.SourceLocale ? null : TranslatedTables.Completed(locale, out _, out _);
            LaunchOptions.LanguageOverride = locale;
            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            GameScreen.SimulatedScreen = screen ?? HudLabelTests.Screen640;

            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = 46;
            profile.Progress.MaxLevel = 45;
            profile.Wallet.Seeds = 2450;
            profile.Consent = new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Adult);
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.SkinEquipped = "af_skin_m02";
            var jar = new InMemoryOffersStore();
            jar.Save(new OffersRecord { PiggySeeds = 1230, PiggyCap = 3000 });
            GameBootstrap.OffersStoreOverride = jar;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
        }

        /// <summary>
        /// Ajustes con el código de borrado y las cuatro pestañas del Mercado (la Cuia de Santarém puesta, el Cántaro a 1.230) en
        /// <paramref name="locale"/> con la fuente a <paramref name="scale"/>; apunta en <paramref name="problems"/> lo que falle y
        /// en <paramref name="counts"/>[0] los textos mirados. Con <paramref name="dir"/>, guarda cada captura.
        /// </summary>
        public static IEnumerator Run(string locale, float scale, string? dir, string prefix, List<string> problems, int[] counts)
        {
            yield return Load(locale, scale);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Loc.Locale, Is.EqualTo(locale), "la UI en el idioma medido");
            var inventory = new LocInventory(locale, scale);
            string tag = locale + " " + Mathf.RoundToInt(scale * 100f).ToString(CultureInfo.InvariantCulture) + " % ";

            // Ajustes: la línea del código de borrado, al final de la lista (se baja hasta ella para la captura).
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            yield return SnapshotKit.Wait(0.3f);
            Assert.That(settings.AccountCodeText, Is.Not.Null, "la línea del código de borrado");
            TMP_Text? code = Find(settings.Canvas.transform, "AccountCode");
            Assert.That(code, Is.Not.Null);
            counts[0]++;
            string codeProblems = inventory.ProblemsOf(code!);
            if (codeProblems.Length > 0)
            {
                problems.Add(tag + "ajustes\tAccountCode\t" + codeProblems + "\t" + Size(code!) + "\t«" + code!.text + "»");
            }

            if (!HasWholeCode(code!.text))
            {
                problems.Add(tag + "ajustes\tAccountCode\tel código no sale entero (26 caracteres de Crockford en orden)\t«" + code.text + "»");
            }

            if (dir != null)
            {
                ScrollRect? scroll = settings.Canvas.GetComponentInChildren<ScrollRect>();
                if (scroll != null)
                {
                    scroll.verticalNormalizedPosition = 0f;
                }

                SnapshotKit.AttachLobby(boot);
                SnapshotKit.AttachOverlay(settings.Canvas);
                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, prefix + "ajustes_codigo");
            }

            Assert.That(boot.CloseSettings(), Is.True);

            // El Mercado, pestaña por pestaña.
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            foreach (MarketTab tab in new[] { MarketTab.Seeds, MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
            {
                boot.Market!.SelectTab(tab);
                Assert.That(boot.Market.Tab, Is.EqualTo(tab), "la pestaña " + tab + " se ve");
                if (dir != null)
                {
                    SnapshotKit.AttachLobby(boot);
                    SnapshotKit.AttachOverlay(boot.Market.Canvas);
                }

                yield return SnapshotKit.Wait(0.3f);
                string screen = "mercado_" + tab.ToString().ToLowerInvariant();
                if (dir != null)
                {
                    SnapshotKit.Save(dir, prefix + screen);
                }

                CheckMarket(boot.Market, inventory, tag + screen, problems, counts);
                if (dir != null && boot.Market.RowsScroll)
                {
                    // Las filas se desplazan: también la captura con la lista abajo del todo.
                    ScrollRect scroll = boot.Market.Canvas.GetComponentInChildren<ScrollRect>();
                    scroll.verticalNormalizedPosition = 0f;
                    yield return SnapshotKit.Wait(0.3f);
                    SnapshotKit.Save(dir, prefix + screen + "_abajo");
                }
            }
        }

        private static void CheckMarket(MarketScreen market, LocInventory inventory, string screen, List<string> problems, int[] counts)
        {
            Canvas canvas = market.Canvas;
            Canvas.ForceUpdateCanvases();
            RectTransform panel = (RectTransform)canvas.transform.Find("Panel");
            Assert.That(panel, Is.Not.Null);
            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.text) || text.color.a < 0.01f)
                {
                    continue;
                }

                counts[0]++;
                string what = inventory.ProblemsOf(text);
                if (what.Length > 0)
                {
                    problems.Add(screen + "\t" + text.transform.parent.name + "/" + text.name + "\t" + what + "\t" + Size(text) + "\t«" + text.text + "»");
                }
            }

            // La tarjeta dentro de la zona segura (dp desde arriba a la izquierda del lienzo) y cada fila debajo de la anterior, sin
            // pisarla (las filas que no caben se desplazan en su visor).
            float cardTop = -panel.anchoredPosition.y;
            float cardBottom = cardTop + panel.sizeDelta.y;
            float cardLeft = panel.anchoredPosition.x;
            if (cardTop < market.SafeTopDp - 0.5f || cardBottom > market.AreaHeight - market.SafeBottomDp + 0.5f
                || cardLeft < -0.5f || cardLeft + panel.sizeDelta.x > market.AreaWidth + 0.5f)
            {
                problems.Add(screen + "\tPanel\tla tarjeta se sale de la pantalla\t" + cardTop.ToString("0.#", CultureInfo.InvariantCulture) + "-"
                    + cardBottom.ToString("0.#", CultureInfo.InvariantCulture) + " dp de " + market.AreaHeight.ToString("0.#", CultureInfo.InvariantCulture));
            }

            float lastBottom = float.PositiveInfinity;
            for (int i = 0; i < MarketScreen.MaxRows; i++)
            {
                Transform? row = panel.Find("Rows/Content/Row" + i.ToString(CultureInfo.InvariantCulture));
                if (row == null || !row.gameObject.activeSelf)
                {
                    continue;
                }

                Rect r = Local((RectTransform)row, (RectTransform)canvas.transform);
                if (r.yMax > lastBottom + 0.5f)
                {
                    problems.Add(screen + "\t" + row.name + "\tpisa la fila de arriba (" + (r.yMax - lastBottom).ToString("0.#", CultureInfo.InvariantCulture) + " dp)");
                }

                lastBottom = r.yMin;
                foreach (TMP_Text text in row.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (string.IsNullOrWhiteSpace(text.text) || text.name == "Label")
                    {
                        continue; // la etiqueta del botón va dentro de su botón
                    }

                    Rect t = Local(text.rectTransform, (RectTransform)canvas.transform);
                    if (t.yMin < r.yMin - 0.5f || t.yMax > r.yMax + 0.5f)
                    {
                        problems.Add(screen + "\t" + row.name + "/" + text.name + "\tla caja se sale de su fila\t«" + text.text + "»");
                    }
                }
            }
        }

        private static Rect Local(RectTransform rt, RectTransform root)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 a = root.InverseTransformPoint(corners[0]);
            Vector3 b = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        private static TMP_Text? Find(Transform root, string name)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == name)
                {
                    return text;
                }
            }

            return null;
        }

        private static string Size(TMP_Text text)
        {
            Rect rect = text.rectTransform.rect;
            return text.fontSize.ToString("0.#", CultureInfo.InvariantCulture) + " sp\t" + rect.width.ToString("0.#", CultureInfo.InvariantCulture)
                + " × " + rect.height.ToString("0.#", CultureInfo.InvariantCulture) + " dp";
        }

        // Los 26 caracteres de Crockford seguidos, con guiones como mucho entre ellos (el corte de línea va tras un guion).
        private static bool HasWholeCode(string text)
        {
            const string crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
            int run = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (crockford.IndexOf(c) >= 0)
                {
                    run++;
                    if (run >= 26)
                    {
                        return true;
                    }
                }
                else if (c != '-' || run == 0)
                {
                    run = 0;
                }
            }

            return false;
        }
    }
}
