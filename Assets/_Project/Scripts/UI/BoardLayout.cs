#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Presentation;
using AF.Presentation.Mechanics;

namespace AF.UI
{
    /// <summary>Resultado de <see cref="BoardLayout.Compute"/> (dp del lienzo; y medida desde arriba).</summary>
    /// <remarks>
    /// Convención de dp de AF.UI (revisión de T52, U1): las alturas van en dp del lienzo medidos desde arriba, así que crecen
    /// hacia abajo. «pie + n» es n dp bajo la línea de pie de la fila (<see cref="RowFootY"/>, <see cref="FootY"/>) y
    /// «pie − n», n dp por encima. Así, la repisa que dibuja GameScreen tiene la cara superior en pie − 3, el canto inferior de
    /// la madera en pie + 17 y el final de su sombra en pie + 23 (<c>GameScreen.ShelfShadowBelowFootDp</c>); el bloque la cuenta
    /// hasta pie + 15 (<see cref="BoardLayout.ShelfDrop"/>). Las piezas de las mecánicas de AF.Presentation miden al
    /// revés, hacia arriba desde el centro del pie (p. ej. <c>RemolinoOverlay.PairMarkRect</c>); al pasarlas al lienzo cambia
    /// el signo, y en los comentarios de AF.UI se escriben siempre con esta convención.
    /// </remarks>
    public sealed class BoardLayoutResult
    {
        internal BoardLayoutResult(int count, int rows)
        {
            Rows = rows;
            PerRow = new int[rows];
            RowFootY = new float[rows];
            CenterX = new float[count];
            FootY = new float[count];
            RowOf = new int[count];
        }

        /// <summary>Filas (1, 2 o 3).</summary>
        public int Rows { get; }

        /// <summary>Cuias por fila, de arriba abajo.</summary>
        public int[] PerRow { get; }

        /// <summary>Línea de pie de cada fila (la cara superior de su repisa está en pie − 3, guía visual v2, 4.5).</summary>
        public float[] RowFootY { get; }

        /// <summary>Ancho de la ranura (área táctil, 02 §3.3).</summary>
        public float SlotWidth { get; internal set; }

        /// <summary>Ancho de la cuia.</summary>
        public float CuiaWidth { get; internal set; }

        /// <summary>Alto de la cuia más alta.</summary>
        public float CuiaHeight { get; internal set; }

        /// <summary>Centro horizontal de cada cuia.</summary>
        public float[] CenterX { get; }

        /// <summary>Pie de cada cuia, medido desde arriba del lienzo (filas alineadas por el pie, 07 §4.5).</summary>
        public float[] FootY { get; }

        /// <summary>Fila de cada cuia.</summary>
        public int[] RowOf { get; }

        /// <summary>Borde superior del bloque: la cuia de la fila 1, elevada (guía 3.2, <c>blockTop</c>).</summary>
        public float BlockTop { get; internal set; }

        /// <summary>Borde inferior del bloque: el final de la repisa de la última fila (pie + 15).</summary>
        public float BlockBottom { get; internal set; }

        /// <summary>Panel del tablero con su marco (guía 3.2): x, y desde arriba, ancho y alto.</summary>
        public float PanelX { get; internal set; }

        /// <summary>Borde superior del panel.</summary>
        public float PanelY { get; internal set; }

        /// <summary>Ancho del panel.</summary>
        public float PanelWidth { get; internal set; }

        /// <summary>Alto del panel.</summary>
        public float PanelHeight { get; internal set; }

        /// <summary>Borde inferior del panel.</summary>
        public float PanelBottom => PanelY + PanelHeight;

        /// <summary>
        /// Borde izquierdo de lo que se dibuja en el tablero, contando el lado más justo (el layout es simétrico): el vidrio de
        /// las cuias y, con la Tinaja, su vasija (más ancha que el vidrio) y el poste del jirau de su cuia. El marco del panel se
        /// adelgaza con él (<c>GameScreen</c>, guía 3.2: ≥ 8 dp entre lo que se dibuja y el marco). Sin tinaja, el borde del
        /// vidrio de la cuia de más a la izquierda.
        /// </summary>
        public float ContentLeft { get; internal set; }

        /// <summary>
        /// Con 3 filas, el tablero deja libre sobre el panel la franja de la rama de la tropa (D-046, punto 3):
        /// <see cref="PanelY"/> ≥ <c>boardTop</c> + <see cref="BoardLayout.TroopBandDp"/>, con el suelo
        /// <see cref="BoardLayout.ThreeRowFloorDropDp"/> más abajo. Falso con 1 o 2 filas y con 3 filas cuando la franja no cabe
        /// sin bajar la cuia de <see cref="BoardLayout.MinCuiaWidth"/>: entonces el layout es el de antes de D-046 (el panel puede
        /// recortarse bajo el HUD, D-028) y la intro vuelve a la tropa lateral o al dosel (D-021).
        /// </summary>
        public bool TroopBand { get; internal set; }
    }

