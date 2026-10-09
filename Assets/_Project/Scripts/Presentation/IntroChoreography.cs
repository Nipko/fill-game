#nullable enable
using System;
using AF.Core.Board;

namespace AF.Presentation
{
    /// <summary>Dónde están los miquitos en «Llenar y teñir» (guion §7.1).</summary>
    public enum IntroPerch
    {
        /// <summary>Sin tropa: el follaje se sacude y cada fruta cae desde detrás del HUD (3 filas, reintentos, mundo 13…).</summary>
        Canopy = 0,

        /// <summary>La tropa de la especie del tramo, en su rama sobre el panel (franja ≥ 44 dp y primer intento).</summary>
        Branch = 1,
    }

    /// <summary>Modelo de suelta de la tropa (guion §4.4): tres modelos parametrizados en lugar de nueve gestos.</summary>
    public enum TroopModel
    {
        /// <summary><c>fila</c>: la tropa corre por la rama baja y suelta sobre cada columna suya.</summary>
        Row = 0,

        /// <summary><c>cuelga</c>: baja colgada de la rama alta sobre su grupo y suelta desde la mano.</summary>
        Hang = 1,

        /// <summary><c>sacude</c>: se queda sobre su parte del tablero y la fruta cae de la rama.</summary>
        Shake = 2,
    }

    /// <summary>
    /// Entrada del planificador de «Llenar y teñir» (guion §7.4), en dp del lienzo con la y medida desde arriba: geometría
    /// de las cuias, de las filas, de las repisas y del panel, el estado visible (dosis y ocultas), el modo y T. Arrays
    /// preasignados: se rellena sin asignar memoria.
    /// </summary>
    public sealed class IntroInput
    {
        /// <summary>Cuias como mucho (02 R1).</summary>
        public const int MaxCuias = BoardState.MaxCuias;

        /// <summary>Dosis como mucho por cuia.</summary>
        public const int MaxDoses = Cuia.MaxCapacity;

        /// <summary>Filas como mucho (D-006).</summary>
        public const int MaxRows = 3;

        /// <summary>Cuias del tablero.</summary>
        public int Count;

        /// <summary>Eje de cada cuia (dp).</summary>
        public readonly float[] CenterX = new float[MaxCuias];

        /// <summary>Línea de pie de cada cuia (dp desde arriba).</summary>
        public readonly float[] FootY = new float[MaxCuias];

        /// <summary>Fila de cada cuia (0 = la de arriba).</summary>
        public readonly int[] Row = new int[MaxCuias];

        /// <summary>Dosis finales de cada cuia (visibles y ocultas: todas cuentan para la altura del agua).</summary>
        public readonly int[] Doses = new int[MaxCuias];

        /// <summary>Tinte de cada dosis, de abajo arriba: <c>[i · MaxDoses + k]</c>.</summary>
        public readonly int[] Tints = new int[MaxCuias * MaxDoses];

        /// <summary>Dosis oculta (Aguas turbias): <c>[i · MaxDoses + k]</c>.</summary>
        public readonly bool[] Hidden = new bool[MaxCuias * MaxDoses];

        /// <summary>u en dp (medio ancho de la cuia, 07 §4.1).</summary>
        public float Unit = 24f;

        /// <summary>Filas del tablero.</summary>
        public int Rows = 1;

        /// <summary>Línea de pie de cada fila (dp desde arriba).</summary>
        public readonly float[] RowFootY = new float[MaxRows];

        /// <summary>Ancho del lienzo (dp).</summary>
        public float CanvasWidth = 360f;

        /// <summary>Borde izquierdo del panel con su marco.</summary>
        public float PanelX;

        /// <summary>Borde superior del panel.</summary>
        public float PanelTop;

        /// <summary>Ancho del panel.</summary>
        public float PanelWidth;

        /// <summary>Borde inferior del panel.</summary>
        public float PanelBottom;

        /// <summary>Grosor del marco tal como se dibuja (<c>GameScreen.PanelFrameDp</c>).</summary>
        public float FrameDp = 13.5f;

        /// <summary>Borde inferior de la fila del HUD (safeTop + 56; 88 a 360 × 780).</summary>
        public float HudBottom = 88f;

        /// <summary>Duración T de la intro (ms).</summary>
        public float TotalMs = 1400f;

        /// <summary>Posadero pedido (el planificador pasa a dosel si la franja no alcanza).</summary>
        public IntroPerch Perch;

        /// <summary>Modelo de suelta de la especie.</summary>
        public TroopModel Model;

        /// <summary><c>tropa_intro</c> de la especie.</summary>
        public int Troop = 2;

        /// <summary><c>tam_dp</c> de la especie.</summary>
        public float MonkeyDp = 40f;

        /// <summary>Semilla de la variación determinista (el número de nivel).</summary>
        public int Seed;

        /// <summary>Vacía la entrada (mismos arrays).</summary>
        public void Clear()
        {
            Count = 0;
            Array.Clear(Doses, 0, Doses.Length);
            Array.Clear(Tints, 0, Tints.Length);
            Array.Clear(Hidden, 0, Hidden.Length);
        }

        /// <summary>Fija la cuia <paramref name="i"/> a partir del modelo (tintes, dosis y ocultas).</summary>
        public void SetCuia(int i, float centerX, float footY, int row, Cuia cuia)
        {
            CenterX[i] = centerX;
            FootY[i] = footY;
            Row[i] = row;
            int count = Math.Min(cuia.Count, MaxDoses);
            Doses[i] = count;
            for (int k = 0; k < MaxDoses; k++)
            {
                bool on = k < count;
                Tints[(i * MaxDoses) + k] = on ? cuia.ColorAt(k) : 0;
                Hidden[(i * MaxDoses) + k] = on && cuia.IsHiddenAt(k);
            }
        }

        /// <summary>Fija la cuia <paramref name="i"/> con sus tintes de abajo arriba y la máscara de ocultas (tests).</summary>
        public void SetCuia(int i, float centerX, float footY, int row, int[] tintsBottomUp, int hiddenMask = 0)
        {
            CenterX[i] = centerX;
            FootY[i] = footY;
            Row[i] = row;
            int count = Math.Min(tintsBottomUp.Length, MaxDoses);
            Doses[i] = count;
            for (int k = 0; k < MaxDoses; k++)
            {
                bool on = k < count;
                Tints[(i * MaxDoses) + k] = on ? tintsBottomUp[k] : 0;
                Hidden[(i * MaxDoses) + k] = on && (hiddenMask & (1 << k)) != 0;
            }
        }
    }

