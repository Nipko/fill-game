#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AF.Platform
{
    /// <summary>
    /// La lista de avisos programados que el programador de Android guarda en el teléfono para contar <c>af_push_sent</c> en la
    /// siguiente apertura (08 §8.2; Q-M5 de D-033). Formato <c>id:tipo:ticks</c> separados por <c>;</c>, con los ticks en UTC: solo
    /// el id, el tipo técnico (<c>daily_ready</c>…) y la hora, ningún dato personal ni texto visible. Sin motor, para probarlo en el
    /// editor (el programador real solo compila en el teléfono). Una entrada ilegible se descarta, sin excepción.
    /// </summary>
    public static class ScheduledNotificationLog
    {
        /// <summary>Escribe <paramref name="entries"/>; se salta las de tipo vacío o con <c>:</c> o <c>;</c> (no se podrían leer).</summary>
        public static string Format(IReadOnlyList<DeliveredNotification> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                DeliveredNotification e = entries[i];
                if (!ValidType(e.Type))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(';');
                }

                sb.Append(e.Id.ToString(CultureInfo.InvariantCulture)).Append(':').Append(e.Type).Append(':')
                    .Append(e.FireAtUtc.Ticks.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        /// <summary>Lee <paramref name="raw"/> (null o vacío = nada) y añade a <paramref name="into"/> las entradas válidas, en orden.</summary>
        public static void Parse(string? raw, List<DeliveredNotification> into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            foreach (string item in raw!.Split(';'))
            {
                string[] parts = item.Split(':');
                if (parts.Length == 3
                    && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                    && long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)
                    && ValidType(parts[1]) && ticks > 0 && ticks <= DateTime.MaxValue.Ticks)
                {
                    into.Add(new DeliveredNotification(id, parts[1], new DateTime(ticks, DateTimeKind.Utc)));
                }
            }
        }

        private static bool ValidType(string? type)
        {
            return !string.IsNullOrEmpty(type) && type!.IndexOf(':') < 0 && type.IndexOf(';') < 0;
        }
    }
}
