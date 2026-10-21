#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Collections;
using AF.Solver.Generation;
using AF.Solver.Search;

namespace AF.Solver.Shuffle
{
    /// <summary>
    /// Pororoca · Barajar (GDD 04 §2.4; 03 §16; 03 §5, tabla de usos del solver; research/02 Complemento 4.4 [E]): baraja las
    /// cuias no fijas conservando colores, número de vacíos y capacidades por cuia, y comprueba con un A* acotado que el par
    /// nuevo no pasa del actual más la holgura (<c>shuffle_par_slack</c> = 2).
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description><b>La ola revela</b> (04 §2.4 a, g; 02 T6): se trabaja sobre el tablero con todas sus dosis ocultas
    /// visibles (Aguas turbias y la Tinaja), así que el tablero devuelto no tiene ocultas. Quien llama decide si se puede revelar
    /// (<c>shuffle_reveals_murky</c>): <see cref="Check"/>.</description></item>
    /// <item><description><b>Cuias fijas</b> (04 §2.4 y b, g): las selladas (R5), las que llevan Barro y las que aún tienen una
    /// mecánica (Hoja o Brote cerrados, Liana atada, Matapi con su contenido, Flor cerrada, Hoja atada [P], la Tinaja y su cuia
    /// colgada); se tratan como selladas. Una mecánica que ya se abrió o se soltó deja la cuia normal (02 R16 para D-007), así que
    /// entra en la ola; la cuia suelta de la tinaja también (como Cuia chica normal). El Remolino participa normalmente (04
    /// §2.4 c): sus dos cuias conservan la pareja. La Cuia extra se conserva como vacío (e).</description></item>
    /// <item><description><b>Reconstrucción inversa</b> (03 §16): las dosis de las cuias movibles forman un estado resuelto
    /// parcial (cada color reunido en la cuia movible más pequeña que lo aloja, del color con más dosis al de menos y, a
    /// igualdad, del de id menor) sobre el que se dan hasta k = max(12, 3 × colores movibles) movimientos inversos legales
    /// (<see cref="InverseMoves"/>, los mismos del generador, que nunca vuelcan una monocolor entera en una vacía) al azar entre
    /// los no visitados, y se toma el estado válido más profundo del paseo [P]: los mismos vacíos que ahora, ninguna cuia movible
    /// llena y monocolor (sellada o no; 04 §2.4) y distinto del tablero actual (03 §16). Las mecánicas de las cuias movibles (el
    /// Remolino) no entran en el paseo, que se da sobre cuias normales de la misma capacidad: el A* las verifica después con
    /// las reglas completas del núcleo.</description></item>
    /// <item><description><b>Verificación</b> (04 §2.4; 03 §5): A* con <c>shuffle_budget_nodes</c> (40.000) y w = 2 sobre el
    /// tablero entero, con las reglas de <see cref="BoardRules"/> y sin poda de fútiles si hay Tinaja
    /// (<see cref="SolverOptions.ForLevel"/>); el tablero vale si resuelve y su longitud es ≤ la del tablero de partida más la
    /// holgura. Si el A* no resuelve el tablero de partida dentro del presupuesto (par desconocido), basta con que resuelva el
    /// nuevo [P].</description></item>
    /// <item><description><b>Respaldo</b> (04 §2.4): tras <see cref="ShuffleOptions.InverseAttempts"/> intentos inversos (o si
    /// los colores movibles no caben en un estado resuelto), Permutar + verificar: las dosis movibles se barajan (Fisher-Yates)
    /// y se reparten en las mismas cuias con el mismo número de dosis cada una, hasta
    /// <see cref="ShuffleOptions.PermuteAttempts"/> veces, con los mismos filtros y la misma verificación. Si nada sale,
    /// <see cref="ShuffleMethod.Failed"/>.</description></item>
    /// </list>
    /// <para><b>Determinismo:</b> solo enteros y el PRNG del núcleo. El intento inverso <c>t</c> (1..20) usa
    /// <c>Prng.SubSeed(semilla, 0x5A0F0000 + t)</c> y la permutación <c>t</c>, <c>Prng.SubSeed(semilla, 0x5A0F1000 + t)</c>; la
    /// semilla de una partida sale de <see cref="SeedFor"/>.</para>
    /// <para><b>Sin asignaciones</b> en el bucle (regla dura 2): búferes propios que solo crecen y el A* por su ruta sin solución
    /// (<c>SolveLength</c>). Solo se asigna el <see cref="BoardState"/> del resultado. No es seguro entre hilos: una instancia
    /// por hilo.</para>
    /// </remarks>
    public sealed class Shuffler
    {
        private const ulong InverseStream = 0x5A0F0000UL;
        private const ulong PermuteStream = 0x5A0F1000UL;
        private const int Width = BoardState.MaxCuias;

