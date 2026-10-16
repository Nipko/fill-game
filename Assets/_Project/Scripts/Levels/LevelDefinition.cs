#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;

namespace AF.Levels
{
    /// <summary>
    /// Un nivel del paquete (formato <c>af-level/1</c>, GDD 08 §4.1; subconjunto v0 de docs/dev/prototipo-v0.md, b):
    /// tablero inicial explícito (<c>caps</c> y <c>cuias</c> de abajo arriba con el índice de motor del glosario §9), las
    /// mecánicas de cada cuia (<c>attrs</c>, 02 §9.1, D-007, D-019 y D-023), las dosis ocultas de Aguas turbias o de la Tinaja
    /// (<c>murky</c> y <c>hidden</c>, 02 §11 y M13), <c>units_per_color</c> y el par del pipeline. Las soluciones nunca viajan en el paquete (08 §4.1).
    /// </summary>
    /// <remarks>
    /// <see cref="LevelPackReader"/> solo crea definiciones cuyo tablero ya construyó <see cref="BoardBuilder"/> y aceptó
    /// <see cref="LevelValidation"/>: <see cref="CreateBoard"/> no puede fallar con los datos de un paquete leído.
    /// </remarks>
    public sealed class LevelDefinition
    {
        /// <summary>Etiqueta <c>normal</c> de <c>difficulty.label</c> (08 §4.1; glosario §11): también la de un nivel sin etiqueta.</summary>
        public const string LabelNormal = "normal";

        /// <summary>Etiqueta <c>hard</c> (Difícil, glosario §6.1).</summary>
        public const string LabelHard = "hard";

        /// <summary>Etiqueta <c>very_hard</c> (Muy difícil, glosario §6.1).</summary>
        public const string LabelVeryHard = "very_hard";

        /// <summary><c>difficulty.band</c> <c>comfort</c> (ciclo de 20, 03 §12).</summary>
        public const string BandComfort = "comfort";

        /// <summary><c>difficulty.band</c> <c>challenge</c> (03 §12).</summary>
        public const string BandChallenge = "challenge";

        /// <summary><c>difficulty.band</c> <c>recovery</c> (03 §12).</summary>
        public const string BandRecovery = "recovery";

        /// <summary><c>difficulty.band</c> <c>milestone</c> (03 §12).</summary>
        public const string BandMilestone = "milestone";

        private static readonly ReliefBoard[] NoRelief = new ReliefBoard[0];
        private static readonly int[][] NoHidden = new int[0][];

        private readonly int[] _caps;
        private readonly int[][] _cuias;
        private readonly MechanicSpec[] _attrs;
        private readonly int[][] _hidden;
        private readonly string[] _palette;
        private readonly ReliefBoard[] _relief;

        internal LevelDefinition(
            string id,
            int level,
            int world,
            string waterBase,
            string[] palette,
            UnitsPerColor units,
            int[] caps,
            int[][] cuias,
            MechanicSpec[] attrs,
            bool murky,
            int[][]? hidden,
            int par,
            string difficultyLabel = LabelNormal,
            ReliefBoard[]? relief = null,
            string? introMechanic = null,
            bool skippable = true,
            bool hintSupported = true,
            string? band = null,
            bool parApprox = false)
        {
            if (attrs.Length != caps.Length)
            {
                throw new ArgumentException("attrs debe tener una entrada por cuia.", nameof(attrs));
            }

            Id = id;
            Level = level;
            World = world;
            WaterBase = waterBase;
            _palette = palette;
            Units = units;
            _caps = caps;
            _cuias = cuias;
            _attrs = attrs;
            HasHiddenDoses = murky;
            HasTinaja = HasMechanic(MechanicType.Tinaja);
            IsMurky = murky && !HasTinaja;
            _hidden = hidden ?? NoHidden;
            Par = par;
            DifficultyLabel = difficultyLabel;
            _relief = relief ?? NoRelief;
            IntroMechanic = introMechanic;
            Skippable = skippable;
            HintSupported = hintSupported;
            Band = band;
            ParApprox = parApprox;
        }

