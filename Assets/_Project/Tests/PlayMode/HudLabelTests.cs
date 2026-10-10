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
    /// GameScreen escala el lienzo para tener al menos 780 dp de alto (<c>ApplyScreenMetrics</c>): un teléfono 16:9 de
    /// 1080 × 1920 px no da un lienzo de 360 × 640 dp sino uno de 438,75 × 780 dp, y es el que se prueba como «360 × 640».
    /// </remarks>
    public sealed class HudLabelTests
    {
        /// <summary>SM-S938B: 360 × 780 dp con zona segura (32 dp arriba, 16 abajo).</summary>
        internal static readonly Vector4 Screen780 = new Vector4(1080f, 2340f, 96f, 48f);

        /// <summary>
        /// 16:9 (360 × 640 dp de 02 §5 a 3 px/dp) con la misma zona segura; GameScreen lo maqueta en 438,75 × 780 dp (ver la
        /// clase).
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
            GameScreen.SimulatedScreen = null;
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
            Assert.That(view.NightVeil.gameObject.activeSelf, Is.True, "el 100 es Muy difícil");
        }

        [UnityTest]
        public IEnumerator Hud_Level106_MurkyChip()
        {
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
            HudSetup.AssertMurkyBesideChip(view);
            HudSetup.AssertHudLabelsClear(view);
            HudSetup.AssertFrameAroundPanel(view);
            Assert.That(view.DifficultyFrameTucked, Is.True, "3 filas: el panel pasa bajo la fila del HUD y el marco no asoma por arriba");
            HudSetup.AssertWalletOffPanel(view);

            // El 41 (normal, sin turbias) no lleva «?» aunque se cargue después en la misma escena.
            yield return HudSetup.LoadPlaying(41);
            Assert.That(GameBootstrap.Current!.View!.HudMurkyChip.Visible, Is.False);
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
            Assert.That(view.NextChipAlpha, Is.EqualTo(1f).Within(0.001f), "aparece con Siguiente");
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
            yield return HudSetup.LoadPlaying(200);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(view.HudLabelsMode, Is.EqualTo(GameScreen.HudLabelMode.Tucked));
            Assert.That(view.HudDifficultyChip.Visible && view.HudCounter.Visible, Is.True);
            HudSetup.AssertHudLabelsClear(view);

            // NOTAS de la ola 2 («Para T09»): la píldora de Canoas «5/5» de la cabecera no pisa el canto del panel recortado bajo
            // el HUD (TopRowY + 40). La columna sube los 9 dp que hay hasta la zona segura y acaba justo en el canto (holgura 0
            // [P]); el marco índigo no asoma por arriba (pasaría a trozos bajo la insignia, las píldoras y Reiniciar).
            Assert.That(view.Layout.PanelY, Is.EqualTo(view.TopRowDp + 40f).Within(0.01f), "panel recortado en BoardTop − 8");
            Assert.That(view.WalletBar.CanoesRect.gameObject.activeSelf, Is.True, "Canoas desde el 31");
            Assert.That(view.WalletLiftDp, Is.EqualTo(9f).Within(0.01f), "sube hasta la zona segura");
            Assert.That(view.DifficultyFrameTucked, Is.True);
            HudSetup.AssertFrameAroundPanel(view);
            HudSetup.AssertWalletOffPanel(view);

            // Sigue apartada tras un cambio de fase (SyncChrome la vuelve a poner en la fila): Volver a la orilla, en la intro
            // y de nuevo en juego.
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

            // El mismo nivel en 16:9 (438,75 × 780 dp): 2 filas de 6 y 5 (la cuia de 48 dp gana a la de 46,9 en 3 filas) y el
            // contador a la derecha de la banda.
            GameScreen.SimulatedScreen = Screen640;
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.CanvasWidth, Is.EqualTo(438.75f).Within(0.01f));
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

            // Contador y etiqueta con la Lluvia en la fila 1 y una cuia alta: el 160 (Muy difícil, 10 cuias) en 16:9.
            yield return HudSetup.LoadPlaying(160);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.HudCounter.Visible && view.HudDifficultyChip.Visible, Is.True);
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
        /// NOTAS de la ola 2 («Para T09»): la columna de píldoras de la cabecera de partida (Semillas y, desde el 31, Canoas)
        /// acaba por encima del canto del panel y del canto de arriba del marco, si asoma (holgura ≥ 0 [P]; con el panel
        /// recortado bajo el HUD no hay sitio para más sin salir de la zona segura), y la píldora de Semillas no sale de la zona
        /// segura.
        /// </summary>
        public static void AssertWalletOffPanel(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            BoardLayoutResult l = view.Layout!;
            WalletBar wallet = view.WalletBar;
            Assert.That(wallet.Visible && wallet.Compact, Is.True, "la cabecera de partida");
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
            Assert.That(lowest.yMax, Is.LessThanOrEqualTo(edge + 0.05f), name + " no pisa el canto del panel ni el del marco: " + lowest + " / canto " + edge
                + " (subió " + view.WalletLiftDp + " dp)");
            Assert.That(seeds.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), "Semillas dentro de la zona segura: " + seeds + " / " + view.SafeTopDp);
            Assert.That(view.WalletLiftDp, Is.LessThanOrEqualTo(view.TopRowDp - view.SafeTopDp + 1f), "como mucho hasta la zona segura");
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
        /// pausa, Reiniciar, los saldos, el texto «Nivel {n}», las cuias elevadas de la fila 1 ni el cielo (32 dp táctiles) de
        /// la cuia del contador; y no reciben toques.
        /// </summary>
        public static void AssertHudLabelsClear(GameScreen view)
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
            int counterCuia = GameBootstrap.Current!.Flow!.Session.Counter.CuiaIndex;
            for (int i = 0; i < view.Views.Length; i++)
            {
                if (l.RowOf[i] != 0)
                {
                    continue;
                }

                float h = view.Views[i].HeightU * u;
                float top = l.FootY[i] - h - (i == counterCuia ? CuiaView.TouchHeadroomDp : CuiaView.LiftDp);
                obstacles.Add(("cuia " + i + (i == counterCuia ? " (con su cielo)" : " elevada"), Rect.MinMaxRect(l.CenterX[i] - u, top, l.CenterX[i] + u, l.FootY[i])));
            }

            for (int i = 0; i < labels.Count; i++)
            {
                Rect r = labels[i].Rect;
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.5f), labels[i].Name + " dentro de la pantalla");
                Assert.That(r.xMax, Is.LessThanOrEqualTo(view.CanvasWidth + 0.5f), labels[i].Name + " dentro de la pantalla");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.TopRowDp), labels[i].Name + " bajo la zona segura");
                for (int j = i + 1; j < labels.Count; j++)
                {
                    Assert.That(Overlap(r, labels[j].Rect), Is.False, labels[i].Name + " y " + labels[j].Name + " se tocan: " + r + " / " + labels[j].Rect);
                }

                for (int j = 0; j < obstacles.Count; j++)
                {
                    Assert.That(Overlap(r, obstacles[j].Rect), Is.False, labels[i].Name + " tapa " + obstacles[j].Name + ": " + r + " / " + obstacles[j].Rect
                        + " (forma " + view.HudLabelsMode + ")");
                }
            }

            Graphic[] graphics = view.HudDifficultyChip.Root.parent.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Assert.That(graphics[i].raycastTarget, Is.False, graphics[i].name + " no recibe toques");
            }
        }

        /// <summary>
        /// El chip del nivel siguiente va dentro del panel de victoria, 6 dp por encima de Siguiente y sin tocar la Cosecha, la
        /// Cosecha doble, Siguiente ni Inicio; Inicio sigue 68 dp bajo Siguiente.
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
            Assert.That(Overlap(chip, nextButton), Is.False, "no tapa Siguiente");
            Assert.That(Overlap(chip, home), Is.False, "no tapa Inicio");
            Assert.That(chip.yMin + next.PillBottom, Is.EqualTo(nextButton.yMin - 6f).Within(0.6f), "píldora 6 dp por encima de Siguiente");
            Assert.That(chip.center.x, Is.EqualTo(nextButton.center.x).Within(1f), "centrado sobre Siguiente");
            Assert.That(panel.Contains(chip.min) && panel.Contains(chip.max), Is.True, "dentro del panel de victoria");
            Assert.That(home.yMin - nextButton.yMin, Is.EqualTo(68f).Within(0.5f), "Inicio, 68 dp bajo el borde del CTA");
            Assert.That(GapAboveNext(view), Is.EqualTo(GameScreen.NextChipRowDp).Within(4f), "el panel crece lo justo para el chip");
            Transform harvest = view.VictoryPanelRect.Find("Harvest");
            Rect harvestRow = VictorySetup.CanvasRect((RectTransform)harvest, root);
            Assert.That(chip.yMin, Is.GreaterThanOrEqualTo(harvestRow.yMax - 0.5f), "debajo de la fila de la Cosecha");
            if (view.DoubleHarvestShown)
            {
                Rect dbl = VictorySetup.CanvasRect((RectTransform)view.DoubleHarvestButton.transform, root);
                Assert.That(chip.yMin, Is.GreaterThanOrEqualTo(dbl.yMax - 0.5f), "debajo de la Cosecha doble");
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
