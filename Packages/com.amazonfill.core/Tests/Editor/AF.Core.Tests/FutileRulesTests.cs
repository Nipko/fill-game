#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Vertidos fútiles, movimientos útiles y bloqueo: GDD 02 §1.4 (R13, R14, R16) y 04 §5.1 (función única
    /// <c>futil</c>, excepción de Aguas turbias y tests de regresión 1-3). Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class FutileRulesTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        [Test]
        public void IsFutile_MonocolorToEmptyWithSameCapacity_IsTrue()
        {
            // 04 §5.1 test (2): cuia estándar con 2 dosis de c → otra estándar vacía es fútil (cap_A = cap_B = U_c).
            BoardState state = Board(C(4, 1, 1), C(4, 2, 2, 1, 1), Cuia.Empty(4), C(4, 2, 2));

            Assert.That(PourRules.IsLegal(state, 0, 2), Is.True);
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.True);
        }

        [Test]
        public void IsFutile_SmallCuiaToItsRightHome_IsFalse()
        {
            // 02 §1.4 rama "casa correcta" y 04 §5.1 test (1): cap_A = 2 ≠ U_c = 4 y cap_B = 4 = U_c.
            BoardState state = Board(C(2, 1, 1), C(2, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2, 2));

            Assert.That(PourRules.IsFutile(state, 0, 2), Is.False);
        }

        [Test]
        public void IsFutile_FromRightHomeToSmallCuia_IsTrue()
        {
            // 02 §1.4: A puede sellar c (cap_A = U_c), así que no estamos en la rama "casa correcta".
            BoardState state = Board(C(4, 1, 1), C(2, 1, 1), Cuia.Empty(2), C(4, 2, 2, 2, 2));

            Assert.That(PourRules.IsFutile(state, 0, 2), Is.True);
        }

        [Test]
        public void IsFutile_BetweenTwoWrongHomes_IsTrue()
        {
            // 02 §1.4: ni A (cap 3) ni B (cap 2) pueden sellar c (U_c = 4): solo recoloca.
            BoardState state = Board(C(3, 1, 1), C(4, 1, 1), Cuia.Empty(2), C(4, 2, 2, 2, 2));

            Assert.That(PourRules.IsFutile(state, 0, 2), Is.True);
        }

        [Test]
        public void IsFutile_MonocolorWithHiddenDoses_IsFalse()
        {
            // 04 §5.1 test (3): con dosis ocultas el jugador no sabe si es monocolor; verter revela información.
            BoardState state = Board(Cuia.Create(4, new[] { 1, 1, 1 }, 0x1), C(4, 2, 2, 2, 1), Cuia.Empty(4), C(4, 2));

            Assert.That(state[0].IsMonocolor, Is.True);
            Assert.That(PourRules.IsLegal(state, 0, 2), Is.True);
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.False);
        }

        [Test]
        public void IsFutile_NotMonocolorSource_IsFalse()
        {
            BoardState state = Board(C(4, 1, 2), C(4, 2, 2, 2, 1), Cuia.Empty(4), C(4, 1, 1));

            Assert.That(PourRules.IsFutile(state, 0, 2), Is.False);
        }

        [Test]
        public void IsFutile_NonEmptyTarget_IsFalse()
        {
            BoardState state = Board(C(4, 1, 1), C(4, 2, 2, 1), Cuia.Empty(4), C(4, 2, 2));

            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
        }

        [Test]
        public void IsFutile_EmptySourceOrSameCuia_IsFalse()
        {
            BoardState state = Board(Cuia.Empty(4), Cuia.Empty(4), C(4, 1, 1, 1, 1));

            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            Assert.That(PourRules.IsFutile(state, 0, 0), Is.False);
        }

        [Test]
        public void IsFutile_IndexOutOfBoard_Throws()
        {
            BoardState state = Board(C(4, 1), Cuia.Empty(4));

            Assert.That(() => { PourRules.IsFutile(state, 0, 2); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CollectUsefulMoves_ExcludesFutiles_InAscendingOrder()
        {
            // movimientos_utiles(S) = { (A, B) : legal(A, B) y no futil(A, B) } (02 §1.4), orden (A, B) ascendente.
            BoardState state = Board(C(4, 1, 1, 2, 2), C(4, 2, 2), C(4, 1, 1), Cuia.Empty(4));
            var buffer = new Pour[PourRules.MaxPours];

            int count = PourRules.CollectUsefulMoves(state, true, buffer);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(new Pour(0, 1, 2)));
            Assert.That(buffer[1], Is.EqualTo(new Pour(0, 3, 2)));
        }

        [Test]
        public void CollectUsefulMoves_WithKillSwitchOff_ReturnsEveryLegalPour()
        {
            // R16: futile_rule_enabled = false vuelve al criterio clásico "sin vertidos legales".
            BoardState state = Board(C(4, 1, 1, 2, 2), C(4, 2, 2), C(4, 1, 1), Cuia.Empty(4));
            var buffer = new Pour[PourRules.MaxPours];

            int count = PourRules.CollectUsefulMoves(state, false, buffer);

            Assert.That(count, Is.EqualTo(4));
            Assert.That(buffer[0], Is.EqualTo(new Pour(0, 1, 2)));
            Assert.That(buffer[1], Is.EqualTo(new Pour(0, 3, 2)));
            Assert.That(buffer[2], Is.EqualTo(new Pour(1, 3, 2)));
            Assert.That(buffer[3], Is.EqualTo(new Pour(2, 3, 2)));
        }

        [Test]
        public void CollectUsefulMoves_AmountIsTheWholeRunThatFits()
        {
            // R9: cantidad = min(racha visible, espacio); el vertido parcial también es un movimiento útil.
            BoardState state = Board(C(4, 2, 1, 1, 1), C(4, 3, 3, 1), Cuia.Empty(4));
            var buffer = new Pour[PourRules.MaxPours];

            int count = PourRules.CollectUsefulMoves(state, true, buffer);

            // 1 → 0 no es legal: 0 está llena.
            Assert.That(count, Is.EqualTo(3));
            Assert.That(buffer[0], Is.EqualTo(new Pour(0, 1, 1)));
            Assert.That(buffer[1], Is.EqualTo(new Pour(0, 2, 3)));
            Assert.That(buffer[2], Is.EqualTo(new Pour(1, 2, 1)));
        }

        [Test]
        public void CollectUsefulMoves_BufferSmallerThanPairs_Throws()
        {
            BoardState state = Board(C(4, 1, 2), Cuia.Empty(4), Cuia.Empty(4));

            Assert.That(() => { PourRules.CollectUsefulMoves(state, true, new Pour[5]); }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void R13_NoLegalPours_IsStuck()
        {
            // 02 R13, caso 1 de research/01 §7: sin vertidos legales.
            BoardState state = Board(C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1));

            Assert.That(PourRules.HasUsefulMove(state, true), Is.False);
            Assert.That(PourRules.IsStuck(state, true), Is.True);
            Assert.That(PourRules.IsStuck(state, false), Is.True);
        }

        [Test]
        public void R13_OnlyFutilePours_IsStuck_UnlessKillSwitchIsOff()
        {
            // 02 R13, caso 2: solo fútiles. Cuia alta de 5 con las 4 dosis de c (U_c = 4) y otra alta vacía:
            // el vertido es legal pero fútil (ninguna de las dos puede sellar c).
            BoardState state = Board(C(5, 1, 1, 1, 1), Cuia.Empty(5), C(4, 2, 2, 2, 2));

            Assert.That(PourRules.IsLegal(state, 0, 1), Is.True);
            Assert.That(PourRules.IsFutile(state, 0, 1), Is.True);
            Assert.That(PourRules.IsStuck(state, true), Is.True);
            Assert.That(PourRules.IsStuck(state, false), Is.False);
        }

        [Test]
        public void SmallCuiaToStandardEmpty_IsNotStuck()
        {
            // 04 §5.1 test de regresión (1): Cuia chica de 2 con 2 dosis de c (U_c = 4), una estándar vacía y el
            // resto sin destino legal → legalNonFutileMoves ≠ ∅ y atascado = false.
            BoardState state = Board(C(2, 1, 1), C(2, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2, 2));
            var buffer = new Pour[PourRules.MaxPours];

            int count = PourRules.CollectUsefulMoves(state, true, buffer);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer[0], Is.EqualTo(new Pour(0, 2, 2)));
            Assert.That(buffer[1], Is.EqualTo(new Pour(1, 2, 2)));
            Assert.That(PourRules.IsStuck(state, true), Is.False);
        }

        [Test]
        public void HiddenMonocolorToEmpty_KeepsTheBoardAlive()
        {
            // 04 §5.1 test de regresión (3) en un tablero: el único vertido es "monocolor → vacía" desde una cuia
            // turbia (llena, U_c = 4, sin sellar por la dosis oculta). Sin la excepción sería fútil y habría bloqueo.
            BoardState state = Board(Cuia.Create(4, new[] { 1, 1, 1, 1 }, 0x1), C(4, 2, 2, 2, 2), Cuia.Empty(4));
            var buffer = new Pour[PourRules.MaxPours];

            int count = PourRules.CollectUsefulMoves(state, true, buffer);

            Assert.That(state.IsSealed(0), Is.False);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(buffer[0], Is.EqualTo(new Pour(0, 2, 3)));
            Assert.That(PourRules.IsStuck(state, true), Is.False);
        }

        [Test]
        public void R14_WonBoard_IsNeverStuck()
        {
            // R10 (e) antes que (f): una victoria no tiene vertidos útiles, pero no es bloqueo.
            BoardState state = Board(C(4, 1, 1, 1, 1), Cuia.Empty(4));

            Assert.That(PourRules.HasUsefulMove(state, true), Is.False);
            Assert.That(PourRules.IsStuck(state, true), Is.False);
        }

        [Test]
        public void R16_FutilePour_KeepsTheCanonicalState()
        {
            // 02 R16 / invariante I5: con capacidades iguales un vertido fútil conserva el multiconjunto de Packed.
            BoardState state = Board(C(4, 1, 1, 2, 2), C(4, 2, 2), C(4, 1, 1), Cuia.Empty(4));
            Assert.That(PourRules.IsFutile(state, 1, 3), Is.True);

            BoardState after = PourRules.Apply(state, 1, 3);

            Assert.That(after.Equals(state), Is.False);
            Assert.That(after.CanonicalHash(), Is.EqualTo(state.CanonicalHash()));
            Assert.That(SortedPacked(after), Is.EqualTo(SortedPacked(state)));
        }

        // Regresiones obligatorias de 04 §5.1 sobre BoardRules, la regla de tablero del anexo B (D-007, Q13). La (4),
        // "monocolor → vacía bajo Lluvia con gotas pendientes, o conectada a Remolino, no es fútil" (anexo B), queda para
        // las tareas que portan esas mecánicas (C2-4 Remolino y C3-1 Lluvia).

        [Test]
        public void BoardRules_Regression1_SmallCuiaToStandardEmpty_IsNotStuck()
        {
            // 04 §5.1 (1): Cuia chica de 2 con 2 dosis de c (U_c = 4) → cuia estándar vacía: útil, no hay bloqueo.
            var cuias = new[] { C(2, 1, 1), C(2, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2, 2) };
            int[] units = Board(cuias).UnitsTable;

            Assert.That(BoardRules.IsLegal(cuias, 0, cuias.Length, units, 0, 2), Is.True);
            Assert.That(BoardRules.IsFutile(cuias, 0, cuias.Length, units, 0, 2), Is.False);
            Assert.That(BoardRules.HasUsefulMove(cuias, 0, cuias.Length, units, true), Is.True);
            Assert.That(BoardRules.IsStuck(cuias, 0, cuias.Length, units, true), Is.False);
        }

        [Test]
        public void BoardRules_Regression2_StandardMonocolorToStandardEmpty_IsFutile()
        {
            // 04 §5.1 (2): cuia estándar con 2 dosis de c → otra estándar vacía (cap_A = cap_B = U_c) es fútil.
            var cuias = new[] { C(4, 1, 1), C(4, 2, 2, 1, 1), Cuia.Empty(4), C(4, 2, 2) };
            int[] units = Board(cuias).UnitsTable;

            Assert.That(BoardRules.IsLegal(cuias, 0, cuias.Length, units, 0, 2), Is.True);
            Assert.That(BoardRules.IsFutile(cuias, 0, cuias.Length, units, 0, 2), Is.True);
        }

        [Test]
        public void BoardRules_Regression3_HiddenMonocolorToEmpty_IsNotFutile()
        {
            // 04 §5.1 (3): una cuia monocolor visible con dosis ocultas → vacía no es fútil (verter revela información).
            var cuias = new[] { Cuia.Create(4, new[] { 1, 1, 1, 1 }, 0x1), C(4, 2, 2, 2, 2), Cuia.Empty(4) };
            int[] units = Board(cuias).UnitsTable;

            Assert.That(BoardRules.IsFutile(cuias, 0, cuias.Length, units, 0, 2), Is.False);
            Assert.That(BoardRules.IsStuck(cuias, 0, cuias.Length, units, true), Is.False);
        }

        [Test]
        public void R13_IsCorrectButNotComplete_TwoUsefulPoursCanReturnToTheSameCanonicalState()
        {
            // Anexo B, «Pruebas»: el test literal «no existe un ciclo de vertidos útiles que vuelva al mismo estado canónico»
            // es falso ya en el juego base. A = [1, 2, 2, 2] y B = [3, 3, 2] (cap 4): A→B mueve 1 dosis, B→A mueve 1, los dos
            // son útiles y el tablero vuelve al inicial. R13 es correcto (no declara bloqueo con progreso posible: P1 de
            // DotnetOnly/FutilePropertyTests) pero no completo; las propiedades P1-P3 sustituyen a ese test.
            BoardState start = Board(C(4, 1, 2, 2, 2), C(4, 3, 3, 2));

            Assert.That(PourRules.IsFutile(start, 0, 1), Is.False);
            BoardState middle = PourRules.Apply(start, 0, 1, PourMode.Run);
            Assert.That(PourRules.IsFutile(middle, 1, 0), Is.False);
            BoardState back = PourRules.Apply(middle, 1, 0, PourMode.Run);

            Assert.That(middle.CanonicalHash(), Is.Not.EqualTo(start.CanonicalHash()));
            Assert.That(back.CanonicalHash(), Is.EqualTo(start.CanonicalHash()));
            Assert.That(back.Equals(start), Is.True);
        }

        [Test]
        public void BoardRules_AgreesWithPourRules_OnEveryPairOfTheseBoards()
        {
            // PourRules delega en BoardRules: mismas respuestas de legal, cantidad y futil (y CuiaRules, el envoltorio
            // heredado, coincide en tableros sin mecánicas).
            BoardState[] boards =
            {
                Board(C(4, 1, 1, 2, 2), C(4, 2, 2), C(4, 1, 1), Cuia.Empty(4)),
                Board(C(2, 1, 1), C(3, 1), Cuia.Empty(4), C(4, 2, 2, 2, 2), Cuia.Empty(2)),
                Board(Cuia.Create(4, new[] { 3, 1, 1 }, 0x1), C(5, 1, 1), Cuia.Empty(4), C(4, 3, 3, 3)),
            };
            foreach (BoardState state in boards)
            {
                for (int a = 0; a < state.Count; a++)
                {
                    for (int b = 0; b < state.Count; b++)
                    {
                        if (a == b)
                        {
                            continue;
                        }

                        bool legal = CuiaRules.IsLegal(state[a], state[b], state.UnitsTable);
                        Assert.That(PourRules.IsLegal(state, a, b), Is.EqualTo(legal));
                        Assert.That(PourRules.IsFutile(state, a, b), Is.EqualTo(CuiaRules.IsFutile(state[a], state[b], state.UnitsTable)));
                        if (legal)
                        {
                            Assert.That(PourRules.Amount(state, a, b, PourMode.Run), Is.EqualTo(CuiaRules.Amount(state[a], state[b], PourMode.Run)));
                        }
                    }
                }
            }
        }

        private static ulong[] SortedPacked(BoardState state)
        {
            var packed = new ulong[state.Count];
            for (int i = 0; i < packed.Length; i++)
            {
                packed[i] = state[i].Packed;
            }

            Array.Sort(packed);
            return packed;
        }
    }
}
