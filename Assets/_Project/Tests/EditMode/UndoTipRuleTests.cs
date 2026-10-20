#nullable enable
using AF.GameFlow;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El tooltip de Marea, una sola vez en la vida (D-030, pregunta 15 (b); tarea DC23; 02 §13 y 06 §2.2, fila del 3):
    /// <see cref="TutorialInstruction.UndoTipFor"/> lo da en el 3 tras el primer vertido que no acerca y, si no salió, en el 5 tras
    /// la intro; ya visto, en ningún nivel; y el 5 no tiene otra instrucción del tutorial.
    /// </summary>
    public sealed class UndoTipRuleTests
    {
        [Test]
        public void NotSeen_Level3AfterPour_Level5AfterIntro_NowhereElse()
        {
            Assert.That(TutorialInstruction.UndoTipFor(3, false), Is.EqualTo(UndoTipMoment.AfterPour));
            Assert.That(TutorialInstruction.UndoTipFor(5, false), Is.EqualTo(UndoTipMoment.AfterIntro));
            for (int level = -1; level <= 1000; level++)
            {
                if (level != 3 && level != 5)
                {
                    Assert.That(TutorialInstruction.UndoTipFor(level, false), Is.EqualTo(UndoTipMoment.None), "nivel " + level);
                }
            }
        }

        [Test]
        public void Seen_NeverAgain()
        {
            for (int level = -1; level <= 1000; level++)
            {
                Assert.That(TutorialInstruction.UndoTipFor(level, true), Is.EqualTo(UndoTipMoment.None), "nivel " + level);
            }
        }

        [Test]
        public void Level5_IsTheFallback_AndHasNoOtherInstruction()
        {
            Assert.That(TutorialInstruction.UndoFallbackLevel, Is.EqualTo(5));
            Assert.That(TutorialInstruction.KeyFor(5), Is.Null, "el 5 no tiene otra instrucción del tutorial");
            Assert.That(TutorialInstruction.Has(5), Is.False);
            Assert.That(TutorialInstruction.KeyFor(TutorialInstruction.UndoLevel), Is.EqualTo(TutorialInstruction.UndoKey));
            Assert.That(TutorialInstruction.IsUndoTipLevel(3), Is.True);
            Assert.That(TutorialInstruction.IsUndoTipLevel(5), Is.True);
            Assert.That(TutorialInstruction.IsUndoTipLevel(4) || TutorialInstruction.IsUndoTipLevel(6)
                || TutorialInstruction.IsUndoTipLevel(12), Is.False);
        }
    }
}
