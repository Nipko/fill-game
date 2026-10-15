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
    /// Capturas de revisión de la tropa lateral (D-021; tarea TL1; <c>[Explicit]</c>, no forman parte de la suite): el primer
    /// nivel de 3 filas de los mundos 4 (el 86, churuco, <c>cuelga</c>: baja colgado de su liana) y 5 (el 143, huapo
    /// colorado, <c>sacude</c>: baja trepando), con la intro que ve el jugador (la corta, 800 ms), a media bajada y en el
    /// primer lanzamiento (la primera fruta en vuelo, 30 ms después de dejar libre la cabeza), a 1080 × 2340 px (360 × 780 dp)
    /// y a 720 × 1600 px (360 × 800 dp):
    /// <c>tl1_&lt;nivel&gt;_bajada_&lt;ancho&gt;</c> y <c>tl1_&lt;nivel&gt;_lanza_&lt;ancho&gt;</c>. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.IntroSideTroopSnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class IntroSideTroopSnapshotTests
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
        public IEnumerator CaptureSideTroop([Values(86, 143)] int level, [Values(1080, 720)] int widthPx)
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
            Debug.Log("AF_SIDE tl1_" + level + "_" + widthPx + ": " + IntroSideTroopTests.Describe(p));
            Assert.That(p.Perch, Is.EqualTo(IntroPerch.Side));

            // A media bajada: el de la izquierda a mitad de su entrada (el de la derecha, cerca).
            float descent = 0.5f * (p.SideStartMs[0] + p.SideArriveMs[0]);
            view.Intro.Seek(descent);
            Assert.That(view.Troop.SideActive, Is.True);
            Save(dir, "tl1_" + level + "_bajada_" + widthPx);

            // En el lanzamiento: la primera fruta en vuelo, 30 ms después de que deja libre la cabeza de su miquito (hasta
            // entonces no se dibuja: SideClearMs), con la siguiente ya en la mano.
            float first = float.MaxValue;
            int kFirst = 0;
            for (int k = 0; k < p.FruitCount; k++)
            {
                if (p.FruitReleaseMs[k] < first)
                {
                    first = p.FruitReleaseMs[k];
                    kFirst = k;
                }
            }

            view.Intro.Seek(first + p.SideClearMs[kFirst] + 30f);
            Save(dir, "tl1_" + level + "_lanza_" + widthPx);
            yield return null;
        }

        private string Prepare(int widthPx)
        {
            _width = widthPx;
            _height = widthPx == 1080 ? 2340 : 1600;
            GameScreen.SimulatedScreen = widthPx == 1080 ? IntroSideTroopTests.Screen1080 : IntroSideTroopTests.Screen720;
            AmbientLayer.Frozen = true;
            _target = new RenderTexture(_width, _height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var camGo = new GameObject("SideTroopSnapshotCamera");
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
