#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
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
    /// Barro mate (T20; hallazgo B1 de la revisión de T16, que pide el Barro «opaco, con grumos y sin brillo, del color
    /// Palette.Mud, pintado por MudMarks.PaintMud»; 02 §9.1 M5; direccion-visual-v2 4.2) en el nivel 123, el estreno del Barro:
    /// su dosis se pinta mate (<see cref="LiquidGraphic.SetMatteDose"/>), sin franjas especulares ni brillo vertical y, en la
    /// cima, con la cara superior de su color y sin la línea clara del borde; las de fruta y el agua base conservan su brillo.
    /// Se mira lo que pinta el shader con sus espejos en CPU (<see cref="LiquidGraphic.PaintedColorAt"/> y
    /// <see cref="LiquidGraphic.PaintedHighlightAt"/>), lo que le llega en la malla (patrón, franja y menisco por vértice, como
    /// en MurkyTests) y, en el texto del shader, que la vía mate apaga cada brillo. Además, el lavado devuelve el brillo a la
    /// fruta nueva, la Marea repone el Barro mate y la vía mate no asigna memoria. El reloj lo lleva el test.
    /// </summary>
    public sealed class MudMatteTests
    {
        private const string LiquidShaderPath = "_Project/Shaders/Resources/AF_UI_Liquid.shader";

        // Lo que puede pasar de Palette.Mud (L*) lo que pinta el shader en el centro de la franja del Barro: sin brillo, nada; el
        // margen solo cubre los redondeos. Antes de T20 eran +10 L*, los del agua base.
        private const float MudMarginL = 1f;

        // Lo que aclara como mínimo la franja de una dosis con brillo (guía 4.2: +10 L*, +12 en los tintes de L* < 40). Los
        // tintes más saturados pierden algo al recortarse a la gama: Aguaje, +7,9; Taperebá, +8,2.
        private const float GlossMinL = 6f;

        private static readonly MethodInfo PopulateMesh = typeof(LiquidGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)!;

        // Profundidades de una banda (0 arriba, 1 abajo) en las que se mira lo que pinta el shader.
        private static readonly float[] Depths = { 0.1f, 0.5f, 0.9f };

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Barro123_MatteWithoutStripes_FruitAndBaseWaterKeepGloss()
        {
            yield return MechanicTestKit.LoadPlaying(123);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardState model = boot.Flow!.Session.Current;
            GameScreen screen = boot.View!;
            FindMud(model, out int mud, out int layer);
            CuiaView view = screen.Views[mud];
            Assert.That(view.DoseColor(layer), Is.EqualTo(Palette.Mud), "el Barro, de Palette.Mud");
            Assert.That(view.Mud.LumpsVisible(layer), Is.True, "con sus grumos");
            AssertMatte(view, layer, "nivel 123, cuia " + mud);

            // Las frutas del tablero, con su franja (Guanábana no la lleva: guía 4.2).
            int fruits = 0;
            for (int i = 0; i < model.Count; i++)
            {
                Cuia cuia = model[i];
                for (int k = 0; k < cuia.Count; k++)
                {
                    int tint = cuia.ColorAt(k);
                    if (tint != Cuia.MudColor && !cuia.IsHiddenAt(k))
                    {
                        AssertGlossy(screen.Views[i], k, tint, "cuia " + i + ", dosis " + k);
                        fruits++;
                    }
                }
            }

            Assert.That(fruits, Is.GreaterThan(0), "el 123 tiene frutas a la vista");

            // El agua base (tinte 0, como la pinta la intro con CuiaView.SetDose) en una cuia vacía: la del nivel y las tres
            // de 07 §2.1, con su franja.
            int empty = FindEmpty(model);
            CuiaView water = screen.Views[empty];
            Color[] waters = { Palette.WaterBase(boot.Flow.Level.WaterBase), Palette.WaterBlack, Palette.WaterWhite, Palette.WaterClear };
            for (int w = 0; w < waters.Length; w++)
            {
                water.SetDose(0, waters[w], 1f, 0);
                AssertGlossy(water, 0, 0, "agua base #" + ColorUtility.ToHtmlStringRGB(waters[w]));
            }

            water.ShowContent(model[empty], model.IsSealed(empty));

            // Cualquier otro SetDose devuelve el brillo a la dosis; el estado (ShowContent → MudMarks.PaintMud) la vuelve mate.
            view.SetDose(layer, Palette.Tint(3), 1f, 3);
            AssertGlossy(view, layer, 3, "la dosis del Barro pintada con una fruta");
            view.ShowContent(model[mud], model.IsSealed(mud));
            AssertMatte(view, layer, "de nuevo con ShowContent");

            // Sin asignaciones (08 §5.4): la dosis pasa de fruta a mate y vuelta, con la reconstrucción del lienzo
            // (OnPopulateMesh) en cada cambio.
            LiquidGraphic liquid = view.Liquid;
            TestDelegate flip = () =>
            {
                for (int f = 0; f < 20; f++)
                {
                    if ((f & 1) == 0)
                    {
                        liquid.SetDose(layer, Palette.Tint(3), 1f, 3);
                    }
                    else
                    {
                        liquid.SetMatteDose(layer, Palette.Mud, 1f);
                    }

                    Canvas.ForceUpdateCanvases();
                }
            };

            flip(); // calentamiento
            Assert.That(flip, Is.Not.AllocatingGCMemory(), "la vía mate y su malla, sin asignaciones");
            view.ShowContent(model[mud], model.IsSealed(mud));
            AssertMatte(view, layer, "al final");
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Barro123_OnTopMatteFace_WashGlossy_UndoMatte()
        {
            yield return MechanicTestKit.LoadPlaying(123);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.BarroWashedMask != 0);
            Assert.That(path, Is.Not.Null, "vertidos que lavan el Barro del 123");
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            CuiaView view = boot.View!.Views[pour.Target];
            Cuia before = boot.Flow!.Session.Current[pour.Target];
            int layer = before.Count - 1;
            Assert.That(before.ColorAt(layer), Is.EqualTo(Cuia.MudColor), "el Barro, en la cima");

            // En la cima: mate, con la cara superior de su color (sin el aclarado del menisco) y, en el shader, sin la línea
            // clara de su borde (el patrón mate).
            AssertMatte(view, layer, "Barro en la cima");
            AssertTopFace(view, layer, Palette.Mud, "Barro en la cima");

            // El lavado: la fruta nueva que se enturbia es agua, con el brillo de su fruta.
            MechanicTestKit.TapPour(boot, pour, out TapResult poured);
            int tint = poured.Pour.Events.Color;
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.MudWash, pour.Target, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.MudWash, pour.Target, MudMarks.TurbidPeakAt);
            Assert.That(view.Mud.IsAnimating && view.Mud.WashLayer == layer, Is.True, "el lavado, en la dosis del Barro");
            AssertGlossy(view, layer, tint, "lavado en su punto más turbio");

            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Cuia after = boot.Flow.Session.Current[pour.Target];
            Assert.That(after.ColorAt(layer), Is.EqualTo(tint), "la fruta ocupa el lugar del Barro");
            AssertGlossy(view, layer, tint, "la fruta que lavó el Barro");
            int top = after.Count - 1;
            AssertTopFace(view, top, ColorMath.Lighten(Palette.Tint(after.ColorAt(top)), Palette.MeniscusLighten(after.ColorAt(top))), "la fruta en la cima");

            // Marea (02 §9.2: «Restaura el barro»): mate otra vez.
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.Flow.Session.Current[pour.Target].ColorAt(layer), Is.EqualTo(Cuia.MudColor), "la Marea repone el Barro");
            AssertMatte(view, layer, "tras la Marea");
            AssertTopFace(view, layer, Palette.Mud, "tras la Marea");
        }

        [Test]
        public void Shader_MatteTurnsOffEveryHighlight()
        {
            // El texto del shader: el patrón mate apaga las dos franjas y el brillo vertical (hl) y la línea clara del borde de
            // la cara superior, con el umbral de LiquidGraphic.IsMattePattern (la constante la compara
            // MurkyMudContentTests.ShaderConstants_MatchLiquidGraphic).
            string text = File.ReadAllText(Path.Combine(Application.dataPath, LiquidShaderPath));
            Assert.That(Regex.IsMatch(text, @"half gloss = patternMode < \(0\.5 \* \(AF_MATTE_MODE - 1\.0\)\)"), Is.True, "el punto medio entre el patrón mate y −1");
            Assert.That(Regex.IsMatch(text, @"float hl = max\(hlMain, max\(hlSec \* 0\.5, hlVert \* 0\.5\)\) \* gloss;"), Is.True, "las franjas y el brillo vertical");
            Assert.That(Regex.IsMatch(text, @"0\.35 \* rim \* inFace \* gloss"), Is.True, "la línea clara del borde de la cara superior");

            // El espejo en CPU separa el patrón mate de todos los demás, y el mate no saca la banda de su dosis al decodificarla.
            Assert.That(LiquidGraphic.IsMattePattern(LiquidGraphic.MattePatternMode), Is.True);
            float[] others = { -1f, 0f, 1f, LiquidGraphic.MurkyPatternMode - LiquidGraphic.MurkyProgressSpan, LiquidGraphic.MurkyPatternMode + LiquidGraphic.MurkyProgressSpan };
            for (int i = 0; i < others.Length; i++)
            {
                Assert.That(LiquidGraphic.IsMattePattern(others[i]), Is.False, "patrón " + others[i] + ": con brillo");
            }

            for (int k = 0; k < Cuia.MaxCapacity; k++)
            {
                float w = LiquidGraphic.MattePatternMode + (4f * k);
                Assert.That(Mathf.FloorToInt((w + 1.5f) / 4f), Is.EqualTo(k), "la banda mate " + k + " sigue en su dosis");
            }
        }

        // La dosis k de view se pinta mate: lo que pinta el shader en el centro de su franja no pasa de Palette.Mud más
        // MudMarginL (espejo PaintedHighlightAt) y la malla le lleva el patrón mate, ninguna fila del atlas y la franja de su
        // propio color.
        private static void AssertMatte(CuiaView view, int k, string where)
        {
            LiquidGraphic liquid = view.Liquid;
            Assert.That(liquid.IsMatteAt(k), Is.True, where + ": mate (LiquidGraphic.SetMatteDose)");
            Assert.That(liquid.DoseTintAt(k), Is.EqualTo(0), where + ": sin tinte de fruta");
            float mudL = ColorMath.LabLightness(Palette.Mud);
            for (int d = 0; d < Depths.Length; d++)
            {
                float lift = ColorMath.LabLightness(liquid.PaintedHighlightAt(k, Depths[d])) - mudL;
                Assert.That(lift, Is.LessThanOrEqualTo(MudMarginL), where + ": sin franja especular a la profundidad " + Depths[d] + " (+" + lift.ToString("F2") + " L*)");
            }

            Vector3 own = ColorMath.ToShaderSpace(Palette.Mud);
            List<UIVertex> band = BandVertices(view, k);
            Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), where + ": tiene banda");
            for (int i = 0; i < band.Count; i++)
            {
                UIVertex v = band[i];
                float pattern = v.uv2.w - (4f * k);
                Assert.That(pattern, Is.EqualTo(LiquidGraphic.MattePatternMode).Within(1e-4f), where + ": el patrón mate en la malla");
                Assert.That(LiquidGraphic.IsMattePattern(pattern), Is.True, where + ": el shader la pinta mate");
                Assert.That(v.uv3.w, Is.EqualTo(0f), where + ": ninguna fila del atlas");
                Assert.That(Distance(v.uv1, own), Is.LessThan(1e-4f), where + ": su franja es su color");
            }
        }

        // La dosis k de view (tinte tint) lleva su brillo: la malla no es mate y, salvo Guanábana (sin franja, guía 4.2), lo que
        // pinta el shader en el centro de su franja aclara al menos GlossMinL sobre su color de base.
        private static void AssertGlossy(CuiaView view, int k, int tint, string where)
        {
            LiquidGraphic liquid = view.Liquid;
            Assert.That(liquid.IsMatteAt(k), Is.False, where + ": con brillo");
            List<UIVertex> band = BandVertices(view, k);
            Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), where + ": tiene banda");
            for (int i = 0; i < band.Count; i++)
            {
                Assert.That(LiquidGraphic.IsMattePattern(band[i].uv2.w - (4f * k)), Is.False, where + ": el shader le pinta sus brillos");
            }

            if (tint == 1)
            {
                return;
            }

            for (int d = 0; d < Depths.Length; d++)
            {
                float lift = ColorMath.LabLightness(liquid.PaintedHighlightAt(k, Depths[d])) - ColorMath.LabLightness(liquid.PaintedColorAt(k, Depths[d]));
                Assert.That(lift, Is.GreaterThanOrEqualTo(GlossMinL), where + ": su franja a la profundidad " + Depths[d] + " (+" + lift.ToString("F2") + " L*)");
            }
        }

        // La cara superior que lleva la malla (uv3.rgb, el menisco, igual en todas las bandas) es expected en el espacio del
        // shader; top es la dosis de arriba.
        private static void AssertTopFace(CuiaView view, int top, Color expected, string where)
        {
            List<UIVertex> band = BandVertices(view, top);
            Assert.That(band.Count, Is.GreaterThanOrEqualTo(3), where + ": la dosis de arriba tiene banda");
            Vector3 want = ColorMath.ToShaderSpace(expected);
            for (int i = 0; i < band.Count; i++)
            {
                Vector4 men = band[i].uv3;
                Assert.That(Mathf.Abs(men.x - want.x) + Mathf.Abs(men.y - want.y) + Mathf.Abs(men.z - want.z), Is.LessThan(1e-4f), where + ": la cara superior, #" + ColorUtility.ToHtmlStringRGB(expected));
            }
        }

        // Vértices de la banda k como la decodifica el shader (floor((uv2.w + 1,5) / 4)), sin la lengua del chorro (patrón −1
        // en la dosis 0, solo con la cuia inclinada; aquí siempre está derecha). Como MurkyTests.BandVertices.
        private static List<UIVertex> BandVertices(CuiaView view, int k)
        {
            var list = new List<UIVertex>();
            using (var vh = new VertexHelper())
            {
                PopulateMesh.Invoke(view.Liquid, new object[] { vh });
                var v = new UIVertex();
                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref v, i);
                    if (Mathf.FloorToInt((v.uv2.w + 1.5f) / 4f) == k)
                    {
                        list.Add(v);
                    }
                }
            }

            return list;
        }

        // La dosis de Barro del tablero (el 123 estrena una).
        private static void FindMud(BoardState board, out int cuia, out int layer)
        {
            for (int i = 0; i < board.Count; i++)
            {
                for (int k = 0; k < board[i].Count; k++)
                {
                    if (board[i].ColorAt(k) == Cuia.MudColor)
                    {
                        cuia = i;
                        layer = k;
                        return;
                    }
                }
            }

            Assert.Fail("el tablero no tiene Barro");
            cuia = -1;
            layer = -1;
        }

        private static int FindEmpty(BoardState board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].Count == 0)
                {
                    return i;
                }
            }

            Assert.Fail("el tablero no tiene cuias vacías");
            return -1;
        }

        private static float Distance(Vector4 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
        }
    }
}
