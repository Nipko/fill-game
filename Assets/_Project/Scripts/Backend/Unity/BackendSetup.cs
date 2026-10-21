#nullable enable
using System;
using AF.Backend.Auth;
using AF.Platform;

namespace AF.Backend.Unity
{
    /// <summary>
    /// URL base del backend compilada por entorno (plan §7; B7: nunca en Remote Config). Las dos están <b>vacías</b> en el
    /// repositorio: la de dev (<c>execute-api</c>) la pone el dueño al desplegar (F4-1/F4-2) y la de prod llega con el dominio
    /// tras el dictamen de marca (F5-1). Mientras estén vacías, el editor y los builds de desarrollo usan
    /// <see cref="FakeBackendClient"/>, y un release usa <see cref="UnavailableBackendClient"/>, que nunca concede nada.
    /// </summary>
    public static class BackendEndpoints
    {
        /// <summary>Entorno dev (editor y <c>DEVELOPMENT_BUILD</c>). Vacía: sin URL real en el repositorio.</summary>
        public const string Dev = "";

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

        /// <summary>URL base del entorno de este build.</summary>
        public static string Current => IsDevelopment ? Dev : Prod;

        /// <summary>Cliente que toca en este build (<see cref="BackendEndpoint.Choose"/>).</summary>
        public static BackendMode Mode => BackendEndpoint.Choose(Current, IsDevelopment);
    }

    /// <summary>
    /// Crea el <see cref="IBackendClient"/> del build. Todavía no lo usa nadie: lo conectan a las compras y a Ajustes F3-3 a
    /// F3-5, y los proveedores reales de tokens (Firebase Auth y App Check) llegan con F3-6.
    /// </summary>
    public static class BackendFactory
    {
        /// <summary>
        /// Cliente para <see cref="BackendEndpoints.Mode"/>. Con <see cref="BackendMode.Http"/> hacen falta los proveedores de
        /// tokens reales; llámalo desde el hilo principal (los reintentos vuelven a él).
        /// </summary>
        /// <param name="clock">Reloj del juego (para el <c>ts</c> del cliente falso).</param>
        /// <param name="serverDate">Recibe la cabecera <c>Date</c> de cada respuesta (puede ser null).</param>
        public static IBackendClient Create(IClock clock, ServerDateTracker? serverDate, IAuthTokenProvider? auth, IIntegrityTokenProvider? integrity)
        {
            return Create(BackendEndpoints.Mode, BackendEndpoints.Current, clock, serverDate, auth, integrity);
        }

        internal static IBackendClient Create(
            BackendMode mode, string baseUrl, IClock clock, ServerDateTracker? serverDate, IAuthTokenProvider? auth, IIntegrityTokenProvider? integrity)
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
                        // Sin proveedores reales (hasta F3-6) no se puede llamar a la API: mejor no conceder nada.
                        return new UnavailableBackendClient();
                    }

                    return new HttpBackendClient(baseUrl, new UnityWebRequestTransport(), auth, integrity, new SynchronizationContextScheduler(), serverDate);
                default:
                    return new UnavailableBackendClient();
            }
        }
    }
}
