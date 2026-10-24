#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Analytics;
using AF.Economy;
using AF.GameFlow;
using AF.IAP;
using AF.Levels;
using AF.Platform;
using AF.Save;
using UnityEngine;

namespace AF.UI
{
    /// <summary>
    /// Arranque del Mercado y de las ofertas (bloque B5; nota <c>docs/dev/mercado.md</c>): <see cref="SetUpMarket"/> crea el Cántaro
    /// (<see cref="PiggyJar"/>), el planificador de ofertas (<see cref="OfferScheduler"/>, el <c>offer_scheduler</c> de 05 §5), el
    /// Mercado (<see cref="MarketScreen"/>), la tarjeta de oferta (<see cref="OfferCard"/>) y las dos entradas del inicio
    /// (<see cref="MarketLobbyIcons"/>), con el perfil, la tienda, la cartera, las skins y las sesiones que ya creó <c>Start</c>, y
    /// añade <see cref="MarketRunner"/>, que cada fotograma:
    /// <list type="bullet">
    /// <item><description>detecta la victoria de campaña: llena el Cántaro (<c>eco_piggy_fill</c>) y, pasados
    /// <see cref="OfferCardDelayMs"/> con la victoria aún a la vista y nada encima, enseña la tarjeta que toque (como mucho una por
    /// sesión: <c>ui_max_takeovers_per_session</c>);</description></item>
    /// <item><description>cuenta los intersticiales vistos para <c>eco_noads_card_trigger</c> (por el contador del día de
    /// <c>AF.Ads</c>, sin tocar sus archivos);</description></item>
    /// <item><description>enseña el icono del Mercado (desde <c>max_level</c> ≥ 10) y el Cántaro (desde <c>eco_piggy_from_level</c>)
    /// sobre el inicio.</description></item>
    /// </list>
    /// Parcial nuevo: <c>GameBootstrap.cs</c> no se toca en la rama. Enganches (parches de <c>docs/dev/traspaso/spark-enganches/</c>):
    /// <list type="bullet">
    /// <item><b>B5-bootstrap</b>: la llamada a <see cref="SetUpMarket"/> en <c>Start</c> (tras <c>SetUpSessions();</c>), Atrás de
    /// Android (<see cref="PressMarketBack"/>), el Mercado y la tarjeta en <c>CommerceOpen</c> (<see cref="MarketCommerceOpen"/>) y
    /// <c>geo_tier</c> desde la región del teléfono (<see cref="GeoTierRule"/>; Q-E3 (a)).</item>
    /// <item><b>B5-profile</b>: <c>piggy</c> y <c>offers</c> en el perfil, con <c>ProfileOffersStore</c> (implementa
    /// <see cref="CreateProfileOffersStore"/>). Sin él: en desarrollo, en memoria (se pierde al cerrar la app); en release no hay
    /// Mercado ni ofertas (D-008: oculto mientras no exista).</item>
    /// <item><b>B5-rc</b>: <c>OffersConfig</c> en <c>RcConfigBinder</c> (implementa <see cref="BoundOffersConfig"/>). Sin él, los
    /// defaults de 08 §9.</item>
    /// </list>
    /// </summary>
    public sealed partial class GameBootstrap
    {
        /// <summary>
        /// La tarjeta de oferta sale este tiempo después de entrar en la victoria (ms) [P] (Q-B5-6): tras la Cosecha, la Crecida y la
        /// Canasta del panel, cuando aparece «Siguiente» (D-033, añadido del 2026-10-20, punto 10: «Siguiente a 1,40 s»).
        /// </summary>
        public const float OfferCardDelayMs = 1500f;

        private IOffersStore? _offersStore;
        private OffersState? _offersState;
        private PiggyJar? _piggy;
        private OfferScheduler? _offers;
        private MarketScreen? _market;
        private OfferCard? _offerCard;
        private MarketLobbyIcons? _marketIcons;
        private MarketRunner? _marketRunner;
        private FlowPhase _marketPhaseSeen;
        private VictoryOfferContext _pendingOfferCtx;
        private OfferKind _pendingOfferKind;
        private bool _pendingOffer;
        private float _pendingOfferMs;
        private VictoryOfferContext _lobbyOfferCtx;
        private OfferKind _lobbyOfferKind;
        private bool _lobbyOffer;
        private bool _offerFromVictory;
        private int _interstitialsTodaySeen;
        private bool _iconsShown;
        private bool _iconsMarket;
        private bool _iconsPiggy;
        private int _iconsPiggySeeds = -1;
        private bool _iconsPiggyFull;

