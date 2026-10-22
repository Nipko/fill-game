#nullable enable
using AF.GameFlow;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la tarea B11 (tutorial, Sin canoas y contenido; 02 §13, 04 §2.7, §7.1 y §7.3, 06 §2.2), de <c>strings.csv</c>
    /// (parche <c>B11-loc.diff</c>; <c>LocTablesTests.UiTextKeys</c> los registra con <see cref="AllKeys"/>). Aparte de
    /// <see cref="UiText"/> para que la rama compile y pase sus pruebas sin el parche de textos, como <c>MarketText</c>.
    /// </summary>
    internal static class TutorialTipsText
    {
        /// <summary>Las claves de B11, una copia nueva en cada llamada.</summary>
        public static string[] AllKeys => new[]
        {
            Keys.TutorialCanoes, TutorialTips.RestartFreeCanoeKey, TutorialTips.RestartFreeKey, TutorialTips.DoubleHarvestKey,
            Keys.NoCanoesMap,
        };

        /// <summary>
        /// <c>tutorial_canoes</c> [P]: la línea de la tarjeta de las Canoas del 31 (04 §7.1; D-033, Q-E4 (a): «Canoas: si te rindes
        /// o sales del nivel, pierdes una. Vuelven solas cada 25 minutos.»).
        /// </summary>
        public static string Canoes => Loc.T(Keys.TutorialCanoes);

        /// <summary><c>no_canoes_map</c>: «Volver al Mapa del río» de Sin canoas (04 §7.3, wireframe).</summary>
        public static string NoCanoesMap => Loc.T(Keys.NoCanoesMap);

        /// <summary>El texto de la clave <paramref name="key"/> de una ayuda de <see cref="TutorialTips"/>.</summary>
        public static string Tip(string key)
        {
            return Loc.T(key);
        }

        /// <summary>Claves de <c>strings.csv</c> de esta parte (las de los tooltips están en <see cref="TutorialTips"/>).</summary>
        public static class Keys
        {
            /// <summary><c>tutorial_canoes</c> [P] (B11; D-033, Q-E4).</summary>
            public const string TutorialCanoes = "tutorial_canoes";

            /// <summary><c>no_canoes_map</c> (B11; 04 §7.3).</summary>
            public const string NoCanoesMap = "no_canoes_map";
        }
    }
}
