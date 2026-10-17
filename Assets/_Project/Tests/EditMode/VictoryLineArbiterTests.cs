#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.GameFlow;
using AF.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El árbitro de la única línea de la victoria (<see cref="VictoryLineArbiter"/>; arquitectura de la tanda 4, §2; 02 §6; T6-A;
    /// pregunta 45, (a), D-022; tarea M1.3a): tabla de casos con todas las combinaciones (Canon o SingleKey × ArrivalEverywhere
    /// o SlothHito × TiTino puede hablar o no × con o sin Mapa) en 10, 11, 20, 21, 40, 41, 60, 61, 80, 81, 140, 200, 201 y 700,
    /// con las paradas reales de 1-420 (42, M4.5a) y con un catálogo sintético de 100 paradas; una sola línea por victoria; el dato
    /// desplazado sale una vez y solo una; las cadencias; los defaults y cero asignaciones.
    /// </summary>
    public sealed class VictoryLineArbiterTests
    {
        private const string Fruit = "fact.acai.01";

        private static readonly int[] CaseLevels = { 10, 11, 20, 21, 40, 41, 60, 61, 80, 81, 140, 200, 201, 700 };

        private static int s_sink;

        // ------------------------------------------------------------------------------------------- tabla de casos

        /// <summary>Qué espera la tabla en cada nivel de la prueba, según el catálogo.</summary>
        private enum Kind
        {
            /// <summary>El 10: la ranura vacía con el Mapa; sin él, la línea de fruta.</summary>
            L10,

            /// <summary>Victoria sin parada ni hito: la línea de fruta.</summary>
            Plain,

            /// <summary>10k con parada que no es hito.</summary>
            Arrival,

            /// <summary>Hito de TiTino con parada.</summary>
            HitoWithStop,

            /// <summary>Hito de TiTino sin parada en el catálogo.</summary>
            HitoNoStop,

            /// <summary>10k+1 tras un hito con parada (puede llevar el dato desplazado).</summary>
            AfterHito,
        }

        private static Kind KindOf(int level, bool full)
        {
            switch (level)
            {
                case 10: return Kind.L10;
                case 11: case 21: return Kind.Plain;
                case 20: return Kind.Arrival;
                case 40: case 60: case 80: case 140: return Kind.HitoWithStop;
                case 41: case 61: case 81: return Kind.AfterHito;
                case 200: return Kind.HitoWithStop;
                case 700: return full ? Kind.HitoWithStop : Kind.HitoNoStop;
                case 201: return Kind.AfterHito;
                default: throw new ArgumentOutOfRangeException(nameof(level), level, "nivel fuera de la tabla");
            }
        }

        [Test]
        public void Table_AllCombinations_RealAndFullCatalogs()
        {
            int checkedCases = 0;
            foreach (bool full in new[] { false, true })
            {
                RiverStopCatalog stops = full ? MapTestCatalogs.Full : MapTestCatalogs.Real;
                foreach (RiverFactPolicy river in new[] { RiverFactPolicy.Canon, RiverFactPolicy.SingleKey })
                {
                    foreach (HitoLinePolicy hito in new[] { HitoLinePolicy.ArrivalEverywhere, HitoLinePolicy.SlothHito })
                    {
                        foreach (bool speak in new[] { false, true })
                        {
                            foreach (bool map in new[] { false, true })
                            {
                                foreach (int level in CaseLevels)
                                {
                                    var ctx = new VictoryContext(level, map, stops, Fruit, river, hito, speak, 10, 1);
                                    string name = (full ? "100 paradas" : "1-420") + ", " + river + ", " + hito + ", habla " + speak
                                        + ", Mapa " + map + ", nivel " + level;
                                    Expect(KindOf(level, full), level, stops, river, hito == HitoLinePolicy.SlothHito && speak, map,
                                        VictoryLineArbiter.For(in ctx), name);
                                    checkedCases++;
                                }
                            }
                        }
                    }
                }
            }

            Assert.That(checkedCases, Is.EqualTo(2 * 2 * 2 * 2 * 2 * CaseLevels.Length));
        }

        // La regla de cada fila de la tabla, escrita por casos (arquitectura §2).
        private static void Expect(Kind kind, int level, RiverStopCatalog stops, RiverFactPolicy river, bool speaks, bool map,
            VictoryLine line, string name)
        {
            bool canon = river == RiverFactPolicy.Canon;
            switch (kind)
            {
                case Kind.L10:
                    Assert.That(line.Arrival, Is.False, name + ": en el 10 la llegada la cuenta el Mapa");
                    Assert.That(line.ArrivalFactKey, Is.Null, name);
                    Assert.That(line.FruitKey, Is.EqualTo(map ? null : Fruit), name + ": con Mapa, la ranura vacía");
                    Assert.That(line.SlothSpeaks, Is.False, name);
                    Assert.That(line.SlothBig, Is.False, name + ": el 10 no es hito");
                    Assert.That(line.DeferredFact, Is.False, name);
                    break;
                case Kind.Plain:
                    Assert.That(line.Arrival, Is.False, name);
                    Assert.That(line.ArrivalFactKey, Is.Null, name);
                    Assert.That(line.DeferredFact, Is.False, name);
                    Assert.That(line.FruitKey, Is.EqualTo(Fruit), name);
                    Assert.That(line.SlothSpeaks, Is.False, name);
                    Assert.That(line.SlothBig, Is.False, name);
                    break;
                case Kind.Arrival:
                    Assert.That(line.Arrival, Is.True, name);
                    Assert.That(line.ArrivalFact, Is.EqualTo(canon), name);
                    Assert.That(line.ArrivalFactKey, Is.EqualTo(canon ? stops.ArrivalOnWin(level)!.ArrivalFact : null), name);
                    Assert.That(line.FruitKey, Is.EqualTo(canon ? null : Fruit), name + ": Canon, fruta a la ficha; SingleKey, fruta");
                    Assert.That(line.DeferredFact, Is.False, name);
                    Assert.That(line.SlothSpeaks, Is.False, name);
                    Assert.That(line.SlothBig, Is.False, name);
                    break;
                case Kind.HitoWithStop:
                    Assert.That(line.Arrival, Is.True, name + ": la placa del lugar sale siempre");
                    Assert.That(line.SlothBig, Is.True, name);
                    Assert.That(line.SlothSpeaks, Is.EqualTo(speaks), name);
                    Assert.That(line.DeferredFact, Is.False, name);
                    if (speaks)
                    {
                        Assert.That(line.ArrivalFact, Is.False, name + ": habla TiTino y el dato pasa al 10k+1");
                        Assert.That(line.ArrivalFactKey, Is.Null, name);
                        Assert.That(line.FruitKey, Is.Null, name + ": una sola línea");
                    }
                    else
                    {
                        Assert.That(line.ArrivalFact, Is.EqualTo(canon), name + ": victoria normal");
                        Assert.That(line.ArrivalFactKey, Is.EqualTo(canon ? stops.ArrivalOnWin(level)!.ArrivalFact : null), name);
                        Assert.That(line.FruitKey, Is.EqualTo(canon ? null : Fruit), name);
                    }

                    break;
                case Kind.HitoNoStop:
                    Assert.That(line.Arrival, Is.False, name);
                    Assert.That(line.ArrivalFactKey, Is.Null, name);
                    Assert.That(line.DeferredFact, Is.False, name);
                    Assert.That(line.SlothBig, Is.True, name);
                    Assert.That(line.SlothSpeaks, Is.EqualTo(speaks), name);
                    Assert.That(line.FruitKey, Is.EqualTo(speaks ? null : Fruit), name);
                    break;
                case Kind.AfterHito:
                    bool deferred = speaks && canon;
                    Assert.That(line.Arrival, Is.False, name);
                    Assert.That(line.ArrivalFact, Is.False, name + ": no es una llegada");
                    Assert.That(line.DeferredFact, Is.EqualTo(deferred), name);
                    Assert.That(line.ArrivalFactKey, Is.EqualTo(deferred ? stops.ArrivalOnWin(level - 1)!.ArrivalFact : null), name);
                    Assert.That(line.FruitKey, Is.EqualTo(deferred ? null : Fruit), name + ": el dato desplazado ocupa la línea de fruta");
                    Assert.That(line.SlothSpeaks, Is.False, name);
                    Assert.That(line.SlothBig, Is.False, name);
                    break;
                default:
                    Assert.Fail("caso sin regla: " + kind);
                    break;
            }
        }

        [Test]
        public void Table_ConcreteCases_FromTheRealCatalog()
        {
            RiverStopCatalog real = MapTestCatalogs.Real;

            // 20: Alter do Chão, con su dato y sin fruta (Canon).
            VictoryLine l20 = For(20, true, real, RiverFactPolicy.Canon, HitoLinePolicy.ArrivalEverywhere, false);
            Assert.That(l20.Arrival && l20.ArrivalFact, Is.True);
            Assert.That(l20.ArrivalFactKey, Is.EqualTo("map.stop.alter_do_chao.dato_01"));
            Assert.That(l20.FruitKey, Is.Null);

            // 40 con SlothHito y TiTino que puede hablar: habla, placa sin dato; el 41 lleva el dato del Encontro.
            VictoryLine l40 = For(40, true, real, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, true);
            Assert.That(l40.SlothSpeaks && l40.SlothBig && l40.Arrival, Is.True);
            Assert.That(l40.ArrivalFact, Is.False);
            Assert.That(l40.ArrivalFactKey, Is.Null);
            Assert.That(l40.FruitKey, Is.Null);
            VictoryLine l41 = For(41, true, real, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, true);
            Assert.That(l41.DeferredFact, Is.True);
            Assert.That(l41.ArrivalFactKey, Is.EqualTo("map.stop.manaos.dato_01"));
            Assert.That(l41.FruitKey, Is.Null);

            // 40 con SlothHito pero sin sitio para TiTino: victoria normal, y el 41 con su fruta.
            VictoryLine l40Mute = For(40, true, real, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, false);
            Assert.That(l40Mute.SlothSpeaks, Is.False);
            Assert.That(l40Mute.ArrivalFactKey, Is.EqualTo("map.stop.manaos.dato_01"));
            VictoryLine l41Mute = For(41, true, real, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, false);
            Assert.That(l41Mute.DeferredFact, Is.False);
            Assert.That(l41Mute.FruitKey, Is.EqualTo(Fruit));

            // 80 (fin del mundo 3) con los defaults (SlothHito desde TI6a, T6-A): si TiTino puede hablar, habla él y la
            // bienvenida a Colombia va sin dato, que pasa al 81; si no puede, la llegada lleva su dato en el canon de hoy.
            VictoryLine l80 = For(80, true, real, MapConfig.RiverPolicy, MapConfig.HitoPolicy, true);
            Assert.That(l80.SlothSpeaks, Is.True, "SlothHito por defecto desde TI6a");
            Assert.That(l80.SlothBig, Is.True);
            Assert.That(l80.Arrival, Is.True);
            Assert.That(l80.ArrivalFact, Is.False);
            Assert.That(l80.ArrivalFactKey, Is.Null);
            VictoryLine l81 = For(81, true, real, MapConfig.RiverPolicy, MapConfig.HitoPolicy, true);
            Assert.That(l81.DeferredFact, Is.True);
            Assert.That(l81.ArrivalFactKey, Is.EqualTo("map.country.co.dato_01"));
            VictoryLine l80Mute = For(80, true, real, MapConfig.RiverPolicy, MapConfig.HitoPolicy, false);
            Assert.That(l80Mute.ArrivalFactKey, Is.EqualTo("map.country.co.dato_01"), "TiTino no puede hablar: la llegada con su dato");
            Assert.That(l80Mute.SlothSpeaks, Is.False);
            Assert.That(l80Mute.SlothBig, Is.True);
            VictoryLine l80Before = For(80, true, real, MapConfig.RiverPolicy, HitoLinePolicy.ArrivalEverywhere, true);
            Assert.That(l80Before.SlothSpeaks, Is.False, "ArrivalEverywhere, la de antes de TI6a");
            Assert.That(l80Before.ArrivalFactKey, Is.EqualTo("map.country.co.dato_01"));

            // Sin catálogo no hay llegadas ni datos desplazados.
            VictoryLine noStops = For(41, true, null, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, true);
            Assert.That(noStops.DeferredFact, Is.False);
            Assert.That(noStops.FruitKey, Is.EqualTo(Fruit));
            Assert.That(For(20, true, null, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, true).Arrival, Is.False);
        }

        private static VictoryLine For(int won, bool map, RiverStopCatalog? stops, RiverFactPolicy river, HitoLinePolicy hito, bool speak)
        {
            var ctx = new VictoryContext(won, map, stops, Fruit, river, hito, speak, 10, 1);
            return VictoryLineArbiter.For(in ctx);
        }

        // ------------------------------------------------------------------------------------------- invariantes

        [Test]
        public void OneLinePerVictory_AndTheArrivalAgreesWithMapMoments()
        {
            foreach (RiverStopCatalog stops in new[] { MapTestCatalogs.Real, MapTestCatalogs.Full })
            {
                foreach (RiverFactPolicy river in new[] { RiverFactPolicy.Canon, RiverFactPolicy.SingleKey })
                {
                    foreach (HitoLinePolicy hito in new[] { HitoLinePolicy.ArrivalEverywhere, HitoLinePolicy.SlothHito })
                    {
                        foreach (bool speak in new[] { false, true })
                        {
                            foreach (bool map in new[] { false, true })
                            {
                                for (int won = 1; won <= WorldTable.LastLevel; won++)
                                {
                                    VictoryLine line = For(won, map, stops, river, hito, speak);
                                    int texts = (line.SlothSpeaks ? 1 : 0) + (line.ArrivalFactKey != null ? 1 : 0)
                                        + (line.FruitKey != null ? 1 : 0);
                                    Assert.That(texts, Is.LessThanOrEqualTo(1), "una sola línea en el " + won);
                                    bool arrival = MapMoments.ForVictory(won, stops, map, MapConfig.UiMapAutopen) == VictoryMoment.Arrival;
                                    Assert.That(line.Arrival, Is.EqualTo(arrival), "llegada como MapMoments en el " + won);
                                    if (line.ArrivalFact)
                                    {
                                        Assert.That(MapMoments.ArrivalShowsFact(won, hito) || !speak, Is.True, "nivel " + won);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void DeferredFact_ShowsOnceAndOnlyOnce_EveryArrivalFactAppears()
        {
            foreach (RiverStopCatalog stops in new[] { MapTestCatalogs.Real, MapTestCatalogs.Full })
            {
                foreach (HitoLinePolicy hito in new[] { HitoLinePolicy.ArrivalEverywhere, HitoLinePolicy.SlothHito })
                {
                    foreach (bool speak in new[] { false, true })
                    {
                        var shown = new List<string>();
                        for (int won = 1; won <= WorldTable.LastLevel; won++)
                        {
                            VictoryLine line = For(won, true, stops, RiverFactPolicy.Canon, hito, speak);
                            if (line.ArrivalFactKey != null)
                            {
                                shown.Add(line.ArrivalFactKey);
                            }
                        }

                        var expected = new List<string>();
                        for (int k = 2; k < stops.Count; k++)
                        {
                            string? fact = stops.At(k)!.ArrivalFact;
                            if (fact != null)
                            {
                                expected.Add(fact);
                            }
                        }

                        Assert.That(shown, Is.EqualTo(expected), hito + ", habla " + speak + ": cada dato de llegada sale una vez, en orden");
                    }
                }
            }
        }

        [Test]
        public void SingleKey_NeverShowsAMapFact()
        {
            for (int won = 1; won <= WorldTable.LastLevel; won++)
            {
                VictoryLine line = For(won, true, MapTestCatalogs.Full, RiverFactPolicy.SingleKey, HitoLinePolicy.SlothHito, true);
                Assert.That(line.ArrivalFactKey, Is.Null, "nivel " + won);
                Assert.That(line.DeferredFact, Is.False, "nivel " + won);
            }
        }

        [Test]
        public void IsSlothHito_EndsOfWorldButTen_Plus40And60()
        {
            var hitos = new List<int>();
            for (int won = 1; won <= WorldTable.LastLevel; won++)
            {
                if (VictoryLineArbiter.IsSlothHito(won))
                {
                    hitos.Add(won);
                }
            }

            Assert.That(hitos, Is.EqualTo(new[] { 40, 60, 80, 140, 200, 300, 420, 560, 700, 820, 920, 960, 1000 }));
            Assert.That(VictoryLineArbiter.IsSlothHito(10), Is.False, "el 10 lo cuenta el Mapa");
        }

        // ------------------------------------------------------------------------------------------- cadencias

        [Test]
        public void FruitCadence_ZeroOneAndTwo()
        {
            RiverStopCatalog real = MapTestCatalogs.Real;
            foreach (int won in new[] { 1, 2, 3, 11, 12, 21 })
            {
                Assert.That(Cadence(won, real, 10, 1).FruitKey, Is.EqualTo(Fruit), "cada victoria, nivel " + won);
                Assert.That(Cadence(won, real, 10, 0).FruitKey, Is.Null, "apagada, nivel " + won);
                Assert.That(Cadence(won, real, 10, 2).FruitKey, Is.EqualTo(won % 2 == 0 ? Fruit : null), "una cada dos, nivel " + won);
                Assert.That(Cadence(won, real, 10, -1).FruitKey, Is.Null, "un valor negativo la apaga");
            }

            var noFruit = new VictoryContext(21, true, real, null, RiverFactPolicy.Canon, HitoLinePolicy.ArrivalEverywhere, false, 10, 1);
            Assert.That(VictoryLineArbiter.For(in noFruit).FruitKey, Is.Null, "nivel sin línea de fruta");
        }

        [Test]
        public void RiverFactCadence_TwentyAndZero()
        {
            RiverStopCatalog real = MapTestCatalogs.Real;
            VictoryLine l30 = Cadence(30, real, 20, 1);
            Assert.That(l30.Arrival, Is.True);
            Assert.That(l30.ArrivalFact, Is.False, "con 20, el 30 llega sin dato");
            Assert.That(l30.FruitKey, Is.EqualTo(Fruit), "y la línea es la de fruta");
            Assert.That(Cadence(40, real, 20, 1).ArrivalFact, Is.True);
            Assert.That(Cadence(40, real, 0, 1).ArrivalFact, Is.False, "0 lo apaga");
            Assert.That(Cadence(40, real, 0, 1).FruitKey, Is.EqualTo(Fruit));

            // El hito sin dato por cadencia no desplaza nada al 10k+1.
            var c41 = new VictoryContext(41, true, real, Fruit, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, true, 0, 1);
            Assert.That(VictoryLineArbiter.For(in c41).DeferredFact, Is.False);
        }

        private static VictoryLine Cadence(int won, RiverStopCatalog stops, int riverEveryN, int fruitEveryN)
        {
            var ctx = new VictoryContext(won, true, stops, Fruit, RiverFactPolicy.Canon, HitoLinePolicy.ArrivalEverywhere, false,
                riverEveryN, fruitEveryN);
            return VictoryLineArbiter.For(in ctx);
        }

        // ------------------------------------------------------------------------------------------- defaults y bordes

        [Test]
        public void ContextFor_UsesTheClientDefaults()
        {
            VictoryContext ctx = VictoryLineArbiter.ContextFor(57, true, MapTestCatalogs.Real, Fruit, true);
            Assert.That(ctx.Won, Is.EqualTo(57));
            Assert.That(ctx.MapAvailable, Is.True);
            Assert.That(ctx.Stops, Is.SameAs(MapTestCatalogs.Real));
            Assert.That(ctx.FruitKey, Is.EqualTo(Fruit));
            Assert.That(ctx.SlothCanSpeak, Is.True);
            Assert.That(ctx.River, Is.EqualTo(RiverFactPolicy.Canon));
            Assert.That(ctx.Hito, Is.EqualTo(MapConfig.HitoPolicy));
            Assert.That(ctx.RiverFactEveryN, Is.EqualTo(10), "ui_river_fact_every_n_levels");
            Assert.That(ctx.FruitEveryN, Is.EqualTo(1), "ui_fruit_line_every_n_levels [P]");
        }

        [Test]
        public void For_OutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => For(0, true, MapTestCatalogs.Real, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => For(1001, true, MapTestCatalogs.Real, RiverFactPolicy.Canon, HitoLinePolicy.SlothHito, true));
        }

        // ------------------------------------------------------------------------------------------- asignaciones

        [Test]
        public void For_AllLevelsAndPolicies_DoesNotAllocate()
        {
            RiverStopCatalog real = MapTestCatalogs.Real;
            RiverStopCatalog full = MapTestCatalogs.Full;
            TestDelegate call = () =>
            {
                int acc = 0;
                for (int policy = 0; policy < 16; policy++)
                {
                    RiverStopCatalog stops = (policy & 8) != 0 ? full : real;
                    RiverFactPolicy river = (policy & 1) != 0 ? RiverFactPolicy.SingleKey : RiverFactPolicy.Canon;
                    HitoLinePolicy hito = (policy & 2) != 0 ? HitoLinePolicy.SlothHito : HitoLinePolicy.ArrivalEverywhere;
                    bool speak = (policy & 4) != 0;
                    for (int won = 1; won <= WorldTable.LastLevel; won++)
                    {
                        var ctx = new VictoryContext(won, (won & 1) == 0, stops, Fruit, river, hito, speak, 10, 1);
                        VictoryLine line = VictoryLineArbiter.For(in ctx);
                        acc += (line.Arrival ? 1 : 0) + (line.ArrivalFactKey != null ? 1 : 0) + (line.FruitKey != null ? 1 : 0)
                            + (line.SlothSpeaks ? 1 : 0) + (line.DeferredFact ? 1 : 0) + (line.SlothBig ? 1 : 0);
                        acc += VictoryLineArbiter.IsSlothHito(won) ? 1 : 0;
                    }

                    VictoryContext defaults = VictoryLineArbiter.ContextFor(40, true, stops, Fruit, speak);
                    acc += VictoryLineArbiter.For(in defaults).Arrival ? 1 : 0;
                }

                s_sink = acc;
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(s_sink, Is.GreaterThan(0));
        }
    }

    /// <summary>
    /// Catálogos de paradas de las pruebas del Mapa (M1.3a): el real de 1-420 (<c>Data/Map/river_stops_v1.json</c>; M4.5a) y
    /// uno sintético de 100 paradas (1-1.000) con un dato de llegada en cada una desde la 1, para los niveles de después del
    /// 420.
    /// </summary>
    internal static class MapTestCatalogs
    {
        private static RiverStopCatalog? s_real;
        private static RiverStopCatalog? s_full;

        /// <summary>Las paradas reales: 42, de 1-420 (M4.5a).</summary>
        public static RiverStopCatalog Real =>
            s_real ??= RiverStopCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Map/river_stops_v1.json")));

        /// <summary>100 paradas sintéticas, de acuerdo con <see cref="WorldTable"/>.</summary>
        public static RiverStopCatalog Full => s_full ??= BuildFull();

        private static RiverStopCatalog BuildFull()
        {
            var stops = new RiverStop[100];
            for (int n = 0; n < stops.Length; n++)
            {
                int first = (10 * n) + 1;
                int world = WorldTable.WorldOf(first);
                StopKind kind = WorldTable.FirstLevel(world) == first ? StopKind.Gate : StopKind.Stop;
                string id = "s" + n.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                stops[n] = new RiverStop(n, world, id, kind, "BR", null, n == 0 ? (int?)null : n * 10, n * 10, "white", null,
                    n == 0 ? null : "map.stop." + id + ".dato_01", Array.Empty<string>(), null, false,
                    new RiverStopBackdrop("bg_kit_w" + world.ToString("00", System.Globalization.CultureInfo.InvariantCulture), null, null, null));
            }

            return new RiverStopCatalog("tests", stops);
        }
    }
}
