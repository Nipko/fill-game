#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los puntos síncronos del guardado en la escena (T-D; revisión de C2, hallazgo 1), con el perfil en disco
    /// (<see cref="LocalProfileStore"/>) y el escritor de segundo plano retenido a mitad de una escritura
    /// (<see cref="HoldableProfileDisk"/>), como un disco lento:
    /// <list type="bullet">
    /// <item><description><c>OnApplicationPause(true)</c> y <c>OnApplicationQuit</c> (<c>GameBootstrap.PersistNow</c>)
    /// esperan al escritor y dejan en disco el perfil del último vertido, sin que nadie llame a <c>FlushSync</c>;</description></item>
    /// <item><description>una mutación de la cartera en juego (Cuia extra pagada con semillas; la cartera guarda por
    /// <c>ProfileSaveBatch.Durable</c>) espera al escritor y escribe en síncrono: las semillas ya están en disco al volver del
    /// toque;</description></item>
    /// <item><description>con el escritor atascado más que <c>ProfileSaveBatch.SyncTimeoutMs</c>, <c>PersistNow</c> vuelve en
    /// un solo tope (el cierre de su lote y el <c>FlushSync</c> de después lo comparten) y avisa de que el perfil no quedó en
    /// disco; al soltarse el escritor no se pierde nada.</description></item>
    /// </list>
    /// </summary>
    public sealed class SyncSaveTests
    {
        // Tutorial: la Cuia extra existe desde el 8 y en el 9 Capi no la regala (se paga con semillas).
        private const int Level = 9;

        // Tope de espera de cualquier comprobación con hilos (el escritor, el disco retenido).
        private const int WaitMs = 10000;

        // El disco retenido se suelta desde otro hilo pasado este tiempo: quien vuelve antes no esperó al escritor.
        private const int ReleaseAfterMs = 100;

        // Tope de espera al escritor del caso atascado (ProfileSaveBatch.SyncTimeoutMs; 1.000 ms en el juego).
        private const int CapMs = 400;

        private string? _dir;
        private HoldableProfileDisk? _disk;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            // Nada queda retenido ni pendiente: al destruir la escena, OnDestroy no espera al escritor ni escribe en la
            // carpeta borrada.
            _disk?.Release();
            GameBootstrap? boot = GameBootstrap.Current;
            ProfileSaveBatch? batch = boot != null ? boot.SaveBatch : null;
            if (batch != null)
            {
                batch.SyncTimeoutMs = ProfileSaveBatch.DefaultSyncTimeoutMs;
                batch.FlushSync(WaitMs);
            }

            LaunchOptions.Reset();
            if (_dir != null && Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [UnityTest]
        public IEnumerator Pause_WaitsForTheWriter_AndLeavesTheLastPourOnDisk()
        {
            yield return PourWithTheWriterHeld(0);
            AssertClosingWritesTheLastPour("OnApplicationPause", new object[] { true });
        }

        [UnityTest]
        public IEnumerator Quit_WaitsForTheWriter_AndLeavesTheLastPourOnDisk()
        {
            yield return PourWithTheWriterHeld(0);
            AssertClosingWritesTheLastPour("OnApplicationQuit", Array.Empty<object>());
        }

        [UnityTest]
        public IEnumerator AWalletMutationInPlay_WaitsForTheWriter_AndIsOnDiskWithoutFlush()
        {
            yield return PourWithTheWriterHeld(500);
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            ProfileWriter writer = batch.Writer!;
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(writer.IsIdle, Is.False, "el escritor sigue con el segundo vertido");
            int writes = batch.WriteCount;

            // «+1 cuia» por 150 semillas: Wallet.Apply guarda por batch.Durable dentro del lote de UseExtraCuia. El disco se
            // suelta a los 100 ms: el toque tiene que esperar al escritor y escribir después, en el hilo principal.
            Thread opener = _disk!.ReleaseLater(ReleaseAfterMs);
            bool used = boot.PressExtraCuia();
            bool idle = writer.IsIdle;
            Profile onDisk = OnDisk();
            opener.Join();

            Assert.That(used, Is.True, "Cuia extra con semillas");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(350));
            Assert.That(batch.WriteCount, Is.EqualTo(writes + 1), "el cobro y la cuia añadida, en una escritura (D-008)");
            Assert.That(batch.LastWriteInBackground, Is.False, "la cartera espera al escritor y escribe en síncrono");
            Assert.That(idle, Is.True, "al volver del toque, el escritor ya terminó el vertido anterior");
            Assert.That(_disk.LastOnMainThread, Is.True, "la escritura de la cartera, en el hilo principal");
            Assert.That(onDisk.Wallet.Seeds, Is.EqualTo(350), "las semillas, ya en disco sin Flush");
            Assert.That(onDisk.LevelState!.History, Is.EqualTo(boot.Profile!.LevelState!.History), "con la cuia añadida, en la misma escritura");
            Assert.That(onDisk.LevelState.Pours, Is.EqualTo(boot.Flow!.Session.AppliedPours));
            Assert.That(batch.PendingSave, Is.False);
            Assert.That(writer.AllWritten, Is.True);
        }

        [UnityTest]
        public IEnumerator WithTheWriterStuck_PauseReturnsInOneCap_AndWarnsThatTheProfileIsNotOnDisk()
        {
            yield return PourWithTheWriterHeld(0);
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            batch.SyncTimeoutMs = CapMs;

            // Una mutación de la cartera fuera de un lote con el escritor atascado: espera un tope, no llega al disco y queda
            // pendiente en la cartera y en el lote. PersistNow la vuelve a anotar en su lote (Wallet.Flush por batch.Durable):
            // el cierre del lote espera un tope entero y el FlushSync de después solo tiene lo que queda de él.
            TransactionResult earned = boot.Wallet!.Apply(Transaction.Earn(SeedSource.Basket, 50));
            Assert.That(earned.IsApplied, Is.True);
            Assert.That(earned.Saved, Is.False, "con el escritor atascado no llegó al disco");
            Assert.That(boot.Wallet.PendingSave, Is.True);
            Assert.That(batch.PendingSave, Is.True);

            LogAssert.Expect(LogType.Warning, new Regex("el perfil no quedó en disco \\(segundo plano\\)"));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            SendUnityMessage(boot, "OnApplicationPause", new object[] { true });
            watch.Stop();
            long elapsed = watch.ElapsedMilliseconds;
            TestContext.WriteLine("OnApplicationPause(true) con el escritor atascado: " + elapsed + " ms (tope " + CapMs + " ms).");
            Assert.That(elapsed, Is.GreaterThanOrEqualTo(CapMs * 3 / 4), "esperó al escritor hasta el tope");
            Assert.That(elapsed, Is.LessThan(CapMs * 3 / 2), "un solo tope para todo el cierre, no dos");
            Assert.That(boot.Wallet.PendingSave, Is.False, "la cartera lo dejó en el lote del cierre…");
            Assert.That(batch.PendingSave, Is.True, "…que lo sigue teniendo pendiente");
            Assert.That(OnDisk().LevelState!.Pours, Is.EqualTo(1), "en disco no hay nada nuevo");

            // Al soltarse el escritor no se pierde nada: el siguiente guardado síncrono lo lleva todo.
            _disk!.Release();
            Assert.That(batch.FlushSync(WaitMs), Is.True);
            Profile onDisk = OnDisk();
            Assert.That(onDisk.Wallet.Seeds, Is.EqualTo(50));
            Assert.That(onDisk.LevelState!.Pours, Is.EqualTo(2));
            Assert.That(batch.PendingSave, Is.False);
        }

        // Nivel 9 con el perfil en disco y seeds semillas: un vertido ya en disco y otro cuya escritura se queda a medias en
        // el escritor (el disco sigue con el primero).
        private IEnumerator PourWithTheWriterHeld(int seeds)
        {
            _dir = Path.Combine(Application.temporaryCachePath, "af_sync_" + Guid.NewGuid().ToString("N"));
            var local = new LocalProfileStore(_dir);
            local.Save(FunnelSetup.Profile(seeds));
            _disk = new HoldableProfileDisk(local);
            LaunchOptions.ProfileStoreOverride = _disk;
            yield return MechanicTestKit.LoadPlaying(Level);
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            Assert.That(batch.Inner, Is.SameAs(_disk));
            Assert.That(batch.Writer, Is.Not.Null, "con el perfil en disco, el escritor de segundo plano está en marcha");
            ProfileWriter writer = batch.Writer!;
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.MoveCount, Is.GreaterThanOrEqualTo(4), "quedan vertidos útiles después de los dos del test");

            PourOnce(boot, solution.GetMove(0));
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(writer.WaitIdle(WaitMs), Is.True);
            Assert.That(OnDisk().LevelState!.Pours, Is.EqualTo(1), "el primer vertido, en disco");

            _disk.Hold();
            PourOnce(boot, solution.GetMove(1));
            Assert.That(batch.LastWriteInBackground, Is.True, "el vertido solo entrega los bytes al escritor");
            Assert.That(_disk.WaitEntered(WaitMs), Is.True, "el escritor empezó la escritura del segundo vertido");
            Assert.That(writer.IsIdle, Is.False, "y se queda a medias");
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(2));
            Assert.That(OnDisk().LevelState!.Pours, Is.EqualTo(1), "el disco sigue con el primer vertido");
        }

        // a) y b): el mensaje de Unity con el disco soltado a los 100 ms. Lo que hay en disco se lee nada más volver: si no
        // hubiera esperado al escritor, este seguiría retenido y el disco tendría el primer vertido.
        private void AssertClosingWritesTheLastPour(string message, object[] args)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            ProfileWriter writer = batch.Writer!;
            int writes = batch.WriteCount;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Thread opener = _disk!.ReleaseLater(ReleaseAfterMs);
            SendUnityMessage(boot, message, args);
            long elapsed = watch.ElapsedMilliseconds;
            bool idle = writer.IsIdle;
            bool allWritten = writer.AllWritten;
            LevelStateData onDisk = OnDisk().LevelState!;
            opener.Join();

            Assert.That(idle, Is.True, message + ": al volver, el escritor ya terminó");
            Assert.That(allWritten, Is.True, message + ": la última entrega, en disco");
            Assert.That(elapsed, Is.GreaterThanOrEqualTo(ReleaseAfterMs / 2), message + ": esperó al escritor");
            Assert.That(onDisk.Pours, Is.EqualTo(boot.Flow!.Session.AppliedPours), message + ": el tablero del último vertido, sin FlushSync");
            Assert.That(onDisk.History, Is.EqualTo(boot.Profile!.LevelState!.History));
            Assert.That(batch.WriteCount, Is.EqualTo(writes), message + ": no hacía falta otra escritura");
            Assert.That(batch.PendingSave, Is.False);
        }

        private Profile OnDisk()
        {
            ProfileLoadResult loaded = new LocalProfileStore(_dir!).Load();
            Assert.That(loaded.HasProfile, Is.True, "perfil en disco: " + loaded.Status);
            return loaded.Profile!;
        }

        private static void PourOnce(GameBootstrap boot, Pour move)
        {
            Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
        }

        // Los mensajes de Unity de GameBootstrap (privados), como los manda el sistema al pasar a segundo plano o al cerrar.
        private static void SendUnityMessage(GameBootstrap boot, string name, object[] args)
        {
            MethodInfo? method = typeof(GameBootstrap).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "GameBootstrap." + name);
            method!.Invoke(boot, args);
        }
    }

    /// <summary>
    /// El perfil en disco de verdad (<see cref="LocalProfileStore"/>) con un disco que puede retener al escritor a mitad de
    /// una escritura, como el <c>ThreadedDisk</c> de <c>SaveBatchTests</c>: tras <see cref="Hold"/>, la siguiente
    /// <see cref="WriteBytes"/> espera, sin tocar los archivos, hasta <see cref="Release"/> (desde cualquier hilo). Anota en
    /// qué hilo fue la última escritura. Al ser un <see cref="IProfileByteStore"/>, <c>ProfileSaveBatch</c> le pone escritor
    /// en segundo plano, como a <see cref="LocalProfileStore"/>.
    /// </summary>
    internal sealed class HoldableProfileDisk : IProfileByteStore
    {
        // Si el test no suelta el disco, la escritura falla (en el hilo del escritor, donde se recoge como un fallo de disco).
        private const int HoldLimitMs = 10000;

        private readonly LocalProfileStore _inner;
        private readonly object _lock = new object();
        private readonly ManualResetEventSlim _open = new ManualResetEventSlim(true);
        private readonly ManualResetEventSlim _entered = new ManualResetEventSlim(false);
        private readonly int _mainThread = Thread.CurrentThread.ManagedThreadId;
        private bool _lastOnMainThread;

        public HoldableProfileDisk(LocalProfileStore inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>El almacén de verdad.</summary>
        public LocalProfileStore Inner => _inner;

        /// <summary>La última escritura terminada fue en el hilo que creó el disco (el principal).</summary>
        public bool LastOnMainThread
        {
            get
            {
                lock (_lock)
                {
                    return _lastOnMainThread;
                }
            }
        }

        /// <summary>La siguiente escritura se queda a medias hasta <see cref="Release"/>.</summary>
        public void Hold()
        {
            _entered.Reset();
            _open.Reset();
        }

        /// <summary>Suelta la escritura retenida (y las siguientes).</summary>
        public void Release()
        {
            _open.Set();
        }

        /// <summary>Espera, como mucho <paramref name="ms"/>, a que alguien empiece a escribir tras <see cref="Hold"/>.</summary>
        public bool WaitEntered(int ms)
        {
            return _entered.Wait(ms);
        }

        /// <summary>Suelta el disco desde otro hilo dentro de <paramref name="ms"/> ms (un disco lento que termina).</summary>
        public Thread ReleaseLater(int ms)
        {
            var opener = new Thread(() =>
            {
                Thread.Sleep(ms);
                Release();
            });
            opener.Start();
            return opener;
        }

        public ProfileLoadResult Load()
        {
            return _inner.Load();
        }

        public void Save(Profile profile)
        {
            WriteBytes(Serialize(profile));
        }

        public void Delete()
        {
            _inner.Delete();
        }

        public byte[] Serialize(Profile profile)
        {
            return _inner.Serialize(profile);
        }

        public void WriteBytes(byte[] bytes)
        {
            _entered.Set();
            if (!_open.Wait(HoldLimitMs))
            {
                throw new IOException("el test no soltó el disco");
            }

            _inner.WriteBytes(bytes);
            lock (_lock)
            {
                _lastOnMainThread = Thread.CurrentThread.ManagedThreadId == _mainThread;
            }
        }
    }
}
