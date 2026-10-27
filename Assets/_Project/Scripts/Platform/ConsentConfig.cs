#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Platform
{
    /// <summary>
    /// Cifras del consentimiento y la edad con el <b>default compilado</b> de la tabla única de 08 §9 (bloque B12):
    /// <c>consent_text_version</c>, <c>consent_under13_policy</c>, <c>consent_age_signal_source</c>,
    /// <c>biz_legal_profile_by_country</c> y <c>biz_under18_no_personalization</c>. Vive en <c>AF.Platform</c>, junto a
    /// <see cref="ConsentPolicy"/>, porque la leen los anuncios, la tienda y el arranque. Las enlaza con Remote Config
    /// <c>AF.RemoteConfig.Binding.ConsentConfigBinder</c>, desde <c>RcConfigBinder</c> (<c>RcConfigs.Consent</c>).
    /// </summary>
    public sealed class ConsentConfig
    {
        /// <summary><c>block</c>: la banda &lt;13 se bloquea (la única de la 1.0).</summary>
        public const string PolicyBlock = "block";

        /// <summary><c>parental_consent</c>: reservado (consentimiento parental verificable, sin implementar en la 1.0).</summary>
        public const string PolicyParentalConsent = "parental_consent";

        /// <summary><c>self_declared</c>: la banda sale de la pantalla neutral.</summary>
        public const string SourceSelfDeclared = "self_declared";

        /// <summary><c>self_declared+play_age_signals</c>: además, Play Age Signals (T-L3; <see cref="AgeSignalPolicy"/>). Default de 08 §9 desde D-057, añadido del 2026-10-27.</summary>
        public const string SourceWithPlayAgeSignals = "self_declared+play_age_signals";

        /// <summary>Clave del resto de países en el mapa (Q-B7-4 (a): <c>"*"</c>).</summary>
        public const string RestKey = "*";

        /// <summary>Clave del grupo EEE en el mapa de 08 §9.</summary>
        public const string EeaKey = "EEE";

        /// <summary>Clave del Reino Unido en el mapa de 08 §9 (su código ISO es <c>GB</c>).</summary>
        public const string UkKey = "UK";

        // EEE = los 27 de la UE más Islandia, Liechtenstein y Noruega (códigos ISO 3166-1 alfa-2). Suiza y el Reino Unido tienen
        // su propia clave en el mapa (CH y UK).
        private static readonly string[] EeaCountries =
        {
            "AT", "BE", "BG", "CY", "CZ", "DE", "DK", "EE", "ES", "FI", "FR", "GR", "HR", "HU", "IE", "IS", "IT", "LI", "LT", "LU",
            "LV", "MT", "NL", "NO", "PL", "PT", "RO", "SE", "SI", "SK",
        };

        /// <summary>Una instancia nueva con los defaults de 08 §9 (cada llamada devuelve otra).</summary>
        public static ConsentConfig Defaults => new ConsentConfig();

        /// <summary><c>consent_text_version</c> (08): sube al cambiar los textos legales y obliga a volver a pedir el consentimiento.</summary>
        [RemoteConfigKey("consent_text_version")]
        public int ConsentTextVersion { get; set; } = 1;

        /// <summary><c>consent_under13_policy</c> (05): <see cref="PolicyBlock"/> o <see cref="PolicyParentalConsent"/> (reservado).</summary>
        [RemoteConfigKey("consent_under13_policy")]
        public string ConsentUnder13Policy { get; set; } = PolicyBlock;

        /// <summary><c>consent_age_signal_source</c> (05): <see cref="SourceSelfDeclared"/> o <see cref="SourceWithPlayAgeSignals"/>.</summary>
        [RemoteConfigKey("consent_age_signal_source")]
        public string ConsentAgeSignalSource { get; set; } = SourceWithPlayAgeSignals;

        /// <summary>
        /// <c>biz_legal_profile_by_country</c> (01): país (ISO alfa-2, o <see cref="EeaKey"/>, <see cref="UkKey"/> y
        /// <see cref="RestKey"/>) → perfil legal (<see cref="LegalProfiles"/>).
        /// </summary>
        [RemoteConfigKey("biz_legal_profile_by_country")]
        public IReadOnlyDictionary<string, string> BizLegalProfileByCountry { get; set; } = DefaultProfiles();

        /// <summary><c>biz_under18_no_personalization</c> (01): con 13-17, <c>npa</c>, contenido G, sin perfilado y doble confirmación de compras.</summary>
        [RemoteConfigKey("biz_under18_no_personalization")]
        public bool BizUnder18NoPersonalization { get; set; } = true;

        /// <summary>El mapa de 08 §9: BR, CO, PE, MX propios; EEE, UK y CH por UMP; el resto, <c>default</c>.</summary>
        public static IReadOnlyDictionary<string, string> DefaultProfiles()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "BR", LegalProfiles.BrLgpdEca },
                { "CO", LegalProfiles.Co1581 },
                { "PE", LegalProfiles.Pe29733 },
                { "MX", LegalProfiles.MxLfpdppp },
                { EeaKey, LegalProfiles.Ump },
                { UkKey, LegalProfiles.Ump },
                { "CH", LegalProfiles.Ump },
                { RestKey, LegalProfiles.Default },
            };
        }

        /// <summary><paramref name="country"/> (ISO alfa-2 en mayúsculas) es del EEE.</summary>
        public static bool IsEea(string country)
        {
            for (int i = 0; i < EeaCountries.Length; i++)
            {
                if (string.Equals(EeaCountries[i], country, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// El perfil legal de <paramref name="country"/> (ISO alfa-2; null o vacío = desconocido): la entrada del país; si no, la de
        /// su grupo (<see cref="EeaKey"/> para el EEE, <see cref="UkKey"/> para <c>GB</c>); si no, la del resto
        /// (<see cref="RestKey"/>). Sin país, <see cref="LegalProfiles.Default"/>, el tratamiento más estricto (Q-T3 (a)). Un perfil
        /// que no se conoce cuenta como <see cref="LegalProfiles.Default"/>.
        /// </summary>
        public string ProfileFor(string? country)
        {
            if (string.IsNullOrEmpty(country))
            {
                return LegalProfiles.Default;
            }

            string code = country!.ToUpperInvariant();
            if (Lookup(code, out string? profile))
            {
                return profile!;
            }

            if (IsEea(code) && Lookup(EeaKey, out profile))
            {
                return profile!;
            }

            if (string.Equals(code, "GB", StringComparison.Ordinal) && Lookup(UkKey, out profile))
            {
                return profile!;
            }

            return Lookup(RestKey, out profile) ? profile! : LegalProfiles.Default;
        }

        /// <summary>Comprueba que los valores tienen sentido (no que coincidan con 08 §9: eso lo hacen los tests).</summary>
        /// <exception cref="InvalidOperationException">Algún valor está fuera de lo admisible.</exception>
        public void Validate()
        {
            if (ConsentTextVersion < 1)
            {
                throw new InvalidOperationException("ConsentConfig: consent_text_version debe ser >= 1.");
            }

            if (!string.Equals(ConsentUnder13Policy, PolicyBlock, StringComparison.Ordinal)
                && !string.Equals(ConsentUnder13Policy, PolicyParentalConsent, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("ConsentConfig: consent_under13_policy debe ser block o parental_consent.");
            }

            if (!string.Equals(ConsentAgeSignalSource, SourceSelfDeclared, StringComparison.Ordinal)
                && !string.Equals(ConsentAgeSignalSource, SourceWithPlayAgeSignals, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("ConsentConfig: consent_age_signal_source no es uno de los de 08 §9.");
            }

            if (BizLegalProfileByCountry == null || !BizLegalProfileByCountry.ContainsKey(RestKey))
            {
                throw new InvalidOperationException("ConsentConfig: biz_legal_profile_by_country necesita la clave \"*\" (el resto).");
            }

            foreach (KeyValuePair<string, string> entry in BizLegalProfileByCountry)
            {
                if (!LegalProfiles.IsKnown(entry.Value))
                {
                    throw new InvalidOperationException("ConsentConfig: perfil legal desconocido «" + entry.Value + "» para " + entry.Key + ".");
                }
            }
        }

        private bool Lookup(string key, out string? profile)
        {
            if (BizLegalProfileByCountry != null && BizLegalProfileByCountry.TryGetValue(key, out string? value) && LegalProfiles.IsKnown(value))
            {
                profile = value;
                return true;
            }

            profile = null;
            return false;
        }
    }
}
