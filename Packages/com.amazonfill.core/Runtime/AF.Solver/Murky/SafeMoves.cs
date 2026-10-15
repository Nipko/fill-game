#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;

namespace AF.Solver.Murky
{
    /// <summary>
    /// Resultado de <see cref="SafeMoves.Find(BoardState, SolverOptions, BoardState, bool, int)"/> y, si no hay vertido
    /// seguro, por qué: el orden de respuestas de Morpho y de la pista por inactividad (propuesta de la tinaja, b.2
    /// «Morpho», y c.2; vale también para Aguas turbias, c.10) y la causa que separa el informe del pipeline (c.3).
    /// </summary>
    public enum SafeMovesOutcome
    {
        /// <summary>Hay al menos un vertido seguro en todas las realidades (respuesta 1: Morpho lo propone y cobra).</summary>
        Found = 0,

        /// <summary>
        /// Ninguna realidad tiene solución desde aquí, también con una sola realidad (respuesta 2: «Desde aquí no hay salida:
        /// la Marea o la orilla», con Deshacer y Volver a la orilla; no cobra).
        /// </summary>
        NoExit = 1,

        /// <summary>
        /// Alguna realidad tiene solución, pero ningún vertido es seguro en todas (respuesta 3: «No veo un vertido seguro: la
        /// luciérnaga ilumina…», sin prometer nada; no cobra). Nunca con una sola realidad: si esa realidad tiene solución, el
        /// primer vertido de su solución es seguro, así que solo puede faltar presupuesto.
        /// </summary>
        SomeRealitySolvable = 2,

        /// <summary>
        /// Se agotó el presupuesto antes de decidir entre las respuestas 1-3 (respuesta 4: «No encuentro un camino desde
        /// aquí»; no cobra), o se cortó por tiempo (<see cref="SafeMoves.WasCancelled"/>): el cliente marca la
        /// <see cref="SolverCancellation"/> al pasar <c>hint_timeout_ms</c> (08 §9; K2), porque el núcleo no lee relojes.
        /// </summary>
        BudgetExceeded = 3,

        /// <summary>El tablero ya es victoria (R12): no hay nada que proponer.</summary>
        Won = 4,

        /// <summary>La enumeración de realidades falló (<see cref="SafeMoves.Status"/> no es <see cref="RealityStatus.Ok"/>).</summary>
        RealitiesRejected = 5,
    }

