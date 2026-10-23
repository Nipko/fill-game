#nullable enable
using System;
using System.Collections.Generic;
using AF.Analytics.Unity;
using AF.Events;
using NUnit.Framework;

namespace AF.Analytics.Tests
{
    /// <summary>
    /// <see cref="EventBus"/> y <see cref="AnalyticsService"/>: el bus entrega en orden y aísla los fallos; el servicio traduce
    /// los eventos de dominio a 08 §8.2, añade los comunes de 08 §8.1, rechaza lo que el catálogo no tiene y respeta el
    /// consentimiento.
    /// </summary>
    public sealed class AnalyticsServiceTests
    {
        private sealed class FixedCommon : ICommonParamsSource
        {
            public int Calls;
            public string? RcVariant = "exp_a";

            public void Fill(CommonParams p)
            {
                Calls++;
                p.Level = 12;
                p.World = 1;
                p.Difficulty = "normal";
                p.Murky = false;
                p.Attempt = 2;
                p.SeedsBalance = 340;
                p.Canoes = 5;
                p.Streak = 0;
                p.GeoTier = "T3";
                p.NoAds = false;
                p.AppVersion = "1.0";
                p.ContentVersion = "1.0.0";
                p.RcVariant = RcVariant;
                p.SessionId = "s1";
                p.Net = "offline";
            }
        }

        [Test]
        public void TheBus_DeliversInOrder_AndAThrowingHandlerStopsNoOne()
        {
            var bus = new EventBus();
            var seen = new List<string>();
            var failures = new List<Type>();
            bus.HandlerFailed = (t, e) => failures.Add(t);
            Action<SeedsEarned> a = e => seen.Add("a" + e.Value);
            bus.Subscribe(a);
            bus.Subscribe<SeedsEarned>(e => throw new InvalidOperationException("analítica rota"));
            bus.Subscribe<SeedsEarned>(e => seen.Add("c" + e.Value));
            bus.Publish(new SeedsEarned("win", 20, 20));
            Assert.That(seen, Is.EqualTo(new[] { "a20", "c20" }));
            Assert.That(failures, Is.EqualTo(new[] { typeof(SeedsEarned) }));
            Assert.That(bus.CountOf<SeedsEarned>(), Is.EqualTo(3));
            Assert.That(bus.Unsubscribe(a), Is.True);
            Assert.That(bus.Unsubscribe(a), Is.False);
            bus.Publish(new SeedsSpent("hint", 100, 0)); // sin oyentes: nada
            bus.Publish(new SeedsEarned("win", 1, 21));
            Assert.That(seen, Is.EqualTo(new[] { "a20", "c20", "c1" }));
        }

        [Test]
        public void SubscribingInsideAHandler_CountsFromTheNextPublication()
        {
            var bus = new EventBus();
            int late = 0;
            bus.Subscribe<SeedsEarned>(e => bus.Subscribe<SeedsEarned>(x => late++));
            bus.Publish(new SeedsEarned("win", 1, 1));
            Assert.That(late, Is.EqualTo(0));
            bus.Publish(new SeedsEarned("win", 1, 2));
            Assert.That(late, Is.EqualTo(1));
        }

