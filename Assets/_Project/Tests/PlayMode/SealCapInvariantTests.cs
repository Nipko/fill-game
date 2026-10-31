#nullable enable
using System;
using System.Collections;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La tapa sigue al modelo (informe del dueño tras la prueba del release 1.0.0 (10001): «una tapa de cuia no se puso al
    /// final del nivel», en uno de los niveles 5-9 del mundo 1). Invariante: con la cola quieta o tras
    /// <see cref="BoardAnimator.SnapAll"/>, toda cuia sellada en el modelo tiene la tapa visible (<see cref="CuiaView.Sealed"/>
    /// y <c>SealCap</c> activa, opaca y en su sitio) y ninguna sin sellar la tiene. Se recorren los tableros reales de los niveles
    /// 5-9 (vertido en tramo, D-005) con toques rápidos, encadenados, con pausas y fotogramas de duración variable, saltos de la
    /// cola en cualquier instante (también con el sello a medias), Marea cerca del final y la victoria, en un tablero propio y en
    /// la escena real.
    /// </summary>
    public sealed class SealCapInvariantTests
    {
        private const int FrameMs = 16;
        private static readonly PourTimings Timings = new PourTimings(100f, 300f, 280f, 80f, 300f);
        private static readonly int[] Frames = { 8, 16, 16, 16, 33, 50, 100 };
        private static ProceduralArt? s_art;

        // Niveles 5-9 del mundo 1 (tutorial_v0.json, m01_v1_n0005 a n0009): capacidad 4, sin mecánicas.
        private static readonly int[][][] Levels =
        {
            new[] { new[] { 8, 4, 8, 8 }, new[] { 1, 4, 1, 1 }, new[] { 4, 3, 3, 4 }, new[] { 3, 1, 3, 8 }, new int[0], new int[0] },
            new[] { new[] { 8, 1, 4, 4 }, new[] { 3, 8, 8, 1 }, new[] { 4, 3, 3, 1 }, new[] { 1, 4, 8, 3 }, new int[0] },
            new[] { new[] { 8, 8, 7, 7 }, new[] { 3, 4, 4, 7 }, new[] { 1, 1, 8, 3 }, new[] { 3, 7, 8, 4 }, new[] { 4, 1, 1, 3 }, new int[0], new int[0] },
            new[] { new[] { 4, 4, 7, 7 }, new[] { 3, 1, 8, 7 }, new[] { 4, 4, 3, 1 }, new[] { 1, 8, 3, 7 }, new[] { 1, 3, 8, 8 }, new int[0], new int[0] },
            new[] { new[] { 1, 7, 7, 8 }, new[] { 4, 4, 8, 1 }, new[] { 3, 4, 1, 7 }, new[] { 7, 8, 4, 3 }, new[] { 3, 1, 3, 8 }, new int[0], new int[0] },
        };

        private Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            LaunchOptions.Reset();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        /// <summary>
        /// La solución del solucionador tocada de golpe (sin avanzar el reloj entre vertidos) y la victoria con la cola aún
        /// llena: <see cref="BoardAnimator.SnapAll"/> en cada instante del último sello (cada 8 ms hasta pasado su fin).
        /// </summary>
        [UnityTest]
        public IEnumerator Solution_SnapAllAtEveryInstantOfTheLastSeal_CapsFollowModel([NUnit.Framework.Range(0, 4)] int level, [Values(false, true)] bool queuedPrefix)
        {
            BoardState start = Build(Levels[level]);
            Pour[] solution = Solve(start);
            BoardState beforeLast = start;
            for (int i = 0; i < solution.Length - 1; i++)
            {
                beforeLast = PourRules.Apply(beforeLast, solution[i].Source, solution[i].Target, PourMode.Run);
            }

            for (int snapAt = 0; snapAt <= 2400; snapAt += FrameMs)
            {
                // Con queuedPrefix, toda la solución se toca de golpe (la cola llena); si no, el último vertido sobre la cola quieta.
                yield return StartRig(queuedPrefix ? start : beforeLast);
                Rig rig = _rig!;
                BoardState state = queuedPrefix ? start : beforeLast;
                if (queuedPrefix)
                {
                    for (int i = 0; i < solution.Length - 1; i++)
                    {
                        state = rig.Pour(state, solution[i]);
                    }
                }

                state = rig.Pour(state, solution[solution.Length - 1]);
                Assert.That(state.IsWon(), Is.True);
                rig.Run(snapAt);
                if (rig.Animator.IsIdle)
                {
                    AssertCaps(rig, state, "nivel " + (level + 5) + ", cola quieta a los " + snapAt + " ms del último vertido");
                }

                rig.Animator.SnapAll();
                AssertCaps(rig, state, "nivel " + (level + 5) + ", SnapAll a los " + snapAt + " ms del último vertido");
            }
        }

        /// <summary>
        /// Partidas al azar (semilla fija) sobre los tableros reales: la jugada del solucionador o una jugada útil cualquiera,
        /// toques sin pausa o con pausas de hasta 900 ms, fotogramas de 8 a 100 ms, saltos de la cola en cualquier instante y
        /// Marea; tras cada salto y cada vez que la cola queda quieta, la tapa como el modelo.
        /// </summary>
        [UnityTest]
        public IEnumerator RandomPlay_FastTaps_SnapsAndUndo_CapsFollowModel([NUnit.Framework.Range(0, 4)] int level)
        {
            BoardState start = Build(Levels[level]);
            var buffer = new Pour[PourRules.MaxPours];
            var solver = new AStarSolver();
            SolverOptions options = SolverOptions.ForPipeline(PourMode.Run);
            for (int seed = 0; seed < 40; seed++)
            {
                yield return StartRig(start);
                Rig rig = _rig!;
                var rnd = new System.Random((level * 1000) + seed);
                BoardState state = start;
                var history = new BoardState[200];
                var played = new Pour[200];
                int depth = 0;
                string where = "nivel " + (level + 5) + ", semilla " + seed;
                for (int move = 0; move < 120 && !state.IsWon(); move++)
                {
                    // Marea (sobre todo cerca del final): la cola salta y las dosis vuelven (GameScreen.OnUndo).
                    if (depth > 0 && rnd.NextDouble() < 0.12)
                    {
                        depth--;
                        BoardState previous = history[depth];
                        rig.Undo(state, previous, played[depth]);
                        state = previous;
                        AssertCaps(rig, state, where + ", tras la Marea (SnapAll y Marea en cola)", queued: true);
                    }
                    else
                    {
                        Pour next;
                        int useful = PourRules.CollectUsefulMoves(state, true, PourMode.Run, buffer);
                        if (useful == 0)
                        {
                            break;
                        }

                        if (rnd.NextDouble() < 0.7)
                        {
                            SolveResult solved = solver.Solve(state, options);
                            next = solved.IsSolved && solved.MoveCount > 0 ? solved.GetMove(0) : buffer[rnd.Next(useful)];
                        }
                        else
                        {
                            next = buffer[rnd.Next(useful)];
                        }

                        history[depth] = state;
                        played[depth] = next;
                        depth++;
                        state = rig.Pour(state, next);
                    }

                    // Pausa: ninguna (toques encadenados), un fotograma o hasta 900 ms en fotogramas variables.
                    double gap = rnd.NextDouble();
                    int waitMs = gap < 0.4 ? 0 : gap < 0.55 ? FrameMs : rnd.Next(900);
                    for (int t = 0; t < waitMs;)
                    {
                        int dt = Frames[rnd.Next(Frames.Length)];
                        rig.Tick(dt);
                        t += dt;
                        if (rig.Animator.IsIdle)
                        {
                            AssertCaps(rig, state, where + ", cola quieta tras la jugada " + move);
                        }
                    }

                    // Salto de la cola (vuelta de segundo plano, relayout): en cualquier instante.
                    if (rnd.NextDouble() < 0.08)
                    {
                        rig.Animator.SnapAll();
                        AssertCaps(rig, state, where + ", SnapAll tras la jugada " + move);
                    }
                }

                // Fin: como LevelFlow, se espera la cola (≤ MaxViewDrainWaitMs) y la celebración hace SnapAll.
                for (int t = 0; t < GameConfig.MaxViewDrainWaitMs && !rig.Animator.IsIdle;)
                {
                    int dt = Frames[rnd.Next(Frames.Length)];
                    rig.Tick(dt);
                    t += dt;
                }

                rig.Animator.SnapAll();
                AssertCaps(rig, state, where + ", celebración (" + (state.IsWon() ? "ganado" : "sin ganar") + ")");
            }
        }

        /// <summary>
        /// Escena real, niveles 5-9: la solución tocada de golpe, con un fotograma o con 200 ms entre vertidos; en la celebración
        /// (tras su SnapAll) y en la pantalla de victoria, la tapa como el modelo en todas las cuias.
        /// </summary>
        [UnityTest]
        public IEnumerator Game_Levels5To9_FastSolution_CapsOnVictory([NUnit.Framework.Range(5, 9)] int level, [Values(0, 16, 200)] int gapMs)
        {
            yield return MechanicTestKit.LoadPlaying(level);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                Assert.That(MechanicTestKit.Tap(boot, move).Kind, Is.EqualTo(TapKind.Poured), "vertido " + i);
                if (gapMs > 0)
                {
                    PlayModeHelpers.Step(boot, gapMs);
                }
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            bool sawCelebration = false;
            for (int t = 0; t < 12000 && flow.Phase != FlowPhase.VictoryScreen; t += FrameMs)
            {
                PlayModeHelpers.Step(boot, FrameMs);
                if (!sawCelebration && flow.Phase == FlowPhase.Celebration)
                {
                    sawCelebration = true;
                    AssertCaps(boot.View!.Views, boot.View.Animator, flow.Session.Current, "nivel " + level + ", celebración");
                }
            }

            Assert.That(sawCelebration, Is.True, "hubo celebración");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            AssertCaps(boot.View!.Views, boot.View.Animator, flow.Session.Current, "nivel " + level + ", pantalla de victoria");
        }

        /// <summary>
        /// Escena real, niveles 5-9: Marea en el penúltimo vertido (si el nivel la permite) y otra vez la jugada, el último
        /// vertido con fotogramas de 100 ms (el tope de GameBootstrap) y una vuelta de segundo plano (SnapToModel) a mitad de su
        /// sello; la tapa como el modelo en la celebración, en la pantalla de victoria y 4 s después.
        /// </summary>
        [UnityTest]
        public IEnumerator Game_Levels5To9_UndoHitchesAndBackground_CapsOnVictory([NUnit.Framework.Range(5, 9)] int level, [Values(0, 300, 600, 900)] int backgroundAtMs)
        {
            yield return MechanicTestKit.LoadPlaying(level);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            int n = solution.MoveCount;
            for (int i = 0; i < n - 1; i++)
            {
                Assert.That(MechanicTestKit.Tap(boot, solution.GetMove(i)).Kind, Is.EqualTo(TapKind.Poured), "vertido " + i);
                if (i == n - 2 && flow.CanUndo)
                {
                    PlayModeHelpers.Step(boot, 400); // la Marea con el sello del penúltimo vertido a medias
                    Assert.That(boot.PressUndo(), Is.True, "Marea");
                    PlayModeHelpers.Step(boot, 100);
                    Assert.That(MechanicTestKit.Tap(boot, solution.GetMove(i)).Kind, Is.EqualTo(TapKind.Poured), "vertido " + i + " otra vez");
                }
            }

            Assert.That(MechanicTestKit.Tap(boot, solution.GetMove(n - 1)).Kind, Is.EqualTo(TapKind.Poured), "último vertido");
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            bool backgrounded = false;
            bool sawCelebration = false;
            for (int t = 0; t < 12000 && flow.Phase != FlowPhase.VictoryScreen; t += 100)
            {
                if (!backgrounded && backgroundAtMs > 0 && t >= backgroundAtMs)
                {
                    backgrounded = true;
                    boot.View!.SnapToModel();
                    AssertCaps(boot.View.Views, boot.View.Animator, flow.Session.Current, "nivel " + level + ", vuelta de segundo plano a los " + t + " ms");
                }

                boot.View!.Tick(100f);
                flow.Tick(100, boot.View.IsViewIdle);
                PlayModeHelpers.AutoNext(boot);
                if (!sawCelebration && flow.Phase == FlowPhase.Celebration)
                {
                    sawCelebration = true;
                    AssertCaps(boot.View.Views, boot.View.Animator, flow.Session.Current, "nivel " + level + ", celebración");
                }
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            AssertCaps(boot.View!.Views, boot.View.Animator, flow.Session.Current, "nivel " + level + ", pantalla de victoria");
            PlayModeHelpers.Step(boot, 4000);
            if (flow.Phase == FlowPhase.VictoryScreen)
            {
                AssertCaps(boot.View.Views, boot.View.Animator, flow.Session.Current, "nivel " + level + ", 4 s en la pantalla de victoria");
            }
        }

        // ---------------------------------------------------------------------------------------------------- invariante

        private static void AssertCaps(Rig rig, BoardState model, string when, bool queued = false)
        {
            if (!queued)
            {
                AssertCaps(rig.Views, rig.Animator, model, when);
                return;
            }

            // Con la Marea en cola, solo las cuias que no anima: el resto se comprueba al quedar quieta.
            rig.RunUntilIdle(30000);
            AssertCaps(rig.Views, rig.Animator, model, when + " (al quedar quieta)");
        }

        /// <summary>
        /// Con la cola quieta (o tras SnapAll), cada vista lleva la tapa del modelo: <see cref="CuiaView.Sealed"/>, la imagen
        /// <c>SealCap</c> activa en la jerarquía, opaca, a escala 1 y en su sitio de reposo; sin sellar, la tapa apagada.
        /// </summary>
        public static void AssertCaps(CuiaView[] views, BoardAnimator animator, BoardState model, string when)
        {
            var failures = new StringBuilder();
            for (int i = 0; i < model.Count && i < views.Length; i++)
            {
                CuiaView v = views[i];
                bool expected = model.IsSealed(i);
                Image cap = v.CapImage;
                bool capOn = cap.gameObject.activeInHierarchy;
                if (v.Sealed != expected || capOn != expected)
                {
                    failures.Append("cuia ").Append(i).Append(": modelo sellado=").Append(expected).Append(", Sealed=").Append(v.Sealed)
                        .Append(", SealCap activa=").Append(capOn).Append("; ");
                    continue;
                }

                if (expected)
                {
                    float restY = (v.HeightU - 0.04f) * v.Unit;
                    if (cap.color.a < 0.99f || Mathf.Abs(cap.rectTransform.anchoredPosition.y - restY) > 0.01f
                        || Mathf.Abs(cap.rectTransform.localScale.x - 1f) > 1e-4f)
                    {
                        failures.Append("cuia ").Append(i).Append(": tapa fuera de su reposo (alfa ").Append(cap.color.a)
                            .Append(", y ").Append(cap.rectTransform.anchoredPosition.y).Append(" de ").Append(restY).Append("); ");
                    }
                }

                if (animator.IsIdle && !animator.ShownOf(i).Equals(model[i]))
                {
                    failures.Append("cuia ").Append(i).Append(": contenido distinto del modelo; ");
                }
            }

            Assert.That(failures.Length, Is.EqualTo(0), when + ": " + failures);
        }

        // ---------------------------------------------------------------------------------------------------- utilidades

        private static BoardState Build(int[][] cuias)
        {
            var caps = new int[cuias.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = 4;
            }

            BoardBuildResult result = BoardBuilder.Build(caps, cuias, (MechanicSpec[]?)null);
            Assert.That(result.Error, Is.Null);
            return result.State!;
        }

        private static Pour[] Solve(BoardState start)
        {
            SolveResult solved = new AStarSolver().Solve(start, SolverOptions.ForPipeline(PourMode.Run));
            Assert.That(solved.IsSolved, Is.True);
            return solved.CopyMoves();
        }

        private IEnumerator StartRig(BoardState board)
        {
            _rig?.Dispose();
            _rig = new Rig(Art, board);
            yield return null; // un fotograma para que el Canvas se construya
        }

        /// <summary>Canvas con el tablero, las capas, los efectos y una vista por cuia, como GameScreen (PourVisualTests).</summary>
        private sealed class Rig : IDisposable
        {
            private readonly GameObject _root;
            private bool _disposed;

            public Rig(ProceduralArt art, BoardState board)
            {
                _root = new GameObject("SealCapRig", typeof(RectTransform), typeof(Canvas));
                var canvas = _root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2
                    | AdditionalCanvasShaderChannels.TexCoord3;
                var rootRt = (RectTransform)_root.transform;
                RectTransform boardRt = NewLayer("Board", rootRt);
                RectTransform back = NewLayer("MechanicBack", boardRt);
                Fx = new FxLayer(boardRt, art);
                Views = new CuiaView[board.Count];
                for (int i = 0; i < board.Count; i++)
                {
                    Views[i] = CuiaView.Create(boardRt, i, board[i].Capacity, art, index => { }, (index, inside) => { }, Fx);
                    Views[i].SetGeometry(new Vector2(36f + (i * 48f), -600f), 40f, 60f);
                    Views[i].SetLiftDuration(100f);
                }

                RectTransform front = NewLayer("MechanicFront", boardRt);
                RectTransform streams = NewLayer("Streams", boardRt);
                Fx.Root.SetAsLastSibling();
                RectTransform pourFront = NewLayer("PourFront", rootRt);
                Animator = new BoardAnimator(streams, pourFront) { Fx = Fx, BoardWidth = 360f };
                Animator.Bind(Views, Timings, 2, back, front);
                for (int i = 0; i < board.Count; i++)
                {
                    Animator.ShowContent(i, board[i], board.IsSealed(i));
                }
            }

            public BoardAnimator Animator { get; }

            public CuiaView[] Views { get; }

            public FxLayer Fx { get; }

            // Aplica el vertido en el modelo y lo encola como GameScreen.EnqueuePour; devuelve el tablero nuevo.
            public BoardState Pour(BoardState state, Pour pour)
            {
                Assert.That(PourRules.IsLegal(state, pour.Source, pour.Target), Is.True, "vertido legal " + pour.Source + " → " + pour.Target);
                BoardState after = PourRules.Apply(state, pour.Source, pour.Target, PourMode.Run, out PourEvents ev);
                Animator.EnqueuePour(new PourVisual(new Pour(pour.Source, pour.Target, ev.Amount), ev, state, after));
                return after;
            }

            // Marea como GameScreen.OnUndo: la cola salta, las dosis vuelven de la tocada al origen y las demás cuias saltan.
            public void Undo(BoardState before, BoardState after, Pour undone)
            {
                Animator.SnapAll();
                int from = undone.Target;
                int to = undone.Source;
                int moved = 0;
                if (before[from].Packed != after[from].Packed || before.IsSealed(from) != after.IsSealed(from))
                {
                    Animator.EnqueueUndo(from, to, after[to].Count - before[to].Count, before[from].TopColor, before[from], after[from], after.IsSealed(from),
                        before[to], after[to], after.IsSealed(to));
                    moved = (1 << from) | (1 << to);
                }

                for (int i = 0; i < after.Count; i++)
                {
                    if (((moved >> i) & 1) == 0 && (before[i].Packed != after[i].Packed || before.IsSealed(i) != after.IsSealed(i)))
                    {
                        Animator.ShowContent(i, after[i], after.IsSealed(i));
                    }
                }
            }

            public void Tick(float dtMs)
            {
                Animator.Tick(dtMs);
                for (int i = 0; i < Views.Length; i++)
                {
                    Views[i].Tick(dtMs);
                }

                Fx.Tick(dtMs);
            }

            public void Run(int ms)
            {
                for (int t = 0; t < ms; t += FrameMs)
                {
                    Tick(FrameMs);
                }
            }

            public void RunUntilIdle(int maxMs)
            {
                for (int t = 0; !Animator.IsIdle && t < maxMs; t += FrameMs)
                {
                    Tick(FrameMs);
                }

                Assert.That(Animator.IsIdle, Is.True, "la cola queda quieta");
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Object.Destroy(_root);
            }

            private static RectTransform NewLayer(string name, RectTransform parent)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return rt;
            }
        }
    }
}
