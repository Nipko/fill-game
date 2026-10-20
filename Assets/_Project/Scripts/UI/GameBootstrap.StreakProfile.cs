#nullable enable
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// El almacén de la Crecida sobre el perfil (parche S3-bootstrap; necesita S1-profile, que trae <see cref="ProfileStreakStore"/>;
    /// E32): implementa el gancho <c>CreateProfileStreakStore</c> de <c>GameBootstrap.Streak.cs</c>.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        // E32: el mismo perfil que la cartera y la vía síncrona del lote (ProfileSaveBatch.Durable), la misma que la cartera: el
        // apunte de un hito y su pago salen en la misma escritura del perfil, y un hito nunca se paga dos veces el mismo día.
        static partial void CreateProfileStreakStore(Profile profile, ProfileSaveBatch batch, IClock clock, ref IStreakStore? store)
        {
            store = new ProfileStreakStore(profile, batch.Durable, clock);
        }
    }
}
