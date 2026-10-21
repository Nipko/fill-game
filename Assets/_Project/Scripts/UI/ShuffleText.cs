#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la Pororoca · Barajar (bloque B2; 04 §2.4; glosario §5.1 y §10: «Pororoca», etiqueta «Barajar», ID
    /// <c>shuffle</c>): solo claves y sus argumentos con nombre, con <see cref="Loc"/>. Las claves nuevas aún <b>no</b> están en
    /// <c>strings.csv</c>: van en <c>docs/dev/traspaso/spark-enganches/B2-strings.csv</c>, todas <c>pendiente_asesor</c>, y el
    /// integrador las copia. No es un parcial de <c>UiText</c> porque los <c>UiText*.cs</c> son del escritorio (encargo del Spark
    /// §3.2), como <c>DailyText</c>.
    /// <para><b>Clave ausente:</b> <see cref="Loc.T(string)"/> devuelve «[clave]» en desarrollo y «» en release, sin excepciones;
    /// los botones y la hoja tienen tamaño fijo, así que se componen igual.</para>
    /// </summary>
    public static class ShuffleText
    {
        /// <summary>Claves de la Pororoca (las nuevas, en <c>B2-strings.csv</c>).</summary>
        public static class Keys
        {
            /// <summary>Etiqueta funcional de la ranura (glosario §5.1 y §10: «Barajar»; 07 §7.4).</summary>
            public const string Label = "shuffle";

            /// <summary>Nombre del comodín (glosario §5.1: «Pororoca»): cabecera de la hoja y de la tarjeta del 30.</summary>
            public const string Name = "shuffle_name";

            /// <summary>Lema del comodín (glosario §5.1; 04 §2.4): cuerpo de la tarjeta del 30.</summary>
            public const string Tagline = "shuffle_tagline";

            /// <summary>Hoja de confirmación de una línea con <c>{price}</c> (04 §2.4).</summary>
            public const string Confirm = "shuffle_confirm";

            /// <summary>Botón que confirma la hoja [P].</summary>
            public const string ConfirmYes = "shuffle_confirm_yes";

            /// <summary>La ola no encontró tablero (04 §2.4): no se cobra.</summary>
            public const string Failed = "shuffle_failed";

            /// <summary>Menos de 2 colores movibles (04 §2.4 b).</summary>
            public const string LowWater = "shuffle_low_water";

            /// <summary>Lluvia o Piraña en el nivel (04 §2.4 d) [P].</summary>
            public const string Unsupported = "shuffle_unsupported";
        }

        /// <summary>
        /// Las claves nuevas (las 8 de <c>B2-strings.csv</c>), para que <c>LocTablesTests</c> las cuente como usadas cuando el
        /// integrador las copie a <c>strings.csv</c> (como <c>DailyText.NewKeys</c>).
        /// </summary>
        public static string[] AllKeys => new[]
        {
            Keys.Label, Keys.Confirm, Keys.ConfirmYes, Keys.Failed, Keys.LowWater, Keys.Name, Keys.Tagline, Keys.Unsupported,
        };

        /// <summary>«Barajar».</summary>
        public static string Label => Loc.T(Keys.Label);

        /// <summary>«Pororoca».</summary>
        public static string Name => Loc.T(Keys.Name);

        /// <summary>«La ola remueve el río sin tocar las cuias llenas.»</summary>
        public static string Tagline => Loc.T(Keys.Tagline);

        /// <summary>«La ola remueve el río sin tocar las cuias llenas. {price} semillas. ¿Seguir?» (04 §2.4).</summary>
        public static string Confirm(int price)
        {
            return Loc.T(Keys.Confirm, LocArgs.Of("price", price));
        }

        /// <summary>«Seguir» [P].</summary>
        public static string ConfirmYes => Loc.T(Keys.ConfirmYes);

        /// <summary>«La ola no pudo remover el río» (04 §2.4).</summary>
        public static string Failed => Loc.T(Keys.Failed);

        /// <summary>«Hay poca agua libre para la ola» (04 §2.4 b).</summary>
        public static string LowWater => Loc.T(Keys.LowWater);

        /// <summary>«La ola no llega con la lluvia ni las pirañas» [P] (04 §2.4 d).</summary>
        public static string Unsupported => Loc.T(Keys.Unsupported);
    }
}
