#nullable enable
using System;
using System.Collections;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
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
    /// Tarea FX13: el último tramo (691-700) apunta a la naciente del Mismi (decisión del dueño del 2026-10-19, viñeta de D-013:
    /// «que el último tramo (691-700) apunte a la naciente (el Mismi) como meta, sin parada nueva; el fin de contenido hace el
    /// viaje final hasta la naciente»). Las paradas del Mapa son las 0-69; tras Tambo (69, en el cauce), la meta es el primer
    /// punto del cauce (<see cref="RiverRoutes.Goal"/>), sin tarjeta, sin mini cuia y sin llegada:
    /// <list type="bullet">
    /// <item>jugando 691-700, el frente y sus 10 boyas van de Tambo a la naciente (la última boya, en ella), la canoa por ese
    /// tramo y «Faltan N» (en el Mapa y en el inicio) nombra la naciente, con N = 701 − nivel;</item>
    /// <item>al ganar el 700, el viaje del fin de contenido lleva la canoa de Tambo a la naciente, río arriba, con el agua del
    /// tramo llena al final, las placas enteras en todo el viaje y sin asignaciones en los fotogramas en que no cambia el
    /// llenado (solo en las ≤ 10 boyas).</item>
    /// </list>
    /// Las capturas son las de <see cref="Fx13SnapshotTests"/>.
    /// </summary>
    public sealed class Fx13Tests
    {
        private const float Near = 1e-4f;

        private static readonly DateTime T0 = new DateTime(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ContentEndMapOverride = null;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator From691To700_TheFrontAndTheBuoysGoFromTamboToTheSource_AndFaltanNamesTheSource()
        {
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return LoadAt(695);
            GameBootstrap boot = GameBootstrap.Current!;

            // El inicio en el 695: «Faltan 6 niveles para El nevado Mismi».
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.App!.PlayLevel, Is.EqualTo(695));
            Assert.That(boot.App.PlayLevelAvailable, Is.True, "premisa: el 695 está publicado (C1b)");
            Assert.That(lobby.StopRemainingShown, Is.True, "«Faltan…» hacia la naciente en el inicio");
            Assert.That(lobby.StopRemainingText, Is.EqualTo("Faltan 6 niveles para El nevado Mismi"));

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True, "el Mapa se abre desde el inicio");
            yield return null;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            RiverStopCatalog stops = boot.RiverStops!;
            Assert.That(stops.Count, Is.EqualTo(70), "sin parada nueva: las 0-69");
            Assert.That(routes.Count, Is.EqualTo(70));
            RiverStopRoute goal = routes.Goal!;
            Assert.That(goal, Is.Not.Null, "la meta tras Tambo");
            Assert.That(routes.At(70), Is.SameAs(goal));
            int cauce = data.LineIndexOf(RiverMap.CauceLineId);
            Vector2 tambo = RiverMapScreen.PlacePoint(data, routes.At(69)!.At);
            Vector2 source = RiverMapScreen.PlacePoint(data, new RiverMapPlace(cauce, 0f));
            Assert.That(Vector2.Distance(RiverMapScreen.PlacePoint(data, goal.At), source), Is.LessThan(Near), "la meta, en la naciente");
            Assert.That(Vector2.Distance(tambo, source), Is.GreaterThan(0.05f), "un tramo con largo a la vista");

            for (int max = 690; max <= 699; max++)
            {
                int play = max + 1;
                Reopen(boot, new MemoryProgressStore(play, max, max), play);
                string where = "max_level " + max + " (jugando el " + play + ")";

                // «Faltan N» hacia la naciente: 701 − nivel (10 en el 691, 1 en el 700).
                Assert.That(map.StopRemainingShown, Is.True, where + ": «Faltan…» en el Mapa");
                Assert.That(map.StopRemainingText, Is.EqualTo(UiText.MapStopRemaining(701 - play, UiText.MapSourceName)), where);
                Assert.That(RiverJourney.LevelsLeft(play), Is.EqualTo(701 - play), where);
                AssertSource(map, source, where); // revisión de FX13: el hito y el rótulo de la naciente, desde la llegada a Tambo

                if (max == 690)
                {
                    // Recién llegados a Tambo: el tramo de la parada 69 (sin largo), entero; la canoa en Tambo.
                    Assert.That(map.FrontStop, Is.EqualTo(69), where);
                    Assert.That(Vector2.Distance(map.CanoeMapPoint, tambo), Is.LessThan(Near), where + ": la canoa en Tambo");
                    continue;
                }

                float fraction = (max - 690) / 10f;
                Assert.That(map.FrontStop, Is.EqualTo(70), where + ": el frente va a la meta");
                Assert.That(map.FrontFraction, Is.EqualTo(fraction).Within(1e-6f), where);
                Assert.That(map.FrontPieceCount, Is.EqualTo(1), where + ": una cinta, por el cauce");
                Assert.That(map.FrontRibbon(0).Line, Is.SameAs(data.Lines[cauce]), where);
                Assert.That(map.BuoyCount, Is.EqualTo(10), where);
                int lit = 0;
                for (int i = 0; i < 10; i++)
                {
                    Assert.That(map.BuoyLevel(i), Is.EqualTo(691 + i), where);
                    Vector2 expected = RiverMapScreen.RoutePoint(data, goal, (i + 1) / 10f);
                    Assert.That(Vector2.Distance(map.BuoyMapPoint(i), expected), Is.LessThan(Near), where + ": la boya del " + (691 + i));
                    lit += map.BuoyStateAt(i) == RiverMapScreen.BuoyState.Lit ? 1 : 0;
                }

                Assert.That(lit, Is.EqualTo(max - 690), where + ": encendidas las ganadas");
                Assert.That(Vector2.Distance(map.BuoyMapPoint(9), source), Is.LessThan(Near), where + ": la boya del 700, en la naciente");

                Vector2 canoe = RiverMapScreen.RoutePoint(data, goal, fraction);
                Assert.That(Vector2.Distance(map.CanoeMapPoint, canoe), Is.LessThan(Near), where + ": la canoa, por el tramo a la naciente");
                Assert.That(RiverMapScreen.TryCanoePoint(data, routes, max, out Vector2 point), Is.True, where);
                Assert.That(Vector2.Distance(point, canoe), Is.LessThan(Near), where);
                Assert.That(map.CanoeInView, Is.True, where + ": la canoa, a la vista");
            }

            // FX12 en el tramo de la meta: en Mundo, jugando el 695, las 10 boyas, la canoa y el hito, sobre la curva dibujada.
            Reopen(boot, new MemoryProgressStore(695, 694, 694), 695);
            AssertOnTheDrawnRiver(map, data, routes, "Mundo, jugando el 695");
            AssertGuideClearOfBuoys(map, "Mundo, jugando el 695", true);

            // Con nivel que jugar, el conmutador Mundo/Cuenca sigue a la izquierda (el centrado es solo del fin de contenido).
            Canvas.ForceUpdateCanvases();
            Rect capsule = StopCardTests.CanvasRect(map.ViewSwitch.rectTransform, map.Canvas);
            Assert.That(capsule.center.x, Is.LessThan((map.CanvasWidth * 0.5f) - 1f), "jugando el 695: el conmutador, a la izquierda");

            // Antes de la llegada a Tambo, el Mapa de siempre: sin hito ni rótulo de la naciente.
            Reopen(boot, new MemoryProgressStore(686, 685, 685), 686);
            Assert.That(map.SourceMarkShown, Is.False, "jugando el 686: aún no");
            Assert.That(map.SourceLabelShown, Is.False);

            // En la Cuenca, el hito y el rótulo también se ven, encima de la canoa, sin que la canoa los pise (revisión de FX13).
            Reopen(boot, new MemoryProgressStore(695, 694, 694), 695);
            map.SetView(MapView.Basin);
            for (int t = 0; t < 2000; t += 16)
            {
                map.Tick(16f);
            }

            Assert.That(map.View, Is.EqualTo(MapView.Basin));
            AssertSource(map, source, "Cuenca, jugando el 695");
            AssertGuideClearOfBuoys(map, "Cuenca, jugando el 695", false);
            Assert.That(map.SourceLayer!.GetSiblingIndex(), Is.GreaterThan(map.Canoe.transform.GetSiblingIndex()), "encima de la canoa");

            // El 700 ganado: la canoa en la naciente (también CanoeAt, el de «ponerse al día»).
            Assert.That(RiverMapScreen.TryCanoePoint(data, routes, 700, out Vector2 at700), Is.True);
            Assert.That(Vector2.Distance(at700, source), Is.LessThan(Near), "con el 700, en la naciente");
            Assert.That(Vector2.Distance(RiverMapScreen.CanoeAt(data, routes, 700f), source), Is.LessThan(Near));
            Assert.That(Vector2.Distance(RiverMapScreen.CanoeAt(data, routes, 695f), RiverMapScreen.RoutePoint(data, goal, 0.5f)), Is.LessThan(Near));
        }

        [UnityTest]
        public IEnumerator ContentEnd_After700_TheCanoeTravelsFromTamboToTheSource_PlatesWhole_NoAllocationsPerFrame()
        {
            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return Fx10Tests.OpenContentEnd();
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            RiverMap data = boot.RiverMapData!;
            RiverRoutes routes = boot.RiverRoutes!;
            Assert.That(map.Source, Is.EqualTo(MapOpenSource.ContentEnd));
            Assert.That(map.ContentEndJourney, Is.True);
            Assert.That(map.ContentEndJourneyFromLevel, Is.EqualTo(690), "desde la llegada a Tambo");
            Assert.That(map.CatchUpToLevel, Is.EqualTo(700));
            Assert.That(map.CatchingUp, Is.True, "el viaje final");
            Assert.That(map.ContentEndJourneyMs, Is.LessThanOrEqualTo(MapConfig.AnimMapWorldMs), "tope de anim_map_world_ms (08 §9)");

            int cauce = data.LineIndexOf(RiverMap.CauceLineId);
            Vector2 tambo = RiverMapScreen.PlacePoint(data, routes.At(69)!.At);
            Vector2 source = RiverMapScreen.PlacePoint(data, new RiverMapPlace(cauce, 0f));
            Assert.That(Vector2.Distance(map.CanoeShownMapPoint, tambo), Is.LessThan(Near), "sale de Tambo");
            Fx10Tests.AssertPlatesFit(map, "fin de contenido del 700, al abrir");

            // Revisión de FX13: la cabecera nombra solo el mundo de la última parada, nunca el mundo 10 (el del 701, que el Mapa
            // no tiene), sin map_world_question (es la de la pregunta del mundo); la meta la nombran el hito y su rótulo, que se
            // ven desde el principio del viaje.
            Assert.That(map.Title, Is.EqualTo(UiText.WorldName(9)));
            Assert.That(map.Title, Is.EqualTo("Ucayali"));
            Assert.That(map.Title, Does.Not.Contain(UiText.WorldName(10)), "sin world_w10_name");
            AssertSource(map, source, "fin de contenido del 700, al abrir");

            // Revisión de FX13 (ronda 2): desde el fotograma 0, el frente es ya el tramo de la naciente, vacío, con sus 10 boyas
            // sobre él (antes salía el tramo de Tambo, sin largo en Mundo: sus 10 boyas en la fila corta bajo la parada).
            AssertGoalBuoysOnLeg(map, data, routes.Goal!, "fin de contenido del 700, fotograma 0");
            Assert.That(map.FrontFraction, Is.EqualTo(0f), "fotograma 0: el tramo de la naciente, aún vacío");
            Assert.That(map.FrontPieceCount, Is.EqualTo(0));
            Assert.That(map.SourceMarkAlpha, Is.EqualTo(1f).Within(1e-3f), "fotograma 0: la canoa, lejos del hito");

            map.Tick(16f);
            AssertGoalBuoysOnLeg(map, data, routes.Goal!, "fin de contenido del 700, primer Tick");
            Canvas.ForceUpdateCanvases();
            float fill = map.FrontFraction;
            float distance = Vector2.Distance(map.CanoeShownMapPoint, source);
            int frames = 0;
            int changes = 0;
            for (int t = 0; t < 30000 && (map.CatchingUp || map.CatchUpFading); t += 16)
            {
                int blocks = AllocBlocks(() => map.Tick(16f)) + AllocBlocks(Canvas.ForceUpdateCanvases);
                frames++;
                bool changed = map.FrontFraction != fill;
                fill = map.FrontFraction;
                changes += changed ? 1 : 0;
                string at = "fin de contenido del 700, a " + t + " ms";
                Assert.That(changed || blocks == 0, Is.True, at + ", sin cambio del llenado: " + blocks + " bloques de GC.Alloc");
                Fx10Tests.AssertPlatesFit(map, at);

                // Río arriba hasta la naciente: la canoa nunca se aleja de ella.
                float now = Vector2.Distance(map.CanoeShownMapPoint, source);
                Assert.That(now, Is.LessThanOrEqualTo(distance + Near), at + ": la canoa sube hacia la naciente");
                distance = now;
                if (map.FrontStop == 70)
                {
                    Assert.That(map.FrontPieceCount, map.FrontFraction > 0f ? Is.EqualTo(1) : Is.EqualTo(0), at);
                }
            }

            Assert.That(map.CatchingUp, Is.False, "el viaje terminó");
            Assert.That(map.CatchUpCompletedByTouch, Is.False);
            Assert.That(changes, Is.InRange(1, 10), "el llenado cambia solo en las boyas");
            Debug.Log("FX13 fin de contenido del 700: " + frames + " fotogramas, " + changes + " con cambio del llenado");

            map.Tick(16f);
            Fx10Tests.AssertPlatesFit(map, "fin de contenido del 700, al acabar");
            Assert.That(Vector2.Distance(map.CanoeMapPoint, source), Is.LessThan(Near), "la canoa, en la naciente");
            Assert.That(Vector2.Distance(map.CanoeShownMapPoint, source), Is.LessThan(Near), "y se pinta allí");
            Assert.That(map.CanoeInView, Is.True, "a la vista");
            Assert.That(map.FrontStop, Is.EqualTo(70), "el tramo de la meta");
            Assert.That(map.FrontFraction, Is.EqualTo(1f), "lleno hasta la naciente");
            Assert.That(map.FrontPieceCount, Is.EqualTo(1));
            Assert.That(map.FrontRibbon(0).Line, Is.SameAs(data.Lines[cauce]));
            Assert.That(map.BuoyCount, Is.EqualTo(10));
            for (int i = 0; i < 10; i++)
            {
                Assert.That(map.BuoyStateAt(i), Is.EqualTo(RiverMapScreen.BuoyState.Lit), "boya del " + map.BuoyLevel(i));
            }

            // No es una parada: ni llegada nueva, ni tarjeta.
            Assert.That(map.CatchUpArrivals, Is.EqualTo(0), "sin stop_arrive: la naciente no está en el catálogo");
            Assert.That(map.CardOpen, Is.False, "sin tarjeta de parada");
            Assert.That(map.StopRemainingShown, Is.False, "nada que jugar: sin «Faltan…»");
            Assert.That(map.Title, Does.Not.Contain(UiText.WorldName(10)), "al acabar, tampoco el mundo 10");
            AssertSource(map, source, "fin de contenido del 700, al acabar");

            // Revisión de FX13 (ronda 2): con la canoa en la naciente, el hito no se pinta sobre su casco y el guía va
            // GuideMastClearDp por delante del mástil, para que el mástil no lo tape.
            Assert.That(map.SourceMarkAlpha, Is.EqualTo(0f).Within(1e-3f), "al acabar: el hito, bajo la canoa, no se ve");
            Vector2 guide = map.Space.ToLocal(map.GuideMapPoint);
            Assert.That(map.Mast, Is.Not.Null);
            Assert.That(map.Mast!.gameObject.activeInHierarchy, Is.True, "premisa: la canoa lleva banderas");
            Assert.That(Mathf.Abs(guide.x - map.Mast.rectTransform.anchoredPosition.x),
                Is.EqualTo(RiverMapScreen.GuideMastClearDp).Within(0.5f), "al acabar: el guía, apartado del mástil");

            // Sin nada que jugar, el conmutador Mundo/Cuenca va centrado en la fila (revisión de FX13, ronda 2; solo en este caso).
            Rect capsule = StopCardTests.CanvasRect(map.ViewSwitch.rectTransform, map.Canvas);
            Assert.That(capsule.center.x, Is.EqualTo(map.CanvasWidth * 0.5f).Within(0.5f), "el conmutador, centrado");

            // FX12 en el tramo de la meta: al acabar el viaje, en Mundo, las 10 boyas, la canoa y el hito, sobre la curva dibujada.
            AssertOnTheDrawnRiver(map, data, routes, "Mundo, fin de contenido del 700 al acabar");
        }

        // Revisión final de FX13: en el tramo de la meta, el guía (que nada por el agua) no se pinta sobre ninguna boya del frente:
        // su centro, a ≥ GuideBuoyClearDp del de cada una.
        // Con onLeg false (la Cuenca, con el tramo diminuto), el guía puede esperar encima de la canoa (GuideRoute −1): entonces no
        // nada por el tramo y no se comprueba.
        private static void AssertGuideClearOfBuoys(RiverMapScreen map, string where, bool onLeg)
        {
            Assert.That(map.GuideShown, Is.True, where + ": premisa, el guía a la vista");
            Debug.Log("FX13 " + where + ": el guía en la ruta " + map.GuideRoute);
            if (!onLeg && map.GuideRoute != 70)
            {
                return;
            }

            Assert.That(map.GuideRoute, Is.EqualTo(70), where + ": premisa, el guía nada por el tramo de la meta");
            Vector2 guide = map.GuideImage!.rectTransform.anchoredPosition;
            for (int i = 0; i < map.BuoyCount; i++)
            {
                float d = Vector2.Distance(map.Space.ToLocal(map.BuoyMapPoint(i)), guide);
                Assert.That(d, Is.GreaterThanOrEqualTo(RiverMapScreen.GuideBuoyClearDp - 0.01f),
                    where + ": el guía, apartado de la boya del " + map.BuoyLevel(i));
            }
        }

        // Revisión final de FX13 (con la de FX12): en la vista Mundo, las 10 boyas del tramo de la meta, la canoa y el hito de la
        // naciente quedan a ≤ Fx12Tests.MaxMarkerApartWidths anchos del eje de la curva dibujada del vidrio (el río suave de FX12),
        // no solo de la polilínea (AssertGoalBuoysOnLeg): la curva se ancla también en el tramo de la meta y en la naciente.
        private static void AssertOnTheDrawnRiver(RiverMapScreen map, RiverMap data, RiverRoutes routes, string where)
        {
            map.SetView(MapView.World);
            map.Tick(16f);
            Canvas.ForceUpdateCanvases();
            Assert.That(map.View, Is.EqualTo(MapView.World), where);
            Assert.That(map.BuoysCompact, Is.False, where + ": las boyas, sobre el tramo y no en la fila corta");
            Assert.That(map.FrontStop, Is.EqualTo(70), where + ": el tramo de la meta");
            string? off = Fx12Tests.MarkersOffTheRiver(map, data, routes, where, out System.Collections.Generic.List<string> seen);
            Assert.That(off, Is.Null, where + ": lo más apartado del río dibujado, " + off);
            int buoys = 0;
            foreach (string what in seen)
            {
                buoys += what == "boya" ? 1 : 0;
            }

            Assert.That(buoys, Is.EqualTo(10), where + ": las 10 boyas comprobadas");
            Assert.That(seen, Does.Contain("canoa"), where + ": la canoa comprobada");
            Assert.That(seen, Does.Contain("hito de la naciente"), where + ": el hito comprobado");
        }

        // Las 10 boyas del tramo de la meta (Tambo → naciente), en Mundo, a 2 px o menos de la polilínea del tramo y no en la
        // fila corta (revisión de FX13, ronda 2).
        internal static void AssertGoalBuoysOnLeg(RiverMapScreen map, RiverMap data, RiverStopRoute goal, string where)
        {
            const int samples = 400;
            const float maxPx = 2f;
            Assert.That(map.FrontStop, Is.EqualTo(goal.N), where + ": el frente, el tramo de la meta");
            Assert.That(map.BuoyCount, Is.EqualTo(10), where);
            Assert.That(map.BuoysCompact, Is.False, where + ": las boyas, sobre el tramo y no en la fila corta");
            const float pxPerDp = 1080f / 360f; // el teléfono de las pruebas (Fx10Tests.Phone): 1080 × 2340 px, 360 × 780 dp
            for (int i = 0; i < 10; i++)
            {
                Vector2 b = map.BuoyLocalPoint(i);
                float best = float.MaxValue;
                Vector2 prev = map.Space.ToLocal(RiverMapScreen.RoutePoint(data, goal, 0f));
                for (int k = 1; k <= samples; k++)
                {
                    Vector2 next = map.Space.ToLocal(RiverMapScreen.RoutePoint(data, goal, k / (float)samples));
                    best = Mathf.Min(best, SegmentDistance(b, prev, next));
                    prev = next;
                }

                Assert.That(best * pxPerDp, Is.LessThanOrEqualTo(maxPx), where + ": la boya del " + map.BuoyLevel(i) + " a " + (best * pxPerDp) + " px del tramo");
            }
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len = ab.sqrMagnitude;
            float t = len > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len) : 0f;
            return Vector2.Distance(p, a + (ab * t));
        }

        // El hito de la naciente en su punto, mayor que una boya, y su rótulo (map_source_name) a la vista, sin pisar el hito ni
        // la canoa, dentro del mapa (revisión de FX13).
        internal static void AssertSource(RiverMapScreen map, Vector2 source, string where)
        {
            Assert.That(map.SourceMarkShown, Is.True, where + ": el hito de la naciente");
            Vector2 local = map.Space.ToLocal(source);
            Assert.That(Vector2.Distance(map.SourceMarkLocal, local), Is.LessThan(0.01f), where + ": el hito, en la naciente");
            float buoy = map.Buoys!.Radius;
            Assert.That(map.SourceMarkRadius, Is.EqualTo(RiverMapScreen.SourceMarkScale * Mathf.Max(buoy, RiverMapScreen.BuoyRadiusMinDp)).Within(1e-4f), where);
            Assert.That(map.SourceMarkRadius, Is.GreaterThan(buoy), where + ": mayor que una boya");
            Assert.That(map.SourceLabelShown, Is.True, where + ": el rótulo");
            Assert.That(map.SourceLabelText, Is.EqualTo(UiText.MapSourceName), where);
            RectTransform rt = map.SourceLabel!.rectTransform;
            var box = new Rect(rt.anchoredPosition - (rt.sizeDelta * 0.5f), rt.sizeDelta);
            Assert.That(box.Overlaps(map.CanoeBoxLocal), Is.False, where + ": el rótulo no pisa la canoa");
            float gap = map.SourceLabelBelow ? local.y - box.yMax : box.yMin - local.y;
            Assert.That(gap, Is.GreaterThanOrEqualTo(map.SourceMarkRadius), where + ": el rótulo no pisa el hito");
            Vector2 content = map.ContentSize;
            Assert.That(box.xMin, Is.GreaterThanOrEqualTo(0f), where + ": dentro del mapa");
            Assert.That(box.xMax, Is.LessThanOrEqualTo(content.x), where);
            Assert.That(box.yMax, Is.LessThanOrEqualTo(0f), where);
            Assert.That(box.yMin, Is.GreaterThanOrEqualTo(-content.y), where);
        }

        // El perfil en el nivel «play» (max y map_seen_level en play − 1) y la app en el inicio.
        internal static IEnumerator LoadAt(int play)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = play;
            profile.Progress.MaxLevel = play - 1;
            profile.Progress.MapSeenLevel = play - 1;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            yield return null;
        }

        // Vuelve a abrir el Mapa de la escena con otro progreso, con el nivel publicado.
        internal static void Reopen(GameBootstrap boot, IMapProgress progress, int playLevel)
        {
            RiverMapScreen map = boot.RiverMapView!;
            map.Close();
            map.Open(MapOpenSource.Lobby, boot.RiverMapData!, boot.RiverRoutes!, boot.RiverStops!, progress, playLevel, true);
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
