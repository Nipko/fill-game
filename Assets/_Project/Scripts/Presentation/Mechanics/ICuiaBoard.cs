#nullable enable
using AF.Core.Board;
using UnityEngine;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// El tablero visto desde un overlay (contrato de overlays, arquitectura del paso 1 §5): lo que muestra cada cuia, dónde
    /// está y cómo pintarla, para los pasos que tocan dos cuias (el mordisco de la Piraña, el canal del Remolino, el hilo de
    /// luz de la Liana). Lo implementa la cola de animación del tablero. Las posiciones van en el espacio de las capas de
    /// tablero (<see cref="CuiaOverlayHost.BoardBackLayer"/> y <see cref="CuiaOverlayHost.BoardFrontLayer"/>).
    /// </summary>
    public interface ICuiaBoard
    {
        /// <summary>Cuias del tablero.</summary>
        int Count { get; }

        /// <summary>Sube cada vez que el tablero se recoloca: lo cacheado con una versión anterior ya no vale.</summary>
        int LayoutVersion { get; }

        /// <summary>Último estado pintado en la cuia <paramref name="i"/>.</summary>
        Cuia ShownCuia(int i);

        /// <summary>Centro del cuerpo de la cuia <paramref name="i"/> en reposo.</summary>
        Vector2 BodyCenter(int i);

        /// <summary>Centro del pie de la cuia <paramref name="i"/> en reposo.</summary>
        Vector2 Foot(int i);

        /// <summary>Rectángulo de la ranura de la cuia <paramref name="i"/> (ranura y repisa).</summary>
        Rect SlotRect(int i);

        /// <summary>Fija color, relleno (0..1) y patrón (tinte; 0 = sin patrón) de la dosis <paramref name="dose"/> de la cuia <paramref name="i"/>.</summary>
        void SetDose(int i, int dose, Color color, float fill, int pattern);

        /// <summary>Pinta en la cuia <paramref name="i"/> el estado <paramref name="cuia"/> (contenido, marcas, tapa y overlay).</summary>
        void ShowContent(int i, Cuia cuia, bool sealedNow);
    }
}
