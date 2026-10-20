#nullable enable
using System.Text;
using AF.GameFlow;
using AF.LiveOps;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sondas del Reto del río (tarea R10; nota <c>docs/dev/reto-del-rio.md</c> §12; 08 §8.2). Hasta que exista <c>AF.Analytics</c>,
    /// los eventos van al log de los builds de desarrollo (<see cref="Enabled"/>) como <c>AF_STATE</c>, con el formato de
    /// <see cref="DailyProbeFormat"/> (AF.LiveOps, probado allí) y el canal de siempre (<see cref="Write"/>):
    /// <list type="bullet">
    /// <item><c>daily_challenge</c> (<c>af_daily_challenge</c>): <c>result = win</c> al reclamar (<see cref="DailyWin"/>; nada con
    /// <see cref="ClaimOutcome.NoReward"/>) y <c>result = fail</c> desde <see cref="DailyChallengeService.DayEnded"/>, una vez por
    /// día (<see cref="DailyDayEnd"/>; R-Q9 con R-Q15). Nunca con <see cref="DailyResult.None"/>;</item>
    /// <item><c>level_start</c>: <c>level_name</c> = <c>d{b}_n{nnnn}</c>, <c>variant</c> = <c>base</c> y <c>band</c>, al empezar o
    /// reanudar una partida del Reto (<see cref="DailyLevelStart"/>);</item>
    /// <item><c>earn_virtual_currency</c> con <c>source = daily_challenge</c>: las semillas pagadas al reclamar o al reconciliar
    /// (<see cref="DailyEarn"/>). La Pororoca de la racha no tiene evento: 08 §8.2 solo define <c>earn_virtual_currency</c> para
    /// semillas y no hay uno de stock de comodines (no se inventa);</item>
    /// <item><c>daily_card</c>: el estado de la tarjeta del inicio cada vez que se recalcula, con la sustitución pedida
    /// (<c>override</c>) y si se ignoró (<c>override_ignored</c>), para jugar con <c>adb</c> (<see cref="DailyCard"/>).</item>
    /// </list>
    /// Estáticas: las llaman <c>GameBootstrap.Daily.cs</c> y <c>DailyChallengeScreen.cs</c>, que no comparten instancia de la sonda.
    /// Solo en el hilo principal (un único <see cref="DailyProbeFormat"/>). Solo claves e identificadores, nunca texto visible.
    /// Cada una devuelve la línea escrita, o null si no escribió nada.
    /// </summary>
    public sealed partial class DevProbe
    {
        private static readonly DailyProbeFormat s_daily = new DailyProbeFormat();

        /// <summary><c>AF_STATE daily_challenge</c> con <c>result = win</c> tras el cobro (<paramref name="tMs"/> null si no se midió).</summary>
        public static string? DailyWin(in DailyClaimReport claim, long? tMs)
        {
            return Enabled ? WriteDaily(s_daily.Win(NowMs(), claim, tMs)) : null;
        }

        /// <summary><c>AF_STATE daily_challenge</c> con <c>result = fail</c> de un día cerrado sin victoria (<see cref="DailyChallengeService.DayEnded"/>).</summary>
        public static string? DailyDayEnd(in DailyPlayEnd end)
        {
            return Enabled ? WriteDaily(s_daily.DayEnd(NowMs(), end)) : null;
        }

        /// <summary><c>AF_STATE level_start</c> de la partida del Reto (tablero <c>d{b}_n{nnnn}</c> y banda congelada).</summary>
        public static string? DailyLevelStart(string boardId, int band)
        {
            return Enabled ? WriteDaily(s_daily.LevelStart(NowMs(), boardId, band)) : null;
        }

        /// <summary><c>AF_STATE earn_virtual_currency</c> del premio del Reto; nada si <paramref name="seeds"/> ≤ 0.</summary>
        public static string? DailyEarn(int seeds)
        {
            return Enabled ? WriteDaily(s_daily.EarnSeeds(NowMs(), seeds)) : null;
        }

        /// <summary>
        /// <c>AF_STATE earn_virtual_currency</c> con la fuente <paramref name="source"/> (<see cref="DailyProbeFormat.DoubleHarvestSourceId"/>
        /// para la Cosecha doble del Reto); nada si <paramref name="seeds"/> ≤ 0.
        /// </summary>
        public static string? DailyEarn(int seeds, string source)
        {
            return Enabled ? WriteDaily(s_daily.EarnSeeds(NowMs(), seeds, source)) : null;
        }

        /// <summary><c>AF_STATE daily_card</c>: el estado recalculado de la tarjeta del inicio.</summary>
        public static string? DailyCard(DailyCardState state, string dateKey, DailySlot? slot, string? boardId, int streakShown)
        {
            return Enabled ? WriteDaily(s_daily.Card(NowMs(), state, dateKey, slot, boardId, streakShown)) : null;
        }

        /// <summary>
        /// Enganche E24 (lo aplica el integrador en <c>DevProbe.cs</c>): tras cada <c>"level":flow.LevelNumber</c> de las líneas de
        /// la partida, añade <c>,"daily":"d{b}_n{nnnn}"</c> si <paramref name="flow"/> es del Reto, para que en el log se distinga el
        /// nivel efectivo (R8) de un nivel de la campaña. Nada en la campaña.
        /// </summary>
        internal static void AppendDailyBoard(StringBuilder sb, LevelFlow flow)
        {
            string? board = flow.DailyBoardId;
            if (board != null)
            {
                sb.Append(",\"daily\":\"").Append(board).Append('"');
            }
        }

        private static long NowMs()
        {
            return (long)(Time.realtimeSinceStartupAsDouble * 1000.0);
        }

        private static string? WriteDaily(string? line)
        {
            if (line != null)
            {
                Write(line);
            }

            return line;
        }
    }
}
