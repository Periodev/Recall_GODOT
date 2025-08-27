
using System;
using System.Collections.Generic;
using System.Linq;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Command;
using CombatCore.Memory;
using static CombatCore.GameConst;

public abstract record HLAIntent(int? TargetId);
public sealed record BasicIntent(ActionType Act, int? TargetId) : HLAIntent(TargetId);
public sealed record RecallIntent(int[] RecallIndices, int? TargetId) : HLAIntent(TargetId);
public delegate bool TryGetActorById(int id, out Actor actor);

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

	public static bool HasAP(this Actor actor, int cost) =>
		(actor.AP?.Value ?? 0) >= cost;
}

public sealed class HLATranslator
{
	// 單一入口：輸入抽象意圖，型別模式分派
	public FailCode TryTranslate(
		HLAIntent intent,
		PhaseContext phase,
		RecallView memory,
		TryGetActorById tryGetActor,   // ← 取代 IActorLookup,
		Actor self,
		out BasicPlan basicPlan,
		out RecallPlan recallPlan)
	{
		basicPlan = default; recallPlan = default;

		// 通用前置檢查
		if (!self.IsAlive) { return FailCode.SelfDead; }

		return intent switch
		{
			BasicIntent bi  => TryBasic(bi, phase, self, tryGetActor, out basicPlan),
			RecallIntent ri => TryRecall(ri, phase, memory, self, tryGetActor, out recallPlan),
			_ =>FailCode.UnknownIntent
		};
	}

	private static FailCode TryBasic(
		BasicIntent bi, PhaseContext phase, Actor self, TryGetActorById tryGetActor,
		out BasicPlan plan)
	{
		plan = default;

		// 嚴格目標驗證
		var tgt = ResolveTarget(bi.TargetId, tryGetActor);
		if (bi.Act == ActionType.A)
		{
			// A 必須有有效且非 self 的目標
			if (tgt is null || ReferenceEquals(tgt, self))
				return FailCode.BadTarget;
		}
		else
		{
			// B/C 一律自我，忽略傳入 TargetId
			tgt = self;
		}
		
		// 計算數值（集中管理）
		var numbers = ComputeBasicNumbers(bi.Act, self);
		
		// AP 檢查：只檢查不扣，真正扣除在 AtomicCmd.ConsumeAP
		if (numbers.APCost > 0 && !self.HasAP(numbers.APCost))
			return FailCode.NoAP;
		
		// 動態決定此次可用的 Charge 數量與加成
		int use = 0;
		if (bi.Act == ActionType.A || bi.Act == ActionType.B)
			use = Math.Min(self.Charge?.Value ?? 0, CHARGE_MAX_PER_ACTION);

		int dmg = numbers.Damage + (bi.Act == ActionType.A ? A_BONUS_PER_CHARGE * use : 0);
		int blk = numbers.Block  + (bi.Act == ActionType.B ? B_BONUS_PER_CHARGE * use : 0);
		
		int finalDmg = dmg;   
		int finalBlk = blk;   
		int chargeCostThisAction = use;

		plan = new BasicPlan(bi.Act, self, tgt, 
			damage: finalDmg, block: finalBlk, chargeCost: chargeCostThisAction, numbers.GainAmount, numbers.APCost);

		return FailCode.None;
	}

	private static FailCode TryRecall(
		RecallIntent ri, PhaseContext phase, RecallView memory, Actor self, TryGetActorById tryGetActor,
		out RecallPlan plan)
	{
		plan = default;

		// 一次/回合檢查
		if (RecallUsedThisTurn(phase)) return FailCode.RecallUsed;


		// 索引合法性檢查（包含空集合檢查）
		FailCode fail = ValidateIndices(ri.RecallIndices, memory, phase.TurnNum);
		if (fail != FailCode.None) return fail;

		// 檢查是否含有 A 動作
		bool hasAttackAction = ri.RecallIndices.Any(idx => memory.Ops[idx] == ActionType.A);
		
		// 目標驗證：若含 A 動作，必須有有效且非 self 的目標
		Actor tgt;
		if (hasAttackAction)
		{
			tgt = ResolveTarget(ri.TargetId, tryGetActor);
			if (tgt is null || ReferenceEquals(tgt, self))
				return FailCode.BadTarget;
		}
		else
		{
			// 全為 B/C，設定目標為自己
			tgt = self;
		}

		// 映射記憶項目到計畫
		var items = new List<RecallItemPlan>();
		
		foreach (var idx in ri.RecallIndices)
		{
			var op = memory.Ops[idx];
			var itemNumbers = ComputeBasicNumbers(op, self);
			
			items.Add(new RecallItemPlan(op, 
				itemNumbers.Damage, itemNumbers.Block, 0, itemNumbers.GainAmount));
		}

		// AP 檢查
		// Recall 的 AP cost: 如果角色有 AP 系統，則消耗 1，否則為 0
		int apCost = (self.AP != null) ? 1 : 0;
		if (apCost > 0 && !self.HasAP(apCost)) return FailCode.NoAP;


		// 使用逐項扣費：batchChargeCost = 0
		plan = new RecallPlan(self, tgt, items, batchChargeCost: 0, apCost);
		return FailCode.None;
	}

	// 數值計算集中處理
	private static (int Damage, int Block, int GainAmount, int ChargeCost, int APCost) ComputeBasicNumbers(ActionType act, Actor self)
	{
		// 如果角色有 AP 系統，則消耗 1，否則為 0
		int apCost = (self.AP != null) ? 1 : 0;
		return act switch
		{
			ActionType.A => (Damage: 5, Block: 0, GainAmount: 0, ChargeCost: 0, APCost: apCost),
			ActionType.B => (Damage: 0, Block: 3, GainAmount: 0, ChargeCost: 0, APCost: apCost),
			ActionType.C => (Damage: 0, Block: 0, GainAmount: 1, ChargeCost: 0, APCost: apCost),
			_ => (0, 0, 0, 0, apCost)
		};
	}

	// 索引驗證
	private static FailCode ValidateIndices(int[] indices, RecallView memory, int currentTurn)
	{
		
		// 空索引防呆：避免花 1 AP 做空操作
		if (indices.Length == 0) return FailCode.BadIndex  ;
		
		// 檢查索引範圍和重複
		if (indices.Any(idx => idx < 0 || idx >= memory.Count) || 
			indices.Distinct().Count() != indices.Length)
		{
			return FailCode.IndexOutOfBound;
		}

		// 排除本回合：檢查是否引用當前回合的記憶
		if (indices.Any(idx => memory.Turns[idx] == currentTurn))
		{
			return FailCode.IndexLimited;
		}

		return FailCode.None;
	}

	// 輔助方法
	private static Actor? ResolveTarget(int? id, TryGetActorById tryGetActor) =>
		id.HasValue && tryGetActor(id.Value, out var a) ? a : null;

	private static bool RecallUsedThisTurn(PhaseContext phase) => phase.RecallUsedThisTurn;
}
