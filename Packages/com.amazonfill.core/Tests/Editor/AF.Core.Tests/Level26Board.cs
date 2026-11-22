#nullable enable
using AF.Core.Board;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Nivel 26 de la campaña (<c>m02_v1_n0026</c>): el tablero de la prueba de la 10002 en el que el modal «No hay más vertidos»
    /// salió con vertidos todavía legales (punto 1 del añadido de D-059 del 2026-10-31; añadido de D-014 del 2026-11-06). Es la
    /// única copia del tablero en los tests del núcleo. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    /// <remarks>
    /// Las capacidades salen de <c>Assets/_Project/Data/Levels/mundo_02_v1.json</c> (la Cuia chica de 2 en el 4.º puesto) y los
    /// colores, de sus cuias iniciales: Y amarillo = 3, P rosa = 7, W blanco = 1, B azul = 10, M morado = 8, R rojo = 6,
    /// O naranja = 4. Índices desde 0; el dueño numera de arriba abajo y de izquierda a derecha desde 1 (la cuia k del dueño es
    /// el índice k − 1).
    /// </remarks>
    internal static class Level26Board
    {
        private static readonly int[] Caps = { 4, 4, 4, 2, 4, 4, 4, 4, 4 };

        /// <summary>De abajo arriba. Fila de arriba: 1-5; fila de abajo: 6-9.</summary>
        private static readonly int[][] Stuck =
        {
            new[] { 3, 7, 7, 1 }, new[] { 1, 1, 1, 7 }, new[] { 3, 3, 3 }, new[] { 10, 10 }, new[] { 10, 8, 8 },
            new[] { 10, 6, 4, 6 }, new[] { 6 }, new[] { 8, 8, 4, 4 }, new[] { 6, 4, 7 },
        };

        /// <summary>
        /// El tablero del añadido de D-059: perdido (12 estados alcanzables, ninguno ganado), con dos vertidos legales, 2 → 9 y
        /// 6 → 7 del dueño; tras 2 → 9, también 1 → 2.
        /// </summary>
        public static BoardState Lost()
        {
            BoardBuildResult built = BoardBuilder.Build(Caps, Stuck, null);
            Assert.That(built.Error, Is.Null, "nivel 26");
            return built.State!;
        }
    }
}
