#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Avanza la Liga del río cada fotograma (<see cref="GameBootstrap.TickLeague"/>: el botón del inicio, la pantalla y el reintento
    /// de la cola), como <see cref="MarketRunner"/>, para no tocar más <c>GameBootstrap.cs</c>. Los tests pueden apagarlo y llamar a
    /// <see cref="GameBootstrap.TickLeague"/> a mano.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LeagueRunner : MonoBehaviour
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
                _boot.TickLeague(Mathf.Min(Time.unscaledDeltaTime * 1000f, 100f));
            }
        }
    }
}
