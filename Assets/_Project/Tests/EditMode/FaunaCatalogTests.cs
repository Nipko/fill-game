#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.Levels;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Catálogo de miquitos <c>af-fauna/0</c> (guion de la intro §4.2-§4.3; A13): el archivo real valida (ids únicos, tramos
    /// sin solapes ni huecos en 1-1.000, <c>nivel_tarjeta</c> dentro de su tramo, colores válidos, todo pendiente del asesor)
    /// y <see cref="IntroCast.Resolve"/> da dosel en el mundo 13, en los reintentos y sin arte, y nunca la especie de otro mundo.
    /// </summary>
    public sealed class FaunaCatalogTests
    {
        private static string RealPath => Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json");

        [Test]
        public void RealCatalog_Validates_AndCoversTheElevenSpeciesOfTheScript()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(RealPath));
            Assert.That(catalog.Species.Count, Is.EqualTo(11), "las especies del guion §4.2 (con la recomendación de Q8)");
            var ids = new HashSet<string>();
            foreach (FaunaSpecies sp in catalog.Species)
            {
                Assert.That(ids.Add(sp.Id), Is.True, "id repetido: " + sp.Id);
                Assert.That(sp.Review, Is.EqualTo("pendiente"), sp.Id + ": pendiente del asesor (07 §8.2)");
                Assert.That(sp.CardLevel, Is.InRange(sp.FromLevel, sp.ToLevel), sp.Id);
                Assert.That(sp.PartsSlot, Is.EqualTo("fauna_" + sp.Id + "_partes"));
                Assert.That(sp.Avoid, Does.Contain("mascota"), sp.Id + ": siempre libre, nunca mascota (guion §4.6)");
                Assert.That(sp.Names.ContainsKey("es"), Is.True);
            }

            // Cobertura 1-1.000 sin huecos: cada nivel tiene especie o está en sin_fauna, y solo uno de los dos.
            for (int level = 1; level <= FaunaCatalog.LastLevel; level++)
            {
                bool species = catalog.ForLevel(level) != null;
                Assert.That(species ^ catalog.IsNoFauna(level), Is.True, "nivel " + level);
            }

            Assert.That(catalog.ForLevel(1)!.Id, Is.EqualTo("saimiri_collinsi"), "protagonista del nivel 1 (guion §4.1)");
            Assert.That(catalog.ForLevel(11)!.Id, Is.EqualTo("mico_argentatus"));
            Assert.That(catalog.ForLevel(41)!.Id, Is.EqualTo("saguinus_bicolor"));
            Assert.That(catalog.IsNoFauna(961), Is.True, "sin miquitos en los Andes");
        }

        [Test]
        public void ThreatenedSpecies_AreMarked_AndTheCatalogNeverUsesTheIucnAsATrophy()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(RealPath));
            Assert.That(catalog.Find("saguinus_bicolor")!.Threatened, Is.True);
            Assert.That(catalog.Find("saguinus_bicolor")!.Iucn, Is.EqualTo("CR"));
            Assert.That(catalog.Find("lagothrix_lagothricha")!.Threatened, Is.True);
            Assert.That(catalog.Find("saimiri_collinsi")!.Threatened, Is.False);
            Assert.That(catalog.Find("saimiri_sciureus_albigena")!.Iucn, Is.Null);
        }

        [Test]
        public void Resolve_GivesTheSpeciesOfTheStretch_OrCanopy()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(RealPath));
            Assert.That(IntroCast.Resolve(catalog, 1, 1, 1, true)!.Id, Is.EqualTo("saimiri_collinsi"));
            Assert.That(IntroCast.Resolve(catalog, 1, 7, 1, true)!.Id, Is.EqualTo("saimiri_collinsi"));
            Assert.That(IntroCast.Resolve(catalog, 2, 25, 1, true)!.Id, Is.EqualTo("mico_argentatus"));
            Assert.That(IntroCast.Resolve(catalog, 9, 600, 1, true)!.Id, Is.EqualTo("ateles_chamek"));

            Assert.That(IntroCast.Resolve(catalog, 13, 970, 1, true), Is.Null, "mundo 13: dosel");
            Assert.That(IntroCast.Resolve(catalog, 1, 7, 2, true), Is.Null, "reintento: dosel");
            Assert.That(IntroCast.Resolve(catalog, 1, 7, 1, false), Is.Null, "sin arte en release: dosel");
            Assert.That(IntroCast.Resolve(null, 1, 7, 1, true), Is.Null, "sin catálogo: dosel");
            Assert.That(IntroCast.Resolve(catalog, 1, 31, 1, true), Is.Null, "nunca la especie de otro mundo (barrera de los ríos)");
            Assert.That(IntroCast.Resolve(catalog, 2, 5, 1, true), Is.Null, "nunca la especie de otro mundo");
        }

        [Test]
        public void Reader_RejectsDuplicates_Overlaps_Gaps_AndBadValues()
        {
            Assert.That(FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 5), Species("b", 501, 1000, 600))).Species.Count, Is.EqualTo(2));
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 5), Species("a", 501, 1000, 600))), "id repetido");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 5), Species("b", 500, 1000, 600))), "solape");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 400, 5), Species("b", 501, 1000, 600))), "hueco");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 5), Species("b", 501, 999, 600))), "no llega a 1.000");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 501), Species("b", 501, 1000, 600))), "tarjeta fuera del tramo");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 5, color: "#12345"), Species("b", 501, 1000, 600))), "color");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 5, color: "#GG0000"), Species("b", 501, 1000, 600))), "color");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 5, skeleton: "E"), Species("b", 501, 1000, 600))), "esqueleto");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(Catalog(Species("a", 1, 500, 5, model: "vuela"), Species("b", 501, 1000, 600))), "modelo");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read("{ \"formato\": \"af-fauna/1\", \"especies\": [] }"), "formato");
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read("no es json"));
        }

        [Test]
        public void ParseColor_ReadsRrggbb()
        {
            Assert.That(FaunaCatalogReader.ParseColor("#8C8A68", "t"), Is.EqualTo(0x8C8A68));
            Assert.That(FaunaCatalogReader.ParseColor("#000000", "t"), Is.EqualTo(0));
        }

        private static string Catalog(string a, string b)
        {
            return "{ \"formato\": \"af-fauna/0\", \"especies\": [" + a + ", " + b + "] }";
        }

        private static string Species(string id, int from, int to, int card, string color = "#8C8A68", string skeleton = "B", string model = "fila")
        {
            return "{ \"id\": \"" + id + "\", \"cientifico\": \"X\", \"mostrar_cientifico\": true, \"nombres\": { \"es\": \"x\" }, "
                + "\"esqueleto\": \"" + skeleton + "\", \"modelo\": \"" + model + "\", \"locomocion\": \"corre\", \"tam_dp\": 40, \"tropa_intro\": 3, "
                + "\"rasgos_reposo\": [], \"paleta\": { \"cuerpo\": \"" + color + "\", \"extremidades\": \"#D7A33E\", \"cara\": \"#F1E9DA\", "
                + "\"hocico\": \"#2B2118\", \"gorra\": \"#8C8A68\", \"punta_cola\": \"#2B2118\" }, \"iluminacion\": \"dia\", \"mundos\": [1], "
                + "\"desde_nivel\": " + from + ", \"hasta_nivel\": " + to + ", \"nivel_tarjeta\": " + card + ", \"uicn\": \"LC\", \"amenazada\": false, "
                + "\"dato_tarjeta\": \"k\", \"ranuras_arte\": { \"partes\": \"fauna_" + id + "_partes\", \"retrato\": \"fauna_" + id + "_retrato\" }, "
                + "\"revision_asesor\": \"pendiente\", \"evitar\": [\"mascota\"] }";
        }
    }
}
