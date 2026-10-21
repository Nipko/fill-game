#nullable enable
using System;

namespace AF.Save
{
    /// <summary>
    /// <c>first_app_version</c>, <c>locale</c> y <c>country_first</c> del perfil (GDD 08 §4.3; bloque B8). Pura: quien llama le da
    /// la versión de la app (<c>Application.version</c>), el idioma del juego resuelto y la etiqueta del teléfono
    /// (<c>IDeviceLocale.LanguageTag</c>).
    /// <list type="bullet">
    /// <item><see cref="StampFirstRun"/>: solo en un perfil de primera ejecución; <c>first_app_version</c> y <c>country_first</c> no
    /// se vuelven a escribir nunca (un perfil anterior a B8 los deja en null: no se inventa una versión).</item>
    /// <item><see cref="UpdateLocale"/>: en cada arranque, el idioma de la UI resuelto [P] (Q-B8-4).</item>
    /// <item><see cref="CountryFromTag"/>: la región de una etiqueta BCP 47 (<c>es-CO</c> → <c>CO</c>; <c>es-419</c> y <c>es</c> →
    /// null), en ISO 3166-1 alfa-2 (Q-T3 (a): sin SIM en el cliente, la región del teléfono) [P].</item>
    /// </list>
    /// </summary>
    public static class ProfileIdentity
    {
        /// <summary>
        /// Escribe <c>first_app_version</c> = <paramref name="appVersion"/>, <c>country_first</c> (de <paramref name="deviceTag"/>) y
        /// <c>locale</c> = <paramref name="gameLocale"/> en un perfil recién creado. Lo ya escrito no se toca. Devuelve si cambió algo.
        /// </summary>
        public static bool StampFirstRun(Profile profile, string? appVersion, string? deviceTag, string? gameLocale)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            bool changed = false;
            string? version = Clean(appVersion);
            if (profile.FirstAppVersion == null && version != null)
            {
                profile.FirstAppVersion = version;
                changed = true;
            }

            string? country = CountryFromTag(deviceTag);
            if (profile.CountryFirst == null && country != null)
            {
                profile.CountryFirst = country;
                changed = true;
            }

            return UpdateLocale(profile, gameLocale) || changed;
        }

        /// <summary>Pone <c>locale</c> = <paramref name="gameLocale"/> (vacío o null: no cambia). Devuelve si cambió.</summary>
        public static bool UpdateLocale(Profile profile, string? gameLocale)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            string? locale = Clean(gameLocale);
            if (locale == null || string.Equals(profile.Locale, locale, StringComparison.Ordinal))
            {
                return false;
            }

            profile.Locale = locale;
            return true;
        }

        /// <summary>
        /// Región ISO 3166-1 alfa-2 en mayúsculas de una etiqueta BCP 47 (<c>es-CO</c>, <c>pt_BR</c>, <c>zh-Hant-TW</c> → <c>CO</c>,
        /// <c>BR</c>, <c>TW</c>), o null si no lleva región de dos letras (<c>es</c>, <c>es-419</c>, <c>""</c>).
        /// </summary>
        public static string? CountryFromTag(string? tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return null;
            }

            string[] parts = tag!.Replace('_', '-').Split('-');
            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 2 && IsAsciiLetter(part[0]) && IsAsciiLetter(part[1]))
                {
                    return new string(new[] { Upper(part[0]), Upper(part[1]) });
                }
            }

            return null;
        }

        private static string? Clean(string? text)
        {
            if (text == null)
            {
                return null;
            }

            string trimmed = text.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static char Upper(char c)
        {
            return c >= 'a' && c <= 'z' ? (char)(c - 32) : c;
        }
    }
}
