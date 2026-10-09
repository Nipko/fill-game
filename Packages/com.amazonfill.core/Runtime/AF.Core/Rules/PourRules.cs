#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;

namespace AF.Core.Rules
{
    /// <summary>
    /// Vertido legal, cantidad según el modo de vertido (GDD 02 §1.2, R9-R11; D-005: <see cref="PourMode.Run"/> por
    /// defecto, <see cref="PourMode.Single"/> para pruebas), vertidos fútiles y bloqueo
    /// (02 §1.4, R13-R16; 04 §5.1; anexo B de D-007), sobre un <see cref="BoardState"/>.
    /// </summary>
    /// <remarks>
    /// <para>Valida los argumentos y delega en <see cref="BoardRules"/>, la única implementación de las reglas con contexto
    /// de tablero y mecánicas (D-007, Q13), que también usan el solver, el bot y los generadores.</para>
    /// <para>Modo de vertido (<see cref="PourMode"/>; D-005: <see cref="PourMode.Run"/> por defecto,
    /// <see cref="PourMode.Single"/> solo para pruebas): solo lo reciben
    /// <see cref="Amount(BoardState, int, int, PourMode)"/>, <see cref="Apply(BoardState, int, int, PourMode)"/> y
    /// <see cref="CollectUsefulMoves(BoardState, bool, PourMode, Pour[])"/>. <see cref="IsLegal"/>,
    /// <see cref="IsFutile"/>, <see cref="HasUsefulMove"/> e <see cref="IsStuck"/> no dependen de la cantidad, así que
    /// valen igual en los dos modos (decisiones.md §2.1.2). Las sobrecargas sin modo son heredadas: equivalen a
    /// <see cref="PourMode.Run"/>, conservan el comportamiento anterior y las retira T6.</para>
    /// </remarks>
    public static class PourRules
    {
        /// <summary>Máximo de vertidos distintos en un tablero: 16 × 15 pares (A, B) ordenados.</summary>
        public const int MaxPours = BoardState.MaxCuias * (BoardState.MaxCuias - 1);

        /// <summary>
        /// La cuia <paramref name="index"/> puede ser origen de un vertido: no está vacía ni sellada ni tiene
        /// <c>bloqueada_origen</c> (02 §1.2; C1-C2 de 02 §3.1). Delega en <see cref="BoardRules.CanBeSource"/>, la misma
        /// condición que usan <see cref="IsLegal"/>, <see cref="CollectUsefulMoves(BoardState, bool, PourMode, Pour[])"/> y
        /// el solver.
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

            return BoardRules.CanBeSource(state.CuiasArray, 0, state.Count, state.UnitsTable, index);
        }

        /// <summary>
        /// <c>legal(A, B)</c>: A distinta de B; A puede ser origen (<see cref="CanBeSource"/>); B no sellada ni
        /// <c>bloqueada_destino</c>; los filtros de la mecánica de B (02 §9); y, por defecto, B no llena y vacía o con la
        /// misma cima que A.
        /// </summary>
        public static bool IsLegal(BoardState state, int source, int target)
        {
            CheckArguments(state, source, target);
            return BoardRules.IsLegal(state.CuiasArray, 0, state.Count, state.UnitsTable, source, target);
        }

        /// <summary>
        /// <c>cantidad(A, B)</c> en el modo <paramref name="mode"/>: <c>min(racha visible de A, espacio de B)</c> con
        /// <see cref="PourMode.Run"/> (R6, R9; por defecto desde D-005) y 1 con <see cref="PourMode.Single"/> (pruebas); 0
        /// si el vertido no es legal (en los dos modos).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Índice fuera del tablero o <paramref name="mode"/> no definido.</exception>
        public static int Amount(BoardState state, int source, int target, PourMode mode)
        {
            PourModes.Check(mode, nameof(mode));
            if (!IsLegal(state, source, target))
            {
                return 0;
            }

            return BoardRules.Amount(state.CuiasArray, 0, state.Count, state.UnitsTable, source, target, mode);
        }

        /// <summary>
        /// Heredada (equivale a <see cref="PourMode.Run"/>): <c>cantidad(A, B) = min(racha visible de A, espacio de B)</c>
        /// (R6, R9); 0 si el vertido no es legal. Ver <see cref="Amount(BoardState, int, int, PourMode)"/>.
        /// </summary>
        public static int Amount(BoardState state, int source, int target)
        {
            return Amount(state, source, target, PourMode.Run);
        }

