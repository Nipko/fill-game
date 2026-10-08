#nullable enable
using AF.Core.Rules;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Línea <c>AF_STATE</c> de la sonda de desarrollo (docs/dev/prototipo-v0.md c) con D-005 (run por defecto; single en
    /// pruebas): cada línea declara el modo de vertido de la sesión y cada vertido lleva su cantidad (<c>"amount"</c> y
    /// el tercer valor de <c>"move"</c>).
    /// </summary>
    public sealed class DevProbeTests
    {
        [Test]
        public void PourLine_CarriesThePourMode_AndTheAmountOfThePour()
        {
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor y en builds de desarrollo");
            // Flujo por defecto (run, D-005): el vertido 0→3 del nivel 2 mueve la racha de 2 aguajes a la cuia vacía.
            var flow = new LevelFlow(TestPacks.Tutorial, new MemoryProgressStore(), new RecordingListener());
            flow.Load(2);
            flow.TapAnywhere();
            flow.TapCuia(0);
            TapResult r = flow.TapCuia(3);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured));

            string? line = new DevProbe().FormatState("pour", flow, 3, r.Pour.Pour);
            Assert.That(line, Is.Not.Null);
            Assert.That(line, Does.StartWith("AF_STATE {\"event\":\"pour\""));
            Assert.That(line, Does.Contain("\"pour_mode\":\"run\""));
            Assert.That(line, Does.Contain("\"move\":[0,3,2]"));
            Assert.That(line, Does.Contain("\"amount\":2"));
            Assert.That(line, Does.Contain("\"pours\":1"));
            Assert.That(line, Does.EndWith("]}"));
        }

        [Test]
        public void PourLine_InSingleMode_SaysSingle_AndMovesOneDose()
        {
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor y en builds de desarrollo");
            var flow = new LevelFlow(TestPacks.Tutorial, new MemoryProgressStore(), new RecordingListener(), true, PourMode.Single);
            flow.Load(2);
            flow.TapAnywhere();
            flow.TapCuia(2);
            TapResult r = flow.TapCuia(3);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured));

            string? line = new DevProbe().FormatState("pour", flow, 3, r.Pour.Pour);
            Assert.That(line, Does.Contain("\"pour_mode\":\"single\""));
            Assert.That(line, Does.Contain("\"move\":[2,3,1]"));
            Assert.That(line, Does.Contain("\"amount\":1"));
        }

        [Test]
        public void LobbyLine_CarriesLevelSeedsAndCanoes()
        {
            // B1-6: AF_STATE del inicio con el nivel del botón Jugar, las semillas y las canoas (desde el 31, con cronómetro).
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor y en builds de desarrollo");
            var economy = new EconomyFixture(seeds: 1240);
            var progress = new MemoryProgressStore(31);
            var flow = new LevelFlow(AF.Levels.LevelCatalog.Create(new[] { TestPacks.Tutorial }, 30), progress, new RecordingListener());
            var app = new AppFlow(flow, progress, economy.Wallet, new NullAppListener());
            Assert.That(economy.Wallet.Apply(AF.Economy.Transaction.HardFail(31)).IsApplied, Is.True);
            app.Start(false);

            string? line = new DevProbe().FormatApp("lobby", app);
            Assert.That(line, Does.StartWith("AF_STATE {\"event\":\"lobby\""));
            Assert.That(line, Does.Contain("\"screen\":\"lobby\""));
            Assert.That(line, Does.Contain("\"level\":31"));
            Assert.That(line, Does.Contain("\"available\":true"));
            Assert.That(line, Does.Contain("\"seeds\":1240"));
            Assert.That(line, Does.Contain("\"canoes\":4"));
            Assert.That(line, Does.Contain("\"canoes_max\":5"));
            Assert.That(line, Does.Contain("\"canoes_visible\":true"));
            Assert.That(line, Does.Contain("\"next_canoe_s\":1500"), "25 min (canoes_regen_min)");
            Assert.That(line, Does.EndWith("}"));
        }

        [Test]
        public void LoadLine_InTheDefaultMode_SaysRun_AndHasNoAmount()
        {
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor y en builds de desarrollo");
            var flow = new LevelFlow(TestPacks.Tutorial, new MemoryProgressStore(), new RecordingListener());
            flow.Load(2);

            string? line = new DevProbe().FormatState("load", flow, int.MinValue, null);
            Assert.That(line, Does.Contain("\"pour_mode\":\"run\""));
            Assert.That(line, Does.Not.Contain("\"amount\""));
            Assert.That(line, Does.Not.Contain("\"move\""));
        }

        private sealed class NullAppListener : IAppFlowListener
        {
            public void OnAppScreenChanged(AppScreen screen)
            {
            }
        }
    }
}