        // La reconstrucción por idioma (RebuildUi) recarga la escena con el mismo perfil y sigue la misma sesión (B6): el cupo de
        // takeovers ya gastado sigue gastado.
        private static Profile? s_marketProfile;
        private static ITakeoverBudget? s_carriedBudget;

        /// <summary>Almacén de <c>piggy</c> y <c>offers</c> para los tests (gana al del perfil); <see cref="ResetMarketOverrides"/>.</summary>
        internal static IOffersStore? OffersStoreOverride { get; set; }

        /// <summary>Cifras de las ofertas para los tests (gana a Remote Config).</summary>
        internal static OffersConfig? OffersConfigOverride { get; set; }

        /// <summary>Cupo de takeovers para los tests (por defecto, uno por sesión de B6).</summary>
        internal static ITakeoverBudget? TakeoverBudgetOverride { get; set; }

        /// <summary>Venta de skins para los tests (por defecto, <see cref="OfferRules.SkinSalesEnabledDefault"/>).</summary>
        internal static bool? SkinSalesOverride { get; set; }

        /// <summary>El Cántaro (null antes de <see cref="SetUpMarket"/>).</summary>
        public PiggyJar? Piggy => _piggy;

        /// <summary>El planificador de ofertas (null antes de <see cref="SetUpMarket"/>).</summary>
        public OfferScheduler? Offers => _offers;

        /// <summary>El Mercado (null antes de <see cref="SetUpMarket"/>).</summary>
        internal MarketScreen? Market => _market;

        /// <summary>La tarjeta de oferta (null antes de <see cref="SetUpMarket"/>).</summary>
        internal OfferCard? OfferCardView => _offerCard;

        /// <summary>Las entradas del inicio (null antes de <see cref="SetUpMarket"/>).</summary>
        internal MarketLobbyIcons? MarketIcons => _marketIcons;

        /// <summary>La última tarjeta de oferta enseñada (tests y sonda), o <see cref="OfferKind.None"/>.</summary>
        public OfferKind LastOfferShown { get; private set; }

        /// <summary>El Mercado o una tarjeta de oferta están delante (pantallas de pago: sin la ventanita de casa; D-013, punto 6).</summary>
        internal bool MarketCommerceOpen => (_market != null && _market.Visible) || (_offerCard != null && _offerCard.Visible);

        /// <summary>Limpia los ganchos de test del Mercado.</summary>
        internal static void ResetMarketOverrides()
        {
            OffersStoreOverride = null;
            OffersConfigOverride = null;
            TakeoverBudgetOverride = null;
            SkinSalesOverride = null;
            s_marketProfile = null;
            s_carriedBudget = null;
            VictoryLeavingOverride = null;
        }

