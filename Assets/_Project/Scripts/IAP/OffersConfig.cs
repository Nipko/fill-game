#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Platform;

namespace AF.IAP
{
    /// <summary>
    /// Cifras de las ofertas contextuales y del Cántaro (bloque B5; 05 §4.1 y §5) con los <b>defaults compilados</b> de la tabla
    /// única de 08 §9 y sus nombres exactos. Lo que 05 fija sin clave (recordatorios de la Mochila en el 15 y el 19, Sin anuncios en
    /// el Mercado desde el 22, una tarjeta al día y solo Mercado tras 3 rechazos, la tarjeta de skin desde el mundo 3) está en
    /// <see cref="OfferRules"/>, citando 05. El tope del Cántaro y su compra mínima siguen en <see cref="StoreConfig"/>
    /// (<c>eco_piggy_cap</c> y <c>eco_piggy_min_buy</c>, ya enlazadas por B7).
    /// </summary>
    public sealed class OffersConfig
    {
        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static OffersConfig Defaults => new OffersConfig();

        /// <summary>
        /// <c>eco_starter_window_levels</c> (08 §9: json <c>[10,20]</c>): la Mochila de inicio se vende desde el primer nivel y
        /// desaparece al pasar el segundo (05 §4.1: «en el Mercado desde el nivel 10 … desaparece al pasar el nivel 20»).
        /// </summary>
        [RemoteConfigKey("eco_starter_window_levels")]
        public IReadOnlyList<int> EcoStarterWindowLevels { get; set; } = new[] { 10, 20 };

        /// <summary><c>eco_starter_timer_h</c> (08 §9: 48): horas de la Mochila desde la primera vista (05 §4.1 y §5).</summary>
        [RemoteConfigKey("eco_starter_timer_h")]
        public int EcoStarterTimerH { get; set; } = 48;

        /// <summary>
        /// <c>eco_starter_first_show</c> (08 §9: <c>"L12_or_session2"</c>): la primera tarjeta sale en la primera victoria con
        /// nivel ≥ 12 dentro de una sesión ≥ 2 (05 §4.1; 06 §2.1, regla 1). Se lee con <see cref="StarterFirstShow"/>.
        /// </summary>
        [RemoteConfigKey("eco_starter_first_show")]
        public string EcoStarterFirstShow { get; set; } = "L12_or_session2";

        /// <summary>
        /// <c>eco_noads_card_trigger</c> (08 §9: <c>"3_interstitials_or_d2"</c>): la tarjeta Sin anuncios sale tras el 3.er
        /// intersticial visto o en el día 2, lo primero (05 §5). Se lee con <see cref="NoAdsTrigger"/>.
        /// </summary>
        [RemoteConfigKey("eco_noads_card_trigger")]
        public string EcoNoadsCardTrigger { get; set; } = "3_interstitials_or_d2";

        /// <summary><c>eco_piggy_from_level</c> (08 §9: 20, rango 15-30): el Cántaro aparece y empieza a llenarse en este nivel (05 §4.1).</summary>
        [RemoteConfigKey("eco_piggy_from_level")]
        public int EcoPiggyFromLevel { get; set; } = 20;

        /// <summary>
        /// <c>eco_piggy_fill</c> (08 §9: json <c>[15,30,40]</c>): semillas que suma al Cántaro una victoria normal, Difícil y Muy
        /// difícil (05 §4.1).
        /// </summary>
        [RemoteConfigKey("eco_piggy_fill")]
        public IReadOnlyList<int> EcoPiggyFill { get; set; } = new[] { 15, 30, 40 };

        /// <summary>Primer nivel de la ventana de la Mochila (<c>eco_starter_window_levels[0]</c>).</summary>
        public int StarterWindowFrom => EcoStarterWindowLevels[0];

        /// <summary>Último nivel de la ventana de la Mochila (<c>eco_starter_window_levels[1]</c>): al ganarlo, desaparece.</summary>
        public int StarterWindowTo => EcoStarterWindowLevels[1];

