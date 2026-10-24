#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AF.Backend.Json;

namespace AF.Backend.Contract
{
    /// <summary>
    /// Formas y patrones de la Liga del río (D-046; <c>docs/dev/liga.md</c> §3.1 y los esquemas de <c>infra/contracts/v1</c>:
    /// <c>league-wins.request</c>, <c>league-wins.response</c> y <c>league-board.response</c>).
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

        /// <summary>Tope de los puntos de la semana (esquemas).</summary>
        public const int MaxPoints = 999999;

        /// <summary>Las cifras del seudónimo (esquemas: 1000-9999).</summary>
        public const int MinNameNum = 1000;

        public const int MaxNameNum = 9999;

        /// <summary>Los motivos de <c>rejected[].reason</c>.</summary>
        public static readonly string[] RejectReasons = { "duplicate", "rate_limited", "unreachable_level", "outside_week", "invalid" };

        /// <summary>Los 24 animales que admite el esquema (liga.md §1), en su orden.</summary>
        public static readonly string[] NameKeys =
        {
            "league_name_capybara", "league_name_jaguar", "league_name_toucan", "league_name_macaw", "league_name_boto",
            "league_name_sloth", "league_name_tapir", "league_name_otter", "league_name_manatee", "league_name_turtle",
            "league_name_howler", "league_name_squirrel_monkey", "league_name_caiman", "league_name_anaconda",
            "league_name_dart_frog", "league_name_morpho", "league_name_hummingbird", "league_name_heron", "league_name_kingfisher",
            "league_name_anteater", "league_name_armadillo", "league_name_ocelot", "league_name_coati", "league_name_arapaima",
        };

        public static bool IsWinId(string? value) => value != null && WinIdRe.IsMatch(value);

        public static bool IsDay(string? value) => value != null && DayRe.IsMatch(value);

        public static bool IsWeek(string? value) => value != null && WeekRe.IsMatch(value);

        public static bool IsCountry(string? value) => value != null && CountryRe.IsMatch(value);

        /// <summary>Forma de una clave de animal (lo que tolera el cliente: el servidor puede tener una lista más nueva).</summary>
        public static bool IsNameKey(string? value) => value != null && NameKeyRe.IsMatch(value);

        /// <summary>Uno de los 24 del esquema (lectura estricta).</summary>
        public static bool IsSchemaNameKey(string? value) => value != null && Array.IndexOf(NameKeys, value) >= 0;

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

        internal static JsonNode NullableText(string? value) => value == null ? JsonNode.Null : JsonNode.FromString(value);

