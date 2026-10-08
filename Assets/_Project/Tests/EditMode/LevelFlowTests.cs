#nullable enable
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Máquina de estados del nivel (GDD 02 §2) con Marea (04 §2.1), Volver a la orilla (04 §2.7) y el modo de vertido
    /// <c>rules_pour_mode</c> (D-005, que sustituye a D-001: por defecto <c>run</c>; 08 §9).
    /// </summary>
    public sealed class LevelFlowTests
    {
        private RecordingListener _listener = null!;
        private MemoryProgressStore _progress = null!;
        private LevelFlow _flow = null!;

        [SetUp]
        public void SetUp()
        {
            _listener = new RecordingListener();
            _progress = new MemoryProgressStore();
            _flow = new LevelFlow(TestPacks.Tutorial, _progress, _listener);
        }

        [Test]
        public void Load_RunsLoadingThenIntroOf1400ms_ThenReady()
        {
            _flow.Load(1);
            Assert.That(_listener.Events[0], Is.EqualTo("loaded:1"));
            Assert.That(_listener.Events[1], Is.EqualTo("intro:" + GameConfig.AnimTintDurationMsEarly));
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(_flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored)); // 02 §14: no se interpreta como selección

            _flow.Tick(GameConfig.AnimTintDurationMsEarly - 1, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Intro));
            _flow.Tick(1, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(_listener.Saw("ready"), Is.True);
        }

        [Test]
        public void RulesPourModeDefault_IsRun_AndEveryLevelLoadsInIt()
        {
            // 08 §9: rules_pour_mode = "run" (D-005, que sustituye a D-001), convertido con la única conversión del núcleo.
            Assert.That(PourModes.TryParse(GameConfig.RulesPourMode, out PourMode configured), Is.True);
            Assert.That(configured, Is.EqualTo(PourMode.Run));
            Assert.That(configured, Is.EqualTo(PourModes.Default));
            Assert.That(_flow.PourMode, Is.EqualTo(PourMode.Run));
            for (int level = 1; level <= 10; level++)
            {
                _flow.Load(level);
                Assert.That(_flow.Session.PourMode, Is.EqualTo(PourMode.Run), "nivel " + level);
            }
        }

        [Test]
        public void InRun_ARunOfThreeOntoAnEmptyCuia_MovesThreeDosesInOnePour()
        {
            // D-005 (02 R9): la racha de 3 açaí del nivel 2 (cuia 2) pasa entera a la cuia vacía 3 en un solo vertido, y
            // Marea devuelve las 3 dosis con un solo uso (04 §2.1).
            StartPlaying(2);
            Assert.That(_flow.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            TapResult r = _flow.TapCuia(3);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(r.Pour.Pour.Amount, Is.EqualTo(3));
            Assert.That(_flow.Session.AppliedPours, Is.EqualTo(1));
            Assert.That(_flow.Session.Current[3].Count, Is.EqualTo(3));
            Assert.That(_flow.Session.Current[2].Count, Is.EqualTo(1));

            Assert.That(_flow.Undo(), Is.True);
            Assert.That(_flow.Session.Current[3].Count, Is.EqualTo(0));
            Assert.That(_flow.Session.Current[2].Count, Is.EqualTo(4));
            Assert.That(_flow.Session.AppliedPours, Is.EqualTo(0));
            Assert.That(_listener.Saw("undo:2->3"), Is.True);
        }

        [Test]
        public void InSingle_ARunOfThree_TakesThreePours_AndMareaReturnsOneDosePerUse()
        {
            // Modo de pruebas (single; D-005): la racha de 3 açaí del nivel 2 (cuia 2) pasa a la cuia vacía 3 con tres
            // vertidos de una dosis; Marea deshace una dosis por uso (04 §2.1).
            var listener = new RecordingListener();
            var flow = new LevelFlow(TestPacks.Tutorial, new MemoryProgressStore(), listener, true, PourMode.Single);
            flow.Load(2);
            flow.TapAnywhere();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.Session.PourMode, Is.EqualTo(PourMode.Single));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(flow.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected), "origen " + i);
                TapResult r = flow.TapCuia(3);
                Assert.That(r.Kind, Is.EqualTo(TapKind.Poured), "destino " + i);
                Assert.That(r.Pour.Pour.Amount, Is.EqualTo(1));
            }

            Assert.That(flow.Session.AppliedPours, Is.EqualTo(3));
            Assert.That(flow.Session.Current[3].Count, Is.EqualTo(3));
            Assert.That(flow.Undo(), Is.True);
            Assert.That(flow.Session.Current[3].Count, Is.EqualTo(2));
            Assert.That(flow.Session.Current[2].Count, Is.EqualTo(2));
            Assert.That(listener.Saw("undo:2->3"), Is.True);
        }

        [Test]
        public void PourModeChange_AppliesFromTheNextLoad_NeverMidLevel()
        {
            // D-005 punto 3 (el de D-001, que conserva): el modo se fija al cargar el nivel (el historial de Marea y el par
            // dependen de él).
            StartPlaying(2);
            _flow.PourMode = PourMode.Single;
            Assert.That(_flow.Session.PourMode, Is.EqualTo(PourMode.Run), "el nivel en curso conserva su modo");
            _flow.TapCuia(2);
            Assert.That(_flow.TapCuia(3).Pour.Pour.Amount, Is.EqualTo(3), "run: la racha visible que cabe (02 R9)");

            Assert.That(_flow.PressRestart(), Is.True);
            Assert.That(_flow.Session.PourMode, Is.EqualTo(PourMode.Run), "Volver a la orilla tampoco lo cambia");

            _flow.Load(2);
            _flow.TapAnywhere();
            Assert.That(_flow.Session.PourMode, Is.EqualTo(PourMode.Single));
            _flow.TapCuia(2);
            Assert.That(_flow.TapCuia(3).Pour.Pour.Amount, Is.EqualTo(1), "single: una dosis por vertido (pruebas)");
        }

        [Test]
        public void PourMode_IsTakenFromTheConstructor_AndRejectsUndefinedValues()
        {
            var single = new LevelFlow(TestPacks.Tutorial, new MemoryProgressStore(), new RecordingListener(), true, PourMode.Single);
            single.Load(1);
            Assert.That(single.Session.PourMode, Is.EqualTo(PourMode.Single));
            Assert.That(() => { _flow.PourMode = (PourMode)7; }, Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(_flow.PourMode, Is.EqualTo(PourMode.Run));
        }

        [Test]
        public void TapAnywhere_SkipsTheIntro_WithoutSelecting()
        {
            _flow.Load(1);
            Assert.That(_flow.TapAnywhere(), Is.True);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(_flow.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(_listener.Saw("ready:skipped"), Is.True);
            Assert.That(_flow.TapAnywhere(), Is.False);
        }

        [Test]
        public void Win_WaitsForTheView_ThenCelebratesForTheConfiguredTime_ThenVictory()
        {
            StartPlaying(1);
            PlaySolution();
            Assert.That(_flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(_listener.Saw("win"), Is.True);
            Assert.That(_progress.NextLevel, Is.EqualTo(2)); // guardado en el instante lógico de la victoria
            Assert.That(_flow.AcceptsBoardInput, Is.False);
            Assert.That(_flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored));

            _flow.Tick(500, false); // la cola visual aún no termina (02 §3.2)
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Playing));
            _flow.Tick(16, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            _flow.Tick(GameConfig.AnimCelebrationMs - 1, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            _flow.Tick(1, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));

            _flow.Tick(60000, true); // sin auto-avance (victory_autonext_s = 0)
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(_flow.Next(), Is.True);
            Assert.That(_flow.LevelNumber, Is.EqualTo(2));
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [Test]
        public void Celebration_IsSkippedByATap()
        {
            StartPlaying(1);
            PlaySolution();
            _flow.Tick(16, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            Assert.That(_flow.TapAnywhere(), Is.True);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }

        [Test]
        public void Win_DoesNotWaitForeverForTheView()
        {
            StartPlaying(1);
            PlaySolution();
            _flow.Tick(GameConfig.MaxViewDrainWaitMs, false);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Celebration));
        }

        [Test]
        public void Win_SavesMaxLevelAndNextLevel_InOneSave()
        {
            // 08 §4.3 (B1-5): progress.max_level = N y progress.next_level = N + 1 en el instante lógico de la victoria.
            StartPlaying(3);
            PlaySolution();
            Assert.That(_progress.MaxLevel, Is.EqualTo(3));
            Assert.That(_progress.NextLevel, Is.EqualTo(4));
            Assert.That(_progress.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void LastLevel_SavesTheNextNumber_AndNextReportsTheEndOfTheContent()
        {
            // Ya no se vuelve al nivel 1 (prototipo v0): tras el último publicado, next_level = 11 y Siguiente no carga nada;
            // AppFlow muestra «El río sigue creciendo» (08 §6.2; 06 §9).
            StartPlaying(10);
            PlaySolution();
            Assert.That(_progress.NextLevel, Is.EqualTo(11));
            Assert.That(_progress.MaxLevel, Is.EqualTo(10));
            _flow.Tick(16, true);
            _flow.TapAnywhere();
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(_flow.Next(), Is.False);
            Assert.That(_flow.LevelNumber, Is.EqualTo(10));
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }

        [Test]
        public void WithALevelOffset_TheCampaignNumberIsShifted_AndTheBoardIsTheContentOne()
        {
            // af_level_offset [P] (B1-5): con 30, el nivel 31 se juega con el tablero del nivel 1 del tutorial.
            var progress = new MemoryProgressStore();
            var flow = new LevelFlow(LevelCatalog.Create(new[] { TestPacks.Tutorial }, 30), progress, new RecordingListener());
            Assert.That(flow.ResolveLevel(1), Is.EqualTo(31), "el 1 no existe: el primero es el 31");
            flow.Load(31);
            Assert.That(flow.LevelNumber, Is.EqualTo(31));
            Assert.That(flow.Level.Level, Is.EqualTo(1));
            Assert.That(flow.IntroDurationMs, Is.EqualTo(GameConfig.AnimTintDurationMsLate), "desde el 21, la intro corta (02 §4.2)");
            flow.TapAnywhere();
            SolveResult result = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            for (int i = 0; i < result.MoveCount; i++)
            {
                flow.TapCuia(result.GetMove(i).Source);
                flow.TapCuia(result.GetMove(i).Target);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(progress.NextLevel, Is.EqualTo(32));
            Assert.That(progress.MaxLevel, Is.EqualTo(31));
        }

        [Test]
        public void Stuck_OpensTheModal400msAfterTheView_R15_AndMareaClosesIt()
        {
            StartPlaying(6);
            Assert.That(_flow.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(_flow.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(_flow.Session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(_listener.Saw("stuck"), Is.True);
            Assert.That(_flow.AcceptsBoardInput, Is.False); // input bloqueado desde el instante lógico (R15)

            _flow.Tick(700, false); // la vista sigue animando el último vertido
            Assert.That(_listener.Saw("modal"), Is.False);
            _flow.Tick(16, true); // la cola termina
            _flow.Tick(GameConfig.StuckModalDelayMs - 1, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Playing));
            _flow.Tick(1, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(_listener.Saw("modal"), Is.True);

            Assert.That(_flow.CanUndo, Is.True);
            Assert.That(_flow.Undo(), Is.True);
            Assert.That(_listener.Saw("undo:3->4:modal"), Is.True);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(_flow.Session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(_flow.Session.AppliedPours, Is.EqualTo(0));
            Assert.That(_flow.AcceptsBoardInput, Is.True);
        }

        [Test]
        public void RestartFromTheModal_RunsTheShortIntro_AndRestoresTheInitialBoard()
        {
            StartPlaying(6);
            _flow.TapCuia(3);
            _flow.TapCuia(4);
            _flow.Tick(16, true);
            _flow.Tick(GameConfig.StuckModalDelayMs, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.StuckModal));

            Assert.That(_flow.Restart(), Is.True);
            Assert.That(_listener.Saw("restart:modal"), Is.True);
            Assert.That(_listener.Saw("intro:" + GameConfig.AnimTintDurationMsLate + ":short"), Is.True);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(_flow.Session.Current.Equals(_flow.Session.Initial), Is.True);
            Assert.That(_flow.Session.AppliedPours, Is.EqualTo(0));
            _flow.Tick(GameConfig.AnimTintDurationMsLate, true);
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        [Test]
        public void Undo_InPlaying_RestoresThePreviousState()
        {
            StartPlaying(1);
            Assert.That(_flow.CanUndo, Is.False);
            Assert.That(_flow.Undo(), Is.False);
            _flow.TapCuia(0);
            _flow.TapCuia(2);
            Assert.That(_flow.CanUndo, Is.True);
            Assert.That(_flow.Undo(), Is.True);
            Assert.That(_flow.Session.Current.Equals(_flow.Session.Initial), Is.True);
            Assert.That(_listener.Saw("undo:0->2"), Is.True);
        }

        [Test]
        public void C8_HudUndoWithSelection_DeselectsThenUndoes()
        {
            // 02 §3.1 C8: toque en HUD/comodín con selección → deselecciona y ejecuta la acción del botón.
            StartPlaying(1);
            _flow.TapCuia(0);
            _flow.TapCuia(2);
            Assert.That(_flow.TapCuia(1).Kind, Is.EqualTo(TapKind.Selected));
            int taps = _listener.Taps.Count;

            Assert.That(_flow.PressUndo(), Is.True);

            Assert.That(_listener.Taps.Count, Is.EqualTo(taps + 1));
            TapResult deselect = _listener.Taps[taps];
            Assert.That(deselect.Kind, Is.EqualTo(TapKind.Deselected));
            Assert.That(deselect.PreviousSelection, Is.EqualTo(1));
            Assert.That(_flow.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(_listener.Events.IndexOf("undo:0->2"), Is.GreaterThan(_listener.Events.LastIndexOf("tap:Deselected")));
            Assert.That(_flow.Session.Current.Equals(_flow.Session.Initial), Is.True);
        }

        [Test]
        public void C8_HudUndoWithoutHistory_StillDeselects()
        {
            StartPlaying(1);
            _flow.TapCuia(0);
            Assert.That(_flow.PressUndo(), Is.False); // sin historial Marea no hace nada, pero la selección se quita igual
            Assert.That(_flow.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(_listener.Saw("tap:Deselected"), Is.True);
        }

        [Test]
        public void C8_HudRestartWithSelection_DeselectsThenRestarts()
        {
            StartPlaying(1);
            _flow.TapCuia(0);
            Assert.That(_flow.PressRestart(), Is.True);
            int deselect = _listener.Events.LastIndexOf("tap:Deselected");
            Assert.That(deselect, Is.GreaterThanOrEqualTo(0));
            Assert.That(_listener.Events.IndexOf("restart"), Is.GreaterThan(deselect));
            Assert.That(_flow.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [Test]
        public void Packs_WithLevelsAfterUnlimitedMarea_AreRejected()
        {
            // 04 §2.1: Marea ilimitada hasta undo_unlimited_until_level (10); desde el 11, 3 por nivel con contador, que v0
            // no implementa. El flujo rechaza el paquete en lugar de dar Marea ilimitada en el 11.
            string level11 = TestPacks.MinimalLevel().Replace("\"id\": \"m01_v1_n0001\", \"level\": 1", "\"id\": \"m01_v1_n0011\", \"level\": 11");
            LevelPack pack = LevelPackReader.Read(TestPacks.MinimalPack(level11, "\"pack_schema\": 1"));
            Assert.That(pack.First.Level, Is.EqualTo(11));
            Assert.That(() => { new LevelFlow(pack, new MemoryProgressStore(), new RecordingListener()); }, Throws.TypeOf<System.NotSupportedException>());
        }

        [Test]
        public void HudActions_AreBlockedDuringTheIntro()
        {
            _flow.Load(1);
            Assert.That(_flow.CanRestart, Is.False);
            Assert.That(_flow.Restart(), Is.False);
            Assert.That(_flow.Next(), Is.False);
        }

        [Test]
        public void LoadSaved_UsesTheProgress_OrTheFirstLevel()
        {
            _progress.SaveNextLevel(4);
            _flow.LoadSaved();
            Assert.That(_flow.LevelNumber, Is.EqualTo(4));

            var empty = new LevelFlow(TestPacks.Tutorial, new MemoryProgressStore(99), new RecordingListener());
            empty.LoadSaved();
            Assert.That(empty.LevelNumber, Is.EqualTo(1));
        }

        private void StartPlaying(int level)
        {
            _flow.Load(level);
            _flow.TapAnywhere();
            Assert.That(_flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        // Juega la solución del solver, en el modo de vertido de la sesión, con toques origen → destino, sin esperar a la
        // vista (encolado, 02 §3.2). El par del paquete es el del modo por defecto (run, D-005).
        private void PlaySolution()
        {
            LevelDefinition level = _flow.Level;
            SolveResult result = new AStarSolver().Solve(_flow.Session.Current, SolverOptions.ForPipeline(_flow.Session.PourMode));
            Assert.That(result.IsSolved, Is.True);
            Assert.That(result.MoveCount, Is.EqualTo(level.Par));
            for (int i = 0; i < result.MoveCount; i++)
            {
                Pour move = result.GetMove(i);
                Assert.That(_flow.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected), "origen " + i);
                TapResult poured = _flow.TapCuia(move.Target);
                Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), "destino " + i);
                Assert.That(poured.Pour.Pour.Amount, Is.EqualTo(move.Amount), "cantidad " + i);
            }

            Assert.That(_flow.Session.AppliedPours, Is.EqualTo(level.Par));
        }
    }
}