    /// <summary>
    /// Vertidos seguros de Aguas turbias (02 §11 T5 y <c>validar_turbio</c>; 03 §8): los vertidos útiles
    /// (<c>MOVIMIENTOS</c> de 03 §5, <see cref="SolverMoves"/>) que dejan el tablero resoluble en <b>todas</b> las realidades
    /// coherentes con lo visible (<see cref="RealityEnumerator"/>). Lo usan el pipeline (<see cref="SafePath"/>), Morpho y la
    /// pista por inactividad; si no hay ninguno, <see cref="Count"/> = 0: «ninguno», Morpho no cobra (T5; no existe Pista
    /// parcial).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Un vertido legal mueve lo mismo en todas las realidades (legalidad, cantidad y <c>futil</c> solo
    /// miran lo visible: las dosis ocultas cortan la racha, R6, y una cuia con ocultas nunca está sellada ni es fútil como
    /// origen), así que se calcula sobre el tablero y se aplica con <see cref="BoardRules"/> en cada realidad.</description></item>
    /// <item><description>Resoluble = A* con el presupuesto de <see cref="SolverOptions"/> por realidad (03 §8: 20.000 nodos en
    /// el pipeline, <see cref="ForPipeline"/>; 40.000 para Morpho, <c>hint_budget_nodes</c>, que pasa el cliente). Agotar el
    /// presupuesto cuenta como no demostrado: el vertido no es seguro.</description></item>
    /// <item><description><b>Memo por estado canónico</b> (03 §8): cada estado ya resuelto (canónico exacto, con
    /// <see cref="BoardCanon"/>, sin colisiones) guarda su par; vale entre pasos, entre llamadas y entre niveles mientras no
    /// cambien las opciones. Un único <see cref="AStarSolver"/> reutilizado.</description></item>
    /// <item><description><b>Por qué no hay vertido seguro</b> (<see cref="Outcome"/>; propuesta de la tinaja, b.2 «Morpho» y
    /// c.2): con <see cref="Count"/> = 0 se resuelve además el estado actual en cada realidad (con el memo) y se separan
    /// «ninguna realidad tiene solución» (<see cref="SafeMovesOutcome.NoExit"/>), «alguna la tiene, pero ningún vertido es
    /// seguro en todas» (<see cref="SafeMovesOutcome.SomeRealitySolvable"/>: cada candidato tiene una realidad demostrada
    /// irresoluble) y «se agotó el presupuesto antes de decidir» (<see cref="SafeMovesOutcome.BudgetExceeded"/>). Con
    /// vertidos seguros no se busca nada más, y el resultado de <see cref="Count"/> y de los vertidos no cambia.</description></item>
    /// <item><description><b>Tinaja</b> (M13): la premisa se cumple (sacar solo depende de que su cuia tenga hueco y siempre
    /// mueve 1; la suelta depende del recuento visible). En un nivel con tinaja las opciones van sin poda de fútiles
    /// (<see cref="SolverOptions.ForLevel"/> y <see cref="ForPipeline(PourMode, BoardState)"/>; b.2 R14).</description></item>
    /// <item><description><b>Corte por tiempo</b> (K2; 08 §9 <c>hint_timeout_ms</c>, «al agotarse no se cobra»; 04 §2.3): con
    /// <see cref="SolverOptions.Cancellation"/> marcada, <see cref="Find(BoardState, SolverOptions, BoardState, bool, int)"/>
    /// para (al empezar, tras enumerar las realidades, o en la búsqueda A* en curso, como mucho
    /// <see cref="SolverCancellation.PollInterval"/> expansiones después) con <see cref="Count"/> = 0,
    /// <see cref="Outcome"/> = <see cref="SafeMovesOutcome.BudgetExceeded"/> y <see cref="WasCancelled"/>: no hay Pista
    /// parcial, aunque ya hubiera vertidos seguros demostrados. El orden de respuestas no cambia. Lo cortado no entra en el
    /// memo; lo que se terminó antes del corte sí, y vale para la siguiente llamada. <see cref="TrueParAfter"/> usa la bandera
    /// de la última <see cref="Find(BoardState, SolverOptions, BoardState, bool, int)"/>: cortado, da
    /// <see cref="BudgetExceeded"/> y <see cref="WasCancelled"/> sin tocar <see cref="Count"/> ni <see cref="Outcome"/> (la
    /// respuesta 4 se lee solo tras <see cref="Find(BoardState, SolverOptions, BoardState, bool, int)"/>).</description></item>
    /// <item><description>Sin asignaciones una vez calentada, salvo el crecimiento del memo. No es segura entre hilos: una
    /// instancia por hilo (con su propio <see cref="AStarSolver"/> y su memo); los <see cref="BoardState"/> son inmutables
    /// y se pueden compartir, y la <see cref="SolverCancellation"/> es lo único que toca otro hilo.</description></item>
    /// </list>
    /// </remarks>
    public sealed class SafeMoves
    {
        /// <summary>Presupuesto del pipeline por realidad (03 §8; tabla de 03 §5: «20.000 por realidad», w = 1).</summary>
        public const int PipelineRealityBudget = 20000;

        /// <summary>El par de un estado sin solución (irresoluble).</summary>
        public const int Unsolvable = -1;

        /// <summary>El par de un estado en el que A* agotó el presupuesto (no demostrado).</summary>
        public const int BudgetExceeded = -2;

        private readonly AStarSolver _solver = new AStarSolver();
        private readonly RealityEnumerator _realities = new RealityEnumerator();
        private readonly SolverMove[] _candidates = new SolverMove[SolverMoves.MaxMoves];
        private readonly SolverMove[] _safe = new SolverMove[SolverMoves.MaxMoves];
        private readonly int[] _failPar = new int[SolverMoves.MaxMoves];
        private readonly int[] _failReality = new int[SolverMoves.MaxMoves];
        private readonly Cuia[] _state = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _child = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _canon = new Cuia[BoardState.MaxCuias];
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly CanonMemo _memo = new CanonMemo();
        private SolverOptions _options;
        private SolverOptions _memoOptions;
        private int _width;
        private int _count;
        private SafeMovesOutcome _outcome = SafeMovesOutcome.RealitiesRejected;
        private bool _cancelled;

