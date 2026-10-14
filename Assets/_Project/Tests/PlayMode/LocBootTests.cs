#nullable enable
using System.Collections;
using System.Text;
using AF.GameFlow;
using AF.Localization;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Carga de los textos al arrancar (D-018, nivel 2; tarea L2b): la escena <c>Game</c> lleva las dos tablas
    /// (<c>GameSceneBuilder</c>) y <c>GameBootstrap</c> configura <see cref="Loc"/> con ellas antes de construir la UI. Sin
    /// <c>af_lang</c>, el idioma automático da es-419 (el único con la UI completa); con <c>af_lang</c> = en-XA (gancho
    /// <see cref="LaunchOptions.LanguageOverride"/>), todo texto de la pantalla sale entre corchetes; y un <c>af_lang</c> sin la
    /// UI completa («en») se queda en es-419 con un aviso.
    /// </summary>
    public sealed class LocBootTests
    {
        [SetUp]
        public void SetUp()
        {
            LaunchOptions.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator Scene_LoadsBothTables_AndTheAutomaticLanguageIsEs419()
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.StringsTableAsset, Is.Not.Null, "strings.csv en la escena (GameSceneBuilder)");
            Assert.That(boot.FactsTableAsset, Is.Not.Null, "facts.csv en la escena (GameSceneBuilder)");
            Assert.That(boot.Locale.HasValue, Is.True);
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Fallback), "sin af_lang ni idioma guardado o del teléfono (L3)");
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(Loc.Catalog.Strings.Count, Is.GreaterThan(100));
            Assert.That(Loc.Catalog.Facts.Count, Is.GreaterThan(0));
            Assert.That(UiText.Next, Is.EqualTo("Siguiente"));
            Assert.That(UiText.FaunaFact("fauna_saimiri_collinsi_dato"), Is.EqualTo("Cruza el bosque en fila, por ramas finas"));
        }

        [UnityTest]
        public IEnumerator AfLang_EnXA_PutsEveryTextOfTheScreenBetweenBrackets()
        {
            LaunchOptions.LanguageOverride = LocLocale.Pseudo;
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.Pseudo));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Dev));
            Assert.That(Loc.Locale, Is.EqualTo(LocLocale.Pseudo));
            boot.TapAnywhere();
            yield return null;

            var outside = new StringBuilder();
            int bracketed = 0;
            foreach (TMP_Text text in boot.View!.Canvas.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!text.isActiveAndEnabled || !HasLetters(text.text))
                {
                    continue;
                }

                if (text.text.IndexOf('[') >= 0 && text.text.IndexOf(']') > 0)
                {
                    bracketed++;
                }
                else
                {
                    outside.Append(text.name).Append(": «").Append(text.text).Append("» ");
                }
            }

            Assert.That(bracketed, Is.GreaterThan(0), "textos de la tabla en pantalla");
            Assert.That(outside.ToString(), Is.Empty, "todo texto con letras sale de la tabla, entre corchetes");
            Assert.That(UiText.Level(1), Does.StartWith("[").And.Contain("1"));
        }

        [UnityTest]
        public IEnumerator AfLang_WithoutACompleteUi_StaysInEs419()
        {
            LaunchOptions.LanguageOverride = LocLocale.English;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("af_lang = \"en\" no tiene la UI completa: se usa es-419"));
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Locale!.Value.Locale, Is.EqualTo(LocLocale.SourceLocale));
            Assert.That(boot.Locale.Value.Source, Is.EqualTo(LocSource.Fallback));
            Assert.That(UiText.Next, Is.EqualTo("Siguiente"));
        }

        private static bool HasLetters(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsLetter(s[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
