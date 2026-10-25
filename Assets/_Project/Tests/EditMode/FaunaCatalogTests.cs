#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AF.GameFlow;
using AF.Levels;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Catálogo de miquitos <c>af-fauna/0</c> (guion de la intro §4.2-§4.3; A13): el archivo real valida (ids únicos, tramos
    /// sin solapes ni huecos en 1-1.000, <c>nivel_tarjeta</c> dentro de su tramo, colores válidos, mundos según la tabla de
    /// mundos del glosario §8, todo pendiente del asesor); el lector rechaza cada valor fuera del formato; las especies
    /// amenazadas nunca llevan la etiqueta UICN como trofeo (§4.5); <see cref="IntroCast.Resolve"/> da dosel en el mundo 13, en
    /// los reintentos y sin arte, y nunca la especie de otro mundo; y <see cref="FaunaArtRule"/> es la única regla del arte.
    /// </summary>
    public sealed class FaunaCatalogTests
    {
        // Etiquetas de la Lista Roja que no pueden salir en una tarjeta (como palabra suelta, en mayúsculas) y palabras que
        // convierten el avistamiento en un trofeo de amenaza (guion §4.5; R13).
        private static readonly string[] IucnLabels = { "CR", "EN", "VU", "NT", "LC", "DD", "EW", "EX", "UICN", "IUCN" };
        private static readonly string[] ThreatWords = { "peligro", "amenaz", "vulnerable", "crítico", "critico", "extinción", "extincion", "lista roja" };

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

        /// <summary>
        /// Barrera de los ríos (guion §4.2) contra la tabla de mundos (glosario §8): cada especie cubre solo niveles de sus
        /// mundos y vive en todos los que cruza su tramo; así, en cada nivel de su tramo y en su mundo, la intro la usa.
        /// </summary>
        [Test]
        public void RealCatalog_EachSpeciesCoversOnlyLevelsOfItsWorlds()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(RealPath));
            foreach (FaunaSpecies sp in catalog.Species)
            {
                var spanned = new HashSet<int>();
                for (int level = sp.FromLevel; level <= sp.ToLevel; level++)
                {
                    int world = WorldTable.WorldOf(level);
                    spanned.Add(world);
                    Assert.That(sp.LivesIn(world), Is.True, sp.Id + ": el nivel " + level + " es del mundo " + world);
                    Assert.That(IntroCast.Resolve(catalog, world, level, 1, true), Is.SameAs(sp), sp.Id + ", nivel " + level);
                }

                Assert.That(spanned, Is.EquivalentTo(sp.Worlds), sp.Id + ": sus mundos son los que cruza su tramo");
            }

            foreach (FaunaGap gap in catalog.NoFauna)
            {
                Assert.That(WorldTable.WorldOf(gap.FromLevel), Is.EqualTo(13));
                Assert.That(gap.Worlds, Is.EquivalentTo(new[] { 13 }));
            }
        }

        /// <summary>
        /// Guion §4.5 y R13: «a la vista», nunca un trofeo. Las especies amenazadas (CR, EN, VU) están marcadas como tales, y la
        /// línea de su tarjeta es una buena noticia de conservación: ni la etiqueta UICN ni palabras de amenaza. Se mira también
        /// el resto del elenco y el texto de la cabecera de la tarjeta.
        /// </summary>
        [Test]
        public void ThreatenedSpecies_AreMarked_AndTheCatalogNeverUsesTheIucnAsATrophy()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(RealPath));
            Assert.That(catalog.Find("saguinus_bicolor")!.Iucn, Is.EqualTo("CR"));
            Assert.That(catalog.Find("saimiri_sciureus_albigena")!.Iucn, Is.Null);
            int threatened = 0;
            foreach (FaunaSpecies sp in catalog.Species)
            {
                bool listed = sp.Iucn == "CR" || sp.Iucn == "EN" || sp.Iucn == "VU";
                Assert.That(sp.Threatened, Is.EqualTo(listed), sp.Id + ": «amenazada» sigue a la categoría UICN (" + sp.Iucn + ")");
                threatened += sp.Threatened ? 1 : 0;
                string fact = UiText.FaunaFact(sp.FactKey);
                Assert.That(fact.Length, Is.InRange(1, 50), sp.Id + ": una línea de ≤ 50 caracteres");
                AssertNoThreatLabel(fact, sp.Id + " (dato de la tarjeta)");
                AssertNoThreatLabel(sp.NameIn("es"), sp.Id + " (nombre)");
            }

            Assert.That(threatened, Is.EqualTo(4), "sauim (CR), churuco y huapo (VU) y maquisapa (EN)");
            AssertNoThreatLabel(UiText.FaunaSightedCard, "cabecera de la tarjeta");
            Assert.That(UiText.FaunaSightedCard.ToLowerInvariant(), Does.Not.Contain("nuevo"), "avistamiento, nunca adquisición");
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
            Assert.That(IntroCast.Resolve(catalog, 1, 7, 1, false), Is.Null, "sin arte: dosel");
            Assert.That(IntroCast.Resolve(null, 1, 7, 1, true), Is.Null, "sin catálogo: dosel");
            Assert.That(IntroCast.Resolve(catalog, 1, 31, 1, true), Is.Null, "nunca la especie de otro mundo (barrera de los ríos)");
            Assert.That(IntroCast.Resolve(catalog, 2, 5, 1, true), Is.Null, "nunca la especie de otro mundo");
        }

        /// <summary>
        /// La única regla del arte (guion §4.3 y §11; D-052): basta el miquito por código, que sale en todos los builds; sin él
        /// (solo si se fuerza), la tropa exige el PNG <c>_partes</c> y la tarjeta además el <c>_retrato</c>.
        /// </summary>
        [Test]
        public void ArtRule_TheCodeMiquitoOrTheFinalPngs()
        {
            Assert.That(FaunaArtRule.TroopArt(true, false), Is.True, "el miquito por código, sin PNG (también en release, D-052)");
            Assert.That(FaunaArtRule.TroopArt(false, false), Is.False, "sin miquito por código ni _partes: dosel");
            Assert.That(FaunaArtRule.TroopArt(false, true), Is.True);
            Assert.That(FaunaArtRule.CardArt(true, false, false), Is.True, "la tarjeta con el miquito por código, sin PNG");
            Assert.That(FaunaArtRule.CardArt(false, true, false), Is.False, "sin miquito por código ni _retrato: sin tarjeta");
            Assert.That(FaunaArtRule.CardArt(false, false, true), Is.False, "sin miquito por código ni _partes: tampoco tarjeta");
            Assert.That(FaunaArtRule.CardArt(false, true, true), Is.True);
        }

        [Test]
        public void WorldTable_IsTheGlossaryTable()
        {
            Assert.That(WorldTable.WorldOf(1), Is.EqualTo(1));
            Assert.That(WorldTable.WorldOf(10), Is.EqualTo(1));
            Assert.That(WorldTable.WorldOf(11), Is.EqualTo(2));
            Assert.That(WorldTable.WorldOf(420), Is.EqualTo(7));
            Assert.That(WorldTable.WorldOf(421), Is.EqualTo(8));
            Assert.That(WorldTable.WorldOf(560), Is.EqualTo(8));
            Assert.That(WorldTable.WorldOf(561), Is.EqualTo(9));
            Assert.That(WorldTable.WorldOf(960), Is.EqualTo(12));
            Assert.That(WorldTable.WorldOf(1000), Is.EqualTo(13));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldTable.WorldOf(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldTable.WorldOf(1001));
            int[] firstLevels = { 1, 11, 41, 81, 141, 201, 301, 421, 561, 701, 821, 921, 961 }; // glosario §8
            for (int w = 1; w <= WorldTable.WorldCount; w++)
            {
                Assert.That(WorldTable.FirstLevel(w), Is.EqualTo(firstLevels[w - 1]), "primer nivel del mundo " + w);
                Assert.That(WorldBanners.ShowsOn(firstLevels[w - 1]), Is.EqualTo(w >= WorldBanners.FirstBannerWorld), "banner del mundo " + w);
                Assert.That(SightingRule.IsBusy(firstLevels[w - 1]), Is.True, "sin tarjeta del miquito en el primer nivel del mundo " + w);
                Assert.That(WorldTable.WorldOf(WorldTable.LastLevelOf(w)), Is.EqualTo(w));
            }
        }

        [Test]
        public void Reader_RejectsDuplicates_Overlaps_Gaps_AndShortRanges()
        {
            Assert.That(FaunaCatalogReader.Read(Catalog(Species("a", 1, 420, 5), Species("b", 421, 1000, 600))).Species.Count, Is.EqualTo(2));
            Rejects(Catalog(Species("a", 1, 420, 5), Species("a", 421, 1000, 600)), "id repetido");
            Rejects(Catalog(Species("a", 1, 420, 5), Species("b", 420, 1000, 600)), "solape");
            Rejects(Catalog(Species("a", 1, 300, 5), Species("b", 421, 1000, 600)), "hueco");
            Rejects(Catalog(Species("a", 1, 420, 5), Species("b", 421, 960, 600)), "no llega a 1.000");
            Rejects(Catalog(Species("a", 1, 420, 421), Species("b", 421, 1000, 600)), "tarjeta fuera del tramo");
            Rejects(Catalog(Species("a", 0, 420, 5), Species("b", 421, 1000, 600)), "tramo desde 0");
            Rejects("{ \"formato\": \"af-fauna/1\", \"especies\": [] }", "formato");
            Rejects("{ \"formato\": \"af-fauna/0\", \"especies\": [] }", "sin especies");
            Rejects("no es json", "JSON");
        }

        [Test]
        public void Reader_RejectsEveryUnknownOrOutOfRangeValue()
        {
            string b = Species("b", 421, 1000, 600);
            Rejects(Catalog(Species("a", 1, 420, 5, color: "#12345"), b), "color de 5 cifras");
            Rejects(Catalog(Species("a", 1, 420, 5, color: "#GG0000"), b), "color no hexadecimal");
            Rejects(Catalog(Species("a", 1, 420, 5, skeleton: "E"), b), "esqueleto");
            Rejects(Catalog(Species("a", 1, 420, 5, model: "vuela"), b), "modelo");
            Rejects(Catalog(Species("a", 1, 420, 5, locomotion: "nada"), b), "locomoción desconocida");
            Rejects(Catalog(Species("a", 1, 420, 5, lighting: "tarde"), b), "iluminación desconocida");
            Rejects(Catalog(Species("a", 1, 420, 5, size: 15), b), "tam_dp < 16");
            Rejects(Catalog(Species("a", 1, 420, 5, size: 97), b), "tam_dp > 96");
            Rejects(Catalog(Species("a", 1, 420, 5, troop: 0), b), "tropa_intro < 1");
            Rejects(Catalog(Species("a", 1, 420, 5, troop: 9), b), "tropa_intro > 8");
            Rejects(Catalog(Species("a", 1, 420, 5, names: "{ \"pt\": \"x\" }"), b), "sin el nombre en español");
            Rejects(Catalog(Species("a", 1, 420, 5, names: "\"x\""), b), "nombres no es un objeto");
            Rejects(Catalog(Species("a", 1, 420, 5, slots: "{ \"partes\": \"fauna_b_partes\", \"retrato\": \"fauna_a_retrato\" }"), b), "ranura de otra especie");
            Rejects(Catalog(Species("a", 1, 420, 5, slots: "{ \"partes\": \"fauna_a_partes\", \"retrato\": \"a_retrato\" }"), b), "ranura mal formada");
            Rejects(Catalog(Species("a", 1, 420, 5, slots: "{ \"partes\": \"fauna_a_partes\" }"), b), "falta el retrato");
            Rejects(Catalog(Species("a", 1, 420, 5, slots: "[]"), b), "ranuras_arte no es un objeto");
            Rejects(Catalog(Species("a", 1, 420, 5, worlds: "[0, 1, 2, 3, 4, 5, 6, 7]"), b), "mundo 0");
            Rejects(Catalog(Species("a", 1, 420, 5), Species("b", 421, 1000, 600, worlds: "[8, 9, 10, 11, 12, 13, 14]")), "mundo 14");
            Rejects(Catalog(Species("a", 1, 420, 5, worlds: "[]"), b), "sin mundos");
            Rejects(Catalog(Species("a", 1, 420, 5, pair: "{ \"cuerpo\": \"#12\", \"extremidades\": \"#000000\", \"cara\": \"#000000\", \"hocico\": \"#000000\", \"gorra\": \"#000000\", \"punta_cola\": \"#000000\" }"), b), "paleta_pareja con un color inválido");
            Rejects(Catalog(Species("a", 1, 420, 5, pair: "{ \"cuerpo\": \"#000000\" }"), b), "paleta_pareja incompleta");
            Rejects(Catalog(Species("a", 1, 420, 5, pair: "\"#000000\""), b), "paleta_pareja no es un objeto");
            Assert.That(FaunaCatalogReader.Read(Catalog(Species("a", 1, 420, 5, pair: Palette("#7A6A5A")), b)).Find("a")!.PairPalette!.Body, Is.EqualTo(0x7A6A5A), "una paleta_pareja válida se lee");
        }

        /// <summary>
        /// Los mundos de cada especie, contra la tabla de mundos (glosario §8): un tramo que se sale de sus mundos dejaría esos
        /// niveles en dosel sin avisar; un mundo de más, una especie que «vive» donde nunca sale.
        /// </summary>
        [Test]
        public void Reader_RejectsWorldsThatDoNotMatchTheStretch()
        {
            string b = Species("b", 41, 1000, 600);
            Assert.That(FaunaCatalogReader.Read(Catalog(Species("a", 1, 40, 5, worlds: "[1, 2]"), b)).Find("a")!.Worlds, Is.EquivalentTo(new[] { 1, 2 }));
            Rejects(Catalog(Species("a", 1, 40, 5, worlds: "[1]"), b), "el tramo 1-40 cruza el mundo 2 (Tapajós)");
            Rejects(Catalog(Species("a", 1, 40, 5, worlds: "[2]"), b), "el tramo empieza en el mundo 1");
            Rejects(Catalog(Species("a", 1, 40, 5, worlds: "[1, 2, 3]"), b), "el mundo 3 no tiene niveles en el tramo");
            Rejects("{ \"formato\": \"af-fauna/0\", \"especies\": [" + Species("a", 1, 960, 5) + "], \"sin_fauna\": [ { \"mundos\": [12], \"desde_nivel\": 961, \"hasta_nivel\": 1000 } ] }", "sin_fauna en un mundo que no es el suyo");
            Assert.That(FaunaCatalogReader.Read("{ \"formato\": \"af-fauna/0\", \"especies\": [" + Species("a", 1, 960, 5) + "], \"sin_fauna\": [ { \"mundos\": [13], \"desde_nivel\": 961, \"hasta_nivel\": 1000 } ] }").IsNoFauna(970), Is.True);
        }

        [Test]
        public void ParseColor_ReadsRrggbb()
        {
            Assert.That(FaunaCatalogReader.ParseColor("#8C8A68", "t"), Is.EqualTo(0x8C8A68));
            Assert.That(FaunaCatalogReader.ParseColor("#000000", "t"), Is.EqualTo(0));
        }

        private static void AssertNoThreatLabel(string text, string where)
        {
            foreach (string word in Words(text))
            {
                Assert.That(IucnLabels, Does.Not.Contain(word), where + ": «" + text + "» lleva la etiqueta UICN «" + word + "»");
            }

            string lower = text.ToLowerInvariant();
            foreach (string w in ThreatWords)
            {
                Assert.That(lower, Does.Not.Contain(w), where + ": «" + text + "» habla de amenaza como trofeo");
            }
        }

        // Palabras de un texto (letras), tal cual, para buscar etiquetas en mayúsculas como palabra suelta.
        private static IEnumerable<string> Words(string text)
        {
            var current = new StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    current.Append(c);
                    continue;
                }

                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
            }
        }

        private static void Rejects(string json, string why)
        {
            Assert.Throws<FaunaFormatException>(() => FaunaCatalogReader.Read(json), why);
        }

        private static string Catalog(string a, string b)
        {
            return "{ \"formato\": \"af-fauna/0\", \"especies\": [" + a + ", " + b + "] }";
        }

        private static string Palette(string body)
        {
            return "{ \"cuerpo\": \"" + body + "\", \"extremidades\": \"#D7A33E\", \"cara\": \"#F1E9DA\", \"hocico\": \"#2B2118\", \"gorra\": \"#8C8A68\", \"punta_cola\": \"#2B2118\" }";
        }

        // Una especie con valores válidos salvo los que se indiquen; sin «worlds», los mundos que cruza su tramo (glosario §8).
        private static string Species(string id, int from, int to, int card, string color = "#8C8A68", string skeleton = "B", string model = "fila",
            string locomotion = "corre", string lighting = "dia", int size = 40, int troop = 3, string? names = null, string? slots = null,
            string? worlds = null, string? pair = null)
        {
            names ??= "{ \"es\": \"x\" }";
            slots ??= "{ \"partes\": \"fauna_" + id + "_partes\", \"retrato\": \"fauna_" + id + "_retrato\" }";
            worlds ??= WorldsOf(from, to);
            string pairJson = pair != null ? ", \"paleta_pareja\": " + pair : string.Empty;
            return "{ \"id\": \"" + id + "\", \"cientifico\": \"X\", \"mostrar_cientifico\": true, \"nombres\": " + names + ", "
                + "\"esqueleto\": \"" + skeleton + "\", \"modelo\": \"" + model + "\", \"locomocion\": \"" + locomotion + "\", \"tam_dp\": " + size
                + ", \"tropa_intro\": " + troop + ", \"rasgos_reposo\": [], \"paleta\": " + Palette(color) + pairJson + ", \"iluminacion\": \"" + lighting
                + "\", \"mundos\": " + worlds + ", \"desde_nivel\": " + from + ", \"hasta_nivel\": " + to + ", \"nivel_tarjeta\": " + card
                + ", \"uicn\": \"LC\", \"amenazada\": false, \"dato_tarjeta\": \"k\", \"ranuras_arte\": " + slots
                + ", \"revision_asesor\": \"pendiente\", \"evitar\": [\"mascota\"] }";
        }

        private static string WorldsOf(int from, int to)
        {
            if (from < 1 || to > WorldTable.LastLevel || from > to)
            {
                return "[1]";
            }

            var sb = new StringBuilder("[");
            int first = WorldTable.WorldOf(from);
            int last = WorldTable.WorldOf(to);
            for (int w = first; w <= last; w++)
            {
                sb.Append(w == first ? string.Empty : ", ").Append(w.ToString(CultureInfo.InvariantCulture));
            }

            return sb.Append(']').ToString();
        }
    }
}
