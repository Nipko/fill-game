#nullable enable
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Presentation;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Consultas de la vista sobre dos tableros (arquitectura del paso 1, §3 y §4): <see cref="BoardDiff.ChangedOthers"/>
    /// marca las cuias que cambiaron en un vertido aparte del origen y del receptor (la gota de la Lluvia, la cueva de la
    /// Piraña, la Liana soltada; 02 §9.1 M3, M6 y M7) y <see cref="BoardDiff.RemolinoPartner"/> encuentra la pareja de un
    /// Remolino (M4). Tableros sintéticos de <see cref="BoardBuilder"/> y vertidos de <see cref="PourRules"/>.
    /// </summary>
    public sealed class BoardDiffTests
    {
        [Test]
        public void ChangedOthers_LluviaDrop()
        {
            // Lluvia en la cuia 4 (agua 10, 3 gotas, k = 2): el segundo vertido útil deja caer una gota en ella.
            BoardState start = Build(
                new[] { new[] { 1, 2 }, new[] { 2 }, new[] { 3, 1 }, new int[0], new[] { 10 } },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Lluvia, key: 10, n: 3, k: 2) });
            BoardState before = PourRules.Apply(start, 0, 1, PourMode.Run, out PourEvents first);
            Assert.That(first.LluviaDropMask, Is.EqualTo(0), "el primer vertido útil solo cuenta");
            Assert.That(BoardDiff.ChangedOthers(start, before, 0, first.Receiver), Is.EqualTo(1 << 4), "el contador de la Lluvia está en la cuia");

            BoardState after = PourRules.Apply(before, 2, 0, PourMode.Run, out PourEvents ev);
            Assert.That(ev.LluviaDropMask, Is.EqualTo(1 << 4), "cae la gota");
            Assert.That(after[4].Count, Is.EqualTo(2));
            Assert.That(BoardDiff.ChangedOthers(before, after, 2, ev.Receiver), Is.EqualTo(1 << 4));
        }

        [Test]
        public void ChangedOthers_PiranaBite()
        {
            // Cueva de la Piraña en la cuia 4 (k = 2): en el segundo vertido útil la piraña muerde la cima del receptor (la
            // víctima es siempre el receptor, 02 §9.1 M7) y la lleva a la cueva. La víctima termina en su estado final como
            // receptor; ChangedOthers marca la cueva.
            BoardState start = Build(
                new[] { new[] { 1, 2 }, new[] { 2 }, new[] { 3, 1 }, new int[0], new int[0] },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Pirana, k: 2) });
            BoardState before = PourRules.Apply(start, 0, 1, PourMode.Run);
            BoardState after = PourRules.Apply(before, 2, 0, PourMode.Run, out PourEvents ev);
            Assert.That(ev.PiranaCaveMask, Is.EqualTo(1 << 4), "la cueva recibe la dosis mordida");
            Assert.That(ev.PiranaBittenMask, Is.EqualTo(1 << ev.Receiver), "la víctima es el receptor");
            Assert.That(after[4].TopColor, Is.EqualTo(1));

            int changed = BoardDiff.ChangedOthers(before, after, 2, ev.Receiver);
            Assert.That(changed, Is.EqualTo(1 << 4), "la cueva, nunca el origen ni el receptor");

            // La víctima perdió la dosis que acababa de recibir: termina en su estado final por el camino del receptor.
            Assert.That(after[ev.Receiver].Count, Is.EqualTo(before[ev.Receiver].Count + ev.Amount - 1));
        }

        [Test]
        public void ChangedOthers_LianaRelease()
        {
            // Liana con fruta 3 en la cuia 2: el vertido 0→1 sella la cuia 1 con la fruta 3 y la suelta (02 §9.1 M3).
            BoardState before = Build(
                new[] { new[] { 3 }, new[] { 3, 3, 3 }, new[] { 1, 1 }, new[] { 1, 1 } },
                new[] { MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Liana, key: 3), MechanicSpec.None });
            BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
            Assert.That(after.IsSealed(1), Is.True);
            Assert.That(ev.LianaReleasedMask, Is.EqualTo(1 << 2));
            Assert.That(after[2].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(BoardDiff.ChangedOthers(before, after, 0, ev.Receiver), Is.EqualTo(1 << 2));
        }

        [Test]
        public void ChangedOthers_ExcludesSourceAndReceiver()
        {
            // Vertido normal: cambian el origen y el receptor y nada más.
            BoardState before = Build(new[] { new[] { 1, 2 }, new[] { 2 }, new int[0] }, null);
            BoardState after = PourRules.Apply(before, 0, 1, PourMode.Run, out PourEvents ev);
            Assert.That(BoardDiff.ChangedOthers(before, after, 0, ev.Receiver), Is.EqualTo(0));
            Assert.That(BoardDiff.ChangedOthers(before, after, -1, -1), Is.EqualTo((1 << 0) | (1 << 1)), "sin excluir: origen y receptor");

            // Remolino: la cuia tocada (1) no cambia; el receptor es la pareja (2) y queda excluido como receptor.
            BoardState board = Remolinos();
            BoardState through = PourRules.Apply(board, 0, 1, PourMode.Run, out PourEvents rev);
            Assert.That(rev.Receiver, Is.EqualTo(2));
            Assert.That(BoardDiff.ChangedOthers(board, through, 0, rev.Receiver), Is.EqualTo(0), "la cuia tocada no cambió");
            Assert.That(BoardDiff.ChangedOthers(board, through, 0, 1), Is.EqualTo(1 << 2), "con la tocada como receptor, la pareja es otra cuia");

            // El mismo tablero no marca nada.
            Assert.That(BoardDiff.ChangedOthers(through, through, -1, -1), Is.EqualTo(0));
        }

        [Test]
        public void RemolinoPartner_FindsPair()
        {
            BoardState board = Remolinos();
            Assert.That(BoardDiff.RemolinoPartner(board, 1), Is.EqualTo(2));
            Assert.That(BoardDiff.RemolinoPartner(board, 2), Is.EqualTo(1));

            // Dos parejas: cada una encuentra la suya.
            BoardState two = Build(
                new[] { new[] { 1 }, new int[0], new int[0], new[] { 2 }, new int[0] },
                new[]
                {
                    new MechanicSpec(MechanicType.Remolino, partner: 3), new MechanicSpec(MechanicType.Remolino, partner: 4), MechanicSpec.None,
                    new MechanicSpec(MechanicType.Remolino, partner: 0), new MechanicSpec(MechanicType.Remolino, partner: 1),
                });
            Assert.That(BoardDiff.RemolinoPartner(two, 0), Is.EqualTo(3));
            Assert.That(BoardDiff.RemolinoPartner(two, 3), Is.EqualTo(0));
            Assert.That(BoardDiff.RemolinoPartner(two, 1), Is.EqualTo(4));
            Assert.That(BoardDiff.RemolinoPartner(two, 4), Is.EqualTo(1));
        }

        [Test]
        public void RemolinoPartner_NoneForPlainCuia()
        {
            BoardState board = Remolinos();
            Assert.That(BoardDiff.RemolinoPartner(board, 0), Is.EqualTo(-1));
            Assert.That(BoardDiff.RemolinoPartner(board, 3), Is.EqualTo(-1));
            Assert.That(BoardDiff.RemolinoPartner(board, -1), Is.EqualTo(-1), "fuera del tablero");
            Assert.That(BoardDiff.RemolinoPartner(board, 4), Is.EqualTo(-1), "fuera del tablero");

            BoardState hoja = Build(new[] { new[] { 1 }, new int[0] }, new[] { MechanicSpec.None, new MechanicSpec(MechanicType.Hoja, key: 1) });
            Assert.That(BoardDiff.RemolinoPartner(hoja, 1), Is.EqualTo(-1), "otra mecánica");
        }

        // Cuia 0 normal con un 1; 1 y 2, la pareja de Remolino (la 2 con un 2); 3 vacía.
        internal static BoardState Remolinos()
        {
            return Build(
                new[] { new[] { 1 }, new int[0], new[] { 2 }, new int[0] },
                new[] { MechanicSpec.None, new MechanicSpec(MechanicType.Remolino, partner: 2), new MechanicSpec(MechanicType.Remolino, partner: 1), MechanicSpec.None });
        }

        // Tablero de capacidad 4 con BoardBuilder (sin LevelValidation: solo hacen falta las reglas del núcleo).
        internal static BoardState Build(int[][] cuias, MechanicSpec[]? attrs, int[][]? hidden = null)
        {
            var caps = new int[cuias.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = 4;
            }

            BoardBuildResult result = BoardBuilder.Build(caps, cuias, attrs, hidden);
            Assert.That(result.Error, Is.Null);
            return result.State!;
        }
    }
}
