#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Economy;
using AF.Platform;
using AF.Solver.Murky;
using AF.Solver.Search;

namespace AF.Boosters
{
    /// <summary>
    /// Respuesta de Morpho, en el orden de 04 §2.3 y 02 T5 (D-019, Consecuencias; propuesta de la tinaja b.2 «Morpho»). Solo
    /// <see cref="Pour"/> cobra: las otras tres son honestas y gratis (research/01 Especificación 14).
    /// </summary>
    public enum HintAnswer
    {
        /// <summary>1. Un vertido (seguro en todas las realidades con dosis ocultas): se resalta origen y destino y se cobra.</summary>
        Pour = 0,

        /// <summary>
        /// 2. «Desde aquí no hay salida: la Marea o la orilla» (con Deshacer y Volver a la orilla): el solver demuestra que ninguna
        /// realidad tiene solución, también con una sola. No cobra.
        /// </summary>
        NoExit = 1,

        /// <summary>
        /// 3. «No veo un vertido seguro: la luciérnaga ilumina una cuia» («… la tinaja» con <see cref="HintResult.Tinaja"/>):
        /// alguna realidad tiene solución, pero ningún vertido es seguro en todas. Nunca con una sola realidad. No cobra.
        /// </summary>
        NoSafePour = 2,

        /// <summary>
        /// 4. «No encuentro un camino desde aquí» (03 §5): se agotó <c>hint_budget_nodes</c> o <c>hint_timeout_ms</c> antes de
        /// decidir. No cobra y no hay Pista parcial.
        /// </summary>
        NotFound = 3,
    }

    /// <summary>Con qué buscó Morpho (03 §5 y §8).</summary>
    public enum HintSolver
    {
        /// <summary><see cref="AStarSolver"/> desde el estado actual: tableros sin dosis ocultas ni tinaja.</summary>
        AStar = 0,

        /// <summary><see cref="SafeMoves"/> sobre las realidades: Aguas turbias con ocultas y cualquier tablero con tinaja (M13).</summary>
        SafeMoves = 1,
    }

    /// <summary>Estado del botón «Pista» (04 §2.3; la ranura la pinta MO2).</summary>
    public enum HintState
    {
        /// <summary>Antes de <c>hint_unlock_level</c> (12) la ranura no existe.</summary>
        Hidden = 0,

        /// <summary>El nivel no declara <c>hint_supported</c>: botón desactivado con «La morpho no conoce este tramo todavía» (04 §2.3 b).</summary>
        Unsupported = 1,

        /// <summary>No queda vertido legal o el tablero no acepta toques (en Aguas estancadas Morpho no está: 04 §2.3).</summary>
        Disabled = 2,

        /// <summary>Morpho está buscando en su hilo.</summary>
        Thinking = 3,

        /// <summary>Enfriamiento de <see cref="BoosterConfig.HintCooldownMs"/> tras la última respuesta [P].</summary>
        Cooldown = 4,

        /// <summary>Se puede pedir: con el stock o pagando.</summary>
        Ready = 5,
    }

    /// <summary>Lo que hace un sondeo de la consulta en curso (<see cref="MorphoBooster.Poll"/>).</summary>
    public enum MorphoPoll
    {
        /// <summary>No hay consulta (o se abandonó).</summary>
        Idle = 0,

        /// <summary>El hilo de trabajo sigue buscando.</summary>
        Running = 1,

        /// <summary>Terminó: la respuesta sale en el parámetro de salida.</summary>
        Done = 2,
    }

    /// <summary>Lo que muestra el botón «Pista»: stock «+N», precio o «Te faltan {n}» (04 §1.1 y §2.3).</summary>
    public readonly struct HintOffer
    {
        /// <summary>Crea la oferta.</summary>
        public HintOffer(HintState state, int stock, int price, int missing, int cooldownMs)
        {
            State = state;
            Stock = stock;
            Price = price;
            Missing = missing;
            CooldownMs = cooldownMs;
        }

        /// <summary>Estado.</summary>
        public HintState State { get; }

        /// <summary>Morphos en el stock (se gastan antes que las semillas).</summary>
        public int Stock { get; }

        /// <summary>Precio en semillas (<c>hint_seed_price</c>, 100).</summary>
        public int Price { get; }

        /// <summary>Semillas que faltan si no hay stock (0 si alcanza): el precio en rojo, «Te faltan {n}» o el video <c>rv_hint</c>.</summary>
        public int Missing { get; }

        /// <summary>Milisegundos de enfriamiento que quedan (0 si no hay).</summary>
        public int CooldownMs { get; }

        /// <summary>Se paga con el stock.</summary>
        public bool FromStock => Stock > 0;

        /// <summary>Se puede tocar: lista y con stock o saldo.</summary>
        public bool CanUse => State == HintState.Ready && (FromStock || Missing == 0);
    }

