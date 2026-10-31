#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Prueba del dueño en el teléfono (release 1.0.0 (10002), 2026-10-31): «pasé el 40, se quedó en un loop en ver mapa e
    /// iniciar el próximo juego» y «al darle al engranaje se sale y queda el nivel 41». Con la escena real, el catálogo y los datos
    /// del Mapa reales y el viaje de mundo encendido como en el juego (D-013, punto 7; Q4-D; 02 §6, fila Banner de mundo; D-053),
    /// el jugador que gana el 40 llega a jugar el 41 tocando lo que la pantalla le ofrece, en un número acotado de pasos: desde la
    /// victoria (Ver mapa › Volver › Siguiente › banner › Ver mapa › «Jugar · Nivel 41») y desde el inicio tras reabrir la app
    /// («Jugar · Nivel 41» › banner › Ver mapa › «Jugar · Nivel 41»).
    /// </summary>
    public sealed class WorldChangeMapLoopTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = true;
            GameScreen.BannerTravelOverride = true;
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.ArrivalDisabledForTests = false;
            GameScreen.FactLineDisabledForTests = false;
            GameScreen.BannerTravelOverride = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Won40_FromTheVictory_SeeMapTwice_PlayStarts41()
        {
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            yield return VictoryArrivalTests.Win(40, _ => SkipLevelCard(GameBootstrap.Current!));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            RiverMapScreen map = boot.RiverMapView!;
            LevelFlow flow = boot.Flow!;

            // La victoria del 40, con la llegada a su parada y «Ver mapa» (M4.3): el Mapa encima con el viaje (D-053) y «Volver».
            Run(boot, 900);
            Assert.That(view.VictoryMapShown, Is.True, "«Ver mapa» en la victoria del 40");
            view.VictoryMapButton.onClick.Invoke();
            Assert.That(map.Visible && map.Source == MapOpenSource.Victory, Is.True);
            WaitMapPlay(boot, map);
            Assert.That(map.PlayLabel, Is.EqualTo(UiText.Back));
            map.PlayButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "de vuelta a la victoria");

            // «Siguiente»: AD_GATE y el 41, con el banner de mundo y su viaje.
            Assert.That(boot.PressNext(), Is.True);
            for (int ms = 0; ms < 10000 && !(flow.LevelNumber == 41 && view.WorldBannerView.CardShown); ms += PlayModeHelpers.FrameMs)
            {
                Run(boot, PlayModeHelpers.FrameMs);
            }

            AssertSeeMapThenPlayStarts41(boot);
        }

        [UnityTest]
        public IEnumerator Won40_FromTheLobbyAfterReopening_SeeMap_PlayStarts41()
        {
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            Profile profile = VictorySetup.Profile(1240);
            profile.Progress.NextLevel = 41;
            profile.Progress.MaxLevel = 40;
            profile.Progress.MapSeenLevel = 40;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.App.PlayLevel, Is.EqualTo(41));
            Assert.That(boot.PressPlay(), Is.True, "«Jugar · Nivel 41» en el inicio");
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            for (int ms = 0; ms < 10000 && !(flow.LevelNumber == 41 && view.WorldBannerView.CardShown); ms += PlayModeHelpers.FrameMs)
            {
                Run(boot, PlayModeHelpers.FrameMs);
            }

            AssertSeeMapThenPlayStarts41(boot);
        }

        // En el banner del 41 con su viaje: «Ver mapa» abre el Mapa encima (world_change) con «Jugar · Nivel 41»; ese «Jugar» tiene
        // que llevar a jugar el 41 (el banner se cierra y empieza la intro), no devolver al banner, cuyo único botón vuelve a ser
        // «Ver mapa»: ese es el bucle de la prueba del 2026-10-31.
        private static void AssertSeeMapThenPlayStarts41(GameBootstrap boot)
        {
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            RiverMapScreen map = boot.RiverMapView!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(flow.LevelNumber, Is.EqualTo(41));
            Assert.That(banner.Visible && banner.CardShown, Is.True, "el banner del mundo 3");
            Assert.That(banner.Travel, Is.Not.Null, "con el viaje de mundo (Q4-D)");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));

            banner.MapButton.onClick.Invoke();
            Assert.That(map.Visible && map.Source == MapOpenSource.WorldChange, Is.True, "«Ver mapa» del banner");
            WaitMapPlay(boot, map);
            Debug.Log("[WorldChangeMapLoop] «Jugar» del Mapa desde el banner: «" + map.PlayLabel + "»");
            map.PlayButton.onClick.Invoke();
            Assert.That(map.Visible, Is.False, "el Mapa se cierra");
            Run(boot, 1000);
            Assert.That(banner.Visible, Is.False, "«" + map.PlayLabel + "» no puede devolver al banner (el bucle Ver mapa ↔ Jugar)");
            Assert.That(flow.LevelNumber, Is.EqualTo(41));
            Assert.That(flow.Phase == FlowPhase.Intro || flow.Phase == FlowPhase.Playing, Is.True, "a jugar el 41: " + flow.Phase);
        }

        // Con las tarjetas encendidas (las del juego, para que el 41 abra su banner), la del 40, si la lleva, se cierra antes de jugarlo.
        private static void SkipLevelCard(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            for (int i = 0; i < 20 && flow.Phase == FlowPhase.LevelCard; i++)
            {
                Run(boot, 400);
                boot.TapAnywhere();
                Run(boot, 400);
            }

            Assert.That(flow.Phase, Is.Not.EqualTo(FlowPhase.LevelCard), "sin la tarjeta del 40");
        }

        private static void WaitMapPlay(GameBootstrap boot, RiverMapScreen map)
        {
            for (int ms = 0; ms < 15000 && !map.PlayShown; ms += PlayModeHelpers.FrameMs)
            {
                Run(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(map.PlayShown, Is.True, "«Jugar» del Mapa, a la vista");
        }

        // Como GameBootstrap.Update con el reloj del test: la vista, el flujo, el avance tras la Cosecha doble y el Mapa.
        private static void Run(GameBootstrap boot, int ms)
        {
            for (int elapsed = 0; elapsed < ms; elapsed += PlayModeHelpers.FrameMs)
            {
                RiverMapScreen? map = boot.RiverMapView;
                if (map != null && map.Visible)
                {
                    map.Tick(PlayModeHelpers.FrameMs);
                }

                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }
        }
    }
}
