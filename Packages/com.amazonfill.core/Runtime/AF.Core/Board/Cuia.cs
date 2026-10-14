#nullable enable
using System;
using System.Globalization;
using System.Text;
using AF.Core.Mechanics;

namespace AF.Core.Board
{
    /// <summary>
    /// Estado de una cuia (GDD 02 §1.1, reglas R1-R7): valor inmutable empaquetado en un
    /// <see cref="ulong"/>, con longitud y 4 bits por dosis (tinte − 1), la máscara de
    /// dosis ocultas y los atributos de su mecánica (GDD 08 §2.2, §4.1; 03 §3).
    /// </summary>
    /// <remarks>
    /// Distribución de bits de <see cref="Packed"/>:
    /// <list type="bullet">
    /// <item><description>0-23: dosis, 6 × 4 bits. La dosis k, contada desde el fondo, ocupa los bits 4k..4k+3 y guarda color − 1.</description></item>
    /// <item><description>24-29: máscara de ocultos. El bit 24+k indica que la dosis k está oculta.</description></item>
    /// <item><description>30-31: libres (0).</description></item>
    /// <item><description>32-34: número de dosis (0..6).</description></item>
    /// <item><description>35: libre (0).</description></item>
    /// <item><description>36-38: capacidad (1..6).</description></item>
    /// <item><description>39-42: tipo de mecánica (<see cref="MechanicType"/>; 0 = ninguna).</description></item>
    /// <item><description>43-46: <c>key</c>, tinte − 1 (Hoja, Hoja atada, Liana con fruta, Lluvia, Flor de vitória-régia).</description></item>
    /// <item><description>47-49: id de la pareja del Remolino (0..7), asignado por el constructor de tableros.</description></item>
    /// <item><description>50-52: <c>n</c> (Liana de cuenta: sellos; Lluvia: gotas pendientes; Brote de dos hojas: hojas cerradas
    /// que le faltan, 2 o 1, y 0 en el Brote normal).</description></item>
    /// <item><description>53-55: <c>k</c>, ritmo en vertidos de Lluvia y Piraña (2-5, 08 §9).</description></item>
    /// <item><description>En la Flor de vitória-régia, que no usa <c>n</c> ni <c>k</c>, los bits 50-53 guardan <c>key2</c>, tinte
    /// − 1, y los 54-55 valen 0 (K3b; firma de <c>pack_schema</c> 3, D-023, punto 5). Las demás mecánicas no cambian de bits ni de
    /// hash.</description></item>
    /// <item><description>56-58: contador de vertidos (Lluvia, Piraña).</description></item>
    /// <item><description>59-60: banderas (bit 59 = cerrada: Hoja puesta, Brote cerrado; bit 60 reservado).</description></item>
    /// <item><description>61-63: libres (0).</description></item>
    /// </list>
    /// Los bits de dosis por encima de la cima y los libres valen siempre 0, así que dos cuias iguales tienen el mismo
    /// <see cref="Packed"/>, y <see cref="Packed"/> es la clave canónica <c>(cap, attr, contenido)</c> más los contadores de
    /// 03 §3. Una cuia sin mecánica tiene a 0 los bits 39-63: los tableros sin mecánicas conservan su <see cref="Packed"/> y
    /// su hash. Cuando una mecánica se abre o se suelta, la cuia pasa a no tener mecánica (todos esos bits a 0; 02 R16 para
    /// D-007). <c>default(Cuia)</c> no es una cuia válida (capacidad 0).
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

        /// <summary>Bandera de <see cref="MechanicFlags"/>: la mecánica está cerrada (Hoja puesta, Brote cerrado; 03 §9).</summary>
        public const int FlagClosed = 1;

        /// <summary>Máximo del id de pareja de Remolino, de <c>n</c>, de <c>k</c> y del contador (3 bits).</summary>
        public const int MaxMechanicField = 7;

        // Campo de mecánica (bits 39-63). Con 0 la cuia es normal: los tableros sin mecánicas no cambian su Packed.
        private const int MechanicShift = 39;
        private const ulong MechanicFieldMask = 0x1FFFFFFUL << MechanicShift;
        private const ulong MechanicTypeMask = 0xFUL;
        private const int KeyShift = 43;
        private const int PairShift = 47;
        private const int NShift = 50;
        private const int KShift = 53;
        private const int Key2Shift = 50; // solo en la Flor, que no usa n ni k: reutiliza sus bits (50-53)
        private const int FlorCode = (int)MechanicType.Flor;
        private const int CounterShift = 56;
        private const int FlagsShift = 59;
        private const ulong FlagsMask = 0x3UL;

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

