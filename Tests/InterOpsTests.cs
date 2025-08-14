// Tests/InterOpsTests.cs
using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Command;

[TestFixture]
public class InterOpsTests
{
    private Actor _player;
    private Actor _enemy;
    private InterOps _ops;

    [SetUp]
    public void Setup()
    {
        _player = new Actor(maxHP: 30, apPerTurn: 3, withCharge: true);
        _enemy  = new Actor(maxHP: 30, apPerTurn: 0, withCharge: false);
        _ops = new InterOps();
    }

    /// <summary>
    /// 驗證並跳過第一個 AP 命令：
    /// - 必為 ConsumeAP
    /// - Source/Target 為同一個執行者
    /// - Value 等於期望 AP 值（可為 0）
    /// 回傳剩餘命令序列以供後續斷言。
    /// </summary>
    private static AtomicCmd[] SkipAP(AtomicCmd[] cmds, int expectedAP, Actor expectedSrc)
    {
        Assert.GreaterOrEqual(cmds.Length, 1);
        Assert.AreEqual(CmdType.ConsumeAP, cmds[0].Type);
        Assert.AreSame(expectedSrc, cmds[0].Source);
        Assert.AreSame(expectedSrc, cmds[0].Target);
        Assert.AreEqual(expectedAP, cmds[0].Value);
        return cmds.Skip(1).ToArray();
    }

    // ---------- Basic ----------

    [Test]
    public void BuildBasic_A_WithTryConsume_NoCharge_StillDealsDamage()
    {
        // Arrange: damage=5, chargeCost=1，但玩家當前 Charge=0
        var plan = new BasicPlan(
            act: ActionType.A, src: _player, dst: _enemy,
            damage: 5, block: 0, chargeCost: 1, gainAmount: 0, apCost: 1
        );

        // Act
        var cmds = _ops.BuildBasic(plan);
        var body = SkipAP(cmds, expectedAP: 1, expectedSrc: _player);

        // Assert: 之後應為 ConsumeCharge(1) → DealDamage(5)
        Assert.AreEqual(2, body.Length);

        Assert.AreEqual(CmdType.ConsumeCharge, body[0].Type);
        Assert.AreSame(_player, body[0].Target);
        Assert.AreEqual(1, body[0].Value);

        Assert.AreEqual(CmdType.DealDamage, body[1].Type);
        Assert.AreSame(_player, body[1].Source);
        Assert.AreSame(_enemy,  body[1].Target);
        Assert.AreEqual(5, body[1].Value);
    }

    [Test]
    public void BuildBasic_A_WithTryConsume_HasCharge_OrderAndParamsCorrect()
    {
        // Arrange: 假設 Translator 已把加成算入 damage
        var plan = new BasicPlan(
            act: ActionType.A, src: _player, dst: _enemy,
            damage: 8, block: 0, chargeCost: 1, gainAmount: 0, apCost: 2
        );

        var cmds = _ops.BuildBasic(plan);
        var body = SkipAP(cmds, expectedAP: 2, expectedSrc: _player);

        Assert.AreEqual(2, body.Length);

        Assert.AreEqual(CmdType.ConsumeCharge, body[0].Type);
        Assert.AreSame(_player, body[0].Target);
        Assert.AreEqual(1, body[0].Value);

        Assert.AreEqual(CmdType.DealDamage, body[1].Type);
        Assert.AreEqual(8, body[1].Value);
    }

    [Test]
    public void BuildBasic_B_WithTryConsume_AddsShieldThenNoExtra()
    {
        var plan = new BasicPlan(
            act: ActionType.B, src: _player, dst: _player,
            damage: 0, block: 6, chargeCost: 1, gainAmount: 0, apCost: 1
        );

        var cmds = _ops.BuildBasic(plan);
        var body = SkipAP(cmds, expectedAP: 1, expectedSrc: _player);

        Assert.AreEqual(2, body.Length);

        Assert.AreEqual(CmdType.ConsumeCharge, body[0].Type);
        Assert.AreSame(_player, body[0].Target);
        Assert.AreEqual(1, body[0].Value);

        Assert.AreEqual(CmdType.AddShield, body[1].Type);
        Assert.AreSame(_player, body[1].Target);
        Assert.AreEqual(6, body[1].Value);
    }

    [Test]
    public void BuildBasic_C_GainChargeOnly_NoConsumeOrDamage()
    {
        var plan = new BasicPlan(
            act: ActionType.C, src: _player, dst: _player,
            damage: 0, block: 0, chargeCost: 0, gainAmount: 2, apCost: 1
        );

        var cmds = _ops.BuildBasic(plan);
        var body = SkipAP(cmds, expectedAP: 1, expectedSrc: _player);

        Assert.AreEqual(1, body.Length);
        Assert.AreEqual(CmdType.GainCharge, body[0].Type);
        Assert.AreEqual(2, body[0].Value);
    }

    // ---------- AP Cost 專項 ----------

    [Test]
    public void BuildBasic_A_AlwaysConsumesAP_FirstCmd()
    {
        var plan = new BasicPlan(
            act: ActionType.A, src: _player, dst: _enemy,
            damage: 5, block: 0, chargeCost: 0, gainAmount: 0, apCost: 2
        );

        var cmds = _ops.BuildBasic(plan);

        Assert.GreaterOrEqual(cmds.Length, 1);
        Assert.AreEqual(CmdType.ConsumeAP, cmds[0].Type);
        Assert.AreSame(_player, cmds[0].Source);
        Assert.AreSame(_player, cmds[0].Target);
        Assert.AreEqual(2, cmds[0].Value);
    }

