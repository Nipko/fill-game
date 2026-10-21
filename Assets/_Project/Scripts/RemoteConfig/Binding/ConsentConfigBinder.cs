#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;

namespace AF.RemoteConfig.Binding
{
    /// <summary>
    /// <see cref="ConsentConfig"/> leída de Remote Config (bloque B12; 08 §9), como las tablas de <see cref="RcConfigBinder"/>: sin
    /// reflexión, cada clave con su asignación; un valor que no se entiende deja el default de esa propiedad y se anota; si la clase
    /// entera no pasa su <c>Validate()</c>, se usan sus defaults (Q-B7-7). Archivo propio de B12: el parche <c>B12-rc.diff</c> lo
    /// conecta a <see cref="RcConfigBinder.Bind(RcSnapshot, string)"/> (<c>RcConfigs.Consent</c>) y a su prueba de cobertura.
    /// </summary>
    public static class ConsentConfigBinder
    {
        /// <summary>Las claves que lee, en orden (los nombres exactos de 08 §9).</summary>
        public static readonly IReadOnlyList<string> Keys = new[]
        {
            "consent_text_version",
            "consent_under13_policy",
            "consent_age_signal_source",
            "biz_legal_profile_by_country",
            "biz_under18_no_personalization",
        };

        /// <summary><see cref="ConsentConfig"/> con los valores <paramref name="rc"/>; los problemas, en <paramref name="errors"/>.</summary>
        public static ConsentConfig Bind(RcSnapshot rc, ICollection<string>? errors = null)
        {
            if (rc == null)
            {
                throw new ArgumentNullException(nameof(rc));
            }

            var c = new ConsentConfig();
            Try(errors, "consent_text_version", () => c.ConsentTextVersion = rc.Get<int>("consent_text_version"));
            Try(errors, "consent_under13_policy", () => c.ConsentUnder13Policy = rc.Get<string>("consent_under13_policy"));
            Try(errors, "consent_age_signal_source", () => c.ConsentAgeSignalSource = rc.Get<string>("consent_age_signal_source"));
            Try(errors, "biz_legal_profile_by_country", () => c.BizLegalProfileByCountry = Profiles(rc.Get<RcJson>("biz_legal_profile_by_country")));
            Try(errors, "biz_under18_no_personalization", () => c.BizUnder18NoPersonalization = rc.Get<bool>("biz_under18_no_personalization"));
            try
            {
                c.Validate();
                return c;
            }
            catch (InvalidOperationException ex)
            {
                errors?.Add(nameof(ConsentConfig) + ": " + ex.Message + " Se usan los defaults de 08 §9.");
                return ConsentConfig.Defaults;
            }
        }

        private static void Try(ICollection<string>? errors, string key, Action apply)
        {
            try
            {
                apply();
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException)
            {
                errors?.Add(key + ": " + ex.Message); // se queda el default de esa propiedad
            }
        }

        private static IReadOnlyDictionary<string, string> Profiles(RcJson json)
        {
            if (json.Kind != RcJsonKind.Object)
            {
                throw new FormatException("se esperaba un objeto {país: perfil}");
            }

            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < json.Members.Count; i++)
            {
                RcJson value = json.Members[i].Value;
                if (value.Kind != RcJsonKind.String || value.Text == null)
                {
                    throw new FormatException("el perfil de " + json.Members[i].Key + " debe ser un texto");
                }

                // Los países, en mayúsculas como los lee ConsentConfig.ProfileFor («br» → BR); «*» no cambia.
                map[json.Members[i].Key.ToUpperInvariant()] = value.Text;
            }

            return map;
        }
    }
}
