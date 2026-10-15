#nullable enable
using System;
using AF.Ads;
using AF.IAP;
using AF.Localization;

namespace AF.UI
{
    /// <summary>
    /// Textos de la pantalla de Ajustes (tarea L3; D-018 Q7-A; 02 §12; glosario §10) y de las hojas simuladas de anuncio y de
    /// compra, de <c>strings.csv</c>. Parcial de <see cref="UiText"/> (arquitectura del paso 1, §9). Los nombres de los idiomas
    /// van cada uno en su propio idioma (endónimos), con la misma clave en todas las columnas.
    /// </summary>
    public static partial class UiText
    {
        /// <summary><c>language</c> (glosario §10): cabecera de la sección Idioma de Ajustes.</summary>
        public static string Language => Loc.T(Keys.Language);

        /// <summary><c>patterns_toggle</c> (glosario §10): cabecera de la sección Patrones de fruta de Ajustes (02 §12).</summary>
        public static string PatternsToggle => Loc.T(Keys.PatternsToggle);

        /// <summary><c>settings_language_auto</c> [P]: «Automático», el idioma del teléfono (D-018 Q7-A; <c>settings.language</c> = auto).</summary>
        public static string SettingsLanguageAuto => Loc.T(Keys.SettingsLanguageAuto);

        /// <summary><c>settings_patterns_auto</c> [P]: «Automático», los Patrones de fruta según el nivel (desde el 41; 02 §12).</summary>
        public static string SettingsPatternsAuto => Loc.T(Keys.SettingsPatternsAuto);

        /// <summary><c>settings_patterns_on</c> [P]: Patrones de fruta siempre.</summary>
        public static string SettingsPatternsOn => Loc.T(Keys.SettingsPatternsOn);

        /// <summary><c>settings_patterns_off</c> [P]: Patrones de fruta nunca.</summary>
        public static string SettingsPatternsOff => Loc.T(Keys.SettingsPatternsOff);

        /// <summary>Rótulo de la pantalla «Anuncio simulado» (<see cref="SimulatedAd.LabelKey"/>; D-008 punto 4).</summary>
        public static string SimulatedAdTitle => Loc.T(Keys.SimulatedAdTitle);

        /// <summary>Rótulo de la hoja «Compra simulada» (<see cref="SimulatedPurchase.LabelKey"/>; D-008 punto 5).</summary>
        public static string SimulatedPurchaseTitle => Loc.T(Keys.SimulatedPurchaseTitle);

        /// <summary>
        /// Clave del nombre propio del idioma <paramref name="locale"/> en Ajustes: <c>language_name_</c> y la etiqueta en
        /// minúsculas con «_» (<c>es-419</c> → <c>language_name_es_419</c>; <c>en-XA</c> → <c>language_name_en_xa</c>).
        /// </summary>
        public static string LanguageNameKey(string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                throw new ArgumentException("Falta el idioma.", nameof(locale));
            }

            switch (locale)
            {
                case LocLocale.SpanishLatinAmerica: return Keys.LanguageNameEs419;
                case LocLocale.PortugueseBrazil: return Keys.LanguageNamePtBr;
                case LocLocale.English: return Keys.LanguageNameEn;
                case LocLocale.Pseudo: return Keys.LanguageNamePseudo;
                default: return "language_name_" + locale.ToLowerInvariant().Replace('-', '_');
            }
        }

        /// <summary>Nombre propio del idioma <paramref name="locale"/> (Ajustes › Idioma; D-018 Q7-A).</summary>
        public static string LanguageName(string locale)
        {
            return Loc.T(LanguageNameKey(locale));
        }

        /// <summary>Claves de los textos de Ajustes y de las hojas simuladas (parcial de <see cref="Keys"/>).</summary>
        public static partial class Keys
        {
            /// <summary><c>language</c> (glosario §10).</summary>
            public const string Language = "language";

            /// <summary><c>patterns_toggle</c> (glosario §10).</summary>
            public const string PatternsToggle = "patterns_toggle";

            /// <summary><c>settings_language_auto</c> [P].</summary>
            public const string SettingsLanguageAuto = "settings_language_auto";

            /// <summary><c>settings_patterns_auto</c> [P].</summary>
            public const string SettingsPatternsAuto = "settings_patterns_auto";

            /// <summary><c>settings_patterns_on</c> [P].</summary>
            public const string SettingsPatternsOn = "settings_patterns_on";

            /// <summary><c>settings_patterns_off</c> [P].</summary>
            public const string SettingsPatternsOff = "settings_patterns_off";

            /// <summary><c>language_name_es_419</c> [P]: «Español».</summary>
            public const string LanguageNameEs419 = "language_name_es_419";

            /// <summary><c>language_name_pt_br</c> [P]: «Português».</summary>
            public const string LanguageNamePtBr = "language_name_pt_br";

            /// <summary><c>language_name_en</c> [P]: «English».</summary>
            public const string LanguageNameEn = "language_name_en";

            /// <summary><c>language_name_en_xa</c> [P]: el idioma falso de desarrollo (solo con <c>showUnapproved</c>).</summary>
            public const string LanguageNamePseudo = "language_name_en_xa";

            /// <summary><c>ad_simulated</c> [P] (<see cref="SimulatedAd.LabelKey"/>).</summary>
            public const string SimulatedAdTitle = SimulatedAd.LabelKey;

            /// <summary><c>iap_simulated_purchase</c> [P] (<see cref="SimulatedPurchase.LabelKey"/>).</summary>
            public const string SimulatedPurchaseTitle = SimulatedPurchase.LabelKey;
        }
    }
}
