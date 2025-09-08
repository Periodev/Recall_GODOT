
using System;
using System.Collections.Generic;
using System.Linq;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Command;
using CombatCore.Recall;
using static CombatCore.GameConst;

namespace CombatCore
{
	public delegate bool TryGetActorById(int id, out Actor actor);
	public abstract record Intent(int? TargetId);
	public sealed record BasicIntent(ActionType Act, int? TargetId) : Intent(TargetId);
	public sealed record RecallIntent(int RecipeId) : Intent((int?)null);
	public sealed record EchoIntent(Echo Echo, int? TargetId) : Intent(TargetId);

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
}

namespace CombatCore.InterOp
{
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
				RecallIntent ri => TranslateRecallIntentInternal(ri, state.PhaseCtx, state.GetRecallView(), state.TryGetActor, self),
				EchoIntent ei => TranslateEchoIntentInternal(ei, state.PhaseCtx, state.TryGetActor, self),
				_ => TranslationResult.Fail(FailCode.UnknownIntent)
			};
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
				ActionType.C => (Damage: 0, Block: 0, GainAmount: C_GAIN_COPY, ChargeCost: 0, APCost: apCost),
				_ => (Damage: 0, Block: 0, GainAmount: 0, ChargeCost: 0, APCost: apCost)
			};
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

			// C 操作的 Copy 上限檢查
			int finalGainAmount = numbers.GainAmount;
			if (intent.Act == ActionType.C && self.Copy != null)
			{
				// 如果已達 Copy 上限，則不獲得 Copy (GainAmount = 0)
				if (self.Copy.Value >= COPY_MAX)
					finalGainAmount = 0;
			}

			// 檢查是否有 Copy 待觸發 (只對 A/B 有效)
			bool hasCopy = self.HasCopy() && (intent.Act == ActionType.A || intent.Act == ActionType.B);
			int copyCost = hasCopy ? 1 : 0;

			// 動態決定此次可用的 Charge 數量與加成 (敵人邏輯)
			int chargeCostThisAction = 0;
			if (intent.Act == ActionType.A || intent.Act == ActionType.B)
				chargeCostThisAction = Math.Min(self.Charge?.Value ?? 0, CHARGE_MAX_PER_ACTION);

			int dmg = numbers.Damage + (intent.Act == ActionType.A ? A_BONUS_PER_CHARGE * chargeCostThisAction : 0);
			int blk = numbers.Block + (intent.Act == ActionType.B ? B_BONUS_PER_CHARGE * chargeCostThisAction : 0);

			int finalDmg = dmg;
			int finalBlk = blk;

			var plan = new BasicPlan(intent.Act, self, tgt, finalDmg, finalBlk, chargeCostThisAction, copyCost, finalGainAmount, numbers.APCost);
			return TranslationResult.Pass(plan, intent);
		}

		private static TranslationResult TranslateRecallIntentInternal(
			RecallIntent intent, PhaseContext phase, RecallView memory, 
			TryGetActorById tryGetActor, Actor self)
		{
			// 一次/回合檢查
			if (RecallUsedThisTurn(phase))
				return TranslationResult.Fail(FailCode.RecallUsed);

			// AP 檢查（核心責任）
			int apCost = (self.AP != null) ? 1 : 0;
			if (apCost > 0 && !self.HasAP(apCost))
				return TranslationResult.Fail(FailCode.NoAP);

			// RecipeId 合法性檢查（簡化版）
			if (intent.RecipeId <= 0)
				return TranslationResult.Fail(FailCode.NoRecipe);

			// 建立 Plan（信任 UI 層已驗證索引）
			var plan = new RecallPlan(self, intent.RecipeId, apCost);
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
			var numbers = ComputeBasicNumbers(echo.Op switch
			{
				HLAop.Attack => ActionType.A,
				HLAop.Block => ActionType.B,
				HLAop.Charge => ActionType.C,
				_ => ActionType.A  // fallback
			}, self);

			return echo.Op switch
			{
				HLAop.Attack => new BasicPlan(ActionType.A, self, target,
					numbers.Damage, 0, 0, 0, 0, echo.CostAP),
				HLAop.Block => new BasicPlan(ActionType.B, self, self,
					0, numbers.Block, 0, 0, 0, echo.CostAP),
				HLAop.Charge => new BasicPlan(ActionType.C, self, self,
					0, 0, 0, 0, numbers.GainAmount, echo.CostAP),
				_ => null  // CA 等其他返回 null
			};
		}
	}
}
