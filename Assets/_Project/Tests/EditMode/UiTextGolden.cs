#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AF.Ads;
using AF.Levels;
using AF.UI;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Golden de los textos de <see cref="UiText"/> en es-419 (D-018, tarea L1: «ningún texto visible cambia»): una línea
    /// <c>caso\ttexto</c> por cada constante sin marcadores y por cada texto que arman las pantallas, en orden ordinal, con los
    /// saltos de línea escritos «\n» y null como «&lt;null&gt;». <c>Golden/uitext_es419.txt</c> se generó con el código de antes
    /// de L1 (2c75897), con las mismas líneas que hacían a mano StuckModal (<c>Format</c> + <c>Replace</c>), AdOverlay
    /// (« · » pegado) y WorldBannerView (la línea del lugar y cada corte de BestSplit); aquí se generan con las plantillas.
    /// Los plurales que cambian a propósito («Te faltan 1 semillas», «en unos 1 días», «1 semillas») quedan fuera: los mira
    /// <see cref="UiTextTrapsTests"/>.
    /// </summary>
    internal static class UiTextGolden
    {
        /// <summary>Nombre del archivo en <c>Tests/EditMode/Golden/</c>.</summary>
        public const string FileName = "uitext_es419.txt";

        /// <summary>Precios de tienda de ejemplo (el de la tienda va tal cual).</summary>
        public static readonly string[] Prices = { string.Empty, "COP 3.900", "US$ 1,99" };

        /// <summary>Claves de la tarjeta del miquito, más una que no existe.</summary>
        public static readonly string[] FaunaKeys =
        {
            "fauna_aotus_trivirgatus_dato", "fauna_ateles_chamek_dato", "fauna_cacajao_calvus_ucayalii_dato", "fauna_lagothrix_lagothricha_dato",
            "fauna_mico_argentatus_dato", "fauna_otro_dato", "fauna_pithecia_pithecia_dato", "fauna_plecturocebus_discolor_dato",
            "fauna_saguinus_bicolor_dato", "fauna_saguinus_imperator_dato", "fauna_saimiri_collinsi_dato", "fauna_saimiri_sciureus_albigena_dato",
        };

        /// <summary>Los textos de ahora, por caso.</summary>
        public static SortedDictionary<string, string?> Build()
        {
            var lines = new SortedDictionary<string, string?>(StringComparer.Ordinal);
            foreach (FieldInfo f in typeof(UiText).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType == typeof(string) && f.GetValue(null) is string v && v.IndexOf('{') < 0)
                {
                    lines.Add("UiText." + f.Name, v);
                }
            }

            foreach (PropertyInfo p in typeof(UiText).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (p.PropertyType == typeof(string) && p.GetValue(null) is string v && v.IndexOf('{') < 0)
                {
                    lines.Add("UiText." + p.Name, v);
                }
            }

            foreach (int n in new[] { 0, 2, 14, 21 })
            {
                lines.Add("ContentEnd(" + n + ")", UiText.ContentEnd(n));
            }

            foreach (string l in new[] { LevelDefinition.LabelNormal, LevelDefinition.LabelHard, LevelDefinition.LabelVeryHard })
            {
                lines.Add("DifficultyChip(" + l + ")", UiText.DifficultyChip(l));
                lines.Add("DifficultySubtitle(" + l + ",on)", UiText.DifficultySubtitle(l, true));
                lines.Add("DifficultySubtitle(" + l + ",off)", UiText.DifficultySubtitle(l, false));
            }

            foreach (int n in new[] { 20, 60, 1000 })
            {
                lines.Add("DoubleHarvestGain(" + n + ")", UiText.DoubleHarvestGain(n));
                lines.Add("RewardLine(" + n + ")", UiText.RewardLine(n));
            }

            foreach (string k in FaunaKeys)
            {
                lines.Add("FaunaFact(" + k + ")", UiText.FaunaFact(k));
            }

            foreach (int n in new[] { 0, 999, 1000, 36000, 1234567 })
            {
                lines.Add("Grouped(" + n + ")", UiText.Grouped(n));
            }

            foreach (int n in new[] { 1, 41, 1000 })
            {
                lines.Add("Level(" + n + ")", UiText.Level(n));
            }

            lines.Add("LevelAttempt(41,2)", UiText.LevelAttempt(41, 2));
            lines.Add("LevelAttempt(7,0)", UiText.LevelAttempt(7, 0));
            foreach (int n in new[] { 0, 2, 21, 460, 1000, 1234567 })
            {
                lines.Add("MissingSeeds(" + n + ")", UiText.MissingSeeds(n));
                lines.Add("RevealMissing(" + n + ")", UiText.RevealMissing(n));
            }

            foreach (int n in new[] { 1, 387, 1000 })
            {
                lines.Add("PlayLevel(" + n + ")", UiText.PlayLevel(n));
            }

            foreach (int n in new[] { 3, 4 })
            {
                lines.Add("Ribbon(" + n + ")", UiText.Ribbon(n));
            }

            foreach (AdPlacement placement in (AdPlacement[])Enum.GetValues(typeof(AdPlacement)))
            {
                foreach (bool rewarded in new[] { false, true })
                {
                    lines.Add("SimulatedAdLabel(" + placement.ToId() + "," + (rewarded ? "rewarded" : "interstitial") + ")",
                        UiText.SimulatedAdLabel(placement.ToId(), rewarded));
                }
            }

            foreach (string price in Prices)
            {
                lines.Add("StuckBalsa(" + price + ")", UiText.StuckBalsaLine(price));
                foreach (int seeds in new[] { 1000, 2500 })
                {
                    lines.Add("StuckPack(" + seeds + "," + price + ")", UiText.StuckPackLine(seeds, price));
                }
            }

            foreach (int refillPrice in new[] { 100, 150, 1000 })
            {
                lines.Add("StuckRefillAd(3," + refillPrice + ")", UiText.StuckRefillVideo(3));
                lines.Add("StuckRefillSeeds(3," + refillPrice + ",two)", UiText.StuckRefillSeeds(3, refillPrice, true));
                lines.Add("StuckRefillSeeds(3," + refillPrice + ",one)", UiText.StuckRefillSeeds(3, refillPrice, false));
            }

            foreach (string m in new[] { "chica", "alta", "hoja", "matapi", "liana", "murky", "remolino", "barro", "lluvia", "brote", "pirana", "liana_n", "hoja_atada", "otro" })
            {
                lines.Add("Tutorial(" + m + ")", UiText.Tutorial(m));
            }

            for (int w = 1; w <= WorldTable.WorldCount; w++)
            {
                lines.Add("WorldBanner(" + w + ")", UiText.WorldBanner(w));
                lines.Add("WorldCountry(" + w + ")", UiText.WorldCountry(w));
                lines.Add("WorldHeading(" + w + ")", UiText.WorldHeading(w));
                lines.Add("WorldName(" + w + ")", UiText.WorldName(w));
                lines.Add("WorldPlace(" + w + ")", UiText.WorldPlace(w));
                lines.Add("WorldPlaceNamed(" + w + ")", UiText.WorldPlaceNamed(w));
                lines.Add("WorldRiver(" + w + ")", UiText.WorldRiver(w));
                lines.Add("WorldTitle(" + w + ")", UiText.WorldTitle(w));
                foreach (bool named in new[] { false, true })
                {
                    string kind = named ? "named" : "place";
                    lines.Add("WorldPlaceLine(" + w + "," + kind + ")", UiText.WorldPlaceLine(w, named, 0));
                    for (int k = 0; k < UiText.WorldPlaceSeparators(named); k++)
                    {
                        lines.Add("WorldPlaceSplit(" + w + "," + kind + "," + k + ")", UiText.WorldPlaceLine(w, named, 1 << k));
                    }
                }
            }

            var buffer = new char[32];
            foreach (long s in new long[] { 0, 59, 1500, 3600, 45296 })
            {
                lines.Add("WriteClock(" + s + ")", new string(buffer, 0, UiText.WriteClock(buffer, 0, s)));
            }

            foreach (int v in new[] { -5, 0, 7, 1240, 1234567 })
            {
                foreach (bool grouped in new[] { false, true })
                {
                    lines.Add("WriteNumber(" + v + "," + (grouped ? "grouped" : "plain") + ")", new string(buffer, 0, UiText.WriteNumber(buffer, 0, v, grouped)));
                }
            }

            return lines;
        }

        /// <summary>El archivo: «caso\ttexto\n» por línea, en orden ordinal.</summary>
        public static string Render(SortedDictionary<string, string?> lines)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string?> line in lines)
            {
                sb.Append(line.Key).Append('\t').Append(Escape(line.Value)).Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>Lee el archivo (líneas «caso\ttexto»; LF o CRLF) a un diccionario con los textos ya sin escapar.</summary>
        public static SortedDictionary<string, string?> Parse(string text)
        {
            var lines = new SortedDictionary<string, string?>(StringComparer.Ordinal);
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.Length == 0)
                {
                    continue;
                }

                int tab = raw.IndexOf('\t');
                string value = raw.Substring(tab + 1);
                lines.Add(raw.Substring(0, tab), value == "<null>" ? null : Unescape(value));
            }

            return lines;
        }

        private static string Escape(string? s)
        {
            if (s == null)
            {
                return "<null>";
            }

            return s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static string Unescape(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\' || i + 1 >= s.Length)
                {
                    sb.Append(s[i]);
                    continue;
                }

                char c = s[++i];
                sb.Append(c == 'n' ? '\n' : c == 'r' ? '\r' : c == 't' ? '\t' : c);
            }

            return sb.ToString();
        }
    }
}
