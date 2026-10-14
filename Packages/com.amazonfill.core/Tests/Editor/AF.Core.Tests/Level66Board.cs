#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Nivel 66 de la campaña (<c>m03_v1_n0066</c>) y la secuencia que jugó el dueño en el teléfono el 2026-10-13: la regresión
    /// de Aguas estancadas por bucle (02 R13 ampliada; D-014 con la pregunta 46 (a);
    /// <c>docs/dev/traspaso/pruebas-telefono-2026-10-13.md</c>). Es la única copia del tablero en los tests del núcleo: la usan
    /// <see cref="DeadEndProbeTests"/> (compartido) y, solo en dotnet, <c>DotnetOnly/DeadEndProbePerformanceTests</c> y
    /// <c>DotnetOnly/HotLoopAllocationTests</c>. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    /// <remarks>
    /// Copiado de <c>Assets/_Project/Data/Levels/mundo_03_v1.json</c>, porque los tests compartidos no leen los packs: 10 cuias de
    /// 4 salvo la 5 (Cuia chica de 2) y una Hoja en la 6 cuya llave es el color 3. <c>StuckLoopTests</c> (PlayMode) no puede
    /// usarlo, porque este ensamblado es solo de Editor: juega el 66 desde su pack y lleva su propia copia de la secuencia.
    /// </remarks>
    internal static class Level66Board
    {
        private static readonly int[] Caps = { 4, 4, 4, 4, 4, 2, 4, 4, 4, 4 };

        private static readonly int[][] Start =
        {
            new[] { 3, 8, 10, 7 }, new[] { 4, 7, 4, 7 }, new[] { 10, 7, 4, 10 }, new[] { 14, 14, 14, 3 }, new[] { 6, 6, 6, 14 },
            new int[0], new int[0], new[] { 8, 8, 10, 6 }, new[] { 3, 3, 8, 1 }, new[] { 1, 1, 1, 4 },
        };

        /// <summary>Primer intento del dueño en el teléfono (logcat del 2026-10-13): 3>6 4>3 7>4 0>5 1>5 2>0 2>1 5>2 1>5 2>1 0>7 1>2.</summary>
        private static readonly int[][] Sequence =
        {
            new[] { 3, 6 }, new[] { 4, 3 }, new[] { 7, 4 }, new[] { 0, 5 }, new[] { 1, 5 }, new[] { 2, 0 },
            new[] { 2, 1 }, new[] { 5, 2 }, new[] { 1, 5 }, new[] { 2, 1 }, new[] { 0, 7 }, new[] { 1, 2 },
        };

        /// <summary>
        /// Tablero tras los 12 vertidos: 0 [3,8,10] · 1 [4,7] · 2 [10,7,7,7] · 3 sellada · 4 sellada · 5 (cap 2) [4,4] · 6 [3] ·
        /// 7 [8,8,10,10] · 8 [3,3,8,1] · 9 [1,1,1,4]; la Hoja ya se abrió (sin mecánicas).
        /// </summary>
        private static readonly int[][] AfterSequence =
        {
            new[] { 3, 8, 10 }, new[] { 4, 7 }, new[] { 10, 7, 7, 7 }, new[] { 14, 14, 14, 14 }, new[] { 6, 6, 6, 6 },
            new[] { 4, 4 }, new[] { 3 }, new[] { 8, 8, 10, 10 }, new[] { 3, 3, 8, 1 }, new[] { 1, 1, 1, 4 },
        };

        /// <summary>Número de vertidos de la secuencia del dueño (12).</summary>
        public static int PourCount => Sequence.Length;

        /// <summary>Origen del vertido <paramref name="pour"/> (desde 0) de la secuencia del dueño.</summary>
        public static int Source(int pour)
        {
            return Sequence[pour][0];
        }

        /// <summary>Destino del vertido <paramref name="pour"/> (desde 0) de la secuencia del dueño.</summary>
        public static int Target(int pour)
        {
            return Sequence[pour][1];
        }

        /// <summary>Tablero inicial del nivel, con la Hoja cerrada en la 6 (llave: el color 3).</summary>
        public static BoardState Initial()
        {
            var attrs = new MechanicSpec[Caps.Length];
            attrs[6] = new MechanicSpec(MechanicType.Hoja, key: 3);
            return Build(Start, attrs);
        }

        /// <summary>
        /// Tablero tras los 12 vertidos de la secuencia del dueño, sin mecánicas: el bucle. Quedan dos vertidos útiles, 2 → 1 y
        /// 7 → 0, que son vaivenes parciales; el cierre se recorre entero y se declara (R13 ampliada).
        /// </summary>
        public static BoardState Final()
        {
            return Build(AfterSequence, null);
        }

        private static BoardState Build(int[][] cuias, MechanicSpec[]? attrs)
        {
            BoardBuildResult built = BoardBuilder.Build(Caps, cuias, attrs);
            Assert.That(built.Error, Is.Null, "nivel 66");
            return built.State!;
        }
    }
}
