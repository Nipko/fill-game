#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;

namespace AF.IAP
{
    /// <summary>
    /// Ofertas que salen solas como tarjeta (05 §5), en el orden de prioridad de <c>offer_scheduler</c>: Mochila de inicio, Sin
    /// anuncios, Cántaro lleno y skin del mundo (el modal de rescate va aparte: es el bloqueo, no un takeover). El Pase Expedición es
    /// de la 1.2 y no está.
    /// </summary>
    public enum OfferKind
    {
        /// <summary>Ninguna.</summary>
        None = 0,

        /// <summary>Mochila de inicio (<c>af_starter</c>): la primera tarjeta o un recordatorio.</summary>
        Starter = 1,

        /// <summary>Sin anuncios (<c>af_noads</c>).</summary>
        NoAds = 2,

        /// <summary>Cántaro lleno (<c>af_piggy</c>, «¡Lleno!»).</summary>
        PiggyFull = 3,

        /// <summary>Skin del mundo (<c>af_skin_m0n</c>), desde el mundo 3.</summary>
        SkinWorld = 4,
    }

    /// <summary>La victoria en que se decide la tarjeta (<see cref="OfferScheduler.PickAtVictory"/>).</summary>
    public readonly struct VictoryOfferContext
    {
        /// <summary>Crea el contexto.</summary>
        /// <param name="wonLevel">Nivel de campaña ganado.</param>
        /// <param name="session">Número de la sesión en curso (1 = la de instalación).</param>
        /// <param name="world">Mundo del nivel ganado (glosario §8).</param>
        /// <param name="firstLevelOfWorld">El nivel ganado es el primero de su mundo («tras la primera victoria del mundo» [P]).</param>
        public VictoryOfferContext(int wonLevel, int session, int world, bool firstLevelOfWorld)
        {
            WonLevel = wonLevel;
            Session = session;
            World = world;
            FirstLevelOfWorld = firstLevelOfWorld;
        }

        /// <summary>Nivel ganado.</summary>
        public int WonLevel { get; }

        /// <summary>Sesión en curso.</summary>
        public int Session { get; }

        /// <summary>Mundo del nivel ganado.</summary>
        public int World { get; }

        /// <summary>Es el primer nivel de su mundo.</summary>
        public bool FirstLevelOfWorld { get; }
    }

    /// <summary>
    /// <c>offer_scheduler</c> de 05 §5 (bloque B5), sin motor y sin asignaciones al consultarlo:
    /// <list type="bullet">
    /// <item><description><b>Cuándo:</b> solo en la victoria de campaña (las tarjetas van «en la pantalla de victoria» o «tras» ella;
    /// 05 §4.1 y §5); nunca en la primera sesión (06 §2.1, regla 1; 05 §5) ni con la tienda apagada (<c>biz_iap_enabled</c>) o el
    /// perfil bloqueado (08 §4.4).</description></item>
    /// <item><description><b>Cupo:</b> como mucho <c>ui_max_takeovers_per_session</c> (1) por sesión (<see cref="ITakeoverBudget"/>,
    /// compartido con la Bitácora de B4).</description></item>
    /// <item><description><b>Prioridad</b> (<see cref="OfferKind"/>): Mochila → Sin anuncios → Cántaro lleno → skin del mundo.</description></item>
    /// <item><description><b>Mochila de inicio</b> (05 §4.1 y §5): primera tarjeta en la primera victoria con nivel ≥ 12 en una
    /// sesión ≥ 2 (<c>eco_starter_first_show</c>), que arranca las <c>eco_starter_timer_h</c> horas [P] (Q-B5-3); recordatorios en la
    /// victoria del 15 y del 19 [P] (Q-B5-4); desaparece al ganar el 20 (<c>eco_starter_window_levels</c>), al vencer el temporizador o
    /// al comprarla (1 por cuenta).</description></item>
    /// <item><description><b>Sin anuncios</b> (05 §5): tras el 3.er intersticial visto o en el día 2 (<c>eco_noads_card_trigger</c>), 1
    /// tarjeta al día y, tras 3 rechazos, solo en el Mercado; la tarjeta no sale antes de <c>rescue_min_level</c> (05 §10: ninguna
    /// oferta de pago antes del 31 salvo la Mochila) [P] (Q-B5-2).</description></item>
    /// <item><description><b>Cántaro lleno:</b> 1 tarjeta por llenado (05 §5).</description></item>
    /// <item><description><b>Skin del mundo:</b> desde el mundo 3 (nivel 41), tras la primera victoria del mundo [P], 1 por mundo y
    /// nunca si ya se tiene la skin (ganada o comprada); con <see cref="SkinSalesEnabled"/> (apagado hasta el arte, Q-B5-1).</description></item>
    /// </list>
    /// También dice qué enseña el Mercado (<see cref="StarterInMarket"/>, <see cref="NoAdsInMarket"/>…).
    /// </summary>
    public sealed class OfferScheduler
    {
        /// <summary>Primera sesión con ofertas de pago: ninguna en la de instalación (06 §2.1, regla 1; 05 §5).</summary>
        public const int FirstOfferSession = 2;

