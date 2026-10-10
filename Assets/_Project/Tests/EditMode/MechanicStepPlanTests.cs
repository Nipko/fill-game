#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Presentation;
using AF.Presentation.Mechanics;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Plan de pasos de mecánica (arquitectura del paso 1, §4; R10 por etapas del anexo B de D-007 con el punto fijo f′):
    /// tableros sintéticos con <see cref="BoardBuilder"/> y vertidos aplicados por <see cref="PourRules"/>, que dan las
    /// máscaras de <see cref="PourEvents"/> que lee <see cref="MechanicStepPlan.Build"/>. El orden de las etapas, el sellado
    /// del receptor como paso, el despacho a la cuia dueña de la mecánica y los vertidos normales sin pasos.
    /// </summary>
    public sealed class MechanicStepPlanTests
    {
        private readonly StepEntry[] _plan = new StepEntry[MechanicStepPlan.MaxSteps];

        [Test]
        public void Plan_Liana_ReceiverSealBeforeRelease() // nivel 83: la tapa del taperebá cae antes de que se suelte la Liana
        {
            PourVisual v = StepBoards.LianaSeal(out int receiver, out int liana);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.EqualTo(2), Describe(n));
            AssertEntry(0, MechanicStepKind.Seal, StepStage.C, receiver, -1);
            AssertEntry(1, MechanicStepKind.Release, StepStage.D, liana, receiver);
            Assert.That(_plan[1].Tint, Is.EqualTo(StepBoards.LianaKey), "la llave de la Liana");
            Assert.That(MechanicStepPlan.ReceiverSealIsStep(in v), Is.True, "con otro paso, el sellado del receptor es un paso");
            Assert.That(v.Events.FixedPointPasses, Is.EqualTo(1), "la Liana soltada en (d) cuenta una vuelta sin nada más");
            Assert.That(MechanicStepPlan.FixedPointSealMask(in v), Is.EqualTo(0), "sin gota ni mordisco, (c′) vacía");
        }

        [Test]
        public void Plan_Matapi_ReceiverSealIsStep() // nivel 63: el Matapi recibe su PlayStep(Seal)
        {
            PourVisual v = StepBoards.MatapiSeal(out int matapi);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.EqualTo(1), Describe(n));
            AssertEntry(0, MechanicStepKind.Seal, StepStage.C, matapi, -1);
            Assert.That(MechanicStepPlan.ReceiverSealIsStep(in v), Is.True, "el receptor tenía mecánica");
            Assert.That(v.After[matapi].Mechanic, Is.EqualTo(MechanicType.Matapi), "el Matapi sigue siéndolo, sellado");
            Assert.That(StepStaging.JobEnd(in v, _plan, n, matapi, out Cuia end, out bool sealedNow), Is.True);
            Assert.That(sealedNow, Is.False, "el trabajo del vertido no lo sella");
            Assert.That(end, Is.EqualTo(v.After[matapi]), "lleno, sin tapa");
        }

        [Test]
        public void Plan_LluviaDropThenSeal_FPrime() // anexo B: la última gota completa el agua y sella en f′
        {
            PourVisual v = StepBoards.LluviaLastDrop(out int rain);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.EqualTo(2), Describe(n));
            AssertEntry(0, MechanicStepKind.RainEnd, StepStage.E, rain, -1);
            AssertEntry(1, MechanicStepKind.Seal, StepStage.CPrime, rain, -1);
            Assert.That(_plan[0].Tint, Is.EqualTo(10), "la gota del tinte agua");
            Assert.That(v.Events.FixedPointPasses, Is.GreaterThanOrEqualTo(1));
            Assert.That(MechanicStepPlan.FixedPointSealMask(in v), Is.EqualTo(1 << rain));
            Assert.That(MechanicStepPlan.SealStage(in v, rain), Is.EqualTo(StepStage.CPrime));
        }

        [Test]
        public void Plan_LluviaDrop_NotLast_NoFPrime() // una gota que no es la última: RainDrop, sin sellado ni (c′)
        {
            PourVisual v = StepBoards.LluviaDrop(out int rain);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.EqualTo(1), Describe(n));
            AssertEntry(0, MechanicStepKind.RainDrop, StepStage.E, rain, -1);
            Assert.That(_plan[0].Tint, Is.EqualTo(10), "la gota del tinte agua");
            Assert.That(_plan[0].LayerMask, Is.EqualTo(1 << 2), "la dosis que cae, la tercera");
            Assert.That(MechanicStepPlan.FixedPointSealMask(in v), Is.EqualTo(0), "la Lluvia no sella mientras tenga la nube");
        }

        [Test]
        public void Plan_LluviaWait_RainWaitStep_ReceiverSealIsStep() // la gota que espera es un paso e impide el sellado en el trabajo
        {
            PourVisual v = StepBoards.LluviaWait(out int rain, out int receiver);
            Assert.That(v.Before[receiver].Mechanic, Is.EqualTo(MechanicType.None), "el receptor no tenía mecánica");
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.EqualTo(2), Describe(n));
            AssertEntry(0, MechanicStepKind.Seal, StepStage.C, receiver, -1);
            AssertEntry(1, MechanicStepKind.RainWait, StepStage.E, rain, -1);
            Assert.That(_plan[1].Tint, Is.EqualTo(10), "la gota del tinte agua");
            Assert.That(_plan[1].LayerMask, Is.EqualTo(0), "no cae ninguna dosis");
            Assert.That(MechanicStepPlan.ReceiverSealIsStep(in v), Is.True, "con RainWait, el sellado del receptor es un paso");
            Assert.That(MechanicStepPlan.SealStage(in v, receiver), Is.EqualTo(StepStage.C));
            Assert.That(StepStaging.JobEnd(in v, _plan, n, receiver, out Cuia end, out bool sealedNow), Is.True);
            Assert.That(sealedNow, Is.False, "el trabajo del vertido no lo sella");
            Assert.That(end, Is.EqualTo(v.After[receiver]), "lleno, sin tapa");
        }

        [Test]
        public void Plan_DropSealRelease_Synthetic() // test obligatorio del anexo B: una gota que sella libera la Liana
        {
            PourVisual v = StepBoards.DropSealsAndReleases(out int rain, out int liana);
            Assert.That(v.Events.LianaReleasedMask, Is.EqualTo(1 << liana), "el núcleo suelta la Liana en el mismo vertido");
            Assert.That(v.Events.FixedPointPasses, Is.EqualTo(2));
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.EqualTo(3), Describe(n));
            AssertEntry(0, MechanicStepKind.RainEnd, StepStage.E, rain, -1);
            AssertEntry(1, MechanicStepKind.Seal, StepStage.CPrime, rain, -1);
            AssertEntry(2, MechanicStepKind.Release, StepStage.DPrime, liana, rain);
            Assert.That(MechanicStepPlan.ReleaseStage(in v, liana), Is.EqualTo(StepStage.DPrime), "ninguna cuia de (c) tiene la llave");
        }

        [Test]
        public void Plan_BiteDispatchedToCave() // la cueva ejecuta el mordisco (no hay CaveReceive)
        {
            PourVisual v = StepBoards.Bite(out int cave, out int victim);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.EqualTo(1), Describe(n));
            AssertEntry(0, MechanicStepKind.Bite, StepStage.E, cave, victim);
            Assert.That(victim, Is.EqualTo(v.Receiver), "la víctima es siempre el receptor");
            Assert.That(_plan[0].Tint, Is.EqualTo(v.After[cave].TopColor), "la fruta mordida");
        }

        [Test]
        public void Plan_BiteLost_OnSealedVictim_SealIsStepThenBiteLost()
        {
            PourVisual v = StepBoards.BiteLostOnSeal(out int cave, out int receiver);
            int n = MechanicStepPlan.Build(in v, _plan);
            Assert.That(n, Is.EqualTo(2), Describe(n));
            AssertEntry(0, MechanicStepKind.Seal, StepStage.C, receiver, -1);
            AssertEntry(1, MechanicStepKind.BiteLost, StepStage.E, cave, receiver);
        }

        [Test]
        public void Plan_NormalPour_NoSteps_SealInJob()
        {
            BoardState before = StepBoards.Build(new[] { new[] { 1, 1, 1 }, new[] { 1 }, new int[0] }, null);
            PourVisual v = StepBoards.Pour(before, 1, 0);
            Assert.That(v.After.IsSealed(0), Is.True);
            Assert.That(MechanicStepPlan.Build(in v, _plan), Is.EqualTo(0), "un vertido normal no tiene pasos");
            Assert.That(MechanicStepPlan.ReceiverSealIsStep(in v), Is.False, "el sellado se queda en el trabajo (450 ms, 07 §5.3)");

            var noEvents = new PourVisual(new Pour(1, 0, 1), default, before, v.After);
            Assert.That(MechanicStepPlan.Build(in noEvents, _plan), Is.EqualTo(0), "sin eventos del núcleo, sin plan");
        }

        [Test]
        public void Plan_FirstStages_RevealMudReceiveOpen()
        {
            PourVisual reveal = StepBoards.Reveal(out int source);
            Assert.That(MechanicStepPlan.Build(in reveal, _plan), Is.EqualTo(1));
            AssertEntry(0, MechanicStepKind.Reveal, StepStage.A, source, -1);
            Assert.That(_plan[0].LayerMask, Is.EqualTo(1 << 1), "la nueva cima");

            PourVisual mud = StepBoards.MudWash(out int washed);
            Assert.That(MechanicStepPlan.Build(in mud, _plan), Is.EqualTo(1));
            AssertEntry(0, MechanicStepKind.MudWash, StepStage.A, washed, -1);
            Assert.That(_plan[0].LayerMask, Is.EqualTo(1 << 1), "la dosis del barro");

            PourVisual whirl = StepBoards.Remolino(out int touched, out int partner);
            Assert.That(MechanicStepPlan.Build(in whirl, _plan), Is.EqualTo(1));
            AssertEntry(0, MechanicStepKind.Receive, StepStage.A, partner, touched);

            PourVisual hoja = StepBoards.HojaOpen(out int leaf);
            Assert.That(MechanicStepPlan.Build(in hoja, _plan), Is.EqualTo(1));
            AssertEntry(0, MechanicStepKind.Open, StepStage.B, leaf, -1);

            PourVisual brote = StepBoards.BroteOpen(out int bud);
            Assert.That(MechanicStepPlan.Build(in brote, _plan), Is.EqualTo(1));
            AssertEntry(0, MechanicStepKind.Open, StepStage.B, bud, -1);
        }

        [Test]
        public void Plan_IsSortedByStage_AndDoesNotAllocate()
        {
            PourVisual v = StepBoards.DropSealsAndReleases(out _, out _);
            Assert.That(() => { MechanicStepPlan.Build(in v, _plan); }, Is.Not.AllocatingGCMemory());
            int n = MechanicStepPlan.Build(in v, _plan);
            for (int i = 1; i < n; i++)
            {
                Assert.That(_plan[i].Stage, Is.GreaterThanOrEqualTo(_plan[i - 1].Stage), "orden de R10");
            }
        }

        private void AssertEntry(int at, MechanicStepKind kind, StepStage stage, int index, int other)
        {
            StepEntry e = _plan[at];
            Assert.That(e.Kind, Is.EqualTo(kind), "paso " + at);
            Assert.That(e.Stage, Is.EqualTo(stage), "etapa del paso " + at);
            Assert.That(e.Index, Is.EqualTo(index), "dueña del paso " + at);
            Assert.That(e.OtherIndex, Is.EqualTo(other), "otra cuia del paso " + at);
        }

        private string Describe(int n)
        {
            var sb = new System.Text.StringBuilder("plan:");
            for (int i = 0; i < n; i++)
            {
                sb.Append(' ').Append(_plan[i].ToString());
            }

            return sb.ToString();
        }
    }

    /// <summary>Tableros sintéticos de capacidad 4 y vertidos del núcleo para los tests del plan y del staging.</summary>
    internal static class StepBoards
    {
        public const int LianaKey = 3;

        public static BoardState Build(int[][] cuias, MechanicSpec[]? attrs, int[]? caps = null, int[][]? hidden = null)
        {
            if (caps == null)
            {
                caps = new int[cuias.Length];
                for (int i = 0; i < caps.Length; i++)
                {
                    caps[i] = 4;
                }
            }

            BoardBuildResult result = BoardBuilder.Build(caps, cuias, attrs, hidden);
            Assert.That(result.Error, Is.Null);
            return result.State!;
        }

        public static PourVisual Pour(BoardState before, int source, int target)
        {
            Assert.That(PourRules.IsLegal(before, source, target), Is.True, source + " → " + target);
            BoardState after = PourRules.Apply(before, source, target, PourMode.Run, out PourEvents ev);
            return new PourVisual(new Pour(source, target, ev.Amount), ev, before, after);
        }

        public static BoardState After(BoardState before, int source, int target)
        {
            return PourRules.Apply(before, source, target, PourMode.Run);
        }

        // 1 → 0 sella el taperebá (3) y suelta la Liana con llave 3.
        public static PourVisual LianaSeal(out int receiver, out int liana)
        {
            BoardState b = Build(
                new[] { new[] { 3, 3, 3 }, new[] { 3 }, new[] { 1, 2, 1, 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, key: LianaKey), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            receiver = 0;
            liana = 2;
            return Pour(b, 1, 0);
        }

        // 1 → 0 llena el Matapi de 5 y lo sella.
        public static PourVisual MatapiSeal(out int matapi)
        {
            BoardState b = Build(
                new[] { new[] { 5, 5 }, new[] { 6, 5, 5 }, new[] { 6, 6, 6 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Matapi), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            matapi = 0;
            return Pour(b, 1, 0);
        }

        // Lluvia (0) con tres aguas, una gota pendiente y k = 2: el primer vertido útil cuenta y el segundo deja caer la
        // última gota, que completa el agua.
        public static PourVisual LluviaLastDrop(out int rain)
        {
            BoardState b = Build(
                new[] { new[] { 10, 10, 10 }, new[] { 2, 1 }, new[] { 1 }, new[] { 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 1, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            rain = 0;
            BoardState counted = After(b, 4, 2);
            Assert.That(counted[0].MechanicCounter, Is.EqualTo(1));
            return Pour(counted, 3, 5);
        }

        // Lluvia (0) con dos aguas, dos gotas pendientes y k = 2: el primer vertido útil cuenta y el segundo deja caer una
        // gota que NO es la última (RainDrop): la nube se queda con una gota pendiente y el contador vuelve a 0.
        public static PourVisual LluviaDrop(out int rain)
        {
            BoardState b = Build(
                new[] { new[] { 10, 10 }, new[] { 2, 1 }, new[] { 1 }, new[] { 2 }, new[] { 1, 1 }, new[] { 2, 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            rain = 0;
            BoardState counted = After(b, 4, 2);
            Assert.That(counted[0].MechanicCounter, Is.EqualTo(1));
            PourVisual v = Pour(counted, 3, 5);
            Assert.That(v.Events.LluviaDropMask, Is.EqualTo(1 << rain), "cae una gota");
            Assert.That(v.Events.LluviaEndedMask, Is.EqualTo(0), "no es la última");
            Assert.That(v.After[rain].Mechanic, Is.EqualTo(MechanicType.Lluvia), "la nube se queda");
            Assert.That(v.After[rain].MechanicN, Is.EqualTo(1), "con una gota pendiente");
            Assert.That(v.After[rain].MechanicCounter, Is.EqualTo(0), "y el contador a 0");
            return v;
        }

        // Lluvia (0) con dos aguas, dos gotas pendientes y k = 2: el primer vertido útil (1 → 0) la llena con la fruta 1 y
        // deja el contador en k − 1; el segundo (3 → 2) sella el receptor (2) y la gota toca, pero la Lluvia está llena: espera
        // (02 §9.1 M6) y el contador se queda en k − 1 (RainWait).
        public static PourVisual LluviaWait(out int rain, out int receiver)
        {
            BoardState b = Build(
                new[] { new[] { 10, 10, 1 }, new[] { 3, 1 }, new[] { 2, 2, 2 }, new[] { 3, 2 }, new[] { 1, 1 }, new[] { 3, 3 } },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 2, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            rain = 0;
            receiver = 2;
            BoardState full = After(b, 1, 0);
            Assert.That(full[0].IsFull, Is.True, "la Lluvia, llena");
            Assert.That(full[0].MechanicCounter, Is.EqualTo(1), "con el contador en k − 1");
            PourVisual v = Pour(full, 3, 2);
            Assert.That(v.Events.LluviaWaitingMask, Is.EqualTo(1 << rain), "la gota espera");
            Assert.That(v.Events.LluviaDropMask, Is.EqualTo(0), "no cae");
            Assert.That(v.After[rain], Is.EqualTo(full[0]), "la Lluvia no cambia: mismo contenido, n y contador k − 1");
            Assert.That(v.After.IsSealed(receiver), Is.True, "el vertido sella el receptor");
            return v;
        }

        // Lluvia (0) + Liana con llave agua (1): la última gota sella el agua en f′ y suelta la Liana en (d′).
        public static PourVisual DropSealsAndReleases(out int rain, out int liana)
        {
            BoardState b = Build(
                new[] { new[] { 10, 10, 10 }, new[] { 1, 2, 1, 2 }, new[] { 2, 1 }, new[] { 1 }, new[] { 2 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Lluvia, key: 10, n: 1, k: 2), new MechanicSpec(MechanicType.Liana, key: 10), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            rain = 0;
            liana = 1;
            BoardState counted = After(b, 3, 2);
            Assert.That(counted[0].MechanicCounter, Is.EqualTo(1));
            return Pour(counted, 2, 5);
        }

        // Cueva de la Piraña (0, cap 2, k = 2): el segundo vertido útil la hace morder la cima del receptor (2).
        public static PourVisual Bite(out int cave, out int victim)
        {
            BoardState b = Build(
                new[] { new int[0], new[] { 3, 4 }, new[] { 4 }, new[] { 3, 3 }, new[] { 4, 4 }, new[] { 3 } },
                new[] { new MechanicSpec(MechanicType.Pirana, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None },
                new[] { 2, 4, 4, 4, 4, 4 });
            cave = 0;
            BoardState counted = After(b, 5, 3);
            Assert.That(counted[0].MechanicCounter, Is.EqualTo(1));
            PourVisual v = Pour(counted, 1, 2);
            victim = 2;
            Assert.That(v.Events.PiranaCaveMask, Is.EqualTo(1 << cave));
            Assert.That(v.Events.PiranaBittenMask, Is.EqualTo(1 << victim));
            return v;
        }

        // El segundo vertido útil sella el receptor: el mordisco se pierde (02 §9.1 M7) y el sellado es un paso.
        public static PourVisual BiteLostOnSeal(out int cave, out int receiver)
        {
            BoardState b = Build(
                new[] { new int[0], new[] { 3, 4 }, new[] { 4, 4, 4 }, new[] { 3, 3 }, new[] { 3 }, new int[0] },
                new[] { new MechanicSpec(MechanicType.Pirana, k: 2), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None },
                new[] { 2, 4, 4, 4, 4, 4 });
            cave = 0;
            receiver = 2;
            BoardState counted = After(b, 4, 3);
            PourVisual v = Pour(counted, 1, 2);
            Assert.That(v.Events.PiranaBiteLostMask, Is.EqualTo(1 << cave));
            Assert.That(v.After.IsSealed(receiver), Is.True);
            return v;
        }

        // 0 → 3 deja la dosis oculta 1 del origen en la cima y la revela (R7).
        public static PourVisual Reveal(out int source)
        {
            BoardState b = Build(new[] { new[] { 3, 1, 2 }, new[] { 1, 1, 1 }, new[] { 3, 3, 3 }, new int[0], new[] { 2, 2, 2 } }, null, null, new[] { new[] { 0, 0 }, new[] { 0, 1 } });
            source = 0;
            return Pour(b, 0, 3);
        }

        // 0 → 1 con Barro en la cima de 1: lo lava.
        public static PourVisual MudWash(out int washed)
        {
            BoardState b = Build(new[] { new[] { 2, 2 }, new[] { 1, Cuia.MudColor }, new int[0] }, null);
            washed = 1;
            return Pour(b, 0, 1);
        }

        // 0 → 1 (Remolino): la racha entra por el fondo de la pareja (2).
        public static PourVisual Remolino(out int touched, out int partner)
        {
            BoardState b = Build(
                new[] { new[] { 1 }, new int[0], new[] { 2 }, new int[0] },
                new[] { MechanicSpec.None, new MechanicSpec(MechanicType.Remolino, partner: 2), new MechanicSpec(MechanicType.Remolino, partner: 1), MechanicSpec.None });
            touched = 1;
            partner = 2;
            return Pour(b, 0, 1);
        }

        // 1 → 0 con la llave (4): la Hoja cae.
        public static PourVisual HojaOpen(out int leaf)
        {
            BoardState b = Build(
                new[] { new int[0], new[] { 3, 4, 4 }, new[] { 4, 4 }, new[] { 3, 3, 3 } },
                new[] { new MechanicSpec(MechanicType.Hoja, key: 4), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            leaf = 0;
            return Pour(b, 1, 0);
        }

        // 1 → 0 con el color de la cima (5): el Brote germina.
        public static PourVisual BroteOpen(out int bud)
        {
            BoardState b = Build(
                new[] { new[] { 3, 5 }, new[] { 6, 5 }, new[] { 3, 3, 3 }, new[] { 5, 5 }, new[] { 6, 6, 6 } },
                new[] { new MechanicSpec(MechanicType.Brote), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            bud = 0;
            return Pour(b, 1, 0);
        }
    }
}
