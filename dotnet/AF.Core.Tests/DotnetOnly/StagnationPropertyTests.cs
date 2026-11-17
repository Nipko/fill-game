#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Propiedades de Aguas estancadas por repetición (02 R13 ampliada; añadido de D-014 del 2026-11-06, punto 1 del añadido de
    /// D-059 del 2026-10-31), en partidas al azar con vertidos y Marea sobre los tableros de referencia y el 26 y el 66:
    /// <list type="bullet">
    /// <item><description>nunca Stuck (por el cierre) si queda un vertido legal a un tablero no visto en el intento;</description></item>
    /// <item><description>siempre Stuck si el cierre da el tablero por perdido y todo vertido legal lleva a un tablero visto;</description></item>
    /// <item><description>Volver a la orilla olvida lo visto; Marea resta 1 al tablero que deshace (añadido de D-014 del
    /// 2026-11-17), y <see cref="LevelSession.UndoEscapes"/> dice antes si la partida quedará en Playing.</description></item>
    /// </list>
    /// El oráculo (<see cref="StagnationOracle"/>) lleva su propio conjunto de vistos. Solo dotnet: lotes grandes.
    /// </summary>
    [TestFixture]
    public sealed class StagnationPropertyTests
    {
        private const int PlayoutsPerBoard = 4;
        private const int StepsPerPlayout = 120;

        private static IEnumerable<TestCaseData> Boards()
        {
            yield return new TestCaseData("nivel 26", PourMode.Run);
            yield return new TestCaseData("nivel 26", PourMode.Single);
            yield return new TestCaseData("nivel 66", PourMode.Run);
            yield return new TestCaseData("nivel 66 final", PourMode.Run);
            yield return new TestCaseData("referencia", PourMode.Run);
            yield return new TestCaseData("referencia", PourMode.Single);
        }

        [TestCaseSource(nameof(Boards))]
        public void R13b_StuckIffLostAndEveryPourRepeats(string set, PourMode mode)
        {
            var starts = new List<KeyValuePair<string, BoardState>>();
            switch (set)
            {
                case "nivel 26":
                    starts.Add(new KeyValuePair<string, BoardState>(set, Level26Board.Lost()));
                    break;
                case "nivel 66":
                    starts.Add(new KeyValuePair<string, BoardState>(set, Level66Board.Initial()));
                    break;
                case "nivel 66 final":
                    starts.Add(new KeyValuePair<string, BoardState>(set, Level66Board.Final()));
                    break;
                default:
                    for (int i = 0; i < ReferenceBoards.All.Length; i++)
                    {
                        starts.Add(new KeyValuePair<string, BoardState>(ReferenceBoards.All[i].Name, ReferenceBoards.All[i].State));
                    }

                    break;
            }

            int stuckByLoop = 0;
            int lostButPlaying = 0;
            for (int s = 0; s < starts.Count; s++)
            {
                string name = starts[s].Key;
                Play(starts[s].Value, mode, Prng.SeedFrom("amazonfill:stagnation:" + name + ":" + mode), name, ref stuckByLoop, ref lostButPlaying);
            }

            TestContext.WriteLine(set + " en " + mode + ": " + stuckByLoop.ToString(CultureInfo.InvariantCulture)
                + " estados en Aguas estancadas por repetición, " + lostButPlaying.ToString(CultureInfo.InvariantCulture)
                + " perdidos que siguen en Playing por un vertido a un tablero no visto");
            if (set != "referencia")
            {
                Assert.That(stuckByLoop, Is.GreaterThan(0));
                Assert.That(lostButPlaying, Is.GreaterThan(0));
            }
        }

        [TestCase("nivel 66 final", PourMode.Run)]
        [TestCase("nivel 26", PourMode.Run)]
        [TestCase("nivel 26", PourMode.Single)]
        public void R13b_UndoEscapes_PredictsMarea_AndWithHistoryMareaAlwaysEscapes(string set, PourMode mode)
        {
            // Opción 3 del dueño (añadido de D-014 del 2026-11-17): en todas las historias de hasta MaxDepth vertidos (en
            // profundidad, con Marea para volver), cada vez que la partida está en Aguas estancadas, UndoEscapes coincide con lo que
            // deja Marea. Con el contador de la opción 1, Marea deshace exactamente lo que el vertido sumó, así que deja los vistos
            // como estaban antes de ese vertido, y entonces la partida estaba en Playing (solo se vierte en Playing): sin dosis
            // ocultas, con historial, Marea siempre saca del atasco. UndoEscapes solo es false sin historial (tras una Pororoca, o
            // al reanudar con ResumeStuck al principio) o si lo revelado cambia el tablero al que se vuelve (Aguas turbias).
            BoardState start = set == "nivel 26" ? Level26Board.Lost() : Level66Board.Final();
            var session = new LevelSession(start, true, mode);
            int escapes = 0;
            int traps = 0;
            Explore(session, mode, 0, ref escapes, ref traps);
            TestContext.WriteLine(set + " en " + mode + ": " + escapes.ToString(CultureInfo.InvariantCulture) + " bloqueos de los que Marea saca, "
                + traps.ToString(CultureInfo.InvariantCulture) + " de los que no");
            Assert.That(session.Current.Equals(start), Is.True);
            Assert.That(session.SeenBoardCount, Is.EqualTo(1), "Marea deshizo todo y olvidó lo deshecho");
            Assert.That(escapes, Is.GreaterThan(0));
            Assert.That(traps, Is.EqualTo(0), "Marea deja los vistos como antes del vertido");
        }

        private const int MaxDepth = 7;

        private static void Explore(LevelSession session, PourMode mode, int depth, ref int escapes, ref int traps)
        {
            if (session.Status == LevelStatus.Stuck)
            {
                if (!session.CanUndo)
                {
                    return;
                }

                bool predicted = session.UndoEscapes;
                Assert.That(session.Undo(), Is.True);
                bool playing = session.Status == LevelStatus.Playing;
                Assert.That(predicted, Is.EqualTo(playing), "UndoEscapes en profundidad " + depth.ToString(CultureInfo.InvariantCulture));
                if (playing)
                {
                    escapes++;
                }
                else
                {
                    traps++;
                }

                return; // el llamante rehace el vertido con su propio Undo: aquí ya se deshizo
            }

            if (depth == MaxDepth || session.Status != LevelStatus.Playing)
            {
                return;
            }

            int count = session.Current.Count;
            for (int a = 0; a < count; a++)
            {
                for (int b = 0; b < count; b++)
                {
                    if (session.Status != LevelStatus.Playing || a == b || !PourRules.IsLegal(session.Current, a, b))
                    {
                        continue;
                    }

                    BoardState before = session.Current;
                    int pours = session.AppliedPours;
                    Assert.That(session.TryPour(a, b, out _), Is.True);
                    Explore(session, mode, depth + 1, ref escapes, ref traps);
                    if (session.AppliedPours > pours)
                    {
                        Assert.That(session.Undo(), Is.True);
                    }

                    Assert.That(session.Current.Equals(before), Is.True);
                }
            }
        }

        /// <summary>
        /// Partidas al azar: en cada paso, vertido legal (2/3) o Marea (1/3); desde Aguas estancadas, Marea o Volver a la orilla.
        /// Tras cada cambio, la partida tiene que coincidir con el oráculo.
        /// </summary>
        private static void Play(BoardState start, PourMode mode, ulong seed, string name, ref int stuckByLoop, ref int lostButPlaying)
        {
            var rng = new Prng(seed);
            var probe = new DeadEndProbe();
            var legal = new Pour[PourRules.MaxPours];
            for (int playout = 0; playout < PlayoutsPerBoard; playout++)
            {
                var session = new LevelSession(start, true, mode);
                var oracle = new StagnationOracle();
                oracle.Check(session, name);
                for (int step = 0; step < StepsPerPlayout && session.Status != LevelStatus.Won; step++)
                {
                    string context = name + ", partida " + playout.ToString(CultureInfo.InvariantCulture) + ", paso "
                        + step.ToString(CultureInfo.InvariantCulture);
                    if (session.Status == LevelStatus.Stuck)
                    {
                        if (!PourRules.IsStuck(session.Current, true))
                        {
                            stuckByLoop++;
                            Assert.That(oracle.HasPourToUnseen(session.Current, mode), Is.False, context + ": Stuck con un vertido a un tablero no visto");
                        }

                        if (rng.NextInt(4) == 0 || !session.CanUndo)
                        {
                            session.Restart();
                            oracle.Clear();
                        }
                        else
                        {
                            BoardState leaving = session.Current;
                            bool predicted = session.UndoEscapes;
                            Assert.That(session.Undo(), Is.True, context);
                            oracle.CheckUndo(session, leaving, predicted, context + " (Marea desde el bloqueo)");
                            continue;
                        }

                        oracle.Check(session, context + " (salida del bloqueo)");
                        continue;
                    }

                    if (probe.IsDeadEnd(session.Current, mode))
                    {
                        lostButPlaying++;
                        Assert.That(oracle.HasPourToUnseen(session.Current, mode), Is.True, context + ": perdido y repetido, pero en Playing");
                    }

                    if (session.CanUndo && rng.NextInt(3) == 0)
                    {
                        BoardState leaving = session.Current;
                        bool predicted = session.UndoEscapes;
                        Assert.That(session.Undo(), Is.True, context);
                        oracle.CheckUndo(session, leaving, predicted, context + " (Marea)");
                        continue;
                    }
                    else
                    {
                        int count = PourRules.CollectUsefulMoves(session.Current, false, mode, legal);
                        Assert.That(count, Is.GreaterThan(0), context);
                        Pour pick = legal[rng.NextInt(count)];
                        Assert.That(session.TryPour(pick.Source, pick.Target, out _), Is.True, context);
                    }

                    oracle.Check(session, context);
                }
            }
        }
    }
}
