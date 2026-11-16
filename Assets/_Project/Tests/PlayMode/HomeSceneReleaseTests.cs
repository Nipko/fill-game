#nullable enable
using System;
using System.Collections;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El arte de escena propio del inicio se suelta mientras se juega y se repinta al volver (<c>docs/dev/memoria.md</c> §7.5,
    /// punto 3, y §9.2; D-060): los destellos del río, la rama de TiTino y la caja de cuias. El panorama, el muelle y el follaje, que
    /// el nivel también pinta, se quedan. Igual con el Reto del río, que oculta el inicio sin cambiar de pantalla en AppFlow; y no se
    /// suelta mientras otro lo tiene a la vista (<see cref="LobbyScreen.HomeSceneHeld"/>, la intro del arranque).
    /// </summary>
    public sealed class HomeSceneReleaseTests
    {
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Monday = new DateTime(2026, 10, 19, 10, 0, 0); // hora local de Bogotá

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator PlayingALevel_ReleasesTheHomeScene_AndHomeRepaintsIt()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(3)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            LobbyScreen lobby = boot.Lobby!;
            ProceduralArt art = boot.Art!;
            Assert.That(lobby.Visible, Is.True);
            AssertPainted(lobby, art, "al arrancar");
            Sprite panorama = art.Panorama;

            Assert.That(boot.PressPlay(), Is.True);
            yield return null;
            AssertReleased(lobby, art, "jugando un nivel");
            Assert.That(art.Panorama, Is.SameAs(panorama), "el panorama lo pinta también el nivel: se queda");
            Assert.That(panorama.texture != null, Is.True);

            LevelFlow flow = boot.Flow!;
            boot.TapAnywhere();
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                boot.TapCuia(move.Source);
                boot.TapCuia(move.Target);
            }

            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.VictoryScreen, "pantalla de victoria");
            Assert.That(boot.PressHome(), Is.True);
            yield return null;
            Assert.That(lobby.Visible, Is.True);
            AssertPainted(lobby, art, "al volver al inicio");
            Assert.That(lobby.LastHomeSceneRebuildMs, Is.GreaterThanOrEqualTo(0.0));
            Debug.Log("AF_ART home_scene_rebuild_test " + lobby.LastHomeSceneRebuildMs.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " ms");
        }

        [UnityTest]
        public IEnumerator TheDailyChallenge_ReleasesTheHomeScene_AndLeavingItRepaintsIt()
        {
            var clock = new ManualClock(Utc(Monday), Bogota);
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(25)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            Step(boot, PlayModeHelpers.FrameMs);
            LobbyScreen lobby = boot.Lobby!;
            ProceduralArt art = boot.Art!;
            AssertPainted(lobby, art, "en el inicio");

            Assert.That(boot.PressDailyCard(), Is.True, "Jugar el Reto (GameBootstrap.Daily.cs: el inicio se oculta con Show(Level))");
            DailyChallengeScreen daily = boot.DailyScreen!;
            AssertReleased(lobby, art, "jugando el Reto");

            LevelFlow flow = daily.Flow!;
            daily.Actions.TapAnywhere(); // salta la intro
            for (int t = 0; t < 10000 && !(flow.Phase == FlowPhase.Playing && flow.AcceptsBoardInput); t += PlayModeHelpers.FrameMs)
            {
                boot.TickDaily(PlayModeHelpers.FrameMs);
            }

            Assert.That(daily.Actions.PressQuit(), Is.True, "pausa › Salir");
            Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(lobby.Visible, Is.True, "al salir del Reto, el inicio vuelve");
            AssertPainted(lobby, art, "al salir del Reto");
        }

        [UnityTest]
        public IEnumerator WhileSomethingHoldsTheHomeScene_PlayingDoesNotReleaseIt()
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(3)));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            LobbyScreen lobby = boot.Lobby!;
            ProceduralArt art = boot.Art!;
            Func<bool>? held = lobby.HomeSceneHeld;
            lobby.HomeSceneHeld = () => true; // como la intro del arranque con la rama de TiTino a la vista
            try
            {
                Assert.That(boot.PressPlay(), Is.True);
                yield return null;
                Assert.That(lobby.HomeSceneReleased, Is.False, "con la rama a la vista de otro, no se suelta");
                Assert.That(art.HomeSceneLoaded, Is.True);
                Assert.That(lobby.Branch.sprite != null && lobby.Branch.sprite.texture != null, Is.True);
            }
            finally
            {
                lobby.HomeSceneHeld = held;
            }
        }

        private static void AssertPainted(LobbyScreen lobby, ProceduralArt art, string when)
        {
            Assert.That(lobby.HomeSceneReleased, Is.False, when);
            Assert.That(art.HomeSceneLoaded, Is.True, when);
            Assert.That(lobby.Branch.sprite, Is.SameAs(art.HomeBranch), when + ": la rama de TiTino");
            Assert.That(lobby.Crate.sprite, Is.SameAs(art.DockCrate), when + ": la caja de cuias");
            UnityEngine.UI.Image streaks = lobby.River.StreaksImage;
            Assert.That(streaks.sprite, Is.SameAs(art.RiverStreaks), when + ": los destellos del río");
            Assert.That(streaks.enabled, Is.True, when + ": los destellos, a la vista");
            Assert.That(art.HomeBranch.texture != null && art.DockCrate.texture != null && art.RiverStreaks.texture != null, Is.True, when);
        }

        private static void AssertReleased(LobbyScreen lobby, ProceduralArt art, string when)
        {
            Assert.That(lobby.Visible, Is.False, when);
            Assert.That(lobby.HomeSceneReleased, Is.True, when);
            Assert.That(art.HomeSceneLoaded, Is.False, when + ": las texturas, sueltas");
            Assert.That(lobby.Branch.sprite, Is.Null, when);
            Assert.That(lobby.Crate.sprite, Is.Null, when);
            Assert.That(lobby.River.StreaksImage.sprite, Is.Null, when);
            Assert.That(lobby.River.StreaksImage.enabled, Is.False, when);
        }

        private static void Step(GameBootstrap boot, int ms)
        {
            for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
            {
                boot.TickDaily(PlayModeHelpers.FrameMs);
            }
        }

        private static Profile ProfileAt(int maxLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(Utc(Monday), Bogota));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }
    }
}
