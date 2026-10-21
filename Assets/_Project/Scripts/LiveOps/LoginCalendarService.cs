#nullable enable
using System;
using System.Collections.Generic;
using AF.Economy;
using AF.Platform;

namespace AF.LiveOps
{
    /// <summary>
    /// Foto de la Bitácora para la libreta y el inicio (<see cref="LoginCalendarService.Peek"/>): el ciclo, la casilla que toca, el
    /// estado de hoy y las tres opciones ya resueltas para el jugador y lo que va fijo.
    /// </summary>
    public sealed class LoginCalendarSnapshot
    {
        internal LoginCalendarSnapshot(int cycle, int nextDay, LoginCalendarState state, LoginCalendarCell cell, LoginCalendarReward[] options, BasketContents basket, string? frameId)
        {
            Cycle = cycle;
            NextDay = nextDay;
            State = state;
            Cell = cell;
            Options = options;
            Basket = basket;
            FrameId = frameId;
        }

        /// <summary>Ciclo en curso (pie «Ciclo {c}»).</summary>
        public int Cycle { get; }

        /// <summary>La casilla que toca sellar (1-30). Las anteriores del ciclo llevan sello.</summary>
        public int NextDay { get; }

        /// <summary>Estado de hoy.</summary>
        public LoginCalendarState State { get; }

        /// <summary>La casilla de <see cref="NextDay"/>.</summary>
        public LoginCalendarCell Cell { get; }

        /// <summary>Sus tres opciones para el jugador, en orden semillas / comodín / mezcla (comodines bloqueados ya sustituidos).</summary>
        public IReadOnlyList<LoginCalendarReward> Options { get; }

        /// <summary>La Canasta o la Canasta grande fija de la casilla (o <see cref="BasketContents.None"/>), como se pagaría hoy.</summary>
        public BasketContents Basket { get; }

        /// <summary>El marco de país fijo de la casilla (día 30), o null.</summary>
        public string? FrameId { get; }

        /// <summary>Hay casilla que sellar hoy.</summary>
        public bool Ready => State == LoginCalendarState.Ready;
    }

    /// <summary>Resultado de <see cref="LoginCalendarService.Claim"/> (para la libreta, la sonda y los tests).</summary>
    public sealed class LoginCalendarClaim
    {
        internal LoginCalendarClaim(int day, int cycle, LoginCalendarChoice choice, LoginCalendarReward reward, int rewardValue, BasketContents basket, string? frameId, int daysMissedBefore, int seedsPaid, bool allApplied)
        {
            Day = day;
            Cycle = cycle;
            Choice = choice;
            Reward = reward;
            RewardValue = rewardValue;
            Basket = basket;
            FrameId = frameId;
            DaysMissedBefore = daysMissedBefore;
            SeedsPaid = seedsPaid;
            AllApplied = allApplied;
        }

        /// <summary>Casilla sellada (1-30): <c>day</c> de <c>af_calendar_claim</c>.</summary>
        public int Day { get; }

        /// <summary>Ciclo de esa casilla: <c>cycle</c> [P].</summary>
        public int Cycle { get; }

        /// <summary>Opción tocada: <c>choice</c> [P].</summary>
        public LoginCalendarChoice Choice { get; }

        /// <summary>Lo recibido por la opción (sin lo fijo).</summary>
        public LoginCalendarReward Reward { get; }

        /// <summary><c>reward_value</c>: semillas-equivalentes de la opción (D-033: «el valor equivalente»), sin lo fijo [P].</summary>
        public int RewardValue { get; }

        /// <summary><c>reward_type</c> [P] (<see cref="LoginCalendarRules.RewardType"/>).</summary>
        public string RewardType => LoginCalendarRules.RewardType(Reward);

        /// <summary>La Canasta (o la grande) pagada con la casilla, o <see cref="BasketContents.None"/>.</summary>
        public BasketContents Basket { get; }

        /// <summary>El marco de país ganado (día 30), o null.</summary>
        public string? FrameId { get; }

        /// <summary><c>days_missed_before</c>.</summary>
        public int DaysMissedBefore { get; }

        /// <summary>Semillas que entraron (opción y Canasta).</summary>
        public int SeedsPaid { get; }

        /// <summary>La cartera aplicó todas las transacciones (una rechazada sería un error del cliente: se registra).</summary>
        public bool AllApplied { get; }
    }

