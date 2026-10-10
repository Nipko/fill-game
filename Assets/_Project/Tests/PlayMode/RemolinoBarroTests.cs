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
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Remolino y Barro (T16; 02 §9.1 M4 y M5; propuesta de D-007 b.2 M4 y M5): la espiral y el canal de la pareja del 103
    /// sin tapar cuias ni repisas con 1, 2 y 3 filas y tras añadir una cuia; la recepción por el fondo de la pareja en
    /// <see cref="RemolinoOverlay.ReceiveMs"/> × factor, con burbujas y en el orden del modelo; Marea de un vertido por
    /// Remolino que salta [P]; el lavado del Barro del 123 en <see cref="MudMarks.WashMs"/> × factor con los colores del
    /// modelo; y los pasos sin asignaciones. Vertidos buscados con <see cref="MechanicTestKit"/>; el reloj lo lleva el test.
    /// </summary>
    public sealed class RemolinoBarroTests
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
        public IEnumerator Remolino103_PairAndChannelVisible()
        {
            yield return MechanicTestKit.LoadPlaying(103);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FindPair(boot.Flow!.Session.Current, out int a, out int b);
            RemolinoOverlay drawer = Whirl(view.Views[a]);
            RemolinoOverlay other = Whirl(view.Views[b]);
            Assert.That(drawer.Visible && other.Visible, Is.True, "las dos cuias de la pareja muestran su remolino");
            Assert.That(VisibleIn(drawer.AttachedHost!.SlotBack), Is.GreaterThanOrEqualTo(2), "remanso y espiral en la base");
            Assert.That(VisibleIn(other.AttachedHost!.SlotBack), Is.GreaterThanOrEqualTo(2), "remanso y espiral en la base");
            Assert.That(drawer.PartnerIndex, Is.EqualTo(b));
            Assert.That(other.PartnerIndex, Is.EqualTo(a));
            Assert.That(drawer.DrawsChannel && drawer.ChannelVisible, Is.True, "el canal lo dibuja la cuia de menor índice");
            Assert.That(other.DrawsChannel || other.ChannelVisible, Is.False, "y solo ella: una sola vez");
            AssertChannel(view.Animator, view.Views, drawer, a, b, "nivel 103");
            TestContext.WriteLine("Nivel 103: pareja " + a + "-" + b + ", " + Rows(view.Animator) + " filas, " + drawer.ChannelSegmentCount + " tramos de " + drawer.ChannelWidthDp.ToString("0.0") + " dp.");
        }

        [UnityTest]
        public IEnumerator Remolino103_ReceiveFromBottom()
        {
            yield return MechanicTestKit.LoadPlaying(103);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, bf, af) => ev.ThroughRemolino);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            GameScreen view = boot.View!;
            BoardAnimator animator = view.Animator;
            BoardState before = boot.Flow!.Session.Current;
            float tapAt = animator.Clock;
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out TapResult poured);
            int partner = poured.Pour.Receiver;
            int touched = pour.Target;
            Assert.That(partner, Is.Not.EqualTo(touched), "por el Remolino recibe la pareja");
            int tint = poured.Pour.Events.Color;
            int amount = visual.After[partner].Count - before[partner].Count;
            Assert.That(amount, Is.GreaterThan(0));
            CuiaView pv = view.Views[partner];
            RemolinoOverlay whirl = Whirl(pv);

            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Receive, partner, 4000);
            Assert.That(whirl.IsAnimating, Is.True, "la pareja anima su recepción");
            Assert.That(pv.LiquidLevel, Is.EqualTo((float)before[partner].Count).Within(1e-3f), "aún no ha entrado nada por el fondo");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.15f);
            Assert.That(whirl.PulseVisible, Is.True, "la espuma recorre el canal desde la cuia tocada");

            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.6f);
            Assert.That(pv.LiquidLevel, Is.GreaterThan(before[partner].Count + 0.05f).And.LessThan(visual.After[partner].Count - 0.05f), "la racha sube a mitad del paso");
            Assert.That(pv.DoseColor(0), Is.EqualTo(Palette.Tint(tint)), "entra por el fondo");
            for (int j = 0; j < before[partner].Count; j++)
            {
                Assert.That(pv.DoseColor(amount + j), Is.EqualTo(Palette.Tint(before[partner].ColorAt(j))), "la dosis " + j + " que ya había sube");
                Assert.That(pv.DoseFill(amount + j), Is.EqualTo(1f).Within(1e-4f));
            }

            Assert.That(animator.ShownOf(touched), Is.EqualTo(before[touched]), "la cuia tocada no cambia");
            Assert.That(view.Views[touched].LiquidLevel, Is.EqualTo((float)before[touched].Count).Within(1e-3f));
            Assert.That(Mathf.Abs(whirl.SpiralAngle), Is.GreaterThan(1f), "la espiral de la pareja gira");
            Assert.That(whirl.PulseVisible, Is.False, "la espuma ya llegó");

            MechanicTestKit.StepToIdle(boot, 6000);
            float elapsed = animator.Clock - tapAt;
            MechanicStepRecord receive = MechanicTestKit.Record(animator, MechanicStepKind.Receive, partner);
            Assert.That(receive.Owner, Is.EqualTo(MechanicType.Remolino));
            Assert.That(receive.UsedDefault, Is.False, "animación propia, no el salto por defecto");
            Assert.That(receive.Stage, Is.EqualTo(StepStage.A));
            Assert.That(receive.OtherIndex, Is.EqualTo(touched));
            Assert.That(receive.EndMs - receive.StartMs, Is.EqualTo(RemolinoOverlay.ReceiveMs * receive.SpeedFactor).Within(1f), "500 ms × SpeedFactor (b.2 M4)");
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            TestContext.WriteLine("Nivel 103: " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Cuia model = boot.Flow.Session.Current[partner];
            for (int k = 0; k < model.Count; k++)
            {
                Assert.That(pv.DoseColor(k), Is.EqualTo(Palette.Tint(model.ColorAt(k))), "dosis " + k + " en el orden del modelo");
            }

            Assert.That(whirl.IsAnimating || whirl.PulseVisible, Is.False);
            Assert.That(whirl.SpiralAngle, Is.EqualTo(0f).Within(0.01f), "la espiral vuelve a su reposo");

            // Banco: burbujas por el fondo de la pareja (como mucho 5) y, con Reducir movimiento, a × 0,5 y sin partículas.
            BoardState board = Board(
                new[] { new[] { 1, 1 }, new[] { 3, 3, 3 }, new[] { 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Remolino, partner: 2), MechanicSpec.None, new MechanicSpec(MechanicType.Remolino, partner: 0), MechanicSpec.None });
            for (int pass = 0; pass < 2; pass++)
            {
                bool reduce = pass == 1;
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, board);
                var fx = new RecordingFx { ReduceMotion = reduce };
                _rig.Animator.Fx = fx;
                _rig.Animator.ReduceMotion = reduce;
                BoardState after = _rig.Pour(board, 1, 0, out PourVisual v);
                Assert.That(v.Receiver, Is.EqualTo(2));
                _rig.RunUntilIdle(4000);
                MechanicStepRecord r = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.Receive, 2);
                Assert.That(r.EndMs - r.StartMs, Is.EqualTo(RemolinoOverlay.ReceiveMs * (reduce ? 0.5f : 1f)).Within(1f), reduce ? "Reducir movimiento: × 0,5" : "500 ms");
                Assert.That(_rig.Animator.ShownOf(2), Is.EqualTo(after[2]));
                Assert.That(_rig.Animator.ShownOf(0), Is.EqualTo(board[0]), "la cuia tocada no cambia");
                if (reduce)
                {
                    Assert.That(fx.BubbleCount, Is.EqualTo(0), "con Reducir movimiento no hay partículas");
                }
                else
                {
                    Assert.That(fx.BubbleCount, Is.GreaterThan(0).And.LessThanOrEqualTo(RemolinoOverlay.BubblesFirst + RemolinoOverlay.BubblesSecond), "burbujas, dentro del tope de 44");
                    CuiaView rv = _rig.Views[2];
                    Vector2 expected = rv.FootPosition + new Vector2(0f, (CuiaGeometry.CavityBottomU + 0.15f) * rv.Unit);
                    Assert.That(Vector2.Distance(fx.FirstBubbleAt, expected), Is.LessThan(0.5f), "por el fondo de la pareja");
                }
            }
        }

        [UnityTest]
        public IEnumerator Remolino103_UndoSnaps()
        {
            yield return MechanicTestKit.LoadPlaying(103);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, bf, af) => ev.ThroughRemolino);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            GameScreen view = boot.View!;

            // Marea de un vertido por Remolino ya terminado: sin traslado, el origen y la pareja saltan al modelo [P].
            TapResult poured = MechanicTestKit.Tap(boot, pour);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured));
            int partner = poured.Pour.Receiver;
            RemolinoOverlay whirl = Whirl(view.Views[partner]);
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            Assert.That(view.Animator.PendingCount, Is.EqualTo(0), "sin la animación inversa de 250 ms: salta");
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(whirl.Visible, Is.True);

            // Marea a mitad de la recepción: la cola salta (SnapStep) y la pareja vuelve al modelo en el acto.
            poured = MechanicTestKit.Tap(boot, pour);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), "el mismo vertido, otra vez");
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Receive, partner, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.3f);
            Assert.That(whirl.IsAnimating, Is.True);
            Assert.That(boot.PressUndo(), Is.True, "Marea a mitad del paso");
            Assert.That(whirl.IsAnimating, Is.False, "SnapStep");
            Assert.That(whirl.PulseVisible, Is.False);
            Assert.That(whirl.SpiralAngle, Is.EqualTo(0f).Within(0.01f));
            Assert.That(view.Animator.Steps.ActiveCount, Is.EqualTo(0));
            MechanicTestKit.AssertViewMatchesModel(boot);
            FindPair(boot.Flow!.Session.Current, out int a, out int b);
            Assert.That(Whirl(view.Views[a]).ChannelVisible, Is.True, "el canal sigue a la vista");
        }

        [UnityTest]
        public IEnumerator Remolino_ChannelAvoidsCuias_ThreeRows()
        {
            yield return null;

            // Tableros de 5, 8 y 16 cuias (1, 2 y 3 filas, 02 §5 y D-006) con la pareja en la misma fila, en filas vecinas y de
            // la fila 1 a la 3, maquetados como la pantalla (BoardLayout) en un lienzo de 360 dp.
            int[] counts = { 5, 8, 16 };
            int[] rows = { 1, 2, 3 };
            int[][] pairs =
            {
                new[] { 1, 3, 0, 4 },
                new[] { 0, 6, 2, 3, 3, 4 },
                new[] { 2, 13, 7, 9, 5, 6, 0, 15, 4, 10 },
            };

            for (int c = 0; c < counts.Length; c++)
            {
                for (int p = 0; p < pairs[c].Length; p += 2)
                {
                    int a = pairs[c][p];
                    int b = pairs[c][p + 1];
                    string what = counts[c] + " cuias, pareja " + a + "-" + b;
                    _rig?.Dispose();
                    _rig = new MechanicTestKit.Rig(Art, PairBoard(counts[c], a, b));
                    BoardLayoutResult l = BoardLayout.Compute(counts[c], CuiaGeometry.HeightU(4), 360f, 150f, 690f);
                    Assert.That(l.Rows, Is.EqualTo(rows[c]), what);
                    for (int i = 0; i < counts[c]; i++)
                    {
                        _rig.Views[i].SetGeometry(new Vector2(l.CenterX[i], -l.FootY[i]), l.CuiaWidth, l.SlotWidth);
                    }

                    _rig.Animator.BoardLaidOut();
                    RemolinoOverlay drawer = Whirl(_rig.Views[a]);
                    Assert.That(drawer.ChannelVisible, Is.True, what);
                    Assert.That(Whirl(_rig.Views[b]).ChannelVisible, Is.False, what + ": una sola vez");
                    AssertChannel(_rig.Animator, _rig.Views, drawer, a, b, what);
                    if (l.RowOf[a] != l.RowOf[b])
                    {
                        Assert.That(drawer.ChannelSegmentCount, Is.GreaterThanOrEqualTo(4), what + ": baja por un hueco de la fila de abajo");
                    }
                    else
                    {
                        Assert.That(drawer.ChannelSegmentCount, Is.EqualTo(3), what + ": U bajo la repisa");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator Remolino_ChannelRelayoutAfterAddCuia()
        {
            FunnelSetup.Hooks();
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return MechanicTestKit.LoadPlaying(103);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FindPair(boot.Flow!.Session.Current, out int a, out int b);
            RemolinoOverlay drawer = Whirl(view.Views[a]);
            int version = view.Animator.LayoutVersion;
            Vector2 footA = view.Animator.Foot(a);
            Vector2 footB = view.Animator.Foot(b);
            int cuias = view.Views.Length;
            Assert.That(boot.PressExtraCuia(), Is.True, "Cuia extra con semillas");
            Assert.That(view.Views.Length, Is.EqualTo(cuias + 1));
            AssertEnds(view.Animator, drawer, a, b, "al empezar el relayout");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(view.Animator.LayoutVersion, Is.GreaterThan(version + 1), "recolocado en cada fotograma del relayout");
            Assert.That(drawer.ChannelVisible, Is.True);
            AssertChannel(view.Animator, view.Views, drawer, a, b, "tras añadir una cuia");
            TestContext.WriteLine("Pie de " + a + ": " + footA + " → " + view.Animator.Foot(a) + "; de " + b + ": " + footB + " → " + view.Animator.Foot(b) + ".");
        }

        [UnityTest]
        public IEnumerator Barro123_WashStep()
        {
            yield return MechanicTestKit.LoadPlaying(123);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[] path = Trigger(boot, (ev, bf, af) => ev.BarroWashedMask != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour pour = path[path.Length - 1];
            GameScreen view = boot.View!;
            CuiaView target = view.Views[pour.Target];
            int mudLayer = boot.Flow!.Session.Current[pour.Target].Count - 1;
            Assert.That(target.Mud.LumpsVisible(mudLayer), Is.True, "el barro, en la cima");
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out TapResult poured);
            int tint = poured.Pour.Events.Color;

            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.MudWash, pour.Target, 4000);
            Assert.That(target.Mud.IsAnimating, Is.True, "IsAnimating mientras dura");
            Assert.That(target.Mud.WashLayer, Is.EqualTo(mudLayer), "la dosis donde estaba el barro");
            Assert.That(target.Mud.LumpsVisible(mudLayer), Is.True, "los grumos aún se ven al empezar a disolverse");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.MudWash, pour.Target, MudMarks.TurbidPeakAt);
            Assert.That(target.Mud.Turbidity, Is.GreaterThan(0.5f * MudMarks.PeakTurbidity), "el barro enturbia la fruta");
            Assert.That(target.Mud.CloudVisible, Is.True, "con su nube parda");
            Assert.That(target.Mud.WashColor, Is.Not.EqualTo(Palette.Tint(tint)));
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.MudWash, pour.Target, 0.8f);
            Assert.That(target.Mud.WashLumpAlpha, Is.EqualTo(0f).Within(1e-4f), "los grumos ya se disolvieron");
            Assert.That(target.Mud.Turbidity, Is.LessThan(0.5f * MudMarks.PeakTurbidity), "y el agua se aclara");

            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicStepRecord wash = MechanicTestKit.Record(view.Animator, MechanicStepKind.MudWash, pour.Target);
            Assert.That(wash.UsedDefault, Is.False);
            Assert.That(wash.EndMs - wash.StartMs, Is.EqualTo(MudMarks.WashMs * wash.SpeedFactor).Within(1f), "400 ms × SpeedFactor (b.2 M5)");
            MechanicTestKit.AssertLogFollowsPlan(view.Animator, in visual);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Cuia model = boot.Flow.Session.Current[pour.Target];
            for (int k = 0; k < model.Count; k++)
            {
                Color expected = model.ColorAt(k) == Cuia.MudColor ? Palette.Mud : Palette.Tint(model.ColorAt(k));
                Assert.That(target.DoseColor(k), Is.EqualTo(expected), "dosis " + k + " con el color del modelo");
            }

            Assert.That(target.Mud.LumpMask, Is.EqualTo(0), "el barro se lavó");
            Assert.That(target.Mud.IsAnimating || target.Mud.CloudVisible || target.Mud.LumpsVisible(mudLayer), Is.False);
            Assert.That(target.Mud.WashLayer, Is.EqualTo(-1));

            // Banco: el chapoteo sale de la capa de efectos de la cuia; con Reducir movimiento, × 0,5 y sin partículas.
            BoardState board = Board(new[] { new[] { 3, 3 }, new[] { 2, Cuia.MudColor }, new int[0] }, null);
            for (int pass = 0; pass < 2; pass++)
            {
                bool reduce = pass == 1;
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, board);
                _rig.Animator.Fx = new RecordingFx { ReduceMotion = reduce }; // el chorro salpica aparte: aquí solo cuenta el barro
                _rig.Animator.ReduceMotion = reduce;
                _rig.Fx.ReduceMotion = reduce;
                BoardState after = _rig.Pour(board, 0, 1, out _);
                RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.MudWash, 1), 3000);
                Assert.That(_rig.Views[1].Mud.IsAnimating, Is.True);
                if (reduce)
                {
                    Assert.That(_rig.Fx.ActiveCount, Is.EqualTo(0), "con Reducir movimiento no hay partículas");
                }
                else
                {
                    Assert.That(_rig.Fx.ActiveCount, Is.GreaterThan(0), "chapoteo pardo al empezar el lavado");
                }
                _rig.RunUntilIdle(3000);
                MechanicStepRecord r = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.MudWash, 1);
                Assert.That(r.EndMs - r.StartMs, Is.EqualTo(MudMarks.WashMs * (reduce ? 0.5f : 1f)).Within(1f));
                Assert.That(_rig.Animator.ShownOf(1), Is.EqualTo(after[1]));
                Assert.That(_rig.Views[1].DoseColor(1), Is.EqualTo(Palette.Tint(3)), "la fruta ocupa el lugar del barro");
                Assert.That(_rig.Views[1].Mud.LumpMask, Is.EqualTo(0));
            }
        }

        [UnityTest]
        public IEnumerator NoAllocDuringSteps()
        {
            yield return null;

            // Calentamiento: la misma secuencia una vez (JIT, sprites perezosos), y después se mide en un banco nuevo.
            for (int pass = 0; pass < 2; pass++)
            {
                RunSteps(pass == 1);
            }
        }

        // Un vertido por el Remolino (recepción por el fondo) y otro sobre el Barro (lavado); con measure, 60 fotogramas sin
        // asignaciones (Recorder GC.Alloc) desde cada toque y otros 60 hasta el final.
        private void RunSteps(bool measure)
        {
            BoardState board = Board(
                new[] { new[] { 1, 1 }, new[] { 2, 2, 2 }, new[] { 3, Cuia.MudColor }, new[] { 4 }, new[] { 3, 3 }, new int[0] },
                new[]
                {
                    new MechanicSpec(MechanicType.Remolino, partner: 3), MechanicSpec.None, MechanicSpec.None,
                    new MechanicSpec(MechanicType.Remolino, partner: 0), MechanicSpec.None, MechanicSpec.None,
                });
            _rig?.Dispose();
            _rig = new MechanicTestKit.Rig(Art, board);
            MechanicTestKit.Rig rig = _rig;
            BoardState s = rig.Pour(board, 1, 0, out PourVisual remolino);
            Assert.That(remolino.Events.ThroughRemolino, Is.True);
            if (measure)
            {
                Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), "Remolino: 60 fotogramas desde el toque");
                Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), "Remolino: 60 fotogramas más");
            }

            rig.RunUntilIdle(4000);
            rig.Pour(s, 4, 2, out PourVisual mud);
            Assert.That(mud.Events.BarroWashedMask, Is.EqualTo(1 << 2));
            if (measure)
            {
                Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), "Barro: 60 fotogramas desde el toque");
                Assert.That(() => rig.Run(60 * PlayModeHelpers.FrameMs), Is.Not.AllocatingGCMemory(), "Barro: 60 fotogramas más");
            }

            rig.RunUntilIdle(4000);
            Assert.That(rig.Animator.IsIdle, Is.True);
            Assert.That(MechanicTestKit.TryFindRecord(rig.Animator, MechanicStepKind.Receive, 3, out _), Is.True, "hubo recepción");
            Assert.That(MechanicTestKit.TryFindRecord(rig.Animator, MechanicStepKind.MudWash, 2, out _), Is.True, "hubo lavado");
        }

        // El canal de la pareja a-b: une los dos pies con tramos horizontales y verticales, no se mete en ninguna cuia (ni
        // elevada 16 dp) salvo el extremo redondo bajo el pie de la pareja, sus tramos horizontales no tocan la cara de ninguna
        // repisa y no se sale de las ranuras del tablero.
        private static void AssertChannel(BoardAnimator board, CuiaView[] views, RemolinoOverlay drawer, int a, int b, string what)
        {
            AssertEnds(board, drawer, a, b, what);
            float w = drawer.ChannelWidthDp;
            Assert.That(w, Is.InRange(RemolinoOverlay.ChannelMinDp, RemolinoOverlay.ChannelMaxDp), what);
            for (int i = 1; i < drawer.ChannelPointCount; i++)
            {
                Vector2 p = drawer.ChannelPoint(i - 1);
                Vector2 q = drawer.ChannelPoint(i);
                Assert.That(Mathf.Abs(p.x - q.x) < 0.01f || Mathf.Abs(p.y - q.y) < 0.01f, Is.True, what + ": tramo " + (i - 1) + " recto (" + p + " → " + q + ")");
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            for (int c = 0; c < views.Length; c++)
            {
                Rect slot = board.SlotRect(c);
                minX = Mathf.Min(minX, slot.xMin);
                maxX = Mathf.Max(maxX, slot.xMax);
            }

            for (int s = 0; s < drawer.ChannelSegmentCount; s++)
            {
                Rect r = drawer.ChannelSegmentRect(s);
                string seg = what + ": tramo " + s + " " + r;
                Assert.That(r.xMin >= minX - 0.01f && r.xMax <= maxX + 0.01f, Is.True, seg + " dentro de las ranuras del tablero");
                bool horizontal = r.width > r.height;
                for (int c = 0; c < views.Length; c++)
                {
                    Vector2 foot = board.Foot(c);
                    float u = views[c].Unit;
                    float top = foot.y + (views[c].HeightU * u) + CuiaView.LiftDp;
                    float bottom = c == a || c == b ? foot.y + (0.5f * w) + 0.01f : foot.y; // el extremo redondo, tras el pie
                    Rect body = Rect.MinMaxRect(foot.x - u, bottom, foot.x + u, top);
                    Assert.That(r.Overlaps(body), Is.False, seg + " no tapa la cuia " + c + " " + body);
                    if (horizontal)
                    {
                        // Cara de la repisa de la fila de c: de su canto inferior (15 dp bajo el pie) a su cara superior (3 dp sobre él).
                        float shelfLow = board.SlotRect(c).yMin;
                        Assert.That(r.yMax <= shelfLow + 0.01f || r.yMin >= foot.y + 3f, Is.True, seg + " no tapa la repisa de la cuia " + c);
                    }
                }
            }
        }

        private static void AssertEnds(BoardAnimator board, RemolinoOverlay drawer, int a, int b, string what)
        {
            int n = drawer.ChannelPointCount;
            Assert.That(n, Is.GreaterThanOrEqualTo(4), what + ": baja de los dos pies y se une por debajo");
            Assert.That(Vector2.Distance(drawer.ChannelPoint(0), board.Foot(a)), Is.LessThan(0.01f), what + ": nace en el pie de " + a);
            Assert.That(Vector2.Distance(drawer.ChannelPoint(n - 1), board.Foot(b)), Is.LessThan(0.01f), what + ": llega al pie de " + b);
        }

        private static RemolinoOverlay Whirl(CuiaView view)
        {
            CuiaOverlay? overlay = view.OverlayOf(MechanicType.Remolino);
            Assert.That(overlay, Is.InstanceOf<RemolinoOverlay>(), "overlay del Remolino en la cuia " + view.Index);
            return (RemolinoOverlay)overlay!;
        }

        private static void FindPair(BoardState board, out int a, out int b)
        {
            a = -1;
            b = -1;
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].Mechanic != MechanicType.Remolino)
                {
                    continue;
                }

                if (a < 0)
                {
                    a = i;
                }
                else if (board[i].MechanicPairId == board[a].MechanicPairId)
                {
                    b = i;
                    break;
                }
            }

            Assert.That(a >= 0 && b > a, Is.True, "una pareja de Remolino en el tablero");
        }

        private static int Rows(BoardAnimator board)
        {
            int rows = 0;
            float last = float.NaN;
            int n = ((ICuiaBoard)board).Count;
            for (int i = 0; i < n; i++)
            {
                float y = board.Foot(i).y;
                if (float.IsNaN(last) || Mathf.Abs(y - last) > 1f)
                {
                    rows++;
                    last = y;
                }
            }

            return rows;
        }

        private static int VisibleIn(Transform anchor)
        {
            int count = 0;
            UnityEngine.UI.Graphic[] graphics = anchor.GetComponentsInChildren<UnityEngine.UI.Graphic>(false);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].enabled && graphics[i].color.a > 0.001f)
                {
                    count++;
                }
            }

            return count;
        }

        // Tablero de count cuias de 4 con la pareja a-b de Remolino; las demás, con una dosis de un color propio.
        private static BoardState PairBoard(int count, int a, int b)
        {
            var cuias = new int[count][];
            var attrs = new MechanicSpec[count];
            for (int i = 0; i < count; i++)
            {
                cuias[i] = i == count - 1 ? new int[0] : new[] { (i % 14) + 1 };
                attrs[i] = i == a ? new MechanicSpec(MechanicType.Remolino, partner: b)
                    : i == b ? new MechanicSpec(MechanicType.Remolino, partner: a) : MechanicSpec.None;
            }

            return Board(cuias, attrs);
        }

        private static Pour[] Trigger(GameBootstrap boot, MechanicTestKit.PourWanted wanted)
        {
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, wanted);
            Assert.That(path, Is.Not.Null, "vertidos que disparan el evento en el nivel " + boot.Flow!.LevelNumber);
            return path!;
        }

        private static void RunUntil(MechanicTestKit.Rig rig, System.Func<bool> condition, int maxMs)
        {
            for (int t = 0; t < maxMs && !condition(); t += PlayModeHelpers.FrameMs)
            {
                rig.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(condition(), Is.True, "condición del banco");
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
            return result.State!;
        }

        // Efectos de prueba: cuenta las burbujas y apunta dónde salió la primera.
        private sealed class RecordingFx : IBoardFx
        {
            public int BubbleCount;
            public Vector2 FirstBubbleAt;

            public bool ReduceMotion { get; set; }

            public void Splash(Vector2 surface, Color tint, float unitDp)
            {
            }

            public void SealDrops(Vector2 rim, Color tint, float unitDp)
            {
            }

            public void Sparkles(Vector2 pos, Color color, int count, float unitDp)
            {
            }

            public void Shards(Vector2 pos, Color color, int count, float unitDp)
            {
            }

            public void Bubbles(Vector2 pos, int count, float unitDp)
            {
                if (ReduceMotion)
                {
                    return;
                }

                if (BubbleCount == 0)
                {
                    FirstBubbleAt = pos;
                }

                BubbleCount += count;
            }
        }
    }
}
