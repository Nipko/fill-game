#nullable enable
using System.IO;
using AF.Levels;
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

        /// <summary>
        /// Atlas de frutas en vuelo y piezas procedurales de cada especie de miquito (guion de la intro §11), con la de la pareja
        /// si el catálogo trae <c>paleta_pareja</c>. La escritura a disco vive aquí (ensamblado de Editor), no en el runtime.
        /// </summary>
        [Test]
        public void ExportIntroArt()
        {
            string dir = Path.Combine(Application.dataPath, "../Logs/art");
            Directory.CreateDirectory(dir);
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            Save(dir, "fruit_flight_atlas", ProceduralArt.FruitFlightPixels(), ProceduralArt.FruitAtlasWidth, ProceduralArt.FruitAtlasHeight);
            int size = ProceduralArt.FaunaPixelsSize;
            foreach (FaunaSpecies species in catalog.Species)
            {
                Save(dir, "fauna_" + species.Id, ProceduralArt.FaunaPixels(species), size, size);
                if (species.PairPalette != null)
                {
                    Save(dir, "fauna_" + species.Id + "_pareja", ProceduralArt.FaunaPixels(species, true), size, size);
                }
            }

            Assert.That(File.Exists(Path.Combine(dir, "fruit_flight_atlas.png")), Is.True);
        }

        /// <summary>La hoja de palma de la Hoja, cerrada y abierta (la que cae), a <c>Logs/art/</c>.</summary>
        [Test]
        public void ExportHojaArt()
        {
            string dir = Path.Combine(Application.dataPath, "../Logs/art");
            Directory.CreateDirectory(dir);
            PixelCanvas palm = ProceduralArt.BuildHojaPalmCanvas();
            PixelCanvas flat = ProceduralArt.DrawHojaFlat();
            Save(dir, "mech_hoja_palm", palm.ToColor32(), palm.Width, palm.Height);
            Save(dir, "mech_hoja_flat", flat.ToColor32(), flat.Width, flat.Height);
            Assert.That(File.Exists(Path.Combine(dir, "mech_hoja_palm.png")), Is.True);
        }

        private static void Save(string dir, string name, Color32[] pixels, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }
    }
}
