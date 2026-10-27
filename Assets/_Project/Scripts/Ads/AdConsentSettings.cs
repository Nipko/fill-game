#nullable enable
using System;
using AF.Platform;

namespace AF.Ads
{
    /// <summary>
    /// Lo que el proveedor de anuncios real (MAX, D-042) recibe del consentimiento y la edad (08 §2.3 y §7; 05 §7; D-033 B12),
    /// calculado sin motor a partir de <see cref="ConsentFlags"/>:
    /// <list type="bullet">
    /// <item><see cref="MayInitialize"/>: solo con una decisión vigente y sin el bloqueo de &lt;13 («MAX arranca solo tras consentir»;
    /// con &lt;13, nunca). Tampoco con 13-17 y el perfil <c>ump</c> (EEE, Reino Unido y Suiza): la automatización de UMP de MAX
    /// enseñaría su mensaje de consentimiento a un menor, así que hasta que decidan el dueño y el abogado, sin anuncios
    /// (Q-MAX-6 (b), recomendada provisional, D-042).</item>
    /// <item><see cref="HasUserConsent"/> y <see cref="DoNotSell"/>: los dos flags de privacidad que expone MAX 13
    /// (<c>MaxSdk.SetHasUserConsent</c> y <c>MaxSdk.SetDoNotSell</c>). Con <c>npa</c> (sin consentimiento de anuncios personalizados,
    /// 13-17 con <c>biz_under18_no_personalization</c> o sin país), false y true. Con el perfil <c>ump</c> de 18+, null: los decide el
    /// mensaje de UMP que MAX muestra al iniciarse (las cadenas TCF v2 que pasa a las redes, 08 §7 #2) y el juego no los fija.</item>
    /// <item><see cref="MaxAdContentRating"/>: G en todas las bandas (D-029). MAX 13 no tiene API para el contenido máximo: se fija en
    /// las consolas de las redes (en AdMob, «Contenido máximo de los anuncios» = G para la app) [NV por red].</item>
    /// <item><see cref="ChildDirected"/>: TFCD con &lt;13; MAX 13 ya no tiene API para usuarios menores (quitó
    /// <c>SetIsAgeRestrictedUser</c>), así que con &lt;13 simplemente no se inicia [NV].</item>
    /// </list>
    /// Inmutable.
    /// </summary>
    public sealed class AdConsentSettings
    {
        /// <summary>Sin decisión: no se inicia.</summary>
        public static readonly AdConsentSettings NotDecided = new AdConsentSettings(false, false, false, true, ConsentFlags.MaxAdContentRatingG, false, false);

        /// <summary>El país legal de EE. UU., donde 13-17 pide el tratamiento restringido de datos (T-L5).</summary>
        public const string RestrictedDataProcessingCountry = "US";

        private AdConsentSettings(
            bool mayInitialize, bool childDirected, bool? hasUserConsent, bool? doNotSell, string rating, bool umpDecides, bool teen, bool rdp = false)
        {
            RestrictedDataProcessing = rdp;
            Teen = teen;
            MayInitialize = mayInitialize;
            ChildDirected = childDirected;
            HasUserConsent = hasUserConsent;
            DoNotSell = doNotSell;
            MaxAdContentRating = rating;
            UmpDecides = umpDecides;
        }

        /// <summary>Se puede iniciar el SDK de anuncios (decisión vigente y no &lt;13).</summary>
        public bool MayInitialize { get; }

        /// <summary>Banda &lt;13 (TFCD). Con ella <see cref="MayInitialize"/> es false.</summary>
        public bool ChildDirected { get; }

        /// <summary><c>SetHasUserConsent</c>: false con <c>npa</c>, true con anuncios personalizados y null si lo decide UMP.</summary>
        public bool? HasUserConsent { get; }

        /// <summary><c>SetDoNotSell</c>: true con <c>npa</c>, false con anuncios personalizados y null si lo decide UMP.</summary>
        public bool? DoNotSell { get; }

