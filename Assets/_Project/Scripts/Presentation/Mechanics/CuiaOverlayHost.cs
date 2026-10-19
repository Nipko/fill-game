#nullable enable
using System;
using UnityEngine;

namespace AF.Presentation.Mechanics
{
    /// <summary>
    /// Lo que una cuia ofrece a su <see cref="CuiaOverlay"/> (contrato de overlays, arquitectura del paso 1 §5): su índice y
    /// su geometría, cuatro anclas con el origen en el centro del pie (y hacia arriba, en dp), el arte, los efectos, las capas
    /// de tablero y el tablero. <see cref="BoardBackLayer"/>, <see cref="BoardFrontLayer"/> y <see cref="Board"/> los rellena
    /// <c>CuiaView.BindBoard</c>, y la vista no crea ningún overlay antes: dentro de <see cref="CuiaOverlay.Attach"/> (y de su
    /// <c>OnAttach</c>) nunca son null. Contrato congelado desde la ola 3 (T07).
    /// </summary>
    /// <remarks>
    /// Anclas, de atrás adelante:
    /// <list type="bullet">
    /// <item><description><see cref="SlotBack"/>: en la ranura, detrás del cuerpo (no viaja al verter).</description></item>
    /// <item><description><see cref="BodyBack"/>: en el cuerpo, entre el vidrio trasero y el líquido.</description></item>
    /// <item><description><see cref="BodyFront"/>: en el cuerpo, sobre las marcas de dosis y bajo la tapa.</description></item>
    /// <item><description><see cref="SlotFront"/>: en la ranura, delante del cuerpo (no viaja al verter).</description></item>
    /// </list>
    /// Lo que cuelga del cuerpo viaja y se inclina con la cuia de origen. Lo que une cuias o ronda el tablero va en
    /// <see cref="BoardBackLayer"/> y <see cref="BoardFrontLayer"/>, nunca en el cuerpo.
    /// </remarks>
    public sealed class CuiaOverlayHost
    {
        /// <summary>Crea el anfitrión de la cuia <paramref name="index"/> con sus anclas, su arte y sus efectos.</summary>
        public CuiaOverlayHost(
            int index,
            int capacity,
            RectTransform slotBack,
            RectTransform bodyBack,
            RectTransform bodyFront,
            RectTransform slotFront,
            ProceduralArt art,
            IBoardFx? fx)
        {
            Index = index;
            Capacity = capacity;
            SlotBack = slotBack ?? throw new ArgumentNullException(nameof(slotBack));
            BodyBack = bodyBack ?? throw new ArgumentNullException(nameof(bodyBack));
            BodyFront = bodyFront ?? throw new ArgumentNullException(nameof(bodyFront));
            SlotFront = slotFront ?? throw new ArgumentNullException(nameof(slotFront));
            Art = art ?? throw new ArgumentNullException(nameof(art));
            Fx = fx;
            U = 24f;
        }

        /// <summary>Índice de la cuia en el tablero.</summary>
        public int Index { get; }

        /// <summary>Capacidad de la cuia (dosis).</summary>
        public int Capacity { get; }

        /// <summary>Unidad u en dp (medio ancho de la cuia, 07 §4.1); la fija <c>CuiaView.SetGeometry</c>.</summary>
        public float U { get; internal set; }

        /// <summary>Alto de la cuia en u (1,60 + capacidad; 07 §4.5).</summary>
        public float HeightU => CuiaGeometry.HeightU(Capacity);

        /// <summary>Ancla en la ranura, detrás del cuerpo.</summary>
        public RectTransform SlotBack { get; }

        /// <summary>Ancla en el cuerpo, entre el vidrio trasero y el líquido.</summary>
        public RectTransform BodyBack { get; }

        /// <summary>Ancla en el cuerpo, sobre las marcas de dosis y bajo la tapa.</summary>
        public RectTransform BodyFront { get; }

        /// <summary>Ancla en la ranura, delante del cuerpo.</summary>
        public RectTransform SlotFront { get; }

        /// <summary>Arte procedural compartido.</summary>
        public ProceduralArt Art { get; }

        /// <summary>Efectos del tablero (gotas, anillos), o null en pruebas sin efectos.</summary>
        public IBoardFx? Fx { get; internal set; }

        /// <summary>Capa del tablero detrás de las cuias (null hasta <c>CuiaView.BindBoard</c>; nunca dentro de un overlay).</summary>
        public RectTransform? BoardBackLayer { get; internal set; }

        /// <summary>Capa del tablero delante de las cuias (null hasta <c>CuiaView.BindBoard</c>; nunca dentro de un overlay).</summary>
        public RectTransform? BoardFrontLayer { get; internal set; }

        /// <summary>
        /// El tablero entero: posiciones y pintado de cualquier cuia (null hasta <c>CuiaView.BindBoard</c>; nunca dentro de un
        /// overlay).
        /// </summary>
        public ICuiaBoard? Board { get; internal set; }

        /// <summary>
        /// Desplazamiento vertical (dp) que un paso del overlay pide para el cuerpo de la cuia en reposo (TJ3: su cuia que se
        /// descuelga del jirau y se asienta en su ranura). <c>CuiaView</c> lo suma a su pose de reposo (con la elevación de la
        /// selección); mientras un trabajo mueve la cuia no cuenta. El overlay lo deja a 0 al terminar o saltar su paso.
        /// </summary>
        public float BodyNudgeDp { get; set; }
    }
}
