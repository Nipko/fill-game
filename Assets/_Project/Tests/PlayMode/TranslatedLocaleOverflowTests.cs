#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Las traducciones de en y pt-BR contra sus cajas (tarea T2 del Spark: traducción con IA, D-022, pregunta 51 (b); D-018 §3.7;
    /// 02 §12: sin desbordes a 360 dp, con la fuente al 100 % y al 130 %): el mismo recorrido de pantallas que
    /// <see cref="PseudoLocOverflowTests"/> en en-XA (<see cref="PseudoLocWalk"/>: inicio, tarjeta previa, banner de mundo,
    /// bandeja de comodines, victoria, Aguas estancadas en sus ramas, ¿Seguro? y Nivel fallido), pero con la UI en en y en pt-BR,
    /// y <b>falla</b> si algún texto visible se sale de su caja, no cabe ni al mínimo del autoajuste, baja de 12 sp o se parte a
    /// media palabra (<see cref="LocInventory"/>).
    /// <para>
    /// Mientras en y pt-BR estén incompletos, el idioma de desarrollo no se puede elegir (<see cref="LocTables.Configure(LocCatalog, string, bool)"/>
    /// se queda en es-419): el test arranca con las tablas del proyecto (<see cref="LaunchOptions.LocTablesOverride"/>) en las que
    /// cada celda vacía del idioma medido se rellena, solo aquí, con el texto de es-419 (que ya mide la compuerta de en-XA con un
    /// 35 % más). Es un andamio de medida, nunca una tabla del juego: lo que se mide de verdad son las traducciones que ya están.
    /// </para>
    /// </summary>
    public sealed class TranslatedLocaleOverflowTests
    {
        [TearDown]
        public void TearDown()
        {
            PseudoLocWalk.Release();
        }

        [UnityTest]
        public IEnumerator Translated_360x640_NoTextOverflows([Values(LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent)
        {
            float scale = percent / 100f;
            var inventory = new LocInventory(locale, scale);
            LocTableTexts tables = TranslatedTables.Completed(locale, out int translated, out int filled);
            yield return TranslatedWalk.Run(locale, scale, tables, inventory);
            TestContext.WriteLine("AF_T2 " + locale + " al " + percent + " %: " + inventory.Scanned.ToString(CultureInfo.InvariantCulture) + " textos mirados, "
                + inventory.Problems.ToString(CultureInfo.InvariantCulture) + " con problemas; strings.csv con " + translated.ToString(CultureInfo.InvariantCulture)
                + " traducciones y " + filled.ToString(CultureInfo.InvariantCulture) + " celdas de andamio en es-419");
            foreach (string note in inventory.Notes)
            {
                TestContext.WriteLine("AF_T2 aviso de contenido: " + note);
            }

            Assert.That(inventory.Scanned, Is.GreaterThan(80), "textos visibles mirados");
            Assert.That(inventory.Lines, Is.Empty, "textos que se salen o bajan de 12 sp en " + locale + " al " + percent + " %:\n" + string.Join("\n", inventory.Lines));
        }

        [Test]
        public void Completed_FillsOnlyTheEmptyCellsOfThatLocale_AndMakesItSelectable()
        {
            LocTableTexts tables = TranslatedTables.Completed(LocLocale.English, out int translated, out int filled);
            LocCatalog catalog = LocCatalog.Parse(tables.Strings!, tables.Facts!);
            Assert.That(catalog.IsComplete(LocLocale.English), Is.True, "el andamio completa en para poder elegirlo");
            Assert.That(translated + filled, Is.EqualTo(catalog.Strings.Count));
            LocTable real = LocTable.Parse(LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile), LocCatalog.StringsFile);
            for (int r = 0; r < real.Count; r++)
            {
                string key = real.KeyAt(r);
                int row = catalog.Strings.IndexOf(key);
                string en = real.TextAt(r, LocLocale.English);
                Assert.That(catalog.Strings.TextAt(row, LocLocale.English), Is.EqualTo(en.Length > 0 ? en : real.TextAt(r, LocLocale.SourceLocale)), key);
                Assert.That(catalog.Strings.TextAt(row, LocLocale.PortugueseBrazil), Is.EqualTo(real.TextAt(r, LocLocale.PortugueseBrazil)), key + ": pt-BR no se toca");
                Assert.That(catalog.Strings.TextAt(row, LocLocale.SourceLocale), Is.EqualTo(real.TextAt(r, LocLocale.SourceLocale)), key);
            }
        }
    }

    /// <summary>El andamio de <see cref="TranslatedLocaleOverflowTests"/>: las tablas del proyecto con un idioma completado.</summary>
    internal static class TranslatedTables
    {
        /// <summary>
        /// Las tablas del proyecto con las celdas vacías de <paramref name="locale"/> en <c>strings.csv</c> rellenas con su es-419;
        /// <paramref name="translated"/> cuenta las que ya tenían traducción y <paramref name="filled"/>, las rellenas.
        /// <c>facts.csv</c> va tal cual (un dato sin traducir no se muestra: D-018, Q10-A).
        /// </summary>
        public static LocTableTexts Completed(string locale, out int translated, out int filled)
        {
            return Completed(new[] { locale }, out translated, out filled, null);
        }

        /// <summary>
        /// Como <see cref="Completed(string, out int, out int)"/>, con varios idiomas a la vez (T2b: Ajustes › Idioma con en y
        /// pt-BR completos); <paramref name="missing"/>, si no es null, recibe «clave (idioma)» de cada celda rellena.
        /// </summary>
        public static LocTableTexts Completed(string[] locales, out int translated, out int filled, List<string>? missing)
        {
            string strings = LocTables.ReadProjectFile(LocTables.StringsAssetPath, LocCatalog.StringsFile);
            string facts = LocTables.ReadProjectFile(LocTables.FactsAssetPath, LocCatalog.FactsFile);
            List<List<string>> records = Parse(strings);
            List<string> header = records[0];
            int source = header.IndexOf(LocLocale.SourceLocale);
            Assert.That(source, Is.GreaterThanOrEqualTo(0));
            translated = 0;
            filled = 0;
            foreach (string locale in locales)
            {
                int target = header.IndexOf(locale);
                Assert.That(target, Is.GreaterThanOrEqualTo(0), locale + " es una columna de strings.csv");
                for (int r = 1; r < records.Count; r++)
                {
                    if (records[r][target].Length > 0)
                    {
                        translated++;
                    }
                    else
                    {
                        records[r][target] = records[r][source];
                        filled++;
                        missing?.Add(records[r][0] + " (" + locale + ")");
                    }
                }
            }

            return new LocTableTexts(Write(records), facts);
        }

        // RFC 4180 mínimo (comillas dobladas, comas y saltos de línea dentro de comillas), como las tablas de 08 §4.7.
        private static List<List<string>> Parse(string text)
        {
            var records = new List<List<string>>();
            var fields = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    fields.Add(cell.ToString());
                    cell.Clear();
                }
                else if (c == '\n')
                {
                    fields.Add(cell.ToString());
                    cell.Clear();
                    records.Add(fields);
                    fields = new List<string>();
                }
                else
                {
                    cell.Append(c);
                }
            }

            Assert.That(quoted, Is.False, "strings.csv sin comillas abiertas");
            Assert.That(fields.Count == 0 && cell.Length == 0, Is.True, "strings.csv termina en salto de línea");
            return records;
        }

        private static string Write(List<List<string>> records)
        {
            var sb = new StringBuilder();
            foreach (List<string> record in records)
            {
                for (int f = 0; f < record.Count; f++)
                {
                    string v = record[f];
                    sb.Append(f == 0 ? string.Empty : ",");
                    if (v.IndexOf(',') >= 0 || v.IndexOf('"') >= 0 || v.IndexOf('\n') >= 0)
                    {
                        sb.Append('"').Append(v.Replace("\"", "\"\"")).Append('"');
                    }
                    else
                    {
                        sb.Append(v);
                    }
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// El recorrido de <see cref="PseudoLocWalk.Run"/> con unas tablas propias (<see cref="LaunchOptions.LocTablesOverride"/>, que
    /// <see cref="PseudoLocWalk.Run"/> limpia al empezar) y la UI en <paramref name="locale"/>; las mismas pantallas y en el mismo orden.
    /// </summary>
    internal static class TranslatedWalk
    {
        public static IEnumerator Run(string locale, float scale, LocTableTexts tables, LocInventory inventory)
        {
            FunnelSetup.Hooks();
            LaunchOptions.LocTablesOverride = tables;
            LaunchOptions.LanguageOverride = locale;
            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            Assert.That(inventory.Scale, Is.EqualTo(scale).Within(1e-5f));

            Profile p4 = Wallet.NewProfile(FunnelSetup.Clock);
            p4.Progress.NextLevel = 4;
            p4.Progress.MaxLevel = 3;
            p4.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p4));
            Assert.That(Loc.Locale, Is.EqualTo(locale), "la UI en el idioma medido");
            yield return Take(inventory, "inicio_l4");

            LaunchOptions.LevelOffsetOverride = 30;
            Profile p31 = FunnelSetup.Profile(2380, 3);
            p31.Progress.NextLevel = 31;
            p31.Progress.MaxLevel = 30;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p31));
            yield return Take(inventory, "inicio_l31");
            LaunchOptions.LevelOffsetOverride = null;

            Profile p40 = Wallet.NewProfile(FunnelSetup.Clock);
            p40.Progress.NextLevel = 40;
            p40.Progress.MaxLevel = 39;
            p40.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p40));
            yield return Take(inventory, "inicio_l40");

            foreach (int level in new[] { 40, 100, 183 })
            {
                yield return CardSetup.Load(level);
                GameBootstrap card = GameBootstrap.Current!;
                Assert.That(card.View!.LevelCard.Visible, Is.True, "tarjeta del " + level);
                card.View.CardFontScale = scale;
                PlayModeHelpers.Step(card, 50);
                yield return Take(inventory, "tarjeta_l" + level.ToString(CultureInfo.InvariantCulture));
            }

            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.WorldBannerView.Visible, Is.True);
            boot.View.BannerFontScale = scale;
            PlayModeHelpers.TickExact(boot, 1100);
            yield return Take(inventory, "banner_l41");

            LaunchOptions.LevelCardsOverride = false;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            GameBootstrap hud = boot;
            yield return PlayModeHelpers.WaitFor(() => hud.Flow!.Phase == FlowPhase.Playing && hud.View!.IsViewIdle, "el 100 en juego");
            yield return SnapshotKit.Wait(0.5f);
            yield return Take(inventory, "bandeja_l100");

            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            boot = GameBootstrap.Current!;
            yield return PseudoLocWalk.WinRealTime(boot);
            yield return SnapshotKit.Wait(1.2f);
            yield return Take(inventory, "victoria_l11");

            yield return PseudoLocWalk.LoadFunnel(31, 1240);
            boot = GameBootstrap.Current!;
            yield return PseudoLocWalk.StuckRealTime(boot);
            yield return Take(inventory, "aguas_estancadas_l31_a");
            boot.PressGiveUp();
            yield return SnapshotKit.Wait(0.3f);
            yield return Take(inventory, "seguro_l31");
            boot.ConfirmGiveUp();
            yield return SnapshotKit.Wait(0.3f);
            yield return Take(inventory, "nivel_fallido_l31");

            yield return PseudoLocWalk.LoadFunnel(31, 140);
            boot = GameBootstrap.Current!;
            yield return PseudoLocWalk.StuckRealTime(boot);
            yield return Take(inventory, "aguas_estancadas_l31_b");

            LaunchOptions.LevelOffsetOverride = null;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(310));
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore(6, 5));
            boot = GameBootstrap.Current!;
            yield return PseudoLocWalk.StuckRealTime(boot);
            yield return Take(inventory, "aguas_estancadas_l6_c");
            Assert.That(Loc.Locale, Is.EqualTo(locale), "la UI siguió en el idioma medido");
        }

        private static IEnumerator Take(LocInventory inventory, string name)
        {
            yield return SnapshotKit.Wait(0.3f);
            inventory.Scan("t2_" + name);
        }
    }
}