    /// <summary>Respuesta de una consulta de Morpho, con lo que mide la sonda <c>AF_PERF morpho</c>.</summary>
    public readonly struct HintResult
    {
        /// <summary>Crea la respuesta.</summary>
        public HintResult(HintAnswer answer, Pour move, bool tinaja, HintSolver solver, int realities, long nodes, double solveMs, bool cancelled, bool charged)
        {
            Answer = answer;
            Move = move;
            Tinaja = tinaja;
            Solver = solver;
            Realities = realities;
            Nodes = nodes;
            SolveMs = solveMs;
            Cancelled = cancelled;
            Charged = charged;
        }

        /// <summary>La respuesta (orden de 04 §2.3).</summary>
        public HintAnswer Answer { get; }

        /// <summary>El vertido que se propone (solo con <see cref="HintAnswer.Pour"/>; índices reales del tablero).</summary>
        public Pour Move { get; }

        /// <summary>El tablero lleva tinaja: la respuesta 3 dice «… la tinaja» en vez de «… una cuia» (D-019).</summary>
        public bool Tinaja { get; }

        /// <summary>Con qué se buscó.</summary>
        public HintSolver Solver { get; }

        /// <summary>Realidades de las capas ocultas (1 sin ocultas).</summary>
        public int Realities { get; }

        /// <summary>Estados expandidos por la consulta (las que salieron del memo de <see cref="SafeMoves"/> no cuentan).</summary>
        public long Nodes { get; }

        /// <summary>Milisegundos de reloj real que tardó el hilo de trabajo.</summary>
        public double SolveMs { get; }

        /// <summary>La búsqueda se cortó por <c>hint_timeout_ms</c> (K2): siempre con <see cref="HintAnswer.NotFound"/>.</summary>
        public bool Cancelled { get; }

        /// <summary>Se cobró (solo con <see cref="HintAnswer.Pour"/>, cuando el flujo lo entrega).</summary>
        public bool Charged { get; }

        /// <summary>Trae un vertido.</summary>
        public bool HasMove => Answer == HintAnswer.Pour;

        /// <summary>La misma respuesta, cobrada.</summary>
        public HintResult AsCharged()
        {
            return new HintResult(Answer, Move, Tinaja, Solver, Realities, Nodes, SolveMs, Cancelled, true);
        }
    }

