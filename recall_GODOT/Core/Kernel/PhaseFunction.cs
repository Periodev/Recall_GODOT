using System;
using System.Diagnostics;
using CombatCore;
using CombatCore.Command;
using CombatCore.InterOp;
using CombatCore.ActorOp;
using CombatCore.UI;

/// Phase 業務邏輯函數庫 - 處理各階段的具體業務邏輯
namespace CombatCore.Kernel
{
	public static class PhaseFunction
	{
		// === Player Phase Functions ===

		/// 處理玩家初始化：AP 恢復等系統操作
		public static PhaseResult HandlePlayerInit(CombatState state)
		{
			Debug.Print($"[PhaseFunction] Executing player init system services");
			Debug.Print($"[PhaseFunction] Player AP before refill: {state.Player.AP?.Value}/{state.Player.AP?.PerTurn}");

			// 🎯 恢復玩家 AP 到每回合最大值
			state.Player.AP?.Refill();
			SignalHub.NotifyAPChanged(((state.Player.AP == null) ? 0 : state.Player.AP.Value));

			Debug.Print($"[PhaseFunction] Player AP after refill: {state.Player.AP?.Value}/{state.Player.AP?.PerTurn}");

			//  clear charge and shield on turn start
			SelfOp.ClearShield(state.Player);
			SelfOp.ClearCharge(state.Player);


			// 🎯 推進到下一階段
			state.PhaseCtx.Step = PhaseStep.PlayerDraw;
			return PhaseResult.Continue;
		}

		public static PhaseResult HandlePlayerExecution(CombatState state)
		{
			if (!CombatPipeline.PlayerQueue.HasIntents)
			{
				Debug.Print($"[PhaseFunction] No player intents in queue");

				state.PhaseCtx.Step = PhaseStep.PlayerInput;
				return PhaseResult.WaitInput;
			}

			Debug.Print($"[PhaseFunction] Processing {CombatPipeline.PlayerQueue.Count} player intents");

			var result = CombatPipeline.ProcessPlayerQueue(state);

			if (CheckCombatEnd(state))
				return PhaseResult.CombatEnd;

			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// === Enemy Phase Functions ===

		/// <summary>
		/// 處理敵人意圖生成：查詢 AI 策略表，決定敵人行動並分派到對應隊列
		/// </summary>
		public static PhaseResult HandleEnemyAI(CombatState state)
		{
			// 生成敵人行動意圖並自動分配到對應隊列
			CombatPipeline.GenerateAndEnqueueEnemyActions(state);

			Debug.Print($"[PhaseFunction] Enemy actions generated and queued");

			state.PhaseCtx.Step = PhaseStep.EnemyExecInstant;
			return PhaseResult.Continue;
		}

		/// <summary>
		/// 處理敵人即時執行隊列
		/// </summary>
		public static PhaseResult HandleEnemyInstantExecution(CombatState state)
		{
			if (CombatPipeline.EnemyInstantQueue.HasIntents)
			{
				Debug.Print($"[PhaseFunction] Processing {CombatPipeline.EnemyInstantQueue.Count} enemy instant intents");

				var result = CombatPipeline.ProcessEnemyInstantQueue(state);

				if (CheckCombatEnd(state))
					return PhaseResult.CombatEnd;
			}
#if DEBUG
		else
		{
			Debug.Print($"[PhaseFunction] No enemy instant intents to process");
		}
#endif

			// 推進到 PlayerInit 階段
			state.PhaseCtx.Step = PhaseStep.PlayerInit;
			return PhaseResult.Continue;
		}

		/// <summary>
		/// 處理 Enemy DelayedQueue：執行回合末的延遲動作
		/// </summary>
		public static PhaseResult HandleEnemyDelayed(CombatState state)
		{
			if (CombatPipeline.EnemyDelayedQueue.HasIntents)
			{
				Debug.Print($"[PhaseFunction] Processing {CombatPipeline.EnemyDelayedQueue.Count} enemy delayed intents");

				var result = CombatPipeline.ProcessEnemyDelayedQueue(state);

				if (CheckCombatEnd(state))
					return PhaseResult.CombatEnd;
			}
#if DEBUG
		else
		{
			Debug.Print($"[PhaseFunction] No enemy delayed intents to process");
		}
#endif

			// 推進到回合結束
			state.PhaseCtx.Step = PhaseStep.TurnEnd;
			return PhaseResult.Continue;
		}

		/// <summary>
		/// 處理回合結束隊列
		/// </summary>
		public static PhaseResult HandleTurnEnd(CombatState state)
		{
			var result = CombatPipeline.ProcessTurnEndQueue(state);

			if (CheckCombatEnd(state))
				return PhaseResult.CombatEnd;

			// 回合結束後，推進到下一個回合的開始
			state.PhaseCtx.Step = PhaseStep.TurnStart;
			return PhaseResult.Continue;
		}

		private static bool CheckCombatEnd(CombatState state)
		{
			if (!state.Player.IsAlive || !state.Enemy.IsAlive)
			{
				state.PhaseCtx.Step = PhaseStep.CombatEnd;
				return true;
			}
			return false;
		}

	}
}