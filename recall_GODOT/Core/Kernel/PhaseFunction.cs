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

			// 冷卻倒數
			foreach (var act in state.actStore.Items)
			{
				if (act.CooldownCounter > 0)
					act.CooldownCounter--;
			}

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
		/// 處理敵人上回合效果
		/// </summary>
		public static PhaseResult HandleEnemyMarkExecution(CombatState state)
		{
			if (CombatPipeline.EnemyMarkQueue.HasIntents)
			{
				Debug.Print($"[PhaseFunction] Processing {CombatPipeline.EnemyMarkQueue.Count} enemy mark intents");

				var result = CombatPipeline.ProcessEnemyMarkQueue(state);

				if (CheckCombatEnd(state))
					return PhaseResult.CombatEnd;
			}
#if DEBUG
			else
			{
				Debug.Print($"[PhaseFunction] No enemy instant intents to process");
			}
#endif

			// 推進到 Enemy Intent 階段
			state.PhaseCtx.Step = PhaseStep.EnemyIntent;
			return PhaseResult.Continue;
		}

		/// <summary>
		/// 處理敵人意圖生成：查詢 AI 策略表，決定敵人行動並分派到對應隊列
		/// </summary>
		public static PhaseResult HandleEnemyAI(CombatState state)
		{
			// 生成敵人行動意圖並自動分配到對應隊列
			CombatPipeline.GenerateAndEnqueueEnemyActions(state);

			Debug.Print($"[PhaseFunction] Enemy actions generated and queued");

			// 交到 Player Init 階段
			state.PhaseCtx.Step = PhaseStep.PlayerInit;
			return PhaseResult.Continue;
		}


		/// <summary>
		/// 處理 Enemy ActionQueue：執行回合動作
		/// </summary>
		public static PhaseResult HandleEnemyAction(CombatState state)
		{
			if (CombatPipeline.EnemyActionQueue.HasIntents)
			{
				Debug.Print($"[PhaseFunction] Processing {CombatPipeline.EnemyActionQueue.Count} enemy intents");

				var result = CombatPipeline.ProcessEnemyActionQueue(state);

				if (CheckCombatEnd(state))
					return PhaseResult.CombatEnd;
			}
#if DEBUG
			else
			{
				Debug.Print($"[PhaseFunction] No enemy intents to process");
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
			// 玩家死亡 → 戰鬥結束
			if (!state.Player.IsAlive)
			{
				state.PhaseCtx.Step = PhaseStep.CombatEnd;
				return true;
			}

			// 所有敵人死亡 → 戰鬥結束
			bool anyEnemyAlive = false;
			foreach (var enemy in state.GetAllEnemies())
			{
				if (enemy.IsAlive)
				{
					anyEnemyAlive = true;
					break;
				}
			}

			if (!anyEnemyAlive)
			{
				state.PhaseCtx.Step = PhaseStep.CombatEnd;
				return true;
			}

			return false;
		}

	}
}