#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using AF.Ads;
using AF.Boosters;
using AF.Economy;
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
            { "VerificationQueueConfig", "F3-1 (AF.Backend, sec_purchase_deferred_ack_h): ya integrada; se enlaza cuando F3-3 conecte el backend en el arranque" },
            { "TakeoverConfig", "B4 (AF.GameFlow, ui_max_takeovers_per_session): ya integrada; falta su tabla en RcConfigBinder y pasarla a la TakeoverQueue (rango 08 §9: solo 1)" },
            { "LoginCalendarConfig", "B4 (AF.LiveOps, Bitácora: biz_feature_login_calendar, eco_calendar_table_v2 y eco_calendar_locked_booster_seeds): ya integrada; falta su tabla en RcConfigBinder y pasarla a SetUpLogbook" },
            { "RevocationConfig", "B8 (AF.Economy, sec_ssv_grace_h): ya integrada; LocalRevocation aún no se crea en el arranque (llega con restoreLedger y el SSV del backend)" },
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
            AssertSame(DailyChallengeConfig.Defaults, c.Daily);
            AssertSame(StreakConfig.Defaults, c.Streak);
            AssertSame(BasketConfig.Defaults, c.Basket);
            AssertSame(ClockSecurityConfig.Defaults, c.ClockSecurity);
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
