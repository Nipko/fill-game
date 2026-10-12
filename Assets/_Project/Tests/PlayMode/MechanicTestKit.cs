#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Kit de las pruebas de pasos de mecánica (T07; congelado tras la ola 2, lo usan T11-T17): busca en niveles reales los
    /// vertidos que disparan cada evento de R10 con <see cref="AStarSolver"/> y <see cref="PourRules"/> (búsqueda limitada),
    /// los juega con <see cref="GameBootstrap.TapCuia"/> y el reloj del test (<see cref="PlayModeHelpers.Step"/>), mide del
    /// toque a la vista quieta, lleva el reloj hasta un instante de un paso, compara el registro de pasos ejecutado con el plan
    /// del mismo vertido (<see cref="AssertLogFollowsPlan"/>) y ofrece un banco sintético (<see cref="Rig"/>) con overlays de
    /// prueba (<see cref="ProbeOverlay"/>).
    /// </summary>
    /// <remarks>
    /// Búsqueda de un disparador (<see cref="FindTrigger"/>): primero la solución del solver (presupuesto
    /// <see cref="SolverNodes"/>) mirando en cada estado todos los vertidos legales (un paso de más); si ninguno sirve, una
    /// búsqueda en anchura de como mucho <see cref="SearchStates"/> estados. Los estados ganados o en Aguas estancadas no
    /// sirven de paso. Devuelve el camino entero: los vertidos previos y, el último, el que dispara el evento.
    /// </remarks>
    internal static class MechanicTestKit
    {
        /// <summary>Nodos del solver para la solución de referencia.</summary>
        public const int SolverNodes = 120000;

        /// <summary>Estados como mucho de la búsqueda en anchura de respaldo.</summary>
        public const int SearchStates = 40000;

        /// <summary>Del toque a la vista quieta, como mucho (presupuesto de <see cref="MechanicStepQueue"/>).</summary>
        public const int BudgetMs = (int)MechanicStepQueue.BudgetFromTapMs;

        /// <summary>
        /// Tolerancia (ms) al comparar instantes del registro de pasos: el reloj de la cola y el del plan se suman por
        /// separado en coma flotante (<see cref="AssertLogFollowsPlan"/>).
        /// </summary>
        public const float LogToleranceMs = 0.05f;

        /// <summary>Lo que se busca en un vertido: sus eventos de R10, el tablero antes y el de después.</summary>
        public delegate bool PourWanted(PourEvents ev, BoardState before, BoardState after);

        /// <summary>
        /// Carga el nivel <paramref name="level"/>, quita el reloj a la escena, salta la intro y avanza hasta Playing con la
        /// vista quieta.
        /// </summary>
        public static IEnumerator LoadPlaying(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "fase Playing del nivel " + level);
            boot.View!.Animator.Steps.ClearLog();
        }

        /// <summary>Primera cuia del nivel cargado con la mecánica <paramref name="type"/> (falla si no hay).</summary>
        public static int IndexOf(LevelFlow flow, MechanicType type)
        {
            BoardState b = flow.Session.Current;
            for (int i = 0; i < b.Count; i++)
            {
                if (b[i].Mechanic == type)
                {
                    return i;
                }
            }

            Assert.Fail("El nivel " + flow.LevelNumber + " no tiene " + type);
            return -1;
        }

        /// <summary>
        /// Vertidos desde el tablero actual de <paramref name="flow"/> cuyo último cumple <paramref name="wanted"/> (ver la
        /// clase); null si no aparece dentro de los límites.
        /// </summary>
        public static Pour[]? FindTrigger(LevelFlow flow, PourWanted wanted)
        {
            return FindTrigger(flow.Session.Current, flow.Session.PourMode, flow.Session.FutileRuleEnabled, wanted);
        }

        /// <summary>Como <see cref="FindTrigger(LevelFlow, PourWanted)"/> desde <paramref name="start"/>.</summary>
        public static Pour[]? FindTrigger(BoardState start, PourMode mode, bool futileRule, PourWanted wanted)
        {
            var path = new List<Pour>();
            SolveResult solution = new AStarSolver().Solve(start, new SolverOptions(SolverNodes, 2, futileRule, mode));
            BoardState current = start;
            for (int i = 0; i <= solution.MoveCount; i++)
            {
                if (TryOneMore(current, mode, wanted, out Pour hit))
                {
                    path.Add(hit);
                    return path.ToArray();
                }

                if (i == solution.MoveCount)
                {
                    break;
                }

                Pour move = solution.GetMove(i);
                path.Add(move);
                current = PourRules.Apply(current, move.Source, move.Target, mode);
                if (current.IsWon() || PourRules.IsStuck(current, futileRule))
                {
                    break;
                }
            }

            return BreadthFirst(start, mode, futileRule, wanted);
        }

        /// <summary>Juega <paramref name="pours"/> salvo el último (<paramref name="skipLast"/>) y deja la vista quieta.</summary>
        public static void PlayPrefix(GameBootstrap boot, Pour[] pours, bool skipLast = true)
        {
            int n = skipLast ? pours.Length - 1 : pours.Length;
            for (int i = 0; i < n; i++)
            {
                TapResult poured = Tap(boot, pours[i]);
                Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), "vertido previo " + i + " (" + pours[i].Source + " → " + pours[i].Target + ")");
                StepToIdle(boot, 6000);
            }

            boot.View!.Animator.Steps.ClearLog();
        }

        /// <summary>Toca el origen y el destino de <paramref name="pour"/> (el segundo toque vierte).</summary>
        public static TapResult Tap(GameBootstrap boot, Pour pour)
        {
            TapResult selected = boot.TapCuia(pour.Source);
            Assert.That(selected.Kind, Is.EqualTo(TapKind.Selected), "selección de " + pour.Source);
            return boot.TapCuia(pour.Target);
        }

        /// <summary>
        /// Juega <paramref name="pour"/> como <see cref="Tap"/>, comprueba que vierte y devuelve el <see cref="PourVisual"/> de
        /// ese vertido (<see cref="VisualOf"/>, con <c>Session.Current</c> antes y después del toque), para
        /// <see cref="AssertLogFollowsPlan"/>.
        /// </summary>
        public static PourVisual TapPour(GameBootstrap boot, Pour pour, out TapResult poured)
        {
            BoardState before = boot.Flow!.Session.Current;
            poured = Tap(boot, pour);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), "vertido " + pour.Source + " → " + pour.Target);
            return VisualOf(before, poured, boot.Flow.Session.Current);
        }

        /// <summary>
        /// El <see cref="PourVisual"/> que la pantalla construye para un vertido del juego (<c>GameScreen.EnqueuePour</c>):
        /// <paramref name="before"/> es <c>Session.Current</c> antes del toque, <paramref name="poured"/> el toque que vierte
        /// (su <c>Pour</c> con los <c>Events</c> de R10) y <paramref name="after"/>, <c>Session.Current</c> después.
        /// </summary>
        public static PourVisual VisualOf(BoardState before, TapResult poured, BoardState after)
        {
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), "el toque vierte");
            return new PourVisual(poured.Pour.Pour, poured.Pour.Events, before, after);
        }

        /// <summary>Avanza hasta la vista quieta y devuelve los ms (falla pasado <paramref name="maxMs"/>).</summary>
        public static int StepToIdle(GameBootstrap boot, int maxMs)
        {
            return PlayModeHelpers.StepUntilIdle(boot, maxMs);
        }

        /// <summary>
        /// Avanza de fotograma en fotograma hasta que haya un paso <paramref name="kind"/> en curso (con dueña
        /// <paramref name="index"/>, o cualquiera con −1) y devuelve los ms avanzados; falla pasado <paramref name="maxMs"/>.
        /// </summary>
        public static int StepUntilRunning(GameBootstrap boot, MechanicStepKind kind, int index, int maxMs)
        {
            MechanicStepQueue steps = boot.View!.Animator.Steps;
            int t = 0;
            while (!steps.IsRunning(kind, index))
            {
                if (t > maxMs || boot.View.IsViewIdle)
                {
                    Assert.Fail("No llegó el paso " + kind + " en la cuia " + index + " (" + t + " ms). " + Describe(boot.View.Animator));
                }

                PlayModeHelpers.TickExact(boot, 4);
                t += 4;
            }

            return t;
        }

        /// <summary>Con el paso <paramref name="kind"/> en curso, avanza hasta la fracción <paramref name="fraction"/> de su duración.</summary>
        public static void StepToFraction(GameBootstrap boot, MechanicStepKind kind, int index, float fraction)
        {
            MechanicStepQueue steps = boot.View!.Animator.Steps;
            Assert.That(steps.TryGetRunning(kind, index, out float elapsed, out float duration), Is.True, kind + " en curso");
            int left = Mathf.FloorToInt((duration * fraction) - elapsed);
            if (left > 0)
            {
                PlayModeHelpers.TickExact(boot, left);
            }
        }

        /// <summary>Último registro de un paso <paramref name="kind"/> con dueña <paramref name="index"/> (−1: cualquiera).</summary>
        public static bool TryFindRecord(BoardAnimator animator, MechanicStepKind kind, int index, out MechanicStepRecord record)
        {
            MechanicStepQueue steps = animator.Steps;
            for (int i = steps.LogCount - 1; i >= 0; i--)
            {
                MechanicStepRecord r = steps.LogAt(i);
                if (r.Kind == kind && (index < 0 || r.Index == index))
                {
                    record = r;
                    return true;
                }
            }

            record = default;
            return false;
        }

        /// <summary>Registro obligatorio (falla con el registro entero si no está).</summary>
        public static MechanicStepRecord Record(BoardAnimator animator, MechanicStepKind kind, int index)
        {
            Assert.That(TryFindRecord(animator, kind, index, out MechanicStepRecord r), Is.True, "paso " + kind + " en " + index + ". " + Describe(animator));
            return r;
        }

        /// <summary>Registro de pasos en una línea (mensajes de error).</summary>
        public static string Describe(BoardAnimator animator)
        {
            MechanicStepQueue steps = animator.Steps;
            var sb = new System.Text.StringBuilder("Pasos:");
            for (int i = 0; i < steps.LogCount; i++)
            {
                sb.Append(" [").Append(steps.LogAt(i).ToString()).Append(']');
            }

            return sb.ToString();
        }

        // Rectángulo interior del panel (dentro del marco) en el espacio de las capas de tablero, desde el layout del tablero:
        // el layout va en dp desde arriba y las capas, hacia arriba, desplazadas por offset.
        public static Rect PanelInner(BoardLayoutResult l, float frame, Vector2 offset)
        {
            float xMin = l.PanelX + frame + offset.x;
            float xMax = l.PanelX + l.PanelWidth - frame + offset.x;
            float yMax = -(l.PanelY + frame) + offset.y;
            float yMin = -(l.PanelY + l.PanelHeight - frame) + offset.y;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        // Rectángulo interior del panel de la pantalla de juego (con el marco tal como se dibuja), en el espacio de las capas de
        // tablero.
        public static Rect PanelInner(GameScreen view)
        {
            BoardLayoutResult l = view.Layout!;
            return PanelInner(l, view.PanelFrameDp, LayoutOffset(view.Animator, l, view.Views.Length));
        }

        // Traslación del layout (pie en (CenterX, −FootY)) a las capas de tablero, igual para la primera y la última cuia.
        public static Vector2 LayoutOffset(BoardAnimator board, BoardLayoutResult l, int count)
        {
            Vector2 first = board.Foot(0) - new Vector2(l.CenterX[0], -l.FootY[0]);
            Vector2 last = board.Foot(count - 1) - new Vector2(l.CenterX[count - 1], -l.FootY[count - 1]);
            Assert.That(Vector2.Distance(first, last), Is.LessThan(0.5f), "el layout y las capas difieren solo en una traslación");
            return first;
        }

        /// <summary>Con la vista quieta, cada cuia muestra el modelo (contenido, tapa y nivel de líquido).</summary>
        public static void AssertViewMatchesModel(GameBootstrap boot)
        {
            BoardState model = boot.Flow!.Session.Current;
            GameScreen view = boot.View!;
            Assert.That(view.IsViewIdle, Is.True, "la vista está quieta");
            for (int i = 0; i < model.Count; i++)
            {
                Assert.That(view.Animator.ShownOf(i), Is.EqualTo(model[i]), "cuia " + i + ": ShownCuia == Session.Current");
                Assert.That(view.Views[i].ShownCuia, Is.EqualTo(model[i]), "cuia " + i + ": la vista pinta el modelo");
                Assert.That(view.Views[i].Sealed, Is.EqualTo(model.IsSealed(i)), "tapa de la cuia " + i);
                Assert.That(view.Views[i].LiquidLevel, Is.EqualTo((float)model[i].Count).Within(1e-3f), "nivel de la cuia " + i);
                CuiaOverlay? overlay = view.Views[i].Overlay;
                if (overlay != null)
                {
                    Assert.That(overlay.IsAnimating, Is.False, "cuia " + i + ": ningún overlay animando");
                    Assert.That(overlay.Visible, Is.EqualTo(model[i].Mechanic == overlay.Type), "cuia " + i + ": overlay como el modelo");
                }
            }
        }

        /// <summary>
        /// El registro de pasos de <paramref name="animator"/> es la ejecución del plan de <paramref name="visual"/>
        /// (<see cref="MechanicStepPlan.Build"/>, arquitectura del paso 1 §4). Se llama con el registro vaciado antes del toque
        /// (<see cref="LoadPlaying"/> y <see cref="PlayPrefix"/> lo vacían) y el plan ya terminado, sin <c>SnapAll</c>:
        /// <list type="bullet">
        /// <item><description>el registro tiene exactamente los pasos del plan (Kind, Stage, Index y OtherIndex), ni uno de
        /// más ni de menos;</description></item>
        /// <item><description>en el orden de las etapas: ningún paso empieza antes que uno de una etapa anterior;</description></item>
        /// <item><description>cada etapa empieza cuando termina el paso más largo de la anterior (StartMs ≥ el mayor EndMs de
        /// la etapa previa con pasos), y sus pasos van a la vez, justo entonces, salvo los que pintan el origen o lo revelan,
        /// que pueden empezar más tarde, cuando vuelve a su ranura (SrcDone).</description></item>
        /// </list>
        /// Devuelve el número de pasos del plan.
        /// </summary>
        public static int AssertLogFollowsPlan(BoardAnimator animator, in PourVisual visual)
        {
            MechanicStepQueue steps = animator.Steps;
            var plan = new StepEntry[MechanicStepPlan.MaxSteps];
            int count = MechanicStepPlan.Build(in visual, plan);
            var sb = new System.Text.StringBuilder(" Plan:");
            for (int e = 0; e < count; e++)
            {
                sb.Append(" [").Append(plan[e].ToString()).Append(']');
            }

            string context = sb.Append(". ").Append(Describe(animator)).ToString();
            Assert.That(steps.ActiveCount, Is.EqualTo(0), "el plan del vertido terminó." + context);
            Assert.That(steps.LogCount, Is.EqualTo(count), "el registro tiene los pasos del plan, ni más ni menos." + context);

            var matched = new bool[count];
            int stage = -1;
            float stageStart = float.NaN; // inicio común de los pasos de la etapa que no esperan al origen
            float stageEnd = float.NaN;   // fin del paso más largo de la etapa en curso
            float previousEnd = float.NaN; // fin del paso más largo de la etapa anterior con pasos
            for (int i = 0; i < count; i++)
            {
                MechanicStepRecord r = steps.LogAt(i);
                string what = "registro " + i + " (" + r + ")";
                int at = -1;
                for (int e = 0; e < count && at < 0; e++)
                {
                    if (!matched[e] && plan[e].Kind == r.Kind && plan[e].Stage == r.Stage && plan[e].Index == r.Index && plan[e].OtherIndex == r.OtherIndex)
                    {
                        at = e;
                    }
                }

                Assert.That(at, Is.GreaterThanOrEqualTo(0), what + ": es un paso del plan (Kind, Stage, Index y OtherIndex)." + context);
                matched[at] = true;
                Assert.That((int)r.Stage, Is.GreaterThanOrEqualTo(stage), what + ": en el orden de las etapas." + context);
                if ((int)r.Stage != stage)
                {
                    stage = (int)r.Stage;
                    previousEnd = stageEnd;
                    stageStart = float.NaN;
                    stageEnd = float.NaN;
                }

                bool waits = r.Kind == MechanicStepKind.Reveal || r.Index == visual.Source || r.OtherIndex == visual.Source;
                if (!float.IsNaN(previousEnd))
                {
                    Assert.That(r.StartMs, Is.GreaterThanOrEqualTo(previousEnd - LogToleranceMs), what + ": empieza cuando termina la etapa anterior." + context);
                    if (!waits)
                    {
                        Assert.That(r.StartMs, Is.EqualTo(previousEnd).Within(LogToleranceMs), what + ": empieza justo al terminar el paso más largo de la etapa anterior." + context);
                    }
                }

                if (!waits)
                {
                    if (float.IsNaN(stageStart))
                    {
                        stageStart = r.StartMs;
                    }

                    Assert.That(r.StartMs, Is.EqualTo(stageStart).Within(LogToleranceMs), what + ": los pasos de una etapa van a la vez." + context);
                }

                Assert.That(r.EndMs, Is.GreaterThanOrEqualTo(r.StartMs), what + ": termina después de empezar." + context);
                stageEnd = float.IsNaN(stageEnd) ? r.EndMs : Math.Max(stageEnd, r.EndMs);
            }

            return count;
        }

        /// <summary>
        /// Criterio de conjunto de los estrenos de la ola 3 (prueba del 2026-10-09): en todo el registro de pasos de
        /// <paramref name="animator"/>, salvo los terminados antes de tiempo (<c>Snapped</c>),
        /// <list type="bullet">
        /// <item><description>ningún Open, Release, Receive, RainDrop, RainEnd, Bite ni MudWash usa el paso por defecto de la
        /// cola ni dura 0 ms;</description></item>
        /// <item><description>cada paso dura su nominal (<see cref="NominalStepMs"/>, la constante de quien lo anima) × su factor
        /// de velocidad;</description></item>
        /// <item><description>y esa nominal cae en el rango que fijan los documentos (<see cref="NominalRangeOf"/>, propuesta de
        /// D-007 b.2, 02 §4.1 y los [P] de la ola 3), escrito con sus números y no con las constantes: un cambio de constante
        /// fuera de rango hace fallar la prueba aunque el overlay anime lo que dice su constante.</description></item>
        /// </list>
        /// Devuelve cuántos pasos de la primera lista comprobó.
        /// </summary>
        public static int AssertOwnTimedSteps(BoardAnimator animator, string what)
        {
            MechanicStepQueue steps = animator.Steps;
            int strict = 0;
            for (int i = 0; i < steps.LogCount; i++)
            {
                MechanicStepRecord r = steps.LogAt(i);
                if (r.Snapped)
                {
                    continue;
                }

                string label = what + ": " + r + ". " + Describe(animator);
                float ms = r.EndMs - r.StartMs;
                switch (r.Kind)
                {
                    case MechanicStepKind.Open:
                    case MechanicStepKind.Release:
                    case MechanicStepKind.Receive:
                    case MechanicStepKind.RainDrop:
                    case MechanicStepKind.RainEnd:
                    case MechanicStepKind.Bite:
                    case MechanicStepKind.MudWash:
                        strict++;
                        Assert.That(r.UsedDefault, Is.False, label + ": lo anima su overlay o sus marcas, no el paso por defecto");
                        Assert.That(ms, Is.GreaterThan(0f), label + ": dura más de 0 ms");
                        break;
                }

                float nominal = NominalStepMs(r);
                Assert.That(nominal, Is.GreaterThan(0f), label + ": paso con duración nominal conocida");
                Assert.That(ms, Is.EqualTo(nominal * r.SpeedFactor).Within(0.5f), label + ": " + nominal + " ms × factor " + r.SpeedFactor.ToString("0.###"));
                AssertNominalInRange(r, nominal, label);
            }

            return strict;
        }

        /// <summary>
        /// Tope (ms) de un vertido, del toque a la vista quieta, según la hoja de la ola 3 («ningún vertido &gt; 2.800 ms»):
        /// ningún paso puede durar más por sí solo. Va escrito aquí, no leído de la cola, para no comparar la cola consigo misma.
        /// </summary>
        public const float PourCapMs = 2800f;

        /// <summary>
        /// Holgura de «≈ x ms» en los rangos de <see cref="NominalRangeOf"/>: x ± 20 %, la que b.2 da a sus rangos explícitos
        /// (Hoja 400-600 = 500 ± 20 %; Liana 600-900 = 750 ± 20 %). Es una tolerancia de la prueba, no una regla del juego.
        /// </summary>
        public const float AboutTolerance = 0.2f;

        /// <summary>
        /// Rango documentado de la duración nominal (ms, sin factor) del paso registrado <paramref name="r"/>, con los números de
        /// las fuentes y no con las constantes de los overlays:
        /// <list type="bullet">
        /// <item><description>Reveal (turbias): &gt; 300 y &lt; 1.500 (b.2 y hoja de la ola 3);</description></item>
        /// <item><description>MudWash (Barro): ≈ 400 (b.2 M5);</description></item>
        /// <item><description>Receive del Remolino: ≈ 500 (b.2 M4);</description></item>
        /// <item><description>Open de la Hoja: 400-600 (b.2 M1); del Brote: ≥ 350 (hoja de la ola 3; 02 §4.1: 350) y como
        /// mucho los 1.500 de los desbloqueos de la propuesta (§g, sobre 02 §4.1: «la propuesta usa 400-1.500 ms»);</description></item>
        /// <item><description>Seal del Matapi: ≈ 800 (b.2 M10); el sellado por defecto: 450 (02 §4.1, «450 ms total»);</description></item>
        /// <item><description>Release de la Liana: 600-900 (b.2 M3);</description></item>
        /// <item><description>RainDrop y RainEnd de la Lluvia: ≈ 500 [P] (arquitectura del paso 1: «gota de Lluvia 500 ms»), que
        /// también deja la gota más larga que un llenado (280 ms de 08 §9);</description></item>
        /// <item><description>Bite de la Piraña: ≈ 500 (b.2 M7); BiteLost: ≈ 400 [P] (hoja de la ola 3).</description></item>
        /// </list>
        /// «≈ x» es x ± <see cref="AboutTolerance"/>. RainWait (400 ms [P] de LluviaOverlay) no tiene número en ningún documento:
        /// devuelve un rango sin fuente (<see cref="NominalRange.Documented"/> = false) y solo cumple <see cref="PourCapMs"/>.
        /// </summary>
        public static NominalRange NominalRangeOf(MechanicStepRecord r)
        {
            switch (r.Kind)
            {
                case MechanicStepKind.Reveal:
                    return new NominalRange(300f, true, 1500f, true, "turbias: > 300 ms y < 1.500 ms (b.2; hoja de la ola 3)");
                case MechanicStepKind.MudWash:
                    return NominalRange.About(400f, "Barro (b.2 M5)");
                case MechanicStepKind.Receive:
                    return r.Owner == MechanicType.Remolino ? NominalRange.About(500f, "Remolino (b.2 M4)") : default;
                case MechanicStepKind.Open:
                    if (r.Owner == MechanicType.Hoja)
                    {
                        return new NominalRange(400f, false, 600f, false, "Hoja: 400-600 ms (b.2 M1)");
                    }

                    return r.Owner == MechanicType.Brote
                        ? new NominalRange(350f, false, 1500f, false, "Brote: ≥ 350 ms (hoja de la ola 3; 02 §4.1) y ≤ 1.500 ms (desbloqueos de la propuesta, §g)")
                        : default;
                case MechanicStepKind.Seal:
                    if (r.Owner == MechanicType.Matapi)
                    {
                        return NominalRange.About(800f, "Matapi (b.2 M10)");
                    }

                    return r.UsedDefault ? new NominalRange(450f, false, 450f, false, "sellado: 450 ms en total (02 §4.1)") : default;
                case MechanicStepKind.Release:
                    return r.Owner == MechanicType.Liana ? new NominalRange(600f, false, 900f, false, "Liana: 600-900 ms (b.2 M3)") : default;
                case MechanicStepKind.RainDrop:
                case MechanicStepKind.RainEnd:
                    return r.Owner == MechanicType.Lluvia ? NominalRange.About(500f, "gota de Lluvia, 500 ms [P] (arquitectura del paso 1)") : default;
                case MechanicStepKind.Bite:
                    return r.Owner == MechanicType.Pirana ? NominalRange.About(500f, "Piraña (b.2 M7)") : default;
                case MechanicStepKind.BiteLost:
                    return r.Owner == MechanicType.Pirana ? NominalRange.About(400f, "mordisco perdido, 400 ms [P] (hoja de la ola 3)") : default;
                default:
                    return default;
            }
        }

        /// <summary>
        /// La duración nominal <paramref name="nominal"/> del paso <paramref name="r"/> cae en su rango documentado
        /// (<see cref="NominalRangeOf"/>, si lo tiene) y, en todo caso, por debajo de <see cref="PourCapMs"/>.
        /// </summary>
        public static void AssertNominalInRange(MechanicStepRecord r, float nominal, string label)
        {
            Assert.That(nominal, Is.LessThan(PourCapMs), label + ": un paso dura menos que el vertido entero (2.800 ms)");
            NominalRange range = NominalRangeOf(r);
            if (!range.Documented)
            {
                return;
            }

            string what = label + ": " + nominal + " ms en " + range;
            if (range.MinOpen)
            {
                Assert.That(nominal, Is.GreaterThan(range.Min), what);
            }
            else
            {
                Assert.That(nominal, Is.GreaterThanOrEqualTo(range.Min), what);
            }

            if (range.MaxOpen)
            {
                Assert.That(nominal, Is.LessThan(range.Max), what);
            }
            else
            {
                Assert.That(nominal, Is.LessThanOrEqualTo(range.Max), what);
            }
        }

        /// <summary>
        /// Duración nominal (ms, sin factor) del paso registrado <paramref name="r"/> según quién lo animó: la del overlay de su
        /// mecánica, la de las marcas (revelado y lavado del barro) o, en el sellado sin overlay, la tapa por defecto; −1 si no
        /// tiene una propia.
        /// </summary>
        public static float NominalStepMs(MechanicStepRecord r)
        {
            switch (r.Kind)
            {
                case MechanicStepKind.Reveal:
                    return MurkyMarks.RevealRuleMs;
                case MechanicStepKind.MudWash:
                    return MudMarks.WashMs;
                case MechanicStepKind.Receive:
                    return r.Owner == MechanicType.Remolino ? RemolinoOverlay.ReceiveMs : -1f;
                case MechanicStepKind.Open:
                    return r.Owner == MechanicType.Hoja ? HojaOverlay.OpenStepMs : r.Owner == MechanicType.Brote ? BroteOverlay.OpenStepMs : -1f;
                case MechanicStepKind.Seal:
                    return r.Owner == MechanicType.Matapi ? MatapiOverlay.SealMs : r.UsedDefault ? CuiaView.SealMs : -1f;
                case MechanicStepKind.Release:
                    return r.Owner == MechanicType.Liana ? LianaOverlay.ReleaseMs : -1f;
                case MechanicStepKind.RainDrop:
                case MechanicStepKind.RainEnd:
                    return r.Owner == MechanicType.Lluvia ? LluviaOverlay.DropStepMs : -1f;
                case MechanicStepKind.RainWait:
                    return r.Owner == MechanicType.Lluvia ? LluviaOverlay.WaitStepMs : -1f;
                case MechanicStepKind.Bite:
                    return r.Owner == MechanicType.Pirana ? PiranaOverlay.BiteStepMs : -1f;
                case MechanicStepKind.BiteLost:
                    return r.Owner == MechanicType.Pirana ? PiranaOverlay.BiteLostStepMs : -1f;
                default:
                    return -1f;
            }
        }

        private static bool TryOneMore(BoardState state, PourMode mode, PourWanted wanted, out Pour hit)
        {
            for (int a = 0; a < state.Count; a++)
            {
                for (int b = 0; b < state.Count; b++)
                {
                    if (a == b || !PourRules.IsLegal(state, a, b))
                    {
                        continue;
                    }

                    BoardState next = PourRules.Apply(state, a, b, mode, out PourEvents ev);
                    if (wanted(ev, state, next))
                    {
                        hit = new Pour(a, b, ev.Amount);
                        return true;
                    }
                }
            }

            hit = default;
            return false;
        }

        private static Pour[]? BreadthFirst(BoardState start, PourMode mode, bool futileRule, PourWanted wanted)
        {
            var parents = new Dictionary<BoardState, (BoardState? Parent, Pour Move)> { { start, (null, default) } };
            var queue = new Queue<BoardState>();
            queue.Enqueue(start);
            while (queue.Count > 0 && parents.Count < SearchStates)
            {
                BoardState current = queue.Dequeue();
                for (int a = 0; a < current.Count; a++)
                {
                    for (int b = 0; b < current.Count; b++)
                    {
                        if (a == b || !PourRules.IsLegal(current, a, b))
                        {
                            continue;
                        }

                        BoardState next = PourRules.Apply(current, a, b, mode, out PourEvents ev);
                        if (wanted(ev, current, next))
                        {
                            var path = new List<Pour> { new Pour(a, b, ev.Amount) };
                            for (BoardState? at = current; at != null && parents[at].Parent != null; at = parents[at].Parent)
                            {
                                path.Insert(0, parents[at].Move);
                            }

                            return path.ToArray();
                        }

                        if (parents.ContainsKey(next) || next.IsWon() || PourRules.IsStuck(next, futileRule))
                        {
                            continue;
                        }

                        parents.Add(next, (current, new Pour(a, b, ev.Amount)));
                        queue.Enqueue(next);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Rango de una duración nominal (ms) con su fuente (<see cref="NominalRangeOf"/>); cada extremo, abierto o cerrado.
        /// El valor por defecto no tiene fuente (<see cref="Documented"/> = false).
        /// </summary>
        public readonly struct NominalRange
        {
            /// <summary>Crea el rango de <paramref name="min"/> a <paramref name="max"/> con su fuente.</summary>
            public NominalRange(float min, bool minOpen, float max, bool maxOpen, string source)
            {
                Min = min;
                MinOpen = minOpen;
                Max = max;
                MaxOpen = maxOpen;
                Source = source;
            }

            /// <summary>Extremo inferior (ms).</summary>
            public float Min { get; }

            /// <summary>El extremo inferior queda fuera (&gt; en vez de ≥).</summary>
            public bool MinOpen { get; }

            /// <summary>Extremo superior (ms).</summary>
            public float Max { get; }

            /// <summary>El extremo superior queda fuera (&lt; en vez de ≤).</summary>
            public bool MaxOpen { get; }

            /// <summary>De dónde sale el rango; null si ningún documento lo fija.</summary>
            public string? Source { get; }

            /// <summary>Algún documento fija el rango.</summary>
            public bool Documented => Source != null;

            /// <summary>«≈ <paramref name="ms"/> ms»: <paramref name="ms"/> ± <see cref="AboutTolerance"/>, extremos incluidos.</summary>
            public static NominalRange About(float ms, string source)
            {
                return new NominalRange(ms * (1f - AboutTolerance), false, ms * (1f + AboutTolerance), false, "≈ " + ms + " ms ± " + (AboutTolerance * 100f).ToString("0") + " %, " + source);
            }

            /// <summary>El rango en una línea (mensajes de error).</summary>
            public override string ToString()
            {
                return Documented
                    ? (MinOpen ? "(" : "[") + Min.ToString("0.#") + ", " + Max.ToString("0.#") + (MaxOpen ? ")" : "]") + " ms: " + Source
                    : "sin rango documentado (solo el tope de 2.800 ms)";
            }
        }

        /// <summary>
        /// Banco sintético: un Canvas con el tablero, las capas de mecánica, los efectos y una vista por cuia, como
        /// <c>GameScreen</c>, y una <see cref="BoardAnimator"/> con el reloj en manos del test. Tableros con
        /// <see cref="BoardBuilder"/>; los vertidos, con <see cref="Pour"/>.
        /// </summary>
        public sealed class Rig : IDisposable
        {
            /// <summary>Duraciones de los defaults de 08 §9 (factor 1).</summary>
            public static readonly PourTimings Timings = new PourTimings(100f, 300f, 280f, 80f, 300f);

            private readonly GameObject _root;
            private bool _disposed;

            /// <summary>Crea el banco con <paramref name="board"/> pintado; <paramref name="beforeBind"/> recibe las vistas aún sin tablero.</summary>
            public Rig(ProceduralArt art, BoardState board, Action<CuiaView[]>? beforeBind = null)
            {
                _root = new GameObject("MechanicRig", typeof(RectTransform), typeof(Canvas));
                var canvas = _root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2
                    | AdditionalCanvasShaderChannels.TexCoord3;
                var rootRt = (RectTransform)_root.transform;
                RectTransform boardRt = NewLayer("Board", rootRt);
                BackLayer = NewLayer("MechanicBack", boardRt);
                Fx = new FxLayer(boardRt, art);
                Views = new CuiaView[board.Count];
                for (int i = 0; i < board.Count; i++)
                {
                    Views[i] = CuiaView.Create(boardRt, i, board[i].Capacity, art, index => { }, (index, inside) => { }, Fx);
                    Views[i].SetGeometry(new Vector2(36f + (i * 64f), -600f), 40f, 60f);
                    Views[i].SetLiftDuration(100f);
                }

                FrontLayer = NewLayer("MechanicFront", boardRt);
                RectTransform streams = NewLayer("Streams", boardRt);
                Fx.Root.SetAsLastSibling();
                RectTransform pourFront = NewLayer("PourFront", rootRt);
                Animator = new BoardAnimator(streams, pourFront) { Fx = Fx, BoardWidth = 400f };
                beforeBind?.Invoke(Views);
                Animator.Bind(Views, Timings, 2, BackLayer, FrontLayer);
                for (int i = 0; i < board.Count; i++)
                {
                    Animator.ShowContent(i, board[i], board.IsSealed(i));
                }

                Animator.BoardLaidOut();
            }

            /// <summary>Cola del banco.</summary>
            public BoardAnimator Animator { get; }

            /// <summary>Vistas de las cuias.</summary>
            public CuiaView[] Views { get; }

            /// <summary>Efectos.</summary>
            public FxLayer Fx { get; }

            /// <summary>Capa de mecánicas trasera.</summary>
            public RectTransform BackLayer { get; }

            /// <summary>Capa de mecánicas delantera.</summary>
            public RectTransform FrontLayer { get; }

            /// <summary>Encola el vertido <paramref name="source"/> → <paramref name="target"/> del tablero <paramref name="before"/> y devuelve el tablero de después.</summary>
            public BoardState Pour(BoardState before, int source, int target, out PourVisual visual)
            {
                Assert.That(PourRules.IsLegal(before, source, target), Is.True, source + " → " + target);
                BoardState after = PourRules.Apply(before, source, target, PourMode.Run, out PourEvents ev);
                visual = new PourVisual(new Pour(source, target, ev.Amount), ev, before, after);
                Animator.EnqueuePour(in visual);
                return after;
            }

            /// <summary>Un fotograma: la cola, las vistas y los efectos.</summary>
            public void Tick(float dtMs)
            {
                Animator.Tick(dtMs);
                for (int i = 0; i < Views.Length; i++)
                {
                    Views[i].Tick(dtMs);
                }

                Fx.Tick(dtMs);
            }

            /// <summary>Avanza <paramref name="ms"/> ms en fotogramas de 16 ms.</summary>
            public void Run(int ms)
            {
                for (int t = 0; t < ms; t += PlayModeHelpers.FrameMs)
                {
                    Tick(PlayModeHelpers.FrameMs);
                }
            }

            /// <summary>Avanza hasta la cola quieta (como mucho <paramref name="maxMs"/>) y devuelve los ms.</summary>
            public int RunUntilIdle(int maxMs)
            {
                int t = 0;
                while (!Animator.IsIdle && t < maxMs)
                {
                    Tick(PlayModeHelpers.FrameMs);
                    t += PlayModeHelpers.FrameMs;
                }

                return t;
            }

            /// <summary>
            /// Destruye el banco en el acto (<c>DestroyImmediate</c>), no al final del fotograma: las pruebas sin asignaciones
            /// calientan en un banco y miden en otro nuevo dentro del mismo fotograma, y con <c>Destroy</c> los gráficos de los
            /// bancos desechados seguirían activos y registrados en las colecciones globales de UGUI (el
            /// <c>GraphicRebuildTracker</c> del Editor, la cola de <c>CanvasUpdateRegistry</c> y <c>GraphicRegistry</c>) hasta el
            /// final del fotograma. El banco medido llegaría entonces a más gráficos vivos que el de calentamiento y esas
            /// colecciones podrían crecer en mitad de la medida, según cuántas piezas tengan los overlays (T21). En el juego, el
            /// tablero viejo se va en el fotograma del cambio de nivel, antes del primer vertido.
            /// </summary>
            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Object.DestroyImmediate(_root);
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

        /// <summary>
        /// Overlay de prueba: dibuja una pieza en <c>BodyFront</c>, apunta lo que el anfitrión traía al unirse
        /// (<see cref="HostWiredAtAttach"/>), cuenta los avisos de recolocación y los pasos, y anima cada paso el tiempo de
        /// <see cref="Durations"/> (0 = paso por defecto de la cola).
        /// </summary>
        public sealed class ProbeOverlay : CuiaOverlay
        {
            private readonly MechanicType _type;
            private Image? _piece;
            private float _left = -1f;

            /// <summary>Crea el overlay de la mecánica <paramref name="type"/>.</summary>
            public ProbeOverlay(MechanicType type)
            {
                _type = type;
            }

            /// <summary>Duración nominal de cada paso (ms; índice = <see cref="MechanicStepKind"/>).</summary>
            public float[] Durations { get; } = new float[16];

            /// <summary>Capas y tablero no nulos al llamar a <c>OnAttach</c>.</summary>
            public bool HostWiredAtAttach { get; private set; }

            /// <summary>Veces que se unió.</summary>
            public int Attaches { get; private set; }

            /// <summary>Avisos de recolocación del tablero.</summary>
            public int BoardLayouts { get; private set; }

            /// <summary>Pasos recibidos.</summary>
            public List<MechanicStepContext> Steps { get; } = new List<MechanicStepContext>();

            /// <summary>Veces que se le pidió terminar el paso en el acto.</summary>
            public int Snaps { get; private set; }

            /// <summary>Último Show con la mecánica a la vista.</summary>
            public int Shows { get; private set; }

            /// <inheritdoc />
            public override MechanicType Type => _type;

            /// <inheritdoc />
            public override bool IsAnimating => _left >= 0f;

            /// <summary>La pieza del overlay está a la vista.</summary>
            public bool PieceVisible => _piece != null && _piece.gameObject.activeSelf;

            /// <inheritdoc />
            public override void Show(Cuia cuia, bool sealedNow)
            {
                Shows++;
                Visible = cuia.Mechanic == _type;
                if (_piece != null)
                {
                    _piece.gameObject.SetActive(Visible);
                }
            }

            /// <inheritdoc />
            public override void OnBoardLayout()
            {
                BoardLayouts++;
            }

            /// <inheritdoc />
            public override float StepDurationMs(MechanicStepKind kind)
            {
                return Durations[(int)kind];
            }

            /// <inheritdoc />
            public override float PlayStep(in MechanicStepContext ctx)
            {
                Steps.Add(ctx);
                float ms = StepDurationMs(ctx.Kind) * ctx.SpeedFactor;
                if (ms > 0f)
                {
                    _left = ms;
                }

                return ms;
            }

            /// <inheritdoc />
            public override void Tick(float dtMs)
            {
                if (_left >= 0f)
                {
                    _left -= dtMs;
                    if (_left <= 0f)
                    {
                        _left = 0f; // la cola cierra con SnapStep
                    }
                }
            }

            /// <inheritdoc />
            public override void SnapStep()
            {
                Snaps++;
                _left = -1f;
            }

            /// <inheritdoc />
            protected override void OnAttach()
            {
                Attaches++;
                HostWiredAtAttach = Host.Board != null && Host.BoardBackLayer != null && Host.BoardFrontLayer != null;
                _piece = OverlayParts.NewImage("ProbePiece", Host.BodyFront, Host.Art.Ring, Color.white);
                _piece.gameObject.SetActive(false);
            }
        }
    }
}