        /// <summary>Número de dosis ocultas (Aguas turbias, 02 §11).</summary>
        public int HiddenCount
        {
            get
            {
                int mask = HiddenMask;
                int count = 0;
                while (mask != 0)
                {
                    mask &= mask - 1;
                    count++;
                }

                return count;
            }
        }

        /// <summary>Representación empaquetada; estable entre CoreCLR, Mono e IL2CPP. Base del hash y de la canonicalización.</summary>
        public ulong Packed => _bits;

        /// <summary>Código del tipo de mecánica (bits 39-42; 0 = cuia normal). Índice de la tabla de despacho de AF.Core.Mechanics.</summary>
        internal int MechanicCode => (int)((_bits >> MechanicShift) & MechanicTypeMask);

        /// <summary>La cuia lleva una mecánica (02 §9): algún bit del campo de mecánica vale 1.</summary>
        public bool HasMechanic => (_bits & MechanicFieldMask) != 0;

        /// <summary>Mecánica de la cuia (02 §9.1; <see cref="MechanicType.None"/> si es normal).</summary>
        public MechanicType Mechanic => (MechanicType)MechanicCode;

        /// <summary>
        /// Tinte <c>key</c> de la mecánica (1-16): la fruta de la Hoja, de la Hoja atada o de la Liana con fruta, la primera
        /// llave de la Flor de vitória-régia, o el tinte agua de la Lluvia. <see cref="NoColor"/> si la mecánica no usa
        /// <c>key</c> (también la Liana de cuenta, con <c>n</c> &gt; 0).
        /// </summary>
        public int MechanicKey
        {
            get
            {
                MechanicType type = Mechanic;
                bool usesKey = type == MechanicType.Hoja
                    || type == MechanicType.HojaAtada
                    || type == MechanicType.Lluvia
                    || type == MechanicType.Flor
                    || (type == MechanicType.Liana && MechanicN == 0);
                return usesKey ? (int)((_bits >> KeyShift) & DoseMask) + 1 : NoColor;
            }
        }

        /// <summary>
        /// Segunda llave <c>key2</c> de la Flor de vitória-régia (1-16; bits 50-53, los de <c>n</c> y <c>k</c>, que la Flor no
        /// usa; K3b). <see cref="NoColor"/> en cualquier otra mecánica.
        /// </summary>
        public int MechanicKey2 => MechanicCode == FlorCode ? (int)((_bits >> Key2Shift) & DoseMask) + 1 : NoColor;

        /// <summary>Id de la pareja del Remolino (0..7); el constructor de tableros lo deduce de <c>partner</c>.</summary>
        public int MechanicPairId => (int)((_bits >> PairShift) & ThreeBitMask);

        /// <summary>
        /// <c>n</c>: sellos de la Liana de cuenta, gotas pendientes de la Lluvia u hojas cerradas que le faltan al Brote de dos
        /// hojas (2 o 1; 0 en el Brote normal) (0..7). 0 en la Flor, que guarda <c>key2</c> en esos bits.
        /// </summary>
        public int MechanicN => MechanicCode == FlorCode ? 0 : (int)((_bits >> NShift) & ThreeBitMask);

        /// <summary>
        /// <c>k</c>: ritmo en vertidos de Lluvia y Piraña (0..7; 2-5 en campaña, 08 §9). 0 en la Flor, que guarda <c>key2</c> en
        /// esos bits.
        /// </summary>
        public int MechanicK => MechanicCode == FlorCode ? 0 : (int)((_bits >> KShift) & ThreeBitMask);

        /// <summary>Contador de vertidos de Lluvia o Piraña (0..7).</summary>
        public int MechanicCounter => (int)((_bits >> CounterShift) & ThreeBitMask);

        /// <summary>Banderas de la mecánica (2 bits; <see cref="FlagClosed"/>).</summary>
        public int MechanicFlags => (int)((_bits >> FlagsShift) & FlagsMask);