        /// <summary>
        /// Crea el Mercado y las ofertas con lo que ya construyó <c>Start</c>. Devuelve false, sin crear nada, antes del arranque o en
        /// release sin almacén en el perfil. Una segunda llamada no hace nada y devuelve true.
        /// </summary>
        public bool SetUpMarket()
        {
            if (_offers != null)
            {
                return true;
            }

            Profile? profile = _profile;
            ProfileSaveBatch? batch = _saveBatch;
            if (profile == null || batch == null || _store == null || _storeLedger == null || _art == null || _app == null || _lobby == null
                || _flow == null || _progress == null || _ads == null)
            {
                Debug.LogError(LogPrefix + "SetUpMarket antes del arranque: el Mercado no se crea.");
                return false;
            }

            IOffersStore? store = OffersStoreOverride;
            if (store == null)
            {
                CreateProfileOffersStore(profile, batch, ref store);
            }

            if (store == null)
            {
                if (!Debug.isDebugBuild)
                {
                    return false; // release sin B5-profile: el Cántaro no se guardaría; el Mercado queda oculto (D-008)
                }

                store = new InMemoryOffersStore();
                Debug.LogWarning(LogPrefix + "Cántaro y ofertas en memoria (sin B5-profile): se pierden al cerrar la app.");
            }

            OffersConfig? config = OffersConfigOverride;
            if (config == null)
            {
                BoundOffersConfig(ref config);
            }

            config ??= OffersConfig.Defaults;
            IClock clock = LaunchOptions.ClockOverride ?? SystemClock.Instance;
            ITakeoverBudget? budget = TakeoverBudgetOverride;
            if (budget == null && _takeovers != null)
            {
                // Q-B5-10 (a): un solo cupo, el de la cola de takeovers de B4 (que lleva sus propias sesiones, Q-M4); sin
                // Bitácora (apagada o sin crear), el cupo propio de B5.
                budget = new TakeoverQueueBudget(_takeovers);
            }

            if (budget == null)
            {
                bool rebuilt = ReferenceEquals(s_marketProfile, profile) && s_carriedBudget != null;
                budget = rebuilt ? s_carriedBudget! : new SessionTakeoverBudget(() => GameBootstrap.Current?._sessions?.Current?.Number ?? 1);
                s_marketProfile = profile;
                s_carriedBudget = budget;
            }
            _offersStore = store;
            _offersState = new OffersState(store);
            _piggy = new PiggyJar(_offersState, config, _storeLedger.Config);
            _offers = new OfferScheduler(_offersState, _storeLedger, _piggy, clock, profile.CreatedAt, budget, config)
            {
                PurchasesBlocked = ProfileLocked,
                SkinSalesEnabled = SkinSalesOverride ?? OfferRules.SkinSalesEnabledDefault,
            };

            IProgressStore progress = _progress;
            IStoreService storeService = _store;
            var deps = new MarketDeps(_store, _offers, _skinService, () => progress.MaxLevel, () => WorldTable.WorldOf(Math.Clamp(progress.MaxLevel + 1, 1, WorldTable.LastLevel)))
            {
                PurchasesBlocked = ProfileLocked,
                Notes = _services?.PurchaseNotes, // F3-5: «Pendiente de confirmar» y «No se pudo confirmar la compra» en cada fila
                OfferShown = sku =>
                {
                    if (storeService is AF.Analytics.Adapters.AnalyticsStoreService analytics)
                    {
                        analytics.OfferShown(sku, PurchaseContext.Shop); // af_iap_offer_shown con context = shop (08 §8.2)
                    }
                },
            };
            _market = new MarketScreen(transform, _art, deps);
            _market.Purchased += OnMarketPurchased;
            _market.SkinSelected += OnMarketSkinSelected;
            _market.Shown += OnMarketShown;
            _offerCard = new OfferCard(transform, _art);
            _marketIcons = new MarketLobbyIcons(transform, _art, () => OpenMarket(MarketTab.Seeds), () => PressPiggy());
            _marketPhaseSeen = _flow.Phase;
            _interstitialsTodaySeen = _ads.Ledger.InterstitialsToday;

            MarketRunner runner = gameObject.GetComponent<MarketRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<MarketRunner>();
            }

            _marketRunner = runner;
            _marketRunner.Bind(this);
            return true;
        }

        /// <summary>
        /// Gancho de B5-profile: <c>GameBootstrap.MarketProfile.cs</c> lo implementa con <c>ProfileOffersStore</c> sobre
        /// <paramref name="profile"/> y la vía síncrona del lote (como la cartera). Sin el parche, <paramref name="store"/> queda null.
        /// </summary>
        static partial void CreateProfileOffersStore(Profile profile, ProfileSaveBatch batch, ref IOffersStore? store);

        /// <summary>Gancho de B5-rc: las cifras de <c>OffersConfig</c> leídas de Remote Config. Sin el parche, null (los defaults).</summary>
        static partial void BoundOffersConfig(ref OffersConfig? config);

        /// <summary>
        /// Abre el Mercado en <paramref name="tab"/> desde el inicio (el icono): solo con el inicio a la vista, sin nada encima y desde
        /// <c>max_level</c> ≥ <see cref="MarketModel.FromMaxLevel"/>. Escribe <c>AF_STATE market_open</c>.
        /// </summary>
        public bool OpenMarket(MarketTab tab)
        {
            MarketScreen? market = _market;
            if (market == null || market.Visible || !MarketAvailable || !LobbyClear())
            {
                return false;
            }

            market.Open(tab, OnMarketClosed);
            if (DevProbe.Enabled)
            {
                MarketProbe("market_open", "\"tab\":\"" + MarketTabId(market.Tab) + "\"");
            }
            _analytics?.Log(AnalyticsEvent.Create("af_shop_view").Add("tab", MarketTabId(market.Tab)).Add("from", "lobby")); // 08 §8.2 [P] from
            return true;
        }

