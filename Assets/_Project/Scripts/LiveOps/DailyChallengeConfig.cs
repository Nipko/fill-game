#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Premio del Reto del río: <c>live_daily_challenge_reward</c> = <c>{"seeds":60,"murky_bonus_mult":2}</c> (08 §9; 06 §4.1;
    /// 05 §2.1, fila «Reto del río»).
    /// </summary>
    public readonly struct DailyChallengeReward : IEquatable<DailyChallengeReward>
    {
        /// <summary>Crea el premio.</summary>
        public DailyChallengeReward(int seeds, int murkyBonusMult)
        {
            Seeds = seeds;
            MurkyBonusMult = murkyBonusMult;
        }

        /// <summary><c>seeds</c>: semillas del premio diario (igual en las tres bandas).</summary>
        public int Seeds { get; }

        /// <summary><c>murky_bonus_mult</c>: multiplicador del bonus Aguas turbias (contenido de la 1.1; <c>ui_murky_bonus_enabled</c>).</summary>
        public int MurkyBonusMult { get; }

        /// <summary>Escribe el premio con la sintaxis de 08 §9: <c>{"seeds":60,"murky_bonus_mult":2}</c>.</summary>
        public string Format()
        {
            return "{\"seeds\":" + Seeds.ToString(CultureInfo.InvariantCulture) + ",\"murky_bonus_mult\":"
                + MurkyBonusMult.ToString(CultureInfo.InvariantCulture) + "}";
        }

        /// <inheritdoc />
        public bool Equals(DailyChallengeReward other)
        {
            return Seeds == other.Seeds && MurkyBonusMult == other.MurkyBonusMult;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is DailyChallengeReward other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Seeds * 397) ^ MurkyBonusMult;
        }
    }

    /// <summary>
    /// Cifras del Reto del río (<c>daily_challenge</c>; 06 §4.1; 03 §16) con los <b>defaults compilados</b> de la tabla única de
    /// 08 §9 y sus nombres exactos (nota <c>docs/dev/reto-del-rio.md</c> §11): 8 claves propias y <c>sec_clock_max_drift_h</c>
    /// a través de <see cref="ClockSecurity"/>. Ninguna cifra sale de aquí:
    /// <c>DailyChallengeConfigTests</c> las contrasta con 08 §9, como <c>EconomyConfigTests</c>. No usa ninguno de los alias
    /// retirados (<c>daily_offset</c>, <c>daily_reward_seeds</c>, <c>daily_bands</c>, <c>daily_bracket_levels</c>…). Cuando
    /// exista <c>AF.RemoteConfig</c>, sus valores se copiarán en una instancia; hasta entonces se usa <see cref="Defaults"/>.
    /// </summary>
    public sealed class DailyChallengeConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static DailyChallengeConfig Defaults => new DailyChallengeConfig();

        /// <summary><c>biz_feature_daily_challenge</c>: kill switch del Reto del río (08 §9, fila <c>biz_feature_&lt;sistema&gt;</c>).</summary>
        [RemoteConfigKey("biz_feature_daily_challenge")]
        public bool BizFeatureDailyChallenge { get; set; } = true;

        /// <summary><c>live_unlock_level_daily</c>: el Reto se desbloquea al superar este nivel (06 §2.3; 06 §4.1).</summary>
        [RemoteConfigKey("live_unlock_level_daily")]
        public int LiveUnlockLevelDaily { get; set; } = 10;

        /// <summary>
        /// <c>lvl_daily_bracket_levels</c>: umbrales de las 3 bandas por nivel máximo (03 §16): B1 por debajo del primero, B2
        /// por debajo del segundo y B3 desde él. Dos valores en orden creciente.
        /// </summary>
        [RemoteConfigKey("lvl_daily_bracket_levels")]
        public IReadOnlyList<int> LvlDailyBracketLevels { get; set; } = new[] { 41, 201 };

        /// <summary><c>live_daily_offset</c>: desplazamiento del índice por fecha (03 §16); puede ser negativo.</summary>
        [RemoteConfigKey("live_daily_offset")]
        public int LiveDailyOffset { get; set; }

        /// <summary>
        /// <c>daily_overrides</c>: sustituciones fechadas, con la forma de R-Q6 <c>{fecha: {banda: id}}</c> (nota §1.3 y §6;
        /// 08 §9 aún dice <c>{fecha: level_id}</c>: lo actualiza el integrador). Default <c>{}</c>.
        /// </summary>
        [RemoteConfigKey("daily_overrides")]
        public DailyOverrides DailyOverrides { get; set; } = DailyOverrides.Empty;

        /// <summary><c>live_daily_challenge_reward</c>: premio diario (60 semillas; el bonus Aguas turbias paga ×2).</summary>
        [RemoteConfigKey("live_daily_challenge_reward")]
        public DailyChallengeReward LiveDailyChallengeReward { get; set; } = new DailyChallengeReward(60, 2);

        /// <summary><c>live_daily_streak7_reward</c>: premio por 7 Retos seguidos (<c>shuffle</c> = 1 Pororoca al stock; 04 §2.4).</summary>
        [RemoteConfigKey("live_daily_streak7_reward")]
        public string LiveDailyStreak7Reward { get; set; } = "shuffle";

        /// <summary><c>ui_murky_bonus_enabled</c>: tarjeta Aguas turbias · Bonus (1.1; false en la 1.0).</summary>
        [RemoteConfigKey("ui_murky_bonus_enabled")]
        public bool UiMurkyBonusEnabled { get; set; }

        /// <summary>
        /// Anti-reloj (08 §10.1): <c>sec_clock_max_drift_h</c>, la clave global que también usará Canoas (04 §7.1). No se declara
        /// aquí: su única fuente es <see cref="ClockSecurityConfig"/> en <c>AF.Platform</c>, porque <c>AF.Economy</c> no puede
        /// referenciar <c>AF.LiveOps</c>. Solo lectura: se cambia su valor, no la instancia (cuando exista <c>AF.RemoteConfig</c>
        /// se copiará en ella, como el resto de las claves).
        /// </summary>
        public ClockSecurityConfig ClockSecurity { get; } = new ClockSecurityConfig();

        /// <summary>Comprueba que los valores tienen sentido (no que coincidan con 08 §9: eso lo hacen los tests).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            Require(LiveUnlockLevelDaily >= 1, "live_unlock_level_daily debe ser >= 1");
            IReadOnlyList<int>? thresholds = LvlDailyBracketLevels;
            Require(thresholds != null && thresholds.Count == DailyCalendar.BandCount - 1,
                "lvl_daily_bracket_levels debe tener " + (DailyCalendar.BandCount - 1) + " umbrales");
            Require(thresholds![0] >= 1 && thresholds[1] > thresholds[0], "lvl_daily_bracket_levels debe ir en orden creciente desde 1");
            Require(DailyOverrides != null, "daily_overrides no puede ser null");
            Require(LiveDailyChallengeReward.Seeds >= 0 && LiveDailyChallengeReward.MurkyBonusMult >= 1,
                "live_daily_challenge_reward inválido");
            Require(!string.IsNullOrEmpty(LiveDailyStreak7Reward), "live_daily_streak7_reward no puede estar vacío");
            ClockSecurity.Validate();
        }

        /// <summary>Escribe los umbrales con la sintaxis de 08 §9: <c>[41, 201]</c>.</summary>
        public static string FormatThresholds(IReadOnlyList<int> thresholds)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < thresholds.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(thresholds[i].ToString(CultureInfo.InvariantCulture));
            }

            return sb.Append(']').ToString();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("DailyChallengeConfig: " + message + ".");
            }
        }
    }
}
