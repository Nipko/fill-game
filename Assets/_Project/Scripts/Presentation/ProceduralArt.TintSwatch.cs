#nullable enable
using UnityEngine;

namespace AF.Presentation
{
    /// <summary>
    /// Muestra del tinte para la UI (tarjeta «Fruta nueva», 06 §4.5: «muestra de color, patrón de accesibilidad»; tarea F5) [P]:
    /// la misma que la llave del medallón de la Hoja atada (<see cref="HojaAtadaSwatch"/>), un disco del tinte con su patrón y su
    /// icono de accesibilidad, como una dosis vista de frente. Sin arte nuevo: lo sustituye el del artista.
    /// </summary>
    public sealed partial class ProceduralArt
    {
        /// <summary>
        /// Muestra del tinte <paramref name="tint"/> (índice de motor 1-14): disco del color con su patrón y su icono de
        /// accesibilidad (<see cref="HojaAtadaSwatch"/>). Perezosa y en caché por tinte: se construye al enseñar la tarjeta, nunca
        /// por fotograma.
        /// </summary>
        public Sprite TintSwatch(int tint)
        {
            return HojaAtadaSwatch(tint);
        }
    }
}
