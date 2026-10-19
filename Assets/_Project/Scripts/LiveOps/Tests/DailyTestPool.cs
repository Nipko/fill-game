#nullable enable
using System.Globalization;
using System.Text;
using AF.Levels;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Pool de prueba del Reto del río con las 3 bandas × 730 días, construido en código y leído con
    /// <see cref="DailyPoolReader"/> (modo estricto). Mismo tablero que <c>AF.Tests.EditMode.DailyPoolTestPacks</c> (el nivel 1
    /// de 02 §13), copiado porque ese ensamblado no es visible desde aquí. No es el pool real (R5).
    /// </summary>
    internal static class DailyTestPool
    {
        private static DailyPool? s_pool;

        /// <summary>El pool completo (se lee una vez).</summary>
        public static DailyPool Full => s_pool ??= DailyPoolReader.Read(Json());

        private static string Json()
        {
            var sb = new StringBuilder(DailyPool.MaxBand * DailyPool.Days * 520 + 400);
            sb.Append("{\"pack_schema\":2,\"pack_id\":\"daily_pool_v1\",\"kind\":\"daily\",");
            sb.Append("\"generator\":{\"tool\":\"level-generator\",\"version\":\"0.1.0\"},\"pool\":[\"acai\",\"aguaje\"],");
            sb.Append("\"k_min\":2,\"k_max\":11,\"max_risk_pairs\":0,\"cap_default\":4,\"empties_default\":1,\"levels\":[");
            bool first = true;
            for (int band = DailyPool.MinBand; band <= DailyPool.MaxBand; band++)
            {
                for (int day = 0; day < DailyPool.Days; day++)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;
                    int world = 1 + day % 9;
                    string water = world == 3 ? "black" : world == 2 ? "clear" : "white";
                    sb.Append("{\"schema\":\"af-level/1\",\"id\":\"").Append(DailyPool.IdOf(band, day))
                        .Append("\",\"slot\":{\"band\":").Append(Num(band)).Append(",\"day\":").Append(Num(day))
                        .Append("},\"seed\":\"").Append(DailyPool.SeedOf(band, day)).Append("\",")
                        .Append("\"gen\":{\"tool\":\"level-generator\",\"version\":\"0.1.0\",\"source\":\"walk\"},")
                        .Append("\"world\":").Append(Num(world)).Append(",\"water_base\":\"").Append(water).Append("\",")
                        .Append("\"palette\":[\"acai\",\"aguaje\"],\"units_per_color\":4,\"caps\":[4,4,4],")
                        .Append("\"cuias\":[[8,8,8,4],[4,4,4,8],[]],\"attrs\":[null,null,null],\"murky\":false,\"hidden\":[],")
                        .Append("\"risk_pairs\":0,\"intro_mechanic\":null,\"skippable\":true,\"hint_supported\":true,\"relief\":[]}");
                }
            }

            return sb.Append("]}").ToString();
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
