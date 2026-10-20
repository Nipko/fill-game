#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Platform;

namespace AF.Economy
{
    /// <summary>
    /// <c>streak_milestones</c> (08 §9): escalera canónica de la Crecida (05 §2.4), <c>{"3":300,"6":"chest","10":"extra_cuia+rio_crecido"}</c>.
    /// Solo el hito 3 es configurable (200-300 semillas); el 6 (Canasta) y el 10 (Cuia extra + río crecido) son fijos.
    /// </summary>
    public readonly struct StreakMilestones : IEquatable<StreakMilestones>
    {
        /// <summary>Valor del hito 6 en el JSON: la Canasta (06 §4.3).</summary>
        public const string Hit6Value = "chest";

        /// <summary>Valor del hito 10 en el JSON: 1 Cuia extra + río crecido, sin semillas (05 §2.4).</summary>
        public const string Hit10Value = "extra_cuia+rio_crecido";

        /// <summary>Mínimo admisible del hito 3 (08 §9, rango «hito 3: 200-300»).</summary>
        public const int Hit3Min = 200;

        /// <summary>Máximo admisible del hito 3.</summary>
        public const int Hit3Max = 300;

        /// <summary>Crea la escalera con <paramref name="hit3Seeds"/> semillas en el hito 3.</summary>
        public StreakMilestones(int hit3Seeds)
        {
            Hit3Seeds = hit3Seeds;
        }

        /// <summary>El default de 08 §9: 300 semillas en el hito 3.</summary>
        public static StreakMilestones Default => new StreakMilestones(300);

        /// <summary>Semillas del hito 3.</summary>
        public int Hit3Seeds { get; }

        /// <summary>El hito 3 está en el rango de 08 §9.</summary>
        public bool IsValid => Hit3Seeds >= Hit3Min && Hit3Seeds <= Hit3Max;

        /// <summary>Escribe la escalera con la sintaxis de 08 §9: <c>{"3":300,"6":"chest","10":"extra_cuia+rio_crecido"}</c>.</summary>
        public string Format()
        {
            return "{\"3\":" + Hit3Seeds.ToString(CultureInfo.InvariantCulture) + ",\"6\":\"" + Hit6Value + "\",\"10\":\"" + Hit10Value + "\"}";
        }

        /// <summary>
        /// Lee el JSON de Remote Config. Si no se puede leer, si falta o sobra algún hito, si el 6 o el 10 no son exactamente
        /// <see cref="Hit6Value"/> y <see cref="Hit10Value"/>, o si el 3 sale de 200-300, devuelve false y deja
        /// <paramref name="result"/> = <see cref="Default"/> (nota <c>docs/dev/crecida-canasta.md</c> §4).
        /// </summary>
        public static bool TryParse(string? json, out StreakMilestones result)
        {
            result = Default;
            if (!EconomyJsonLite.TryReadFlatObject(json, out List<FlatMember> members) || members.Count != 3)
            {
                return false;
            }

            int? hit3 = null;
            bool hit6 = false;
            bool hit10 = false;
            for (int i = 0; i < members.Count; i++)
            {
                FlatMember m = members[i];
                switch (m.Key)
                {
                    case "3":
                        if (m.IsString || !int.TryParse(m.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int seeds))
                        {
                            return false;
                        }

                        hit3 = seeds;
                        break;
                    case "6":
                        hit6 = m.IsString && string.Equals(m.Value, Hit6Value, StringComparison.Ordinal);
                        break;
                    case "10":
                        hit10 = m.IsString && string.Equals(m.Value, Hit10Value, StringComparison.Ordinal);
                        break;
                    default:
                        return false;
                }
            }

            if (hit3 == null || !hit6 || !hit10)
            {
                return false;
            }

            var parsed = new StreakMilestones(hit3.Value);
            if (!parsed.IsValid)
            {
                return false;
            }

            result = parsed;
            return true;
        }

        /// <inheritdoc />
        public bool Equals(StreakMilestones other)
        {
            return Hit3Seeds == other.Hit3Seeds;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is StreakMilestones other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Hit3Seeds;
        }
    }

