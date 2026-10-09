#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Solver.Generation
{
    /// <summary>
    /// <c>MOVIMIENTOS_INVERSOS(S)</c> de GDD 03 §4.1: el inverso exacto de un vertido útil. Un movimiento inverso
    /// <c>(a → b, m)</c> pasa <c>m</c> dosis de la cima de <c>a</c> a la cima de <c>b</c>, y es válido si y solo si en el
    /// estado resultante el vertido directo <c>b → a</c> es legal, mueve exactamente <c>m</c> dosis en el modo de vertido y
    /// no es fútil, todo evaluado con <see cref="BoardRules"/>. Por eso el estado inicial que sale de un paseo inverso tiene
    /// solución por construcción con <c>movimientos_utiles</c> (03 §4).
    /// </summary>
    /// <remarks>
    /// <para>En <see cref="PourMode.Run"/> equivale a las condiciones literales de 03 §4.1 (no extraer la racha entera si
    /// deja otro color arriba; <c>b</c> sin <c>c</c> arriba para que el directo mueva exactamente <c>m</c>; no volcar una
    /// monocolor entera en una vacía, que sería un directo fútil), con la excepción «casa correcta» de las capacidades mixtas
    /// resuelta por la misma <c>futil</c> que el juego. En <see cref="PourMode.Single"/>, <c>m</c> = 1 y desaparece la
    /// condición de la cima de <c>b</c> (nota de 03 §4.1).</para>
    /// <para>Solo tableros sin mecánicas ni dosis ocultas: las mecánicas se superponen después sobre el tablero base y se
    /// verifican con el solver (03 §9, consecuencia 2). Sin asignaciones; el vertido directo se evalúa sobre el par
    /// <c>(b, a)</c>.</para>
    /// </remarks>
    public static class InverseMoves
    {
        /// <summary>Máximo de movimientos inversos de un tablero: 16 orígenes × 6 cantidades × 15 destinos.</summary>
        public const int MaxMoves = BoardState.MaxCuias * Cuia.MaxCapacity * (BoardState.MaxCuias - 1);

        /// <summary>
        /// Escribe en <paramref name="buffer"/> los movimientos inversos de <paramref name="count"/> cuias desde
        /// <paramref name="offset"/>, en orden (a, m, b) ascendente, y devuelve cuántos hay.
        /// </summary>
        /// <param name="pair">Búfer de 2 cuias para evaluar el vertido directo.</param>
        public static int Collect(Cuia[] cuias, int offset, int count, int[] units, PourMode mode, InverseMove[] buffer, Cuia[] pair)
        {
            int written = 0;
            for (int a = 0; a < count; a++)
            {
                Cuia from = cuias[offset + a];
                if (from.IsEmpty)
                {
                    continue;
                }

                int color = from.TopColor;
                int maxAmount = mode == PourMode.Run ? from.TopRunLength : 1;
                for (int m = 1; m <= maxAmount; m++)
                {
                    Cuia rest = from.RemoveTop(m);
                    for (int b = 0; b < count; b++)
                    {
                        if (b == a)
                        {
                            continue;
                        }

                        Cuia to = cuias[offset + b];
                        if (to.Space < m || (mode == PourMode.Run && !to.IsEmpty && to.TopColor == color))
                        {
                            continue; // no cabe, o el directo movería más de m (03 §4.1)
                        }

                        pair[0] = to.AddOnTop(color, m); // b tras el inverso: origen del directo
                        pair[1] = rest;                  // a tras el inverso: destino del directo
                        if (!BoardRules.IsLegal(pair, 0, 2, units, 0, 1)
                            || BoardRules.Amount(pair, 0, 2, units, 0, 1, mode) != m
                            || BoardRules.IsFutile(pair, 0, 2, units, 0, 1))
                        {
                            continue;
                        }

                        buffer[written++] = new InverseMove(a, b, m);
                    }
                }
            }

            return written;
        }

        /// <summary>Aplica el movimiento inverso: <c>m</c> dosis de la cima de <c>a</c> a la cima de <c>b</c>.</summary>
        public static void Apply(Cuia[] cuias, int offset, InverseMove move)
        {
            Cuia from = cuias[offset + move.Source];
            int color = from.TopColor;
            cuias[offset + move.Source] = from.RemoveTop(move.Amount);
            cuias[offset + move.Target] = cuias[offset + move.Target].AddOnTop(color, move.Amount);
        }
    }

    /// <summary>Movimiento inverso <c>(a → b, m)</c> de 03 §4.1; su vertido directo es <c>b → a</c> con <c>m</c> dosis.</summary>
    public readonly struct InverseMove : IEquatable<InverseMove>
    {
        private readonly int _bits;

        /// <summary>Crea un movimiento inverso.</summary>
        public InverseMove(int source, int target, int amount)
        {
            _bits = source | (target << 4) | (amount << 8);
        }

        /// <summary>Cuia de la que salen las dosis en el paseo inverso (destino del vertido directo).</summary>
        public int Source => _bits & 0xF;

        /// <summary>Cuia que las recibe en el paseo inverso (origen del vertido directo).</summary>
        public int Target => (_bits >> 4) & 0xF;

        /// <summary>Dosis movidas.</summary>
        public int Amount => (_bits >> 8) & 0x7;

        /// <inheritdoc />
        public bool Equals(InverseMove other)
        {
            return _bits == other._bits;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is InverseMove other && _bits == other._bits;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _bits;
        }

        /// <summary>Forma legible: <c>Inverse(0->2 x1)</c>.</summary>
        public override string ToString()
        {
            return "Inverse(" + Source.ToString(CultureInfo.InvariantCulture) + "->" + Target.ToString(CultureInfo.InvariantCulture)
                + " x" + Amount.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}