    /// <summary>
    /// Director de «Llenar y teñir» (guion de la nueva intro, §6-§7): planificador puro y determinista que se ejecuta una vez
    /// al arrancar la intro y deja en arrays preasignados todo lo que la vista evalúa por tiempo. Agua: el río crece, la
    /// lámina desborda el canto superior del marco desde el centro, las tiras bajan por los costados y corren por las
    /// repisas, y sobre cada cuia no vacía cae un chorrito ≤ 30 ms después del paso del agua por su eje que la llena desde el
    /// fondo hasta sus dosis finales (las vacías quedan secas); todo lleno antes de 700 / 400 ms (escalado por T), con las
    /// velocidades subidas si hace falta. Fruta: una por racha visible (≤ 24; si hay más, una por cuia con cascada de 25 ms
    /// de abajo arriba), filas de arriba abajo, por pasadas en el sentido de la tropa y de abajo arriba en la cuia; impactos
    /// repartidos entre el inicio de la fase d y T − su floración, de modo que la última floración termina en T; caída con
    /// g = 12.000 dp/s² y τ entre 80 y 260 ms, programada hacia atrás desde el impacto. Tropa: grupos contiguos de columnas,
    /// entrada por la rama a ≤ 450 dp/s cuando la fila 1 está llena (larga) o posada desde t = 0 (corta), parada de 60 ms
    /// sobre cada columna, suelta ≥ 50 ms tras llegar, salida de espaldas en 300 ms. Las dosis ocultas no forman racha ni
    /// reciben fruta. Sin LINQ, sin <c>UnityEngine.Random</c> y sin asignaciones tras construirlo.
    /// </summary>
    public sealed class IntroChoreography
    {
        /// <summary>Gravedad de la fruta: 12.000 dp/s² (guion §7.4, paso 5), en dp/ms².</summary>
        public const float GravityDpPerMs2 = 0.012f;

        /// <summary>Caída mínima (ms).</summary>
        public const float MinFallMs = 80f;

        /// <summary>Caída máxima (ms): por encima, la fruta sale con velocidad hacia abajo.</summary>
        public const float MaxFallMs = 260f;

        /// <summary>Frutas como mucho (guion §7.4, paso 2; R4).</summary>
        public const int MaxFruits = 24;

        /// <summary>Una franja florece en 100 ms (02 §4.2, canon).</summary>
        public const float BloomMs = 100f;

        /// <summary>Cascada entre rachas en el modo de una fruta por cuia (guion §7.4, paso 2).</summary>
        public const float CascadeMs = 25f;

        /// <summary>La fruta se funde en 40 ms al tocar el agua.</summary>
        public const float FruitFadeMs = 40f;

        /// <summary>Anillo sobre la superficie: 0,4 → 1,3 u en 250 ms.</summary>
        public const float RingMs = 250f;

        /// <summary>El penacho baja hasta su racha en ≤ 60 ms.</summary>
        public const float PlumeMs = 60f;

        /// <summary>La fruta se ve en la mano este tiempo antes de soltarse (gesto de suelta de unos 100 ms).</summary>
        public const float HoldMs = 60f;

        /// <summary>Velocidad de la tropa por la rama: ≤ 450 dp/s (R2), en dp/ms.</summary>
        public const float MonkeySpeedDpPerMs = 0.45f;

        /// <summary>
        /// La fruta sale de la mano con, como mucho, 250 dp/s de velocidad horizontal para llegar a una columna vecina de su
        /// grupo (guion §4.4), en dp/ms: así un miquito no tiene que ir y volver entre columnas en cada pasada.
        /// </summary>
        public const float MaxTossDpPerMs = 0.25f;

        /// <summary>Cada miquito frena 60 ms sobre cada columna suya.</summary>
        public const float MonkeyStopMs = 60f;

        /// <summary>Cada suelta llega ≥ 50 ms después de que el miquito llegue a su columna (guion §7.4, paso 7).</summary>
        public const float ReleaseAfterArrivalMs = 50f;

        /// <summary>El gesto de suelta acaba 40 ms después de soltar.</summary>
        public const float GestureTailMs = 40f;

        /// <summary>La tropa sale de espaldas en 300 ms.</summary>
        public const float ExitMs = 300f;

        /// <summary>Con T ≥ 1.000 ms, versión larga (fases de 1.400 escaladas); si no, la corta (fases de 800).</summary>
        public const float LongFromMs = 1000f;

        /// <summary>En dosel, la fruta cae desde hudBottom − 12, por detrás del HUD.</summary>
        public const float CanopyAboveHudDp = 12f;

        /// <summary>Rama baja en panel.top − 8.</summary>
        public const float LowBranchAbovePanelDp = 8f;

        /// <summary>Rama alta en hudBottom + 6 (modelo <c>cuelga</c>).</summary>
        public const float HighBranchBelowHudDp = 6f;

        /// <summary>Modo rama solo con una franja libre ≥ 44 dp.</summary>
        public const float BranchMinBandDp = 44f;

        /// <summary>Con menos de 64 dp de franja, <c>cuelga</c> se sienta en la rama baja y suelta como <c>fila</c>.</summary>
        public const float HangMinBandDp = 64f;

        /// <summary>Tamaño mínimo del miquito en pantalla (si no cabe, dosel).</summary>
        public const float MinMonkeyDp = 28f;

        /// <summary>Ancho de las tiras de agua del marco y de las repisas.</summary>
        public const float StripDp = 5f;

        /// <summary>La tira de cada repisa corre por su cara delantera, 8 dp bajo la línea de pie (≥ 8 dp de las cuias).</summary>
        public const float ShelfStripBelowFootDp = 8f;

        /// <summary>Borde inferior de la repisa: pie + 15 (<c>BoardLayout.ShelfDrop</c>), de donde caen los chorritos de abajo.</summary>
        public const float ShelfUndersideBelowFootDp = 15f;

        /// <summary>La lámina del desborde empieza 4 dp por encima del panel.</summary>
        public const float SheetAbovePanelDp = 4f;

        /// <summary>Miquitos como mucho en pantalla.</summary>
        public const int MaxMonkeys = 6;

        /// <summary>Claves de trayectoria como mucho por miquito.</summary>
        public const int MaxKeys = (2 * MaxFruits) + 6;

        /// <summary>Rachas como mucho.</summary>
        public const int MaxRuns = IntroInput.MaxCuias * IntroInput.MaxDoses;

        private const int N = IntroInput.MaxCuias;
        private const int D = IntroInput.MaxDoses;

        private readonly int[] _sorted = new int[N];
        private readonly float[] _ideal = new float[MaxFruits];
        private readonly float[] _mx = new float[MaxMonkeys];
        private readonly float[] _mt = new float[MaxMonkeys];
        private readonly float[] _arrived = new float[MaxMonkeys];
        private readonly float[] _lastRelease = new float[MaxMonkeys];
        private readonly int[] _cycleFruits = new int[MaxMonkeys];
        private readonly float[] _cycleStart = new float[MaxMonkeys];

