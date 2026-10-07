#nullable enable
using System.Text.RegularExpressions;
using AF.Core.Rules;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Modo de vertido de la partida (D-001; 08 §9 <c>rules_pour_mode</c>; <c>docs/dev/decisiones.md</c> §2.1.7):
    /// <see cref="LaunchOptions.ResolvePourMode"/> da prioridad al gancho de test, luego al extra de intent
    /// <c>af_rules_pour_mode</c> (solo builds de desarrollo en Android) y por último al default compilado
    /// <see cref="GameConfig.RulesPourMode"/>; un valor desconocido acaba en <c>"single"</c> con un aviso en el log.
    /// En el Editor no hay intent: la rama del extra se prueba en el teléfono (criterio A8).
    /// </summary>
    public sealed class LaunchOptionsTests
    {
        private static readonly Regex UnknownModeWarning = new Regex(@"^\[AF\.LaunchOptions\] valor de modo de vertido desconocido .*se usa ""single""");

        [SetUp]
        public void SetUp()
        {
            LaunchOptions.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [Test]
        public void WithoutOverrideOrIntent_UsesRulesPourModeDefault_Single()
        {
            Assert.That(LaunchOptions.PourModeOverride, Is.Null);
            Assert.That(LaunchOptions.ReadIntentPourMode(), Is.Null, "fuera de Android no se lee ningún extra de intent");
            Assert.That(PourModes.TryParse(GameConfig.RulesPourMode, out PourMode compiled), Is.True);

            PourMode mode = LaunchOptions.ResolvePourMode();

            Assert.That(mode, Is.EqualTo(compiled));
            Assert.That(mode, Is.EqualTo(PourMode.Single), "D-001: una dosis por vertido por defecto");
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase("run", PourMode.Run)]
        [TestCase("single", PourMode.Single)]
        public void Override_TakesPriorityOverTheCompiledDefault(string value, PourMode expected)
        {
            LaunchOptions.PourModeOverride = value;

            Assert.That(LaunchOptions.ResolvePourMode(), Is.EqualTo(expected));
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase("bogus")]
        [TestCase("RUN")]
        [TestCase(" run")]
        [TestCase("")]
        public void UnknownValue_FallsBackToSingle_WithAWarning(string value)
        {
            LaunchOptions.PourModeOverride = value;
            LogAssert.Expect(LogType.Warning, UnknownModeWarning);

            Assert.That(LaunchOptions.ResolvePourMode(), Is.EqualTo(PourMode.Single));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Reset_ClearsTheOverride_AndTheDefaultApplies()
        {
            LaunchOptions.PourModeOverride = "run";
            Assert.That(LaunchOptions.ResolvePourMode(), Is.EqualTo(PourMode.Run));

            LaunchOptions.Reset();

            Assert.That(LaunchOptions.PourModeOverride, Is.Null);
            Assert.That(LaunchOptions.ResolvePourMode(), Is.EqualTo(PourMode.Single));
        }

        [Test]
        public void IntentExtras_AreNotReadOutsideAndroid()
        {
            Assert.That(LaunchOptions.ReadIntentLevel(), Is.EqualTo(0));
            Assert.That(LaunchOptions.ReadIntentPourMode(), Is.Null);
            Assert.That(LaunchOptions.IntentExtraPourMode, Is.EqualTo("af_rules_pour_mode"));
        }
    }
}