        /// <summary>
        /// Devuelve la cuia con la mecánica indicada y sus atributos (03 §3: la clave canónica incluye
        /// <c>(cap, tipo, key, partner, n, k, flags)</c> y los contadores; con la Flor, también <c>key2</c>). Sustituye cualquier
        /// mecánica anterior.
        /// </summary>
        /// <param name="type">Mecánica; <see cref="MechanicType.None"/> equivale a <see cref="WithoutMechanic"/> (todos los atributos a 0).</param>
        /// <param name="key">Tinte llave 1-16, o 0 si la mecánica no lo usa (se guarda tinte − 1).</param>
        /// <param name="pairId">Id de la pareja del Remolino (0..7).</param>
        /// <param name="n">Sellos de la Liana de cuenta, gotas pendientes de la Lluvia u hojas que le faltan al Brote de dos hojas (0..7).</param>
        /// <param name="k">Ritmo en vertidos (0..7).</param>
        /// <param name="counter">Contador de vertidos (0..7).</param>
        /// <param name="flags">Banderas (0..3; <see cref="FlagClosed"/>).</param>
        /// <param name="key2">
        /// Segunda llave de la Flor de vitória-régia, tinte 1-16 (se guarda tinte − 1 en los bits de <c>n</c> y <c>k</c>), o 0;
        /// solo la Flor la lleva, y la Flor no lleva <c>n</c> ni <c>k</c>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Algún atributo está fuera de su campo.</exception>
        /// <exception cref="ArgumentException">Atributos en una cuia sin mecánica, <c>key2</c> fuera de la Flor o la Flor con <c>n</c> o <c>k</c>.</exception>
        public Cuia WithMechanic(MechanicType type, int key = 0, int pairId = 0, int n = 0, int k = 0, int counter = 0, int flags = 0, int key2 = 0)
        {
            if (!MechanicTypes.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Mecánica desconocida.");
            }

            if (type == MechanicType.None)
            {
                if (key != 0 || pairId != 0 || n != 0 || k != 0 || counter != 0 || flags != 0 || key2 != 0)
                {
                    throw new ArgumentException("Una cuia sin mecánica no lleva atributos.", nameof(type));
                }

                return WithoutMechanic();
            }

            if (key < 0 || key > MaxColor)
            {
                throw new ArgumentOutOfRangeException(nameof(key), key, "La llave es un tinte de 1 a 16, o 0 si no se usa.");
            }

            if (key2 < 0 || key2 > MaxColor)
            {
                throw new ArgumentOutOfRangeException(nameof(key2), key2, "La segunda llave es un tinte de 1 a 16, o 0 si no se usa.");
            }

            CheckField(pairId, nameof(pairId));
            CheckField(n, nameof(n));
            CheckField(k, nameof(k));
            CheckField(counter, nameof(counter));
            if (flags < 0 || flags > (int)FlagsMask)
            {
                throw new ArgumentOutOfRangeException(nameof(flags), flags, "Las banderas ocupan 2 bits.");
            }

            bool flor = type == MechanicType.Flor;
            if (flor && (n != 0 || k != 0))
            {
                throw new ArgumentException("La Flor guarda key2 en los bits de n y k: no lleva n ni k.", nameof(type));
            }

            if (!flor && key2 != 0)
            {
                throw new ArgumentException("Solo la Flor de vitória-régia lleva key2.", nameof(key2));
            }

            ulong field = ((ulong)type << MechanicShift)
                | ((ulong)(key == 0 ? 0 : key - 1) << KeyShift)
                | ((ulong)pairId << PairShift)
                | ((ulong)n << NShift)
                | ((ulong)k << KShift)
                | ((ulong)counter << CounterShift)
                | ((ulong)flags << FlagsShift);
            if (flor)
            {
                field |= (ulong)(key2 == 0 ? 0 : key2 - 1) << Key2Shift;
            }

            return new Cuia((_bits & ~MechanicFieldMask) | field);
        }

        /// <summary>La misma cuia sin mecánica: lo que queda cuando una mecánica se abre o se suelta (02 R16 para D-007).</summary>
        public Cuia WithoutMechanic()
        {
            return new Cuia(_bits & ~MechanicFieldMask);
        }

        /// <summary>La misma cuia con otro contador de vertidos (Lluvia, Piraña). Bucle caliente: no valida el rango.</summary>
        internal Cuia WithMechanicCounter(int counter)
        {
            return new Cuia((_bits & ~(ThreeBitMask << CounterShift)) | ((ulong)counter << CounterShift));
        }

        /// <summary>
        /// La misma cuia con otro <c>n</c> (gotas pendientes de la Lluvia, hojas que le faltan al Brote de dos hojas). Bucle
        /// caliente: no valida el rango.
        /// </summary>
        internal Cuia WithMechanicN(int n)
        {
            return new Cuia((_bits & ~(ThreeBitMask << NShift)) | ((ulong)n << NShift));
        }

        /// <summary>Color de la cima, o <see cref="NoColor"/> si está vacía. La cima siempre es visible (R7).</summary>
        public int TopColor => IsEmpty ? NoColor : RawColorAt(Count - 1);

