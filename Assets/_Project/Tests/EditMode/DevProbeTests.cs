#nullable enable
using System.Text;
using System.Text.RegularExpressions;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels.Json;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Línea <c>AF_STATE</c> de la sonda de desarrollo (docs/dev/prototipo-v0.md c) con D-005 (run por defecto; single en
    /// pruebas): cada línea declara el modo de vertido de la sesión y cada vertido lleva su cantidad (<c>"amount"</c> y
    /// el tercer valor de <c>"move"</c>). Con las mecánicas de la 1.0 (arquitectura del paso 1, §9): el mundo, el agua, la
    /// etiqueta y los campos del tablero (<c>murky</c>, <c>mech</c>, <c>hidden</c>, <c>sealed</c>, <c>counter</c>) en cada
    /// línea, y <c>recv</c> y <c>ev</c> en cada vertido, con formateadores puros probados sobre tableros sintéticos; y los
    /// eventos sueltos de <see cref="DevProbe.Event"/>.
    /// </summary>
    public sealed class DevProbeTests
    {
        [Test]
        public void BoardFields_MechHiddenSealedCounter_Synthetic()
        {
            // 0: turbia (capas 0 y 1 ocultas); 1: Hoja de la fruta 4; 2-3: pareja de Remolino; 4: sellada; 5: Lluvia (agua 10,
            // 2 gotas, k = 3); 6: Liana de cuenta (n = 1).
            BoardState initial = BoardDiffTests.Build(
                new[] { new[] { 5, 6, 1 }, new int[0], new int[0], new[] { 2 }, new[] { 7, 7, 7, 7 }, new[] { 10, 10 }, new[] { 8 } },
                new[]
                {
                    MechanicSpec.None, new MechanicSpec(MechanicType.Hoja, key: 4), new MechanicSpec(MechanicType.Remolino, partner: 3),
                    new MechanicSpec(MechanicType.Remolino, partner: 2), MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 3),
                    new MechanicSpec(MechanicType.Liana, n: 1),
                },
                new[] { new[] { 0, 0 }, new[] { 0, 1 } });

            var sb = new StringBuilder();
            DevProbe.AppendBoardFields(sb, initial, initial);
            string fields = sb.ToString();
            Assert.That(fields, Does.StartWith(",\"murky\":true"));
            Assert.That(fields, Does.Contain("\"mech\":[\"-\",\"hoja;key=4;closed\",\"remolino;pair=0\",\"remolino;pair=0\",\"-\",\"lluvia;key=10;n=2;k=3\",\"liana;n=1\"]"));
            Assert.That(fields, Does.Contain("\"hidden\":[3,0,0,0,0,0,0]"));
            Assert.That(fields, Does.Contain("\"sealed\":[4]"));
            Assert.That(fields, Does.Contain("\"counter\":{\"type\":\"lluvia\",\"cuia\":5,\"remaining\":3,\"pending\":2,\"blocked\":false}"));
            AssertJsonObject(fields);

            // Por el Remolino: la cima de la 0 se va, la capa 1 se revela (R7) y el contador de la Lluvia avanza.
            BoardState current = PourRules.Apply(initial, 0, 2, PourMode.Run);
            sb.Clear();
            DevProbe.AppendBoardFields(sb, initial, current);
            fields = sb.ToString();
            Assert.That(fields, Does.StartWith(",\"murky\":true"), "el nivel sigue siendo turbio");
            Assert.That(fields, Does.Contain("\"lluvia;key=10;n=2;k=3;c=1\""));
            Assert.That(fields, Does.Contain("\"hidden\":[1,0,0,0,0,0,0]"));
            Assert.That(fields, Does.Contain("\"remaining\":2"));
            AssertJsonObject(fields);

            // Sin mecánicas ni ocultas: todo "-", nada sellado y sin contador.
            BoardState plain = BoardDiffTests.Build(new[] { new[] { 1, 2 }, new int[0] }, null);
            sb.Clear();
            DevProbe.AppendBoardFields(sb, plain, plain);
            Assert.That(sb.ToString(), Is.EqualTo(",\"murky\":false,\"mech\":[\"-\",\"-\"],\"hidden\":[0,0],\"sealed\":[]"));
        }

        [Test]
        public void PourEventsFields_Synthetic()
        {
            // Por el Remolino con revelado: recibe la pareja (3) y se revela la nueva cima del origen (0).
            BoardState initial = BoardDiffTests.Build(
                new[] { new[] { 5, 6, 1 }, new int[0], new int[0], new[] { 2 } },
                new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Remolino, partner: 3), new MechanicSpec(MechanicType.Remolino, partner: 2) },
                new[] { new[] { 0, 0 }, new[] { 0, 1 } });
            PourRules.Apply(initial, 0, 2, PourMode.Run, out PourEvents ev);
            var sb = new StringBuilder();
            DevProbe.AppendPourEvents(sb, in ev);
            Assert.That(sb.ToString(), Is.EqualTo(
                ",\"recv\":3,\"ev\":{\"rev\":1,\"mud\":0,\"open\":0,\"seal\":0,\"rel\":0,\"drop\":0,\"wait\":0,\"end\":0,\"bite\":0,\"cave\":0,\"lost\":0,\"futile\":false}"));
            AssertJsonObject(sb.ToString());

            // Sellado que suelta una Liana con fruta (02 §9.1 M3).
            BoardState liana = BoardDiffTests.Build(
                new[] { new[] { 3 }, new[] { 3, 3, 3 }, new[] { 1, 1 }, new[] { 1, 1 } },
                new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, key: 3), MechanicSpec.None });
            PourRules.Apply(liana, 0, 1, PourMode.Run, out PourEvents sealing);
            sb.Clear();
            DevProbe.AppendPourEvents(sb, in sealing);
            Assert.That(sb.ToString(), Does.StartWith(",\"recv\":1,"));
            Assert.That(sb.ToString(), Does.Contain("\"seal\":2,\"rel\":4,"));

            // Mordisco de la Piraña: la víctima es el receptor (0) y la cueva, la 4.
            BoardState pirana = BoardDiffTests.Build(
                new[] { new[] { 1, 2 }, new[] { 2 }, new[] { 3, 1 }, new int[0], new int[0] },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Pirana, k: 2) });
            BoardState once = PourRules.Apply(pirana, 0, 1, PourMode.Run);
            PourRules.Apply(once, 2, 0, PourMode.Run, out PourEvents bite);
            sb.Clear();
            DevProbe.AppendPourEvents(sb, in bite);
            Assert.That(sb.ToString(), Does.Contain("\"bite\":1,\"cave\":16,\"lost\":0"));
        }

        [Test]
        public void FormatState_Tutorial_NoMechanics()
        {
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor y en builds de desarrollo");
            var flow = new LevelFlow(TestPacks.Tutorial, new MemoryProgressStore(), new RecordingListener());
            flow.Load(2);
            var probe = new DevProbe();

            string? load = probe.FormatState("load", flow, int.MinValue, null);
            Assert.That(load, Is.Not.Null);
            Assert.That(load, Does.Contain("\"cuias\":[[4,8,4,4],[3,3,3,4],[3,8,8,8],[],[]],\"world\":1,\"water\":\"white\",\"label\":\"normal\""), "los campos nuevos van tras los de siempre");
            Assert.That(load, Does.Contain("\"murky\":false"));
            Assert.That(load, Does.Contain("\"mech\":[\"-\",\"-\",\"-\",\"-\",\"-\"]"));
            Assert.That(load, Does.Contain("\"hidden\":[0,0,0,0,0]"));
            Assert.That(load, Does.Contain("\"sealed\":[]"));
            Assert.That(load, Does.Not.Contain("\"counter\""));
            Assert.That(load, Does.Not.Contain("\"recv\""));
            AssertStateLine(load!);

            flow.TapAnywhere();
            flow.TapCuia(0);
            TapResult r = flow.TapCuia(3);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured));
            string? pour = probe.FormatState("pour", flow, 3, r.Pour);
            Assert.That(pour, Does.Contain("\"move\":[0,3,2],\"amount\":2"), "move y amount como siempre");
            Assert.That(pour, Does.Contain("\"recv\":3,\"ev\":{\"rev\":0,"));
            Assert.That(pour, Does.EndWith("\"futile\":false}}"));
            AssertStateLine(pour!);
        }

        [Test]
        public void Event_WritesLine()
        {
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor y en builds de desarrollo");
            var probe = new DevProbe();
            string? line = probe.FormatEvent("step_budget", "\"planned_ms\":3100,\"left_ms\":2800");
            Assert.That(line, Does.StartWith("AF_STATE {\"event\":\"step_budget\",\"t\":"));
            Assert.That(line, Does.EndWith(",\"planned_ms\":3100,\"left_ms\":2800}"));
            AssertStateLine(line!);

            // Con llaves o sin campos.
            Assert.That(probe.FormatEvent("x", "{\"a\":1}"), Does.EndWith(",\"a\":1}"));
            AssertStateLine(probe.FormatEvent("x", "{\"a\":1}")!);
            Assert.That(probe.FormatEvent("x", null), Does.Match("^AF_STATE \\{\"event\":\"x\",\"t\":\\d+\\}$"));
            AssertStateLine(probe.FormatEvent("x", "  ")!);

            LogAssert.Expect(LogType.Log, new Regex("^AF_STATE \\{\"event\":\"step_budget\",\"t\":\\d+,\"planned_ms\":3100\\}$"));
            probe.Event("step_budget", "\"planned_ms\":3100");
        }

        // La línea es "AF_STATE " seguido de un objeto JSON válido (RFC 8259).
        private static void AssertStateLine(string line)
        {
            Assert.That(line, Does.StartWith("AF_STATE {"));
            JsonValue value = JsonParser.Parse(line.Substring("AF_STATE ".Length));
            Assert.That(value.Kind, Is.EqualTo(JsonKind.Object), line);
        }

        // Campos sueltos (",\"a\":1,...") que forman un objeto JSON válido.
        private static void AssertJsonObject(string fields)
        {
            JsonValue value = JsonParser.Parse("{\"_\":0" + fields + "}");
            Assert.That(value.Kind, Is.EqualTo(JsonKind.Object), fields);
        }

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

        [Test]
        public void VictoryLine_CarriesTheHarvest_AndTheBalance()
        {
            // B2-4 y B2-6: AF_STATE de la victoria con la Cosecha y el saldo (semillas y canoas).
            var rig = new LevelStateTests.Rig(offset: 10, seeds: 5);
            rig.Start(11);
            LevelStateTests.Win(rig.Flow);
            rig.Flow.Tick(16, true);
            rig.Flow.TapAnywhere();
            string? line = new DevProbe().FormatVictory("victory", rig.Flow, true);
            Assert.That(line, Does.StartWith("AF_STATE {\"event\":\"victory\""));
            Assert.That(line, Does.Contain("\"level\":11"));
            Assert.That(line, Does.Contain("\"phase\":\"VICTORY_SCREEN\""));
            Assert.That(line, Does.Contain("\"harvest\":20"));
            Assert.That(line, Does.Contain("\"double_offered\":true"));
            Assert.That(line, Does.Contain("\"seeds\":25"));
            Assert.That(line, Does.Contain("\"canoes\":5"));
            Assert.That(line, Does.EndWith("}"));
        }

        [Test]
        public void AdLine_CarriesItsFormatPlacementOutcome_AndTheBalance()
        {
            var economy = new EconomyFixture(seeds: 300);
            var presenter = new AF.Ads.ImmediateAdPresenter();
            var service = new AF.Ads.FakeAdService(presenter);
            service.Show(AF.Ads.AdPlacement.InterstitialPostLevel, _ => { });
            string? line = new DevProbe().FormatAd("ad", presenter.Last!, "shown", 24, economy.Wallet);
            Assert.That(line, Does.StartWith("AF_STATE {\"event\":\"ad\""));
            Assert.That(line, Does.Contain("\"format\":\"interstitial\""));
            Assert.That(line, Does.Contain("\"placement\":\"interstitial_post_level\""));
            Assert.That(line, Does.Contain("\"outcome\":\"shown\""));
            Assert.That(line, Does.Contain("\"level\":24"));
            Assert.That(line, Does.Contain("\"seeds\":300"));
            Assert.That(line, Does.Contain("\"canoes\":5"));

            service.Show(AF.Ads.AdPlacement.DoubleHarvest, _ => { });
            Assert.That(new DevProbe().FormatAd("ad_closed", presenter.Last!, "completed", 24, economy.Wallet), Does.Contain("\"format\":\"rewarded\""));
        }

        private sealed class NullAppListener : IAppFlowListener
        {
            public void OnAppScreenChanged(AppScreen screen)
            {
            }
        }
    }
}
