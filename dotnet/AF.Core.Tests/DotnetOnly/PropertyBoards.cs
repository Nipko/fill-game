#nullable enable
using System;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Solver.Tests;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>Familia de capacidades de los tableros de propiedad (02 §8).</summary>
    public enum CapacityFamily
    {
        /// <summary>Todas las cuias de capacidad 4 y <c>U_c</c> = 4 (el tutorial y la mayoría de la campaña).</summary>
        Uniform,

        /// <summary>Cuia chica (1-3) como búfer y Cuia alta (5-6) con su fruta abundante (<c>U_c</c> = cap).</summary>
        Mixed,
    }

    /// <summary>
    /// Generador de tableros iniciales pequeños (≤ 6 cuias) con semilla fija para los tests de propiedad de R13, R14 y R16
    /// (02 §1.4; anexo B de D-007): sus estados alcanzables se recorren enteros con
    /// <c>AF.Solver.Tests.ExhaustiveSearch</c>. Los tableros no tienen por qué ser resolubles.
    /// </summary>
    internal static class PropertyBoards
    {
        /// <summary>Un tablero inicial de la familia, o null si el inyector no pudo poner su mecánica.</summary>
        public static BoardState? Start(Prng rng, CapacityFamily family, IMechanicInjector? injector)
        {
            var caps = new List<int>();
            var pool = new List<int>();
            int color = 1;
            if (family == CapacityFamily.Uniform)
            {
                int colors = 2 + rng.NextInt(3);
                int empties = 1 + rng.NextInt(2);
                if (colors + empties > 6)
                {
                    empties = 6 - colors;
                }

                for (int c = 0; c < colors; c++, color++)
                {
                    caps.Add(4);
                    AddUnits(pool, color, 4);
                }

                for (int e = 0; e < empties; e++)
                {
                    caps.Add(4);
                }
            }
            else
            {
                bool tall = rng.NextInt(3) != 0;
                int standard = 1 + rng.NextInt(2);
                int small = (tall ? 0 : 1) + rng.NextInt(2);
                if (tall)
                {
                    int cap = 5 + rng.NextInt(2);
                    caps.Add(cap);
                    AddUnits(pool, color++, cap);
                }

                for (int c = 0; c < standard; c++, color++)
                {
                    caps.Add(4);
                    AddUnits(pool, color, 4);
                }

                for (int s = 0; s < small; s++)
                {
                    caps.Add(1 + rng.NextInt(3));
                }

                if (caps.Count < 6 && rng.NextInt(2) == 0)
                {
                    caps.Add(4); // un vacío estándar
                }

                if (Total(caps) <= pool.Count)
                {
                    caps.Add(1 + rng.NextInt(3)); // al menos un hueco libre
                }
            }

            int width = caps.Count;
            var doses = new List<int>[width];
            for (int i = 0; i < width; i++)
            {
                doses[i] = new List<int>();
            }

            int[] capArray = caps.ToArray();
            Deal(rng, pool, doses, capArray);
            var attrs = new MechanicSpec[width];
            if (injector != null && !injector.Inject(rng, capArray, doses, attrs))
            {
                return null;
            }

            var cuias = new int[width][];
            for (int i = 0; i < width; i++)
            {
                cuias[i] = doses[i].ToArray();
            }

            BoardBuildResult built = BoardBuilder.Build(capArray, cuias, injector == null ? null : attrs);
            if (!built.IsValid)
            {
                throw new InvalidOperationException("El generador de propiedad creó un tablero inválido: " + built.Error);
            }

            return built.State;
        }

        private static void AddUnits(List<int> pool, int color, int units)
        {
            for (int u = 0; u < units; u++)
            {
                pool.Add(color);
            }
        }

        private static int Total(List<int> caps)
        {
            int total = 0;
            for (int i = 0; i < caps.Count; i++)
            {
                total += caps[i];
            }

            return total;
        }

        private static void Deal(Prng rng, List<int> pool, List<int>[] doses, int[] caps)
        {
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                int tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }

            for (int d = 0; d < pool.Count; d++)
            {
                int target = rng.NextInt(doses.Length);
                while (doses[target].Count >= caps[target])
                {
                    target = (target + 1) % doses.Length;
                }

                doses[target].Add(pool[d]);
            }
        }
    }
}
