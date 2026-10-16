#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Los 6 paquetes de la escena (tutorial_v0 y mundo_02..05_v1, <c>pack_schema</c> 2; traspaso §6 pasos 1.1-1.2; y
    /// mundo_06_v1, <c>pack_schema</c> 3, desde la tarea C1a; 08 §4.1 y §4.4; contrato 21): cubren los niveles 1-300 sin huecos,
    /// cada tablero (base y relief) construye con las mecánicas del núcleo, pasa <see cref="LevelValidation"/> y arranca una
    /// sesión jugable, los estrenos de D-007 (02 §9.1 y §11; 02 §8) están donde dice el calendario y los del mundo 6, la Liana
    /// de cuenta en el 243 y la Hoja atada en el 283 (03 §11; D-023, punto 5), también.
    /// </summary>
    public sealed class CampaignPackTests
    {
        // D-007: nivel del estreno → intro_mechanic (Cuia chica 14, Cuia alta 27, Hoja 43, Matapi 63, Liana con fruta 83, Aguas
        // turbias 100, Remolino 103, Barro 123, Lluvia 143, Brote 163 y Piraña 183). La lista es la única del código (T-F):
        // DebutIntros va en su mismo orden.
        private static readonly int[] DebutLevels = AF.GameFlow.SightingRule.MechanicDebutLevels;
        private static readonly string[] DebutIntros = { "chica", "alta", "hoja", "matapi", "liana", "murky", "remolino", "barro", "lluvia", "brote", "pirana" };

        [Test]
        public void Packs_LoadAndCover1To300()
        {
            IReadOnlyList<LevelPack> packs = TestPacks.Campaign;
            Assert.That(packs.Count, Is.EqualTo(6));
            LevelCatalog catalog = LevelCatalog.Create(packs);
            Assert.That(catalog.FirstLevel, Is.EqualTo(1));
            Assert.That(catalog.LastLevel, Is.EqualTo(300));
            Assert.That(catalog.Count, Is.EqualTo(300));
            Assert.That(catalog.IsContiguous, Is.True, catalog.DescribeGaps());
            Assert.That(catalog.NextLevelAfter(200), Is.EqualTo(201), "el 200 ya no es el fin de contenido");
            Assert.That(catalog.NextLevelAfter(300), Is.Null, "tras el 300, «El río sigue creciendo» (06 §9)");

            for (int level = 1; level <= 300; level++)
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
        }

        [Test]
        public void Boards_BuildAndValidate_All290()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            int checkedLevels = 0;
            for (int level = 11; level <= 300; level++)
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
                checkedLevels++;
            }

            Assert.That(checkedLevels, Is.EqualTo(290));
        }

        [Test]
        public void Relief_BuildAndValidate_All532()
        {
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            int variants = 0;
            for (int level = 1; level <= 300; level++)
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
                    Assert.That(relief.Attrs.Count, Is.EqualTo(relief.CuiaCount), where);
                    Assert.That(LevelValidation.Validate(board), Is.Null, where);
                    Assert.That(board.HasHidden, Is.False, "relief sin ocultos: " + where);
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

            Assert.That(variants, Is.EqualTo(334 + 198), "334 en 1-200 y 198 en el mundo 6 (100 de un color menos y 98 de una vacía más)");
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
            // liana_n [P]) y la Hoja atada (283, hoja_atada; D-023, punto 5); son los únicos niveles no saltables del mundo 6.
            LevelCatalog catalog = LevelCatalog.Create(TestPacks.Campaign);
            Assert.That(AF.GameFlow.SightingRule.VariantDebutLevels, Is.EqualTo(new[] { 243, 283 }));
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
