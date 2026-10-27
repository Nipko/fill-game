#nullable enable
using System;

namespace AF.Platform
{
    /// <summary>Paso de <see cref="ConsentFlow"/> (lo que la pantalla enseña).</summary>
    public enum ConsentStep
    {
        /// <summary>Nada abierto.</summary>
        None = 0,

        /// <summary>La pantalla de edad neutral (tres bandas sin ninguna marcada).</summary>
        Age = 1,

        /// <summary>La pantalla de consentimiento: finalidades separadas y «Rechazar» al mismo nivel que «Aceptar» (05 §7).</summary>
        Choices = 2,

        /// <summary>«Pide a un adulto que instale el juego» (&lt;13; 08 §7): sin botones y sin sesión de juego.</summary>
        Blocked = 3,

        /// <summary>Decidido: se puede seguir.</summary>
        Done = 4,
    }

    /// <summary>Qué abrió el flujo.</summary>
    public enum ConsentFlowMode
    {
        /// <summary>El arranque (08 §2.3): sin decisión vigente.</summary>
        Boot = 0,

        /// <summary>Ajustes › cambiar las finalidades (05 §7: «revocación en Ajustes»).</summary>
        Change = 1,

        /// <summary>Ajustes › corregir la edad, una sola vez (08 §7; 05 §7).</summary>
        CorrectAge = 2,
    }

    /// <summary>Cómo cerró el jugador la pantalla de consentimiento.</summary>
    public enum ConsentAnswer
    {
        /// <summary>«Aceptar todo»: las finalidades que se ofrecen, encendidas.</summary>
        AcceptAll = 0,

        /// <summary>«Rechazar todo»: todas apagadas.</summary>
        RejectAll = 1,

        /// <summary>«Guardar mi elección»: las marcadas.</summary>
        Save = 2,
    }

    /// <summary>
    /// Un aviso de <see cref="ConsentFlow"/> para <c>af_consent</c> (08 §8.2: <c>profile</c>, <c>text_version</c>,
    /// <c>analytics</c>, <c>ads_personalized</c>, <c>age_band</c> y <c>step ∈ {shown, accepted, rejected, changed}</c>).
    /// </summary>
    public readonly struct ConsentReport
    {
        /// <summary><c>shown</c>.</summary>
        public const string Shown = "shown";

        /// <summary><c>accepted</c>.</summary>
        public const string Accepted = "accepted";

        /// <summary><c>rejected</c>.</summary>
        public const string Rejected = "rejected";

        /// <summary><c>changed</c>.</summary>
        public const string Changed = "changed";

        /// <summary>Crea el aviso.</summary>
        public ConsentReport(string step, string profile, int textVersion, bool analytics, bool adsPersonalized, AgeBand band)
        {
            Step = step;
            Profile = profile;
            TextVersion = textVersion;
            Analytics = analytics;
            AdsPersonalized = adsPersonalized;
            Band = band;
        }

        /// <summary><c>step</c>.</summary>
        public string Step { get; }

        /// <summary><c>profile</c>.</summary>
        public string Profile { get; }

        /// <summary><c>text_version</c>.</summary>
        public int TextVersion { get; }

        /// <summary><c>analytics</c>.</summary>
        public bool Analytics { get; }

        /// <summary><c>ads_personalized</c>.</summary>
        public bool AdsPersonalized { get; }

        /// <summary><c>age_band</c> (<see cref="AgeBands.Id"/>; null sin declarar).</summary>
        public AgeBand Band { get; }
    }