        /// <summary>El Mercado se puede abrir: creado, con la tienda encendida y desde <c>max_level</c> ≥ 10.</summary>
        public bool MarketAvailable => _market != null && _store != null && (_store.IsAvailable || StoreDownForBackend) && _progress != null
            && _progress.MaxLevel >= MarketModel.FromMaxLevel;

        /// <summary>
        /// La tienda está encendida (<c>biz_iap_enabled</c>) pero no vende porque no hay backend con el que verificar (F3-5: apagado con
        /// <c>biz_backend_enabled</c>, o un build sin backend): el Mercado se abre y lo dice («Las compras no están disponibles ahora»).
        /// </summary>
        internal bool StoreDownForBackend => _verifiedStore != null && _storeLedger != null && _storeLedger.Config.BizIapEnabled && !BackendReady;

        /// <summary>
        /// Toque en el Cántaro del inicio (05 §5: «≥ 1.000 y toque en el icono»): su tarjeta, con el botón activo desde
        /// <c>eco_piggy_min_buy</c>. No es un takeover: no gasta el cupo de la sesión.
        /// </summary>
        public bool PressPiggy()
        {
            PiggyJar? piggy = _piggy;
            if (piggy == null || _offerCard == null || !OfferReady() || !LobbyClear() || !piggy.VisibleAt((_progress?.MaxLevel ?? 0) + 1))
            {
                return false;
            }

            // 05 §5: «1 tarjeta al llenarse». Vista a mano la primera vez de este llenado, ya no sale sola ni se vuelve a contar.
            bool firstFull = piggy.IsFull && _offers != null && !_offers.PiggyFullCardShown;
            if (firstFull)
            {
                _offers!.MarkShown(OfferKind.PiggyFull, default);
            }

            ShowOfferCard(OfferKind.PiggyFull, StoreCatalog.Piggy, PurchaseContext.Piggy, (_progress?.MaxLevel ?? 0) + 1, false, firstFull);
            return true;
        }

        /// <summary>
        /// Atrás de Android con el Mercado o una tarjeta de oferta delante (enganche B5-bootstrap en <c>PressAndroidBack</c>): la
        /// tarjeta se cierra como «Ahora no»; el Mercado, como su X. Devuelve si cerró algo.
        /// </summary>
        public bool PressMarketBack()
        {
            if (_offerCard != null && _offerCard.Visible)
            {
                _offerCard.Close(false);
                return true;
            }

            if (_market != null && _market.Visible)
            {
                _market.Close();
                return true;
            }

            return false;
        }

