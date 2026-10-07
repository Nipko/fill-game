#nullable enable
using System;
using System.Globalization;
using AF.Core.Board;

namespace AF.Core.Rules
{
    /// <summary>
    /// Un vertido A→B y las dosis que mueve (R9): <c>cantidad(A, B)</c> en el estado en que se hizo.
    /// Un vertido, parcial o completo, cuenta como 1 movimiento.
    /// </summary>
    public readonly struct Pour : IEquatable<Pour>
    {
        /// <summary>Crea un vertido.</summary>
        /// <param name="source">Índice de la cuia origen A (0..15).</param>
        /// <param name="target">Índice de la cuia destino B (0..15), distinto de A.</param>
        /// <param name="amount">Dosis que se mueven (1..6).</param>
        public Pour(int source, int target, int amount)
        {
            if ((uint)source >= BoardState.MaxCuias)
            {
                throw new ArgumentOutOfRangeException(nameof(source), source, "Índice de cuia fuera de rango (0..15).");
            }

            if ((uint)target >= BoardState.MaxCuias || target == source)
            {
                throw new ArgumentOutOfRangeException(nameof(target), target, "El destino debe ser otra cuia (0..15).");
            }

            if (amount < 1 || amount > Cuia.MaxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Un vertido mueve entre 1 y 6 dosis.");
            }

            Source = source;
            Target = target;
            Amount = amount;
        }

        /// <summary>Índice de la cuia origen A.</summary>
        public int Source { get; }

        /// <summary>Índice de la cuia destino B.</summary>
        public int Target { get; }

        /// <summary>Dosis movidas: <c>cantidad(A, B)</c>.</summary>
        public int Amount { get; }

        /// <inheritdoc />
        public bool Equals(Pour other)
        {
            return Source == other.Source && Target == other.Target && Amount == other.Amount;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Pour other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Source << 8) | (Target << 4) | Amount;
        }

        /// <summary>Igualdad por valor.</summary>
        public static bool operator ==(Pour left, Pour right)
        {
            return left.Equals(right);
        }

        /// <summary>Desigualdad por valor.</summary>
        public static bool operator !=(Pour left, Pour right)
        {
            return !left.Equals(right);
        }

        /// <summary>Forma legible para tests y registros: <c>Pour(0->2 x1)</c>.</summary>
        public override string ToString()
        {
            return "Pour(" + Source.ToString(CultureInfo.InvariantCulture)
                + "->" + Target.ToString(CultureInfo.InvariantCulture)
                + " x" + Amount.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}