    /// <summary>
    /// El recorrido de la edad y el consentimiento, sin motor (08 §2.3; 05 §7; 06 §2.1, regla 3), que pinta <c>AF.UI.ConsentScreen</c>:
    /// <list type="bullet">
    /// <item><b>Arranque</b> (<see cref="StartBoot"/>): con el bloqueo guardado, <see cref="ConsentStep.Blocked"/>; con una decisión
    /// vigente del mismo perfil legal, <see cref="ConsentStep.Done"/> sin enseñar nada; si no, la edad (si no se había declarado) y
    /// después las finalidades. Al subir <c>consent_text_version</c> o cambiar el perfil legal del país se vuelven a pedir las
    /// finalidades, no la edad [P]. Elegir <c>&lt;13</c> bloquea, se guarda y no se vuelve a preguntar.</item>
    /// <item><b>Cambiar</b> (<see cref="BeginChange"/>, desde Ajustes): las finalidades con lo guardado marcado; guardar avisa
    /// <c>changed</c>; cerrar no cambia nada.</item>
    /// <item><b>Corregir la edad</b> (<see cref="BeginAgeCorrection"/>, desde Ajustes, una vez): la pantalla neutral otra vez; una banda
    /// distinta se guarda con la marca de corregida (y <c>&lt;13</c> bloquea); la misma no gasta la corrección.</item>
    /// </list>
    /// Cada decisión se guarda en el acto (<see cref="IConsentStore"/>) con la fecha, la versión de los textos y el perfil legal: es
    /// la prueba que pide la LGPD (05 §7). No es seguro entre hilos.
    /// </summary>
    public sealed class ConsentFlow
    {
        private readonly ConsentConfig _config;
        private readonly IConsentStore _store;
        private readonly IClock _clock;
        private readonly string _profile;
        private readonly bool _countryKnown;
        private readonly string? _country;

        /// <summary>
        /// Crea el flujo para el perfil legal <paramref name="legalProfile"/> (<see cref="ConsentConfig.ProfileFor"/>);
        /// <paramref name="countryKnown"/> = false si no se supo el país (Q-T3 (a): el tratamiento más estricto, sin elegir anuncios
        /// personalizados). <paramref name="country"/> es el país de ahora (ISO alfa-2), para la edad de consentimiento digital de la
        /// analítica con 13-17 (<see cref="ConsentPolicy.OffersAnalyticsChoice"/>; D-057); null con el país conocido = el resto del mapa.
        /// </summary>
        public ConsentFlow(ConsentConfig config, IConsentStore store, IClock clock, string legalProfile, bool countryKnown = true, string? country = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _profile = LegalProfiles.IsKnown(legalProfile) ? legalProfile : LegalProfiles.Default;
            _countryKnown = countryKnown;
            _country = countryKnown ? country : null;
            Record = store.Load();
        }

        /// <summary>Lo decidido (el último registro guardado o leído), o null.</summary>
        public ConsentRecord? Record { get; private set; }

        /// <summary>El paso a la vista.</summary>
        public ConsentStep Step { get; private set; }

        /// <summary>Qué abrió el flujo.</summary>
        public ConsentFlowMode Mode { get; private set; }

        /// <summary>El perfil legal de este dispositivo.</summary>
        public string Profile => _profile;

        /// <summary>Se supo el país (si no, el tratamiento más estricto).</summary>
        public bool CountryKnown => _countryKnown;

        /// <summary>La configuración vigente.</summary>
        public ConsentConfig Config => _config;

        /// <summary>La banda de la pantalla abierta: la guardada (o <see cref="AgeBand.Unknown"/> hasta elegir).</summary>
        public AgeBand Band { get; private set; }

        /// <summary>La pantalla de consentimiento ofrece «Anuncios personalizados» (<see cref="ConsentPolicy.OffersAdsChoice"/>).</summary>
        public bool OffersAdsChoice => ConsentPolicy.OffersAdsChoice(_profile, Band, _config, _countryKnown);

        /// <summary>
        /// La pantalla de consentimiento ofrece «Analítica» con la banda de la pantalla abierta (<see cref="ConsentPolicy.OffersAnalyticsChoice"/>;
        /// D-057): no con 13-17 donde la edad de consentimiento digital del país pasa de 13, ni sin país.
        /// </summary>
        public bool OffersAnalyticsChoice => ConsentPolicy.OffersAnalyticsChoice(Band, _config, _country, _countryKnown);

        /// <summary>Lo marcado al abrir las finalidades: en el arranque, nada (consentimiento expreso); al cambiar, lo guardado.</summary>
        public bool InitialAnalytics { get; private set; }

        /// <summary>Lo marcado al abrir las finalidades (anuncios personalizados).</summary>
        public bool InitialAdsPersonalized { get; private set; }

        /// <summary>Se puede corregir la edad desde Ajustes: hay banda declarada, no es &lt;13 y no se corrigió ya.</summary>
        public bool CanCorrectAge => Record != null && Record.AgeBand != AgeBand.Unknown && Record.AgeBand != AgeBand.Under13 && !Record.AgeBandCorrected;

        /// <summary>Los flags vigentes (<see cref="ConsentPolicy.Flags"/>).</summary>
        public ConsentFlags Flags => ConsentPolicy.Flags(Record, _config, _profile, _countryKnown, _country);

