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
    /// cada fotograma de la hoja escriben en el log una línea <c>AF_BOARD</c> o <c>AF_SHEET</c> con sus medidas. «Llenar y
    /// teñir» (guion de la intro, A14) va en <see cref="CaptureIntro"/>, con el instante fijado por el enganche de depuración.
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

            // Nivel 7: tablero, selección (solo el origen sube, sin aros en los destinos: D-002) y vertido. La intro, en
            // CaptureIntro.
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Attach(boot);
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
        /// «Llenar y teñir» (guion de la intro, A14), deterministas con el reloj del test, el enganche de depuración
        /// (<c>GameScreen.DebugReplayIntro</c> e <see cref="IntroAnimation.Seek"/>) y el reloj de los shaders congelado en el
        /// tiempo de la intro (<see cref="ShaderClock"/>; se comprueba capturando dos veces el mismo instante):
        /// <c>01_intro_l7_*</c> a 0, 50, 150, 300, 700, 1.100 y 1.400 ms (larga, rama con el mono ardilla; 0-150: el río crece
        /// antes que nada, A3); <c>01c_intro_l1_*</c> a 0, 50, 150, 300, 900 y 1.300; <c>01d_intro_short_l7_*</c> a 200,
        /// 500 y 700 (corta, tropa posada); <c>01e_intro_16_0500</c> (16 cuias en 3 filas: dosel, una fruta por cuia). Hojas
        /// cada 100 ms (<c>21_sheet_&lt;tablero&gt;_&lt;versión&gt;_&lt;ms&gt;</c>) de la larga y la corta con 3, 7 y 13 cuias, y del
        /// reintento (dosel) del nivel 7. Galería de especies (<c>22_species_&lt;id&gt;</c>): el tablero del nivel 7 en un nivel de
        /// cada tramo del catálogo, corta a 300 ms; y <c>23_sighting_l5</c>, la tarjeta «¡Miquito a la vista!». Cada hoja escribe
        /// una línea <c>AF_INTRO</c> con el plan.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureIntro()
        {
            string dir = Prepare();

            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            Replay(boot, 1400, false, "01_intro_l7");
            foreach (int ms in new[] { 0, 50, 150, 300, 700, 1100, 1400 })
            {
                boot.View!.Intro.Seek(ms);
                Save(dir, "01_intro_l7_" + Ms(ms));
            }

            // A14: el mismo instante, capturado dos veces con fotogramas reales por medio, sale idéntico byte a byte (el reloj
            // de los shaders del líquido y del chorro es el de la intro, no el del motor).
            foreach (int ms in new[] { 300, 1100 })
            {
                boot.View!.Intro.Seek(ms);
                byte[] first = Capture();
                for (int i = 0; i < 5; i++)
                {
                    yield return null;
                }

                boot.View.Intro.Seek(ms);
                byte[] second = Capture();
                Assert.That(second, Is.EqualTo(first), "captura determinista a " + ms + " ms");
            }

            Sheet(boot, dir, "21_sheet_l7_long", 1400, false);
            Sheet(boot, dir, "21_sheet_l7_short", 800, false);
            Replay(boot, 800, false, "01d_intro_short_l7");
            foreach (int ms in new[] { 200, 500, 700 })
            {
                boot.View!.Intro.Seek(ms);
                Save(dir, "01d_intro_short_l7_" + Ms(ms));
            }

            Sheet(boot, dir, "21_sheet_l7_retry", 800, true);

            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            Replay(boot, 1400, false, "01c_intro_l1");
            foreach (int ms in new[] { 0, 50, 150, 300, 900, 1300 })
            {
                boot.View!.Intro.Seek(ms);
                Save(dir, "01c_intro_l1_" + Ms(ms));
            }

            Sheet(boot, dir, "21_sheet_l1_long", 1400, false);
            Sheet(boot, dir, "21_sheet_l1_short", 800, false);

            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.ThirteenCuiaPack(), 1, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            Sheet(boot, dir, "21_sheet_13_long", 1400, false);
            Sheet(boot, dir, "21_sheet_13_short", 800, false);

            LaunchOptions.Reset();
            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.SixteenCuiaPack(), 1, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            Replay(boot, 1400, false, "01e_intro_16");
            boot.View!.Intro.Seek(500);
            Save(dir, "01e_intro_16_0500");
            boot.View.Intro.Seek(1250);
            Save(dir, "01e_intro_16_1250");

            // Galería de especies: el tablero del nivel 7 en un nivel de cada tramo (guion de la intro §4.2), corta a 300 ms.
            var stretches = new (int World, string WorldId, string Water, int Level)[]
            {
                (2, "w02_tapajos", "clear", 13), (3, "w03_manaos", "black", 42), (4, "w04_trapecio", "white", 82),
                (5, "w05_loreto", "white", 142), (6, "w06_oriente", "white", 202), (7, "w07_madre_de_dios", "white", 302),
                (8, "w08_madidi", "white", 422), (10, "w10_alto_negro", "black", 702), (11, "w11_escudo_guayana", "white", 822),
                (12, "w12_orinoco", "clear", 922),
            };
            foreach ((int world, string worldId, string water, int level) in stretches)
            {
                LaunchOptions.Reset();
                yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.Level7BoardPack(world, worldId, water, level), level, new MemoryProgressStore());
                boot = GameBootstrap.Current!;
                Attach(boot);
                PlayModeHelpers.TakeClock(boot);
                Replay(boot, 800, false, "22_species_l" + level);
                boot.View!.Intro.Seek(300);
                string id = boot.View.IntroSpecies != null ? boot.View.IntroSpecies.Id : "dosel";
                Save(dir, "22_species_" + id);
                if (level == 13 || level == 142 || level == 82)
                {
                    Sheet(boot, dir, "21_sheet_l" + level + "_long", 1400, false);
                }
            }

            // «¡Miquito a la vista!» (guion §4.5): nivel 5, tras la intro, sobre la franja del panorama.
            LaunchOptions.Reset();
            yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.TickExact(boot, 400);
            Save(dir, "23_sighting_l5");

            Assert.That(File.Exists(Path.Combine(dir, "01e_intro_16_0500.png")), Is.True);
        }

        // Relanza la intro del nivel cargado desde t = 0 y deja el plan en el log.
        private static void Replay(GameBootstrap boot, int durationMs, bool retry, string name)
        {
            boot.View!.DebugReplayIntro(durationMs, retry);
            IntroChoreography p = boot.View.Intro.Plan;
            var line = new System.Text.StringBuilder("AF_INTRO " + name + ": T " + durationMs + " ms");
            line.Append(", especie ").Append(boot.View.IntroSpecies != null ? boot.View.IntroSpecies.Id : "dosel");
            line.Append(", posadero ").Append(p.Perch).Append(", modelo ").Append(p.Model).Append(", miquitos ").Append(p.MonkeyCount);
            line.Append(" de ").Append(p.MonkeyDp.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" dp");
            int loose = 0;
            for (int k = 0; k < p.FruitCount; k++)
            {
                loose += p.FruitMonkey[k] < 0 ? 1 : 0;
            }

            line.Append(", frutas ").Append(p.FruitCount).Append(p.FruitPerCuia ? " (una por cuia)" : " (una por racha)");
            line.Append(", sin miquito ").Append(loose);
            line.Append(", fila 1 llena ").Append(p.Row1FullMs.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
            line.Append(", llenado hasta ").Append(p.LastFillEndMs.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
            line.Append(", primer impacto ").Append(p.FirstImpactMs.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
            line.Append(", última floración ").Append(p.LastBloomEndMs.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            Debug.Log(line.ToString());
        }

        // Hoja de la intro: una captura cada 100 ms de 0 a T + 300 (la salida de la tropa).
        private void Sheet(GameBootstrap boot, string dir, string name, int durationMs, bool retry)
        {
            Replay(boot, durationMs, retry, name);
            IntroAnimation intro = boot.View!.Intro;
            for (int ms = 0; ms <= durationMs + 300; ms += 100)
            {
                intro.Seek(ms);
                Save(dir, name + "_" + Ms(ms));
            }
        }

        private static string Ms(int ms)
        {
            return ms.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
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
        /// Revisión de la pista B: <c>16a_hud_l31</c> (la cabecera de partida con las píldoras de Semillas y Canoas, 07 §7.4),
        /// <c>14b_retry_l31</c> (la intro corta de «Reintentar» con «Nivel 31 · intento 2», 02 §2; ficha §4.1, t = 340-342) y
        /// <c>20_profile_locked</c> (el aviso «Actualiza Tintalú» con el perfil en solo lectura, 08 §4.4).
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

            // 16a: la cabecera de partida en el 31 (Semillas 1.240 y Canoas 4/5, 07 §7.4); 16b: pausa, y 16: «¿Salir del nivel?
            // Perderás 1 canoa».
            yield return LoadFunnel(31, 1240, 4);
            boot = GameBootstrap.Current!;
            Attach(boot);
            boot.TapAnywhere();
            yield return Wait(0.3f);
            Save(dir, "16a_hud_l31");
            boot.View!.PauseButton.onClick.Invoke();
            yield return Wait(0.2f);
            Save(dir, "16b_pause");
            boot.View.Pause.QuitButton.onClick.Invoke();
            yield return Wait(0.2f);
            Save(dir, "16_quit_confirm");

            // 14b: salir confirmado → Nivel fallido → «Reintentar»: la intro corta con «Nivel 31 · intento 2» (02 §2).
            boot.View.Confirm.ConfirmButton.onClick.Invoke();
            yield return Wait(0.2f);
            boot.PressRetry();
            yield return Wait(0.3f);
            Save(dir, "14b_retry_l31");

            // 20: perfil de una versión más nueva: aviso «Actualiza Tintalú» sobre el tablero (08 §4.4).
            LaunchOptions.LevelOffsetOverride = null;
            LaunchOptions.ProfileStoreOverride = new ReadOnlyProfileStore(ProfileLoadStatus.TooNew);
            yield return PlayModeHelpers.LoadGame(7, new MemoryProgressStore(7, 6));
            boot = GameBootstrap.Current!;
            Attach(boot);
            AttachOverlay(boot.LockedNotice!.Canvas);
            boot.TapAnywhere();
            yield return Wait(0.3f);
            Save(dir, "20_profile_locked");
            Assert.That(File.Exists(Path.Combine(dir, "20_profile_locked.png")), Is.True);
        }

        /// <summary>
        /// Victoria con la cadena corta y anuncios y compras simulados de D-008 (tarea B2-4), para compararlos con Magic Sort
        /// (ficha §4.6 y §4.8, victoria con «Rewards:» y «Continue», <c>Logs/device/magicsort/analisis/ov/</c>; §6.2, anuncios
        /// a pantalla completa con «Close»; §5.4, tienda): <c>17_victory_l11</c> (Cosecha +20 con la Cosecha doble «+20 → +40»),
        /// <c>17b_ad_rewarded</c> (el video de la Cosecha doble con su cuenta atrás), <c>17c_victory_l11_doubled</c> (+40),
        /// <c>18_ad_interstitial</c> (el intersticial tras «Siguiente» en el 24, 3.ª victoria en 22-60, desde la 2.ª sesión) y
        /// <c>19_store_sheet</c> («Compra simulada» de Canoas infinitas 1 h desde Sin canoas, con el precio en COP).
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureVictoryAndAds()
        {
            string dir = Prepare();
            FunnelSetup.Hooks();
            LaunchOptions.AdPresenterOverride = null; // pantallas de verdad: AdOverlay y StoreSheet
            LaunchOptions.PurchasePresenterOverride = null;

            // 17: victoria del nivel 11 (tablero 1 con af_level_offset = 10) con 1.240 semillas.
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            Attach(boot);
            yield return WinRealTime(boot);
            yield return Wait(1.2f); // panel arriba (250 ms) y la Cosecha ya contada (250 + 400 ms)
            Save(dir, "17_victory_l11");

            // 17b: el video de la Cosecha doble (recompensado de 3 s [P]) y 17c: la Cosecha ya doblada.
            AttachOverlay(boot.AdOverlayView!.Canvas);
            boot.View!.DoubleHarvestButton.onClick.Invoke();
            yield return Wait(1.0f);
            Save(dir, "17b_ad_rewarded");
            yield return Wait(2.2f);
            boot.AdOverlayView.CloseButton.onClick.Invoke();
            yield return Wait(0.8f);
            Save(dir, "17c_victory_l11_doubled");

            // 18: tercera victoria en 22-60 desde la 2.ª sesión: el intersticial tras «Siguiente» (tablero 3 como nivel 24).
            LaunchOptions.LevelOffsetOverride = 21;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240, levelsSinceInterstitial: 2));
            yield return PlayModeHelpers.LoadGame(24, new MemoryProgressStore(24, 23));
            boot = GameBootstrap.Current!;
            Attach(boot);
            AttachOverlay(boot.AdOverlayView!.Canvas);
            FunnelSetup.Clock.Advance(System.TimeSpan.FromSeconds(200));
            yield return WinRealTime(boot);
            yield return Wait(1.0f);
            boot.View!.NextButton.onClick.Invoke();
            yield return Wait(1.0f);
            Save(dir, "18_ad_interstitial");
            Assert.That(boot.AdOverlayView.Visible, Is.True, "intersticial simulado a pantalla completa");

            // 19: Sin canoas en el 31 (tablero 6) con 1 canoa: rendirse, Reintentar y «Canoas infinitas 1 h» → Compra simulada.
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(140, canoes: 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            boot = GameBootstrap.Current!;
            Attach(boot);
            AttachOverlay(boot.StoreSheetView!.Canvas);
            yield return StuckRealTime(boot);
            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            yield return Wait(0.3f);
            boot.View!.NoCanoes.InfiniteButton.onClick.Invoke();
            yield return Wait(0.3f);
            Save(dir, "19_store_sheet");
            Assert.That(boot.StoreSheetView.Visible, Is.True);
            Assert.That(File.Exists(Path.Combine(dir, "19_store_sheet.png")), Is.True);
        }

        // Victoria con la solución del solver, en tiempo real.
        private static IEnumerator WinRealTime(GameBootstrap boot)
        {
            boot.TapAnywhere();
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            for (int i = 0; i < solution.MoveCount; i++)
            {
                boot.TapCuia(solution.GetMove(i).Source);
                boot.TapCuia(solution.GetMove(i).Target);
            }

            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.VictoryScreen, "victoria");
        }

        private void AttachOverlay(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 5f;
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
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), Capture());
        }

        private byte[] Capture()
        {
            Canvas.ForceUpdateCanvases();
            _camera!.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            byte[] png = tex.EncodeToPNG();
            Object.Destroy(tex);
            return png;
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
