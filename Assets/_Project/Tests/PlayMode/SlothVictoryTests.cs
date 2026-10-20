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
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// TiTino en la victoria (tarea TI6a; propuesta de TiTino d.4 y paso 6a de (i); 02 §6, fila TiTino; 07 §5.4; T6-A; D-016):
    /// <list type="bullet">
    /// <item>la caja: ≥ 104 dp, gesto y bocadillo; 56-103, solo el gesto; menos, nada; TiTino se encoge para caber entero con
    /// todo lo que mueve su gesto. Con la victoria con vista (el default; DC2, pregunta 53 a de D-028) la caja es la franja de la
    /// cabecera a la cinta sin el héroe del sitio (SlothVictoryBandTests) y las filas no cuentan; sin ella, la de TI6a, y con 3
    /// filas no sale;</item>
    /// <item>SlothHito por defecto: en 40, 60, 80 y 140 habla si cabe (y la llegada va sin dato) y, si no, el hito lleva su dato;
    /// con los mensajes apagados, solo el gesto; con <c>ui_sloth_enabled</c> apagado, nada;</item>
    /// <item>tiempos: la rama empieza a entrar al abrirse la pantalla de victoria (la fila 0,80 [P]: 07 §5.4 dice 0,60, pero
    /// la caja depende del panel, que se coloca ahí; pendiente del dueño) y entra en 0,3 s; durante la celebración no está;
    /// el bocadillo, a los 1,10 s y se va a los 4 s o con Siguiente, que sigue a los 1,40 s; con Reducir movimiento, sin
    /// deslizar ni fundidos;</item>
    /// <item>TiTino se va en todas las salidas de la victoria: el nivel siguiente y «El río sigue creciendo» tras AD_GATE;</item>
    /// <item>ninguna intersección de TiTino ni del bocadillo con la cinta, las cuias (con su elevación), el saldo, el panel (la
    /// Cosecha, la ranura y los botones), la cabecera ni el héroe del sitio, a 360 × 640 y 360 × 780, con 1 y 3 filas, al 100 %
    /// y al 130 %, en todos los fotogramas de la victoria;</item>
    /// <item>sin toques: ningún gráfico es <c>raycastTarget</c>.</item>
    /// </list>
    /// <para>Salvo <see cref="RealHitos_SpeakIfItFits_ElseTheArrivalKeepsItsFact"/>, que mide los hitos del catálogo real con
    /// la ranura y sin ella, las victorias van sin la ranura del Dato del río (<see cref="GameScreen.ArrivalDisabledForTests"/> y
    /// <see cref="GameScreen.FactLineDisabledForTests"/>) y con tableros de una fila: son las únicas en las que la caja da para
    /// TiTino en TI6a. En los hitos reales (40, 60, 80 y 140, tableros de 2 filas), ShiftToClear sube las cuias hasta la cabecera
    /// y la caja de TI6a medía 0 dp (<see cref="GameScreen.SlothBoxUnclampedDp"/>); desde DC2 (pregunta 53 a de D-028), con la
    /// victoria con vista, TiTino usa la franja de la cabecera a la cinta y sale en sus hitos (SlothVictoryBandTests).</para>
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
                // Con el perfil nuevo (sin frase vista del momento), el fin de mundo rota con el número de mundo (D-030, pregunta
                // 16 b): el 40, la de siempre; el 80, la 1.ª alternativa; el 140, la 2.ª.
                string key = level == 60 ? "sloth_line_milestone_02"
                    : level == 80 ? "sloth_line_milestone_03"
                    : level == 140 ? "sloth_line_milestone_04"
                    : "sloth_line_milestone_01";
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
            AssertBalancedLines(sloth, "40");
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
        public IEnumerator Heron_FadesWhileTheSlothIsShown_AndComesBackWhenItLeaves()
        {
            // Revisión de TI6a: la garza blanca del fondo cae detrás de TiTino o bajo sus pies y las dos siluetas se funden.
            yield return WinPack(OneRowPack(40, 2), 40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothVictory sloth = view.SlothView;
            Assert.That(sloth.Visible, Is.True);
            Assert.That(sloth.ClockMs, Is.EqualTo(0f));
            Assert.That(view.HeronAlpha, Is.EqualTo(1f), "al abrirse la victoria, aún entera");
            PlayModeHelpers.TickExact(boot, (int)(GameScreen.HeronFadeMs / 2f));
            Assert.That(view.HeronAlpha, Is.InRange(0.3f, 0.7f), "a mitad del fundido");
            PlayModeHelpers.TickExact(boot, (int)GameScreen.HeronFadeMs);
            Assert.That(view.HeronAlpha, Is.EqualTo(0f), "con TiTino a la vista, la garza no está");
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.HeronAlpha, Is.EqualTo(0f));

            view.NextButton.onClick.Invoke();
            for (int t = 0; t < 5000 && !(boot.Flow!.LevelNumber == 41 && VictorySetup.InNewLevel(boot.Flow.Phase)); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(41));
            Assert.That(sloth.Visible, Is.False);
            Assert.That(view.HeronAlpha, Is.EqualTo(1f), "TiTino se fue: la garza vuelve");
        }

        [Test]
        public void Facts_AreTheVictorysOwn()
        {
            // D-016, punto 5: la cadencia (stats.levels_won con esta victoria; TI6b), Difícil o Muy difícil, el
            // estreno, la Cuia de rescate y Morpho (FX8). La Crecida aún no existe en el cliente.
            SlothVictoryFacts f = GameScreen.SlothFactsFor(264, 264, LevelDefinition.LabelHard, null, true, true, false);
            Assert.That(f.WinCount, Is.EqualTo(264));
            SlothVictoryFacts counted = GameScreen.SlothFactsFor(264, 7, LevelDefinition.LabelNormal, null, false, true, false);
            Assert.That(counted.WinCount, Is.EqualTo(7), "la cadencia es stats.levels_won, no el nivel ganado (TI6b)");
            Assert.That(GameScreen.SlothFactsFor(201, 3, null, null, false, true, false).Debut, Is.EqualTo(SlothDebut.Fruit),
                "el estreno sí es del nivel ganado");
            Assert.That(f.Hard, Is.True);
            Assert.That(f.UsedRescue, Is.True);
            Assert.That(f.SpeakBox, Is.True);
            Assert.That(f.Streak, Is.EqualTo(0));
            Assert.That(f.StreakMilestone, Is.False);
            Assert.That(f.UsedHint, Is.False);
            Assert.That(GameScreen.SlothFactsFor(264, 264, null, null, false, true, false, true).UsedHint, Is.True, "Morpho en este nivel (FX8)");
            Assert.That(GameScreen.SlothFactsFor(264, 264, LevelDefinition.LabelVeryHard, null, false, true, false).Hard, Is.True);
            Assert.That(GameScreen.SlothFactsFor(264, 264, LevelDefinition.LabelNormal, null, false, true, false).Hard, Is.False);
            Assert.That(GameScreen.SlothFactsFor(264, 264, null, null, false, true, false).Hard, Is.False);

            Assert.That(GameScreen.SlothDebutOf(283, "hoja_atada"), Is.EqualTo(SlothDebut.Mechanic));
            Assert.That(GameScreen.SlothDebutOf(201, "liana"), Is.EqualTo(SlothDebut.Mechanic), "la mecánica, primero");
            Assert.That(GameScreen.SlothDebutOf(201, null), Is.EqualTo(SlothDebut.Fruit), "Fruta nueva en el 201");
            Assert.That(GameScreen.SlothDebutOf(421, null), Is.EqualTo(SlothDebut.World), "primer nivel del mundo 8");
            Assert.That(GameScreen.SlothDebutOf(561, string.Empty), Is.EqualTo(SlothDebut.World));
            Assert.That(GameScreen.SlothDebutOf(264, null), Is.EqualTo(SlothDebut.None));

            // Con la ranura vacía (sin fruta ni llegada), 1 de cada 4 y del banco más específico.
            SlothContext c = new SlothSession(new SlothData(), true, true, true, 4, 4).Context;
            SlothLine hard = SlothLines.ForVictory(264, default(VictoryLine), c.WithVictory(in f));
            Assert.That(hard.Speaks, Is.True, "264: le toca y es Difícil");
            Assert.That(hard.Moment, Is.EqualTo(SlothMoment.Hard));
            SlothVictoryFacts normal = GameScreen.SlothFactsFor(265, 265, LevelDefinition.LabelNormal, null, false, true, false);
            Assert.That(SlothLines.ForVictory(265, default(VictoryLine), c.WithVictory(in normal)).Speaks, Is.False, "265: no le toca");
            SlothVictoryFacts debut = GameScreen.SlothFactsFor(324, 324, LevelDefinition.LabelNormal, "tinaja", false, true, false);
            Assert.That(SlothLines.ForVictory(324, default(VictoryLine), c.WithVictory(in debut)).Moment, Is.EqualTo(SlothMoment.Debut));
        }

        [UnityTest]
        public IEnumerator NonHito_WithoutFruitLine_SpeaksOneInFour()
        {
            // D-016, punto 5 (y 02 §6, fila TiTino): fuera de sus hitos, frase en 1 de cada 4 victorias si la victoria no lleva otra
            // línea. Hoy pasa en 261-300 y 321-700, sin serie de fruta; aquí, el 44 y el 45 sin la línea de fruta.
            yield return WinPack(OneRowPack(44), 44, v => v.FruitKeyOf = null);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            SlothVictory sloth = view.SlothView;
            Assert.That(view.CurrentVictoryLine.FruitKey, Is.Null);
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.False, "no es su hito: la línea no es del árbitro");
            Assert.That(view.LastSlothLine.Speaks, Is.True, "el 44 le toca: habla");
            Assert.That(view.LastSlothLine.Moment, Is.EqualTo(SlothMoment.Win));
            Assert.That(sloth.Pose, Is.EqualTo(SlothPoseId.Cheer));
            Assert.That(sloth.BubbleShown, Is.True);
            Assert.That(sloth.BubbleText, Is.EqualTo(Loc.T(view.LastSlothLine.Key!)));
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(1));
            AssertBalancedLines(sloth, "44");
            AssertNoIntersection(view, "44");

            yield return WinPack(OneRowPack(45), 45, v => v.FruitKeyOf = null);
            view = GameBootstrap.Current!.View!;
            PlayModeHelpers.Step(GameBootstrap.Current, 1600);
            Assert.That(view.SlothView.Visible, Is.True);
            Assert.That(view.LastSlothLine.Speaks, Is.False, "el 45 no le toca: solo el gesto");
            Assert.That(view.SlothView.HasBubble, Is.False);

            // Con la línea de fruta, el 44 es solo el gesto (una sola línea por victoria).
            yield return WinPack(OneRowPack(44), 44);
            view = GameBootstrap.Current!.View!;
            PlayModeHelpers.Step(GameBootstrap.Current, 1600);
            Assert.That(view.CurrentVictoryLine.FruitKey, Is.Not.Null);
            Assert.That(view.LastSlothLine.Speaks, Is.False, "con la línea de fruta, solo el gesto");
            Assert.That(view.SlothView.HasBubble, Is.False);
        }

        /// <summary>
        /// Pendiente de MO1 (tarea FX8): <c>usedHint</c> llega a TiTino desde las pistas de Morpho cobradas en el nivel
        /// (<see cref="LevelFlow.HintsUsed"/> &gt; 0). Sin pista, false; con una cobrada, true, y TiTino no dice las frases n.º 4 y 8
        /// del banco de victoria, que presumen de haberlo resuelto solo (propuesta de TiTino e.4; <see cref="SlothLines.ForVictory"/>).
        /// El 63 con 43 victorias antes es la victoria 44: le toca hablar, y con su semilla (63 + 44 = 107) sin pista elige la n.º 4
        /// (con el dato del color aprobado, 107 mod 8 = 3) o la 8 (sin él, 107 mod 7 = 6), así que la pista cambia la frase: si
        /// <c>usedHint</c> no llegara, la aserción con pista fallaría.
        /// </summary>
        [UnityTest]
        public IEnumerator UsedHint_IsTheChargedMorphoHints_AndTitinoSkipsTheSoloLines()
        {
            yield return WinPack(OneRowPack(63), 63, v => v.FruitKeyOf = null, 43);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(boot.Flow!.HintsUsed, Is.EqualTo(0));
            Assert.That(view.LastSlothFacts.UsedHint, Is.False, "sin pista");
            Assert.That(view.LastSlothFacts.WinCount, Is.EqualTo(44), "premisa: los datos son los de esta victoria");
            Assert.That(view.LastSlothLine.Speaks, Is.True, "premisa: victoria número 44, le toca");
            Assert.That(view.LastSlothLine.Key, Is.EqualTo("sloth_line_win_04").Or.EqualTo("sloth_line_win_08"),
                "premisa: sin pista, con esta semilla, una de las frases que presumen de haberlo resuelto solo");
            string unhinted = view.LastSlothLine.Key!;

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGameWithPack(OneRowPack(63), 63, new MemoryProgressStore(63, 62, levelsWon: 43));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            view = boot.View!;
            view.FruitKeyOf = null;
            LevelFlow flow = boot.Flow!;
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (flow.Phase != FlowPhase.Playing || !view.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.PressHint(), Is.True, "Morpho, desde el 12");
            float start = Time.realtimeSinceStartup;
            while (flow.HintThinking)
            {
                Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "Morpho no respondió");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                yield return null;
            }

            Assert.That(flow.LastHint!.Value.Charged, Is.True, "una pista cobrada");
            Assert.That(flow.HintsUsed, Is.EqualTo(1));
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(flow.HintsUsed, Is.EqualTo(1), "sigue contada en la victoria");
            Assert.That(view.LastSlothFacts.UsedHint, Is.True, "con una pista de Morpho cobrada");
            Assert.That(view.LastSlothFacts.WinCount, Is.EqualTo(44));
            Assert.That(view.LastSlothLine.Speaks, Is.True, "victoria número 44: le toca");
            Assert.That(view.LastSlothLine.Moment, Is.EqualTo(SlothMoment.Win));
            Assert.That(view.LastSlothLine.Key, Is.Not.EqualTo("sloth_line_win_04").And.Not.EqualTo("sloth_line_win_08"),
                "nunca tras Morpho las frases que presumen de haberlo resuelto solo");
            Assert.That(view.LastSlothLine.Key, Is.Not.EqualTo(unhinted), "la pista cambia la frase de la misma semilla");

            // La misma regla en SlothLines, sin la escena: con la misma semilla, sin pista elige la n.º 4 y tras Morpho otra.
            SlothContext c = new SlothSession(new SlothData(), true, true, true, 4, 4).Context;
            SlothVictoryFacts solo = GameScreen.SlothFactsFor(63, 44, LevelDefinition.LabelNormal, null, false, true, true, false);
            SlothLine soloLine = SlothLines.ForVictory(63, default(VictoryLine), c.WithVictory(in solo));
            Assert.That(soloLine.Speaks, Is.True);
            Assert.That(soloLine.Key, Is.EqualTo("sloth_line_win_04"), "premisa: sin pista, la n.º 4");
            SlothVictoryFacts hinted = GameScreen.SlothFactsFor(63, 44, LevelDefinition.LabelNormal, null, false, true, true, true);
            Assert.That(hinted.UsedHint, Is.True);
            SlothLine line = SlothLines.ForVictory(63, default(VictoryLine), c.WithVictory(in hinted));
            Assert.That(line.Speaks, Is.True);
            Assert.That(line.Key, Is.Not.EqualTo("sloth_line_win_04").And.Not.EqualTo("sloth_line_win_08"));
        }

        [UnityTest]
        public IEnumerator NonHito_TheCadenceCountsLevelsWon_NotTheLevel()
        {
            // Pendiente de TI6a (TI6b): la cadencia de 1 de cada 4 cuenta las victorias (stats.levels_won, que suma cada victoria),
            // no el número del nivel. El 45 con 43 victorias antes es la victoria 44: le toca; el 44 con 44 antes (la 45), no.
            yield return WinPack(OneRowPack(45), 45, v => v.FruitKeyOf = null, 43);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(boot.Flow!.LevelsWon, Is.EqualTo(44), "la victoria del 45 sumó una");
            Assert.That(view.LastSlothLine.Speaks, Is.True, "victoria número 44: le toca");
            Assert.That(view.SlothView.BubbleShown, Is.True);

            yield return WinPack(OneRowPack(44), 44, v => v.FruitKeyOf = null, 44);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(boot.Flow!.LevelsWon, Is.EqualTo(45));
            Assert.That(view.LastSlothLine.Speaks, Is.False, "victoria número 45: no le toca, aunque el nivel sea el 44");
            Assert.That(view.SlothView.HasBubble, Is.False);
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
        public IEnumerator ThreeRows_WithoutTheView_NoSloth_AndTheHitoKeepsItsFact()
        {
            // Sin la victoria con vista (ui_victory_view apagada), la caja de TI6a: con 3 filas TiTino no sale. Con la vista (el
            // default), sí: SlothVictoryBandTests (DC2).
            GameScreen.VictoryViewOverride = false;
            try
            {
                yield return WinPack(FifteenCuiaPack(40), 40);
                GameScreen view = GameBootstrap.Current!.View!;
                PlayModeHelpers.Step(GameBootstrap.Current, 1600);
                TestContext.WriteLine("caja en el 40 de 3 filas sin la vista: " + F(view.SlothBoxDp) + " dp");
                Assert.That(view.Layout!.Rows, Is.EqualTo(3));
                Assert.That(view.VictoryViewOn, Is.False);
                Assert.That(view.SlothView.Visible, Is.False, "con 3 filas TiTino no sale");
                Assert.That(view.SlothCouldSpeak, Is.False);
                Assert.That(view.CurrentVictoryLine.ArrivalFact, Is.True, "el hito lleva su dato");
            }
            finally
            {
                GameScreen.VictoryViewOverride = null;
            }
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
                        // Con la victoria con vista (el default; DC2), también con 3 filas: la caja va de la cabecera a la cinta.
                        Assert.That(view.VictoryViewOn, Is.True, at);
                        if (box >= GameScreen.SlothSpeakBoxDp)
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

                        if (!small && scale < 1.01f)
                        {
                            // La referencia: a 360 × 780 y al 100 %, con una fila o con 3, la caja da para el bocadillo y TiTino habla.
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
        /// los botones), los botones, el saldo ni las cuias con su elevación (donde están ahora; en la victoria con vista, desde que
        /// el tablero se empieza a ir, no), ni, en la victoria con vista, con el héroe del sitio del fondo (DC2), y no suben por
        /// encima del pie de la cabecera. Sin TiTino a la vista, no hay nada que comprobar.
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
            others.Add(("cabecera", Rect.MinMaxRect(-1000f, -1000f, 1000f, view.SlothHeaderBottomDp)));
            Rect hero = view.HeroRect;
            if (view.VictoryViewOn && hero.width > 0f && hero.height > 0f)
            {
                others.Add(("héroe del sitio", hero)); // victoria con vista (DC2)
            }

            // Con la victoria con vista ya empezada, las cuias se van (o ya se fueron) con el tablero (M2.2) y TiTino usa la franja
            // de la cabecera a la cinta (DC2): no cuentan. Con 10 cuias o menos la vista empieza al abrirse la victoria y la rama de
            // TiTino entra a la vez (la fila 0,80): durante el fundido del tablero (≤ 250 ms) se cruza con las cuias y el panel del
            // tablero, que se desvanecen, solapamiento marcado [P; DC2] en GameScreen.SlothShowVictory para que DC1 lo lleve a
            // 07 §5.4. Con 11-16 cuias TiTino espera a la vista: nunca se cruza con las cuias a la vista.
            bool boardLeaving = view.VictoryViewOn && view.VictoryViewWaitMs < 0f;
            BoardLayoutResult layout = view.Layout!;
            float shift = view.BoardContainer.anchoredPosition.y;
            for (int i = 0; i < (boardLeaving ? 0 : view.Views.Length); i++)
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

        // Con dos líneas, el bocadillo las equilibra (revisión de TI6a): nunca una palabra sola en la última.
        private static void AssertBalancedLines(SlothVictory sloth, string at)
        {
            TextMeshProUGUI label = sloth.BubbleLabel!;
            label.ForceMeshUpdate();
            TMP_TextInfo info = label.textInfo;
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < info.lineCount; i++)
            {
                TMP_LineInfo line = info.lineInfo[i];
                lines.Append(i == 0 ? string.Empty : " / ").Append(label.text, line.firstCharacterIndex, line.lastCharacterIndex - line.firstCharacterIndex + 1);
            }

            TestContext.WriteLine("AF_SLOTH_LINES " + at + ": " + lines + " (bocadillo de " + F(sloth.BubbleRect.width) + " dp)");
            Assert.That(info.lineCount, Is.EqualTo(sloth.BubbleLines), at + ": las líneas medidas son las que se ven");
            if (info.lineCount >= 2)
            {
                Assert.That(info.lineInfo[info.lineCount - 1].wordCount, Is.GreaterThan(1), at + ": sin una palabra sola en la última línea");
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

        /// <summary>
        /// Carga el paquete <paramref name="json"/> en el nivel <paramref name="level"/>, quita el reloj y gana hasta la victoria.
        /// Antes de ganarlo, <c>stats.levels_won</c> vale <paramref name="levelsWonBefore"/> o, si es negativo, el del juego lineal
        /// (<paramref name="level"/> − 1): la victoria es entonces la número <paramref name="level"/> (cadencia de TiTino, TI6b).
        /// </summary>
        internal static IEnumerator WinPack(string json, int level, System.Action<GameScreen>? before = null, int levelsWonBefore = -1)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            int won = levelsWonBefore >= 0 ? levelsWonBefore : level - 1;
            yield return PlayModeHelpers.LoadGameWithPack(json, level, new MemoryProgressStore(level, level - 1, levelsWon: won));
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
