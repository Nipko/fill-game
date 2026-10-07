#nullable enable
using System;

namespace AF.Solver.Collections
{
    /// <summary>
    /// Montículo binario de mínimos: valores <see cref="int"/> con prioridad <see cref="int"/>.
    /// Sustituye a <c>PriorityQueue</c>, que no existe en .NET Standard 2.1; lo usará el A* del solver
    /// (GDD 08 §2.2).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Desempate total: a igual prioridad sale primero el que entró antes (FIFO). Así el orden es
    /// el mismo en CoreCLR, Mono e IL2CPP (informe D §3.5).</description></item>
    /// <item><description>Sin asignaciones en <see cref="Push"/> ni en <see cref="TryPop"/>, salvo cuando el
    /// array interno tiene que crecer (duplica su tamaño).</description></item>
    /// </list>
    /// </remarks>
    public sealed class MinHeap
    {
        private Entry[] _entries;
        private int _count;
        private long _nextSequence;

        /// <summary>Crea un montículo vacío con capacidad inicial <paramref name="initialCapacity"/>.</summary>
        public MinHeap(int initialCapacity)
        {
            if (initialCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity, "La capacidad inicial debe ser al menos 1.");
            }

            _entries = new Entry[initialCapacity];
        }

        /// <summary>Elementos en el montículo.</summary>
        public int Count => _count;

        /// <summary>Tamaño actual del array interno.</summary>
        public int Capacity => _entries.Length;

        /// <summary>Vacía el montículo sin liberar el array (reutilizable entre búsquedas).</summary>
        public void Clear()
        {
            _count = 0;
            _nextSequence = 0;
        }

        /// <summary>Inserta <paramref name="value"/> con prioridad <paramref name="priority"/> (menor sale antes).</summary>
        public void Push(int value, int priority)
        {
            if (_count == _entries.Length)
            {
                Grow();
            }

            var entry = new Entry(priority, _nextSequence++, value);
            int i = _count++;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (!Precedes(entry, _entries[parent]))
                {
                    break;
                }

                _entries[i] = _entries[parent];
                i = parent;
            }

            _entries[i] = entry;
        }

        /// <summary>Saca el valor de menor prioridad.</summary>
        /// <exception cref="InvalidOperationException">El montículo está vacío.</exception>
        public int Pop()
        {
            if (!TryPop(out int value, out _))
            {
                throw new InvalidOperationException("El montículo está vacío.");
            }

            return value;
        }

        /// <summary>Saca el valor de menor prioridad si existe.</summary>
        public bool TryPop(out int value, out int priority)
        {
            if (_count == 0)
            {
                value = 0;
                priority = 0;
                return false;
            }

            Entry top = _entries[0];
            value = top.Value;
            priority = top.Priority;

            _count--;
            if (_count > 0)
            {
                Entry last = _entries[_count];
                int i = 0;
                while (true)
                {
                    int left = (i << 1) + 1;
                    if (left >= _count)
                    {
                        break;
                    }

                    int right = left + 1;
                    int child = right < _count && Precedes(_entries[right], _entries[left]) ? right : left;
                    if (!Precedes(_entries[child], last))
                    {
                        break;
                    }

                    _entries[i] = _entries[child];
                    i = child;
                }

                _entries[i] = last;
            }

            return true;
        }

        private static bool Precedes(in Entry a, in Entry b)
        {
            return a.Priority < b.Priority || (a.Priority == b.Priority && a.Sequence < b.Sequence);
        }

        private void Grow()
        {
            var bigger = new Entry[_entries.Length * 2];
            Array.Copy(_entries, bigger, _count);
            _entries = bigger;
        }

        private readonly struct Entry
        {
            public readonly int Priority;
            public readonly long Sequence;
            public readonly int Value;

            public Entry(int priority, long sequence, int value)
            {
                Priority = priority;
                Sequence = sequence;
                Value = value;
            }
        }
    }
}
