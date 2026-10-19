#nullable enable
using System.Collections.Generic;
using AF.Audio;
using AF.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Peso del AAB del juego por grupo (tarea B2): cada entrada del Build Report cae en un solo grupo, los assets se cuentan
    /// una vez por grupo y ruta (y una vez por ruta en el total) y los MB suman todas sus entradas.
    /// </summary>
    public sealed class AabSizeReportTests
    {
        private static int Index(string group)
        {
            return System.Array.IndexOf(AabSizeReport.Groups, group);
        }

        [Test]
        public void GroupOf_ClassifiesByPathAndType()
        {
            Assert.That(AabSizeReport.GroupOf("Assets/_Project/Data/Levels/mundo_06_v1.json", typeof(TextAsset)),
                Is.EqualTo(Index("Niveles (packs)")));
            Assert.That(AabSizeReport.GroupOf("Assets/_Project/Data/Loc/strings.csv", typeof(TextAsset)),
                Is.EqualTo(Index("Textos, Mapa y datos")));
            Assert.That(AabSizeReport.GroupOf("Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF.asset", typeof(Texture2D)),
                Is.EqualTo(Index("Fuentes")), "el atlas de una fuente cuenta como fuente, no como arte");
            Assert.That(AabSizeReport.GroupOf("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", typeof(Material)),
                Is.EqualTo(Index("Fuentes")));
            Assert.That(AabSizeReport.GroupOf(AudioClipIds.Folder + "music/w01_bed.wav", typeof(AudioClip)),
                Is.EqualTo(Index("Audio")));
            Assert.That(AabSizeReport.GroupOf("Assets/_Project/Art/Generated/bg_kit_w01.png", typeof(Texture2D)),
                Is.EqualTo(Index("Arte")));
            Assert.That(AabSizeReport.GroupOf("Assets/TextMesh Pro/Sprites/EmojiOne.png", typeof(Texture2D)),
                Is.EqualTo(Index("Arte")));
            Assert.That(AabSizeReport.GroupOf("Assets/_Project/Shaders/Liquid.shadergraph", typeof(Shader)),
                Is.EqualTo(Index("Shaders y materiales")));
            Assert.That(AabSizeReport.GroupOf("Assets/_Project/Scenes/Game.unity", typeof(GameObject)),
                Is.EqualTo(Index("Escenas y scripts")));
            Assert.That(AabSizeReport.GroupOf("Packages/com.unity.render-pipelines.universal/Shaders/Sprite.shader", typeof(Shader)),
                Is.EqualTo(Index("Paquetes")));
            Assert.That(AabSizeReport.GroupOf("Resources/unity_builtin_extra", typeof(Shader)),
                Is.EqualTo(Index("Recursos integrados de Unity")));
            Assert.That(AabSizeReport.GroupOf(string.Empty, null), Is.EqualTo(Index("Recursos integrados de Unity")));
            Assert.That(AabSizeReport.GroupOf("Assets/Settings/UniversalRP.asset", typeof(ScriptableObject)),
                Is.EqualTo(Index("Otros")));
        }

        [Test]
        public void Report_SumsBytesPerGroup_AndCountsEachAssetOnce()
        {
            var contents = new List<PackedEntry>
            {
                new PackedEntry("Assets/_Project/Data/Levels/mundo_06_v1.json", typeof(TextAsset), 300000),
                new PackedEntry("Assets/_Project/Data/Levels/mundo_07_v1.json", typeof(TextAsset), 200000),
                new PackedEntry(AudioClipIds.Folder + "sfx/sfx_seal.wav", typeof(AudioClip), 1000000),
                new PackedEntry(AudioClipIds.Folder + "sfx/sfx_seal.wav", typeof(AudioClip), 2000000),
                new PackedEntry("Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF.asset", typeof(Texture2D), 1500000),
                new PackedEntry("Resources/unity_builtin_extra", typeof(Shader), 5000000),
            };

            string report = AabSizeReport.Report(contents, 40000000);

            Assert.That(report, Does.Contain("| Niveles (packs) | 2 | 0.50 | 5.0 |"));
            Assert.That(report, Does.Contain("| Audio | 1 | 3.00 | 30.0 |"));
            Assert.That(report, Does.Contain("| Fuentes | 1 | 1.50 | 15.0 |"));
            Assert.That(report, Does.Contain("| Recursos integrados de Unity | 1 | 5.00 | 50.0 |"));
            Assert.That(report, Does.Contain("| Arte | 0 | 0.00 | 0.0 |"));
            Assert.That(report, Does.Contain("| **Datos empaquetados** | 5 | **10.00** | 100.0 |"));
            Assert.That(report, Does.Contain("AAB: 40.00 MB"));
        }

        [Test]
        public void Report_ListsTheFontsByAssetAndType_HeaviestFirst()
        {
            // FNT1: el desglose del grupo «Fuentes» suma las entradas de cada asset y tipo, de más a menos peso, sin otros grupos.
            var contents = new List<PackedEntry>
            {
                new PackedEntry("Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF.asset", typeof(Material), 2000),
                new PackedEntry("Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF.asset", typeof(Texture2D), 786432),
                new PackedEntry("Assets/TextMesh Pro/Fonts/LiberationSans.ttf", typeof(Font), 350000),
                new PackedEntry("Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF - Plain.mat", typeof(Material), 1000),
                new PackedEntry("Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF - Plain.mat", typeof(Material), 700),
                new PackedEntry("Assets/_Project/Data/Loc/strings.csv", typeof(TextAsset), 900000),
            };

            string report = AabSizeReport.Report(contents, 10000000);
            string fonts = report.Substring(report.IndexOf("## Fuentes por asset", System.StringComparison.Ordinal));

            Assert.That(fonts, Is.EqualTo("## Fuentes por asset\n\n| Asset | Tipo | MB |\n| --- | --- | ---: |\n"
                + "| Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF.asset | Texture2D | 0.786 |\n"
                + "| Assets/TextMesh Pro/Fonts/LiberationSans.ttf | Font | 0.350 |\n"
                + "| Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF.asset | Material | 0.002 |\n"
                + "| Assets/_Project/Resources/AF/Fonts/Nunito-Regular SDF - Plain.mat | Material | 0.002 |\n"));
        }

        [Test]
        public void Report_CountsAssetInEachGroupItSpans_AndSkipsEntriesWithoutPath()
        {
            var contents = new List<PackedEntry>
            {
                // Un mismo .asset con una textura (Arte) y un ScriptableObject (Otros): reparte MB y cuenta en los dos grupos.
                new PackedEntry("Assets/Settings/Atlas.asset", typeof(Texture2D), 3000000),
                new PackedEntry("Assets/Settings/Atlas.asset", typeof(ScriptableObject), 1000000),
                // Entradas sin ruta: suman MB en los recursos integrados, pero no son assets.
                new PackedEntry(string.Empty, typeof(Shader), 500000),
                new PackedEntry(string.Empty, null, 500000),
            };

            string report = AabSizeReport.Report(contents, 10000000);

            Assert.That(report, Does.Contain("| Arte | 1 | 3.00 | 60.0 |"));
            Assert.That(report, Does.Contain("| Otros | 1 | 1.00 | 20.0 |"));
            Assert.That(report, Does.Contain("| Recursos integrados de Unity | 0 | 1.00 | 20.0 |"));
            Assert.That(report, Does.Contain("| **Datos empaquetados** | 1 | **5.00** | 100.0 |"));
        }
    }
}
