#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.RemoteConfig.Tests
{
    /// <summary>
    /// <see cref="BizGates"/> (08 §9): mantenimiento, versión mínima, kill switches de anuncios e IAP y los ocho
    /// <c>biz_feature_&lt;sistema&gt;</c>.
    /// </summary>
    public sealed class BizGatesTests
    {
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

        [Test]
        public void TheDefaults_LeaveEverythingOn()
        {
            var g = new BizGates(new RemoteConfigService().Freeze(), "1.0");
            Assert.That(g.MaintenanceMode, Is.False);
            Assert.That(g.MaintenanceMessage, Is.EqualTo(""));
            Assert.That(g.MinAppVersion, Is.EqualTo("1.0.0"));
            Assert.That(g.UpdateRequired, Is.False, "1.0 = 1.0.0");
            Assert.That(g.IapEnabled && g.AdsEnabled && g.InterstitialsEnabled && g.RewardedEnabled, Is.True);
            for (int i = 0; i < BizGates.FeatureCount; i++)
            {
                Assert.That(g.Feature((BizFeature)i), Is.True, BizGates.KeyOf((BizFeature)i));
            }
        }

        [Test]
        public void TheFeatureKeys_AreTheOnesOf08_9()
        {
            var keys = new List<string>();
            for (int i = 0; i < BizGates.FeatureCount; i++)
            {
                keys.Add(BizGates.KeyOf((BizFeature)i));
                Assert.That(RemoteConfigDefaults.Compiled.Find(keys[i]), Is.Not.Null, keys[i]);
            }

            Assert.That(keys, Is.EqualTo(new[]
            {
                "biz_feature_daily_challenge", "biz_feature_login_calendar", "biz_feature_win_streak", "biz_feature_lives",
                "biz_feature_rescue", "biz_feature_murky", "biz_feature_piggy", "biz_feature_shop",
            }));
            Assert.Throws<ArgumentOutOfRangeException>(() => BizGates.KeyOf((BizFeature)8));
        }

        [Test]
        public void Maintenance_BlocksAdsAndIap_AndTheGameGoesOn()
        {
            var g = new BizGates(With("biz_maintenance_mode", "true", "biz_maintenance_message", "maintenance_generic"), "1.0");
            Assert.That(g.MaintenanceMode, Is.True);
            Assert.That(g.MaintenanceMessage, Is.EqualTo("maintenance_generic"));
            Assert.That(g.AdsEnabled || g.InterstitialsEnabled || g.RewardedEnabled || g.IapEnabled, Is.False);
            Assert.That(g.Feature(BizFeature.DailyChallenge) && g.Feature(BizFeature.Lives), Is.True, "el juego offline sigue");
        }

        [Test]
        public void BelowTheMinimumVersion_AnUpdateIsRequired_AndPurchasesStop()
        {
            var g = new BizGates(With("biz_min_app_version", "1.2.0"), "1.1.9");
            Assert.That(g.UpdateRequired, Is.True);
            Assert.That(g.IapEnabled, Is.False, "compras bloqueadas");
            Assert.That(g.AdsEnabled, Is.True);
            Assert.That(new BizGates(With("biz_min_app_version", "1.2.0"), "1.2").UpdateRequired, Is.False);
        }

        [Test]
        public void EachKillSwitch_TurnsOffOnlyItsPart()
        {
            Assert.That(new BizGates(With("biz_interstitials_enabled", "false"), "1.0").InterstitialsEnabled, Is.False);
            Assert.That(new BizGates(With("biz_interstitials_enabled", "false"), "1.0").RewardedEnabled, Is.True);
            Assert.That(new BizGates(With("biz_ads_enabled", "false"), "1.0").RewardedEnabled, Is.False);
            Assert.That(new BizGates(With("biz_iap_enabled", "false"), "1.0").IapEnabled, Is.False);
            var g = new BizGates(With("biz_feature_rescue", "false", "biz_feature_shop", "false"), "1.0");
            Assert.That(g.Feature(BizFeature.Rescue), Is.False);
            Assert.That(g.Feature(BizFeature.Shop), Is.False);
            Assert.That(g.Feature(BizFeature.Lives), Is.True);
            Assert.That(g.IapEnabled, Is.True, "el Mercado tiene su interruptor; las compras del modal siguen");
        }

        [TestCase("1.0", "1.0.0", 0)]
        [TestCase("1.0.1", "1.0.0", 1)]
        [TestCase("1.0.0", "1.0.1", -1)]
        [TestCase("1.10.0", "1.9.9", 1)]
        [TestCase("2", "1.99.99", 1)]
        [TestCase("1.2-dev", "1.2.0", 0)]
        [TestCase("1.0", "", 0)]
        [TestCase("1.0", "x", 0)]
        public void CompareVersions_ByNumbers(string version, string minimum, int expected)
        {
            Assert.That(Math.Sign(BizGates.CompareVersions(version, minimum)), Is.EqualTo(expected));
        }
    }
}
