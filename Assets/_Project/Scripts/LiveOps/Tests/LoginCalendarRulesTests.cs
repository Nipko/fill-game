#nullable enable
using System;
using AF.Economy;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Reglas de la Bitácora (B4; 06 §4.2; D-033, añadido del 2026-10-20, puntos 3-6): el estado del día, el día jugado
    /// (<c>played_date</c> [P]), el reclamo secuencial sin reinicio y el cambio de ciclo, <c>days_missed_before</c>, la sustitución
    /// de los comodines bloqueados («nunca dos opciones iguales») y los ids de <c>af_calendar_claim</c>.
    /// </summary>
    public sealed class LoginCalendarRulesTests
    {
        private static readonly LoginCalendarUnlocks NothingButUndo = new LoginCalendarUnlocks(false, false, false);

        [Test]
        public void StateOf_FollowsAvailabilityThePlayedDayAndTheLastSeal()
        {
            var record = new LoginCalendarRecord();
            Assert.That(LoginCalendarRules.StateOf(record, "2026-10-20", false), Is.EqualTo(LoginCalendarState.Hidden));
            Assert.That(LoginCalendarRules.StateOf(record, "2026-10-20", true), Is.EqualTo(LoginCalendarState.Waiting), "sin nivel terminado hoy");

            record.PlayedDate = "2026-10-19";
            Assert.That(LoginCalendarRules.StateOf(record, "2026-10-20", true), Is.EqualTo(LoginCalendarState.Waiting), "jugar ayer no vale hoy");
            record.PlayedDate = "2026-10-20";
            Assert.That(LoginCalendarRules.StateOf(record, "2026-10-20", true), Is.EqualTo(LoginCalendarState.Ready));

            record.LastClaimDate = "2026-10-20";
            Assert.That(LoginCalendarRules.StateOf(record, "2026-10-20", true), Is.EqualTo(LoginCalendarState.Sealed), "una página por día");
            record.LastClaimDate = "2026-10-25";
            Assert.That(LoginCalendarRules.StateOf(record, "2026-10-20", true), Is.EqualTo(LoginCalendarState.Sealed), "reloj atrasado: no se sella otra");
            record.LastClaimDate = "2026-10-19";
            Assert.That(LoginCalendarRules.StateOf(record, "2026-10-20", true), Is.EqualTo(LoginCalendarState.Ready));
            Assert.Throws<ArgumentNullException>(() => LoginCalendarRules.StateOf(null!, "2026-10-20", true));
        }

        [Test]
        public void IsAvailable_NeedsTheSwitchAndTheUnlockLevel()
        {
            Assert.That(LoginCalendarRules.IsAvailable(true, 10, 10), Is.True, "live_unlock_level_daily = 10");
            Assert.That(LoginCalendarRules.IsAvailable(true, 9, 10), Is.False);
            Assert.That(LoginCalendarRules.IsAvailable(false, 50, 10), Is.False, "biz_feature_login_calendar apagado");
        }

        [Test]
        public void MarkPlayed_ChangesOnlyTheFirstTimeOfTheDay_AndNeverBackwards()
        {
            var record = new LoginCalendarRecord();
            Assert.That(LoginCalendarRules.MarkPlayed(record, "2026-10-20"), Is.True);
            Assert.That(LoginCalendarRules.MarkPlayed(record, "2026-10-20"), Is.False);
            Assert.That(record.PlayedDate, Is.EqualTo("2026-10-20"));
            Assert.That(LoginCalendarRules.MarkPlayed(record, "2026-10-18"), Is.False, "reloj atrasado");
            Assert.That(record.PlayedDate, Is.EqualTo("2026-10-20"));
            Assert.That(LoginCalendarRules.MarkPlayed(record, "2026-10-21"), Is.True);
        }

        [Test]
        public void Advance_IsSequentialWithoutReset_AndStartsAnotherCycleAfter30()
        {
            var record = new LoginCalendarRecord();
            LoginCalendarRules.Advance(record, "2026-10-20");
            Assert.That(record.NextDay, Is.EqualTo(2));
            Assert.That(record.Cycle, Is.EqualTo(1));
            Assert.That(record.LastClaimDate, Is.EqualTo("2026-10-20"));

            record.NextDay = 30;
            LoginCalendarRules.Advance(record, "2026-11-30");
            Assert.That(record.NextDay, Is.EqualTo(1), "al sellar la 30 empieza un ciclo nuevo (06 §4.2)");
            Assert.That(record.Cycle, Is.EqualTo(2));

            record.NextDay = 0;
            record.Cycle = -3;
            LoginCalendarRules.Advance(record, "2026-12-01");
            Assert.That(record.NextDay, Is.EqualTo(2), "un perfil fuera de rango vuelve a la casilla 1 antes de avanzar");
            Assert.That(record.Cycle, Is.EqualTo(1));
        }

        [Test]
        public void DaysMissedBefore_CountsTheLocalDaysWithoutSeal()
        {
            var record = new LoginCalendarRecord();
            Assert.That(LoginCalendarRules.DaysMissedBefore(record, new DateTime(2026, 10, 20)), Is.EqualTo(0), "primera vez");
            record.LastClaimDate = "2026-10-19";
            Assert.That(LoginCalendarRules.DaysMissedBefore(record, new DateTime(2026, 10, 20)), Is.EqualTo(0), "ayer");
            record.LastClaimDate = "2026-10-16";
            Assert.That(LoginCalendarRules.DaysMissedBefore(record, new DateTime(2026, 10, 20)), Is.EqualTo(3));
            record.LastClaimDate = "2026-10-25";
            Assert.That(LoginCalendarRules.DaysMissedBefore(record, new DateTime(2026, 10, 20)), Is.EqualTo(0), "reloj atrasado");
            record.LastClaimDate = "basura";
            Assert.That(LoginCalendarRules.DaysMissedBefore(record, new DateTime(2026, 10, 20)), Is.EqualTo(0));
        }

        [Test]
        public void ResolveOptions_WithEverythingUnlocked_IsTheTable()
        {
            LoginCalendarTable table = LoginCalendarTable.Default;
            var into = new LoginCalendarReward[3];
            for (int day = 1; day <= LoginCalendarTable.Days; day++)
            {
                LoginCalendarRules.ResolveOptions(table.Cell(day), LoginCalendarUnlocks.All, into);
                for (int c = 0; c < 3; c++)
                {
                    Assert.That(into[c], Is.EqualTo(table.Cell(day).Option((LoginCalendarChoice)c)), "casilla " + day);
                }
            }
        }

        [Test]
        public void ResolveOptions_ReplacesLockedBoosters_WithTheNextUnlockedOfTheSameValue()
        {
            LoginCalendarTable table = LoginCalendarTable.Default;
            // Nivel 10-11: Cuia extra (8) sí; Morpho (12) y Luciérnaga (100) no. Morpho → Cuia extra (D-033, punto 3).
            LoginCalendarUnlocks atTen = LoginCalendarUnlocks.AtLevel(11, 12, 100, 8);
            Assert.That(atTen, Is.EqualTo(new LoginCalendarUnlocks(false, false, true)));
            Assert.That(Json(table.Cell(4), atTen), Is.EqualTo(new[] { "{\"seeds\":100}", "{\"extra_cuia\":1}", "{\"seeds\":40,\"undo\":2}" }));
            Assert.That(Json(table.Cell(7), atTen), Is.EqualTo(new[] { "{\"seeds\":250}", "{\"seeds\":50,\"extra_cuia\":2}", "{\"seeds\":100,\"undo\":5}" }));
            Assert.That(Json(table.Cell(21), atTen), Is.EqualTo(new[] { "{\"seeds\":400}", "{\"extra_cuia\":4}", "{\"seeds\":200,\"extra_cuia\":2}" }));

            // Casilla 29: Luciérnaga → (Morpho bloqueado) → Cuia extra; la mezcla (1 Morpho) daría también Cuia extra, igual que
            // la anterior: da un paso más, a «2 Marea + 40» (nunca dos opciones iguales).
            Assert.That(Json(table.Cell(29), atTen), Is.EqualTo(new[] { "{\"seeds\":100}", "{\"extra_cuia\":1}", "{\"seeds\":40,\"undo\":2}" }));

            // Con Morpho y sin Luciérnaga (nivel 12-99): Luciérnaga → Morpho chocaría con la mezcla (1 Morpho, sin nada
            // bloqueado, que queda como en la tabla): la opción sustituida da un paso más, a Cuia extra.
            LoginCalendarUnlocks atFifty = LoginCalendarUnlocks.AtLevel(50, 12, 100, 8);
            Assert.That(Json(table.Cell(29), atFifty), Is.EqualTo(new[] { "{\"seeds\":100}", "{\"extra_cuia\":1}", "{\"hint\":1}" }));

            // Casilla 30 sin Morpho: las dos darían «3 Cuia extra + 300»; la mezcla, que va después, pasa a 3 × «2 Marea + 40».
            Assert.That(Json(table.Cell(30), atTen), Is.EqualTo(new[] { "{\"seeds\":600}", "{\"seeds\":300,\"extra_cuia\":3}", "{\"seeds\":420,\"undo\":6}" }));
        }

        [Test]
        public void ResolveOptions_KeepsTheValue_AndNeverRepeatsAnOptionWithLockedBoosters()
        {
            LoginCalendarTable table = LoginCalendarTable.Default;
            LoginCalendarValueRate rate = LoginCalendarValueRate.Default;
            var into = new LoginCalendarReward[3];
            for (int mask = 0; mask < 8; mask++)
            {
                var unlocks = new LoginCalendarUnlocks((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0);
                for (int day = 1; day <= LoginCalendarTable.Days; day++)
                {
                    LoginCalendarCell cell = table.Cell(day);
                    LoginCalendarRules.ResolveOptions(cell, unlocks, into);
                    int value = cell.Option(LoginCalendarChoice.Seeds).Value(rate);
                    bool anyLocked = false;
                    for (int c = 0; c < 3; c++)
                    {
                        Assert.That(into[c].Value(rate), Is.EqualTo(value), "casilla " + day + ", máscara " + mask + ", opción " + c);
                        for (int k = 0; k < BoosterKinds.Count; k++)
                        {
                            if (into[c].Stock.Get((BoosterKind)k) > 0)
                            {
                                Assert.That(unlocks.IsUnlocked((BoosterKind)k), Is.True, "nunca un comodín bloqueado");
                            }

                            anyLocked |= cell.Option((LoginCalendarChoice)c).Stock.Get((BoosterKind)k) > 0 && !unlocks.IsUnlocked((BoosterKind)k);
                        }
                    }

                    // Con la Cuia extra bloqueada la cadena puede acabarse (con los defaults no ocurre: se desbloquea en el 8 y la
                    // Bitácora en el 10); en el resto, nunca dos opciones iguales.
                    if (anyLocked && unlocks.ExtraCuia)
                    {
                        Assert.That(into[0].Equals(into[1]) || into[0].Equals(into[2]) || into[1].Equals(into[2]), Is.False,
                            "casilla " + day + ", máscara " + mask + ": nunca dos opciones iguales");
                    }
                }
            }
        }

        [Test]
        public void ResolveOptions_WithNothingUnlocked_EndsInTwoTidesAndForty()
        {
            LoginCalendarTable table = LoginCalendarTable.Default;
            Assert.That(Json(table.Cell(6), NothingButUndo), Is.EqualTo(new[] { "{\"seeds\":100}", "{\"seeds\":40,\"undo\":2}", "{\"seeds\":40,\"undo\":2}" }),
                "Cuia extra bloqueada → «2 Marea + 40»; la mezcla ya es eso en la tabla (no tiene nada bloqueado)");
            Assert.That(Json(table.Cell(30), NothingButUndo)[1], Is.EqualTo("{\"seeds\":420,\"undo\":6}"), "2 Cuia + 1 Morpho → 3 × (2 Marea + 40)");
            Assert.Throws<ArgumentException>(() => LoginCalendarRules.ResolveOptions(table.Cell(1), LoginCalendarUnlocks.All, new LoginCalendarReward[2]));
            Assert.Throws<ArgumentNullException>(() => LoginCalendarRules.ResolveOptions(null!, LoginCalendarUnlocks.All, new LoginCalendarReward[3]));
        }

        [Test]
        public void RewardType_ChoiceIdAndFrameId_AreTheIdsOfTheEvent()
        {
            Assert.That(LoginCalendarRules.RewardType(LoginCalendarReward.OfSeeds(60)), Is.EqualTo("seeds"));
            Assert.That(LoginCalendarRules.RewardType(LoginCalendarReward.Of(BoosterKind.Undo, 2)), Is.EqualTo("booster"));
            Assert.That(LoginCalendarRules.RewardType(LoginCalendarReward.Of(BoosterKind.Undo, 1, 30)), Is.EqualTo("mix"));
            Assert.That(LoginCalendarTable.ChoiceId(LoginCalendarChoice.Seeds), Is.EqualTo("seeds"));
            Assert.That(LoginCalendarTable.ChoiceId(LoginCalendarChoice.Booster), Is.EqualTo("booster"));
            Assert.That(LoginCalendarTable.ChoiceId(LoginCalendarChoice.Mix), Is.EqualTo("mix"));
            Assert.That(LoginCalendarRules.FrameId(1), Is.EqualTo("frame_w01"));
            Assert.That(LoginCalendarRules.FrameId(13), Is.EqualTo("frame_w13"));
            Assert.Throws<ArgumentOutOfRangeException>(() => LoginCalendarRules.FrameId(0));
        }

        [Test]
        public void Unlocks_AtLevel_FollowTheUnlockLevels()
        {
            Assert.That(LoginCalendarUnlocks.AtLevel(7, 12, 100, 8), Is.EqualTo(new LoginCalendarUnlocks(false, false, false)));
            Assert.That(LoginCalendarUnlocks.AtLevel(12, 12, 100, 8), Is.EqualTo(new LoginCalendarUnlocks(true, false, true)));
            Assert.That(LoginCalendarUnlocks.AtLevel(100, 12, 100, 8), Is.EqualTo(LoginCalendarUnlocks.All));
            Assert.That(NothingButUndo.IsUnlocked(BoosterKind.Undo), Is.True, "Marea, desde el nivel 1 (04 §2.1)");
        }

        private static string[] Json(LoginCalendarCell cell, LoginCalendarUnlocks unlocks)
        {
            var into = new LoginCalendarReward[3];
            LoginCalendarRules.ResolveOptions(cell, unlocks, into);
            return new[] { into[0].ToJson(), into[1].ToJson(), into[2].ToJson() };
        }
    }
}
