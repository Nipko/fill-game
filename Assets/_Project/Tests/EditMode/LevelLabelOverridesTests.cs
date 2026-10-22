#nullable enable
using System.Collections.Generic;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// <c>lvl_label_override</c> (08 §9: json <c>{nivel: label}</c>, default <c>{}</c>; 02 §10, «Honestidad»: «etiqueta recalibrada
    /// en vivo con intentos medios por nivel»; 03 §7: la etiqueta se muestra y paga; tarea B11): <see cref="LevelLabelOverrides"/>.
    /// </summary>
    public sealed class LevelLabelOverridesTests
    {
        [Test]
        public void Default_IsEmpty_08_9()
        {
            Assert.That(LevelLabelOverrides.LocalSource, Is.EqualTo("{}"), "08 §9: default {}");
            LevelLabelOverrides table = LevelLabelOverrides.Create(LevelLabelOverrides.LocalSource);
            Assert.That(table.Count, Is.EqualTo(0));
            Assert.That(table.Problems, Is.Empty);
            Assert.That(LevelLabelOverrides.Create(null).Count, Is.EqualTo(0));
            Assert.That(LevelLabelOverrides.Create("  ").Count, Is.EqualTo(0));
            Assert.That(LevelLabelOverrides.Key, Is.EqualTo("lvl_label_override"));
        }

        [Test]
        public void Floors_AreTheDefaultsOf08_9()
        {
            // 08 §9: lvl_labels_from_level / lvl_very_hard_from_level = 40 / 60, «primer nivel que puede mostrar Difícil / Muy
            // difícil».
            Assert.That(LevelLabelOverrides.LabelsFromLevelDefault, Is.EqualTo(40));
            Assert.That(LevelLabelOverrides.VeryHardFromLevelDefault, Is.EqualTo(60));
        }

        [Test]
        public void Reads_TheThreeLabels_SortedByLevel()
        {
            LevelLabelOverrides table = LevelLabelOverrides.Create("{\"75\": \"very_hard\", \"41\": \"hard\", \"46\": \"normal\"}");
            Assert.That(table.Problems, Is.Empty);
            Assert.That(table.Levels, Is.EqualTo(new[] { 41, 46, 75 }));
            Assert.That(table.TryGet(41, out string a) && a == LevelDefinition.LabelHard, Is.True);
            Assert.That(table.TryGet(46, out string b) && b == LevelDefinition.LabelNormal, Is.True);
            Assert.That(table.TryGet(75, out string c) && c == LevelDefinition.LabelVeryHard, Is.True);
            Assert.That(table.TryGet(42, out _), Is.False);
        }

        [TestCase("{\"39\": \"hard\"}", Description = "Difícil antes del 40 (lvl_labels_from_level)")]
        [TestCase("{\"59\": \"very_hard\"}", Description = "Muy difícil antes del 60 (lvl_very_hard_from_level)")]
        [TestCase("{\"50\": \"facil\"}", Description = "no es una de las tres etiquetas de difficulty.label")]
        [TestCase("{\"50\": 3}", Description = "no es una cadena")]
        [TestCase("{\"0\": \"hard\"}", Description = "nivel no positivo")]
        [TestCase("{\"x\": \"hard\"}", Description = "nivel que no es un número")]
        [TestCase("[]", Description = "no es un objeto")]
        [TestCase("{nope", Description = "JSON roto")]
        public void Invalid_IsDiscarded_WithAProblem(string json)
        {
            LevelLabelOverrides table = LevelLabelOverrides.Create(json);
            Assert.That(table.Count, Is.EqualTo(0));
            Assert.That(table.Problems.Count, Is.EqualTo(1));
            Assert.That(table.Problems[0], Does.StartWith(LevelLabelOverrides.Key));
        }

        [Test]
        public void Floors_ComeFromTheCaller()
        {
            Assert.That(LevelLabelOverrides.Create("{\"39\": \"hard\"}", 30, 60).Count, Is.EqualTo(1), "con lvl_labels_from_level = 30");
            Assert.That(LevelLabelOverrides.Create("{\"10\": \"normal\"}").Count, Is.EqualTo(1), "normal vale en cualquier nivel");
        }

        [Test]
        public void Repeated_Level_KeepsTheFirst()
        {
            // El lector rechaza las claves duplicadas literales; «041» es otra clave para el mismo nivel.
            LevelLabelOverrides table = LevelLabelOverrides.Create("{\"41\": \"hard\", \"041\": \"normal\"}");
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.TryGet(41, out string label) && label == LevelDefinition.LabelHard, Is.True);
            Assert.That(table.Problems.Count, Is.EqualTo(1));
        }

        [Test]
        public void WithLabel_ChangesOnlyTheLabel()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            LevelDefinition source = catalog.Find(118)!; // reto Difícil con Remolino y Hoja y su alivio (03 §13)
            Assert.That(source.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard));
            LevelDefinition copy = source.WithLabel(LevelDefinition.LabelNormal);
            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(source.WithLabel(LevelDefinition.LabelHard), Is.SameAs(source), "la misma etiqueta: el mismo nivel");
            Assert.Throws<System.ArgumentException>(() => source.WithLabel("facil"));
            Assert.That(copy.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelNormal));
            Assert.That(copy.Id, Is.EqualTo(source.Id));
            Assert.That(copy.Level, Is.EqualTo(source.Level));
            Assert.That(copy.World, Is.EqualTo(source.World));
            Assert.That(copy.WaterBase, Is.EqualTo(source.WaterBase));
            Assert.That(copy.Par, Is.EqualTo(source.Par));
            Assert.That(copy.Band, Is.EqualTo(source.Band));
            Assert.That(copy.IntroMechanic, Is.EqualTo(source.IntroMechanic));
            Assert.That(copy.Skippable, Is.EqualTo(source.Skippable));
            Assert.That(copy.HintSupported, Is.EqualTo(source.HintSupported));
            Assert.That(copy.IsMurky, Is.EqualTo(source.IsMurky));
            Assert.That(copy.Relief, Is.EqualTo(source.Relief), "las mismas variantes de alivio");
            Assert.That(copy.Palette, Is.EqualTo(source.Palette));
            Assert.That(copy.CreateBoard().Count, Is.EqualTo(source.CreateBoard().Count));
            for (int i = 0; i < source.CuiaCount; i++)
            {
                Assert.That(copy.CapacityOf(i), Is.EqualTo(source.CapacityOf(i)));
                Assert.That(copy.CopyDoses(i), Is.EqualTo(source.CopyDoses(i)));
                Assert.That(copy.Attrs[i], Is.EqualTo(source.Attrs[i]));
            }
        }

        [Test]
        public void WithLabel_KeepsTheHiddenDosesOfMurkyLevels()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            LevelDefinition murky = catalog.Find(100)!; // estreno de Aguas turbias (02 §11)
            Assert.That(murky.IsMurky, Is.True);
            LevelDefinition copy = murky.WithLabel(murky.DifficultyLabel == LevelDefinition.LabelHard ? LevelDefinition.LabelVeryHard : LevelDefinition.LabelHard);
            Assert.That(copy.IsMurky, Is.True);
            Assert.That(copy.HiddenCount, Is.EqualTo(murky.HiddenCount));
            for (int i = 0; i < murky.HiddenCount; i++)
            {
                Assert.That(copy.Hidden[i], Is.EqualTo(murky.Hidden[i]));
            }
        }

        [Test]
        public void Apply_RelabelsOnlyTheListedLevels_AndKeepsTheArrayWhenNothingChanges()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            var served = new List<LevelDefinition>();
            var numbers = new List<int>();
            for (int level = 40; level <= 45; level++)
            {
                served.Add(catalog.Find(level)!);
                numbers.Add(level);
            }

            LevelDefinition[] array = served.ToArray();
            Assert.That(LevelLabelOverrides.Empty.Apply(array, numbers.ToArray()), Is.SameAs(array), "sin entradas, el mismo array");
            string same = array[0].DifficultyLabel;
            Assert.That(LevelLabelOverrides.Create("{\"40\": \"" + same + "\"}").Apply(array, numbers.ToArray()), Is.SameAs(array),
                "la misma etiqueta que el paquete no cambia nada");

            LevelLabelOverrides table = LevelLabelOverrides.Create("{\"42\": \"hard\", \"999\": \"normal\"}");
            LevelDefinition[] result = table.Apply(array, numbers.ToArray());
            Assert.That(result, Is.Not.SameAs(array));
            for (int i = 0; i < result.Length; i++)
            {
                if (numbers[i] == 42)
                {
                    Assert.That(result[i].DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard));
                }
                else
                {
                    Assert.That(result[i], Is.SameAs(array[i]), "los demás, sin tocar");
                }
            }
        }
    }
}
