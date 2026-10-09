#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;

namespace AF.Solver.Murky
{
    /// <summary>Resultado de <see cref="RealityEnumerator.Enumerate(BoardState, BoardState, bool, int)"/>.</summary>
    public enum RealityStatus
    {
        /// <summary>Enumeradas todas las realidades (como mucho el máximo).</summary>
        Ok = 0,

        /// <summary>Hay más realidades que el máximo (24 por defecto): el nivel se rechaza (02 T2; 03 §8).</summary>
        TooMany = 1,

        /// <summary>
        /// Ninguna asignación cuadra con lo visible, con <c>units_per_color</c> y con T4 (por ejemplo, una fruta con más dosis
        /// visibles que <c>U_c</c>): sin realidades, todo vertido sería «seguro» en vacío, así que es un error.
        /// </summary>
        Inconsistent = 2,
    }

    /// <summary>
    /// Realidades de Aguas turbias (02 §11 T2, T4 y <c>validar_turbio</c>; 03 §8; 08 §2.2: la enumeración vive en AF.Solver):
    /// todas las asignaciones de colores a las posiciones ocultas coherentes con lo visible, con <c>units_per_color</c> y con
    /// T4. Cada realidad es el tablero con esas dosis de ese color y la misma máscara de ocultos (las reglas siguen cortando
    /// la racha en ellas, R6), así que el solver juega en cada realidad la partida real.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><b>Multiconjunto oculto:</b> para cada color, <c>U_c</c> menos sus dosis visibles y sus gotas de Lluvia
    /// pendientes (02 R3). Los colores que el tablero guarda en las posiciones ocultas no se miran: el jugador no los
    /// conoce.</description></item>
    /// <item><description><b>T4</b> (02 §11): una dosis oculta nunca tiene el color de la dosis de encima. Si la de encima
    /// también está oculta, se compara con su color asignado; si es visible, con la dosis de encima en el tablero de
    /// referencia (normalmente el inicial del nivel), porque T4 habla de la dosis original: una dosis oculta nunca se mueve
    /// (R7), pero el lavado del Barro sustituye la dosis de encima sin revelar la oculta (02 §9.1 M5).</description></item>
    /// <item><description><b>Orden</b> determinista: posiciones por cuia ascendente y capa descendente; colores ascendentes.
    /// Cada asignación distinta sale una sola vez (permutaciones de un multiconjunto, sin duplicados).</description></item>
    /// <item><description>Si salen más de <c>maxRealities</c> (24 por defecto, 02 T2) se para y devuelve
    /// <see cref="RealityStatus.TooMany"/>: el nivel se rechaza.</description></item>
    /// <item><description>Sin asignaciones una vez creada (búferes de tamaño fijo; el de realidades crece solo si se pide un
    /// máximo mayor). No es segura entre hilos.</description></item>
    /// </list>
    /// </remarks>
    public sealed class RealityEnumerator
    {
        /// <summary>Máximo de realidades por defecto: 4 dosis ocultas → 4! = 24 (02 T2; 08 §9 <c>lvl_murky_max_hidden_units</c>).</summary>
        public const int DefaultMaxRealities = 24;

        /// <summary>Máximo de posiciones ocultas representables: 16 cuias × 5 capas bajo la cima.</summary>
        public const int MaxHiddenPositions = BoardState.MaxCuias * (Cuia.MaxCapacity - 1);

        private const int AboveIsHidden = -1;

        private readonly Cuia[] _base = new Cuia[BoardState.MaxCuias];
        private readonly int[] _units = new int[CuiaRules.UnitsTableLength];
        private readonly int[] _remaining = new int[CuiaRules.UnitsTableLength];
        private readonly int[] _posCuia = new int[MaxHiddenPositions];
        private readonly int[] _posLayer = new int[MaxHiddenPositions];
        private readonly int[] _posAbove = new int[MaxHiddenPositions];
        private readonly int[] _assigned = new int[MaxHiddenPositions];
        private Cuia[] _realities = new Cuia[(DefaultMaxRealities + 1) * BoardState.MaxCuias];
        private int _width;
        private int _positions;
        private int _count;
        private int _max;
        private bool _enforceT4;
        private bool _overflow;

        /// <summary>Resultado de la última enumeración.</summary>
        public RealityStatus Status { get; private set; }

        /// <summary>Realidades enumeradas (válidas si <see cref="Status"/> es <see cref="RealityStatus.Ok"/>).</summary>
        public int Count => _count;

        /// <summary>Cuias de cada realidad.</summary>
        public int Width => _width;

        /// <summary>Posiciones ocultas del tablero enumerado.</summary>
        public int HiddenPositions => _positions;

        /// <summary>Realidades guardadas en orden, <see cref="Width"/> cuias cada una, desde <c>realidad · Width</c>.</summary>
        internal Cuia[] Buffer => _realities;

        /// <summary>Tabla <c>U_c</c> del tablero enumerado (la misma en todas las realidades).</summary>
        internal int[] Units => _units;

