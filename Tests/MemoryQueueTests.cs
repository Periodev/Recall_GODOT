// File: tests/MemoryQueueTests.cs
using NUnit.Framework;
using System;
using CombatCore;
using CombatCore.Memory;

namespace Tests
{
    public class MemoryQueueTests
    {
        [Test]
        public void Capacity3_PushABCA_ShouldBeBCA()
        {
            var q = new MemoryQueue(3);
            q.PushBasic(ActionType.A);
            q.PushBasic(ActionType.B);
            q.PushBasic(ActionType.C);
            q.PushBasic(ActionType.A); // 滿則淘汰最舊 A

            Assert.AreEqual(3, q.Count);
            var snap = q.Snapshot();
            CollectionAssert.AreEqual(new[] { ActionType.B, ActionType.C, ActionType.A }, snap);
            Assert.AreEqual(ActionType.B, q.Peek(0));
            Assert.AreEqual(ActionType.C, q.Peek(1));
            Assert.AreEqual(ActionType.A, q.Peek(2));
        }

        [Test]
        public void RecallSimulation_ReadOnly_ShouldNotChangeQueue()
        {
            var q = new MemoryQueue(3);
            q.PushBasic(ActionType.A);
            q.PushBasic(ActionType.B);
            q.PushBasic(ActionType.C);

            // 模擬 Echo/Recall：僅讀取，不呼叫 PushBasic
            var before = q.Snapshot();
            // 假裝把 before 轉命令，但不寫入 q
            var after = q.Snapshot();

            CollectionAssert.AreEqual(before, after);
            Assert.AreEqual(3, q.Count);
        }

        [Test]
        public void Peek_OutOfRange_ShouldThrow()
        {
            var q = new MemoryQueue(2);
            q.PushBasic(ActionType.A);

            Assert.Throws<ArgumentOutOfRangeException>(() => q.Peek(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => q.Peek(1)); // 目前 count=1，索引1無效
        }

        [Test]
        public void Construct_InvalidCapacity_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryQueue(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryQueue(-1));
        }
    }
}