        /// <summary>Lo que el Mercado hace cada fotograma (lo llama <see cref="MarketRunner"/>). Sin asignaciones mientras nada cambia.</summary>
        internal void TickMarket(float dtMs)
        {
            if (AccountDeleted)
            {
                return; // Nada tras «Eliminar cuenta y datos»: el perfil ya no existe (GameBootstrap.Account.cs).
            }

            OfferScheduler? offers = _offers;
            LevelFlow? flow = _flow;
            if (offers == null || flow == null)
            {
                return;
            }

            FlowPhase phase = flow.Phase;
            if (phase != _marketPhaseSeen)
            {
                bool leftVictory = _marketPhaseSeen == FlowPhase.VictoryScreen;
                _marketPhaseSeen = phase;
                if (phase == FlowPhase.VictoryScreen)
                {
                    OnCampaignVictoryForMarket(flow);
                }
                else
                {
                    _pendingOffer = false; // se salió de la victoria antes de la tarjeta: esa victoria ya no la enseña
                    if (leftVictory && _offerFromVictory && _offerCard != null && _offerCard.Visible)
                    {
                        // El nivel siguiente ya entró (el avance solo tras la Cosecha doble, D-011): la tarjeta no se queda encima.
                        _offerCard.Dismiss();
                        _offerFromVictory = false;
                    }
                }
            }

            if (_pendingOffer)
            {
                _pendingOfferMs += dtMs;
                if (_pendingOfferMs >= OfferCardDelayMs && phase == FlowPhase.VictoryScreen && VictoryClear())
                {
                    _pendingOffer = false;
                    VictoryOfferContext ctx = _pendingOfferCtx;
                    if (OfferReady() && offers.TakeIf(in ctx, _pendingOfferKind))
                    {
                        ShowTakeover(_pendingOfferKind, in ctx, true);
                    }
                }
            }

            if (_lobbyOffer && LobbyClear())
            {
                // Sin anuncios y la skin salen al volver al inicio (Q-B5-6 [P]), con la sesión de ahora (el cupo es de esta).
                _lobbyOffer = false;
                var ctx = new VictoryOfferContext(_lobbyOfferCtx.WonLevel, _sessions?.Current?.Number ?? 1, _lobbyOfferCtx.World);
                if (OfferReady() && offers.TakeIf(in ctx, _lobbyOfferKind))
                {
                    ShowTakeover(_lobbyOfferKind, in ctx, false);
                }
            }

            int interstitials = _ads?.Ledger.InterstitialsToday ?? 0;
            if (interstitials != _interstitialsTodaySeen)
            {
                // El contador es del día: si volvió a empezar (medianoche), los nuevos son los de hoy.
                int added = interstitials > _interstitialsTodaySeen ? interstitials - _interstitialsTodaySeen : interstitials;
                for (int i = 0; i < added; i++)
                {
                    offers.RecordInterstitialSeen();
                }

                _interstitialsTodaySeen = interstitials;
            }

            SyncMarketIcons();

            // F3-5: una nota de compra cambió (pendiente, rechazada o acreditada tarde) con el Mercado abierto: se rehace.
            MarketScreen? market = _market;
            if (market != null && market.Visible && _services != null && market.NotesShown != _services.PurchaseNotes.Version)
            {
                market.Refresh();
            }
        }

        private void OnCampaignVictoryForMarket(LevelFlow flow)
        {
            if (_flow != flow || !flow.HasLevel)
            {
                return;
            }

            int won = flow.LevelNumber;
            PiggyFill? fill = _piggy?.OnCampaignVictory(won, flow.HarvestTier);
            if (fill.HasValue)
            {
                PiggyFill f = fill.Value;
                if (DevProbe.Enabled)
                {
                    MarketProbe("piggy_fill", "\"level\":" + Num(won) + ",\"added\":" + Num(f.Added) + ",\"seeds\":" + Num(f.Seeds) + ",\"full\":" + (f.BecameFull ? "true" : "false"));
                }
            }

            if (_piggy != null && won + 1 == _piggy.Config.EcoPiggyFromLevel && OfferReady())
            {
                LogPiggy("unlocked", 0); // af_piggy: el Cántaro aparece con el nivel siguiente (08 §8.2); sin tienda no aparece
            }

            OfferScheduler? offers = _offers;
            if (offers == null || !OfferReady())
            {
                return; // sin tienda (biz_iap_enabled, mantenimiento) o con el perfil bloqueado: ninguna tarjeta ni apunte
            }

            if (PrePromptDueAtCampaignVictory(won))
            {
                return; // 06 §8.1: el pre-prompt de push va primero; la Mochila (y cualquier oferta) pasa a la siguiente victoria
            }

            int world = WorldTable.WorldOf(Math.Clamp(won, 1, WorldTable.LastLevel));
            var ctx = new VictoryOfferContext(won, _sessions?.Current?.Number ?? 1, world);
            OfferKind kind = offers.PickAtVictory(in ctx);
            if (kind == OfferKind.None)
            {
                return;
            }

            if (OfferScheduler.ShowsOnVictory(kind))
            {
                _pendingOfferCtx = ctx;
                _pendingOfferKind = kind;
                _pendingOffer = true;
                _pendingOfferMs = 0f;
                return;
            }

            _lobbyOfferCtx = ctx;
            _lobbyOfferKind = kind;
            _lobbyOffer = true;
        }

        // La tienda puede vender ahora: encendida y con el perfil escribible (08 §4.4).
        private bool OfferReady()
        {
            return _store != null && _store.IsAvailable && !ProfileLocked;
        }

