#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using AF.Ads;
using AF.Backend.Verification;
using AF.Boosters;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.LiveOps;
using AF.Platform;
using AF.RemoteConfig.Binding;
using NUnit.Framework;

namespace AF.RemoteConfig.Tests
{
    /// <summary>
    /// <see cref="RcConfigBinder"/>: con los defaults compilados salen exactamente los <c>X.Defaults</c> de cada clase de cifras
    /// (así el JSON generado de 08 §9 y el código no pueden separarse), cada propiedad con <c>[RemoteConfigKey]</c> está enlazada
    /// y un valor remoto llega a su propiedad; lo que no se entiende o se sale del rango vuelve a los defaults.
    /// </summary>
    public sealed class RcConfigBinderTests
    {
        // Clases con [RemoteConfigKey] que el binder aún no enlaza, con su motivo (nombre simple del tipo). Si una de estas llega
        // a esta rama sin entrada en RcConfigBinder.BoundKeys, la prueba la acepta; si llega una que no está aquí ni enlazada,
        // falla: toda clase de cifras se enlaza o se declara aquí (nota §2.3).
        private static readonly Dictionary<string, string> NotBoundYet = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "ShuffleConfig", "B2 (AF.Boosters, 5 claves): ya integrada; falta su tabla en RcConfigBinder y pasarla a la Pororoca" },
            { "SkipConfig", "B3 (Salto del Boto, 3 claves): ya integrada; falta su tabla en RcConfigBinder y pasarla al Salto" },
            { "ComebackConfig", "B6 (AF.LiveOps, live_comeback_gift): ya integrada; falta su tabla en RcConfigBinder y pasarla al Regalo de regreso" },
            { "PushConfig", "B6 (AF.LiveOps, 5 claves push_*): ya integrada; falta su tabla en RcConfigBinder y pasarla a los avisos" },
        };

        private static RcSnapshot With(params string[] kv)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < kv.Length; i += 2)
            {
                d[kv[i]] = kv[i + 1];
            }

            var rc = new RemoteConfigService();
            rc.Activate(d);
            return rc.Freeze();
        }

        private static RcConfigs Bind(params string[] kv) => RcConfigBinder.Bind(With(kv), "1.0");

        [Test]
        public void WithTheCompiledDefaults_EveryConfigIsItsDefaults()
        {
            RcConfigs c = RcConfigBinder.Bind(new RemoteConfigService(), "1.0");
            Assert.That(c.Errors, Is.Empty);
            Assert.That(c.Values.Source, Is.EqualTo(RcSource.Default));
            AssertSame(EconomyConfig.Defaults, c.Economy);
            AssertSame(BoosterConfig.Defaults, c.Boosters);
            AssertSame(AdsConfig.Defaults, c.Ads);
            AssertSame(StoreConfig.Defaults, c.Store);
            AssertSame(OffersConfig.Defaults, c.Offers);
            AssertSame(DailyChallengeConfig.Defaults, c.Daily);
            AssertSame(StreakConfig.Defaults, c.Streak);
            AssertSame(BasketConfig.Defaults, c.Basket);
            AssertSame(ClockSecurityConfig.Defaults, c.ClockSecurity);
            AssertSame(VerificationQueueConfig.Defaults, c.Verification);
            AssertSame(RevocationConfig.Defaults, c.Revocation);
            AssertSame(TakeoverConfig.Defaults, c.Takeovers);
            AssertSame(LoginCalendarConfig.Defaults, c.LoginCalendar);
            AssertSame(A11yConfig.Defaults, c.A11y); // B9
            Assert.That(c.BackendSwitch.BizBackendEnabled, Is.True, "biz_backend_enabled = true en 08 §9 [P]");
            AssertSame(ConsentConfig.Defaults, c.Consent); // B12
        }

        [Test]
        public void TheF35AndB4Configs_ReadTheirKeys()
        {
            // F3-5: sec_purchase_deferred_ack_h (48-72) y sec_ssv_grace_h; B4: la tasa de la Bitácora y su interruptor.
            RcConfigs c = Bind(
                "sec_purchase_deferred_ack_h", "72",
                "sec_ssv_grace_h", "12",
                "biz_feature_login_calendar", "false",
                "eco_calendar_locked_booster_seeds", "{\"other\":100,\"undo3\":30}");
            Assert.That(c.Errors, Is.Empty, string.Join(" | ", c.Errors));
            Assert.That(c.Verification.SecPurchaseDeferredAckH, Is.EqualTo(72), "el máximo del rango: distinto del default de 48");
            Assert.That(c.Revocation.SecSsvGraceH, Is.EqualTo(12));
            Assert.That(c.LoginCalendar.BizFeatureLoginCalendar, Is.False);
            Assert.That(c.LoginCalendar.EcoCalendarLockedBoosterSeeds.Undo, Is.EqualTo(30));
            Assert.That(c.LoginCalendar.EcoCalendarTableV2.Format(), Is.EqualTo(AF.LiveOps.LoginCalendarTable.Default.Format()),
                "eco_calendar_table_v2 sin valor remoto (pending en 08 §9): la tabla compilada (Q-B4)");
            Assert.That(c.Takeovers.UiMaxTakeoversPerSession, Is.EqualTo(1));
        }

        [Test]
        public void TheF35AndB4Configs_OutOfRangeOrUnreadable_FallBackToTheirDefaults()
        {
            RcConfigs c = Bind(
                "sec_purchase_deferred_ack_h", "96",
                "ui_max_takeovers_per_session", "2",
                "eco_calendar_table_v2", "{\"cycle\":[]}");
            Assert.That(c.Verification.SecPurchaseDeferredAckH, Is.EqualTo(48), "08 §9: 48-72, default 48 (D-040, Q-BILL-5)");
            Assert.That(c.Takeovers.UiMaxTakeoversPerSession, Is.EqualTo(1), "08 §9: solo 1");
            Assert.That(c.LoginCalendar.EcoCalendarTableV2.Format(), Is.EqualTo(AF.LiveOps.LoginCalendarTable.Default.Format()),
                "una tabla remota que no vale deja la compilada (Q-B4)");
            Assert.That(c.Errors.Count, Is.EqualTo(3), string.Join(" | ", c.Errors));
            Assert.That(string.Join(" | ", c.Errors), Does.Contain("VerificationQueueConfig:").And.Contain("TakeoverConfig:").And.Contain("eco_calendar_table_v2:"));
        }

        [Test]
        public void TheB11Values_AreTheirDefaults_WithTheCompiledDefaults()
        {
            // Cierre de B11 (nota tutorial-b11 §3): drag_pour_enabled = true y lvl_label_override = {} (08 §9).
            RcConfigs c = RcConfigBinder.Bind(new RemoteConfigService(), "1.0");
            Assert.That(c.DragPourEnabled, Is.EqualTo(DragPour.EnabledDefault));
            Assert.That(c.DragPourEnabled, Is.True);
            Assert.That(c.LevelLabels.Count, Is.EqualTo(0));
            Assert.That(RcGameplayBinder.Keys, Is.EqualTo(new[] { "drag_pour_enabled", "lvl_label_override", "lvl_labels_from_level", "lvl_very_hard_from_level" }));
            foreach (string key in RcGameplayBinder.Keys)
            {
                Assert.That(RemoteConfigDefaults.Compiled.Find(key), Is.Not.Null, key + " está en 08 §9");
            }
        }

        [Test]
        public void TheB11Values_ReadTheirKeys()
        {
            RcConfigs c = Bind(
                "drag_pour_enabled", "false",
                "lvl_label_override", "{\"65\":\"very_hard\",\"41\":\"hard\",\"120\":\"normal\"}");
            Assert.That(c.Errors, Is.Empty, string.Join(" | ", c.Errors));
            Assert.That(c.DragPourEnabled, Is.False);
            Assert.That(c.LevelLabels.Levels, Is.EqualTo(new[] { 41, 65, 120 }));
            Assert.That(c.LevelLabels.TryGet(65, out string label65), Is.True);
            Assert.That(label65, Is.EqualTo(AF.Levels.LevelDefinition.LabelVeryHard));
            Assert.That(c.LevelLabels.TryGet(120, out string label120), Is.True);
            Assert.That(label120, Is.EqualTo(AF.Levels.LevelDefinition.LabelNormal));
        }

        [Test]
        public void TheLabelTable_UsesTheFirstLevelOfEachLabel_FromRemoteConfig()
        {
            // lvl_labels_from_level / lvl_very_hard_from_level (08 §9): con 45 y 70, «hard» en el 41 y «very_hard» en el 65 no valen.
            RcConfigs c = Bind(
                "lvl_labels_from_level", "45",
                "lvl_very_hard_from_level", "70",
                "lvl_label_override", "{\"41\":\"hard\",\"65\":\"very_hard\",\"50\":\"hard\"}");
            Assert.That(c.LevelLabels.Levels, Is.EqualTo(new[] { 50 }));
            Assert.That(c.Errors.Count, Is.EqualTo(2), string.Join(" | ", c.Errors));
            Assert.That(string.Join(" | ", c.Errors), Does.Contain("lvl_labels_from_level = 45").And.Contain("lvl_very_hard_from_level = 70"));
        }

        [Test]
        public void TheB11Values_Unreadable_FallBackToTheirDefaults_WithoutBreakingTheRest()
        {
            RcConfigs c = Bind(
                "lvl_label_override", "[1,2]",
                "eco_reward_hard", "70");
            Assert.That(c.DragPourEnabled, Is.True);
            Assert.That(c.LevelLabels.Count, Is.EqualTo(0), "un JSON que no es un objeto deja {} (08 §9)");
            Assert.That(c.Economy.EcoRewardHard, Is.EqualTo(70), "lo demás se lee igual");
            Assert.That(string.Join(" | ", c.Errors), Does.Contain("lvl_label_override"));
            Assert.Throws<ArgumentNullException>(() => RcGameplayBinder.DragPourEnabled(null!));
            Assert.Throws<ArgumentNullException>(() => RcGameplayBinder.LevelLabels(null!));
        }

        [Test]
        public void TheBackendSwitch_ReadsBizBackendEnabled()
        {
            Assert.That(Bind("biz_backend_enabled", "false").BackendSwitch.BizBackendEnabled, Is.False);
            Assert.That(new RcBackendSwitch(With("biz_backend_enabled", "true")).BizBackendEnabled, Is.True);
            Assert.That(new RcBackendSwitch(With()).BizBackendEnabled, Is.EqualTo(BackendSwitch.Default));
            Assert.Throws<ArgumentNullException>(() => new RcBackendSwitch(null!));
        }

        [Test]
        public void EveryTypeWithRemoteConfigKeys_IsBound_OrDeclaredAsNotBoundYet()
        {
            var bound = 0;
            foreach (KeyValuePair<string, KeyValuePair<Type, List<string>>> t in RemoteConfigKeyScan.Types())
            {
                Type type = t.Value.Key;
                if (RcConfigBinder.BoundKeys.TryGetValue(type, out IReadOnlyList<string>? boundKeys))
                {
                    Assert.That(NotBoundYet.ContainsKey(type.Name), Is.False, type.Name + " ya está enlazada: quítala de NotBoundYet");
                    var keys = new List<string>(t.Value.Value);
                    var expected = new List<string>(boundKeys);
                    keys.Sort(StringComparer.Ordinal);
                    expected.Sort(StringComparer.Ordinal);
                    Assert.That(expected, Is.EqualTo(keys), type.Name + ": cada [RemoteConfigKey] enlazada, y nada más");
                    bound++;
                    continue;
                }

                Assert.That(NotBoundYet.ContainsKey(type.Name), Is.True,
                    t.Key + " tiene [RemoteConfigKey] y RcConfigBinder no la enlaza: añádela a RcConfigBinder o a NotBoundYet con su motivo");
            }

            Assert.That(bound, Is.EqualTo(RcConfigBinder.BoundKeys.Count), "cada clase del binder está entre las descubiertas");
        }

        [Test]
        public void RemoteValues_ReachTheirProperties()
        {
            RcConfigs c = Bind(
                "eco_reward_hard", "70",
                "rescue_first_day_mult", "0.5",
                "rescue_price_bands", "[[31,600]]",
                "hint_weight", "2.5",
                "stuck_order_t1", "rescue,undo,balsa,pack",
                "ads_interstitial_every_n_levels", "[[22,4],[61,3]]",
                "ads_rv_cooldown_s_extra_canoe", "120",
                "eco_seed_pack_amounts", "{\"af_coins_2\":1100,\"af_coins_3\":2800,\"af_coins_4\":6000,\"af_coins_5\":13000,\"af_coins_6\":36000}",
                "live_daily_challenge_reward", "{\"seeds\":70,\"murky_bonus_mult\":2}",
                "lvl_daily_bracket_levels", "[41,201]",
                "daily_overrides", "{}",
                "streak_milestones", "{\"3\":250,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"}",
                "eco_basket_cycle", "[\"hint\",\"undo3\"]",
                "sec_clock_max_drift_h", "36");
            Assert.That(c.Errors, Is.Empty, string.Join(" | ", c.Errors));
            Assert.That(c.Economy.EcoRewardHard, Is.EqualTo(70));
            Assert.That(c.Boosters.HintWeight, Is.EqualTo(2.5));
            Assert.That(c.Boosters.StuckOrderT1, Is.EqualTo("rescue,undo,balsa,pack"));
            Assert.That(c.Ads.AdsInterstitialEveryNLevels[0].EveryLevels, Is.EqualTo(4));
            Assert.That(c.Ads.AdsInterstitialEveryNLevels[1].FromLevel, Is.EqualTo(61));
            Assert.That(c.Ads.AdsRvCooldownSExtraCanoe, Is.EqualTo(120));
            Assert.That(c.Store.EcoSeedPackAmounts[0].Seeds, Is.EqualTo(1100));
            Assert.That(c.Daily.LiveDailyChallengeReward.Seeds, Is.EqualTo(70));
            Assert.That(c.Streak.StreakMilestones.Hit3Seeds, Is.EqualTo(250));
            Assert.That(c.Basket.EcoBasketCycle, Is.EqualTo(new[] { "hint", "undo3" }));
            Assert.That(c.ClockSecurity.SecClockMaxDriftH, Is.EqualTo(36));
            Assert.That(c.Daily.ClockSecurity.SecClockMaxDriftH, Is.EqualTo(36), "la copia del Reto, con la misma clave");
        }

        [Test]
        public void TheOffers_ReadTheirKeys_AndAnOutOfRangeValueFallsBackToTheirDefaults()
        {
            // B5 (05 §4.1 y §5; 08 §9): la Mochila, Sin anuncios y el Cántaro.
            RcConfigs c = Bind(
                "eco_starter_window_levels", "[10,18]",
                "eco_starter_timer_h", "24",
                "eco_starter_first_show", "L13_or_session3",
                "eco_noads_card_trigger", "2_interstitials_or_d3",
                "eco_piggy_from_level", "25",
                "eco_piggy_fill", "[10,20,30]");
            Assert.That(c.Errors, Is.Empty, string.Join(" | ", c.Errors));
            Assert.That(c.Offers.StarterWindowTo, Is.EqualTo(18));
            Assert.That(c.Offers.EcoStarterTimerH, Is.EqualTo(24));
            Assert.That(c.Offers.StarterFirstShow.MinLevel, Is.EqualTo(13));
            Assert.That(c.Offers.StarterFirstShow.MinSession, Is.EqualTo(3));
            Assert.That(c.Offers.NoAdsTrigger.Interstitials, Is.EqualTo(2));
            Assert.That(c.Offers.NoAdsTrigger.Day, Is.EqualTo(3));
            Assert.That(c.Offers.EcoPiggyFromLevel, Is.EqualTo(25));
            Assert.That(c.Offers.EcoPiggyFill, Is.EqualTo(new[] { 10, 20, 30 }));

            RcConfigs bad = Bind("eco_piggy_from_level", "40", "eco_starter_timer_h", "24");
            Assert.That(bad.Offers.EcoPiggyFromLevel, Is.EqualTo(20), "08 §9: 15-30");
            Assert.That(bad.Offers.EcoStarterTimerH, Is.EqualTo(48), "toda la clase vuelve a sus defaults");
            Assert.That(bad.Errors.Count, Is.EqualTo(1));
            StringAssert.StartsWith("OffersConfig:", bad.Errors[0]);
        }

        [Test]
        public void AValueThatDoesNotRead_KeepsItsDefault_AndIsReported()
        {
            RcConfigs c = Bind("streak_milestones", "{\"3\":300}", "ads_interstitial_every_n_levels", "[[22]]", "live_daily_challenge_reward", "{\"seeds\":70}");
            Assert.That(c.Streak.StreakMilestones.Hit3Seeds, Is.EqualTo(300));
            Assert.That(c.Ads.AdsInterstitialEveryNLevels.Count, Is.EqualTo(2));
            Assert.That(c.Daily.LiveDailyChallengeReward.Seeds, Is.EqualTo(60));
            Assert.That(c.Errors.Count, Is.EqualTo(3), string.Join(" | ", c.Errors));
            StringAssert.StartsWith("ads_interstitial_every_n_levels:", c.Errors[0]);
        }

        [Test]
        public void AValueOutOfItsRange_FallsBackToTheWholeDefaults()
        {
            RcConfigs c = Bind("canoes_max", "0", "eco_reward_hard", "70");
            Assert.That(c.Economy.CanoesMax, Is.EqualTo(5), "EconomyConfig.Validate: canoes_max >= 1");
            Assert.That(c.Economy.EcoRewardHard, Is.EqualTo(60), "toda la clase vuelve a sus defaults");
            Assert.That(c.Errors.Count, Is.EqualTo(1));
            StringAssert.StartsWith("EconomyConfig:", c.Errors[0]);
        }

        [Test]
        public void TheBusinessGates_AreFoldedIntoAdsAndStore()
        {
            RcConfigs m = Bind("biz_maintenance_mode", "true");
            Assert.That(m.Ads.BizAdsEnabled || m.Ads.BizInterstitialsEnabled || m.Ads.BizRewardedEnabled, Is.False);
            Assert.That(m.Store.BizIapEnabled, Is.False);
            Assert.That(m.Gates.MaintenanceMode, Is.True);

            RcConfigs old = RcConfigBinder.Bind(With("biz_min_app_version", "1.1.0"), "1.0");
            Assert.That(old.Store.BizIapEnabled, Is.False, "por debajo de biz_min_app_version, sin compras");
            Assert.That(old.Ads.BizAdsEnabled, Is.True);
            Assert.That(old.Gates.UpdateRequired, Is.True);

            RcConfigs rv = Bind("biz_rewarded_enabled", "false", "biz_feature_daily_challenge", "false", "biz_feature_win_streak", "false");
            Assert.That(rv.Ads.BizRewardedEnabled, Is.False);
            Assert.That(rv.Ads.BizInterstitialsEnabled, Is.True);
            Assert.That(rv.Daily.BizFeatureDailyChallenge, Is.False);
            Assert.That(rv.Streak.BizFeatureWinStreak, Is.False);

            RcConfigs noRescue = Bind("biz_feature_rescue", "false");
            Assert.That(noRescue.Boosters.RescueMaxPerLevel, Is.EqualTo(0), "biz_feature_rescue: sin Cuia de rescate [P]");
            Assert.That(Bind().Boosters.RescueMaxPerLevel, Is.EqualTo(1));
        }

        // Los valores de las propiedades con [RemoteConfigKey], como texto comparable.
        private static void AssertSame(object expected, object actual)
        {
            foreach (PropertyInfo p in expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetCustomAttribute<RemoteConfigKeyAttribute>() == null)
                {
                    continue;
                }

                Assert.That(Describe(p.GetValue(actual)), Is.EqualTo(Describe(p.GetValue(expected))),
                    expected.GetType().Name + "." + p.Name + ": el default de RemoteConfigDefaults.json no es el del código");
            }
        }

        private static string Describe(object? value)
        {
            switch (value)
            {
                case null: return "null";
                case string s: return "\"" + s + "\"";
                case bool b: return b ? "true" : "false";
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case IEnumerable list:
                {
                    var sb = new StringBuilder("[");
                    foreach (object? item in list)
                    {
                        sb.Append(Describe(item)).Append(',');
                    }

                    return sb.Append(']').ToString();
                }
            }

            MethodInfo? format = value.GetType().GetMethod("Format", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (format != null && format.ReturnType == typeof(string))
            {
                return (string)format.Invoke(value, null);
            }

            var props = new StringBuilder("{");
            foreach (PropertyInfo p in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length == 0)
                {
                    props.Append(p.Name).Append('=').Append(Describe(p.GetValue(value))).Append(';');
                }
            }

            return props.Append('}').ToString();
        }
    }
}
