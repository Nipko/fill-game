#nullable enable
using System;
using System.Collections;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El Mapa del río v2, bloque M2-4 «La tripulación» (D-069, P6 (a); <c>docs/dev/propuesta-mapa-rio-v2.md</c> §6, §7.2 y §7.3), en
    /// la escena <c>Game</c> real:
    /// <list type="bullet">
    /// <item>la presentación del tucán (21) y del tucuxi (71) la primera vez que se abre el Mapa tras su punto: llega, el chip de 2 s,
    /// ♥ de Capi y TalkBack; la segunda vez, ya a bordo y sin presentación (sin campo nuevo en el guardado);</item>
    /// <item>la llegada: el tucán abre el pico y el tucuxi y Rosa saltan juntos, sin retrasar «Jugar»;</item>
    /// <item>al cambiar de mundo, el compañero se queda en su tramo; el cameo del miquito en un viaje (nunca a bordo);</item>
    /// <item>Reducir movimiento (aparece en su sitio y sale el chip), <c>ui_map_crew</c> apagada (el Mapa de M2-3) y la memoria (el
    /// atlas se suelta al cerrar; sin asignaciones por fotograma);</item>
    /// <item>«Faltan N» en el rótulo de la parada siguiente (respuesta del dueño a la pregunta 2 de M2-3), en el recorrido del lector,
    /// y la placa de la 1.0 con <c>ui_map_v2</c> apagada.</item>
    /// </list>
    /// </summary>
    public sealed class MapCrewPlayTests
    {
        [SetUp]
        public void SetUp()
        {
            RiverMapScreen.ResetCapiSession();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameBootstrap.A11yHostOverride = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator TheToucan_JoinsTheFirstTimeAfter21_ArrivesWithTheChipAndAHeart_ThenIsAboard()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return MapMarkersTests.OpenApp(22, 20); // max 21: el Mapa no se vio desde Alter do Chão (20)
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CrewActive, Is.True, "ui_map_crew encendida por defecto (08 §9)");
            Assert.That(map.CrewArt.Built, Is.True);
            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Toucan));
            Assert.That(map.Capi.PlannedEmotion, Is.EqualTo(CapiEmotion.Heart), "♥ al unirse alguien (§5.3)");
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.ToucanAboardShown, Is.False, "aún no se ha unido: llega al cruzar su punto");

            bool arriving = false;
            bool heart = false;
            bool chip = false;
            for (int ms = 0; ms < 3500; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                arriving |= map.CrewArriving && map.ToucanAboardShown;
                heart |= map.CapiRig!.ShownEmotion == CapiEmotion.Heart;
                chip |= map.CrewChipShown;
            }

            Assert.That(arriving, Is.True, "llega volando y se acomoda en la proa");
            Assert.That(heart, Is.True, "Capi hace ♥");
            Assert.That(chip, Is.True, "el chip de 2 s");
            Assert.That(map.CrewChipText, Is.EqualTo("¡Se une al viaje! · Tucán"));
            Assert.That(map.CrewChipShown, Is.False, "2 s y se va");
            Assert.That(map.ToucanAboardShown, Is.True, "polizón en la proa");
            Assert.That(map.ToucanRect!.parent, Is.EqualTo(map.CapiRig.Root), "va en la canoa (se voltea y escala con ella)");
            Assert.That(host.Announcements, Has.Some.EqualTo("Se une al viaje: tucán"), "TalkBack (§7.3)");

            // La segunda vez (map_seen_level ya es 21): a bordo desde el principio y sin presentación.
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(map.CrewArt.Built, Is.False, "el atlas se suelta al cerrar (§7.2)");
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.CrewIntroKind, Is.Null, "ya presentado: lo dice map_seen_level, sin campo nuevo");
            Assert.That(map.ToucanAboardShown, Is.True);
            Assert.That(map.CrewChipShown, Is.False);
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator At20_NoToucanYet_AndAt41_ItStaysInItsStretch()
        {
            yield return MapMarkersTests.OpenApp(21);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CrewActive, Is.True);
            Assert.That(map.ToucanAboardShown, Is.False, "max 20: aún no");
            Assert.That(map.CrewIntroKind, Is.Null);
            map.Close();

            MapMarkersTests.Reopen(boot, new MemoryProgressStore(42, 41, 41), 42);
            map.Tick(PlayModeHelpers.FrameMs);
            Assert.That(map.ToucanAboardShown, Is.False, "en el mundo 3 ya no va a bordo");
            Assert.That(map.ToucanHomeShown, Is.True, "se quedó en su tramo y se le ve allí (§6.1, regla 7)");
            Vector2 alter = map.Space.ToLocal(RiverMapScreen.CanoeAt(boot.RiverMapData!, boot.RiverRoutes!, RiverCrew.ToucanJoinLevel));
            Assert.That(Vector2.Distance(map.ToucanHomeRect!.anchoredPosition, alter), Is.LessThan(30f), "junto a donde se unió, al salir de Alter do Chão");
            Assert.That(map.TucuxiAboardShown, Is.False, "el tucuxi, en el 71");
            map.Close();

            // Con el 81, el tucuxi se queda en el cauce del Solimões al salir de Tefé: nunca en el lago (D-069, puntos sensibles).
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(82, 81, 81), 82);
            map.Tick(PlayModeHelpers.FrameMs);
            Assert.That(map.TucuxiAboardShown, Is.False);
            Assert.That(map.TucuxiHomeShown, Is.True);
            RiverStopRoute leg = boot.RiverRoutes!.At(8)!;
            Assert.That(boot.RiverMapData!.Lines[leg.Path[0].Line].Id, Is.EqualTo("amazonas"), "el tramo de salida de Tefé va por el cauce");
            Vector2 channel = map.Space.ToLocal(RiverMapScreen.CanoeAt(boot.RiverMapData, boot.RiverRoutes, RiverCrew.TucuxiJoinLevel));
            Assert.That(Vector2.Distance(map.TucuxiHomeRect!.anchoredPosition, channel), Is.LessThan(0.5f), "en el cauce, donde la canoa iba con el 71");
            map.Close();
        }

        [UnityTest]
        public IEnumerator TheTucuxi_JoinsAt71_SwimsBesideRosa_AndTheyJumpTogetherAtTheArrival_WithoutDelayingPlay()
        {
            yield return MapMarkersTests.OpenApp(72, 70); // max 71
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CrewIntroKind, Is.EqualTo(CrewKind.Tucuxi));
            Level10MapTests.Tick(map, 3000);
            Assert.That(map.TucuxiAboardShown, Is.True, "nada junto a Rosa");
            Assert.That(map.CrewChipText, Is.EqualTo("¡Se une al viaje! · Tucuxi"));
            Assert.That(map.GuideId, Is.EqualTo(RiverJourney.GuideRosa));
            Assert.That(Vector2.Distance(map.TucuxiImage!.rectTransform.anchoredPosition, map.GuideImage!.rectTransform.anchoredPosition),
                Is.LessThan(20f), "junto a Rosa");
            Assert.That(map.ToucanAboardShown, Is.False, "el tucán se quedó en el mundo 2");
            map.Close();

            // Llegada a Leticia (80) con el tucuxi ya presentado: saltan juntos (≤ 400 ms) y «Jugar» no espera.
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(81, 80, 75), 81);
            Assert.That(map.CatchingUp, Is.True);
            Assert.That(map.CrewIntroKind, Is.Null);
            float hopStart = -1f;
            float hopEnd = -1f;
            float arrivedAt = -1f;
            float hopTop = float.MinValue;
            for (int ms = 0; ms < 5000; ms += PlayModeHelpers.FrameMs)
            {
                bool before = map.CatchingUp;
                bool hopping = map.CrewHopping;
                map.Tick(PlayModeHelpers.FrameMs);
                if (before && !map.CatchingUp)
                {
                    arrivedAt = ms;
                }

                if (!hopping && map.CrewHopping)
                {
                    hopStart = ms;
                }

                if (hopping && !map.CrewHopping)
                {
                    hopEnd = ms;
                }

                if (map.CrewHopping)
                {
                    hopTop = Mathf.Max(hopTop, map.GuideImage!.rectTransform.anchoredPosition.y);
                }
            }

            Vector2 rest = map.GuideImage!.rectTransform.anchoredPosition;
            Assert.That(hopStart, Is.GreaterThanOrEqualTo(0f), "la tripulación salta en la llegada");
            Assert.That(hopEnd - hopStart, Is.LessThanOrEqualTo(RiverMapScreen.CrewHopMs + PlayModeHelpers.FrameMs), "≤ 400 ms (§6.3)");
            Assert.That(hopTop - rest.y, Is.GreaterThan(2f), "Rosa salta con el tucuxi");
            Assert.That(arrivedAt, Is.GreaterThanOrEqualTo(0f));
            Assert.That(map.PlayShown, Is.True, "«Jugar» a la vista: el salto no lo retrasa");
            map.SetView(MapView.Basin);
            map.SetView(MapView.World); // la canoa y su guía, colocados de nuevo por la vista: Rosa no se quedó en el aire
            Assert.That(map.GuideImage.rectTransform.anchoredPosition.y, Is.EqualTo(rest.y).Within(0.01f), "y Rosa vuelve a su sitio");
            map.Close();
        }

        [UnityTest]
        public IEnumerator ReduceMotion_TheCompanionAppearsInPlace_AndTheChipShows()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return MapMarkersTests.OpenApp(21);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            map.ProgressReduceMotion = true;
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(22, 21, 20), 22);
            Assert.That(map.CatchingUp, Is.False, "sin viaje");
            Assert.That(map.CrewIntroFired, Is.True);
            Assert.That(map.CrewArriving, Is.False, "aparece en su sitio, sin volar");
            Assert.That(map.ToucanAboardShown, Is.True);
            Assert.That(map.CrewChipShown, Is.True, "y sale el chip");
            Assert.That(map.Capi.Emotion, Is.EqualTo(CapiEmotion.Heart), "la emoción, quieta");
            Assert.That(host.Announcements, Has.Some.EqualTo("Se une al viaje: tucán"));
            Vector2 at = map.ToucanRect!.anchoredPosition;
            map.Tick(600f);
            Assert.That(map.ToucanRect.anchoredPosition, Is.EqualTo(at), "quieto");
            map.Tick(1500f);
            Assert.That(map.CrewChipShown, Is.False);
            map.ProgressReduceMotion = false;
            map.Close();
        }

        [UnityTest]
        public IEnumerator WithUiMapCrewOff_TheMapIsAsInM23()
        {
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapCrew = false };
            yield return MapMarkersTests.OpenApp(72, 60);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.LiveActive && map.CapiActive, Is.True, "el Mapa v2 con Capi al remo");
            Assert.That(map.CrewActive, Is.False);
            Assert.That(map.CrewArt.Built, Is.False, "sin atlas de la tripulación");
            Assert.That(map.CrewIntroKind, Is.Null);
            Assert.That(map.CameoLevel, Is.EqualTo(-1f));
            for (int i = 0; i < map.CapiEventCount; i++)
            {
                Assert.That(map.CapiEventAt(i), Is.Not.EqualTo(CapiMoment.Join));
            }

            Level10MapTests.Tick(map, 3000);
            Assert.That(map.ToucanAboardShown || map.TucuxiAboardShown || map.CameoShown || map.CrewChipShown, Is.False);
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator TheMonkeyCameo_WavesFromABankTree_HalfwayThroughTheJourney_NeverAboard()
        {
            yield return MapMarkersTests.OpenApp(71, 60); // de Anavilhanas (60) a Tefé (70): el sauim-de-coleira de Manaos
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.CameoLevel, Is.EqualTo(65f), "a mitad del viaje");
            Assert.That(map.CrewArt.HasMonkey, Is.True, "con la paleta de la especie del tramo");
            bool shown = false;
            bool waving = false;
            for (int ms = 0; ms < 6000; ms += PlayModeHelpers.FrameMs)
            {
                map.Tick(PlayModeHelpers.FrameMs);
                shown |= map.CameoShown;
                waving |= map.CameoWaving;
                Assert.That(map.CameoRect == null || map.CameoRect.parent != map.CapiRig!.Root, Is.True, "nunca en la canoa (D-010)");
            }

            Assert.That(shown && waving, Is.True, "saluda desde su árbol");
            Assert.That(map.CameoShown, Is.False, "y se va");
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator Memory_TheAtlasIsInAfPerf_AndTheJourneyWithCrewDoesNotAllocatePerFrame()
        {
            yield return MapMarkersTests.OpenApp(72, 70);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            StringAssert.Contains("\"crew_tex_kib\":128", map.CrewMemJson());
            Canvas.ForceUpdateCanvases();
            Level10MapTests.Tick(map, 3000); // la presentación ya salió
            map.Close();
            MapMarkersTests.Reopen(boot, new MemoryProgressStore(81, 80, 75), 81);
            Canvas.ForceUpdateCanvases();
            map.Tick(PlayModeHelpers.FrameMs);
            for (int ms = 0; ms < 4000; ms += PlayModeHelpers.FrameMs)
            {
                float fill = map.FrontFraction;
                bool before = map.CatchingUp;
                bool hop = map.CrewHopping;
                int blocks = AllocBlocks(() => map.Tick(PlayModeHelpers.FrameMs)) + AllocBlocks(Canvas.ForceUpdateCanvases);
                bool changed = map.FrontFraction != fill || before != map.CatchingUp || hop != map.CrewHopping;
                Assert.That(changed || blocks == 0, Is.True, "a " + ms + " ms, sin cambio del llenado: " + blocks + " bloques de GC.Alloc");
            }

            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(map.CrewArt.Built, Is.False);
            Assert.That(map.CrewMemJson(), Does.Contain("\"crew_tex_kib\":0"));
        }

        [UnityTest]
        public IEnumerator Faltan_GoesInTheNextStopLabel_InTheReaderPath_AndThe10FooterStays()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return MapMarkersTests.OpenApp(58);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(map.LiveActive, Is.True);
            Assert.That(map.StopRemainingInLabel, Is.True, "la pregunta 2 de M2-3: se junta con el rótulo");
            Assert.That(map.StopRemainingShown, Is.False, "sin la placa de arriba a la izquierda");
            Assert.That(map.RemainingChipViewRect.width, Is.EqualTo(0f), "el encuadre de las mini cuias ya no la cuenta");
            int next = map.RemainingStopN();
            Assert.That(next, Is.EqualTo(6), "Anavilhanas, la parada siguiente con el 58");
            string place = UiText.StopName(boot.RiverStops!.At(next)!);
            string expected = UiText.MapStopLabelRemaining(place, RiverJourney.LevelsLeft(58));
            Assert.That(map.StopLabelText(next), Is.EqualTo(expected));
            StringAssert.Contains("faltan 3 niveles", expected);
            TMPro.TextMeshProUGUI label = map.StopLabelOf(next)!;
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing, Is.False, "el texto cabe");
            Assert.That(label.textInfo.lineCount, Is.EqualTo(2));
            // TalkBack: la mini cuia de la parada es un botón y su etiqueta son sus textos a la vista, el rótulo con «Faltan N» incluido
            // (A11yScanner; en el editor sin gráficos la pantalla real es pequeña y lo de abajo queda fuera del recorrido: se mira la
            // etiqueta del botón, que es lo que publica el lector en el teléfono).
            UnityEngine.UI.Button marker = label.GetComponentInParent<UnityEngine.UI.Button>();
            Assert.That(marker, Is.Not.Null);
            Assert.That(marker.enabled, Is.True);
            A11yItem item = A11yScanner.ButtonItem(marker);
            StringAssert.Contains("faltan 3 niveles", item.Label, "«Faltan N» sigue en el recorrido del lector, dentro del rótulo");
            StringAssert.Contains(place, item.Label);
            boot.A11y!.Refresh();
            Assert.That(host.Find("Faltan 3 niveles para " + place), Is.Null, "sin la placa suelta de M2-3");
            Assert.That(boot.CloseRiverMap(), Is.True);

            // Sin los pueblos (ui_map_towns apagada), el rótulo de la parada siguiente sigue llevando «Faltan N».
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapTowns = false };
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.StopRemainingInLabel, Is.True);
            Assert.That(map.StopLabelText(next), Is.EqualTo(expected));
            Assert.That(map.StopLabelText(5), Is.Empty, "sin los pueblos, solo el de la siguiente");
            Assert.That(boot.CloseRiverMap(), Is.True);

            // Con ui_map_v2 apagada: la placa de la 1.0, intacta.
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapV2 = false };
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.StopRemainingShown, Is.True);
            Assert.That(map.StopRemainingInLabel, Is.False);
            Assert.That(map.StopRemainingText, Is.EqualTo(UiText.MapStopRemaining(RiverJourney.LevelsLeft(58), place)));
            boot.CloseRiverMap();
        }

        [UnityTest]
        public IEnumerator Faltan_TowardsTheSource_KeepsThePlate_InsideTheMap_ClearOfAndes()
        {
            // Hacia la naciente (691-700) no hay parada siguiente que lleve el rótulo: la placa de M2-3 de respaldo [P].
            yield return MapMarkersTests.OpenApp(695);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(map.LiveActive, Is.True);
            Assert.That(map.StopRemainingInLabel, Is.False);
            Assert.That(map.StopRemainingShown, Is.True);
            Rect viewport = StopCardTests.CanvasRect(map.ViewportRect, map.Canvas);
            Rect chip = StopCardTests.CanvasRect(map.StopRemainingRect, map.Canvas);
            Assert.That(viewport.Contains(chip.min) && viewport.Contains(chip.max - new Vector2(0.01f, 0.01f)), Is.True, "la placa, en el Mapa");
            Assert.That(map.RemainingChipViewRect.width, Is.GreaterThan(0f), "el encuadre de las mini cuias la cuenta");
            if (map.AndesLabel != null && map.AndesLabel.gameObject.activeInHierarchy && map.AndesLabel.text.Length > 0)
            {
                Assert.That(chip.Overlaps(StopCardTests.CanvasRect(map.AndesLabel.rectTransform, map.Canvas)), Is.False, "«Andes ↑»");
            }

            map.Close();
        }

        [UnityTest]
        public IEnumerator Map10_BasinPlates_DodgeTheCanoeBoxComputedInTheBasin()
        {
            // Pregunta 5 de M2-3 (D-069): con ui_map_v2 apagada, la caja de la canoa con su mástil que esquivan las placas de la Cuenca
            // se calcula en la Cuenca (antes quedaba la de la vista Mundo).
            GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapV2 = false };
            yield return MapMarkersTests.OpenApp(201);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            Assert.That(map.LiveActive, Is.False);
            Rect world = map.CanoeGearBoxLocal;
            map.SetView(MapView.Basin);
            Canvas.ForceUpdateCanvases();
            Rect basin = map.CanoeGearBoxLocal;
            Vector2 canoe = map.Canoe.rectTransform.anchoredPosition;
            Assert.That(basin, Is.Not.EqualTo(world), "otra vista, otra caja");
            Assert.That(basin.Contains(canoe), Is.True, "la caja está donde se pinta la canoa en la Cuenca");
            Assert.That(map.SilhouetteBlockedAt(-1, basin), Is.True, "una placa ahí, bloqueada");
            map.Close();
        }

        [UnityTest]
        public IEnumerator WorldChange_TheFinalShot_HasCapiTheCrewAndTheFlag()
        {
            // Propuesta §5.5 y §6.3: en el viaje de mundo del banner (D-012), el plano final con Capi, la tripulación y la bandera.
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            GameScreen.BannerTravelOverride = true;
            try
            {
                foreach ((int level, bool toucan, bool tucuxi) in new[] { (41, true, false), (81, false, true) })
                {
                    string where = "banner del " + level;
                    yield return CardSetup.Load(level);
                    WorldBannerView banner = GameBootstrap.Current!.View!.WorldBannerView;
                    BannerTravelStrip strip = banner.TravelStrip;
                    Assert.That(strip.Visible && strip.CrewOn, Is.True, where);
                    strip.Apply(0f, false);
                    Assert.That(strip.ToucanShown || strip.TucuxiShown, Is.False, where + ": al salir, aún no (es el plano final)");
                    strip.Apply(float.MaxValue, false);
                    Assert.That(strip.ToucanShown, Is.EqualTo(toucan), where + ": el tucán en la proa");
                    Assert.That(strip.TucuxiShown, Is.EqualTo(tucuxi), where + ": el tucuxi junto al guía");
                    Assert.That(strip.CrewArt.Built, Is.True, where);
                    banner.HideNow();
                    Assert.That(strip.CrewArt.Built, Is.False, where + ": el atlas se suelta con la franja");
                }

                GameBootstrap.MapLiveConfigOverride = new MapLiveConfig { UiMapCrew = false };
                yield return CardSetup.Load(41);
                BannerTravelStrip off = GameBootstrap.Current!.View!.WorldBannerView.TravelStrip;
                off.Apply(float.MaxValue, false);
                Assert.That(off.CrewOn || off.ToucanShown || off.CrewArt.Built, Is.False, "ui_map_crew apagada: el banner de M2-3");
            }
            finally
            {
                GameScreen.BannerTravelOverride = null;
                LocSetUpFixture.ConfigureEs419();
            }
        }

        private static int AllocBlocks(Action action)
        {
            UnityEngine.Profiling.Recorder recorder = UnityEngine.Profiling.Recorder.Get("GC.Alloc");
            recorder.FilterToCurrentThread();
            recorder.enabled = false;
            recorder.enabled = true;
            action();
            recorder.enabled = false;
            int n = recorder.sampleBlockCount;
            recorder.CollectFromAllThreads();
            return n;
        }
    }
}

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de M2-4 (D-069) a 360 dp: el tucán a bordo, el tucuxi con Rosa, el chip «¡Se une al viaje!», el cameo del miquito y
    /// «Faltan N» en el rótulo de la parada siguiente, con un recorte ×1 alrededor de la canoa en las dos primeras. Los PNG quedan en
    /// <c>Logs/snapshots/es-419/m24_*.png</c>. Explícito: necesita gráficos (sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas del Mapa v2 (M2-4); necesitan dispositivo gráfico.")]
    public sealed class MapCrewSnapshotTests
    {
        private static readonly System.DateTime T0 = new System.DateTime(2026, 11, 7, 15, 0, 0, System.DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.MapLiveConfigOverride = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheCrew_TheChip_TheCameo_AndFaltanInTheLabel_At360dp()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedPxPerDp = 3f;

            // El tucán a bordo (jugando el 26) y el tucuxi con Rosa (jugando el 73), ya presentados.
            yield return Open(26, 25, 25);
            RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
            yield return SnapshotKit.Wait(1.0f);
            map.HoldCapiPose(CapiGesture.Rest, 0f, CapiEmotion.None);
            yield return SnapshotKit.Wait(0.2f);
            Assert.That(map.ToucanAboardShown, Is.True);
            SnapshotKit.Save(dir, "m24_tucan_a_bordo_360dp");
            SaveCrop(dir, "m24_tucan_a_bordo_360dp_zoom", map);
            GameBootstrap.Current!.CloseRiverMap();

            yield return Open(73, 72, 72);
            map = GameBootstrap.Current!.RiverMapView!;
            yield return SnapshotKit.Wait(1.0f);
            Assert.That(map.TucuxiAboardShown, Is.True);
            SnapshotKit.Save(dir, "m24_tucuxi_con_rosa_360dp");
            SaveCrop(dir, "m24_tucuxi_con_rosa_360dp_zoom", map);
            GameBootstrap.Current!.CloseRiverMap();

            // El chip, con la presentación del tucán (el Mapa no se vio desde el 20).
            yield return Open(22, 21, 20);
            map = GameBootstrap.Current!.RiverMapView!;
            for (int i = 0; i < 60 && !(map.CrewChipShown && !map.CrewArriving); i++)
            {
                yield return null;
            }

            yield return SnapshotKit.Wait(0.3f);
            Assert.That(map.CrewChipShown, Is.True);
            SnapshotKit.Save(dir, "m24_chip_360dp");
            GameBootstrap.Current!.CloseRiverMap();

            // El cameo del miquito en el viaje de Anavilhanas a Tefé.
            yield return Open(71, 70, 60);
            map = GameBootstrap.Current!.RiverMapView!;
            for (int i = 0; i < 400 && !map.CameoWaving; i++)
            {
                yield return null;
            }

            Assert.That(map.CameoWaving, Is.True);
            yield return SnapshotKit.Wait(0.4f);
            SnapshotKit.Save(dir, "m24_cameo_miquito_360dp");
            SaveCrop(dir, "m24_cameo_miquito_360dp_zoom", map, map.CameoRect);
            GameBootstrap.Current!.CloseRiverMap();

            // «Faltan N» en el rótulo de la parada siguiente (respuesta del dueño a la pregunta 2 de M2-3).
            yield return Open(58, 57, 57);
            map = GameBootstrap.Current!.RiverMapView!;
            yield return SnapshotKit.Wait(1.0f);
            Assert.That(map.StopRemainingInLabel, Is.True);
            SnapshotKit.Save(dir, "m24_faltan_rotulo_360dp");
            Debug.Log("AF_MAP m24: rótulo «" + map.StopLabelText(map.RemainingStopN()).Replace("\n", " / ") + "», arte "
                + ((map.LiveArt.TextureBytes + map.TownArt.TextureBytes + map.CapiArt.TextureBytes + map.CrewArt.TextureBytes) / 1024L)
                + " KiB (tripulación " + (map.CrewArt.TextureBytes / 1024L) + ")");
            GameBootstrap.Current!.CloseRiverMap();
        }

        private static IEnumerator Open(int play, int max, int seen)
        {
            AF.Save.Profile profile = AF.Economy.Wallet.NewProfile(new AF.Platform.ManualClock(T0));
            profile.Progress.NextLevel = play;
            profile.Progress.MaxLevel = max;
            profile.Progress.MapSeenLevel = seen;
            profile.Wallet.Seeds = 1250;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.AttachLobby(boot);
            yield return null;
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            SnapshotKit.AttachOverlay(boot.RiverMapView!.Canvas);
        }

        // Un recorte de 330 × 270 px alrededor de la canoa (o de at; a 3 px/dp: 110 × 90 dp).
        private static void SaveCrop(string dir, string name, RiverMapScreen map, RectTransform? at = null)
        {
            var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Assert.That(full.LoadImage(SnapshotKit.Capture()), Is.True);
            Camera cam = SnapshotKit.CaptureCamera!;
            Vector3 p = cam.WorldToScreenPoint((at != null ? at : map.Canoe.rectTransform).position);
            float sx = full.width / (float)cam.pixelWidth;
            float sy = full.height / (float)cam.pixelHeight;
            const int w = 330;
            const int h = 270;
            int x0 = Mathf.Clamp(Mathf.RoundToInt((p.x * sx) - (w * 0.5f)), 0, full.width - w);
            int y0 = Mathf.Clamp(Mathf.RoundToInt((p.y * sy) - (h * 0.35f)), 0, full.height - h);
            var crop = new Texture2D(w, h, TextureFormat.RGB24, false);
            crop.SetPixels(full.GetPixels(x0, y0, w, h));
            crop.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), crop.EncodeToPNG());
            UnityEngine.Object.Destroy(full);
            UnityEngine.Object.Destroy(crop);
        }
    }
}
