#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Prueba de conjunto de los nueve estrenos de la ola 3 (criterio de la prueba del dueño del 2026-10-09; tanda B2,
    /// ola3.conjunto): en 43, 63, 83, 100, 103, 123, 143, 163 y 183 se juega el vertido que dispara la mecánica (y los previos
    /// que lleven a él) y, en el registro de pasos de cada vertido, salvo los terminados antes de tiempo, ningún Open,
    /// Release, Receive, RainDrop, RainEnd, Bite ni MudWash usa el paso por defecto ni dura 0 ms, cada paso dura su nominal de
    /// b.2 × factor (<see cref="MechanicTestKit.AssertOwnTimedSteps"/>) y ningún vertido pasa de 2.800 ms del toque a la vista
    /// quieta. El paso que estrena la mecánica lo anima el overlay (o las marcas) de su cuia.
    /// </summary>
    public sealed class MechanicDebutsTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Debut_TriggerPour_OwnStepsAtNominalDuration([Values(43, 63, 83, 100, 103, 123, 143, 163, 183)] int level)
        {
            yield return MechanicTestKit.LoadPlaying(level);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardState start = boot.Flow!.Session.Current;
            MechanicStepKind kind = TriggerKind(level);
            MechanicTestKit.PourWanted wanted = Wanted(level, start);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, wanted);
            Assert.That(path, Is.Not.Null, "vertidos que estrenan la mecánica del nivel " + level);

            BoardAnimator animator = boot.View!.Animator;
            int checkedSteps = 0;
            int worst = 0;
            for (int i = 0; i < path!.Length; i++)
            {
                bool trigger = i == path.Length - 1;
                string what = "nivel " + level + (trigger ? ", vertido que estrena" : ", vertido previo " + i) + " (" + path[i].Source + " → " + path[i].Target + ")";
                animator.Steps.ClearLog();
                PourVisual visual = MechanicTestKit.TapPour(boot, path[i], out TapResult poured);
                int elapsed = MechanicTestKit.StepToIdle(boot, 6000);
                worst = Mathf.Max(worst, elapsed);
                Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), what + ": del toque a la vista quieta. " + MechanicTestKit.Describe(animator));
                MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
                checkedSteps += MechanicTestKit.AssertOwnTimedSteps(animator, what);
                if (!trigger)
                {
                    continue;
                }

                int owner = TriggerOwner(level, start, path[i], poured);
                MechanicStepRecord record = MechanicTestKit.Record(animator, kind, owner);
                Assert.That(record.UsedDefault || record.Snapped, Is.False, what + ": el estreno lo anima su mecánica, entero");
                Assert.That(record.EndMs - record.StartMs, Is.GreaterThan(0f), what + ": y dura más de 0 ms");
                MechanicTestKit.AssertViewMatchesModel(boot);
                TestContext.WriteLine("Nivel " + level + ": " + path.Length + " vertidos, el más largo " + worst + " ms del toque a la vista quieta; "
                    + checkedSteps + " pasos Open/Release/Receive/RainDrop/RainEnd/Bite/MudWash propios. " + MechanicTestKit.Describe(animator));
            }

        }

        // Lo que estrena cada nivel (02 §9.1 con D-007; lista única de debuts del paso 2).
        private static MechanicStepKind TriggerKind(int level)
        {
            switch (level)
            {
                case 43:
                case 163:
                    return MechanicStepKind.Open;
                case 63:
                    return MechanicStepKind.Seal;
                case 83:
                    return MechanicStepKind.Release;
                case 100:
                    return MechanicStepKind.Reveal;
                case 103:
                    return MechanicStepKind.Receive;
                case 123:
                    return MechanicStepKind.MudWash;
                case 143:
                    return MechanicStepKind.RainDrop;
                default:
                    return MechanicStepKind.Bite;
            }
        }

        // El vertido que dispara la mecánica del nivel (los mismos que MechanicStepsTests).
        private static MechanicTestKit.PourWanted Wanted(int level, BoardState start)
        {
            switch (level)
            {
                case 43:
                {
                    int bit = 1 << IndexOf(start, MechanicType.Hoja);
                    return (ev, b, a) => (ev.HojaOpenedMask & bit) != 0;
                }

                case 63:
                {
                    int bit = 1 << IndexOf(start, MechanicType.Matapi);
                    return (ev, b, a) => (ev.SealedMask & bit) != 0;
                }

                case 83:
                {
                    int bit = 1 << IndexOf(start, MechanicType.Liana);
                    return (ev, b, a) => (ev.LianaReleasedMask & bit) != 0;
                }

                case 100:
                    return (ev, b, a) => ev.RevealedMask != 0;
                case 103:
                    return (ev, b, a) => ev.ThroughRemolino;
                case 123:
                    return (ev, b, a) => ev.BarroWashedMask != 0;
                case 143:
                {
                    int bit = 1 << IndexOf(start, MechanicType.Lluvia);
                    return (ev, b, a) => (ev.LluviaDropMask & bit) != 0 && (ev.LluviaEndedMask & bit) == 0;
                }

                case 163:
                {
                    int bit = 1 << IndexOf(start, MechanicType.Brote);
                    return (ev, b, a) => (ev.BroteOpenedMask & bit) != 0;
                }

                default:
                {
                    int bit = 1 << IndexOf(start, MechanicType.Pirana);
                    return (ev, b, a) => (ev.PiranaCaveMask & bit) != 0;
                }
            }
        }

        // Cuia dueña del paso que estrena la mecánica.
        private static int TriggerOwner(int level, BoardState start, Pour pour, TapResult poured)
        {
            switch (level)
            {
                case 43:
                    return IndexOf(start, MechanicType.Hoja);
                case 63:
                    return IndexOf(start, MechanicType.Matapi);
                case 83:
                    return IndexOf(start, MechanicType.Liana);
                case 100:
                    return pour.Source; // se revela el origen
                case 103:
                    return poured.Pour.Receiver; // recibe la pareja
                case 123:
                    return pour.Target; // el barro del receptor
                case 143:
                    return IndexOf(start, MechanicType.Lluvia);
                case 163:
                    return IndexOf(start, MechanicType.Brote);
                default:
                    return IndexOf(start, MechanicType.Pirana);
            }
        }

        private static int IndexOf(BoardState board, MechanicType type)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].Mechanic == type)
                {
                    return i;
                }
            }

            Assert.Fail("El tablero no tiene " + type);
            return -1;
        }
    }
}