        /// <summary>
        /// Los anuncios los decide el mensaje de UMP con la banda de la pantalla abierta: perfil <c>ump</c>, país conocido y 18+ (o
        /// 13-17 con <c>biz_under18_no_personalization</c> apagado), como <see cref="ConsentFlags.AdsByUmp"/>.
        /// </summary>
        public bool AdsByUmp => _countryKnown
            && string.Equals(_profile, LegalProfiles.Ump, StringComparison.Ordinal)
            && (Band == AgeBand.Adult || (Band == AgeBand.Teen && !_config.BizUnder18NoPersonalization));

        /// <summary>Cada aviso para <c>af_consent</c> (lo conecta el arranque a la analítica).</summary>
        public Action<ConsentReport>? Reported { get; set; }

        /// <summary>Arranque (08 §2.3). Devuelve el paso en el que queda.</summary>
        public ConsentStep StartBoot()
        {
            Mode = ConsentFlowMode.Boot;
            ConsentRecord? record = Record;
            if (ConsentPolicy.IsBlocked(record))
            {
                Band = AgeBand.Under13;
                return Go(ConsentStep.Blocked);
            }

            // Con otro perfil legal que el de la decisión (el país cambió: otra SIM, un viaje), las finalidades se vuelven a pedir con
            // el perfil de ahora, conservando la banda [P] (Q-B12-15): los flags siempre salen del perfil del país actual.
            bool otherProfile = record != null && !string.Equals(record.Profile, _profile, StringComparison.Ordinal);
            if (!ConsentPolicy.NeedsPrompt(record, _config) && !otherProfile)
            {
                Band = record!.AgeBand;
                return Go(ConsentStep.Done);
            }

            Band = record?.AgeBand ?? AgeBand.Unknown;
            InitialAnalytics = false;
            InitialAdsPersonalized = false;
            Report(ConsentReport.Shown, _profile, _config.ConsentTextVersion, false, false, Band);
            return Go(Band == AgeBand.Unknown ? ConsentStep.Age : ConsentStep.Choices);
        }

        /// <summary>Ajustes › cambiar las finalidades, con lo guardado marcado.</summary>
        /// <exception cref="InvalidOperationException">No hay una decisión que cambiar.</exception>
        public ConsentStep BeginChange()
        {
            ConsentRecord record = RequireDecided();
            Mode = ConsentFlowMode.Change;
            Band = record.AgeBand;
            InitialAnalytics = record.Analytics && OffersAnalyticsChoice;
            InitialAdsPersonalized = record.AdsPersonalized && OffersAdsChoice;
            return Go(ConsentStep.Choices);
        }

        /// <summary>Ajustes › corregir la edad (una vez).</summary>
        /// <exception cref="InvalidOperationException">Ya se corrigió, o no hay banda que corregir.</exception>
        public ConsentStep BeginAgeCorrection()
        {
            if (!CanCorrectAge)
            {
                throw new InvalidOperationException("La edad ya se corrigió o no hay banda que corregir (08 §7: una sola vez).");
            }

            Mode = ConsentFlowMode.CorrectAge;
            Band = Record!.AgeBand;
            return Go(ConsentStep.Age);
        }

        /// <summary>El jugador eligió <paramref name="band"/> en la pantalla de edad neutral.</summary>
        /// <exception cref="InvalidOperationException">No está en la pantalla de edad.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Banda sin declarar.</exception>
        public ConsentStep ChooseAge(AgeBand band)
        {
            if (Step != ConsentStep.Age)
            {
                throw new InvalidOperationException("La pantalla de edad no está abierta.");
            }

            if (band == AgeBand.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(band));
            }

