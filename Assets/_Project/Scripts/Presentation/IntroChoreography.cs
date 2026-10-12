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
    /// Versión de la intro (guion §7.2-§7.3). La decide la clave de Remote Config que dio la duración
    /// (<c>anim_tint_duration_ms_early</c>: larga; <c>_late</c> y los reintentos: corta), no T: con la clave tardía a 1.000 ms
    /// (rango 600-1.000 de 08 §9) sigue siendo la corta, escalada por T / 800.
    /// </summary>
    public enum IntroVersion
    {
        /// <summary>Sin decidir (tests y herramientas): larga con T ≥ 1.000 ms.</summary>
        Auto = 0,

        /// <summary>Larga (niveles 1-20, primer intento): fases de 1.400 ms escaladas.</summary>
        Long = 1,

        /// <summary>Corta (desde el 21 y en los reintentos): fases de 800 ms escaladas.</summary>
        Short = 2,
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

        /// <summary>Versión pedida (la decide la clave de la duración; <see cref="IntroVersion.Auto"/>: por T).</summary>
        public IntroVersion Version;

        /// <summary>
        /// La pareja se sienta junta (rasgo <c>colas_entrelazadas</c> del catálogo, modelo <c>sacude</c>): centros a 0,9 del
        /// tamaño, lado a lado y mirando al mismo sitio, con las colas colgando bajo la rama, trenzadas.
        /// </summary>
        public bool Pair;

        /// <summary>Vacía la entrada (mismos arrays).</summary>
        public void Clear()
        {
            Count = 0;
            Version = IntroVersion.Auto;
            Pair = false;
            Array.Clear(Doses, 0, Doses.Length);
            Array.Clear(Tints, 0, Tints.Length);
            Array.Clear(Hidden, 0, Hidden.Length);
        }

        /// <summary>
        /// La dosis <paramref name="k"/> de la cuia <paramref name="i"/> no forma racha ni recibe fruta: está oculta (Aguas
        /// turbias, guion §10) o es Barro (color 15, 02 §9.1 M5: inerte, la regla de 02 §4.2 aplicada al Barro [P]).
        /// </summary>
        public bool IsInert(int i, int k)
        {
            int index = (i * MaxDoses) + k;
            return Hidden[index] || Tints[index] == Cuia.MudColor;
        }

        /// <summary>Fija la cuia <paramref name="i"/> a partir del modelo (tintes, dosis y ocultas; el Barro va como tinte 15).</summary>
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
    /// g = 12.000 dp/s² y τ entre 80 y 260 ms, programada hacia atrás desde el impacto. Tropa: grupos contiguos de columnas
    /// separados donde cabe la distancia mínima entre vecinos (1,1 del tamaño entre centros, contando la mano), llegada a la
    /// primera columna como si salieran a ≤ 450 dp/s cuando la fila 1 está llena (larga; con tropa de 4 o más, la mitad por
    /// cada esquina; si no, por un solo lado que alterna con la variación) o posada desde t = 0 (corta), parada de 60 ms sobre
    /// cada columna suya con la mano sobre el eje (la fruta cae recta), suelta ≥ 50 ms tras llegar, y el impulso lateral de
    /// ≤ 250 dp/s solo hacia una columna vecina de su grupo; el centro de cada miquito queda en [0,5·S + 6, ancho − 0,5·S − 6];
    /// si no llega a tiempo, se aprieta el calendario hacia T (paso 7), nunca se acorta el recorrido. Con el calendario ya
    /// hecho (D-011, punto 3), la entrada que se ve: en ≈ 500 ms desde el follaje de su esquina mientras el río llena (en la
    /// larga empieza antes y llega igual; en la corta llega ≥ 50 ms antes de la primera suelta de cada miquito, a ≤ 450 dp/s, o
    /// sigue posado desde t = 0 si no le da tiempo), el reposo de
    /// ≈ 1.500 ms tras la última suelta, ya en juego, y la salida en 500 ms hacia el follaje de la esquina más cercana. Las
    /// claves de trayectoria son el centro del cuerpo (la raíz del rig) con su orientación, así que un giro no desplaza el
    /// cuerpo. Las dosis ocultas no forman racha ni reciben fruta. Sin LINQ, sin <c>UnityEngine.Random</c> y sin asignaciones
    /// tras construirlo.
    /// </summary>
    /// <remarks>
    /// Sin tope de 3 ciclos de suelta por miquito (guion §7.3): ese tope suponía un miquito por columna; aquí cada miquito
    /// atiende un grupo contiguo y el calendario ya separa sus sueltas (llegada + 50 ms, gesto de 40 ms entre ciclos y como
    /// mucho 2 frutas por ciclo sin moverse), así que cortar en el tercer ciclo solo pasaría frutas al dosel en mitad de la
    /// tropa. El Barro (color 15) se trata como una dosis oculta: no es una racha, no recibe fruta y corta las rachas
    /// (<see cref="IntroInput.IsInert"/>).
    /// </remarks>
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

        /// <summary>
        /// La tropa sale de espaldas trepando hacia el follaje de su esquina en 500 ms (D-011, punto 3; antes 300): los de cada
        /// esquina, a la misma velocidad (≤ 450 dp/s), la que lleva al más lejano hasta el follaje en ese tiempo.
        /// </summary>
        public const float ExitMs = 500f;

        /// <summary>
        /// La tropa entra trepando por la rama desde el follaje de su esquina en ≈ 500 ms mientras el río llena (D-011, punto 3).
        /// Solo cambia lo que se ve antes de la llegada a su asiento, nunca el calendario: en la larga la llegada es la de antes
        /// y la entrada empieza antes (como mucho a 450 dp/s); en la corta, cada miquito llega ≥ 50 ms antes de su primera
        /// suelta al sitio donde su trayectoria lo tiene en ese instante (si ya se había movido, a ese sitio), así que su entrada
        /// dura min(500 ms, su primera suelta − 50 ms), también a ≤ 450 dp/s: quien no llega a tiempo sin correr más (con T
        /// corta, los que se sientan hacia el centro, a ≈ 180 dp de su esquina) sigue posado desde t = 0, como antes.
        /// </summary>
        public const float EnterMs = 500f;

        /// <summary>
        /// En la corta, la entrada de cada miquito dura al menos esto: con menos hueco antes de su primera suelta (− 50 ms) o
        /// antes de dejar el sitio al que llega (T muy corta), sigue posado desde t = 0, y con él los de su esquina que se sientan
        /// más lejos (R2: nada de borrones).
        /// </summary>
        public const float MinShortEnterMs = 150f;

        /// <summary>
        /// Reposo de la tropa en la rama tras la última suelta, ya en juego (D-011, punto 3): la salida empieza en
        /// max(T, última suelta + 1.500 ms). La especie cambia el gesto, nunca la duración.
        /// </summary>
        public const float RestMs = 1500f;

        /// <summary>Al salir, sube hacia el follaje de la esquina como mucho 30 dp (sin pasar sobre el HUD).</summary>
        public const float ExitRiseDp = 30f;

        /// <summary>Al salir, la tropa conserva el alfa 1 salvo en los últimos 80 ms.</summary>
        public const float ExitFadeMs = 80f;

        /// <summary>Sin versión pedida (<see cref="IntroVersion.Auto"/>): con T ≥ 1.000 ms, la larga; si no, la corta.</summary>
        public const float LongFromMs = 1000f;

        /// <summary>
        /// Parte visible de la celda de 64 px que ocupa el dibujo de cada fruta (≈ 42 px de 64): el sprite se agranda en esa
        /// proporción para que el tamaño visible sea el del guion (0,6 u en vuelo y 0,45 u al impacto, mínimo 8 dp).
        /// </summary>
        public const float FruitArtFill = 0.65f;

        /// <summary>Mano del brazo que suelta, sentado, delante del centro del cuerpo (S): <c>MonkeyRig.SitHandS.x</c>.</summary>
        public const float SitHandOffsetS = 0.30f;

        /// <summary>Mano libre, colgado, delante del agarre (S): <c>MonkeyRig.HangHandS.x</c>.</summary>
        public const float HangHandOffsetS = 0.14f;

        /// <summary>Mano libre, colgado, bajo el agarre (S): lo más bajo del cuerpo colgado.</summary>
        public const float HangDepthS = 0.95f;

        /// <summary>Distancia mínima entre los centros de dos miquitos vecinos (S).</summary>
        public const float NeighborGapS = 1.1f;

        /// <summary>La pareja de <c>colas_entrelazadas</c>, junta: centros a 0,9 S.</summary>
        public const float PairGapS = 0.9f;

        /// <summary>
        /// Las colas de la pareja cuelgan bajo la rama 0,75 S, trenzadas (revisión del 2026-10-09: «colas colgando bajo la
        /// rama, trenzadas dos vueltas»).
        /// </summary>
        public const float PairTailHangS = 0.75f;

        /// <summary>La punta de la trenza acaba ≥ 6 dp sobre el panel (la lámina del desborde llega a panel.top − 4).</summary>
        public const float PairTailClearDp = 6f;

        /// <summary>Alto del miquito sentado sobre su apoyo (S): la cabeza, también al mirar al cielo.</summary>
        public const float SitTopS = 1.05f;

        /// <summary>La rama de la pareja, ya subida, solo sube 2 dp más hacia las esquinas (la baja sube 6).</summary>
        public const float PairCornerRiseDp = 2f;

        /// <summary>Margen del centro de cada miquito con los bordes de la pantalla: [0,5·S + 6, ancho − 0,5·S − 6].</summary>
        public const float EdgeMarginDp = 6f;

        /// <summary>Con las columnas juntas, la tropa se encoge hasta el 85 % antes de perder miquitos.</summary>
        public const float ShrinkToFit = 0.85f;

        /// <summary>En dosel, la fruta cae desde hudBottom − 12, por detrás del HUD.</summary>
        public const float CanopyAboveHudDp = 12f;

        /// <summary>
        /// Rama baja con su punto más bajo en panel.top − 8 (guion §7.1): con 6 dp de grosor ocupa de panel.top − 11 a
        /// panel.top − 5, por encima de la lámina del desborde (panel.top − 4), y sube 6 dp hacia el follaje de las esquinas.
        /// </summary>
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

        /// <summary>Claves de trayectoria como mucho por miquito (salida, llegada y giro por fruta, más entrada y salida).</summary>
        public const int MaxKeys = (3 * MaxFruits) + 6;

        /// <summary>Rachas como mucho.</summary>
        public const int MaxRuns = IntroInput.MaxCuias * IntroInput.MaxDoses;

        private const int N = IntroInput.MaxCuias;
        private const int D = IntroInput.MaxDoses;

        private readonly int[] _sorted = new int[N];
        private readonly float[] _ideal = new float[MaxFruits];
        private readonly float[] _mx = new float[MaxMonkeys];
        private readonly float[] _mt = new float[MaxMonkeys];
        private readonly int[] _facing = new int[MaxMonkeys];
        private readonly float[] _seat = new float[MaxMonkeys];
        private readonly int[] _seatFacing = new int[MaxMonkeys];
        private readonly float[] _laneLo = new float[MaxMonkeys];
        private readonly float[] _laneHi = new float[MaxMonkeys];
        private readonly float[] _groupLo = new float[MaxMonkeys];
        private readonly float[] _groupHi = new float[MaxMonkeys];
        private readonly float[] _arrived = new float[MaxMonkeys];
        private readonly float[] _relLog = new float[MaxMonkeys * MaxFruits];
        private readonly int[] _relCount = new int[MaxMonkeys];
        private readonly float[] _entrySeat = new float[MaxMonkeys];
        private readonly float[] _entryBack = new float[MaxMonkeys];
        private readonly int[] _entryOrder = new int[MaxMonkeys];
        private readonly float[] _entryLimit = new float[MaxMonkeys];
        private readonly float[] _entryRelease = new float[MaxMonkeys];
        private readonly int[] _entryAnchor = new int[MaxMonkeys];
        private readonly bool[] _entering = new bool[MaxMonkeys];
        private float _canvasW = 360f;
        private float _corner = 6f;
        private float _sag;
        private float _hangRelease;
        private bool _crowdedCycles;

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

        /// <summary>Orientación de cada miquito en t = 0 (+1 mira a la derecha).</summary>
        public readonly int[] MonkeyStartFacing = new int[MaxMonkeys];

        /// <summary>
        /// Cada miquito empieza a entrar desde el follaje de su esquina (ms; D-011, punto 3). En la larga, la tropa de cada
        /// esquina sale a la vez, nunca antes de que el río desborde (A3); en la corta, desde t = 0 como pronto (0, con
        /// <see cref="MonkeyEnterEndMs"/> = 0: posado desde t = 0, sin entrada).
        /// </summary>
        public readonly float[] MonkeyEnterStartMs = new float[MaxMonkeys];

        /// <summary>
        /// Cada miquito llega a su primer sitio (ms): en la larga, su llegada de siempre a la primera columna; en la corta, ≥ 50 ms
        /// antes de su primera suelta. Desde aquí su trayectoria es la del calendario.
        /// </summary>
        public readonly float[] MonkeyEnterEndMs = new float[MaxMonkeys];

        /// <summary>Claves de trayectoria por miquito.</summary>
        public readonly int[] MonkeyKeyCount = new int[MaxMonkeys];

        /// <summary>Tiempo de cada clave: <c>[m · MaxKeys + j]</c>.</summary>
        public readonly float[] MonkeyKeyT = new float[MaxMonkeys * MaxKeys];

        /// <summary>x del centro del cuerpo (la raíz del rig) en cada clave: <c>[m · MaxKeys + j]</c>.</summary>
        public readonly float[] MonkeyKeyX = new float[MaxMonkeys * MaxKeys];

        /// <summary>Orientación desde cada clave (+1 derecha, −1 izquierda): <c>[m · MaxKeys + j]</c>.</summary>
        public readonly int[] MonkeyKeyFacing = new int[MaxMonkeys * MaxKeys];

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

        /// <summary>Lado del sprite de la fruta en vuelo: el visible (<see cref="FruitFlightVisibleDp"/>) / <see cref="FruitArtFill"/>.</summary>
        public float FruitFlightDp { get; private set; }

        /// <summary>Lado del sprite de la fruta al impacto: el visible (<see cref="FruitImpactVisibleDp"/>) / <see cref="FruitArtFill"/>.</summary>
        public float FruitImpactDp { get; private set; }

        /// <summary>Fruta en vuelo a la vista: 0,6 u, con un mínimo de 8 dp (siempre menor que la boca de 0,68 u).</summary>
        public float FruitFlightVisibleDp { get; private set; }

        /// <summary>Fruta al impacto a la vista: 0,45 u, con un mínimo de 8 dp.</summary>
        public float FruitImpactVisibleDp { get; private set; }

        /// <summary>u en dp.</summary>
        public float Unit { get; private set; }

        /// <summary>Posadero efectivo.</summary>
        public IntroPerch Perch { get; private set; }

        /// <summary>Modelo efectivo.</summary>
        public TroopModel Model { get; private set; }

        /// <summary>Tamaño del miquito en pantalla: min(tam_dp, franja − 10).</summary>
        public float MonkeyDp { get; private set; }

        /// <summary>
        /// Tamaño del rig: el de <see cref="MonkeyDp"/> salvo en <c>cuelga</c>, donde el cuerpo colgado se reduce para acabar
        /// ≥ 8 dp por encima del panel.
        /// </summary>
        public float BodyDp { get; private set; }

        /// <summary>Mano que suelta delante del centro del cuerpo (dp; 0 en <c>sacude</c>: la fruta cae de la rama).</summary>
        public float HandOffsetDp { get; private set; }

        /// <summary>y del agarre del cuerpo colgado en <c>cuelga</c> (dp desde arriba).</summary>
        public float HangGripY { get; private set; }

        /// <summary>Miquitos en pantalla: hasta clamp(tropa, 2, columnas con contenido), con vecinos a ≥ 1,1 S.</summary>
        public int MonkeyCount { get; private set; }

        /// <summary>
        /// La pareja de <c>colas_entrelazadas</c> sentada junta, lado a lado y mirando al mismo sitio, con las colas colgando
        /// bajo la rama, trenzadas (modelo <c>sacude</c>). Sin sitio en la franja para las colas, se sientan como los demás.
        /// </summary>
        public bool Pair { get; private set; }

        /// <summary>Lo que cuelgan bajo la rama las colas trenzadas de la pareja (dp; 0 sin pareja).</summary>
        public float PairTailHangDp { get; private set; }

        /// <summary>
        /// y de la rama de la tropa (dp desde arriba): el punto más bajo de la rama baja (en el centro) o la altura de la alta.
        /// Con la pareja, la rama baja sube lo que cuelgan sus colas.
        /// </summary>
        public float BranchY { get; private set; }

        /// <summary>y de la rama baja (panel.top − 8).</summary>
        public float LowBranchY { get; private set; }

        /// <summary>
        /// Referencia del calendario de la tropa en la versión larga (0 en la corta): la fila 1 está llena. Cada llegada a la
        /// primera columna es la de salir de aquí a 450 dp/s; la entrada que se ve empieza antes (<see cref="MonkeyEnterStartMs"/>)
        /// y llega en ese mismo instante (D-011, punto 3).
        /// </summary>
        public float TroopEnterMs { get; private set; }

        /// <summary>
        /// La tropa empieza a salir: max(T, última suelta + <see cref="RestMs"/>, fin del último gesto); sin tropa, T. Entre T
        /// y aquí reposa en la rama mientras ya se juega (D-011, punto 3).
        /// </summary>
        public float ExitStartMs { get; private set; }

        /// <summary>Última suelta de la tropa (ms; −1 sin tropa ni frutas).</summary>
        public float LastReleaseMs { get; private set; }

        /// <summary>Columnas con contenido.</summary>
        public int ColumnCount { get; private set; }

        /// <summary>Variación determinista: con una tropa de menos de 4, entra por la derecha.</summary>
        public bool EntryFromRight { get; private set; }

        /// <summary>La tropa entra por las dos esquinas (larga): las pasadas van de fuera hacia dentro.</summary>
        public bool EntryTwoSides { get; private set; }

        /// <summary>Variación determinista: el que lidera la fila va 0,3 S más adelantado.</summary>
        public bool LeaderSwap { get; private set; }

        /// <summary>
        /// Variación determinista de reposo (F13): 0 rascarse, 1 mirar al cielo, 2 acicalarse. Desde D-011 también rota el gesto
        /// de reposo de cada miquito tras su última suelta (<see cref="TroopLayer.ChooseRest"/>).
        /// </summary>
        public int RestVariant { get; private set; }

        /// <summary>Planifica la intro para <paramref name="input"/>. Sin asignaciones.</summary>
        public void Plan(IntroInput input)
        {
            int n = Clamp(input.Count, 0, N);
            CuiaCount = n;
            float total = Math.Max(1f, input.TotalMs);
            TotalMs = total;
            IsLong = input.Version == IntroVersion.Auto ? total >= LongFromMs : input.Version == IntroVersion.Long;
            float s = total / (IsLong ? 1400f : 800f);
            Scale = s;
            Rows = Clamp(input.Rows, 1, IntroInput.MaxRows);
            Unit = Math.Max(1f, input.Unit);
            _canvasW = input.CanvasWidth;
            FruitFlightVisibleDp = Math.Max(8f, 0.6f * Unit);
            FruitImpactVisibleDp = Math.Max(8f, 0.45f * Unit);
            FruitFlightDp = FruitFlightVisibleDp / FruitArtFill;
            FruitImpactDp = FruitImpactVisibleDp / FruitArtFill;

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

        /// <summary>Orientación del miquito <paramref name="m"/> en <paramref name="t"/> (+1 mira a la derecha): nunca a una cuia.</summary>
        public int MonkeyFacing(int m, float t)
        {
            int count = MonkeyKeyCount[m];
            int b = m * MaxKeys;
            int facing = MonkeyStartFacing[m];
            for (int j = 0; j < count; j++)
            {
                if (MonkeyKeyT[b + j] > t)
                {
                    break;
                }

                facing = MonkeyKeyFacing[b + j];
            }

            return facing;
        }

        /// <summary>x del primer sitio del miquito <paramref name="m"/> (al acabar su entrada): Reducir movimiento lo deja ahí, quieto.</summary>
        public float MonkeySeatX(int m)
        {
            return MonkeyX(m, MonkeyEnterEndMs[m]);
        }

        /// <summary>Orientación del miquito <paramref name="m"/> en su primer sitio (al acabar su entrada).</summary>
        public int MonkeySeatFacing(int m)
        {
            return MonkeyFacing(m, MonkeyEnterEndMs[m]);
        }

        /// <summary>x de la mano que suelta del miquito <paramref name="m"/> en <paramref name="t"/> (en <c>sacude</c>, el centro).</summary>
        public float MonkeyHandX(int m, float t)
        {
            return MonkeyX(m, t) + (MonkeyFacing(m, t) * HandOffsetDp);
        }

        /// <summary>
        /// y de la rama de la tropa en <paramref name="x"/> (dp desde arriba): sube hacia el follaje de las esquinas. La baja
        /// tiene su punto más bajo en el centro, en <see cref="BranchY"/>, y sube 6 dp en los extremos; la alta baja 3 dp en el
        /// centro y sube 1 dp en los extremos (sin pasar sobre el HUD).
        /// </summary>
        public float BranchCurveY(float x)
        {
            float u = Math.Min(1f, Math.Max(0f, (x + BranchOverhangDp) / (_canvasW + (2f * BranchOverhangDp))));
            return BranchY - _corner + (4f * u * (1f - u) * (_sag + _corner));
        }

        /// <summary>La rama sobresale 12 dp por cada lado (sale del follaje de las esquinas).</summary>
        public const float BranchOverhangDp = 12f;

        /// <summary>x del centro del cuerpo del miquito <paramref name="m"/> en <paramref name="t"/> (interpolación lineal de sus claves).</summary>
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
                    if (input.IsInert(i, k))
                    {
                        k++; // las ocultas (guion §10) y el Barro no forman racha ni reciben fruta
                        continue;
                    }

                    int tint = input.Tints[(i * D) + k];
                    int from = k;
                    while (k + 1 < doses && !input.IsInert(i, k + 1) && input.Tints[(i * D) + k + 1] == tint)
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
            _corner = model == TroopModel.Hang ? 1f : 6f;
            _sag = model == TroopModel.Hang ? 3f : 0f;
            PairTailHangDp = 0f;

            // Cuelga: el cuerpo colgado acaba ≥ 8 dp por encima del panel (la mano libre, con la fruta, en la suelta).
            float hangRelease = input.PanelTop - LowBranchAbovePanelDp - 2f;
            float body = MonkeyDp;
            if (model == TroopModel.Hang && perch == IntroPerch.Branch)
            {
                body = Math.Max(12f, Math.Min(MonkeyDp, (hangRelease - (BranchY + 4f)) / HangDepthS));
            }

            _hangRelease = hangRelease;
            SetBody(body);
            int maxM = perch == IntroPerch.Branch ? Math.Min(Math.Min(Math.Max(input.Troop, 2), ColumnCount), MaxMonkeys) : 0;
            Pair = maxM >= 2 && input.Pair && model == TroopModel.Shake;
            if (Pair)
            {
                // Las colas cuelgan bajo la rama, trenzadas: la rama sube lo que cuelgan (la punta, ≥ 6 dp sobre el panel), sube
                // solo 2 dp hacia las esquinas, y el tamaño se ajusta para que la pareja, sentada o entrando por la rama desde el
                // follaje de las esquinas, no pase sobre el HUD. Si no cabe con ≥ 28 dp, sin pareja.
                float fit = (band - PairTailClearDp - 2f - PairCornerRiseDp) / (SitTopS + PairTailHangS);
                float pairBody = Math.Min(body, fit);
                if (pairBody >= MinMonkeyDp)
                {
                    body = pairBody;
                    MonkeyDp = Math.Min(MonkeyDp, pairBody);
                    SetBody(body);
                    PairTailHangDp = PairTailHangS * body;
                    BranchY = Math.Min(LowBranchY, input.PanelTop - PairTailClearDp - PairTailHangDp);
                    _corner = PairCornerRiseDp;
                }
                else
                {
                    Pair = false;
                }
            }
            for (int j = 0; j < MaxMonkeys; j++)
            {
                MonkeyKeyCount[j] = 0;
                MonkeyFromRight[j] = false;
                MonkeyExitDir[j] = -1;
                MonkeyStartFacing[j] = 1;
                MonkeyGroupX[j] = 0f;
            }

            EntryTwoSides = false;
            if (maxM == 0)
            {
                MonkeyCount = 0;
                return;
            }

            // Columnas en grupos contiguos, uno por miquito, y un carril propio para el centro de cada uno: los carriles de
            // dos vecinos quedan a ≥ 1,1 S (la frontera, a medio camino entre sus columnas), así que dos miquitos nunca se
            // solapan aunque las columnas estén juntas (filas de 4 y 3 al tresbolillo). Se usan tantos miquitos como quepan
            // (hasta clamp(tropa, 2, columnas)) con todas sus columnas al alcance de la mano. La pareja de colas_entrelazadas
            // se reparte el tablero por mitades y se sienta junta en el centro del panel, lado a lado y mirando al mismo sitio
            // (a lo largo de la rama, nunca a una cuia; el lado alterna con la variación).
            float mid = input.CanvasWidth * 0.5f;
            int m = maxM;
            if (Pair)
            {
                m = 2;
                AssignGroups(m);
                float c = input.PanelX + (input.PanelWidth * 0.5f);
                int look = EntryFromRight ? -1 : 1;
                for (int g = 0; g < 2; g++)
                {
                    float seat = ClampEdges(c + ((g == 0 ? -0.5f : 0.5f) * PairGapS * body));
                    _laneLo[g] = seat;
                    _laneHi[g] = seat;
                    _seat[g] = seat;
                    _seatFacing[g] = look;
                }
            }
            else
            {
                // Con las columnas muy juntas, antes que quitar miquitos se encogen un poco (hasta el 85 %): la tropa del
                // nivel 7 (columnas a 42 dp) cabe entera a 38 dp, hombro con hombro.
                float full = body;
                float chosenBody = full;
                int chosen = 1;
                for (int mm = maxM; mm >= 1; mm--)
                {
                    bool ok = false;
                    float floor = Math.Max(12f, ShrinkToFit * full);
                    for (float s2 = full; s2 >= floor - 0.001f; s2 -= 1f)
                    {
                        SetBody(s2);
                        if (AssignLanes(mm))
                        {
                            ok = true;
                            break;
                        }
                    }

                    if (ok)
                    {
                        chosen = mm;
                        chosenBody = BodyDp;
                        break;
                    }
                }

                m = chosen;
                SetBody(chosenBody);
                MonkeyDp *= chosenBody / Math.Max(1f, full);
                AssignLanes(m);

                for (int g = 0; g < m; g++)
                {
                    _seat[g] = ClampCenter(g, MonkeyGroupX[g]);
                    _seatFacing[g] = _seat[g] < mid ? 1 : -1;
                }
            }

            MonkeyCount = m;

            // Entrada (larga): con tropa de 4 o más, cada miquito por la esquina más cercana a su grupo (la mitad por cada
            // lado, guion §7.2); con menos, todos por un solo lado que alterna con la variación (§7.4, paso 8), salvo que el
            // primero de la fila no llegue a su columna antes de la primera suelta (el lado único se pensó para recorridos
            // cortos: R2): entonces también por las dos esquinas (la pareja se encuentra en el centro).
            bool twoSides = m >= 4 && !Pair;
            if (!twoSides && m >= 2 && IsLong)
            {
                float far = EntryFromRight ? input.CanvasWidth - _seat[0] : _seat[m - 1];
                float window = DStartMs - Row1FullMs - ((ReleaseAfterArrivalMs + 130f) * Scale);
                twoSides = far / MonkeySpeedDpPerMs > window;
            }

            EntryTwoSides = twoSides;
            int lefts = 0;
            int rights = 0;
            for (int g = 0; g < m; g++)
            {
                lefts += MonkeyGroupX[g] < mid - 0.5f ? 1 : 0;
                rights += MonkeyGroupX[g] > mid + 0.5f ? 1 : 0;
            }

            for (int g = 0; g < m; g++)
            {
                float x = MonkeyGroupX[g];
                bool tie = Math.Abs(x - mid) <= 0.5f;
                MonkeyFromRight[g] = twoSides ? (tie ? (lefts == rights ? EntryFromRight : rights < lefts) : x > mid) : EntryFromRight;
            }
        }

        // Tamaño del rig y lo que depende de él (mano y agarre del colgado).
        private void SetBody(float body)
        {
            BodyDp = body;
            HangGripY = _hangRelease - (HangDepthS * body);
            HandOffsetDp = Model == TroopModel.Shake ? 0f : (Model == TroopModel.Hang ? HangHandOffsetS : SitHandOffsetS) * body;
        }

        // Grupos contiguos y equilibrados de columnas para m miquitos, con el centro de cada grupo.
        private void AssignGroups(int m)
        {
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
                _groupLo[g] = lo;
                _groupHi[g] = hi;
            }
        }

        // Carriles de m miquitos: entre dos grupos vecinos, la frontera a medio camino y 0,55 S de margen a cada lado; los de
        // los extremos, hasta el borde de la pantalla. Vale si ningún carril queda vacío y, en fila y cuelga, la mano llega a
        // todas sus columnas con lo que deja el impulso lateral más corto (0,25 dp/ms × 80 ms = 20 dp).
        private bool AssignLanes(int m)
        {
            AssignGroups(m);
            float half = 0.5f * NeighborGapS * BodyDp;
            for (int g = 0; g < m; g++)
            {
                float lo = g == 0 ? EdgeLo() : ((_groupHi[g - 1] + _groupLo[g]) * 0.5f) + half;
                float hi = g == m - 1 ? EdgeHi() : ((_groupHi[g] + _groupLo[g + 1]) * 0.5f) - half;
                _laneLo[g] = Math.Max(lo, EdgeLo());
                _laneHi[g] = Math.Min(hi, EdgeHi());
                if (_laneLo[g] > _laneHi[g] + 0.001f)
                {
                    return false;
                }
            }

            float reach = MaxTossDpPerMs * MinFallMs;
            for (int j = 0; j < ColumnCount; j++)
            {
                int g = ColumnGroup[j];
                float col = ColumnX[j];
                float drift = Math.Max(0f, Math.Max(_laneLo[g] - HandOffsetDp - col, col - (_laneHi[g] + HandOffsetDp)));
                if (Model != TroopModel.Shake && drift > reach)
                {
                    return false;
                }
            }

            return true;
        }

        private float EdgeLo()
        {
            return (0.5f * BodyDp) + EdgeMarginDp;
        }

        private float EdgeHi()
        {
            return _canvasW - (0.5f * BodyDp) - EdgeMarginDp;
        }

        // Centro del cuerpo dentro de [0,5·S + 6, ancho − 0,5·S − 6].
        private float ClampEdges(float x)
        {
            float lo = EdgeLo();
            float hi = EdgeHi();
            return hi < lo ? _canvasW * 0.5f : Math.Min(Math.Max(x, lo), hi);
        }

        // Centro del cuerpo del miquito m dentro de su carril (y de la pantalla).
        private float ClampCenter(int m, float x)
        {
            return Math.Min(Math.Max(x, _laneLo[m]), _laneHi[m]);
        }

        private void BuildFruits(IntroInput input, int n)
        {
            // Pasadas en el sentido de la tropa: de la esquina de entrada hacia la otra o, si entra por las dos, de fuera
            // hacia dentro (primero las columnas a las que llega antes).
            bool descending = Perch == IntroPerch.Branch && !EntryTwoSides && EntryFromRight;
            bool outsideIn = Perch == IntroPerch.Branch && EntryTwoSides;
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
                    while (at > 0 && Before(input.CenterX[i], input.CenterX[_sorted[at - 1]], descending, outsideIn, input.CanvasWidth))
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
            uint spin = Mix(input.Seed ^ 0x51ED27);
            for (int k = 0; k < count; k++)
            {
                int c = FruitCuia[k];
                FruitImpactX[k] = input.CenterX[c];
                FruitImpactY[k] = SurfaceY[c];
                FruitReleaseX[k] = input.CenterX[c];
                FruitReleaseY[k] = ReleaseY(input, input.CenterX[c]);
                spin = Next(spin);
                float deg = 180f + (180f * ((spin & 0xFFFF) / 65535f));
                FruitSpin[k] = ((spin >> 16) & 1u) != 0u ? deg / 1000f : -deg / 1000f;
            }

            if (count == 0)
            {
                InitMonkeys(input);
                FinishMonkeys(input);
                return;
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
                Move move = default;
                bool stay = false;
                float stayX = 0f;
                float minRelease = float.MinValue;
                if (Perch == IntroPerch.Branch && MonkeyCount > 0)
                {
                    m = ColumnGroup[Math.Max(0, CuiaColumn[c])];
                    imp = Math.Min(imp, total - dur);
                    if (TryStay(m, k, tau, imp - tau, out stayX))
                    {
                        stay = true; // la suelta cae en un rato en que ya está quieto (las frutas de la fila de abajo caen más)
                    }
                    else
                    {
                        // Si no llega a tiempo (o su ciclo de suelta está ocupado), se aprieta el calendario hacia T (paso 7).
                        minRelease = Choose(m, k, tau, imp - tau, out move);
                        imp = Math.Max(imp, NextFree(m, Math.Max(imp - tau, minRelease)) + tau);
                    }
                }

                imp = Math.Min(imp, total - dur); // nunca se pasa de T
                float release = imp - tau;
                if (m >= 0 && !stay)
                {
                    if (release + 0.01f < minRelease || !CycleFree(m, release))
                    {
                        // No cabe con su miquito: pasa al vecino que llegue a tiempo desde su carril o, si nadie llega, cae de
                        // la rama de la tropa sobre su columna.
                        int chosen = -1;
                        for (int d = -1; d <= 1 && chosen < 0; d += 2)
                        {
                            int other = m + d;
                            if (other < 0 || other >= MonkeyCount)
                            {
                                continue;
                            }

                            if (TryStay(other, k, tau, release, out stayX))
                            {
                                chosen = other;
                                stay = true;
                                continue;
                            }

                            float need = Choose(other, k, tau, release, out Move alt);
                            if (release + 0.01f >= need && CycleFree(other, release))
                            {
                                chosen = other;
                                move = alt;
                            }
                        }

                        if (chosen < 0)
                        {
                            // Último recurso antes de la rama: su propio miquito, aunque junte más de 2 frutas en un ciclo de
                            // suelta (se ve como una mano que deja caer fruta tras fruta), mejor que una fruta sin miquito.
                            _crowdedCycles = true;
                            if (TryStay(m, k, tau, release, out stayX))
                            {
                                chosen = m;
                                stay = true;
                            }
                            else if (release + 0.01f >= Choose(m, k, tau, release, out Move own))
                            {
                                chosen = m;
                                move = own;
                            }

                            _crowdedCycles = false;
                        }

                        m = chosen;
                    }

                    if (m >= 0 && !stay)
                    {
                        Commit(m, move, release);
                        FruitReleaseX[k] = move.ReleaseX; // desde la mano sobre el eje (o, a una columna vecina, con ≤ 250 dp/s)
                    }
                    else if (m < 0)
                    {
                        // Ningún miquito llega a tiempo (tropas pequeñas con muchas columnas: churuco, mono nocturno): la
                        // fruta cuelga de la rama de la tropa sobre su columna y cae de ella, como al sacudirla; nunca del
                        // cielo, por detrás del HUD, con la tropa a la vista.
                        FruitReleaseY[k] = BranchDropY(FruitImpactX[k]);
                        tau = FallTime(k);
                        release = imp - tau;
                    }
                }

                if (m >= 0 && stay)
                {
                    LogRelease(m, release);
                    FruitReleaseX[k] = stayX;
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

        /// <summary>Altura de la mano que suelta, sentado, sobre la rama (S): <c>MonkeyRig.SitHandS.y</c>.</summary>
        public const float SitHandHeightS = 0.36f;

        // y de la suelta: la mano del miquito sentado, la mano libre del colgado (≥ 8 dp sobre el panel), la rama sacudida o
        // el dosel por detrás del HUD.
        private float ReleaseY(IntroInput input, float x)
        {
            if (Perch == IntroPerch.Canopy || MonkeyCount == 0)
            {
                return input.HudBottom - CanopyAboveHudDp;
            }

            switch (Model)
            {
                case TroopModel.Hang:
                    return input.PanelTop - LowBranchAbovePanelDp - 2f;
                case TroopModel.Shake:
                    return BranchCurveY(x) - 2f;
                default:
                    return BranchCurveY(x) - (SitHandHeightS * BodyDp);
            }
        }

        /// <summary>
        /// y desde la que cae una fruta que ningún miquito alcanza en el modo rama (dp desde arriba): cuelga de la rama de la
        /// tropa en <paramref name="x"/> (bajo la rama alta en <c>cuelga</c>; sobre la baja en <c>fila</c> y <c>sacude</c>).
        /// </summary>
        public float BranchDropY(float x)
        {
            return Model == TroopModel.Hang ? BranchCurveY(x) + 3f : BranchCurveY(x) - 2f;
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
            float size = BodyDp;
            float spacing = NeighborGapS * size;
            float mid = input.CanvasWidth * 0.5f;
            for (int g = 0; g < m; g++)
            {
                _relCount[g] = 0;
                MonkeyKeyCount[g] = 0;
                float x;
                int f;
                if (IsLong)
                {
                    // Oculta en el follaje de su esquina; entra cuando la fila 1 está llena, en fila a 1,1 S, el que va más
                    // lejos primero (con la variación, el primero va 0,3 S más adelantado). Nadie adelanta a nadie.
                    bool right = MonkeyFromRight[g];
                    int order = 0;
                    for (int o = 0; o < m; o++)
                    {
                        if (MonkeyFromRight[o] == right && (right ? o < g : o > g))
                        {
                            order++;
                        }
                    }

                    float back = (0.6f * size) + HandOffsetDp + (order * spacing) + (LeaderSwap && order > 0 ? 0.3f * size : 0f);
                    x = right ? input.CanvasWidth + back : -back;
                    f = right ? -1 : 1;
                    _mt[g] = TroopEnterMs;
                    _arrived[g] = float.MinValue;
                }
                else
                {
                    // Posada desde t = 0: en sacude, en su asiento; si no, con la mano sobre la primera columna que atiende.
                    if (Model == TroopModel.Shake)
                    {
                        x = _seat[g];
                        f = _seatFacing[g];
                    }
                    else
                    {
                        float col = FirstColumnOf(g);
                        f = col < mid ? 1 : -1;
                        x = ClampCenter(g, col - (f * HandOffsetDp));
                        float other = ClampCenter(g, col + (f * HandOffsetDp));
                        if (Math.Abs(other - (f * HandOffsetDp) - col) + 0.01f < Math.Abs(x + (f * HandOffsetDp) - col))
                        {
                            f = -f; // de la otra mano llega mejor a la columna desde su carril
                            x = other;
                        }
                    }

                    _mt[g] = 0f;
                    _arrived[g] = -1000f;
                }

                _mx[g] = x;
                _facing[g] = f;
                MonkeyStartFacing[g] = f;
                AddKey(g, 0f, x, f);
            }

            if (!IsLong)
            {
                return;
            }

            // Larga: cuando la fila 1 está llena, todos salen a la vez del follaje hacia su primera columna (o su asiento) a
            // ≤ 450 dp/s, en fila: nadie adelanta a nadie y cada uno se queda en su carril.
            for (int g = 0; g < m; g++)
            {
                AddKey(g, TroopEnterMs, _mx[g], _facing[g]);
                Move entry = MoveTo(g, Model == TroopModel.Shake ? _seat[g] : FirstColumnOf(g));
                if (entry.Moved)
                {
                    AddKey(g, entry.Arrive, entry.Target, entry.EndFacing);
                    _mx[g] = entry.Target;
                    _arrived[g] = entry.Arrive;
                    _mt[g] = entry.Arrive;
                }

                _facing[g] = entry.EndFacing;
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

        // Un desplazamiento del miquito m para soltar una fruta: adónde va (centro del cuerpo), con qué orientación viaja y
        // queda, cuándo llega y desde dónde sale la fruta.
        private struct Move
        {
            public float Target;
            public int TravelFacing;
            public int EndFacing;
            public float Arrive;
            public float ReleaseX;
            public bool Moved;
        }

        // Suelta más temprana del miquito m para la fruta k (que cae en tau; su suelta ideal es idealRelease) y el
        // desplazamiento elegido. En fila y cuelga, la mano queda sobre el eje de la columna (la fruta cae recta); solo si así
        // no llega a tiempo y la columna es vecina en su grupo, la suelta desde donde está con ≤ 250 dp/s de impulso lateral
        // (guion §4.4). En sacude, va a su asiento y la fruta cae de la rama sobre la columna.
        private float Choose(int m, int k, float tau, float idealRelease, out Move move)
        {
            float col = FruitImpactX[k];
            float reach = MaxTossDpPerMs * tau;
            move = MoveTo(m, col);
            float min = Model != TroopModel.Shake && Math.Abs(move.ReleaseX - col) > reach + 0.01f ? float.MaxValue : MinRelease(m, move);
            if (Model != TroopModel.Shake && move.Moved && min > idealRelease + 0.01f)
            {
                float hand = _mx[m] + (_facing[m] * HandOffsetDp);
                float drift = Math.Abs(col - hand);
                if (drift <= reach && InGroup(m, col))
                {
                    var stay = new Move
                    {
                        Target = _mx[m],
                        TravelFacing = _facing[m],
                        EndFacing = _facing[m],
                        Arrive = _arrived[m],
                        ReleaseX = hand,
                        Moved = false,
                    };
                    float minStay = MinRelease(m, stay);
                    if (minStay < min)
                    {
                        move = stay;
                        min = minStay;
                    }
                }
            }

            return min;
        }

        private Move MoveTo(int m, float col)
        {
            var move = default(Move);
            float x = _mx[m];
            int f = _facing[m];
            if (Model == TroopModel.Shake)
            {
                move.Target = _seat[m];
                float dx = move.Target - x;
                move.TravelFacing = Math.Abs(dx) < 0.5f ? f : (dx > 0f ? 1 : -1);
                move.EndFacing = Pair ? _seatFacing[m] : move.TravelFacing;
                move.ReleaseX = col;
            }
            else
            {
                float off = HandOffsetDp;
                if (Math.Abs(col - (x + (f * off))) < 0.5f)
                {
                    move.Target = x; // ya tiene la mano sobre el eje
                    move.TravelFacing = f;
                    move.EndFacing = f;
                }
                else
                {
                    // La mano sobre el eje con cada orientación, dentro de su carril; gana la que deja la mano más cerca del
                    // eje y, a igualdad, la que llega andando hacia delante (sin darse la vuelta al llegar) o la de ahora.
                    float a = ClampCenter(m, col - off);
                    float b = ClampCenter(m, col + off);
                    float da = Math.Abs(a + off - col);
                    float db = Math.Abs(b - off - col);
                    bool forwardA = a - x > 0.5f;
                    bool forwardB = x - b > 0.5f;
                    bool pickA = da + 0.01f < db || (Math.Abs(da - db) <= 0.01f && (forwardA != forwardB ? forwardA : f > 0));
                    move.Target = pickA ? a : b;
                    move.EndFacing = pickA ? 1 : -1;
                    float dx = move.Target - x;
                    move.TravelFacing = Math.Abs(dx) < 0.5f ? f : (dx > 0f ? 1 : -1);
                }

                move.ReleaseX = move.Target + (move.EndFacing * off);
            }

            float dist = Math.Abs(move.Target - x);
            move.Moved = dist >= 0.5f;
            move.Arrive = move.Moved ? _mt[m] + (dist / MonkeySpeedDpPerMs) : _arrived[m];
            return move;
        }

        // Ciclo de suelta (guion §7.3): 1-2 frutas por ciclo (una en cada mano, sin moverse) y el siguiente ciclo, al acabar
        // el gesto del anterior; nunca antes de 50 ms tras llegar y, en la corta, nunca antes del primer ciclo (100 ms).
        private float MinRelease(int m, Move move)
        {
            float min = move.Arrive + ReleaseAfterArrivalMs + 0.05f;
            if (!IsLong)
            {
                min = Math.Max(min, 100f * Scale);
            }

            return NextFree(m, min);
        }

        // Primera suelta ≥ r que deja libre el ciclo de suelta del miquito m.
        private float NextFree(int m, float r)
        {
            int b = m * MaxFruits;
            for (int guard = 0; guard <= MaxFruits && !CycleFree(m, r); guard++)
            {
                float next = r + 1f;
                for (int i = 0; i < _relCount[m]; i++)
                {
                    float x = _relLog[b + i];
                    if (Math.Abs(x - r) < GestureTailMs)
                    {
                        next = Math.Max(next, x + GestureTailMs);
                    }
                }

                r = next;
            }

            return r;
        }

        // Como mucho 2 frutas por ciclo de suelta (una en cada mano, guion §7.3): a menos de 40 ms (el final del gesto) de una
        // suelta nueva solo puede haber otra suelta del mismo miquito.
        private bool CycleFree(int m, float r)
        {
            if (Model == TroopModel.Shake || _crowdedCycles)
            {
                return true; // una sacudida de la rama suelta varias frutas a la vez
            }

            int b = m * MaxFruits;
            int near = 0;
            for (int i = 0; i < _relCount[m]; i++)
            {
                if (Math.Abs(_relLog[b + i] - r) < GestureTailMs)
                {
                    near++;
                }
            }

            return near <= 1;
        }

        private void LogRelease(int m, float r)
        {
            if (_relCount[m] < MaxFruits)
            {
                _relLog[(m * MaxFruits) + _relCount[m]] = r;
                _relCount[m]++;
            }
        }

        // Suelta en r desde donde el miquito m ya está quieto según su trayectoria comprometida (sin moverlo): llegó hace
        // ≥ 50 ms, no se va antes de acabar el gesto (40 ms), está en su carril, la fruta llega a su columna con ≤ 250 dp/s
        // y el ciclo de suelta lo permite. Así una fruta de la fila de abajo (cae más, sale antes) se suelta mientras el
        // miquito atiende la de arriba, a veces con la otra mano en el mismo ciclo.
        private bool TryStay(int m, int k, float tau, float r, out float releaseX)
        {
            releaseX = 0f;
            int count = MonkeyKeyCount[m];
            if (count == 0 || r >= _mt[m] - 0.01f || (!IsLong && r < 100f * Scale) || (IsLong && r < TroopEnterMs))
            {
                return false;
            }

            int b = m * MaxKeys;
            int j = -1;
            for (int i = 0; i < count; i++)
            {
                if (MonkeyKeyT[b + i] <= r)
                {
                    j = i;
                }
                else
                {
                    break;
                }
            }

            if (j < 0)
            {
                return false;
            }

            float x = MonkeyKeyX[b + j];
            float depart = float.MaxValue;
            for (int i = j + 1; i < count; i++)
            {
                if (Math.Abs(MonkeyKeyX[b + i] - x) > 0.01f)
                {
                    depart = MonkeyKeyT[b + i - 1];
                    break;
                }
            }

            if (j + 1 < count && Math.Abs(MonkeyKeyX[b + j + 1] - x) > 0.01f)
            {
                return false; // de camino
            }

            int s = j;
            while (s > 0 && Math.Abs(MonkeyKeyX[b + s - 1] - x) < 0.01f)
            {
                s--;
            }

            float since = s == 0 ? (IsLong ? float.MaxValue : -1000f) : MonkeyKeyT[b + s];
            if (r < since + ReleaseAfterArrivalMs || r + GestureTailMs > depart || x < _laneLo[m] - 0.5f || x > _laneHi[m] + 0.5f
                || !CycleFree(m, r))
            {
                return false;
            }

            float col = FruitImpactX[k];
            if (Model == TroopModel.Shake)
            {
                releaseX = col;
                return true;
            }

            float hand = x + (MonkeyFacing(m, r) * HandOffsetDp);
            if (Math.Abs(col - hand) > (MaxTossDpPerMs * tau) + 0.01f)
            {
                return false;
            }

            releaseX = hand;
            return true;
        }

        private bool InGroup(int m, float col)
        {
            for (int j = 0; j < ColumnCount; j++)
            {
                if (Math.Abs(ColumnX[j] - col) < 1f)
                {
                    return ColumnGroup[j] == m;
                }
            }

            return false;
        }

        private void Commit(int m, Move move, float release)
        {
            if (move.Moved)
            {
                AddKey(m, _mt[m], _mx[m], move.TravelFacing);
                AddKey(m, move.Arrive, move.Target, move.EndFacing); // al llegar se queda (la pareja, mirando al mismo sitio)
                _mx[m] = move.Target;
                _arrived[m] = move.Arrive;
            }
            else if (move.EndFacing != _facing[m])
            {
                AddKey(m, _mt[m], _mx[m], move.EndFacing);
            }

            _facing[m] = move.EndFacing;
            _mt[m] = Math.Max(Math.Max(_mt[m], move.Arrive + MonkeyStopMs), release + GestureTailMs);
            LogRelease(m, release);
        }

        // Reposo y salida (D-011, punto 3): tras la última suelta, la tropa se queda ≈ 1.500 ms en la rama, ya en juego, y sale
        // toda a la vez hacia la esquina más cercana (de espaldas al tablero): los de cada esquina a la misma velocidad, la que
        // lleva al más lejano en 500 ms (≤ 450 dp/s); TroopLayer los eleva hacia el follaje de esa esquina. Después, la entrada
        // que se ve (PlanEntries), con el calendario ya cerrado.
        private void FinishMonkeys(IntroInput input)
        {
            float start = TotalMs;
            float last = -1f;
            if (MonkeyCount > 0)
            {
                for (int k = 0; k < FruitCount; k++)
                {
                    last = Math.Max(last, FruitReleaseMs[k]);
                }

                start = Math.Max(start, last + RestMs);
            }

            for (int g = 0; g < MonkeyCount; g++)
            {
                start = Math.Max(start, _mt[g]);
            }

            LastReleaseMs = last;
            ExitStartMs = start;
            float size = BodyDp;
            float left = 0f;
            float right = 0f;
            for (int g = 0; g < MonkeyCount; g++)
            {
                int dir = _mx[g] < input.CanvasWidth * 0.5f ? -1 : 1;
                MonkeyExitDir[g] = dir;
                float dist = Math.Abs(ExitCorner(input, dir) - _mx[g]);
                left = dir < 0 ? Math.Max(left, dist) : left;
                right = dir > 0 ? Math.Max(right, dist) : right;
            }

            // Los de cada esquina, a la misma velocidad (nadie alcanza a nadie): la que lleva al más lejano en 500 ms, ≤ 450 dp/s.
            for (int g = 0; g < MonkeyCount; g++)
            {
                int dir = MonkeyExitDir[g];
                float speed = Math.Max(0.01f, Math.Min(MonkeySpeedDpPerMs, (dir < 0 ? left : right) / ExitMs));
                float travel = Math.Min(Math.Abs(ExitCorner(input, dir) - _mx[g]), speed * ExitMs);
                AddKey(g, start, _mx[g], dir);
                AddKey(g, start + (travel / speed), _mx[g] + (dir * travel), dir);
            }

            PlanEntries(input);
        }

        // Esquina de la salida: el centro del cuerpo, 0,6 S + 6 dp fuera de la pantalla.
        private float ExitCorner(IntroInput input, int dir)
        {
            float off = (0.6f * BodyDp) + EdgeMarginDp;
            return dir < 0 ? -off : input.CanvasWidth + off;
        }

        // Entrada que se ve (D-011, punto 3), calculada después del calendario y sin tocarlo: solo cambia las claves de antes de
        // que cada miquito llegue a su primer sitio, así que _mt, _arrived, las sueltas y los impactos son los de antes.
        // Larga: la llegada a la primera columna es la de siempre (como si salieran a 450 dp/s cuando la fila 1 está llena); la
        // tropa de cada esquina sale a la vez ≈ 500 ms antes del primero en llegar, nunca antes de que el río desborde (A3:
        // primero el río), así que cada uno va a ≤ 450 dp/s. Corta: estaban posados desde t = 0; ahora entran desde el follaje
        // de su esquina más cercana (empate: el lado de la variación) y llegan, todos los de una esquina a la vez, ≥ 50 ms antes
        // de su primera suelta, al sitio donde su trayectoria los tiene en ese instante (si su primer desplazamiento era
        // temprano, ya en su destino: no se ve) y antes de dejarlo: la entrada dura min(500 ms, primera suelta − 50 ms). Los de
        // una misma esquina salen en fila a 1,1 S, el que va más lejos primero, y llegan a la vez:
        // como su distancia a su vecino cambia de forma lineal, nunca baja de 1,1 S ni se adelantan. Nadie entra a más de
        // 450 dp/s (R2): si no le da tiempo, se queda posado desde t = 0 el que va más lejos y la fila se acorta.
        private void PlanEntries(IntroInput input)
        {
            for (int g = 0; g < MaxMonkeys; g++)
            {
                MonkeyEnterStartMs[g] = 0f;
                MonkeyEnterEndMs[g] = 0f;
            }

            if (MonkeyCount == 0)
            {
                return;
            }

            if (IsLong)
            {
                PlanLongEntries();
            }
            else
            {
                PlanShortEntries(input);
            }
        }

        private void PlanLongEntries()
        {
            int m = MonkeyCount;
            for (int side = 0; side < 2; side++)
            {
                bool right = side == 1;
                float first = float.MaxValue;
                for (int g = 0; g < m; g++)
                {
                    if (MonkeyFromRight[g] == right && IsLongEntry(g))
                    {
                        first = Math.Min(first, MonkeyKeyT[(g * MaxKeys) + 2]);
                    }
                }

                if (first == float.MaxValue)
                {
                    continue;
                }

                float start = Math.Min(TroopEnterMs, first - EnterMs);
                start = Math.Max(start, Math.Min(SheetStartMs, TroopEnterMs));
                for (int g = 0; g < m; g++)
                {
                    if (MonkeyFromRight[g] != right)
                    {
                        continue;
                    }

                    int b = g * MaxKeys;
                    if (IsLongEntry(g))
                    {
                        MonkeyKeyT[b + 1] = start;
                        MonkeyEnterStartMs[g] = start;
                        MonkeyEnterEndMs[g] = MonkeyKeyT[b + 2];
                    }
                    else
                    {
                        MonkeyEnterStartMs[g] = TroopEnterMs;
                        MonkeyEnterEndMs[g] = TroopEnterMs;
                    }
                }
            }
        }

        // Claves de la entrada larga (InitMonkeys): en el follaje en t = 0, salida en TroopEnterMs y llegada a la primera columna.
        private bool IsLongEntry(int g)
        {
            int b = g * MaxKeys;
            return MonkeyKeyCount[g] >= 3 && Math.Abs(MonkeyKeyT[b + 1] - TroopEnterMs) < 0.001f
                && Math.Abs(MonkeyKeyX[b + 1] - MonkeyKeyX[b]) < 0.001f && Math.Abs(MonkeyKeyX[b + 2] - MonkeyKeyX[b + 1]) >= 0.5f;
        }

        private void PlanShortEntries(IntroInput input)
        {
            int m = MonkeyCount;
            float w = input.CanvasWidth;
            float mid = w * 0.5f;

            // Llegada de cada uno: ≥ 50 ms antes de su primera suelta (decisión 1), al sitio donde su trayectoria lo tiene en ese
            // instante. Si se movía antes (un primer desplazamiento temprano, que antes se veía posado desde t = 0), entra directo
            // a ese sitio y lo de antes no se ve; si ese sitio lo deja antes, llega antes de dejarlo. Entra si le quedan claves
            // libres y al menos 150 ms para entrar.
            for (int g = 0; g < m; g++)
            {
                int b = g * MaxKeys;
                int count = MonkeyKeyCount[g];
                float limit = TotalMs;
                for (int k = 0; k < FruitCount; k++)
                {
                    if (FruitMonkey[k] == g)
                    {
                        limit = Math.Min(limit, FruitReleaseMs[k] - ReleaseAfterArrivalMs);
                    }
                }

                _entryRelease[g] = limit;
                int anchor = 0;
                for (int j = 1; j < count; j++)
                {
                    if (MonkeyKeyT[b + j] > limit + 0.001f)
                    {
                        break;
                    }

                    anchor = j;
                }

                if (anchor + 1 < count && Math.Abs(MonkeyKeyX[b + anchor + 1] - MonkeyKeyX[b + anchor]) >= 0.01f)
                {
                    limit = Math.Min(limit, MonkeyKeyT[b + anchor]); // deja ese sitio antes: llega antes de irse
                }

                float seat = MonkeyKeyX[b + anchor];
                _entryAnchor[g] = anchor;
                _entrySeat[g] = seat;
                MonkeyFromRight[g] = Math.Abs(seat - mid) <= 0.5f ? EntryFromRight : seat > mid;
                _entryLimit[g] = limit;
                _entering[g] = limit >= MinShortEnterMs && count - anchor + 3 <= MaxKeys;
            }

            // Quien no puede entrar (menos de 150 ms o sin claves libres) sigue posado desde t = 0, como antes, y con él los de su
            // esquina que se sientan más lejos: los que entran nunca pasan por encima de un miquito sentado. Si alguno tuviera que
            // ir a más de 450 dp/s (R2: un borrón; la fila entera tras la cola de los de delante, con T corta), se sienta desde
            // t = 0 el que va más lejos y se vuelve a planificar con una fila más corta. Cada pasada fallida deja fuera a uno, así
            // que acaba; y como quien se queda posado puede estar en la mitad de la otra esquina (su sitio de t = 0 no es el de
            // su llegada), se repiten las dos esquinas hasta que nadie más se quede fuera.
            float leftStart = -1f;
            float rightStart = -1f;
            for (int round = 0; round <= MaxMonkeys; round++)
            {
                int before = EnteringCount();
                for (int side = 0; side < 2; side++)
                {
                    bool right = side == 1;
                    SeatBeyondTheSeated(right, w);
                    float start = -1f;
                    for (int pass = 0; pass <= MaxMonkeys; pass++)
                    {
                        start = PlanSide(right, w);
                        if (start < 0f || EntriesKeepTheirPace(right, w, start))
                        {
                            break;
                        }

                        int farthest = OrderOf(right, 0);
                        if (farthest < 0)
                        {
                            start = -1f;
                            break;
                        }

                        _entering[farthest] = false;
                    }

                    if (right)
                    {
                        rightStart = start;
                    }
                    else
                    {
                        leftStart = start;
                    }
                }

                if (EnteringCount() == before)
                {
                    break;
                }
            }

            for (int g = 0; g < m; g++)
            {
                bool right = MonkeyFromRight[g];
                float start = right ? rightStart : leftStart;
                if (start >= 0f && _entering[g])
                {
                    int travel = right ? -1 : 1;
                    PrependEntry(g, _entryAnchor[g], right ? w + _entryBack[g] : -_entryBack[g], travel, start, MonkeyEnterEndMs[g]);
                    MonkeyStartFacing[g] = travel;
                }
                else
                {
                    MonkeyEnterStartMs[g] = 0f;
                    MonkeyEnterEndMs[g] = 0f;
                }
            }
        }

        private int EnteringCount()
        {
            int count = 0;
            for (int g = 0; g < MonkeyCount; g++)
            {
                count += _entering[g] ? 1 : 0;
            }

            return count;
        }

        // Si alguno no entra (sigue posado desde t = 0), tampoco entran los de esa esquina que se sientan más lejos de ella que
        // él: cuenta cada sitio por el que pasa antes de su primera suelta, sea de la esquina que sea.
        private void SeatBeyondTheSeated(bool right, float width)
        {
            float nearest = float.MaxValue;
            for (int g = 0; g < MonkeyCount; g++)
            {
                if (_entering[g])
                {
                    continue;
                }

                int b = g * MaxKeys;
                nearest = Math.Min(nearest, FromCorner(MonkeyKeyX[b], width, right));
                for (int j = 1; j < MonkeyKeyCount[g] && MonkeyKeyT[b + j] <= _entryRelease[g] + 0.001f; j++)
                {
                    nearest = Math.Min(nearest, FromCorner(MonkeyKeyX[b + j], width, right));
                }
            }

            for (int g = 0; g < MonkeyCount; g++)
            {
                if (MonkeyFromRight[g] == right && FromCorner(_entrySeat[g], width, right) >= nearest)
                {
                    _entering[g] = false;
                }
            }
        }

        // Entrada de los que entran por esa esquina: salen a la vez, en fila desde su follaje (el que va más lejos, primero; con
        // la variación, 0,3 S más adelantado), y cada uno llega cuando le toca, en ≤ 500 ms. Devuelve la salida (−1: nadie entra).
        private float PlanSide(bool right, float width)
        {
            int m = MonkeyCount;
            float size = BodyDp;
            float spacing = NeighborGapS * size;
            float first = float.MaxValue;
            int count = 0;
            for (int g = 0; g < m; g++)
            {
                if (MonkeyFromRight[g] == right && _entering[g])
                {
                    first = Math.Min(first, _entryLimit[g]);
                    count++;
                }
            }

            if (count == 0)
            {
                return -1f;
            }

            float start = Math.Max(0f, first - EnterMs);
            for (int g = 0; g < m; g++)
            {
                if (MonkeyFromRight[g] != right || !_entering[g])
                {
                    continue;
                }

                int order = 0;
                for (int o = 0; o < m; o++)
                {
                    if (o != g && MonkeyFromRight[o] == right && _entering[o] && Ahead(_entrySeat[o], o, _entrySeat[g], g, right))
                    {
                        order++;
                    }
                }

                _entryOrder[g] = order;
                _entryBack[g] = (0.6f * size) + HandOffsetDp + (order * spacing) + (LeaderSwap && order > 0 ? 0.3f * size : 0f);
                MonkeyEnterStartMs[g] = start;
                MonkeyEnterEndMs[g] = Math.Min(_entryLimit[g], start + EnterMs);
            }

            // De atrás adelante: si el de delante llegaría más tarde que el de detrás, va lo bastante deprisa para que este,
            // al llegar a su sitio, siga a ≥ 1,1 S de él (las dos distancias a la esquina cambian de forma lineal).
            for (int k = count - 2; k >= 0; k--)
            {
                int front = OrderOf(right, k);
                int behind = OrderOf(right, k + 1);
                if (front < 0 || behind < 0 || MonkeyEnterEndMs[front] <= MonkeyEnterEndMs[behind])
                {
                    continue;
                }

                float a = FromCorner(_entrySeat[front], width, right) + _entryBack[front];
                float b = FromCorner(_entrySeat[behind], width, right) + spacing + _entryBack[front];
                float bound = (MonkeyEnterEndMs[behind] - start) * a / Math.Max(0.01f, b);
                MonkeyEnterEndMs[front] = start + Math.Min(MonkeyEnterEndMs[front] - start, bound);
            }

            return start;
        }

        // Los que entran por esa esquina tardan ≥ 150 ms y van a ≤ 450 dp/s.
        private bool EntriesKeepTheirPace(bool right, float width, float start)
        {
            for (int g = 0; g < MonkeyCount; g++)
            {
                if (MonkeyFromRight[g] != right || !_entering[g])
                {
                    continue;
                }

                float span = MonkeyEnterEndMs[g] - start;
                float dist = FromCorner(_entrySeat[g], width, right) + _entryBack[g];
                if (span < MinShortEnterMs - 0.01f || dist > (MonkeySpeedDpPerMs * span) + 0.01f)
                {
                    return false;
                }
            }

            return true;
        }

        // El miquito o (en seatO) va por delante del g (en seatG) en la fila que entra por la esquina derecha o izquierda.
        private static bool Ahead(float seatO, int o, float seatG, int g, bool right)
        {
            return right ? seatO < seatG || (seatO == seatG && o < g) : seatO > seatG || (seatO == seatG && o > g);
        }

        // Miquito de esa esquina con ese puesto en la fila (−1 si no hay).
        private int OrderOf(bool right, int order)
        {
            for (int g = 0; g < MonkeyCount; g++)
            {
                if (MonkeyFromRight[g] == right && _entering[g] && _entryOrder[g] == order)
                {
                    return g;
                }
            }

            return -1;
        }

        // Distancia de x a la esquina por la que se entra (dp).
        private static float FromCorner(float x, float width, bool right)
        {
            return right ? width - x : x;
        }

        // Antepone la entrada a las claves del miquito g: en el follaje (from) hasta start y, en end, en el sitio de su clave
        // anchor (la de t = 0 o, si su trayectoria ya lo había movido, la del sitio donde está ≥ 50 ms antes de su primera
        // suelta) con su orientación de siempre. Las claves de antes de anchor (lo que se movía mientras entraba) no se ven; las
        // de después no cambian. Si llega antes de la clave anchor, la conserva: espera en su sitio hasta ella.
        private void PrependEntry(int g, int anchor, float from, int travelFacing, float start, float end)
        {
            int b = g * MaxKeys;
            int count = MonkeyKeyCount[g];
            float seatT = MonkeyKeyT[b + anchor];
            float seatX = MonkeyKeyX[b + anchor];
            int seatFacing = MonkeyKeyFacing[b + anchor];
            bool hold = end < seatT - 0.001f;
            int head = start > 0.0001f ? 3 : 2; // follaje, (salida) y llegada
            int src = hold ? anchor : anchor + 1;
            int keep = count - src;
            int shift = head - src;
            if (shift > 0)
            {
                for (int j = count - 1; j >= src; j--)
                {
                    MonkeyKeyT[b + j + shift] = MonkeyKeyT[b + j];
                    MonkeyKeyX[b + j + shift] = MonkeyKeyX[b + j];
                    MonkeyKeyFacing[b + j + shift] = MonkeyKeyFacing[b + j];
                }
            }
            else if (shift < 0)
            {
                for (int j = src; j < count; j++)
                {
                    MonkeyKeyT[b + j + shift] = MonkeyKeyT[b + j];
                    MonkeyKeyX[b + j + shift] = MonkeyKeyX[b + j];
                    MonkeyKeyFacing[b + j + shift] = MonkeyKeyFacing[b + j];
                }
            }

            MonkeyKeyT[b] = 0f;
            MonkeyKeyX[b] = from;
            MonkeyKeyFacing[b] = travelFacing;
            if (head == 3)
            {
                MonkeyKeyT[b + 1] = start;
                MonkeyKeyX[b + 1] = from;
                MonkeyKeyFacing[b + 1] = travelFacing;
            }

            MonkeyKeyT[b + head - 1] = end; // su primer sitio a la vista
            MonkeyKeyX[b + head - 1] = seatX;
            MonkeyKeyFacing[b + head - 1] = seatFacing;
            MonkeyKeyCount[g] = head + keep;
        }

        private void AddKey(int m, float t, float x, int facing)
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
                    MonkeyKeyFacing[b + count - 1] = facing;
                    return;
                }
            }

            if (count >= MaxKeys)
            {
                return;
            }

            MonkeyKeyT[b + count] = t;
            MonkeyKeyX[b + count] = x;
            MonkeyKeyFacing[b + count] = facing;
            MonkeyKeyCount[m] = count + 1;
        }

        private static bool Before(float a, float b, bool descending, bool outsideIn, float width)
        {
            if (outsideIn)
            {
                float ea = Math.Min(a, width - a);
                float eb = Math.Min(b, width - b);
                if (Math.Abs(ea - eb) > 0.5f)
                {
                    return ea < eb;
                }
            }

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
