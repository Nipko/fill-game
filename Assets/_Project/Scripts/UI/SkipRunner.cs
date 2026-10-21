#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>Avanza el banner del Salto del Boto cada fotograma (lo añade <see cref="GameBootstrap.SetUpSkip"/>; bloque B3).</summary>
    public sealed class SkipRunner : MonoBehaviour
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
                _boot.TickSkipBanner();
            }
        }
    }
}
