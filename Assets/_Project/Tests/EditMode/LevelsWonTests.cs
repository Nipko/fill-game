#nullable enable
using System;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// <c>stats.levels_won</c> (08 §4.3; pendiente de TI6a, cerrado en TI6b): cada victoria suma una en su instante lógico, en el
    /// mismo guardado del perfil que <c>max_level</c> y <c>next_level</c>, y una sola vez (la celebración, la pantalla de victoria,
    /// «Siguiente» y la Cosecha doble no vuelven a sumar). Ni el fallo, ni Volver a la orilla, ni guardar el siguiente nivel la
    /// tocan. Es la cadencia de 1 de cada <c>ui_sloth_win_line_every_n</c> de TiTino (D-016, punto 5; propuesta de TiTino d.6,
    /// punto 2: «la victoria número N, 2N, 3N…»).
    /// </summary>
    public sealed class LevelsWonTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ProfileProgressStore_EachWinAddsOne_InTheSameSave()
        {
            var store = new MemoryProfileStore();
            Profile profile = AF.Economy.Wallet.NewProfile(new ManualClock(T0));
            var progress = new ProfileProgressStore(profile, store);
            Assert.That(progress.LevelsWon, Is.EqualTo(0));

            progress.SaveWin(1, 2);
            Assert.That(progress.LevelsWon, Is.EqualTo(1));
            Assert.That(store.SaveCount, Is.EqualTo(1), "una sola escritura con el progreso");
            Assert.That(ProfileJson.Deserialize(store.Json!).Stats.LevelsWon, Is.EqualTo(1), "stats.levels_won, guardado");

            progress.SaveNextLevel(5);
            progress.SaveMapSeenLevel(1);
            Assert.That(progress.LevelsWon, Is.EqualTo(1), "guardar el siguiente nivel o el Mapa no es una victoria");

            progress.SaveWin(2, 3);
            progress.SaveWin(2, 3);
            Assert.That(progress.LevelsWon, Is.EqualTo(3), "cada victoria cuenta, también la de un nivel ya ganado");
            Assert.That(ProfileJson.Deserialize(store.Json!).Stats.LevelsWon, Is.EqualTo(3));
        }

        [Test]
        public void TheCount_NeverWrapsNorGoesNegative()
        {
            Profile profile = AF.Economy.Wallet.NewProfile(new ManualClock(T0));
            profile.Stats.LevelsWon = int.MaxValue;
            var progress = new ProfileProgressStore(profile, new MemoryProfileStore());
            progress.SaveWin(1, 2);
            Assert.That(progress.LevelsWon, Is.EqualTo(int.MaxValue));

            profile.Stats.LevelsWon = -4; // perfil editado a mano
            Assert.That(progress.LevelsWon, Is.EqualTo(0));
            progress.SaveWin(1, 2);
            Assert.That(progress.LevelsWon, Is.EqualTo(1));

            var memory = new MemoryProgressStore(levelsWon: -3);
            Assert.That(memory.LevelsWon, Is.EqualTo(0));
            memory.SaveWin(1, 2);
            Assert.That(memory.LevelsWon, Is.EqualTo(1));
            Assert.That(new MemoryProgressStore(41, 40, levelsWon: 40).LevelsWon, Is.EqualTo(40));
        }

        [Test]
        public void TheFlow_CountsTheWinOnce_AtTheLogicalVictory()
        {
            var rig = new LevelStateTests.Rig(offset: 10, seeds: 5);
            rig.Start(11);
            Assert.That(rig.Flow.LevelsWon, Is.EqualTo(0));
            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Progress.LevelsWon, Is.EqualTo(1), "en el instante lógico, con la Cosecha y el progreso");
            Assert.That(rig.Flow.LevelsWon, Is.EqualTo(1));

            rig.Flow.Tick(0, true);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.Celebration));
            Assert.That(rig.Flow.TapAnywhere(), Is.True);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(rig.Flow.ApplyDoubleHarvest(), Is.True);
            Assert.That(rig.Flow.LevelsWon, Is.EqualTo(1), "la celebración, la victoria y la Cosecha doble no suman otra");
            Assert.That(rig.Flow.BeginAdGate(() => { }), Is.True);
            Assert.That(rig.Flow.LevelsWon, Is.EqualTo(1), "«Siguiente» tampoco");
        }

        [Test]
        public void FailingAndRestarting_DoNotCount()
        {
            var rig = new LevelStateTests.Rig();
            rig.Start(6);
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(rig.Flow.Restart(), Is.True, "Volver a la orilla desde el modal (1-30)");
            Assert.That(rig.Flow.LevelsWon, Is.EqualTo(0));
            Assert.That(rig.Flow.Attempt.FailsLevel, Is.EqualTo(1));

            for (int i = 0; i < 200 && rig.Flow.Phase != FlowPhase.Playing; i++)
            {
                rig.Flow.Tick(16, true);
            }

            LevelStateTests.Win(rig.Flow);
            Assert.That(rig.Flow.LevelsWon, Is.EqualTo(1), "la victoria tras el reinicio cuenta una vez");
        }
    }
}
