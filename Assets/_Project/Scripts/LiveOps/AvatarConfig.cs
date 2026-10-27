#nullable enable
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Cifras de los avatares del jugador (D-056; <c>docs/dev/avatares.md</c> §4.3) con el default compilado de la tabla única de
    /// 08 §9: solo el interruptor de respaldo. Los hitos van en el código (<see cref="AvatarCatalog"/>), no en Remote Config.
    /// </summary>
    public sealed class AvatarConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static AvatarConfig Defaults => new AvatarConfig();

        /// <summary>
        /// <c>ui_avatars_enabled</c>: apagada, sin retrato en el inicio, sin fila en Ajustes y sin avatar en la Liga ni en la victoria;
        /// el perfil conserva el campo y la Liga deja de enviarlo.
        /// </summary>
        [RemoteConfigKey("ui_avatars_enabled")]
        public bool UiAvatarsEnabled { get; set; } = true;

        /// <summary>Nada que comprobar (un solo bool).</summary>
        public void Validate()
        {
        }
    }
}
