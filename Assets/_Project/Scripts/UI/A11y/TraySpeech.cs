#nullable enable
using AF.Boosters;

namespace AF.UI
{
    /// <summary>
    /// Descripción de los botones de la barra de comodines para el lector de pantalla (bloque B9; 02 §12: «todo botón lleva
    /// <c>contentDescription</c> con etiqueta funcional + estado ("Deshacer, 2 restantes")»). La etiqueta funcional es la que se ve
    /// en la ranura (glosario §0.2 y §5.1); el estado son los usos que quedan sin pagar (el cupo gratis más el stock, 04 §1.1), del
    /// mismo estado que pinta la bandeja (<c>LevelFlow.UndoStatus</c>, <c>ExtraCuia</c>, <c>Hint</c> y <c>Shuffle</c>). Sin usos
    /// contados (ilimitada, o con precio o video), solo la etiqueta [P]: el precio se ve y lo puede leer el lector si la ranura se
    /// describe con sus textos (Q-B9-9). «Desactivado» lo dice el propio lector (el botón gris), en el idioma del teléfono.
    /// </summary>
    public static class TraySpeech
    {
        /// <summary>Marea (04 §2.1): «Deshacer, 2 restantes» con cupo o stock; ilimitada o sin nada, «Deshacer».</summary>
        public static string Undo(string label, UndoStatus status)
        {
            int left = status.FreeLeft + status.Stock;
            return status.Unlimited || left <= 0 ? label : A11yText.ButtonRemaining(label, left);
        }

        /// <summary>Cuia extra (04 §2.2): con stock, «+1 cuia, 1 restante»; si no, la etiqueta.</summary>
        public static string Extra(string label, ExtraCuiaOffer offer)
        {
            return offer.FromStock ? A11yText.ButtonRemaining(label, offer.Stock) : label;
        }

        /// <summary>Pista · Morpho (04 §2.3): con stock, «Pista, 2 restantes»; si no, la etiqueta.</summary>
        public static string Hint(string label, HintOffer offer)
        {
            return offer.FromStock ? A11yText.ButtonRemaining(label, offer.Stock) : label;
        }

        /// <summary>Pororoca (04 §2.4): con stock, «Barajar, 1 restante»; si no, la etiqueta.</summary>
        public static string Shuffle(string label, ShuffleOffer offer)
        {
            return offer.FromStock ? A11yText.ButtonRemaining(label, offer.Stock) : label;
        }
    }
}
