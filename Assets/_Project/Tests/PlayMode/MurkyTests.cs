#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Aguas turbias en el shader y su revelado (T15; 07 §4.3; 08 §5.1-§5.2; 02 §11 T3; 02 §4.2; propuesta de D-007 b.2; ficha
    /// de Magic Sort §3.1; 04 §2.5; D-010), con los niveles reales: el 100 (estreno, una cuia turbia con 2 dosis ocultas) y el
    /// 106 (una oculta bajo una fruta que se vierte). Las ocultas las pinta el shader del líquido (pardo, trama y «?», sin su
    /// tinte) y la cuia muestra «?×n»; inclinada, la trama y el «?» (isótropo, de 0,30 u como mínimo o apagado) se quedan en su
    /// banda; el revelado por la regla empieza cuando el origen vuelve y dura 1.200 ms × factor con 3-4 destellos y una
    /// burbuja; la Luciérnaga baja su frente de luz de arriba abajo en 600 ms × factor, entre capas y dentro de cada capa;
    /// Marea y Volver a la orilla no vuelven a ocultar nada (I5); en la intro, todas a la vez en el primer impacto (y ya
    /// turbias en el fundido de Reducir movimiento). Los dos revelados son fundidos cruzados sin saltos (la trama se va, el «?»
    /// se disuelve o gira, entra el patrón de la fruta y la insignia baja con un pop cuando el «?» se va; D-003); cortado a
    /// medias (Marea, 02 §14) no deja destellos ni burbujas tarde; con Reducir movimiento, sin pop ni partículas (02 §12); y ni
    /// el revelado, ni la malla turbia, ni la intro del 100 asignan memoria (08 §5.4; A10). El reloj lo lleva el test.
    /// </summary>
    public sealed class MurkyTests
    {
        private static readonly MethodInfo PopulateMesh = typeof(LiquidGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        // Profundidades de una banda (0 arriba, 1 abajo) en las que se mide lo que pinta el frente de la Luciérnaga.
        private static readonly float[] Depths = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Murky100_HiddenRenderedByShader()
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            Cuia cuia = boot.Flow!.Session.Current[murky];
            CuiaView view = boot.View!.Views[murky];
            Assert.That(CountBits(cuia.HiddenMask), Is.EqualTo(2), "el 100 estrena con 2 dosis ocultas (b.2)");

            var rows = new float[cuia.Capacity];
            var patterns = new float[cuia.Capacity];
            MeshBands(view, rows, patterns);
            for (int k = 0; k < cuia.Count; k++)
            {
                if (cuia.IsHiddenAt(k))
                {
                    Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden), "dosis " + k + ": turbia en el shader");
                    Assert.That(view.DoseColor(k), Is.EqualTo(Palette.Murky), "pardo #8A7B6A");
                    Assert.That(view.DoseColor(k), Is.Not.EqualTo(Palette.Tint(cuia.ColorAt(k))), "nunca su tinte");
                    Assert.That(view.Murky.MarkVisible(k), Is.True, "con su «?»");
                    Assert.That(rows[k], Is.EqualTo((float)ProceduralArt.MurkyPatternRow), "la fila turbia del atlas (trama y «?»), no la de su fruta");
                    Assert.That(patterns[k], Is.EqualTo(LiquidGraphic.MurkyPatternMode).Within(1e-4f), "patrón turbio, sin la mezcla del 15 %");
                }
                else
                {
                    Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None));
                    Assert.That(rows[k], Is.EqualTo((float)cuia.ColorAt(k)), "la visible, con la fila de su fruta");
                }
            }

            int loose = 0;
            Image[] images = view.Murky.Root.GetComponentsInChildren<Image>(false);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != view.Murky.Badge && !images[i].transform.IsChildOf(view.Murky.Badge.transform))
                {
                    loose++;
                }
            }

            Assert.That(loose, Is.EqualTo(0), "ningún «?» suelto en sprites");

            // Un solo material de líquido para todas las cuias (un lote), con el atlas que lleva la fila turbia.
            CuiaView[] views = boot.View.Views;
            for (int i = 0; i < views.Length; i++)
            {
                Assert.That(views[i].Liquid.material, Is.SameAs(views[0].Liquid.material), "cuia " + i + ": el mismo material");
            }

            Texture atlas = view.Liquid.mainTexture;
            Assert.That(atlas.height, Is.EqualTo((ProceduralArt.MurkyPatternRow + 1) * ProceduralArt.PatternRowPx), "atlas con la fila turbia");
            Assert.That(views[0].Liquid.material.GetFloat("_PatternRows"), Is.EqualTo(ProceduralArt.MurkyPatternRow + 1f));
        }

        [UnityTest]
        public IEnumerator Murky100_BadgeCount()
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            ProceduralArt art = view.OverlayHost.Art;
            MurkyMarks marks = view.Murky;
            Assert.That(marks.BadgeVisible, Is.True, "la cuia turbia muestra «?×n» (02 §11 T3)");
            Assert.That(marks.BadgeCount, Is.EqualTo(2));
            Assert.That(marks.BadgeDigit.sprite, Is.SameAs(art.MurkyBadgeDigit(2)), "«?×2»");
            Image[] glyphs = marks.Badge.GetComponentsInChildren<Image>(false);
            Assert.That(glyphs.Length, Is.EqualTo(4), "píldora, «?», «×» y la cifra");
            Assert.That(glyphs[1].sprite, Is.SameAs(art.MurkyBadgeQuestion));
            Assert.That(glyphs[2].sprite, Is.SameAs(art.MurkyBadgeTimes));

            // Bajo el pie, en la cara de la repisa (15 dp) y no más ancha que la cuia.
            RectTransform badge = marks.Badge.rectTransform;
            float top = badge.anchoredPosition.y + (0.5f * badge.sizeDelta.y);
            float bottom = badge.anchoredPosition.y - (0.5f * badge.sizeDelta.y);
            Assert.That(top, Is.LessThanOrEqualTo(0f), "bajo el pie: no tapa el líquido");
            Assert.That(bottom, Is.GreaterThanOrEqualTo(-15f), "dentro de la repisa");
            Assert.That(badge.sizeDelta.x, Is.LessThanOrEqualTo(2f * view.Unit), "no más ancha que la cuia");

            for (int i = 0; i < boot.View.Views.Length; i++)
            {
                if (i != murky)
                {
                    Assert.That(boot.View.Views[i].Murky.BadgeVisible, Is.False, "cuia " + i + " sin ocultas: sin insignia");
                }
            }
        }

        [UnityTest]
        public IEnumerator Murky106_TiltKeepsBand()
        {
            yield return MechanicTestKit.LoadPlaying(106);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            Pour[] path = Trigger(boot, (ev, b, a) => a[murky].Count < b[murky].Count); // la cuia turbia vierte (y se inclina)
            MechanicTestKit.PlayPrefix(boot, path);
            CuiaView source = boot.View!.Views[murky];
            Cuia before = boot.Flow!.Session.Current[murky];
            int hidden = Lowest(before.HiddenMask);
            MechanicTestKit.Tap(boot, path[path.Length - 1]);
            float maxTilt = 0f;
            int checkedFrames = 0;
            int glyphFrames = 0;
            float minGlyph = float.MaxValue;
            int t = 0;
            while (!boot.View.IsViewIdle)
            {
                Assert.That(t, Is.LessThan(6000));
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                float tilt = source.Liquid.TiltDeg;
                maxTilt = Mathf.Max(maxTilt, Mathf.Abs(tilt));
                if (!source.BodyInFront || Mathf.Abs(tilt) < 30f)
                {
                    continue;
                }

                checkedFrames++;
                string where = "t = " + t + " ms, " + tilt.ToString("F1") + "°";
                Assert.That(source.Liquid.MurkyLookAt(hidden), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden), "en vuelo, la oculta sigue turbia");
                Assert.That(source.Murky.BadgeVisible, Is.False, "la insignia se oculta en vuelo");
                Assert.That(source.Liquid.MurkyGlyphPlacement(hidden, out float cx, out float cy, out float size, out float room), Is.True, where);
                bool drawn = LiquidGraphic.GlyphFade(room) > 0.001f;
                Assert.That(source.Murky.MarkVisible(hidden), Is.EqualTo(drawn), where + ": el «?» se ve si cabe uno de 0,30 u (si no, solo la trama)");
                if (drawn)
                {
                    glyphFrames++;
                    minGlyph = Mathf.Min(minGlyph, size);
                    Assert.That(size, Is.GreaterThanOrEqualTo(LiquidGraphic.GlyphMinU - 1e-4f), where + ": «?» de 0,30 u como mínimo");
                    Assert.That(size, Is.LessThanOrEqualTo(room + 1e-3f), where + ": el «?» cabe en el hueco de su columna");
                    AssertGlyphInsideGlass(source, cx, cy, size, tilt, where);
                }

                AssertMurkyBandUpright(source, hidden, tilt, size, where);
            }

            Assert.That(maxTilt, Is.GreaterThan(30f), "el origen se inclinó al verter");
            Assert.That(checkedFrames, Is.GreaterThan(3), "fotogramas inclinados comprobados");
            Assert.That(glyphFrames, Is.GreaterThan(0), "con el «?» a la vista en vuelo");
            TestContext.WriteLine("Nivel 106 en vuelo: " + checkedFrames + " fotogramas inclinados, " + glyphFrames + " con «?» (el más pequeño, "
                + minGlyph.ToString("F2") + " u); inclinación máxima " + maxTilt.ToString("F1") + "°.");
            MechanicTestKit.AssertViewMatchesModel(boot);
            Cuia now = boot.Flow.Session.Current[murky];
            Assert.That(source.Liquid.MurkyLookAt(hidden), Is.EqualTo(now.IsHiddenAt(hidden) ? LiquidGraphic.MurkyLook.Hidden : LiquidGraphic.MurkyLook.None), "de vuelta, como el modelo");
            Assert.That(source.Murky.BadgeVisible, Is.EqualTo(now.HasHidden), "la insignia, como el modelo");
        }

        [UnityTest]
        public IEnumerator Murky100_RevealAfterReturnWithSparkles()
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, b, a) => ev.RevealedMask != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            CuiaView source = boot.View!.Views[pour.Source];
            BoardAnimator animator = boot.View.Animator;
            FxLayer fx = boot.View.Fx;
            Assert.That(source.Murky.BadgeCount, Is.EqualTo(2), "antes, «?×2»");
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out _);
            Cuia after = boot.Flow!.Session.Current[pour.Source];
            int layer = after.Count - 1;
            int tint = after.ColorAt(layer);
            float backAt = -1f;
            float revealSeen = -1f;
            bool flew = false;
            int maxAdditive = 0;
            float maxDissolve = 0f;
            float minTintedFaded = 1f;
            int fadedFrames = 0;
            float dropAt = -1f;
            int elapsed = StepWatching(boot, () =>
            {
                flew |= source.BodyInFront;
                if (flew && !source.BodyInFront && backAt < 0f)
                {
                    backAt = animator.Clock;
                }

                if (source.Murky.IsAnimating)
                {
                    if (revealSeen < 0f)
                    {
                        revealSeen = animator.Clock;
                        Assert.That(source.BodyInFront, Is.False, "el revelado empieza con el origen en su ranura");
                    }

                    if (dropAt < 0f && source.Murky.BadgeCount == 1)
                    {
                        dropAt = source.Murky.RevealProgress;
                    }

                    maxAdditive = Mathf.Max(maxAdditive, fx.AdditiveCount);
                    LiquidGraphic.MurkyLook look = source.Liquid.MurkyLookAt(layer);
                    if (look == LiquidGraphic.MurkyLook.Dissolve)
                    {
                        maxDissolve = Mathf.Max(maxDissolve, source.Liquid.MurkyProgressAt(layer));
                    }

                    // Como en Magic Sort (ficha §3.1), el «?» se disuelve sobre el color nuevo: en ningún fotograma queda el
                    // pardo sin «?» (07 §3, fila Cacao). Opacidad del «?» como la calcula el shader.
                    float mark = LiquidGraphic.MurkyMarkOpacity(look, source.Liquid.MurkyProgressAt(layer));
                    float tinted = Lighten(source, layer, tint);
                    string at = "revelado al " + (100f * source.Murky.RevealProgress).ToString("F0") + " %";
                    Assert.That(Mathf.Max(mark, tinted), Is.GreaterThanOrEqualTo(0.5f), at + ": nunca el pardo sin «?» (07 §3)");
                    if (mark < 0.5f)
                    {
                        fadedFrames++;
                        minTintedFaded = Mathf.Min(minTintedFaded, tinted);
                        Assert.That(tinted, Is.GreaterThanOrEqualTo(0.75f), at + ": el «?» se apaga sobre el color nuevo (ficha de Magic Sort §3.1)");
                    }
                }
                else if (revealSeen < 0f)
                {
                    Assert.That(source.DoseColor(layer), Is.EqualTo(Palette.Murky), "turbia hasta su paso Reveal");
                }
            });

            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord reveal = MechanicTestKit.Record(animator, MechanicStepKind.Reveal, pour.Source);
            Assert.That(backAt, Is.GreaterThan(0f), "el origen voló y volvió");
            Assert.That(reveal.StartMs, Is.GreaterThanOrEqualTo(backAt - PlayModeHelpers.FrameMs), "el revelado empieza después de que el origen vuelva");
            Assert.That(reveal.EndMs - reveal.StartMs, Is.EqualTo(MurkyMarks.RevealRuleMs * reveal.SpeedFactor).Within(1f), "1.200 ms × factor (b.2: < 1,5 s)");
            Assert.That(MurkyMarks.RevealRuleMs, Is.EqualTo(1200f));
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "dentro del presupuesto desde el toque (T07)");
            Assert.That(source.Murky.LastSparkles, Is.InRange(3, 4), "3-4 destellos de estrella (Magic Sort §3.1)");
            Assert.That(source.Murky.LastBubbles, Is.EqualTo(1), "una burbuja (b.2)");
            Assert.That(maxAdditive, Is.GreaterThanOrEqualTo(3), "los destellos, en la capa de efectos");
            Assert.That(maxDissolve, Is.GreaterThan(0.3f), "el «?» se deforma y se disuelve en el shader");
            Assert.That(fadedFrames, Is.GreaterThan(3), "fotogramas con el «?» ya apagado comprobados");
            float frame = PlayModeHelpers.FrameMs / (MurkyMarks.RevealRuleMs * reveal.SpeedFactor);
            Assert.That(dropAt, Is.EqualTo(MurkyMarks.RuleMarkGoneAt).Within(frame + 1e-3f), "«?×1» justo cuando el «?» termina de irse (≈ 47 %), no al 55 %");
            TestContext.WriteLine("Nivel 100: " + elapsed + " ms del toque a la vista quieta; con el «?» por debajo de la mitad, la dosis va al menos al "
                + (100f * minTintedFaded).ToString("F0") + " % de su tinte. " + MechanicTestKit.Describe(animator));

            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(source.Liquid.MurkyLookAt(layer), Is.EqualTo(LiquidGraphic.MurkyLook.None), "revelada: su tinte");
            Assert.That(source.DoseColor(layer), Is.EqualTo(Palette.Tint(after.ColorAt(layer))));
            Assert.That(source.Murky.MarkVisible(layer), Is.False);
            Assert.That(source.Murky.BadgeCount, Is.EqualTo(1), "al terminar, «?×1»");
            Assert.That(source.Murky.BadgeVisible, Is.True);
            Assert.That(source.Murky.BadgeDigit.sprite, Is.SameAs(source.OverlayHost.Art.MurkyBadgeDigit(1)));
        }

        [UnityTest]
        public IEnumerator Reveal_RuleBloomsInOkLabWithEaseOut() // D-010 (guion §10: una sola floración OKLab); 02 §4.1 (ease-out)
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            Cuia cuia = boot.Flow!.Session.Current[murky];
            int layer = Highest(cuia.HiddenMask);
            Color target = Palette.Tint(cuia.ColorAt(layer));
            view.PlayReveal(1 << layer, RevealStyle.Rule, 1f);
            float span = MurkyMarks.RuleTintFullAt - MurkyMarks.RuleTintFrom;
            for (int ms = 10; view.Murky.IsAnimating; ms += 10)
            {
                view.Tick(10f);
                float t = ms / MurkyMarks.RevealRuleMs;
                float e = Ease.Out(Mathf.Clamp01((t - MurkyMarks.RuleTintFrom) / span));
                Color expected = e <= 0f ? Palette.Murky : (e >= 1f ? target : ColorMath.OkLabLerp(Palette.Murky, target, e));
                if (view.Murky.IsAnimating)
                {
                    Assert.That(Distance(view.Liquid.DoseColorAt(layer), expected), Is.LessThan(1e-4f), "t = " + ms + " ms: floración OKLab con ease-out");
                }
            }

            Assert.That(view.Liquid.DoseColorAt(layer), Is.EqualTo(target), "al terminar, su tinte exacto");
        }

        [UnityTest]
        public IEnumerator Reveal_FireflyStyleTopDown()
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            Cuia cuia = boot.Flow!.Session.Current[murky];
            int mask = cuia.HiddenMask;
            int topLayer = Highest(mask);
            int bottomLayer = Lowest(mask);
            Assert.That(topLayer, Is.GreaterThan(bottomLayer), "dos ocultas");
            Assert.That(view.RevealStepMs(RevealStyle.Firefly), Is.EqualTo(600f), "04 §2.5: 0,6 s");

            // Con factor 0,5 dura de verdad 300 ms (sigue en curso a los 290) y con factor 1, 600 ms (en curso a los 590).
            RunFirefly(view, cuia, mask, 0.5f, 300);
            RunFirefly(view, cuia, mask, 1f, 600);
            Assert.That(view.Murky.FruitVisible(topLayer) || view.Murky.FruitVisible(bottomLayer), Is.False, "sin iconos al terminar");
            Assert.That(view.Liquid.MurkyLookAt(topLayer), Is.EqualTo(LiquidGraphic.MurkyLook.None));
            Assert.That(view.Liquid.MurkyLookAt(bottomLayer), Is.EqualTo(LiquidGraphic.MurkyLook.None));
            Assert.That(Lighten(view, bottomLayer, cuia.ColorAt(bottomLayer)), Is.EqualTo(1f).Within(0.02f), "con su tinte");
            Assert.That(view.Murky.BadgeCount, Is.EqualTo(0), "sin «?» que contar");
        }

        [UnityTest]
        public IEnumerator Reveal_FireflyFrontComesDownTheBand() // 04 §2.5: «su luz baja por el vidrio» (hallazgo 12)
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            Cuia cuia = boot.Flow!.Session.Current[murky];
            int mask = cuia.HiddenMask;
            view.PlayReveal(mask, RevealStyle.Firefly, 1f);

            // Cuando el frente pasa por la mitad de cada capa, su mitad de arriba ya es de su color y la de abajo, aún parda.
            int halfway = 0;
            var seen = new HashSet<int>();
            for (int ms = 0; view.Murky.IsAnimating && ms < 700; ms += 2)
            {
                view.Tick(2f);
                for (int k = 0; k < cuia.Count; k++)
                {
                    if (((mask >> k) & 1) == 0 || seen.Contains(k) || view.Liquid.MurkyLookAt(k) != LiquidGraphic.MurkyLook.Turn)
                    {
                        continue;
                    }

                    float front = LiquidGraphic.FireflyFrontDepth(view.Liquid.MurkyProgressAt(k));
                    if (front < 0.5f)
                    {
                        continue;
                    }

                    seen.Add(k);
                    float upper = ColorMath.DeltaE00(view.Liquid.PaintedColorAt(k, 0.25f), Palette.Murky);
                    float lower = ColorMath.DeltaE00(view.Liquid.PaintedColorAt(k, 0.75f), Palette.Murky);
                    TestContext.WriteLine("Capa " + k + " con el frente a " + front.ToString("F2") + " (" + ms + " ms): mitad de arriba a ΔE00 "
                        + upper.ToString("F1") + " del pardo y la de abajo a " + lower.ToString("F1") + ".");
                    Assert.That(upper, Is.GreaterThanOrEqualTo(20f), "capa " + k + ": la mitad de arriba ya se aclaró");
                    Assert.That(lower, Is.LessThanOrEqualTo(5f), "capa " + k + ": la de abajo sigue parda");
                    Assert.That(LiquidGraphic.MurkyHatchAt(LiquidGraphic.MurkyLook.Turn, view.Liquid.MurkyProgressAt(k), 0.25f), Is.EqualTo(0f).Within(1e-4f), "sin trama encima del frente");
                    Assert.That(LiquidGraphic.MurkyHatchAt(LiquidGraphic.MurkyLook.Turn, view.Liquid.MurkyProgressAt(k), 0.75f), Is.EqualTo(LiquidGraphic.MurkyHatchOpacity).Within(1e-4f), "con trama debajo");
                    halfway++;
                }
            }

            Assert.That(halfway, Is.EqualTo(2), "el frente pasó por la mitad de las 2 capas");
            view.SnapMarks();
        }

        [UnityTest]
        public IEnumerator Reveal_FireflyCrossfadeWithoutJumps() // D-003; 04 §2.5; 07 §3 y §4.3 (hallazgos 2, 9 y 13)
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            MurkyMarks marks = view.Murky;
            Cuia cuia = boot.Flow!.Session.Current[murky];
            int mask = cuia.HiddenMask;
            ProceduralArt art = view.OverlayHost.Art;
            Assert.That(CountBits(mask), Is.EqualTo(2));
            Assert.That(view.PlayReveal(mask, RevealStyle.Firefly, 1f), Is.EqualTo(MurkyMarks.RevealFireflyMs).Within(1e-3f));

            var prevHatch = new float[cuia.Capacity];
            var prevFruit = new float[cuia.Capacity];
            var prevLight = new float[cuia.Capacity];
            var normalAt = new int[cuia.Capacity];
            var lastTurn = new UIVertex[cuia.Capacity];
            var lastTurnProgress = new float[cuia.Capacity];
            for (int k = 0; k < cuia.Capacity; k++)
            {
                prevHatch[k] = LiquidGraphic.MurkyHatchOpacity;
                normalAt[k] = -1;
            }

            int count = 2;
            int drops = 0;
            for (int ms = 10; marks.IsAnimating; ms += 10)
            {
                Assert.That(ms, Is.LessThanOrEqualTo(700), "termina a los 600 ms");
                view.Tick(10f);
                for (int k = 0; k < cuia.Count; k++)
                {
                    if (((mask >> k) & 1) == 0)
                    {
                        continue;
                    }

                    string at = "capa " + k + ", t = " + ms + " ms";
                    int tint = cuia.ColorAt(k);
                    LiquidGraphic.MurkyLook look = view.Liquid.MurkyLookAt(k);
                    float p = view.Liquid.MurkyProgressAt(k);
                    float hatch = LiquidGraphic.MurkyHatchAmount(look, p);
                    float fruit = LiquidGraphic.FruitPatternWeight(look, p);
                    float light = BandLight(view, k, tint);

                    // Sigue en Turn hasta el final de su ventana y solo entonces pasa a la dosis normal (y ya no vuelve).
                    if (normalAt[k] >= 0)
                    {
                        Assert.That(look, Is.EqualTo(LiquidGraphic.MurkyLook.None), at + ": ya normal, sigue normal");
                    }
                    else if (look == LiquidGraphic.MurkyLook.None)
                    {
                        normalAt[k] = ms;
                        Assert.That(lastTurnProgress[k], Is.GreaterThan(LiquidGraphic.FireflyPatternTo), at + ": pasa a normal con todo a 1");
                        Assert.That(LiquidGraphic.FireflyLightAt(lastTurnProgress[k], 1f), Is.EqualTo(1f).Within(1e-4f), at + ": el frente ya salió por abajo");
                        Assert.That(LiquidGraphic.FruitPatternWeight(LiquidGraphic.MurkyLook.Turn, lastTurnProgress[k]), Is.EqualTo(1f).Within(1e-4f), at + ": el patrón ya entró");
                        List<UIVertex> band = BandVertices(view, k);
                        Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), at + ": la dosis normal tiene malla");
                        UIVertex first = band[0];
                        // Nivel 100, con Patrones de fruta: la fila lleva además la marca de su icono, que entra con su patrón.
                        Assert.That(view.FruitIconsOn, Is.True, at + ": nivel con Patrones de fruta");
                        float code = LiquidGraphic.RevealRowCode(tint, true);
                        Assert.That(lastTurn[k].uv3.w, Is.EqualTo(code), at + ": con la Luciérnaga, la fila de su fruta (+ 0,5 si va en marfil) y la marca de su icono");
                        Assert.That(code - LiquidGraphic.RevealIconFlag, Is.EqualTo(LiquidGraphic.FruitRowCode(tint)), at + ": que el shader quita antes de leer la fila");
                        Assert.That(first.uv3.w, Is.EqualTo((float)tint), at + ": la dosis normal, con la fila de su fruta");
                        Vector4 a = lastTurn[k].uv1;
                        Vector4 b = first.uv1;
                        Assert.That(Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z), Is.LessThan(0.02f), at + ": la franja de su tinte ya entró");
                        Assert.That(Distance(lastTurn[k].color, first.color), Is.LessThan(2f / 255f), at + ": el color de la dosis ya es su tinte");
                    }
                    else
                    {
                        Assert.That(look, Is.EqualTo(LiquidGraphic.MurkyLook.Turn), at);
                        Assert.That(p, Is.LessThan(1f), at);
                        List<UIVertex> band = BandVertices(view, k, true);
                        Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), at + ": la banda que se revela tiene malla");
                        lastTurn[k] = band[0];
                        lastTurnProgress[k] = p;
                    }

                    // La trama baja y el patrón de la fruta sube sin saltos, uno después del otro (nunca los dos a la vez); lo
                    // que pinta el frente solo aclara, y sin saltos.
                    Assert.That(hatch, Is.LessThanOrEqualTo(prevHatch[k] + 1e-6f), at + ": la trama solo baja");
                    Assert.That(fruit, Is.GreaterThanOrEqualTo(prevFruit[k] - 1e-6f), at + ": el patrón de la fruta solo sube");
                    Assert.That(fruit - prevFruit[k], Is.LessThan(0.15f), at + ": y sin saltos (también al pasar a la dosis normal)");
                    Assert.That(hatch > 0f && fruit > 0f, Is.False, at + ": nunca la trama y el patrón de la fruta a la vez");
                    Assert.That(light, Is.GreaterThanOrEqualTo(prevLight[k] - 1e-3f), at + ": la capa solo se aclara");
                    Assert.That(light - prevLight[k], Is.LessThan(0.35f), at + ": y sin saltos");
                    prevHatch[k] = hatch;
                    prevFruit[k] = fruit;
                    prevLight[k] = light;
                }

                // La insignia baja en uno cuando cada «?» queda de canto, con el pop de la cifra.
                if (marks.BadgeCount < count)
                {
                    Assert.That(marks.BadgeCount, Is.EqualTo(count - 1), "t = " + ms + " ms: de una en una");
                    count = marks.BadgeCount;
                    drops++;
                    if (count > 0)
                    {
                        Assert.That(marks.BadgeDigit.sprite, Is.SameAs(art.MurkyBadgeDigit(count)), "la cifra cambia");
                        Assert.That(marks.BadgePopping, Is.True, "y arranca su pop");
                    }
                }
            }

            Assert.That(drops, Is.EqualTo(2), "la insignia bajó con cada «?»");
            for (int k = 0; k < cuia.Count; k++)
            {
                if (((mask >> k) & 1) != 0)
                {
                    Assert.That(normalAt[k], Is.GreaterThan(0), "capa " + k + ": acabó normal");
                    Assert.That(view.Liquid.DoseColorAt(k), Is.EqualTo(Palette.Tint(cuia.ColorAt(k))), "capa " + k + ": su tinte");
                }
            }

            TestContext.WriteLine("Luciérnaga del 100: la capa de arriba pasa a normal a los " + normalAt[Highest(mask)] + " ms y la de abajo a los "
                + normalAt[Lowest(mask)] + " ms.");
            Assert.That(normalAt[Highest(mask)], Is.LessThan(normalAt[Lowest(mask)]), "de arriba abajo");
            Assert.That(marks.BadgeCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Reveal_CrossfadeWithoutJumps() // D-003; ficha de Magic Sort §3.1; 07 §3 y §4.3
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            MurkyMarks marks = view.Murky;
            Cuia cuia = boot.Flow!.Session.Current[murky];
            int layer = Highest(cuia.HiddenMask);
            int tint = cuia.ColorAt(layer);
            ProceduralArt art = view.OverlayHost.Art;
            Assert.That(ProceduralArt.HasPattern(tint), Is.True, "la fruta de la capa tiene patrón (si no, no habría nada que fundir)");
            Assert.That(marks.BadgeCount, Is.EqualTo(2));
            Assert.That(view.PlayReveal(1 << layer, RevealStyle.Rule, 1f), Is.EqualTo(MurkyMarks.RevealRuleMs).Within(1e-3f));

            // En fracciones del revelado (t), lo que el shader anima en el avance del Dissolve (t / RuleDissolveEndAt).
            float end = MurkyMarks.RuleDissolveEndAt;
            float hatchFrom = LiquidGraphic.HatchFadeFrom * end;
            float hatchGone = LiquidGraphic.HatchFadeTo * end;
            float patternFrom = LiquidGraphic.FruitPatternFrom * end;
            Assert.That(hatchFrom, Is.EqualTo(0.10f).Within(0.01f), "la trama empieza a irse hacia el 10 %");
            Assert.That(hatchGone, Is.EqualTo(0.30f).Within(0.01f), "y ya no está hacia el 30 %");
            Assert.That(patternFrom, Is.EqualTo(0.35f).Within(0.01f), "el patrón de la fruta entra desde el 35 %");
            Assert.That(MurkyMarks.RuleMarkGoneAt, Is.EqualTo(0.47f).Within(0.01f), "el «?» se va hacia el 47 %");

            float prevHatch = LiquidGraphic.MurkyHatchOpacity;
            float prevFruit = 0f;
            float dropAt = -1f;
            float maxScale = 1f;
            float maxScaleAt = -1f;
            UIVertex lastDissolve = default;
            UIVertex firstNormal = default;
            bool normalSeen = false;
            for (int ms = 10; marks.IsAnimating; ms += 10)
            {
                Assert.That(ms, Is.LessThanOrEqualTo(1300), "termina a los 1.200 ms");
                view.Tick(10f);
                float t = ms / MurkyMarks.RevealRuleMs;
                string at = "t = " + ms + " ms (" + (100f * t).ToString("F1") + " %)";
                LiquidGraphic.MurkyLook look = view.Liquid.MurkyLookAt(layer);
                float progress = view.Liquid.MurkyProgressAt(layer);
                float hatch = LiquidGraphic.MurkyHatchAmount(look, progress);
                float fruit = LiquidGraphic.FruitPatternWeight(look, progress);
                float mark = LiquidGraphic.MurkyMarkOpacity(look, progress);

                // La trama baja y el patrón de la fruta sube sin saltos, uno después del otro (nunca los dos a la vez).
                Assert.That(look, Is.EqualTo(t < end ? LiquidGraphic.MurkyLook.Dissolve : LiquidGraphic.MurkyLook.None), at);
                Assert.That(hatch, Is.LessThanOrEqualTo(prevHatch + 1e-6f), at + ": la trama solo baja");
                Assert.That(prevHatch - hatch, Is.LessThan(0.03f), at + ": y sin saltos");
                Assert.That(fruit, Is.GreaterThanOrEqualTo(prevFruit - 1e-6f), at + ": el patrón de la fruta solo sube");
                Assert.That(fruit - prevFruit, Is.LessThan(0.1f), at + ": y sin saltos (también al pasar a la dosis normal)");
                Assert.That(hatch > 0f && fruit > 0f, Is.False, at + ": nunca la trama y el patrón de la fruta a la vez");
                if (t <= hatchFrom - 0.005f)
                {
                    Assert.That(hatch, Is.EqualTo(LiquidGraphic.MurkyHatchOpacity), at + ": trama entera al empezar");
                }

                if (t >= hatchGone + 0.005f)
                {
                    Assert.That(hatch, Is.EqualTo(0f), at + ": sin trama sobre el color nuevo");
                }

                if (t <= patternFrom - 0.005f)
                {
                    Assert.That(fruit, Is.EqualTo(0f), at + ": el patrón de la fruta aún no entra");
                }

                // La insignia baja justo cuando el «?» termina de irse (no antes ni al 55 %), con el pop de la cifra.
                if (mark > 0.01f)
                {
                    Assert.That(marks.BadgeCount, Is.EqualTo(2), at + ": con el «?» a la vista, sigue contado");
                }

                if (t >= MurkyMarks.RuleMarkGoneAt)
                {
                    Assert.That(mark, Is.LessThan(1e-3f), at + ": el «?» ya se fue");
                    Assert.That(marks.BadgeCount, Is.EqualTo(1), at + ": «?×1» en cuanto se va el «?»");
                    Assert.That(marks.MarkVisible(layer), Is.False, at);
                }

                float scale = marks.BadgeDigit.rectTransform.localScale.x;
                if (dropAt < 0f && marks.BadgeCount == 1)
                {
                    dropAt = ms;
                    Assert.That(marks.BadgeDigit.sprite, Is.SameAs(art.MurkyBadgeDigit(1)), at + ": la cifra cambia");
                    Assert.That(marks.BadgePopping, Is.True, at + ": y arranca su pop");
                }

                if (dropAt < 0f)
                {
                    Assert.That(scale, Is.EqualTo(1f), at + ": sin pop antes de bajar");
                }
                else if (ms - dropAt > MurkyMarks.BadgePopMs)
                {
                    Assert.That(scale, Is.EqualTo(1f), at + ": el pop dura 160 ms");
                    Assert.That(marks.BadgePopping, Is.False, at);
                }

                if (scale > maxScale)
                {
                    maxScale = scale;
                    maxScaleAt = ms;
                }

                // Malla: la última del Dissolve ya es la de la dosis normal (fila y marfil de su fruta, franja de su tinte).
                if (look == LiquidGraphic.MurkyLook.Dissolve)
                {
                    List<UIVertex> band = BandVertices(view, layer, true);
                    Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), at + ": la banda que se disuelve tiene malla");
                    lastDissolve = band[0];
                }
                else if (!normalSeen)
                {
                    normalSeen = true;
                    List<UIVertex> band = BandVertices(view, layer);
                    Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), at + ": la dosis normal tiene malla");
                    firstNormal = band[0];
                }

                prevHatch = hatch;
                prevFruit = fruit;
            }

            Assert.That(dropAt, Is.GreaterThan(0f), "la insignia bajó");
            TestContext.WriteLine("Revelado por la regla: «?×1» a los " + dropAt + " ms; pop hasta " + maxScale.ToString("F3") + " a los " + maxScaleAt + " ms.");
            Assert.That(maxScale, Is.InRange(MurkyMarks.BadgePopScale - 0.03f, MurkyMarks.BadgePopScale + 1e-4f), "pop de la cifra: 1 → 1,2 → 1");
            Assert.That(maxScaleAt - dropAt, Is.InRange(10f, 100f), "con ease-out: el pico llega pronto");
            Assert.That(marks.BadgeCount, Is.EqualTo(1));
            Assert.That(normalSeen, Is.True);
            // Nivel 100, con Patrones de fruta: la fila lleva además la marca de su icono, que entra con su patrón (D-003).
            Assert.That(view.FruitIconsOn, Is.True, "nivel con Patrones de fruta");
            Assert.That(lastDissolve.uv3.w, Is.EqualTo(LiquidGraphic.RevealRowCode(tint, true)), "al disolverse, la fila de su fruta (+ 0,5 si sus marcas van en marfil) y la marca de su icono");
            float code = lastDissolve.uv3.w - LiquidGraphic.RevealIconFlag; // lo que lee el shader tras quitar la marca
            Assert.That(code, Is.EqualTo(LiquidGraphic.FruitRowCode(tint)), "la fila de su fruta");
            Assert.That(Mathf.Floor(code + 0.25f), Is.EqualTo((float)tint), "el shader decodifica su fila");
            Assert.That(2f * (code - tint), Is.EqualTo(Palette.PatternBase(tint) == Palette.UiPaper ? 1f : 0f), "y el color de sus marcas");
            Assert.That(firstNormal.uv3.w, Is.EqualTo((float)tint), "la dosis normal, con la fila de su fruta");
            Vector4 a = lastDissolve.uv1;
            Vector4 b = firstNormal.uv1;
            Assert.That(Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z), Is.LessThan(0.02f), "la franja de su tinte ya entró al final del Dissolve");
        }

        [UnityTest]
        public IEnumerator Reveal_SnapMidway_NoLateEffects([Values(0.05f, 0.5f)] float fraction) // 02 §14: Marea a mitad
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, b, a) => ev.RevealedMask != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            CuiaView source = boot.View!.Views[pour.Source];
            MurkyMarks marks = source.Murky;
            BoardAnimator animator = boot.View.Animator;
            MechanicTestKit.Tap(boot, pour);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Reveal, pour.Source, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Reveal, pour.Source, fraction);
            Assert.That(marks.IsAnimating, Is.True, "revelado en curso al " + (100f * fraction) + " %");
            int sparkles = marks.LastSparkles;
            int bubbles = marks.LastBubbles;
            if (fraction < MurkyMarks.RuleSparklesAt)
            {
                Assert.That(sparkles, Is.EqualTo(0), "aún sin destellos antes del corte");
            }
            else
            {
                Assert.That(sparkles, Is.InRange(3, 4), "los destellos, antes del corte");
            }

            Assert.That(bubbles, Is.EqualTo(fraction < MurkyMarks.RuleBubbleAt ? 0 : 1), "burbujas pedidas antes del corte");

            animator.SnapAll();
            Assert.That(marks.IsAnimating, Is.False, "SnapAll termina el revelado");
            Assert.That(animator.Steps.ActiveCount, Is.EqualTo(0), "sin pasos en curso");
            Assert.That(marks.LastSparkles, Is.EqualTo(sparkles), "sin destellos al saltar");
            Assert.That(marks.LastBubbles, Is.EqualTo(bubbles), "sin burbuja al saltar");
            Assert.That(marks.BadgePopping, Is.False, "sin pop al saltar");
            Assert.That(marks.BadgeDigit.rectTransform.localScale.x, Is.EqualTo(1f));
            Assert.That(marks.BadgeCount, Is.EqualTo(1), "«?×1»");
            Assert.That(marks.BadgeDigit.sprite, Is.SameAs(source.OverlayHost.Art.MurkyBadgeDigit(1)));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Cuia after = boot.Flow!.Session.Current[pour.Source];
            int layer = after.Count - 1;
            Assert.That(source.Liquid.MurkyLookAt(layer), Is.EqualTo(LiquidGraphic.MurkyLook.None), "revelada: su tinte");
            Assert.That(source.DoseColor(layer), Is.EqualTo(Palette.Tint(after.ColorAt(layer))));
            Assert.That(source.Murky.MarkVisible(layer), Is.False);

            // Después, nada llega tarde: ni destellos, ni burbujas, ni pasos.
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(marks.LastSparkles, Is.EqualTo(sparkles), "ningún destello tras el Snap");
            Assert.That(marks.LastBubbles, Is.EqualTo(bubbles), "ninguna burbuja tras el Snap");
            Assert.That(marks.IsAnimating, Is.False);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(marks.BadgeCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Reveal_ReduceMotion_NoBadgePop() // 02 §12: Reducir movimiento, sin sacudidas ni partículas (hallazgo 1)
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            MurkyMarks marks = view.Murky;
            FxLayer fx = boot.View.Fx;
            Cuia cuia = boot.Flow!.Session.Current[murky];
            try
            {
                fx.ReduceMotion = true;
                for (int i = 0; i < 400 && fx.ActiveCount > 0; i++)
                {
                    fx.Tick(PlayModeHelpers.FrameMs);
                }

                Assert.That(fx.ActiveCount, Is.EqualTo(0), "sin partículas antes de empezar");
                Assert.That(marks.BadgeCount, Is.EqualTo(2));
                Assert.That(view.PlayReveal(1 << Highest(cuia.HiddenMask), RevealStyle.Rule, 1f), Is.GreaterThan(0f));
                int ms = 0;
                while (marks.IsAnimating)
                {
                    Assert.That(ms, Is.LessThanOrEqualTo(1300), "termina a los 1.200 ms");
                    view.Tick(10f);
                    fx.Tick(10f);
                    ms += 10;
                    string at = "t = " + ms + " ms";
                    Assert.That(marks.BadgePopping, Is.False, at + ": sin pop de la cifra");
                    Assert.That(marks.BadgeDigit.rectTransform.localScale.x, Is.EqualTo(1f), at + ": la cifra no se mueve");
                    Assert.That(fx.ActiveCount, Is.EqualTo(0), at + ": sin destellos ni burbuja");
                }

                Assert.That(marks.BadgeCount, Is.EqualTo(1), "al terminar, «?×1»");
                Assert.That(marks.BadgeDigit.sprite, Is.SameAs(view.OverlayHost.Art.MurkyBadgeDigit(1)));
            }
            finally
            {
                fx.ReduceMotion = false;
            }
        }

        [UnityTest]
        public IEnumerator Reveal_NoAlloc() // 08 §5.4; arquitectura del paso 1 §12
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            int murky = MurkyCuia(boot);
            CuiaView view = boot.View!.Views[murky];
            FxLayer fx = boot.View.Fx;
            Cuia cuia = boot.Flow!.Session.Current[murky];
            int top = 1 << Highest(cuia.HiddenMask);
            Assert.That(CountBits(cuia.HiddenMask), Is.EqualTo(2));

            // Pasada 0, calentamiento (primer uso de cada sprite, de cada gráfico y de la malla turbia); pasada 1, medida: el
            // revelado por la regla de la de arriba (destellos, burbuja, la cifra que cambia de sprite con su pop, fundido y
            // final) y la Luciérnaga de las 2 (frente, iconos con su disco), cada fotograma con la vista, los efectos y la
            // reconstrucción del lienzo (OnPopulateMesh de la malla turbia).
            for (int pass = 0; pass < 2; pass++)
            {
                bool measure = pass == 1;
                RevealFrames(view, fx, cuia, top, RevealStyle.Rule, 80, measure);
                Assert.That(view.Murky.LastSparkles, Is.EqualTo(MurkyMarks.SparklesPerLayer), "pasada " + pass + ": los destellos");
                Assert.That(view.Murky.LastBubbles, Is.EqualTo(1), "pasada " + pass + ": la burbuja");
                Assert.That(view.Murky.BadgeCount, Is.EqualTo(1), "pasada " + pass + ": «?×1»");
                RevealFrames(view, fx, cuia, cuia.HiddenMask, RevealStyle.Firefly, 40, measure);
                Assert.That(view.Murky.BadgeCount, Is.EqualTo(0), "pasada " + pass + ": la Luciérnaga las revela todas");
            }

            view.ShowContent(cuia, false);
        }

        [UnityTest]
        public IEnumerator Murky_UndoKeepsRevealed()
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, b, a) => ev.RevealedMask != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            MechanicTestKit.Tap(boot, pour);
            MechanicTestKit.StepToIdle(boot, 6000);
            Cuia revealed = boot.Flow!.Session.Current[pour.Source];
            int layer = revealed.Count - 1;
            int tint = revealed.ColorAt(layer);
            CuiaView source = boot.View!.Views[pour.Source];
            Assert.That(source.Murky.BadgeCount, Is.EqualTo(1));

            // Marea (04 §2.1; I5): el vertido vuelve, la capa revelada no se vuelve a ocultar.
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Cuia undone = boot.Flow.Session.Current[pour.Source];
            Assert.That(undone.IsHiddenAt(layer), Is.False, "I5: revelada para siempre");
            Assert.That(source.Liquid.MurkyLookAt(layer), Is.EqualTo(LiquidGraphic.MurkyLook.None), "Marea no vuelve a enturbiar");
            Assert.That(source.DoseColor(layer), Is.EqualTo(Palette.Tint(tint)));
            Assert.That(source.Murky.BadgeCount, Is.EqualTo(1), "«?×1»");

            // Volver a la orilla (intro de nuevo): la capa sigue revelada.
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla");
            boot.TapAnywhere();
            for (int t = 0; t < 6000 && (boot.Flow.Phase != FlowPhase.Playing || !boot.View.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (boot.Flow.Phase == FlowPhase.Intro)
                {
                    boot.TapAnywhere();
                }
            }

            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.Flow.Session.Current[pour.Source].IsHiddenAt(layer), Is.False, "I5 tras Volver a la orilla");
            Assert.That(source.Liquid.MurkyLookAt(layer), Is.EqualTo(LiquidGraphic.MurkyLook.None), "la intro no la vuelve a enturbiar");
            Assert.That(source.Murky.BadgeCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Intro_HiddenAppearAtFirstImpact()
        {
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen screen = boot.View!;
            int murky = MurkyCuia(boot);
            Cuia cuia = boot.Flow!.Session.Current[murky];
            CuiaView view = screen.Views[murky];
            screen.DebugReplayIntro(1400, false);
            IntroAnimation intro = screen.Intro;
            float impact = intro.Plan.FirstImpactMs;
            Assert.That(impact, Is.GreaterThan(0f), "hay un primer impacto");

            intro.Seek(impact - 20f);
            for (int k = 0; k < cuia.Count; k++)
            {
                if (cuia.IsHiddenAt(k))
                {
                    Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None), "antes del primer impacto, agua del río");
                    Assert.That(view.Murky.MarkVisible(k), Is.False);
                }
            }

            Assert.That(view.Murky.BadgeVisible, Is.False, "sin insignia antes del primer impacto");
            intro.Seek(impact);
            int seen = 0;
            for (int k = 0; k < cuia.Count; k++)
            {
                if (cuia.IsHiddenAt(k))
                {
                    seen++;
                    Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden), "dosis " + k + ": en el primer impacto, turbia");
                    Assert.That(view.DoseColor(k), Is.EqualTo(Palette.Murky), "sin fruta: pardo");
                }
            }

            Assert.That(seen, Is.EqualTo(2), "todas a la vez, en el mismo fotograma");
            Assert.That(view.Murky.BadgeVisible, Is.True, "la insignia sale con ellas");
            Assert.That(view.Murky.BadgeCount, Is.EqualTo(2));
            for (int k = 0; k < cuia.Count; k++)
            {
                if (cuia.IsHiddenAt(k))
                {
                    Assert.That(intro.Plan.RunOf(murky, k), Is.LessThan(0), "dosis " + k + ": las ocultas no reciben fruta");
                }
            }

            intro.Seek(1400f);
            Assert.That(view.Liquid.MurkyLookAt(Lowest(cuia.HiddenMask)), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden), "al final, turbias");
        }

        [UnityTest]
        public IEnumerator Intro_ReduceMotion_HiddenFadeInMurky() // 02 §12; 07 §3 (fila Cacao): nunca el pardo sin «?» (hallazgo 1)
        {
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen screen = boot.View!;
            int murky = MurkyCuia(boot);
            Cuia cuia = boot.Flow!.Session.Current[murky];
            CuiaView view = screen.Views[murky];
            try
            {
                boot.Flow.ReduceMotion = true;
                screen.DebugReplayIntro(GameConfig.ReducedMotionIntroMs, false);
                IntroAnimation intro = screen.Intro;
                Assert.That(intro.ReduceMotion, Is.True, "la intro va en fundido");

                // A mitad del fundido (≈ 100 de 250 ms): las ocultas se funden ya turbias, con su trama y su «?».
                intro.Seek(100f);
                int seen = 0;
                for (int k = 0; k < cuia.Count; k++)
                {
                    if (!cuia.IsHiddenAt(k))
                    {
                        continue;
                    }

                    seen++;
                    float alpha = view.Liquid.DoseColorAt(k).a;
                    Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden), "dosis " + k + ": turbia durante el fundido");
                    Assert.That(alpha, Is.GreaterThan(0f).And.LessThan(1f), "dosis " + k + ": a medio fundir");
                    Assert.That(view.Murky.MarkVisible(k), Is.True, "dosis " + k + ": con su «?» (el pardo nunca va sin él)");
                }

                Assert.That(seen, Is.EqualTo(2));
                Assert.That(view.Murky.BadgeVisible, Is.False, "la insignia sale al final");

                // Al final del fundido, la insignia con las dos.
                intro.Seek(GameConfig.ReducedMotionIntroMs);
                Assert.That(view.Murky.BadgeVisible, Is.True, "al final, «?×2»");
                Assert.That(view.Murky.BadgeCount, Is.EqualTo(2));
                for (int k = 0; k < cuia.Count; k++)
                {
                    if (cuia.IsHiddenAt(k))
                    {
                        Assert.That(view.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.Hidden));
                        Assert.That(view.Liquid.DoseColorAt(k), Is.EqualTo(Palette.Murky), "opaca");
                        Assert.That(view.Murky.MarkVisible(k), Is.True);
                    }
                }
            }
            finally
            {
                boot.Flow.ReduceMotion = false;
            }
        }

        [UnityTest]
        public IEnumerator IntroTick_Murky100_AllocatesNothing() // A10 en un nivel con Aguas turbias (hallazgo 4)
        {
            yield return PlayModeHelpers.LoadGame(100, new MemoryProgressStore(100, 99));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen screen = boot.View!;
            int murky = MurkyCuia(boot);
            CuiaView view = screen.Views[murky];
            IntroAnimation intro = screen.Intro;

            // Calentamiento: una pasada entera (primer uso de cada gráfico; también la insignia «?×2» en el primer impacto).
            screen.DebugReplayIntro(1400, false);
            for (int t = 0; intro.Active && t < 4000; t += 16)
            {
                intro.Tick(16f);
            }

            screen.DebugReplayIntro(1400, false);
            float impact = intro.Plan.FirstImpactMs;
            Assert.That(impact, Is.GreaterThan(0f), "hay un primer impacto");
            int frames = 0;
            int nearImpact = 0;
            bool badgeBefore = view.Murky.BadgeVisible;
            for (int t = 0; intro.Active && t < 4000; t += 16)
            {
                TestDelegate step = () => intro.Tick(16f);
                Assert.That(step, Is.Not.AllocatingGCMemory(), "t = " + t + " ms (primer impacto a los " + impact.ToString("F0") + ")");
                frames++;
                if (Mathf.Abs(t - impact) <= 48f)
                {
                    nearImpact++;
                }
            }

            Assert.That(badgeBefore, Is.False, "sin insignia al arrancar");
            Assert.That(nearImpact, Is.GreaterThanOrEqualTo(3), "fotogramas medidos alrededor del primer impacto (la insignia sale ahí)");
            Assert.That(frames, Is.GreaterThan(10), "fotogramas medidos");
            Assert.That(view.Murky.BadgeVisible, Is.True, "al final, «?×2»");
            Assert.That(view.Murky.BadgeCount, Is.EqualTo(2));
        }

        // Comprueba en la malla que la banda turbia k de la cuia inclinada tiltDeg grados lleva el «?» derecho en pantalla: su
        // x (uv1.w) es la x de pantalla e = X · cos θ − y · sen θ en u, desde el centro del «?» (pendiente 1, cero dentro de la
        // banda), y la banda abarca la ventana del «?» (medio alto del «?» a cada lado).
        private static void AssertMurkyBandUpright(CuiaView view, int k, float tiltDeg, float glyphU, string where)
        {
            List<UIVertex> band = BandVertices(view, k, true);
            Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), where + ": la banda turbia tiene malla");
            float rad = tiltDeg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            int a = 0;
            int b = 0;
            float eMin = float.MaxValue;
            float eMax = float.MinValue;
            float gMin = float.MaxValue;
            float gMax = float.MinValue;
            for (int i = 0; i < band.Count; i++)
            {
                float e = E(band[i], cs, sn);
                if (e < eMin)
                {
                    eMin = e;
                    a = i;
                }

                if (e > eMax)
                {
                    eMax = e;
                    b = i;
                }

                gMin = Mathf.Min(gMin, band[i].uv1.w);
                gMax = Mathf.Max(gMax, band[i].uv1.w);
                Assert.That(band[i].uv3.w, Is.EqualTo((float)ProceduralArt.MurkyPatternRow), where + ": fila turbia");
            }

            Assert.That(eMax - eMin, Is.GreaterThan(0.1f), where + ": la banda tiene ancho en pantalla");
            float slope = (band[b].uv1.w - band[a].uv1.w) / (eMax - eMin);
            Assert.That(slope, Is.EqualTo(1f).Within(1e-3f), where + ": la x del «?» va en u de pantalla (isótropo)");
            for (int i = 0; i < band.Count; i++)
            {
                float expected = band[a].uv1.w + (E(band[i], cs, sn) - eMin);
                Assert.That(band[i].uv1.w, Is.EqualTo(expected).Within(1e-3f), where + ": la x del «?» solo depende de la x de pantalla (derecho)");
            }

            float half = 0.5f * Mathf.Max(glyphU, LiquidGraphic.GlyphMinU); // la ventana del «?» (0,25 de la fila con 0,5 u)
            Assert.That(gMin, Is.LessThanOrEqualTo(-half), where + ": la banda abarca el «?» por la izquierda");
            Assert.That(gMax, Is.GreaterThanOrEqualTo(half), where + ": y por la derecha");
        }

        // El «?» de alto sizeU centrado en (cx, cy) (coordenadas de la cuia) con la cuia inclinada tiltDeg: las esquinas de su
        // caja en pantalla (alto × 0,7 de ancho, con su halo) quedan dentro de la silueta interior, así que el punto del «?» no
        // toca el vidrio.
        private static void AssertGlyphInsideGlass(CuiaView view, float cx, float cy, float sizeU, float tiltDeg, string where)
        {
            float rad = tiltDeg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            float e0 = (cx * cs) - (cy * sn);
            float d0 = (cx * sn) + (cy * cs);
            int capacity = view.Capacity;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    float e = e0 + (sx * 0.35f * sizeU);
                    float d = d0 + (sy * 0.5f * sizeU);
                    float x = (e * cs) + (d * sn);
                    float y = (d * cs) - (e * sn);
                    float floor = CuiaGeometry.CavityFloorU(x);
                    float wall = CuiaGeometry.InnerHalfWidth(Mathf.Max(y, CuiaGeometry.CavityBottomU), capacity);
                    Assert.That(y, Is.GreaterThanOrEqualTo(floor - 0.02f), where + ": la esquina (" + sx + ", " + sy + ") del «?» no baja del fondo de vidrio");
                    Assert.That(Mathf.Abs(x), Is.LessThanOrEqualTo(wall + 0.02f), where + ": ni sale por la pared");
                }
            }
        }

        private static float E(UIVertex v, float cs, float sn)
        {
            return ((v.uv0.x - 1f) * cs) - (v.uv0.y * sn);
        }

        // Vértices de la banda k (como la decodifica el shader: floor((uv2.w + 1,5) / 4)); con murkyOnly, solo los de patrón
        // turbio (la lengua del chorro también cae en la dosis 0, con el patrón −1).
        private static List<UIVertex> BandVertices(CuiaView view, int k, bool murkyOnly = false)
        {
            var list = new List<UIVertex>();
            using (var vh = new VertexHelper())
            {
                PopulateMesh.Invoke(view.Liquid, new object[] { vh });
                var v = new UIVertex();
                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref v, i);
                    if (Mathf.FloorToInt((v.uv2.w + 1.5f) / 4f) == k && (!murkyOnly || v.uv2.w - (4f * k) > 1.5f))
                    {
                        list.Add(v);
                    }
                }
            }

            return list;
        }

        // Fila del atlas (uv3.w) y patrón (uv2.w − 4 · dosis) de cada banda; NaN si la dosis no tiene banda.
        private static void MeshBands(CuiaView view, float[] rows, float[] patterns)
        {
            for (int k = 0; k < rows.Length; k++)
            {
                List<UIVertex> band = BandVertices(view, k);
                rows[k] = band.Count > 0 ? band[0].uv3.w : float.NaN;
                patterns[k] = band.Count > 0 ? band[0].uv2.w - (4f * k) : float.NaN;
            }
        }

        // Luciérnaga con factor speed: dura exactamente durationMs (600 ms × factor) y la capa de abajo sigue revelándose (Turn)
        // hasta el final. Con el espejo del frente (LiquidGraphic.PaintedColorAt), de arriba abajo entre capas (la de arriba se
        // aclara antes) y dentro de cada capa (su parte de arriba antes que la de abajo); con el icono de la fruta y su disco.
        private static void RunFirefly(CuiaView view, Cuia cuia, int mask, float speed, int durationMs)
        {
            int topLayer = Highest(mask);
            int bottomLayer = Lowest(mask);
            float ms = view.PlayReveal(mask, RevealStyle.Firefly, speed);
            Assert.That(ms, Is.EqualTo((float)durationMs).Within(1e-3f), "600 ms × " + speed);
            Assert.That(view.Murky.IsAnimating, Is.True);
            Assert.That(BandLight(view, bottomLayer, cuia.ColorAt(bottomLayer)), Is.EqualTo(0f).Within(1e-4f), "empieza parda");
            bool topFirst = false;
            bool fruitSeen = false;
            ProceduralArt art = view.OverlayHost.Art;
            for (int t = 10; t < durationMs; t += 10)
            {
                view.Tick(10f);
                string at = "factor " + speed + ", t = " + t + " ms";
                Assert.That(view.Murky.IsAnimating, Is.True, at + ": sigue en curso (600 ms × factor)");
                Assert.That(view.Liquid.MurkyLookAt(bottomLayer), Is.EqualTo(LiquidGraphic.MurkyLook.Turn), at + ": la de abajo sigue revelándose");
                float top = BandLight(view, topLayer, cuia.ColorAt(topLayer));
                float bottom = BandLight(view, bottomLayer, cuia.ColorAt(bottomLayer));
                Assert.That(top, Is.GreaterThanOrEqualTo(bottom - 1e-4f), at + ": de arriba abajo");
                topFirst |= top > bottom + 0.05f;
                for (int k = 0; k < cuia.Count; k++)
                {
                    if (((mask >> k) & 1) != 0)
                    {
                        int tint = cuia.ColorAt(k);
                        Assert.That(LightAt(view, k, tint, 0.2f), Is.GreaterThanOrEqualTo(LightAt(view, k, tint, 0.8f) - 1e-4f), at + ", capa " + k + ": dentro de la capa, de arriba abajo");
                    }
                }

                if (view.Murky.FruitVisible(topLayer))
                {
                    fruitSeen = true;
                    Assert.That(view.Murky.FruitImage(topLayer).sprite, Is.SameAs(art.FruitFlight(cuia.ColorAt(topLayer))), "el icono de su fruta");
                    Assert.That(view.Murky.FruitDisc(topLayer).gameObject.activeSelf, Is.True, "sobre su disco");
                    Assert.That(view.Murky.FruitDisc(topLayer).sprite, Is.SameAs(art.MurkyFruitDisc));
                }
            }

            view.Tick(10f);
            Assert.That(view.Murky.IsAnimating, Is.False, "factor " + speed + ": termina justo a los " + durationMs + " ms");
            Assert.That(topFirst, Is.True, "la de arriba se aclara antes");
            Assert.That(fruitSeen, Is.True, "cada «?» gira y muestra su fruta");
            Assert.That(view.Murky.FruitDisc(topLayer).gameObject.activeSelf || view.Murky.FruitDisc(bottomLayer).gameObject.activeSelf, Is.False, "sin discos al terminar");
        }

        // Vuelve a pintar turbias las ocultas de cuia y revela mask con style: frames fotogramas de 16 ms con la vista, los
        // efectos y la reconstrucción del lienzo (la malla turbia, la cifra y los iconos). Con measure, todo, desde PlayReveal,
        // dentro de Is.Not.AllocatingGCMemory. Comprueba que el revelado terminó y que pasó por donde se quiere medir.
        private static void RevealFrames(CuiaView view, FxLayer fx, Cuia cuia, int mask, RevealStyle style, int frames, bool measure)
        {
            view.ShowContent(cuia, false);
            for (int i = 0; i < 400 && fx.ActiveCount > 0; i++)
            {
                fx.Tick(PlayModeHelpers.FrameMs); // se apagan los efectos de la pasada anterior
            }

            Canvas.ForceUpdateCanvases();
            MurkyMarks marks = view.Murky;
            Assert.That(marks.BadgeCount, Is.EqualTo(CountBits(cuia.HiddenMask)), "de nuevo turbias");
            int maxAdditive = 0;
            float maxScale = 1f;
            bool fruitSeen = false;
            string what = (style == RevealStyle.Rule ? "por la regla" : "Luciérnaga") + (measure ? " (medida)" : " (calentamiento)");
            TestDelegate run = () =>
            {
                view.PlayReveal(mask, style, 1f);
                for (int f = 0; f < frames; f++)
                {
                    view.Tick(PlayModeHelpers.FrameMs);
                    fx.Tick(PlayModeHelpers.FrameMs);
                    Canvas.ForceUpdateCanvases();
                    maxAdditive = Mathf.Max(maxAdditive, fx.AdditiveCount);
                    maxScale = Mathf.Max(maxScale, marks.BadgeDigit.rectTransform.localScale.x);
                    for (int k = 0; k < cuia.Count; k++)
                    {
                        fruitSeen |= marks.FruitVisible(k);
                    }
                }
            };

            if (measure)
            {
                Assert.That(run, Is.Not.AllocatingGCMemory(), what + ": " + frames + " fotogramas sin asignaciones");
            }
            else
            {
                run();
            }

            Assert.That(marks.IsAnimating, Is.False, what + ": terminó dentro de los fotogramas");
            Assert.That(maxScale, Is.GreaterThan(1.15f), what + ": la cifra hizo su pop");
            if (style == RevealStyle.Rule)
            {
                Assert.That(maxAdditive, Is.GreaterThanOrEqualTo(3), what + ": los destellos, en la capa de efectos");
            }
            else
            {
                Assert.That(fruitSeen, Is.True, what + ": los iconos de fruta");
            }
        }

        // Cuánto se ha aclarado la dosis k hacia su tinte (0 pardo, 1 su tinte), por la distancia en RGB del color con el que
        // se pinta (sin cuantizar a 8 bits: así se ven también los últimos milisegundos de la curva).
        private static float Lighten(CuiaView view, int k, int tint)
        {
            return Progress(view.Liquid.DoseColorAt(k), tint);
        }

        // Lo mismo a la profundidad depth de la banda (0 arriba, 1 abajo), con el espejo del frente de la Luciérnaga: lo que
        // pinta el shader (LiquidGraphic.PaintedColorAt).
        private static float LightAt(CuiaView view, int k, int tint, float depth)
        {
            return Progress(view.Liquid.PaintedColorAt(k, depth), tint);
        }

        // Media de LightAt en la banda k (cinco profundidades).
        private static float BandLight(CuiaView view, int k, int tint)
        {
            float sum = 0f;
            for (int i = 0; i < Depths.Length; i++)
            {
                sum += LightAt(view, k, tint, Depths[i]);
            }

            return sum / Depths.Length;
        }

        private static float Progress(Color now, int tint)
        {
            Color from = Palette.Murky;
            Color to = Palette.Tint(tint);
            float total = Distance(from, to);
            return total > 1e-4f ? Mathf.Clamp01(1f - (Distance(now, to) / total)) : 1f;
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
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

        private static Pour[] Trigger(GameBootstrap boot, MechanicTestKit.PourWanted wanted)
        {
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, wanted);
            Assert.That(path, Is.Not.Null, "vertidos que disparan el evento en el nivel " + boot.Flow!.LevelNumber);
            return path!;
        }

        // Avanza de fotograma en fotograma hasta la vista quieta, llamando a each en cada uno; devuelve los ms.
        private static int StepWatching(GameBootstrap boot, System.Action each)
        {
            int t = 0;
            while (!boot.View!.IsViewIdle)
            {
                Assert.That(t, Is.LessThan(6000), "la vista no se quedó quieta");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                each();
            }

            return t;
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

        private static int CountBits(int mask)
        {
            int n = 0;
            for (int m = mask; m != 0; m &= m - 1)
            {
                n++;
            }

            return n;
        }
    }
}
