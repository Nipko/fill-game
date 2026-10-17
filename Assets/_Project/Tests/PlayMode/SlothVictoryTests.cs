#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// TiTino en la victoria (tarea TI6a; propuesta de TiTino d.4 y paso 6a de (i); 02 §6, fila TiTino; 07 §5.4; T6-A; D-016):
    /// <list type="bullet">
    /// <item>la caja: ≥ 104 dp, gesto y bocadillo; 56-103, solo el gesto; menos o con 3 filas, nada; TiTino se encoge para caber
    /// entero con todo lo que mueve su gesto;</item>
    /// <item>SlothHito por defecto: en 40, 60, 80 y 140 habla si cabe (y la llegada va sin dato) y, si no, el hito lleva su dato;
    /// con los mensajes apagados, solo el gesto; con <c>ui_sloth_enabled</c> apagado, nada;</item>
    /// <item>tiempos: la rama empieza a entrar al abrirse la pantalla de victoria (la fila 0,80 [P]: 07 §5.4 dice 0,60, pero
    /// la caja depende del panel, que se coloca ahí; pendiente del dueño) y entra en 0,3 s; durante la celebración no está;
    /// el bocadillo, a los 1,10 s y se va a los 4 s o con Siguiente, que sigue a los 1,40 s; con Reducir movimiento, sin
    /// deslizar ni fundidos;</item>
    /// <item>TiTino se va en todas las salidas de la victoria: el nivel siguiente y «El río sigue creciendo» tras AD_GATE;</item>
    /// <item>ninguna intersección de TiTino ni del bocadillo con la cinta, las cuias (con su elevación), el saldo, el panel (la
    /// Cosecha, la ranura y los botones) ni la cabecera, a 360 × 640 y 360 × 780, con 1 y 3 filas, al 100 % y al 130 %, en
    /// todos los fotogramas de la victoria; con 3 filas no sale;</item>
    /// <item>sin toques: ningún gráfico es <c>raycastTarget</c>.</item>
    /// </list>
    /// <para>Salvo <see cref="RealHitos_SpeakIfItFits_ElseTheArrivalKeepsItsFact"/>, que mide los hitos del catálogo real con
    /// la ranura y sin ella, las victorias van sin la ranura del Dato del río (<see cref="GameScreen.ArrivalDisabledForTests"/> y
    /// <see cref="GameScreen.FactLineDisabledForTests"/>) y con tableros de una fila: son las únicas en las que la caja da para
    /// TiTino. En los hitos reales (40, 60, 80 y 140, tableros de 2 filas), ShiftToClear sube las cuias hasta la cabecera para
    /// despejar el panel de victoria y la caja mide 0 dp a 360 × 780 y a 360 × 640, con la ranura y también sin ella: hoy, en el
    /// juego real, TiTino no sale en sus hitos (medido en TI6a; decisión pendiente del dueño, con las medidas en el informe de
    /// la tarea y en <see cref="GameScreen.SlothBoxUnclampedDp"/>).</para>
    /// </summary>
    public sealed class SlothVictoryTests
    {
        private static readonly string[] Fruits = { "acai", "aguaje", "tapereba", "guanabana", "cupuacu", "urucum", "camu_camu", "bacaba", "jenipapo", "lulo", "araza", "cacao" };

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            NoSlot(true);
        }

        [TearDown]
        public void TearDown()
        {
            NoSlot(false);
            MapArtRule.ForceProvisional = null;
            ArtSlots.ForceProcedural = false;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        // ------------------------------------------------------------------------------------------- caja y tamaño

        [Test]
        public void Mode_FollowsTheBoxAndTheRows()
        {
            Assert.That(SlothVictory.ModeFor(104f, 1), Is.EqualTo(SlothVictoryMode.Speak), "≥ 104 dp: gesto y bocadillo");
            Assert.That(SlothVictory.ModeFor(300f, 2), Is.EqualTo(SlothVictoryMode.Speak));
            Assert.That(SlothVictory.ModeFor(103.9f, 1), Is.EqualTo(SlothVictoryMode.Gesture), "56-103: solo el gesto");
            Assert.That(SlothVictory.ModeFor(56f, 2), Is.EqualTo(SlothVictoryMode.Gesture));
            Assert.That(SlothVictory.ModeFor(55.9f, 1), Is.EqualTo(SlothVictoryMode.None), "< 56: nada");
            Assert.That(SlothVictory.ModeFor(0f, 1), Is.EqualTo(SlothVictoryMode.None));
            Assert.That(SlothVictory.ModeFor(400f, 3), Is.EqualTo(SlothVictoryMode.None), "con 3 filas no sale");
            Assert.That(GameScreen.SlothSpeakBoxDp, Is.EqualTo(104f));

            Assert.That(SlothVictory.SizeFor(SlothVictoryMode.None, 200f), Is.EqualTo(0f));
            Assert.That(SlothVictory.SizeFor(SlothVictoryMode.Speak, 400f), Is.EqualTo(SlothVictory.SpeakSizeDp), "72 dp como mucho");
            Assert.That(SlothVictory.SizeFor(SlothVictoryMode.Gesture, 400f), Is.EqualTo(SlothVictory.GestureSizeDp), "48 dp como mucho");
            foreach (float box in new[] { 56f, 70f, 90f, 103f, 104f, 120f, 150f })
            {
                SlothVictoryMode mode = SlothVictory.ModeFor(box, 1);
                float s = SlothVictory.SizeFor(mode, box);
                Assert.That((s * (SlothVictory.RigUpS + SlothVictory.RigDownS)) + (2f * SlothVictory.InsetDp), Is.LessThanOrEqualTo(box + 0.01f),
                    "TiTino cabe entero en una caja de " + box + " dp");
                Assert.That(s, Is.GreaterThan(30f), "y se le ve (" + s + " dp)");
            }
        }

        [Test]
        public void RigExtents_AreTheOnesTheBoxReserves()
        {
            // Todo lo que mueven sloth_cheer y sloth_cheer_big (la garra alzada, la media vuelta y las hojas) cabe en
            // [−RigLeftS, +media rama… la hoja] × [−RigUpS, +RigDownS] alrededor de la línea de la rama.
            ArtSlots.ForceProcedural = true;
            var art = new ProceduralArt("white");
            var canvasGo = new GameObject("SlothExtentsCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var layerGo = new GameObject("Layer", typeof(RectTransform));
                layerGo.transform.SetParent(canvasGo.transform, false);
                var space = (RectTransform)layerGo.transform;
                space.anchorMin = Vector2.zero;
                space.anchorMax = Vector2.one;
                space.sizeDelta = Vector2.zero;
                SlothRig rig = SlothRig.Create(space, art.SlothRigArt(), 72f)!;
                Assert.That(rig, Is.Not.Null);
                var rects = new List<Rect>();
                const float x = 300f;
                const float y = 300f;
                float maxUp = 0f;
                float maxDown = 0f;
                float maxLeft = 0f;
                foreach (SlothPoseId pose in new[] { SlothPoseId.Cheer, SlothPoseId.CheerBig })
                {
                    rig.Place(x, y);
                    rig.Play(pose, false);
                    for (float ms = 0f; ms <= 3000f; ms += 8f)
                    {
                        rig.Seek(ms);
                        rects.Clear();
                        rig.CollectRects(space, rects);
                        foreach (Rect r in rects)
                        {
                            maxUp = Mathf.Max(maxUp, (y - r.yMin) / 72f);
                            maxDown = Mathf.Max(maxDown, (r.yMax - y) / 72f);
                            maxLeft = Mathf.Max(maxLeft, (x - r.xMin) / 72f);
                        }
                    }
                }

                TestContext.WriteLine("extensión del rig (S): arriba " + F(maxUp) + ", abajo " + F(maxDown) + ", izquierda " + F(maxLeft));
                Assert.That(maxUp, Is.LessThanOrEqualTo(SlothVictory.RigUpS), "la garra alzada y la rama");
                Assert.That(maxDown, Is.LessThanOrEqualTo(SlothVictory.RigDownS), "el torso y las hojas que caen");
                Assert.That(maxLeft, Is.LessThanOrEqualTo(SlothVictory.RigLeftS), "la media vuelta de sloth_cheer_big");
            }
            finally
            {
                Object.Destroy(canvasGo);
                art.Dispose();
            }
        }

        [Test]
        public void Relayout_ShrinksToTheGesture_AndLeaves_WhenTheBoxShrinks()
        {
            ArtSlots.ForceProcedural = true;
            var art = new ProceduralArt("white");
            var canvasGo = new GameObject("SlothRelayoutCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var sloth = new SlothVictory((RectTransform)canvasGo.transform, art);
                Assert.That(sloth.Rig, Is.Null, "perezoso: nada hasta la primera victoria");
                Assert.That(sloth.Show(100f, 260f, 1, SlothGesture.CheerBig, "¡Mundo completo! Cuánto color trajiste.", 360f, false), Is.True);
                Assert.That(sloth.Mode, Is.EqualTo(SlothVictoryMode.Speak));
                Assert.That(sloth.HasBubble, Is.True);
                Assert.That(sloth.SizeDp, Is.EqualTo(SlothVictory.SpeakSizeDp));

                sloth.Relayout(100f, 180f, 1, 360f);
                Assert.That(sloth.Mode, Is.EqualTo(SlothVictoryMode.Gesture), "la caja de 80 dp: solo el gesto");
                Assert.That(sloth.HasBubble, Is.False, "y la frase se pierde");
                Assert.That(sloth.SizeDp, Is.LessThanOrEqualTo(SlothVictory.GestureSizeDp));
                Assert.That(sloth.Visible, Is.True);

                sloth.Relayout(100f, 140f, 1, 360f);
                Assert.That(sloth.Visible, Is.False, "40 dp: se va");
                Assert.That(sloth.Mode, Is.EqualTo(SlothVictoryMode.None));

                Assert.That(sloth.Show(100f, 400f, 3, SlothGesture.Cheer, null, 360f, false), Is.False, "con 3 filas no sale");
                Assert.That(sloth.Visible, Is.False);
                Assert.That(sloth.Show(100f, 400f, 1, SlothGesture.None, null, 360f, false), Is.False, "sin gesto (ui_sloth_enabled apagado)");
            }
            finally
            {
                Object.Destroy(canvasGo);
                art.Dispose();
            }
        }

        // ------------------------------------------------------------------------------------------- hitos (SlothHito)

        [Test]
        public void HitoPolicy_IsSlothHitoByDefault()
        {
            Assert.That(MapConfig.HitoPolicy, Is.EqualTo(HitoLinePolicy.SlothHito), "T6-A: desde TI6a");
        }

        [UnityTest]
        public IEnumerator RealHitos_SpeakIfItFits_ElseTheArrivalKeepsItsFact([Values(40, 60, 80, 140)] int level, [Values(780, 640)] int height,
            [Values(true, false)] bool slot)
        {
            // La victoria del juego (con la llegada y la línea de fruta) y la misma sin la ranura, a 360 × 780 y 360 × 640.
            NoSlot(!slot);
            GameScreen.SimulatedScreen = height == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            yield return VictoryArrivalTests.Win(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            VictoryLine line = view.CurrentVictoryLine;
            SlothVictory sloth = view.SlothView;
            string at = "nivel " + level + " a 360 × " + height + (slot ? " con" : " sin") + " ranura (caja " + F(view.SlothBoxDp)
                + " dp, sin el tope de la cabecera " + F(view.SlothBoxUnclampedDp) + " dp, " + view.Layout!.Rows + " filas)";
            TestContext.WriteLine("AF_SLOTH_REAL " + at + ": habla " + line.SlothSpeaks + ", TiTino " + sloth.Mode + " a " + F(sloth.SizeDp)
                + " dp, dato de llegada " + line.ArrivalFact);
            Assert.That(VictoryLineArbiter.IsSlothHito(level), Is.True, at);
            Assert.That(line.SlothBig, Is.True, at);
            Assert.That(view.SlothCouldSpeak, Is.EqualTo(line.SlothSpeaks), at + ": SlothCanSpeak decide la línea (SlothHito)");
            if (line.SlothSpeaks)
            {
                Assert.That(view.SlothBoxDp, Is.GreaterThanOrEqualTo(GameScreen.SlothSpeakBoxDp), at);
                Assert.That(sloth.BubbleShown, Is.True, at + ": el bocadillo, a la vista");
                string key = level == 60 ? "sloth_line_milestone_02" : "sloth_line_milestone_01";
                Assert.That(sloth.BubbleText, Is.EqualTo(Loc.T(key)), at);
                Assert.That(view.LastSlothLine.Key, Is.EqualTo(key), at);
                Assert.That(line.ArrivalFact, Is.False, at + ": la llegada va sin dato");
                Assert.That(line.FruitKey, Is.Null, at);
            }
            else
            {
                Assert.That(line.ArrivalFact, Is.True, at + ": si TiTino no cabe, el hito lleva su dato");
                Assert.That(sloth.HasBubble, Is.False, at);
                Assert.That(view.LastSlothLine.Speaks, Is.False, at);
            }

            Assert.That(sloth.Visible, Is.EqualTo(SlothVictory.ModeFor(view.SlothBoxDp, view.Layout.Rows) != SlothVictoryMode.None), at + ": sale si cabe");
            if (sloth.Visible)
            {
                Assert.That(sloth.Pose, Is.EqualTo(SlothPoseId.CheerBig), at + ": sloth_cheer_big en sus hitos");
            }

            AssertNoIntersection(view, at);
        }

        [UnityTest]
        public IEnumerator Hito40OneRow_Speaks_AndThe41GetsTheDeferredFact()
        {
            // Un paquete con el 40 y el 41 (los dos de una fila): Siguiente lleva al 41, no a «El río sigue creciendo».
            yield return WinPack(OneRowPack(40, 2), 40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            SlothVictory sloth = view.SlothView;
            TestContext.WriteLine("caja en el 40 de una fila: " + F(view.SlothBoxDp) + " dp; TiTino a " + F(sloth.SizeDp) + " dp; bocadillo "
                + sloth.BubbleRect + " en " + sloth.BubbleLines + " líneas a " + F(sloth.BubbleFontDp) + " dp");
            Assert.That(view.SlothBoxDp, Is.GreaterThanOrEqualTo(GameScreen.SlothSpeakBoxDp), "una fila deja la caja entera");
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.True, "el 40 es su hito y cabe: habla él");
            Assert.That(view.CurrentVictoryLine.ArrivalFact, Is.False);
            Assert.That(sloth.Mode, Is.EqualTo(SlothVictoryMode.Speak));
            Assert.That(sloth.SizeDp, Is.LessThanOrEqualTo(SlothVictory.SpeakSizeDp).And.GreaterThan(60f));
            Assert.That(sloth.Pose, Is.EqualTo(SlothPoseId.CheerBig));
            Assert.That(sloth.BubbleText, Is.EqualTo(Loc.T("sloth_line_milestone_01")));
            Assert.That(sloth.BubbleLines, Is.LessThanOrEqualTo(SlothVictory.BubbleMaxLines), "≤ 2 líneas");
            Assert.That(sloth.BubbleFontDp, Is.InRange(UiBuilder.MinTextSp, SlothVictory.BubbleTextSp), "14 sp, nunca menos de 12");
            Assert.That(sloth.BubbleRect.width, Is.LessThanOrEqualTo(SlothVictory.BubbleMaxWidthDp + 20f), "ancho máximo (con su borde y su pico)");
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(1), "la frase cuenta para el tope de la sesión");
            AssertNoIntersection(view, "40");

            // El 41 lleva el dato desplazado (T6-A) y TiTino solo hace el gesto.
            view.NextButton.onClick.Invoke();
            for (int t = 0; t < 5000 && !(boot.Flow!.LevelNumber == 41 && VictorySetup.InNewLevel(boot.Flow.Phase)); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "el 41 está en el paquete");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(41));
            Assert.That(sloth.Visible, Is.False, "fuera de la victoria, TiTino se va");
            Assert.That(sloth.Layer.gameObject.activeSelf, Is.False);
            Assert.That(sloth.ClockMs, Is.LessThan(0f));
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.CurrentVictoryLine.DeferredFact, Is.True, "el dato del 40 sale en el 41");
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False);
            Assert.That(sloth.HasBubble, Is.False, "una sola línea por victoria");
            Assert.That(view.LastSlothLine.Speaks, Is.False);
            if (sloth.Visible)
            {
                Assert.That(sloth.Pose, Is.EqualTo(SlothPoseId.Cheer), "fuera de sus hitos, sloth_cheer");
            }
        }

        [UnityTest]
        public IEnumerator Hito40_NextToContentEnd_TheSlothLeaves()
        {
            // Sin nivel 41 en el paquete, Siguiente (AD_GATE) lleva a «El río sigue creciendo»: TiTino no se queda colgado.
            yield return WinPack(OneRowPack(40), 40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            SlothVictory sloth = view.SlothView;
            Assert.That(sloth.Visible, Is.True, "TiTino, en la victoria del 40");
            view.NextButton.onClick.Invoke();
            for (int t = 0; t < 5000 && boot.App!.Screen != AppScreen.ContentEnd; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.ContentEnd), "sin el 41, «El río sigue creciendo»");
            Assert.That(sloth.Visible, Is.False, "TiTino se va con la victoria");
            Assert.That(sloth.Layer.gameObject.activeSelf, Is.False);
            Assert.That(sloth.ClockMs, Is.LessThan(0f));
            Assert.That(sloth.BubbleShown, Is.False);
        }

        [UnityTest]
        public IEnumerator Hito40_WithoutMessagesOrDisabled_TheArrivalKeepsItsFact()
        {
            // Mensajes de TiTino apagado (settings.sloth_messages): solo el gesto, y el hito es una victoria normal.
            yield return WinPack(OneRowPack(40), 40, v => v.SlothVoice = false);
            GameScreen view = GameBootstrap.Current!.View!;
            PlayModeHelpers.Step(GameBootstrap.Current, 1600);
            Assert.That(view.SlothBoxDp, Is.GreaterThanOrEqualTo(GameScreen.SlothSpeakBoxDp));
            Assert.That(view.SlothCouldSpeak, Is.False);
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False);
            Assert.That(view.CurrentVictoryLine.ArrivalFact, Is.True, "la llegada lleva su dato");
            Assert.That(view.SlothView.Visible, Is.True, "TiTino hace el gesto");
            Assert.That(view.SlothView.Pose, Is.EqualTo(SlothPoseId.CheerBig));
            Assert.That(view.SlothView.HasBubble, Is.False);

            // ui_sloth_enabled apagado: no sale.
            yield return WinPack(OneRowPack(40), 40, v => v.SlothEnabled = false);
            view = GameBootstrap.Current!.View!;
            PlayModeHelpers.Step(GameBootstrap.Current, 1600);
            Assert.That(view.SlothCouldSpeak, Is.False);
            Assert.That(view.CurrentVictoryLine.ArrivalFact, Is.True);
            Assert.That(view.SlothView.Visible, Is.False);
            Assert.That(view.SlothView.Rig, Is.Null, "ni se crea");
        }

        [UnityTest]
        public IEnumerator ThreeRows_NoSloth_AndTheHitoKeepsItsFact()
        {
            yield return WinPack(FifteenCuiaPack(40), 40);
            GameScreen view = GameBootstrap.Current!.View!;
            PlayModeHelpers.Step(GameBootstrap.Current, 1600);
            TestContext.WriteLine("caja en el 40 de 3 filas: " + F(view.SlothBoxDp) + " dp");
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(view.SlothView.Visible, Is.False, "con 3 filas TiTino no sale");
            Assert.That(view.SlothCouldSpeak, Is.False);
            Assert.That(view.CurrentVictoryLine.ArrivalFact, Is.True, "el hito lleva su dato");
        }

        [UnityTest]
        public IEnumerator NonHito_OnlyTheGesture()
        {
            yield return WinPack(OneRowPack(39), 39);
            GameScreen view = GameBootstrap.Current!.View!;
            PlayModeHelpers.Step(GameBootstrap.Current, 1600);
            Assert.That(view.SlothView.Visible, Is.True, "sale si cabe en cualquier victoria");
            Assert.That(view.SlothView.Pose, Is.EqualTo(SlothPoseId.Cheer));
            Assert.That(view.SlothView.HasBubble, Is.False, "la línea es la de fruta: TiTino solo hace el gesto");
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False);
            AssertNoIntersection(view, "39");
        }

        // ------------------------------------------------------------------------------------------- tiempos

        [UnityTest]
        public IEnumerator Timing_BranchFromTheVictoryScreen_In300Ms_BubbleAt110_UntilFour_NextUnchanged()
        {
            // Instante de inicio [P]: durante la celebración (filas 0,00-0,80) TiTino no está; su rama empieza a entrar en el
            // fotograma en que se abre la pantalla de victoria (la fila 0,80, con la subida del panel), no a la 0,60 de 07 §5.4.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGameWithPack(OneRowPack(40), 40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            SlothVictory sloth = view.SlothView;
            LevelFlow flow = boot.Flow!;
            Pour last = VictorySetup.PlayAllButTheLastPour(boot);
            boot.TapCuia(last.Source);
            boot.TapCuia(last.Target);
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            int celebrationMs = -1;
            for (int t = 0; t < 15000 && flow.Phase != FlowPhase.VictoryScreen; t += PlayModeHelpers.FrameMs)
            {
                bool celebrating = flow.Phase == FlowPhase.Celebration;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (celebrating)
                {
                    celebrationMs = celebrationMs < 0 ? PlayModeHelpers.FrameMs : celebrationMs + PlayModeHelpers.FrameMs;
                }

                if (flow.Phase != FlowPhase.VictoryScreen)
                {
                    Assert.That(sloth.Visible, Is.False, "en la celebración (a los " + celebrationMs + " ms), TiTino aún no está");
                    Assert.That(sloth.ClockMs, Is.LessThan(0f));
                }
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            TestContext.WriteLine("celebración: " + celebrationMs + " ms");
            Assert.That(celebrationMs, Is.GreaterThanOrEqualTo(GameConfig.AnimCelebrationMs - PlayModeHelpers.FrameMs), "la celebración entera");
            Assert.That(sloth.Visible, Is.True, "sale con la pantalla de victoria");
            Assert.That(sloth.ClockMs, Is.EqualTo(0f), "su reloj arranca en el fotograma en que se abre la victoria");
            Assert.That(sloth.RootX, Is.LessThan(sloth.RestX - 20f), "la rama entra desde el borde izquierdo");
            PlayModeHelpers.TickExact(boot, 150);
            Assert.That(sloth.RootX, Is.GreaterThan(0f).And.LessThan(sloth.RestX));
            PlayModeHelpers.TickExact(boot, (int)SlothVictory.EnterMs - 150);
            Assert.That(sloth.RootX, Is.EqualTo(sloth.RestX).Within(0.01f), "en 0,3 s");
            Assert.That(sloth.BubbleAlpha, Is.EqualTo(0f), "el bocadillo aún no");
            PlayModeHelpers.TickExact(boot, (int)SlothVictory.BubbleInMs - (int)SlothVictory.EnterMs + 1);
            Assert.That(sloth.BubbleAlpha, Is.GreaterThan(0f), "a los 1,10 s");
            PlayModeHelpers.TickExact(boot, (int)SlothVictory.BubbleFadeMs);
            Assert.That(sloth.BubbleAlpha, Is.EqualTo(1f));

            // Siguiente sigue a los 1,40 s (600 ms de la pantalla de victoria), con TiTino o sin él.
            float now = sloth.ClockMs;
            Assert.That(view.NextAlpha, Is.EqualTo(0f), "a los " + now + " ms, Siguiente aún no");
            PlayModeHelpers.TickExact(boot, (int)(GameScreen.VictoryButtonsInMs - now) + 1);
            Assert.That(view.NextAlpha, Is.GreaterThan(0f), "Siguiente entra a los 1,40 s");
            Assert.That(view.NextButton.interactable, Is.True);

            PlayModeHelpers.TickExact(boot, (int)(SlothVictory.BubbleOutMs - sloth.ClockMs) - 1);
            Assert.That(sloth.BubbleAlpha, Is.EqualTo(1f), "hasta los 4 s");
            PlayModeHelpers.TickExact(boot, (int)SlothVictory.BubbleFadeMs + 2);
            Assert.That(sloth.BubbleAlpha, Is.EqualTo(0f), "a los 4 s se va");
            Assert.That(sloth.Visible, Is.True, "TiTino sigue con su gesto");
        }

        [UnityTest]
        public IEnumerator Bubble_LeavesWithNext()
        {
            yield return WinPack(OneRowPack(40), 40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TickExact(boot, 1000);
            Assert.That(view.SlothView.BubbleAlpha, Is.EqualTo(1f));
            view.NextButton.onClick.Invoke();
            PlayModeHelpers.TickExact(boot, (int)SlothVictory.BubbleFadeMs + 2);
            Assert.That(view.SlothView.BubbleAlpha, Is.EqualTo(0f), "con Siguiente, el bocadillo se va");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_NoSlide_AndTheBubbleWithoutFades()
        {
            yield return WinPack(OneRowPack(40), 40, v => GameBootstrap.Current!.Flow!.ReduceMotion = true);
            GameBootstrap boot = GameBootstrap.Current!;
            SlothVictory sloth = boot.View!.SlothView;
            Assert.That(sloth.RootX, Is.EqualTo(sloth.RestX).Within(0.01f), "en su sitio desde el principio");
            Assert.That(sloth.Rig!.ReduceMotion, Is.True, "pose quieta con el fundido del rig");
            PlayModeHelpers.TickExact(boot, (int)SlothVictory.BubbleInMs + 1);
            Assert.That(sloth.BubbleAlpha, Is.EqualTo(1f), "sin fundido");
        }

        // ------------------------------------------------------------------------------------------- sin intersecciones

        [UnityTest]
        public IEnumerator NoIntersection_640And780_OneAndThreeRows_100And130()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    foreach (bool threeRows in new[] { false, true })
                    {
                        string at = (small ? "640" : "780") + " al " + (scale > 1.01f ? "130" : "100") + " % con " + (threeRows ? "3 filas" : "1 fila");
                        FunnelSetup.Hooks();
                        MapArtRule.ForceProvisional = true;
                        GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
                        LaunchOptions.FontScaleOverride = scale;
                        yield return WinPack(threeRows ? FifteenCuiaPack(40) : OneRowPack(40), 40);
                        GameBootstrap boot = GameBootstrap.Current!;
                        GameScreen view = boot.View!;
                        SlothVictory sloth = view.SlothView;
                        bool spoke = false;
                        for (int ms = 0; ms <= 3600; ms += PlayModeHelpers.FrameMs)
                        {
                            AssertNoIntersection(view, at + " a los " + ms + " ms");
                            spoke |= sloth.BubbleShown;
                            PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                        }

                        float box = view.SlothBoxDp;
                        TestContext.WriteLine("AF_SLOTH_BOX " + at + ": caja " + F(box) + " dp (sin el tope de la cabecera " + F(view.SlothBoxUnclampedDp)
                            + " dp), TiTino " + sloth.Mode + " a " + F(sloth.SizeDp) + " dp, bocadillo " + sloth.BubbleLines + " líneas a "
                            + F(sloth.BubbleFontDp) + " dp");
                        if (threeRows)
                        {
                            Assert.That(sloth.Visible, Is.False, at + ": con 3 filas no sale");
                        }
                        else if (box >= GameScreen.SlothSpeakBoxDp)
                        {
                            // ≥ 104 dp: gesto y bocadillo, si la frase cabe (SlothCanSpeak la mide antes del árbitro).
                            Assert.That(sloth.Visible, Is.True, at);
                            Assert.That(sloth.Mode, Is.EqualTo(SlothVictoryMode.Speak), at);
                            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.EqualTo(view.SlothCouldSpeak), at + ": la línea es suya si puede hablar");
                            Assert.That(spoke, Is.EqualTo(view.CurrentVictoryLine.SlothSpeaks), at + ": habla si la línea es suya");
                        }
                        else if (box >= SlothVictory.GestureBoxDp)
                        {
                            // 56-103 dp: solo el gesto, y el hito es una victoria normal.
                            Assert.That(sloth.Visible, Is.True, at + ": TiTino, a la vista");
                            Assert.That(sloth.Mode, Is.EqualTo(SlothVictoryMode.Gesture), at + ": solo el gesto");
                            Assert.That(spoke, Is.False, at + ": sin bocadillo");
                            Assert.That(sloth.HasBubble, Is.False, at);
                            Assert.That(view.SlothCouldSpeak, Is.False, at);
                            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False, at);
                        }
                        else
                        {
                            Assert.That(sloth.Visible, Is.False, at + ": menos de 56 dp, no sale");
                        }

                        if (!threeRows && !small && scale < 1.01f)
                        {
                            // La referencia: a 360 × 780 y al 100 %, con una fila, la caja da para el bocadillo y TiTino habla.
                            Assert.That(box, Is.GreaterThanOrEqualTo(GameScreen.SlothSpeakBoxDp), at);
                            Assert.That(spoke, Is.True, at + ": con una fila a 780 y al 100 %, habla");
                        }

                        LaunchOptions.Reset();
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator NeverCapturesTouches()
        {
            yield return WinPack(OneRowPack(40), 40);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TickExact(boot, 1200);
            SlothVictory sloth = boot.View!.SlothView;
            var graphics = new List<Graphic>();
            sloth.CollectGraphics(graphics);
            Assert.That(graphics.Count, Is.GreaterThan(SlothRig.BodyGraphics), "el rig y el bocadillo");
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name + ": sin raycast");
            }

            Assert.That(sloth.BlocksRaycasts, Is.False);
            Assert.That(boot.View.NextButton.interactable, Is.True);
        }

        // ------------------------------------------------------------------------------------------- utilidades

        /// <summary>
        /// Ni TiTino (sus piezas a la vista) ni su bocadillo se cruzan con la cinta, el panel de victoria (la Cosecha, la ranura y
        /// los botones), los botones, el saldo ni las cuias con su elevación (donde están ahora), y no suben por encima del pie de
        /// la cabecera. Sin TiTino a la vista, no hay nada que comprobar.
        /// </summary>
        internal static void AssertNoIntersection(GameScreen view, string at)
        {
            SlothVictory sloth = view.SlothView;
            if (!sloth.Visible)
            {
                return;
            }

            RectTransform space = sloth.Layer;
            var mine = new List<Rect>();
            sloth.CollectRects(space, mine, true);
            var others = new List<(string, Rect)>();
            float panelTop = view.VictoryPanelTopDp - 38f;
            others.Add(("panel y cinta", Rect.MinMaxRect(-1000f, panelTop, 1000f, view.VictoryPanelBottomDp + 1000f)));
            others.Add(("cinta", VictorySetup.CanvasRect(view.VictoryRibbonRect, space)));
            AddIfActive(others, "Siguiente", view.NextButton.transform, space);
            AddIfActive(others, "Inicio", view.HomeButton.transform, space);
            AddIfActive(others, "Cosecha doble", view.DoubleHarvestButton.transform, space);
            AddIfActive(others, "saldo: semillas", view.WalletBar.SeedsRect, space);
            AddIfActive(others, "saldo: canoas", view.WalletBar.CanoesRect, space);
            others.Add(("cabecera", Rect.MinMaxRect(-1000f, -1000f, 1000f, view.SlothBandTopDp)));
            BoardLayoutResult layout = view.Layout!;
            float shift = view.BoardContainer.anchoredPosition.y;
            for (int i = 0; i < view.Views.Length; i++)
            {
                CuiaView cuia = view.Views[i];
                float half = (layout.CuiaWidth * 0.5f) + 2f;
                float top = layout.FootY[i] - (cuia.HeightU * cuia.Unit) - CuiaView.LiftDp - shift;
                float bottom = layout.FootY[i] - shift;
                others.Add(("cuia " + i, Rect.MinMaxRect(layout.CenterX[i] - half, top, layout.CenterX[i] + half, bottom)));
            }

            foreach (Rect m in mine)
            {
                if (m.width <= 0.01f || m.height <= 0.01f)
                {
                    continue;
                }

                foreach ((string name, Rect r) in others)
                {
                    Assert.That(Overlaps(m, r), Is.False, at + ": TiTino " + m + " se cruza con " + name + " " + r);
                }
            }
        }

        private static void AddIfActive(List<(string, Rect)> list, string name, Transform t, RectTransform space)
        {
            if (t.gameObject.activeInHierarchy)
            {
                list.Add((name, VictorySetup.CanvasRect((RectTransform)t, space)));
            }
        }

        private static bool Overlaps(Rect a, Rect b)
        {
            return a.xMin < b.xMax - 0.01f && b.xMin < a.xMax - 0.01f && a.yMin < b.yMax - 0.01f && b.yMin < a.yMax - 0.01f;
        }

        /// <summary>
        /// Sin (<paramref name="on"/> = true) o con la ranura del Dato del río: la llegada y la línea de fruta (los dos únicos
        /// proveedores).
        /// </summary>
        internal static void NoSlot(bool on)
        {
            GameScreen.ArrivalDisabledForTests = on;
            GameScreen.FactLineDisabledForTests = on;
        }

        /// <summary>Carga el paquete <paramref name="json"/> en el nivel <paramref name="level"/>, quita el reloj y gana hasta la victoria.</summary>
        internal static IEnumerator WinPack(string json, int level, System.Action<GameScreen>? before = null)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGameWithPack(json, level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            before?.Invoke(boot.View!);
            VictorySetup.WinToVictoryScreen(boot);
        }

        /// <summary>
        /// Paquete de <paramref name="count"/> niveles seguidos desde <paramref name="level"/>, cada uno con 5 cuias en una fila
        /// (3 frutas de 4 dosis y 2 vacías) y en el mundo que le toca (el 40 en el 2 y el 41 en el 3).
        /// </summary>
        internal static string OneRowPack(int level, int count = 1)
        {
            int[][] board = { new[] { 1, 2, 3, 1 }, new[] { 2, 3, 1, 2 }, new[] { 3, 1, 2, 3 }, new int[0], new int[0] };
            string[] palette = { Fruits[0], Fruits[1], Fruits[2] };
            string first = Pack(level, palette, board);
            if (count <= 1)
            {
                return first;
            }

            // Los niveles de los demás paquetes van a la lista «levels» del primero (cada nivel lleva su mundo y su agua).
            const string Open = "\"levels\": [ ";
            const string Close = " ] }";
            Assert.That(first.EndsWith(Close, System.StringComparison.Ordinal), Is.True);
            var json = new System.Text.StringBuilder(first, 0, first.Length - Close.Length, first.Length * count);
            for (int k = 1; k < count; k++)
            {
                string other = Pack(level + k, palette, board);
                int from = other.IndexOf(Open, System.StringComparison.Ordinal) + Open.Length;
                json.Append(", ").Append(other, from, other.Length - Close.Length - from);
            }

            return json.Append(Close).ToString();
        }

        /// <summary>
        /// Paquete de un nivel con 15 cuias, en 3 filas (D-006): 6 parejas de cuias con dos frutas cruzadas (2 + 2 dosis) y 3
        /// vacías, que el solver resuelve enseguida.
        /// </summary>
        internal static string FifteenCuiaPack(int level)
        {
            var board = new int[15][];
            for (int p = 0; p < 6; p++)
            {
                int a = (2 * p) + 1;
                int b = (2 * p) + 2;
                board[2 * p] = new[] { a, a, b, b };
                board[(2 * p) + 1] = new[] { b, b, a, a };
            }

            for (int i = 12; i < 15; i++)
            {
                board[i] = new int[0];
            }

            return Pack(level, Fruits, board);
        }

        private static readonly string[] WorldIds = { string.Empty, "w01_estuario", "w02_tapajos", "w03_manaos", "w04_trapecio", "w05_loreto" };
        private static readonly string[] Waters = { string.Empty, "white", "clear", "black", "white", "white" };

        private static string Pack(int level, string[] palette, int[][] board)
        {
            int world = WorldTable.WorldOf(level);
            Assert.That(world, Is.InRange(1, WorldIds.Length - 1));
            return PlayModeHelpers.BoardPack(world, WorldIds[world], Waters[world], level, palette, board);
        }

        internal static string F(float v)
        {
            return v.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
