#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using AF.Backend.Http;
using AF.Backend.Unity;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Backend.Tests
{
    /// <summary>
    /// F3-2: <see cref="UnityWebRequestTransport"/> sin red real. Un puerto local cerrado devuelve el fallo de red como código 0,
    /// sin lanzar ni copiar la URL.
    /// </summary>
    public sealed class UnityWebRequestTransportTests
    {
        [UnityTest]
        public IEnumerator ClosedLocalPortIsANetworkFailure()
        {
            var transport = new UnityWebRequestTransport();
            HttpResponseData? response = null;
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Authorization", "Bearer secret-id-token"),
                new KeyValuePair<string, string>("Content-Type", "application/json; charset=utf-8"),
            };
            transport.Send(new HttpRequestData("POST", "https://127.0.0.1:9/v1/ledger/restore", headers, "{\"pending_nonces\":[]}", 5), r => response = r);

            var watch = Stopwatch.StartNew();
            while (response == null && watch.Elapsed < TimeSpan.FromSeconds(20))
            {
                yield return null;
            }

            Assert.That(response, Is.Not.Null, "el transporte debe responder (tiempo agotado de 5 s)");
            Assert.That(response!.StatusCode, Is.EqualTo(0));
            Assert.That(response.HasResponse, Is.False);
            Assert.That(response.TransportError, Is.Not.Null.And.Not.Empty);
            Assert.That(response.TransportError, Does.Not.Contain("127.0.0.1"));
            Assert.That(response.TransportError, Does.Not.Contain("secret"));
        }
    }
}