        // Una tarjeta que salió sola (takeover): la de la victoria o la del inicio.
        private void ShowTakeover(OfferKind kind, in VictoryOfferContext ctx, bool onVictory)
        {
            string? sku = OfferScheduler.SkuOf(kind, in ctx);
            if (sku == null)
            {
                return;
            }

            ShowOfferCard(kind, sku, OfferScheduler.ContextOf(kind), ctx.WonLevel + 1, true, true);
            _offerFromVictory = onVictory;
        }

        // logFull: af_piggy shown_full, una vez por llenado (la tarjeta que sale sola o el primer toque en el icono con él lleno).
        private void ShowOfferCard(OfferKind kind, string sku, PurchaseContext context, int level, bool takeover, bool logFull)
        {
            OfferCard? card = _offerCard;
            OfferScheduler? offers = _offers;
            IStoreService? store = _store;
            if (card == null || offers == null || store == null || _art == null)
            {
                return;
            }

            StoreProduct product = offers.Ledger.Catalog.Get(sku);
            string body;
            string note = string.Empty;
            Sprite icon = _art.IconSeed;
            Color iconColor = Color.white;
            bool buyEnabled = store.IsAvailable && !ProfileLocked;
            switch (kind)
            {
                case OfferKind.Starter:
                    body = MarketText.StarterContent(product.Content.Seeds, product.Content.Stock.Get(BoosterKind.ExtraCuia), product.Content.Stock.Get(BoosterKind.Hint));
                    int hours = offers.StarterHoursLeft;
                    note = hours > 0 ? MarketText.StarterLeft(hours) : string.Empty;
                    icon = _art.IconCuiaPlus;
                    break;
                case OfferKind.NoAds:
                    body = MarketText.NoAdsContent(product.Content.Seeds);
                    icon = _art.IconVideo;
                    break;
                case OfferKind.PiggyFull:
                    PiggyJar piggy = offers.Piggy;
                    body = MarketText.PiggyContent(piggy.Seeds, piggy.Cap);
                    note = piggy.IsFull ? MarketText.PiggyFull : (piggy.CanBuy ? string.Empty : MarketText.PiggyLocked(piggy.MinBuy));
                    if (note.Length == 0)
                    {
                        note = MarketText.PiggyFills;
                    }

                    buyEnabled &= piggy.CanBuy;
                    icon = _art.Disc;
                    iconColor = AF.Presentation.Palette.TinajaClay;
                    break;
                default:
                    body = MarketText.SkinName(sku);
                    icon = _art.Disc;
                    iconColor = AF.Presentation.CuiaSkin.For(sku).Rim;
                    break;
            }

            LastOfferShown = kind;
            _offerFromVictory = false;
            if (kind == OfferKind.PiggyFull && logFull && offers.Piggy.IsFull)
            {
                LogPiggy("shown_full", offers.Piggy.Seeds);
            }

            card.Present(sku, MarketText.ProductName(sku), icon, iconColor, body, note, store.PriceOf(sku).ToString(), buyEnabled,
                buy => OnOfferCardClosed(kind, sku, context, level, buy), () => !store.IsPurchasing && !ProfileLocked);
            if (buyEnabled && store is AF.Analytics.Adapters.AnalyticsStoreService analytics)
            {
                analytics.OfferShown(sku, context); // af_iap_offer_shown (08 §8.2): solo lo que se puede comprar
            }

            if (DevProbe.Enabled)
            {
                MarketProbe("offer_shown", "\"kind\":\"" + OfferScheduler.IdOf(kind) + "\",\"sku\":\"" + sku + "\",\"takeover\":" + (takeover ? "true" : "false"));
            }
            if (DevProbe.Enabled && _probe != null)
            {
                var ids = new List<string>(3);
                var rects = new List<RectTransform>(3);
                card.VisibleButtons(ids, rects);
                _probe.Layout("offer_card", 0, null, ids.ToArray(), rects.ToArray()); // AF_LAYOUT para jugar con adb
            }
        }

