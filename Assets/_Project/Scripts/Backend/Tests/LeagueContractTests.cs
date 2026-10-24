#nullable enable
using System;
using AF.Backend.Auth;
using AF.Backend.Contract;
using AF.Backend.Http;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>
    /// La Liga del río (D-046) en <c>AF.Backend</c>, con los ejemplos de <c>infra/contracts/v1/examples</c> (leídos del repositorio,
    /// como el resto de F3-1; la ida y vuelta de todos la hace <c>ContractExamplesTests</c>) y <see cref="HttpBackendClient"/> con sus
    /// dos rutas.
    /// </summary>
    public sealed class LeagueContractTests
    {
        private const string BaseUrl = "https://api.example.test/dev";

        private static string Example(string name) => BackendTestFiles.ReadExample(name);

        [Test]
        public void TheRequestExample_ParsesStrictly_AndIsWhatTheClientWrites()
        {
            Assert.That(LeagueWinsRequestDto.TryParseStrict(Example("league-wins.request.json"), out LeagueWinsRequestDto? req, out string? error), Is.True, error);
            Assert.That(req!.Wins.Count, Is.EqualTo(2));
            Assert.That(req.Wins[0].WinId, Is.EqualTo("c:57"));
            Assert.That(req.Wins[0].Level, Is.EqualTo(57));
            Assert.That(req.Wins[1].Day, Is.EqualTo("2026-10-23"));
            Assert.That(req.Wins[1].WonAt, Is.EqualTo(new DateTime(2026, 10, 23, 16, 0, 0, DateTimeKind.Utc)));
            Assert.That(req.MaxLevel, Is.EqualTo(58));
            Assert.That(req.Region, Is.EqualTo("CO"));

            // El cliente escribe lo mismo desde sus tipos.
            var built = new LeagueWinsRequestDto(new[]
            {
                new LeagueWinDto("c:57", "campaign", 57, null, new DateTime(2026, 10, 23, 15, 4, 5, DateTimeKind.Utc)),
                new LeagueWinDto("d:2026-10-23", "daily", null, "2026-10-23", new DateTime(2026, 10, 23, 16, 0, 0, DateTimeKind.Utc)),
            }, 58, "CO");
            Assert.That(JsonEqual(built.ToJson(), Example("league-wins.request.json")), Is.True, built.ToJson());
        }

        [Test]
        public void TheMixedRequest_IsValidForTheSchema_ButNotWhatTheClientWrites()
        {
            // Solo el sobre da 400: un elemento null, otro sin campos y el 701 van como invalid en la respuesta (liga.md §3.2).
            string mixed = Example("league-wins.request.mixed.json");
            Assert.That(LeagueWinsRequestDto.TryParse(mixed, out LeagueWinsRequestDto? req, out string? error), Is.True, error);
            Assert.That(req!.ItemCount, Is.EqualTo(5));
            Assert.That(req.Wins.Count, Is.EqualTo(3), "c:42, c:701 y c:41 están bien formadas");
            Assert.That(req.Region, Is.Null, "region null: país desconocido");
            Assert.That(LeagueWinsRequestDto.TryParseStrict(mixed, out _, out _), Is.False, "el cliente nunca manda elementos mal formados");
            Assert.That(LeagueWinsRequestDto.TryParse(Example("league-wins.request.invalid-too-many.json"), out _, out _), Is.False, "más de 50");
        }

        [Test]
        public void TheStrictRequest_RejectsWhatTheClientNeverWrites()
        {
            Assert.That(LeagueWinsRequestDto.TryParseStrict("{\"wins\":[],\"max_level\":1}", out _, out _), Is.False, "sin victorias");
            Assert.That(LeagueWinsRequestDto.TryParseStrict(
                "{\"wins\":[{\"win_id\":\"c:1\",\"kind\":\"campaign\",\"level\":1,\"won_at\":\"2026-10-23T15:04:05Z\",\"points\":10}],\"max_level\":1}", out _, out _),
                Is.False, "el cliente nunca manda puntos (liga.md §3.2)");
            Assert.That(LeagueWinsRequestDto.TryParseStrict(
                "{\"wins\":[{\"win_id\":\"c:1\",\"kind\":\"campaign\",\"level\":1,\"won_at\":\"2026-10-23T15:04:05Z\"}],\"max_level\":1,\"region\":\"col\"}", out _, out _), Is.False);
            Assert.That(LeagueWinsRequestDto.TryParse("{\"wins\":[{}],\"max_level\":1,\"extra\":1}", out _, out _), Is.False, "el sobre no admite claves nuevas");
            Assert.Throws<ArgumentException>(() => new LeagueWinsRequestDto(new LeagueWinDto[51], 1, null), "como mucho 50");
            Assert.Throws<ArgumentException>(() => new LeagueWinDto("c:5", "daily", 5, null, DateTime.UtcNow));
        }

        [Test]
        public void TheRequest_WithoutRegion_OmitsTheField()
        {
            var req = new LeagueWinsRequestDto(new[] { new LeagueWinDto("c:41", "campaign", 41, null, new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Utc)) }, 41, null);
            Assert.That(req.ToJson(), Does.Not.Contain("region"));
        }

        [Test]
        public void TheResponseExamples_Parse()
        {
            Assert.That(LeagueWinsResponseDto.TryParseStrict(Example("league-wins.response.json"), out LeagueWinsResponseDto? wins, out string? error), Is.True, error);
            Assert.That(wins!.Week, Is.EqualTo("2026-W43"));
            Assert.That(wins.Points, Is.EqualTo(140));
            Assert.That(wins.Accepted, Is.EqualTo(new[] { "c:57" }));
            Assert.That(wins.Rejected[0].Key, Is.EqualTo("d:2026-10-23"));
            Assert.That(wins.Rejected[0].Value, Is.EqualTo("duplicate"));
            Assert.That(wins.NameNum, Is.EqualTo(4821));
            Assert.That(wins.Country, Is.EqualTo("CO"));

            Assert.That(LeagueBoardResponseDto.TryParseStrict(Example("league-board.response.json"), out LeagueBoardResponseDto? board, out error), Is.True, error);
            Assert.That(board!.Scope, Is.EqualTo("country"));
            Assert.That(board.StartsAt, Is.EqualTo(new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc)));
            Assert.That(board.Top[0].Points, Is.EqualTo(980));
            Assert.That(board.MeRank, Is.Null);
            Assert.That(board.MePercentile, Is.EqualTo(12));
        }

        [Test]
        public void TheMixedResponse_WithAnEmptyWinId_Parses()
        {
            // Un elemento mal formado vuelve en rejected con win_id «» (liga.md §3.1): no rompe la respuesta.
            Assert.That(LeagueWinsResponseDto.TryParseStrict(Example("league-wins.response.mixed.json"), out LeagueWinsResponseDto? dto, out string? error), Is.True, error);
            Assert.That(dto!.Accepted, Is.EqualTo(new[] { "c:41", "c:42" }));
            Assert.That(dto.Rejected.Count, Is.EqualTo(3));
            Assert.That(dto.Rejected[0].Key, Is.Empty);
            Assert.That(dto.Country, Is.Null);
            Assert.That(LeagueWinsResponseDto.TryParse(Example("league-wins.response.mixed.json"), out _, out error), Is.True, error);
        }

        [Test]
        public void TheStrictReading_HasTheSchemaLimits_AndTheTolerantOneDoesNot()
        {
            string wins = Example("league-wins.response.json");
            Assert.That(LeagueWinsResponseDto.TryParseStrict(wins.Replace("4821", "999"), out _, out _), Is.False, "name_num ≥ 1000");
            Assert.That(LeagueWinsResponseDto.TryParseStrict(wins.Replace("\"points\": 140", "\"points\": 1000000"), out _, out _), Is.False, "points ≤ 999.999");
            Assert.That(LeagueWinsResponseDto.TryParseStrict(wins.Replace("league_name_capybara", "league_name_quetzal"), out _, out _), Is.False, "uno de los 24");
            Assert.That(LeagueWinsResponseDto.TryParse(wins.Replace("league_name_capybara", "league_name_quetzal"), out _, out string? error), Is.True,
                "el cliente tolera un animal más nuevo («Jugador {n}»): " + error);

            string board = Example("league-board.response.json");
            Assert.That(LeagueBoardResponseDto.TryParseStrict(board.Replace("\"rank\": 1,", "\"rank\": 101,"), out _, out _), Is.False, "rank ≤ 100");
            Assert.That(LeagueBoardResponseDto.TryParseStrict(board.Replace("\"points\": 980", "\"points\": 0"), out _, out _), Is.False, "top[].points ≥ 1");
            Assert.That(LeagueBoardResponseDto.TryParseStrict(board.Replace("\"percentile\": 12", "\"percentile\": 101"), out _, out _), Is.False);
            Assert.That(LeagueBoardResponseDto.TryParseStrict(board.Replace("1203", "120"), out _, out _), Is.False, "name_num ≥ 1000");
            Assert.That(LeagueBoardResponseDto.TryParse(board.Replace("2026-W43", "W43"), out _, out _), Is.False);
            Assert.That(LeagueWinsResponseDto.TryParse("{}", out _, out _), Is.False);
        }

        [Test]
        public void TheClient_PostsTheWins_WithIdTokenAndAppCheck()
        {
            var http = new FakeHttpTransport();
            var auth = new FakeAuthTokenProvider();
            var client = new HttpBackendClient(BaseUrl, http, auth, new FakeIntegrityTokenProvider(), new ManualBackendScheduler());
            http.Enqueue(200, Example("league-wins.response.json"));
            Assert.That(LeagueWinsRequestDto.TryParseStrict(Example("league-wins.request.json"), out LeagueWinsRequestDto? req, out _), Is.True);
            BackendResult<LeagueWinsResponseDto>? result = null;
            client.SendLeagueWins(req!, r => result = r);

            Assert.That(result!.IsOk, Is.True, result.ToString());
            HttpRequestData sent = http.Requests[0];
            Assert.That(sent.Method, Is.EqualTo("POST"));
            Assert.That(sent.Url, Is.EqualTo(BaseUrl + "/v1/league/wins"));
            Assert.That(sent.GetHeader("Authorization"), Is.EqualTo("Bearer " + auth.CurrentToken));
            Assert.That(sent.GetHeader("X-Firebase-AppCheck"), Is.Not.Null.And.Not.Empty, "el mismo autorizador: App Check (liga.md §3.1)");
            Assert.That(LeagueWinsRequestDto.TryParseStrict(sent.Body, out _, out string? error), Is.True, error);
        }

        [Test]
        public void TheClient_GetsTheBoard_ByScope()
        {
            var http = new FakeHttpTransport();
            var client = new HttpBackendClient(BaseUrl, http, new FakeAuthTokenProvider(), new FakeIntegrityTokenProvider(), new ManualBackendScheduler());
            http.Enqueue(200, Example("league-board.response.json"));
            BackendResult<LeagueBoardResponseDto>? result = null;
            client.GetLeagueBoard("country", r => result = r);

            Assert.That(result!.IsOk, Is.True, result.ToString());
            HttpRequestData sent = http.Requests[0];
            Assert.That(sent.Method, Is.EqualTo("GET"));
            Assert.That(sent.Url, Is.EqualTo(BaseUrl + "/v1/league?scope=country"));
            Assert.That(sent.Body, Is.Null);
            Assert.That(sent.GetHeader("X-Firebase-AppCheck"), Is.Not.Null.And.Not.Empty);
            Assert.Throws<ArgumentException>(() => client.GetLeagueBoard("bucket", _ => { }));
        }

        [Test]
        public void TheClient_ReportsTheCountryUnknown404()
        {
            var http = new FakeHttpTransport();
            var client = new HttpBackendClient(BaseUrl, http, new FakeAuthTokenProvider(), new FakeIntegrityTokenProvider(), new ManualBackendScheduler());
            http.Enqueue(404, "{\"error\":{\"code\":\"country_unknown\",\"message\":\"no country\"}}");
            BackendResult<LeagueBoardResponseDto>? result = null;
            client.GetLeagueBoard("country", r => result = r);
            Assert.That(result!.IsOk, Is.False);
            Assert.That(result.Error!.HttpStatus, Is.EqualTo(404));
            Assert.That(result.Error.Code, Is.EqualTo(ApiErrorCode.CountryUnknown));
        }

        [Test]
        public void TheLeagueOperations_AreAppOperations()
        {
            Assert.That(BackendOperation.LeagueWins.Path, Is.EqualTo("/v1/league/wins"));
            Assert.That(BackendOperation.LeagueBoard.Method, Is.EqualTo("GET"));
            Assert.That(Array.IndexOf(BackendOperation.AppOperations, BackendOperation.LeagueWins), Is.GreaterThanOrEqualTo(0));
            Assert.That(Array.IndexOf(BackendOperation.AppOperations, BackendOperation.LeagueBoard), Is.GreaterThanOrEqualTo(0));
        }

        private static bool JsonEqual(string a, string b) =>
            AF.Backend.Json.JsonNode.StructurallyEqual(AF.Backend.Json.BackendJson.Parse(a), AF.Backend.Json.BackendJson.Parse(b));
    }
}
