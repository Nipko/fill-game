#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Frecuencia de la tropa lateral (D-021, punto 2): función pura y determinista por el número de nivel, sin azar. El primer
    /// nivel de 3 filas de cada tramo (mundo y, cuando exista el elenco, especie nueva) va siempre con la tropa lateral; de los
    /// demás de 3 filas, 1 de cada 3; los reintentos, siempre en dosel. Con los paquetes reales de 1-200 a 360 × 780 dp (y a
    /// 360 × 800, el 720 × 1600 px): 3 filas desde el 86 y en 77 de los 100 niveles del 101-200 (la medida de D-021); de los
    /// 82 niveles de 3 filas del 86-200, 28 con la tropa lateral (los primeros de los mundos 4 y 5, el 86 y el 143) y 54 en
    /// dosel.
    /// </summary>
    public sealed class IntroSideTroopScheduleTests
    {
        // Lienzo de 360 × 780 dp (1080 × 2340 px, zona segura de 32 dp arriba y 24 abajo): el tablero va de safeTop + 56 = 88
        // a 780 − 24 − 16 − 80 − 40 = 620 (GameScreen.BoardTop y BoardBottom).
        private const float Width = 360f;
        private const float BoardTop = 88f;
        private const float BoardBottom780 = 620f;

        // 360 × 800 dp (720 × 1600 px): el tablero baja hasta 640.
        private const float BoardBottom800 = 640f;

        private static readonly int[] ExpectedSide =
        {
            86, 95, 104, 107, 111, 115, 118, 124, 127, 132, 136, 139, 143, 147, 150, 153, 156, 159, 165, 168, 171, 174, 179, 185,
            189, 192, 195, 198,
        };

        [Test]
        public void IsSide_FirstOfTheSegment_ThenOneInThree_NeverOnRetry()
        {
            Assert.That(IntroSideSchedule.SideEveryN, Is.EqualTo(3));
            Assert.That(IntroSideSchedule.IsSide(0, false), Is.True, "el primero de 3 filas del tramo");
            Assert.That(IntroSideSchedule.IsSide(1, false), Is.False);
            Assert.That(IntroSideSchedule.IsSide(2, false), Is.False);
            Assert.That(IntroSideSchedule.IsSide(3, false), Is.True);
            Assert.That(IntroSideSchedule.IsSide(6, false), Is.True);
            Assert.That(IntroSideSchedule.IsSide(7, false), Is.False);
            Assert.That(IntroSideSchedule.IsSide(-1, false), Is.False, "sin 3 filas, nunca");
            for (int ordinal = -1; ordinal < 12; ordinal++)
            {
                Assert.That(IntroSideSchedule.IsSide(ordinal, true), Is.False, "un reintento va en dosel (ordinal " + ordinal + ")");
            }
        }

        [Test]
        public void SegmentStart_IsTheWorld_OrALaterSpeciesInsideIt()
        {
            Assert.That(IntroSideSchedule.SegmentStart(86, 81, 81), Is.EqualTo(81), "churuco: todo el mundo 4");
            Assert.That(IntroSideSchedule.SegmentStart(143, 141, 141), Is.EqualTo(141));
            Assert.That(IntroSideSchedule.SegmentStart(500, 421, 481), Is.EqualTo(481), "una especie nueva a mitad de mundo (elenco de D-010)");
            Assert.That(IntroSideSchedule.SegmentStart(450, 421, 481), Is.EqualTo(421), "antes de su tramo, el de la anterior");
            Assert.That(IntroSideSchedule.SegmentStart(130, 81, 70), Is.EqualTo(81), "una especie que viene de otro mundo no lo parte");
        }

        [Test]
        public void RealPacks_86To200_FirstOfEachWorldAndOneInThree()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            FaunaCatalog fauna = Fauna();
            Assert.That(catalog.FirstLevel, Is.EqualTo(1));
            Assert.That(catalog.LastLevel, Is.EqualTo(200));

            var side = new List<int>();
            var canopy = new List<int>();
            var firsts = new List<int>();
            int threeRows101 = 0;
            int firstThreeRows = -1;
            for (int level = 1; level <= 200; level++)
            {
                int ordinal = GameScreen.ThreeRowOrdinal(catalog, fauna, level, Width, BoardTop, BoardBottom780);
                bool three = GameScreen.HasThreeRows(catalog.Find(level)!, Width, BoardTop, BoardBottom780);
                Assert.That(ordinal >= 0, Is.EqualTo(three), "nivel " + level + ": ordinal solo con 3 filas");
                if (!three)
                {
                    continue;
                }

                firstThreeRows = firstThreeRows < 0 ? level : firstThreeRows;
                threeRows101 += level >= 101 ? 1 : 0;
                firsts.AddRange(ordinal == 0 ? new[] { level } : new int[0]);
                (IntroSideSchedule.IsSide(ordinal, false) ? side : canopy).Add(level);
                Assert.That(IntroSideSchedule.IsSide(ordinal, true), Is.False, "nivel " + level + ": en un reintento, dosel");
            }

            Assert.That(firstThreeRows, Is.EqualTo(86), "3 filas desde el 86 (D-021)");
            Assert.That(threeRows101, Is.EqualTo(77), "77 de los 100 niveles del 101-200 (D-021)");
            Assert.That(firsts, Is.EqualTo(new[] { 86, 143 }), "primeros de 3 filas de los mundos 4 (churuco) y 5 (huapo colorado)");
            Assert.That(side.Count + canopy.Count, Is.EqualTo(82));
            Assert.That(side, Is.EqualTo(ExpectedSide), "28 con la tropa lateral");
            Assert.That(canopy.Count, Is.EqualTo(54), "54 en dosel");
            foreach (int first in firsts)
            {
                Assert.That(side, Has.Member(first), "el primero de cada mundo, siempre con la tropa lateral");
            }

            // 1 de cada 3 dentro de cada mundo: entre dos laterales seguidos del mismo mundo hay exactamente 2 de 3 filas en dosel.
            for (int i = 1; i < side.Count; i++)
            {
                if (WorldTable.WorldOf(side[i]) != WorldTable.WorldOf(side[i - 1]))
                {
                    continue;
                }

                int between = 0;
                foreach (int level in canopy)
                {
                    between += level > side[i - 1] && level < side[i] ? 1 : 0;
                }

                Assert.That(between, Is.EqualTo(2), "entre el " + side[i - 1] + " y el " + side[i]);
            }
        }

        [Test]
        public void RealPacks_SameScheduleAt360x800_AndRunToRunAndInAnyOrder()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            FaunaCatalog fauna = Fauna();
            var forward = new List<int>();
            var tall = new List<int>();
            for (int level = 86; level <= 200; level++)
            {
                if (IntroSideSchedule.IsSide(GameScreen.ThreeRowOrdinal(catalog, fauna, level, Width, BoardTop, BoardBottom780), false))
                {
                    forward.Add(level);
                }

                if (IntroSideSchedule.IsSide(GameScreen.ThreeRowOrdinal(catalog, fauna, level, Width, BoardTop, BoardBottom800), false))
                {
                    tall.Add(level);
                }
            }

            var backward = new List<int>();
            for (int level = 200; level >= 86; level--)
            {
                if (IntroSideSchedule.IsSide(GameScreen.ThreeRowOrdinal(catalog, fauna, level, Width, BoardTop, BoardBottom780), false))
                {
                    backward.Insert(0, level);
                }
            }

            Assert.That(forward, Is.EqualTo(ExpectedSide));
            Assert.That(backward, Is.EqualTo(forward), "función del número de nivel: no depende del orden en que se juega");
            Assert.That(tall, Is.EqualTo(forward), "720 × 1600 px (360 × 800 dp): los mismos niveles");
        }

        [Test]
        public void ThreeRowOrdinal_IsMinusOne_OutsideTheCatalogOrWithoutThreeRows()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            FaunaCatalog fauna = Fauna();
            Assert.That(GameScreen.ThreeRowOrdinal(catalog, fauna, 7, Width, BoardTop, BoardBottom780), Is.EqualTo(-1), "nivel 7: 2 filas");
            Assert.That(GameScreen.ThreeRowOrdinal(catalog, fauna, 201, Width, BoardTop, BoardBottom780), Is.EqualTo(-1), "fuera del catálogo");
            Assert.That(GameScreen.ThreeRowOrdinal(catalog, null, 86, Width, BoardTop, BoardBottom780), Is.EqualTo(0), "sin catálogo de fauna, por mundo");
            Assert.That(GameScreen.ThreeRowOrdinal(catalog, fauna, 95, Width, BoardTop, BoardBottom780), Is.EqualTo(3));
        }

        private static FaunaCatalog Fauna()
        {
            return FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
        }
    }
}
