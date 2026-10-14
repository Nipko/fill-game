#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;

namespace AF.Core.Session
{
    /// <summary>
    /// Cierre acotado de Aguas estancadas por bucle (02 R13 ampliada; D-014): un estado en el que todavía hay vertidos útiles
    /// está igualmente perdido si todos los estados que se alcanzan desde él con todos los vertidos legales (opción (a) de la
    /// pregunta 46 de <c>docs/GDD.md</c> §6.3, decidida el 2026-10-13; D-020) son pocos (como mucho
    /// <see cref="MaxStates"/>) y ninguno es victoria (R12). Es el caso del nivel 66 (prueba en el teléfono del 2026-10-13): dos
    /// vaivenes parciales útiles que llevan siempre a los mismos tableros, sin salida.
    /// </summary>
    /// <remarks>
    /// <para><b>Recorrido.</b> En anchura desde S, en orden determinista (vertidos (A, B) ascendentes sobre el tablero en orden
    /// canónico), con estados canónicos: cuias ordenadas con <see cref="BoardCanon.Sort"/> y hash FNV-1a de 64 bits de
    /// <see cref="BoardCanon.Hash"/>; la igualdad se confirma por valor (<see cref="Cuia.Packed"/> cuia a cuia), nunca solo por el
    /// hash. Los vertidos se evalúan y se aplican con <see cref="BoardRules"/> (R10 (a)-(f′), la misma implementación que la
    /// partida y el solver), así que las mecánicas con estado (Hoja, Brote, Liana, contadores de Lluvia y Piraña…) forman parte
    /// del estado y del hash. Las cuias idénticas se prueban una sola vez como origen y como destino, como en el solver (dan los
    /// mismos hijos canónicos). Se recorren <b>todos los vertidos legales</b>: los útiles y también los fútiles (con su
    /// <c>futil</c> de R13 y el anexo B, que decide que un fútil no avanza los contadores, Q18 B). Con capacidades iguales, un
    /// fútil entre cuias sin mecánica no cambia el estado canónico en <see cref="PourMode.Run"/> (P2 de R16), así que no añade
    /// estados; con capacidades mixtas sí puede abrir un camino a la victoria (CHANGELOG del núcleo, C1-8), y dejarlo fuera
    /// haría que el cierre declarase perdido un tablero que se gana (el texto de D-014 dice «vertidos útiles», pero también
    /// exige que nunca se declare un tablero ganable; en el propio nivel 66, tras el 6.º vertido del dueño, el cierre solo con
    /// útiles no tiene victoria y el nivel se gana con un fútil). Por lo mismo no depende de <c>futile_rule_enabled</c>: el
    /// kill-switch de R16 solo cambia R13. Es la opción (a) de la pregunta 46 de <c>docs/GDD.md</c> §6.3, la recomendada,
    /// decidida por el dueño el 2026-10-13 (D-020).</para>
    /// <para><b>Veredicto.</b> Si el cierre se agota con <see cref="MaxStates"/> estados o menos (S incluido) y ninguno es
    /// victoria, S está perdido (<see cref="IsDeadEnd(BoardState, PourMode)"/> = true). Si aparece un estado ganado o haría falta
    /// guardar uno más, no se declara nada: es conservador y nunca declara perdido un tablero desde el que se puede ganar (R14).
    /// El veredicto no depende del orden del recorrido (el cierre es un conjunto). Con Aguas turbias usa el estado verdadero: el
    /// núcleo conoce las dosis ocultas y un vertido las revela como en la partida (R7).</para>
    /// <para><b>Bucle caliente</b> (CLAUDE.md, reglas duras 1 y 2): sin LINQ, sin closures y sin <c>new</c> por nodo. Los búferes
    /// (<see cref="MaxStates"/> estados de ancho T, una tabla hash abierta de 128 entradas y el tablero hijo) se reservan una vez
    /// y solo crecen si llega un tablero más ancho que los anteriores; después no asigna.</para>
    /// <para>No es seguro entre hilos: una instancia por partida (<see cref="LevelSession"/> tiene la suya).</para>
    /// </remarks>
    public sealed class DeadEndProbe
    {
        /// <summary>
        /// Máximo de estados del cierre (<c>DeadEndProbeMaxStates</c>, 64 [P]; D-014): con más, no se declara Aguas estancadas.
        /// Constante del núcleo, no de Remote Config.
        /// </summary>
        public const int MaxStates = 64;

        // Tabla hash abierta: potencia de 2 y al menos el doble de MaxStates, para que las búsquedas sean cortas.
        private const int TableSize = 128;
        private const int TableMask = TableSize - 1;

        private readonly ulong[] _hashes = new ulong[MaxStates];
        private readonly int[] _table = new int[TableSize];
        private readonly Cuia[] _child = new Cuia[BoardState.MaxCuias];
        private Cuia[] _nodes = Array.Empty<Cuia>();
        private int _width;
        private int _count;

        /// <summary>
        /// Estados distintos que guardó la última llamada, S incluido (de 1 a <see cref="MaxStates"/>; 0 si S ya era victoria).
        /// Diagnóstico para tests y registros: con veredicto false puede quedarse corto, porque el recorrido para al encontrar una
        /// victoria o al llenarse.
        /// </summary>
        public int LastStateCount => _count;

