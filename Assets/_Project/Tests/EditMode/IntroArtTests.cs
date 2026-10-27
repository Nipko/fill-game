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
            Assert.That(MinGenerationMs(art => art.FruitFlight(1), art => art.FruitAtlasMs), Is.LessThanOrEqualTo(PerfBudget.Ms(10.0)), "generación del atlas tras el JIT (mínimo de 3), " + PerfBudget.Describe(10.0));
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
        public void FruitCells_AreNotEmpty()
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

        /// <summary>
        /// Ninguna fruta en vuelo tiene la silueta y el color de la semilla-moneda (glosario §2.1; guion §5 y R7): un óvalo
        /// cerrado (la silueta coincide con su elipse de momentos en ≥ 92 %) y ámbar (la mitad o más de sus píxeles a menos de
        /// 0,22 del ámbar de la moneda). El detector marca una semilla ámbar sintética; el copoazú, en concreto, es un trozo
        /// partido y no un óvalo (revisión: a 7 dp parecía un grano).
        /// </summary>
        [Test]
        public void FruitCells_AreNeverTheAmberSeedOfTheCurrency()
        {
            const int n = ProceduralArt.FruitFlightPx;
            var mask = new bool[n * n];
            var colors = new Color[n * n];

            // Semilla sintética: óvalo ámbar de 20 × 27 px.
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f - 32f) / 10f;
                    float dy = (y + 0.5f - 32f) / 13.5f;
                    mask[(y * n) + x] = (dx * dx) + (dy * dy) <= 1f;
                    colors[(y * n) + x] = ProceduralArt.SeedAmber;
                }
            }

            SeedLikeness(mask, colors, out float seedIou, out float seedAmber);
            Assert.That(seedIou >= 0.92f && seedAmber >= 0.5f, Is.True, "el detector reconoce la semilla-moneda (" + seedIou + ", " + seedAmber + ")");

            Color32[] px = ProceduralArt.FruitFlightPixels();
            for (int tint = 1; tint <= 14; tint++)
            {
                RectInt cell = ProceduralArt.FruitCell(tint);
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        Color32 c = px[((cell.y + y) * ProceduralArt.FruitAtlasWidth) + cell.x + x];
                        mask[(y * n) + x] = c.a > 128;
                        colors[(y * n) + x] = c;
                    }
                }

                SeedLikeness(mask, colors, out float iou, out float amber);
                string what = ProceduralArt.TintIds[tint] + ": óvalo " + iou.ToString("0.00") + ", ámbar " + amber.ToString("0.00");
                Assert.That(iou >= 0.92f && amber >= 0.5f, Is.False, "parece la semilla-moneda: " + what);
                if (tint == 2)
                {
                    Assert.That(iou, Is.LessThan(0.88f), "el copoazú es un trozo partido, nunca un óvalo cerrado: " + what);
                }
            }
        }

        // Cuánto se parece una silueta a un óvalo (IoU con la elipse de sus momentos) y qué parte de sus píxeles es ámbar.
        private static void SeedLikeness(bool[] mask, Color[] colors, out float ellipseIou, out float amberShare)
        {
            int n = ProceduralArt.FruitFlightPx;
            double sx = 0;
            double sy = 0;
            int count = 0;
            int amber = 0;
            Color seed = ProceduralArt.SeedAmber;
            for (int i = 0; i < mask.Length; i++)
            {
                if (!mask[i])
                {
                    continue;
                }

                sx += i % n;
                sy += i / n;
                count++;
                Color c = colors[i];
                float d = Mathf.Sqrt(((c.r - seed.r) * (c.r - seed.r)) + ((c.g - seed.g) * (c.g - seed.g)) + ((c.b - seed.b) * (c.b - seed.b)));
                amber += d < 0.22f ? 1 : 0;
            }

            if (count == 0)
            {
                ellipseIou = 0f;
                amberShare = 0f;
                return;
            }

            double cx = sx / count;
            double cy = sy / count;
            double xx = 0;
            double yy = 0;
            double xy = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i])
                {
                    double dx = (i % n) - cx;
                    double dy = (i / n) - cy;
                    xx += dx * dx;
                    yy += dy * dy;
                    xy += dx * dy;
                }
            }

            xx /= count;
            yy /= count;
            xy /= count;
            double tr = xx + yy;
            double det = (xx * yy) - (xy * xy);
            double disc = System.Math.Sqrt(System.Math.Max(0.0, (tr * tr / 4.0) - det));
            double l1 = (tr / 2.0) + disc;
            double l2 = System.Math.Max(1e-6, (tr / 2.0) - disc);
            double angle = 0.5 * System.Math.Atan2(2.0 * xy, xx - yy);
            double a = 2.0 * System.Math.Sqrt(l1);
            double b = 2.0 * System.Math.Sqrt(l2);
            double cos = System.Math.Cos(angle);
            double sin = System.Math.Sin(angle);
            int inter = 0;
            int union = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                double dx = (i % n) - cx;
                double dy = (i / n) - cy;
                double u = (dx * cos) + (dy * sin);
                double v = (-dx * sin) + (dy * cos);
                bool inEllipse = ((u * u) / (a * a)) + ((v * v) / (b * b)) <= 1.0;
                inter += inEllipse && mask[i] ? 1 : 0;
                union += inEllipse || mask[i] ? 1 : 0;
            }

            ellipseIou = union == 0 ? 0f : inter / (float)union;
            amberShare = amber / (float)count;
        }

        [Test]
        public void MonkeyArt_ThreeSpecies_AreDistinguishable_AndGeneratedWithin10ms()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            Art.MonkeyArtFor(catalog.Find("saimiri_collinsi")!); // JIT
            FaunaSpecies silvery = catalog.Find("mico_argentatus")!;
            Assert.That(MinGenerationMs(art => art.MonkeyArtFor(silvery), art => art.FaunaArtMs), Is.LessThanOrEqualTo(PerfBudget.Ms(10.0)), "piezas de una especie (mínimo de 3), " + PerfBudget.Describe(10.0));
            var second = new ProceduralArt("white");
            try
            {
                MonkeyArt squirrel = second.MonkeyArtFor(silvery);
                MonkeyArt monkey = second.MonkeyArtFor(catalog.Find("saimiri_collinsi")!);
                MonkeyArt uakari = second.MonkeyArtFor(catalog.Find("cacajao_calvus_ucayalii")!);
                MonkeyArt woolly = second.MonkeyArtFor(catalog.Find("lagothrix_lagothricha")!);
                MonkeyArt spider = second.MonkeyArtFor(catalog.Find("ateles_chamek")!);
                MonkeyArt emperor = second.MonkeyArtFor(catalog.Find("saguinus_imperator")!);
                Assert.That(second.MonkeyArtFor(catalog.Find("saimiri_collinsi")!), Is.SameAs(monkey), "en caché");

                // Silueta (esqueleto, cabeza, extremidades), colores y cola distintos.
                Assert.That(uakari.TailLength, Is.LessThan(monkey.TailLength), "el huapo, cola corta");
                Assert.That(woolly.TailCurls, Is.True, "el churuco, cola prensil enrollada");
                Assert.That(uakari.TailWidthS, Is.GreaterThan(monkey.TailWidthS), "cola mullida del pitécido");

                // Churuco y maquisapa (mismo esqueleto D) ya no son la misma figura (revisión, 22_species_*).
                Assert.That(woolly.TorsoWidthScale, Is.GreaterThanOrEqualTo(1.25f), "churuco: torso 1,25 × más ancho");
                Assert.That(woolly.LimbScale, Is.LessThanOrEqualTo(0.8f), "churuco: extremidades a 0,8");
                Assert.That(woolly.HugsFruit, Is.True, "churuco: la fruta abrazada al pecho");
                Assert.That(spider.LimbScale, Is.GreaterThanOrEqualTo(1.4f), "maquisapa: extremidades 1,4 × más largas");
                Assert.That(spider.HeadS, Is.EqualTo(woolly.HeadS * 0.8f).Within(0.001f), "maquisapa: cabeza a 0,8");
                Assert.That(spider.ArmUp, Is.True, "maquisapa: un brazo arriba agarrado a la rama");
                Assert.That(spider.LimbScale / woolly.LimbScale, Is.GreaterThanOrEqualTo(1.7f));

                // Pichico emperador: bigote blanco aparte, que baja más allá de la cabeza (hasta pasar los hombros) y se abre a
                // los lados 0,6 de la cabeza.
                Assert.That(emperor.Whiskers, Is.Not.Null, "pichico: bigote");
                Assert.That(emperor.WhiskersOffsetS.y - (emperor.WhiskersSizeS.y * 0.5f), Is.LessThan(-emperor.HeadS * 0.5f), "baja más allá de la cabeza");
                Assert.That(monkey.Whiskers, Is.Null);
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

        /// <summary>
        /// Rasgos de reposo que se pintan en las piezas (revisión, 22_species_*): el sauim con el babero blanco bajo la cabeza
        /// negra y los brazos del color del cuerpo; el mono nocturno con los ojos ámbar enormes. Se mira el color en el atlas.
        /// </summary>
        [Test]
        public void RestTraits_ArePaintedOnTheParts()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            int size = ProceduralArt.FaunaPixelsSize;

            FaunaSpecies sauim = catalog.Find("saguinus_bicolor")!;
            Color32[] px = ProceduralArt.FaunaPixels(sauim);
            Color limb = px[(56 * size) + 16]; // centro de la extremidad (celda 0, 0, 32 × 112)
            Assert.That(Distance(limb, Palette.Hex(sauim.Palette.Body)), Is.LessThan(0.08f), "sauim: brazos del color del cuerpo, no blancos");
            int white = 0;
            for (int y = 128; y < 256; y++)
            {
                for (int x = 128; x < 256; x++)
                {
                    white += Distance(px[(y * size) + x], Palette.Hex(sauim.Palette.Limbs)) < 0.06f ? 1 : 0;
                }
            }

            Assert.That(white, Is.GreaterThan(400), "sauim: babero blanco en el torso");

            // El babero asoma bajo el cráneo en la pieza de la cabeza (por encima del torso: colgado, el brazo de delante tapa
            // el pecho; revisión de 22_species_saguinus_bicolor).
            int collar = 0;
            for (int y = 128; y < 142; y++)
            {
                for (int x = 40; x < 110; x++)
                {
                    collar += Distance(px[(y * size) + x], Palette.Hex(sauim.Palette.Limbs)) < 0.06f ? 1 : 0;
                }
            }

            Assert.That(collar, Is.GreaterThan(150), "sauim: babero blanco bajo la cabeza negra");

            FaunaSpecies night = catalog.Find("aotus_trivirgatus")!;
            px = ProceduralArt.FaunaPixels(night);
            int amber = 0;
            for (int y = 128; y < 256; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    amber += Amber(px[(y * size) + x]) ? 1 : 0;
                }
            }

            Assert.That(amber, Is.GreaterThan(150), "mono nocturno: iris ámbar de ojos enormes");
        }

        /// <summary>
        /// Revisión del 2026-10-09 (22_species_saguinus_imperator y 22_species_cacajao_calvus_ucayalii; guion §4.2 y §4.6.2): el
        /// bigote del pichico son hebras blancas #F4F1EA sin contorno de tinta (con borde de 1 px #BDB8AE) que bajan del labio
        /// más allá de los hombros; la cabeza del huapo es un óvalo vertical rojo, sin nariz saliente, con ojos pequeños de color
        /// ámbar y pelo largo rojizo alrededor de la cabeza y en los hombros. Se mira en el atlas de piezas (y desde abajo).
        /// </summary>
        [Test]
        public void ReviewedTraits_MoustacheWithoutInk_AndBaldRedOvalHeadWithLongHair()
        {
            FaunaCatalog catalog = FaunaCatalogReader.Read(File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json")));
            int size = ProceduralArt.FaunaPixelsSize;

            // Bigote: celda (80, 0, 176 × 78).
            Color32[] px = ProceduralArt.FaunaPixels(catalog.Find("saguinus_imperator")!);
            int white = 0;
            int ink = 0;
            int lowest = int.MaxValue;
            for (int y = 0; y < 78; y++)
            {
                for (int x = 80; x < 256; x++)
                {
                    Color32 c = px[(y * size) + x];
                    if (c.a < 128)
                    {
                        continue;
                    }

                    bool isWhite = Distance(c, Palette.Hex(0xF4F1EA)) < 0.04f;
                    white += isWhite ? 1 : 0;
                    ink += ((0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b)) / 255f < 0.45f ? 1 : 0;
                    lowest = isWhite ? Mathf.Min(lowest, y) : lowest;
                }
            }

            Assert.That(white, Is.GreaterThan(300), "pichico: hebras blancas #F4F1EA");
            Assert.That(ink, Is.EqualTo(0), "pichico: el bigote, sin contorno de tinta (se leía como colmillos)");
            Assert.That(lowest, Is.LessThan(20), "baja del labio (y 63 en la celda) más allá de los hombros (y ≈ 40)");

            // Huapo: cabeza en la celda (0, 128) y torso en la (128, 128).
            FaunaSpecies uakari = catalog.Find("cacajao_calvus_ucayalii")!;
            px = ProceduralArt.FaunaPixels(uakari);
            Color red = Palette.Hex(uakari.Palette.Face);
            Color hair = Palette.Hex(0xC8743A);
            int x0 = int.MaxValue;
            int x1 = int.MinValue;
            int y0 = int.MaxValue;
            int y1 = int.MinValue;
            int nose = 0;
            int headHair = 0;
            int amber = 0;
            for (int y = 128; y < 256; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    Color32 c = px[(y * size) + x];
                    if (c.a < 128)
                    {
                        continue;
                    }

                    if (Distance(c, red) < 0.05f)
                    {
                        x0 = Mathf.Min(x0, x);
                        x1 = Mathf.Max(x1, x);
                        y0 = Mathf.Min(y0, y);
                        y1 = Mathf.Max(y1, y);
                    }

                    nose += Distance(c, Palette.Hex(uakari.Palette.Muzzle)) < 0.03f ? 1 : 0;
                    headHair += Hair(c, hair) ? 1 : 0;
                    amber += Amber(c) ? 1 : 0;
                }
            }

            Assert.That(y1 - y0, Is.GreaterThan((x1 - x0) * 1.15f), "huapo: la cara y el cuero cabelludo, un óvalo vertical rojo");
            Assert.That(x1 - x0, Is.LessThan(60), "no un disco que llena la cabeza");
            Assert.That(nose, Is.LessThan(30), "sin nariz saliente (solo dos fosas a ras de la cara)");
            Assert.That(headHair, Is.GreaterThan(250), "pelo largo rojizo alrededor de la cabeza (antes, 0: un disco con flecos del color del cuerpo)");
            Assert.That(amber, Is.InRange(20, 150), "ojos pequeños de color ámbar");

            int torsoHair = 0;
            for (int y = 128; y < 256; y++)
            {
                for (int x = 128; x < 256; x++)
                {
                    torsoHair += Hair(px[(y * size) + x], hair) ? 1 : 0;
                }
            }

            Assert.That(torsoHair, Is.GreaterThan(400), "y en los hombros y el lomo");
        }

        [Test]
        public void PairPalette_IsOptional_AndPaintsTheSecondIndividual()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Data/Fauna/miquitos_v0.json"));
            FaunaCatalog catalog = FaunaCatalogReader.Read(json);
            FaunaSpecies saki = catalog.Find("pithecia_pithecia")!;
            var art = new ProceduralArt("white");
            try
            {
                MonkeyArt male = art.MonkeyArtFor(saki);
                Assert.That(art.MonkeyArtFor(saki, true), Is.SameAs(male), "sin paleta_pareja, el segundo individuo es igual");
            }
            finally
            {
                art.Dispose();
            }

            // Con paleta_pareja (la hembra gris parda del saquí), el segundo individuo tiene sus propias piezas.
            string withPair = json.Replace("\"id\": \"pithecia_pithecia\",", "\"id\": \"pithecia_pithecia\", \"paleta_pareja\": { \"cuerpo\": \"#7A6A5A\", \"extremidades\": \"#6E5E50\", \"cara\": \"#C9B9A0\", \"hocico\": \"#6E5A4E\", \"gorra\": \"#7A6A5A\", \"punta_cola\": \"#5E5044\" },");
            Assert.That(withPair, Is.Not.EqualTo(json));
            FaunaSpecies pairSaki = FaunaCatalogReader.Read(withPair).Find("pithecia_pithecia")!;
            Assert.That(pairSaki.PairPalette, Is.Not.Null);
            Assert.That(pairSaki.PairPalette!.Body, Is.EqualTo(0x7A6A5A));
            art = new ProceduralArt("white");
            try
            {
                Assert.That(art.MonkeyArtFor(pairSaki, true), Is.Not.SameAs(art.MonkeyArtFor(pairSaki)));
                Color32[] female = ProceduralArt.FaunaPixels(pairSaki, true);
                int size = ProceduralArt.FaunaPixelsSize;
                Assert.That(Distance(female[(190 * size) + 192], Palette.Hex(0x7A6A5A)), Is.LessThan(0.1f), "hembra gris parda");
            }
            finally
            {
                art.Dispose();
            }
        }

        // Un píxel del iris ámbar (#C8902A): desde los personajes de juguete (D-058) el iris va en degradado (más oscuro arriba, más
        // claro abajo), así que se mira la familia del color (el matiz ámbar, saturado y no oscuro), no un tono exacto.
        private static bool Amber(Color c)
        {
            if (c.a < 0.5f)
            {
                return false;
            }

            Color.RGBToHSV(c, out float h, out float s, out float v);
            return h >= 0.088f && h <= 0.13f && s >= 0.5f && v >= 0.35f;
        }

        // Un píxel del pelo largo rojizo: desde los personajes de juguete (D-058) el pelo va en tres tonos (sombra, medio y luz), así
        // que cuenta cualquiera de los tres, como el color de antes.
        private static bool Hair(Color c, Color hair)
        {
            ToyTones t = ToyShading.Tones(hair);
            return c.a >= 0.5f && (Distance(c, hair) < 0.06f || Distance(c, t.Light) < 0.06f || Distance(c, t.Shadow) < 0.06f);
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Sqrt(((a.r - b.r) * (a.r - b.r)) + ((a.g - b.g) * (a.g - b.g)) + ((a.b - b.b) * (a.b - b.b)));
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
