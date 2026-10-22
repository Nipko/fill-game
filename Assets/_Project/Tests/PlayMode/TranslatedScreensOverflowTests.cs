#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Economy;
using AF.IAP;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Las traducciones de en y pt-BR de la segunda tanda (T2b, 2026-10-22: D-022, pregunta 51 (b)) contra sus cajas, en las
    /// pantallas que no recorre <see cref="TranslatedLocaleOverflowTests"/> y que estrenan esas cadenas: Ajustes (con Privacidad,
    /// Avisos, Alto contraste, Reducir movimiento y el código de borrado), los dos pasos de «Eliminar cuenta y datos», las cuatro
    /// pestañas del Mercado, la edad neutral, las finalidades y la doble confirmación de 13-17. A 360 × 640 dp, con la fuente al
    /// 100 % y al 130 % (02 §12), y con el criterio de <see cref="LocInventory"/>: falla si un texto se sale de su caja, no cabe ni
    /// al mínimo del autoajuste, baja de 12 sp o se parte a media palabra, salvo que la misma pieza falle igual en es-419 en la
    /// misma pantalla (la línea base: eso es un límite del diseño de la pantalla, no de la traducción, y sale como aviso).
    /// <para>
    /// Usa el andamio de <see cref="TranslatedTables"/>: las celdas que siguen vacías a propósito (las de
    /// <c>docs/dev/traspaso/spark-traducciones/pendientes_t2b.csv</c>) se rellenan solo aquí con su es-419, para que el idioma se
    /// pueda elegir; lo que se mide de verdad son las traducciones que ya están.
    /// </para>
    /// <para>
    /// Y Ajustes › Idioma: con en y pt-BR completos, ofrece English y Português (D-018, Q7-A; observación 1 de la prueba en el
    /// teléfono del 2026-10-22).
    /// </para>
    /// </summary>
    public sealed class TranslatedScreensOverflowTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 22, 15, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            GameBootstrap.ConsentEnabledOverride = true;
            GameBootstrap.SimCountryOverride = new ManualSimCountry("CO");
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetConsentOverrides();
            PseudoLocWalk.Release();
        }

        [UnityTest]
        public IEnumerator Translated_360x640_SettingsMarketConsent_NoTextOverflows([Values(LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent)
        {
            float scale = percent / 100f;
            var inventory = new LocInventory(locale, scale);
            LocTableTexts tables = TranslatedTables.Completed(locale, out int translated, out int filled);
            yield return Walk(locale, scale, tables, inventory);

            // La línea base: el mismo recorrido en es-419. Lo que ya falla en español (la misma pieza en la misma pantalla) es un
            // límite del diseño de esa pantalla, no de la traducción: se apunta como aviso y no hace fallar este test.
            var baseline = new LocInventory(LocLocale.SourceLocale, scale);
            yield return Walk(LocLocale.SourceLocale, scale, null, baseline);
            var inSpanish = new HashSet<string>();
            foreach (string line in baseline.Lines)
            {
                inSpanish.Add(Where(line));
            }

            var own = new List<string>();
            foreach (string line in inventory.Lines)
            {
                if (inSpanish.Contains(Where(line)))
                {
                    TestContext.WriteLine("AF_T2B aviso (también en es-419): " + line);
                }
                else
                {
                    own.Add(line);
                }
            }

            TestContext.WriteLine("AF_T2B " + locale + " al " + percent + " %: " + inventory.Scanned.ToString(CultureInfo.InvariantCulture) + " textos mirados, "
                + inventory.Problems.ToString(CultureInfo.InvariantCulture) + " con problemas (" + own.Count.ToString(CultureInfo.InvariantCulture)
                + " solo de la traducción); es-419: " + baseline.Problems.ToString(CultureInfo.InvariantCulture) + "; strings.csv con "
                + translated.ToString(CultureInfo.InvariantCulture) + " traducciones y " + filled.ToString(CultureInfo.InvariantCulture) + " celdas de andamio en es-419");
            Assert.That(inventory.Scanned, Is.GreaterThan(60), "textos visibles mirados");
            Assert.That(own, Is.Empty, "textos que se salen o bajan de 12 sp en " + locale + " al " + percent + " % y no en es-419:\n" + string.Join("\n", own));
        }

        // «pantalla\truta» de una línea de LocInventory: la misma pieza en la misma pantalla.
        private static string Where(string line)
        {
            int first = line.IndexOf('\t');
            int second = first < 0 ? -1 : line.IndexOf('\t', first + 1);
            return second < 0 ? line : line.Substring(0, second);
        }

        private static IEnumerator Walk(string locale, float scale, LocTableTexts? tables, LocInventory inventory)
        {
            // Ajustes y «Eliminar cuenta y datos», con el consentimiento ya dado (Privacidad y «Corregir mi edad» a la vista).
            Prime(tables, locale, scale);
            Profile profile = ProfileAt(46, 2450, new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Adult));
            profile.Entitlements.Skins.Add("af_skin_m02");
            profile.Entitlements.SkinEquipped = "af_skin_m02";
            var jar = new InMemoryOffersStore();
            jar.Save(new OffersRecord { PiggySeeds = 1230, PiggyCap = 3000 });
            GameBootstrap.OffersStoreOverride = jar;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Loc.Locale, Is.EqualTo(locale), "la UI en el idioma medido");
            yield return Take(inventory, "inicio");

            Assert.That(boot.PressSettings(), Is.True);
            yield return Take(inventory, "ajustes");
            Assert.That(boot.OpenDeleteAccount(), Is.True);
            yield return Take(inventory, "eliminar_cuenta_1");
            Assert.That(boot.ContinueDeleteAccount(), Is.True);
            yield return Take(inventory, "eliminar_cuenta_2");
            Assert.That(boot.CancelDeleteAccount(), Is.True);
            Assert.That(boot.CloseSettings(), Is.True);

            // El Mercado, pestaña por pestaña (la Cuia de Santarém puesta, el Cántaro a 1.230).
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.TickMarket(16f);
            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            yield return Take(inventory, "mercado_semillas");
            foreach (MarketTab tab in new[] { MarketTab.Boosters, MarketTab.Cuias, MarketTab.NoAds })
            {
                boot.Market!.SelectTab(tab);
                yield return Take(inventory, "mercado_" + tab.ToString().ToLowerInvariant());
            }

            // La edad neutral y las finalidades, la primera vez.
            Prime(tables, locale, scale);
            yield return PlayModeHelpers.LoadAppWithConsent(new MemoryProfileStore());
            boot = GameBootstrap.Current!;
            Assert.That(boot.ConsentView != null && boot.ConsentView.Visible, Is.True, "la pantalla de la edad");
            yield return Take(inventory, "edad");
            boot.ConsentView!.AgeButton(AgeBand.Adult).onClick.Invoke();
            Assert.That(boot.ConsentView.View, Is.EqualTo(ConsentView.Choices));
            yield return Take(inventory, "finalidades");

            // La doble confirmación de 13-17, encima del inicio.
            Prime(tables, locale, scale);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            yield return PlayModeHelpers.LoadAppWithConsent(
                new MemoryProfileStore(ProfileAt(4, 0, new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, false, AgeBand.Teen))));
            boot = GameBootstrap.Current!;
            boot.Store!.Purchase(new PurchaseRequest("af_coins_2"), _ => { });
            Assert.That(boot.ConsentView!.View, Is.EqualTo(ConsentView.Confirm));
            yield return Take(inventory, "confirmar_compra");
            boot.ConsentView.ConfirmCancelButton.onClick.Invoke();
            Assert.That(Loc.Locale, Is.EqualTo(locale), "la UI siguió en el idioma medido");
        }

        [UnityTest]
        public IEnumerator CompleteTables_SettingsLanguage_OffersEnglishAndPortuguese()
        {
            var missing = new List<string>();
            LocTableTexts tables = TranslatedTables.Completed(new[] { LocLocale.English, LocLocale.PortugueseBrazil }, out _, out int filled, missing);
            TestContext.WriteLine("AF_T2B celdas de strings.csv aún vacías en en o pt-BR (" + filled.ToString(CultureInfo.InvariantCulture) + "): "
                + string.Join(", ", missing));

            LaunchOptions.LocTablesOverride = tables;
            LaunchOptions.LanguageOverride = LocLocale.SourceLocale;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4, 0, new ConsentRecord(LegalProfiles.Co1581, 1, T0, true, true, AgeBand.Adult))));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.LanguageButton(LocLocale.SourceLocale), Is.Not.Null, "Español");
            Assert.That(settings.LanguageButton(LocLocale.English), Is.Not.Null, "English, con la UI completa");
            Assert.That(settings.LanguageButton(LocLocale.PortugueseBrazil), Is.Not.Null, "Português, con la UI completa");
            Assert.That(settings.LanguageLabel(LocLocale.English), Is.EqualTo("English"));
            Assert.That(settings.LanguageLabel(LocLocale.PortugueseBrazil), Is.EqualTo("Português"));
        }

        private static void Prime(LocTableTexts? tables, string locale, float scale)
        {
            FunnelSetup.Hooks();
            LaunchOptions.LocTablesOverride = tables;
            LaunchOptions.LanguageOverride = locale;
            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
        }

        private static Profile ProfileAt(int nextLevel, int seeds, ConsentRecord consent)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            profile.Wallet.Seeds = seeds;
            profile.Consent = consent;
            return profile;
        }

        private static IEnumerator Take(LocInventory inventory, string name)
        {
            yield return SnapshotKit.Wait(0.3f);
            inventory.Scan("t2b_" + name);
        }
    }
}
