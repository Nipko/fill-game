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
        public void Reset_ClearsTheReuseDataReadsOverride()
        {
            // Gancho solo de tests (T6, carril A: los datos de la escena leídos una vez por pasada): null en el juego y tras Reset.
            Assert.That(LaunchOptions.ReuseDataReadsOverride, Is.Null);
            LaunchOptions.ReuseDataReadsOverride = true;
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.ReuseDataReadsOverride, Is.Null);
        }

        [Test]
        public void IntentExtras_AreNotReadOutsideAndroid()
        {
            Assert.That(LaunchOptions.ReadIntentLevel(), Is.EqualTo(0));
            Assert.That(LaunchOptions.ReadIntentPourMode(), Is.Null);
            Assert.That(LaunchOptions.ReadIntentInt(LaunchOptions.IntentExtraLevelOffset), Is.EqualTo(0));
            Assert.That(LaunchOptions.IntentExtraPourMode, Is.EqualTo("af_rules_pour_mode"));
            Assert.That(LaunchOptions.IntentExtraLevelOffset, Is.EqualTo("af_level_offset"));
        }

        [Test]
        public void LevelOffset_IsZeroByDefault_TakesTheOverride_AndRejectsOutOfRange()
        {
            // af_level_offset [P] (B1-5): solo builds de desarrollo; fuera de Android y sin gancho, 0.
            Assert.That(LaunchOptions.ResolveLevelOffset(), Is.EqualTo(0));
            LaunchOptions.LevelOffsetOverride = 30;
            Assert.That(LaunchOptions.ResolveLevelOffset(), Is.EqualTo(30));
            LaunchOptions.LevelOffsetOverride = -1;
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[AF\.LaunchOptions\] af_level_offset fuera de"));
            Assert.That(LaunchOptions.ResolveLevelOffset(), Is.EqualTo(0));
        }

        [TestCase("fill", AF.Ads.FakeAdFill.Fill, true)]
        [TestCase("no_fill", AF.Ads.FakeAdFill.NoFill, true)]
        [TestCase("error", AF.Ads.FakeAdFill.Error, true)]
        [TestCase("NoFill", AF.Ads.FakeAdFill.Fill, false)]
        [TestCase(null, AF.Ads.FakeAdFill.Fill, false)]
        public void AdsFill_ParsesTheDevExtra(string? value, AF.Ads.FakeAdFill expected, bool known)
        {
            // af_ads_fill [P] (B2-6): solo builds de desarrollo; fuera de Android y sin gancho, fill.
            Assert.That(LaunchOptions.TryParseAdFill(value, out AF.Ads.FakeAdFill fill), Is.EqualTo(known));
            Assert.That(fill, Is.EqualTo(expected));
        }

        [Test]
        public void AdsFill_TakesTheTestHook_OrFill()
        {
            Assert.That(LaunchOptions.ResolveAdFill(), Is.EqualTo(AF.Ads.FakeAdFill.Fill));
            Assert.That(LaunchOptions.ReadIntentString(LaunchOptions.IntentExtraAdsFill), Is.Null);
            LaunchOptions.AdFillOverride = AF.Ads.FakeAdFill.NoFill;
            Assert.That(LaunchOptions.ResolveAdFill(), Is.EqualTo(AF.Ads.FakeAdFill.NoFill));
        }

        [Test]
        public void DevWallet_SetsSeedsAndCanoes_WithTheCanoeClockRunning()
        {
            // af_seeds y af_canoes [P] (B2-6): fuera de Android no hay extras y nada cambia.
            var economy = new EconomyFixture(seeds: 10);
            Assert.That(LaunchOptions.ReadIntentInt(LaunchOptions.IntentExtraSeeds, -1), Is.EqualTo(-1));
            Assert.That(LaunchOptions.ApplyDevWallet(economy.Profile, economy.Clock), Is.False);
            Assert.That(economy.Profile.Wallet.Seeds, Is.EqualTo(10));

            LogAssert.Expect(LogType.Warning, new Regex(@"^\[AF\.LaunchOptions\] perfil de desarrollo"));
            Assert.That(LaunchOptions.ApplyDevWallet(economy.Profile, economy.Clock, 1000, 1, AF.Economy.EconomyConfig.Defaults), Is.True);
            Assert.That(economy.Profile.Wallet.Seeds, Is.EqualTo(1000));
            Assert.That(economy.Profile.Wallet.Canoes.Count, Is.EqualTo(1));
            Assert.That(economy.Profile.Wallet.Canoes.NextRegenAt, Is.EqualTo(EconomyFixture.T0 + System.TimeSpan.FromMinutes(25)), "canoes_regen_min");
            Assert.That(economy.Wallet.Canoes.Count, Is.EqualTo(1));

            LogAssert.Expect(LogType.Warning, new Regex(@"^\[AF\.LaunchOptions\] perfil de desarrollo"));
            Assert.That(LaunchOptions.ApplyDevWallet(economy.Profile, economy.Clock, -1, 9, AF.Economy.EconomyConfig.Defaults), Is.True);
            Assert.That(economy.Profile.Wallet.Canoes.Count, Is.EqualTo(5), "como mucho canoes_max");
            Assert.That(economy.Profile.Wallet.Canoes.NextRegenAt, Is.Null);
            Assert.That(economy.Profile.Wallet.Seeds, Is.EqualTo(1000), "−1 no toca las semillas");
        }

        [Test]
        public void LevelCardsOverride_TakesPriority_AndResetClearsIt()
        {
            // Tarea T10 del paso 1: la tarjeta previa (LEVEL_CARD, 02 §10) la decide el gancho del test o, si no,
            // GameConfig.LevelCardsEnabledDefault (true desde T13, que trajo su vista). Solo tests: ningún extra de intent.
            Assert.That(LaunchOptions.LevelCardsOverride, Is.Null);
            Assert.That(LaunchOptions.ResolveLevelCards(), Is.EqualTo(GameConfig.LevelCardsEnabledDefault));
            LaunchOptions.LevelCardsOverride = true;
            Assert.That(LaunchOptions.ResolveLevelCards(), Is.True);
            LaunchOptions.LevelCardsOverride = false;
            Assert.That(LaunchOptions.ResolveLevelCards(), Is.False);
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.LevelCardsOverride, Is.Null);
            Assert.That(LaunchOptions.ResolveLevelCards(), Is.EqualTo(GameConfig.LevelCardsEnabledDefault));
        }

        [Test]
        public void Reset_ClearsTheProfileClockAndOffsetHooks()
        {
            LaunchOptions.ProfileStoreOverride = new AF.Save.MemoryProfileStore();
            LaunchOptions.ClockOverride = new AF.Platform.ManualClock(new System.DateTime(2026, 10, 8, 15, 0, 0, System.DateTimeKind.Utc));
            LaunchOptions.LevelOffsetOverride = 30;
            LaunchOptions.Reset();
            Assert.That(LaunchOptions.ProfileStoreOverride, Is.Null);
            Assert.That(LaunchOptions.ClockOverride, Is.Null);
            Assert.That(LaunchOptions.LevelOffsetOverride, Is.Null);
        }
    }
}
