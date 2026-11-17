#nullable enable
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Oráculo independiente de Aguas estancadas por repetición (02 R13 ampliada; añadidos de D-014 del 2026-11-06 y del
    /// 2026-11-17): lleva su propio contador de tableros vistos (con <see cref="BoardState.CanonicalHash"/>, no con el de
    /// <see cref="LevelSession"/>; Marea resta 1 al tablero que deja) y calcula los hijos con
    /// <see cref="PourRules.Apply(BoardState, int, int, PourMode)"/>. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    internal sealed class StagnationOracle
    {
        private readonly Dictionary<ulong, int> _seen = new Dictionary<ulong, int>();
        private readonly DeadEndProbe _probe = new DeadEndProbe();

        /// <summary>Tableros distintos vistos (por hash canónico).</summary>
        public int SeenCount => _seen.Count;

        /// <summary><paramref name="state"/> queda en pantalla (un vertido, el inicio, la Luciérnaga, la Pororoca): suma 1.</summary>
        public void Visit(BoardState state)
        {
            ulong hash = state.CanonicalHash();
            _seen[hash] = _seen.TryGetValue(hash, out int times) ? times + 1 : 1;
        }

        /// <summary>
        /// Marea deja <paramref name="leaving"/> (resta 1; a 0 deja de estar visto) y vuelve a <paramref name="back"/> (visto, con 1
        /// si no lo estaba).
        /// </summary>
        public void Undo(BoardState leaving, BoardState back)
        {
            ulong hash = leaving.CanonicalHash();
            if (_seen.TryGetValue(hash, out int times))
            {
                if (times > 1)
                {
                    _seen[hash] = times - 1;
                }
                else
                {
                    _seen.Remove(hash);
                }
            }

            ulong backHash = back.CanonicalHash();
            if (!_seen.ContainsKey(backHash))
            {
                _seen[backHash] = 1;
            }
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
                        && !_seen.ContainsKey(PourRules.Apply(state, a, b, mode).CanonicalHash()))
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
            Verify(session, context);
        }

        /// <summary>
        /// Tras <c>session.Undo()</c> desde <paramref name="leaving"/>: anota la Marea y comprueba el estado, y que
        /// <see cref="LevelSession.UndoEscapes"/> lo dijera antes (<paramref name="predicted"/>).
        /// </summary>
        public void CheckUndo(LevelSession session, BoardState leaving, bool predicted, string context)
        {
            Undo(leaving, session.Current);
            Verify(session, context);
            Assert.That(predicted, Is.EqualTo(session.Status == LevelStatus.Playing), context + ": UndoEscapes");
        }

        /// <summary>Comprueba el estado de <paramref name="session"/> con lo visto hasta ahora.</summary>
        public void Verify(LevelSession session, string context)
        {
            Assert.That(session.Status, Is.EqualTo(Expected(session.Current, session.FutileRuleEnabled, session.PourMode)), context);
            Assert.That(session.SeenBoardCount, Is.EqualTo(SeenCount), context + ": vistos");
        }

        /// <summary>
        /// Prueba cada vertido legal desde el tablero actual y lo deshace con Marea, hasta <paramref name="passes"/> vueltas o
        /// hasta que la partida deja de estar en Playing. Devuelve el estado final. Desde el añadido de D-014 del 2026-11-17, Marea
        /// saca de los vistos lo que deshace, así que probar y deshacer ya no agota el tablero: un tablero perdido sigue en Playing
        /// (lo que el dueño pidió). Para agotarlo, <see cref="WalkUntilStuck"/>.
        /// </summary>
        public static LevelStatus TryEveryPourAndUndo(LevelSession session, int passes = 3)
        {
            for (int pass = 0; pass < passes && session.Status == LevelStatus.Playing; pass++)
            {
                TryEveryPourAndUndoOnce(session);
            }

            return session.Status;
        }

        /// <summary>
        /// El jugador que repite sin Marea (añadido de D-014 del 2026-11-17): en cada paso hace el primer vertido legal (A, B
        /// ascendentes) a un tablero que aún no pisó en este paseo; si no queda ninguno, para. Desde un tablero perdido (R13
        /// ampliada) acaba en Aguas estancadas en como mucho <see cref="DeadEndProbe.MaxStates"/> pasos, porque todo lo alcanzable
        /// también está perdido y cada paso pisa un tablero nuevo del cierre. Devuelve el estado final.
        /// </summary>
        public static LevelStatus WalkUntilStuck(LevelSession session)
        {
            var walked = new HashSet<ulong> { session.Current.CanonicalHash() };
            for (int step = 0; step <= DeadEndProbe.MaxStates && session.Status == LevelStatus.Playing; step++)
            {
                BoardState state = session.Current;
                bool moved = false;
                for (int a = 0; a < state.Count && !moved; a++)
                {
                    for (int b = 0; b < state.Count && !moved; b++)
                    {
                        if (a != b && PourRules.Amount(state, a, b, session.PourMode) > 0
                            && walked.Add(PourRules.Apply(state, a, b, session.PourMode).CanonicalHash()))
                        {
                            Assert.That(session.TryPour(a, b, out _), Is.True);
                            moved = true;
                        }
                    }
                }

                if (!moved)
                {
                    break;
                }
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