    [Test]
    public void BuildBasic_A_WithZeroAPCost_StillEmitsConsumeAP_Zero()
    {
        var plan = new BasicPlan(
            act: ActionType.A, src: _player, dst: _enemy,
            damage: 4, block: 0, chargeCost: 0, gainAmount: 0, apCost: 0
        );

        var cmds = _ops.BuildBasic(plan);

        Assert.GreaterOrEqual(cmds.Length, 1);
        Assert.AreEqual(CmdType.ConsumeAP, cmds[0].Type);
        Assert.AreSame(_player, cmds[0].Source);
        Assert.AreSame(_player, cmds[0].Target);
        Assert.AreEqual(0, cmds[0].Value);

        var body = cmds.Skip(1).ToArray();
        Assert.AreEqual(1, body.Length);
        Assert.AreEqual(CmdType.DealDamage, body[0].Type);
        Assert.AreEqual(4, body[0].Value);
    }

    // ---------- Recall ----------

    [Test]
    public void BuildRecall_BatchOnceConsume_AttackThenBlock()
    {
        // Arrange: 批次一次扣 1 點，然後打5、擋6
        var items = new List<RecallItemPlan>
        {
            new RecallItemPlan(ActionType.A, damage:5, block:0,  chargeCost:0, gainAmount:0),
            new RecallItemPlan(ActionType.B,  damage:0, block:6,  chargeCost:0, gainAmount:0),
        };
        var plan = new RecallPlan(_player, _enemy, items, batchChargeCost: 1, apCost: 1);

        var cmds = _ops.BuildRecall(plan);
        var body = SkipAP(cmds, expectedAP: 1, expectedSrc: _player);

        // Assert: 先批次 ConsumeCharge(1)，再 DealDamage(5) → AddShield(6)
        Assert.AreEqual(3, body.Length);

        Assert.AreEqual(CmdType.ConsumeCharge, body[0].Type);
        Assert.AreEqual(1, body[0].Value);

        Assert.AreEqual(CmdType.DealDamage, body[1].Type);
        Assert.AreSame(_player, body[1].Source);
        Assert.AreSame(_enemy,  body[1].Target);
        Assert.AreEqual(5, body[1].Value);

        Assert.AreEqual(CmdType.AddShield, body[2].Type);
        Assert.AreSame(_player, body[2].Target); // 新的期望：B 加盾給自己
        Assert.AreEqual(6, body[2].Value);
    }

    [Test]
    public void BuildRecall_PerItemConsume_AttackAndBlockEachTryConsume()
    {
        var items = new List<RecallItemPlan>
        {
            new RecallItemPlan(ActionType.A, damage:5, block:0, chargeCost:1, gainAmount:0),
            new RecallItemPlan(ActionType.B,  damage:0, block:6, chargeCost:1, gainAmount:0),
            new RecallItemPlan(ActionType.C, damage:0, block:0, chargeCost:0, gainAmount:2),
        };
        var plan = new RecallPlan(_player, _enemy, items, batchChargeCost: 0, apCost: 3);

        var cmds = _ops.BuildRecall(plan);
        var body = SkipAP(cmds, expectedAP: 3, expectedSrc: _player);

        // 序列：Consume(1), DealDamage(5), Consume(1), AddShield(6), GainCharge(2)
        Assert.AreEqual(5, body.Length);

        Assert.AreEqual(CmdType.ConsumeCharge, body[0].Type);
        Assert.AreEqual(1, body[0].Value);

        Assert.AreEqual(CmdType.DealDamage, body[1].Type);
        Assert.AreEqual(5, body[1].Value);

        Assert.AreEqual(CmdType.ConsumeCharge, body[2].Type);
        Assert.AreEqual(1, body[2].Value);

        Assert.AreEqual(CmdType.AddShield, body[3].Type);
        Assert.AreEqual(6, body[3].Value);

        Assert.AreEqual(CmdType.GainCharge, body[4].Type);
        Assert.AreEqual(2, body[4].Value);
    }

    [Test]
    public void BuildRecall_AlwaysConsumesAP_FirstCmd()
    {
        var items = new List<RecallItemPlan>
        {
            new RecallItemPlan(ActionType.A, damage: 5)
        };
        var plan = new RecallPlan(_player, _enemy, items, batchChargeCost: 0, apCost: 3);

        var cmds = _ops.BuildRecall(plan);

        Assert.GreaterOrEqual(cmds.Length, 1);
        Assert.AreEqual(CmdType.ConsumeAP, cmds[0].Type);
        Assert.AreSame(_player, cmds[0].Source);
        Assert.AreSame(_player, cmds[0].Target);
        Assert.AreEqual(3, cmds[0].Value);
    }

    [Test]
    public void BuildRecall_WithZeroAPCost_StillEmitsConsumeAP_Zero()
    {
        var items = new List<RecallItemPlan>
        {
            new RecallItemPlan(ActionType.C, gainAmount: 2)
        };
        var plan = new RecallPlan(_player, _enemy, items, batchChargeCost: 0, apCost: 0);

        var cmds = _ops.BuildRecall(plan);

        Assert.GreaterOrEqual(cmds.Length, 1);
        Assert.AreEqual(CmdType.ConsumeAP, cmds[0].Type);
        Assert.AreSame(_player, cmds[0].Source);
        Assert.AreSame(_player, cmds[0].Target);
        Assert.AreEqual(0, cmds[0].Value);

        var body = cmds.Skip(1).ToArray();
        Assert.AreEqual(1, body.Length);
        Assert.AreEqual(CmdType.GainCharge, body[0].Type);
        Assert.AreEqual(2, body[0].Value);
    }
}