            DateTime now = _clock.UtcNow;
            if (Mode == ConsentFlowMode.CorrectAge)
            {
                ConsentRecord current = Record!;
                if (band == current.AgeBand)
                {
                    return Go(ConsentStep.Done); // la misma banda: no gasta la corrección
                }

                // La corrección cambia solo la banda: el perfil legal y la versión de los textos son los de la decisión guardada
                // (el registro y af_consent dicen lo mismo).
                Band = band;
                if (band == AgeBand.Under13)
                {
                    Save(new ConsentRecord(current.Profile, current.TextVersion, now, false, false, AgeBand.Under13, true));
                    return Go(ConsentStep.Blocked);
                }

                // 13-17 sin personalización: los anuncios personalizados se apagan aunque se hubieran aceptado con 18+; y la analítica,
                // donde la edad de consentimiento digital pasa de 13 (D-057).
                bool ads = current.AdsPersonalized && ConsentPolicy.OffersAdsChoice(current.Profile, band, _config, _countryKnown);
                bool analytics = current.Analytics && OffersAnalyticsChoice;
                var corrected = new ConsentRecord(current.Profile, current.TextVersion, now, analytics, ads, band, true);
                Save(corrected);
                Report(ConsentReport.Changed, corrected.Profile, corrected.TextVersion, corrected.Analytics, corrected.AdsPersonalized, band);
                return Go(ConsentStep.Done);
            }

            Band = band;
            if (band == AgeBand.Under13)
            {
                // Bloqueo en la 1.0 (consent_under13_policy): se guarda para aplicarlo en cualquier arranque posterior (08 §7). Sin
                // af_consent: con <13 no hay analítica (solo el sumidero local la vería).
                Save(new ConsentRecord(_profile, _config.ConsentTextVersion, now, false, false, AgeBand.Under13));
                return Go(ConsentStep.Blocked);
            }

            return Go(ConsentStep.Choices);
        }

        /// <summary>
        /// El jugador cerró la pantalla de consentimiento con <paramref name="answer"/>; con <see cref="ConsentAnswer.Save"/> valen
        /// <paramref name="analytics"/> y <paramref name="adsPersonalized"/> (lo que no se ofrece queda apagado).
        /// </summary>
        /// <exception cref="InvalidOperationException">La pantalla de consentimiento no está abierta.</exception>
        public ConsentStep Choose(ConsentAnswer answer, bool analytics = false, bool adsPersonalized = false)
        {
            if (Step != ConsentStep.Choices)
            {
                throw new InvalidOperationException("La pantalla de consentimiento no está abierta.");
            }

            bool offersAds = OffersAdsChoice;
            bool offersAnalytics = OffersAnalyticsChoice;
            switch (answer)
            {
                case ConsentAnswer.AcceptAll:
                    analytics = offersAnalytics;
                    adsPersonalized = offersAds;
                    break;
                case ConsentAnswer.RejectAll:
                    analytics = false;
                    adsPersonalized = false;
                    break;
                default:
                    analytics = analytics && offersAnalytics;
                    adsPersonalized = adsPersonalized && offersAds;
                    break;
            }

            DateTime now = _clock.UtcNow;
            ConsentRecord record = Record != null && Record.AgeBand != AgeBand.Unknown
                ? Record.WithChoices(_profile, _config.ConsentTextVersion, now, analytics, adsPersonalized)
                : new ConsentRecord(_profile, _config.ConsentTextVersion, now, analytics, adsPersonalized, Band);
            Save(record);
            string step = Mode == ConsentFlowMode.Change
                ? ConsentReport.Changed
                : (analytics || adsPersonalized ? ConsentReport.Accepted : ConsentReport.Rejected);
            Report(step, record.Profile, record.TextVersion, analytics, adsPersonalized, Band);
            return Go(ConsentStep.Done);
        }

        /// <summary>Cerrar sin decidir (la X de Ajustes). En el arranque no se puede: no hay X.</summary>
        /// <exception cref="InvalidOperationException">En el arranque.</exception>
        public void Cancel()
        {
            if (Mode == ConsentFlowMode.Boot && Step != ConsentStep.Done && Step != ConsentStep.None)
            {
                throw new InvalidOperationException("En el arranque no se puede cerrar sin decidir (06 §2.1, regla 3).");
            }

            Go(ConsentStep.None);
        }

        private ConsentRecord RequireDecided()
        {
            ConsentRecord? record = Record;
            if (record == null || record.AgeBand == AgeBand.Unknown || record.AgeBand == AgeBand.Under13)
            {
                throw new InvalidOperationException("No hay una decisión que cambiar.");
            }

            return record;
        }

        private void Save(ConsentRecord record)
        {
            _store.Save(record);
            Record = record;
        }

        private void Report(string step, string profile, int textVersion, bool analytics, bool ads, AgeBand band)
        {
            Reported?.Invoke(new ConsentReport(step, profile, textVersion, analytics, ads, band));
        }

        private ConsentStep Go(ConsentStep step)
        {
            Step = step;
            return step;
        }
    }
}
