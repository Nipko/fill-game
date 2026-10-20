#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Tarea FX15: arreglos de la prueba de la parte C de la ola 7 en el teléfono (<c>docs/dev/traspaso/pruebas-telefono-2026-10-20.md</c>,
    /// «Observaciones y pendientes» 1-4):
    /// <list type="bullet">
    /// <item><description>1. El viaje final del 700 (D-013, viñeta del 2026-10-19: «el fin de contenido hace el viaje final hasta
    /// la naciente»), por el flujo real y con el bucle del juego (<c>GameBootstrap.Update</c>, tiempo real), no con el reloj del
    /// test: «Siguiente» en la victoria del 700 → AD_GATE → «El río sigue creciendo» con el Mapa encima, que sale con la canoa en
    /// Tambo, la lleva río arriba y la deja en la naciente en ≈ 1,5 s (<see cref="RiverJourney.CatchUpMs"/>), sin que nada lo
    /// complete antes; la sonda <c>map_journey</c> lo deja en el logcat (en capturas cada 2 s no se ve). La segunda vez que se
    /// abre el Mapa ya no hay viaje.</description></item>
    /// <item><description>4. La especie de la tarjeta de parada sale con el nombre visible de su fila de <c>facts.csv</c>
    /// (<c>fauna.saguinus_bicolor.name</c>, «sauim-de-coleira», <c>pendiente_asesor</c>), no con su id: es el nombre común
    /// que usa la tabla (pregunta para el asesor cultural).</description></item>
    /// </list>
    /// Las capturas son las de <see cref="Fx15SnapshotTests"/>.
    /// </summary>
    public sealed class Fx15Tests
    {
        private const float Near = 1e-4f;

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ContentEndMapOverride = null;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            GameBootstrap? boot = GameBootstrap.Current;
            if (boot != null)
            {
                PlayModeHelpers.TakeClock(boot);
            }

            LaunchOptions.Reset();
            OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator Winning700_SiguienteWithTheGameLoop_TheCanoeLeavesTambo_TravelsUpriver_AndReachesTheSource()
        {
            var journey = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_journey\"", StringComparison.Ordinal))
                {
                    journey.Add(message);
                }
            }

            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return FactLineTests.Win(Fx10Tests.ContentEndLevel);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(boot.App!.ContentEndMapAvailable, Is.True, "premisa: la escena trae el Mapa");
            PlayModeHelpers.Step(boot, 900); // el panel de la victoria ya subió, como cuando se toca en el teléfono
            yield return null; // fotograma nuevo con el reloj aún tomado: el primer dt de Update cuenta desde aquí

            Vector2 tambo = default;
            Vector2 source = default;
            Application.logMessageReceived += OnLog;
            try
            {
                // El toque en «Siguiente» y el bucle del juego (GameBootstrap.Update, con el tiempo real de cada fotograma).
                boot.enabled = true;
                float pressedAt = Time.realtimeSinceStartup;
                view.NextButton.onClick.Invoke();
                while (!map.Visible)
                {
                    Assert.That(Time.realtimeSinceStartup - pressedAt, Is.LessThan(5f), "el Mapa del fin de contenido se abre tras AD_GATE");
                    yield return null;
                }

                Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.ContentEnd), "06 §9: «El río sigue creciendo», debajo");

                // La geometría se lee al abrir el Mapa por primera vez (EnsureRiverMap).
                RiverMap data = boot.RiverMapData!;
                RiverRoutes routes = boot.RiverRoutes!;
                int cauce = data.LineIndexOf(RiverMap.CauceLineId);
                tambo = RiverMapScreen.PlacePoint(data, routes.At(69)!.At);
                source = RiverMapScreen.PlacePoint(data, new RiverMapPlace(cauce, 0f));
                float leg = Vector2.Distance(tambo, source);
                Assert.That(leg, Is.GreaterThan(0f), "premisa: Tambo no es la naciente");
                Assert.That(map.Source, Is.EqualTo(MapOpenSource.ContentEnd));
                Assert.That(map.ContentEndJourney, Is.True);
                Assert.That(map.CatchUpFromLevel, Is.EqualTo(690), "desde la llegada a Tambo, aunque la franja del 700 ya anotara lo visto");
                Assert.That(map.CatchUpToLevel, Is.EqualTo(700));
                Assert.That(map.CatchingUp, Is.True, "el viaje final empieza");
                Assert.That(map.Title, Is.EqualTo("Ucayali"), "la cabecera decidida en FX13, sin cambios");

                // Mientras dura, con el bucle del juego: la canoa sale de Tambo, sube por el tramo sin alejarse nunca de la naciente
                // y llega a ella; no lo completa nada antes de tiempo.
                float openedAt = Time.realtimeSinceStartup;
                float first = Vector2.Distance(map.CanoeShownMapPoint, source);
                Assert.That(Vector2.Distance(map.CanoeShownMapPoint, tambo), Is.LessThan(Near), "al abrir, la canoa en Tambo");
                float distance = first;
                int between = 0;
                int frames = 0;
                while (map.CatchingUp)
                {
                    Assert.That(Time.realtimeSinceStartup - openedAt, Is.LessThan(10f), "el viaje acaba");
                    yield return null;
                    frames++;
                    float now = Vector2.Distance(map.CanoeShownMapPoint, source);
                    Assert.That(now, Is.LessThanOrEqualTo(distance + Near), "a los " + frames + " fotogramas: la canoa sube hacia la naciente");
                    distance = now;
                    if (map.CatchingUp && now > Near && now < leg - Near)
                    {
                        between++;
                    }
                }

                float lasted = Time.realtimeSinceStartup - openedAt;
                Debug.Log("FX15 viaje del fin de contenido con el bucle del juego: " + frames + " fotogramas, " + between
                    + " a medio tramo, " + lasted.ToString("0.000") + " s (" + map.ContentEndJourneyMs + " ms previstos)");
                Assert.That(map.CatchUpCompletedByTouch, Is.False, "llegó sola: ningún toque ni cierre lo completó");
                Assert.That(map.CatchUpElapsedMs, Is.GreaterThanOrEqualTo(map.ContentEndJourneyMs), "recorrió el tramo entero");
                Assert.That(between, Is.GreaterThanOrEqualTo(5), "se ve a medio tramo en varios fotogramas");

                // dt de GameBootstrap.Update va topado a 100 ms por fotograma: el viaje de 1,5 s no puede durar menos de ~1,5 s reales
                // salvo por el primer fotograma, que se salta (_skipNextDelta).
                Assert.That(lasted, Is.GreaterThanOrEqualTo((map.ContentEndJourneyMs / 1000f) - 0.1f), "el viaje se ve entero, en tiempo real");
                Assert.That(Vector2.Distance(map.CanoeShownMapPoint, source), Is.LessThan(Near), "al acabar, la canoa en la naciente");
                Assert.That(map.FrontFraction, Is.EqualTo(1f), "el tramo de la meta, lleno");
                Assert.That(map.CatchUpArrivals, Is.EqualTo(0), "la naciente no es una parada: sin stop_arrive");
                Assert.That(map.Visible, Is.True, "el Mapa se queda hasta que se cierra");
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }

            PlayModeHelpers.TakeClock(boot);
            if (DevProbe.Enabled)
            {
                Assert.That(journey.Count, Is.EqualTo(2), string.Join("\n", journey));
                StringAssert.Contains("\"phase\":\"start\"", journey[0]);
                StringAssert.Contains("\"source\":\"content_end\"", journey[0]);
                StringAssert.Contains("\"from\":690,\"to\":700,\"ms\":" + map.ContentEndJourneyMs, journey[0]);
                StringAssert.Contains("\"elapsed_ms\":0,\"by\":null", journey[0]);
                StringAssert.Contains("\"phase\":\"end\"", journey[1]);
                StringAssert.Contains("\"by\":\"time\"", journey[1]);
            }
            else
            {
                Assert.That(journey, Is.Empty);
            }

            // Se cierra: «El río sigue creciendo»; Volver, al inicio. La segunda vez que se abre el Mapa, ya sin viaje.
            map.BackButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.ContentEnd));
            boot.Lobby!.BackButton.onClick.Invoke();
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            Assert.That(map.ContentEndJourney, Is.False);
            Assert.That(map.CatchingUp, Is.False, "nada nuevo que ver: sin viaje");
            Assert.That(Vector2.Distance(map.CanoeShownMapPoint, source), Is.LessThan(Near), "la canoa sigue en la naciente");
        }

        [UnityTest]
        public IEnumerator ContentEndJourney_ATouch_TheProbeSaysTouch()
        {
            var journey = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (message.StartsWith("AF_STATE {\"event\":\"map_journey\"", StringComparison.Ordinal))
                {
                    journey.Add(message);
                }
            }

            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            Application.logMessageReceived += OnLog;
            try
            {
                yield return Fx10Tests.OpenContentEnd();
                RiverMapScreen map = GameBootstrap.Current!.RiverMapView!;
                Assert.That(map.CatchingUp, Is.True);
                map.Tick(300f);
                Assert.That(map.CatchUpTouch(), Is.True);
                Assert.That(map.CatchingUp, Is.False);

                // Otro Mapa (desde el inicio, sin viaje del fin de contenido): sin sonda.
                map.Close();
                Fx13Tests.Reopen(GameBootstrap.Current!, new MemoryProgressStore(701, 700, 690), 700);
                Assert.That(map.CatchingUp, Is.True, "ponerse al día de siempre");
                Assert.That(map.ContentEndJourney, Is.False);
                map.CatchUpTouch();
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }

            if (!DevProbe.Enabled)
            {
                Assert.That(journey, Is.Empty);
                yield break;
            }

            Assert.That(journey.Count, Is.EqualTo(2), "solo el viaje del fin de contenido: " + string.Join("\n", journey));
            StringAssert.Contains("\"phase\":\"end\"", journey[1]);
            StringAssert.Contains("\"by\":\"touch\"", journey[1]);
            StringAssert.Contains("\"elapsed_ms\":", journey[1]);
            StringAssert.DoesNotContain("\"elapsed_ms\":0,", journey[1]);
        }

        [UnityTest]
        public IEnumerator ManaosCard_TheMonkeyGoesWithTheNameOfItsRow_NotItsId()
        {
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            Assert.That(map.TapStop(5), Is.True, "Manaos (51-60)");
            yield return null;
            StopCard card = map.Card!;
            FaunaSpecies species = card.Species!;
            Assert.That(species, Is.Not.Null);
            Assert.That(species.Id, Is.EqualTo("saguinus_bicolor"), "el miquito del mundo 3 en el tramo de Manaos");
            Assert.That(species.NameKey, Is.EqualTo("fauna.saguinus_bicolor.name"));
            Assert.That(card.FaunaName, Is.EqualTo(Loc.Fact(species.NameKey)), "el nombre de su fila de facts.csv");
            Assert.That(card.FaunaName, Is.Not.EqualTo(species.Id), "no el id");
            Assert.That(card.FaunaName, Is.Not.EqualTo(species.NameKey), "ni la clave");
            Assert.That(card.FaunaName, Is.EqualTo("sauim-de-coleira"), "el nombre común de la tabla (pendiente_asesor): pregunta para el asesor");
        }
    }
}
