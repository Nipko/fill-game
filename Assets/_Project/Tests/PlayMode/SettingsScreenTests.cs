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
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Ajustes (tarea L3; D-018 Q7-A y Q8-A; 02 §12; GDD.md §2 #5) en la escena <c>Game</c> real: el idioma con el teléfono
    /// fijado (<see cref="ManualDeviceLocale"/>): desarrollo &gt; guardado &gt; teléfono &gt; en, con el respaldo es-419 y las
    /// cifras con la región del teléfono (es-CO sin ella); la pantalla se abre desde el engranaje del inicio y vuelve con la X;
    /// elegir un idioma lo guarda y reconstruye toda la UI volviendo al inicio (en-XA, todo entre corchetes), con Ajustes otra
    /// vez abierta en el idioma nuevo (FXT); los Patrones de
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
            // Teléfono en México: es-* → es-419 y las cifras de es-MX, «1,000».
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

            // Teléfono en otro idioma sin región (en, pt): con en y pt-BR completos (traducción 3, D-033), la UI sale en ese
            // idioma y las cifras, de la fila del idioma de la UI (DefaultNumberTag: en «1,000», pt-BR «1.000»).
            foreach ((string tag, string ui, string next, string grouped) in new[] { ("en", LocLocale.English, "Next", "1,000"), ("pt", LocLocale.PortugueseBrazil, "Próximo", "1.000") })
            {
                LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale(tag);
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
                boot = GameBootstrap.Current!;
                Assert.That(boot.DeviceTag, Is.EqualTo(tag), "AF_STATE loc enseña la etiqueta leída");
                Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(ui), tag);
                Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Device), tag);
                Assert.That(Loc.NumberFormat.Tag, Is.EqualTo(ui), tag + " sin región: la fila del idioma de la UI");
                Assert.That(UiText.Grouped(1000), Is.EqualTo(grouped), tag);
                Assert.That(UiText.Next, Is.EqualTo(next), tag);
            }

            // Teléfono en Brasil: pt-BR completo → la UI en portugués, con las cifras de Brasil.
            LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("pt-BR");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(4)));
            boot = GameBootstrap.Current!;
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.PortugueseBrazil));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Device), "pt-* → pt-BR, ya completo");
            Assert.That(Loc.NumberFormat.Tag, Is.EqualTo("pt-BR"));
            Assert.That(UiText.Next, Is.EqualTo("Próximo"));

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
            Assert.That(settings.LanguageOptions, Is.EqualTo(new[] { "auto", LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil, LocLocale.Pseudo }),
                "Automático, los idiomas con la UI completa (es-419, en y pt-BR desde la traducción 3, D-033) y, en desarrollo, en-XA");
            Assert.That(settings.LanguageLabel("auto"), Is.EqualTo("Automático"));
            Assert.That(settings.LanguageLabel(LocLocale.SourceLocale), Is.EqualTo("Español"), "con su nombre propio");
            Assert.That(settings.LanguageLabel(LocLocale.English), Is.EqualTo("English"), "con su nombre propio");
            Assert.That(settings.LanguageLabel(LocLocale.PortugueseBrazil), Is.EqualTo("Português"), "con su nombre propio");
            Assert.That(settings.LanguageLabel("fr"), Is.Null, "fr no es una columna: no se ofrece");
            Assert.That(settings.LanguageMarked("auto"), Is.True, "lo guardado: auto");
            Assert.That(settings.PatternsLabel(PatternsSetting.Auto), Is.EqualTo("Automático"));
            Assert.That(settings.PatternsLabel(PatternsSetting.On), Is.EqualTo("Siempre"));
            Assert.That(settings.PatternsLabel(PatternsSetting.Off), Is.EqualTo("Nunca"));
            Assert.That(settings.PatternsMarked(PatternsSetting.Auto), Is.True);
            Assert.That(settings.AboutSlot.gameObject.activeSelf, Is.True, "Sobre la Amazonía (F4), con el açaí empezado");
            Assert.That(settings.IdleHintSlot.gameObject.activeSelf, Is.True, "la fila de MO3, a la vista");
            Assert.That(settings.IdleHintLabel, Is.EqualTo("Pista por inactividad"), "02 §12");
            Assert.That(settings.IdleHintMarked && settings.SelectedIdleHint, Is.True, "activada por defecto (02 §12)");
            Assert.That(settings.SlothMessagesSlot.gameObject.activeSelf, Is.True, "la fila de TI3, a la vista");
            Assert.That(settings.SlothMessagesLabel, Is.EqualTo("Mensajes de TiTino"));
            Assert.That(settings.SlothMessagesMarked && settings.SelectedSlothMessages, Is.True, "activada por defecto (D-016)");

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
        public IEnumerator SlothMessages_TogglesAndSavesInTheAct_AndReopensMarked()
        {
            // Ajustes guarda settings.sloth_messages (D-016, punto 4; tarea TI3): se desmarca y se vuelve a marcar sin cerrar.
            var store = new MemoryProfileStore(ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.SlothMessagesMarked, Is.True);

            settings.SlothMessagesButton.onClick.Invoke();
            Assert.That(settings.SlothMessagesMarked, Is.False, "desmarcada: solo gestos");
            Assert.That(settings.Visible, Is.True);
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.SlothMessages, Is.False, "guardado en el acto");

            settings.CloseButton.onClick.Invoke();
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(settings.SlothMessagesMarked, Is.False, "se abre con lo guardado");

            settings.SlothMessagesButton.onClick.Invoke();
            Assert.That(settings.SlothMessagesMarked, Is.True);
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.SlothMessages, Is.True);
            Assert.That(boot.ChooseSlothMessages(false), Is.True);
            Assert.That(settings.SlothMessagesMarked, Is.False);
            Assert.That(((RectTransform)settings.SlothMessagesButton.transform).rect.height, Is.GreaterThanOrEqualTo(48f), "fila ≥ 48 dp");
            settings.CloseButton.onClick.Invoke();
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

            // en-XA: se guarda, Loc cambia y la UI entera se reconstruye en el inicio, con Ajustes otra vez abierta en el idioma
            // nuevo y la opción elegida marcada (FXT, observación 3 de la prueba de la ola 1): sin settings_closed.
            var lines = new List<string>();
            Application.LogCallback onLog = (condition, stack, type) =>
            {
                if (condition.StartsWith("AF_STATE ", StringComparison.Ordinal))
                {
                    lines.Add(condition);
                }
            };
            Application.logMessageReceived += onLog;
            GameBootstrap boot;
            try
            {
                first.SettingsView.LanguageButton(LocLocale.Pseudo)!.onClick.Invoke();
                Assert.That(Loc.Locale, Is.EqualTo(LocLocale.Pseudo));
                Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Language, Is.EqualTo(LocLocale.Pseudo), "guardado antes de reconstruir");
                yield return PlayModeHelpers.WaitFor(
                    () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                    "reconstrucción de la UI en en-XA");
                yield return null;
                boot = GameBootstrap.Current!;
            }
            finally
            {
                Application.logMessageReceived -= onLog;
            }

            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "vuelve al inicio");
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.Pseudo));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Saved));
            AssertBracketed(boot.Lobby!.Canvas, "inicio");
            SettingsScreen settings = boot.SettingsView!;
            Assert.That(settings.Visible, Is.True, "elegir un idioma no cierra Ajustes: sigue abierta sobre el inicio");
            Assert.That(boot.Lobby.Visible, Is.True, "con el inicio debajo");
            Assert.That(boot.PressSettings(), Is.False, "ya abierta");
            Assert.That(settings.LanguageMarked(LocLocale.Pseudo), Is.True, "con la opción elegida marcada");
            Assert.That(settings.PatternsMarked(PatternsSetting.Auto), Is.True);
            AssertBracketed(settings.Canvas, "Ajustes");
            Assert.That(lines.Exists(l => l.StartsWith("AF_STATE {\"event\":\"settings_closed\"", StringComparison.Ordinal)), Is.False,
                "no se cerró: no sale settings_closed");
            Assert.That(lines.FindLast(l => l.StartsWith("AF_STATE {\"event\":\"settings\"", StringComparison.Ordinal)), Does.Contain("\"language\":\"en-XA\""),
                "la sonda vuelve a escribir AF_STATE settings, en el idioma nuevo");

            // Automático: el teléfono en español (PlayModeHelpers) → es-419, y otra vez se reconstruye.
            settings.LanguageButton("auto")!.onClick.Invoke();
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Language, Is.EqualTo(SettingsData.LanguageAuto));
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "reconstrucción de la UI en es-419");
            boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(boot.Locale!.Value.Source, Is.EqualTo(LocSource.Device));
            Assert.That(boot.Lobby!.PlayLabel, Is.EqualTo("Jugar · Nivel 4"));
            Assert.That(boot.SettingsView!.Visible && boot.SettingsView.LanguageMarked("auto"), Is.True, "Ajustes sigue abierta, con Automático");
            Assert.That(boot.SettingsView.Title, Is.EqualTo("Ajustes"), "ya en español");
            Assert.That(boot.CloseSettings(), Is.True, "y se cierra como siempre");
            Assert.That(boot.ChooseLanguage("fr"), Is.False, "un idioma que no se ofrece no se guarda");
            Assert.That(ProfileJson.Deserialize(store.Json!).Settings.Language, Is.EqualTo(SettingsData.LanguageAuto));
        }

        [UnityTest]
        public IEnumerator ChoosingALanguage_KeepsTheSessionWallet_WithoutReapplyingTheDevExtras()
        {
            // Build de desarrollo abierto con af_seeds 500 y af_canoes 2 (simulados): se aplican al arrancar en frío…
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.DevWalletOverride = (500, 2);
            var store = new MemoryProfileStore(ProfileAt(4));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(first.Wallet!.Seeds, Is.EqualTo(500), "af_seeds al arrancar");
            Assert.That(first.Wallet.Canoes.Count, Is.EqualTo(2), "af_canoes al arrancar");
            Assert.That(first.Wallet.Apply(Transaction.Earn(SeedSource.Basket, 50)).IsApplied, Is.True);
            Assert.That(first.Wallet.Seeds, Is.EqualTo(550), "lo ganado en la sesión");

            // …pero volver al inicio tras elegir idioma no es un arranque en frío: la cartera sigue como estaba.
            Assert.That(first.PressSettings(), Is.True);
            first.SettingsView!.LanguageButton(LocLocale.Pseudo)!.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "reconstrucción de la UI en en-XA");
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.Pseudo));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(550), "los extras de desarrollo no se vuelven a aplicar");
            Assert.That(boot.Wallet.Canoes.Count, Is.EqualTo(2));
            Assert.That(ProfileJson.Deserialize(store.Json!).Wallet.Seeds, Is.EqualTo(550), "ni se guardan encima de lo jugado");
        }

        [UnityTest]
        public IEnumerator ChoosingALanguage_WithTheProfileLocked_KeepsWhatWasPlayedInMemory()
        {
            // Perfil en solo lectura (08 §4.4): lo jugado en la sesión solo vive en memoria, y la reconstrucción no lo descarta.
            var store = new ReadOnlyProfileStore(ProfileLoadStatus.TooNew);
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap first = GameBootstrap.Current!;
            Assert.That(first.ProfileLocked, Is.True);
            first.LockedNotice!.PlayButton.onClick.Invoke();
            int seeds = first.Wallet!.Seeds;
            Assert.That(first.Wallet.Apply(Transaction.Earn(SeedSource.Basket, 75)).IsApplied, Is.True);
            Profile played = first.Profile!;

            Assert.That(first.PressSettings(), Is.True);
            first.SettingsView!.LanguageButton(LocLocale.Pseudo)!.onClick.Invoke();
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "reconstrucción de la UI en en-XA");
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.ProfileLocked, Is.True, "sigue en solo lectura");
            Assert.That(boot.Profile, Is.SameAs(played), "el mismo perfil en memoria, no uno en blanco");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(seeds + 75), "lo ganado en la sesión sigue");
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.Pseudo), "con el idioma elegido, aunque no se pudo guardar");
            Assert.That(boot.Services!.PurchasesBlocked, Is.True, "y sin compras");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
        }

        [UnityTest]
        public IEnumerator ChoosingALanguage_WithANewerProfileOnDisk_NeverOverwritesIt()
        {
            // 08 §4.4: un profile.json de un esquema más nuevo (TooNew) «no se sobrescribe». Con el LocalProfileStore real del
            // arranque: elegir idioma reconstruye la escena, y el arranque siguiente no puede crear otro almacén sin Load (sin
            // bloquear) que guarde el perfil en blanco de la sesión encima (revisión de L3).
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "af-l3-toonew-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                string mainPath = System.IO.Path.Combine(dir, LocalProfileStore.FileName);
                string backupPath = System.IO.Path.Combine(dir, LocalProfileStore.BackupFileName);
                System.IO.File.WriteAllText(mainPath, "{\"profile_schema\": " + (Profile.CurrentSchema + 1) + ", \"wallet\": {\"seeds\": 5000}}");
                System.IO.File.WriteAllText(backupPath, ProfileJson.Serialize(ProfileAt(4)));
                byte[] main = System.IO.File.ReadAllBytes(mainPath);
                byte[] backup = System.IO.File.ReadAllBytes(backupPath);

                LaunchOptions.ClockOverride = new ManualClock(T0);
                yield return PlayModeHelpers.LoadAppFromDisk(dir);
                GameBootstrap first = GameBootstrap.Current!;
                Assert.That(first.ProfileLocked, Is.True, "TooNew");
                Assert.That(first.ProfileStore, Is.InstanceOf<LocalProfileStore>());
                first.LockedNotice!.PlayButton.onClick.Invoke();

                Assert.That(first.PressSettings(), Is.True);
                first.SettingsView!.LanguageButton(LocLocale.Pseudo)!.onClick.Invoke();
                yield return PlayModeHelpers.WaitFor(
                    () => GameBootstrap.Current != null && GameBootstrap.Current != first && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                    "reconstrucción de la UI en en-XA");
                GameBootstrap boot = GameBootstrap.Current!;
                Assert.That(boot.ProfileLocked, Is.True, "sigue en solo lectura");
                Assert.That(boot.ProfileStore, Is.SameAs(first.ProfileStore), "el mismo almacén, que sigue bloqueado");
                Assert.That(((LocalProfileStore)boot.ProfileStore!).WriteBlocked, Is.True);
                Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.Pseudo));

                // Patrones (SaveSettings), una mutación de la cartera y el cierre de la escena (PersistNow de OnDestroy): nada
                // llega al disco.
                Assert.That(boot.ChoosePatterns(PatternsSetting.On), Is.True);
                Assert.That(boot.Profile!.Settings.Patterns, Is.EqualTo(PatternsSetting.On), "en memoria, para esta sesión");
                Assert.That(boot.Wallet!.Apply(Transaction.Earn(SeedSource.Basket, 50)).IsApplied, Is.True);
                UnityEngine.Object.Destroy(boot.gameObject);
                yield return null;
                yield return null;
                Assert.That(GameBootstrap.Current, Is.Null, "escena destruida");

                Assert.That(System.IO.File.ReadAllBytes(mainPath), Is.EqualTo(main), "profile.json intacto");
                Assert.That(System.IO.File.ReadAllBytes(backupPath), Is.EqualTo(backup), "profile.bak intacto");
                Assert.That(System.IO.File.Exists(System.IO.Path.Combine(dir, LocalProfileStore.TempFileName)), Is.False, "ni un .tmp");
            }
            finally
            {
                System.IO.Directory.Delete(dir, true);
            }
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

        [Test]
        public void Settings_OptionBoundaryHasContrast3()
        {
            // WCAG 1.4.11: el límite de una opción no elegida se distingue de la tarjeta (revisión de L3).
            foreach ((string name, Color boundary, Color background) in SettingsScreen.BoundaryContrastPairs())
            {
                Assert.That(ColorMath.ContrastRatio(boundary, background), Is.GreaterThanOrEqualTo(3f), name);
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

        /// <summary>
        /// Los miles según el idioma y el país (observación 22 de la jornada 6, decisión del dueño del 2026-10-30): con el teléfono
        /// en en-US (la región que ponía «13,040» en todos los idiomas) y la UI en pt-BR, el saldo del inicio y el del Mercado dicen
        /// «13.040» y ningún botón del Mercado lleva la coma de miles; en es-419, el país de la cuenta de Play manda sobre la región
        /// del teléfono (Colombia «13.040», México «13,040»); en en, «13,040».
        /// </summary>
        [UnityTest]
        public IEnumerator Balance_ThousandsFollowTheLanguage_AndInSpanishTheCountry()
        {
            try
            {
                Profile pt = ProfileAt(26);
                pt.Wallet.Seeds = 13040;
                pt.Settings.Language = LocLocale.PortugueseBrazil;
                LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("en-US");
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(pt));
                GameBootstrap boot = GameBootstrap.Current!;
                yield return null;
                Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.PortugueseBrazil));
                Assert.That(Loc.NumberFormat.Tag, Is.EqualTo("pt-BR"), "pt-BR, aunque el teléfono esté en en-US");
                Assert.That(boot.Lobby!.SeedsShown, Is.EqualTo(13040));
                Assert.That(boot.Lobby.SeedsText, Is.EqualTo("13.040"), "el saldo del inicio en pt-BR");

                Assert.That(boot.SetUpMarket(), Is.True);
                boot.TickMarket(16f);
                Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
                MarketScreen market = boot.Market!;
                Assert.That(market.SeedsText, Is.EqualTo("13.040"), "el saldo del Mercado en pt-BR");
                for (int i = 0; i < market.Rows.Count; i++)
                {
                    Assert.That(market.RowButtonText(i), Does.Not.Match(@"\d,\d{3}(?!\d)"), "pt-BR sin coma de miles: " + market.RowButtonText(i));
                }

                foreach ((string? play, string device, string expected) in new[] { ("CO", "es-MX", "13.040"), ((string?)null, "es-MX", "13,040"), ("MX", "es-AR", "13,040"), ((string?)null, "es", "13.040") })
                {
                    Profile es = ProfileAt(26);
                    es.Wallet.Seeds = 13040;
                    GameBootstrap.PlayAccountCountryOverride = new MemoryPlayAccountCountry(play);
                    LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale(device);
                    yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(es));
                    boot = GameBootstrap.Current!;
                    yield return null;
                    string at = " (Play " + (play ?? "-") + ", teléfono " + device + ")";
                    Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.SourceLocale), at);
                    Assert.That(boot.Lobby!.SeedsText, Is.EqualTo(expected), "es-419" + at);
                }

                Profile en = ProfileAt(26);
                en.Wallet.Seeds = 13040;
                en.Settings.Language = LocLocale.English;
                GameBootstrap.PlayAccountCountryOverride = new MemoryPlayAccountCountry("BR");
                LaunchOptions.DeviceLocaleOverride = new ManualDeviceLocale("pt-BR");
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(en));
                boot = GameBootstrap.Current!;
                yield return null;
                Assert.That(boot.Lobby!.SeedsText, Is.EqualTo("13,040"), "en siempre con coma, aunque el país sea Brasil");
            }
            finally
            {
                GameBootstrap.PlayAccountCountryOverride = null;
            }
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
    public sealed class SettingsBackButtonTests : UiInputTestFixture
    {
        /// <inheritdoc />
        public override void TearDown()
        {
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
