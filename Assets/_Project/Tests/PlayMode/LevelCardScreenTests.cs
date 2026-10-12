#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Tarjeta previa de 1 toque y tutorial de una pantalla de cada estreno en la escena (tarea T13 del paso 1; 02 §10, §11 T4,
    /// §12 y §13; propuesta-mecanicas-dificultad.md §b.0 #4, §b.2 y §c.1; 04 §2.5; 07 §7.7), con el catálogo real (1-200) y
    /// las tarjetas encendidas (<see cref="LaunchOptions.LevelCardsOverride"/> = true; el resto de tests PlayMode las apaga con
    /// <see cref="PlayModeHelpers"/>, aunque <see cref="GameConfig.LevelCardsEnabledDefault"/> ya es true): rana, «Difícil»,
    /// «Aguas bravas», «Premio: 60», «×3» y los iconos en el 40; jaguar, «Noche de jaguar», «Premio: 80» y «×4» en el 60; nada en
    /// el 41; los 11 tutoriales con su texto exacto en 2 líneas como máximo a 360 dp y ≥ 12 sp; la tarjeta combinada del 100 con
    /// la línea T4, la de la Luciérnaga, «+1 Luciérnaga» y el chip «?»; el chip en el 106; el Matapi sobre agua oscura (72 y
    /// 63); 1 toque → intro → Playing (también a
    /// mitad de la entrada, con la salida desde la opacidad y la escala del toque), con el raycast del EventSystem dando
    /// primero en el velo en las esquinas, en el centro y sobre Reiniciar; nada se recorta con la fuente al 130 % (también en
    /// 16:9); las superficies de las tarjetas de 1-200 salen de 3 altos compartidos; las llaves de 43, 83 y 163 siguen siendo
    /// las frutas que nombran los textos; y la tarjeta no sale al reanudar ni al reintentar. El estreno de la Piraña (183) lleva,
    /// en lugar del medallón, el mordisco animado (D-012, punto 1: <see cref="PiranaBiteDemo"/>): avanza, termina con la tarjeta
    /// abierta, un toque la cierra, con Reducir movimiento es una versión quieta y no asigna memoria por fotograma; su texto dice
    /// que sellar a tiempo evita el mordisco.
    /// </summary>
    public sealed class LevelCardScreenTests
    {
        /// <summary>Estrenos de D-007 en 1-200, con su <c>intro_mechanic</c> y su texto (02 §8, §9.1 y §11; §b.2).</summary>
        internal static readonly (int Level, string Mechanic, string Text)[] Debuts =
        {
            (14, "chica", UiText.TutorialChica),
            (27, "alta", UiText.TutorialAlta),
            (43, "hoja", UiText.TutorialHoja),
            (63, "matapi", UiText.TutorialMatapi),
            (83, "liana", UiText.TutorialLiana),
            (100, "murky", UiText.TutorialMurky),
            (103, "remolino", UiText.TutorialRemolino),
            (123, "barro", UiText.TutorialBarro),
            (143, "lluvia", UiText.TutorialLluvia),
            (163, "brote", UiText.TutorialBrote),
            (183, "pirana", UiText.TutorialPirana),
        };

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // 360 × 780 dp con zona segura
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Card_Level40_HardContents()
        {
            Assert.That(GameConfig.LevelCardsEnabledDefault, Is.True, "T13 enciende las tarjetas en el juego");
            yield return CardSetup.Load(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Difficulty));

            // 02 §10 y §c.1: rana dardo, «Difícil», «Aguas bravas», «Premio: 60» con la semilla y la cinta ×3.
            Assert.That(card.TitleText, Is.EqualTo("Difícil"));
            Assert.That(card.HeaderBadge!.name, Is.EqualTo("label_rana_dardo"));
            Assert.That(card.HeaderBandSprite, Is.SameAs(view.CardArt.HeaderBand(Palette.UiAmber)), "cabecera ámbar (§c.1)");
            Assert.That(card.SubtitleText, Is.EqualTo("Aguas bravas"));
            Assert.That(card.RewardText, Is.EqualTo("Premio: 60"));
            Assert.That(card.RewardIcon, Is.SameAs(view.CardArt.IconSeed));
            Assert.That(card.RibbonText, Is.EqualTo("×3"));

            // Iconos [P] de §c.1: el 40 trae una Cuia chica (2) y una alta (5), sin mecánicas ni Barro (LevelCardTests).
            Assert.That(IconNames(card), Is.EqualTo(new[] { "card_icon_cuia_chica", "card_icon_cuia_alta" }));
            Assert.That(card.MurkyShown, Is.False);
            Assert.That(card.TutorialText, Is.Null, "sin estreno no hay tutorial");
            Assert.That(card.HeroSprite, Is.Null);
            Assert.That(card.FireflyText, Is.Null);
            Assert.That(card.GiftText, Is.Null);
            CardSetup.AssertInsidePanel(view);

            // Sonda de desarrollo: tipo, nivel y líneas.
            Assert.That(view.LastCardProbe, Does.Contain("\"kind\":\"difficulty\"").And.Contain("\"level\":40").And.Contain("\"lines\":0"));
            Assert.That(view.LastCardProbe, Does.Contain("\"icons\":[\"chica\",\"alta\"]"));

            // Un toque: la intro y después Playing.
            CardSetup.TapCard(card);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            CardSetup.StepUntilPlaying(boot);
            Assert.That(card.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Card_Level60_VeryHardContents()
        {
            yield return CardSetup.Load(60);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Difficulty));
            Assert.That(card.TitleText, Is.EqualTo("Muy difícil"));
            Assert.That(card.HeaderBadge!.name, Is.EqualTo("label_jaguar_head"), "jaguar, solo el animal (07 §7.7)");
            Assert.That(card.HeaderBandSprite, Is.SameAs(view.CardArt.HeaderBand(Palette.UiIndigo)), "cabecera índigo (§c.1)");
            Assert.That(card.SubtitleText, Is.EqualTo("Noche de jaguar"));
            Assert.That(card.RewardText, Is.EqualTo("Premio: 80"));
            Assert.That(card.RibbonText, Is.EqualTo("×4"));

            // El 60 lleva una Hoja y una Cuia chica (LevelCardTests): los dos iconos, la Hoja con el icono de 07 §7.6.
            Assert.That(IconNames(card), Is.EqualTo(new[] { "mech_icon_hoja", "card_icon_cuia_chica" }));
            Assert.That(card.IconAt(0), Is.SameAs(view.CardArt.MechanicIcon(MechanicType.Hoja)));
            Assert.That(card.MurkyShown, Is.False);
            CardSetup.AssertInsidePanel(view);
        }

        [UnityTest]
        public IEnumerator Card_Level41_None()
        {
            // Normal y sin estreno: ninguna tarjeta previa (§c.1). Desde A4 (decisión del dueño del 2026-10-11), el 41 es el primer
            // nivel del mundo 3 y abre antes el banner de mundo (WorldBannerTests); al cerrarlo, directo a la intro, sin tarjeta.
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.WorldBanner, Is.EqualTo(3), "el banner del mundo 3 va antes");
            Assert.That(boot.Flow.LevelCard, Is.Null);
            Assert.That(boot.View!.LevelCard.Visible, Is.False);
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            WorldBannerSetup.Tap(boot.View.WorldBannerView);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro), "normal y sin estreno: directo a la intro (§c.1)");
            Assert.That(boot.Flow.LevelCard, Is.Null);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(boot.View.LevelCard.Visible, Is.False);
            Assert.That(boot.View.LastCardProbe, Is.Null);
            Assert.That(boot.View.CardIntroHeld, Is.False);
        }

        [UnityTest]
        public IEnumerator Debut_AllElevenTexts_TwoLinesMax()
        {
            // 02 §12 y §13 (b.0 #4): cada tutorial cabe en 2 líneas a 360 dp con la fuente al 100 %, en 16, 14 o 12 sp, con su texto
            // exacto. El tamaño es el mayor de los tres con el que cabe.
            yield return CardSetup.Load(Debuts[0].Level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.CanvasWidth, Is.EqualTo(360f).Within(0.5f));
            var sizes = new System.Text.StringBuilder();
            foreach ((int level, string mechanic, string text) in Debuts)
            {
                if (level != Debuts[0].Level)
                {
                    boot.Flow!.Load(level);
                    PlayModeHelpers.Step(boot, 300);
                }

                LevelCardView card = view.LevelCard;
                Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard), "nivel " + level);
                Assert.That(card.Visible, Is.True, "nivel " + level);
                Assert.That(card.Card!.IntroMechanic, Is.EqualTo(mechanic), "nivel " + level);
                Assert.That(card.Card.Kind, Is.EqualTo(level == 100 ? LevelCardKind.Combined : LevelCardKind.Debut), "nivel " + level);
                Assert.That(card.TutorialText, Is.EqualTo(text), "texto exacto del nivel " + level);
                Assert.That(card.TutorialText, Is.EqualTo(UiText.Tutorial(mechanic)));
                Assert.That(card.TutorialShownText!.Replace('\n', ' '), Is.EqualTo(text), "se pinta el mismo texto (como mucho, un salto tras la puntuación)");

                TextMeshProUGUI t = card.Tutorial;
                Assert.That(t.font, Is.SameAs(UiFonts.Font(TextStyle.Body)), "Nunito Regular (07 §7.1)");
                Assert.That(t.color, Is.EqualTo((Color)Palette.Ink), "en tinta (07 §7.1)");
                if (card.BodyMaterial != null)
                {
                    Assert.That(t.fontSharedMaterial, Is.SameAs(card.BodyMaterial), "algo engrosada: el material propio de la tarjeta");
                }
                int lines = CardSetup.LineCount(t);
                Assert.That(lines, Is.InRange(1, LevelCardView.MaxTutorialLines), "líneas del tutorial del nivel " + level);
                Assert.That(card.TutorialLines, Is.EqualTo(lines), "la medida de la vista coincide con TMP en el " + level);
                Assert.That(t.textInfo.characterCount, Is.EqualTo(text.Length), "ningún carácter recortado en el " + level);
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(LevelCardView.MinTutorialSp), "≥ 12 sp en el " + level);
                Assert.That(t.fontSize, Is.LessThanOrEqualTo(16f));
                Assert.That(t.fontSize, Is.EqualTo(card.TutorialBaseSp), "con la fuente al 100 %");
                Assert.That(card.TextBoxWidth, Is.LessThanOrEqualTo(LevelCardView.TutorialMaxWidthDp), "caja de ≤ 328 dp");
                Assert.That(t.rectTransform.rect.width, Is.LessThanOrEqualTo(card.TextBoxWidth + 0.01f));

                // El mayor de 16, 14 y 12 sp con el que cabe: con un tamaño más, ya no cabe.
                if (card.TutorialBaseSp < 16f)
                {
                    float bigger = card.TutorialBaseSp + 2f;
                    Assert.That(CardSetup.LinesAt(t, text, bigger, card.TextBoxWidth), Is.GreaterThan(LevelCardView.MaxTutorialLines), "a " + bigger + " sp no cabe en el " + level);
                }

                if (level == 183)
                {
                    // D-012: en lugar del medallón, el mordisco animado (Debut_Level183_AnimatedBite_*).
                    Assert.That(card.HeroSprite, Is.Null, "sin medallón en el 183");
                    Assert.That(card.BiteDemo, Is.Not.Null, "el mordisco animado en el 183");
                    Assert.That(card.TitleText, Is.Null);
                    Assert.That(card.RewardText, Is.Null);
                }
                else if (level != 100)
                {
                    Assert.That(card.BiteDemo, Is.Null, "el mordisco animado, solo en el 183");
                    Assert.That(card.HeroSprite, Is.Not.Null, "icono grande del estreno en el " + level);
                    Assert.That(card.HeroSprite!.name, Is.EqualTo("card_hero_" + mechanic));
                    Assert.That(card.TitleText, Is.Null, "en un estreno normal el tutorial hace de tarjeta (§c.1)");
                    Assert.That(card.RewardText, Is.Null);
                }

                Assert.That(view.LastCardProbe, Does.Contain("\"level\":" + level).And.Contain("\"lines\":" + lines).And.Contain("\"intro\":\"" + mechanic + "\""));
                CardSetup.AssertInsidePanel(view);
                sizes.Append(level).Append(": ").Append(card.TutorialBaseSp).Append(" sp, ").Append(lines).Append(" líneas; ");
            }

            Debug.Log("AF_T13 tutoriales: " + sizes);
        }

        [UnityTest]
        public IEnumerator Debut_Level100_Combined_HasT4LineAndFirefly()
        {
            // Pregunta 1 del paso 1 (recomendación): una sola tarjeta con todo; la línea T4 es obligatoria (02 §11 T4 y §13).
            yield return CardSetup.Load(100);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Combined));
            Assert.That(card.TitleText, Is.EqualTo("Muy difícil"));
            Assert.That(card.HeaderBadge!.name, Is.EqualTo("label_jaguar_head"));
            Assert.That(card.SubtitleText, Is.EqualTo("Noche de jaguar"));
            Assert.That(card.RewardText, Is.EqualTo("Premio: 80"));
            Assert.That(card.RibbonText, Is.EqualTo("×4"));
            Assert.That(card.MurkyShown, Is.True, "chip gris-pardo «?» (07 §7.7)");
            Assert.That(card.Murky.GlyphSprite, Is.SameAs(view.CardArt.QuestionMark));

            // El tutorial de b.2 con la línea T4 entera, sin recortar.
            Assert.That(card.TutorialText, Is.EqualTo("El agua turbia esconde frutas. Debajo nunca se repite la fruta de encima."));
            Assert.That(card.TutorialText, Does.EndWith(UiText.TutorialMurkyRule));
            Assert.That(UiText.TutorialMurkyRule, Is.EqualTo("Debajo nunca se repite la fruta de encima."));
            TextMeshProUGUI t = card.Tutorial;
            int lines = CardSetup.LineCount(t);
            Assert.That(lines, Is.EqualTo(2));
            Assert.That(t.textInfo.characterCount, Is.EqualTo(card.TutorialText!.Length), "la línea T4 se pinta entera");
            string second = t.text.Substring(t.textInfo.lineInfo[1].firstCharacterIndex).Trim();
            Assert.That(second, Is.EqualTo(UiText.TutorialMurkyRule), "la regla T4 en su propia línea, sin partir");
            Assert.That(t.overflowMode, Is.EqualTo(TextOverflowModes.Overflow), "nunca se trunca");
            Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(LevelCardView.MinTutorialSp));

            // La Luciérnaga de 04 §2.5, regalada al cargar, junto a «+1 Luciérnaga».
            Assert.That(card.FireflyText, Is.EqualTo("Una luciérnaga ilumina las de una cuia"));
            Assert.That(card.GiftText, Is.EqualTo("+1 Luciérnaga"));
            Assert.That(card.GiftIcon, Is.SameAs(view.CardArt.CardFirefly));
            Assert.That(boot.Wallet!.Stock.Get(AF.Economy.BoosterKind.Reveal), Is.EqualTo(1));
            Assert.That(view.LastCardProbe, Does.Contain("\"kind\":\"combined\"").And.Contain("\"firefly\":true").And.Contain("\"murky\":true"));
            CardSetup.AssertInsidePanel(view);
            CardSetup.AssertNoOverflow(view);
        }

        [UnityTest]
        public IEnumerator Card_Level106_MurkyChip()
        {
            // 07 §7.7: el chip «?» de Aguas turbias va en el HUD y en la tarjeta previa, con la misma regla (murky del nivel).
            yield return CardSetup.Load(106);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            Assert.That(boot.Flow!.Level.IsMurky, Is.True);
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Difficulty), "el 106 no estrena nada");
            Assert.That(card.MurkyShown, Is.True);
            Assert.That(card.Murky.GlyphSprite, Is.SameAs(view.CardArt.QuestionMark));
            Assert.That(card.TutorialText, Is.Null);
            Assert.That(card.GiftText, Is.Null, "la Luciérnaga solo se regala en el estreno (04 §2.5)");

            // El chip va en la fila de iconos, delante de ellos y sin pisarlos.
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect chip = VictorySetup.CanvasRect(card.Murky.Root, root);
            for (int i = 0; i < card.IconCount; i++)
            {
                Rect icon = VictorySetup.CanvasRect(card.IconRect(i), root);
                Assert.That(chip.Overlaps(icon), Is.False, "el chip no pisa el icono " + i);
                Assert.That(chip.xMax, Is.LessThanOrEqualTo(icon.xMin + 0.5f), "el chip va delante");
            }

            CardSetup.AssertInsidePanel(view);
        }

        [UnityTest]
        public IEnumerator Card_Matapi_OnDarkWater()
        {
            // La fibra clara del Matapi apenas contrasta con la madera clara de la tarjeta: su icono (el de 07 §7.6, sin cambiarlo)
            // va en una ranura de agua oscura y su estreno, en el medallón de agua, como se ve en el tablero sobre el panel oscuro
            // [P]. Los demás iconos, en madera clara.
            yield return CardSetup.Load(72); // Difícil, con Hoja y Matapi
            GameScreen view = GameBootstrap.Current!.View!;
            LevelCardView card = view.LevelCard;
            Assert.That(IconNames(card), Is.EqualTo(new[] { "mech_icon_hoja", "mech_icon_matapi" }));
            Assert.That(card.IconAt(1), Is.SameAs(view.CardArt.MechanicIcon(MechanicType.Matapi)));
            Assert.That(card.SlotColorAt(0), Is.Not.EqualTo(ProceduralArt.CardWater), "la Hoja, en madera clara");
            Assert.That(card.SlotColorAt(1), Is.EqualTo(ProceduralArt.CardWater), "el Matapi, en agua oscura");
            CardSetup.AssertInsidePanel(view);

            yield return CardSetup.Load(63);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.LevelCard.HeroSprite!.name, Is.EqualTo("card_hero_matapi"));
            Assert.That(view.LevelCard.MedallionSprite, Is.SameAs(view.CardArt.CardMedallionWater));

            yield return CardSetup.Load(43);
            view = GameBootstrap.Current!.View!;
            Assert.That(view.LevelCard.MedallionSprite, Is.SameAs(view.CardArt.CardMedallion), "los demás estrenos, en madera clara");
        }

        [UnityTest]
        public IEnumerator Card_OneTapStartsIntro()
        {
            yield return CardSetup.Load(43);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            LevelFlow flow = boot.Flow!;
            Assert.That(card.Visible && card.BlocksRaycasts, Is.True);

            // Ligera y de 1 toque: el velo cubre la pantalla y es lo único que recibe toques (nada de debajo se pulsa).
            List<Graphic> targets = card.RaycastTargets();
            Assert.That(targets, Is.EqualTo(new Graphic[] { card.Dim }));
            RectTransform dim = card.Dim.rectTransform;
            Assert.That(dim.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(dim.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(card.Root.GetSiblingIndex(), Is.EqualTo(card.Root.parent.childCount - 1), "por encima de todo");

            // No se cierra sola, y detrás el tablero espera en el fotograma 0 de la intro (cuias vacías y secas) [P].
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(view.CardIntroHeld, Is.True);
            Assert.That(view.Intro.Running, Is.True);
            Assert.That(view.Intro.TimeMs, Is.EqualTo(0f));
            for (int i = 0; i < view.Views.Length; i++)
            {
                Assert.That(view.Views[i].LiquidLevel, Is.EqualTo(0f), "cuia " + i + " vacía hasta la intro");
            }

            // 1 toque en cualquier punto: LevelFlow.DismissLevelCard y la intro de siempre.
            CardSetup.TapCard(card);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(flow.LevelCard, Is.Null);
            Assert.That(card.Dismissing, Is.True);
            Assert.That(card.BlocksRaycasts, Is.False, "el toque siguiente ya salta la intro");
            PlayModeHelpers.Step(boot, 160);
            Assert.That(card.Visible, Is.False, "sale en 140 ms");
            Assert.That(view.CardIntroHeld, Is.False);
            Assert.That(view.Intro.Running, Is.True, "la intro de verdad");
            Assert.That(view.Intro.TimeMs, Is.GreaterThan(0f));

            // Un toque más salta la intro (02 §4.2) y queda en Playing con el tablero del nivel.
            Assert.That(boot.TapAnywhere(), Is.True);
            CardSetup.StepUntilPlaying(boot);
            BoardState board = flow.Session.Current;
            for (int i = 0; i < view.Views.Length; i++)
            {
                Assert.That(view.Views[i].LiquidLevel, Is.EqualTo(board[i].Count).Within(0.001f), "cuia " + i);
            }

            // El fondo y las cuias también la cierran (LevelFlow), sin seleccionar nada.
            flow.Load(40);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(card.Visible, Is.True);
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection));
            PlayModeHelpers.Step(boot, 160);
            Assert.That(card.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Card_TapDuringEntry_LeavesFromCurrentLook()
        {
            // Un toque a mitad de la entrada cierra la tarjeta igual, y la salida parte de la opacidad y la escala de ese instante:
            // nada salta a opacidad 1 ni a escala 1 (la tarjeta se funde y encoge sin parpadeo hacia 0,96).
            yield return CardSetup.Load(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            card.Show(card.Card!, false, view.CanvasWidth, view.CardCanvasHeight, view.SafeTopDp, view.CardSafeBottom); // entrada desde 0
            PlayModeHelpers.TickExact(boot, 60);
            float alpha = card.Alpha;
            float scale = card.Pop.localScale.x;
            Assert.That(alpha, Is.InRange(0.2f, 0.8f), "a mitad de la entrada");

            CardSetup.TapCard(card);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro), "el toque vale también durante la entrada");
            Assert.That(card.Dismissing, Is.True);
            Assert.That(card.BlocksRaycasts, Is.False);
            const float popOutTo = 0.96f;
            for (int t = PlayModeHelpers.FrameMs; t <= 160 && card.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                if (!card.Visible)
                {
                    break;
                }

                float k = card.Pop.localScale.x;
                Assert.That(card.Alpha, Is.LessThanOrEqualTo(alpha + 1e-4f), "la opacidad solo baja desde la del toque (" + t + " ms)");
                Assert.That(Mathf.Abs(k - popOutTo), Is.LessThanOrEqualTo(Mathf.Abs(scale - popOutTo) + 1e-4f), "la escala va de la del toque a 0,96 (" + t + " ms)");
                alpha = card.Alpha;
                scale = k;
            }

            Assert.That(card.Visible, Is.False, "sale en 140 ms");
            Assert.That(card.Alpha, Is.EqualTo(1f), "oculta, lista para la siguiente");
            Assert.That(card.Pop.localScale.x, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Card_FontScale130_NoOverflow()
        {
            // 02 §12: hasta el 130 % sin desbordes ni recortes a 360 dp; los contenedores crecen. También a 1080 × 1920 px (16:9,
            // la pantalla de 360 × 640 dp de 02 §5; GameScreen la maqueta en 438,75 × 780 dp, ver HudLabelTests).
            foreach (Vector4 screen in new[] { HudLabelTests.Screen780, HudLabelTests.Screen640 })
            {
                GameScreen.SimulatedScreen = screen;
                foreach (int level in new[] { 40, 60, 43, 100, 103, 183 })
                {
                    yield return CardSetup.Load(level);
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    LevelCardView card = view.LevelCard;
                    float baseSp = card.TutorialBaseSp;
                    int baseLines = card.TutorialLines;
                    float baseHeight = card.Panel.rect.height;
                    view.CardFontScale = 1.3f;
                    PlayModeHelpers.Step(boot, 50);
                    string where = "nivel " + level + " a " + screen.x + " × " + screen.y;
                    Assert.That(card.FontScale, Is.EqualTo(1.3f), where);
                    if (baseSp > 0f)
                    {
                        Assert.That(card.Tutorial.fontSize, Is.EqualTo(baseSp * 1.3f).Within(0.01f), "el tutorial crece, no se encoge: " + where);
                        Assert.That(card.TutorialLines, Is.GreaterThanOrEqualTo(baseLines), where);
                        Assert.That(CardSetup.LineCount(card.Tutorial), Is.EqualTo(card.TutorialLines), "la medida de la vista coincide con TMP: " + where);
                    }

                    if (level == 100)
                    {
                        // El corte de frase de 100 % se conserva: la regla T4 sigue empezando línea, entera.
                        Assert.That(card.TutorialShownText, Does.Contain("frutas.\n" + UiText.TutorialMurkyRule.Substring(0, 6)), where);
                        Assert.That(card.TutorialShownText!.Replace('\n', ' '), Is.EqualTo(UiText.TutorialMurky), where);
                    }

                    Assert.That(card.Panel.rect.height, Is.GreaterThan(baseHeight), "la tarjeta crece: " + where);
                    CardSetup.AssertNoOverflow(view);
                    CardSetup.AssertInsidePanel(view);
                    CardSetup.AssertPanelOnScreen(view);
                }
            }
        }

        [UnityTest]
        public IEnumerator CardSurfaces_ThreeSharedHeights_StretchedToEachCard()
        {
            // Memoria (revisión de T13): cada superficie de madera es una textura que se guarda toda la sesión (0,5-0,9 MB) y
            // cuesta generarla. Las tarjetas de 1-200 a 360 dp tienen 5 o 6 altos (el 100 sin regalo, el 6.º), pero sus
            // superficies salen de la escala de altos compartidos (ModalKit.SharedSurfaceHeight): como mucho 3, estiradas al alto
            // de cada tarjeta como mucho √1,25 ≈ 1,12 veces (el degradado y el lustre son proporcionales al alto).
            yield return CardSetup.Load(40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            LevelCatalog catalog = boot.Flow!.Catalog;
            var heights = new SortedSet<int>();
            var surfaces = new Dictionary<Sprite, int>();
            float worst = 1f;
            int cards = 0;
            for (int level = 1; level <= 200; level++)
            {
                LevelDefinition? definition = catalog.Find(level);
                if (definition == null)
                {
                    continue;
                }

                for (int pass = 0; pass < (level == 100 ? 2 : 1); pass++)
                {
                    LevelCardInfo? info = LevelCardInfo.For(definition, level, AF.Economy.EconomyConfig.Defaults, level == 100 && pass == 0);
                    if (info == null)
                    {
                        continue;
                    }

                    card.Show(info, false, view.CanvasWidth, view.CardCanvasHeight, view.SafeTopDp, view.CardSafeBottom);
                    cards++;
                    float body = card.Panel.rect.height - 16f;
                    Sprite surface = card.Surface.sprite;
                    float surfaceDp = surface.rect.height * 100f / surface.pixelsPerUnit;
                    Assert.That(card.Surface.rectTransform.rect.height, Is.EqualTo(body).Within(0.01f), "la superficie llena el cuerpo en el " + level);
                    Assert.That(surfaceDp, Is.EqualTo(ModalKit.SharedSurfaceHeight(body)).Within(0.5f), "de la escala de altos en el " + level);
                    float stretch = Mathf.Max(body / surfaceDp, surfaceDp / body);
                    worst = Mathf.Max(worst, stretch);
                    heights.Add(Mathf.RoundToInt(card.Panel.rect.height));
                    surfaces[surface] = Mathf.RoundToInt(surfaceDp);
                }
            }

            var shared = new List<int>(surfaces.Values);
            shared.Sort();
            Debug.Log("AF_T24 tarjetas: " + cards + "; altos " + string.Join(", ", heights) + " dp; superficies " + string.Join(", ", shared)
                + " dp; estirado máximo " + worst.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
            Assert.That(cards, Is.GreaterThan(20), "tarjetas en 1-200");
            Assert.That(surfaces.Count, Is.LessThanOrEqualTo(3), "superficies distintas en 1-200");
            Assert.That(surfaces.Count, Is.LessThan(heights.Count), "varios altos comparten superficie");
            Assert.That(worst, Is.LessThanOrEqualTo(Mathf.Sqrt(ModalKit.SharedSurfaceStep) + 0.005f), "estirado máximo");
        }

        [UnityTest]
        public IEnumerator Debut_KeysMatchTexts()
        {
            // Los textos nombran la fruta llave: aguaje (4) en la Hoja del 43, taperebá (3) en la Liana del 83 y camu camu (6) en la
            // cima del Brote del 163 (el Brote no lleva key: su color es la cima, 02 §9.1 M2). Si un pack cambia la llave, el texto
            // miente: este test lo detecta. El 27 dice «agua» porque su color de cinco dosis es un agua (pregunta 2 del paso 1).
            yield return CardSetup.Load(43);
            LevelCatalog catalog = GameBootstrap.Current!.Flow!.Catalog;
            Assert.That(KeyOf(catalog.Find(43)!, MechanicType.Hoja), Is.EqualTo(4));
            Assert.That(UiText.TutorialHoja, Does.Contain("aguaje"));
            Assert.That(KeyOf(catalog.Find(83)!, MechanicType.Liana), Is.EqualTo(3));
            Assert.That(UiText.TutorialLiana, Does.Contain("taperebá"));
            LevelDefinition brote = catalog.Find(163)!;
            Assert.That(brote.IntroMechanic, Is.EqualTo("brote"));
            int[] doses = brote.CopyDoses(IndexOf(brote, MechanicType.Brote));
            Assert.That(doses[doses.Length - 1], Is.EqualTo(6), "cima del Brote del 163: camu camu");
            Assert.That(UiText.TutorialBrote, Does.Contain("camu camu"));

            LevelDefinition tall = catalog.Find(27)!;
            Assert.That(tall.IntroMechanic, Is.EqualTo("alta"));
            int five = -1;
            for (int color = 1; color <= Cuia.MaxTintColor; color++)
            {
                if (tall.Units.Of(color) == 5)
                {
                    five = color;
                }
            }

            Assert.That(five == 10 || five == 14, Is.True, "el color de cinco dosis del 27 es un agua (10 o 14), no una fruta: " + five);
            Assert.That(UiText.TutorialAlta, Does.StartWith("Esta agua tiene cinco dosis"));
        }

        [UnityTest]
        public IEnumerator Card_NotOnResumeNorRetry()
        {
            // 02 §14: al reabrir la app el nivel vuelve sin intro y sin tarjeta; al reintentar (Volver a la orilla) tampoco sale
            // (GameConfig.LevelCardOnRetry [P]).
            var store = new MemoryProfileStore(FunnelSetup.Profile(1240));
            LaunchOptions.ProfileStoreOverride = store;
            LaunchOptions.LevelCardsOverride = true;
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300);
            LevelCardView card = boot.View!.LevelCard;
            Assert.That(card.Visible, Is.True);
            CardSetup.TapCard(card);
            Assert.That(boot.TapAnywhere(), Is.True);
            CardSetup.StepUntilPlaying(boot);
            SolveResult solution = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            Pour move = solution.GetMove(0);
            boot.TapCuia(move.Source);
            Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);

            // Volver a la orilla: intro corta, sin tarjeta.
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            PlayModeHelpers.Step(boot, 50);
            Assert.That(card.Visible, Is.False);
            Assert.That(boot.Flow.LevelCard, Is.Null);
            Assert.That(boot.View.CardIntroHeld, Is.False);
            Assert.That(boot.TapAnywhere(), Is.True);
            CardSetup.StepUntilPlaying(boot);
            boot.TapCuia(move.Source);
            Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);

            // Reabrir la app con el nivel en curso: Playing, sin tarjeta.
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap again = GameBootstrap.Current!;
            Assert.That(again, Is.Not.SameAs(boot));
            PlayModeHelpers.TakeClock(again);
            PlayModeHelpers.Step(again, 300);
            Assert.That(again.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(again.Flow!.LevelNumber, Is.EqualTo(40));
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "sin intro ni tarjeta (02 §14)");
            Assert.That(again.Flow.LevelCard, Is.Null);
            Assert.That(again.View!.LevelCard.Visible, Is.False);
            Assert.That(again.View.CardIntroHeld, Is.False);
        }

        [UnityTest]
        public IEnumerator Debut_Level183_AnimatedBite_EndsWithTheCardOpen_OneTapCloses()
        {
            // D-012, punto 1 (opción C + D): antes de jugar, la tarjeta enseña el mordisco: la cuia de la izquierda recibe una dosis
            // (el vertido que completa el ritmo: los 4 puntos de la cueva) y la Piraña sale de la cueva y se lleva la dosis de la
            // cima a su cueva. Avanza, termina con la tarjeta abierta (sin repetirse) y un toque la cierra.
            yield return CardSetup.Load(183);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelCardView card = view.LevelCard;
            LevelFlow flow = boot.Flow!;
            PiranaBiteDemo bite = card.BiteDemo!;
            Assert.That(bite, Is.Not.Null);
            Assert.That(card.Card!.IntroMechanic, Is.EqualTo("pirana"));
            Assert.That(bite.Visible && bite.Playing && !bite.Finished, Is.True);
            Assert.That(bite.RaycastTargetCount(), Is.EqualTo(0), "el escenario no recibe toques: el toque es del velo");
            Assert.That(card.RaycastTargets(), Is.EqualTo(new Graphic[] { card.Dim }));
            Assert.That(view.LastCardProbe, Does.Contain("\"bite_demo\":\"animated\""));
            CardSetup.AssertInsidePanel(view);
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect stage = VictorySetup.CanvasRect(card.BiteRect!, root);
            Rect text = VictorySetup.CanvasRect(card.Tutorial.rectTransform, root);
            Assert.That(stage.yMax, Is.LessThanOrEqualTo(text.yMin + 0.5f), "el escenario, encima del texto, sin pisarlo");

            // El texto (D-012): sellar a tiempo evita el mordisco, en 2 líneas como mucho y ≥ 12 sp (Debut_AllElevenTexts_TwoLinesMax).
            Assert.That(card.TutorialText, Is.EqualTo(UiText.TutorialPirana));
            Assert.That(UiText.TutorialPirana, Does.Contain("a tiempo evita el mordisco"));
            Assert.That(CardSetup.LineCount(card.Tutorial), Is.InRange(1, LevelCardView.MaxTutorialLines));
            Assert.That(card.Tutorial.fontSize, Is.GreaterThanOrEqualTo(LevelCardView.MinTutorialSp));

            // Al abrirse: la víctima con 2 dosis y la cueva con 1 y 3 de sus 4 puntos.
            CuiaView victim = bite.Victim;
            CuiaView cave = bite.Cave;
            Assert.That(victim.ShownCuia.Count, Is.EqualTo(2));
            Assert.That(cave.ShownCuia.Count, Is.EqualTo(1));
            Assert.That(cave.ShownCuia.MechanicCounter, Is.EqualTo(PiranaBiteDemo.CaveK - 1));
            Assert.That(bite.CaveOverlay, Is.Not.Null, "el overlay de la Piraña del juego");

            // La cuia recibe la dosis.
            float t = bite.ElapsedMs;
            PlayModeHelpers.TickExact(boot, (int)(PiranaBiteDemo.ReceiveStartMs + (PiranaBiteDemo.ReceiveMs * 0.5f) - t));
            Assert.That(victim.DoseFill(2), Is.InRange(0.2f, 0.99f), "a mitad de la llegada");
            PlayModeHelpers.TickExact(boot, (int)(PiranaBiteDemo.BiteStartMs - bite.ElapsedMs) - 20);
            Assert.That(victim.DoseFill(2), Is.EqualTo(1f), "la dosis entera antes del mordisco");
            Assert.That(victim.ShownCuia.Count, Is.EqualTo(3));
            Assert.That(bite.Biting, Is.False);

            // El mordisco: el paso Bite del overlay, más despacio que en el tablero; a mitad, la cima se va vaciando.
            PlayModeHelpers.TickExact(boot, 40);
            Assert.That(bite.Biting, Is.True);
            Assert.That(bite.CaveOverlay!.IsAnimating, Is.True);
            PlayModeHelpers.TickExact(boot, (int)(PiranaOverlay.BiteStepMs * PiranaBiteDemo.BiteSpeed * 0.62f));
            Assert.That(victim.DoseFill(2), Is.InRange(0.01f, 0.99f), "la víctima pierde la dosis de la cima");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));

            // Termina con la tarjeta abierta: la víctima sin la dosis, la cueva con ella y los puntos a cero.
            PlayModeHelpers.Step(boot, (int)(PiranaBiteDemo.TotalMs - bite.ElapsedMs) + 100);
            Assert.That(bite.Finished, Is.True);
            Assert.That(bite.Playing, Is.False);
            Assert.That(bite.CaveOverlay.IsAnimating, Is.False);
            Assert.That(victim.ShownCuia.Count, Is.EqualTo(2));
            Assert.That(victim.DoseFill(2), Is.EqualTo(0f));
            Assert.That(cave.ShownCuia.Count, Is.EqualTo(2));
            Assert.That(cave.ShownCuia.ColorAt(1), Is.EqualTo(PiranaBiteDemo.BittenTint), "la dosis mordida, en la cueva");
            Assert.That(cave.ShownCuia.MechanicCounter, Is.EqualTo(0));
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(bite.Finished, Is.True, "no se repite");
            Assert.That(victim.ShownCuia.Count, Is.EqualTo(2));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta sigue abierta");
            Assert.That(card.Visible && !card.Dismissing, Is.True);

            // Un toque la cierra; el escenario se va con la tarjeta.
            CardSetup.TapCard(card);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            PlayModeHelpers.Step(boot, 160);
            Assert.That(card.Visible, Is.False);
            Assert.That(bite.Visible, Is.False);

            // Un toque a mitad del mordisco también la cierra (el paso se suelta sin dejar nada a medias).
            flow.Load(183);
            PlayModeHelpers.Step(boot, (int)PiranaBiteDemo.BiteStartMs + 300);
            Assert.That(card.BiteDemo!.Biting, Is.True);
            CardSetup.TapCard(card);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(card.Visible, Is.False);
            Assert.That(bite.CaveOverlay.IsAnimating, Is.False);
            CardSetup.StepUntilPlaying(boot);
        }

        [UnityTest]
        public IEnumerator Debut_Level183_ReduceMotion_StillBite()
        {
            // 07 §7.8: con Reducir movimiento, una versión quieta, en el instante del mordisco (la cima a medio vaciar), sin que
            // nada avance.
            yield return CardSetup.Load(183);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            flow.ReduceMotion = true;
            flow.Load(183);
            PlayModeHelpers.Step(boot, 50);
            LevelCardView card = boot.View!.LevelCard;
            PiranaBiteDemo bite = card.BiteDemo!;
            Assert.That(bite.Still, Is.True);
            Assert.That(bite.Finished && !bite.Playing, Is.True);
            Assert.That(boot.View.LastCardProbe, Does.Contain("\"bite_demo\":\"still\""));
            float fill = bite.Victim.DoseFill(2);
            Assert.That(fill, Is.InRange(0.01f, 0.99f), "la cima a medio vaciar: el mordisco");
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(bite.Victim.DoseFill(2), Is.EqualTo(fill), "quieta");
            CardSetup.TapCard(card);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(card.Visible, Is.False);
            Assert.That(bite.CaveOverlay!.IsAnimating, Is.False);
        }

        [UnityTest]
        public IEnumerator Debut_Level183_NoAllocWhileBiting()
        {
            // 08 §5.4: la llegada de la dosis, el mordisco y la ronda del cardumen no asignan memoria por fotograma.
            yield return CardSetup.Load(183);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelCardView card = boot.View!.LevelCard;
            PiranaBiteDemo bite = card.BiteDemo!;
            void Run20()
            {
                for (int i = 0; i < 20; i++)
                {
                    bite.Tick(PlayModeHelpers.FrameMs);
                }
            }

            for (int round = 0; round < 9; round++)
            {
                Assert.That(Run20, Is.Not.AllocatingGCMemory(), "20 fotogramas desde " + bite.ElapsedMs + " ms");
            }

            Assert.That(bite.Finished, Is.True, "toda la demostración");
            Assert.That(Run20, Is.Not.AllocatingGCMemory(), "la ronda del cardumen al final");
        }

        private static string[] IconNames(LevelCardView card)
        {
            var names = new string[card.IconCount];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = card.IconAt(i)!.name;
            }

            return names;
        }

        private static int KeyOf(LevelDefinition level, MechanicType type)
        {
            return level.Attrs[IndexOf(level, type)].Key;
        }

        private static int IndexOf(LevelDefinition level, MechanicType type)
        {
            for (int i = 0; i < level.Attrs.Count; i++)
            {
                if (level.Attrs[i].Type == type)
                {
                    return i;
                }
            }

            Assert.Fail("el nivel " + level.Level + " no tiene " + type);
            return -1;
        }
    }

    /// <summary>Preparación y comprobaciones comunes de los tests y capturas de la tarjeta previa (tarea T13).</summary>
    internal static class CardSetup
    {
        /// <summary>
        /// Carga el nivel del catálogo real con las tarjetas encendidas, 1.240 semillas y 5 canoas, quita el reloj y deja que la
        /// tarjeta termine de entrar.
        /// </summary>
        public static IEnumerator Load(int level)
        {
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300); // entrada de 220 ms
        }

        /// <summary>
        /// Un toque en la tarjeta por el EventSystem, el mismo camino que un dedo: en las cuatro esquinas de la pantalla, en su
        /// centro, sobre Reiniciar y sobre la pausa (a la vista bajo el velo desde la limpieza L2a), el primer impacto de
        /// <see cref="EventSystem.RaycastAll"/> es el velo (Dim), así que ningún botón de debajo recibe el toque; el clic va a ese
        /// primer impacto, en el centro.
        /// </summary>
        public static void TapCard(LevelCardView card)
        {
            EventSystem events = EventSystem.current;
            Assert.That(events, Is.Not.Null, "EventSystem de la escena");
            Canvas canvas = card.Root.GetComponentInParent<Canvas>().rootCanvas;
            Camera? cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            GameScreen? view = GameBootstrap.Current != null ? GameBootstrap.Current.View : null;

            // En batchmode la pantalla real es más pequeña que la simulada de la maqueta (360 × 780 dp a 3 px/dp) y parte del HUD
            // (Reiniciar, arriba a la derecha) queda fuera de ella, donde ningún toque llega: mientras se comprueban los toques,
            // el lienzo se escala para que toda la maqueta quepa en la pantalla real. Con la cámara de las capturas ya cabe.
            float scale = canvas.scaleFactor;
            if (cam == null && view != null)
            {
                float fit = Mathf.Min(Screen.width / view.CanvasWidth, Screen.height / view.CardCanvasHeight);
                if (fit < scale)
                {
                    canvas.scaleFactor = fit;
                }
            }

            var hits = new List<RaycastResult>();
            var data = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
            try
            {
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                ((RectTransform)canvas.transform).GetWorldCorners(corners);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                var inset = new Vector2(4f, 4f); // px dentro de la pantalla
                var points = new List<(string Name, Vector2 Point)>
                {
                    ("abajo a la izquierda", min + inset),
                    ("arriba a la izquierda", new Vector2(min.x + inset.x, max.y - inset.y)),
                    ("arriba a la derecha", max - inset),
                    ("abajo a la derecha", new Vector2(max.x - inset.x, min.y + inset.y)),
                    ("en el centro", (min + max) * 0.5f),
                };
                if (view != null && view.RestartButton.gameObject.activeInHierarchy)
                {
                    var restart = (RectTransform)view.RestartButton.transform;
                    points.Add(("sobre Reiniciar", RectTransformUtility.WorldToScreenPoint(cam, restart.TransformPoint(restart.rect.center))));
                }

                if (view != null)
                {
                    // La pausa ya se ve bajo el velo (limpieza L2a: no aparece de golpe al cerrar la tarjeta), así que es lo más
                    // fácil de tocar por error: tampoco recibe el toque.
                    Assert.That(view.PauseButton.gameObject.activeInHierarchy, Is.True, "la pausa, a la vista bajo el velo");
                    var pause = (RectTransform)view.PauseButton.transform;
                    points.Add(("sobre la pausa", RectTransformUtility.WorldToScreenPoint(cam, pause.TransformPoint(pause.rect.center))));
                }

                foreach ((string name, Vector2 point) in points)
                {
                    data.position = point;
                    hits.Clear();
                    events.RaycastAll(data, hits);
                    string where = "el toque " + name + " (" + point + " px; pantalla " + Screen.width + " × " + Screen.height + ")";
                    Assert.That(hits.Count, Is.GreaterThan(0), where + " da en algo");
                    Assert.That(hits[0].gameObject, Is.SameAs(card.Dim.gameObject), where + ": el primer impacto es el velo, no " + hits[0].gameObject.name);
                }

                // El clic, en el centro y al primer impacto.
                data.position = points[4].Point;
                hits.Clear();
                events.RaycastAll(data, hits);
                data.pointerCurrentRaycast = hits[0];
                data.pointerPressRaycast = hits[0];
            }
            finally
            {
                if (canvas.scaleFactor != scale)
                {
                    canvas.scaleFactor = scale;
                    Canvas.ForceUpdateCanvases();
                }
            }

            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>Avanza hasta Playing con la vista quieta (como mucho 4 s).</summary>
        public static void StepUntilPlaying(GameBootstrap boot)
        {
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        /// <summary>Líneas de TMP del texto ya maquetado en su lienzo (<c>textInfo.lineCount</c>).</summary>
        public static int LineCount(TextMeshProUGUI t)
        {
            Canvas.ForceUpdateCanvases();
            t.ForceMeshUpdate(true, true);
            return t.textInfo.lineCount;
        }

        /// <summary>Líneas que ocuparía <paramref name="text"/> a <paramref name="size"/> sp en <paramref name="width"/> dp.</summary>
        public static int LinesAt(TextMeshProUGUI t, string text, float size, float width)
        {
            float keep = t.fontSize;
            t.fontSize = size;
            float one = t.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).y;
            int lines = Mathf.RoundToInt(t.GetPreferredValues(text, width, float.PositiveInfinity).y / one);
            t.fontSize = keep;
            return lines;
        }

        /// <summary>
        /// Ningún texto de la tarjeta se sale de su caja ni queda por debajo de 12 sp: alto (y ancho de los de una línea) preferido
        /// dentro de su rectángulo y la geometría del texto dentro de él (02 §12: «sin desbordes ni truncados»).
        /// </summary>
        public static void AssertNoOverflow(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            foreach (TextMeshProUGUI t in view.LevelCard.VisibleTexts())
            {
                t.ForceMeshUpdate(true, true);
                Rect r = t.rectTransform.rect;
                string what = "«" + t.text + "»";
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(12f), what + " ≥ 12 sp");
                Assert.That(t.overflowMode, Is.EqualTo(TextOverflowModes.Overflow), what + " nunca se trunca");
                Assert.That(t.textInfo.characterCount, Is.EqualTo(t.text.Length), what + " entero");
                Assert.That(t.preferredHeight, Is.LessThanOrEqualTo(r.height + 0.5f), what + " cabe en alto");
                if (t.textWrappingMode == TextWrappingModes.NoWrap)
                {
                    Assert.That(t.preferredWidth, Is.LessThanOrEqualTo(r.width + 0.5f), what + " cabe en ancho");
                }

                Bounds b = t.textBounds;
                Assert.That(b.min.x, Is.GreaterThanOrEqualTo(r.xMin - 0.5f), what + " por la izquierda");
                Assert.That(b.max.x, Is.LessThanOrEqualTo(r.xMax + 0.5f), what + " por la derecha");
                Assert.That(b.min.y, Is.GreaterThanOrEqualTo(r.yMin - 0.5f), what + " por abajo");
                Assert.That(b.max.y, Is.LessThanOrEqualTo(r.yMax + 0.5f), what + " por arriba");
            }
        }

        /// <summary>
        /// Todo lo de la tarjeta (textos, iconos, chip, cabecera, fila del premio, medallón y píldora) queda dentro de su cuerpo.
        /// </summary>
        public static void AssertInsidePanel(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            LevelCardView card = view.LevelCard;
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect panel = VictorySetup.CanvasRect(card.Panel, root);
            var parts = new List<RectTransform>();
            foreach (TextMeshProUGUI t in card.VisibleTexts())
            {
                parts.Add(t.rectTransform);
            }

            for (int i = 0; i < card.IconCount; i++)
            {
                parts.Add(card.IconRect(i));
            }

            if (card.MurkyShown)
            {
                parts.Add(card.Murky.Root);
            }

            // Los bloques enteros: la cabecera, la fila del premio (semilla, texto y cinta), el medallón, el escenario del mordisco
            // animado y la píldora.
            foreach (RectTransform? block in new[] { card.HeaderRect, card.RewardRowRect, card.MedallionRect, card.BiteRect, card.GiftRect })
            {
                if (block != null)
                {
                    parts.Add(block);
                }
            }

            foreach (RectTransform part in parts)
            {
                Rect r = VictorySetup.CanvasRect(part, root);
                string what = part.name + " (" + part.parent.name + ")";
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(panel.xMin + 7.5f), what + " dentro del marco por la izquierda");
                Assert.That(r.xMax, Is.LessThanOrEqualTo(panel.xMax - 7.5f), what + " dentro del marco por la derecha");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(panel.yMin + 7.5f), what + " dentro del marco por arriba");
                Assert.That(r.yMax, Is.LessThanOrEqualTo(panel.yMax - 7.5f), what + " dentro del marco por abajo");
            }
        }

        /// <summary>La tarjeta cabe entera en la pantalla, dentro de la zona segura y con margen a los lados.</summary>
        public static void AssertPanelOnScreen(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect panel = VictorySetup.CanvasRect(view.LevelCard.Panel, root);
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(LevelCardView.SideMarginDp - 0.5f));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(view.CanvasWidth - LevelCardView.SideMarginDp + 0.5f));
            // El lienzo de batchmode mide la pantalla real; la maqueta usa la simulada (alto y zona segura de GameScreen).
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp), "bajo la zona segura de arriba");
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom), "sobre la zona segura de abajo");
        }
    }
}
