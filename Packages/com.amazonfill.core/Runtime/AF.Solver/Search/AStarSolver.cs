#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Collections;

namespace AF.Solver.Search
{
    /// <summary>
    /// Solver A* (ponderado con <c>w</c> entero) sobre estados canónicos (GDD 03 §3 y §5) en los dos modos de vertido
    /// (D-001): heurística <see cref="SolverHeuristic"/> del modo (<c>H</c> en <see cref="PourMode.Run"/>,
    /// <c>H_single</c> en <see cref="PourMode.Single"/>), movimientos = <c>movimientos_utiles</c> de 02 §1.4 evaluados con
    /// <see cref="CuiaRules"/> (la misma implementación que el cliente), presupuesto de estados expandidos y
    /// prueba de irresolubilidad por agotamiento. Admite tableros de hasta 16 cuias (14 generadas, R1).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Estado canónico:</b> cada nodo guarda sus cuias ordenadas con <see cref="BoardCanon.Sort"/> y
    /// su hash FNV-1a de 64 bits (<see cref="BoardCanon.Hash"/>); los visitados son una tabla hash abierta propia sobre
    /// arrays (sin <c>Dictionary</c>).</description></item>
    /// <item><description><b>Movimientos:</b> los de 03 §5 <c>MOVIMIENTOS</c>, con la cantidad de
    /// <see cref="CuiaRules.Amount(Cuia, Cuia, PourMode)"/>: la racha completa que cabe en <see cref="PourMode.Run"/> y una
    /// dosis en <see cref="PourMode.Single"/>. Los pares (A, B) que se expanden son los mismos en los dos modos (el factor de
    /// ramificación por nodo no cambia); en <c>Single</c> crece el conjunto de estados alcanzables (decisiones.md §2.1.3).
    /// Las cuias idénticas (mismo <see cref="Cuia.Packed"/>, que incluye la capacidad) se expanden una sola vez como
    /// origen y como destino: generalizan "un solo vacío por (capacidad, attr)" de 03 §5 y producen exactamente los
    /// mismos hijos canónicos.</description></item>
    /// <item><description><b>Orden:</b> el de <c>ASTAR</c> en 03 §5 y de <c>tools/levelgen_proto.py</c>, que encolan
    /// <c>(f, g, S)</c> y <c>(f, g, contador, clave)</c>: prioridad <c>f = g + w·H</c>; a igual <c>f</c>, primero el de
    /// <b>menor</b> <c>g</c>; a igualdad total, orden de llegada (<see cref="MinHeap"/>). Con <c>w = 1</c> el par no
    /// depende del desempate (H admisible); con <c>w &gt; 1</c> (Morpho, Pororoca, Río sin fin) el desempate decide qué
    /// solución subóptima sale, y así coincide con el prototipo. Determinista en CoreCLR, Mono e IL2CPP.</description></item>
    /// <item><description><b>Solución:</b> se guarda en índices canónicos y se traduce al final a los índices reales del
    /// tablero de partida, rehaciendo cada vertido con <see cref="CuiaRules"/>.</description></item>
    /// <item><description><b>Memoria:</b> cada nodo guarda solo las <c>T</c> cuias del tablero (paso <c>T</c>, no las 16 de
    /// <see cref="BoardState.MaxCuias"/>). Los arrays de nodos, la tabla y el montículo se reutilizan entre llamadas y
    /// solo crecen (duplicando, o al empezar una búsqueda con un tablero más ancho) cuando hace falta; el bucle de
    /// expansión no asigna. Por llamada solo se asigna el array de la solución.</description></item>
    /// </list>
    /// No es seguro entre hilos: usa una instancia por hilo.
    /// </remarks>
    public sealed class AStarSolver
    {
        private const int DefaultInitialStates = 4096;
        private const int TieSpan = 1024;
        private const int MaxPriorityF = (int.MaxValue / TieSpan) - 1;
        private const int NoParent = -1;

        private readonly MinHeap _open;
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly Cuia[] _parentBuf = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _childBuf = new Cuia[BoardState.MaxCuias];
        private readonly Cuia[] _realBuf = new Cuia[BoardState.MaxCuias];
        private readonly int[] _perm = new int[BoardState.MaxCuias];