        // ---- Agua por cuia ----

        /// <summary>Paso del agua sobre el eje de la cuia (ms); −1 si la cuia queda seca.</summary>
        public readonly float[] PassMs = new float[N];

        /// <summary>Apertura del chorrito (≤ 30 ms tras el paso).</summary>
        public readonly float[] StreamOpenMs = new float[N];

        /// <summary>La cabeza del chorrito toca el fondo de la cavidad.</summary>
        public readonly float[] ContactMs = new float[N];

        /// <summary>Empieza a subir el agua (≤ 50 ms tras el contacto).</summary>
        public readonly float[] FillStartMs = new float[N];

        /// <summary>El agua llega a las dosis finales; el chorrito se corta y su cola cae.</summary>
        public readonly float[] FillEndMs = new float[N];

        /// <summary>De dónde cae el chorrito (dp desde arriba): el canto interior del marco o el borde inferior de la repisa de arriba.</summary>
        public readonly float[] StreamTopY = new float[N];

        /// <summary>Fondo de la cavidad (dp desde arriba).</summary>
        public readonly float[] CavityFloorY = new float[N];

        /// <summary>Superficie del agua con las dosis finales (dp desde arriba).</summary>
        public readonly float[] SurfaceY = new float[N];

        // ---- Rachas (de abajo arriba por cuia) ----

        /// <summary>Primera racha de cada cuia.</summary>
        public readonly int[] RunFirst = new int[N];

        /// <summary>Rachas de cada cuia.</summary>
        public readonly int[] RunsOf = new int[N];

        /// <summary>Cuia de cada racha.</summary>
        public readonly int[] RunCuia = new int[MaxRuns];

        /// <summary>Primera dosis de la racha.</summary>
        public readonly int[] RunFrom = new int[MaxRuns];

        /// <summary>Última dosis de la racha (incluida).</summary>
        public readonly int[] RunTo = new int[MaxRuns];

        /// <summary>Tinte de la racha.</summary>
        public readonly int[] RunTint = new int[MaxRuns];

        /// <summary>Inicio de la floración de la racha (ms).</summary>
        public readonly float[] RunBloomMs = new float[MaxRuns];

        /// <summary>Fruta que tiñe la racha.</summary>
        public readonly int[] RunFruit = new int[MaxRuns];

        // ---- Frutas, en orden de impacto ----

        /// <summary>Cuia de la fruta.</summary>
        public readonly int[] FruitCuia = new int[MaxFruits];

        /// <summary>Tinte de la fruta (el de su racha; en el modo por cuia, el de la racha de arriba).</summary>
        public readonly int[] FruitTint = new int[MaxFruits];

        /// <summary>Primera racha que tiñe la fruta.</summary>
        public readonly int[] FruitRunFirst = new int[MaxFruits];

        /// <summary>Última racha que tiñe la fruta.</summary>
        public readonly int[] FruitRunLast = new int[MaxFruits];

        /// <summary>Impacto en la superficie (ms).</summary>
        public readonly float[] FruitImpactMs = new float[MaxFruits];

        /// <summary>Suelta (ms): impacto − τ.</summary>
        public readonly float[] FruitReleaseMs = new float[MaxFruits];

        /// <summary>x de la suelta (dp).</summary>
        public readonly float[] FruitReleaseX = new float[MaxFruits];

        /// <summary>y de la suelta (dp desde arriba).</summary>
        public readonly float[] FruitReleaseY = new float[MaxFruits];

        /// <summary>x del impacto: el eje de la cuia.</summary>
        public readonly float[] FruitImpactX = new float[MaxFruits];

        /// <summary>y del impacto: la superficie del agua.</summary>
        public readonly float[] FruitImpactY = new float[MaxFruits];

        /// <summary>Velocidad inicial hacia abajo (dp/ms).</summary>
        public readonly float[] FruitV0 = new float[MaxFruits];

        /// <summary>Gravedad efectiva (dp/ms²).</summary>
        public readonly float[] FruitG = new float[MaxFruits];

        /// <summary>Giro (°/ms, con signo): 180-360 °/s.</summary>
        public readonly float[] FruitSpin = new float[MaxFruits];

        /// <summary>Fin de la floración de todas sus rachas (ms).</summary>
        public readonly float[] FruitBloomEndMs = new float[MaxFruits];

        /// <summary>Miquito que la suelta (−1 = cae del dosel, por detrás del HUD).</summary>
        public readonly int[] FruitMonkey = new int[MaxFruits];

        /// <summary>Primer impacto de su fila (solo esos dejan gotas, ≤ 20 partículas).</summary>
        public readonly bool[] FruitFirstInRow = new bool[MaxFruits];

        // ---- Columnas y tropa ----

        /// <summary>x de las columnas con contenido, ordenadas.</summary>
        public readonly float[] ColumnX = new float[N];

        /// <summary>Columna de cada cuia (−1 si no tiene racha visible).</summary>
        public readonly int[] CuiaColumn = new int[N];

        /// <summary>Grupo (miquito) de cada columna.</summary>
        public readonly int[] ColumnGroup = new int[N];

        /// <summary>Centro del grupo de columnas de cada miquito.</summary>
        public readonly float[] MonkeyGroupX = new float[MaxMonkeys];

        /// <summary>El miquito entra por la esquina derecha.</summary>
        public readonly bool[] MonkeyFromRight = new bool[MaxMonkeys];

        /// <summary>Sentido de la salida (−1 izquierda, +1 derecha).</summary>
        public readonly int[] MonkeyExitDir = new int[MaxMonkeys];

        /// <summary>Claves de trayectoria por miquito.</summary>
        public readonly int[] MonkeyKeyCount = new int[MaxMonkeys];

        /// <summary>Tiempo de cada clave: <c>[m · MaxKeys + j]</c>.</summary>
        public readonly float[] MonkeyKeyT = new float[MaxMonkeys * MaxKeys];

        /// <summary>x de la mano en cada clave: <c>[m · MaxKeys + j]</c>.</summary>
        public readonly float[] MonkeyKeyX = new float[MaxMonkeys * MaxKeys];

        /// <summary>Cuias del plan.</summary>
        public int CuiaCount { get; private set; }

        /// <summary>T (ms).</summary>
        public float TotalMs { get; private set; }

        /// <summary>Versión larga (T ≥ 1.000).</summary>
        public bool IsLong { get; private set; }

        /// <summary>T / 1.400 (larga) o T / 800 (corta): escala de tiempos y velocidades.</summary>
        public float Scale { get; private set; }