        /// <summary>
        /// El cierre de estados alcanzables desde <paramref name="state"/> con los vertidos de <paramref name="mode"/> tiene
        /// como mucho <see cref="MaxStates"/> estados y ninguno es victoria (02 R13 ampliada, D-014). Precondición del uso en
        /// la partida: <paramref name="state"/> no es victoria (R10 g); si lo es, devuelve false.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> es null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> no es un modo definido.</exception>
        public bool IsDeadEnd(BoardState state, PourMode mode)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            PourModes.Check(mode, nameof(mode));
            return IsDeadEnd(state.CuiasArray, 0, state.Count, state.UnitsTable, mode);
        }

        /// <summary>
        /// Como <see cref="IsDeadEnd(BoardState, PourMode)"/> sobre <paramref name="width"/> cuias de un array desde
        /// <paramref name="offset"/> y la tabla <c>U_c</c> (0..16). No modifica el array ni valida argumentos (bucle caliente).
        /// </summary>
        internal bool IsDeadEnd(Cuia[] cuias, int offset, int width, int[] units, PourMode mode)
        {
            Reset(width);
            Array.Copy(cuias, offset, _child, 0, width);
            BoardCanon.Sort(_child, 0, width);
            var root = new BoardSlice(_child, 0, width, units);
            if (BoardRules.IsWon(in root))
            {
                return false;
            }

            Add(BoardCanon.Hash(_child, 0, width));
            for (int head = 0; head < _count; head++)
            {
                if (!Expand(head, units, mode))
                {
                    return false; // victoria alcanzable o más de MaxStates estados: no se declara (conservador)
                }
            }

            return true;
        }

        /// <summary>Prepara los búferes para un tablero de <paramref name="width"/> cuias y vacía el cierre.</summary>
        private void Reset(int width)
        {
            if (_nodes.Length < MaxStates * width)
            {
                _nodes = new Cuia[MaxStates * width]; // solo con un tablero más ancho que los anteriores, fuera del recorrido
            }

            _width = width;
            _count = 0;
            Array.Clear(_table, 0, TableSize);
        }

        /// <summary>
        /// Aplica cada vertido del estado <paramref name="node"/> y guarda los hijos nuevos. Devuelve false si un hijo es victoria
        /// o si no cabe (el cierre pasaría de <see cref="MaxStates"/>).
        /// </summary>
        private bool Expand(int node, int[] units, PourMode mode)
        {
            int width = _width;
            int offset = node * width;
            var parent = new BoardSlice(_nodes, offset, width, units);
            var child = new BoardSlice(_child, 0, width, units);
            for (int a = 0; a < width; a++)
            {
                Cuia from = parent[a];
                if (!BoardRules.IsSourceCuia(from, units))
                {
                    continue;
                }

                if (a > 0 && from.Packed == parent[a - 1].Packed)
                {
                    continue; // origen idéntico al anterior: mismos hijos canónicos
                }

                for (int b = 0; b < width; b++)
                {
                    if (b == a)
                    {
                        continue;
                    }

                    Cuia to = parent[b];
                    if (b > 0 && b - 1 != a && to.Packed == parent[b - 1].Packed)
                    {
                        continue; // destino idéntico al anterior
                    }

                    if (!BoardRules.Accepts(in parent, a, b))
                    {
                        continue;
                    }

                    // Útil o fútil, cuenta: el fútil también es legal (R13). Su futil solo decide que no avance contadores (Q18 B).
                    bool futile = BoardRules.IsFutileCuias(from, to, units);
                    Array.Copy(_nodes, offset, _child, 0, width);
                    var events = default(PourEvents);
                    BoardRules.Apply(in child, a, b, mode, futile, ref events);
                    BoardCanon.Sort(_child, 0, width);
                    ulong hash = BoardCanon.Hash(_child, 0, width);
                    if (Contains(hash))
                    {
                        continue;
                    }

                    if (BoardRules.IsWon(in child) || _count == MaxStates)
                    {
                        return false;
                    }

                    Add(hash);
                }
            }

            return true;
        }

        /// <summary>El estado de <see cref="_child"/> ya está en el cierre (mismo hash y mismas cuias).</summary>
        private bool Contains(ulong hash)
        {
            int width = _width;
            int slot = Slot(hash);
            while (true)
            {
                int entry = _table[slot];
                if (entry == 0)
                {
                    return false;
                }

                int node = entry - 1;
                if (_hashes[node] == hash && SameAsChild(node * width, width))
                {
                    return true;
                }

                slot = (slot + 1) & TableMask;
            }
        }

        private bool SameAsChild(int offset, int width)
        {
            for (int k = 0; k < width; k++)
            {
                if (_nodes[offset + k].Packed != _child[k].Packed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Guarda <see cref="_child"/> como estado nuevo del cierre (el llamante sabe que no está y que cabe).</summary>
        private void Add(ulong hash)
        {
            int node = _count++;
            Array.Copy(_child, 0, _nodes, node * _width, _width);
            _hashes[node] = hash;
            int slot = Slot(hash);
            while (_table[slot] != 0)
            {
                slot = (slot + 1) & TableMask;
            }

            _table[slot] = node + 1;
        }

        private static int Slot(ulong hash)
        {
            return unchecked((int)hash ^ (int)(hash >> 32)) & TableMask;
        }
    }
}
