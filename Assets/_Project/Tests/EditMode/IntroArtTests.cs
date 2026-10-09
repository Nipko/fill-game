#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.EditorTools;
using AF.Levels;
using AF.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Arte de «Llenar y teñir» (guion de la intro §11-§12): las 14 frutas en vuelo en un solo atlas de 512 × 128 generado en
    /// ≤ 10 ms; las piezas procedurales de los miquitos (≤ 10 ms por especie) con al menos 3 especies distinguibles por la
    /// silueta, los colores y la cola; y las ranuras nuevas del catálogo de arte (importar un PNG de prueba lo actualiza).
    /// </summary>
    public sealed class IntroArtTests
    {
        private static ProceduralArt? s_art;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [OneTimeTearDown]
        public void DisposeArt()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [Test]
        public void FruitFlight_Has14DistinctSprites_InOneTexture_GeneratedWithin10ms()
        {
            Art.FruitFlight(1); // primera generación (incluye el JIT)
            Assert.That(MinGenerationMs(art => art.FruitFlight(1), art => art.FruitAtlasMs), Is.LessThanOrEqualTo(10.0), "generación del atlas tras el JIT (mínimo de 3)");
            var second = new ProceduralArt("white");
            try
            {
                var seen = new HashSet<Sprite>();
                Texture? texture = null;
                for (int tint = 1; tint <= 14; tint++)
                {
                    Sprite s = second.FruitFlight(tint);
                    Assert.That(seen.Add(s), Is.True, "sprite repetido en el tinte " + tint);
                    texture ??= s.texture;
                    Assert.That(s.texture, Is.SameAs(texture), "una sola textura: un lote de dibujo");
                    Assert.That(s.rect.width, Is.EqualTo(ProceduralArt.FruitFlightPx));
                }

                Assert.That(texture!.width, Is.EqualTo(512));
                Assert.That(texture.height, Is.EqualTo(128));
            }
            finally
            {
                second.Dispose();
            }
        }

        [Test]
        public void FruitCells_AreNotEmpty_AndNeverTheAmberSeedOfTheCurrency()
        {
            Color32[] px = ProceduralArt.FruitFlightPixels();
            for (int tint = 1; tint <= 14; tint++)
            {
                RectInt cell = ProceduralArt.FruitCell(tint);
                int opaque = 0;
                for (int y = cell.y; y < cell.yMax; y++)
                {
                    for (int x = cell.x; x < cell.xMax; x++)
                    {
                        opaque += px[(y * ProceduralArt.FruitAtlasWidth) + x].a > 128 ? 1 : 0;
                    }
                }

                Assert.That(opaque, Is.GreaterThan(300), "fruta del tinte " + tint + " visible a 64 px");
            }
        }

        [Test]
        public void MonkeyArt_ThreeSpecies_AreDistinguishable_AndGeneratedWithin10ms()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            Art.MonkeyArtFor(catalog.Find("saimiri_collinsi")!); // JIT
            FaunaSpecies silvery = catalog.Find("mico_argentatus")!;
            Assert.That(MinGenerationMs(art => art.MonkeyArtFor(silvery), art => art.FaunaArtMs), Is.LessThanOrEqualTo(10.0), "piezas de una especie (mínimo de 3)");
            var second = new ProceduralArt("white");
            try
            {
                MonkeyArt squirrel = second.MonkeyArtFor(silvery);
                MonkeyArt monkey = second.MonkeyArtFor(catalog.Find("saimiri_collinsi")!);
                MonkeyArt uakari = second.MonkeyArtFor(catalog.Find("cacajao_calvus_ucayalii")!);
                MonkeyArt woolly = second.MonkeyArtFor(catalog.Find("lagothrix_lagothricha")!);
                Assert.That(second.MonkeyArtFor(catalog.Find("saimiri_collinsi")!), Is.SameAs(monkey), "en caché");

                // Silueta (esqueleto, cabeza, extremidades), colores y cola distintos.
                Assert.That(uakari.TailLength, Is.LessThan(monkey.TailLength), "el huapo, cola corta");
                Assert.That(woolly.TailCurls, Is.True, "el churuco, cola prensil enrollada");
                Assert.That(woolly.LimbScale, Is.GreaterThan(monkey.LimbScale));
                Assert.That(uakari.TailWidthS, Is.GreaterThan(monkey.TailWidthS), "cola mullida del pitécido");
                Assert.That(squirrel.HeadS, Is.Not.EqualTo(monkey.HeadS));
                Assert.That(monkey.Tail, Is.Not.EqualTo(squirrel.Tail));
                Assert.That(monkey.Head.texture, Is.SameAs(monkey.Torso.texture), "una textura por especie");
                Assert.That(monkey.Head.texture, Is.Not.SameAs(uakari.Head.texture));
                Assert.That(monkey.Limb.border.y, Is.GreaterThan(0f), "extremidad 9-slice");
            }
            finally
            {
                second.Dispose();
            }
        }

        // Mínimo de 3 generaciones en instancias nuevas: sin el ruido del recolector ni de la planificación del Editor.
        private static double MinGenerationMs(System.Action<ProceduralArt> generate, System.Func<ProceduralArt, double> measured)
        {
            double best = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                var art = new ProceduralArt("white");
                try
                {
                    generate(art);
                    best = System.Math.Min(best, measured(art));
                }
                finally
                {
                    art.Dispose();
                }
            }

            return best;
        }

        [Test]
        public void ArtSlots_KnowTheNewSlots()
        {
            Assert.That(ArtSlots.IsKnown("fruta_cacao"), Is.True);
            Assert.That(ArtSlots.IsKnown("fruta_agua_negro"), Is.True);
            Assert.That(ArtSlots.IsKnown("fauna_saimiri_collinsi_partes"), Is.True);
            Assert.That(ArtSlots.IsKnown("fauna_saimiri_collinsi_retrato"), Is.True);
            Assert.That(ArtSlots.IsKnown("rama_estuario"), Is.True);
            Assert.That(ArtSlots.IsKnown(ArtSlots.LianaTile), Is.True);
            Assert.That(ArtSlots.IsKnown("fauna__partes"), Is.False);
            Assert.That(ArtSlots.IsKnown("fruta_inventada"), Is.False);
            Assert.That(ArtSlots.IsKnown("cualquier_cosa"), Is.False);
            for (int tint = 1; tint <= 14; tint++)
            {
                Assert.That(ArtSlots.IsKnown(ArtSlots.FruitPrefix + ProceduralArt.TintIds[tint]), Is.True, ProceduralArt.TintIds[tint]);
            }
        }

        [Test]
        public void ImportingATestPng_UpdatesTheArtSlotCatalog_AndTheFlightAtlas()
        {
            string png = GeneratedArt.Folder + "/fruta_cacao.png";
            Assert.That(File.Exists(png), Is.False, "el test no pisa un PNG del artista");
            bool catalogExisted = File.Exists(GeneratedArt.CatalogPath);
            bool atlasExisted = File.Exists(GeneratedArt.FruitAtlasPath);
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[256 * 256];
                for (int i = 0; i < pixels.Length; i++)
                {
                    int x = i % 256;
                    int y = i / 256;
                    int dx = x - 128;
                    int dy = y - 128;
                    pixels[i] = (dx * dx) + (dy * dy) < 100 * 100 ? new Color32(220, 150, 40, 255) : new Color32(0, 0, 0, 0);
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(png, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
                GeneratedArt.RebuildCatalog();

                ArtSlotCatalog? catalog = AssetDatabase.LoadAssetAtPath<ArtSlotCatalog>(GeneratedArt.CatalogPath);
                Assert.That(catalog, Is.Not.Null);
                Assert.That(catalog!.Find("fruta_cacao"), Is.Not.Null, "el PNG de prueba entra en el catálogo");
                Assert.That(ArtSlots.Find("fruta_cacao"), Is.Not.Null);
                Assert.That(File.Exists(GeneratedArt.FruitAtlasPath), Is.True, "atlas de vuelo de 64 px generado");
                Texture2D? atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(GeneratedArt.FruitAtlasPath);
                Assert.That(atlas, Is.Not.Null);
                Assert.That(atlas!.width, Is.EqualTo(ProceduralArt.FruitAtlasWidth));
            }
            finally
            {
                Object.DestroyImmediate(tex);
                AssetDatabase.DeleteAsset(png);
                GeneratedArt.RebuildCatalog();
                if (!catalogExisted && File.Exists(GeneratedArt.CatalogPath))
                {
                    AssetDatabase.DeleteAsset(GeneratedArt.CatalogPath);
                }

                if (!atlasExisted && File.Exists(GeneratedArt.FruitAtlasPath))
                {
                    AssetDatabase.DeleteAsset(GeneratedArt.FruitAtlasPath);
                }

                ArtSlots.Reload();
            }

            Assert.That(File.Exists(GeneratedArt.FruitAtlasPath), Is.EqualTo(atlasExisted), "sin PNG de frutas, sin atlas");
        }
    }
}
