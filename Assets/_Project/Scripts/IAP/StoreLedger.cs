#nullable enable
using System;
using AF.Economy;
using AF.Platform;
using AF.Save;

namespace AF.IAP
{
    /// <summary>
    /// Lo que la tienda lee y escribe en el perfil (GDD 05 §4.1 y §5; 04 §6.2; 08 §4.3), sea cual sea el proveedor:
    /// <list type="bullet">
    /// <item><see cref="Fulfill"/>: acredita lo comprado con <c>Wallet.Apply(Transaction.Purchase(…))</c> (semillas, stock,
    /// Canoas infinitas, rescate aplicado y Sin anuncios) y anota lo que no es de la cartera: <c>ads.last_iap</c> (corta los
    /// intersticiales 24 h, también con <c>af_infinite_canoes_1h</c>), <c>entitlements.starter_bought</c>,
    /// <c>entitlements.skins</c> y, con la Balsa, vuelve a 0 sus rechazos. Si la cartera la rechaza, deshace esas
    /// anotaciones: nada a medias;</item>
    /// <item>la Balsa de rescate: 1 impresión al día (<c>af_rescue_daily_cap</c>) y oculta 48 h tras 2 rechazos
    /// (<c>af_rescue_hide_after_declines</c>); un rechazo es cancelar su compra (04 §6.2).</item>
    /// </list>
    /// Usa el mismo <see cref="Profile"/> que la <see cref="Wallet"/>. Solo datos locales: sin red ni identificadores.
    /// </summary>
    public sealed class StoreLedger
    {
        private readonly Profile _profile;
        private readonly IProfileStore _store;
        private readonly IClock _clock;

        /// <summary>Crea el registro sobre <paramref name="profile"/>, el perfil de <paramref name="wallet"/>.</summary>
        /// <exception cref="InvalidOperationException">La configuración no es válida.</exception>
        public StoreLedger(Profile profile, Wallet wallet, IProfileStore store, IClock clock, StoreConfig? config = null)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Config = config ?? StoreConfig.Defaults;
            Catalog = new StoreCatalog(Config);
        }

        /// <summary>Cifras.</summary>
        public StoreConfig Config { get; }

        /// <summary>Catálogo.</summary>
        public StoreCatalog Catalog { get; }

        /// <summary>Cartera donde se acredita.</summary>
        public Wallet Wallet { get; }

        /// <summary>Impresiones de la Balsa de rescate hoy (día local).</summary>
        public int BalsaShownToday => DailyCounters.IsStale(_profile.Daily, _clock.LocalDateKey()) ? 0 : _profile.Daily.AfRescueShown;

        /// <summary>La Balsa está oculta por rechazos hasta esta hora (UTC), o null.</summary>
        public DateTime? BalsaHiddenUntil
        {
            get
            {
                DateTime? until = _profile.Daily.AfRescueHiddenUntil;
                return until.HasValue && _clock.UtcNow < until.Value ? until : null;
            }
        }

        /// <summary>Un cambio quedó sin guardar porque falló la escritura; se reintenta con la próxima anotación o con <see cref="Flush"/>.</summary>
        public bool PendingSave { get; private set; }

        /// <summary>Ya tiene <paramref name="product"/> (Sin anuncios, Mochila de inicio o la skin).</summary>
        public bool IsOwned(StoreProduct product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            if (product.Content.GrantsNoAds)
            {
                return _profile.Entitlements.NoAds;
            }

            if (product.Kind == ProductKind.OncePerAccount)
            {
                return _profile.Entitlements.StarterBought;
            }

            return product.Content.GrantsSkin && _profile.Entitlements.Skins.Contains(product.Sku);
        }

