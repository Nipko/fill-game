#nullable enable
using System;
using AF.Platform;

namespace AF.Ads
{
    /// <summary>
    /// Los anuncios detrás del consentimiento y la edad (bloque B12; 08 §2.3: <c>Ads.InitAsync(profile.consent, age_band)</c>,
    /// «los anuncios arrancan solo tras consentir»; 08 §7: TFCD con &lt;13, <c>npa</c> sin consentimiento o con 13-17 y
    /// <c>maxAdContentRating</c> = G en todas las bandas). Envuelve al proveedor (<see cref="FakeAdService"/> en el editor y los tests;
    /// <see cref="GoogleAdService"/> en el teléfono, D-049):
    /// hasta <see cref="Apply"/> con una decisión, no hay anuncio cargado (<see cref="IsReady"/> = false y <see cref="Show"/> da
    /// <see cref="AdOutcome.NoFill"/>), igual que un SDK sin inicializar. Los flags que recibe el SDK quedan en
    /// <see cref="ChildDirected"/>, <see cref="NonPersonalized"/> y <see cref="MaxAdContentRating"/>; si el proveedor es
    /// <see cref="IAdConsentTarget"/> (Google), recibe además cada decisión como <see cref="AdConsentSettings"/>: así el SDK no se inicia
    /// antes de consentir ni nunca con &lt;13. Si el proveedor informa de impresiones (<see cref="IAdImpressionSource"/>), se reenvían.
    /// Hilo principal.
    /// </summary>
    public sealed class ConsentAdService : IAdService, IAdImpressionSource
    {
        private readonly IAdService _inner;

        /// <summary>Envuelve <paramref name="inner"/>; empieza sin inicializar.</summary>
        public ConsentAdService(IAdService inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>El proveedor envuelto.</summary>
        public IAdService Inner => _inner;

        /// <inheritdoc />
        public event Action<AdImpression>? Impression
        {
            add
            {
                if (_inner is IAdImpressionSource source)
                {
                    source.Impression += value;
                }
            }

            remove
            {
                if (_inner is IAdImpressionSource source)
                {
                    source.Impression -= value;
                }
            }
        }

        /// <summary>Anuncios «inicializados»: hay una decisión de consentimiento y edad que permite jugar.</summary>
        public bool Initialized { get; private set; }

        /// <summary><c>TagForChildDirectedTreatment</c>.</summary>
        public bool ChildDirected { get; private set; }

        /// <summary><c>npa</c> = 1 (anuncios no personalizados). Cierto hasta que haya consentimiento de anuncios personalizados.</summary>
        public bool NonPersonalized { get; private set; } = true;

        /// <summary><c>maxAdContentRating</c>.</summary>
        public string MaxAdContentRating { get; private set; } = ConsentFlags.MaxAdContentRatingG;

        /// <summary>
        /// Perfil <c>ump</c> con 18+: la personalización la deciden las cadenas TCF del mensaje de UMP, así que no se fuerza <c>npa</c>
        /// (<see cref="NonPersonalized"/> = false) y el adaptador real no pasa ese flag.
        /// </summary>
        public bool UmpDecides { get; private set; }

        /// <summary>Veces que se pidieron las opciones de privacidad de UMP desde Ajustes (Google: el formulario de UMP para un usuario ya preguntado).</summary>
        public int PrivacyOptionsRequests { get; private set; }

        /// <summary>Veces que se aplicaron flags (la primera inicializa; las siguientes, desde Ajustes, los actualizan).</summary>
        public int Applies { get; private set; }

        /// <summary>
        /// Aplica <paramref name="flags"/>: con una decisión vigente, inicializa (la primera vez) y fija los flags; con el bloqueo o sin
        /// decisión, deja los anuncios sin cargar (y con el bloqueo, TFCD por si algún día se inicializara).
        /// </summary>
        public void Apply(ConsentFlags flags)
        {
            if (flags == null)
            {
                throw new ArgumentNullException(nameof(flags));
            }

            ChildDirected = flags.ChildDirected;
            UmpDecides = flags.AdsByUmp;
            NonPersonalized = !flags.AdsPersonalized && !flags.AdsByUmp;
            MaxAdContentRating = flags.MaxAdContentRating;
            Initialized = flags.Decided && !flags.Blocked;
            Applies++;
            if (_inner is IAdConsentTarget target)
            {
                target.ApplyConsent(AdConsentSettings.From(flags));
            }
        }

        /// <summary>
        /// Ajustes › Privacidad con el perfil <c>ump</c>: vuelve a abrir el mensaje de UMP para cambiar o retirar el consentimiento
        /// de los anuncios (05 §7: «revocación en Ajustes»). Con el proveedor falso solo se cuenta; con Google, su formulario de
        /// privacidad [NV]. Devuelve si se pidió (solo con <see cref="UmpDecides"/>).
        /// </summary>
        public bool ShowPrivacyOptions()
        {
            if (!UmpDecides)
            {
                return false;
            }

            PrivacyOptionsRequests++;
            if (_inner is IAdConsentTarget target)
            {
                return target.ShowPrivacyOptions(); // Google: false si UMP no pide opciones de privacidad
            }

            return true;
        }

        /// <inheritdoc />
        public bool IsShowing => _inner.IsShowing;

        /// <inheritdoc />
        public bool IsReady(AdPlacement placement) => Initialized && _inner.IsReady(placement);

        /// <inheritdoc />
        public void Show(AdPlacement placement, Action<AdOutcome> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            if (!Initialized)
            {
                onDone(AdOutcome.NoFill); // sin inicializar no hay nada cargado
                return;
            }

            _inner.Show(placement, onDone);
        }
    }
}
