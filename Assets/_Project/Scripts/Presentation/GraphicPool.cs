#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AF.Presentation
{
    /// <summary>
    /// Gráficos de pools que se muestran y se ocultan en mitad de una animación (la intro y su tropa: 0 B por fotograma tras
    /// <c>Start</c>, guion de la intro §13 y A10). UGUI guarda los gráficos ocultos en el registro de su Canvas
    /// (<see cref="GraphicRegistry"/>) y mostrarlos solo los reactiva, sin asignar; pero si el Canvas anidado cambia de estado o
    /// de jerarquía (crearlo, reordenarlo al montar el tablero, activarlo), UGUI desregistra los que estaban ocultos y el primer
    /// <c>enabled = true</c> de cada uno haría crecer esas colecciones en mitad de la animación. <see cref="EnsureRegistered"/> los
    /// vuelve a registrar, ocultos, al arrancar (donde sí se puede asignar).
    /// </summary>
    public static class GraphicPool
    {
        /// <summary>
        /// Deja <paramref name="graphic"/> (oculto y en un objeto activo) registrado en su Canvas: si UGUI lo había desregistrado,
        /// lo muestra y lo oculta en el acto (nada se dibuja). Sin efecto si está a la vista, en un objeto inactivo o ya
        /// registrado.
        /// </summary>
        public static void EnsureRegistered(Graphic graphic)
        {
            if (graphic.enabled || !graphic.gameObject.activeInHierarchy)
            {
                return;
            }

            Canvas? canvas = graphic.canvas;
            if (canvas != null && GraphicRegistry.GetGraphicsForCanvas(canvas).Contains(graphic))
            {
                return;
            }

            graphic.enabled = true;
            graphic.enabled = false;
        }

        /// <summary>Registrado en su Canvas (tests).</summary>
        public static bool IsRegistered(Graphic graphic)
        {
            Canvas? canvas = graphic.canvas;
            return canvas != null && GraphicRegistry.GetGraphicsForCanvas(canvas).Contains(graphic);
        }
    }
}
