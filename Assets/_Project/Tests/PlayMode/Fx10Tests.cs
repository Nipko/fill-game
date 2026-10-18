#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Arreglos de la prueba de la ola 6 de la tanda 4 en el teléfono (<c>docs/dev/traspaso/pruebas-telefono-2026-10-18-tanda4-ola6.md</c>;
    /// tarea FX10). La observación 2 (el <c>dev.apk</c> que crece con el build incremental) se prueba en
    /// <c>AF.Tests.EditMode.BuildDevTests</c>; aquí:
    /// <list type="bullet">
    /// <item><description>observación 1: logcat corta cada mensaje en unos 1.024 caracteres, así que ninguna línea de la sonda
    /// (<c>AF_STATE</c>, <c>AF_LAYOUT</c> ni <c>AF_PERF</c>) pasa de <see cref="DevProbe.MaxLineBytes"/> bytes de UTF-8, contada
    /// entera: en el Mapa con <c>af_max_level</c> 560 y 700, con y sin el Muestrario; en Ajustes con «Frutas del río» y la
    /// ficha abierta; y en el nivel y la victoria del 300 (las líneas del tablero, las más largas de <c>AF_STATE</c>). Cada
    /// línea es JSON válido, las partes salen seguidas y numeradas, con la misma cabecera, y juntas dan la línea entera
    /// (<see cref="Join"/>);</description></item>
    /// <item><description>observación 3: con el Mapa a tamaño de teléfono (360 × 780 dp), desde la pausa del 41 (vista Mundo; con
    /// <c>map_seen_level</c> por debajo de 40, «ponerse al día» pasa la boya del 40 y la placa del mundo 4, «Trapecio»,
    /// heredaba el sitio de la del 3, cortada por la derecha) y en el Mapa del fin de contenido del 300 (la del 8, «Madidi»,
    /// heredaba el de la del 7, cortada por la izquierda y bajo la ventanita de casa), ninguna placa de mundo futuro que se vea
    /// se sale de la zona visible ni pisa la ventanita, al abrir, en cada fotograma del viaje y al acabar, y tampoco en la
    /// Cuenca ni al volver a la vista Mundo.</description></item>
    /// </list>
    /// No necesitan gráficos. Las capturas son las de <see cref="Fx10SnapshotTests"/>.
    /// </summary>
    public sealed class Fx10Tests
    {
        /// <summary>Pantalla de teléfono de las pruebas en el SM-S938B (1080 × 2340 px a 3×: 360 × 780 dp, con su zona segura).</summary>
        internal static readonly Vector4 Phone = new Vector4(1080f, 2340f, 96f, 48f);

        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            GameBootstrap.ContentEndMapOverride = null;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        // ============================================================================================ observación 1

        [Test]
        public void ProbeLines_ALineThatFits_AsAlways_OneThatDoesNot_InPartsUnderTheCap_ThatJoinToIt()
        {
            const int cap = DevProbe.MaxLineBytes;
            Assert.That(cap, Is.LessThanOrEqualTo(1000), "por debajo de los ~1.024 caracteres en que logcat corta un mensaje");
            Assert.That(DevProbe.MaxLayoutLineBytes, Is.EqualTo(cap), "el mismo tope para AF_LAYOUT");

            const string small = "AF_STATE {\"event\":\"pour\",\"t\":12,\"level\":3,\"cuias\":[[1,2],[3]]}";
            Assert.That(DevProbe.ProbeLines(small, cap), Is.EqualTo(new[] { small }), "una línea que cabe sale como siempre");

            // Una línea con cadenas que llevan comas, llaves, comillas escapadas y tildes (dos bytes cada una) y listas anidadas.
            var sb = new StringBuilder("AF_STATE {\"event\":\"load\",\"t\":123456,\"level\":300,\"pour_mode\":\"run\"");
            for (int i = 0; i < 40; i++)
            {
                sb.Append(",\"k").Append(i.ToString(CultureInfo.InvariantCulture)).Append("\":");
                switch (i % 4)
                {
                    case 0:
                        sb.Append("[[1,2,3],[4,5],[6]]");
                        break;
                    case 1:
                        sb.Append("\"Açaí, {Tapajós}: \\\"río\\\" ñ\"");
                        break;
                    case 2:
                        sb.Append("{\"on\":true,\"opacity\":0.20,\"x\":[1,{\"y\":\"a,b\"}]}");
                        break;
                    default:
                        sb.Append("-17");
                        break;
                }
            }

            sb.Append('}');
            string whole = sb.ToString();
            Fx7Tests.AssertProbeJson(whole);
            Assert.That(Encoding.UTF8.GetByteCount(whole), Is.GreaterThan(cap));
            string[] parts = DevProbe.ProbeLines(whole, cap);
            AssertParts(parts, cap, "AF_STATE {\"event\":\"load\",\"t\":123456,\"level\":300", "40 miembros");
            Assert.That(Join(parts), Is.EqualTo(whole), "juntas, la línea entera");

            // En bytes, no en caracteres: una línea de menos de 960 caracteres que con las tildes pasa de 960 bytes, partida.
            string accents = "AF_STATE {\"event\":\"fact\",\"t\":1,\"a\":\"" + new string('í', 300) + "\",\"b\":\"" + new string('ñ', 300) + "\"}";
            Assert.That(accents.Length, Is.LessThan(cap));
            Assert.That(Encoding.UTF8.GetByteCount(accents), Is.GreaterThan(cap));
            string[] accentParts = DevProbe.ProbeLines(accents, cap);
            AssertParts(accentParts, cap, "AF_STATE {\"event\":\"fact\",\"t\":1", "con tildes");
            Assert.That(Join(accentParts), Is.EqualTo(accents));

            // La excepción documentada: un miembro que ni solo cabe va solo en su parte (y esa parte pasa del tope).
            string huge = "AF_PERF {\"event\":\"x\",\"a\":1,\"big\":\"" + new string('z', 2000) + "\",\"b\":2}";
            string[] hugeParts = DevProbe.ProbeLines(huge, cap);
            Assert.That(hugeParts.Length, Is.EqualTo(3));
            Assert.That(hugeParts[1], Does.Contain("\"big\":").And.Not.Contain("\"a\":").And.Not.Contain("\"b\":"));
            Assert.That(Encoding.UTF8.GetByteCount(hugeParts[0]), Is.LessThanOrEqualTo(cap));
            Assert.That(Encoding.UTF8.GetByteCount(hugeParts[2]), Is.LessThanOrEqualTo(cap));
            Assert.That(Join(hugeParts), Is.EqualTo(huge));

            // AF_LAYOUT, con el esquema de FX7 y el tope nuevo: los 20 botones de un Mapa con 560 (unos 1.000 bytes), partidos.
            const string head = "AF_LAYOUT {\"screen\":\"river_map\",\"level\":561,\"sw\":1080,\"sh\":2340";
            var buttons = new string[20];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = "{\"id\":\"stop_s" + i.ToString("000", CultureInfo.InvariantCulture) + "\",\"x\":1234,\"y\":2048,\"w\":144,\"h\":144}";
            }

            string[] layout = DevProbe.LayoutLines(head, string.Empty, buttons, DevProbe.MaxLayoutLineBytes);
            Assert.That(layout.Length, Is.GreaterThan(1));
            foreach (string line in layout)
            {
                Assert.That(Encoding.UTF8.GetByteCount(line), Is.LessThanOrEqualTo(cap));
                Fx7Tests.AssertProbeJson(line);
            }
        }

        [UnityTest]
        public IEnumerator Map_At560And700_WithAndWithoutTheSamples_NoProbeLineOverTheCap()
        {
            foreach (int max in new[] { 560, 700 })
            {
                string at = "Mapa con " + max;
                _lines.Clear();
                yield return MapMarkersTests.OpenApp(max + 1, max);
                RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
                map.Tick(16f);
                AssertTheLines(at, "river_map");
                _lines.Clear();
                map.OpenSamples();
                map.Tick(16f);
                AssertTheLines(at + " con el Muestrario", "river_map");
                map.CloseSamples();
            }
        }

        [UnityTest]
        public IEnumerator Settings_WithTheFruitsOfTheRiver_AndTheSheet_NoProbeLineOverTheCap()
        {
            foreach (int next in new[] { 59, 561 })
            {
                _lines.Clear();
                Profile profile = SettingsScreenTests.ProfileAt(next);
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                Assert.That(boot.PressSettings(), Is.True);
                SettingsScreen settings = boot.SettingsView!;
                Assert.That(settings.FruitCount, Is.GreaterThan(10), "las fichas de «Frutas del río», con sus botones fruit_*");
                string at = "Ajustes con " + (next - 1);
                AssertTheLines(at, "settings");
                string[] ids = AfLayoutLog.ButtonIds(LastProbe("settings"));
                int fruits = 0;
                foreach (string id in ids)
                {
                    fruits += id.StartsWith("fruit_", StringComparison.Ordinal) ? 1 : 0;
                }

                Assert.That(fruits, Is.EqualTo(settings.FruitCount), at + ": un botón fruit_* por ficha, en sus partes");
                settings.FruitButton("acai")!.onClick.Invoke();
                PlayModeHelpers.Step(boot, 600);
                AssertTheLines(at + " con la ficha del açaí", null);
            }
        }

        [UnityTest]
        public IEnumerator Level300_AndItsVictory_NoProbeLineOverTheCap_TheBoardLinePartsJoinToTheLine()
        {
            GameScreen.SimulatedScreen = Phone;
            yield return FactLineTests.Win(300);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TickExact(boot, 3000); // el panel termina de subir y la ranura se llena: AF_LAYOUT victory
            AssertTheLines("el 300 y su victoria", "victory");
            int pours = 0;
            foreach (string line in _lines)
            {
                pours += line.StartsWith("AF_STATE {\"event\":\"pour\"", StringComparison.Ordinal) ? 1 : 0;
            }

            Assert.That(pours, Is.GreaterThan(0), "las líneas de los vertidos del 300");

            // La línea del tablero con los datos reales del 300 (la más larga de AF_STATE), con el tope de verdad y con topes más
            // pequeños: las partes, bajo el tope, juntas dan la línea entera.
            var probe = new DevProbe();
            string? whole = probe.FormatState("load", boot.Flow!, int.MinValue, null);
            Assert.That(whole, Is.Not.Null);
            Debug.Log("FX10 la línea load del 300 mide " + Encoding.UTF8.GetByteCount(whole!) + " bytes");
            foreach (int cap in new[] { DevProbe.MaxLineBytes, 400, 250 })
            {
                string[] parts = DevProbe.ProbeLines(whole!, cap);
                if (parts.Length > 1)
                {
                    AssertParts(parts, cap, "AF_STATE {\"event\":\"load\",\"t\":", "load del 300 con " + cap);
                }

                Assert.That(Join(parts), Is.EqualTo(whole), "tope " + cap + ": juntas, la línea entera");
            }
        }

        // Todas las líneas de la sonda capturadas: a lo sumo MaxLineBytes, JSON válido y, las partidas, seguidas, numeradas de 1
        // a n y con la misma cabecera. Si screen no es null, hay una AF_LAYOUT de esa pantalla.
        private void AssertTheLines(string at, string? screen)
        {
            Assert.That(_lines.Count, Is.GreaterThan(0), at + ": líneas de la sonda");
            int longest = 0;
            int split = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                string line = _lines[i];
                int bytes = Encoding.UTF8.GetByteCount(line);
                longest = Mathf.Max(longest, bytes);
                Assert.That(bytes, Is.LessThanOrEqualTo(DevProbe.MaxLineBytes), at + ": una línea de " + bytes + " bytes: " + line);
                Fx7Tests.AssertProbeJson(line);
                (int part, int parts) = PartOf(line);
                if (part == 1)
                {
                    split++;
                    Assert.That(i + parts, Is.LessThanOrEqualTo(_lines.Count), at + ": las " + parts + " partes, capturadas: " + line);
                    string head = HeadOf(line);
                    for (int k = 1; k < parts; k++)
                    {
                        string next = _lines[i + k];
                        Assert.That(PartOf(next), Is.EqualTo((k + 1, parts)), at + ": las partes, seguidas: " + next);
                        Assert.That(HeadOf(next), Is.EqualTo(head), at + ": con la misma cabecera: " + next);
                    }
                }
            }

            if (screen != null)
            {
                Assert.That(LastProbe(screen), Is.Not.Null, at + ": AF_LAYOUT " + screen);
            }

            Debug.Log("FX10 " + at + ": " + _lines.Count + " líneas de la sonda, la más larga de " + longest + " bytes, " + split + " partidas");
        }

        // La última sonda AF_LAYOUT de la pantalla (sus partes, una por renglón, para leer sus botones), o null.
        private string? LastProbe(string screen)
        {
            string tag = "AF_LAYOUT {\"screen\":\"" + screen + "\",";
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (!_lines[i].StartsWith(tag, StringComparison.Ordinal))
                {
                    continue;
                }

                (int part, int parts) = PartOf(_lines[i]);
                if (parts == 0)
                {
                    return _lines[i];
                }

                Assert.That(part, Is.EqualTo(parts), "la última es la última parte");
                return string.Join("\n", _lines.GetRange(i - parts + 1, parts));
            }

            return null;
        }

        // Partes de una línea partida: bajo el tope, JSON válido, con la cabecera y "part":k,"parts":n.
        private static void AssertParts(string[] parts, int cap, string head, string at)
        {
            Assert.That(parts.Length, Is.GreaterThan(1), at + ": partida");
            for (int k = 0; k < parts.Length; k++)
            {
                string line = parts[k];
                Assert.That(Encoding.UTF8.GetByteCount(line), Is.LessThanOrEqualTo(cap), at + ", parte " + (k + 1) + ": " + line);
                Assert.That(line.StartsWith(head, StringComparison.Ordinal), Is.True, at + ": la cabecera: " + line);
                Assert.That(PartOf(line), Is.EqualTo((k + 1, parts.Length)), at + ": " + line);
                Fx7Tests.AssertProbeJson(line);
            }
        }

        /// <summary>
        /// Junta las partes de una línea <c>AF_STATE</c>/<c>AF_PERF</c> partida (DevProbe.ProbeLines): la cabecera (lo que va antes
        /// de <c>"part"</c>) y, de cada parte en orden, lo que va detrás de <c>"parts":n</c>, sin la llave de cierre. Una sola
        /// línea sin partes se devuelve tal cual.
        /// </summary>
        internal static string Join(string[] parts)
        {
            if (parts.Length == 1 && PartOf(parts[0]).Parts == 0)
            {
                return parts[0];
            }

            var sb = new StringBuilder(HeadOf(parts[0]));
            foreach (string line in parts)
            {
                int at = line.IndexOf(",\"parts\":", StringComparison.Ordinal) + ",\"parts\":".Length;
                while (at < line.Length && char.IsDigit(line[at]))
                {
                    at++;
                }

                sb.Append(line, at, line.Length - 1 - at);
            }

            return sb.Append('}').ToString();
        }

        private static string HeadOf(string line)
        {
            int at = line.IndexOf(",\"part\":", StringComparison.Ordinal);
            return at < 0 ? line : line.Substring(0, at);
        }

        private static (int Part, int Parts) PartOf(string line)
        {
            int at = line.IndexOf(",\"part\":", StringComparison.Ordinal);
            if (at < 0)
            {
                return (0, 0);
            }

            at += ",\"part\":".Length;
            int part = 0;
            while (char.IsDigit(line[at]))
            {
                part = (part * 10) + (line[at++] - '0');
            }

            Assert.That(string.CompareOrdinal(line, at, ",\"parts\":", 0, 9), Is.EqualTo(0), line);
            at += 9;
            int parts = 0;
            while (char.IsDigit(line[at]))
            {
                parts = (parts * 10) + (line[at++] - '0');
            }

            return (part, parts);
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith("AF_STATE ", StringComparison.Ordinal) || condition.StartsWith("AF_LAYOUT ", StringComparison.Ordinal)
                || condition.StartsWith("AF_PERF ", StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }

        // ============================================================================================ observación 3

        [UnityTest]
        public IEnumerator PauseMap_At41_ThePlatesWholeInTheView_ClearOfTheHomeWindow_AlsoWhileCatchingUp()
        {
            MapArtRule.ForceProvisional = true; // la casa con su arte: la ventanita sale (como en el teléfono)
            foreach ((int max, int seen) in new[] { (40, 30), (40, 0), (40, 40), (39, 39), (20, 20), (58, 58) })
            {
                string at = "pausa del 41 con max " + max + " y map_seen_level " + seen;
                yield return OpenFromPause41(max, seen);
                RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
                Assert.That(map.Source, Is.EqualTo(MapOpenSource.Pause));
                Assert.That(map.View, Is.EqualTo(MapView.World));
                CatchUpAndCheck(map, at);
                if (max == 40)
                {
                    Assert.That(Worlds(map), Does.Contain(4), at + ": la placa del mundo 4 (Trapecio)");
                }

                CheckTheViews(map, at);
            }
        }

        [UnityTest]
        public IEnumerator ContentEndMap_After300_ThePlatesWholeInTheView_ClearOfTheHomeWindow_AlsoOnTheJourney()
        {
            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Phone;
            yield return OpenContentEnd();
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.ContentEnd));
            Assert.That(map.CatchingUp, Is.True, "el viaje del último tramo");
            CatchUpAndCheck(map, "fin de contenido");
            Assert.That(Worlds(map), Does.Contain(8), "la placa del mundo 8 (Madidi)");
            Assert.That(map.HomeWindowShown, Is.True, "la ventanita de casa, a la vista (como en el teléfono)");
            CheckTheViews(map, "fin de contenido");
        }

        /// <summary>El 41 en juego (perfil con max y map_seen_level dados), en pausa y con el Mapa abierto desde ella.</summary>
        internal static IEnumerator OpenFromPause41(int max, int seen)
        {
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = Phone;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500, 3));
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, max, seen));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            for (int i = 0; i < 400 && boot.Flow!.Phase != FlowPhase.Playing; i++)
            {
                boot.TapAnywhere();
                PlayModeHelpers.Step(boot, 50);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "el 41 en juego");
            boot.View!.PauseButton.onClick.Invoke();
            boot.View.Pause.MapButton.onClick.Invoke();
            Assert.That(boot.RiverMapView!.Visible, Is.True, "el Mapa, desde la pausa");
        }

        /// <summary>La victoria del 300 y «Siguiente»: el Mapa del fin de contenido, recién abierto.</summary>
        internal static IEnumerator OpenContentEnd()
        {
            yield return FactLineTests.Win(300);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressNext(), Is.True);
            for (int elapsed = 0; elapsed < 10000 && boot.App!.Screen == AppScreen.Level; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.RiverMapView!.Visible, Is.True, "el Mapa del fin de contenido");
        }

        // Al abrir, en cada fotograma de «ponerse al día» y al acabar: las placas, enteras a la vista y fuera de la ventanita.
        private static void CatchUpAndCheck(RiverMapScreen map, string at)
        {
            map.Tick(16f);
            AssertPlatesFit(map, at + ", al abrir");
            for (int t = 0; t < 30000 && (map.CatchingUp || map.CatchUpFading); t += 16)
            {
                map.Tick(16f);
                AssertPlatesFit(map, at + ", a " + t + " ms");
            }

            Assert.That(map.CatchingUp, Is.False, at + ": «ponerse al día» terminó");
            map.Tick(16f);
            AssertPlatesFit(map, at + ", al acabar");
        }

        // La Cuenca y otra vez el Mundo, cada una con su encuadre de apertura.
        private static void CheckTheViews(RiverMapScreen map, string at)
        {
            map.SetView(MapView.Basin);
            map.Tick(16f);
            AssertPlatesFit(map, at + ", Cuenca");
            map.SetView(MapView.World);
            map.Tick(16f);
            AssertPlatesFit(map, at + ", Mundo otra vez");
        }

        /// <summary>
        /// Ninguna placa de mundo futuro que se vea (asoma a la zona visible y no está oculta por FX10) se sale de la zona visible
        /// ni pisa la ventanita de casa si sale.
        /// </summary>
        internal static void AssertPlatesFit(RiverMapScreen map, string at)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 view = map.ViewportSize;
            var full = new Rect(0f, 0f, view.x, view.y);
            Rect window = map.HomeWindowShown ? map.HomeWindowViewRect : default;
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                Rect box = map.SilhouetteViewBox(i);
                string plate = "la placa «" + map.SilhouetteText(i) + "» " + box;
                if (map.PlateFitHidden(i))
                {
                    Assert.That(map.SilhouettePlate(i).enabled, Is.False, at + ": " + plate + " oculta, sin pintar");
                    Debug.Log("FX10 " + at + ": " + plate + " no cabe con este encuadre: oculta");
                    continue;
                }

                if (!box.Overlaps(full))
                {
                    continue; // fuera de la vista: no se ve
                }

                Assert.That(box.xMin >= -0.01f && box.yMin >= -0.01f && box.xMax <= view.x + 0.01f && box.yMax <= view.y + 0.01f, Is.True,
                    at + ": " + plate + " se sale de la zona visible " + full);
                if (window.width > 0f)
                {
                    Assert.That(box.Overlaps(window), Is.False, at + ": " + plate + " pisa la ventanita de casa " + window);
                }
            }
        }

        private static List<int> Worlds(RiverMapScreen map)
        {
            var worlds = new List<int>();
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                worlds.Add(map.SilhouetteWorld(i));
            }

            return worlds;
        }
    }
}
