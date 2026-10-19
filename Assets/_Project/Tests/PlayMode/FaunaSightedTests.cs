#nullable enable
using System.Collections;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Avistamiento de fauna en la escena real (Q11 del guion de la intro, D-029; tarea DC15): en el 5 (la tarjeta «¡Miquito a la
    /// vista!» de la primera especie), la especie entra en <c>fauna.sighted</c> del perfil con su nivel y se escribe la sonda
    /// <c>af_fauna_sighted</c> una vez, con <c>first</c> = true, cuando la tarjeta queda a la vista (no antes, mientras espera a
    /// que se vaya la tropa); el perfil se guarda una vez por el lote. Si la especie ya estaba, la sonda sale con <c>first</c> =
    /// false, la lista no crece y no se guarda nada. Si se gana antes de que se vaya la tropa, la tarjeta no sale y no hay
    /// avistamiento ni sonda. Y el texto de la sonda: <c>{species_id, level, first}</c>, solo identificadores.
    /// </summary>
    public sealed class FaunaSightedTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level5_TheCardShows_TheSpeciesGoesToTheProfile_AndTheProbeIsWrittenOnce()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return LoadLevel5FromTheStart(store);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Profile profile = boot.Profile!;
            string id = SpeciesOf5(boot);

            PlayModeHelpers.TickExact(boot, flow.IntroDurationMs);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.SightingPending, Is.True, "la tarjeta espera a que se vaya la tropa");
            Assert.That(boot.FaunaSightedCount, Is.EqualTo(0), "se apunta al mostrar la tarjeta, no al llegar la tropa");
            Assert.That(profile.Fauna.Sighted, Is.Empty);
            int saves = store.SaveCount;

            StepUntilTheCardShows(boot);

            Assert.That(boot.FaunaSightedCount, Is.EqualTo(1), "una sonda por tarjeta");
            Assert.That(boot.LastFaunaSightedFirst, Is.True);
            Assert.That(profile.Fauna.Sighted.Count, Is.EqualTo(1));
            Assert.That(profile.Fauna.Sighted[0].SpeciesId, Is.EqualTo(id));
            Assert.That(profile.Fauna.Sighted[0].Level, Is.EqualTo(5));
            Assert.That(store.SaveCount, Is.EqualTo(saves + 1), "el avistamiento nuevo se guarda una vez");
            StringAssert.Contains("\"species_id\": \"" + id + "\"", store.Json);

            // La tarjeta sigue a la vista unos segundos: ni otra sonda ni otro guardado.
            PlayModeHelpers.TickExact(boot, 1500);
            Assert.That(view.Sighting.Visible, Is.True);
            Assert.That(boot.FaunaSightedCount, Is.EqualTo(1));
            Assert.That(store.SaveCount, Is.EqualTo(saves + 1));
        }

        [UnityTest]
        public IEnumerator Level5_TheSpeciesWasAlreadySighted_FirstIsFalse_AndTheListDoesNotGrow()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return LoadLevel5FromTheStart(store);
            GameBootstrap boot = GameBootstrap.Current!;
            Profile profile = boot.Profile!;
            string id = SpeciesOf5(boot);
            Assert.That(profile.Fauna.RecordSighting(id, 3), Is.True, "ya vista en otro nivel (perfil de otra partida)");
            int saves = store.SaveCount;

            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro: la tropa se funde en 100 ms");
            StepUntilTheCardShows(boot);

            Assert.That(boot.FaunaSightedCount, Is.EqualTo(1), "la sonda sale igual");
            Assert.That(boot.LastFaunaSightedFirst, Is.False, "la segunda vez de la misma especie");
            Assert.That(profile.Fauna.Sighted.Count, Is.EqualTo(1), "la lista no crece");
            Assert.That(profile.Fauna.Sighted[0].Level, Is.EqualTo(3), "se conserva su primer nivel");
            Assert.That(store.SaveCount, Is.EqualTo(saves), "nada nuevo que guardar");
        }

        [UnityTest]
        public IEnumerator Level5_WonBeforeTheTroopLeaves_NoCard_NoSighting()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return LoadLevel5FromTheStart(store);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TickExact(boot, flow.IntroDurationMs);
            Assert.That(view.SightingPending, Is.True);

            int left = Mathf.CeilToInt(view.Intro.TroopGoneMs - view.Intro.TimeMs);
            Assert.That(left, Is.GreaterThan(PlayModeHelpers.FrameMs));
            PlayModeHelpers.TickExact(boot, left - 1);
            TapTheSolution(boot);
            Assert.That(flow.IsResolvingWin, Is.True);
            for (int t = 0; t < 6000 && flow.Phase != FlowPhase.VictoryScreen; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.Sighting.Visible, Is.False, "la tarjeta no sale (t = " + t + " ms)");
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(boot.FaunaSightedCount, Is.EqualTo(0), "sin tarjeta no hay avistamiento (pregunta 41 (2))");
            Assert.That(boot.Profile!.Fauna.Sighted, Is.Empty);
        }

        [Test]
        public void TheProbeLine_HasTheSpeciesTheLevelAndFirst()
        {
            string? text = new DevProbe().FormatFaunaSighted("saimiri_collinsi", 5, true);
            Assert.That(text, Is.Not.Null, "en el Editor la sonda escribe (build de desarrollo)");
            StringAssert.StartsWith("AF_STATE {\"event\":\"fauna_sighted\"", text);
            StringAssert.Contains(",\"species_id\":\"saimiri_collinsi\",\"level\":5,\"first\":true}", text);

            string? again = new DevProbe().FormatFaunaSighted("saimiri_collinsi", 9, false);
            StringAssert.Contains(",\"level\":9,\"first\":false}", again);
        }

        private static IEnumerator LoadLevel5FromTheStart(MemoryProfileStore store)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore(5, 4));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(5); // desde t = 0: el primer intento del 5, con la tarjeta del miquito
        }

        // La especie de la tarjeta del 5 (la del tramo, del catálogo de fauna).
        private static string SpeciesOf5(GameBootstrap boot)
        {
            FaunaSpecies? species = boot.Fauna?.ForLevel(5);
            Assert.That(species, Is.Not.Null, "el 5 lleva miquito");
            return species!.Id;
        }

        private static void StepUntilTheCardShows(GameBootstrap boot)
        {
            GameScreen view = boot.View!;
            for (int t = 0; t < 4000 && !view.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.Sighting.Visible, Is.True, "la tarjeta, a la vista");
        }

        // Juega de golpe la solución del solver, sin dejar que la vista avance (como NoticeTests).
        private static void TapTheSolution(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
        }
    }
}
