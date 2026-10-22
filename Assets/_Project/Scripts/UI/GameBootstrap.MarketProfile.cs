#nullable enable
using AF.GameFlow;
using AF.IAP;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// Enganche B5-profile (bloque B5): el Cántaro y las ofertas se guardan en los bloques <c>piggy</c> y <c>offers</c> del mismo
    /// perfil que la cartera, por la vía síncrona del lote (<see cref="ProfileSaveBatch.Durable"/>), como la cartera. Comprar el
    /// Cántaro son dos escrituras seguidas (la cartera acredita y después el Cántaro se vacía): el riesgo, en
    /// <c>docs/dev/mercado.md</c> §8. Implementa el gancho de <c>GameBootstrap.Market.cs</c>.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        static partial void CreateProfileOffersStore(Profile profile, ProfileSaveBatch batch, ref IOffersStore? store)
        {
            store = new ProfileOffersStore(profile, batch.Durable);
        }
    }
}
