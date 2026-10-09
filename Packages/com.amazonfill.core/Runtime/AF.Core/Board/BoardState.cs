#nullable enable
using System;
using System.Text;
using AF.Core.Rules;

namespace AF.Core.Board
{
    /// <summary>
    /// Estado inmutable de un tablero: lista ordenada de cuias (R1) y unidades por color <c>U_c</c> (R3).
    /// Cada vertido produce un estado nuevo (GDD 08 §2.1, principio 5).
    /// </summary>
    /// <remarks>
    /// Las cuias pueden llevar mecánica (<see cref="Cuia.Mechanic"/>; los niveles se construyen con
    /// <see cref="BoardBuilder"/>, que valida sus atributos). Admite el color 15 (Barro, 02 §9.1 M5: dosis inerte; la tabla
    /// <c>U_c</c> cuenta en la posición 15 las dosis de Barro, que no son un color de <c>units_per_color</c>) y rechaza el 16
    /// (reservado, 02 R2). Con el constructor público <c>U_c</c> se cuenta a partir del contenido inicial;
    /// <see cref="BoardBuilder"/> suma además las gotas pendientes de la Lluvia (02 R3).
    /// Esta clase asigna memoria en cada vertido: sirve para reglas, cliente y tests, no para el bucle
    /// caliente del solver, que usa su propia representación empaquetada (AF.Solver).
    /// La igualdad compara <see cref="Cuia.Packed"/> cuia a cuia y en orden (no es la igualdad canónica:
    /// para esa, <see cref="CanonicalHash"/> y <see cref="BoardCanon"/>).
    /// </remarks>
    public sealed class BoardState : IEquatable<BoardState>
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
                    if (color == Cuia.ReservedColor)
                    {
                        throw new NotSupportedException("El color 16 está reservado (02 R2): ninguna regla lo usa.");
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

        /// <summary>
        /// Tablero ya validado por <see cref="BoardBuilder"/>, con su tabla <c>U_c</c> (que puede incluir dosis que aún no
        /// están en el tablero, como las gotas pendientes de la Lluvia). Toma posesión de los dos arrays.
        /// </summary>
        internal static BoardState FromValidated(Cuia[] cuias, int[] units)
        {
            return new BoardState(cuias, units);
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

        /// <summary>Victoria (R12): toda cuia está vacía o sellada (<see cref="BoardRules.IsWon"/>).</summary>
        public bool IsWon()
        {
            return BoardRules.IsWon(_cuias, 0, _cuias.Length, _units);
        }

        /// <summary>Alguna cuia tiene dosis ocultas (Aguas turbias, 02 §11).</summary>
        public bool HasHidden
        {
            get
            {
                for (int i = 0; i < _cuias.Length; i++)
                {
                    if (_cuias[i].HasHidden)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Número de dosis ocultas del tablero (Aguas turbias, 02 §11).</summary>
        public int HiddenCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _cuias.Length; i++)
                {
                    count += _cuias[i].HiddenCount;
                }

                return count;
            }
        }

        /// <summary>
        /// Hash de 64 bits del estado canónico (03 §3): igual para todas las permutaciones de las mismas cuias.
        /// Asigna un array temporal: no es para el bucle caliente (allí, <see cref="BoardCanon"/> sobre un búfer propio).
        /// </summary>
        public ulong CanonicalHash()
        {
            var sorted = (Cuia[])_cuias.Clone();
            BoardCanon.Sort(sorted, 0, sorted.Length);
            return BoardCanon.Hash(sorted, 0, sorted.Length);
        }

        /// <summary>Igualdad por valor: mismas cuias (<see cref="Cuia.Packed"/>) en el mismo orden.</summary>
        public bool Equals(BoardState? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || other._cuias.Length != _cuias.Length)
            {
                return false;
            }

            for (int i = 0; i < _cuias.Length; i++)
            {
                if (_cuias[i].Packed != other._cuias[i].Packed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as BoardState);
        }

        /// <summary>Hash determinista del estado en orden (FNV-1a de <see cref="BoardCanon"/>, plegado a 32 bits).</summary>
        public override int GetHashCode()
        {
            ulong hash = BoardCanon.Hash(_cuias, 0, _cuias.Length);
            return unchecked((int)hash ^ (int)(hash >> 32));
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

        /// <summary>
        /// Aplica un vertido ya validado (legal) sobre una copia con <see cref="BoardRules"/>, la única implementación que
        /// aplica vertidos: R10 (a)-(f′) en el orden del anexo B, con lo ocurrido en <paramref name="events"/>.
        /// </summary>
        internal BoardState WithPour(int source, int target, PourMode mode, bool isFutile, ref PourEvents events)
        {
            var next = (Cuia[])_cuias.Clone();
            BoardRules.ApplyInPlace(next, 0, next.Length, _units, source, target, mode, isFutile, ref events);
            return new BoardState(next, _units);
        }

        /// <summary>
        /// El mismo tablero con las capas de <paramref name="revealedByCuia"/>[i] de la cuia i visibles (máscara de revelado
        /// de la partida, 02 T6). Devuelve este mismo objeto si no cambia nada. Las cuias sin entrada (índice ≥ longitud) no
        /// cambian.
        /// </summary>
        internal BoardState WithRevealed(int[] revealedByCuia)
        {
            Cuia[]? next = null;
            int limit = revealedByCuia.Length < _cuias.Length ? revealedByCuia.Length : _cuias.Length;
            for (int i = 0; i < limit; i++)
            {
                Cuia cuia = _cuias[i];
                if ((cuia.HiddenMask & revealedByCuia[i]) == 0)
                {
                    continue;
                }

                next ??= (Cuia[])_cuias.Clone();
                next[i] = cuia.WithRevealed(revealedByCuia[i] & cuia.HiddenMask);
            }

            return next == null ? this : new BoardState(next, _units);
        }

        /// <summary>Tabla <c>U_c</c> indexada por color (0..16) para <see cref="CuiaRules"/>. No se modifica.</summary>
        internal int[] UnitsTable => _units;

        /// <summary>Array interno de cuias, sin copiar, para <see cref="BoardRules"/>. Nadie lo modifica: el estado es inmutable.</summary>
        internal Cuia[] CuiasArray => _cuias;

        /// <summary>Cuia <paramref name="index"/> sin comprobar el índice (el llamante ya lo hizo).</summary>
        internal Cuia CuiaAt(int index)
        {
            return _cuias[index];
        }

        private bool IsSealedCuia(Cuia cuia)
        {
            return CuiaRules.IsSealed(cuia, _units);
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