        /// <summary>Filas.</summary>
        public int Rows { get; private set; }

        /// <summary>El río termina de crecer (0-150 / 0-80 ms, escalado).</summary>
        public float RiseEndMs { get; private set; }

        /// <summary>La lámina empieza a desbordar el canto superior del marco.</summary>
        public float SheetStartMs { get; private set; }

        /// <summary>Fundido de entrada de la lámina.</summary>
        public float SheetFadeMs { get; private set; }

        /// <summary>Velocidad del frente de la lámina (dp/ms, a cada lado del centro).</summary>
        public float SheetSpeed { get; private set; }

        /// <summary>La lámina llega a las esquinas superiores.</summary>
        public float SheetCornerMs { get; private set; }

        /// <summary>Centro del panel (origen de la lámina).</summary>
        public float SheetCenterX { get; private set; }

        /// <summary>Medio ancho del panel.</summary>
        public float SheetHalfWidth { get; private set; }

        /// <summary>Canto interior superior del marco (dp desde arriba).</summary>
        public float FrameInnerY { get; private set; }

        /// <summary>Borde interior izquierdo del marco.</summary>
        public float InnerLeftX { get; private set; }

        /// <summary>Borde interior derecho del marco.</summary>
        public float InnerRightX { get; private set; }

        /// <summary>Hasta dónde bajan las tiras de los costados (esquinas inferiores del panel).</summary>
        public float SideBottomY { get; private set; }

        /// <summary>Velocidad de las tiras de los costados (dp/ms).</summary>
        public float SideSpeed { get; private set; }

        /// <summary>Velocidad de las tiras de las repisas (dp/ms, desde cada extremo hacia el centro).</summary>
        public float ShelfSpeed { get; private set; }

        /// <summary>El agua llega a la repisa <c>r</c> (la de debajo de la fila <c>r</c>).</summary>
        public readonly float[] ShelfArriveMs = new float[IntroInput.MaxRows];

        /// <summary>y de la tira de la repisa <c>r</c> (borde de arriba).</summary>
        public readonly float[] ShelfStripY = new float[IntroInput.MaxRows];

        /// <summary>La cola del chorrito cae en este tiempo tras el fin del llenado.</summary>
        public float StreamTailMs { get; private set; }

        /// <summary>La madera empieza a escurrirse.</summary>
        public float DrainStartMs { get; private set; }

        /// <summary>La madera queda seca.</summary>
        public float DrainEndMs { get; private set; }

        /// <summary>Inicio de la fase d (teñir).</summary>
        public float DStartMs { get; private set; }

        /// <summary>La fila 1 está llena (la tropa entra en la versión larga).</summary>
        public float Row1FullMs { get; private set; }

        /// <summary>Fin del último llenado.</summary>
        public float LastFillEndMs { get; private set; }

        /// <summary>Rachas visibles.</summary>
        public int RunCount { get; private set; }

        /// <summary>Frutas.</summary>
        public int FruitCount { get; private set; }

        /// <summary>Una fruta por cuia (más de 24 rachas) con cascada de 25 ms.</summary>
        public bool FruitPerCuia { get; private set; }

        /// <summary>Primer impacto: el «?» de todas las ocultas aparece en ese fotograma (−1 sin frutas).</summary>
        public float FirstImpactMs { get; private set; }

        /// <summary>Fin de la última floración (= T salvo que no haya frutas).</summary>
        public float LastBloomEndMs { get; private set; }

        /// <summary>Fruta en vuelo: 0,6 u, con un mínimo de 8 dp.</summary>
        public float FruitFlightDp { get; private set; }

        /// <summary>Fruta al impacto: 0,45 u, con un mínimo de 8 dp.</summary>
        public float FruitImpactDp { get; private set; }

        /// <summary>u en dp.</summary>
        public float Unit { get; private set; }

        /// <summary>Posadero efectivo.</summary>
        public IntroPerch Perch { get; private set; }

        /// <summary>Modelo efectivo.</summary>
        public TroopModel Model { get; private set; }

        /// <summary>Tamaño del miquito en pantalla: min(tam_dp, franja − 10).</summary>
        public float MonkeyDp { get; private set; }

        /// <summary>Miquitos en pantalla: clamp(tropa, 2, columnas con contenido).</summary>
        public int MonkeyCount { get; private set; }

        /// <summary>y de la rama de la tropa (dp desde arriba).</summary>
        public float BranchY { get; private set; }

        /// <summary>y de la rama baja (panel.top − 8).</summary>
        public float LowBranchY { get; private set; }

        /// <summary>Entrada de la tropa (versión larga): la fila 1 está llena.</summary>
        public float TroopEnterMs { get; private set; }

        /// <summary>Columnas con contenido.</summary>
        public int ColumnCount { get; private set; }

        /// <summary>Variación determinista: con una tropa de menos de 4, entra por la derecha.</summary>
        public bool EntryFromRight { get; private set; }

        /// <summary>Variación determinista: el segundo de la fila lidera.</summary>
        public bool LeaderSwap { get; private set; }

        /// <summary>Variación determinista de reposo: 0 rascarse, 1 mirar al cielo, 2 acicalarse.</summary>
        public int RestVariant { get; private set; }

        /// <summary>Planifica la intro para <paramref name="input"/>. Sin asignaciones.</summary>
        public void Plan(IntroInput input)
        {
            int n = Clamp(input.Count, 0, N);
            CuiaCount = n;
            float total = Math.Max(1f, input.TotalMs);
            TotalMs = total;
            IsLong = total >= LongFromMs;
            float s = total / (IsLong ? 1400f : 800f);
            Scale = s;
            Rows = Clamp(input.Rows, 1, IntroInput.MaxRows);
            Unit = Math.Max(1f, input.Unit);
            FruitFlightDp = Math.Max(8f, 0.6f * Unit);
            FruitImpactDp = Math.Max(8f, 0.45f * Unit);

            RiseEndMs = (IsLong ? 150f : 80f) * s;
            SheetStartMs = (IsLong ? 120f : 50f) * s;
            SheetFadeMs = (IsLong ? 60f : 30f) * s;
            StreamTailMs = (IsLong ? 60f : 40f) * s;
            DrainStartMs = (IsLong ? 700f : 400f) * s;
            DrainEndMs = (IsLong ? 850f : 550f) * s;
            DStartMs = (IsLong ? 950f : 550f) * s;

            FrameInnerY = input.PanelTop + input.FrameDp;
            SheetCenterX = input.PanelX + (input.PanelWidth * 0.5f);
            SheetHalfWidth = Math.Max(1f, input.PanelWidth * 0.5f);
            InnerLeftX = input.PanelX + input.FrameDp;
            InnerRightX = input.PanelX + input.PanelWidth - input.FrameDp;
            SideBottomY = Math.Max(FrameInnerY, input.PanelBottom - input.FrameDp);
            for (int r = 0; r < IntroInput.MaxRows; r++)
            {
                ShelfStripY[r] = r < Rows - 1 ? input.RowFootY[r] + ShelfStripBelowFootDp : 0f;
            }

            // Agua: con las velocidades de base; si el llenado no cabe antes de 700 / 400 ms (escalado), todo lo que viene
            // tras el desborde se comprime de forma uniforme (el director sube las velocidades: guion §7.2).
            float limit = (IsLong ? 700f : 400f) * s;
            ComputeWater(input, n, 1f);
            if (LastFillEndMs > limit && LastFillEndMs > SheetStartMs)
            {
                float f = (limit - SheetStartMs - 0.5f) / (LastFillEndMs - SheetStartMs);
                ComputeWater(input, n, Math.Max(0.05f, f));
            }

            BuildRuns(input, n);
            BuildColumns(input, n);
            ChoosePerch(input);
            BuildFruits(input, n);
            Schedule(input);
        }

