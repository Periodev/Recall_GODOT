// File: tests/MemoryQueueTests.cs
using NUnit.Framework;
using System;
using System.Linq;
using CombatCore;
using CombatCore.Memory;

namespace Tests
{
    public class MemoryQueueTests
    {
        [Test]
        public void Capacity3_PushABCA_ShouldKeepBCA_WithTurns()
        {
            var q = new MemoryQueue(3);
            q.Push(ActionType.A, 1);
            q.Push(ActionType.B, 1);
            q.Push(ActionType.C, 2);
            q.Push(ActionType.A, 3); // 滿則淘汰最舊 A

            Assert.AreEqual(3, q.Count);

            var ops   = q.SnapshotOps();
            var turns = q.SnapshotTurns();

            CollectionAssert.AreEqual(new[] { ActionType.B, ActionType.C, ActionType.A }, ops);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, turns);
        }

        [Test]
        public void RecallSimulation_ReadOnlySnapshot_DoesNotMutateQueue()
        {
            var q = new MemoryQueue(3);
            q.Push(ActionType.A, 1);
            q.Push(ActionType.B, 1);
            q.Push(ActionType.C, 2);

            var beforeOps   = q.SnapshotOps().ToArray();
            var beforeTurns = q.SnapshotTurns().ToArray();

            // 模擬回放：只讀不寫
            var afterOps   = q.SnapshotOps().ToArray();
            var afterTurns = q.SnapshotTurns().ToArray();

            CollectionAssert.AreEqual(beforeOps, afterOps);
            CollectionAssert.AreEqual(beforeTurns, afterTurns);
            Assert.AreEqual(3, q.Count);
        }

        [Test]
        public void Clear_ShouldEmptyBothOpsAndTurns()
        {
            var q = new MemoryQueue(2);
            q.Push(ActionType.A, 10);
            q.Push(ActionType.B, 11);

            q.Clear();

            Assert.AreEqual(0, q.Count);
            Assert.AreEqual(0, q.SnapshotOps().Count);
            Assert.AreEqual(0, q.SnapshotTurns().Count);
        }

        [Test]
        public void Construct_InvalidCapacity_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryQueue(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryQueue(-1));
        }
    }
}
