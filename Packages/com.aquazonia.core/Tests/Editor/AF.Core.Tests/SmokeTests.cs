#nullable enable
using AF.Core.Board;
using AF.Core.Rules;
using NUnit.Framework;

namespace AF.Core.Tests
{
    /// <summary>
    /// Prueba de humo del núcleo: un nivel de tres cuias se resuelve en tres vertidos (R9, R5, R12).
    /// Test compartido: compila en Unity (NUnit 3.5, C# 9) y en dotnet test (NUnit 3.14).
    /// Solo Assert.That con restricciones y atributos clásicos.
    /// </summary>
    [TestFixture]
    public sealed class SmokeTests
    {
        [Test]
        public void ThreeCuiaLevel_IsSolvedInThreePours()
        {
            var start = new BoardState(new[]
            {
                Cuia.Create(4, new[] { 1, 1, 2, 2 }),
                Cuia.Create(4, new[] { 2, 2, 1, 1 }),
                Cuia.Empty(4),
            });
            Assert.That(start.IsWon(), Is.False);

            BoardState afterFirst = PourRules.Apply(start, 0, 2);
            BoardState afterSecond = PourRules.Apply(afterFirst, 1, 0);
            BoardState solved = PourRules.Apply(afterSecond, 1, 2);

            Assert.That(afterSecond.IsSealed(0), Is.True);
            Assert.That(solved.IsSealed(2), Is.True);
            Assert.That(solved[1].IsEmpty, Is.True);
            Assert.That(solved.IsWon(), Is.True);
        }
    }
}