    /// <summary>
    /// Morpho · Pista en el cliente (GDD 04 §2.3; 03 §5 y §8; 08 §9 <c>hint_budget_nodes</c> = 40.000, <c>hint_weight</c> = 2,
    /// <c>hint_timeout_ms</c> = 150, <c>hint_unlock_level</c> = 12 y <c>hint_seed_price</c> = 100; D-019, Consecuencias; K2):
    /// busca el vertido que se propone desde el estado actual en un hilo de trabajo, sin bloquear el hilo principal, y da la
    /// respuesta en el orden de <see cref="HintAnswer"/>. Sin motor y sin <c>AF.Levels</c>: el tablero entra como
    /// <see cref="BoardState"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Solver:</b> <see cref="SafeMoves"/> (presupuesto por realidad, memo por estado canónico, T4 con el
    /// tablero inicial del nivel) si el tablero tiene dosis ocultas o tinaja (<see cref="UsesSafeMoves"/>); si no,
    /// <see cref="AStarSolver"/> y el primer vertido de su solución. Las opciones salen de <see cref="OptionsFor"/>: presupuesto
    /// y peso de 08 §9, el modo de vertido y la regla de fútiles de la partida y, con tinaja, sin poda
    /// (<see cref="SolverOptions.ForLevel"/>).</description></item>
    /// <item><description><b>Hilo:</b> cada consulta va al <see cref="ThreadPool"/>; el solver y <see cref="SafeMoves"/> son
    /// de esta instancia y solo los usa una consulta a la vez (<see cref="Start"/> no lanza otra mientras la anterior, aunque se
    /// haya abandonado, siga en el hilo), así que hay una instancia de solver por hilo que busca. Los <see cref="BoardState"/>
    /// son inmutables; la <see cref="SolverCancellation"/> (una por consulta) es lo único que toca el otro hilo.</description></item>
    /// <item><description><b>Corte por tiempo:</b> el hilo principal, al sondear (<see cref="Poll"/>), marca la bandera cuando
    /// el <see cref="IClock"/> pasa <c>hint_timeout_ms</c> desde <see cref="Start"/> (o va hacia atrás); el núcleo para en como mucho
    /// <see cref="SolverCancellation.PollInterval"/> expansiones y la respuesta es <see cref="HintAnswer.NotFound"/>. Si la
    /// búsqueda terminó antes de ver la bandera, su respuesta es completa y vale.</description></item>
    /// <item><description><b>Cobro:</b> esta clase nunca toca la cartera. Cobra el flujo (<c>LevelFlow.Hint</c>) con
    /// <see cref="Pay"/>, y solo al entregar una respuesta <see cref="HintAnswer.Pour"/>: nunca cobra sin dar un
    /// vertido.</description></item>
    /// </list>
    /// </remarks>
    public sealed class MorphoBooster
    {
        private readonly BoosterConfig _config;
        private readonly Action? _workerHook;
        private readonly AStarSolver _solver = new AStarSolver();
        private readonly SafeMoves _safe = new SafeMoves();
        private readonly WaitCallback _work;

        private IClock _clock;

        // Petición en curso: la escribe el hilo principal antes de encolarla y la lee el hilo de trabajo.
        private BoardState? _state;
        private BoardState? _reference;
        private SolverOptions _options;
        private int _levelKey;

        // Solo del hilo de trabajo (o de Compute, que no corre a la vez): el nivel del memo de SafeMoves.
        private int _memoLevel = int.MinValue;

        // Solo del hilo principal.
        private SolverCancellation? _cancellation;
        private DateTime _startedUtc;
        private bool _busy;
        private bool _abandoned;

        // Respuesta: la escribe el hilo de trabajo antes de marcar _finished (escritura volátil) y la lee el principal después.
        private HintResult _result;
        private Exception? _error;
        private int _finished = 1;
        private int _computing;

        /// <summary>Crea Morpho.</summary>
        /// <param name="config">Cifras de 08 §9 (presupuesto, peso, corte por tiempo).</param>
        /// <param name="clock">Reloj del hilo principal para <c>hint_timeout_ms</c>.</param>
        /// <param name="workerHook">Solo tests: se ejecuta en el hilo de trabajo antes de cada búsqueda (p. ej. para retenerla y
        /// comprobar que el hilo principal no espera).</param>
        public MorphoBooster(BoosterConfig config, IClock clock, Action? workerHook = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _workerHook = workerHook;
            _work = Work;
        }

