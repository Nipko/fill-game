#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas visuales de revisión (no forman parte de la suite: <c>[Explicit]</c>). Simulan una pantalla de
    /// 1080 × 2340 px (20:9 aprox., 360 × 780 dp) con zona segura y escriben PNG en <c>Logs/snapshots/</c> (ignorada por
    /// git). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter VisualSnapshotTests</c> sin
    /// <c>-nographics</c>. El ambiente se congela con semilla y tiempo fijos (guía visual v2, 9) y se usa siempre el arte
    /// procedural. Con D-004 (cuias más anchas): <c>02b_board_l1</c> (3 cuias de 64 dp), <c>04b_pouring_l1</c> (primer
    /// vertido del nivel 1: el origen inclinado queda bajo el canto interior del marco y el chorro entra en la boca del
    /// destino), <c>02d_board_l2</c> (5 cuias de ≈ 47 dp) y <c>02e_board_l4</c> (6 cuias en dos filas de 3, 48 dp). Con D-005 y
    /// D-006 (vertido, cuia y tablero como Magic Sort), con el reloj del test: <c>04c_pouring_run3_l2</c> (racha de 3 en run,
    /// durante la 2.ª dosis), <c>04d_pouring_empty_l1</c> (un vertido que vacía el origen, ≈ 92°), <c>09_board_13</c>
    /// (13 cuias en 3 filas de 5/4/4) y <c>09b_pouring_row1_13</c> (vertido de la fila 3 a la fila 1 que vacía el origen, sobre el HUD).
    /// Hoja de vertido como la de Magic Sort (ficha §2.6): <c>10_sheet_l1_0000</c> … <c>10_sheet_l1_1000</c>, una captura
    /// cada 100 ms desde la salida de un vertido de 1 dosis de una cuia llena (nivel 1, 0 → 2, ya elevada). Cada tablero y
    /// cada fotograma de la hoja escriben en el log una línea <c>AF_BOARD</c> o <c>AF_SHEET</c> con sus medidas.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class VisualSnapshotTests
    {
        private const int Width = 1080;
        private const int Height = 2340;

        private Camera? _camera;
        private RenderTexture? _target;

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            AmbientLayer.Frozen = false;
            LaunchOptions.Reset();
            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
            }

            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }
        }

        [UnityTest]
        public IEnumerator CaptureKeyScreens()
        {
            string dir = Prepare();

            // Nivel 7: intro, tablero, selección (solo el origen sube, sin aros en los destinos: D-002) y vertido.
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Attach(boot);
            yield return Wait(0.55f);
            Save(dir, "01_intro_l7");
            yield return Wait(0.6f);
            Save(dir, "01b_intro_tint_l7");
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing, "fin de la intro");
            yield return Wait(0.3f);
            Save(dir, "02_board_l7");
            LogBoard("02_board_l7", boot);
            SolveResult solution = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            Pour first = solution.GetMove(0);
            boot.TapCuia(first.Source);
            yield return Wait(0.25f);
            Save(dir, "03_selected_l7");
            boot.TapCuia(first.Target);
            // Con la cabeza del chorro ya en la superficie del destino (revisión visual del 2026-10-07: la captura salía con
            // el chorro a medio crecer y parecía terminar en el borde).
            yield return PlayModeHelpers.WaitFor(StreamReachedSurface, "chorro hasta la superficie");
            Save(dir, "04_pouring_l7");
            yield return PlayModeHelpers.WaitFor(() => boot.View!.IsViewIdle, "fin del vertido");
            yield return Wait(0.2f);
            Save(dir, "05_after_pour_l7");

            // Nivel 1: tablero de una fila (02b), primer vertido (04b), sellado, celebración y victoria.
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            boot.TapAnywhere();
            yield return Wait(0.3f);
            Save(dir, "02b_board_l1");
            SolveResult s1 = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            boot.TapCuia(s1.GetMove(0).Source);
            boot.TapCuia(s1.GetMove(0).Target);
            yield return PlayModeHelpers.WaitFor(StreamReachedSurface, "chorro hasta la superficie (nivel 1)");
            Save(dir, "04b_pouring_l1");
            GameBootstrap boot1 = boot;
            yield return PlayModeHelpers.WaitFor(() => boot1.View!.IsViewIdle, "fin del vertido (nivel 1)");
            for (int i = 1; i < s1.MoveCount; i++)
            {
                boot.TapCuia(s1.GetMove(i).Source);
                boot.TapCuia(s1.GetMove(i).Target);
            }

            LevelFlow f1 = boot.Flow;
            yield return PlayModeHelpers.WaitFor(() => f1.Phase == FlowPhase.Celebration, "celebración");
            yield return Wait(0.9f); // estallido a mitad de vuelo (arranca a 0,5 s; guía 6.9)
            Save(dir, "06_celebration_l1");
            yield return PlayModeHelpers.WaitFor(() => f1.Phase == FlowPhase.VictoryScreen, "victoria");
            yield return Wait(0.9f); // guía 6.9 y VA-13: Siguiente aparece a 1,4 s del inicio de la celebración
            Save(dir, "07_victory_l1");

            // Nivel 6: Aguas estancadas.
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            boot.TapAnywhere();
            boot.TapCuia(3);
            boot.TapCuia(4);
            LevelFlow f6 = boot.Flow!;
            yield return PlayModeHelpers.WaitFor(() => f6.Phase == FlowPhase.StuckModal, "modal");
            yield return Wait(0.4f);
            Save(dir, "08_stuck_l6");
            Assert.That(File.Exists(Path.Combine(dir, "08_stuck_l6.png")), Is.True);

            // D-004: nivel 2 (5 cuias de ≈ 47 dp en una fila) y nivel 4 (6 cuias en dos filas de 3, 48 dp).
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            boot.TapAnywhere();
            yield return Wait(0.3f);
            Save(dir, "02d_board_l2");
            LogBoard("02d_board_l2", boot);

            yield return PlayModeHelpers.LoadGame(4, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            boot.TapAnywhere();
            yield return Wait(0.3f);
            Save(dir, "02e_board_l4");
            LogBoard("02e_board_l4", boot);
            Assert.That(File.Exists(Path.Combine(dir, "02e_board_l4.png")), Is.True);

            // D-005 y D-006, con el reloj del test: nivel 2 en run, la racha de 3 açaí (2 → 3) durante la 2.ª dosis.
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Pour.Pour.Amount, Is.EqualTo(3));
            CuiaView target3 = boot.View!.Views[3];
            for (int i = 0; i < 200 && target3.LiquidLevel <= 1.5f; i++)
            {
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(target3.LiquidLevel, Is.GreaterThan(1f).And.LessThan(3f), "durante la 2.ª dosis");
            Save(dir, "04c_pouring_run3_l2");
            PlayModeHelpers.StepUntilIdle(boot, 3000);

            // Un vertido que vacía el origen (≈ 92°): nivel 1, último vertido de la solución del informe, 1 → 2 ×3 (tras
            // 0 → 2 y 1 → 0), al terminar la 3.ª dosis.
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            int[,] l1 = { { 0, 2 }, { 1, 0 } };
            for (int i = 0; i < 2; i++)
            {
                boot.TapCuia(l1[i, 0]);
                boot.TapCuia(l1[i, 1]);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            boot.TapCuia(1);
            Assert.That(boot.TapCuia(2).Pour.Pour.Amount, Is.EqualTo(3));
            PourTimings timings = boot.View!.Timings;
            int emptyAt = Mathf.RoundToInt((timings.TravelMs * BoardAnimator.StreamStartFraction) + (3f * timings.DoseMs)) - 8;
            PlayModeHelpers.TickExact(boot, emptyAt);
            Save(dir, "04d_pouring_empty_l1");
            Debug.Log("AF_SNAP 04d: inclinación " + Mathf.Abs(boot.View.Views[1].BodyAngle).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "°");

            // D-006: tablero de 13 cuias (3 filas de 5/4/4) y un vertido de la fila 3 a la fila 1, con el origen sobre el HUD.
            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.ThirteenCuiaPack(), 1, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            PlayModeHelpers.TickExact(boot, 300);
            Save(dir, "09_board_13");
            LogBoard("09_board_13", boot);
            // De la cuia 12 (fila 3, a la derecha) a la 0 (fila 1, a la izquierda), con una sola dosis en el origen: al
            // vaciarse queda a ≈ 92° a la altura del borde de la fila 1 y tapa el rótulo del nivel, como Magic Sort en t = 250.
            Pour[]? setup = PlayModeHelpers.FindSetup(boot.Flow!.Session.Current, 12, 0, boot.Flow.Session.PourMode, boot.Flow.Session.FutileRuleEnabled);
            Assert.That(setup, Is.Not.Null);
            for (int i = 0; i < setup!.Length; i++)
            {
                boot.TapCuia(setup[i].Source);
                boot.TapCuia(setup[i].Target);
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            boot.TapCuia(12);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(boot.Flow.Session.Current[12].Count, Is.EqualTo(0), "el origen se vacía");
            PlayModeHelpers.TickExact(boot, Mathf.RoundToInt((timings.TravelMs * BoardAnimator.StreamStartFraction) + timings.DoseMs) - 8);
            Debug.Log("AF_SNAP 09b: inclinación " + Mathf.Abs(boot.View!.Views[12].BodyAngle).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "°");
            Save(dir, "09b_pouring_row1_13");
            Assert.That(File.Exists(Path.Combine(dir, "09b_pouring_row1_13.png")), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 3000);

            // Hoja de vertido (D-006; ficha §2.6 y ms_pour_sheet.png, una viñeta cada 100 ms): nivel 1, 0 → 2, la cuia llena
            // suelta 1 dosis. Se selecciona y se espera a que suba, como en la hoja (que la tiene elevada desde 1.400 ms).
            LaunchOptions.Reset(); // sin el paquete de 13 cuias: el nivel 1 del tutorial
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            LogBoard("02b_board_l1", boot);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.TickExact(boot, 200);
            Assert.That(boot.TapCuia(2).Pour.Pour.Amount, Is.EqualTo(1));
            CuiaView src = boot.View!.Views[0];
            CuiaView dst = boot.View.Views[2];
            for (int ms = 0; ms <= 1000; ms += 100)
            {
                if (ms > 0)
                {
                    PlayModeHelpers.TickExact(boot, 100);
                }

                Save(dir, "10_sheet_l1_" + ms.ToString("0000", System.Globalization.CultureInfo.InvariantCulture));
                float rim = dst.FootPosition.y + (dst.HeightU * dst.Unit);
                Debug.Log("AF_SHEET " + ms + " ms: inclinación " + Mathf.Abs(src.BodyAngle).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                    + "°, punto más bajo de la boca sobre el borde del destino " + ((src.MouthLowestInContainer().y - rim) / dst.Unit).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    + " u, centro de la boca a " + ((src.MouthCenterInContainer().x - dst.FootPosition.x) / dst.Unit).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " u del eje");
            }

            PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(File.Exists(Path.Combine(dir, "10_sheet_l1_1000.png")), Is.True);
        }

        /// <summary>
        /// Pantalla de inicio y sus superposiciones (tarea B1-6; 06 §4.7; D-008 punto 6), para compararlas con el Home de Magic
        /// Sort (ficha §7.1; <c>Logs/device/magicsort/analisis/det_ms/d_000070.jpg</c> y <c>d_011780.jpg</c>):
        /// <c>10_lobby</c> (nivel 4 guardado, 1.240 semillas, sin canoas antes del 31), <c>10b_lobby_l31</c> (con
        /// <c>af_level_offset</c> = 30: canoas 3/5 con cronómetro), <c>10c_content_end</c> («El río sigue creciendo» tras el 10),
        /// <c>10d_no_canoes</c> (Sin canoas en el 31, con el cronómetro) y <c>10e_victory_home</c> (victoria con «Inicio»).
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureLobby()
        {
            string dir = Prepare();
            var t0 = new System.DateTime(2026, 10, 8, 15, 0, 0, System.DateTimeKind.Utc);
            var clock = new ManualClock(t0, System.TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = clock;

            Profile p4 = Wallet.NewProfile(clock);
            p4.Progress.NextLevel = 4;
            p4.Progress.MaxLevel = 3;
            p4.Wallet.Seeds = 1240;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p4));
            GameBootstrap boot = GameBootstrap.Current!;
            AttachLobby(boot);
            yield return Wait(0.3f);
            Save(dir, "10_lobby");

            LaunchOptions.LevelOffsetOverride = 30;
            Profile p31 = Wallet.NewProfile(clock);
            p31.Progress.NextLevel = 31;
            p31.Progress.MaxLevel = 30;
            p31.Wallet.Seeds = 2380;
            p31.Wallet.Canoes.Count = 3;
            p31.Wallet.Canoes.NextRegenAt = t0 + System.TimeSpan.FromMinutes(18) + System.TimeSpan.FromSeconds(42);
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p31));
            boot = GameBootstrap.Current!;
            AttachLobby(boot);
            yield return Wait(0.3f);
            Save(dir, "10b_lobby_l31");

            p31.Wallet.Canoes.Count = 0;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p31));
            boot = GameBootstrap.Current!;
            AttachLobby(boot);
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.NoCanoes));
            yield return Wait(0.3f);
            Save(dir, "10d_no_canoes");

            LaunchOptions.LevelOffsetOverride = null;
            Profile p11 = Wallet.NewProfile(clock);
            p11.Progress.NextLevel = 11;
            p11.Progress.MaxLevel = 10;
            p11.Wallet.Seeds = 300;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(p11));
            boot = GameBootstrap.Current!;
            AttachLobby(boot);
            Assert.That(boot.PressPlay(), Is.True);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.ContentEnd));
            yield return Wait(0.3f);
            Save(dir, "10c_content_end");

            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            SolveResult s1 = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            boot.TapAnywhere();
            for (int i = 0; i < s1.MoveCount; i++)
            {
                boot.TapCuia(s1.GetMove(i).Source);
                boot.TapCuia(s1.GetMove(i).Target);
            }

            LevelFlow f1 = boot.Flow;
            yield return PlayModeHelpers.WaitFor(() => f1.Phase == FlowPhase.VictoryScreen, "victoria");
            yield return Wait(0.9f);
            Save(dir, "10e_victory_home");
            Assert.That(File.Exists(Path.Combine(dir, "10e_victory_home.png")), Is.True);
        }

        /// <summary>
        /// Bandeja y embudo de bloqueo de D-008 (tareas B2-1 a B2-3), para compararlos con Magic Sort (ficha §4.4-§4.8;
        /// <c>Logs/device/magicsort/analisis/det_ms/d_004040.jpg</c> «No Available Moves», <c>d_008030.jpg</c> «Continue?»,
        /// <c>d_000065.jpg</c> «Level Failed!» y <c>d_002890.jpg</c> «More Lives»): <c>11_tray_l8</c> (Marea y Cuia extra con
        /// el regalo de Capi), <c>11b_tray_l8_added</c> (la cuia añadida en la fila más corta), <c>12_stuck_l31</c> (rama A,
        /// 300 con el 600 tachado), <c>12b_stuck_l6</c> (rama C), <c>12c_stuck_l31_noseeds</c> (rama B con Puñado y Balsa),
        /// <c>13_giveup_confirm</c>, <c>14_level_failed</c>, <c>15_no_canoes</c>, <c>16_quit_confirm</c> y <c>16b_pause</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureFunnel()
        {
            string dir = Prepare();
            FunnelSetup.Hooks();

            // 11: nivel 8 con la Cuia extra regalada y el aviso de Capi; 11b, tras usarla.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            GameBootstrap boot = GameBootstrap.Current!;
            Attach(boot);
            boot.TapAnywhere();
            yield return Wait(0.4f);
            Save(dir, "11_tray_l8");
            boot.PressExtraCuia();
            yield return Wait(0.9f);
            Save(dir, "11b_tray_l8_added");
            LogBoard("11b_tray_l8_added", boot);

            // 12: nivel 31 (tablero 6 con af_level_offset = 25) con 1.240 semillas: rama A.
            yield return LoadFunnel(31, 1240, 3);
            boot = GameBootstrap.Current!;
            Attach(boot);
            yield return StuckRealTime(boot);
            Save(dir, "12_stuck_l31");

            // 13: ¿Seguro? (paso 2).
            boot.PressGiveUp();
            yield return Wait(0.3f);
            Save(dir, "13_giveup_confirm");

            // 14: Nivel fallido (paso 3) y 15: Sin canoas (paso 4, tras gastar las canoas que quedan).
            boot.ConfirmGiveUp();
            yield return Wait(0.3f);
            Save(dir, "14_level_failed");
            while (boot.Wallet!.Canoes.Count > 0)
            {
                boot.Wallet.Apply(AF.Economy.Transaction.HardFail(31));
            }

            boot.PressRetry();
            yield return Wait(0.3f);
            Save(dir, "15_no_canoes");

            // 12b: nivel 6, rama C.
            LaunchOptions.LevelOffsetOverride = null;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(310));
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore(6, 5));
            boot = GameBootstrap.Current!;
            Attach(boot);
            yield return StuckRealTime(boot);
            Save(dir, "12b_stuck_l6");

            // 12c: nivel 31 sin saldo: rama B (Puñado y Balsa en el orden T3).
            yield return LoadFunnel(31, 140, 3);
            boot = GameBootstrap.Current!;
            Attach(boot);
            yield return StuckRealTime(boot);
            Save(dir, "12c_stuck_l31_noseeds");

            // 16b: pausa, y 16: «¿Salir del nivel? Perderás 1 canoa».
            yield return LoadFunnel(31, 1240, 4);
            boot = GameBootstrap.Current!;
            Attach(boot);
            boot.TapAnywhere();
            yield return Wait(0.3f);
            boot.View!.PauseButton.onClick.Invoke();
            yield return Wait(0.2f);
            Save(dir, "16b_pause");
            boot.View.Pause.QuitButton.onClick.Invoke();
            yield return Wait(0.2f);
            Save(dir, "16_quit_confirm");
            Assert.That(File.Exists(Path.Combine(dir, "16_quit_confirm.png")), Is.True);
        }

        private static IEnumerator LoadFunnel(int level, int seeds, int canoes)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds, canoes));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
        }

        // Tablero 6: 3 → 4 deja Aguas estancadas; espera al modal y a que el panel termine de subir.
        private static IEnumerator StuckRealTime(GameBootstrap boot)
        {
            boot.TapAnywhere();
            boot.TapCuia(3);
            boot.TapCuia(4);
            LevelFlow flow = boot.Flow!;
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.StuckModal, "modal");
            yield return Wait(0.45f);
        }

        // Cámara y textura de las capturas (1080 × 2340 px, 20:9 aprox., 360 × 780 dp) con zona segura; devuelve la carpeta.
        private string Prepare()
        {
            GameScreen.SimulatedScreen = new Vector4(Width, Height, 96f, 48f); // 32 dp de cutout arriba, 16 dp de gestos abajo
            AmbientLayer.Frozen = true;
            _target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var camGo = new GameObject("SnapshotCamera");
            Object.DontDestroyOnLoad(camGo);
            _camera = camGo.AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.orthographic = true;
            _camera.enabled = false;
            string dir = Path.Combine(Application.dataPath, "../Logs/snapshots");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private void AttachLobby(GameBootstrap boot)
        {
            Canvas canvas = boot.Lobby!.Canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 10f;
        }

        // Medidas del tablero para la comparación con Magic Sort (ficha §2.1 y §2.2): cuias, filas, ancho y alto de la cuia y
        // paso horizontal, en dp (× 3 = px de la captura).
        private static void LogBoard(string name, GameBootstrap boot)
        {
            CuiaView[] views = boot.View!.Views;
            var line = new System.Text.StringBuilder("AF_BOARD " + name + ": " + views.Length + " cuias");
            float u = views[0].Unit;
            line.Append(", ancho ").Append((2f * u).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                .Append(" dp, alto ").Append((views[0].HeightU * u).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" dp");
            float pitch = 0f;
            int rows = 1;
            for (int i = 1; i < views.Length; i++)
            {
                if (Mathf.Abs(views[i].FootPosition.y - views[i - 1].FootPosition.y) < 0.5f)
                {
                    if (pitch <= 0f)
                    {
                        pitch = views[i].FootPosition.x - views[i - 1].FootPosition.x;
                    }
                }
                else
                {
                    rows++;
                }
            }

            line.Append(", paso ").Append(pitch.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" dp, filas ").Append(rows);
            for (int i = 0; i < views.Length; i++)
            {
                line.Append(i == 0 ? " | pies (dp): " : " ").Append('(').Append(views[i].FootPosition.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(';').Append(views[i].FootPosition.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(')');
            }

            Debug.Log(line.ToString());
        }

        private void Attach(GameBootstrap boot)
        {
            Canvas canvas = boot.View!.Canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 10f;
        }

        private void Save(string dir, string name)
        {
            Canvas.ForceUpdateCanvases();
            _camera!.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        private static bool StreamReachedSurface()
        {
            StreamGraphic[] streams = Object.FindObjectsByType<StreamGraphic>(FindObjectsSortMode.None);
            for (int i = 0; i < streams.Length; i++)
            {
                if (streams[i].enabled && streams[i].Range.y >= 0.999f)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerator Wait(float seconds)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < seconds)
            {
                yield return null;
            }
        }
    }
}
