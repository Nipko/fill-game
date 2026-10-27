#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>
    /// Lo que el consentimiento y la edad permiten (08 §2.3 y §7; 05 §7): los flags que reciben los anuncios
    /// (<c>AF.Ads.ConsentAdService</c>), la analítica (<c>AnalyticsService.SetCollectionEnabled</c>) y la tienda
    /// (<c>AF.IAP.ConsentStoreService</c>), más los de Play Games y Remote Config personalizado, que se anotan para cuando lleguen
    /// sus SDK. Inmutable; lo calcula <see cref="ConsentPolicy.Flags"/>.
    /// </summary>
    public sealed class ConsentFlags
    {
        /// <summary>El contenido máximo de los anuncios: G en todas las bandas (05 §7, fila «Anunciantes y cierre», D-029).</summary>
        public const string MaxAdContentRatingG = "G";

        internal ConsentFlags(
            bool decided, bool blocked, AgeBand band, string? profile, bool analytics, bool adsPersonalized, bool childDirected,
            bool purchasesAllowed, bool purchaseDoubleConfirm, bool playGamesAllowed, bool rcPersonalization, bool adsByUmp = false,
            bool bandFromSignal = false)
        {
            AdsByUmp = adsByUmp;
            BandFromSignal = bandFromSignal;
            Decided = decided;
            Blocked = blocked;
            Band = band;
            Profile = profile;
            AnalyticsEnabled = analytics;
            AdsPersonalized = adsPersonalized;
            ChildDirected = childDirected;
            PurchasesAllowed = purchasesAllowed;
            PurchaseDoubleConfirm = purchaseDoubleConfirm;
            PlayGamesAllowed = playGamesAllowed;
            RcPersonalization = rcPersonalization;
        }

        /// <summary>
        /// Hay una decisión que permite jugar (banda y finalidades, con los textos vigentes). Sin ella, los anuncios no se inicializan
        /// (08 §2.3: los anuncios arrancan solo tras consentir) y la analítica no recoge.
        /// </summary>
        public bool Decided { get; }

        /// <summary>Banda &lt;13: no se crea sesión de juego (pantalla «Pide a un adulto que instale el juego»).</summary>
        public bool Blocked { get; }

        /// <summary>
        /// La banda que se aplica: la declarada o, con Play Age Signals (T-L3), la más estricta de la declarada y la de la señal
        /// (<see cref="AgeSignalPolicy.Effective"/>).
        /// </summary>
        public AgeBand Band { get; }

        /// <summary>
        /// La señal de Play Age Signals cambió la banda declarada (o bloqueó sin banda declarada). Entonces la banda no se manda a la
        /// analítica (T-L4; [F25]): <c>af_consent.age_band</c> solo lleva la autodeclarada y solo si la señal no la corrigió.
        /// </summary>
        public bool BandFromSignal { get; }

        /// <summary>El perfil legal del registro (null sin decidir).</summary>
        public string? Profile { get; }

        /// <summary>La analítica puede recoger (<c>Analytics.SetEnabled(profile.consent.analytics)</c>, 08 §2.3).</summary>
        public bool AnalyticsEnabled { get; }

        /// <summary>Anuncios personalizados según la pantalla propia; false = <c>npa</c> (salvo con <see cref="AdsByUmp"/>).</summary>
        public bool AdsPersonalized { get; }

        /// <summary>
        /// Perfil <c>ump</c> con 18+: la personalización de los anuncios la decide el mensaje de UMP (las cadenas TCF que UMP pasa a
        /// las redes, 08 §7 #2), no la pantalla propia; el juego no fuerza <c>npa</c>.
        /// </summary>
        public bool AdsByUmp { get; }

        /// <summary><c>TagForChildDirectedTreatment</c> = true (banda &lt;13; 08 §7).</summary>
        public bool ChildDirected { get; }

        /// <summary><c>maxAdContentRating</c>: <see cref="MaxAdContentRatingG"/>.</summary>
        public string MaxAdContentRating => MaxAdContentRatingG;

        /// <summary>Se puede comprar (no con &lt;13 ni sin decidir).</summary>
        public bool PurchasesAllowed { get; }

        /// <summary>13-17: cada compra pide una segunda confirmación en el juego antes de la hoja de Google Play (Q-T4 (a)).</summary>
        public bool PurchaseDoubleConfirm { get; }

        /// <summary>Inicio de sesión en Play Games (no con &lt;13; 08 §7). Sin SDK aún: solo se anota.</summary>
        public bool PlayGamesAllowed { get; }

        /// <summary>Remote Config personalizado (no con &lt;13 ni, con <c>biz_under18_no_personalization</c>, con 13-17). Solo se anota.</summary>
        public bool RcPersonalization { get; }
    }

    /// <summary>
    /// Las reglas del consentimiento y la edad, sin motor (08 §2.3 y §7; 05 §7; Q-T3 (a) y Q-T4 (a) de D-033):
    /// <list type="bullet">
    /// <item>Sin registro (o con la banda sin declarar), todo apagado y sin decidir: la pantalla de edad neutral y la de
    /// consentimiento van antes del nivel 1 (06 §2.1, regla 3).</item>
    /// <item><c>&lt;13</c>: bloqueo (<c>consent_under13_policy</c>; <c>parental_consent</c> está reservado y, sin implementar,
    /// también bloquea [P]); TFCD, sin analítica, sin compras, sin Play Games ni Remote Config personalizado. Es definitivo: no se
    /// vuelve a preguntar ni al subir <c>consent_text_version</c> («en cualquier arranque posterior», 08 §7) [P].</item>
    /// <item><c>13-17</c> con <c>biz_under18_no_personalization</c>: <c>npa</c> aunque se acepte, sin Remote Config personalizado
    /// y con doble confirmación de compras; la analítica, según la decisión, salvo donde la edad de consentimiento digital del país
    /// pasa de 13 o sin país (<see cref="OffersAnalyticsChoice"/>; D-057): ahí no se ofrece y queda apagada.</item>
    /// <item><c>18+</c>: lo que se decidió.</item>
    /// <item>En todas las bandas, <c>maxAdContentRating</c> = G (D-029).</item>
    /// </list>
    /// </summary>
    public static class ConsentPolicy
    {
        /// <summary>Sin decidir: todo apagado.</summary>
        public static ConsentFlags Undecided { get; } = new ConsentFlags(false, false, AgeBand.Unknown, null, false, false, false, false, false, false, false);

        /// <summary>
        /// Hay que pedir (o volver a pedir) la decisión: no hay registro, la banda no se declaró o los textos legales son más nuevos
        /// que los aceptados (<c>consent_text_version</c>). Con <c>&lt;13</c> nunca (el bloqueo es definitivo).
        /// </summary>
        public static bool NeedsPrompt(ConsentRecord? record, ConsentConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (record == null || record.AgeBand == AgeBand.Unknown)
            {
                return true;
            }

            return record.AgeBand != AgeBand.Under13 && record.TextVersion < config.ConsentTextVersion;
        }

        /// <summary>La banda guardada bloquea el juego.</summary>
        public static bool IsBlocked(ConsentRecord? record)
        {
            return record != null && record.AgeBand == AgeBand.Under13;
        }

        /// <summary>
        /// El arranque puede encender los servicios que recogen o descargan antes de la pantalla de edad: Crashlytics, la descarga
        /// de Remote Config, el sumidero de Firebase Analytics (apagado hasta el consentimiento) y la nube. Sin banda guardada
        /// (primer arranque), 13-17 y 18+, sí, como siempre (08 §2.3); con la banda &lt;13 guardada, nada en ningún momento del
        /// arranque: se lee del perfil antes de iniciar ningún SDK (B12; 08 §7 y §13.1 #11; 05 §7).
        /// </summary>
        public static bool StartsDataServicesAtBoot(ConsentRecord? record)
        {
            return !IsBlocked(record);
        }

        /// <summary>
        /// Los flags de <paramref name="record"/> con <paramref name="config"/>. Con <paramref name="currentProfile"/> (el perfil legal
        /// del país de ahora), una decisión de otro perfil cuenta como sin decidir hasta volver a preguntar (Q-B12-15); sin país
        /// (<paramref name="countryKnown"/> = false), los anuncios nunca son personalizados ni los decide UMP (Q-T3 (a), Q-B12-13). Con
        /// 13-17, la analítica solo si <see cref="OffersAnalyticsChoice"/> con <paramref name="country"/> (el país de ahora; null con
        /// <paramref name="countryKnown"/> = true, el resto del mapa) (D-057). <paramref name="signalBand"/> es la banda de Play Age
        /// Signals (T-L3; <see cref="AgeBand.Unknown"/> sin señal): 0-12 bloquea y, si no, se aplica la más estricta de la declarada y
        /// la de la señal.
        /// </summary>
        public static ConsentFlags Flags(
            ConsentRecord? record, ConsentConfig config, string? currentProfile = null, bool countryKnown = true, string? country = null,
            AgeBand signalBand = AgeBand.Unknown)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Play Age Signals (T-L3): con una señal de 0-12, el bloqueo aunque no haya banda declarada (sin guardarlo: la señal se
            // vuelve a pedir en cada arranque); con 13-17, la más estricta de las dos (AgeSignalPolicy.Effective).
            AgeBand declared = record?.AgeBand ?? AgeBand.Unknown;
            if (signalBand == AgeBand.Under13 && declared != AgeBand.Under13)
            {
                return new ConsentFlags(
                    decided: false, blocked: true, band: AgeBand.Under13, profile: record?.Profile, analytics: false, adsPersonalized: false,
                    childDirected: true, purchasesAllowed: false, purchaseDoubleConfirm: false, playGamesAllowed: false, rcPersonalization: false,
                    bandFromSignal: true);
            }

            if (record == null || declared == AgeBand.Unknown)
            {
                return Undecided;
            }

            bool sameProfile = currentProfile == null || string.Equals(record.Profile, currentProfile, StringComparison.Ordinal);
            AgeBand band = AgeSignalPolicy.Effective(declared, signalBand);
            bool fromSignal = band != declared;

            switch (band)
            {
                case AgeBand.Under13:
                    return new ConsentFlags(
                        decided: false, blocked: true, band: AgeBand.Under13, profile: record.Profile, analytics: false, adsPersonalized: false,
                        childDirected: true, purchasesAllowed: false, purchaseDoubleConfirm: false, playGamesAllowed: false, rcPersonalization: false);

                case AgeBand.Teen:
                {
                    // Con biz_under18_no_personalization apagado, 13-17 como 18+ (también con UMP), salvo TFCD.
                    bool strict = config.BizUnder18NoPersonalization;
                    bool decided = !NeedsPrompt(record, config) && sameProfile;
                    bool ump = string.Equals(record.Profile, LegalProfiles.Ump, StringComparison.Ordinal);
                    bool analytics = decided && record.Analytics && OffersAnalyticsChoice(AgeBand.Teen, config, country, countryKnown);
                    return new ConsentFlags(
                        decided: decided, blocked: false, band: AgeBand.Teen, profile: record.Profile, analytics: analytics,
                        adsPersonalized: decided && !strict && !ump && countryKnown && record.AdsPersonalized, childDirected: false,
                        purchasesAllowed: decided, purchaseDoubleConfirm: strict, playGamesAllowed: decided, rcPersonalization: decided && !strict,
                        adsByUmp: decided && !strict && ump && countryKnown, bandFromSignal: fromSignal);
                }

                default:
                {
                    bool decided = !NeedsPrompt(record, config) && sameProfile;
                    bool ump = string.Equals(record.Profile, LegalProfiles.Ump, StringComparison.Ordinal);
                    return new ConsentFlags(
                        decided: decided, blocked: false, band: AgeBand.Adult, profile: record.Profile, analytics: decided && record.Analytics,
                        adsPersonalized: decided && !ump && countryKnown && record.AdsPersonalized, childDirected: false, purchasesAllowed: decided,
                        purchaseDoubleConfirm: false, playGamesAllowed: decided, rcPersonalization: decided, adsByUmp: decided && ump && countryKnown,
                        bandFromSignal: fromSignal);
                }
            }
        }

        /// <summary>
        /// La pantalla de consentimiento propia ofrece la finalidad «Anuncios personalizados»: solo con 18+ (con 13-17 y
        /// <c>biz_under18_no_personalization</c> no se puede activar), fuera de UMP (en el EEE, el Reino Unido y Suiza la decide el
        /// mensaje de UMP de Google Mobile Ads, 08 §7 #2) y con el país conocido: sin país (Q-T3 (a), «el tratamiento
        /// más estricto»), <c>npa</c> sin elección [P] (Q-B12-13).
        /// </summary>
        public static bool OffersAdsChoice(string profile, AgeBand band, ConsentConfig config, bool countryKnown = true)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!countryKnown || string.Equals(profile, LegalProfiles.Ump, StringComparison.Ordinal))
            {
                return false;
            }

            return band == AgeBand.Adult || (band == AgeBand.Teen && !config.BizUnder18NoPersonalization);
        }

        /// <summary>
        /// La pantalla de consentimiento ofrece la finalidad «Analítica» (D-057, P2 (a); 05 §7; 08 §9,
        /// <c>consent_digital_age_by_country</c>): con 18+, siempre; con 13-17, solo si la edad de consentimiento digital del país
        /// (<see cref="ConsentConfig.DigitalAgeFor"/>) es 13. Donde pasa de 13 (RGPD art. 8 en buena parte del EEE, Suiza, Colombia, Brasil, México, Corea, la
        /// India…) la banda 13-17 no distingue a quien ya puede consentir, así que la analítica no se ofrece y queda apagada. Sin país
        /// (<paramref name="countryKnown"/> = false), tampoco: el tratamiento más estricto (Q-T3 (a); D-057, decisiones de la revisión, punto 3). Con
        /// <paramref name="country"/> null y el país conocido, la edad del resto del mapa.
        /// </summary>
        public static bool OffersAnalyticsChoice(AgeBand band, ConsentConfig config, string? country, bool countryKnown = true)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (band != AgeBand.Teen)
            {
                return true;
            }

            return countryKnown && config.DigitalAgeFor(country) <= ConsentConfig.MinDigitalAge;
        }
    }

    /// <summary>De dónde salió el país del perfil legal (Q-T3 (a)).</summary>
    public enum CountrySource
    {
        /// <summary>No se supo: perfil <c>default</c>, el más estricto.</summary>
        None = 0,

        /// <summary>El país de la SIM (<c>TelephonyManager.getSimCountryIso</c>, sin permiso).</summary>
        Sim = 1,

        /// <summary>La región del teléfono (la de <see cref="IDeviceLocale"/>, p. ej. <c>es-CO</c> → CO).</summary>
        Region = 2,

        /// <summary>
        /// El país de la cuenta de Google Play (<c>getBillingConfigAsync</c>, guardado en el dispositivo desde la última conexión con
        /// Play; <see cref="IPlayAccountCountry"/>; D-057, P4 (a)).
        /// </summary>
        PlayAccount = 3,
    }

    /// <summary>
    /// País de la SIM (Q-T3 (a)): la lectura de Android por el JNI de Unity vive en <c>AF.UI</c> (<c>AndroidSimCountry</c>; necesita
    /// el motor); el Editor y los tests usan <see cref="ManualSimCountry"/>.
    /// </summary>
    public interface ISimCountry
    {
        /// <summary>El código ISO del país de la SIM tal como lo da el sistema, o <c>""</c> si no hay SIM o no se puede leer. Nunca null.</summary>
        string CountryIso { get; }
    }

    /// <summary>
    /// El país de la cuenta de Google Play que vio la última conexión con Play (D-057, P4 (a)): lo pide <c>AF.IAP.PlayBillingClient</c>
    /// (<c>getBillingConfigAsync</c>) y lo guarda <c>AF.UI.PlayerPrefsPlayAccountCountry</c> en el dispositivo, porque Play Billing se
    /// conecta después de la pantalla de edad y el país legal se decide antes. Es un país, no un identificador: no viaja a la nube,
    /// pero el país legal que resulta (SIM → Play → región) sí va como <c>region</c> de la Liga y, agregado, en <c>geo_tier</c> (D-057,
    /// decisiones de la revisión, punto 1). El Editor y los tests usan <see cref="ManualPlayAccountCountry"/>.
    /// </summary>
    public interface IPlayAccountCountry
    {
        /// <summary>El código ISO del país de la cuenta tal como se guardó, o <c>""</c> si aún no se sabe. Nunca null.</summary>
        string CountryIso { get; }
    }

    /// <summary><see cref="IPlayAccountCountry"/> que además guarda el país que llega de Play (D-057).</summary>
    public interface IPlayAccountCountryStore : IPlayAccountCountry
    {
        /// <summary>Guarda <paramref name="countryIso"/> si son dos letras (en mayúsculas); otra cosa no cambia nada.</summary>
        void Save(string countryIso);
    }

    /// <summary><see cref="IPlayAccountCountryStore"/> en memoria (Editor y tests).</summary>
    public sealed class MemoryPlayAccountCountry : IPlayAccountCountryStore
    {
        /// <summary>Crea el almacén con <paramref name="countryIso"/> ya guardado (o vacío).</summary>
        public MemoryPlayAccountCountry(string? countryIso = null)
        {
            CountryIso = ConsentCountry.Normalize(countryIso) ?? string.Empty;
        }

        /// <inheritdoc />
        public string CountryIso { get; private set; }

        /// <summary>Veces que cambió lo guardado.</summary>
        public int Saves { get; private set; }

        /// <inheritdoc />
        public void Save(string countryIso)
        {
            string? code = ConsentCountry.Normalize(countryIso);
            if (code != null && code != CountryIso)
            {
                CountryIso = code;
                Saves++;
            }
        }
    }

    /// <summary><see cref="IPlayAccountCountry"/> fijo (Editor y tests).</summary>
    public sealed class ManualPlayAccountCountry : IPlayAccountCountry
    {
        /// <summary>Sin país de Play.</summary>
        public static readonly ManualPlayAccountCountry None = new ManualPlayAccountCountry(string.Empty);

        /// <summary>Crea el país de Play falso con <paramref name="countryIso"/>.</summary>
        public ManualPlayAccountCountry(string? countryIso)
        {
            CountryIso = countryIso ?? string.Empty;
        }

        /// <inheritdoc />
        public string CountryIso { get; }
    }

    /// <summary><see cref="ISimCountry"/> fijo (Editor y tests).</summary>
    public sealed class ManualSimCountry : ISimCountry
    {
        /// <summary>Sin SIM.</summary>
        public static readonly ManualSimCountry None = new ManualSimCountry(string.Empty);

        /// <summary>Crea la SIM falsa con <paramref name="countryIso"/>.</summary>
        public ManualSimCountry(string? countryIso)
        {
            CountryIso = countryIso ?? string.Empty;
        }

        /// <inheritdoc />
        public string CountryIso { get; }
    }

    /// <summary>
    /// El país del perfil legal sin servidor (Q-T3 (a) de D-033 y D-057, P4 (a)), por este orden:
    /// <list type="number">
    /// <item>la SIM (<see cref="CountrySource.Sim"/>): donde está la línea, la mejor señal de dónde vive el jugador;</item>
    /// <item>la cuenta de Google Play (<see cref="CountrySource.PlayAccount"/>): donde paga y donde Play distribuye; cubre las
    /// tabletas sin SIM, que antes caían en la región del idioma (un alemán con el teléfono en en-US quedaba como US);</item>
    /// <item>la región del teléfono (<see cref="CountrySource.Region"/>): solo una preferencia de idioma;</item>
    /// <item>ninguno: <see cref="ConsentConfig.ProfileFor"/> da <c>default</c>, el tratamiento más estricto.</item>
    /// </list>
    /// El país de Play llega después de la pantalla de edad (Play Billing se conecta tras ella), así que vale desde el arranque
    /// siguiente; si entonces cambia el perfil legal, las finalidades se vuelven a pedir (Q-B12-15). Detalle en
    /// <c>docs/dev/consentimiento.md</c>.
    /// </summary>
    public static class ConsentCountry
    {
        /// <summary>El país (ISO alfa-2 en mayúsculas) o null, y de dónde salió (sin el país de Play).</summary>
        public static string? Resolve(ISimCountry? sim, IDeviceLocale? locale, out CountrySource source)
        {
            return Resolve(sim, null, locale, out source);
        }

        /// <summary>El país (ISO alfa-2 en mayúsculas) o null, y de dónde salió: SIM → cuenta de Play → región → ninguno.</summary>
        public static string? Resolve(ISimCountry? sim, IPlayAccountCountry? playAccount, IDeviceLocale? locale, out CountrySource source)
        {
            string? fromSim = Normalize(sim?.CountryIso);
            if (fromSim != null)
            {
                source = CountrySource.Sim;
                return fromSim;
            }

            string? fromPlay = Normalize(playAccount?.CountryIso);
            if (fromPlay != null)
            {
                source = CountrySource.PlayAccount;
                return fromPlay;
            }

            string? fromRegion = RegionOf(locale?.LanguageTag);
            if (fromRegion != null)
            {
                source = CountrySource.Region;
                return fromRegion;
            }

            source = CountrySource.None;
            return null;
        }

        /// <summary>Nombre de <paramref name="source"/> en la sonda: <c>sim</c>, <c>play</c>, <c>region</c> o <c>none</c>.</summary>
        public static string SourceKey(CountrySource source)
        {
            switch (source)
            {
                case CountrySource.Sim: return "sim";
                case CountrySource.PlayAccount: return "play";
                case CountrySource.Region: return "region";
                default: return "none";
            }
        }

        /// <summary>
        /// La región de una etiqueta BCP 47 (<c>es-CO</c> → CO, <c>zh-Hant-TW</c> → TW, <c>pt_BR</c> → BR): la primera subetiqueta
        /// de dos letras tras el idioma. Las regiones numéricas (<c>es-419</c>) no son un país: null.
        /// </summary>
        public static string? RegionOf(string? tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return null;
            }

            string[] parts = tag!.Split('-', '_');
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 1)
                {
                    break; // extensiones (-u-, -x-…): lo que sigue no es región
                }

                string? code = Normalize(parts[i]);
                if (code != null)
                {
                    return code;
                }
            }

            return null;
        }

        /// <summary>Dos letras ASCII → mayúsculas; cualquier otra cosa (vacío, «419», «Hant»), null.</summary>
        public static string? Normalize(string? code)
        {
            if (code == null)
            {
                return null;
            }

            string trimmed = code.Trim();
            if (trimmed.Length != 2)
            {
                return null;
            }

            char a = trimmed[0];
            char b = trimmed[1];
            if (!IsAsciiLetter(a) || !IsAsciiLetter(b))
            {
                return null;
            }

            return trimmed.ToUpperInvariant();
        }

        private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }
}
