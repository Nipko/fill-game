#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using AF.Backend.Http;
using UnityEngine.Networking;

namespace AF.Backend.Unity
{
    /// <summary>
    /// <see cref="IHttpTransport"/> con <c>UnityWebRequest</c> (módulo de Unity ya incluido; sin paquete nuevo; plan §7, F3-2).
    /// <list type="bullet">
    /// <item>Sin redirecciones (<c>redirectLimit = 0</c>): la cabecera <c>Authorization</c> no sale nunca hacia otro host.</item>
    /// <item>Un fallo de red vuelve con código 0 y un motivo genérico, sin la URL.</item>
    /// <item>No escribe nada en los logs: ni URL, ni cabeceras (el ID token lleva el UID en <c>sub</c>), ni cuerpos.</item>
    /// </list>
    /// La cabecera <c>Date</c> de la respuesta llega intacta a <see cref="HttpBackendClient"/>, que la pasa a
    /// <c>ServerDateTracker</c> (<c>IServerDate</c>, 08 §10.1). Se usa desde el hilo principal: <c>completed</c> llega en él.
    /// </summary>
    public sealed class UnityWebRequestTransport : IHttpTransport
    {
        public void Send(HttpRequestData request, Action<HttpResponseData> onDone)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            UnityWebRequest uwr;
            try
            {
                uwr = Build(request);
            }
            catch (Exception)
            {
                // URL o cabecera que UnityWebRequest no acepta: se trata como fallo de red, sin copiar el mensaje (llevaría la URL).
                onDone(HttpResponseData.NetworkFailure("request_rejected"));
                return;
            }

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = uwr.SendWebRequest();
            }
            catch (Exception)
            {
                uwr.Dispose();
                onDone(HttpResponseData.NetworkFailure("send_failed"));
                return;
            }

            operation.completed += _ =>
            {
                HttpResponseData response;
                try
                {
                    response = ToResponse(uwr);
                }
                finally
                {
                    uwr.Dispose();
                }

                onDone(response);
            };
        }

        private static UnityWebRequest Build(HttpRequestData request)
        {
            var uwr = new UnityWebRequest(request.Url, request.Method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                redirectLimit = 0,
                timeout = request.TimeoutSeconds,
            };

            try
            {
                Configure(uwr, request);
            }
            catch
            {
                uwr.Dispose();
                throw;
            }

            return uwr;
        }

        private static void Configure(UnityWebRequest uwr, HttpRequestData request)
        {
            if (request.Body != null)
            {
                var upload = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request.Body));
                string? contentType = request.GetHeader("Content-Type");
                if (contentType != null)
                {
                    upload.contentType = contentType;
                }

                uwr.uploadHandler = upload;
            }

            for (int i = 0; i < request.Headers.Count; i++)
            {
                KeyValuePair<string, string> header = request.Headers[i];
                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase) && request.Body != null)
                {
                    continue;
                }

                uwr.SetRequestHeader(header.Key, header.Value);
            }
        }

        private static HttpResponseData ToResponse(UnityWebRequest uwr)
        {
            int status = (int)uwr.responseCode;
            if (uwr.result == UnityWebRequest.Result.ConnectionError || status <= 0)
            {
                return HttpResponseData.NetworkFailure(uwr.result == UnityWebRequest.Result.ConnectionError ? "connection_error" : "no_response");
            }

            string? body = uwr.downloadHandler != null ? uwr.downloadHandler.text : null;
            Dictionary<string, string>? headers = uwr.GetResponseHeaders();
            return new HttpResponseData(status, body, headers);
        }
    }
}
