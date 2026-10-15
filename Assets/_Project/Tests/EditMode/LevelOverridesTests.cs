#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AF.Core.Mechanics;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Kill switch de mecánicas en el cliente (tarea KS1; D-020, pregunta 31 b; 08 §2.2, §4.4 y §9): los defaults
    /// <c>mech_enabled.*</c> de <see cref="GameConfig"/>, la tabla <c>level_overrides</c> (<see cref="LevelOverrides"/>), la
    /// sustitución de <see cref="LevelCatalog"/> (con todo encendido ningún nivel cambia; con una mecánica apagada, el override o
    /// el publicado con aviso) y el extra de desarrollo <c>af_mech_off</c>. Cargar un nivel no asigna memoria.
    /// </summary>
    public sealed class LevelOverridesTests
    {
        private static LevelDefinition? s_sink;

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [Test]
        public void Switches_HaveTheNineKeysOf08Section9_InOrder_AndAllAreOnByDefault()
        {
            string[] expected = { "hoja", "matapi", "liana", "remolino", "barro", "lluvia", "brote", "pirana", "tinaja" };
            Assert.That(MechanicSwitches.Count, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                var mechanic = (MechanicSwitch)i;
                Assert.That(MechanicSwitches.IdOf(mechanic), Is.EqualTo(expected[i]));
                Assert.That(MechanicSwitches.KeyOf(mechanic), Is.EqualTo("mech_enabled." + expected[i]));
                Assert.That(MechanicSwitches.TryParse(expected[i], out MechanicSwitch parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(mechanic));
                Assert.That(MechanicSwitches.TryParse(" mech_enabled." + expected[i] + " ", out parsed), Is.True, "clave completa");
                Assert.That(parsed, Is.EqualTo(mechanic));
                Assert.That(GameConfig.MechEnabled.IsEnabled(mechanic), Is.True, "default de 08 §9: true");
            }

            Assert.That(GameConfig.MechEnabled, Is.EqualTo(MechanicSwitches.AllOn));
            Assert.That(GameConfig.MechEnabled.AnyOff, Is.False);
            Assert.That(MechanicSwitches.TryParse("Hoja", out _), Is.False, "ordinal: sin mayúsculas");
            Assert.That(MechanicSwitches.TryParse("murky", out _), Is.False, "Aguas turbias no tiene kill switch");
            Assert.That(MechanicSwitches.TryParse("chica", out _), Is.False);
            Assert.That(MechanicSwitches.TryParse(null, out _), Is.False);
        }

        [Test]
        public void OffList_TurnsOffTheKnownIds_AndReturnsTheUnknownOnes()
        {
            MechanicSwitches off = MechanicSwitches.AllOn.WithOffList("hoja, pirana,,mech_enabled.tinaja,foo, Barro", out string? unknown);
            Assert.That(off.IsEnabled(MechanicSwitch.Hoja), Is.False);
            Assert.That(off.IsEnabled(MechanicSwitch.Pirana), Is.False);
            Assert.That(off.IsEnabled(MechanicSwitch.Tinaja), Is.False);
            Assert.That(off.IsEnabled(MechanicSwitch.Barro), Is.True, "«Barro» no es el id");
            Assert.That(off.IsEnabled(MechanicSwitch.Matapi), Is.True);
            Assert.That(unknown, Is.EqualTo("foo,Barro"));
            Assert.That(off.With(MechanicSwitch.Hoja, true).IsEnabled(MechanicSwitch.Hoja), Is.True);

            Assert.That(MechanicSwitches.AllOn.WithOffList(null, out unknown), Is.EqualTo(MechanicSwitches.AllOn));
            Assert.That(unknown, Is.Null);
            Assert.That(MechanicSwitches.AllOn.WithOffList("  ", out unknown), Is.EqualTo(MechanicSwitches.AllOn));
        }

        [Test]
        public void DevExtra_MechOff_ResolvesTheSwitches_AndTheLocalSourceIsEmpty()
        {
            Assert.That(LaunchOptions.IntentExtraMechOff, Is.EqualTo("af_mech_off"));
            Assert.That(LaunchOptions.ResolveMechSwitches(), Is.EqualTo(MechanicSwitches.AllOn), "sin extra: los defaults");

            LaunchOptions.MechOffOverride = "matapi,nada";
            LogAssert.Expect(LogType.Warning, new Regex("af_mech_off: mecánicas desconocidas \\(\"nada\"\\)"));
            LogAssert.Expect(LogType.Warning, new Regex("mech_enabled apagadas: mech_enabled\\.matapi"));
            MechanicSwitches switches = LaunchOptions.ResolveMechSwitches();
            Assert.That(switches.IsEnabled(MechanicSwitch.Matapi), Is.False);
            Assert.That(switches.OffMask, Is.EqualTo(1 << (int)MechanicSwitch.Matapi));

            Assert.That(LevelOverrides.LocalSource, Is.EqualTo("{}"), "default de level_overrides en 08 §9");
            Assert.That(LaunchOptions.ResolveLevelOverridesJson(), Is.EqualTo("{}"));
            LaunchOptions.LevelOverridesJsonOverride = "{\"43\": \"m03_v1_n0042\"}";
            Assert.That(LaunchOptions.ResolveLevelOverridesJson(), Is.EqualTo("{\"43\": \"m03_v1_n0042\"}"));
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.MechOffOverride, Is.Null);
            Assert.That(LaunchOptions.LevelOverridesJsonOverride, Is.Null);
        }

        [Test]
        public void UsedMask_FollowsTheAttrsAndTheMud_OfEveryCampaignLevel()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            int seen = 0;
            for (int level = catalog.FirstLevel; level <= catalog.LastLevel; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                int mask = MechanicSwitches.UsedMask(def);
                seen |= mask;
                Assert.That(Bit(mask, MechanicSwitch.Hoja), Is.EqualTo(def.HasMechanic(MechanicType.Hoja) || def.HasMechanic(MechanicType.HojaAtada)), "hoja en " + level);
                Assert.That(Bit(mask, MechanicSwitch.Matapi), Is.EqualTo(def.HasMechanic(MechanicType.Matapi)), "matapi en " + level);
                Assert.That(Bit(mask, MechanicSwitch.Liana), Is.EqualTo(def.HasMechanic(MechanicType.Liana) || def.HasMechanic(MechanicType.Flor)), "liana en " + level);
                Assert.That(Bit(mask, MechanicSwitch.Remolino), Is.EqualTo(def.HasMechanic(MechanicType.Remolino)), "remolino en " + level);
                Assert.That(Bit(mask, MechanicSwitch.Barro), Is.EqualTo(def.HasMud), "barro en " + level);
                Assert.That(Bit(mask, MechanicSwitch.Lluvia), Is.EqualTo(def.HasMechanic(MechanicType.Lluvia)), "lluvia en " + level);
                Assert.That(Bit(mask, MechanicSwitch.Brote), Is.EqualTo(def.HasMechanic(MechanicType.Brote)), "brote en " + level);
                Assert.That(Bit(mask, MechanicSwitch.Pirana), Is.EqualTo(def.HasMechanic(MechanicType.Pirana)), "piraña en " + level);
                Assert.That(Bit(mask, MechanicSwitch.Tinaja), Is.EqualTo(def.HasMechanic(MechanicType.Tinaja) || def.HasMechanic(MechanicType.Cucharon)), "tinaja en " + level);
                Assert.That(MechanicSwitches.AllOn.BlockedMask(def), Is.EqualTo(0));
            }

            // Los estrenos de 1-200 (02 §9.1): hoja, matapi, liana, remolino, barro, lluvia, brote y piraña; la tinaja es del 323.
            int expected = ((1 << MechanicSwitches.Count) - 1) & ~(1 << (int)MechanicSwitch.Tinaja);
            Assert.That(seen, Is.EqualTo(expected), MechanicSwitches.Describe(seen));
        }

        [Test]
        public void Overrides_ReadSlotToLevelId_AndResolveTheIdsInThePacks()
        {
            LevelOverrides table = LevelOverrides.Create(
                "{\"45\": \"m01_v1_n0003\", \"7\": \"m01_v1_n0001\", \"043\": \"m01_v1_n0002\"}",
                new[] { TestPacks.Tutorial });
            Assert.That(table.Problems.Count, Is.EqualTo(0), string.Join(" | ", table.Problems));
            Assert.That(table.Count, Is.EqualTo(3));
            Assert.That(table.Slots, Is.EqualTo(new[] { 7, 43, 45 }), "de menor a mayor");
            Assert.That(table.TryGet(43, out LevelDefinition def), Is.True);
            Assert.That(def, Is.SameAs(TestPacks.Tutorial.Find(2)));
            Assert.That(table.TryGet(45, out def), Is.True);
            Assert.That(def, Is.SameAs(TestPacks.Tutorial.Find(3)));
            Assert.That(table.TryGet(44, out _), Is.False);

            Assert.That(LevelOverrides.Create(null, new[] { TestPacks.Tutorial }), Is.SameAs(LevelOverrides.Empty));
            Assert.That(LevelOverrides.Create(LevelOverrides.LocalSource, new[] { TestPacks.Tutorial }).Count, Is.EqualTo(0));
            Assert.That(LevelOverrides.Empty.Count, Is.EqualTo(0));
            Assert.That(LevelOverrides.Empty.Problems.Count, Is.EqualTo(0));
        }

        [Test]
        public void Overrides_BadEntries_AreDropped_WithAProblem_AndNeverThrow()
        {
            LevelPack[] pool = { TestPacks.Tutorial };
            LevelOverrides table = LevelOverrides.Create(
                "{\"abc\": \"m01_v1_n0001\", \"0\": \"m01_v1_n0001\", \"-3\": \"m01_v1_n0001\", \"12\": 5, \"13\": \"m09_v1_n0999\", "
                + "\"14\": \"m01_v1_n0004\", \"014\": \"m01_v1_n0005\"}",
                pool);
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.TryGet(14, out LevelDefinition def), Is.True);
            Assert.That(def.Id, Is.EqualTo("m01_v1_n0004"), "vale el primero de un slot repetido");
            Assert.That(table.Problems.Count, Is.EqualTo(6), string.Join(" | ", table.Problems));
            Assert.That(table.Problems[0], Does.Contain("\"abc\""));
            Assert.That(table.Problems[3], Does.Contain("12"));
            Assert.That(table.Problems[4], Does.Contain("m09_v1_n0999"));
            Assert.That(table.Problems[5], Does.Contain("repetido"));

            LevelOverrides broken = LevelOverrides.Create("{\"43\": ", pool);
            Assert.That(broken.Count, Is.EqualTo(0));
            Assert.That(broken.Problems.Count, Is.EqualTo(1));
            Assert.That(broken.Problems[0], Does.StartWith("level_overrides: no es JSON válido"));

            LevelOverrides notObject = LevelOverrides.Create("[\"m01_v1_n0001\"]", pool);
            Assert.That(notObject.Count, Is.EqualTo(0));
            Assert.That(notObject.Problems[0], Does.Contain("{slot: level_id}"));
        }

        [Test]
        public void AllOn_NoLevelChanges_EvenWithOverrides()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            LevelCatalog plain = LevelCatalog.Create(packs);
            int hoja = FirstWith(plain, MechanicSwitch.Hoja);
            LevelOverrides table = LevelOverrides.Create("{\"" + hoja + "\": \"" + plain.Find(1)!.Id + "\"}", packs);
            Assert.That(table.Count, Is.EqualTo(1));

            var warnings = new List<string>();
            LevelCatalog catalog = LevelCatalog.Create(packs, 0, table, GameConfig.MechEnabled, warnings.Add);
            Assert.That(catalog.Count, Is.EqualTo(plain.Count));
            for (int level = plain.FirstLevel; level <= plain.LastLevel; level++)
            {
                Assert.That(catalog.Find(level), Is.SameAs(plain.Find(level)), "nivel " + level);
                Assert.That(catalog.Serve(level), Is.SameAs(plain.Find(level)), "nivel " + level);
                Assert.That(catalog.IsOverridden(level), Is.False);
            }

            Assert.That(catalog.OverriddenLevels.Count, Is.EqualTo(0));
            Assert.That(catalog.FallbackLevels.Count, Is.EqualTo(0));
            Assert.That(warnings, Is.Empty);
        }

        [Test]
        public void MechanicOff_WithOverride_ServesTheOverride_AndWithoutIt_ThePublishedWithAWarning()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            LevelCatalog plain = LevelCatalog.Create(packs);
            int debut = FirstWith(plain, MechanicSwitch.Hoja);
            int other = NextWith(plain, MechanicSwitch.Hoja, debut);
            LevelDefinition replacement = plain.Find(debut - 1)!;
            Assume.That(MechanicSwitches.UsedMask(replacement), Is.EqualTo(0), "el nivel anterior al estreno no usa mecánicas");

            LevelOverrides table = LevelOverrides.Create("{\"" + debut + "\": \"" + replacement.Id + "\"}", packs);
            MechanicSwitches switches = MechanicSwitches.AllOn.With(MechanicSwitch.Hoja, false);
            var warnings = new List<string>();
            LevelCatalog catalog = LevelCatalog.Create(packs, 0, table, switches, warnings.Add);

            // Con override: se sirve el override, sin aviso; el publicado sigue a mano.
            Assert.That(catalog.Find(debut), Is.SameAs(replacement));
            Assert.That(catalog.Serve(debut), Is.SameAs(replacement));
            Assert.That(catalog.IsOverridden(debut), Is.True);
            Assert.That(catalog.FindPublished(debut), Is.SameAs(plain.Find(debut)));
            Assert.That(catalog.PackOf(debut), Is.SameAs(plain.PackOf(debut)), "el paquete es el del publicado");
            Assert.That(warnings, Is.Empty);

            // Sin override: el publicado, con un aviso la primera vez que se carga (no al buscarlo).
            Assert.That(catalog.Find(other), Is.SameAs(plain.Find(other)));
            Assert.That(warnings, Is.Empty, "Find no avisa");
            Assert.That(catalog.Serve(other), Is.SameAs(plain.Find(other)));
            Assert.That(catalog.Serve(other), Is.SameAs(plain.Find(other)));
            Assert.That(warnings.Count, Is.EqualTo(1), "un aviso por nivel");
            Assert.That(warnings[0], Does.StartWith("nivel " + other.ToString(CultureInfo.InvariantCulture) + " (" + plain.Find(other)!.Id + "): usa mech_enabled.hoja = false"));
            Assert.That(warnings[0], Does.Contain("no tiene override en level_overrides"));
            Assert.That(warnings[0], Does.Contain("[P]"));

            // Cada nivel con hoja está en una de las dos listas; el resto no cambia.
            Assert.That(catalog.OverriddenLevels, Is.EqualTo(new[] { debut }));
            for (int level = plain.FirstLevel; level <= plain.LastLevel; level++)
            {
                bool usesHoja = Bit(MechanicSwitches.UsedMask(plain.Find(level)!), MechanicSwitch.Hoja);
                bool fallback = Contains(catalog.FallbackLevels, level);
                Assert.That(fallback, Is.EqualTo(usesHoja && level != debut), "nivel " + level);
                if (level != debut)
                {
                    Assert.That(catalog.Find(level), Is.SameAs(plain.Find(level)), "nivel " + level);
                }
            }
        }

        [Test]
        public void AnOverrideThatUsesAnotherSwitchedOffMechanic_IsNotServed()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            LevelCatalog plain = LevelCatalog.Create(packs);
            int hoja = FirstWith(plain, MechanicSwitch.Hoja);
            int matapi = FirstWith(plain, MechanicSwitch.Matapi);
            Assume.That(Bit(MechanicSwitches.UsedMask(plain.Find(matapi)!), MechanicSwitch.Hoja), Is.False);

            LevelOverrides table = LevelOverrides.Create("{\"" + hoja + "\": \"" + plain.Find(matapi)!.Id + "\"}", packs);
            MechanicSwitches switches = MechanicSwitches.AllOn.WithOffList("hoja,matapi", out _);
            var warnings = new List<string>();
            LevelCatalog catalog = LevelCatalog.Create(packs, 0, table, switches, warnings.Add);

            Assert.That(catalog.Serve(hoja), Is.SameAs(plain.Find(hoja)), "el override también usa una mecánica apagada");
            Assert.That(catalog.IsOverridden(hoja), Is.False);
            Assert.That(warnings.Count, Is.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("su override " + plain.Find(matapi)!.Id + " también usa mech_enabled.matapi = false"));
            Assert.That(catalog.Switches, Is.EqualTo(switches));
        }

        [Test]
        public void SlotsAreCampaignNumbers_WithTheDevOffset()
        {
            // af_level_offset 30: el tutorial se juega como 31-40; el slot 33 es el del tablero del nivel 3.
            LevelPack tutorial = TestPacks.Tutorial;
            LevelOverrides table = LevelOverrides.Create("{\"33\": \"" + tutorial.Find(1)!.Id + "\"}", new[] { tutorial });
            LevelCatalog catalog = LevelCatalog.Create(new[] { tutorial }, 30, table, MechanicSwitches.AllOn.With(MechanicSwitch.Hoja, false), null);
            Assert.That(catalog.Find(33), Is.SameAs(tutorial.Find(3)), "el tutorial no usa la Hoja: nada que sustituir");
            Assert.That(catalog.OverriddenLevels.Count, Is.EqualTo(0));
            Assert.That(catalog.FallbackLevels.Count, Is.EqualTo(0));
        }

        [Test]
        public void LoadingALevel_ServesTheOverride_InTheFlow()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            LevelCatalog plain = LevelCatalog.Create(packs);
            int debut = FirstWith(plain, MechanicSwitch.Matapi);
            LevelDefinition replacement = plain.Find(debut - 1)!;
            LevelOverrides table = LevelOverrides.Create("{\"" + debut + "\": \"" + replacement.Id + "\"}", packs);
            LevelCatalog catalog = LevelCatalog.Create(packs, 0, table, MechanicSwitches.AllOn.With(MechanicSwitch.Matapi, false), null);

            var flow = new LevelFlow(catalog, new MemoryProgressStore(), new RecordingListener());
            flow.Load(debut);
            Assert.That(flow.LevelNumber, Is.EqualTo(debut), "el número de campaña no cambia");
            Assert.That(flow.Level, Is.SameAs(replacement), "se juega el tablero del override");
            Assert.That(flow.Level.HasMechanic(MechanicType.Matapi), Is.False);
        }

        [Test]
        public void LoadingALevel_WithoutOverride_ServesThePublished_AndWarnsOnce_InTheFlow()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            LevelCatalog plain = LevelCatalog.Create(packs);
            int debut = FirstWith(plain, MechanicSwitch.Matapi);
            var warnings = new List<string>();
            LevelCatalog catalog = LevelCatalog.Create(packs, 0, LevelOverrides.Empty, MechanicSwitches.AllOn.With(MechanicSwitch.Matapi, false), warnings.Add);

            var flow = new LevelFlow(catalog, new MemoryProgressStore(), new RecordingListener());
            flow.Load(debut);
            Assert.That(flow.LevelNumber, Is.EqualTo(debut));
            Assert.That(flow.Level, Is.SameAs(plain.Find(debut)), "sin override: el tablero publicado");
            Assert.That(warnings.Count, Is.EqualTo(1), "la carga del nivel escribe el aviso");
            Assert.That(warnings[0], Does.Contain("usa mech_enabled.matapi = false"));

            flow.Load(debut);
            Assert.That(flow.Level, Is.SameAs(plain.Find(debut)));
            Assert.That(warnings.Count, Is.EqualTo(1), "volver a cargar el nivel no repite el aviso");
        }

        [Test]
        public void ResumingALevel_WithoutOverride_WarnsIfNotYetWarned_AndNeverTwice()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            LevelCatalog plain = LevelCatalog.Create(packs);
            int debut = FirstWith(plain, MechanicSwitch.Matapi);
            MechanicSwitches switches = MechanicSwitches.AllOn.With(MechanicSwitch.Matapi, false);
            var store = new MemoryLevelStateStore();

            // Primera sesión: se carga el nivel (un aviso) y queda guardado en curso.
            var firstWarnings = new List<string>();
            LevelCatalog firstCatalog = LevelCatalog.Create(packs, 0, LevelOverrides.Empty, switches, firstWarnings.Add);
            var first = new LevelFlow(firstCatalog, new MemoryProgressStore(), new RecordingListener()) { StateStore = store };
            first.Load(debut);
            Assert.That(first.TapAnywhere(), Is.True);
            Assert.That(firstWarnings.Count, Is.EqualTo(1));
            LevelStateData saved = store.Load()!;
            Assert.That(saved.Level, Is.EqualTo(debut));

            // Con el catálogo que ya avisó, reanudar no repite el aviso.
            var sameCatalog = new LevelFlow(firstCatalog, new MemoryProgressStore(), new RecordingListener()) { StateStore = store };
            Assert.That(sameCatalog.Resume(saved), Is.True);
            Assert.That(firstWarnings.Count, Is.EqualTo(1), "el aviso ya estaba escrito");

            // Se reabre la app (catálogo nuevo): reanudar es la carga del nivel y escribe el aviso; cargarlo después, no.
            var warnings = new List<string>();
            LevelCatalog catalog = LevelCatalog.Create(packs, 0, LevelOverrides.Empty, switches, warnings.Add);
            var again = new LevelFlow(catalog, new MemoryProgressStore(), new RecordingListener()) { StateStore = store };
            Assert.That(again.CheckResume(saved), Is.EqualTo(ResumeCheck.Resumable));
            Assert.That(warnings, Is.Empty, "comprobar el estado no avisa");
            Assert.That(again.Resume(saved), Is.True);
            Assert.That(again.Level, Is.SameAs(plain.Find(debut)), "sin override: el tablero publicado");
            Assert.That(warnings.Count, Is.EqualTo(1), "reanudar escribe el aviso");
            Assert.That(warnings[0], Does.Contain("usa mech_enabled.matapi = false"));

            again.Load(debut);
            Assert.That(warnings.Count, Is.EqualTo(1), "cargar el nivel tras reanudarlo no repite el aviso");
        }

        [Test]
        public void ServingALevel_DoesNotAllocate()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            LevelCatalog plain = LevelCatalog.Create(packs);
            int debut = FirstWith(plain, MechanicSwitch.Hoja);
            int other = NextWith(plain, MechanicSwitch.Hoja, debut);
            LevelOverrides table = LevelOverrides.Create("{\"" + debut + "\": \"" + plain.Find(1)!.Id + "\"}", packs);
            var warnings = new List<string>(4);
            LevelCatalog catalog = LevelCatalog.Create(packs, 0, table, MechanicSwitches.AllOn.With(MechanicSwitch.Hoja, false), warnings.Add);
            LevelCatalog allOn = LevelCatalog.Create(packs, 0, table, MechanicSwitches.AllOn, null);

            TestDelegate call = () =>
            {
                s_sink = catalog.Serve(debut);
                s_sink = catalog.Serve(other);
                s_sink = catalog.Serve(1);
                s_sink = catalog.Find(debut);
                s_sink = allOn.Serve(debut);
                table.TryGet(debut, out LevelDefinition found);
                s_sink = found;
            };
            call(); // calentamiento: el primer Serve de «other» escribe su aviso
            Assert.That(warnings.Count, Is.EqualTo(1));
            Assert.That(call, Is.Not.AllocatingGCMemory());
        }

        private static bool Bit(int mask, MechanicSwitch mechanic)
        {
            return (mask & (1 << (int)mechanic)) != 0;
        }

        private static int FirstWith(LevelCatalog catalog, MechanicSwitch mechanic)
        {
            return NextWith(catalog, mechanic, catalog.FirstLevel - 1);
        }

        private static int NextWith(LevelCatalog catalog, MechanicSwitch mechanic, int after)
        {
            for (int level = after + 1; level <= catalog.LastLevel; level++)
            {
                if (Bit(MechanicSwitches.UsedMask(catalog.Find(level)!), mechanic))
                {
                    return level;
                }
            }

            Assert.Fail("ningún nivel después del " + after + " usa " + MechanicSwitches.IdOf(mechanic));
            return 0;
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
