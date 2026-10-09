#nullable enable
using System;
using System.Globalization;

namespace AF.Core.Mechanics
{
    /// <summary>
    /// Espejo de un <c>attrs[i]</c> de <c>af-level/1</c> (08 §4.1): <c>{type, key, partner, n, k}</c>, con los campos que
    /// usa cada mecánica según 02 §9.1 y D-007. Es la entrada de <see cref="Board.BoardBuilder"/>; dentro de la cuia los
    /// atributos viven empaquetados en <see cref="Board.Cuia.Packed"/>.
    /// </summary>
    /// <remarks>
    /// Un campo ausente vale 0 (<see cref="Key"/>, <see cref="N"/>, <see cref="K"/>) o −1 (<see cref="Partner"/>, porque 0
    /// es un índice válido). <c>default(MechanicSpec)</c> es <see cref="None"/>: el <c>null</c> del JSON.
    /// </remarks>
    public readonly struct MechanicSpec : IEquatable<MechanicSpec>
    {
        private readonly int _partnerPlusOne;

        /// <summary>Crea la especificación de una mecánica.</summary>
        /// <param name="type">Tipo (<c>type</c>).</param>
        /// <param name="key">Tinte llave (<c>key</c>, 1-16) o 0 si falta.</param>
        /// <param name="partner">Índice 0-based de la pareja del Remolino (<c>partner</c>) o −1 si falta.</param>
        /// <param name="n">Sellos de la Liana de cuenta o gotas de la Lluvia (<c>n</c>) o 0 si falta.</param>
        /// <param name="k">Ritmo en vertidos de Lluvia o Piraña (<c>k</c>) o 0 si falta.</param>
        public MechanicSpec(MechanicType type, int key = 0, int partner = -1, int n = 0, int k = 0)
        {
            if (!MechanicTypes.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Mecánica desconocida.");
            }

            if (partner < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(partner), partner, "partner es un índice 0-based, o −1 si falta.");
            }

            Type = type;
            Key = key;
            _partnerPlusOne = partner + 1;
            N = n;
            K = k;
        }

        /// <summary>Sin mecánica (<c>null</c> en el JSON).</summary>
        public static MechanicSpec None => default;

        /// <summary>Tipo de mecánica.</summary>
        public MechanicType Type { get; }

        /// <summary>Tinte llave (1-16) o 0 si falta.</summary>
        public int Key { get; }

        /// <summary>Índice de la pareja del Remolino o −1 si falta.</summary>
        public int Partner => _partnerPlusOne - 1;

        /// <summary><c>n</c> o 0 si falta.</summary>
        public int N { get; }

        /// <summary><c>k</c> o 0 si falta.</summary>
        public int K { get; }

        /// <summary>No hay mecánica.</summary>
        public bool IsNone => Type == MechanicType.None;

        /// <inheritdoc />
        public bool Equals(MechanicSpec other)
        {
            return Type == other.Type && Key == other.Key && _partnerPlusOne == other._partnerPlusOne && N == other.N && K == other.K;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is MechanicSpec other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ((int)Type << 24) ^ (Key << 16) ^ (_partnerPlusOne << 8) ^ (N << 4) ^ K;
        }

        /// <summary>Forma legible del JSON: <c>{type:hoja, key:4}</c>; <c>null</c> sin mecánica.</summary>
        public override string ToString()
        {
            if (IsNone)
            {
                return "null";
            }

            string text = "{type:" + MechanicTypes.ToKey(Type);
            if (Key != 0)
            {
                text += ", key:" + Key.ToString(CultureInfo.InvariantCulture);
            }

            if (Partner >= 0)
            {
                text += ", partner:" + Partner.ToString(CultureInfo.InvariantCulture);
            }

            if (N != 0)
            {
                text += ", n:" + N.ToString(CultureInfo.InvariantCulture);
            }

            if (K != 0)
            {
                text += ", k:" + K.ToString(CultureInfo.InvariantCulture);
            }

            return text + "}";
        }
    }
}
