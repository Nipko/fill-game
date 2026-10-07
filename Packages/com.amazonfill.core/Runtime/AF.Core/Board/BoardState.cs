#nullable enable
using System;
using System.Text;

namespace AF.Core.Board
{
    /// <summary>
    /// Estado inmutable de un tablero: lista ordenada de cuias (R1) y unidades por color <c>U_c</c> (R3).
    /// Cada vertido produce un estado nuevo (GDD 08 §2.1, principio 5).
    /// </summary>
    /// <remarks>
    /// Alcance actual: núcleo canónico sin mecánicas (GDD 02 §9 aún sin portar). Por eso se rechazan
    /// los colores 15 (Barro) y 16 (reservado) hasta que exista su mecánica. <c>U_c</c> se cuenta a
    /// partir del contenido inicial; cuando se porte Lluvia habrá que sumar las dosis que aún no han caído.
    /// Esta clase asigna memoria en cada vertido: sirve para reglas, cliente y tests, no para el bucle
    /// caliente del solver, que usará su propia representación empaquetada.
    /// </remarks>
    public sealed class BoardState
    {
        /// <summary>Máximo de cuias en pantalla, contando Cuia extra y Cuia de rescate (R1).</summary>
        public const int MaxCuias = 16;

        private readonly Cuia[] _cuias;

        // Índice = color (1..16); la posición 0 no se usa.
        private readonly int[] _units;

        /// <summary>Crea un estado a partir de una copia de <paramref name="cuias"/>.</summary>
        /// <param name="cuias">Cuias en orden de pantalla; entre 1 y <see cref="MaxCuias"/>.</param>
        public BoardState(Cuia[] cuias)
        {
            if (cuias == null)
            {
                throw new ArgumentNullException(nameof(cuias));
            }

            if (cuias.Length < 1 || cuias.Length > MaxCuias)
            {
                throw new ArgumentOutOfRangeException(nameof(cuias), cuias.Length, "R1: un tablero tiene entre 1 y 16 cuias.");
            }

            var copy = new Cuia[cuias.Length];
            var units = new int[Cuia.MaxColor + 1];
            for (int i = 0; i < cuias.Length; i++)
            {
                Cuia cuia = cuias[i];
                if (cuia.Capacity < Cuia.MinCapacity)
                {
                    throw new ArgumentException("default(Cuia) no es una cuia válida: usa Cuia.Empty o Cuia.Create.", nameof(cuias));
                }

                for (int k = 0; k < cuia.Count; k++)
                {
                    int color = cuia.ColorAt(k);
                    if (color > Cuia.MaxTintColor)
                    {
                        throw new NotSupportedException("Los colores 15 (Barro) y 16 (reservado) necesitan las mecánicas de GDD 02 §9, que aún no están portadas.");
                    }

                    units[color]++;
                }

                copy[i] = cuia;
            }

            _cuias = copy;
            _units = units;
        }

        private BoardState(Cuia[] cuias, int[] units)
        {
            _cuias = cuias;
            _units = units;
        }

        /// <summary>Número de cuias (T en R1).</summary>
        public int Count => _cuias.Length;

        /// <summary>Cuia en la posición <paramref name="index"/>.</summary>
        public Cuia this[int index]
        {
            get
            {
                CheckIndex(index, nameof(index));
                return _cuias[index];
            }
        }

        /// <summary><c>U_c</c>: dosis totales del color <paramref name="color"/> en el nivel (R3).</summary>
        public int UnitsOf(int color)
        {
            if (color < Cuia.MinColor || color > Cuia.MaxColor)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "R2: el color va de 1 a 16.");
            }

            return _units[color];
        }

        /// <summary>
        /// Cuia sellada (R5): llena, monocolor, sin dosis ocultas y con todas las dosis de su color.
        /// Una cuia de capacidad menor que <c>U_c</c> nunca sella ese color: es un búfer.
        /// </summary>
        public bool IsSealed(int index)
        {
            CheckIndex(index, nameof(index));
            return IsSealedCuia(_cuias[index]);
        }

        /// <summary>Victoria (R12): toda cuia está vacía o sellada.</summary>
        public bool IsWon()
        {
            for (int i = 0; i < _cuias.Length; i++)
            {
                Cuia cuia = _cuias[i];
                if (!cuia.IsEmpty && !IsSealedCuia(cuia))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Forma legible para tests y registros.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder(16 + (_cuias.Length * 24));
            sb.Append("BoardState[");
            for (int i = 0; i < _cuias.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(_cuias[i].ToString());
            }

            return sb.Append(']').ToString();
        }

        /// <summary>Aplica un vertido ya validado por <see cref="Rules.PourRules"/>.</summary>
        internal BoardState WithPour(int source, int target, int amount)
        {
            var next = (Cuia[])_cuias.Clone();
            Cuia from = next[source];
            int color = from.TopColor;
            next[source] = from.RemoveTop(amount);
            next[target] = next[target].AddOnTop(color, amount);
            return new BoardState(next, _units);
        }

        private bool IsSealedCuia(Cuia cuia)
        {
            return !cuia.IsEmpty
                && cuia.IsFull
                && !cuia.HasHidden
                && cuia.IsMonocolor
                && cuia.Capacity == _units[cuia.TopColor];
        }

        private void CheckIndex(int index, string paramName)
        {
            if ((uint)index >= (uint)_cuias.Length)
            {
                throw new ArgumentOutOfRangeException(paramName, index, "Índice de cuia fuera del tablero.");
            }
        }
    }
}
