#nullable enable
using System;
using System.Globalization;
using AF.IAP;

namespace AF.Analytics.Adapters
{
    /// <summary>
    /// Analítica de compras (08 §8.2: <c>af_iap_result</c> al terminar cada compra y <c>purchase</c> con la compra acreditada)
    /// como envoltorio de un <see cref="IStoreService"/>, sin tocar <c>AF.IAP</c>. <c>af_iap_offer_shown</c> lo emite la vista
    /// al enseñar la oferta (<see cref="OfferShown"/>). No hay <c>transaction_id</c>, que no se inventa [P] (el <c>purchaseToken</c> no
    /// va a la analítica); <c>value</c> es el precio local de la tienda y <c>items</c>, el SKU.
    /// <para>
    /// Con la tienda verificada (<see cref="VerifiedStoreService"/>, F3-5) también escucha
    /// <see cref="VerifiedStoreService.VerificationChanged"/>: lo que se resuelve fuera de una compra abierta (desde la cola de 72 h, al
    /// volver a primer plano o en el siguiente arranque; <see cref="PurchaseVerificationEvent.IsLive"/> false) sale como
    /// <c>af_iap_result</c> sin <c>context</c> (la cola no lo guarda: no se inventa): <c>ok</c> (y <c>purchase</c>) al acreditarse,
    /// <c>verify_failed</c> si el servidor la rechaza y <c>deferred_ack</c> al cumplir el plazo sin respuesta. Lo de una compra abierta
    /// sale una sola vez, con su resultado, por <see cref="Purchase"/>.
    /// </para>
    /// </summary>
    public sealed class AnalyticsStoreService : IStoreService
    {
        private readonly IStoreService _inner;
        private readonly AnalyticsService _analytics;

        /// <summary>Envuelve <paramref name="inner"/>.</summary>
        public AnalyticsStoreService(IStoreService inner, AnalyticsService analytics)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            if (inner is VerifiedStoreService verified)
            {
                verified.VerificationChanged += OnVerificationChanged;
            }
        }

        /// <inheritdoc />
        public bool IsAvailable => _inner.IsAvailable;

        /// <inheritdoc />
        public bool IsPurchasing => _inner.IsPurchasing;

        /// <inheritdoc />
        public StoreCatalog Catalog => _inner.Catalog;

        /// <summary>El servicio envuelto.</summary>
        public IStoreService Inner => _inner;

        /// <inheritdoc />
        public LocalPrice PriceOf(string sku) => _inner.PriceOf(sku);

        /// <inheritdoc />
        public void Purchase(PurchaseRequest request, Action<PurchaseResult> onDone)
        {
            _inner.Purchase(request, result =>
            {
                Record(result);
                onDone?.Invoke(result);
            });
        }

        /// <summary><c>af_iap_offer_shown</c> (08 §8.2): la vista enseña la oferta <paramref name="sku"/> en <paramref name="context"/>.</summary>
        public void OfferShown(string sku, PurchaseContext context)
        {
            LocalPrice price = _inner.PriceOf(sku);
            _analytics.Log(AnalyticsEvent.Create("af_iap_offer_shown")
                .Add("sku", sku)
                .Add("context", ContextId(context))
                .Add("price_local", (double)price.Amount)
                .Add("currency", price.Currency));
        }

        /// <summary>Los eventos de un resultado (público para las pruebas).</summary>
        public void Record(PurchaseResult result)
        {
            string sku = result.Request.Sku;
            string context = ContextId(result.Request.Context);
            _analytics.Log(AnalyticsEvent.Create("af_iap_result").Add("sku", sku).Add("result", ResultId(result.Status)).Add("context", context));
            if (!result.IsPurchased)
            {
                return;
            }

            LocalPrice price = _inner.PriceOf(sku);
            _analytics.Log(AnalyticsEvent.Create("purchase")
                .Add("value", (double)price.Amount)
                .Add("currency", price.Currency)
                .Add("items", sku)
                .Add("context", context));
        }

        /// <summary>
        /// <c>af_iap_result</c> de una compra que se resuelve fuera de una compra abierta (público para las pruebas). Lo de una compra
        /// abierta (<see cref="PurchaseVerificationEvent.IsLive"/>) no sale aquí: ya sale con su resultado.
        /// </summary>
        public void OnVerificationChanged(PurchaseVerificationEvent e)
        {
            if (e == null || e.IsLive)
            {
                return;
            }

            string? result;
            switch (e.State)
            {
                case PurchaseVerificationState.Credited: result = "ok"; break;
                case PurchaseVerificationState.Rejected: result = "verify_failed"; break;
                case PurchaseVerificationState.DeferredAck: result = "deferred_ack"; break;
                default: result = null; break; // verifying, pending, en cola, ya aplicada: nada nuevo que contar
            }

            if (result == null)
            {
                return;
            }

            _analytics.Log(AnalyticsEvent.Create("af_iap_result").Add("sku", e.Sku).Add("result", result));
            if (e.State != PurchaseVerificationState.Credited)
            {
                return;
            }

            LocalPrice price = _inner.PriceOf(e.Sku);
            _analytics.Log(AnalyticsEvent.Create("purchase")
                .Add("value", (double)price.Amount)
                .Add("currency", price.Currency)
                .Add("items", e.Sku));
        }

        /// <summary><c>context</c> de 08 §8.2 (<c>stuck</c>, <c>no_canoes</c>, <c>shop</c>, <c>starter_window</c>, <c>noads_prompt</c>, <c>piggy</c>).</summary>
        public static string ContextId(PurchaseContext context)
        {
            switch (context)
            {
                case PurchaseContext.Shop: return "shop";
                case PurchaseContext.Stuck: return "stuck";
                case PurchaseContext.NoCanoes: return "no_canoes";
                case PurchaseContext.StarterWindow: return "starter_window";
                case PurchaseContext.NoAdsPrompt: return "noads_prompt";
                case PurchaseContext.Piggy: return "piggy";
                default: return ((int)context).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// <c>result</c> de <c>af_iap_result</c> (08 §8.2: <c>ok</c>, <c>cancel</c>, <c>pending</c>, <c>failed</c>,
        /// <c>verify_failed</c>, <c>deferred_ack</c>): pagada y aún sin verificar es <c>pending</c>, rechazada por el servidor es
        /// <c>verify_failed</c> (08 §10.3, F3-3); lo que no se pudo comprar por otra causa (ya lo tiene, no se vende ahora, tienda
        /// apagada o compra en curso) cuenta como <c>failed</c> [P].
        /// </summary>
        public static string ResultId(PurchaseStatus status)
        {
            switch (status)
            {
                case PurchaseStatus.Purchased: return "ok";
                case PurchaseStatus.Cancelled: return "cancel";
                case PurchaseStatus.Pending: return "pending";
                case PurchaseStatus.VerifyFailed: return "verify_failed";
                default: return "failed";
            }
        }
    }
}