        /// <summary>x de la mano del miquito <paramref name="m"/> en <paramref name="t"/> (interpolación lineal de sus claves).</summary>
        public float MonkeyX(int m, float t)
        {
            int count = MonkeyKeyCount[m];
            if (count == 0)
            {
                return 0f;
            }

            int b = m * MaxKeys;
            if (t <= MonkeyKeyT[b])
            {
                return MonkeyKeyX[b];
            }

            for (int j = 1; j < count; j++)
            {
                float t1 = MonkeyKeyT[b + j];
                if (t <= t1)
                {
                    float t0 = MonkeyKeyT[b + j - 1];
                    float span = t1 - t0;
                    float a = span <= 0.0001f ? 1f : (t - t0) / span;
                    return MonkeyKeyX[b + j - 1] + ((MonkeyKeyX[b + j] - MonkeyKeyX[b + j - 1]) * a);
                }
            }

            return MonkeyKeyX[b + count - 1];
        }

        /// <summary>Velocidad horizontal del miquito <paramref name="m"/> en <paramref name="t"/> (dp/ms; 0 parado).</summary>
        public float MonkeyVelocity(int m, float t)
        {
            int count = MonkeyKeyCount[m];
            int b = m * MaxKeys;
            for (int j = 1; j < count; j++)
            {
                float t0 = MonkeyKeyT[b + j - 1];
                float t1 = MonkeyKeyT[b + j];
                if (t >= t0 && t < t1 && t1 - t0 > 0.0001f)
                {
                    return (MonkeyKeyX[b + j] - MonkeyKeyX[b + j - 1]) / (t1 - t0);
                }
            }

            return 0f;
        }

        /// <summary>Posición de la fruta <paramref name="k"/> a <paramref name="dtMs"/> de su suelta (dp desde arriba).</summary>
        public float FruitY(int k, float dtMs)
        {
            float t = Math.Max(0f, dtMs);
            return FruitReleaseY[k] + (FruitV0[k] * t) + (0.5f * FruitG[k] * t * t);
        }

        /// <summary>Racha que contiene la dosis <paramref name="k"/> de la cuia <paramref name="i"/> (−1 si es oculta o vacía).</summary>
        public int RunOf(int i, int k)
        {
            int first = RunFirst[i];
            int last = first + RunsOf[i];
            for (int r = first; r < last; r++)
            {
                if (k >= RunFrom[r] && k <= RunTo[r])
                {
                    return r;
                }
            }

            return -1;
        }

        private void ComputeWater(IntroInput input, int n, float f)
        {
            float s = Scale;
            float sheetV = (IsLong ? 1.3f : 2.6f) / (s * f);
            float sideV = (IsLong ? 2.0f : 4.0f) / (s * f);
            float shelfV = (IsLong ? 1.6f : 3.2f) / (s * f);
            float openDelay = (IsLong ? 20f : 15f) * s * f;
            float headMs = (IsLong ? 30f : 20f) * s * f;
            float fillMs = (IsLong ? 220f : 130f) * s * f;
            SheetSpeed = sheetV;
            SideSpeed = sideV;
            ShelfSpeed = shelfV;
            SheetCornerMs = SheetStartMs + (SheetHalfWidth / sheetV);
            for (int r = 0; r < IntroInput.MaxRows; r++)
            {
                ShelfArriveMs[r] = r < Rows - 1 ? SheetCornerMs + (Math.Max(0f, ShelfStripY[r] - FrameInnerY) / sideV) : -1f;
            }

            float last = 0f;
            float row1 = -1f;
            for (int i = 0; i < N; i++)
            {
                PassMs[i] = -1f;
                StreamOpenMs[i] = -1f;
                ContactMs[i] = -1f;
                FillStartMs[i] = -1f;
                FillEndMs[i] = -1f;
            }

            for (int i = 0; i < n; i++)
            {
                float foot = input.FootY[i];
                CavityFloorY[i] = foot - (CuiaGeometry.CavityBottomU * Unit);
                SurfaceY[i] = foot - ((CuiaGeometry.CavityBottomU + Math.Max(0, input.Doses[i])) * Unit);
                int row = Clamp(input.Row[i], 0, Rows - 1);
                float x = input.CenterX[i];
                if (row == 0)
                {
                    StreamTopY[i] = FrameInnerY;
                }
                else
                {
                    StreamTopY[i] = input.RowFootY[row - 1] + ShelfUndersideBelowFootDp;
                }

                if (input.Doses[i] <= 0)
                {
                    continue; // las vacías quedan secas: ni chorrito ni agua
                }

                float pass;
                if (row == 0)
                {
                    pass = SheetStartMs + (Math.Abs(x - SheetCenterX) / sheetV);
                }
                else
                {
                    float fromEnd = Math.Max(0f, Math.Min(x - InnerLeftX, InnerRightX - x));
                    pass = ShelfArriveMs[row - 1] + (fromEnd / shelfV);
                }

                PassMs[i] = pass;
                StreamOpenMs[i] = pass + openDelay;
                ContactMs[i] = StreamOpenMs[i] + headMs;
                FillStartMs[i] = ContactMs[i];
                FillEndMs[i] = FillStartMs[i] + fillMs;
                last = Math.Max(last, FillEndMs[i]);
                if (row == 0)
                {
                    row1 = Math.Max(row1, FillEndMs[i]);
                }
            }

            LastFillEndMs = last;
            Row1FullMs = row1 >= 0f ? row1 : SheetCornerMs;
        }

