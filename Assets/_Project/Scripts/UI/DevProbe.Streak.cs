#nullable enable
using System.Collections.Generic;
using AF.Economy;

namespace AF.UI
{
    /// <summary>
    /// Sondas de la Crecida, la Hoja escudo y la Canasta (B1a, A7; nota <c>docs/dev/crecida-canasta.md</c> §5 y E33). Son solo de
    /// desarrollo (logcat), no eventos de 08 §8.2: van al log de los builds de desarrollo (<see cref="Enabled"/>) como
    /// <c>AF_STATE</c>, con el formato de <see cref="StreakProbeFormat"/> (AF.Economy, probado en EditMode) y el canal de siempre
    /// (<see cref="Write"/>, con el tope de <see cref="MaxLineBytes"/>):
    /// <list type="bullet">
    /// <item><c>streak_fail</c>, <c>streak_shield_used</c>, <c>free_double_harvest</c>, <c>streak_milestone</c> y
    /// <c>basket_open</c> de cada <see cref="StreakServiceResult"/>, en ese orden, seguidas de <c>streak_state</c> si el estado
    /// cambió (<see cref="StreakResult"/>);</item>
    /// <item><c>streak_state</c> al crear el servicio y al cambiar el día local (<see cref="StreakState"/>).</item>
    /// </list>
    /// Estáticas, como las del Reto: las llama <c>GameBootstrap.Streak.cs</c> (<see cref="GameBootstrap.TickStreakProbe"/>). Solo
    /// en el hilo principal (un único <see cref="StreakProbeFormat"/>). Solo claves y números, nunca texto visible.
    /// </summary>
    public sealed partial class DevProbe
    {
        private static readonly StreakProbeFormat s_streak = new StreakProbeFormat();
        private static readonly List<string> s_streakLines = new List<string>(6);

        /// <summary>
        /// Las líneas de <paramref name="result"/> (<see cref="StreakProbeFormat.AppendResult"/>), con <c>streak_state</c> de
        /// <paramref name="after"/> (consultado para <paramref name="level"/>) si el estado cambió. Devuelve cuántas escribió (0 fuera
        /// de desarrollo).
        /// </summary>
        public static int StreakResult(StreakServiceResult result, StreakSnapshot? after, int level)
        {
            if (!Enabled || result == null)
            {
                return 0;
            }

            s_streakLines.Clear();
            int count = s_streak.AppendResult(s_streakLines, NowMs(), result, after, level);
            for (int i = 0; i < s_streakLines.Count; i++)
            {
                Write(s_streakLines[i]);
            }

            s_streakLines.Clear();
            return count;
        }

        /// <summary>
        /// <c>AF_STATE streak_state</c> con <c>cause</c> = <paramref name="cause"/> (<see cref="StreakProbeFormat.CauseStart"/> o
        /// <see cref="StreakProbeFormat.CauseDay"/>). Devuelve la línea escrita, o null fuera de desarrollo.
        /// </summary>
        public static string? StreakState(string cause, int level, in StreakSnapshot state)
        {
            if (!Enabled)
            {
                return null;
            }

            string line = s_streak.State(NowMs(), cause, level, in state);
            Write(line);
            return line;
        }
    }
}
