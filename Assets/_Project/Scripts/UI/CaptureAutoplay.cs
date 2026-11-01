#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Solver.Search;

namespace AF.UI
{
    /// <summary>
    /// Reproduce la solución del solver en el nivel cargado con ritmo «humano» (modo de captura, D-066; spark-marketing §6 E1):
    /// espera a que el tablero acepte toques y la vista esté quieta, deja <see cref="CaptureOptions.StartMs"/>, y por cada vertido
    /// toca el origen (la cuia se eleva, como con el dedo), espera <see cref="CaptureOptions.SelectMs"/>, toca el destino y, con la
    /// vista otra vez quieta, espera <see cref="CaptureOptions.PauseMs"/> ± <see cref="CaptureOptions.JitterMs"/> (variación
    /// determinista con el <see cref="Prng"/> del núcleo y <see cref="CaptureOptions.Seed"/>; nunca <c>System.Random</c>).
    /// <para>La solución es la de <see cref="AStarSolver"/> desde el tablero del intento con las opciones del pipeline para el modo
    /// de vertido de la partida (<c>rules_pour_mode</c>, D-005) y su regla fútil: los toques pasan por <see cref="IGameActions.TapCuia"/>,
    /// así que las reglas, la animación, el sonido y el guardado son los del juego. Si un toque no hace lo previsto (una mecánica
    /// que cambió el tablero), se vuelve a resolver desde el tablero de ese momento. Se para con la victoria o cuando a la solución
    /// le quedan <see cref="CaptureOptions.StopBefore"/> vertidos («falta uno»). Cada paso escribe su línea <c>AF_CAPTURE</c>.</para>
    /// </summary>
    internal sealed class CaptureAutoplay
    {
        private const int MaxReplans = 4;

        private readonly CaptureOptions _options;
        private readonly Prng _prng;
        private AStarSolver? _solver;
        private Pour[]? _plan;
        private int _next;
        private Step _step = Step.WaitReady;
        private float _waitMs;
        private int _replans;
        private int _level = -1;

        public CaptureAutoplay(CaptureOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _prng = new Prng(unchecked((ulong)(long)options.Seed) ^ 0xC0FFEEUL);
        }

        private enum Step
        {
            WaitReady,
            Delay,
            Target,
            Settle,
            Done,
        }

        /// <summary>Terminó (victoria, parada a N del final o error).</summary>
        public bool Done => _step == Step.Done;

        /// <summary>Cómo terminó: <c>won</c>, <c>stopped</c> o <c>error</c>; null mientras sigue.</summary>
        public string? EndReason { get; private set; }

        /// <summary>Vertidos hechos.</summary>
        public int Pours { get; private set; }

        /// <summary>Vertidos que le quedaban a la solución al parar (0 con la victoria).</summary>
        public int Left { get; private set; }

        /// <summary>Largo de la primera solución (el par desde el tablero del intento), −1 antes de resolver.</summary>
        public int PlanLength { get; private set; } = -1;

