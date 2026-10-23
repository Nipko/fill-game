#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Ads
{
    /// <summary>
    /// MAX falso para el editor y los tests (D-042): apunta cada llamada en <see cref="Calls"/>, en orden, y deja disparar los
    /// callbacks a mano (<see cref="CompleteInitialization"/>, <see cref="Raise"/>, <see cref="RaiseRevenue"/>). Sin red ni SDK.
    /// </summary>
    public sealed class FakeMaxSdkPort : IMaxSdkPort
    {
        private readonly bool[] _ready = new bool[2];

        /// <summary>Cada llamada, en orden: <c>consent:false</c>, <c>dns:true</c>, <c>user</c>, <c>init</c>, <c>load:rewarded</c>,
        /// <c>show:rewarded:rv_hint:…</c>, <c>cmp</c>. El UID no se apunta (nunca a los logs).</summary>
        public List<string> Calls { get; } = new List<string>();

        /// <inheritdoc />
        public IMaxSdkListener? Listener { get; set; }

        /// <summary>Último <c>SetHasUserConsent</c> (null si nunca).</summary>
        public bool? HasUserConsent { get; private set; }

        /// <summary>Último <c>SetDoNotSell</c> (null si nunca).</summary>
        public bool? DoNotSell { get; private set; }

        /// <summary>Último <c>SetUserId</c> (null si nunca).</summary>
        public string? UserId { get; private set; }

        /// <summary>Veces que se llamó a <c>InitializeSdk</c>.</summary>
        public int Initializations { get; private set; }

        /// <summary>El último <c>custom_data</c> de un recompensado.</summary>
        public string? LastCustomData { get; private set; }

        /// <summary>La última ubicación mostrada.</summary>
        public string? LastPlacement { get; private set; }

        /// <summary>Cargas automáticas: cada <c>Load</c> deja el anuncio listo y avisa en el acto.</summary>
        public bool AutoFill { get; set; }

        /// <summary>Hay CMP (UMP) que mostrar.</summary>
        public bool HasCmp { get; set; } = true;

        /// <summary>Cargas pedidas por formato.</summary>
        public int Loads(MaxAdKind kind)
        {
            int n = 0;
            string key = "load:" + Name(kind);
            for (int i = 0; i < Calls.Count; i++)
            {
                if (Calls[i] == key)
                {
                    n++;
                }
            }

            return n;
        }

        /// <inheritdoc />
        public void SetHasUserConsent(bool value)
        {
            HasUserConsent = value;
            Calls.Add("consent:" + (value ? "true" : "false"));
        }

        /// <inheritdoc />
        public void SetDoNotSell(bool value)
        {
            DoNotSell = value;
            Calls.Add("dns:" + (value ? "true" : "false"));
        }

        /// <inheritdoc />
        public void SetUserId(string userId)
        {
            UserId = userId;
            Calls.Add("user");
        }

        /// <inheritdoc />
        public void Initialize()
        {
            Initializations++;
            Calls.Add("init");
        }

        /// <inheritdoc />
        public void Load(MaxAdKind kind)
        {
            Calls.Add("load:" + Name(kind));
            if (AutoFill)
            {
                _ready[(int)kind] = true;
                Listener?.OnAdEvent(kind, MaxAdEvent.Loaded);
            }
        }

        /// <inheritdoc />
        public bool IsReady(MaxAdKind kind) => _ready[(int)kind];

        /// <inheritdoc />
        public void Show(MaxAdKind kind, string placement, string? customData)
        {
            _ready[(int)kind] = false;
            LastPlacement = placement;
            if (kind == MaxAdKind.Rewarded)
            {
                LastCustomData = customData;
            }

            Calls.Add("show:" + Name(kind) + ":" + placement);
        }

        /// <inheritdoc />
        public bool ShowCmpForExistingUser()
        {
            Calls.Add("cmp");
            return HasCmp;
        }

        /// <summary>MAX termina de iniciarse.</summary>
        public void CompleteInitialization()
        {
            Listener?.OnInitialized();
        }

        /// <summary>Deja (o quita) un anuncio cargado sin avisar.</summary>
        public void SetReady(MaxAdKind kind, bool ready)
        {
            _ready[(int)kind] = ready;
        }

        /// <summary>Dispara un callback; <see cref="MaxAdEvent.Loaded"/> deja además el anuncio listo.</summary>
        public void Raise(MaxAdKind kind, MaxAdEvent ev)
        {
            if (ev == MaxAdEvent.Loaded)
            {
                _ready[(int)kind] = true;
            }
            else if (ev == MaxAdEvent.LoadFailed)
            {
                _ready[(int)kind] = false;
            }

            Listener?.OnAdEvent(kind, ev);
        }

        /// <summary>Dispara <c>OnAdRevenuePaidEvent</c>.</summary>
        public void RaiseRevenue(MaxAdKind kind, string network, double revenue)
        {
            Listener?.OnRevenuePaid(kind, network, revenue);
        }

        private static string Name(MaxAdKind kind) => kind == MaxAdKind.Rewarded ? "rewarded" : "interstitial";
    }
}
