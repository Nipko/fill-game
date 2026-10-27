#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AF.Platform;
using NUnit.Framework;
using UnityEngine;

namespace AF.RemoteConfig.Tests
{
    /// <summary>
    /// <c>RemoteConfigDefaults.json</c> (08 §9) y su copia compilada: son el mismo texto, se leen enteros, tienen las claves de
    /// la tabla y ninguna propiedad con <c>[RemoteConfigKey]</c> del cliente nombra una clave que no esté en ellos.
    /// </summary>
    public sealed class RemoteConfigDefaultsTests
    {
        internal static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Test]
        public void TheCompiledJson_IsTheFile_ByteForByte()
        {
            string path = Path.Combine(RepoRoot, "Assets", "_Project", "Data", "Config", "RemoteConfigDefaults.json");
            Assert.That(File.Exists(path), Is.True, path);
            string file = Encoding.UTF8.GetString(File.ReadAllBytes(path));
            Assert.That(RemoteConfigDefaults.CompiledJson, Is.EqualTo(file), "vuelve a ejecutar python tools/remote-config/rc_defaults.py");
            Assert.That(file.IndexOf('\r'), Is.EqualTo(-1), "LF");
            Assert.That(file[0], Is.Not.EqualTo('﻿'), "sin BOM");
        }

        [Test]
        public void TheDefaults_ReadEntire_WithTheTableKeys()
        {
            RemoteConfigDefaults d = RemoteConfigDefaults.Compiled;
            Assert.That(d.Count, Is.GreaterThan(240), "la tabla de 08 §9 entera");
            Assert.That(d.Params[0].Key, Is.EqualTo("rc_schema"), "en el orden de 08 §9");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (RcParam p in d.Params)
            {
                Assert.That(Regex.IsMatch(p.Key, "^[a-z0-9_.]+$"), Is.True, p.Key + ": sin plantillas ni espacios");
                Assert.That(seen.Add(p.Key), Is.True, p.Key + " repetida");
                Assert.That(p.Default.HasValue ^ (p.Pending != null), Is.True, p.Key + ": o default o pendiente");
                if (p.Default.HasValue)
                {
                    Assert.That(p.Default.Value.Type, Is.EqualTo(p.Type), p.Key);
                }
            }

            Assert.That(d.PendingTemplates, Is.EqualTo(new[] { "eco_price_tier_<country>" }));
        }

        [TestCase("rc_schema", "1")]
        [TestCase("biz_min_app_version", "1.0.0")]
        [TestCase("biz_maintenance_mode", "false")]
        [TestCase("biz_maintenance_message", "")]
        [TestCase("biz_feature_lives", "true")]
        [TestCase("biz_feature_shop", "true")]
        [TestCase("mech_debut_level_tinaja", "323")]
        [TestCase("mech_enabled_matapi", "true")]
        [TestCase("ads_rv_cap_daily_chest", "1")]
        [TestCase("ads_rv_cooldown_s_extra_canoe", "90")]
        [TestCase("ads_rv_cooldown_s_hint", "60")]
        [TestCase("live_unlock_level_trail", "41")]
        [TestCase("live_unlock_level_village", "31")]
        [TestCase("relief_second_step_fails", "0")]
        [TestCase("ui_murky_bonus_enabled", "false")]
        [TestCase("anim_tint_skip_alarm_pct", "0.4")]
        [TestCase("lvl_daily_bracket_levels", "[41,201]")]
        [TestCase("streak_milestones", "{\"3\":300,\"6\":\"chest\",\"10\":\"extra_cuia+rio_crecido\"}")]
        [TestCase("biz_country_enabled", "{\"*\":true}")] // D-057: salida mundial
        [TestCase("biz_market_phase", "global")]
        [TestCase("consent_digital_age_by_country", "{\"*\":13,\"AT\":14,\"BE\":13,\"BG\":14,\"BR\":18,\"CA\":14,\"CH\":16,\"CO\":18,\"CY\":14,\"CZ\":15,\"DE\":16,\"DK\":13,\"EE\":13,\"ES\":14,\"FI\":13,\"FR\":15,\"GR\":15,\"HR\":16,\"HU\":16,\"IE\":16,\"IN\":18,\"IS\":13,\"IT\":14,\"JP\":16,\"KR\":14,\"LI\":16,\"LT\":14,\"LU\":16,\"LV\":13,\"MT\":13,\"MX\":18,\"NL\":16,\"NO\":15,\"PE\":14,\"PL\":16,\"PT\":13,\"RO\":16,\"SE\":13,\"SI\":16,\"SK\":16,\"UK\":13}")]
        [TestCase("eco_model_version", "B-2026-10-06e")]
        public void SomeDefaults_AreTheOnesOf08_9(string key, string expected)
        {
            RcParam? p = RemoteConfigDefaults.Compiled.Find(key);
            Assert.That(p, Is.Not.Null, key);
            Assert.That(p!.Default!.Value.ToRemoteString(), Is.EqualTo(expected), key);
        }

