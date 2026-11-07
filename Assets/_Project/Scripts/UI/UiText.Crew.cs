#nullable enable
using AF.GameFlow;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos del Mapa del río v2 de M2-4 (D-069): la tripulación (propuesta del Mapa v2 §6.2: el chip «¡Se une al viaje! · {especie}»
    /// y su etiqueta de TalkBack, una frase completa por especie) y «Faltan N» dentro del rótulo de la parada siguiente (respuesta del
    /// dueño a la pregunta 2 de M2-3). Pendientes de la revisión de Gemini (D-054).
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>map_crew_join_toucan</c> o <c>map_crew_join_tucuxi</c> [P]: el chip de 2 s cuando el compañero se une.</summary>
        public static string MapCrewJoin(CrewKind kind)
        {
            return kind == CrewKind.Tucuxi ? Loc.T(Keys.MapCrewJoinTucuxi) : Loc.T(Keys.MapCrewJoinToucan);
        }

        /// <summary><c>a11y_map_crew_join_toucan</c> o <c>a11y_map_crew_join_tucuxi</c> [P]: lo que anuncia TalkBack («Se une al viaje: tucán»).</summary>
        public static string A11yMapCrewJoin(CrewKind kind)
        {
            return kind == CrewKind.Tucuxi ? Loc.T(Keys.A11yMapCrewJoinTucuxi) : Loc.T(Keys.A11yMapCrewJoinToucan);
        }

        /// <summary>
        /// <c>map_stop_label_remaining</c> [P]: el rótulo de la parada siguiente con lo que falta, en dos líneas («Tefé» y «faltan 5
        /// niveles»; el salto lo decide la cadena). <paramref name="levels"/> con las cifras del idioma y del país; un negativo, 0.
        /// </summary>
        public static string MapStopLabelRemaining(string place, int levels)
        {
            return Format(Keys.MapStopLabelRemaining, LocArgs.Of("n", NonNegative(levels)).And("place", place));
        }

        public static partial class Keys
        {
            /// <summary><c>map_crew_join_toucan</c> [P] (D-069, M2-4).</summary>
            public const string MapCrewJoinToucan = "map_crew_join_toucan";

            /// <summary><c>map_crew_join_tucuxi</c> [P] (D-069, M2-4).</summary>
            public const string MapCrewJoinTucuxi = "map_crew_join_tucuxi";

            /// <summary><c>a11y_map_crew_join_toucan</c> [P] (D-069, M2-4).</summary>
            public const string A11yMapCrewJoinToucan = "a11y_map_crew_join_toucan";

            /// <summary><c>a11y_map_crew_join_tucuxi</c> [P] (D-069, M2-4).</summary>
            public const string A11yMapCrewJoinTucuxi = "a11y_map_crew_join_tucuxi";

            /// <summary><c>map_stop_label_remaining</c> [P] (D-069, pregunta 2 de M2-3).</summary>
            public const string MapStopLabelRemaining = "map_stop_label_remaining";
        }
    }
}
