#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Oráculo independiente de Aguas estancadas por repetición (02 R13 ampliada; añadido de D-014 del 2026-11-06): lleva su
    /// propio conjunto de tableros vistos (con <see cref="BoardState.CanonicalHash"/>, no con el de <see cref="LevelSession"/>)
    /// y calcula los hijos con <see cref="PourRules.Apply(BoardState, int, int, PourMode)"/>. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    internal sealed class StagnationOracle
    {
        private readonly HashSet<ulong> _seen = new HashSet<ulong>();
        private readonly DeadEndProbe _probe = new DeadEndProbe();

        /// <summary>Tableros distintos vistos (por hash canónico).</summary>
        public int SeenCount => _seen.Count;

        /// <summary><paramref name="state"/> queda en pantalla.</summary>
        public void Visit(BoardState state)
        {
            _seen.Add(state.CanonicalHash());
        }

        /// <summary>Volver a la orilla: intento nuevo.</summary>
        public void Clear()
        {
            _seen.Clear();
        }

        /// <summary>Hay un vertido legal desde <paramref name="state"/> a un tablero no visto.</summary>
        public bool HasPourToUnseen(BoardState state, PourMode mode)
        {
            for (int a = 0; a < state.Count; a++)
            {
                for (int b = 0; b < state.Count; b++)
                {
                    if (a != b && PourRules.Amount(state, a, b, mode) > 0
                        && !_seen.Contains(PourRules.Apply(state, a, b, mode).CanonicalHash()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Estado que tiene que dar <see cref="LevelSession"/> en <paramref name="state"/> con lo visto hasta ahora.</summary>
        public LevelStatus Expected(BoardState state, bool futileRuleEnabled, PourMode mode)
        {
            LevelStatus classic = BoardRules.EvaluateStatus(state.CuiasArray, 0, state.Count, state.UnitsTable, futileRuleEnabled);
            if (classic != LevelStatus.Playing)
            {
                return classic;
            }

            return _probe.IsDeadEnd(state, mode) && !HasPourToUnseen(state, mode) ? LevelStatus.Stuck : LevelStatus.Playing;
        }

        /// <summary>Anota el tablero actual de <paramref name="session"/> y comprueba su estado.</summary>
        public void Check(LevelSession session, string context)
        {
            Visit(session.Current);
            Assert.That(session.Status, Is.EqualTo(Expected(session.Current, session.FutileRuleEnabled, session.PourMode)), context);
            Assert.That(session.SeenBoardCount, Is.EqualTo(SeenCount), context + ": vistos");
        }

        /// <summary>
        /// Prueba cada vertido legal desde el tablero actual y lo deshace con Marea, hasta que la partida deja de estar en
        /// Playing: el jugador que repite. Con Aguas turbias, Marea deja visible lo revelado (otro tablero), así que da hasta
        /// <paramref name="passes"/> vueltas. Devuelve el estado final.
        /// </summary>
        public static LevelStatus TryEveryPourAndUndo(LevelSession session, int passes = 3)
        {
            for (int pass = 0; pass < passes && session.Status == LevelStatus.Playing; pass++)
            {
                TryEveryPourAndUndoOnce(session);
            }

            return session.Status;
        }

        private static void TryEveryPourAndUndoOnce(LevelSession session)
        {
            int count = session.Current.Count;
            for (int a = 0; a < count; a++)
            {
                for (int b = 0; b < count; b++)
                {
                    if (session.Status != LevelStatus.Playing)
                    {
                        return;
                    }

                    if (a != b && PourRules.IsLegal(session.Current, a, b))
                    {
                        Assert.That(session.TryPour(a, b, out _), Is.True);
                        Assert.That(session.Undo(), Is.True);
                    }
                }
            }
        }
    }
}