        /// <summary>
        /// Avanza <paramref name="realMs"/> ms de reloj real. <paramref name="flow"/> es el flujo del nivel, <paramref name="viewIdle"/>
        /// si la cola visual está vacía y <paramref name="actions"/> recibe los toques.
        /// </summary>
        public void Tick(float realMs, LevelFlow flow, bool viewIdle, IGameActions actions)
        {
            if (_step == Step.Done || flow == null || !flow.HasLevel)
            {
                return;
            }

            if (_level >= 0 && flow.LevelNumber != _level)
            {
                Finish("error", "el nivel cambió durante la reproducción");
                return;
            }

            if (flow.Session.Status == LevelStatus.Won)
            {
                Left = 0;
                Finish("won", null);
                return;
            }

            bool ready = flow.AcceptsBoardInput && viewIdle;
            switch (_step)
            {
                case Step.WaitReady:
                    if (!ready)
                    {
                        return;
                    }

                    _level = flow.LevelNumber;
                    if (!Plan(flow))
                    {
                        return;
                    }

                    PlanLength = _plan!.Length;
                    CaptureLog.Write("autoplay_start", "\"level\":" + I(_level) + ",\"moves\":" + I(PlanLength) + ",\"stop_before\":" + I(_options.StopBefore)
                        + ",\"pour_mode\":\"" + PourModes.ToKey(flow.PourMode) + "\"");
                    _waitMs = _options.StartMs;
                    _step = Step.Delay;
                    return;

                case Step.Delay:
                    _waitMs -= realMs;
                    if (_waitMs > 0f || !ready)
                    {
                        return;
                    }

                    int left = _plan!.Length - _next;
                    if (left <= _options.StopBefore)
                    {
                        Left = left;
                        Finish("stopped", null);
                        return;
                    }

                    Pour pour = _plan[_next];
                    TapResult first = actions.TapCuia(pour.Source);
                    if (first.Kind != TapKind.Selected)
                    {
                        Replan(flow, "el origen " + I(pour.Source) + " no se pudo elegir (" + first.Kind + ")");
                        return;
                    }

                    _waitMs = _options.SelectMs;
                    _step = Step.Target;
                    return;

                case Step.Target:
                    _waitMs -= realMs;
                    if (_waitMs > 0f)
                    {
                        return;
                    }

                    Pour target = _plan![_next];
                    TapResult second = actions.TapCuia(target.Target);
                    if (second.Kind != TapKind.Poured)
                    {
                        if (flow.Selected >= 0)
                        {
                            actions.TapBackground(); // C7: sin vertido, la selección se suelta antes de volver a resolver
                        }

                        Replan(flow, "el vertido " + I(target.Source) + "→" + I(target.Target) + " no se hizo (" + second.Kind + ")");
                        return;
                    }

                    Pours++;
                    _next++;
                    CaptureLog.Write("pour", "\"level\":" + I(_level) + ",\"i\":" + I(Pours) + ",\"from\":" + I(target.Source) + ",\"to\":" + I(target.Target)
                        + ",\"left\":" + I(_plan.Length - _next));
                    _step = Step.Settle;
                    return;

                case Step.Settle:
                    if (!ready)
                    {
                        return;
                    }

                    _waitMs = Math.Max(0, _options.PauseMs + Jitter());
                    _step = Step.Delay;
                    return;
            }
        }

        /// <summary>Corta la reproducción (el nivel se fue: inicio, otra pantalla) con <c>AF_CAPTURE autoplay_end</c> <c>error</c>.</summary>
        public void Abort(string why)
        {
            if (_step != Step.Done && _level >= 0)
            {
                Finish("error", why);
            }
        }

        private int Jitter()
        {
            int j = _options.JitterMs;
            return j <= 0 ? 0 : _prng.NextInt(-j, j + 1);
        }

        private bool Plan(LevelFlow flow)
        {
            LevelSession session = flow.Session;
            BoardState state = session.Current;
            SolveResult result;
            try
            {
                _solver ??= new AStarSolver();
                SolverOptions options = SolverOptions.ForPipeline(session.PourMode, state).WithFutileRule(session.FutileRuleEnabled);
                result = _solver.Solve(state, options);
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
            {
                Finish("error", "el solver no pudo con este tablero: " + e.Message);
                return false;
            }

            if (!result.IsSolved)
            {
                Finish("error", "el solver no encontró solución (" + result.Status + ", " + I(result.Expanded) + " nodos)");
                return false;
            }

            _plan = result.CopyMoves();
            _next = 0;
            return true;
        }

        private void Replan(LevelFlow flow, string why)
        {
            _replans++;
            if (_replans > MaxReplans)
            {
                Finish("error", why);
                return;
            }

            CaptureLog.Write("autoplay_replan", "\"level\":" + I(_level) + ",\"why\":" + CaptureLog.Quote(why));
            if (Plan(flow))
            {
                _waitMs = _options.PauseMs;
                _step = Step.Delay;
            }
        }

        private void Finish(string reason, string? why)
        {
            EndReason = reason;
            _step = Step.Done;
            if (reason == "error" && why != null)
            {
                CaptureLog.Error("autoplay", why);
            }

            CaptureLog.Write("autoplay_end", "\"level\":" + I(_level) + ",\"reason\":\"" + reason + "\",\"pours\":" + I(Pours) + ",\"left\":" + I(Left));
        }

        private static string I(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
