#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AF.Backend.Json;

namespace AF.Backend.Contract
{
    /// <summary>
    /// Formas y patrones de la Liga del río (D-046; <c>docs/dev/liga.md</c> §3.1, la fuente única hasta que lleguen sus esquemas a
    /// <c>infra/contracts/v1</c>: <c>league-wins.request</c>, <c>league-wins.response</c> y <c>league-board.response</c>).
    /// </summary>
    public static class LeagueContract
    {
        private static readonly Regex WinIdRe = new Regex(@"^(c:[1-9][0-9]{0,5}|d:[0-9]{4}-[0-9]{2}-[0-9]{2})$", RegexOptions.CultureInvariant);
        private static readonly Regex DayRe = new Regex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}$", RegexOptions.CultureInvariant);
        private static readonly Regex WeekRe = new Regex(@"^[0-9]{4}-W[0-9]{2}$", RegexOptions.CultureInvariant);
        private static readonly Regex CountryRe = new Regex(@"^[A-Z]{2}$", RegexOptions.CultureInvariant);
        private static readonly Regex NameKeyRe = new Regex(@"^league_name_[a-z0-9_]{1,48}$", RegexOptions.CultureInvariant);
        private static readonly Regex TimeRe = new Regex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?Z$", RegexOptions.CultureInvariant);

        /// <summary>Victorias por petición como mucho.</summary>
        public const int MaxWins = 50;

        /// <summary>Filas del top como mucho.</summary>
        public const int MaxTop = 100;

        /// <summary>Los motivos de <c>rejected[].reason</c>.</summary>
        public static readonly string[] RejectReasons = { "duplicate", "rate_limited", "unreachable_level", "outside_week", "invalid" };

        public static bool IsWinId(string? value) => value != null && WinIdRe.IsMatch(value);

        public static bool IsDay(string? value) => value != null && DayRe.IsMatch(value);

        public static bool IsWeek(string? value) => value != null && WeekRe.IsMatch(value);

        public static bool IsCountry(string? value) => value != null && CountryRe.IsMatch(value);

        public static bool IsNameKey(string? value) => value != null && NameKeyRe.IsMatch(value);

        public static bool IsUtc(string? value) => value != null && TimeRe.IsMatch(value);

        public static bool IsRejectReason(string? value) => value != null && Array.IndexOf(RejectReasons, value) >= 0;

