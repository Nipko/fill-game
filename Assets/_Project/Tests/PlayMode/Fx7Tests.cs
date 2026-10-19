#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.GameFlow;
using AF.Platform;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Arreglos de la prueba de la ola 5 de la tanda 4 en el teléfono (<c>docs/dev/traspaso/pruebas-telefono-2026-10-17-tanda4-ola5.md</c>;
    /// tarea FX7). La observación 3 (los remates tras la toada esperan lo que dura su clip) se prueba en
    /// <c>AudioRulesTests</c> y <c>AudioClipsPlayTests</c>; aquí:
    /// <list type="bullet">
    /// <item><description>observación 4: ninguna línea <c>AF_LAYOUT</c> pasa de <see cref="DevProbe.MaxLayoutLineBytes"/> bytes
    /// (logcat corta cada entrada en 4.068): con el Mapa a 560 y a 700 como en el teléfono (con el <c>map_seen_level</c> que
    /// traía el perfil, que <c>af_max_level</c> no toca), una sonda sale partida solo si entera no cabe (la principal hoy cabe; la
    /// del Muestrario abierto, no), en líneas de JSON válido con <c>"part":k,"parts":n</c> que, juntas, traen todos sus
    /// botones;</description></item>
    /// <item><description>observación 5: en la vista Cuenca con max 58, ninguna placa de mundo futuro pisa «Norte arriba» (la del
    /// mundo 5, «Loreto · Nivel 141», lo rozaba), ni «Volver a la canoa» si sale, con la vista de siempre y con el norte
    /// arriba; y con otros progresos. En la Cuenca sin girar tampoco quedan bajo la canoa, su mástil, sus banderas, el guía ni la
    /// fila de boyas del frente (revisión de FX7: con max 200 la placa del mundo 9 la rozaba); y cada placa tapa las menos líneas
    /// del lleno que puede entre los sitios libres que prueba, sin alejarse de su parada más de
    /// <see cref="RiverMapScreen.SilhouetteFarMaxDistDp"/> más medio ancho (la del mundo 9, Ucayali, saltaba con max 200 a la otra
    /// punta de la Cuenca); si tapa el lleno (con max 200, la del 7, la más ancha), es un límite aceptado, no un caso resuelto, y el
    /// test comprueba que a su alcance no queda ningún sitio libre fuera del lleno con lo que había al colocarla. Los países de la
    /// Cuenca tampoco quedan bajo «Norte arriba» ni los demás controles (con max 140 «Ecuador» quedaba tras el botón): o se
    /// apartan o, solo si no les queda ningún sitio libre de controles, de otros rótulos y del río y los controles les quitaron
    /// todos o uno que no lo pisaba, no salen en esa vista [P] (<see cref="RiverMapScreen.CountryBasinFallback"/>).</description></item>
    /// </list>
    /// No necesitan gráficos. La captura de la Cuenca con 58 es <c>15_map_lleno_cuenca_max58</c> (<c>MapProgressSnapshotTests</c>).
    /// </summary>
    public sealed class Fx7Tests
    {
        private AfLayoutLog? _layout;

        [SetUp]
        public void SetUp()
        {
            _layout = new AfLayoutLog();
        }

        [TearDown]
        public void TearDown()
        {
            _layout?.Dispose();
            RiverMapScreen.PlateTestNoNearSpotWorld = 0;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        // ============================================================================================ observación 4

        [Test]
        public void LayoutLines_FitsInOneLine_AsAlways_OrSplitsUnderTheCap_InOrder()
        {
            const string head = "AF_LAYOUT {\"screen\":\"river_map\",\"level\":561,\"sw\":1080,\"sh\":2340";
            const string cuias = "{\"i\":0,\"x\":1,\"y\":2,\"w\":3,\"h\":4},{\"i\":1,\"x\":5,\"y\":6,\"w\":7,\"h\":8}";
            string[] few = Buttons(3);
            string[] one = DevProbe.LayoutLines(head, cuias, few, DevProbe.MaxLayoutLineBytes);
            Assert.That(one.Length, Is.EqualTo(1), "una sonda que cabe, en una sola línea");
            Assert.That(one[0], Is.EqualTo(head + ",\"cuias\":[" + cuias + "],\"buttons\":[" + string.Join(",", few) + "]}"), "como siempre, sin part");
            AssertJson(one[0]);

            Assert.That(DevProbe.MaxLayoutLineBytes, Is.LessThanOrEqualTo(3900), "con margen bajo los 4.068 bytes de logcat");
            foreach (int count in new[] { 80, 140, 400 })
            {
                string[] buttons = Buttons(count);
                string[] lines = DevProbe.LayoutLines(head, cuias, buttons, DevProbe.MaxLayoutLineBytes);
                Assert.That(lines.Length, Is.GreaterThan(1), count + " botones no caben en una línea");
                var ids = new List<string>();
                for (int k = 0; k < lines.Length; k++)
                {
                    string line = lines[k];
                    Assert.That(Encoding.UTF8.GetByteCount(line), Is.LessThanOrEqualTo(DevProbe.MaxLayoutLineBytes), "parte " + (k + 1));
                    Assert.That(line.StartsWith(head + ",\"part\":" + (k + 1) + ",\"parts\":" + lines.Length + ",\"cuias\":[", StringComparison.Ordinal), Is.True,
                        "la misma cabecera, con su parte detrás de sh: " + line);
                    Assert.That(AfLayoutLog.PartOf(line), Is.EqualTo((k + 1, lines.Length)));
                    Assert.That(AfLayoutLog.Cuias(line).Length, Is.EqualTo(k == 0 ? 2 : 0), "las cuias, solo en la primera");
                    AssertJson(line);
                    ids.AddRange(AfLayoutLog.ButtonIds(line));
                }

                Assert.That(ids.Count, Is.EqualTo(count), "cada botón una vez");
                for (int i = 0; i < count; i++)
                {
                    Assert.That(ids[i], Is.EqualTo("stop_s" + i.ToString("000", CultureInfo.InvariantCulture)), "en orden");
                }
            }

            // Un tope en que cabe un botón por parte, pero no dos: cinco partes, cada una bajo el tope.
            int headBytes = Encoding.UTF8.GetByteCount(head);
            int buttonBytes = Encoding.UTF8.GetByteCount(Buttons(1)[0]);
            int oneEach = headBytes + Encoding.UTF8.GetByteCount(",\"part\":1,\"parts\":5,\"cuias\":[],\"buttons\":[]}") + buttonBytes + 8;
            AssertParts(DevProbe.LayoutLines(head, string.Empty, Buttons(5), oneEach), oneEach, 5, 0, "un botón por parte");

            // La excepción documentada: con un tope en que un botón suelto ya no cabe, cada botón va solo en su parte y solo esas
            // partes de un botón pasan del tope.
            int tiny = headBytes + 80;
            string[] tight = DevProbe.LayoutLines(head, string.Empty, Buttons(5), tiny);
            Assert.That(tight.Length, Is.EqualTo(5));
            for (int k = 0; k < tight.Length; k++)
            {
                Assert.That(AfLayoutLog.ButtonIds(tight[k]).Length, Is.EqualTo(1), "tope pequeño, parte " + (k + 1) + ": un botón suelto");
                Assert.That(Encoding.UTF8.GetByteCount(tight[k]), Is.GreaterThan(tiny), "solo pasa del tope porque ese botón no cabe ni solo");
                AssertJson(tight[k]);
            }

            // La parte 1 lleva las cuias: si con ellas no cabe ni el primer botón, la parte 1 va sin botones y ese botón abre la 2.
            var many = new StringBuilder();
            int cuiaCount = 0;
            int cap = DevProbe.MaxLayoutLineBytes;
            while (headBytes + 50 + many.Length + 1 + buttonBytes <= cap) // 50: "part", "parts" (4 cifras) y los corchetes
            {
                if (cuiaCount > 0)
                {
                    many.Append(',');
                }

                many.Append("{\"i\":").Append(cuiaCount.ToString(CultureInfo.InvariantCulture)).Append(",\"x\":1,\"y\":2,\"w\":3,\"h\":4}");
                cuiaCount++;
            }

            string[] heavy = DevProbe.LayoutLines(head, many.ToString(), Buttons(80), cap);
            AssertParts(heavy, cap, 80, cuiaCount, "cuias que casi llenan la parte 1");
            Assert.That(AfLayoutLog.ButtonIds(heavy[0]), Is.Empty, "la parte 1, solo con las cuias");
        }

        // Las partes de una sonda partida: bajo el tope, JSON válido, numeradas, las cuias en la primera y los botones, en orden.
        private static void AssertParts(string[] lines, int cap, int buttons, int cuias, string at)
        {
            Assert.That(lines.Length, Is.GreaterThan(1), at + ": partida");
            var ids = new List<string>();
            for (int k = 0; k < lines.Length; k++)
            {
                Assert.That(Encoding.UTF8.GetByteCount(lines[k]), Is.LessThanOrEqualTo(cap), at + ", parte " + (k + 1));
                Assert.That(AfLayoutLog.PartOf(lines[k]), Is.EqualTo((k + 1, lines.Length)), at);
                Assert.That(AfLayoutLog.Cuias(lines[k]).Length, Is.EqualTo(k == 0 ? cuias : 0), at + ": las cuias, solo en la primera");
                AssertJson(lines[k]);
                ids.AddRange(AfLayoutLog.ButtonIds(lines[k]));
            }

            Assert.That(ids.Count, Is.EqualTo(buttons), at + ": cada botón una vez");
            for (int i = 0; i < buttons; i++)
            {
                Assert.That(ids[i], Is.EqualTo("stop_s" + i.ToString("000", CultureInfo.InvariantCulture)), at + ": en orden");
            }
        }

        [UnityTest]
        public IEnumerator MapLayout_At560And700_NoLineOverTheCap_ValidJson_AllTheButtons()
        {
            // Como en el teléfono (observación 4): af_max_level 560 y 700 sobre un perfil que ya había visto el Mapa con otro progreso.
            // LaunchOptions.ApplyDevProgress no toca map_seen_level, así que el Mapa abre «poniéndose al día» desde el que traía el
            // perfil (0 en uno limpio; 68 tras la prueba de «ponerse al día» con af_max_level 68) o sin ponerse al día (= max). La app
            // abre en el inicio con «Jugar» en el siguiente nivel y el Mapa se abre desde su pestaña; abre en el mundo del siguiente
            // (con 560, el 9 y la s56; con 700, la última parada). Medido en la revisión de FX7: map_seen_level no cambia la sonda
            // principal (con 560, unos 1.000 bytes y 19-20 botones; con 700, unos 530 y 9), que cabe en una línea. La que pasa de
            // los 4.068 bytes de logcat es la del Mapa con el Muestrario abierto (con 560, unos 5.100 bytes): sale partida.
            foreach (int max in new[] { 560, 700 })
            {
                foreach (int seen in new[] { 0, 68, max })
                {
                    string at = "max " + max + ", map_seen_level " + seen;
                    _layout!.Clear();
                    yield return MapMarkersTests.OpenApp(max + 1, seen);
                    GameBootstrap boot = GameBootstrap.Current!;
                    RiverMapScreen map = boot.RiverMapView!;
                    for (int t = 0; t < 200 && (map.CatchingUp || map.CatchUpFading); t++)
                    {
                        map.Tick(16f); // «ponerse al día», hasta el final
                    }

                    Assert.That(map.CatchingUp, Is.False, at + ": «ponerse al día» terminó");

                    // La sonda principal del Mapa (sin el Muestrario): cada línea bajo el tope y JSON válido, todos sus botones y,
                    // si entera no cabe, partida.
                    string probe = AssertUnderTheCap(max + 1, at);
                    string[] ids = AfLayoutLog.ButtonIds(probe);
                    Assert.That(ids, Does.Contain("back").And.Contain("view_world").And.Contain("view_basin").And.Contain("play").And.Contain("samples"),
                        at + ": los botones del Mapa, en sus partes");
                    if (max == 560)
                    {
                        Assert.That(ids, Does.Contain("stop_s56"), at + ": la parada de la canoa");
                    }

                    AssertSplitOnlyIfNeeded(probe, at);

                    // Con el Muestrario abierto, todas las paradas del catálogo (también las futuras): la sonda más larga del Mapa.
                    _layout.Clear();
                    map.OpenSamples();
                    at += " con el Muestrario";
                    probe = AssertUnderTheCap(max + 1, at);
                    ids = AfLayoutLog.ButtonIds(probe);
                    Assert.That(ids, Does.Contain("samples_back").And.Contain("sample_s01").And.Contain("back").And.Contain("play"), at);
                    var samples = new HashSet<string>();
                    foreach (string id in ids)
                    {
                        if (id.StartsWith("sample_s", StringComparison.Ordinal))
                        {
                            Assert.That(samples.Add(id), Is.True, at + ": " + id + " una sola vez");
                        }
                    }

                    Assert.That(samples.Count, Is.GreaterThanOrEqualTo(50), at + ": las muestras de las paradas, en sus partes");
                    Assert.That(AfLayoutLog.Parts(probe).Length, Is.GreaterThan(1), at + ": no cabe en una línea: sale partido");
                    AssertSplitOnlyIfNeeded(probe, at);
                    map.CloseSamples();
                }
            }
        }

        // La sonda sale partida si y solo si, entera (sus botones en una sola línea, como antes de FX7), pasa del tope.
        private static void AssertSplitOnlyIfNeeded(string probe, string at)
        {
            string[] parts = AfLayoutLog.Parts(probe);
            string first = parts[0];
            int cut = first.IndexOf(",\"part\":", StringComparison.Ordinal);
            if (cut < 0)
            {
                cut = first.IndexOf(",\"cuias\":[", StringComparison.Ordinal);
            }

            Assert.That(cut, Is.GreaterThan(0), at + ": la cabecera");
            Assert.That(AfLayoutLog.Cuias(first), Is.Empty, at + ": el Mapa no lleva cuias");
            var buttons = new List<string>();
            foreach (string line in parts)
            {
                foreach (string id in AfLayoutLog.ButtonIds(line))
                {
                    RectInt r = AfLayoutLog.Button(line, id)!.Value;
                    buttons.Add("{\"id\":\"" + id + "\",\"x\":" + r.x.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + r.y.ToString(CultureInfo.InvariantCulture)
                        + ",\"w\":" + r.width.ToString(CultureInfo.InvariantCulture) + ",\"h\":" + r.height.ToString(CultureInfo.InvariantCulture) + "}");
                }
            }

            string whole = DevProbe.LayoutLines(first.Substring(0, cut), string.Empty, buttons.ToArray(), int.MaxValue)[0];
            int bytes = Encoding.UTF8.GetByteCount(whole);
            Assert.That(parts.Length > 1, Is.EqualTo(bytes > DevProbe.MaxLayoutLineBytes),
                at + ": entera mediría " + bytes + " bytes y salió en " + parts.Length + " línea(s)");
            if (parts.Length == 1)
            {
                Assert.That(first, Is.EqualTo(whole), at + ": una sonda que cabe sale como siempre");
            }
        }

        // ============================================================================================ observación 5

        [UnityTest]
        public IEnumerator BasinPlates_ClearOfTheMapControls_At58_AndOtherProgress_WithAndWithoutNorthUp()
        {
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach (int max in new[] { 58, 10, 120, 140, 200, 300 })
            {
                if (max != 58)
                {
                    MapMarkersTests.Reopen(boot, new MemoryProgressStore(max + 1, max, max), max + 1);
                }

                map.SetView(MapView.Basin);
                map.Tick(16f);
                Canvas.ForceUpdateCanvases();
                Assert.That(map.NorthUpShown, Is.True);
                int plates = map.SilhouetteCount;
                if (max == 58)
                {
                    Assert.That(plates, Is.GreaterThanOrEqualTo(2), "con 58, las placas de los mundos futuros (la del 5, Loreto)");
                }

                AssertPlatesClear(map, plates, "max " + max + ", Cuenca");
                AssertPlatesClearOfTheGear(map, plates, "max " + max + ", Cuenca");
                AssertPlatesClearOfTheBuoys(map, plates, "max " + max + ", Cuenca");

                // Con el norte arriba, las placas que salen tampoco lo pisan.
                map.NorthUpButton.onClick.Invoke();
                map.Tick(MapConfig.NorthUpRotateMs);
                map.Tick(16f);
                Assert.That(map.NorthUpTurning, Is.False);
                Canvas.ForceUpdateCanvases();
                AssertPlatesClear(map, map.NorthUpPlateCount, "max " + max + ", norte arriba");
                map.NorthUpButton.onClick.Invoke();
                map.Tick(MapConfig.NorthUpRotateMs);
                map.SetView(MapView.World);
            }
        }

        [UnityTest]
        public IEnumerator BasinPlates_CoverTheLeastFillTheyCan_NoneWhenThereIsRoom()
        {
            yield return MapMarkersTests.OpenApp(201);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach (int max in new[] { 200, 140, 300, 58 })
            {
                if (max != 200)
                {
                    MapMarkersTests.Reopen(boot, new MemoryProgressStore(max + 1, max, max), max + 1);
                }

                map.SetView(MapView.Basin);
                map.Tick(16f);
                Canvas.ForceUpdateCanvases();
                string at = "max " + max + ", Cuenca";
                Assert.That(map.BasinFrameLocal(out Rect frame), Is.True, at + ": la zona visible de la Cuenca");
                int plates = map.SilhouetteCount;
                var boxes = new List<Rect>();
                for (int i = 0; i < plates; i++)
                {
                    int world = map.SilhouetteWorld(i);
                    Rect box = map.SilhouetteLocalBox(i);
                    int hits = map.FillLinesUnderBox(box); // recontadas en la caja donde quedó la placa
                    Debug.Log("FX7 " + at + ": la placa del mundo " + world + " " + box + " tapa " + hits + " línea(s) del lleno (la búsqueda vio "
                        + map.SilhouetteFillMin(i) + " como mínimo)");
                    Assert.That(map.SilhouetteFillHits(i), Is.EqualTo(hits), at + ": la placa del mundo " + world + " está donde la colocó su búsqueda");
                    Assert.That(map.SilhouetteFillMin(i), Is.GreaterThanOrEqualTo(0), at + ": la placa del mundo " + world + " encontró un sitio libre");
                    Assert.That(hits, Is.EqualTo(map.SilhouetteFillMin(i)),
                        at + ": la placa del mundo " + world + " " + box + " tapa " + hits + " línea(s) del lleno; había un sitio que tapaba menos");
                    foreach (Rect other in boxes)
                    {
                        Assert.That(box.Overlaps(other), Is.False, at + ": dos placas se pisan");
                    }

                    boxes.Add(box);

                    // Cerca de su parada, en su región de la Cuenca (revisión de FX7: con max 200 la placa de Ucayali, en Perú, salía
                    // junto a «Brasil»): el centro, a lo sumo a SilhouetteFarMaxDistDp más medio ancho.
                    Vector2 stop = map.Space.ToLocal(map.SilhouetteMapPoint(i));
                    float reach = RiverMapScreen.SilhouetteFarMaxDistDp + (box.width * 0.5f);
                    float distance = Vector2.Distance(box.center, stop);
                    Assert.That(distance, Is.LessThanOrEqualTo(reach + 0.01f),
                        at + ": la placa del mundo " + world + " " + box + " queda a " + distance + " dp de su parada " + stop);

                    // Si tapa el lleno, no le quedaba ningún sitio libre fuera de él a su alcance (con max 200, la del mundo 7, la más
                    // ancha: es el límite aceptado, RiverMapScreen.PlaceSilhouettes).
                    if (hits > 0)
                    {
                        AssertNoFreeRoomOutOfTheFill(map, frame, i, stop, reach, at);
                    }
                }

                if (max == 200)
                {
                    Assert.That(boxes.Count, Is.EqualTo(3), at + ": las placas de los mundos 7, 8 y 9");
                    bool ucayali = false;
                    for (int i = 0; i < plates; i++)
                    {
                        ucayali |= map.SilhouetteWorld(i) == 9;
                    }

                    Assert.That(ucayali, Is.True, at + ": la placa del mundo 9 (Ucayali), con la distancia a su parada comprobada");
                }

                // Lo de siempre sigue valiendo con las placas lejos de su parada.
                AssertPlatesClear(map, plates, at);
                AssertPlatesClearOfTheGear(map, plates, at);
                AssertPlatesClearOfTheBuoys(map, plates, at);
                map.SetView(MapView.World);
            }
        }

        /// <summary>
        /// La colocación de las placas de la Cuenca no depende de los huecos entre meandros junto a su parada (M5.1c): con la
        /// geometría de producción, los países y los ríos más detallados pueden llenarlos. Para cada placa con max 200 y 300, el
        /// test le quita todos los sitios junto a su parada (<see cref="RiverMapScreen.PlateTestNoNearSpotWorld"/>: a su derecha
        /// y a su izquierda, subiendo y bajando hasta <see cref="RiverMapScreen.SilhouetteMaxShiftDp"/>). La placa encuentra sitio
        /// igual, en la rejilla de la zona visible (fuera del lleno o, si no lo hay, el que tapa menos líneas del lleno; antes se
        /// quedaba en el primero junto a la parada, aunque pisara algo), con las reglas de FX7: a su alcance de la parada, sin
        /// pisar otra placa, los controles, la canoa con su equipo ni las boyas, y tapando las líneas del lleno que vio su
        /// búsqueda como mínimo.
        /// </summary>
        [UnityTest]
        public IEnumerator BasinPlates_NoRoomNextToTheStop_FindAGridSpot_WithinReach_ClearOfTheControls()
        {
            yield return MapMarkersTests.OpenApp(201);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach (int max in new[] { 200, 300 })
            {
                if (max != 200)
                {
                    MapMarkersTests.Reopen(boot, new MemoryProgressStore(max + 1, max, max), max + 1);
                }

                RiverMapScreen.PlateTestNoNearSpotWorld = 0;
                map.SetView(MapView.Basin);
                map.Tick(16f);
                int plates = map.SilhouetteCount;
                Assert.That(plates, Is.GreaterThan(0), "max " + max + ": placas de mundos futuros");
                var worlds = new List<int>();
                for (int i = 0; i < plates; i++)
                {
                    worlds.Add(map.SilhouetteWorld(i));
                }

                foreach (int world in worlds)
                {
                    string at = "max " + max + ", Cuenca, la placa del mundo " + world + " sin sitio junto a su parada";
                    RiverMapScreen.PlateTestNoNearSpotWorld = world;
                    map.SetView(MapView.World);
                    map.SetView(MapView.Basin);
                    map.Tick(16f);
                    Canvas.ForceUpdateCanvases();
                    Assert.That(map.SilhouetteCount, Is.EqualTo(plates), at);
                    int i = -1;
                    for (int k = 0; k < plates && i < 0; k++)
                    {
                        i = map.SilhouetteWorld(k) == world ? k : -1;
                    }

                    Assert.That(i, Is.GreaterThanOrEqualTo(0), at);
                    Rect box = map.SilhouetteLocalBox(i);
                    Vector2 stop = map.Space.ToLocal(map.SilhouetteMapPoint(i));
                    int hits = map.FillLinesUnderBox(box);
                    Debug.Log("M5.1c " + at + ": " + box + " para la parada " + stop + ", tapa " + hits + " línea(s) del lleno (la búsqueda vio "
                        + map.SilhouetteFillMin(i) + " como mínimo)");
                    Assert.That(map.SilhouetteFillMin(i), Is.GreaterThanOrEqualTo(0), at + ": encontró un sitio libre en la rejilla");
                    Assert.That(map.SilhouetteFillHits(i), Is.EqualTo(hits), at + ": está donde la colocó su búsqueda");
                    Assert.That(hits, Is.EqualTo(map.SilhouetteFillMin(i)), at + ": tapa las líneas del lleno que vio como mínimo");
                    float reach = RiverMapScreen.SilhouetteFarMaxDistDp + (box.width * 0.5f);
                    float distance = Vector2.Distance(box.center, stop);
                    Assert.That(distance, Is.LessThanOrEqualTo(reach + 0.01f), at + ": " + box + " a " + distance + " dp de su parada " + stop);
                    for (int k = 0; k < plates; k++)
                    {
                        Assert.That(k == i || !box.Overlaps(map.SilhouetteLocalBox(k)), Is.True, at + ": pisa la placa del mundo " + map.SilhouetteWorld(k));
                    }

                    // Lo mínimo es el de la rejilla de la búsqueda (SilhouetteFarStepDp): aquí no se comprueba con la de 2 dp de
                    // AssertNoFreeRoomOutOfTheFill, porque un hueco fuera del lleno más estrecho que el paso puede escapársele (con
                    // max 300, la placa del mundo 8 sin sitio junto a su parada) [P].
                    AssertPlatesClear(map, plates, at);
                    AssertPlatesClearOfTheGear(map, plates, at);
                    AssertPlatesClearOfTheBuoys(map, plates, at);
                }

                RiverMapScreen.PlateTestNoNearSpotWorld = 0;
                map.SetView(MapView.World);
            }
        }

        // Toda caja del tamaño de la placa «plate» dentro de la zona visible de la Cuenca (frame, coordenadas locales del contenido;
        // rejilla de 2 dp) y con el centro a lo sumo a «reach» de su parada «stop» que no pisa ninguna línea del lleno pisa «Norte
        // arriba», «Andes ↑», «Océano Atlántico ↓» o la canoa con su equipo (con MarkerObstacleGapDp de hueco), un país o una placa
        // colocada antes que ella (las de después no le quitan sitio: es lo que había cuando la buscó).
        private static void AssertNoFreeRoomOutOfTheFill(RiverMapScreen map, Rect frame, int plate, Vector2 stop, float reach, string at)
        {
            RectTransform layer = map.LabelLayer!;
            float gap = RiverMapScreen.MarkerObstacleGapDp * layer.lossyScale.x;
            var obstacles = new List<Rect> { Grow(AfLayoutLog.WorldRect((RectTransform)map.NorthUpButton.transform), gap) };
            foreach (TMPro.TextMeshProUGUI? fixedLabel in new[] { map.AndesLabel, map.AtlanticLabel })
            {
                if (fixedLabel != null && fixedLabel.gameObject.activeInHierarchy && !string.IsNullOrEmpty(fixedLabel.text))
                {
                    obstacles.Add(Grow(AfLayoutLog.WorldRect(fixedLabel.rectTransform), gap));
                }
            }

            if (map.HasCanoe)
            {
                obstacles.Add(Grow(AfLayoutLog.WorldRect(map.Canoe.rectTransform), gap));
                if (map.Mast != null && map.Mast.gameObject.activeInHierarchy)
                {
                    obstacles.Add(Grow(AfLayoutLog.WorldRect(map.Mast.rectTransform), gap));
                }

                for (int f = 0; f < map.CanoeFlagCount; f++)
                {
                    if (map.CanoeFlagImage(f).gameObject.activeInHierarchy)
                    {
                        obstacles.Add(Grow(AfLayoutLog.WorldRect(map.CanoeFlagImage(f).rectTransform), gap));
                    }
                }
            }

            for (int i = 0; i < map.Map!.Labels.Count; i++)
            {
                if (map.CountryLabel(i).gameObject.activeInHierarchy && !map.CountryBasinHidden(i))
                {
                    obstacles.Add(LocalToWorld(layer, map.CountryLocalBox(i)));
                }
            }

            // Solo las placas que se colocaron antes que ella: las de después no le quitan ningún sitio.
            for (int i = 0; i < plate; i++)
            {
                obstacles.Add(LocalToWorld(layer, map.SilhouetteLocalBox(i)));
            }

            Vector2 size = map.SilhouetteLocalBox(plate).size;
            float edge = RiverMapScreen.LabelEdgeDp;
            float contentW = map.Space.DpPerUnit * map.Map!.Width;
            float contentH = map.Space.DpPerUnit * map.Map!.Height;
            float xMin = Mathf.Max(frame.xMin, 0f) + edge;
            float xMax = Mathf.Min(frame.xMax, contentW) - edge;
            float yMin = Mathf.Max(frame.yMin, -contentH) + edge;
            float yMax = Mathf.Min(frame.yMax, 0f) - edge;
            int outOfTheFill = 0;
            float yLow = float.PositiveInfinity;
            float yHigh = float.NegativeInfinity;
            for (float y = yMin; y + size.y <= yMax; y += 2f)
            {
                for (float x = xMin; x + size.x <= xMax; x += 2f)
                {
                    var box = new Rect(x, y, size.x, size.y);
                    if (Vector2.Distance(box.center, stop) > reach || map.FillLinesUnderBox(box) > 0)
                    {
                        continue;
                    }

                    outOfTheFill++;
                    yLow = Mathf.Min(yLow, box.yMin);
                    yHigh = Mathf.Max(yHigh, box.yMax);
                    Rect w = LocalToWorld(layer, box);
                    bool blocked = false;
                    foreach (Rect o in obstacles)
                    {
                        blocked |= w.Overlaps(o);
                    }

                    Assert.That(blocked, Is.True, at + ": la placa " + plate + " cabría fuera del lleno en " + box + " sin pisar nada");
                }
            }

            Debug.Log("FX7 " + at + ": la placa " + plate + " tiene " + outOfTheFill + " sitios fuera del lleno (y de " + yLow + " a " + yHigh + "), todos ocupados");
        }

        // Caja local del contenido (dp, y hacia arriba desde su esquina superior izquierda) en coordenadas del mundo.
        private static Rect LocalToWorld(RectTransform layer, Rect box)
        {
            Rect r = layer.rect;
            Vector3 a = layer.TransformPoint(new Vector3(r.xMin + box.xMin, r.yMax + box.yMin, 0f));
            Vector3 b = layer.TransformPoint(new Vector3(r.xMin + box.xMax, r.yMax + box.yMax, 0f));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static Rect Grow(Rect r, float by)
        {
            return Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);
        }

        [UnityTest]
        public IEnumerator BasinCountries_NotUnderTheMapControls_At140_AndOtherProgress()
        {
            // La regla de ocultar un país en la Cuenca sin girar [P], también en casos que estos datos no dan (por ejemplo, un país
            // cuyo punto no queda bajo ningún control, pero algún sitio de su búsqueda sí; revisión de FX7).
            float none = float.NegativeInfinity;
            Assert.That(RiverMapScreen.CountryBasinFallback(false, none, none), Is.False, "sin ningún control de por medio, como antes de FX7");
            Assert.That(RiverMapScreen.CountryBasinFallback(true, 2f, 0f), Is.False, "le queda un sitio fuera de los controles que no pisa el río");
            Assert.That(RiverMapScreen.CountryBasinFallback(true, -4f, -2f), Is.False,
                "fuera de los controles solo pisa el río, y bajo ellos también: el de más hueco fuera de ellos, como antes");
            Assert.That(RiverMapScreen.CountryBasinFallback(true, -4f, 0f), Is.True, "el control le quitó el único sitio que no pisaba el río");
            Assert.That(RiverMapScreen.CountryBasinFallback(true, none, -2f), Is.True, "los controles y los otros rótulos le quitan todos los sitios");

            yield return MapMarkersTests.OpenApp(141);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            foreach (int max in new[] { 140, 58, 200, 300 })
            {
                if (max != 140)
                {
                    MapMarkersTests.Reopen(boot, new MemoryProgressStore(max + 1, max, max), max + 1);
                }

                map.SetView(MapView.Basin);
                map.Tick(16f);
                Canvas.ForceUpdateCanvases();
                string at = "max " + max + ", Cuenca";
                Rect button = AfLayoutLog.WorldRect((RectTransform)map.NorthUpButton.transform);
                bool canoeShown = map.BackToCanoeShown;
                Rect canoe = canoeShown ? AfLayoutLog.WorldRect((RectTransform)map.BackToCanoeButton.transform) : default;
                Rect andes = AfLayoutLog.WorldRect(map.AndesLabel!.rectTransform);
                int shown = 0;
                for (int i = 0; i < map.Map!.Labels.Count; i++)
                {
                    TMPro.TextMeshProUGUI label = map.CountryLabel(i);
                    if (!label.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    // Lo que vio su búsqueda: el mejor hueco con el río fuera de los controles y de los otros rótulos (free) y bajo
                    // un control (under, como mucho 0).
                    map.CountryBasinSearch(i, out float free, out float under);
                    bool fallback = RiverMapScreen.CountryBasinFallback(true, free, under);
                    Debug.Log("FX7 " + at + ": «" + label.text + "» " + (map.CountryBasinHidden(i) ? "no sale" : "sale") + "; hueco con el río fuera de los controles "
                        + free.ToString("0.0", CultureInfo.InvariantCulture) + " dp, bajo ellos " + under.ToString("0.0", CultureInfo.InvariantCulture) + " dp");
                    if (map.CountryBasinHidden(i))
                    {
                        // Solo si no le quedaba ningún sitio libre de controles, de otros rótulos y del río, y los controles le
                        // quitaron todos o uno que no pisaba el río (revisión de FX7: con un sitio fuera de los controles que tampoco
                        // pisa el río, o si bajo ellos también lo pisaba, sale en su región).
                        Assert.That(free, Is.LessThan(0f), at + ": «" + label.text + "» se oculta aunque tenía un sitio libre de controles y del río");
                        Assert.That(fallback, Is.True, at + ": «" + label.text + "» se oculta aunque fuera de los controles cabía como bajo ellos");
                        Assert.That(label.canvasRenderer.GetAlpha(), Is.EqualTo(0f), at + ": «" + label.text + "» no sale en la Cuenca");
                        continue;
                    }

                    if (fallback)
                    {
                        // Salió corrido a la izquierda del control que pisaba en su punto: sin pisar el río.
                        float gap = RiverMapScreen.LineClearance(map.Map!, map.Space, MapView.Basin, map.CountryLocalBox(i), 0f);
                        Assert.That(gap, Is.GreaterThanOrEqualTo(0f), at + ": «" + label.text + "», corrido a la izquierda del control, pisa el río");
                    }

                    shown++;
                    Assert.That(label.canvasRenderer.GetAlpha(), Is.EqualTo(1f), at + ": «" + label.text + "» sale");
                    Rect box = AfLayoutLog.WorldRect(label.rectTransform);
                    Assert.That(box.Overlaps(button), Is.False, at + ": «" + label.text + "» " + box + " queda bajo «Norte arriba» " + button);
                    Assert.That(box.Overlaps(andes), Is.False, at + ": «" + label.text + "» " + box + " pisa «Andes ↑» " + andes);
                    if (canoeShown)
                    {
                        Assert.That(box.Overlaps(canoe), Is.False, at + ": «" + label.text + "» " + box + " queda bajo «Volver a la canoa» " + canoe);
                    }
                }

                Assert.That(shown, Is.GreaterThanOrEqualTo(map.CountryLabelCount - 1), at + ": a lo sumo un país se queda sin sitio");

                // Con el norte arriba, un país que no salía en la Cuenca vuelve (si cabe con el norte arriba).
                map.NorthUpButton.onClick.Invoke();
                map.Tick(MapConfig.NorthUpRotateMs);
                map.Tick(16f);
                for (int i = 0; i < map.Map!.Labels.Count; i++)
                {
                    TMPro.TextMeshProUGUI label = map.CountryLabel(i);
                    if (label.gameObject.activeInHierarchy && map.CountryBasinHidden(i) && !map.NorthUpCountryHidden(i))
                    {
                        Assert.That(label.canvasRenderer.GetAlpha(), Is.EqualTo(1f), at + ", norte arriba: «" + label.text + "» vuelve");
                    }
                }

                map.NorthUpButton.onClick.Invoke();
                map.Tick(MapConfig.NorthUpRotateMs);
                map.SetView(MapView.World);
            }
        }

        // Ninguna de las primeras «count» placas (las que se ven) pisa «Norte arriba» ni, si sale, «Volver a la canoa», en pantalla.
        private static void AssertPlatesClear(RiverMapScreen map, int count, string at)
        {
            Rect button = AfLayoutLog.WorldRect((RectTransform)map.NorthUpButton.transform);
            bool canoeShown = map.BackToCanoeShown;
            Rect canoe = canoeShown ? AfLayoutLog.WorldRect((RectTransform)map.BackToCanoeButton.transform) : default;
            for (int i = 0; i < count; i++)
            {
                RectTransform plate = map.SilhouettePlate(i).rectTransform;
                if (!plate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect box = AfLayoutLog.WorldRect(plate);
                Assert.That(box.Overlaps(button), Is.False, at + ": la placa «" + map.SilhouettePlate(i).transform.parent.name + "» " + box + " pisa «Norte arriba» " + button);
                if (canoeShown)
                {
                    Assert.That(box.Overlaps(canoe), Is.False, at + ": la placa " + i + " " + box + " pisa «Volver a la canoa» " + canoe);
                }
            }
        }

        // En la Cuenca sin girar, ninguna placa se cruza con la fila de boyas del frente (la caja de las boyas que se ven, con su
        // radio; revisión de FX7: con max 200 la placa del mundo 9 la rozaba).
        private static void AssertPlatesClearOfTheBuoys(RiverMapScreen map, int count, string at)
        {
            RiverMapScreen.BuoyGraphic? buoys = map.Buoys;
            if (map.BuoyCount == 0 || buoys == null)
            {
                return;
            }

            RectTransform rt = buoys.rectTransform;
            float r = buoys.Radius * rt.lossyScale.x;
            Rect row = default;
            for (int i = 0; i < map.BuoyCount; i++)
            {
                Vector2 p = rt.TransformPoint(map.BuoyLocalPoint(i));
                var box = Rect.MinMaxRect(p.x - r, p.y - r, p.x + r, p.y + r);
                row = i == 0 ? box : Rect.MinMaxRect(Mathf.Min(row.xMin, box.xMin), Mathf.Min(row.yMin, box.yMin), Mathf.Max(row.xMax, box.xMax), Mathf.Max(row.yMax, box.yMax));
            }

            for (int i = 0; i < count; i++)
            {
                RectTransform plate = map.SilhouettePlate(i).rectTransform;
                if (!plate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect box = AfLayoutLog.WorldRect(plate);
                Assert.That(box.Overlaps(row), Is.False, at + ": la placa «" + plate.parent.name + "» " + box + " se cruza con las boyas " + row);
            }
        }

        // En la Cuenca sin girar, ninguna placa queda bajo la canoa, su mástil, sus banderas o el guía (las capturas de FX7 con max
        // 140 y 200 tenían una placa bajo las banderas).
        private static void AssertPlatesClearOfTheGear(RiverMapScreen map, int count, string at)
        {
            if (!map.HasCanoe)
            {
                return;
            }

            var figures = new List<(string Name, Rect Box)> { ("la canoa", AfLayoutLog.WorldRect(map.Canoe.rectTransform)) };
            if (map.Mast != null && map.Mast.gameObject.activeInHierarchy)
            {
                figures.Add(("el mástil", AfLayoutLog.WorldRect(map.Mast.rectTransform)));
            }

            for (int f = 0; f < map.CanoeFlagCount; f++)
            {
                if (map.CanoeFlagImage(f).gameObject.activeInHierarchy)
                {
                    figures.Add(("la bandera " + f, AfLayoutLog.WorldRect(map.CanoeFlagImage(f).rectTransform)));
                }
            }

            if (map.GuideImage != null && map.GuideImage.gameObject.activeInHierarchy)
            {
                figures.Add(("el guía", AfLayoutLog.WorldRect(map.GuideImage.rectTransform)));
            }

            for (int i = 0; i < count; i++)
            {
                RectTransform plate = map.SilhouettePlate(i).rectTransform;
                if (!plate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect box = AfLayoutLog.WorldRect(plate);
                foreach ((string name, Rect figure) in figures)
                {
                    Assert.That(box.Overlaps(figure), Is.False, at + ": la placa «" + plate.parent.name + "» " + box + " queda bajo " + name + " " + figure);
                }
            }
        }

        // La última sonda river_map del nivel: cada línea, a lo sumo MaxLayoutLineBytes bytes y JSON válido.
        private string AssertUnderTheCap(int level, string at)
        {
            string? probe = _layout!.LastOf("river_map", level);
            Assert.That(probe, Is.Not.Null, at + ": AF_LAYOUT del Mapa");
            int total = 0;
            foreach (string line in AfLayoutLog.Parts(probe!))
            {
                int bytes = Encoding.UTF8.GetByteCount(line);
                total += bytes;
                Assert.That(bytes, Is.LessThanOrEqualTo(DevProbe.MaxLayoutLineBytes), at + ": una línea de " + bytes + " bytes");
                AssertJson(line);
            }

            Debug.Log("FX7 " + at + ": " + AfLayoutLog.Parts(probe!).Length + " línea(s), " + total + " bytes, "
                + AfLayoutLog.ButtonIds(probe!).Length + " botones");
            return probe!;
        }

        private static string[] Buttons(int count)
        {
            var buttons = new string[count];
            for (int i = 0; i < count; i++)
            {
                buttons[i] = "{\"id\":\"stop_s" + i.ToString("000", CultureInfo.InvariantCulture) + "\",\"x\":1234,\"y\":2048,\"w\":144,\"h\":144}";
            }

            return buttons;
        }

        // La línea es «AF_LAYOUT » y un objeto JSON válido, entero (sin nada detrás).
        private static void AssertJson(string line)
        {
            const string prefix = "AF_LAYOUT ";
            Assert.That(line.StartsWith(prefix, StringComparison.Ordinal), Is.True, line);
            int at = prefix.Length;
            Assert.That(JsonValue(line, ref at), Is.True, "JSON válido: " + line);
            Assert.That(at, Is.EqualTo(line.Length), "nada detrás del objeto: " + line);
        }

        /// <summary>
        /// La línea es una etiqueta de la sonda (<c>AF_STATE </c>, <c>AF_LAYOUT </c>, <c>AF_PERF </c>…) y un objeto JSON válido,
        /// entero (tarea FX10).
        /// </summary>
        internal static void AssertProbeJson(string line)
        {
            int at = line.IndexOf(" {", StringComparison.Ordinal) + 1;
            Assert.That(at > 0 && line.StartsWith("AF_", StringComparison.Ordinal), Is.True, "una línea de la sonda: " + line);
            Assert.That(JsonValue(line, ref at), Is.True, "JSON válido: " + line);
            Assert.That(at, Is.EqualTo(line.Length), "nada detrás del objeto: " + line);
        }

        // Un validador mínimo de JSON (objetos, listas, cadenas sin escapes raros, números, true, false y null).
        private static bool JsonValue(string s, ref int i)
        {
            if (i >= s.Length)
            {
                return false;
            }

            char c = s[i];
            if (c == '{' || c == '[')
            {
                bool obj = c == '{';
                char close = obj ? '}' : ']';
                i++;
                if (i < s.Length && s[i] == close)
                {
                    i++;
                    return true;
                }

                while (true)
                {
                    if (obj)
                    {
                        if (i >= s.Length || s[i] != '"' || !JsonString(s, ref i) || i >= s.Length || s[i] != ':')
                        {
                            return false;
                        }

                        i++;
                    }

                    if (!JsonValue(s, ref i) || i >= s.Length)
                    {
                        return false;
                    }

                    if (s[i] == ',')
                    {
                        i++;
                        continue;
                    }

                    if (s[i] != close)
                    {
                        return false;
                    }

                    i++;
                    return true;
                }
            }

            if (c == '"')
            {
                return JsonString(s, ref i);
            }

            foreach (string word in new[] { "true", "false", "null" })
            {
                if (string.CompareOrdinal(s, i, word, 0, word.Length) == 0)
                {
                    i += word.Length;
                    return true;
                }
            }

            int start = i;
            if (s[i] == '-')
            {
                i++;
            }

            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+' || s[i] == '-'))
            {
                i++;
            }

            return i > start && char.IsDigit(s[i - 1]);
        }

        private static bool JsonString(string s, ref int i)
        {
            i++; // la comilla de apertura
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '\\')
                {
                    i++;
                }
                else if (c == '"')
                {
                    return true;
                }
            }

            return false;
        }
    }
}
