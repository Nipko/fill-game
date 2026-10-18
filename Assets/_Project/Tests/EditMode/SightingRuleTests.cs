#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AF.GameFlow;
using AF.Levels;
using AF.Levels.Json;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Regla de «nivel libre» de la tarjeta «¡Miquito a la vista!» (guion de la intro §4.5 y §14): sin banner de mundo, sin
    /// Fruta nueva, sin hito o debut y sin tutorial pendiente; una sola tarjeta por nivel y solo en el primer intento.
    /// </summary>
    public sealed class SightingRuleTests
    {
        // Estreno de la Tinaja (D-019), que entra con TJ4 y C1b.
        private const int PendingTinajaDebut = 323;

        private static string CampaignV2Path => Path.Combine(Application.dataPath, "..", "tools", "level-generator", "specs", "campaign_v2.json");

        [Test]
        public void BusyLevels_AreTheCanonicalBannersDebutsAndMilestones()
        {
            Assert.That(SightingRule.IsBusy(1), Is.True, "Fruta nueva y tutorial (sin banner: decisión del dueño)");
            Assert.That(SightingRule.IsBusy(11), Is.True, "banner del Tapajós");
            Assert.That(SightingRule.IsBusy(12), Is.True, "Morpho");
            Assert.That(SightingRule.IsBusy(41), Is.True, "banner de Manaos y urucum");
            Assert.That(SightingRule.IsBusy(43), Is.True, "Hoja");
            Assert.That(SightingRule.IsBusy(5), Is.False);
            Assert.That(SightingRule.IsBusy(13), Is.False);
        }

        /// <summary>
        /// La instrucción del tutorial con TiTino (TU1a; 02 §13; guion de la intro §14) ocupa el 1, el 2, el 3 y el 6: ninguna
        /// tarjeta del miquito ni «Fruta nueva» en ellos (tutorial &gt; Fruta nueva &gt; miquito, una tarjeta por nivel como mucho:
        /// D-023, punto 39; 02 §4.2; guion §14), tampoco en el 2, aunque 06 §2.2 la liste; el 8 es de Capi y no lleva instrucción de
        /// TiTino.
        /// </summary>
        [Test]
        public void TutorialLevels_AreBusy_AndKeepTheirOrder()
        {
            Assert.That(TutorialInstruction.Levels, Is.EqualTo(new[] { 1, 2, 3, 6 }));
            foreach (int level in TutorialInstruction.Levels)
            {
                Assert.That(SightingRule.IsBusy(level), Is.True, "el " + level + " lleva la instrucción del tutorial");
                Assert.That(TutorialInstruction.KeyFor(level), Is.Not.Null.And.StartsWith("tutorial_"), "nivel " + level);
            }

            Assert.That(TutorialInstruction.Has(8), Is.False, "el 8 es de Capi");
            Assert.That(TutorialInstruction.Has(12), Is.False, "el 12 es de TU1b");
            Assert.That(TutorialInstruction.KeyFor(4), Is.Null);
            Assert.That(TutorialInstruction.KeyFor(TutorialInstruction.UndoLevel), Is.EqualTo(TutorialInstruction.UndoKey), "el 3, Marea");
            Assert.That(TutorialInstruction.PointsAtUndo(3), Is.True);
            Assert.That(TutorialInstruction.PointsAtUndo(6), Is.False);
            foreach (int level in TutorialInstruction.Levels)
            {
                Assert.That(SightingRule.FruitNewAt(level, true), Is.EqualTo(-1),
                    "el " + level + ": sin «Fruta nueva», una tarjeta por nivel como mucho (D-023, punto 39; 02 §4.2)");
            }

            Assert.That(SightingRule.FruitNewAt(4, true), Is.GreaterThanOrEqualTo(0), "el 4 sí: no tiene instrucción");
        }

        /// <summary>
        /// T-F (prueba del 2026-10-09): Aguas turbias con la Luciérnaga regalada (100, 04 §2.5) y el Brote (163, D-007) también
        /// tienen su tarjeta de estreno, así que no comparten nivel con la del miquito; la lista de estrenos es una sola.
        /// </summary>
        [Test]
        public void Debuts100And163_AreBusy_AndEveryDebutIsAMilestone()
        {
            Assert.That(SightingRule.IsBusy(100), Is.True, "Aguas turbias y la Luciérnaga regalada (04 §2.5)");
            Assert.That(SightingRule.IsBusy(163), Is.True, "Brote (D-007)");
            Assert.That(SightingRule.FirstFreeLevel(100), Is.Not.EqualTo(100));
            Assert.That(SightingRule.FirstFreeLevel(100), Is.EqualTo(101));
            Assert.That(SightingRule.FirstFreeLevel(163), Is.EqualTo(164));
            Assert.That(SightingRule.MechanicDebutLevels, Is.EqualTo(new[] { 14, 27, 43, 63, 83, 100, 103, 123, 143, 163, 183 }), "D-007, en orden");
            foreach (int debut in SightingRule.MechanicDebutLevels)
            {
                Assert.That(SightingRule.IsBusy(debut), Is.True, "estreno del " + debut);
                Assert.That(SightingRule.MilestoneLevels, Does.Contain(debut), "estreno del " + debut + " entre los hitos");
            }

            int[] m = SightingRule.MilestoneLevels;
            for (int i = 1; i < m.Length; i++)
            {
                Assert.That(m[i], Is.GreaterThan(m[i - 1]), "hitos en orden y sin repetidos");
            }

            foreach (int milestone in new[] { 3, 7, 8, 10, 11, 12, 14, 20, 21, 27, 30, 31 })
            {
                Assert.That(SightingRule.IsBusy(milestone), Is.True, "hito del " + milestone + " (02 §13)");
            }

            Assert.That(SightingRule.IsBusy(101), Is.False);
            Assert.That(SightingRule.IsBusy(164), Is.False);
        }

        /// <summary>
        /// La lista de estrenos del cliente (VA1a y VA1b) coincide con la especificación de la campaña 201-700
        /// (<c>tools/level-generator/specs/campaign_v2.json</c>: <c>mechanics</c> con <c>variant_of</c> y su celda ★ del
        /// calendario; 02 §9.1, lista única y «Variantes»): 243 Liana de cuenta (<c>liana_n</c>), 283 Hoja atada
        /// (<c>hoja_atada</c>), 343 Brote de dos hojas (<c>brote_n2</c>) y 403 Flor (<c>flor</c>), todas en
        /// <see cref="SightingRule.VariantDebutLevels"/>, sin pendientes. Cada estreno de variante es un hito: no lleva la tarjeta
        /// del miquito, y tiene su texto de tutorial.
        /// </summary>
        [Test]
        public void VariantDebuts_MatchCampaignV2()
        {
            JsonValue spec = JsonParser.Parse(File.ReadAllText(CampaignV2Path));
            var variants = new SortedDictionary<int, string>();
            var codes = new Dictionary<int, string>();
            foreach (JsonValue m in spec.Get("mechanics")!.Items)
            {
                if (m.Has("variant_of"))
                {
                    int debut = m.Get("debut")!.AsInt();
                    variants.Add(debut, m.Get("id")!.AsString());
                    codes.Add(debut, m.Get("code")!.AsString());
                }
            }

            var client = new SortedSet<int>(SightingRule.VariantDebutLevels);
            Assert.That(client.Count, Is.EqualTo(SightingRule.VariantDebutLevels.Length), "sin repetidos");
            Assert.That(SightingRule.VariantDebutLevels, Is.Ordered, "en orden");
            Assert.That(client, Is.EqualTo(variants.Keys), "las variantes de campaign_v2 y las del cliente, sin pendientes (VA1b)");
            Assert.That(SightingRule.VariantDebutLevels, Is.EqualTo(new[] { 243, 283, 343, 403 }));
            Assert.That(variants[243], Is.EqualTo(LevelCardInfo.LianaCountIntro), "243: Liana de cuenta");
            Assert.That(variants[283], Is.EqualTo(LevelCardInfo.HojaAtadaIntro), "283: Hoja atada");
            Assert.That(variants[343], Is.EqualTo(LevelCardInfo.BroteTwoLeavesIntro), "343: Brote de dos hojas");
            Assert.That(variants[403], Is.EqualTo(LevelCardInfo.FlorIntro), "403: Flor de vitória-régia");
            foreach (int debut in SightingRule.VariantDebutLevels)
            {
                Assert.That(AF.UI.UiText.Tutorial(variants[debut]), Is.Not.Null.And.Not.Empty, "el estreno del " + debut + " tiene su texto");
            }

            JsonValue calendar = spec.Get("calendar")!;
            foreach (int debut in SightingRule.VariantDebutLevels)
            {
                string cell = calendar.Get(debut.ToString(CultureInfo.InvariantCulture))!.AsString();
                Assert.That(cell, Does.StartWith("★" + codes[debut]), "celda del calendario del " + debut + " (★ = estreno)");
                Assert.That(SightingRule.IsDebut(debut), Is.True, "estreno del " + debut);
                Assert.That(SightingRule.MilestoneLevels, Does.Contain(debut), "estreno del " + debut + " entre los hitos");
                Assert.That(SightingRule.IsBusy(debut), Is.True, "el " + debut + " no lleva la tarjeta del miquito");
                Assert.That(SightingRule.FirstFreeLevel(debut), Is.EqualTo(debut + 1), "la tarjeta del miquito pasa al " + (debut + 1));
                Assert.That(SightingRule.MechanicDebutLevels, Has.No.Member(debut), "las variantes van en su lista");
            }

            Assert.That(SightingRule.IsDebut(244), Is.False);
            Assert.That(SightingRule.IsBusy(284), Is.False);
            Assert.That(SightingRule.IsDebut(344) || SightingRule.IsDebut(404), Is.False);
        }

        /// <summary>
        /// Los estrenos de mecánicas y capacidades del cliente (<see cref="SightingRule.MechanicDebutLevels"/>) son los de
        /// <c>campaign_v2.json</c> (<c>mechanics</c> sin <c>variant_of</c> y el primero de cada capacidad, <c>chica</c> y
        /// <c>alta</c>), salvo la Tinaja (323), que entra con TJ4 y C1b.
        /// </summary>
        [Test]
        public void MechanicDebuts_MatchCampaignV2_WithTheTinajaPending()
        {
            JsonValue spec = JsonParser.Parse(File.ReadAllText(CampaignV2Path));
            var expected = new SortedSet<int>();
            foreach (JsonValue m in spec.Get("mechanics")!.Items)
            {
                if (!m.Has("variant_of"))
                {
                    expected.Add(m.Get("debut")!.AsInt());
                }
            }

            var firstOfCapacity = new Dictionary<string, int>();
            foreach (JsonValue c in spec.Get("capacities")!.Items)
            {
                string id = c.Get("id")!.AsString();
                int debut = c.Get("debut")!.AsInt();
                firstOfCapacity[id] = firstOfCapacity.TryGetValue(id, out int seen) ? System.Math.Min(seen, debut) : debut;
            }

            foreach (int debut in firstOfCapacity.Values)
            {
                expected.Add(debut);
            }

            var client = new SortedSet<int>(SightingRule.MechanicDebutLevels) { PendingTinajaDebut };
            Assert.That(client, Is.EqualTo(expected), "estrenos de campaign_v2 (más la Tinaja, pendiente)");
        }

        /// <summary>
        /// En el mundo 6 (201-300, <c>mundo_06_v1.json</c>, <c>pack_schema</c> 3) los niveles con <c>intro_mechanic</c> son
        /// exactamente los estrenos de variantes de ese tramo, con su id, tienen tarjeta de estreno (02 §10) y no se saltan
        /// (<c>skippable</c> = false: 04 §2.6, los estrenos no admiten el Salto del Boto).
        /// </summary>
        [Test]
        public void World6Pack_DebutsAreTheVariantList_AndCannotBeSkipped()
        {
            LevelPack pack = LevelPackReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Levels/mundo_06_v1.json")));
            var debuts = new SortedDictionary<int, string>();
            foreach (LevelDefinition level in pack.Levels)
            {
                if (level.IntroMechanic != null)
                {
                    debuts.Add(level.Level, level.IntroMechanic);
                    Assert.That(level.Skippable, Is.False, "el estreno del " + level.Level + " no se salta");
                    Assert.That(LevelCardInfo.Shows(level), Is.True, "el " + level.Level + " tiene tarjeta de estreno");
                }
            }

            var expected = new SortedDictionary<int, string>
            {
                { 243, LevelCardInfo.LianaCountIntro },
                { 283, LevelCardInfo.HojaAtadaIntro },
            };
            Assert.That(debuts, Is.EqualTo(expected), "estrenos del mundo 6");
            foreach (int level in debuts.Keys)
            {
                Assert.That(SightingRule.VariantDebutLevels, Does.Contain(level));
            }
        }

        [Test]
        public void EveryCardLevelOfTheCatalog_IsAFreeLevel()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            foreach (FaunaSpecies sp in catalog.Species)
            {
                Assert.That(SightingRule.FirstFreeLevel(sp.CardLevel), Is.EqualTo(sp.CardLevel), sp.Id);
            }
        }

        [Test]
        public void ShowsCard_OnlyOnTheFirstFreeLevel_FirstAttempt()
        {
            Assert.That(SightingRule.ShowsCard(5, 5, true), Is.True);
            Assert.That(SightingRule.ShowsCard(5, 5, false), Is.False, "los reintentos ya la vieron");
            Assert.That(SightingRule.ShowsCard(4, 5, true), Is.False);
            Assert.That(SightingRule.ShowsCard(6, 5, true), Is.False, "una sola vez");
            // Con el tutorial de Marea pendiente en el 5, la tarjeta pasa al 9: el 6 lleva la instrucción de TiTino (TU1a), el 7
            // Fruta nueva y el Reto del río y el 8 la Cuia extra de Capi.
            Assert.That(SightingRule.FirstFreeLevel(5, l => l == 5), Is.EqualTo(9), "con el tutorial de Marea pendiente en el 5");
            Assert.That(SightingRule.ShowsCard(9, 5, true, l => l == 5), Is.True);
            Assert.That(SightingRule.ShowsCard(6, 5, true, l => l == 5), Is.False, "el 6 es del tutorial con TiTino");
            Assert.That(SightingRule.FirstFreeLevel(11), Is.EqualTo(13), "11 banner y 12 Morpho");
        }
    }
}
