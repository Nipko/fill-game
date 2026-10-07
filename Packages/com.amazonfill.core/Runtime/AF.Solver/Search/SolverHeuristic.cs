#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Solver.Search
{
    /// <summary>
    /// Heurística del solver (GDD 03 §5): <c>H(S) = max(h1, h2)</c>, con
    /// <c>h1 = Σ_cuias (segmentos − 1)</c> (cortes de color: un vertido elimina a lo sumo uno) y
    /// <c>h2 = Σ_colores (cuias que contienen c − 1)</c> (dispersión: un vertido saca un color de a lo sumo una cuia).
    /// Ambas son admisibles y consistentes, así que A* con <c>w = 1</c> devuelve el par exacto.
    /// </summary>
    /// <remarks>
    /// <para>Con <see cref="PourMode.Single"/> (D-001) <c>H</c> sigue siendo admisible y consistente (un vertido de una
    /// dosis también elimina como mucho un corte y saca un color de como mucho una cuia), pero informa menos: una racha de 3
    /// cuesta 3 vertidos y <c>h1</c> la cuenta como 1. En ese modo se usa
    /// <c>H_single = max(h3, h2)</c> [P] (decisiones.md §2.1.3), con <c>h3 = Σ_cuias (dosis − racha del fondo)</c>:</para>
    /// <list type="bullet">
    /// <item><description>admisible: toda dosis por encima de la racha del fondo tiene que moverse al menos una vez (la
    /// dosis de otro color que tiene debajo no puede salir antes) y cada vertido <c>Single</c> mueve exactamente una;</description></item>
    /// <item><description>consistente: cada vertido cambia <c>h3</c> en −1, 0 o +1 (sale una dosis de A y entra una en B);</description></item>
    /// <item><description><c>h3 ≥ h1</c> (cada corte deja al menos una dosis encima), así que domina a <c>H</c>.</description></item>
    /// </list>
    /// <para>Mira el color real de cada dosis, también de las ocultas. <c>h2</c> se calcula con máscaras de bits:
    /// <c>Σ popcount(máscara_i) − popcount(⋃ máscara_i)</c>. Sin asignaciones; solo enteros.</para>
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

        /// <summary>
        /// Dosis por encima de la racha del fondo: <c>dosis − longitud de la racha del fondo</c> (0 si está vacía o es
        /// monocolor). Término de <c>h3</c> en <c>H_single</c>.
        /// </summary>
        public static int DosesAboveBottomRun(Cuia cuia)
        {
            int count = cuia.Count;
            if (count == 0)
            {
                return 0;
            }

            int bottom = cuia.ColorAt(0);
            for (int k = 1; k < count; k++)
            {
                if (cuia.ColorAt(k) != bottom)
                {
                    return count - k;
                }
            }

            return 0;
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
            return Estimate(cuias, offset, count, PourMode.Run);
        }

        /// <summary>
        /// Heurística del modo <paramref name="mode"/> sobre <paramref name="count"/> cuias desde <paramref name="offset"/>:
        /// <c>H = max(h1, h2)</c> con <see cref="PourMode.Run"/> y <c>H_single = max(h3, h2)</c> con
        /// <see cref="PourMode.Single"/>. Bucle caliente: no valida <paramref name="mode"/> (cualquier valor distinto de
        /// <see cref="PourMode.Single"/> da <c>H</c>, que es admisible en los dos modos).
        /// </summary>
        public static int Estimate(Cuia[] cuias, int offset, int count, PourMode mode)
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
            int above = 0;
            int maskBits = 0;
            int union = 0;
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                int mask = Accumulate(cuias[i], ref breaks, ref above);
                maskBits += PopCount(mask);
                union |= mask;
            }

            return Combine(breaks, above, maskBits - PopCount(union), mode);
        }

        /// <summary><c>H = max(h1, h2)</c> de un tablero.</summary>
        public static int Estimate(BoardState state)
        {
            return Estimate(state, PourMode.Run);
        }

        /// <summary>
        /// Heurística del modo <paramref name="mode"/> de un tablero: <c>H</c> con <see cref="PourMode.Run"/> y
        /// <c>H_single</c> con <see cref="PourMode.Single"/>.
        /// </summary>
        public static int Estimate(BoardState state, PourMode mode)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            int breaks = 0;
            int above = 0;
            int maskBits = 0;
            int union = 0;
            for (int i = 0; i < state.Count; i++)
            {
                int mask = Accumulate(state[i], ref breaks, ref above);
                maskBits += PopCount(mask);
                union |= mask;
            }

            return Combine(breaks, above, maskBits - PopCount(union), mode);
        }

        private static int Combine(int breaks, int above, int dispersion, PourMode mode)
        {
            // h3 ≥ h1 siempre: en Single basta max(h3, h2).
            int local = mode == PourMode.Single ? above : breaks;
            return local > dispersion ? local : dispersion;
        }

        /// <summary>
        /// Suma los cortes de <paramref name="cuia"/> a <paramref name="breaks"/> y sus dosis por encima de la racha del
        /// fondo a <paramref name="above"/>, y devuelve su máscara de colores (una pasada).
        /// </summary>
        private static int Accumulate(Cuia cuia, ref int breaks, ref int above)
        {
            int count = cuia.Count;
            if (count == 0)
            {
                return 0;
            }

            int previous = cuia.ColorAt(0);
            int mask = 1 << (previous - 1);
            int bottomRun = count;
            for (int k = 1; k < count; k++)
            {
                int color = cuia.ColorAt(k);
                if (color != previous)
                {
                    if (bottomRun == count)
                    {
                        bottomRun = k;
                    }

                    breaks++;
                    mask |= 1 << (color - 1);
                    previous = color;
                }
            }

            above += count - bottomRun;
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
