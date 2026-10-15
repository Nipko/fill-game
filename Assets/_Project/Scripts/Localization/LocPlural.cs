#nullable enable
using System;

namespace AF.Localization
{
    /// <summary>Categoría plural de CLDR que usan los idiomas de la tanda 1 (zero, two y few no hacen falta todavía).</summary>
    public enum LocPluralCategory : byte
    {
        /// <summary><c>one</c>.</summary>
        One = 0,

        /// <summary><c>many</c>: la de los millones en español y portugués («1.000.000 de semillas»).</summary>
        Many = 1,

        /// <summary><c>other</c>.</summary>
        Other = 2,
    }

    /// <summary>
    /// Reglas plurales de CLDR (propuesta de idiomas §3.2, regla 3, y §3.6, medidas con ICU 77.1 y CLDR 47; nunca
    /// <c>n == 1</c>) para la sintaxis de la tabla <c>{n:plural:a|b[|c]}</c>:
    /// <list type="bullet">
    /// <item>es-419 (y en-XA, que es el español falseado): one, many y other; el 0 es other.</item>
    /// <item>pt-BR: el 0 y el 1 son one; many como en español. pt-PT: solo el 1 es one.</item>
    /// <item>en: one y other.</item>
    /// <item>Cualquier otro idioma, de momento, con la regla de en [P]: los de la tanda 2 se añaden con ella.</item>
    /// </list>
    /// Con 2 formas, many usa la segunda (other); con 3, son one|many|other. Sin asignaciones.
    /// </summary>
    public static class LocPlural
    {
        private enum Rules : byte
        {
            English,
            Spanish,
            PortugueseBrazil,
            PortuguesePortugal,
        }

        /// <summary>Categoría de un entero en <paramref name="locale"/>.</summary>
        public static LocPluralCategory Category(string locale, long value)
        {
            ulong n = value < 0 ? (ulong)(-(value + 1)) + 1UL : (ulong)value;
            return Select(RulesFor(locale), n, 0, 0UL);
        }

        /// <summary>
        /// Categoría de un número escrito con <paramref name="decimals"/> decimales (los operandos i, v y f de CLDR salen del
        /// número redondeado como se muestra: «1,0» es one en español y other en inglés).
        /// </summary>
        public static LocPluralCategory Category(string locale, double value, int decimals)
        {
            LocNumber.Split(value, decimals, out _, out ulong integer, out ulong fraction);
            return Select(RulesFor(locale), integer, decimals, fraction);
        }

        /// <summary>
        /// Forma que toca de <paramref name="formCount"/> (1-3): con 1, siempre esa; con 2, one|other (many usa other); con 3,
        /// one|many|other.
        /// </summary>
        public static int FormIndex(LocPluralCategory category, int formCount)
        {
            if (formCount <= 1)
            {
                return 0;
            }

            if (formCount == 2)
            {
                return category == LocPluralCategory.One ? 0 : 1;
            }

            return category == LocPluralCategory.One ? 0 : (category == LocPluralCategory.Many ? 1 : 2);
        }

        private static LocPluralCategory Select(Rules rules, ulong i, int v, ulong f)
        {
            // many (es, pt-BR y pt-PT): e = 0 and i != 0 and i % 1000000 = 0 and v = 0.
            bool many = i != 0UL && i % 1000000UL == 0UL && v == 0;
            switch (rules)
            {
                case Rules.Spanish:
                    // one: n = 1 (también «1,0»).
                    if (i == 1UL && f == 0UL)
                    {
                        return LocPluralCategory.One;
                    }

                    return many ? LocPluralCategory.Many : LocPluralCategory.Other;
                case Rules.PortugueseBrazil:
                    // one: i = 0..1.
                    if (i <= 1UL)
                    {
                        return LocPluralCategory.One;
                    }

                    return many ? LocPluralCategory.Many : LocPluralCategory.Other;
                case Rules.PortuguesePortugal:
                    // one: i = 1 and v = 0.
                    if (i == 1UL && v == 0)
                    {
                        return LocPluralCategory.One;
                    }

                    return many ? LocPluralCategory.Many : LocPluralCategory.Other;
                default:
                    // en — one: i = 1 and v = 0.
                    return i == 1UL && v == 0 ? LocPluralCategory.One : LocPluralCategory.Other;
            }
        }

        private static Rules RulesFor(string locale)
        {
            if (locale == null)
            {
                throw new ArgumentNullException(nameof(locale));
            }

            if (LocLocale.IsPseudo(locale) || LocLocale.HasLanguage(locale, "es"))
            {
                return Rules.Spanish;
            }

            if (LocLocale.HasLanguage(locale, "pt"))
            {
                return LocLocale.HasRegion(locale, "PT") ? Rules.PortuguesePortugal : Rules.PortugueseBrazil;
            }

            return Rules.English;
        }
    }
}
