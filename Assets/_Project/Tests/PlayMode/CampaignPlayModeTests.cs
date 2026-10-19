#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Levels;
using AF.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La campaña 11-300 en la escena <c>Game</c> real (traspaso §6 pasos 1.1-1.2; 08 §4.1 y §4.4; tarea C1a): la escena enlaza
    /// los 6 paquetes, cada nivel de muestra arranca con el tablero de <see cref="LevelDefinition.CreateBoard"/> y llega a jugarse, y
    /// las reglas del núcleo de tres estrenos de D-007 se ven a través de <see cref="LevelFlow.TapCuia"/>: la Hoja del 43
    /// cae con su fruta (02 §9.1 M1), el Remolino del 103 mete la racha por el fondo de la pareja (M4) y las dosis turbias del
    /// 100 siguen ocultas hasta llegar a la cima (02 §11, R7).
    /// </summary>
    public sealed class CampaignPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator LoadGame_StartsWithTheCatalogBoard_AndReachesPlaying(
            [Values(11, 41, 43, 81, 100, 103, 141, 183, 200, 201, 300, 301, 421, 561, 700)] int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Catalog.FirstLevel, Is.EqualTo(1));
            Assert.That(flow.Catalog.LastLevel, Is.EqualTo(700), "la escena enlaza tutorial_v0 y mundo_02..09_v1 (C1a y C1b)");
            Assert.That(flow.LevelNumber, Is.EqualTo(level));
            Assert.That(flow.Level.Level, Is.EqualTo(level));
            Assert.That(flow.Level.World, Is.EqualTo(WorldTable.WorldOf(level)), "glosario §8");
            Assert.That(flow.Session.Current, Is.EqualTo(flow.Level.CreateBoard()), "el tablero del paquete, con sus mecánicas y ocultos");
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Playing));

            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => flow.Phase == FlowPhase.Playing, "fase Playing del nivel " + level);
            Assert.That(flow.AcceptsBoardInput, Is.True);
        }

        [UnityTest]
        public IEnumerator Hoja43_KeyPourOpens()
        {
            yield return LoadPlaying(43);
            LevelFlow flow = GameBootstrap.Current!.Flow!;
            int hoja = IndexOf(flow.Level, MechanicType.Hoja);
            int key = flow.Level.Attrs[hoja].Key;
            Assert.That(key, Is.EqualTo(4), "aguaje (glosario §9)");
            BoardState before = flow.Session.Current;
            Assert.That(before[hoja].Mechanic, Is.EqualTo(MechanicType.Hoja));
            Assert.That(before[hoja].IsEmpty, Is.True);

            // Otra fruta no entra en la Hoja (02 §9.1 M1: solo acepta su llave).
            int other = -1;
            for (int i = 0; i < before.Count && other < 0; i++)
            {
                if (i != hoja && PourRules.CanBeSource(before, i) && before[i].TopColor != key)
                {
                    other = i;
                }
            }

            Assert.That(other, Is.GreaterThanOrEqualTo(0));
            Assert.That(PourRules.IsLegal(before, other, hoja), Is.False);
            Assert.That(flow.TapCuia(other).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(flow.TapCuia(hoja).Kind, Is.EqualTo(TapKind.Rejected), "otra fruta no entra en la Hoja: rechazo (C6)");
            Assert.That(flow.Session.Current, Is.EqualTo(before), "el rechazo no cambia el tablero (R11)");
            flow.TapBackground();

            // La llave sí: la hoja cae y la cuia queda normal, con la fruta dentro.
            int source = FindSource(flow.Session.Current, hoja, c => c == key);
            Assert.That(source, Is.GreaterThanOrEqualTo(0), "hay una cuia con aguaje en la cima");
            int amount = PourRules.Amount(flow.Session.Current, source, hoja, flow.Session.PourMode);
            Assert.That(flow.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            TapResult poured = flow.TapCuia(hoja);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured));
            Cuia opened = flow.Session.Current[hoja];
            Assert.That(opened.Mechanic, Is.EqualTo(MechanicType.None), "02 R16 (D-007): la Hoja cae con su primer vertido");
            Assert.That(opened.Count, Is.EqualTo(amount));
            Assert.That(opened.TopColor, Is.EqualTo(key));
        }

        [UnityTest]
        public IEnumerator Remolino103_PourGoesToPartnerBottom()
        {
            yield return LoadPlaying(103);
            LevelFlow flow = GameBootstrap.Current!.Flow!;
            int tapped = IndexOf(flow.Level, MechanicType.Remolino);
            int partner = flow.Level.Attrs[tapped].Partner;
            Assert.That(flow.Level.Attrs[partner].Type, Is.EqualTo(MechanicType.Remolino));
            Assert.That(flow.Level.Attrs[partner].Partner, Is.EqualTo(tapped));

            // Las dos cuias del par empiezan llenas: se hace sitio en la pareja con vertidos legales del modelo.
            Pour[]? setup = FindPath(flow, s => !s[partner].IsFull && FindSource(s, tapped, partner) >= 0);
            Assert.That(setup, Is.Not.Null, "vertidos que dejan sitio en la pareja");
            Play(flow, setup!);

            BoardState before = flow.Session.Current;
            int source = FindSource(before, tapped, partner);
            int color = before[source].TopColor;
            int amount = PourRules.Amount(before, source, tapped, flow.Session.PourMode);
            Assert.That(amount, Is.GreaterThan(0));
            Assert.That(flow.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(flow.TapCuia(tapped).Kind, Is.EqualTo(TapKind.Poured));

            BoardState after = flow.Session.Current;
            Assert.That(after[tapped], Is.EqualTo(before[tapped]), "02 §9.1 M4: la cuia tocada no cambia");
            Cuia receiver = after[partner];
            Assert.That(receiver.Count, Is.EqualTo(before[partner].Count + amount));
            for (int k = 0; k < amount; k++)
            {
                Assert.That(receiver.ColorAt(k), Is.EqualTo(color), "la racha entra por el fondo de la pareja (capa " + k + ")");
            }

            for (int k = 0; k < before[partner].Count; k++)
            {
                Assert.That(receiver.ColorAt(amount + k), Is.EqualTo(before[partner].ColorAt(k)), "lo que había sube (capa " + k + ")");
            }

            Assert.That(after[source].Count, Is.EqualTo(before[source].Count - amount));
        }

        [UnityTest]
        public IEnumerator Murky100_HiddenStayHiddenUntilTop()
        {
            yield return LoadPlaying(100);
            LevelFlow flow = GameBootstrap.Current!.Flow!;
            Assert.That(flow.Level.IsMurky, Is.True);
            BoardState start = flow.Session.Current;
            Assert.That(start.HiddenCount, Is.EqualTo(2));
            int[] bottom = flow.Level.CopyDoses(0);
            Assert.That(start[0].IsHiddenAt(0) && start[0].IsHiddenAt(1), Is.True, "[0, 0] y [0, 1] ocultas");
            Assert.That(start[0].IsHiddenAt(start[0].Count - 1), Is.False, "R7: la cima siempre es visible");

            // Vertidos legales (PourRules, búsqueda en anchura) hasta que la dosis del fondo de la cuia 0 llega a la cima.
            Pour[]? path = FindPath(flow, s => s[0].HiddenMask == 0);
            Assert.That(path, Is.Not.Null, "vertidos que destapan la cuia 0");
            var reachedTop = new bool[Cuia.MaxCapacity];
            for (int i = 0; i < path!.Length; i++)
            {
                Assert.That(flow.TapCuia(path[i].Source).Kind, Is.EqualTo(TapKind.Selected), "origen " + i);
                Assert.That(flow.TapCuia(path[i].Target).Kind, Is.EqualTo(TapKind.Poured), "destino " + i);
                BoardState now = flow.Session.Current;
                for (int c = 0; c < now.Count; c++)
                {
                    Assert.That(now[c].Count == 0 || !now[c].IsHiddenAt(now[c].Count - 1), Is.True, "R7 en la cuia " + c + " tras el vertido " + i);
                }

                Cuia murky = now[0];
                if (murky.Count > 0)
                {
                    reachedTop[murky.Count - 1] = true;
                }

                for (int k = 0; k < 2 && k < murky.Count; k++)
                {
                    Assert.That(murky.IsHiddenAt(k), Is.EqualTo(!reachedTop[k]), "02 §11: la dosis [0, " + k + "] sigue oculta hasta llegar a la cima (vertido " + i + ")");
                }
            }

            Cuia revealed = flow.Session.Current[0];
            Assert.That(revealed.HiddenMask, Is.EqualTo(0));
            Assert.That(revealed.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(revealed.ColorAt(0), Is.EqualTo(bottom[0]), "la fruta del fondo, ya a la vista");
            Assert.That(flow.Session.RevealedLayers(0), Is.EqualTo(0b11), "02 T6: lo revelado queda revelado");
        }

        // Carga el nivel y salta la intro hasta Playing.
        private static IEnumerator LoadPlaying(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing, "fase Playing del nivel " + level);
        }

        private static int IndexOf(LevelDefinition def, MechanicType type)
        {
            for (int i = 0; i < def.Attrs.Count; i++)
            {
                if (def.Attrs[i].Type == type)
                {
                    return i;
                }
            }

            Assert.Fail(def.Id + " no tiene " + type);
            return -1;
        }

        // Primera cuia (distinta de target) cuya cima cumple colorOk y que puede verter en target.
        private static int FindSource(BoardState state, int target, Func<int, bool> colorOk)
        {
            for (int i = 0; i < state.Count; i++)
            {
                if (i != target && !state[i].IsEmpty && colorOk(state[i].TopColor) && PourRules.IsLegal(state, i, target))
                {
                    return i;
                }
            }

            return -1;
        }

        // Primera cuia (distinta de target y de exclude) que puede verter en target.
        private static int FindSource(BoardState state, int target, int exclude)
        {
            for (int i = 0; i < state.Count; i++)
            {
                if (i != target && i != exclude && PourRules.IsLegal(state, i, target))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void Play(LevelFlow flow, Pour[] path)
        {
            for (int i = 0; i < path.Length; i++)
            {
                Assert.That(flow.TapCuia(path[i].Source).Kind, Is.EqualTo(TapKind.Selected), "origen " + i);
                Assert.That(flow.TapCuia(path[i].Target).Kind, Is.EqualTo(TapKind.Poured), "destino " + i);
            }
        }

        // Vertidos legales más cortos (búsqueda en anchura con las reglas del núcleo y el modo de la partida) desde el tablero
        // actual hasta uno que cumpla goal; vacío si ya lo cumple y null si no aparece en los primeros estados. Los estados
        // ganados o en Aguas estancadas no sirven de paso: en ellos el flujo no acepta toques.
        private static Pour[]? FindPath(LevelFlow flow, Func<BoardState, bool> goal)
        {
            const int maxStates = 20000;
            BoardState start = flow.Session.Current;
            PourMode mode = flow.Session.PourMode;
            bool futile = flow.Session.FutileRuleEnabled;
            var parents = new Dictionary<BoardState, (BoardState? Parent, Pour Move)> { { start, (null, default) } };
            var queue = new Queue<BoardState>();
            queue.Enqueue(start);
            while (queue.Count > 0 && parents.Count < maxStates)
            {
                BoardState current = queue.Dequeue();
                if (goal(current))
                {
                    var path = new List<Pour>();
                    for (BoardState? at = current; at != null && parents[at].Parent != null; at = parents[at].Parent)
                    {
                        path.Add(parents[at].Move);
                    }

                    path.Reverse();
                    return path.ToArray();
                }

                for (int a = 0; a < current.Count; a++)
                {
                    for (int b = 0; b < current.Count; b++)
                    {
                        if (a == b || !PourRules.IsLegal(current, a, b))
                        {
                            continue;
                        }

                        BoardState next = PourRules.Apply(current, a, b, mode);
                        if (parents.ContainsKey(next) || next.IsWon() || PourRules.IsStuck(next, futile))
                        {
                            continue;
                        }

                        parents.Add(next, (current, new Pour(a, b, PourRules.Amount(current, a, b, mode))));
                        queue.Enqueue(next);
                    }
                }
            }

            return null;
        }
    }
}