        private readonly OffersState _state;
        private readonly StoreLedger _ledger;
        private readonly IClock _clock;
        private readonly ITakeoverBudget _budget;
        private readonly DateTime _installedAtUtc;
        private readonly FirstShowRule _firstShow;
        private readonly NoAdsTriggerRule _noAdsTrigger;

        /// <summary>Crea el planificador.</summary>
        /// <param name="state">Registro de <c>piggy</c> y <c>offers</c>.</param>
        /// <param name="ledger">Registro de la tienda: lo que el jugador ya tiene, el catálogo y <c>biz_iap_enabled</c>.</param>
        /// <param name="piggy">El Cántaro.</param>
        /// <param name="clock">Reloj (día local de la tarjeta Sin anuncios y temporizador de la Mochila).</param>
        /// <param name="installedAtUtc"><c>created_at</c> del perfil: el día 1 de <c>eco_noads_card_trigger</c>.</param>
        /// <param name="budget">Cupo de takeovers de la sesión.</param>
        /// <param name="config">Cifras; por defecto, las de 08 §9.</param>
        /// <exception cref="InvalidOperationException">La configuración no es válida.</exception>
        public OfferScheduler(
            OffersState state, StoreLedger ledger, PiggyJar piggy, IClock clock, DateTime installedAtUtc, ITakeoverBudget budget, OffersConfig? config = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            Piggy = piggy ?? throw new ArgumentNullException(nameof(piggy));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _budget = budget ?? throw new ArgumentNullException(nameof(budget));
            _installedAtUtc = installedAtUtc;
            Config = config ?? OffersConfig.Defaults;
            Config.Validate();
            _firstShow = Config.StarterFirstShow;
            _noAdsTrigger = Config.NoAdsTrigger;
        }

        /// <summary>Cifras.</summary>
        public OffersConfig Config { get; }

        /// <summary>El Cántaro.</summary>
        public PiggyJar Piggy { get; }

        /// <summary>Registro de la tienda.</summary>
        public StoreLedger Ledger => _ledger;

        /// <summary>Compras bloqueadas (perfil bloqueado, 08 §4.4): ninguna tarjeta.</summary>
        public bool PurchasesBlocked { get; set; }

        /// <summary>Venta de skins (fila del Mercado y tarjeta) [P]; por defecto <see cref="OfferRules.SkinSalesEnabledDefault"/>.</summary>
        public bool SkinSalesEnabled { get; set; } = OfferRules.SkinSalesEnabledDefault;

        /// <summary>Primer nivel con tarjetas de pago que no son la Mochila: <c>rescue_min_level</c> (31; 05 §10) [P].</summary>
        public int PaidCardFromLevel => _ledger.Wallet.Config.RescueMinLevel;

        /// <summary>El registro guardado falló en el último cambio.</summary>
        public bool PendingSave => _state.PendingSave;

