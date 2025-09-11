
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
	public sealed record RecallIntent(int RecipeId) : Intent((int?)null);
	public sealed record EchoIntent(Echo Echo, int? TargetId) : Intent(TargetId);

	public readonly struct RecallView
	{
		public RecallView(IReadOnlyList<TokenType> ops, IReadOnlyList<int> turns)
		{
			Ops = ops ?? Array.Empty<TokenType>();
			Turns = turns ?? Array.Empty<int>();
		}

		public IReadOnlyList<TokenType> Ops { get; }
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

				RecallIntent ri => TranslateRecallIntentInternal(ri, state.PhaseCtx, state.GetRecallView(), state.TryGetActor, self),
				EchoIntent ei => TranslateEchoIntentInternal(ei, state.PhaseCtx, state.GetRecallView(), state.TryGetActor, self),
				_ => TranslationResult.Fail(FailCode.UnknownIntent)
			};
		}

		// 數值計算集中處理
		private static (int Damage, int Block, int GainAmount, int ChargeCost, int APCost) ComputeBasicNumbers(TokenType act, Actor self)
		{
			// 如果角色有 AP 系統，則消耗 1，否則為 0
			int apCost = (self.AP != null) ? 1 : 0;
			return act switch
			{
				TokenType.A => (Damage: 5, Block: 0, GainAmount: 0, ChargeCost: 0, APCost: apCost),
				TokenType.B => (Damage: 0, Block: 3, GainAmount: 0, ChargeCost: 0, APCost: apCost),
				TokenType.C => (Damage: 0, Block: 0, GainAmount: C_GAIN_COPY, ChargeCost: 0, APCost: apCost),
				_ => (Damage: 0, Block: 0, GainAmount: 0, ChargeCost: 0, APCost: apCost)
			};
		}


		// 輔助方法
		private static Actor? ResolveTarget(int? id, TryGetActorById tryGetActor) =>
			id.HasValue && tryGetActor(id.Value, out var a) ? a : null;

		private static bool RecallUsedThisTurn(PhaseContext phase) => phase.RecallUsedThisTurn;


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
			if (intent.RecipeId <= 0 || !RecipeRegistry.ContainsRecipe(intent.RecipeId))
				return TranslationResult.Fail(FailCode.NoRecipe);

			// 建立 Plan（信任 UI 層已驗證索引）
			var plan = new RecallPlan(self, intent.RecipeId, apCost);
			return TranslationResult.Pass(plan, intent);
		}

		private static TranslationResult TranslateEchoIntentInternal(
			EchoIntent intent, PhaseContext phase, RecallView memory, 
			TryGetActorById tryGetActor, Actor self)
		{
			var echo = intent.Echo;
			
			// 冷卻檢查
			if (!echo.IsReady)
				return TranslationResult.Fail(FailCode.EchoCooldown);
			
			// 目標解析
			var target = ResolveTarget(intent.TargetId, tryGetActor);
			if (echo.TargetType == TargetType.Target && (target == null || ReferenceEquals(target, self)))
				return TranslationResult.Fail(FailCode.BadTarget);
			if (echo.TargetType == TargetType.Self)
				target = self;
			
			// AP 檢查
			if (echo.CostAP > 0 && !self.HasAP(echo.CostAP))
				return TranslationResult.Fail(FailCode.NoAP);
			
			// 根據 HLAop 生成計劃
			var plan = echo.Op switch
			{
				HLAop.Attack => new BasicPlan(TokenType.A, self, target!, 5, 0, 0, 0, 0, echo.CostAP),
				HLAop.Block => new BasicPlan(TokenType.B, self, self, 0, 3, 0, 0, 0, echo.CostAP),
				HLAop.Charge => new BasicPlan(TokenType.C, self, self, 0, 0, 0, 0, 2, echo.CostAP),
				_ => throw new ArgumentException($"Unsupported HLAop: {echo.Op}")
			};
			
			return TranslationResult.Pass(plan, intent);
		}

	}
}