        /// <summary>
        /// <c>barro_expuesto</c> (02 §1.2; §9.1 M5): la cima es Barro (<see cref="MudColor"/>). La cuia no es origen y, como
        /// destino, acepta cualquier color: el primer vertido lava el barro y ocupa su lugar.
        /// </summary>
        public bool MudExposed => TopColor == MudColor;

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
        /// La misma cuia con las dosis de <paramref name="layerMask"/> (bit k = dosis k desde el fondo) visibles: Luciérnaga,
        /// Pororoca y la máscara de revelado de la partida (02 T6; 04 §2.5, §2.4 a). Las demás dosis no cambian; los bits que
        /// no son dosis ocultas se ignoran.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="layerMask"/> no es una máscara de 6 bits.</exception>
        public Cuia WithRevealed(int layerMask)
        {
            if (layerMask < 0 || layerMask > (int)HiddenFieldMask)
            {
                throw new ArgumentOutOfRangeException(nameof(layerMask), layerMask, "La máscara de capas ocupa 6 bits.");
            }

            return new Cuia(_bits & ~((ulong)layerMask << HiddenShift));
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
                | doses
                | (_bits & MechanicFieldMask));
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

        /// <summary>
        /// Remolino (02 §9.1 M4): devuelve la cuia con <paramref name="amount"/> dosis visibles de <paramref name="color"/>
        /// insertadas por el <b>fondo</b>; las demás suben (con su visibilidad). Precondición: caben (<see cref="Space"/>).
        /// Bucle caliente: no valida.
        /// </summary>
        internal Cuia InsertAtBottom(int color, int amount)
        {
            int count = Count;
            ulong bottom = 0;
            for (int i = 0; i < amount; i++)
            {
                bottom |= (ulong)(color - 1) << (i * BitsPerDose);
            }

            ulong doses = (((_bits & DosesFieldMask) << (amount * BitsPerDose)) | bottom) & DosesFieldMask;
            ulong hidden = ((ulong)HiddenMask << amount) & HiddenFieldMask;
            ulong bits = (_bits & ~DosesFieldMask & ~(HiddenFieldMask << HiddenShift) & ~(ThreeBitMask << CountShift))
                | doses
                | (hidden << HiddenShift)
                | ((ulong)(count + amount) << CountShift);
            return new Cuia(bits);
        }

        /// <summary>
        /// La misma cuia con la dosis <paramref name="index"/> de color <paramref name="color"/>, sin tocar su visibilidad: es
        /// como AF.Solver asigna colores a las posiciones ocultas en cada realidad de Aguas turbias (03 §8). Bucle caliente: no
        /// valida (índice dentro de la cuia, color 1-16).
        /// </summary>
        internal Cuia WithColorAt(int index, int color)
        {
            int shift = index * BitsPerDose;
            return new Cuia((_bits & ~(DoseMask << shift)) | ((ulong)(color - 1) << shift));
        }

        /// <summary>
        /// Lavado del Barro (02 §9.1 M5): la dosis de Barro de la cima desaparece y <paramref name="amount"/> dosis visibles de
        /// <paramref name="color"/> ocupan su lugar (<c>espacio_efectivo + 1</c>). La dosis de debajo no queda arriba, así que
        /// no se revela (R7) aunque esté oculta. Precondición: <see cref="MudExposed"/>; bucle caliente.
        /// </summary>
        internal Cuia WashMud(int color, int amount)
        {
            int top = Count - 1;
            ulong bits = _bits
                & ~(DoseMask << (top * BitsPerDose))
                & ~(1UL << (HiddenShift + top))
                & ~(ThreeBitMask << CountShift);
            bits |= (ulong)top << CountShift;
            return new Cuia(bits).AddOnTop(color, amount);
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

        /// <summary>
        /// Forma legible para tests y registros, del fondo a la cima: <c>Cuia(4: 3* 1 1)</c>; <c>*</c> = oculta. Con
        /// mecánica añade sus atributos no nulos: <c>Cuia(4: | hoja key=4 flags=1)</c>.
        /// </summary>
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

            if (HasMechanic)
            {
                sb.Append(" | ").Append(MechanicTypes.ToKey(Mechanic));
                AppendField(sb, "key", MechanicKey);
                AppendField(sb, "key2", MechanicKey2);
                AppendField(sb, "pair", MechanicPairId);
                AppendField(sb, "n", MechanicN);
                AppendField(sb, "k", MechanicK);
                AppendField(sb, "counter", MechanicCounter);
                AppendField(sb, "flags", MechanicFlags);
            }

            return sb.Append(')').ToString();
        }

        private static void AppendField(StringBuilder sb, string name, int value)
        {
            if (value != 0)
            {
                sb.Append(' ').Append(name).Append('=').Append(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void CheckField(int value, string paramName)
        {
            if (value < 0 || value > MaxMechanicField)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Los atributos de mecánica ocupan 3 bits (0..7).");
            }
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
