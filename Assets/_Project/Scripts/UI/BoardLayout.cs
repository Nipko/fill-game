#nullable enable
using System;

namespace AF.UI
{
    /// <summary>Resultado de <see cref="BoardLayout.Compute"/> (dp del lienzo; y medida desde arriba).</summary>
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
    }

    /// <summary>
    /// Layout del tablero (GDD 02 §5; 07 §4.1 y §7.4; guía visual v2, 3.2; D-006, punto 4, como Magic Sort: propuesta §3.3 y
    /// ficha §2.1). Filas: 1 con T ≤ 5; 2 con T = 6-10 (⌈T/2⌉ arriba); con T = 11-14, 2 filas (hasta 7 por fila) o 3
    /// (reparto ⌈restantes/filas⌉ de arriba abajo: 4/4/3, 4/4/4, 5/4/4, 5/5/4), la que dé la cuia más ancha, a igualdad 2
    /// [P]; 3 con T = 15-16 (5/5/5 y 6/5/5). El número de filas se decide al cargar el nivel con las T cuias generadas y no
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
    /// Alrededor del bloque va el panel del tablero, <see cref="PanelMargin"/> (22 dp) por abajo y por arriba en todos los
    /// tableros, y 8 dp desde los bordes del ancho útil. No hay techo del vertido ni hueco reservado para él (D-006): el
    /// origen se dibuja delante de todo mientras vierte.
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
        /// Holgura mínima (dp) entre el bloque y la zona del tablero, repartida arriba y abajo [P] (D-006): 2 × 13,5. Con el
        /// panel recortado en <c>boardTop − 8</c>, el canto interior del marco queda en <c>boardTop + 5,5</c> y la fila 1
        /// elevada, ≥ 8 dp por debajo.
        /// </summary>
        public const float MinBlockSlack = 27f;

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
        /// Filas para <paramref name="generatedCount"/> cuias generadas (D-006): 1 con T ≤ 5, 2 con 6-10, 2 o 3 con 11-14 (la
        /// que dé la cuia más ancha con este lienzo; a igualdad, 2 [P]) y 3 con 15-16.
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

            if (generatedCount >= 15)
            {
                return 3;
            }

            float cap = MaxCuiaWidthFor(generatedCount);
            float two = CuiaWidthFor(2, (generatedCount + 1) / 2, cap, heightU, canvasWidth, boardTop, boardBottom);
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
            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                int left = rows - r;
                int n = (remaining + left - 1) / left;
                result.PerRow[r] = n;
                remaining -= n;
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
            float cuiaW = CuiaWidthFor(rows, maxPerRow, MaxCuiaWidthFor(count), heightU, canvasWidth, boardTop, boardBottom);
            float blockH = BlockHeight(rows, cuiaW, heightU);
            float cuiaH = heightU * (cuiaW / 2f);
            float block = Lift + blockH + ShelfDrop; // B de la guía 3.2
            float slack = (boardBottom - boardTop) - block;
            float blockTop = boardTop + (slack / 2f); // centrado (D-003): fin del anclaje inferior
            result.SlotWidth = slot;
            result.CuiaWidth = cuiaW;
            result.CuiaHeight = cuiaH;
            result.BlockTop = blockTop;
            result.BlockBottom = blockTop + block;

            for (int r = 0; r < rows; r++)
            {
                result.RowFootY[r] = blockTop + Lift + (r * (cuiaH + Lift + RowGap)) + cuiaH;
            }

            // Cada fila, centrada con su número final de cuias (x0 con el PerRow definitivo); la columna pasa a centro.
            for (int i = 0; i < count; i++)
            {
                int r = result.RowOf[i];
                float x0 = (canvasWidth - (result.PerRow[r] * slot)) / 2f;
                result.CenterX[i] = x0 + ((result.CenterX[i] + 0.5f) * slot);
                result.FootY[i] = result.RowFootY[r];
            }

            // Panel (guía 3.2): 8 dp desde el ancho útil (≤ 480) y 22 dp bajo y sobre el bloque en todos los tableros (D-006
            // retira el margen de 36 dp de una fila, que solo existía por el techo del vertido); si sube por encima de
            // boardTop − 8 se recorta (el marco pasa bajo el HUD). Nunca se encoge una cuia por el marco.
            float panelX = ((canvasWidth - width) / 2f) + PanelSide;
            float panelTop = Math.Max(blockTop - PanelMargin, boardTop - 8f);
            float panelBottom = result.BlockBottom + PanelMargin;
            result.PanelX = panelX;
            result.PanelY = panelTop;
            result.PanelWidth = canvasWidth - (2f * panelX);
            result.PanelHeight = panelBottom - panelTop;
            return result;
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
