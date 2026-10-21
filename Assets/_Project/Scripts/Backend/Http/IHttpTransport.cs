#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Backend.Http
{
    /// <summary>
    /// Petición HTTP del cliente del backend. <see cref="ToString"/> no enseña cabeceras ni cuerpo: llevan el ID token (cuyo
    /// <c>sub</c> es el UID), el token de integridad y los <c>purchaseToken</c> (plan §5; reglas de <c>sdk.md</c>).
    /// </summary>
    public sealed class HttpRequestData
    {
        public HttpRequestData(string method, string url, IReadOnlyList<KeyValuePair<string, string>> headers, string? body, int timeoutSeconds)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("Falta el método.", nameof(method));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Falta la URL.", nameof(url));
            }

            if (timeoutSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            }

            Method = method;
            Url = url;
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Body = body;
            TimeoutSeconds = timeoutSeconds;
        }

        /// <summary><c>POST</c> o <c>GET</c>.</summary>
        public string Method { get; }

        /// <summary>URL absoluta (base del entorno + ruta <c>/v1/…</c>), sin parámetros con identificadores.</summary>
        public string Url { get; }

        public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

        /// <summary>Cuerpo JSON en UTF-8, o null sin cuerpo.</summary>
        public string? Body { get; }

        /// <summary>Tiempo máximo de la petición completa.</summary>
        public int TimeoutSeconds { get; }

        /// <summary>Valor de la cabecera <paramref name="name"/> (sin distinguir mayúsculas), o null.</summary>
        public string? GetHeader(string name)
        {
            for (int i = 0; i < Headers.Count; i++)
            {
                if (string.Equals(Headers[i].Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return Headers[i].Value;
                }
            }

            return null;
        }

        /// <summary>Solo el método y la ruta.</summary>
        public override string ToString()
        {
            return Method + " " + PathOf(Url);
        }

        internal static string PathOf(string url)
        {
            int scheme = url.IndexOf("://", StringComparison.Ordinal);
            int slash = scheme < 0 ? -1 : url.IndexOf('/', scheme + 3);
            return slash < 0 ? "/" : url.Substring(slash);
        }
    }

    /// <summary>Respuesta HTTP, o el fallo de red si no hubo respuesta (<see cref="StatusCode"/> = 0).</summary>
    public sealed class HttpResponseData
    {
        private static readonly Dictionary<string, string> NoHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> _headers;

        public HttpResponseData(int statusCode, string? body, IEnumerable<KeyValuePair<string, string>>? headers = null, string? transportError = null)
        {
            if (statusCode < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(statusCode));
            }

            StatusCode = statusCode;
            Body = body;
            TransportError = transportError;
            _headers = NoHeaders;
            if (headers != null)
            {
                _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> h in headers)
                {
                    if (h.Key != null && h.Value != null)
                    {
                        _headers[h.Key] = h.Value;
                    }
                }
            }
        }

        /// <summary>Sin respuesta (sin red, DNS, TLS, tiempo agotado…). <paramref name="reason"/> no debe llevar la URL.</summary>
        public static HttpResponseData NetworkFailure(string reason)
        {
            return new HttpResponseData(0, null, null, string.IsNullOrEmpty(reason) ? "network" : reason);
        }

        /// <summary>Código HTTP; 0 = no hubo respuesta.</summary>
        public int StatusCode { get; }

        public string? Body { get; }

        /// <summary>Motivo del fallo de red (solo con <see cref="StatusCode"/> = 0).</summary>
        public string? TransportError { get; }

        public bool HasResponse => StatusCode > 0;

        /// <summary>Valor de la cabecera <paramref name="name"/> (sin distinguir mayúsculas), o null.</summary>
        public string? GetHeader(string name)
        {
            return _headers.TryGetValue(name, out string? value) ? value : null;
        }
    }

    /// <summary>
    /// Transporte HTTP del backend (plan §7). La implementación real es <c>UnityWebRequestTransport</c> en
    /// <c>AF.Backend.Unity</c>; el editor y los tests usan <see cref="FakeHttpTransport"/>. El transporte no escribe nada en
    /// los logs (ni URL con parámetros, ni cabeceras, ni cuerpos).
    /// </summary>
    public interface IHttpTransport
    {
        /// <summary>
        /// Envía <paramref name="request"/> y llama a <paramref name="onDone"/> exactamente una vez, en el hilo principal (la
        /// implementación falsa puede llamarlo antes de volver). Nunca lanza por un fallo de red: lo devuelve con
        /// <see cref="HttpResponseData.StatusCode"/> = 0.
        /// </summary>
        void Send(HttpRequestData request, Action<HttpResponseData> onDone);
    }
}
