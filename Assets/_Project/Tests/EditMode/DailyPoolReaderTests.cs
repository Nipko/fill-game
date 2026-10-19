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

        // El pool completo (3 bandas × 730 días = 2.190 tableros) se lee una vez.
        private static DailyPool Full => _full ??= DailyPoolReader.Read(DailyPoolTestPacks.Pool(DailyPoolTestPacks.Boards(1, 2, 3)));

        // ---------- Casos buenos ----------

        [Test]
        public void Read_FullPool_HasThreeBandsOf730Days()
        {
            DailyPool pool = Full;
            Assert.That(pool.PackId, Is.EqualTo(DailyPoolTestPacks.PackId));
            Assert.That(pool.PackSchema, Is.EqualTo(2));
            Assert.That(pool.BandCount, Is.EqualTo(3));
            Assert.That(pool.Count, Is.EqualTo(3 * 730));
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
            [Values(0, 729)] int day)
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
            Assert.That(pool.FindById("d3_n0729"), Is.SameAs(pool.Find(3, 729)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("d2_n417")]
        [TestCase("d4_n0001")]
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
            Assert.That(pool.Count, Is.EqualTo(730));
            Assert.That(pool.HasBand(2), Is.True);
            Assert.That(pool.HasBand(1), Is.False);
            Assert.That(pool.Find(1, 0), Is.Null);
            Assert.That(pool.Find(2, 729)!.Id, Is.EqualTo("d2_n0729"));
            Assert.That(pool.FindById("d1_n0000"), Is.Null);
        }

        [Test]
        public void Read_OrderOfBoardsDoesNotMatter()
        {
            List<string> boards = DailyPoolTestPacks.Boards(3);
            boards.Reverse();
            DailyPool pool = DailyPoolReader.Read(DailyPoolTestPacks.Pool(boards), DailyPoolCoverage.PresentBands);
            Assert.That(pool.Find(3, 0)!.Id, Is.EqualTo("d3_n0000"));
            Assert.That(pool.Find(3, 729)!.Id, Is.EqualTo("d3_n0729"));
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
            Assert.That(DailyPool.SeedOf(3, 729), Is.EqualTo("amazonfill:v1:daily:b3:d729"));
            Assert.Throws<ArgumentOutOfRangeException>(() => DailyPool.IdOf(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => DailyPool.SeedOf(1, 730));
        }

        [Test]
        public void FindAndFindById_DoNotAllocate()
        {
            DailyPool pool = Full;
            var ids = new string[3 * 730];
            for (int band = 1; band <= 3; band++)
            {
                for (int day = 0; day < 730; day++)
                {
                    ids[(band - 1) * 730 + day] = DailyPool.IdOf(band, day);
                }
            }

            int found = Lookups(pool, ids);
            Assert.That(found, Is.EqualTo(2 * 3 * 730));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 20; i++)
            {
                found = Lookups(pool, ids);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(found, Is.EqualTo(2 * 3 * 730));
            Assert.That(allocated, Is.EqualTo(0L), "Find y FindById no asignan memoria (nota §3, fila AF.Levels)");
        }

        private static int Lookups(DailyPool pool, string[] ids)
        {
            int found = 0;
            for (int band = 1; band <= 3; band++)
            {
                for (int day = 0; day < 730; day++)
                {
                    LevelDefinition? board = pool.Find(band, day);
                    if (board != null && pool.BandOf(board) == band && pool.DayOf(board) == day)
                    {
                        found++;
                    }

                    if (pool.FindById(ids[(band - 1) * 730 + day]) == board)
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
        [TestCase("\"day\":5", "\"day\":730", "'slot.day' 730 fuera de 0-729")]
        [TestCase("\"day\":5", "\"day\":-1", "'slot.day' -1 fuera de 0-729")]
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
            AssertRejects(DailyPoolTestPacks.Pool(boards), "la banda 1 está incompleta: tiene 729 de 730 días (falta el día 5, d1_n0005", DailyPoolCoverage.PresentBands);
            AssertRejects(DailyPoolTestPacks.Pool(boards), "la banda 1 está incompleta");
        }

        [Test]
        public void Read_IncompleteBandInFullPool_Throws()
        {
            List<string> boards = DailyPoolTestPacks.Boards(1, 2, 3);
            boards.RemoveAt(730 + 729);
            AssertRejects(DailyPoolTestPacks.Pool(boards), "la banda 2 está incompleta: tiene 729 de 730 días (falta el día 729, d2_n0729");
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
            AssertRejects(DailyPoolTestPacks.Pool(boards), "levels[730] (d1_n0417): la banda 1 y el día 417 están repetidos (ya los tiene d1_n0417)", DailyPoolCoverage.PresentBands);
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

        private static void AssertRejects(string json, string message, DailyPoolCoverage coverage = DailyPoolCoverage.AllBands)
        {
            LevelFormatException? e = Assert.Throws<LevelFormatException>(() => DailyPoolReader.Read(json, coverage));
            Assert.That(e!.Message, Does.Contain(message));
        }
    }
}
