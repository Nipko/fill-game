#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.RemoteConfig.Tests
{
    /// <summary>
    /// <see cref="RemoteConfigService"/>, <see cref="RC"/>, la congelación por pantalla y el lector JSON (08 §2.2 y §9;
    /// 04 §10.2): acceso tipado, origen Default/Cached/Remote, valores remotos malos, esquemas mayores y caché.
    /// </summary>
    public sealed class RemoteConfigServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            RC.Reset();
        }

        private static Dictionary<string, string> Values(params string[] kv)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < kv.Length; i += 2)
            {
                d[kv[i]] = kv[i + 1];
            }

            return d;
        }

        [Test]
        public void Get_ReadsEachTypeOfTheTable()
        {
            var rc = new RemoteConfigService();
            Assert.That(rc.Get<int>("canoes_max"), Is.EqualTo(5));
            Assert.That(rc.Get<long>("hint_budget_nodes"), Is.EqualTo(40000L));
            Assert.That(rc.Get<double>("rescue_first_day_mult"), Is.EqualTo(0.5));
            Assert.That(rc.Get<float>("ui_pattern_opacity"), Is.EqualTo(0.2f));
            Assert.That(rc.Get<double>("canoes_max"), Is.EqualTo(5.0), "un int se lee también como decimal");
            Assert.That(rc.Get<bool>("biz_iap_enabled"), Is.True);
            Assert.That(rc.Get<string>("rules_pour_mode"), Is.EqualTo("run"));
            Assert.That(rc.Get<string>("rescue_price_bands"), Is.EqualTo("[[31,600]]"), "json como texto compacto");
            RcJson bands = rc.Get<RcJson>("rescue_price_bands");
            Assert.That(bands.Items[0].Items[1].Number, Is.EqualTo(600));
            Assert.That(rc.Source, Is.EqualTo(RcSource.Default));
            Assert.That(rc.Variant, Is.EqualTo(""), "biz_experiment_tag");
        }

        [Test]
        public void Get_RejectsInventedKeys_AndWrongTypes()
        {
            var rc = new RemoteConfigService();
            Assert.Throws<KeyNotFoundException>(() => rc.Get<int>("lives_max"), "alias retirado");
            Assert.Throws<KeyNotFoundException>(() => rc.Get<int>("no_existe"));
            Assert.Throws<InvalidOperationException>(() => rc.Get<int>("biz_iap_enabled"));
            Assert.Throws<InvalidOperationException>(() => rc.Get<bool>("canoes_max"));
            Assert.Throws<InvalidOperationException>(() => rc.Get<int>("anim_tint_skip_alarm_pct"), "un float no se lee como int");
            Assert.That(rc.TryGet("biz_iap_enabled", out int _), Is.False);
            Assert.That(rc.TryGet("canoes_max", out int max) && max == 5, Is.True);
            rc.Activate(Values("hint_budget_nodes", "9999999999"));
            Assert.That(rc.TryGet("hint_budget_nodes", out int _), Is.False, "no cabe en un int: false, sin excepción");
            Assert.That(rc.Get<long>("hint_budget_nodes"), Is.EqualTo(9999999999L));
        }

        [Test]
        public void APendingKey_HasNoValue_UntilRemoteGivesOne()
        {
            var rc = new RemoteConfigService();
            Assert.Throws<InvalidOperationException>(() => rc.Get<string>("eco_calendar_table_v1"));
            Assert.That(rc.TryGet("eco_calendar_table_v1", out string _), Is.False);
            rc.Activate(Values("eco_calendar_table_v1", "[1,2]"));
            Assert.That(rc.Get<string>("eco_calendar_table_v1"), Is.EqualTo("[1,2]"));
        }

        [Test]
        public void Activate_UsesTheRemoteValues_OverTheDefaults()
        {
            var rc = new RemoteConfigService();
            RcSnapshot before = rc.Freeze();
            Assert.That(rc.Activate(Values("canoes_max", "6", "biz_experiment_tag", "price_a", "rules_pour_mode", "single")), Is.True);
            Assert.That(rc.Get<int>("canoes_max"), Is.EqualTo(6));
            Assert.That(rc.Current.SourceOf("canoes_max"), Is.EqualTo(RcSource.Remote));
            Assert.That(rc.Current.SourceOf("canoes_regen_min"), Is.EqualTo(RcSource.Default));
            Assert.That(rc.Source, Is.EqualTo(RcSource.Remote));
            Assert.That(rc.Variant, Is.EqualTo("price_a"));
            Assert.That(before.Get<int>("canoes_max"), Is.EqualTo(5), "lo congelado antes no cambia (04 §10.2)");
            Assert.That(rc.Current.Version, Is.GreaterThan(before.Version));
        }

        [Test]
        public void ABadRemoteValue_IsRejected_AndThePreviousStays()
        {
            var rc = new RemoteConfigService();
            rc.Activate(Values("canoes_max", "6"));
            rc.Activate(Values("canoes_max", "seis", "biz_iap_enabled", "si", "rescue_price_bands", "[[31,", "hint_weight", "NaN"));
            Assert.That(rc.Get<int>("canoes_max"), Is.EqualTo(6), "se queda el remoto anterior");
            Assert.That(rc.Get<bool>("biz_iap_enabled"), Is.True, "se queda el default");
            Assert.That(rc.Get<string>("rescue_price_bands"), Is.EqualTo("[[31,600]]"));
            Assert.That(rc.Get<double>("hint_weight"), Is.EqualTo(2.0));
            Assert.That(rc.Rejected, Is.EqualTo(new[] { "biz_iap_enabled", "canoes_max", "hint_weight", "rescue_price_bands" }));
        }

        [Test]
        public void UnknownRemoteKeys_AreIgnored_AndANewerSchema_IgnoresEverything()
        {
            var rc = new RemoteConfigService();
            Assert.That(rc.Activate(Values("clave_de_la_1_2", "1", "canoes_max", "6")), Is.True);
            Assert.That(rc.Get<int>("canoes_max"), Is.EqualTo(6));
            Assert.That(rc.Activate(Values("rc_schema", "2", "canoes_max", "7")), Is.False, "08 §9: el cliente ignora esquemas mayores");
            Assert.That(rc.IgnoredNewerSchema, Is.True);
            Assert.That(rc.Get<int>("canoes_max"), Is.EqualTo(6), "sigue lo que había");
            Assert.That(rc.Activate(Values("rc_schema", "1", "canoes_max", "7")), Is.True);
            Assert.That(rc.Get<int>("canoes_max"), Is.EqualTo(7));
        }

        [Test]
        public void ARemoteValueEqualToTheDefault_StillCountsAsTheDefault()
        {
            var rc = new RemoteConfigService();
            rc.Activate(Values("canoes_max", "5"));
            Assert.That(rc.Current.SourceOf("canoes_max"), Is.EqualTo(RcSource.Default));
        }

        [Test]
        public void TheFakeBackend_GivesTheCache_AndFetchesEvery12h()
        {
            var backend = new FakeRemoteConfigBackend().Set("canoes_max", "6");
            var rc = new RemoteConfigService(backend: backend);
            Assert.That(rc.ActivateCached(), Is.False, "sin caché todavía");
            var t0 = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
            bool? ok = null;
            Assert.That(rc.FetchIfDue(t0, null, r => ok = r), Is.True);
            Assert.That(ok, Is.True);
            Assert.That(rc.Get<int>("canoes_max"), Is.EqualTo(5), "lo descargado se activa en el arranque siguiente");
            Assert.That(rc.FetchIfDue(t0.AddHours(11.9), t0), Is.False, "intervalo mínimo de 12 h");
            Assert.That(rc.FetchIfDue(t0.AddHours(12), t0), Is.True);
            Assert.That(backend.Fetches, Is.EqualTo(2));

            var next = new RemoteConfigService(backend: backend);
            Assert.That(next.ActivateCached(), Is.True);
            Assert.That(next.Get<int>("canoes_max"), Is.EqualTo(6));
            Assert.That(next.Current.SourceOf("canoes_max"), Is.EqualTo(RcSource.Cached));
            Assert.That(next.Source, Is.EqualTo(RcSource.Cached));

            backend.Offline = true;
            bool? offline = null;
            new RemoteConfigService(backend: backend).FetchIfDue(t0, null, r => offline = r);
            Assert.That(offline, Is.False);
        }

        [Test]
        public void TheScreenFreezer_KeepsTheValuesOfWhenTheScreenOpened()
        {
            var rc = new RemoteConfigService();
            var screens = new RcScreenFreezer(rc);
            RcSnapshot stuck = screens.Open("stuck_modal");
            rc.Activate(Values("undo_refill_seed_price", "40"));
            Assert.That(screens.For("stuck_modal").Get<int>("undo_refill_seed_price"), Is.EqualTo(30), "dentro del modal abierto no cambia");
            Assert.That(ReferenceEquals(screens.For("stuck_modal"), stuck), Is.True);
            Assert.That(screens.For("shop").Get<int>("undo_refill_seed_price"), Is.EqualTo(40), "una pantalla cerrada ve lo vigente");
            Assert.That(screens.Close("stuck_modal"), Is.True);
            Assert.That(screens.IsOpen("stuck_modal"), Is.False);
            Assert.That(screens.Open("stuck_modal").Get<int>("undo_refill_seed_price"), Is.EqualTo(40), "al volver a abrir, lo de ese momento");
        }

        [Test]
        public void RC_ReadsTheCompiledDefaults_UntilTheGameGivesItsService()
        {
            Assert.That(RC.Get<int>("canoes_max"), Is.EqualTo(5));
            var rc = new RemoteConfigService();
            rc.Activate(Values("canoes_max", "6"));
            RC.Use(rc);
            Assert.That(RC.Get<int>("canoes_max"), Is.EqualTo(6));
            Assert.That(RC.Freeze().Get<int>("canoes_max"), Is.EqualTo(6));
            RC.Reset();
            Assert.That(RC.Get<int>("canoes_max"), Is.EqualTo(5));
        }

        [Test]
        public void RcValue_FromRemote_IsTheInverseOfToRemoteString()
        {
            Assert.That(RcValue.TryFromRemote(RcType.Int, " 12 ", out RcValue i) && i.Integer == 12, Is.True);
            Assert.That(RcValue.TryFromRemote(RcType.Int, "1.5", out _), Is.False);
            Assert.That(RcValue.TryFromRemote(RcType.Float, "0.8", out RcValue f) && f.Number == 0.8, Is.True);
            Assert.That(RcValue.TryFromRemote(RcType.Float, "0,8", out _), Is.False, "punto decimal, nunca coma");
            Assert.That(RcValue.TryFromRemote(RcType.Bool, "True", out _), Is.False, "true/false exactos");
            Assert.That(RcValue.TryFromRemote(RcType.Json, "{\"a\": [1, 2]}", out RcValue j) && j.Text == "{\"a\":[1,2]}", Is.True);
            Assert.That(RcValue.TryFromRemote(RcType.String, "", out RcValue s) && s.Text == "", Is.True);
            Assert.That(RcValue.OfFloat(0.05).ToRemoteString(), Is.EqualTo("0.05"));
            Assert.That(RcValue.OfInt(-3).ToRemoteString(), Is.EqualTo("-3"));
        }

        [Test]
        public void RcJson_ReadsAndWritesJson_AndRejectsTheRest()
        {
            RcJson n = RcJson.Parse(" {\"a\" : [1, -2.5e1, true, null, \"x\\\"\\u00e9\"], \"b\": {}} ");
            Assert.That(n.ToJson(), Is.EqualTo("{\"a\":[1,-2.5e1,true,null,\"x\\\"é\"],\"b\":{}}"));
            Assert.That(n["a"]!.Items[1].Number, Is.EqualTo(-25.0));
            Assert.That(n["a"]!.Items[0].IsInteger, Is.True);
            Assert.That(n["a"]!.Items[1].IsInteger, Is.False);
            foreach (string bad in new[] { "", "{", "[1,]", "{\"a\":1,\"a\":2}", "01", "1 2", "{a:1}", "\"\\x\"", "tru", "[1}", "1e999", "[-1e400]" })
            {
                Assert.That(RcJson.TryParse(bad, out _, out string? error), Is.False, bad);
                Assert.That(error, Is.Not.Null.And.Not.Empty, bad);
            }
        }
    }
}
