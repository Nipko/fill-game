#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;

namespace AF.Economy
{
    /// <summary>Qué pasó en una llamada a <see cref="StreakService"/> (para la UI y las sondas de <c>docs/dev/crecida-canasta.md</c> §5).</summary>
    [Flags]
    public enum StreakServiceEvents
    {
        /// <summary>Nada.</summary>
        None = 0,

        /// <summary>Cambió el estado de la Crecida (sonda <c>streak_state</c> [P]).</summary>
        StateChanged = 1,

        /// <summary>Se pagó un hito (<c>af_streak_milestone</c>; no el que se salta por la regla de una vez al día).</summary>
        MilestonePaid = 2,

        /// <summary>La Hoja escudo cubrió un fallo duro (<c>af_streak_shield_used</c>).</summary>
        ShieldUsed = 4,

        /// <summary>Fallo duro de campaña que la Crecida contó (sonda <c>streak_fail</c> [P]).</summary>
        StreakFail = 8,

        /// <summary>Se pagó la Canasta de nivel (<c>af_basket_open</c>, Q-C13 (a)).</summary>
        BasketOpened = 16,

        /// <summary>Se aplicó la Cosecha doble sin anuncio del río crecido (04 §2.8; Q-C1 (a)).</summary>
        FreeDoubleHarvest = 32,
    }

    /// <summary>
    /// Lo que devuelve <see cref="StreakService"/>: el resultado de las reglas, la Canasta de nivel, cada transacción con su
    /// resultado de <see cref="Wallet.Apply"/> y los eventos. Se crea uno por fin de nivel (no por fotograma).
    /// </summary>
    public sealed class StreakServiceResult
    {
        private readonly List<TransactionResult> _transactions = new List<TransactionResult>(4);

        internal StreakServiceResult(StreakOutcome streak)
        {
            Streak = streak;
        }

        /// <summary>
        /// Resultado de <see cref="StreakRules.OnVictory"/> o <see cref="StreakRules.OnHardFail"/>; <c>default</c> en un Salto
        /// del Boto (no toca la Crecida, C11). De aquí salen <c>streak_broken</c> y <c>shield_used</c> de <c>af_level_fail</c> (Q-C7)
        /// y el banco <c>streak</c> de TiTino (E33).
        /// </summary>
        public StreakOutcome Streak { get; }

        /// <summary>Canasta de nivel pagada (K1-K9), o <see cref="BasketContents.None"/>.</summary>
        public BasketContents LevelBasket { get; internal set; }

        /// <summary>
        /// La victoria lleva la Cosecha doble sin anuncio del río crecido (Q-C1 (a)): el flujo no ofrece el botón x2 por vídeo
        /// (Q-C9 (a)). Se aplica dentro del servicio; su resultado está en <see cref="DoubleHarvest"/>.
        /// </summary>
        public bool FreeDoubleHarvest => Streak.FreeDoubleHarvest;

        /// <summary>
        /// Resultado de la Cosecha doble del río crecido, o null si no tocaba. En la rama sin el parche <c>S4-wallet</c> (E35)
        /// la cartera la rechaza con <see cref="RejectReason.NotUnlocked"/> si <c>ads_rv_double_harvest_from_level</c> subiera por
        /// encima del nivel (Q-C14); con el parche, nunca.
        /// </summary>
        public TransactionResult? DoubleHarvest { get; internal set; }

        /// <summary>Semillas que entraron por la Cosecha doble del río crecido (<c>source = double_harvest</c>).</summary>
        public int DoubleHarvestSeeds { get; internal set; }

        /// <summary>Semillas que entraron por la Crecida (<c>source = streak</c>: hito 3 y Canasta del hito 6).</summary>
        public int StreakSeeds { get; internal set; }

        /// <summary>Semillas que entraron por la Canasta de nivel (<c>source = basket</c>).</summary>
        public int BasketSeeds { get; internal set; }

        /// <summary>Todo lo que entró en semillas en esta llamada.</summary>
        public int SeedsPaid => DoubleHarvestSeeds + StreakSeeds + BasketSeeds;

        /// <summary>Cada transacción que se pidió a la cartera, en orden, con su resultado.</summary>
        public IReadOnlyList<TransactionResult> Transactions => _transactions;

        /// <summary>La cartera aplicó todas las transacciones (ninguna rechazada).</summary>
        public bool AllApplied { get; private set; } = true;

        /// <summary>Eventos para la UI y las sondas.</summary>
        public StreakServiceEvents Events { get; internal set; }

        /// <summary>Tiene el evento <paramref name="e"/>.</summary>
        public bool Has(StreakServiceEvents e)
        {
            return (Events & e) == e && e != StreakServiceEvents.None;
        }

