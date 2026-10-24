#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
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
    /// Etiquetas de D-009 en el HUD y en Siguiente con el catálogo real (tarea T09; 02 §5 y §10; 07 §7.4 y §7.7;
    /// propuesta-mecanicas-dificultad.md §c.1 y §c.5), por los ganchos <c>Labels*</c> de GameScreen.Labels.cs: chip y marco
    /// ámbar en el 40; chip, marco índigo y velo del 15 % detrás del panel en el 60; nada en el 41; el chip gris-pardo «?» de
    /// Aguas turbias en el 100 y el 106; «Difícil ×3» y «Muy difícil ×4» sobre Siguiente al ganar el 39 y el 59; el contador
    /// de la Lluvia del 143 (solo baja con vertidos útiles, Q18 B; vuelve con Marea y con Volver a la orilla en el acto) y el
    /// de la Piraña del 183; y la banda sin solapes a 360 × 780 y en 16:9 (1080 × 1920 px, la pantalla de 360 × 640 dp de
    /// 02 §5) con 1, 2 y 3 filas.
    /// </summary>
    /// <remarks>
    /// GameScreen escala el lienzo a 360 dp de ancho con al menos 640 dp de alto (<see cref="GameScreen.CanvasScale"/>; D-028,
    /// tarea DC3): un teléfono 16:9 de 1080 × 1920 px da un lienzo de 360 × 640 dp, el de 02 §5 (antes de DC3, uno de
    /// 438,75 × 780 dp, con el texto al 82 %).
    /// </remarks>
    public sealed class HudLabelTests
    {
        /// <summary>SM-S938B: 360 × 780 dp con zona segura (32 dp arriba, 16 abajo).</summary>
        internal static readonly Vector4 Screen780 = new Vector4(1080f, 2340f, 96f, 48f);

        /// <summary>
        /// 16:9 (360 × 640 dp de 02 §5 a 3 px/dp) con la misma zona segura; GameScreen lo maqueta en 360 × 640 dp (ver la
        /// clase; tarea DC3).
        /// </summary>
        internal static readonly Vector4 Screen640 = new Vector4(1080f, 1920f, 96f, 48f);

        /// <summary>360 × 780 dp con un recorte de 48 dp arriba (02 §5: zona segura superior de hasta 48 dp).</summary>
        internal static readonly Vector4 Screen780Cutout = new Vector4(1080f, 2340f, 144f, 48f);

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = Screen780;
        }

        [TearDown]
        public void TearDown()
        {
            BoardLayout.TroopBandOffForTests = false;
            GameScreen.SimulatedScreen = null;
            GameScreen.DifficultyLightEnabled = true;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Hud_Level40_HardChipAndFrame()
        {
            yield return HudSetup.LoadAtIntro(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.HudDifficultyChip.Visible, Is.True, "el 40 es el primer Difícil (02 §10)");
            Assert.That(view.HudLabelsAlpha, Is.EqualTo(0f), "durante la intro no tapa la tropa de miquitos");

            HudSetup.SkipIntro(boot);
            Assert.That(view.HudLabelsAlpha, Is.EqualTo(1f).Within(0.001f), "aparece al terminar la intro");
            DifficultyChip chip = view.HudDifficultyChip;
            Assert.That(chip.Text, Is.EqualTo("Difícil"));
            Assert.That(chip.VeryHard, Is.False);
            Assert.That(chip.PillSprite!.name, Is.EqualTo("label_pill_hard"));
            Assert.That(chip.Icon!.name, Is.EqualTo("label_rana_dardo"));
            Assert.That(chip.SealShown, Is.True, "con sitio por encima del tablero, con su sello");
            Assert.That(chip.RibbonShown, Is.False, "en el HUD el chip va sin cinta");
            Assert.That(view.HudMurkyChip.Visible, Is.False);
            Assert.That(view.HudCounter.Visible, Is.False);

            Image frame = view.DifficultyFrame;
            Assert.That(frame.gameObject.activeInHierarchy && frame.enabled, Is.True, "marco ámbar (07 §7.7)");
            Assert.That(frame.color, Is.EqualTo((Color)Palette.UiAmber));
            HudSetup.AssertFrameAroundPanel(view);
            Assert.That(view.NightVeil.gameObject.activeSelf, Is.False, "el velo es solo de Muy difícil");

            // Bajo la insignia, en la banda entre la fila superior y el canto del panel (360 × 780, 2 filas).
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Full));
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect c = VictorySetup.CanvasRect(chip.Root, root);
            Rect badge = VictorySetup.CanvasRect(view.LevelBadgeRect, root);
            Assert.That(c.yMin, Is.GreaterThanOrEqualTo(view.TopRowDp + 48f), "debajo de la fila superior");
            Assert.That(c.yMax, Is.LessThanOrEqualTo(view.Layout!.PanelY), "por encima del canto del panel");
            Assert.That(c.center.x, Is.EqualTo(badge.center.x).Within(1f), "centrado bajo «Nivel 40»");
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertViewOnly(boot);
        }

        [UnityTest]
        public IEnumerator Hud_Level60_VeryHardNightVeil()
        {
            // Con la luz de dificultad apagada, el velo del 15 % del paso 1 (02 §10); con ella (B14), la LUT de noche lo sustituye
            // (MetaTests.VeryHardLevels_GetTheNightLut_AndHardOnes_TheAfternoonLight).
            GameScreen.DifficultyLightEnabled = false;
            yield return HudSetup.LoadPlaying(60);
            GameScreen view = GameBootstrap.Current!.View!;
            DifficultyChip chip = view.HudDifficultyChip;
            Assert.That(chip.Visible, Is.True, "el 60 es el primer Muy difícil (02 §10)");
            Assert.That(chip.Text, Is.EqualTo("Muy difícil"));
            Assert.That(chip.VeryHard, Is.True);
            Assert.That(chip.PillSprite!.name, Is.EqualTo("label_pill_very_hard"));
            Assert.That(chip.Icon!.name, Is.EqualTo("label_jaguar_head"));
            Assert.That(chip.TextColor, Is.EqualTo((Color)Palette.UiPaper), "marfil sobre índigo");
            Assert.That(chip.RibbonShown, Is.False);

            Image frame = view.DifficultyFrame;
            Assert.That(frame.gameObject.activeInHierarchy && frame.enabled, Is.True, "marco índigo (07 §7.7)");
            Assert.That(frame.color, Is.EqualTo((Color)Palette.UiIndigo));
            HudSetup.AssertFrameAroundPanel(view);

            // 02 §10: «fondo 15 % más oscuro», con un velo propio (no _sceneVeil) en la escena, detrás del panel.
            Image veil = view.NightVeil;
            Assert.That(veil.gameObject.activeInHierarchy, Is.True);
            Assert.That(veil.color.a, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(veil.color.r + veil.color.g + veil.color.b, Is.EqualTo(0f), "negro: oscurece sin teñir");
            Assert.That(veil.name, Is.EqualTo("NightVeil"));
            Assert.That(veil.raycastTarget, Is.False);
            Transform scene = veil.transform.parent;
            Transform canvasRoot = view.Canvas.transform;
            Assert.That(scene.parent, Is.SameAs(canvasRoot), "en la escena");
            Assert.That(scene.GetSiblingIndex(), Is.LessThan(view.BoardContainer.GetSiblingIndex()), "detrás del panel");
            Assert.That(veil.transform.GetSiblingIndex(), Is.GreaterThan(scene.Find("SceneVeil").GetSiblingIndex()), "su propio velo, delante del de la escena");
            RectTransform rt = veil.rectTransform;
            Assert.That(rt.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rt.anchorMax, Is.EqualTo(Vector2.one), "cubre todo el fondo");
            HudSetup.AssertHudLabelsClear(view);
        }

        [UnityTest]
        public IEnumerator Hud_Level41_None()
        {
            yield return HudSetup.LoadPlaying(41);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.HudDifficultyChip.Visible, Is.False, "normal: sin chip (02 §10)");
            Assert.That(view.HudMurkyChip.Visible, Is.False, "el 41 no es de Aguas turbias");
            Assert.That(view.HudCounter.Visible, Is.False);
            Assert.That(view.DifficultyFrame.gameObject.activeSelf, Is.False, "sin marco");
            Assert.That(view.NightVeil.gameObject.activeSelf, Is.False, "sin velo");
        }

        [UnityTest]
        public IEnumerator Hud_Level100_MurkyChip()
        {
            yield return HudSetup.LoadPlaying(100);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(GameBootstrap.Current.Flow!.Level.IsMurky, Is.True);
            Assert.That(view.HudDifficultyChip.Visible, Is.True);
            Assert.That(view.HudDifficultyChip.Text, Is.EqualTo("Muy difícil"));
            MurkyChip murky = view.HudMurkyChip;
            Assert.That(murky.Visible, Is.True, "07 §7.7: chip adicional gris-pardo con «?»");
            Assert.That(MurkyChip.PillColor, Is.EqualTo((Color)Palette.Murky), "#8A7B6A");
            Assert.That(murky.Pill.sprite!.name, Is.EqualTo("label_pill_murky"), "la píldora gris-pardo pintada");
            Color center = murky.Pill.sprite.texture.GetPixelBilinear(0.5f, 0.5f);
            Color murkyColor = Palette.Murky;
            Assert.That(Mathf.Abs(center.r - murkyColor.r) + Mathf.Abs(center.g - murkyColor.g) + Mathf.Abs(center.b - murkyColor.b), Is.LessThan(0.03f), "el centro de la cara es el gris-pardo: " + center);
            Assert.That(murky.GlyphSprite!.name, Is.EqualTo("mech_question"), "el «?» es un sprite, no texto");
            Assert.That(murky.Root.GetComponentsInChildren<TMPro.TMP_Text>(true), Is.Empty, "sin texto");
            Assert.That(MurkyChip.GlyphContrast(), Is.GreaterThanOrEqualTo(3f), "«?» marfil sobre gris-pardo ≥ 3:1");

            // 07 §7.8 (≥ 3:1 para un gráfico) contra la cara REAL que queda detrás de la tinta del «?», no contra el gris-pardo
            // plano: la píldora se aclara arriba (la luz de la pareja, el chip de dificultad) y el arco del «?» empieza hacia el
            // 15 % del alto. Se miden los píxeles del sprite en las filas de la tinta (sin contar el halo de tinta, que mejora el
            // borde): la luz fuerte se queda en la franja de encima del glifo y detrás solo se aclara 0,05.
            Assert.That(MurkyChip.GlyphFaceContrastMin(), Is.GreaterThanOrEqualTo(3f), "«?» marfil ≥ 3:1 frente a la cara real detrás de su tinta");
            Rect glyph = VictorySetup.CanvasRect(murky.GlyphRect, (RectTransform)view.Canvas.transform);
            Rect pill = VictorySetup.CanvasRect(murky.Root, (RectTransform)view.Canvas.transform);
            float inkTop = glyph.yMin + (glyph.height * (1f - (ProceduralArt.QuestionGlyphU / ProceduralArt.QuestionMarkU)) * 0.5f);
            Assert.That((inkTop - pill.yMin) / murky.Height, Is.EqualTo(MurkyChip.GlyphInkTopFraction).Within(0.01f), "la tinta empieza donde se mide");
            HudSetup.AssertMurkyBesideChip(view);
            HudSetup.AssertHudLabelsClear(view);
            Assert.That(view.NightVeil.gameObject.activeSelf, Is.False, "el 100 es Muy difícil: su noche es la LUT del fondo (B14), sin el velo");
            Assert.That(view.StopBackdrop.Light, Is.EqualTo(StopBackdrop.NightLut));
        }

        [UnityTest]
        public IEnumerator Hud_Level106_MurkyChip()
        {
            // La banda pegada bajo la placa con 3 filas: desde D-046 (punto 3), solo donde no cabe la franja de la tropa (16:9);
            // aquí, con el tablero de antes en el mismo lienzo. Con la franja, Hud_ThreeRows_WithTheTroopBand_GoFull.
            BoardLayout.TroopBandOffForTests = true; // el tablero de 3 filas de antes de D-046 (sin la franja de la tropa), el respaldo
            yield return HudSetup.LoadPlaying(106);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(GameBootstrap.Current.Flow!.Level.IsMurky, Is.True);
            Assert.That(view.HudMurkyChip.Visible, Is.True, "el 106 es de Aguas turbias");
            Assert.That(view.HudDifficultyChip.Visible, Is.True, "y Difícil");
            Assert.That(view.HudDifficultyChip.Text, Is.EqualTo("Difícil"));
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "11 cuias: 3 filas a 360 × 780 (02 §5)");
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Tucked), "pegado bajo la placa");
            Assert.That(view.HudDifficultyChip.Compact, Is.True);
            Assert.That(view.HudDifficultyChip.TextSize, Is.GreaterThanOrEqualTo(12f), "07 §7.7: 12-14 dp");

            // Pegado bajo la placa va sobre el panel que rodea las cuias: sin el sello de la rana (su naranja #D05802 está a
            // ΔE00 9,8 del Agua del Río Negro y del Achiote; NOTAS de la ola 2, «Para T09»). AssertHudLabelsClear mira el ΔE00
            // de lo que queda sobre el panel.
            Assert.That(view.HudDifficultyChip.SealShown, Is.False, "sin sello sobre el panel");
            HudSetup.AssertMurkyBesideChip(view);
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertFrameAroundPanel(view);
            Assert.That(view.DifficultyFrameTucked, Is.True, "3 filas: el panel pasa bajo la fila del HUD y el marco no asoma por arriba");
            HudSetup.AssertWalletOffPanel(view);
            Assert.That(view.WalletSqueezed, Is.True, "la cabecera se compacta dentro de la fila");

            // El 41 (normal, sin turbias) no lleva «?» aunque se cargue después en la misma escena.
            yield return HudSetup.LoadPlaying(41);
            Assert.That(GameBootstrap.Current!.View!.HudMurkyChip.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Hud_NormalLevelAfterTucked_ModeNone()
        {
            // Revisión de T09: del 106 (pegado bajo la placa) se pasa con Siguiente al 107 (normal, sin «?» ni contador) en la
            // misma GameScreen. La forma de la banda y la sonda AF_STATE hud_label dicen «none», no la del nivel anterior.
            BoardLayout.TroopBandOffForTests = true; // el tablero de 3 filas de antes de D-046 (sin la franja de la tropa), el respaldo
            yield return HudSetup.LoadPlaying(106);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Tucked));
            var lines = new List<string>();
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (message.Contains("\"event\":\"hud_label\""))
                {
                    lines.Add(message);
                }
            };
            Application.logMessageReceived += capture;
            try
            {
                VictorySetup.WinToVictoryScreen(boot);
                PlayModeHelpers.Step(boot, 900);
                Assert.That(boot.PressNext(), Is.True);
                PlayModeHelpers.Step(boot, 100);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }

            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(107));
            Assert.That(boot.Flow.Level.DifficultyLabel, Is.EqualTo("normal"));
            Assert.That(boot.Flow.Level.IsMurky, Is.False);
            Assert.That(boot.Flow.Session.Counter.IsActive, Is.False);
            Assert.That(view.HudDifficultyChip.Visible || view.HudMurkyChip.Visible || view.HudCounter.Visible, Is.False);
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.None), "sin etiquetas, ninguna forma");
            string? load107 = null;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("\"level\":107"))
                {
                    load107 = lines[i];
                }
            }

            Assert.That(load107, Is.Not.Null, "AF_STATE hud_label al cargar el 107");
            StringAssert.Contains("\"mode\":\"none\"", load107!, "la sonda no arrastra «tucked» del 106");
            StringAssert.Contains("\"label\":\"normal\"", load107);
            Assert.That(GameScreen.ModeKey(GameScreen.HudLabelMode.None), Is.EqualTo("none"));
        }

        [Test]
        public void ModeFor_NeedsRoomAboveTheVisibleEdgeOfTheBoard()
        {
            // Revisión de T09: la variante compacta en TopRowY + 50 (CompactBelow) tiene que quedar por encima del canto de arriba
            // visible del tablero, no solo de las cuias elevadas. Si el panel empieza a menos de 22 dp de la banda aunque las cuias
            // queden más abajo, va pegada bajo la placa (Tucked).
            const float row = 40f; // TopRowY: la banda normal en 90 y el pie de la fila del HUD en 88
            Assert.That(GameScreen.ModeFor(row, 130f, 152f), Is.EqualTo(GameScreen.HudLabelMode.Full));
            Assert.That(GameScreen.ModeFor(row, 122f, 144f), Is.EqualTo(GameScreen.HudLabelMode.Full), "30 + 2 dp desde TopRowY + 50");
            Assert.That(GameScreen.BandTopFor(GameScreen.HudLabelMode.Full, row, 122f, 144f), Is.EqualTo(90f));
            Assert.That(GameScreen.ModeFor(row, 121f, 143f), Is.EqualTo(GameScreen.HudLabelMode.CompactBelow));
            Assert.That(GameScreen.BandTopFor(GameScreen.HudLabelMode.CompactBelow, row, 121f, 143f), Is.EqualTo(90f), "en TopRowY + 50 si cabe");
            Assert.That(GameScreen.ModeFor(row, 110f, 140f), Is.EqualTo(GameScreen.HudLabelMode.CompactBelow), "20 + 2 dp desde el pie de la fila");
            Assert.That(GameScreen.BandTopFor(GameScreen.HudLabelMode.CompactBelow, row, 110f, 140f), Is.EqualTo(88f), "sube hasta el pie de la fila, no más");
            Assert.That(GameScreen.BandTopFor(GameScreen.HudLabelMode.CompactBelow, row, 111f, 140f), Is.EqualTo(89f), "2 dp sobre el canto");
            Assert.That(GameScreen.ModeFor(row, 109.9f, 140f), Is.EqualTo(GameScreen.HudLabelMode.Tucked), "con menos de 2 dp de aire, pegada bajo la placa");
            Assert.That(GameScreen.ModeFor(row, 105f, 130f), Is.EqualTo(GameScreen.HudLabelMode.Tucked), "el canto del panel manda aunque las cuias queden lejos");
            Assert.That(GameScreen.ModeFor(row, 130f, 105f), Is.EqualTo(GameScreen.HudLabelMode.Tucked), "y las cuias elevadas también");
            Assert.That(GameScreen.ModeFor(row, 80f, 101.5f), Is.EqualTo(GameScreen.HudLabelMode.Tucked), "3 filas: panel en TopRowY + 40");
            Assert.That(GameScreen.BandTopFor(GameScreen.HudLabelMode.Tucked, row, 80f, 101.5f), Is.EqualTo(76f), "pegada bajo la placa: TopRowY + 36");

            // FitSpan: un tramo fuera del cielo de la cuia del contador, lo más cerca posible de donde se quiere.
            Assert.That(GameScreen.FitSpan(50f, 99f, 56f, 192f, true, 112f, 164f, out float x), Is.True);
            Assert.That(x, Is.EqualTo(62f), "a la izquierda del cielo, lo más cerca posible de 99");
            Assert.That(GameScreen.FitSpan(60f, 99f, 56f, 192f, true, 112f, 164f, out x), Is.False, "no cabe ni a la izquierda (56) ni a la derecha (28)");
            Assert.That(GameScreen.FitSpan(20f, 145f, 56f, 192f, true, 112f, 164f, out x), Is.True);
            Assert.That(x, Is.EqualTo(164f), "a la derecha: 164 está más cerca de 145 que 92");
            Assert.That(GameScreen.FitSpan(20f, 120f, 56f, 192f, true, 112f, 164f, out x), Is.True);
            Assert.That(x, Is.EqualTo(92f), "a la izquierda: 92 está más cerca de 120 que 164");
            Assert.That(GameScreen.FitSpan(60f, 94f, 56f, 192f, false, 112f, 164f, out x), Is.True);
            Assert.That(x, Is.EqualTo(94f), "sin cielo que estorbe, donde se quiere");
        }

        [UnityTest]
        public IEnumerator Next_AfterWin39_ShowsHardX3()
        {
            yield return HudSetup.LoadPlaying(39);
            GameBootstrap boot = GameBootstrap.Current!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            GameScreen view = boot.View!;
            DifficultyChip next = view.NextChip;
            Assert.That(next.Visible, Is.True, "propuesta §c.1: el chip del nivel siguiente sobre Siguiente");
            Assert.That(next.Text, Is.EqualTo("Difícil"));
            Assert.That(next.RibbonText, Is.EqualTo("×3"), "60 / 20 (eco_reward_*)");
            Assert.That(next.VeryHard, Is.False);
            Assert.That(next.SealShown, Is.True);
            Assert.That(view.NextChipAlpha, Is.EqualTo(1f).Within(0.001f), "aparece con Siguiente");

            // Revisión de T09: la cinta es de madera oscura con la cifra crema, no del azul Agua de «Cosecha doble: x2 por
            // video», que va justo encima y con el que la «×3» se leía como parte de la oferta del video.
            Assert.That(next.RibbonSpriteShown!.name, Is.EqualTo("label_ribbon_wood"));
            Assert.That(DifficultyChip.RibbonColor, Is.EqualTo((Color)Palette.WoodDark), "#36261C");
            Assert.That(ColorMath.ContrastRatio(Palette.Cream, DifficultyChip.RibbonColor), Is.GreaterThanOrEqualTo(4.5f));
            Color body = next.RibbonSpriteShown.texture.GetPixelBilinear(0.5f, 0.55f);
            Assert.That(ColorMath.DeltaE00(body, DifficultyChip.RibbonColor), Is.LessThan(4f), "el cuerpo de la cinta es madera oscura: " + body);
            Assert.That(ColorMath.DeltaE00(body, Palette.FamilyWater.Deep), Is.GreaterThan(15f), "y no el agua profunda de la familia Agua");

            // La cifra lleva el contorno de madera de los títulos sobre placa, no el agua_contorno de los CTA Agua, que dejaba un
            // filo azul alrededor de la «×3» (capturas 40 y 47).
            Material? ribbonText = next.RibbonTextMaterial;
            Assert.That(ribbonText, Is.SameAs(UiFonts.Material(TextStyle.ModalTitle)), "contorno y sombra de madera");
            Assert.That(ribbonText, Is.Not.SameAs(UiFonts.Material(TextStyle.CtaWater)), "sin el contorno azul de la familia Agua");
            HudSetup.AssertNextChipClear(view);

            // Siguiente sigue funcionando y entra en el 40, que lleva su chip en el HUD.
            Assert.That(boot.PressNext(), Is.True);
            PlayModeHelpers.Step(boot, 100);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(40));
            Assert.That(view.HudDifficultyChip.Text, Is.EqualTo("Difícil"));
        }

        [UnityTest]
        public IEnumerator Next_AfterWin59_ShowsVeryHardX4()
        {
            yield return HudSetup.LoadPlaying(59);
            GameBootstrap boot = GameBootstrap.Current!;
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.View!.NextChipAlpha, Is.EqualTo(0f), "entra con Siguiente, a los 600 ms");
            PlayModeHelpers.Step(boot, 900);
            GameScreen view = boot.View!;
            DifficultyChip next = view.NextChip;
            Assert.That(next.Visible, Is.True);
            Assert.That(next.Text, Is.EqualTo("Muy difícil"));
            Assert.That(next.RibbonText, Is.EqualTo("×4"), "80 / 20 (eco_reward_*)");
            Assert.That(next.Icon!.name, Is.EqualTo("label_jaguar_head"));
            Assert.That(view.NextChipAlpha, Is.EqualTo(1f).Within(0.001f));
            HudSetup.AssertNextChipClear(view);

            // Ganar el 60 lleva al 61 (normal): sin chip sobre Siguiente.
            Assert.That(boot.PressNext(), Is.True);
            PlayModeHelpers.Step(boot, 100);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(60));
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            Assert.That(view.NextChip.Visible, Is.False, "el 61 es normal");
            Assert.That(HudSetup.GapAboveNext(view), Is.LessThan(10f), "sin chip el panel no crece: Siguiente va justo después");
        }

        [UnityTest]
        public IEnumerator Counter_Lluvia143_UsefulOnly()
        {
            yield return HudSetup.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            HudCounterChip counter = boot.View!.HudCounter;
            Assert.That(counter.Visible, Is.True, "la Lluvia lleva su contador en el HUD (traspaso §6 paso 1.3)");
            Assert.That(counter.Type, Is.EqualTo(MechanicType.Lluvia));
            Assert.That(counter.Icon!.name, Is.EqualTo("hud_counter_cloud"), "la nube sola: las únicas gotas del chip son las pendientes");
            Assert.That(counter.Remaining, Is.EqualTo(3), "lluvia_k_pours = 3");
            Assert.That(counter.Text, Is.EqualTo("3"));
            Assert.That(counter.DropsShown, Is.EqualTo(2), "n = 2 gotas pendientes");
            Assert.That(counter.DropColor, Is.EqualTo(Palette.Tint(10)), "el agua del mundo (Agua del Tapajós)");
            Assert.That(counter.Blocked, Is.False);
            HudSetup.AssertCounterGroups(counter);
            HudSetup.AssertCounterMaterial(counter);

            HudSetup.FindUsefulThenFutile(flow, out Pour useful, out Pour futile);
            Assert.That(HudSetup.Tap(boot, useful), Is.EqualTo(TapKind.Poured));
            Assert.That(counter.Remaining, Is.EqualTo(2), "baja con un vertido útil");
            Assert.That(flow.Session.Counter.Remaining, Is.EqualTo(2));
            PlayModeHelpers.StepUntilIdle(boot, 6000);

            Assert.That(PourRules.IsFutile(flow.Session.Current, futile.Source, futile.Target), Is.True);
            Assert.That(HudSetup.Tap(boot, futile), Is.EqualTo(TapKind.Poured));
            Assert.That(counter.Remaining, Is.EqualTo(2), "Q18 B: un vertido fútil no avanza el contador");
            PlayModeHelpers.StepUntilIdle(boot, 6000);

            Assert.That(boot.PressUndo(), Is.True);
            Assert.That(counter.Remaining, Is.EqualTo(2), "deshacer el fútil no cambia el contador");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(boot.PressUndo(), Is.True);
            Assert.That(counter.Remaining, Is.EqualTo(3), "Marea lo devuelve");
            Assert.That(counter.DropsShown, Is.EqualTo(2));
            Assert.That(flow.Session.Counter.Remaining, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Counter_Lluvia143_ResetsOnRestartImmediately()
        {
            yield return HudSetup.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            HudCounterChip counter = boot.View!.HudCounter;
            HudSetup.FindUsefulThenFutile(flow, out Pour useful, out Pour _);
            Assert.That(HudSetup.Tap(boot, useful), Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(counter.Remaining, Is.EqualTo(2));

            // Volver a la orilla: el contador vuelve a 3 en cuanto arranca la intro, sin esperar al siguiente vertido.
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(counter.Remaining, Is.EqualTo(3), "LabelsBoardChanged al final de OnIntroStarted");
            Assert.That(counter.DropsShown, Is.EqualTo(2));
            Assert.That(boot.View.HudLabelsAlpha, Is.EqualTo(0f), "oculto durante la intro del intento");
            HudSetup.SkipIntro(boot);
            Assert.That(counter.Remaining, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Counter_Pirana183()
        {
            yield return HudSetup.LoadPlaying(183);
            GameBootstrap boot = GameBootstrap.Current!;
            HudCounterChip counter = boot.View!.HudCounter;
            Assert.That(counter.Visible, Is.True);
            Assert.That(counter.Type, Is.EqualTo(MechanicType.Pirana));
            Assert.That(counter.Icon!.name, Is.EqualTo("mech_icon_pirana"));
            Assert.That(counter.Remaining, Is.EqualTo(4), "pirana_k_pours = 4");
            Assert.That(counter.Text, Is.EqualTo("4"));
            Assert.That(counter.DropsShown, Is.EqualTo(0), "sin gotitas en la Piraña");
            Assert.That(counter.Alpha, Is.EqualTo(1f), "la cueva no está llena");
            Assert.That(boot.View.HudDifficultyChip.Visible, Is.False, "el 183 es normal");
            HudSetup.AssertCounterMaterial(counter);
            HudSetup.AssertHudLabelsClear(boot.View);

            // Un vertido útil lo baja a 3.
            HudSetup.FindUsefulThenFutile(boot.Flow!, out Pour useful, out Pour _, false);
            Assert.That(HudSetup.Tap(boot, useful), Is.EqualTo(TapKind.Poured));
            Assert.That(counter.Remaining, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Counter_Lluvia143_DropFallsAfterKUsefulPours()
        {
            // 143: Lluvia con k = 3 y n = 2 (02 §9.1 M6). Tres vertidos útiles lejos de su cuia (para que no se llene): al
            // tercero cae una gota, así que quedan 1 gotita en el chip y el contador vuelve a 3 (el núcleo lo dice en el toque:
            // LabelsBoardChanged tras Poured).
            yield return HudSetup.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            HudCounterChip counter = boot.View!.HudCounter;
            int lluvia = flow.Session.Counter.CuiaIndex;
            int countBefore = flow.Session.Current[lluvia].Count;
            Assert.That(counter.Remaining, Is.EqualTo(3));
            Assert.That(counter.DropsShown, Is.EqualTo(2));

            int[] remainingAfter = { 2, 1, 3 };
            int[] dropsAfter = { 2, 2, 1 };
            for (int p = 0; p < 3; p++)
            {
                Pour useful = HudSetup.UsefulPourAvoiding(flow, lluvia);
                Assert.That(HudSetup.Tap(boot, useful), Is.EqualTo(TapKind.Poured), "vertido útil " + (p + 1));
                Assert.That(flow.Session.Counter.Remaining, Is.EqualTo(remainingAfter[p]), "núcleo tras el vertido útil " + (p + 1));
                Assert.That(counter.Remaining, Is.EqualTo(remainingAfter[p]), "chip tras el vertido útil " + (p + 1));
                Assert.That(counter.DropsShown, Is.EqualTo(dropsAfter[p]), "gotitas tras el vertido útil " + (p + 1));
                Assert.That(counter.Text, Is.EqualTo(remainingAfter[p].ToString(System.Globalization.CultureInfo.InvariantCulture)));
                PlayModeHelpers.StepUntilIdle(boot, 6000);
            }

            Assert.That(flow.Session.Counter.PendingDrops, Is.EqualTo(1), "cayó una de las dos gotas");
            Assert.That(flow.Session.Current[lluvia].Count, Is.EqualTo(countBefore + 1), "la gota está en la cuia de la Lluvia");
            Assert.That(counter.Blocked, Is.False);
            Assert.That(counter.Alpha, Is.EqualTo(1f));
            HudSetup.AssertCounterGroups(counter);
            HudSetup.AssertHudLabelsClear(boot.View);
        }

        [UnityTest]
        public IEnumerator Counter_Blocked_DimsTheChip()
        {
            // T09 §5: «atenuado si Blocked» (LevelCounter.Blocked: la cuia de la Lluvia o la cueva de la Piraña, llenas). Con
            // tableros sintéticos pintados en el chip del HUD del 143: la vista pinta el contador del núcleo tal cual.
            yield return HudSetup.LoadPlaying(143);
            GameBootstrap boot = GameBootstrap.Current!;
            HudCounterChip counter = boot.View!.HudCounter;
            Assert.That(counter.Alpha, Is.EqualTo(1f));

            MechanicSpec rain = new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 3);
            BoardState full = HudSetup.Board(
                new[] { new[] { 1, 2 }, new[] { 2 }, new[] { 3 }, new[] { 10, 1, 2, 3 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, rain });
            LevelCounter blocked = LevelCounter.Of(full);
            Assert.That(blocked.Blocked, Is.True, "Lluvia llena: la gota esperará");
            counter.Show(blocked, full[3].MechanicKey);
            Assert.That(counter.Blocked, Is.True);
            Assert.That(counter.Alpha, Is.EqualTo(HudCounterChip.BlockedAlpha), "atenuado");
            Assert.That(counter.DropsShown, Is.EqualTo(2), "las gotas pendientes se siguen viendo");
            Assert.That(counter.Remaining, Is.EqualTo(3));

            // Un vertido útil que deja hueco en la Lluvia (su cima, 3, sobre el 3 de la cuia 2): vuelve a opacidad 1.
            Assert.That(PourRules.IsLegal(full, 3, 2), Is.True);
            Assert.That(PourRules.IsFutile(full, 3, 2), Is.False);
            BoardState open = PourRules.Apply(full, 3, 2, PourMode.Run);
            LevelCounter free = LevelCounter.Of(open);
            Assert.That(free.Blocked, Is.False);
            counter.Show(free, open[3].MechanicKey);
            Assert.That(counter.Blocked, Is.False);
            Assert.That(counter.Alpha, Is.EqualTo(1f), "con hueco, a opacidad 1");
            Assert.That(counter.Remaining, Is.EqualTo(free.Remaining));

            // La cueva de la Piraña llena: el mordisco se perderá (Q-C), atenuado igual; sin gotitas.
            BoardState cave = HudSetup.Board(
                new[] { new[] { 1, 2 }, new[] { 2 }, new int[0], new[] { 3, 1, 2, 3 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Pirana, k: 4) });
            LevelCounter bite = LevelCounter.Of(cave);
            Assert.That(bite.Blocked, Is.True);
            counter.Show(bite, 0);
            Assert.That(counter.Type, Is.EqualTo(MechanicType.Pirana));
            Assert.That(counter.Alpha, Is.EqualTo(HudCounterChip.BlockedAlpha));
            Assert.That(counter.DropsShown, Is.EqualTo(0));
            Assert.That(counter.Divider.gameObject.activeSelf, Is.False, "sin gotitas no hay filete");
        }

        [UnityTest]
        public IEnumerator Layout_NoOverlap_360x640_ThreeRows()
        {
            // 360 × 780, nivel 200 (Muy difícil con la Piraña y 11 cuias): 3 filas, pegado bajo la placa con el contador.
            BoardLayout.TroopBandOffForTests = true; // el tablero de 3 filas de antes de D-046 (sin la franja de la tropa), el respaldo
            yield return HudSetup.LoadPlaying(200);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Tucked));
            Assert.That(view.HudDifficultyChip.Visible && view.HudCounter.Visible, Is.True);
            HudSetup.AssertHudLabelsClear(view);

            // Revisión de T09: el contador no va en el grupo de las etiquetas («Muy difícil 4» se leía como la cinta «×4»).
            // Pegado bajo la placa va a ≥ 16 dp del chip, con su material propio (cara clara, cifra en tinta, contorno de madera
            // oscura y la piraña delante de la cifra). A la derecha de la banda no cabe: la píldora «Reiniciar» ocupa
            // TopRowY + 32..48. Y la banda queda ≥ 4 dp por encima de las cuias elevadas de la fila 1.
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect chipRect = VictorySetup.CanvasRect(view.HudDifficultyChip.Root, root);
            Rect counterRect = VictorySetup.CanvasRect(view.HudCounter.Root, root);
            Assert.That(counterRect.xMin - chipRect.xMax, Is.GreaterThanOrEqualTo(GameScreen.HudCounterGapDp - 0.5f), "el contador, aparte del chip: " + chipRect + " / " + counterRect);
            Assert.That(view.HudDifficultyChip.SealShown, Is.False, "sobre el panel, sin el sello del jaguar");
            HudSetup.AssertCounterMaterial(view.HudCounter);
            Rect restartPill = VictorySetup.CanvasRect((RectTransform)view.RestartButton.transform, root);
            Assert.That(restartPill.yMax, Is.GreaterThan(counterRect.yMin), "a la derecha, la banda pegada bajo la placa pisaría Reiniciar");

            // NOTAS de la ola 2 («Para T09») y revisión de T09: la píldora de Canoas «5/5» de la cabecera no pisa el canto del panel
            // recortado bajo el HUD (TopRowY + 40): la columna se compacta dentro de la fila (píldoras de 18 dp con 2 de hueco, de
            // TopRowY a TopRowY + 38) y queda 2 dp por encima del canto, alineada con la pausa, la insignia y Reiniciar; el marco
            // índigo no asoma por arriba (pasaría a trozos bajo la insignia, las píldoras y Reiniciar).
            Assert.That(view.Layout.PanelY, Is.EqualTo(view.TopRowDp + 40f).Within(0.01f), "panel recortado en BoardTop − 8");
            Assert.That(view.WalletBar.CanoesRect.gameObject.activeSelf, Is.True, "Canoas desde el 31");
            Assert.That(view.WalletSqueezed, Is.True, "compactada en la fila");
            Assert.That(VictorySetup.CanvasRect(view.WalletBar.CanoesRect, root).yMax, Is.EqualTo(view.TopRowDp + 38f).Within(0.05f), "Canoas acaba 2 dp sobre el canto");
            Assert.That(view.DifficultyFrameTucked, Is.True);
            HudSetup.AssertFrameAroundPanel(view);
            HudSetup.AssertWalletOffPanel(view);

            // Sigue compactada tras un cambio de fase (SyncChrome la vuelve a poner en la fila a su tamaño): Volver a la orilla, en
            // la intro y de nuevo en juego.
            Assert.That(boot.PressRestart(), Is.True);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            HudSetup.AssertWalletOffPanel(view);
            HudSetup.SkipIntro(boot);
            HudSetup.AssertWalletOffPanel(view);

            // Con un recorte de 48 dp arriba (la fila superior baja 16 dp) sigue sin solapes.
            GameScreen.SimulatedScreen = Screen780Cutout;
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.TopRowDp, Is.EqualTo(56f).Within(0.01f));
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Tucked));
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertWalletOffPanel(view);

            // El mismo nivel en 16:9 (360 × 640 dp desde DC3): 2 filas de 6 y 5 (D-006; 02 §5: «ganan las 2 filas con 11-12 cuias
            // generadas») y el contador a la derecha de la banda.
            GameScreen.SimulatedScreen = Screen640;
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.CanvasWidth, Is.EqualTo(360f).Within(0.01f));
            Assert.That(view.Layout!.Rows, Is.EqualTo(2));
            Assert.That(view.HudLabelsMode, Is.Not.EqualTo(GameScreen.HudLabelMode.Tucked));
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertFrameAroundPanel(view);
            HudSetup.AssertWalletOffPanel(view);

            // 2 filas con «?» en 16:9 (nivel 100: 10 cuias, Muy difícil y Aguas turbias).
            yield return HudSetup.LoadPlaying(100);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(2));
            HudSetup.AssertMurkyBesideChip(view);
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertWalletOffPanel(view);

            // Contador y etiqueta con la Lluvia en la fila 1 y una cuia alta: el 160 (Muy difícil, 10 cuias) en 16:9. En el lienzo
            // de 438,75 × 780 dp de antes de DC3, ni la banda normal ni la compacta cabían sobre el marco y el contador pasaba a la
            // derecha de la insignia, bajo la cabecera compactada. Desde DC3 (360 × 640 dp; D-028) el tablero es otro: la banda va
            // pegada bajo la placa y el código elige dónde va el contador (junto al grupo o junto a la cabecera); se exige que
            // nada se cruce y que la cabecera quede fuera del panel.
            yield return HudSetup.LoadPlaying(160);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.HudCounter.Visible && view.HudDifficultyChip.Visible, Is.True);
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Tucked));
            TestContext.WriteLine("el 160 en 16:9: contador junto a la cabecera " + view.HudCounterBesideWallet + ", cabecera compactada "
                + view.WalletSqueezed + ", sello " + view.HudDifficultyChip.SealShown);
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertWalletOffPanel(view);

            // 3 filas en 16:9: 16 cuias (6/5/5) en un nivel Muy difícil (paquete de prueba).
            LaunchOptions.LevelPackJsonOverride = HudSetup.SixteenCuiaVeryHardPack();
            yield return HudSetup.LoadPlaying(1);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.Views.Length, Is.EqualTo(16));
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Tucked));
            Assert.That(view.HudDifficultyChip.Visible, Is.True);
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertFrameAroundPanel(view);
            HudSetup.AssertWalletOffPanel(view);

            // Y a 360 × 780, con las mismas 16 cuias.
            GameScreen.SimulatedScreen = Screen780;
            PlayModeHelpers.Step(GameBootstrap.Current!, 50);
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertFrameAroundPanel(view);
            HudSetup.AssertWalletOffPanel(view);

            // 1 fila (5 cuias, Difícil y de Aguas turbias; paquete de prueba) en 16:9 y a 360 × 780: chips normales.
            GameScreen.SimulatedScreen = Screen640;
            LaunchOptions.LevelPackJsonOverride = HudSetup.OneRowHardMurkyPack();
            yield return HudSetup.LoadPlaying(1);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(1));
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Full));
            Assert.That(view.HudDifficultyChip.Visible && view.HudMurkyChip.Visible, Is.True);
            HudSetup.AssertMurkyBesideChip(view);
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertWalletOffPanel(view);
            GameScreen.SimulatedScreen = Screen780;
            PlayModeHelpers.Step(GameBootstrap.Current!, 50);
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Full));
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertFrameAroundPanel(view);
            Assert.That(view.DifficultyFrameTucked, Is.False, "1 fila: el marco rodea el panel también por arriba");
            HudSetup.AssertWalletOffPanel(view);
        }

        [UnityTest]
        public IEnumerator Wallet_BackToFullSizeBeforePaint_WhenTheFunnelOpens()
        {
            // La cabecera compactada (3 filas) vuelve a escala 1 en cuanto SyncChrome pasa al saldo grande, también si lo hace desde
            // un evento posterior al Tick de la vista (aquí, «Salir» confirmado: fallo duro y Nivel fallido): WalletBar dibuja la
            // columna que le da LayoutWalletBar (con LabelsWalletColumn), así que ya está así antes de pintar ese fotograma. Al
            // reintentar, se compacta otra vez.
            BoardLayout.TroopBandOffForTests = true; // el tablero de 3 filas de antes de D-046 (sin la franja de la tropa), el respaldo
            yield return HudSetup.LoadPlaying(200);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.WalletSqueezed, Is.True);
            Assert.That(view.WalletBar.SeedsRect.localScale.x, Is.EqualTo(GameScreen.WalletSqueezeScale).Within(0.0001f));
            Assert.That(boot.PressQuit(), Is.True);
            Assert.That(boot.Flow!.Phase, Is.Not.EqualTo(FlowPhase.Playing));
            Assert.That(view.WalletBar.Compact, Is.False, "el saldo grande del embudo");
            Canvas.ForceUpdateCanvases(); // lo que pasa antes de pintar el fotograma
            Assert.That(view.WalletBar.SeedsRect.localScale.x, Is.EqualTo(1f), "el saldo grande, a su tamaño en el mismo fotograma");
            Assert.That(view.WalletBar.CanoesRect.localScale.x, Is.EqualTo(1f));
            Assert.That(view.WalletSqueezed, Is.False);

            PlayModeHelpers.Step(boot, 600);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed), "fallo duro con 5 canoas: Nivel fallido");
            Assert.That(boot.PressRetry(), Is.True);
            HudSetup.SkipIntro(boot);
            Assert.That(view.WalletBar.Compact, Is.True);
            Assert.That(view.WalletSqueezed, Is.True, "de vuelta al tablero de 3 filas, compactada");
            HudSetup.AssertWalletOffPanel(view);
        }

        [UnityTest]
        public IEnumerator Counter_Lluvia194_StaysOffTheLiftedCloud()
        {
            // Revisión de T09: en el 194 (normal, Lluvia en la cuia 1 de la fila 1, 3 filas a 360 × 780) el contador no tapa la
            // nube, ni en reposo ni con la cuia elevada al seleccionarla (16 dp más arriba): va a la izquierda de su cielo.
            BoardLayout.TroopBandOffForTests = true; // el tablero de 3 filas de antes de D-046 (sin la franja de la tropa), el respaldo
            yield return HudSetup.LoadPlaying(194);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Tucked));
            int rain = flow.Session.Counter.CuiaIndex;
            Assert.That(flow.Session.Counter.Type, Is.EqualTo(MechanicType.Lluvia));
            Assert.That(view.Layout.RowOf[rain], Is.EqualTo(0), "la Lluvia en la fila 1");
            HudSetup.AssertHudLabelsClear(view);

            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect before = VictorySetup.CanvasRect(view.HudCounter.Root, root);
            float u = view.Layout.CuiaWidth * 0.5f;
            Assert.That(before.xMax, Is.LessThanOrEqualTo(view.Layout.CenterX[rain] - u + 0.5f), "a la izquierda de la nube: " + before);

            // Con la cuia de la Lluvia seleccionada (elevada; empieza vacía, así que antes recibe un vertido), nada se mueve (D-002)
            // y su cielo sigue libre.
            Assert.That(HudSetup.FillIfEmpty(boot, rain), Is.True, "un vertido legal hacia la cuia de la Lluvia");
            HudSetup.AssertHudLabelsClear(view);
            before = VictorySetup.CanvasRect(view.HudCounter.Root, root);
            Assert.That(boot.TapCuia(rain).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 300);
            Assert.That(VictorySetup.CanvasRect(view.HudCounter.Root, root), Is.EqualTo(before), "D-002: seleccionar no mueve el contador");
            HudSetup.AssertHudLabelsClear(view);
            boot.TapCuia(rain);
        }

        [UnityTest]
        public IEnumerator Layout_NoOverlap_EveryLevel_360x780And16x9()
        {
            // Revisión de T09: barrido de los 200 niveles del catálogo a 360 × 780 y en 16:9, en la misma escena. En cada uno, el
            // HUD enseña lo que dice el nivel (chip con difficulty.label, «?» con IsMurky y contador con el del núcleo) y, sin
            // solapes (con el cielo de la cuia del contador elevada), el contador aparte del grupo de las etiquetas, la cabecera
            // sobre el canto y, sobre el panel, ni el sello ni colores a menos de ΔE00 10 de los tintes. Cubre los Difícil de 3
            // filas con Agua del Río Negro o Achiote (86-198) y los niveles con contador (143-200).
            yield return HudSetup.LoadPlaying(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            int labeled = 0;
            int tucked = 0;
            int counters = 0;
            var murkyLevels = new List<int>();
            for (int level = 1; level <= 200; level++)
            {
                Assert.That(flow.Catalog.Find(level), Is.Not.Null, "el catálogo tiene el nivel " + level);
                GameScreen.SimulatedScreen = Screen780;
                flow.Load(level);
                HudSetup.SkipIntro(boot);
                Assert.That(flow.LevelNumber, Is.EqualTo(level));
                string where = "nivel " + level + " a 360 × 780";
                string label = flow.Level.DifficultyLabel;
                bool hasLabel = label == "hard" || label == "very_hard";
                Assert.That(view.HudDifficultyChip.Visible, Is.EqualTo(hasLabel), "chip de dificultad: " + label + " (" + where + ")");
                Assert.That(view.HudMurkyChip.Visible, Is.EqualTo(flow.Level.IsMurky), "chip «?» (" + where + ")");
                Assert.That(view.HudCounter.Visible, Is.EqualTo(flow.Session.Counter.IsActive), "contador (" + where + ")");
                Assert.That(view.HudLabelsMode == GameScreen.HudLabelMode.None, Is.EqualTo(!hasLabel && !flow.Level.IsMurky && !flow.Session.Counter.IsActive),
                    "forma «none» solo sin etiquetas (" + where + ")");
                labeled += hasLabel ? 1 : 0;
                tucked += view.HudLabelsMode == GameScreen.HudLabelMode.Tucked ? 1 : 0;
                if (flow.Level.IsMurky)
                {
                    murkyLevels.Add(level);
                }

                HudSetup.AssertHudLabelsClear(view, where);
                HudSetup.AssertWalletOffPanel(view, where);
                if (view.HudCounter.Visible)
                {
                    counters++;
                    HudSetup.AssertCounterMaterial(view.HudCounter);
                }

                GameScreen.SimulatedScreen = Screen640;
                PlayModeHelpers.Step(boot, 50);
                where = "nivel " + level + " en 16:9";
                HudSetup.AssertHudLabelsClear(view, where);
                HudSetup.AssertWalletOffPanel(view, where);
            }

            Assert.That(labeled, Is.GreaterThanOrEqualTo(40), "niveles Difícil o Muy difícil entre el 40 y el 200");
            Assert.That(tucked, Is.EqualTo(0), "D-046: con la franja de la tropa, ningún nivel de 3 filas lleva la banda pegada bajo la placa a 360 × 780");
            Assert.That(counters, Is.GreaterThanOrEqualTo(15), "niveles con contador entre el 143 y el 200");
            Assert.That(murkyLevels, Is.EquivalentTo(new[] { 100, 106, 115, 135, 155, 175, 195 }), "los de Aguas turbias de T09 §2");
        }
    }

    /// <summary>Preparación y comprobaciones comunes de las pruebas y capturas del HUD de etiquetas (tarea T09).</summary>
    internal static class HudSetup
    {
        /// <summary>Carga el nivel del catálogo real con 5 canoas, quita el reloj y se queda en la intro.</summary>
        public static IEnumerator LoadAtIntro(int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 50);
        }

        /// <summary>Como <see cref="LoadAtIntro"/>, salta la intro y espera a que aparezcan las etiquetas.</summary>
        public static IEnumerator LoadPlaying(int level)
        {
            yield return LoadAtIntro(level);
            SkipIntro(GameBootstrap.Current!);
        }

        /// <summary>Salta la intro y avanza hasta Playing con la vista quieta y las etiquetas a la vista.</summary>
        public static void SkipIntro(GameBootstrap boot)
        {
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            PlayModeHelpers.Step(boot, 400); // fundido de entrada (180 ms) y el rótulo del intento, si lo hay
            for (int t = 0; t < 3000 && boot.View!.HudLabelsAlpha < 1f; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }
        }

        /// <summary>Selecciona y vierte <paramref name="pour"/>; devuelve el tipo del segundo toque.</summary>
        public static TapKind Tap(GameBootstrap boot, Pour pour)
        {
            Assert.That(boot.TapCuia(pour.Source).Kind, Is.EqualTo(TapKind.Selected));
            return boot.TapCuia(pour.Target).Kind;
        }

        /// <summary>
        /// Un vertido útil desde el tablero de ahora tras el que hay un vertido legal y fútil (02 §1.4 con la regla completa del
        /// núcleo, <see cref="PourRules.IsFutile"/>). Sin <paramref name="needFutile"/>, cualquier vertido útil.
        /// </summary>
        public static void FindUsefulThenFutile(LevelFlow flow, out Pour useful, out Pour futile, bool needFutile = true)
        {
            BoardState start = flow.Session.Current;
            PourMode mode = flow.Session.PourMode;
            int n = start.Count;
            Pour firstUseful = default;
            bool any = false;
            for (int a = 0; a < n; a++)
            {
                for (int b = 0; b < n; b++)
                {
                    if (a == b || !PourRules.IsLegal(start, a, b) || PourRules.IsFutile(start, a, b))
                    {
                        continue;
                    }

                    var u = new Pour(a, b, PourRules.Amount(start, a, b, mode));
                    if (!any)
                    {
                        firstUseful = u;
                        any = true;
                    }

                    BoardState next = PourRules.Apply(start, a, b, mode);
                    if (next.IsWon() || PourRules.IsStuck(next, flow.Session.FutileRuleEnabled))
                    {
                        continue;
                    }

                    for (int c = 0; c < n && needFutile; c++)
                    {
                        for (int d = 0; d < n; d++)
                        {
                            if (c != d && PourRules.IsLegal(next, c, d) && PourRules.IsFutile(next, c, d))
                            {
                                useful = u;
                                futile = new Pour(c, d, PourRules.Amount(next, c, d, mode));
                                return;
                            }
                        }
                    }
                }
            }

            Assert.That(any, Is.True, "no hay ningún vertido útil en el nivel " + flow.LevelNumber);
            Assert.That(needFutile, Is.False, "no hay un vertido útil seguido de uno fútil en el nivel " + flow.LevelNumber);
            useful = firstUseful;
            futile = default;
        }

        /// <summary>
        /// El marco rodea el panel 3 dp por fuera y va detrás del relleno del panel. Si el canto de arriba del panel pasa bajo
        /// la fila del HUD (la insignia acaba en <c>TopRowY + 50</c>; 3 filas), el marco empieza en el canto del panel y no asoma
        /// por arriba (recogido); si no, tampoco lo tapa nada de la fila del HUD.
        /// </summary>
        public static void AssertFrameAroundPanel(GameScreen view)
        {
            Image frame = view.DifficultyFrame;
            BoardLayoutResult l = view.Layout!;
            RectTransform board = view.BoardContainer;
            Assert.That(frame.transform.parent, Is.SameAs(board));
            bool tucked = l.PanelY - 3f < view.TopRowDp + GameScreen.HudLabelsBelowRowDp;
            Assert.That(view.DifficultyFrameTucked, Is.EqualTo(tucked), "recogido solo con el panel bajo la fila del HUD");
            float top = tucked ? l.PanelY : l.PanelY - 3f;
            Assert.That(view.DifficultyFrameTopDp, Is.EqualTo(top).Within(0.01f));
            RectTransform rt = frame.rectTransform;
            Assert.That(rt.anchoredPosition.x, Is.EqualTo(l.PanelX - 3f).Within(0.01f));
            Assert.That(-rt.anchoredPosition.y, Is.EqualTo(top).Within(0.01f));
            Assert.That(rt.sizeDelta.x, Is.EqualTo(l.PanelWidth + 6f).Within(0.01f), "3 dp a cada lado (07 §7.7)");
            Assert.That(rt.sizeDelta.y, Is.EqualTo(l.PanelHeight + (tucked ? 3f : 6f)).Within(0.01f), "3 dp por abajo (y por arriba si no va recogido)");
            if (!tucked)
            {
                Canvas.ForceUpdateCanvases();
                RectTransform root = (RectTransform)view.Canvas.transform;
                Assert.That(VictorySetup.CanvasRect(view.LevelBadgeRect, root).yMax, Is.LessThanOrEqualTo(top + 0.05f), "la insignia no tapa el canto de arriba del marco");
                Assert.That(VictorySetup.CanvasRect((RectTransform)view.RestartButton.transform, root).yMax, Is.LessThanOrEqualTo(top + 0.05f), "Reiniciar tampoco");
                Assert.That(VictorySetup.CanvasRect((RectTransform)view.PauseButton.transform, root).yMax, Is.LessThanOrEqualTo(top + 0.05f), "ni la pausa");
            }

            Assert.That(frame.transform.GetSiblingIndex(), Is.LessThan(board.Find("PanelFill").GetSiblingIndex()), "detrás del relleno: solo asoma alrededor");
            Assert.That(frame.transform.GetSiblingIndex(), Is.GreaterThan(board.Find("PanelShadow").GetSiblingIndex()), "sobre la sombra del panel");
            Assert.That(frame.raycastTarget, Is.False);
        }

        /// <summary>
        /// NOTAS de la ola 2 («Para T09») y revisión de T09: la columna de píldoras de la cabecera de partida (Semillas y, desde
        /// el 31, Canoas) acaba ≥ <see cref="GameScreen.WalletPanelClearDp"/> por encima del canto del panel y del canto de arriba
        /// del marco, si asoma, sin salir de la fila de 48 dp del HUD (07 §7.4; la columna normal acaba en TopRowY + 49), y sus
        /// cifras se leen a ≥ 12 dp también compactadas (02 §12).
        /// </summary>
        public static void AssertWalletOffPanel(GameScreen view, string where = "")
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            BoardLayoutResult l = view.Layout!;
            WalletBar wallet = view.WalletBar;
            Assert.That(wallet.Visible && wallet.Compact, Is.True, "la cabecera de partida " + where);
            Rect seeds = VictorySetup.CanvasRect(wallet.SeedsRect, root);
            bool canoes = wallet.CanoesRect.gameObject.activeSelf;
            Rect lowest = canoes ? VictorySetup.CanvasRect(wallet.CanoesRect, root) : seeds;
            float edge = l.PanelY;
            Image frame = view.DifficultyFrame;
            if (frame.gameObject.activeInHierarchy && frame.enabled)
            {
                edge = Mathf.Min(edge, view.DifficultyFrameTopDp);
            }

            string name = canoes ? "Canoas" : "Semillas";
            Assert.That(lowest.yMax, Is.LessThanOrEqualTo(edge - GameScreen.WalletPanelClearDp + 0.05f), name + " queda 2 dp por encima del canto del panel y del marco "
                + where + ": " + lowest + " / canto " + edge + " (compactada: " + view.WalletSqueezed + ")");
            Assert.That(seeds.yMin, Is.GreaterThanOrEqualTo(view.TopRowDp - 0.05f), "Semillas dentro de la fila del HUD " + where + ": " + seeds + " / " + view.TopRowDp);
            Assert.That(lowest.yMax, Is.LessThanOrEqualTo(view.TopRowDp + 49.05f), name + " dentro de la fila del HUD " + where);
            if (canoes)
            {
                Rect canoesRect = VictorySetup.CanvasRect(wallet.CanoesRect, root);
                Assert.That(canoesRect.yMin, Is.GreaterThanOrEqualTo(seeds.yMax + 1.9f), "Canoas bajo Semillas, con hueco " + where);
            }

            float scale = wallet.SeedsRect.localScale.y;
            Assert.That(wallet.CanoesRect.localScale.y, Is.EqualTo(scale), "las dos píldoras a la misma escala");
            TMPro.TMP_Text[] texts = wallet.SeedsRect.parent.GetComponentsInChildren<TMPro.TMP_Text>(false);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].ForceMeshUpdate();
                if (texts[i].text.Length > 0)
                {
                    Assert.That(texts[i].fontSize * scale, Is.GreaterThanOrEqualTo(11.99f), texts[i].name + " «" + texts[i].text + "» a ≥ 12 dp " + where);
                }
            }
        }

        /// <summary>
        /// Material del contador (revisión de T09): cara clara plana <see cref="HudCounterChip.FaceColor"/> con contorno de madera
        /// oscura, cifra en tinta y el icono de la mecánica de ≥ 16 dp de ancho delante de la cifra; distinto del ámbar, del
        /// índigo y del gris-pardo de las etiquetas.
        /// </summary>
        public static void AssertCounterMaterial(HudCounterChip counter)
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(counter.Plate.sprite!.name, Does.StartWith("hud_counter_plate"));
            Assert.That(counter.TextColor, Is.EqualTo((Color)Palette.Ink), "cifra en tinta #2B2118");
            Assert.That(ColorMath.ContrastRatio(Palette.Ink, HudCounterChip.FaceColor), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(HudCounterChip.OutlineColor, Is.EqualTo((Color)Palette.WoodDark), "contorno #36261C");
            Texture2D plate = counter.Plate.sprite.texture;
            Color face = plate.GetPixelBilinear(0.5f, 0.6f);
            Assert.That(ColorMath.DeltaE00(face, HudCounterChip.FaceColor), Is.LessThan(1f), "cara plana: " + face);
            Color edge = plate.GetPixel(plate.width / 2, plate.height - 2);
            Assert.That(ColorMath.DeltaE00(edge, HudCounterChip.OutlineColor), Is.LessThan(6f), "contorno de madera oscura arriba: " + edge);
            foreach (Color label in new[] { (Color)Palette.UiAmber, (Color)Palette.UiIndigo, (Color)Palette.Murky })
            {
                Assert.That(ColorMath.DeltaE00(HudCounterChip.FaceColor, label), Is.GreaterThan(20f), "otro material que las etiquetas");
            }

            RectTransform root = counter.Root;
            Rect icon = VictorySetup.CanvasRect(counter.IconRect, root);
            Rect value = VictorySetup.CanvasRect(counter.ValueRect, root);
            Assert.That(icon.width, Is.GreaterThanOrEqualTo(HudCounterChip.MinIconWidthDp - 0.01f), "icono de ≥ 16 dp");
            Assert.That(icon.xMax, Is.LessThanOrEqualTo(value.xMin + 0.05f), "la mecánica antes que la cifra");
        }

        /// <summary>
        /// En el chip de la Lluvia, la cifra (vertidos que faltan) y las gotitas (gotas pendientes) son dos grupos: entre ellos,
        /// <see cref="HudCounterChip.DropsGapDp"/> con el filete de 1 dp <c>muelle_sombra</c> en medio; la nube va a la izquierda
        /// de la cifra y las gotitas, a la derecha del filete.
        /// </summary>
        public static void AssertCounterGroups(HudCounterChip counter)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = counter.Root;
            Rect icon = VictorySetup.CanvasRect(counter.IconRect, root);
            Rect value = VictorySetup.CanvasRect(counter.ValueRect, root);
            Assert.That(icon.xMax, Is.LessThanOrEqualTo(value.xMin + 0.05f), "la nube a la izquierda de la cifra");
            Image divider = counter.Divider;
            if (counter.DropsShown == 0)
            {
                Assert.That(divider.gameObject.activeSelf, Is.False);
                return;
            }

            Assert.That(divider.gameObject.activeSelf, Is.True, "filete entre la cifra y las gotitas");
            Assert.That(divider.color, Is.EqualTo((Color)Palette.DockShadow), "#5A3D28");
            Assert.That(divider.raycastTarget, Is.False);
            Rect line = VictorySetup.CanvasRect(divider.rectTransform, root);
            Rect firstDrop = VictorySetup.CanvasRect(counter.Drop(0).rectTransform, root);
            Assert.That(line.width, Is.EqualTo(HudCounterChip.DividerDp).Within(0.01f));
            Assert.That(firstDrop.xMin - value.xMax, Is.EqualTo(HudCounterChip.DropsGapDp).Within(0.05f), "6 dp entre la caja de la cifra y la primera gotita");
            Assert.That(line.xMin - value.xMax, Is.EqualTo(firstDrop.xMin - line.xMax).Within(0.05f), "el filete en medio del hueco");
            Assert.That(line.center.y, Is.EqualTo(value.center.y).Within(0.5f));
            for (int i = 0; i < counter.DropsShown; i++)
            {
                Assert.That(counter.Drop(i).gameObject.activeSelf, Is.True);
                Assert.That(VictorySetup.CanvasRect(counter.Drop(i).rectTransform, root).xMin, Is.GreaterThanOrEqualTo(line.xMax), "gotita " + i + " tras el filete");
            }
        }

        /// <summary>
        /// Deja la cuia <paramref name="cuia"/> lista para seleccionarla: si no puede ser origen (vacía), le vierte desde la primera
        /// cuia con un vertido legal hacia ella y espera a que la vista quede quieta. False si no hay ninguno.
        /// </summary>
        public static bool FillIfEmpty(GameBootstrap boot, int cuia)
        {
            LevelFlow flow = boot.Flow!;
            BoardState board = flow.Session.Current;
            if (PourRules.CanBeSource(board, cuia))
            {
                return true;
            }

            for (int a = 0; a < board.Count; a++)
            {
                if (a != cuia && PourRules.IsLegal(board, a, cuia))
                {
                    Assert.That(Tap(boot, new Pour(a, cuia, PourRules.Amount(board, a, cuia, flow.Session.PourMode))), Is.EqualTo(TapKind.Poured));
                    PlayModeHelpers.StepUntilIdle(boot, 6000);
                    return PourRules.CanBeSource(flow.Session.Current, cuia);
                }
            }

            return false;
        }

        /// <summary>Un vertido útil (legal, no fútil, que no gana ni atasca) que no toca la cuia <paramref name="avoid"/>.</summary>
        public static Pour UsefulPourAvoiding(LevelFlow flow, int avoid)
        {
            BoardState start = flow.Session.Current;
            PourMode mode = flow.Session.PourMode;
            for (int a = 0; a < start.Count; a++)
            {
                for (int b = 0; b < start.Count; b++)
                {
                    if (a == b || a == avoid || b == avoid || !PourRules.IsLegal(start, a, b) || PourRules.IsFutile(start, a, b))
                    {
                        continue;
                    }

                    BoardState next = PourRules.Apply(start, a, b, mode);
                    if (!next.IsWon() && !PourRules.IsStuck(next, flow.Session.FutileRuleEnabled))
                    {
                        return new Pour(a, b, PourRules.Amount(start, a, b, mode));
                    }
                }
            }

            Assert.Fail("no hay un vertido útil lejos de la cuia " + avoid + " en el nivel " + flow.LevelNumber);
            return default;
        }

        /// <summary>Tablero sintético de cuias de 4 con <see cref="BoardBuilder"/> (sin contrastar U_c).</summary>
        public static BoardState Board(int[][] cuias, MechanicSpec[]? attrs)
        {
            var caps = new int[cuias.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = 4;
            }

            BoardBuildResult result = BoardBuilder.Build(caps, cuias, attrs);
            Assert.That(result.Error, Is.Null);
            return result.State!;
        }

        /// <summary>El «?» va a la derecha del chip de dificultad, en la misma banda y sin tocarlo.</summary>
        public static void AssertMurkyBesideChip(GameScreen view)
        {
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect murky = VictorySetup.CanvasRect(view.HudMurkyChip.Root, root);
            Rect chip = VictorySetup.CanvasRect(view.HudDifficultyChip.Root, root);
            Assert.That(murky.xMin, Is.GreaterThanOrEqualTo(chip.xMax), "a la derecha del chip de dificultad");
            float murkyFace = murky.yMin + (view.HudMurkyChip.Height * 0.5f);
            Assert.That(murkyFace, Is.EqualTo(chip.center.y).Within(1.5f), "en la misma banda");
        }

        /// <summary>
        /// Las etiquetas del HUD a la vista (chip, «?» y contador) quedan dentro del lienzo, sin tocarse entre sí, sin tapar la
        /// pausa, Reiniciar, los saldos, el texto «Nivel {n}», las cuias elevadas de la fila 1 ni el cielo de la cuia del
        /// contador con la cuia elevada (los 32 dp táctiles sobre la boca más los 16 de la elevación); y no reciben toques.
        /// Además (revisión de T09): en la banda normal y en la compacta no pisan el canto de arriba visible del tablero (el del
        /// marco si asoma, si no el del panel); pegadas bajo la placa quedan ≥ 4 dp por encima de las cuias elevadas; el contador
        /// va a ≥ 16 dp del grupo de las etiquetas; y lo que pisa el panel no lleva el sello y solo tiene superficies a ΔE00 ≥ 10
        /// de los 14 tintes (direccion-visual-v2 §2.4, regla 1), salvo el índigo de Muy difícil frente al Huito (C13, abierta: es
        /// también el color del marco de 3 dp de 07 §7.7, que rodea el tablero).
        /// </summary>
        public static void AssertHudLabelsClear(GameScreen view, string where = "")
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            var labels = new List<(string Name, Rect Rect)>();
            if (view.HudDifficultyChip.Visible)
            {
                labels.Add(("chip", VictorySetup.CanvasRect(view.HudDifficultyChip.Root, root)));
            }

            if (view.HudMurkyChip.Visible)
            {
                labels.Add(("«?»", VictorySetup.CanvasRect(view.HudMurkyChip.Root, root)));
            }

            if (view.HudCounter.Visible)
            {
                labels.Add(("contador", VictorySetup.CanvasRect(view.HudCounter.Root, root)));
            }

            var obstacles = new List<(string Name, Rect Rect)>
            {
                ("pausa", VictorySetup.CanvasRect((RectTransform)view.PauseButton.transform, root)),
                ("Reiniciar", VictorySetup.CanvasRect((RectTransform)view.RestartButton.transform, root)),
            };
            if (view.WalletBar.Visible && view.WalletBar.Compact)
            {
                obstacles.Add(("Semillas", VictorySetup.CanvasRect(view.WalletBar.SeedsRect, root)));
                if (view.WalletBar.CanoesRect.gameObject.activeSelf)
                {
                    obstacles.Add(("Canoas", VictorySetup.CanvasRect(view.WalletBar.CanoesRect, root)));
                }
            }

            // Texto «Nivel {n}»: la caja de la tinta de los glifos dibujados (sin el interlineado).
            obstacles.Add(("texto del nivel", GlyphRect(view.LevelTitle, root)));

            BoardLayoutResult l = view.Layout!;
            float u = l.CuiaWidth * 0.5f;
            LevelCounter levelCounter = GameBootstrap.Current!.Flow!.Session.Counter;
            int counterCuia = levelCounter.CuiaIndex;

            // El cielo de la cuia del contador es del ancho de la cuia, salvo con la Piraña: su cueva (2,4 u) sobresale 0,2 u por
            // lado sobre la boca, unos 4,7 dp (revisión de T09 y T17: con el cielo de la cuia ± 2 dp, en la fila 1 de los niveles
            // 184, 185, 187 y 200 un chip podía quedar sobre sus piedras).
            float skyHalf = levelCounter.IsActive && levelCounter.Type == MechanicType.Pirana
                ? Mathf.Max(1f, 0.5f * ProceduralArt.PiranaCaveWidthU) * u
                : u;
            float liftedTop = float.MaxValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                if (l.RowOf[i] != 0)
                {
                    continue;
                }

                float h = view.Views[i].HeightU * u;
                float lifted = l.FootY[i] - h - CuiaView.LiftDp;
                liftedTop = Mathf.Min(liftedTop, lifted);
                bool sky = i == counterCuia;
                float top = sky ? lifted - CuiaView.TouchHeadroomDp : lifted;
                float half = sky ? skyHalf : u;
                obstacles.Add(("cuia " + i + (sky ? " elevada con su cielo" : " elevada"), Rect.MinMaxRect(l.CenterX[i] - half, top, l.CenterX[i] + half, l.FootY[i])));
            }

            // Canto de arriba visible del tablero (el del marco si asoma por arriba; si no, el del panel).
            Image frame = view.DifficultyFrame;
            bool frameUp = frame.gameObject.activeInHierarchy && frame.enabled && !view.DifficultyFrameTucked;
            float edge = frameUp ? Mathf.Min(l.PanelY, view.DifficultyFrameTopDp) : l.PanelY;
            Rect board = Rect.MinMaxRect(l.PanelX - 3f, edge, l.PanelX + l.PanelWidth + 3f, l.PanelY + l.PanelHeight);
            bool tucked = view.HudLabelsMode == GameScreen.HudLabelMode.Tucked;
            if (labels.Count > 0)
            {
                Assert.That(view.HudLabelsMode, Is.Not.EqualTo(GameScreen.HudLabelMode.None), "con etiquetas, la banda tiene forma " + where);
            }

            if (!tucked)
            {
                obstacles.Add(("canto del tablero", board));
            }

            for (int i = 0; i < labels.Count; i++)
            {
                Rect r = labels[i].Rect;
                string name = labels[i].Name + (where.Length > 0 ? " (" + where + ")" : string.Empty);
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.5f), name + " dentro de la pantalla");
                Assert.That(r.xMax, Is.LessThanOrEqualTo(view.CanvasWidth + 0.5f), name + " dentro de la pantalla");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.TopRowDp), name + " bajo la zona segura");
                if (tucked)
                {
                    Assert.That(r.yMax, Is.LessThanOrEqualTo(liftedTop - GameScreen.HudLabelsTuckedClearDp + 0.05f), name + ": ≥ 4 dp sobre las cuias elevadas de la fila 1");
                }

                for (int j = i + 1; j < labels.Count; j++)
                {
                    Assert.That(Overlap(r, labels[j].Rect), Is.False, name + " y " + labels[j].Name + " se tocan: " + r + " / " + labels[j].Rect);
                }

                for (int j = 0; j < obstacles.Count; j++)
                {
                    Assert.That(Overlap(r, obstacles[j].Rect), Is.False, name + " tapa " + obstacles[j].Name + ": " + r + " / " + obstacles[j].Rect
                        + " (forma " + view.HudLabelsMode + ", TopRowY " + view.TopRowDp + ", canto " + edge + ", cuias elevadas " + liftedTop
                        + ", panel " + l.PanelY + ", marco " + view.DifficultyFrameTopDp + (view.DifficultyFrameTucked ? " recogido" : string.Empty) + ")");
                }
            }

            // El contador, aparte del grupo de las etiquetas (≥ 16 dp en la misma banda).
            if (view.HudCounter.Visible)
            {
                Rect counter = VictorySetup.CanvasRect(view.HudCounter.Root, root);
                for (int i = 0; i < labels.Count; i++)
                {
                    if (labels[i].Name == "contador")
                    {
                        continue;
                    }

                    Rect r = labels[i].Rect;
                    float gap = counter.xMin >= r.xMax ? counter.xMin - r.xMax : r.xMin - counter.xMax;
                    Assert.That(gap, Is.GreaterThanOrEqualTo(GameScreen.HudCounterGapDp - 0.5f), "el contador, a ≥ 16 dp de " + labels[i].Name + " " + where + ": " + counter + " / " + r);
                }
            }

            // Lo que pisa el panel: sin sello y a ΔE00 ≥ 10 de los tintes.
            var colors = new List<(string Name, Color Color)>();
            if (view.HudDifficultyChip.Visible && Overlap(VictorySetup.CanvasRect(view.HudDifficultyChip.Root, root), board))
            {
                Assert.That(view.HudDifficultyChip.SealShown, Is.False, "el sello de la etiqueta no va sobre el panel " + where);
                view.HudDifficultyChip.CollectSurfaceColors(colors);
            }

            if (view.HudMurkyChip.Visible && Overlap(VictorySetup.CanvasRect(view.HudMurkyChip.Root, root), board))
            {
                colors.Add(("píldora gris-pardo", MurkyChip.PillColor));
            }

            if (view.HudCounter.Visible && Overlap(VictorySetup.CanvasRect(view.HudCounter.Root, root), board))
            {
                view.HudCounter.CollectSurfaceColors(colors);
            }

            for (int c = 0; c < colors.Count; c++)
            {
                for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
                {
                    if (colors[c].Color == (Color)Palette.UiIndigo && tint == 9)
                    {
                        continue; // C13 (abierta): ui_indigo frente al Huito, el mismo par que el marco de 07 §7.7
                    }

                    float de = ColorMath.DeltaE00(colors[c].Color, Palette.Tint(tint));
                    Assert.That(de, Is.GreaterThanOrEqualTo(10f), colors[c].Name + " sobre el panel, a ΔE00 " + de.ToString("0.00") + " del tinte " + tint + " " + where);
                }
            }

            Graphic[] graphics = view.HudDifficultyChip.Root.parent.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Assert.That(graphics[i].raycastTarget, Is.False, graphics[i].name + " no recibe toques");
            }
        }

        /// <summary>
        /// El chip del nivel siguiente cuelga de Siguiente (revisión de T09; como la «x5» de Magic Sort, ficha §3.6): su parte alta
        /// 24 dp por encima del canto del botón y 6 dp montados sobre él, centrado, delante del botón y sin recibir toques (así
        /// Siguiente se toca también bajo esos 6 dp); dentro del panel de victoria, con ≥ 8 dp de aire bajo la Cosecha doble o la
        /// fila de la Cosecha y sin tocar Inicio, que sigue 68 dp bajo Siguiente.
        /// </summary>
        public static void AssertNextChipClear(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            DifficultyChip next = view.NextChip;
            Rect chip = VictorySetup.CanvasRect(next.Root, root);
            Rect nextButton = VictorySetup.CanvasRect((RectTransform)view.NextButton.transform, root);
            Rect home = VictorySetup.CanvasRect((RectTransform)view.HomeButton.transform, root);
            Rect panel = VictorySetup.CanvasRect(view.VictoryPanelRect, root);
            Assert.That(chip.yMin, Is.EqualTo(nextButton.yMin - GameScreen.NextChipHangDp).Within(0.6f), "24 dp por encima del canto de Siguiente");
            Assert.That(chip.yMax, Is.EqualTo(nextButton.yMin + GameScreen.NextChipOverlapDp).Within(0.6f), "y 6 dp montados sobre el botón");
            Assert.That(GameScreen.NextChipOverlapDp, Is.EqualTo(6f));
            Assert.That(Overlap(chip, home), Is.False, "no tapa Inicio");
            Assert.That(chip.center.x, Is.EqualTo(nextButton.center.x).Within(1f), "centrado sobre Siguiente");
            Assert.That(panel.Contains(chip.min) && panel.Contains(chip.max), Is.True, "dentro del panel de victoria");
            Assert.That(next.Root.parent, Is.SameAs(view.NextButton.transform.parent), "en el panel, junto a Siguiente");
            Assert.That(next.Root.GetSiblingIndex(), Is.GreaterThan(view.NextButton.transform.GetSiblingIndex()), "delante de Siguiente");
            Assert.That(view.NextChipGroup.blocksRaycasts, Is.False, "no recibe toques: Siguiente se toca también bajo el chip");
            Graphic[] graphics = next.Root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Assert.That(graphics[i].raycastTarget, Is.False, graphics[i].name + " no recibe toques");
            }

            Assert.That(home.yMin - nextButton.yMin, Is.EqualTo(68f).Within(0.5f), "Inicio, 68 dp bajo el borde del CTA");
            Assert.That(GapAboveNext(view), Is.EqualTo(GameScreen.NextChipRowDp).Within(4f), "el panel crece lo justo para el chip");
            Transform harvest = view.VictoryPanelRect.Find("Harvest");
            Rect harvestRow = VictorySetup.CanvasRect((RectTransform)harvest, root);
            Assert.That(chip.yMin, Is.GreaterThanOrEqualTo(harvestRow.yMax + 8f), "≥ 8 dp bajo la fila de la Cosecha");
            if (view.DoubleHarvestShown)
            {
                Rect dbl = VictorySetup.CanvasRect((RectTransform)view.DoubleHarvestButton.transform, root);
                Assert.That(chip.yMin - dbl.yMax, Is.GreaterThanOrEqualTo(8f), "≥ 8 dp bajo la Cosecha doble: el chip es de Siguiente");
            }
        }

        /// <summary>Hueco (dp) entre lo que va encima de Siguiente (la Cosecha doble o la fila de la Cosecha) y Siguiente.</summary>
        public static float GapAboveNext(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect nextButton = VictorySetup.CanvasRect((RectTransform)view.NextButton.transform, root);
            Rect above = view.DoubleHarvestShown
                ? VictorySetup.CanvasRect((RectTransform)view.DoubleHarvestButton.transform, root)
                : VictorySetup.CanvasRect((RectTransform)view.VictoryPanelRect.Find("Harvest"), root);
            return nextButton.yMin - above.yMax;
        }

        /// <summary>Ningún gráfico de las etiquetas recibe toques y el HUD sigue igual al seleccionar (D-002).</summary>
        public static void AssertViewOnly(GameBootstrap boot)
        {
            GameScreen view = boot.View!;
            Rect before = VictorySetup.CanvasRect(view.HudDifficultyChip.Root, (RectTransform)view.Canvas.transform);
            BoardState board = boot.Flow!.Session.Current;
            for (int i = 0; i < board.Count; i++)
            {
                if (PourRules.CanBeSource(board, i))
                {
                    Assert.That(boot.TapCuia(i).Kind, Is.EqualTo(TapKind.Selected));
                    PlayModeHelpers.Step(boot, 200);
                    Rect after = VictorySetup.CanvasRect(view.HudDifficultyChip.Root, (RectTransform)view.Canvas.transform);
                    Assert.That(after, Is.EqualTo(before), "D-002: seleccionar no cambia las etiquetas");
                    Assert.That(view.HudLabelsAlpha, Is.EqualTo(1f).Within(0.001f));
                    boot.TapCuia(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Paquete de prueba de 16 cuias (6/5/5 en 3 filas, D-006) Muy difícil: el de <see cref="PlayModeHelpers.SixteenCuiaPack"/>
        /// con <c>difficulty.label</c> = <c>very_hard</c> (sus cuias van llenas, así que no puede ser de Aguas turbias: 02 §11 T3).
        /// </summary>
        public static string SixteenCuiaVeryHardPack()
        {
            string json = PlayModeHelpers.SixteenCuiaPack();
            string withLabel = json.Replace("\"difficulty\": { \"par\": 1 }", "\"difficulty\": { \"label\": \"very_hard\", \"par\": 1 }");
            Assert.That(withLabel, Is.Not.EqualTo(json), "el paquete de 16 cuias cambió de forma");
            return withLabel;
        }

        /// <summary>
        /// Paquete de prueba de una fila (5 cuias: 3 tintes de 4 dosis, una cuia con 1 dosis y otra vacía) Difícil y de Aguas
        /// turbias, con la dosis del fondo de la cuia 0 oculta: la cuia turbia lleva 3 dosis (02 §11 T3: nunca llena) y la de encima
        /// es otra fruta (T4).
        /// </summary>
        public static string OneRowHardMurkyPack()
        {
            string[] ids = { "guanabana", "cupuacu", "tapereba" };
            int[][] board = { new[] { 1, 2, 3 }, new[] { 2, 3, 1, 2 }, new[] { 3, 1, 2, 3 }, new[] { 1 }, new int[0] };
            string json = PlayModeHelpers.BoardPack(1, "w01_estuario", "white", 1, ids, board);
            string withLabel = json.Replace("\"difficulty\": { \"par\": 1 }", "\"difficulty\": { \"label\": \"hard\", \"par\": 1 }");
            string withMurky = withLabel.Replace("\"murky\": false, \"hidden\": []", "\"murky\": true, \"hidden\": [[0, 0]]");
            Assert.That(withMurky, Is.Not.EqualTo(withLabel), "el paquete de prueba cambió de forma");
            return withMurky;
        }

        /// <summary>
        /// Caja (dp del lienzo, y hacia abajo) del texto «Nivel {n}» dibujado: de lado a lado de los caracteres visibles y de su
        /// parte alta hasta la línea base (sin descendentes: «Nivel» y las cifras) más el contorno y la sombra del estilo Badge
        /// (2,5 dp de contorno; guía 5.6) [P]. Los extremos de <c>TMP_CharacterInfo</c> llevan el relleno del SDF, así que
        /// abajo se usa la línea base.
        /// </summary>
        public static Rect GlyphRect(TMPro.TextMeshProUGUI text, RectTransform root)
        {
            const float outlineAndShadow = 4f;
            text.ForceMeshUpdate();
            TMPro.TMP_TextInfo info = text.textInfo;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            Rect rootRect = root.rect;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMPro.TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible)
                {
                    continue;
                }

                Vector3 a = root.InverseTransformPoint(text.rectTransform.TransformPoint(c.bottomLeft));
                Vector3 b = root.InverseTransformPoint(text.rectTransform.TransformPoint(c.topRight));
                Vector3 baseLine = root.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector3(c.bottomLeft.x, c.baseLine, 0f)));
                xMin = Mathf.Min(xMin, Mathf.Min(a.x, b.x) - rootRect.xMin);
                xMax = Mathf.Max(xMax, Mathf.Max(a.x, b.x) - rootRect.xMin);
                yMin = Mathf.Min(yMin, rootRect.yMax - Mathf.Max(a.y, b.y));
                yMax = Mathf.Max(yMax, rootRect.yMax - baseLine.y);
            }

            Assert.That(xMax, Is.GreaterThan(xMin), "el texto «" + text.text + "» tiene glifos visibles");
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax + outlineAndShadow);
        }

        private static bool Overlap(Rect a, Rect b)
        {
            const float slack = 0.5f;
            return a.xMin < b.xMax - slack && a.xMax > b.xMin + slack && a.yMin < b.yMax - slack && a.yMax > b.yMin + slack;
        }
    }
}
