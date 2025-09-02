
using System;
using System.Collections.Generic;
using System.Linq;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Command;
using CombatCore.Recall;
using static CombatCore.GameConst;

public delegate bool TryGetActorById(int id, out Actor actor);
public abstract record Intent(int? TargetId);
public sealed record BasicIntent(ActionType Act, int? TargetId) : Intent(TargetId);
public sealed record RecallIntent(int[] RecallIndices) : Intent((int?)null);
public sealed record EchoIntent(Echo Echo, int? TargetId, int SlotIndex) : Intent(TargetId);

public readonly struct TranslationResult
{
	public bool Success { get; }
	public FailCode ErrorCode { get; }  
	public Plan Plan { get; }
	public Intent OriginalIntent { get; }
	
	private TranslationResult(bool success, FailCode errorCode, Plan plan, Intent originalIntent)
	{
		Success = success;
		ErrorCode = errorCode;
		Plan = plan;
		OriginalIntent = originalIntent;
	}
	
	public static TranslationResult Pass(Plan plan, Intent intent) =>
		new(true, FailCode.None, plan, intent);
		
	public static TranslationResult Fail(FailCode code) =>
		new(false, code, null!, null!);
}

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

public sealed class Translator
{
	public static TranslationResult TryTranslate(
		Intent intent,
		CombatState state,
		Actor self)
	{
		// 通用前置檢查
		if (!self.IsAlive) 
			return TranslationResult.Fail(FailCode.SelfDead);
		
		// 分派到對應處理方法
		return intent switch
		{

			BasicIntent bi => TranslateBasicIntentInternal(bi, state.PhaseCtx, state.TryGetActor, self),
			RecallIntent ri => TranslateRecallIntentInternal(ri, state, self),
			EchoIntent ei => TranslateEchoIntentInternal(ei, state.PhaseCtx, state.TryGetActor, self),
			_ => TranslationResult.Fail(FailCode.UnknownIntent)
		};
	}

	private static FailCode TryBasic(
		BasicIntent bi, PhaseContext phase, Actor self, TryGetActorById tryGetActor,
		out BasicPlan plan)
	{
		plan = null!;

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

		plan = new BasicPlan(bi.Act, self, tgt, finalDmg, finalBlk, chargeCostThisAction, numbers.GainAmount, numbers.APCost);

		return FailCode.None;
	}

