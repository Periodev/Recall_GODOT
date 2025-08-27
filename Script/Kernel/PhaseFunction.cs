
#if DEBUG
using Godot;
#endif

using System;
using CombatCore;
using CombatCore.Command;
using CombatCore.InterOp;
using CombatCore.ActorOp;

/// Phase 業務邏輯函數庫 - 處理各階段的具體業務邏輯
public static class PhaseFunction
{
	// === Player Phase Functions ===

	/// 處理玩家初始化：AP 恢復等系統操作
	public static PhaseResult HandlePlayerInit(CombatState state)
	{
#if DEBUG
		GD.Print($"[PhaseFunction] Executing player init system services");
		GD.Print($"[PhaseFunction] Player AP before refill: {state.Player.AP.Value}/{state.Player.AP.PerTurn}");
#endif

		// 🎯 恢復玩家 AP 到每回合最大值
		state.Player.AP.Refill();
		UISignalHub.NotifyAPChanged(state.Player.AP.Value);
#if DEBUG
		GD.Print($"[PhaseFunction] Player AP after refill: {state.Player.AP.Value}/{state.Player.AP.PerTurn}");
#endif

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
#if DEBUG
			GD.Print($"[PhaseFunction] No player intents in queue");
#endif
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

#if DEBUG
		GD.Print($"[PhaseFunction] Processing {CombatPipeline.PlayerQueue.Count} player intents");
#endif

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
		// 查詢 AI 策略表，生成敵人意圖
		var intent = CombatPipeline.GenerateEnemyIntent(state);

		// 決定執行時機
		var timing = EnemyStrategy.DetermineTiming(intent);

		if (timing == ExecuteTiming.Instant)
		{
			// B, C 使用現有的 PendingIntent 機制
			state.PhaseCtx.SetIntent(intent);
		}
		else if (timing == ExecuteTiming.Delayed)
		{
			// A 入隊到 DelayedQueue
			CombatPipeline.EnemyDelayedQueue.Enqueue(state.Enemy, intent, $"Enemy {intent} delayed");
#if DEBUG
			GD.Print($"[PhaseFunction] Enemy action queued for delayed execution: {intent}");
#endif
		}

		state.PhaseCtx.Step = PhaseStep.EnemyExecInstant;
		return PhaseResult.Continue;
	}

	/// 處理敵人計劃和執行階段：意圖轉換 + 命令執行
	public static PhaseResult HandleEnemyPlanningAndExecution(CombatState state)
	{
		if (!state.PhaseCtx.TryConsumeIntent(out var intent))
		{
			// 異常：沒有 Intent，回退到意圖階段
			state.PhaseCtx.Step = PhaseStep.EnemyIntent;
			return PhaseResult.Continue;
		}

		var translationResult = CombatPipeline.TranslateIntent(state, state.Enemy, intent);

		if (!translationResult.Success)
		{
			// 轉換失敗，跳過敵人行動
			state.PhaseCtx.Step = PhaseStep.PlayerInit;

#if DEBUG
			GD.PrintErr($"[PhaseFunction] Enemy intent translation failed.");
#endif
			return PhaseResult.Continue;
		}

		// 立即執行命令，不保存中間結果
		var execResult = CombatPipeline.ExecuteCommands(state, translationResult.Commands, translationResult.OriginalIntent);

		if (true == CheckCombatEnd(state))
        	return PhaseResult.CombatEnd;

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
#if DEBUG
			GD.Print($"[PhaseFunction] Processing {CombatPipeline.EnemyDelayedQueue.Count} enemy delayed intents");
#endif
			var result = CombatPipeline.ProcessEnemyDelayedQueue(state);
			
			if (CheckCombatEnd(state))
				return PhaseResult.CombatEnd;
		}
#if DEBUG
		else
		{
			GD.Print($"[PhaseFunction] No enemy delayed intents to process");
		}
#endif
		
		// 推進到回合結束
		state.PhaseCtx.Step = PhaseStep.TurnEnd;
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
