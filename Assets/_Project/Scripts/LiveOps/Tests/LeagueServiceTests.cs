#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// La cola, el registro del perfil y el servicio de la Liga del río (D-046; liga.md §1 y §4): idempotencia por <c>win_id</c>, una
    /// vez por semana cada nivel y una por día el Reto, lotes de ≤ 50, la cola sin conexión y tras reiniciar (el JSON del perfil), el
    /// cambio de semana, la caché por ámbito con <c>live_league_cache_minutes</c> y el borrado.
    /// </summary>
    public sealed class LeagueServiceTests
    {
        private ManualClock _clock = null!;
        private InMemoryLeagueStore _store = null!;
        private FakeLeagueApi _api = null!;
        private LeagueConfig _config = null!;
        private int _maxLevel;
        private bool _allowed;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc));
            _store = new InMemoryLeagueStore();
            _api = new FakeLeagueApi(_clock);
            _config = new LeagueConfig { LiveLeagueEnabled = true };
            _maxLevel = 60;
            _allowed = true;
        }

        private LeagueService Service(ILeagueStore? store = null) =>
            new LeagueService(store ?? _store, _api, _clock, _config, () => _maxLevel, () => "co", () => _allowed);

        [Test]
        public void Queue_EnqueueIsIdempotent_AndWeekly()
        {
            var record = new LeagueRecord();
            DateTime now = _clock.UtcNow;
            LeagueQueue.Roll(record, now);
            Assert.That(LeagueQueue.Enqueue(record, LeagueWin.Campaign(57, now)), Is.True);
            Assert.That(LeagueQueue.Enqueue(record, LeagueWin.Campaign(57, now)), Is.False, "el mismo nivel no suma dos veces en la semana");
            Assert.That(LeagueQueue.Enqueue(record, LeagueWin.Daily("2026-10-23", now)), Is.True);
            Assert.That(LeagueQueue.Enqueue(record, LeagueWin.Daily("2026-10-23", now)), Is.False, "el Reto, una vez por día");
            Assert.That(LeagueQueue.Enqueue(record, LeagueWin.Daily("2026-10-24", now)), Is.True);
            Assert.That(record.Pending.Count, Is.EqualTo(3));

            // Lunes 12:00 UTC: la semana nueva vuelve a contar el 57; lo pendiente de la anterior se descarta.
            DateTime monday = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            Assert.That(LeagueQueue.Roll(record, monday), Is.True);
            Assert.That(record.Pending, Is.Empty, "el servidor lo rechazaría con outside_week");
            Assert.That(record.Counted, Is.Empty);
            Assert.That(LeagueQueue.Enqueue(record, LeagueWin.Campaign(57, monday)), Is.True);
        }

        [Test]
        public void Queue_BatchesOf50_AndApplyRemovesAcceptedAndRejectedOnly()
        {
            var record = new LeagueRecord();
            DateTime now = _clock.UtcNow;
            LeagueQueue.Roll(record, now);
            for (int level = 1; level <= 120; level++)
            {
                LeagueQueue.Enqueue(record, LeagueWin.Campaign(level, now));
            }

            List<LeagueWin> batch = LeagueQueue.NextBatch(record);
            Assert.That(batch.Count, Is.EqualTo(50));
            Assert.That(batch[0].WinId, Is.EqualTo("c:1"));
            var result = new LeagueWinsResult("2026-W43", 300, new[] { "c:1", "c:2" }, new[] { new LeagueRejection("c:3", "rate_limited") },
                "league_name_otter", 77, "PE");
            int removed = LeagueQueue.Apply(record, batch, result);
            Assert.That(removed, Is.EqualTo(3), "lo que el servidor no nombra se queda para el próximo envío");
            Assert.That(record.Pending.Count, Is.EqualTo(117));
            Assert.That(record.MePoints, Is.EqualTo(300));
            Assert.That(record.MeCountry, Is.EqualTo("PE"));
        }

        [Test]
        public void Queue_MatchesTheAnswerByWinId_NeverByPosition()
        {
            // liga.md §3.1 (revisión del 2026-10-24): el servidor procesa las campañas por nivel, así que el orden de accepted no es el
            // de la petición, y un elemento mal formado vuelve con win_id "".
            var record = new LeagueRecord();
            DateTime now = _clock.UtcNow;
            LeagueQueue.Roll(record, now);
            LeagueQueue.Enqueue(record, LeagueWin.Campaign(42, now));
            LeagueQueue.Enqueue(record, LeagueWin.Campaign(41, now));
            LeagueQueue.Enqueue(record, LeagueWin.Campaign(701, now));
            LeagueQueue.Enqueue(record, LeagueWin.Campaign(43, now));
            List<LeagueWin> batch = LeagueQueue.NextBatch(record);
            var result = new LeagueWinsResult("2026-W43", 20, new[] { "c:41", "c:42" },
                new[] { new LeagueRejection(string.Empty, "invalid"), new LeagueRejection("bad", "invalid"), new LeagueRejection("c:701", "invalid") },
                "league_name_capybara", 1000, null);
            Assert.That(LeagueQueue.Apply(record, batch, result), Is.EqualTo(3));
            Assert.That(record.Pending.Count, Is.EqualTo(1));
            Assert.That(record.Pending[0].WinId, Is.EqualTo("c:43"), "lo que el servidor no nombra se queda");
            Assert.That(record.MeCountry, Is.Null);
        }

        [Test]
        public void Record_RoundTripsThroughTheProfileJson_AndIsTolerant()
        {
            DateTime now = _clock.UtcNow;
            var record = new LeagueRecord();
            LeagueQueue.Roll(record, now);
            LeagueQueue.Enqueue(record, LeagueWin.Campaign(41, now));
            LeagueQueue.Enqueue(record, LeagueWin.Daily("2026-10-23", now));
            record.MeWeek = "2026-W43";
            record.MePoints = 30;
            record.MeNameKey = "league_name_capybara";
            record.MeNameNum = 4821;
            record.MeCountry = "CO";
            record.CacheGlobal = new LeagueCacheEntry(_api.Board(LeagueScope.Global), now);

            string json = LeagueRecordJson.Write(record);
            LeagueRecord back = LeagueRecordJson.Read(json);
            Assert.That(LeagueRecordJson.Write(back), Is.EqualTo(json), "ida y vuelta exacta");
            Assert.That(back.Pending.Count, Is.EqualTo(2));
            Assert.That(back.Pending[1].Day, Is.EqualTo("2026-10-23"));
            Assert.That(back.CacheGlobal!.Board.Top.Count, Is.EqualTo(100));
            Assert.That(back.CacheGlobal.Board.Top[37].NameKey, Is.EqualTo("league_name_unknown_future"));

            Assert.That(LeagueRecordJson.Read("no es json").IsEmpty, Is.True);
            Assert.That(LeagueRecordJson.Read("[1,2]").IsEmpty, Is.True);
            LeagueRecord partial = LeagueRecordJson.Read("{\"pending\":[{\"win_id\":\"c:5\",\"kind\":\"campaign\",\"level\":\"x\",\"won_at\":\"2026-10-23T15:00:00Z\"},"
                + "{\"kind\":\"daily\",\"day\":\"2026-10-23\",\"won_at\":\"2026-10-23T15:00:00Z\"}],\"cache\":{\"global\":{\"week\":1}}}");
            Assert.That(partial.Pending.Count, Is.EqualTo(1), "lo que no se entiende se descarta: es cola y caché");
            Assert.That(partial.CacheGlobal, Is.Null);
        }

        [Test]
        public void Record_WithBothCaches_StaysSmall()
        {
            DateTime now = _clock.UtcNow;
            var record = new LeagueRecord
            {
                CacheGlobal = new LeagueCacheEntry(_api.Board(LeagueScope.Global), now),
                CacheCountry = new LeagueCacheEntry(_api.Board(LeagueScope.Country), now),
            };
            Assert.That(LeagueRecordJson.Write(record).Length, Is.LessThan(16 * 1024), "el perfil entero va por debajo de 100 KB (08 §4.3)");
        }

        [Test]
        public void Service_DoesNothing_WhenOffDisabledLockedOrBeforeTheUnlock()
        {
            _config.LiveLeagueEnabled = false;
            Assert.That(Service().RecordCampaignWin(57, RewardTier.Normal), Is.EqualTo(0));
            _config.LiveLeagueEnabled = true;
            _allowed = false; // banda < 13, perfil en solo lectura o cuenta borrada
            Assert.That(Service().RecordCampaignWin(57, RewardTier.Normal), Is.EqualTo(0));
            _allowed = true;
            _maxLevel = 40;
            Assert.That(Service().RecordCampaignWin(40, RewardTier.Normal), Is.EqualTo(0));
            Assert.That(Service().RequestBoard(LeagueScope.Global), Is.False);
            Assert.That(_api.WinsCalls + _api.BoardCalls, Is.EqualTo(0), "sin Liga, ni red");
            Assert.That(_store.SaveCount, Is.EqualTo(0));
        }

        [Test]
        public void Service_GivesThePointsOfTheLine_AndSendsTheWin()
        {
            _maxLevel = 41;
            LeagueService league = Service();
            Assert.That(league.RecordCampaignWin(41, RewardTier.Hard), Is.EqualTo(20), "desde el 41 (liga.md §1)");
            Assert.That(_api.WinsCalls, Is.EqualTo(1));
            Assert.That(_api.LastRequest!.Region, Is.EqualTo("CO"));
            Assert.That(_api.LastRequest.MaxLevel, Is.EqualTo(41));
            Assert.That(league.PendingCount, Is.EqualTo(0));
            Assert.That(league.Record.MePoints, Is.EqualTo(10), "los puntos del servidor (aquí, el falso con todo Normal)");
            Assert.That(league.RecordCampaignWin(41, RewardTier.Hard), Is.EqualTo(0), "repetida, no suma ni se envía");
            Assert.That(_api.WinsCalls, Is.EqualTo(1));
            Assert.That(league.RecordDailyWin("2026-10-23"), Is.EqualTo(20));
            Assert.That(league.Country, Is.EqualTo("CO"));
        }

        [Test]
        public void Service_KeepsTheQueueOffline_AcrossARestart_AndSendsItInBatches()
        {
            _api.Offline = true;
            var reports = new List<LeagueSendReport>();
            var failures = new List<LeagueFailure>();
            LeagueService league = Service();
            league.Unavailable += failures.Add;
            for (int level = 41; level < 161; level++)
            {
                _maxLevel = level;
                Assert.That(league.RecordCampaignWin(level, RewardTier.Normal), Is.EqualTo(10));
            }

            Assert.That(league.PendingCount, Is.EqualTo(120));
            Assert.That(failures.Count, Is.EqualTo(120), "cada intento sin red avisa (af_league_unavailable)");

            // Reinicio: otro servicio sobre el mismo JSON del perfil.
            var restarted = new InMemoryLeagueStore();
            restarted.Save(LeagueRecordJson.Read(_store.LastJson));
            _api.Offline = false;
            LeagueService again = Service(restarted);
            again.WinsSent += reports.Add;
            int calls = _api.WinsCalls;
            Assert.That(again.Flush(), Is.True);
            Assert.That(_api.WinsCalls - calls, Is.EqualTo(3), "120 en lotes de 50, 50 y 20");
            Assert.That(reports.Count, Is.EqualTo(3));
            Assert.That(reports[0].Count, Is.EqualTo(50));
            Assert.That(reports[2].Accepted, Is.EqualTo(20));
            Assert.That(again.PendingCount, Is.EqualTo(0));
            Assert.That(_api.Points, Is.EqualTo(1200));
        }

        [Test]
        public void Service_SendsOneBatchAtATime()
        {
            _api.Hold = true;
            LeagueService league = Service();
            league.RecordCampaignWin(50, RewardTier.Normal);
            league.RecordCampaignWin(51, RewardTier.Normal);
            Assert.That(_api.WinsCalls, Is.EqualTo(1), "con uno en vuelo, el segundo espera");
            Assert.That(league.Sending, Is.True);
            _api.Hold = false;
            _api.Release();
            Assert.That(_api.WinsCalls, Is.EqualTo(2), "al responder, sigue con el resto");
            Assert.That(league.PendingCount, Is.EqualTo(0));
        }

        [Test]
        public void Service_DuplicatesFromTheServer_LeaveTheQueue()
        {
            LeagueService league = Service();
            league.RecordCampaignWin(57, RewardTier.Normal);
            // Otro dispositivo (o la nube) vuelve a mandar el mismo win_id: el servidor lo rechaza y sale de la cola sin sumar.
            var record = _store.Load();
            LeagueQueue.Roll(record, _clock.UtcNow);
            record.Counted.Clear();
            Assert.That(league.RecordCampaignWin(57, RewardTier.Normal), Is.EqualTo(10));
            Assert.That(league.PendingCount, Is.EqualTo(0));
            Assert.That(_api.Points, Is.EqualTo(10), "idempotente por win_id");
        }

        [Test]
        public void Service_CachesEachScope_ForTheConfiguredMinutes()
        {
            _config.LiveLeagueCacheMinutes = 10;
            var changed = new List<LeagueScope>();
            LeagueService league = Service();
            league.BoardChanged += changed.Add;
            Assert.That(league.StateOf(LeagueScope.Global), Is.EqualTo(LeagueBoardState.Offline));
            Assert.That(league.RequestBoard(LeagueScope.Global), Is.True);
            Assert.That(changed, Is.EqualTo(new[] { LeagueScope.Global }));
            Assert.That(league.StateOf(LeagueScope.Global), Is.EqualTo(LeagueBoardState.Fresh));
            Assert.That(league.RequestBoard(LeagueScope.Global), Is.False, "fresca: no se vuelve a pedir");
            Assert.That(league.Cached(LeagueScope.Country), Is.Null, "cada ámbito, la suya");

            _clock.Advance(TimeSpan.FromMinutes(10));
            Assert.That(league.StateOf(LeagueScope.Global), Is.EqualTo(LeagueBoardState.Stale));
            _api.Offline = true;
            Assert.That(league.RequestBoard(LeagueScope.Global), Is.True);
            Assert.That(league.StateOf(LeagueScope.Global), Is.EqualTo(LeagueBoardState.Stale), "sin conexión, la guardada");
            Assert.That(league.LastFailure(LeagueScope.Global), Is.EqualTo(LeagueFailure.Offline));
            Assert.That(league.Cached(LeagueScope.Global), Is.Not.Null);

            // Tras sumar, la guardada ya no cuenta los puntos de ahora: se vuelve a pedir al abrir.
            _api.Offline = false;
            Assert.That(league.RequestBoard(LeagueScope.Global, force: true), Is.True);
            Assert.That(league.IsFresh(LeagueScope.Global), Is.True);
            league.RecordCampaignWin(58, RewardTier.Normal);
            Assert.That(league.IsFresh(LeagueScope.Global), Is.False);

            // El lunes, la de la semana pasada ya no se enseña.
            _clock.Set(new DateTime(2026, 10, 26, 12, 30, 0, DateTimeKind.Utc));
            Assert.That(league.Cached(LeagueScope.Global), Is.Null);
        }

        [Test]
        public void Service_CountryUnknown_LeavesOnlyGlobal()
        {
            _api.Country = null;
            LeagueService league = Service();
            league.RecordCampaignWin(57, RewardTier.Normal);
            Assert.That(league.Country, Is.Null);
            league.RequestBoard(LeagueScope.Country);
            Assert.That(league.LastFailure(LeagueScope.Country), Is.EqualTo(LeagueFailure.CountryUnknown));
        }

        [Test]
        public void Service_Wipe_ForgetsQueueAndCache()
        {
            _api.Offline = true;
            LeagueService league = Service();
            league.RecordCampaignWin(57, RewardTier.Normal);
            Assert.That(league.PendingCount, Is.EqualTo(1));
            league.Wipe();
            Assert.That(league.PendingCount, Is.EqualTo(0));
            Assert.That(_store.LastJson, Is.Null);
            Assert.That(league.Cached(LeagueScope.Global), Is.Null);
        }

        [Test]
        public void FakeApi_GivesTheRankOrThePercentile()
        {
            _api.StartPoints = 0;
            LeagueBoard empty = _api.Board(LeagueScope.Global);
            Assert.That(empty.Me.Rank, Is.Null);
            Assert.That(empty.Me.Percentile, Is.Null, "sin puntos, ni posición ni percentil (liga.md §3.1)");
            _api.TopPoints = 50;
            LeagueService league = Service();
            league.RecordCampaignWin(57, RewardTier.Normal);
            league.RecordCampaignWin(58, RewardTier.Normal);
            league.RecordCampaignWin(59, RewardTier.Normal);
            league.RecordCampaignWin(60, RewardTier.Normal);
            LeagueBoard board = _api.Board(LeagueScope.Global);
            Assert.That(board.Me.Rank, Is.Not.Null, "con 40 puntos entra en el top");
            Assert.That(board.Top.Count, Is.EqualTo(100));
        }
    }
}
