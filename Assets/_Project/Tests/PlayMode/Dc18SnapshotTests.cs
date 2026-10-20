#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de DC18 (P12 de D-006, opción c; D-030: 3 filas al cargar con T generadas ≥ 13; <c>[Explicit]</c>, no forman parte
    /// de la suite), en 16:9 (1080 × 1920 px, <see cref="HudLabelTests.Screen640"/>; GameScreen lo maqueta en 438,75 × 780 dp):
    /// <list type="bullet">
    /// <item><description><c>dc18_l374_13_16x9</c>: el 374 (13 cuias de 5 dosis), que en este lienzo iba en 2 filas (7/6) y ahora va
    /// en 3 (5/4/4);</description></item>
    /// <item><description><c>dc18_l&lt;n&gt;_14_16x9</c> y <c>dc18_l&lt;n&gt;_14mas2_16x9</c>: el primer nivel de 14 cuias de
    /// <see cref="FourteenCandidates"/> en el que el buscador llega a Aguas estancadas con el rescate a la venta, al cargar
    /// (5/5/4) y con la Cuia de rescate y la Cuia extra (14 + 2: 5/5/6, ninguna fila de 8).</description></item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.Dc18SnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas de DC18; necesitan dispositivo gráfico.")]
    public sealed class Dc18SnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator Capture13Cuias_In16x9()
        {
            string dir = SnapshotKit.Prepare();
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Load(374);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.Views.Length, Is.EqualTo(13));
            Assert.That(boot.View.Layout!.Rows, Is.EqualTo(3), "13 generadas: 3 filas (P12 de D-006, c)");
            Assert.That(Split(boot.View.Layout), Is.EqualTo("5/4/4"));
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.SaveTop(dir, "dc18_l374_13_16x9", (int)HudLabelTests.Screen640.y);
            SnapshotKit.LogBoard("dc18_l374_13_16x9", boot);
        }

        // Niveles de 14 cuias de los mundos 7-9: el 640 (matapí) es el primero en el que el buscador llega a Aguas estancadas con
        // el rescate a la venta (camino de 1 vertido; 694, 344 y 345 no llegan); los demás, de respaldo.
        private static readonly int[] FourteenCandidates = { 640, 694, 344, 345, 343, 369, 378, 335, 580, 600, 620, 660, 680, 700 };

        [UnityTest]
        public IEnumerator Capture14PlusTwo_In16x9()
        {
            string dir = SnapshotKit.Prepare();
            GameBootstrap? boot = null;
            Pour[]? path = null;
            int level = 0;
            foreach (int candidate in FourteenCandidates)
            {
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(5000));
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                yield return Load(candidate);
                boot = GameBootstrap.Current!;
                path = FindRescuableStuck(boot.Flow!);
                Debug.Log("DC18: nivel " + candidate + (path == null ? ": sin camino hasta Aguas estancadas" : ": camino de " + path.Length));
                if (path != null)
                {
                    level = candidate;
                    break;
                }
            }

            Assert.That(path, Is.Not.Null, "algún nivel de 14 cuias llega a Aguas estancadas con el rescate a la venta");
            GameScreen view = boot!.View!;
            string name = "dc18_l" + level + "_14";
            Assert.That(view.Views.Length, Is.EqualTo(14));
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "14 generadas: 3 filas (P12 de D-006, c)");
            Assert.That(Split(view.Layout), Is.EqualTo("5/5/4"));
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.SaveTop(dir, name + "_16x9", (int)HudLabelTests.Screen640.y);
            SnapshotKit.LogBoard(name + "_16x9", boot);

            // El rescate de verdad (como PhoneFixesTests.RescueForReal) y después la Cuia extra.
            LevelFlow flow = boot.Flow!;
            MechanicTestKit.PlayPrefix(boot, path!, false);
            for (int ms = 0; ms < 8000 && flow.Phase != FlowPhase.StuckModal; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, 300);
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "el rescate aplicado");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Views.Length, Is.EqualTo(15), "con la Cuia de rescate");
            Assert.That(boot.PressExtraCuia(), Is.True, "Cuia extra con semillas");
            PlayModeHelpers.Step(boot, 700); // la recolocación (600 ms)
            Assert.That(view.Views.Length, Is.EqualTo(16), "14 + 2");
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "las filas no cambian a mitad de nivel (D-006, punto 4)");
            Assert.That(Split(view.Layout), Is.EqualTo("5/5/6"), "ninguna fila de 8 (P12)");
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            yield return SnapshotKit.Wait(0.3f);
            SnapshotKit.SaveTop(dir, name + "mas2_16x9", (int)HudLabelTests.Screen640.y);
            SnapshotKit.LogBoard(name + "mas2_16x9", boot);
        }

        // Camino de vertidos útiles hasta el primer Aguas estancadas con el rescate a la venta, en profundidad y probando antes
        // los vertidos a una cuia vacía (así se llenan antes de tiempo); null si no lo encuentra (solo tests: asigna).
        private static Pour[]? FindRescuableStuck(LevelFlow flow)
        {
            var seen = new HashSet<BoardState>();
            var trail = new List<Pour>();
            var rescue = new RescueBooster(flow.Boosters);
            return Visit(flow.Session.Current, flow, rescue, seen, trail, 0) ? trail.ToArray() : null;
        }

        private static bool Visit(BoardState state, LevelFlow flow, RescueBooster rescue, HashSet<BoardState> seen, List<Pour> trail, int depth)
        {
            if (depth >= 80 || seen.Count >= 60000 || !seen.Add(state))
            {
                return false;
            }

            bool futile = flow.Session.FutileRuleEnabled;
            PourMode mode = flow.Session.PourMode;
            var moves = new Pour[PourRules.MaxPours];
            int n = PourRules.CollectUsefulMoves(state, futile, mode, moves);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    if (state[moves[i].Target].IsEmpty != (pass == 0))
                    {
                        continue;
                    }

                    var game = new LevelSession(state, futile, mode);
                    if (game.Status != LevelStatus.Playing || !game.TryPour(moves[i].Source, moves[i].Target, out PourResult poured)
                        || poured.Status == LevelStatus.Won)
                    {
                        continue;
                    }

                    trail.Add(moves[i]);
                    if (poured.Status == LevelStatus.Stuck)
                    {
                        if (RescueBooster.IsOffered(rescue.Check(game.Current, mode, futile)))
                        {
                            return true;
                        }
                    }
                    else if (Visit(game.Current, flow, rescue, seen, trail, depth + 1))
                    {
                        return true;
                    }

                    trail.RemoveAt(trail.Count - 1);
                }
            }

            return false;
        }

        private static IEnumerator Load(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 6000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + level);
            Assert.That(boot.View!.CanvasWidth, Is.EqualTo(438.75f).Within(0.01f), "16:9");
            PlayModeHelpers.Step(boot, 400);
            Debug.Log("DC18: nivel " + level + " en " + boot.View.CanvasWidth + " dp de ancho");
        }

        private static string Split(BoardLayoutResult r)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < r.Rows; i++)
            {
                if (i > 0)
                {
                    text.Append('/');
                }

                text.Append(r.PerRow[i]);
            }

            return text.ToString();
        }
    }
}
