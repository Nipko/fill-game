#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Geometría del Mapa del río <c>af-river-map/1</c> en el cliente (D-013; 08 §2.2 y §4.5; contrato 29; codificación de P-M1;
    /// tarea M3.2): el lector acepta el archivo real y un documento mínimo, decodifica el base64 con sus deltas como
    /// <c>tools/river-map/geometry.py</c> (ida y vuelta, y los primeros puntos del archivo real), calcula la misma s, rechaza
    /// cada caso inválido con la ruta del valor, lee las rutas de las paradas (<c>at</c> y <c>leg</c>) contra el Mapa, y las
    /// búsquedas no asignan memoria.
    /// </summary>
    public sealed class RiverMapReaderTests
    {
        private static object? s_sink;
        private static int s_count;

        private static string RealMapPath => Path.Combine(Application.dataPath, "_Project/Data/Map/river_map_v1.json");

        private static string RealStopsPath => Path.Combine(Application.dataPath, "_Project/Data/Map/river_stops_v1.json");

        // ---------------------------------------------------------------------------------------------- archivo real

        [Test]
        public void RealFile_Reads_WithItsLinesLandAndExtras()
        {
            RiverMap map = RiverMapReader.Read(File.ReadAllText(RealMapPath));
            Assert.That(map.Frame.Aspect, Is.EqualTo(1.31498).Within(1e-5), "aspecto = 33° · cos(−5°) / 25° (propuesta §3.1)");
            Assert.That(map.Lines.Count, Is.EqualTo(31), "27 de 1-200 y 4 ríos locales de 201-420 (M4.5a)");

            RiverLine amazonas = map.Lines[0];
            Assert.That(amazonas.Id, Is.EqualTo("amazonas"));
            Assert.That(amazonas.Kind, Is.EqualTo(RiverLineKind.Main));
            Assert.That(amazonas.Class, Is.EqualTo(1));
            Assert.That(amazonas.PointCount, Is.EqualTo(105));
            Assert.That(amazonas.Mouth, Is.Null);
            Assert.That(amazonas.Water.Count, Is.EqualTo(2));
            Assert.That(amazonas.Water[0].Water, Is.EqualTo(RiverWater.Andean));
            Assert.That(amazonas.Water[1].Water, Is.EqualTo(RiverWater.White));
            Assert.That(amazonas.Splits.Count, Is.EqualTo(2));
            Assert.That(amazonas.Splits[0].Id, Is.EqualTo("encontro"));
            Assert.That(amazonas.Splits[0].Left, Is.EqualTo(RiverWater.Black));
            Assert.That(amazonas.Splits[0].Right, Is.EqualTo(RiverWater.White));

            RiverLine tapajos = map.Line("tapajos")!;
            Assert.That(tapajos.Kind, Is.EqualTo(RiverLineKind.Tributary));
            Assert.That(tapajos.Class, Is.EqualTo(2));
            Assert.That(tapajos.Mouth!.Value.Line, Is.EqualTo(0), "el Tapajós desemboca en el cauce");
            Assert.That(tapajos.Mouth!.Value.S, Is.EqualTo(0.855387f));
            Assert.That(tapajos.FillOnPass, Is.EqualTo(0.1455f));
            Assert.That(tapajos.WaterAt(0.5f), Is.EqualTo(RiverWater.Clear));

            Assert.That(map.Line("atacuari")!.PointCount, Is.EqualTo(1), "de algunos ríos locales solo se conoce la boca");
            Assert.That(map.Line("atacuari")!.Length, Is.EqualTo(0f));
            Assert.That(map.Line("pacaya")!.Water.Count, Is.EqualTo(0), "agua sin comprobar: solo vidrio");
            Assert.That(map.Line("pacaya")!.WaterAt(0.5f), Is.EqualTo(RiverWater.None));

            Assert.That(map.Land.VertexCount, Is.EqualTo(40));
            Assert.That(map.Land.TriangleCount, Is.EqualTo(38));
            Assert.That(map.Land.Zones.Count, Is.EqualTo(1));
            Assert.That(map.Land.Zones[0].Tile, Is.EqualTo("canopy"));
            Assert.That(map.Insets.Count, Is.EqualTo(0), "la 1.0 deja los recuadros vacíos");
            Assert.That(map.Lines[map.Home.Line].Id, Is.EqualTo("para"), "la casa de TiTino, en la bahía de Guajará");
            Assert.That(map.Home.S, Is.EqualTo(0.727595f));
            Assert.That(map.Labels.Count, Is.EqualTo(9));
            Assert.That(map.Labels[0].Country, Is.EqualTo("BR"));
            Assert.That(map.Labels[0].NameKey, Is.EqualTo("map.country.br.name"));
            Assert.That(map.Viewpoints.Count, Is.EqualTo(3));
            Assert.That(map.Viewpoints[0].Id, Is.EqualTo("boca_del_amazonas"));
            Assert.That(map.Viewpoints[0].RevealOn, Is.EqualTo(10));
            Assert.That(map.Viewpoints[0].Fact, Is.EqualTo("map.stop.estuario.dato_01"));
            Assert.That(map.Passing.Count, Is.EqualTo(18), "17 de 1-200 y teotonio (M4.5a)");
            Assert.That(map.Passing[17].Id, Is.EqualTo("teotonio"));
            Assert.That(map.Lines[map.Passing[17].At.Line].Id, Is.EqualTo("madeira"));
            Assert.That(map.Passing[17].Fact, Is.EqualTo("map.stop.teotonio.dato_01"));
            Assert.That(map.Lines[map.Line("cuyabeno")!.Mouth!.Value.Line].Id, Is.EqualTo("aguarico"));
            Assert.That(map.Line("cuyabeno")!.WaterAt(0.5f), Is.EqualTo(RiverWater.Black));
            Assert.That(map.Passing[0].Id, Is.EqualTo("breves"));
            Assert.That(map.Lines[map.Passing[0].At.Line].Id, Is.EqualTo("para"));
            Assert.That(map.Passing[0].Fact, Is.Null);
            Assert.That(map.Passing[0].NameKey, Is.EqualTo("map.stop.breves.name"));
        }

        [Test]
        public void RealFile_EveryLine_IsDecodedOnceIntoFrameUnits_WithTheToolsS()
        {
            string json = File.ReadAllText(RealMapPath);
            RiverMap map = RiverMapReader.Read(json);
            double aspect = map.Frame.Aspect;
            for (int k = 0; k < map.Lines.Count; k++)
            {
                RiverLine line = map.Lines[k];
                Assert.That(line.Index, Is.EqualTo(k));
                Assert.That(line.Xy.Length, Is.EqualTo(2 * line.PointCount), line.Id);
                Assert.That(line.Cumulative.Length, Is.EqualTo(line.PointCount), line.Id);
                Assert.That(line.Cumulative[0], Is.EqualTo(0f), line.Id);
                Assert.That(line.Kind == RiverLineKind.Main, Is.EqualTo(line.Mouth == null), line.Id + ": boca si y solo si no es main");

                // La s de la herramienta: longitud en (X / 65535, Y / 65535 · aspecto) desde el primer punto, en doble precisión.
                ushort[] raw = RawPoints(json, line.Id);
                double total = 0.0;
                var cumulative = new double[raw.Length / 2];
                for (int i = 1; i < cumulative.Length; i++)
                {
                    double dx = (raw[2 * i] - raw[(2 * i) - 2]) / 65535.0;
                    double dy = (raw[(2 * i) + 1] - raw[(2 * i) - 1]) / 65535.0 * aspect;
                    total += Math.Sqrt((dx * dx) + (dy * dy));
                    cumulative[i] = total;
                }

                Assert.That(line.Length, Is.EqualTo((float)total).Within(1e-6f), line.Id);
                for (int i = 0; i < line.PointCount; i++)
                {
                    Assert.That(line.X(i), Is.EqualTo((float)(raw[2 * i] / 65535.0)).Within(1e-7f), line.Id + " x" + i);
                    Assert.That(line.Y(i), Is.EqualTo((float)(raw[(2 * i) + 1] / 65535.0 * aspect)).Within(1e-6f), line.Id + " y" + i);
                    if (total > 0.0)
                    {
                        Assert.That(line.SOfPoint(i), Is.EqualTo((float)(cumulative[i] / total)).Within(1e-6f), line.Id + " s" + i);
                    }
                }
            }
        }

        [Test]
        public void RealFile_FirstPoints_MatchGeometryPy()
        {
            // Valores de geometry.decode_points sobre el archivo real (amazonas y land.verts).
            string json = File.ReadAllText(RealMapPath);
            ushort[] amazonas = RawPoints(json, "amazonas");
            Assert.That(amazonas[0], Is.EqualTo(6522));
            Assert.That(amazonas[1], Is.EqualTo(16479));
            Assert.That(amazonas[2], Is.EqualTo(7400));
            Assert.That(amazonas[3], Is.EqualTo(16611));
            Assert.That(amazonas[4], Is.EqualTo(7924));
            Assert.That(amazonas[5], Is.EqualTo(16662));

            RiverMap map = RiverMapReader.Read(json);
            Assert.That(map.Land.Xy[0], Is.EqualTo(1f));
            Assert.That(map.Land.Xy[1], Is.EqualTo((float)(42459 / 65535.0 * map.Frame.Aspect)).Within(1e-6f));
            Assert.That(map.Land.Xy[2], Is.EqualTo((float)(65014 / 65535.0)).Within(1e-7f));
            Assert.That(map.Land.Xy[3], Is.EqualTo((float)(43382 / 65535.0 * map.Frame.Aspect)).Within(1e-6f));
            for (int i = 0; i < map.Land.Triangles.Length; i++)
            {
                Assert.That(map.Land.Triangles[i], Is.InRange(0, map.Land.VertexCount - 1));
            }
        }

        [Test]
        public void RealStops_Routes_FollowTheMap()
        {
            RiverMap map = RiverMapReader.Read(File.ReadAllText(RealMapPath));
            RiverRoutes routes = RiverMapReader.ReadRoutes(File.ReadAllText(RealStopsPath), map);
            Assert.That(routes.Count, Is.EqualTo(42), "1-420 (M4.5a)");

            RiverStopRoute belem = routes.At(0)!;
            Assert.That(belem.Id, Is.EqualTo("belem"));
            Assert.That(map.Lines[belem.At.Line].Id, Is.EqualTo("para"));
            Assert.That(belem.Path.Count, Is.EqualTo(1));
            Assert.That(belem.Path[0].S0, Is.EqualTo(belem.Path[0].S1), "n = 0: un tramo sin largo sobre la propia parada");

            RiverStopRoute santarem = routes.At(1)!;
            Assert.That(santarem.Id, Is.EqualTo("santarem"));
            Assert.That(santarem.Path.Count, Is.EqualTo(2));
            Assert.That(map.Lines[santarem.Path[0].Line].Id, Is.EqualTo("para"));
            Assert.That(santarem.Path[0].S1, Is.EqualTo(0f), "el ramal hasta Gurupá");
            Assert.That(map.Lines[santarem.Path[1].Line].Id, Is.EqualTo("amazonas"));
            Assert.That(santarem.Path[1].S1, Is.LessThan(santarem.Path[1].S0), "río arriba, s decrece");
            Assert.That(santarem.Via, Is.EqualTo(new[] { "breves", "gurupa", "boca_del_xingu", "monte_alegre" }));
            Assert.That(santarem.Mouths.Count, Is.EqualTo(2));
            Assert.That(map.Lines[santarem.Mouths[0]].Id, Is.EqualTo("xingu"));
            Assert.That(map.Lines[santarem.Mouths[1]].Id, Is.EqualTo("tapajos"));

            // Alter do Chão sale de Santarém por la boca del Tapajós: el mismo punto del agua en dos líneas.
            RiverStopRoute alter = routes.At(2)!;
            Assert.That(map.Lines[alter.Path[0].Line].Id, Is.EqualTo("tapajos"));
            Assert.That(alter.Path[0].S0, Is.EqualTo(1f));
            Assert.That(map.Lines[santarem.At.Line].Id, Is.EqualTo("amazonas"));

            for (int n = 0; n < routes.Count; n++)
            {
                RiverStopRoute route = routes.At(n)!;
                RiverMapPlace from = n == 0 ? route.At : routes.At(n - 1)!.At;
                Assert.That(GapDp(map, route.Path[0].Line, route.Path[0].S0, from.Line, from.S), Is.LessThan(0.25f),
                    route.Id + ": sale de la parada anterior");
                for (int k = 1; k < route.Path.Count; k++)
                {
                    Assert.That(GapDp(map, route.Path[k].Line, route.Path[k].S0, route.Path[k - 1].Line, route.Path[k - 1].S1),
                        Is.LessThan(0.25f), route.Id + ": tramo " + k + " seguido (la boca del Negro queda a 0,2 dp de su s)");
                }

                RiverPathPiece last = route.Path[route.Path.Count - 1];
                Assert.That(last.Line, Is.EqualTo(route.At.Line), route.Id);
                Assert.That(last.S1, Is.EqualTo(route.At.S), route.Id + ": llega a la suya");
            }

            RiverStopRoute heath = routes.At(30)!;
            Assert.That(heath.Id, Is.EqualTo("puerto_heath"));
            Assert.That(heath.Via[heath.Via.Count - 1], Is.EqualTo("teotonio"), "el relevo del guía en el viaje del 300 (Q18-A)");
            Assert.That(routes.At(42), Is.Null);
        }

        // ---------------------------------------------------------------------------------------------- base64 (P-M1)

        [Test]
        public void Base64_RoundTrip_PointsWithDeltasModulo65536()
        {
            // Puntos con saltos en los dos sentidos y por el borde (deltas negativos, que van módulo 65.536).
            var points = new ushort[]
            {
                65535, 0, 0, 65535, 1, 1, 32768, 12345, 32767, 54321, 0, 0, 65535, 65535, 40000, 2,
            };
            ushort[] decoded = RiverMapReader.DecodePoints(EncodePoints(points), "pts");
            Assert.That(decoded, Is.EqualTo(points));

            // Una pseudoaleatoria determinista larga (generador congruencial propio).
            uint state = 12345u;
            var many = new ushort[2 * 500];
            for (int i = 0; i < many.Length; i++)
            {
                state = (state * 1664525u) + 1013904223u;
                many[i] = (ushort)(state >> 16);
            }

            Assert.That(RiverMapReader.DecodePoints(EncodePoints(many), "pts"), Is.EqualTo(many));
            Assert.That(RiverMapReader.DecodePoints(EncodePoints(new ushort[] { 7, 9 }), "pts"), Is.EqualTo(new ushort[] { 7, 9 }), "un solo punto");
        }

        [Test]
        public void Base64_RoundTrip_AbsoluteIndices()
        {
            var indices = new ushort[] { 0, 1, 2, 65535, 3, 39, 39, 0, 1 };
            Assert.That(RiverMapReader.DecodeUInt16(EncodeIndices(indices), "tris"), Is.EqualTo(indices));
        }

        // ---------------------------------------------------------------------------------------------- documento mínimo

        [Test]
        public void MinimalDoc_Reads()
        {
            RiverMap map = RiverMapReader.Read(Doc());
            Assert.That(map.Lines.Count, Is.EqualTo(2));
            RiverLine main = map.Lines[0];
            RiverLine negro = map.Lines[1];
            Assert.That(negro.Mouth!.Value.Line, Is.EqualTo(0));
            Assert.That(negro.Mouth!.Value.S, Is.EqualTo(0.3f));
            Assert.That(negro.FillOnPass, Is.EqualTo(0.1f));
            Assert.That(main.WaterAt(0f), Is.EqualTo(RiverWater.Andean));
            Assert.That(main.WaterAt(0.39f), Is.EqualTo(RiverWater.Andean));
            Assert.That(main.WaterAt(0.4f), Is.EqualTo(RiverWater.White), "tramos semiabiertos [s0, s1)");
            Assert.That(main.WaterAt(1f), Is.EqualTo(RiverWater.White));
            Assert.That(main.TryGetSplitAt(0.55f, out RiverSplit split), Is.True);
            Assert.That(split.Left, Is.EqualTo(RiverWater.Black));
            Assert.That(main.TryGetSplitAt(0.6f, out _), Is.False);
            Assert.That(main.NextBoundaryAfter(0f), Is.EqualTo(0.4f));
            Assert.That(main.NextBoundaryAfter(0.4f), Is.EqualTo(0.5f));
            Assert.That(main.NextBoundaryAfter(0.5f), Is.EqualTo(0.6f));
            Assert.That(main.NextBoundaryAfter(0.6f), Is.EqualTo(2f), "sin más cambios");
            Assert.That(map.LineIndexOf("negro"), Is.EqualTo(1));
            Assert.That(map.LineIndexOf("nadie"), Is.EqualTo(-1));
            Assert.That(map.PassingPoint("gurupa")!.At.S, Is.EqualTo(0.8f));
            Assert.That(map.Labels[0].X, Is.EqualTo((float)(100 / 65535.0)).Within(1e-7f));
        }

        [Test]
        public void MinimalDoc_ZonesInAnyOrder_CoverAllTriangles()
        {
            string land = Land(zones: "[{\"id\": \"b\", \"tile\": \"rock\", \"first\": 1, \"count\": 1}, "
                + "{\"id\": \"a\", \"tile\": \"canopy\", \"first\": 0, \"count\": 1}]");
            RiverMap map = RiverMapReader.Read(Doc(land: land));
            Assert.That(map.Land.Zones.Count, Is.EqualTo(2));
            Assert.That(map.Land.Zones[0].Tile, Is.EqualTo("rock"));
        }

        [Test]
        public void MinimalDoc_FactKeys_AcceptTheFourForms()
        {
            // Las cuatro formas de la clave de dato del Mapa (esquema, $defs/fact_key; glosario §10): el archivo real solo trae
            // map.stop.*.dato_NN, así que las de país, mito y general se prueban aquí.
            string viewpoints = "[" + Viewpoint(fact: "\"map.country.br.dato_01\"") + ", "
                + "{\"id\": \"mirador_boto\", \"xy\": [20, 20], \"reveal_on\": 20, \"fact\": \"map.myth.boto\"}]";
            string passing = "[{\"id\": \"gurupa\", \"line\": \"amazonas\", \"s\": 0.8, \"fact\": \"map.general.cuenca\"}, "
                + "{\"id\": \"breves\", \"line\": \"amazonas\", \"s\": 0.7, \"fact\": \"map.stop.breves.dato_02\"}]";
            RiverMap map = RiverMapReader.Read(Doc(viewpoints: viewpoints, passing: passing));
            Assert.That(map.Viewpoints[0].Fact, Is.EqualTo("map.country.br.dato_01"));
            Assert.That(map.Viewpoints[1].Fact, Is.EqualTo("map.myth.boto"));
            Assert.That(map.Passing[0].Fact, Is.EqualTo("map.general.cuenca"));
            Assert.That(map.Passing[1].Fact, Is.EqualTo("map.stop.breves.dato_02"));
        }

        // ---------------------------------------------------------------------------------------------- casos inválidos

        private static IEnumerable<TestCaseData> InvalidCases()
        {
            yield return new TestCaseData("{\"format\": ", "El Mapa del río no es JSON válido:")
                .SetName("Reader_RejectsInvalid_WithThePath(no es JSON)");
            yield return Case("format ajeno", Doc(format: "af-river-stops/1"), "format");
            yield return Case("giro distinto", Doc(frame: Frame(rotation: "ccw90")), "frame.rotation");
            yield return Case("lat al reves", Doc(frame: Frame(lat: "[7.0, -18.0]")), "frame.lat");
            yield return Case("lat0 en el polo", Doc(frame: Frame(lat0: "90.0")), "frame.lat0");
            yield return Case("sin lineas", Doc(lines: "[]"), "lines");
            yield return Case("id repetido", Doc(lines: Lines(Main(), Trib(id: "amazonas"))), "lines[1].id");
            yield return Case("belen en un id", Doc(lines: Lines(Main(), Trib(id: "belen_viejo"))), "lines[1].id");
            yield return Case("id con salto de linea", Doc(lines: Lines(Main(), Trib(id: "negro\\n"))), "lines[1].id");
            yield return Case("kind desconocido", Doc(lines: Lines(Main(), Trib(kind: "canal"))), "lines[1].kind");
            yield return Case("class fuera de 1-3", Doc(lines: Lines(Main(cls: "4"), Trib())), "lines[0].class");
            yield return Case("src desconocido", Doc(lines: Lines(Main(src: "osm"), Trib())), "lines[0].src");
            yield return Case("pts no es base64", Doc(lines: Lines(Main(pts: "ab!d"), Trib())), "lines[0].pts");
            yield return Case("pts con espacios", Doc(lines: Lines(Main(pts: "AAAA AAA"), Trib())), "lines[0].pts");
            yield return Case("pts sin pares", Doc(lines: Lines(Main(pts: EncodeIndices(new ushort[] { 1, 2, 3 })), Trib())), "lines[0].pts");
            yield return Case("water pasa de 1", Doc(lines: Lines(Main(water: Water("0.0", "0.4", "andean") + ", " + Water("0.4", "1.2", "white")),
                Trib())), "lines[0].water[1].s1");
            yield return Case("water con hueco", Doc(lines: Lines(Main(water: Water("0.0", "0.4", "andean") + ", " + Water("0.5", "1.0", "white")),
                Trib())), "lines[0].water[1].s0");
            yield return Case("water sin llegar a 1", Doc(lines: Lines(Main(water: Water("0.0", "0.4", "andean") + ", " + Water("0.4", "0.9", "white")),
                Trib())), "lines[0].water[1].s1");
            yield return Case("water de tipo desconocido", Doc(lines: Lines(Main(water: Water("0.0", "1.0", "turbia")), Trib())),
                "lines[0].water[0].type");
            yield return Case("split al reves", Doc(lines: Lines(Main(splits: Split("0.6", "0.5")), Trib())), "lines[0].splits[0]");
            yield return Case("splits solapados", Doc(lines: Lines(Main(splits: Split("0.5", "0.6") + ", " + Split("0.55", "0.7", "otro")), Trib())),
                "lines[0].splits[1].s0");
            yield return Case("split en un afluente", Doc(lines: Lines(Main(), Trib(extra: ", \"splits\": [" + Split("0.1", "0.2") + "]"))),
                "lines[1].splits");
            yield return Case("main con boca", Doc(lines: Lines(Main(extra: ", \"mouth\": {\"on\": \"negro\", \"s\": 0.5}"), Trib())),
                "lines[0].mouth");
            yield return Case("boca en una linea desconocida", Doc(lines: Lines(Main(), Trib(mouth: "{\"on\": \"nadie\", \"s\": 0.3}"))),
                "lines[1].mouth.on");
            yield return Case("boca en si misma", Doc(lines: Lines(Main(), Trib(mouth: "{\"on\": \"negro\", \"s\": 0.3}"))), "lines[1].mouth.on");
            yield return Case("boca fuera de [0, 1]", Doc(lines: Lines(Main(), Trib(mouth: "{\"on\": \"amazonas\", \"s\": 1.5}"))),
                "lines[1].mouth.s");
            yield return Case("afluente sin fill_on_pass", Doc(lines: Lines(Main(), Trib(fill: string.Empty))), "lines[1].fill_on_pass");
            yield return Case("fill_on_pass 0", Doc(lines: Lines(Main(), Trib(fill: ", \"fill_on_pass\": 0"))), "lines[1].fill_on_pass");
            yield return Case("tris fuera de verts", Doc(land: Land(tris: EncodeIndices(new ushort[] { 0, 1, 2, 0, 2, 4 }))), "land.tris");
            yield return Case("tris sin triangulos", Doc(land: Land(tris: EncodeIndices(new ushort[] { 0, 1, 2, 3 }))), "land.tris");
            yield return Case("zonas sin cubrir", Doc(land: Land(zones: "[{\"id\": \"a\", \"tile\": \"canopy\", \"first\": 0, \"count\": 1}]")),
                "land.zones");
            yield return Case("zonas solapadas", Doc(land: Land(zones: "[{\"id\": \"a\", \"tile\": \"canopy\", \"first\": 0, \"count\": 2}, "
                + "{\"id\": \"b\", \"tile\": \"rock\", \"first\": 1, \"count\": 1}]")), "land.zones[1]");
            yield return Case("casa en una linea desconocida", Doc(home: "{\"on\": \"nadie\", \"s\": 0.9}"), "home.on");
            yield return Case("punto de paso en una linea desconocida", Doc(passing: "[{\"id\": \"gurupa\", \"line\": \"nadie\", \"s\": 0.8, \"fact\": null}]"),
                "passing[0].line");
            yield return Case("punto de paso con el id de un mirador", Doc(passing: "[{\"id\": \"boca\", \"line\": \"amazonas\", \"s\": 0.8, \"fact\": null}]"),
                "passing[0].id");
            yield return Case("dato de mirador fuera de map.*", Doc(viewpoints: "[{\"id\": \"boca\", \"xy\": [10, 10], \"reveal_on\": 10, \"fact\": \"fruta.acai\"}]"),
                "viewpoints[0].fact");
            yield return Case("mirador revelado en el 0", Doc(viewpoints: "[{\"id\": \"boca\", \"xy\": [10, 10], \"reveal_on\": 0, \"fact\": null}]"),
                "viewpoints[0].reveal_on");
            yield return Case("pais en minusculas", Doc(labels: "[{\"country\": \"br\", \"xy\": [100, 200]}]"), "labels[0].country");
            yield return Case("rotulo fuera de uint16", Doc(labels: "[{\"country\": \"BR\", \"xy\": [70000, 200]}]"), "labels[0].xy[0]");
            yield return Case("recuadro enlazado a nada", Doc(insets: Inset(link: "casiquiare")), "insets[0].link");
            yield return Case("main con fill_on_pass", Doc(lines: Lines(Main(extra: ", \"fill_on_pass\": 0.1"), Trib())), "lines[0].fill_on_pass");
            yield return Case("water de largo cero", Doc(lines: Lines(Main(water: Water("0.0", "0.4", "andean") + ", " + Water("0.4", "0.4", "white")),
                Trib())), "lines[0].water[1].s1");
            yield return Case("tierra con dos vertices", Doc(land: Land(verts: EncodePoints(new ushort[] { 0, 0, 65535, 0 }))), "land.verts");
            yield return Case("zonas vacias", Doc(land: Land(zones: "[]")), "land.zones");
            yield return Case("zona fuera de los triangulos", Doc(land: Land(zones: "[{\"id\": \"a\", \"tile\": \"canopy\", \"first\": 1, \"count\": 2}]")),
                "land.zones[0]");
            yield return Case("id de mirador repetido", Doc(viewpoints: "[" + Viewpoint() + ", " + Viewpoint() + "]"), "viewpoints[1].id");
            yield return Case("id de punto de paso repetido", Doc(passing: "[{\"id\": \"gurupa\", \"line\": \"amazonas\", \"s\": 0.8, \"fact\": null}, "
                + "{\"id\": \"gurupa\", \"line\": \"amazonas\", \"s\": 0.7, \"fact\": null}]"), "passing[1].id");
            yield return Case("mirador revelado despues del 1000", Doc(viewpoints: "[" + Viewpoint(reveal: "1001") + "]"), "viewpoints[0].reveal_on");
            yield return Case("mirador sin fact", Doc(viewpoints: "[{\"id\": \"boca\", \"xy\": [10, 10], \"reveal_on\": 10}]"), "viewpoints[0].fact");
            yield return Case("fact con belen", Doc(viewpoints: "[" + Viewpoint(fact: "\"map.stop.belen.dato_01\"") + "]"), "viewpoints[0].fact");
            yield return Case("recuadro al reves", Doc(insets: Inset(rect: "[0.98, 0.05, 0.78, 0.3]")), "insets[0].rect");
            yield return Case("recuadro de tres valores", Doc(insets: Inset(rect: "[0.78, 0.05, 0.98]")), "insets[0].rect");
            yield return Case("recuadro fuera de [0, 1]", Doc(insets: Inset(rect: "[0.78, 0.05, 1.2, 0.3]")), "insets[0].rect[2]");
            yield return Case("recuadro con una linea desconocida", Doc(insets: Inset(lines: "[\"nadie\"]")), "insets[0].lines[0]");
            yield return Case("dato de pais con tres letras", Doc(viewpoints: "[" + Viewpoint(fact: "\"map.country.bra.dato_01\"") + "]"),
                "viewpoints[0].fact");
            yield return Case("dato de parada con un digito", Doc(viewpoints: "[" + Viewpoint(fact: "\"map.stop.x.dato_1\"") + "]"),
                "viewpoints[0].fact");
            yield return Case("franja con agua andina", Doc(lines: Lines(Main(splits: "{\"id\": \"encontro\", \"s0\": 0.5, \"s1\": 0.6, "
                + "\"left\": \"andean\", \"right\": \"white\"}"), Trib())), "lines[0].splits[0].left");
        }

        [TestCaseSource(nameof(InvalidCases))]
        public void Reader_RejectsInvalid_WithThePath(string json, string prefix)
        {
            var e = Assert.Throws<LevelFormatException>(() => RiverMapReader.Read(json));
            Assert.That(e!.Message, Does.Contain(prefix));
        }

        private static IEnumerable<TestCaseData> InvalidRoutes()
        {
            yield return RouteCase("linea de la parada desconocida", Routes(Stop(0, at: "{\"line\": \"nadie\", \"s\": 0.9}")), "stops[0].at.line");
            yield return RouteCase("n no contiguo", Routes(Stop(0), Stop(2)), "stops[1].n");
            yield return RouteCase("ruta que no sale de la anterior", Routes(Stop(0),
                Stop(1, path: "[{\"line\": \"amazonas\", \"s0\": 0.8, \"s1\": 0.3}, {\"line\": \"negro\", \"s0\": 1.0, \"s1\": 0.5}]")),
                "stops[1].leg.path[0]");
            yield return RouteCase("ruta que no llega a la parada", Routes(Stop(0),
                Stop(1, path: "[{\"line\": \"amazonas\", \"s0\": 0.9, \"s1\": 0.3}, {\"line\": \"negro\", \"s0\": 1.0, \"s1\": 0.6}]")),
                "stops[1].leg.path[1]");
            yield return RouteCase("tramos que no se unen", Routes(Stop(0),
                Stop(1, path: "[{\"line\": \"amazonas\", \"s0\": 0.9, \"s1\": 0.5}, {\"line\": \"negro\", \"s0\": 1.0, \"s1\": 0.5}]")),
                "stops[1].leg.path[1]");
            yield return RouteCase("s del tramo fuera de [0, 1]", Routes(Stop(0),
                Stop(1, path: "[{\"line\": \"amazonas\", \"s0\": 0.9, \"s1\": -0.1}, {\"line\": \"negro\", \"s0\": 1.0, \"s1\": 0.5}]")),
                "stops[1].leg.path[0].s1");
            yield return RouteCase("via que no es punto de paso", Routes(Stop(0), Stop(1, via: "[\"manaos\"]")), "stops[1].leg.via[0]");
            yield return RouteCase("boca de una linea main", Routes(Stop(0), Stop(1, mouths: "[\"amazonas\"]")), "stops[1].leg.mouths[0]");
            yield return RouteCase("parada sin id", Routes(Stop(0), Stop(1, id: null)), "stops[1].id");
            yield return RouteCase("parada con id numerico", Routes(Stop(0), Stop(1, id: "7")), "stops[1].id");
            yield return RouteCase("leg que no es un objeto", Routes(Stop(0), Stop(1, leg: "[]")), "stops[1].leg");
            yield return RouteCase("leg ausente", Routes(Stop(0), Stop(1, leg: string.Empty)), "stops[1].leg");
            yield return RouteCase("path vacio", Routes(Stop(0), Stop(1, path: "[]")), "stops[1].leg.path");
            yield return RouteCase("via que no es una lista", Routes(Stop(0), Stop(1, via: "\"gurupa\"")), "stops[1].leg.via");
            yield return RouteCase("mouths que no es una lista", Routes(Stop(0), Stop(1, mouths: "{}")), "stops[1].leg.mouths");
        }

        [Test]
        public void MinimalRoutes_Read()
        {
            RiverRoutes routes = RiverMapReader.ReadRoutes(Routes(Stop(0), Stop(1)), RiverMapReader.Read(Doc()));
            Assert.That(routes.Count, Is.EqualTo(2));
            Assert.That(routes.At(1)!.Path.Count, Is.EqualTo(2));
            Assert.That(routes.At(1)!.Via, Is.EqualTo(new[] { "gurupa" }));
            Assert.That(routes.At(1)!.Mouths, Is.EqualTo(new[] { 1 }));
        }

        [TestCaseSource(nameof(InvalidRoutes))]
        public void Routes_RejectInvalid_WithThePath(string json, string prefix)
        {
            RiverMap map = RiverMapReader.Read(Doc());
            var e = Assert.Throws<LevelFormatException>(() => RiverMapReader.ReadRoutes(json, map));
            Assert.That(e!.Message, Does.Contain(prefix));
        }

        // ---------------------------------------------------------------------------------------------- sin asignaciones

        [Test]
        public void Lookups_DoNotAllocate()
        {
            RiverMap map = RiverMapReader.Read(File.ReadAllText(RealMapPath));
            RiverRoutes routes = RiverMapReader.ReadRoutes(File.ReadAllText(RealStopsPath), map);
            RiverLine amazonas = map.Lines[0];
            TestDelegate call = () =>
            {
                int acc = map.LineIndexOf("negro") + map.LineIndexOf("pacaya");
                acc += (int)amazonas.WaterAt(0.05f) + (int)amazonas.WaterAt(0.715f);
                acc += amazonas.TryGetSplitAt(0.715f, out RiverSplit split) ? (int)split.Left : 0;
                acc += amazonas.NextBoundaryAfter(0.3f) > 0.5f ? 1 : 0;
                s_sink = map.Line("tapajos");
                s_sink = map.PassingPoint("gurupa");
                s_sink = routes.At(5);
                acc += (int)(amazonas.SOfPoint(10) * 100f);
                s_count = acc;
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(s_sink, Is.Not.Null);
            Assert.That(s_count, Is.GreaterThan(0));
        }

        // ---------------------------------------------------------------------------------------------- utilidades

        // Distancia en dp de la Cuenca (328 dp por unidad) entre dos posiciones (línea, s).
        private static float GapDp(RiverMap map, int lineA, float sA, int lineB, float sB)
        {
            map.Lines[lineA].PointAt(sA, out float ax, out float ay);
            map.Lines[lineB].PointAt(sB, out float bx, out float by);
            return (float)Math.Sqrt(((ax - bx) * (ax - bx)) + ((ay - by) * (ay - by))) * 328f;
        }

        /// <summary>
        /// Codifica pares uint16 como <c>tools/river-map/geometry.py</c> (<c>encode_points</c>): little-endian, el primer par
        /// absoluto y los demás como deltas módulo 65.536, en base64.
        /// </summary>
        internal static string EncodePoints(ushort[] xy)
        {
            var bytes = new byte[xy.Length * 2];
            for (int i = 0; i < xy.Length; i++)
            {
                int value = i < 2 ? xy[i] : (xy[i] - xy[i - 2] + 65536) % 65536;
                bytes[2 * i] = (byte)(value & 0xFF);
                bytes[(2 * i) + 1] = (byte)(value >> 8);
            }

            return Convert.ToBase64String(bytes);
        }

        /// <summary>Codifica índices uint16 absolutos (<c>encode_indices</c>).</summary>
        internal static string EncodeIndices(ushort[] values)
        {
            var bytes = new byte[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                bytes[2 * i] = (byte)(values[i] & 0xFF);
                bytes[(2 * i) + 1] = (byte)(values[i] >> 8);
            }

            return Convert.ToBase64String(bytes);
        }

        // pts de la línea «id» del JSON real, decodificados (sin pasar por Read).
        internal static ushort[] RawPoints(string json, string id)
        {
            string marker = "{\"id\": \"" + id + "\", \"kind\"";
            int at = json.IndexOf(marker, StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), id);
            int pts = json.IndexOf("\"pts\": \"", at, StringComparison.Ordinal) + 8;
            int end = json.IndexOf('"', pts);
            return RiverMapReader.DecodePoints(json.Substring(pts, end - pts), id);
        }

        internal static string Doc(string format = RiverMap.Format, string? frame = null, string? lines = null, string? land = null,
            string insets = "[]", string home = "{\"on\": \"amazonas\", \"s\": 0.9}", string labels = "[{\"country\": \"BR\", \"xy\": [100, 200]}]",
            string viewpoints = "[{\"id\": \"boca\", \"xy\": [10, 10], \"reveal_on\": 10, \"fact\": \"map.stop.estuario.dato_01\"}]",
            string passing = "[{\"id\": \"gurupa\", \"line\": \"amazonas\", \"s\": 0.8, \"fact\": null}]")
        {
            var sb = new StringBuilder();
            sb.Append("{\"format\": \"").Append(format).Append("\",\n");
            sb.Append("\"frame\": ").Append(frame ?? Frame()).Append(",\n");
            sb.Append("\"lines\": ").Append(lines ?? Lines(Main(), Trib())).Append(",\n");
            sb.Append("\"land\": ").Append(land ?? Land()).Append(",\n");
            sb.Append("\"insets\": ").Append(insets).Append(",\n");
            sb.Append("\"home\": ").Append(home).Append(",\n");
            sb.Append("\"labels\": ").Append(labels).Append(",\n");
            sb.Append("\"viewpoints\": ").Append(viewpoints).Append(",\n");
            sb.Append("\"passing\": ").Append(passing).Append("\n}\n");
            return sb.ToString();
        }

        internal static string Frame(string rotation = "cw90", string lat = "[-18.0, 7.0]", string lon = "[-80.0, -47.0]", string lat0 = "-5.0")
        {
            return "{\"rotation\": \"" + rotation + "\", \"lat\": " + lat + ", \"lon\": " + lon + ", \"lat0\": " + lat0 + "}";
        }

        internal static string Lines(params string[] lines)
        {
            return "[\n  " + string.Join(",\n  ", lines) + "\n]";
        }

        internal static string Main(string id = "amazonas", string cls = "1", string src = "rio_amazonas", string? pts = null,
            string? water = null, string? splits = null, string extra = "")
        {
            pts ??= EncodePoints(new ushort[] { 1000, 1000, 30000, 20000, 60000, 50000 });
            water ??= Water("0.0", "0.4", "andean") + ", " + Water("0.4", "1.0", "white");
            splits ??= Split("0.5", "0.6");
            return "{\"id\": \"" + id + "\", \"kind\": \"main\", \"class\": " + cls + ", \"src\": \"" + src + "\", \"pts\": \"" + pts
                + "\", \"water\": [" + water + "], \"splits\": [" + splits + "]" + extra + "}";
        }

        internal static string Trib(string id = "negro", string kind = "tributary", string? pts = null,
            string mouth = "{\"on\": \"amazonas\", \"s\": 0.3}", string fill = ", \"fill_on_pass\": 0.1", string extra = "")
        {
            pts ??= EncodePoints(new ushort[] { 20000, 40000, 30000, 20000 });
            return "{\"id\": \"" + id + "\", \"kind\": \"" + kind + "\", \"class\": 2, \"src\": \"rio_amazonas\", \"pts\": \"" + pts
                + "\", \"water\": [" + Water("0.0", "1.0", "black") + "], \"mouth\": " + mouth + fill + extra + "}";
        }

        internal static string Water(string s0, string s1, string type)
        {
            return "{\"s0\": " + s0 + ", \"s1\": " + s1 + ", \"type\": \"" + type + "\"}";
        }

        internal static string Split(string s0, string s1, string id = "encontro")
        {
            return "{\"id\": \"" + id + "\", \"s0\": " + s0 + ", \"s1\": " + s1 + ", \"left\": \"black\", \"right\": \"white\"}";
        }

        internal static string Land(string? verts = null, string? tris = null,
            string zones = "[{\"id\": \"land\", \"tile\": \"canopy\", \"first\": 0, \"count\": 2}]")
        {
            verts ??= EncodePoints(new ushort[] { 0, 0, 65535, 0, 65535, 65535, 0, 65535 });
            tris ??= EncodeIndices(new ushort[] { 0, 1, 2, 0, 2, 3 });
            return "{\"verts\": \"" + verts + "\", \"tris\": \"" + tris + "\", \"zones\": " + zones + "}";
        }

        private static string Viewpoint(string reveal = "10", string fact = "\"map.stop.estuario.dato_01\"")
        {
            return "{\"id\": \"boca\", \"xy\": [10, 10], \"reveal_on\": " + reveal + ", \"fact\": " + fact + "}";
        }

        private static string Inset(string rect = "[0.78, 0.05, 0.98, 0.3]", string lines = "[\"negro\"]", string link = "amazonas")
        {
            return "[{\"id\": \"orinoco\", \"rect\": " + rect + ", \"lines\": " + lines + ", \"link\": \"" + link + "\"}]";
        }

        // Paradas mínimas para las rutas sobre el documento mínimo: n = 0 en el cauce (s 0,9) y n = 1 en el Negro (s 0,5),
        // remontando el cauce hasta su boca (s 0,3) y el Negro desde la boca.
        // id: el valor JSON del campo (por defecto "p<n>"; null lo omite). leg: el objeto entero (por defecto, con path, via y
        // mouths; vacío lo omite).
        private static string Stop(int n, string? at = null, string? path = null, string? via = null, string? mouths = null,
            string? id = "", string? leg = null)
        {
            at ??= n == 0 ? "{\"line\": \"amazonas\", \"s\": 0.9}" : "{\"line\": \"negro\", \"s\": 0.5}";
            path ??= n == 0
                ? "[{\"line\": \"amazonas\", \"s0\": 0.9, \"s1\": 0.9}]"
                : "[{\"line\": \"amazonas\", \"s0\": 0.9, \"s1\": 0.3}, {\"line\": \"negro\", \"s0\": 1.0, \"s1\": 0.5}]";
            via ??= n == 0 ? "[]" : "[\"gurupa\"]";
            mouths ??= n == 0 ? "[]" : "[\"negro\"]";
            leg ??= "{\"path\": " + path + ", \"via\": " + via + ", \"mouths\": " + mouths + "}";
            if (id != null && id.Length == 0)
            {
                id = "\"p" + n.ToString(CultureInfo.InvariantCulture) + "\"";
            }

            return "{\"n\": " + n.ToString(CultureInfo.InvariantCulture) + (id != null ? ", \"id\": " + id : string.Empty)
                + ", \"at\": " + at + (leg.Length > 0 ? ", \"leg\": " + leg : string.Empty) + "}";
        }

        private static string Routes(params string[] stops)
        {
            return "{\"format\": \"af-river-stops/1\", \"stops\": [" + string.Join(", ", stops) + "]}";
        }

        // El lector escribe «Mapa del río, <ruta>: …» (y las rutas, «Rutas del Mapa, <ruta>: …»): se compara ese prefijo exacto,
        // para que un error en una ruta más profunda (land.zones[0] en lugar de land.zones) no pase por bueno.
        private static TestCaseData Case(string name, string json, string path)
        {
            return new TestCaseData(json, "Mapa del río, " + path + ":").SetName("Reader_RejectsInvalid_WithThePath(" + name + ")");
        }

        private static TestCaseData RouteCase(string name, string json, string path)
        {
            return new TestCaseData(json, "Rutas del Mapa, " + path + ":").SetName("Routes_RejectInvalid_WithThePath(" + name + ")");
        }
    }
}