        private int _width;
        private PourMode _pourMode;
        private int _nodeCount;
        private int _nodeCapacity;
        private Cuia[] _nodeCuias;
        private int[] _nodeG;
        private int[] _nodeH;
        private int[] _nodeParent;
        private int[] _nodeMove;
        private ulong[] _nodeHash;
        private int[] _table;
        private int _tableMask;
        private int[] _pathBuf;

        /// <summary>Crea un solver con espacio inicial para 4.096 estados.</summary>
        public AStarSolver()
            : this(DefaultInitialStates)
        {
        }

        /// <summary>Crea un solver con espacio inicial para <paramref name="initialStateCapacity"/> estados (crece solo).</summary>
        public AStarSolver(int initialStateCapacity)
        {
            if (initialStateCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(initialStateCapacity), initialStateCapacity, "La capacidad inicial debe ser al menos 1.");
            }

            _nodeCapacity = initialStateCapacity;
            _nodeCuias = Array.Empty<Cuia>(); // se dimensiona en Reset con el ancho real del tablero
            _nodeG = new int[initialStateCapacity];
            _nodeH = new int[initialStateCapacity];
            _nodeParent = new int[initialStateCapacity];
            _nodeMove = new int[initialStateCapacity];
            _nodeHash = new ulong[initialStateCapacity];
            int tableSize = TableSizeFor(initialStateCapacity);
            _table = new int[tableSize];
            _tableMask = tableSize - 1;
            _open = new MinHeap(initialStateCapacity);
            _pathBuf = new int[64];
        }

        /// <summary>
        /// Busca una solución desde <paramref name="start"/> (pseudocódigo <c>ASTAR</c> de 03 §5). Con
        /// <c>Weight = 1</c> la solución es óptima y su longitud es el par.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="options"/> es <c>default</c> o no es válido.</exception>
        public SolveResult Solve(BoardState start, SolverOptions options)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (!options.IsValid)
            {
                throw new ArgumentException("Opciones no válidas: crea SolverOptions con su constructor o usa SolverOptions.ForPipeline.", nameof(options));
            }

            Reset(start, options.PourMode);
            int width = _width;
            for (int i = 0; i < width; i++)
            {
                _childBuf[i] = start[i];
            }

            BoardCanon.Sort(_childBuf, 0, width);
            int root = AddNode(BoardCanon.Hash(_childBuf, 0, width), 0, NoParent, 0);
            int weight = options.Weight;
            bool futileRuleEnabled = options.FutileRuleEnabled;
            int budget = options.NodeBudget;
            _open.Push(root, Priority(root, weight));

            int expanded = 0;
            while (_open.TryPop(out int node, out int priority))
            {
                if (priority != Priority(node, weight))
                {
                    continue; // entrada obsoleta: el nodo se mejoró después de encolarlo
                }

                if (IsGoal(node))
                {
                    return BuildSolution(start, node, expanded);
                }

                expanded++;
                if (expanded > budget)
                {
                    return new SolveResult(SolveStatus.BudgetExceeded, null, expanded, _nodeCount);
                }

                Expand(node, weight, futileRuleEnabled);
            }

