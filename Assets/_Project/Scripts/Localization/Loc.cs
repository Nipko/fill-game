#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Localization
{
    /// <summary>
    /// Fachada de textos del juego (D-018; 08 §2.2, <c>AF.Localization</c>; contrato 32; arquitectura de la tanda 4, §1): la
    /// única puerta a las tablas de textos, sin motor y sin paquetes. En la fase 3, Unity Localization 1.5.13 irá detrás de
    /// esta misma fachada sin tocar las pantallas.
    /// <list type="bullet">
    /// <item><see cref="T(string)"/>: textos de UI (<c>strings.csv</c>). Si falta la clave o el texto en el idioma, da
    /// «[clave]» en desarrollo y «» en release.</item>
    /// <item><see cref="Fact(string)"/>: textos largos (<c>facts.csv</c>). Da «» si falta la clave, si no hay texto en el idioma
    /// (nunca mezcla idiomas; Q10-A) o si no está aprobado y no se enseña lo no aprobado (<see cref="ShowUnapproved"/>).</item>
    /// <item>Cifras (<see cref="Number(long)"/>, <see cref="WriteNumber"/>) con los separadores del idioma del juego y, en
    /// español, del país (<see cref="SetNumberCountry"/>; <see cref="LocLocale.NumberTagFor"/>; observación 22 de la jornada 6,
    /// decisión del dueño del 2026-10-30): en «1,000», pt-BR «1.000» y es-419 según el país (es-CO [P] si no se sabe). Una fila
    /// forzada (<see cref="SetNumberTag"/>, tests y desarrollo) manda sobre las dos. Los precios, con el mismo formato
    /// (<see cref="Price"/>; D-025).</item>
    /// <item><c>showUnapproved</c> es el interruptor de desarrollo: en los builds de desarrollo y en los tests enseña lo no
    /// aprobado, marca las claves que faltan y permite en-XA.</item>
    /// </list>
    /// <see cref="T(string)"/>, <see cref="Fact(string)"/> y <see cref="WriteNumber"/> no asignan memoria: los textos sin
    /// argumentos se preparan al configurar o cambiar de idioma. Los que llevan argumentos se formatean al construir la
    /// pantalla, nunca por fotograma. Solo desde el hilo principal.
    /// </summary>
    public static class Loc
    {
        private static LocCatalog? s_catalog;
        private static LocCatalog s_active = LocCatalog.Empty;
        private static string s_locale = LocLocale.SourceLocale;
        private static bool s_showUnapproved = true;
        private static string? s_numberTag;
        private static string? s_numberCountry;
        private static LocNumberFormat s_numbers = LocNumber.For(LocLocale.DefaultNumberTag(LocLocale.SourceLocale));
        private static string[] s_uiTemplate = Array.Empty<string>();
        private static string[] s_uiText = Array.Empty<string>();
        private static string[] s_factTemplate = Array.Empty<string>();
        private static string[] s_factText = Array.Empty<string>();
        private static readonly Dictionary<string, string> s_missing = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Cambió el idioma, la región de las cifras o el catálogo (también al configurar): las pantallas y las cachés de texto
        /// se rehacen (Q7-A: cambiar de idioma vuelve al inicio y reconstruye la UI).
        /// </summary>
        public static event Action? Changed;

        /// <summary>Idioma de la UI («es-419», «pt-BR», «en» o «en-XA»).</summary>
        public static string Locale => s_locale;

        /// <summary>Si ya se llamó a <see cref="Configure(LocCatalog, string, bool)"/> (sin configurar, el catálogo está vacío).</summary>
        public static bool IsConfigured => s_catalog != null;

        /// <summary>El catálogo activo (<see cref="LocCatalog.Empty"/> sin configurar).</summary>
        public static LocCatalog Catalog => s_active;

        /// <summary>Si se enseña lo no aprobado, se marcan las claves que faltan y vale en-XA (desarrollo y tests).</summary>
        public static bool ShowUnapproved => s_showUnapproved;

        /// <summary>Fila de cifras forzada (tests y desarrollo), o null si se deduce del idioma y del país.</summary>
        public static string? RequestedNumberTag => s_numberTag;

        /// <summary>País de las cifras (ISO alfa-2 en mayúsculas), o null si no se sabe (observación 22 de la jornada 6).</summary>
        public static string? NumberCountry => s_numberCountry;

        /// <summary>Formato de cifras en uso (su <see cref="LocNumberFormat.Tag"/> es la fila de la tabla: «es-CO», «en»…).</summary>
        public static LocNumberFormat NumberFormat => s_numbers;

        /// <summary>
        /// Configura la fachada con <paramref name="catalog"/> en <paramref name="locale"/> (ya resuelto con
        /// <see cref="LocLocale.Resolve"/>), con las cifras del idioma. Lanza <see cref="Changed"/>.
        /// </summary>
        public static void Configure(LocCatalog catalog, string locale, bool showUnapproved)
        {
            Configure(catalog, locale, showUnapproved, null);
        }

        /// <summary>
        /// Como <see cref="Configure(LocCatalog, string, bool)"/>, con una fila de cifras forzada (<paramref name="numberTag"/>,
        /// como «es-MX»; null = la del idioma, sin país). Para tests y desarrollo.
        /// </summary>
        public static void Configure(LocCatalog catalog, string locale, bool showUnapproved, string? numberTag)
        {
            Configure(catalog, locale, showUnapproved, numberTag, null);
        }

        /// <summary>
        /// Como <see cref="Configure(LocCatalog, string, bool, string)"/>, con el país de las cifras (<paramref name="numberCountry"/>,
        /// ISO alfa-2; null = desconocido): con la UI en español decide el separador (<see cref="LocLocale.NumberTagFor"/>).
        /// </summary>
        public static void Configure(LocCatalog catalog, string locale, bool showUnapproved, string? numberTag, string? numberCountry)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            CheckLocale(catalog, locale, showUnapproved);
            s_catalog = catalog;
            s_active = catalog;
            s_locale = locale;
            s_showUnapproved = showUnapproved;
            s_numberTag = Normalize(numberTag);
            s_numberCountry = LocLocale.CountryCode(numberCountry);
            Rebuild();
        }

        /// <summary>Vuelve al estado sin configurar (catálogo vacío, es-419, modo de desarrollo). Lanza <see cref="Changed"/>.</summary>
        public static void Reset()
        {
            s_catalog = null;
            s_active = LocCatalog.Empty;
            s_locale = LocLocale.SourceLocale;
            s_showUnapproved = true;
            s_numberTag = null;
            s_numberCountry = null;
            Rebuild();
        }

        /// <summary>
        /// Cambia el idioma de la UI (una columna del catálogo, o en-XA con <see cref="ShowUnapproved"/>). Si cambia, rehace
        /// los textos y lanza <see cref="Changed"/>.
        /// </summary>
        public static void SetLocale(string locale)
        {
            CheckLocale(s_active, locale, s_showUnapproved);
            if (string.Equals(locale, s_locale, StringComparison.Ordinal))
            {
                return;
            }

            s_locale = locale;
            Rebuild();
        }

        /// <summary>
        /// Fuerza la fila de las cifras (tests y desarrollo; null o vacío = la del idioma y el país). Si cambian los
        /// separadores, rehace los textos y lanza <see cref="Changed"/>; si solo cambia la fila (es-CO → es-AR), no.
        /// </summary>
        public static void SetNumberTag(string? tag)
        {
            string? normalized = Normalize(tag);
            if (string.Equals(normalized, s_numberTag, StringComparison.Ordinal))
            {
                return;
            }

            s_numberTag = normalized;
            ApplyNumbers();
        }

        /// <summary>
        /// Fija el país de las cifras (ISO alfa-2; null o no válido = desconocido; observación 22 de la jornada 6). Solo cuenta
        /// con la UI en español. Si cambian los separadores, rehace los textos y lanza <see cref="Changed"/>.
        /// </summary>
        public static void SetNumberCountry(string? country)
        {
            string? code = LocLocale.CountryCode(country);
            if (string.Equals(code, s_numberCountry, StringComparison.Ordinal))
            {
                return;
            }

            s_numberCountry = code;
            ApplyNumbers();
        }

        private static void ApplyNumbers()
        {
            LocNumberFormat next = ResolveNumbers();
            if (next.Group == s_numbers.Group && next.Decimal == s_numbers.Decimal)
            {
                s_numbers = next;
                return;
            }

            Rebuild();
        }

        /// <summary>Texto de UI de <paramref name="key"/> en el idioma actual. Sin asignaciones si la clave existe.</summary>
        public static string T(string key)
        {
            int row = s_active.Strings.IndexOf(key);
            if (row >= 0)
            {
                string text = s_uiText[row];
                if (text.Length > 0)
                {
                    return text;
                }
            }

            return Missing(key);
        }

        /// <summary>Texto de UI de <paramref name="key"/> con <paramref name="args"/>; cada <c>{sep}</c>, un salto de línea.</summary>
        public static string T(string key, in LocArgs args)
        {
            return T(key, args, LocFormat.LineBreak, 0);
        }

        /// <summary>
        /// Texto de UI con <paramref name="args"/>; el <c>{sep}</c> número i es un salto de línea si el bit i de
        /// <paramref name="breaks"/> está puesto y <paramref name="separator"/> si no (<see cref="LocFormat"/>).
        /// </summary>
        public static string T(string key, in LocArgs args, string separator, int breaks)
        {
            int row = s_active.Strings.IndexOf(key);
            if (row < 0 || s_uiTemplate[row].Length == 0)
            {
                return Missing(key);
            }

            return LocFormat.Format(s_uiTemplate[row], args, s_active.Strings.ArgsAt(row), s_locale, s_numbers, separator, breaks);
        }

        /// <summary>
        /// Texto largo de <paramref name="key"/> en el idioma actual, con las cifras de su columna <c>args</c>; «» si falta, si
        /// no hay traducción o si no está aprobado (con <see cref="ShowUnapproved"/> apagado). Sin asignaciones.
        /// </summary>
        public static string Fact(string key)
        {
            int row = s_active.Facts.IndexOf(key);
            return row < 0 ? string.Empty : s_factText[row];
        }

        /// <summary>
        /// Como <see cref="Fact(string)"/>, con <paramref name="args"/> (que mandan sobre los de la columna <c>args</c>).
        /// </summary>
        public static string Fact(string key, in LocArgs args)
        {
            int row = s_active.Facts.IndexOf(key);
            if (row < 0 || s_factTemplate[row].Length == 0)
            {
                return string.Empty;
            }

            return LocFormat.Format(s_factTemplate[row], args, s_active.Facts.ArgsAt(row), s_locale, s_numbers, LocFormat.LineBreak, 0);
        }

        /// <summary>
        /// Plantilla de <paramref name="key"/> en el idioma actual (la de UI o la de textos largos que se enseñaría), o null si
        /// no hay: para contar sus <c>{sep}</c> (<see cref="LocFormat.SeparatorCount"/>).
        /// </summary>
        public static string? Template(string key)
        {
            int row = s_active.Strings.IndexOf(key);
            if (row >= 0)
            {
                return s_uiTemplate[row].Length > 0 ? s_uiTemplate[row] : null;
            }

            row = s_active.Facts.IndexOf(key);
            return row >= 0 && s_factTemplate[row].Length > 0 ? s_factTemplate[row] : null;
        }

        /// <summary>
        /// Si <paramref name="key"/> tiene texto que enseñar en el idioma actual: con texto en <c>strings.csv</c>, o con
        /// <see cref="Fact(string)"/> no vacío en <c>facts.csv</c>.
        /// </summary>
        public static bool Has(string key)
        {
            int row = s_active.Strings.IndexOf(key);
            if (row >= 0)
            {
                return s_uiText[row].Length > 0;
            }

            row = s_active.Facts.IndexOf(key);
            return row >= 0 && s_factText[row].Length > 0;
        }

        /// <summary>Estado de <paramref name="key"/> en <paramref name="locale"/> (<see cref="LocCatalog.StatusOf"/>).</summary>
        public static LocStatus StatusOf(string key, string locale)
        {
            return s_active.StatusOf(key, locale);
        }

        /// <summary>Si todas las claves de UI tienen texto en <paramref name="locale"/> (<see cref="LocCatalog.IsComplete"/>).</summary>
        public static bool IsComplete(string locale)
        {
            return s_active.IsComplete(locale);
        }

        /// <summary>Entero con separador de miles de la región.</summary>
        public static string Number(long value)
        {
            return LocNumber.Format(value, true, s_numbers);
        }

        /// <summary>Número con <paramref name="decimals"/> decimales y los separadores de la región.</summary>
        public static string Number(double value, int decimals)
        {
            return LocNumber.Format(value, decimals, s_numbers);
        }

        /// <summary>
        /// Precio con el formato de las cifras (D-025): código ISO 4217, un espacio y el importe con los separadores del idioma
        /// del juego y, en español, del país (añadido de D-025 del 2026-10-30) y los decimales de la moneda (<see cref="LocPrice"/>:
        /// 0 en COP y MXN, 2 en el resto), p. ej. «COP 3,900» con la UI en en. <paramref name="minorUnits"/> va en las unidades de <see cref="LocPrice.DecimalsOf"/> (BRL 4,99 →
        /// 499; COP 3.900 → 3900). Es la única función de precios: ningún precio lleva un formato propio.
        /// </summary>
        public static string Price(string iso4217, long minorUnits)
        {
            return LocPrice.Format(iso4217, minorUnits, s_numbers);
        }

        /// <summary>
        /// Como <see cref="Price"/>, desde los micros de Play Billing (<c>priceAmountMicros</c>; D-025, punto 3 [P]): el adaptador
        /// de la tienda real compone así el precio con la moneda del producto, en lugar de la cadena formateada de Google Play.
        /// </summary>
        public static string PriceFromMicros(string iso4217, long micros)
        {
            return LocPrice.Format(iso4217, LocPrice.MinorUnitsFromMicros(iso4217, micros), s_numbers);
        }

        /// <summary>Solo el importe de <see cref="Price"/>, sin el código (p. ej. «3,900»).</summary>
        public static string PriceAmount(string iso4217, long minorUnits)
        {
            return LocPrice.FormatAmount(iso4217, minorUnits, s_numbers);
        }

        /// <summary>
        /// Escribe <paramref name="value"/> en <paramref name="buffer"/> desde <paramref name="pos"/> con los separadores de la
        /// región (de miles solo si <paramref name="grouped"/>) y devuelve la posición siguiente. Sin asignaciones: sustituye a
        /// <c>UiText.WriteNumber</c> en los recuentos por fotograma (con <c>TMP_Text.SetCharArray</c>). Los negativos llevan «-».
        /// </summary>
        public static int WriteNumber(char[] buffer, int pos, long value, bool grouped)
        {
            return LocNumber.Write(buffer, pos, value, grouped, s_numbers);
        }

        private static void CheckLocale(LocCatalog catalog, string locale, bool showUnapproved)
        {
            if (locale == null)
            {
                throw new ArgumentNullException(nameof(locale));
            }

            if (locale == LocLocale.Pseudo)
            {
                if (!showUnapproved)
                {
                    throw new ArgumentException("en-XA solo vale en desarrollo (showUnapproved).", nameof(locale));
                }

                return;
            }

            if (!catalog.HasLocale(locale))
            {
                throw new ArgumentException("El catálogo no tiene el idioma «" + locale + "».", nameof(locale));
            }
        }

        private static string? Normalize(string? tag)
        {
            return string.IsNullOrWhiteSpace(tag) ? null : tag!.Trim();
        }

        private static LocNumberFormat ResolveNumbers()
        {
            return LocNumber.For(s_numberTag ?? LocLocale.NumberTagFor(s_locale, s_numberCountry));
        }

        private static string Missing(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!s_showUnapproved)
            {
                return string.Empty;
            }

            if (!s_missing.TryGetValue(key, out string? marked))
            {
                marked = "[" + key + "]";
                s_missing.Add(key, marked);
            }

            return marked;
        }

        private static void Rebuild()
        {
            s_numbers = ResolveNumbers();
            s_missing.Clear();
            string textLocale = LocCatalog.TextLocale(s_locale);
            bool pseudo = s_locale == LocLocale.Pseudo;

            LocTable strings = s_active.Strings;
            var uiTemplate = new string[strings.Count];
            var uiText = new string[strings.Count];
            for (int r = 0; r < strings.Count; r++)
            {
                string raw = strings.TextAt(r, textLocale);
                string template = pseudo && raw.Length > 0 ? LocPseudo.Transform(raw) : raw;
                uiTemplate[r] = template;
                uiText[r] = template.Length == 0
                    ? string.Empty
                    : LocFormat.Format(template, LocArgs.None, strings.ArgsAt(r), s_locale, s_numbers, LocFormat.LineBreak, 0);
            }

            LocTable facts = s_active.Facts;
            var factTemplate = new string[facts.Count];
            var factText = new string[facts.Count];
            for (int r = 0; r < facts.Count; r++)
            {
                string raw = facts.TextAt(r, textLocale);
                bool visible = raw.Length > 0 && (s_showUnapproved || facts.IsApprovedAt(r, textLocale));
                string template = !visible ? string.Empty : (pseudo ? LocPseudo.Transform(raw) : raw);
                factTemplate[r] = template;
                factText[r] = template.Length == 0
                    ? string.Empty
                    : LocFormat.Format(template, LocArgs.None, facts.ArgsAt(r), s_locale, s_numbers, LocFormat.LineBreak, 0);
            }

            s_uiTemplate = uiTemplate;
            s_uiText = uiText;
            s_factTemplate = factTemplate;
            s_factText = factText;
            Changed?.Invoke();
        }
    }
}
