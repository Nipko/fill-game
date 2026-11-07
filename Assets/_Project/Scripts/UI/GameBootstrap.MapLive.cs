#nullable enable
using AF.GameFlow;

namespace AF.UI
{
    /// <summary>
    /// El Mapa del río v2 (D-069, P8 (a)): <c>ui_map_v2</c> de Remote Config (08 §9) llega a <see cref="RiverMapScreen.LiveRequested"/>
    /// antes de cada apertura; apagada, el Mapa de la 1.0 con los mismos datos <c>/2</c>. <c>ui_map_crew</c> y <c>ui_map_towns</c>
    /// son de M2-4 y M2-2 (sin efecto hasta entonces).
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>Interruptores del Mapa v2 para los tests (ganan a Remote Config).</summary>
        internal static MapLiveConfig? MapLiveConfigOverride { get; set; }

        /// <summary>Los interruptores del Mapa v2 de esta sesión: los del test, los de Remote Config o los defaults de 08 §9.</summary>
        public MapLiveConfig MapLiveSettings => MapLiveConfigOverride ?? _rcConfigs?.MapLive ?? MapLiveConfig.Defaults;
    }
}
