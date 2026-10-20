#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la interfaz de la Crecida, la Hoja escudo y la Canasta (tarea B1b de la tanda 5; 02 §5-§7; 04 §4, §6.5 y §8; 06
    /// §4.3-§4.4 y §4.7; D-033, panel único de la victoria), de <c>strings.csv</c>. Parcial de <see cref="UiText"/>. Las cifras
    /// van con marcadores con nombre y el formato de la región (<see cref="Loc"/>); se arman al colocar, nunca por fotograma.
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>tutorial_streak</c> [P]: la línea de la tarjeta de la Crecida del 25 (04 §4; glosario §7.2, la frase del sistema).</summary>
        public static string TutorialStreak => Loc.T(Keys.TutorialStreak);

        /// <summary><c>shield_toast</c> (04 §6.5 y §8): el aviso del primer fallo duro que cubre la Hoja escudo.</summary>
        public static string ShieldToast => Loc.T(Keys.ShieldToast);

        /// <summary><c>stuck_shield_ready</c> (04 §6.5): la línea de la Hoja escudo en Aguas estancadas, con la hoja disponible.</summary>
        public static string StuckShieldReady => Loc.T(Keys.StuckShieldReady);

        /// <summary>
        /// <c>stuck_shield_used</c> (04 §6.5): la línea de Aguas estancadas sin Hoja escudo hoy, con la barra que se perdería
        /// (<paramref name="wins"/>, sin separador).
        /// </summary>
        public static string StuckShieldUsed(int wins)
        {
            return Format(Keys.StuckShieldUsed, LocArgs.OfDigits("n", NonNegative(wins)));
        }

        /// <summary><c>streak_bar</c> [P]: «Crecida {n}/10», la réplica de la barra en la victoria (06 §4.3, «HUD»).</summary>
        public static string StreakBar(int wins)
        {
            return Format(Keys.StreakBar, LocArgs.OfDigits("n", NonNegative(wins)));
        }

        /// <summary>
        /// <c>basket_bar</c> [P]: «Canasta · {sitio} k/10» (02 §6, fila «Barras de evento», con D-013), con la parada a la que se
        /// llega al cerrar el tramo; sin nombre de parada, <c>basket_bar_plain</c> («Canasta k/10»).
        /// </summary>
        public static string BasketBar(string? place, int count)
        {
            return string.IsNullOrEmpty(place)
                ? Format(Keys.BasketBarPlain, LocArgs.OfDigits("n", NonNegative(count)))
                : Format(Keys.BasketBar, LocArgs.OfDigits("n", NonNegative(count)).And("place", place!));
        }

        /// <summary><c>victory_bar_seeds</c> [P]: «+{n}», las semillas de un hito o de una Canasta junto a la semilla.</summary>
        public static string VictoryBarSeeds(int seeds)
        {
            return Format(Keys.VictoryBarSeeds, LocArgs.Of("n", NonNegative(seeds)));
        }

        /// <summary><c>victory_bar_units</c> [P]: «×{n}», las unidades de un comodín junto a su icono.</summary>
        public static string VictoryBarUnits(int units)
        {
            return Format(Keys.VictoryBarUnits, LocArgs.OfDigits("n", NonNegative(units)));
        }

        /// <summary>Claves de la Crecida y la Canasta (parcial de <see cref="UiText.Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>basket_bar</c> [P] (B1b).</summary>
            public const string BasketBar = "basket_bar";

            /// <summary><c>basket_bar_plain</c> [P] (B1b).</summary>
            public const string BasketBarPlain = "basket_bar_plain";

            /// <summary><c>shield_toast</c> (04 §6.5).</summary>
            public const string ShieldToast = "shield_toast";

            /// <summary><c>streak_bar</c> [P] (B1b).</summary>
            public const string StreakBar = "streak_bar";

            /// <summary><c>stuck_shield_ready</c> (04 §6.5).</summary>
            public const string StuckShieldReady = "stuck_shield_ready";

            /// <summary><c>stuck_shield_used</c> (04 §6.5).</summary>
            public const string StuckShieldUsed = "stuck_shield_used";

            /// <summary><c>tutorial_streak</c> [P] (B1b).</summary>
            public const string TutorialStreak = "tutorial_streak";

            /// <summary><c>victory_bar_seeds</c> [P] (B1b).</summary>
            public const string VictoryBarSeeds = "victory_bar_seeds";

            /// <summary><c>victory_bar_units</c> [P] (B1b).</summary>
            public const string VictoryBarUnits = "victory_bar_units";
        }
    }
}