        /// <summary>Opciones del pipeline en el modo <paramref name="mode"/>: 20.000 nodos por realidad, w = 1, con fútiles (03 §8).</summary>
        public static SolverOptions ForPipeline(PourMode mode)
        {
            return new SolverOptions(PipelineRealityBudget, 1, true, mode);
        }

        /// <summary>
        /// Como <see cref="ForPipeline(PourMode)"/> para el nivel <paramref name="level"/>: sin poda de fútiles si lleva Tinaja
        /// (<see cref="SolverOptions.RequiresUnprunedSearch"/>; propuesta de la tinaja, b.2 R14 y c.2); en cualquier otro
        /// nivel, las mismas opciones.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="level"/> es null.</exception>
        public static SolverOptions ForPipeline(PourMode mode, BoardState level)
        {
            return ForPipeline(mode).ForLevel(level);
        }

        /// <summary>Vertidos seguros de la última llamada (0 = ninguno, T5; el motivo, en <see cref="Outcome"/>).</summary>
        public int Count => _count;

        /// <summary>
        /// Resultado de la última llamada: <see cref="SafeMovesOutcome.Found"/> si <see cref="Count"/> &gt; 0 y, si no, por qué
        /// no hay vertido seguro (orden de respuestas de Morpho; propuesta de la tinaja, b.2 y c.2).
        /// </summary>
        public SafeMovesOutcome Outcome => _outcome;

        /// <summary>
        /// La última llamada a <see cref="Find(BoardState, SolverOptions, BoardState, bool, int)"/> o a
        /// <see cref="TrueParAfter"/> se cortó por <see cref="SolverOptions.Cancellation"/> (<c>hint_timeout_ms</c>, K2). Cada
        /// una de las dos lo reinicia al empezar. False si la llamada terminó, aunque la bandera se marcara después de su última
        /// consulta: el resultado es entonces completo y válido.
        /// <list type="bullet">
        /// <item><description>Tras <see cref="Find(BoardState, SolverOptions, BoardState, bool, int)"/> (lo que lee Morpho):
        /// <see cref="Count"/> = 0 y <see cref="Outcome"/> = <see cref="SafeMovesOutcome.BudgetExceeded"/>, la respuesta 4 («No
        /// encuentro un camino desde aquí», sin cobrar).</description></item>
        /// <item><description>Tras <see cref="TrueParAfter"/>: solo que ese par no se demostró por el corte (devolvió
        /// <see cref="BudgetExceeded"/>). <see cref="Count"/>, <see cref="Outcome"/> y los vertidos siguen siendo los de la
        /// última <see cref="Find(BoardState, SolverOptions, BoardState, bool, int)"/>, que pudo terminar con
        /// <see cref="SafeMovesOutcome.Found"/>.</description></item>
        /// </list>
        /// </summary>
        public bool WasCancelled => _cancelled;

        /// <summary>Realidades de la última llamada (<see cref="RealityEnumerator.Count"/>).</summary>
        public int RealityCount => _realities.Count;

        /// <summary>Estado de la enumeración de la última llamada.</summary>
        public RealityStatus Status => _realities.Status;

        /// <summary>
        /// Búsquedas A* hechas desde que se creó o se vació el memo (las demás salieron del memo), también las que se cortaron.
        /// </summary>
        public int Searches { get; private set; }

        /// <summary>Estados expandidos por esas búsquedas.</summary>
        public long Expanded { get; private set; }

        /// <summary>Estados canónicos guardados en el memo.</summary>
        public int MemoCount => _memo.Count;

        /// <summary>
        /// Calcula los vertidos seguros de <paramref name="state"/> en el orden de <see cref="SolverMoves"/> (origen, destino).
        /// </summary>
        /// <param name="state">Tablero actual con sus dosis ocultas.</param>
        /// <param name="options">Presupuesto por realidad, peso, fútiles, modo de vertido y, en el cliente, la bandera de corte
        /// por tiempo (<see cref="SolverOptions.Cancellation"/>).</param>
        /// <param name="t4Reference">Tablero de referencia de T4 (el inicial del nivel); null = <paramref name="state"/>.</param>
        /// <param name="enforceT4">Aplicar T4 a las realidades (true en campaña).</param>
        /// <param name="maxRealities">Máximo de realidades (24 por defecto, 02 T2).</param>
        /// <returns>El estado de la enumeración; si no es <see cref="RealityStatus.Ok"/>, <see cref="Count"/> = 0.</returns>
        public RealityStatus Find(BoardState state, SolverOptions options, BoardState? t4Reference = null, bool enforceT4 = true, int maxRealities = RealityEnumerator.DefaultMaxRealities)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!options.IsValid)
            {
                throw new ArgumentException("Opciones no válidas: usa SafeMoves.ForPipeline o el constructor de SolverOptions.", nameof(options));
            }