        private void BuildRuns(IntroInput input, int n)
        {
            int count = 0;
            for (int i = 0; i < N; i++)
            {
                RunFirst[i] = count;
                RunsOf[i] = 0;
                if (i >= n)
                {
                    continue;
                }

                int doses = Clamp(input.Doses[i], 0, D);
                int k = 0;
                while (k < doses)
                {
                    if (input.Hidden[(i * D) + k])
                    {
                        k++; // las ocultas no forman racha ni reciben fruta (guion §10)
                        continue;
                    }

                    int tint = input.Tints[(i * D) + k];
                    int from = k;
                    while (k + 1 < doses && !input.Hidden[(i * D) + k + 1] && input.Tints[(i * D) + k + 1] == tint)
                    {
                        k++;
                    }

                    RunCuia[count] = i;
                    RunFrom[count] = from;
                    RunTo[count] = k;
                    RunTint[count] = tint;
                    RunBloomMs[count] = -1f;
                    RunFruit[count] = -1;
                    RunsOf[i]++;
                    count++;
                    k++;
                }
            }

            RunCount = count;
            FruitPerCuia = count > MaxFruits;
        }

        private void BuildColumns(IntroInput input, int n)
        {
            int c = 0;
            for (int i = 0; i < n; i++)
            {
                CuiaColumn[i] = -1;
                if (RunsOf[i] == 0)
                {
                    continue;
                }

                float x = input.CenterX[i];
                bool found = false;
                for (int j = 0; j < c; j++)
                {
                    if (Math.Abs(ColumnX[j] - x) < 1f)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                // Inserción ordenada.
                int at = c;
                while (at > 0 && ColumnX[at - 1] > x)
                {
                    ColumnX[at] = ColumnX[at - 1];
                    at--;
                }

                ColumnX[at] = x;
                c++;
            }

            ColumnCount = c;
            for (int i = 0; i < n; i++)
            {
                if (RunsOf[i] == 0)
                {
                    continue;
                }

                for (int j = 0; j < c; j++)
                {
                    if (Math.Abs(ColumnX[j] - input.CenterX[i]) < 1f)
                    {
                        CuiaColumn[i] = j;
                        break;
                    }
                }
            }
        }

        private void ChoosePerch(IntroInput input)
        {
            uint h = Mix(input.Seed);
            EntryFromRight = (h & 1u) != 0u;
            LeaderSwap = ((h >> 9) & 1u) != 0u;
            RestVariant = (int)((h >> 17) % 3u);

            float band = input.PanelTop - input.HudBottom;
            IntroPerch perch = input.Perch;
            TroopModel model = input.Model;
            if (model == TroopModel.Hang && band < HangMinBandDp)
            {
                model = TroopModel.Row; // con menos de 64 dp, se sienta en la rama baja y suelta como fila
            }

            float size = Math.Min(input.MonkeyDp, band - 10f);
            if (perch == IntroPerch.Branch && (band < BranchMinBandDp || size < MinMonkeyDp || ColumnCount == 0))
            {
                perch = IntroPerch.Canopy;
            }

            Perch = perch;
            Model = model;
            MonkeyDp = perch == IntroPerch.Branch ? size : 0f;
            LowBranchY = input.PanelTop - LowBranchAbovePanelDp;
            BranchY = model == TroopModel.Hang ? input.HudBottom + HighBranchBelowHudDp : LowBranchY;
            int m = perch == IntroPerch.Branch ? Math.Min(Math.Max(input.Troop, 2), ColumnCount) : 0;
            m = Math.Min(m, MaxMonkeys);
            MonkeyCount = m;
            for (int j = 0; j < MaxMonkeys; j++)
            {
                MonkeyKeyCount[j] = 0;
                MonkeyFromRight[j] = false;
                MonkeyExitDir[j] = -1;
                MonkeyGroupX[j] = 0f;
            }

            if (m == 0)
            {
                return;
            }

            // Columnas en grupos contiguos, uno por miquito.
            for (int j = 0; j < ColumnCount; j++)
            {
                ColumnGroup[j] = (j * m) / ColumnCount;
            }

            for (int g = 0; g < m; g++)
            {
                float lo = float.MaxValue;
                float hi = float.MinValue;
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (ColumnGroup[j] == g)
                    {
                        lo = Math.Min(lo, ColumnX[j]);
                        hi = Math.Max(hi, ColumnX[j]);
                    }
                }

                MonkeyGroupX[g] = (lo + hi) * 0.5f;
            }

            // Cada miquito entra por la esquina más cercana a su grupo (con tropa de 4 o más, la mitad por cada lado, guion
            // §7.2): recorridos cortos. Un grupo en el centro exacto entra por el lado de la variación.
            float mid = input.CanvasWidth * 0.5f;
            for (int g = 0; g < m; g++)
            {
                float x = MonkeyGroupX[g];
                MonkeyFromRight[g] = Math.Abs(x - mid) < 0.5f ? EntryFromRight : x > mid;
            }
        }

        private void BuildFruits(IntroInput input, int n)
        {
            bool descending = Perch == IntroPerch.Branch && MonkeyCount < 4 && EntryFromRight;
            int count = 0;
            for (int row = 0; row < Rows; row++)
            {
                // Cuias de la fila ordenadas por x en el sentido de la tropa (inserción, sin asignaciones).
                int k = 0;
                for (int i = 0; i < n; i++)
                {
                    if (Clamp(input.Row[i], 0, Rows - 1) != row || RunsOf[i] == 0)
                    {
                        continue;
                    }

                    int at = k;
                    while (at > 0 && Before(input.CenterX[i], input.CenterX[_sorted[at - 1]], descending))
                    {
                        _sorted[at] = _sorted[at - 1];
                        at--;
                    }

                    _sorted[at] = i;
                    k++;
                }

                bool first = true;
                if (FruitPerCuia)
                {
                    for (int j = 0; j < k && count < MaxFruits; j++)
                    {
                        int c = _sorted[j];
                        int r0 = RunFirst[c];
                        int r1 = r0 + RunsOf[c] - 1;
                        AddFruit(count, c, r0, r1, first);
                        first = false;
                        count++;
                    }
                }
                else
                {
                    int maxRuns = 0;
                    for (int j = 0; j < k; j++)
                    {
                        maxRuns = Math.Max(maxRuns, RunsOf[_sorted[j]]);
                    }

                    for (int p = 0; p < maxRuns; p++)
                    {
                        for (int j = 0; j < k && count < MaxFruits; j++)
                        {
                            int c = _sorted[j];
                            if (p < RunsOf[c])
                            {
                                int r = RunFirst[c] + p;
                                AddFruit(count, c, r, r, first);
                                first = false;
                                count++;
                            }
                        }
                    }
                }
            }

            FruitCount = count;
        }

