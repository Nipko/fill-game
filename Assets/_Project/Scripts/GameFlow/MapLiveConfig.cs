#nullable enable
using AF.Platform;

namespace AF.GameFlow
{
    /// <summary>
    /// Interruptores del Mapa del río v2 de la 1.1 (D-069, P8 (a); <c>docs/dev/propuesta-mapa-rio-v2.md</c>) con los defaults
    /// compilados de la tabla única de 08 §9. <see cref="UiMapV2"/> enciende el río vivo (M2-1); apagada, el Mapa de la 1.0 con los
    /// mismos datos <c>/2</c>. <see cref="UiMapTowns"/> enciende los pueblos de paso y la excursión local (M2-2); <see cref="UiMapCrew"/> es de M2-4: hasta que entre, sin efecto.
    /// </summary>
    public sealed class MapLiveConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static MapLiveConfig Defaults => new MapLiveConfig();

        /// <summary><c>ui_map_v2</c>: el Mapa v2 (pantalla completa y río vivo); apagada, el de la 1.0.</summary>
        [RemoteConfigKey("ui_map_v2")]
        public bool UiMapV2 { get; set; } = true;

        /// <summary><c>ui_map_crew</c>: la tripulación del Mapa v2 (M2-4). Solo cuenta con <see cref="UiMapV2"/>.</summary>
        [RemoteConfigKey("ui_map_crew")]
        public bool UiMapCrew { get; set; } = true;

        /// <summary><c>ui_map_towns</c>: los pueblos de paso del Mapa v2 (M2-2). Solo cuenta con <see cref="UiMapV2"/>.</summary>
        [RemoteConfigKey("ui_map_towns")]
        public bool UiMapTowns { get; set; } = true;

        /// <summary>Nada que comprobar (tres bool).</summary>
        public void Validate()
        {
        }
    }
}
