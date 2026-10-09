#nullable enable
using AF.Core.Board;

namespace AF.Core.Rules
{
    /// <summary>
    /// Vista sin asignaciones de un tablero en un array: <see cref="Count"/> cuias desde <see cref="Offset"/> y la tabla
    /// <c>U_c</c> (03 §3). Es la forma en que <see cref="BoardRules"/> pasa el contexto de tablero a las mecánicas: el
    /// solver guarda sus nodos en un array plano y el cliente en el de <see cref="BoardState"/>.
    /// </summary>
    /// <remarks>
    /// Los índices de los métodos son relativos (0 a <see cref="Count"/> − 1). Es un <c>readonly struct</c> que se pasa con
    /// <c>in</c>: no copia el array ni asigna. <see cref="Set"/> escribe en el array subyacente (solo lo hace
    /// <see cref="BoardRules.ApplyInPlace(Cuia[], int, int, int[], int, int, PourMode, bool, ref PourEvents)"/>).
    /// </remarks>
    internal readonly struct BoardSlice
    {
        public readonly Cuia[] Cuias;
        public readonly int Offset;
        public readonly int Count;
        public readonly int[] Units;

        public BoardSlice(Cuia[] cuias, int offset, int count, int[] units)
        {
            Cuias = cuias;
            Offset = offset;
            Count = count;
            Units = units;
        }

        /// <summary>Cuia <paramref name="index"/> (relativo).</summary>
        public Cuia this[int index] => Cuias[Offset + index];

        /// <summary>Sustituye la cuia <paramref name="index"/> (relativo).</summary>
        public void Set(int index, Cuia cuia)
        {
            Cuias[Offset + index] = cuia;
        }

        /// <summary>R5 sobre la cuia <paramref name="index"/>.</summary>
        public bool IsSealed(int index)
        {
            return CuiaRules.IsSealed(Cuias[Offset + index], Units);
        }
    }
}
