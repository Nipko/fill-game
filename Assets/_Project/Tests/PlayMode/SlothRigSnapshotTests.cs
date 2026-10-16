#nullable enable
using System.Collections;
using System.IO;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión del rig provisional del perezoso protagonista (tarea TI4; propuesta del personaje c.4-c.7 y paso 4
    /// de (i); <c>[Explicit]</c>, no forman parte de la suite), a 360 × 780 dp (1080 × 2340 px) y a 360 × 640 dp (1080 × 1920 px):
    /// <list type="bullet">
    /// <item><c>ti4_poses_&lt;alto&gt;</c>: las 10 poses en una rejilla de 2 × 5, en el orden de <see cref="SlothPoseId"/> (por
    /// filas: idle y wake; wave y present; cheer y cheer_big; encourage y look; sunbathe y sleep), en un instante que las
    /// caracteriza, la columna izquierda sobre la selva clara y la derecha sobre el panel azul petróleo (D-004; c.5, punto 9);</item>
    /// <item><c>ti4_sizes_&lt;alto&gt;</c>: <c>sloth_idle</c> a los tamaños de c.4 (180, 140, 120, 96, 72, 64 y 48 dp), en filas alternas sobre la
    /// selva clara y sobre el panel azul petróleo; a 780 dp, una quinta fila con 72, 64 y 48 dp sobre la selva clara (el peor
    /// contraste).</item>
    /// </list>
    /// Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.SlothRigSnapshotTests</c> sin
    /// <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/es-419/</c>. Sin textos: el nombre de la pose va en el del archivo.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class SlothRigSnapshotTests
    {
        private const float PxPerDp = 3f;
        private const float WidthDp = 360f;

        // Instante de cada pose en la captura (ms), en el que se ve su rasgo propio: idle en reposo; wake aún dormido (ojos
        // cerrados y la cabeza caída 14°); wave con la garra suelta; present señalando; cheer con la garra alta y sus 4 hojas;
        // cheer_big a media vuelta (BodySpin = −1: de espaldas, con el espéculo) y sus 6 hojas; encourage en lo más hondo del
        // balanceo y la cabezada; look girando la cabeza; y los dos bucles en su inicio.
        private static readonly float[] PoseMs = { 0f, 300f, 400f, 600f, 700f, 900f, 600f, 500f, 0f, 0f };

        // Los tamaños de c.4; los pequeños (72, 64 y 48 dp: Mapa del río, victoria y Nivel fallido) dos veces, sobre el panel y
        // sobre la selva clara, el peor contraste del pelaje.
        private static readonly float[] SizesDp = { 180f, 140f, 120f, 96f, 72f, 64f, 48f, 72f, 64f, 48f };

        // Tamaños por fila (cada uno con el ancho de su rama, 1,5 S, en 360 dp): 180; 140; 120 y 96; 72, 64 y 48 (panel); 72,
        // 64 y 48 (selva clara). A 640 dp no cabe la quinta fila: solo las cuatro primeras.
        private static readonly int[] SizesPerRow = { 1, 1, 2, 3, 3 };

        private Camera? _camera;
        private RenderTexture? _target;
        private GameObject? _canvasGo;
        private ProceduralArt? _art;
        private int _heightPx;

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = false;
            _art?.Dispose();
            _art = null;
            if (_canvasGo != null)
            {
                Object.Destroy(_canvasGo);
            }

            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
            }

            if (_target != null)
            {
                _target.Release();
                Object.Destroy(_target);
            }

            _canvasGo = null;
            _camera = null;
            _target = null;
        }

        [UnityTest]
        public IEnumerator CapturePoses([Values(780, 640)] int heightDp)
        {
            RectTransform layer = Prepare(heightDp, out string dir);
            float cellW = WidthDp / 2f;
            float cellH = heightDp / 5f;
            Background(layer, 0f, 0f, cellW, heightDp, Palette.JungleLight);
            Background(layer, cellW, 0f, cellW, heightDp, Palette.Panel.Top);
            float size = Mathf.Floor(cellH / 1.62f);
            SlothArt art = _art!.SlothRigArt()!;
            for (int i = 0; i < 10; i++)
            {
                var pose = (SlothPoseId)i;
                SlothRig rig = SlothRig.Create(layer, art, size)!;
                float x = (cellW * (i % 2)) + (cellW * 0.5f);
                float y = (cellH * (i / 2)) + (size * 0.6f);
                rig.Place(x, y);
                rig.Play(pose, false);
                rig.Seek(PoseMs[i]);
                Debug.Log("AF_SLOTH ti4_poses_" + heightDp + ": " + SlothRig.IdOf(pose) + " a " + PoseMs[i] + " ms, S = " + size + " dp");
            }

            yield return null;
            Save(dir, "ti4_poses_" + heightDp);
        }

        [UnityTest]
        public IEnumerator CaptureSizes([Values(780, 640)] int heightDp)
        {
            RectTransform layer = Prepare(heightDp, out string dir);
            SlothArt art = _art!.SlothRigArt()!;
            float rowTop = 0f;
            int next = 0;
            for (int row = 0; row < SizesPerRow.Length; row++)
            {
                float rowH = (1.12f * SizesDp[next]) + 8f;
                if (rowTop + rowH > heightDp)
                {
                    break;
                }

                Background(layer, 0f, rowTop, WidthDp, rowH, row % 2 == 0 ? Palette.JungleLight : Palette.Panel.Top);
                float x = 0f;
                for (int k = 0; k < SizesPerRow[row]; k++, next++)
                {
                    float s = SizesDp[next];
                    float w = 1.5f * s; // el ancho de su rama
                    SlothRig rig = SlothRig.Create(layer, art, s)!;
                    rig.Place(x + (w * 0.5f), rowTop + 4f + (0.12f * s));
                    rig.Play(SlothPoseId.Idle, false);
                    x += w;
                }

                rowTop += rowH;
            }

            Background(layer, 0f, rowTop, WidthDp, heightDp - rowTop, Palette.Panel.Top);
            yield return null;
            Save(dir, "ti4_sizes_" + heightDp);
        }

        // Lienzo de 360 × alto dp a 3 px/dp en la cámara de las capturas; devuelve la capa (origen arriba a la izquierda).
        private RectTransform Prepare(int heightDp, out string dir)
        {
            MapArtRule.ForceProvisional = true;
            ArtSlots.ForceProcedural = true;
            _art = new ProceduralArt("white");
            _heightPx = Mathf.RoundToInt(heightDp * PxPerDp);
            int widthPx = Mathf.RoundToInt(WidthDp * PxPerDp);
            _target = new RenderTexture(widthPx, _heightPx, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var camGo = new GameObject("SlothSnapshotCamera");
            _camera = camGo.AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.orthographic = true;
            _camera.enabled = false;

            _canvasGo = new GameObject("SlothSnapshotCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = _canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = PxPerDp;
            var layerGo = new GameObject("Layer", typeof(RectTransform));
            layerGo.transform.SetParent(_canvasGo.transform, false);
            var layer = (RectTransform)layerGo.transform;
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.sizeDelta = Vector2.zero;

            dir = Path.Combine(Application.dataPath, "../Logs/snapshots/es-419");
            Directory.CreateDirectory(dir);
            return layer;
        }

        private static void Background(RectTransform layer, float x, float yDown, float w, float h, Color color)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(layer, false);
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = color;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yDown);
            rt.sizeDelta = new Vector2(w, h);
            rt.SetAsFirstSibling();
        }

        private void Save(string dir, string name)
        {
            Canvas.ForceUpdateCanvases();
            _camera!.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            var tex = new Texture2D(_target!.width, _heightPx, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _target.width, _heightPx), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