            if (maxRealities < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRealities), maxRealities, "Al menos 1 realidad.");
            }

            BoardState reference = t4Reference ?? state;
            return Find(state.CuiasArray, state.Count, state.UnitsTable, reference.CuiasArray, reference.Count, options, enforceT4, maxRealities);
        }

        /// <summary>Vertido seguro <paramref name="index"/> (índices reales del tablero).</summary>
        public Pour GetMove(int index)
        {
            CheckIndex(index);
            return _safe[index].ToPour();
        }

        /// <summary>
        /// Par de la realidad verdadera (el tablero de la última llamada, con sus colores reales) tras el vertido seguro
        /// <paramref name="index"/>, con el memo: lo que usa <see cref="SafePath"/> para elegir (03 §8: «el seguro con menor
        /// par en S_real»). <see cref="Unsolvable"/> o <see cref="BudgetExceeded"/> si no hay solución demostrada. Usa las
        /// opciones de la última <see cref="Find(BoardState, SolverOptions, BoardState, bool, int)"/>, con su bandera: si la
        /// búsqueda se corta (K2), <see cref="BudgetExceeded"/> sin guardarlo en el memo y <see cref="WasCancelled"/> = true hasta
        /// la llamada siguiente; <see cref="Count"/>, <see cref="Outcome"/> y los vertidos no cambian.
        /// </summary>
        public int TrueParAfter(int index)
        {
            CheckIndex(index);
            _cancelled = false; // WasCancelled habla de esta llamada (K2)
            SolverMove move = _safe[index];
            Array.Copy(_state, _child, _width);
            var events = default(PourEvents);
            BoardRules.ApplyInPlace(_child, 0, _width, _units, move.Source, move.Target, _options.PourMode, move.IsFutile, ref events);
            return ParOfChild();
        }

        /// <summary>Vacía el memo y los contadores (por ejemplo, para medir un nivel aparte).</summary>
        public void ClearMemo()
        {
            _memo.Clear();
            Searches = 0;
            Expanded = 0;
        }

        /// <summary>Ruta sin asignaciones de <see cref="Find(BoardState, SolverOptions, BoardState, bool, int)"/>.</summary>
        internal RealityStatus Find(Cuia[] cuias, int width, int[] units, Cuia[] reference, int referenceWidth, SolverOptions options, bool enforceT4, int maxRealities)
        {
            if (!SameOptions(options, _memoOptions))
            {
                ClearMemo(); // el par guardado depende del presupuesto, del peso, del modo y de los fútiles
                _memoOptions = options;
            }

            _options = options;
            _width = width;
            _count = 0;
            _cancelled = false;
            _outcome = SafeMovesOutcome.RealitiesRejected;
            Array.Copy(cuias, 0, _state, 0, width);
            Array.Copy(units, _units, CuiaRules.UnitsTableLength);
            RealityStatus status = _realities.Enumerate(cuias, 0, width, units, reference, referenceWidth, enforceT4, maxRealities);
            if (status != RealityStatus.Ok)
            {
                return status;
            }

            SolverCancellation? cancellation = options.Cancellation;
            if (cancellation != null && cancellation.IsRequested)
            {
                return Cancelled(status); // marcada antes de empezar: ni una búsqueda
            }

            int candidates = SolverMoves.Collect(_state, 0, width, _units, options.PourMode, options.FutileRuleEnabled, _candidates);
            Cuia[] realities = _realities.Buffer;
            int count = _realities.Count;
            for (int m = 0; m < candidates; m++)
            {
                SolverMove move = _candidates[m];
                bool safe = true;
                for (int r = 0; r < count; r++)
                {
                    int par = ParAfter(realities, r, move);
                    if (_cancelled)
                    {
                        return Cancelled(status);
                    }

                    if (par < 0)
                    {
                        // Primera realidad en la que el vertido no es seguro: irresoluble o presupuesto agotado (para Outcome).
                        safe = false;
                        _failPar[m] = par;
                        _failReality[m] = r;
                        break;
                    }
                }

                if (safe)
                {
                    _safe[_count++] = move;
                }
            }

            _outcome = _count > 0 ? SafeMovesOutcome.Found : Explain(candidates, realities, count);
            return _cancelled ? Cancelled(status) : status;
        }

        /// <summary>Corte por <see cref="SolverOptions.Cancellation"/> (K2): sin Pista parcial, respuesta 4 y no cobra.</summary>
        private RealityStatus Cancelled(RealityStatus status)
        {
            _cancelled = true;
            _count = 0;
            _outcome = SafeMovesOutcome.BudgetExceeded;
            return status;
        }

        /// <summary>
        /// Por qué no hay vertido seguro (orden de respuestas de Morpho; propuesta de la tinaja, b.2 y c.2), con el memo y sin
        /// asignaciones: victoria; si ninguna realidad tiene solución demostrada desde aquí, «sin salida» o, si alguna agotó el
        /// presupuesto, «presupuesto agotado»; si alguna la tiene, «alguna realidad tiene solución» solo si cada candidato
        /// tiene una realidad demostrada irresoluble tras él (si no, falta presupuesto para decidirlo).
        /// </summary>
        private SafeMovesOutcome Explain(int candidates, Cuia[] realities, int count)
        {
            int width = _width;
            if (BoardRules.IsWon(_state, 0, width, _units))
            {
                return SafeMovesOutcome.Won;
            }

            bool solvable = false;
            bool budget = false;
            for (int r = 0; r < count; r++)
            {
                Array.Copy(realities, r * width, _child, 0, width);
                int par = ParOfChild();
                if (_cancelled)
                {
                    return SafeMovesOutcome.BudgetExceeded;
                }

                solvable |= par >= 0;
                budget |= par == BudgetExceeded;
            }

            if (!solvable)
            {
                return budget ? SafeMovesOutcome.BudgetExceeded : SafeMovesOutcome.NoExit;
            }

            if (count == 1)
            {
                // Una realidad con solución: el primer vertido de esa solución deja el tablero resoluble, así que si no salió
                // como seguro es que su búsqueda agotó el presupuesto. Nunca la Luciérnaga con R = 1 (b.2, respuesta 3). Con
                // w = 1 y H consistente no ocurre (los expandidos desde el hijo óptimo no superan los del padre); con w > 1 sí
                // (TinajaSolverTests.Morpho_OneSolvableReality_WithoutASafePour_IsOutOfBudget_NeverTheFirefly).
                return SafeMovesOutcome.BudgetExceeded;
            }

            for (int m = 0; m < candidates; m++)
            {
                if (_failPar[m] == Unsolvable)
                {
                    continue;
                }

                bool proven = false;
                for (int r = _failReality[m] + 1; r < count && !proven; r++)
                {
                    proven = ParAfter(realities, r, _candidates[m]) == Unsolvable;
                    if (_cancelled)
                    {
                        return SafeMovesOutcome.BudgetExceeded;
                    }
                }

                if (!proven)
                {
                    return SafeMovesOutcome.BudgetExceeded; // este vertido podría ser seguro con más presupuesto
                }
            }

            return SafeMovesOutcome.SomeRealitySolvable;
        }

        /// <summary>Par (con el memo) de la realidad <paramref name="reality"/> tras el vertido <paramref name="move"/>.</summary>
        private int ParAfter(Cuia[] realities, int reality, SolverMove move)
        {
            int width = _width;
            Array.Copy(realities, reality * width, _child, 0, width);
            var events = default(PourEvents);
            BoardRules.ApplyInPlace(_child, 0, width, _units, move.Source, move.Target, _options.PourMode, move.IsFutile, ref events);
            return ParOfChild();
        }

        /// <summary>
        /// Par de <see cref="_child"/>: 0 si ya es victoria; si no, del memo o de una búsqueda A*. Si la búsqueda se corta
        /// (K2), <see cref="BudgetExceeded"/> sin guardarlo en el memo y <see cref="_cancelled"/>.
        /// </summary>
        private int ParOfChild()
        {
            int width = _width;
            if (BoardRules.IsWon(_child, 0, width, _units))
            {
                return 0;
            }

            Array.Copy(_child, _canon, width);
            BoardCanon.Sort(_canon, 0, width);
            ulong hash = BoardCanon.Hash(_canon, 0, width);
            if (_memo.TryGet(_canon, width, hash, out int par))
            {
                return par;
            }

            SolveStatus status = _solver.SolveLength(_child, 0, width, _units, _options, out int length, out int expanded, out bool cancelled);
            Searches++;
            Expanded += expanded;
            if (cancelled)
            {
                _cancelled = true;
                return BudgetExceeded; // no es el resultado del presupuesto: no entra en el memo
            }

            par = status == SolveStatus.Solved ? length : status == SolveStatus.Unsolvable ? Unsolvable : BudgetExceeded;
            _memo.Add(_canon, width, hash, par);
            return par;
        }

        private void CheckIndex(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice fuera de los vertidos seguros.");
            }
        }

        private static bool SameOptions(SolverOptions a, SolverOptions b)
        {
            return a.NodeBudget == b.NodeBudget && a.Weight == b.Weight && a.FutileRuleEnabled == b.FutileRuleEnabled && a.PourMode == b.PourMode;
        }

        /// <summary>
        /// Memo exacto por estado canónico (03 §8): tabla hash abierta sobre arrays con la comparación completa de las cuias,
        /// como <see cref="Collections.StateSet"/>, más el par de cada estado. Solo asigna al crecer.
        /// </summary>
        private sealed class CanonMemo
        {
            private const int InitialCapacity = 1024;

            private int _capacity = InitialCapacity;
            private int _count;
            private Cuia[] _states = new Cuia[InitialCapacity * BoardState.MaxCuias];
            private int[] _widths = new int[InitialCapacity];
            private ulong[] _hashes = new ulong[InitialCapacity];
            private int[] _values = new int[InitialCapacity];
            private int[] _table = new int[InitialCapacity * 2];
            private int _mask = (InitialCapacity * 2) - 1;

            public int Count => _count;

            public void Clear()
            {
                _count = 0;
                Array.Clear(_table, 0, _table.Length);
            }

            public bool TryGet(Cuia[] canonical, int width, ulong hash, out int value)
            {
                int index = Find(canonical, width, hash);
                value = index >= 0 ? _values[index] : 0;
                return index >= 0;
            }

            public void Add(Cuia[] canonical, int width, ulong hash, int value)
            {
                if (_count == _capacity)
                {
                    Grow();
                }

                int index = _count++;
                Array.Copy(canonical, 0, _states, index * BoardState.MaxCuias, width);
                _widths[index] = width;
                _hashes[index] = hash;
                _values[index] = value;
                Insert(index, hash);
            }

            private int Find(Cuia[] canonical, int width, ulong hash)
            {
                int slot = Slot(hash);
                while (true)
                {
                    int entry = _table[slot];
                    if (entry == 0)
                    {
                        return -1;
                    }

                    int index = entry - 1;
                    if (_hashes[index] == hash && _widths[index] == width && Same(index, canonical, width))
                    {
                        return index;
                    }

                    slot = (slot + 1) & _mask;
                }
            }

            private bool Same(int index, Cuia[] canonical, int width)
            {
                int start = index * BoardState.MaxCuias;
                for (int k = 0; k < width; k++)
                {
                    if (_states[start + k].Packed != canonical[k].Packed)
                    {
                        return false;
                    }
                }

                return true;
            }

            private void Insert(int index, ulong hash)
            {
                int slot = Slot(hash);
                while (_table[slot] != 0)
                {
                    slot = (slot + 1) & _mask;
                }

                _table[slot] = index + 1;
            }

            private int Slot(ulong hash)
            {
                return unchecked((int)hash ^ (int)(hash >> 32)) & _mask;
            }

            private void Grow()
            {
                int capacity = _capacity * 2;
                var states = new Cuia[capacity * BoardState.MaxCuias];
                Array.Copy(_states, states, _count * BoardState.MaxCuias);
                _states = states;
                _widths = GrowArray(_widths, capacity, _count);
                _values = GrowArray(_values, capacity, _count);
                var hashes = new ulong[capacity];
                Array.Copy(_hashes, hashes, _count);
                _hashes = hashes;
                _capacity = capacity;
                _table = new int[capacity * 2];
                _mask = _table.Length - 1;
                for (int i = 0; i < _count; i++)
                {
                    Insert(i, _hashes[i]);
                }
            }

            private static int[] GrowArray(int[] source, int capacity, int count)
            {
                var bigger = new int[capacity];
                Array.Copy(source, bigger, count);
                return bigger;
            }
        }
    }
}
