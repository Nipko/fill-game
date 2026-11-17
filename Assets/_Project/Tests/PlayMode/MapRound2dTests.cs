#nullable enable
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los arreglos del Mapa de la prueba r2d de la 1.1 (<c>docs/dev/traspaso/pruebas-telefono-2026-11-16-1.1-r2d.md</c>), en la
    /// escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>observaciones 1 y 2 (b): el Mapa abierto directamente en la vista Mundo, al día o con <c>map_seen_level</c> atrasado
    /// (la canoa se pone al día desde una parada anterior: el caso del teléfono, con <c>zoom</c> 4,00 en el 521), lleva siempre el
    /// rótulo de la parada siguiente con «faltan N», entero a la vista, con el Premio del mundo dentro cuando toca (sin píldora
    /// aparte) y sin boyas encima de rótulos ni placas. Antes, la parada siguiente era aún una boya al abrir, cedía como parada
    /// futura con su línea y, sin cámara que siguiera el viaje, nada la volvía a poner;</item>
    /// <item>observación 2 (a): «Mundo» pulsado justo al acabar el viaje del Mapa del 10 deja la vista con sus rótulos y su placa,
    /// igual que al volver a pedir «Mundo» desde la Cuenca. En el 79, con el tucuxi por presentar, su primer plano (añadido de
    /// D-069 del 2026-11-16) apagaba los rótulos hasta que la cámara llegaba a ×1, unos 3,6 s: con la cámara ya casi en su sitio
    /// no se veía ninguno a los 3 s. Ahora vuelven al bajar de ×1,15 y a los 3 s están.</item>
    /// </list>
    /// </summary>
    public sealed class MapRound2dTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 16, 22, 0, 0, System.DateTimeKind.Utc);

        [SetUp]
        public void SetUp()
        {
            RiverMapScreen.ResetCapiSession();
            GameBootstrap.L10SequenceOverride = true; // los helpers de PlayMode la apagan si el test no la pide (PlayModeHelpers)
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameBootstrap.L10SequenceOverride = null;
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator OpeningDirectlyInTheWorld_TheNextStopLabel_IsWholeOnScreen_WithThePrize_AndNoBuoyOverLabels(
            [Values(79, 71, 521)] int max, [Values(0, 10, 29)] int behind)
        {
            GameScreen.SimulatedPxPerDp = 3f;
            yield return MapMarkersTests.OpenApp(max + 1, max - behind);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            string at = max + " (visto " + (max - behind) + ")";
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.Lobby));
            Assert.That(map.View, Is.EqualTo(MapView.World), "la pestaña del inicio abre en la vista Mundo");
            Assert.That(map.CatchingUp, Is.EqualTo(behind > 0), at + ": se pone al día si lo visto va atrasado");
            map.Tick(PlayModeHelpers.FrameMs);
            AssertNextLabel(map, at + " al abrir", false);
            Debug.Log("AF_MAP r2d apertura " + at + ": canoa " + map.LocalToView(map.CanoeGearBoxLocal) + ", zoom ×" + map.Zoom.ToString("0.00") + " alejado " + map.RemainingZoomedOut + ", rótulo "
                + map.LocalToView(map.StopLabelLocalRect(map.RemainingStopN())) + " «" + map.StopLabelText(map.RemainingStopN()).Replace('\n', ' ') + "»");

            // El viaje (o el relleno de «ponerse al día»), el acercamiento de llegada y la presentación del compañero, si la hay.
            TickUntilQuiet(map, at);
            Level10MapTests.Tick(map, 3000);
            AssertNextLabel(map, at + " tras ponerse al día", true);
            AssertNoBuoyOverLabels(map, at);
            Rect canoe = map.LocalToView(map.CanoeGearBoxLocal);
            var area = new Rect(Vector2.zero, map.ViewportSize);
            Assert.That(canoe.xMin >= -0.5f && canoe.xMax <= area.xMax + 0.5f && canoe.yMin >= -0.5f && canoe.yMax <= area.yMax + 0.5f, Is.True,
                at + ": la canoa con su equipo, entera a la vista con la parada siguiente: " + canoe);
            if (max == 71 || max == 79)
            {
                Assert.That(map.PrizeInStopLabel, Is.True, at + ": el Premio del mundo, en el rótulo de Leticia");
                Assert.That(map.PrizePill, Is.Null, at + ": sin la píldora del Premio aparte");
            }

            Debug.Log("AF_MAP r2d apertura " + at + " al final: zoom ×" + map.Zoom.ToString("0.00") + ", rótulo " + map.LocalToView(map.StopLabelLocalRect(map.RemainingStopN()))
                + ", rótulos y placas a la vista " + map.VisibleMapLabelCount() + ", premio junto " + map.PrizeInStopLabel + ", canoa " + map.LocalToView(map.CanoeGearBoxLocal)
                + " en " + map.ViewportSize);
            map.Close();
        }

        [UnityTest]
        public IEnumerator TappingWorld_RightAfterTheJourneyOfTheMapOf10_TheViewHasItsLabelsAndPlate_AsWhenAskedLater([Values(79, 521)] int max)
        {
            GameScreen.SimulatedPxPerDp = 3f;
            yield return LoadUnseen(max);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Visible && map.Source == MapOpenSource.L10, Is.True, max + ": el Mapa del 10 (el inicio lo abre solo con el Mapa sin ver)");
            for (int ms = 0; ms < 15000 && map.JourneyReadyMs < 0f; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(map.JourneyReadyMs, Is.GreaterThanOrEqualTo(0f), "map_play_ready");
            Assert.That(map.L10SequenceActive, Is.True, "aún queda secuencia: el caso de la prueba");
            yield return MapRound2cTests.TapTop(map, map.ViewWorldButton);
            map.Tick(PlayModeHelpers.FrameMs);
            Assert.That(map.View, Is.EqualTo(MapView.World));
            Assert.That(map.L10SequenceActive, Is.False);
            int quietMs = TickUntilQuiet(map, max + " tras «Mundo»");
            Debug.Log("AF_MAP r2d «Mundo» tras el viaje del 10 (" + max + "): compañero " + map.CrewIntroKind + ", rótulos de vuelta a los " + quietMs + " ms, zoom ×"
                + map.CameraCloseUp.ToString("0.00"));
            Assert.That(quietMs, Is.LessThan(3000), "a los 3 s de pulsar «Mundo» (con la presentación del compañero, si la hay), los rótulos ya se ven");
            Level10MapTests.Tick(map, 3000 - quietMs);
            View after = AssertNextLabel(map, max + " a los 3 s de «Mundo»", true);
            Level10MapTests.Tick(map, 3000);
            View settled = AssertNextLabel(map, max + " tras «Mundo», ya quieto", true);
            Assert.That(settled.Text, Is.EqualTo(after.Text));
            after = settled;
            AssertNoBuoyOverLabels(map, max + " tras «Mundo»");

            // Lo mismo que da la vista Mundo pedida otra vez desde la Cuenca (hoja-d de la prueba).
            map.SetView(MapView.Basin);
            map.Tick(PlayModeHelpers.FrameMs);
            map.SetView(MapView.World);
            Level10MapTests.Tick(map, 3000);
            View later = AssertNextLabel(map, max + " otra vez desde la Cuenca", true);
            Debug.Log("AF_MAP r2d «Mundo» (" + max + "): tras el viaje " + after + "; desde la Cuenca " + later);
            Assert.That(after.Text, Is.EqualTo(later.Text), "el mismo rótulo de la parada siguiente");
            Assert.That(Vector2.Distance(after.Label.center, later.Label.center), Is.LessThan(0.5f), "en el mismo sitio");
            Assert.That(after.Plate, Is.EqualTo(later.Plate), "la placa del mundo siguiente, igual");
            Assert.That(after.Prize, Is.EqualTo(later.Prize), "el Premio del mundo, igual");
            Assert.That(after.Shown, Is.EqualTo(later.Shown), "los mismos rótulos y placas a la vista");
            boot.CloseRiverMap();
        }

        // Segunda vuelta de la r2d (la captura del 79 al abrir en Mundo con lo visto atrasado): en cada fotograma de la apertura, del
        // viaje y del acercamiento de cada llegada, los rótulos que se ven quedan enteros en la pantalla con el margen de las placas
        // (también con la escala de la cámara) y ninguno, salvo el de la parada siguiente, pisa la canoa con su mástil, sus banderas
        // y su tripulación donde va ahora. El de la parada siguiente no se apaga nunca (salvo en el primer plano del compañero).
        [UnityTest]
        public IEnumerator OpeningInTheWorld_EveryFrame_LabelsAreWholeOnScreen_AndClearOfTheCanoe([Values(79, 71)] int max, [Values(0, 10)] int behind)
        {
            GameScreen.SimulatedPxPerDp = 3f;
            yield return MapMarkersTests.OpenApp(max + 1, max - behind);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            string at = max + " (visto " + (max - behind) + ")";
            float e = RiverMapScreen.CameraLabelEdgeDp - 0.5f;
            var view = new Rect(Vector2.zero, map.ViewportSize);
            var area = new Rect(e, e, view.width - (2f * e), view.height - (2f * e));
            int frames = 0;
            int checkedLabels = 0;
            for (int ms = 0; ms <= 8000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                if (map.CloseUpLabelAlpha < 1f)
                {
                    continue; // el primer plano los apaga todos (añadido de D-069 del 2026-11-16)
                }

                frames++;
                Canvas.ForceUpdateCanvases();
                Rect crew = map.CrewBoxNowLocal;
                int next = map.RemainingStopN();
                for (int n = 0; n < boot.RiverStops!.Count; n++)
                {
                    Rect r = map.StopLabelLocalRect(n);
                    if (r.width <= 0f)
                    {
                        Assert.That(n, Is.Not.EqualTo(next), at + " a los " + ms + " ms: el rótulo de la parada siguiente sale");
                        continue;
                    }

                    TMPro.TextMeshProUGUI label = map.StopLabelOf(n)!;
                    if (label.canvasRenderer.GetAlpha() <= 0f)
                    {
                        Assert.That(n, Is.Not.EqualTo(next), at + " a los " + ms + " ms: el rótulo de la parada siguiente no se apaga");
                        continue;
                    }

                    checkedLabels++;
                    Rect seen = map.LocalToScreenView(r);
                    Assert.That(Inside(seen, area), Is.True, at + " a los " + ms + " ms (cámara ×" + map.CameraZoom.ToString("0.00") + "): el rótulo de la parada " + n + " cortado: " + seen);
                    if (n != next)
                    {
                        Assert.That(r.Overlaps(crew), Is.False, at + " a los " + ms + " ms: el rótulo de la parada " + n + " pisa la canoa " + crew);
                    }
                }

                for (int i = 0; i < map.TownMarkerCount; i++)
                {
                    Rect r = map.TownLabelLocalRect(i);
                    if (r.width <= 0f || map.TownLabelMaskedByCamera(i))
                    {
                        continue;
                    }

                    checkedLabels++;
                    Rect seen = map.LocalToScreenView(r);
                    if (seen.Overlaps(view))
                    {
                        Assert.That(Inside(seen, area), Is.True, at + " a los " + ms + " ms (cámara ×" + map.CameraZoom.ToString("0.00") + "): el rótulo del pueblo " + i + " cortado: " + seen);
                    }

                    Assert.That(r.Overlaps(crew), Is.False, at + " a los " + ms + " ms: el rótulo del pueblo " + i + " pisa la canoa " + crew);
                }
            }

            Debug.Log("AF_MAP r2d rótulos fotograma a fotograma " + at + ": " + frames + " fotogramas, " + checkedLabels + " rótulos vistos, " + map.LabelsMaskedByCameraTotal
                + " apagados por la canoa o el borde, " + map.RefitsAfterJourney + " recolocaciones al acabar el viaje");
            Assert.That(frames, Is.GreaterThan(100));
            map.Close();
        }

        // Revisión de la r2d: al acabar un viaje se recoloca todo una sola vez. Con «Reducir movimiento» no hay viaje: el Mapa abre ya
        // con las paradas de max_level (solo funde el agua nueva) y no hace falta recolocar nada [P]; el rótulo sale igual.
        [UnityTest]
        public IEnumerator AfterAJourney_EverythingIsRefitOnce([Values(false, true)] bool reduceMotion)
        {
            GameScreen.SimulatedPxPerDp = 3f;
            yield return MapMarkersTests.OpenApp(80, 79);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.Close();
            RiverMapScreen.ResetCapiSession();
            map.ProgressReduceMotion = reduceMotion;
            try
            {
                map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, new MemoryProgressStore(80, 79, 69), 80, true);
                Assert.That(map.CatchingUp || reduceMotion, Is.True, "se pone al día del 69 al 79");
                Assert.That(map.RefitsAfterJourney, Is.EqualTo(0), "al abrir, aún nada");
                Level10MapTests.Tick(map, 12000);
                Assert.That(map.CatchingUp, Is.False);
                Debug.Log("AF_MAP r2d recolocaciones (Reducir movimiento " + reduceMotion + "): " + map.RefitsAfterJourney);
                int once = reduceMotion ? 0 : 1;
                Assert.That(map.RefitsAfterJourney, Is.EqualTo(once), reduceMotion ? "sin viaje, nada que recolocar" : "una vez por viaje");
                AssertNextLabel(map, "79 (visto 69), Reducir movimiento " + reduceMotion, true);
                Level10MapTests.Tick(map, 3000);
                Assert.That(map.RefitsAfterJourney, Is.EqualTo(once), "y ninguna más con el Mapa quieto");
            }
            finally
            {
                map.ProgressReduceMotion = false;
            }

            map.Close();
        }

        private static bool Inside(Rect r, Rect area)
        {
            return r.xMin >= area.xMin && r.xMax <= area.xMax && r.yMin >= area.yMin && r.yMax <= area.yMax;
        }

        // Avanza hasta que nada apaga los rótulos (el viaje, el acercamiento de llegada, el primer plano del compañero); como mucho
        // 12 s. Devuelve lo que tardó.
        internal static int TickUntilQuiet(RiverMapScreen map, string at)
        {
            int ms = 0;
            for (; ms < 12000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                if (ms >= 500 && !map.CatchingUp && !map.CrewIntroWaiting && !map.CrewArriving && map.CameraCloseUp <= RiverMapScreen.CloseUpLabelsBackBelow
                    && map.CloseUpLabelAlpha >= 1f)
                {
                    break;
                }
            }

            Assert.That(map.CloseUpLabelAlpha, Is.EqualTo(1f), at + ": los rótulos vuelven tras el primer plano");
            return ms;
        }

        private readonly struct View
        {
            public View(string text, Rect label, bool plate, bool prize, int shown)
            {
                Text = text;
                Label = label;
                Plate = plate;
                Prize = prize;
                Shown = shown;
            }

            public string Text { get; }

            public Rect Label { get; }

            public bool Plate { get; }

            public bool Prize { get; }

            public int Shown { get; }

            public override string ToString()
            {
                return "«" + Text.Replace('\n', ' ') + "» en " + Label + ", placa " + Plate + ", premio junto " + Prize + ", " + Shown + " a la vista";
            }
        }

        // El rótulo de la parada siguiente sale, con «faltan N», entero en la zona visible del mapa, fuera de la ventanita de casa y
        // (si opaque) con opacidad 1, también la heredada; la placa del mundo siguiente que cae en la vista, entera y visible.
        private static View AssertNextLabel(RiverMapScreen map, string at, bool opaque)
        {
            Canvas.ForceUpdateCanvases();
            int n = map.RemainingStopN();
            Assert.That(n, Is.GreaterThanOrEqualTo(0), at + ": hay parada siguiente");
            Rect local = map.StopLabelLocalRect(n);
            Assert.That(local.width, Is.GreaterThan(0f), at + ": el rótulo de la parada " + n + " sale (placa de respaldo " + map.RemainingLabelOffView + ")");
            Assert.That(map.RemainingLabelOffView, Is.False, at + ": «faltan N» en el rótulo, no en la placa de respaldo");
            string text = map.StopLabelText(n);
            Assert.That(text.Contains("\n"), Is.True, at + ": el rótulo lleva «faltan N»: «" + text + "»");
            Rect seen = map.LocalToView(local);
            var view = new Rect(Vector2.zero, map.ViewportSize);
            Assert.That(seen.xMin >= view.xMin - 0.5f && seen.xMax <= view.xMax + 0.5f && seen.yMin >= view.yMin - 0.5f && seen.yMax <= view.yMax + 0.5f, Is.True,
                at + ": entero a la vista: " + seen + " en " + view);
            if (map.HomeWindowShown)
            {
                Assert.That(seen.Overlaps(map.HomeWindowViewRect), Is.False, at + ": bajo la ventanita de casa");
            }

            if (opaque)
            {
                TextMeshProUGUI label = map.StopLabelOf(n)!;
                Assert.That(label.canvasRenderer.GetInheritedAlpha(), Is.EqualTo(1f).Within(1e-3f), at + ": el texto del rótulo, opaco");
                Assert.That(label.transform.parent.GetComponent<CanvasRenderer>().GetInheritedAlpha(), Is.EqualTo(1f).Within(1e-3f), at + ": su píldora, opaca");
                Assert.That(map.CloseUpLabelAlpha, Is.EqualTo(1f), at + ": sin primer plano que los apague");
            }

            bool plate = false;
            for (int i = 0; i < map.SilhouetteCount; i++)
            {
                Rect box = map.SilhouetteViewBox(i);
                if (!map.SilhouettePlate(i).enabled || !box.Overlaps(view))
                {
                    continue;
                }

                Assert.That(box.xMin >= view.xMin - 0.5f && box.xMax <= view.xMax + 0.5f && box.yMin >= view.yMin - 0.5f && box.yMax <= view.yMax + 0.5f, Is.True,
                    at + ": la placa del mundo " + map.SilhouetteWorld(i) + " cortada: " + box);
                if (opaque)
                {
                    Assert.That(map.SilhouetteLabel(i).canvasRenderer.GetInheritedAlpha(), Is.GreaterThan(0.99f), at + ": la placa del mundo " + map.SilhouetteWorld(i) + ", visible");
                }

                plate = true;
            }

            return new View(text, seen, plate, map.PrizeInStopLabel, map.VisibleMapLabelCount());
        }

        // Ninguna boya por ganar ni ganada dibujada encima de un rótulo o una placa (observación 3 de la r2b, otra vez en la r2d).
        private static void AssertNoBuoyOverLabels(RiverMapScreen map, string at)
        {
            foreach (RiverMapScreen.BuoyGraphic g in new[] { map.Buoys!, map.BuoysAhead! })
            {
                float r = g.DrawnRadius;
                for (int i = 0; i < map.BuoyCount; i++)
                {
                    RiverMapScreen.BuoyState s = map.BuoyStateAt(i);
                    if (s == RiverMapScreen.BuoyState.Skipped || (s == RiverMapScreen.BuoyState.Unlit) != g.Ahead || g.IsHidden(i))
                    {
                        continue;
                    }

                    Vector2 p = map.BuoyLocalPoint(i);
                    foreach (Rect box in MapRound2bTests.RealLabelBoxes(map))
                    {
                        Assert.That(MapRound2Tests.Hits(box, p, r), Is.False, at + ": la boya del " + map.BuoyLevel(i) + " sobre un rótulo o una placa");
                    }
                }
            }
        }

        // La app en el inicio con max niveles ganados y el Mapa sin ver: el inicio abre solo el Mapa del 10 (af_max_level de un salto).
        internal static IEnumerator LoadUnseen(int max, bool clock = true)
        {
            if (!GameScreen.SimulatedScreen.HasValue)
            {
                GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // un teléfono de 360 × 780 dp
            }

            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = max + 1;
            profile.Progress.MaxLevel = max;
            profile.Progress.MapSeenLevel = 0;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            if (clock)
            {
                PlayModeHelpers.TakeClock(boot);
            }

            if (!boot.RiverMapView!.Visible)
            {
                Assert.That(boot.OpenRiverMap(MapOpenSource.L10), Is.True);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
