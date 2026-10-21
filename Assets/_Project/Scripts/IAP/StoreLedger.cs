#nullable enable
using System;
using AF.Backend.Contract;
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

        /// <summary>
        /// Las Canoas infinitas que acredita una compra: con <c>biz_feature_lives</c> apagado (<see cref="Wallet.LivesEnabled"/>), la
        /// Balsa de rescate (el producto que aplica la Cuia de rescate) no las da y queda en «semillas y una cuia», con el mismo
        /// precio (D-038, añadido del 2026-10-21; 04 §6 y §7.2). Con el interruptor en su default, lo del catálogo o del servidor.
        /// </summary>
        public TimeSpan CreditedCanoes(ProductContent content, TimeSpan canoes)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return content.AppliesRescue && !Wallet.LivesEnabled ? TimeSpan.Zero : canoes;
        }

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
                product.Sku, seeds, content.Stock, CreditedCanoes(content, content.InfiniteCanoes), content.AppliesRescue, content.GrantsNoAds);

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

        /// <summary>Perfil sobre el que trabaja (el de la cartera).</summary>
        internal Profile Profile => _profile;

        /// <summary>
        /// Acredita una compra que el servidor verificó (F3-3; 08 §10.3), <b>una sola vez por</b> <paramref name="tokenHash"/>: si ya
        /// está en <c>applied_purchases</c> no cambia nada (<see cref="VerifiedCreditKind.AlreadyApplied"/>). Con <c>ok</c>,
        /// <paramref name="serverGrant"/> es el <c>grant</c> del servidor; con <c>duplicate</c> (sin <c>grant</c>) es null y se acredita el
        /// contenido del catálogo (05 §4.1), que el servidor espeja. El hash entra en <c>applied_purchases</c> antes de
        /// <c>Wallet.Apply</c>, para que salgan en la misma escritura; si la cartera la rechaza, se deshace todo. Si el jugador ya tiene
        /// el producto (Sin anuncios, la skin o la Mochila), solo anota el hash (<see cref="VerifiedCreditKind.AlreadyOwned"/>).
        /// </summary>
        /// <param name="product">Producto (del catálogo).</param>
        /// <param name="tokenHash"><c>token_hash</c> de la compra.</param>
        /// <param name="serverGrant">El <c>grant</c> de <c>ok</c>, o null con <c>duplicate</c>.</param>
        /// <param name="entitlements">Los <c>entitlements</c> de <c>ok</c>, o null.</param>
        /// <param name="clientSeeds">Las semillas del Cántaro (<c>af_piggy</c>), si se conocen.</param>
        /// <param name="appliesRescueNow">La Balsa se acredita en el momento de comprarla (su Cuia de rescate se aplica ya y cuenta para
        /// <c>rescues_today</c>); false si llega más tarde (cola, arranque).</param>
        internal VerifiedCredit CreditVerified(
            StoreProduct product, string tokenHash, PurchaseGrant? serverGrant, Entitlements? entitlements, int? clientSeeds, bool appliesRescueNow)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            if (tokenHash == null)
            {
                throw new ArgumentNullException(nameof(tokenHash));
            }

            if (_profile.HasAppliedPurchase(tokenHash))
            {
                return new VerifiedCredit(VerifiedCreditKind.AlreadyApplied, null, 0);
            }

            // La Mochila (1 por cuenta) la hace cumplir el servidor (F2c): si respondió ok o duplicate, ya la reconoció y Play no la
            // reembolsará, así que se acredita aunque la marca local ya esté puesta (p. ej. traída de otro perfil).
            if (IsOwned(product) && product.Kind != ProductKind.OncePerAccount)
            {
                _profile.AppliedPurchases.Add(tokenHash);
                Save();
                return new VerifiedCredit(VerifiedCreditKind.AlreadyOwned, null, 0);
            }

            ProductContent content = product.Content;
            int unknownItems = 0;
            long piggyCredited = 0;
            Transaction transaction;
            try
            {
                long piggySeeds = clientSeeds ?? Config.EcoPiggyMinBuy;
                long seeds;
                StockDelta stock = default;
                long canoesMinutes = 0;
                bool rescue = false;
                if (serverGrant != null)
                {
                    seeds = serverGrant.Seeds;
                    for (int i = 0; i < serverGrant.Items.Count; i++)
                    {
                        GrantItem item = serverGrant.Items[i];
                        if (TryBooster(item.Id, out BoosterKind kind))
                        {
                            stock = stock.Plus(kind, checked((int)item.Count));
                        }
                        else if (item.Id == GrantItemIds.Piggy)
                        {
                            seeds = checked(seeds + piggySeeds);
                            piggyCredited = checked(piggyCredited + piggySeeds);
                        }
                        else if (item.Id == GrantItemIds.InfiniteCanoesMinutes)
                        {
                            canoesMinutes = checked(canoesMinutes + item.Count);
                        }
                        else if (item.Id == GrantItemIds.RescueCuia)
                        {
                            rescue = true;
                        }
                        else
                        {
                            unknownItems++; // un ítem que este cliente no conoce no se acredita (ni rompe la compra)
                        }
                    }
                }
                else
                {
                    seeds = content.PiggySeeds ? piggySeeds : content.Seeds;
                    piggyCredited = content.PiggySeeds ? piggySeeds : 0;
                    stock = content.Stock;
                    canoesMinutes = (long)content.InfiniteCanoes.TotalMinutes;
                    rescue = content.AppliesRescue;
                }

                bool noAds = content.GrantsNoAds || (entitlements != null && entitlements.NoAds == true);
                transaction = Transaction.Purchase(
                    product.Sku, checked((int)seeds), stock, CreditedCanoes(content, TimeSpan.FromMinutes(canoesMinutes)), rescue && appliesRescueNow,
                    noAds);
            }
            catch (OverflowException)
            {
                return new VerifiedCredit(VerifiedCreditKind.Rejected, null, unknownItems);
            }

            AdsData ads = _profile.Ads;
            EntitlementsData owned = _profile.Entitlements;
            DailyData daily = _profile.Daily;
            DateTime? lastIap = ads.LastIap;
            bool starter = owned.StarterBought;
            int declined = daily.AfRescueDeclinedDays;
            DateTime? hiddenUntil = daily.AfRescueHiddenUntil;
            bool addedSkin = false;
            _profile.AppliedPurchases.Add(tokenHash);
            ads.LastIap = _clock.UtcNow;
            if (product.Kind == ProductKind.OncePerAccount)
            {
                owned.StarterBought = true;
            }

            if (content.GrantsSkin && !owned.Skins.Contains(product.Sku))
            {
                owned.Skins.Add(product.Sku);
                addedSkin = true;
            }

            if (content.AppliesRescue)
            {
                daily.AfRescueDeclinedDays = 0;
                daily.AfRescueHiddenUntil = null;
            }

            TransactionResult result = Wallet.Apply(transaction);
            if (!result.IsApplied)
            {
                _profile.AppliedPurchases.Remove(tokenHash);
                ads.LastIap = lastIap;
                owned.StarterBought = starter;
                if (addedSkin)
                {
                    owned.Skins.Remove(product.Sku);
                }

                daily.AfRescueDeclinedDays = declined;
                daily.AfRescueHiddenUntil = hiddenUntil;
                return new VerifiedCredit(VerifiedCreditKind.Rejected, result, unknownItems);
            }

            PendingSave = Wallet.PendingSave;
            return new VerifiedCredit(VerifiedCreditKind.Credited, result, unknownItems, checked((int)piggyCredited));
        }

        /// <summary>Guarda ya el perfil (la cola de verificación cambió).</summary>
        internal void SaveNow()
        {
            Save();
        }

        private static bool TryBooster(string id, out BoosterKind kind)
        {
            for (int i = 0; i < BoosterKinds.Count; i++)
            {
                var k = (BoosterKind)i;
                if (k.ToId() == id)
                {
                    kind = k;
                    return true;
                }
            }

            kind = default;
            return false;
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

namespace AF.IAP
{
    /// <summary>Ids de <c>grant.items</c> que no son comodines (<c>infra/functions/shared/catalog.ts</c>; [P], D-035, añadido de F2-3 a F2-8).</summary>
    public static class GrantItemIds
    {
        /// <summary><c>piggy</c>: las semillas acumuladas del Cántaro, que solo conoce el cliente.</summary>
        public const string Piggy = "piggy";

        /// <summary><c>infinite_canoes_min</c>: minutos de Canoas infinitas.</summary>
        public const string InfiniteCanoesMinutes = "infinite_canoes_min";

        /// <summary><c>rescue_cuia</c>: la Cuia de rescate que la Balsa aplica al instante.</summary>
        public const string RescueCuia = "rescue_cuia";
    }

    /// <summary>Qué hizo <see cref="StoreLedger.CreditVerified"/>.</summary>
    internal enum VerifiedCreditKind
    {
        /// <summary>Acreditada ahora (y su hash, en <c>applied_purchases</c>).</summary>
        Credited = 0,

        /// <summary>Ya estaba en <c>applied_purchases</c>: no se acredita otra vez.</summary>
        AlreadyApplied = 1,

        /// <summary>El jugador ya tenía el producto: solo se anotó el hash.</summary>
        AlreadyOwned = 2,

        /// <summary>La cartera la rechazó (desbordamiento): no cambió nada.</summary>
        Rejected = 3,
    }

    /// <summary>Resultado de <see cref="StoreLedger.CreditVerified"/>.</summary>
    internal sealed class VerifiedCredit
    {
        public VerifiedCredit(VerifiedCreditKind kind, TransactionResult? transaction, int unknownItems, int piggySeeds = 0)
        {
            Kind = kind;
            Transaction = transaction;
            UnknownItems = unknownItems;
            PiggySeeds = piggySeeds;
        }

        public VerifiedCreditKind Kind { get; }

        public TransactionResult? Transaction { get; }

        /// <summary>Ítems del <c>grant</c> que este cliente no conoce (no se acreditaron).</summary>
        public int UnknownItems { get; }

        /// <summary>Semillas del Cántaro (<c>af_piggy</c>) que entraron en la cartera con esta acreditación (0 si ninguna).</summary>
        public int PiggySeeds { get; }

        /// <summary>El hash queda en <c>applied_purchases</c> (acreditada ahora o antes): se puede terminar la compra en la tienda.</summary>
        public bool IsSettled => Kind != VerifiedCreditKind.Rejected;
    }
}
