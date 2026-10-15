#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Ajustes (tarea L3; D-018 Q7-A y Q8-A; 02 §12; GDD.md §2 #5) en la escena <c>Game</c> real: el idioma con el teléfono
    /// fijado (<see cref="ManualDeviceLocale"/>): desarrollo &gt; guardado &gt; teléfono &gt; en, con el respaldo es-419 y las
    /// cifras con la región del teléfono (es-CO sin ella); la pantalla se abre desde el engranaje del inicio y vuelve con la X;
    /// elegir un idioma lo guarda y reconstruye toda la UI volviendo al inicio (en-XA, todo entre corchetes); los Patrones de
    /// fruta (auto, on y off) se guardan y se aplican al nivel cargado sin recargarlo; y la hoja «Compra simulada» enseña los
    /// nombres de la tabla, nunca las claves. Todo texto ≥ 12 sp y sin recortes a 360 × 640 y al 130 %.
    /// </summary>
    public sealed class SettingsScreenTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 14, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator Language_DevThenSavedThenPhoneThenEn_AndTheNumbersFollowThePhoneRegion()
        {
            // Teléfono en México: es-* → es-419 (el único completo) y las cifras de es-MX, «1,000».
            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("es-MX");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.DeviceTag, Is.EqualTo("es-MX"));
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Device), "el idioma del teléfono");
            Assert.That(Loc.NumberFormat.Tag, Is.EqualTo("es-MX"), "Q8-A: las cifras, con la región del teléfono");
            Assert.That(UiText.Grouped(1000), Is.EqualTo("1,000"));
            Assert.That(boot.Services!.DeviceLocale, Is.SameAs(LaunchOptions.DeviceLocaleOverride), "GameServices lleva el idioma del teléfono");

            // Teléfono en español sin región: es-419 y las cifras de es-CO [P].
            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("es");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
            boot = GameBootstrap.Current!;
            Assert.That(boot.Locale!.Value.Source, Is.EqualTo(LocSource.Device));
            Assert.That(Loc.NumberFormat.Tag, Is.EqualTo("es-CO"), "sin región, es-CO [P]");
            Assert.That(UiText.Grouped(1000), Is.EqualTo("1.000"));

            // Teléfono en Brasil: pt-BR aún no está completo y en tampoco → es-419 de respaldo, con las cifras de Brasil.
            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("pt-BR");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
            boot = GameBootstrap.Current!;
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Fallback), "notas de la tanda 4, punto 10");
            Assert.That(Loc.NumberFormat.Tag, Is.EqualTo("pt-BR"));
            Assert.That(UiText.Next, Is.EqualTo("Siguiente"), "nunca otro idioma a medias");

            // El idioma guardado manda sobre el del teléfono…
            Profile saved = ProfileAt(4);
            saved.Settings.Language = LocLocale.Pseudo;
            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("es-MX");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(saved));
            boot = GameBootstrap.Current!;
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.Pseudo));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Saved), "settings.language > teléfono");
            Assert.That(boot.Lobby!.PlayLabel, Does.StartWith("["), "la UI se construyó en en-XA");

            // …y el de desarrollo (af_lang), sobre el guardado.
            LaunchOptions.LanguageOverride = LocLocale.SourceLocale;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(saved));
            boot = GameBootstrap.Current!;
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Dev), "af_lang > settings.language");
        }

        [UnityTest]
        public IEnumerator Settings_OpensFromTheLobbyGear_ShowsTheOptions_AndTheXReturns()
        {
            var store = new MemoryProfileStore(ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(settings.Visible, Is.False);

            lobby.SettingsButton.onClick.Invoke();
            Assert.That(settings.Visible, Is.True, "el engranaje abre Ajustes");
            Assert.That(boot.PressSettings(), Is.False, "ya abierta");
            Assert.That(settings.Title, Is.EqualTo("Ajustes"));
            Assert.That(settings.LanguageTitle, Is.EqualTo("Idioma"));
            Assert.That(settings.PatternsTitle, Is.EqualTo("Patrones de fruta"));
            Assert.That(settings.LanguageOptions, Is.EqualTo(new[] { "auto", LocLocale.SourceLocale, LocLocale.Pseudo }),
                "Automático, los idiomas con la UI completa (hoy es-419) y, en desarrollo, en-XA");
            Assert.That(settings.LanguageLabel("auto"), Is.EqualTo("Automático"));
            Assert.That(settings.LanguageLabel(LocLocale.SourceLocale), Is.EqualTo("Español"), "con su nombre propio");
            Assert.That(settings.LanguageLabel("en"), Is.Null, "en no está completo: no se ofrece");
            Assert.That(settings.LanguageMarked("auto"), Is.True, "lo guardado: auto");
            Assert.That(settings.PatternsLabel(PatternsSetting.Auto), Is.EqualTo("Automático"));
            Assert.That(settings.PatternsLabel(PatternsSetting.On), Is.EqualTo("Siempre"));
            Assert.That(settings.PatternsLabel(PatternsSetting.Off), Is.EqualTo("Nunca"));
            Assert.That(settings.PatternsMarked(PatternsSetting.Auto), Is.True);
            Assert.That(settings.SlothMessagesSlot.gameObject.activeSelf || settings.IdleHintSlot.gameObject.activeSelf || settings.AboutSlot.gameObject.activeSelf,
                Is.False, "los huecos de TI3, MO3 y F4, ocultos (D-008)");

            // Patrones desde su botón: se guarda y se marca, sin cerrar.
            settings.PatternsButton(PatternsSetting.On).onClick.Invoke();
            Assert.That(settings.PatternsMarked(PatternsSetting.On) && !settings.PatternsMarked(PatternsSetting.Auto), Is.True);
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Patterns, Is.EqualTo(PatternsSetting.On), "guardado en el acto");
            Assert.That(settings.Visible, Is.True);

            settings.CloseButton.onClick.Invoke();
            Assert.That(settings.Visible, Is.False, "la X cierra");
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby), "y vuelve al inicio");
            Assert.That(lobby.Visible, Is.True);
            Assert.That(boot.CloseSettings(), Is.False);

            // Solo desde el inicio.
            lobby.PlayButton.onClick.Invoke();
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(boot.PressSettings(), Is.False, "en el nivel no se abre");
            Assert.That(boot.View!.FruitPatternsOn, Is.True, "el nivel 4 sale con los patrones de «Siempre»");
        }

        [UnityTest]
        public IEnumerator ChoosingEnXA_SavesIt_AndRebuildsTheWholeUiBackInTheLobby()
        {
            var store = new MemoryProfileStore(ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(first.Lobby!.PlayLabel, Is.EqualTo("Jugar · Nivel 4"));

            // Español, el que ya está: se guarda y no se reconstruye nada.
            first.PressSettings();
            first.SettingsView!.LanguageButton(LocLocale.SourceLocale)!.onClick.Invoke();
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Language, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(first.SettingsView.LanguageMarked(LocLocale.SourceLocale), Is.True);
            Assert.That(first.SettingsView.Visible, Is.True, "mismo idioma: Ajustes sigue abierta");
            Assert.That(first.Locale!.Value.Source, Is.EqualTo(LocSource.Saved));
            yield return null;
            Assert.That(GameBootstrap.Current, Is.SameAs(first));

            // en-XA: se guarda, Loc cambia y la UI entera se reconstruye en el inicio.
            first.SettingsView.LanguageButton(LocLocale.Pseudo)!.onClick.Invoke();
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.Pseudo));
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Language, Is.EqualTo(LocLocale.Pseudo), "guardado antes de reconstruir");
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "reconstrucción de la UI en en-XA");
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "vuelve al inicio");
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.Pseudo));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Saved));
            AssertBracketed(boot.Lobby!.Canvas, "inicio");
            boot.PressSettings();
            yield return null;
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.LanguageMarked(LocLocale.Pseudo), Is.True);
            AssertBracketed(settings.Canvas, "Ajustes");

            // Automático: el teléfono no da nada → es-419 de respaldo, y otra vez se reconstruye.
            settings.LanguageButton("auto")!.onClick.Invoke();
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Language, Is.EqualTo(SettingsData.LanguageAuto));
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "reconstrucción de la UI en es-419");
            boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(boot.Locale!.Value.Source, Is.EqualTo(LocSource.Fallback));
            Assert.That(boot.Lobby!.PlayLabel, Is.EqualTo("Jugar · Nivel 4"));
            Assert.That(boot.ChooseLanguage("en"), Is.False, "un idioma que no se ofrece no se guarda");
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Language, Is.EqualTo(SettingsData.LanguageAuto));
        }

        [UnityTest]
        public IEnumerator Patterns_OnOffAuto_ApplyToTheLoadedLevel_WithoutReloading()
        {
            var store = new MemoryProfileStore(ProfileAt(41));
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(view.FruitPatternsOn, Is.True, "auto en el 41: encendidos");
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(Palette.PatternOpacityOn).Within(1e-6f));

            Assert.That(boot.ChoosePatterns(PatternsSetting.Off), Is.True);
            Assert.That(view.FruitPatternsOn, Is.False, "off: se apagan al momento");
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(Palette.PatternOpacitySubtle).Within(1e-6f), "patrón sutil del 15 %");
            Assert.That(boot.Flow, Is.SameAs(flow));
            Assert.That(flow.LevelNumber, Is.EqualTo(41), "sin recargar el nivel");
            Assert.That(GameBootstrap.Current, Is.SameAs(boot));
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Patterns, Is.EqualTo(PatternsSetting.Off));

            Assert.That(boot.ChoosePatterns(PatternsSetting.On), Is.True);
            Assert.That(view.FruitPatternsOn, Is.True);
            Assert.That(boot.ChoosePatterns(PatternsSetting.Auto), Is.True);
            Assert.That(view.FruitPatternsOn, Is.True, "auto en el 41");

            // On en el 40: encendidos aunque el automático los apague hasta el 40.
            Profile on = ProfileAt(40);
            on.Settings.Patterns = PatternsSetting.On;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(on);
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            Assert.That(GameBootstrap.Current!.View!.FruitPatternsOn, Is.True, "on: también hasta el 40");
            Assert.That(GameBootstrap.Current.ChoosePatterns(PatternsSetting.Auto), Is.True);
            Assert.That(GameBootstrap.Current.View!.FruitPatternsOn, Is.False, "auto en el 40: apagados");
        }

        [UnityTest]
        public IEnumerator StoreSheet_ShowsTheNamesOfTheTable_NotTheKeys()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(31)));
            GameBootstrap boot = GameBootstrap.Current!;
            StoreSheet sheet = boot.StoreSheetView!;
            Assert.That(sheet.TitleText, Is.EqualTo("Compra simulada"), "el rótulo, de iap_simulated_purchase");
            Assert.That(boot.AdOverlayView!.TitleText, Is.EqualTo("Anuncio simulado"), "y el del anuncio, de ad_simulated");

            int shown = 0;
            foreach (StoreProduct product in StoreCatalog.Default.Products)
            {
                Assert.That(Loc.Has(product.NameKey), Is.True, product.NameKey + " en strings.csv");
                boot.Store!.Purchase(new PurchaseRequest(product.Sku, PurchaseContext.Shop, 31), _ => { });
                if (!sheet.Visible)
                {
                    continue; // el registro no lo vende ahora (p. ej. la Balsa fuera de Aguas estancadas)
                }

                shown++;
                Assert.That(sheet.ProductText, Is.EqualTo(Loc.T(product.NameKey)), product.Sku);
                Assert.That(sheet.ProductText, Does.Not.Contain("iap_").And.Not.Contain("["), product.Sku + ": el nombre, no la clave");
                sheet.Close(false);
            }

            Assert.That(shown, Is.GreaterThanOrEqualTo(10), "casi todo el catálogo se puede abrir desde el Mercado");
            Assert.That(Loc.T("iap_af_coins_2_name"), Is.EqualTo("Puñado de semillas"), "05 §4.1");
            Assert.That(Loc.T("iap_af_skin_m03_name"), Is.EqualTo("Pack Cuia del Encuentro"));
        }

        [UnityTest]
        public IEnumerator Settings_FitsAt360x640_AndAt130Percent_WithEveryText12SpOrMore()
        {
            foreach (string locale in new[] { LocLocale.SourceLocale, LocLocale.Pseudo })
            {
                LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
                GameBootstrap boot = GameBootstrap.Current!;
                boot.PressSettings();
                yield return null;
                SettingsScreen settings = boot.SettingsView!;
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    settings.FontScale = scale;
                    Canvas.ForceUpdateCanvases();
                    string where = locale + " al " + Mathf.RoundToInt(scale * 100f) + " %";
                    Assert.That(settings.CanvasHeight, Is.EqualTo(640f).Within(0.5f), "360 × 640 dp");
                    AssertFits(settings, where);
                }
            }
        }

        [Test]
        public void Settings_TextColorsHaveContrast45()
        {
            foreach ((string name, Color text, Color background) in SettingsScreen.ContrastPairs())
            {
                Assert.That(ColorMath.ContrastRatio(text, background), Is.GreaterThanOrEqualTo(4.5f), name);
            }
        }

        internal static void AssertFits(SettingsScreen settings, string where)
        {
            Rect panel = settings.Panel.rect;
            Vector2 pos = settings.Panel.anchoredPosition;
            float top = -pos.y;
            Assert.That(top, Is.GreaterThanOrEqualTo(GameScreen.MinSafeTop), "la tarjeta, bajo la zona segura: " + where);
            Assert.That(top + panel.height, Is.LessThanOrEqualTo(settings.CanvasHeight - GameScreen.MinSafeBottom + 0.5f), "y sobre la de gestos: " + where);
            Assert.That(pos.x >= 0f && pos.x + panel.width <= settings.CanvasWidth + 0.5f, Is.True, "a lo ancho: " + where);
            Canvas.ForceUpdateCanvases();
            foreach (TextMeshProUGUI text in settings.Texts())
            {
                text.ForceMeshUpdate(true, true);
                Rect r = text.rectTransform.rect;
                string what = text.name + " «" + text.text + "» (" + where + ")";
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(12f), "≥ 12 sp: " + what);
                Assert.That(text.overflowMode, Is.EqualTo(TextOverflowModes.Overflow), "nunca se trunca: " + what);
                Assert.That(text.textInfo.characterCount, Is.EqualTo(text.text.Length), "entero: " + what);
                Bounds b = text.textBounds;
                Assert.That(b.min.x >= r.xMin - 0.5f && b.max.x <= r.xMax + 0.5f, Is.True, "cabe a lo ancho: " + what);
                Assert.That(b.min.y >= r.yMin - 0.5f && b.max.y <= r.yMax + 0.5f, Is.True, "cabe a lo alto: " + what);
            }

            foreach (string id in settings.LanguageOptions)
            {
                RectTransform row = (RectTransform)settings.LanguageButton(id)!.transform;
                Assert.That(row.rect.height, Is.GreaterThanOrEqualTo(48f), "opción ≥ 48 dp: " + id + " (" + where + ")");
            }

            Assert.That(((RectTransform)settings.CloseButton.transform).rect.height, Is.GreaterThanOrEqualTo(48f));
        }

        private static void AssertBracketed(Canvas canvas, string what)
        {
            int texts = 0;
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!text.isActiveAndEnabled || !HasLetters(text.text))
                {
                    continue;
                }

                texts++;
                Assert.That(text.text, Does.StartWith("[").And.Contain("]"), what + ": " + text.name + " «" + text.text + "»");
            }

            Assert.That(texts, Is.GreaterThan(0), what + ": hay textos");
        }

        private static bool HasLetters(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsLetter(text[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }
    }

    /// <summary>
    /// El Atrás de Android cierra Ajustes (tarea L3): llega como <c>Keyboard.escapeKey</c> (ver <see cref="BackButtonTests"/>),
    /// y con Ajustes abierto vuelve al inicio, que sigue igual.
    /// </summary>
    public sealed class SettingsBackButtonTests : InputTestFixture
    {
        /// <inheritdoc />
        public override void TearDown()
        {
            InputSystemUIInputModule[] modules = UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None);
            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].enabled = false;
            }

            LaunchOptions.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator AndroidBack_ClosesSettings_AndStaysInTheLobby()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(SettingsScreenTests.ProfileAt(4)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            yield return null;
            Assert.That(boot.SettingsView!.Visible, Is.True);

            Press(keyboard.escapeKey);
            yield return null;
            yield return null;
            Release(keyboard.escapeKey);
            yield return null;
            Assert.That(boot.SettingsView.Visible, Is.False, "Atrás cierra Ajustes");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "y se queda en el inicio");
            Assert.That(boot.Lobby!.Visible, Is.True);

            // Sin Ajustes abierto, Atrás no hace nada en el inicio.
            Press(keyboard.escapeKey);
            yield return null;
            Release(keyboard.escapeKey);
            yield return null;
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
        }
    }
}
