#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Solver.Generation
{
    /// <summary>
    /// Especificación de un tablero para el barajado inverso (03 §4.1-§4.2): <c>K</c> colores, <c>E</c> vacíos en el
    /// resultado, capacidades por cuia y <c>U_c</c> por color (02 §8: Cuia chica, Cuia alta con su fruta abundante), más los
    /// filtros <c>compl_min</c> y el tope de profundidad del paseo.
    /// </summary>
    /// <remarks>
    /// El tablero resuelto de partida es el del prototipo: recorriendo <c>caps</c> en orden, la cuia se llena con el
    /// siguiente color si su capacidad es igual a su <c>U_c</c>; si no, empieza vacía. Todas las cuias de <c>caps</c> que no
    /// alojan un color empiezan vacías (Cuia chica, vacíos estándar).
    /// </remarks>
    public sealed class InverseSpec
    {
        /// <summary>Crea una especificación.</summary>
        /// <param name="caps">Capacidad de cada cuia (1-6), T ≤ 16.</param>
        /// <param name="units">U_c de los colores 1..K (<c>units[c − 1]</c>).</param>
        /// <param name="empties">Vacíos exigidos en el tablero devuelto (<c>E</c>).</param>
        /// <param name="minComplexity"><c>compl_min</c>; −1 = 3K, como el prototipo.</param>
        /// <param name="maxDepth">Tope de vertidos inversos de <c>WALK</c>; −1 = 6K, como el prototipo.</param>
        /// <param name="mode">Modo de vertido del que es inverso (03 §4.1; <c>run</c> en producción, D-005).</param>
        public InverseSpec(int[] caps, int[] units, int empties, int minComplexity = -1, int maxDepth = -1, PourMode mode = PourMode.Run)
        {
            if (caps == null || units == null)
            {
                throw new ArgumentNullException(caps == null ? nameof(caps) : nameof(units));
            }

            if (caps.Length < 1 || caps.Length > BoardState.MaxCuias)
            {
                throw new ArgumentOutOfRangeException(nameof(caps), caps.Length, "R1: entre 1 y 16 cuias.");
            }

            if (units.Length < 1 || units.Length > Cuia.MaxTintColor)
            {
                throw new ArgumentOutOfRangeException(nameof(units), units.Length, "Entre 1 y 14 colores.");
            }

            if (empties < 0 || empties >= caps.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(empties), empties, "Vacíos entre 0 y T − 1.");
            }

            if (!PourModes.IsDefined(mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo de vertido desconocido.");
            }

            Caps = (int[])caps.Clone();
            Units = (int[])units.Clone();
            Colors = units.Length;
            Empties = empties;
            MinComplexity = minComplexity >= 0 ? minComplexity : 3 * Colors;
            MaxDepth = maxDepth >= 1 ? maxDepth : 6 * Colors;
            Mode = mode;
            Solved = BuildSolved();
        }

        /// <summary>Capacidades.</summary>
        public int[] Caps { get; }

        /// <summary>U_c de los colores 1..K.</summary>
        public int[] Units { get; }

        /// <summary>K.</summary>
        public int Colors { get; }

        /// <summary>E.</summary>
        public int Empties { get; }

        /// <summary><c>compl_min</c>.</summary>
        public int MinComplexity { get; }

        /// <summary>Tope de profundidad de <c>WALK</c>.</summary>
        public int MaxDepth { get; }

        /// <summary>Modo de vertido.</summary>
        public PourMode Mode { get; }

        /// <summary>Tablero resuelto de partida.</summary>
        public BoardState Solved { get; }

        /// <summary>Uniforme: K colores de 4 dosis en cuias de 4 más E vacías de 4.</summary>
        public static InverseSpec Uniform(int colors, int empties, PourMode mode = PourMode.Run)
        {
            var caps = new int[colors + empties];
            var units = new int[colors];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = Cuia.DefaultCapacity;
            }

            for (int c = 0; c < colors; c++)
            {
                units[c] = UnitsPerColor.DefaultUnits;
            }

            return new InverseSpec(caps, units, empties, -1, -1, mode);
        }

        private BoardState BuildSolved()
        {
            var cuias = new Cuia[Caps.Length];
            int color = 1;
            for (int i = 0; i < Caps.Length; i++)
            {
                if (color <= Colors && Caps[i] == Units[color - 1])
                {
                    var doses = new int[Caps[i]];
                    for (int k = 0; k < doses.Length; k++)
                    {
                        doses[k] = color;
                    }

                    cuias[i] = Cuia.Create(Caps[i], doses);
                    color++;
                }
                else
                {
                    cuias[i] = Cuia.Empty(Caps[i]);
                }
            }

            if (color <= Colors)
            {
                throw new ArgumentException(
                    "Las capacidades no alojan los " + Colors.ToString(CultureInfo.InvariantCulture)
                    + " colores: cada color necesita, en orden, una cuia con cap = U_c (02 §8).", nameof(Caps));
            }

            return new BoardState(cuias);
        }
    }

    /// <summary>Resultado de un generador inverso.</summary>
    public readonly struct InverseResult
    {
        internal InverseResult(BoardState? state, int attempts, int depth, int complexity, int nodes)
        {
            State = state;
            Attempts = attempts;
            Depth = depth;
            Complexity = complexity;
            Nodes = nodes;
        }

        /// <summary>Tablero inicial generado, o null si ningún intento cumplió los filtros.</summary>
        public BoardState? State { get; }

        /// <summary>Intentos (sub-semillas) usados.</summary>
        public int Attempts { get; }

        /// <summary>Vertidos inversos desde el tablero resuelto hasta el devuelto.</summary>
        public int Depth { get; }

        /// <summary>Complejidad del tablero devuelto.</summary>
        public int Complexity { get; }

        /// <summary>Estados visitados en el intento que tuvo éxito (DEEP) o pasos dados (WALK).</summary>
        public int Nodes { get; }

        /// <summary>Hay tablero.</summary>
        public bool Succeeded => State != null;
    }
}
