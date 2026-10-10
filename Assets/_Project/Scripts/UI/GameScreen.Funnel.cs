#nullable enable
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// Parcial del embudo de D-008 (arquitectura del paso 1, §7; tarea T08): viste las cabeceras de ¿Seguro? y Nivel fallido con
    /// la etiqueta del nivel (propuesta-mecanicas-dificultad.md §c.1, D-009: ámbar en Difícil, índigo en Muy difícil y la de
    /// siempre en normal) sin editar GameScreen.cs. La etiqueta sale de <c>difficulty.label</c> del nivel, nunca del premio:
    /// Remote Config puede igualar dos tramos de <c>eco_reward_*</c>.
    /// </summary>
    public sealed partial class GameScreen
    {
        // Gancho de T04: GameScreen.cs lo llama justo antes de cada _giveUp.Show y _failed.Show (al abrir el paso del embudo y al
        // reanudar el nivel guardado con el modal abierto).
        partial void FunnelModalShowing(FlowPhase phase, LevelFlow flow)
        {
            string label = flow.Level.DifficultyLabel;
            if (phase == FlowPhase.GiveUpConfirm)
            {
                _giveUp.SetLabel(label);
            }
            else if (phase == FlowPhase.LevelFailed)
            {
                _failed.SetLabel(label);
            }
        }
    }
}