        internal static DateTime Utc(string text)
        {
            return DateTime.SpecifyKind(
                DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), DateTimeKind.Utc);
        }

        internal static string Format(DateTime utc)
        {
            return utc.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
        }

        // Un campo que puede ser null: null si falta o es null; si no, una cadena válida.
        internal static string? NullableString(ObjectReader r, string key, Func<string?, bool> isValid)
        {
            JsonNode? node = r.Optional(key);
            return node == null || node.Kind == JsonKind.Null ? null : r.AsString(node, key, isValid);
        }

        internal static long? NullableInteger(JsonNode? node, string where, string key, long min, long max)
        {
            if (node == null || node.Kind == JsonKind.Null)
            {
                return null;
            }

            if (node.Kind != JsonKind.Number || !long.TryParse(node.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long v) || v < min || v > max)
            {
                throw new ContractException(where + ": '" + key + "' debe ser un entero entre " + min + " y " + max + " o null.");
            }

            return v;
        }
    }

    /// <summary>Una victoria de <c>POST /v1/league/wins</c>.</summary>
    public sealed class LeagueWinDto
    {
        public LeagueWinDto(string winId, string kind, int? level, string? day, DateTime wonAt)
        {
            if (!LeagueContract.IsWinId(winId))
            {
                throw new ArgumentException("win_id fuera del contrato.", nameof(winId));
            }

            bool daily = kind == "daily";
            if (!daily && kind != "campaign")
            {
                throw new ArgumentException("kind fuera del contrato.", nameof(kind));
            }

            if (daily ? !LeagueContract.IsDay(day) || level.HasValue : !level.HasValue || level.Value < 1 || day != null)
            {
                throw new ArgumentException("campaign lleva level y daily lleva day.", nameof(kind));
            }

            WinId = winId;
            Kind = kind;
            Level = level;
            Day = day;
            WonAt = wonAt;
        }

        public string WinId { get; }

        /// <summary><c>campaign</c> o <c>daily</c>.</summary>
        public string Kind { get; }

        public int? Level { get; }

        public string? Day { get; }

        public DateTime WonAt { get; }
    }

    /// <summary><c>POST /v1/league/wins</c>, petición (<c>league-wins.request.schema.json</c>; liga.md §3.1).</summary>
    public sealed class LeagueWinsRequestDto
    {
        public LeagueWinsRequestDto(IReadOnlyList<LeagueWinDto> wins, int maxLevel, string? region)
        {
            if (wins == null || wins.Count < 1 || wins.Count > LeagueContract.MaxWins)
            {
                throw new ArgumentException("Entre 1 y 50 victorias.", nameof(wins));
            }

            if (region != null && !LeagueContract.IsCountry(region))
            {
                throw new ArgumentException("region fuera del contrato.", nameof(region));
            }

            Wins = wins;
            MaxLevel = Math.Max(0, maxLevel);
            Region = region;
        }

        public IReadOnlyList<LeagueWinDto> Wins { get; }

        public int MaxLevel { get; }

        /// <summary>ISO 3166-1 alfa-2, o null (entonces no se manda).</summary>
        public string? Region { get; }

        public string ToJson()
        {
            JsonNode wins = JsonNode.NewArray();
            for (int i = 0; i < Wins.Count; i++)
            {
                LeagueWinDto w = Wins[i];
                JsonNode item = JsonNode.NewObject().Set("win_id", JsonNode.FromString(w.WinId)).Set("kind", JsonNode.FromString(w.Kind));
                if (w.Level.HasValue)
                {
                    item.Set("level", JsonNode.FromLong(w.Level.Value));
                }

                if (w.Day != null)
                {
                    item.Set("day", JsonNode.FromString(w.Day));
                }

                item.Set("won_at", JsonNode.FromString(LeagueContract.Format(w.WonAt)));
                wins.Add(item);
            }

            JsonNode root = JsonNode.NewObject().Set("wins", wins).Set("max_level", JsonNode.FromLong(MaxLevel));
            if (Region != null)
            {
                root.Set("region", JsonNode.FromString(Region));
            }

            return BackendJson.Write(root);
        }

        /// <summary>Lee una petición (estricta: los ejemplos del contrato y las pruebas).</summary>
        public static bool TryParse(string? json, out LeagueWinsRequestDto? value, out string? error) =>
            ContractRules.TryRead(json, Read, out value, out error);

        internal static LeagueWinsRequestDto Read(JsonNode node)
        {
            var r = new ObjectReader(node, "leagueWins.request", strict: true);
            JsonNode array = r.RequiredArray("wins", LeagueContract.MaxWins);
            var wins = new List<LeagueWinDto>(array.Items.Count);
            for (int i = 0; i < array.Items.Count; i++)
            {
                var w = new ObjectReader(array.Items[i], "leagueWins.request.wins", strict: true);
                string id = w.RequiredString("win_id", LeagueContract.IsWinId);
                string kind = w.RequiredString("kind", k => k == "campaign" || k == "daily");
                long? level = w.OptionalInteger("level", 1, 999999);
                string? day = w.OptionalString("day", LeagueContract.IsDay);
                string at = w.RequiredString("won_at", LeagueContract.IsUtc);
                w.End();
                try
                {
                    wins.Add(new LeagueWinDto(id, kind, level.HasValue ? (int)level.Value : (int?)null, day, LeagueContract.Utc(at)));
                }
                catch (ArgumentException e)
                {
                    throw new ContractException("leagueWins.request.wins: " + e.Message);
                }
            }

            if (wins.Count == 0)
            {
                throw new ContractException("leagueWins.request: 'wins' vacío.");
            }

            int max = (int)r.RequiredInteger("max_level", 0, 999999);
            string? region = r.OptionalString("region", LeagueContract.IsCountry);
            r.End();
            return new LeagueWinsRequestDto(wins, max, region);
        }
    }

    /// <summary>Respuesta 200 de <c>POST /v1/league/wins</c> (<c>league-wins.response.schema.json</c>).</summary>
    public sealed class LeagueWinsResponseDto
    {
        private LeagueWinsResponseDto(string week, int points, List<string> accepted, List<KeyValuePair<string, string>> rejected, string nameKey, int nameNum, string? country)
        {
            Week = week;
            Points = points;
            Accepted = accepted;
            Rejected = rejected;
            NameKey = nameKey;
            NameNum = nameNum;
            Country = country;
        }

        public string Week { get; }

        public int Points { get; }

        public IReadOnlyList<string> Accepted { get; }

        /// <summary><c>win_id</c> → <c>reason</c>, en orden.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Rejected { get; }

        public string NameKey { get; }

        public int NameNum { get; }

        public string? Country { get; }

        public static bool TryParse(string? json, out LeagueWinsResponseDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        public static bool TryParseStrict(string? json, out LeagueWinsResponseDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static LeagueWinsResponseDto Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "leagueWins.response", strict);
            string week = r.RequiredString("week", LeagueContract.IsWeek);
            int points = (int)r.RequiredInteger("points", 0, int.MaxValue);
            List<string> accepted = r.StringSet(r.RequiredArray("accepted", LeagueContract.MaxWins), "accepted", LeagueContract.IsWinId);
            JsonNode rejectedNode = r.RequiredArray("rejected", LeagueContract.MaxWins);
            var rejected = new List<KeyValuePair<string, string>>(rejectedNode.Items.Count);
            for (int i = 0; i < rejectedNode.Items.Count; i++)
            {
                var x = new ObjectReader(rejectedNode.Items[i], "leagueWins.response.rejected", strict);
                string id = x.RequiredString("win_id", LeagueContract.IsWinId);
                // Un motivo que este cliente no conoce no rompe la respuesta (lo que importa es que sale de la cola).
                string reason = x.RequiredString("reason", strict ? LeagueContract.IsRejectReason : (Func<string?, bool>?)null);
                x.End();
                rejected.Add(new KeyValuePair<string, string>(id, reason));
            }

            string nameKey = r.RequiredString("name_key", LeagueContract.IsNameKey);
            int nameNum = (int)r.RequiredInteger("name_num", 0, 9999);
            string? country = LeagueContract.NullableString(r, "country", LeagueContract.IsCountry);
            r.End();
            return new LeagueWinsResponseDto(week, points, accepted, rejected, nameKey, nameNum, country);
        }
    }

    /// <summary>Una fila de <c>top[]</c>.</summary>
    public sealed class LeagueRowDto
    {
        internal LeagueRowDto(int rank, string nameKey, int nameNum, string? country, int points)
        {
            Rank = rank;
            NameKey = nameKey;
            NameNum = nameNum;
            Country = country;
            Points = points;
        }

        public int Rank { get; }

        public string NameKey { get; }

        public int NameNum { get; }

        public string? Country { get; }

        public int Points { get; }
    }

    /// <summary>Respuesta 200 de <c>GET /v1/league?scope=</c> (<c>league-board.response.schema.json</c>).</summary>
    public sealed class LeagueBoardResponseDto
    {
        private LeagueBoardResponseDto()
        {
        }

        public string Week { get; private set; } = "";

        public DateTime StartsAt { get; private set; }

        public DateTime EndsAt { get; private set; }

        /// <summary><c>global</c> o <c>country</c>.</summary>
        public string Scope { get; private set; } = "global";

        public string? Country { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public IReadOnlyList<LeagueRowDto> Top { get; private set; } = Array.Empty<LeagueRowDto>();

        public int MePoints { get; private set; }

        public int? MeRank { get; private set; }

        public int? MePercentile { get; private set; }

        public string? MeNameKey { get; private set; }

        public int MeNameNum { get; private set; }

        public string? MeCountry { get; private set; }

        public static bool TryParse(string? json, out LeagueBoardResponseDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        public static bool TryParseStrict(string? json, out LeagueBoardResponseDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static LeagueBoardResponseDto Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "leagueBoard.response", strict);
            var b = new LeagueBoardResponseDto
            {
                Week = r.RequiredString("week", LeagueContract.IsWeek),
                StartsAt = LeagueContract.Utc(r.RequiredString("starts_at", LeagueContract.IsUtc)),
                EndsAt = LeagueContract.Utc(r.RequiredString("ends_at", LeagueContract.IsUtc)),
                Scope = r.RequiredString("scope", s => s == "global" || s == "country"),
                Country = LeagueContract.NullableString(r, "country", LeagueContract.IsCountry),
                UpdatedAt = LeagueContract.Utc(r.RequiredString("updated_at", LeagueContract.IsUtc)),
            };

            JsonNode top = r.RequiredArray("top", LeagueContract.MaxTop);
            var rows = new List<LeagueRowDto>(top.Items.Count);
            for (int i = 0; i < top.Items.Count; i++)
            {
                var x = new ObjectReader(top.Items[i], "leagueBoard.response.top", strict);
                int rank = (int)x.RequiredInteger("rank", 1, 1000000);
                string key = x.RequiredString("name_key", LeagueContract.IsNameKey);
                int num = (int)x.RequiredInteger("name_num", 0, 9999);
                string? country = LeagueContract.NullableString(x, "country", LeagueContract.IsCountry);
                int points = (int)x.RequiredInteger("points", 0, int.MaxValue);
                x.End();
                rows.Add(new LeagueRowDto(rank, key, num, country, points));
            }

            b.Top = rows;
            var me = new ObjectReader(r.Required("me"), "leagueBoard.response.me", strict);
            b.MePoints = (int)me.RequiredInteger("points", 0, int.MaxValue);
            b.MeRank = (int?)LeagueContract.NullableInteger(me.Optional("rank"), "leagueBoard.response.me", "rank", 1, 1000000);
            b.MePercentile = (int?)LeagueContract.NullableInteger(me.Optional("percentile"), "leagueBoard.response.me", "percentile", 1, 100);
            b.MeNameKey = LeagueContract.NullableString(me, "name_key", LeagueContract.IsNameKey);
            b.MeNameNum = (int)(me.OptionalInteger("name_num", 0, 9999) ?? 0);
            b.MeCountry = LeagueContract.NullableString(me, "country", LeagueContract.IsCountry);
            me.End();
            r.End();
            return b;
        }
    }
}