        [Test]
        public void TheDomainEvents_BecomeThe08_2Events()
        {
            var bus = new EventBus();
            var sink = new FakeAnalyticsSink();
            var analytics = new AnalyticsService(bus);
            analytics.AddSink(sink);
            bus.Publish(new LevelStarted(12, "m01_v1_n0012", "base", "comfort", 1, 1, "normal", false));
            bus.Publish(new SeedsEarned("win", 20, 20));
            bus.Publish(new LevelWon(12, "m01_v1_n0012", 18, 15, 2, "{\"undo\":2}", 41000, 20, false, false));
            bus.Publish(new SeedsSpent("continue_cuia", 300, 40));
            bus.Publish(new LevelFailed(31, "m02_v1_n0031", true, true, false, false, true, 22, 90000));
            bus.Publish(new LevelFailed(31, "m02_v1_n0031", false, false, false, false, false, 9, 30000));
            bus.Publish(new LevelAbandoned(31, "m02_v1_n0031", 4, 12000, "app_background"));
            Assert.That(sink.Names, Is.EqualTo(new[]
            {
                "level_start", "earn_virtual_currency", "af_level_win", "level_end", "spend_virtual_currency", "af_level_fail",
                "level_end", "af_level_fail", "af_level_abandon",
            }));
            Assert.That(analytics.Violations, Is.Empty);
            Assert.That(sink.Events[0].ToJson(), Is.EqualTo("{\"event\":\"level_start\",\"level_name\":\"m01_v1_n0012\",\"variant\":\"base\",\"band\":\"comfort\"}"));
            Assert.That(sink.Events[1].ToJson(), Is.EqualTo("{\"event\":\"earn_virtual_currency\",\"virtual_currency_name\":\"seeds\",\"source\":\"win\",\"value\":20}"));
            Assert.That(sink.Events[2].ToJson(), Is.EqualTo(
                "{\"event\":\"af_level_win\",\"pours\":18,\"par\":15,\"pours_over_par\":3,\"undo_used\":2,\"boosters_json\":\"{\\\"undo\\\":2}\","
                + "\"t_ms\":41000,\"reward_seeds\":20,\"double_harvest\":false,\"rescued\":false}"));
            Assert.That(sink.Events[3].ToJson(), Is.EqualTo("{\"event\":\"level_end\",\"success\":true,\"level_name\":\"m01_v1_n0012\"}"));
            Assert.That(sink.Events[4].Find("item_name")!.Value.Text, Is.EqualTo("continue_cuia"));
            Assert.That(sink.Events[5].Find("kind")!.Value.Text, Is.EqualTo("hard"));
            Assert.That(sink.Events[6].Find("success")!.Value.Bool, Is.False, "el fallo duro termina el nivel");
            Assert.That(sink.Events[7].Find("kind")!.Value.Text, Is.EqualTo("soft"));
            Assert.That(sink.Events[8].Find("reason")!.Value.Text, Is.EqualTo("app_background"));

            analytics.Dispose();
            bus.Publish(new SeedsEarned("win", 20, 40));
            Assert.That(sink.Events.Count, Is.EqualTo(9), "tras Dispose ya no escucha");
        }

        [Test]
        public void EveryEvent_CarriesTheCommonParams_InTheOrderOf08_1()
        {
            var common = new FixedCommon();
            var sink = new FakeAnalyticsSink();
            var analytics = new AnalyticsService(null, common);
            analytics.AddSink(sink);
            analytics.Log(AnalyticsEvent.Create("af_basket_open").Add("level", 20L).Add("contents_json", "{}"));
            AnalyticsEvent e = sink.Events[0];
            var names = new List<string>();
            foreach (AnalyticsParam p in e.Common)
            {
                names.Add(p.Name);
            }

            var expected = new List<string>(AnalyticsCatalog.CommonParamNames);
            expected.Remove("level"); // el específico manda
            Assert.That(names, Is.EqualTo(expected));
            Assert.That(e.Find("level")!.Value.Long, Is.EqualTo(20), "el level del evento, no el común");
            Assert.That(e.Find("net")!.Value.Text, Is.EqualTo("offline"));
            Assert.That(common.Calls, Is.EqualTo(1));
            Assert.That(e.Params.Count + e.Common.Count, Is.LessThanOrEqualTo(25), "08 §8.1: 25 parámetros por evento");
        }

        [Test]
        public void ANullCommon_IsNotSent()
        {
            var sink = new FakeAnalyticsSink();
            var analytics = new AnalyticsService();
            analytics.AddSink(sink);
            analytics.Log(AnalyticsEvent.Create("af_shop_view").Add("tab", "seeds").Add("from", "lobby"));
            Assert.That(sink.Events[0].Common, Is.Empty);
        }

