#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Levels;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Pools de prueba del Reto del río (<c>kind</c> = <c>daily</c>; esquema <c>schemas/af-level-1.schema.json</c>, rama
    /// <c>daily</c>; nota <c>docs/dev/reto-del-rio.md</c> §13) construidos en código para <see cref="DailyPoolReaderTests"/>. No
    /// son el pool real (R5): el tablero de cada slot es el nivel 1 de 02 §13 (dos frutas y una cuia vacía).
    /// </summary>
    internal static class DailyPoolTestPacks
    {
        /// <summary><c>pack_id</c> de los pools de prueba.</summary>
        public const string PackId = "daily_pool_v1";

        /// <summary>Mundo que lleva el tablero del día <paramref name="day"/> (la rotación [P] de la nota §13: 1 + día mod 9).</summary>
        public static int WorldOf(int day)
        {
            return 1 + day % 9;
        }

        /// <summary>Agua del mundo (glosario §8): negra en el 3, clara en el 2 y blanca en el resto.</summary>
        public static string WaterOf(int world)
        {
            return world == 3 ? "black" : world == 2 ? "clear" : "white";
        }

        /// <summary>
        /// Un tablero válido del slot (<paramref name="band"/>, <paramref name="day"/>) con su id y su semilla; <paramref name="extra"/>
        /// añade campos al final (p. ej. <c>, "difficulty": {...}</c>).
        /// </summary>
        public static string Board(int band, int day, string extra = "")
        {
            int world = WorldOf(day);
            return "{\"schema\":\"af-level/1\",\"id\":\"" + DailyPool.IdOf(band, day) + "\",\"slot\":{\"band\":" + Num(band)
                + ",\"day\":" + Num(day) + "},\"seed\":\"" + DailyPool.SeedOf(band, day) + "\","
                + "\"gen\":{\"tool\":\"level-generator\",\"version\":\"0.1.0\",\"source\":\"walk\"},"
                + "\"world\":" + Num(world) + ",\"water_base\":\"" + WaterOf(world) + "\","
                + "\"palette\":[\"acai\",\"aguaje\"],\"units_per_color\":4,\"caps\":[4,4,4],"
                + "\"cuias\":[[8,8,8,4],[4,4,4,8],[]],\"attrs\":[null,null,null],\"murky\":false,\"hidden\":[],"
                + "\"risk_pairs\":0,\"intro_mechanic\":null,\"skippable\":true,\"hint_supported\":true,\"relief\":[]" + extra + "}";
        }

        /// <summary>Los 729 tableros (DailyPool.Days) de cada banda de <paramref name="bands"/>, en orden de banda y día.</summary>
        public static List<string> Boards(params int[] bands)
        {
            var boards = new List<string>(bands.Length * DailyPool.Days);
            for (int i = 0; i < bands.Length; i++)
            {
                for (int day = 0; day < DailyPool.Days; day++)
                {
                    boards.Add(Board(bands[i], day));
                }
            }

            return boards;
        }

        /// <summary>
        /// El pool con esos tableros; <paramref name="header"/> sustituye a los campos de cabecera que preceden a <c>levels</c>
        /// (por defecto <c>pack_schema</c> 2, <c>pack_id</c> y <c>kind</c> = <c>daily</c>).
        /// </summary>
        public static string Pool(IReadOnlyList<string> boards, string? header = null)
        {
            var sb = new StringBuilder(boards.Count * 480 + 400);
            sb.Append('{');
            sb.Append(header ?? Header());
            sb.Append(",\"generator\":{\"tool\":\"level-generator\",\"version\":\"0.1.0\"},\"pool\":[\"acai\",\"aguaje\"],");
            sb.Append("\"k_min\":2,\"k_max\":11,\"max_risk_pairs\":0,\"cap_default\":4,\"empties_default\":1,\"levels\":[");
            for (int i = 0; i < boards.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(boards[i]);
            }

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>Cabecera por defecto con el <c>pack_schema</c> y el <c>kind</c> indicados.</summary>
        public static string Header(int packSchema = 2, string kind = "daily")
        {
            return "\"pack_schema\":" + Num(packSchema) + ",\"pack_id\":\"" + PackId + "\",\"kind\":\"" + kind + "\"";
        }

        /// <summary>Pool de un solo tablero (para los rechazos que salen antes de comprobar las bandas).</summary>
        public static string Single(string board, string? header = null)
        {
            return Pool(new[] { board }, header);
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