        private void AddFruit(int k, int cuia, int runFirst, int runLast, bool firstInRow)
        {
            FruitCuia[k] = cuia;
            FruitRunFirst[k] = runFirst;
            FruitRunLast[k] = runLast;
            FruitTint[k] = RunTint[runLast];
            FruitFirstInRow[k] = firstInRow;
            FruitMonkey[k] = -1;
            for (int r = runFirst; r <= runLast; r++)
            {
                RunFruit[r] = k;
            }
        }

        private void Schedule(IntroInput input)
        {
            int count = FruitCount;
            float total = TotalMs;
            FirstImpactMs = -1f;
            LastBloomEndMs = 0f;
            TroopEnterMs = IsLong ? Row1FullMs : 0f;
            if (count == 0)
            {
                InitMonkeys(input);
                FinishMonkeys(input);
                return;
            }

            float releaseY;
            switch (Perch == IntroPerch.Canopy ? -1 : (int)Model)
            {
                case (int)TroopModel.Row:
                    releaseY = BranchY - (0.35f * MonkeyDp); // la mano del miquito sentado
                    break;
                case (int)TroopModel.Hang:
                    releaseY = input.PanelTop - LowBranchAbovePanelDp - 2f; // la mano del cuerpo colgado, ≥ 8 dp sobre el panel
                    break;
                case (int)TroopModel.Shake:
                    releaseY = LowBranchY - 2f; // cae de la rama sacudida
                    break;
                default:
                    releaseY = input.HudBottom - CanopyAboveHudDp; // por detrás del HUD
                    break;
            }

            uint spin = Mix(input.Seed ^ 0x51ED27);
            for (int k = 0; k < count; k++)
            {
                int c = FruitCuia[k];
                FruitImpactX[k] = input.CenterX[c];
                FruitImpactY[k] = SurfaceY[c];
                FruitReleaseX[k] = input.CenterX[c];
                FruitReleaseY[k] = releaseY;
                spin = Next(spin);
                float deg = 180f + (180f * ((spin & 0xFFFF) / 65535f));
                FruitSpin[k] = ((spin >> 16) & 1u) != 0u ? deg / 1000f : -deg / 1000f;
            }

            // Impactos ideales repartidos de forma uniforme entre el inicio de d y T − la floración de la última.
            float lastIdeal = total - BloomDuration(count - 1);
            for (int k = 0; k < count; k++)
            {
                float ideal = count == 1 ? lastIdeal : DStartMs + ((lastIdeal - DStartMs) * k / (count - 1));
                _ideal[k] = Math.Min(ideal, lastIdeal);
            }

            InitMonkeys(input);
            float prev = float.MinValue;
            for (int k = 0; k < count; k++)
            {
                int c = FruitCuia[k];
                float tau = FallTime(k);
                float dur = BloomDuration(k);
                float imp = Math.Max(_ideal[k], prev);
                imp = Math.Max(imp, FillEndMs[c] + 1f); // cada impacto, después del fin del llenado de su cuia
                int m = -1;
                float target = 0f;
                float arrive = 0f;
                float minRelease = float.MinValue;
                if (Perch == IntroPerch.Branch)
                {
                    m = ColumnGroup[Math.Max(0, CuiaColumn[c])];
                    minRelease = Needs(m, FruitImpactX[k], tau, out target, out arrive);
                    imp = Math.Max(imp, minRelease + tau);
                }

                imp = Math.Min(imp, total - dur); // nunca se pasa de T
                float release = imp - tau;
                if (m >= 0)
                {
                    if (release + 0.01f < minRelease)
                    {
                        // No cabe con su miquito: pasa al vecino que llegue a tiempo, o cae del dosel (por detrás del HUD).
                        int chosen = -1;
                        for (int d = -1; d <= 1 && chosen < 0; d += 2)
                        {
                            int other = m + d;
                            if (other < 0 || other >= MonkeyCount)
                            {
                                continue;
                            }

                            float need = Needs(other, FruitImpactX[k], tau, out float t2, out float a2);
                            if (release + 0.01f >= need)
                            {
                                chosen = other;
                                target = t2;
                                arrive = a2;
                            }
                        }

                        m = chosen;
                    }

                    if (m >= 0)
                    {
                        Commit(m, target, arrive, release);
                        if (Model != TroopModel.Shake)
                        {
                            FruitReleaseX[k] = target; // desde la mano, con ≤ 250 dp/s hasta su columna
                        }
                    }
                    else
                    {
                        FruitReleaseY[k] = input.HudBottom - CanopyAboveHudDp;
                        tau = FallTime(k);
                        release = imp - tau;
                    }
                }

                FruitMonkey[k] = m;
                FruitImpactMs[k] = imp;
                FruitReleaseMs[k] = release;
                FruitBloomEndMs[k] = imp + dur;
                for (int r = FruitRunFirst[k]; r <= FruitRunLast[k]; r++)
                {
                    RunBloomMs[r] = imp + ((r - FruitRunFirst[k]) * CascadeMs); // de abajo arriba; la de la fruta, la última
                    LastBloomEndMs = Math.Max(LastBloomEndMs, RunBloomMs[r] + BloomMs);
                }

                prev = imp;
            }

            // Primer impacto del tablero (el «?» de las ocultas) y de cada fila (solo esos dejan gotas).
            float first = float.MaxValue;
            for (int k = 0; k < count; k++)
            {
                first = Math.Min(first, FruitImpactMs[k]);
                FruitFirstInRow[k] = true;
                int row = Clamp(input.Row[FruitCuia[k]], 0, Rows - 1);
                for (int j = 0; j < count; j++)
                {
                    if (j != k && Clamp(input.Row[FruitCuia[j]], 0, Rows - 1) == row
                        && (FruitImpactMs[j] < FruitImpactMs[k] || (FruitImpactMs[j] == FruitImpactMs[k] && j < k)))
                    {
                        FruitFirstInRow[k] = false;
                        break;
                    }
                }
            }

            FirstImpactMs = first;
            FinishMonkeys(input);
        }

        private float BloomDuration(int k)
        {
            return ((FruitRunLast[k] - FruitRunFirst[k]) * CascadeMs) + BloomMs;
        }

        // τ = √(2h/g) entre 80 y 260 ms; por encima, sale con velocidad hacia abajo; por debajo, cae más despacio (nunca
        // hacia arriba: la suelta no es un lanzamiento).
        private float FallTime(int k)
        {
            float h = Math.Max(1f, FruitImpactY[k] - FruitReleaseY[k]);
            float g = GravityDpPerMs2;
            float tau = (float)Math.Sqrt(2f * h / g);
            if (tau > MaxFallMs)
            {
                tau = MaxFallMs;
                FruitV0[k] = (h - (0.5f * g * tau * tau)) / tau;
                FruitG[k] = g;
            }
            else if (tau < MinFallMs)
            {
                tau = MinFallMs;
                FruitV0[k] = 0f;
                FruitG[k] = 2f * h / (tau * tau);
            }
            else
            {
                FruitV0[k] = 0f;
                FruitG[k] = g;
            }

            return tau;
        }

