#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Reloj de la meta de B14: cada fotograma llama a <see cref="GameBootstrap.TickMeta"/> (el aviso de skin y las sondas). Es un
    /// componente aparte, como <see cref="StreakProbeRunner"/>, para no tocar <c>GameBootstrap.Update</c>; a diferencia de aquel,
    /// también corre en release (el aviso es del juego).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MetaRunner : MonoBehaviour
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
                _boot.TickMeta(Mathf.Min(Time.unscaledDeltaTime * 1000f, 100f));
            }
        }
    }
}
