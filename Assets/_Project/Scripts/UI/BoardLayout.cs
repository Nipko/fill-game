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
            CenterX = new float[count];
            FootY = new float[count];
            RowOf = new int[count];
        }

        /// <summary>Filas (1, 2 o 3).</summary>
        public int Rows { get; }

        /// <summary>Cuias por fila, de arriba abajo.</summary>
        public int[] PerRow { get; }

        /// <summary>Ancho de la ranura (área táctil, 02 §3.3).</summary>
        public float SlotWidth { get; internal set; }

        /// <summary>Ancho de la cuia.</summary>
        public float CuiaWidth { get; internal set; }

        /// <summary>Alto de la cuia más alta.</summary>
        public float CuiaHeight { get; internal set; }

        /// <summary>Centro horizontal de cada cuia.</summary>
        public float[] CenterX { get; }

        /// <summary>Pie de cada cuia, medido desde arriba del lienzo (filas alineadas por el pie, 07 §4.5; la última fila en la línea de pie).</summary>
        public float[] FootY { get; }

        /// <summary>Fila de cada cuia.</summary>
        public int[] RowOf { get; }
    }

    /// <summary>
    /// Layout del tablero (GDD 02 §5; 07 §4.1 y §7.4): 1 fila con T ≤ 5, 2 filas con 6-14 (⌈T/2⌉ arriba), 3 con 15-16;
    /// ranura = (ancho − 24) / cuias_por_fila; cuia_w = clamp(0,70 × ranura, 28, 48); alto = 6 u (u = cuia_w / 2);
    /// paso de fila = alto + 16 (elevación) + 24 (hueco). El tablero se centra en horizontal a 480 dp como máximo
    /// (tabletas) y en vertical se ancla abajo: la última fila apoya el pie en la línea de pie (centro de la banda de río
    /// del fondo del tablero, 07 §7.4) y las demás se apilan encima. Si las filas no caben (16:9), las cuias se reducen
    /// hasta el mínimo de 28 dp.
    /// </summary>
    public static class BoardLayout
    {
        /// <summary>Margen lateral total (02 §5).</summary>
        public const float SideMargins = 24f;

        /// <summary>Ancho máximo del tablero (02 §5, tabletas).</summary>
        public const float MaxBoardWidth = 480f;

        /// <summary>Ancho mínimo de la cuia (02 §5).</summary>
        public const float MinCuiaWidth = 28f;

        /// <summary>Ancho máximo de la cuia (02 §5).</summary>
        public const float MaxCuiaWidth = 48f;

        /// <summary>Elevación de la selección (02 §4.1).</summary>
        public const float Lift = 16f;

        /// <summary>Hueco entre filas (02 §5).</summary>
        public const float RowGap = 24f;

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
        /// <paramref name="canvasWidth"/> dp: la última fila apoya el pie en <paramref name="footLine"/> y la primera, elevada,
        /// no sube de <paramref name="boardTop"/> (medidas desde arriba).
        /// </summary>
        public static BoardLayoutResult Compute(int count, float heightU, float canvasWidth, float boardTop, float footLine)
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
            float cuiaW = Clamp(0.70f * slot, MinCuiaWidth, MaxCuiaWidth);
            float available = footLine - boardTop - Lift;
            float blockH = BlockHeight(rows, cuiaW, heightU);
            if (blockH > available && available > 0f)
            {
                // Ancho que hace caber las filas con sus huecos fijos (elevación + hueco), sin bajar de 28 dp.
                float fit = 2f * (available - ((rows - 1) * (Lift + RowGap))) / (rows * heightU);
                cuiaW = Math.Max(MinCuiaWidth, Math.Min(cuiaW, fit));
                blockH = BlockHeight(rows, cuiaW, heightU);
            }

            float cuiaH = heightU * (cuiaW / 2f);
            float top = footLine - blockH; // anclado abajo: el hueco sobrante queda arriba
            result.SlotWidth = slot;
            result.CuiaWidth = cuiaW;
            result.CuiaHeight = cuiaH;

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                int n = result.PerRow[r];
                float x0 = (canvasWidth - (n * slot)) / 2f;
                float foot = top + (r * (cuiaH + Lift + RowGap)) + cuiaH;
                for (int j = 0; j < n; j++)
                {
                    result.CenterX[index] = x0 + ((j + 0.5f) * slot);
                    result.FootY[index] = foot;
                    result.RowOf[index] = r;
                    index++;
                }
            }

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
