#nullable enable
using System;
using AF.Economy;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// La tabla de la Bitácora «1 de 3» (B4): las cifras de D-033 (añadido del 2026-10-20, punto 2) sin cambiar ni una, los
    /// totales del ciclo de 06 §4.2 («Los totales del ciclo no cambian»), la tasa de valor (punto 1), el formato de
    /// <c>eco_calendar_table_v2</c> [P] y lo que <see cref="LoginCalendarTable.Parse"/> rechaza.
    /// </summary>
    public sealed class LoginCalendarTableTests
    {
        // D-033, punto 2: por casilla, el canon y las tres opciones (semillas / comodín / mezcla), en el JSON estable del cliente.
        // Canon de las casillas 2, 10, 17, 19 y 27: «1 Marea + 30» (D-033, añadido del 2026-10-21; antes «3 Marea + 30»).
        private static readonly string[][] D033 =
        {
            new[] { "{\"seeds\":60}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },                   // 1
            new[] { "{\"seeds\":30,\"undo\":1}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },        // 2
            new[] { "{\"seeds\":60}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },                   // 3
            new[] { "{\"hint\":1}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                    // 4
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 5
            new[] { "{\"extra_cuia\":1}", "{\"seeds\":100}", "{\"extra_cuia\":1}", "{\"seeds\":40,\"undo\":2}" },        // 6
            new[] { "{\"seeds\":250}", "{\"seeds\":250}", "{\"seeds\":50,\"hint\":2}", "{\"seeds\":100,\"undo\":5}" },   // 7
            new[] { "{\"seeds\":60}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },                   // 8
            new[] { "{\"seeds\":60}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },                   // 9
            new[] { "{\"seeds\":30,\"undo\":1}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },        // 10
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 11
            new[] { "{\"hint\":1}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                    // 12
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 13
            new[] { "{\"seeds\":300,\"extra_cuia\":1}", "{\"seeds\":400}", "{\"seeds\":300,\"extra_cuia\":1}", "{\"seeds\":100,\"hint\":2,\"extra_cuia\":1}" }, // 14
            new[] { "{\"seeds\":60}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },                   // 15
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 16
            new[] { "{\"seeds\":30,\"undo\":1}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },        // 17
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 18
            new[] { "{\"seeds\":30,\"undo\":1}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },        // 19
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 20
            new[] { "{\"seeds\":400}", "{\"seeds\":400}", "{\"hint\":2,\"extra_cuia\":2}", "{\"seeds\":200,\"hint\":1,\"extra_cuia\":1}" }, // 21
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 22
            new[] { "{\"extra_cuia\":1}", "{\"seeds\":100}", "{\"extra_cuia\":1}", "{\"seeds\":40,\"undo\":2}" },        // 23
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 24
            new[] { "{\"hint\":1}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                    // 25
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 26
            new[] { "{\"seeds\":30,\"undo\":1}", "{\"seeds\":60}", "{\"undo\":2}", "{\"seeds\":30,\"undo\":1}" },        // 27
            new[] { "{\"seeds\":100}", "{\"seeds\":100}", "{\"hint\":1}", "{\"seeds\":40,\"undo\":2}" },                 // 28
            new[] { "{\"reveal\":1}", "{\"seeds\":100}", "{\"reveal\":1}", "{\"hint\":1}" },                             // 29
            new[] { "{\"seeds\":600}", "{\"seeds\":600}", "{\"seeds\":300,\"hint\":1,\"extra_cuia\":2}", "{\"seeds\":300,\"hint\":3}" }, // 30
        };

        [Test]
        public void Default_HasTheCellsOfD033_InTheFixedOrderSeedsBoosterMix()
        {
            LoginCalendarTable table = LoginCalendarTable.Default;
            Assert.That(D033.Length, Is.EqualTo(LoginCalendarTable.Days));
            for (int day = 1; day <= LoginCalendarTable.Days; day++)
            {
                LoginCalendarCell cell = table.Cell(day);
                string[] row = D033[day - 1];
                Assert.That(cell.Day, Is.EqualTo(day));
                Assert.That(cell.Canon.ToJson(), Is.EqualTo(row[0]), "canon de la casilla " + day);
                Assert.That(cell.Option(LoginCalendarChoice.Seeds).ToJson(), Is.EqualTo(row[1]), "semillas de la casilla " + day);
                Assert.That(cell.Option(LoginCalendarChoice.Booster).ToJson(), Is.EqualTo(row[2]), "comodín de la casilla " + day);
                Assert.That(cell.Option(LoginCalendarChoice.Mix).ToJson(), Is.EqualTo(row[3]), "mezcla de la casilla " + day);
            }
        }

        [Test]
        public void Default_FixesTheBasketsAndTheFrameOnlyInTheirMilestones()
        {
            LoginCalendarTable table = LoginCalendarTable.Default;
            for (int day = 1; day <= LoginCalendarTable.Days; day++)
            {
                LoginCalendarExtras expected = day == 7 || day == 21 ? LoginCalendarExtras.Basket
                    : day == 30 ? LoginCalendarExtras.GrandBasket | LoginCalendarExtras.Frame
                    : LoginCalendarExtras.None;
                Assert.That(table.Cell(day).Extras, Is.EqualTo(expected), "casilla " + day);
                Assert.That(table.Cell(day).IsMilestone, Is.EqualTo(day == 7 || day == 14 || day == 21 || day == 30));
            }
        }

        [Test]
        public void TheCycleTotals_DoNotChange()
        {
            // D-033, punto 2: «3.000 semillas, 3 Morpho, 3 Cuia extra, 1 Luciérnaga, 2 Canastas y 1 Canasta grande»; Marea sueltas:
            // 5 (añadido del 2026-10-21: «El ciclo pasa de 15 a 5 Mareas sueltas; las semillas del ciclo no cambian»).
            LoginCalendarTable table = LoginCalendarTable.Default;
            Assert.That(table.CanonSeeds(), Is.EqualTo(LoginCalendarTable.CanonSeedsPerCycle).And.EqualTo(3000));
            Assert.That(table.CanonUnits(BoosterKind.Undo), Is.EqualTo(5));
            Assert.That(table.CanonUnits(BoosterKind.Hint), Is.EqualTo(3));
            Assert.That(table.CanonUnits(BoosterKind.ExtraCuia), Is.EqualTo(3));
            Assert.That(table.CanonUnits(BoosterKind.Reveal), Is.EqualTo(1));
            Assert.That(table.CanonUnits(BoosterKind.Shuffle), Is.EqualTo(0));
            Assert.That(table.CountExtras(LoginCalendarExtras.Basket), Is.EqualTo(2));
            Assert.That(table.CountExtras(LoginCalendarExtras.GrandBasket), Is.EqualTo(1));
            Assert.That(table.CountExtras(LoginCalendarExtras.Frame), Is.EqualTo(1));

            // «el peso de los hitos baja del 53 % al 51,7 %»: 250 + 300 + 400 + 600 = 1.550 de 3.000.
            int milestones = 0;
            foreach (int day in new[] { 7, 14, 21, 30 })
            {
                milestones += table.Cell(day).Canon.Seeds;
            }

            Assert.That(milestones, Is.EqualTo(1550));
            Assert.That(Math.Round(milestones * 1000.0 / 3000.0) / 10.0, Is.EqualTo(51.7));
        }

        [Test]
        public void ThePicksAlwaysSeeds_GiveTheEstimateOfD033()
        {
            // «Peor caso "siempre semillas": +850 semillas por ciclo (estimación, no [sim])».
            LoginCalendarTable table = LoginCalendarTable.Default;
            int seeds = 0;
            for (int day = 1; day <= LoginCalendarTable.Days; day++)
            {
                seeds += table.Cell(day).Option(LoginCalendarChoice.Seeds).Seeds;
            }

            Assert.That(seeds - table.CanonSeeds(), Is.EqualTo(850));
        }

        [Test]
        public void EveryCell_HasThreeDistinctOptionsOfEqualValue_WithTheRateOfD033()
        {
            LoginCalendarValueRate rate = LoginCalendarValueRate.Default;
            Assert.That(rate.Undo, Is.EqualTo(30), "D-033, punto 1: Marea = 30");
            Assert.That(rate.Other, Is.EqualTo(100), "cualquier otro comodín = 100");
            LoginCalendarTable table = LoginCalendarTable.Default;
            table.Validate(rate);
            int[] expected = { 60, 60, 60, 100, 100, 100, 250, 60, 60, 60, 100, 100, 100, 400, 60, 100, 60, 100, 60, 100, 400, 100, 100, 100, 100, 100, 60, 100, 100, 600 };
            for (int day = 1; day <= LoginCalendarTable.Days; day++)
            {
                LoginCalendarCell cell = table.Cell(day);
                Assert.That(cell.Canon.Value(rate), Is.EqualTo(expected[day - 1]), "canon de la casilla " + day + " (D-033, añadido del 2026-10-21)");
                for (int c = 0; c < LoginCalendarTable.Choices; c++)
                {
                    Assert.That(cell.Option((LoginCalendarChoice)c).Value(rate), Is.EqualTo(expected[day - 1]), "casilla " + day + ", opción " + c);
                }
            }
        }

        [Test]
        public void Default_IsAFreshInstanceEachTime()
        {
            Assert.That(LoginCalendarTable.Default, Is.Not.SameAs(LoginCalendarTable.Default));
        }

        [Test]
        public void Format_AndParse_RoundTrip()
        {
            string json = LoginCalendarTable.Default.Format();
            StringAssert.StartsWith("{\"days\":[{\"day\":1,\"canon\":{\"seeds\":60},\"options\":[{\"seeds\":60},{\"undo\":2},{\"seeds\":30,\"undo\":1}],\"extras\":[]}", json);
            StringAssert.Contains("{\"day\":7,\"canon\":{\"seeds\":250},", json);
            StringAssert.Contains("\"extras\":[\"basket\"]}", json);
            StringAssert.EndsWith("\"extras\":[\"grand_basket\",\"frame\"]}]}", json);
            LoginCalendarTable back = LoginCalendarTable.Parse(json, LoginCalendarValueRate.Default);
            Assert.That(back.Format(), Is.EqualTo(json));
        }

        [Test]
        public void Parse_AcceptsAnyKeyOrderAndSpaces_AndMissingExtras()
        {
            string json = LoginCalendarTable.Default.Format()
                .Replace("\"canon\":{\"seeds\":30,\"undo\":1}", "\"canon\" : { \"undo\" : 1 , \"seeds\" : 30 }")
                .Replace(",\"extras\":[]", string.Empty);
            LoginCalendarTable table = LoginCalendarTable.Parse(json, LoginCalendarValueRate.Default);
            Assert.That(table.Cell(2).Canon.ToJson(), Is.EqualTo("{\"seeds\":30,\"undo\":1}"));
            Assert.That(table.Cell(1).Extras, Is.EqualTo(LoginCalendarExtras.None));
            Assert.That(table.Format(), Is.EqualTo(LoginCalendarTable.Default.Format()));
        }

        [Test]
        public void Parse_RejectsWhatIsNotTheTable()
        {
            string good = LoginCalendarTable.Default.Format();
            string[] bad =
            {
                "",
                "[]",
                "{}",
                "{\"days\":[]}",
                good.Replace("{\"day\":30,", "{\"day\":31,"),                                                      // día fuera de orden
                good.Replace("\"options\":[{\"seeds\":60},{\"undo\":2},", "\"options\":[{\"seeds\":60},{\"undo\":3},"), // valores distintos
                good.Replace("{\"undo\":2}", "{\"shuffle\":2}"),                                                   // Pororoca
                good.Replace("{\"undo\":2}", "{\"skip\":2}"),                                                      // clave desconocida
                good.Replace("{\"seeds\":60},{\"undo\":2}", "{\"seeds\":-60},{\"undo\":2}"),                       // negativa
                good.Replace("{\"seeds\":30,\"undo\":1}", "{\"undo\":2}"),                                         // dos opciones iguales
                good.Replace("{\"seeds\":60},{\"undo\":2},{\"seeds\":30,\"undo\":1}],\"extras\":[]", "{\"seeds\":60},{\"undo\":2},{\"seeds\":30,\"undo\":1}],\"extras\":[\"basket\"]"), // Canasta fuera de hito
                good.Replace("\"extras\":[\"grand_basket\",\"frame\"]", "\"extras\":[\"grand_basket\",\"basket\"]"),   // Canasta y grande a la vez
                good.Replace("\"extras\":[\"grand_basket\",\"frame\"]", "\"extras\":[\"chest\"]"),                     // extra desconocido
                good.Replace("{\"seeds\":60},{\"undo\":2},{\"seeds\":30,\"undo\":1}", "{\"seeds\":60},{\"undo\":2}"),  // dos opciones
                good.Replace("{\"seeds\":60}", "{\"seeds\":99999999999}"),                                         // no cabe en int
                good.Substring(0, good.Length - 1),
            };

            foreach (string json in bad)
            {
                Assert.Throws<FormatException>(() => LoginCalendarTable.Parse(json, LoginCalendarValueRate.Default), json.Length > 80 ? json.Substring(0, 80) : json);
                LoginCalendarTable fallback = LoginCalendarTable.ParseOrDefault(json, LoginCalendarValueRate.Default, out string? error);
                Assert.That(fallback.Format(), Is.EqualTo(good), "la Bitácora nunca queda vacía: la tabla del cliente");
                if (json.Length > 0)
                {
                    Assert.That(error, Is.Not.Null.And.Not.Empty);
                }
            }

            Assert.Throws<ArgumentNullException>(() => LoginCalendarTable.Parse(null!, LoginCalendarValueRate.Default));
            Assert.That(LoginCalendarTable.ParseOrDefault(null, LoginCalendarValueRate.Default, out string? none).Format(), Is.EqualTo(good));
            Assert.That(none, Is.Null);
        }

        [Test]
        public void ValueRate_ReadsAndWritesTheSyntaxOf08_9()
        {
            Assert.That(LoginCalendarValueRate.Default.Format(), Is.EqualTo("{\"undo3\":30,\"other\":100}"));
            Assert.That(LoginCalendarValueRate.TryParse(" { \"other\" : 90 , \"undo3\" : 20 } ", out LoginCalendarValueRate rate), Is.True);
            Assert.That(rate, Is.EqualTo(new LoginCalendarValueRate(20, 90)));
            foreach (string json in new[] { null!, "", "{}", "[]", "{\"undo3\":30}", "{\"undo3\":0,\"other\":100}", "{\"undo3\":30,\"other\":\"100\"}", "{\"undo3\":30,\"other\":100,\"x\":1}", "{\"undo3\":30.5,\"other\":100}" })
            {
                Assert.That(LoginCalendarValueRate.TryParse(json, out rate), Is.False, json ?? "null");
                Assert.That(rate, Is.EqualTo(LoginCalendarValueRate.Default));
            }
        }

        [Test]
        public void Reward_RejectsShuffleAndNegatives_AndWritesStableJson()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LoginCalendarReward.Of(BoosterKind.Shuffle, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => LoginCalendarReward.OfSeeds(-1));
            Assert.That(LoginCalendarReward.OfSeeds(0).ToJson(), Is.EqualTo("{\"seeds\":0}"));
            Assert.That(LoginCalendarReward.Of(BoosterKind.ExtraCuia, 1, 5).Plus(BoosterKind.Undo, 2).ToJson(), Is.EqualTo("{\"seeds\":5,\"undo\":2,\"extra_cuia\":1}"));
            Assert.That(LoginCalendarReward.Of(BoosterKind.Hint, 1).PlusSeeds(10), Is.EqualTo(LoginCalendarReward.Of(BoosterKind.Hint, 1, 10)));
        }
    }
}
