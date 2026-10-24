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

        // 360 × 640 dp (16:9): 480.
        private const float BoardBottom640 = 480f;

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
            Assert.That(catalog.LastLevel, Is.EqualTo(700), "la escena llega al 700 desde C1b; la medida de D-021 es la de 86-200");

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

        /// <summary>
        /// D-046, punto 3: en todos los niveles de 3 filas de los paquetes 1-700, a 360 × 780 y a 360 × 800 dp, el tablero deja la
        /// franja de la rama (≥ 44 dp entre el HUD y el panel) con la cuia ≥ 28 dp, con sus capacidades reales (Cuia alta) y la
        /// pareja de la Tinaja en su fila; así la tropa va en su rama en el primer intento y la tropa lateral (D-021) solo queda
        /// de respaldo. En 16:9 (360 × 640) no cabe en ninguno y vuelve el comportamiento de antes.
        /// </summary>
        [Test]
        public void RealPacks_EveryThreeRowLevel_LeavesTheTroopBand_At360x780And800_AndNoneIn16x9()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            int three780 = 0;
            int three640 = 0;
            int tall = 0;
            int tinaja = 0;
            float narrowest = float.MaxValue;
            for (int level = catalog.FirstLevel; level <= catalog.LastLevel; level++)
            {
                LevelDefinition? def = catalog.Find(level);
                if (def == null)
                {
                    continue;
                }

                float heightU = 0f;
                for (int i = 0; i < def.CuiaCount; i++)
                {
                    heightU = Mathf.Max(heightU, CuiaGeometry.HeightU(def.CapacityOf(i)));
                }

                int pair = BoardLayout.TinajaPairStart(def.CreateBoard());
                foreach (float bottom in new[] { BoardBottom780, BoardBottom800 })
                {
                    BoardLayoutResult l = BoardLayout.Compute(def.CuiaCount, def.CuiaCount, heightU, Width, BoardTop, bottom, pair);
                    Assert.That(l.Rows == 3, Is.EqualTo(GameScreen.HasThreeRows(def, Width, BoardTop, bottom)), "nivel " + level);
                    if (l.Rows != 3)
                    {
                        Assert.That(l.TroopBand, Is.False, "nivel " + level + ": con 1 o 2 filas, nada cambia");
                        continue;
                    }

                    string at = "nivel " + level + " (" + def.CuiaCount + " cuias, " + heightU + " u), boardBottom " + bottom;
                    Assert.That(l.TroopBand, Is.True, at + ": la franja de la tropa");
                    Assert.That(l.PanelY - BoardTop, Is.GreaterThanOrEqualTo(IntroChoreography.BranchMinBandDp), at);
                    Assert.That(l.CuiaWidth, Is.GreaterThanOrEqualTo(BoardLayout.MinCuiaWidth), at);
                    narrowest = Mathf.Min(narrowest, l.CuiaWidth);
                    if (bottom == BoardBottom780)
                    {
                        three780++;
                        tall += heightU > CuiaGeometry.HeightU(4) + 0.001f ? 1 : 0;
                        tinaja += pair >= 0 ? 1 : 0;
                    }
                }

                BoardLayoutResult short16x9 = BoardLayout.Compute(def.CuiaCount, def.CuiaCount, heightU, Width, BoardTop, BoardBottom640, pair);
                if (short16x9.Rows == 3)
                {
                    three640++;
                    Assert.That(short16x9.TroopBand, Is.False, "nivel " + level + " en 16:9: sin franja, como antes");
                }
            }

            Debug.Log("AF_TROOP_BAND niveles de 3 filas a 360 × 780: " + three780 + " (con Cuia alta: " + tall + "; con Tinaja: " + tinaja
                + "), todos con la franja; cuia más estrecha " + narrowest.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " dp; en 16:9, " + three640 + " de 3 filas, ninguno con la franja");
            Assert.That(three780, Is.GreaterThan(82), "más que los 82 del 86-200");
            Assert.That(three640, Is.GreaterThan(0));
        }

        [Test]
        public void ThreeRowOrdinal_IsMinusOne_OutsideTheCatalogOrWithoutThreeRows()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            FaunaCatalog fauna = Fauna();
            Assert.That(GameScreen.ThreeRowOrdinal(catalog, fauna, 7, Width, BoardTop, BoardBottom780), Is.EqualTo(-1), "nivel 7: 2 filas");
            Assert.That(GameScreen.ThreeRowOrdinal(catalog, fauna, 701, Width, BoardTop, BoardBottom780), Is.EqualTo(-1), "fuera del catálogo (acaba en el 700)");
            Assert.That(GameScreen.ThreeRowOrdinal(catalog, null, 86, Width, BoardTop, BoardBottom780), Is.EqualTo(0), "sin catálogo de fauna, por mundo");
            Assert.That(GameScreen.ThreeRowOrdinal(catalog, fauna, 95, Width, BoardTop, BoardBottom780), Is.EqualTo(3));
        }

        // ---- Regla de D-021, punto 4, en el planificador puro (revisión de TL1): si la tropa lateral no cabe sin bajar de
        // MinMonkeyDp, no se encoge y se aplica la regla de D-010 (rama si la franja mide ≥ 44 dp; si no, dosel). ----

        [Test]
        public void SideOnThreeRowsOfFive_DoesNotFit_FallsBackToTheD010Rule_WithoutShrinking(
            [Values(13, 14, 15, 16)] int count,
            [Values(TroopModel.Row, TroopModel.Shake, TroopModel.Hang)] TroopModel model,
            [Values(800f, 1400f)] float totalMs)
        {
            IntroInput input = IntroChoreographyTests.Input(count, 780f, totalMs, IntroPerch.Side);
            input.Model = model;
            string at = count + " cuias, " + model + ", T " + totalMs;
            Assert.That(input.Rows, Is.EqualTo(3), at + ": 3 filas (D-006)");

            var side = new IntroChoreography();
            side.Plan(input);
            input.Perch = IntroPerch.Branch;
            var branch = new IntroChoreography();
            branch.Plan(input);

            Assert.That(side.SideFitDp, Is.LessThan(IntroChoreography.MinMonkeyDp), at + ": con 5 o 6 cuias por fila no cabe (D-021)");
            Assert.That(side.Perch, Is.Not.EqualTo(IntroPerch.Side), at + ": sin tropa lateral");
            float band = input.PanelTop - input.HudBottom;
            if (band < IntroChoreography.BranchMinBandDp)
            {
                Assert.That(side.Perch, Is.EqualTo(IntroPerch.Canopy), at + ": franja de " + band + " dp (< 44): dosel");
            }

            AssertSamePlan(side, branch, at);
            if (side.MonkeyCount > 0)
            {
                Assert.That(side.MonkeyDp, Is.GreaterThanOrEqualTo(IntroChoreography.MinMonkeyDp), at + ": nunca se encoge por debajo de 28 dp");
            }
            else
            {
                Assert.That(side.MonkeyDp, Is.EqualTo(0f), at + ": sin tropa");
            }
        }

        [Test]
        public void SideOnTwoRows_GoesByTheD010Rule([Values(7, 10)] int count, [Values(TroopModel.Row, TroopModel.Hang)] TroopModel model)
        {
            IntroInput input = IntroChoreographyTests.Input(count, 780f, 800f, IntroPerch.Side);
            input.Model = model;
            string at = count + " cuias, " + model;
            Assert.That(input.Rows, Is.EqualTo(2), at);

            var side = new IntroChoreography();
            side.Plan(input);
            input.Perch = IntroPerch.Branch;
            var branch = new IntroChoreography();
            branch.Plan(input);

            Assert.That(side.Perch, Is.Not.EqualTo(IntroPerch.Side), at + ": solo con 3 filas");
            AssertSamePlan(side, branch, at);
        }

        [Test]
        public void SideWithoutAnyFruitColumn_GoesByTheD010Rule_AndFitsWithFourPerRow()
        {
            // Control: 12 cuias (4 + 4 + 4) caben con un miquito por lado de ≥ 28 dp.
            IntroInput input = IntroChoreographyTests.Input(12, 780f, 800f, IntroPerch.Side);
            Assert.That(input.Rows, Is.EqualTo(3));
            var fits = new IntroChoreography();
            fits.Plan(input);
            Assert.That(fits.Perch, Is.EqualTo(IntroPerch.Side), "4 cuias por fila: cabe");
            Assert.That(fits.SideFitDp, Is.GreaterThanOrEqualTo(IntroChoreography.MinMonkeyDp));
            Assert.That(fits.MonkeyCount, Is.EqualTo(IntroChoreography.SideMonkeys));
            Assert.That(fits.BodyDp, Is.GreaterThanOrEqualTo(IntroChoreography.MinMonkeyDp));

            // Las mismas 12, todas vacías: ninguna columna con fruta, así que no hay tropa lateral.
            for (int i = 0; i < input.Count; i++)
            {
                input.SetCuia(i, input.CenterX[i], input.FootY[i], input.Row[i], System.Array.Empty<int>());
            }

            var side = new IntroChoreography();
            side.Plan(input);
            input.Perch = IntroPerch.Branch;
            var branch = new IntroChoreography();
            branch.Plan(input);
            Assert.That(side.ColumnCount, Is.EqualTo(0));
            Assert.That(side.Perch, Is.Not.EqualTo(IntroPerch.Side), "sin columnas con fruta");
            AssertSamePlan(side, branch, "12 vacías");
        }

        // Pedir la tropa lateral cuando no cabe da lo mismo que pedir la rama: posadero, modelo, tropa, tamaño y calendario.
        private static void AssertSamePlan(IntroChoreography side, IntroChoreography branch, string at)
        {
            Assert.That(side.Perch, Is.EqualTo(branch.Perch), at + ": el posadero de la regla de D-010");
            Assert.That(side.Model, Is.EqualTo(branch.Model), at);
            Assert.That(side.MonkeyCount, Is.EqualTo(branch.MonkeyCount), at);
            Assert.That(side.MonkeyDp, Is.EqualTo(branch.MonkeyDp), at);
            Assert.That(side.BodyDp, Is.EqualTo(branch.BodyDp), at);
            Assert.That(side.TotalMs, Is.EqualTo(branch.TotalMs), at);
            Assert.That(side.FruitCount, Is.EqualTo(branch.FruitCount), at);
            for (int k = 0; k < side.FruitCount; k++)
            {
                Assert.That(side.FruitMonkey[k], Is.EqualTo(branch.FruitMonkey[k]), at + ", fruta " + k);
                Assert.That(side.FruitReleaseMs[k], Is.EqualTo(branch.FruitReleaseMs[k]), at + ", fruta " + k);
                Assert.That(side.FruitReleaseX[k], Is.EqualTo(branch.FruitReleaseX[k]), at + ", fruta " + k);
                Assert.That(side.FruitImpactMs[k], Is.EqualTo(branch.FruitImpactMs[k]), at + ", fruta " + k);
            }
        }

        private static FaunaCatalog Fauna()
        {
            return FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
        }
    }
}