    /// <summary>
    /// Cifras de la Crecida y la Hoja escudo (06 §4.3; 05 §2.4; 04 §8) con los <b>defaults compilados</b> de la tabla única de
    /// 08 §9 y sus nombres exactos (nota <c>docs/dev/crecida-canasta.md</c> §4). Ninguna cifra sale de aquí:
    /// <c>StreakBasketConfigTests</c> las contrasta con 08 §9. Cuando exista <c>AF.RemoteConfig</c>, sus valores se copiarán en
    /// una instancia; hasta entonces se usa <see cref="Defaults"/>.
    /// </summary>
    public sealed class StreakConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static StreakConfig Defaults => new StreakConfig();

        /// <summary><c>biz_feature_win_streak</c>: kill switch de la Crecida (08 §9, fila <c>biz_feature_&lt;sistema&gt;</c>; Q-C11).</summary>
        [RemoteConfigKey("biz_feature_win_streak")]
        public bool BizFeatureWinStreak { get; set; } = true;

        /// <summary><c>streak_unlock_level</c>: debut de la Crecida y la Hoja escudo (06 §3 y §4.3; 04 §4).</summary>
        [RemoteConfigKey("streak_unlock_level")]
        public int StreakUnlockLevel { get; set; } = 25;

        /// <summary><c>streak_milestones</c>: escalera 3 → 300 · 6 → Canasta · 10 → Cuia extra + río crecido (05 §2.4).</summary>
        [RemoteConfigKey("streak_milestones")]
        public StreakMilestones StreakMilestones { get; set; } = StreakMilestones.Default;

        /// <summary><c>streak_hits_once_per_day</c>: cada hito se paga una vez por día local (05 §2.4).</summary>
        [RemoteConfigKey("streak_hits_once_per_day")]
        public bool StreakHitsOncePerDay { get; set; } = true;

        /// <summary><c>streak_daily_cap</c>: tope diario sobre las 300 del hito 3 y las 50 de la Canasta del hito 6 (05 §2.4).</summary>
        [RemoteConfigKey("streak_daily_cap")]
        public int StreakDailyCap { get; set; } = 600;

        /// <summary>
        /// <c>streak_shield_per_day</c>: Hojas escudo por día local. En la 1.0 hay una sola, con el bool <c>daily.shield_available</c>
        /// (Q-C6 (a)): un valor de 2, admitido por el rango 1-2 de 08 §9, no da una segunda hoja.
        /// </summary>
        [RemoteConfigKey("streak_shield_per_day")]
        public int StreakShieldPerDay { get; set; } = 1;

        /// <summary>
        /// <c>streak_restart_counts_as_fail</c>: false, fijado por la definición canónica de fallo (04 §5.2; 06, pregunta abierta 1):
        /// el reinicio voluntario nunca rompe la Crecida. <see cref="Validate"/> rechaza true.
        /// </summary>
        [RemoteConfigKey("streak_restart_counts_as_fail")]
        public bool StreakRestartCountsAsFail { get; set; }

        /// <summary><c>streak_reminder_hours</c>: push local «Crecida te espera» (06 §8.3). La Crecida no caduca: aquí solo se lee.</summary>
        [RemoteConfigKey("streak_reminder_hours")]
        public int StreakReminderHours { get; set; } = 36;

        /// <summary>Comprueba que los valores están en los rangos de 08 §9.</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            Require(StreakUnlockLevel >= 20 && StreakUnlockLevel <= 40, "streak_unlock_level debe estar en 20-40");
            Require(StreakMilestones.IsValid, "el hito 3 de streak_milestones debe estar en 200-300");
            Require(StreakDailyCap >= 300 && StreakDailyCap <= 900, "streak_daily_cap debe estar en 300-900");
            Require(StreakShieldPerDay >= 1 && StreakShieldPerDay <= 2, "streak_shield_per_day debe estar en 1-2");
            Require(!StreakRestartCountsAsFail, "streak_restart_counts_as_fail es false por 04 §5.2");
            Require(StreakReminderHours >= 24 && StreakReminderHours <= 48, "streak_reminder_hours debe estar en 24-48");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("StreakConfig: " + message + ".");
            }
        }
    }
}
