#nullable enable
using System;
using AF.Core.Board;
using AF.Levels;
using AF.Levels.Json;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>Lector JSON mínimo y lector de paquetes <c>af-level/1</c> (08 §4.1; docs/dev/prototipo-v0.md b y c).</summary>
    public sealed class LevelPackReaderTests
    {
        [Test]
        public void Json_ParsesObjectsArraysNumbersAndEscapes()
        {
            JsonValue v = JsonParser.Parse("{\"a\": [1, -2, 3.5e2, true, false, null], \"s\": \"\\u00e7a\\\"\\n\"}");
            Assert.That(v.Kind, Is.EqualTo(JsonKind.Object));
            JsonValue a = v.Get("a")!;
            Assert.That(a.Items.Count, Is.EqualTo(6));
            Assert.That(a.Items[0].AsInt(), Is.EqualTo(1));
            Assert.That(a.Items[1].AsInt(), Is.EqualTo(-2));
            Assert.That(a.Items[2].AsDouble(), Is.EqualTo(350.0));
            Assert.That(a.Items[3].AsBool(), Is.True);
            Assert.That(a.Items[5].IsNull, Is.True);
            Assert.That(v.Get("s")!.AsString(), Is.EqualTo("ça\"\n"));
        }

        [TestCase("")]
        [TestCase("{")]
        [TestCase("[1, 2")]
        [TestCase("{\"a\": 1,}")]
        [TestCase("{\"a\": 01}")]
        [TestCase("{\"a\": 1, \"a\": 2}")]
        [TestCase("[1] x")]
        public void Json_RejectsInvalidText(string text)
        {
            Assert.That(() => JsonParser.Parse(text), Throws.TypeOf<FormatException>());
        }

        [Test]
        public void Json_DecimalNumberIsNotAnInt()
        {
            JsonValue v = JsonParser.Parse("[1.5]");
            Assert.That(() => v.Items[0].AsInt(), Throws.TypeOf<FormatException>());
        }

        [Test]
        public void Tutorial_HasLevels1To10InOrder()
        {
            LevelPack pack = TestPacks.Tutorial;
            Assert.That(pack.PackId, Is.EqualTo("tutorial_v0"));
            Assert.That(pack.WaterBase, Is.EqualTo("white"));
            Assert.That(pack.Count, Is.EqualTo(10));
            for (int i = 0; i < pack.Count; i++)
            {
                Assert.That(pack.Levels[i].Level, Is.EqualTo(i + 1));
            }
        }

        [Test]
        public void Tutorial_Level1_IsTheLiteralStateOf02_13()
        {
            // 02 §13, nivel 1: [açaí, açaí, açaí, aguaje], [aguaje, aguaje, aguaje, açaí], vacía.
            BoardState board = TestPacks.Tutorial.Find(1)!.CreateBoard();
            Assert.That(board.Count, Is.EqualTo(3));
            Assert.That(board[0], Is.EqualTo(Cuia.Create(4, new[] { 8, 8, 8, 4 })));
            Assert.That(board[1], Is.EqualTo(Cuia.Create(4, new[] { 4, 4, 4, 8 })));
            Assert.That(board[2], Is.EqualTo(Cuia.Empty(4)));
            Assert.That(TestPacks.Tutorial.Find(1)!.Par, Is.EqualTo(3));
        }

        [Test]
        public void NextLevelAfter_WrapsFromLastToFirst()
        {
            LevelPack pack = TestPacks.Tutorial;
            Assert.That(pack.NextLevelAfter(3), Is.EqualTo(4));
            Assert.That(pack.NextLevelAfter(10), Is.EqualTo(1));
            Assert.That(pack.Find(11), Is.Null);
        }

        [Test]
        public void Reader_AcceptsSubsetOfAndPackSchema1()
        {
            Assert.That(LevelPackReader.Read(TestPacks.MinimalPack()).Count, Is.EqualTo(1));
            Assert.That(LevelPackReader.Read(TestPacks.MinimalPack(null, "\"pack_schema\": 1")).Count, Is.EqualTo(1));
        }

        [TestCase("\"pack_schema\": 2")]
        [TestCase("\"subset_of\": \"af-level/2\"")]
        [TestCase("\"note\": \"sin esquema\"")]
        public void Reader_RejectsUnknownSchema(string header)
        {
            Assert.That(() => LevelPackReader.Read(TestPacks.MinimalPack(null, header)), Throws.TypeOf<LevelFormatException>());
        }

        [Test]
        public void Reader_RejectsMechanicsMurkyAndHidden()
        {
            Assert.That(
                () => LevelPackReader.Read(TestPacks.MinimalPack(TestPacks.MinimalLevel(attrs: "[{\"type\": \"hoja\"}, null, null]"))),
                Throws.TypeOf<LevelFormatException>());
            Assert.That(
                () => LevelPackReader.Read(TestPacks.MinimalPack(TestPacks.MinimalLevel(murky: "true"))),
                Throws.TypeOf<LevelFormatException>());
            Assert.That(
                () => LevelPackReader.Read(TestPacks.MinimalPack(TestPacks.MinimalLevel(hidden: "[[0, 1]]"))),
                Throws.TypeOf<LevelFormatException>());
        }

        [Test]
        public void Reader_RejectsInconsistentBoards()
        {
            // longitudes distintas, más dosis que capacidad, Barro (15), tinte fuera de rango y U_c distinto (02 R3)
            string[] cuias =
            {
                "[[8, 8, 8, 4], [4, 4, 4, 8]]",
                "[[8, 8, 8, 4, 4], [4, 4, 8], []]",
                "[[8, 8, 8, 15], [4, 4, 4, 8], []]",
                "[[8, 8, 8, 0], [4, 4, 4, 8], []]",
                "[[8, 8, 8, 4], [4, 4, 4, 8], [8]]",
            };
            for (int i = 0; i < cuias.Length; i++)
            {
                string json = TestPacks.MinimalPack(TestPacks.MinimalLevel(cuias: cuias[i]));
                Assert.That(() => LevelPackReader.Read(json), Throws.TypeOf<LevelFormatException>(), cuias[i]);
            }
        }

        [Test]
        public void Reader_IsCultureInvariant()
        {
            System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("es-CO");
                Assert.That(LevelPackReader.Read(TestPacks.MinimalPack()).First.Par, Is.EqualTo(3));
                Assert.That(JsonParser.Parse("[2.5]").Items[0].AsDouble(), Is.EqualTo(2.5));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