	private static FailCode TryRecall(
		RecallIntent ri, PhaseContext phase, RecallView memory, Actor self,
		out RecallPlan plan)
	{
		plan = null!;

		// 一次/回合檢查
		if (RecallUsedThisTurn(phase)) return FailCode.RecallUsed;


		// 索引合法性檢查（包含空集合檢查）
		FailCode fail = ValidateIndices(ri.RecallIndices, memory, phase.TurnNum);
		if (fail != FailCode.None) return fail;


		// AP 檢查
		// Recall 的 AP cost: 如果角色有 AP 系統，則消耗 1，否則為 0
		int apCost = (self.AP != null) ? 1 : 0;
		if (apCost > 0 && !self.HasAP(apCost)) return FailCode.NoAP;

		var sequence = ri.RecallIndices.Select(idx => memory.Ops[idx]).ToArray();
		plan = new RecallPlan(self, sequence, apCost);

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

	private static TranslationResult TranslateBasicIntentInternal(
		BasicIntent intent, PhaseContext phase, TryGetActorById tryGetActor, Actor self)
	{
		// 嚴格目標驗證
		var tgt = ResolveTarget(intent.TargetId, tryGetActor);
		if (intent.Act == ActionType.A)
		{
			// A 必須有有效且非 self 的目標
			if (tgt is null || ReferenceEquals(tgt, self))
				return TranslationResult.Fail(FailCode.BadTarget);
		}
		else
		{
			// B/C 一律自我，忽略傳入 TargetId
			tgt = self;
		}
		
		// 計算數值（集中管理）
		var numbers = ComputeBasicNumbers(intent.Act, self);
		
		// AP 檢查：只檢查不扣，真正扣除在 AtomicCmd.ConsumeAP
		if (numbers.APCost > 0 && !self.HasAP(numbers.APCost))
			return TranslationResult.Fail(FailCode.NoAP);
		
		// 動態決定此次可用的 Charge 數量與加成
		int use = 0;
		if (intent.Act == ActionType.A || intent.Act == ActionType.B)
			use = Math.Min(self.Charge?.Value ?? 0, CHARGE_MAX_PER_ACTION);

		int dmg = numbers.Damage + (intent.Act == ActionType.A ? A_BONUS_PER_CHARGE * use : 0);
		int blk = numbers.Block  + (intent.Act == ActionType.B ? B_BONUS_PER_CHARGE * use : 0);
		
		int finalDmg = dmg;   
		int finalBlk = blk;   
		int chargeCostThisAction = use;

		var plan = new BasicPlan(intent.Act, self, tgt, finalDmg, finalBlk, chargeCostThisAction, numbers.GainAmount, numbers.APCost);
		return TranslationResult.Pass(plan, intent);
	}

	private static TranslationResult TranslateRecallIntentInternal(
		RecallIntent intent, CombatState state, Actor self)
	{
		// 一次/回合檢查
		if (RecallUsedThisTurn(state.PhaseCtx)) return TranslationResult.Fail(FailCode.RecallUsed);

		// EchoStore full check
		if (state.IsEchoStoreFull) 
			return TranslationResult.Fail(FailCode.EchoSlotsFull);

		// 索引合法性檢查（包含空集合檢查）
		var memory = state.GetRecallView();
		FailCode fail = ValidateIndices(intent.RecallIndices, memory, state.PhaseCtx.TurnNum);
		if (fail != FailCode.None) return TranslationResult.Fail(fail);

		// 連續性檢查：去重 + 由小到大排序 → 相鄰索引必須差 1（預留給未來 2L/3L）
		var span = intent.RecallIndices.Distinct().OrderBy(x => x).ToArray();
		for (int i = 1; i < span.Length; i++)
		{
			if (span[i] != span[i - 1] + 1)
				return TranslationResult.Fail(FailCode.IndixNotContiguous);
		}


		// 暫時開放 1L Echo
		if (intent.RecallIndices == null || intent.RecallIndices.Length != 1)
			return TranslationResult.Fail(FailCode.IndexLimited); // 僅開放 1L


		// AP 檢查
		// Recall 的 AP cost: 如果角色有 AP 系統，則消耗 1，否則為 0
		int apCost = (self.AP != null) ? 1 : 0;
		if (apCost > 0 && !self.HasAP(apCost)) return TranslationResult.Fail(FailCode.NoAP);

		var sequence = intent.RecallIndices.Select(idx => memory.Ops[idx]).ToArray();
		var plan = new RecallPlan(self, sequence, apCost);
		return TranslationResult.Pass(plan, intent);
	}
	
	private static TranslationResult TranslateEchoIntentInternal(
		EchoIntent intent, PhaseContext phase, TryGetActorById tryGetActor, Actor self)
	{

		// AP 檢查
		if (!self.HasAP(intent.Echo.CostAP))
			return TranslationResult.Fail(FailCode.NoAP);
		
		// 目標驗證
		var target = ValidateEchoTarget(intent.Echo.TargetType, intent.TargetId, tryGetActor, self);
		if (target == null)
			return TranslationResult.Fail(FailCode.BadTarget);
		
		// Op 映射 (僅支援 A/B/C)
		var plan = MapEchoToBasicPlan(intent.Echo, self, target);
		return plan != null 
			? TranslationResult.Pass(plan, intent)
			: TranslationResult.Fail(FailCode.NoRecipe);
	}

	private static Actor? ValidateEchoTarget(TargetType targetType, int? targetId, TryGetActorById tryGetActor, Actor self)
	{
		return targetType switch
		{
			TargetType.Self => self,
			TargetType.Target => targetId.HasValue && tryGetActor(targetId.Value, out var t) ? t : null,
			TargetType.None => self, // 預設自己
			TargetType.All => null,  // 暫不支援，返回 null 觸發 BadTarget
			_ => null
		};
	}

	private static BasicPlan? MapEchoToBasicPlan(Echo echo, Actor self, Actor target)
	{
		// 使用與 Basic 相同的數值
		var numbers = ComputeBasicNumbers(echo.Op switch {
			HLAop.Attack => ActionType.A,
			HLAop.Block => ActionType.B, 
			HLAop.Charge => ActionType.C,
			_ => ActionType.A  // fallback
		}, self);
		
		return echo.Op switch
		{
			HLAop.Attack => new BasicPlan(ActionType.A, self, target, 
				numbers.Damage, 0, 0, 0, echo.CostAP),
			HLAop.Block => new BasicPlan(ActionType.B, self, self,
				0, numbers.Block, 0, 0, echo.CostAP),
			HLAop.Charge => new BasicPlan(ActionType.C, self, self,
				0, 0, 0, numbers.GainAmount, echo.CostAP),
			_ => null  // CA 等其他返回 null
		};
	}
}