        internal static JsonNode NullableNumber(int? value) => value.HasValue ? JsonNode.FromLong(value.Value) : JsonNode.Null;
    }

    /// <summary>Una victoria de <c>POST /v1/league/wins</c>.</summary>
    public sealed class LeagueWinDto
    {
        public LeagueWinDto(string winId, string kind, int? level, string? day, DateTime wonAt)
            : this(winId, kind, level, day, LeagueContract.Format(wonAt))
        {
        }

        private LeagueWinDto(string winId, string kind, int? level, string? day, string wonAtText)
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

            if (!LeagueContract.IsUtc(wonAtText))
            {
                throw new ArgumentException("won_at fuera del contrato.", nameof(wonAtText));
            }

            WinId = winId;
            Kind = kind;
            Level = level;
            Day = day;
            WonAtText = wonAtText;
            WonAt = LeagueContract.Utc(wonAtText);
        }

        public string WinId { get; }

        /// <summary><c>campaign</c> o <c>daily</c>.</summary>
        public string Kind { get; }

        public int? Level { get; }

        public string? Day { get; }

        public DateTime WonAt { get; }

        /// <summary><c>won_at</c> tal como va en el JSON.</summary>
        public string WonAtText { get; }

        internal JsonNode ToNode()
        {
            JsonNode item = JsonNode.NewObject().Set("win_id", JsonNode.FromString(WinId)).Set("kind", JsonNode.FromString(Kind));
            if (Level.HasValue)
            {
                item.Set("level", JsonNode.FromLong(Level.Value));
            }

            if (Day != null)
            {
                item.Set("day", JsonNode.FromString(Day));
            }

            return item.Set("won_at", JsonNode.FromString(WonAtText));
        }

        // Una victoria bien formada (lectura estricta); null si no lo es.
        internal static LeagueWinDto? TryRead(JsonNode node)
        {
            try
            {
                var w = new ObjectReader(node, "leagueWins.request.wins", strict: true);
                string id = w.RequiredString("win_id", LeagueContract.IsWinId);
                string kind = w.RequiredString("kind", k => k == "campaign" || k == "daily");
                long? level = w.OptionalInteger("level", 1, 999999);
                string? day = w.OptionalString("day", LeagueContract.IsDay);
                string at = w.RequiredString("won_at", LeagueContract.IsUtc);
                w.End();
                return new LeagueWinDto(id, kind, level.HasValue ? (int)level.Value : (int?)null, day, at);
            }
            catch (ContractException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// <c>POST /v1/league/wins</c>, petición (<c>league-wins.request.schema.json</c>). El esquema solo fija el sobre (1-50 elementos,
    /// <c>max_level</c> y <c>region</c> libre): cada elemento se juzga por separado en el servidor y uno mal formado vuelve como
    /// <c>invalid</c>. Por eso hay dos lecturas: <see cref="TryParse"/> (la del esquema: conserva los elementos tal cual) y
    /// <see cref="TryParseStrict"/> (lo que escribe este cliente: todas bien formadas y la región ISO o ausente).
    /// </summary>
    public sealed class LeagueWinsRequestDto
    {
        private readonly List<JsonNode>? _rawItems;
        private readonly JsonNode? _rawRegion;
        private readonly bool _regionPresent;

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

            for (int i = 0; i < wins.Count; i++)
            {
                if (wins[i] == null)
                {
                    throw new ArgumentException("Victoria nula.", nameof(wins));
                }
            }

            Wins = wins;
            MaxLevel = Math.Max(0, maxLevel);
            Region = region;
            _regionPresent = region != null;
        }

        private LeagueWinsRequestDto(List<LeagueWinDto> wins, List<JsonNode> raw, int maxLevel, string? region, bool regionPresent, JsonNode? rawRegion)
        {
            Wins = wins;
            _rawItems = raw;
            MaxLevel = maxLevel;
            Region = region;
            _regionPresent = regionPresent;
            _rawRegion = rawRegion;
        }

        /// <summary>Las victorias bien formadas.</summary>
        public IReadOnlyList<LeagueWinDto> Wins { get; }

        /// <summary>Elementos de <c>wins</c> tal como llegaron (con los mal formados); igual que <see cref="Wins"/> si los escribió el cliente.</summary>
        public int ItemCount => _rawItems?.Count ?? Wins.Count;

        public int MaxLevel { get; }

        /// <summary>ISO 3166-1 alfa-2, o null (el cliente entonces no manda el campo; cualquier otro valor es país desconocido).</summary>
        public string? Region { get; }

        public string ToJson()
        {
            JsonNode wins = JsonNode.NewArray();
            if (_rawItems != null)
            {
                for (int i = 0; i < _rawItems.Count; i++)
                {
                    wins.Add(_rawItems[i]);
                }
            }
            else
            {
                for (int i = 0; i < Wins.Count; i++)
                {
                    wins.Add(Wins[i].ToNode());
                }
            }

            JsonNode root = JsonNode.NewObject().Set("wins", wins).Set("max_level", JsonNode.FromLong(MaxLevel));
            if (_regionPresent)
            {
                root.Set("region", _rawRegion ?? JsonNode.FromString(Region!));
            }

            return BackendJson.Write(root);
        }

        /// <summary>La lectura del esquema: el sobre estricto, cada elemento tal cual (los bien formados, en <see cref="Wins"/>).</summary>
        public static bool TryParse(string? json, out LeagueWinsRequestDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        /// <summary>Lo que escribe este cliente: todas las victorias bien formadas y la región ISO o ausente.</summary>
        public static bool TryParseStrict(string? json, out LeagueWinsRequestDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static LeagueWinsRequestDto Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "leagueWins.request", strict: true);
            JsonNode array = r.RequiredArray("wins", LeagueContract.MaxWins);
            if (array.Items.Count == 0)
            {
                throw new ContractException("leagueWins.request: 'wins' vacío.");
            }

            var wins = new List<LeagueWinDto>(array.Items.Count);
            var raw = new List<JsonNode>(array.Items.Count);
            for (int i = 0; i < array.Items.Count; i++)
            {
                raw.Add(array.Items[i]);
                LeagueWinDto? win = LeagueWinDto.TryRead(array.Items[i]);
                if (win != null)
                {
                    wins.Add(win);
                }
                else if (strict)
                {
                    throw new ContractException("leagueWins.request.wins[" + i.ToString(CultureInfo.InvariantCulture) + "]: victoria mal formada.");
                }
            }

            int max = (int)r.RequiredInteger("max_level", 0, 999999);
            bool regionPresent = r.Has("region");
            JsonNode? rawRegion = r.Optional("region");
            string? region = rawRegion != null && rawRegion.Kind == JsonKind.String && LeagueContract.IsCountry(rawRegion.Text) ? rawRegion.Text : null;
            if (strict && regionPresent && region == null)
            {
                throw new ContractException("leagueWins.request: 'region' debe ser ISO 3166-1 alfa-2 o no ir.");
            }

            r.End();
            return new LeagueWinsRequestDto(wins, raw, max, region, regionPresent, rawRegion);
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

        /// <summary><c>win_id</c> → <c>reason</c>, en orden (el <c>win_id</c> puede ser «» si el elemento no lo tenía).</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Rejected { get; }

        public string NameKey { get; }

        public int NameNum { get; }

        public string? Country { get; }

        public string ToJson()
        {
            JsonNode accepted = JsonNode.NewArray();
            for (int i = 0; i < Accepted.Count; i++)
            {
                accepted.Add(JsonNode.FromString(Accepted[i]));
            }

            JsonNode rejected = JsonNode.NewArray();
            for (int i = 0; i < Rejected.Count; i++)
            {
                rejected.Add(JsonNode.NewObject().Set("win_id", JsonNode.FromString(Rejected[i].Key)).Set("reason", JsonNode.FromString(Rejected[i].Value)));
            }

            return BackendJson.Write(JsonNode.NewObject()
                .Set("week", JsonNode.FromString(Week))
                .Set("points", JsonNode.FromLong(Points))
                .Set("accepted", accepted)
                .Set("rejected", rejected)
                .Set("name_key", JsonNode.FromString(NameKey))
                .Set("name_num", JsonNode.FromLong(NameNum))
                .Set("country", LeagueContract.NullableText(Country)));
        }

        /// <summary>Lectura tolerante (lo que usa el cliente): claves nuevas y animales más nuevos no la rompen.</summary>
        public static bool TryParse(string? json, out LeagueWinsResponseDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        /// <summary>Lectura con los límites exactos del esquema.</summary>
        public static bool TryParseStrict(string? json, out LeagueWinsResponseDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static LeagueWinsResponseDto Read(JsonNode node, bool strict)
        {
            var r = new ObjectReader(node, "leagueWins.response", strict);
            string week = r.RequiredString("week", LeagueContract.IsWeek);
            int points = (int)r.RequiredInteger("points", 0, strict ? LeagueContract.MaxPoints : int.MaxValue);

            // accepted y rejected se casan con la cola por win_id, nunca por posición; el esquema solo pide cadenas: un win_id que
            // no es de la cola (o «» de un elemento mal formado) no rompe la respuesta.
            JsonNode acceptedNode = r.RequiredArray("accepted", LeagueContract.MaxWins);
            var accepted = new List<string>(acceptedNode.Items.Count);
            for (int i = 0; i < acceptedNode.Items.Count; i++)
            {
                accepted.Add(r.AsString(acceptedNode.Items[i], "accepted", null));
            }

            JsonNode rejectedNode = r.RequiredArray("rejected", LeagueContract.MaxWins);
            var rejected = new List<KeyValuePair<string, string>>(rejectedNode.Items.Count);
            for (int i = 0; i < rejectedNode.Items.Count; i++)
            {
                var x = new ObjectReader(rejectedNode.Items[i], "leagueWins.response.rejected", strict);
                string id = x.RequiredString("win_id");
                // Un motivo que este cliente no conoce no rompe la respuesta tolerante (lo que importa es que sale de la cola).
                string reason = x.RequiredString("reason", strict ? LeagueContract.IsRejectReason : (Func<string?, bool>?)null);
                x.End();
                rejected.Add(new KeyValuePair<string, string>(id, reason));
            }

            string nameKey = r.RequiredString("name_key", strict ? LeagueContract.IsSchemaNameKey : (Func<string?, bool>)LeagueContract.IsNameKey);
            int nameNum = (int)r.RequiredInteger("name_num", strict ? LeagueContract.MinNameNum : 0, LeagueContract.MaxNameNum);
            if (strict && !r.Has("country"))
            {
                throw new ContractException("leagueWins.response: falta 'country'.");
            }

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
        private string _startsAtText = "";
        private string _endsAtText = "";
        private string _updatedAtText = "";

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

        public string ToJson()
        {
            JsonNode top = JsonNode.NewArray();
            for (int i = 0; i < Top.Count; i++)
            {
                LeagueRowDto row = Top[i];
                top.Add(JsonNode.NewObject()
                    .Set("rank", JsonNode.FromLong(row.Rank))
                    .Set("name_key", JsonNode.FromString(row.NameKey))
                    .Set("name_num", JsonNode.FromLong(row.NameNum))
                    .Set("country", LeagueContract.NullableText(row.Country))
                    .Set("points", JsonNode.FromLong(row.Points)));
            }

            JsonNode me = JsonNode.NewObject()
                .Set("points", JsonNode.FromLong(MePoints))
                .Set("rank", LeagueContract.NullableNumber(MeRank))
                .Set("percentile", LeagueContract.NullableNumber(MePercentile))
                .Set("name_key", LeagueContract.NullableText(MeNameKey))
                .Set("name_num", JsonNode.FromLong(MeNameNum))
                .Set("country", LeagueContract.NullableText(MeCountry));
            return BackendJson.Write(JsonNode.NewObject()
                .Set("week", JsonNode.FromString(Week))
                .Set("starts_at", JsonNode.FromString(_startsAtText))
                .Set("ends_at", JsonNode.FromString(_endsAtText))
                .Set("scope", JsonNode.FromString(Scope))
                .Set("country", LeagueContract.NullableText(Country))
                .Set("updated_at", JsonNode.FromString(_updatedAtText))
                .Set("top", top)
                .Set("me", me));
        }

        /// <summary>Lectura tolerante (lo que usa el cliente).</summary>
        public static bool TryParse(string? json, out LeagueBoardResponseDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: false), out value, out error);

        /// <summary>Lectura con los límites exactos del esquema.</summary>
        public static bool TryParseStrict(string? json, out LeagueBoardResponseDto? value, out string? error) =>
            ContractRules.TryRead(json, n => Read(n, strict: true), out value, out error);

        internal static LeagueBoardResponseDto Read(JsonNode node, bool strict)
        {
            const string where = "leagueBoard.response";
            Func<string?, bool> nameKey = strict ? LeagueContract.IsSchemaNameKey : (Func<string?, bool>)LeagueContract.IsNameKey;
            int maxPoints = strict ? LeagueContract.MaxPoints : int.MaxValue;
            int minNum = strict ? LeagueContract.MinNameNum : 0;
            var r = new ObjectReader(node, where, strict);
            var b = new LeagueBoardResponseDto
            {
                Week = r.RequiredString("week", LeagueContract.IsWeek),
                _startsAtText = r.RequiredString("starts_at", LeagueContract.IsUtc),
                _endsAtText = r.RequiredString("ends_at", LeagueContract.IsUtc),
                Scope = r.RequiredString("scope", s => s == "global" || s == "country"),
            };
            if (strict && !r.Has("country"))
            {
                throw new ContractException(where + ": falta 'country'.");
            }

            b.Country = LeagueContract.NullableString(r, "country", LeagueContract.IsCountry);
            b._updatedAtText = r.RequiredString("updated_at", LeagueContract.IsUtc);
            b.StartsAt = LeagueContract.Utc(b._startsAtText);
            b.EndsAt = LeagueContract.Utc(b._endsAtText);
            b.UpdatedAt = LeagueContract.Utc(b._updatedAtText);

            JsonNode top = r.RequiredArray("top", LeagueContract.MaxTop);
            var rows = new List<LeagueRowDto>(top.Items.Count);
            for (int i = 0; i < top.Items.Count; i++)
            {
                var x = new ObjectReader(top.Items[i], where + ".top", strict);
                int rank = (int)x.RequiredInteger("rank", 1, strict ? LeagueContract.MaxTop : 1000000);
                string key = x.RequiredString("name_key", nameKey);
                int num = (int)x.RequiredInteger("name_num", minNum, LeagueContract.MaxNameNum);
                string? country = LeagueContract.NullableString(x, "country", LeagueContract.IsCountry);
                int points = (int)x.RequiredInteger("points", strict ? 1 : 0, maxPoints);
                x.End();
                rows.Add(new LeagueRowDto(rank, key, num, country, points));
            }

            b.Top = rows;
            var me = new ObjectReader(r.Required("me"), where + ".me", strict);
            b.MePoints = (int)me.RequiredInteger("points", 0, maxPoints);
            b.MeRank = (int?)LeagueContract.NullableInteger(me.Optional("rank"), where + ".me", "rank", 1, strict ? LeagueContract.MaxTop : 1000000);
            b.MePercentile = (int?)LeagueContract.NullableInteger(me.Optional("percentile"), where + ".me", "percentile", 1, 100);
            b.MeNameKey = strict ? me.RequiredString("name_key", nameKey) : LeagueContract.NullableString(me, "name_key", nameKey);
            b.MeNameNum = (int)(strict ? me.RequiredInteger("name_num", minNum, LeagueContract.MaxNameNum) : me.OptionalInteger("name_num", 0, LeagueContract.MaxNameNum) ?? 0);
            if (strict && (!me.Has("rank") || !me.Has("percentile") || !me.Has("country")))
            {
                throw new ContractException(where + ".me: faltan 'rank', 'percentile' o 'country'.");
            }

            b.MeCountry = LeagueContract.NullableString(me, "country", LeagueContract.IsCountry);
            me.End();
            r.End();
            return b;
        }
    }
}