        private void OnOfferCardClosed(OfferKind kind, string sku, PurchaseContext context, int level, bool buy)
        {
            _offerFromVictory = false;
            OfferScheduler? offers = _offers;
            IStoreService? store = _store;
            if (offers == null || store == null)
            {
                return;
            }

            if (!buy)
            {
                offers.RecordDeclined(kind);
                if (DevProbe.Enabled)
                {
                    MarketProbe("offer_declined", "\"kind\":\"" + OfferScheduler.IdOf(kind) + "\"");
                }
                return;
            }

            PurchaseRequest request = kind == OfferKind.PiggyFull
                ? offers.Piggy.Request(context, level)
                : new PurchaseRequest(sku, context, Math.Max(0, level));
            store.Purchase(request, result =>
            {
                offers.Piggy.OnPurchaseResult(result);
                if (result.Status == PurchaseStatus.Cancelled)
                {
                    offers.RecordDeclined(kind); // cancelar la hoja de compra también es no comprar (Sin anuncios: cuenta un rechazo)
                }

                OnMarketPurchased(result);
            });
        }

        // Fin de una compra del Mercado o de una tarjeta: la sonda (AF_STATE purchase), la skin comprada y la analítica.
        private void OnMarketPurchased(PurchaseResult result)
        {
            _probe?.Purchase(result, _wallet);
            if (result.Product.Content.GrantsSkin && result.IsPurchased)
            {
                _analytics?.Log(AnalyticsEvent.Create("af_skin_unlock").Add("skin_id", result.Product.Sku).Add("source", "iap")); // 08 §8.2
                if (_skinService != null && _skinService.Select(result.Product.Sku))
                {
                    OnMarketSkinSelected(result.Product.Sku); // la skin comprada se pone, como la ganada (Q-M8 (a)) [P]
                }
            }

            if (result.Product.Content.PiggySeeds && result.IsPurchased)
            {
                LogPiggy("bought", result.Request.PiggySeeds);
            }

            _iconsPiggySeeds = -1; // el Cántaro del inicio se repinta (el saldo del inicio lo relee LobbyScreen.Tick)
        }

        private void OnMarketSkinSelected(string skinId)
        {
            if (_art != null && _skinService != null)
            {
                _art.CuiaSkin = AF.Presentation.CuiaSkin.For(_skinService.Equipped); // las cuias del próximo nivel ya la llevan
            }

            if (DevProbe.Enabled)
            {
                MarketProbe("skin_selected", "\"skin\":\"" + (_skinService?.Equipped ?? skinId) + "\"");
            }
        }

        // AF_LAYOUT del Mercado al abrirlo o cambiar de pestaña (desarrollo: para jugar con adb).
        private void OnMarketShown()
        {
            if (!DevProbe.Enabled || _probe == null || _market == null)
            {
                return;
            }

            var ids = new List<string>(12);
            var rects = new List<RectTransform>(12);
            _market.VisibleButtons(ids, rects);
            _probe.Layout("market", 0, null, ids.ToArray(), rects.ToArray());
        }

        // af_piggy (08 §8.2): unlocked, shown_full o bought, con las semillas que guarda.
        private void LogPiggy(string state, int seedsInside)
        {
            _analytics?.Log(AnalyticsEvent.Create("af_piggy").Add("state", state).Add("seeds_inside", seedsInside));
        }

        private void OnMarketClosed()
        {
            _services?.PurchaseNotes.ClearFailures(); // F3-5: «No se pudo confirmar la compra» es un aviso de una vez
            MarketProbe("market_closed", null);
        }

        // El inicio a la vista sin nada encima (como la tarjeta del Reto).
        private bool LobbyClear()
        {
            return _app != null && _app.Screen == AppScreen.Lobby && _lobby != null && _lobby.Visible && !_lobby.OverlayVisible
                && !HomeWindowCovered && !MarketCommerceOpen && (_settingsScreen == null || !_settingsScreen.Visible)
                && (_dailyScreen == null || !_dailyScreen.IsOpen) && (_lockedNotice == null || !_lockedNotice.Visible) && !FruitSheetOpen
                && !LeagueOpen; // D-046: la pantalla de la Liga tapa el inicio
        }

        // La victoria de campaña sin nada encima (ni el Mapa, ni la ficha de una fruta, ni un anuncio o la hoja de compra, ni otra
        // tarjeta de oferta) y sin salida en marcha: ni el avance solo tras la Cosecha doble (D-011) ni los 250 ms de «Siguiente»
        // (D-033, punto 9). Si no, la tarjeta gastaría el cupo para quitarse al momento.
        private bool VictoryClear()
        {
            return _app != null && _app.Screen == AppScreen.Level && !HomeWindowCovered && !MarketCommerceOpen && !FruitSheetOpen && !PrePromptCardVisible
                && (_dailyScreen == null || !_dailyScreen.IsOpen) && (_lockedNotice == null || !_lockedNotice.Visible)
                && !VictoryLeaving;
        }

