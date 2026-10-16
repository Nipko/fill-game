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
    /// hoja pequeña con la llave, una muestra de la dosis, en el medallón que tapa el nudo, sin tapar la boca ni el líquido), nunca
    /// cae (recibe su fruta, se vacía como origen, apartando la llave del labio al verter, y sella, y el overlay sigue igual y sin
    /// pasos propios) y pintarla no asigna memoria; en la escena, con el paquete del mundo 6 (<c>mundo_06_v1.json</c>,
    /// <c>pack_schema</c> 3), el 283 sale con su tarjeta de estreno y la regla, que no se cierra sola ni se salta con un toque en
    /// una cuia, y el nivel no admite el Salto del Boto (<c>skippable</c> = false, 04 §2.6); el 243 (Liana de cuenta) lleva su
    /// tarjeta con su propio icono (la liana con sus cuentas) y su texto; en el 283 la Hoja atada sale en su cuia con su llave,
    /// reflejada en la última columna, se selecciona como origen sin cambiar (D-002) y vierte; y con 14 cuias (5/5/4) en la
    /// última columna queda a ≥ 6 dp del marco (revisión de VA1a).
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
            Assert.That(atada.FruitVisible, Is.True, "con la llave grande (b.3)");
            Assert.That(atada.FruitSprite!.name, Is.EqualTo(SwatchName(Tapereba)), "la llave, una muestra de la dosis: su tinte, su patrón y su icono (revisión de VA1a)");
            Assert.That(atada.FruitSprite, Is.Not.SameAs(_rig.Views[1].OverlayHost.Art.FruitFlight(Tapereba)), "no la fruta en vuelo, de otro color que su líquido");
            Assert.That(atada.FruitAngleDeg, Is.EqualTo(0f).Within(0.01f), "la muestra, derecha: su icono se lee como en las capas");
            Assert.That(hoja.FruitSprite, Is.SameAs(_rig.Views[0].OverlayHost.Art.FruitFlight(Aguaje)), "la Hoja sigue con la fruta");
            Assert.That(atada.StepDurationMs(MechanicStepKind.Open), Is.EqualTo(0f), "nunca se suelta: sin paso Open (b.3)");
            Assert.That(atada.StepDurationMs(MechanicStepKind.Release), Is.EqualTo(0f));

            // La hoja de la Hoja atada es la misma hoja de palma, más pequeña, y cuelga girada del nudo, que va a la derecha.
            RectTransform tag = Piece(_rig.Views[1], "HojaAtadaLeaf");
            RectTransform cover = Piece(_rig.Views[0], "HojaLeaf");
            Assert.That(tag.GetComponent<UnityEngine.UI.Image>().sprite, Is.SameAs(cover.GetComponent<UnityEngine.UI.Image>().sprite));
            Assert.That(tag.localScale.x, Is.EqualTo(HojaOverlay.TiedLeafScale).Within(1e-4f), "pequeña");
            Assert.That(atada.TagAngleDeg, Is.InRange(-85f, -5f), "cuelga hacia abajo, a la derecha");
            Assert.That(atada.TiedSide, Is.EqualTo(1f), "en el hombro derecho: no es la última cuia de su fila");
            Assert.That(atada.TiedKnotU.x, Is.GreaterThan(0f), "el nudo, a la derecha del cuello");
            Assert.That(atada.TiedKeyRectU.Contains(atada.TiedKnotU), Is.True, "el medallón cuelga del nudo y lo tapa: ningún ajuste de TiedKeyXU deja el nudo suelto a la vista");
            Assert.That(Piece(_rig.Views[1], "HojaAtadaCord").GetComponent<UnityEngine.UI.Image>().color, Is.EqualTo(ProceduralArt.HojaCordTint), "de fibra, no de liana");

            // El cordel ciñe el cuello 0,02 u bajo el borde de fibra, del canto izquierdo del cuello al nudo, sin colgar sobre la
            // boca (revisión de VA1a, captura 54e): todo su tramo delantero queda entre esa línea y el nudo.
            Assert.That(Piece(_rig.Views[1], "HojaAtadaCord").GetComponent<UnityEngine.UI.Image>().sprite.name, Is.EqualTo("mech_hoja_atada_cord"));
            Assert.That(HojaOverlay.TiedCordFrontAboveCavityU, Is.EqualTo(CuiaGeometry.ShoulderU + CuiaGeometry.NeckU - 0.02f).Within(1e-4f));
            Vector2 cordStart = ProceduralArt.HojaAtadaCordPointU(0f);
            Assert.That(cordStart.x, Is.EqualTo(-CuiaGeometry.NeckOuterHalfWidthU).Within(1e-4f), "desde el canto izquierdo del cuello");
            for (int i = 0; i <= 20; i++)
            {
                Vector2 p = ProceduralArt.HojaAtadaCordPointU(i / 20f);
                float y = HojaOverlay.TiedCordAboveCavityU + p.y;
                Assert.That(y, Is.GreaterThanOrEqualTo(HojaOverlay.TiedCordFrontAboveCavityU - 1e-4f), "no cuelga sobre la boca (t = " + (i / 20f) + ")");
                Assert.That(y, Is.LessThanOrEqualTo(HojaOverlay.TiedCordAboveCavityU + ProceduralArt.LianaCollarKnotYU + 1e-4f));
            }

            // La llave va en el hombro derecho: deja libre el eje (el chorro y la boca del origen, D-006), queda sobre la cara del
            // líquido de la cuia llena (Q17 A) y no pasa del tope de la Hoja (la fila de arriba en los tableros de 3 filas).
            Rect key = atada.TiedKeyRectU;
            float cavityTop = CuiaGeometry.CavityTopU(4);
            Assert.That(key.xMin, Is.GreaterThan(0.25f), "la llave, a un lado del eje");

            // El medallón, de unos 24 dp (como el de la Hoja) y nunca por debajo de 22 dp con cuias de 48 dp; la muestra, de unos
            // 16 dp con las cuias de 47 dp de los tableros de 3 filas (revisión de VA1a).
            const float dpPerU48 = 48f / CuiaGeometry.WidthU;
            const float dpPerU47 = 47f / CuiaGeometry.WidthU;
            Assert.That(ProceduralArt.HojaBadgeWidthU * HojaOverlay.TiedBadgeScale * dpPerU48, Is.GreaterThanOrEqualTo(22f));
            Assert.That(key.width, Is.EqualTo(ProceduralArt.HojaBadgeWidthU * HojaOverlay.TiedBadgeScale).Within(1e-4f));
            Assert.That(ProceduralArt.HojaAtadaSwatchU * dpPerU47, Is.InRange(15f, 17f));
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

            // Al verter hacia la derecha (giro horario: la boca baja por su hombro derecho), el medallón se funde y reaparece en el
            // hombro de arriba, el izquierdo, y la llave se aparta del labio: el arranque del chorro queda limpio (revisión de
            // VA1a, captura 54c). Al volver, todo exacto a su sitio.
            Assert.That(atada.TiedSide, Is.EqualTo(1f));
            BoardState poured = _rig.Pour(received, 0, 3, out _);
            float maxAway = 0f;
            float minAlpha = 1f;
            float awaySide = 0f;
            float awayKeyX = 0f;
            int ms = 0;
            for (; ms < 6000 && !_rig.Animator.IsIdle; ms += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                minAlpha = Mathf.Min(minAlpha, atada.TiedShownAlpha);
                if (atada.TiedPourAway > maxAway)
                {
                    maxAway = atada.TiedPourAway;
                    awaySide = atada.TiedShownSide;
                    awayKeyX = atada.TiedKeyU.x;
                }
            }

            Assert.That(ms, Is.LessThan(6000));
            Assert.That(poured[0].IsEmpty, Is.True, "vertió su tramo: sí es origen (02 §1.2)");
            Assert.That(maxAway, Is.EqualTo(1f).Within(1e-4f), "inclinada más de 50°, la llave apartada del todo");
            Assert.That(awaySide, Is.EqualTo(-1f), "en el hombro de arriba, no bajo el labio por el que cae el chorro");
            Assert.That(minAlpha, Is.LessThan(0.1f), "cambia de hombro fundiéndose (casi transparente a 16 ms por fotograma), sin salto");
            Assert.That(awayKeyX, Is.LessThan(-HojaOverlay.TiedKeyXU - 0.1f), "apartada del labio, hacia el hombro");
            Assert.That(atada.TiedPourAway, Is.EqualTo(0f), "de vuelta en su ranura, en su sitio");
            Assert.That(atada.TiedShownSide, Is.EqualTo(1f));
            Assert.That(atada.TiedShownAlpha, Is.EqualTo(1f));
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
            Vector2 restKey = atada.TiedKeyU;
            BoardState sealedBoard = _rig.Pour(three, 1, 0, out _);
            Assert.That(_rig.RunUntilIdle(6000), Is.LessThan(6000));
            Assert.That(sealedBoard.IsSealed(0), Is.True);
            Assert.That(sealedBoard[0].Mechanic, Is.EqualTo(MechanicType.HojaAtada), "sellada sigue atada");
            Assert.That(atada.Visible && atada.CordVisible && atada.TagVisible && atada.FruitVisible, Is.True, "sellada: atada, con su llave (b.3)");
            Assert.That(atada.IsAnimating || atada.FallingLeafVisible || atada.ClosedLeafVisible, Is.False, "sellada");

            // Sellada, el medallón baja TiedSealDropU: deja a la vista el canto de la tapa (que empieza 0,04 u bajo lo alto de la
            // cuia) y sigue tapando el nudo (revisión de VA1a, captura 54f).
            Assert.That(atada.TiedKeyU.x, Is.EqualTo(restKey.x).Within(1e-4f));
            Assert.That(atada.TiedKeyU.y, Is.EqualTo(restKey.y - HojaOverlay.TiedSealDropU).Within(1e-4f));
            Assert.That(atada.TiedKeyRectU.yMax, Is.LessThanOrEqualTo(CuiaGeometry.HeightU(4) - 0.04f), "la placa no tapa la tapa");
            Assert.That(atada.TiedKeyRectU.Contains(atada.TiedKnotU), Is.True, "sigue colgada del nudo");

            // Al deshacer el sellado (Show sin sellar), vuelve exacta a su sitio.
            atada.Show(three[0], false);
            AssertStillTied(atada, rest, "sin sellar otra vez");
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
            Assert.That(ProceduralArt.LianaCountIconKey, Is.EqualTo(LevelCardInfo.LianaCountIntro));
            Assert.That(card.TutorialText, Is.EqualTo(UiText.TutorialLianaN));
            Assert.That(card.TutorialText, Does.Contain("cuentas"));
            Assert.That(CardSetup.LineCount(card.Tutorial), Is.InRange(1, LevelCardView.MaxTutorialLines));
            Assert.That(card.Tutorial.fontSize, Is.GreaterThanOrEqualTo(LevelCardView.MinTutorialSp));
            Assert.That(card.HeroSprite, Is.SameAs(view.CardArt.CardHeroIcon(LevelCardInfo.LianaCountIntro)));
            Assert.That(card.HeroSprite!.name, Is.EqualTo("card_hero_liana_n"));
            Assert.That(card.HeroSprite, Is.Not.SameAs(view.CardArt.CardHeroIcon("liana")), "no el de la Liana con fruta (sin baya)");
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
            Assert.That(atada.FruitSprite!.name, Is.EqualTo(SwatchName(Aguaje)), "la muestra del aguaje: naranja, con escamas y su icono");
            Assert.That(index, Is.EqualTo(11), "la última cuia: última columna de la fila de abajo (4/4/4)");
            Assert.That(atada.TiedSide, Is.EqualTo(-1f), "en la última columna, reflejada al hombro izquierdo, lejos del marco");
            Assert.That(atada.TiedKeyRectU.Contains(atada.TiedKnotU), Is.True, "reflejada, el medallón sigue en el nudo");
            AssertClearOfFrame(boot, view, 6f);
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

        [UnityTest]
        public IEnumerator HojaAtada_LastColumnOfFive_MirrorsAndKeepsClearOfTheFrame()
        {
            // 14 cuias (5/5/4, D-006: los hitos de K = 13 + 1 vacío del mundo 7+) con la Hoja atada en la última columna de la
            // fila 1, la de cuias más estrechas: el medallón y la hoja que cuelga se reflejan al hombro izquierdo y quedan a ≥ 6 dp
            // del canto interior del marco (revisión de VA1a).
            LaunchOptions.LevelPackJsonOverride = FourteenCuiaPack();
            yield return MechanicTestKit.LoadPlaying(283);
            GameBootstrap boot = GameBootstrap.Current!;
            int index = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.HojaAtada);
            Assert.That(index, Is.EqualTo(FourteenAtada));
            Assert.That(boot.Flow!.Session.Current.Count, Is.EqualTo(14));
            CuiaView view = boot.View!.Views[index];
            HojaOverlay atada = OverlayOf(view, MechanicType.HojaAtada);
            Assert.That(atada.Visible && atada.CordVisible && atada.TagVisible && atada.FruitVisible, Is.True);
            Assert.That(atada.TiedSide, Is.EqualTo(-1f), "la última columna de su fila: reflejada");
            Assert.That(atada.TiedKnotU.x, Is.LessThan(0f), "el nudo, a la izquierda del cuello");
            Assert.That(atada.TiedKeyRectU.Contains(atada.TiedKnotU), Is.True);
            Assert.That(atada.TagAngleDeg, Is.InRange(5f, 85f), "la hoja cuelga hacia abajo, a la izquierda");
            AssertClearOfFrame(boot, view, 6f);
        }

        /// <summary>Índice de la Hoja atada en <see cref="FourteenCuiaPack"/>: la última columna de la fila 1 (5/5/4).</summary>
        internal const int FourteenAtada = 4;

        /// <summary>
        /// Paquete de prueba (<c>pack_schema</c> 3) en el 283 del mundo 6 con 14 cuias de 4 (5/5/4, D-006): 13 tintes del glosario
        /// §9 (índices de motor 1-13) con 4 dosis cada uno en escalera (la cuia i lleva i, i + 3, i + 6 e i + 9) y una vacía; la
        /// Hoja atada de urucum (la fruta de su dosis del fondo, b.3) en la cuia <see cref="FourteenAtada"/>, la última columna de la fila 1.
        /// </summary>
        internal static string FourteenCuiaPack()
        {
            string[] ids = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo", "araza", "cacao" };
            var cuias = new StringBuilder();
            var caps = new StringBuilder();
            var attrs = new StringBuilder();
            for (int i = 0; i < 14; i++)
            {
                string sep = i == 0 ? string.Empty : ", ";
                caps.Append(sep).Append('4');
                attrs.Append(sep).Append(i == FourteenAtada ? "{ \"type\": \"hoja_atada\", \"key\": 5 }" : "null");
                cuias.Append(sep).Append('[');
                if (i < ids.Length)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        cuias.Append(k == 0 ? string.Empty : ", ").Append(((i + (3 * k)) % ids.Length) + 1);
                    }
                }

                cuias.Append(']');
            }

            return "{ \"pack_schema\": 3, \"pack_id\": \"test_m06_v1_n0283_14\", \"kind\": \"campaign\", \"world\": 6, \"world_id\": \"w06_oriente\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"m06_v1_n0283\", \"level\": 283, \"world\": 6, \"lap\": 1, "
                + "\"water_base\": \"white\", \"palette\": [\"" + string.Join("\", \"", ids) + "\"], \"units_per_color\": 4, "
                + "\"caps\": [" + caps + "], \"cuias\": [" + cuias + "], \"attrs\": [" + attrs + "], "
                + "\"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": [] } ] }";
        }

        /// <summary>Nombre del sprite de la muestra de la llave de la Hoja atada (<c>ProceduralArt.HojaAtadaSwatch</c>).</summary>
        internal static string SwatchName(int tint)
        {
            return "mech_hoja_atada_swatch_" + ProceduralArt.TintIds[tint];
        }

        /// <summary>
        /// Margen (dp) entre lo que dibuja la Hoja atada de la cuia <paramref name="view"/> (el cordel, la placa del medallón y la
        /// hoja que cuelga, con su rectángulo girado entero, transparencias incluidas) y el canto interior del marco
        /// (<c>PanelInnerShadow</c>), en el espacio del tablero (dp).
        /// </summary>
        internal static float FrameMarginDp(GameBootstrap boot, CuiaView view)
        {
            RectTransform board = boot.View!.BoardContainer;
            Transform? inner = board.Find("PanelInnerShadow");
            Assert.That(inner, Is.Not.Null, "PanelInnerShadow");
            Rect frame = BoundsIn(board, (RectTransform)inner!);
            float margin = float.MaxValue;
            string[] pieces = { "HojaAtadaCord", "HojaAtadaBadge", "HojaAtadaLeaf", "HojaAtadaFruit" };
            for (int i = 0; i < pieces.Length; i++)
            {
                Rect r = BoundsIn(board, Piece(view, pieces[i]));
                margin = Mathf.Min(margin, Mathf.Min(r.xMin - frame.xMin, frame.xMax - r.xMax));
            }

            return margin;
        }

        private static void AssertClearOfFrame(GameBootstrap boot, CuiaView view, float minDp)
        {
            float margin = FrameMarginDp(boot, view);
            TestContext.WriteLine("AF_VA1A margen al marco: " + margin.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " dp");
            Assert.That(margin, Is.GreaterThanOrEqualTo(minDp), "el medallón y la hoja, a ≥ " + minDp + " dp del canto interior del marco");
        }

        // Caja de un RectTransform (girado o no) en el espacio local de space.
        private static Rect BoundsIn(RectTransform space, RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = space.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
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
