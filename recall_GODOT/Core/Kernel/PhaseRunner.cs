using System;
using System.Diagnostics;
using CombatCore;


/// 移除複雜的服務調度邏輯，直接使用 PhaseMap 統一處理
namespace CombatCore.Kernel
{
	public static class PhaseRunner
	{
		// === 公開接口：UI 層調用 ===

		/// 嘗試執行玩家動作（帶完整保護）
		/// 這是 Combat UI 應該使用的唯一入口

		public static PhaseResult TryExecutePlayerAction(CombatState state, Intent intent)
		{
			// 🔒 階段保護：只有在正確階段才能執行
			if (!CanPlayerAct(state.PhaseCtx))
			{
				Debug.Print($"[PhaseRunner] Player action blocked: Wrong phase ({state.PhaseCtx.Step})");

				return PhaseResult.PhaseLocked;
			}


			// 🔒 戰鬥狀態保護
			if (state.PhaseCtx.Step == PhaseStep.CombatEnd)
			{
				return PhaseResult.CombatEnd;
			}

			Debug.Print($"[PhaseRunner] Accepting player action: {intent}");

			// ✅ 保護檢查通過，將 Intent 加入 PlayerQueue 並推進流程
			CombatPipeline.EnqueuePlayerAction(state.Player, intent, "Via PhaseRunner");
			return AdvanceUntilInput(state);
		}


		/// 嘗試結束玩家回合（帶完整保護）
		public static PhaseResult TryEndPlayerTurn(CombatState state)
		{
			// 🔒 階段保護：只有在玩家輸入階段才能結束回合
			if (!CanPlayerAct(state.PhaseCtx))
			{
				Debug.Print($"[PhaseRunner] End turn blocked: Wrong phase ({state.PhaseCtx.Step})");

				return PhaseResult.PhaseLocked;
			}


			Debug.Print($"[PhaseRunner] Player ending turn");

			// ✅ 直接跳到敵人延遲執行階段
			state.PhaseCtx.Step = PhaseStep.EnemyExec;
			return AdvanceUntilInput(state);
		}


		/// 初始化戰鬥流程（遊戲開始時調用）
		public static PhaseResult InitializeCombat(CombatState state)
		{
			Debug.Print($"[PhaseRunner] Initializing combat, starting phase: {state.PhaseCtx.Step}");

			return AdvanceUntilInput(state);
		}

		/// 檢查當前是否為玩家階段
		public static bool IsPlayerPhase(CombatState state)
		{
			return ((byte)state.PhaseCtx.Step & 0xF0) == 0x00;
		}

		// === 內部邏輯：簡化的流程控制 ===


		/// 執行單個 Phase 步驟（簡化版本）
		/// 直接使用 PhaseMap.StepMaps，不再需要複雜的服務調度
		public static PhaseResult Run(CombatState state)
		{
			var step = state.PhaseCtx.Step;

			if (PhaseMap.StepMaps.TryGetValue(step, out var StepMap))
			{
				return StepMap(state);
			}

			Debug.Print($"[PhaseRunner] Unknown phase step: {step}. Halting execution to prevent infinite loop.");

			// 未知的 Phase Step，返回 Interrupt 防止無窮迴圈
			return PhaseResult.Interrupt;
		}

		/// 推進直到需要輸入（簡化版本）
		public static PhaseResult AdvanceUntilInput(CombatState state)
		{
			PhaseResult result = PhaseResult.Continue;
			int maxIterations = 100; // 安全保護
			int iterations = 0;

			while (result == PhaseResult.Continue && iterations < maxIterations)
			{
				iterations++;

				//Debug.Print($"[PhaseRunner] Iteration {iterations}: Step={state.PhaseCtx.Step}");

				result = Run(state);

				if (IsStoppingResult(result))
					break;
			}

			if (iterations >= maxIterations)
			{
				Debug.Print($"[PhaseRunner] Max iterations reached! Current step: {state.PhaseCtx.Step}");
				return PhaseResult.Interrupt;
			}

			return result;
		}

		// === 輔助方法 ===

		/// 檢查玩家是否可以執行動作
		private static bool CanPlayerAct(PhaseContext ctx)
		{
			return ctx.Step == PhaseStep.PlayerInput;
		}

		/// 檢查是否為停止結果
		private static bool IsStoppingResult(PhaseResult result)
		{
			return result == PhaseResult.WaitInput ||
				   result == PhaseResult.Pending ||
				   result == PhaseResult.Interrupt ||
				   result == PhaseResult.CombatEnd;
		}
	}
}