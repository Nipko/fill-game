#nullable enable
using AF.GameFlow;
using AF.LiveOps;
using AF.Save;

namespace AF.UI
{
    /// <summary>
    /// Los almacenes de B6 sobre el perfil (parche B6-bootstrap; necesita B6-profile, que trae <see cref="ProfileSessionStore"/> y
    /// <see cref="ProfilePushSettingsStore"/>): implementa el gancho <c>CreateProfileSessionStores</c> de
    /// <c>GameBootstrap.Sessions.cs</c>.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        // El mismo perfil que la cartera y la vía síncrona del lote (ProfileSaveBatch.Durable), la misma que la cartera:
        // stats.last_seen_at llega al disco antes que el Regalo de regreso, y una ausencia nunca se cobra dos veces.
        static partial void CreateProfileSessionStores(Profile profile, ProfileSaveBatch batch, ref ISessionStore? sessions, ref IPushSettingsStore? push)
        {
            sessions = new ProfileSessionStore(profile, batch.Durable);
            push = new ProfilePushSettingsStore(profile, batch.Durable);
        }
    }
}