        /// <summary>
        /// T4 (02 §11) en un tablero con los colores reales: ninguna dosis oculta repite el color de la dosis inmediatamente
        /// superior. El pipeline la exige al tablero inicial (03 §8: si no, RECHAZAR).
        /// </summary>
        public static bool SatisfiesT4(BoardState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            for (int i = 0; i < state.Count; i++)
            {
                Cuia cuia = state[i];
                for (int k = 0; k < cuia.Count - 1; k++)
                {
                    if (cuia.IsHiddenAt(k) && cuia.ColorAt(k) == cuia.ColorAt(k + 1))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Enumera las realidades de <paramref name="state"/>. Sin dosis ocultas hay una sola: el propio tablero.
        /// </summary>
        /// <param name="state">Tablero actual (los colores de sus posiciones ocultas no se usan).</param>
        /// <param name="t4Reference">Tablero de referencia de T4, normalmente el inicial del nivel; null = el propio
        /// <paramref name="state"/>.</param>
        /// <param name="enforceT4">Aplicar T4 (true en campaña). Con false solo cuenta el multiconjunto (como el prototipo de
        /// 03 §18.3, anterior a T4).</param>
        /// <param name="maxRealities">Máximo admitido (24 por defecto, 02 T2).</param>
        public RealityStatus Enumerate(BoardState state, BoardState? t4Reference = null, bool enforceT4 = true, int maxRealities = DefaultMaxRealities)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (maxRealities < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRealities), maxRealities, "Al menos 1 realidad.");
            }

            BoardState reference = t4Reference ?? state;
            return Enumerate(state.CuiasArray, 0, state.Count, state.UnitsTable, reference.CuiasArray, reference.Count, enforceT4, maxRealities);
        }

        /// <summary>Cuia <paramref name="index"/> de la realidad <paramref name="reality"/>.</summary>
        public Cuia GetCuia(int reality, int index)
        {
            if ((uint)reality >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(reality), reality, "Realidad fuera de la enumeración.");
            }

            if ((uint)index >= (uint)_width)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice de cuia fuera del tablero.");
            }

            return _realities[(reality * _width) + index];
        }

        /// <summary>La realidad <paramref name="reality"/> como tablero (asigna: para tests y herramientas).</summary>
        public BoardState GetReality(int reality)
        {
            if ((uint)reality >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(reality), reality, "Realidad fuera de la enumeración.");
            }

            var cuias = new Cuia[_width];
            Array.Copy(_realities, reality * _width, cuias, 0, _width);
            return BoardState.FromValidated(cuias, (int[])_units.Clone());
        }

        /// <summary>Ruta sin asignaciones de <see cref="Enumerate(BoardState, BoardState, bool, int)"/>.</summary>
        internal RealityStatus Enumerate(Cuia[] cuias, int offset, int width, int[] units, Cuia[] reference, int referenceWidth, bool enforceT4, int maxRealities)
        {
            _width = width;
            _count = 0;
            _positions = 0;
            _max = maxRealities;
            _enforceT4 = enforceT4;
            _overflow = false;
            int needed = (maxRealities + 1) * width;
            if (_realities.Length < needed)
            {
                _realities = new Cuia[needed]; // solo si se pide un máximo mayor que el de por defecto
            }

            Array.Copy(cuias, offset, _base, 0, width);
            Array.Copy(units, _units, CuiaRules.UnitsTableLength);
            Array.Copy(units, _remaining, CuiaRules.UnitsTableLength);
            int hiddenTotal = 0;
            for (int i = 0; i < width; i++)
            {
                Cuia cuia = _base[i];
                if (cuia.Mechanic == MechanicType.Lluvia)
                {
                    _remaining[cuia.MechanicKey] -= cuia.MechanicN; // gotas pendientes (02 R3)
                }

                for (int k = cuia.Count - 1; k >= 0; k--)
                {
                    if (!cuia.IsHiddenAt(k))
                    {
                        _remaining[cuia.ColorAt(k)]--;
                        continue;
                    }

                    // Posiciones por cuia y capa descendente: la de encima, si está oculta, ya tiene color al llegar aquí.
                    int p = _positions++;
                    _posCuia[p] = i;
                    _posLayer[p] = k;
                    _posAbove[p] = cuia.IsHiddenAt(k + 1) ? AboveIsHidden : ReferenceColor(reference, referenceWidth, i, k + 1);
                    hiddenTotal++;
                }
            }

            for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
            {
                if (_remaining[c] < 0)
                {
                    Status = RealityStatus.Inconsistent;
                    return Status;
                }

                hiddenTotal -= _remaining[c];
            }

            if (hiddenTotal != 0)
            {
                Status = RealityStatus.Inconsistent;
                return Status;
            }

            Assign(0);
            Status = _overflow ? RealityStatus.TooMany : _count == 0 ? RealityStatus.Inconsistent : RealityStatus.Ok;
            return Status;
        }

        private int ReferenceColor(Cuia[] reference, int referenceWidth, int cuia, int layer)
        {
            if (cuia < referenceWidth && layer < reference[cuia].Count)
            {
                return reference[cuia].ColorAt(layer);
            }

            return _base[cuia].ColorAt(layer);
        }

        /// <summary>Asigna colores a las posiciones desde <paramref name="p"/> (vuelta atrás, sin asignaciones).</summary>
        private void Assign(int p)
        {
            if (_overflow)
            {
                return;
            }

            if (p == _positions)
            {
                Emit();
                return;
            }

            int above = _posAbove[p] == AboveIsHidden ? _assigned[p - 1] : _posAbove[p];
            for (int color = Cuia.MinColor; color <= Cuia.MaxColor && !_overflow; color++)
            {
                if (_remaining[color] == 0 || (_enforceT4 && color == above))
                {
                    continue;
                }

                _remaining[color]--;
                _assigned[p] = color;
                Assign(p + 1);
                _remaining[color]++;
            }
        }

        private void Emit()
        {
            if (_count == _max)
            {
                _overflow = true;
                return;
            }

            int start = _count * _width;
            Array.Copy(_base, 0, _realities, start, _width);
            for (int p = 0; p < _positions; p++)
            {
                int at = start + _posCuia[p];
                _realities[at] = _realities[at].WithColorAt(_posLayer[p], _assigned[p]);
            }

            _count++;
        }
    }
}
