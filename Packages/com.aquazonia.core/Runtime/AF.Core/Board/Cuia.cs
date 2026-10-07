#nullable enable
using System;
using System.Globalization;
using System.Text;

namespace AF.Core.Board
{
    /// <summary>
    /// Estado de una cuia (GDD 02 §1.1, reglas R1-R7): valor inmutable empaquetado en un
    /// <see cref="ulong"/>, con longitud y 4 bits por dosis (tinte − 1) más la máscara de
    /// dosis ocultas (GDD 08 §2.2).
    /// </summary>
    /// <remarks>
    /// Distribución de bits de <see cref="Packed"/>:
    /// <list type="bullet">
    /// <item><description>0-23: dosis, 6 × 4 bits. La dosis k, contada desde el fondo, ocupa los bits 4k..4k+3 y guarda color − 1.</description></item>
    /// <item><description>24-29: máscara de ocultos. El bit 24+k indica que la dosis k está oculta.</description></item>
    /// <item><description>32-34: número de dosis (0..6).</description></item>
    /// <item><description>36-38: capacidad (1..6).</description></item>
    /// </list>
    /// Los bits de dosis por encima de la cima valen siempre 0, así que dos cuias iguales tienen el mismo
    /// <see cref="Packed"/>. <c>default(Cuia)</c> no es una cuia válida (capacidad 0).
    /// </remarks>
    public readonly struct Cuia : IEquatable<Cuia>
    {
        /// <summary>Capacidad mínima de una cuia (R1).</summary>
        public const int MinCapacity = 1;

        /// <summary>Capacidad máxima de una cuia (R1).</summary>
        public const int MaxCapacity = 6;

        /// <summary>Capacidad por defecto (R1, dirección 2 y 4).</summary>
        public const int DefaultCapacity = 4;

        /// <summary>Valor que devuelve <see cref="TopColor"/> en una cuia vacía.</summary>
        public const int NoColor = 0;

        /// <summary>Primer color válido (R2).</summary>
        public const int MinColor = 1;

        /// <summary>Último tinte jugable: los IDs 1-14 son los tintes de la paleta (R2).</summary>
        public const int MaxTintColor = 14;

        /// <summary>Barro: inerte y no jugable (R2; mecánica de GDD 02 §9).</summary>
        public const int MudColor = 15;

        /// <summary>ID reservado (R2).</summary>
        public const int ReservedColor = 16;

        /// <summary>Último color representable: K_motor = 16 (R2, dirección 3).</summary>
        public const int MaxColor = 16;

        private const int BitsPerDose = 4;
        private const ulong DoseMask = 0xFUL;
        private const ulong DosesFieldMask = 0xFFFFFFUL;
        private const int HiddenShift = 24;
        private const ulong HiddenFieldMask = 0x3FUL;
        private const int CountShift = 32;
        private const int CapacityShift = 36;
        private const ulong ThreeBitMask = 0x7UL;

        private readonly ulong _bits;

        private Cuia(ulong bits)
        {
            _bits = bits;
        }

        /// <summary>Crea una cuia vacía de la capacidad indicada.</summary>
        public static Cuia Empty(int capacity)
        {
            ValidateCapacity(capacity);
            return new Cuia((ulong)capacity << CapacityShift);
        }

        /// <summary>Crea una cuia con todas sus dosis visibles.</summary>
        /// <param name="capacity">Capacidad, de 1 a 6 (R1).</param>
        /// <param name="colorsBottomToTop">Colores de las dosis, del fondo a la cima (R2).</param>
        public static Cuia Create(int capacity, int[] colorsBottomToTop)
        {
            return Create(capacity, colorsBottomToTop, 0);
        }

        /// <summary>Crea una cuia con dosis ocultas (Aguas turbias).</summary>
        /// <param name="capacity">Capacidad, de 1 a 6 (R1).</param>
        /// <param name="colorsBottomToTop">Colores de las dosis, del fondo a la cima (R2).</param>
        /// <param name="hiddenMask">Bit k = la dosis k (desde el fondo) está oculta. La cima nunca puede estar oculta (R7).</param>
        public static Cuia Create(int capacity, int[] colorsBottomToTop, int hiddenMask)
        {
            ValidateCapacity(capacity);
            if (colorsBottomToTop == null)
            {
                throw new ArgumentNullException(nameof(colorsBottomToTop));
            }

            int count = colorsBottomToTop.Length;
            if (count > capacity)
            {
                throw new ArgumentException("R1: una cuia no puede tener más dosis que su capacidad.", nameof(colorsBottomToTop));
            }

            if (hiddenMask < 0 || (hiddenMask >> count) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hiddenMask), hiddenMask, "La máscara de ocultos solo puede marcar dosis existentes.");
            }