        [TestCase("eco_calendar_table_v1", "tabla 05 §2.3")]
        [TestCase("biz_kpi_targets", "01 §KPIs")]
        [TestCase("biz_kpi_alert_thresholds", "09 tabla 6.1")]
        [TestCase("league_prizes", "tablas 06 §6 y §5.2")]
        [TestCase("regatta_prizes", "tablas 06 §6 y §5.2")]
        public void TheDefaultsThatPointToAnotherTable_ArePending_NotInvented(string key, string pendingStart)
        {
            RcParam p = RemoteConfigDefaults.Compiled.Find(key)!;
            Assert.That(p.Default.HasValue, Is.False, key);
            StringAssert.StartsWith(pendingStart, p.Pending, key);
        }

        [Test]
        public void EveryRemoteConfigKey_OfTheClient_IsInTheDefaults()
        {
            int count = 0;
            foreach (KeyValuePair<string, KeyValuePair<Type, List<string>>> t in RemoteConfigKeyScan.Types())
            {
                foreach (string key in t.Value.Value)
                {
                    count++;
                    Assert.That(RemoteConfigDefaults.Compiled.Find(key), Is.Not.Null, t.Key + ": «" + key + "» no está en RemoteConfigDefaults.json");
                }
            }

            Assert.That(count, Is.GreaterThanOrEqualTo(88), "las propiedades con [RemoteConfigKey] de las 8 clases de cifras");
        }

        [Test]
        public void TheScan_SeesAtLeastTheSixAssembliesOfToday()
        {
            IReadOnlyList<System.Reflection.Assembly> scanned = RemoteConfigKeyScan.Assemblies();
            foreach (System.Reflection.Assembly known in RemoteConfigKeyScan.Known())
            {
                Assert.That(scanned, Has.Member(known), known.GetName().Name + ": el escaneo no puede salir vacío en silencio");
            }

            Assert.That(RemoteConfigKeyScan.Known().Count, Is.EqualTo(6));
        }

        [Test]
        public void Parse_RejectsWhatIsNotTheFormat()
        {
            Assert.Throws<FormatException>(() => RemoteConfigDefaults.Parse("{}"));
            Assert.Throws<FormatException>(() => RemoteConfigDefaults.Parse("{\"format\":\"af-rc-defaults/1\"}"));
            Assert.Throws<FormatException>(() => RemoteConfigDefaults.Parse(
                "{\"format\":\"af-rc-defaults/1\",\"params\":{\"a\":{\"type\":\"int\",\"default\":1.5}}}"));
            Assert.Throws<FormatException>(() => RemoteConfigDefaults.Parse(
                "{\"format\":\"af-rc-defaults/1\",\"params\":{\"a\":{\"type\":\"int\",\"default\":null}}}"));
            Assert.Throws<FormatException>(() => RemoteConfigDefaults.Parse(
                "{\"format\":\"af-rc-defaults/1\",\"params\":{\"a\":{\"type\":\"número\",\"default\":1}}}"));
            RemoteConfigDefaults ok = RemoteConfigDefaults.Parse(
                "{\"format\":\"af-rc-defaults/1\",\"params\":{\"a\":{\"type\":\"int\",\"default\":2},\"b\":{\"type\":\"json\",\"default\":null,\"pending\":\"tabla\"}}}");
            Assert.That(ok.Count, Is.EqualTo(2));
            Assert.That(ok.Find("a")!.Default!.Value.Integer, Is.EqualTo(2));
            Assert.That(ok.Find("b")!.Pending, Is.EqualTo("tabla"));
        }
    }
}