        /// <summary>
        /// La victoria se está yendo: el avance solo tras la Cosecha doble (D-011) o los 250 ms de «Siguiente» (D-033, punto 9).
        /// </summary>
        internal bool VictoryLeaving => VictoryLeavingOverride ?? (_screen != null && (_screen.AutoNextPending || _screen.NextHoldPending));

        /// <summary>Para los tests: fuerza <see cref="VictoryLeaving"/> (null = el de la pantalla).</summary>
        internal static bool? VictoryLeavingOverride { get; set; }

        private void SyncMarketIcons()
        {
            MarketLobbyIcons? icons = _marketIcons;
            PiggyJar? piggy = _piggy;
            if (icons == null || piggy == null || _lobby == null || _progress == null)
            {
                return;
            }

            bool lobby = LobbyClear();
            int max = _progress.MaxLevel;
            bool market = lobby && MarketAvailable;
            bool piggyShown = lobby && OfferReady() && piggy.VisibleAt(max + 1); // sin tienda no hay Cántaro que comprar (D-008)
            int seeds = piggy.Seeds;
            bool full = piggy.IsFull;
            RectTransform settings = _lobby.PiggyAnchorRect; // el Cántaro, bajo Ajustes y los botones de Música y Sonido (D-046)
            bool measured = lobby && (icons.Remeasure() | icons.TargetsMoved(_lobby.HomeTabRect, settings));
            if (lobby == _iconsShown && market == _iconsMarket && piggyShown == _iconsPiggy && seeds == _iconsPiggySeeds && full == _iconsPiggyFull && !measured)
            {
                return;
            }

            _iconsShown = lobby;
            _iconsMarket = market;
            _iconsPiggy = piggyShown;
            _iconsPiggySeeds = seeds;
            _iconsPiggyFull = full;
            if (!lobby)
            {
                icons.Hide();
                return;
            }

            icons.Sync(market, piggyShown, seeds, full, _lobby.HomeTabRect, settings);
            if (DevProbe.Enabled && _probe != null && (market || piggyShown))
            {
                var ids = new List<string>(2);
                var rects = new List<RectTransform>(2);
                icons.VisibleButtons(ids, rects);
                _probe.Layout("lobby_market", 0, null, ids.ToArray(), rects.ToArray()); // las entradas de B5 en el inicio
            }
        }

        private static string MarketTabId(MarketTab tab)
        {
            switch (tab)
            {
                case MarketTab.Boosters: return "boosters";
                case MarketTab.Cuias: return "cuias";
                case MarketTab.NoAds: return "no_ads";
                default: return "seeds";
            }
        }

        private static string Num(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        // AF_STATE {"event":…,"t":…,…} de la sonda de desarrollo (B5): solo en desarrollo y en momentos sueltos (abrir, tarjeta,
        // compra), nunca por fotograma.
        private static void MarketProbe(string evt, string? fields)
        {
            if (!DevProbe.Enabled)
            {
                return;
            }

            long t = (long)(Time.realtimeSinceStartupAsDouble * 1000.0);
            DevProbe.Write("AF_STATE {\"event\":\"" + evt + "\",\"t\":" + t.ToString(CultureInfo.InvariantCulture)
                + (string.IsNullOrEmpty(fields) ? string.Empty : "," + fields) + "}");
        }
    }

    /// <summary>
    /// El cupo de las ofertas sobre la cola de takeovers de B4 (Q-B5-10 (a); nota <c>docs/dev/mercado.md</c> §6): una sola cuenta
    /// de «un pop-up fuera de hitos por sesión» (<c>ui_max_takeovers_per_session</c>) para la Bitácora y las ofertas.
    /// </summary>
    internal sealed class TakeoverQueueBudget : ITakeoverBudget
    {
        private readonly TakeoverQueue _queue;

        public TakeoverQueueBudget(TakeoverQueue queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public bool QuotaLeft => _queue.QuotaLeft;

        public void Spend()
        {
            _queue.MarkShown(TakeoverKind.Offer);
        }
    }
}
