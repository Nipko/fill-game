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

            // El inicio en el 695: «Faltan 6 niveles para Nevado Mismi».
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.App!.PlayLevel, Is.EqualTo(695));
            Assert.That(boot.App.PlayLevelAvailable, Is.True, "premisa: el 695 está publicado (C1b)");
            Assert.That(lobby.StopRemainingShown, Is.True, "«Faltan…» hacia la naciente en el inicio");
            Assert.That(lobby.StopRemainingText, Is.EqualTo("Faltan 6 niveles para Nevado Mismi"));

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

            map.Tick(16f);
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
