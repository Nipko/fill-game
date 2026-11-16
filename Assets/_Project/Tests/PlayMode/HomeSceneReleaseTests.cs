#nullable enable
using System.Collections;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
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
    /// punto 3; D-060): los destellos del río, la rama de TiTino y la caja de cuias. El panorama, el muelle y el follaje, que el
    /// nivel también pinta, se quedan. La línea <c>AF_ART home_scene</c> y <see cref="LobbyScreen.LastHomeSceneRebuildMs"/> dicen lo
    /// que cuesta repintarlo.
    /// </summary>
    public sealed class HomeSceneReleaseTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator PlayingALevel_ReleasesTheHomeScene_AndHomeRepaintsIt()
        {
            Profile profile = Wallet.NewProfile(new ManualClock(new System.DateTime(2026, 11, 16, 15, 0, 0, System.DateTimeKind.Utc)));
            profile.Progress.MaxLevel = 3;
            profile.Progress.NextLevel = 4;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return null;
            LobbyScreen lobby = boot.Lobby!;
            ProceduralArt art = boot.Art!;
            Assert.That(lobby.Visible, Is.True);
            Assert.That(art.HomeSceneLoaded, Is.True, "el inicio pinta su río, la rama y la caja");
            Sprite panorama = art.Panorama;

            Assert.That(boot.PressPlay(), Is.True);
            yield return null;
            Assert.That(lobby.HomeSceneReleased, Is.True, "jugando, el inicio suelta su arte propio");
            Assert.That(art.HomeSceneLoaded, Is.False);
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
            Assert.That(lobby.HomeSceneReleased, Is.False, "al volver, el inicio repinta su arte");
            Assert.That(art.HomeSceneLoaded, Is.True);
            Assert.That(art.RiverStreaks.texture != null && art.HomeBranch.texture != null && art.DockCrate.texture != null, Is.True);
            Assert.That(lobby.LastHomeSceneRebuildMs, Is.GreaterThanOrEqualTo(0.0));
            Debug.Log("AF_ART home_scene_rebuild_test " + lobby.LastHomeSceneRebuildMs.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " ms");
        }
    }
}
