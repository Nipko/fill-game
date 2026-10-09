#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Core.Board
{
    /// <summary>
    /// <c>units_per_color</c> de <c>af-level/1</c> (08 §4.1): un entero (4 para todos los colores) o un objeto con las
    /// excepciones, como <c>{"13": 5}</c> para la fruta abundante de una Cuia alta (02 §8); los colores que el objeto no
    /// nombra valen <see cref="DefaultUnits"/>.
    /// </summary>
    public sealed class UnitsPerColor
    {
        /// <summary>Dosis por color por defecto (02 R3).</summary>
        public const int DefaultUnits = 4;

        private readonly int[] _table = new int[Cuia.MaxColor + 1];

        private UnitsPerColor(int defaultUnits)
        {
            if (defaultUnits < 1 || defaultUnits > Cuia.MaxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultUnits), defaultUnits, "U_c va de 1 a 6 (02 R3).");
            }

            Default = defaultUnits;
            for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
            {
                _table[c] = defaultUnits;
            }
        }

        /// <summary>Valor para los colores sin excepción.</summary>
        public int Default { get; }

        /// <summary>La forma entera: todos los colores con <paramref name="units"/> dosis.</summary>
        public static UnitsPerColor Uniform(int units)
        {
            return new UnitsPerColor(units);
        }

        /// <summary>
        /// La forma objeto: <paramref name="overrides"/> (color → dosis) y <see cref="DefaultUnits"/> para el resto.
        /// Rellenar la tabla no depende del orden del diccionario.
        /// </summary>
        public static UnitsPerColor FromObject(IReadOnlyDictionary<int, int> overrides)
        {
            if (overrides == null)
            {
                throw new ArgumentNullException(nameof(overrides));
            }

            var units = new UnitsPerColor(DefaultUnits);
            int badColor = int.MaxValue; // el menor color inválido, para que el error no dependa del orden del diccionario
            foreach (KeyValuePair<int, int> pair in overrides)
            {
                bool valid = pair.Key >= Cuia.MinColor && pair.Key <= Cuia.MaxColor && pair.Value >= 1 && pair.Value <= Cuia.MaxCapacity;
                if (!valid)
                {
                    badColor = Math.Min(badColor, pair.Key);
                    continue;
                }

                units._table[pair.Key] = pair.Value;
            }

            if (badColor != int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(overrides), badColor, "units_per_color: el color va de 1 a 16 y U_c de 1 a 6 (02 R2, R3).");
            }

            return units;
        }

        /// <summary>U_c declarado para <paramref name="color"/>.</summary>
        public int Of(int color)
        {
            if (color < Cuia.MinColor || color > Cuia.MaxColor)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "R2: el color va de 1 a 16.");
            }

            return _table[color];
        }
    }
}
