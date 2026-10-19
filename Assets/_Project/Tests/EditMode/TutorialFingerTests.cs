#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Levels;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Dedo guía del tutorial (tarea DC6; D-028, pregunta 61 (a) «con P5»; 02 §13, filas 1 y 2; 06 §2.2): qué niveles lo llevan
    /// (<see cref="TutorialInstruction.FingerFor"/>: solo el 1 y el 2; el 12, sin dedo) y qué vertido señala
    /// (<see cref="TutorialFinger.TryChoose"/>) sobre los tableros reales de <c>tutorial_v0</c>: en el 1, el primero de una
    /// solución óptima; en el 2, el vertido parcial de la lección, una racha de 3 sobre un hueco de 2 (D-005), en cuanto es legal.
    /// </summary>
    public sealed class TutorialFingerTests
    {
        [Test]
        public void OnlyLevels1And2_HaveAFinger_12DoesNot()
        {
            Assert.That(TutorialInstruction.FingerFor(1), Is.EqualTo(FingerLesson.FirstPour));
            Assert.That(TutorialInstruction.FingerFor(2), Is.EqualTo(FingerLesson.PartialRun));
            for (int level = 3; level <= 700; level++)
            {
                Assert.That(TutorialInstruction.HasFinger(level), Is.False, "nivel " + level);
            }

            Assert.That(TutorialInstruction.HasFinger(TutorialInstruction.MorphoLevel), Is.False, "el 12, sin dedo (D-028, 61 a)");
            Assert.That(TutorialInstruction.HasFinger(6), Is.False, "el 6, «sin dedo guía» (02 §13)");
            Assert.That(TutorialInstruction.HasFinger(0) || TutorialInstruction.HasFinger(-1), Is.False);
        }

        [Test]
        public void Level1_PointsAtTheFirstPourOfAnOptimalSolution()
        {
            LevelDefinition def = TestPacks.Tutorial.Find(1)!;
            BoardState board = def.CreateBoard();
            var solver = new AStarSolver();
            Assert.That(TutorialFinger.TryChoose(FingerLesson.FirstPour, board, PourModes.Default, true, solver, out Pour pour), Is.True);
            Assert.That(pour.Source, Is.Not.EqualTo(pour.Target));
            Assert.That(PourRules.IsLegal(board, pour.Source, pour.Target), Is.True, "un vertido legal");
            Assert.That(pour.Amount, Is.EqualTo(PourRules.Amount(board, pour.Source, pour.Target, PourModes.Default)));

            // Es el primero de una solución óptima: lo que queda se resuelve en par − 1.
            BoardState next = PourRules.Apply(board, pour.Source, pour.Target, PourModes.Default);
            SolveResult rest = solver.Solve(next, SolverOptions.ForPipeline(PourModes.Default, next).WithFutileRule(true));
            Assert.That(rest.IsSolved, Is.True);
            Assert.That(rest.MoveCount, Is.EqualTo(def.Par - 1), "el primer vertido de la solución (par " + def.Par + ")");

            // Determinista: la misma elección con otro solver.
            Assert.That(TutorialFinger.TryChoose(FingerLesson.FirstPour, board, PourModes.Default, true, new AStarSolver(), out Pour again), Is.True);
            Assert.That(again, Is.EqualTo(pour));
        }

        [Test]
        public void Level2_TheLessonIsNotLegalAtTheStart_AndThenPointsAtTheRunOf3OnTheHoleOf2()
        {
            LevelDefinition def = TestPacks.Tutorial.Find(2)!;
            BoardState board = def.CreateBoard();
            var solver = new AStarSolver();

            // El tablero inicial del 2 está lleno: no hay hueco de 2 y el dedo espera.
            Assert.That(TutorialFinger.TryChoose(FingerLesson.PartialRun, board, PourModes.Default, true, solver, out _), Is.False);

            // Tras cualquier primer vertido que deje la lección legal, el dedo la señala: 3 dosis de la racha, 2 caben y la tercera
            // se queda, sin dejar el tablero sin salida. La solución óptima del 2 (par 6) no pasa por el parcial (hacerlo cuesta un
            // vertido más), así que no se puede tomar «de la solución»: es el primero legal que deja salida (TryChoose).
            int lessons = 0;
            for (int s = 0; s < board.Count; s++)
            {
                for (int t = 0; t < board.Count; t++)
                {
                    if (s == t || !PourRules.IsLegal(board, s, t))
                    {
                        continue;
                    }

                    BoardState walk = PourRules.Apply(board, s, t, PourModes.Default);
                    if (!TutorialFinger.TryChoose(FingerLesson.PartialRun, walk, PourModes.Default, true, solver, out Pour pour))
                    {
                        continue;
                    }

                    lessons++;
                    Assert.That(TutorialInstruction.IsPartialRunLesson(walk, pour.Source, pour.Target, PourModes.Default), Is.True);
                    Assert.That(walk[pour.Source].TopRunLength, Is.GreaterThanOrEqualTo(TutorialInstruction.FingerLessonRun));
                    Assert.That(walk[pour.Target].Space, Is.EqualTo(TutorialInstruction.FingerLessonHole));
                    Assert.That(pour.Amount, Is.EqualTo(TutorialInstruction.FingerLessonHole), "el vertido parcial");
                    BoardState after = PourRules.Apply(walk, pour.Source, pour.Target, PourModes.Default);
                    SolveResult rest = solver.Solve(after, SolverOptions.ForPipeline(PourModes.Default, after).WithFutileRule(true));
                    Assert.That(rest.IsSolved, Is.True, "no deja el tablero sin salida");
                    TestContext.WriteLine("AF_FINGER 2: tras " + s + "→" + t + ", la lección " + pour.Source + "→" + pour.Target
                        + " (" + pour.Amount + "); después faltan " + rest.MoveCount + " (par " + def.Par + ")");
                }
            }

            Assert.That(lessons, Is.GreaterThan(0), "algún primer vertido deja la lección legal");
        }

        [Test]
        public void Level2_AfterTheEmptyingPour_TheFingerPointsAtTheAcaiRunOnTheFirstCuia()
        {
            // El primer vertido natural del 2: las dos dosis de arriba de la cuia 0 a la vacía 3. Queda la cuia 0 con un hueco de 2
            // y açaí arriba, y la racha de 3 açaí de la cuia 2 cabe solo en parte.
            BoardState board = TestPacks.Tutorial.Find(2)!.CreateBoard();
            BoardState next = PourRules.Apply(board, 0, 3, PourModes.Default);
            Assert.That(TutorialFinger.TryChoose(FingerLesson.PartialRun, next, PourModes.Default, true, new AStarSolver(), out Pour pour), Is.True);
            Assert.That(pour, Is.EqualTo(new Pour(2, 0, 2)));
        }

        [Test]
        public void PartialRunLesson_OnlyTheRunOf3OnAHoleOf2_InRunMode()
        {
            // [açaí ×3] sobre [aguaje, açaí] (hueco de 2): la lección. Sobre la vacía (hueco de 4), no; en single, nunca.
            BoardState state = Board(new[] { 4, 8, 8, 8 }, new[] { 4, 8 }, new int[0], new[] { 4, 4 });
            Assert.That(TutorialInstruction.IsPartialRunLesson(state, 0, 1, PourMode.Run), Is.True);
            Assert.That(TutorialInstruction.IsPartialRunLesson(state, 0, 2, PourMode.Run), Is.False, "a la vacía cabe entera");
            Assert.That(TutorialInstruction.IsPartialRunLesson(state, 0, 1, PourMode.Single), Is.False, "en single no hay parcial");
            Assert.That(TutorialInstruction.IsPartialRunLesson(state, 1, 0, PourMode.Run), Is.False, "ilegal: llena");
            Assert.That(TutorialInstruction.IsPartialRunLesson(state, 3, 1, PourMode.Run), Is.False, "otra cima");
            Assert.That(TutorialInstruction.IsPartialRunLesson(state, 0, 0, PourMode.Run), Is.False);
            Assert.That(TutorialInstruction.IsPartialRunLesson(state, 0, 9, PourMode.Run), Is.False);

            // Una racha de 2 sobre un hueco de 2 cabe entera: no es la lección.
            BoardState two = Board(new[] { 4, 4, 8, 8 }, new[] { 4, 8 }, new int[0], new[] { 4, 8 });
            Assert.That(TutorialInstruction.IsPartialRunLesson(two, 0, 1, PourMode.Run), Is.False);
        }

        [Test]
        public void NoLesson_OrAWonBoard_NoFinger()
        {
            BoardState board = TestPacks.Tutorial.Find(1)!.CreateBoard();
            Assert.That(TutorialFinger.TryChoose(FingerLesson.None, board, PourModes.Default, true, new AStarSolver(), out _), Is.False);
            BoardState won = Board(new[] { 8, 8, 8, 8 }, new[] { 4, 4, 4, 4 }, new int[0], new int[0]);
            Assert.That(TutorialFinger.TryChoose(FingerLesson.FirstPour, won, PourModes.Default, true, new AStarSolver(), out _), Is.False);
        }

        private static BoardState Board(params int[][] cuias)
        {
            var list = new Cuia[cuias.Length];
            for (int i = 0; i < cuias.Length; i++)
            {
                list[i] = cuias[i].Length == 0 ? Cuia.Empty(4) : Cuia.Create(4, cuias[i]);
            }

            return new BoardState(list);
        }
    }
}
