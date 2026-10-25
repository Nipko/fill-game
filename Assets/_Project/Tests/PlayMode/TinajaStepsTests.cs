#nullable enable
using System.Collections;
using AF.Audio;
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
using Is =UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La Tinaja en movimiento (tarea TJ3; M13, D-019; 02 §4.1, R7, R10 f; propuesta de la Tinaja, b.2 «Animación y sonido» y
    /// c.6; D-010; Q-Ti6): el gesto de <b>sacar</b> (se mueve su cuia, no la tinaja; sin chorro), el revelado en la boca (etapa a,
    /// en su overlay y nunca como agua turbia), la suelta en la etapa (f) (su cuia baja del jirau y la tinaja se tumba boca abajo,
    /// después de que su cuia vuelva), en el orden de R10; Marea y Volver a la orilla, que vuelven a colgar su cuia y conservan lo
    /// revelado; y la intro de D-010 (la fruta solo en la boca y el chip en el primer impacto). Tablero del 323 del paquete de
    /// muestra (<see cref="TinajaVisualTests.Board323"/>: su cuia en la 1 y la tinaja en la 2 con [jenipapo*, lulo*, jenipapo]).
    /// </summary>
    public sealed class TinajaStepsTests
    {
        private const int Cucharon = TinajaVisualTests.CucharonIndex;
        private const int Tinaja = TinajaVisualTests.TinajaIndex;
        private const int Empty7 = 7; // una vacía de 4 del 323
        private const int Jenipapo = 9;
        private const int Lulo = 11;

        // Con los tiempos del banco (08 §9 con factor 1): viaje 300, llenado 280 (1 dosis) y vuelta 300.
        private const float FlowEndMs = 300f + 280f;
        private const float RevealStartMs = FlowEndMs + BoardAnimator.StreamGrowMs;
        private const float CuiaBackMs = FlowEndMs + 300f;

        private static ProceduralArt? s_art;
        private MechanicTestKit.Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [SetUp]
        public void SetUp()
        {
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            BoardLayout.TroopBandOffForTests = false;
            _rig?.Dispose();
            _rig = null;
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [Test]
        public void Plan_RevealInStageA_TheReleaseInStageF_InTheOrderOfR10()
        {
            BoardState s0 = TinajaVisualTests.Board323();
            var buffer = new StepEntry[MechanicStepPlan.MaxSteps];

            // Sacar revela la nueva boca (R7): un Reveal en la tinaja, en (a), con la capa 1; sin suelta.
            PourVisual v1 = Visual(s0, Tinaja, Cucharon);
            Assert.That(v1.IsScoop, Is.True);
            Assert.That(v1.Mover, Is.EqualTo(Cucharon), "al sacar vuela su cuia");
            Assert.That(MechanicStepPlan.Build(in v1, buffer), Is.EqualTo(1));
            AssertEntry(buffer[0], MechanicStepKind.Reveal, StepStage.A, Tinaja, -1);
            Assert.That(buffer[0].LayerMask, Is.EqualTo(0b10));
            Assert.That(buffer[0].Tint, Is.EqualTo(Lulo));

            // La última sacada: la tinaja queda vacía y su cuia se suelta en (f), con dos pasos a la vez.
            BoardState s3 = Apply(Apply(Apply(s0, Tinaja, Cucharon), Tinaja, Cucharon), Cucharon, Empty7);
            PourVisual v4 = Visual(s3, Tinaja, Cucharon);
            Assert.That(v4.After[Tinaja].IsEmpty, Is.True);
            Assert.That(v4.Events.CucharonReleasedMask, Is.EqualTo(1 << Cucharon));
            Assert.That(v4.After[Cucharon].Mechanic, Is.EqualTo(MechanicType.None), "R10 f: una Cuia chica normal");
            int n = MechanicStepPlan.Build(in v4, buffer);
            Assert.That(n, Is.EqualTo(2), "sin revelado: ya no quedan ocultas");
            AssertEntry(buffer[0], MechanicStepKind.Unhook, StepStage.F, Cucharon, Tinaja);
            AssertEntry(buffer[1], MechanicStepKind.Upend, StepStage.F, Tinaja, Cucharon);
            Assert.That(MechanicStepPlan.ReleaseStepMask(in v4), Is.EqualTo(0), "la suelta de su cuia no es de (d) (VA1b)");
            Assert.That((int)StepStage.F, Is.GreaterThan((int)StepStage.E).And.LessThan((int)StepStage.CPrime), "R10: (f) tras los contadores y antes de f′");

            // Hasta su paso, su cuia sigue colgada (con su contenido nuevo) y la tinaja, vacía.
            Cuia pre = StepStaging.Pre(in v4, Cucharon, StepStage.F);
            Assert.That(pre.Mechanic, Is.EqualTo(MechanicType.Cucharon));
            Assert.That(pre.Count, Is.EqualTo(v4.After[Cucharon].Count));
            Assert.That(StepStaging.Pre(in v4, Tinaja, StepStage.F), Is.EqualTo(v4.After[Tinaja]));
            Assert.That(StepStaging.JobEnd(in v4, buffer, n, Cucharon, out Cuia jobEnd, out _), Is.True);
            Assert.That(jobEnd.Mechanic, Is.EqualTo(MechanicType.Cucharon), "el gesto la devuelve aún colgada");

            // Un vertido desde su cuia usa el gesto normal.
            PourVisual fromCuia = Visual(Apply(s0, Tinaja, Cucharon), Cucharon, Empty7);
            Assert.That(fromCuia.IsScoop, Is.False);
            Assert.That(fromCuia.Mover, Is.EqualTo(Cucharon));
        }

        [UnityTest]
        public IEnumerator Scoop_MovesItsCuia_NotTheTinaja_WithoutAStream()
        {
            yield return null;
            BoardState board = TinajaVisualTests.Board323();
            _rig = new MechanicTestKit.Rig(Art, board);
            CuiaView tv = _rig.Views[Tinaja];
            CuiaView cv = _rig.Views[Cucharon];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;
            BoardState s1 = _rig.Pour(board, Tinaja, Cucharon, out PourVisual _);
            float maxCuiaAngle = 0f;
            float maxCuiaRise = 0f;
            bool layered = false;
            int maxDrips = 0;
            float highestDrip = float.MinValue;
            float liftMs = BoardAnimator.ScoopLiftFraction * 300f;
            float lastDripEnd = FlowEndMs + liftMs + ((BoardAnimator.ScoopDripCount - 1) * BoardAnimator.ScoopDripEveryMs) + TinajaOverlay.DripFallMs + TinajaOverlay.DripFadeMs;
            float half = 0.5f * ProceduralArt.TinajaMouthWidthU;
            float mouthTop = ProceduralArt.TinajaMouthCenterU + (0.5f * ProceduralArt.TinajaMouthHeightU);
            for (int t = 0; t < (int)CuiaBackMs + 200; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                Assert.That(tv.BodyAngle, Is.EqualTo(0f).Within(0.01f), "la tinaja no se inclina (" + t + " ms)");
                Assert.That(tv.BodyOffset.magnitude, Is.LessThan(0.01f), "ni se mueve (" + t + " ms)");
                Assert.That(_rig.Animator.TryGetPourPose(Tinaja, out _, out _), Is.False, "sin pose de vertido en la tinaja");
                Assert.That(_rig.Animator.ActiveStreams, Is.EqualTo(0), "sin chorro: su cuia recoge el agua en la boca (" + t + " ms)");

                // Las gotas de la vuelta (02 §4.1; b.2; revisión de TJ3): tras salir su cuia de la boca, del punto más bajo de su cuia
                // al centro de la cavidad, y se apagan allí; las borra el revelado al florecer la fruta nueva.
                int drips = tin.ActiveDrips;
                maxDrips = Mathf.Max(maxDrips, drips);
                if (drips > 0)
                {
                    Assert.That(t + PlayModeHelpers.FrameMs, Is.GreaterThan(FlowEndMs + liftMs), "no caen antes de que su cuia salga de la boca (" + t + " ms)");
                    Assert.That(t, Is.LessThan(lastDripEnd + PlayModeHelpers.FrameMs), "ni después (" + t + " ms)");
                }

                for (int k = 0; k < TinajaOverlay.MaxDrips; k++)
                {
                    Rect d = tin.DripRectU(k);
                    if (tin.DripActive(k))
                    {
                        highestDrip = Mathf.Max(highestDrip, d.center.y);
                        Assert.That(d.center.y, Is.LessThanOrEqualTo(mouthTop + 1f), "cae de su cuia, junto a la boca (" + t + " ms)");
                        Assert.That(Mathf.Abs(d.center.x), Is.LessThanOrEqualTo(half), "la gota cae en la boca (" + t + " ms)");
                        Assert.That(d.center.y, Is.GreaterThanOrEqualTo(ProceduralArt.TinajaMouthCenterU - TinajaOverlay.DripLandBelowU - 0.01f), "y para en el centro de la cavidad (" + t + " ms)");
                        Assert.That(d.width * tv.Unit, Is.EqualTo(TinajaOverlay.DripDiameterDp).Within(0.01f), "6 dp");
                        Color c = tin.DripColor(k);
                        Assert.That(ColorMath.LabLightness(new Color(c.r, c.g, c.b, 1f)), Is.GreaterThan(ColorMath.LabLightness(Palette.Tint(Jenipapo))), "el tinte, aclarado");
                    }
                }

                if (t >= (int)CuiaBackMs - 40)
                {
                    continue;
                }

                maxCuiaAngle = Mathf.Max(maxCuiaAngle, Mathf.Abs(cv.BodyAngle));
                maxCuiaRise = Mathf.Max(maxCuiaRise, cv.BodyOffset.y);
                if (cv.Body.parent != cv.Root)
                {
                    Assert.That(tv.Body.parent, Is.SameAs(cv.Body.parent), "las dos en la capa de vertido (" + t + " ms)");
                    Assert.That(tv.Body.GetSiblingIndex(), Is.GreaterThan(cv.Body.GetSiblingIndex()), "la tinaja encima: su cuia entra por detrás del labio");
                    layered = true;
                }

                if (t >= 360 && t <= 520)
                {
                    // En la boca: inclinada 20°, con el pie en el eje de la tinaja, bajo el centro de su boca.
                    Assert.That(Mathf.Abs(cv.BodyAngle), Is.EqualTo(BoardAnimator.ScoopTiltDeg).Within(0.01f), "inclinada 20° (" + t + " ms)");
                    Vector2 foot = cv.FootPosition + cv.BodyOffset;
                    Vector2 dip = BoardAnimator.ScoopFootFor(tv);
                    Assert.That(foot.x, Is.EqualTo(dip.x).Within(0.01f), "en el eje de la tinaja");
                    Assert.That(foot.y, Is.EqualTo(dip.y).Within((BoardAnimator.ScoopBobU * tv.Unit) + 0.01f), "en la boca, con su vaivén");
                }
            }

            Assert.That(layered, Is.True);
            Assert.That(tin.DripsStarted, Is.EqualTo(BoardAnimator.ScoopDripCount), "vuelve con sus gotas, una vez");
            Assert.That(maxDrips, Is.GreaterThanOrEqualTo(2), "2-3 gotas cayendo a la vez");
            Assert.That(highestDrip, Is.GreaterThan(ProceduralArt.TinajaTopU + 0.1f), "se ven caer: nacen por encima del labio");
            Assert.That(tin.ActiveDrips, Is.EqualTo(0), "y no quedan pegadas al borde");
            Assert.That(maxCuiaAngle, Is.EqualTo(BoardAnimator.ScoopTiltDeg).Within(0.01f));
            Assert.That(maxCuiaRise, Is.GreaterThan((ProceduralArt.TinajaMouthCenterU - BoardAnimator.ScoopFootBelowMouthU) * tv.Unit), "el arco pasa por encima de la vasija");
            _rig.RunUntilIdle(4000);
            Assert.That(cv.Body.parent, Is.SameAs(cv.Root), "su cuia, de vuelta en su ranura");
            Assert.That(tv.Body.parent, Is.SameAs(tv.Root), "y la tinaja también");
            Assert.That(cv.BodyOffset.magnitude, Is.LessThan(0.01f));
            Assert.That(cv.BodyAngle, Is.EqualTo(0f).Within(0.01f));
            Assert.That(cv.ShownCuia.Count, Is.EqualTo(1));
            Assert.That(cv.ShownCuia.TopColor, Is.EqualTo(Jenipapo), "su cuia con lo que sacó");

            // Desde su cuia, el gesto normal: su cuia se inclina sobre el destino (D-006).
            _rig.Pour(s1, Cucharon, Empty7, out PourVisual _);
            _rig.Run(400);
            Assert.That(_rig.Animator.TryGetPourPose(Cucharon, out _, out float angle), Is.True, "vierte con su pose");
            Assert.That(Mathf.Abs(angle), Is.GreaterThan(30f));
            _rig.RunUntilIdle(4000);
        }

        [UnityTest]
        public IEnumerator Reveal_InTheMouth_WhenItsCuiaLeaves_NeverAsMurkyWater()
        {
            yield return null;
            BoardState board = TinajaVisualTests.Board323();
            _rig = new MechanicTestKit.Rig(Art, board);
            CuiaView tv = _rig.Views[Tinaja];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;
            _rig.Animator.Steps.ClearLog();
            float t0 = _rig.Animator.Clock;
            _rig.Pour(board, Tinaja, Cucharon, out PourVisual _);
            bool ring = false;
            bool bounce = false;
            bool dark = false;
            int t = 0;
            for (; t < 4000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                Assert.That(tv.MarksAnimating, Is.False, "las marcas de turbias no corren (" + t + " ms)");
                Assert.That(tv.Murky.BadgeVisible, Is.False);
                if (_rig.Animator.Steps.TryGetRunning(MechanicStepKind.Reveal, Tinaja, out float elapsed, out _))
                {
                    if (elapsed < TinajaOverlay.RevealDarkMs - PlayModeHelpers.FrameMs)
                    {
                        Assert.That(Opaque(tin.MouthColor), Is.EqualTo(Opaque(Palette.WoodDark)), "la boca, oscura (" + elapsed + " ms)");
                        Assert.That(tin.ChipDigitSprite, Is.SameAs(Art.MurkyBadgeDigit(3)), "el chip aún «×3»");
                        dark = true;
                    }

                    if (elapsed > TinajaOverlay.RevealDarkMs + PlayModeHelpers.FrameMs)
                    {
                        Assert.That(tin.ChipDigitSprite, Is.SameAs(Art.MurkyBadgeDigit(2)), "el chip baja a «×2»");
                    }

                    ring |= tin.RingVisible;
                    bounce |= tin.ChipScale > 1.05f;
                }
            }

            Assert.That(dark && ring && bounce, Is.True, "oscura, anillo de onda y rebote del chip (b.2)");
            MechanicStepRecord r = Find(MechanicStepKind.Reveal);
            Assert.That(r.Stage, Is.EqualTo(StepStage.A));
            Assert.That(r.Owner, Is.EqualTo(MechanicType.Tinaja), "lo ejecuta su overlay");
            Assert.That(r.UsedDefault, Is.False);
            Assert.That(r.EndMs - r.StartMs, Is.EqualTo(TinajaOverlay.RevealStepMs).Within(1f), "≤ 400 ms");
            Assert.That(r.StartMs - t0, Is.EqualTo(RevealStartMs).Within(PlayModeHelpers.FrameMs + 1f), "en cuanto su cuia sale de la boca");
            Assert.That(r.StartMs - t0, Is.LessThan(CuiaBackMs), "sin esperar a que vuelva");
            Assert.That(tin.MouthTint, Is.EqualTo(Lulo));
            Assert.That(tin.ChipCount, Is.EqualTo(2));
            Assert.That(tin.ChipScale, Is.EqualTo(1f));
            Assert.That(tin.RingVisible, Is.False);
            Assert.That(tin.IsAnimating, Is.False);
            for (int k = 0; k < tv.Capacity; k++)
            {
                Assert.That(tv.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None));
            }
        }

        [UnityTest]
        public IEnumerator LastScoop_ItsCuiaUnhooks_AndTheTinajaUpends_AfterItsCuiaIsBack_R10f()
        {
            yield return null;
            BoardState s0 = TinajaVisualTests.Board323();
            _rig = new MechanicTestKit.Rig(Art, s0);
            BoardState s1 = _rig.Pour(s0, Tinaja, Cucharon, out _);
            _rig.RunUntilIdle(4000);
            BoardState s2 = _rig.Pour(s1, Tinaja, Cucharon, out _);
            _rig.RunUntilIdle(4000);
            BoardState s3 = _rig.Pour(s2, Cucharon, Empty7, out _);
            _rig.RunUntilIdle(4000);
            CuiaView tv = _rig.Views[Tinaja];
            CuiaView cv = _rig.Views[Cucharon];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;
            var cuch = (CucharonOverlay)cv.OverlayOf(MechanicType.Cucharon)!;
            Assert.That(tin.ChipCount, Is.EqualTo(1));
            Assert.That(cuch.Visible && cuch.AsaVisible && cuch.PostVisible, Is.True);

            _rig.Animator.Steps.ClearLog();
            float t0 = _rig.Animator.Clock;
            BoardState s4 = _rig.Pour(s3, Tinaja, Cucharon, out _);
            Assert.That(s4[Tinaja].IsEmpty && s4[Cucharon].Mechanic == MechanicType.None, Is.True);
            bool waited = false;
            bool drying = false;
            bool tumbling = false;
            bool baseShown = false;
            bool shaded = false;
            bool fading = false;
            bool rose = false;
            bool settled = false;
            float hb = ProceduralArt.TinajaHalfWidthU(0f);
            for (int t = 0; t < 4000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                float now = _rig.Animator.Clock - t0;
                Assert.That(tin.RingVisible, Is.False, "sin anillo de onda: no hay dosis nueva (" + now + " ms)");
                if (now > RevealStartMs + 20f && now < CuiaBackMs - 20f)
                {
                    // La boca enseña el fondo seco mientras su cuia vuelve: la tinaja vacía aún boca arriba y su cuia, colgada.
                    Assert.That(tin.HoldingUpright, Is.True, "espera boca arriba (" + now + " ms)");
                    Assert.That(tin.UpsideDown, Is.False);
                    Assert.That(tin.MouthVisible && tin.DryMouthVisible, Is.True, "con la boca seca");
                    Assert.That(tin.MouthGlossVisible, Is.False, "mate: sin el brillo del agua");
                    if (now > RevealStartMs + TinajaOverlay.DryFadeMs + PlayModeHelpers.FrameMs)
                    {
                        Assert.That(Opaque(tin.MouthColor), Is.EqualTo(TinajaOverlay.DryMouthRim), "el fondo seco, #6B4A33 (" + now + " ms)");
                    }
                    else
                    {
                        drying |= Opaque(tin.MouthColor) != TinajaOverlay.DryMouthRim; // pasa al fondo seco en 150 ms
                    }

                    Assert.That(Opaque(tin.MouthColor), Is.Not.EqualTo(Opaque(Palette.WoodDark)), "no es la boca oscura del revelado");
                    Assert.That(tin.MouthGlyphVisible || tin.ChipVisible, Is.False, "sin fruta ni chip");
                    Assert.That(cuch.Visible, Is.True, "su cuia sigue colgada hasta su paso");
                    waited = true;
                }

                if (tin.BodyScaleY > -0.9f && tin.BodyScaleY < 0.9f)
                {
                    tumbling = true;

                    // Un sólido que da la vuelta apoyado en el jirau: el pivote, en su canto de abajo (sin levantarse).
                    float a = tin.BodyAngleDeg * Mathf.Deg2Rad;
                    float h = ProceduralArt.TinajaBodyHeightU * Mathf.Abs(tin.BodyScaleY);
                    float expected = ((hb * Mathf.Abs(Mathf.Sin(a))) + (0.5f * h * Mathf.Cos(a))) * tv.Unit;
                    Assert.That(tin.BodyCenterDp.y, Is.EqualTo(expected).Within(0.05f), "apoyada en el jirau (" + now + " ms)");
                    baseShown |= tin.BaseCapVisible && tin.BodyScaleY < -0.2f;
                    shaded |= tin.BodyColor.r < 0.9f;
                }

                // El hueco de la boca solo se ve mientras la boca mira arriba: desde la mitad del giro, el cuerpo cerrado.
                if (tin.BodyScaleY < 0f)
                {
                    Assert.That(tin.BodyShowsCavity, Is.False, "sin cavidad desde la mitad del vuelco (" + now + " ms)");
                    Assert.That(tin.UpendCapVisible, Is.False, "ni la cara de la boca (" + now + " ms)");
                }
                else if (tin.BodyScaleY > 0.5f)
                {
                    Assert.That(tin.BodyShowsCavity, Is.True, "boca arriba, con su boca (" + now + " ms)");
                    Assert.That(tin.BaseCapVisible, Is.False, "sin la tapa de la base (" + now + " ms)");
                }

                rose |= cv.BodyOffset.y > 1f;
                settled |= cv.BodyOffset.y < -0.2f;
                fading |= cuch.AsaAlpha > 0.05f && cuch.AsaAlpha < 0.95f;
            }

            Assert.That(waited && drying && tumbling && fading, Is.True, "espera con la boca secándose, se tumba y su cuia se descuelga");
            Assert.That(baseShown && shaded, Is.True, "se ve la base que sube y la cara de canto, más oscura");
            Assert.That(rose && settled, Is.True, "su cuia se descuelga (sube), baja y se asienta en su ranura");
            Assert.That(tin.BodyColor.r + tin.BodyColor.g + tin.BodyColor.b, Is.EqualTo(3f).Within(0.001f), "la cara, otra vez sin sombra");
            Assert.That(tin.UpendCapVisible, Is.False);
            Assert.That(tin.BaseCapVisible, Is.True, "boca abajo: la tapa de la base arriba");
            Rect cap = tin.BaseCapRectU;
            Assert.That(cap.height / cap.width, Is.EqualTo(ProceduralArt.TinajaBaseCapAspect).Within(0.001f), "con la proporción de la boca");
            Assert.That(cap.center.y, Is.EqualTo(ProceduralArt.TinajaBodyHeightU).Within(0.001f), "sobre lo alto de la vasija volteada");
            Assert.That(tin.BodyShowsCavity, Is.False, "y abajo el canto del labio, sin cavidad");
            Assert.That(tin.BodyAngleDeg, Is.EqualTo(0f).Within(0.01f));
            MechanicStepRecord unhook = Find(MechanicStepKind.Unhook);
            MechanicStepRecord upend = Find(MechanicStepKind.Upend);
            Assert.That(unhook.Stage, Is.EqualTo(StepStage.F));
            Assert.That(upend.Stage, Is.EqualTo(StepStage.F));
            Assert.That(unhook.Owner, Is.EqualTo(MechanicType.Cucharon));
            Assert.That(upend.Owner, Is.EqualTo(MechanicType.Tinaja));
            Assert.That(unhook.UsedDefault || upend.UsedDefault, Is.False);
            Assert.That(unhook.EndMs - unhook.StartMs, Is.EqualTo(CucharonOverlay.UnhookMs).Within(1f));
            Assert.That(upend.EndMs - upend.StartMs, Is.EqualTo(TinajaOverlay.UpendHoldMs + TinajaOverlay.UpendMs).Within(1f), "≈ 0,8 s (b.2)");
            Assert.That(unhook.StartMs - t0, Is.GreaterThanOrEqualTo(CuiaBackMs - 1f), "la suelta, con su cuia ya en su ranura");
            Assert.That(upend.StartMs, Is.EqualTo(unhook.StartMs).Within(0.01f), "a la vez: su cuia baja mientras la tinaja espera");
            Assert.That(upend.EndMs - t0, Is.LessThan(MechanicStepQueue.BudgetFromTapMs), "dentro del presupuesto de los pasos");

            Assert.That(tin.UpsideDown, Is.True, "boca abajo sobre el jirau");
            Assert.That(tin.BodyScaleY, Is.EqualTo(-1f));
            Assert.That(tin.HoldingUpright || tin.IsAnimating, Is.False);
            Assert.That(tin.JirauVisible, Is.True, "el jirau es permanente");
            Assert.That(cuch.Visible || cuch.AsaVisible || cuch.PostVisible, Is.False, "su cuia, una Cuia chica normal");
            Assert.That(cv.ShownCuia.Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(tv.Body.parent, Is.SameAs(tv.Root));
            Assert.That(cv.Body.parent, Is.SameAs(cv.Root));
        }

        [UnityTest]
        public IEnumerator SnapAll_MidScoop_LeavesTheModelState_AndNoHold()
        {
            yield return null;
            BoardState s0 = TinajaVisualTests.Board323();
            _rig = new MechanicTestKit.Rig(Art, s0);
            CuiaView tv = _rig.Views[Tinaja];
            CuiaView cv = _rig.Views[Cucharon];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;
            BoardState s1 = _rig.Pour(s0, Tinaja, Cucharon, out _);
            _rig.Run(450);
            _rig.Animator.SnapAll();
            Assert.That(_rig.Animator.IsIdle, Is.True);
            Assert.That(tv.Body.parent, Is.SameAs(tv.Root));
            Assert.That(cv.Body.parent, Is.SameAs(cv.Root));
            Assert.That(cv.BodyAngle, Is.EqualTo(0f).Within(0.01f));
            Assert.That(tv.ShownCuia, Is.EqualTo(s1[Tinaja]));
            Assert.That(cv.ShownCuia, Is.EqualTo(s1[Cucharon]));
            Assert.That(tin.MouthTint, Is.EqualTo(Lulo));
            Assert.That(tin.ChipCount, Is.EqualTo(2));

            // La última sacada, saltada antes de su paso Upend: boca abajo y sin espera.
            BoardState s2 = _rig.Pour(s1, Tinaja, Cucharon, out _);
            _rig.RunUntilIdle(4000);
            BoardState s3 = _rig.Pour(s2, Cucharon, Empty7, out _);
            _rig.RunUntilIdle(4000);
            _rig.Pour(s3, Tinaja, Cucharon, out _);
            _rig.Run(700);
            Assert.That(tin.HoldingUpright, Is.True);
            _rig.Animator.SnapAll();
            Assert.That(tin.HoldingUpright, Is.False);
            Assert.That(tin.UpsideDown, Is.True);
            Assert.That(tin.BodyScaleY, Is.EqualTo(-1f));
            Assert.That(cv.ShownCuia.Mechanic, Is.EqualTo(MechanicType.None));
        }

        [UnityTest]
        public IEnumerator Scoop_DoesNotAllocate_08_5_4()
        {
            yield return null;
            BoardState s0 = TinajaVisualTests.Board323();
            BoardState s1 = PourRules.Apply(s0, Tinaja, Cucharon, PourMode.Run, out PourEvents ev);
            var visual = new PourVisual(new Pour(Tinaja, Cucharon, ev.Amount), ev, s0, s1);
            using (var warm = new MechanicTestKit.Rig(Art, s0))
            {
                warm.Animator.EnqueuePour(in visual);
                warm.RunUntilIdle(4000);
            }

            _rig = new MechanicTestKit.Rig(Art, s0);
            MechanicTestKit.Rig rig = _rig;
            rig.Run(50);
            Assert.That(
                () =>
                {
                    rig.Animator.EnqueuePour(in visual);
                    rig.Run(1600);
                },
                Is.Not.AllocatingGCMemory());
            Assert.That(rig.Animator.IsIdle, Is.True);
        }

        [UnityTest]
        public IEnumerator LastScoop_TheReleaseSteps_DoNotAllocate_08_5_4()
        {
            // La última sacada: sacar con sus gotas, el fondo seco y los pasos Unhook (su cuia se asienta) y Upend (el vuelco).
            yield return null;
            BoardState s3 = Apply(Apply(Apply(TinajaVisualTests.Board323(), Tinaja, Cucharon), Tinaja, Cucharon), Cucharon, Empty7);
            PourVisual visual = Visual(s3, Tinaja, Cucharon);
            Assert.That(visual.Events.CucharonReleasedMask, Is.EqualTo(1 << Cucharon));
            using (var warm = new MechanicTestKit.Rig(Art, s3))
            {
                warm.Animator.EnqueuePour(in visual);
                warm.RunUntilIdle(4000);
            }

            _rig = new MechanicTestKit.Rig(Art, s3);
            MechanicTestKit.Rig rig = _rig;
            rig.Run(50);
            var tin = (TinajaOverlay)rig.Views[Tinaja].OverlayOf(MechanicType.Tinaja)!;
            Assert.That(
                () =>
                {
                    rig.Animator.EnqueuePour(in visual);
                    rig.Run(2600);
                },
                Is.Not.AllocatingGCMemory());
            Assert.That(rig.Animator.IsIdle, Is.True);
            Assert.That(tin.UpsideDown, Is.True, "se ejecutaron los pasos de la suelta");
            Assert.That(tin.DripsStarted, Is.EqualTo(BoardAnimator.ScoopDripCount));
        }

        [Test]
        public void ScoopArc_StaysUnderTheCeiling_NeverBelowTheMouth()
        {
            const float u = 20f;
            const float reach = 4.1f;
            Assert.That(BoardAnimator.ScoopArcTop(0f, 100f, reach, u, float.PositiveInfinity), Is.EqualTo(100f + (BoardAnimator.ScoopArcU * u)), "sin techo: 1,5 u sobre la boca");
            float ceiling = 100f + ((reach + BoardAnimator.ScoopCeilingMarginU + 0.5f) * u);
            Assert.That(BoardAnimator.ScoopArcTop(0f, 100f, reach, u, ceiling), Is.EqualTo(100f + (0.5f * u)).Within(0.001f), "lo justo bajo el techo");
            Assert.That(BoardAnimator.ScoopArcTop(0f, 100f, reach, u, 50f), Is.EqualTo(100f), "nunca por debajo de la boca");
        }

        [Test]
        public void ScoopReach_CountsTheTiltedGlass_AndItsAsa()
        {
            // Su cuia es una Cuia chica de 2 (3,60 u) y su asa llega a 4,00 u: derecha, manda el asa.
            int cap = LevelValidation.CucharonCapacity;
            Rect asa = ProceduralArt.TinajaAsaRectU;
            Assert.That(BoardAnimator.ScoopReachU(cap, 0f, false), Is.EqualTo(CuiaGeometry.HeightU(cap)).Within(0.001f), "derecha: su borde");
            Assert.That(BoardAnimator.ScoopReachU(cap, 0f, true), Is.EqualTo(asa.yMax).Within(0.001f), "derecha: lo alto del asa");
            float tilt = BoardAnimator.ScoopTiltDeg * Mathf.Deg2Rad;
            float corner = Mathf.Sqrt((asa.xMax * asa.xMax) + (asa.yMax * asa.yMax));
            Assert.That(Mathf.Atan2(asa.xMax, asa.yMax), Is.LessThan(tilt), "la esquina del asa pasa por la vertical del pie antes de los 20°");
            Assert.That(BoardAnimator.ScoopReachU(cap, BoardAnimator.ScoopTiltDeg, true), Is.EqualTo(corner).Within(0.001f), "inclinándose a 20°: la esquina del asa, justo encima del pie");
            Assert.That(BoardAnimator.ScoopReachU(cap, BoardAnimator.ScoopTiltDeg, false), Is.GreaterThan(CuiaGeometry.HeightU(cap) * Mathf.Cos(tilt)), "el vidrio girado, con su esquina");
        }

        [UnityTest]
        public IEnumerator Scoop_UnderALowCeiling_ItsCuiaSinksInTheMouth_AsaIncluded()
        {
            // El techo a 2 u sobre el pie de su cuia en la boca (revisión de TJ3): de pie en la boca, su cuia (≈ 4,1 u con el asa
            // y la inclinación) no cabe; se hunde tras la tinaja y nada de ella pasa del techo en ningún fotograma.
            yield return null;
            BoardState board = TinajaVisualTests.Board323();
            _rig = new MechanicTestKit.Rig(Art, board);
            CuiaView tv = _rig.Views[Tinaja];
            CuiaView cv = _rig.Views[Cucharon];
            float u = cv.Unit;
            float dipY = BoardAnimator.ScoopFootFor(tv).y;
            float ceiling = dipY + (2f * u);
            _rig.Animator.ScoopCeilingY = ceiling;
            _rig.Pour(board, Tinaja, Cucharon, out PourVisual _);
            float lipTop = tv.FootPosition.y + (ProceduralArt.TinajaTopU * tv.Unit);
            float maxFoot = float.MinValue;
            bool sank = false;
            for (int t = 0; t < 1200 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                float top = CuiaTopY(cv);
                Assert.That(top, Is.LessThanOrEqualTo(ceiling + 0.5f), "bajo el techo, con el asa y la esquina girada (" + t + " ms)");
                float foot = cv.FootPosition.y + cv.BodyOffset.y;
                maxFoot = Mathf.Max(maxFoot, foot);
                if (t >= 360 && t <= 520)
                {
                    Assert.That(foot, Is.LessThan(dipY - u), "hundida en la boca (" + t + " ms)");
                    Assert.That(cv.BodyAngle, Is.EqualTo(0f).Within(0.01f), "derecha: hundida más de ScoopUprightSinkU (" + t + " ms)");
                    Assert.That(top, Is.GreaterThan(lipTop), "pero asoma sobre el labio (" + t + " ms)");
                    sank = true;
                }
            }

            Assert.That(sank, Is.True);
            Assert.That(maxFoot, Is.GreaterThan(cv.FootPosition.y + u), "el arco sigue subiendo a la boca");
            _rig.RunUntilIdle(4000);
        }

        [UnityTest]
        public IEnumerator Scoop_InAThreeRowBoard_ItsCuiaAndItsAsaStayUnderTheHud()
        {
            // El caso real (D-006: 3 filas desde 11 cuias, lo normal en 323-700): la tinaja en la fila de arriba, bajo la cabecera.
            // En cada fotograma de sacar, lo más alto de su cuia (el vidrio girado y el asa) queda bajo el techo de GameScreen. Con
            // la pantalla de un teléfono de 360 × 780 dp (la de las capturas): la de batchmode es otra y el techo queda lejos.
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            // El caso del arreglo es el de la fila 1 pegada al HUD: desde D-046 (punto 3) solo sin la franja de la tropa (16:9);
            // aquí, con el tablero de antes en el mismo lienzo.
            BoardLayout.TroopBandOffForTests = true; // el tablero de 3 filas de antes de D-046 (sin la franja de la tropa), el respaldo
            yield return PlayModeHelpers.LoadGameWithPack(TinajaStepsSnapshotTests.Pack323ThreeRows(), 323, new MemoryProgressStore(323, 322));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            for (int t = 0; t < 12000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            if (flow.Phase != FlowPhase.Playing)
            {
                boot.TapAnywhere();
            }

            PlayModeHelpers.StepUntilIdle(boot, 6000);
            GameScreen screen = boot.View!;
            BoardAnimator animator = screen.Animator;
            CuiaView tv = screen.Views[Tinaja];
            CuiaView cv = screen.Views[Cucharon];
            float ceiling = animator.ScoopCeilingY;
            Assert.That(float.IsPositiveInfinity(ceiling), Is.False, "GameScreen fija el techo bajo la cabecera");
            float dipY = BoardAnimator.ScoopFootFor(tv).y;
            Assert.That(dipY + (BoardAnimator.ScoopReachU(cv.Capacity, BoardAnimator.ScoopTiltDeg, true) * cv.Unit), Is.GreaterThan(ceiling), "de pie en la boca no cabría: el caso del arreglo");

            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            float highest = float.MinValue;
            bool inMouth = false;
            for (int t = 0; t < 2000 && !animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                float top = CuiaTopY(cv);
                highest = Mathf.Max(highest, top);
                Assert.That(top, Is.LessThanOrEqualTo(ceiling + 0.5f), "su cuia no invade la cabecera (" + t + " ms; techo " + ceiling + ")");
                if (Mathf.Abs((cv.FootPosition.x + cv.BodyOffset.x) - tv.FootPosition.x) < 0.5f)
                {
                    // En la boca: hundida tras la tinaja (más de 1 u), así que entra derecha (ScoopTiltFor).
                    Assert.That(cv.FootPosition.y + cv.BodyOffset.y, Is.LessThan(dipY - (BoardAnimator.ScoopUprightSinkU * cv.Unit)), "hundida en la boca (" + t + " ms)");
                    Assert.That(cv.BodyAngle, Is.EqualTo(0f).Within(0.01f), "y derecha (" + t + " ms)");
                    inMouth = true;
                }
            }

            Assert.That(inMouth, Is.True, "sacó en la boca");
            Assert.That(highest, Is.GreaterThan(ceiling - cv.Unit), "y llegó cerca del techo: el tope se probó");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(flow.Session.Current[Cucharon].Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ReducedMotion_ScoopAndRelease_FadesWithoutRingBounceOrTumble_SameState()
        {
            // Reducir movimiento (b.2: «Fundidos de 0,25 s»): el revelado sin anillo ni rebote del chip, sin gotas; la suelta sin
            // giro ni cara del vuelco (un fundido cruzado a la vasija boca abajo) y su cuia quieta; el estado final, el mismo.
            yield return null;
            BoardState s0 = TinajaVisualTests.Board323();
            _rig = new MechanicTestKit.Rig(Art, s0);
            _rig.Animator.ReduceMotion = true;
            CuiaView tv = _rig.Views[Tinaja];
            CuiaView cv = _rig.Views[Cucharon];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;
            var cuch = (CucharonOverlay)cv.OverlayOf(MechanicType.Cucharon)!;
            _rig.Animator.Steps.ClearLog();
            BoardState s1 = _rig.Pour(s0, Tinaja, Cucharon, out _);
            bool revealed = false;
            for (int t = 0; t < 4000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                Assert.That(tin.RingVisible, Is.False, "sin anillo (" + t + " ms)");
                Assert.That(tin.ChipScale, Is.EqualTo(1f), "sin rebote del chip (" + t + " ms)");
                Assert.That(tin.ActiveDrips, Is.EqualTo(0), "sin gotas (" + t + " ms)");
                revealed |= _rig.Animator.Steps.TryGetRunning(MechanicStepKind.Reveal, Tinaja, out _, out _);
            }

            Assert.That(revealed, Is.True, "el revelado es un paso, con su fundido");
            Assert.That(tin.MouthTint, Is.EqualTo(Lulo));
            Assert.That(tin.ChipCount, Is.EqualTo(2));
            Assert.That(tin.DripsStarted, Is.EqualTo(0));

            BoardState s2 = _rig.Pour(s1, Tinaja, Cucharon, out _);
            _rig.RunUntilIdle(4000);
            BoardState s3 = _rig.Pour(s2, Cucharon, Empty7, out _);
            _rig.RunUntilIdle(4000);
            _rig.Animator.Steps.ClearLog();
            BoardState s4 = _rig.Pour(s3, Tinaja, Cucharon, out _);
            bool fading = false;
            bool upendRan = false;
            bool unhookRan = false;
            for (int t = 0; t < 4000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                Assert.That(tin.UpendCapVisible, Is.False, "sin la cara del vuelco (" + t + " ms)");
                Assert.That(tin.BodyAngleDeg, Is.EqualTo(0f).Within(0.01f), "sin ladearse (" + t + " ms)");
                Assert.That(Mathf.Abs(tin.BodyScaleY), Is.EqualTo(1f), "sin girar: boca arriba o boca abajo (" + t + " ms)");
                Assert.That(cv.OverlayHost.BodyNudgeDp, Is.EqualTo(0f), "su cuia, quieta en su ranura (" + t + " ms)");
                float a = tin.FlipFadeAlpha;
                fading |= a > 0.05f && a < 0.95f;
                upendRan |= _rig.Animator.Steps.TryGetRunning(MechanicStepKind.Upend, Tinaja, out _, out _);
                unhookRan |= _rig.Animator.Steps.TryGetRunning(MechanicStepKind.Unhook, Cucharon, out _, out _);
            }

            Assert.That(upendRan && unhookRan, Is.True, "los pasos de la suelta se ejecutan");
            Assert.That(fading, Is.True, "un fundido cruzado a la vasija boca abajo");
            Assert.That(tin.FlipFadeAlpha, Is.EqualTo(0f), "la copia del fundido se retira");
            Assert.That(s4[Tinaja].IsEmpty, Is.True);
            Assert.That(tin.UpsideDown, Is.True, "el mismo estado final: boca abajo");
            Assert.That(tin.BodyScaleY, Is.EqualTo(-1f));
            Assert.That(tin.BaseCapVisible && !tin.BodyShowsCavity, Is.True);
            Assert.That(tin.BodyColor.a, Is.EqualTo(1f), "opaca");
            Assert.That(tin.HoldingUpright || tin.IsAnimating, Is.False);
            Assert.That(cuch.Visible || cuch.AsaVisible || cuch.PostVisible, Is.False, "su cuia, suelta");
            Assert.That(cv.ShownCuia.Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(cv.OverlayHost.BodyNudgeDp, Is.EqualTo(0f));
        }

        /// <summary>
        /// Lo más alto de la cuia <paramref name="cv"/> en el contenedor, con su pose: el contorno del vidrio
        /// (<see cref="CuiaGeometry.OuterHalfWidth"/>) y el rectángulo del asa de su cuia colgada, girados sobre el centro del pie.
        /// </summary>
        internal static float CuiaTopY(CuiaView cv)
        {
            float u = cv.Unit;
            Vector2 foot = cv.FootPosition + cv.BodyOffset;
            float rad = cv.BodyAngle * Mathf.Deg2Rad;
            float h = cv.HeightU;
            float best = float.MinValue;
            for (int i = 0; i <= 90; i++)
            {
                float y = h * i / 90f;
                float x = CuiaGeometry.OuterHalfWidth(Mathf.Min(y, h - 0.0001f), cv.Capacity);
                best = Mathf.Max(best, foot.y + CuiaView.Rotate(new Vector2(x, y) * u, rad).y);
                best = Mathf.Max(best, foot.y + CuiaView.Rotate(new Vector2(-x, y) * u, rad).y);
            }

            Rect asa = ProceduralArt.TinajaAsaRectU;
            best = Mathf.Max(best, foot.y + CuiaView.Rotate(new Vector2(asa.xMin, asa.yMax) * u, rad).y);
            best = Mathf.Max(best, foot.y + CuiaView.Rotate(new Vector2(asa.xMax, asa.yMax) * u, rad).y);
            best = Mathf.Max(best, foot.y + CuiaView.Rotate(new Vector2(asa.xMin, asa.yMin) * u, rad).y);
            best = Mathf.Max(best, foot.y + CuiaView.Rotate(new Vector2(asa.xMax, asa.yMin) * u, rad).y);
            return best;
        }

        [UnityTest]
        public IEnumerator Scoop_Sounds_GlupThenTheReveal_AndTheReleaseWithItsHaptic_NeverAPour()
        {
            // 02 §4.1: sacar suena «glup» (sfx_tinaja_scoop) al llegar su cuia a la boca, el revelado de la boca, su gota
            // (sfx_tinaja_reveal), y la suelta, roce de fibra y «toc» (sfx_tinaja_release) con un háptico ligero; nunca sfx_pour_*.
            yield return null;
            var audio = new FakeAudioService();
            AudioCues.Install(audio);
            try
            {
                BoardState s0 = TinajaVisualTests.Board323();
                _rig = new MechanicTestKit.Rig(Art, s0);
                BoardState s1 = _rig.Pour(s0, Tinaja, Cucharon, out _);
                _rig.RunUntilIdle(4000);
                AssertNoPour(audio, "sacar");
                Assert.That(audio.Count(SfxId.TinajaScoop, false), Is.EqualTo(1), "una vez por gesto");
                Assert.That(audio.Count(SfxId.TinajaReveal, false), Is.EqualTo(1));
                Assert.That(IndexOf(audio, SfxId.TinajaScoop), Is.LessThan(IndexOf(audio, SfxId.TinajaReveal)), "scoop → reveal");
                Assert.That(audio.Count(SfxId.TinajaRelease, false) + audio.HapticTotal(), Is.EqualTo(0), "sin suelta");

                BoardState s2 = _rig.Pour(s1, Tinaja, Cucharon, out _);
                _rig.RunUntilIdle(4000);
                BoardState s3 = _rig.Pour(s2, Cucharon, Empty7, out _);
                _rig.RunUntilIdle(4000);
                Assert.That(audio.Count(SfxId.PourStart, false), Is.EqualTo(1), "desde su cuia, el vertido normal, con su chorro");

                audio.Clear();
                _rig.Pour(s3, Tinaja, Cucharon, out _);
                _rig.RunUntilIdle(4000);
                AssertNoPour(audio, "la última sacada");
                Assert.That(audio.Count(SfxId.TinajaScoop, false), Is.EqualTo(1));
                Assert.That(audio.Count(SfxId.TinajaReveal, false), Is.EqualTo(0), "vacía: no revela nada");
                Assert.That(audio.Count(SfxId.TinajaRelease, false), Is.EqualTo(1), "la suelta suena una vez (Unhook), no en cada pieza");
                Assert.That(audio.HapticCount(HapticKind.TinajaRelease), Is.EqualTo(1), "con su háptico ligero");
                Assert.That(Haptics.StrengthOf(HapticKind.TinajaRelease), Is.EqualTo(HapticStrength.Light));
                Assert.That(IndexOf(audio, SfxId.TinajaScoop), Is.LessThan(IndexOf(audio, SfxId.TinajaRelease)), "scoop → release");
            }
            finally
            {
                AudioCues.Install(null);
            }
        }

        private static void AssertNoPour(FakeAudioService audio, string what)
        {
            Assert.That(audio.Count(SfxId.PourStart, false) + audio.Count(SfxId.PourEnd, false), Is.EqualTo(0), what + ": sin sfx_pour_*");
            for (int i = 0; i < audio.Events.Count; i++)
            {
                Assert.That(audio.Events[i].Kind, Is.Not.EqualTo(AudioEventKind.PourBegin), what + ": sin bucle del chorro");
            }
        }

        private static int IndexOf(FakeAudioService audio, SfxId id)
        {
            for (int i = 0; i < audio.Events.Count; i++)
            {
                if (audio.Events[i].Kind == AudioEventKind.Sfx && audio.Events[i].Sfx == id)
                {
                    return i;
                }
            }

            Assert.Fail("no sonó " + id);
            return -1;
        }

        [Test]
        public void IntroInput_TheTinaja_FruitOnlyInTheMouth_D010()
        {
            Cuia tinaja = TinajaVisualTests.Board323()[Tinaja];
            var input = new IntroInput();
            input.SetCuia(0, 100f, 600f, 0, tinaja);
            Assert.That(input.Jar[0], Is.True);
            Assert.That(input.IsInert(0, 0) && input.IsInert(0, 1), Is.True, "las de debajo no reciben fruta");
            Assert.That(input.IsInert(0, 2), Is.False, "la boca, sí");

            // Ya reveladas (Volver a la orilla): tampoco reciben fruta; la fruta, solo en la boca.
            input.SetCuia(0, 100f, 600f, 0, tinaja.WithRevealed(0b11));
            Assert.That(input.IsInert(0, 0) && input.IsInert(0, 1), Is.True);
            input.SetCuia(0, 100f, 600f, 0, Cuia.Create(4, new[] { Jenipapo, Lulo, Jenipapo }));
            Assert.That(input.Jar[0], Is.False);
            Assert.That(input.IsInert(0, 0), Is.False, "una cuia normal, como siempre");
        }

        [UnityTest]
        public IEnumerator Level323_Intro_FruitInTheMouth_ChipOnTheFirstImpact_D010()
        {
            yield return PlayModeHelpers.LoadGameWithPack(TinajaVisualTests.Pack323(Cucharon, Tinaja), 323, new MemoryProgressStore(323, 322));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen screen = boot.View!;
            LevelFlow flow = boot.Flow!;
            CuiaView tv = screen.Views[Tinaja];
            var tin = (TinajaOverlay)tv.OverlayOf(MechanicType.Tinaja)!;
            bool sawDry = false;
            bool sawChipInIntro = false;
            int introFrames = 0;
            for (int t = 0; t < 12000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (!tin.InIntro)
                {
                    continue;
                }

                IntroChoreography plan = screen.Intro.Plan;
                float ti = screen.Intro.TimeMs;
                introFrames++;
                if (!sawDry)
                {
                    int fruits = 0;
                    for (int k = 0; k < plan.FruitCount; k++)
                    {
                        if (plan.FruitCuia[k] == Tinaja)
                        {
                            fruits++;
                            Assert.That(plan.FruitImpactY[k], Is.EqualTo(screen.Intro.Input.FootY[Tinaja] - (ProceduralArt.TinajaMouthCenterU * tv.Unit)).Within(0.01f), "cae en la boca");
                            Assert.That(plan.FruitTint[k], Is.EqualTo(Jenipapo));
                        }
                    }

                    Assert.That(fruits, Is.EqualTo(1), "una sola fruta, en la boca");
                    Assert.That(plan.FirstImpactMs, Is.GreaterThan(0f), "hay primer impacto");
                    sawDry = true;
                }

                // D-010: el chip y las bandas salen en el primer impacto, ni antes ni después (con la tolerancia de un fotograma).
                bool due = ti >= plan.FirstImpactMs;
                if (Mathf.Abs(ti - plan.FirstImpactMs) > PlayModeHelpers.FrameMs)
                {
                    Assert.That(tin.ChipVisible, Is.EqualTo(due), "el chip, en el primer impacto (" + ti + " ms; impacto a " + plan.FirstImpactMs + ")");
                    if (!due)
                    {
                        Assert.That(tin.BandCount, Is.EqualTo(0), "sin bandas antes del primer impacto (" + ti + " ms)");
                    }
                }

                // La boca, seca hasta que el chorrito de la tinaja la llena.
                if (ti < plan.FillEndMs[Tinaja] - PlayModeHelpers.FrameMs)
                {
                    Assert.That(Opaque(tin.MouthColor), Is.EqualTo(IntroAnimation.DryMouth), "la boca, seca (" + ti + " ms)");
                    Assert.That(tin.DryMouthVisible && !tin.MouthGlossVisible, Is.True, "el fondo seco, mate (" + ti + " ms)");
                    Assert.That(tin.MouthGlyphVisible, Is.False, "sin fruta aún (" + ti + " ms)");
                }

                sawChipInIntro |= tin.ChipVisible;
                Assert.That(tv.Liquid.MurkyLookAt(0), Is.EqualTo(LiquidGraphic.MurkyLook.None));
            }

            Assert.That(introFrames, Is.GreaterThan(10), "la intro se recorre fotograma a fotograma");
            if (flow.Phase != FlowPhase.Playing)
            {
                boot.TapAnywhere();
                PlayModeHelpers.StepUntilIdle(boot, 4000);
            }

            Assert.That(sawDry, Is.True, "la intro de la tinaja");
            Assert.That(sawChipInIntro, Is.True, "el chip sale en la intro, con el primer impacto");
            Assert.That(tin.InIntro, Is.False);
            Assert.That(tin.ChipCount, Is.EqualTo(3));
            Assert.That(tin.MouthTint, Is.EqualTo(Jenipapo));
            Assert.That(tin.MouthGlyphVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator ReducedMotionIntro_TheTinajaHiddenDoses_AreClay_NeverMurky()
        {
            yield return PlayModeHelpers.LoadGameWithPack(TinajaVisualTests.Pack323(Cucharon, Tinaja), 323, new MemoryProgressStore(323, 322));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            flow.ReduceMotion = true;
            flow.Load(323);
            CuiaView tv = boot.View!.Views[Tinaja];
            for (int t = 0; t < 2000; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                for (int k = 0; k < 2; k++)
                {
                    Assert.That(tv.Liquid.MurkyLookAt(k), Is.EqualTo(LiquidGraphic.MurkyLook.None), "capa " + k + " sin «?» (" + t + " ms)");
                    Color c = tv.DoseColor(k);
                    Assert.That(new Color(c.r, c.g, c.b, 1f), Is.Not.EqualTo(Palette.Murky), "capa " + k + " sin el pardo (" + t + " ms)");
                }
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(tv.DoseColor(0), Is.EqualTo(Palette.TinajaClay), "el barro de su vasija");
        }

        [UnityTest]
        public IEnumerator MareaAndBackToTheShore_RehangItsCuia_AndKeepWhatWasRevealed_QTi6()
        {
            yield return PlayModeHelpers.LoadGameWithPack(TinajaVisualTests.Pack323(Cucharon, Tinaja), 323, new MemoryProgressStore(323, 322));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            LevelFlow flow = boot.Flow!;
            GameScreen screen = boot.View!;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            var tin = (TinajaOverlay)screen.Views[Tinaja].OverlayOf(MechanicType.Tinaja)!;
            var cuch = (CucharonOverlay)screen.Views[Cucharon].OverlayOf(MechanicType.Cucharon)!;

            Scoop(boot);
            Scoop(boot);
            Assert.That(flow.Session.RevealedLayers(Tinaja), Is.EqualTo(0b11));
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(flow.Session.Current[Tinaja].Count, Is.EqualTo(2));
            Assert.That(flow.Session.Current[Tinaja].HiddenMask, Is.EqualTo(0), "lo revelado sigue revelado (R7, I5)");
            Assert.That(tin.MouthTint, Is.EqualTo(Lulo));
            Assert.That(tin.BandMask, Is.EqualTo(0b01), "la capa de abajo, ya vista, como banda");
            Assert.That(tin.ChipCount, Is.EqualTo(2));

            // La suelta y su Marea: su cuia se vuelve a colgar.
            Scoop(boot);
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(Empty7).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Scoop(boot);
            Assert.That(flow.Session.Current[Tinaja].IsEmpty, Is.True);
            Assert.That(tin.UpsideDown, Is.True);
            Assert.That(cuch.Visible, Is.False, "suelta");
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(flow.Session.Current[Cucharon].Mechanic, Is.EqualTo(MechanicType.Cucharon));
            Assert.That(cuch.Visible && cuch.PostVisible && cuch.AsaVisible, Is.True, "Marea la vuelve a colgar");
            Assert.That(cuch.AsaAlpha, Is.EqualTo(1f), "con su asa entera");
            Assert.That(tin.UpsideDown, Is.False);
            Assert.That(tin.MouthTint, Is.EqualTo(Jenipapo));
            Assert.That(tin.ChipCount, Is.EqualTo(1));
            Assert.That(flow.Session.RevealedLayers(Tinaja), Is.EqualTo(0b11));

            // Volver a la orilla: el mismo tablero con lo revelado del intento (T6): las dos bandas, «×3» y su cuia colgada.
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla");
            for (int t = 0; t < 8000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (tin.InIntro)
                {
                    Assert.That(tin.UpsideDown, Is.False, "en la intro, boca arriba");
                }
            }

            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.Session.Current[Tinaja].HiddenMask, Is.EqualTo(0), "T6: lo revelado sigue revelado");
            Assert.That(tin.BandMask, Is.EqualTo(0b11));
            Assert.That(tin.ChipCount, Is.EqualTo(3));
            Assert.That(tin.MouthTint, Is.EqualTo(Jenipapo));
            Assert.That(cuch.Visible && cuch.PostVisible, Is.True);
        }

        // Sacar en la escena: tinaja, su cuia y la vista quieta.
        internal static void Scoop(GameBootstrap boot)
        {
            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);
        }

        private MechanicStepRecord Find(MechanicStepKind kind)
        {
            MechanicStepQueue q = _rig!.Animator.Steps;
            for (int i = 0; i < q.LogCount; i++)
            {
                if (q.LogAt(i).Kind == kind)
                {
                    return q.LogAt(i);
                }
            }

            Assert.Fail("sin paso " + kind);
            return default;
        }

        private static BoardState Apply(BoardState before, int source, int target)
        {
            Assert.That(PourRules.IsLegal(before, source, target), Is.True, source + " → " + target);
            return PourRules.Apply(before, source, target, PourMode.Run, out _);
        }

        private static PourVisual Visual(BoardState before, int source, int target)
        {
            Assert.That(PourRules.IsLegal(before, source, target), Is.True, source + " → " + target);
            BoardState after = PourRules.Apply(before, source, target, PourMode.Run, out PourEvents ev);
            return new PourVisual(new Pour(source, target, ev.Amount), ev, before, after);
        }

        private static void AssertEntry(StepEntry e, MechanicStepKind kind, StepStage stage, int index, int other)
        {
            Assert.That(e.Kind, Is.EqualTo(kind), e.ToString());
            Assert.That(e.Stage, Is.EqualTo(stage), e.ToString());
            Assert.That(e.Index, Is.EqualTo(index), e.ToString());
            Assert.That(e.OtherIndex, Is.EqualTo(other), e.ToString());
        }

        private static Color Opaque(Color c)
        {
            return new Color(c.r, c.g, c.b, 1f);
        }
    }

    /// <summary>
    /// Capturas de revisión de los gestos de la Tinaja (tarea TJ3; <c>[Explicit]</c>, no forman parte de la suite), con el 323
    /// del paquete de muestra en la pantalla simulada de <see cref="SnapshotKit"/> (360 × 780 dp): la intro (<c>tj3_01</c>, la
    /// boca seca y sin chip; <c>tj3_02a</c>, el chip en el primer impacto; <c>tj3_02b</c>, la fruta ya florecida solo en la boca,
    /// con su icono, y la barriga lisa), sacar (<c>tj3_03</c>, su cuia en arco sobre la vasija; <c>tj3_04</c>, en la boca,
    /// inclinada 20° y sumergida tras el labio; <c>tj3_07</c>, su cuia saliendo con las gotas que caen en la boca; <c>tj3_05</c>,
    /// la boca oscura del revelado; <c>tj3_06</c>, el anillo y la fruta nueva floreciendo con el chip en su rebote), Marea tras la
    /// primera sacada (<c>tj3_12</c>: «×3», la boca azul y la capa verde revelada como banda de luz con su icono) y Volver a la
    /// orilla en ese estado (<c>tj3_13</c>: la banda verde sigue tras la intro), la suelta (<c>tj3_08</c>, el fondo seco mate
    /// esperando boca arriba; <c>tj3_09</c>, la vasija a mitad del vuelco, con su base y la cara de canto; <c>tj3_10</c>, boca
    /// abajo y su cuia suelta) y Marea tras la suelta (<c>tj3_11</c>: su cuia otra vez colgada y la tinaja con «×1», la boca azul
    /// y la barriga lisa: no queda nada revelado bajo la boca). Aparte, un tablero de 3 filas: el arco de sacar (<c>tj3_14</c>) y
    /// su cuia hundida en la boca (<c>tj3_15</c>), las dos bajo la cabecera, asa incluida. Necesitan gráficos:
    /// <c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.TinajaStepsSnapshotTests</c> sin <c>-nographics</c>;
    /// los PNG quedan en <c>Logs/snapshots/</c>.
    /// </summary>
    [Explicit("Capturas visuales manuales; necesitan dispositivo gráfico.")]
    public sealed class TinajaStepsSnapshotTests
    {
        private const int Cucharon = TinajaVisualTests.CucharonIndex;
        private const int Tinaja = TinajaVisualTests.TinajaIndex;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
            SnapshotKit.Release();
        }

        [UnityTest]
        public IEnumerator CaptureGestures()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGameWithPack(TinajaVisualTests.Pack323(Cucharon, Tinaja), 323, new MemoryProgressStore(323, 322));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            var tin = (TinajaOverlay)boot.View!.Views[Tinaja].OverlayOf(MechanicType.Tinaja)!;

            // La intro: la boca seca y sin chip; el chip en el primer impacto; la fruta florecida en la boca (y nada más).
            bool dry = false;
            bool chip = false;
            bool fruit = false;
            int chipAt = -1;
            int glyphAt = -1;
            for (int t = 0; t < 12000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (!dry && tin.InIntro && t > 64)
                {
                    SnapshotKit.Save(dir, "tj3_01_l323_intro_boca_seca");
                    dry = true;
                }

                if (chipAt < 0 && tin.InIntro && tin.ChipVisible)
                {
                    chipAt = t; // el primer impacto
                }

                if (!chip && chipAt >= 0 && tin.InIntro && t >= chipAt + 48)
                {
                    SnapshotKit.Save(dir, "tj3_02a_l323_intro_chip");
                    chip = true;
                }

                if (glyphAt < 0 && chipAt >= 0 && tin.MouthGlyphVisible)
                {
                    glyphAt = t; // la fruta de la boca, florecida (el tinte entero y su icono)
                }

                if (!fruit && glyphAt >= 0 && t >= glyphAt + 16)
                {
                    SnapshotKit.Save(dir, "tj3_02b_l323_intro_fruta_en_la_boca");
                    fruit = true;
                }
            }

            if (!fruit)
            {
                // La intro acabó antes: la fruta florece en la boca al final del guion (o justo después).
                PlayModeHelpers.Step(boot, 112);
                Debug.Log("[TJ3] tj3_02b tras la intro: icono en la boca " + tin.MouthGlyphVisible + ", glifo a " + glyphAt + " ms");
                SnapshotKit.Save(dir, "tj3_02b_l323_intro_fruta_en_la_boca");
                fruit = tin.MouthGlyphVisible;
            }

            if (flow.Phase != FlowPhase.Playing)
            {
                boot.TapAnywhere();
            }

            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 300);

            // Sacar (con los tiempos por defecto: viaje 300, llenado 280 y vuelta 300; la vuelta empieza a los 580 ms).
            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 240);
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, 176);
            SnapshotKit.Save(dir, "tj3_03_l323_sacar_arco");
            PlayModeHelpers.Step(boot, 256);
            SnapshotKit.Save(dir, "tj3_04_l323_sacar_en_la_boca");
            PlayModeHelpers.Step(boot, 256);
            Assert.That(tin.ActiveDrips, Is.GreaterThan(0), "las gotas de la vuelta");
            SnapshotKit.Save(dir, "tj3_07_l323_vuelta_gotas");
            PlayModeHelpers.Step(boot, 32);
            SnapshotKit.Save(dir, "tj3_05_l323_revelado_oscura");
            PlayModeHelpers.Step(boot, 80);
            SnapshotKit.Save(dir, "tj3_06_l323_revelado_onda");
            PlayModeHelpers.StepUntilIdle(boot, 6000);

            // Marea tras la primera sacada: «×3», la boca azul y la verde revelada como banda (D-019 Q-Ti6; 07 §4.3).
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(tin.BandMask, Is.EqualTo(0b10), "la capa 2, revelada, como banda");
            SnapshotKit.Save(dir, "tj3_12_l323_marea_banda");

            // Volver a la orilla en ese estado: la banda verde sigue después de la intro.
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla");
            for (int t = 0; t < 12000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            if (flow.Phase != FlowPhase.Playing)
            {
                boot.TapAnywhere();
            }

            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(tin.BandMask, Is.EqualTo(0b10), "tras la intro, la banda verde");
            SnapshotKit.Save(dir, "tj3_13_l323_orilla_banda");

            // Hasta la última sacada: la suelta.
            TinajaStepsTests.Scoop(boot);
            TinajaStepsTests.Scoop(boot);
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(7).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, 832);
            Assert.That(tin.DryMouthVisible, Is.True);
            SnapshotKit.Save(dir, "tj3_08_l323_suelta_boca_seca");
            PlayModeHelpers.Step(boot, 560);
            SnapshotKit.Save(dir, "tj3_09_l323_suelta_volcando");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(tin.UpsideDown, Is.True);
            SnapshotKit.Save(dir, "tj3_10_l323_suelta_boca_abajo");

            Assert.That(boot.PressUndo(), Is.True, "Marea");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            SnapshotKit.Save(dir, "tj3_11_l323_marea_colgada");
            Assert.That(dry && chip && fruit, Is.True);
        }

        [UnityTest]
        public IEnumerator CaptureThreeRowArc()
        {
            string dir = SnapshotKit.Prepare();
            yield return PlayModeHelpers.LoadGameWithPack(Pack323ThreeRows(), 323, new MemoryProgressStore(323, 322));
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            for (int t = 0; t < 12000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            if (flow.Phase != FlowPhase.Playing)
            {
                boot.TapAnywhere();
            }

            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 300);
            CuiaView cv = boot.View!.Views[Cucharon];
            BoardAnimator animator = boot.View.Animator;
            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 240);
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            float highest = float.MinValue;
            for (int t = 0; t < 900; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                highest = Mathf.Max(highest, TinajaStepsTests.CuiaTopY(cv)); // el vidrio girado y el asa
                if (t == 176)
                {
                    SnapshotKit.Save(dir, "tj3_14_l323_tres_filas_arco");
                }

                if (t == 432)
                {
                    SnapshotKit.Save(dir, "tj3_15_l323_tres_filas_en_la_boca");
                }
            }

            Debug.Log("[TJ3] tres filas: lo mas alto de su cuia (asa incluida) " + highest + " dp; techo del arco " + animator.ScoopCeilingY + " dp");
            Assert.That(highest, Is.LessThanOrEqualTo(animator.ScoopCeilingY + 0.5f), "su cuia, asa incluida, bajo la cabecera");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
        }

        // El 323 con una cuia vacía de 4 más al final: 11 cuias, 3 filas (D-006).
        internal static string Pack323ThreeRows()
        {
            string json = TinajaVisualTests.Pack323(Cucharon, Tinaja);
            json = Insert(json, "], \"cuias\"", ", 4");
            json = Insert(json, "], \"attrs\"", ", []");
            json = Insert(json, "], \"murky\"", ", null");
            return json;
        }

        private static string Insert(string json, string before, string text)
        {
            int at = json.IndexOf(before, System.StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThan(0), before);
            return json.Substring(0, at) + text + json.Substring(at);
        }
    }
}
