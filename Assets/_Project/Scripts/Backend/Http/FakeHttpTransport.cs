#nullable enable
using System;
using System.Collections.Generic;

namespace AF.Backend.Http
{
    /// <summary>
    /// <see cref="IHttpTransport"/> para los tests (08 §2.1, principio 6): responde con <see cref="Handler"/> o con las respuestas
    /// encoladas en <see cref="Enqueue"/>, y guarda cada petición en <see cref="Requests"/>. Con <see cref="Deferred"/> retiene la
    /// respuesta hasta <see cref="CompleteNext"/>, para probar lo que pasa mientras una petición está en vuelo.
    /// </summary>
    public sealed class FakeHttpTransport : IHttpTransport
    {
        private readonly Queue<HttpResponseData> _queued = new Queue<HttpResponseData>();
        private readonly Queue<KeyValuePair<HttpResponseData, Action<HttpResponseData>>> _inFlight =
            new Queue<KeyValuePair<HttpResponseData, Action<HttpResponseData>>>();

        /// <summary>Peticiones recibidas, en orden.</summary>
        public List<HttpRequestData> Requests { get; } = new List<HttpRequestData>();

        /// <summary>Responde a cada petición; si es null se usan las encoladas.</summary>
        public Func<HttpRequestData, HttpResponseData>? Handler { get; set; }

        /// <summary>Retiene las respuestas hasta <see cref="CompleteNext"/>.</summary>
        public bool Deferred { get; set; }

        /// <summary>Peticiones retenidas.</summary>
        public int PendingCount => _inFlight.Count;

        /// <summary>Encola una respuesta (se usan en orden).</summary>
        public FakeHttpTransport Enqueue(HttpResponseData response)
        {
            _queued.Enqueue(response ?? throw new ArgumentNullException(nameof(response)));
            return this;
        }

        /// <summary>Encola una respuesta con código, cuerpo y cabecera <c>Date</c> opcional.</summary>
        public FakeHttpTransport Enqueue(int status, string? body, string? date = null)
        {
            var headers = new List<KeyValuePair<string, string>>();
            if (date != null)
            {
                headers.Add(new KeyValuePair<string, string>("Date", date));
            }

            return Enqueue(new HttpResponseData(status, body, headers));
        }

        /// <inheritdoc />
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

            Requests.Add(request);
            HttpResponseData response;
            if (Handler != null)
            {
                response = Handler(request);
            }
            else if (_queued.Count > 0)
            {
                response = _queued.Dequeue();
            }
            else
            {
                response = HttpResponseData.NetworkFailure("fake: sin respuesta encolada");
            }

            if (Deferred)
            {
                _inFlight.Enqueue(new KeyValuePair<HttpResponseData, Action<HttpResponseData>>(response, onDone));
                return;
            }

            onDone(response);
        }

        /// <summary>Entrega la respuesta retenida más antigua. False si no hay ninguna.</summary>
        public bool CompleteNext()
        {
            if (_inFlight.Count == 0)
            {
                return false;
            }

            KeyValuePair<HttpResponseData, Action<HttpResponseData>> next = _inFlight.Dequeue();
            next.Value(next.Key);
            return true;
        }
    }
}
