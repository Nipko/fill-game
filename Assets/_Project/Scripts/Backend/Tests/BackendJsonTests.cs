#nullable enable
using System;
using AF.Backend.Json;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>F3-1: el lector y el escritor de JSON propios de <c>AF.Backend</c>.</summary>
    public sealed class BackendJsonTests
    {
        [Test]
        public void WritesCompactAsciiInInsertionOrder()
        {
            JsonNode node = JsonNode.NewObject()
                .Set("b", JsonNode.FromString("ñ\"\n"))
                .Set("a", JsonNode.NewArray().Add(JsonNode.FromLong(-3)).Add(JsonNode.FromBool(false)).Add(JsonNode.Null));
            Assert.That(BackendJson.Write(node), Is.EqualTo("{\"b\":\"\\u00f1\\\"\\n\",\"a\":[-3,false,null]}"));
        }

        [Test]
        public void ParsesAndComparesStructurally()
        {
            JsonNode a = BackendJson.Parse("\uFEFF { \"x\": [1, 2.0, \"\\u00e9\"], \"y\": {\"z\": true} } ");
            JsonNode b = BackendJson.Parse("{\"y\":{\"z\":true},\"x\":[1,2,\"é\"]}");
            Assert.That(JsonNode.StructurallyEqual(a, b), Is.True);
            Assert.That(JsonNode.StructurallyEqual(a, BackendJson.Parse("{\"y\":{\"z\":true},\"x\":[1,2]}")), Is.False);
        }

        [Test]
        public void RejectsMalformedJson()
        {
            foreach (string bad in new[] { "", "{", "{\"a\":1,}", "{\"a\":1,\"a\":2}", "[01]", "\"x", "{} {}", "nul", "{\"a\":\"\u0001\"}" })
            {
                Assert.That(() => BackendJson.Parse(bad), Throws.InstanceOf<FormatException>(), bad);
            }
        }

        [Test]
        public void RejectsDeepNesting()
        {
            string deep = new string('[', 40) + new string(']', 40);
            Assert.That(() => BackendJson.Parse(deep), Throws.InstanceOf<FormatException>());
        }
    }
}
