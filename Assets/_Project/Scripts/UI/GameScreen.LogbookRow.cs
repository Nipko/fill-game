#nullable enable
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// La fila «Bitácora · elige tu premio» del panel único de la victoria (D-033, añadido del 2026-10-20, puntos 5 y 10; B4, Q-B11;
    /// cierre de B11): el día en que la casilla de la Bitácora se puede sellar (<see cref="IGameActions.LogbookVictoryRowDue"/>,
    /// «al terminar el primer nivel del día»), la victoria de campaña lleva la tercera barra rotativa (prioridad skin ganada &gt;
    /// Bitácora lista &gt; Reto: de las tres, hoy solo existe esta), bajo la Crecida y la Canasta (<see cref="VictoryBars"/>). Un
    /// toque abre la libreta encima de la victoria (<see cref="IGameActions.OpenLogbookFromVictory"/>), que sigue igual debajo, como
    /// «Ver mapa»; al cerrarla se vuelve a la victoria y, si la casilla ya se selló, la fila se va y la victoria se recoloca. Responde
    /// como «Ver mapa»: nada en AD_GATE ni mientras la victoria espera a avanzar sola tras la Cosecha doble. En <c>AF_LAYOUT victory</c>,
    /// el id <c>victory_logbook</c>.
    /// <list type="bullet">
    /// <item>Victoria compacta (FX16b y B1b): la fila cuenta en la victoria más alta posible solo cuando esta victoria la lleva, así que
    /// las demás no cambian de escalón; en los escalones 0-2 va a la vista (bajo la fila de las dos barras a media anchura desde el
    /// 2), y si con ella haría falta el escalón 3, donde iría tras el chevron (D-033, punto 10: «2 barras y chevron»), no sale y el
    /// escalón se elige sin ella [P] (<see cref="VictoryBars.LogbookSuppressed"/>). Sin ella, la libreta sale sola al volver al inicio
    /// (D-033, punto 5).</item>
    /// <item>No sale en la victoria del 10 (su secuencia abre la libreta después: D-033, punto 11), ni en el Reto (sin barras: Q-C2), ni
    /// en el hito de TiTino que se queda sin barras (B1b-Q10 (c)) [P].</item>
    /// </list>
    /// </summary>
    public sealed partial class GameScreen
    {
        /// <summary>Nivel cuya victoria no lleva la fila: la secuencia del 10 abre la libreta tras ella (06 §2.3; D-033, punto 11).</summary>
        internal const int LogbookRowSkipLevel = 10;

        /// <summary>Veces que la fila abrió la libreta (tests).</summary>
        internal int VictoryLogbookOpened { get; private set; }

        /// <summary>La victoria de ahora lleva la fila a la vista.</summary>
        internal bool VictoryLogbookShown => _victoryBars.LogbookShown;

        // StreakVictoryPlan: esta victoria lleva la fila (campaña con barras, no el 10, y la casilla de hoy se puede sellar).
        private bool LogbookRowWanted(LevelFlow flow)
        {
            return VictoryBarsPossible && flow.LevelNumber != LogbookRowSkipLevel && _actions.LogbookVictoryRowDue;
        }

        // El toque en la fila: la libreta encima de la victoria (no en AD_GATE ni en la espera tras la Cosecha doble o Siguiente).
        private void OnVictoryLogbookPressed()
        {
            LevelFlow? flow = _flow;
            if (!_victoryBars.LogbookShown || _autoNextMs >= 0f || _nextHoldMs >= 0f || flow == null || flow.Phase != FlowPhase.VictoryScreen)
            {
                return;
            }

            if (_actions.OpenLogbookFromVictory())
            {
                VictoryLogbookOpened++;
            }
        }

        // Cada fotograma de la victoria (StreakTick): si la casilla ya no se puede sellar (se selló en la libreta), la fila se va y la
        // victoria se recoloca, como tras la Cosecha doble. Sin asignaciones mientras nada cambia.
        private void LogbookRowTick()
        {
            if (!_victoryBars.Plan.Logbook || _actions.LogbookVictoryRowDue)
            {
                return;
            }

            _victoryBars.DropLogbook();
            LevelFlow? flow = _flow;
            if (flow == null || !flow.HasLevel || (flow.Phase != FlowPhase.VictoryScreen && flow.Phase != FlowPhase.AdGate))
            {
                return;
            }

            LayoutVictory();
            ShiftBoard(ShiftToClear(_victoryPanelY - RibbonRiseDp, 8f), true);
            ApplyVictoryAnim(_victoryAnim >= 0f ? _victoryAnim : PanelSlideMs + 1000f);
        }
    }
}
