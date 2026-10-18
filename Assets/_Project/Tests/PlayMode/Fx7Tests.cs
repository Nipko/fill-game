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
    /// (logcat corta cada entrada en 4.068): con el Mapa a 560 y a 700, también con el Muestrario abierto, la sonda sale partida
    /// en líneas de JSON válido con <c>"part":k,"parts":n</c> y, juntas, traen todos sus botones;</description></item>
    /// <item><description>observación 5: en la vista Cuenca con max 58, ninguna placa de mundo futuro pisa «Norte arriba» (la del
    /// mundo 5, «Loreto · Nivel 141», lo rozaba), ni «Volver a la canoa» si sale, con la vista de siempre y con el norte
    /// arriba; y con otros progresos. En la Cuenca sin girar tampoco quedan bajo la canoa, su mástil, sus banderas ni el guía,
    /// como ya pasaba con el norte arriba.</description></item>
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

            // Un tope pequeño: cada parte se llena hasta donde cabe, y un botón que solo ya no cabe va solo.
            string[] tight = DevProbe.LayoutLines(head, string.Empty, Buttons(5), head.Length + 80);
            Assert.That(tight.Length, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator MapLayout_At560And700_NoLineOverTheCap_ValidJson_AllTheButtons()
        {
            // Como con af_max_level 560 y 700 en el teléfono: el Mapa abre en el mundo 9 (la s56) y en la última parada.
            yield return MapMarkersTests.OpenApp(11);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            bool split = false;
            foreach (int max in new[] { 560, 700 })
            {
                _layout!.Clear();
                MapMarkersTests.Reopen(boot, new MemoryProgressStore(max + 1, max, max), max + 1);
                string probe = AssertUnderTheCap(max + 1, "max " + max);
                string[] ids = AfLayoutLog.ButtonIds(probe);
                Assert.That(ids, Does.Contain("back").And.Contain("view_world").And.Contain("view_basin").And.Contain("play").And.Contain("samples"),
                    "max " + max + ": los botones del Mapa, en sus partes");
                split |= AfLayoutLog.Parts(probe).Length > 1;

                // Con el Muestrario abierto, todas las paradas del catálogo (también las futuras): la sonda más larga del Mapa.
                _layout.Clear();
                map.OpenSamples();
                probe = AssertUnderTheCap(max + 1, "max " + max + " con el Muestrario");
                ids = AfLayoutLog.ButtonIds(probe);
                Assert.That(ids, Does.Contain("samples_back").And.Contain("sample_s01"), "max " + max + ": el Muestrario");
                var samples = new HashSet<string>();
                foreach (string id in ids)
                {
                    if (id.StartsWith("sample_s", StringComparison.Ordinal))
                    {
                        Assert.That(samples.Add(id), Is.True, "max " + max + ": " + id + " una sola vez");
                    }
                }

                Assert.That(samples.Count, Is.GreaterThanOrEqualTo(50), "max " + max + ": las muestras de las paradas, en sus partes");
                Assert.That(AfLayoutLog.Parts(probe).Length, Is.GreaterThan(1), "max " + max + ": el Muestrario no cabe en una línea: sale partido");
                split = true;
                map.CloseSamples();
            }

            Assert.That(split, Is.True);
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
