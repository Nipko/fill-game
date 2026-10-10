#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Levels.Json;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// <c>AF_STATE</c> con los niveles reales (tarea T06 del paso 1; docs/dev/prototipo-v0.md c; arquitectura del paso 1, §9):
    /// completa lo que <see cref="DevProbeTests"/> (T04) prueba con tableros sintéticos. Con <see cref="LevelFlow"/> sobre el
    /// catálogo de la escena (<see cref="TestPacks.CampaignPaths"/>, niveles 1-200), la línea de carga lleva la Hoja del 43, las
    /// ocultas del 100, la pareja del Remolino del 103, el contador de la Lluvia del 143 y el mundo y el agua de cada nivel.
    /// </summary>
    public sealed class DevProbeCampaignTests
    {
        [Test]
        public void Hoja43_MechField()
        {
            // D-007: estreno de la Hoja en el 43, en la cuia 1, con la fruta 4 (02 §9.1 M1) y puesta (cerrada).
            LevelFlow flow = Load(43);
            JsonValue line = LoadLine(flow, out string text);
            List<string> mech = Mech(line);
            Assert.That(mech.Count, Is.EqualTo(flow.Session.Current.Count));
            for (int i = 0; i < mech.Count; i++)
            {
                Assert.That(mech[i], Is.EqualTo(i == 1 ? "hoja;key=4;closed" : "-"), "cuia " + i);
            }

            Assert.That(text, Does.Contain("\"mech\":[\"-\",\"hoja;key=4;closed\",\"-\""));
            Assert.That(line.Get("murky")!.AsBool(), Is.False);
            Assert.That(text, Does.Not.Contain("\"counter\""));
        }

        [Test]
        public void Murky100_HiddenField()
        {
            // D-007: estreno de Aguas turbias en el 100 (02 §11: 1 cuia y 2 ocultas, capas 0 y 1 de la cuia 0).
            LevelFlow flow = Load(100);
            JsonValue line = LoadLine(flow, out string text);
            Assert.That(line.Get("murky")!.AsBool(), Is.True);
            List<int> hidden = Ints(line, "hidden");
            Assert.That(hidden.Count, Is.EqualTo(10));
            Assert.That(hidden[0], Is.EqualTo(0b11), "2 bits en la cuia 0");
            for (int i = 1; i < hidden.Count; i++)
            {
                Assert.That(hidden[i], Is.EqualTo(0), "cuia " + i);
            }

            Assert.That(text, Does.Contain("\"murky\":true,\"mech\":["));
            Assert.That(text, Does.Contain("\"hidden\":[3,0,0,0,0,0,0,0,0,0]"));

            // 0 → 1 revela la capa 1 (R7): la línea del vertido lo dice en hidden y en ev.rev (bit 0 = cuia 0).
            flow.TapAnywhere();
            flow.TapCuia(0);
            TapResult r = flow.TapCuia(1);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured));
            string? pour = new DevProbe().FormatState("pour", flow, 1, r.Pour);
            Assert.That(pour, Is.Not.Null);
            Assert.That(pour, Does.Contain("\"hidden\":[1,0,0,0,0,0,0,0,0,0]"));
            Assert.That(pour, Does.Contain("\"ev\":{\"rev\":1,"));
            Assert.That(Parse(pour!).Get("murky")!.AsBool(), Is.True, "el nivel sigue siendo turbio");
        }

        [Test]
        public void Remolino103_PairField()
        {
            // D-007: estreno del Remolino en el 103: la pareja de las cuias 2 y 8 lleva el mismo pair (02 §9.1 M4).
            LevelFlow flow = Load(103);
            List<string> mech = Mech(LoadLine(flow, out _));
            int pair = -1;
            for (int i = 0; i < mech.Count; i++)
            {
                if (i != 2 && i != 8)
                {
                    Assert.That(mech[i], Is.EqualTo("-"), "cuia " + i);
                    continue;
                }

                StringAssert.StartsWith("remolino;pair=", mech[i], "cuia " + i);
                int id = int.Parse(mech[i].Substring("remolino;pair=".Length), System.Globalization.CultureInfo.InvariantCulture);
                if (pair < 0)
                {
                    pair = id;
                }

                Assert.That(id, Is.EqualTo(pair), "las dos cuias del Remolino, la misma pareja");
            }

            Assert.That(pair, Is.GreaterThanOrEqualTo(0));
            Assert.That(flow.Session.Current[2].MechanicPairId, Is.EqualTo(pair));
            Assert.That(flow.Session.Current[8].MechanicPairId, Is.EqualTo(pair));
        }

        [Test]
        public void Lluvia143_CounterField()
        {
            // D-007: estreno de la Lluvia en el 143: cuia 2, agua 10, 2 gotas y una cada 3 vertidos útiles (02 §9.1 M6).
            LevelFlow flow = Load(143);
            JsonValue line = LoadLine(flow, out string text);
            Assert.That(Mech(line)[2], Is.EqualTo("lluvia;key=10;n=2;k=3"));
            JsonValue counter = line.Get("counter")!;
            Assert.That(counter.Get("type")!.AsString(), Is.EqualTo("lluvia"));
            Assert.That(counter.Get("cuia")!.AsInt(), Is.EqualTo(2));
            Assert.That(counter.Get("remaining")!.AsInt(), Is.EqualTo(3));
            Assert.That(counter.Get("pending")!.AsInt(), Is.EqualTo(2));
            Assert.That(counter.Get("blocked")!.AsBool(), Is.False);
            Assert.That(text, Does.Contain("\"counter\":{\"type\":\"lluvia\",\"cuia\":2,\"remaining\":3,\"pending\":2,\"blocked\":false}"));
        }

        [Test]
        public void World3_WaterBlack()
        {
            // Glosario §8 y 07 §2.1: el mundo 3 (41-80, Manaos) va con agua negra; el 2 (11-40), clara; el 1 (tutorial), blanca.
            for (int level = 41; level <= 80; level += 39)
            {
                JsonValue line = LoadLine(Load(level), out string text);
                Assert.That(line.Get("world")!.AsInt(), Is.EqualTo(3), "nivel " + level);
                Assert.That(line.Get("water")!.AsString(), Is.EqualTo("black"), "nivel " + level);
                Assert.That(text, Does.Contain("\"world\":3,\"water\":\"black\""));
            }

            Assert.That(LoadLine(Load(40), out _).Get("water")!.AsString(), Is.EqualTo("clear"));
            Assert.That(LoadLine(Load(1), out _).Get("water")!.AsString(), Is.EqualTo("white"));
        }

        private static LevelFlow Load(int level)
        {
            var flow = new LevelFlow(LevelCatalog.Create(TestPacks.Campaign), new MemoryProgressStore(), new RecordingListener());
            flow.Load(level);
            Assert.That(flow.LevelNumber, Is.EqualTo(level));
            return flow;
        }

        // Línea "load" de la sonda, como objeto JSON (RFC 8259) y como texto.
        private static JsonValue LoadLine(LevelFlow flow, out string text)
        {
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor y en builds de desarrollo");
            string? line = new DevProbe().FormatState("load", flow, int.MinValue, null);
            Assert.That(line, Is.Not.Null);
            text = line!;
            JsonValue value = Parse(text);
            Assert.That(value.Get("level")!.AsInt(), Is.EqualTo(flow.LevelNumber));
            return value;
        }

        private static JsonValue Parse(string line)
        {
            Assert.That(line, Does.StartWith("AF_STATE {"));
            JsonValue value = JsonParser.Parse(line.Substring("AF_STATE ".Length));
            Assert.That(value.Kind, Is.EqualTo(JsonKind.Object), line);
            return value;
        }

        private static List<string> Mech(JsonValue line)
        {
            var mech = new List<string>();
            IReadOnlyList<JsonValue> items = line.Get("mech")!.Items;
            for (int i = 0; i < items.Count; i++)
            {
                mech.Add(items[i].AsString());
            }

            return mech;
        }

        private static List<int> Ints(JsonValue line, string field)
        {
            var values = new List<int>();
            IReadOnlyList<JsonValue> items = line.Get(field)!.Items;
            for (int i = 0; i < items.Count; i++)
            {
                values.Add(items[i].AsInt());
            }

            return values;
        }
    }
}
