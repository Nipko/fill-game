#nullable enable
using System;
using System.Globalization;
using System.Text;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Catálogo de niveles (tarea B1-5; 08 §2.2 <c>AF.Levels</c>: índice nivel → paquete): resuelve varios paquetes,
    /// detecta huecos y repetidos, da el siguiente disponible y aplica el desplazamiento de desarrollo
    /// <c>af_level_offset</c> [P].
    /// </summary>
    public sealed class LevelCatalogTests
    {
        [Test]
        public void TwoSyntheticPacks_ResolveEveryLevelToItsPack()
        {
            LevelPack a = Pack("mundo_a", 1, 2);
            LevelPack b = Pack("mundo_b", 3, 4);
            LevelCatalog catalog = LevelCatalog.Create(new[] { b, a });

            Assert.That(catalog.Count, Is.EqualTo(4));
            Assert.That(catalog.FirstLevel, Is.EqualTo(1));
            Assert.That(catalog.LastLevel, Is.EqualTo(4));
            Assert.That(catalog.IsContiguous, Is.True);
            Assert.That(catalog.Gaps.Count, Is.EqualTo(0));
            for (int level = 1; level <= 4; level++)
            {
                Assert.That(catalog.Contains(level), Is.True, "nivel " + level);
                Assert.That(catalog.Find(level)!.Level, Is.EqualTo(level));
                Assert.That(catalog.PackOf(level), Is.SameAs(level <= 2 ? a : b), "paquete del nivel " + level);
            }

            Assert.That(catalog.Contains(0), Is.False);
            Assert.That(catalog.Contains(5), Is.False);
            Assert.That(catalog.Find(5), Is.Null);
            Assert.That(catalog.PackOf(5), Is.Null);
            Assert.That(catalog.NextLevelAfter(2), Is.EqualTo(3), "del último del paquete A al primero del B");
            Assert.That(catalog.NextLevelAfter(4), Is.Null, "fin del contenido: «El río sigue creciendo»");
        }

        [Test]
        public void Gaps_AreDetected_AndTheMissingLevelIsNotAvailable()
        {
            LevelCatalog catalog = LevelCatalog.Create(new[] { Pack("mundo_a", 1, 2), Pack("mundo_c", 5, 6), Pack("mundo_d", 8, 8) });

            Assert.That(catalog.IsContiguous, Is.False);
            Assert.That(catalog.Gaps.Count, Is.EqualTo(2));
            Assert.That(catalog.Gaps[0].From, Is.EqualTo(3));
            Assert.That(catalog.Gaps[0].To, Is.EqualTo(4));
            Assert.That(catalog.Gaps[1].From, Is.EqualTo(7));
            Assert.That(catalog.Gaps[1].To, Is.EqualTo(7));
            Assert.That(catalog.DescribeGaps(), Is.EqualTo("3-4, 7"));
            Assert.That(catalog.NextLevelAfter(2), Is.Null, "el 3 falta: no se salta al 5");
            Assert.That(catalog.Contains(7), Is.False);
            Assert.That(catalog.LastLevel, Is.EqualTo(8));
        }

        [Test]
        public void ALevelInTwoPacks_IsRejected()
        {
            Assert.That(
                () => LevelCatalog.Create(new[] { Pack("mundo_a", 1, 3), Pack("mundo_b", 3, 4) }),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("3"));
        }

        [Test]
        public void EmptyListNullPackAndBadOffset_AreRejected()
        {
            Assert.That(() => LevelCatalog.Create(Array.Empty<LevelPack>()), Throws.TypeOf<ArgumentException>());
            Assert.That(() => LevelCatalog.Create(new LevelPack[] { null! }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => LevelCatalog.Create(new[] { Pack("a", 1, 1) }, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => LevelCatalog.Create(new[] { Pack("a", 1, 1) }, LevelCatalog.MaxLevelOffset + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Tutorial_IsACatalogOfLevels1To10WithoutGaps()
        {
            LevelCatalog catalog = LevelCatalog.FromPack(TestPacks.Tutorial);
            Assert.That(catalog.FirstLevel, Is.EqualTo(1));
            Assert.That(catalog.LastLevel, Is.EqualTo(10));
            Assert.That(catalog.IsContiguous, Is.True);
            Assert.That(catalog.LevelOffset, Is.EqualTo(0));
            Assert.That(catalog.WaterBase, Is.EqualTo("white"));
            Assert.That(catalog.Find(7), Is.SameAs(TestPacks.Tutorial.Find(7)));
        }

        [Test]
        public void Offset_ShiftsTheCampaignNumbers_ToTryTheGatesOf11_22And31()
        {
            // af_level_offset [P]: con 30, los tableros del tutorial se juegan como niveles 31-40 (Canoas, rescate).
            LevelCatalog catalog = LevelCatalog.Create(new[] { TestPacks.Tutorial }, 30);
            Assert.That(catalog.LevelOffset, Is.EqualTo(30));
            Assert.That(catalog.FirstLevel, Is.EqualTo(31));
            Assert.That(catalog.LastLevel, Is.EqualTo(40));
            Assert.That(catalog.Contains(1), Is.False);
            Assert.That(catalog.Find(31)!.Level, Is.EqualTo(1));
            Assert.That(catalog.Find(40)!.Level, Is.EqualTo(10));
            Assert.That(catalog.NextLevelAfter(40), Is.Null);
        }

        // Paquete subconjunto de af-level/1 con los niveles from..to (el tablero del nivel 1 de 02 §13 en todos).
        private static LevelPack Pack(string packId, int from, int to)
        {
            var levels = new StringBuilder();
            for (int level = from; level <= to; level++)
            {
                string id = "m01_v1_n" + level.ToString("0000", CultureInfo.InvariantCulture);
                string json = TestPacks.MinimalLevel().Replace(
                    "\"id\": \"m01_v1_n0001\", \"level\": 1,",
                    "\"id\": \"" + id + "\", \"level\": " + level.ToString(CultureInfo.InvariantCulture) + ",");
                levels.Append(level == from ? string.Empty : ", ").Append(json);
            }

            return LevelPackReader.Read("{ \"subset_of\": \"af-level/1\", \"pack_id\": \"" + packId + "\", \"kind\": \"campaign\", \"world\": 1, "
                + "\"world_id\": \"w01_estuario\", \"lap\": 1, \"water_base\": \"white\", \"levels\": [ " + levels + " ] }");
        }
    }
}
