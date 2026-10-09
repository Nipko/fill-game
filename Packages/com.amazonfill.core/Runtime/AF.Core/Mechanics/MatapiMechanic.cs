#nullable enable
using AF.Core.Board;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// M10 Matapi (D-007, debut 63; propuesta de D-007 b.2 M10, reglas 1-8): trampa de camarón; lo que entra no sale.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description>Nunca es origen (<c>bloqueada_origen</c>): un toque como origen es un vertido ilegal (R11).</description></item>
    /// <item><description>Como destino sigue la regla normal: vacío acepta cualquier fruta; con contenido, solo la de su cima;
    /// en <c>run</c> entra el tramo que cabe.</description></item>
    /// <item><description>Empieza vacío o con 1-3 dosis visibles de un solo color.</description></item>
    /// <item><description>Para ganar tiene que acabar sellado o vacío (R12, sin cambios).</description></item>
    /// <item><description>Como destino nunca es fútil (<c>cambia_al_recibir</c> del anexo B).</description></item>
    /// <item><description>No lleva Barro ni dosis ocultas; nunca es pareja de Remolino, cuia de Lluvia ni cueva de Piraña
    /// (una mecánica por cuia).</description></item>
    /// </list>
    /// Validación [P, derivado de R12]: si empieza con dosis, su capacidad es la <c>U_c</c> de su color (si no, nunca podría
    /// sellar ni vaciarse y el nivel no tendría solución). La excepción de la Piraña (regla 7) llega con la Piraña (C3-2).
    /// No tiene estado: nunca se abre.
    /// </remarks>
    internal sealed class MatapiMechanic : MechanicBase
    {
        public override bool BlocksSource(Cuia self)
        {
            return true;
        }

        public override bool ChangesOnReceive(Cuia self)
        {
            return true;
        }

        public override string? ValidateInitial(BoardState state, int index)
        {
            Cuia matapi = state[index];
            if (matapi.IsEmpty)
            {
                return null;
            }

            if (matapi.Count > 3)
            {
                return "D-007 (Matapi, regla 3): empieza vacío o con 1-3 dosis";
            }

            if (matapi.HasHidden)
            {
                return "D-007 (Matapi, regla 6): sin dosis ocultas";
            }

            if (!matapi.IsMonocolor)
            {
                return "D-007 (Matapi, regla 3): sus dosis son de un solo color";
            }

            for (int k = 0; k < matapi.Count; k++)
            {
                if (matapi.ColorAt(k) == Cuia.MudColor)
                {
                    return "D-007 (Matapi, regla 6): sin Barro";
                }
            }

            if (matapi.Capacity != state.UnitsOf(matapi.TopColor))
            {
                return "R12: un Matapi con dosis solo puede acabar sellado, así que su capacidad es la U_c de su color [P]";
            }

            return null;
        }
    }
}
