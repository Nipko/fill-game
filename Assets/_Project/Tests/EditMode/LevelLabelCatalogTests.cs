#nullable enable
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// <c>lvl_label_override</c> en el catálogo (tarea B11, parche B11-labels; 08 §9; 02 §10; 03 §7: la etiqueta se muestra y
    /// paga): <see cref="LevelCatalog.Create(System.Collections.Generic.IReadOnlyList{LevelPack}, int, LevelOverrides, MechanicSwitches, System.Action{string}, LevelLabelOverrides)"/>
    /// sirve el nivel con la etiqueta de la tabla, la tarjeta previa y la Cosecha la leen, y el arranque toma la tabla de
    /// <see cref="LevelLabelSource"/> (vacía en release).
    /// </summary>
    public sealed class LevelLabelCatalogTests
    {
        [TearDown]
        public void TearDown()
        {
            LevelLabelSource.JsonOverride = null;
        }

        [Test]
        public void Catalog_ServesTheOverriddenLabel_AndTheCardPaysIt()
        {
            LevelLabelOverrides labels = LevelLabelOverrides.Create("{\"42\": \"hard\", \"40\": \"normal\"}");
            LevelCatalog plain = LevelCatalog.Create(TestPacks.Campaign);
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign, 0, null, MechanicSwitches.AllOn, null, labels);
            Assert.That(plain.Find(42)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelNormal), "condición del test");
            Assert.That(plain.Find(40)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard), "condición del test: el primer Difícil");
            Assert.That(catalog.Find(42)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard));
            Assert.That(catalog.Find(40)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelNormal), "también quita una etiqueta");
            Assert.That(catalog.Find(43), Is.SameAs(plain.Find(43)), "los demás, el mismo nivel del paquete");

            // La etiqueta se muestra y paga (03 §7): la tarjeta previa del 42 es la del Difícil, con su premio y su cinta.
            EconomyConfig economy = EconomyConfig.Defaults;
            LevelCardInfo card = LevelCardInfo.For(catalog.Find(42)!, 42, economy, false)!;
            Assert.That(card.Kind, Is.EqualTo(LevelCardKind.Difficulty));
            Assert.That(card.RewardSeeds, Is.EqualTo(economy.EcoRewardHard), "eco_reward_hard (05)");
            Assert.That(LevelCardInfo.For(catalog.Find(40)!, 40, economy, false), Is.Null, "el 40 sin etiqueta: sin tarjeta");
        }

        [Test]
        public void Relabeled_IsNotAnOverride_AndAnOverrideTakesItsLabel()
        {
            LevelCatalog plain = LevelCatalog.Create(TestPacks.Campaign);
            LevelCatalog relabeled = LevelCatalog.Create(TestPacks.Campaign, 0, null, MechanicSwitches.AllOn, null,
                LevelLabelOverrides.Create("{\"42\": \"hard\"}"));
            Assert.That(relabeled.IsOverridden(42), Is.False, "solo reetiquetado: no es un override de level_overrides (KS1)");
            Assert.That(relabeled.OverriddenLevels, Is.Empty);
            Assert.That(relabeled.IsRelabeled(42), Is.True, "af_label_shown: source = override (08 §8.2)");
            Assert.That(relabeled.IsRelabeled(41), Is.False, "source = pack");
            Assert.That(relabeled.RelabeledLevels, Is.EqualTo(new[] { 42 }));
            Assert.That(plain.RelabeledLevels, Is.Empty);

            // Con la Hoja apagada, el 43 (su estreno) se sirve con su override (el tablero del 42) y con la etiqueta de la tabla.
            LevelDefinition replacement = plain.Find(42)!;
            Assert.That(plain.Find(43)!.HasMechanic(AF.Core.Mechanics.MechanicType.Hoja), Is.True, "condición del test");
            Assert.That(replacement.HasMechanic(AF.Core.Mechanics.MechanicType.Hoja), Is.False, "condición del test");
            LevelOverrides overrides = LevelOverrides.Create("{\"43\": \"" + replacement.Id + "\"}", TestPacks.Campaign);
            MechanicSwitches switches = MechanicSwitches.AllOn.With(MechanicSwitch.Hoja, false);
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign, 0, overrides, switches, null,
                LevelLabelOverrides.Create("{\"43\": \"hard\"}"));
            Assert.That(catalog.IsOverridden(43), Is.True);
            Assert.That(catalog.IsRelabeled(43), Is.True);
            Assert.That(catalog.Find(43)!.Id, Is.EqualTo(replacement.Id));
            Assert.That(catalog.Find(43)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard), "la etiqueta, sobre el nivel servido");
        }

        [Test]
        public void Catalog_WithoutTheTable_IsThePack()
        {
            LevelCatalog plain = LevelCatalog.Create(TestPacks.Campaign);
            LevelCatalog empty = LevelCatalog.Create(TestPacks.Campaign, 0, null, MechanicSwitches.AllOn, null, LevelLabelOverrides.Empty);
            for (int level = 1; level <= 700; level++)
            {
                Assert.That(empty.Find(level), Is.SameAs(plain.Find(level)), "nivel " + level);
            }
        }

        [Test]
        public void Source_IsEmptyInRelease_AndTheTestHookOtherwise()
        {
            LevelLabelSource.JsonOverride = null;
            Assert.That(LevelLabelSource.Create(null, null).Count, Is.EqualTo(0), "08 §9: default {}");
            LevelLabelSource.JsonOverride = "{\"41\": \"hard\", \"10\": \"hard\"}";
            try
            {
                string? warned = null;
                LevelLabelOverrides table = LevelLabelSource.Create(null, w => warned = w);
                Assert.That(table.Count, Is.EqualTo(1));
                Assert.That(warned, Does.Contain("10").And.Contains("lvl_labels_from_level"), "la entrada descartada se avisa");
            }
            finally
            {
                LevelLabelSource.JsonOverride = null;
            }
        }

        [Test]
        public void Source_WithRemoteConfig_IsItsTable_AndTheTestHookWins()
        {
            // Cierre de B11: el arranque lee lvl_label_override de Remote Config (RcGameplayBinder) y el catálogo usa esa tabla.
            var service = new AF.RemoteConfig.RemoteConfigService();
            service.Activate(new System.Collections.Generic.Dictionary<string, string> { { "lvl_label_override", "{\"57\":\"very_hard\"}" } });
            AF.RemoteConfig.Binding.RcConfigs rc = AF.RemoteConfig.Binding.RcConfigBinder.Bind(service.Freeze(), "1.0");
            LevelLabelSource.JsonOverride = null;
            string? warned = null;
            LevelLabelOverrides table = LevelLabelSource.Create(rc, w => warned = w);
            Assert.That(table, Is.SameAs(rc.LevelLabels));
            Assert.That(table.TryGet(57, out string label), Is.True);
            Assert.That(label, Is.EqualTo(LevelDefinition.LabelVeryHard));
            Assert.That(warned, Is.Null, "los avisos de la tabla de Remote Config ya salieron con los de Remote Config");

            LevelLabelSource.JsonOverride = "{}";
            try
            {
                Assert.That(LevelLabelSource.Create(rc, null).Count, Is.EqualTo(0), "el gancho del test gana");
            }
            finally
            {
                LevelLabelSource.JsonOverride = null;
            }
        }
    }
}
