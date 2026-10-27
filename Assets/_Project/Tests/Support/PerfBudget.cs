#nullable enable
using System;
using System.Globalization;

namespace AF.Tests
{
    /// <summary>
    /// Presupuestos de tiempo de reloj (ms) de los tests de rendimiento. Los números de los tests son los del escritorio, donde
    /// se miden tal cual (factor 1). La CI de GitHub (runner Linux compartido, sin GPU y más lento) los multiplica por un
    /// factor que pide ella misma: ci.yml pasa <c>-afPerfBudgetFactor 3</c>. La primera ejecución real de ci.yml (2026-10-27)
    /// dio 17,4 ms frente a 10 y 14,95 frente a 8 (≈ 1,9 ×). Así la CI sigue parando una regresión grande (un bucle o una
    /// asignación por píxel) sin romperse por la máquina, y el escritorio sigue vigilando el presupuesto exacto.
    /// <para>
    /// El factor sale, por orden, del argumento <c>-afPerfBudgetFactor N</c> de la línea de comandos de Unity o de la variable
    /// <c>AF_PERF_BUDGET_FACTOR</c>; si no, 1. No se deduce del entorno (<c>CI</c>, <c>GITHUB_ACTIONS</c>): solo afloja quien
    /// lo pide. Nunca es menor que 1. Un valor ilegible se ignora y se pasa a la fuente siguiente.
    /// </para>
    /// </summary>
    public static class PerfBudget
    {
        /// <summary>Argumento de la línea de comandos de Unity con el factor.</summary>
        public const string FactorArgument = "-afPerfBudgetFactor";

        /// <summary>Variable de entorno con el factor.</summary>
        public const string FactorVariable = "AF_PERF_BUDGET_FACTOR";

        private static double? s_factor;

        /// <summary>Factor de esta ejecución (1 en el escritorio).</summary>
        public static double Factor => s_factor ??= Resolve(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable);

        /// <summary>El presupuesto del escritorio <paramref name="desktopMs"/> con el factor de esta ejecución.</summary>
        public static double Ms(double desktopMs) => desktopMs * Factor;

        /// <summary>Texto para el mensaje del assert: «≤ 8 ms» o «≤ 24 ms (8 × 3, factor de la CI)».</summary>
        public static string Describe(double desktopMs)
        {
            string budget = Ms(desktopMs).ToString("0.##", CultureInfo.InvariantCulture);
            if (Factor == 1d)
            {
                return "≤ " + budget + " ms";
            }

            return "≤ " + budget + " ms (" + desktopMs.ToString("0.##", CultureInfo.InvariantCulture) + " × "
                + Factor.ToString("0.##", CultureInfo.InvariantCulture) + ", factor de la CI)";
        }

        /// <summary>Calcula el factor a partir de los argumentos y del entorno (público para sus tests).</summary>
        public static double Resolve(string[]? args, Func<string, string?> environment)
        {
            if (args != null)
            {
                for (int i = 0; i + 1 < args.Length; i++)
                {
                    if (string.Equals(args[i], FactorArgument, StringComparison.OrdinalIgnoreCase) && TryParse(args[i + 1], out double fromArgs))
                    {
                        return fromArgs;
                    }
                }
            }

            if (TryParse(environment(FactorVariable), out double fromVariable))
            {
                return fromVariable;
            }

            return 1d;
        }

        private static bool TryParse(string? raw, out double factor)
        {
            factor = 1d;
            if (string.IsNullOrWhiteSpace(raw)
                || !double.TryParse(raw!.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                || double.IsNaN(parsed) || double.IsInfinity(parsed))
            {
                return false;
            }

            factor = Math.Max(1d, parsed);
            return true;
        }
    }
}
