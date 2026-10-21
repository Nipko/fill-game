#nullable enable
using System;
using System.Globalization;
using AF.Backend.Contract;

namespace AF.Backend
{
    /// <summary>Por qué falló una llamada al backend.</summary>
    public enum BackendErrorKind
    {
        /// <summary>Sin respuesta: sin red, DNS, TLS o tiempo agotado.</summary>
        Network = 0,

        /// <summary>El servidor respondió con un código distinto de 2xx (ver <see cref="BackendError.HttpStatus"/> y <see cref="BackendError.Code"/>).</summary>
        Http = 1,

        /// <summary>Respondió 2xx con un cuerpo que no cumple el contrato v1.</summary>
        InvalidResponse = 2,

        /// <summary>No hay ID token de Firebase Auth (SDK sin iniciar, sin red…).</summary>
        AuthUnavailable = 3,

        /// <summary>No hay token de integridad (App Check).</summary>
        IntegrityUnavailable = 4,

        /// <summary>El backend no está configurado en este build (sin URL base en release).</summary>
        NotConfigured = 5,
    }

    /// <summary>
    /// Error de una llamada al backend. No lleva tokens, UID ni URL: se puede registrar tal cual (<see cref="ToString"/>).
    /// </summary>
    public sealed class BackendError
    {
        public BackendError(BackendErrorKind kind, int httpStatus = 0, ApiErrorCode code = ApiErrorCode.Unknown, string? detail = null, TimeSpan? retryAfter = null)
        {
            Kind = kind;
            HttpStatus = httpStatus;
            Code = code;
            Detail = detail;
            RetryAfter = retryAfter;
        }

        public BackendErrorKind Kind { get; }

        /// <summary>Código HTTP (0 si no hubo respuesta).</summary>
        public int HttpStatus { get; }

        /// <summary>Código del contrato (<c>error.code</c>), o <see cref="ApiErrorCode.Unknown"/>.</summary>
        public ApiErrorCode Code { get; }

        /// <summary>Detalle para diagnóstico (mensaje del servidor o motivo local); no se enseña al jugador.</summary>
        public string? Detail { get; }

        /// <summary>Espera que pide el servidor (<c>retry_after_s</c> o la cabecera <c>Retry-After</c>).</summary>
        public TimeSpan? RetryAfter { get; }

        /// <summary>
        /// Fallo pasajero que merece reintentar ya, dentro de la misma llamada: sin red, 408, 429, 500, 502, 503 y 504
        /// (<see cref="RetryPolicy"/>).
        /// </summary>
        public bool IsTransient
        {
            get
            {
                if (Kind == BackendErrorKind.Network)
                {
                    return true;
                }

                if (Kind != BackendErrorKind.Http)
                {
                    return false;
                }

                return HttpStatus == 408 || HttpStatus == 429 || HttpStatus == 500 || HttpStatus == 502 || HttpStatus == 503 || HttpStatus == 504;
            }
        }

        public static BackendError Network(string? reason) => new BackendError(BackendErrorKind.Network, detail: reason);

        public static BackendError NotConfigured() => new BackendError(BackendErrorKind.NotConfigured, detail: "backend sin configurar");

        public static BackendError Http(int status, ApiErrorCode code, string? detail = null, TimeSpan? retryAfter = null) =>
            new BackendError(BackendErrorKind.Http, status, code, detail, retryAfter);

        public override string ToString()
        {
            string text = "BackendError(" + Kind;
            if (HttpStatus != 0)
            {
                text += " " + HttpStatus.ToString(CultureInfo.InvariantCulture);
            }

            if (Code != ApiErrorCode.Unknown)
            {
                text += " " + ApiError.CodeToWire(Code);
            }

            return text + ")";
        }
    }

    /// <summary>Resultado de una llamada: el valor o el error, nunca los dos.</summary>
    public sealed class BackendResult<T>
        where T : class
    {
        private BackendResult(T? value, BackendError? error)
        {
            Value = value;
            Error = error;
        }

        public T? Value { get; }

        public BackendError? Error { get; }

        public bool IsOk => Value != null;

        public static BackendResult<T> Ok(T value) => new BackendResult<T>(value ?? throw new ArgumentNullException(nameof(value)), null);

        public static BackendResult<T> Fail(BackendError error) => new BackendResult<T>(null, error ?? throw new ArgumentNullException(nameof(error)));

        public override string ToString() => IsOk ? "BackendResult(ok)" : "BackendResult(" + Error + ")";
    }
}