            if (count > 0 && (hiddenMask & (1 << (count - 1))) != 0)
            {
                throw new ArgumentException("R7: la cima de una cuia no vacía siempre es visible.", nameof(hiddenMask));
            }

            ulong bits = ((ulong)capacity << CapacityShift)
                | ((ulong)count << CountShift)
                | ((ulong)hiddenMask << HiddenShift);
            for (int i = 0; i < count; i++)
            {
                int color = colorsBottomToTop[i];
                ValidateColor(color, nameof(colorsBottomToTop));
                bits |= (ulong)(color - 1) << (i * BitsPerDose);
            }

            return new Cuia(bits);
        }

        /// <summary>Capacidad <c>cap_i</c> (R1).</summary>
        public int Capacity => (int)((_bits >> CapacityShift) & ThreeBitMask);

        /// <summary>Número de dosis que contiene.</summary>
        public int Count => (int)((_bits >> CountShift) & ThreeBitMask);

        /// <summary>Huecos libres: <c>cap_i − dosis</c>.</summary>
        public int Space => Capacity - Count;

        /// <summary>Vacía: sin dosis (R4).</summary>
        public bool IsEmpty => Count == 0;

        /// <summary>Llena: tiene <c>cap_i</c> dosis (R4).</summary>
        public bool IsFull => Count == Capacity;

        /// <summary>Máscara de dosis ocultas (bit k = dosis k desde el fondo).</summary>
        public int HiddenMask => (int)((_bits >> HiddenShift) & HiddenFieldMask);

        /// <summary>Tiene al menos una dosis oculta.</summary>
        public bool HasHidden => HiddenMask != 0;

        /// <summary>Representación empaquetada; estable entre CoreCLR, Mono e IL2CPP. Base del hash y de la canonicalización.</summary>
        public ulong Packed => _bits;

        /// <summary>Color de la cima, o <see cref="NoColor"/> si está vacía. La cima siempre es visible (R7).</summary>
        public int TopColor => IsEmpty ? NoColor : RawColorAt(Count - 1);

        /// <summary>
        /// Longitud de la racha superior visible (R6): dosis contiguas desde la cima con el mismo color
        /// y visibles. Una dosis oculta corta la racha aunque su color coincida.
        /// </summary>
        public int TopRunLength
        {
            get
            {
                int count = Count;
                if (count == 0)
                {
                    return 0;
                }

                int top = RawColorAt(count - 1);
                int hidden = HiddenMask;
                int run = 0;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (RawColorAt(i) != top || ((hidden >> i) & 1) != 0)
                    {
                        break;
                    }

                    run++;
                }

                return run;
            }
        }

        /// <summary>
        /// Monocolor (R4): todas sus dosis tienen el mismo color, ignorando si son visibles.
        /// Una cuia vacía es monocolor por vacuidad, como <c>is_mono</c> en <c>tools/levelgen_proto.py</c>.
        /// </summary>
        public bool IsMonocolor
        {
            get
            {
                int count = Count;
                if (count <= 1)
                {
                    return true;
                }

                int first = RawColorAt(0);
                for (int i = 1; i < count; i++)
                {
                    if (RawColorAt(i) != first)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Color de la dosis <paramref name="index"/>, contada desde el fondo (0).</summary>
        public int ColorAt(int index)
        {
            CheckDoseIndex(index);
            return RawColorAt(index);
        }

        /// <summary>Indica si la dosis <paramref name="index"/>, contada desde el fondo (0), está oculta.</summary>
        public bool IsHiddenAt(int index)
        {
            CheckDoseIndex(index);
            return ((_bits >> (HiddenShift + index)) & 1UL) != 0;
        }

        /// <summary>
        /// Devuelve la cuia sin sus <paramref name="amount"/> dosis superiores. Aplica R7: si la nueva
        /// cima estaba oculta, queda visible.
        /// </summary>
        public Cuia RemoveTop(int amount)
        {
            int count = Count;
            if (amount < 1 || amount > count)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Solo se pueden quitar entre 1 y el número de dosis.");
            }

            int newCount = count - amount;
            ulong doses = _bits & DosesFieldMask & ((1UL << (newCount * BitsPerDose)) - 1UL);
            int hidden = HiddenMask & ((1 << newCount) - 1);
            if (newCount > 0)
            {
                hidden &= ~(1 << (newCount - 1));
            }

            return new Cuia(((ulong)Capacity << CapacityShift)
                | ((ulong)newCount << CountShift)
                | ((ulong)hidden << HiddenShift)
                | doses);
        }

        /// <summary>Devuelve la cuia con <paramref name="amount"/> dosis visibles de <paramref name="color"/> sobre la cima.</summary>
        public Cuia AddOnTop(int color, int amount)
        {
            ValidateColor(color, nameof(color));
            int count = Count;
            if (amount < 1 || amount > Capacity - count)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Solo caben entre 1 y los huecos libres de la cuia.");
            }

            ulong bits = _bits & ~(ThreeBitMask << CountShift);
            for (int i = count; i < count + amount; i++)
            {
                bits |= (ulong)(color - 1) << (i * BitsPerDose);
            }

            bits |= (ulong)(count + amount) << CountShift;
            return new Cuia(bits);
        }

        /// <inheritdoc />
        public bool Equals(Cuia other)
        {
            return _bits == other._bits;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Cuia other && _bits == other._bits;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return unchecked((int)_bits ^ (int)(_bits >> 32));
        }

        /// <summary>Igualdad por valor.</summary>
        public static bool operator ==(Cuia left, Cuia right)
        {
            return left._bits == right._bits;
        }

        /// <summary>Desigualdad por valor.</summary>
        public static bool operator !=(Cuia left, Cuia right)
        {
            return left._bits != right._bits;
        }

        /// <summary>Forma legible para tests y registros, del fondo a la cima: <c>Cuia(4: 3* 1 1)</c>; <c>*</c> = oculta.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder(32);
            sb.Append("Cuia(").Append(Capacity.ToString(CultureInfo.InvariantCulture)).Append(':');
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                sb.Append(' ').Append(RawColorAt(i).ToString(CultureInfo.InvariantCulture));
                if (((_bits >> (HiddenShift + i)) & 1UL) != 0)
                {
                    sb.Append('*');
                }
            }

            return sb.Append(')').ToString();
        }

        private int RawColorAt(int index)
        {
            return (int)((_bits >> (index * BitsPerDose)) & DoseMask) + 1;
        }

        private void CheckDoseIndex(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Índice de dosis fuera de la cuia.");
            }
        }

        private static void ValidateCapacity(int capacity)
        {
            if (capacity < MinCapacity || capacity > MaxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "R1: la capacidad de una cuia va de 1 a 6.");
            }
        }

        private static void ValidateColor(int color, string paramName)
        {
            if (color < MinColor || color > MaxColor)
            {
                throw new ArgumentOutOfRangeException(paramName, color, "R2: el color de una dosis va de 1 a 16 (K_motor).");
            }
        }
    }
}
