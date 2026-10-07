#nullable enable
using System;
using AF.Solver.Collections;
using NUnit.Framework;

namespace AF.Solver.Tests
{
    /// <summary>
    /// Prueba de humo de AF.Solver: el montículo que usará el A* ordena por prioridad y desempata por orden
    /// de llegada. Test compartido (NUnit 3.5, C# 9).
    /// </summary>
    [TestFixture]
    public sealed class SmokeTests
    {
        [Test]
        public void MinHeap_PopsByPriority_AndBreaksTiesInArrivalOrder()
        {
            var heap = new MinHeap(2);
            heap.Push(10, 5);
            heap.Push(11, 1);
            heap.Push(12, 5);
            heap.Push(13, 3);
            heap.Push(14, 1);

            var popped = new int[5];
            for (int i = 0; i < popped.Length; i++)
            {
                popped[i] = heap.Pop();
            }

            Assert.That(popped, Is.EqualTo(new[] { 11, 14, 13, 10, 12 }));
            Assert.That(heap.Count, Is.EqualTo(0));
        }

        [Test]
        public void MinHeap_GrowsBeyondInitialCapacity()
        {
            var heap = new MinHeap(1);
            for (int i = 99; i >= 0; i--)
            {
                heap.Push(i, i);
            }

            Assert.That(heap.Count, Is.EqualTo(100));
            Assert.That(heap.Capacity, Is.GreaterThanOrEqualTo(100));
            for (int expected = 0; expected < 100; expected++)
            {
                Assert.That(heap.TryPop(out int value, out int priority), Is.True);
                Assert.That(value, Is.EqualTo(expected));
                Assert.That(priority, Is.EqualTo(expected));
            }

            Assert.That(heap.TryPop(out _, out _), Is.False);
        }

        [Test]
        public void MinHeap_PopOnEmpty_Throws()
        {
            var heap = new MinHeap(4);

            Assert.That(() => { heap.Pop(); }, Throws.TypeOf<InvalidOperationException>());
        }
    }
}