        internal void Add(TransactionResult result)
        {
            _transactions.Add(result);
            if (!result.IsApplied)
            {
                AllApplied = false;
            }
        }
    }

    /// <summary>
    /// Estado de la Crecida para el HUD y la sonda <c>streak_state</c> [P] (<see cref="StreakService.Peek"/>). Es un valor.
    /// </summary>
    public readonly struct StreakSnapshot
    {
        internal StreakSnapshot(StreakRecord r, bool active, bool freeDouble, bool enabled)
        {
            Wins = r.Wins;
            RiverFull = r.RiverFull;
            ShieldAvailable = r.ShieldAvailable;
            SeedsToday = r.SeedsToday;
            DateLocal = r.DateLocal;
            int mask = 0;
            for (int m = 0; m < StreakRecord.Milestones.Count; m++)
            {
                if (r.HitsToday.Contains(StreakRecord.Milestones[m]))
                {
                    mask |= 1 << m;
                }
            }

            HitsMask = mask;
            Active = active;
            FreeDoubleHarvestNext = freeDouble;
            Enabled = enabled;
        }

        /// <summary>Barra de la Crecida (0-9).</summary>
        public int Wins { get; }

        /// <summary>Río crecido.</summary>
        public bool RiverFull { get; }

        /// <summary>Hoja escudo de hoy disponible.</summary>
        public bool ShieldAvailable { get; }

        /// <summary>Semillas de la Crecida pagadas hoy (contra <c>streak_daily_cap</c>).</summary>
        public int SeedsToday { get; }

        /// <summary>Día local del estado (<c>yyyy-MM-dd</c>), o null.</summary>
        public string? DateLocal { get; }

        /// <summary>Bit <c>i</c> = el hito <c>StreakRecord.Milestones[i]</c> ya se pagó hoy.</summary>
        public int HitsMask { get; }

        /// <summary><c>biz_feature_win_streak</c> encendido (Q-C11: apagado, ni barra ni x2).</summary>
        public bool Enabled { get; }

        /// <summary>La Crecida cuenta en el nivel consultado (campaña, encendida y desde <c>streak_unlock_level</c>): hay barra.</summary>
        public bool Active { get; }

        /// <summary>La próxima victoria en el nivel consultado llevaría la Cosecha doble sin anuncio (Q-C9: sin botón de vídeo).</summary>
        public bool FreeDoubleHarvestNext { get; }

        /// <summary>El hito <paramref name="milestone"/> (3, 6 o 10) ya se pagó hoy.</summary>
        public bool HitPaid(int milestone)
        {
            for (int m = 0; m < StreakRecord.Milestones.Count; m++)
            {
                if (StreakRecord.Milestones[m] == milestone)
                {
                    return (HitsMask & (1 << m)) != 0;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Servicio de la Crecida, la Hoja escudo y la Canasta (B1a, A5; 06 §4.3-§4.4; 05 §2.1 y §2.4; 04 §2.6, §5.2 y §8; nota
    /// <c>docs/dev/crecida-canasta.md</c>): une <see cref="StreakRules"/>, <see cref="BasketRules"/>, el
    /// <see cref="IStreakStore"/>, la <see cref="Wallet"/> y el <see cref="IClock"/>. Lo llaman los enganches del flujo: la
    /// victoria (E30, <see cref="ApplyWin"/>, tras <c>Transaction.Win</c>), el fallo duro (E29, <see cref="ApplyHardFail"/>, tras
    /// <c>Transaction.HardFail</c>) y el Salto del Boto (E34, <see cref="ApplySkip"/>).
    /// <para>
    /// Cada operación empieza con <see cref="IStreakStore.Load"/>, pasa el registro al día local del reloj
    /// (<see cref="StreakRules.RollDay"/>), aplica la regla y, si la Crecida contó el evento, llama a
    /// <see cref="IStreakStore.Save"/> <b>antes</b> de pagar: así el hito queda apuntado antes de que la cartera lo pague (el
    /// requisito de <see cref="IStreakStore"/>). No guarda el registro entre un nivel y otro. Si la cartera rechazara un pago
    /// (desbordamiento), el hito queda cobrado igual: puede pagar de menos, nunca dos veces.
    /// </para>
    /// <para>
    /// Toda mutación de la cartera va por <see cref="Wallet.Apply"/>: el hito 3 y la Canasta del hito 6 con
    /// <c>source = streak</c> (cuentan en el tope: Q-C13), la Canasta de nivel con <c>source = basket</c> (K7), la Cosecha doble
    /// del río crecido con <c>source = double_harvest</c> (C8). El kill switch <c>biz_feature_win_streak</c> (C13, Q-C11) congela
    /// la Crecida (ni suma, ni rompe, ni x2, y el estado guardado se conserva), pero no la Canasta de nivel, que no es de la
    /// Crecida (K1). Se usa desde el hilo principal.
    /// </para>
    /// <para>
    /// <b>No es reentrante:</b> <see cref="ApplyWin"/> y <see cref="ApplySkip"/> reutilizan una lista interna de transacciones
    /// mientras pagan. Un oyente de <see cref="Wallet.Changed"/> (que salta dentro de <see cref="Wallet.Apply"/>) no debe llamar
    /// a este servicio; si necesita reaccionar, que lo haga después de que vuelva la llamada en curso.
    /// </para>
    /// </summary>
    public sealed class StreakService
    {
        private readonly IStreakStore _store;
        private readonly Wallet _wallet;
        private readonly IClock _clock;
        private readonly List<Transaction> _buffer = new List<Transaction>(6);

        /// <summary>Crea el servicio.</summary>
        /// <param name="store">Estado de la Crecida (en el juego, <c>ProfileStreakStore</c> sobre el mismo perfil que la cartera).</param>
        /// <param name="wallet">Cartera.</param>
        /// <param name="clock">Reloj (día local).</param>
        /// <param name="basketUnlockLevel">
        /// <c>live_unlock_level_daily</c> (08 §9; 10): primera Canasta de nivel. Su única fuente es <c>DailyChallengeConfig</c>
        /// (<c>AF.LiveOps</c>), que <c>AF.Economy</c> no ve: lo pasa el arranque.
        /// </param>
        /// <param name="config">Cifras de la Crecida (por defecto, las de 08 §9).</param>
        /// <param name="basket">Cifras de la Canasta (por defecto, las de 08 §9).</param>
        /// <exception cref="InvalidOperationException">Alguna configuración no es válida.</exception>
        public StreakService(
            IStreakStore store, Wallet wallet, IClock clock, int basketUnlockLevel, StreakConfig? config = null,
            BasketConfig? basket = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (basketUnlockLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(basketUnlockLevel), "Los niveles empiezan en 1.");
            }

            BasketUnlockLevel = basketUnlockLevel;
            Config = config ?? StreakConfig.Defaults;
            Basket = basket ?? BasketConfig.Defaults;
            Config.Validate();
            Basket.Validate();
        }

        /// <summary>Cifras de la Crecida.</summary>
        public StreakConfig Config { get; }

        /// <summary>Cifras de la Canasta.</summary>
        public BasketConfig Basket { get; }

        /// <summary><c>live_unlock_level_daily</c>: desde qué nivel hay Canasta de nivel (K1).</summary>
        public int BasketUnlockLevel { get; }

        /// <summary>El almacén tiene un guardado pendiente (<see cref="IStreakStore.PendingSave"/>).</summary>
        public bool PendingSave => _store.PendingSave;

        /// <summary>
        /// Estado de la Crecida en este instante, ya pasado al día local (el HUD, la barra y la sonda <c>streak_state</c>), con
        /// <see cref="StreakSnapshot.Active"/> y <see cref="StreakSnapshot.FreeDoubleHarvestNext"/> para el nivel
        /// <paramref name="level"/>. Lee con <see cref="IStreakStore.Load"/> y no guarda. Para pintar al abrir una pantalla o tras
        /// un evento, no en cada fotograma.
        /// </summary>
        public StreakSnapshot Peek(int level, bool isCampaign)
        {
            StreakRules rules = Rules();
            return new StreakSnapshot(
                rules.Record, rules.IsActive(level, isCampaign), rules.FreeDoubleHarvestOn(level, isCampaign), Config.BizFeatureWinStreak);
        }

        /// <summary>
        /// Victoria en el nivel <paramref name="level"/> (E30; C1-C8 y K1-K8), tras <c>Transaction.Win</c>: aplica
        /// <see cref="StreakRules.OnVictory"/> (hitos, tope y río crecido), guarda, y paga con <see cref="Wallet.Apply"/> la Cosecha
        /// doble del río crecido (decidida con el estado anterior; la victoria del hito 10 no la lleva), el hito y la Canasta de
        /// nivel si este es un <c>10k</c> de campaña superado por primera vez (<paramref name="level"/> &gt;
        /// <paramref name="maxLevelBefore"/>; K8: se deduce de <c>max_level</c>).
        /// </summary>
        /// <param name="level">Nivel ganado (≥ 1).</param>
        /// <param name="isCampaign">Campaña (false en el Reto del río y los eventos: ni Crecida ni Canasta, C1 y Q-C2).</param>
        /// <param name="maxLevelBefore">
        /// <c>max_level</c> antes de esta victoria: el enganche E30 lo lee <b>antes</b> de <c>_progress.SaveWin</c>. Sirve para no
        /// pagar dos veces la Canasta de un nivel rejugado y para el comodín del hito 6
        /// (<c>BasketRules.HitSix(Math.Max(level, maxLevelBefore))</c>, Q-C5 (a)).
        /// </param>
        /// <param name="tier">Etiqueta de la victoria (la Cosecha doble dobla la de esa etiqueta).</param>
        public StreakServiceResult ApplyWin(int level, bool isCampaign, int maxLevelBefore, RewardTier tier)
        {
            StreakRules rules = Rules();
            StreakOutcome outcome = rules.OnVictory(level, isCampaign, maxLevelBefore);
            var result = new StreakServiceResult(outcome);
            if (outcome.Counted)
            {
                _store.Save(rules.Record);
                result.Events |= StreakServiceEvents.StateChanged;
                if (outcome.MilestonePaid)
                {
                    result.Events |= StreakServiceEvents.MilestonePaid;
                }
            }

            _buffer.Clear();
            outcome.AppendTransactions(_buffer, tier);
            for (int i = 0; i < _buffer.Count; i++)
            {
                Transaction t = _buffer[i];
                TransactionResult paid = Pay(result, t);
                if (t.Kind == TransactionKind.DoubleHarvest)
                {
                    result.DoubleHarvest = paid;
                    if (paid.IsApplied)
                    {
                        result.DoubleHarvestSeeds += paid.SeedsDelta;
                        result.Events |= StreakServiceEvents.FreeDoubleHarvest;
                    }
                }
                else if (paid.IsApplied && t.Kind == TransactionKind.Earn)
                {
                    result.StreakSeeds += paid.SeedsDelta;
                }
            }

            if (isCampaign && level > maxLevelBefore)
            {
                PayLevelBasket(result, level);
            }

            _buffer.Clear();
            return result;
        }

        /// <summary>
        /// Fallo duro en el nivel <paramref name="level"/> (E29; 04 §5.2 con D-008; C9-C10), tras <c>Transaction.HardFail</c>:
        /// aplica <see cref="StreakRules.OnHardFail"/> (gasta la Hoja escudo o rompe la barra y apaga el río) y guarda. No paga
        /// nada. El fallo blando, la Marea, el rescate y el resto de C11 no llegan aquí.
        /// </summary>
        public StreakServiceResult ApplyHardFail(int level, bool isCampaign)
        {
            StreakRules rules = Rules();
            StreakOutcome outcome = rules.OnHardFail(level, isCampaign);
            var result = new StreakServiceResult(outcome);
            if (outcome.Counted)
            {
                _store.Save(rules.Record);
                result.Events |= StreakServiceEvents.StateChanged | StreakServiceEvents.StreakFail;
                if (outcome.ShieldUsed)
                {
                    result.Events |= StreakServiceEvents.ShieldUsed;
                }
            }

            return result;
        }

        /// <summary>
        /// Salto del Boto confirmado en el nivel <paramref name="level"/> (E34; K9; 04 §2.6): si es un <c>10k</c> de campaña, paga
        /// su Canasta de nivel con <c>source = basket</c>, igual que la victoria. No toca la Crecida (ni la avanza ni la rompe,
        /// C11): no lee ni guarda el almacén. Fuera de campaña no hace nada. No pide <c>max_level</c>, a diferencia de
        /// <see cref="ApplyWin"/>: el Salto del Boto solo se ofrece en el nivel actual, el siguiente a <c>max_level</c>, así que
        /// el nivel saltado siempre se supera por primera vez.
        /// </summary>
        public StreakServiceResult ApplySkip(int level, bool isCampaign)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Los niveles empiezan en 1.");
            }

            var result = new StreakServiceResult(default);
            if (isCampaign)
            {
                PayLevelBasket(result, level);
            }

            return result;
        }

        private StreakRules Rules()
        {
            StreakRecord record = _store.Load();
            var rules = new StreakRules(record, Config, Basket);
            rules.RollDay(_clock.LocalDateKey());
            return rules;
        }

        private void PayLevelBasket(StreakServiceResult result, int level)
        {
            BasketContents basket = BasketRules.For(level, BasketUnlockLevel, Basket);
            if (basket.IsNone)
            {
                return;
            }

            result.LevelBasket = basket;
            result.Events |= StreakServiceEvents.BasketOpened;
            _buffer.Clear();
            basket.AppendTransactions(_buffer, SeedSource.Basket);
            for (int i = 0; i < _buffer.Count; i++)
            {
                TransactionResult paid = Pay(result, _buffer[i]);
                if (paid.IsApplied && _buffer[i].Kind == TransactionKind.Earn)
                {
                    result.BasketSeeds += paid.SeedsDelta;
                }
            }

            _buffer.Clear();
        }

        private TransactionResult Pay(StreakServiceResult result, Transaction t)
        {
            TransactionResult paid = _wallet.Apply(t);
            result.Add(paid);
            return paid;
        }
    }
}
