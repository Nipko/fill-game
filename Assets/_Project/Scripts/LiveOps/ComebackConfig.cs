#nullable enable
using System;
using System.Globalization;
using AF.Levels.Json;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Regalo de regreso: <c>live_comeback_gift</c> = <c>{"days":7,"seeds":200,"extra_cuia":1,"refill_canoes":true}</c> (08 §9; 06 §9,
    /// fila «Dormido»: 200 semillas + 1 Cuia extra al inventario + canoas 5/5 + Hoja escudo activa). La Hoja escudo no está en el
    /// JSON: la repone el cambio de día de la Crecida (06 §4.3, <c>on_app_open</c>), que siempre ocurre tras 7 días fuera [P].
    /// </summary>
    public readonly struct ComebackGift : IEquatable<ComebackGift>
    {
        /// <summary>Crea el regalo.</summary>
        public ComebackGift(int days, int seeds, int extraCuia, bool refillCanoes)
        {
            Days = days;
            Seeds = seeds;
            ExtraCuia = extraCuia;
            RefillCanoes = refillCanoes;
        }

        /// <summary>El default de 08 §9.</summary>
        public static ComebackGift Default => new ComebackGift(7, 200, 1, true);

        /// <summary><c>days</c>: días sin sesión desde los que se da el regalo (el segmento «Dormido» de 06 §9 empieza en 7).</summary>
        public int Days { get; }

        /// <summary><c>seeds</c>: semillas.</summary>
        public int Seeds { get; }

        /// <summary><c>extra_cuia</c>: unidades de Cuia extra al inventario (<c>stock.extra_cuia</c>).</summary>
        public int ExtraCuia { get; }

        /// <summary><c>refill_canoes</c>: las canoas a su tope (5/5, <c>canoes_max</c>).</summary>
        public bool RefillCanoes { get; }

        /// <summary>Escribe el regalo con la sintaxis de 08 §9.</summary>
        public string Format()
        {
            return "{\"days\":" + Days.ToString(CultureInfo.InvariantCulture)
                + ",\"seeds\":" + Seeds.ToString(CultureInfo.InvariantCulture)
                + ",\"extra_cuia\":" + ExtraCuia.ToString(CultureInfo.InvariantCulture)
                + ",\"refill_canoes\":" + (RefillCanoes ? "true" : "false") + "}";
        }

        /// <summary>
        /// Lee el JSON de Remote Config sin lanzar. Exige las cuatro claves con su tipo (enteros ≥ 0, <c>days</c> ≥ 1); si falta
        /// alguna o sobra una desconocida, devuelve false y deja <paramref name="gift"/> = <see cref="Default"/>.
        /// </summary>
        public static bool TryParse(string? json, out ComebackGift gift)
        {
            gift = Default;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            JsonValue root;
            try
            {
                root = JsonParser.Parse(json!);
            }
            catch (FormatException)
            {
                return false;
            }

            if (root.Kind != JsonKind.Object || root.Members.Count != 4)
            {
                return false;
            }

            if (!TryInt(root, "days", 1, out int days) || !TryInt(root, "seeds", 0, out int seeds)
                || !TryInt(root, "extra_cuia", 0, out int extraCuia))
            {
                return false;
            }

            JsonValue? refill = root.Get("refill_canoes");
            if (refill == null || refill.Kind != JsonKind.Bool)
            {
                return false;
            }

            gift = new ComebackGift(days, seeds, extraCuia, refill.AsBool());
            return true;
        }

        /// <inheritdoc />
        public bool Equals(ComebackGift other)
        {
            return Days == other.Days && Seeds == other.Seeds && ExtraCuia == other.ExtraCuia && RefillCanoes == other.RefillCanoes;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ComebackGift other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ((Days * 397) ^ Seeds) * 31 + (ExtraCuia * 2) + (RefillCanoes ? 1 : 0);
        }

        private static bool TryInt(JsonValue root, string key, int min, out int value)
        {
            value = 0;
            JsonValue? node = root.Get(key);
            if (node == null || node.Kind != JsonKind.Number)
            {
                return false;
            }

            try
            {
                value = node.AsInt();
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }

            return value >= min;
        }
    }

    /// <summary>
    /// Cifras del Regalo de regreso (06 §9) con los defaults compilados de 08 §9 y su nombre exacto (<c>live_comeback_gift</c>;
    /// nunca el alias retirado <c>comeback_*</c>). <c>live_comeback_offer_enabled</c> (la Mochila de regreso, 1.1) no está aquí: es
    /// de la tienda (B5). Los umbrales de los segmentos «Perdido» (14 días) y «Churn» (30 días) son de la tabla de 06 §9, no de
    /// Remote Config (<see cref="ComebackRules"/>).
    /// </summary>
    public sealed class ComebackConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9.</summary>
        public static ComebackConfig Defaults => new ComebackConfig();

        /// <summary><c>live_comeback_gift</c>: el regalo y su umbral de días.</summary>
        [RemoteConfigKey("live_comeback_gift")]
        public ComebackGift LiveComebackGift { get; set; } = ComebackGift.Default;

        /// <summary>
        /// Anti-reloj (08 §10.1): <c>sec_clock_max_drift_h</c>, con su fuente única en <see cref="ClockSecurityConfig"/>. Con la hora
        /// del servidor y un desfase mayor, no se da el regalo en esa sesión [P].
        /// </summary>
        public ClockSecurityConfig ClockSecurity { get; } = new ClockSecurityConfig();

        /// <summary>Comprueba que los valores tienen sentido.</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            ComebackGift g = LiveComebackGift;
            if (g.Days < 1 || g.Days >= ComebackRules.LostFromDays || g.Seeds < 0 || g.ExtraCuia < 0)
            {
                throw new InvalidOperationException("ComebackConfig: live_comeback_gift inválido (days en 1-13, cantidades >= 0).");
            }

            ClockSecurity.Validate();
        }
    }
}
