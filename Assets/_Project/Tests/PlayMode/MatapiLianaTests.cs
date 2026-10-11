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
    /// Liana; entre filas el cometa sale de la tapa con un destello y baja (o sube) por un pasillo entre columnas sin pasar
    /// por delante de otras cuias; el contenido de la Liana se ve; y los pasos no asignan memoria hasta la vista quieta. Los
    /// vertidos se buscan con <see cref="MechanicTestKit"/> y el reloj lo lleva el test.
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
            bool funnelUntilCapLands = true;
            bool funnelGoneUnderCap = false;
            bool sawCap = false;
            bool sawGlow = false;
            int maxDrips = 0;
            float lowestDrip = float.MaxValue;
            int elapsed = StepWatching(boot, () =>
            {
                if (screen.Animator.Steps.IsRunning(MechanicStepKind.Seal, matapi))
                {
                    Assert.That(overlay.IsAnimating, Is.True, "el overlay anima su sellado");
                    Assert.That(view.Sealed, Is.False, "la tapa de la cuia la pone el final del paso");
                    maxLift = Mathf.Max(maxLift, overlay.TrapLift);
                    int drips = 0;
                    for (int d = 0; d < MatapiOverlay.DripCount; d++)
                    {
                        if (overlay.DripVisible(d))
                        {
                            drips++;
                            Vector2 at = overlay.DripPosU(d);
                            Assert.That(Mathf.Abs(at.x), Is.GreaterThan(CuiaGeometry.BodyOuterHalfWidthU), "gota " + d + " por fuera de la silueta, no dentro del vidrio");
                            lowestDrip = Mathf.Min(lowestDrip, at.y);
                        }
                    }

                    maxDrips = Mathf.Max(maxDrips, drips);
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
                    if (overlay.SealProgress < 0.7f)
                    {
                        funnelUntilCapLands &= overlay.FunnelVisible;
                    }
                    else if (overlay.SealProgress > 0.9f)
                    {
                        funnelGoneUnderCap |= !overlay.FunnelVisible;
                    }
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
            Assert.That(funnelUntilCapLands, Is.True, "el embudo se ve, abierto y luego cerrado, hasta que la tapa toca el borde");
            Assert.That(funnelGoneUnderCap, Is.True, "y se apaga bajo la tapa");
            Assert.That(maxClose, Is.GreaterThan(1f), "el embudo se cierra con rebote");
            Assert.That(maxLift, Is.GreaterThan(0.9f * MatapiOverlay.TrapLiftU), "la trampa sube (unos 7 dp en el pico)");
            Assert.That(maxDrips, Is.GreaterThan(0), "gotean gotas del aro de abajo");
            Assert.That(lowestDrip, Is.LessThan(CuiaGeometry.FootTopU), "y caen hasta la repisa");
            Assert.That(sawCap, Is.True, "la tapa cae");
            Assert.That(sawGlow, Is.True, "aparece un brillo");
            Assert.That(maxParticles, Is.GreaterThan(0), "gotas y destellos (FxLayer)");
            Assert.That(maxParticles, Is.LessThanOrEqualTo(FxLayer.MaxParticles));

            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(view.Sealed, Is.True, "sellado como el modelo");
            Assert.That(overlay.Visible, Is.True, "el Matapi sigue a la vista: nunca se abre");
            Assert.That(overlay.FunnelClosed, Is.True, "con el embudo cerrado");
            Assert.That(overlay.FunnelVisible, Is.False, "bajo la tapa: ninguna trama del embudo sobre el borde, que queda limpio como en las demás selladas");
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
            Assert.That(overlay.BeamRoot.parent, Is.SameAs(overlay.AttachedHost!.BoardFrontLayer), "el hilo va en la capa delantera del tablero");
            Assert.That(overlay.Beam.transform.parent, Is.SameAs(overlay.BeamRoot));

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
            Assert.That(VisibleTailLength(overlay), Is.LessThanOrEqualTo((LianaOverlay.MaxTailU * u) + 1f), "cola corta: 1,5 u como mucho");
            Assert.That(overlay.OriginFlashVisible, Is.False, "el destello de la tapa ya se apagó");
            BezierStripGraphic[] strips = overlay.Beam.GetComponentsInChildren<BezierStripGraphic>(false);
            Assert.That(strips.Length, Is.EqualTo(1 + LianaOverlay.BeamHalos), "el núcleo y las tiras del halo, a la vista");
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

            // La fruta cuelga del nudo sobre el hombro derecho y nunca tapa el líquido (aviso de las capturas 34, 39c y 57-59c):
            // columna a columna, su borde de abajo (el primer píxel visible de la celda) queda al menos 2 dp por encima de la cara
            // superior de la dosis de arriba con la cuia llena (el canto de atrás de la elipse, a la altura de esa columna).
            float u = view.Unit;
            float cavityTop = CuiaGeometry.CavityTopU(view.Capacity);
            AssertFruitClearsFullCuia(boot.Flow!.Level.Attrs[liana].Key, u);

            // Sobre las dosis solo pasan las vueltas de la liana (trazos finos, con su luz de borde de 1 dp); el resto queda en el
            // hombro y el cuello. Las vueltas son 2-3 en total (b.2 M3), contando la del cuello, del mismo grosor, que acaba en el
            // nudo. Las vueltas del cuerpo son paralelas (una hélice: el mismo sprite, sin espejo).
            Image[] images = overlay.AttachedHost!.BodyFront.GetComponentsInChildren<Image>(false);
            int bodyWraps = 0;
            int turns = 0;
            for (int i = 0; i < images.Length; i++)
            {
                string name = images[i].name;
                bool body = name == "LianaWrap0" || name == "LianaWrap1";
                if (body || name == "LianaCollar")
                {
                    turns++;
                    Assert.That(images[i].color, Is.EqualTo(LianaOverlay.FreshTint), name + ": liana atada, verde hoja oscuro");
                }

                if (body)
                {
                    bodyWraps++;
                    Assert.That(images[i].rectTransform.localScale, Is.EqualTo(Vector3.one), name + ": paralela a las demás (hélice)");
                    Assert.That(images[i].sprite.name, Is.EqualTo("mech_liana_coil"));
                    continue;
                }

                if (name.StartsWith("LianaShine", System.StringComparison.Ordinal))
                {
                    continue; // la luz de borde de una vuelta del cuerpo
                }

                RectTransform rt = images[i].rectTransform;
                float centerU = (rt.anchoredPosition.y + ((RectTransform)rt.parent).anchoredPosition.y) / u;
                Assert.That(centerU, Is.GreaterThan(cavityTop), name + " queda por encima de las dosis");
            }

            Assert.That(turns, Is.InRange(2, LianaOverlay.MaxWraps), "2-3 vueltas de liana, con la del cuello (b.2 M3)");
            Assert.That(turns, Is.EqualTo(overlay.WrapCount));
            Assert.That(bodyWraps, Is.EqualTo(overlay.BodyWrapCount).And.EqualTo(overlay.WrapCount - 1), "las del cuerpo, más la del cuello");

            // El tramo trasero de cada vuelta va detrás del líquido (BodyBack): solo se ve sobre el vidrio vacío.
            Image[] back = overlay.AttachedHost.BodyBack.GetComponentsInChildren<Image>(false);
            Assert.That(back.Length, Is.EqualTo(overlay.BodyWrapCount), "un tramo trasero por vuelta del cuerpo");

            // La fruta cuelga delante del nudo (el nudo, detrás: no se lee como su cáliz).
            Transform vine = images[0].transform.parent;
            Assert.That(vine.Find("LianaCollar").GetSiblingIndex(), Is.LessThan(vine.Find("LianaFruit").GetSiblingIndex()), "el nudo, detrás de la fruta");
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
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.05f);
            Assert.That(overlay.OriginFlashVisible, Is.True, "un destello en la tapa de la cuia sellada: se ve de dónde sale");
            Assert.That(Vector2.Distance(overlay.OriginFlashPos, overlay.BeamFrom), Is.LessThan(0.5f), "en la tapa");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.3f);
            Assert.That(overlay.BeamVisible, Is.True, "el cometa en vuelo");
            Assert.That(Vector2.Distance(overlay.BeamFrom, animator.Foot(receiver)), Is.GreaterThan(4f * u), "sale de la fila de arriba");
            AssertArcUnderCeiling(animator, overlay, receiver, liana, u);
            AssertAisleRoute(animator, overlay, receiver, liana, u, "el 84");
            Assert.That(VisibleTailLength(overlay), Is.LessThanOrEqualTo((LianaOverlay.MaxTailU * u) + 1f), "cola corta: 1,5 u como mucho");
            MechanicTestKit.StepToIdle(boot, 4000);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Liana85_RouteUpTheAisleFromTheRowBelow()
        {
            // En el 85 la Liana está en la fila de arriba: si el taperebá se sella en la de abajo, el cometa va por delante de la
            // repisa hasta el pasillo junto a la Liana y sube por él hasta la fruta, sin pasar por delante de ninguna cuia.
            yield return MechanicTestKit.LoadPlaying(85);
            GameBootstrap boot = GameBootstrap.Current!;
            BoardAnimator animator = boot.View!.Animator;
            int liana = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Liana);
            int key = boot.Flow!.Level.Attrs[liana].Key;
            float lianaFootY = animator.Foot(liana).y;
            float u = boot.View.Views[liana].Unit;
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.LianaReleasedMask & (1 << liana)) != 0 && a.IsSealed(ev.Receiver)
                && a[ev.Receiver].TopColor == key && animator.Foot(ev.Receiver).y < lianaFootY - u);
            MechanicTestKit.PlayPrefix(boot, path);
            var overlay = (LianaOverlay)boot.View.Views[liana].Overlay!;
            TapResult poured = MechanicTestKit.Tap(boot, path[path.Length - 1]);
            int receiver = poured.Pour.Receiver;
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Release, liana, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Release, liana, 0.3f);
            Assert.That(overlay.BeamVisible, Is.True, "el cometa en vuelo");
            Assert.That(overlay.BeamTo.y, Is.GreaterThan(overlay.BeamFrom.y + (2f * u)), "sube a la fila de arriba");
            AssertArcUnderCeiling(animator, overlay, receiver, liana, u);
            AssertAisleRoute(animator, overlay, receiver, liana, u, "el 85");
            MechanicTestKit.StepToIdle(boot, 4000);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator LianaCount_NoCheck_BeamToTheDots()
        {
            // Liana de cuenta (n = 1, variante del 243): una cuenta bajo el nudo y el mismo desbloqueo sin la ✓ ni el brillo de la
            // fruta, con el cometa hasta las cuentas; al acabar, oculta.
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
            Assert.That(overlay.CountDots, Is.EqualTo(1), "de cuenta: n = 1 cuenta en la vuelta del cuello");

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
            Vector2 bead = rig.Animator.Foot(2) + (overlay.BeadCenterU(0, 1) * u);
            Assert.That(Vector2.Distance(overlay.BeamTo, bead), Is.LessThan(0.5f), "que llega a la cuenta");
            Assert.That(Vector2.Distance(overlay.Beam.Evaluate(1f), bead), Is.LessThan(0.5f), "y a mitad del paso ya llegó");
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
        public void Matapi_NoRodInFrontOfTheLiquid()
        {
            // 07 §3: el patrón es el canal para daltónicos (rayas verticales = Camu camu; con los aros, la rejilla del Huito).
            // Ninguna varilla pasa por delante del líquido: las de los bordes van enteras sobre la pared de vidrio y las de dentro
            // empiezan sobre la cara superior del líquido (su canto de atrás), con cualquier nivel, también a medio llenar.
            Assert.That(MatapiOverlay.RodCount, Is.EqualTo(6));
            int wall = 0;
            for (int i = 0; i < MatapiOverlay.RodCount; i++)
            {
                float x = MatapiOverlay.RodXU(i);
                float half = 0.5f * MatapiOverlay.RodWidthU(i);
                Assert.That(MatapiOverlay.RodXU(MatapiOverlay.RodCount - 1 - i), Is.EqualTo(-x).Within(1e-5f), "simétricas");
                Assert.That(Mathf.Abs(x) + half, Is.LessThanOrEqualTo(CuiaGeometry.BodyOuterHalfWidthU), "varilla " + i + " sobre el cuerpo");
                if (MatapiOverlay.RodOnWall(i))
                {
                    wall++;
                    Assert.That(Mathf.Abs(x) - half, Is.GreaterThanOrEqualTo(CuiaGeometry.CavityHalfWidthU), "varilla " + i + " entera sobre la pared");
                }
            }

            Assert.That(wall, Is.EqualTo(2), "las dos de los bordes, sobre la pared; las otras cuatro, dentro");
            for (int cap = 2; cap <= 6; cap++)
            {
                for (float level = 0f; level <= cap + 1e-3f; level += 0.125f)
                {
                    int shownInside = 0;
                    for (int i = 0; i < MatapiOverlay.RodCount; i++)
                    {
                        bool shown = MatapiOverlay.RodSpanU(i, cap, level, out float bottom, out float top);
                        string at = "varilla " + i + ", capacidad " + cap + ", nivel " + level;
                        if (MatapiOverlay.RodOnWall(i))
                        {
                            Assert.That(shown, Is.True, at + ": la de la pared siempre se ve");
                            continue;
                        }

                        if (!shown)
                        {
                            continue;
                        }

                        shownInside++;
                        Assert.That(top - bottom, Is.GreaterThanOrEqualTo(MatapiOverlay.MinRodU), at);
                        if (level > 0f)
                        {
                            float inner = Mathf.Abs(MatapiOverlay.RodXU(i)) - (0.5f * MatapiOverlay.RodWidthU(i));
                            float face = CuiaGeometry.CavityBottomU + level + CuiaGeometry.FaceSagU(inner);
                            Assert.That(bottom, Is.GreaterThan(face), at + ": empieza sobre la cara superior del líquido");
                        }
                    }

                    if (level <= cap - 1f)
                    {
                        Assert.That(shownInside, Is.EqualTo(MatapiOverlay.RodCount - 2), "con una dosis vacía o más, las de dentro se ven por el vidrio (capacidad " + cap + ", nivel " + level + ")");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator Matapi_RodsFollowTheLiquidWhileFilling()
        {
            // El Matapi con 1 dosis recibe 2 (no sella): en cada fotograma, ninguna varilla de dentro baja de la cara superior
            // del líquido que se ve (el relleno a medias incluido), y las de la pared siguen enteras.
            yield return null;
            BoardState start = Board(
                new[] { new[] { 6 }, new[] { 6, 6 }, new[] { 1, 2, 1, 2 }, new[] { 6, 1, 1 }, new[] { 2, 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Matapi), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            MechanicTestKit.Rig rig = _rig;
            CuiaView view = rig.Views[0];
            var overlay = (MatapiOverlay)view.Overlay!;
            rig.Tick(PlayModeHelpers.FrameMs);
            AssertRodsAboveLiquid(overlay, view, "antes del vertido");
            Assert.That(ShownInsideRods(overlay), Is.EqualTo(MatapiOverlay.RodCount - 2), "con 1 dosis, las de dentro se ven por el vidrio vacío");

            rig.Pour(start, 1, 0, out _);
            float maxLevel = 0f;
            int t = 0;
            for (; !rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(t, Is.LessThan(4000), "el vertido termina");
                rig.Tick(PlayModeHelpers.FrameMs);
                maxLevel = Mathf.Max(maxLevel, view.LiquidLevel);
                AssertRodsAboveLiquid(overlay, view, "a los " + t + " ms (nivel " + view.LiquidLevel.ToString("0.00") + ")");
            }

            Assert.That(maxLevel, Is.EqualTo(3f).Within(1e-3f), "el Matapi llega a 3 dosis");
            Assert.That(ShownInsideRods(overlay), Is.EqualTo(MatapiOverlay.RodCount - 2), "con 3 dosis, aún se ven por la dosis vacía");
            Assert.That(overlay.ShownRodSpanU(2, out float bottom, out _), Is.True);
            Assert.That(bottom, Is.GreaterThan(CuiaGeometry.CavityBottomU + 3f), "por encima de la tercera dosis");
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

        // Sellado de un Matapi, Liana con fruta y Liana de cuenta en el banco, y la Liana con fruta una fila más abajo (como
        // LluviaPiranaTests.Lluvia_EndRemovesCloud(true)), cuyo cometa va por la ruta de 2 Bézier del pasillo entre columnas;
        // con measure, sin asignaciones (GC.Alloc del Recorder, por la restricción AllocatingGCMemory) en los 60 primeros
        // fotogramas desde el toque y, después, hasta la vista quieta (el final del Release: la caída, SnapStep y el Show que
        // oculta la Liana), sea cual sea la duración.
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
                Board(
                    new[] { new[] { 3, 3, 3 }, new[] { 3 }, new[] { 1, 2, 1, 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                    new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, key: 3), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None }),
            };

            MechanicStepKind[] kinds = { MechanicStepKind.Seal, MechanicStepKind.Release, MechanicStepKind.Release, MechanicStepKind.Release };
            int[] owners = { 0, 2, 2, 2 };
            MechanicType[] types = { MechanicType.Matapi, MechanicType.Liana, MechanicType.Liana, MechanicType.Liana };
            bool[] lianaBelow = { false, false, false, true };
            for (int c = 0; c < starts.Length; c++)
            {
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, starts[c]);
                if (lianaBelow[c])
                {
                    // Ranuras contiguas, como en la pantalla (los pasillos entre columnas son sus juntas), y la Liana (cuia 2)
                    // una fila más abajo en su columna: elevación 16 + separación 24 = 40 dp de su pie a la boca de la fila de
                    // arriba (BoardLayout), como la Lluvia de Lluvia_EndRemovesCloud(true).
                    for (int i = 0; i < _rig.Views.Length; i++)
                    {
                        CuiaView v = _rig.Views[i];
                        float footY = i == 2 ? -600f - 40f - (v.HeightU * v.Unit) : -600f;
                        v.SetGeometry(new Vector2(36f + (i * 64f), footY), 40f, 64f);
                    }

                    _rig.Animator.BoardLaidOut();
                }

                _rig.Pour(starts[c], 1, 0, out _);
                MechanicTestKit.Rig rig = _rig;
                if (measure)
                {
                    Assert.That(() => { rig.Run(60 * PlayModeHelpers.FrameMs); }, Is.Not.AllocatingGCMemory(), "cadena " + c + ": 60 fotogramas desde el toque");
                    Assert.That(() => { rig.RunUntilIdle(4000); }, Is.Not.AllocatingGCMemory(), "cadena " + c + ": hasta la vista quieta");
                    Assert.That(rig.Animator.IsIdle, Is.True, "cadena " + c + ": la medida cubre hasta la vista quieta");
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

                if (lianaBelow[c])
                {
                    Assert.That(r.OtherIndex, Is.EqualTo(0), "cadena " + c + ": la suelta la cuia sellada, una fila más arriba");
                    var overlay = (LianaOverlay)rig.Views[2].Overlay!;
                    Assert.That(overlay.BeamRouteSegments, Is.EqualTo(2), "cadena " + c + ": el cometa medido va por el pasillo, en 2 Bézier");
                }
            }
        }

        // Borde de abajo de la fruta llave key (píxeles del atlas procedural, el que usa el juego mientras no haya PNG) frente a
        // la cara superior de una cuia llena: en cada columna sobre la cavidad, al menos 2 dp de aire. La celda de la fruta es
        // LianaOverlay.FruitCellRectU (u desde el eje y desde el techo de la cavidad) y el sprite la llena.
        private static void AssertFruitClearsFullCuia(int key, float u)
        {
            Assert.That(key, Is.GreaterThan(0), "Liana con fruta");
            Assert.That(Resources.Load<Texture2D>(ProceduralArt.FruitFlightAtlasResource), Is.Null, "el juego usa el atlas procedural");
            Color32[] atlas = ProceduralArt.FruitFlightPixels();
            RectInt cellPx = ProceduralArt.FruitCell(key);
            Rect cell = LianaOverlay.FruitCellRectU;
            float px = cell.width / cellPx.width;
            float clearU = 2f / u;
            int columns = 0;
            float minGapU = float.MaxValue;
            for (int cx = 0; cx < cellPx.width; cx++)
            {
                int lowest = -1;
                for (int cy = 0; cy < cellPx.height && lowest < 0; cy++)
                {
                    if (atlas[((cellPx.y + cy) * ProceduralArt.FruitAtlasWidth) + cellPx.x + cx].a > 26)
                    {
                        lowest = cy;
                    }
                }

                float x = cell.xMin + ((cx + 0.5f) * px);
                if (lowest < 0 || Mathf.Abs(x) >= CuiaGeometry.CavityHalfWidthU)
                {
                    continue;
                }

                columns++;
                float bottom = cell.yMin + (lowest * px);
                minGapU = Mathf.Min(minGapU, bottom - CuiaGeometry.FaceSagU(x));
            }

            Assert.That(columns, Is.GreaterThan(0), "la fruta cuelga sobre el hombro, encima de la cavidad");
            Assert.That(minGapU, Is.GreaterThanOrEqualTo(clearU), "la fruta no tapa la cara superior de la cuia llena: " + (minGapU * u).ToString("0.0") + " dp de aire (mínimo 2)");
            TestContext.WriteLine("Fruta llave " + key + ": " + (minGapU * u).ToString("0.0") + " dp sobre la cara superior de la cuia llena.");
        }

        // Las varillas del Matapi a la vista: las de la pared, de la base a lo alto; las de dentro, sobre la cara superior del
        // líquido que pinta la vista ahora (su canto de atrás a la altura de la varilla).
        private static void AssertRodsAboveLiquid(MatapiOverlay overlay, CuiaView view, string when)
        {
            float level = view.LiquidLevel;
            for (int i = 0; i < MatapiOverlay.RodCount; i++)
            {
                bool shown = overlay.ShownRodSpanU(i, out float bottom, out float top);
                if (MatapiOverlay.RodOnWall(i))
                {
                    Assert.That(shown, Is.True, when + ": varilla " + i + " de la pared");
                    Assert.That(bottom, Is.EqualTo(CuiaGeometry.FootTopU).Within(1e-3f), when + ": varilla " + i + " desde la base");
                    continue;
                }

                if (!shown || level <= 0.001f)
                {
                    continue;
                }

                float inner = Mathf.Abs(MatapiOverlay.RodXU(i)) - (0.5f * MatapiOverlay.RodWidthU(i));
                float face = CuiaGeometry.CavityBottomU + level + CuiaGeometry.FaceSagU(inner);
                Assert.That(bottom, Is.GreaterThanOrEqualTo(face), when + ": varilla " + i + " por delante del líquido");
            }
        }

        private static int ShownInsideRods(MatapiOverlay overlay)
        {
            int n = 0;
            for (int i = 0; i < MatapiOverlay.RodCount; i++)
            {
                if (!MatapiOverlay.RodOnWall(i) && overlay.ShownRodSpanU(i, out _, out _))
                {
                    n++;
                }
            }

            return n;
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

        // Entre filas, la ruta del cometa no pasa por delante del cuerpo de ninguna cuia: va por un pasillo entre columnas (dos
        // tramos), y el punto donde cambia de tramo cae en un pasillo (entre las ranuras de dos cuias vecinas de una fila).
        private static void AssertAisleRoute(BoardAnimator animator, LianaOverlay overlay, int sealedCuia, int liana, float u, string where)
        {
            Assert.That(overlay.BeamRouteSegments, Is.EqualTo(2), where + ": por un pasillo entre columnas");
            Assert.That(overlay.BeamRouteCoverDp, Is.LessThanOrEqualTo(1f), where + ": sin pasar por delante del cuerpo de ninguna cuia (riesgo de T12; direccion-visual-v2 §7)");
            var board = (ICuiaBoard)animator;
            Vector2 joint = overlay.RoutePoint(1, 0f);
            bool aisle = false;
            for (int i = 0; i < board.Count && !aisle; i++)
            {
                for (int k = 0; k < board.Count && !aisle; k++)
                {
                    aisle = i != k && Mathf.Abs(board.Foot(i).y - board.Foot(k).y) < 0.5f * u
                        && Mathf.Abs(board.SlotRect(i).xMax - joint.x) < 1f && Mathf.Abs(board.SlotRect(k).xMin - joint.x) < 1f;
                }
            }

            Assert.That(aisle, Is.True, where + ": el pasillo está entre dos ranuras vecinas (x = " + joint.x.ToString("0.0") + ")");
            TestContext.WriteLine(where + ": cometa de la cuia " + sealedCuia + " a la Liana " + liana + " por el pasillo x = " + joint.x.ToString("0.0")
                + "; " + overlay.BeamRouteCoverDp.ToString("0.0") + " dp por delante de cuerpos.");
        }

        // Largo de lo que se ve del cometa (los núcleos a la vista), en dp.
        private static float VisibleTailLength(LianaOverlay overlay)
        {
            float length = 0f;
            BezierStripGraphic[] strips = overlay.BeamRoot.GetComponentsInChildren<BezierStripGraphic>(false);
            for (int i = 0; i < strips.Length; i++)
            {
                if (!strips[i].name.StartsWith("LianaBeamCore", System.StringComparison.Ordinal))
                {
                    continue;
                }

                Vector2 prev = strips[i].Evaluate(0f);
                for (int k = 1; k <= 32; k++)
                {
                    Vector2 q = strips[i].Evaluate(k / 32f);
                    length += Vector2.Distance(prev, q);
                    prev = q;
                }
            }

            return length;
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
