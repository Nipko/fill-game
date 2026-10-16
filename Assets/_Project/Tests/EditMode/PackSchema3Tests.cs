#nullable enable
using System.Collections.Generic;
using System.IO;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Levels;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El cliente lee <c>pack_schema</c> 3 (TJ1; 08 §4.1 y §4.4; contrato 21; D-019, punto 9, y D-023, punto 5): la Tinaja y su
    /// cuia (M13), la Hoja atada, el Brote con <c>n</c> (Brote de dos hojas) y la Flor de vitória-régia con <c>key2</c>, más los
    /// <c>intro_mechanic</c> del 3; rechaza todo eso con el 1 y el 2, y rechaza un paquete 4. La muestra
    /// <c>Fixtures/pack3_sample.json</c> copia niveles de las muestras del generador (P1: <c>tools/out/sample_tinaja</c>; P5:
    /// <c>tools/out/sample_variants</c>).
    /// </summary>
    public sealed class PackSchema3Tests
    {
        private static string SamplePath => Path.Combine(Application.dataPath, "_Project/Tests/EditMode/Fixtures/pack3_sample.json");

        private static string World6Path => Path.Combine(Application.dataPath, "_Project/Data/Levels/mundo_06_v1.json");

        private static LevelPack? _sample;

        private static LevelPack Sample => _sample ??= LevelPackReader.Read(File.ReadAllText(SamplePath));

        [Test]
        public void Reader_AcceptsPackSchema3_08_4_4()
        {
            Assert.That(LevelPackReader.SupportedPackSchema, Is.EqualTo(3));
            Assert.That(LevelPackReader.Read(TestPacks.MinimalPack(null, "\"pack_schema\": 3")).Count, Is.EqualTo(1));
        }

        [Test]
        public void Reader_RejectsPackSchema4_WithAClearMessage_08_4_4()
        {
            // 08 §4.4: el cliente rechaza un paquete con un pack_schema mayor del que conoce; el mensaje dice el paquete, la
            // versión que no admite y las que sí lee.
            Assert.That(() => LevelPackReader.Read(TestPacks.MinimalPack(null, "\"pack_schema\": 4")),
                Throws.TypeOf<LevelFormatException>()
                    .With.Message.Contains("t: pack_schema 4 no admitido")
                    .And.Message.Contains("versiones 1 a 3")
                    .And.Message.Contains("08 §4.4: hace falta una versión más nueva del juego"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Reader_RejectsPackSchemaBelow1_WithoutAskingForANewerGame(int schema)
        {
            // Un pack_schema menor que 1 es un paquete mal formado, no uno demasiado nuevo: el mensaje no pide actualizar el juego.
            string? message = null;
            try
            {
                LevelPackReader.Read(TestPacks.MinimalPack(null, "\"pack_schema\": " + schema.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            catch (LevelFormatException e)
            {
                message = e.Message;
            }

            Assert.That(message, Is.Not.Null);
            Assert.That(message, Does.Contain("t: pack_schema " + schema.ToString(System.Globalization.CultureInfo.InvariantCulture) + " no admitido"));
            Assert.That(message, Does.Contain("versiones 1 a 3"));
            Assert.That(message, Does.Not.Contain("más nueva"));
        }

        [Test]
        public void Sample_LoadsEveryLevelAndEveryRelief()
        {
            LevelPack pack = Sample;
            Assert.That(pack.PackId, Is.EqualTo("mundo_07_v1"));
            int[] expected = { 243, 263, 283, 303, 306, 323, 329 };
            Assert.That(pack.Count, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                LevelDefinition level = pack.Levels[i];
                Assert.That(level.Level, Is.EqualTo(expected[i]));
                Assert.That(level.CreateBoard().Count, Is.EqualTo(level.CuiaCount), level.Id);
                Assert.That(level.Relief.Count, Is.EqualTo(2), level.Id);
                for (int r = 0; r < level.Relief.Count; r++)
                {
                    Assert.That(level.Relief[r].CreateBoard().Count, Is.EqualTo(level.Relief[r].CuiaCount), level.Id + " relief " + r);
                }
            }
        }

        [Test]
        public void Sample_FlorKeepsKey2_D023()
        {
            // D-023, punto 5: flor {key, key2} con key ≠ key2. El 303 estrena la Flor con las llaves 13 y 5 en la cuia 10.
            LevelDefinition flor = Sample.Find(303)!;
            Assert.That(flor.IntroMechanic, Is.EqualTo("flor"));
            Assert.That(flor.Attrs[10], Is.EqualTo(new MechanicSpec(MechanicType.Flor, key: 13, key2: 5)));
            Cuia cuia = flor.CreateBoard()[10];
            Assert.That(cuia.Mechanic, Is.EqualTo(MechanicType.Flor));
            Assert.That(cuia.MechanicKey, Is.EqualTo(13));
            Assert.That(cuia.MechanicKey2, Is.EqualTo(5));

            // Y en las variantes de relief, que repiten la Flor con sus dos llaves.
            for (int r = 0; r < flor.Relief.Count; r++)
            {
                Assert.That(flor.Relief[r].Attrs[10].Key2, Is.EqualTo(5));
                Assert.That(flor.Relief[r].CreateBoard()[10].MechanicKey2, Is.EqualTo(5));
            }

            // El 306 lleva la Flor (2 y 14) y un Brote de dos hojas en el mismo tablero.
            BoardState both = Sample.Find(306)!.CreateBoard();
            Assert.That(both[5].MechanicKey, Is.EqualTo(2));
            Assert.That(both[5].MechanicKey2, Is.EqualTo(14));
            Assert.That(both[10].Mechanic, Is.EqualTo(MechanicType.Brote));
            Assert.That(both[10].MechanicN, Is.EqualTo(2));
        }

        [Test]
        public void Sample_BroteWithN2_IsTheTwoLeafBrote_D023()
        {
            // Propuesta de D-007 b.3; D-023, punto 5: el Brote de dos hojas es brote con n = 2 (debut brote_n2).
            LevelDefinition brote = Sample.Find(283)!;
            Assert.That(brote.IntroMechanic, Is.EqualTo("brote_n2"));
            Assert.That(brote.Attrs[1], Is.EqualTo(new MechanicSpec(MechanicType.Brote, n: 2)));
            Cuia cuia = brote.CreateBoard()[1];
            Assert.That(cuia.Mechanic, Is.EqualTo(MechanicType.Brote));
            Assert.That(cuia.MechanicN, Is.EqualTo(2));
            Assert.That(cuia.MechanicFlags & Cuia.FlagClosed, Is.EqualTo(Cuia.FlagClosed));
        }

        [Test]
        public void Schema3_BroteWithN1_IsTheNormalBrote()
        {
            // Con el 3, n = 1 es el Brote normal: el mismo tablero que sin n (BoardBuilder deja n = 0 en la cuia).
            string withN = TestPacks.Schema2Pack(Brote("{\"type\": \"brote\", \"n\": 1}"), BroteCuias, schema: 3);
            string withoutN = TestPacks.Schema2Pack(Brote("{\"type\": \"brote\"}"), BroteCuias, schema: 3);
            BoardState a = LevelPackReader.Read(withN).First.CreateBoard();
            BoardState b = LevelPackReader.Read(withoutN).First.CreateBoard();
            Assert.That(a[3].Mechanic, Is.EqualTo(MechanicType.Brote));
            Assert.That(a[3].MechanicN, Is.EqualTo(0));
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void Sample_HojaAtadaAndLianaDeCuenta()
        {
            LevelDefinition atada = Sample.Find(263)!;
            Assert.That(atada.IntroMechanic, Is.EqualTo("hoja_atada"));
            Assert.That(atada.Attrs[9], Is.EqualTo(new MechanicSpec(MechanicType.HojaAtada, key: 11)));
            Assert.That(atada.CreateBoard()[9].Mechanic, Is.EqualTo(MechanicType.HojaAtada));
            Assert.That(atada.CreateBoard()[9].MechanicKey, Is.EqualTo(11));

            LevelDefinition liana = Sample.Find(243)!;
            Assert.That(liana.IntroMechanic, Is.EqualTo("liana_n"));
            Assert.That(liana.Attrs[9], Is.EqualTo(new MechanicSpec(MechanicType.Liana, n: 1)));
        }

        [Test]
        public void Sample_TinajaLoadsWithItsMask_AndIsNotMurkyWater_M13()
        {
            // M13 (propuesta de la tinaja, b.2 regla 1 y c.6): murky = true y hidden = todas las capas de la tinaja menos la boca.
            // El modificador murky ya no es Aguas turbias: IsMurky es falso, HasHiddenDoses y HasTinaja, verdaderos.
            LevelDefinition debut = Sample.Find(323)!;
            Assert.That(debut.IntroMechanic, Is.EqualTo("tinaja"));
            Assert.That(debut.Skippable, Is.False);
            Assert.That(debut.HasTinaja, Is.True);
            Assert.That(debut.HasHiddenDoses, Is.True);
            Assert.That(debut.IsMurky, Is.False);
            Assert.That(debut.HiddenCount, Is.EqualTo(2));
            BoardState board = debut.CreateBoard();
            Assert.That(board[2].Mechanic, Is.EqualTo(MechanicType.Tinaja));
            Assert.That(board[1].Mechanic, Is.EqualTo(MechanicType.Cucharon));
            Assert.That(board[2].IsHiddenAt(0), Is.True);
            Assert.That(board[2].IsHiddenAt(1), Is.True);
            Assert.That(board[2].IsHiddenAt(2), Is.False, "la boca (R7)");
            Assert.That(board.HiddenCount, Is.EqualTo(2));

            // La variante de relief repite la tinaja con su máscara (08 §4.1; decisión del dueño del 2026-10-14, opción a).
            for (int r = 0; r < debut.Relief.Count; r++)
            {
                ReliefBoard relief = debut.Relief[r];
                Assert.That(relief.HiddenCount, Is.EqualTo(2), relief.Type);
                BoardState reliefBoard = relief.CreateBoard();
                Assert.That(reliefBoard[2].Mechanic, Is.EqualTo(MechanicType.Tinaja));
                Assert.That(reliefBoard[2].IsHiddenAt(0) && reliefBoard[2].IsHiddenAt(1), Is.True, relief.Type);
                Assert.That(reliefBoard.HiddenCount, Is.EqualTo(2), relief.Type);
            }
        }

        [Test]
        public void Sample_TinajaWithFourHidden_UsesItsOwnCap_M13()
        {
            // El 329 tiene la tinaja llena de 5 con 4 ocultas: el tope propio de la tinaja (LevelValidation.TinajaMaxHiddenUnits).
            LevelDefinition full = Sample.Find(329)!;
            Assert.That(full.HiddenCount, Is.EqualTo(LevelValidation.TinajaMaxHiddenUnits));
            Assert.That(full.CreateBoard()[9].Count, Is.EqualTo(LevelValidation.TinajaCapacity));
            Assert.That(full.IsMurky, Is.False);
        }

        [Test]
        public void World6_LoadsAsPackSchema3_201To300()
        {
            // mundo_06_v1 (P7a, 201-300) es el primer pack 3 publicable: la Liana de cuenta en el 243 y la Hoja atada en el 283.
            LevelPack pack = LevelPackReader.Read(File.ReadAllText(World6Path));
            Assert.That(pack.PackId, Is.EqualTo("mundo_06_v1"));
            Assert.That(pack.Count, Is.EqualTo(100));
            Assert.That(pack.Levels[0].Level, Is.EqualTo(201));
            Assert.That(pack.Levels[99].Level, Is.EqualTo(300));
            Assert.That(pack.Find(243)!.IntroMechanic, Is.EqualTo("liana_n"));
            Assert.That(pack.Find(283)!.IntroMechanic, Is.EqualTo("hoja_atada"));
            Assert.That(pack.Find(283)!.HasMechanic(MechanicType.HojaAtada), Is.True);
            for (int i = 0; i < pack.Count; i++)
            {
                LevelDefinition level = pack.Levels[i];
                Assert.That(level.HasTinaja, Is.False, level.Id);
                Assert.That(level.IsMurky, Is.EqualTo(level.HasHiddenDoses), level.Id);
            }
        }

        [Test]
        public void Packs1To200_LoadAsBefore_MurkyIsStillAguasTurbias()
        {
            // Los packs 1-200 no cambian: ninguno lleva la tinaja, así que IsMurky sigue siendo el murky del paquete, y ninguna
            // variante de relief lleva ocultas.
            IReadOnlyList<LevelPack> campaign = TestPacks.Campaign;
            int murky = 0;
            int levels = 0;
            for (int p = 0; p < campaign.Count; p++)
            {
                if (campaign[p].First.Level > 200)
                {
                    continue; // mundo_06_v1 (pack_schema 3) también está en la escena desde C1a: World6_LoadsAsPackSchema3_201To300
                }

                for (int i = 0; i < campaign[p].Count; i++)
                {
                    LevelDefinition level = campaign[p].Levels[i];
                    levels++;
                    Assert.That(level.HasTinaja, Is.False, level.Id);
                    Assert.That(level.IsMurky, Is.EqualTo(level.HasHiddenDoses), level.Id);
                    Assert.That(level.IsMurky, Is.EqualTo(level.HiddenCount > 0), level.Id);
                    murky += level.IsMurky ? 1 : 0;
                    for (int r = 0; r < level.Relief.Count; r++)
                    {
                        Assert.That(level.Relief[r].HiddenCount, Is.EqualTo(0), level.Id);
                    }
                }
            }

            Assert.That(levels, Is.EqualTo(200));
            Assert.That(murky, Is.GreaterThan(0), "Aguas turbias desde el 100");
        }

        // Paquetes de un nivel que el lector rechaza por la versión (contrato 21): lo que solo existe con el 3 falla con el 1 y
        // el 2, y lo que el 3 restringe falla con el 3. Cada caso lleva el fragmento del mensaje que prueba su motivo.
        private static readonly TestCaseData[] InvalidByVersion =
        {
            Invalid("tinaja con pack_schema 2", TinajaPack(2), "el tipo tinaja necesita pack_schema 3"),
            Invalid("cucharon con pack_schema 2", TestPacks.Schema2Pack("[null, null, null, {\"type\": \"cucharon\"}]"), "el tipo cucharon necesita pack_schema 3"),
            Invalid("hoja_atada con pack_schema 2", TestPacks.Schema2Pack("[null, null, null, {\"type\": \"hoja_atada\", \"key\": 4}]"), "el tipo hoja_atada necesita pack_schema 3"),
            Invalid("flor con pack_schema 2", TestPacks.Schema2Pack("[null, null, null, {\"type\": \"flor\", \"key\": 4, \"key2\": 8}]"), "el tipo flor necesita pack_schema 3"),
            Invalid("tinaja con pack_schema 1", TinajaPack(1), "el tipo tinaja necesita pack_schema 3"),
            Invalid("key2 con pack_schema 2", TestPacks.Schema2Pack("[null, null, {\"type\": \"hoja\", \"key\": 4, \"key2\": 8}, null]"), "'key2' necesita pack_schema 3"),
            Invalid("Brote de dos hojas con pack_schema 2", TestPacks.Schema2Pack(Brote("{\"type\": \"brote\", \"n\": 2}"), BroteCuias), "el Brote de dos hojas (n = 2) necesita pack_schema 3"),
            Invalid("intro tinaja con pack_schema 2", TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"tinaja\""), "'intro_mechanic' tinaja necesita pack_schema 3"),
            Invalid("intro liana_n con pack_schema 2", TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"liana_n\""), "'intro_mechanic' liana_n necesita pack_schema 3"),
            Invalid("intro hoja_atada con pack_schema 2", TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"hoja_atada\""), "'intro_mechanic' hoja_atada necesita pack_schema 3"),
            Invalid("intro brote_n2 con pack_schema 2", TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"brote_n2\""), "'intro_mechanic' brote_n2 necesita pack_schema 3"),
            Invalid("intro flor con pack_schema 1", TestPacks.Schema2Pack(extra: ", \"intro_mechanic\": \"flor\"", schema: 1), "'intro_mechanic' flor necesita pack_schema 3"),
            Invalid("hidden en relief con pack_schema 2", WithRelief(TestPacks.Schema2Pack(), PlusEmptyRelief(", \"hidden\": [[0, 0]]")), "relief[0]: 'hidden' en una variante de relief necesita pack_schema 3"),
            Invalid("Brote con n 3", TestPacks.Schema2Pack(Brote("{\"type\": \"brote\", \"n\": 3}"), BroteCuias, schema: 3), "el n del Brote es 1 o 2"),
            Invalid("Brote con n 0", TestPacks.Schema2Pack(Brote("{\"type\": \"brote\", \"n\": 0}"), BroteCuias, schema: 3), "el n del Brote es 1 o 2"),
            Invalid("key2 fuera de la Flor", TestPacks.Schema2Pack("[null, null, {\"type\": \"hoja\", \"key\": 4, \"key2\": 8}, null]", schema: 3), "key2 solo lo lleva la Flor"),
            Invalid("Flor con dos llaves iguales", TestPacks.Schema2Pack("[null, null, null, {\"type\": \"flor\", \"key\": 4, \"key2\": 4}]", schema: 3), "dos tintes distintos"),
            Invalid("Flor sin key2", TestPacks.Schema2Pack("[null, null, null, {\"type\": \"flor\", \"key\": 4}]", schema: 3), "dos tintes distintos"),
            Invalid("hidden en relief sin la tinaja", WithRelief(TestPacks.Schema2Pack(schema: 3), PlusEmptyRelief(", \"hidden\": [[0, 0]]")), "relief[0]: una variante de relief solo lleva 'hidden' con la tinaja"),
            Invalid("relief con la tinaja sin su máscara", WithRelief(TinajaPack(3), TinajaRelief(string.Empty)), "relief[0]: una variante de relief con la tinaja lleva su máscara en 'hidden'"),
            Invalid("relief con la tinaja y una máscara que no es la suya", WithRelief(TinajaPack(3), TinajaRelief(", \"hidden\": [[8, 0]]")), "relief[0]: " + TinajaMaskReason),
            Invalid("tinaja con 5 ocultas (su tope propio)", TinajaPack(3, "[[8, 0], [8, 1], [8, 2], [8, 3], [8, 4]]"), "la tinaja oculta como mucho 4 (su tope propio"),
            Invalid("tinaja sin su máscara (murky = false, hidden vacío)", TinajaPack(3, "[]", "false"), "m01_v1_n0001: " + TinajaMaskReason),
            Invalid("tinaja con su máscara y murky = false", TinajaPack(3, murky: "false"), "'hidden' solo puede tener dosis ocultas con murky = true"),
        };

        [TestCaseSource(nameof(InvalidByVersion))]
        public void Reader_RejectsWhatTheVersionDoesNotAdmit_Contract21(string json, string expected)
        {
            Assert.That(() => LevelPackReader.Read(json),
                Throws.TypeOf<LevelFormatException>().With.Message.Contains("m01_v1_n0001").And.Message.Contains(expected));
        }

        [Test]
        public void TinajaPack_IsValid_WithAndWithoutRelief()
        {
            // Las bases de los casos negativos de la tinaja leen bien: así cada caso falla solo por lo que dice su nombre.
            LevelDefinition level = LevelPackReader.Read(WithRelief(TinajaPack(3), TinajaRelief(", \"hidden\": [[8, 0], [8, 1]]"))).First;
            Assert.That(level.HasTinaja, Is.True);
            Assert.That(level.IsMurky, Is.False);
            Assert.That(level.Relief[0].HiddenCount, Is.EqualTo(2));
            Assert.That(level.Relief[0].CreateBoard()[8].IsHiddenAt(1), Is.True);
            LevelDefinition plain = LevelPackReader.Read(TinajaPack(3)).First;
            Assert.That(plain.HasTinaja, Is.True);
            Assert.That(plain.Relief.Count, Is.EqualTo(0));
            Assert.That(LevelPackReader.Read(TestPacks.Schema2Pack(Brote("{\"type\": \"brote\"}"), BroteCuias)).First.HasMechanic(MechanicType.Brote), Is.True);
        }

        // Motivo de M13 regla 1 (TinajaMechanic.ValidateInitial, a través de LevelValidation) cuando la máscara de la tinaja
        // (cuia 8, con 3 dosis) no es todas sus capas menos la boca.
        private const string TinajaMaskReason = "cuia 8 (tinaja): M13 (regla 1): todas las dosis de la tinaja están ocultas salvo la boca";

        // Brote en la cuia 3 con 2 dosis y espacio para 2 (02 §9.1 M2: empieza con 1-3 dosis; el de dos hojas, con espacio 2).
        private const string BroteCuias = "[[8, 8, 4], [4, 4, 8], [], [8, 4]]";

        private static string Brote(string attr)
        {
            return "[null, null, null, " + attr + "]";
        }

        // Ejemplo de nivel de b.2 (TinajaTests.BuildB2Example del núcleo): caps [4 × 8, 5, 2], la tinaja en la cuia 8 con
        // [3, 8, 6] (ocultas [[8, 0], [8, 1]]) y su cuia en la 9.
        private const string TinajaCaps = "[4, 4, 4, 4, 4, 4, 4, 4, 5, 2]";
        private const string TinajaCuias = "[[1, 4, 10, 6], [10, 3, 1, 4], [4, 8, 10, 1], [6, 1, 8], [3, 10, 4], [8, 6, 3], [], [], [3, 8, 6], []]";
        private const string TinajaAttrs = "[null, null, null, null, null, null, null, null, {\"type\": \"tinaja\"}, {\"type\": \"cucharon\"}]";

        private static string TinajaPack(int schema, string hidden = "[[8, 0], [8, 1]]", string murky = "true")
        {
            return TestPacks.Schema2Pack(TinajaAttrs, TinajaCuias, TinajaCaps, murky, hidden, schema: schema);
        }

        // plus_empty del ejemplo de b.2: el mismo tablero con una cuia vacía más, la tinaja y su cuia, y lo que añade extra.
        private static string TinajaRelief(string extra)
        {
            return "{ \"type\": \"plus_empty\", \"caps\": [4, 4, 4, 4, 4, 4, 4, 4, 5, 2, 4], "
                + "\"cuias\": [[1, 4, 10, 6], [10, 3, 1, 4], [4, 8, 10, 1], [6, 1, 8], [3, 10, 4], [8, 6, 3], [], [], [3, 8, 6], [], []], "
                + "\"attrs\": [null, null, null, null, null, null, null, null, {\"type\": \"tinaja\"}, {\"type\": \"cucharon\"}, null], \"par\": null" + extra + " }";
        }

        // plus_empty del nivel por defecto de TestPacks.Schema2Pack (5 cuias, sin mecánicas), con lo que añade extra.
        private static string PlusEmptyRelief(string extra)
        {
            return "{ \"type\": \"plus_empty\", \"caps\": [4, 4, 4, 4, 4], \"cuias\": [[8, 8, 8, 4], [4, 4, 4, 8], [], [], []], \"attrs\": null, \"par\": null" + extra + " }";
        }

        private static string WithRelief(string pack, string reliefBoard)
        {
            return pack.Replace("\"relief\": []", "\"relief\": [" + reliefBoard + "]");
        }

        private static TestCaseData Invalid(string why, string json, string expected)
        {
            return new TestCaseData(json, expected).SetName("Reader_RejectsWhatTheVersionDoesNotAdmit_Contract21(" + why + ")");
        }
    }
}
