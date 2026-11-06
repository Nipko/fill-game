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
    /// <item><description>Volver a la orilla olvida lo visto; Marea no.</description></item>
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
                            Assert.That(session.Undo(), Is.True, context);
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
                        Assert.That(session.Undo(), Is.True, context);
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