        /// <summary>
        /// ¿Se puede vender <paramref name="product"/> con <paramref name="request"/>? Null si sí; si no,
        /// <see cref="PurchaseStatus.AlreadyOwned"/> o <see cref="PurchaseStatus.NotAvailable"/> (Balsa antes de
        /// <c>rescue_min_level</c>, que 05 §10 prohíbe; Cántaro fuera de <c>eco_piggy_min_buy</c>-<c>eco_piggy_cap</c>).
        /// </summary>
        public PurchaseStatus? CheckPurchasable(StoreProduct product, PurchaseRequest request)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!string.Equals(product.Sku, request.Sku, StringComparison.Ordinal))
            {
                throw new ArgumentException("La petición es de otro SKU.", nameof(request));
            }

            if (IsOwned(product))
            {
                return PurchaseStatus.AlreadyOwned;
            }

            if (product.Content.AppliesRescue && request.Level < Wallet.Config.RescueMinLevel)
            {
                return PurchaseStatus.NotAvailable;
            }

            if (product.Content.PiggySeeds && (request.PiggySeeds < Config.EcoPiggyMinBuy || request.PiggySeeds > Config.EcoPiggyCap))
            {
                return PurchaseStatus.NotAvailable;
            }

            return null;
        }

        /// <summary>
        /// ¿Se ofrece hoy la Balsa de rescate en <paramref name="level"/>? Tienda encendida, nivel ≥ <c>rescue_min_level</c>,
        /// menos de <c>af_rescue_daily_cap</c> impresiones hoy y no oculta por rechazos. Que el jugador no tenga saldo para
        /// el rescate lo comprueba el modal (04 §6.2 <c>balsa_ok</c>).
        /// </summary>
        public bool CanOfferBalsa(int level)
        {
            return Config.BizIapEnabled && level >= Wallet.Config.RescueMinLevel && !BalsaHiddenUntil.HasValue
                && BalsaShownToday < Config.AfRescueDailyCap;
        }

        /// <summary>El modal enseñó la Balsa de rescate: cuenta para <c>af_rescue_daily_cap</c>.</summary>
        public void RecordBalsaShown()
        {
            DailyCounters.RollOver(_profile.Daily, _clock.LocalDateKey());
            _profile.Daily.AfRescueShown++;
            _profile.EconomyTs = _clock.UtcNow;
            Save();
        }

        /// <summary>
        /// El jugador rechazó la Balsa (canceló su compra): +1 rechazo; al llegar a <c>af_rescue_hide_after_declines</c> se
        /// oculta <see cref="StoreConfig.BalsaHideDuration"/> y la cuenta vuelve a 0.
        /// </summary>
        public void RecordBalsaDeclined()
        {
            DailyData daily = _profile.Daily;
            DateTime now = _clock.UtcNow;
            daily.AfRescueDeclinedDays++;
            if (daily.AfRescueDeclinedDays >= Config.AfRescueHideAfterDeclines)
            {
                daily.AfRescueHiddenUntil = now + StoreConfig.BalsaHideDuration;
                daily.AfRescueDeclinedDays = 0;
            }

            _profile.EconomyTs = now;
            Save();
        }

        /// <summary>
        /// Acredita una compra ya confirmada por la tienda (en la tienda real, tras verificar el recibo). Devuelve
        /// <see cref="PurchaseStatus.Purchased"/>, o <see cref="PurchaseStatus.AlreadyOwned"/>,
        /// <see cref="PurchaseStatus.NotAvailable"/> o <see cref="PurchaseStatus.Failed"/> sin cambiar nada.
        /// </summary>
        public PurchaseResult Fulfill(PurchaseRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            StoreProduct product = Catalog.Get(request.Sku);
            PurchaseStatus? blocked = CheckPurchasable(product, request);
            if (blocked.HasValue)
            {
                return new PurchaseResult(request, product, blocked.Value);
            }

            ProductContent content = product.Content;
            int seeds = content.PiggySeeds ? request.PiggySeeds : content.Seeds;
            Transaction transaction = Transaction.Purchase(
                product.Sku, seeds, content.Stock, content.InfiniteCanoes, content.AppliesRescue, content.GrantsNoAds);

            // Lo que no es de la cartera se anota antes para que el mismo guardado de Wallet.Apply lo incluya.
            AdsData ads = _profile.Ads;
            EntitlementsData entitlements = _profile.Entitlements;
            DailyData daily = _profile.Daily;
            DateTime? lastIap = ads.LastIap;
            int declined = daily.AfRescueDeclinedDays;
            DateTime? hiddenUntil = daily.AfRescueHiddenUntil;
            ads.LastIap = _clock.UtcNow;
            if (product.Kind == ProductKind.OncePerAccount)
            {
                entitlements.StarterBought = true;
            }

            if (content.GrantsSkin)
            {
                entitlements.Skins.Add(product.Sku);
            }

            if (content.AppliesRescue)
            {
                daily.AfRescueDeclinedDays = 0;
                daily.AfRescueHiddenUntil = null;
            }

            TransactionResult result = Wallet.Apply(transaction);
            if (!result.IsApplied)
            {
                ads.LastIap = lastIap;
                if (product.Kind == ProductKind.OncePerAccount)
                {
                    entitlements.StarterBought = false;
                }

                if (content.GrantsSkin)
                {
                    entitlements.Skins.Remove(product.Sku);
                }

                daily.AfRescueDeclinedDays = declined;
                daily.AfRescueHiddenUntil = hiddenUntil;
                return new PurchaseResult(request, product, PurchaseStatus.Failed, result);
            }

            PendingSave = Wallet.PendingSave;
            return new PurchaseResult(request, product, PurchaseStatus.Purchased, result);
        }

        /// <summary>Reintenta un guardado pendiente. Devuelve si no queda nada por guardar.</summary>
        public bool Flush()
        {
            if (PendingSave)
            {
                Save();
            }

            return !PendingSave;
        }

        private void Save()
        {
            PendingSave = !_store.TrySave(_profile);
        }
    }
}