        [Test]
        public void AnEmptyRcVariant_IsNotSent_LikeANullCommon()
        {
            // biz_experiment_tag vale «» por defecto (08 §9): sin cohorte no viaja rc_variant, como los demás comunes nulos.
            var common = new FixedCommon { RcVariant = "" };
            var sink = new FakeAnalyticsSink();
            var analytics = new AnalyticsService(null, common);
            analytics.AddSink(sink);
            analytics.Log(AnalyticsEvent.Create("af_shop_view").Add("tab", "seeds").Add("from", "lobby"));
            Assert.That(sink.Events[0].Find("rc_variant"), Is.Null);
            Assert.That(sink.Events[0].Find("session_id")!.Value.Text, Is.EqualTo("s1"), "los demás comunes sí van");

            common.RcVariant = "exp_a";
            analytics.Log(AnalyticsEvent.Create("af_shop_view").Add("tab", "seeds").Add("from", "lobby"));
            Assert.That(sink.Events[1].Find("rc_variant")!.Value.Text, Is.EqualTo("exp_a"));
        }

        [Test]
        public void WhatTheCatalogDoesNotHave_IsNotSent()
        {
            var sink = new FakeAnalyticsSink();
            var analytics = new AnalyticsService();
            analytics.AddSink(sink);
            Assert.That(analytics.Log(AnalyticsEvent.Create("af_inventado")), Is.False);
            Assert.That(analytics.Log(AnalyticsEvent.Create("af_shop_view").Add("tab", "seeds").Add("color", "rojo")), Is.True);
            Assert.That(sink.Names, Is.EqualTo(new[] { "af_shop_view" }));
            Assert.That(sink.Events[0].Find("color"), Is.Null);
            Assert.That(analytics.Violations, Is.EqualTo(new[] { "af_inventado", "af_shop_view.color" }));
            Assert.That(analytics.SetUserProperty("geo_tier", "T3"), Is.True);
            Assert.That(analytics.SetUserProperty("favorite_fruit", "acai"), Is.False);
            Assert.That(sink.Properties["geo_tier"], Is.EqualTo("T3"));
            for (int i = 0; i < AnalyticsService.MaxViolations + 10; i++)
            {
                analytics.Log(AnalyticsEvent.Create("af_inventado"));
            }

            Assert.That(analytics.Violations.Count, Is.EqualTo(AnalyticsService.MaxViolations), "con tope");
            Assert.That(analytics.ViolationCount, Is.EqualTo(AnalyticsService.MaxViolations + 13));
        }

        [Test]
        public void ASinkThatSendsOutside_WaitsForConsent()
        {
            var real = new FakeAnalyticsSink(requiresConsent: true);
            var local = new FakeAnalyticsSink();
            var analytics = new AnalyticsService();
            analytics.AddSink(real);
            analytics.AddSink(local);
            Assert.That(analytics.CollectionEnabled, Is.False, "08 §2.3: apagado hasta el consentimiento");
            analytics.Log(AnalyticsEvent.Create("af_shop_view").Add("tab", "seeds"));
            analytics.SetUserProperty("geo_tier", "T3");
            Assert.That(real.Events, Is.Empty);
            Assert.That(real.Properties, Is.Empty);
            Assert.That(local.Events.Count, Is.EqualTo(1));
            analytics.SetCollectionEnabled(true);
            analytics.Log(AnalyticsEvent.Create("af_shop_view").Add("tab", "seeds"));
            Assert.That(real.Events.Count, Is.EqualTo(1));
            analytics.SetCollectionEnabled(false);
            analytics.Log(AnalyticsEvent.Create("af_shop_view").Add("tab", "seeds"));
            Assert.That(real.Events.Count, Is.EqualTo(1), "corte por consentimiento");
            Assert.That(analytics.Sent, Is.EqualTo(3));
        }

        private sealed class AwareSink : IAnalyticsSink, ICollectionAwareSink
        {
            public readonly List<bool> Changes = new List<bool>();