        private readonly Prng _rng = new Prng(0UL);
        private readonly AStarSolver _solver;
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly int[] _colorDoses = new int[CuiaRules.UnitsTableLength];
        private readonly int[] _colorOrder = new int[CuiaRules.UnitsTableLength];
        private readonly Cuia[] _board = new Cuia[Width];
        private readonly Cuia[] _candidate = new Cuia[Width];
        private readonly int[] _movable = new int[Width];
        private readonly bool[] _used = new bool[Width];
        private readonly Cuia[] _current = new Cuia[Width];
        private readonly Cuia[] _currentCanon = new Cuia[Width];
        private readonly Cuia[] _resolved = new Cuia[Width];
        private readonly Cuia[] _sub = new Cuia[Width];
        private readonly Cuia[] _best = new Cuia[Width];
        private readonly Cuia[] _scratch = new Cuia[Width];
        private readonly Cuia[] _pair = new Cuia[2];
        private readonly InverseMove[] _moves = new InverseMove[InverseMoves.MaxMoves];
        private readonly int[] _candidates = new int[InverseMoves.MaxMoves];
        private readonly int[] _doses = new int[Width * Cuia.MaxCapacity];
        private readonly StateSet _visited = new StateSet(128);

        private int _width;
        private int _movableCount;
        private int _empties;
        private int _parBefore;
        private int _parAfter;
        private int _tries;

        /// <summary>Crea un barajador con su propio A* (espacio inicial para 4.096 estados; crece solo).</summary>
        public Shuffler()
        {
            _solver = new AStarSolver();
        }

        /// <summary>
        /// Semilla de la ola (determinista, sin reloj): la del nivel (<paramref name="levelSeed"/>, p. ej.
        /// <c>amazonfill:v1:m02:v1:n0030</c>) con el hash canónico del tablero <b>revelado</b> y el ordinal de la Pororoca en el
        /// nivel. Así, rehacer el historial guardado (02 R8) vuelve a dar la misma ola aunque las capas que se vieron se restauren
        /// después, y dos Pororocas del mismo nivel sobre el mismo tablero no dan lo mismo. Asigna (no es del bucle caliente).
        /// </summary>
        public static ulong SeedFor(string levelSeed, BoardState board, int ordinal)
        {
            if (levelSeed == null)
            {
                throw new ArgumentNullException(nameof(levelSeed));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var revealed = new Cuia[board.Count];
            for (int i = 0; i < revealed.Length; i++)
            {
                Cuia cuia = board[i];
                revealed[i] = cuia.WithRevealed(cuia.HiddenMask);
            }

            BoardCanon.Sort(revealed, 0, revealed.Length);
            ulong hash = BoardCanon.Hash(revealed, 0, revealed.Length);
            return Prng.SubSeed(Prng.SeedFrom(levelSeed), unchecked(hash + (ulong)ordinal));
        }

        /// <summary>
        /// Si la ola se puede usar en <paramref name="board"/> (04 §2.4 b, d y a): con Lluvia o Piraña, nunca; con menos de 2 colores
        /// movibles, tampoco («Hay poca agua libre para la ola»); con dosis ocultas, solo si <paramref name="revealsHidden"/>
        /// (<c>shuffle_reveals_murky</c>). No mira el estado de la partida (bloqueo, victoria) ni los topes: eso es del cliente.
        /// </summary>
        public static ShuffleBlock Check(BoardState board, bool revealsHidden)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (HasTemporalMechanic(board))
            {
                return ShuffleBlock.TemporalMechanic;
            }

            if (board.HasHidden && !revealsHidden)
            {
                return ShuffleBlock.HiddenWithoutReveal;
            }

            return MovableColors(board) < 2 ? ShuffleBlock.LowWater : ShuffleBlock.None;
        }