    /// <summary>
    /// Layout del tablero (GDD 02 §5; 07 §4.1 y §7.4; guía visual v2, 3.2; D-006, punto 4, como Magic Sort: propuesta §3.3 y
    /// ficha §2.1). Filas: 1 con T ≤ 5; 2 con T = 6-10 (⌈T/2⌉ arriba); con T = 11-12, 2 filas (6/5 y 6/6) o 3 (reparto
    /// ⌈restantes/filas⌉ de arriba abajo: 4/4/3, 4/4/4), la que dé la cuia más ancha, a igualdad 2 [P]; 3 con T = 13-16
    /// (5/4/4, 5/5/4, 5/5/5 y 6/5/5) en cualquier lienzo: con T + 2 (Cuia extra y Cuia de rescate) por encima de
    /// <see cref="MaxTwoRowWithAdded"/>, 2 filas dejarían una de 8 (P12 de D-006, opción c, D-030). El número de filas se
    /// decide al cargar el nivel con las T cuias generadas y no
    /// cambia a mitad de nivel: Cuia extra, Cuia de rescate o Balsa van a la derecha de la fila más corta (a igualdad, la
    /// de más abajo [P]), ninguna cuia cambia de fila y solo se recalcula el ancho. Ranura = (ancho − 24) / cuias de la
    /// fila más larga; cuia_w = clamp(0,70 × ranura, 28, tope(T)), con tope(T) = 64 / 56 / 48 dp para T ≤ 3 / 4-5 / ≥ 6
    /// (D-004, T = cuias en pantalla); alto = (1,60 + capacidad) u (5,60 u con 4 dosis; u = cuia_w / 2; D-006); paso de
    /// fila = alto + 16 (elevación) + 24 (hueco: 40 dp con la repisa de D-003). El tablero se centra en horizontal a 480 dp
    /// como máximo (tabletas; la cuia tampoco supera tope(T)) y, desde D-003, también en vertical: el bloque (elevación de
    /// la fila 1, filas, huecos y la repisa de 15 dp bajo la última fila) queda centrado entre <c>boardTop</c> y
    /// <c>boardBottom</c>, sin espacio muerto abajo. El bloque no supera <c>(boardBottom − boardTop) − 27</c> dp
    /// (<see cref="MinBlockSlack"/> [P]): así la fila 1 elevada queda ≥ 8 dp bajo el canto interior del marco de 13,5 dp
    /// aunque el panel se recorte bajo el HUD (guía 3.2); si no cabe, las cuias se reducen hasta el mínimo de 28 dp.
    /// La Tinaja y su cuia (M13; D-019; 02 §5; 07 §4.5: «su cuia, en la ranura contigua de la misma fila») van siempre en la
    /// misma fila: si el reparto de arriba las parte, se mueve el corte (<see cref="Compute(int, int, float, float, float, float, int)"/>);
    /// y si la vasija (o el poste de su cuia) cae en una punta de la fila y se saldría de lo que ocupa el vidrio en una fila
    /// llena, esa fila se aprieta hacia dentro (<see cref="Compute(int, int, float, float, float, float, int, int)"/>).
    /// Alrededor del bloque va el panel del tablero, <see cref="PanelMargin"/> (22 dp) por abajo y por arriba en todos los
    /// tableros, y 8 dp desde los bordes del ancho útil. No hay techo del vertido ni hueco reservado para él (D-006): el
    /// origen se dibuja delante de todo mientras vierte.
    /// <para>Con 3 filas (D-046, punto 3; matiza D-006 y la excepción de D-028): arriba se reserva la franja de la rama de la
    /// tropa, <see cref="TroopBandDp"/> entre <c>boardTop</c> y el panel, y el suelo del tablero baja
    /// <see cref="ThreeRowFloorDropDp"/>, encogiendo las cuias lo justo (41,61 dp a 360 × 780; Q-T6T-3 y Q-T6T-4, decididas por el
    /// dueño el 2026-10-24). Ni la fila 1 ni su cielo entran en el
    /// HUD. Si así la cuia bajaría de <see cref="MinCuiaWidth"/> (que con la
    /// Cuia alta es también el mínimo de u = 14 dp de 07 §4.5), el layout de 3 filas es el de antes
    /// (<see cref="BoardLayoutResult.TroopBand"/> falso). Con 1 o 2 filas nada cambia.</para>
    /// </summary>
    public static class BoardLayout
    {
        /// <summary>Margen lateral total (02 §5).</summary>
        public const float SideMargins = 24f;

        /// <summary>Ancho máximo del tablero (02 §5, tabletas).</summary>
        public const float MaxBoardWidth = 480f;

