#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
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
    /// destino), <c>02d_board_l2</c> (5 cuias de ≈ 47 dp) y <c>02e_board_l4</c> (6 cuias en dos filas de 3, 48 dp).
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

            yield return PlayModeHelpers.LoadGame(4, new MemoryProgressStore());
            boot = GameBootstrap.Current!;
            Attach(boot);
            boot.TapAnywhere();
            yield return Wait(0.3f);
            Save(dir, "02e_board_l4");
            Assert.That(File.Exists(Path.Combine(dir, "02e_board_l4.png")), Is.True);
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
