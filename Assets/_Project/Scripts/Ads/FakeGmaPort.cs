#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Ads
{
    /// <summary>
    /// Google Mobile Ads falso para el editor y los tests (D-049): apunta cada llamada en <see cref="Calls"/>, en orden, y deja
    /// disparar los avisos a mano (<see cref="ResolveConsent"/>, <see cref="CompleteInitialization"/>, <see cref="Raise"/>,
    /// <see cref="RaisePaid"/>) o solos (<see cref="AutoConsent"/>, <see cref="AutoInitialize"/>, <see cref="AutoFill"/>). Sin red ni SDK.
    /// </summary>
    public sealed class FakeGmaPort : IGmaPort
    {
        private readonly bool[] _ready = new bool[2];

        /// <summary>
        /// Cada llamada, en orden: <c>config:tfua=false</c>, <c>ump:tfua=false</c>, <c>init</c>, <c>load:rewarded:npa=true</c> (con <c>:rdp=true</c> si va <c>rdp=1</c>),
        /// <c>show:rewarded:user</c> (o <c>:nouser</c>), <c>privacy</c>. El UID no se apunta (nunca a los logs).
        /// </summary>
        public List<string> Calls { get; } = new List<string>();

        /// <inheritdoc />
        public IGmaListener? Listener { get; set; }

        /// <summary>
        /// Cada <see cref="SetMediationConsent"/>, en orden y aparte de <see cref="Calls"/>: <c>unity:gdpr=false:privacy=false</c> (una
        /// clave que no se manda sale como <c>-</c>) y, al final, <c>:before-init</c> o <c>:after-init</c>.
        /// </summary>
        public List<string> MediationConsent { get; } = new List<string>();

        /// <summary>Veces que se llamó a <c>MobileAds.Initialize</c>.</summary>
        public int Initializations { get; private set; }

        /// <summary>Veces que se pidió UMP.</summary>
        public int ConsentRequests { get; private set; }

        /// <summary>El último <c>user_id</c> del SSV (null si no hubo).</summary>
        public string? LastUserId { get; private set; }

        /// <summary>El último <c>custom_data</c> del SSV.</summary>
        public string? LastCustomData { get; private set; }

        /// <summary>UMP responde en el acto con <see cref="CanRequestAds"/>.</summary>
        public bool AutoConsent { get; set; }

        /// <summary>Lo que responde UMP (<c>CanRequestAds</c>).</summary>
        public bool CanRequestAds { get; set; } = true;

        /// <summary><c>MobileAds.Initialize</c> avisa en el acto.</summary>
        public bool AutoInitialize { get; set; }

        /// <summary>Cargas automáticas: cada <c>Load</c> deja el anuncio listo y avisa en el acto.</summary>
        public bool AutoFill { get; set; }

        /// <inheritdoc />
        public bool PrivacyOptionsRequired { get; set; }

        /// <summary>Veces que se llamó a <see cref="Pump"/>.</summary>
        public int Pumps { get; private set; }

        /// <summary>Cuántas llamadas empiezan por <paramref name="prefix"/>.</summary>
        public int Count(string prefix)
        {
            int n = 0;
            for (int i = 0; i < Calls.Count; i++)
            {
                if (Calls[i].StartsWith(prefix, StringComparison.Ordinal))
                {
                    n++;
                }
            }

            return n;
        }

        /// <inheritdoc />
        public void SetRequestConfiguration(bool underAgeOfConsent)
        {
            Calls.Add("config:tfua=" + (underAgeOfConsent ? "true" : "false"));
        }

        /// <inheritdoc />
        public void SetMediationConsent(bool? hasUserConsent, bool? doNotSell)
        {
            MediationConsent.Add(
                "unity:gdpr=" + Flag(hasUserConsent)
                + ":privacy=" + Flag(doNotSell.HasValue ? !doNotSell.Value : (bool?)null)
                + (Initializations == 0 ? ":before-init" : ":after-init"));
        }

        /// <inheritdoc />
        public void RequestConsent(bool underAgeOfConsent)
        {
            ConsentRequests++;
            Calls.Add("ump:tfua=" + (underAgeOfConsent ? "true" : "false"));
            if (AutoConsent)
            {
                ResolveConsent(CanRequestAds);
            }
        }

        /// <inheritdoc />
        public bool ShowPrivacyOptionsForm()
        {
            Calls.Add("privacy");
            return true;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            Initializations++;
            Calls.Add("init");
            if (AutoInitialize)
            {
                CompleteInitialization();
            }
        }

        /// <inheritdoc />
        public void Load(GmaAdKind kind, bool nonPersonalized, bool restrictedDataProcessing)
        {
            // rdp solo se apunta cuando va (T-L5), para que las llamadas de siempre se lean igual.
            Calls.Add("load:" + Name(kind) + ":npa=" + (nonPersonalized ? "true" : "false") + (restrictedDataProcessing ? ":rdp=true" : string.Empty));
            if (AutoFill)
            {
                _ready[(int)kind] = true;
                Listener?.OnAdEvent(kind, GmaAdEvent.Loaded);
            }
        }

        /// <inheritdoc />
        public bool IsReady(GmaAdKind kind) => _ready[(int)kind];

        /// <inheritdoc />
        public void Show(GmaAdKind kind, string? userId, string? customData)
        {
            _ready[(int)kind] = false;
            if (kind == GmaAdKind.Rewarded)
            {
                LastUserId = userId;
                LastCustomData = customData;
            }

            Calls.Add("show:" + Name(kind) + ":" + (userId != null ? "user" : "nouser"));
        }

        /// <inheritdoc />
        public void Pump()
        {
            Pumps++;
        }

        /// <summary>UMP termina.</summary>
        public void ResolveConsent(bool canRequestAds)
        {
            Listener?.OnConsentResolved(canRequestAds);
        }

        /// <summary>Google termina de iniciarse.</summary>
        public void CompleteInitialization()
        {
            Listener?.OnInitialized();
        }

        /// <summary>Deja (o quita) un anuncio cargado sin avisar.</summary>
        public void SetReady(GmaAdKind kind, bool ready)
        {
            _ready[(int)kind] = ready;
        }

        /// <summary>Dispara un aviso; <see cref="GmaAdEvent.Loaded"/> deja además el anuncio listo.</summary>
        public void Raise(GmaAdKind kind, GmaAdEvent ev)
        {
            if (ev == GmaAdEvent.Loaded)
            {
                _ready[(int)kind] = true;
            }
            else if (ev == GmaAdEvent.LoadFailed)
            {
                _ready[(int)kind] = false;
            }

            Listener?.OnAdEvent(kind, ev);
        }

        /// <summary>Dispara <c>OnAdPaid</c>.</summary>
        public void RaisePaid(GmaAdKind kind, string adSource, long valueMicros, string currency)
        {
            Listener?.OnPaid(kind, adSource, valueMicros, currency);
        }

        private static string Flag(bool? value) => value.HasValue ? (value.Value ? "true" : "false") : "-";

        private static string Name(GmaAdKind kind) => kind == GmaAdKind.Rewarded ? "rewarded" : "interstitial";
    }
}