        /// <summary>Ancho mínimo de la cuia (02 §5).</summary>
        public const float MinCuiaWidth = 28f;

        /// <summary>Tope del ancho con T ≤ 3 cuias en pantalla (D-004; 02 §5).</summary>
        public const float MaxCuiaWidthUpTo3 = 64f;

        /// <summary>Tope del ancho con T = 4-5 cuias en pantalla (D-004; 02 §5).</summary>
        public const float MaxCuiaWidthUpTo5 = 56f;

        /// <summary>Tope del ancho con T ≥ 6 cuias en pantalla (02 §5, sin cambios con D-004).</summary>
        public const float MaxCuiaWidthFrom6 = 48f;

        /// <summary>Elevación de la selección (02 §4.1; D-006: se mantienen 16 dp).</summary>
        public const float Lift = 16f;

        /// <summary>Hueco entre filas (02 §5).</summary>
        public const float RowGap = 24f;

        /// <summary>La repisa termina 15 dp bajo la línea de pie (guía 3.2 y 4.5).</summary>
        public const float ShelfDrop = 15f;

        /// <summary>
        /// Margen del panel sobre y bajo el bloque: 22 dp (revisión visual del 2026-10-07; la guía 3.2 decía 14). Con el
        /// marco tallado de 13,5 dp (1,5 de contorno + 12 de madera), la cuia elevada de la fila 1 queda ≥ 8 dp por debajo
        /// del canto interior del marco en lugar de tocarlo.
        /// </summary>
        public const float PanelMargin = 22f;

        /// <summary>Margen del panel desde el borde del ancho útil (guía 3.2).</summary>
        public const float PanelSide = 8f;

        /// <summary>
        /// Lo que la vasija de la Tinaja sale de cada lado del vidrio de su ranura (u): medio ancho del sprite del cuerpo
        /// (<see cref="ProceduralArt.TinajaBodyWidthU"/>, 1,35 u) menos el medio ancho de la cuia (1 u) = 0,35 u (TJ2).
        /// </summary>
        public const float TinajaSideExtraU = (0.5f * ProceduralArt.TinajaBodyWidthU) - (0.5f * CuiaGeometry.WidthU);

        /// <summary>
        /// Lo que el poste del jirau (y el jirau bajo él) sale del vidrio de su cuia, del lado contrario a la tinaja (u):
        /// <see cref="TinajaOverlay.JirauCuiaExtentU"/> (1,33 u) menos el medio ancho de la cuia = 0,33 u (TJ2).
        /// </summary>
        public const float TinajaPostExtraU = TinajaOverlay.JirauCuiaExtentU - (0.5f * CuiaGeometry.WidthU);

        /// <summary>
        /// Lo que el jirau se queda, como mínimo, dentro de lo que ocupa el vidrio por el lado del poste (dp) [P]: así no sale de
        /// la punta de la repisa (02 §5; 07 §4.3: el jirau va bajo las dos ranuras, sobre la repisa). Por el lado de la tinaja ya
        /// queda dentro de la vasija (<see cref="TinajaOverlay.JirauTinajaExtentU"/> &lt; medio ancho del cuerpo).
        /// </summary>
        public const float TinajaJirauInsetDp = 2f;

        /// <summary>
        /// Distancia mínima (dp) desde el borde del panel hasta lo que se dibuja con la Tinaja cuando sobra sitio: el marco
        /// tallado más ancho (13,5 dp) + 8 dp (guía 3.2) [P]. Con menos sitio, el límite es el vidrio de una fila llena.
        /// </summary>
        public const float TinajaContentFromPanel = 13.5f + 8f;

        /// <summary>
        /// Holgura mínima (dp) entre el bloque y la zona del tablero, repartida arriba y abajo [P] (D-006): 2 × 13,5. Con el
        /// panel recortado en <c>boardTop − 8</c>, el canto interior del marco queda en <c>boardTop + 5,5</c> y la fila 1
        /// elevada, ≥ 8 dp por debajo.
        /// </summary>
        public const float MinBlockSlack = 27f;

        /// <summary>
        /// Cuias añadidas a mitad de nivel con las que se dimensionan las filas al cargar: Cuia extra y Cuia de rescate (P12 de
        /// D-006, «T + 2»).
        /// </summary>
        public const int AddedCuiasForRows = 2;

        /// <summary>
        /// Máximo de cuias en pantalla (generadas + <see cref="AddedCuiasForRows"/>) que aún caben en 2 filas: con más, 2 filas
        /// dejarían una de 8 cuias (ranura de 42 dp en 16:9, &lt; 48 dp de 02 §3.3), así que el nivel se carga en 3 (P12 de
        /// D-006, opción c, decidida el 2026-10-19 en D-030: T generadas ≥ 13).
        /// </summary>
        public const int MaxTwoRowWithAdded = 14;

