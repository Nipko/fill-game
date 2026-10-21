#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la Bitácora del río (B4; 06 §2.3 y §4.2; D-033, añadido del 2026-10-20): solo claves y sus argumentos con nombre,
    /// con <see cref="Loc"/>. Las claves nuevas aún <b>no</b> están en <c>strings.csv</c>: van en
    /// <c>docs/dev/traspaso/spark-enganches/B4-strings.csv</c> y el integrador las copia. No es un parcial de <c>UiText</c> porque
    /// los <c>UiText*.cs</c> están reservados (encargo §3.2). Sin la fila en la tabla, <see cref="Loc.T(string)"/> da «[clave]» en
    /// desarrollo y «» en release: la libreta se compone igual (los botones y las casillas tienen tamaño fijo).
    /// <para>Nada se concatena: cada premio sale en filas separadas (icono + una cadena de la tabla), y las cifras con los
    /// marcadores de la tabla (<c>{n}</c>, <c>{k}</c>, <c>{c}</c>).</para>
    /// </summary>
    public static class LogbookText
    {
        /// <summary>Claves (todas nuevas, en <c>B4-strings.csv</c>).</summary>
        public static class Keys
        {
            /// <summary>Nombre del sistema (glosario §10, <c>login_calendar</c>): cabecera de la libreta y de la tarjeta del inicio.</summary>
            public const string Title = "logbook_title";

            /// <summary>«Cada día que juegues, sella una página» (06 §2.3, pantalla 2 del nivel 10).</summary>
            public const string Intro = "logbook_intro";

            /// <summary>«Página {k} de 30 · Ciclo {c}» (06 §4.2 «Pantalla»).</summary>
            public const string Footer = "logbook_footer";

            /// <summary>Estado: hay que terminar un nivel hoy para sellar.</summary>
            public const string StatusPlay = "logbook_status_play";

            /// <summary>Estado: la casilla de hoy se puede sellar (toca la que pulsa).</summary>
            public const string StatusReady = "logbook_status_ready";

            /// <summary>Estado: elige uno de los tres premios.</summary>
            public const string StatusChoose = "logbook_status_choose";

            /// <summary>Estado: hoy ya se selló; la siguiente, mañana.</summary>
            public const string StatusSealed = "logbook_status_sealed";

            /// <summary>Estado de la tarjeta del inicio: la página de hoy espera (con el punto).</summary>
            public const string CardReady = "logbook_card_ready";

            /// <summary>Estado de la tarjeta del inicio: hay que jugar un nivel hoy.</summary>
            public const string CardPlay = "logbook_card_play";

            /// <summary>Estado de la tarjeta del inicio: sellada hoy (06 §2.3, pantalla 4: «Bitácora (sellada hoy)»).</summary>
            public const string CardSealed = "logbook_card_sealed";

            /// <summary>«+{n}» semillas, junto al icono de semilla.</summary>
            public const string ItemSeeds = "logbook_item_seeds";

            /// <summary>«Marea ×{n}».</summary>
            public const string ItemUndo = "logbook_item_undo";

            /// <summary>«Morpho ×{n}».</summary>
            public const string ItemHint = "logbook_item_hint";

            /// <summary>«Luciérnaga ×{n}».</summary>
            public const string ItemReveal = "logbook_item_reveal";

            /// <summary>«Cuia extra ×{n}».</summary>
            public const string ItemExtraCuia = "logbook_item_extra_cuia";

            /// <summary>«+ Canasta» (fija en los días 7 y 21).</summary>
            public const string ItemBasket = "logbook_item_basket";

            /// <summary>«+ Canasta grande» (fija en el día 30).</summary>
            public const string ItemGrandBasket = "logbook_item_grand_basket";

            /// <summary>«+ Marco de país» (fijo en el día 30).</summary>
            public const string ItemFrame = "logbook_item_frame";

            /// <summary>Botón de la secuencia del 10: «Seguir» (06 §2.3: «Un solo botón "Seguir"»).</summary>
            public const string Continue = "logbook_continue";

            /// <summary>Botón para cerrar la libreta (la casilla, si no se eligió, queda pendiente).</summary>
            public const string Close = "logbook_close";
        }

        /// <summary>Las claves nuevas (filas de <c>B4-strings.csv</c>).</summary>
        public static readonly string[] NewKeys =
        {
            Keys.Title, Keys.Intro, Keys.Footer, Keys.StatusPlay, Keys.StatusReady, Keys.StatusChoose, Keys.StatusSealed,
            Keys.CardReady, Keys.CardPlay, Keys.CardSealed, Keys.ItemSeeds, Keys.ItemUndo, Keys.ItemHint, Keys.ItemReveal,
            Keys.ItemExtraCuia, Keys.ItemBasket, Keys.ItemGrandBasket, Keys.ItemFrame, Keys.Continue, Keys.Close,
        };

        /// <summary>«Bitácora del río».</summary>
        public static string Title => Loc.T(Keys.Title);

        /// <summary>«Cada día que juegues, sella una página».</summary>
        public static string Intro => Loc.T(Keys.Intro);

        /// <summary>«Página {k} de 30 · Ciclo {c}».</summary>
        public static string Footer(int page, int cycle)
        {
            return Loc.T(Keys.Footer, LocArgs.OfDigits("k", page).AndDigits("c", cycle));
        }

        /// <summary>«+{n}» semillas.</summary>
        public static string Seeds(int seeds)
        {
            return Loc.T(Keys.ItemSeeds, LocArgs.Of("n", seeds));
        }

        /// <summary>«{comodín} ×{n}» con la clave de <paramref name="key"/>.</summary>
        public static string Units(string key, int units)
        {
            return Loc.T(key, LocArgs.OfDigits("n", units));
        }

        /// <summary>Texto sin argumentos.</summary>
        public static string Plain(string key)
        {
            return Loc.T(key);
        }
    }
}
