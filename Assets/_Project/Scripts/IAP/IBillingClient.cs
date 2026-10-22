#nullable enable
using System;
using System.Collections.Generic;

namespace AF.IAP
{
    /// <summary>Cómo terminó una compra en la tienda de la plataforma (Play Billing), antes de verificarla.</summary>
    public enum BillingResultKind
    {
        /// <summary>Play la da por pagada (<c>PURCHASED</c>): trae su <see cref="BillingTransaction"/> para verificarla.</summary>
        Purchased = 0,

        /// <summary>
        /// Play la tiene pendiente de pago (<c>PENDING</c>: efectivo, transferencia…): no se verifica ni se acredita; cuando se pague,
        /// Play la entrega como compra sin terminar (<see cref="IBillingClient.UnfinishedTransactions"/>).
        /// </summary>
        Pending = 1,

        /// <summary>El jugador canceló.</summary>
        Cancelled = 2,

        /// <summary>Falló el pago.</summary>
        Failed = 3,
    }

    /// <summary>
    /// Una compra de la tienda sin terminar: Play la cobró y la app aún no la consumió (consumibles) ni la confirmó (no consumibles).
    /// El <see cref="Token"/> es el <c>purchaseToken</c>: va al servidor y al perfil, nunca a los logs.
    /// </summary>
    public sealed class BillingTransaction
    {
        /// <param name="sku">SKU <c>af_…</c>.</param>
        /// <param name="token"><c>purchaseToken</c> de Play Billing.</param>
        /// <param name="purchasedUtc">Hora de la compra según Play (<c>purchaseTime</c>), si se conoce.</param>
        public BillingTransaction(string sku, string token, DateTime? purchasedUtc = null)
        {
            if (string.IsNullOrEmpty(sku))
            {
                throw new ArgumentException("Falta el SKU.", nameof(sku));
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Falta el token.", nameof(token));
            }

            if (purchasedUtc.HasValue && purchasedUtc.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Se esperaba una hora UTC.", nameof(purchasedUtc));
            }

            Sku = sku;
            Token = token;
            PurchasedUtc = purchasedUtc;
        }

        public string Sku { get; }

        /// <summary><c>purchaseToken</c>. No va a los logs.</summary>
        public string Token { get; }

        public DateTime? PurchasedUtc { get; }

        /// <summary>Sin el token.</summary>
        public override string ToString() => "BillingTransaction(" + Sku + ")";
    }

    /// <summary>Resultado de <see cref="IBillingClient.Purchase"/>.</summary>
    public sealed class BillingResult
    {
        private BillingResult(BillingResultKind kind, BillingTransaction? transaction)
        {
            Kind = kind;
            Transaction = transaction;
        }

        public BillingResultKind Kind { get; }

        /// <summary>La compra (solo con <see cref="BillingResultKind.Purchased"/>).</summary>
        public BillingTransaction? Transaction { get; }

        public static BillingResult Purchased(BillingTransaction transaction) =>
            new BillingResult(BillingResultKind.Purchased, transaction ?? throw new ArgumentNullException(nameof(transaction)));

        public static BillingResult Pending() => new BillingResult(BillingResultKind.Pending, null);

        public static BillingResult Cancelled() => new BillingResult(BillingResultKind.Cancelled, null);

        public static BillingResult Failed() => new BillingResult(BillingResultKind.Failed, null);
    }

    /// <summary>
    /// La tienda de la plataforma por debajo de <see cref="VerifiedStoreService"/> (F3-3; 08 §10.3; D-035): <see cref="PlayBillingClient"/>
    /// sobre Play Billing Library 9.0.0 en el teléfono (D-040), y <see cref="FakeBillingClient"/> en el editor, los tests y los builds sin
    /// backend real. No acredita nada: eso lo hace <see cref="VerifiedStoreService"/> solo tras verificar en el servidor. Hilo principal; cada
    /// llamada con <c>onDone</c> lo llama una sola vez (puede ser antes de volver).
    /// <para>
    /// Correspondencia con Play Billing: <see cref="Purchase"/> = <c>launchBillingFlow</c> con <c>setObfuscatedAccountId</c>;
    /// <see cref="UnfinishedTransactions"/> = lo que devuelve <c>queryPurchasesAsync</c> sin terminar; <see cref="Finish"/> =
    /// <c>consumeAsync</c> (consumibles) o <c>acknowledgePurchase</c> (el resto); <see cref="AcknowledgeLocal"/> = reconocer sin consumir
    /// (<c>acknowledgePurchase</c>).
    /// </para>
    /// </summary>
    public interface IBillingClient
    {
        /// <summary>Precio local de <paramref name="product"/> (D-025: desde <c>priceAmountMicros</c> y la moneda, nunca la cadena de Play).</summary>
        LocalPrice PriceOf(StoreProduct product);

