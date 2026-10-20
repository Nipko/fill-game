#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Levels;
using AF.Levels.Json;

namespace AF.LiveOps
{
    /// <summary>
    /// Valor de <c>daily_overrides</c> (08 §9) con la forma decidida en R-Q6: <c>{fecha: {banda: id del pool}}</c>, p. ej.
    /// <c>{"2026-12-25": {"1": "d1_n0100", "2": "d2_n0033"}}</c> (nota <c>docs/dev/reto-del-rio.md</c> §6). Inmutable. La
    /// consulta (<see cref="TryGet"/>) es una búsqueda por clave, sin asignaciones y sin recorrer el diccionario.
    /// </summary>
    /// <remarks>
    /// Que el id exista en el pool no se comprueba aquí: lo decide <c>DailyCalendar.Resolve</c>
    /// con <see cref="DailyPool.FindById"/>.
    /// </remarks>
    public sealed class DailyOverrides
    {
        /// <summary>El default de 08 §9, <c>{}</c>: ninguna sustitución.</summary>
        public static readonly DailyOverrides Empty = new DailyOverrides(new Dictionary<string, string?[]>(StringComparer.Ordinal));

        // fecha yyyy-MM-dd → id por banda (índice banda − 1; null = la banda no tiene sustitución).
        private readonly Dictionary<string, string?[]> _byDate;

        private DailyOverrides(Dictionary<string, string?[]> byDate)
        {
            _byDate = byDate;
        }

        /// <summary>Número de fechas con alguna sustitución.</summary>
        public int DateCount => _byDate.Count;

        /// <summary>
        /// Id sustituto para la fecha <paramref name="dateKey"/> (<c>yyyy-MM-dd</c>) y la banda <paramref name="band"/> (1-3), si la
        /// tabla lo trae. No asigna memoria.
        /// </summary>
        public bool TryGet(string dateKey, int band, out string id)
        {
            id = "";
            if (dateKey == null || band < DailyPool.MinBand || band > DailyPool.MaxBand)
            {
                return false;
            }

            if (!_byDate.TryGetValue(dateKey, out string?[]? ids))
            {
                return false;
            }

            string? found = ids[band - DailyPool.MinBand];
            if (found == null)
            {
                return false;
            }

            id = found;
            return true;
        }

        /// <summary>
        /// Lee el JSON de Remote Config. Rechaza (con <see cref="FormatException"/>) lo que no tiene la forma de R-Q6: raíz que no
        /// es objeto, fecha que no es <c>yyyy-MM-dd</c> válida, banda que no es <c>"1"</c>-<c>"3"</c>, id que no es una cadena no
        /// vacía, y fechas o bandas repetidas. El llamador decide qué hacer con un valor rechazado (lo prudente: el default
        /// <see cref="Empty"/>, que es lo que hace <see cref="TryParse"/>).
        /// </summary>
        /// <remarks>
        /// Las comprobaciones de fecha y de banda repetidas son defensa en profundidad: <see cref="JsonParser"/> ya rechaza las
        /// claves duplicadas dentro de un mismo objeto, así que hoy no se alcanzan; se mantienen por si el lector de JSON cambia.
        /// </remarks>
        public static DailyOverrides Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            JsonValue root = JsonParser.Parse(json);
            if (root.Kind != JsonKind.Object)
            {
                throw new FormatException("daily_overrides: la raíz debe ser un objeto {fecha: {banda: id}}.");
            }

            var byDate = new Dictionary<string, string?[]>(StringComparer.Ordinal);
            IReadOnlyList<KeyValuePair<string, JsonValue>> dates = root.Members;
            for (int i = 0; i < dates.Count; i++)
            {
                string date = dates[i].Key;
                if (!IsDateKey(date))
                {
                    throw new FormatException("daily_overrides: «" + date + "» no es una fecha yyyy-MM-dd.");
                }

                if (byDate.ContainsKey(date))
                {
                    throw new FormatException("daily_overrides: la fecha " + date + " está repetida.");
                }

                JsonValue perBand = dates[i].Value;
                if (perBand.Kind != JsonKind.Object)
                {
                    throw new FormatException("daily_overrides[" + date + "]: debe ser un objeto {banda: id} (R-Q6).");
                }

                var ids = new string?[DailyPool.MaxBand];
                IReadOnlyList<KeyValuePair<string, JsonValue>> bands = perBand.Members;
                for (int j = 0; j < bands.Count; j++)
                {
                    int band = BandOfKey(bands[j].Key);
                    if (band == 0)
                    {
                        throw new FormatException("daily_overrides[" + date + "]: «" + bands[j].Key + "» no es una banda 1-3.");
                    }

                    if (ids[band - 1] != null)
                    {
                        throw new FormatException("daily_overrides[" + date + "]: la banda " + bands[j].Key + " está repetida.");
                    }

                    JsonValue id = bands[j].Value;
                    if (id.Kind != JsonKind.String || id.AsString().Length == 0)
                    {
                        throw new FormatException("daily_overrides[" + date + "][" + bands[j].Key + "]: el id debe ser una cadena no vacía.");
                    }

                    ids[band - 1] = id.AsString();
                }

                byDate.Add(date, ids);
            }

            return byDate.Count == 0 ? Empty : new DailyOverrides(byDate);
        }

        /// <summary>
        /// Lee el JSON de Remote Config sin lanzar: si el valor no tiene la forma de R-Q6 (<see cref="Parse"/>) o es null, deja
        /// <paramref name="result"/> = <see cref="Empty"/> (el default <c>{}</c> de 08 §9), pone el motivo en
        /// <paramref name="error"/> y devuelve false. Con un valor válido, <paramref name="error"/> queda vacío.
        /// </summary>
        public static bool TryParse(string? json, out DailyOverrides result, out string error)
        {
            if (json == null)
            {
                result = Empty;
                error = "daily_overrides: el valor es null.";
                return false;
            }

            try
            {
                result = Parse(json);
                error = "";
                return true;
            }
            catch (FormatException e)
            {
                result = Empty;
                error = e.Message;
                return false;
            }
        }

        /// <summary>
        /// Escribe la tabla como JSON compacto, con las fechas en orden ordinal y las bandas en orden (<c>{}</c> si está vacía). Solo
        /// para registro y tests: no decide nada.
        /// </summary>
        public string Format()
        {
            var dates = new List<string>(_byDate.Keys);
            dates.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder("{");
            for (int i = 0; i < dates.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('"').Append(dates[i]).Append("\":{");
                string?[] ids = _byDate[dates[i]];
                bool first = true;
                for (int b = 0; b < ids.Length; b++)
                {
                    if (ids[b] == null)
                    {
                        continue;
                    }

                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;
                    sb.Append('"').Append((b + 1).ToString(CultureInfo.InvariantCulture)).Append("\":\"").Append(Escape(ids[b]!)).Append('"');
                }

                sb.Append('}');
            }

            return sb.Append('}').ToString();
        }

        private static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static bool IsDateKey(string text)
        {
            return text.Length == 10
                && DateTime.TryParseExact(text, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        private static int BandOfKey(string key)
        {
            if (key.Length != 1)
            {
                return 0;
            }

            int band = key[0] - '0';
            return band >= DailyPool.MinBand && band <= DailyPool.MaxBand ? band : 0;
        }
    }
}
