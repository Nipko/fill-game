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
    /// Qué toca del Mapa del río en cada momento (<see cref="MapMoments"/>; 08 §2.2; D-013, puntos 3, 7 y 8; 02 §6; 06 §2.3;
    /// tarea M1.3a): la victoria con y sin Mapa (el 10 lo cuenta el Mapa; 20 llega; 21 nada; 200 llega a Mazán y 420 nada con
    /// las 42 paradas de 1-420, M4.5a), el dato de la llegada con las dos políticas de los hitos (sin fijar ninguna como
    /// canon), el viaje de mundo dentro del banner según <c>ui_map_autopen</c> y sin asignaciones. También las claves de 08 §9
    /// de <see cref="MapConfig"/>.
    /// </summary>
    public sealed class MapMomentsTests
    {
        private static int s_sink;

        [Test]
        public void ForVictory_L10_DependsOnTheMap()
        {
            RiverStopCatalog real = MapTestCatalogs.Real;
            Assert.That(MapMoments.ForVictory(10, real, false, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.None),
                "sin Mapa, nada (02 §6)");
            Assert.That(MapMoments.ForVictory(10, real, true, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.L10Map));
            Assert.That(MapMoments.ForVictory(10, real, true, MapConfig.AutopenL10), Is.EqualTo(VictoryMoment.L10Map),
                "los dos valores de ui_map_autopen abren el Mapa en el 10");
            Assert.That(MapMoments.ForVictory(10, null, true, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.L10Map));
        }

        [Test]
        public void ForVictory_ArrivalOnTenKWithAStop_NoneElsewhere()
        {
            RiverStopCatalog real = MapTestCatalogs.Real;
            foreach (bool map in new[] { false, true })
            {
                Assert.That(MapMoments.ForVictory(20, real, map, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.Arrival));
                Assert.That(MapMoments.ForVictory(21, real, map, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.None));
                Assert.That(MapMoments.ForVictory(1, real, map, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.None));
                Assert.That(MapMoments.ForVictory(11, real, map, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.None));
                Assert.That(MapMoments.ForVictory(190, real, map, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.Arrival));
                Assert.That(MapMoments.ForVictory(200, real, map, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.Arrival),
                    "el 200 lleva a Mazán, la parada 20 (M4.5a)");
                Assert.That(MapMoments.ForVictory(420, real, map, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.None),
                    "con las 42 paradas de 1-420 no hay parada 42");
                Assert.That(MapMoments.ForVictory(20, null, map, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.None),
                    "sin catálogo no hay llegadas");
            }

            RiverStopCatalog full = MapTestCatalogs.Full;
            Assert.That(MapMoments.ForVictory(200, full, true, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.Arrival));
            Assert.That(MapMoments.ForVictory(700, full, true, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.Arrival));
            Assert.That(MapMoments.ForVictory(1000, full, true, MapConfig.UiMapAutopen), Is.EqualTo(VictoryMoment.None),
                "no hay parada 100");
            int arrivals = 0;
            for (int level = 1; level <= WorldTable.LastLevel; level++)
            {
                if (MapMoments.ForVictory(level, real, true, MapConfig.UiMapAutopen) == VictoryMoment.Arrival)
                {
                    arrivals++;
                    Assert.That(RiverJourney.IsArrival(level), Is.True);
                }
            }

            Assert.That(arrivals, Is.EqualTo(40), "20, 30 … 410: del 10 se encarga el Mapa y el 420 no tiene parada (M4.5a)");
        }

        [Test]
        public void ForVictory_OutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MapMoments.ForVictory(0, MapTestCatalogs.Real, true, MapConfig.UiMapAutopen));
            Assert.Throws<ArgumentOutOfRangeException>(() => MapMoments.ForVictory(1001, MapTestCatalogs.Real, true, MapConfig.UiMapAutopen));
        }

        [Test]
        public void ArrivalShowsFact_BothPolicies()
        {
            foreach (int hito in new[] { 40, 60, 80, 140, 200 })
            {
                Assert.That(MapMoments.ArrivalShowsFact(hito, HitoLinePolicy.ArrivalEverywhere), Is.True, "nivel " + hito);
                Assert.That(MapMoments.ArrivalShowsFact(hito, HitoLinePolicy.SlothHito), Is.False, "nivel " + hito);
            }

            foreach (int level in new[] { 10, 20, 30, 50, 70, 90, 130, 150, 190, 41, 61 })
            {
                Assert.That(MapMoments.ArrivalShowsFact(level, HitoLinePolicy.ArrivalEverywhere), Is.True, "nivel " + level);
                Assert.That(MapMoments.ArrivalShowsFact(level, HitoLinePolicy.SlothHito), Is.True, "nivel " + level);
            }
        }

        [Test]
        public void TravelInBanner_OnlyFromThe41_WithL10World()
        {
            Assert.That(MapMoments.TravelInBanner(11, MapConfig.AutopenL10World), Is.False, "el primer viaje lo cuenta el Mapa del 10");
            foreach (int level in new[] { 41, 81, 141, 201, 301 })
            {
                Assert.That(MapMoments.TravelInBanner(level, MapConfig.AutopenL10World), Is.True, "nivel " + level);
                Assert.That(MapMoments.TravelInBanner(level, MapConfig.AutopenL10), Is.False, "nivel " + level + " con l10");
                Assert.That(MapMoments.TravelInBanner(level, "otro"), Is.False, "un valor desconocido no trae el viaje");
            }

            foreach (int level in new[] { 1, 10, 40, 42, 80, 140, 0, 1001 })
            {
                Assert.That(MapMoments.TravelInBanner(level, MapConfig.AutopenL10World), Is.False, "sin banner en el " + level);
            }

            Assert.That(MapMoments.TravelInBanner(41, "L10_WORLD"), Is.False, "comparación ordinal");
        }

        [Test]
        public void MapConfig_KeysWithThe08Defaults()
        {
            Assert.That(MapConfig.UiMapMode, Is.EqualTo("stops"));
            Assert.That(MapConfig.UiMapAutopen, Is.EqualTo("l10_world"));
            Assert.That(MapConfig.AutopenL10, Is.EqualTo("l10"));
            Assert.That(MapConfig.MapModeWorlds, Is.EqualTo("worlds"));
            Assert.That(MapConfig.UiBgPerStop, Is.True);
            Assert.That(MapConfig.UiStopLabel, Is.True);
            Assert.That(MapConfig.UiVictoryView, Is.True);
            Assert.That(MapConfig.AnimMapWorldMs, Is.EqualTo(6000));
            Assert.That(MapConfig.UiMapQuiz, Is.EqualTo("off"));
            Assert.That(MapConfig.UiRiverFactEveryNLevels, Is.EqualTo(10));
            Assert.That(MapConfig.RiverPolicy, Is.EqualTo(RiverFactPolicy.Canon), "pregunta 45, (a): el canon de hoy");
            Assert.That(MapConfig.HitoPolicy, Is.EqualTo(HitoLinePolicy.SlothHito), "T6-A, desde TI6a");
            Assert.That(MapConfig.ArrivalStripMs, Is.EqualTo(1200));
            Assert.That(MapConfig.WorldTravelInBannerMaxMs, Is.LessThanOrEqualTo(4000));
        }

        [Test]
        public void AllFunctions_DoNotAllocate()
        {
            RiverStopCatalog real = MapTestCatalogs.Real;
            TestDelegate call = () =>
            {
                int acc = 0;
                for (int level = 1; level <= WorldTable.LastLevel; level++)
                {
                    acc += (int)MapMoments.ForVictory(level, real, true, MapConfig.UiMapAutopen);
                    acc += (int)MapMoments.ForVictory(level, real, false, MapConfig.UiMapAutopen);
                    acc += MapMoments.ArrivalShowsFact(level, HitoLinePolicy.SlothHito) ? 1 : 0;
                    acc += MapMoments.ArrivalShowsFact(level, HitoLinePolicy.ArrivalEverywhere) ? 1 : 0;
                    acc += MapMoments.TravelInBanner(level, MapConfig.UiMapAutopen) ? 1 : 0;
                    acc += MapMoments.TravelInBanner(level, MapConfig.AutopenL10) ? 1 : 0;
                }

                s_sink = acc;
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(s_sink, Is.GreaterThan(0));
        }
    }
}