        private void InitMonkeys(IntroInput input)
        {
            int m = MonkeyCount;
            float size = MonkeyDp;
            float spacing = 0.9f * size;
            int left = 0;
            for (int g = 0; g < m; g++)
            {
                left += MonkeyFromRight[g] ? 0 : 1;
            }

            for (int g = 0; g < m; g++)
            {
                _lastRelease[g] = float.MinValue;
                _cycleFruits[g] = 0;
                _cycleStart[g] = float.MinValue;
                MonkeyKeyCount[g] = 0;
                float x;
                if (IsLong)
                {
                    // Oculta en el follaje de su esquina; entra cuando la fila 1 está llena, en fila, el que va más lejos
                    // primero (o el segundo, con la variación).
                    int order;
                    if (MonkeyFromRight[g])
                    {
                        order = g - left;
                        int count = m - left;
                        if (LeaderSwap && count >= 2 && order < 2)
                        {
                            order = 1 - order;
                        }

                        x = input.CanvasWidth + (0.6f * size) + (order * spacing);
                    }
                    else
                    {
                        order = left - 1 - g;
                        if (LeaderSwap && left >= 2 && order < 2)
                        {
                            order = 1 - order;
                        }

                        x = -(0.6f * size) - (order * spacing);
                    }

                    _mx[g] = x;
                    _mt[g] = TroopEnterMs;
                    _arrived[g] = float.MinValue;
                    AddKey(g, 0f, x);
                    AddKey(g, TroopEnterMs, x);
                }
                else
                {
                    // Posada desde t = 0 sobre su grupo (sobre la primera columna que atiende; en sacude, el centro).
                    x = Model == TroopModel.Shake ? MonkeyGroupX[g] : FirstColumnOf(g);
                    _mx[g] = x;
                    _mt[g] = 0f;
                    _arrived[g] = -1000f;
                    AddKey(g, 0f, x);
                }
            }
        }

        private float FirstColumnOf(int g)
        {
            for (int k = 0; k < FruitCount; k++)
            {
                int col = CuiaColumn[FruitCuia[k]];
                if (col >= 0 && ColumnGroup[col] == g)
                {
                    return FruitImpactX[k];
                }
            }

            return MonkeyGroupX[g];
        }

        // Suelta más temprana posible del miquito m para una fruta sobre x que cae en tau (y adónde va y cuándo llega): en
        // fila y cuelga, el punto más cercano desde el que la fruta llega a su columna con ≤ 250 dp/s; en sacude, el centro de
        // su grupo (la fruta cae de la rama sobre la columna).
        private float Needs(int m, float columnX, float tau, out float target, out float arrive)
        {
            float reach = MaxTossDpPerMs * tau;
            target = Model == TroopModel.Shake ? MonkeyGroupX[m] : Math.Min(Math.Max(_mx[m], columnX - reach), columnX + reach);
            float dist = Math.Abs(target - _mx[m]);
            arrive = dist < 0.5f ? _arrived[m] : _mt[m] + (dist / MonkeySpeedDpPerMs);

            // Ciclo de suelta (guion §7.3): 1-2 frutas por ciclo (una en cada mano, sin moverse) y el siguiente ciclo, al
            // acabar el gesto del anterior.
            bool join = dist < 0.5f && _cycleFruits[m] == 1;
            float after = join ? _lastRelease[m] : (_cycleStart[m] == float.MinValue ? float.MinValue : _cycleStart[m] + GestureTailMs);
            float min = Math.Max(arrive + ReleaseAfterArrivalMs, after);
            if (!IsLong)
            {
                min = Math.Max(min, 100f * Scale); // el primer ciclo de suelta
            }

            return min;
        }

        private void Commit(int m, float target, float arrive, float release)
        {
            bool moved = Math.Abs(target - _mx[m]) >= 0.5f;
            if (moved)
            {
                AddKey(m, _mt[m], _mx[m]);
                AddKey(m, arrive, target);
                _mx[m] = target;
                _arrived[m] = arrive;
            }

            _mt[m] = Math.Max(Math.Max(_mt[m], arrive + MonkeyStopMs), release + GestureTailMs);
            bool sameCycle = !moved && _cycleFruits[m] == 1 && _cycleStart[m] != float.MinValue && release < _cycleStart[m] + GestureTailMs;
            if (sameCycle)
            {
                _cycleFruits[m] = 2;
            }
            else
            {
                _cycleFruits[m] = 1;
                _cycleStart[m] = release;
            }

            _lastRelease[m] = release;
        }

        private void FinishMonkeys(IntroInput input)
        {
            for (int g = 0; g < MonkeyCount; g++)
            {
                int dir = _mx[g] < input.CanvasWidth * 0.5f ? -1 : 1; // hacia la esquina más cercana, de espaldas
                MonkeyExitDir[g] = dir;
                float start = Math.Max(TotalMs, _mt[g]);
                AddKey(g, start, _mx[g]);
                AddKey(g, start + ExitMs, _mx[g] + (dir * MonkeySpeedDpPerMs * ExitMs));
            }
        }

        private void AddKey(int m, float t, float x)
        {
            int count = MonkeyKeyCount[m];
            int b = m * MaxKeys;
            if (count > 0)
            {
                float lastT = MonkeyKeyT[b + count - 1];
                float lastX = MonkeyKeyX[b + count - 1];
                if (t < lastT)
                {
                    t = lastT;
                }

                if (Math.Abs(t - lastT) < 0.0001f && Math.Abs(x - lastX) < 0.0001f)
                {
                    return;
                }
            }

            if (count >= MaxKeys)
            {
                return;
            }

            MonkeyKeyT[b + count] = t;
            MonkeyKeyX[b + count] = x;
            MonkeyKeyCount[m] = count + 1;
        }

        private static bool Before(float a, float b, bool descending)
        {
            return descending ? a > b : a < b;
        }

        private static int Clamp(int v, int lo, int hi)
        {
            return v < lo ? lo : (v > hi ? hi : v);
        }

        // Hash de la semilla (xorshift con semilla fija; sin UnityEngine.Random).
        private static uint Mix(int seed)
        {
            uint x = ((uint)seed * 2654435761u) ^ 0x9E3779B9u;
            if (x == 0u)
            {
                x = 0x2545F491u;
            }

            x = Next(x);
            x = Next(x);
            return Next(x);
        }

        private static uint Next(uint x)
        {
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return x;
        }
    }
}
