#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AF.Core.Mechanics;
using AF.Levels;
using AF.RemoteConfig;
using AF.RemoteConfig.Binding;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La reserva del 20 % en el cliente (D-033, añadido «la reserva del 20 %», Q-B11-7 a; 03 §17; D-020, pregunta 31 b): los
    /// paquetes de reserva publicados (<c>reserva_03_v1</c> … <c>reserva_09_v1</c>, <c>kind</c> = <c>reserve</c>,
    /// <c>pack_schema</c> 4), que el catálogo nunca toma como campaña y que <c>level_overrides</c> puede pedir para su slot, y
    /// las claves de Remote Config del catálogo (<see cref="RcLevelBinder"/>: <c>mech_enabled.*</c> y <c>level_overrides</c>).
    /// </summary>
    public sealed class ReservePackTests
    {
        private static List<LevelPack>? s_campaign;
        private static List<LevelPack>? s_reserve;

        private static List<LevelPack> Campaign => s_campaign ??= ReadAll(TestPacks.CampaignPaths);

        private static List<LevelPack> Reserve => s_reserve ??= ReadAll(ReservePaths());

        /// <summary>Los de <c>GameSceneBuilder.ReservePackPaths</c>, en su orden (mundos 3-9).</summary>
        private static readonly string[] ReserveFiles =
        {
            "reserva_03_v1.json", "reserva_04_v1.json", "reserva_05_v1.json", "reserva_06_v1.json", "reserva_07_v1.json",
            "reserva_08_v1.json", "reserva_09_v1.json",
        };

        [Test]
        public void ReservePacks_AreOnePerWorldWithMechanics_AndTheSceneCarriesThem_03_17()
        {
            Assert.That(Reserve.Count, Is.EqualTo(7));
            string scene = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Scenes/Game.unity"));
            int at = scene.IndexOf("reserveLevelPacks:", StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThan(0), "Game.unity regenerada con los paquetes de reserva (GameSceneBuilder.CreateGameScene)");
            int cursor = at;
            foreach (string path in ReservePaths())
            {
                Match guid = Regex.Match(File.ReadAllText(path + ".meta"), "^guid: ([0-9a-f]{32})$", RegexOptions.Multiline);
                Assert.That(guid.Success, Is.True, "el .meta lo genera Unity");
                int next = scene.IndexOf("guid: " + guid.Groups[1].Value, cursor, StringComparison.Ordinal);
                Assert.That(next, Is.GreaterThan(cursor), path + " en reserveLevelPacks, en orden");
                cursor = next;
            }

            for (int i = 0; i < Reserve.Count; i++)
            {
                LevelPack pack = Reserve[i];
                int world = i + 3;
                Assert.That(pack.IsReserve, Is.True, pack.PackId);
                Assert.That(pack.Kind, Is.EqualTo(LevelPack.KindReserve));
                Assert.That(pack.PackId, Is.EqualTo("reserva_0" + world + "_v1"));
                Assert.That(pack.World, Is.EqualTo(world));
            }

            Assert.That(File.Exists(Path.Combine(Application.dataPath, "_Project/Data/Levels/reserva_02_v1.json")), Is.False,
                "el mundo 2 solo lleva capacidades, sin kill switch: no tiene reserva");
        }

        [Test]
        public void EachReserveBoard_ReplacesAKillSwitchedLevelOfItsSlot_WithoutAnyMechanic()
        {
            var byId = new Dictionary<string, LevelDefinition>();
            foreach (LevelPack pack in Campaign)
            {
                foreach (LevelDefinition level in pack.Levels)
                {
                    byId[level.Id] = level;
                }
            }

            int boards = 0;
            foreach (LevelPack pack in Reserve)
            {
                foreach (LevelDefinition r in pack.Levels)
                {
                    boards++;
                    Assert.That(r.IsReserve, Is.True, r.Id);
                    Assert.That(r.Replaces, Is.Not.Null);
                    Assert.That(r.Id, Is.EqualTo(r.Replaces + "_r1"), "id del slot + _r1");
                    Assert.That(byId.TryGetValue(r.Replaces!, out LevelDefinition published), Is.True, r.Replaces + " publicado");
                    Assert.That(published.Level, Is.EqualTo(r.Level), "mismo slot");
                    Assert.That(published.World, Is.EqualTo(pack.World));
                    Assert.That(MechanicSwitches.UsedMask(published), Is.Not.EqualTo(0), r.Replaces + " usa una mecánica con kill switch");
                    Assert.That(MechanicSwitches.UsedMask(r), Is.EqualTo(0), r.Id + " sin mecánicas ni Barro");
                    Assert.That(r.IntroMechanic, Is.Null);
                    Assert.That(r.Skippable, Is.True);
                    Assert.That(r.HasHiddenDoses, Is.False);
                    Assert.That(r.Band, Is.EqualTo(published.Band), "la banda del slot");
                    if (r.Level >= 31)
                    {
                        Assert.That(r.Relief.Count, Is.GreaterThan(0), "alivio desde el 31 (03 §13)");
                    }
                }
            }

            Assert.That(boards, Is.EqualTo(132), "8 + 12 + 12 + 20 + 24 + 28 + 28 (el 20 % de 3-9)");
        }

        [Test]
        public void EachReservePack_IsAtMost20PercentOfItsWorld()
        {
            foreach (LevelPack pack in Reserve)
            {
                LevelPack world = Campaign.Find(p => p.World == pack.World && !p.IsReserve && p.PackId.StartsWith("mundo_", StringComparison.Ordinal))!;
                Assert.That(pack.Count, Is.LessThanOrEqualTo(world.Count * 20 / 100), pack.PackId);
            }
        }

        [Test]
        public void Catalog_RejectsAReservePack_AsCampaign()
        {
            var packs = new List<LevelPack>(Campaign) { Reserve[0] };
            Assert.That(() => LevelCatalog.Create(packs),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("reserva_03_v1 es un paquete de reserva"));
        }

        [Test]
        public void Overrides_ServeTheReserveBoard_WhenItsMechanicIsOff_KS1()
        {
            var pool = new List<LevelPack>(Campaign);
            pool.AddRange(Reserve);
            LevelOverrides table = LevelOverrides.Create("{\"43\": \"m03_v1_n0043_r1\", \"63\": \"m03_v1_n0063_r1\"}", pool);
            Assert.That(table.Problems, Is.Empty);
            Assert.That(table.Count, Is.EqualTo(2));

            MechanicSwitches hojaOff = MechanicSwitches.AllOn.With(MechanicSwitch.Hoja, false);
            var warnings = new List<string>();
            LevelCatalog catalog = LevelCatalog.Create(Campaign, 0, table, hojaOff, warnings.Add);
            Assert.That(catalog.Find(43)!.Id, Is.EqualTo("m03_v1_n0043_r1"), "la reserva sin la Hoja");
            Assert.That(catalog.IsOverridden(43), Is.True);
            Assert.That(catalog.FindPublished(43)!.Id, Is.EqualTo("m03_v1_n0043"));
            Assert.That(catalog.Find(63)!.Id, Is.EqualTo("m03_v1_n0063"), "el Matapi sigue encendido: el 63 no cambia");
            Assert.That(catalog.Find(44)!.Id, Is.EqualTo("m03_v1_n0044"), "sin override: el publicado, con aviso");
            Assert.That(catalog.FallbackLevels, Does.Contain(44));
            Assert.That(catalog.Count, Is.EqualTo(700), "la reserva no añade niveles de campaña");

            LevelCatalog allOn = LevelCatalog.Create(Campaign, 0, table, MechanicSwitches.AllOn, null);
            Assert.That(allOn.Find(43)!.Id, Is.EqualTo("m03_v1_n0043"), "con todo encendido ningún nivel cambia");
        }

        [Test]
        public void Overrides_DiscardAReserveBoard_AskedForAnotherSlot()
        {
            var pool = new List<LevelPack>(Campaign);
            pool.AddRange(Reserve);
            LevelOverrides table = LevelOverrides.Create("{\"44\": \"m03_v1_n0043_r1\"}", pool);
            Assert.That(table.Count, Is.EqualTo(0));
            Assert.That(table.Problems.Count, Is.EqualTo(1));
            Assert.That(table.Problems[0], Does.Contain("es la reserva del slot 43"));
            Assert.That(table.TryGet(44, out _), Is.False);

            LevelOverrides missing = LevelOverrides.Create("{\"43\": \"m03_v1_n0043_r1\"}", Campaign);
            Assert.That(missing.Count, Is.EqualTo(0), "sin los paquetes de reserva en el pool, el id no existe");
            Assert.That(missing.Problems[0], Does.Contain("no está en ningún paquete"));
        }

        [Test]
        public void Reader_KindReserve_NeedsPackSchema4_Replaces_AndNoIntro_ContractNumber21()
        {
            string text = File.ReadAllText(ReservePaths()[0]);
            Assert.That(LevelPackReader.Read(text).IsReserve, Is.True);
            Assert.That(() => LevelPackReader.Read(text.Replace("\"pack_schema\":4", "\"pack_schema\":3")),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("necesita pack_schema 4"));
            Assert.That(() => LevelPackReader.Read(text.Replace("\"replaces\":\"m03_v1_n0043\",", string.Empty)),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("necesita 'replaces'"));
            Assert.That(() => LevelPackReader.Read(text.Replace(
                    "\"intro_mechanic\":null,\"skippable\":true", "\"intro_mechanic\":\"hoja\",\"skippable\":true")),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("no estrena nada"));

            string campaign = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Levels/mundo_03_v1.json"));
            Assert.That(() => LevelPackReader.Read(campaign.Replace(
                    "\"id\":\"m03_v1_n0041\",", "\"id\":\"m03_v1_n0041\",\"replaces\":\"m03_v1_n0041\",")),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("'replaces' solo va en un paquete de reserva"));
        }

        [Test]
        public void RcLevelBinder_ReadsMechEnabledAndLevelOverrides_FromRemoteConfig_08_9()
        {
            var defaults = new RemoteConfigService();
            Assert.That(RcLevelBinder.MechEnabled(defaults.Freeze(), MechanicSwitches.AllOn), Is.EqualTo(MechanicSwitches.AllOn));
            Assert.That(RcLevelBinder.LevelOverridesJson(defaults.Freeze()), Is.EqualTo("{}"));

            var backend = new FakeRemoteConfigBackend()
                .Set("mech_enabled.matapi", "false")
                .Set("mech_enabled.tinaja", "false")
                .Set("level_overrides", "{\"63\":\"m03_v1_n0063_r1\"}");
            var rc = new RemoteConfigService(null, backend);
            rc.Activate(backend.Remote);
            var errors = new List<string>();
            MechanicSwitches switches = RcLevelBinder.MechEnabled(rc.Freeze(), MechanicSwitches.AllOn, errors);
            Assert.That(errors, Is.Empty);
            Assert.That(switches.IsEnabled(MechanicSwitch.Matapi), Is.False);
            Assert.That(switches.IsEnabled(MechanicSwitch.Tinaja), Is.False);
            Assert.That(switches.IsEnabled(MechanicSwitch.Hoja), Is.True);
            Assert.That(RcLevelBinder.MechEnabled(rc.Freeze(), MechanicSwitches.AllOn.With(MechanicSwitch.Hoja, false)).IsEnabled(MechanicSwitch.Hoja),
                Is.False, "lo que el cliente apaga sigue apagado");

            string json = RcLevelBinder.LevelOverridesJson(rc.Freeze());
            var pool = new List<LevelPack>(Campaign);
            pool.AddRange(Reserve);
            LevelCatalog catalog = LevelCatalog.Create(Campaign, 0, LevelOverrides.Create(json, pool), switches, null);
            Assert.That(catalog.Find(63)!.Id, Is.EqualTo("m03_v1_n0063_r1"), "Remote Config apaga el Matapi y apunta a la reserva");
        }

        private static string[] ReservePaths()
        {
            var paths = new string[ReserveFiles.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = Path.Combine(Application.dataPath, "_Project/Data/Levels", ReserveFiles[i]);
            }

            return paths;
        }

        private static List<LevelPack> ReadAll(IReadOnlyList<string> paths)
        {
            var packs = new List<LevelPack>(paths.Count);
            for (int i = 0; i < paths.Count; i++)
            {
                packs.Add(LevelPackReader.Read(File.ReadAllText(paths[i])));
            }

            return packs;
        }
    }
}
