#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de la tarea FX15 (arreglos de la prueba de la parte C de la ola 7 en el teléfono; <c>[Explicit]</c>, no forman
    /// parte de la suite), a 1080 × 2340 px (360 × 780 dp), en <c>Logs/snapshots/es-419/</c>:
    /// <list type="bullet">
    /// <item><description><c>fx15_fin_contenido_bucle_abre</c>, <c>fx15_fin_contenido_bucle_medio</c> y
    /// <c>fx15_fin_contenido_bucle_llega</c>: «Siguiente» en la victoria del 700 con el bucle del juego (tiempo real, como en el
    /// teléfono): el Mapa del fin de contenido al abrir (la canoa en Tambo), a medio viaje y al llegar a la naciente;</description></item>
    /// <item><description><c>fx15_rio_sigue_creciendo</c> y <c>fx15_inicio_tras_700</c>: la pantalla de debajo al cerrar el
    /// Mapa y el inicio después, con «Jugar · Nivel 701» (observación 2: pregunta para el dueño, sin cambios);</description></item>
    /// <item><description><c>fx15_l2_al_empezar</c>: el 2 con perfil limpio, 6 s después de la intro (la instrucción de TiTino ya se fue) y sin
    /// dedo guía (por diseño: sale tras el primer vertido que deja la lección posible; las capturas con el dedo son las
    /// <c>dc6_l2_*</c> de <see cref="TutorialFingerSnapshotTests"/>);</description></item>
    /// <item><description><c>fx15_tarjeta_manaos</c>: la tarjeta de Manaos con el miquito del tramo y el nombre de su fila,
    /// «sauim-de-coleira» (pendiente del asesor).</description></item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.Fx15SnapshotTests</c> sin
    /// <c>-nographics</c>.
    /// </summary>
    [Explicit("Capturas de FX15; necesitan dispositivo gráfico.")]
    public sealed class Fx15SnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            GameBootstrap? boot = GameBootstrap.Current;
            if (boot != null)
            {
                PlayModeHelpers.TakeClock(boot);
            }

            GameBootstrap.ContentEndMapOverride = null;
            GameScreen.TutorialFingerOverride = null;
            GameScreen.TutorialSlothOverride = null;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
        }

        [UnityTest]
        public IEnumerator CaptureTheContentEndJourneyWithTheGameLoop()
        {
            string dir = SnapshotKit.Prepare();
            GameBootstrap.ContentEndMapOverride = true;
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return FactLineTests.Win(Fx10Tests.ContentEndLevel);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            PlayModeHelpers.Step(boot, 900);
            yield return null;

            boot.enabled = true;
            boot.View!.NextButton.onClick.Invoke();
            while (!map.Visible)
            {
                yield return null;
            }

            // Al abrir: reloj tomado para la captura (el viaje sigue después donde estaba).
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Assert.That(map.CatchingUp, Is.True);
            SnapshotKit.Save(dir, "fx15_fin_contenido_bucle_abre");
            Debug.Log("FX15 fx15_fin_contenido_bucle_abre: canoa " + map.CanoeShownMapPoint + ", " + map.CatchUpElapsedMs + " ms");

            boot.enabled = true;
            while (map.CatchingUp && map.CatchUpElapsedMs < map.ContentEndJourneyMs * 0.5f)
            {
                yield return null;
            }

            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.Save(dir, "fx15_fin_contenido_bucle_medio");
            Debug.Log("FX15 fx15_fin_contenido_bucle_medio: canoa " + map.CanoeShownMapPoint + ", " + map.CatchUpElapsedMs + " ms");

            boot.enabled = true;
            while (map.CatchingUp)
            {
                yield return null;
            }

            yield return null;
            PlayModeHelpers.TakeClock(boot);
            SnapshotKit.Save(dir, "fx15_fin_contenido_bucle_llega");
            Debug.Log("FX15 fx15_fin_contenido_bucle_llega: canoa " + map.CanoeShownMapPoint + ", " + map.CatchUpElapsedMs + " ms, por toque "
                + map.CatchUpCompletedByTouch);

            map.BackButton.onClick.Invoke();
            yield return null;
            SnapshotKit.Save(dir, "fx15_rio_sigue_creciendo");
            boot.Lobby!.BackButton.onClick.Invoke();
            boot.Lobby.Tick(16f);
            yield return null;
            SnapshotKit.Save(dir, "fx15_inicio_tras_700");
            Debug.Log("FX15 fx15_inicio_tras_700: Jugar en el " + boot.App!.PlayLevel + ", publicado " + boot.App.PlayLevelAvailable);
        }

        [UnityTest]
        public IEnumerator CaptureLevel2AtTheStart()
        {
            TutorialSlothTests.Hooks();
            GameScreen.TutorialFingerOverride = true;
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = 1f;
            yield return TutorialSlothTests.Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 6000);
            Assert.That(view.IntroTroopVisible, Is.False);
            Assert.That(view.TutorialFingerVisible, Is.False, "por diseño: el tablero inicial del 2, lleno, no deja la lección");
            Assert.That(view.TutorialFingerArmed, Is.True, "espera al primer vertido que la deja posible");
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "fx15_l2_al_empezar");
        }

        [UnityTest]
        public IEnumerator CaptureTheManaosCard()
        {
            string dir = SnapshotKit.Prepare();
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = Fx10Tests.Phone;
            yield return MapMarkersTests.OpenApp(59);
            GameBootstrap boot = GameBootstrap.Current!;
            RiverMapScreen map = boot.RiverMapView!;
            SnapshotKit.AttachLobby(boot);
            SnapshotKit.AttachOverlay(map.Canvas);
            Assert.That(map.TapStop(5), Is.True);
            for (int t = 0; t < 1500; t += 16)
            {
                map.Tick(16f);
            }

            yield return null;
            Assert.That(map.Card!.FaunaName, Is.EqualTo("sauim-de-coleira"));
            SnapshotKit.Save(dir, "fx15_tarjeta_manaos");
        }
    }
}