        /// <summary>El tablero lleva una mecánica con estado temporal (Lluvia o Piraña, 04 §2.4 d).</summary>
        public static bool HasTemporalMechanic(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            for (int i = 0; i < board.Count; i++)
            {
                MechanicType type = board[i].Mechanic;
                if (type == MechanicType.Lluvia || type == MechanicType.Pirana)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// La cuia <paramref name="index"/> queda fija con la ola (04 §2.4 y b, g): sellada, con Barro o con una mecánica que no
        /// sea el Remolino. Mira la cuia con sus ocultas reveladas.
        /// </summary>
        public static bool IsFixed(BoardState board, int index)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            Cuia cuia = board[index];
            return IsFixedCuia(cuia.WithRevealed(cuia.HiddenMask), board.UnitsTable);
        }

        /// <summary>Colores distintos con alguna dosis en las cuias movibles (04 §2.4 b: «si quedan &lt;2 colores movibles»).</summary>
        public static int MovableColors(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            int seen = 0;
            int[] units = board.UnitsTable;
            for (int i = 0; i < board.Count; i++)
            {
                Cuia cuia = board[i];
                cuia = cuia.WithRevealed(cuia.HiddenMask);
                if (IsFixedCuia(cuia, units))
                {
                    continue;
                }

                for (int k = 0; k < cuia.Count; k++)
                {
                    seen |= 1 << cuia.ColorAt(k);
                }
            }

            int colors = 0;
            for (; seen != 0; seen &= seen - 1)
            {
                colors++;
            }

            return colors;
        }

        /// <summary>
        /// Baraja <paramref name="current"/> con la semilla <paramref name="seed"/> (<see cref="SeedFor"/>). Con
        /// <see cref="ShuffleMethod.Failed"/> no hay tablero (y quien llama no cobra: 04 §2.4). No mira los topes ni el estado de
        /// la partida.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="options"/> es <c>default</c>.</exception>
        public ShuffleResult Shuffle(BoardState current, ulong seed, ShuffleOptions options)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (!options.IsValid)
            {
                throw new ArgumentException("Opciones no válidas: usa el constructor de ShuffleOptions o ShuffleOptions.Default.", nameof(options));
            }

            int revealed = current.HiddenCount;
            ShuffleBlock block = HasTemporalMechanic(current) ? ShuffleBlock.TemporalMechanic : ShuffleBlock.None;
            if (block != ShuffleBlock.None)
            {
                return new ShuffleResult(ShuffleMethod.Failed, block, null, -1, -1, 0, revealed);
            }

            ShuffleMethod method = ShuffleInto(current, seed, options, out block);
            if (method == ShuffleMethod.Failed)
            {
                return new ShuffleResult(method, block, null, _parBefore, -1, _tries, revealed);
            }

            var cuias = new Cuia[_width];
            Array.Copy(_candidate, cuias, _width);
            return new ShuffleResult(method, ShuffleBlock.None, new BoardState(cuias), _parBefore, _parAfter, _tries, revealed);
        }

        /// <summary>
        /// El barajado sin el resultado: deja el tablero nuevo en <see cref="_candidate"/> (las <see cref="_width"/> primeras) y
        /// el par y los intentos en sus campos. No asigna memoria una vez calentado (lo miden los tests del bucle caliente).
        /// </summary>
        internal ShuffleMethod ShuffleInto(BoardState current, ulong seed, ShuffleOptions options, out ShuffleBlock block)
        {
            _tries = 0;
            _parBefore = -1;
            _parAfter = -1;
            block = ShuffleBlock.None;
            Load(current);
            int colors = MovableColorsLoaded();
            if (colors < 2)
            {
                block = ShuffleBlock.LowWater;
                return ShuffleMethod.Failed;
            }

            SolverOptions solve = new SolverOptions(options.BudgetNodes, options.Weight, options.FutileRuleEnabled, options.PourMode).ForLevel(current);
            _parBefore = Solve(_board, solve);
            int moves = Math.Max(ShuffleOptions.MinInverseMoves, ShuffleOptions.InverseMovesPerColor * colors);
            if (BuildResolved())
            {
                for (int t = 1; t <= options.InverseAttempts; t++)
                {
                    _tries++;
                    _rng.Reseed(Prng.SubSeed(seed, InverseStream + (ulong)t));
                    if (Walk(moves, options.PourMode) && Verify(solve, options.ParSlack))
                    {
                        return ShuffleMethod.Inverse;
                    }
                }
            }

            for (int t = 1; t <= options.PermuteAttempts; t++)
            {
                _tries++;
                _rng.Reseed(Prng.SubSeed(seed, PermuteStream + (ulong)t));
                if (Permute() && Verify(solve, options.ParSlack))
                {
                    return ShuffleMethod.Permute;
                }
            }

            return ShuffleMethod.Failed;
        }

