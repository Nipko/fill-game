#nullable enable
using System.Collections;
using System.IO;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
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
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Hoja atada en el cliente (tarea VA1a; 02 §9.1 «Variantes» y §1.2; propuesta de D-007 b.3: «hoja pequeña atada al cuello con
    /// fibra, con icono grande; nunca cae»; D-002; D-024) y las tarjetas de estreno de las variantes del mundo 6 (02 §10): en el
    /// banco de <see cref="MechanicTestKit"/>, la Hoja atada se distingue de la Hoja (boca abierta, cordel de fibra en el cuello y la
    /// hoja pequeña con la llave en el hombro, sin tapar la boca ni el líquido), nunca cae (recibe su fruta, se vacía como origen y
    /// sella, y el overlay sigue igual y sin pasos propios) y pintarla no asigna memoria; en la escena, con el paquete del mundo 6
    /// (<c>mundo_06_v1.json</c>, <c>pack_schema</c> 3), el 283 sale con su tarjeta de estreno y la regla, que no se cierra sola ni se
    /// salta con un toque en una cuia, y el nivel no admite el Salto del Boto (<c>skippable</c> = false, 04 §2.6); el 243 (Liana de
    /// cuenta) lleva su tarjeta con el icono de la Liana y su texto; y en el 283 la Hoja atada sale en su cuia con su llave, se
    /// selecciona como origen sin cambiar (D-002) y vierte.
    /// </summary>
    public sealed class HojaAtadaVisualTests
    {
        private const int Tapereba = 3; // índices de motor (glosario §9)
        private const int Aguaje = 4;
        private const int Bacaba = 7;
        private const int Acai = 8;

        private static ProceduralArt? s_art;
        private MechanicTestKit.Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            LaunchOptions.Reset();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        /// <summary>El paquete del mundo 6 (201-300), con los estrenos de la Liana de cuenta (243) y de la Hoja atada (283).</summary>
        internal static string World6Pack()
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Data", "Levels", "mundo_06_v1.json"));
        }

        [UnityTest]
        public IEnumerator HojaAtada_IsDistinctFromHoja()
        {
            yield return null;

            // La Hoja (vacía, llave aguaje) y la Hoja atada (con dos taperebá, su llave) en el mismo banco.
            BoardState board = Board(
                new[] { new int[0], new[] { Tapereba, Tapereba }, new[] { Tapereba, Aguaje, Acai, Bacaba }, new[] { Aguaje, Acai, Bacaba, Tapereba }, new[] { Acai, Bacaba, Aguaje }, new[] { Aguaje, Acai, Bacaba }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Hoja, key: Aguaje), new MechanicSpec(MechanicType.HojaAtada, key: Tapereba), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, board);
            HojaOverlay hoja = OverlayOf(_rig.Views[0], MechanicType.Hoja);
            HojaOverlay atada = OverlayOf(_rig.Views[1], MechanicType.HojaAtada);

            Assert.That(CuiaOverlays.Create(MechanicType.HojaAtada), Is.InstanceOf<HojaOverlay>(), "la variante de HojaOverlay (b.3)");
            Assert.That(atada.Type, Is.EqualTo(MechanicType.HojaAtada));
            Assert.That(atada.IsTied, Is.True);
            Assert.That(hoja.IsTied, Is.False);

            // La Hoja tapa la boca con la hoja de palma; la Hoja atada deja la boca abierta y lleva el cordel y la hoja pequeña.
            Assert.That(hoja.ClosedLeafVisible, Is.True, "la Hoja, cerrada sobre la boca");
            Assert.That(hoja.CordVisible || hoja.TagVisible, Is.False);
            Assert.That(atada.Visible, Is.True);
            Assert.That(atada.ClosedLeafVisible, Is.False, "la Hoja atada no tapa la boca: también es origen y destino (02 §1.2)");
            Assert.That(atada.CordVisible, Is.True, "el cordel de fibra en el cuello (b.3)");
            Assert.That(atada.TagVisible, Is.True, "la hoja pequeña que cuelga del nudo (b.3)");
            Assert.That(atada.FallingLeafVisible || atada.IsAnimating, Is.False);
            Assert.That(atada.ShownKey, Is.EqualTo(Tapereba));
            Assert.That(atada.FruitVisible, Is.True, "con la fruta llave grande (b.3)");
            Assert.That(atada.FruitSprite, Is.SameAs(_rig.Views[1].OverlayHost.Art.FruitFlight(Tapereba)));
            Assert.That(atada.FruitAngleDeg, Is.EqualTo(HojaOverlay.FruitTiltDeg).Within(0.01f), "girada sobre el medallón, como en la Hoja");
            Assert.That(atada.StepDurationMs(MechanicStepKind.Open), Is.EqualTo(0f), "nunca se suelta: sin paso Open (b.3)");
            Assert.That(atada.StepDurationMs(MechanicStepKind.Release), Is.EqualTo(0f));

            // La hoja de la Hoja atada es la misma hoja de palma, más pequeña, y cuelga girada del nudo, que va a la derecha.
            RectTransform tag = Piece(_rig.Views[1], "HojaAtadaLeaf");
            RectTransform cover = Piece(_rig.Views[0], "HojaLeaf");
            Assert.That(tag.GetComponent<UnityEngine.UI.Image>().sprite, Is.SameAs(cover.GetComponent<UnityEngine.UI.Image>().sprite));
            Assert.That(tag.localScale.x, Is.EqualTo(HojaOverlay.TiedLeafScale).Within(1e-4f), "pequeña");
            Assert.That(atada.TagAngleDeg, Is.InRange(-85f, -5f), "cuelga hacia abajo, a la derecha");
            Assert.That(atada.TiedKnotU.x, Is.GreaterThan(0f), "el nudo, a la derecha del cuello");
            Assert.That(Piece(_rig.Views[1], "HojaAtadaCord").GetComponent<UnityEngine.UI.Image>().color, Is.EqualTo(ProceduralArt.HojaCordTint), "de fibra, no de liana");

            // La llave va en el hombro derecho: deja libre el eje (el chorro y la boca del origen, D-006), queda sobre la cara del
            // líquido de la cuia llena (Q17 A) y no pasa del tope de la Hoja (la fila de arriba en los tableros de 3 filas).
            Rect key = atada.TiedKeyRectU;
            float cavityTop = CuiaGeometry.CavityTopU(4);
            Assert.That(key.xMin, Is.GreaterThan(0.3f), "la llave, a un lado del eje");
            Assert.That(key.yMin, Is.GreaterThan(cavityTop + 0.1f), "por encima del líquido de la cuia llena");
            Assert.That(atada.KeyTopAboveLipU, Is.LessThanOrEqualTo(HojaOverlay.KeyTopMaxU));
            Assert.That(atada.FruitTopAboveLipU, Is.LessThanOrEqualTo(HojaOverlay.KeyTopMaxU));
            Assert.That(atada.KeyTopAboveLipU, Is.LessThan(hoja.KeyTopAboveLipU), "más baja que la llave de la Hoja, que va sobre la boca");
            Assert.That(Signature(_rig.Views[1].OverlayHost.BodyFront), Is.Not.EqualTo(Signature(_rig.Views[0].OverlayHost.BodyFront)));

            // Con la paleta de la Hoja (regla 1 de la guía §2.4; el cordel es fibra, fuera de la regla).
            var atadaColors = new System.Collections.Generic.List<Color>();
            var hojaColors = new System.Collections.Generic.List<Color>();
            atada.CollectSurfaceColors(atadaColors);
            hoja.CollectSurfaceColors(hojaColors);
            Assert.That(atadaColors, Is.EqualTo(hojaColors));
        }

        [UnityTest]
        public IEnumerator HojaAtada_NeverFalls_ReceivesItsFruit_IsSource_AndSeals()
        {
            yield return null;

            // Vacía (02 §1.2: solo recibe su fruta): el aguaje se rechaza y el taperebá entra; después vierte (sí es origen) y queda
            // vacía, atada. Un banco con su llave taperebá, capacidad = U_c (D-024).
            BoardState start = Board(
                new[] { new int[0], new[] { Tapereba, Tapereba, Aguaje, Aguaje }, new[] { Aguaje, Aguaje, Tapereba, Tapereba }, new int[0] },
                new[] { new MechanicSpec(MechanicType.HojaAtada, key: Tapereba), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            HojaOverlay atada = OverlayOf(_rig.Views[0], MechanicType.HojaAtada);
            string rest = Signature(_rig.Views[0].OverlayHost.BodyFront);
            Assert.That(atada.Visible && atada.CordVisible && atada.TagVisible && atada.FruitVisible, Is.True, "vacía: atada, con su llave");
            Assert.That(PourRules.IsLegal(start, 1, 0), Is.False, "el aguaje no entra en la hoja atada de taperebá (02 §1.2)");

            BoardState received = _rig.Pour(start, 2, 0, out _);
            Assert.That(_rig.RunUntilIdle(6000), Is.LessThan(6000));
            Assert.That(received[0].Count, Is.EqualTo(2));
            Assert.That(received[0].Mechanic, Is.EqualTo(MechanicType.HojaAtada), "nunca se suelta (b.3)");
            AssertStillTied(atada, rest, "tras recibir su fruta");

            BoardState poured = _rig.Pour(received, 0, 3, out _);
            Assert.That(_rig.RunUntilIdle(6000), Is.LessThan(6000));
            Assert.That(poured[0].IsEmpty, Is.True, "vertió su tramo: sí es origen (02 §1.2)");
            AssertStillTied(atada, rest, "tras verter como origen");

            Assert.That(MechanicTestKit.TryFindRecord(_rig.Animator, MechanicStepKind.Open, 0, out _), Is.False, "sin paso Open");
            Assert.That(MechanicTestKit.TryFindRecord(_rig.Animator, MechanicStepKind.Release, 0, out _), Is.False, "ni Release");
            _rig.Dispose();

            // Sellada conserva la cuerda (b.3): con tres taperebá y la cuarta encima.
            BoardState three = Board(
                new[] { new[] { Tapereba, Tapereba, Tapereba }, new[] { Aguaje, Aguaje, Aguaje, Tapereba }, new[] { Aguaje }, new int[0] },
                new[] { new MechanicSpec(MechanicType.HojaAtada, key: Tapereba), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, three);
            atada = OverlayOf(_rig.Views[0], MechanicType.HojaAtada);
            rest = Signature(_rig.Views[0].OverlayHost.BodyFront);
            BoardState sealedBoard = _rig.Pour(three, 1, 0, out _);
            Assert.That(_rig.RunUntilIdle(6000), Is.LessThan(6000));
            Assert.That(sealedBoard.IsSealed(0), Is.True);
            Assert.That(sealedBoard[0].Mechanic, Is.EqualTo(MechanicType.HojaAtada), "sellada sigue atada");
            AssertStillTied(atada, rest, "sellada");
        }

        [UnityTest]
        public IEnumerator HojaAtada_Show_DoesNotAllocate()
        {
            yield return null;
            BoardState board = Board(
                new[] { new[] { Tapereba, Tapereba }, new[] { Aguaje, Aguaje, Aguaje, Tapereba }, new[] { Aguaje, Tapereba }, new int[0] },
                new[] { new MechanicSpec(MechanicType.HojaAtada, key: Tapereba), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, board);
            HojaOverlay atada = OverlayOf(_rig.Views[0], MechanicType.HojaAtada);
            Cuia cuia = board[0];
            Cuia plain = board[3];
            atada.Show(cuia, false); // calentamiento: la llave ya está puesta
            atada.Show(plain, false);
            atada.Show(cuia, false);
            Assert.That(() =>
            {
                atada.Show(cuia, false);
                atada.Show(plain, false);
                atada.Show(cuia, false);
                atada.Tick(16f);
                atada.Layout(24f, CuiaGeometry.HeightU(4));
            }, Is.Not.AllocatingGCMemory(), "Show, Tick y Layout sin asignaciones (08 §5.4)");
            Assert.That(atada.Visible && atada.TagVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator Level283_DebutCard_WithTheRule_CannotBeSkipped()
        {
            yield return LoadWorld6WithCards(283);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            LevelCardView card = view.LevelCard;

            Assert.That(flow.Level.Skippable, Is.False, "el estreno no admite el Salto del Boto (skippable = false, 04 §2.6)");
            Assert.That(SightingRule.IsBusy(283), Is.True, "ni la tarjeta del miquito");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "sale la tarjeta de estreno (02 §10)");
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Debut));
            Assert.That(card.Card.IntroMechanic, Is.EqualTo(LevelCardInfo.HojaAtadaIntro));
            Assert.That(card.Card.IntroIconKey, Is.EqualTo(LevelCardInfo.HojaAtadaIntro));
            Assert.That(card.TutorialText, Is.EqualTo(UiText.TutorialHojaAtada), "la regla: nunca cae, solo recibe su fruta y es origen [P]");
            Assert.That(UiText.Tutorial(LevelCardInfo.HojaAtadaIntro), Is.EqualTo(UiText.TutorialHojaAtada));
            Assert.That(card.TutorialText, Does.Contain("nunca cae").And.Contain("solo recibe su fruta").And.Contain("verter desde ella"));
            Assert.That(CardSetup.LineCount(card.Tutorial), Is.InRange(1, LevelCardView.MaxTutorialLines), "≤ 2 líneas a 360 dp (02 §12)");
            Assert.That(card.Tutorial.fontSize, Is.GreaterThanOrEqualTo(LevelCardView.MinTutorialSp));
            Assert.That(card.Tutorial.textInfo.characterCount, Is.EqualTo(UiText.TutorialHojaAtada.Length), "sin recortes");
            Assert.That(card.HeroSprite, Is.Not.Null, "con el icono grande de la Hoja atada");
            Assert.That(card.HeroSprite!.name, Is.EqualTo("card_hero_hoja_atada"));
            Assert.That(card.HeroSprite, Is.Not.SameAs(view.CardArt.CardHeroIcon("hoja")), "no el de la Hoja");
            Assert.That(card.MedallionSprite, Is.SameAs(view.CardArt.CardMedallion));
            Assert.That(card.TitleText, Is.Null, "un estreno normal: el tutorial hace de tarjeta (§c.1)");
            Assert.That(view.LastCardProbe, Does.Contain("\"intro\":\"hoja_atada\"").And.Contain("\"kind\":\"debut\""));
            CardSetup.AssertInsidePanel(view);

            // No se cierra sola ni deja jugar sin verla: el primer toque en una cuia solo la cierra (no selecciona ni vierte).
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(card.Visible, Is.True);
            int atada = MechanicTestKit.IndexOf(flow, MechanicType.HojaAtada);
            Assert.That(boot.TapCuia(atada).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection));
            PlayModeHelpers.Step(boot, 160);
            Assert.That(card.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator Level243_DebutCard_LianaCount()
        {
            yield return LoadWorld6WithCards(243);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            LevelCardView card = view.LevelCard;

            // La Liana de cuenta ya tenía overlay (LianaOverlay, con sus cuentas): aquí, su tarjeta.
            Assert.That(flow.Level.Skippable, Is.False);
            Assert.That(SightingRule.IsBusy(243), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Debut));
            Assert.That(card.Card.IntroMechanic, Is.EqualTo(LevelCardInfo.LianaCountIntro));
            Assert.That(card.Card.IntroIconKey, Is.EqualTo("liana"), "el icono de la Liana [P]");
            Assert.That(card.TutorialText, Is.EqualTo(UiText.TutorialLianaN));
            Assert.That(card.TutorialText, Does.Contain("cuentas"));
            Assert.That(CardSetup.LineCount(card.Tutorial), Is.InRange(1, LevelCardView.MaxTutorialLines));
            Assert.That(card.Tutorial.fontSize, Is.GreaterThanOrEqualTo(LevelCardView.MinTutorialSp));
            Assert.That(card.HeroSprite, Is.SameAs(view.CardArt.CardHeroIcon("liana")));
            Assert.That(card.HeroSprite!.name, Is.EqualTo("card_hero_liana"));
            Assert.That(view.LastCardProbe, Does.Contain("\"intro\":\"liana_n\""));
            CardSetup.AssertInsidePanel(view);

            int liana = MechanicTestKit.IndexOf(flow, MechanicType.Liana);
            Assert.That(flow.Session.Current[liana].MechanicN, Is.GreaterThan(0), "de cuenta");
            CardSetup.TapCard(card);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(boot.TapAnywhere(), Is.True);
            CardSetup.StepUntilPlaying(boot);
            var overlay = boot.View!.Views[liana].OverlayOf(MechanicType.Liana) as LianaOverlay;
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay!.CountDots, Is.EqualTo(flow.Session.Current[liana].MechanicN), "las cuentas que nombra el texto");
        }

        [UnityTest]
        public IEnumerator Level283_TiedLeaf_OnItsCuia_SelectedWithoutChange_AndPours()
        {
            LaunchOptions.LevelPackJsonOverride = World6Pack();
            yield return MechanicTestKit.LoadPlaying(283);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            int index = MechanicTestKit.IndexOf(flow, MechanicType.HojaAtada);
            Cuia cuia = flow.Session.Current[index];
            Assert.That(cuia.MechanicKey, Is.EqualTo(Aguaje), "el 283 estrena la Hoja atada con el aguaje (mundo_06_v1.json)");
            CuiaView view = boot.View!.Views[index];
            HojaOverlay atada = OverlayOf(view, MechanicType.HojaAtada);
            Assert.That(atada.Visible && atada.CordVisible && atada.TagVisible && atada.FruitVisible, Is.True);
            Assert.That(atada.ShownKey, Is.EqualTo(Aguaje));
            Assert.That(atada.FruitSprite, Is.SameAs(view.OverlayHost.Art.FruitFlight(Aguaje)));
            for (int i = 0; i < boot.View.Views.Length; i++)
            {
                if (i != index)
                {
                    CuiaOverlay? other = boot.View.Views[i].OverlayOf(MechanicType.HojaAtada);
                    Assert.That(other == null || !other.Visible, Is.True, "solo en su cuia: " + i);
                }
            }

            // D-002: como origen se eleva, pero la hoja atada no se ilumina ni cambia.
            string rest = Signature(view.OverlayHost.BodyFront);
            TapResult select = boot.TapCuia(index);
            Assert.That(select.Kind, Is.EqualTo(TapKind.Selected), "sí es origen (02 §1.2)");
            PlayModeHelpers.Step(boot, 240);
            Assert.That(view.IsLifted, Is.True);
            Assert.That(Signature(view.OverlayHost.BodyFront), Is.EqualTo(rest), "nada cambia al seleccionarla (D-002)");

            int target = -1;
            BoardState board = flow.Session.Current;
            for (int j = 0; j < board.Count && target < 0; j++)
            {
                if (j != index && PourRules.IsLegal(board, index, j))
                {
                    target = j;
                }
            }

            Assert.That(target, Is.GreaterThanOrEqualTo(0), "un destino para la cima de la hoja atada");
            TapResult pour = boot.TapCuia(target);
            Assert.That(pour.Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(flow.Session.Current[index].Mechanic, Is.EqualTo(MechanicType.HojaAtada), "nunca se suelta");
            AssertStillTied(atada, rest, "tras verter en el 283");
        }

        // Carga el nivel del mundo 6 con las tarjetas encendidas, como CardSetup.Load con el catálogo real.
        private static IEnumerator LoadWorld6WithCards(int level)
        {
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGameWithPack(World6Pack(), level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300); // entrada de 220 ms
        }

        private static void AssertStillTied(HojaOverlay atada, string rest, string when)
        {
            Assert.That(atada.Visible && atada.CordVisible && atada.TagVisible && atada.FruitVisible, Is.True, when + ": atada, con su llave (nunca cae, b.3)");
            Assert.That(atada.IsAnimating || atada.FallingLeafVisible || atada.ClosedLeafVisible, Is.False, when);
            Assert.That(Signature(atada.AttachedHost!.BodyFront), Is.EqualTo(rest), when + ": igual que en reposo");
        }

        private static HojaOverlay OverlayOf(CuiaView view, MechanicType type)
        {
            var overlay = view.OverlayOf(type) as HojaOverlay;
            Assert.That(overlay, Is.Not.Null, "overlay de " + type);
            return overlay!;
        }

        private static RectTransform Piece(CuiaView view, string name)
        {
            Transform? t = view.OverlayHost.BodyFront.Find(name);
            Assert.That(t, Is.Not.Null, name);
            return (RectTransform)t!;
        }

        private static BoardState Board(int[][] cuias, MechanicSpec[]? attrs)
        {
            var caps = new int[cuias.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = 4;
            }

            BoardBuildResult result = BoardBuilder.Build(caps, cuias, attrs);
            Assert.That(result.Error, Is.Null);
            Assert.That(LevelValidation.Validate(result.State!), Is.Null, "tablero válido (D-024)");
            return result.State!;
        }

        // Estado visible de las piezas bajo un ancla (como HojaBroteTests): activación, pose relativa al ancla, Image.enabled,
        // color y sprite de cada una.
        private static string Signature(Transform anchor)
        {
            var sb = new StringBuilder(512);
            Transform[] nodes = anchor.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                sb.Append(node.name).Append(node.gameObject.activeSelf ? '+' : '-');
                if (node is RectTransform rt)
                {
                    sb.Append(rt.anchoredPosition.ToString("F3")).Append(rt.sizeDelta.ToString("F3"));
                }

                sb.Append(node.localScale.ToString("F3")).Append(node.localEulerAngles.ToString("F3"));
                var image = node.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    sb.Append(image.enabled ? 'E' : 'D').Append(image.color.ToString("F3")).Append(image.sprite != null ? image.sprite.name : "-");
                }

                sb.Append('|');
            }

            return sb.ToString();
        }
    }
}
