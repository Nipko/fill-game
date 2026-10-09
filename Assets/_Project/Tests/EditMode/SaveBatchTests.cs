#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Una escritura del perfil por acción del jugador (revisión de B2-5; 02 R8 y §14; 08 §5.4): <see cref="ProfileSaveBatch"/>
    /// junta los guardados de la cartera, el progreso y <c>level_state</c> de una acción en una sola escritura, de modo que la
    /// victoria ya no hace cuatro escrituras con su fsync y un cierre entre el cobro y <c>level_state</c> ya no puede reabrir
    /// el rescate o la rendición y cobrarlos otra vez.
    /// </summary>
    public sealed class SaveBatchTests
    {
        [Test]
        public void Batch_DefersSaves_AndWritesOnceWhenTheOutermostScopeCloses()
        {
            var disk = new RecordingProfileStore();
            var batch = new ProfileSaveBatch(disk);
            Profile profile = Wallet.NewProfile(new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5)));

            batch.Save(profile);
            Assert.That(disk.Snapshots.Count, Is.EqualTo(1), "fuera de un lote, cada guardado pasa tal cual");

            using (batch.Begin())
            {
                profile.Wallet.Seeds = 10;
                batch.Save(profile);
                using (batch.Begin())
                {
                    profile.Wallet.Seeds = 20;
                    batch.Save(profile);
                }

                Assert.That(disk.Snapshots.Count, Is.EqualTo(1), "el lote interno no escribe");
                Assert.That(batch.PendingSave, Is.True);
                profile.Wallet.Seeds = 30;
                batch.Save(profile);
            }

            Assert.That(disk.Snapshots.Count, Is.EqualTo(2), "una sola escritura al cerrar el lote externo");
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(30));
            Assert.That(batch.PendingSave, Is.False);
            Assert.That(batch.WriteCount, Is.EqualTo(2));

            using (batch.Begin())
            {
            }

            Assert.That(disk.Snapshots.Count, Is.EqualTo(2), "un lote sin cambios no escribe");
            default(ProfileSaveScope).Dispose(); // el valor por defecto (sin lote) no hace nada
        }

        [Test]
        public void Batch_AFailedWrite_StaysPending_AndFlushRetriesIt()
        {
            var disk = new RecordingProfileStore();
            var batch = new ProfileSaveBatch(disk);
            Profile profile = Wallet.NewProfile(new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5)));
            disk.FailuresLeft = 1;
            using (batch.Begin())
            {
                profile.Wallet.Seeds = 50;
                batch.Save(profile);
            }

            Assert.That(disk.Snapshots, Is.Empty);
            Assert.That(batch.PendingSave, Is.True, "el cambio sigue en memoria");
            Assert.That(batch.Flush(), Is.True);
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(50));
            Assert.That(batch.PendingSave, Is.False);

            disk.FailuresLeft = 1;
            Assert.That(() => batch.Save(profile), Throws.InstanceOf<IOException>(), "fuera de un lote, el error llega a quien guarda (Wallet)");
        }

        [Test]
        public void InsideABatch_TheWalletReportsSaved_ButOnlyTheBatchKnowsTheWriteFailed()
        {
            // Revisión de la pista B, punto 4: dentro de un lote, TransactionResult.Saved y Wallet.PendingSave dicen «anotado»,
            // no «en disco». Si la escritura del cierre falla, el pendiente lo lleva el lote y no se pierde nada.
            var disk = new RecordingProfileStore();
            var batch = new ProfileSaveBatch(disk);
            var clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var wallet = new Wallet(profile, batch, clock);
            disk.FailuresLeft = 1;
            TransactionResult result;
            using (batch.Begin())
            {
                result = wallet.Apply(Transaction.Earn(SeedSource.Basket, 50));
            }

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.Saved, Is.True, "dentro del lote, Saved = anotado");
            Assert.That(wallet.PendingSave, Is.False, "la cartera no ve el fallo del cierre del lote");
            Assert.That(disk.Snapshots, Is.Empty, "la escritura del cierre falló");
            Assert.That(batch.PendingSave, Is.True, "el único que lo sabe es el lote");

            Assert.That(batch.Flush(), Is.True, "OnApplicationPause: se reintenta y no se pierde nada");
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(50));

            // Fuera de un lote, el mismo fallo sí llega a la cartera.
            disk.FailuresLeft = 1;
            TransactionResult direct = wallet.Apply(Transaction.Earn(SeedSource.Basket, 10));
            Assert.That(direct.Saved, Is.False);
            Assert.That(wallet.PendingSave, Is.True);
            Assert.That(wallet.Flush(), Is.True);
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(60));
        }

        [Test]
        public void EveryPour_IsOneWrite_AndTheVictoryIsOneWriteToo()
        {
            var rig = new BatchRig();
            rig.Start(7);
            SolveResult solution = LevelStateTests.Solve(rig.Flow);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                int before = rig.Disk.Snapshots.Count;
                Pour move = solution.GetMove(i);
                rig.Flow.TapCuia(move.Source);
                rig.Flow.TapCuia(move.Target);
                Assert.That(rig.Disk.Snapshots.Count, Is.EqualTo(before + 1), "vertido " + (i + 1) + ": una escritura");
            }

            // La última fue la victoria: level_state borrado, progreso, Cosecha y cadencia de intersticiales, juntos.
            Profile won = rig.Disk.Last;
            Assert.That(won.LevelState, Is.Null);
            Assert.That(won.Progress.MaxLevel, Is.EqualTo(7));
            Assert.That(won.Progress.NextLevel, Is.EqualTo(8));
            Assert.That(won.Wallet.Seeds, Is.EqualTo(rig.Flow.HarvestSeeds), "la Cosecha va en la misma escritura");
            Assert.That(rig.Flow.HarvestSeeds, Is.GreaterThan(0));
        }

        [Test]
        public void BuyingTheRescue_ChargesAndSavesTheAddedCuia_InOneWrite()
        {
            var rig = new BatchRig(offset: 30, seeds: 1000, maxLevel: 35);
            rig.Start(36);
            LevelStateTests.GetStuck(rig.Flow);
            int before = rig.Disk.Snapshots.Count;
            Assert.That(rig.Flow.BuyRescue()!.IsApplied, Is.True);
            Assert.That(rig.Disk.Snapshots.Count, Is.EqualTo(before + 1), "una escritura: no hay un perfil cobrado con el modal abierto");
            Profile saved = rig.Disk.Last;
            Assert.That(saved.Wallet.Seeds, Is.EqualTo(700), "300, el primero del día");
            StringAssert.StartsWith("h1|P|R|", saved.LevelState!.History, "la cuia de rescate y el tablero en juego");
            Assert.That(saved.LevelState.RescuesLevel, Is.EqualTo(1));
        }

        [Test]
        public void GivingUp_ChargesTheCanoeAndSavesLevelFailed_InOneWrite()
        {
            var rig = new BatchRig(offset: 30, seeds: 0, maxLevel: 35);
            rig.Start(36);
            LevelStateTests.GetStuck(rig.Flow);
            Assert.That(rig.Flow.PressGiveUp(), Is.True);
            int before = rig.Disk.Snapshots.Count;
            Assert.That(rig.Flow.ConfirmGiveUp(), Is.True);
            Assert.That(rig.Disk.Snapshots.Count, Is.EqualTo(before + 1), "una escritura: ¿Seguro? no se reabre con la canoa cobrada");
            Profile saved = rig.Disk.Last;
            Assert.That(saved.Wallet.Canoes.Count, Is.EqualTo(4));
            StringAssert.StartsWith("h1|F|", saved.LevelState!.History);

            // Salir desde la pausa (otra vez en el 36): la canoa y Nivel fallido, también juntos.
            var quit = new BatchRig(offset: 30, seeds: 0, maxLevel: 35);
            quit.Start(36);
            before = quit.Disk.Snapshots.Count;
            Assert.That(quit.Flow.ConfirmQuit(), Is.True);
            Assert.That(quit.Disk.Snapshots.Count, Is.EqualTo(before + 1));
            Assert.That(quit.Disk.Last.Wallet.Canoes.Count, Is.EqualTo(4));
            StringAssert.StartsWith("h1|F|", quit.Disk.Last.LevelState!.History);
        }

        [Test]
        public void LoadingLevel8_SavesCapisGiftAndTheLevel_InOneWrite()
        {
            var rig = new BatchRig();
            int before = rig.Disk.Snapshots.Count;
            rig.Flow.Load(8);
            Assert.That(rig.Disk.Snapshots.Count, Is.EqualTo(before + 1));
            Profile saved = rig.Disk.Last;
            Assert.That(saved.Stock.ExtraCuia, Is.EqualTo(1));
            Assert.That(saved.LevelState!.History, Is.EqualTo("h1|P||c|"));
        }

        /// <summary>Flujo con cartera, progreso y nivel en curso en el mismo perfil, guardado por el lote sobre un disco que graba cada escritura.</summary>
        private sealed class BatchRig
        {
            public BatchRig(int offset = 0, int seeds = 0, int maxLevel = 0)
            {
                Clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
                Profile = Wallet.NewProfile(Clock);
                Profile.Wallet.Seeds = seeds;
                Profile.Progress.MaxLevel = maxLevel;
                Disk = new RecordingProfileStore();
                Batch = new ProfileSaveBatch(Disk);
                Wallet = new Wallet(Profile, Batch, Clock);
                Listener = new RecordingListener();
                Flow = new LevelFlow(
                    LevelCatalog.Create(new[] { TestPacks.Tutorial }, offset),
                    new ProfileProgressStore(Profile, Batch),
                    Listener,
                    GameConfig.TapSwitchSelection,
                    PourModes.Default,
                    Wallet)
                {
                    StateStore = new ProfileLevelStateStore(Profile, Batch),
                    SaveBatch = Batch,
                };
            }

            public ManualClock Clock { get; }

            public Profile Profile { get; }

            public RecordingProfileStore Disk { get; }

            public ProfileSaveBatch Batch { get; }

            public Wallet Wallet { get; }

            public RecordingListener Listener { get; }

            public LevelFlow Flow { get; }

            public void Start(int level)
            {
                Flow.Load(level);
                Assert.That(Flow.TapAnywhere(), Is.True);
                Assert.That(Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            }
        }

        /// <summary>Disco de prueba: guarda una copia del perfil en cada escritura y puede fallar a propósito.</summary>
        private sealed class RecordingProfileStore : IProfileStore
        {
            public readonly List<string> Snapshots = new List<string>();

            public int FailuresLeft { get; set; }

            public Profile Last => ProfileJson.Deserialize(Snapshots[Snapshots.Count - 1]);

            public ProfileLoadResult Load()
            {
                return Snapshots.Count == 0
                    ? new ProfileLoadResult(ProfileLoadStatus.NotFound, null)
                    : new ProfileLoadResult(ProfileLoadStatus.Loaded, Last);
            }

            public void Save(Profile profile)
            {
                if (FailuresLeft > 0)
                {
                    FailuresLeft--;
                    throw new IOException("disco lleno");
                }

                Snapshots.Add(ProfileJson.Serialize(profile));
            }

            public void Delete()
            {
                Snapshots.Clear();
            }
        }
    }
}