        private static bool IsFixedCuia(Cuia cuia, int[] units)
        {
            if (cuia.HasMechanic && cuia.Mechanic != MechanicType.Remolino)
            {
                return true;
            }

            if (CuiaRules.IsSealed(cuia, units))
            {
                return true;
            }

            for (int k = 0; k < cuia.Count; k++)
            {
                if (cuia.ColorAt(k) == Cuia.MudColor)
                {
                    return true;
                }
            }

            return false;
        }

        // Cuia normal de la misma capacidad y las mismas dosis (sin mecánica): la del paseo inverso.
        private static Cuia Plain(Cuia cuia)
        {
            Cuia plain = Cuia.Empty(cuia.Capacity);
            for (int k = 0; k < cuia.Count; k++)
            {
                plain = plain.AddOnTop(cuia.ColorAt(k), 1);
            }

            return plain;
        }

        // Las dosis de content sobre la cuia shape vaciada (conserva su capacidad y su mecánica: el Remolino y su pareja).
        private static Cuia Refill(Cuia shape, Cuia content)
        {
            Cuia cuia = shape.Count > 0 ? shape.RemoveTop(shape.Count) : shape;
            for (int k = 0; k < content.Count; k++)
            {
                cuia = cuia.AddOnTop(content.ColorAt(k), 1);
            }

            return cuia;
        }

        // El tablero revelado, U_c, las cuias movibles (en orden de pantalla) con su contenido normal y su forma canónica, los
        // vacíos movibles y las dosis movibles por color.
        private void Load(BoardState current)
        {
            _width = current.Count;
            Array.Copy(current.UnitsTable, _units, CuiaRules.UnitsTableLength);
            Array.Clear(_colorDoses, 0, _colorDoses.Length);
            _movableCount = 0;
            _empties = 0;
            for (int i = 0; i < _width; i++)
            {
                Cuia cuia = current.CuiaAt(i);
                cuia = cuia.WithRevealed(cuia.HiddenMask);
                _board[i] = cuia;
                if (IsFixedCuia(cuia, _units))
                {
                    continue;
                }

                int j = _movableCount++;
                _movable[j] = i;
                _current[j] = Plain(cuia);
                _empties += cuia.IsEmpty ? 1 : 0;
                for (int k = 0; k < cuia.Count; k++)
                {
                    _colorDoses[cuia.ColorAt(k)]++;
                }
            }

            Array.Copy(_current, _currentCanon, _movableCount);
            BoardCanon.Sort(_currentCanon, 0, _movableCount);
        }

        private int MovableColorsLoaded()
        {
            int colors = 0;
            for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
            {
                colors += _colorDoses[c] > 0 ? 1 : 0;
            }

            return colors;
        }

        // Estado resuelto parcial (03 §16): cada color en la cuia movible libre más pequeña que lo aloja, del color con más
        // dosis al de menos (a igualdad, el de id menor), y en la misma cuia a igualdad de capacidad la de índice menor. False si
        // algún color no cabe en ninguna (entonces solo queda Permutar).
        private bool BuildResolved()
        {
            int n = 0;
            for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
            {
                if (_colorDoses[c] == 0)
                {
                    continue;
                }

                int at = n++;
                while (at > 0 && Before(c, _colorOrder[at - 1]))
                {
                    _colorOrder[at] = _colorOrder[at - 1];
                    at--;
                }

                _colorOrder[at] = c;
            }

            for (int j = 0; j < _movableCount; j++)
            {
                _used[j] = false;
                _resolved[j] = Cuia.Empty(_current[j].Capacity);
            }

            for (int o = 0; o < n; o++)
            {
                int color = _colorOrder[o];
                int doses = _colorDoses[color];
                int pick = -1;
                for (int j = 0; j < _movableCount; j++)
                {
                    int cap = _resolved[j].Capacity;
                    if (!_used[j] && cap >= doses && (pick < 0 || cap < _resolved[pick].Capacity))
                    {
                        pick = j;
                    }
                }

                if (pick < 0)
                {
                    return false;
                }

                _used[pick] = true;
                _resolved[pick] = _resolved[pick].AddOnTop(color, doses);
            }

            return true;
        }

        private bool Before(int a, int b)
        {
            return _colorDoses[a] > _colorDoses[b] || (_colorDoses[a] == _colorDoses[b] && a < b);
        }

