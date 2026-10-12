#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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
    /// el rescate o la rendición y cobrarlos otra vez. Con un almacén que separa la serialización del disco
    /// (<see cref="IProfileByteStore"/>), el cierre del lote entrega los bytes al escritor de segundo plano (T-D, opción B): gana
    /// el último, sus fallos se publican al hilo principal y dejan el perfil pendiente, y el fin de nivel, la cartera y
    /// <see cref="ProfileSaveBatch.FlushSync"/> esperan al escritor y escriben en síncrono.
    /// </summary>
    public sealed class SaveBatchTests
    {
        // Tope de las esperas a otros hilos en estos tests (no se alcanza si todo va bien).
        private const int WaitMs = 10000;

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
            StringAssert.StartsWith("h2|P|R|", saved.LevelState!.History, "la cuia de rescate y el tablero en juego");
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
            StringAssert.StartsWith("h2|F|", saved.LevelState!.History);

            // Salir desde la pausa (otra vez en el 36): la canoa y Nivel fallido, también juntos.
            var quit = new BatchRig(offset: 30, seeds: 0, maxLevel: 35);
            quit.Start(36);
            before = quit.Disk.Snapshots.Count;
            Assert.That(quit.Flow.ConfirmQuit(), Is.True);
            Assert.That(quit.Disk.Snapshots.Count, Is.EqualTo(before + 1));
            Assert.That(quit.Disk.Last.Wallet.Canoes.Count, Is.EqualTo(4));
            StringAssert.StartsWith("h2|F|", quit.Disk.Last.LevelState!.History);
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
            Assert.That(saved.LevelState!.History, Is.EqualTo("h2|P||c||"));
        }

        [Test]
        public void Background_EachBatchHandsItsBytesToTheWriter_AndTheLatestWins()
        {
            // T-D: el cierre del lote solo serializa y entrega; el disco va en el hilo del escritor, con cola de 1.
            var disk = new ThreadedDisk();
            var batch = new ProfileSaveBatch(disk);
            Assert.That(batch.Writer, Is.Not.Null, "un IProfileByteStore lleva escritor en segundo plano");
            Profile profile = Wallet.NewProfile(new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5)));

            disk.Hold(); // el escritor se queda a medias en la primera escritura
            using (batch.Begin())
            {
                profile.Wallet.Seeds = 1;
                batch.Save(profile);
            }

            Assert.That(disk.WaitEntered(WaitMs), Is.True, "el escritor empezó la primera");
            Assert.That(batch.LastWriteInBackground, Is.True);
            Assert.That(batch.PendingSave, Is.False, "entregado al escritor: no queda nada sin entregar");
            for (int seeds = 2; seeds <= 4; seeds++)
            {
                using (batch.Begin())
                {
                    profile.Wallet.Seeds = seeds;
                    batch.Save(profile);
                }
            }

            Assert.That(disk.Count, Is.EqualTo(0), "el hilo principal no espera al disco");
            Assert.That(batch.WriteCount, Is.EqualTo(4), "una escritura pedida por acción (D-008)");
            Assert.That(batch.Writer!.IsIdle, Is.False);

            disk.Release();
            Assert.That(batch.Writer.WaitIdle(WaitMs), Is.True);
            Assert.That(disk.Count, Is.EqualTo(2), "la primera, ya empezada, y la última: la cola de 1 descarta la 2 y la 3");
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(4), "gana el último");
            Assert.That(disk.WritesOnMainThread, Is.EqualTo(0), "el disco nunca en el hilo principal");
            Assert.That(batch.Writer.BackgroundWrites, Is.EqualTo(2));
            Assert.That(batch.LastBackgroundMs, Is.GreaterThan(0.0), "bg_ms: escrituras de verdad medidas con el reloj de verdad");
            Assert.That(batch.FlushSync(WaitMs), Is.True, "todo en disco");
            Assert.That(disk.Count, Is.EqualTo(2), "FlushSync no reescribe lo que ya está en disco");
            Assert.That(disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        [Test]
        public void Background_AFailedWrite_IsPublishedToTheMainThread_StaysPending_AndIsRetried()
        {
            // T-D (la política de B2-5, llevada al escritor): si falla en su hilo, el perfil sigue en memoria y sale con el
            // siguiente guardado o con el Flush síncrono; la excepción nunca llega a quien guarda.
            var disk = new ThreadedDisk();
            var batch = new ProfileSaveBatch(disk);
            Profile profile = Wallet.NewProfile(new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5)));

            disk.FailuresLeft = 1;
            using (batch.Begin())
            {
                profile.Wallet.Seeds = 50;
                batch.Save(profile);
            }

            Assert.That(batch.Writer!.WaitIdle(WaitMs), Is.True);
            Assert.That(disk.Count, Is.EqualTo(0));
            Assert.That(batch.PendingSave, Is.True, "el fallo del escritor llega al hilo principal: el perfil sigue pendiente");
            Assert.That(batch.BackgroundFailures, Is.EqualTo(1));
            StringAssert.Contains("disco lleno", batch.LastBackgroundError);

            // 1. Reintento con el siguiente guardado (otra vez en segundo plano).
            using (batch.Begin())
            {
                profile.Wallet.Seeds = 60;
                batch.Save(profile);
            }

            Assert.That(batch.Writer.WaitIdle(WaitMs), Is.True);
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(60));
            Assert.That(batch.PendingSave, Is.False);

            // 2. Reintento con el Flush síncrono (OnApplicationPause): escribe en el hilo principal.
            disk.FailuresLeft = 1;
            using (batch.Begin())
            {
                profile.Wallet.Seeds = 70;
                batch.Save(profile);
            }

            Assert.That(batch.Writer.WaitIdle(WaitMs), Is.True);
            Assert.That(batch.FlushSync(WaitMs), Is.True, "se reescribe en síncrono y no se pierde nada");
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(70));
            Assert.That(disk.LastOnMainThread, Is.True);
            Assert.That(batch.PendingSave, Is.False);

            // 3. Un guardado fuera de lote no recibe la excepción del escritor: escribe el perfil entero y ya está.
            disk.FailuresLeft = 1;
            using (batch.Begin())
            {
                profile.Wallet.Seeds = 80;
                batch.Save(profile);
            }

            Assert.That(batch.Writer.WaitIdle(WaitMs), Is.True);
            Assert.That(() => batch.Save(profile), Throws.Nothing);
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(80));
            Assert.That(batch.PendingSave, Is.False);
            Assert.That(batch.BackgroundFailures, Is.EqualTo(3));
            Assert.That(disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        [Test]
        public void Durable_TheWalletWaitsForTheWriter_AndWritesOnTheMainThread()
        {
            // T-D: la cartera (batch.Durable, como en GameBootstrap) espera al escritor y escribe en síncrono al cerrar su
            // acción; fuera de un lote sigue viendo sus fallos, como antes. El disco solo retiene al escritor: si la cartera
            // escribiera sin esperarlo (o sin pasar por él), entraría con él a medias y lo anotaría Overlaps.
            var disk = new ThreadedDisk();
            var batch = new ProfileSaveBatch(disk);
            var clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
            Profile profile = Wallet.NewProfile(clock);
            var wallet = new Wallet(profile, batch.Durable, clock);

            disk.Hold();
            using (batch.Begin())
            {
                profile.Stats.Sessions = 1;
                batch.Save(profile); // un vertido: se queda a medias en el escritor
            }

            Assert.That(disk.WaitEntered(WaitMs), Is.True);
            var opener = new Thread(() =>
            {
                Thread.Sleep(100);
                disk.Release();
            });
            opener.Start();
            using (batch.Begin())
            {
                Assert.That(wallet.Apply(Transaction.Earn(SeedSource.Basket, 50)).IsApplied, Is.True);
            }

            // Lo que hay al volver de la acción, antes de esperar al hilo que suelta el disco.
            bool idle = batch.Writer!.IsIdle;
            int count = disk.Count;
            int seeds = disk.Last.Wallet.Seeds;
            bool lastOnMain = disk.LastOnMainThread;
            opener.Join();
            Assert.That(disk.Overlaps, Is.EqualTo(0), "la cartera esperó al escritor: nunca escribió con él a medias");
            Assert.That(idle, Is.True, "al volver de la acción, el escritor ya terminó el vertido");
            Assert.That(batch.LastWriteInBackground, Is.False, "la cartera escribe en síncrono");
            Assert.That(count, Is.EqualTo(2), "primero termina la del escritor y después va la de la cartera");
            Assert.That(seeds, Is.EqualTo(50), "en disco al cerrar la acción, sin Flush");
            Assert.That(lastOnMain, Is.True);
            Assert.That(disk.Count, Is.EqualTo(2), "y nada más llega después");
            Assert.That(batch.PendingSave, Is.False);

            disk.FailuresLeft = 1;
            TransactionResult direct = wallet.Apply(Transaction.Earn(SeedSource.Basket, 10));
            Assert.That(direct.Saved, Is.False, "fuera de un lote, el fallo llega a la cartera");
            Assert.That(wallet.PendingSave, Is.True);
            Assert.That(wallet.Flush(), Is.True);
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(60));
            Assert.That(disk.LastOnMainThread, Is.True);
        }

        [Test]
        public void FlushSync_WaitsForTheWriterWithACap_AndIsTrueOnlyWhenTheLatestIsOnDisk()
        {
            var disk = new ThreadedDisk();
            var batch = new ProfileSaveBatch(disk) { SyncTimeoutMs = 50 };
            Profile profile = Wallet.NewProfile(new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5)));

            disk.Hold();
            using (batch.Begin())
            {
                profile.Wallet.Seeds = 1;
                batch.Save(profile);
            }

            Assert.That(disk.WaitEntered(WaitMs), Is.True);
            using (batch.Begin())
            {
                profile.Wallet.Seeds = 2;
                batch.Save(profile); // en la cola
            }

            Assert.That(batch.FlushSync(50), Is.False, "tope de espera: el escritor sigue a medias");

            // Fin de nivel con el escritor atascado más que el tope: no escribe a la vez que él; sigue pendiente.
            using (batch.Begin())
            {
                batch.RequireDurable();
                profile.Wallet.Seeds = 3;
                batch.Save(profile);
            }

            Assert.That(batch.LastWriteInBackground, Is.False);
            Assert.That(batch.PendingSave, Is.True, "sigue en memoria");
            Assert.That(disk.Count, Is.EqualTo(0), "la escritura síncrona no pasó por delante del escritor retenido");
            Assert.That(disk.Overlaps, Is.EqualTo(0));

            disk.Release();
            Assert.That(batch.FlushSync(WaitMs), Is.True);
            int[] written = disk.SeedsWritten();
            Assert.That(written[0], Is.EqualTo(1), "primero termina la que el escritor tenía a medias");
            Assert.That(written, Has.No.Member(2), "la 2 nunca se escribe: la escritura síncrona vació la cola");
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(3), "el último perfil, también en disco y el último en llegar");
            Assert.That(batch.PendingSave, Is.False);
            Assert.That(batch.Writer!.AllWritten, Is.True);
            Assert.That(disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        [Test]
        public void WithTheWriter_EveryPourIsOneBackgroundWrite_AndTheVictoryIsSynchronous()
        {
            // T-D y D-008: un vertido, una escritura entregada al escritor; la victoria (fin de nivel), una escritura síncrona
            // en el hilo principal que ya deja en disco el perfil del nivel ganado.
            var rig = new BatchRig(threaded: true);
            rig.Start(7);
            SolveResult solution = LevelStateTests.Solve(rig.Flow);
            int last = solution.MoveCount - 1;
            for (int i = 0; i <= last; i++)
            {
                int before = rig.Batch.WriteCount;
                Pour move = solution.GetMove(i);
                rig.Flow.TapCuia(move.Source);
                rig.Flow.TapCuia(move.Target);
                Assert.That(rig.Batch.WriteCount, Is.EqualTo(before + 1), "vertido " + (i + 1) + ": una escritura");
                Assert.That(rig.Batch.LastWriteInBackground, Is.EqualTo(i < last), i < last ? "vertido en segundo plano" : "victoria en síncrono");
            }

            ThreadedDisk disk = rig.Threaded!;
            Assert.That(disk.LastOnMainThread, Is.True);
            Profile won = disk.Last;
            Assert.That(won.LevelState, Is.Null);
            Assert.That(won.Progress.NextLevel, Is.EqualTo(8));
            Assert.That(won.Wallet.Seeds, Is.EqualTo(rig.Flow.HarvestSeeds), "la Cosecha va en la misma escritura");
            Assert.That(rig.Batch.Writer!.IsIdle, Is.True, "la victoria esperó al escritor");
            Assert.That(rig.Batch.FlushSync(WaitMs), Is.True);
            Assert.That(disk.WritesOnMainThread, Is.EqualTo(1), "solo la victoria en el hilo principal");
            Assert.That(disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        [Test]
        public void TheVictoryIsSynchronous_BecauseItIsEndOfLevel_EvenWithAWalletThatDoesNotAskForIt()
        {
            // Revisión de C2, punto 2: aísla el RequireDurable de ResolveWin. Con la cartera sobre el lote normal (no por
            // Durable), la Cosecha no pide escritura síncrona: lo único que hace síncrona la victoria es que es fin de nivel.
            var rig = new BatchRig(threaded: true, durableWallet: false);
            rig.Start(7);
            SolveResult solution = LevelStateTests.Solve(rig.Flow);
            int last = solution.MoveCount - 1;
            for (int i = 0; i <= last; i++)
            {
                Pour move = solution.GetMove(i);
                rig.Flow.TapCuia(move.Source);
                rig.Flow.TapCuia(move.Target);
                Assert.That(rig.Batch.LastWriteInBackground, Is.EqualTo(i < last), i < last ? "vertido en segundo plano" : "victoria en síncrono");
            }

            Assert.That(rig.Flow.HarvestSeeds, Is.GreaterThan(0), "la victoria cobró la Cosecha por la cartera");
            ThreadedDisk disk = rig.Threaded!;
            Assert.That(disk.LastOnMainThread, Is.True);
            Assert.That(rig.Batch.Writer!.IsIdle, Is.True, "la victoria esperó al escritor");
            Profile won = disk.Last;
            Assert.That(won.LevelState, Is.Null, "en disco al volver del toque, sin Flush");
            Assert.That(won.Progress.NextLevel, Is.EqualTo(8));
            Assert.That(won.Wallet.Seeds, Is.EqualTo(rig.Flow.HarvestSeeds));
            Assert.That(disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        [Test]
        public void LeavingTheLevel_IsEndOfLevel_AndWritesSynchronouslyOnTheMainThread()
        {
            // Revisión de C2, punto 2: salir del nivel sin ganarlo (X de Nivel fallido, «Volver al inicio», «Salir») es fin de
            // nivel (T-D.sync): espera al escritor y escribe en síncrono, aunque el vertido anterior fuera en segundo plano.
            var rig = new BatchRig(threaded: true);
            rig.Start(7);
            Pour move = LevelStateTests.Solve(rig.Flow).GetMove(0);
            rig.Flow.TapCuia(move.Source);
            rig.Flow.TapCuia(move.Target);
            Assert.That(rig.Batch.LastWriteInBackground, Is.True, "el vertido, en segundo plano");

            int before = rig.Batch.WriteCount;
            rig.Flow.LeaveLevel();
            Assert.That(rig.Batch.WriteCount, Is.EqualTo(before + 1), "una escritura");
            Assert.That(rig.Batch.LastWriteInBackground, Is.False, "fin de nivel: en síncrono");
            ThreadedDisk disk = rig.Threaded!;
            Assert.That(disk.LastOnMainThread, Is.True);
            Assert.That(rig.Batch.Writer!.IsIdle, Is.True, "esperó al escritor");
            Assert.That(LevelStateSnapshot.TryParseHistory(disk.Last.LevelState!.History, out SavedHistory saved), Is.True);
            Assert.That(saved.Phase, Is.EqualTo(SavedPhase.Exited), "en disco al volver de LeaveLevel, sin Flush");
            Assert.That(disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        [Test]
        public void FlushSync_IsNeverTrueWhileTheLatestProfileIsNotOnDisk()
        {
            // Revisión de C2, punto 1 (T-D.sync: FlushSync da true solo con el perfil en disco). Un guardado fuera de lote que
            // falla en síncrono llega a quien guarda (la cartera) y, además, deja el perfil pendiente en el lote.
            var disk = new ThreadedDisk();
            var batch = new ProfileSaveBatch(disk);
            Profile profile = Wallet.NewProfile(new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5)));
            profile.Wallet.Seeds = 10;
            batch.Save(profile);
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(10));

            disk.FailuresLeft = 1;
            profile.Wallet.Seeds = 20;
            Assert.That(batch.TrySave(profile), Is.False, "fuera de un lote, el fallo llega a quien guarda");
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(10), "en disco sigue el perfil anterior");
            Assert.That(batch.PendingSave, Is.True, "el lote no lo olvida");
            Assert.That(batch.FlushSync(WaitMs), Is.True, "lo reescribe en síncrono");
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(20));
            Assert.That(disk.LastOnMainThread, Is.True);
            Assert.That(batch.PendingSave, Is.False);

            // Aunque la escritura síncrona falle sin pasar por Save (el escritor es público), FlushSync no da true sin escribir:
            // con el escritor parado y la última entrega fuera del disco, la vuelve a escribir.
            disk.FailuresLeft = 1;
            profile.Wallet.Seeds = 30;
            Assert.That(() => batch.Writer!.WriteNow(disk.Serialize(profile), WaitMs), Throws.InstanceOf<IOException>());
            Assert.That(batch.Writer!.AllWritten, Is.False);
            Assert.That(batch.FlushSync(WaitMs), Is.True, "lo reescribe en síncrono");
            Assert.That(disk.Last.Wallet.Seeds, Is.EqualTo(30));
            Assert.That(batch.Writer.AllWritten, Is.True);
            Assert.That(batch.PendingSave, Is.False);
            Assert.That(disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        /// <summary>Flujo con cartera, progreso y nivel en curso en el mismo perfil, guardado por el lote sobre un disco que graba cada escritura.</summary>
        private sealed class BatchRig
        {
            public BatchRig(int offset = 0, int seeds = 0, int maxLevel = 0, bool threaded = false, bool durableWallet = true)
            {
                Clock = new ManualClock(EconomyFixture.T0, TimeSpan.FromHours(-5));
                Profile = Wallet.NewProfile(Clock);
                Profile.Wallet.Seeds = seeds;
                Profile.Progress.MaxLevel = maxLevel;
                Disk = new RecordingProfileStore();
                Threaded = threaded ? new ThreadedDisk() : null;
                Batch = new ProfileSaveBatch(Threaded ?? (IProfileStore)Disk);

                // Como GameBootstrap (T-D): la cartera guarda por batch.Durable; sin él, por el lote normal.
                Wallet = new Wallet(Profile, durableWallet ? Batch.Durable : Batch, Clock);
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

            /// <summary>Disco con escritor en segundo plano (null: <see cref="Disk"/>, síncrono).</summary>
            public ThreadedDisk? Threaded { get; }

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

        /// <summary>
        /// Disco de prueba que separa la serialización de la escritura (<see cref="IProfileByteStore"/>, como
        /// <c>LocalProfileStore</c>): graba cada escritura con el hilo que la hizo, puede fallar a propósito y puede retener al
        /// escritor a mitad de una escritura (<see cref="Hold"/>). Solo retiene al escritor (los hilos que no son el que creó el
        /// disco, como el gancho de <c>LocalProfileStoreTests</c>): una escritura del hilo principal nunca se queda en la
        /// compuerta, así que si no esperase al escritor entraría con él a medias, y eso queda anotado en
        /// <see cref="Overlaps"/> sea cual sea el orden en que los suelte el sistema.
        /// </summary>
        private sealed class ThreadedDisk : IProfileByteStore
        {
            private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

            private readonly object _lock = new object();
            private readonly List<string> _snapshots = new List<string>();
            private readonly List<bool> _onMain = new List<bool>();
            private readonly ManualResetEventSlim _open = new ManualResetEventSlim(true);
            private readonly ManualResetEventSlim _entered = new ManualResetEventSlim(false);
            private readonly int _mainThread = Thread.CurrentThread.ManagedThreadId;
            private int _failuresLeft;

            // Escrituras dentro de WriteBytes ahora mismo (también la retenida) y las que entraron con otra a medias.
            private int _inFlight;
            private int _overlaps;

            /// <summary>
            /// Escrituras que empezaron con otra a medias (por ejemplo, una síncrona del hilo principal que no esperó al
            /// escritor retenido). Con el código correcto, siempre 0.
            /// </summary>
            public int Overlaps => Volatile.Read(ref _overlaps);

            public int FailuresLeft
            {
                get
                {
                    lock (_lock)
                    {
                        return _failuresLeft;
                    }
                }

                set
                {
                    lock (_lock)
                    {
                        _failuresLeft = value;
                    }
                }
            }

            public int Count
            {
                get
                {
                    lock (_lock)
                    {
                        return _snapshots.Count;
                    }
                }
            }

            public Profile Last
            {
                get
                {
                    lock (_lock)
                    {
                        return ProfileJson.Deserialize(_snapshots[_snapshots.Count - 1]);
                    }
                }
            }

            public bool LastOnMainThread
            {
                get
                {
                    lock (_lock)
                    {
                        return _onMain[_onMain.Count - 1];
                    }
                }
            }

            /// <summary>Las semillas de cada escritura, en el orden en que llegaron al disco.</summary>
            public int[] SeedsWritten()
            {
                lock (_lock)
                {
                    var seeds = new int[_snapshots.Count];
                    for (int i = 0; i < seeds.Length; i++)
                    {
                        seeds[i] = ProfileJson.Deserialize(_snapshots[i]).Wallet.Seeds;
                    }

                    return seeds;
                }
            }

            public int WritesOnMainThread
            {
                get
                {
                    lock (_lock)
                    {
                        int n = 0;
                        for (int i = 0; i < _onMain.Count; i++)
                        {
                            n += _onMain[i] ? 1 : 0;
                        }

                        return n;
                    }
                }
            }

            /// <summary>La siguiente escritura del escritor (no la del hilo principal) se queda a medias hasta <see cref="Release"/>.</summary>
            public void Hold()
            {
                _entered.Reset();
                _open.Reset();
            }

            public void Release()
            {
                _open.Set();
            }

            public bool WaitEntered(int ms)
            {
                return _entered.Wait(ms);
            }

            public ProfileLoadResult Load()
            {
                lock (_lock)
                {
                    return _snapshots.Count == 0
                        ? new ProfileLoadResult(ProfileLoadStatus.NotFound, null)
                        : new ProfileLoadResult(ProfileLoadStatus.Loaded, ProfileJson.Deserialize(_snapshots[_snapshots.Count - 1]));
                }
            }

            public void Save(Profile profile)
            {
                WriteBytes(Serialize(profile));
            }

            public void Delete()
            {
                lock (_lock)
                {
                    _snapshots.Clear();
                    _onMain.Clear();
                }
            }

            public byte[] Serialize(Profile profile)
            {
                return Utf8.GetBytes(ProfileJson.Serialize(profile));
            }

            public void WriteBytes(byte[] bytes)
            {
                bool onMain = Thread.CurrentThread.ManagedThreadId == _mainThread;
                if (Interlocked.Increment(ref _inFlight) > 1)
                {
                    Interlocked.Increment(ref _overlaps);
                }

                try
                {
                    if (!onMain)
                    {
                        _entered.Set();
                        if (!_open.Wait(WaitMs))
                        {
                            throw new IOException("el test no soltó el disco");
                        }
                    }

                    lock (_lock)
                    {
                        if (_failuresLeft > 0)
                        {
                            _failuresLeft--;
                            throw new IOException("disco lleno");
                        }

                        _snapshots.Add(Utf8.GetString(bytes));
                        _onMain.Add(onMain);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _inFlight);
                }
            }
        }
    }
}
