#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los 9 paquetes de la escena (tutorial_v0 y mundo_02..05_v1, <c>pack_schema</c> 2; traspaso §6 pasos 1.1-1.2;
    /// mundo_06_v1, <c>pack_schema</c> 3, desde la tarea C1a; y mundo_07..09_v1, <c>pack_schema</c> 3, desde la tarea C1b, en la
    /// variante <see cref="LevelCatalog.PublishedVariant"/>; 08 §4.1 y §4.4; contrato 21): cubren los niveles 1-700 sin huecos,
    /// cada tablero (base y relief) construye con las mecánicas del núcleo, pasa <see cref="LevelValidation"/>, arranca una
    /// sesión jugable y cabe en el tablero (≤ 16 cuias, D-006), los estrenos de D-007 (02 §9.1 y §11; 02 §8) están donde dice el
    /// calendario y los del mundo 6, la Liana de cuenta en el 243 y la Hoja atada en el 283 (03 §11; D-023, punto 5), y los de
    /// los mundos 7-9, la Tinaja en el 323 (solo en la variante «a», D-019), el Brote de dos hojas en el 343 y la Flor en el 403,
    /// también. La variante «a» de los mundos 7-9, que la escena no enlaza mientras se publique la «b», se comprueba aparte.
    /// </summary>
    public sealed class CampaignPackTests
    {
        // D-007: nivel del estreno → intro_mechanic (Cuia chica 14, Cuia alta 27, Hoja 43, Matapi 63, Liana con fruta 83, Aguas
        // turbias 100, Remolino 103, Barro 123, Lluvia 143, Brote 163 y Piraña 183). La lista es la única del código (T-F):
        // DebutIntros va en su mismo orden.
        private static readonly int[] DebutLevels = AF.GameFlow.SightingRule.MechanicDebutLevels;
        private static readonly string[] DebutIntros = { "chica", "alta", "hoja", "matapi", "liana", "murky", "remolino", "barro", "lluvia", "brote", "pirana" };

        [Test]
        public void Packs_LoadAndCover1To700()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            Assert.That(packs.Count, Is.EqualTo(9));
            LevelCatalog catalog = LevelCatalog.Create(packs);
            Assert.That(catalog.FirstLevel, Is.EqualTo(1));
            Assert.That(catalog.LastLevel, Is.EqualTo(700));
            Assert.That(catalog.Count, Is.EqualTo(700));
            Assert.That(catalog.IsContiguous, Is.True, catalog.DescribeGaps());
            Assert.That(catalog.NextLevelAfter(200), Is.EqualTo(201), "el 200 ya no es el fin de contenido");
            Assert.That(catalog.NextLevelAfter(300), Is.EqualTo(301), "el 300 ya no es el fin de contenido (C1b)");
            Assert.That(catalog.NextLevelAfter(700), Is.Null, "tras el 700, «El río sigue creciendo» (06 §9)");

            for (int level = 1; level <= 700; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                Assert.That(def.Level, Is.EqualTo(level));
                Assert.That(def.World, Is.EqualTo(WorldTable.WorldOf(level)), "glosario §8: mundo del nivel " + level);
                Assert.That(catalog.PackOf(level)!.World, Is.EqualTo(def.World), "paquete del nivel " + level);
            }

            Assert.That(packs[1].PackId, Is.EqualTo("mundo_02_v1"));
            Assert.That(packs[1].First.Level, Is.EqualTo(11));
            Assert.That(packs[4].PackId, Is.EqualTo("mundo_05_v1"));
            Assert.That(packs[5].PackId, Is.EqualTo("mundo_06_v1"));
            Assert.That(packs[5].First.Level, Is.EqualTo(201));
            Assert.That(packs[5].World, Is.EqualTo(6), "glosario §8: Oriente");
            Assert.That(catalog.PackOf(300), Is.SameAs(packs[5]));
            int[] first = { 301, 421, 561 };
            int[] last = { 420, 560, 700 };
            for (int w = 0; w < 3; w++)
            {
                LevelPack pack = packs[6 + w];
                Assert.That(pack.PackId, Is.EqualTo("mundo_0" + (7 + w) + "_v1"));
                Assert.That(pack.World, Is.EqualTo(7 + w), "glosario §8: Madre de Dios, Madidi y Ucayali");
                Assert.That(pack.First.Level, Is.EqualTo(first[w]));
                Assert.That(pack.Levels[pack.Count - 1].Level, Is.EqualTo(last[w]));
                Assert.That(catalog.PackOf(last[w]), Is.SameAs(pack));
            }
        }

        [Test]
        public void Boards_BuildAndValidate_All690()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            int checkedLevels = 0;
            for (int level = 11; level <= 700; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                BoardState board = def.CreateBoard();
                Assert.That(board.Count, Is.EqualTo(def.CuiaCount), def.Id);
                Assert.That(LevelValidation.Validate(board), Is.Null, def.Id);
                var session = new LevelSession(board, true, PourModes.Default);
                Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing), def.Id);
                for (int i = 0; i < board.Count; i++)
                {
                    Assert.That(board[i].Mechanic, Is.EqualTo(def.Attrs[i].Type), def.Id + ", cuia " + i);
                }

                Assert.That(board.HiddenCount, Is.EqualTo(def.HiddenCount), def.Id);
                Assert.That(def.CuiaCount, Is.LessThanOrEqualTo(BoardState.MaxCuias), "D-006: ≤ 16 ranuras (" + def.Id + ")");
                checkedLevels++;
            }

            Assert.That(checkedLevels, Is.EqualTo(690));
        }

        [Test]
        public void Relief_BuildAndValidate_All1310()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            int variants = 0;
            for (int level = 1; level <= 700; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                if (level >= 31)
                {
                    Assert.That(def.Relief.Count, Is.InRange(1, 2), "03 §13: relief desde el 31 (" + def.Id + ")");
                }
                else
                {
                    Assert.That(def.Relief.Count, Is.EqualTo(0), "03 §13: relief = [] antes del 31 (" + def.Id + ")");
                }
                for (int r = 0; r < def.Relief.Count; r++)
                {
                    ReliefBoard relief = def.Relief[r];
                    BoardState board = relief.CreateBoard();
                    string where = def.Id + ".relief[" + r + "] " + relief.Type;
                    Assert.That(board.Count, Is.EqualTo(relief.CuiaCount), where);
                    Assert.That(relief.CuiaCount, Is.LessThanOrEqualTo(BoardState.MaxCuias), "D-006: ≤ 16 ranuras (" + where + ")");
                    Assert.That(relief.Attrs.Count, Is.EqualTo(relief.CuiaCount), where);
                    Assert.That(LevelValidation.Validate(board), Is.Null, where);
                    if (!def.HasMechanic(MechanicType.Tinaja))
                    {
                        Assert.That(board.HasHidden, Is.False, "relief sin ocultos (salvo la máscara de la Tinaja): " + where);
                    }
                    Assert.That(new LevelSession(board, true, PourModes.Default).Status, Is.EqualTo(LevelStatus.Playing), where);
                    if (relief.Type == ReliefBoard.TypePlusEmpty)
                    {
                        Assert.That(relief.CuiaCount, Is.EqualTo(def.CuiaCount + 1), where);
                    }
                    else
                    {
                        Assert.That(relief.Par, Is.Not.Null, where);
                    }

                    variants++;
                }
            }

            int worlds7to9 = LevelCatalog.TinajaPublished ? 774 : 778;
            Assert.That(variants, Is.EqualTo(334 + 198 + worlds7to9),
                "334 en 1-200, 198 en el mundo 6 (100 de un color menos y 98 de una vacía más) y, en los mundos 7-9, 774 en la variante «a» "
                + "(379 y 395) o 778 en la «b» (384 y 394)");
        }

        [Test]
        public void Tutorial_Unchanged()
        {
            // tutorial_v0 da el mismo BoardState que antes de BoardBuilder: Cuia.Create con sus dosis, sin mecánicas ni ocultos.
            LevelPack tutorial = TestPacks.Campaign[0];
            Assert.That(tutorial.PackId, Is.EqualTo("tutorial_v0"));
            Assert.That(tutorial.Count, Is.EqualTo(10));
            for (int n = 0; n < tutorial.Count; n++)
            {
                LevelDefinition def = tutorial.Levels[n];
                var cuias = new Cuia[def.CuiaCount];
                for (int i = 0; i < cuias.Length; i++)
                {
                    int[] doses = def.CopyDoses(i);
                    cuias[i] = doses.Length == 0 ? Cuia.Empty(def.CapacityOf(i)) : Cuia.Create(def.CapacityOf(i), doses);
                }

                var before = new BoardState(cuias);
                BoardState now = def.CreateBoard();
                Assert.That(now, Is.EqualTo(before), def.Id);
                for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
                {
                    Assert.That(now.UnitsOf(c), Is.EqualTo(before.UnitsOf(c)), def.Id + ", U_c del color " + c);
                }

                Assert.That(def.IntroMechanic, Is.Null, def.Id);
                Assert.That(def.Skippable, Is.True, def.Id);
                Assert.That(def.IsMurky, Is.False, def.Id);
                Assert.That(def.HasMechanic(MechanicType.Hoja) || def.HasMud || def.HasSmallCuia || def.HasTallCuia, Is.False, def.Id);
                Assert.That(def.Units.Of(8), Is.EqualTo(4), def.Id);
            }
        }

        [Test]
        public void Debuts_MatchD007()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            for (int level = 1; level <= 200; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                int debut = System.Array.IndexOf(DebutLevels, level);
                Assert.That(def.IntroMechanic, Is.EqualTo(debut >= 0 ? DebutIntros[debut] : null), "intro_mechanic del " + level);
                Assert.That(def.Skippable, Is.EqualTo(debut < 0), "skippable es false justo en los estrenos (" + level + ")");
            }

            // Cada estreno lleva lo que estrena (02 §8, §9.1 y §11).
            Assert.That(catalog.Find(14)!.HasSmallCuia, Is.True, "Cuia chica");
            Assert.That(catalog.Find(27)!.HasTallCuia, Is.True, "Cuia alta");
            Assert.That(catalog.Find(43)!.HasMechanic(MechanicType.Hoja), Is.True);
            Assert.That(catalog.Find(63)!.HasMechanic(MechanicType.Matapi), Is.True);
            Assert.That(catalog.Find(83)!.HasMechanic(MechanicType.Liana), Is.True);
            Assert.That(catalog.Find(83)!.Attrs[9], Is.EqualTo(new MechanicSpec(MechanicType.Liana, key: 3)), "Liana con fruta (D-007)");
            Assert.That(catalog.Find(100)!.IsMurky, Is.True);
            Assert.That(catalog.Find(103)!.HasMechanic(MechanicType.Remolino), Is.True);
            Assert.That(catalog.Find(123)!.HasMud, Is.True, "Barro");
            Assert.That(catalog.Find(143)!.HasMechanic(MechanicType.Lluvia), Is.True);
            Assert.That(catalog.Find(163)!.HasMechanic(MechanicType.Brote), Is.True);
            Assert.That(catalog.Find(183)!.HasMechanic(MechanicType.Pirana), Is.True);

            // Antes de su estreno, ningún nivel lleva la mecánica.
            for (int level = 1; level < 43; level++)
            {
                Assert.That(catalog.Find(level)!.HasMechanic(MechanicType.Hoja), Is.False, "Hoja antes del 43: " + level);
            }

            for (int level = 1; level < 100; level++)
            {
                Assert.That(catalog.Find(level)!.IsMurky, Is.False, "Aguas turbias antes del 100: " + level);
            }

            for (int level = 1; level < 123; level++)
            {
                Assert.That(catalog.Find(level)!.HasMud, Is.False, "Barro antes del 123: " + level);
            }
        }

        [Test]
        public void World6Debuts_CountLiana243_AndTiedLeaf283()
        {
            // 03 §11 y SightingRule.VariantDebutLevels (VA1a): en 201-300 solo estrenan la Liana de cuenta (243, intro_mechanic
            // liana_n [P]) y la Hoja atada (283, hoja_atada; D-023, punto 5); son los únicos niveles no saltables del mundo 6. Los
            // estrenos del Brote de dos hojas (343) y de la Flor (403), de VA1b, caen en el mundo 7.
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            Assert.That(AF.GameFlow.SightingRule.VariantDebutLevels, Is.EqualTo(new[] { 243, 283, 343, 403 }));
            for (int level = 201; level <= 300; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                string? intro = level == 243 ? AF.GameFlow.LevelCardInfo.LianaCountIntro
                    : level == 283 ? AF.GameFlow.LevelCardInfo.HojaAtadaIntro : null;
                Assert.That(def.IntroMechanic, Is.EqualTo(intro), "intro_mechanic del " + level);
                Assert.That(def.Skippable, Is.EqualTo(intro == null), "skippable es false justo en los estrenos (" + level + ")");
                Assert.That(def.World, Is.EqualTo(6));
                Assert.That(def.HasMechanic(MechanicType.Tinaja), Is.False, "la Tinaja estrena en el 323 (D-019)");
                if (level < 243)
                {
                    Assert.That(CountLiana(def), Is.LessThan(0), "Liana de cuenta antes del 243: " + level);
                }

                if (level < 283)
                {
                    Assert.That(def.HasMechanic(MechanicType.HojaAtada), Is.False, "Hoja atada antes del 283: " + level);
                }
            }

            LevelDefinition l243 = catalog.Find(243)!;
            int liana = CountLiana(l243);
            Assert.That(liana, Is.GreaterThanOrEqualTo(0), "el 243 lleva su Liana de cuenta");
            Assert.That(l243.Attrs[liana].N, Is.EqualTo(1), "n = 1 (mundo_06_v1.json)");
            Assert.That(catalog.Find(283)!.HasMechanic(MechanicType.HojaAtada), Is.True);
        }

        [Test]
        public void Worlds7To9Debuts_InThePublishedVariant_AreTheOnlyNotSkippableLevels()
        {
            // 03 §11 y SightingRule.VariantDebutLevels (VA1b): en 301-700 estrenan el Brote de dos hojas (343, brote_n2 [P]) y la
            // Flor (403, flor) en las dos variantes, y la Tinaja (323, tinaja; D-019) solo en la «a». Con la «b» publicada (salida
            // B de D-019 Q-Ti3), ningún nivel de 301-700 lleva la Tinaja y el 323 es un nivel normal (en la b, Ma).
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            bool tinaja = LevelCatalog.TinajaPublished;
            for (int level = 301; level <= 700; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                string? intro = level == 343 ? AF.GameFlow.LevelCardInfo.BroteTwoLeavesIntro
                    : level == 403 ? AF.GameFlow.LevelCardInfo.FlorIntro
                    : level == 323 && tinaja ? "tinaja" : null;
                Assert.That(def.IntroMechanic, Is.EqualTo(intro), "intro_mechanic del " + level + " (variante " + LevelCatalog.PublishedVariant + ")");
                Assert.That(def.Skippable, Is.EqualTo(intro == null), "skippable es false justo en los estrenos (" + level + ")");
                Assert.That(def.World, Is.EqualTo(WorldTable.WorldOf(level)));
                if (!tinaja || level < 323)
                {
                    Assert.That(def.HasMechanic(MechanicType.Tinaja), Is.False, "sin la Tinaja: " + level);
                }

                if (level < 343)
                {
                    Assert.That(BroteTwoLeaves(def), Is.False, "Brote de dos hojas antes del 343: " + level);
                }

                if (level < 403)
                {
                    Assert.That(def.HasMechanic(MechanicType.Flor), Is.False, "Flor antes del 403: " + level);
                }
            }

            Assert.That(BroteTwoLeaves(catalog.Find(343)!), Is.True, "el 343 lleva su Brote de dos hojas");
            Assert.That(catalog.Find(403)!.HasMechanic(MechanicType.Flor), Is.True, "el 403 lleva su Flor");
        }

        [Test]
        public void VariantA_Debuts323_343_403_ArriveNotSkippable_AndEveryBoardBuildsAndFits()
        {
            // La variante «a» de los mundos 7-9 (GEN79, D-027), con la Tinaja: la publica C1c si se cumple la condición de D-019.
            // Sus estrenos (la Tinaja en el 323, el Brote de dos hojas en el 343 y la Flor en el 403) llegan al cliente con
            // skippable = false, y todos sus tableros (base y relief, con la máscara de la Tinaja) construyen, pasan
            // LevelValidation y caben en el tablero (≤ 16 cuias, D-006).
            var packs = new[] { TestPacks.WorldVariantA(7), TestPacks.WorldVariantA(8), TestPacks.WorldVariantA(9) };
            LevelCatalog catalog = LevelCatalog.Create(packs);
            Assert.That(catalog.FirstLevel, Is.EqualTo(301));
            Assert.That(catalog.LastLevel, Is.EqualTo(700));
            Assert.That(catalog.IsContiguous, Is.True, catalog.DescribeGaps());
            int tinajaLevels = 0;
            int variants = 0;
            for (int level = 301; level <= 700; level++)
            {
                LevelDefinition def = catalog.Find(level)!;
                string? intro = level == 323 ? "tinaja"
                    : level == 343 ? AF.GameFlow.LevelCardInfo.BroteTwoLeavesIntro
                    : level == 403 ? AF.GameFlow.LevelCardInfo.FlorIntro : null;
                Assert.That(def.IntroMechanic, Is.EqualTo(intro), "intro_mechanic del " + level + " (variante a)");
                Assert.That(def.Skippable, Is.EqualTo(intro == null), "skippable es false justo en los estrenos (" + level + ", variante a)");
                if (def.HasMechanic(MechanicType.Tinaja))
                {
                    Assert.That(level, Is.GreaterThanOrEqualTo(323), "la Tinaja estrena en el 323 (D-019)");
                    tinajaLevels++;
                }

                BoardState board = def.CreateBoard();
                Assert.That(LevelValidation.Validate(board), Is.Null, def.Id);
                Assert.That(new LevelSession(board, true, PourModes.Default).Status, Is.EqualTo(LevelStatus.Playing), def.Id);
                Assert.That(def.CuiaCount, Is.LessThanOrEqualTo(BoardState.MaxCuias), "D-006: " + def.Id);
                for (int r = 0; r < def.Relief.Count; r++)
                {
                    ReliefBoard relief = def.Relief[r];
                    string where = def.Id + ".relief[" + r + "] " + relief.Type;
                    BoardState rb = relief.CreateBoard();
                    Assert.That(LevelValidation.Validate(rb), Is.Null, where);
                    Assert.That(relief.CuiaCount, Is.LessThanOrEqualTo(BoardState.MaxCuias), "D-006: " + where);
                    variants++;
                }
            }

            Assert.That(catalog.Find(323)!.HasMechanic(MechanicType.Tinaja), Is.True, "el 323 estrena la Tinaja");
            Assert.That(tinajaLevels, Is.EqualTo(18), "la Tinaja en 18 niveles de 301-700 (10 en el mundo 7, 3 en el 8 y 5 en el 9)");
            Assert.That(variants, Is.EqualTo(774));
        }

        [Test]
        public void PublishedVariant_IsTheGeneratorsPublishedVariant_AndTheRegistrys()
        {
            // d.5.8: lo que publica el cliente (LevelCatalog.PublishedVariant, la variante de los paquetes de Data/Levels/ que
            // escribe el generador) es el published_variant de campaign_v2.json y la variante del registro de combinaciones
            // versionado (d.5.6), para que los mundos siguientes se generen contra el historial publicado.
            string specs = Path.Combine(TestPacks.RepoRoot, "tools", "level-generator", "specs");
            string spec = File.ReadAllText(Path.Combine(specs, "campaign_v2.json"));
            string registry = File.ReadAllText(Path.Combine(specs, "combinations_registry_v1.json"));
            Assert.That(Regex.Match(spec, "\"published_variant\"\\s*:\\s*\"([a-z])\"").Groups[1].Value, Is.EqualTo(LevelCatalog.PublishedVariant),
                "published_variant de campaign_v2.json");
            Assert.That(Regex.Match(registry, "\"variant\"\\s*:\\s*\"([a-z])\"").Groups[1].Value, Is.EqualTo(LevelCatalog.PublishedVariant),
                "variant de combinations_registry_v1.json");
            Assert.That(Directory.Exists(Path.Combine(Application.dataPath, "_Project", "Data", "Levels", "salida_b")), Is.False,
                "una sola copia de 301-700 en Assets/: la publicada, en Data/Levels/");
        }

        [Test]
        public void Worlds7To9_EveryBoardAndRelief_FitsTheBoardAt360x780()
        {
            // Criterio de C1b «ningún nivel desborda el tablero», sacado de los paquetes y no de una lista: cada tablero de 301-700
            // (base y relief) de la variante publicada y de la «a» cabe a 360 × 780 dp (el teléfono de pruebas; BoardLayoutTests:
            // tablero entre 88 y 620 dp) con el layout de la escena (D-006: ≤ 16 ranuras, 2 o 3 filas, cuia ≥ 28 dp y todo el
            // vidrio dentro del panel). Incluye los de 14 cuias (hitos de 561-700 y los del mundo 7) y los de 13 cuias de capacidad
            // 5 con su alivio de 14, los más altos. Worlds7to9ContentTests lo repite en la escena con esos tableros.
            var catalogs = new[]
            {
                LevelCatalog.Create(TestPacks.Campaign),
                LevelCatalog.Create(new[] { TestPacks.WorldVariantA(7), TestPacks.WorldVariantA(8), TestPacks.WorldVariantA(9) }),
            };
            string[] names = { LevelCatalog.PublishedVariant, LevelCatalog.VariantA };
            int fourteen = 0;
            int tallFourteen = 0;
            for (int c = 0; c < catalogs.Length; c++)
            {
                for (int level = 301; level <= 700; level++)
                {
                    LevelDefinition def = catalogs[c].Find(level)!;
                    string what = def.Id + " (variante " + names[c] + ")";
                    BoardState board = def.CreateBoard();
                    AssertFitsAt360x780(board, what);
                    fourteen += c == 0 && board.Count == 14 ? 1 : 0;
                    for (int r = 0; r < def.Relief.Count; r++)
                    {
                        BoardState rb = def.Relief[r].CreateBoard();
                        AssertFitsAt360x780(rb, what + ".relief[" + r + "] " + def.Relief[r].Type);
                        tallFourteen += c == 0 && rb.Count == 14 && MaxCapacity(rb) > 4 ? 1 : 0;
                    }
                }
            }

            Assert.That(fourteen, Is.GreaterThan(0), "la variante publicada tiene tableros de 14 cuias");
            Assert.That(tallFourteen, Is.GreaterThan(0), "y alivios de 14 cuias de capacidad 5");
        }

        // 360 × 780 dp: el tablero entre 88 y 620 dp (BoardLayoutTests; guía 3.2).
        private const float FitTop = 88f;
        private const float FitBottom = 620f;

        // El tablero cabe con el layout de la escena (GameScreen.LayoutBoardPanel: el alto de la cuia más alta y la pareja de
        // la Tinaja), como el AssertFits de Worlds7to9ContentTests.
        private static void AssertFitsAt360x780(BoardState board, string what)
        {
            int count = board.Count;
            Assert.That(count, Is.LessThanOrEqualTo(BoardState.MaxCuias), "D-006: ≤ 16 ranuras (" + what + ")");
            float heightU = CuiaGeometry.HeightU(MaxCapacity(board));
            BoardLayoutResult layout = BoardLayout.Compute(count, count, heightU, 360f, FitTop, FitBottom, BoardLayout.TinajaPairStart(board));
            Assert.That(layout.Rows, Is.LessThanOrEqualTo(3), what + ": como mucho 3 filas (D-006)");
            Assert.That(layout.CuiaWidth, Is.GreaterThanOrEqualTo(BoardLayout.MinCuiaWidth), what + ": cuia ≥ 28 dp");
            float left = layout.PanelX;
            float right = layout.PanelX + layout.PanelWidth;
            for (int i = 0; i < count; i++)
            {
                float half = 0.5f * layout.CuiaWidth;
                if (layout.CenterX[i] - half < left - 0.01f || layout.CenterX[i] + half > right + 0.01f
                    || layout.FootY[i] > layout.PanelBottom + 0.01f || layout.FootY[i] - layout.CuiaHeight < layout.PanelY - 0.01f)
                {
                    Assert.Fail(what + ": la cuia " + i + " se sale del panel");
                }
            }
        }

        private static int MaxCapacity(BoardState board)
        {
            int max = 0;
            for (int i = 0; i < board.Count; i++)
            {
                max = Mathf.Max(max, board[i].Capacity);
            }

            return max;
        }

        // El nivel lleva un Brote de dos hojas (n = 2).
        private static bool BroteTwoLeaves(LevelDefinition def)
        {
            for (int i = 0; i < def.CuiaCount; i++)
            {
                if (def.Attrs[i].Type == MechanicType.Brote && def.Attrs[i].N == 2)
                {
                    return true;
                }
            }

            return false;
        }

        // Índice de la primera Liana de cuenta (n > 0) del nivel, o -1.
        private static int CountLiana(LevelDefinition def)
        {
            for (int i = 0; i < def.CuiaCount; i++)
            {
                if (def.Attrs[i].Type == MechanicType.Liana && def.Attrs[i].N > 0)
                {
                    return i;
                }
            }

            return -1;
        }

        [Test]
        public void Level100_IsMurkyWithTwoHidden()
        {
            LevelDefinition def = LevelCatalog.Create(TestPacks.Campaign).Find(100)!;
            Assert.That(def.IsMurky, Is.True);
            Assert.That(def.HiddenCount, Is.EqualTo(2));
            Assert.That(def.Hidden[0], Is.EqualTo(new[] { 0, 0 }));
            Assert.That(def.Hidden[1], Is.EqualTo(new[] { 0, 1 }));
            Assert.That(def.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelVeryHard));
            Assert.That(def.Band, Is.EqualTo(LevelDefinition.BandMilestone));
            BoardState board = def.CreateBoard();
            Assert.That(board.HiddenCount, Is.EqualTo(2));
            Assert.That(board[0].IsHiddenAt(0) && board[0].IsHiddenAt(1), Is.True);
            Assert.That(board[0].IsHiddenAt(2), Is.False, "R7: la cima es visible");
        }

        [Test]
        public void Level27_UnitsObject()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            LevelDefinition def = catalog.Find(27)!;
            Assert.That(def.Units.Of(10), Is.EqualTo(5), "02 §8: {\"10\": 5}, el tinte 10 tiene 5 dosis");
            Assert.That(def.Units.Of(7), Is.EqualTo(4));
            Assert.That(def.HasTallCuia, Is.True);
            Assert.That(def.CreateBoard().UnitsOf(10), Is.EqualTo(5));

            LevelDefinition l160 = catalog.Find(160)!;
            Assert.That(l160.HasTallCuia, Is.True, "nivel 160: Cuia alta de 6");
            Assert.That(l160.Units.Of(7), Is.EqualTo(6));
        }
    }
}
