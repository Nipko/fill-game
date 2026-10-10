#nullable enable
using System.Collections;
using System.IO;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capturas de revisión de las Aguas turbias en el shader (T15; <c>[Explicit]</c>, no forman parte de la suite):
    /// <c>70_murky_l100</c> (las 2 ocultas del estreno, pardas con trama y «?»), <c>71_murky_tilt_l106</c> (la cuia turbia
    /// inclinada al verter: la trama y el «?» en su banda), <c>72_reveal_l100</c> (el revelado por la regla al 20 %: el «?» se
    /// deforma entre destellos y el color nuevo empieza a salir), <c>72b_reveal_mid_l100</c> (al 45 %: la dosis ya en su
    /// tinte, la burbuja y lo último del «?» deshaciéndose encima, con el patrón de la fruta entrando),
    /// <c>72d_reveal_tNNN_l100_zoom</c> (recortes al 30, 35, 40, 48, 50, 54, 56, 60 y 70 %: el fundido cruzado y el pop de la
    /// cifra), <c>72c_firefly_l100</c> (la Luciérnaga a mitad: de arriba abajo y el icono de la fruta) y
    /// <c>73_murky_badge_l100</c> (tras el revelado, «?×1»). De cada una, además, un recorte a tamaño real de la cuia turbia
    /// (<c>*_zoom</c>) para mirar la legibilidad a 48-56 dp, los solapes, los colores y la línea D-003. Muestrean además los
    /// píxeles: en 70, el «?» del shader en el marfil UiPaper (ΔE00 ≤ 3); del 30 al 70 % del revelado, nunca el pardo sin «?»
    /// (07 §3); del 54 al 56 % (paso a la dosis normal), menos del 10 % de los píxeles de la banda cambian; las medidas salen
    /// en el log como <c>AF_MURKY</c>. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode
    /// PlayMode --filter AF.Tests.PlayMode.MurkySnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en
    /// <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class MurkySnapshotTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureMurkyL100()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            PlayModeHelpers.Step(boot, 300);
            Save(dir, "70_murky_l100", boot, murky);

            // El «?» del shader sale en el marfil de la insignia (UiPaper #F4EBDA; direccion-visual-v2 §2.4, regla 2; D-003).
            CuiaView turbid = boot.View!.Views[murky];
            int hiddenLayer = Lowest(boot.Flow!.Session.Current[murky].HiddenMask);
            MarkSample still = SampleMark(turbid, hiddenLayer);
            float paperDe = ColorMath.DeltaE00(still.Plateau, Palette.UiPaper);
            Debug.Log("AF_MURKY 70: meseta del «?» #" + ColorUtility.ToHtmlStringRGB(still.Plateau) + " (" + still.PlateauCount + " px), ΔE00 "
                + paperDe.ToString("F1") + " de UiPaper; contraste " + ColorMath.ContrastRatio(still.Plateau, Palette.Murky).ToString("F2") + ":1 frente al pardo");
            Assert.That(paperDe, Is.LessThanOrEqualTo(3f), "el «?» en marfil UiPaper, no #E7D4B3");

            // El revelado por la regla: al 20 % (72), al 45 % (72b) y, en recortes, del 30 al 70 % (72d): en ningún fotograma
            // queda el pardo sin «?» (07 §3; el «?» se disuelve sobre el color nuevo, ficha de Magic Sort §3.1). Fundido
            // cruzado (D-003): la trama se va hacia el 30 %, el patrón de la fruta entra del 35 al 55 % y la cifra hace su pop
            // hacia el 47-50 %; del 54 al 56 % (el paso a la dosis normal) casi ningún píxel de la banda cambia.
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.RevealedMask != 0);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            MechanicTestKit.Tap(boot, pour);
            Cuia revealed = boot.Flow!.Session.Current[pour.Source];
            int layer = revealed.Count - 1;
            Color tint = Palette.Tint(revealed.ColorAt(layer));
            CuiaView source = boot.View.Views[pour.Source];
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Reveal, pour.Source, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Reveal, pour.Source, 0.2f);
            Save(dir, "72_reveal_l100", boot, pour.Source);
            float[] fractions = { 0.30f, 0.35f, 0.40f, 0.45f, 0.48f, 0.50f, 0.54f, 0.56f, 0.60f, 0.70f };
            Color32[]? beforeSwitch = null;
            for (int i = 0; i < fractions.Length; i++)
            {
                float f = fractions[i];
                MechanicTestKit.StepToFraction(boot, MechanicStepKind.Reveal, pour.Source, f);
                string name = f == 0.45f ? "72b_reveal_mid_l100" : "72d_reveal_t" + Mathf.RoundToInt(100f * f).ToString("000") + "_l100";
                if (f == 0.45f)
                {
                    Save(dir, name, boot, pour.Source);
                }
                else
                {
                    SaveCrop(dir, name + "_zoom", source.Root, 28f);
                }

                MarkSample band = SampleMark(source, layer);
                float toMurky = ColorMath.DeltaE00(band.Mean, Palette.Murky);
                Debug.Log("AF_MURKY " + name + ": media de la banda #" + ColorUtility.ToHtmlStringRGB(band.Mean) + ", ΔE00 " + toMurky.ToString("F1")
                    + " del pardo y " + ColorMath.DeltaE00(band.Mean, tint).ToString("F1") + " de su tinte; «?» marfil " + band.PaperCount + " px de " + band.Total
                    + "; insignia «?×" + source.Murky.BadgeCount + "» a escala " + source.Murky.BadgeDigit.rectTransform.localScale.x.ToString("F2"));
                Assert.That(toMurky >= 8f || band.PaperCount * 50 >= band.Total, Is.True, name + ": nunca el pardo sin «?» (07 §3)");
                if (f == 0.54f)
                {
                    beforeSwitch = band.Pixels;
                }
                else if (f == 0.56f)
                {
                    int changed = Changed(beforeSwitch!, band.Pixels, 8);
                    Debug.Log("AF_MURKY 54 -> 56 %: " + changed + " de " + band.Total + " píxeles de la banda cambian más de 8/255");
                    Assert.That(changed * 10, Is.LessThan(band.Total), "el paso a la dosis normal no salta (menos del 10 % de los píxeles)");
                }
            }

            MechanicTestKit.StepToIdle(boot, 4000);
            PlayModeHelpers.Step(boot, 600); // los últimos destellos y la burbuja se apagan
            Save(dir, "73_murky_badge_l100", boot, pour.Source);
            Assert.That(boot.View!.Views[pour.Source].Murky.BadgeCount, Is.EqualTo(1), "«?×1»");

            // La Luciérnaga (la usa T14) a mitad, sobre la oculta que queda y la revelada de nuevo turbia en la vista.
            CuiaView view = boot.View.Views[pour.Source];
            int mask = boot.Flow!.Session.Current[pour.Source].HiddenMask;
            Assert.That(mask, Is.Not.EqualTo(0));
            view.PlayReveal(mask, RevealStyle.Firefly, 1f);
            for (int t = 0; t < 330; t += 10)
            {
                view.Tick(10f);
            }

            Save(dir, "72c_firefly_l100", boot, pour.Source);
            view.SnapMarks();
            Assert.That(File.Exists(Path.Combine(dir, "70_murky_l100.png")), Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureMurkyTiltL106()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(106);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => a[murky].Count < b[murky].Count);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            CuiaView source = boot.View!.Views[murky];
            MechanicTestKit.Tap(boot, path![path.Length - 1]);
            float best = 0f;
            for (int t = 0; t < 3000; t += 4)
            {
                PlayModeHelpers.TickExact(boot, 4);
                float tilt = Mathf.Abs(source.Liquid.TiltDeg);
                if (source.BodyInFront && tilt >= best)
                {
                    best = tilt;
                }
                else if (best > 30f)
                {
                    break; // la inclinación máxima, mientras vierte
                }
            }

            Assert.That(best, Is.GreaterThan(30f), "inclinada");
            Save(dir, "71_murky_tilt_l106", boot, murky, source.Body);
            Assert.That(File.Exists(Path.Combine(dir, "71_murky_tilt_l106.png")), Is.True);
        }

        private static IEnumerator Load(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            boot.View!.Animator.Steps.ClearLog();
        }

        // La captura entera y un recorte a tamaño real (3 px/dp) alrededor de la cuia (o de target), con la repisa.
        private static void Save(string dir, string name, GameBootstrap boot, int cuia, RectTransform? target = null)
        {
            SnapshotKit.Save(dir, name);
            CuiaView view = boot.View!.Views[cuia];
            SaveCrop(dir, name + "_zoom", target ?? view.Root, 28f);
        }

        private static void SaveCrop(string dir, string name, RectTransform target, float marginDp)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture rt = cam.targetTexture;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(corners[i]);
                xMin = Mathf.Min(xMin, p.x);
                yMin = Mathf.Min(yMin, p.y);
                xMax = Mathf.Max(xMax, p.x);
                yMax = Mathf.Max(yMax, p.y);
            }

            float margin = marginDp * 3f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(xMin - margin), 0, rt.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(yMin - margin), 0, rt.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(xMax + margin), x0 + 1, rt.width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(yMax + margin), y0 + 1, rt.height);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, x1 - x0, y1 - y0), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        // Píxeles de la captura en una caja de ±0,3 u alrededor del centro del «?» de la dosis k (el de la banda a media altura
        // entre sus arcos, MurkyMarks): color medio, meseta clara (el color más repetido entre los claros: el núcleo del «?») y
        // cuántos píxeles están en el marfil de UiPaper (ΔE00 ≤ 8).
        private static MarkSample SampleMark(CuiaView view, int k)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            float u = view.Unit;
            float yc = (CuiaGeometry.CavityBottomU + k + 0.5f - (0.5f * CuiaGeometry.TopFaceU)) * u;
            RectTransform liquid = view.Liquid.rectTransform;
            Vector3 c = cam.WorldToScreenPoint(liquid.TransformPoint(new Vector3(0f, yc, 0f)));
            Vector3 r = cam.WorldToScreenPoint(liquid.TransformPoint(new Vector3(0.3f * u, yc, 0f)));
            float half = Vector2.Distance(c, r);
            RenderTexture rt = cam.targetTexture;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(c.x - half), 0, rt.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(c.y - half), 0, rt.height - 1);
            int w = Mathf.Clamp(Mathf.CeilToInt(2f * half), 1, rt.width - x0);
            int h = Mathf.Clamp(Mathf.CeilToInt(2f * half), 1, rt.height - y0);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            Color32[] px = tex.GetPixels32();
            Object.Destroy(tex);

            var counts = new System.Collections.Generic.Dictionary<int, int>();
            float sr = 0f;
            float sg = 0f;
            float sb = 0f;
            int paper = 0;
            int best = -1;
            int bestCount = 0;
            for (int i = 0; i < px.Length; i++)
            {
                Color col = px[i];
                sr += col.r;
                sg += col.g;
                sb += col.b;
                if (ColorMath.DeltaE00(col, Palette.UiPaper) <= 8f)
                {
                    paper++;
                }

                if (ColorMath.RelativeLuminance(col) < 0.35f)
                {
                    continue;
                }

                int key = (px[i].r << 16) | (px[i].g << 8) | px[i].b;
                counts.TryGetValue(key, out int n);
                counts[key] = ++n;
                if (n > bestCount)
                {
                    bestCount = n;
                    best = key;
                }
            }

            var sample = new MarkSample
            {
                Mean = new Color(sr / px.Length, sg / px.Length, sb / px.Length, 1f),
                Plateau = best < 0 ? Color.black : (Color)new Color32((byte)(best >> 16), (byte)(best >> 8), (byte)best, 255),
                PlateauCount = bestCount,
                PaperCount = paper,
                Total = px.Length,
                Pixels = px,
            };
            return sample;
        }

        // Píxeles que cambian más de limit (0-255) en algún canal entre dos muestras de la misma caja.
        private static int Changed(Color32[] a, Color32[] b, int limit)
        {
            int n = 0;
            int count = Mathf.Min(a.Length, b.Length);
            for (int i = 0; i < count; i++)
            {
                if (Mathf.Abs(a[i].r - b[i].r) > limit || Mathf.Abs(a[i].g - b[i].g) > limit || Mathf.Abs(a[i].b - b[i].b) > limit)
                {
                    n++;
                }
            }

            return n + Mathf.Abs(a.Length - b.Length);
        }

        private static int Lowest(int mask)
        {
            for (int k = 0; k < 32; k++)
            {
                if (((mask >> k) & 1) != 0)
                {
                    return k;
                }
            }

            return -1;
        }

        private static int MurkyCuia(GameBootstrap boot)
        {
            BoardState b = boot.Flow!.Session.Current;
            for (int i = 0; i < b.Count; i++)
            {
                if (b[i].HasHidden)
                {
                    return i;
                }
            }

            Assert.Fail("El nivel " + boot.Flow.LevelNumber + " no tiene dosis ocultas");
            return -1;
        }

        private struct MarkSample
        {
            public Color Mean;
            public Color Plateau;
            public int PlateauCount;
            public int PaperCount;
            public int Total;
            public Color32[] Pixels;
        }
    }
}
