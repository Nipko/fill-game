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
    /// Modo de vertido de la partida (D-005, que sustituye a D-001; 08 §9 <c>rules_pour_mode</c>;
    /// <c>docs/dev/decisiones.md</c> §2.1.7): <see cref="LaunchOptions.ResolvePourMode"/> da prioridad al gancho de test,
    /// luego al extra de intent <c>af_rules_pour_mode</c> (solo builds de desarrollo en Android; sirve para probar
    /// <c>"single"</c>) y por último al default compilado <see cref="GameConfig.RulesPourMode"/> (<c>"run"</c>); un valor
    /// desconocido acaba en <c>"run"</c> con un aviso en el log.
    /// En el Editor no hay intent: la rama del extra se prueba en el teléfono (criterio A8).
    /// </summary>
    public sealed class LaunchOptionsTests
    {
        private static readonly Regex UnknownModeWarning = new Regex(@"^\[AF\.LaunchOptions\] valor de modo de vertido desconocido .*se usa ""run""");

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
        public void WithoutOverrideOrIntent_UsesRulesPourModeDefault_Run()
        {
            Assert.That(LaunchOptions.PourModeOverride, Is.Null);
            Assert.That(LaunchOptions.ReadIntentPourMode(), Is.Null, "fuera de Android no se lee ningún extra de intent");
            Assert.That(PourModes.TryParse(GameConfig.RulesPourMode, out PourMode compiled), Is.True);

            PourMode mode = LaunchOptions.ResolvePourMode();

            Assert.That(mode, Is.EqualTo(compiled));
            Assert.That(mode, Is.EqualTo(PourMode.Run), "D-005: la racha que cabe por defecto, como Magic Sort");
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
        [TestCase("Single")]
        [TestCase("")]
        public void UnknownValue_FallsBackToRun_WithAWarning(string value)
        {
            LaunchOptions.PourModeOverride = value;
            LogAssert.Expect(LogType.Warning, UnknownModeWarning);

            Assert.That(LaunchOptions.ResolvePourMode(), Is.EqualTo(PourMode.Run));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Reset_ClearsTheOverride_AndTheDefaultApplies()
        {
            LaunchOptions.PourModeOverride = "single";
            Assert.That(LaunchOptions.ResolvePourMode(), Is.EqualTo(PourMode.Single));

            LaunchOptions.Reset();

            Assert.That(LaunchOptions.PourModeOverride, Is.Null);
            Assert.That(LaunchOptions.ResolvePourMode(), Is.EqualTo(PourMode.Run));
        }

        [Test]
        public void Reset_ClearsTheLevelPackOverride()
        {
            // Gancho solo de tests (D-006, tablero de 13 cuias): null en producción y tras Reset.
            Assert.That(LaunchOptions.LevelPackJsonOverride, Is.Null);
            LaunchOptions.LevelPackJsonOverride = "{}";
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.LevelPackJsonOverride, Is.Null);
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
