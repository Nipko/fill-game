#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Levels.Json;

namespace AF.LiveOps
{
    /// <summary>Una clasificación guardada (caché) con la hora del dispositivo a la que se descargó.</summary>
    public sealed class LeagueCacheEntry
    {
        public LeagueCacheEntry(LeagueBoard board, DateTime fetchedAt)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            FetchedAt = DateTime.SpecifyKind(fetchedAt, DateTimeKind.Utc);
        }

        public LeagueBoard Board { get; }

        /// <summary>Cuándo se descargó (reloj del dispositivo, UTC); <see cref="DateTime.MinValue"/> = hay que volver a pedirla.</summary>
        public DateTime FetchedAt { get; set; }
    }

    /// <summary>
    /// El bloque <c>league</c> del perfil (08 §4.3 con D-046; liga.md §4), solo del dispositivo y nunca fuente de verdad:
    /// <list type="bullet">
    /// <item><c>week</c>: la semana de <c>counted</c>;</item>
    /// <item><c>pending[]</c>: la cola de victorias aún sin respuesta del servidor (<c>{win_id, kind, level | day, won_at}</c>);</item>
    /// <item><c>counted[]</c>: los <c>win_id</c> ya puestos en la cola esta semana (una vez por semana cada nivel, una por día el Reto);</item>
    /// <item><c>me</c>: lo último que dijo el servidor del jugador (<c>{week, points, name_key, name_num, country}</c>);</item>
    /// <item><c>cache</c>: la última clasificación por ámbito (<c>global</c> y <c>country</c>), con <c>fetched_at</c>.</item>
    /// </list>
    /// </summary>
    public sealed class LeagueRecord
    {
        /// <summary>Victorias en la cola como mucho [P]: con más, se pierden las más antiguas (el servidor rechaza más de 40 por hora).</summary>
        public const int MaxPending = 200;

        public string? Week { get; set; }

        public List<LeagueWin> Pending { get; } = new List<LeagueWin>();

        public List<string> Counted { get; } = new List<string>();

        public string? MeWeek { get; set; }

        public int MePoints { get; set; }

        public string? MeNameKey { get; set; }

        public int MeNameNum { get; set; }

        public string? MeCountry { get; set; }

        public LeagueCacheEntry? CacheGlobal { get; set; }

        public LeagueCacheEntry? CacheCountry { get; set; }

        /// <summary>El registro no tiene nada (no hace falta guardar el bloque).</summary>
        public bool IsEmpty => Week == null && Pending.Count == 0 && Counted.Count == 0 && MeWeek == null && CacheGlobal == null && CacheCountry == null;

        public LeagueCacheEntry? Cache(LeagueScope scope) => scope == LeagueScope.Country ? CacheCountry : CacheGlobal;

        public void SetCache(LeagueScope scope, LeagueCacheEntry? entry)
        {
            if (scope == LeagueScope.Country)
            {
                CacheCountry = entry;
            }
            else
            {
                CacheGlobal = entry;
            }
        }
    }

    /// <summary>
    /// JSON del bloque <c>league</c> (<see cref="LeagueRecord"/>): orden de claves estable, InvariantCulture y horas UTC del
    /// contrato. Al leer es tolerante: lo que no se entiende se descarta (es caché y cola, nunca dinero ni progreso), y un texto
    /// ilegible da un registro vacío.
    /// </summary>
    public static class LeagueRecordJson
    {
        public static string Write(LeagueRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var sb = new StringBuilder(256);
            sb.Append('{');
            Key(sb, "week", true);
            Str(sb, record.Week);
            Key(sb, "pending", false);
            sb.Append('[');
            for (int i = 0; i < record.Pending.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                LeagueWin w = record.Pending[i];
                sb.Append('{');
                Key(sb, "win_id", true);
                Str(sb, w.WinId);
                Key(sb, "kind", false);
                Str(sb, LeagueIds.Of(w.Kind));
                if (w.Kind == LeagueWinKind.Daily)
                {
                    Key(sb, "day", false);
                    Str(sb, w.Day);
                }
                else
                {
                    Key(sb, "level", false);
                    Int(sb, w.Level);
                }

                Key(sb, "won_at", false);
                Str(sb, LeagueRules.FormatUtc(w.WonAt));
                sb.Append('}');
            }

            sb.Append(']');
            Key(sb, "counted", false);
            sb.Append('[');
            for (int i = 0; i < record.Counted.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                Str(sb, record.Counted[i]);
            }

            sb.Append(']');
            Key(sb, "me", false);
            if (record.MeWeek == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append('{');
                Key(sb, "week", true);
                Str(sb, record.MeWeek);
                Key(sb, "points", false);
                Int(sb, record.MePoints);
                Key(sb, "name_key", false);
                Str(sb, record.MeNameKey);
                Key(sb, "name_num", false);
                Int(sb, record.MeNameNum);
                Key(sb, "country", false);
                Str(sb, record.MeCountry);
                sb.Append('}');
            }

            Key(sb, "cache", false);
            sb.Append('{');
            Key(sb, "global", true);
            CacheJson(sb, record.CacheGlobal);
            Key(sb, "country", false);
            CacheJson(sb, record.CacheCountry);
            sb.Append("}}");
            return sb.ToString();
        }

        /// <summary>El registro de <paramref name="json"/> (vacío si es null o no se entiende).</summary>
        public static LeagueRecord Read(string? json)
        {
            var record = new LeagueRecord();
            if (string.IsNullOrEmpty(json))
            {
                return record;
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json!);
            }
            catch (FormatException)
            {
                return record;
            }

            if (root.Kind != JsonKind.Object)
            {
                return record;
            }

            record.Week = OptString(root, "week");
            JsonValue? pending = root.Get("pending");
            if (pending != null && pending.Kind == JsonKind.Array)
            {
                for (int i = 0; i < pending.Items.Count && record.Pending.Count < LeagueRecord.MaxPending; i++)
                {
                    LeagueWin? win = ReadWin(pending.Items[i]);
                    if (win != null && FindPending(record, win.WinId) < 0)
                    {
                        record.Pending.Add(win);
                    }
                }
            }

            JsonValue? counted = root.Get("counted");
            if (counted != null && counted.Kind == JsonKind.Array)
            {
                for (int i = 0; i < counted.Items.Count; i++)
                {
                    JsonValue item = counted.Items[i];
                    if (item.Kind == JsonKind.String && !record.Counted.Contains(item.AsString()))
                    {
                        record.Counted.Add(item.AsString());
                    }
                }
            }

            JsonValue? me = root.Get("me");
            if (me != null && me.Kind == JsonKind.Object && OptString(me, "week") != null)
            {
                record.MeWeek = OptString(me, "week");
                record.MePoints = Math.Max(0, OptInt(me, "points") ?? 0);
                record.MeNameKey = OptString(me, "name_key");
                record.MeNameNum = OptInt(me, "name_num") ?? 0;
                string? country = OptString(me, "country");
                record.MeCountry = LeagueRules.IsCountryCode(country) ? country : null;
            }

            JsonValue? cache = root.Get("cache");
            if (cache != null && cache.Kind == JsonKind.Object)
            {
                record.CacheGlobal = ReadCache(cache.Get("global"), LeagueScope.Global);
                record.CacheCountry = ReadCache(cache.Get("country"), LeagueScope.Country);
            }

            return record;
        }

        /// <summary>Posición de <paramref name="winId"/> en la cola, o −1.</summary>
        public static int FindPending(LeagueRecord record, string winId)
        {
            for (int i = 0; i < record.Pending.Count; i++)
            {
                if (string.Equals(record.Pending[i].WinId, winId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        // Caché compacta: las filas como [rank, name_key, name_num, country, points] (el perfil entero va por debajo de 100 KB).
        private static void CacheJson(StringBuilder sb, LeagueCacheEntry? entry)
        {
            if (entry == null)
            {
                sb.Append("null");
                return;
            }

            LeagueBoard b = entry.Board;
            sb.Append('{');
            Key(sb, "fetched_at", true);
            Str(sb, LeagueRules.FormatUtc(entry.FetchedAt == DateTime.MinValue ? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc) : entry.FetchedAt));
            Key(sb, "week", false);
            Str(sb, b.Week);
            Key(sb, "starts_at", false);
            Str(sb, LeagueRules.FormatUtc(b.StartsAt));
            Key(sb, "ends_at", false);
            Str(sb, LeagueRules.FormatUtc(b.EndsAt));
            Key(sb, "country", false);
            Str(sb, b.Country);
            Key(sb, "updated_at", false);
            Str(sb, LeagueRules.FormatUtc(b.UpdatedAt));
            Key(sb, "top", false);
            sb.Append('[');
            for (int i = 0; i < b.Top.Count; i++)
            {
                LeagueRow r = b.Top[i];
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append('[');
                Int(sb, r.Rank);
                sb.Append(',');
                Str(sb, r.NameKey);
                sb.Append(',');
                Int(sb, r.NameNum);
                sb.Append(',');
                Str(sb, r.Country);
                sb.Append(',');
                Int(sb, r.Points);
                sb.Append(']');
            }

            sb.Append(']');
            Key(sb, "me", false);
            sb.Append('{');
            Key(sb, "points", true);
            Int(sb, b.Me.Points);
            Key(sb, "rank", false);
            OptIntJson(sb, b.Me.Rank);
            Key(sb, "percentile", false);
            OptIntJson(sb, b.Me.Percentile);
            Key(sb, "name_key", false);
            Str(sb, b.Me.NameKey);
            Key(sb, "name_num", false);
            Int(sb, b.Me.NameNum);
            Key(sb, "country", false);
            Str(sb, b.Me.Country);
            sb.Append("}}");
        }

        private static LeagueCacheEntry? ReadCache(JsonValue? node, LeagueScope scope)
        {
            try
            {
                return ReadCacheCore(node, scope);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static LeagueCacheEntry? ReadCacheCore(JsonValue? node, LeagueScope scope)
        {
            if (node == null || node.Kind != JsonKind.Object)
            {
                return null;
            }

            string? week = OptString(node, "week");
            if (week == null
                || !LeagueRules.TryParseUtc(OptString(node, "fetched_at"), out DateTime fetched)
                || !LeagueRules.TryParseUtc(OptString(node, "starts_at"), out DateTime starts)
                || !LeagueRules.TryParseUtc(OptString(node, "ends_at"), out DateTime ends)
                || !LeagueRules.TryParseUtc(OptString(node, "updated_at"), out DateTime updated))
            {
                return null;
            }

            var top = new List<LeagueRow>();
            JsonValue? rows = node.Get("top");
            if (rows != null && rows.Kind == JsonKind.Array)
            {
                for (int i = 0; i < rows.Items.Count && top.Count < LeagueRules.MaxTop; i++)
                {
                    JsonValue r = rows.Items[i];
                    if (r.Kind != JsonKind.Array || r.Items.Count != 5 || r.Items[0].Kind != JsonKind.Number || r.Items[1].Kind != JsonKind.String
                        || r.Items[2].Kind != JsonKind.Number || r.Items[4].Kind != JsonKind.Number)
                    {
                        continue;
                    }

                    string? country = r.Items[3].Kind == JsonKind.String ? r.Items[3].AsString() : null;
                    top.Add(new LeagueRow(r.Items[0].AsInt(), r.Items[1].AsString(), r.Items[2].AsInt(), LeagueRules.IsCountryCode(country) ? country : null, r.Items[4].AsInt()));
                }
            }

            JsonValue? me = node.Get("me");
            LeagueMe mine = me != null && me.Kind == JsonKind.Object
                ? new LeagueMe(Math.Max(0, OptInt(me, "points") ?? 0), OptInt(me, "rank"), OptInt(me, "percentile"), OptString(me, "name_key"),
                    OptInt(me, "name_num") ?? 0, OptString(me, "country"))
                : new LeagueMe(0, null, null, null, 0, null);
            string? boardCountry = OptString(node, "country");
            var board = new LeagueBoard(week, starts, ends, scope, LeagueRules.IsCountryCode(boardCountry) ? boardCountry : null, updated, top, mine);
            return new LeagueCacheEntry(board, fetched);
        }

        private static LeagueWin? ReadWin(JsonValue node)
        {
            if (node.Kind != JsonKind.Object || !LeagueIds.TryKind(OptString(node, "kind"), out LeagueWinKind kind)
                || !LeagueRules.TryParseUtc(OptString(node, "won_at"), out DateTime wonAt))
            {
                return null;
            }

            try
            {
                if (kind == LeagueWinKind.Daily)
                {
                    string? day = OptString(node, "day");
                    return LeagueRules.IsDayKey(day) ? LeagueWin.Daily(day!, wonAt) : null;
                }

                int level = OptInt(node, "level") ?? 0;
                return level >= 1 ? LeagueWin.Campaign(level, wonAt) : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static string? OptString(JsonValue node, string key)
        {
            JsonValue? v = node.Get(key);
            return v != null && v.Kind == JsonKind.String ? v.AsString() : null;
        }

        private static int? OptInt(JsonValue node, string key)
        {
            JsonValue? v = node.Get(key);
            if (v == null || v.Kind != JsonKind.Number)
            {
                return null;
            }

            try
            {
                return v.AsInt();
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static void Key(StringBuilder sb, string key, bool first)
        {
            if (!first)
            {
                sb.Append(',');
            }

            sb.Append('"').Append(key).Append("\":");
        }

        private static void Int(StringBuilder sb, int value)
        {
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void OptIntJson(StringBuilder sb, int? value)
        {
            if (value.HasValue)
            {
                Int(sb, value.Value);
            }
            else
            {
                sb.Append("null");
            }
        }

        // Solo ASCII imprimible y sin comillas ni barras (claves, ids, fechas y códigos de país): lo demás se escapa en \uXXXX.
        private static void Str(StringBuilder sb, string? value)
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
                if (c < 0x20 || c > 0x7E || c == '"' || c == '\\')
                {
                    sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(c);
                }
            }

            sb.Append('"');
        }
    }
}
