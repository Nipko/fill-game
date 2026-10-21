#nullable enable
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El aviso de nivel terminado de la Bitácora con las llamadas del parche <c>B4-2-flow.diff</c> (06 §4.2; D-033, añadido del
    /// 2026-10-20, punto 5: «al terminar el primer nivel del día, campaña o Reto, se gane o se pierda»): la victoria y el fallo
    /// duro avisan una vez, dentro del lote del fin de nivel y antes que la vista; el fallo blando (04 §5.2) no.
    /// </summary>
    public sealed class LevelPlayedHookTests
    {
        [Test]
        public void TheWin_TellsTheLogbook_BeforeTheView()
        {
            var rig = new StreakFlowRig(offset: 22);
            var seen = new List<LevelPlayedInfo>();
            int viewEventsAtNotice = -1;
            rig.Flow.LevelPlayed += info =>
            {
                seen.Add(info);
                viewEventsAtNotice = rig.Listener.Saw("win") ? 1 : 0;
            };

            rig.Start(25);
            LevelStateTests.Win(rig.Flow);
            Assert.That(seen.Count, Is.EqualTo(1));
            Assert.That(seen[0].Level, Is.EqualTo(25));
            Assert.That(seen[0].Won, Is.True);
            Assert.That(seen[0].Daily, Is.False);
            Assert.That(viewEventsAtNotice, Is.EqualTo(0), "antes de OnWin: la victoria ya sabe que la libreta está lista");
            Assert.That(rig.Flow.LevelsPlayed, Is.EqualTo(1));
        }

        [Test]
        public void TheHardFail_TellsIt_AndTheSoftFailDoesNot()
        {
            // El 10 es el tablero 6 del tutorial (desplazado 4): 3 → 4 lo bloquea; Volver a la orilla desde el modal es el fallo
            // duro de 1-30.
            var rig = new StreakFlowRig(offset: 4);
            var seen = new List<LevelPlayedInfo>();
            rig.Flow.LevelPlayed += seen.Add;
            rig.Start(10);
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.Restart(), Is.True);
            Assert.That(rig.Listener.Saw("fail:Hard:1"), Is.True);
            Assert.That(seen.Count, Is.EqualTo(1));
            Assert.That(seen[0].Won, Is.False);
            Assert.That(seen[0].Level, Is.EqualTo(10));

            var soft = new StreakFlowRig(offset: 0, maxLevel: 30, pack: SoftPack());
            var softSeen = new List<LevelPlayedInfo>();
            soft.Flow.LevelPlayed += softSeen.Add;
            soft.Start(31);
            PourBackAndForth(soft.Flow, soft.Flow.Boosters.RestartConfirmMinPours);
            Assert.That(soft.Flow.PressRestart(), Is.True);
            Assert.That(soft.Listener.Saw("fail:Soft:1"), Is.True);
            Assert.That(softSeen, Is.Empty, "04 §5.2: el fallo blando no es terminar el nivel");
        }

        private static LevelPack SoftPack()
        {
            return LevelPackReader.Read(TestPacks.MinimalPack(
                "{ \"id\": \"m02_v1_n0031\", \"level\": 31, \"world\": 2, \"lap\": 1, \"water_base\": \"white\", "
                + "\"palette\": [\"acai\", \"aguaje\"], \"units_per_color\": 4, \"caps\": [4, 4, 4, 4], "
                + "\"cuias\": [[1, 1, 2, 2], [2, 2, 1, 1], [], []], \"attrs\": [null, null, null, null], \"murky\": false, \"hidden\": [], "
                + "\"difficulty\": { \"par\": 3 } }"));
        }

        // 0 → 2 y luego 2 ↔ 3 hasta n vertidos (como StreakFlowHookTests).
        private static void PourBackAndForth(LevelFlow flow, int n)
        {
            Assert.That(flow.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(flow.TapCuia(2).Kind, Is.EqualTo(TapKind.Poured));
            int from = 2;
            int to = 3;
            while (flow.Session.AppliedPours < n)
            {
                Assert.That(flow.TapCuia(from).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(flow.TapCuia(to).Kind, Is.EqualTo(TapKind.Poured));
                int t = from;
                from = to;
                to = t;
            }
        }
    }
}