    /// <summary>
    /// La Bitácora del río en el juego (06 §4.2; D-033, añadido del 2026-10-20, puntos 1-7): une las reglas
    /// (<see cref="LoginCalendarRules"/>), la tabla (<see cref="LoginCalendarConfig.EcoCalendarTableV2"/>), el almacén
    /// (<see cref="ILoginCalendarStore"/>), el reloj y la cartera (<see cref="Wallet.Apply"/>). Sin motor.
    /// <list type="bullet">
    /// <item><description><see cref="MarkPlayed"/>: al terminar un nivel (campaña o Reto, ganado o con fallo duro). Solo escribe la
    /// primera vez del día.</description></item>
    /// <item><description><see cref="Peek"/> / <see cref="StateFor"/>: para la libreta, la tarjeta del inicio (punto si
    /// <see cref="LoginCalendarState.Ready"/>) y la cola de takeovers.</description></item>
    /// <item><description><see cref="Claim"/>: un toque en una de las tres tarjetas sella sin confirmación (punto 4). Primero se
    /// guarda el avance de <c>next_day</c> (y el marco), después se paga con la cartera; en el juego, el almacén y la cartera son
    /// el mismo perfil y quien llama lo envuelve en un lote de guardado, así que sale una sola escritura (punto 6).</description></item>
    /// </list>
    /// La Canasta de los días 7 y 21 es <c>eco_basket_seeds</c> + el comodín de la próxima Canasta de nivel
    /// (<see cref="BasketRules.HitSix"/>, la misma regla que la del hito 6 de la Crecida: Q-B7 [P]); la del 30, la Canasta grande.
    /// Todas sus semillas entran con <see cref="SeedSource.Calendar"/>. No es reentrante ni seguro entre hilos.
    /// </summary>
    public sealed class LoginCalendarService
    {
        private readonly ILoginCalendarStore _store;
        private readonly Wallet _wallet;
        private readonly IClock _clock;
        private readonly LoginCalendarConfig _config;
        private readonly BasketConfig _basket;
        private readonly int _unlockLevel;
        private readonly List<Transaction> _pay = new List<Transaction>(8);

        /// <summary>Crea el servicio.</summary>
        /// <param name="store">El bloque <c>calendar</c>.</param>
        /// <param name="wallet">La cartera del mismo perfil.</param>
        /// <param name="clock">Reloj (día local).</param>
        /// <param name="unlockLevel"><c>live_unlock_level_daily</c> (de <see cref="DailyChallengeConfig"/>).</param>
        /// <param name="config">Cifras (por defecto, <see cref="LoginCalendarConfig.Defaults"/>).</param>
        /// <param name="basket">Cifras de la Canasta (por defecto, <see cref="BasketConfig.Defaults"/>).</param>
        public LoginCalendarService(ILoginCalendarStore store, Wallet wallet, IClock clock, int unlockLevel, LoginCalendarConfig? config = null, BasketConfig? basket = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? LoginCalendarConfig.Defaults;
            _config.Validate();
            _basket = basket ?? BasketConfig.Defaults;
            _basket.Validate();
            _unlockLevel = unlockLevel;
        }

        /// <summary>Cifras.</summary>
        public LoginCalendarConfig Config => _config;

        /// <summary>Nivel de desbloqueo (<c>live_unlock_level_daily</c>).</summary>
        public int UnlockLevel => _unlockLevel;

        /// <summary>El registro vivo (no lo cambies fuera del servicio).</summary>
        public LoginCalendarRecord Record => _store.Load();

        /// <summary>El almacén (para reintentar un guardado pendiente).</summary>
        public ILoginCalendarStore Store => _store;

        /// <summary>La última casilla sellada en esta sesión, o null.</summary>
        public LoginCalendarClaim? LastClaim { get; private set; }

        /// <summary>Se selló una casilla (después de pagarla).</summary>
        public event Action<LoginCalendarClaim>? Claimed;

        /// <summary>La Bitácora existe para el jugador con <paramref name="maxLevel"/>.</summary>
        public bool IsAvailable(int maxLevel)
        {
            return LoginCalendarRules.IsAvailable(_config.BizFeatureLoginCalendar, maxLevel, _unlockLevel);
        }

        /// <summary>Estado de hoy (sin asignaciones mientras no cambia el día).</summary>
        public LoginCalendarState StateFor(int maxLevel)
        {
            return LoginCalendarRules.StateOf(_store.Load(), _clock.LocalDateKey(), IsAvailable(maxLevel));
        }

        /// <summary>
        /// Apunta que hoy se terminó un nivel (D-033, punto 5: «al terminar el primer nivel del día, campaña o Reto, se gane o se
        /// pierda»). Solo guarda la primera vez del día. Devuelve si cambió.
        /// </summary>
        public bool MarkPlayed()
        {
            LoginCalendarRecord record = _store.Load();
            if (!LoginCalendarRules.MarkPlayed(record, _clock.LocalDateKey()))
            {
                return false;
            }

            _store.Save(record);
            return true;
        }