        /// <summary>Reloj con el que el hilo principal mide <c>hint_timeout_ms</c>.</summary>
        public IClock Clock
        {
            get => _clock;
            set => _clock = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Hay una consulta en el hilo de trabajo (también una abandonada que aún no ha parado).</summary>
        public bool IsBusy => _busy && Volatile.Read(ref _finished) == 0;

        /// <summary>La consulta en curso o la última se cortó por tiempo desde el hilo principal.</summary>
        public bool TimeoutRequested { get; private set; }

        /// <summary>Excepción de la última consulta en el hilo de trabajo (null si no hubo); la respuesta fue <see cref="HintAnswer.NotFound"/>.</summary>
        public Exception? LastError { get; private set; }

        /// <summary>La ranura «Pista» existe en <paramref name="level"/> (<c>hint_unlock_level</c>).</summary>
        public static bool IsUnlocked(int level, BoosterConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return level >= config.HintUnlockLevel;
        }

        /// <summary>
        /// Regalo del tutorial (04 §2.3: «al cargar el nivel 12 TiTino presenta el botón … y se acreditan 3 Morphos al stock»;
        /// tarea TU1b): al cargar el nivel de desbloqueo (<c>hint_unlock_level</c>) aún no superado (<paramref name="maxLevel"/>
        /// &lt; nivel), si no se regaló ya en este nivel (<paramref name="alreadyGifted"/>, que <c>AF.GameFlow</c> guarda en
        /// <c>level_state</c> también al salir al inicio y que vuelve al reanudar). El mismo patrón que la Cuia extra del 8 y la
        /// Luciérnaga del 100, salvo que no mira el stock: son <see cref="BoosterConfig.HintTutorialUnits"/> regalados, no «uno si
        /// no tienes» [P] (un Morpho de una compra del Mercado antes del 12 no se come el regalo). Así salir y volver a entrar,
        /// reintentar o reabrir la app no lo repite.
        /// </summary>
        public static bool ShouldGift(int level, int maxLevel, bool alreadyGifted, Wallet? wallet, BoosterConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return wallet != null && !alreadyGifted && level == config.HintUnlockLevel && maxLevel < level;
        }

        /// <summary>
        /// Estado del botón en <paramref name="level"/> (04 §2.3): <paramref name="hintSupported"/> del nivel,
        /// <paramref name="hasLegalPour"/> (PLAYING con algún vertido legal), <paramref name="thinking"/>, el enfriamiento que
        /// queda y el stock y el saldo de <paramref name="wallet"/>.
        /// </summary>
        public static HintOffer Evaluate(int level, bool hintSupported, bool hasLegalPour, bool thinking, int cooldownMs, Wallet? wallet, BoosterConfig config)
        {
            int stock = wallet?.Stock.Get(BoosterKind.Hint) ?? 0;
            int price = Price(wallet);
            int missing = stock > 0 ? 0 : (wallet == null ? price : wallet.Missing(price));
            int cooldown = cooldownMs < 0 ? 0 : cooldownMs;
            HintState state;
            if (!IsUnlocked(level, config))
            {
                state = HintState.Hidden;
            }
            else if (!hintSupported)
            {
                state = HintState.Unsupported;
            }
            else if (thinking)
            {
                state = HintState.Thinking;
            }
            else if (!hasLegalPour)
            {
                state = HintState.Disabled;
            }
            else if (cooldown > 0)
            {
                state = HintState.Cooldown;
            }
            else
            {
                state = HintState.Ready;
            }

            return new HintOffer(state, stock, price, missing, cooldown);
        }

        /// <summary>Precio en semillas (<c>hint_seed_price</c>; sin cartera, el default de 08 §9).</summary>
        public static int Price(Wallet? wallet)
        {
            return wallet?.Price(SeedSink.Hint) ?? EconomyConfig.Defaults.HintSeedPrice;
        }

        /// <summary>Se puede cobrar un Morpho ahora: hay stock, o el saldo llega al precio.</summary>
        public static bool CanPay(Wallet wallet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            return wallet.Stock.Has(BoosterKind.Hint) || wallet.Missing(wallet.Price(SeedSink.Hint)) == 0;
        }

        /// <summary>
        /// Cobra un Morpho: una unidad del stock si la hay (regalada en el 12, de la Canasta o del video <c>rv_hint</c>); si no,
        /// <c>hint_seed_price</c> semillas. Lo llama el flujo solo al entregar un vertido. Si se rechaza, no cambia nada.
        /// </summary>
        public static TransactionResult Pay(Wallet wallet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            return wallet.Stock.Has(BoosterKind.Hint)
                ? wallet.Apply(Transaction.UseStock(BoosterKind.Hint))
                : wallet.Apply(Transaction.Buy(SeedSink.Hint));
        }

        /// <summary>
        /// Morpho busca con <see cref="SafeMoves"/>: el tablero tiene dosis ocultas (Aguas turbias, o la tinaja) o lleva tinaja
        /// (M13: también vacía; la respuesta 3 sigue siendo imposible con una sola realidad). Si no, con <see cref="AStarSolver"/>.
        /// </summary>
        public static bool UsesSafeMoves(BoardState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return state.HiddenCount > 0 || SolverOptions.RequiresUnprunedSearch(state);
        }

        /// <summary>Respuesta de Morpho para un resultado de <see cref="SafeMoves"/> (orden de D-019).</summary>
        public static HintAnswer AnswerOf(SafeMovesOutcome outcome)
        {
            switch (outcome)
            {
                case SafeMovesOutcome.Found:
                    return HintAnswer.Pour;
                case SafeMovesOutcome.NoExit:
                    return HintAnswer.NoExit;
                case SafeMovesOutcome.SomeRealitySolvable:
                    return HintAnswer.NoSafePour;
                default:
                    // Presupuesto o tiempo agotados, tablero ya ganado o realidades rechazadas: no hay nada que prometer.
                    return HintAnswer.NotFound;
            }
        }

        /// <summary>Respuesta de Morpho para un resultado de <see cref="AStarSolver"/> sin ocultas (una sola realidad).</summary>
        public static HintAnswer AnswerOf(SolveStatus status, int moveCount)
        {
            switch (status)
            {
                case SolveStatus.Solved:
                    return moveCount > 0 ? HintAnswer.Pour : HintAnswer.NotFound;
                case SolveStatus.Unsolvable:
                    return HintAnswer.NoExit;
                default:
                    return HintAnswer.NotFound;
            }
        }

        /// <summary>
        /// Opciones de Morpho para <paramref name="state"/> (sin bandera de corte): <c>hint_budget_nodes</c> (por realidad con
        /// <see cref="SafeMoves"/>), <c>hint_weight</c>, la regla de fútiles y el modo de vertido de la partida y, con tinaja, sin
        /// poda (<see cref="SolverOptions.ForLevel"/>).
        /// </summary>
        public SolverOptions OptionsFor(BoardState state, PourMode mode, bool futileRuleEnabled)
        {
            return new SolverOptions(_config.HintBudgetNodes, _config.SolverWeight, futileRuleEnabled, mode).ForLevel(state);
        }

        /// <summary>
        /// Lanza una consulta en el hilo de trabajo desde <paramref name="state"/> y vuelve enseguida. False si aún corre la
        /// anterior (también una abandonada) o si no se pudo encolar.
        /// </summary>
        /// <param name="state">Tablero actual.</param>
        /// <param name="t4Reference">Tablero de referencia de T4 para las realidades (el inicial del nivel); null =
        /// <paramref name="state"/>.</param>
        /// <param name="mode">Modo de vertido de la partida.</param>
        /// <param name="futileRuleEnabled">Regla de fútiles de la partida.</param>
        /// <param name="levelKey">Nivel: al cambiar, el memo de <see cref="SafeMoves"/> se vacía (no crece entre niveles).</param>
        public bool Start(BoardState state, BoardState? t4Reference, PourMode mode, bool futileRuleEnabled, int levelKey)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (IsBusy || Volatile.Read(ref _computing) != 0)
            {
                return false;
            }

            var cancellation = new SolverCancellation(); // una por consulta: un corte tardío nunca alcanza a la siguiente
            _state = state;
            _reference = t4Reference ?? state;
            _options = OptionsFor(state, mode, futileRuleEnabled).WithCancellation(cancellation);
            _levelKey = levelKey;
            _cancellation = cancellation;
            _startedUtc = _clock.UtcNow;
            _abandoned = false;
            TimeoutRequested = false;
            LastError = null;
            _busy = true;
            Volatile.Write(ref _finished, 0);
            if (!ThreadPool.QueueUserWorkItem(_work))
            {
                _busy = false;
                Volatile.Write(ref _finished, 1);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sondeo del hilo principal (cada fotograma mientras haya consulta): si terminó, <see cref="MorphoPoll.Done"/> con la
        /// respuesta en <paramref name="result"/>; si no, y el reloj pasó <c>hint_timeout_ms</c> desde <see cref="Start"/> (o fue
        /// hacia atrás), marca
        /// el corte (la respuesta llegará como <see cref="HintAnswer.NotFound"/>) y devuelve <see cref="MorphoPoll.Running"/>.
        /// Nunca espera al hilo de trabajo.
        /// </summary>
        public MorphoPoll Poll(out HintResult result)
        {
            result = default;
            if (!_busy)
            {
                return MorphoPoll.Idle;
            }

            if (Volatile.Read(ref _finished) != 0)
            {
                _busy = false;
                LastError = _error;
                if (_abandoned)
                {
                    return MorphoPoll.Idle;
                }

                result = _result;
                return MorphoPoll.Done;
            }

            if (_abandoned)
            {
                return MorphoPoll.Idle;
            }

            SolverCancellation cancellation = _cancellation!;
            if (!cancellation.IsRequested && TimedOut(ElapsedMs()))
            {
                cancellation.Request();
                TimeoutRequested = true;
            }

            return MorphoPoll.Running;
        }

        /// <summary>
        /// Abandona la consulta en curso (el estado cambió: vertido, Marea, Cuia extra, nivel nuevo…): marca el corte y su
        /// respuesta se descarta. La siguiente <see cref="Start"/> espera a que el hilo pare (como mucho
        /// <see cref="SolverCancellation.PollInterval"/> expansiones).
        /// </summary>
        public void Abandon()
        {
            if (!_busy)
            {
                return;
            }

            _abandoned = true;
            _cancellation?.Request();
            if (Volatile.Read(ref _finished) != 0)
            {
                _busy = false;
            }
        }

        /// <summary>
        /// La consulta en el hilo que llama (tests y herramientas): la misma búsqueda y las mismas respuestas que la del hilo de
        /// trabajo, con <paramref name="cancellation"/> opcional. No se puede llamar mientras corre una consulta en el hilo.
        /// </summary>
        /// <exception cref="InvalidOperationException">Hay una consulta en el hilo de trabajo o en otra llamada.</exception>
        public HintResult Compute(BoardState state, BoardState? t4Reference, PourMode mode, bool futileRuleEnabled, int levelKey = 0, SolverCancellation? cancellation = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (IsBusy || Interlocked.Exchange(ref _computing, 1) != 0)
            {
                throw new InvalidOperationException("Morpho ya está buscando: una consulta a la vez por instancia.");
            }

            try
            {
                return Search(state, t4Reference ?? state, OptionsFor(state, mode, futileRuleEnabled).WithCancellation(cancellation), levelKey);
            }
            finally
            {
                Volatile.Write(ref _computing, 0);
            }
        }

        // Hilo de trabajo: una consulta. Una excepción (no debería haberla) responde «No encuentro» y queda en LastError.
        private void Work(object? unused)
        {
            HintResult result;
            Exception? error = null;
            try
            {
                _workerHook?.Invoke();
                result = Search(_state!, _reference!, _options, _levelKey);
            }
            catch (Exception e)
            {
                error = e;
                result = new HintResult(HintAnswer.NotFound, default, false, HintSolver.AStar, 0, 0, 0, false, false);
            }

            _result = result;
            _error = error;
            Volatile.Write(ref _finished, 1);
        }

        private HintResult Search(BoardState state, BoardState reference, SolverOptions options, int levelKey)
        {
            long started = Stopwatch.GetTimestamp();
            bool tinaja = SolverOptions.RequiresUnprunedSearch(state);
            HintAnswer answer;
            Pour move = default;
            int realities;
            long nodes;
            bool cancelled;
            HintSolver solver;
            if (UsesSafeMoves(state))
            {
                if (levelKey != _memoLevel)
                {
                    _safe.ClearMemo();
                    _memoLevel = levelKey;
                }

                solver = HintSolver.SafeMoves;
                long before = _safe.Expanded;
                RealityStatus status = _safe.Find(state, options, reference);
                nodes = _safe.Expanded - before;
                realities = _safe.RealityCount;
                cancelled = _safe.WasCancelled;
                answer = status == RealityStatus.Ok ? AnswerOf(_safe.Outcome) : HintAnswer.NotFound;
                if (answer == HintAnswer.Pour)
                {
                    move = _safe.GetMove(0);
                }
            }
            else
            {
                solver = HintSolver.AStar;
                SolveResult solved = _solver.Solve(state, options);
                nodes = solved.Expanded;
                realities = 1;
                cancelled = solved.WasCancelled;
                answer = AnswerOf(solved.Status, solved.MoveCount);
                if (answer == HintAnswer.Pour)
                {
                    move = solved.GetMove(0);
                }
            }

            double ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            return new HintResult(answer, move, tinaja, solver, realities, nodes, ms, cancelled, false);
        }

        private double ElapsedMs()
        {
            return (_clock.UtcNow - _startedUtc).TotalMilliseconds;
        }

        // Con el reloj del dispositivo hacia atrás desde Start, el tiempo sale negativo y no llegaría a hint_timeout_ms hasta
        // que el reloj lo alcanzara (la búsqueda solo pararía al agotar el presupuesto): se corta enseguida.
        private bool TimedOut(double elapsedMs)
        {
            return elapsedMs < 0 || elapsedMs >= _config.HintTimeoutMs;
        }
    }
}