            public bool RequiresConsent => true;

            public void Send(AnalyticsEvent e)
            {
            }

            public void SetUserProperty(string name, string value)
            {
            }

            public void SetCollectionEnabled(bool enabled) => Changes.Add(enabled);
        }

        [Test]
        public void ASinkThatCollectsOnItsOwn_FollowsTheConsent()
        {
            // Firebase Analytics recoge sesiones por su cuenta: se apaga al añadirlo y sigue cada cambio de consentimiento (08 §2.3; F3-6).
            var analytics = new AnalyticsService();
            var aware = new AwareSink();
            analytics.AddSink(aware);
            Assert.That(aware.Changes, Is.EqualTo(new[] { false }), "apagado hasta el consentimiento");
            analytics.SetCollectionEnabled(true);
            analytics.SetCollectionEnabled(false);
            Assert.That(aware.Changes, Is.EqualTo(new[] { false, true, false }));

            var late = new AwareSink();
            analytics.SetCollectionEnabled(true);
            analytics.AddSink(late);
            Assert.That(late.Changes, Is.EqualTo(new[] { true }), "el que llega tarde recibe el estado vigente");
        }

        [Test]
        public void NoFirebaseInTheEditor()
        {
            Assert.That(AF.Analytics.Firebase.FirebaseAnalyticsSink.TryCreate(), Is.Null, "el editor usa solo los sumideros locales (08 §2.1, principio 6)");
            Assert.That(AF.Analytics.Firebase.CrashReporting.Start(), Is.False, "sin Crashlytics en el editor");
            Assert.That(AF.Analytics.Firebase.FirebaseAnalyticsSink.BoolValue(true), Is.EqualTo(1L), "en Firebase, los booleanos van como 0/1 (Q-B7-18 (a))");
            Assert.That(AF.Analytics.Firebase.FirebaseAnalyticsSink.BoolValue(false), Is.EqualTo(0L));
        }

        [Test]
        public void CrashReporting_TheLastRequestWins_WhenFirebaseAnswers()
        {
            // B12 (08 §7): un encendido que esperaba a Firebase no gana a un apagado posterior, y al revés.
            var intent = new AF.Analytics.Firebase.CrashReportingIntent();
            Assert.That(intent.Wanted, Is.False, "apagado al empezar, como el manifiesto");
            intent.Request(true);
            intent.Request(false);
            Assert.That(intent.WhenBooted(true), Is.EqualTo(AF.Analytics.Firebase.CrashReportingAction.DisableInSdk));
            intent.Request(true);
            Assert.That(intent.WhenBooted(true), Is.EqualTo(AF.Analytics.Firebase.CrashReportingAction.EnableInSdk));
        }

        [Test]
        public void CrashReporting_StopWithoutFirebase_GoesNative_AndAFailedBootStillTurnsItOff()
        {
            var intent = new AF.Analytics.Firebase.CrashReportingIntent();
            intent.Request(false);
            Assert.That(intent.OnStop(firebaseBootRequested: false), Is.EqualTo(AF.Analytics.Firebase.CrashReportingAction.DisableNative),
                "arranque con <13 guardado: sin arrancar Firebase ni App Check");
            Assert.That(intent.OnStop(firebaseBootRequested: true), Is.EqualTo(AF.Analytics.Firebase.CrashReportingAction.None), "espera a Firebase");
            Assert.That(intent.WhenBooted(false), Is.EqualTo(AF.Analytics.Firebase.CrashReportingAction.DisableNative),
                "Firebase no disponible: se apaga igualmente en el SDK de Android");
            intent.Request(true);
            Assert.That(intent.WhenBooted(false), Is.EqualTo(AF.Analytics.Firebase.CrashReportingAction.None), "sin Firebase no se puede encender");
        }