        /// <summary>
        /// <c>relief</c> (08 §4.1; 03 §13): tableros de alivio en orden de prioridad (vacía antes del 31 y en el subconjunto v0).
        /// El cliente sirve <c>relief[0]</c> al llegar a <c>relief_after_fails</c> fallos con el cartel «El río bajó» (04 §2.7).
        /// </summary>
        public IReadOnlyList<ReliefBoard> Relief => _relief;

        /// <summary>
        /// <c>difficulty.label</c> (08 §4.1): <see cref="LabelNormal"/>, <see cref="LabelHard"/> o <see cref="LabelVeryHard"/>.
        /// Un nivel sin etiqueta (el subconjunto v0 del tutorial) es <see cref="LabelNormal"/>.
        /// </summary>
        public string DifficultyLabel { get; }

        /// <summary>Identificador <c>m{mm}_v{v}_n{nnnn}</c> (glosario §11).</summary>
        public string Id { get; }

        /// <summary>Número de nivel de campaña (1..).</summary>
        public int Level { get; }

        /// <summary>Mundo (glosario §8).</summary>
        public int World { get; }

        /// <summary>Agua base del mundo: <c>black</c>, <c>white</c> o <c>clear</c> (glosario §11).</summary>
        public string WaterBase { get; }

        /// <summary>IDs de tinte de la paleta del nivel (glosario §9).</summary>
        public IReadOnlyList<string> Palette => _palette;

        /// <summary>
        /// <c>units_per_color</c> (08 §4.1; 02 R3): el entero (4 para todos los colores) o el objeto con las excepciones, como
        /// <c>{"10": 5}</c> para la fruta abundante de una Cuia alta (02 §8).
        /// </summary>
        public AF.Core.Board.UnitsPerColor Units { get; }

        /// <summary>
        /// <c>attrs</c> (08 §4.1; 02 §9.1; D-007): la mecánica de cada cuia, una entrada por cuia; <see cref="MechanicSpec.None"/>
        /// es el <c>null</c> del JSON (y todas lo son si el nivel no trae <c>attrs</c>).
        /// </summary>
        public IReadOnlyList<MechanicSpec> Attrs => _attrs;

        /// <summary>
        /// Nivel de Aguas turbias (02 §11; <c>lvl_murky_from_level</c> = 100, 08 §9): <c>murky</c> = true sin la Tinaja. Con
        /// <c>pack_schema</c> 3 el campo <c>murky</c> también marca las ocultas de la tinaja (M13; propuesta de la tinaja, b.2
        /// regla 1 y c.6), que no son agua turbia: la tinaja es la única fuente de ocultas de su nivel (LevelValidation), así que
        /// un nivel con tinaja nunca es de Aguas turbias. Para «el nivel tiene dosis ocultas», <see cref="HasHiddenDoses"/>.
        /// </summary>
        public bool IsMurky { get; }

        /// <summary>
        /// <c>murky</c> tal como viene en el paquete: el nivel tiene dosis ocultas, de Aguas turbias (<see cref="IsMurky"/>) o
        /// de la Tinaja (<see cref="HasTinaja"/>).
        /// </summary>
        public bool HasHiddenDoses { get; }

        /// <summary>
        /// El nivel lleva la Tinaja (M13; D-019; solo <c>pack_schema</c> 3) con su cuia chica: sus dosis ocultas (todas menos
        /// la boca) son las de <see cref="Hidden"/>.
        /// </summary>
        public bool HasTinaja { get; }

        /// <summary>
        /// <c>hidden</c>: posiciones ocultas <c>[cuia, capa]</c>, en el orden del paquete: las de Aguas turbias (02 §11) o, si
        /// <see cref="HasTinaja"/>, la máscara de la tinaja.
        /// </summary>
        public IReadOnlyList<int[]> Hidden => _hidden;

