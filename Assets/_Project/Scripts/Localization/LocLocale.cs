#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Localization
{
    /// <summary>De dónde salió el idioma resuelto por <see cref="LocLocale.Resolve"/>.</summary>
    public enum LocSource : byte
    {
        /// <summary>El de desarrollo (<c>af_lang</c>, solo en builds de desarrollo).</summary>
        Dev = 0,

        /// <summary>La elección guardada en Ajustes (<c>settings.language</c> ≠ <c>auto</c>).</summary>
        Saved = 1,

        /// <summary>El idioma del teléfono, con el mapeo de Q7-A.</summary>
        Device = 2,

        /// <summary>El último eslabón: en.</summary>
        Default = 3,

        /// <summary>Ningún candidato tenía la UI completa: es-419 [P].</summary>
        Fallback = 4,
    }

    /// <summary>Idioma resuelto y de dónde salió (para la sonda <c>AF_STATE loc</c>).</summary>
    public readonly struct LocResolution
    {
        /// <summary>Idioma de la UI («es-419», «pt-BR», «en» o «en-XA»).</summary>
        public readonly string Locale;

        /// <summary>De dónde salió.</summary>
        public readonly LocSource Source;

        /// <summary>Resultado con su idioma y su origen.</summary>
        public LocResolution(string locale, LocSource source)
        {
            Locale = locale ?? throw new ArgumentNullException(nameof(locale));
            Source = source;
        }

        /// <inheritdoc/>
        public override string ToString() => Locale + " (" + Source + ")";
    }

    /// <summary>
    /// Idiomas de la UI y su resolución (D-018, Q7-A; propuesta de idiomas §3.5; arquitectura de la tanda 4, §1):
    /// desarrollo (<c>af_lang</c>) → elección guardada (<c>settings.language</c> ≠ <c>auto</c>) → teléfono → en, con el mapeo
    /// es-* → es-419, pt-* → pt-BR y el resto → en. Solo se acepta un candidato con la UI completa
    /// (<see cref="LocCatalog.IsComplete"/>); si no lo es, se prueba el siguiente y, si ninguno vale, es-419 [P] (pregunta 10;
    /// notas de la tanda 4, punto 10: mientras EN y PT-BR no estén completos, el idioma automático se queda en es-419). en-XA
    /// (pseudolocalización, <see cref="LocPseudo"/>) solo vale con <c>showUnapproved</c>, es decir, en desarrollo y en los tests.
    /// </summary>
    public static class LocLocale
    {
        /// <summary>Español latinoamericano: el texto fuente (Q6-A).</summary>
        public const string SpanishLatinAmerica = "es-419";

        /// <summary>Portugués de Brasil (Q6-A).</summary>
        public const string PortugueseBrazil = "pt-BR";

        /// <summary>Inglés.</summary>
        public const string English = "en";

        /// <summary>Idioma falso de desarrollo: el español con acentos, alargado y entre corchetes (§3.8).</summary>
        public const string Pseudo = "en-XA";

        /// <summary>Idioma del texto fuente de las tablas (columna obligatoria).</summary>
        public const string SourceLocale = SpanishLatinAmerica;

        /// <summary>Respaldo si ningún candidato tiene la UI completa [P] (pregunta 10).</summary>
        public const string FallbackLocale = SpanishLatinAmerica;

        /// <summary>Valor de <c>settings.language</c> que significa «seguir al teléfono» (D-018, Q7-A).</summary>
        public const string Auto = "auto";

        private static readonly string[] s_tableLocales =
        {
            SpanishLatinAmerica, PortugueseBrazil, English, "fr", "de", "it", "nl", "tr", "ja", "ko", "ru",
        };

        private static readonly IReadOnlyList<string> s_tableLocalesView = Array.AsReadOnly(s_tableLocales);

        /// <summary>
        /// Idiomas que pueden ser una columna de las tablas de textos, escritos exactamente así (glosario §11, fila
        /// Localización; 08 §4.7): es-419, pt-BR y en (tanda 1) y fr, de, it, nl, tr, ja, ko y ru (tanda 2). Es una lista
        /// cerrada para que una errata en la cabecera («pt-br», «ctx») sea un error y no un idioma fantasma que nunca está
        /// completo; un idioma nuevo entra aquí cuando entre en el glosario. en-XA no es una columna: sale de es-419
        /// (<see cref="LocPseudo"/>).
        /// </summary>
        public static IReadOnlyList<string> TableLocales => s_tableLocalesView;

        /// <summary>Si <paramref name="name"/> es uno de <see cref="TableLocales"/>, escrito igual (ordinal). Sin asignaciones.</summary>
        public static bool IsTableLocale(string? name)
        {
            if (name == null)
            {
                return false;
            }

            for (int i = 0; i < s_tableLocales.Length; i++)
            {
                if (string.Equals(s_tableLocales[i], name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Idioma de la UI para una etiqueta (Q7-A): en-XA tal cual; es-* → es-419; pt-* → pt-BR; cualquier otro → en. Null,
        /// vacío o <c>auto</c> → null (no hay candidato).
        /// </summary>
        public static string? Map(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }

            string trimmed = tag!.Trim();
            if (string.Equals(trimmed, Auto, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (IsPseudo(trimmed))
            {
                return Pseudo;
            }

            if (HasLanguage(trimmed, "es"))
            {
                return SpanishLatinAmerica;
            }

            return HasLanguage(trimmed, "pt") ? PortugueseBrazil : English;
        }

        /// <summary>
        /// Si <paramref name="locale"/> se puede usar: con la UI completa en <paramref name="catalog"/>, o en-XA con
        /// <paramref name="showUnapproved"/> (y el español completo, que es su fuente).
        /// </summary>
        public static bool IsOffered(LocCatalog catalog, string locale, bool showUnapproved)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (locale == Pseudo)
            {
                return showUnapproved && catalog.IsComplete(SourceLocale);
            }

            return catalog.IsComplete(locale);
        }

        /// <summary>
        /// Idiomas que se pueden elegir en Ajustes: los de la tabla con la UI completa, en su orden, y en-XA al final con
        /// <paramref name="showUnapproved"/>.
        /// </summary>
        public static IReadOnlyList<string> Offered(LocCatalog catalog, bool showUnapproved)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var offered = new List<string>();
            IReadOnlyList<string> locales = catalog.Locales;
            for (int i = 0; i < locales.Count; i++)
            {
                if (catalog.IsComplete(locales[i]))
                {
                    offered.Add(locales[i]);
                }
            }

            if (IsOffered(catalog, Pseudo, showUnapproved))
            {
                offered.Add(Pseudo);
            }

            return offered;
        }

        /// <summary>
        /// Resuelve el idioma de la UI: <paramref name="dev"/> (<c>af_lang</c>) → <paramref name="saved"/>
        /// (<c>settings.language</c>; <c>auto</c> no cuenta) → <paramref name="device"/> (etiqueta del teléfono) → en; cada
        /// candidato pasa por <see cref="Map"/> y solo vale si <see cref="IsOffered"/>. Si ninguno vale, es-419 [P].
        /// </summary>
        public static LocResolution Resolve(LocCatalog catalog, string? dev, string? saved, string? device, bool showUnapproved)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (TryCandidate(catalog, dev, showUnapproved, out string locale))
            {
                return new LocResolution(locale, LocSource.Dev);
            }

            if (TryCandidate(catalog, saved, showUnapproved, out locale))
            {
                return new LocResolution(locale, LocSource.Saved);
            }

            if (TryCandidate(catalog, device, showUnapproved, out locale))
            {
                return new LocResolution(locale, LocSource.Device);
            }

            if (IsOffered(catalog, English, showUnapproved))
            {
                return new LocResolution(English, LocSource.Default);
            }

            return new LocResolution(FallbackLocale, LocSource.Fallback);
        }

        /// <summary>
        /// Etiqueta de cifras cuando no se sabe la región del teléfono: es-419 y en-XA → es-CO [P] (lo de hoy, «1.000»;
        /// notas de la tanda 4, punto 10), pt-BR → pt-BR, en → en; cualquier otra, ella misma.
        /// </summary>
        public static string DefaultNumberTag(string locale)
        {
            if (locale == null)
            {
                throw new ArgumentNullException(nameof(locale));
            }

            if (IsPseudo(locale) || HasLanguage(locale, "es"))
            {
                return LocNumber.SpanishDefaultTag;
            }

            return locale;
        }

        /// <summary>
        /// Fila de cifras del juego (observación 22 de la jornada 6, decisión del dueño del 2026-10-30): el separador de miles y el
        /// decimal siguen al <b>idioma del juego</b> y, en español, al <b>país</b>; nunca a <c>CultureInfo.CurrentCulture</c> (en IL2CPP
        /// Android suele ser invariante) ni a la región del teléfono por sí sola.
        /// <list type="bullet">
        /// <item>en → «en»: «1,000» y «1.5».</item>
        /// <item>pt-BR (y cualquier pt) → «pt-BR»: «1.000» y «1,5».</item>
        /// <item>es-419 (y en-XA, el español falseado) → «es-» + <paramref name="country"/> en la tabla de <see cref="LocNumber"/>:
        /// punto de miles en CO, AR, CL, UY, PY, VE, EC, BO, BR (y ES); coma en MX, PE, GT, HN, NI, PA, SV, DO, PR, CU, BZ y US;
        /// espacio duro en CR (ICU). País desconocido, sin país o fuera de la tabla → es-CO, el punto de la mayoría de LatAm.</item>
        /// <item>Otro idioma → su propia fila (<see cref="LocNumber.For"/>).</item>
        /// </list>
        /// <paramref name="country"/> es ISO 3166-1 alfa-2 (cualquier caja); lo que no son dos letras ASCII cuenta como desconocido.
        /// </summary>
        public static string NumberTagFor(string locale, string? country)
        {
            if (locale == null)
            {
                throw new ArgumentNullException(nameof(locale));
            }

            if (IsPseudo(locale) || HasLanguage(locale, "es"))
            {
                string? code = CountryCode(country);
                return code == null ? LocNumber.SpanishDefaultTag : "es-" + code;
            }

            if (HasLanguage(locale, "en"))
            {
                return English;
            }

            if (HasLanguage(locale, "pt"))
            {
                return PortugueseBrazil;
            }

            return locale;
        }

        /// <summary>
        /// País para las cifras (observación 22 de la jornada 6), con las fuentes de D-057 que ya usan el <c>geo_tier</c> y los
        /// precios: el de la cuenta de Play (<paramref name="playCountry"/>, guardado desde la última conexión con Play) y, sin él,
        /// la región de la etiqueta del teléfono (<paramref name="deviceTag"/>; «es-419» no es un país). Null si no se sabe.
        /// </summary>
        public static string? NumberCountry(string? playCountry, string? deviceTag)
        {
            string? play = CountryCode(playCountry);
            if (play != null)
            {
                return play;
            }

            if (string.IsNullOrWhiteSpace(deviceTag))
            {
                return null;
            }

            SplitTag(deviceTag!, out _, out string? region);
            return CountryCode(region);
        }

        /// <summary>Dos letras ASCII → mayúsculas; cualquier otra cosa (null, vacío, «419»), null.</summary>
        internal static string? CountryCode(string? code)
        {
            if (code == null)
            {
                return null;
            }

            string t = code.Trim();
            if (t.Length != 2 || !IsAsciiLetter(t[0]) || !IsAsciiLetter(t[1]))
            {
                return null;
            }

            return t.ToUpperInvariant();
        }

        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>Si <paramref name="tag"/> es en-XA (sin distinguir mayúsculas, con «-» o «_»).</summary>
        public static bool IsPseudo(string tag)
        {
            return HasLanguage(tag, "en") && HasRegion(tag, "XA");
        }

        /// <summary>
        /// Si el primer subtag de <paramref name="tag"/> es <paramref name="language"/> (sin distinguir mayúsculas). Sin
        /// asignaciones.
        /// </summary>
        public static bool HasLanguage(string tag, string language)
        {
            if (tag == null || language == null || tag.Length < language.Length)
            {
                return false;
            }

            if (string.Compare(tag, 0, language, 0, language.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            return tag.Length == language.Length || IsSeparator(tag[language.Length]);
        }

        /// <summary>
        /// Si la región de <paramref name="tag"/> (el primer subtag de 2 letras o 3 cifras tras el idioma y el script) es
        /// <paramref name="region"/> (sin distinguir mayúsculas). Sin asignaciones.
        /// </summary>
        public static bool HasRegion(string tag, string region)
        {
            if (tag == null || region == null || !TryFindRegion(tag, out int start, out int length))
            {
                return false;
            }

            return length == region.Length && string.Compare(tag, start, region, 0, length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Si <paramref name="tag"/> trae región (un subtag de 2 letras o 3 cifras tras el idioma y el script): «es-MX», «es-419» y
        /// «zh-Hant-TW» sí; «es», «pt», «en-u-ca-gregory» y null, no. Sin asignaciones.
        /// </summary>
        public static bool HasAnyRegion(string? tag)
        {
            return tag != null && TryFindRegion(tag, out _, out _);
        }

        /// <summary>
        /// Parte una etiqueta en idioma (minúsculas) y región (mayúsculas, o null): «es_co» → es, CO; «zh-Hant-TW» → zh, TW;
        /// «es-419» → es, 419; «en-US-u-mu-celsius» → en, US.
        /// </summary>
        internal static void SplitTag(string tag, out string language, out string? region)
        {
            string t = tag.Trim();
            int end = 0;
            while (end < t.Length && !IsSeparator(t[end]))
            {
                end++;
            }

            language = t.Substring(0, end).ToLowerInvariant();
            region = TryFindRegion(t, out int start, out int length) ? t.Substring(start, length).ToUpperInvariant() : null;
        }

        private static bool TryFindRegion(string tag, out int start, out int length)
        {
            int i = 0;
            while (i < tag.Length && !IsSeparator(tag[i]))
            {
                i++;
            }

            while (i < tag.Length)
            {
                int s = i + 1;
                int e = s;
                while (e < tag.Length && !IsSeparator(tag[e]))
                {
                    e++;
                }

                int len = e - s;
                if (len == 4)
                {
                    i = e; // script (Hant, Latn): sigue buscando la región
                    continue;
                }

                if ((len == 2 && IsLetter(tag[s]) && IsLetter(tag[s + 1]))
                    || (len == 3 && IsDigit(tag[s]) && IsDigit(tag[s + 1]) && IsDigit(tag[s + 2])))
                {
                    start = s;
                    length = len;
                    return true;
                }

                break; // variante o extensión: ya no hay región
            }

            start = 0;
            length = 0;
            return false;
        }

        private static bool TryCandidate(LocCatalog catalog, string? tag, bool showUnapproved, out string locale)
        {
            string? mapped = Map(tag);
            if (mapped != null && IsOffered(catalog, mapped, showUnapproved))
            {
                locale = mapped;
                return true;
            }

            locale = string.Empty;
            return false;
        }

        private static bool IsSeparator(char c) => c == '-' || c == '_';

        private static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private static bool IsDigit(char c) => c >= '0' && c <= '9';
    }
}
