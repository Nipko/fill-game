#nullable enable
using System.Collections;
using System.Collections.Generic;
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
    /// inclinada al verter, en el fotograma más inclinado con el «?» a la vista: la trama y el «?», isótropo y de 0,30 u como
    /// mínimo, en su cuña), <c>72_reveal_l100</c> (el revelado por la regla al 20 %: el «?» se deforma entre destellos y el
    /// color nuevo empieza a salir), <c>72b_reveal_mid_l100</c> (al 45 %: la dosis ya en su tinte, la burbuja y lo último del
    /// «?» deshaciéndose en blanco encima, con el patrón de la fruta entrando), <c>72d_reveal_tNNN_l100_zoom</c> (recortes al
    /// 30, 35, 40, 48, 50, 54, 56, 60 y 70 %: el fundido cruzado y el pop de la cifra), <c>72c_firefly_l100</c> (la Luciérnaga
    /// sobre la oculta que queda), <c>72e_firefly_tNNN_l100_zoom</c> (la Luciérnaga sobre las 2 ocultas del estreno, del 10 al
    /// 95 %: el frente de luz que baja, el «?» que gira y el icono de la fruta sobre su disco) y <c>73_murky_badge_l100</c>
    /// (tras el revelado, «?×1»). De cada una, además, un recorte a tamaño real de la cuia turbia (<c>*_zoom</c>) para mirar la
    /// legibilidad a 48-56 dp, los solapes, los colores y la línea D-003. Muestrean además los píxeles: en 70, el «?» del shader
    /// en el marfil UiPaper (ΔE00 ≤ 3); en 71, que el «?» se ve y su alto inclinado (≥ 0,30 u, con su punto; se exige: sin él
    /// la captura no mediría nada); del 30 al 70 % del revelado, nunca el pardo sin «?»
    /// (07 §3) y el «?» que se disuelve claro (L* ≥ 75 mientras se ve a más de la mitad); del 54 al 56 % (paso a la dosis normal),
    /// menos del 10 % de los píxeles de la banda cambian; con la Luciérnaga, la mitad de arriba de la capa ya aclarada y la de
    /// abajo aún parda cuando el frente pasa por su mitad, el paso a la dosis normal sin saltos y el disco frente a la banda
    /// (≥ 3:1). Las medidas salen en el log como <c>AF_MURKY</c>. Necesitan gráficos: <c>unity test &lt;repo&gt; --mode PlayMode
    /// --filter AF.Tests.PlayMode.MurkySnapshotTests</c> sin <c>-nographics</c>; los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class MurkySnapshotTests
    {
        // Visibilidad del «?» por el hueco de su banda (LiquidGraphic.GlyphFade) desde la que se mide en la captura 71: entero.
        private const float GlyphShownFade = 0.99f;

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
            // queda el pardo sin «?» (07 §3; el «?» se disuelve sobre el color nuevo, ficha de Magic Sort §3.1) y el «?» que se
            // disuelve va hacia el blanco (sin su halo de tinta: claro, nunca gris malva sobre la fruta). Fundido cruzado (D-003):
            // la trama se va hacia el 30 %, el patrón de la fruta entra del 35 al 55 % y la cifra hace su pop hacia el 47-50 %;
            // del 54 al 56 % (el paso a la dosis normal) casi ningún píxel de la banda cambia.
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

                MarkSample band = SampleMarkWithoutFx(boot, source, layer); // la banda y su «?», sin los destellos ni la burbuja encima
                float toMurky = ColorMath.DeltaE00(band.Mean, Palette.Murky);
                float opacity = LiquidGraphic.MurkyMarkOpacity(source.Liquid.MurkyLookAt(layer), source.Liquid.MurkyProgressAt(layer));
                Debug.Log("AF_MURKY " + name + ": media de la banda #" + ColorUtility.ToHtmlStringRGB(band.Mean) + ", ΔE00 " + toMurky.ToString("F1")
                    + " del pardo y " + ColorMath.DeltaE00(band.Mean, tint).ToString("F1") + " de su tinte; «?» marfil " + band.PaperCount + " px de " + band.Total
                    + "; «?» al " + (100f * opacity).ToString("F0") + " %, su núcleo #" + ColorUtility.ToHtmlStringRGB(band.Core) + " (L* "
                    + ColorMath.LabLightness(band.Core).ToString("F0") + "); insignia «?×" + source.Murky.BadgeCount + "» a escala "
                    + source.Murky.BadgeDigit.rectTransform.localScale.x.ToString("F2"));
                Assert.That(toMurky >= 8f || band.PaperCount * 50 >= band.Total, Is.True, name + ": nunca el pardo sin «?» (07 §3)");
                if (opacity >= 0.5f)
                {
                    Assert.That(ColorMath.LabLightness(band.Core), Is.GreaterThanOrEqualTo(75f), name + ": el «?» que se disuelve, claro (L* ≥ 75)");
                }

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
        public IEnumerator CaptureFireflyL100()
        {
            string dir = SnapshotKit.Prepare();
            yield return Load(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            Cuia cuia = boot.Flow!.Session.Current[murky];
            int mask = cuia.HiddenMask;
            int top = Highest(mask);
            int bottom = Lowest(mask);
            Assert.That(top, Is.GreaterThan(bottom), "las 2 ocultas del estreno");
            PlayModeHelpers.Step(boot, 300);
            view.PlayReveal(mask, RevealStyle.Firefly, 1f);

            // Del 10 al 95 % de los 600 ms, en pasos de 2 ms: recortes del frente que baja y, cuando el frente pasa por la mitad de
            // cada capa, su mitad de arriba ya aclarada y la de abajo aún parda; el paso de cada capa a su dosis normal sin saltos;
            // el disco de la fruta frente a la banda.
            int[] shots = { 60, 120, 180, 240, 300, 360, 420, 480, 570 };
            int shot = 0;
            var halfway = new HashSet<int>();
            var switched = new HashSet<int>();
            var lastTurn = new Dictionary<int, Color32[]>();
            bool discMeasured = false;
            for (int ms = 2; view.Murky.IsAnimating && ms <= 600; ms += 2)
            {
                view.Tick(2f);
                if (shot < shots.Length && ms == shots[shot])
                {
                    SaveCrop(dir, "72e_firefly_t" + Mathf.RoundToInt(ms / 6f).ToString("000") + "_l100_zoom", view.Root, 28f);
                    shot++;
                }

                for (int k = bottom; k <= top; k++)
                {
                    if (((mask >> k) & 1) == 0)
                    {
                        continue;
                    }

                    LiquidGraphic.MurkyLook look = view.Liquid.MurkyLookAt(k);
                    float p = view.Liquid.MurkyProgressAt(k);
                    if (look == LiquidGraphic.MurkyLook.Turn && !halfway.Contains(k) && LiquidGraphic.FireflyFrontDepth(p) >= 0.5f)
                    {
                        halfway.Add(k);
                        Color upper = SampleBand(view, k, 0.12f, 0.36f, out _);
                        Color lower = SampleBand(view, k, 0.64f, 0.88f, out _);
                        float upperDe = ColorMath.DeltaE00(upper, Palette.Murky);
                        float lowerDe = ColorMath.DeltaE00(lower, Palette.Murky);
                        SaveCrop(dir, "72e_firefly_half" + k + "_l100_zoom", view.Root, 28f);
                        Debug.Log("AF_MURKY 72e: frente a media capa " + k + " (" + ms + " ms): arriba #" + ColorUtility.ToHtmlStringRGB(upper) + " a ΔE00 "
                            + upperDe.ToString("F1") + " del pardo; abajo #" + ColorUtility.ToHtmlStringRGB(lower) + " a " + lowerDe.ToString("F1"));
                        Assert.That(upperDe, Is.GreaterThanOrEqualTo(20f), "capa " + k + ": la mitad de arriba ya se aclaró (su luz baja por el vidrio)");
                        Assert.That(lowerDe, Is.LessThanOrEqualTo(8f), "capa " + k + ": la de abajo sigue parda");
                    }

                    if (look == LiquidGraphic.MurkyLook.Turn && p > 0.95f)
                    {
                        SampleBand(view, k, 0.1f, 0.9f, out Color32[] px);
                        lastTurn[k] = px;
                    }
                    else if (look == LiquidGraphic.MurkyLook.None && !switched.Contains(k) && lastTurn.ContainsKey(k))
                    {
                        switched.Add(k);
                        SampleBand(view, k, 0.1f, 0.9f, out Color32[] px);
                        int changed = Changed(lastTurn[k], px, 8);
                        Debug.Log("AF_MURKY 72e: capa " + k + " pasa a la dosis normal a los " + ms + " ms: " + changed + " de " + px.Length + " píxeles cambian más de 8/255");
                        Assert.That(changed * 10, Is.LessThan(px.Length), "capa " + k + ": el paso a la dosis normal no salta");
                    }

                    if (!discMeasured && view.Murky.FruitVisible(k) && view.Murky.FruitDisc(k).rectTransform.localScale.x > 0.95f && view.Murky.FruitDisc(k).color.a > 0.95f)
                    {
                        discMeasured = true;
                        SaveCrop(dir, "72e_firefly_disc_l100_zoom", view.Root, 28f);
                        DiscSample disc = SampleDisc(view, k);
                        float contrast = Mathf.Max(ColorMath.ContrastRatio(disc.Fill, disc.Band), ColorMath.ContrastRatio(disc.Rim, disc.Band));
                        Debug.Log("AF_MURKY 72e: disco de la capa " + k + ": marfil #" + ColorUtility.ToHtmlStringRGB(disc.Fill) + ", filo #"
                            + ColorUtility.ToHtmlStringRGB(disc.Rim) + ", banda #" + ColorUtility.ToHtmlStringRGB(disc.Band) + ": contraste " + contrast.ToString("F2") + ":1");
                        Assert.That(contrast, Is.GreaterThanOrEqualTo(3f), "el disco frente a la banda (≥ 3:1)");
                    }
                }
            }

            Assert.That(halfway.Count, Is.EqualTo(2), "el frente pasó por la mitad de las 2 capas");
            Assert.That(switched.Count, Is.EqualTo(2), "las 2 capas pasaron a su dosis normal");
            Assert.That(discMeasured, Is.True, "el icono de la fruta, sobre su disco");
            view.SnapMarks();
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
            int hidden = Lowest(boot.Flow!.Session.Current[murky].HiddenMask);
            MechanicTestKit.Tap(boot, path![path.Length - 1]);

            // El fotograma más inclinado con el «?» a la vista (revisión de T15, n.º 5: sin él, la captura no mide nada): la
            // inclinación máxima mientras vierte y, si ahí la cuña no da para el «?» (se apaga por debajo de 0,30 u de hueco), el
            // primero de la vuelta en que ya se ve entero, con la cuia aún inclinada.
            float best = 0f;
            bool returning = false;
            for (int t = 0; t < 3000; t += 4)
            {
                PlayModeHelpers.TickExact(boot, 4);
                float tilt = Mathf.Abs(source.Liquid.TiltDeg);
                if (!returning)
                {
                    if (source.BodyInFront && tilt >= best)
                    {
                        best = tilt;
                        continue;
                    }

                    if (best <= 30f)
                    {
                        continue;
                    }

                    returning = true; // ya pasó la inclinación máxima, mientras vierte
                }

                if (GlyphShown(source, hidden) || tilt <= 30f)
                {
                    break;
                }
            }

            float shotTilt = Mathf.Abs(source.Liquid.TiltDeg);
            Assert.That(best, Is.GreaterThan(30f), "inclinada");
            Save(dir, "71_murky_tilt_l106", boot, murky, source.Body);

            // El «?» inclinado: a la vista, isótropo, de 0,30 u como mínimo y entero (con su punto).
            Assert.That(source.Liquid.MurkyGlyphPlacement(hidden, out float cx, out float cy, out float size, out float room), Is.True);
            Assert.That(shotTilt, Is.GreaterThan(30f), "el fotograma elegido sigue inclinado");
            Assert.That(LiquidGraphic.GlyphFade(room), Is.GreaterThanOrEqualTo(GlyphShownFade), "con el «?» entero a la vista (hueco de " + room.ToString("F2") + " u)");
            GlyphBox glyph = MeasureGlyph(source, cx, cy, size);
            float pxPerU = PixelsPerU(source);
            Debug.Log("AF_MURKY 71: inclinada " + source.Liquid.TiltDeg.ToString("F1") + "° (máximo " + best.ToString("F1") + "°), «?» de " + size.ToString("F2")
                + " u en un hueco de " + room.ToString("F2") + " u; en la captura, " + glyph.Width + " × " + glyph.Height + " px de marfil ("
                + (glyph.Height / pxPerU).ToString("F2") + " u de alto, alto/ancho " + (glyph.Height / Mathf.Max(1f, (float)glyph.Width)).ToString("F2") + ")");
            Assert.That(glyph.Height / pxPerU, Is.GreaterThanOrEqualTo(0.85f * LiquidGraphic.GlyphMinU), "el «?» inclinado mide 0,30 u como mínimo");
            Assert.That(glyph.Height / Mathf.Max(1f, (float)glyph.Width), Is.GreaterThan(1.3f), "isótropo: no se aplasta (en reposo, ≈ 1,6)");
            Assert.That(glyph.Height / pxPerU, Is.GreaterThanOrEqualTo(0.8f * size), "entero: con su punto, sin cortarse en el vidrio");
            Assert.That(File.Exists(Path.Combine(dir, "71_murky_tilt_l106.png")), Is.True);
        }

        // El «?» de la dosis k se ve entero en la banda (GlyphFade del hueco que lleva la malla), con la inclinación de ahora.
        private static bool GlyphShown(CuiaView view, int k)
        {
            return view.Liquid.MurkyGlyphPlacement(k, out _, out _, out _, out float room) && LiquidGraphic.GlyphFade(room) >= GlyphShownFade;
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

        // SampleMark con la capa de efectos oculta un momento: lo que pinta el shader, sin los destellos blancos del revelado.
        private static MarkSample SampleMarkWithoutFx(GameBootstrap boot, CuiaView view, int k)
        {
            GameObject fx = boot.View!.Fx.Root.gameObject;
            bool was = fx.activeSelf;
            fx.SetActive(false);
            try
            {
                return SampleMark(view, k);
            }
            finally
            {
                fx.SetActive(was);
            }
        }

        // Píxeles de la captura en una caja de ±0,3 u alrededor del centro del «?» de la dosis k (el de la banda a media altura
        // entre sus arcos, MurkyMarks): color medio, meseta clara (el color más repetido entre los claros: el núcleo del «?»),
        // núcleo (la media de los píxeles a menos de 6 de L* del más claro) y cuántos píxeles están en el marfil de UiPaper
        // (ΔE00 ≤ 8).
        private static MarkSample SampleMark(CuiaView view, int k)
        {
            float u = view.Unit;
            float yc = (CuiaGeometry.CavityBottomU + k + 0.5f - (0.5f * CuiaGeometry.TopFaceU)) * u;
            Color32[] px = ReadBox(view, 0f, yc, 0.3f * u, 0.3f * u, out _, out _);
            var counts = new Dictionary<int, int>();
            float sr = 0f;
            float sg = 0f;
            float sb = 0f;
            int paper = 0;
            int best = -1;
            int bestCount = 0;
            float maxL = float.MinValue;
            for (int i = 0; i < px.Length; i++)
            {
                Color col = px[i];
                sr += col.r;
                sg += col.g;
                sb += col.b;
                maxL = Mathf.Max(maxL, ColorMath.LabLightness(col));
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

            float cr = 0f;
            float cg = 0f;
            float cb = 0f;
            int coreCount = 0;
            for (int i = 0; i < px.Length; i++)
            {
                Color col = px[i];
                if (ColorMath.LabLightness(col) >= maxL - 6f)
                {
                    cr += col.r;
                    cg += col.g;
                    cb += col.b;
                    coreCount++;
                }
            }

            var sample = new MarkSample
            {
                Mean = new Color(sr / px.Length, sg / px.Length, sb / px.Length, 1f),
                Plateau = best < 0 ? Color.black : (Color)new Color32((byte)(best >> 16), (byte)(best >> 8), (byte)best, 255),
                PlateauCount = bestCount,
                Core = coreCount > 0 ? new Color(cr / coreCount, cg / coreCount, cb / coreCount, 1f) : Color.black,
                PaperCount = paper,
                Total = px.Length,
                Pixels = px,
            };
            return sample;
        }

        // Mediana (canal a canal) de los píxeles de la banda k entre las profundidades depthFrom y depthTo (0 arriba, 1 abajo,
        // entre sus arcos), en X de 0,42 a 0,58 u (a la derecha del «?» y del disco de la fruta y lejos de los brillos y del
        // oscurecido de la pared); px, la caja entera.
        private static Color SampleBand(CuiaView view, int k, float depthFrom, float depthTo, out Color32[] px)
        {
            float u = view.Unit;
            float sag = CuiaGeometry.FaceSagU(0.5f);
            float yTop = (CuiaGeometry.CavityBottomU + k + 1f - depthFrom - sag) * u;
            float yBottom = (CuiaGeometry.CavityBottomU + k + 1f - depthTo - sag) * u;
            px = ReadBox(view, 0.5f * u, 0.5f * (yTop + yBottom), 0.08f * u, 0.5f * (yTop - yBottom), out _, out _);
            return Median(px);
        }

        // Disco de la fruta de la capa k a la vista: su marfil (la mediana de los píxeles claros de su centro), su filo (el
        // anillo oscuro) y la banda a su lado (SampleBand).
        private static DiscSample SampleDisc(CuiaView view, int k)
        {
            float u = view.Unit;
            float yc = (CuiaGeometry.CavityBottomU + k + 0.5f - (0.5f * CuiaGeometry.TopFaceU)) * u;
            float r = 0.5f * MurkyMarks.FireflyDiscU * u;
            Color32[] ring = ReadBox(view, 0f, yc, r, r, out int w, out int h);
            var fill = new List<Color32>();
            var rim = new List<Color32>();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = ((x + 0.5f) / w) - 0.5f;
                    float dy = ((y + 0.5f) / h) - 0.5f;
                    float d = 2f * Mathf.Sqrt((dx * dx) + (dy * dy)); // 0 en el centro, 1 en el borde de la caja
                    Color32 c = ring[(y * w) + x];
                    if (d > 0.8f && d < 0.9f)
                    {
                        rim.Add(c); // el filo: del 78 al 90 % del radio de la caja
                    }
                    else if (d > 0.68f && d < 0.76f)
                    {
                        fill.Add(c); // el marfil entre el icono y el filo
                    }
                }
            }

            var sample = new DiscSample
            {
                Fill = Median(fill.ToArray()),
                Rim = Darkest(rim.ToArray()),
                Band = SampleBand(view, k, 0.2f, 0.8f, out _),
            };
            return sample;
        }

        // Caja de ±halfW × ±halfH dp alrededor de (x, y) dp en las coordenadas del líquido de la vista, leída de la captura.
        private static Color32[] ReadBox(CuiaView view, float x, float y, float halfW, float halfH, out int width, out int height)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RectTransform liquid = view.Liquid.rectTransform;
            Vector3 c = cam.WorldToScreenPoint(liquid.TransformPoint(new Vector3(x, y, 0f)));
            Vector3 rx = cam.WorldToScreenPoint(liquid.TransformPoint(new Vector3(x + halfW, y, 0f)));
            Vector3 ry = cam.WorldToScreenPoint(liquid.TransformPoint(new Vector3(x, y + halfH, 0f)));
            float hw = Vector2.Distance(c, rx);
            float hh = Vector2.Distance(c, ry);
            RenderTexture rt = cam.targetTexture;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(c.x - hw), 0, rt.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(c.y - hh), 0, rt.height - 1);
            width = Mathf.Clamp(Mathf.CeilToInt(2f * hw), 1, rt.width - x0);
            height = Mathf.Clamp(Mathf.CeilToInt(2f * hh), 1, rt.height - y0);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x0, y0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            Color32[] px = tex.GetPixels32();
            Object.Destroy(tex);
            return px;
        }

        // Caja de marfil del «?» en la captura: los píxeles a ΔE00 ≤ 6 de UiPaper (su núcleo; el vidrio queda a ≈ 9) en una caja
        // de 1,2 × su alto alrededor de su centro (cx, cy), en coordenadas de la cuia (la caja de pantalla no gira con la cuia:
        // el «?» va derecho).
        private static GlyphBox MeasureGlyph(CuiaView view, float cx, float cy, float sizeU)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            float u = view.Unit;
            RectTransform liquid = view.Liquid.rectTransform;
            Vector3 c = cam.WorldToScreenPoint(liquid.TransformPoint(new Vector3(cx * u, cy * u, 0f)));
            float half = 0.6f * sizeU * PixelsPerU(view);
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
            int left = w;
            int right = -1;
            int bottom = h;
            int top = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (ColorMath.DeltaE00(px[(y * w) + x], Palette.UiPaper) <= 6f)
                    {
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                        bottom = Mathf.Min(bottom, y);
                        top = Mathf.Max(top, y);
                    }
                }
            }

            return new GlyphBox { Width = right >= left ? right - left + 1 : 0, Height = top >= bottom ? top - bottom + 1 : 0 };
        }

        // Píxeles de la captura por u de la cuia (3 px/dp por la unidad).
        private static float PixelsPerU(CuiaView view)
        {
            Camera cam = SnapshotKit.CaptureCamera!;
            RectTransform liquid = view.Liquid.rectTransform;
            Vector3 a = cam.WorldToScreenPoint(liquid.TransformPoint(Vector3.zero));
            Vector3 b = cam.WorldToScreenPoint(liquid.TransformPoint(new Vector3(view.Unit, 0f, 0f)));
            return Vector2.Distance(a, b);
        }

        private static Color Median(Color32[] px)
        {
            if (px.Length == 0)
            {
                return Color.black;
            }

            var r = new byte[px.Length];
            var g = new byte[px.Length];
            var b = new byte[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                r[i] = px[i].r;
                g[i] = px[i].g;
                b[i] = px[i].b;
            }

            System.Array.Sort(r);
            System.Array.Sort(g);
            System.Array.Sort(b);
            int m = px.Length / 2;
            return new Color32(r[m], g[m], b[m], 255);
        }

        private static Color Darkest(Color32[] px)
        {
            Color best = Color.white;
            float bestL = float.MaxValue;
            for (int i = 0; i < px.Length; i++)
            {
                float l = ColorMath.RelativeLuminance(px[i]);
                if (l < bestL)
                {
                    bestL = l;
                    best = px[i];
                }
            }

            return best;
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

        private static int Highest(int mask)
        {
            for (int k = 31; k >= 0; k--)
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
            public Color Core;
            public int PaperCount;
            public int Total;
            public Color32[] Pixels;
        }

        private struct DiscSample
        {
            public Color Fill;
            public Color Rim;
            public Color Band;
        }

        private struct GlyphBox
        {
            public int Width;
            public int Height;
        }
    }
}
