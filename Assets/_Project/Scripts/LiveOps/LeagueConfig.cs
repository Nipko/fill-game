#nullable enable
using System;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Cifras de la Liga del río de la 1.0 (D-046; <c>docs/dev/liga.md</c> §5) con los defaults compilados de la tabla única de
    /// 08 §9 y sus nombres exactos: el interruptor (apagado hasta que haya backend en prod), el nivel de desbloqueo y los minutos
    /// que vale la clasificación descargada. Los puntos no son de Remote Config: los calcula el servidor (liga.md §3.2) y el
    /// cliente solo los enseña en la victoria (<see cref="LeagueRules"/>).
    /// </summary>
    public sealed class LeagueConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static LeagueConfig Defaults => new LeagueConfig();

        /// <summary><c>live_league_enabled</c>: la Liga existe en el cliente (botón, pantalla, cola y red). Apagada por defecto (D-046).</summary>
        [RemoteConfigKey("live_league_enabled")]
        public bool LiveLeagueEnabled { get; set; }

        /// <summary>
        /// <c>live_league_unlock_level</c>: la Liga aparece al ganar este nivel (06 §6; liga.md §1). Fijo en 41 (08 §9, rango 41-41): el
        /// servidor no suma por debajo (<see cref="LeagueRules.ServerUnlockLevel"/>); cambiarlo exige cambiar también el backend.
        /// </summary>
        [RemoteConfigKey("live_league_unlock_level")]
        public int LiveLeagueUnlockLevel { get; set; } = 41;

        /// <summary><c>live_league_cache_minutes</c> [P]: minutos que vale la clasificación descargada antes de volver a pedirla.</summary>
        [RemoteConfigKey("live_league_cache_minutes")]
        public int LiveLeagueCacheMinutes { get; set; } = 10;

        /// <summary>Comprueba que los valores tienen sentido (no que coincidan con 08 §9: eso lo hacen los tests).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            if (LiveLeagueUnlockLevel != LeagueRules.ServerUnlockLevel)
            {
                throw new InvalidOperationException("LeagueConfig: live_league_unlock_level es fijo en 41 (08 §9; el backend no suma por debajo).");
            }

            if (LiveLeagueCacheMinutes < 0 || LiveLeagueCacheMinutes > 24 * 60)
            {
                throw new InvalidOperationException("LeagueConfig: live_league_cache_minutes debe estar entre 0 y 1440.");
            }
        }
    }
}
