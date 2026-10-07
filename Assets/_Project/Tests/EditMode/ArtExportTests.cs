#nullable enable
using System.IO;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Exporta el respaldo procedural de la escena a <c>Logs/art/</c> para revisarlo (no forma parte de la suite:
    /// <c>[Explicit]</c>; guía visual v2, 4.9). <c>unity test &lt;repo&gt; --mode EditMode --filter ArtExportTests</c>.
    /// </summary>
    [Explicit("Exportación manual del arte procedural para revisión.")]
    public sealed class ArtExportTests
    {
        [Test]
        public void ExportSceneFallbacks()
        {
            string dir = Path.Combine(Application.dataPath, "../Logs/art");
            ProceduralArt.ExportSceneFallbacks(dir);
            Assert.That(File.Exists(Path.Combine(dir, "foliage_tr.png")), Is.True);
        }
    }
}