        // Paseo inverso de hasta moves pasos desde el estado resuelto, al azar entre los movimientos no visitados; deja en
        // _candidate el tablero entero del estado válido más profundo. False si el paseo no pasó por ninguno.
        private bool Walk(int moves, PourMode mode)
        {
            int m = _movableCount;
            Array.Copy(_resolved, _sub, m);
            _visited.Reset(m);
            AddVisited(_sub);
            int bestDepth = 0;
            for (int step = 0; step < moves; step++)
            {
                int count = InverseMoves.Collect(_sub, 0, m, _units, mode, _moves, _pair);
                int open = 0;
                for (int k = 0; k < count; k++)
                {
                    Array.Copy(_sub, _scratch, m);
                    InverseMoves.Apply(_scratch, 0, _moves[k]);
                    BoardCanon.Sort(_scratch, 0, m);
                    if (!_visited.Contains(_scratch, 0, BoardCanon.Hash(_scratch, 0, m)))
                    {
                        _candidates[open++] = k;
                    }
                }

                if (open == 0)
                {
                    break; // el paseo muere (03 §1, hecho 3)
                }

                InverseMoves.Apply(_sub, 0, _moves[_candidates[_rng.NextInt(open)]]);
                AddVisited(_sub);
                if (IsValidSub(_sub))
                {
                    Array.Copy(_sub, _best, m);
                    bestDepth = step + 1;
                }
            }

            if (bestDepth == 0)
            {
                return false;
            }

            Compose(_best);
            return true;
        }

        // Permutar (04 §2.4): las dosis movibles barajadas (Fisher-Yates) y repartidas en las mismas cuias, con el mismo número
        // de dosis cada una; deja el tablero en _candidate si pasa los filtros.
        private bool Permute()
        {
            int m = _movableCount;
            int total = 0;
            for (int j = 0; j < m; j++)
            {
                Cuia cuia = _current[j];
                for (int k = 0; k < cuia.Count; k++)
                {
                    _doses[total++] = cuia.ColorAt(k);
                }
            }

            for (int i = total - 1; i > 0; i--)
            {
                int r = _rng.NextInt(i + 1);
                int tmp = _doses[i];
                _doses[i] = _doses[r];
                _doses[r] = tmp;
            }

            int next = 0;
            for (int j = 0; j < m; j++)
            {
                Cuia cuia = Cuia.Empty(_current[j].Capacity);
                int count = _current[j].Count;
                for (int k = 0; k < count; k++)
                {
                    cuia = cuia.AddOnTop(_doses[next++], 1);
                }

                _sub[j] = cuia;
            }

            if (!IsValidSub(_sub))
            {
                return false;
            }

            Compose(_sub);
            return true;
        }

        // Filtros de 04 §2.4 y 03 §16 sobre las cuias movibles: los mismos vacíos, ninguna llena y monocolor y distinto del
        // tablero actual (en forma canónica).
        private bool IsValidSub(Cuia[] sub)
        {
            int m = _movableCount;
            int empties = 0;
            for (int j = 0; j < m; j++)
            {
                Cuia cuia = sub[j];
                if (cuia.IsEmpty)
                {
                    empties++;
                }
                else if (cuia.IsFull && cuia.IsMonocolor)
                {
                    return false;
                }
            }

            if (empties != _empties)
            {
                return false;
            }

            Array.Copy(sub, _scratch, m);
            BoardCanon.Sort(_scratch, 0, m);
            for (int j = 0; j < m; j++)
            {
                if (_scratch[j].Packed != _currentCanon[j].Packed)
                {
                    return true;
                }
            }

            return false; // el mismo tablero que ahora (03 §16)
        }

        private void Compose(Cuia[] sub)
        {
            Array.Copy(_board, _candidate, _width);
            for (int j = 0; j < _movableCount; j++)
            {
                int position = _movable[j];
                _candidate[position] = Refill(_board[position], sub[j]);
            }
        }

        // El candidato vale si el A* lo resuelve con par ≤ par de partida + holgura (o, sin par de partida, si lo resuelve).
        private bool Verify(SolverOptions solve, int slack)
        {
            if (BoardRules.IsWon(_candidate, 0, _width, _units))
            {
                return false;
            }

            int par = Solve(_candidate, solve);
            if (par < 0 || (_parBefore >= 0 && par > _parBefore + slack))
            {
                return false;
            }

            _parAfter = par;
            return true;
        }

        private int Solve(Cuia[] cuias, SolverOptions solve)
        {
            SolveStatus status = _solver.SolveLength(cuias, 0, _width, _units, solve, out int length, out _, out _);
            return status == SolveStatus.Solved ? length : -1;
        }

        private void AddVisited(Cuia[] sub)
        {
            int m = _movableCount;
            Array.Copy(sub, _scratch, m);
            BoardCanon.Sort(_scratch, 0, m);
            _visited.Add(_scratch, 0, BoardCanon.Hash(_scratch, 0, m));
        }
    }
}
