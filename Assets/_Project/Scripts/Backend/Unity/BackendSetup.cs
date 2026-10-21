#nullable enable
using System;
using AF.Backend.Auth;
using AF.Backend.Firebase;
using AF.Platform;

namespace AF.Backend.Unity
{
    /// <summary>
    /// URL base del backend compilada por entorno (plan §7; B7: nunca en Remote Config). La de dev es la del API de dev desplegado
    /// (<c>execute-api</c>, que no es secreta; D-035, añadido de F3-1 y F3-2, y <c>docs/dev/traspaso/codex-estado.md</c>, «Dev
    /// desplegado y comprobado»); la de prod llega con el dominio tras el dictamen de marca (F5-1). El <b>editor</b> no usa ninguna:
    /// sin Firebase no hay tokens (08 §2.1, principio 6), así que siempre va con <see cref="FakeBackendClient"/>. Un build de
    /// desarrollo sin la configuración de Firebase (sin <c>google-services.json</c>) también sigue con el falso
    /// (<see cref="BackendFactory"/>), y un release sin URL o sin Firebase usa <see cref="UnavailableBackendClient"/>, que nunca concede nada.
    /// </summary>
    public static class BackendEndpoints
    {
        /// <summary>Entorno dev (<c>DEVELOPMENT_BUILD</c> en el teléfono): el API de dev en AWS (<c>sa-east-1</c>).</summary>
        public const string Dev = "https://4tbucfarv9.execute-api.sa-east-1.amazonaws.com";

        /// <summary>Entorno prod (release). Vacía hasta F5-1.</summary>
        public const string Prod = "";

        /// <summary>Build de desarrollo: el editor o un build con <c>DEVELOPMENT_BUILD</c> (<c>BuildDev</c>).</summary>
        public static bool IsDevelopment
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Es el editor (ARM64 o x64): sin Firebase, nunca llama al API.</summary>
        public static bool IsEditor
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>URL base del entorno de este build (vacía en el editor).</summary>
        public static string Current => IsEditor ? "" : IsDevelopment ? Dev : Prod;

        /// <summary>Cliente que toca en este build (<see cref="BackendEndpoint.Choose"/>).</summary>
        public static BackendMode Mode => BackendEndpoint.Choose(Current, IsDevelopment);
    }

    /// <summary>
    /// Crea el <see cref="IBackendClient"/> del build. Con <see cref="BackendMode.Http"/> usa los proveedores de tokens que reciba o,
    /// si no recibe ninguno, los reales de Firebase Auth y App Check (F3-6; <see cref="FirebaseTokenProviders"/>), que solo existen
    /// en el teléfono con la configuración de Firebase. Sin proveedores, un build de desarrollo sigue con <see cref="FakeBackendClient"/>
    /// y un release no concede nada (<see cref="UnavailableBackendClient"/>).
    /// </summary>
    public static class BackendFactory
    {
        /// <summary>
        /// Cliente para <see cref="BackendEndpoints.Mode"/>; llámalo desde el hilo principal (los reintentos vuelven a él).
        /// </summary>
        /// <param name="clock">Reloj del juego (para el <c>ts</c> del cliente falso).</param>
        /// <param name="serverDate">Recibe la cabecera <c>Date</c> de cada respuesta (puede ser null).</param>
        /// <param name="auth">Proveedor del ID token; null = el real de Firebase, si lo hay.</param>
        /// <param name="integrity">Proveedor del token de App Check; null = el real de Firebase, si lo hay.</param>
        public static IBackendClient Create(IClock clock, ServerDateTracker? serverDate, IAuthTokenProvider? auth, IIntegrityTokenProvider? integrity)
        {
            if (auth == null && integrity == null && FirebaseTokenProviders.TryGet(out IAuthTokenProvider? realAuth, out IIntegrityTokenProvider? realIntegrity))
            {
                auth = realAuth;
                integrity = realIntegrity;
            }

            return Create(BackendEndpoints.Mode, BackendEndpoints.Current, BackendEndpoints.IsDevelopment, clock, serverDate, auth, integrity);
        }

        internal static IBackendClient Create(
            BackendMode mode, string baseUrl, bool development, IClock clock, ServerDateTracker? serverDate, IAuthTokenProvider? auth,
            IIntegrityTokenProvider? integrity)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            switch (mode)
            {
                case BackendMode.Fake:
                    return new FakeBackendClient(clock);
                case BackendMode.Http:
                    if (auth == null || integrity == null)
                    {
                        // Sin Firebase en el build (sin google-services.json) no se puede llamar a la API: en desarrollo, el falso de
                        // siempre; en release, nada (no se podría acreditar ninguna compra).
                        return development ? new FakeBackendClient(clock) : (IBackendClient)new UnavailableBackendClient();
                    }

                    return new HttpBackendClient(baseUrl, new UnityWebRequestTransport(), auth, integrity, new SynchronizationContextScheduler(), serverDate);
                default:
                    return new UnavailableBackendClient();
            }
        }
    }
}
