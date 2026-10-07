#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;

namespace AF.Core.Rules
{
    /// <summary>
    /// Vertido legal, cantidad y vertido parcial del núcleo canónico (GDD 02 §1.2, R9-R11).
    /// </summary>
    /// <remarks>
    /// Implementa <c>legal(A, B)</c> y <c>cantidad(A, B)</c> sin las ramas de mecánicas
    /// (<c>bloqueada_origen</c>, <c>bloqueada_destino</c>, Remolino, Barro y Hoja, GDD 02 §9), que se
    /// añadirán cuando se porten. Fútiles y bloqueo (R13-R16) tampoco están aún.
    /// </remarks>
    public static class PourRules
    {
        /// <summary>
        /// <c>legal(A, B)</c>: A distinta de B; A no vacía ni sellada; B no sellada ni llena; y B vacía
        /// o con la misma cima que A.
        /// </summary>
        public static bool IsLegal(BoardState state, int source, int target)
        {
            CheckArguments(state, source, target);
            if (source == target)
            {
                return false;
            }

            Cuia from = state[source];
            if (from.IsEmpty || state.IsSealed(source))
            {
                return false;
            }

            if (state.IsSealed(target))
            {
                return false;
            }

            Cuia to = state[target];
            if (to.IsFull)
            {
                return false;
            }

            return to.IsEmpty || to.TopColor == from.TopColor;
        }

        /// <summary>
        /// <c>cantidad(A, B) = min(racha visible de A, espacio de B)</c> (R6, R9); 0 si el vertido no es legal.
        /// </summary>
        public static int Amount(BoardState state, int source, int target)
        {
            if (!IsLegal(state, source, target))
            {
                return 0;
            }

            return Math.Min(state[source].TopRunLength, state[target].Space);
        }

        /// <summary>
        /// Ejecuta un vertido legal (R9): mueve <see cref="Amount"/> dosis de la racha visible de A a la
        /// cima de B; si no cabe entera, el resto queda en A (vertido parcial). Aplica R10 (a): la nueva
        /// cima de A queda visible (R7). El sellado y la victoria se derivan del estado nuevo.
        /// </summary>
        /// <exception cref="InvalidOperationException">El vertido no es legal (R11: el llamante debe comprobarlo antes).</exception>
        public static BoardState Apply(BoardState state, int source, int target)
        {
            int amount = Amount(state, source, target);
            if (amount == 0)
            {
                throw new InvalidOperationException(
                    "Vertido ilegal de la cuia " + source.ToString(CultureInfo.InvariantCulture)
                    + " a la cuia " + target.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return state.WithPour(source, target, amount);
        }

        private static void CheckArguments(BoardState state, int source, int target)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((uint)source >= (uint)state.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(source), source, "Índice de cuia fuera del tablero.");
            }

            if ((uint)target >= (uint)state.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(target), target, "Índice de cuia fuera del tablero.");
            }
        }
    }
}
