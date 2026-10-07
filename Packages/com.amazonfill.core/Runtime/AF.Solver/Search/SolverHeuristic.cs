#nullable enable
using System;
using AF.Core.Board;

namespace AF.Solver.Search
{
    /// <summary>
    /// Heurística del solver (GDD 03 §5): <c>H(S) = max(h1, h2)</c>, con
    /// <c>h1 = Σ_cuias (segmentos − 1)</c> (cortes de color: un vertido elimina a lo sumo uno) y
    /// <c>h2 = Σ_colores (cuias que contienen c − 1)</c> (dispersión: un vertido saca un color de a lo sumo una cuia).
    /// Ambas son admisibles y consistentes, así que A* con <c>w = 1</c> devuelve el par exacto.
    /// </summary>
    /// <remarks>
    /// Mira el color real de cada dosis, también de las ocultas. <c>h2</c> se calcula con máscaras de bits:
    /// <c>Σ popcount(máscara_i) − popcount(⋃ máscara_i)</c>. Sin asignaciones; solo enteros.
    /// </remarks>
    public static class SolverHeuristic
    {
        /// <summary>Cortes de color de una cuia: segmentos de color contiguos − 1 (0 si está vacía).</summary>
        public static int ColorBreaks(Cuia cuia)
        {
            int count = cuia.Count;
            int breaks = 0;
            for (int k = 1; k < count; k++)
            {
                if (cuia.ColorAt(k) != cuia.ColorAt(k - 1))
                {
                    breaks++;
                }
            }

            return breaks;
        }

        /// <summary>Máscara de los colores presentes en la cuia: bit <c>c − 1</c> por cada color <c>c</c>.</summary>
        public static int ColorMask(Cuia cuia)
        {
            int count = cuia.Count;
            int mask = 0;
            for (int k = 0; k < count; k++)
            {
                mask |= 1 << (cuia.ColorAt(k) - 1);
            }

            return mask;
        }

        /// <summary><c>H = max(h1, h2)</c> de <paramref name="count"/> cuias desde <paramref name="offset"/>.</summary>
        public static int Estimate(Cuia[] cuias, int offset, int count)
        {
            if (cuias == null)
            {
                throw new ArgumentNullException(nameof(cuias));
            }

            if (offset < 0 || count < 0 || offset > cuias.Length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "El tramo [offset, offset + count) se sale del array.");
            }

            int breaks = 0;
            int maskBits = 0;
            int union = 0;
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                int mask = Accumulate(cuias[i], ref breaks);
                maskBits += PopCount(mask);
                union |= mask;
            }

            int dispersion = maskBits - PopCount(union);
            return breaks > dispersion ? breaks : dispersion;
        }

        /// <summary><c>H = max(h1, h2)</c> de un tablero.</summary>
        public static int Estimate(BoardState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            int breaks = 0;
            int maskBits = 0;
            int union = 0;
            for (int i = 0; i < state.Count; i++)
            {
                int mask = Accumulate(state[i], ref breaks);
                maskBits += PopCount(mask);
                union |= mask;
            }

            int dispersion = maskBits - PopCount(union);
            return breaks > dispersion ? breaks : dispersion;
        }

        /// <summary>Suma los cortes de <paramref name="cuia"/> a <paramref name="breaks"/> y devuelve su máscara de colores (una pasada).</summary>
        private static int Accumulate(Cuia cuia, ref int breaks)
        {
            int count = cuia.Count;
            if (count == 0)
            {
                return 0;
            }

            int previous = cuia.ColorAt(0);
            int mask = 1 << (previous - 1);
            for (int k = 1; k < count; k++)
            {
                int color = cuia.ColorAt(k);
                if (color != previous)
                {
                    breaks++;
                    mask |= 1 << (color - 1);
                    previous = color;
                }
            }

            return mask;
        }

        /// <summary>Bits a 1 de <paramref name="value"/> (SWAR; <c>BitOperations</c> no existe en netstandard2.1).</summary>
        internal static int PopCount(int value)
        {
            uint v = (uint)value;
            v -= (v >> 1) & 0x55555555u;
            v = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);
            v = (v + (v >> 4)) & 0x0F0F0F0Fu;
            return (int)((v * 0x01010101u) >> 24);
        }
    }
}
