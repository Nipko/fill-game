#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Tableros pequeños con Tinaja y su cuia (M13; propuesta de la tinaja, b.2 y c.6) para comparar A* con la búsqueda
    /// exhaustiva, comprobar la heurística y, en dotnet, las propiedades P1-P3 de R14 (<c>DotnetOnly/FutilePropertyTests</c>).
    /// No pueden ser un <see cref="IMechanicInjector"/>: la tinaja añade dos cuias (capacidad 5 y su Cuia chica de 2) y la
    /// máscara de ocultos. Test compartido (NUnit 3.5, C# 9): solo usa el PRNG del núcleo.
    /// </summary>
    /// <remarks>
    /// Tablero base: 2-3 frutas de 4 dosis en cuias de 4; con <c>mixed</c>, a veces una Cuia alta de 5 o 6 con su fruta
    /// abundante y una Cuia chica normal de 1-3; a veces un vacío de 4. La tinaja saca 3-4 dosis del reparto, con T4 (vecinas
    /// distintas) y todas ocultas salvo la boca; su cuia va vacía en la ranura contigua. Capacidades presentes: 2, 4, 5 y, con
    /// <c>mixed</c>, 1-3 y 6. Los tableros no tienen por qué ser resolubles ni pasar <see cref="LevelValidation"/>.
    /// </remarks>
    internal static class TinajaBoards
    {
        /// <summary>
        /// <paramref name="count"/> tableros con tinaja de como mucho <paramref name="maxStates"/> estados alcanzables en
        /// <c>single</c> (todos los vertidos legales).
        /// </summary>
        public static List<BoardState> Generate(ulong seed, int count, bool mixed, int maxStates)
        {
            var rng = new Prng(seed);
            var boards = new List<BoardState>();
            for (int attempt = 0; attempt < count * 50 && boards.Count < count; attempt++)
            {
                BoardState? board = Start(rng, mixed);
                if (board != null && Fits(board, maxStates))
                {
                    boards.Add(board);
                }
            }

            Assert.That(boards.Count, Is.EqualTo(count), "tinaja: no se generaron bastantes tableros");
            return boards;
        }

        /// <summary>Un tablero con tinaja, o null si el reparto no deja poner sus dosis con T4.</summary>
        public static BoardState? Start(Prng rng, bool mixed)
        {
            var caps = new List<int>();
            var pool = new List<int>();
            int colors = 2 + rng.NextInt(2);
            int color = 1;
            if (mixed && rng.NextInt(2) == 0)
            {
                int tall = 5 + rng.NextInt(2);
                caps.Add(tall);
                AddUnits(pool, color++, tall);
                colors--;
            }

            for (int c = 0; c < colors; c++, color++)
            {
                caps.Add(4);
                AddUnits(pool, color, 4);
            }

            if (mixed && rng.NextInt(2) == 0)
            {
                caps.Add(1 + rng.NextInt(3)); // Cuia chica normal
            }

            if (rng.NextInt(2) == 0)
            {
                caps.Add(4); // un vacío
            }

            Shuffle(rng, pool);
            int doses = 3 + rng.NextInt(2);
            var tinaja = new List<int>();
            for (int k = 0; k < doses; k++)
            {
                int pick = -1;
                for (int p = 0; p < pool.Count && pick < 0; p++)
                {
                    if (tinaja.Count == 0 || pool[p] != tinaja[tinaja.Count - 1])
                    {
                        pick = p; // T4 en toda la columna: dos vecinas nunca son de la misma fruta
                    }
                }

                if (pick < 0)
                {
                    return null;
                }

                tinaja.Add(pool[pick]);
                pool.RemoveAt(pick);
            }

            int normal = caps.Count;
            var dealt = new List<int>[normal];
            for (int i = 0; i < normal; i++)
            {
                dealt[i] = new List<int>();
            }

            for (int d = 0; d < pool.Count; d++)
            {
                int target = rng.NextInt(normal);
                while (dealt[target].Count >= caps[target])
                {
                    target = (target + 1) % normal;
                }

                dealt[target].Add(pool[d]);
            }

            int at = rng.NextInt(normal + 1);
            bool cuiaFirst = rng.NextInt(2) == 0;
            int width = normal + 2;
            var allCaps = new int[width];
            var cuias = new int[width][];
            var attrs = new MechanicSpec[width];
            int tinajaIndex = cuiaFirst ? at + 1 : at;
            int cuiaIndex = cuiaFirst ? at : at + 1;
            for (int i = 0, n = 0; i < width; i++)
            {
                if (i == tinajaIndex)
                {
                    allCaps[i] = LevelValidation.TinajaCapacity;
                    cuias[i] = tinaja.ToArray();
                    attrs[i] = new MechanicSpec(MechanicType.Tinaja);
                }
                else if (i == cuiaIndex)
                {
                    allCaps[i] = LevelValidation.CucharonCapacity;
                    cuias[i] = new int[0];
                    attrs[i] = new MechanicSpec(MechanicType.Cucharon);
                }
                else
                {
                    allCaps[i] = caps[n];
                    cuias[i] = dealt[n].ToArray();
                    n++;
                }
            }

            var hidden = new int[doses - 1][];
            for (int k = 0; k < doses - 1; k++)
            {
                hidden[k] = new[] { tinajaIndex, k };
            }

            BoardBuildResult built = BoardBuilder.Build(allCaps, cuias, attrs, hidden);
            if (!built.IsValid)
            {
                throw new InvalidOperationException("tinaja: tablero inválido: " + built.Error);
            }

            return built.State;
        }

        /// <summary>Índice de la tinaja (−1 si no hay).</summary>
        public static int TinajaIndex(BoardState state)
        {
            for (int i = 0; i < state.Count; i++)
            {
                if (state[i].Mechanic == MechanicType.Tinaja)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool Fits(BoardState board, int maxStates)
        {
            try
            {
                ExhaustiveSearch.Explore(board, PourMode.Single, maxStates);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static void AddUnits(List<int> pool, int color, int units)
        {
            for (int u = 0; u < units; u++)
            {
                pool.Add(color);
            }
        }

        private static void Shuffle(Prng rng, List<int> pool)
        {
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                int tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }
        }
    }
}