        /// <summary>Día desde la instalación en la fecha local, con D1 el de la instalación (para <c>eco_noads_card_trigger</c>).</summary>
        public int DayIndex
        {
            get
            {
                DateTime installLocal = (_installedAtUtc + _clock.LocalOffset).Date;
                int days = (_clock.LocalDate() - installLocal).Days;
                return days < 0 ? 1 : days + 1;
            }
        }

        /// <summary>Ya compró la Mochila (1 por cuenta).</summary>
        public bool StarterOwned => _ledger.IsOwned(_ledger.Catalog.Get(StoreCatalog.Starter));

        /// <summary>Ya tiene Sin anuncios.</summary>
        public bool NoAdsOwned => _ledger.IsOwned(_ledger.Catalog.Get(StoreCatalog.NoAds));

        /// <summary>Vence la Mochila (primera tarjeta + <c>eco_starter_timer_h</c>), o null si aún no se enseñó.</summary>
        public DateTime? StarterExpiresAt
        {
            get
            {
                DateTime? seen = _state.Record.StarterFirstSeenAt;
                return seen.HasValue ? seen.Value + TimeSpan.FromHours(Config.EcoStarterTimerH) : (DateTime?)null;
            }
        }

        /// <summary>Horas enteras que le quedan a la Mochila, redondeadas hacia arriba (0 si su temporizador no ha empezado o venció).</summary>
        public int StarterHoursLeft
        {
            get
            {
                DateTime? expires = StarterExpiresAt;
                if (!expires.HasValue)
                {
                    return 0;
                }

                double hours = (expires.Value - _clock.UtcNow).TotalHours;
                return hours <= 0 ? 0 : (int)Math.Ceiling(hours);
            }
        }

        /// <summary>
        /// La Mochila sigue a la venta con <paramref name="maxLevel"/>: no comprada, con el nivel en juego dentro de
        /// <c>eco_starter_window_levels</c> (desde el 10; desaparece al ganar el 20) y sin el temporizador vencido.
        /// </summary>
        public bool StarterAvailable(int maxLevel)
        {
            if (StarterOwned || maxLevel >= Config.StarterWindowTo || maxLevel + 1 < Config.StarterWindowFrom)
            {
                return false;
            }

            DateTime? expires = StarterExpiresAt;
            return !expires.HasValue || _clock.UtcNow < expires.Value;
        }

        /// <summary>El Mercado enseña la Mochila (05 §4.1: «en el Mercado desde el nivel 10», ventana 10-20).</summary>
        public bool StarterInMarket(int maxLevel)
        {
            return StarterAvailable(maxLevel);
        }

        /// <summary>El Mercado enseña Sin anuncios: no comprado y desde el nivel 22 (05 §4.1).</summary>
        public bool NoAdsInMarket(int maxLevel)
        {
            return !NoAdsOwned && maxLevel + 1 >= OfferRules.NoAdsMarketFromLevel;
        }

        /// <summary>
        /// El Mercado enseña Canoas infinitas 1 h: desde <c>rescue_min_level</c> (31), el debut de las Canoas (04 §7; 05 §10: ninguna
        /// oferta de Canoas antes del 31).
        /// </summary>
        public bool CanoesInMarket(int maxLevel)
        {
            return maxLevel + 1 >= _ledger.Wallet.Config.RescueMinLevel;
        }

        /// <summary>El Mercado enseña el Cántaro (desde <c>eco_piggy_from_level</c>, como el icono).</summary>
        public bool PiggyInMarket(int maxLevel)
        {
            return Piggy.VisibleAt(maxLevel + 1);
        }

        /// <summary>
        /// La skin <paramref name="milestone"/> se puede comprar en el Mercado con <paramref name="maxLevel"/>: con la venta de skins
        /// encendida, sin tenerla y con el jugador ya en su mundo o después (05 §4.1: «Mercado al entrar al mundo»).
        /// </summary>
        public bool SkinPurchasable(SkinMilestone milestone, int maxLevel, int currentWorld)
        {
            return SkinSalesEnabled && currentWorld >= milestone.World && !_ledger.IsOwned(_ledger.Catalog.Get(milestone.SkinId))
                && maxLevel < milestone.Level;
        }

