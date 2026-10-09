#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
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
        public void Reader_AcceptsSubsetOfAndPackSchema1And2()
        {
            Assert.That(LevelPackReader.Read(TestPacks.MinimalPack()).Count, Is.EqualTo(1));
            Assert.That(LevelPackReader.Read(TestPacks.MinimalPack(null, "\"pack_schema\": 1")).Count, Is.EqualTo(1));
            Assert.That(LevelPackReader.Read(TestPacks.MinimalPack(null, "\"pack_schema\": 2")).Count, Is.EqualTo(1));
        }

        [TestCase("\"pack_schema\": 0", "t: pack_schema 0 no admitido")]
        [TestCase("\"pack_schema\": 3", "t: pack_schema 3 no admitido")]
        [TestCase("\"subset_of\": \"af-level/2\"", "t: el paquete no declara pack_schema")]
        [TestCase("\"note\": \"sin esquema\"", "t: el paquete no declara pack_schema")]
        public void Reader_RejectsUnknownSchema(string header, string expected)
        {
            // 08 §4.4: el cliente rechaza un paquete con un pack_schema que no conoce (lee el 1 y el 2, contrato 21). El mensaje
            // empieza por el id del paquete ("t") y dice por qué: la versión que no admite o que no declara ninguna.
            Assert.That(() => LevelPackReader.Read(TestPacks.MinimalPack(null, header)),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("pack_schema").And.Message.Contains(expected));
        }

        [Test]
        public void Reader_Schema2_AcceptsMechanicsMurkyHiddenAndUnitsObject()
        {
            // Hoja (02 §9.1 M1): vacía, con su llave en el tablero; se construye cerrada.
            LevelDefinition hoja = LevelPackReader.Read(TestPacks.Schema2Pack("[null, null, {\"type\": \"hoja\", \"key\": 4}, null]")).First;
            Assert.That(hoja.Attrs.Count, Is.EqualTo(4));
            Assert.That(hoja.Attrs[2], Is.EqualTo(new MechanicSpec(MechanicType.Hoja, key: 4)));
            Assert.That(hoja.Attrs[0].IsNone, Is.True);
            Assert.That(hoja.HasMechanic(MechanicType.Hoja), Is.True);
            Assert.That(hoja.HasMechanic(MechanicType.Liana), Is.False);
            BoardState hojaBoard = hoja.CreateBoard();
            Assert.That(hojaBoard[2].Mechanic, Is.EqualTo(MechanicType.Hoja));
            Assert.That(hojaBoard[2].MechanicKey, Is.EqualTo(4));
            Assert.That(hojaBoard[2].MechanicFlags & Cuia.FlagClosed, Is.EqualTo(Cuia.FlagClosed));
            Assert.That(hoja.CreateBoard(), Is.EqualTo(hojaBoard).And.Not.SameAs(hojaBoard), "un estado nuevo en cada llamada");

            // Remolino (M4): un par simétrico con el mismo id de pareja; Matapi (D-007) sin campos.
            LevelDefinition remolino = LevelPackReader.Read(TestPacks.Schema2Pack("[null, null, {\"type\": \"remolino\", \"partner\": 3}, {\"type\": \"remolino\", \"partner\": 2}]")).First;
            BoardState pair = remolino.CreateBoard();
            Assert.That(pair[2].Mechanic, Is.EqualTo(MechanicType.Remolino));
            Assert.That(pair[3].MechanicPairId, Is.EqualTo(pair[2].MechanicPairId));
            LevelDefinition matapi = LevelPackReader.Read(TestPacks.Schema2Pack("[null, null, null, {\"type\": \"matapi\"}]")).First;
            Assert.That(matapi.CreateBoard()[3].Mechanic, Is.EqualTo(MechanicType.Matapi));

            // Aguas turbias (02 §11): murky = true con la dosis [0, 0] oculta (T3: 3 dosis, cima visible; T4: distinta de la de encima).
            LevelDefinition murky = LevelPackReader.Read(TestPacks.Schema2Pack(
                cuias: "[[4, 8, 4], [8, 8, 4, 4], [8], []]", murky: "true", hidden: "[[0, 0]]")).First;
            Assert.That(murky.IsMurky, Is.True);
            Assert.That(murky.HiddenCount, Is.EqualTo(1));
            Assert.That(murky.Hidden[0], Is.EqualTo(new[] { 0, 0 }));
            Assert.That(murky.CreateBoard()[0].IsHiddenAt(0), Is.True);
            Assert.That(murky.CreateBoard().HiddenCount, Is.EqualTo(1));

            // units_per_color objeto (02 §8): la fruta abundante de la Cuia alta de 5; los demás colores, 4.
            LevelDefinition tall = LevelPackReader.Read(TestPacks.Schema2Pack(
                "[null, null, null]", "[[8, 8, 8, 8, 4], [4, 4, 4, 8], []]", "[5, 4, 4]", units: "{\"8\": 5}")).First;
            Assert.That(tall.Units.Of(8), Is.EqualTo(5));
            Assert.That(tall.Units.Of(4), Is.EqualTo(4));
            Assert.That(tall.HasTallCuia, Is.True);
            Assert.That(tall.HasSmallCuia, Is.False);
            Assert.That(tall.CreateBoard().UnitsOf(8), Is.EqualTo(5));

            // Campos opcionales: intro_mechanic, skippable, hint_supported, difficulty.band y difficulty.par_approx [P].
            string optional = TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"chica\", \"skippable\": false, \"hint_supported\": false")
                .Replace("\"difficulty\": { \"par\": 3 }", "\"difficulty\": { \"label\": \"hard\", \"par\": 3, \"band\": \"milestone\", \"par_approx\": true }");
            LevelDefinition withOptional = LevelPackReader.Read(optional).First;
            Assert.That(withOptional.IntroMechanic, Is.EqualTo("chica"));
            Assert.That(withOptional.Skippable, Is.False);
            Assert.That(withOptional.HintSupported, Is.False);
            Assert.That(withOptional.Band, Is.EqualTo(LevelDefinition.BandMilestone));
            Assert.That(withOptional.ParApprox, Is.True);
            Assert.That(withOptional.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard));

            // Sin ellos: sin mecánicas ni ocultos, units 4, null, true, true, sin banda y par exacto.
            LevelDefinition plain = LevelPackReader.Read(TestPacks.MinimalPack()).First;
            Assert.That(plain.IntroMechanic, Is.Null);
            Assert.That(plain.Skippable, Is.True);
            Assert.That(plain.HintSupported, Is.True);
            Assert.That(plain.Band, Is.Null);
            Assert.That(plain.ParApprox, Is.False);
            Assert.That(plain.IsMurky, Is.False);
            Assert.That(plain.HiddenCount, Is.EqualTo(0));
            Assert.That(plain.Units.Of(8), Is.EqualTo(4));
            Assert.That(plain.HasMud, Is.False);
        }

        // Paquetes pack_schema 2 de un nivel que el lector rechaza (contrato 21; 02 §9.1, §11 y R2-R7). Cada caso lleva el
        // fragmento del mensaje que prueba su motivo: cualquier error posterior a leer el id también diría el id, así que el id
        // solo no basta para saber que el caso falla por lo que dice su nombre. El nombre del test es el motivo, no el JSON.
        private static readonly TestCaseData[] InvalidSchema2Levels =
        {
            Invalid("tipo desconocido", TestPacks.Schema2Pack("[null, null, {\"type\": \"aguape\"}, null]"), "desconocido 'aguape'"),
            Invalid("barro no es un type", TestPacks.Schema2Pack("[null, null, {\"type\": \"barro\"}, null]"), "desconocido 'barro'"),
            Invalid("hoja sin key", TestPacks.Schema2Pack("[null, null, {\"type\": \"hoja\"}, null]"), "(hoja): lleva solo key"),
            Invalid("partner incoherente", TestPacks.Schema2Pack("[null, null, {\"type\": \"remolino\", \"partner\": 3}, null]"), "la pareja tiene que ser un Remolino"),
            Invalid("partner negativo", TestPacks.Schema2Pack("[null, null, {\"type\": \"remolino\", \"partner\": -2}, {\"type\": \"remolino\", \"partner\": 2}]"), "partner es un índice 0-based"),
            Invalid("attrs de otra longitud", TestPacks.Schema2Pack("[null, null, null]"), "'attrs' debe tener una entrada por cuia"),
            Invalid("oculta en la cima (R7)", TestPacks.Schema2Pack(cuias: "[[4, 8, 4], [8, 8, 4, 4], [8], []]", murky: "true", hidden: "[[0, 2]]"), "R7, la cima"),
            Invalid("oculta fuera del tablero", TestPacks.Schema2Pack(cuias: "[[4, 8, 4], [8, 8, 4, 4], [8], []]", murky: "true", hidden: "[[4, 0]]"), "[4, 0] está fuera del tablero"),
            Invalid("oculta repetida", TestPacks.Schema2Pack(cuias: "[[4, 8, 4], [8, 8, 4, 4], [8], []]", murky: "true", hidden: "[[0, 0], [0, 0]]"), "repite la dosis [0, 0]"),
            Invalid("murky sin ocultas", TestPacks.Schema2Pack(murky: "true"), "murky = true necesita al menos una dosis oculta"),
            Invalid("ocultas sin murky", TestPacks.Schema2Pack(cuias: "[[4, 8, 4], [8, 8, 4, 4], [8], []]", hidden: "[[0, 0]]"), "solo puede tener dosis ocultas con murky = true"),
            Invalid("units con clave 17", TestPacks.Schema2Pack(units: "{\"17\": 4}"), "'units_per_color' tiene la clave '17'"),
            Invalid("units con valor 7", TestPacks.Schema2Pack(units: "{\"8\": 7}"), "'units_per_color' del color 8 fuera de 1..6"),
            Invalid("units objeto vacío", TestPacks.Schema2Pack(units: "{}"), "'units_per_color' debe ser un entero de 1 a 6 o un objeto"),
            Invalid("units que no cuadra", TestPacks.Schema2Pack(units: "{\"8\": 5}"), "units_per_color: el color 8 tiene 4 dosis y se declararon 5"),
            Invalid("color 16", TestPacks.Schema2Pack(cuias: "[[8, 8, 8, 4], [4, 4, 4, 8], [16], []]"), "color 16 fuera de 1..15"),
            Invalid("intro desconocida", TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"aguape\""), "'intro_mechanic' debe ser null o"),
            Invalid("banda desconocida", TestPacks.Schema2Pack().Replace("\"difficulty\": { \"par\": 3 }", "\"difficulty\": { \"par\": 3, \"band\": \"easy\" }"), "'difficulty.band' debe ser"),
            Invalid("matapi con pack_schema 1", TestPacks.Schema2Pack("[null, null, null, {\"type\": \"matapi\"}]", schema: 1), "el tipo matapi necesita pack_schema 2"),
            Invalid("intro chica con pack_schema 1", TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"chica\"", schema: 1), "'intro_mechanic' chica necesita pack_schema 2"),
            Invalid("par_approx con pack_schema 1", TestPacks.Schema2Pack(schema: 1).Replace("\"difficulty\": { \"par\": 3 }", "\"difficulty\": { \"par\": 3, \"par_approx\": false }"), "'difficulty.par_approx' necesita pack_schema 2"),
            Invalid("tablero que no valida", TestPacks.Schema2Pack(cuias: "[[8, 8, 8, 8], [4, 4, 4, 4], [], []]"), "03 §17 #5: la cuia 0 empieza sellada"),
        };

        private static TestCaseData Invalid(string why, string json, string expected)
        {
            return new TestCaseData(json, expected).SetName("Reader_Schema2_RejectsInvalidMechanicsMurkyAndHidden(" + why + ")");
        }

        [TestCaseSource(nameof(InvalidSchema2Levels))]
        public void Reader_Schema2_RejectsInvalidMechanicsMurkyAndHidden(string json, string expected)
        {
            // Falla cerrado (BoardBuilder y LevelValidation, como tools/level-qa): el mensaje dice el id del nivel y el motivo.
            Assert.That(() => LevelPackReader.Read(json),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("m01_v1_n0001").And.Message.Contains(expected));
        }

        [Test]
        public void Reader_Schema1_AcceptsTheSameMechanicsWithoutMatapi()
        {
            // pack_schema 1 lee las mecánicas de siempre; solo el tipo matapi, las intro de D-007 y par_approx son del 2.
            LevelDefinition hoja = LevelPackReader.Read(TestPacks.Schema2Pack("[null, null, {\"type\": \"hoja\", \"key\": 4}, null]", schema: 1)).First;
            Assert.That(hoja.HasMechanic(MechanicType.Hoja), Is.True);
            Assert.That(LevelPackReader.Read(TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"hoja\"", schema: 1)).First.IntroMechanic, Is.EqualTo("hoja"));
        }

        // Tableros del subconjunto af-level/1 (caps [4, 4, 4]) que el lector rechaza, con el fragmento del mensaje que prueba
        // su motivo, como InvalidSchema2Levels: el formato lo comprueba ReadBoard, U_c (02 R3) BoardBuilder y la colocación del
        // Barro (02 §9.1 M5) LevelValidation.
        private static readonly TestCaseData[] InconsistentBoards =
        {
            Inconsistent("longitudes distintas", "[[8, 8, 8, 4], [4, 4, 4, 8]]", "'cuias' y 'caps' deben tener la misma longitud"),
            Inconsistent("más dosis que capacidad", "[[8, 8, 8, 4, 4], [4, 4, 8], []]", "la cuia 0 tiene más dosis que su capacidad"),
            Inconsistent("Barro fuera de su sitio", "[[8, 8, 8, 4], [4, 4, 4, 8], [15]]", "cuia 2, capa 0: 02 §9.1 M5: el Barro va en la capa 2 o 3"),
            Inconsistent("tinte 0", "[[8, 8, 8, 0], [4, 4, 4, 8], []]", "color 0 fuera de 1..15"),
            Inconsistent("color 16 reservado", "[[8, 8, 8, 4], [4, 4, 4, 8], [16]]", "color 16 fuera de 1..15"),
            Inconsistent("U_c distinto", "[[8, 8, 8, 4], [4, 4, 4, 8], [8]]", "units_per_color: el color 8 tiene 5 dosis y se declararon 4"),
        };

        private static TestCaseData Inconsistent(string why, string cuias, string expected)
        {
            return new TestCaseData(TestPacks.MinimalPack(TestPacks.MinimalLevel(cuias: cuias)), expected)
                .SetName("Reader_RejectsInconsistentBoards(" + why + ")");
        }

        [TestCaseSource(nameof(InconsistentBoards))]
        public void Reader_RejectsInconsistentBoards(string json, string expected)
        {
            Assert.That(() => LevelPackReader.Read(json),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("m01_v1_n0001").And.Message.Contains(expected));
        }

        [TestCase(null, LevelDefinition.LabelNormal)]
        [TestCase("\"normal\"", LevelDefinition.LabelNormal)]
        [TestCase("\"hard\"", LevelDefinition.LabelHard)]
        [TestCase("\"very_hard\"", LevelDefinition.LabelVeryHard)]
        [TestCase("null", LevelDefinition.LabelNormal)]
        public void Reader_ReadsTheDifficultyLabel(string? label, string expected)
        {
            // 08 §4.1: difficulty.label ∈ normal / hard / very_hard; sin etiqueta (subconjunto v0), normal. El chip Difícil o
            // Muy difícil del botón Jugar (06 §4.7 [P]) sale de aquí.
            string level = TestPacks.MinimalLevel();
            if (label != null)
            {
                level = level.Replace("\"difficulty\": { \"par\": 3 }", "\"difficulty\": { \"label\": " + label + ", \"par\": 3 }");
            }

            Assert.That(LevelPackReader.Read(TestPacks.MinimalPack(level)).First.DifficultyLabel, Is.EqualTo(expected));
        }

        [TestCase("\"Hard\"")]
        [TestCase("\"super_hard\"")]
        [TestCase("2")]
        public void Reader_RejectsAnUnknownDifficultyLabel(string label)
        {
            string level = TestPacks.MinimalLevel().Replace("\"difficulty\": { \"par\": 3 }", "\"difficulty\": { \"label\": " + label + ", \"par\": 3 }");
            Assert.That(() => LevelPackReader.Read(TestPacks.MinimalPack(level)), Throws.TypeOf<LevelFormatException>());
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
