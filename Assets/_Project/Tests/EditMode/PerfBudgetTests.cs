#nullable enable
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// <see cref="PerfBudget"/>: en el escritorio, los presupuestos de tiempo tal cual; en la CI, con holgura; nunca más
    /// estrictos que en el escritorio.
    /// </summary>
    public sealed class PerfBudgetTests
    {
        private static System.Func<string, string?> Env(params string[] pairs)
        {
            var map = new Dictionary<string, string>();
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                map[pairs[i]] = pairs[i + 1];
            }

            return name => map.TryGetValue(name, out string value) ? value : null;
        }

        [Test]
        public void Desktop_WithoutCiOrFactor_IsOne()
        {
            Assert.That(PerfBudget.Resolve(new[] { "Unity.exe", "-batchmode" }, Env()), Is.EqualTo(1d));
            Assert.That(PerfBudget.Resolve(null, Env("CI", "false", "GITHUB_ACTIONS", "")), Is.EqualTo(1d));
        }

        [Test]
        public void GitHubActionsOrCi_UsesTheCiFactor()
        {
            Assert.That(PerfBudget.Resolve(null, Env("GITHUB_ACTIONS", "true")), Is.EqualTo(PerfBudget.CiFactor));
            Assert.That(PerfBudget.Resolve(null, Env("CI", "TRUE")), Is.EqualTo(PerfBudget.CiFactor));
            Assert.That(PerfBudget.Resolve(null, Env("CI", "1")), Is.EqualTo(PerfBudget.CiFactor));
        }

        [Test]
        public void TheArgument_WinsOverTheVariable_AndTheVariableOverCi()
        {
            string[] args = { "Unity", PerfBudget.FactorArgument, "2.5" };
            Assert.That(PerfBudget.Resolve(args, Env(PerfBudget.FactorVariable, "4", "CI", "true")), Is.EqualTo(2.5d));
            Assert.That(PerfBudget.Resolve(null, Env(PerfBudget.FactorVariable, "4", "CI", "true")), Is.EqualTo(4d));
        }

        [Test]
        public void NeverTighter_AndUnreadableValuesFallThrough()
        {
            Assert.That(PerfBudget.Resolve(new[] { PerfBudget.FactorArgument, "0.5" }, Env()), Is.EqualTo(1d), "solo afloja");
            Assert.That(PerfBudget.Resolve(new[] { PerfBudget.FactorArgument, "x" }, Env("GITHUB_ACTIONS", "true")),
                Is.EqualTo(PerfBudget.CiFactor), "ilegible: la fuente siguiente");
            Assert.That(PerfBudget.Resolve(new[] { PerfBudget.FactorArgument }, Env(PerfBudget.FactorVariable, "NaN")), Is.EqualTo(1d));
        }

        [Test]
        public void ThisRun_IsAtLeastOne_AndScalesTheBudget()
        {
            Assert.That(PerfBudget.Factor, Is.GreaterThanOrEqualTo(1d));
            Assert.That(PerfBudget.Ms(8d), Is.EqualTo(8d * PerfBudget.Factor));
            StringAssert.StartsWith("≤ ", PerfBudget.Describe(8d));
        }
    }
}
