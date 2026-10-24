#nullable enable
using System;
using AF.Backend.Auth;
using AF.Backend.Contract;
using AF.Backend.Http;
using AF.Platform;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>
    /// La Liga del río (D-046) en <c>AF.Backend</c>: los JSON de ejemplo de <c>docs/dev/liga.md</c> §3.1 (copiados aquí porque los
    /// esquemas de <c>infra/contracts/v1</c> los añade el backend) y <see cref="HttpBackendClient"/> con sus dos rutas.
    /// </summary>
    public sealed class LeagueContractTests
    {
        // liga.md §3.1, tal cual.
        internal const string WinsRequestExample =
            "{ \"wins\": [ { \"win_id\": \"c:57\", \"kind\": \"campaign\", \"level\": 57, \"won_at\": \"2026-10-26T15:04:05Z\" },\n"
            + "            { \"win_id\": \"d:2026-10-26\", \"kind\": \"daily\", \"day\": \"2026-10-26\", \"won_at\": \"2026-10-26T16:00:00Z\" } ],\n"
            + "  \"max_level\": 58, \"region\": \"CO\" }";

        internal const string WinsResponseExample =
            "{ \"week\": \"2026-W43\", \"points\": 140, \"accepted\": [\"c:57\"], \"rejected\": [ { \"win_id\": \"d:2026-10-26\", \"reason\": \"duplicate\" } ],\n"
            + "  \"name_key\": \"league_name_capybara\", \"name_num\": 4821, \"country\": \"CO\" }";

        internal const string BoardResponseExample =
            "{ \"week\": \"2026-W43\", \"starts_at\": \"2026-10-19T12:00:00Z\", \"ends_at\": \"2026-10-26T12:00:00Z\", \"scope\": \"country\", \"country\": \"CO\",\n"
            + "  \"updated_at\": \"2026-10-23T20:00:00Z\",\n"
            + "  \"top\": [ { \"rank\": 1, \"name_key\": \"league_name_jaguar\", \"name_num\": 1203, \"country\": \"CO\", \"points\": 980 } ],\n"
            + "  \"me\": { \"points\": 140, \"rank\": null, \"percentile\": 12, \"name_key\": \"league_name_capybara\", \"name_num\": 4821, \"country\": \"CO\" } }";

        private const string BaseUrl = "https://api.example.test/dev";

        [Test]
        public void TheRequestExample_ParsesStrictly_AndRoundTrips()
        {
            Assert.That(LeagueWinsRequestDto.TryParse(WinsRequestExample, out LeagueWinsRequestDto? req, out string? error), Is.True, error);
            Assert.That(req!.Wins.Count, Is.EqualTo(2));
            Assert.That(req.Wins[0].WinId, Is.EqualTo("c:57"));
            Assert.That(req.Wins[0].Level, Is.EqualTo(57));
            Assert.That(req.Wins[1].Day, Is.EqualTo("2026-10-26"));
            Assert.That(req.Wins[1].WonAt, Is.EqualTo(new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc)));
            Assert.That(req.MaxLevel, Is.EqualTo(58));
            Assert.That(req.Region, Is.EqualTo("CO"));

            string json = req.ToJson();
            Assert.That(json, Is.EqualTo(
                "{\"wins\":[{\"win_id\":\"c:57\",\"kind\":\"campaign\",\"level\":57,\"won_at\":\"2026-10-26T15:04:05Z\"},"
                + "{\"win_id\":\"d:2026-10-26\",\"kind\":\"daily\",\"day\":\"2026-10-26\",\"won_at\":\"2026-10-26T16:00:00Z\"}],"
                + "\"max_level\":58,\"region\":\"CO\"}"));
            Assert.That(LeagueWinsRequestDto.TryParse(json, out _, out error), Is.True, error);
        }

        [Test]
        public void TheRequest_RejectsWhatTheContractForbids()
        {
            Assert.That(LeagueWinsRequestDto.TryParse("{\"wins\":[],\"max_level\":1}", out _, out _), Is.False, "sin victorias");
            Assert.That(LeagueWinsRequestDto.TryParse(
                "{\"wins\":[{\"win_id\":\"c:1\",\"kind\":\"campaign\",\"level\":1,\"won_at\":\"2026-10-26T15:04:05Z\",\"points\":10}],\"max_level\":1}", out _, out _),
                Is.False, "el cliente nunca manda puntos (liga.md §3.2)");
            Assert.That(LeagueWinsRequestDto.TryParse(
                "{\"wins\":[{\"win_id\":\"x:1\",\"kind\":\"campaign\",\"level\":1,\"won_at\":\"2026-10-26T15:04:05Z\"}],\"max_level\":1}", out _, out _), Is.False);
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
            Assert.That(LeagueWinsResponseDto.TryParseStrict(WinsResponseExample, out LeagueWinsResponseDto? wins, out string? error), Is.True, error);
            Assert.That(wins!.Week, Is.EqualTo("2026-W43"));
            Assert.That(wins.Points, Is.EqualTo(140));
            Assert.That(wins.Accepted, Is.EqualTo(new[] { "c:57" }));
            Assert.That(wins.Rejected[0].Key, Is.EqualTo("d:2026-10-26"));
            Assert.That(wins.Rejected[0].Value, Is.EqualTo("duplicate"));
            Assert.That(wins.NameKey, Is.EqualTo("league_name_capybara"));
            Assert.That(wins.NameNum, Is.EqualTo(4821));
            Assert.That(wins.Country, Is.EqualTo("CO"));

            Assert.That(LeagueBoardResponseDto.TryParseStrict(BoardResponseExample, out LeagueBoardResponseDto? board, out error), Is.True, error);
            Assert.That(board!.Scope, Is.EqualTo("country"));
            Assert.That(board.StartsAt, Is.EqualTo(new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc)));
            Assert.That(board.Top.Count, Is.EqualTo(1));
            Assert.That(board.Top[0].Points, Is.EqualTo(980));
            Assert.That(board.MeRank, Is.Null);
            Assert.That(board.MePercentile, Is.EqualTo(12));
        }

        [Test]
        public void TheResponses_AreTolerant_WithNewFieldsAndNulls_ButNotWithBrokenOnes()
        {
            string withExtra = WinsResponseExample.Replace("\"country\": \"CO\" }", "\"country\": null, \"flagged\": false }");
            Assert.That(LeagueWinsResponseDto.TryParse(withExtra, out LeagueWinsResponseDto? wins, out string? error), Is.True, error);
            Assert.That(wins!.Country, Is.Null);
            Assert.That(LeagueWinsResponseDto.TryParseStrict(withExtra, out _, out _), Is.False, "estricto: sin claves nuevas");

            string noPoints = BoardResponseExample.Replace("\"points\": 140, \"rank\": null, \"percentile\": 12", "\"points\": 0, \"rank\": null, \"percentile\": null");
            Assert.That(LeagueBoardResponseDto.TryParse(noPoints, out LeagueBoardResponseDto? board, out error), Is.True, error);
            Assert.That(board!.MePercentile, Is.Null);

            Assert.That(LeagueBoardResponseDto.TryParse(BoardResponseExample.Replace("\"percentile\": 12", "\"percentile\": 101"), out _, out _), Is.False);
            Assert.That(LeagueBoardResponseDto.TryParse(BoardResponseExample.Replace("2026-W43", "W43"), out _, out _), Is.False);
            Assert.That(LeagueWinsResponseDto.TryParse("{}", out _, out _), Is.False);
        }

        [Test]
        public void TheClient_PostsTheWins_WithIdTokenAndAppCheck()
        {
            var http = new FakeHttpTransport();
            var auth = new FakeAuthTokenProvider();
            var integrity = new FakeIntegrityTokenProvider();
            var client = new HttpBackendClient(BaseUrl, http, auth, integrity, new ManualBackendScheduler());
            http.Enqueue(200, WinsResponseExample);
            Assert.That(LeagueWinsRequestDto.TryParse(WinsRequestExample, out LeagueWinsRequestDto? req, out _), Is.True);
            BackendResult<LeagueWinsResponseDto>? result = null;
            client.SendLeagueWins(req!, r => result = r);

            Assert.That(result!.IsOk, Is.True, result.ToString());
            HttpRequestData sent = http.Requests[0];
            Assert.That(sent.Method, Is.EqualTo("POST"));
            Assert.That(sent.Url, Is.EqualTo(BaseUrl + "/v1/league/wins"));
            Assert.That(sent.GetHeader("Authorization"), Is.EqualTo("Bearer " + auth.CurrentToken));
            Assert.That(sent.GetHeader("X-Firebase-AppCheck"), Is.Not.Null.And.Not.Empty, "el mismo autorizador: App Check (liga.md §3.1)");
            Assert.That(LeagueWinsRequestDto.TryParse(sent.Body, out _, out string? error), Is.True, error);
        }

        [Test]
        public void TheClient_GetsTheBoard_ByScope()
        {
            var http = new FakeHttpTransport();
            var client = new HttpBackendClient(BaseUrl, http, new FakeAuthTokenProvider(), new FakeIntegrityTokenProvider(), new ManualBackendScheduler());
            http.Enqueue(200, BoardResponseExample);
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
        }

        [Test]
        public void TheLeagueOperations_AreKeptApartFromTheIndexOnes()
        {
            Assert.That(BackendOperation.LeagueWins.Path, Is.EqualTo("/v1/league/wins"));
            Assert.That(BackendOperation.LeagueBoard.Method, Is.EqualTo("GET"));
            Assert.That(BackendOperation.LeagueWins.RequiresIntegrity && BackendOperation.LeagueBoard.RequiresIntegrity, Is.True);
            Assert.That(Array.IndexOf(BackendOperation.AppOperations, BackendOperation.LeagueWins), Is.EqualTo(-1),
                "entran en AppOperations cuando el backend las añada a index.json");
        }
    }
}
