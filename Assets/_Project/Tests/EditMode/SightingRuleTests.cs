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
            Assert.That(SightingRule.IsBusy(1), Is.True, "Fruta nueva y tutorial (sin banner: decisión del dueño)");
            Assert.That(SightingRule.IsBusy(11), Is.True, "banner del Tapajós");
            Assert.That(SightingRule.IsBusy(12), Is.True, "Morpho");
            Assert.That(SightingRule.IsBusy(41), Is.True, "banner de Manaos y urucum");
            Assert.That(SightingRule.IsBusy(43), Is.True, "Hoja");
            Assert.That(SightingRule.IsBusy(5), Is.False);
            Assert.That(SightingRule.IsBusy(13), Is.False);
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
