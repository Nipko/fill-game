#nullable enable
using System.Collections;
using System.Collections.Generic;
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
    /// Brote de dos hojas y Flor de vitória-régia en el cliente (tarea VA1b; 02 §9.1 «Variantes», §1.2 y R10 b y d; propuesta
    /// de D-007 b.3: «Brote con dos hojas cerradas; cada dosis abre una» y «dos llaves en lógica Y … con una sola, la flor está
    /// blanca y entreabierta; con las dos, rosa y abierta»; D-002; Q17 A) y sus tarjetas de estreno (343 y 403; 02 §10). En el
    /// banco de <see cref="MechanicTestKit"/>: el Brote de dos hojas se distingue del Brote (dos hojas grandes cerradas por delante
    /// en lugar de las hojitas de detrás), cada dosis de su fruta abre una hoja con un paso Open propio sin germinar, la segunda
    /// lo hace germinar, un tramo de 2 lo abre de una vez y la Marea (repintar el estado anterior) vuelve a cerrar la hoja; la
    /// Flor tapa la boca cerrada y blanca con sus dos frutas llave a la vista, se entreabre con la ✓ de la primera llave sellada,
    /// el paso Release la vuelve rosa y la abre al sellar la segunda, y repintar el estado anterior la vuelve a cerrar. El plan de
    /// pasos (<see cref="MechanicStepPlan"/>) los ordena en sus etapas y pintar no asigna memoria. En la escena, con paquetes de
    /// prueba del mundo 7 (<c>pack_schema</c> 3): las tarjetas del 343 y del 403 con su icono, su texto y sin dejar jugar sin
    /// verlas, y la Marea con la escena entera.
    /// </summary>
    public sealed class VariantsVisualTests
    {
        private const int Tapereba = 3; // índices de motor (glosario §9)
        private const int Aguaje = 4;
        private const int Urucum = 5;
        private const int Bacaba = 7;
        private const int Acai = 8;
        private const int Cacao = 13;

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

        // ---------------------------------------------------------------- Brote de dos hojas

        [UnityTest]
        public IEnumerator BroteTwoLeaves_IsDistinctFromBrote()
        {
            yield return null;

            // El Brote (taperebá) y el Brote de dos hojas (aguaje, con espacio 2) en el mismo banco.
            BoardState board = Board(
                new[] { new[] { Tapereba, Tapereba }, new[] { Aguaje, Aguaje }, new[] { Tapereba, Aguaje, Bacaba, Acai }, new[] { Acai, Bacaba, Tapereba, Aguaje }, new[] { Bacaba, Acai, Bacaba, Acai }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Brote), new MechanicSpec(MechanicType.Brote, n: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            Assert.That(board[1].MechanicN, Is.EqualTo(2), "n = 2 en la cuia (BoardBuilder)");
            _rig = new MechanicTestKit.Rig(Art, board);
            BroteOverlay brote = Brote(_rig.Views[0]);
            BroteOverlay two = Brote(_rig.Views[1]);

            // El Brote: las hojitas detrás del capullo, sin las hojas grandes.
            Assert.That(brote.TwoLeaves, Is.False);
            Assert.That(brote.ClosedLeaves, Is.EqualTo(0));
            Assert.That(brote.LeavesVisible, Is.True);
            Assert.That(brote.WrapsVisible, Is.False);

            // El Brote de dos hojas: las dos hojas grandes cerradas por delante, sin las hojitas; el capullo con el color de su cima.
            Assert.That(two.Visible && two.CorkVisible && two.SepalsVisible, Is.True);
            Assert.That(two.TwoLeaves, Is.True);
            Assert.That(two.ClosedLeaves, Is.EqualTo(2), "las dos cerradas (b.3)");
            Assert.That(two.WrapsVisible, Is.True);
            Assert.That(two.LeavesVisible, Is.False, "sin las hojitas del Brote: se distingue de la base");
            Assert.That(two.BudColor, Is.EqualTo(Palette.Tint(Aguaje)), "el capullo dice la fruta que lo abre");
            for (int side = -1; side <= 1; side += 2)
            {
                Assert.That(two.WrapOutDeg(side), Is.EqualTo(BroteOverlay.WrapClosedDeg).Within(0.05f), "cerrada sobre el capullo: " + side);
                Assert.That(two.WrapColor(side).r, Is.EqualTo(BroteOverlay.WrapClosedShade).Within(1e-3f), "plegada, en sombra: " + side);
            }

            // Cerradas, las dos hojas dejan el capullo a la vista entre ellas y por encima: sus puntas no llegan a la del capullo.
            float corkTop = two.CorkRectU.yMax;
            Assert.That(two.WrapTip(-1).y, Is.LessThan(corkTop - 0.05f));
            Assert.That(two.WrapTip(1).y, Is.LessThan(corkTop - 0.05f));
            Assert.That(two.WrapTip(1).x - two.WrapTip(-1).x, Is.GreaterThan(0.1f), "un hueco entre las puntas: se ve el capullo");
            Assert.That(two.VisibleTopU, Is.LessThanOrEqualTo(CuiaGeometry.HeightU(4) + BroteOverlay.TopAboveRimU + 1e-3f), "no pasa del tope del Brote");
            Assert.That(Shown(_rig.Views[1]), Is.Not.EqualTo(Shown(_rig.Views[0])));
            Assert.That(two.StepDurationMs(MechanicStepKind.Open), Is.EqualTo(BroteOverlay.LeafStepMs));
            AssertSurfaceColors(two, "Brote de dos hojas");
        }

        [UnityTest]
        public IEnumerator BroteTwoLeaves_EachDoseOpensALeaf_TheSecondGerminates_AndUndoClosesIt()
        {
            yield return null;
            BoardState start = TwoLeafBoard();
            _rig = new MechanicTestKit.Rig(Art, start);
            BroteOverlay two = Brote(_rig.Views[0]);
            Assert.That(two.ClosedLeaves, Is.EqualTo(2));

            // Una dosis de aguaje: el núcleo abre una hoja sin germinar (BroteLeafOpenedMask) y la cola hace un paso Open propio.
            BoardState one = _rig.Pour(start, 1, 0, out PourVisual first);
            Assert.That(first.Events.BroteLeafOpenedMask, Is.EqualTo(1));
            Assert.That(first.Events.BroteOpenedMask, Is.EqualTo(0));
            Assert.That(one[0].Mechanic, Is.EqualTo(MechanicType.Brote));
            Assert.That(one[0].MechanicN, Is.EqualTo(1), "le falta una hoja (02 R10 b)");

            var plan = new StepEntry[MechanicStepPlan.MaxSteps];
            int count = MechanicStepPlan.Build(in first, plan);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(plan[0].Kind, Is.EqualTo(MechanicStepKind.Open));
            Assert.That(plan[0].Stage, Is.EqualTo(StepStage.B));
            Assert.That(plan[0].Index, Is.EqualTo(0));
            Assert.That(StepStaging.Pre(in first, 0, StepStage.B).MechanicN, Is.EqualTo(2), "antes del paso, con las dos hojas cerradas");

            bool sawLeafStep = false;
            float maxOut = float.NegativeInfinity;
            int ms = 0;
            for (; ms < 6000 && !_rig.Animator.IsIdle; ms += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                if (two.IsLeafStep)
                {
                    sawLeafStep = true;
                    maxOut = Mathf.Max(maxOut, two.WrapOutDeg(-1));
                    Assert.That(two.CorkVisible, Is.True, "el capullo sigue cerrado mientras se abre la hoja");
                    Assert.That(two.WrapOutDeg(1), Is.EqualTo(BroteOverlay.WrapClosedDeg).Within(0.05f), "la otra hoja, cerrada");
                }
            }

            Assert.That(ms, Is.LessThan(6000));
            Assert.That(sawLeafStep, Is.True, "paso Open de la hoja (sin germinar)");
            Assert.That(maxOut, Is.GreaterThan(BroteOverlay.WrapOpenDeg), "se abre con un pequeño rebote");
            MechanicStepRecord rec = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.Open, 0);
            Assert.That(rec.Index, Is.EqualTo(0));
            Assert.That(two.Visible && two.CorkVisible, Is.True, "sigue siendo Brote");
            Assert.That(two.ClosedLeaves, Is.EqualTo(1));
            Assert.That(two.WrapOutDeg(-1), Is.EqualTo(BroteOverlay.WrapOpenDeg).Within(0.05f), "la izquierda, abierta");
            Assert.That(two.WrapColor(-1).r, Is.EqualTo(1f).Within(1e-3f), "abierta, a plena luz");
            Assert.That(two.WrapOutDeg(1), Is.EqualTo(BroteOverlay.WrapClosedDeg).Within(0.05f), "la derecha, cerrada");
            Assert.That(two.VisibleTopU, Is.LessThanOrEqualTo(CuiaGeometry.HeightU(4) + BroteOverlay.TopAboveRimU + 1e-3f));
            Assert.That(two.WrapTip(-1).x, Is.GreaterThan(-CuiaGeometry.BodyOuterHalfWidthU - 0.05f), "abierta, no se sale del ancho de la cuia");

            // Repintar el estado de antes (lo que hace la Marea) vuelve a cerrar la hoja.
            two.Show(start[0], false);
            Assert.That(two.ClosedLeaves, Is.EqualTo(2));
            Assert.That(two.WrapOutDeg(-1), Is.EqualTo(BroteOverlay.WrapClosedDeg).Within(0.05f), "Marea: la hoja se cierra (02 §1.2)");
            two.Show(one[0], false);

            // La segunda dosis lo hace germinar: el paso Open del Brote, con las hojas grandes abiertas y desvanecidas.
            BoardState sprouted = _rig.Pour(one, 2, 0, out PourVisual second);
            Assert.That(second.Events.BroteOpenedMask, Is.EqualTo(1));
            Assert.That(sprouted[0].Mechanic, Is.EqualTo(MechanicType.None), "germinó (02 R10 b)");
            bool sawGerminate = false;
            for (ms = 0; ms < 6000 && !_rig.Animator.IsIdle; ms += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                sawGerminate |= two.IsAnimating && !two.IsLeafStep;
            }

            Assert.That(sawGerminate, Is.True, "la germinación del Brote");
            Assert.That(two.Visible || two.CorkVisible || two.WrapsVisible, Is.False, "abierto: la cuia es normal");
        }

        [UnityTest]
        public IEnumerator BroteTwoLeaves_RunOfTwo_GerminatesAtOnce()
        {
            yield return null;

            // Un tramo de 2 de aguaje (D-005, run; 02 R9) abre las dos hojas de una vez: germina sin paso de hoja.
            BoardState start = Board(
                new[] { new[] { Aguaje, Aguaje }, new[] { Tapereba, Bacaba, Aguaje, Aguaje }, new[] { Tapereba, Acai, Bacaba, Tapereba }, new[] { Acai, Bacaba, Acai, Tapereba }, new[] { Bacaba, Acai }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Brote, n: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, start);
            BroteOverlay two = Brote(_rig.Views[0]);
            BoardState after = _rig.Pour(start, 1, 0, out PourVisual v);
            Assert.That(v.Events.Amount, Is.EqualTo(2));
            Assert.That(v.Events.BroteLeafOpenedMask, Is.EqualTo(0));
            Assert.That(after[0].Mechanic, Is.EqualTo(MechanicType.None));
            bool sawLeafStep = false;
            for (int ms = 0; ms < 6000 && !_rig.Animator.IsIdle; ms += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                sawLeafStep |= two.IsLeafStep;
            }

            Assert.That(sawLeafStep, Is.False, "sin paso de hoja: germina de una vez");
            Assert.That(MechanicTestKit.TryFindRecord(_rig.Animator, MechanicStepKind.Open, 0, out _), Is.True);
            Assert.That(two.Visible, Is.False);
        }

        // ---------------------------------------------------------------- Flor de vitória-régia

        [UnityTest]
        public IEnumerator Flor_ClosedWithBothKeys_HalfOpensWithOne_OpensPinkWithBoth()
        {
            yield return null;
            BoardState start = FlorBoard();
            _rig = new MechanicTestKit.Rig(Art, start);
            FlorOverlay flor = Flor(_rig.Views[FlorIndex]);

            // Cerrada: no es origen ni destino (02 §1.2), tapa la boca y enseña sus dos frutas.
            Assert.That(CuiaOverlays.Create(MechanicType.Flor), Is.InstanceOf<FlorOverlay>());
            Assert.That(PourRules.IsLegal(start, FlorIndex, 5), Is.False, "cerrada no es origen");
            Assert.That(PourRules.IsLegal(start, 4, FlorIndex), Is.False, "ni destino");
            Assert.That(flor.Visible && flor.PetalsVisible && flor.SepalsVisible && flor.FruitsVisible, Is.True);
            Assert.That(flor.ShownKey, Is.EqualTo(Cacao));
            Assert.That(flor.ShownKey2, Is.EqualTo(Urucum));
            Assert.That(flor.FruitSprite(1)!.name, Is.EqualTo("mech_hoja_atada_swatch_" + ProceduralArt.TintIds[Cacao]), "las dos frutas llave a la vista (b.3), como muestras de su dosis");
            Assert.That(flor.FruitSprite(2)!.name, Is.EqualTo("mech_hoja_atada_swatch_" + ProceduralArt.TintIds[Urucum]));
            Assert.That(flor.KeysMet, Is.EqualTo(0));
            Assert.That(flor.CheckVisible(1) || flor.CheckVisible(2), Is.False);
            Assert.That(flor.Openness, Is.EqualTo(0f), "cerrada");
            Assert.That(flor.Pinkness, Is.EqualTo(0f));
            Assert.That(Rgb(flor.PetalColor), Is.EqualTo(ProceduralArt.FlorWhite), "blanca (b.3)");
            Assert.That(flor.PetalTopU, Is.LessThanOrEqualTo(CuiaGeometry.HeightU(4) + FlorOverlay.TopAboveRimU + 1e-3f));
            Assert.That(FlorOverlay.TopAboveRimU, Is.LessThanOrEqualTo(BroteOverlay.TopAboveRimU), "no tapa la fila de arriba");
            Rect left = flor.FruitRectU(1);
            Rect right = flor.FruitRectU(2);
            Assert.That(left.center.x, Is.LessThan(-0.3f), "una fruta en cada hombro");
            Assert.That(right.center.x, Is.GreaterThan(0.3f));
            Assert.That(left.center.y - (0.5f * FlorOverlay.KeyDiscU), Is.GreaterThan(CuiaGeometry.CavityTopU(4) + 0.05f), "sobre el líquido de la cuia llena (Q17 A)");
            Assert.That(right.center.x + (0.5f * FlorOverlay.KeyDiscU), Is.LessThanOrEqualTo(CuiaGeometry.BodyOuterHalfWidthU), "el disco, dentro del ancho de la cuia");
            Assert.That(flor.StepDurationMs(MechanicStepKind.Release), Is.EqualTo(FlorOverlay.ReleaseMs));
            Assert.That(flor.StepDurationMs(MechanicStepKind.Open), Is.EqualTo(0f));
            AssertSurfaceColors(flor, "Flor");
            string closed = Shown(_rig.Views[FlorIndex]);

            // D-002: tocarla no la cambia (no es origen; el toque es un vertido ilegal).
            Assert.That(Shown(_rig.Views[FlorIndex]), Is.EqualTo(closed));

            // La primera llave (cacao) se sella en otra cuia: su ✓ y la flor entreabierta, blanca.
            BoardState half = _rig.Pour(start, 2, 0, out PourVisual sealCacao);
            Assert.That(half.IsSealed(0), Is.True);
            Assert.That(half[FlorIndex].Mechanic, Is.EqualTo(MechanicType.Flor), "con una sola llave sigue cerrada (02 R10 d)");
            Assert.That(MechanicStepPlan.ReleaseStepMask(in sealCacao), Is.EqualTo(0));
            Assert.That(_rig.RunUntilIdle(6000), Is.LessThan(6000));
            _rig.Run((int)FlorOverlay.HalfOpenMs + 64);
            Assert.That(flor.KeysMet, Is.EqualTo(1));
            Assert.That(flor.KeyMet(1), Is.True);
            Assert.That(flor.CheckVisible(1), Is.True, "✓ en el cacao");
            Assert.That(flor.CheckVisible(2), Is.False);
            Assert.That(flor.Openness, Is.EqualTo(FlorOverlay.HalfOpen).Within(1e-3f), "entreabierta (b.3)");
            Assert.That(Rgb(flor.PetalColor), Is.EqualTo(ProceduralArt.FlorWhite), "y blanca");
            Assert.That(flor.PetalOutDeg(0), Is.GreaterThan(20f), "los pétalos de fuera, abiertos a medias");
            Assert.That(MechanicTestKit.TryFindRecord(_rig.Animator, MechanicStepKind.Release, FlorIndex, out _), Is.False);

            // La segunda (urucum): el núcleo la suelta en (d) y el paso Release la vuelve rosa y la abre del todo.
            BoardState open = _rig.Pour(half, 2, 1, out PourVisual sealUrucum);
            Assert.That(open.IsSealed(1), Is.True);
            Assert.That(open[FlorIndex].Mechanic, Is.EqualTo(MechanicType.None), "se abre con las dos (lógica Y)");
            Assert.That(MechanicStepPlan.ReleaseStepMask(in sealUrucum), Is.EqualTo(1 << FlorIndex));
            var plan = new StepEntry[MechanicStepPlan.MaxSteps];
            int count = MechanicStepPlan.Build(in sealUrucum, plan);
            int release = -1;
            for (int e = 0; e < count; e++)
            {
                if (plan[e].Kind == MechanicStepKind.Release)
                {
                    release = e;
                }
            }

            Assert.That(release, Is.GreaterThanOrEqualTo(0), "un paso Release para la Flor");
            Assert.That(plan[release].Stage, Is.EqualTo(StepStage.D));
            Assert.That(plan[release].Index, Is.EqualTo(FlorIndex));
            Assert.That(plan[release].OtherIndex, Is.EqualTo(1), "la cuia que sella la segunda llave");
            Assert.That(plan[release].Tint, Is.EqualTo(Urucum));
            Assert.That(StepStaging.Pre(in sealUrucum, FlorIndex, StepStage.D).Mechanic, Is.EqualTo(MechanicType.Flor), "cerrada antes de su etapa");

            float maxPink = 0f;
            float maxOpen = 0f;
            bool bothChecks = false;
            int ms = 0;
            for (; ms < 6000 && !_rig.Animator.IsIdle; ms += PlayModeHelpers.FrameMs)
            {
                _rig.Tick(PlayModeHelpers.FrameMs);
                if (flor.IsAnimating)
                {
                    maxPink = Mathf.Max(maxPink, flor.Pinkness);
                    maxOpen = Mathf.Max(maxOpen, flor.Openness);
                    bothChecks |= flor.CheckVisible(1) && flor.CheckVisible(2);
                }
            }

            Assert.That(ms, Is.LessThan(6000));
            Assert.That(MechanicTestKit.Record(_rig.Animator, MechanicStepKind.Release, FlorIndex).Index, Is.EqualTo(FlorIndex));
            Assert.That(maxPink, Is.EqualTo(1f).Within(1e-3f), "rosa (b.3)");
            Assert.That(maxOpen, Is.EqualTo(1f).Within(1e-3f), "y abierta del todo");
            Assert.That(bothChecks, Is.True, "con la ✓ en las dos frutas");
            Assert.That(flor.Visible || flor.PetalsVisible || flor.SepalsVisible || flor.FruitsVisible, Is.False, "abierta: la cuia es normal");

            // Repintar el estado de antes (lo que hace la Marea) la vuelve a cerrar: primero entreabierta (el cacao sigue sellado)
            // y, deshecho el cacao, cerrada.
            for (int i = 0; i < half.Count; i++)
            {
                if (i != FlorIndex)
                {
                    _rig.Animator.ShowContent(i, half[i], half.IsSealed(i));
                }
            }

            _rig.Animator.ShowContent(FlorIndex, half[FlorIndex], false);
            _rig.Run(32);
            Assert.That(flor.Visible && flor.PetalsVisible && flor.FruitsVisible, Is.True, "Marea: la flor vuelve");
            Assert.That(flor.KeysMet, Is.EqualTo(1));
            Assert.That(flor.Openness, Is.EqualTo(FlorOverlay.HalfOpen).Within(1e-3f));
            Assert.That(flor.Pinkness, Is.EqualTo(0f), "blanca otra vez");
            for (int i = 0; i < start.Count; i++)
            {
                _rig.Animator.ShowContent(i, start[i], start.IsSealed(i));
            }

            _rig.Run((int)FlorOverlay.HalfOpenMs + 64);
            Assert.That(flor.KeysMet, Is.EqualTo(0));
            Assert.That(flor.Openness, Is.EqualTo(0f).Within(1e-3f), "cerrada (02 §1.2: Marea restaura)");
            Assert.That(flor.CheckVisible(1), Is.False);
            Assert.That(Shown(_rig.Views[FlorIndex]), Is.EqualTo(closed), "igual que al empezar");
        }

        [UnityTest]
        public IEnumerator VariantOverlays_ShowAndTick_DoNotAllocate()
        {
            yield return null;
            BoardState board = FlorBoard();
            _rig = new MechanicTestKit.Rig(Art, board);
            FlorOverlay flor = Flor(_rig.Views[FlorIndex]);
            Cuia flowerCuia = board[FlorIndex];
            Cuia plain = board[5];
            Cuia two = Cuia.Create(4, new[] { Aguaje, Aguaje }).WithMechanic(MechanicType.Brote, n: 2);
            Cuia oneLeft = Cuia.Create(4, new[] { Aguaje, Aguaje, Aguaje }).WithMechanic(MechanicType.Brote, n: 1);
            CuiaView view = _rig.Views[4];
            view.ShowContent(two, false); // crea el overlay del Brote en la cuia 4 (vacía en el modelo: aquí solo se pinta)
            BroteOverlay brote = Brote(view);
            flor.Show(flowerCuia, false);
            flor.Tick(16f);
            brote.Show(oneLeft, false);
            brote.Show(two, false);
            Assert.That(() =>
            {
                flor.Show(flowerCuia, false);
                flor.Tick(16f);
                flor.Layout(24f, CuiaGeometry.HeightU(4));
                flor.Show(plain, false);
                flor.Show(flowerCuia, false);
                brote.Show(oneLeft, false);
                brote.Show(two, false);
                brote.Tick(16f);
                brote.Layout(24f, CuiaGeometry.HeightU(4));
            }, Is.Not.AllocatingGCMemory(), "Show, Tick y Layout sin asignaciones (08 §5.4)");
            Assert.That(flor.Visible && brote.WrapsVisible, Is.True);
        }

        // ---------------------------------------------------------------- Escena: tarjetas de estreno y Marea

        [UnityTest]
        public IEnumerator Level343_DebutCard_BroteTwoLeaves_CannotBeSkipped()
        {
            yield return LoadWithCards(BroteDebutPack(), 343);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            LevelCardView card = view.LevelCard;

            Assert.That(SightingRule.IsDebut(343), Is.True, "el 343 es estreno con tarjeta (VA1b)");
            Assert.That(SightingRule.IsBusy(343), Is.True, "ni la tarjeta del miquito");
            Assert.That(flow.Level.Skippable, Is.False, "el estreno no admite el Salto del Boto (skippable = false, 04 §2.6)");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "sale la tarjeta de estreno (02 §10)");
            Assert.That(card.Visible, Is.True);
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Debut));
            Assert.That(card.Card.IntroMechanic, Is.EqualTo(LevelCardInfo.BroteTwoLeavesIntro));
            Assert.That(card.TutorialText, Is.EqualTo(UiText.TutorialBroteN2));
            Assert.That(card.TutorialText, Does.Contain("dos hojas").And.Contain("abre una").And.Contain("germina"));
            Assert.That(CardSetup.LineCount(card.Tutorial), Is.InRange(1, LevelCardView.MaxTutorialLines), "≤ 2 líneas a 360 dp (02 §12)");
            Assert.That(card.Tutorial.fontSize, Is.GreaterThanOrEqualTo(LevelCardView.MinTutorialSp));
            Assert.That(card.Tutorial.textInfo.characterCount, Is.EqualTo(UiText.TutorialBroteN2.Length), "sin recortes");
            Assert.That(card.HeroSprite, Is.Not.Null, "con su icono grande");
            Assert.That(card.HeroSprite!.name, Is.EqualTo("card_hero_brote_n2"));
            Assert.That(card.HeroSprite, Is.Not.SameAs(view.CardArt.CardHeroIcon("brote")), "no el del Brote");
            Assert.That(ProceduralArt.BroteTwoLeavesIconKey, Is.EqualTo(LevelCardInfo.BroteTwoLeavesIntro));
            Assert.That(card.MedallionSprite, Is.SameAs(view.CardArt.CardMedallion));
            Assert.That(view.LastCardProbe, Does.Contain("\"intro\":\"brote_n2\"").And.Contain("\"kind\":\"debut\""));
            CardSetup.AssertInsidePanel(view);
            AssertCardIsNotSkipped(boot, MechanicTestKit.IndexOf(flow, MechanicType.Brote));
        }

        [UnityTest]
        public IEnumerator Level403_DebutCard_Flor_CannotBeSkipped()
        {
            yield return LoadWithCards(FlorDebutPack(), 403);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            LevelCardView card = view.LevelCard;

            Assert.That(SightingRule.IsDebut(403), Is.True, "el 403 es estreno con tarjeta (VA1b)");
            Assert.That(SightingRule.IsBusy(403), Is.True);
            Assert.That(flow.Level.Skippable, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(card.Card!.Kind, Is.EqualTo(LevelCardKind.Debut));
            Assert.That(card.Card.IntroMechanic, Is.EqualTo(LevelCardInfo.FlorIntro));
            Assert.That(card.TutorialText, Is.EqualTo(UiText.TutorialFlor));
            Assert.That(card.TutorialText, Does.Contain("dos frutas").And.Contain("entreabre"));
            Assert.That(CardSetup.LineCount(card.Tutorial), Is.InRange(1, LevelCardView.MaxTutorialLines));
            Assert.That(card.Tutorial.fontSize, Is.GreaterThanOrEqualTo(LevelCardView.MinTutorialSp));
            Assert.That(card.Tutorial.textInfo.characterCount, Is.EqualTo(UiText.TutorialFlor.Length), "sin recortes");
            Assert.That(card.HeroSprite, Is.SameAs(view.CardArt.CardHeroIcon(LevelCardInfo.FlorIntro)));
            Assert.That(card.HeroSprite!.name, Is.EqualTo("card_hero_flor"));
            Assert.That(card.HeroSprite, Is.Not.SameAs(view.CardArt.CardHeroIcon("liana")), "no el de la Liana con fruta, su base");
            Assert.That(view.CardArt.MechanicIcon(MechanicType.Flor), Is.Not.Null, "y su icono de 32 dp para la fila de iconos");
            Assert.That(view.LastCardProbe, Does.Contain("\"intro\":\"flor\""));
            CardSetup.AssertInsidePanel(view);
            AssertCardIsNotSkipped(boot, MechanicTestKit.IndexOf(flow, MechanicType.Flor));
        }

        [UnityTest]
        public IEnumerator Level403_Flor_InTheScene_OpensWithBothKeys_AndUndoClosesIt()
        {
            LaunchOptions.LevelPackJsonOverride = FlorComparePack();
            yield return MechanicTestKit.LoadPlaying(403);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(MechanicTestKit.IndexOf(flow, MechanicType.Flor), Is.EqualTo(FlorIndex));
            CuiaView view = boot.View!.Views[FlorIndex];
            FlorOverlay flor = Flor(view);
            Assert.That(flor.Visible && flor.FruitsVisible && flor.KeysMet == 0, Is.True);

            // D-002 y 02 §1.2: tocar la flor cerrada no la selecciona ni la cambia.
            string closed = Shown(view);
            boot.TapCuia(FlorIndex);
            PlayModeHelpers.Step(boot, 240);
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection), "cerrada no es origen");
            Assert.That(Shown(view), Is.EqualTo(closed));

            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepToIdle(boot, 6000);
            PlayModeHelpers.Step(boot, (int)FlorOverlay.HalfOpenMs + 64);
            Assert.That(flor.KeysMet, Is.EqualTo(1), "el cacao, sellado");
            Assert.That(flor.Openness, Is.EqualTo(FlorOverlay.HalfOpen).Within(1e-3f));

            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.Release, FlorIndex);
            Assert.That(flow.Session.Current[FlorIndex].Mechanic, Is.EqualTo(MechanicType.None), "se abrió");
            Assert.That(flor.Visible, Is.False);
            MechanicTestKit.AssertViewMatchesModel(boot);

            // Marea: el urucum vuelve y la flor se cierra a medias; otra Marea, del todo.
            Assert.That(boot.PressUndo(), Is.True);
            MechanicTestKit.StepToIdle(boot, 6000);
            PlayModeHelpers.Step(boot, (int)FlorOverlay.HalfOpenMs + 64);
            Assert.That(flow.Session.Current[FlorIndex].Mechanic, Is.EqualTo(MechanicType.Flor));
            Assert.That(flor.Visible && flor.PetalsVisible, Is.True, "Marea la vuelve a cerrar (02 §1.2)");
            Assert.That(flor.KeysMet, Is.EqualTo(1));
            Assert.That(flor.Openness, Is.EqualTo(FlorOverlay.HalfOpen).Within(1e-3f));
            Assert.That(boot.PressUndo(), Is.True);
            MechanicTestKit.StepToIdle(boot, 6000);
            PlayModeHelpers.Step(boot, (int)FlorOverlay.HalfOpenMs + 64);
            Assert.That(flor.KeysMet, Is.EqualTo(0));
            Assert.That(flor.Openness, Is.EqualTo(0f).Within(1e-3f));
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Level343_BroteTwoLeaves_InTheScene_LeafOpens_AndUndoClosesIt()
        {
            LaunchOptions.LevelPackJsonOverride = BroteComparePack();
            yield return MechanicTestKit.LoadPlaying(343);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            BroteOverlay normal = Brote(boot.View!.Views[CompareBrote]);
            BroteOverlay two = Brote(boot.View.Views[CompareTwoLeaves]);
            Assert.That(normal.TwoLeaves, Is.False);
            Assert.That(two.TwoLeaves && two.ClosedLeaves == 2, Is.True);

            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(CompareTwoLeaves).Kind, Is.EqualTo(TapKind.Poured));
            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.Open, CompareTwoLeaves);
            Assert.That(flow.Session.Current[CompareTwoLeaves].MechanicN, Is.EqualTo(1));
            Assert.That(two.ClosedLeaves, Is.EqualTo(1));
            Assert.That(two.WrapOutDeg(-1), Is.EqualTo(BroteOverlay.WrapOpenDeg).Within(0.05f));
            MechanicTestKit.AssertViewMatchesModel(boot);

            Assert.That(boot.PressUndo(), Is.True);
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(flow.Session.Current[CompareTwoLeaves].MechanicN, Is.EqualTo(2));
            Assert.That(two.ClosedLeaves, Is.EqualTo(2), "Marea: las dos hojas cerradas otra vez");
            Assert.That(two.WrapOutDeg(-1), Is.EqualTo(BroteOverlay.WrapClosedDeg).Within(0.05f));
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        // ---------------------------------------------------------------- Tableros y paquetes de prueba

        /// <summary>Índice de la Flor en <see cref="FlorBoard"/> y <see cref="FlorComparePack"/>.</summary>
        internal const int FlorIndex = 3;

        /// <summary>Índice del Brote en <see cref="BroteComparePack"/>.</summary>
        internal const int CompareBrote = 1;

        /// <summary>Índice del Brote de dos hojas en <see cref="BroteComparePack"/>.</summary>
        internal const int CompareTwoLeaves = 2;

        /// <summary>
        /// La Flor de cacao y urucum en la cuia <see cref="FlorIndex"/> (con bacaba y açaí, sin sus frutas dentro), tres cacaos en
        /// la 0, tres urucums en la 1 y, en la 2, el cuarto de cada uno arriba (urucum bajo el cacao): 2 → 0 sella el cacao y 2 → 1
        /// el urucum. 7 cuias de 4 (4/3); capacidades uniformes (bloqueo C1-9).
        /// </summary>
        internal static int[][] FlorCuias()
        {
            return new[] { new[] { Cacao, Cacao, Cacao }, new[] { Urucum, Urucum, Urucum }, new[] { Bacaba, Acai, Urucum, Cacao }, new[] { Bacaba, Acai }, new[] { Bacaba, Acai, Bacaba, Acai }, new int[0], new int[0] };
        }

        private static BoardState FlorBoard()
        {
            var attrs = new MechanicSpec[7];
            for (int i = 0; i < attrs.Length; i++)
            {
                attrs[i] = MechanicSpec.None;
            }

            attrs[FlorIndex] = new MechanicSpec(MechanicType.Flor, key: Cacao, key2: Urucum);
            return Board(FlorCuias(), attrs);
        }

        // El Brote de dos hojas de aguaje (con dos) en la 0; un aguaje suelto arriba en la 1 y en la 2.
        private static BoardState TwoLeafBoard()
        {
            return Board(
                new[] { new[] { Aguaje, Aguaje }, new[] { Tapereba, Bacaba, Acai, Aguaje }, new[] { Bacaba, Acai, Tapereba, Aguaje }, new[] { Tapereba, Bacaba, Acai }, new[] { Acai, Tapereba, Bacaba }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Brote, n: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
        }

        /// <summary>
        /// Paquete de prueba (<c>pack_schema</c> 3) en el 343 del mundo 7: 7 cuias de 4 (4/3) con el Brote de taperebá (dos) en la
        /// <see cref="CompareBrote"/> y el Brote de dos hojas de aguaje (dos) en la <see cref="CompareTwoLeaves"/>, uno junto al
        /// otro en la fila de arriba (b.3 no los junta en un nivel; aquí solo para compararlos); la 0 y la 3 llevan un aguaje
        /// suelto arriba (0 → 2 abre una hoja y 3 → 2 lo hace germinar y lo sella).
        /// </summary>
        internal static string BroteComparePack()
        {
            return Pack(343, "test_m07_v1_n0343", new[] { "tapereba", "aguaje", "bacaba", "acai" },
                "[3, 7, 8, 4], [3, 3], [4, 4], [7, 8, 7, 4], [8, 3, 8, 7], [], []",
                "null, { \"type\": \"brote\" }, { \"type\": \"brote\", \"n\": 2 }, null, null, null, null", 7, null);
        }

        /// <summary>Paquete de prueba en el 403 del mundo 7 con el tablero de <see cref="FlorCuias"/> (la Flor de cacao y urucum).</summary>
        internal static string FlorComparePack()
        {
            return Pack(403, "test_m07_v1_n0403", new[] { "urucum", "bacaba", "acai", "cacao" },
                "[13, 13, 13], [5, 5, 5], [7, 8, 5, 13], [7, 8], [7, 8, 7, 8], [], []",
                "null, null, null, { \"type\": \"flor\", \"key\": 13, \"key2\": 5 }, null, null, null", 7, null);
        }

        /// <summary>
        /// El estreno del 343 con el tablero del Brote de dos hojas de la muestra de <c>pack_schema</c> 3
        /// (<c>Tests/EditMode/Fixtures/pack3_sample.json</c>, generado por level-generator): 12 cuias (4/4/4), el Brote de dos
        /// hojas de aguaje en la 1, <c>intro_mechanic</c> <c>brote_n2</c> y <c>skippable</c> = false.
        /// </summary>
        internal static string BroteDebutPack()
        {
            return Pack(343, "test_m07_v1_n0343", new[] { "guanabana", "tapereba", "aguaje", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "agua_negro" },
                "[3, 3, 3, 14], [4], [9, 9, 9, 7], [6, 6, 7, 6], [1, 1, 7, 4], [7, 4, 6], [8, 8, 1, 9], [14, 8, 10, 8], [], [], [10, 10, 10, 14], [3, 14, 1, 4]",
                "null, { \"type\": \"brote\", \"n\": 2 }, null, null, null, null, null, null, null, null, null, null", 12, LevelCardInfo.BroteTwoLeavesIntro);
        }

        /// <summary>
        /// El estreno del 403 con el tablero de la Flor de la muestra de <c>pack_schema</c> 3: 11 cuias (4/4/3), la Flor de cacao
        /// y urucum en la 10, <c>intro_mechanic</c> <c>flor</c> y <c>skippable</c> = false.
        /// </summary>
        internal static string FlorDebutPack()
        {
            return Pack(403, "test_m07_v1_n0403", new[] { "cupuacu", "tapereba", "aguaje", "urucum", "bacaba", "acai", "agua_tapajos", "araza", "cacao" },
                "[8, 8, 10, 4], [3, 5, 3, 7], [12, 4, 4, 4], [7, 12, 12, 2], [], [13, 13, 13, 10], [5, 5, 2, 7], [8, 8, 5, 10], [2, 2, 10, 13], [], [3, 3, 7, 12]",
                "null, null, null, null, null, null, null, null, null, null, { \"type\": \"flor\", \"key\": 13, \"key2\": 5 }", 11, LevelCardInfo.FlorIntro);
        }

        private static string Pack(int level, string packId, string[] palette, string cuias, string attrs, int count, string? intro)
        {
            var caps = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                caps.Append(i == 0 ? string.Empty : ", ").Append('4');
            }

            string id = "m07_v1_n0" + level;
            string debut = intro == null ? string.Empty : ", \"intro_mechanic\": \"" + intro + "\", \"skippable\": false";
            return "{ \"pack_schema\": 3, \"pack_id\": \"" + packId + "\", \"kind\": \"campaign\", \"world\": 7, \"world_id\": \"w07_madre_de_dios\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ { \"id\": \"" + id + "\", \"level\": " + level + ", \"world\": 7, \"lap\": 1, "
                + "\"water_base\": \"white\", \"palette\": [\"" + string.Join("\", \"", palette) + "\"], \"units_per_color\": 4, "
                + "\"caps\": [" + caps + "], \"cuias\": [" + cuias + "], \"attrs\": [" + attrs + "], "
                + "\"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": []" + debut + " } ] }";
        }

        // ---------------------------------------------------------------- Ayudas

        // Carga el paquete con las tarjetas encendidas (como CardSetup.Load con el catálogo real).
        private static IEnumerator LoadWithCards(string pack, int level)
        {
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGameWithPack(pack, level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300); // entrada de 220 ms
        }

        // La tarjeta no se cierra sola ni deja jugar sin verla: el primer toque en una cuia solo la cierra (no selecciona ni vierte).
        private static void AssertCardIsNotSkipped(GameBootstrap boot, int cuia)
        {
            LevelFlow flow = boot.Flow!;
            LevelCardView card = boot.View!.LevelCard;
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "no se cierra sola");
            Assert.That(card.Visible, Is.True);
            Assert.That(boot.TapCuia(cuia).Kind, Is.EqualTo(TapKind.Ignored), "el toque solo cierra la tarjeta");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection));
            PlayModeHelpers.Step(boot, 160);
            Assert.That(card.Visible, Is.False);
        }

        private static void AssertSurfaceColors(CuiaOverlay overlay, string what)
        {
            var colors = new List<Color>();
            overlay.CollectSurfaceColors(colors);
            Assert.That(colors.Count, Is.GreaterThan(0), what);
            for (int c = 0; c < colors.Count; c++)
            {
                for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
                {
                    Assert.That(ColorMath.DeltaE00(colors[c], Palette.Tint(tint)), Is.GreaterThanOrEqualTo(10f),
                        what + ", color " + c + " frente al tinte " + tint + " (direccion-visual-v2 §2.4, regla 1)");
                }
            }
        }

        private static Color Rgb(Color c)
        {
            return new Color(c.r, c.g, c.b, 1f);
        }

        private static BroteOverlay Brote(CuiaView view)
        {
            var overlay = view.OverlayOf(MechanicType.Brote) as BroteOverlay;
            Assert.That(overlay, Is.Not.Null, "overlay del Brote");
            return overlay!;
        }

        private static FlorOverlay Flor(CuiaView view)
        {
            var overlay = view.OverlayOf(MechanicType.Flor) as FlorOverlay;
            Assert.That(overlay, Is.Not.Null, "overlay de la Flor");
            return overlay!;
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
            Assert.That(LevelValidation.Validate(result.State!), Is.Null, "tablero válido");
            return result.State!;
        }

        // Lo que se ve de las piezas del overlay bajo el cuerpo: nombre, activación, posición, giro, escala y color de cada imagen.
        private static string Shown(CuiaView view)
        {
            var sb = new StringBuilder(512);
            Transform anchor = view.OverlayHost.BodyFront;
            for (int i = 0; i < anchor.childCount; i++)
            {
                var rt = (RectTransform)anchor.GetChild(i);
                sb.Append(rt.name).Append(rt.gameObject.activeSelf ? '+' : '-');
                if (rt.gameObject.activeSelf)
                {
                    sb.Append(rt.anchoredPosition.ToString("F2")).Append(rt.localEulerAngles.z.ToString("F1")).Append(rt.localScale.ToString("F2"));
                    var img = rt.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        sb.Append(img.color.ToString("F3"));
                    }
                }

                sb.Append('|');
            }

            return sb.ToString();
        }
    }
}
