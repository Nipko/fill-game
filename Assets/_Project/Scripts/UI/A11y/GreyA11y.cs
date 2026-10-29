#nullable enable
using System;
using TMPro;
using UnityEngine.UI;

namespace AF.UI
{
    /// <summary>
    /// Lectura de TalkBack de un botón en gris por falta de semillas que ahora responde al toque (observación 6 de la jornada 5 y 2
    /// de la jornada 6; revisión de <c>tanda6/fx-j6-ui</c>): 02 §12 pide «etiqueta funcional + estado», así que se lee «Salto del
    /// Boto, 300, Te faltan 300 semillas» (sus textos a la vista y <c>stuck_rescue_missing</c>, con el patrón de lista
    /// <see cref="A11yText.Append"/>), no solo su etiqueta. Con saldo, la de siempre. La línea «Te faltan» suelta del modal, si la
    /// hay, se calla mientras el botón la lleva, para no leerla dos veces. La lectura se arma solo cuando TalkBack recorre la
    /// pantalla, nunca por fotograma.
    /// </summary>
    internal static class GreyA11y
    {
        /// <summary>
        /// Registra la lectura de <paramref name="button"/>: sus textos y, mientras <paramref name="missing"/> devuelva más de 0,
        /// «Te faltan {n} semillas»; y calla <paramref name="note"/> (la línea suelta con lo mismo) mientras tanto.
        /// </summary>
        public static void Register(Button button, Func<int> missing, TextMeshProUGUI? note = null)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (missing == null)
            {
                throw new ArgumentNullException(nameof(missing));
            }

            A11yLabels.Set(button, () => Reading(button, missing()));
            if (note != null)
            {
                A11yLabels.SetText(note, () => missing() > 0 ? string.Empty : note.text.Trim());
            }
        }

        /// <summary>La lectura de <paramref name="button"/> con <paramref name="missing"/> semillas que faltan (0: la de siempre).</summary>
        public static string Reading(Button button, int missing)
        {
            string text = A11yScanner.TextsLabel(button);
            return missing > 0 ? A11yText.Append(text, UiText.MissingSeeds(missing)) : text;
        }
    }
}
