
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

	/// 處理玩家計劃和執行階段：Intent 轉換 + 命令執行與狀態提交
	public static PhaseResult HandlePlayerPlanningAndExecution(CombatState state)
	{
		// 檢查是否有待處理的 Intent
		if (!state.PhaseCtx.TryConsumeIntent(out var intent))
		{
#if DEBUG
			GD.Print($"[PhaseFunction] No pending Intent.");
#endif
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 使用 CombatPipeline 轉換 Intent
		var translationResult = CombatPipeline.TranslateIntent(state, state.Player, intent);

#if DEBUG
		GD.Print($"[PhaseFunction] Translating player intent.");
#endif

		if (!translationResult.Success)
		{
#if DEBUG
			GD.Print($"[PhaseFunction] Intent translation failed, code: {translationResult.ErrorCode}.");
#endif
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 立即執行命令，不保存中間結果
		var execResult = CombatPipeline.ExecuteCommands(state, translationResult.Commands, translationResult.OriginalIntent);

		if (execResult.Success)
        {
			CommitPlayerAction(state, translationResult.OriginalIntent, execResult);
		}

		if (true == CheckCombatEnd(state))
        	return PhaseResult.CombatEnd;

		state.PhaseCtx.Step = PhaseStep.PlayerInput;
		return PhaseResult.WaitInput;
	}

	// === Enemy Phase Functions ===

	/// 處理敵人意圖生成：查詢 AI 策略表，決定敵人行動
	public static PhaseResult HandleEnemyAI(CombatState state)
	{
		// 檢查是否已有 Intent
		if (state.PhaseCtx.HasPendingIntent)
		{
			state.PhaseCtx.Step = PhaseStep.EnemyExecInstant;
			return PhaseResult.Continue;
		}

		// 查詢 AI 策略表，生成敵人意圖
		var intent = CombatPipeline.GenerateEnemyIntent(state);

		// 設定 Intent 並推進到執行階段
		state.PhaseCtx.SetIntent(intent);
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

	/// 提交玩家行動結果到遊戲狀態（從 CombatPipeline.CommitAction 移入）
	private static void CommitPlayerAction(CombatState state, HLAIntent intent, ExecutionResult execResult)
	{
		// Memory 管理：Basic 動作需要寫入記憶
		if (intent is BasicIntent basicIntent && PhaseRunner.IsPlayerPhase(state))
		{
			state.Mem?.Push(basicIntent.Act, state.PhaseCtx.TurnNum);
		}

		// Recall 標記：標記本回合已使用 Recall
		if (intent is RecallIntent)
		{
			state.PhaseCtx.MarkRecallUsed();
		}
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
