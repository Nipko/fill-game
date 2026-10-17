#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.GameFlow;
using AF.Levels;
using AF.Levels.Json;
using AF.Localization;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Niveles ocupados de <see cref="SightingRule"/> contrastados con la especificación (tarea F5; propuesta de Frutas §3.3 y
    /// paso F5 de §10): los estrenos de la campaña (<c>mechanics[].debut</c> ∪ <c>capacities[].debut</c> de
    /// <c>tools/level-generator/specs/campaign_v1.json</c> y <c>campaign_v2.json</c>) más los hitos de 02 §13 quedan ocupados para
    /// la tarjeta del miquito, incluidos 100, 163, 18, 22, 160, 243 y 283. Y la tarjeta «Fruta nueva» (06 §4.5): sus tintes y sus
    /// niveles son los del glosario §9 y los de los packs (el primer nivel en que sale cada tinte), sale en 2, 4, 7, 11, 21, 41, 61,
    /// 81, 111, 141 y 201 (y en el 301), nunca en el 1 ni fuera del primer intento, y sus textos están en la tabla.
    /// </summary>
    public sealed class OccupiedLevelsTests
    {
        // Hitos con su propia presentación de 02 §13 (Marea 3, Reto del río 7, Cuia extra 8, Mercado 10, contador de Marea y
        // Cosecha doble 11, Morpho 12, Cántaro 20, animación corta 21, Pororoca 30, Canoas y Cuia de rescate 31).
        private static readonly int[] Section13Milestones = { 3, 7, 8, 10, 11, 12, 20, 21, 30, 31 };

        // Estrenos de campaign_v2 que el cliente aún no tiene: la Tinaja (323, D-019; TJ4 y C1b), y el Brote de dos hojas (343) y
        // la Flor (403), de VA1b. Sus tareas los añaden a SightingRule y los quitan de aquí.
        private static readonly int[] PendingDebuts = { 323, 343, 403 };

        // Los niveles que nombra la tarea F5.
        private static readonly int[] NamedOccupied = { 100, 163, 18, 22, 160, 243, 283 };

        // Niveles de la tarjeta «Fruta nueva» en 1-320 (criterio de aceptación de F5, más el 301 del arazá).
        private static readonly int[] FruitNewLevels = { 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201, 301 };

        private static readonly string[] Packs = { "tutorial_v0", "mundo_02_v1", "mundo_03_v1", "mundo_04_v1", "mundo_05_v1", "mundo_06_v1" };

        private static string SpecPath(string name) => Path.Combine(Application.dataPath, "..", "tools", "level-generator", "specs", name);

        // mechanics[].debut ∪ capacities[].debut de una especificación de campaña.
        private static SortedSet<int> SpecDebuts(string name)
        {
            JsonValue spec = JsonParser.Parse(File.ReadAllText(SpecPath(name)));
            var debuts = new SortedSet<int>();
            foreach (JsonValue m in spec.Get("mechanics")!.Items)
            {
                debuts.Add(m.Get("debut")!.AsInt());
            }

            foreach (JsonValue c in spec.Get("capacities")!.Items)
            {
                debuts.Add(c.Get("debut")!.AsInt());
            }

            return debuts;
        }

        [Test]
        public void OccupiedDebuts_AreTheDebutsOfCampaignV1AndV2()
        {
            SortedSet<int> v1 = SpecDebuts("campaign_v1.json");
            SortedSet<int> v2 = SpecDebuts("campaign_v2.json");
            Assert.That(v1.IsSubsetOf(v2), Is.True, "campaign_v2 conserva los estrenos de 1-200 de campaign_v1");
            var expected = new SortedSet<int>(v1);
            expected.UnionWith(v2);
            foreach (int pending in PendingDebuts)
            {
                Assert.That(expected.Remove(pending), Is.True, "el " + pending + " es un estreno de campaign_v2");
                Assert.That(SightingRule.OccupiedDebutLevels, Has.No.Member(pending), "el " + pending + " entra con su tarea");
            }

            Assert.That(SightingRule.OccupiedDebutLevels, Is.Ordered, "en orden");
            Assert.That(new SortedSet<int>(SightingRule.OccupiedDebutLevels).Count, Is.EqualTo(SightingRule.OccupiedDebutLevels.Length), "sin repetidos");
            Assert.That(new SortedSet<int>(SightingRule.OccupiedDebutLevels), Is.EqualTo(expected), "mechanics[].debut ∪ capacities[].debut");
            foreach (int debut in expected)
            {
                Assert.That(SightingRule.IsBusy(debut), Is.True, "estreno del " + debut + ": sin tarjeta del miquito");
            }
        }

        [Test]
        public void CapacityVariants_AreTheSpecCapacitiesAfterTheFirstOfEach()
        {
            JsonValue spec = JsonParser.Parse(File.ReadAllText(SpecPath("campaign_v1.json")));
            var first = new Dictionary<string, int>();
            var all = new List<(string Id, int Debut)>();
            foreach (JsonValue c in spec.Get("capacities")!.Items)
            {
                string id = c.Get("id")!.AsString();
                int debut = c.Get("debut")!.AsInt();
                all.Add((id, debut));
                first[id] = first.TryGetValue(id, out int seen) ? System.Math.Min(seen, debut) : debut;
            }

            var variants = new SortedSet<int>();
            foreach ((string id, int debut) in all)
            {
                if (debut != first[id])
                {
                    variants.Add(debut);
                }
            }

            Assert.That(variants, Is.EqualTo(new SortedSet<int>(SightingRule.CapacityVariantLevels)), "Cuia chica de 1 y de 3, Cuia alta de 6");
            Assert.That(SightingRule.CapacityVariantLevels, Is.EqualTo(new[] { 18, 22, 160 }));
            foreach (int level in first.Values)
            {
                Assert.That(SightingRule.MechanicDebutLevels, Does.Contain(level), "la primera de cada capacidad lleva su tarjeta de estreno");
            }
        }

        [Test]
        public void Section13Milestones_AreOccupied()
        {
            foreach (int level in Section13Milestones)
            {
                Assert.That(SightingRule.MilestoneLevels, Does.Contain(level), "hito del " + level + " (02 §13)");
                Assert.That(SightingRule.IsBusy(level), Is.True, "hito del " + level);
            }

            // Los hitos son exactamente los de 02 §13 más los estrenos con tarjeta.
            var expected = new SortedSet<int>(Section13Milestones);
            expected.UnionWith(SightingRule.MechanicDebutLevels);
            expected.UnionWith(SightingRule.VariantDebutLevels);
            Assert.That(new SortedSet<int>(SightingRule.MilestoneLevels), Is.EqualTo(expected));
        }

        [Test]
        public void NamedLevels_AreOccupied_AndTheMonkeyCardMovesOn()
        {
            foreach (int level in NamedOccupied)
            {
                Assert.That(SightingRule.IsBusy(level), Is.True, "el " + level + " queda ocupado");
                Assert.That(SightingRule.FirstFreeLevel(level), Is.EqualTo(level + 1), "la tarjeta del miquito pasa al " + (level + 1));
                Assert.That(SightingRule.ShowsCard(level, level, true), Is.False, "ninguna tarjeta del miquito en el " + level);
            }

            Assert.That(SightingRule.IsBusy(19), Is.False);
            Assert.That(SightingRule.IsBusy(23), Is.False);
            Assert.That(SightingRule.IsBusy(161), Is.False);
        }

        [Test]
        public void FruitDebutTints_AreTheGlossaryPalette_InDebutOrder()
        {
            string[] tints = SightingRule.FruitDebutTints;
            int[] levels = SightingRule.FruitDebutTintLevels;
            Assert.That(tints.Length, Is.EqualTo(14), "K_paleta = 14 (glosario §9)");
            Assert.That(levels.Length, Is.EqualTo(tints.Length));
            Assert.That(levels, Is.Ordered, "por orden de estreno");
            var engine = new SortedSet<string>();
            for (int i = 1; i < ProceduralArt.TintIds.Length; i++)
            {
                engine.Add(ProceduralArt.TintIds[i]);
            }

            Assert.That(new SortedSet<string>(tints), Is.EqualTo(engine), "los 14 tintes del glosario §9, una vez cada uno");
            Assert.That(SightingRule.FruitDebutLevels, Is.EqualTo(new[] { 1, 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201, 301 }), "07 §3 y 06 §4.5");
        }

        [Test]
        public void FruitDebutLevels_AreTheFirstLevelOfEachTintInThePacks()
        {
            var firstSeen = new Dictionary<string, int>();
            int lastLevel = 0;
            foreach (string pack in Packs)
            {
                LevelPack p = LevelPackReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Levels", pack + ".json")));
                foreach (LevelDefinition level in p.Levels)
                {
                    lastLevel = Mathf.Max(lastLevel, level.Level);
                    foreach (string tint in level.Palette)
                    {
                        if (!firstSeen.TryGetValue(tint, out int seen) || level.Level < seen)
                        {
                            firstSeen[tint] = level.Level;
                        }
                    }
                }
            }

            for (int i = 0; i < SightingRule.FruitDebutTints.Length; i++)
            {
                string tint = SightingRule.FruitDebutTints[i];
                int debut = SightingRule.FruitDebutTintLevels[i];
                if (debut > lastLevel)
                {
                    Assert.That(firstSeen.ContainsKey(tint), Is.False, tint + ": aún no sale en los packs (estreno en el " + debut + ")");
                    continue;
                }

                Assert.That(firstSeen.TryGetValue(tint, out int seen), Is.True, tint + " sale en los packs");
                Assert.That(seen, Is.EqualTo(debut), tint + ": primer nivel de los packs = estreno del glosario §9");
            }
        }

        [Test]
        public void FruitNew_ShowsOnTheDebutsFromLevel2_FirstAttemptOnly()
        {
            var shown = new List<int>();
            for (int level = 1; level <= 1000; level++)
            {
                int fruit = SightingRule.FruitNewAt(level, true);
                Assert.That(SightingRule.FruitNewAt(level, false), Is.EqualTo(-1), "ni al reintentar ni al reanudar (" + level + ")");
                if (fruit < 0)
                {
                    continue;
                }

                shown.Add(level);
                Assert.That(SightingRule.FruitDebutTintLevels[fruit], Is.EqualTo(level), "el tinte que se estrena en el " + level);
            }

            Assert.That(shown, Is.EqualTo(FruitNewLevels), "una por nivel: 2, 4, 7, 11, 21, 41, 61, 81, 111, 141, 201 (y 301)");
            Assert.That(SightingRule.FruitNewAt(SightingRule.FirstContactLevel, true), Is.EqualTo(-1), "el 1 es del tutorial (07 §6.4)");
            Assert.That(SightingRule.FruitDebutTints[SightingRule.FruitNewAt(2, true)], Is.EqualTo("tapereba"));
            Assert.That(SightingRule.FruitDebutTints[SightingRule.FruitNewAt(4, true)], Is.EqualTo("guanabana"));
            Assert.That(SightingRule.FruitDebutTints[SightingRule.FruitNewAt(7, true)], Is.EqualTo("bacaba"));
            Assert.That(SightingRule.FruitDebutTints[SightingRule.FruitNewAt(11, true)], Is.EqualTo("agua_tapajos"));
            Assert.That(SightingRule.FruitDebutTints[SightingRule.FruitNewAt(201, true)], Is.EqualTo("cacao"));
        }

        [Test]
        public void FruitNewLevels_LeaveNoRoomForTheMonkeyCardOrTheStopLabel()
        {
            foreach (int level in FruitNewLevels)
            {
                Assert.That(SightingRule.IsBusy(level), Is.True, "sin tarjeta del miquito en el " + level + " (Fruta nueva > miquito)");
                Assert.That(StopLabelRule.IsTaken(level), Is.True, "sin rótulo del lugar en el " + level + " (Fruta nueva > rótulo)");
            }
        }

        [Test]
        public void FruitNewKeys_AreTheTablesTagsAndNames()
        {
            for (int i = 0; i < SightingRule.FruitDebutTints.Length; i++)
            {
                string tint = SightingRule.FruitDebutTints[i];
                Assert.That(SightingRule.FruitTagKey(i), Is.EqualTo("fact." + tint + ".tag"), "08 §4.7");
                Assert.That(SightingRule.FruitNameKey(i), Is.EqualTo("tint." + tint + ".name"), "08 §4.7");
                Assert.That(Loc.Has(SightingRule.FruitTagKey(i)), Is.True, tint + ": frase de «Fruta nueva» en facts.csv");
                Assert.That(Loc.Has(SightingRule.FruitNameKey(i)), Is.True, tint + ": nombre visible en facts.csv");
                Assert.That(Loc.Fact(SightingRule.FruitTagKey(i)), Is.Not.Empty, tint);
            }

            Assert.That(Loc.Fact(SightingRule.FruitTagKey(SightingRule.FruitNewAt(2, true))), Is.EqualTo("Taperebá: la ciruela de los tapires"));
            Assert.That(Loc.Fact(SightingRule.FruitNameKey(SightingRule.FruitNewAt(2, true))), Is.EqualTo("Taperebá"));
        }

        [Test]
        public void FruitNewAt_AndItsKeys_DoNotAllocate()
        {
            SightingRule.FruitNewAt(141, true); // tipo ya iniciado
            Assert.That(() =>
            {
                for (int level = 1; level <= 320; level++)
                {
                    int fruit = SightingRule.FruitNewAt(level, true);
                    if (fruit >= 0)
                    {
                        SightingRule.FruitTagKey(fruit);
                        SightingRule.FruitNameKey(fruit);
                    }
                }
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