        /// <summary>
        /// Ejecuta un vertido legal (R9) en el modo <paramref name="mode"/>: mueve
        /// <see cref="Amount(BoardState, int, int, PourMode)"/> dosis de la cima de A a la cima de B. Con
        /// <see cref="PourMode.Single"/> mueve una sola dosis aunque debajo haya más del mismo color (no existe el
        /// vertido parcial); con <see cref="PourMode.Run"/> mueve la racha visible que cabe y el resto queda en A
        /// (vertido parcial). Aplica R10 (a)-(f′) en el orden del anexo B con <see cref="BoardRules"/>: la nueva cima de A
        /// queda visible (R7) y las mecánicas se abren, se sueltan o avanzan. El sellado y la victoria se derivan del estado
        /// nuevo. Un vertido cuenta como 1 movimiento en los dos modos.
        /// </summary>
        /// <exception cref="InvalidOperationException">El vertido no es legal (R11: el llamante debe comprobarlo antes).</exception>
        /// <exception cref="ArgumentOutOfRangeException">Índice fuera del tablero o <paramref name="mode"/> no definido.</exception>
        public static BoardState Apply(BoardState state, int source, int target, PourMode mode)
        {
            return Apply(state, source, target, mode, out _);
        }

        /// <summary>
        /// Como <see cref="Apply(BoardState, int, int, PourMode)"/> y devuelve en <paramref name="events"/> lo que ocurrió
        /// en cada etapa de R10 (a)-(f′) (sin evaluar todavía la victoria ni el bloqueo).
        /// </summary>
        /// <exception cref="InvalidOperationException">El vertido no es legal (R11).</exception>
        /// <exception cref="ArgumentOutOfRangeException">Índice fuera del tablero o <paramref name="mode"/> no definido.</exception>
        public static BoardState Apply(BoardState state, int source, int target, PourMode mode, out PourEvents events)
        {
            int amount = Amount(state, source, target, mode);
            if (amount == 0)
            {
                throw new InvalidOperationException(
                    "Vertido ilegal de la cuia " + source.ToString(CultureInfo.InvariantCulture)
                    + " a la cuia " + target.ToString(CultureInfo.InvariantCulture) + ".");
            }

            bool futile = BoardRules.IsFutile(state.CuiasArray, 0, state.Count, state.UnitsTable, source, target);
            events = default;
            return state.WithPour(source, target, mode, futile, ref events);
        }

        /// <summary>
        /// Heredada (equivale a <see cref="PourMode.Run"/>): mueve la racha visible que cabe; si no cabe entera, el
        /// resto queda en A (vertido parcial). Ver <see cref="Apply(BoardState, int, int, PourMode)"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">El vertido no es legal (R11: el llamante debe comprobarlo antes).</exception>
        public static BoardState Apply(BoardState state, int source, int target)
        {
            return Apply(state, source, target, PourMode.Run);
        }

        /// <summary>
        /// <c>futil(A, B)</c> (02 §1.4 y 04 §5.1 con el anexo B de D-007): A monocolor, no sellada y sin dosis ocultas hacia
        /// una B vacía, salvo la rama "casa correcta" (<c>cap_A ≠ U_c</c> y <c>cap_B = U_c</c>), un destino que cambia al
        /// recibir (Hoja, Matapi…) o un origen exento. Un vertido fútil sigue siendo legal (R13): solo no cuenta para evitar
        /// el bloqueo. Falso si A = B o A está vacía. No depende del modo de vertido; en <see cref="PourMode.Single"/> un
        /// fútil puede partir una cuia monocolor en dos (decisiones.md §2.1.2, E2), lo que con capacidades iguales solo
        /// aumenta la dispersión.
        /// </summary>
        public static bool IsFutile(BoardState state, int source, int target)
        {
            CheckArguments(state, source, target);
            if (source == target)
            {
                return false;
            }

            return BoardRules.IsFutile(state.CuiasArray, 0, state.Count, state.UnitsTable, source, target);
        }