        /// <summary>
        /// La tarjeta que tocaría en la victoria <paramref name="ctx"/>, sin cambiar nada (ni el registro ni el cupo); o
        /// <see cref="OfferKind.None"/>.
        /// </summary>
        public OfferKind PickAtVictory(in VictoryOfferContext ctx)
        {
            if (PurchasesBlocked || !_ledger.Config.BizIapEnabled || ctx.Session < FirstOfferSession || !_budget.QuotaLeft)
            {
                return OfferKind.None;
            }

            if (StarterDue(in ctx))
            {
                return OfferKind.Starter;
            }

            if (NoAdsDue(in ctx))
            {
                return OfferKind.NoAds;
            }

            if (PiggyFullDue(in ctx))
            {
                return OfferKind.PiggyFull;
            }

            return SkinDue(in ctx) ? OfferKind.SkinWorld : OfferKind.None;
        }

        /// <summary>
        /// En la victoria <paramref name="ctx"/>: <see cref="PickAtVictory"/> y, si sale una, la apunta (<see cref="MarkShown"/>) y gasta
        /// el cupo de la sesión. Quien llama enseña la tarjeta.
        /// </summary>
        public OfferKind TakeAtVictory(in VictoryOfferContext ctx)
        {
            OfferKind kind = PickAtVictory(in ctx);
            if (kind != OfferKind.None)
            {
                MarkShown(kind, in ctx);
                _budget.Spend();
            }

            return kind;
        }

        /// <summary>
        /// La tarjeta <paramref name="kind"/> se enseñó en <paramref name="ctx"/>: la Mochila arranca su temporizador (o apunta el
        /// recordatorio), Sin anuncios apunta el día, el Cántaro su llenado y la skin su mundo. No gasta cupo. Guarda.
        /// </summary>
        public void MarkShown(OfferKind kind, in VictoryOfferContext ctx)
        {
            OffersRecord r = _state.Record;
            switch (kind)
            {
                case OfferKind.Starter:
                    if (!r.StarterFirstSeenAt.HasValue)
                    {
                        r.StarterFirstSeenAt = _clock.UtcNow;
                    }
                    else if (!r.StarterReminders.Contains(ctx.WonLevel))
                    {
                        r.StarterReminders.Add(ctx.WonLevel);
                    }

                    break;
                case OfferKind.NoAds:
                    r.NoAdsCardDate = _clock.LocalDateKey();
                    break;
                case OfferKind.PiggyFull:
                    r.PiggyFullCardShown = true;
                    break;
                case OfferKind.SkinWorld:
                    if (!r.SkinCardWorlds.Contains(ctx.World))
                    {
                        r.SkinCardWorlds.Add(ctx.World);
                    }

                    break;
                default:
                    return;
            }

            _state.Persist();
        }

        /// <summary>
        /// El jugador cerró la tarjeta <paramref name="kind"/> sin comprar: con Sin anuncios cuenta un rechazo (al 3.º, solo Mercado;
        /// 05 §5). Las demás no cuentan rechazos (no se repiten por sí mismas). Guarda si cambió.
        /// </summary>
        public void RecordDeclined(OfferKind kind)
        {
            if (kind != OfferKind.NoAds)
            {
                return;
            }

            _state.Record.NoAdsDeclines++;
            _state.Persist();
        }

        /// <summary>
        /// Se vio entero un intersticial: cuenta para <c>eco_noads_card_trigger</c> (con tope en el que hace falta). Guarda si
        /// cambió.
        /// </summary>
        public void RecordInterstitialSeen()
        {
            OffersRecord r = _state.Record;
            if (r.InterstitialsSeen >= _noAdsTrigger.Interstitials)
            {
                return;
            }

            r.InterstitialsSeen++;
            _state.Persist();
        }