        /// <summary>Anuncios no personalizados (<c>npa</c>).</summary>
        public bool NonPersonalized => HasUserConsent == false;

        /// <summary>Contenido máximo: G (D-029).</summary>
        public string MaxAdContentRating { get; }

        /// <summary>Perfil <c>ump</c> con 18+: la personalización la decide el mensaje de UMP.</summary>
        public bool UmpDecides { get; }

        /// <summary>
        /// Banda 13-17 (D-049): Google Mobile Ads marca la petición y el formulario de UMP como de un menor de la edad de
        /// consentimiento (<c>TagForUnderAgeOfConsent</c>), además de <c>npa</c>.
        /// </summary>
        public bool Teen { get; }

        /// <summary>
        /// Tratamiento restringido de datos de Google (RDP; T-L5 de <c>docs/legal/investigacion-legal-mundial.md</c> §3.3 y §11, [F28]):
        /// con 13-17 y el país legal <c>US</c>, cada petición de anuncios lleva el extra <c>rdp=1</c>, para que los anuncios de un menor
        /// no cuenten como «compartir» en las leyes de los estados de EE. UU.
        /// </summary>
        public bool RestrictedDataProcessing { get; }

        /// <summary>
        /// Lo que corresponde a <paramref name="flags"/>; <paramref name="legalCountry"/> es el país del perfil legal (ISO alfa-2, null si
        /// no se supo): con <c>US</c> y 13-17, <see cref="RestrictedDataProcessing"/>.
        /// </summary>
        public static AdConsentSettings From(ConsentFlags flags, string? legalCountry = null)
        {
            if (flags == null)
            {
                throw new ArgumentNullException(nameof(flags));
            }

            if (flags.Blocked)
            {
                return new AdConsentSettings(false, true, false, true, flags.MaxAdContentRating, false, false);
            }

            if (!flags.Decided)
            {
                return NotDecided;
            }

            if (flags.Band == AgeBand.Teen && string.Equals(flags.Profile, LegalProfiles.Ump, StringComparison.Ordinal))
            {
                return new AdConsentSettings(false, false, false, true, flags.MaxAdContentRating, false, true); // Q-MAX-6 (b)
            }

            if (flags.AdsByUmp)
            {
                return new AdConsentSettings(true, false, null, null, flags.MaxAdContentRating, true, false);
            }

            bool personalized = flags.AdsPersonalized;
            bool teen = flags.Band == AgeBand.Teen;
            bool rdp = teen && string.Equals(legalCountry, RestrictedDataProcessingCountry, StringComparison.OrdinalIgnoreCase);
            return new AdConsentSettings(true, false, personalized, !personalized, flags.MaxAdContentRating, false, teen, rdp);
        }
    }

    /// <summary>Un proveedor de anuncios con trabajo por fotograma y al volver de segundo plano (MAX: los márgenes de cierre).</summary>
    public interface IAdTicker
    {
        /// <summary>Cada fotograma.</summary>
        void Tick();

        /// <summary>La app vuelve de segundo plano.</summary>
        void OnResume();
    }

    /// <summary>
    /// Un proveedor de anuncios que necesita el consentimiento (MAX, D-042): <see cref="ConsentAdService"/> le pasa cada decisión y le
    /// reenvía las opciones de privacidad de UMP. El proveedor falso no lo implementa.
    /// </summary>
    public interface IAdConsentTarget
    {
        /// <summary>Aplica <paramref name="settings"/>: la primera vez que permitan iniciar, inicia el SDK; después solo actualiza los flags.</summary>
        void ApplyConsent(AdConsentSettings settings);

        /// <summary>Vuelve a abrir el mensaje de UMP (Ajustes › Privacidad con el perfil <c>ump</c>). Devuelve si se pudo pedir.</summary>
        bool ShowPrivacyOptions();
    }
}
