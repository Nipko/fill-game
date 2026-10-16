#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Viaje de mundo dentro del banner (D-013, punto 7; Q4-D; 02 §6, fila Banner de mundo; propuesta del Mapa §4.2, §5.3 y §6.1;
    /// tarea M3.6), en la escena con el catálogo y los datos del Mapa reales y el viaje encendido como en el juego
    /// (<see cref="GameScreen.BannerTravelOverride"/> = true; los demás tests lo apagan, <see cref="PlayModeHelpers.NoBannerTravelUnlessSet"/>):
    /// el 41, el 81 y el 141 llevan bajo la cabecera la pregunta del mundo y la franja del Mapa, con la canoa que recorre el tramo
    /// de la gran parada en ≤ 4 s y, en el 81 y el 141, la bandera de Colombia y la del Perú izándose en 800 ms; el 11 no (lo
    /// cuenta el Mapa del 10), ni ninguno con <c>ui_map_autopen</c> = <c>l10</c>; el primer toque completa el viaje y el siguiente
    /// cierra el banner; «Ver mapa» abre el Mapa encima del banner (<see cref="MapOpenSource.WorldChange"/>) y al cerrarlo vuelve
    /// a él; el guía se releva en Teotônio con un catálogo de prueba (P-M8); con Reducir movimiento, el final con un fundido de
    /// 150 ms; sin datos, el banner de siempre; en release (<see cref="MapArtRule"/> falso), sin bandera simplificada y con la
    /// oficial siempre; con el respaldo de 800 ms del arte, el viaje sigue sin el guía; el arte pedido antes con el viaje cuadra
    /// con el banner que se abre; sin asignaciones mientras viaja; y las mallas, en ≤ 8 ms. Salvo el del arte pedido antes, el
    /// arte del banner va en el acto (<see cref="CardSetup.Load"/>).
    /// </summary>
    public sealed class WorldBannerTravelTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // 360 × 780 dp con zona segura
            GameScreen.BannerTravelOverride = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.BannerTravelOverride = null;
            GameScreen.BannerAutopenOverride = null;
            GameScreen.BannerTravelDataOverride = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Travel_41_81_141_TheCanoeCrossesInUnder4s_WithTheQuestion_AndTheFlags()
        {
            foreach ((int level, int world, string? flag) in new (int, int, string?)[] { (41, 3, null), (81, 4, "CO"), (141, 5, "PE") })
            {
                string where = "nivel " + level;
                yield return CardSetup.Load(level);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                LevelFlow flow = boot.Flow!;
                WorldBannerView banner = view.WorldBannerView;
                Assert.That(flow.WorldBanner, Is.EqualTo(world), where);
                BannerTravel? travel = banner.Travel;
                Assert.That(travel, Is.Not.Null, where + ": con el viaje de mundo");
                RiverStop gate = boot.RiverStops!.StopOf(level)!;
                Assert.That(travel!.Stop, Is.SameAs(gate), where + ": el tramo de la gran parada");
                Assert.That(gate.Kind, Is.EqualTo(StopKind.Gate), where);
                Assert.That(travel.Route, Is.SameAs(boot.RiverRoutes!.At(gate.N)), where);
                Assert.That(travel.Flag, Is.EqualTo(flag), where + ": flag_raise");
                Assert.That(banner.TravelFlag, Is.EqualTo(flag), where + ": la bandera que se iza (en el Editor, la simplificada)");
                Assert.That(banner.TravelMs, Is.LessThanOrEqualTo(MapConfig.WorldTravelInBannerMaxMs), where + ": ≤ 4 s");
                Assert.That(banner.TravelMs, Is.EqualTo(WorldBannerView.TravelDelayMs + WorldBannerView.CanoeTravelMs + (flag != null ? MapConfig.FlagRaiseMs : 0f)), where);

                // La pregunta del mundo, sola, bajo la cabecera y encima de la franja: sin la plantilla de la cabecera del Mapa
                // (map_world_question), que repetía el nombre del mundo que ya dice la cabecera del banner.
                string question = Loc.Fact(gate.WorldQuestion!);
                Assert.That(question, Is.Not.Empty, where);
                Assert.That(banner.QuestionLabel.gameObject.activeInHierarchy, Is.True, where);
                Assert.That(banner.QuestionLabel.text, Is.EqualTo(question), where);
                Assert.That(banner.QuestionLabel.text, Does.Not.Contain(UiText.WorldName(world)), where + ": el nombre del mundo, solo en la cabecera");
                Assert.That(banner.MapButtonLabel.text, Is.EqualTo(UiText.MapOpen), where + ": «Ver mapa»");
                Assert.That(banner.RaycastTargets(), Is.EqualTo(new Graphic[] { banner.Dim, banner.MapButton.targetGraphic }), where + ": el velo y «Ver mapa»");

                // La sonda world_banner suma el viaje.
                string flagJson = flag != null ? "\"" + flag + "\"" : "null";
                Assert.That(view.LastWorldBannerProbe, Does.Contain("\"travel\":true,\"travel_ms\":" + Mathf.RoundToInt(banner.TravelMs).ToString(CultureInfo.InvariantCulture)
                    + ",\"flag\":" + flagJson + "}"), where);

                // La canoa sale de la parada anterior, recorre el tramo dentro de la franja y llega a la gran parada; la bandera, al final.
                BannerTravelStrip strip = banner.TravelStrip;
                Assert.That(strip.Visible, Is.True, where);
                Assert.That(strip.GlassCount, Is.GreaterThan(0), where + ": el vidrio de los ríos");
                Assert.That(strip.WaterCount, Is.GreaterThan(0), where + ": el agua que ya llena el Mapa");
                Assert.That(strip.Canoe.sprite, Is.Not.Null, where);
                AssertTravelLaidOut(view, where);
                float last = strip.CanoeFraction;
                Assert.That(last, Is.GreaterThanOrEqualTo(0f).And.LessThan(0.05f), where + ": recién salida");
                int steps = 0;
                while (strip.CanoeFraction < 1f)
                {
                    Assert.That(banner.TravelRunning, Is.True, where);
                    Assert.That(banner.Settled, Is.False, where + ": se mueve");
                    Assert.That(strip.FlagRaise, Is.EqualTo(0f), where + ": la bandera, al llegar");
                    PlayModeHelpers.TickExact(boot, 160);
                    Assert.That(strip.CanoeFraction, Is.GreaterThanOrEqualTo(last), where + ": nunca retrocede");
                    last = strip.CanoeFraction;
                    Vector2 c = strip.Canoe.rectTransform.anchoredPosition;
                    Rect box = strip.Root.rect;
                    Assert.That(Mathf.Abs(c.x) <= (box.width * 0.5f) - 8f && Mathf.Abs(c.y) <= (box.height * 0.5f) - 8f, Is.True, where + ": la canoa, en la franja: " + c);
                    Assert.That(++steps, Is.LessThanOrEqualTo(Mathf.CeilToInt(WorldBannerView.CanoeTravelMs / 160f) + 1), where + ": la canoa, en " + WorldBannerView.CanoeTravelMs + " ms");
                }

                Assert.That(steps, Is.GreaterThanOrEqualTo(Mathf.FloorToInt((WorldBannerView.CanoeTravelMs - 300f) / 160f)), where + ": no llega antes");
                if (flag != null)
                {
                    // El izado se ve: la bandera sale del pie del mástil y sube 17 dp por él, a mitad de recorrido a mitad de tiempo.
                    float raise0 = strip.FlagRaise;
                    float lift0 = strip.FlagLiftDp;
                    Assert.That(raise0, Is.LessThan(0.25f), where + ": el izado, recién empezado");
                    PlayModeHelpers.TickExact(boot, Mathf.RoundToInt((0.5f - raise0) * MapConfig.FlagRaiseMs));
                    float liftMid = strip.FlagLiftDp;
                    Assert.That(strip.FlagRaise, Is.EqualTo(0.5f).Within(0.03f), where + ": a 400 ms");
                    PlayModeHelpers.TickExact(boot, Mathf.CeilToInt(0.5f * MapConfig.FlagRaiseMs) + 40);
                    float liftEnd = strip.FlagLiftDp;
                    Assert.That(liftEnd - lift0, Is.GreaterThanOrEqualTo(12f), where + ": sube por el mástil (" + lift0 + " → " + liftEnd + " dp)");
                    float half = ProceduralArt.MapFlagHeightDp * 0.5f;
                    Assert.That(liftEnd, Is.EqualTo(BannerTravelStrip.MastHeightDp - half).Within(0.1f), where + ": en lo alto del mástil");
                    Assert.That(liftMid, Is.EqualTo((half + liftEnd) * 0.5f).Within(1.5f), where + ": a mitad del mástil a mitad del izado");
                    Assert.That(strip.Flag.rectTransform.localEulerAngles.z, Is.EqualTo(0f).Within(1e-3f), where + ": acaba quieta, sin ondear");
                }

                PlayModeHelpers.TickExact(boot, Mathf.CeilToInt(banner.TravelMs - WorldBannerView.TravelDelayMs - WorldBannerView.CanoeTravelMs) + 40);
                Assert.That(strip.CanoeFraction, Is.EqualTo(1f), where + ": en la gran parada");
                Assert.That(strip.FlagRaise, Is.EqualTo(flag != null ? 1f : 0f), where + ": izada en 800 ms");
                Assert.That(strip.Flag.gameObject.activeSelf, Is.EqualTo(flag != null), where);
                Assert.That(banner.TravelRunning, Is.False, where);
                Assert.That(banner.TravelDone, Is.True, where);
                Assert.That(banner.Settled, Is.True, where + ": quieto al terminar (guía 8)");
                AssertCanoeAtTheStop(boot, banner, where);
                AssertGuideWaitsClear(strip, where);
                for (int i = 0; i < strip.PassingCount; i++)
                {
                    Assert.That(strip.PassingAlpha(i), Is.EqualTo(0f), where + ": los nombres de paso, ya atrás");
                    Assert.That(InStrip(strip, strip.PassingBox(i), 0f), Is.True, where + ": «" + strip.Passing(i).text + "», dentro de la franja");
                    for (int j = 0; j < i; j++)
                    {
                        if (!strip.Overlaps(i, j))
                        {
                            continue; // nunca se ven a la vez
                        }

                        Assert.That(BoxOverlapDp(strip.PassingBox(i), strip.PassingBox(j)), Is.EqualTo(0f), where + ": «" + strip.Passing(i).text + "» y «"
                            + strip.Passing(j).text + "», sin pisarse");
                    }
                }

                // El banner sigue sin cerrarse solo; un toque lo cierra como siempre.
                PlayModeHelpers.Step(boot, 1000);
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), where);
                yield return null;
                WorldBannerSetup.Tap(banner);
                Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True, where + ": con el viaje terminado, un toque cierra");
                PlayModeHelpers.Step(boot, 400);
                Assert.That(banner.Visible, Is.False, where);
                Assert.That(strip.Visible, Is.False, where + ": la franja, fuera con el banner");
            }
        }

        [UnityTest]
        public IEnumerator Travel_FitsTheSafeArea_At100And130_In780_16x9_AndTheCutout()
        {
            // El viaje es un caso más de la regla del banner: la fuente hasta el 130 % sin desbordes ni recortes (02 §12; D-015,
            // pregunta 43; T52) en 360 × 780 dp, en 16:9 (02 §5; GameScreen lo maqueta en 438,75 × 780 dp) y con el recorte de 48 dp
            // arriba. La tarjeta con viaje crece (la pregunta, la franja de 112 dp y «Ver mapa»): dentro de la zona segura, sin
            // solapes, a ≥ 12 sp y con los textos enteros, en el 41, el 81 y el 141.
            var report = new System.Text.StringBuilder();
            float worst = float.MaxValue;
            string worstWhere = string.Empty;
            foreach ((string name, Vector4 screen) in new[] { ("360 × 780", HudLabelTests.Screen780), ("16:9", HudLabelTests.Screen640), ("recorte de 48 dp", HudLabelTests.Screen780Cutout) })
            {
                GameScreen.SimulatedScreen = screen;
                foreach (int level in new[] { 41, 81, 141 })
                {
                    yield return CardSetup.Load(level);
                    GameScreen view = GameBootstrap.Current!.View!;
                    WorldBannerView banner = view.WorldBannerView;
                    Assert.That(banner.Travel, Is.Not.Null, "nivel " + level + " en " + name + ": con el viaje");
                    RectTransform root = (RectTransform)view.Canvas.transform;
                    foreach (float scale in new[] { 1f, 1.3f })
                    {
                        string where = "nivel " + level + " al " + (scale * 100f).ToString("0", CultureInfo.InvariantCulture) + " % en " + name;
                        view.BannerFontScale = scale;
                        Assert.That(banner.FontScale, Is.EqualTo(scale), where);
                        Assert.That(banner.TitleLabel.fontSize, Is.EqualTo(WorldBannerView.TitleSp * scale).Within(0.01f), where + ": la escala, aplicada");
                        AssertTravelLaidOut(view, where);
                        Canvas.ForceUpdateCanvases();
                        Rect panel = VictorySetup.CanvasRect(banner.Panel, root);
                        float slack = Mathf.Min(panel.yMin - view.SafeTopDp, view.CardCanvasHeight - view.CardSafeBottom - panel.yMax);
                        report.Append(where).Append(": ").Append(panel.height.ToString("0", CultureInfo.InvariantCulture)).Append(" dp de alto, ")
                            .Append(slack.ToString("0", CultureInfo.InvariantCulture)).Append(" dp de aire; ");
                        if (slack < worst)
                        {
                            worst = slack;
                            worstWhere = where;
                        }
                    }

                    view.BannerFontScale = 1f;
                }
            }

            Assert.That(worst, Is.GreaterThanOrEqualTo(8f), "8 dp de aire con la zona segura como mínimo (" + worstWhere + "): " + report);
            TestContext.WriteLine("AF_M36 travel_fit " + report);
        }

        [UnityTest]
        public IEnumerator Travel_Not11_NorWithAutopenL10()
        {
            // El 11 nunca: su viaje lo cuenta el Mapa del 10 (D-013, punto 7). El banner de siempre: sin pregunta, sin franja y sin
            // «Ver mapa»; el velo es lo único que recibe toques.
            yield return CardSetup.Load(11);
            GameBootstrap boot = GameBootstrap.Current!;
            AssertBannerOfToday(boot, 11, "el 11");

            // Con ui_map_autopen = l10, ningún banner lleva el viaje.
            GameScreen.BannerAutopenOverride = MapConfig.AutopenL10;
            yield return CardSetup.Load(41);
            AssertBannerOfToday(GameBootstrap.Current!, 41, "41 con autopen = l10");
            Assert.That(MapMoments.TravelInBanner(41, MapConfig.AutopenL10World), Is.True);
            Assert.That(MapMoments.TravelInBanner(11, MapConfig.AutopenL10World), Is.False);
        }

        [UnityTest]
        public IEnumerator Travel_FirstTapCompletes_TheNextCloses()
        {
            yield return CardSetup.Load(81);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = boot.View!.WorldBannerView;
            BannerTravelStrip strip = banner.TravelStrip;
            PlayModeHelpers.TickExact(boot, 700);
            Assert.That(banner.TravelRunning, Is.True);
            Assert.That(strip.CanoeFraction, Is.GreaterThan(0f).And.LessThan(1f), "a medio tramo");

            // El primer toque completa: la canoa en Leticia, la bandera de Colombia izada y todo quieto; el banner sigue.
            yield return null;
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.WorldBannerClosing, Is.False, "no cierra");
            Assert.That(banner.Visible && !banner.Dismissing, Is.True);
            Assert.That(banner.TravelRunning, Is.False);
            Assert.That(banner.TravelDone, Is.True);
            Assert.That(strip.CanoeFraction, Is.EqualTo(1f));
            Assert.That(strip.FlagRaise, Is.EqualTo(1f));
            Assert.That(banner.Settled, Is.True, "quieto en el acto");
            Assert.That(banner.RosaRise, Is.EqualTo(1f).Within(1e-3f), "la entrada, completa también");
            Assert.That(banner.PawAngle, Is.EqualTo(WorldBannerView.PawUpDeg).Within(0.5f));
            AssertCanoeAtTheStop(boot, banner, "completado");
            PlayModeHelpers.Step(boot, 200);
            Assert.That(strip.CanoeFraction, Is.EqualTo(1f), "no vuelve a empezar");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));

            // El siguiente cierra, como siempre.
            yield return null;
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True);
            Assert.That(boot.View.LastWorldBannerClosedProbe, Is.EqualTo("{\"level\":81,\"world\":4,\"next\":\"intro\"}"));
            PlayModeHelpers.Step(boot, 400);
            Assert.That(banner.Visible, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [UnityTest]
        public IEnumerator Travel_SeeMap_OpensTheMapOverTheBanner_AndComesBack()
        {
            yield return CardSetup.Load(141);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            PlayModeHelpers.TickExact(boot, 500);
            Assert.That(banner.TravelRunning, Is.True);

            // «Ver mapa», con un dedo: completa el viaje y abre el Mapa encima del banner, desde world_change.
            yield return null;
            PressMapButton(banner);
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.Visible, Is.True, "el Mapa, encima");
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.WorldChange));
            Assert.That(view.BannerMapOpened, Is.EqualTo(1));
            Assert.That(banner.Visible && !banner.Dismissing, Is.True, "el banner sigue debajo");
            Assert.That(banner.TravelDone, Is.True, "el viaje, completado");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBannerClosing, Is.False);
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(banner.Visible && flow.Phase == FlowPhase.LevelCard, Is.True, "con el Mapa abierto, nada sigue");
            Assert.That(view.Intro.TimeMs, Is.EqualTo(0f));

            // Al cerrar el Mapa (←, Atrás o «Jugar»), de vuelta al banner, quieto; un toque lo cierra.
            Assert.That(boot.CloseRiverMap(), Is.True);
            Assert.That(map.Visible, Is.False);
            Assert.That(banner.Visible && banner.CardShown && !banner.Dismissing, Is.True, "de vuelta al banner");
            Assert.That(banner.Settled, Is.True);
            yield return null;
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True, "el toque cierra el banner");
            PlayModeHelpers.Step(boot, 400);
            Assert.That(banner.Visible, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [UnityTest]
        public IEnumerator Travel_GuideRelayAtTeotonio_WithATestCatalog()
        {
            // P-M8 (D-023, punto 19): el guía se deduce del mundo (el bufeo boliviano en los mundos 7 y 8) y el relevo es el punto de
            // paso «teotonio» del tramo. Catálogo de prueba: el 41 llega a una gran parada del mundo 7 desde una del 6, y su tramo pasa
            // por «teotonio» (a mitad del Amazonas del tramo real).
            yield return CardSetup.Load(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.RiverGeometry!(out RiverMap? realMap, out RiverRoutes? realRoutes), Is.True, "la geometría del Mapa");
            GameScreen.BannerTravelDataOverride = RelayData(realMap!, realRoutes!, boot.RiverStops!, out float relayS);
            boot.Flow!.Load(41);
            WorldBannerView banner = view.WorldBannerView;
            BannerTravel travel = banner.Travel!;
            Assert.That(travel, Is.Not.Null);
            Assert.That(travel.GuideFrom, Is.EqualTo(RiverJourney.GuideRosa));
            Assert.That(travel.GuideTo, Is.EqualTo(RiverJourney.GuideBufeo));
            Assert.That(travel.HasRelay, Is.True);
            Assert.That(travel.RelayFraction, Is.GreaterThan(0.1f).And.LessThan(0.9f), "en Teotônio, no al salir");
            BannerTravelStrip strip = banner.TravelStrip;
            Assert.That(strip.GuideA.gameObject.activeSelf && strip.GuideB.gameObject.activeSelf, Is.True, "los dos guías");
            Assert.That(strip.GuideA.color, Is.EqualTo(Color.white), "Rosa, con su color");
            Assert.That(strip.GuideB.color, Is.EqualTo(BannerTravelStrip.BufeoTint), "el bufeo, con otra paleta");
            Vector2 relay = strip.StripPoint(travel.PointAt(travel.RelayFraction));
            float relayDp = travel.RelayFraction * strip.TotalDp;

            bool sawBefore = false;
            bool sawAfter = false;
            bool sawRamp = false;
            for (int ms = 0; ms < 4200; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                float arc = strip.CanoeFraction * strip.TotalDp;
                if (strip.GuideAAlpha > 0f && strip.GuideBAlpha > 0f)
                {
                    // A mitad del relevo, los dos guías a la vista: sin pisarse (≤ 4 dp) y dentro de la franja.
                    sawRamp = true;
                    Assert.That(GuideOverlapDp(strip.GuideA, strip.GuideB), Is.LessThanOrEqualTo(4f), "los dos guías, sin pisarse, a " + ms + " ms");
                    Assert.That(InStrip(strip, strip.GuideA.rectTransform, 0f), Is.True, "Rosa, que se queda, dentro de la franja");
                    Assert.That(InStrip(strip, strip.GuideB.rectTransform, 0f), Is.True, "el bufeo, dentro de la franja");
                }

                if (arc < relayDp - 1f)
                {
                    sawBefore = true;
                    Assert.That(strip.GuideAAlpha, Is.EqualTo(1f), "antes de Teotônio, Rosa delante");
                    Assert.That(strip.GuideBAlpha, Is.EqualTo(0f));
                    Assert.That(travel.GuideAt(strip.CanoeFraction), Is.EqualTo(RiverJourney.GuideRosa));
                }
                else if (arc > relayDp + BannerTravelStrip.RelayRampDp + 1f)
                {
                    sawAfter = true;
                    Assert.That(strip.GuideAAlpha, Is.EqualTo(0f), "Rosa se queda en los rápidos");
                    Assert.That(strip.GuideBAlpha, Is.EqualTo(1f), "el bufeo sigue delante");
                    Assert.That(Vector2.Distance(strip.GuideA.rectTransform.anchoredPosition, relay), Is.LessThan(0.5f), "Rosa, en Teotônio");
                    Assert.That(travel.GuideAt(strip.CanoeFraction), Is.EqualTo(RiverJourney.GuideBufeo));
                }
            }

            Assert.That(sawBefore && sawAfter, Is.True, "el relevo, a mitad del viaje");
            Assert.That(sawRamp, Is.True, "a mitad del relevo, con los dos guías");
            AssertGuideWaitsClear(strip, "el bufeo, al llegar");
            Assert.That(relayS, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator Travel_ReduceMotion_TheFinalStateWithA150msFade()
        {
            yield return CardSetup.Load(140);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            flow.ReduceMotion = true;
            flow.Load(141);
            WorldBannerView banner = boot.View!.WorldBannerView;
            BannerTravelStrip strip = banner.TravelStrip;
            Assert.That(banner.Travel, Is.Not.Null);
            Assert.That(banner.TravelMs, Is.EqualTo((float)MapConfig.ReducedMotionFadeMs), "solo el fundido");
            Assert.That(boot.View.LastWorldBannerProbe, Does.Contain("\"reduce_motion\":true,\"travel\":true,\"travel_ms\":150,\"flag\":\"PE\""));
            Assert.That(strip.CanoeFraction, Is.EqualTo(1f), "sin viaje: la canoa ya en Iquitos");
            Assert.That(strip.FlagRaise, Is.EqualTo(1f), "la bandera, ya izada");
            Assert.That(strip.Root.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f), "la franja aún sin fundir");
            PlayModeHelpers.TickExact(boot, 75);
            Assert.That(strip.Root.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.5f).Within(0.01f), "a mitad del fundido de 150 ms");
            PlayModeHelpers.TickExact(boot, 150);
            Assert.That(strip.Root.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(strip.CanoeFraction, Is.EqualTo(1f));
            Assert.That(banner.TravelDone && banner.Settled, Is.True);

            // Con el viaje ya hecho, el primer toque cierra.
            yield return null;
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True);
        }

        [UnityTest]
        public IEnumerator Travel_WithoutData_TheBannerOfToday()
        {
            // Sin el tramo de la gran parada (sin largo), el banner de siempre; sin datos, también.
            Assert.That(BannerTravel.For(41, null, null, null), Is.Null);
            yield return CardSetup.Load(40);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.RiverGeometry!(out RiverMap? map, out RiverRoutes? routes), Is.True);
            Assert.That(BannerTravel.For(41, map, routes, null), Is.Null);
            Assert.That(BannerTravel.For(42, map, routes, boot.RiverStops), Is.Null, "el 42 no abre mundo");
            Assert.That(BannerTravel.For(1, map, routes, boot.RiverStops), Is.Null, "el 1, sin banner");
            GameScreen.BannerTravelDataOverride = new BannerTravelData(map!, NoLegRoutes(routes!, 4), boot.RiverStops!);
            boot.Flow!.Load(41);
            AssertBannerOfToday(boot, 41, "41 sin el tramo");
        }

        [UnityTest]
        public IEnumerator Travel_Release_NoSimplifiedFlag_TheOfficialPngAlways()
        {
            // En release (MapArtRule falso) no sale la bandera simplificada: la canoa viaja sin ella (y el viaje, sin el izado).
            MapArtRule.ForceProvisional = false;
            yield return CardSetup.Load(81);
            GameBootstrap boot = GameBootstrap.Current!;
            WorldBannerView banner = boot.View!.WorldBannerView;
            Assert.That(banner.Travel, Is.Not.Null, "el viaje sí");
            Assert.That(banner.Travel!.Flag, Is.EqualTo("CO"), "flag_raise de Leticia");
            Assert.That(banner.TravelFlag, Is.Null, "sin la bandera simplificada");
            Assert.That(banner.TravelStrip.Flag.gameObject.activeSelf || banner.TravelStrip.Mast.gameObject.activeSelf, Is.False);
            Assert.That(banner.TravelMs, Is.EqualTo(WorldBannerView.TravelDelayMs + WorldBannerView.CanoeTravelMs), "sin izado");
            Assert.That(boot.View.LastWorldBannerProbe, Does.Contain("\"travel\":true").And.Contain("\"flag\":null}"));

            // La regla: el PNG oficial (map_flag_<iso2>) manda siempre; la simplificada, solo con el provisional permitido y solo en
            // BR, CO, PE, EC y BO.
            ProceduralArt art = boot.View.CardArt;
            var tex = new Texture2D(48, 33);
            Sprite official = Sprite.Create(tex, new Rect(0, 0, 48, 33), new Vector2(0f, 0.5f));
            try
            {
                Assert.That(art.MapFlagFor("CO", official, false), Is.SameAs(official), "la oficial, también en release");
                Assert.That(art.MapFlagFor("CO", null, false), Is.Null, "en release, sin la simplificada");
                Assert.That(art.MapFlagFor("VE", null, true), Is.Null, "sin simplificada de Venezuela");
                foreach (string iso in ProceduralArt.SimplifiedFlags)
                {
                    Sprite? simplified = art.MapFlagFor(iso, null, true);
                    Assert.That(simplified, Is.Not.Null, iso);
                    Assert.That(simplified!.rect.width * 100f / simplified.pixelsPerUnit, Is.EqualTo(ProceduralArt.MapFlagWidthDp).Within(0.5f), iso);
                    Assert.That(art.MapFlagFor(iso, null, true), Is.SameAs(simplified), iso + ": una sola vez");
                }

                Assert.That(ProceduralArt.MapFlagSlot("CO"), Is.EqualTo("map_flag_co"));
                Assert.That(ArtSlots.IsKnown(ProceduralArt.MapFlagSlot("PE")), Is.True);
                Assert.That(art.MapFlagsBuilt, Is.True);
            }
            finally
            {
                Object.Destroy(official);
                Object.Destroy(tex);
            }
        }

        [UnityTest]
        public IEnumerator Travel_ArtFallback_StillTravels_WithoutTheGuide()
        {
            // El respaldo de 800 ms del arte (D-012) intacto: sin arte, la tarjeta sin Rosa ni Capi, y el viaje sigue con la canoa
            // y la bandera, sin el guía (su sprite es el de Rosa).
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtFaultOverride = BannerArtFault.QueueRejected;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(80, new MemoryProgressStore(80, 79));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.CardArt.ReleaseBannerArt();
            if (boot.Flow!.Phase == FlowPhase.LevelCard)
            {
                boot.TapAnywhere();
            }

            boot.Flow.Load(81);
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(banner.CardShown, Is.True, "sin cola, en el acto");
            Assert.That(banner.Fallback, Is.EqualTo(WorldBannerView.FallbackQueue));
            Assert.That(banner.Rosa.gameObject.activeInHierarchy || banner.Paw.gameObject.activeInHierarchy, Is.False, "sin Rosa ni Capi");
            Assert.That(banner.Travel, Is.Not.Null, "el viaje, también sin el arte de las mascotas");
            Assert.That(banner.TravelStrip.GuideA.gameObject.activeSelf || banner.TravelStrip.GuideB.gameObject.activeSelf, Is.False, "sin guía");
            Assert.That(banner.TravelStrip.Canoe.sprite, Is.Not.Null, "la canoa de Capi del Mapa");
            PlayModeHelpers.TickExact(boot, Mathf.CeilToInt(banner.TravelMs) + 40);
            Assert.That(banner.TravelStrip.CanoeFraction, Is.EqualTo(1f));
            Assert.That(banner.TravelStrip.FlagRaise, Is.EqualTo(1f));
            Assert.That(banner.Settled, Is.True);
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"fallback\":\"queue\""));
            yield return null;
            WorldBannerSetup.Tap(banner);
            Assert.That(boot.Flow.WorldBannerClosing, Is.True);
        }

        [UnityTest]
        public IEnumerator Travel_ArtRequestedWhenPlayOffers81_EntersAtOnce()
        {
            // El arte pedido antes con el viaje (la tarjeta más alta) es el mismo que pide el banner al abrirse: entra en el acto.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            Profile profile = FunnelSetup.Profile(1240);
            profile.Progress.NextLevel = 81;
            profile.Progress.MaxLevel = 80;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            Assert.That(view.CardArt.BannerArtPending, Is.True, "«Jugar · Nivel 81»: el arte, pedido");
            Assert.That(view.CardArt.WaitForBannerArt(30000), Is.True);
            Assert.That(boot.PressPlay(), Is.True);
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(banner.Travel, Is.Not.Null);
            Assert.That(banner.CardShown && banner.ArtReady, Is.True, "entra en el acto, con su arte");
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"level\":81").And.Contain("\"fallback\":\"none\""));
        }

        [UnityTest]
        public IEnumerator Travel_NoAllocWhileTraveling()
        {
            // 08 §5.4: el viaje (la canoa, los guías, los nombres de paso y la bandera) no asigna memoria por fotograma.
            yield return CardSetup.Load(81);
            GameBootstrap boot = GameBootstrap.Current!;
            WorldBannerView banner = boot.View!.WorldBannerView;
            Assert.That(banner.TravelRunning, Is.True);
            banner.Tick(PlayModeHelpers.FrameMs);
            void Run20()
            {
                for (int i = 0; i < 20; i++)
                {
                    banner.Tick(PlayModeHelpers.FrameMs);
                }
            }

            for (int k = 0; k < 10; k++)
            {
                Assert.That(Run20, Is.Not.AllocatingGCMemory(), "viaje, tramo " + k);
            }

            Assert.That(banner.TravelStrip.FlagRaise, Is.GreaterThan(0f), "también el izado");
        }

        [UnityTest]
        public IEnumerator Travel_MeshesBuildIn8ms()
        {
            // Las mallas de la franja (la tierra y las cintas) se construyen en ≤ 8 ms, con el lienzo pintado. La primera vez cuenta
            // el JIT; se mide la mejor de tres rondas, cada viaje con el encuadre del anterior distinto (todo se reconstruye).
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            WorldBannerView banner = boot.View!.WorldBannerView;
            BannerTravelStrip strip = banner.TravelStrip;
            int[] levels = { 41, 81, 141 };
            var best = new double[levels.Length];
            var glass = new int[levels.Length];
            var water = new int[levels.Length];
            var land = new int[levels.Length];
            for (int i = 0; i < best.Length; i++)
            {
                best[i] = double.MaxValue;
            }

            for (int round = 0; round < 3; round++)
            {
                for (int i = 0; i < levels.Length; i++)
                {
                    BannerTravel travel = BannerTravel.For(levels[i], boot.RiverMapData, boot.RiverRoutes, boot.RiverStops)!;
                    var watch = Stopwatch.StartNew();
                    strip.Build(travel, false);
                    Canvas.ForceUpdateCanvases();
                    watch.Stop();
                    best[i] = System.Math.Min(best[i], watch.Elapsed.TotalMilliseconds);
                    glass[i] = strip.GlassCount;
                    water[i] = strip.WaterCount;
                    land[i] = strip.LandVertexCount;
                }
            }

            var report = new System.Text.StringBuilder();
            for (int i = 0; i < levels.Length; i++)
            {
                report.Append(levels[i]).Append(": ").Append(best[i].ToString("0.00", CultureInfo.InvariantCulture)).Append(" ms, ")
                    .Append(glass[i]).Append(" cintas de vidrio, ").Append(water[i]).Append(" de agua, ").Append(land[i]).Append(" vértices de tierra; ");
            }

            for (int i = 0; i < levels.Length; i++)
            {
                Assert.That(best[i], Is.LessThanOrEqualTo(8.0), "nivel " + levels[i] + ": " + report);
                Assert.That(land[i], Is.GreaterThan(0), "la tierra");
            }

            Debug.Log("AF_M36 travel_build " + report);
            TestContext.WriteLine("AF_M36 travel_build " + report);
        }

        // ---------------------------------------------------------------------------------------------------------- ayudas

        // El banner de siempre (D-012): sin viaje, sin pregunta, sin franja y sin «Ver mapa»; el velo, lo único que recibe toques; la
        // sonda con travel false.
        private static void AssertBannerOfToday(GameBootstrap boot, int level, string where)
        {
            WorldBannerView banner = boot.View!.WorldBannerView;
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(level), where);
            Assert.That(banner.Visible, Is.True, where);
            Assert.That(banner.Travel, Is.Null, where + ": sin viaje");
            Assert.That(banner.TravelMs, Is.EqualTo(0f), where);
            Assert.That(banner.TravelRunning, Is.False, where);
            Assert.That(banner.QuestionLabel.gameObject.activeSelf, Is.False, where + ": sin pregunta");
            Assert.That(banner.TravelStrip.Visible, Is.False, where + ": sin franja");
            Assert.That(banner.MapButton.gameObject.activeSelf, Is.False, where + ": sin «Ver mapa»");
            Assert.That(banner.RaycastTargets(), Is.EqualTo(new Graphic[] { banner.Dim }), where);
            Assert.That(boot.View.LastWorldBannerProbe, Does.EndWith("\"travel\":false,\"travel_ms\":0,\"flag\":null}"), where);
        }

        // La canoa terminó en la gran parada: su punto en la capa sin girar es el de la parada (route.At) en el Mapa girado.
        private static void AssertCanoeAtTheStop(GameBootstrap boot, WorldBannerView banner, string where)
        {
            BannerTravelStrip strip = banner.TravelStrip;
            BannerTravel travel = banner.Travel!;
            Vector2 stop = RiverMapScreen.PlacePoint(travel.Map, travel.Route.At);
            Assert.That(Vector2.Distance(travel.PointAt(1f), stop), Is.LessThan(1e-3f), where + ": el tramo acaba en la parada");
            Vector2 local = strip.StripPoint(stop);
            Assert.That(Vector2.Distance(strip.Canoe.rectTransform.anchoredPosition, local), Is.LessThan(0.5f), where);

            // El mismo punto por el Mapa girado (MapSpace con el origen en el centro de la franja, 90° antihorario: norte arriba).
            float dpu = strip.DpPerUnit;
            Vector2 c = strip.MapCenter;
            var inContent = new Vector3((stop.x - c.x) * dpu, (c.y - stop.y) * dpu, 0f);
            Vector3 viaMap = strip.Content.TransformPoint(inContent);
            Vector3 viaOverlay = strip.Overlay.TransformPoint(new Vector3(local.x, local.y, 0f));
            float scale = strip.Content.lossyScale.x;
            Assert.That(Vector3.Distance(viaMap, viaOverlay) / scale, Is.LessThan(0.5f), where + ": la canoa, sobre el río dibujado");
        }

        // La franja, la pregunta y «Ver mapa» dentro del marco, en orden y sin pisarse con la cabecera, la viñeta y el lugar; textos
        // enteros a ≥ 12 sp y la pregunta en 2 líneas como mucho; la tarjeta, dentro de la zona segura.
        private static void AssertTravelLaidOut(GameScreen view, string where)
        {
            Canvas.ForceUpdateCanvases();
            WorldBannerView banner = view.WorldBannerView;
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect panel = VictorySetup.CanvasRect(banner.Panel, root);
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp), where + ": bajo la zona segura");
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom), where + ": sobre la zona segura");
            Rect title = VictorySetup.CanvasRect(banner.TitleLabel.rectTransform, root);
            Rect question = VictorySetup.CanvasRect(banner.QuestionLabel.rectTransform, root);
            Rect strip = VictorySetup.CanvasRect(banner.TravelStrip.Root, root);
            Rect scene = VictorySetup.CanvasRect(banner.Scene, root);
            Rect place = VictorySetup.CanvasRect(banner.PlaceLabel.rectTransform, root);
            Rect button = VictorySetup.CanvasRect((RectTransform)banner.MapButton.transform, root);
            foreach ((string name, Rect r) in new[] { ("pregunta", question), ("franja", strip), ("Ver mapa", button) })
            {
                Assert.That(r.xMin >= panel.xMin + 7.5f && r.xMax <= panel.xMax - 7.5f && r.yMin >= panel.yMin + 7.5f && r.yMax <= panel.yMax - 7.5f, Is.True,
                    where + ": " + name + " dentro del marco: " + r + " en " + panel);
            }

            Assert.That(strip.width, Is.EqualTo(WorldBannerView.StripWidthDp).Within(0.5f), where + ": 296 dp");
            Assert.That(strip.height, Is.EqualTo(WorldBannerView.StripHeightDp).Within(0.5f), where + ": 112 dp");
            Assert.That(title.yMax, Is.LessThanOrEqualTo(question.yMin + 0.5f), where + ": la cabecera, sobre la pregunta");
            Assert.That(question.yMax, Is.LessThanOrEqualTo(strip.yMin + 0.5f), where + ": la pregunta, sobre la franja");
            Assert.That(strip.yMax, Is.LessThanOrEqualTo(scene.yMin + 0.5f), where + ": la franja, sobre la viñeta");
            Assert.That(scene.yMax, Is.LessThanOrEqualTo(place.yMin + 0.5f), where + ": la viñeta, sobre el lugar");
            Assert.That(place.yMax, Is.LessThanOrEqualTo(button.yMin + 0.5f), where + ": «Ver mapa», bajo el lugar");
            Assert.That(button.height, Is.GreaterThanOrEqualTo(WorldBannerView.MinTouchDp - 0.5f), where + ": 48 dp táctiles");
            foreach (TextMeshProUGUI t in new[] { banner.TitleLabel, banner.QuestionLabel, banner.PlaceLabel, banner.MapButtonLabel })
            {
                t.ForceMeshUpdate(true, true);
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(12f), where + ": «" + t.text + "» ≥ 12 sp");
                Assert.That(t.textInfo.characterCount, Is.EqualTo(t.GetParsedText().Length), where + ": «" + t.text + "» entero");
                Assert.That(t.isTextTruncated, Is.False, where + ": «" + t.text + "» sin recortes");
                Assert.That(t.preferredHeight, Is.LessThanOrEqualTo(t.rectTransform.rect.height + 0.5f), where + ": «" + t.text + "» cabe en alto");
            }

            Assert.That(banner.QuestionLabel.textInfo.lineCount, Is.LessThanOrEqualTo(2), where + ": la pregunta, en 2 líneas como mucho");
        }

        // Al llegar, el guía espera sin tapar a Capi en la canoa (≤ 4 dp de solape con su caja) y dentro de la franja.
        private static void AssertGuideWaitsClear(BannerTravelStrip strip, string where)
        {
            Image guide = strip.GuideB.gameObject.activeSelf && strip.GuideBAlpha > 0f ? strip.GuideB : strip.GuideA;
            if (!guide.gameObject.activeSelf)
            {
                return; // sin arte, sin guía
            }

            float overlap = GuideOverlapDp(guide, strip.Canoe);
            Assert.That(overlap, Is.LessThanOrEqualTo(4f), where + ": el guía (" + guide.rectTransform.anchoredPosition + ") no tapa a Capi en la canoa ("
                + strip.Canoe.rectTransform.anchoredPosition + ")");
            Assert.That(InStrip(strip, guide.rectTransform, 0f), Is.True, where + ": el guía, dentro de la franja");
        }

        // Solape de las cajas de dos piezas de la franja (dp: el menor de los dos lados del solape; 0 si no se tocan).
        internal static float GuideOverlapDp(Image a, Image b)
        {
            return BoxOverlapDp(a.rectTransform, b.rectTransform);
        }

        private static float BoxOverlapDp(RectTransform a, RectTransform b)
        {
            Rect ra = BoxOf(a);
            Rect rb = BoxOf(b);
            float x = Mathf.Min(ra.xMax, rb.xMax) - Mathf.Max(ra.xMin, rb.xMin);
            float y = Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin);
            return x > 0f && y > 0f ? Mathf.Min(x, y) : 0f;
        }

        // La caja de rt (en la capa sin girar de la franja, origen en el centro) cabe en la franja con margin dp.
        internal static bool InStrip(BannerTravelStrip strip, RectTransform rt, float margin)
        {
            Rect box = BoxOf(rt);
            Rect r = strip.Root.rect;
            float hx = (r.width * 0.5f) - margin + 0.01f;
            float hy = (r.height * 0.5f) - margin + 0.01f;
            return box.xMin >= -hx && box.xMax <= hx && box.yMin >= -hy && box.yMax <= hy;
        }

        // La caja de una pieza de la capa sin girar (anclada en el centro), con su pivote y su escala.
        private static Rect BoxOf(RectTransform rt)
        {
            var size = new Vector2(rt.sizeDelta.x * Mathf.Abs(rt.localScale.x), rt.sizeDelta.y * Mathf.Abs(rt.localScale.y));
            Vector2 p = rt.anchoredPosition;
            return new Rect(p.x - (rt.pivot.x * size.x), p.y - (rt.pivot.y * size.y), size.x, size.y);
        }

        // Ms desde que entra la tarjeta en que la canoa llega a la fracción fraction del tramo (la inversa de Ease.InOut).
        internal static float MsAtFraction(float fraction)
        {
            float lo = 0f;
            float hi = 1f;
            for (int i = 0; i < 40; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Ease.InOut(mid) < fraction)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return WorldBannerView.TravelDelayMs + (lo * WorldBannerView.CanoeTravelMs);
        }

        // «Ver mapa» con un dedo, por el EventSystem: el primer impacto en el centro del botón es el botón (no el velo).
        private static void PressMapButton(WorldBannerView banner)
        {
            EventSystem events = EventSystem.current;
            Assert.That(events, Is.Not.Null, "EventSystem de la escena");
            Canvas canvas = banner.Root.GetComponentInParent<Canvas>().rootCanvas;
            Camera? cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            GameScreen? view = GameBootstrap.Current != null ? GameBootstrap.Current.View : null;
            float scale = canvas.scaleFactor;
            if (cam == null && view != null)
            {
                float fit = Mathf.Min(Screen.width / view.CanvasWidth, Screen.height / view.CardCanvasHeight);
                if (fit < scale)
                {
                    canvas.scaleFactor = fit; // en batchmode la pantalla real es más pequeña que la maqueta
                }
            }

            var hits = new List<RaycastResult>();
            var data = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
            try
            {
                Canvas.ForceUpdateCanvases();
                var rt = (RectTransform)banner.MapButton.transform;
                data.position = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
                events.RaycastAll(data, hits);
                Assert.That(hits.Count, Is.GreaterThan(0), "el toque da en algo");
                Assert.That(hits[0].gameObject.transform.IsChildOf(rt), Is.True, "el primer impacto es «Ver mapa», no " + hits[0].gameObject.name);
                data.pointerCurrentRaycast = hits[0];
                data.pointerPressRaycast = hits[0];
            }
            finally
            {
                if (canvas.scaleFactor != scale)
                {
                    canvas.scaleFactor = scale;
                    Canvas.ForceUpdateCanvases();
                }
            }

            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        // Datos de prueba del relevo: las paradas reales con la 3 en el mundo 6 y la 4 en el 7, y el tramo de la 4 con «teotonio»
        // (un punto de paso nuevo en el Mapa, a mitad de su pieza del Amazonas). relayS: la s de «teotonio».
        internal static BannerTravelData RelayData(RiverMap real, RiverRoutes routes, RiverStopCatalog stops, out float relayS)
        {
            RiverStopRoute leg = routes.At(4)!;
            RiverPathPiece amazon = default;
            for (int i = 0; i < leg.Path.Count; i++)
            {
                if (real.Lines[leg.Path[i].Line].Kind == RiverLineKind.Main && leg.Path[i].S0 != leg.Path[i].S1)
                {
                    amazon = leg.Path[i];
                }
            }

            relayS = (amazon.S0 + amazon.S1) * 0.5f;
            var passing = new List<RiverPassingPoint>(real.Passing) { new RiverPassingPoint(BannerTravel.RelayPassingId, new RiverMapPlace(amazon.Line, relayS), null) };
            var lines = new List<RiverLine>(real.Lines);
            var map = new RiverMap(real.Frame, lines.ToArray(), real.Land, new List<RiverInset>(real.Insets).ToArray(), real.Home,
                new List<RiverMapLabel>(real.Labels).ToArray(), new List<RiverViewpoint>(real.Viewpoints).ToArray(), passing.ToArray());

            var via = new List<string>(leg.Via) { BannerTravel.RelayPassingId };
            var pieces = new List<RiverPathPiece>(leg.Path);
            var all = new RiverStopRoute[routes.Count];
            for (int n = 0; n < all.Length; n++)
            {
                all[n] = n == 4 ? new RiverStopRoute(leg.N, leg.Id, leg.At, pieces.ToArray(), via.ToArray(), new List<int>(leg.Mouths).ToArray()) : routes.At(n)!;
            }

            var copied = new RiverStop[stops.Count];
            for (int n = 0; n < copied.Length; n++)
            {
                RiverStop s = stops.At(n)!;
                int world = n == 3 ? 6 : n == 4 ? 7 : s.World;
                copied[n] = new RiverStop(s.N, world, s.Id, s.Kind, s.Country, s.CountryName, s.KmFromSea, s.KmHome, s.Sample, s.FlagRaise, s.ArrivalFact,
                    new List<string>(s.Facts).ToArray(), s.WorldQuestion, s.AnswersWorldQuestion, s.Backdrop);
            }

            return new BannerTravelData(map, new RiverRoutes(all), new RiverStopCatalog(stops.Source, copied));
        }

        // Las rutas reales con la de la parada n sin largo (un tramo sobre la propia parada).
        private static RiverRoutes NoLegRoutes(RiverRoutes routes, int n)
        {
            var all = new RiverStopRoute[routes.Count];
            for (int i = 0; i < all.Length; i++)
            {
                RiverStopRoute r = routes.At(i)!;
                all[i] = i == n
                    ? new RiverStopRoute(r.N, r.Id, r.At, new[] { new RiverPathPiece(r.At.Line, r.At.S, r.At.S) }, new string[0], new int[0])
                    : r;
            }

            return new RiverRoutes(all);
        }
    }
}