            return new SolveResult(SolveStatus.Unsolvable, null, expanded, _nodeCount);
        }

        private void Reset(BoardState start, PourMode pourMode)
        {
            _width = start.Count;
            _pourMode = pourMode;
            _nodeCount = 0;
            int needed = _nodeCapacity * _width;
            if (_nodeCuias.Length < needed)
            {
                _nodeCuias = new Cuia[needed]; // capacidad × T (no × 16): fuera del bucle de expansión
            }

            Array.Clear(_table, 0, _table.Length);
            _open.Clear();
            _units[0] = 0;
            for (int color = Cuia.MinColor; color <= Cuia.MaxColor; color++)
            {
                _units[color] = start.UnitsOf(color);
            }
        }

        private void Expand(int node, int weight, bool futileRuleEnabled)
        {
            int width = _width;
            Array.Copy(_nodeCuias, node * width, _parentBuf, 0, width);
            int childG = _nodeG[node] + 1;
            int[] units = _units;
            PourMode pourMode = _pourMode;
            for (int i = 0; i < width; i++)
            {
                Cuia from = _parentBuf[i];
                if (!CuiaRules.CanBeSource(from, units))
                {
                    continue;
                }

                if (i > 0 && from.Packed == _parentBuf[i - 1].Packed)
                {
                    continue; // origen idéntico al anterior: mismos hijos canónicos
                }

                int fromTop = from.TopColor;
                for (int j = 0; j < width; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    Cuia to = _parentBuf[j];
                    if (to.IsFull || (!to.IsEmpty && to.TopColor != fromTop))
                    {
                        continue; // descarte rápido de pares que IsLegal rechazaría igual (B llena o con otra cima)
                    }

                    if (j > 0 && j - 1 != i && to.Packed == _parentBuf[j - 1].Packed)
                    {
                        continue; // destino idéntico al anterior (03 §5: vacíos equivalentes una sola vez)
                    }

                    if (!CuiaRules.IsLegal(from, to, units))
                    {
                        continue;
                    }

                    if (futileRuleEnabled && CuiaRules.IsFutile(from, to, units))
                    {
                        continue;
                    }

                    int amount = CuiaRules.Amount(from, to, pourMode);
                    Array.Copy(_parentBuf, _childBuf, width);
                    _childBuf[i] = from.RemoveTop(amount);
                    _childBuf[j] = to.AddOnTop(from.TopColor, amount);
                    BoardCanon.Sort(_childBuf, 0, width);
                    ulong hash = BoardCanon.Hash(_childBuf, 0, width);
                    int move = (i << 8) | j;
                    int existing = Find(hash);
                    if (existing < 0)
                    {
                        int child = AddNode(hash, childG, node, move);
                        _open.Push(child, Priority(child, weight));
                    }
                    else if (childG < _nodeG[existing])
                    {
                        _nodeG[existing] = childG;
                        _nodeParent[existing] = node;
                        _nodeMove[existing] = move;
                        _open.Push(existing, Priority(existing, weight));
                    }
                }
            }
        }

        private bool IsGoal(int node)
        {
            int width = _width;
            int offset = node * width;
            for (int k = 0; k < width; k++)
            {
                Cuia cuia = _nodeCuias[offset + k];
                if (!cuia.IsEmpty && !CuiaRules.IsSealed(cuia, _units))
                {
                    return false;
                }
            }

            return true;
        }

        private int Priority(int node, int weight)
        {
            int g = _nodeG[node];
            int f = g + (weight * _nodeH[node]);
            if (f > MaxPriorityF)
            {
                f = MaxPriorityF;
            }

            // 03 §5: a igual f sale primero el de menor g (tupla (f, g, S) del pseudocódigo y del prototipo).
            int depth = g < TieSpan ? g : TieSpan - 1;
            return (f * TieSpan) + depth;
        }

        /// <summary>Busca en la tabla el estado de <see cref="_childBuf"/>; -1 si no está.</summary>
        private int Find(ulong hash)
        {
            int width = _width;
            int slot = Slot(hash);
            while (true)
            {
                int entry = _table[slot];
                if (entry == 0)
                {
                    return -1;
                }

                int node = entry - 1;
                if (_nodeHash[node] == hash && SameAsChild(node, width))
                {
                    return node;
                }

                slot = (slot + 1) & _tableMask;
            }
        }

        private bool SameAsChild(int node, int width)
        {
            int offset = node * width;
            for (int k = 0; k < width; k++)
            {
                if (_nodeCuias[offset + k].Packed != _childBuf[k].Packed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Guarda el estado de <see cref="_childBuf"/> como nodo nuevo (el llamante sabe que no existe).</summary>
        private int AddNode(ulong hash, int g, int parent, int move)
        {
            if (_nodeCount == _nodeCapacity)
            {
                Grow();
            }

            int width = _width;
            int node = _nodeCount++;
            Array.Copy(_childBuf, 0, _nodeCuias, node * width, width);
            _nodeG[node] = g;
            _nodeH[node] = SolverHeuristic.Estimate(_childBuf, 0, width, _pourMode);
            _nodeParent[node] = parent;
            _nodeMove[node] = move;
            _nodeHash[node] = hash;
            InsertInTable(node, hash);
            return node;
        }

        private void InsertInTable(int node, ulong hash)
        {
            int slot = Slot(hash);
            while (_table[slot] != 0)
            {
                slot = (slot + 1) & _tableMask;
            }

            _table[slot] = node + 1;
        }

        private int Slot(ulong hash)
        {
            return unchecked((int)hash ^ (int)(hash >> 32)) & _tableMask;
        }

        private void Grow()
        {
            int capacity = _nodeCapacity * 2;
            var cuias = new Cuia[capacity * _width];
            Array.Copy(_nodeCuias, cuias, _nodeCount * _width);
            _nodeCuias = cuias;
            _nodeG = GrowArray(_nodeG, capacity, _nodeCount);
            _nodeH = GrowArray(_nodeH, capacity, _nodeCount);
            _nodeParent = GrowArray(_nodeParent, capacity, _nodeCount);
            _nodeMove = GrowArray(_nodeMove, capacity, _nodeCount);
            var hashes = new ulong[capacity];
            Array.Copy(_nodeHash, hashes, _nodeCount);
            _nodeHash = hashes;
            _nodeCapacity = capacity;

            int tableSize = TableSizeFor(capacity);
            if (tableSize > _table.Length)
            {
                _table = new int[tableSize];
                _tableMask = tableSize - 1;
                for (int node = 0; node < _nodeCount; node++)
                {
                    InsertInTable(node, _nodeHash[node]);
                }
            }
        }

        private SolveResult BuildSolution(BoardState start, int goal, int expanded)
        {
            // Longitud de la cadena de padres (no se usa g: con w > 1 un padre reabierto puede haber mejorado).
            int length = 0;
            for (int node = goal; _nodeParent[node] != NoParent; node = _nodeParent[node])
            {
                length++;
            }

            if (_pathBuf.Length < length)
            {
                _pathBuf = new int[Math.Max(length, _pathBuf.Length * 2)];
            }

            int step = length;
            for (int node = goal; _nodeParent[node] != NoParent; node = _nodeParent[node])
            {
                _pathBuf[--step] = _nodeMove[node];
            }

            // Traduce los índices canónicos a los reales rehaciendo los vertidos sobre el tablero de partida.
            int width = _width;
            for (int i = 0; i < width; i++)
            {
                _realBuf[i] = start[i];
            }

            var moves = new Pour[length];
            for (int s = 0; s < length; s++)
            {
                CanonicalOrder(width);
                int source = _perm[_pathBuf[s] >> 8];
                int target = _perm[_pathBuf[s] & 0xFF];
                Cuia from = _realBuf[source];
                Cuia to = _realBuf[target];
                if (!CuiaRules.IsLegal(from, to, _units))
                {
                    throw new InvalidOperationException("AF.Solver: la solución canónica no se puede rehacer sobre el tablero real.");
                }

                int amount = CuiaRules.Amount(from, to, _pourMode);
                moves[s] = new Pour(source, target, amount);
                _realBuf[source] = from.RemoveTop(amount);
                _realBuf[target] = to.AddOnTop(from.TopColor, amount);
            }

            return new SolveResult(SolveStatus.Solved, moves, expanded, _nodeCount);
        }

        /// <summary>
        /// <see cref="_perm"/>[posición canónica] = índice real: orden de <see cref="_realBuf"/> por
        /// (<see cref="Cuia.Packed"/>, índice), el mismo que da <see cref="BoardCanon.Sort"/>.
        /// </summary>
        private void CanonicalOrder(int width)
        {
            for (int i = 0; i < width; i++)
            {
                _perm[i] = i;
            }

            for (int i = 1; i < width; i++)
            {
                int index = _perm[i];
                ulong packed = _realBuf[index].Packed;
                int j = i - 1;
                while (j >= 0 && _realBuf[_perm[j]].Packed > packed)
                {
                    _perm[j + 1] = _perm[j];
                    j--;
                }

                _perm[j + 1] = index;
            }
        }

        private static int[] GrowArray(int[] source, int capacity, int count)
        {
            var bigger = new int[capacity];
            Array.Copy(source, bigger, count);
            return bigger;
        }

        private static int TableSizeFor(int stateCapacity)
        {
            int size = 16;
            while (size < stateCapacity * 2)
            {
                size <<= 1;
            }

            return size;
        }
    }
}
