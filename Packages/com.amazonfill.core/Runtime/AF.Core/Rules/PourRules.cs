#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;

namespace AF.Core.Rules
{
    /// <summary>
    /// Vertido legal, cantidad y vertido parcial (GDD 02 §1.2, R9-R11), vertidos fútiles y bloqueo
    /// (02 §1.4, R13-R16; 04 §5.1) del núcleo canónico, sobre un <see cref="BoardState"/>.
    /// </summary>
    /// <remarks>
    /// Delega en <see cref="CuiaRules"/>, la única implementación de las reglas, que también usan el solver y el
    /// bot. Sin las ramas de mecánicas (<c>bloqueada_origen</c>, <c>bloqueada_destino</c>, Remolino, Barro, Hoja,
    /// Lluvia; GDD 02 §9), que se añadirán cuando se porten.
    /// </remarks>
    public static class PourRules
    {
        /// <summary>Máximo de vertidos distintos en un tablero: 16 × 15 pares (A, B) ordenados.</summary>
        public const int MaxPours = BoardState.MaxCuias * (BoardState.MaxCuias - 1);

        /// <summary>
        /// La cuia <paramref name="index"/> puede ser origen de un vertido: no está vacía ni sellada (02 §1.2; C1-C2
        /// de 02 §3.1). Delega en <see cref="CuiaRules.CanBeSource"/>, la misma condición que usan
        /// <see cref="IsLegal"/>, <see cref="CollectUsefulMoves"/> y el solver.
        /// </summary>
        public static bool CanBeSource(BoardState state, int index)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((uint)index >= (uint)state.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice de cuia fuera del tablero.");
            }

            return CuiaRules.CanBeSource(state.CuiaAt(index), state.UnitsTable);
        }

        /// <summary>
        /// <c>legal(A, B)</c>: A distinta de B; A puede ser origen (<see cref="CanBeSource"/>); B no sellada ni llena;
        /// y B vacía o con la misma cima que A.
        /// </summary>
        public static bool IsLegal(BoardState state, int source, int target)
        {
            CheckArguments(state, source, target);
            if (source == target)
            {
                return false;
            }

            return CuiaRules.IsLegal(state.CuiaAt(source), state.CuiaAt(target), state.UnitsTable);
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

            return CuiaRules.Amount(state.CuiaAt(source), state.CuiaAt(target));
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

        /// <summary>
        /// <c>futil(A, B)</c> (02 §1.4 y 04 §5.1): A monocolor, no sellada y sin dosis ocultas hacia una B vacía,
        /// salvo la rama "casa correcta" (<c>cap_A ≠ U_c</c> y <c>cap_B = U_c</c>). Un vertido fútil sigue siendo
        /// legal (R13): solo no cuenta para evitar el bloqueo. Falso si A = B o A está vacía.
        /// </summary>
        public static bool IsFutile(BoardState state, int source, int target)
        {
            CheckArguments(state, source, target);
            if (source == target)
            {
                return false;
            }

            return CuiaRules.IsFutile(state.CuiaAt(source), state.CuiaAt(target), state.UnitsTable);
        }

        /// <summary>
        /// <c>movimientos_utiles(S)</c> (02 §1.4; <c>legalNonFutileMoves</c> de 04 §5.1): escribe en
        /// <paramref name="buffer"/> los vertidos legales y no fútiles, en orden (A, B) ascendente, y devuelve
        /// cuántos hay. Con <paramref name="futileRuleEnabled"/> = false (kill-switch <c>futile_rule_enabled</c>,
        /// R16) devuelve todos los vertidos legales. No asigna memoria.
        /// </summary>
        /// <param name="state">Tablero.</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c> (08 §9).</param>
        /// <param name="buffer">Destino; al menos <c>T·(T−1)</c> posiciones (basta con <see cref="MaxPours"/>).</param>
        public static int CollectUsefulMoves(BoardState state, bool futileRuleEnabled, Pour[] buffer)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            int count = state.Count;
            if (buffer.Length < count * (count - 1))
            {
                throw new ArgumentException("El búfer necesita al menos T·(T−1) posiciones (PourRules.MaxPours basta).", nameof(buffer));
            }

            int[] units = state.UnitsTable;
            int written = 0;
            for (int a = 0; a < count; a++)
            {
                Cuia from = state.CuiaAt(a);
                if (!CuiaRules.CanBeSource(from, units))
                {
                    continue;
                }

                for (int b = 0; b < count; b++)
                {
                    if (b == a)
                    {
                        continue;
                    }

                    Cuia to = state.CuiaAt(b);
                    if (!CuiaRules.IsLegal(from, to, units))
                    {
                        continue;
                    }

                    if (futileRuleEnabled && CuiaRules.IsFutile(from, to, units))
                    {
                        continue;
                    }

                    buffer[written++] = new Pour(a, b, CuiaRules.Amount(from, to));
                }
            }

            return written;
        }

        /// <summary>
        /// <c>movimientos_utiles(S) ≠ ∅</c>: existe un vertido legal y no fútil (o solo legal, con el kill-switch
        /// apagado). No asigna memoria.
        /// </summary>
        public static bool HasUsefulMove(BoardState state, bool futileRuleEnabled)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            int count = state.Count;
            int[] units = state.UnitsTable;
            for (int a = 0; a < count; a++)
            {
                Cuia from = state.CuiaAt(a);
                if (!CuiaRules.CanBeSource(from, units))
                {
                    continue;
                }

                for (int b = 0; b < count; b++)
                {
                    if (b == a)
                    {
                        continue;
                    }

                    Cuia to = state.CuiaAt(b);
                    if (CuiaRules.IsLegal(from, to, units)
                        && !(futileRuleEnabled && CuiaRules.IsFutile(from, to, units)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Aguas estancadas (R13): no es victoria y no queda ningún vertido útil. Se evalúa después de la
        /// victoria, como fija el orden de R10 (e, f); por R14 nunca es verdadero mientras exista un vertido útil.
        /// </summary>
        public static bool IsStuck(BoardState state, bool futileRuleEnabled)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return !state.IsWon() && !HasUsefulMove(state, futileRuleEnabled);
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
