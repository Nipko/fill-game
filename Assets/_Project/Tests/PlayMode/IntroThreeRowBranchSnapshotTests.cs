#nullable enable
using System.Collections;
using System.IO;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de la tropa en su rama con 3 filas (D-046, punto 3; tarea T6 · tropa; <c>[Explicit]</c>, no forman
    /// parte de la suite): el primer nivel de 3 filas de los mundos 4 (el 86, churuco) y 5 (el 143, huapo colorado), con la
    /// intro corta (800 ms) que ve el jugador, con la tropa sentada en la rama sobre la franja de 44 dp, y ya en juego, a
    /// 1080 × 2340 px (360 × 780 dp) y 720 × 1600 px (360 × 800 dp): <c>t6t_&lt;nivel&gt;_rama_&lt;ancho&gt;</c> y
    /// <c>t6t_&lt;nivel&gt;_juego_&lt;ancho&gt;</c>. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.IntroThreeRowBranchSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en
    /// <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class IntroThreeRowBranchSnapshotTests
    {
        private Camera? _camera;
        private RenderTexture? _target;
        private int _width;
        private int _height;

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

            _camera = null;
            _target = null;
        }

        [UnityTest]
        public IEnumerator CaptureThreeRowBranch([Values(86, 143)] int level, [Values(1080, 720)] int widthPx)
        {
            string dir = Prepare(widthPx);
            yield return IntroSideTroopTests.LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            Canvas canvas = boot.View!.Canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 10f;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View;

            view.DebugReplayIntro(800, false);
            IntroChoreography p = view.Intro.Plan;
            Assert.That(p.Perch, Is.EqualTo(IntroPerch.Branch));

            // Con la tropa en la rama: la primera suelta.
            float first = float.MaxValue;
            for (int k = 0; k < p.FruitCount; k++)
            {
                first = Mathf.Min(first, p.FruitReleaseMs[k]);
            }

            view.Intro.Seek(first + 30f);
            Save(dir, "t6t_" + level + "_rama_" + widthPx);

            // Ya en juego, con la tropa en su reposo (D-011, punto 3).
            view.Intro.Seek(p.TotalMs + 300f);
            Save(dir, "t6t_" + level + "_juego_" + widthPx);
            yield return null;
        }

        private string Prepare(int widthPx)
        {
            _width = widthPx;
            _height = widthPx == 1080 ? 2340 : 1600;
            GameScreen.SimulatedScreen = widthPx == 1080 ? IntroSideTroopTests.Screen1080 : IntroSideTroopTests.Screen720;
            AmbientLayer.Frozen = true;
            _target = new RenderTexture(_width, _height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var camGo = new GameObject("ThreeRowBranchSnapshotCamera");
            Object.DontDestroyOnLoad(camGo);
            _camera = camGo.AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.orthographic = true;
            _camera.enabled = false;
            string dir = Path.Combine(Application.dataPath, "../Logs/snapshots/es-419");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private void Save(string dir, string name)
        {
            Canvas.ForceUpdateCanvases();
            _camera!.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            var tex = new Texture2D(_width, _height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
