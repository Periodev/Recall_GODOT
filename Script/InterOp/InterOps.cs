
using System;

using System.Collections.Generic;
using CombatCore.Abstractions;
using CombatCore.Memory;
using CombatCore.Command;

namespace CombatCore.InterOp
{
	public sealed class InterOps : IInterOps
	{
		public string LastError { get; private set; } = string.Empty;

		// 遊戲常數（後續移至 GameConstants）
		private const int AP_COST = 1;
		private const int BASE_A = 5, BASE_B = 5, BASE_C = 1;
		private const int CHARGE_BONUS_A = 3, CHARGE_BONUS_B = 2;
		private const int CHARGE_COST_PER_USE = 1;

		public AtomicCmd[] Translate(InterOpCall call, IActorLookup lookup, IMemoryQueue queue)
		{
			LastError = string.Empty;

			if (!TryGetActors(lookup, call.SourceId, call.TargetId, out var src, out var dst))
				return Fail("actor_not_found");

			return call.Op switch
			{
				InterOpCode.BasicA => TranslateBasicA(src, dst),
				InterOpCode.BasicB => TranslateBasicB(src, dst),
				InterOpCode.BasicC => TranslateBasicC(src),
				InterOpCode.RecallEcho => TranslateRecallEcho(call, src, dst, queue),
				_ => Fail("unknown_operation")
			};
		}

		// ========== 基本動作翻譯 ==========

		private AtomicCmd[] TranslateBasicA(Actor src, Actor dst)
		{
			if (!CheckBasicActionPreconditions(src, dst)) 
				return Array.Empty<AtomicCmd>();

			var (damage, chargeSpent) = CalculateBasicAValues(src);
			var commands = new List<AtomicCmd>(capacity: chargeSpent > 0 ? 2 : 1);

			commands.Add(AtomicCmd.DealDamage(src, dst, damage));
			
			if (chargeSpent > 0)
				commands.Add(AtomicCmd.GainCharge(src, -chargeSpent));

			return commands.ToArray();
		}

		private AtomicCmd[] TranslateBasicB(Actor src, Actor dst)
		{
			if (!CheckBasicActionPreconditions(src, dst)) 
				return Array.Empty<AtomicCmd>();

			var (shield, chargeSpent) = CalculateBasicBValues(src);
			var commands = new List<AtomicCmd>(capacity: chargeSpent > 0 ? 2 : 1);

			commands.Add(AtomicCmd.AddShield(dst, shield));
			
			if (chargeSpent > 0)
				commands.Add(AtomicCmd.GainCharge(src, -chargeSpent));

			return commands.ToArray();
		}

		private AtomicCmd[] TranslateBasicC(Actor src)
		{
			if (!HasSufficientAP(src)) 
				return Fail("insufficient_ap");

			return new[] { AtomicCmd.GainCharge(src, BASE_C) };
		}

		// ========== Echo 系統翻譯 ==========

		private AtomicCmd[] TranslateRecallEcho(InterOpCall call, Actor src, Actor dst, IMemoryQueue queue)
		{
			if (call.Indices == null || call.Indices.Count == 0)
				return Fail("empty_indices");

			if (!ValidateEchoIndices(call.Indices, queue))
				return Array.Empty<AtomicCmd>();

			var commands = new List<AtomicCmd>();
			bool hasUsedChargeInBatch = false;

			foreach (var index in call.Indices)
			{
				var action = queue.Peek(index);
				var echoCmds = TranslateEchoAction(action, src, dst, ref hasUsedChargeInBatch);
				commands.AddRange(echoCmds);
			}

			// 批次結束後統一扣除 Charge（如果本批次有使用）
			if (hasUsedChargeInBatch)
				commands.Add(AtomicCmd.GainCharge(src, -CHARGE_COST_PER_USE));

			return commands.ToArray();
		}

		private AtomicCmd[] TranslateEchoAction(ActionType action, Actor src, Actor dst, ref bool hasUsedChargeInBatch)
		{
			return action switch
			{
				ActionType.A => CreateEchoAttack(src, dst, ref hasUsedChargeInBatch),
				ActionType.B => CreateEchoBlock(src, dst, ref hasUsedChargeInBatch),
				ActionType.C => new[] { AtomicCmd.GainCharge(src, BASE_C) },
				_ => Array.Empty<AtomicCmd>()
			};
		}

		private AtomicCmd[] CreateEchoAttack(Actor src, Actor dst, ref bool hasUsedChargeInBatch)
		{
			var canUseCharge = !hasUsedChargeInBatch && CanUseCharge(src);
			var damage = BASE_A + (canUseCharge ? CHARGE_BONUS_A : 0);
			
			if (canUseCharge)
				hasUsedChargeInBatch = true;

			return new[] { AtomicCmd.DealDamage(src, dst, damage) };
		}

		private AtomicCmd[] CreateEchoBlock(Actor src, Actor dst, ref bool hasUsedChargeInBatch)
		{
			var canUseCharge = !hasUsedChargeInBatch && CanUseCharge(src);
			var shield = BASE_B + (canUseCharge ? CHARGE_BONUS_B : 0);
			
			if (canUseCharge)
				hasUsedChargeInBatch = true;

			return new[] { AtomicCmd.AddShield(dst, shield) };
		}

		// ========== 輔助計算方法 ==========

		private (int damage, int chargeSpent) CalculateBasicAValues(Actor src)
		{
			var canUseCharge = CanUseCharge(src);
			var damage = BASE_A + (canUseCharge ? CHARGE_BONUS_A : 0);
			var chargeSpent = canUseCharge ? CHARGE_COST_PER_USE : 0;
			return (damage, chargeSpent);
		}

		private (int shield, int chargeSpent) CalculateBasicBValues(Actor src)
		{
			var canUseCharge = CanUseCharge(src);
			var shield = BASE_B + (canUseCharge ? CHARGE_BONUS_B : 0);
			var chargeSpent = canUseCharge ? CHARGE_COST_PER_USE : 0;
			return (shield, chargeSpent);
		}

		// ========== 前置條件檢查 ==========

		private bool CheckBasicActionPreconditions(Actor src, Actor dst)
		{
			if (!HasSufficientAP(src)) 
				return FailBool("insufficient_ap");
			
			if (!dst.IsAlive) 
				return FailBool("target_not_alive");
			
			return true;
		}

		private bool ValidateEchoIndices(IReadOnlyList<int> indices, IMemoryQueue queue)
		{
			foreach (var index in indices)
			{
				if (index < 0 || index >= queue.Count)
					return FailBool("index_out_of_range");
				
				// TODO: 檢查是否指向「當回合」動作（需要額外的回合邊界資訊）
				// 暫時跳過此檢查，由上層 EchoExecutor 負責
			}
			return true;
		}

		// ========== 基礎檢查方法 ==========

		private bool HasSufficientAP(Actor actor) => 
			actor?.AP?.Value >= AP_COST;

		private bool CanUseCharge(Actor actor) => 
			actor?.Charge?.Value >= CHARGE_COST_PER_USE;

		private bool TryGetActors(IActorLookup lookup, int sourceId, int targetId, out Actor src, out Actor dst)
		{
			src = lookup.GetActor(sourceId);
			dst = lookup.GetActor(targetId);
			return src != null && dst != null;
		}

		// ========== 錯誤處理 ==========

		private AtomicCmd[] Fail(string errorMessage)
		{
			LastError = errorMessage;
			return Array.Empty<AtomicCmd>();
		}

		private bool FailBool(string errorMessage)
		{
			LastError = errorMessage;
			return false;
		}
	}
}
