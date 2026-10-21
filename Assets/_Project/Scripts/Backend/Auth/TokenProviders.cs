#nullable enable
using System;

namespace AF.Backend.Auth
{
    /// <summary>Resultado de pedir un token: el token, o el motivo por el que no lo hay (sin datos personales).</summary>
    public sealed class TokenResult
    {
        private TokenResult(string? token, string? failure)
        {
            Token = token;
            Failure = failure;
        }

        /// <summary>El token (nunca va a los logs), o null si falló.</summary>
        public string? Token { get; }

        /// <summary>Motivo del fallo, o null si hay token.</summary>
        public string? Failure { get; }

        public bool IsOk => Token != null;

        public static TokenResult Ok(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token vacío.", nameof(token));
            }

            return new TokenResult(token, null);
        }

        public static TokenResult Fail(string reason)
        {
            return new TokenResult(null, string.IsNullOrEmpty(reason) ? "unavailable" : reason);
        }

        /// <summary>Nunca enseña el token.</summary>
        public override string ToString()
        {
            return IsOk ? "TokenResult(ok)" : "TokenResult(" + Failure + ")";
        }
    }

    /// <summary>
    /// ID token de Firebase Auth anónimo para la cabecera <c>Authorization: Bearer</c> (contrato v1, <c>authHeader</c>; plan §7:
    /// <c>FirebaseUser.TokenAsync</c>). El real llega con F3-6 (SDK de Firebase aprobado); el editor usa
    /// <see cref="FakeAuthTokenProvider"/>.
    /// </summary>
    public interface IAuthTokenProvider
    {
        /// <summary>
        /// Pide el ID token. Con <paramref name="forceRefresh"/> pide uno nuevo aunque el guardado siga vigente (tras un 401).
        /// Llama a <paramref name="onDone"/> exactamente una vez, en el hilo principal.
        /// </summary>
        void GetIdToken(bool forceRefresh, Action<TokenResult> onDone);
    }

    /// <summary>
    /// Token de App Check de uso limitado para la cabecera <c>X-Firebase-AppCheck</c> (Q-AWS-4; contrato v1,
    /// <c>integrityHeader</c>), solo en las rutas con <c>integrity: true</c>. Cada token sirve para una sola petición. El real
    /// llega con F3-6; el editor usa <see cref="FakeIntegrityTokenProvider"/>.
    /// </summary>
    public interface IIntegrityTokenProvider
    {
        /// <summary>Pide un token nuevo. Llama a <paramref name="onDone"/> exactamente una vez, en el hilo principal.</summary>
        void GetLimitedUseToken(Action<TokenResult> onDone);
    }

    /// <summary><see cref="IAuthTokenProvider"/> para el editor y los tests: token fijo, con versión al refrescar, o fallo.</summary>
    public sealed class FakeAuthTokenProvider : IAuthTokenProvider
    {
        private readonly string _prefix;
        private int _version = 1;

        public FakeAuthTokenProvider(string prefix = "fake-id-token")
        {
            _prefix = string.IsNullOrEmpty(prefix) ? "fake-id-token" : prefix;
        }

        /// <summary>Si no es null, todas las peticiones fallan con este motivo.</summary>
        public string? FailWith { get; set; }

        /// <summary>Veces que se pidió el token.</summary>
        public int Calls { get; private set; }

        /// <summary>Veces que se pidió con <c>forceRefresh</c>.</summary>
        public int Refreshes { get; private set; }

        /// <summary>Token que se entrega ahora (cambia tras cada refresco).</summary>
        public string CurrentToken => _prefix + "-" + _version.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public void GetIdToken(bool forceRefresh, Action<TokenResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            Calls++;
            if (FailWith != null)
            {
                onDone(TokenResult.Fail(FailWith));
                return;
            }

            if (forceRefresh)
            {
                Refreshes++;
                _version++;
            }

            onDone(TokenResult.Ok(CurrentToken));
        }
    }

    /// <summary><see cref="IIntegrityTokenProvider"/> para el editor y los tests: un token distinto en cada llamada, o fallo.</summary>
    public sealed class FakeIntegrityTokenProvider : IIntegrityTokenProvider
    {
        /// <summary>Si no es null, todas las peticiones fallan con este motivo.</summary>
        public string? FailWith { get; set; }

        /// <summary>Tokens entregados.</summary>
        public int Calls { get; private set; }

        public void GetLimitedUseToken(Action<TokenResult> onDone)
        {
            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            if (FailWith != null)
            {
                onDone(TokenResult.Fail(FailWith));
                return;
            }

            Calls++;
            onDone(TokenResult.Ok("fake-appcheck-" + Calls.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }
}
