#nullable enable
using System;
using System.Collections.Generic;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Lector del pool del Reto del río (tarea R6 de <c>docs/dev/traspaso/spark-reto-del-rio.md</c>; nota
    /// <c>docs/dev/reto-del-rio.md</c> §3, fila <c>AF.Levels</c>, y §13; 03 §16; esquema, rama <c>daily</c>):
    /// <see cref="DailyPoolReader"/> y <see cref="DailyPool"/>, con pools de prueba de <see cref="DailyPoolTestPacks"/>.
    /// </summary>
    public sealed class DailyPoolReaderTests
    {
        private static DailyPool? _full;

        // Último día del pool (728 con DailyPool.Days = 729; R-Q2b).
        private const int LastDay = DailyPool.Days - 1;

        // El pool completo (3 bandas × 729 días = 2.187 tableros; R-Q2b) se lee una vez.
        private static DailyPool Full => _full ??= DailyPoolReader.Read(DailyPoolTestPacks.Pool(DailyPoolTestPacks.Boards(1, 2, 3)));

        // ---------- Casos buenos ----------

        [Test]
        public void Read_FullPool_HasThreeBandsOf729Days()
        {
            DailyPool pool = Full;
            Assert.That(pool.PackId, Is.EqualTo(DailyPoolTestPacks.PackId));
            Assert.That(pool.PackSchema, Is.EqualTo(2));
            Assert.That(pool.BandCount, Is.EqualTo(3));
            Assert.That(pool.Count, Is.EqualTo(3 * DailyPool.Days));
            Assert.That(DailyPool.Days, Is.EqualTo(729), "R-Q2b: 81 × 9 días");
            for (int band = 1; band <= 3; band++)
            {
                Assert.That(pool.HasBand(band), Is.True, "banda " + band);
            }

            Assert.That(pool.HasBand(0), Is.False);
            Assert.That(pool.HasBand(4), Is.False);
        }

        [Test]
        public void Find_FirstAndLastDayOfEachBand_ReturnsTheBoardOfThatSlot(
            [Values(1, 2, 3)] int band,
            [Values(0, LastDay)] int day)
        {
            LevelDefinition? board = Full.Find(band, day);
            Assert.That(board, Is.Not.Null);
            Assert.That(board!.Id, Is.EqualTo(DailyPool.IdOf(band, day)));
            Assert.That(board.Level, Is.EqualTo(0), "el Reto no tiene número de nivel de campaña");
            int world = DailyPoolTestPacks.WorldOf(day);
            Assert.That(board.World, Is.EqualTo(world));
            Assert.That(board.WaterBase, Is.EqualTo(DailyPoolTestPacks.WaterOf(world)));
            Assert.That(board.CuiaCount, Is.EqualTo(3));
            Assert.That(board.Palette, Is.EqualTo(new[] { "acai", "aguaje" }));
            Assert.That(board.Par, Is.EqualTo(0), "sin difficulty, par 0");
            Assert.That(board.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelNormal));
            Assert.That(board.Band, Is.Null, "la banda del ciclo de 20 no aplica");
            Assert.That(board.Relief.Count, Is.EqualTo(0));
            Assert.That(board.HiddenCount, Is.EqualTo(0));
            Assert.That(board.IsMurky, Is.False);
            Assert.That(board.CreateBoard(), Is.Not.Null);
            Assert.That(Full.BandOf(board), Is.EqualTo(band));
            Assert.That(Full.DayOf(board), Is.EqualTo(day));
        }

        [Test]
        public void FindById_ReturnsTheSameBoardAsFind()
        {
            DailyPool pool = Full;
            Assert.That(pool.FindById("d2_n0417"), Is.SameAs(pool.Find(2, 417)));
            Assert.That(pool.FindById("d1_n0000"), Is.SameAs(pool.Find(1, 0)));
            Assert.That(pool.FindById("d3_n0728"), Is.SameAs(pool.Find(3, LastDay)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("d2_n417")]
        [TestCase("d4_n0001")]
        [TestCase("d1_n0729")]
        [TestCase("d1_n0730")]
        [TestCase("D1_n0001")]
        [TestCase("m01_v1_n0001")]
        public void FindById_UnknownId_ReturnsNull(string? id)
        {
            Assert.That(Full.FindById(id), Is.Null);
        }

        [TestCase(0, 0)]
        [TestCase(4, 0)]
        [TestCase(1, -1)]
        [TestCase(1, DailyPool.Days)]
        [TestCase(1, 730)]
        [TestCase(-1, 5)]
        public void Find_OutOfRange_ReturnsNull(int band, int day)
        {
            Assert.That(Full.Find(band, day), Is.Null);
        }

        [Test]
        public void BandOfAndDayOf_ForeignBoard_AreZeroAndMinusOne()
        {
            LevelDefinition foreign = TestPacks.Tutorial.First;
            Assert.That(Full.BandOf(foreign), Is.EqualTo(0));
            Assert.That(Full.DayOf(foreign), Is.EqualTo(-1));
        }

        [Test]
        public void Read_PresentBands_AcceptsOneCompleteBand()
        {
            DailyPool pool = DailyPoolReader.Read(DailyPoolTestPacks.Pool(DailyPoolTestPacks.Boards(2)), DailyPoolCoverage.PresentBands);
            Assert.That(pool.BandCount, Is.EqualTo(1));
            Assert.That(pool.Count, Is.EqualTo(DailyPool.Days));
            Assert.That(pool.HasBand(2), Is.True);
            Assert.That(pool.HasBand(1), Is.False);
            Assert.That(pool.Find(1, 0), Is.Null);
            Assert.That(pool.Find(2, LastDay)!.Id, Is.EqualTo("d2_n0728"));
            Assert.That(pool.FindById("d1_n0000"), Is.Null);
        }

        [Test]
        public void Read_OrderOfBoardsDoesNotMatter()
        {
            List<string> boards = DailyPoolTestPacks.Boards(3);
            boards.Reverse();
            DailyPool pool = DailyPoolReader.Read(DailyPoolTestPacks.Pool(boards), DailyPoolCoverage.PresentBands);
            Assert.That(pool.Find(3, 0)!.Id, Is.EqualTo("d3_n0000"));
            Assert.That(pool.Find(3, LastDay)!.Id, Is.EqualTo("d3_n0728"));
        }

        [Test]
        public void Read_OptionalDifficulty_ReadsParAndLabel()
        {
            List<string> boards = DailyPoolTestPacks.Boards(1);
            boards[5] = DailyPoolTestPacks.Board(1, 5, ",\"difficulty\":{\"label\":\"hard\",\"bot_fail\":0.5,\"par\":12,\"band\":\"challenge\"}");
            DailyPool pool = DailyPoolReader.Read(DailyPoolTestPacks.Pool(boards), DailyPoolCoverage.PresentBands);
            Assert.That(pool.Find(1, 5)!.Par, Is.EqualTo(12));
            Assert.That(pool.Find(1, 5)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelHard));
            Assert.That(pool.Find(1, 6)!.Par, Is.EqualTo(0));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Read_AcceptsEveryKnownPackSchema(int packSchema)
        {
            DailyPool pool = DailyPoolReader.Read(
                DailyPoolTestPacks.Pool(DailyPoolTestPacks.Boards(1), DailyPoolTestPacks.Header(packSchema)), DailyPoolCoverage.PresentBands);
            Assert.That(pool.PackSchema, Is.EqualTo(packSchema));
        }

        [Test]
        public void IdOfAndSeedOf_UseTheExactPadding()
        {
            Assert.That(DailyPool.IdOf(2, 417), Is.EqualTo("d2_n0417"));
            Assert.That(DailyPool.SeedOf(2, 417), Is.EqualTo("amazonfill:v1:daily:b2:d417"));
            Assert.That(DailyPool.IdOf(1, 5), Is.EqualTo("d1_n0005"));
            Assert.That(DailyPool.SeedOf(1, 5), Is.EqualTo("amazonfill:v1:daily:b1:d005"));
            Assert.That(DailyPool.IdOf(3, 0), Is.EqualTo("d3_n0000"));
            Assert.That(DailyPool.SeedOf(3, LastDay), Is.EqualTo("amazonfill:v1:daily:b3:d728"));
            Assert.Throws<ArgumentOutOfRangeException>(() => DailyPool.IdOf(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => DailyPool.SeedOf(1, DailyPool.Days));
            Assert.Throws<ArgumentOutOfRangeException>(() => DailyPool.IdOf(1, DailyPool.Days));
        }

        [Test]
        public void FindAndFindById_DoNotAllocate()
        {
            DailyPool pool = Full;
            var ids = new string[3 * DailyPool.Days];
            for (int band = 1; band <= 3; band++)
            {
                for (int day = 0; day < DailyPool.Days; day++)
                {
                    ids[(band - 1) * DailyPool.Days + day] = DailyPool.IdOf(band, day);
                }
            }

            int found = Lookups(pool, ids);
            Assert.That(found, Is.EqualTo(2 * 3 * DailyPool.Days));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 20; i++)
            {
                found = Lookups(pool, ids);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(found, Is.EqualTo(2 * 3 * DailyPool.Days));
            Assert.That(allocated, Is.EqualTo(0L), "Find y FindById no asignan memoria (nota §3, fila AF.Levels)");
        }

        private static int Lookups(DailyPool pool, string[] ids)
        {
            int found = 0;
            for (int band = 1; band <= 3; band++)
            {
                for (int day = 0; day < DailyPool.Days; day++)
                {
                    LevelDefinition? board = pool.Find(band, day);
                    if (board != null && pool.BandOf(board) == band && pool.DayOf(board) == day)
                    {
                        found++;
                    }

                    if (pool.FindById(ids[(band - 1) * DailyPool.Days + day]) == board)
                    {
                        found++;
                    }
                }
            }

            return found;
        }

        // ---------- Rechazos ----------

        [Test]
        public void Read_NotJson_Throws()
        {
            AssertRejects("{ esto no es json", "no es JSON válido");
        }

        [TestCase("campaign")]
        [TestCase("event")]
        [TestCase("Daily")]
        public void Read_KindOtherThanDaily_Throws(string kind)
        {
            AssertRejects(DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 0), DailyPoolTestPacks.Header(kind: kind)), "'kind' es '" + kind + "'");
        }

        [Test]
        public void Read_MissingKind_Throws()
        {
            AssertRejects(DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 0), "\"pack_schema\":2,\"pack_id\":\"daily_pool_v1\""), "falta la cadena 'kind'");
        }

        [Test]
        public void Read_PackSchemaNewerThanSupported_Throws()
        {
            int newer = LevelPackReader.SupportedPackSchema + 1;
            AssertRejects(DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 0), DailyPoolTestPacks.Header(newer)), "pack_schema " + newer + " no admitido");
        }

        [Test]
        public void Read_PackSchemaZero_Throws()
        {
            AssertRejects(DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 0), DailyPoolTestPacks.Header(0)), "pack_schema 0 no admitido");
        }

        [Test]
        public void Read_WithoutPackSchema_Throws()
        {
            AssertRejects(
                DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 0), "\"subset_of\":\"af-level/1\",\"pack_id\":\"daily_pool_v1\",\"kind\":\"daily\""),
                "debe declarar pack_schema");
        }

        [TestCase("\"id\":\"d1_n0005\"", "\"id\":\"d1_n005\"", "el id 'd1_n005' no corresponde al slot")]
        [TestCase("\"id\":\"d1_n0005\"", "\"id\":\"d2_n0005\"", "el id 'd2_n0005' no corresponde al slot")]
        [TestCase("\"id\":\"d1_n0005\"", "\"id\":\"d1_n0006\"", "pide 'd1_n0005'")]
        [TestCase(":d005\"", ":d0005\"", "la semilla 'amazonfill:v1:daily:b1:d0005' no corresponde al slot")]
        [TestCase(":d005\"", ":d5\"", "no corresponde al slot, que pide 'amazonfill:v1:daily:b1:d005'")]
        [TestCase("daily:b1:", "daily:b2:", "la semilla 'amazonfill:v1:daily:b2:d005'")]
        [TestCase(":d005\"", ":d006\"", "la semilla 'amazonfill:v1:daily:b1:d006'")]
        public void Read_SlotIncoherentWithIdOrSeed_Throws(string from, string to, string message)
        {
            string board = DailyPoolTestPacks.Board(1, 5);
            Assert.That(board, Does.Contain(from), "el caso cambia algo que existe");
            AssertRejects(DailyPoolTestPacks.Single(board.Replace(from, to)), message);
        }

        [TestCase("\"band\":1", "\"band\":4", "'slot.band' 4 fuera de 1-3")]
        [TestCase("\"band\":1", "\"band\":0", "'slot.band' 0 fuera de 1-3")]
        [TestCase("\"day\":5", "\"day\":729", "'slot.day' 729 fuera de 0-728")]
        [TestCase("\"day\":5", "\"day\":730", "'slot.day' 730 fuera de 0-728")]
        [TestCase("\"day\":5", "\"day\":-1", "'slot.day' -1 fuera de 0-728")]
        [TestCase("\"slot\":{\"band\":1,\"day\":5}", "\"level\":5", "lleva 'slot' y no 'level'")]
        [TestCase("\"slot\":{\"band\":1,\"day\":5}", "\"slat\":1", "falta el objeto 'slot'")]
        [TestCase("\"day\":5", "\"dia\":5", "falta el entero 'day'")]
        public void Read_BadSlot_Throws(string from, string to, string message)
        {
            string board = DailyPoolTestPacks.Board(1, 5);
            Assert.That(board, Does.Contain(from));
            AssertRejects(DailyPoolTestPacks.Single(board.Replace(from, to)), message);
        }

        [Test]
        public void Read_LevelAndSlotTogether_Throws()
        {
            AssertRejects(DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 5, ",\"level\":5")), "d1_n0005): un tablero del Reto lleva 'slot' y no 'level'");
        }

        [Test]
        public void Read_IncompleteBand_Throws()
        {
            List<string> boards = DailyPoolTestPacks.Boards(1);
            boards.RemoveAt(5);
            AssertRejects(DailyPoolTestPacks.Pool(boards), "la banda 1 está incompleta: tiene 728 de 729 días (falta el día 5, d1_n0005", DailyPoolCoverage.PresentBands);
            AssertRejects(DailyPoolTestPacks.Pool(boards), "la banda 1 está incompleta");
        }

        [Test]
        public void Read_IncompleteBandInFullPool_Throws()
        {
            List<string> boards = DailyPoolTestPacks.Boards(1, 2, 3);
            boards.RemoveAt(DailyPool.Days + LastDay);
            AssertRejects(DailyPoolTestPacks.Pool(boards), "la banda 2 está incompleta: tiene 728 de 729 días (falta el día 728, d2_n0728");
        }

        [Test]
        public void Read_MissingBandInDefaultMode_Throws()
        {
            AssertRejects(DailyPoolTestPacks.Pool(DailyPoolTestPacks.Boards(1, 3)), "falta la banda 2");
        }

        [Test]
        public void Read_RepeatedDay_Throws()
        {
            List<string> boards = DailyPoolTestPacks.Boards(1);
            boards.Add(DailyPoolTestPacks.Board(1, 417));
            AssertRejects(DailyPoolTestPacks.Pool(boards), "levels[729] (d1_n0417): la banda 1 y el día 417 están repetidos (ya los tiene d1_n0417)", DailyPoolCoverage.PresentBands);
        }

        [TestCase("solution", "[[0,2]]")]
        [TestCase("safe_path", "[[0,2]]")]
        [TestCase("qa", "{\"ok\":true}")]
        public void Read_FieldExcludedBySchema_Throws(string field, string value)
        {
            AssertRejects(
                DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 5, ",\"" + field + "\":" + value)),
                "d1_n0005: '" + field + "' no puede viajar en el paquete");
        }

        [Test]
        public void Read_NonEmptyRelief_Throws()
        {
            string relief = "[{\"type\":\"plus_empty\",\"caps\":[4,4,4,4],\"cuias\":[[8,8,8,4],[4,4,4,8],[],[]],\"par\":null,\"bot_fail\":0.1}]";
            AssertRejects(
                DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 5).Replace("\"relief\":[]", "\"relief\":" + relief)),
                "d1_n0005: 'relief' debe estar vacío en el Reto del río (sin alivio; 03 §16) y trae 1 variantes");
        }

        [Test]
        public void Read_MissingRelief_Throws()
        {
            AssertRejects(
                DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 5).Replace(",\"relief\":[]", "")),
                "'relief' debe ser una lista vacía");
        }

        [TestCase("\"attrs\":[null,null,null]", "\"attrs\":[{\"type\":\"hoja\",\"key\":8},null,null]", "attrs[0] lleva una mecánica")]
        [TestCase("\"intro_mechanic\":null", "\"intro_mechanic\":\"hoja\"", "'intro_mechanic' debe ser null")]
        [TestCase("\"murky\":false,\"hidden\":[]", "\"murky\":true,\"hidden\":[[0,0]]", "'murky' debe ser false")]
        [TestCase("\"hidden\":[]", "\"hidden\":[[0,0]]", "'hidden' debe estar vacío")]
        public void Read_MechanicsOrHiddenDoses_Throws(string from, string to, string message)
        {
            string board = DailyPoolTestPacks.Board(1, 5);
            Assert.That(board, Does.Contain(from));
            AssertRejects(DailyPoolTestPacks.Single(board.Replace(from, to)), "d1_n0005: " + message);
        }

        [Test]
        public void Read_InvalidBoard_ThrowsWithTheId()
        {
            // Tres dosis del color 8 y cinco del 4: units_per_color 4 no cuadra (02 R3).
            string board = DailyPoolTestPacks.Board(1, 5).Replace("[[8,8,8,4],[4,4,4,8],[]]", "[[8,8,4,4],[4,4,4,8],[]]");
            AssertRejects(DailyPoolTestPacks.Single(board), "d1_n0005: ");
        }

        [Test]
        public void Read_BadDifficultyPar_Throws()
        {
            AssertRejects(
                DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 5, ",\"difficulty\":{\"label\":\"normal\",\"bot_fail\":0.5,\"par\":0,\"band\":\"comfort\"}")),
                "d1_n0005: 'difficulty.par' debe ser ≥ 1");
        }

        // ---------- Revisión de R6: campos opcionales, estructura y la copia de ReadUnits/ReadBoard ----------

        // Tablero d1_n0000 copiado literal de tools/out/daily_sample/daily_pool_v1.json (salida de R3, level-generator 1.0.0):
        // si el generador cambia el formato, este test del cliente se rompe.
        private const string GeneratedBoardD1N0000 =
            "{\"schema\":\"af-level/1\",\"id\":\"d1_n0000\",\"slot\":{\"band\":1,\"day\":0},\"world\":1,\"seed\":\"amazonfill:v1:daily:b1:d000\","
            + "\"gen\":{\"tool\":\"level-generator\",\"version\":\"1.0.0\",\"source\":\"walk\",\"depth\":15,\"candidates\":14},\"water_base\":\"white\","
            + "\"palette\":[\"guanabana\",\"tapereba\",\"aguaje\",\"camu_camu\",\"acai\",\"agua_tapajos\"],\"units_per_color\":4,"
            + "\"caps\":[4,4,4,4,4,4,4,4],\"cuias\":[[4,4,8,10],[],[1,1,6,3],[4,3,10,3],[6,6,6,10],[8,8,10,3],[],[4,1,1,8]],"
            + "\"attrs\":[null,null,null,null,null,null,null,null],\"murky\":false,\"hidden\":[],\"risk_pairs\":0,\"intro_mechanic\":null,"
            + "\"skippable\":false,\"hint_supported\":true,\"relief\":[]}";

        [Test]
        public void Read_BoardCopiedFromTheGenerator_IsRead()
        {
            // Dentro de un pool sintético completo de la banda 1 (PresentBands exige los 729 días).
            List<string> boards = DailyPoolTestPacks.Boards(1);
            boards[0] = GeneratedBoardD1N0000;
            DailyPool pool = DailyPoolReader.Read(DailyPoolTestPacks.Pool(boards), DailyPoolCoverage.PresentBands);
            LevelDefinition board = pool.FindById("d1_n0000")!;
            Assert.That(board, Is.SameAs(pool.Find(1, 0)));
            Assert.That(board.World, Is.EqualTo(1));
            Assert.That(board.WaterBase, Is.EqualTo("white"));
            Assert.That(board.CuiaCount, Is.EqualTo(8));
            Assert.That(board.Palette.Count, Is.EqualTo(6));
            Assert.That(board.CopyDoses(0), Is.EqualTo(new[] { 4, 4, 8, 10 }));
            Assert.That(board.CopyDoses(1), Is.Empty);
            Assert.That(board.Skippable, Is.False);
            Assert.That(board.HintSupported, Is.True);
            Assert.That(board.Par, Is.EqualTo(0));
            Assert.That(board.CreateBoard(), Is.Not.Null);
        }

        [Test]
        public void Read_OptionalFields_WorldMissingDifficultyWithoutLabelAndFalseFlags()
        {
            List<string> boards = DailyPoolTestPacks.Boards(2);
            string world3 = "\"world\":" + DailyPoolTestPacks.WorldOf(3).ToString(System.Globalization.CultureInfo.InvariantCulture) + ",";
            boards[3] = DailyPoolTestPacks.Board(2, 3).Replace(world3, "");
            boards[4] = DailyPoolTestPacks.Board(2, 4, ",\"difficulty\":{\"bot_fail\":0.5,\"par\":14}");
            boards[5] = DailyPoolTestPacks.Board(2, 5).Replace("\"skippable\":true,\"hint_supported\":true", "\"skippable\":false,\"hint_supported\":false");
            boards[6] = DailyPoolTestPacks.Board(2, 6).Replace("\"units_per_color\":4", "\"units_per_color\":{\"8\":4}");
            Assert.That(boards[3], Does.Not.Contain("\"world\""));
            Assert.That(boards[5], Does.Contain("\"skippable\":false"));
            Assert.That(boards[6], Does.Contain("{\"8\":4}"));
            DailyPool pool = DailyPoolReader.Read(DailyPoolTestPacks.Pool(boards), DailyPoolCoverage.PresentBands);
            Assert.That(pool.Find(2, 3)!.World, Is.EqualTo(0), "sin world, 0");
            Assert.That(pool.Find(2, 4)!.Par, Is.EqualTo(14));
            Assert.That(pool.Find(2, 4)!.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelNormal), "difficulty sin label, normal");
            Assert.That(pool.Find(2, 5)!.Skippable, Is.False);
            Assert.That(pool.Find(2, 5)!.HintSupported, Is.False);
            Assert.That(pool.Find(2, 6)!.Skippable, Is.True);
            Assert.That(pool.Find(2, 6)!.HintSupported, Is.True);
            Assert.That(pool.Find(2, 6)!.CreateBoard(), Is.Not.Null);
        }

        [TestCase("\"world\":6", "\"world\":0", "'world' 0 fuera de 1-13")]
        [TestCase("\"world\":6", "\"world\":14", "'world' 14 fuera de 1-13")]
        [TestCase("\"world\":6", "\"world\":\"6\"", "'world' debe ser un entero")]
        [TestCase("\"skippable\":true", "\"skippable\":\"no\"", "'skippable' debe ser un booleano")]
        [TestCase("\"hint_supported\":true", "\"hint_supported\":1", "'hint_supported' debe ser un booleano")]
        [TestCase("\"murky\":false", "\"murky\":0", "'murky' debe ser un booleano")]
        [TestCase("\"attrs\":[null,null,null]", "\"attrs\":[null,null]", "'attrs' debe tener una entrada por cuia")]
        [TestCase("\"attrs\":[null,null,null]", "\"attrs\":5", "'attrs' debe tener una entrada por cuia")]
        [TestCase("\"hidden\":[]", "\"hidden\":5", "'hidden' debe estar vacío")]
        [TestCase("\"relief\":[]", "\"relief\":{}", "'relief' debe ser una lista vacía")]
        [TestCase("\"water_base\":\"white\"", "\"water_base\":\"brown\"", "'water_base' debe ser black, white o clear")]
        [TestCase("\"palette\":[\"acai\",\"aguaje\"]", "\"palette\":[\"acai\",3]", "'palette' debe ser una lista de cadenas")]
        [TestCase("\"units_per_color\":4", "\"units_per_color\":7", "'units_per_color' fuera de 1..6")]
        [TestCase("\"units_per_color\":4", "\"units_per_color\":{\"17\":4}", "'units_per_color' tiene la clave '17'")]
        [TestCase("\"units_per_color\":4", "\"units_per_color\":{\"08\":4}", "'units_per_color' tiene la clave '08'")]
        [TestCase("\"units_per_color\":4", "\"units_per_color\":{\"8\":0}", "'units_per_color' del color 8 fuera de 1..6")]
        [TestCase("\"units_per_color\":4", "\"units_per_color\":{}", "'units_per_color' debe ser un entero de 1 a 6 o un objeto")]
        [TestCase("\"units_per_color\":4,", "", "falta 'units_per_color'")]
        [TestCase("\"caps\":[4,4,4]", "\"caps\":[4,4]", "'cuias' y 'caps' deben tener la misma longitud")]
        [TestCase("\"caps\":[4,4,4]", "\"caps\":[7,4,4]", "capacidad fuera de 1..6 en la cuia 0")]
        [TestCase("\"caps\":[4,4,4],\"cuias\":[[8,8,8,4],[4,4,4,8],[]]", "\"caps\":[],\"cuias\":[]", "'caps' debe tener entre 1 y 16 cuias")]
        [TestCase("\"caps\":[4,4,4]", "\"caps\":4", "'caps' debe ser una lista")]
        [TestCase("[[8,8,8,4],[4,4,4,8],[]]", "[[8,8,8,4,4],[4,4,8],[]]", "la cuia 0 tiene más dosis que su capacidad")]
        [TestCase("[[8,8,8,4],[4,4,4,8],[]]", "[[8,8,8,4],5,[]]", "cada cuia debe ser una lista de dosis")]
        [TestCase("[[8,8,8,4],[4,4,4,8],[]]", "[[8,8,8,16],[4,4,4,8],[]]", "color 16 fuera de 1..15")]
        [TestCase("[[8,8,8,4],[4,4,4,8],[]]", "[[8,8,8,0],[4,4,4,8],[]]", "color 0 fuera de 1..15")]
        public void Read_BadBoardField_Throws(string from, string to, string message)
        {
            string board = DailyPoolTestPacks.Board(1, 5);
            Assert.That(board, Does.Contain(from), "el caso cambia algo que existe");
            AssertRejects(DailyPoolTestPacks.Single(board.Replace(from, to)), "d1_n0005: " + message);
        }

        [TestCase(",\"difficulty\":5", "'difficulty' debe ser un objeto")]
        [TestCase(",\"difficulty\":{\"label\":\"easy\",\"bot_fail\":0.5,\"par\":12,\"band\":\"comfort\"}", "'difficulty.label' debe ser normal, hard o very_hard")]
        [TestCase(",\"difficulty\":{\"label\":3,\"par\":12}", "'difficulty.label' debe ser normal, hard o very_hard")]
        [TestCase(",\"difficulty\":{\"label\":\"normal\"}", "falta el entero 'par'")]
        public void Read_BadDifficulty_Throws(string extra, string message)
        {
            AssertRejects(DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 5, extra)), "d1_n0005: " + message);
        }

        [TestCase("[1, 2]", "La raíz del pool del Reto del río debe ser un objeto")]
        [TestCase("\"daily\"", "La raíz del pool del Reto del río debe ser un objeto")]
        [TestCase("{\"pack_schema\":2,\"kind\":\"daily\",\"levels\":[]}", "pool: falta la cadena 'pack_id'")]
        [TestCase("{\"pack_schema\":2,\"pack_id\":\"daily_pool_v1\",\"kind\":\"daily\"}", "daily_pool_v1: 'levels' debe ser una lista no vacía")]
        [TestCase("{\"pack_schema\":2,\"pack_id\":\"daily_pool_v1\",\"kind\":\"daily\",\"levels\":[]}", "daily_pool_v1: 'levels' debe ser una lista no vacía")]
        [TestCase("{\"pack_schema\":2,\"pack_id\":\"daily_pool_v1\",\"kind\":\"daily\",\"levels\":{}}", "daily_pool_v1: 'levels' debe ser una lista no vacía")]
        [TestCase("{\"pack_schema\":2,\"pack_id\":\"daily_pool_v1\",\"kind\":\"daily\",\"levels\":[5]}", "daily_pool_v1.levels[0]: cada tablero debe ser un objeto")]
        [TestCase("{\"pack_schema\":\"2\",\"pack_id\":\"daily_pool_v1\",\"kind\":\"daily\",\"levels\":[]}", "daily_pool_v1: 'pack_schema' debe ser un entero")]
        public void Read_BadStructure_Throws(string json, string message)
        {
            AssertRejects(json, message);
        }

        [Test]
        public void Read_UnknownCoverage_ThrowsArgumentOutOfRange()
        {
            string json = DailyPoolTestPacks.Single(DailyPoolTestPacks.Board(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => DailyPoolReader.Read(json, (DailyPoolCoverage)7));
        }

        private static void AssertRejects(string json, string message, DailyPoolCoverage coverage = DailyPoolCoverage.AllBands)
        {
            LevelFormatException? e = Assert.Throws<LevelFormatException>(() => DailyPoolReader.Read(json, coverage));
            Assert.That(e!.Message, Does.Contain(message));
        }
    }
}
