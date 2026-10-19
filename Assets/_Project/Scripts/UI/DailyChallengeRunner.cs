#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Reloj del Reto del río (tarea R9): avanza cada fotograma la tarjeta, la partida y el calendario del Reto
    /// (<see cref="GameBootstrap.TickDaily"/>). Es un componente aparte porque <c>GameBootstrap.Update</c> está en
    /// <c>GameBootstrap.cs</c>, que la rama del Reto no toca (encargo §3.2): <c>GameBootstrap.SetUpDaily</c> lo añade al mismo
    /// objeto. Los tests lo apagan (<c>enabled = false</c>) y llaman a <see cref="GameBootstrap.TickDaily"/> con pasos fijos, como
    /// <c>PlayModeHelpers.TakeClock</c> con el arranque.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DailyChallengeRunner : MonoBehaviour
    {
        private const float MaxDeltaMs = 100f;

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
                _boot.TickDaily(Mathf.Min(Time.unscaledDeltaTime * 1000f, MaxDeltaMs));
            }
        }
    }
}
