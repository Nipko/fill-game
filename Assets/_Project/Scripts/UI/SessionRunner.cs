#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Lleva las sesiones y los avisos de B6 (<see cref="GameBootstrap.SetUpSessions"/> lo añade al mismo objeto): el segundo plano y
    /// la vuelta (<c>OnApplicationPause</c>), el cierre de la app (<c>OnApplicationQuit</c>: se programan los avisos como al pasar a
    /// segundo plano) y, cada fotograma, la detección de victorias para <c>stats.last_win_at</c> y el pre-prompt
    /// (<see cref="GameBootstrap.TickSessions"/>). Es un componente aparte, como <see cref="StreakProbeRunner"/>, para no tocar
    /// <c>GameBootstrap.cs</c>. Los tests pueden apagarlo (<c>enabled = false</c>) y llamar a los métodos a mano.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SessionRunner : MonoBehaviour
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
                _boot.TickSessions();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (_boot != null && enabled)
            {
                _boot.SessionsPaused(paused);
            }
        }

        private void OnApplicationQuit()
        {
            if (_boot != null && enabled)
            {
                _boot.SessionsPaused(true);
            }
        }
    }
}