        /// <summary>
        /// Con 3 filas, cuánto baja el suelo del tablero (dp) [P] (D-046, punto 3; Q-T6T-3, decidida por el dueño el 2026-10-24:
        /// «Bajar y mover avisos»): de <c>boardBottom</c> (bandeja − 40) a bandeja − 20, con el panel a (bandeja − 20) − 13,5 + 22 =
        /// bandeja − 11,5 cuando las cuias encogen por el alto. Las burbujas «Te faltan {n}» y el aviso del tablero que ya no caben
        /// entre el panel y la bandeja van a la franja de la tropa, sobre el panel (<c>GameScreen</c> y <c>BoosterTray</c>).
        /// </summary>
        public const float ThreeRowFloorDropDp = 20f;

        /// <summary>
        /// Con 3 filas, franja libre entre <c>boardTop</c> (pie del HUD) y el panel para la rama de la tropa (dp) [P] (D-046,
        /// punto 3; Q-T6T-4, decidida por el dueño el 2026-10-24: «Franja de 56 dp»). Con la franja justa de 44 dp de D-010
        /// (<c>IntroChoreography.BranchMinBandDp</c>), el tamaño de 07 §6.4 dejaba la tropa rozar la fila del HUD.
        /// </summary>
        public const float TroopBandDp = 56f;

        /// <summary>
        /// Solo tests: sin la franja de la tropa con 3 filas, para medir en lienzos altos el respaldo de antes de D-046 (la tropa
        /// lateral de D-021). Nunca se cambia fuera de los tests.
        /// </summary>
        internal static bool TroopBandOffForTests { get; set; }

        /// <summary>
        /// Tope del ancho de la cuia para T = <paramref name="count"/> cuias del tablero en pantalla, incluidas Cuia extra y
        /// Cuia de rescate (02 R1): 64 dp con T ≤ 3, 56 con T = 4-5 y 48 con T ≥ 6 (D-004; 02 §5). Si T cambia a mitad de
        /// nivel, el ancho se recalcula con el T nuevo (02 §4.1, re-layout de 300 ms); las filas, no (D-006).
        /// </summary>
        public static float MaxCuiaWidthFor(int count)
        {
            return count <= 3 ? MaxCuiaWidthUpTo3 : (count <= 5 ? MaxCuiaWidthUpTo5 : MaxCuiaWidthFrom6);
        }

        /// <summary>
        /// Filas para <paramref name="generatedCount"/> cuias generadas (D-006): 1 con T ≤ 5, 2 con 6-10, 2 o 3 con 11-12 (la
        /// que dé la cuia más ancha con este lienzo; a igualdad, 2 [P]) y 3 con 13-16 en cualquier lienzo, porque T + 2 pasa de
        /// <see cref="MaxTwoRowWithAdded"/> (P12 de D-006, opción c; D-030). Las filas no cambian a mitad de nivel (D-006,
        /// punto 4).
        /// </summary>
        public static int RowsFor(int generatedCount, float heightU, float canvasWidth, float boardTop, float boardBottom)
        {
            if (generatedCount <= 5)
            {
                return 1;
            }

            if (generatedCount <= 10)
            {
                return 2;
            }

            // P12 de D-006 (c): con Cuia extra y Cuia de rescate, 2 filas dejarían una de 8; se carga en 3 (T ≥ 13).
            if (generatedCount + AddedCuiasForRows > MaxTwoRowWithAdded)
            {
                return 3;
            }

            float cap = MaxCuiaWidthFor(generatedCount);
            float two = CuiaWidthFor(2, (generatedCount + 1) / 2, cap, heightU, canvasWidth, boardTop, boardBottom);
            // Con la zona de antes de D-046 (sin la franja de la tropa): la franja encoge las cuias de 3 filas, pero no cambia
            // cuántas filas lleva un nivel (D-046, punto 3: «con 3 filas las cuias encogen lo justo»; Q-T6T-2).
            float three = CuiaWidthFor(3, (generatedCount + 2) / 3, cap, heightU, canvasWidth, boardTop, boardBottom);
            return three > two + 0.01f ? 3 : 2;
        }

        /// <summary>
        /// Calcula el layout de <paramref name="count"/> cuias de <paramref name="heightU"/> u de alto en un lienzo de
        /// <paramref name="canvasWidth"/> dp, con el bloque centrado entre <paramref name="boardTop"/> y
        /// <paramref name="boardBottom"/> (medidas desde arriba), y las filas de las cuias generadas al cargar el nivel
        /// (<see cref="Compute(int, int, float, float, float, float)"/> con <c>generatedCount = count</c>).
        /// </summary>
        public static BoardLayoutResult Compute(int count, float heightU, float canvasWidth, float boardTop, float boardBottom)
        {
            return Compute(count, count, heightU, canvasWidth, boardTop, boardBottom);
        }

