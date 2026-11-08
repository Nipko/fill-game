#nullable enable
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// El Mapa del río v2 (D-069, P8 (a)): <c>ui_map_v2</c> de Remote Config (08 §9) llega a <see cref="RiverMapScreen.LiveRequested"/>
    /// antes de cada apertura; apagada, el Mapa de la 1.0 con los mismos datos <c>/2</c>. <c>ui_map_towns</c> llega a
    /// <see cref="RiverMapScreen.TownsRequested"/> (M2-2: los pueblos de paso y la excursión local); <c>ui_map_crew</c> llega a
    /// <see cref="RiverMapScreen.CrewRequested"/> (M2-4: la tripulación y el cameo de los miquitos).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Interruptores del Mapa v2 para los tests (ganan a Remote Config).</summary>
        internal static MapLiveConfig? MapLiveConfigOverride { get; set; }

        /// <summary>
        /// Los interruptores del Mapa v2 de esta sesión: los del test, los de Remote Config o los defaults de 08 §9. En desarrollo, el
        /// extra <c>af_map_v1</c> (<see cref="LaunchOptions.ResolveMapV1"/>) apaga <c>ui_map_v2</c> (la medida de referencia de la
        /// memoria con el Mapa de la 1.0; añadido de D-069 del 2026-11-08).
        /// </summary>
        public MapLiveConfig MapLiveSettings
        {
            get
            {
                MapLiveConfig config = MapLiveConfigOverride ?? _rcConfigs?.MapLive ?? MapLiveConfig.Defaults;
                if (MapLiveConfigOverride == null && config.UiMapV2 && LaunchOptions.ResolveMapV1(UnityEngine.Debug.isDebugBuild))
                {
                    return new MapLiveConfig { UiMapV2 = false, UiMapTowns = config.UiMapTowns, UiMapCrew = config.UiMapCrew };
                }

                return config;
            }
        }
    }
}
