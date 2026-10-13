#nullable enable
using System.IO;
using AF.GameFlow;
using AF.Levels;
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
        [Test]
        public void BusyLevels_AreTheCanonicalBannersDebutsAndMilestones()
        {
            Assert.That(SightingRule.IsBusy(1), Is.True, "banner del Estuario y Fruta nueva");
            Assert.That(SightingRule.IsBusy(11), Is.True, "banner del Tapajós");
            Assert.That(SightingRule.IsBusy(12), Is.True, "Morpho");
            Assert.That(SightingRule.IsBusy(41), Is.True, "banner de Manaos y urucum");
            Assert.That(SightingRule.IsBusy(43), Is.True, "Hoja");
            Assert.That(SightingRule.IsBusy(5), Is.False);
            Assert.That(SightingRule.IsBusy(13), Is.False);
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
            Assert.That(SightingRule.FirstFreeLevel(5, l => l == 5), Is.EqualTo(6), "con el tutorial de Marea pendiente en el 5");
            Assert.That(SightingRule.ShowsCard(6, 5, true, l => l == 5), Is.True);
            Assert.That(SightingRule.FirstFreeLevel(11), Is.EqualTo(13), "11 banner y 12 Morpho");
        }
    }
}
