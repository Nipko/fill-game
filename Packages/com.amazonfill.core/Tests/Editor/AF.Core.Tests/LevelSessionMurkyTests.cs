#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Aguas turbias en la partida (C2-1): 02 R7 y §11 (T6), 04 §2.1 (Marea conserva lo revelado), §2.4 a (Pororoca revela
    /// todo), §2.5 (Luciérnaga revela una cuia), §2.7 (Volver a la orilla conserva las capas reveladas) y §5.1 (regresión 3);
    /// <see cref="LevelSession.RestoreRevealed"/> para reanudar y rehacer la sesión (02 R8, §14; 04 §2.2). Test compartido
    /// (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class LevelSessionMurkyTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static Cuia Murky(int capacity, int hiddenMask, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop, hiddenMask);
        }

        /// <summary>[2*, 1], [1, 1, 1], [2, 2, 2], vacía: el vertido 0 → 1 sella el 1 y revela el 2 (R7).</summary>
        private static BoardState RevealByPour()
        {
            return new BoardState(new[] { Murky(4, 0x1, 2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4) });
        }

        /// <summary>[3*, 2*, 1], [1, 1, 1], [2, 2, 2], [3, 3, 3], vacía.</summary>
        private static BoardState OneMurkyCuia()
        {
            return new BoardState(new[] { Murky(4, 0x3, 3, 2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2), C(4, 3, 3, 3), Cuia.Empty(4) });
        }

        /// <summary>
        /// Tablero de <see cref="LevelSession.RestoreRevealed"/>, con la cuia 0 del nivel 100 (m04_v1_n0100): [10*, 8*, 6]
        /// (ocultas [[0, 0], [0, 1]]), [6, 6, 6], [8, 8, 8], [10, 10], [10] y vacía, todas de capacidad 4 (08 §4.1). Ni ganado
        /// ni atascado: 0 → 1 sella el 6 y revela solo la capa 1 (R7); 4 → 3 no revela nada.
        /// </summary>
        private static BoardState Level100Board()
        {
            BoardBuildResult built = BoardBuilder.Build(
                new[] { 4, 4, 4, 4, 4, 4 },
                new[] { new[] { 10, 8, 6 }, new[] { 6, 6, 6 }, new[] { 8, 8, 8 }, new[] { 10, 10 }, new[] { 10 }, new int[0] },
                null,
                new[] { new[] { 0, 0 }, new[] { 0, 1 } });
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }

        [TestCase(PourMode.Run)]
        [TestCase(PourMode.Single)]
        public void T6_R7_ADoseRevealedByAPour_StaysVisibleAfterMarea_0421(PourMode mode)
        {
            // 04 §2.1: «lo único que no se revierte es el conocimiento: las unidades reveladas siguen visibles».
            BoardState initial = RevealByPour();
            var session = new LevelSession(initial, true, mode);
            Assert.That(session.HiddenCount, Is.EqualTo(1));

            Assert.That(session.TryPour(0, 1, out PourResult result), Is.True);
            Assert.That(result.Events.SourceRevealed, Is.True);
            Assert.That(result.RevealedMask, Is.EqualTo(1 << 0));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0x1));

            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 2, 1).Packed));
            Assert.That(session.Current[0].IsHiddenAt(0), Is.False);
            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(initial[0].IsHiddenAt(0), Is.True); // el tablero inicial no cambia
            Assert.That(session.Initial.Equals(initial), Is.True);
        }

        [Test]
        public void T6_InSingle_TheDoseIsRevealedWhenTheLastDoseOfTheRunLeaves_AndMareaKeepsIt()
        {
            // R10 (a) en single: el revelado llega cuando sale la última dosis de la racha; dos Mareas no lo ocultan.
            var initial = new BoardState(new[] { Murky(4, 0x1, 2, 1, 1), C(4, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4) });
            var session = new LevelSession(initial, true, PourMode.Single);

            Assert.That(session.TryPour(0, 1, out PourResult first), Is.True);
            Assert.That(first.RevealedMask, Is.EqualTo(0));
            Assert.That(session.TryPour(0, 1, out PourResult second), Is.True);
            Assert.That(second.RevealedMask, Is.EqualTo(1 << 0));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Undo(), Is.True);

            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 2, 1, 1).Packed));
        }

        [Test]
        public void T6_Restart_KeepsTheRevealedDoses_0427()
        {
            // 04 §2.7: Volver a la orilla restaura el tablero inicial con la misma asignación de capas ocultas y conserva las
            // capas reveladas (I4 con I5).
            BoardState initial = RevealByPour();
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.TryPour(0, 1, out _), Is.True);
            Assert.That(session.TryPour(2, 0, out _), Is.True);

            session.Restart();

            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 2, 1).Packed));
            Assert.That(session.Current.Equals(initial), Is.False);
            for (int i = 1; i < initial.Count; i++)
            {
                Assert.That(session.Current[i].Packed, Is.EqualTo(initial[i].Packed));
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void T6_Luciernaga_RevealsAllHiddenDosesOfOneCuia_ThenMareaKeepsThem_0425()
        {
            // 04 §2.5: la Luciérnaga revela todas las unidades ocultas de una cuia; «lo revelado permanece visible tras Marea
            // y tras Volver a la orilla». No entra en el historial (Marea no deshace comodines, 04 §2.1).
            BoardState initial = OneMurkyCuia();
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.TryPour(2, 4, out _), Is.True); // un vertido que no revela nada

            Assert.That(session.Reveal(0), Is.EqualTo(2));

            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0x3));
            Assert.That(session.AppliedPours, Is.EqualTo(1));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 3, 2, 1).Packed));
            Assert.That(session.Current[2].Packed, Is.EqualTo(initial[2].Packed));
            Assert.That(session.CanUndo, Is.False);

            session.Restart();
            Assert.That(session.Current[0].Packed, Is.EqualTo(C(4, 3, 2, 1).Packed));
            Assert.That(session.Reveal(0), Is.EqualTo(0));
        }

        [Test]
        public void T6_Pororoca_RevealAll_RevealsEveryHiddenDoseOfTheLevel_0424a()
        {
            // 04 §2.4 a: la ola revela todas las capas ocultas del nivel; permanecen reveladas.
            var initial = new BoardState(new[]
            {
                Murky(4, 0x3, 3, 2, 1), Murky(4, 0x1, 2, 1), C(4, 1, 1), C(4, 2, 2), C(4, 3, 3, 3), Cuia.Empty(4),
            });
            var session = new LevelSession(initial, true, PourMode.Run);

            Assert.That(session.RevealAll(), Is.EqualTo(3));

            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0x3));
            Assert.That(session.RevealedLayers(1), Is.EqualTo(0x1));
            Assert.That(session.RevealAll(), Is.EqualTo(0));
            Assert.That(session.TryPour(4, 5, out _), Is.True);
            Assert.That(session.Undo(), Is.True);
            session.Restart();
            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(initial.HiddenCount, Is.EqualTo(3));
        }

        [Test]
        public void T6_Reveal_OnACuiaWithoutHiddenDoses_ReturnsZero_AndChecksTheIndex()
        {
            var session = new LevelSession(OneMurkyCuia(), true, PourMode.Run);

            Assert.That(session.Reveal(1), Is.EqualTo(0));
            Assert.That(session.HiddenCount, Is.EqualTo(2));
            Assert.That(() => { session.Reveal(5); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { session.RevealedLayers(-1); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void T6_ARevealedMurkyCuia_PlaysLikeAVisibleOne()
        {
            // Revelar no cambia el contenido: solo la visibilidad. Tras revelar, la racha ya no se corta en las dosis que
            // estaban ocultas (R6) y el sellado (R5) las admite.
            var initial = new BoardState(new[] { Murky(4, 0x1, 1, 1, 1), C(4, 1), C(4, 2, 2, 2, 2), Cuia.Empty(4) });
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.Current[0].TopRunLength, Is.EqualTo(2));

            session.Reveal(0);

            Assert.That(session.Current[0].TopRunLength, Is.EqualTo(3));
            Assert.That(session.TryPour(1, 0, out PourResult result), Is.True);
            Assert.That(result.TargetSealed, Is.True);
        }

        [Test]
        public void Regression3_0451_HiddenMonocolorVisibleToEmpty_KeepsTheSessionPlaying()
        {
            // 04 §5.1, regresión (3): [2*, 1, 1, 1] → vacía no es fútil (verter revela información). Es el único vertido útil:
            // sin la excepción la partida empezaría en Aguas estancadas.
            var initial = new BoardState(new[] { Murky(4, 0x1, 2, 1, 1, 1), C(4, 2, 2, 2), Cuia.Empty(4) });
            Assert.That(PourRules.IsFutile(initial, 1, 2), Is.True);
            Assert.That(PourRules.IsFutile(initial, 0, 2), Is.False);

            var session = new LevelSession(initial, true, PourMode.Run);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.TryPour(0, 2, out PourResult result), Is.True);
            Assert.That(result.Pour.Amount, Is.EqualTo(3));
            Assert.That(result.RevealedMask, Is.EqualTo(1 << 0));
        }

        [Test]
        public void HiddenDosesWithARemolino_AreRejected()
        {
            // [P]: el Remolino inserta por el fondo y desplazaría las capas ocultas; la validación ya prohíbe la combinación.
            var initial = new BoardState(new[]
            {
                Murky(4, 0x1, 2, 1), C(4, 1, 1, 1), C(4, 2, 2, 2),
                Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0), Cuia.Empty(4).WithMechanic(MechanicType.Remolino, pairId: 0),
            });

            Assert.That(() => { new LevelSession(initial, true, PourMode.Run); }, Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void RestoreRevealed_PartialLayer_KeepsOtherHidden()
        {
            // 02 §11 T6 y R8: un vertido puede revelar solo una de las dos ocultas de la cuia 0 (nivel 100); al reanudar o al
            // rehacer la sesión se restaura esa capa y la otra sigue oculta. Solo cambia la visibilidad, no el contenido.
            BoardState initial = Level100Board();
            Assert.That(LevelValidation.Validate(initial), Is.Null);
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));

            Assert.That(session.RestoreRevealed(0, 0b10), Is.EqualTo(1));

            Assert.That(session.Current[0].HiddenMask, Is.EqualTo(0b01));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0b10));
            Assert.That(session.HiddenCount, Is.EqualTo(1));
            Assert.That(session.Current[0].Packed, Is.EqualTo(initial[0].WithRevealed(0b10).Packed));
            for (int i = 1; i < initial.Count; i++)
            {
                Assert.That(session.Current[i].Packed, Is.EqualTo(initial[i].Packed));
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.Initial.Equals(initial), Is.True); // el tablero inicial no cambia
            Assert.That(initial[0].HiddenMask, Is.EqualTo(0b11));

            // La capa restaurada ya es visible: el vertido que la deja arriba no la vuelve a revelar (R7).
            Assert.That(session.TryPour(0, 1, out PourResult result), Is.True);
            Assert.That(result.RevealedMask, Is.EqualTo(0));
            Assert.That(session.Current[0].HiddenMask, Is.EqualTo(0b01));
        }

        [Test]
        public void RestoreRevealed_SurvivesUndoAndRestart()
        {
            // I5 (02 T6; 04 §2.1 y §2.7): lo restaurado sigue visible tras Marea y tras Volver a la orilla, también en los
            // estados del historial guardados antes de restaurarlo.
            BoardState initial = Level100Board();
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.TryPour(4, 3, out PourResult pour), Is.True); // 10 sobre [10, 10]: no revela nada
            Assert.That(pour.RevealedMask, Is.EqualTo(0));

            Assert.That(session.RestoreRevealed(0, 0b10), Is.EqualTo(1));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current[0].HiddenMask, Is.EqualTo(0b01));
            Assert.That(session.Current[4].Packed, Is.EqualTo(initial[4].Packed));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0b10));

            Assert.That(session.TryPour(4, 3, out _), Is.True);
            session.Restart();

            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(session.Current[0].HiddenMask, Is.EqualTo(0b01));
            Assert.That(session.Current[0].Packed, Is.EqualTo(initial[0].WithRevealed(0b10).Packed));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0b10));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void RestoreRevealed_RejectsLayersNotHidden()
        {
            // 02 T6: solo se restauran dosis ocultas del tablero inicial; la cima nunca lo está (R7). Un rechazo no aplica
            // nada, ni siquiera los bits válidos de la máscara.
            BoardState initial = Level100Board();
            var session = new LevelSession(initial, true, PourMode.Run);

            Assert.That(() => { session.RestoreRevealed(0, 0b100); }, Throws.TypeOf<ArgumentException>()); // la cima
            Assert.That(() => { session.RestoreRevealed(0, 0b110); }, Throws.TypeOf<ArgumentException>()); // capa 1 y la cima
            Assert.That(() => { session.RestoreRevealed(0, 1 << 3); }, Throws.TypeOf<ArgumentException>()); // sin dosis
            Assert.That(() => { session.RestoreRevealed(0, 1 << 6); }, Throws.TypeOf<ArgumentException>()); // fuera de los 6 bits
            Assert.That(() => { session.RestoreRevealed(1, 0b1); }, Throws.TypeOf<ArgumentException>()); // cuia sin ocultas
            Assert.That(() => { session.RestoreRevealed(5, 0b1); }, Throws.TypeOf<ArgumentException>()); // cuia vacía
            Assert.That(() => { session.RestoreRevealed(0, -1); }, Throws.TypeOf<ArgumentException>());
            Assert.That(() => { session.RestoreRevealed(0, int.MinValue); }, Throws.TypeOf<ArgumentException>());

            Assert.That(session.RevealedLayers(0), Is.EqualTo(0));
            Assert.That(session.HiddenCount, Is.EqualTo(2));
            Assert.That(session.Current.Equals(initial), Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void RestoreRevealed_RejectsCuiaOutOfRange()
        {
            BoardState initial = Level100Board();
            var session = new LevelSession(initial, true, PourMode.Run);

            Assert.That(() => { session.RestoreRevealed(-1, 0b1); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { session.RestoreRevealed(initial.Count, 0b1); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { session.RestoreRevealed(initial.Count, -1); }, Throws.TypeOf<ArgumentOutOfRangeException>()); // el índice va primero

            Assert.That(session.HiddenCount, Is.EqualTo(2));
            Assert.That(session.Current.Equals(initial), Is.True);
        }

        [Test]
        public void RestoreRevealed_IsIdempotentAndOutsideHistory()
        {
            // Reanudar (02 §14, R8): se rehacen los vertidos y después se restaura la máscara guardada. Las capas que el
            // historial ya reveló (R7) no suman, y restaurar no entra en el historial de Marea (04 §2.1).
            BoardState initial = Level100Board();
            var session = new LevelSession(initial, true, PourMode.Run);
            Assert.That(session.TryPour(0, 1, out PourResult replayed), Is.True); // sella el 6 y revela la capa 1 (R7)
            Assert.That(replayed.RevealedMask, Is.EqualTo(1 << 0));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0b10));

            Assert.That(session.RestoreRevealed(0, 0b10), Is.EqualTo(0));
            Assert.That(session.RestoreRevealed(0, 0b11), Is.EqualTo(1));
            Assert.That(session.HiddenCount, Is.EqualTo(0));
            Assert.That(session.AppliedPours, Is.EqualTo(1));

            BoardState current = session.Current;
            LevelStatus status = session.Status;
            Assert.That(session.RestoreRevealed(0, 0b11), Is.EqualTo(0));
            Assert.That(session.RestoreRevealed(0, 0b01), Is.EqualTo(0));
            Assert.That(session.RestoreRevealed(0, 0), Is.EqualTo(0));
            Assert.That(session.Current, Is.SameAs(current));
            Assert.That(session.Status, Is.EqualTo(status));
            Assert.That(session.RevealedLayers(0), Is.EqualTo(0b11));
            Assert.That(session.AppliedPours, Is.EqualTo(1));

            // Marea deshace el vertido y nada más.
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.Undo(), Is.False);
            Assert.That(session.Current[0].Packed, Is.EqualTo(initial[0].WithRevealed(0b11).Packed));
            Assert.That(session.Current[1].Packed, Is.EqualTo(initial[1].Packed));
        }

        [Test]
        public void RestoreRevealed_RecomputesStatusLikeReveal_R10gh()
        {
            // R10 (g)-(h), como Reveal: [1*, 1, 1, 1] no sella mientras tenga una oculta (R5); al restaurar la capa 0 sella y
            // la partida queda ganada (R12). El resultado es el mismo que con la Luciérnaga (04 §2.5).
            var initial = new BoardState(new[] { Murky(4, 0x1, 1, 1, 1, 1), C(4, 2, 2, 2, 2), Cuia.Empty(4) });
            var restored = new LevelSession(initial, true, PourMode.Run);
            var revealed = new LevelSession(initial, true, PourMode.Run);
            Assert.That(restored.Status, Is.EqualTo(LevelStatus.Playing));

            Assert.That(restored.RestoreRevealed(0, 0x1), Is.EqualTo(1));
            Assert.That(revealed.Reveal(0), Is.EqualTo(1));

            Assert.That(restored.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(restored.Current.Equals(revealed.Current), Is.True);
            Assert.That(restored.Status, Is.EqualTo(revealed.Status));
        }

        [Test]
        public void RestoreRevealed_AtTheRevealPoint_ReplaysTheHistoryExactly()
        {
            // 02 R8 y §14, caso teórico: el paso 1 restaura la máscara al final del rehacer (README, «Restaurar las capas
            // reveladas»), y es exacto porque ningún nivel de 1-200 lleva Barro en una cuia turbia. Con Barro sobre la oculta, una
            // capa revelada a mitad del historial sin que el historial la revele (aquí, la Luciérnaga tras lavar el Barro con el
            // color de la oculta, 02 §9.1 M5) solo se reproduce restaurándola en ese punto: revelar alarga la racha (R6) y el
            // vertido siguiente mueve 2 dosis y no 1.
            var initial = new BoardState(new[]
            {
                Murky(4, 0x1, 3, Cuia.MudColor, 2), C(4, 2, 2, 2), C(4, 3), C(4, 3, 3), Cuia.Empty(4),
            });
            var played = new LevelSession(initial, true, PourMode.Run);
            Assert.That(played.TryPour(0, 1, out _), Is.True); // el 2 sella la cuia 1 y deja el Barro arriba
            Assert.That(played.TryPour(2, 0, out PourResult wash), Is.True); // el 3 lava el Barro: [3*, 3]
            Assert.That(wash.BarroWashed, Is.True);
            Assert.That(played.Reveal(0), Is.EqualTo(1));
            Assert.That(played.TryPour(0, 3, out PourResult two), Is.True);
            Assert.That(two.Pour.Amount, Is.EqualTo(2));

            var atTheEnd = new LevelSession(initial, true, PourMode.Run);
            Assert.That(atTheEnd.TryPour(0, 1, out _), Is.True);
            Assert.That(atTheEnd.TryPour(2, 0, out _), Is.True);
            Assert.That(atTheEnd.TryPour(0, 3, out PourResult one), Is.True);
            Assert.That(one.Pour.Amount, Is.EqualTo(1)); // sin la capa revelada, la racha se corta en la oculta

            var atThePoint = new LevelSession(initial, true, PourMode.Run);
            Assert.That(atThePoint.TryPour(0, 1, out _), Is.True);
            Assert.That(atThePoint.TryPour(2, 0, out _), Is.True);
            Assert.That(atThePoint.RestoreRevealed(0, played.RevealedLayers(0)), Is.EqualTo(1));
            Assert.That(atThePoint.TryPour(0, 3, out PourResult replayed), Is.True);
            Assert.That(replayed.Pour.Amount, Is.EqualTo(2));
            Assert.That(atThePoint.Current.Equals(played.Current), Is.True);
            Assert.That(atThePoint.Status, Is.EqualTo(played.Status));
            Assert.That(atThePoint.AppliedPours, Is.EqualTo(played.AppliedPours));
        }
    }
}
