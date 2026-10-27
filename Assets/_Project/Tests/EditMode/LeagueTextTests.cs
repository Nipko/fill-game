#nullable enable
using System;
using System.Collections.Generic;
using AF.Backend;
using AF.Backend.Contract;
using AF.LiveOps;
using AF.Localization;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Textos de la Liga del río (D-046; liga.md §1 y §4): las claves de <see cref="LeagueText"/> están en <c>strings.csv</c> en es,
    /// en y pt-BR; los 24 animales nacen <c>pendiente_asesor</c>; el seudónimo y su respaldo «Jugador {n}»; los países; los plazos y la
    /// línea de la victoria con su plural; y el adaptador del API real (<see cref="LeagueBackendApi"/>) con los ejemplos del contrato.
    /// </summary>
    public sealed class LeagueTextTests
    {
        private static readonly string[] Locales = { "es-419", "en", "pt-BR" };

        [TearDown]
        public void TearDown()
        {
            LocSetUpFixture.ConfigureEs419();
        }

        private static IEnumerable<string> UiKeys()
        {
            yield return LeagueText.ButtonKey;
            yield return LeagueText.TitleKey;
            yield return LeagueText.TabGlobalKey;
            yield return LeagueText.WeekKey;
            yield return LeagueText.EndsInKey;
            yield return LeagueText.UpdatedAgoKey;
            yield return LeagueText.OfflineKey;
            yield return LeagueText.LoadingKey;
            yield return LeagueText.EmptyWeekKey;
            yield return LeagueText.YouKey;
            yield return LeagueText.PercentileKey;
            yield return LeagueText.NoPointsKey;
            yield return LeagueText.VictoryPointsKey;
            yield return LeagueText.SettingsCountryKey;
            yield return LeagueText.PlayerNameKey;
            yield return LeagueText.PlayerFallbackKey;
            yield return LeagueText.RowA11yKey;
            yield return LeagueText.RowPlayerKey;
            yield return LeagueText.MeRankA11yKey;
            yield return LeagueText.MePercentileA11yKey;
            yield return LeagueText.PointsKey;
            yield return LeagueText.TimeDaysKey;
            yield return LeagueText.TimeHoursKey;
            yield return LeagueText.TimeMinutesKey;
            yield return LeagueText.TimeNowKey;
        }

        [Test]
        public void EveryKey_HasItsTextInTheThreeLanguages()
        {
            var keys = new List<string>(UiKeys());
            keys.AddRange(LeagueRules.NameKeys);
            keys.Add(LeagueText.CountryOtherKey);
            foreach (string code in LeagueText.NamedCountries)
            {
                keys.Add(LeagueText.CountryKey(code));
            }

            foreach (string locale in Locales)
            {
                Loc.Configure(LocSetUpFixture.Catalog, locale, true);
                foreach (string key in keys)
                {
                    Assert.That(Loc.Has(key), Is.True, key);
                    Assert.That(Loc.T(key, LocArgs.Of("n", 3).And("p", 12).And("time", "x")), Is.Not.Empty, locale + " " + key);
                }
            }
        }

        [Test]
        public void TheAnimals_ArePendingTheCulturalAdvisor()
        {
            foreach (string key in LeagueRules.NameKeys)
            {
                Assert.That(Loc.StatusOf(key, "es-419"), Is.EqualTo(LocStatus.PendingAdvisor).Or.EqualTo(LocStatus.ApprovedEs), key + ": pendiente del asesor o revisado en D-054");
            }
        }

        [Test]
        public void TheCountries_ArePendingTheCulturalAdvisor()
        {
            // Lugares (localizacion.md: lo cultural nace pendiente_asesor), también «Otro país».
            foreach (string code in LeagueText.NamedCountries)
            {
                Assert.That(Loc.StatusOf(LeagueText.CountryKey(code), "es-419"), Is.EqualTo(LocStatus.PendingAdvisor).Or.EqualTo(LocStatus.ApprovedEs), code);
            }

            Assert.That(Loc.StatusOf(LeagueText.CountryOtherKey, "es-419"), Is.EqualTo(LocStatus.PendingAdvisor).Or.EqualTo(LocStatus.ApprovedEs));
            Assert.That(LeagueText.NamedCountries.Length, Is.EqualTo(21));
        }

        [Test]
        public void ThePseudonym_IsTheAnimalAndItsDigits_OrPlayerN()
        {
            Assert.That(LeagueText.Player("league_name_capybara", 4821), Is.EqualTo("Capibara 4821"), "sin separador de miles");
            Assert.That(LeagueText.Player("league_name_quetzal", 77), Is.EqualTo("Jugador 77"), "una clave que el cliente no conoce");
            Assert.That(LeagueText.Player(null, 5), Is.EqualTo("Jugador 5"));
            Loc.Configure(LocSetUpFixture.Catalog, "pt-BR", true);
            Assert.That(LeagueText.Player("league_name_jaguar", 1203), Is.EqualTo("Onça 1203"));
        }

        [Test]
        public void TheCountries_HaveTheirNames_AndTheRestTheirCode()
        {
            Assert.That(LeagueText.Country("CO"), Is.EqualTo("Colombia"));
            Assert.That(LeagueText.Country("BR"), Is.EqualTo("Brasil"));
            Assert.That(LeagueText.Country("JP"), Is.EqualTo("Otro país"), "sin fila, «Otro país» (el código ISO va en la placa neutra)");
            Assert.That(LeagueText.CountryKey("GF"), Is.EqualTo("league_country_gf"));
            foreach (string flag in LeagueText.FlagCountries)
            {
                Assert.That(Array.IndexOf(LeagueText.NamedCountries, flag), Is.GreaterThanOrEqualTo(0), "toda bandera lleva su nombre: " + flag);
            }

            Assert.That(LeagueText.SettingsCountry("PE"), Is.EqualTo("País en la Liga: Perú"));
        }

        [Test]
        public void TheDurations_AndTheVictoryLine()
        {
            Assert.That(LeagueText.Duration(TimeSpan.FromSeconds(30)), Is.EqualTo("un momento"));
            Assert.That(LeagueText.Duration(TimeSpan.FromMinutes(8)), Is.EqualTo("8 min"));
            Assert.That(LeagueText.Duration(new TimeSpan(5, 12, 0)), Is.EqualTo("5 h 12 min"));
            Assert.That(LeagueText.Duration(new TimeSpan(2, 3, 40, 0)), Is.EqualTo("2 d 3 h"));
            Assert.That(LeagueText.EndsIn(new TimeSpan(2, 3, 0, 0)), Is.EqualTo("Termina en 2 d 3 h"));
            Assert.That(LeagueText.VictoryPoints(20), Is.EqualTo("+20 puntos de Liga"));
            Loc.Configure(LocSetUpFixture.Catalog, "en", true);
            Assert.That(LeagueText.VictoryPoints(10), Is.EqualTo("+10 League points"));
            Assert.That(LeagueText.Percentile(12), Is.EqualTo("You're in the top 12%"));
        }

        [Test]
        public void TheRowReading_SaysRankPseudonymCountryAndPoints()
        {
            var row = new LeagueRow(3, "league_name_jaguar", 1203, "CO", 980);
            Assert.That(LeagueText.RowA11y(row, true), Is.EqualTo("Puesto 3: Jaguar 1203, Colombia, 980 puntos"));
            Assert.That(LeagueText.RowA11y(row, false), Is.EqualTo("Puesto 3: Jaguar 1203, 980 puntos"), "en la pestaña del país, sin repetirlo");
            var me = new LeagueMe(140, null, 12, "league_name_capybara", 4821, "CO");
            Assert.That(LeagueText.MeA11y(me, false), Is.EqualTo("Tú, Capibara 4821: estás en el 12 % mejor, 140 puntos"));
        }

        [Test]
        public void TheRealApiAdapter_MapsTheContractExamples()
        {
            const string wins = "{ \"week\": \"2026-W43\", \"points\": 140, \"accepted\": [\"c:57\"], \"rejected\": [ { \"win_id\": \"d:2026-10-23\", \"reason\": \"duplicate\" } ], \"name_key\": \"league_name_capybara\", \"name_num\": 4821, \"country\": \"CO\" }";
            Assert.That(LeagueWinsResponseDto.TryParse(wins, out LeagueWinsResponseDto? dto, out _), Is.True);
            LeagueWinsResult result = LeagueBackendApi.FromDto(dto!);
            Assert.That(result.Accepted, Is.EqualTo(new[] { "c:57" }));
            Assert.That(result.Rejected[0].Reason, Is.EqualTo("duplicate"));
            Assert.That(result.Country, Is.EqualTo("CO"));

            var request = new LeagueWinsRequest(new[]
            {
                LeagueWin.Campaign(57, new DateTime(2026, 10, 23, 15, 4, 5, DateTimeKind.Utc)),
                LeagueWin.Daily("2026-10-23", new DateTime(2026, 10, 23, 16, 0, 0, DateTimeKind.Utc)),
            }, 58, "co");
            Assert.That(LeagueBackendApi.ToDto(request).ToJson(), Is.EqualTo(
                "{\"wins\":[{\"win_id\":\"c:57\",\"kind\":\"campaign\",\"level\":57,\"won_at\":\"2026-10-23T15:04:05Z\"},"
                + "{\"win_id\":\"d:2026-10-23\",\"kind\":\"daily\",\"day\":\"2026-10-23\",\"won_at\":\"2026-10-23T16:00:00Z\"}],\"max_level\":58,\"region\":\"CO\"}"),
                "la petición de ejemplo de liga.md §3.1");

            Assert.That(LeagueBackendApi.FailureOf(BackendError.Network("x"), false), Is.EqualTo(LeagueFailure.Offline));
            Assert.That(LeagueBackendApi.FailureOf(BackendError.Http(404, ApiErrorCode.Unknown), true), Is.EqualTo(LeagueFailure.CountryUnknown));
            Assert.That(LeagueBackendApi.FailureOf(BackendError.Http(500, ApiErrorCode.Unknown), false), Is.EqualTo(LeagueFailure.Server));
        }

        /// <summary>
        /// D-056 (avatares.md §4.4): el envío del avatar descarta el pendiente solo con un 400; sin red, con un 404 de una ruta aún sin
        /// desplegar o con un 429, se queda para la siguiente apertura. Un índice fuera de 0-63 no llega al backend.
        /// </summary>
        [Test]
        public void TheAvatarFailures_OnlyA400Rejects_AndAnOutOfRangeIndexNeverLeaves()
        {
            Assert.That(LeagueBackendApi.AvatarFailureOf(BackendError.Http(400, ApiErrorCode.BadRequest)), Is.EqualTo(LeagueFailure.Rejected));
            Assert.That(LeagueBackendApi.AvatarFailureOf(BackendError.Http(404, ApiErrorCode.Unknown)), Is.Not.EqualTo(LeagueFailure.Rejected));
            Assert.That(LeagueBackendApi.AvatarFailureOf(BackendError.Http(429, ApiErrorCode.RateLimited)), Is.Not.EqualTo(LeagueFailure.Rejected));
            Assert.That(LeagueBackendApi.AvatarFailureOf(BackendError.Network("x")), Is.EqualTo(LeagueFailure.Offline));

            var backend = new CountingLeagueBackend();
            var api = new LeagueBackendApi(backend, null);
            foreach (int avatar in new[] { 64, -1 })
            {
                LeagueResult<LeagueAvatarResult>? result = null;
                api.SetAvatar(avatar, r => result = r);
                Assert.That(result, Is.Not.Null, "responde en el acto");
                Assert.That(result!.IsOk, Is.False);
                Assert.That(result.Failure, Is.EqualTo(LeagueFailure.Rejected), avatar + ": fuera de 0-63");
            }

            Assert.That(backend.Calls, Is.EqualTo(0), "sin llamar al backend");
        }

        /// <summary>Un <see cref="ILeagueBackend"/> que solo cuenta las llamadas (nunca responde).</summary>
        private sealed class CountingLeagueBackend : ILeagueBackend
        {
            public int Calls { get; private set; }

            public void SendLeagueWins(LeagueWinsRequestDto request, Action<BackendResult<LeagueWinsResponseDto>> onDone) => Calls++;

            public void GetLeagueBoard(string scope, Action<BackendResult<LeagueBoardResponseDto>> onDone) => Calls++;

            public void SetLeagueAvatar(LeagueAvatarRequestDto request, Action<BackendResult<LeagueAvatarResponseDto>> onDone) => Calls++;
        }
    }
}
