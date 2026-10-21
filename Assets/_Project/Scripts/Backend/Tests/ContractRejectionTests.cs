#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using AF.Backend.Contract;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>
    /// F3-1: las respuestas del servidor fuera del contrato v1 se rechazan (los ejemplos inválidos del contrato son todos
    /// peticiones). El modo no estricto (<c>TryParse</c>) solo ignora claves desconocidas: no relaja tipos, rangos ni patrones.
    /// </summary>
    public sealed class ContractRejectionTests
    {
        private const string Hash = "3f0c6b0e2a5d4c1b9e8f7a6d5c4b3a291807f6e5d4c3b2a1908f7e6d5c4b3a29";
        private const string Nonce = "6f1c2a4e-8b3d-4f5a-9c7e-1d2b3a4c5e6f";

        private static string Q(string json) => json.Replace('\'', '"');

        private static void Rejects<T>(TryParseFn<T> parse, string json, string because)
            where T : class
        {
            Assert.That(parse(Q(json), out T? value, out string? error), Is.False, because + ": " + json);
            Assert.That(value, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        private static T Accepts<T>(TryParseFn<T> parse, string json)
            where T : class
        {
            Assert.That(parse(Q(json), out T? value, out string? error), Is.True, json + " → " + error);
            return value!;
        }

        private delegate bool TryParseFn<T>(string? json, out T? value, out string? error)
            where T : class;

        private static string Ok(string grant, string extra = "") =>
            "{'status':'ok','token_hash':'" + Hash + "','grant':" + grant + extra + "}";

        [Test]
        public void VerifyResponse_OneOfIsEnforced()
        {
            Rejects<VerifyPurchaseResponse>(VerifyPurchaseResponse.TryParseStrict, "{'status':'pending','token_hash':'" + Hash + "'}", "pending sin más campos");
            Rejects<VerifyPurchaseResponse>(VerifyPurchaseResponse.TryParseStrict, "{'status':'pending','grant':{'seeds':1,'items':[]}}", "pending sin grant");
            Rejects<VerifyPurchaseResponse>(VerifyPurchaseResponse.TryParseStrict, "{'status':'duplicate','token_hash':'" + Hash + "','grant':{'seeds':1,'items':[]}}", "duplicate sin grant");
            foreach (TryParseFn<VerifyPurchaseResponse> parse in new TryParseFn<VerifyPurchaseResponse>[] { VerifyPurchaseResponse.TryParse, VerifyPurchaseResponse.TryParseStrict })
            {
                Rejects(parse, "{'status':'duplicate'}", "duplicate sin token_hash");
                Rejects(parse, "{'status':'ok','token_hash':'" + Hash + "'}", "ok sin grant");
                Rejects(parse, "{'status':'granted'}", "status desconocido");
                Rejects(parse, "{'status':1}", "status no es cadena");
                Rejects(parse, "[]", "no es un objeto");
                Rejects(parse, "", "vacío");
                Rejects(parse, "{'status':'duplicate','token_hash':'" + Hash.ToUpperInvariant() + "'}", "hash en mayúsculas");
                Rejects(parse, "{'status':'duplicate','token_hash':'" + Hash.Substring(1) + "'}", "hash corto");
            }

            Assert.That(Accepts<VerifyPurchaseResponse>(VerifyPurchaseResponse.TryParse, "{'status':'pending','token_hash':'" + Hash + "'}").Status,
                Is.EqualTo(VerifyStatus.Pending), "el modo no estricto ignora claves de más");
        }

        [Test]
        public void VerifyResponse_GrantAndEntitlements()
        {
            foreach (TryParseFn<VerifyPurchaseResponse> parse in new TryParseFn<VerifyPurchaseResponse>[] { VerifyPurchaseResponse.TryParse, VerifyPurchaseResponse.TryParseStrict })
            {
                Rejects(parse, Ok("{'seeds':-1,'items':[]}"), "seeds negativas");
                Rejects(parse, Ok("{'seeds':1.5,'items':[]}"), "seeds no enteras");
                Rejects(parse, Ok("{'seeds':'5','items':[]}"), "seeds como cadena");
                Rejects(parse, Ok("{'seeds':99999999999999999999,'items':[]}"), "mayor que long");
                Rejects(parse, Ok("{'seeds':1}"), "sin items");
                Rejects(parse, Ok("{'seeds':1,'items':[{'id':'piggy','count':0}]}"), "count 0");
                Rejects(parse, Ok("{'seeds':1,'items':[{'id':'Piggy','count':1}]}"), "id con mayúsculas");
                Rejects(parse, Ok("{'seeds':1,'items':[{'count':1}]}"), "ítem sin id");
                Rejects(parse, Ok("{'seeds':1,'items':" + Items(33) + "}"), "más de 32 ítems");
                Rejects(parse, Ok("{'seeds':1,'items':[]}", ",'entitlements':{'skins':['a','a']}"), "skins repetidas");
                Rejects(parse, Ok("{'seeds':1,'items':[]}", ",'entitlements':{'skins':['A']}"), "skin con mayúsculas");
                Rejects(parse, Ok("{'seeds':1,'items':[]}", ",'entitlements':{'no_ads':'yes'}"), "no_ads no booleano");
                Rejects(parse, Ok("{'seeds':1,'items':[]}", ",'entitlements':{'pass_until':-5}"), "pass_until negativo");
            }

            Assert.That(Accepts<VerifyPurchaseResponse>(VerifyPurchaseResponse.TryParseStrict, Ok("{'seeds':1.0,'items':" + Items(32) + "}")).Grant!.Seeds,
                Is.EqualTo(1), "JSON Schema admite 1.0 como entero; 32 ítems es el máximo");
            Rejects<VerifyPurchaseResponse>(VerifyPurchaseResponse.TryParseStrict, Ok("{'seeds':1,'items':[],'bonus':1}"), "clave extra en grant");
            Rejects<VerifyPurchaseResponse>(VerifyPurchaseResponse.TryParseStrict, Ok("{'seeds':1,'items':[]}", ",'entitlements':{'vip':true}"), "clave extra en entitlements");
        }

        [Test]
        public void RestoreResponse()
        {
            foreach (TryParseFn<RestoreLedgerResponse> parse in new TryParseFn<RestoreLedgerResponse>[] { RestoreLedgerResponse.TryParse, RestoreLedgerResponse.TryParseStrict })
            {
                Rejects(parse, "{'purchases':[]}", "sin confirmed_nonces");
                Rejects(parse, "{'confirmed_nonces':[]}", "sin purchases");
                Rejects(parse, "{'purchases':[],'confirmed_nonces':['" + Nonce + "','" + Nonce + "']}", "nonces repetidos");
                Rejects(parse, "{'purchases':[],'confirmed_nonces':['" + Nonce.ToUpperInvariant() + "']}", "nonce en mayúsculas");
                Rejects(parse, "{'purchases':[],'confirmed_nonces':" + Nonces(201) + "}", "más de 200 nonces");
                Rejects(parse, "{'purchases':[" + Purchase("'state':'refunded'") + "],'confirmed_nonces':[]}", "state desconocido");
                Rejects(parse, "{'purchases':[" + Purchase("'state':'ok'", sku: "noads") + "],'confirmed_nonces':[]}", "sku sin af_");
                Rejects(parse, "{'purchases':[" + Purchase("'state':'ok'", ts: "-1") + "],'confirmed_nonces':[]}", "ts negativo");
                Rejects(parse, "{'purchases':[" + Purchase("'state':'voided','voided_at':1.5") + "],'confirmed_nonces':[]}", "voided_at no entero");
                Rejects(parse, "{'purchases':" + PurchaseList(1001) + ",'confirmed_nonces':[]}", "más de 1.000 compras");
            }

            Accepts<RestoreLedgerResponse>(RestoreLedgerResponse.TryParseStrict, "{'purchases':" + PurchaseList(1000) + ",'confirmed_nonces':" + Nonces(200) + "}");
            Rejects<RestoreLedgerResponse>(RestoreLedgerResponse.TryParseStrict, "{'purchases':[" + Purchase("'state':'ok','uid':'x'") + "],'confirmed_nonces':[]}", "clave extra");
            Assert.That(Accepts<RestoreLedgerResponse>(RestoreLedgerResponse.TryParse,
                "{'purchases':[" + Purchase("'state':'ok','new':1") + "],'confirmed_nonces':[],'cursor':'x'}").Purchases, Has.Count.EqualTo(1));
        }

        [Test]
        public void AccountAndErrorResponses()
        {
            foreach (TryParseFn<AccountCodeResponse> parse in new TryParseFn<AccountCodeResponse>[] { AccountCodeResponse.TryParse, AccountCodeResponse.TryParseStrict })
            {
                Rejects(parse, "{'code':'8HZ7M2Q9XK4T8VB1NC6DRF0WPE'}", "primer símbolo mayor que 7");
                Rejects(parse, "{'code':'3HZ7M2Q9XK4T8VB1NC6DRF0WPI'}", "I no es de Crockford");
                Rejects(parse, "{'code':'3HZ7M2Q9XK4T8VB1NC6DRF0WPL'}", "L no es de Crockford");
                Rejects(parse, "{'code':'3HZ7M2Q9XK4T8VB1NC6DRF0WPO'}", "O no es de Crockford");
                Rejects(parse, "{'code':'3HZ7M2Q9XK4T8VB1NC6DRF0WPU'}", "U no es de Crockford");
                Rejects(parse, "{'code':'3hz7m2q9xk4t8vb1nc6drf0wpe'}", "minúsculas");
                Rejects(parse, "{'code':'3HZ7M2Q9XK4T8VB1NC6DRF0WP'}", "25 símbolos");
            }

            Rejects<DeleteAccountResponse>(DeleteAccountResponse.TryParse, "{'status':'ok'}", "status distinto de deleted");
            Rejects<DeleteRequestResponse>(DeleteRequestResponse.TryParse, "{'request_token':'q7Yt0dVx3kM9rB2nL5sW8eJ1uC4hF6gA0zXyPiQoRkE','expires_in_s':301}", "más de 300 s");
            foreach (TryParseFn<ApiError> parse in new TryParseFn<ApiError>[] { ApiError.TryParse, ApiError.TryParseStrict })
            {
                Rejects(parse, "{'error':{'code':'teapot','message':'x'}}", "código desconocido");
                Rejects(parse, "{'error':{'code':'rate_limited'}}", "sin message");
                Rejects(parse, "{'error':{'code':'rate_limited','message':'x','retry_after_s':0}}", "retry_after_s 0");
                Rejects(parse, "{'error':{'code':'rate_limited','message':'x','retry_after_s':86401}}", "retry_after_s > 1 día");
                Rejects(parse, "{'error':{'code':'rate_limited','message':'" + new string('x', 301) + "'}}", "message de más de 300");
                Rejects(parse, "{'code':'rate_limited','message':'x'}", "sin el objeto error");
            }
        }

        private static string Items(int n)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < n; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append("{'id':'i").Append(i).Append("','count':1}");
            }

            return sb.Append(']').ToString();
        }

        private static string Nonces(int n)
        {
            var list = new List<string>();
            for (int i = 0; i < n; i++)
            {
                list.Add("'" + i.ToString("x8", System.Globalization.CultureInfo.InvariantCulture) + "-8b3d-4f5a-9c7e-1d2b3a4c5e6f'");
            }

            return "[" + string.Join(",", list) + "]";
        }

        private static string Purchase(string stateAndMore, string sku = "af_noads", string ts = "1792500000000") =>
            "{'token_hash':'" + Hash + "','sku':'" + sku + "'," + stateAndMore + ",'ts':" + ts + "}";

        private static string PurchaseList(int n)
        {
            var list = new List<string>(n);
            for (int i = 0; i < n; i++)
            {
                list.Add(Purchase("'state':'ok'"));
            }

            return "[" + string.Join(",", list) + "]";
        }
    }
}