        /// <summary>
        /// Calcula el layout de <paramref name="count"/> cuias en pantalla de un nivel que se generó con
        /// <paramref name="generatedCount"/> (D-006): las filas son las de las cuias generadas (índices
        /// <c>0 … generatedCount − 1</c>, fila a fila) y cada cuia de más (Cuia extra, Cuia de rescate, Balsa), en orden de
        /// índice, va a la derecha de la fila más corta en ese momento, a igualdad la de más abajo [P]. Así
        /// <see cref="BoardLayoutResult.RowOf"/> de una cuia no cambia al añadir otra (prueba del 2026-10-09, T-C) y, dentro
        /// de cada fila, <see cref="BoardLayoutResult.CenterX"/> crece con el índice; cada fila se centra con su número final
        /// de cuias. El ancho usa la fila más larga y tope(<paramref name="count"/>). La línea de pie máxima es
        /// <c>boardBottom − 15</c> (<see cref="ShelfDrop"/>) y el bloque deja al menos <see cref="MinBlockSlack"/> de holgura
        /// en la zona del tablero.
        /// </summary>
        public static BoardLayoutResult Compute(int count, int generatedCount, float heightU, float canvasWidth, float boardTop, float boardBottom)
        {
            return Compute(count, generatedCount, heightU, canvasWidth, boardTop, boardBottom, -1);
        }

        /// <summary>
        /// Como <see cref="Compute(int, int, float, float, float, float)"/>, con la pareja de la Tinaja (M13; 02 §5; 07 §4.5) en
        /// las cuias generadas <paramref name="pairStart"/> y <paramref name="pairStart"/> + 1 (la tinaja y su cuia, contiguas;
        /// <see cref="TinajaPairStart"/>), o −1 sin pareja. Las dos van siempre en la misma fila: si el reparto de 02 §5 (D-006)
        /// pone el corte entre ellas, se usa el reparto en el mismo número de filas que no las separa con la fila más larga lo
        /// más corta posible (la cuia más ancha) y, a igualdad, el más parecido al de 02 §5 y el que deja más cuias arriba [P].
        /// El número de filas no cambia (<see cref="RowsFor"/>) y ninguna cuia añadida se mete entre las dos (van a la derecha
        /// de una fila).
        /// </summary>
        public static BoardLayoutResult Compute(int count, int generatedCount, float heightU, float canvasWidth, float boardTop, float boardBottom, int pairStart)
        {
            return Compute(count, generatedCount, heightU, canvasWidth, boardTop, boardBottom, pairStart, -1);
        }

        /// <summary>
        /// Como <see cref="Compute(int, int, float, float, float, float, int)"/>, sabiendo cuál es la tinaja
        /// (<paramref name="tinajaIndex"/>: <paramref name="pairStart"/> o <paramref name="pairStart"/> + 1; −1 si no se sabe).
        /// La vasija es más ancha que el vidrio (<see cref="TinajaSideExtraU"/>) y el poste de su cuia sale de su ranura del
        /// lado contrario (<see cref="TinajaPostExtraU"/>): si caen en una punta de su fila y se saldrían de lo que ocupa el
        /// vidrio en una fila llena (o, con sitio de sobra, de <see cref="TinajaContentFromPanel"/> desde el panel), esa fila se
        /// aprieta hacia dentro desde esa punta (las demás cuias de la fila, igual de separadas entre sí; la otra punta no se
        /// mueve) [P]. Así la vasija, el poste y el jirau quedan sobre la repisa y a ≥ 8 dp del marco como el vidrio
        /// (<see cref="BoardLayoutResult.ContentLeft"/>), sin cambiar filas ni el ancho de la cuia. Solo hay que pasar la tinaja
        /// cuando esas piezas se dibujan (el arte provisional o el del artista; <c>GameScreen.LayoutTinajaIndex</c>): sin ellas,
        /// con −1, la fila conserva el reparto uniforme de 02 §5 y 07 §4.1, y el corte entre filas se mueve igual con
        /// <paramref name="pairStart"/>.
        /// </summary>
        public static BoardLayoutResult Compute(int count, int generatedCount, float heightU, float canvasWidth, float boardTop, float boardBottom, int pairStart, int tinajaIndex)
        {
            if (count < 1 || count > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Entre 1 y 16 cuias (02 R1).");
            }

            if (generatedCount < 1 || generatedCount > count)
            {
                throw new ArgumentOutOfRangeException(nameof(generatedCount), generatedCount, "Entre 1 y las cuias en pantalla.");
            }

            int rows = RowsFor(generatedCount, heightU, canvasWidth, boardTop, boardBottom);
            var result = new BoardLayoutResult(count, rows);

            // Asignación en dos pasos (D-006; prueba del 2026-10-09, T-C). Mientras se reparte, CenterX guarda la columna de
            // cada cuia dentro de su fila (sin otro array); al final se convierte en el centro con el PerRow definitivo.
            // 1) Las generadas, fila a fila como al cargar el nivel: ⌈restantes/filas⌉ de arriba abajo.
            int remaining = generatedCount;
            for (int r = 0; r < rows; r++)
            {
                int left = rows - r;
                int n = (remaining + left - 1) / left;
                result.PerRow[r] = n;
                remaining -= n;
            }

            // La Tinaja y su cuia, en la misma fila (02 §5; 07 §4.5): si el corte cae entre las dos, otro reparto.
            if (pairStart >= 0 && pairStart + 1 < generatedCount && SplitsPair(result.PerRow, rows, pairStart))
            {
                KeepPairTogether(result.PerRow, rows, generatedCount, pairStart);
            }

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                int n = result.PerRow[r];
                for (int j = 0; j < n; j++)
                {
                    result.RowOf[index] = r;
                    result.CenterX[index] = j;
                    index++;
                }
            }

