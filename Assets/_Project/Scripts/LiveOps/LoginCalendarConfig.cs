#nullable enable
using System;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Cifras de la Bitácora del río (<c>login_calendar</c>; 06 §4.2; 05 §2.3) con los defaults compilados de la tabla única de
    /// 08 §9 y sus nombres exactos, más la clave nueva [P] de D-033 (añadido del 2026-10-20, «Remote Config: clave nueva
    /// <c>eco_calendar_table_v2</c>»), que aún no está en 08 §9: la da de alta el integrador (nota <c>docs/dev/bitacora.md</c> §9).
    /// No usa los alias retirados. Cuando exista <c>AF.RemoteConfig</c>, sus valores se copiarán en una instancia; hasta entonces se
    /// usa <see cref="Defaults"/>.
    /// <list type="bullet">
    /// <item><c>live_unlock_level_daily</c> (la Bitácora se desbloquea con el Reto, 08 §9) no se declara aquí: su única fuente es
    /// <see cref="DailyChallengeConfig"/>; <see cref="LoginCalendarService"/> lo recibe como argumento.</item>
    /// <item><c>eco_calendar_double_ad_enabled</c> tampoco: su única fuente es <c>AF.Ads.AdsConfig</c> (la ubicación
    /// <c>rv_daily_chest</c>, apagada en la 1.0); la Bitácora de la 1.0 no la necesita.</item>
    /// <item><c>eco_calendar_table_v1</c> no se lee: la 1.0 lee la v2, y la v1 queda en RC de respaldo (D-033) para builds que
    /// no la conocen; el respaldo del cliente es <see cref="LoginCalendarTable.Default"/> (Q-B4 [P]).</item>
    /// </list>
    /// </summary>
    public sealed class LoginCalendarConfig
    {
        /// <summary>Una instancia nueva con los defaults (cada llamada devuelve otra).</summary>
        public static LoginCalendarConfig Defaults => new LoginCalendarConfig();

        /// <summary><c>biz_feature_login_calendar</c>: kill switch de la Bitácora (08 §9, fila <c>biz_feature_&lt;sistema&gt;</c>).</summary>
        [RemoteConfigKey("biz_feature_login_calendar")]
        public bool BizFeatureLoginCalendar { get; set; } = true;

        /// <summary>
        /// <c>eco_calendar_table_v2</c> [P] (D-033): las 30 casillas × 3 opciones y el canon (<see cref="LoginCalendarTable.Format"/>).
        /// Default: la tabla aprobada por el dueño (<see cref="LoginCalendarTable.Default"/>).
        /// </summary>
        [RemoteConfigKey("eco_calendar_table_v2")]
        public LoginCalendarTable EcoCalendarTableV2 { get; set; } = LoginCalendarTable.Default;

        /// <summary>
        /// <c>eco_calendar_locked_booster_seeds</c> (08 §9: <c>{"undo3":30,"other":100}</c>): con D-033, punto 1, la tasa de valor
        /// de las tres opciones (y de <c>reward_value</c>), no la conversión a semillas de la v1, que el «1 de 3» sustituye por la
        /// regla del comodín siguiente (punto 3).
        /// </summary>
        [RemoteConfigKey("eco_calendar_locked_booster_seeds")]
        public LoginCalendarValueRate EcoCalendarLockedBoosterSeeds { get; set; } = LoginCalendarValueRate.Default;

        /// <summary>Comprueba que los valores tienen sentido (no que coincidan con 08 §9: eso lo hacen los tests).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            if (EcoCalendarTableV2 == null)
            {
                throw new InvalidOperationException("LoginCalendarConfig: eco_calendar_table_v2 no puede ser null.");
            }

            if (EcoCalendarLockedBoosterSeeds.Undo <= 0 || EcoCalendarLockedBoosterSeeds.Other <= 0)
            {
                throw new InvalidOperationException("LoginCalendarConfig: eco_calendar_locked_booster_seeds debe ser positivo.");
            }

            EcoCalendarTableV2.Validate(EcoCalendarLockedBoosterSeeds);
        }
    }
}
