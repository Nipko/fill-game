#nullable enable
using System.Collections.Generic;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// <see cref="PerfBudget"/>: en el escritorio, los presupuestos de tiempo tal cual; con holgura solo si se pide
    /// (<c>-afPerfBudgetFactor</c> o <c>AF_PERF_BUDGET_FACTOR</c>), y nunca más estrictos que en el escritorio.
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
        public void WithoutArgumentOrVariable_IsOne()
        {
            Assert.That(PerfBudget.Resolve(new[] { "Unity.exe", "-batchmode" }, Env()), Is.EqualTo(1d));
            Assert.That(PerfBudget.Resolve(null, Env()), Is.EqualTo(1d));
        }

        [Test]
        public void TheCiEnvironment_DoesNotLoosenByItself()
        {
            Assert.That(PerfBudget.Resolve(null, Env("GITHUB_ACTIONS", "true", "CI", "true")), Is.EqualTo(1d),
                "solo afloja quien lo pide: ci.yml pasa -afPerfBudgetFactor");
        }

        [Test]
        public void TheArgument_WinsOverTheVariable()
        {
            string[] args = { "Unity", PerfBudget.FactorArgument, "2.5" };
            Assert.That(PerfBudget.Resolve(args, Env(PerfBudget.FactorVariable, "4")), Is.EqualTo(2.5d));
            Assert.That(PerfBudget.Resolve(null, Env(PerfBudget.FactorVariable, "4")), Is.EqualTo(4d));
            Assert.That(PerfBudget.Resolve(new[] { "-afperfbudgetfactor", "3" }, Env()), Is.EqualTo(3d), "sin distinguir mayúsculas");
        }

        [Test]
        public void NeverTighter_AndUnreadableValuesFallThrough()
        {
            Assert.That(PerfBudget.Resolve(new[] { PerfBudget.FactorArgument, "0.5" }, Env()), Is.EqualTo(1d), "solo afloja");
            Assert.That(PerfBudget.Resolve(new[] { PerfBudget.FactorArgument, "x" }, Env(PerfBudget.FactorVariable, "2")),
                Is.EqualTo(2d), "ilegible: la fuente siguiente");
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
