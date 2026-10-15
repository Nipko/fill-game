#nullable enable
using System;
using AF.GameFlow;
using AF.Levels;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El viaje del Mapa del río en funciones puras (<see cref="RiverJourney"/>; 08 §2.2; D-013; tarea M1.3a): la parada de cada
    /// nivel, las llegadas, «Faltan N», el llenado del tramo, los cambios de mundo y el guía (Q18-A), en los bordes 1, 10, 11,
    /// 20, 21, 40, 41, 140, 141, 200, 201, 700, 701 y 1.000, fuera de rango y sin asignaciones.
    /// </summary>
    public sealed class RiverJourneyTests
    {
        private static int s_sink;

        // Nivel, parada, llegada, «Faltan N» jugando ese nivel, cambio de mundo y llenado con ese max_level.
        private static readonly object[] Borders =
        {
            new object[] { 1, 0, false, 10, false, 0.1f },
            new object[] { 10, 0, true, 1, true, 1f },
            new object[] { 11, 1, false, 10, false, 0.1f },
            new object[] { 20, 1, true, 1, false, 1f },
            new object[] { 21, 2, false, 10, false, 0.1f },
            new object[] { 40, 3, true, 1, true, 1f },
            new object[] { 41, 4, false, 10, false, 0.1f },
            new object[] { 140, 13, true, 1, true, 1f },
            new object[] { 141, 14, false, 10, false, 0.1f },
            new object[] { 200, 19, true, 1, true, 1f },
            new object[] { 201, 20, false, 10, false, 0.1f },
            new object[] { 700, 69, true, 1, true, 1f },
            new object[] { 701, 70, false, 10, false, 0.1f },
            new object[] { 1000, 99, true, 1, false, 1f },
        };

        [TestCaseSource(nameof(Borders))]
        public void Borders_StopArrivalLeftWorldChangeAndFill(int level, int stop, bool arrival, int left, bool worldChange, float fill)
        {
            Assert.That(RiverJourney.StopIndexOf(level), Is.EqualTo(stop), "parada (L − 1) / 10");
            Assert.That(RiverJourney.IsArrival(level), Is.EqualTo(arrival), "llegada en los 10k desde el 10");
            Assert.That(RiverJourney.LevelsLeft(level), Is.EqualTo(left), "Faltan N");
            Assert.That(RiverJourney.IsWorldChange(level), Is.EqualTo(worldChange), "cambio de mundo");
            Assert.That(RiverJourney.LegFill(level), Is.EqualTo(fill).Within(1e-6f), "llenado del tramo");
        }

        [Test]
        public void StopIndexOf_CoversTenLevelsPerStop_AndMatchesTheCatalogRule()
        {
            for (int level = 1; level <= WorldTable.LastLevel; level++)
            {
                int k = RiverJourney.StopIndexOf(level);
                Assert.That(level, Is.InRange((10 * k) + 1, (10 * k) + 10), "nivel " + level);
                Assert.That(RiverJourney.LevelsLeft(level), Is.InRange(1, 10), "nivel " + level);
                Assert.That(RiverJourney.LevelsLeft(level) + level - 1, Is.EqualTo((10 * k) + 10), "llega al ganar el 10k+10");
            }
        }

        [Test]
        public void LevelsLeft_AndLegFill_FromThePlan()
        {
            Assert.That(RiverJourney.LevelsLeft(58), Is.EqualTo(3), "58, 59 y 60");
            Assert.That(RiverJourney.LevelsLeft(51), Is.EqualTo(10));
            Assert.That(RiverJourney.LevelsLeft(60), Is.EqualTo(1));
            Assert.That(RiverJourney.LegFill(57), Is.EqualTo(0.7f).Within(1e-6f));
            Assert.That(RiverJourney.LegFill(0), Is.EqualTo(0f), "sin niveles ganados, el tramo vacío");
            Assert.That(RiverJourney.LegFill(9), Is.EqualTo(0.9f).Within(1e-6f));
            Assert.That(RiverJourney.LegFill(10), Is.EqualTo(1f), "los múltiplos de 10 desde el 10 dan 1");
            Assert.That(RiverJourney.LegFill(60), Is.EqualTo(1f));
            Assert.That(RiverJourney.LegFill(61), Is.EqualTo(0.1f).Within(1e-6f));
        }

        [Test]
        public void IsWorldChange_TrueOnlyBeforeTheFirstLevelOfAWorld()
        {
            foreach (int level in new[] { 10, 40, 80, 140, 200, 300, 420, 560, 700, 820, 920, 960 })
            {
                Assert.That(RiverJourney.IsWorldChange(level), Is.True, "nivel " + level);
            }

            Assert.That(RiverJourney.IsWorldChange(20), Is.False);
            Assert.That(RiverJourney.IsWorldChange(1000), Is.False, "no hay nivel siguiente en la primera vuelta");
            int count = 0;
            for (int level = 1; level <= WorldTable.LastLevel; level++)
            {
                if (RiverJourney.IsWorldChange(level))
                {
                    count++;
                    Assert.That(WorldBanners.ShowsOn(level + 1), Is.True, "el nivel siguiente lleva el banner de su mundo");
                }
            }

            Assert.That(count, Is.EqualTo(WorldTable.WorldCount - 1));
        }

        [Test]
        public void IsArrival_EveryTenFromTen()
        {
            int count = 0;
            for (int level = 1; level <= WorldTable.LastLevel; level++)
            {
                if (RiverJourney.IsArrival(level))
                {
                    count++;
                    Assert.That(level % 10, Is.EqualTo(0));
                }
            }

            Assert.That(count, Is.EqualTo(100));
        }

        [Test]
        public void GuideFor_BufeoInWorlds7And8_RosaElsewhere()
        {
            for (int world = 1; world <= WorldTable.WorldCount; world++)
            {
                string expected = world == 7 || world == 8 ? RiverJourney.GuideBufeo : RiverJourney.GuideRosa;
                Assert.That(RiverJourney.GuideFor(world), Is.EqualTo(expected), "mundo " + world);
            }

            Assert.That(RiverJourney.GuideBufeo, Is.EqualTo("bufeo"));
            Assert.That(RiverJourney.GuideRosa, Is.EqualTo("rosa"));
        }

        [Test]
        public void OutOfRange_Throws_LikeWorldTable()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.StopIndexOf(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.StopIndexOf(1001));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.IsArrival(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.IsArrival(1001));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.LevelsLeft(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.LevelsLeft(1001));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.IsWorldChange(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.IsWorldChange(1001));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.LegFill(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.LegFill(1001));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.GuideFor(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RiverJourney.GuideFor(14));
        }

        [Test]
        public void AllFunctions_DoNotAllocate()
        {
            TestDelegate call = () =>
            {
                int acc = 0;
                for (int level = 1; level <= WorldTable.LastLevel; level++)
                {
                    acc += RiverJourney.StopIndexOf(level) + RiverJourney.LevelsLeft(level);
                    acc += RiverJourney.IsArrival(level) ? 1 : 0;
                    acc += RiverJourney.IsWorldChange(level) ? 1 : 0;
                    acc += (int)(RiverJourney.LegFill(level) * 10f);
                }

                for (int world = 1; world <= WorldTable.WorldCount; world++)
                {
                    acc += RiverJourney.GuideFor(world).Length;
                }

                s_sink = acc;
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(s_sink, Is.GreaterThan(0));
        }
    }
}