            // 2) Cada cuia de más (Cuia extra, Cuia de rescate, Balsa), en orden, a la derecha de la fila más corta en ese
            // momento; a igualdad, la de más abajo [P]. Ninguna cuia cambia de fila al añadir otra.
            for (int extra = generatedCount; extra < count; extra++)
            {
                int shortest = rows - 1;
                for (int r = rows - 2; r >= 0; r--)
                {
                    if (result.PerRow[r] < result.PerRow[shortest])
                    {
                        shortest = r;
                    }
                }

                result.RowOf[extra] = shortest;
                result.CenterX[extra] = result.PerRow[shortest];
                result.PerRow[shortest]++;
            }

            int maxPerRow = 0;
            for (int r = 0; r < rows; r++)
            {
                maxPerRow = Math.Max(maxPerRow, result.PerRow[r]);
            }

            float width = Math.Min(canvasWidth, MaxBoardWidth);
            float slot = (width - SideMargins) / maxPerRow;
            // Con 3 filas, la zona del bloque deja arriba la franja de la tropa y baja el suelo, si cabe (D-046, punto 3).
            bool band = false;
            float zoneTop = boardTop;
            float zoneBottom = boardBottom;
            float cuiaW = rows == 3
                ? ThreeRowCuiaWidth(maxPerRow, MaxCuiaWidthFor(count), heightU, canvasWidth, boardTop, boardBottom, out band, out zoneTop, out zoneBottom)
                : CuiaWidthFor(rows, maxPerRow, MaxCuiaWidthFor(count), heightU, canvasWidth, boardTop, boardBottom);
            float blockH = BlockHeight(rows, cuiaW, heightU);
            float cuiaH = heightU * (cuiaW / 2f);
            float block = Lift + blockH + ShelfDrop; // B de la guía 3.2
            float slack = (zoneBottom - zoneTop) - block;
            float blockTop = zoneTop + (slack / 2f); // centrado (D-003): fin del anclaje inferior
            result.TroopBand = band;
            result.SlotWidth = slot;
            result.CuiaWidth = cuiaW;
            result.CuiaHeight = cuiaH;
            result.BlockTop = blockTop;
            result.BlockBottom = blockTop + block;

            for (int r = 0; r < rows; r++)
            {
                result.RowFootY[r] = blockTop + Lift + (r * (cuiaH + Lift + RowGap)) + cuiaH;
            }

            // La Tinaja (TJ2): lo que su vasija y el poste de su cuia salen del vidrio y, si se saldrían por una punta de su fila,
            // la fila apretada hacia dentro (primer centro y paso). CenterX aún guarda la columna de cada cuia.
            // Solo con la pareja entera (tinajaIndex dentro de ella, las dos en la misma fila); si no, como sin tinaja.
            bool pair = tinajaIndex >= 0 && pairStart >= 0 && pairStart + 1 < generatedCount && (tinajaIndex == pairStart || tinajaIndex == pairStart + 1)
                && result.RowOf[pairStart] == result.RowOf[pairStart + 1];
            int partner = pair ? (tinajaIndex == pairStart ? pairStart + 1 : pairStart) : -1;
            float u = cuiaW / 2f;
            float extraT = pair ? TinajaSideExtraU * u : 0f;
            float extraP = pair ? (TinajaPostExtraU * u) + TinajaJirauInsetDp : 0f;
            int tRow = -1;
            float tFirst = 0f;
            float tPitch = slot;
            if (pair)
            {
                tRow = result.RowOf[tinajaIndex];
                int n = result.PerRow[tRow];
                int colT = (int)result.CenterX[tinajaIndex];
                int colP = (int)result.CenterX[partner];
                float extraL = colT == 0 ? extraT : (colP == 0 ? extraP : 0f);
                float extraR = colT == n - 1 ? extraT : (colP == n - 1 ? extraP : 0f);
                float usableLeft = (canvasWidth - width) / 2f;
                float fullGlassLeft = ((canvasWidth - (maxPerRow * slot)) / 2f) + (0.5f * slot) - (0.5f * cuiaW);
                float limitL = Math.Min(fullGlassLeft, usableLeft + PanelSide + TinajaContentFromPanel);
                float x0 = (canvasWidth - (n * slot)) / 2f;
                float first = x0 + (0.5f * slot);
                float last = x0 + ((n - 0.5f) * slot);
                float overL = Math.Max(0f, limitL - (first - (0.5f * cuiaW) - extraL));
                float overR = Math.Max(0f, (last + (0.5f * cuiaW) + extraR) - (canvasWidth - limitL));
                tFirst = first + overL;
                tPitch = n > 1 ? Math.Max(cuiaW, ((last - overR) - tFirst) / (n - 1)) : slot;
            }

