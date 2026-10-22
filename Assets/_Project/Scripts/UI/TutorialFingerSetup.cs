#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Solver.Search;

namespace AF.UI
{
    /// <summary>
    /// El paso previo del dedo guía del nivel 2 (tarea B11; 02 §13, fila 2: «Vertido parcial con dedo guía: una racha de 3 sobre
    /// un hueco de 2»; 06 §2.2). En el tablero inicial del 2 la lección no es legal (las cuias con fruta están llenas y las dos
    /// vacías no tienen hueco de 2 con fruta debajo), así que el dedo no salía hasta que el jugador, por su cuenta, hacía el vertido
    /// que la dejaba posible (prueba en el teléfono del 2026-10-20, observación 3). Con esta regla [P] (pregunta Q-B11-1), el dedo
    /// señala antes ese vertido: el primero que deja la lección legal y el tablero con solución (<see cref="TryChoose"/>); hecho,
    /// señala la lección. Como el resto del dedo guía, es una ayuda explícita del tutorial (D-028 con P5; compatible con D-002).
    /// Sin motor de Unity; el solver solo corre cuando cambia el tablero (lo llama <c>GameScreen.TickTutorialFinger</c>).
    /// </summary>
    internal static class TutorialFingerSetup
    {
        /// <summary>
        /// El vertido previo de la lección del 2 sobre <paramref name="state"/>: si la lección ya es legal, o si el tablero ya es
        /// victoria, false. Si no, primero el de la solución del solver si deja la lección posible; si no, el primero en orden de
        /// origen y destino que la deje posible con solución (<see cref="TutorialFinger.TryChoose"/> de
        /// <see cref="FingerLesson.PartialRun"/> sobre el tablero siguiente, que ya mira que tras la lección siga habiendo
        /// solución). False si no hay ninguno.
        /// </summary>
        public static bool TryChoose(BoardState state, PourMode mode, bool futileRule, AStarSolver solver, out Pour pour)
        {
            pour = default;
            if (state == null || LessonLegal(state, mode))
            {
                return false; // con la lección ya legal no hay paso previo (la señala TutorialFinger.TryChoose)
            }

            return TryChooseWhileIllegal(state, mode, futileRule, solver, out pour);
        }

        /// <summary>
        /// Como <see cref="TryChoose"/>, cuando quien llama ya comprobó que la lección no es legal (<see cref="LessonLegal"/> false):
        /// así <c>GameScreen.TickTutorialFinger</c> no la comprueba dos veces (sugerencia de la revisión del Spark de B11).
        /// </summary>
        internal static bool TryChooseWhileIllegal(BoardState state, PourMode mode, bool futileRule, AStarSolver solver, out Pour pour)
        {
            pour = default;
            if (state.IsWon())
            {
                return false;
            }

            SolverOptions options = SolverOptions.ForPipeline(mode).WithFutileRule(futileRule).ForLevel(state);
            SolveResult solved = solver.Solve(state, options);
            if (solved.Status == SolveStatus.Solved && solved.MoveCount > 0)
            {
                Pour first = solved.GetMove(0);
                if (OpensLesson(state, first.Source, first.Target, mode, futileRule, solver))
                {
                    pour = first;
                    return true;
                }
            }

            for (int s = 0; s < state.Count; s++)
            {
                for (int t = 0; t < state.Count; t++)
                {
                    if (s != t && OpensLesson(state, s, t, mode, futileRule, solver))
                    {
                        pour = new Pour(s, t, PourRules.Amount(state, s, t, mode));
                        return true;
                    }
                }
            }

            return false;
        }

        // El vertido de s a t deja, en el tablero siguiente, la lección legal y con solución tras ella. El solver solo corre si la
        // lección queda legal (la comprobación barata va antes).
        private static bool OpensLesson(BoardState state, int s, int t, PourMode mode, bool futileRule, AStarSolver solver)
        {
            if (!PourRules.IsLegal(state, s, t))
            {
                return false;
            }

            BoardState next = PourRules.Apply(state, s, t, mode);
            return !next.IsWon() && LessonLegal(next, mode)
                && TutorialFinger.TryChoose(FingerLesson.PartialRun, next, mode, futileRule, solver, out _);
        }

        /// <summary>
        /// Algún vertido de <paramref name="state"/> es el de la lección del 2 (<see cref="TutorialInstruction.IsPartialRunLesson"/>),
        /// sin solver: la comprobación barata antes de buscar. Sin asignaciones.
        /// </summary>
        public static bool LessonLegal(BoardState state, PourMode mode)
        {
            for (int s = 0; s < state.Count; s++)
            {
                for (int t = 0; t < state.Count; t++)
                {
                    if (TutorialInstruction.IsPartialRunLesson(state, s, t, mode))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