        /// <summary>El SKU de la tarjeta <paramref name="kind"/> en la victoria <paramref name="ctx"/>, o null.</summary>
        public static string? SkuOf(OfferKind kind, in VictoryOfferContext ctx)
        {
            switch (kind)
            {
                case OfferKind.Starter: return StoreCatalog.Starter;
                case OfferKind.NoAds: return StoreCatalog.NoAds;
                case OfferKind.PiggyFull: return StoreCatalog.Piggy;
                case OfferKind.SkinWorld: return SkinRewards.TryForWorld(ctx.World, out SkinMilestone m) ? m.SkinId : null;
                default: return null;
            }
        }

        /// <summary>El <c>context</c> de <c>af_iap_offer_shown</c> de la tarjeta <paramref name="kind"/> (08 §8.2).</summary>
        public static PurchaseContext ContextOf(OfferKind kind)
        {
            switch (kind)
            {
                case OfferKind.Starter: return PurchaseContext.StarterWindow;
                case OfferKind.NoAds: return PurchaseContext.NoAdsPrompt;
                case OfferKind.PiggyFull: return PurchaseContext.Piggy;
                default: return PurchaseContext.Shop; // la skin no tiene contexto propio en 08 §8.2 [P]
            }
        }

        /// <summary>Id de la sonda: <c>none</c>, <c>starter</c>, <c>noads</c>, <c>piggy_full</c> o <c>skin_world</c>.</summary>
        public static string IdOf(OfferKind kind)
        {
            switch (kind)
            {
                case OfferKind.Starter: return "starter";
                case OfferKind.NoAds: return "noads";
                case OfferKind.PiggyFull: return "piggy_full";
                case OfferKind.SkinWorld: return "skin_world";
                default: return "none";
            }
        }

        private bool StarterDue(in VictoryOfferContext ctx)
        {
            if (!StarterAvailable(ctx.WonLevel))
            {
                return false;
            }

            OffersRecord r = _state.Record;
            if (!r.StarterFirstSeenAt.HasValue)
            {
                return ctx.WonLevel >= _firstShow.MinLevel && ctx.Session >= _firstShow.MinSession;
            }

            IReadOnlyList<int> reminders = OfferRules.StarterReminderLevels;
            for (int i = 0; i < reminders.Count; i++)
            {
                if (reminders[i] == ctx.WonLevel)
                {
                    return !r.StarterReminders.Contains(ctx.WonLevel);
                }
            }

            return false;
        }

        private bool NoAdsDue(in VictoryOfferContext ctx)
        {
            OffersRecord r = _state.Record;
            if (NoAdsOwned || ctx.WonLevel < PaidCardFromLevel || r.NoAdsDeclines >= OfferRules.NoAdsDeclinesToStop)
            {
                return false;
            }

            if (r.NoAdsCardDate != null && string.Equals(r.NoAdsCardDate, _clock.LocalDateKey(), StringComparison.Ordinal))
            {
                return false; // OfferRules.NoAdsCardsPerDay = 1
            }

            return r.InterstitialsSeen >= _noAdsTrigger.Interstitials || DayIndex >= _noAdsTrigger.Day;
        }

        private bool PiggyFullDue(in VictoryOfferContext ctx)
        {
            return ctx.WonLevel >= PaidCardFromLevel && Piggy.IsFull && !_state.Record.PiggyFullCardShown;
        }

        private bool SkinDue(in VictoryOfferContext ctx)
        {
            if (!SkinSalesEnabled || ctx.WonLevel < OfferRules.SkinCardFromLevel || !ctx.FirstLevelOfWorld
                || !SkinRewards.TryForWorld(ctx.World, out SkinMilestone milestone))
            {
                return false;
            }

            return !_state.Record.SkinCardWorlds.Contains(ctx.World) && !_ledger.IsOwned(_ledger.Catalog.Get(milestone.SkinId));
        }
    }
}