        /// <summary>
        /// <c>movimientos_utiles(S)</c> (02 §1.4; <c>legalNonFutileMoves</c> de 04 §5.1): escribe en
        /// <paramref name="buffer"/> los vertidos legales y no fútiles, en orden (A, B) ascendente, con la cantidad del
        /// modo <paramref name="mode"/>, y devuelve cuántos hay. Los pares (A, B) son los mismos en los dos modos; solo
        /// cambia <see cref="Pour.Amount"/> (siempre 1 en <see cref="PourMode.Single"/>). Con
        /// <paramref name="futileRuleEnabled"/> = false (kill-switch <c>futile_rule_enabled</c>, R16) devuelve todos los
        /// vertidos legales. No asigna memoria.
        /// </summary>
        /// <param name="state">Tablero.</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c> (08 §9).</param>
        /// <param name="mode">Modo de vertido (<c>rules_pour_mode</c>; D-005: <see cref="PourMode.Run"/> por defecto,
        /// <see cref="PourMode.Single"/> para pruebas).</param>
        /// <param name="buffer">Destino; al menos <c>T·(T−1)</c> posiciones (basta con <see cref="MaxPours"/>).</param>
        public static int CollectUsefulMoves(BoardState state, bool futileRuleEnabled, PourMode mode, Pour[] buffer)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            PourModes.Check(mode, nameof(mode));
            int count = state.Count;
            if (buffer.Length < count * (count - 1))
            {
                throw new ArgumentException("El búfer necesita al menos T·(T−1) posiciones (PourRules.MaxPours basta).", nameof(buffer));
            }

            var board = new BoardSlice(state.CuiasArray, 0, count, state.UnitsTable);
            int[] units = state.UnitsTable;
            int written = 0;
            for (int a = 0; a < count; a++)
            {
                Cuia from = board[a];
                if (!BoardRules.IsSourceCuia(from, units))
                {
                    continue;
                }

                for (int b = 0; b < count; b++)
                {
                    if (b == a || !BoardRules.Accepts(in board, a, b))
                    {
                        continue;
                    }

                    if (futileRuleEnabled && BoardRules.IsFutileCuias(from, board[b], units))
                    {
                        continue;
                    }

                    int amount = mode == PourMode.Run ? BoardRules.RunAmount(in board, a, b) : 1;
                    buffer[written++] = new Pour(a, b, amount);
                }
            }

            return written;
        }

        /// <summary>
        /// Heredada (equivale a <see cref="PourMode.Run"/>): <c>movimientos_utiles(S)</c> con la cantidad de la racha que
        /// cabe. Ver <see cref="CollectUsefulMoves(BoardState, bool, PourMode, Pour[])"/>. No asigna memoria.
        /// </summary>
        public static int CollectUsefulMoves(BoardState state, bool futileRuleEnabled, Pour[] buffer)
        {
            return CollectUsefulMoves(state, futileRuleEnabled, PourMode.Run, buffer);
        }

        /// <summary>
        /// <c>movimientos_utiles(S) ≠ ∅</c>: existe un vertido legal y no fútil (o solo legal, con el kill-switch
        /// apagado). No depende del modo de vertido, ni en <see cref="PourMode.Run"/> (por defecto, D-005) ni en
        /// <see cref="PourMode.Single"/> (pruebas): ni <c>legal</c> ni <c>futil</c> dependen de la cantidad (decisiones.md
        /// §2.1.2).
        /// No asigna memoria.
        /// </summary>
        public static bool HasUsefulMove(BoardState state, bool futileRuleEnabled)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return BoardRules.HasUsefulMove(state.CuiasArray, 0, state.Count, state.UnitsTable, futileRuleEnabled);
        }

        /// <summary>
        /// Aguas estancadas (R13): no es victoria y no queda ningún vertido útil. Se evalúa después de la
        /// victoria, como fija el orden de R10 (g, h); por R14 nunca es verdadero mientras exista un vertido útil.
        /// Para un mismo estado vale igual en los dos modos de vertido (<see cref="PourMode.Run"/>, por defecto desde D-005,
        /// y <see cref="PourMode.Single"/>, de pruebas; decisiones.md §2.1.2); lo que cambia es qué estados se alcanzan.
        /// </summary>
        public static bool IsStuck(BoardState state, bool futileRuleEnabled)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return BoardRules.IsStuck(state.CuiasArray, 0, state.Count, state.UnitsTable, futileRuleEnabled);
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
