#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    /// disco; al soltarse el escritor no se pierde nada;</description></item>
    /// <item><description>la línea <c>AF_PERF</c> de cada vertido (T-D.perf; revisión de C2, ronda 4, hallazgo 1), con la que el
    /// dueño mide en el teléfono: <c>save_ms</c> es <c>ProfileSaveBatch.LastWriteMs</c> (solo el hilo principal),
    /// <c>bg_ms</c> es <c>ProfileSaveBatch.LastBackgroundMs</c> (la última escritura terminada del escritor) y
    /// <c>sync</c> solo es true en el vertido que gana, que espera al escritor en el toque;</description></item>
    /// <item><description>un fallo del escritor en su hilo se avisa en el hilo principal en el siguiente toque
    /// (<c>GameBootstrap.ReportBackgroundFailures</c>) y el vertido siguiente lo lleva al disco.</description></item>
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
            // suelta a los 100 ms: el toque tiene que esperar al escritor y escribir después, en el hilo principal. El disco
            // solo retiene al escritor: si la cartera escribiera sin esperarlo (o sin pasar por él), entraría con él a medias
            // y lo anotaría Overlaps.
            Thread opener = _disk!.ReleaseLater(ReleaseAfterMs);
            bool used = boot.PressExtraCuia();
            bool idle = writer.IsIdle;
            bool lastOnMain = _disk.LastOnMainThread;
            Profile onDisk = OnDisk();
            opener.Join();

            Assert.That(_disk.Overlaps, Is.EqualTo(0), "la cartera esperó al escritor: nunca escribió con él a medias");
            Assert.That(used, Is.True, "Cuia extra con semillas");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(350));
            Assert.That(batch.WriteCount, Is.EqualTo(writes + 1), "el cobro y la cuia añadida, en una escritura (D-008)");
            Assert.That(batch.LastWriteInBackground, Is.False, "la cartera espera al escritor y escribe en síncrono");
            Assert.That(idle, Is.True, "al volver del toque, el escritor ya terminó el vertido anterior");
            Assert.That(lastOnMain, Is.True, "la escritura de la cartera, en el hilo principal");
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
            Assert.That(_disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        [UnityTest]
        public IEnumerator AfPerf_SaveMsIsTheMainThread_BgMsIsTheWriter_AndOnlyTheWinningPourIsSync()
        {
            // T-D.perf: la línea con la que el dueño mide el criterio del teléfono (p95 < 5 ms y ninguno > 8 ms). Se lee la
            // línea que de verdad emite GameBootstrap.TapCuia y se compara con el lote: si se intercambian save_ms y bg_ms, si
            // bg_ms sale a 0 o si sync se invierte, falla.
            yield return LoadWithTheProfileOnDisk(0);
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            ProfileWriter writer = batch.Writer!;
            LevelFlow flow = boot.Flow!;
            SolveResult solution = Solve(flow);
            Assert.That(DevProbe.Enabled, Is.True, "la sonda escribe en el Editor");
            var perf = new List<string>();
            Application.LogCallback capture = (condition, stackTrace, type) =>
            {
                if (condition.StartsWith("AF_PERF ", StringComparison.Ordinal))
                {
                    perf.Add(condition);
                }
            };

            Application.logMessageReceived += capture;
            try
            {
                // 1. La escritura del primer vertido se queda ~100 ms en el escritor: bg_ms de verdad, muy distinto de save_ms.
                Assert.That(writer.WaitIdle(WaitMs), Is.True);
                _disk!.Hold();
                PourOnce(boot, solution.GetMove(0));
                Assert.That(_disk.WaitEntered(WaitMs), Is.True, "el escritor empezó la escritura del primer vertido");
                Thread opener = _disk.ReleaseLater(ReleaseAfterMs);
                Assert.That(writer.WaitIdle(WaitMs), Is.True);
                opener.Join();
                PlayModeHelpers.StepUntilIdle(boot, 4000);

                // 2. El segundo vertido, con el disco retenido otra vez: mientras su escritura sigue a medias, bg_ms es la del
                // primero, igual en la línea y en el lote.
                _disk.Hold();
                perf.Clear();
                PourOnce(boot, solution.GetMove(1));
                double saveMs = batch.LastWriteMs;
                double bgMs = batch.LastBackgroundMs;
                bool inBackground = batch.LastWriteInBackground;
                Assert.That(_disk.WaitEntered(WaitMs), Is.True, "el escritor empezó la escritura del segundo vertido");
                Assert.That(perf.Count, Is.EqualTo(1), "una línea AF_PERF por vertido: " + string.Join(" | ", perf));
                Match pour = PourLine(false).Match(perf[0]);
                Assert.That(pour.Success, Is.True, "formato de AF_PERF de un vertido en segundo plano: " + perf[0]);
                Assert.That(inBackground, Is.True, "el vertido solo entrega los bytes al escritor");
                Assert.That(pour.Groups[1].Value, Is.EqualTo(Ms(saveMs)), "save_ms = LastWriteMs (solo el hilo principal)");
                Assert.That(pour.Groups[2].Value, Is.EqualTo(Ms(bgMs)), "bg_ms = LastBackgroundMs (la última escritura del escritor)");
                Assert.That(bgMs, Is.GreaterThanOrEqualTo(ReleaseAfterMs / 2), "bg_ms: la escritura retenida del primer vertido");
                Assert.That(pour.Groups[2].Value, Is.Not.EqualTo(pour.Groups[1].Value), "bg_ms no es save_ms");
                _disk.Release();
                Assert.That(writer.WaitIdle(WaitMs), Is.True);
                PlayModeHelpers.StepUntilIdle(boot, 4000);

                // 3. Hasta el penúltimo vertido, cuya escritura se queda a medias en el escritor (un disco lento).
                int last = solution.MoveCount - 1;
                for (int i = 2; i < last; i++)
                {
                    if (i == last - 1)
                    {
                        Assert.That(writer.WaitIdle(WaitMs), Is.True);
                        _disk.Hold();
                    }

                    perf.Clear();
                    PourOnce(boot, solution.GetMove(i));
                    Assert.That(perf.Count, Is.EqualTo(1), "vertido " + (i + 1) + ": una línea AF_PERF");
                    Assert.That(PourLine(false).IsMatch(perf[0]), Is.True, "vertido " + (i + 1) + ", en segundo plano: " + perf[0]);
                    PlayModeHelpers.StepUntilIdle(boot, 4000);
                }

                // 4. El vertido que gana: fin de nivel, espera al escritor (soltado desde otro hilo a los 100 ms) y escribe en el
                // toque. Es el único con sync = true, y su save_ms incluye esa espera.
                Assert.That(_disk.WaitEntered(WaitMs), Is.True, "el escritor empezó la escritura del penúltimo vertido");
                Assert.That(writer.IsIdle, Is.False, "y se queda a medias");
                Pour win = solution.GetMove(last);
                Assert.That(boot.TapCuia(win.Source).Kind, Is.EqualTo(TapKind.Selected));
                perf.Clear();
                Thread winOpener = _disk.ReleaseLater(ReleaseAfterMs);
                TapResult won = boot.TapCuia(win.Target);

                // Lo que hay al volver del toque, antes de esperar al hilo que suelta el disco.
                bool idle = writer.IsIdle;
                bool lastOnMain = _disk.LastOnMainThread;
                Profile onDisk = OnDisk();
                winOpener.Join();

                Assert.That(won.Kind, Is.EqualTo(TapKind.Poured));
                Assert.That(_disk.Overlaps, Is.EqualTo(0), "la victoria esperó al escritor: nunca escribió con él a medias");
                Assert.That(batch.LastWriteInBackground, Is.False, "el vertido que gana escribe en síncrono");
                Assert.That(idle, Is.True, "al volver del toque, el escritor ya terminó el penúltimo vertido");
                Assert.That(lastOnMain, Is.True, "la victoria, en el hilo principal");
                Assert.That(onDisk.LevelState, Is.Null, "la última escritura es la victoria: en disco al volver del toque, sin Flush");
                Assert.That(onDisk.Wallet.Seeds, Is.EqualTo(boot.Wallet!.Seeds), "con la Cosecha");
                Assert.That(perf.Count, Is.EqualTo(1), "una línea AF_PERF para el vertido que gana: " + string.Join(" | ", perf));
                Match winLine = PourLine(true).Match(perf[0]);
                Assert.That(winLine.Success, Is.True, "el vertido que gana sale con sync = true: " + perf[0]);
                Assert.That(winLine.Groups[1].Value, Is.EqualTo(Ms(batch.LastWriteMs)), "save_ms = LastWriteMs");
                Assert.That(winLine.Groups[2].Value, Is.EqualTo(Ms(batch.LastBackgroundMs)), "bg_ms = LastBackgroundMs");
                Assert.That(batch.LastWriteMs, Is.GreaterThanOrEqualTo(ReleaseAfterMs / 2), "save_ms del vertido que gana: la espera al escritor, en el toque");
                Assert.That(batch.PendingSave, Is.False);
                Assert.That(writer.AllWritten, Is.True);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }

        [UnityTest]
        public IEnumerator AFailedBackgroundWrite_IsReportedOnTheNextTap_AndTheNextPourTakesItToDisk()
        {
            // T-D.retry en la escena: el escritor falla en su hilo (disco lleno); la excepción no sale de él y el hilo
            // principal avisa en el toque siguiente (ReportBackgroundFailures, desde TapCuia). El perfil sigue en memoria y el
            // vertido siguiente lo lleva entero al disco.
            yield return LoadWithTheProfileOnDisk(0);
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            ProfileWriter writer = batch.Writer!;
            SolveResult solution = Solve(boot.Flow!);
            Assert.That(writer.WaitIdle(WaitMs), Is.True);

            _disk!.FailNext();
            PourOnce(boot, solution.GetMove(0));
            Assert.That(writer.WaitIdle(WaitMs), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(writer.AllWritten, Is.False, "la escritura del primer vertido falló en el hilo del escritor");
            Assert.That(OnDisk().LevelState?.Pours ?? 0, Is.EqualTo(0), "en disco no está el primer vertido");

            const string Warning = "falló la escritura del perfil en segundo plano";
            var warnings = new List<string>();
            Application.LogCallback capture = (condition, stackTrace, type) =>
            {
                if (type == LogType.Warning && condition.Contains(Warning))
                {
                    warnings.Add(condition);
                }
            };

            Application.logMessageReceived += capture;
            try
            {
                LogAssert.Expect(LogType.Warning, new Regex(Warning + " \\(fallos nuevos: 1\\)"));
                PourOnce(boot, solution.GetMove(1));
                Assert.That(warnings.Count, Is.EqualTo(1), "el aviso sale en el toque siguiente, una vez: " + string.Join(" | ", warnings));
                StringAssert.Contains("IOException", warnings[0]);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }

            Assert.That(batch.BackgroundFailures, Is.EqualTo(1));
            Assert.That(writer.WaitIdle(WaitMs), Is.True);
            Assert.That(batch.PendingSave, Is.False, "el vertido siguiente lo llevó entero");
            Assert.That(writer.AllWritten, Is.True);
            Assert.That(OnDisk().LevelState!.Pours, Is.EqualTo(2), "los dos vertidos, en disco");
            Assert.That(_disk.Overlaps, Is.EqualTo(0), "nunca dos escrituras a la vez");
        }

        // Nivel 9 con el perfil en disco (seeds semillas) y el escritor de segundo plano en marcha.
        private IEnumerator LoadWithTheProfileOnDisk(int seeds)
        {
            _dir = Path.Combine(Application.temporaryCachePath, "af_sync_" + Guid.NewGuid().ToString("N"));
            var local = new LocalProfileStore(_dir);
            local.Save(FunnelSetup.Profile(seeds));
            _disk = new HoldableProfileDisk(local);
            LaunchOptions.ProfileStoreOverride = _disk;
            yield return MechanicTestKit.LoadPlaying(Level);
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            Assert.That(((SigningProfileStore)batch.Inner).Inner, Is.SameAs(_disk)); // B8: el lote firma (SigningProfileStore) sobre el almacén
            Assert.That(batch.Writer, Is.Not.Null, "con el perfil en disco, el escritor de segundo plano está en marcha");
        }

        // Nivel 9 con el perfil en disco y seeds semillas: un vertido ya en disco y otro cuya escritura se queda a medias en
        // el escritor (el disco sigue con el primero).
        private IEnumerator PourWithTheWriterHeld(int seeds)
        {
            yield return LoadWithTheProfileOnDisk(seeds);
            GameBootstrap boot = GameBootstrap.Current!;
            ProfileSaveBatch batch = boot.SaveBatch!;
            ProfileWriter writer = batch.Writer!;
            LevelFlow flow = boot.Flow!;
            SolveResult solution = Solve(flow);

            PourOnce(boot, solution.GetMove(0));
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(writer.WaitIdle(WaitMs), Is.True);
            Assert.That(OnDisk().LevelState!.Pours, Is.EqualTo(1), "el primer vertido, en disco");

            _disk!.Hold();
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

            Assert.That(_disk.Overlaps, Is.EqualTo(0), message + ": nunca dos escrituras a la vez");
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

        private static SolveResult Solve(LevelFlow flow)
        {
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.MoveCount, Is.GreaterThanOrEqualTo(4), "quedan vertidos útiles después de los dos del test");
            return solution;
        }

        private static void PourOnce(GameBootstrap boot, Pour move)
        {
            Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
        }

        // La línea AF_PERF de un vertido del nivel 9 con una escritura (DevProbe.SaveCost); grupos: save_ms y bg_ms.
        private static Regex PourLine(bool sync)
        {
            return new Regex("^AF_PERF \\{\"event\":\"pour\",\"level\":" + Level.ToString(CultureInfo.InvariantCulture)
                + ",\"writes\":1,\"save_ms\":(\\d+\\.\\d\\d),\"bg_ms\":(\\d+\\.\\d\\d),\"sync\":" + (sync ? "true" : "false") + "\\}$");
        }

        // Como DevProbe.SaveCost formatea save_ms y bg_ms.
        private static string Ms(double value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
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
    /// <see cref="WriteBytes"/> del escritor espera, sin tocar los archivos, hasta <see cref="Release"/> (desde cualquier
    /// hilo). Solo retiene al escritor (los hilos que no son el que creó el disco, como el gancho de
    /// <c>LocalProfileStoreTests</c>): una escritura del hilo principal nunca se queda en la compuerta, así que si no esperase
    /// al escritor entraría con él a medias, y eso queda anotado en <see cref="Overlaps"/> sea cual sea el orden en que los
    /// suelte el sistema. Anota en qué hilo fue la última escritura y puede hacer fallar la siguiente del escritor
    /// (<see cref="FailNext"/>). Al ser un <see cref="IProfileByteStore"/>,
    /// <c>ProfileSaveBatch</c> le pone escritor en segundo plano, como a <see cref="LocalProfileStore"/>.
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

        // Escrituras dentro de WriteBytes ahora mismo (también la retenida) y las que entraron con otra a medias.
        private int _inFlight;
        private int _overlaps;

        // 1: la siguiente escritura del escritor falla (FailNext).
        private int _failNext;

        public HoldableProfileDisk(LocalProfileStore inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>El almacén de verdad.</summary>
        public LocalProfileStore Inner => _inner;

        /// <summary>
        /// Escrituras que empezaron con otra a medias (por ejemplo, una síncrona del hilo principal que no esperó al
        /// escritor retenido). Con el código correcto, siempre 0.
        /// </summary>
        public int Overlaps => Volatile.Read(ref _overlaps);

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

        /// <summary>La siguiente escritura del escritor (no la del hilo principal) se queda a medias hasta <see cref="Release"/>.</summary>
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

        /// <summary>
        /// La siguiente escritura del escritor (no la del hilo principal) falla con <see cref="IOException"/> en su hilo, sin
        /// tocar los archivos (un disco lleno).
        /// </summary>
        public void FailNext()
        {
            Interlocked.Exchange(ref _failNext, 1);
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
                    if (!_open.Wait(HoldLimitMs))
                    {
                        throw new IOException("el test no soltó el disco");
                    }

                    if (Interlocked.Exchange(ref _failNext, 0) == 1)
                    {
                        throw new IOException("disco lleno (prueba)");
                    }
                }

                _inner.WriteBytes(bytes);
                lock (_lock)
                {
                    _lastOnMainThread = onMain;
                }
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }
    }
}
