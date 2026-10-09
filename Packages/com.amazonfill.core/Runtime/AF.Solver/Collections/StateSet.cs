#nullable enable
using System;
using AF.Core.Board;

namespace AF.Solver.Collections
{
    /// <summary>
    /// Conjunto de estados canónicos (03 §3) para los generadores (visitados de <c>WALK</c> y <c>DEEP</c>, 03 §4): tabla
    /// hash abierta sobre arrays con la comparación completa de las cuias (sin colisiones de hash posibles) y sin
    /// <c>Dictionary</c>. Solo asigna al crecer; se reutiliza con <see cref="Reset"/>.
    /// </summary>
    public sealed class StateSet
    {
        private int _width;
        private int _count;
        private int _capacity;
        private Cuia[] _states;
        private ulong[] _hashes;
        private int[] _table;
        private int _mask;

        /// <summary>Crea un conjunto con espacio inicial para <paramref name="initialCapacity"/> estados.</summary>
        public StateSet(int initialCapacity = 256)
        {
            if (initialCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity, "Al menos 1.");
            }

            _capacity = initialCapacity;
            _states = Array.Empty<Cuia>();
            _hashes = new ulong[initialCapacity];
            int size = 16;
            while (size < initialCapacity * 2)
            {
                size <<= 1;
            }

            _table = new int[size];
            _mask = size - 1;
        }

        /// <summary>Estados guardados.</summary>
        public int Count => _count;

        /// <summary>Vacía el conjunto para tableros de <paramref name="width"/> cuias.</summary>
        public void Reset(int width)
        {
            _width = width;
            _count = 0;
            if (_states.Length < _capacity * width)
            {
                _states = new Cuia[_capacity * width];
            }

            Array.Clear(_table, 0, _table.Length);
        }

        /// <summary>El estado canónico de <paramref name="canonical"/> (desde <paramref name="offset"/>) ya está.</summary>
        public bool Contains(Cuia[] canonical, int offset, ulong hash)
        {
            return Find(canonical, offset, hash) >= 0;
        }

        /// <summary>Añade el estado si no estaba; devuelve false si ya estaba.</summary>
        public bool Add(Cuia[] canonical, int offset, ulong hash)
        {
            if (Find(canonical, offset, hash) >= 0)
            {
                return false;
            }

            if (_count == _capacity)
            {
                Grow();
            }

            int index = _count++;
            Array.Copy(canonical, offset, _states, index * _width, _width);
            _hashes[index] = hash;
            Insert(index, hash);
            return true;
        }

        private int Find(Cuia[] canonical, int offset, ulong hash)
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
                if (_hashes[index] == hash && Same(index, canonical, offset))
                {
                    return index;
                }

                slot = (slot + 1) & _mask;
            }
        }

        private bool Same(int index, Cuia[] canonical, int offset)
        {
            int start = index * _width;
            for (int k = 0; k < _width; k++)
            {
                if (_states[start + k].Packed != canonical[offset + k].Packed)
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
            var states = new Cuia[capacity * _width];
            Array.Copy(_states, states, _count * _width);
            _states = states;
            var hashes = new ulong[capacity];
            Array.Copy(_hashes, hashes, _count);
            _hashes = hashes;
            _capacity = capacity;
            if (capacity * 2 > _table.Length)
            {
                _table = new int[_table.Length * 2];
                _mask = _table.Length - 1;
                for (int i = 0; i < _count; i++)
                {
                    Insert(i, _hashes[i]);
                }
            }
        }
    }
}