            // Cada fila, centrada con su número final de cuias (x0 con el PerRow definitivo); la columna pasa a centro. La de la
            // tinaja, apretada si hace falta (arriba).
            for (int i = 0; i < count; i++)
            {
                int r = result.RowOf[i];
                float x0 = (canvasWidth - (result.PerRow[r] * slot)) / 2f;
                result.CenterX[i] = r == tRow ? tFirst + (result.CenterX[i] * tPitch) : x0 + ((result.CenterX[i] + 0.5f) * slot);
                result.FootY[i] = result.RowFootY[r];
            }

            // Lo que se dibuja, por el lado más justo: el vidrio y, con la Tinaja, su vasija y el poste de su cuia.
            float minLeft = float.MaxValue;
            float maxRight = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float left = result.CenterX[i] - (0.5f * cuiaW);
                float right = result.CenterX[i] + (0.5f * cuiaW);
                if (pair && i == tinajaIndex)
                {
                    left -= extraT;
                    right += extraT;
                }
                else if (i == partner)
                {
                    if (partner < tinajaIndex)
                    {
                        left -= extraP;
                    }
                    else
                    {
                        right += extraP;
                    }
                }

                minLeft = Math.Min(minLeft, left);
                maxRight = Math.Max(maxRight, right);
            }

            result.ContentLeft = Math.Min(minLeft, canvasWidth - maxRight);

