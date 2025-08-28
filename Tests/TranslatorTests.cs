using NUnit.Framework;
using System;
using System.Collections.Generic;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Abstractions;
using CombatCore.Memory;
using CombatCore.Command;

[TestFixture]
public class TranslatorTests
{
	private Translator _translator;
	private TestActorLookup _actorLookup;
	private Actor _player;
	private Actor _enemy;
	private PhaseContext _phase;
	private InterOps _interOps;

	[SetUp]
	public void Setup()
	{
		_translator = new Translator();
		_interOps = new InterOps();
		_player = new Actor(100, apPerTurn: 3, withCharge: true);
		_enemy = new Actor(80, apPerTurn: 3, withCharge: false);
		_actorLookup = new TestActorLookup(_player, _enemy);
		
		_phase = new PhaseContext();
		_phase.Init();
		_phase.TurnNum = 1;
		_phase.Step = PhaseStep.PlayerInput;
	}

	#region Basic Intent Tests

	[Test]
	public void Basic_A_Success_WhenApAndChargeEnough()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		var intent = new BasicIntent(ActionType.A, TargetId: 1);
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out BasicPlan basicPlan, out RecallPlan recallPlan, out string fail);

		// Assert
		Assert.IsTrue(result);
		Assert.AreEqual(string.Empty, fail);
		Assert.AreEqual(ActionType.A, basicPlan.Act);
		Assert.AreEqual(_player, basicPlan.Source);
		Assert.AreEqual(_enemy, basicPlan.Target);
		Assert.AreEqual(5, basicPlan.Damage);
		Assert.AreEqual(1, basicPlan.ChargeCost);
		Assert.AreEqual(1, basicPlan.APCost);
	}

	[Test]
	public void Basic_A_Fail_WhenTargetMissingOrSelf()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		// Test 1: No target specified
		var intentNoTarget = new BasicIntent(ActionType.A, TargetId: null);
		bool result1 = _translator.TryTranslate(intentNoTarget, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail1);
		Assert.IsFalse(result1);
		Assert.AreEqual("bad target", fail1);

		// Test 2: Invalid target ID
		var intentInvalidTarget = new BasicIntent(ActionType.A, TargetId: 999);
		bool result2 = _translator.TryTranslate(intentInvalidTarget, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail2);
		Assert.IsFalse(result2);
		Assert.AreEqual("bad target", fail2);

		// Test 3: Target is self
		var intentSelfTarget = new BasicIntent(ActionType.A, TargetId: 0);
		bool result3 = _translator.TryTranslate(intentSelfTarget, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail3);
		Assert.IsFalse(result3);
		Assert.AreEqual("bad target", fail3);
	}

	[Test]
	public void Basic_A_Fail_NoCharge()
	{
		// Arrange - A action requires charge
		_player.AP.Add(1);
		_player.Charge.Clear(); // No charge
		var intent = new BasicIntent(ActionType.A, TargetId: 1);
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("no charge", fail);
	}

	[Test]
	public void Basic_B_AlwaysTargetsSelf()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		var intent = new BasicIntent(ActionType.B, TargetId: 1); // Try to target enemy
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out BasicPlan basicPlan, out _, out string fail);

		// Assert
		Assert.IsTrue(result);
		Assert.AreEqual(string.Empty, fail);
		Assert.AreEqual(_player, basicPlan.Target); // Should target self, ignore enemy ID
		Assert.AreEqual(ActionType.B, basicPlan.Act);
		Assert.AreEqual(6, basicPlan.Block);
	}

	[Test]
	public void Basic_B_Fail_NoCharge()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Clear(); // No charge
		var intent = new BasicIntent(ActionType.B, TargetId: null);
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("no charge", fail);
	}

	[Test]
	public void Basic_C_AlwaysTargetsSelf_IgnoresCharge()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Clear(); // No charge - should not matter for C
		var intent = new BasicIntent(ActionType.C, TargetId: 1); // Try to target enemy
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out BasicPlan basicPlan, out _, out string fail);

		// Assert
		Assert.IsTrue(result);
		Assert.AreEqual(string.Empty, fail);
		Assert.AreEqual(_player, basicPlan.Target); // Should target self
		Assert.AreEqual(ActionType.C, basicPlan.Act);
		Assert.AreEqual(2, basicPlan.GainAmount);
		Assert.AreEqual(0, basicPlan.ChargeCost); // C doesn't cost charge
	}

	[Test]
	public void Basic_Fail_NoAp()
	{
		// Arrange
		_player.AP.Clear(); // No AP
		_player.Charge.Add(1);
		var intent = new BasicIntent(ActionType.A, TargetId: 1);
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("no ap", fail);
	}

	[Test]
	public void Basic_Fail_PhaseLockedWhenNotPlayerInput()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		_phase.Step = PhaseStep.EnemyInit; // Wrong phase
		var intent = new BasicIntent(ActionType.A, TargetId: 1);
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("phase locked", fail);
	}

	#endregion

	#region Recall Intent Tests

	[Test]
	public void Recall_A_Fail_WhenTargetMissing()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		var memory = new RecallView(new List<ActionType> { ActionType.A }, new List<int> { 0 });
		_phase.TurnNum = 1;

		// Test 1: No target for A
		var intentNoTarget = new RecallIntent(new int[] { 0 }, TargetId: null);
		bool result1 = _translator.TryTranslate(intentNoTarget, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail1);
		Assert.IsFalse(result1);
		Assert.AreEqual("bad target", fail1);

		// Test 2: Invalid target
		var intentInvalidTarget = new RecallIntent(new int[] { 0 }, TargetId: 999);
		bool result2 = _translator.TryTranslate(intentInvalidTarget, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail2);
		Assert.IsFalse(result2);
		Assert.AreEqual("bad target", fail2);

		// Test 3: Self target
		var intentSelfTarget = new RecallIntent(new int[] { 0 }, TargetId: 0);
		bool result3 = _translator.TryTranslate(intentSelfTarget, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail3);
		Assert.IsFalse(result3);
		Assert.AreEqual("bad target", fail3);
	}

	[Test]
	public void Recall_Success_OnlyBC_TargetsSelf()
	{
		// Arrange - Recall with only B/C actions, no target needed
		_player.AP.Add(1);
		_player.Charge.Add(2);
		var memory = new RecallView(
			new List<ActionType> { ActionType.B, ActionType.C }, 
			new List<int> { 0, 0 });
		_phase.TurnNum = 1;
		var intent = new RecallIntent(new int[] { 0, 1 }, TargetId: null);

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out RecallPlan recallPlan, out string fail);

		// Assert
		Assert.IsTrue(result);
		Assert.AreEqual(string.Empty, fail);
		Assert.AreEqual(_player, recallPlan.Target);
		Assert.AreEqual(2, recallPlan.Items.Count);
		Assert.AreEqual(ActionType.B, recallPlan.Items[0].Op);
		Assert.AreEqual(ActionType.C, recallPlan.Items[1].Op);
	}

	[Test]
	public void Recall_Fail_PhaseLocked()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		_phase.Step = PhaseStep.EnemyInit; // Wrong phase
		var memory = new RecallView(new List<ActionType> { ActionType.A }, new List<int> { 0 });
		var intent = new RecallIntent(new int[] { 0 }, TargetId: 1);

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("phase locked", fail);
	}

	[Test]
	public void Recall_Fail_UsedThisTurn()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		_phase.RecallUsedThisTurn = true; // Already used
		var memory = new RecallView(new List<ActionType> { ActionType.A }, new List<int> { 0 });
		var intent = new RecallIntent(new int[] { 0 }, TargetId: 1);

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("recall used", fail);
	}

	[Test]
	public void Recall_Fail_EmptyIndices()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		var intent = new RecallIntent(new int[] { }, TargetId: 1); // Empty indices
		var memory = new RecallView(new List<ActionType> { ActionType.A }, new List<int> { 0 });

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("bad indices", fail);
	}

	[Test]
	public void Recall_Fail_DuplicateIndices()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(2);
		var intent = new RecallIntent(new int[] { 0, 0 }, TargetId: 1); // Duplicate indices
		var memory = new RecallView(new List<ActionType> { ActionType.A }, new List<int> { 0 });

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("bad indices", fail);
	}

	[Test]
	public void Recall_Fail_OutOfBoundsIndices()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		var intent = new RecallIntent(new int[] { 5 }, TargetId: 1); // Index out of bounds
		var memory = new RecallView(new List<ActionType> { ActionType.A }, new List<int> { 0 });

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("bad indices", fail);
	}

	[Test]
	public void Recall_Fail_CurrentTurnIndices()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1);
		_phase.TurnNum = 1;
		var intent = new RecallIntent(new int[] { 0 }, TargetId: 1);
		var memory = new RecallView(new List<ActionType> { ActionType.A }, new List<int> { 1 }); // Same turn

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("bad indices", fail);
	}

	[Test]
	public void Recall_Fail_InsufficientCharge()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(1); // Only 1 charge, but need 2 for A+B
		_phase.TurnNum = 1;
		var intent = new RecallIntent(new int[] { 0, 1 }, TargetId: 1);
		var memory = new RecallView(
			new List<ActionType> { ActionType.A, ActionType.B }, 
			new List<int> { 0, 0 });

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out string fail);

		// Assert
		Assert.IsFalse(result);
		Assert.AreEqual("no charge", fail);
	}

	[Test]
	public void Recall_Success_MapsOpsCorrectly()
	{
		// Arrange
		_player.AP.Add(1);
		_player.Charge.Add(2);
		_phase.TurnNum = 1;
		var intent = new RecallIntent(new int[] { 0, 1 }, TargetId: 1);
		var memory = new RecallView(
			new List<ActionType> { ActionType.A, ActionType.B }, 
			new List<int> { 0, 0 });

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out RecallPlan recallPlan, out string fail);

		// Assert
		Assert.IsTrue(result);
		Assert.AreEqual(string.Empty, fail);
		Assert.AreEqual(_player, recallPlan.Source);
		Assert.AreEqual(_enemy, recallPlan.Target);
		Assert.AreEqual(2, recallPlan.Items.Count);
		Assert.AreEqual(0, recallPlan.BatchChargeCost); // Using individual costs
		
		Assert.AreEqual(ActionType.A, recallPlan.Items[0].Op);
		Assert.AreEqual(5, recallPlan.Items[0].Damage);
		Assert.AreEqual(1, recallPlan.Items[0].ChargeCost);
		
		Assert.AreEqual(ActionType.B, recallPlan.Items[1].Op);
		Assert.AreEqual(6, recallPlan.Items[1].Block);
		Assert.AreEqual(1, recallPlan.Items[1].ChargeCost);
	}

	#endregion

	#region InterOps Zero Value Tests

	[Test]
	public void Basic_A_WithZeroDamage_EmitsNoDealDamage()
	{
		// This would require modifying ComputeBasicNumbers to return 0 damage
		// For now, we test the current behavior where A always has 5 damage
		// If you want to test zero damage, you'd need to create a custom plan
		var plan = new BasicPlan(ActionType.A, _player, _enemy, damage: 0, chargeCost: 1, apCost: 1);
		var commands = _interOps.BuildBasic(plan);
		
		// Should have ConsumeAP, ConsumeCharge, but no DealDamage
		Assert.AreEqual(2, commands.Length);
		Assert.AreEqual(CmdType.ConsumeAP, commands[0].Type);
		Assert.AreEqual(CmdType.ConsumeCharge, commands[1].Type);
		// No DealDamage command should be present
	}

	[Test]
	public void Basic_B_WithZeroBlock_EmitsNoAddShield()
	{
		var plan = new BasicPlan(ActionType.B, _player, _player, block: 0, chargeCost: 1, apCost: 1);
		var commands = _interOps.BuildBasic(plan);
		
		Assert.AreEqual(2, commands.Length);
		Assert.AreEqual(CmdType.ConsumeAP, commands[0].Type);
		Assert.AreEqual(CmdType.ConsumeCharge, commands[1].Type);
		// No AddShield command should be present
	}

	[Test]
	public void Basic_C_WithZeroGain_EmitsNoGainCharge()
	{
		var plan = new BasicPlan(ActionType.C, _player, _player, gainAmount: 0, apCost: 1);
		var commands = _interOps.BuildBasic(plan);
		
		Assert.AreEqual(1, commands.Length);
		Assert.AreEqual(CmdType.ConsumeAP, commands[0].Type);
		// No GainCharge command should be present
	}

	#endregion

	#region B Target Semantics Test

	[Test]
	public void Recall_MixedAB_BTargetsSelf_ATargetsEnemy()
	{
		// Test current behavior: B in Recall adds shield to plan.Target, not plan.Source
		// This documents the current InterOps behavior
		_player.AP.Add(1);
		_player.Charge.Add(2);
		_phase.TurnNum = 1;
		var intent = new RecallIntent(new int[] { 0, 1 }, TargetId: 1);
		var memory = new RecallView(
			new List<ActionType> { ActionType.A, ActionType.B }, 
			new List<int> { 0, 0 });

		// Act
		bool result = _translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out RecallPlan recallPlan, out _);
		
		Assert.IsTrue(result);
		var commands = _interOps.BuildRecall(recallPlan);
		
		// Find the AddShield command (should be index 3: ConsumeAP, ConsumeCharge, DealDamage, ConsumeCharge, AddShield)
		var addShieldCmd = Array.Find(commands, cmd => cmd.Type == CmdType.AddShield);
		Assert.IsNotNull(addShieldCmd);
		
		// Current behavior: B adds shield to plan.Source (self), not plan.Target (enemy)
		Assert.AreEqual(_player, addShieldCmd.Target);
	}

	#endregion

	#region Utility Tests

	[Test]
	public void HasCharge_NullCharge_ReturnsFalse()
	{
		var actorWithoutCharge = new Actor(100, apPerTurn: 3, withCharge: false);
		
		Assert.IsFalse(actorWithoutCharge.HasCharge(1));
		Assert.IsFalse(actorWithoutCharge.HasCharge(0));
	}

	[Test]
	public void Translator_DoesNotMutateActorState()
	{
		int initialAP = _player.AP.Value;
		int initialCharge = _player.Charge.Value;
		
		_player.AP.Add(1);
		_player.Charge.Add(1);
		var intent = new BasicIntent(ActionType.A, TargetId: 1);
		var memory = new RecallView(new List<ActionType>(), new List<int>());

		_translator.TryTranslate(intent, _phase, memory, _actorLookup, _player,
			out _, out _, out _);

		Assert.AreEqual(initialAP + 1, _player.AP.Value);
		Assert.AreEqual(initialCharge + 1, _player.Charge.Value);
	}

	#endregion

	#region AtomicCmd Execution Tests

	[Test]
	public void CmdExecutor_ConsumeAP_Throws_WhenNotEnough()
	{
		// Arrange
		_player.AP.Clear(); // No AP
		var cmd = AtomicCmd.ConsumeAP(_player, 1);
		var executor = new CmdExecutor();

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => {
			executor.ExecuteAll(new[] { cmd });
		});
	}

	#endregion

	// Helper class for testing
	private class TestActorLookup : IActorLookup
	{
		private readonly Actor _player;
		private readonly Actor _enemy;

		public TestActorLookup(Actor player, Actor enemy)
		{
			_player = player;
			_enemy = enemy;
		}

		public Actor GetById(int id)
		{
			return id switch
			{
				0 => _player,
				1 => _enemy,
				_ => null
			};
		}
	}
}