        /// <summary>Número de dosis ocultas al empezar (02 T2: como mucho 4).</summary>
        public int HiddenCount => _hidden.Length;

        /// <summary>
        /// <c>intro_mechanic</c> (08 §4.1; contrato 21): la mecánica que estrena este nivel (<c>chica</c>, <c>alta</c>,
        /// <c>hoja</c>, <c>matapi</c>, <c>liana</c>, <c>murky</c>, <c>remolino</c>, <c>barro</c>, <c>lluvia</c>, <c>brote</c> o
        /// <c>pirana</c>; D-007; y con <c>pack_schema</c> 3, <c>tinaja</c>, <c>liana_n</c>, <c>hoja_atada</c>,
        /// <c>brote_n2</c> o <c>flor</c>; D-019 y D-023, punto 5), o null.
        /// </summary>
        public string? IntroMechanic { get; }

        /// <summary><c>skippable</c>: se permite el Salto del Boto (por defecto true; false en los estrenos).</summary>
        public bool Skippable { get; }

        /// <summary><c>hint_supported</c>: se permite el Morpho (por defecto true).</summary>
        public bool HintSupported { get; }

        /// <summary>
        /// <c>difficulty.band</c> (03 §12): <see cref="BandComfort"/>, <see cref="BandChallenge"/>, <see cref="BandRecovery"/> o
        /// <see cref="BandMilestone"/>; null si el paquete no la trae (subconjunto v0).
        /// </summary>
        public string? Band { get; }

        /// <summary><c>difficulty.par_approx</c> [P] (solo <c>pack_schema</c> 2): el par no es el óptimo exacto (03 §9, Piraña).</summary>
        public bool ParApprox { get; }

        /// <summary>Par: vertidos de la solución óptima del pipeline (03 §5).</summary>
        public int Par { get; }

        /// <summary>Número de cuias T (02 R1).</summary>
        public int CuiaCount => _caps.Length;

