#nullable enable
using System;
using System.Collections.Generic;

namespace AF.LiveOps
{
    /// <summary>
    /// La cola de victorias de la Liga (liga.md §4; 06 §6, fila «Offline»: «puntos encolados y enviados al reconectar»), pura,
    /// sobre un <see cref="LeagueRecord"/>:
    /// <list type="bullet">
    /// <item><see cref="Roll"/>: al cambiar la semana (lunes 12:00 UTC), <c>counted</c> se vacía y lo que quedó en la cola de la
    /// semana anterior se descarta (el servidor lo rechazaría con <c>outside_week</c>);</item>
    /// <item><see cref="Enqueue"/>: idempotente por <c>win_id</c>: un nivel suma una vez por semana y el Reto una vez por día;</item>
    /// <item><see cref="NextBatch"/>: las primeras ≤ 50, en orden;</item>
    /// <item><see cref="Apply"/>: lo aceptado y lo rechazado sale de la cola (rechazado no se reintenta); lo que el servidor no nombra
    /// se queda para el próximo envío.</item>
    /// </list>
    /// </summary>
    public static class LeagueQueue
    {
        /// <summary>Pone el registro en la semana de <paramref name="nowUtc"/>. True si cambió algo.</summary>
        public static bool Roll(LeagueRecord record, DateTime nowUtc)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            string week = LeagueRules.WeekId(nowUtc);
            if (string.Equals(record.Week, week, StringComparison.Ordinal))
            {
                return false;
            }

            DateTime start = LeagueRules.WeekStart(nowUtc);
            record.Week = week;
            record.Counted.Clear();
            for (int i = record.Pending.Count - 1; i >= 0; i--)
            {
                if (record.Pending[i].WonAt < start)
                {
                    record.Pending.RemoveAt(i);
                }
            }

            // Lo que queda en la cola es de esta semana (por ejemplo, con el reloj atrasado al guardar): sigue contado.
            for (int i = 0; i < record.Pending.Count; i++)
            {
                if (!record.Counted.Contains(record.Pending[i].WinId))
                {
                    record.Counted.Add(record.Pending[i].WinId);
                }
            }

            if (!string.Equals(record.MeWeek, week, StringComparison.Ordinal))
            {
                record.MeWeek = null;
                record.MePoints = 0;
            }

            return true;
        }

        /// <summary>
        /// Pone <paramref name="win"/> en la cola si su <c>win_id</c> no se contó ya esta semana (llamar antes a <see cref="Roll"/>).
        /// True si entró (la victoria suma; la línea de la victoria la enseña).
        /// </summary>
        public static bool Enqueue(LeagueRecord record, LeagueWin win)
        {
            if (record == null || win == null)
            {
                throw new ArgumentNullException(record == null ? nameof(record) : nameof(win));
            }

            if (record.Counted.Contains(win.WinId) || LeagueRecordJson.FindPending(record, win.WinId) >= 0)
            {
                return false;
            }

            record.Counted.Add(win.WinId);
            record.Pending.Add(win);
            while (record.Pending.Count > LeagueRecord.MaxPending)
            {
                record.Pending.RemoveAt(0);
            }

            return true;
        }

        /// <summary>Las primeras ≤ <see cref="LeagueRules.MaxBatch"/> de la cola, en orden (vacía si no hay nada).</summary>
        public static List<LeagueWin> NextBatch(LeagueRecord record)
        {
            int n = Math.Min(LeagueRules.MaxBatch, record.Pending.Count);
            var batch = new List<LeagueWin>(n);
            for (int i = 0; i < n; i++)
            {
                batch.Add(record.Pending[i]);
            }

            return batch;
        }

        /// <summary>
        /// Aplica la respuesta a <paramref name="sent"/>: saca de la cola lo aceptado y lo rechazado y guarda lo que dijo el servidor
        /// del jugador. Devuelve cuántas salieron.
        /// </summary>
        public static int Apply(LeagueRecord record, IReadOnlyList<LeagueWin> sent, LeagueWinsResult result)
        {
            if (record == null || sent == null || result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            int removed = 0;
            for (int i = 0; i < sent.Count; i++)
            {
                string id = sent[i].WinId;
                if (!Named(result, id))
                {
                    continue;
                }

                int at = LeagueRecordJson.FindPending(record, id);
                if (at >= 0)
                {
                    record.Pending.RemoveAt(at);
                    removed++;
                }
            }

            record.MeWeek = result.Week;
            record.MePoints = Math.Max(0, result.Points);
            record.MeNameKey = result.NameKey;
            record.MeNameNum = result.NameNum;
            record.MeCountry = LeagueRules.IsCountryCode(result.Country) ? result.Country : null;
            return removed;
        }

        private static bool Named(LeagueWinsResult result, string id)
        {
            for (int i = 0; i < result.Accepted.Count; i++)
            {
                if (string.Equals(result.Accepted[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            for (int i = 0; i < result.Rejected.Count; i++)
            {
                if (string.Equals(result.Rejected[i].WinId, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
