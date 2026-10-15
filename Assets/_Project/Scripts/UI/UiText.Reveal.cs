#nullable enable
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la Luciérnaga · Revelar (tarea T14 del paso 1; 04 §2.5; glosario §5.1 y §10), de <c>strings.csv</c>. Parcial de
    /// <see cref="UiText"/> (arquitectura del paso 1, §9). Nombran fauna: pendientes de revisión del asesor cultural (07 §8.2).
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>reveal</c>: etiqueta funcional de la Luciérnaga en la bandeja (glosario §5.1 y §10: «Revelar»).</summary>
        public static string RevealLabel => Loc.T(Keys.Reveal);

        /// <summary>
        /// Aviso del regalo del estreno de Aguas turbias (04 §2.5: «regala 1 Luciérnaga al stock»; nivel 100) [P]: «+1
        /// Luciérnaga» (<c>reveal_gift_toast</c>). Sale al cerrar la tarjeta previa, si la hay, o al empezar la intro.
        /// </summary>
        public static string RevealGiftToast => Loc.T(Keys.RevealGiftToast);

        /// <summary>
        /// Aviso de Revelar sin stock, sin saldo y sin video (<c>rv_reveal</c>) [P]: «Te faltan {n} semillas», el mismo texto
        /// que el rescate (<c>stuck_rescue_missing</c>; <see cref="MissingSeeds"/>).
        /// </summary>
        public static string RevealMissing(int seeds)
        {
            return MissingSeeds(seeds);
        }
    }
}
