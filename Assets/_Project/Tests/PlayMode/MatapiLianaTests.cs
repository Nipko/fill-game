#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
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
    /// Matapi y Liana con fruta (T12; propuesta de D-007 b.2 M10 y M3; ficha de Magic Sort §2.7 y §3.3): en el 63 el Matapi
    /// como origen se rechaza con la sacudida y el latido del embudo, sin texto ni resaltado (R11; D-002), y su sellado es su
    /// propio paso Seal de 800 ms × factor (tirón, la trampa sale entre gotas y destellos, tapa y brillo) que acaba como el
    /// modelo; en el 83 el Release de la Liana empieza cuando termina el Seal de la cuia de taperebá, con esa cuia como
    /// <c>OtherIndex</c>, la ✓ y el hilo de luz se ven a mitad del paso, el overlay acaba oculto y Marea vuelve a atar la
    /// Liana; el contenido de la Liana se ve; y los pasos no asignan memoria. Los vertidos se buscan con
    /// <see cref="MechanicTestKit"/> y el reloj lo lleva el test.
    /// </summary>
    public sealed class MatapiLianaTests
    {
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

        [UnityTest]
        public IEnumerator Matapi63_RejectPulsesFunnel()
        {
            yield return MechanicTestKit.LoadPlaying(63);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen screen = boot.View!;
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            CuiaView view = screen.Views[matapi];
            var overlay = (MatapiOverlay)view.Overlay!;
            Assert.That(overlay.Visible, Is.True);
            Assert.That(overlay.FunnelPulse, Is.EqualTo(1f), "en reposo, el embudo no late");

            TapResult rejected = boot.TapCuia(matapi);
            Assert.That(rejected.Kind, Is.EqualTo(TapKind.Rejected), "el Matapi nunca es origen (b.2 M10, regla 1)");
            Assert.That(rejected.Selection, Is.EqualTo(-1), "nada queda seleccionado");

            PlayModeHelpers.TickExact(boot, 15);
            Assert.That(Mathf.Abs(view.BodyOffset.x), Is.GreaterThan(1f), "la cuia se sacude (02 §4.1)");
            Assert.That(overlay.FunnelPulse, Is.GreaterThan(1f), "y el embudo late con la sacudida");
            PlayModeHelpers.TickExact(boot, (int)(MatapiOverlay.PulseMs * 0.5f) - 15);
            Assert.That(overlay.FunnelPulse, Is.EqualTo(MatapiOverlay.FunnelPulseScale).Within(0.01f), "pico del latido: 1 → 1,15 → 1");
            AssertNoHighlight(screen, "durante el rechazo");

            PlayModeHelpers.Step(boot, 160);
            Assert.That(overlay.FunnelPulse, Is.EqualTo(1f), "el latido acaba con la sacudida");
            Assert.That(view.BodyOffset.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(overlay.FunnelClosed, Is.False, "sin sellar, el embudo sigue abierto");
            AssertOnlyImages(view.OverlayHost, "el rechazo no lleva texto (R11)");
            AssertNoHighlight(screen, "tras el rechazo");
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Matapi63_SealStepCalledOnReceiver()
        {
            yield return MechanicTestKit.LoadPlaying(63);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen screen = boot.View!;
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.SealedMask & (1 << matapi)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            CuiaView view = screen.Views[matapi];
            var overlay = (MatapiOverlay)view.Overlay!;
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            Assert.That(poured.Pour.Receiver, Is.EqualTo(matapi), "el Matapi solo sella como receptor");

            float maxLift = 0f;
            float maxClose = 0f;
            int maxParticles = 0;
            bool sawCord = false;
            bool sawSlackCord = false;
            bool sawTautCord = false;
            float minCordTilt = 90f;
            bool sawCap = false;
            bool sawGlow = false;
            int elapsed = StepWatching(boot, () =>
            {
                if (screen.Animator.Steps.IsRunning(MechanicStepKind.Seal, matapi))
                {
                    Assert.That(overlay.IsAnimating, Is.True, "el overlay anima su sellado");
                    Assert.That(view.Sealed, Is.False, "la tapa de la cuia la pone el final del paso");
                    maxLift = Mathf.Max(maxLift, overlay.TrapLift);
                    maxClose = Mathf.Max(maxClose, overlay.FunnelClose);
                    maxParticles = Mathf.Max(maxParticles, screen.Fx.ActiveCount);
                    sawCord |= overlay.CordVisible;
                    if (overlay.CordVisible)
                    {
                        sawSlackCord |= overlay.CordSlack;
                        sawTautCord |= !overlay.CordSlack;
                        minCordTilt = Mathf.Min(minCordTilt, overlay.CordTiltDeg);
                    }

                    sawCap |= overlay.StepCapVisible;
                    sawGlow |= overlay.GlowVisible;
                }
                else if (!MechanicTestKit.TryFindRecord(screen.Animator, MechanicStepKind.Seal, matapi, out _))
                {
                    Assert.That(view.Sealed, Is.False, "el trabajo del vertido no sella el Matapi");
                }
            });

            BoardAnimator animator = screen.Animator;
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord seal = MechanicTestKit.Record(animator, MechanicStepKind.Seal, matapi);
            Assert.That(seal.Stage, Is.EqualTo(StepStage.C));
            Assert.That(seal.Owner, Is.EqualTo(MechanicType.Matapi), "PlayStep(Seal) del Matapi");
            Assert.That(seal.UsedDefault, Is.False, "el overlay anima el sellado, no el paso por defecto");
            Assert.That(seal.EndMs - seal.StartMs, Is.EqualTo(MatapiOverlay.SealMs * seal.SpeedFactor).Within(0.5f), "800 ms × factor de velocidad");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            TestContext.WriteLine("Nivel 63: " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(animator));

            Assert.That(sawCord, Is.True, "tirón de cuerda");
            Assert.That(sawSlackCord && sawTautCord, Is.True, "la cuerda asoma floja (catenaria) y el tirón la tensa");
            Assert.That(minCordTilt, Is.GreaterThan(15f), "inclinada, para que se lea como cuerda y no como una antena");
            Assert.That(maxClose, Is.GreaterThan(1f), "el embudo se cierra con rebote");
            Assert.That(maxLift, Is.GreaterThan(0.5f * MatapiOverlay.TrapLiftU), "la trampa sube un poco");
            Assert.That(sawCap, Is.True, "la tapa cae");
            Assert.That(sawGlow, Is.True, "aparece un brillo");
            Assert.That(maxParticles, Is.GreaterThan(0), "gotas y destellos (FxLayer)");
            Assert.That(maxParticles, Is.LessThanOrEqualTo(FxLayer.MaxParticles));

            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(view.Sealed, Is.True, "sellado como el modelo");
            Assert.That(overlay.Visible, Is.True, "el Matapi sigue a la vista: nunca se abre");
            Assert.That(overlay.FunnelClosed, Is.True, "con el embudo cerrado");
            float height = CuiaGeometry.HeightU(view.Capacity);
            Assert.That(overlay.FunnelCenterU - (0.5f * overlay.FunnelHeightU), Is.GreaterThanOrEqualTo(height - CuiaGeometry.RimU - 0.01f), "el embudo cerrado queda entero sobre el borde: el cuello de vidrio, despejado");
            Assert.That(overlay.TieVisible, Is.False, "sin el aro de cuerda bajo la tapa");
            Assert.That(overlay.TrapLift, Is.EqualTo(0f), "la trampa vuelve a su sitio");
            Assert.That(overlay.StepCapVisible || overlay.GlowVisible || overlay.CordVisible, Is.False, "sin piezas del paso");
        }

        [UnityTest]
        public IEnumerator Liana83_ReleaseAfterSealOfTapereba()
        {
            yield return MechanicTestKit.LoadPlaying(83);
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            int key = boot.Flow!.Level.Attrs[liana].Key;
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            var overlay = (LianaOverlay)boot.View!.Views[liana].Overlay!;
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out TapResult poured);
            int receiver = poured.Pour.Receiver;
            BoardState after = boot.Flow.Session.Current;
            Assert.That(after.IsSealed(receiver) && after[receiver].TopColor == key, Is.True, "el vertido sella una cuia de taperebá, la llave");
            int elapsed = MechanicTestKit.StepToIdle(boot, 6000);

            BoardAnimator animator = boot.View.Animator;
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicStepRecord seal = MechanicTestKit.Record(animator, MechanicStepKind.Seal, receiver);
            MechanicStepRecord release = MechanicTestKit.Record(animator, MechanicStepKind.Release, liana);
            Assert.That(release.Owner, Is.EqualTo(MechanicType.Liana));
            Assert.That(release.UsedDefault, Is.False, "el overlay anima el desbloqueo");
            Assert.That(release.OtherIndex, Is.EqualTo(receiver), "OtherIndex = la cuia recién sellada con la llave");
            Assert.That(release.StartMs, Is.EqualTo(seal.EndMs).Within(MechanicTestKit.LogToleranceMs), "empieza justo cuando termina el Seal de esa cuia. " + MechanicTestKit.Describe(animator));
            Assert.That(release.EndMs - release.StartMs, Is.EqualTo(LianaOverlay.ReleaseMs * release.SpeedFactor).Within(0.5f), "750 ms × factor de velocidad");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            TestContext.WriteLine("Nivel 83: " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(animator));

            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(overlay.Visible, Is.False, "suelta: el overlay queda oculto");
            Assert.That(overlay.IsAnimating, Is.False);
            Assert.That(overlay.BeamVisible, Is.False, "el hilo de luz se apaga");
            Assert.That(overlay.CheckVisible, Is.False);
            Assert.That(VisibleIn(overlay.AttachedHost!.BodyFront), Is.EqualTo(0), "ninguna pieza de la liana a la vista");
        }

        [UnityTest]
        public IEnumerator Liana83_CheckAndBeamVisibleMidStep()
        {
            yield return MechanicTestKit.LoadPlaying(83);
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            CuiaView view = boot.View!.Views[liana];
            var overlay = (LianaOverlay)view.Overlay!;
            TapResult poured = MechanicTestKit.Tap(boot, path[path.Length - 1]);
            int receiver = poured.Pour.Receiver;
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            Assert.That(overlay.IsAnimating, Is.True);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.5f);

            Assert.That(overlay.ReleaseProgress, Is.EqualTo(0.5f).Within(0.05f), "a mitad del paso");
            Assert.That(overlay.Visible, Is.True, "la liana sigue a la vista mientras se suelta");
            Assert.That(overlay.CheckVisible, Is.True, "la ✓ verde sobre la fruta");
            Assert.That(overlay.BeamVisible, Is.True, "el hilo de luz");
            Assert.That(overlay.Beam.transform.parent, Is.SameAs(overlay.AttachedHost!.BoardFrontLayer), "el hilo va en la capa delantera del tablero");

            BoardAnimator animator = boot.View.Animator;
            float u = view.Unit;
            Vector2 foot = animator.Foot(receiver);
            Vector2 top = foot + (2f * (animator.BodyCenter(receiver) - foot));
            Assert.That(Vector2.Distance(overlay.BeamFrom, top), Is.LessThan(0.5f * u), "sale de la tapa de la cuia recién sellada");
            float cavityTop = CuiaGeometry.CavityTopU(view.Capacity);
            Vector2 fruit = animator.Foot(liana) + (new Vector2(LianaOverlay.FruitCenterXU, cavityTop + LianaOverlay.FruitCenterAboveCavityU) * u);
            Assert.That(Vector2.Distance(overlay.BeamTo, fruit), Is.LessThan(0.5f), "llega a la fruta");
            Assert.That(Vector2.Distance(overlay.Beam.Evaluate(1f), fruit), Is.LessThan(0.5f), "a mitad del paso el cometa ya llegó");
            Assert.That(Vector2.Distance(overlay.Beam.Evaluate(0f), overlay.BeamFrom), Is.GreaterThan(1f * u), "solo se ve la cola del cometa, no el arco entero");
            Assert.That(overlay.Beam.color.a, Is.GreaterThan(0.5f));
            Assert.That(overlay.BeamSparkVisible, Is.True, "con el destello en la punta");
            Assert.That(overlay.FruitGlowVisible, Is.True, "y el brillo de la fruta al llegar");
            AssertArcUnderCeiling(animator, overlay, receiver, liana, u);

            MechanicTestKit.StepToIdle(boot, 4000);
            Assert.That(overlay.Visible || overlay.BeamVisible || overlay.CheckVisible, Is.False, "al terminar, todo oculto");
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Liana83_UndoReties()
        {
            yield return MechanicTestKit.LoadPlaying(83);
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            int key = boot.Flow!.Level.Attrs[liana].Key;
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            var overlay = (LianaOverlay)boot.View!.Views[liana].Overlay!;
            Pour trigger = path[path.Length - 1];

            // Marea con la vista quieta: la liana vuelve atada, verde y en su sitio.
            MechanicTestKit.Tap(boot, trigger);
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(overlay.Visible, Is.False);
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            MechanicTestKit.StepToIdle(boot, 4000);
            Assert.That(boot.Flow.Session.Current[liana].Mechanic, Is.EqualTo(MechanicType.Liana), "el modelo vuelve a atarla");
            AssertTied(overlay, key, "tras Marea");
            MechanicTestKit.AssertViewMatchesModel(boot);

            // Marea a mitad del desbloqueo (02 §14): SnapAll cierra el paso y la liana vuelve igual.
            MechanicTestKit.Tap(boot, trigger);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.8f);
            Assert.That(overlay.FallAmount, Is.GreaterThan(0f), "ya caía");
            Assert.That(overlay.DryAmount, Is.GreaterThan(0.5f), "y estaba seca");
            Assert.That(boot.PressUndo(), Is.True, "Marea a mitad del paso");
            Assert.That(overlay.IsAnimating, Is.False, "SnapAll termina el paso");
            MechanicTestKit.StepToIdle(boot, 4000);
            AssertTied(overlay, key, "tras Marea a mitad del paso");
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Liana_ContentVisible()
        {
            yield return MechanicTestKit.LoadPlaying(83);
            GameBootstrap boot = GameBootstrap.Current!;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Cuia cuia = boot.Flow!.Session.Current[liana];
            CuiaView view = boot.View!.Views[liana];
            var overlay = (LianaOverlay)view.Overlay!;
            Assert.That(overlay.Visible, Is.True);
            Assert.That(cuia.Count, Is.GreaterThan(0), "el 83 ata una cuia con contenido");

            // Q17 A: cada dosis se pinta con su tinte, entera.
            Assert.That(view.LiquidLevel, Is.EqualTo((float)cuia.Count).Within(1e-3f));
            for (int k = 0; k < cuia.Count; k++)
            {
                Assert.That(cuia.IsHiddenAt(k), Is.False, "dosis " + k + " visible en el modelo");
                Assert.That(view.DoseColor(k), Is.EqualTo(Palette.Tint(cuia.ColorAt(k))), "dosis " + k + ": su tinte");
            }

            // La fruta cuelga del nudo sobre el hombro derecho: su centro queda sobre el techo de la cavidad y su celda (con el
            // margen transparente) tapa como mucho una esquina de la dosis de arriba (aviso de las capturas 34 y 39c).
            float u = view.Unit;
            float cavityTop = CuiaGeometry.CavityTopU(view.Capacity);
            Rect cell = LianaOverlay.FruitCellRectU;
            Assert.That(LianaOverlay.FruitCenterAboveCavityU, Is.GreaterThan(0f), "la fruta, encima de la cavidad");
            float overlapW = Mathf.Max(0f, Mathf.Min(cell.xMax, CuiaGeometry.CavityHalfWidthU) - Mathf.Max(cell.xMin, -CuiaGeometry.CavityHalfWidthU));
            float overlapH = Mathf.Max(0f, Mathf.Min(cell.yMax, 0f) - Mathf.Max(cell.yMin, -1f));
            float topDose = 2f * CuiaGeometry.CavityHalfWidthU;
            Assert.That(overlapW * overlapH / topDose, Is.LessThan(0.2f), "la fruta tapa como mucho una esquina de la dosis de arriba");

            // Sobre las dosis solo pasan las vueltas de la liana (trazos finos en diagonal); el resto queda en el hombro y el
            // cuello. Las vueltas son 2-3 en total (b.2 M3), contando la del cuello, que lleva el mismo sprite con hojitas.
            Image[] images = overlay.AttachedHost!.BodyFront.GetComponentsInChildren<Image>(false);
            int bodyWraps = 0;
            int turns = 0;
            for (int i = 0; i < images.Length; i++)
            {
                bool body = images[i].name.StartsWith("LianaWrap", System.StringComparison.Ordinal);
                bool vine = body || (images[i].sprite != null && images[i].sprite.name == "mech_liana_vine");
                if (vine)
                {
                    turns++;
                    Assert.That(images[i].color, Is.EqualTo(LianaOverlay.FreshTint), images[i].name + ": liana atada, verde hoja oscuro");
                }

                if (body)
                {
                    bodyWraps++;
                    continue;
                }

                RectTransform rt = images[i].rectTransform;
                float centerU = (rt.anchoredPosition.y + ((RectTransform)rt.parent).anchoredPosition.y) / u;
                Assert.That(centerU, Is.GreaterThan(cavityTop), images[i].name + " queda por encima de las dosis");
            }

            Assert.That(turns, Is.InRange(2, LianaOverlay.MaxWraps), "2-3 vueltas de liana, con la del cuello (b.2 M3)");
            Assert.That(turns, Is.EqualTo(overlay.WrapCount));
            Assert.That(bodyWraps, Is.EqualTo(overlay.BodyWrapCount).And.EqualTo(overlay.WrapCount - 1), "las del cuerpo, más la del cuello");
        }

        [UnityTest]
        public IEnumerator Liana84_ArcStaysInPanelFromTopRow()
        {
            // En el 84 la Liana está en la fila de abajo y el taperebá se sella en la de arriba: el cometa sale de la fila
            // superior y su arco no pasa del área táctil de las ranuras (no se sale del marco del panel ni pisa el HUD).
            yield return MechanicTestKit.LoadPlaying(84);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardAnimator animator = boot.View!.Animator;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            int key = boot.Flow!.Level.Attrs[liana].Key;
            float lianaFootY = animator.Foot(liana).y;
            float u = boot.View.Views[liana].Unit;
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0 && a.IsSealed(ev.Receiver)
                && a[ev.Receiver].TopColor == key && animator.Foot(ev.Receiver).y > lianaFootY + u);
            MechanicTestKit.PlayPrefix(boot, path);
            var overlay = (LianaOverlay)boot.View.Views[liana].Overlay!;
            TapResult poured = MechanicTestKit.Tap(boot, path[path.Length - 1]);
            int receiver = poured.Pour.Receiver;
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.3f);
            Assert.That(overlay.BeamVisible, Is.True, "el cometa en vuelo");
            Assert.That(Vector2.Distance(overlay.BeamFrom, animator.Foot(receiver)), Is.GreaterThan(4f * u), "sale de la fila de arriba");
            AssertArcUnderCeiling(animator, overlay, receiver, liana, u);
            MechanicTestKit.StepToIdle(boot, 4000);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator LianaCount_NoCheck_BeamToKnot()
        {
            // Liana de cuenta (n = 1, variante del 243): un punto en el nudo y el mismo desbloqueo sin la ✓ ni el brillo de la
            // fruta, con el cometa hasta el nudo; al acabar, oculta.
            yield return null;
            BoardState start = Board(
                new[] { new[] { 3, 3, 3 }, new[] { 3 }, new[] { 1, 2, 1, 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, n: 1), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            MechanicTestKit.Rig rig = _rig;
            CuiaView view = rig.Views[2];
            var overlay = (LianaOverlay)view.Overlay!;
            Assert.That(overlay.Visible, Is.True);
            Assert.That(overlay.ShownKey, Is.EqualTo(0), "sin fruta");
            Assert.That(overlay.CountDots, Is.EqualTo(1), "de cuenta: n = 1 punto en el nudo");

            rig.Pour(start, 1, 0, out _);
            MechanicStepQueue steps = rig.Animator.Steps;
            for (int t = 0; !steps.IsRunning(MechanicStepKind.Release, 2); t += 4)
            {
                Assert.That(t, Is.LessThan(4000), "llega el Release de la Liana de cuenta. " + MechanicTestKit.Describe(rig.Animator));
                rig.Tick(4f);
            }

            Assert.That(steps.TryGetRunning(MechanicStepKind.Release, 2, out float elapsed, out float duration), Is.True);
            for (float left = (0.5f * duration) - elapsed; left > 0f; left -= PlayModeHelpers.FrameMs)
            {
                rig.Tick(Mathf.Min(left, PlayModeHelpers.FrameMs));
            }

            Assert.That(overlay.ReleaseProgress, Is.EqualTo(0.5f).Within(0.02f), "a mitad del paso");
            Assert.That(overlay.CheckVisible, Is.False, "la Liana de cuenta no lleva la ✓");
            Assert.That(overlay.FruitGlowVisible, Is.False, "ni el brillo de la fruta");
            Assert.That(overlay.BeamVisible, Is.True, "pero sí el cometa");
            float u = view.Unit;
            Vector2 knot = rig.Animator.Foot(2) + (new Vector2(LianaOverlay.KnotXU, CuiaGeometry.CavityTopU(view.Capacity) + LianaOverlay.KnotAboveCavityU) * u);
            Assert.That(Vector2.Distance(overlay.BeamTo, knot), Is.LessThan(0.5f), "que llega al nudo, no a la fruta");
            Assert.That(Vector2.Distance(overlay.Beam.Evaluate(1f), knot), Is.LessThan(0.5f), "y a mitad del paso ya llegó");
            Assert.That(overlay.CountDots, Is.EqualTo(1), "los puntos siguen mientras se suelta");

            rig.RunUntilIdle(4000);
            Assert.That(rig.Animator.IsIdle, Is.True);
            Assert.That(overlay.Visible, Is.False, "suelta: oculta");
            Assert.That(overlay.IsAnimating || overlay.BeamVisible || overlay.CheckVisible, Is.False);
            MechanicStepRecord r = MechanicTestKit.Record(rig.Animator, MechanicStepKind.Release, 2);
            Assert.That(r.UsedDefault, Is.False, "el overlay anima el desbloqueo");
            Assert.That(r.OtherIndex, Is.EqualTo(0), "por la cuia sellada");
        }

        [UnityTest]
        public IEnumerator Liana_BeamLeavesWithTheBoard()
        {
            // El cometa vive en la capa delantera del tablero, fuera de la cuia: al cambiar de nivel (GameScreen.BuildBoard
            // destruye las vistas y conserva la capa) no queda ningún LianaBeam huérfano, solo los de las Lianas del nivel nuevo.
            yield return MechanicTestKit.LoadPlaying(83);
            GameBootstrap boot = GameBootstrap.Current!;
            RectTransform front = boot.View!.MechanicFrontLayer;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            Assert.That(BeamNames(front), Is.EqualTo("LianaBeam" + liana), "un cometa por Liana del 83");

            Pour[] win = Trigger(boot, (ev, b, a) => a.IsWon());
            MechanicTestKit.PlayPrefix(boot, win, skipLast: false);
            for (int t = 0; boot.Flow!.Phase != FlowPhase.VictoryScreen; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(t, Is.LessThan(20000), "pantalla de victoria del 83 (fase " + boot.Flow.Phase + ")");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.PressNext(), Is.True, "Siguiente");
            for (int frames = 0; boot.Flow!.LevelNumber != 84; frames++)
            {
                Assert.That(frames, Is.LessThan(600), "carga el 84 (fase " + boot.Flow.Phase + ")");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                yield return null;
            }

            yield return null; // Destroy de las vistas y, en su OnDestroy, el del cometa: al final del fotograma
            yield return null;
            var expected = new System.Text.StringBuilder();
            CuiaView[] views = boot.View!.Views;
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i].Overlay is LianaOverlay)
                {
                    expected.Append(expected.Length > 0 ? "," : string.Empty).Append("LianaBeam").Append(i);
                }
            }

            Assert.That(BeamNames(front), Is.EqualTo(expected.ToString()), "sin el cometa de la Liana del 83, solo los del 84");
        }

        [Test]
        public void Matapi_RodsLeaveTheCenterClear()
        {
            // 07 §3: las rayas verticales son el patrón del Camu camu y, con los aros, la rejilla del Huito; el centro, el parche
            // de VA-05 y el icono de fruta. Las varillas se juntan en los bordes, como un cilindro visto de frente.
            Assert.That(MatapiOverlay.RodCount, Is.EqualTo(6));
            for (int i = 0; i < MatapiOverlay.RodCount; i++)
            {
                float x = MatapiOverlay.RodXU(i);
                Assert.That(Mathf.Abs(x), Is.GreaterThanOrEqualTo(MatapiOverlay.RodClearHalfU), "varilla " + i + " fuera del centro");
                Assert.That(Mathf.Abs(x), Is.LessThan(CuiaGeometry.CavityHalfWidthU + 0.1f), "varilla " + i + " sobre el cuerpo");
                Assert.That(MatapiOverlay.RodXU(MatapiOverlay.RodCount - 1 - i), Is.EqualTo(-x).Within(1e-5f), "simétricas");
            }
        }

        [UnityTest]
        public IEnumerator NoAllocDuringSteps()
        {
            yield return null;

            // Calentamiento: las mismas cadenas una vez (JIT y primeras llamadas), y después se miden en bancos nuevos.
            for (int pass = 0; pass < 2; pass++)
            {
                RunChains(pass == 1);
            }
        }

        // Sellado de un Matapi, Liana con fruta y Liana de cuenta en el banco; con measure, 60 fotogramas sin asignaciones
        // (GC.Alloc del Recorder, por la restricción AllocatingGCMemory) desde el toque y otros 60 hasta el final.
        private void RunChains(bool measure)
        {
            BoardState[] starts =
            {
                Board(
                    new[] { new[] { 6, 6, 6 }, new[] { 6 }, new[] { 1, 2, 1, 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                    new[] { new MechanicSpec(MechanicType.Matapi), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None }),
                Board(
                    new[] { new[] { 3, 3, 3 }, new[] { 3 }, new[] { 1, 2, 1, 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                    new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, key: 3), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None }),
                Board(
                    new[] { new[] { 3, 3, 3 }, new[] { 3 }, new[] { 1, 2, 1, 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                    new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, n: 1), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None }),
            };

            MechanicStepKind[] kinds = { MechanicStepKind.Seal, MechanicStepKind.Release, MechanicStepKind.Release };
            int[] owners = { 0, 2, 2 };
            MechanicType[] types = { MechanicType.Matapi, MechanicType.Liana, MechanicType.Liana };
            for (int c = 0; c < starts.Length; c++)
            {
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, starts[c]);
                _rig.Pour(starts[c], 1, 0, out _);
                MechanicTestKit.Rig rig = _rig;
                if (measure)
                {
                    Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), "cadena " + c + ": 60 fotogramas desde el toque");
                    Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), "cadena " + c + ": 60 fotogramas más");
                }

                rig.RunUntilIdle(4000);
                Assert.That(rig.Animator.IsIdle, Is.True);
                MechanicStepRecord r = MechanicTestKit.Record(rig.Animator, kinds[c], owners[c]);
                Assert.That(r.Owner, Is.EqualTo(types[c]), "cadena " + c + ": el paso lo anima su overlay");
                Assert.That(r.UsedDefault, Is.False, "cadena " + c);
                if (c == 2)
                {
                    Assert.That(r.OtherIndex, Is.EqualTo(0), "la Liana de cuenta se suelta por la cuia sellada");
                }
            }
        }

        // La Liana atada y en reposo: verde, en su sitio, con su fruta y sin piezas del paso.
        private static void AssertTied(LianaOverlay overlay, int key, string when)
        {
            Assert.That(overlay.Visible, Is.True, when + ": atada");
            Assert.That(overlay.IsAnimating, Is.False, when);
            Assert.That(overlay.ShownKey, Is.EqualTo(key), when + ": con su fruta");
            Assert.That(overlay.VineTint, Is.EqualTo(LianaOverlay.FreshTint), when + ": verde");
            Assert.That(overlay.DryAmount, Is.EqualTo(0f), when);
            Assert.That(overlay.FallAmount, Is.EqualTo(0f), when + ": en su sitio");
            Assert.That(overlay.BeamVisible || overlay.CheckVisible, Is.False, when + ": sin hilo ni ✓");
            Assert.That(VisibleIn(overlay.AttachedHost!.BodyFront), Is.GreaterThanOrEqualTo(overlay.WrapCount + 2), when + ": vueltas, nudo y fruta");
        }

        // El arco entero del cometa queda bajo el techo: el canto inferior de la ranura (la repisa) de la fila de arriba de la
        // más alta de las dos cuias o, si esa es la fila superior, el área táctil de las ranuras; con el margen y la cabeza.
        private static void AssertArcUnderCeiling(BoardAnimator animator, LianaOverlay overlay, int sealedCuia, int liana, float u)
        {
            var board = (ICuiaBoard)animator;
            float highest = Mathf.Max(board.Foot(sealedCuia).y, board.Foot(liana).y);
            float above = float.MaxValue;
            float slotsTop = float.MinValue;
            for (int i = 0; i < board.Count; i++)
            {
                Rect slot = board.SlotRect(i);
                slotsTop = Mathf.Max(slotsTop, slot.yMax);
                if (board.Foot(i).y > highest + (0.5f * u))
                {
                    above = Mathf.Min(above, slot.yMin);
                }
            }

            float ceiling = above < float.MaxValue ? above : slotsTop;
            string where = above < float.MaxValue ? "bajo la repisa de la fila de arriba" : "dentro del área de las ranuras";
            Assert.That(overlay.BeamPeakY, Is.LessThanOrEqualTo(ceiling - LianaOverlay.BeamCeilingMarginDp + 0.01f), "el arco del cometa queda " + where);
            Assert.That(overlay.BeamPeakY, Is.GreaterThanOrEqualTo(Mathf.Max(overlay.BeamFrom.y, overlay.BeamTo.y) - 0.01f), "y no baja de sus extremos");
        }

        // Nombres de los cometas de Liana (hijos directos de la capa delantera del tablero), separados por comas.
        private static string BeamNames(RectTransform front)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < front.childCount; i++)
            {
                string name = front.GetChild(i).name;
                if (name.StartsWith("LianaBeam", System.StringComparison.Ordinal))
                {
                    names.Append(names.Length > 0 ? "," : string.Empty).Append(name);
                }
            }

            return names.ToString();
        }

        // D-002: ninguna cuia se eleva ni cambia el brillo de su vidrio por el rechazo.
        private static void AssertNoHighlight(GameScreen screen, string when)
        {
            for (int i = 0; i < screen.Views.Length; i++)
            {
                Assert.That(screen.Views[i].IsLifted, Is.False, when + ": cuia " + i + " sin elevar");
                Assert.That(screen.Views[i].GlassBoosted, Is.False, when + ": cuia " + i + " sin brillo de selección");
            }
        }

        // Las piezas del overlay son imágenes: ningún texto.
        private static void AssertOnlyImages(CuiaOverlayHost host, string message)
        {
            Transform[] anchors = { host.SlotBack, host.BodyBack, host.BodyFront, host.SlotFront };
            for (int a = 0; a < anchors.Length; a++)
            {
                Graphic[] graphics = anchors[a].GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    Assert.That(graphics[i] is Image, Is.True, message + ": " + graphics[i].name);
                }
            }
        }

        private static int VisibleIn(Transform anchor)
        {
            int count = 0;
            Graphic[] graphics = anchor.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].enabled && graphics[i].gameObject.activeInHierarchy && graphics[i].color.a > 0.001f)
                {
                    count++;
                }
            }

            return count;
        }

        // Vertidos del nivel cargado hasta uno que cumple wanted (el último del camino).
        private static Pour[] Trigger(GameBootstrap boot, MechanicTestKit.PourWanted wanted)
        {
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, wanted);
            Assert.That(path, Is.Not.Null, "vertidos que disparan el evento en el nivel " + boot.Flow!.LevelNumber);
            return path!;
        }

        // Avanza de fotograma en fotograma hasta la vista quieta, llamando a each en cada uno; devuelve los ms.
        private static int StepWatching(GameBootstrap boot, System.Action each)
        {
            int t = 0;
            while (!boot.View!.IsViewIdle)
            {
                Assert.That(t, Is.LessThan(6000), "la vista no se quedó quieta");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                each();
            }

            return t;
        }

        private static BoardState Board(int[][] cuias, MechanicSpec[] attrs)
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
    }
}
