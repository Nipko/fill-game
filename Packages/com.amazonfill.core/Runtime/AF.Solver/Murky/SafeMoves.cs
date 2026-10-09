#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;

namespace AF.Solver.Murky
{
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
    /// <item><description>Sin asignaciones una vez calentada, salvo el crecimiento del memo. No es segura entre
    /// hilos.</description></item>
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
        private readonly Cuia[] _state = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _child = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _canon = new Cuia[BoardState.MaxCuias];
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly CanonMemo _memo = new CanonMemo();
        private SolverOptions _options;
        private SolverOptions _memoOptions;
        private int _width;
        private int _count;

        /// <summary>Opciones del pipeline en el modo <paramref name="mode"/>: 20.000 nodos por realidad, w = 1, con fútiles (03 §8).</summary>
        public static SolverOptions ForPipeline(PourMode mode)
        {
            return new SolverOptions(PipelineRealityBudget, 1, true, mode);
        }

        /// <summary>Vertidos seguros de la última llamada (0 = ninguno, T5).</summary>
        public int Count => _count;

        /// <summary>Realidades de la última llamada (<see cref="RealityEnumerator.Count"/>).</summary>
        public int RealityCount => _realities.Count;

        /// <summary>Estado de la enumeración de la última llamada.</summary>
        public RealityStatus Status => _realities.Status;

        /// <summary>Búsquedas A* hechas desde que se creó o se vació el memo (las demás salieron del memo).</summary>
        public int Searches { get; private set; }

        /// <summary>Estados expandidos por esas búsquedas.</summary>
        public long Expanded { get; private set; }

        /// <summary>Estados canónicos guardados en el memo.</summary>
        public int MemoCount => _memo.Count;

        /// <summary>
        /// Calcula los vertidos seguros de <paramref name="state"/> en el orden de <see cref="SolverMoves"/> (origen, destino).
        /// </summary>
        /// <param name="state">Tablero actual con sus dosis ocultas.</param>
        /// <param name="options">Presupuesto por realidad, peso, fútiles y modo de vertido.</param>
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
        /// par en S_real»). <see cref="Unsolvable"/> o <see cref="BudgetExceeded"/> si no hay solución demostrada.
        /// </summary>
        public int TrueParAfter(int index)
        {
            CheckIndex(index);
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
            Array.Copy(cuias, 0, _state, 0, width);
            Array.Copy(units, _units, CuiaRules.UnitsTableLength);
            RealityStatus status = _realities.Enumerate(cuias, 0, width, units, reference, referenceWidth, enforceT4, maxRealities);
            if (status != RealityStatus.Ok)
            {
                return status;
            }

            int candidates = SolverMoves.Collect(_state, 0, width, _units, options.PourMode, options.FutileRuleEnabled, _candidates);
            Cuia[] realities = _realities.Buffer;
            int count = _realities.Count;
            for (int m = 0; m < candidates; m++)
            {
                SolverMove move = _candidates[m];
                bool safe = true;
                for (int r = 0; r < count && safe; r++)
                {
                    Array.Copy(realities, r * width, _child, 0, width);
                    var events = default(PourEvents);
                    BoardRules.ApplyInPlace(_child, 0, width, _units, move.Source, move.Target, options.PourMode, move.IsFutile, ref events);
                    safe = ParOfChild() >= 0;
                }

                if (safe)
                {
                    _safe[_count++] = move;
                }
            }

            return status;
        }

        /// <summary>Par de <see cref="_child"/>: 0 si ya es victoria; si no, del memo o de una búsqueda A*.</summary>
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

            SolveStatus status = _solver.SolveLength(_child, 0, width, _units, _options, out int length, out int expanded);
            Searches++;
            Expanded += expanded;
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
