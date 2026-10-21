#nullable enable
using AF.IAP;

namespace AF.UI
{
    /// <summary>
    /// Enganche B5-rc (bloque B5): las cifras de las ofertas y del Cántaro (<see cref="OffersConfig"/>) salen de Remote Config, como
    /// el resto (<c>RcConfigBinder</c>, B7). Implementa el gancho de <c>GameBootstrap.Market.cs</c>.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        static partial void BoundOffersConfig(ref OffersConfig? config)
        {
            config = ActiveRcConfigs?.Offers;
        }
    }
}
