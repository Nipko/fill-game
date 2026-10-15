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
    /// Paradas del Mapa del río <c>af-river-stops/1</c> en el cliente (D-013; 08 §2.2 y §4.5; contrato 30; tarea M1.2): el lector
    /// acepta el JSON mínimo y el archivo real, rechaza cada caso inválido con la ruta del valor (las reglas de
    /// <c>tools/river-map/stops.py --check</c>, entre ellas que la última parada termine en <see cref="WorldTable.LastLevelOf"/> de
    /// su mundo), y las búsquedas (<see cref="RiverStopCatalog.StopOf"/>, <see cref="RiverStopCatalog.ArrivalOnWin"/>…) no
    /// asignan memoria.
    /// </summary>
    public sealed class RiverStopCatalogTests
    {
        private static object? s_sink;

        private static string RealPath => Path.Combine(Application.dataPath, "_Project/Data/Map/river_stops_v1.json");

        // ---------------------------------------------------------------------------------------------- JSON mínimo

        [Test]
        public void MinimalJson_OneStopInWorldOne_Reads()
        {
            RiverStopCatalog catalog = RiverStopCatalogReader.Read(Doc(Stop(0)));
            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(catalog.LastLevel, Is.EqualTo(10));
            Assert.That(catalog.Source, Is.EqualTo("docs/research/mapa-del-rio/rio-amazonas.json"));
            RiverStop s = catalog.At(0)!;
            Assert.That(s.Id, Is.EqualTo("casa"));
            Assert.That(s.Kind, Is.EqualTo(StopKind.Gate));
            Assert.That(s.World, Is.EqualTo(1));
            Assert.That(s.FirstLevel, Is.EqualTo(1));
            Assert.That(s.LastLevel, Is.EqualTo(10));
            Assert.That(s.Country, Is.EqualTo("BR"));
            Assert.That(s.KmFromSea, Is.Null);
            Assert.That(s.KmHome, Is.EqualTo(0));
            Assert.That(s.FlagRaise, Is.EqualTo("BR"));
            Assert.That(s.ArrivalFact, Is.Null);
            Assert.That(s.Facts, Is.Empty);
            Assert.That(s.Backdrop.Kit, Is.EqualTo("bg_kit_w01"));
            Assert.That(s.Backdrop.Hero, Is.EqualTo("bg_hero_casa"));
            Assert.That(s.Backdrop.Code, Is.Null);
            Assert.That(s.NameKey, Is.EqualTo("map.stop.casa.name"), "nombre por convención (08 §4.5)");
            Assert.That(s.RiverKey, Is.EqualTo("map.stop.casa.river"));
            Assert.That(s.CountryNameKeyOrDefault, Is.EqualTo("map.country.br.name"));
            Assert.That(catalog.StopOf(1), Is.SameAs(s));
            Assert.That(catalog.StopOf(10), Is.SameAs(s));
            Assert.That(catalog.StopOf(11), Is.Null);
            Assert.That(catalog.ArrivalOnWin(10), Is.Null, "no hay parada 1");
        }

        [Test]
        public void TwoWorlds_Fixture_Reads_WithGateStopAndClose()
        {
            RiverStopCatalog catalog = RiverStopCatalogReader.Read(Doc(TwoWorlds()));
            Assert.That(catalog.Count, Is.EqualTo(4));
            Assert.That(catalog.At(1)!.Kind, Is.EqualTo(StopKind.Gate));
            Assert.That(catalog.At(2)!.Kind, Is.EqualTo(StopKind.Stop));
            Assert.That(catalog.At(3)!.Kind, Is.EqualTo(StopKind.Close));
            Assert.That(catalog.FirstStopOfWorld(1)!.N, Is.EqualTo(0));
            Assert.That(catalog.FirstStopOfWorld(2)!.N, Is.EqualTo(1));
            Assert.That(catalog.FirstStopOfWorld(3), Is.Null);
            Assert.That(catalog.At(3)!.AnswersWorldQuestion, Is.True);
            Assert.That(catalog.At(1)!.WorldQuestion, Is.EqualTo("map.world.w02.question"));
            Assert.That(catalog.At(3)!.CountryName, Is.EqualTo("map.stop.cierre.country"));
            Assert.That(catalog.At(3)!.CountryNameKeyOrDefault, Is.EqualTo("map.stop.cierre.country"));
            Assert.That(catalog.At(2)!.Facts, Is.EqualTo(new[] { "map.myth.boto", "map.general.dato_negra" }));
        }

        // ---------------------------------------------------------------------------------------------- casos inválidos

        private static IEnumerable<TestCaseData> InvalidCases()
        {
            yield return Case("no es JSON", "{\"format\": ", "no son JSON válido");
            yield return Case("format ajeno", Doc(new[] { Stop(0) }, format: "af-river-map/1"), "format");
            yield return Case("sin paradas", Doc(Array.Empty<string>()), "stops");
            yield return Case("n no contiguo", Doc(Replace(TwoWorlds(), 1, Stop(2, world: 2, id: "uno"))), "stops[1].n");
            yield return Case("levels fuera de la cadencia", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio", levels: "[21, 31]"))),
                "stops[2].levels");
            yield return Case("mundo distinto de WorldTable", Doc(Replace(TwoWorlds(), 1, Stop(1, world: 3, id: "puerta", kind: "gate",
                arrival: "\"map.stop.casa.dato_01\"", kit: "bg_kit_w03"))), "stops[1].world");
            yield return Case("gate fuera del primer tramo", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio", kind: "gate"))),
                "stops[2].kind");
            yield return Case("primera del mundo sin gate", Doc(Replace(TwoWorlds(), 1, Stop(1, world: 2, id: "puerta", kind: "stop",
                question: "\"map.world.w02.question\""))), "stops[1].kind");
            yield return Case("ultima del mundo sin close", Doc(Replace(TwoWorlds(), 3, Stop(3, world: 2, id: "cierre", kind: "stop",
                answers: true))), "stops[3].kind");
            yield return Case("close fuera del ultimo tramo", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio", kind: "close"))),
                "stops[2].kind");
            yield return Case("termina a mitad de un mundo", Doc(Take(TwoWorlds(), 3)), "stops[2].levels");
            yield return Case("kit de otro mundo", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio", kit: "bg_kit_w01"))),
                "stops[2].bg.kit");
            yield return Case("ni hero ni code", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio", hero: "null"))), "stops[2].bg");
            yield return Case("belen en un id", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "belen_viejo"))), "stops[2].id");
            // Un salto de línea final (escape \n del JSON) no pasa por id, clave ni pieza (en .NET, «$» lo admitiría).
            yield return Case("id con salto de linea final", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio\\n"))), "stops[2].id");
            yield return Case("llegada con salto de linea final", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio",
                arrival: "\"map.stop.medio.dato_01\\n\""))), "stops[2].arrival_fact");
            yield return Case("dato con salto de linea final", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio",
                facts: "[\"map.myth.boto\\n\"]"))), "stops[2].facts[0]");
            yield return Case("hero con salto de linea final", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio",
                hero: "\"bg_hero_medio\\n\""))), "stops[2].bg.hero");
            yield return Case("code con salto de linea final", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio", hero: "null",
                code: "\"kit:orilla\\n\""))), "stops[2].bg.code");
            yield return Case("id repetido",Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "puerta"))), "stops[2].id");
            yield return Case("llegada null fuera de la parada 0", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio", arrival: "null"))),
                "stops[2].arrival_fact");
            yield return Case("bandera sin cambio de pais", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio", flag: "\"BR\""))),
                "stops[2].flag_raise");
            yield return Case("pregunta sin respuesta", Doc(Replace(TwoWorlds(), 3, Stop(3, world: 2, id: "cierre", kind: "close",
                countryName: "\"map.stop.cierre.country\""))), "stops[1].world_question");
            yield return Case("pregunta de otro mundo", Doc(Replace(TwoWorlds(), 1, Stop(1, world: 2, id: "puerta", kind: "gate",
                question: "\"map.world.w03.question\""))), "stops[1].world_question");
            yield return Case("country_name fuera de la convencion", Doc(Replace(TwoWorlds(), 3, Stop(3, world: 2, id: "cierre", kind: "close",
                answers: true, countryName: "\"map.stop.otro.country\""))), "stops[3].country_name");
            yield return Case("falta un campo obligatorio", Doc(Replace(TwoWorlds(), 2, Stop(2, world: 2, id: "medio")
                .Replace("\"sample\": \"white\", ", string.Empty))), "stops[2].sample");
        }

        [TestCaseSource(nameof(InvalidCases))]
        public void Reader_RejectsInvalid_WithThePath(string json, string path)
        {
            var e = Assert.Throws<LevelFormatException>(() => RiverStopCatalogReader.Read(json));
            Assert.That(e!.Message, Does.Contain(path));
        }

        // ---------------------------------------------------------------------------------------------- archivo real

        [Test]
        public void RealFile_Validates_AndAnswersTheAcceptanceLookups()
        {
            RiverStopCatalog catalog = RiverStopCatalogReader.Read(File.ReadAllText(RealPath));
            Assert.That(catalog.Count, Is.EqualTo(20), "1-200: 20 paradas (08 §4.5)");
            Assert.That(catalog.LastLevel, Is.EqualTo(200));
            Assert.That(catalog.StopOf(58)!.Id, Is.EqualTo("manaos"), "StopOf(58) es Manaos");
            Assert.That(catalog.StopOf(58)!.N, Is.EqualTo(5));
            Assert.That(catalog.ArrivalOnWin(20)!.Id, Is.EqualTo("alter_do_chao"), "ArrivalOnWin(20) es Alter do Chão");
            Assert.That(catalog.ArrivalOnWin(21), Is.Null);
            Assert.That(catalog.StopOf(201), Is.Null);
            Assert.That(catalog.StopOf(0), Is.Null);
            Assert.That(catalog.ArrivalOnWin(0), Is.Null);
            Assert.That(catalog.ArrivalOnWin(10)!.Id, Is.EqualTo("santarem"), "el 10 lleva a la parada 1 (Mapa del 10)");
            Assert.That(catalog.ArrivalOnWin(200), Is.Null, "sin parada 20 en 1-200");
            Assert.That(catalog.At(1)!.ArrivalFact, Is.EqualTo("map.stop.estuario.dato_03"), "el Dato del río 1 (06 §2.3)");
            Assert.That(catalog.FirstStopOfWorld(3)!.Id, Is.EqualTo("encontro"));
            Assert.That(catalog.FirstStopOfWorld(6), Is.Null);
            Assert.That(catalog.At(13)!.CountryNameKeyOrDefault, Is.EqualTo("map.stop.rio_frontera.country"), "«Colombia y Perú»");
        }

        [Test]
        public void RealFile_StopsFollowWorldTable_KitsAndNoBelen()
        {
            RiverStopCatalog catalog = RiverStopCatalogReader.Read(File.ReadAllText(RealPath));
            var worlds = new HashSet<int>();
            for (int n = 0; n < catalog.Count; n++)
            {
                RiverStop s = catalog.At(n)!;
                Assert.That(s.N, Is.EqualTo(n), "k contiguos");
                for (int level = s.FirstLevel; level <= s.LastLevel; level++)
                {
                    Assert.That(catalog.StopOf(level), Is.SameAs(s), "nivel " + level);
                    Assert.That(WorldTable.WorldOf(level), Is.EqualTo(s.World), s.Id + ", nivel " + level);
                }

                Assert.That(s.Backdrop.Kit, Is.EqualTo("bg_kit_w" + s.World.ToString("00", CultureInfo.InvariantCulture)), s.Id);
                Assert.That(s.Id, Does.Not.Contain("belen"));
                Assert.That(s.Backdrop.Hero ?? string.Empty, Does.Not.Contain("belen"));
                Assert.That(s.Backdrop.Code ?? string.Empty, Does.Not.Contain("belen"));
                Assert.That(s.ArrivalFact ?? string.Empty, Does.Not.Contain("belen"));
                worlds.Add(s.World);
            }

            // Cada mundo presente está completo: su primera parada es la gate y la última termina en WorldTable.LastLevelOf.
            foreach (int world in worlds)
            {
                RiverStop gate = catalog.FirstStopOfWorld(world)!;
                Assert.That(gate.Kind, Is.EqualTo(StopKind.Gate), "mundo " + world);
                Assert.That(gate.FirstLevel, Is.EqualTo(WorldTable.FirstLevel(world)));
                RiverStop last = catalog.StopOf(WorldTable.LastLevelOf(world))!;
                Assert.That(last.World, Is.EqualTo(world));
                Assert.That(last.LastLevel, Is.EqualTo(WorldTable.LastLevelOf(world)), "mundo " + world);
                Assert.That(last.Kind, Is.EqualTo(world == 1 ? StopKind.Gate : StopKind.Close), "mundo " + world);
            }

            Assert.That(catalog.LastLevel, Is.EqualTo(WorldTable.LastLevelOf(catalog.At(catalog.Count - 1)!.World)),
                "la última parada termina en el último nivel de su mundo (1-200: el 200)");
        }

        // ---------------------------------------------------------------------------------------------- sin asignaciones

        [Test]
        public void Lookups_DoNotAllocate() // 08 §2.2: StopOf sin asignaciones
        {
            RiverStopCatalog catalog = RiverStopCatalogReader.Read(File.ReadAllText(RealPath));
            TestDelegate call = () =>
            {
                s_sink = catalog.StopOf(58);
                s_sink = catalog.StopOf(201);
                s_sink = catalog.ArrivalOnWin(20);
                s_sink = catalog.ArrivalOnWin(21);
                s_sink = catalog.FirstStopOfWorld(4);
                s_sink = catalog.At(19);
            };
            call(); // calentamiento
            Assert.That(call, Is.Not.AllocatingGCMemory());
            Assert.That(s_sink, Is.Not.Null);
        }

        // ---------------------------------------------------------------------------------------------- fixtures

        // Mundo 1 (n = 0) y mundo 2 completo (11-40: gate, stop y close), con la pregunta del mundo 2 planteada en n = 1 y
        // respondida en n = 3.
        private static string[] TwoWorlds()
        {
            return new[]
            {
                Stop(0),
                Stop(1, world: 2, id: "puerta", kind: "gate", question: "\"map.world.w02.question\"", code: "\"codigo:dos_aguas\"",
                    hero: "null"),
                Stop(2, world: 2, id: "medio", facts: "[\"map.myth.boto\", \"map.general.dato_negra\"]"),
                Stop(3, world: 2, id: "cierre", kind: "close", answers: true, countryName: "\"map.stop.cierre.country\""),
            };
        }

        private static string Stop(int n, int world = 1, string id = "casa", string? kind = null, string? levels = null,
            string? arrival = null, string? flag = null, string facts = "[]", string question = "null", bool answers = false,
            string? kit = null, string? hero = null, string code = "null", string countryName = "null")
        {
            string Inv(int v) => v.ToString(CultureInfo.InvariantCulture);
            kind ??= n == 0 ? "gate" : "stop";
            levels ??= "[" + Inv((10 * n) + 1) + ", " + Inv((10 * n) + 10) + "]";
            arrival ??= n == 0 ? "null" : "\"map.stop." + id + ".dato_01\"";
            flag ??= n == 0 ? "\"BR\"" : "null";
            kit ??= "bg_kit_w" + world.ToString("00", CultureInfo.InvariantCulture);
            hero ??= "\"bg_hero_" + id + "\"";
            string km = n == 0 ? "null" : Inv(100 * n);
            return "{\"n\": " + Inv(n) + ", \"levels\": " + levels + ", \"world\": " + Inv(world) + ", \"id\": \"" + id + "\", \"kind\": \""
                + kind + "\", \"country\": \"BR\", \"country_name\": " + countryName + ", \"km_from_sea\": " + km + ", \"km_home\": "
                + Inv(100 * n) + ", \"sample\": \"white\", \"flag_raise\": " + flag + ", \"arrival_fact\": " + arrival + ", \"facts\": "
                + facts + ", \"world_question\": " + question + ", \"answers_world_question\": " + (answers ? "true" : "false")
                + ", \"bg\": {\"kit\": \"" + kit + "\", \"hero\": " + hero + ", \"code\": " + code + ", \"lut\": null}}";
        }

        private static string Doc(string stop)
        {
            return Doc(new[] { stop });
        }

        private static string Doc(string[] stops, string format = RiverStopCatalog.Format)
        {
            var sb = new StringBuilder();
            sb.Append("{\"format\": \"").Append(format).Append("\", \"source\": \"docs/research/mapa-del-rio/rio-amazonas.json\", \"stops\": [");
            for (int i = 0; i < stops.Length; i++)
            {
                sb.Append(i == 0 ? "\n  " : ",\n  ").Append(stops[i]);
            }

            return sb.Append("\n]}\n").ToString();
        }

        private static string[] Replace(string[] stops, int index, string stop)
        {
            var copy = (string[])stops.Clone();
            copy[index] = stop;
            return copy;
        }

        private static string[] Take(string[] stops, int count)
        {
            var copy = new string[count];
            Array.Copy(stops, copy, count);
            return copy;
        }

        private static TestCaseData Case(string name, string json, string path)
        {
            return new TestCaseData(json, path).SetName("Reader_RejectsInvalid_WithThePath(" + name + ")");
        }
    }
}
