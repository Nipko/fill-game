#nullable enable
using System.Collections;
using System.Text.RegularExpressions;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Patrones de fruta en el juego (T-E de la prueba del 2026-10-09; 07 §3, reglas 1, 2 y 5; 02 §12; 08 §9): al cargar el 40,
    /// el patrón al 15 % y sin icono; desde el 41, el 20 % en la propiedad <c>_PatternOpacity</c> del material y el icono de
    /// fruta en cada dosis visible, nunca en las ocultas (100), en el Barro (123) ni en el agua base; en el 7, el icono solo con
    /// la cuia levantada (selección o touch-down; regla 2) y sin tocar las demás (D-002); el icono sigue a la banda en el
    /// vertido y no asigna memoria (08 §5.4); la cuia añadida lo recibe; y <c>AF_STATE load</c> lleva
    /// <c>"patterns":{"on":bool,"opacity":x}</c>. El reloj lo lleva el test.
    /// </summary>
    public sealed class FruitPatternsPlayTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level40_Subtle_Level41_PatternsOn_AndAfStateLoad() // T-E.1, T-E.6 y T-E.7
        {
            LogAssert.Expect(LogType.Log, new Regex(@"^AF_STATE \{""event"":""load"",.*""level"":40,.*,""patterns"":\{""on"":false,""opacity"":0\.15\}\}$"));
            yield return MechanicTestKit.LoadPlaying(40);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(0.15f).Within(1e-6f), "el 40: _PatternOpacity = 0,15");
            Assert.That(Palette.PatternOpacity, Is.EqualTo(0.15f).Within(1e-6f));
            Assert.That(boot.View!.Animator.FruitPatterns, Is.False);
            AssertIcons(boot, false, "nivel 40");

            LogAssert.Expect(LogType.Log, new Regex(@"^AF_STATE \{""event"":""load"",.*""level"":41,.*,""patterns"":\{""on"":true,""opacity"":0\.20\}\}$"));
            yield return MechanicTestKit.LoadPlaying(41);
            boot = GameBootstrap.Current!;
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(0.20f).Within(1e-6f), "el 41: _PatternOpacity = 0,20");
            Assert.That(Palette.PatternOpacity, Is.EqualTo(0.20f).Within(1e-6f), "y PatternMark (el bocado de la Piraña), igual");
            Assert.That(boot.View!.Animator.FruitPatterns, Is.True);
            AssertIcons(boot, true, "nivel 41");
        }

        [UnityTest]
        public IEnumerator Level63_IconOnEachVisibleDose_AddedCuiaToo() // T-E.4
        {
            FunnelSetup.Hooks();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return MechanicTestKit.LoadPlaying(63);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(LiquidGraphic.ShaderPatternOpacity, Is.EqualTo(0.20f).Within(1e-6f));
            int drawn = AssertIcons(boot, true, "nivel 63");
            Assert.That(drawn, Is.GreaterThan(10), "iconos a la vista en el 63");

            int cuias = boot.View!.Views.Length;
            Assert.That(boot.PressExtraCuia(), Is.True, "Cuia extra con semillas");
            PlayModeHelpers.Step(boot, 700);
            CuiaView added = boot.View.Views[cuias];
            Assert.That(added.FruitPatterns && added.FruitIconsOn, Is.True, "la cuia añadida, con los Patrones de fruta del nivel");
        }

        [UnityTest]
        public IEnumerator Level7_IconOnlyWhileLifted() // T-E.5; 07 §3, regla 2; D-002
        {
            yield return MechanicTestKit.LoadPlaying(7);
            GameBootstrap boot = GameBootstrap.Current!;
            CuiaView[] views = boot.View!.Views;
            AssertIcons(boot, false, "nivel 7 en reposo");
            BoardState state = boot.Flow!.Session.Current;
            int source = -1;
            for (int i = 0; i < state.Count && source < 0; i++)
            {
                if (PourRules.CanBeSource(state, i))
                {
                    source = i;
                }
            }

            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 200);
            for (int i = 0; i < views.Length; i++)
            {
                Assert.That(views[i].FruitIconsOn, Is.EqualTo(i == source), "cuia " + i + ": icono solo en la levantada");
            }

            for (int k = 0; k < state[source].Count; k++)
            {
                Assert.That(views[source].Liquid.FruitIconDrawn(k), Is.True, "dosis " + k + " del origen, con su icono");
            }

            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Deselected));
            PlayModeHelpers.Step(boot, 200);
            AssertIcons(boot, false, "al bajarla");

            // Touch-down (02 §3.1): sube y muestra el icono ya en el toque; soltar fuera lo cancela y el icono se va.
            var press = new PointerEventData(EventSystem.current) { position = new Vector2(-1000f, -1000f) };
            views[source].OnPointerDown(press);
            Assert.That(views[source].IsLifted && views[source].FruitIconsOn, Is.True, "touch-down: elevada y con iconos");
            views[source].OnPointerUp(press);
            Assert.That(views[source].IsLifted || views[source].FruitIconsOn, Is.False, "soltar fuera: abajo y sin iconos");
        }

        [UnityTest]
        public IEnumerator Level100_MurkyWithoutIcon_RevealShowsIt() // T-E.4: nunca en las ocultas; al revelarse, sin chocar con MurkyMarks
        {
            yield return MechanicTestKit.LoadPlaying(100);
            GameBootstrap boot = GameBootstrap.Current!;
            AssertIcons(boot, true, "nivel 100");
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.RevealedMask != 0);
            Assert.That(path, Is.Not.Null, "un vertido que revela");
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            CuiaView source = boot.View!.Views[pour.Source];
            MechanicTestKit.Tap(boot, pour);
            for (int t = 0; t < 8000 && !boot.View.IsViewIdle; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                for (int k = 0; k < source.Capacity; k++)
                {
                    if (source.Liquid.MurkyLookAt(k) != LiquidGraphic.MurkyLook.None)
                    {
                        Assert.That(source.Liquid.FruitIconDrawn(k), Is.False, "a los " + t + " ms: dosis " + k + " turbia o revelándose, sin icono");
                    }
                }
            }

            MechanicTestKit.AssertViewMatchesModel(boot);
            Cuia revealed = boot.Flow!.Session.Current[pour.Source];
            int layer = revealed.Count - 1;
            Assert.That(revealed.IsHiddenAt(layer), Is.False);
            Assert.That(source.Liquid.FruitIconDrawn(layer), Is.True, "revelada, con su icono");
            AssertIcons(boot, true, "tras revelar");

            // Luciérnaga (04 §2.5): su icono temporal (MurkyMarks) y el de Patrones de fruta nunca a la vez en la misma dosis.
            int murky = -1;
            BoardState state = boot.Flow.Session.Current;
            for (int i = 0; i < state.Count && murky < 0; i++)
            {
                if (state[i].HasHidden)
                {
                    murky = i;
                }
            }

            Assert.That(murky, Is.GreaterThanOrEqualTo(0), "queda una cuia con ocultas");
            CuiaView view = boot.View.Views[murky];
            Cuia cuia = state[murky];
            Assert.That(view.PlayReveal(cuia.HiddenMask, RevealStyle.Firefly, 1f), Is.GreaterThan(0f));
            bool tempSeen = false;
            for (int f = 0; f < 60 && view.Murky.IsAnimating; f++)
            {
                view.Tick(PlayModeHelpers.FrameMs);
                for (int k = 0; k < cuia.Count; k++)
                {
                    bool temp = view.Murky.FruitVisible(k);
                    tempSeen |= temp;
                    Assert.That(temp && view.Liquid.FruitIconDrawn(k), Is.False, "fotograma " + f + ", dosis " + k + ": un solo icono");
                }
            }

            Assert.That(tempSeen, Is.True, "se vio el icono de la Luciérnaga");
            for (int k = 0; k < cuia.Count; k++)
            {
                Assert.That(view.Liquid.FruitIconDrawn(k), Is.True, "dosis " + k + ": al terminar, el icono de Patrones de fruta");
            }

            view.ShowContent(cuia, false);
        }

        [UnityTest]
        public IEnumerator Level123_MudWithoutIcon() // T-E.4: nunca en el Barro (02 §9.1 M5)
        {
            yield return MechanicTestKit.LoadPlaying(123);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardState state = boot.Flow!.Session.Current;
            int mud = 0;
            for (int i = 0; i < state.Count; i++)
            {
                for (int k = 0; k < state[i].Count; k++)
                {
                    if (state[i].ColorAt(k) == Cuia.MudColor)
                    {
                        mud++;
                        Assert.That(boot.View!.Views[i].Liquid.FruitIconDrawn(k), Is.False, "cuia " + i + ", dosis " + k + ": el Barro sin icono");
                    }
                }
            }

            Assert.That(mud, Is.GreaterThan(0), "el 123 tiene Barro");
            AssertIcons(boot, true, "nivel 123");
        }

        [UnityTest]
        public IEnumerator Level63_IconsFollowThePour_NoAlloc() // T-E.4 y 08 §5.4
        {
            yield return MechanicTestKit.LoadPlaying(63);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen screen = boot.View!;
            BoardState state = boot.Flow!.Session.Current;
            int source = -1;
            int target = -1;
            for (int pass = 0; pass < 2 && source < 0; pass++)
            {
                // Mejor a una cuia con líquido (el icono de la dosis de abajo sigue a la vista); si no hay, a una vacía.
                for (int a = 0; a < state.Count && source < 0; a++)
                {
                    for (int b = 0; b < state.Count; b++)
                    {
                        if (a != b && state[a].Count >= 2 && (pass == 1 || state[b].Count > 0) && PourRules.IsLegal(state, a, b))
                        {
                            source = a;
                            target = b;
                            break;
                        }
                    }
                }
            }

            Assert.That(source, Is.GreaterThanOrEqualTo(0), "un vertido de una cuia con 2 dosis o más");
            CuiaView src = screen.Views[source];
            CuiaView dst = screen.Views[target];

            // Pasada de calentamiento con las comprobaciones: con el origen inclinado, sus iconos siguen dentro de su banda; en el
            // destino, el de la dosis que se llena aparece y crece con ella.
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured));
            int tiltedWithIcon = 0;
            int fillingWithIcon = 0;
            int receiving = dst.ShownCuia.Count;
            for (int t = 0; t < 4000 && !screen.IsViewIdle; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Canvas.ForceUpdateCanvases();
                if (Mathf.Abs(src.BodyAngle) > 20f)
                {
                    for (int k = 0; k < src.Capacity; k++)
                    {
                        if (src.Liquid.FruitIconDrawn(k))
                        {
                            tiltedWithIcon++;
                            Assert.That(src.Liquid.FruitIconPlacement(k, out _, out _, out float size, out _), Is.True);
                            Assert.That(size, Is.InRange(0.27f - 1e-4f, 0.45f + 1e-4f), "icono inclinado de 0,27 a 0,45 u");
                        }
                    }
                }

                float fill = dst.DoseFill(receiving);
                if (fill > 0.05f && fill < 0.95f && dst.Liquid.FruitIconDrawn(receiving))
                {
                    fillingWithIcon++;
                    Assert.That(dst.Liquid.FruitIconPlacement(receiving, out _, out float cy, out float size, out _), Is.True);
                    Assert.That(cy, Is.LessThan(CuiaGeometry.CavityBottomU + receiving + fill), "dentro de la dosis que se llena");
                    Assert.That(size, Is.LessThanOrEqualTo(0.45f + 1e-4f));
                }
            }

            TestContext.WriteLine("Vertido " + source + " → " + target + ": " + tiltedWithIcon + " iconos con el origen inclinado y " + fillingWithIcon + " fotogramas con la dosis a medio llenar y su icono.");
            Assert.That(tiltedWithIcon, Is.GreaterThan(0), "los iconos siguen a sus bandas con la cuia inclinada");
            Assert.That(fillingWithIcon, Is.GreaterThan(0), "y a la dosis que se llena");
            MechanicTestKit.AssertViewMatchesModel(boot);
            AssertIcons(boot, true, "tras el vertido");

            // Medida: Marea y el mismo vertido otra vez. En cada fotograma, la reconstrucción del lienzo (OnPopulateMesh de las
            // mallas del líquido con los iconos, en el origen inclinado y en el destino que se llena) sin asignaciones. El Tick
            // de la vista va fuera de la medida: su primer fotograma y la vuelta del origen ya asignaban sin iconos (punto
            // abierto de T-E), y los iconos solo cambian la malla.
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Canvas.ForceUpdateCanvases();
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured));
            LevelFlow flow = boot.Flow;
            TestDelegate rebuild = Canvas.ForceUpdateCanvases;
            int frames = 0;
            int tilted = 0;
            for (int f = 0; f < 250 && !screen.IsViewIdle; f++)
            {
                screen.Tick(PlayModeHelpers.FrameMs);
                flow.Tick(PlayModeHelpers.FrameMs, screen.IsViewIdle);
                string what = "fotograma " + f + " del vertido con iconos: el lienzo se reconstruye sin asignaciones (08 §5.4)";
                Assert.That(rebuild, Is.Not.AllocatingGCMemory(), what);
                frames++;
                tilted += Mathf.Abs(src.BodyAngle) > 20f ? 1 : 0;
            }

            Assert.That(frames, Is.GreaterThan(30), "se midió el vertido entero");
            Assert.That(tilted, Is.GreaterThan(5), "con el origen inclinado");
            Assert.That(screen.IsViewIdle, Is.True);
        }

        // Cada cuia: icono en cada dosis visible con fruta (on) o en ninguna (!on); nunca en ocultas, Barro ni agua base.
        // Devuelve los iconos a la vista.
        private static int AssertIcons(GameBootstrap boot, bool on, string where)
        {
            BoardState state = boot.Flow!.Session.Current;
            CuiaView[] views = boot.View!.Views;
            int drawn = 0;
            for (int i = 0; i < state.Count; i++)
            {
                Cuia cuia = state[i];
                Assert.That(views[i].FruitIconsOn, Is.EqualTo(on), where + ", cuia " + i + ": iconos " + (on ? "encendidos" : "apagados"));
                for (int k = 0; k < views[i].Capacity; k++)
                {
                    bool visible = k < cuia.Count && !cuia.IsHiddenAt(k) && ProceduralArt.HasFruitIcon(cuia.ColorAt(k));
                    bool shown = views[i].Liquid.FruitIconDrawn(k);
                    Assert.That(shown, Is.EqualTo(on && visible), where + ", cuia " + i + ", dosis " + k);
                    drawn += shown ? 1 : 0;
                }
            }

            return drawn;
        }
    }
}