            // Panel (guía 3.2): 8 dp desde el ancho útil (≤ 480) y 22 dp bajo y sobre el bloque en todos los tableros (D-006
            // retira el margen de 36 dp de una fila, que solo existía por el techo del vertido); si sube por encima de
            // boardTop − 8 se recorta (el marco pasa bajo el HUD). Nunca se encoge una cuia por el marco. Con la franja de la
            // tropa (D-046), nunca por encima de boardTop + TroopBandDp (el máximo solo absorbe el redondeo del ajuste).
            float panelX = ((canvasWidth - width) / 2f) + PanelSide;
            float panelTop = Math.Max(blockTop - PanelMargin, band ? boardTop + TroopBandDp : boardTop - 8f);
            float panelBottom = result.BlockBottom + PanelMargin;
            result.PanelX = panelX;
            result.PanelY = panelTop;
            result.PanelWidth = canvasWidth - (2f * panelX);
            result.PanelHeight = panelBottom - panelTop;
            return result;
        }

        /// <summary>
        /// Primera cuia de la pareja de la Tinaja en <paramref name="board"/> (la de índice menor de las dos: la tinaja y su cuia
        /// son contiguas, M13, b.2 regla 1), o −1 si el tablero no tiene tinaja con su cuia colgada. Se mira en el tablero
        /// inicial del nivel: la suelta quita el tipo a su cuia, pero las filas no cambian a mitad de nivel (D-006). Sin
        /// asignaciones.
        /// </summary>
        public static int TinajaPairStart(BoardState board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].Mechanic != MechanicType.Tinaja)
                {
                    continue;
                }

                if (i + 1 < board.Count && board[i + 1].Mechanic == MechanicType.Cucharon)
                {
                    return i;
                }

                if (i > 0 && board[i - 1].Mechanic == MechanicType.Cucharon)
                {
                    return i - 1;
                }
            }

            return -1;
        }

        // El corte entre dos filas cae entre la cuia pairStart y la siguiente.
        private static bool SplitsPair(int[] perRow, int rows, int pairStart)
        {
            int boundary = 0;
            for (int r = 0; r < rows - 1; r++)
            {
                boundary += perRow[r];
                if (boundary == pairStart + 1)
                {
                    return true;
                }
            }

            return false;
        }

        // Reparto de generated cuias en rows filas (2 o 3, ≥ 1 por fila) sin corte entre pairStart y pairStart + 1: la fila más
        // larga, lo más corta posible; a igualdad, lo más cerca del reparto de 02 §5 (perRow al entrar) y, después, con más
        // cuias arriba [P]. Como mucho 3 filas y 16 cuias: unas decenas de repartos. Escribe el elegido en perRow.
        private static void KeepPairTogether(int[] perRow, int rows, int generated, int pairStart)
        {
            int c0 = perRow[0];
            int c1 = perRow[1];
            int c2 = rows > 2 ? perRow[2] : 0;
            int cut = pairStart + 1;
            int best0 = -1;
            int best1 = 0;
            int bestMax = int.MaxValue;
            int bestDist = int.MaxValue;
            for (int n0 = 1; n0 < generated; n0++)
            {
                int n1From = rows == 3 ? 1 : generated - n0;
                int n1To = rows == 3 ? generated - n0 - 1 : generated - n0;
                for (int n1 = n1From; n1 <= n1To; n1++)
                {
                    int n2 = rows == 3 ? generated - n0 - n1 : 0;
                    if (n0 == cut || (rows == 3 && n0 + n1 == cut))
                    {
                        continue;
                    }

                    int max = Math.Max(n0, Math.Max(n1, n2));
                    int dist = Math.Abs(n0 - c0) + Math.Abs(n1 - c1) + Math.Abs(n2 - c2);
                    bool better = max < bestMax
                        || (max == bestMax && dist < bestDist)
                        || (max == bestMax && dist == bestDist && (n0 > best0 || (n0 == best0 && n1 > best1)));
                    if (better)
                    {
                        best0 = n0;
                        best1 = n1;
                        bestMax = max;
                        bestDist = dist;
                    }
                }
            }

            if (best0 < 0)
            {
                return; // sin otro reparto (no pasa con la pareja dentro de las generadas y 2 o 3 filas): se queda el de 02 §5
            }

            perRow[0] = best0;
            perRow[1] = best1;
            if (rows == 3)
            {
                perRow[2] = generated - best0 - best1;
            }
        }

        // Ancho de la cuia con 3 filas de como mucho perRow cuias (D-046, punto 3): en la zona con la franja de la tropa arriba
        // (el panel, 22 dp sobre el bloque y su media holgura de 13,5, en boardTop + TroopBandDp) y el suelo bajado
        // ThreeRowFloorDropDp; si ahí el bloque no cabe ni con la cuia de MinCuiaWidth, la zona y el ancho de antes (band falso).
        private static float ThreeRowCuiaWidth(int perRow, float cap, float heightU, float canvasWidth, float boardTop, float boardBottom, out bool band, out float zoneTop, out float zoneBottom)
        {
            if (!TroopBandOffForTests)
            {
                float top = boardTop + TroopBandDp + PanelMargin - (0.5f * MinBlockSlack);
                float bottom = boardBottom + ThreeRowFloorDropDp;
                float cuiaW = CuiaWidthFor(3, perRow, cap, heightU, canvasWidth, top, bottom);
                float available = (bottom - top) - MinBlockSlack - Lift - ShelfDrop;
                if (BlockHeight(3, cuiaW, heightU) <= available + 0.01f)
                {
                    band = true;
                    zoneTop = top;
                    zoneBottom = bottom;
                    return cuiaW;
                }
            }

            band = false;
            zoneTop = boardTop;
            zoneBottom = boardBottom;
            return CuiaWidthFor(3, perRow, cap, heightU, canvasWidth, boardTop, boardBottom);
        }

        // Ancho de la cuia con rows filas de como mucho perRow cuias: clamp(0,70 × ranura, 28, tope) y, si el bloque no
        // cabe con la holgura de MinBlockSlack, el ancho que lo hace caber con sus huecos fijos, sin bajar de 28 dp.
        private static float CuiaWidthFor(int rows, int perRow, float cap, float heightU, float canvasWidth, float boardTop, float boardBottom)
        {
            float width = Math.Min(canvasWidth, MaxBoardWidth);
            float slot = (width - SideMargins) / perRow;
            float cuiaW = Clamp(0.70f * slot, MinCuiaWidth, cap);
            float available = (boardBottom - boardTop) - MinBlockSlack - Lift - ShelfDrop;
            if (BlockHeight(rows, cuiaW, heightU) > available && available > 0f)
            {
                float fit = 2f * (available - ((rows - 1) * (Lift + RowGap))) / (rows * heightU);
                cuiaW = Math.Max(MinCuiaWidth, Math.Min(cuiaW, fit));
            }

            return cuiaW;
        }

        private static float BlockHeight(int rows, float cuiaW, float heightU)
        {
            float cuiaH = heightU * (cuiaW / 2f);
            return (rows * cuiaH) + ((rows - 1) * (Lift + RowGap));
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