        [Test]
        public void TheLogcatLine_HasItsOwnPrefix_NotAfState()
        {
            var e = AnalyticsEvent.Create("af_settings_change").Add("key", "sound").Add("value", "off");
            Assert.That(LogcatAnalyticsSink.Line(e), Is.EqualTo("AF_EVENT {\"event\":\"af_settings_change\",\"key\":\"sound\",\"value\":\"off\"}"));
            Assert.That(new LogcatAnalyticsSink().RequiresConsent, Is.False);
        }

        /// <summary>
        /// Observación 5 de la hoja del 2026-10-22: con la analítica apagada seguían saliendo líneas <c>AF_EVENT</c>. Son solo de
        /// logcat: el sumidero que envía fuera (Firebase, aquí uno falso con <c>RequiresConsent</c>) no recibe nada, y la línea lo
        /// dice con su prefijo <c>AF_EVENT_UNSENT</c>; con el consentimiento, <c>AF_EVENT</c> y el sumidero que envía lo recibe.
        /// </summary>
        [Test]
        public void WithoutConsent_OnlyLogcat_AndItsLineSaysUnsent()
        {
            var lines = new List<string>();
            void OnLog(string condition, string stack, UnityEngine.LogType type)
            {
                if (condition.StartsWith("AF_EVENT", StringComparison.Ordinal))
                {
                    lines.Add(condition);
                }
            }

            UnityEngine.Application.logMessageReceived += OnLog;
            try
            {
                var service = new AnalyticsService();
                var logcat = new LogcatAnalyticsSink();
                var firebase = new FakeAnalyticsSink(requiresConsent: true);
                service.AddSink(logcat);
                service.AddSink(firebase);
                Assert.That(logcat.CollectionEnabled, Is.False, "apagado hasta el consentimiento (08 §2.3)");

                Assert.That(service.Log(AnalyticsEvent.Create("af_map_open").Add("source", "home")), Is.True);
                service.SetUserProperty("geo_tier", "t3");
                Assert.That(firebase.Events, Is.Empty, "sin consentimiento, nada al sumidero que envía (B12, F3-6)");
                Assert.That(firebase.Properties, Is.Empty);
                Assert.That(lines.Count, Is.EqualTo(2));
                Assert.That(lines[0], Does.StartWith(LogcatAnalyticsSink.UnsentPrefix + "{\"event\":\"af_map_open\""));
                Assert.That(lines[1], Does.StartWith(LogcatAnalyticsSink.UnsentPrefix + "{\"user_property\":\"geo_tier\""));

                lines.Clear();
                service.SetCollectionEnabled(true);
                Assert.That(logcat.CollectionEnabled, Is.True);
                service.Log(AnalyticsEvent.Create("af_map_open").Add("source", "home"));
                Assert.That(firebase.Names, Is.EqualTo(new[] { "af_map_open" }));
                Assert.That(lines.Count, Is.EqualTo(1));
                Assert.That(lines[0], Does.StartWith(LogcatAnalyticsSink.Prefix + "{\"event\":\"af_map_open\""));

                lines.Clear();
                service.SetCollectionEnabled(false); // «Privacidad y datos» › analítica apagada: al momento
                service.Log(AnalyticsEvent.Create("af_map_close").Add("dwell_ms", 1200));
                Assert.That(service.Violations, Is.Empty);
                Assert.That(firebase.Names, Is.EqualTo(new[] { "af_map_open" }), "nada más tras apagarla");
                Assert.That(lines[0], Does.StartWith(LogcatAnalyticsSink.UnsentPrefix));
            }
            finally
            {
                UnityEngine.Application.logMessageReceived -= OnLog;
            }
        }

        [Test]
        public void TextValues_AreEscaped()
        {
            var e = AnalyticsEvent.Create("af_error").Add("module", "a\"b\\c\n").Add("code", 1.5).Add("fatal", true);
            Assert.That(e.ToJson(), Is.EqualTo("{\"event\":\"af_error\",\"module\":\"a\\\"b\\\\c\\u000a\",\"code\":1.5,\"fatal\":true}"));
        }
    }
}
