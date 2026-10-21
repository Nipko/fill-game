#nullable enable
using System;
using AF.Platform;

namespace AF.IAP
{
    /// <summary>
    /// La segunda confirmación de una compra en el juego (Q-T4 (a) de D-033: con 13-17, «doble confirmación en el juego + la
    /// autenticación de Google Play»). La pinta <c>AF.UI.ConsentScreen</c>; los tests usan <see cref="ImmediatePurchaseConfirmation"/>.
    /// </summary>
    public interface IPurchaseConfirmation
    {
        /// <summary>
        /// Pregunta si se compra <paramref name="product"/> por <paramref name="price"/> y llama una sola vez a
        /// <paramref name="onAnswer"/> (true = comprar).
        /// </summary>
        void Confirm(StoreProduct product, LocalPrice price, Action<bool> onAnswer);
    }

    /// <summary><see cref="IPurchaseConfirmation"/> que responde en el acto (tests).</summary>
    public sealed class ImmediatePurchaseConfirmation : IPurchaseConfirmation
    {
        /// <summary>Crea la confirmación con la respuesta <paramref name="answer"/>.</summary>
        public ImmediatePurchaseConfirmation(bool answer = true)
        {
            Answer = answer;
        }

        /// <summary>Lo que responde.</summary>
        public bool Answer { get; set; }

        /// <summary>Veces que se preguntó.</summary>
        public int Asked { get; private set; }

        /// <summary>El último producto preguntado.</summary>
        public StoreProduct? Last { get; private set; }

        /// <inheritdoc />
        public void Confirm(StoreProduct product, LocalPrice price, Action<bool> onAnswer)
        {
            Asked++;
            Last = product;
            onAnswer(Answer);
        }
    }

    /// <summary>
    /// La tienda detrás del consentimiento y la edad (bloque B12; 08 §2.3 y §7; 05 §7), envolviendo a la tienda real o falsa:
    /// <list type="bullet">
    /// <item>Sin compras permitidas (<see cref="ConsentFlags.PurchasesAllowed"/>: &lt;13 o sin decidir), toda compra da
    /// <see cref="PurchaseStatus.StoreUnavailable"/> sin abrir nada (08 §7: «sin IAP»).</item>
    /// <item>Con 13-17 y <c>biz_under18_no_personalization</c> (<see cref="ConsentFlags.PurchaseDoubleConfirm"/>), antes de la hoja de
    /// Google Play sale la segunda confirmación en el juego (<see cref="IPurchaseConfirmation"/>); «Cancelar» da
    /// <see cref="PurchaseStatus.Cancelled"/> sin descontar nada (05 §4.1). La autenticación de Google Play la pide la propia Play
    /// según los ajustes del teléfono y de Family Link: el cliente no la puede forzar [NV, a confirmar con el abogado]; con la tienda
    /// falsa, la hoja «Compra simulada» hace sus veces.</item>
    /// <item>Con 18+, pasa tal cual. Hasta el primer <see cref="Apply"/>, ninguna compra (falla cerrado).</item>
    /// </list>
    /// Mientras la confirmación está abierta, <see cref="IsPurchasing"/> es cierto (una sola compra a la vez). Hilo principal.
    /// </summary>
    public sealed class ConsentStoreService : IStoreService
    {
        private readonly IStoreService _inner;
        private bool _confirming;

        /// <summary>Envuelve <paramref name="inner"/>; hasta <see cref="Apply"/> no deja comprar (falla cerrado: «sin decidir, sin compras»).</summary>
        public ConsentStoreService(IStoreService inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>La tienda envuelta.</summary>
        public IStoreService Inner => _inner;

        /// <summary>La segunda confirmación (la fija el arranque). Sin ella, una compra que la necesita se cancela.</summary>
        public IPurchaseConfirmation? Confirmation { get; set; }

        /// <summary>Los flags aplicados (null hasta <see cref="Apply"/>).</summary>
        public ConsentFlags? Flags { get; private set; }

        /// <summary>Se puede comprar con los flags vigentes (sin flags, no).</summary>
        public bool PurchasesAllowed => Flags != null && Flags.PurchasesAllowed;

        /// <summary>
        /// Comprobación de que el producto se puede comprar antes de la segunda confirmación (la fija el arranque con
        /// <c>StoreLedger.CheckPurchasable</c>): así nunca se confirma con precio una compra que la tienda va a rechazar (ya
        /// comprada, Balsa antes del 31…). Null = sin comprobación previa.
        /// </summary>
        public Func<StoreProduct, PurchaseRequest, PurchaseStatus?>? Precheck { get; set; }

        /// <summary>Aplica <paramref name="flags"/> (al arrancar y al cambiar la edad en Ajustes).</summary>
        public void Apply(ConsentFlags flags)
        {
            Flags = flags ?? throw new ArgumentNullException(nameof(flags));
        }

        /// <inheritdoc />
        public bool IsAvailable => _inner.IsAvailable && PurchasesAllowed;

        /// <inheritdoc />
        public bool IsPurchasing => _confirming || _inner.IsPurchasing;

        /// <inheritdoc />
        public StoreCatalog Catalog => _inner.Catalog;

        /// <inheritdoc />
        public LocalPrice PriceOf(string sku) => _inner.PriceOf(sku);

        /// <inheritdoc />
        public void Purchase(PurchaseRequest request, Action<PurchaseResult> onDone)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (onDone == null)
            {
                throw new ArgumentNullException(nameof(onDone));
            }

            StoreProduct product = Catalog.Get(request.Sku);
            if (IsPurchasing)
            {
                onDone(new PurchaseResult(request, product, PurchaseStatus.Busy));
                return;
            }

            if (!PurchasesAllowed)
            {
                onDone(new PurchaseResult(request, product, PurchaseStatus.StoreUnavailable));
                return;
            }

            if (!Flags!.PurchaseDoubleConfirm || !_inner.IsAvailable)
            {
                _inner.Purchase(request, onDone); // la tienda envuelta responde ella misma si está apagada
                return;
            }

            PurchaseStatus? blocked = Precheck?.Invoke(product, request);
            if (blocked.HasValue)
            {
                onDone(new PurchaseResult(request, product, blocked.Value)); // sin confirmar lo que no se puede comprar
                return;
            }

            IPurchaseConfirmation? confirmation = Confirmation;
            if (confirmation == null)
            {
                onDone(new PurchaseResult(request, product, PurchaseStatus.Cancelled));
                return;
            }

            bool answered = false;
            _confirming = true;
            confirmation.Confirm(product, _inner.PriceOf(request.Sku), yes =>
            {
                if (answered)
                {
                    return;
                }

                answered = true;
                _confirming = false;
                if (!yes)
                {
                    onDone(new PurchaseResult(request, product, PurchaseStatus.Cancelled));
                    return;
                }

                _inner.Purchase(request, onDone);
            });
        }
    }
}
