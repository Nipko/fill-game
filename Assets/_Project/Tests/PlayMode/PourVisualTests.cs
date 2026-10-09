#nullable enable
using System;
using System.Collections;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La vista sigue al modelo en cada vertido con mecánicas (arquitectura del paso 1, §4, ola 1): vistas de cuia reales en
    /// un Canvas propio y una <see cref="BoardAnimator"/> a la que se encolan <see cref="PourVisual"/> de tableros
    /// sintéticos (<see cref="BoardBuilder"/> y <see cref="PourRules"/>), con el reloj en manos del test. Por un Remolino la
    /// cuia tocada no se llena y la pareja termina como el modelo (02 §9.1 M4); con Barro en la cima el relleno empieza en su
    /// dosis (M5); la gota de la Lluvia llega a su cuia al terminar el caudal (M6); los vertidos normales siguen encadenando
    /// (02 §3.2) y los que cambian otras cuias no; y con Reducir movimiento no hay partículas (02 §4).
    /// </summary>
    public sealed class PourVisualTests
    {
        private const int FrameMs = 16;
        private static readonly PourTimings Timings = new PourTimings(100f, 300f, 280f, 80f, 300f);
        private static ProceduralArt? s_art;

        private Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        [UnityTest]
        public IEnumerator Remolino_TouchedUnchanged_PartnerEndsAsModel()
        {
            // 0 → 1 (Remolino): la racha entra por el fondo de la pareja (2).
            BoardState before = Build(
                new[] { new[] { 1 }, new int[0], new[] { 2 }, new int[0] },
                new[] { MechanicSpec.None, new MechanicSpec(MechanicType.Remolino, partner: 2), new MechanicSpec(MechanicType.Remolino, partner: 1), MechanicSpec.None });
            BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
            Assert.That(ev.Receiver, Is.EqualTo(2));
            yield return StartRig(before);
            Rig rig = _rig!;

            var visual = new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after);
            Assert.That(visual.ThroughRemolino, Is.True);
            rig.Animator.EnqueuePour(in visual);
            bool partnerJumped = false;
            int elapsed = rig.RunUntilIdle(3000, t =>
            {
                Assert.That(rig.Animator.ShownOf(1), Is.EqualTo(before[1]), "la cuia tocada no cambia (t = " + t + ")");
                Assert.That(rig.Views[1].LiquidLevel, Is.EqualTo(0f).Within(1e-4f), "la cuia tocada no se llena (t = " + t + ")");
                partnerJumped |= !rig.Animator.IsIdle && rig.Animator.ShownOf(2) == after[2];
            });

            Assert.That(rig.Animator.IsIdle, Is.True, "quieta en " + elapsed + " ms");
            Assert.That(partnerJumped, Is.True, "la pareja salta al llegar el caudal, antes de que vuelva el origen");
            AssertShowsModel(rig, after);
            Assert.That(rig.Views[2].LiquidLevel, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(rig.Views[2].DoseColor(0), Is.EqualTo(Palette.Tint(1)), "lo vertido entra por el fondo");
            Assert.That(rig.Animator.ChainedCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MudExposed_FillStartsOnMud()
        {
            // 0 → 1 con Barro en la cima de 1: lava el barro y las 2 dosis ocupan su lugar (espacio + 1; 02 §9.1 M5).
            BoardState before = Build(new[] { new[] { 2, 2 }, new[] { 1, Cuia.MudColor }, new int[0] }, null);
            BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
            Assert.That(ev.BarroWashedMask, Is.EqualTo(1 << 1));
            Assert.That(after[1].Count, Is.EqualTo(3));
            yield return StartRig(before);
            Rig rig = _rig!;
            Color mud = rig.Views[1].DoseColor(1);
            Color poured = Palette.Tint(2);
            Assert.That(mud, Is.Not.EqualTo(poured));

            rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after));

            // Antes de que llegue el chorro (210 ms + 60 de caída) el barro sigue a la vista.
            rig.Run(240);
            Assert.That(rig.Views[1].DoseColor(1), Is.EqualTo(mud), "el barro se ve hasta que llega el chorro");

            // Con el chorro llegando, la dosis del barro (Count − 1) se llena del color vertido.
            rig.Run(160);
            Assert.That(rig.Views[1].DoseColor(1), Is.EqualTo(poured), "la dosis 15 se sustituye por el color vertido");
            Assert.That(rig.Views[1].LiquidLevel, Is.LessThan(3f), "aún llenándose");
            Assert.That(rig.Views[1].LiquidLevel, Is.GreaterThan(1f), "sobre la dosis de abajo");

            rig.RunUntilIdle(3000, null);
            Assert.That(rig.Animator.IsIdle, Is.True);
            AssertShowsModel(rig, after);
            Assert.That(rig.Views[1].LiquidLevel, Is.EqualTo(3f).Within(1e-4f), "nunca por encima de lo que hay");
            Assert.That(rig.Views[1].DoseColor(2), Is.EqualTo(poured));
        }

        [UnityTest]
        public IEnumerator LluviaDrop_OtherCuiaEndsAsModel()
        {
            // Lluvia en la 4 (agua 10, 3 gotas, k = 2): el primer vertido útil (fuera de la vista) cuenta y el segundo, 2 → 0,
            // deja caer una gota en ella.
            BoardState start = Build(
                new[] { new[] { 1, 2 }, new[] { 2 }, new[] { 3, 1 }, new int[0], new[] { 10 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 2) });
            BoardState before = PourRules.Apply(start, 0, 1, PourMode.Run);
            BoardState after = PourRules.Apply(before, 2, 0, PourMode.Run, out PourEvents ev);
            Assert.That(ev.LluviaDropMask, Is.EqualTo(1 << 4));
            yield return StartRig(before);
            Rig rig = _rig!;

            rig.Animator.EnqueuePour(new PourVisual(new Pour(2, 0, ev.Amount), ev, before, after));

            // Caudal: 210 ms + 1 dosis de 280 y 60 de caída = 550 ms; hasta entonces la Lluvia sigue como estaba.
            rig.Run(528);
            Assert.That(rig.Animator.ShownOf(4), Is.EqualTo(before[4]));
            Assert.That(rig.Views[4].LiquidLevel, Is.EqualTo(1f).Within(1e-4f));

            // Al terminar el caudal la gota ya está, aunque el origen aún vuelve a su ranura.
            rig.Run(48);
            Assert.That(rig.Animator.IsIdle, Is.False, "el origen aún vuelve");
            Assert.That(rig.Animator.ShownOf(4), Is.EqualTo(after[4]), "la Lluvia salta a su estado final al llegar el caudal");

            rig.RunUntilIdle(3000, null);
            Assert.That(rig.Animator.IsIdle, Is.True);
            AssertShowsModel(rig, after);
            Assert.That(rig.Views[4].LiquidLevel, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(rig.Views[4].DoseColor(1), Is.EqualTo(Palette.Tint(10)), "la gota del tinte agua");
        }

        [UnityTest]
        public IEnumerator NormalPours_StillChain()
        {
            // single (pruebas): tres 0 → 1 de una dosis seguidos se encadenan en una sola inclinación, sin saltos (02 §3.2).
            BoardState s0 = Build(new[] { new[] { 2, 1, 1, 1 }, new[] { 1 }, new int[0] }, null);
            yield return StartRig(s0);
            Rig rig = _rig!;
            BoardState s3 = EnqueueThreeSingles(rig, s0);
            Assert.That(rig.Animator.ChainedCount, Is.EqualTo(2), "los tres van en un trabajo");
            Assert.That(rig.Animator.PendingCount, Is.EqualTo(1));
            rig.RunUntilIdle(4000, null);
            Assert.That(rig.Animator.IsIdle, Is.True);
            Assert.That(rig.Animator.LagSnapCount, Is.EqualTo(0), "sin saltos");
            AssertShowsModel(rig, s3);

            // Con una Lluvia, cada vertido útil cambia su contador (otra cuia): no se encadenan y la vista acaba igual que el
            // modelo.
            rig.Dispose();
            BoardState r0 = Build(
                new[] { new[] { 2, 1, 1, 1 }, new[] { 1 }, new int[0], new[] { 10 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 5) });
            yield return StartRig(r0);
            rig = _rig!;
            BoardState r3 = EnqueueThreeSingles(rig, r0);
            Assert.That(rig.Animator.ChainedCount, Is.EqualTo(0), "con otras cuias que cambian no se encadena");
            rig.RunUntilIdle(6000, null);
            Assert.That(rig.Animator.IsIdle, Is.True);
            AssertShowsModel(rig, r3);
            Assert.That(r3[3].MechanicCounter, Is.EqualTo(3), "el contador avanzó en el modelo");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_NoParticles()
        {
            // 0 → 1 sella la 1: salpicadura por dosis y gotas del sellado (guía 6.6 y 6.8), salvo con Reducir movimiento.
            BoardState before = Build(new[] { new[] { 1 }, new[] { 1, 1, 1 }, new int[0] }, null);
            BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
            Assert.That(after.IsSealed(1), Is.True);

            int peak = 0;
            yield return StartRig(before);
            Rig rig = _rig!;
            rig.Fx.ReduceMotion = true;
            rig.Animator.ReduceMotion = true;
            rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after));
            rig.RunUntilIdle(3000, _ => peak = Mathf.Max(peak, rig.Fx.ActiveCount));
            Assert.That(peak, Is.EqualTo(0), "sin partículas con Reducir movimiento");
            AssertShowsModel(rig, after);

            rig.Fx.Splash(new Vector2(100f, -300f), Color.white, 20f);
            rig.Fx.SealDrops(new Vector2(100f, -300f), Color.white, 20f);
            rig.Tick(FrameMs);
            Assert.That(rig.Fx.ActiveCount, Is.EqualTo(0), "Splash y SealDrops no emiten");

            // Sin Reducir movimiento, el mismo vertido sí emite (el camino existe).
            rig.Dispose();
            yield return StartRig(before);
            rig = _rig!;
            peak = 0;
            rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, before, after));
            rig.RunUntilIdle(3000, _ => peak = Mathf.Max(peak, rig.Fx.ActiveCount));
            Assert.That(peak, Is.GreaterThan(0));
        }

        // Encola tres vertidos single 0 → 1 desde s0 sin avanzar el reloj y devuelve el tablero final del modelo.
        private static BoardState EnqueueThreeSingles(Rig rig, BoardState s0)
        {
            BoardState state = s0;
            for (int i = 0; i < 3; i++)
            {
                BoardState next = PourRules.Apply(state, 0, 1, PourMode.Single, out PourEvents ev);
                rig.Animator.EnqueuePour(new PourVisual(new Pour(0, 1, ev.Amount), ev, state, next));
                state = next;
            }

            return state;
        }

        private static void AssertShowsModel(Rig rig, BoardState model)
        {
            for (int i = 0; i < model.Count; i++)
            {
                Assert.That(rig.Animator.ShownOf(i), Is.EqualTo(model[i]), "cuia " + i);
                Assert.That(rig.Views[i].LiquidLevel, Is.EqualTo((float)model[i].Count).Within(1e-4f), "nivel de la cuia " + i);
                Assert.That(rig.Views[i].Sealed, Is.EqualTo(model.IsSealed(i)), "tapa de la cuia " + i);
            }
        }

        private IEnumerator StartRig(BoardState board)
        {
            _rig?.Dispose();
            _rig = new Rig(Art, board);
            yield return null; // un fotograma para que el Canvas se construya
        }

        // Tablero de capacidad 4 (sin LevelValidation: solo hacen falta las reglas del núcleo).
        private static BoardState Build(int[][] cuias, MechanicSpec[]? attrs)
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

        /// <summary>Canvas con el tablero, las capas, los efectos y una vista por cuia, como GameScreen.</summary>
        private sealed class Rig : IDisposable
        {
            private readonly GameObject _root;
            private bool _disposed;

            public Rig(ProceduralArt art, BoardState board)
            {
                _root = new GameObject("PourVisualRig", typeof(RectTransform), typeof(Canvas));
                var canvas = _root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2
                    | AdditionalCanvasShaderChannels.TexCoord3;
                var rootRt = (RectTransform)_root.transform;
                RectTransform boardRt = NewLayer("Board", rootRt);
                RectTransform back = NewLayer("MechanicBack", boardRt);
                Fx = new FxLayer(boardRt, art);
                Views = new CuiaView[board.Count];
                for (int i = 0; i < board.Count; i++)
                {
                    Views[i] = CuiaView.Create(boardRt, i, board[i].Capacity, art, index => { }, (index, inside) => { }, Fx);
                    Views[i].SetGeometry(new Vector2(36f + (i * 64f), -600f), 40f, 60f);
                    Views[i].SetLiftDuration(100f);
                }

                RectTransform front = NewLayer("MechanicFront", boardRt);
                RectTransform streams = NewLayer("Streams", boardRt);
                Fx.Root.SetAsLastSibling();
                RectTransform pourFront = NewLayer("PourFront", rootRt);
                Animator = new BoardAnimator(streams, pourFront) { Fx = Fx, BoardWidth = 360f };
                Animator.Bind(Views, Timings, 2, back, front);
                for (int i = 0; i < board.Count; i++)
                {
                    Animator.ShowContent(i, board[i], board.IsSealed(i));
                }
            }

            public BoardAnimator Animator { get; }

            public CuiaView[] Views { get; }

            public FxLayer Fx { get; }

            public void Tick(float dtMs)
            {
                Animator.Tick(dtMs);
                for (int i = 0; i < Views.Length; i++)
                {
                    Views[i].Tick(dtMs);
                }

                Fx.Tick(dtMs);
            }

            // Avanza ms milisegundos en fotogramas de 16 ms.
            public void Run(int ms)
            {
                for (int t = 0; t < ms; t += FrameMs)
                {
                    Tick(FrameMs);
                }
            }

            // Avanza hasta que la cola queda quieta (como mucho maxMs) y devuelve el tiempo; each recibe el tiempo de cada
            // fotograma.
            public int RunUntilIdle(int maxMs, Action<int>? each)
            {
                int t = 0;
                while (!Animator.IsIdle && t < maxMs)
                {
                    Tick(FrameMs);
                    t += FrameMs;
                    each?.Invoke(t);
                }

                return t;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Object.Destroy(_root);
            }

            private static RectTransform NewLayer(string name, RectTransform parent)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return rt;
            }
        }
    }
}
