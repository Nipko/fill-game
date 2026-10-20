#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Reloj de las sondas de la Crecida (B1a, A7): cada fotograma llama a <see cref="GameBootstrap.TickStreakProbe"/>, que
    /// escribe las líneas de cada fin de nivel nuevo y el <c>streak_state</c> del cambio de día. Es un componente aparte, como
    /// <see cref="DailyChallengeRunner"/>, porque <c>GameBootstrap.Update</c> está en <c>GameBootstrap.cs</c>, que la rama no
    /// toca: <see cref="GameBootstrap.SetUpStreak"/> lo añade al mismo objeto, solo en desarrollo (<see cref="DevProbe.Enabled"/>).
    /// Los tests pueden apagarlo (<c>enabled = false</c>) y llamar a <see cref="GameBootstrap.TickStreakProbe"/> a mano.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StreakProbeRunner : MonoBehaviour
    {
        private GameBootstrap? _boot;

        /// <summary>Asocia el arranque que lo creó.</summary>
        public void Bind(GameBootstrap boot)
        {
            _boot = boot;
        }

        private void Update()
        {
            if (_boot != null)
            {
                _boot.TickStreakProbe();
            }
        }
    }
}
