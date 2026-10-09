#nullable enable
using System;
using AF.Core.Board;

namespace AF.Solver.Generation
{
    /// <summary>
    /// Complejidad de laicwben2 (GDD 03 §4.1; research/02 §3): <c>2·transiciones + 2·cuias_mezcladas + cuias_parciales</c>.
    /// Es un filtro del generador (<c>compl_min</c>, por defecto 3K como el prototipo), no entra en el índice D (03 §7).
    /// Solo enteros y sin asignaciones.
    /// </summary>
    public static class Complexity
    {
        /// <summary>
        /// Complejidad de <paramref name="count"/> cuias desde <paramref name="offset"/>: cada cambio de color dentro de una
        /// cuia es una transición, una cuia con más de un color está mezclada y una cuia no vacía y no llena es parcial.
        /// </summary>
        public static int Of(Cuia[] cuias, int offset, int count)
        {
            int transitions = 0;
            int mixed = 0;
            int partial = 0;
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                Cuia cuia = cuias[i];
                int n = cuia.Count;
                if (n == 0)
                {
                    continue;
                }

                int breaks = 0;
                for (int k = 1; k < n; k++)
                {
                    if (cuia.ColorAt(k) != cuia.ColorAt(k - 1))
                    {
                        breaks++;
                    }
                }

                transitions += breaks;
                mixed += breaks > 0 ? 1 : 0;
                partial += n < cuia.Capacity ? 1 : 0;
            }

            return (2 * transitions) + (2 * mixed) + partial;
        }

        /// <summary>Complejidad de un tablero.</summary>
        public static int Of(BoardState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var cuias = new Cuia[state.Count];
            for (int i = 0; i < cuias.Length; i++)
            {
                cuias[i] = state[i];
            }

            return Of(cuias, 0, cuias.Length);
        }
    }
}
