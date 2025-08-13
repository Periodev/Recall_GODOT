// File: Tests/InterOpsTests.cs
using NUnit.Framework;
using System.Collections.Generic;
using CombatCore.InterOp;
using CombatCore.Memory;

namespace Tests
{
    [TestFixture]
    public class InterOpsTests
    {
        private FakeCombatState _state;
        private IMemoryQueue _q;
        private InterOps _ops;

        [SetUp]
        public void Setup()
        {
            // 使用 CombatStateFactory 建立測試狀態
            _state = CombatStateFactory.Create();      // Player: id=0, Enemy: id=1
            _q = new MemoryQueue(3);
            _ops = new InterOps();

            // 預設記憶：A, B, C（0=最舊）
            _q.PushBasic(ActionType.A);
            _q.PushBasic(ActionType.B);
            _q.PushBasic(ActionType.C);
        }

        [Test]
        public void BasicA_NoCharge_ShouldProduce1Cmd()
        {
            var call = InterOpCall.BasicA(0, 1);
            var cmds = _ops.Translate(call, _state, _q);

            Assert.That(_ops.LastError, Is.EqualTo(string.Empty));
            Assert.That(cmds.Length, Is.EqualTo(1));   // 只有 DealDamage
        }

        [Test]
        public void BasicA_WithCharge_ShouldProduce2Cmds_AndSpendOnce()
        {
            _state = CombatStateFactory.Create(playerCharge: 1);

            var call = InterOpCall.BasicA(0, 1);
            var cmds = _ops.Translate(call, _state, _q);

            Assert.That(_ops.LastError, Is.EqualTo(string.Empty));
            Assert.That(cmds.Length, Is.EqualTo(2));   // DealDamage + GainCharge(-1)
        }

        [Test]
        public void BasicB_WithCharge_ShouldProduce2Cmds_AndSpendOnce()
        {
            _state = CombatStateFactory.Create(playerCharge: 1);

            var call = InterOpCall.BasicB(0, 1);
            var cmds = _ops.Translate(call, _state, _q);

            Assert.That(_ops.LastError, Is.EqualTo(string.Empty));
            Assert.That(cmds.Length, Is.EqualTo(2));   // AddShield + GainCharge(-1)
        }

        [Test]
        public void BasicC_ShouldProduce1Cmd_AndNotSpendCharge()
        {
            _state = CombatStateFactory.Create(playerCharge: 2);

            var call = InterOpCall.BasicC(0);
            var cmds = _ops.Translate(call, _state, _q);

            Assert.That(_ops.LastError, Is.EqualTo(string.Empty));
            Assert.That(cmds.Length, Is.EqualTo(1));   // GainCharge(+1)
        }

        [Test]
        public void BasicA_InsufficientAP_ShouldFail_AndReturnEmpty()
        {
            _state = CombatStateFactory.Create(playerAP: 0);

            var call = InterOpCall.BasicA(0, 1);
            var cmds = _ops.Translate(call, _state, _q);

            Assert.That(cmds.Length, Is.EqualTo(0));
            Assert.That(_ops.LastError, Is.EqualTo("insufficient_ap"));
        }

        [Test]
        public void BasicA_TargetDead_ShouldFail_AndReturnEmpty()
        {
            _state = CombatStateFactory.Create(enemyHP: 0);

            var call = InterOpCall.BasicA(0, 1);
            var cmds = _ops.Translate(call, _state, _q);

            Assert.That(cmds.Length, Is.EqualTo(0));
            Assert.That(_ops.LastError, Is.EqualTo("target_not_alive"));
        }

        [Test]
        public void RecallEcho_IndicesOutOfRange_ShouldFail_AndReturnEmpty()
        {
            var call = InterOpCall.RecallEcho(0, 1, new List<int> { 0, 3 }); // 3 越界
            var before = _q.Snapshot();
            var cmds = _ops.Translate(call, _state, _q);
            var after = _q.Snapshot();

            Assert.That(cmds.Length, Is.EqualTo(0));
            Assert.That(_ops.LastError, Is.EqualTo("index_out_of_range"));
            CollectionAssert.AreEqual(before, after);  // 不改變記憶
        }

        [Test]
        public void RecallEcho_AB_WithCharge_ShouldSpendOnceAtBatchEnd()
        {
            // 記憶目前為 [A, B, C]，取前兩個索引 0,1
            _state = CombatStateFactory.Create(playerCharge: 2);

            var call = InterOpCall.RecallEcho(0, 1, new List<int> { 0, 1 });
            var cmds = _ops.Translate(call, _state, _q);

            Assert.That(_ops.LastError, Is.EqualTo(string.Empty));
            // 期望：A → DealDamage，B → AddShield，批次尾端 GainCharge(-1)
            Assert.That(cmds.Length, Is.EqualTo(3));
        }

        [Test]
        public void RecallEcho_AA_WithCharge_ShouldSpendOnceForTwoAttacks()
        {
            // 將記憶改成 [A, A, C]
            _q = new MemoryQueue(3);
            _q.PushBasic(ActionType.A);
            _q.PushBasic(ActionType.A);
            _q.PushBasic(ActionType.C);

            _state = CombatStateFactory.Create(playerCharge: 2);

            var call = InterOpCall.RecallEcho(0, 1, new List<int> { 0, 1 });
            var cmds = _ops.Translate(call, _state, _q);

            Assert.That(_ops.LastError, Is.EqualTo(string.Empty));
            // 期望：兩個 DealDamage + 批次尾端 GainCharge(-1) = 3
            Assert.That(cmds.Length, Is.EqualTo(3));
        }

        [Test]
        public void RecallEcho_AC_WithCharge_ShouldHave3Cmds_Dmg_GainC_SpendOnce()
        {
            // 記憶 [A, B, C]，選 A 與 C
            _state = CombatStateFactory.Create(playerCharge: 1);

            var call = InterOpCall.RecallEcho(0, 1, new List<int> { 0, 2 });
            var before = _q.Snapshot();
            var cmds = _ops.Translate(call, _state, _q);
            var after = _q.Snapshot();

            Assert.That(_ops.LastError, Is.EqualTo(string.Empty));
            // 期望：DealDamage, GainCharge(+1 from C), GainCharge(-1 spend) = 3
            Assert.That(cmds.Length, Is.EqualTo(3));
            CollectionAssert.AreEqual(before, after);  // Echo 不入隊
        }
    }
}