        /// <summary>Alguna cuia lleva Barro (color 15, 02 §9.1 M5).</summary>
        public bool HasMud
        {
            get
            {
                for (int i = 0; i < _cuias.Length; i++)
                {
                    if (Array.IndexOf(_cuias[i], Cuia.MudColor) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Alguna cuia es una Cuia chica (capacidad menor que 4, 02 §8).</summary>
        public bool HasSmallCuia
        {
            get
            {
                for (int i = 0; i < _caps.Length; i++)
                {
                    if (_caps[i] < Cuia.DefaultCapacity)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Alguna cuia es una Cuia alta (capacidad mayor que 4, 02 §8).</summary>
        public bool HasTallCuia
        {
            get
            {
                for (int i = 0; i < _caps.Length; i++)
                {
                    if (_caps[i] > Cuia.DefaultCapacity)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Alguna cuia lleva la mecánica <paramref name="type"/> al empezar.</summary>
        public bool HasMechanic(MechanicType type)
        {
            for (int i = 0; i < _attrs.Length; i++)
            {
                if (_attrs[i].Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Capacidad de la cuia <paramref name="index"/>.</summary>
        public int CapacityOf(int index)
        {
            return _caps[index];
        }

        /// <summary>Dosis de la cuia <paramref name="index"/>, de abajo arriba (copia).</summary>
        public int[] CopyDoses(int index)
        {
            return (int[])_cuias[index].Clone();
        }

        /// <summary>
        /// Tablero inicial del núcleo, construido por <see cref="BoardBuilder"/> con las mecánicas, las dosis ocultas y
        /// <see cref="Units"/> del nivel (un estado nuevo en cada llamada).
        /// </summary>
        public BoardState CreateBoard()
        {
            return Build(_caps, _cuias, _attrs, _hidden.Length == 0 ? null : _hidden, Units, Id);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "LevelDefinition(" + Id + ", T=" + _caps.Length.ToString(CultureInfo.InvariantCulture)
                + ", par=" + Par.ToString(CultureInfo.InvariantCulture) + ")";
        }

        internal static BoardState Build(int[] caps, int[][] cuias, MechanicSpec[] attrs, int[][]? hidden, AF.Core.Board.UnitsPerColor units, string where)
        {
            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs, hidden, units);
            if (built.State == null)
            {
                // El lector ya lo construyó al leer el paquete: solo se llega aquí si alguien cambió el núcleo entre medias.
                throw new InvalidOperationException(where + ": " + built.Error);
            }

            return built.State;
        }
    }

    /// <summary>
    /// Una entrada de <c>relief[]</c> (08 §4.1; <c>schemas/af-level-1.schema.json</c> <c>relief_board</c>): tablero completo
    /// pregenerado con una cuia vacía más (<see cref="TypePlusEmpty"/>) o un color menos (<see cref="TypeMinusColor"/>), con
    /// sus mecánicas (<c>attrs</c>). Sin dosis ocultas, salvo la máscara de la Tinaja si la variante la lleva (<c>hidden</c>,
    /// solo con <c>pack_schema</c> 3; 08 §4.1): en Aguas turbias las variantes van sin ocultos [P].
    /// </summary>
    public sealed class ReliefBoard
    {
        private static readonly int[][] NoHidden = new int[0][];

        /// <summary><c>plus_empty</c>: +1 cuia vacía («El río bajó: esta vez hay una cuia más»).</summary>
        public const string TypePlusEmpty = "plus_empty";

        /// <summary><c>minus_color</c>: −1 color («El río bajó: esta vez hay un color menos»).</summary>
        public const string TypeMinusColor = "minus_color";

        private readonly int[] _caps;
        private readonly int[][] _cuias;
        private readonly MechanicSpec[] _attrs;
        private readonly AF.Core.Board.UnitsPerColor _units;
        private readonly int[][] _hidden;
        private readonly string _where;

        internal ReliefBoard(
            string type,
            int[] caps,
            int[][] cuias,
            MechanicSpec[] attrs,
            AF.Core.Board.UnitsPerColor units,
            int? par,
            string where,
            int[][]? hidden = null)
        {
            if (attrs.Length != caps.Length)
            {
                throw new ArgumentException("attrs debe tener una entrada por cuia.", nameof(attrs));
            }

            Type = type;
            _caps = caps;
            _cuias = cuias;
            _attrs = attrs;
            _units = units;
            _hidden = hidden ?? NoHidden;
            Par = par;
            _where = where;
        }

        /// <summary>
        /// <c>hidden</c> de la variante: la máscara de la Tinaja <c>[cuia, capa]</c> con los índices de la variante, si la lleva
        /// (M13; solo <c>pack_schema</c> 3); vacía en cualquier otra variante.
        /// </summary>
        public IReadOnlyList<int[]> Hidden => _hidden;

        /// <summary>Número de dosis ocultas de la variante al empezar (las de la tinaja; 0 sin ella).</summary>
        public int HiddenCount => _hidden.Length;

        /// <summary><see cref="TypePlusEmpty"/> o <see cref="TypeMinusColor"/>.</summary>
        public string Type { get; }

        /// <summary>Par de la variante (null solo en <c>plus_empty</c>, 03 §13).</summary>
        public int? Par { get; }

        /// <summary>Cuias de la variante.</summary>
        public int CuiaCount => _caps.Length;

        /// <summary>Mecánica de cada cuia de la variante (<see cref="MechanicSpec.None"/> si no tiene; todas si no trae <c>attrs</c>).</summary>
        public IReadOnlyList<MechanicSpec> Attrs => _attrs;

        /// <summary>
        /// Tablero de la variante con <see cref="BoardBuilder"/>, las <c>units_per_color</c> del nivel y la máscara de la
        /// tinaja si la lleva (un estado nuevo en cada llamada).
        /// </summary>
        public BoardState CreateBoard()
        {
            return LevelDefinition.Build(_caps, _cuias, _attrs, _hidden.Length == 0 ? null : _hidden, _units, _where);
        }
    }
}
