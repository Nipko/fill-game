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
    /// Layout del tablero (GDD 02 §5; 07 §4.1 y §7.4; guía visual v2, 3.2): 1 fila con T ≤ 5, 2 filas con 6-14 (⌈T/2⌉
    /// arriba), 3 con 15-16; ranura = (ancho − 24) / cuias_por_fila; cuia_w = clamp(0,70 × ranura, 28, tope(T)), con
    /// tope(T) = 64 / 56 / 48 dp para T ≤ 3 / 4-5 / ≥ 6 (D-004); alto = 6 u (u = cuia_w / 2); paso de fila = alto + 16
    /// (elevación) + 24 (hueco). El tablero se centra en horizontal a 480 dp como máximo (tabletas; la cuia tampoco supera
    /// tope(T)) y, desde D-003, también en vertical: el bloque (elevación de la fila 1, filas, huecos y la
    /// repisa de 15 dp bajo la última fila) queda centrado entre <c>boardTop</c> y <c>boardBottom</c>, sin espacio muerto
    /// abajo. Si las filas no caben (16:9), las cuias se reducen hasta el mínimo de 28 dp. Alrededor del bloque va el
    /// panel del tablero, <see cref="PanelMargin"/> (22 dp) por abajo y por arriba (<see cref="PanelMarginTopOneRow"/>,
    /// 36 dp, por arriba en los tableros de una fila) y 8 dp desde los bordes del ancho útil.
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

        /// <summary>Elevación de la selección (02 §4.1).</summary>
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

        /// <summary>
        /// Margen del panel sobre el bloque en los tableros de una fila: 36 dp ≈ 22 + 13,5 del marco [P] (revisión de D-004).
        /// Con u = 32 el origen inclinado del vertido sube ≈ 34,5 dp sobre el borde de las cuias en reposo (elevación de
        /// ≈ 0,14 u, recortada por el techo del vertido, 4 dp bajo el canto interior del marco: GameScreen.PourCeilingAirDp);
        /// con este margen queda bajo ese canto en lugar de montarse sobre él o rozarlo. En dos y tres filas se mantienen
        /// 22 dp: 36 estrecharía el río del nivel 7 de 34,5 a 20,5 dp.
        /// </summary>
        public const float PanelMarginTopOneRow = 36f;

        /// <summary>Margen del panel desde el borde del ancho útil (guía 3.2).</summary>
        public const float PanelSide = 8f;

        /// <summary>
        /// Tope del ancho de la cuia para T = <paramref name="count"/> cuias del tablero en pantalla, incluidas Cuia extra y
        /// Cuia de rescate (02 R1): 64 dp con T ≤ 3, 56 con T = 4-5 y 48 con T ≥ 6 (D-004; 02 §5). Si T cambia a mitad de
        /// nivel, el layout se recalcula con el T nuevo (02 §4.1, re-layout de 300 ms).
        /// </summary>
        public static float MaxCuiaWidthFor(int count)
        {
            return count <= 3 ? MaxCuiaWidthUpTo3 : (count <= 5 ? MaxCuiaWidthUpTo5 : MaxCuiaWidthFrom6);
        }

        /// <summary>Filas para <paramref name="count"/> cuias.</summary>
        public static int RowsFor(int count)
        {
            if (count <= 5)
            {
                return 1;
            }

            return count <= 14 ? 2 : 3;
        }

        /// <summary>
        /// Calcula el layout de <paramref name="count"/> cuias de <paramref name="heightU"/> u de alto en un lienzo de
        /// <paramref name="canvasWidth"/> dp, con el bloque centrado entre <paramref name="boardTop"/> y
        /// <paramref name="boardBottom"/> (medidas desde arriba). La línea de pie máxima es
        /// <c>boardBottom − 15</c> (<see cref="ShelfDrop"/>) y la fila 1, elevada, no sube de <paramref name="boardTop"/>.
        /// </summary>
        public static BoardLayoutResult Compute(int count, float heightU, float canvasWidth, float boardTop, float boardBottom)
        {
            if (count < 1 || count > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Entre 1 y 16 cuias (02 R1).");
            }

            int rows = RowsFor(count);
            var result = new BoardLayoutResult(count, rows);
            int remaining = count;
            for (int r = 0; r < rows; r++)
            {
                int left = rows - r;
                int n = (remaining + left - 1) / left;
                result.PerRow[r] = n;
                remaining -= n;
            }

            int maxPerRow = result.PerRow[0];
            float width = Math.Min(canvasWidth, MaxBoardWidth);
            float slot = (width - SideMargins) / maxPerRow;
            float cuiaW = Clamp(0.70f * slot, MinCuiaWidth, MaxCuiaWidthFor(count));
            float footMax = boardBottom - ShelfDrop;
            float available = footMax - boardTop - Lift;
            float blockH = BlockHeight(rows, cuiaW, heightU);
            if (blockH > available && available > 0f)
            {
                // Ancho que hace caber las filas con sus huecos fijos (elevación + hueco), sin bajar de 28 dp.
                float fit = 2f * (available - ((rows - 1) * (Lift + RowGap))) / (rows * heightU);
                cuiaW = Math.Max(MinCuiaWidth, Math.Min(cuiaW, fit));
                blockH = BlockHeight(rows, cuiaW, heightU);
            }

            float cuiaH = heightU * (cuiaW / 2f);
            float block = Lift + blockH + ShelfDrop; // B de la guía 3.2
            float slack = (boardBottom - boardTop) - block;
            float blockTop = boardTop + (slack / 2f); // centrado (D-003): fin del anclaje inferior
            result.SlotWidth = slot;
            result.CuiaWidth = cuiaW;
            result.CuiaHeight = cuiaH;
            result.BlockTop = blockTop;
            result.BlockBottom = blockTop + block;

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                int n = result.PerRow[r];
                float x0 = (canvasWidth - (n * slot)) / 2f;
                float foot = blockTop + Lift + (r * (cuiaH + Lift + RowGap)) + cuiaH;
                result.RowFootY[r] = foot;
                for (int j = 0; j < n; j++)
                {
                    result.CenterX[index] = x0 + ((j + 0.5f) * slot);
                    result.FootY[index] = foot;
                    result.RowOf[index] = r;
                    index++;
                }
            }

            // Panel (guía 3.2): 8 dp desde el ancho útil (≤ 480), 22 dp bajo el bloque y 22 sobre él (36 en una fila); si
            // sube por encima de boardTop − 8 se recorta (el marco pasa bajo el HUD). Nunca se encoge una cuia por el marco.
            float panelX = ((canvasWidth - width) / 2f) + PanelSide;
            float topMargin = rows == 1 ? PanelMarginTopOneRow : PanelMargin;
            float panelTop = Math.Max(blockTop - topMargin, boardTop - 8f);
            float panelBottom = result.BlockBottom + PanelMargin;
            result.PanelX = panelX;
            result.PanelY = panelTop;
            result.PanelWidth = canvasWidth - (2f * panelX);
            result.PanelHeight = panelBottom - panelTop;
            return result;
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