        /// <summary>
        /// La libreta de hoy para el jugador con <paramref name="maxLevel"/> y lo desbloqueado <paramref name="unlocks"/>; el mundo
        /// <paramref name="world"/> (1-9, el de <c>max_level</c> + 1) fija el marco del día 30.
        /// </summary>
        public LoginCalendarSnapshot Peek(int maxLevel, LoginCalendarUnlocks unlocks, int world)
        {
            LoginCalendarRecord record = _store.Load();
            record.Sanitize();
            LoginCalendarState state = LoginCalendarRules.StateOf(record, _clock.LocalDateKey(), IsAvailable(maxLevel));
            LoginCalendarCell cell = _config.EcoCalendarTableV2.Cell(record.NextDay);
            var options = new LoginCalendarReward[LoginCalendarTable.Choices];
            LoginCalendarRules.ResolveOptions(cell, unlocks, options);
            return new LoginCalendarSnapshot(record.Cycle, record.NextDay, state, cell, options, BasketFor(cell, maxLevel), FrameFor(cell, world));
        }

        /// <summary>
        /// Sella la casilla de hoy con la opción <paramref name="choice"/> (D-033, punto 4: un toque, sin confirmación): guarda el
        /// avance y, si toca, el marco, y paga la opción y lo fijo con la cartera. Null si hoy no se puede sellar
        /// (<see cref="LoginCalendarState.Ready"/>): nunca se sella sola ni dos veces el mismo día.
        /// </summary>
        public LoginCalendarClaim? Claim(LoginCalendarChoice choice, int maxLevel, LoginCalendarUnlocks unlocks, int world)
        {
            if (choice < LoginCalendarChoice.Seeds || choice > LoginCalendarChoice.Mix)
            {
                throw new ArgumentOutOfRangeException(nameof(choice));
            }

            LoginCalendarRecord record = _store.Load();
            record.Sanitize();
            string today = _clock.LocalDateKey();
            if (LoginCalendarRules.StateOf(record, today, IsAvailable(maxLevel)) != LoginCalendarState.Ready)
            {
                return null;
            }

            int day = record.NextDay;
            int cycle = record.Cycle;
            LoginCalendarCell cell = _config.EcoCalendarTableV2.Cell(day);
            LoginCalendarReward reward = LoginCalendarRules.ResolveOption(cell, choice, unlocks);
            BasketContents basket = BasketFor(cell, maxLevel);
            string? frame = FrameFor(cell, world);
            int missed = LoginCalendarRules.DaysMissedBefore(record, _clock.LocalDate());

            // 1) El avance (y el derecho del marco), guardado antes de pagar: una casilla nunca se cobra dos veces.
            LoginCalendarRules.Advance(record, today);
            if (frame != null && !record.Frames.Contains(frame))
            {
                record.Frames.Add(frame);
            }

            _store.Save(record);

            // 2) El pago, con la cartera del mismo perfil (en el juego, dentro del lote de quien llama: una escritura).
            _pay.Clear();
            if (reward.Seeds > 0)
            {
                _pay.Add(Transaction.Earn(SeedSource.Calendar, reward.Seeds));
            }

            AppendStock(_pay, reward.Stock);
            basket.AppendTransactions(_pay, SeedSource.Calendar);
            int seedsPaid = 0;
            bool all = true;
            for (int i = 0; i < _pay.Count; i++)
            {
                TransactionResult result = _wallet.Apply(_pay[i]);
                if (result.IsApplied)
                {
                    seedsPaid += result.SeedsDelta;
                }
                else
                {
                    all = false;
                }
            }

            _pay.Clear();
            var claim = new LoginCalendarClaim(day, cycle, choice, reward, reward.Value(_config.EcoCalendarLockedBoosterSeeds), basket, frame, missed, seedsPaid, all);
            LastClaim = claim;
            Claimed?.Invoke(claim);
            return claim;
        }

        private BasketContents BasketFor(LoginCalendarCell cell, int maxLevel)
        {
            if ((cell.Extras & LoginCalendarExtras.GrandBasket) != 0)
            {
                return new BasketContents(BasketKind.Grand, 0, BasketRules.GrandSeeds, BasketRules.GrandStock);
            }

            if ((cell.Extras & LoginCalendarExtras.Basket) != 0)
            {
                BasketContents next = BasketRules.HitSix(maxLevel, _basket); // eco_basket_seeds + el comodín de la próxima Canasta de nivel (Q-B7)
                return new BasketContents(BasketKind.Normal, next.Level, next.Seeds, next.Stock);
            }

            return BasketContents.None;
        }

        private static string? FrameFor(LoginCalendarCell cell, int world)
        {
            return (cell.Extras & LoginCalendarExtras.Frame) != 0 ? LoginCalendarRules.FrameId(Math.Max(1, world)) : null;
        }

        private static void AppendStock(List<Transaction> into, StockDelta stock)
        {
            for (int k = 0; k < BoosterKinds.Count; k++)
            {
                int units = stock.Get((BoosterKind)k);
                if (units > 0)
                {
                    into.Add(Transaction.GrantStock((BoosterKind)k, units));
                }
            }
        }
    }
}
