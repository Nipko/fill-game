#nullable enable
using System.Collections;
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
            _rig?.Dispose();
            _rig = null;
            MapArtRule.ForceProvisional = null;
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
            BoardState s1 = _rig.Pour(board, Tinaja, Cucharon, out PourVisual _);
            float maxCuiaAngle = 0f;
            float maxCuiaRise = 0f;
            bool layered = false;
            for (int t = 0; t < (int)CuiaBackMs - 40; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                Assert.That(tv.BodyAngle, Is.EqualTo(0f).Within(0.01f), "la tinaja no se inclina (" + t + " ms)");
                Assert.That(tv.BodyOffset.magnitude, Is.LessThan(0.01f), "ni se mueve (" + t + " ms)");
                Assert.That(_rig.Animator.TryGetPourPose(Tinaja, out _, out _), Is.False, "sin pose de vertido en la tinaja");
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
            bool tumbling = false;
            bool fading = false;
            for (int t = 0; t < 4000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                float now = _rig.Animator.Clock - t0;
                if (now > RevealStartMs + 20f && now < CuiaBackMs - 20f)
                {
                    // La boca enseña el fondo seco mientras su cuia vuelve: la tinaja vacía aún boca arriba y su cuia, colgada.
                    Assert.That(tin.HoldingUpright, Is.True, "espera boca arriba (" + now + " ms)");
                    Assert.That(tin.UpsideDown, Is.False);
                    Assert.That(tin.MouthVisible, Is.True, "con la boca seca");
                    Assert.That(Opaque(tin.MouthColor), Is.EqualTo(Opaque(Palette.WoodDark)));
                    Assert.That(tin.MouthGlyphVisible || tin.ChipVisible, Is.False, "sin fruta ni chip");
                    Assert.That(cuch.Visible, Is.True, "su cuia sigue colgada hasta su paso");
                    waited = true;
                }

                tumbling |= tin.BodyScaleY > -0.9f && tin.BodyScaleY < 0.9f;
                fading |= cuch.AsaAlpha > 0.05f && cuch.AsaAlpha < 0.95f;
            }

            Assert.That(waited && tumbling && fading, Is.True, "espera, se tumba y su cuia se descuelga");
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
            for (int t = 0; t < 12000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (!tin.InIntro)
                {
                    continue;
                }

                IntroChoreography plan = screen.Intro.Plan;
                if (!sawDry)
                {
                    Assert.That(tin.ChipVisible || tin.BandCount > 0, Is.False, "sin chip ni bandas antes del primer impacto");
                    Assert.That(Opaque(tin.MouthColor), Is.EqualTo(IntroAnimation.DryMouth), "la boca, seca");
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
                    sawDry = true;
                }

                sawChipInIntro |= tin.ChipVisible;
                Assert.That(tv.Liquid.MurkyLookAt(0), Is.EqualTo(LiquidGraphic.MurkyLook.None));
            }

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
    /// boca seca y sin chip; <c>tj3_02</c>, tras el primer impacto: el chip y la fruta en la boca), sacar (<c>tj3_03</c>, su cuia
    /// en arco sobre la vasija; <c>tj3_04</c>, en la boca, inclinada 20° y por detrás del labio; <c>tj3_05</c>, la boca oscura
    /// del revelado; <c>tj3_06</c>, el anillo y la fruta nueva floreciendo con el chip en su rebote; <c>tj3_07</c>, su cuia de
    /// vuelta con las gotas), la suelta (<c>tj3_08</c>, la boca seca esperando boca arriba; <c>tj3_09</c>, su cuia
    /// descolgándose y la vasija a mitad del vuelco; <c>tj3_10</c>, boca abajo y su cuia suelta) y Marea (<c>tj3_11</c>: su cuia
    /// otra vez colgada y lo revelado como bandas). Necesitan gráficos:
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

            // La intro: la boca seca y sin chip; después del primer impacto, el chip y la fruta en la boca.
            bool dry = false;
            bool chip = false;
            int chipAt = -1;
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

                if (!chip && chipAt >= 0 && tin.InIntro && t >= chipAt + 96)
                {
                    SnapshotKit.Save(dir, "tj3_02_l323_intro_fruta_en_la_boca");
                    chip = true;
                }
            }

            if (flow.Phase != FlowPhase.Playing)
            {
                boot.TapAnywhere();
            }

            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 300);

            // Sacar.
            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 240);
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, 176);
            SnapshotKit.Save(dir, "tj3_03_l323_sacar_arco");
            PlayModeHelpers.Step(boot, 256);
            SnapshotKit.Save(dir, "tj3_04_l323_sacar_en_la_boca");
            PlayModeHelpers.Step(boot, 240);
            SnapshotKit.Save(dir, "tj3_05_l323_revelado_oscura");
            PlayModeHelpers.Step(boot, 128);
            SnapshotKit.Save(dir, "tj3_06_l323_revelado_onda");
            PlayModeHelpers.Step(boot, 64);
            SnapshotKit.Save(dir, "tj3_07_l323_vuelta_gotas");
            PlayModeHelpers.StepUntilIdle(boot, 6000);

            // Hasta la última sacada: la suelta.
            TinajaStepsTests.Scoop(boot);
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(7).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            Assert.That(boot.TapCuia(Tinaja).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(Cucharon).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.Step(boot, 736);
            SnapshotKit.Save(dir, "tj3_08_l323_suelta_boca_seca");
            PlayModeHelpers.Step(boot, 704);
            SnapshotKit.Save(dir, "tj3_09_l323_suelta_volcando");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(tin.UpsideDown, Is.True);
            SnapshotKit.Save(dir, "tj3_10_l323_suelta_boca_abajo");

            Assert.That(boot.PressUndo(), Is.True, "Marea");
            PlayModeHelpers.StepUntilIdle(boot, 6000);
            PlayModeHelpers.Step(boot, 200);
            SnapshotKit.Save(dir, "tj3_11_l323_marea_colgada");
            Assert.That(dry && chipAt >= 0, Is.True);
        }
    }
}
