#nullable enable
using System.Collections;
using System.IO;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas del borde a borde y las pantallas grandes (añadido de D-059 del 2026-11-06; <c>[Explicit]</c>, no forman parte de la
    /// suite): el inicio, el Mapa, el Mercado, la Liga, Ajustes, el nivel 374 (3 filas) y su pausa en cada pantalla de
    /// <see cref="WideScreenLayoutTests.All"/>, a su tamaño en píxeles, con todos los lienzos en una cámara de ese tamaño (así el
    /// lienzo real mide lo simulado). Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.WideScreenSnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/borde-a-borde/</c>.
    /// </summary>
    [Explicit("Capturas del borde a borde; necesitan dispositivo gráfico.")]
    public sealed class WideScreenSnapshotTests
    {
        private Camera? _camera;
        private RenderTexture? _target;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            AmbientLayer.Frozen = true;
        }

        [TearDown]
        public void TearDown()
        {
            AmbientLayer.Frozen = false;
            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
            }

            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }

            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetMarketOverrides();
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedSideInsets = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
        }

        private static System.Collections.Generic.IEnumerable<WideScreenLayoutTests.Device> Devices() => WideScreenLayoutTests.All;

        [UnityTest]
        public IEnumerator Capture([ValueSource(nameof(Devices))] WideScreenLayoutTests.Device device)
        {
            string dir = Path.Combine(Application.dataPath, "../Logs/snapshots/borde-a-borde");
            Directory.CreateDirectory(dir);
            device.Use();
            Prepare((int)device.Screen.x, (int)device.Screen.y);

            yield return WideScreenLayoutTests.LoadLobby();
            GameBootstrap boot = GameBootstrap.Current!;
            yield return Shoot(dir, device + "_01_inicio");

            Assert.That(boot.OpenRiverMap(MapOpenSource.Lobby), Is.True);
            yield return Shoot(dir, device + "_02_mapa");
            boot.CloseRiverMap();

            Assert.That(boot.OpenMarket(MarketTab.Seeds), Is.True);
            yield return Shoot(dir, device + "_03_mercado");
            boot.Market!.Close();

            Assert.That(boot.OpenLeague(), Is.True);
            boot.TickLeague(16f);
            yield return Shoot(dir, device + "_04_liga");
            boot.CloseLeague();

            Assert.That(boot.PressSettings(), Is.True);
            yield return Shoot(dir, device + "_05_ajustes");
            boot.CloseSettings();

            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            device.Use();
            yield return Canvas16x9LayoutTests.LoadPlaying(374);
            boot = GameBootstrap.Current!;
            yield return Shoot(dir, device + "_06_nivel374");
            boot.View!.PauseButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
            yield return Shoot(dir, device + "_07_pausa");
        }

        private void Prepare(int width, int height)
        {
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var go = new GameObject("WideSnapshotCamera");
            Object.DontDestroyOnLoad(go);
            _camera = go.AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.magenta; // lo que ningún fondo cubra se vería magenta
            _camera.orthographic = true;
            _camera.enabled = false;
        }

        private IEnumerator Shoot(string dir, string name)
        {
            for (int i = 0; i < 4; i++)
            {
                yield return null;
            }

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = _camera;
                    canvas.planeDistance = 10f;
                }
            }

            yield return null;
            Canvas.ForceUpdateCanvases();
            _camera!.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            var tex = new Texture2D(_target!.width, _target.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
