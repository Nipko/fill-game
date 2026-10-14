#nullable enable
using System;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Modo de vertido (decisión del dueño D-005, docs/dev/decisiones.md, que sustituye a D-001): <see cref="PourMode.Run"/>,
    /// el modo por defecto, mueve la racha visible que cabe, con vertido parcial (02 R6, R9); en <see cref="PourMode.Single"/>,
    /// el modo de pruebas, cada vertido mueve exactamente una dosis aunque haya racha. Cubre la cantidad, la legalidad (que no cambia), los fútiles y el bloqueo con los ejemplos E1-E6 de
    /// decisiones.md §2.1.2, la partida y Marea. Los casos que valen en los dos modos están parametrizados: el caso
    /// <c>Run</c> reproduce lo que ya probaban PourRulesTests, FutileRulesTests y LevelSessionTests con las sobrecargas
    /// heredadas. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class PourModeTests
    {
        private static Cuia C(int capacity, params int[] colorsBottomToTop)
        {
            return Cuia.Create(capacity, colorsBottomToTop);
        }

        private static BoardState Board(params Cuia[] cuias)
        {
            return new BoardState(cuias);
        }

        /// <summary>Nivel 1 de 02 §13: [açaí ×3, aguaje], [aguaje ×3, açaí], vacía (índices de motor del ejemplo de v0).</summary>
        private static BoardState TutorialLevel1()
        {
            return Board(C(4, 8, 8, 8, 4), C(4, 4, 4, 4, 8), Cuia.Empty(4));
        }

        /// <summary>Tablero de LevelSessionTests: el vertido 0 → 3 lleva las dosis de 2 a la única vacía.</summary>
        private static BoardState OnePourFromStuck()
        {
            return Board(C(4, 2, 3, 2, 2), C(4, 3, 3, 3, 1), C(4, 2, 1, 1, 1), Cuia.Empty(4));
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

        /// <summary>Cuias que contienen <paramref name="color"/> (término de la dispersión h2 de 03 §5).</summary>
        private static int CuiasContaining(BoardState state, int color)
        {
            int found = 0;
            for (int i = 0; i < state.Count; i++)
            {
                for (int k = 0; k < state[i].Count; k++)
                {
                    if (state[i].ColorAt(k) == color)
                    {
                        found++;
                        break;
                    }
                }
            }

            return found;
        }

        // ---------------------------------------------------------------- PourModes (rules_pour_mode)

        [Test]
        public void D005_DefaultIsRun_AndIsTheZeroValue()
        {
            // D-005 (como Magic Sort): por defecto la racha que cabe; default(PourMode) es el modo por defecto.
            Assert.That(PourModes.Default, Is.EqualTo(PourMode.Run));
            Assert.That(default(PourMode), Is.EqualTo(PourMode.Run));
            Assert.That((int)PourMode.Run, Is.EqualTo(0));
            Assert.That((int)PourMode.Single, Is.EqualTo(1));
        }

        [TestCase("single", PourMode.Single)]
        [TestCase("run", PourMode.Run)]
        public void TryParse_RemoteConfigValues(string key, PourMode expected)
        {
            bool ok = PourModes.TryParse(key, out PourMode mode);

            Assert.That(ok, Is.True);
            Assert.That(mode, Is.EqualTo(expected));
            Assert.That(PourModes.ToKey(mode), Is.EqualTo(key));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Single")]
        [TestCase("RUN")]
        [TestCase(" run")]
        [TestCase("single ")]
        [TestCase("runs")]
        [TestCase("1")]
        public void TryParse_UnknownValue_ReturnsFalseAndTheDefault(string? key)
        {
            // Comparación ordinal y exacta; el llamante (GameConfig) decide el aviso en el log y usa el default (Run, D-005).
            bool ok = PourModes.TryParse(key, out PourMode mode);

            Assert.That(ok, Is.False);
            Assert.That(mode, Is.EqualTo(PourModes.Default));
            Assert.That(mode, Is.EqualTo(PourMode.Run));
        }

        [Test]
        public void UndefinedMode_IsRejected()
        {
            var undefined = (PourMode)2;
            BoardState state = TutorialLevel1();

            Assert.That(PourModes.IsDefined(PourMode.Single), Is.True);
            Assert.That(PourModes.IsDefined(PourMode.Run), Is.True);
            Assert.That(PourModes.IsDefined(undefined), Is.False);
            Assert.That(() => { PourModes.ToKey(undefined); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { PourRules.Amount(state, 0, 2, undefined); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { PourRules.Apply(state, 0, 2, undefined); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { PourRules.CollectUsefulMoves(state, true, undefined, new Pour[PourRules.MaxPours]); }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => { new LevelSession(state, true, undefined); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // ---------------------------------------------------------------- cantidad (R9; D-005: Run por defecto, Single de pruebas)

        [TestCase(PourMode.Single, 1)]
        [TestCase(PourMode.Run, 3)]
        public void R9_RunOfThreeIntoEmpty(PourMode mode, int expected)
        {
            // En Run (por defecto, D-005) sale la racha entera; en Single (pruebas), una sola dosis aunque debajo haya dos
            // más del mismo color.
            BoardState state = Board(C(4, 2, 1, 1, 1), Cuia.Empty(4));

            BoardState next = PourRules.Apply(state, 0, 1, mode);

            Assert.That(PourRules.Amount(state, 0, 1, mode), Is.EqualTo(expected));
            Assert.That(next[0].Count, Is.EqualTo(4 - expected));
            Assert.That(next[1].Count, Is.EqualTo(expected));
            Assert.That(next[1].IsMonocolor, Is.True);
            Assert.That(next[1].TopColor, Is.EqualTo(1));
        }

        [TestCase(PourMode.Single, 1)]
        [TestCase(PourMode.Run, 2)]
        public void R9_RunOfThreeOntoTwoHoles_PartialOnlyInRun(PourMode mode, int expected)
        {
            // Lección del nivel 2 (02 §13): en Run vierte lo que cabe (parcial, 2 de 3); en Single no existe el parcial.
            BoardState state = Board(C(4, 2, 1, 1, 1), C(4, 3, 1));

            BoardState next = PourRules.Apply(state, 0, 1, mode);

            Assert.That(PourRules.Amount(state, 0, 1, mode), Is.EqualTo(expected));
            Assert.That(next[0], Is.EqualTo(mode == PourMode.Run ? C(4, 2, 1) : C(4, 2, 1, 1)));
            Assert.That(next[1], Is.EqualTo(mode == PourMode.Run ? C(4, 3, 1, 1, 1) : C(4, 3, 1, 1)));
        }

        [Test]
        public void R9_RunLongerThanTheHole_MovesOneDoseInBothModes()
        {
            // Mismo resultado en los dos modos cuando solo cabe una dosis (Apply_PartialPour de PourRulesTests).
            BoardState state = Board(C(4, 2, 1, 1, 1), C(4, 3, 3, 1));

            BoardState single = PourRules.Apply(state, 0, 1, PourMode.Single);
            BoardState run = PourRules.Apply(state, 0, 1, PourMode.Run);

            Assert.That(single.Equals(run), Is.True);
            Assert.That(single[0], Is.EqualTo(C(4, 2, 1, 1)));
            Assert.That(single[1], Is.EqualTo(C(4, 3, 3, 1, 1)));
        }

        [Test]
        public void D001_Single_RepeatedPours_MoveOneDoseEachTime_AndAddUpToTheRunPour()
        {
            // decisiones.md A3: un vertido Run de k dosis = k vertidos Single legales y seguidos que llegan al mismo estado.
            BoardState start = Board(C(4, 2, 1, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2));
            BoardState run = PourRules.Apply(start, 0, 1, PourMode.Run);

            BoardState state = start;
            for (int i = 0; i < 3; i++)
            {
                Assert.That(PourRules.Amount(state, 0, 1, PourMode.Single), Is.EqualTo(1), "vertido " + i);
                state = PourRules.Apply(state, 0, 1, PourMode.Single);
                Assert.That(state[1].Count, Is.EqualTo(i + 1));
            }

            Assert.That(state.Equals(run), Is.True);
            Assert.That(PourRules.IsLegal(state, 0, 1), Is.False);
            Assert.That(PourRules.Amount(state, 0, 1, PourMode.Single), Is.EqualTo(0));
        }

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void R11_IllegalPairs_HaveAmountZero_AndApplyThrows(PourMode mode)
        {
            // La legalidad no depende del modo (decisiones.md §2.1.1): misma cuia, otra cima, destino lleno, origen sellado.
            BoardState state = Board(C(4, 1, 2), C(4, 2, 1), C(4, 3, 3, 3, 2), C(4, 4, 4, 4, 4), Cuia.Empty(4));

            Assert.That(PourRules.Amount(state, 0, 0, mode), Is.EqualTo(0));
            Assert.That(PourRules.Amount(state, 0, 1, mode), Is.EqualTo(0));
            Assert.That(PourRules.Amount(state, 1, 2, mode), Is.EqualTo(0));
            Assert.That(PourRules.Amount(state, 3, 4, mode), Is.EqualTo(0));
            Assert.That(PourRules.Amount(state, 4, 0, mode), Is.EqualTo(0));
            Assert.That(() => { PourRules.Apply(state, 0, 1, mode); }, Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => { PourRules.Amount(state, 0, 5, mode); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void LegalityAndAmount_OnEveryPair_SingleIsOneIffLegal_RunMatchesTheLegacyOverloads()
        {
            // decisiones.md A1 en un tablero con cuias de varias capacidades, ocultas y sellada.
            BoardState state = Board(
                C(4, 1, 2, 2, 2), C(4, 2, 1), Cuia.Create(4, new[] { 3, 1, 1 }, 0x1), C(2, 1), C(4, 4, 4, 4, 4),
                Cuia.Empty(4), Cuia.Empty(2), C(5, 3, 3, 2));
            for (int a = 0; a < state.Count; a++)
            {
                for (int b = 0; b < state.Count; b++)
                {
                    bool legal = PourRules.IsLegal(state, a, b);
                    int single = PourRules.Amount(state, a, b, PourMode.Single);
                    int run = PourRules.Amount(state, a, b, PourMode.Run);
                    string pair = a + "->" + b;

                    Assert.That(single, Is.EqualTo(legal ? 1 : 0), pair);
                    Assert.That(run, Is.EqualTo(PourRules.Amount(state, a, b)), pair);
                    Assert.That(run >= single, Is.True, pair);
                    if (legal)
                    {
                        Assert.That(PourRules.Apply(state, a, b, PourMode.Run).Equals(PourRules.Apply(state, a, b)), Is.True, pair);
                    }
                }
            }
        }

        [TestCase(PourMode.Single, 2, true)]
        [TestCase(PourMode.Run, 1, false)]
        public void R7_R10a_HiddenDoseIsRevealedWhenTheLastDoseOfTheVisibleRunLeaves(PourMode mode, int poursToReveal, bool hiddenAfterFirst)
        {
            // 02 R7 y R10 (a): la dosis oculta solo se revela al quedar arriba; en Single, uno o más vertidos después
            // (decisiones.md §2.1.1, fila R7/R10).
            BoardState state = Board(Cuia.Create(4, new[] { 3, 1, 1 }, 0x1), Cuia.Empty(4), C(4, 3, 3, 3, 1));

            BoardState first = PourRules.Apply(state, 0, 1, mode);
            Assert.That(first[0].HasHidden, Is.EqualTo(hiddenAfterFirst));

            BoardState revealed = first;
            for (int i = 1; i < poursToReveal; i++)
            {
                revealed = PourRules.Apply(revealed, 0, 1, mode);
            }

            Assert.That(revealed[0], Is.EqualTo(C(4, 3)));
            Assert.That(revealed[1], Is.EqualTo(C(4, 1, 1)));
        }

        [Test]
        public void CollectUsefulMoves_SamePairsInBothModes_AmountOneInSingle()
        {
            // decisiones.md A2: los pares (A, B) de movimientos_utiles no dependen del modo; solo Amount (1 en Single).
            BoardState state = Board(C(4, 2, 1, 1, 1), C(4, 3, 3, 1), Cuia.Empty(4));
            var single = new Pour[PourRules.MaxPours];
            var run = new Pour[PourRules.MaxPours];
            var legacy = new Pour[PourRules.MaxPours];

            int singleCount = PourRules.CollectUsefulMoves(state, true, PourMode.Single, single);
            int runCount = PourRules.CollectUsefulMoves(state, true, PourMode.Run, run);
            int legacyCount = PourRules.CollectUsefulMoves(state, true, legacy);

            Assert.That(singleCount, Is.EqualTo(3));
            Assert.That(runCount, Is.EqualTo(3));
            Assert.That(legacyCount, Is.EqualTo(3));
            Assert.That(single[0], Is.EqualTo(new Pour(0, 1, 1)));
            Assert.That(single[1], Is.EqualTo(new Pour(0, 2, 1)));
            Assert.That(single[2], Is.EqualTo(new Pour(1, 2, 1)));
            Assert.That(run[0], Is.EqualTo(new Pour(0, 1, 1)));
            Assert.That(run[1], Is.EqualTo(new Pour(0, 2, 3)));
            Assert.That(run[2], Is.EqualTo(new Pour(1, 2, 1)));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(legacy[i], Is.EqualTo(run[i]));
            }
        }

        [TestCase(true, 2)]
        [TestCase(false, 4)]
        public void CollectUsefulMoves_FutileRuleIsTheSameInBothModes(bool futileRuleEnabled, int expectedCount)
        {
            // 02 §1.4 y R16: el kill-switch quita o devuelve los mismos fútiles en los dos modos.
            BoardState state = Board(C(4, 1, 1, 2, 2), C(4, 2, 2), C(4, 1, 1), Cuia.Empty(4));
            var single = new Pour[PourRules.MaxPours];
            var run = new Pour[PourRules.MaxPours];

            int singleCount = PourRules.CollectUsefulMoves(state, futileRuleEnabled, PourMode.Single, single);
            int runCount = PourRules.CollectUsefulMoves(state, futileRuleEnabled, PourMode.Run, run);

            Assert.That(singleCount, Is.EqualTo(expectedCount));
            Assert.That(runCount, Is.EqualTo(expectedCount));
            for (int i = 0; i < expectedCount; i++)
            {
                Assert.That(single[i].Source, Is.EqualTo(run[i].Source));
                Assert.That(single[i].Target, Is.EqualTo(run[i].Target));
                Assert.That(single[i].Amount, Is.EqualTo(1));
                Assert.That(run[i].Amount, Is.EqualTo(2));
            }
        }

        // ---------------------------------------------------------------- fútiles (decisiones.md §2.1.2, E1-E6)

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void E1_SingleDoseMonocolorToEmpty_IsFutile_AndKeepsTheCanonicalState(PourMode mode)
        {
            // E1: A = [c] → B vacía. Fútil en los dos modos; ambos mueven 1 y R16 se cumple literalmente.
            BoardState state = Board(C(4, 1), C(4, 2, 1, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2));
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.True);

            BoardState after = PourRules.Apply(state, 0, 2, mode);

            Assert.That(PourRules.Amount(state, 0, 2, mode), Is.EqualTo(1));
            Assert.That(after.Equals(PourRules.Apply(state, 0, 2, PourMode.Single)), Is.True);
            Assert.That(SortedPacked(after), Is.EqualTo(SortedPacked(state)));
        }

        [Test]
        public void E2_MonocolorRunToEmpty_IsFutileInBothModes_SingleSplitsIt()
        {
            // E2: A = [c, c, c] → B vacía, capacidades iguales. Fútil en los dos modos. Run deja el estado canónico igual
            // (R16); Single parte la monocolor: A = [c, c], B = [c] y la dispersión de c sube 1 (retroceso).
            BoardState state = Board(C(4, 1, 1, 1), C(4, 2, 2, 2, 1), Cuia.Empty(4), C(4, 2));
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.True);

            BoardState run = PourRules.Apply(state, 0, 2, PourMode.Run);
            BoardState single = PourRules.Apply(state, 0, 2, PourMode.Single);

            Assert.That(SortedPacked(run), Is.EqualTo(SortedPacked(state)));
            Assert.That(single[0], Is.EqualTo(C(4, 1, 1)));
            Assert.That(single[2], Is.EqualTo(C(4, 1)));
            Assert.That(SortedPacked(single), Is.Not.EqualTo(SortedPacked(state)));
            Assert.That(CuiasContaining(single, 1), Is.EqualTo(CuiasContaining(state, 1) + 1));
        }

        [TestCase(PourMode.Single, 1, 2)]
        [TestCase(PourMode.Run, 2, 1)]
        public void E3_SmallCuiaToItsRightHome_IsNotFutile(PourMode mode, int amount, int poursHome)
        {
            // E3: Cuia chica (cap 2) = [c, c] → estándar vacía. Rama "casa correcta" (02 §1.4): no fútil en ningún modo; en
            // Single hacen falta dos vertidos para llevar las dos dosis, y el segundo (destino no vacío) tampoco es fútil.
            BoardState state = Board(C(2, 1, 1), C(2, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2, 2));
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.False);
            Assert.That(PourRules.Amount(state, 0, 2, mode), Is.EqualTo(amount));

            BoardState after = state;
            for (int i = 0; i < poursHome; i++)
            {
                Assert.That(PourRules.IsFutile(after, 0, 2), Is.False, "vertido " + i);
                after = PourRules.Apply(after, 0, 2, mode);
            }

            Assert.That(after[0].IsEmpty, Is.True);
            Assert.That(after[2], Is.EqualTo(C(4, 1, 1)));
            Assert.That(PourRules.IsStuck(after, true), Is.False);
        }

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void E4_RegressivePourOntoMatchingTop_IsUseful(PourMode mode)
        {
            // E4: A = [c, c, c] → B = [x, c]. B no está vacía: no es fútil y cuenta como útil para R13 en los dos modos.
            BoardState state = Board(C(4, 1, 1, 1), C(4, 2, 1), C(4, 2, 2, 2));
            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            Assert.That(PourRules.IsStuck(state, true), Is.False);

            BoardState after = PourRules.Apply(state, 0, 1, mode);

            Assert.That(after[0], Is.EqualTo(mode == PourMode.Run ? C(4, 1) : C(4, 1, 1)));
            Assert.That(after[1], Is.EqualTo(mode == PourMode.Run ? C(4, 2, 1, 1, 1) : C(4, 2, 1, 1)));
        }

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void E5_RunOverOtherColorToEmpty_IsNotFutile(PourMode mode)
        {
            // E5: A = [x, c, c] → B vacía. A no es monocolor: no fútil. En Single hace falta otro vertido para destapar x.
            BoardState state = Board(C(4, 2, 1, 1), C(4, 1, 1, 2, 2), Cuia.Empty(4), C(4, 2));
            Assert.That(PourRules.IsFutile(state, 0, 2), Is.False);

            BoardState after = PourRules.Apply(state, 0, 2, mode);

            Assert.That(after[0], Is.EqualTo(mode == PourMode.Run ? C(4, 2) : C(4, 2, 1)));
            Assert.That(after[0].TopColor, Is.EqualTo(mode == PourMode.Run ? 2 : 1));
        }

        [Test]
        public void E6_SingleCycleOfTwoPours_IsNeverStuck()
        {
            // E6: A = [c, c] y B = [c]. En Single A→B y B→A se alternan (ciclo de longitud 2); ninguno es fútil y los ciclos no
            // son Aguas estancadas (R14). En Run, A→B junta las tres dosis en un vertido.
            BoardState state = Board(C(4, 1, 1), C(4, 1), C(4, 2, 2, 2, 1), C(4, 2));
            var session = new LevelSession(state, true, PourMode.Single);

            Assert.That(PourRules.IsFutile(state, 0, 1), Is.False);
            Assert.That(PourRules.IsFutile(state, 1, 0), Is.False);
            Assert.That(session.TryPour(0, 1, out PourResult forth), Is.True);
            Assert.That(PourRules.IsFutile(session.Current, 1, 0), Is.False);
            Assert.That(session.TryPour(1, 0, out PourResult back), Is.True);

            Assert.That(forth.Pour, Is.EqualTo(new Pour(0, 1, 1)));
            Assert.That(back.Pour, Is.EqualTo(new Pour(1, 0, 1)));
            Assert.That(session.Current.Equals(state), Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.AppliedPours, Is.EqualTo(2));
            Assert.That(PourRules.Apply(state, 0, 1, PourMode.Run)[1], Is.EqualTo(C(4, 1, 1, 1)));
        }

        // ---------------------------------------------------------------- bloqueo (R13-R15)

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void R13_OnlyFutilePours_StartsStuck_InBothModes(PourMode mode)
        {
            // 02 R13 caso 2 (solo fútiles) y R15: la partida empieza en Aguas estancadas y rechaza el vertido legal.
            var session = new LevelSession(Board(C(5, 1, 1, 1, 1), Cuia.Empty(5), C(4, 2, 2, 2, 2)), true, mode);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(session.TryPour(0, 1, out _), Is.False);
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void R13_NoLegalPours_StartsStuck_InBothModes(PourMode mode)
        {
            var session = new LevelSession(Board(C(4, 1, 2, 1, 2), C(4, 2, 1, 2, 1)), true, mode);

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck));
        }

        [Test]
        public void R13_Single_ReachesStuckOnePourLaterThanRun()
        {
            // El mismo vertido 0 → 3 bloquea en Run (mueve las 2 dosis de 2) y en Single deja otra dosis por mover; el
            // segundo 0 → 3 bloquea. Marea desde Aguas estancadas devuelve una sola dosis (04 §2.1, R15).
            var run = new LevelSession(OnePourFromStuck(), true, PourMode.Run);
            var single = new LevelSession(OnePourFromStuck(), true, PourMode.Single);

            Assert.That(run.TryPour(0, 3, out PourResult runPour), Is.True);
            Assert.That(single.TryPour(0, 3, out PourResult firstSingle), Is.True);

            Assert.That(runPour.Pour, Is.EqualTo(new Pour(0, 3, 2)));
            Assert.That(runPour.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(firstSingle.Pour, Is.EqualTo(new Pour(0, 3, 1)));
            Assert.That(firstSingle.Status, Is.EqualTo(LevelStatus.Playing));
            BoardState afterFirst = single.Current;

            Assert.That(single.TryPour(0, 3, out PourResult secondSingle), Is.True);
            Assert.That(secondSingle.Status, Is.EqualTo(LevelStatus.Stuck));
            Assert.That(single.Current.Equals(run.Current), Is.True);

            Assert.That(single.Undo(), Is.True);
            Assert.That(single.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(single.Current, Is.SameAs(afterFirst));
            Assert.That(single.Current[3], Is.EqualTo(C(4, 2)));
        }

        // ---------------------------------------------------------------- LevelSession y Marea

        [Test]
        public void LevelSession_KeepsItsMode_LegacyConstructorIsRun()
        {
            Assert.That(new LevelSession(TutorialLevel1(), true, PourModes.Default).PourMode, Is.EqualTo(PourMode.Run));
            Assert.That(new LevelSession(TutorialLevel1(), true, PourMode.Single).PourMode, Is.EqualTo(PourMode.Single));
            Assert.That(new LevelSession(TutorialLevel1(), true, PourMode.Run).PourMode, Is.EqualTo(PourMode.Run));
            Assert.That(new LevelSession(TutorialLevel1(), true).PourMode, Is.EqualTo(PourMode.Run));
        }

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void R12_TutorialLevel1_ParPath_WinsInThreeInBothModes(PourMode mode)
        {
            // 02 §13 nivel 1 (par 3 en los dos modos): 1→2, 0→1, 2→0; cada vertido mueve una dosis.
            var session = new LevelSession(TutorialLevel1(), true, mode);

            Assert.That(session.TryPour(1, 2, out PourResult first), Is.True);
            Assert.That(session.TryPour(0, 1, out PourResult second), Is.True);
            Assert.That(session.TryPour(2, 0, out PourResult third), Is.True);

            Assert.That(first.Pour.Amount, Is.EqualTo(1));
            Assert.That(second.TargetSealed, Is.True);
            Assert.That(third.TargetSealed, Is.True);
            Assert.That(third.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.AppliedPours, Is.EqualTo(3));
        }

        [TestCase(PourMode.Single, 1, 5)]
        [TestCase(PourMode.Run, 3, 3)]
        public void D001_TryPour_MovesOneDoseEvenWithARun(PourMode mode, int thirdAmount, int poursToWin)
        {
            // Secuencia de LevelSessionTests (0→2, 1→0, 1→2): el tercer vertido lleva la racha [4, 4, 4]. En Run gana en 3;
            // en Single mueve una dosis por vertido y hacen falta dos 1→2 más.
            var session = new LevelSession(TutorialLevel1(), true, mode);
            session.TryPour(0, 2, out _);
            session.TryPour(1, 0, out _);

            Assert.That(session.TryPour(1, 2, out PourResult third), Is.True);
            Assert.That(third.Pour, Is.EqualTo(new Pour(1, 2, thirdAmount)));
            while (session.Status == LevelStatus.Playing)
            {
                Assert.That(session.TryPour(1, 2, out PourResult more), Is.True);
                Assert.That(more.Pour.Amount, Is.EqualTo(1));
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(session.AppliedPours, Is.EqualTo(poursToWin));
        }

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void I2_IllegalPour_ReturnsFalseAndChangesNothing(PourMode mode)
        {
            var session = new LevelSession(TutorialLevel1(), true, mode);
            BoardState before = session.Current;

            Assert.That(session.TryPour(0, 1, out PourResult result), Is.False);
            Assert.That(result.Pour, Is.EqualTo(default(Pour)));
            Assert.That(session.Current, Is.SameAs(before));
            Assert.That(session.AppliedPours, Is.EqualTo(0));
        }

        [Test]
        public void I3_Single_UndoReturnsOneDosePerUse()
        {
            // 04 §2.1 en Single (modo de pruebas desde D-005): tres vertidos iguales de una dosis se deshacen de uno en uno,
            // al estado anterior exacto. La cuia [1] completa U_1 = 4 para que el tablero se gane y R13 ampliada (D-014) no lo
            // declare a mitad de la prueba.
            BoardState start = Board(C(4, 2, 1, 1, 1), Cuia.Empty(4), C(4, 2, 2, 2), C(4, 1));
            var session = new LevelSession(start, true, PourMode.Single);
            var states = new BoardState[4];
            states[0] = session.Current;
            for (int i = 1; i <= 3; i++)
            {
                Assert.That(session.TryPour(0, 1, out PourResult pour), Is.True);
                Assert.That(pour.Pour, Is.EqualTo(new Pour(0, 1, 1)));
                states[i] = session.Current;
            }

            Assert.That(session.Current[1], Is.EqualTo(C(4, 1, 1, 1)));
            for (int i = 3; i >= 1; i--)
            {
                Assert.That(session.Undo(), Is.True);
                Assert.That(session.Current, Is.SameAs(states[i - 1]));
                Assert.That(session.AppliedPours, Is.EqualTo(i - 1));
                Assert.That(session.Current[1].Count, Is.EqualTo(i - 1));
            }

            Assert.That(session.Undo(), Is.False);
            Assert.That(session.Current.Equals(start), Is.True);
        }

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void I3_I4_UndoAndRestart_InBothModes(PourMode mode)
        {
            var session = new LevelSession(TutorialLevel1(), true, mode);
            session.TryPour(1, 2, out _);
            BoardState beforeSeal = session.Current;
            session.TryPour(0, 1, out PourResult sealing);
            Assert.That(sealing.TargetSealed, Is.True);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Current, Is.SameAs(beforeSeal));
            Assert.That(session.Current.IsSealed(1), Is.False);
            Assert.That(session.AppliedPours, Is.EqualTo(1));

            session.Restart();

            Assert.That(session.Current.Equals(session.Initial), Is.True);
            Assert.That(session.AppliedPours, Is.EqualTo(0));
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(session.PourMode, Is.EqualTo(mode));
        }

        [TestCase(PourMode.Single)]
        [TestCase(PourMode.Run)]
        public void R8_ReplayingTheHistoryWithTheSameMode_RebuildsCurrent(PourMode mode)
        {
            // 02 R8: el historial reconstruye el estado con el mismo modo. Rehacerlo con el otro modo da otro tablero: por
            // eso el modo se fija al cargar el nivel (punto 3 de D-001, sustituida por D-005, que lo conserva; riesgo R-2).
            var session = new LevelSession(TutorialLevel1(), true, mode);
            session.TryPour(0, 2, out _);
            session.TryPour(1, 0, out _);
            session.TryPour(1, 2, out _);
            PourMode other = mode == PourMode.Single ? PourMode.Run : PourMode.Single;

            BoardState rebuilt = session.Initial;
            BoardState otherMode = session.Initial;
            for (int i = 0; i < session.AppliedPours; i++)
            {
                Pour pour = session.GetAppliedPour(i);
                Assert.That(PourRules.Amount(rebuilt, pour.Source, pour.Target, mode), Is.EqualTo(pour.Amount));
                rebuilt = PourRules.Apply(rebuilt, pour.Source, pour.Target, mode);
                otherMode = PourRules.Apply(otherMode, pour.Source, pour.Target, other);
            }

            Assert.That(rebuilt.Equals(session.Current), Is.True);
            Assert.That(otherMode.Equals(session.Current), Is.False);
        }

        [Test]
        public void HiddenDoses_AreAcceptedInBothModes()
        {
            // C2-1: Aguas turbias en la partida (02 §11) en los dos modos de vertido.
            var board = Board(Cuia.Create(4, new[] { 1, 2, 1 }, 0x1), C(4, 2, 2, 1, 1), Cuia.Empty(4));

            Assert.That(new LevelSession(board, true, PourMode.Single).HiddenCount, Is.EqualTo(1));
            Assert.That(new LevelSession(board, true, PourMode.Run).HiddenCount, Is.EqualTo(1));
        }
    }
}
