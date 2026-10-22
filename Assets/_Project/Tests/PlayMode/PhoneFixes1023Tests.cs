#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Las observaciones 1 y 2 de la prueba en el teléfono del 2026-10-23 (<c>docs/dev/traspaso/pruebas-telefono-2026-10-23.md</c>),
    /// con las tablas del proyecto (sin andamio), a 360 × 780 dp:
    /// <list type="bullet">
    /// <item><description><b>Observación 1, textos vacíos en en y pt-BR</b>: en el inicio y en el Mapa (vistas Mundo y Cuenca),
    /// todo texto que en es-419 sale con texto sale también con texto en en y pt-BR (la misma pieza en la misma pantalla), y en
    /// concreto «Faltan N niveles para {parada}» del inicio y del Mapa, la pregunta del mundo de la cabecera y «Premio del mundo ·
    /// faltan N». Antes, con las filas <c>map.*</c> de <c>facts.csv</c> sin traducir, salían vacíos.</description></item>
    /// <item><description><b>Observación 2, placas de la Cuenca al 130 %</b>: en es-419, en y pt-BR, al 100 % y al 130 % y con
    /// varios progresos, las placas de los mundos futuros de la Cuenca no se pisan entre ellas ni con un país, quedan enteras en la
    /// zona visible de la Cuenca y su texto cabe en su caja a ≥ 12 sp (× escala), sin partirse a media palabra
    /// (<see cref="LocInventory.ProblemsOf"/>).</description></item>
    /// </list>
    /// No necesitan gráficos; las capturas, en <see cref="PhoneFixes1023SnapshotTests"/>.
    /// </summary>
    public sealed class PhoneFixes1023Tests
    {
        internal static readonly DateTime T0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

        /// <summary>Progresos con los que se miden las placas de la Cuenca (nivel de «Jugar»).</summary>
        internal static readonly int[] BasinPlays = { 11, 21, 41, 59, 121, 201, 301 };

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator HomeAndMap_ShowEveryTextOfSpanish_InEnglishAndPortuguese(
            [Values(LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(46, 75)] int play)
        {
            Dictionary<string, string> spanish = new Dictionary<string, string>();
            yield return Walk(LocLocale.SourceLocale, play, spanish, null);
            Dictionary<string, string> shown = new Dictionary<string, string>();
            var named = new List<string>();
            yield return Walk(locale, play, shown, named);

            var empty = new List<string>();
            foreach (KeyValuePair<string, string> kv in spanish)
            {
                if (kv.Value.Length > 0 && (!shown.TryGetValue(kv.Key, out string? text) || string.IsNullOrWhiteSpace(text)))
                {
                    empty.Add(kv.Key + "\t(es-419: «" + kv.Value + "»)");
                }
            }

            TestContext.WriteLine("AF_FX1023 " + locale + " jugando el " + play.ToString(CultureInfo.InvariantCulture) + ": "
                + spanish.Count.ToString(CultureInfo.InvariantCulture) + " textos en es-419, " + shown.Count.ToString(CultureInfo.InvariantCulture)
                + " en " + locale + "\n" + string.Join("\n", named));
            Assert.That(spanish.Count, Is.GreaterThan(20), "textos del inicio y del Mapa mirados en es-419");
            Assert.That(named, Is.Empty, "textos concretos de la observación 1 vacíos en " + locale);
            Assert.That(empty, Is.Empty, "textos que salen en es-419 y vacíos en " + locale + ":\n" + string.Join("\n", empty));
        }

        [UnityTest]
        public IEnumerator BasinPlates_NoOverlap_WholeInView_TextFits(
            [Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent)
        {
            float scale = percent / 100f;
            var problems = new List<string>();
            var twoLines = new List<string>();
            var hidden = new List<string>();
            int plates = 0;
            foreach (int play in BasinPlays)
            {
                yield return Open(locale, scale, play);
                RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
                map.SetView(MapView.Basin);
                map.Tick(16f);
                yield return null;
                Canvas.ForceUpdateCanvases();
                plates += map.SilhouetteCount;
                CheckBasinPlates(map, locale, scale, "jugando el " + play.ToString(CultureInfo.InvariantCulture), problems, twoLines, hidden);
            }

            TestContext.WriteLine("AF_FX1023 Cuenca " + locale + " al " + percent.ToString(CultureInfo.InvariantCulture) + " %: "
                + plates.ToString(CultureInfo.InvariantCulture) + " placas, " + twoLines.Count.ToString(CultureInfo.InvariantCulture) + " en dos líneas, "
                + hidden.Count.ToString(CultureInfo.InvariantCulture) + " sin sitio, " + problems.Count.ToString(CultureInfo.InvariantCulture) + " problemas\n"
                + string.Join("\n", twoLines) + (hidden.Count > 0 ? "\n" + string.Join("\n", hidden) : string.Empty));
            Assert.That(plates, Is.GreaterThan(10), "placas de mundos futuros mirados");
            if (percent == 100)
            {
                Assert.That(twoLines, Is.Empty, "al 100 % las placas van en una línea, como antes");
                Assert.That(hidden, Is.Empty, "al 100 % salen todas las placas, como antes");
            }

            Assert.That(problems, Is.Empty, "placas de la Cuenca en " + locale + " al " + percent + " %:\n" + string.Join("\n", problems));
        }

        /// <summary>
        /// Las placas de la Cuenca de <paramref name="map"/>: ni se pisan entre ellas ni con un país que sale, quedan enteras en la
        /// zona visible de la Cuenca y su texto cabe (<see cref="LocInventory.ProblemsOf"/>); apunta lo que falle en
        /// <paramref name="problems"/>.
        /// </summary>
        internal static void CheckBasinPlates(RiverMapScreen map, string locale, float scale, string at, List<string> problems,
            List<string>? twoLines = null, List<string>? hidden = null)
        {
            Assert.That(map.View, Is.EqualTo(MapView.Basin));
            Assert.That(map.BasinFrameLocal(out Rect frame), Is.True, at + ": la zona visible de la Cuenca");
            var inventory = new LocInventory(locale, scale);
            int count = map.SilhouetteCount;
            if (count > 0 && map.PlateFitHidden(0))
            {
                problems.Add(at + "\tla placa del mundo siguiente («" + map.SilhouetteText(0) + "») no sale");
            }

            for (int i = 0; i < count; i++)
            {
                if (map.PlateFitHidden(i))
                {
                    hidden?.Add(at + "\tplaca «" + map.SilhouetteText(i) + "» no sale (ni en dos líneas cabía)");
                    continue;
                }

                if (map.SilhouetteTwoLines(i))
                {
                    twoLines?.Add(at + "\tplaca «" + map.SilhouetteText(i).Replace("\n", " / ") + "» en dos líneas");
                }

                Rect box = map.SilhouetteLocalBox(i);
                string name = at + "\tplaca «" + map.SilhouetteText(i) + "»";
                const float tol = 0.5f;
                if (box.xMin < frame.xMin - tol || box.xMax > frame.xMax + tol || box.yMin < frame.yMin - tol || box.yMax > frame.yMax + tol)
                {
                    problems.Add(name + "\tcortada: " + box + " fuera de la zona visible " + frame);
                }

                for (int k = i + 1; k < count; k++)
                {
                    if (map.PlateFitHidden(k))
                    {
                        continue;
                    }

                    Rect other = map.SilhouetteLocalBox(k);
                    if (Shrink(box).Overlaps(Shrink(other)))
                    {
                        problems.Add(name + "\tpisa la placa «" + map.SilhouetteText(k) + "»: " + box + " / " + other);
                    }
                }

                for (int c = 0; c < map.CountryLabelCount; c++)
                {
                    TextMeshProUGUI country = map.CountryLabel(c);
                    if (country.gameObject.activeInHierarchy && !map.CountryBasinHidden(c) && country.text.Length > 0
                        && Shrink(box).Overlaps(Shrink(map.CountryLocalBox(c))))
                    {
                        problems.Add(name + "\tpisa el país «" + country.text + "»");
                    }
                }

                TextMeshProUGUI label = map.SilhouetteLabel(i);
                string text = inventory.ProblemsOf(label);
                if (text.Length > 0)
                {
                    problems.Add(name + "\t" + text + "\t" + label.fontSize.ToString("0.#", CultureInfo.InvariantCulture) + " sp");
                }
            }
        }

        // Medio dp hacia dentro: dos cajas que solo se tocan en el borde no se pisan.
        private static Rect Shrink(Rect r)
        {
            return Rect.MinMaxRect(r.xMin + 0.5f, r.yMin + 0.5f, r.xMax - 0.5f, r.yMax - 0.5f);
        }

        /// <summary>
        /// Arranca el juego en <paramref name="locale"/> (las tablas del proyecto) con la fuente a <paramref name="scale"/>, a
        /// 360 × 780 dp, jugando el <paramref name="play"/>, y abre el Mapa desde el inicio.
        /// </summary>
        internal static IEnumerator Open(string locale, float scale, int play)
        {
            yield return Load(locale, scale, play);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, "el Mapa se abre desde el inicio");
            RiverMapScreen map = boot.RiverMapView!;
            map.Tick(16f);
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        internal static IEnumerator Load(string locale, float scale, int play)
        {
            LaunchOptions.LocTablesOverride = null;
            LaunchOptions.LanguageOverride = locale;
            LaunchOptions.FontScaleOverride = scale;
            LaunchOptions.ClockOverride = new ManualClock(T0);
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // un teléfono de 360 × 780 dp
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = play;
            profile.Progress.MaxLevel = play - 1;
            profile.Progress.MapSeenLevel = play - 1;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            yield return null;
            Assert.That(Loc.Locale, Is.EqualTo(locale), "la UI en el idioma medido");
            PlayModeHelpers.TakeClock(GameBootstrap.Current!);
        }

        // El inicio y el Mapa (vistas Mundo y Cuenca) en locale jugando el play: cada texto visible, por su ruta en la jerarquía, en
        // texts; con named, también los textos concretos de la observación 1 que salen vacíos.
        private static IEnumerator Walk(string locale, int play, Dictionary<string, string> texts, List<string>? named)
        {
            yield return Load(locale, 1f, play);
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Canvas.ForceUpdateCanvases();
            Collect("inicio", lobby.Canvas.transform, texts);
            if (named != null && string.IsNullOrWhiteSpace(lobby.StopRemainingText))
            {
                named.Add("inicio: «Faltan N niveles para {parada}» (map_stop_remaining)");
            }

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, "el Mapa se abre desde el inicio");
            RiverMapScreen map = boot.RiverMapView!;
            map.Tick(16f);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Collect("mapa", map.Canvas.transform, texts);
            if (named != null)
            {
                if (string.IsNullOrWhiteSpace(map.StopRemainingText))
                {
                    named.Add("mapa: el botón «Faltan N niveles para {parada}» (map_stop_remaining)");
                }

                RiverStop? first = boot.RiverStops!.FirstStopOfWorld(WorldTable.WorldOf(play));
                if (first != null && first.WorldQuestion != null && (Loc.Fact(first.WorldQuestion).Length == 0 || !map.Title.Contains(Loc.Fact(first.WorldQuestion))))
                {
                    named.Add("mapa: la pregunta del mundo en la cabecera (" + first.WorldQuestion + "): «" + map.Title + "»");
                }

                if (map.PrizeMarker >= 0 && string.IsNullOrWhiteSpace(map.PrizeText))
                {
                    named.Add("mapa: «Premio del mundo · faltan N» (map_world_prize)");
                }
            }

            map.SetView(MapView.Basin);
            map.Tick(16f);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Collect("cuenca", map.Canvas.transform, texts);
        }

        private static void Collect(string screen, Transform root, Dictionary<string, string> texts)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!text.enabled || text.color.a < 0.01f)
                {
                    continue;
                }

                texts[screen + "\t" + Path(text.transform, root)] = text.text ?? string.Empty;
            }
        }

        private static string Path(Transform t, Transform root)
        {
            var sb = new StringBuilder(t.name);
            for (Transform? p = t.parent; p != null && p != root; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Capturas de <see cref="PhoneFixes1023Tests"/>: la vista Cuenca del Mapa jugando el 41 y el 121 (con placas de mundos
    /// futuros), y el Mapa y el inicio jugando el 46, en es-419, en y pt-BR, al 100 % y al 130 %, en
    /// <c>Logs/snapshots/&lt;idioma&gt;/fx1023_*.png</c>, con los problemas de las placas en <c>fx1023_&lt;escala&gt;_cuenca.txt</c>.
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.PhoneFixes1023SnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de la prueba en el teléfono del 2026-10-23; necesitan dispositivo gráfico.")]
    public sealed class PhoneFixes1023SnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale, [Values(100, 130)] int percent)
        {
            string dir = SnapshotKit.Prepare(locale);
            MapArtRule.ForceProvisional = true;
            float scale = percent / 100f;
            string prefix = "fx1023_" + percent.ToString(CultureInfo.InvariantCulture) + "_";
            var problems = new List<string>();
            foreach (int play in new[] { 41, 121 })
            {
                yield return PhoneFixes1023Tests.Open(locale, scale, play);
                GameBootstrap boot = GameBootstrap.Current!;
                RiverMapScreen map = boot.RiverMapView!;
                map.SetView(MapView.Basin);
                map.Tick(16f);
                SnapshotKit.AttachLobby(boot);
                SnapshotKit.AttachOverlay(map.Canvas);
                yield return SnapshotKit.Wait(0.3f);
                Canvas.ForceUpdateCanvases();
                PhoneFixes1023Tests.CheckBasinPlates(map, locale, scale, "jugando el " + play.ToString(CultureInfo.InvariantCulture), problems);
                SnapshotKit.Save(dir, prefix + "cuenca_" + play.ToString(CultureInfo.InvariantCulture));
            }

            yield return PhoneFixes1023Tests.Load(locale, scale, 46);
            GameBootstrap home = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(home);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, prefix + "inicio_46");
            Assert.That(home.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            RiverMapScreen world = home.RiverMapView!;
            world.Tick(16f);
            SnapshotKit.AttachOverlay(world.Canvas);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.Save(dir, prefix + "mapa_46");

            var sb = new StringBuilder();
            sb.Append("# FX1023 Cuenca ").Append(locale).Append(" al ").Append(percent.ToString(CultureInfo.InvariantCulture)).Append(" %: ")
                .Append(problems.Count.ToString(CultureInfo.InvariantCulture)).Append(" problemas\n");
            foreach (string line in problems)
            {
                sb.Append(line).Append('\n');
            }

            string report = System.IO.Path.Combine(dir, prefix + "cuenca.txt");
            System.IO.File.WriteAllText(report, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("AF_FX1023 " + locale + " al " + percent + " %: " + problems.Count.ToString(CultureInfo.InvariantCulture) + " problemas; " + report);
        }
    }
}
