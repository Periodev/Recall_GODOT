using NUnit.Framework;
using System;
using System.Collections.Generic;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Memory;

[TestFixture]
public class TranslationResultTests
{
    private Actor _player;
    private Actor _enemy;
    private PhaseContext _phase;

    [SetUp]
    public void Setup()
    {
        _player = new Actor(100, apPerTurn: 3, withCharge: true);
        _enemy = new Actor(80, apPerTurn: 3, withCharge: false);
        
        _phase = new PhaseContext();
        _phase.Init();
        _phase.TurnNum = 1;
        _phase.Step = PhaseStep.PlayerInput;
    }

    private bool TryGetActorById(int id, out Actor actor)
    {
        actor = id == 1 ? _enemy : null!;
        return id == 1;
    }

    [Test]
    public void TryTranslateUnified_BasicIntent_ShouldReturnCorrectPlan()
    {
        // Arrange
        _player.AP!.Add(1);
        _player.Charge!.Add(1);
        var intent = new BasicIntent(ActionType.A, 1);
        var memory = new RecallView(new List<ActionType>(), new List<int>());

        // Act
        var result = Translator.TryTranslate(
            intent, _phase, memory, TryGetActorById, _player);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Plan, Is.InstanceOf<BasicPlan>());
        
        var basicPlan = (BasicPlan)result.Plan;
        Assert.That(basicPlan.Act, Is.EqualTo(ActionType.A));
        Assert.That(basicPlan.Source, Is.EqualTo(_player));
        Assert.That(basicPlan.Target, Is.EqualTo(_enemy));
        Assert.That(basicPlan.Damage, Is.EqualTo(6)); // 5 base + 1 charge bonus
        Assert.That(basicPlan.ChargeCost, Is.EqualTo(1));
        Assert.That(basicPlan.APCost, Is.EqualTo(1));
    }
    
    [Test] 
    public void TryTranslateUnified_InvalidTarget_ShouldFail()
    {
        // Arrange
        _player.AP!.Add(1);
        var intent = new BasicIntent(ActionType.A, 99); // 無效目標
        var memory = new RecallView(new List<ActionType>(), new List<int>());

        // Act
        var result = Translator.TryTranslate(
            intent, _phase, memory, TryGetActorById, _player);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(FailCode.BadTarget));
    }

    [Test]
    public void TryTranslateUnified_RecallIntent_ShouldReturnCorrectPlan()
    {
        // Arrange
        _player.AP!.Add(1);
        var ops = new List<ActionType> { ActionType.A, ActionType.B };
        var turns = new List<int> { 0, 0 };
        var memory = new RecallView(ops, turns);
        var intent = new RecallIntent(new int[] { 0, 1 });

        // Act
        var result = Translator.TryTranslate(
            intent, _phase, memory, TryGetActorById, _player);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Plan, Is.InstanceOf<RecallPlan>());
        
        var recallPlan = (RecallPlan)result.Plan;
        Assert.That(recallPlan.Source, Is.EqualTo(_player));
        Assert.That(recallPlan.ActionSequence, Is.EqualTo(new ActionType[] { ActionType.A, ActionType.B }));
        Assert.That(recallPlan.APCost, Is.EqualTo(1));
    }

    [Test]
    public void InterOps_Build_ShouldWorkWithUnifiedPlan()
    {
        // Arrange
        _player.AP!.Add(1);
        _player.Charge!.Add(1);
        var intent = new BasicIntent(ActionType.A, 1);
        var memory = new RecallView(new List<ActionType>(), new List<int>());

        // Act
        var translationResult = Translator.TryTranslate(
            intent, _phase, memory, TryGetActorById, _player);
        
        Assert.That(translationResult.Success, Is.True);
        
        var commands = InterOps.Build(translationResult.Plan);

        // Assert
        Assert.That(commands, Is.Not.Null);
        Assert.That(commands.Length, Is.GreaterThan(0));
        // Should have ConsumeAP, ConsumeCharge, and DealDamage commands
        Assert.That(commands.Length, Is.EqualTo(3));
    }
}