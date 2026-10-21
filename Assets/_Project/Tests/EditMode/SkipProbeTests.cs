#nullable enable
using System.IO;
using AF.Economy;
using AF.GameFlow;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Formato de la sonda <c>AF_STATE af_skip_used</c> (bloque B3; 04 §11 y 08 §8.2: <c>fails_level</c>, <c>since_last_skip</c>,
    /// <c>level_tag</c>) y claves del Salto en <see cref="SkipText"/> (las de <c>B3-strings.csv</c>).
    /// </summary>
    public sealed class SkipProbeTests
    {
        [Test]
        public void SkipUsed_CarriesTheParametersOf04_11()
        {
            var result = new SkipResult(36, 5, 36, "hard", default(TransactionResult), null);
            string line = DevProbe.FormatSkipUsed(1234, result, 700);
            Assert.That(
                line,
                Is.EqualTo("AF_STATE {\"event\":\"af_skip_used\",\"t\":1234,\"level\":36,\"fails_level\":5,\"since_last_skip\":36,\"level_tag\":\"hard\",\"seeds\":700}"));
        }

        [Test]
        public void Keys_AreSnakeCaseAndOneMeaningEach()
        {
            string[] keys =
            {
                SkipText.Keys.Option, SkipText.Keys.Tagline, SkipText.Keys.ConfirmTitle, SkipText.Keys.ConfirmExtraCuia,
                SkipText.Keys.ConfirmCta, SkipText.Keys.Banner,
            };
            Assert.That(keys, Is.Unique);
            foreach (string key in keys)
            {
                StringAssert.IsMatch("^[a-z][a-z0-9_]*$", key);
            }

            Assert.That(SkipText.Keys.Option, Is.EqualTo("stuck_skip"), "la clave de 04 §6.5");

            // Todas en el CSV del enganche, con la cabecera de strings.csv, su texto en es-419 y pendientes del asesor.
            string csv = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "docs", "dev", "traspaso", "spark-enganches", "B3-strings.csv");
            string[] lines = File.ReadAllText(csv).Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            Assert.That(lines[0], Is.EqualTo("key,es-419,en,pt-BR,context,max_chars,status,status_en,status_pt-BR,notes,args"));
            Assert.That(lines.Length, Is.EqualTo(keys.Length + 1));
            foreach (string key in keys)
            {
                string? row = null;
                for (int i = 1; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith(key + ",", System.StringComparison.Ordinal))
                    {
                        row = lines[i];
                    }
                }

                Assert.That(row, Is.Not.Null, key + " falta en B3-strings.csv");
                StringAssert.Contains(",pendiente_asesor,", row, key);
            }
        }
    }
}
