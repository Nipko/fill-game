#nullable enable
using System;

namespace AF.Backend
{
    /// <summary>Qué cliente usa el juego.</summary>
    public enum BackendMode
    {
        /// <summary><see cref="FakeBackendClient"/>: editor y builds de desarrollo sin URL base.</summary>
        Fake = 0,

        /// <summary><see cref="HttpBackendClient"/> contra la URL base del entorno.</summary>
        Http = 1,

        /// <summary><see cref="UnavailableBackendClient"/>: release sin URL base o con una URL inválida. Nunca concede nada.</summary>
        Unavailable = 2,
    }

    /// <summary>
    /// URL base del backend. Va compilada por entorno y nunca en Remote Config (plan §7, B7): así un valor remoto no puede
    /// desviar las llamadas, que llevan el ID token, a otro servidor. Se exige <c>https://host[/etapa]</c>, sin usuario, puerto
    /// raro, consulta ni fragmento.
    /// </summary>
    public static class BackendEndpoint
    {
        /// <summary>
        /// Elige el cliente: con una URL válida, <see cref="BackendMode.Http"/>; sin URL, <see cref="BackendMode.Fake"/> en
        /// desarrollo (editor o <c>DEVELOPMENT_BUILD</c>) y <see cref="BackendMode.Unavailable"/> en release; con una URL inválida,
        /// <see cref="BackendMode.Unavailable"/> siempre.
        /// </summary>
        public static BackendMode Choose(string? baseUrl, bool development)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return development ? BackendMode.Fake : BackendMode.Unavailable;
            }

            return Normalize(baseUrl) != null ? BackendMode.Http : BackendMode.Unavailable;
        }

        /// <summary>URL normalizada sin barra final, o null si no es <c>https://host[/etapa]</c>.</summary>
        public static string? Normalize(string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            string trimmed = baseUrl!.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || uri.UserInfo.Length != 0
                || uri.Query.Length != 0
                || uri.Fragment.Length != 0
                || !uri.IsDefaultPort
                || uri.Host.Length == 0
                || trimmed.IndexOf('?') >= 0
                || trimmed.IndexOf('#') >= 0)
            {
                return null;
            }

            // Lo que sale del Uri ya validado (esquema, host y ruta), no el texto de entrada.
            return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        }
    }
}
