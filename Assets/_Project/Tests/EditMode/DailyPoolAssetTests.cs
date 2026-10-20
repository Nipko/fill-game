#nullable enable
using System.IO;
using AF.Levels;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El pool publicado del Reto del río (tarea R5 de <c>docs/dev/traspaso/spark-reto-del-rio.md</c>):
    /// <c>Assets/_Project/Data/Daily/daily_pool_v1.json</c>, generado por <c>tools/level-generator --daily</c> y validado por
    /// <c>tools/level-qa --daily</c>, se lee con <see cref="DailyPoolReader"/> en el modo estricto del juego
    /// (<see cref="DailyPoolCoverage.AllBands"/>, que ya comprueba la semilla de cada slot): 3 bandas × 729 días (03 §16; R-Q2b), con el
    /// slot y el id de cada día, sin alivio, y el K y los vacíos de su banda (03 §16 con R-Q1b (a): B1 6-7 con 2 vacíos, B2 solo 8
    /// y B3 10-11 con 1).
    /// </summary>
    public sealed class DailyPoolAssetTests
    {
        private static DailyPool? _pool;

        private static DailyPool Pool => _pool ??= DailyPoolReader.Read(
            File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Data", "Daily", "daily_pool_v1.json")));

        [Test]
        public void PublishedPool_ReadsStrict_ThreeBandsOf729Days()
        {
            DailyPool pool = Pool;
            Assert.That(pool.PackId, Is.EqualTo("daily_pool_v1"));
            Assert.That(pool.BandCount, Is.EqualTo(3));
            Assert.That(DailyPool.Days, Is.EqualTo(729), "R-Q2b: 81 × 9 días");
            Assert.That(pool.Count, Is.EqualTo(3 * DailyPool.Days));
            for (int band = DailyPool.MinBand; band <= DailyPool.MaxBand; band++)
            {
                Assert.That(pool.HasBand(band), Is.True, "banda " + band);
                for (int day = 0; day < DailyPool.Days; day++)
                {
                    LevelDefinition? board = pool.Find(band, day);
                    Assert.That(board, Is.Not.Null, DailyPool.IdOf(band, day));
                    Assert.That(board!.Id, Is.EqualTo(DailyPool.IdOf(band, day)));
                    Assert.That(board.Relief.Count, Is.EqualTo(0), "03 §16: sin alivio");
                    Assert.That(pool.BandOf(board), Is.EqualTo(band));
                    Assert.That(pool.DayOf(board), Is.EqualTo(day));
                    Assert.That(board.World, Is.EqualTo(1 + (day % 9)), "R-Q2b: mundo 1 + (día mod 9)");
                }
            }
        }

        [Test]
        public void PublishedPool_KAndEmptiesPerBand_MatchR_Q1b()
        {
            DailyPool pool = Pool;
            for (int day = 0; day < DailyPool.Days; day++)
            {
                int k1 = day % 2 == 0 ? 6 : 7;
                int k3 = day % 2 == 0 ? 10 : 11;
                Assert.That(pool.Find(1, day)!.Palette.Count, Is.EqualTo(k1), "B1 día " + day);
                Assert.That(pool.Find(1, day)!.CuiaCount, Is.EqualTo(k1 + 2), "B1 día " + day + ": 2 vacíos");
                Assert.That(pool.Find(2, day)!.Palette.Count, Is.EqualTo(8), "B2 día " + day + " (R-Q1b (a): solo K = 8)");
                Assert.That(pool.Find(2, day)!.CuiaCount, Is.EqualTo(9), "B2 día " + day + ": 1 vacío");
                Assert.That(pool.Find(3, day)!.Palette.Count, Is.EqualTo(k3), "B3 día " + day);
                Assert.That(pool.Find(3, day)!.CuiaCount, Is.EqualTo(k3 + 1), "B3 día " + day + ": 1 vacío");
            }
        }
    }
}
