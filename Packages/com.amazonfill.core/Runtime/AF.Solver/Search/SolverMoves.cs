#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Solver.Search
{
    /// <summary>
    /// <c>MOVIMIENTOS(S)</c> de GDD 03 §5: los vertidos útiles (legales y no fútiles; con el kill-switch apagado, todos los
    /// legales) con la cantidad del modo de vertido, sin repetir cuias equivalentes. Es la lista que expande
    /// <see cref="AStarSolver"/> y la que usan el bot casual (03 §6) y las métricas (03 §7), con las reglas de
    /// <see cref="BoardRules"/> (03 §5: «el solver no mantiene una poda propia»).
    /// </summary>
    /// <remarks>
    /// <para><b>Deduplicación</b> (la del solver desde 0.1.0): un origen idéntico (mismo <see cref="Cuia.Packed"/>) al de
    /// la posición anterior no se repite, ni un destino idéntico al de la posición anterior (salvo que esa posición sea el
    /// origen). Cuias idénticas dan los mismos hijos canónicos: generaliza «los vacíos equivalentes por (cap, attr) una sola
    /// vez» de 03 §5. Sobre un tablero en orden canónico (<see cref="BoardCanon.Sort"/>) las cuias idénticas son vecinas y la
    /// deduplicación es completa; sobre otro orden solo se fusionan las vecinas (sigue siendo correcto).</para>
    /// <para>Orden de salida: (origen, destino) ascendente. Sin asignaciones ni validación de argumentos (bucle caliente).</para>
    /// </remarks>
    public static class SolverMoves
    {
        /// <summary>Máximo de movimientos de un tablero: 16 × 15 pares (A, B) ordenados.</summary>
        public const int MaxMoves = PourRules.MaxPours;

        /// <summary>
        /// Escribe en <paramref name="buffer"/> los movimientos de 03 §5 de <paramref name="count"/> cuias desde
        /// <paramref name="offset"/> y devuelve cuántos hay.
        /// </summary>
        /// <param name="cuias">Array con el tablero.</param>
        /// <param name="offset">Primera cuia del tablero en el array.</param>
        /// <param name="count">Número de cuias (T).</param>
        /// <param name="units">Tabla <c>U_c</c> indexada por color (0..16).</param>
        /// <param name="mode">Modo de vertido de la cantidad.</param>
        /// <param name="futileRuleEnabled">Valor de <c>futile_rule_enabled</c>: con false también salen los fútiles (marcados).</param>
        /// <param name="buffer">Destino; basta con <see cref="MaxMoves"/> posiciones.</param>
        public static int Collect(Cuia[] cuias, int offset, int count, int[] units, PourMode mode, bool futileRuleEnabled, SolverMove[] buffer)
        {
            var board = new BoardSlice(cuias, offset, count, units);
            int written = 0;
            for (int i = 0; i < count; i++)
            {
                Cuia from = board[i];
                if (!BoardRules.IsSourceCuia(from, units))
                {
                    continue;
                }

                if (i > 0 && from.Packed == board[i - 1].Packed)
                {
                    continue; // origen idéntico al anterior: mismos hijos canónicos
                }

                int fromTop = from.TopColor;
                for (int j = 0; j < count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    Cuia to = board[j];
                    if (!to.HasMechanic && !to.MudExposed && (to.IsFull || (!to.IsEmpty && to.TopColor != fromTop)))
                    {
                        // Descarte rápido de destinos normales que la regla normal rechazaría igual. No vale para las cuias
                        // con mecánica (Remolino: entra en la pareja, de cualquier color) ni con Barro en la cima
                        // (barro_expuesto acepta cualquier color, 02 §1.2): esas las decide BoardRules.Accepts.
                        continue;
                    }

                    if (j > 0 && j - 1 != i && to.Packed == board[j - 1].Packed)
                    {
                        continue; // destino idéntico al anterior (03 §5: vacíos equivalentes una sola vez)
                    }

                    if (!BoardRules.Accepts(in board, i, j))
                    {
                        continue;
                    }

                    bool futile = BoardRules.IsFutileCuias(from, to, units);
                    if (futileRuleEnabled && futile)
                    {
                        continue;
                    }

                    int amount = mode == PourMode.Run ? BoardRules.RunAmount(in board, i, j) : 1;
                    buffer[written++] = new SolverMove(i, j, amount, futile);
                }
            }

            return written;
        }
    }

    /// <summary>Un movimiento de <see cref="SolverMoves"/>: vertido A→B, dosis que mueve y si es fútil (02 §1.4, anexo B).</summary>
    public readonly struct SolverMove : IEquatable<SolverMove>
    {
        private readonly int _bits;

        internal SolverMove(int source, int target, int amount, bool futile)
        {
            _bits = source | (target << 4) | (amount << 8) | (futile ? 1 << 11 : 0);
        }

        /// <summary>Índice de la cuia origen A (relativo al tablero).</summary>
        public int Source => _bits & 0xF;

        /// <summary>Índice de la cuia destino B.</summary>
        public int Target => (_bits >> 4) & 0xF;

        /// <summary>Dosis que mueve: <c>cantidad(A, B)</c> en el modo de la búsqueda (R9).</summary>
        public int Amount => (_bits >> 8) & 0x7;

        /// <summary>El vertido es fútil (solo aparece con el kill-switch apagado). No avanza contadores (Q18 B).</summary>
        public bool IsFutile => (_bits & (1 << 11)) != 0;

        /// <summary>El mismo vertido como <see cref="Pour"/>.</summary>
        public Pour ToPour()
        {
            return new Pour(Source, Target, Amount);
        }

        /// <inheritdoc />
        public bool Equals(SolverMove other)
        {
            return _bits == other._bits;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is SolverMove other && _bits == other._bits;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _bits;
        }

        /// <summary>Forma legible: <c>Move(0->2 x1)</c>, con <c> futil</c> si lo es.</summary>
        public override string ToString()
        {
            return "Move(" + Source.ToString(CultureInfo.InvariantCulture) + "->" + Target.ToString(CultureInfo.InvariantCulture)
                + " x" + Amount.ToString(CultureInfo.InvariantCulture) + (IsFutile ? " futil)" : ")");
        }
    }
}
