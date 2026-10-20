#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Economy
{
    /// <summary>
    /// Formato de las líneas <c>AF_STATE</c> de la Crecida, la Hoja escudo y la Canasta (B1a, A7; nota
    /// <c>docs/dev/crecida-canasta.md</c> §5 y E33). Son <b>sondas de desarrollo</b> (logcat), no eventos de 08 §8.2: los nombres
    /// <c>streak_milestone</c>, <c>streak_shield_used</c> y <c>basket_open</c> reflejan <c>af_streak_milestone</c>,
    /// <c>af_streak_shield_used</c> y <c>af_basket_open</c> sin el prefijo, como <c>daily_challenge</c> en el Reto; <c>streak_fail</c>,
    /// <c>streak_state</c> y <c>free_double_harvest</c> son solo de la sonda [P]. Sin motor, para probarlo en EditMode:
    /// <c>DevProbe.Streak.cs</c> (AF.UI) solo le pasa la hora de la sonda (<c>t</c>) y escribe las líneas con <c>DevProbe.Write</c>.
    /// Mismo esquema que el resto de <c>DevProbe</c>: <c>AF_STATE {"event":…,"t":…,…}</c>, JSON de una línea, campos en orden fijo,
    /// solo claves, números e identificadores (nunca texto visible).
    /// <list type="bullet">
    /// <item><c>streak_state</c>: <c>cause</c> (<c>start</c>, <c>day</c>, <c>win</c> o <c>hard_fail</c>), <c>level</c> (el nivel
    /// consultado con <see cref="StreakService.Peek"/>), <c>wins</c>, <c>river_full</c>, <c>shield</c>, <c>hits</c> (los hitos
    /// pagados hoy, en orden creciente), <c>seeds_today</c>, <c>date</c> (null sin día), <c>enabled</c>, <c>active</c> y
    /// <c>free_double_next</c>;</item>
    /// <item><c>streak_fail</c>: <c>level</c>, <c>wins_before</c>, <c>river_full_before</c>, <c>shield_used</c>,
    /// <c>streak_broken</c> y <c>river_ended</c>;</item>
    /// <item><c>streak_shield_used</c>: <c>level</c> y <c>streak_at_use</c> (la barra que la hoja salvó);</item>
    /// <item><c>free_double_harvest</c>: <c>level</c> y <c>seeds</c> (las que entraron por la Cosecha doble del río crecido);</item>
    /// <item><c>streak_milestone</c>: <c>level</c>, <c>milestone</c>, <c>reward_type</c> y <c>reward_value</c> (cadena: las
    /// semillas, el <c>contents_json</c> de la Canasta o <c>1</c>), solo con el hito pagado;</item>
    /// <item><c>basket_open</c>: <c>level</c> y <c>contents_json</c> (cadena) de la Canasta de nivel.</item>
    /// </list>
    /// Un resultado de <see cref="StreakService"/> sale siempre en el orden de <see cref="AppendResult"/>. Un único hilo (el
    /// principal): reutiliza su <see cref="StringBuilder"/>.
    /// </summary>
    public sealed class StreakProbeFormat
    {
        /// <summary><c>cause</c> de <c>streak_state</c> al crear el servicio en el arranque.</summary>
        public const string CauseStart = "start";

        /// <summary><c>cause</c> de <c>streak_state</c> al cambiar el día local (la hoja vuelve y los hitos de hoy se vacían).</summary>
        public const string CauseDay = "day";

        /// <summary><c>cause</c> de <c>streak_state</c> tras una victoria que la Crecida contó.</summary>
        public const string CauseWin = "win";

        /// <summary><c>cause</c> de <c>streak_state</c> tras un fallo duro que la Crecida contó.</summary>
        public const string CauseHardFail = "hard_fail";

        private readonly StringBuilder _sb = new StringBuilder(256);

        /// <summary>
        /// Línea <c>streak_state</c> con el estado <paramref name="state"/> consultado para el nivel <paramref name="level"/>.
        /// </summary>
        public string State(long t, string cause, int level, in StreakSnapshot state)
        {
            StringBuilder sb = Begin("streak_state", t);
            sb.Append(",\"cause\":");
            AppendString(sb, cause);
            AppendInt(sb, "level", level);
            AppendInt(sb, "wins", state.Wins);
            AppendBool(sb, "river_full", state.RiverFull);
            AppendBool(sb, "shield", state.ShieldAvailable);
            sb.Append(",\"hits\":[");
            bool first = true;
            for (int m = 0; m < StreakRecord.Milestones.Count; m++)
            {
                int hit = StreakRecord.Milestones[m];
                if (state.HitPaid(hit))
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    sb.Append(hit.ToString(CultureInfo.InvariantCulture));
                    first = false;
                }
            }

            sb.Append(']');
            AppendInt(sb, "seeds_today", state.SeedsToday);
            sb.Append(",\"date\":");
            AppendString(sb, state.DateLocal);
            AppendBool(sb, "enabled", state.Enabled);
            AppendBool(sb, "active", state.Active);
            AppendBool(sb, "free_double_next", state.FreeDoubleHarvestNext);
            return End(sb);
        }

        /// <summary>Línea <c>streak_fail</c> de un fallo duro que la Crecida contó; null si no lo hubo.</summary>
        public string? Fail(long t, StreakServiceResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Has(StreakServiceEvents.StreakFail))
            {
                return null;
            }

            StreakOutcome o = result.Streak;
            StringBuilder sb = Begin("streak_fail", t);
            AppendInt(sb, "level", o.Level);
            AppendInt(sb, "wins_before", o.WinsBefore);
            AppendBool(sb, "river_full_before", o.RiverFullBefore);
            AppendBool(sb, "shield_used", o.ShieldUsed);
            AppendBool(sb, "streak_broken", o.StreakBroken);
            AppendBool(sb, "river_ended", o.RiverEnded);
            return End(sb);
        }

        /// <summary>Línea <c>streak_shield_used</c> si la Hoja escudo cubrió el fallo duro; null si no.</summary>
        public string? ShieldUsed(long t, StreakServiceResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Has(StreakServiceEvents.ShieldUsed))
            {
                return null;
            }

            StreakOutcome o = result.Streak;
            StringBuilder sb = Begin("streak_shield_used", t);
            AppendInt(sb, "level", o.Level);
            AppendInt(sb, "streak_at_use", o.WinsBefore);
            return End(sb);
        }

        /// <summary>Línea <c>free_double_harvest</c> si se aplicó la Cosecha doble del río crecido; null si no.</summary>
        public string? FreeDoubleHarvest(long t, StreakServiceResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Has(StreakServiceEvents.FreeDoubleHarvest))
            {
                return null;
            }

            StringBuilder sb = Begin("free_double_harvest", t);
            AppendInt(sb, "level", result.Streak.Level);
            AppendInt(sb, "seeds", result.DoubleHarvestSeeds);
            return End(sb);
        }

        /// <summary>Línea <c>streak_milestone</c> del hito pagado; null sin hito pagado (también el que salta la regla de una vez al día).</summary>
        public string? Milestone(long t, StreakServiceResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            StreakOutcome o = result.Streak;
            string? type = o.MilestoneRewardType;
            if (!result.Has(StreakServiceEvents.MilestonePaid) || type == null)
            {
                return null;
            }

            StringBuilder sb = Begin("streak_milestone", t);
            AppendInt(sb, "level", o.Level);
            AppendInt(sb, "milestone", o.Milestone);
            sb.Append(",\"reward_type\":");
            AppendString(sb, type);
            sb.Append(",\"reward_value\":");
            AppendString(sb, o.MilestoneRewardValue);
            return End(sb);
        }

        /// <summary>Línea <c>basket_open</c> de la Canasta de nivel pagada (victoria o Salto del Boto); null si no hubo.</summary>
        public string? Basket(long t, StreakServiceResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            BasketContents basket = result.LevelBasket;
            if (!result.Has(StreakServiceEvents.BasketOpened) || basket.IsNone)
            {
                return null;
            }

            StringBuilder sb = Begin("basket_open", t);
            AppendInt(sb, "level", basket.Level);
            sb.Append(",\"contents_json\":");
            AppendString(sb, basket.ContentsJson());
            return End(sb);
        }

        /// <summary>
        /// Añade a <paramref name="into"/> las líneas de <paramref name="result"/>, siempre en este orden: <c>streak_fail</c>,
        /// <c>streak_shield_used</c>, <c>free_double_harvest</c>, <c>streak_milestone</c>, <c>basket_open</c> y, si el estado
        /// cambió (<see cref="StreakServiceEvents.StateChanged"/>) y llega <paramref name="after"/>, <c>streak_state</c> con
        /// <c>cause</c> = <c>win</c> o <c>hard_fail</c> para el nivel <paramref name="level"/>. Devuelve cuántas añadió.
        /// </summary>
        public int AppendResult(List<string> into, long t, StreakServiceResult result, StreakSnapshot? after, int level)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            int before = into.Count;
            AddIf(into, Fail(t, result));
            AddIf(into, ShieldUsed(t, result));
            AddIf(into, FreeDoubleHarvest(t, result));
            AddIf(into, Milestone(t, result));
            AddIf(into, Basket(t, result));
            if (after.HasValue && result.Has(StreakServiceEvents.StateChanged))
            {
                string cause = result.Streak.Event == StreakEvent.HardFail ? CauseHardFail : CauseWin;
                StreakSnapshot state = after.Value;
                into.Add(State(t, cause, level, in state));
            }

            return into.Count - before;
        }

        private static void AddIf(List<string> into, string? line)
        {
            if (line != null)
            {
                into.Add(line);
            }
        }

        private StringBuilder Begin(string eventName, long t)
        {
            StringBuilder sb = _sb;
            sb.Clear();
            sb.Append("AF_STATE {\"event\":\"").Append(eventName).Append('"');
            sb.Append(",\"t\":").Append(t.ToString(CultureInfo.InvariantCulture));
            return sb;
        }

        private static string End(StringBuilder sb)
        {
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendInt(StringBuilder sb, string key, int value)
        {
            sb.Append(",\"").Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendBool(StringBuilder sb, string key, bool value)
        {
            sb.Append(",\"").Append(key).Append("\":").Append(value ? "true" : "false");
        }

        // Una cadena JSON (o null), con las comillas y las barras escapadas y sin caracteres de control (contents_json lleva comillas).
        private static void AppendString(StringBuilder sb, string? value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\')
                {
                    sb.Append('\\').Append(c);
                }
                else if (c >= ' ')
                {
                    sb.Append(c);
                }
            }

            sb.Append('"');
        }
    }
}
