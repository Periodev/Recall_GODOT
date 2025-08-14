
using System;
using System.Collections.Generic;
using System.Linq;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Abstractions;
using CombatCore.Command;
using CombatCore.Memory;

public abstract record HLAIntent(int? TargetId);
public sealed record BasicIntent(ActionType Act, int? TargetId) : HLAIntent(TargetId);
public sealed record RecallIntent(int[] RecallIndices, int? TargetId) : HLAIntent(TargetId);

public readonly struct RecallView
{
	public RecallView(IReadOnlyList<ActionType> ops, IReadOnlyList<int> turns)
	{
		Ops = ops ?? Array.Empty<ActionType>();
		Turns = turns ?? Array.Empty<int>();
	}
	
	public IReadOnlyList<ActionType> Ops { get; }
	public IReadOnlyList<int> Turns { get; }
	public int Count => Ops.Count;
}

// Extension method for Actor to ensure null-safe HasCharge
public static class ActorExtensions
{
	public static bool HasCharge(this Actor actor, int cost) =>
		(actor.Charge?.Value ?? 0) >= cost;
}

public sealed class HLATranslator
{
	// 單一入口：輸入抽象意圖，型別模式分派
	public bool TryTranslate(
		HLAIntent intent,
		PhaseContext phase,
		RecallView memory,
		IActorLookup actors,
		Actor self,
		out BasicPlan basicPlan,
		out RecallPlan recallPlan,
		out string fail)
	{
		basicPlan = default; recallPlan = default; fail = string.Empty;

		// 通用前置檢查
		if (!self.IsAlive) { fail = "self dead"; return false; }

		return intent switch
		{
			BasicIntent bi  => TryBasic(bi, phase, self, actors, out basicPlan, out fail),
			RecallIntent ri => TryRecall(ri, phase, memory, self, actors, out recallPlan, out fail),
			_ => Fail("unknown intent", out fail)
		};
	}

	private static bool TryBasic(
		BasicIntent bi, PhaseContext phase, Actor self, IActorLookup actors,
		out BasicPlan plan, out string fail)
	{
		plan = default; fail = string.Empty;

		// Phase 檢查
		if (!CanPlayerAct(phase)) return Fail("phase locked", out fail);

		// 嚴格目標驗證
		var tgt = ResolveTarget(bi.TargetId, actors);
		if (bi.Act == ActionType.A)
		{
			// A 必須有有效且非 self 的目標
			if (tgt is null || ReferenceEquals(tgt, self))
				return Fail("bad target", out fail);
		}
		else
		{
			// B/C 一律自我，忽略傳入 TargetId
			tgt = self;
		}
		
		// 計算數值（集中管理）
		var numbers = ComputeBasicNumbers(bi.Act);
		
		// AP 檢查：只檢查不扣，真正扣除在 AtomicCmd.ConsumeAP
		if (numbers.APCost > 0 && !self.HasAP(numbers.APCost))
			return Fail("no ap", out fail);
		
		// Charge 檢查：A/B 動作需要檢查 Charge
		if (numbers.ChargeCost > 0 && !self.HasCharge(numbers.ChargeCost))
			return Fail("no charge", out fail);

		plan = new BasicPlan(bi.Act, self, tgt, 
			numbers.Damage, numbers.Block, numbers.ChargeCost, numbers.GainAmount, numbers.APCost);
		return true;
	}

	private static bool TryRecall(
		RecallIntent ri, PhaseContext phase, RecallView memory, Actor self, IActorLookup actors,
		out RecallPlan plan, out string fail)
	{
		plan = default; fail = string.Empty;

		// Phase 檢查
		if (!CanPlayerAct(phase)) return Fail("phase locked", out fail);

		// 一次/回合檢查
		if (RecallUsedThisTurn(phase)) return Fail("recall used", out fail);

		// 索引合法性檢查（包含空集合檢查）
		if (!ValidateIndices(ri.RecallIndices, memory, phase.TurnNum, out fail))
			return false;

		// 檢查是否含有 A 動作
		bool hasAttackAction = ri.RecallIndices.Any(idx => memory.Ops[idx] == ActionType.A);
		
		// 目標驗證：若含 A 動作，必須有有效且非 self 的目標
		Actor tgt;
		if (hasAttackAction)
		{
			tgt = ResolveTarget(ri.TargetId, actors);
			if (tgt is null || ReferenceEquals(tgt, self))
				return Fail("bad target", out fail);
		}
		else
		{
			// 全為 B/C，設定目標為自己
			tgt = self;
		}

		// 映射記憶項目到計畫
		var items = new List<RecallItemPlan>();
		int totalChargeCost = 0;
		
		foreach (var idx in ri.RecallIndices)
		{
			var op = memory.Ops[idx];
			var itemNumbers = ComputeBasicNumbers(op);
			
			// 逐項扣費策略：A/B 各扣 1 Charge
			int itemChargeCost = (op == ActionType.A || op == ActionType.B) ? 1 : 0;
			totalChargeCost += itemChargeCost;
			
			items.Add(new RecallItemPlan(op, 
				itemNumbers.Damage, itemNumbers.Block, itemChargeCost, itemNumbers.GainAmount));
		}

		// Charge 檢查：檢查總需求量
		if (totalChargeCost > 0 && !self.HasCharge(totalChargeCost))
			return Fail("no charge", out fail);

		// AP 檢查
		const int apCost = 1;
		if (!self.HasAP(apCost))
			return Fail("no ap", out fail);

		// 使用逐項扣費：batchChargeCost = 0
		plan = new RecallPlan(self, tgt, items, batchChargeCost: 0, apCost);
		return true;
	}

	// 數值計算集中處理
	private static (int Damage, int Block, int GainAmount, int ChargeCost, int APCost) ComputeBasicNumbers(ActionType act)
	{
		return act switch
		{
			ActionType.A => (Damage: 5, Block: 0, GainAmount: 0, ChargeCost: 1, APCost: 1),
			ActionType.B => (Damage: 0, Block: 6, GainAmount: 0, ChargeCost: 1, APCost: 1),
			ActionType.C => (Damage: 0, Block: 0, GainAmount: 2, ChargeCost: 0, APCost: 1),
			_ => (0, 0, 0, 0, 1)
		};
	}

	// 索引驗證
	private static bool ValidateIndices(int[] indices, RecallView memory, int currentTurn, out string fail)
	{
		fail = string.Empty;
		
		// 空索引防呆：避免花 1 AP 做空操作
		if (indices.Length == 0)
		{
			fail = "bad indices";
			return false;
		}
		
		// 檢查索引範圍和重複
		if (indices.Any(idx => idx < 0 || idx >= memory.Count) || 
			indices.Distinct().Count() != indices.Length)
		{
			fail = "bad indices";
			return false;
		}

		// 排除本回合：檢查是否引用當前回合的記憶
		if (indices.Any(idx => memory.Turns[idx] == currentTurn))
		{
			fail = "bad indices";
			return false;
		}

		return true;
	}

	// 輔助方法
	private static Actor? ResolveTarget(int? id, IActorLookup actors) =>
		id.HasValue ? actors.GetById(id.Value) : null;

	private static bool Fail(string msg, out string fail) { fail = msg; return false; }

	// Phase 系統整合點
	private static bool CanPlayerAct(PhaseContext phase)
	{
		// 檢查是否為玩家行動階段
		return phase.Step == PhaseStep.PlayerInput;
	}

	private static bool RecallUsedThisTurn(PhaseContext phase)
	{
		// 檢查本回合是否已使用 Recall
		return phase.RecallUsedThisTurn;
	}
}
