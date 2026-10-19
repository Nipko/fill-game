#nullable enable
using System.Globalization;
using System.Text;
using AF.Boosters;
using AF.GameFlow;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Sonda de Morpho en la partida (tarea MO1; 04 §2.3; 08 §9 <c>hint_timeout_ms</c>), solo en desarrollo: por cada respuesta
    /// que recibe el jugador, <c>AF_PERF {"event":"morpho","source":"hint","level":…,"answer":"pour","solver":"astar",
    /// "realities":1,"nodes":…,"ms":…,"wait_ms":…,"cancelled":false,"charged":true,"tinaja":false,"timeout_ms":150}</c>.
    /// <c>ms</c> es lo que buscó el hilo de trabajo (reloj real) y <c>wait_ms</c>, lo que esperó el jugador desde que la pidió
    /// (reloj del flujo); <c>answer</c> es <c>pour</c>, <c>no_exit</c>, <c>no_safe_pour</c> o <c>not_found</c>. Las líneas del
    /// bench de PF1 (<c>DevBench</c>) no llevan <c>source</c>.
    /// </summary>
    public sealed partial class DevProbe : IHintProbe
    {
        /// <summary>Última línea <c>AF_PERF morpho</c> de la partida (tests; null sin sonda).</summary>
        public string? LastMorphoLine { get; private set; }

        /// <inheritdoc />
        public void Morpho(in HintResult result, int level, int waitMs, int timeoutMs)
        {
            if (!Enabled)
            {
                return;
            }

            LastMorphoLine = FormatMorpho(result, level, waitMs, timeoutMs);
            Write(LastMorphoLine);
        }

        /// <summary>Texto de la línea <c>AF_PERF morpho</c> de una respuesta.</summary>
        public static string FormatMorpho(in HintResult result, int level, int waitMs, int timeoutMs)
        {
            var sb = new StringBuilder(256);
            sb.Append("AF_PERF {\"event\":\"morpho\",\"source\":\"hint\",\"level\":").Append(level.ToString(CultureInfo.InvariantCulture))
                .Append(",\"answer\":\"").Append(AnswerKey(result.Answer)).Append('"')
                .Append(",\"solver\":\"").Append(result.Solver == HintSolver.SafeMoves ? "safe_moves" : "astar").Append('"')
                .Append(",\"realities\":").Append(result.Realities.ToString(CultureInfo.InvariantCulture))
                .Append(",\"nodes\":").Append(result.Nodes.ToString(CultureInfo.InvariantCulture))
                .Append(",\"ms\":").Append(result.SolveMs.ToString("0.0", CultureInfo.InvariantCulture))
                .Append(",\"wait_ms\":").Append(waitMs.ToString(CultureInfo.InvariantCulture))
                .Append(",\"cancelled\":").Append(result.Cancelled ? "true" : "false")
                .Append(",\"charged\":").Append(result.Charged ? "true" : "false")
                .Append(",\"tinaja\":").Append(result.Tinaja ? "true" : "false");
            if (result.HasMove)
            {
                sb.Append(",\"move\":[").Append(result.Move.Source.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(result.Move.Target.ToString(CultureInfo.InvariantCulture)).Append(']');
            }

            sb.Append(",\"timeout_ms\":").Append(timeoutMs.ToString(CultureInfo.InvariantCulture)).Append('}');
            return sb.ToString();
        }

        /// <summary>Nombre de la respuesta en la línea.</summary>
        public static string AnswerKey(HintAnswer answer)
        {
            switch (answer)
            {
                case HintAnswer.Pour:
                    return "pour";
                case HintAnswer.NoExit:
                    return "no_exit";
                case HintAnswer.NoSafePour:
                    return "no_safe_pour";
                default:
                    return "not_found";
            }
        }
    }
}