        /// <summary><c>eco_starter_first_show</c> leída: nivel mínimo y sesión mínima de la primera tarjeta.</summary>
        public FirstShowRule StarterFirstShow => FirstShowRule.Parse(EcoStarterFirstShow);

        /// <summary><c>eco_noads_card_trigger</c> leída: intersticiales vistos o día desde la instalación.</summary>
        public NoAdsTriggerRule NoAdsTrigger => NoAdsTriggerRule.Parse(EcoNoadsCardTrigger);

        /// <summary>Comprueba los valores (rangos de 08 §9 donde los hay; forma de los json y de las cadenas en el resto).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            IReadOnlyList<int> window = EcoStarterWindowLevels;
            Require(window != null && window.Count == 2, "eco_starter_window_levels lleva 2 niveles [desde, hasta]");
            Require(window![0] >= 1 && window[0] <= window[1], "eco_starter_window_levels: 1 ≤ desde ≤ hasta");
            Require(EcoStarterTimerH > 0, "eco_starter_timer_h > 0");
            Require(FirstShowRule.TryParse(EcoStarterFirstShow, out _), "eco_starter_first_show con la forma L{nivel}_or_session{n}");
            Require(NoAdsTriggerRule.TryParse(EcoNoadsCardTrigger, out _), "eco_noads_card_trigger con la forma {n}_interstitials_or_d{día}");
            Require(EcoPiggyFromLevel >= 15 && EcoPiggyFromLevel <= 30, "eco_piggy_from_level debe estar en 15-30");
            IReadOnlyList<int> fill = EcoPiggyFill;
            Require(fill != null && fill.Count == 3, "eco_piggy_fill lleva 3 cifras [normal, Difícil, Muy difícil]");
            Require(fill![0] > 0 && fill[1] > 0 && fill[2] > 0, "eco_piggy_fill: cifras > 0");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("OffersConfig: " + message + ".");
            }
        }
    }

    /// <summary>
    /// <c>eco_starter_first_show</c> leída (<c>L{nivel}_or_session{n}</c>). El canon (05 §4.1; 06 §2.1, regla 1, y §2.2, fila 12)
    /// pide las dos condiciones a la vez: «primera victoria con nivel ≥ 12 dentro de una sesión ≥ 2», nunca en la primera sesión.
    /// </summary>
    public readonly struct FirstShowRule
    {
        private const string LevelPrefix = "L";
        private const string SessionInfix = "_or_session";

        /// <summary>Crea la regla.</summary>
        public FirstShowRule(int minLevel, int minSession)
        {
            MinLevel = minLevel;
            MinSession = minSession;
        }

        /// <summary>Nivel ganado mínimo (12).</summary>
        public int MinLevel { get; }

        /// <summary>Sesión mínima (2; Q-M4 (a) de D-033: arranque en frío o vuelta tras ≥ 30 min fuera).</summary>
        public int MinSession { get; }

        /// <summary>Lee <paramref name="text"/>.</summary>
        /// <exception cref="FormatException">No tiene la forma <c>L{nivel}_or_session{n}</c>.</exception>
        public static FirstShowRule Parse(string text)
        {
            if (!TryParse(text, out FirstShowRule rule))
            {
                throw new FormatException("eco_starter_first_show no tiene la forma L{nivel}_or_session{n}: " + text);
            }

            return rule;
        }

        /// <summary>Intenta leer <paramref name="text"/>.</summary>
        public static bool TryParse(string? text, out FirstShowRule rule)
        {
            rule = default;
            if (text == null || !text.StartsWith(LevelPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            int infix = text.IndexOf(SessionInfix, StringComparison.Ordinal);
            if (infix <= LevelPrefix.Length)
            {
                return false;
            }

            if (!TryPositive(text.Substring(LevelPrefix.Length, infix - LevelPrefix.Length), out int level)
                || !TryPositive(text.Substring(infix + SessionInfix.Length), out int session))
            {
                return false;
            }

            rule = new FirstShowRule(level, session);
            return true;
        }

        internal static bool TryPositive(string digits, out int value)
        {
            value = 0;
            if (digits.Length == 0 || digits.Length > 6)
            {
                return false;
            }

            for (int i = 0; i < digits.Length; i++)
            {
                if (digits[i] < '0' || digits[i] > '9')
                {
                    return false;
                }
            }

            return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;
        }
    }

    /// <summary>
    /// <c>eco_noads_card_trigger</c> leída (<c>{n}_interstitials_or_d{día}</c>): la tarjeta Sin anuncios sale tras el n-ésimo
    /// intersticial visto o en el día <c>{día}</c> desde la instalación (D1 = el día de la instalación), lo primero (05 §5).
    /// </summary>
    public readonly struct NoAdsTriggerRule
    {
        private const string Infix = "_interstitials_or_d";

        /// <summary>Crea la regla.</summary>
        public NoAdsTriggerRule(int interstitials, int day)
        {
            Interstitials = interstitials;
            Day = day;
        }

        /// <summary>Intersticiales vistos (3).</summary>
        public int Interstitials { get; }

        /// <summary>Día desde la instalación, con D1 el de la instalación (2).</summary>
        public int Day { get; }

        /// <summary>Lee <paramref name="text"/>.</summary>
        /// <exception cref="FormatException">No tiene la forma <c>{n}_interstitials_or_d{día}</c>.</exception>
        public static NoAdsTriggerRule Parse(string text)
        {
            if (!TryParse(text, out NoAdsTriggerRule rule))
            {
                throw new FormatException("eco_noads_card_trigger no tiene la forma {n}_interstitials_or_d{día}: " + text);
            }

            return rule;
        }

        /// <summary>Intenta leer <paramref name="text"/>.</summary>
        public static bool TryParse(string? text, out NoAdsTriggerRule rule)
        {
            rule = default;
            if (text == null)
            {
                return false;
            }

            int infix = text.IndexOf(Infix, StringComparison.Ordinal);
            if (infix <= 0)
            {
                return false;
            }

            if (!FirstShowRule.TryPositive(text.Substring(0, infix), out int count)
                || !FirstShowRule.TryPositive(text.Substring(infix + Infix.Length), out int day))
            {
                return false;
            }

            rule = new NoAdsTriggerRule(count, day);
            return true;
        }
    }

    /// <summary>Lo que 05 fija para las ofertas sin clave en 08 §9 (no son Remote Config).</summary>
    public static class OfferRules
    {
        /// <summary>Recordatorios de la Mochila de inicio: en el nivel 15 y en el 19 (05 §5, fila Mochila, «recordatorio en el nivel 15 y 19»).</summary>
        public static IReadOnlyList<int> StarterReminderLevels { get; } = new[] { 15, 19 };

        /// <summary>Sin anuncios entra en el Mercado en este nivel (05 §4.1: «Mercado desde el nivel 22»).</summary>
        public const int NoAdsMarketFromLevel = 22;

        /// <summary>Tarjetas Sin anuncios al día (05 §4.1 y §5: «1 tarjeta/día»).</summary>
        public const int NoAdsCardsPerDay = 1;

        /// <summary>Tras estos rechazos de la tarjeta, Sin anuncios solo se ve en el Mercado (05 §5: «tras 3 rechazos, solo Mercado»).</summary>
        public const int NoAdsDeclinesToStop = 3;

        /// <summary>
        /// La tarjeta propia de la skin del mundo sale desde el mundo 3, nivel 41 (05 §4.1 y §5; 06 §4.5; D-015): en el mundo 2,
        /// solo en el Mercado.
        /// </summary>
        public const int SkinCardFromLevel = 41;

        /// <summary>
        /// Interruptor de la venta de skins (fila del Mercado y tarjeta desde el 41) [P]: apagado mientras falte el arte final de las
        /// skins (Q-E6 (a) de D-033: «el Mercado abre ya … sin arte de skins»; pregunta Q-B5-1 de <c>docs/dev/mercado.md</c>). La
        /// elección de la skin puesta (pestaña Cuias) no depende de él.
        /// </summary>
        public const bool SkinSalesEnabledDefault = false;
    }
}
