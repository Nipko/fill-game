#nullable enable
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Avanza el Mercado y las ofertas de B5 cada fotograma (<see cref="GameBootstrap.SetUpMarket"/> lo añade al mismo objeto): la
    /// victoria (Cántaro y tarjeta), los intersticiales vistos y las entradas del inicio (<see cref="GameBootstrap.TickMarket"/>). Es
    /// un componente aparte, como <see cref="SessionRunner"/>, para no tocar <c>GameBootstrap.cs</c>. Los tests pueden apagarlo
    /// (<c>enabled = false</c>) y llamar a <see cref="GameBootstrap.TickMarket"/> a mano.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MarketRunner : MonoBehaviour
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
                _boot.TickMarket(Mathf.Min(Time.unscaledDeltaTime * 1000f, 100f));
            }
        }
    }
}