        /// <summary>
        /// Abre la compra de <paramref name="sku"/> con <c>setObfuscatedAccountId</c> = <paramref name="obfuscatedAccountId"/> (el UID de
        /// Firebase Auth, si lo hay; 08 §10.1) y llama a <paramref name="onDone"/> una vez.
        /// </summary>
        void Purchase(string sku, string? obfuscatedAccountId, Action<BillingResult> onDone);

        /// <summary>Las compras cobradas que la app aún no terminó (se vuelven a entregar en cada arranque; 08 §10.3).</summary>
        IReadOnlyList<BillingTransaction> UnfinishedTransactions();

        /// <summary>
        /// Termina una compra ya acreditada: con <paramref name="consume"/> la consume (consumibles: lo hace el cliente, D-035, añadido
        /// de F2-3 a F2-8); sin él la confirma (no consumibles; el servidor ya la reconoció). Nunca con una compra rechazada.
        /// </summary>
        void Finish(string sku, string token, bool consume);

        /// <summary>
        /// <c>acknowledge</c> local sin consumir (<c>deferred_ack</c>, 08 §10.3): evita que Play la reembolse a los 3 días mientras el
        /// servidor no responde. No acredita nada.
        /// </summary>
        void AcknowledgeLocal(string sku, string token);
    }

    /// <summary>
    /// Interruptor de corte del backend: <c>biz_backend_enabled</c> [P] (D-035, añadido de F3-1 y F3-2; plan §7, B7), que aún no está en
    /// la tabla de 08 §9 (la añade la integración del GDD; default <c>true</c>). Apagado, no se habla con el backend y no se venden
    /// compras nuevas (no se podrían acreditar); lo ya pagado espera en la cola y el plazo del <c>acknowledge</c> local sigue corriendo.
    /// Mientras no exista <c>AF.RemoteConfig</c>, la implementación es <see cref="BackendSwitch"/> con el default.
    /// </summary>
    public interface IBackendSwitch
    {
        /// <summary><c>biz_backend_enabled</c>.</summary>
        bool BizBackendEnabled { get; }
    }

    /// <summary><see cref="IBackendSwitch"/> con el default compilado (<c>true</c>), modificable (tests y, más adelante, Remote Config).</summary>
    public sealed class BackendSwitch : IBackendSwitch
    {
        /// <summary>Clave de Remote Config [P], pendiente de alta en 08 §9.</summary>
        public const string Key = "biz_backend_enabled";

        /// <summary>Default compilado.</summary>
        public const bool Default = true;

        /// <inheritdoc />
        public bool BizBackendEnabled { get; set; } = Default;
    }

    /// <summary>
    /// El UID de Firebase Auth anónimo (08 §11), que va como <c>setObfuscatedAccountId</c> en la compra (08 §10.1) y como
    /// <c>obfuscated_account_id</c> en <c>verifyPurchase</c>. El real llega con F3-6 (SDK de Firebase); hasta entonces,
    /// <see cref="FixedPlayerUid"/>. Nunca va a los logs.
    /// </summary>
    public interface IPlayerUidProvider
    {
        /// <summary>El UID, o null si aún no lo hay.</summary>
        string? Uid { get; }
    }

    /// <summary><see cref="IPlayerUidProvider"/> fijo (editor, tests y builds sin Firebase).</summary>
    public sealed class FixedPlayerUid : IPlayerUidProvider
    {
        public FixedPlayerUid(string? uid = null)
        {
            Uid = uid;
        }

        /// <inheritdoc />
        public string? Uid { get; set; }
    }
}
