#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos del Salto del Boto (bloque B3; 04 §2.6 y §6.5; glosario §5.1): solo claves y sus argumentos con nombre, con
    /// <see cref="Loc"/>. Las claves nuevas aún <b>no</b> están en <c>strings.csv</c>: van en
    /// <c>docs/dev/traspaso/spark-enganches/B3-strings.csv</c> (estado <c>pendiente_asesor</c>: Rosa es cultural) y las copia el
    /// integrador. No es un parcial de <c>UiText</c> porque los <c>UiText*.cs</c> están reservados (encargo §3.2), como
    /// <see cref="DailyText"/>. Clave ausente: «[clave]» en desarrollo y «» en release (<see cref="Loc.T(string)"/>), sin
    /// excepciones; los botones tienen su tamaño fijo y funcionan igual.
    /// </summary>
    public static class SkipText
    {
        /// <summary>Claves (las nuevas, en <c>B3-strings.csv</c>).</summary>
        public static class Keys
        {
            /// <summary>«Salto del Boto»: etiqueta de la opción en Aguas estancadas y en la pausa (04 §6.5 <c>stuck_skip</c>, sin la cifra: el precio va en su etiqueta, como «Continuar: +1 cuia»).</summary>
            public const string Option = "stuck_skip";

            /// <summary>«Rosa te lleva río arriba, al siguiente nivel» (04 §2.6, D-013 Q13-A): bajo la opción y en la confirmación.</summary>
            public const string Tagline = "skip_tagline";

            /// <summary>«¿Saltar el nivel?» [P]: título de la confirmación de una línea (04 §2.6, «Confirmación de una línea»).</summary>
            public const string ConfirmTitle = "skip_confirm_title";

            /// <summary>«Perderás la Cuia extra de este nivel» [P] (04 §2.6 c: aviso en la confirmación).</summary>
            public const string ConfirmExtraCuia = "skip_confirm_extra_cuia";

            /// <summary>«Saltar nivel»: CTA de la confirmación, con la clave del glosario §10 (<c>skip</c>); el precio va en su etiqueta.</summary>
            public const string ConfirmCta = "skip";

            /// <summary>«Rosa puede llevarte río arriba · {price}»: banner de la entrada del nivel (04 §2.6, «Inventario y HUD»).</summary>
            public const string Banner = "skip_banner";
        }

        /// <summary>«Salto del Boto».</summary>
        public static string Option => Loc.T(Keys.Option);

        /// <summary>«Rosa te lleva río arriba, al siguiente nivel».</summary>
        public static string Tagline => Loc.T(Keys.Tagline);

        /// <summary>«¿Saltar el nivel?».</summary>
        public static string ConfirmTitle => Loc.T(Keys.ConfirmTitle);

        /// <summary>«Perderás la Cuia extra de este nivel».</summary>
        public static string ConfirmExtraCuia => Loc.T(Keys.ConfirmExtraCuia);

        /// <summary>«Saltar nivel».</summary>
        public static string ConfirmCta => Loc.T(Keys.ConfirmCta);

        /// <summary>«Rosa puede llevarte río arriba · {price}», con el precio en semillas.</summary>
        public static string Banner(int price)
        {
            return Loc.T(Keys.Banner, LocArgs.Of("price", price < 0 ? 0 : price));
        }
    }
}
