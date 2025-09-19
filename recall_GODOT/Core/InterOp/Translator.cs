using System;
using System.Collections.Generic;
using System.Linq;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Command;
using CombatCore.Recall;
using static CombatCore.GameConst;

namespace CombatCore.InterOp
{
	public readonly struct TranslationResult
	{
		public bool Success { get; }
		public FailCode ErrorCode { get; }
		public AtomicCmd[] Commands { get; }
		public Intent OriginalIntent { get; }

		private TranslationResult(bool success, FailCode errorCode, AtomicCmd[] commands, Intent originalIntent)
		{
			Success = success;
			ErrorCode = errorCode;
			Commands = commands ?? Array.Empty<AtomicCmd>();
			OriginalIntent = originalIntent;
		}

		public static TranslationResult Pass(AtomicCmd[] commands, Intent intent) =>
			new(true, FailCode.None, commands, intent);

		public static TranslationResult Fail(FailCode code) =>
			new(false, code, Array.Empty<AtomicCmd>(), null!);
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
				ActIntent actIntent => TranslateAct(actIntent, state, self),
				RecallIntent recallIntent => TranslateRecall(recallIntent, state, self),
				_ => TranslationResult.Fail(FailCode.UnknownIntent)
			};
		}

		// 輔助方法
		private static Actor? ResolveTarget(int? targetId, TargetType targetType, TryGetActorById tryGetActor, Actor self)
		{
			return targetType switch
			{
				TargetType.None => null,
				TargetType.Self => self,
				TargetType.Target => targetId.HasValue && tryGetActor(targetId.Value, out var actor) && !ReferenceEquals(actor, self) ? actor : null,
				TargetType.All => null, // 暫不處理群體目標
				_ => null
			};
		}

		private static bool IsPlayer(Actor actor, CombatState state)
		{
			return ReferenceEquals(actor, state.Player);
		}


		private static TranslationResult TranslateRecall(RecallIntent intent, CombatState state, Actor self)
		{
			// 一次/回合檢查
			if (state.PhaseCtx.RecallUsedThisTurn)
				return TranslationResult.Fail(FailCode.RecallUsed);

			// ActStore 滿檢查
			if (state.actStore.IsFull)
				return TranslationResult.Fail(FailCode.ActSlotsFull);

			// RecipeId 合法性檢查
			if (intent.RecipeId <= 0 || !RecipeRegistry.ContainsRecipe(intent.RecipeId))
				return TranslationResult.Fail(FailCode.NoRecipe);

			// AP 檢查（只檢查玩家）
			int apCost = IsPlayer(self, state) ? 1 : 0;
			if (apCost > 0 && !self.HasAP(apCost))
				return TranslationResult.Fail(FailCode.NoAP);

			// 命令生成（只有 AP 消耗）
			var commands = new List<AtomicCmd>();
			if (apCost > 0)
				commands.Add(AtomicCmd.ConsumeAP(self, apCost));

			return TranslationResult.Pass(commands.ToArray(), intent);
		}

		private static TranslationResult TranslateAct(ActIntent intent, CombatState state, Actor self)
		{
			var act = intent.Act;

			// 冷卻檢查
			if (!act.IsReady)
				return TranslationResult.Fail(FailCode.ActCooldown);

			// 目標解析
			var target = ResolveTarget(intent.TargetId, act.TargetType, state.TryGetActor, self);
			if (target == null)
				return TranslationResult.Fail(FailCode.BadTarget);

			// AP 檢查（只檢查玩家）
			if (IsPlayer(self, state) && !self.HasAP(act.CostAP))
				return TranslationResult.Fail(FailCode.NoAP);

			// 命令生成
			var commands = new List<AtomicCmd>();

			// AP 消耗（只有玩家消耗 AP）
			if (IsPlayer(self, state) && act.CostAP > 0)
				commands.Add(AtomicCmd.ConsumeAP(self, act.CostAP));

			// 根據 Op 生成行為命令
			switch (act.Op)
			{
				case HLAop.Attack:
					int damage = A_BASE_DMG;
					if (self.HasCopy())
					{
						commands.Add(AtomicCmd.ConsumeCopy(self, 1));
						commands.Add(AtomicCmd.DealDamage(self, target, damage));
						commands.Add(AtomicCmd.DealDamage(self, target, damage));
					}
					else
					{
						commands.Add(AtomicCmd.DealDamage(self, target, damage));
					}
					break;

				case HLAop.Block:
					int block = B_BASE_BLOCK;
					if (self.HasCopy())
					{
						commands.Add(AtomicCmd.ConsumeCopy(self, 1));
						commands.Add(AtomicCmd.AddShield(target, block));
						commands.Add(AtomicCmd.AddShield(target, block));
					}
					else
					{
						commands.Add(AtomicCmd.AddShield(target, block));
					}
					break;

				case HLAop.Charge:
					int gain = C_GAIN_COPY;
					if (self.Copy != null)
						commands.Add(AtomicCmd.GainCopy(self, gain));
					else if (self.Charge != null)
						commands.Add(AtomicCmd.GainCharge(self, gain));
					break;

				default:
					return TranslationResult.Fail(FailCode.UnknownIntent);
			}

			return TranslationResult.Pass(commands.ToArray(), intent);
		}

	}
}
