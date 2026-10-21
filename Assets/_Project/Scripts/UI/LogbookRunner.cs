#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Avanza la Bitácora del río cada fotograma (<see cref="GameBootstrap.TickLogbook"/>: la libreta, la tarjeta del inicio y el
    /// takeover al entrar al inicio) y avisa de las idas y vueltas a segundo plano (<see cref="GameBootstrap.LogbookPause"/>: la
    /// sesión de la cola de takeovers, Q-M4). Lo añade <see cref="GameBootstrap.SetUpLogbook"/>; no lleva estado propio.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LogbookRunner : MonoBehaviour
    {
        private GameBootstrap? _bootstrap;

        /// <summary>Une el componente al arranque.</summary>
        public void Bind(GameBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
        }

        private void Update()
        {
            _bootstrap?.TickLogbook(Time.unscaledDeltaTime * 1000f);
        }

        private void OnApplicationPause(bool paused)
        {
            _bootstrap?.LogbookPause(paused);
        }
    }
}
