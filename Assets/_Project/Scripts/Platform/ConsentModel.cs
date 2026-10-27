#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Banda de edad declarada en la pantalla de edad neutral (05 §7; 08 §2.3 y §7; GDD.md §6 #15): <c>&lt;13</c>, <c>13-17</c> y
    /// <c>18+</c>. Vive solo en el dispositivo (<c>consent.age_band</c>, sin ID).
    /// </summary>
    public enum AgeBand
    {
        /// <summary>Aún no se ha declarado.</summary>
        Unknown = 0,

        /// <summary><c>&lt;13</c>: bloqueo en la 1.0 (<c>consent_under13_policy</c> = <c>block</c>).</summary>
        Under13 = 1,

        /// <summary><c>13-17</c>: <c>npa</c>, contenido G y doble confirmación de compras (<c>biz_under18_no_personalization</c>; Q-T4 (a)).</summary>
        Teen = 2,

        /// <summary><c>18+</c>: flujo de consentimiento normal.</summary>
        Adult = 3,
    }

    /// <summary>Los ids de <see cref="AgeBand"/> en el perfil y en la analítica (<c>af_consent.age_band</c>, 08 §8.2).</summary>
    public static class AgeBands
    {
        /// <summary><c>&lt;13</c>.</summary>
        public const string Under13Id = "<13";

        /// <summary><c>13-17</c>.</summary>
        public const string TeenId = "13-17";

        /// <summary><c>18+</c>.</summary>
        public const string AdultId = "18+";

        /// <summary>El id de <paramref name="band"/>, o null sin declarar.</summary>
        public static string? Id(AgeBand band)
        {
            switch (band)
            {
                case AgeBand.Under13: return Under13Id;
                case AgeBand.Teen: return TeenId;
                case AgeBand.Adult: return AdultId;
                default: return null;
            }
        }

        /// <summary>Lee un id; lo que no es una banda conocida (o null) da <see cref="AgeBand.Unknown"/>.</summary>
        public static AgeBand Parse(string? id)
        {
            switch (id)
            {
                case Under13Id: return AgeBand.Under13;
                case TeenId: return AgeBand.Teen;
                case AdultId: return AgeBand.Adult;
                default: return AgeBand.Unknown;
            }
        }
    }

    /// <summary>
    /// Perfiles legales de <c>biz_legal_profile_by_country</c> (08 §9; 08 §7, «Consentimiento»): pantalla propia en BR, CO, PE y MX;
    /// UMP (de Google Mobile Ads) en EEE, Reino Unido y Suiza; <c>default</c> en el resto y cuando no se sabe el país (Q-T3 (a): el
    /// tratamiento más estricto).
    /// </summary>
    public static class LegalProfiles
    {
        /// <summary>Brasil: LGPD y ECA Digital.</summary>
        public const string BrLgpdEca = "br_lgpd_eca";

        /// <summary>Colombia: Ley 1581.</summary>
        public const string Co1581 = "co_1581";

        /// <summary>Perú: Ley 29733.</summary>
        public const string Pe29733 = "pe_29733";

        /// <summary>México: LFPDPPP.</summary>
        public const string MxLfpdppp = "mx_lfpdppp";

        /// <summary>EEE, Reino Unido y Suiza: el mensaje de UMP de Google Mobile Ads (08 §7 #2).</summary>
        public const string Ump = "ump";

        /// <summary>El resto, y el país desconocido (Q-T3 (a)).</summary>
        public const string Default = "default";

        /// <summary>Los seis perfiles, en el orden de 08 §9.</summary>
        public static readonly string[] All = { BrLgpdEca, Co1581, Pe29733, MxLfpdppp, Ump, Default };

        /// <summary><paramref name="profile"/> es uno de <see cref="All"/>.</summary>
        public static bool IsKnown(string? profile)
        {
            if (profile == null)
            {
                return false;
            }

            for (int i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i], profile, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Lo que el jugador decidió (<c>profile.consent</c> de 08 §4.3: <c>profile</c>, <c>text_version</c>, <c>at</c>,
    /// <c>analytics</c>, <c>ads_personalized</c> y <c>age_band</c>), más <see cref="AgeBandCorrected"/> [P] (B12): la banda ya se
    /// corrigió desde Ajustes, que 08 §7 y 05 §7 permiten una sola vez. Inmutable: cada cambio da otro registro. Es también el
    /// «log de fecha, versión, país y decisión» de 05 §7 (LGPD): el país va como su perfil legal.
    /// </summary>
    public sealed class ConsentRecord
    {
        /// <summary>Crea el registro; <paramref name="at"/> en UTC.</summary>
        /// <exception cref="ArgumentException">Perfil vacío.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Versión negativa.</exception>
        public ConsentRecord(string profile, int textVersion, DateTime at, bool analytics, bool adsPersonalized, AgeBand ageBand, bool ageBandCorrected = false)
        {
            if (string.IsNullOrEmpty(profile))
            {
                throw new ArgumentException("Falta el perfil legal.", nameof(profile));
            }

            if (textVersion < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(textVersion));
            }

            Profile = profile;
            TextVersion = textVersion;
            At = at.Kind == DateTimeKind.Utc ? at : DateTime.SpecifyKind(at, DateTimeKind.Utc);
            Analytics = analytics;
            AdsPersonalized = adsPersonalized;
            AgeBand = ageBand;
            AgeBandCorrected = ageBandCorrected;
        }

        /// <summary><c>profile</c>: el perfil legal con el que se pidió (<see cref="LegalProfiles"/>).</summary>
        public string Profile { get; }

        /// <summary><c>text_version</c>: la versión de los textos legales aceptada (<c>consent_text_version</c>, 08 §9).</summary>
        public int TextVersion { get; }

        /// <summary><c>at</c>: cuándo se decidió, UTC.</summary>
        public DateTime At { get; }

        /// <summary><c>analytics</c>: consentimiento de analítica.</summary>
        public bool Analytics { get; }

        /// <summary><c>ads_personalized</c>: consentimiento de anuncios personalizados (sin él, <c>npa</c>).</summary>
        public bool AdsPersonalized { get; }

        /// <summary><c>age_band</c>.</summary>
        public AgeBand AgeBand { get; }

        /// <summary><c>age_band_corrected</c> [P]: la banda ya se corrigió desde Ajustes (una sola vez).</summary>
        public bool AgeBandCorrected { get; }

        /// <summary>Otro registro con las finalidades elegidas, la versión de los textos, el perfil y la fecha nuevos.</summary>
        public ConsentRecord WithChoices(string profile, int textVersion, DateTime at, bool analytics, bool adsPersonalized)
        {
            return new ConsentRecord(profile, textVersion, at, analytics, adsPersonalized, AgeBand, AgeBandCorrected);
        }
    }

    /// <summary>
    /// Dónde se guarda <see cref="ConsentRecord"/>: en el juego, en <c>profile.consent</c> (enganche <c>B12-profile.diff</c>:
    /// <c>AF.Save.ProfileConsentStore</c>); en los tests, <see cref="MemoryConsentStore"/>.
    /// </summary>
    public interface IConsentStore
    {
        /// <summary>El registro guardado, o null si nunca se decidió (o si no se entiende: se vuelve a preguntar).</summary>
        ConsentRecord? Load();

        /// <summary>Guarda <paramref name="record"/> (se escribe ya: es la prueba del consentimiento).</summary>
        void Save(ConsentRecord record);
    }

    /// <summary><see cref="IConsentStore"/> en memoria (tests; y el juego sin el enganche del perfil: entonces se pregunta en cada arranque).</summary>
    public sealed class MemoryConsentStore : IConsentStore
    {
        /// <summary>Crea el almacén con <paramref name="record"/> ya guardado (o vacío).</summary>
        public MemoryConsentStore(ConsentRecord? record = null)
        {
            Record = record;
        }

        /// <summary>Lo guardado.</summary>
        public ConsentRecord? Record { get; private set; }

        /// <summary>Escrituras hechas.</summary>
        public int Saves { get; private set; }

        /// <inheritdoc />
        public ConsentRecord? Load() => Record;

        /// <inheritdoc />
        public void Save(ConsentRecord record)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Saves++;
        }
    }
}
