#nullable enable
using System;
using System.Collections.Generic;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// Los avatares del jugador sin motor (D-056; avatares.md §1, §3.1, §3.2 y §3.5 con Q-AV-1 a Q-AV-12 en la recomendada): el
    /// catálogo de 16 (6 libres) con índices fijos, los diez hitos, la retroactividad, que un desbloqueo no se pierde, el default al
    /// azar entre los libres, la elección, el punto del inicio y el ocelote oculto con la Liga apagada.
    /// </summary>
    public sealed class AvatarRulesTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

        private static AvatarProgress P(int maxLevel = 0, bool hard = false, bool veryHard = false, bool river = false, int streak = 0, int stamps = 0, int league = 0)
            => new AvatarProgress(maxLevel, hard, veryHard, river, streak, stamps, league);

        [Test]
        public void Catalog_Has16_SixFree_FixedIndices_AndLeagueNames()
        {
            var expected = new[]
            {
                "macaw", "toucan", "tapir", "turtle", "coati", "hummingbird", "dart_frog", "otter", "jaguar", "kingfisher", "caiman",
                "anteater", "heron", "howler", "ocelot", "armadillo",
            };
            Assert.That(AvatarCatalog.Count, Is.EqualTo(16));
            for (int i = 0; i < expected.Length; i++)
            {
                AvatarInfo a = AvatarCatalog.All[i];
                Assert.That(a.Index, Is.EqualTo(i), "el índice es la posición: la lista solo crece por el final");
                Assert.That(a.Id, Is.EqualTo(expected[i]));
                Assert.That(a.Free, Is.EqualTo(i < AvatarCatalog.FreeCount));
                Assert.That(LeagueRules.IsKnownName(a.NameKey), Is.True, a.NameKey + " es un animal de la Liga (avatares.md §1.1)");
                Assert.That(AvatarCatalog.AtIndex(i), Is.SameAs(a));
                Assert.That(AvatarCatalog.Find(a.Id), Is.SameAs(a));
                Assert.That(AvatarCatalog.MilestoneId(a.Milestone) == null, Is.EqualTo(a.Free));
            }

            foreach (string excluded in new[] { "sloth", "capybara", "boto", "anaconda", "manatee", "squirrel_monkey", "morpho", "arapaima" })
            {
                Assert.That(AvatarCatalog.Find(excluded), Is.Null, excluded + " queda fuera (avatares.md §1.1)");
            }

            Assert.That(AvatarCatalog.AtIndex(16), Is.Null, "un índice de una versión más nueva: disco neutro");
            Assert.That(AvatarCatalog.AtIndex(null), Is.Null);
            Assert.That(AvatarCatalog.AtIndex(-1), Is.Null);
        }

        [Test]
        public void Milestones_AreMetByTheExistingState()
        {
            Assert.That(AvatarRules.IsMet(AvatarMilestone.HardWin, P(maxLevel: 40)), Is.False);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.HardWin, P(maxLevel: 40, hard: true)), Is.True, "la victoria de un Difícil");
            Assert.That(AvatarRules.IsMet(AvatarMilestone.HardWin, P(maxLevel: 41)), Is.True, "retroactivo: max_level ≥ 41");
            Assert.That(AvatarRules.IsMet(AvatarMilestone.VeryHardWin, P(maxLevel: 60, hard: true)), Is.False);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.VeryHardWin, P(maxLevel: 60, veryHard: true)), Is.True);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.VeryHardWin, P(maxLevel: 61)), Is.True, "retroactivo: max_level ≥ 61");
            Assert.That(AvatarRules.IsMet(AvatarMilestone.World3, P(maxLevel: 79)), Is.False);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.World3, P(maxLevel: 80)), Is.True, "gana el 80 (Manaos)");
            Assert.That(AvatarRules.IsMet(AvatarMilestone.World4, P(maxLevel: 140)), Is.True);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.World5, P(maxLevel: 199)), Is.False);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.World6, P(maxLevel: 300)), Is.True);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.Streak10, P(river: true)), Is.True);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.DailyStreak7, P(streak: 6)), Is.False);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.DailyStreak7, P(streak: 7)), Is.True);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.DailyStamps20, P(stamps: 20)), Is.True);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.League50, P(league: 49)), Is.False);
            Assert.That(AvatarRules.IsMet(AvatarMilestone.League50, P(league: 50)), Is.True);
            Assert.That(AvatarRules.LevelsLeft(AvatarMilestone.World4, 117), Is.EqualTo(23), "«Completa Trapecio (te faltan 23 niveles)»");
            Assert.That(AvatarRules.LevelsLeft(AvatarMilestone.HardWin, 117), Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_UnlocksOnce_NeverLoses_AndReportsRetro()
        {
            var data = new AvatarData { Id = "macaw" };
            int saves = 0;
            var service = new AvatarService(data, () => saves++, new ManualClock(T0));
            var events = new List<AvatarUnlock>();
            service.Unlocked += u => events.Add(u);

            Assert.That(service.Evaluate(P(maxLevel: 145), retro: true), Is.EqualTo(4), "Difícil, Muy difícil, mundos 3 y 4");
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(events.Count, Is.EqualTo(4));
            Assert.That(events[0].Avatar.Id, Is.EqualTo("dart_frog"));
            Assert.That(events[0].Retro, Is.True);
            Assert.That(data.Unlocked, Is.EqualTo(new[] { "anteater", "caiman", "dart_frog", "jaguar" }));

            Assert.That(service.Evaluate(P(maxLevel: 145), retro: false), Is.EqualTo(0), "lo ya desbloqueado no se repite");
            Assert.That(saves, Is.EqualTo(1));

            Assert.That(service.Evaluate(P(streak: 7, league: 60), retro: false), Is.EqualTo(2));
            Assert.That(events[5].Retro, Is.False);
            Assert.That(service.Evaluate(P(), retro: false), Is.EqualTo(0));
            Assert.That(data.Unlocked.Count, Is.EqualTo(6), "la racha y los puntos vuelven a 0, el desbloqueo no se pierde");
        }

        [Test]
        public void EnsureValid_AssignsAFreeOne_AtRandom_WithoutAsking()
        {
            var data = new AvatarData();
            int saves = 0;
            var service = new AvatarService(data, () => saves++, new ManualClock(T0));
            int asked = -1;
            Assert.That(service.EnsureValid(n => { asked = n; return 4; }), Is.True);
            Assert.That(asked, Is.EqualTo(6), "uno de los 6 libres");
            Assert.That(data.Id, Is.EqualTo("coati"));
            Assert.That(data.Chosen, Is.False, "asignado, no elegido: el punto invita a cambiarlo");
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(service.EnsureValid(n => 0), Is.False, "uno válido no se toca");

            data.Id = "jaguar"; // de hito, sin desbloquear (una fusión o un perfil manipulado)
            Assert.That(service.EnsureValid(n => 99), Is.True);
            Assert.That(data.Id, Is.EqualTo("hummingbird"), "recortado al último libre");
            data.Id = "unicorn";
            Assert.That(service.EnsureValid(n => 0), Is.True);
            Assert.That(data.Id, Is.EqualTo("macaw"));
        }

        [Test]
        public void Select_FreeOrUnlocked_OnlyOnce_AndNeverLocked()
        {
            var data = new AvatarData { Id = "coati" };
            int saves = 0;
            var service = new AvatarService(data, () => saves++, new ManualClock(T0));
            var selected = new List<string>();
            service.Selected += (a, free, first) => selected.Add(a.Id + "|" + free + "|" + first);

            Assert.That(service.Select("jaguar", true), Is.EqualTo(AvatarSelectResult.Locked));
            Assert.That(service.Select("unicorn", true), Is.EqualTo(AvatarSelectResult.Unknown));
            Assert.That(saves, Is.EqualTo(0));

            Assert.That(service.Select("toucan", true), Is.EqualTo(AvatarSelectResult.Selected));
            Assert.That(data.Id, Is.EqualTo("toucan"));
            Assert.That(data.Chosen, Is.True);
            Assert.That(data.At, Is.EqualTo(T0));
            Assert.That(selected, Is.EqualTo(new[] { "toucan|True|True" }));
            Assert.That(service.Select("toucan", true), Is.EqualTo(AvatarSelectResult.AlreadyChosen));
            Assert.That(selected.Count, Is.EqualTo(1), "el mismo no vuelve a contar");

            data.AddUnlocked("jaguar");
            Assert.That(service.Select("jaguar", true), Is.EqualTo(AvatarSelectResult.Selected));
            Assert.That(selected[1], Is.EqualTo("jaguar|False|False"));
        }

        [Test]
        public void TheLeagueAvatar_IsHiddenWithTheLeagueOff_UnlessAlreadyUnlocked()
        {
            var data = new AvatarData { Id = "macaw" };
            AvatarInfo ocelot = AvatarCatalog.Find("ocelot")!;
            Assert.That(AvatarRules.VisibleInPicker(ocelot, data, leagueEnabled: false), Is.False, "Q-AV-8: sin una meta imposible");
            Assert.That(AvatarRules.VisibleInPicker(ocelot, data, leagueEnabled: true), Is.True);
            Assert.That(AvatarRules.VisibleInPicker(AvatarCatalog.Find("jaguar")!, data, false), Is.True);
            data.AddUnlocked("ocelot");
            Assert.That(AvatarRules.VisibleInPicker(ocelot, data, leagueEnabled: false), Is.True, "si ya lo ganó, sigue en su rejilla");
            var service = new AvatarService(new AvatarData { Id = "macaw" }, () => { }, new ManualClock(T0));
            Assert.That(service.Select("ocelot", false), Is.EqualTo(AvatarSelectResult.Unknown));
        }

        [Test]
        public void TheHomeDot_From10_UntilChosen_AndForEveryUnseenUnlock()
        {
            var data = new AvatarData { Id = "macaw" };
            Assert.That(AvatarRules.DotShown(data, 9), Is.False, "antes del 10, el inicio sigue igual");
            Assert.That(AvatarRules.DotShown(data, 10), Is.True, "sin elegir nunca");
            data.Chosen = true;
            Assert.That(AvatarRules.DotShown(data, 10), Is.False);
            data.AddUnlocked("otter");
            Assert.That(AvatarRules.DotShown(data, 30), Is.True, "un desbloqueado sin ver");
            Assert.That(AvatarRules.IsNew(AvatarCatalog.Find("otter")!, data), Is.True);

            var service = new AvatarService(data, () => { }, new ManualClock(T0));
            Assert.That(service.MarkSeen(), Is.EqualTo(1));
            Assert.That(AvatarRules.DotShown(data, 30), Is.False);
            Assert.That(AvatarRules.IsNew(AvatarCatalog.Find("otter")!, data), Is.False);
            Assert.That(service.MarkSeen(), Is.EqualTo(0));
        }
    }
}
