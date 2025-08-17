
#if DEBUG
using Godot;
#endif

using System;
using CombatCore;
using CombatCore.Command;
using CombatCore.InterOp;

/// Phase 業務邏輯函數庫 - 處理各階段的具體業務邏輯
public static class PhaseFunction
{
	// === Player Phase Functions ===

	/// 處理玩家初始化：AP 恢復等系統操作
	public static PhaseResult HandlePlayerInit(ref CombatState state)
	{
#if DEBUG
		GD.Print($"[PhaseFunction] Executing player init system services");
		GD.Print($"[PhaseFunction] Player AP before refill: {state.Player.AP.Value}/{state.Player.AP.PerTurn}");
#endif

		// 🎯 恢復玩家 AP 到每回合最大值
		state.Player.AP.Refill();

#if DEBUG
		GD.Print($"[PhaseFunction] Player AP after refill: {state.Player.AP.Value}/{state.Player.AP.PerTurn}");
#endif

		// 🎯 推進到下一階段
		state.PhaseCtx.Step = PhaseStep.PlayerDraw;
		return PhaseResult.Continue;
	}

	/// 處理玩家計劃階段：Intent 轉換
	public static PhaseResult HandlePlayerPlanning(ref CombatState state)
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

		// 保存轉換結果並推進
		state.PhaseCtx.SetTranslation(translationResult);
		state.PhaseCtx.Step = PhaseStep.PlayerExecute;
		return PhaseResult.Continue;
	}

	/// 處理玩家執行階段：命令執行與狀態提交
	public static PhaseResult HandlePlayerExecution(ref CombatState state)
	{
		if (!state.PhaseCtx.TryConsumeTranslation(out var translation))
		{
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		var execResult = CombatPipeline.ExecuteCommands(state, translation.Commands, translation.OriginalIntent);

		CommitPlayerAction(state, translation.OriginalIntent, execResult);

		state.PhaseCtx.Step = PhaseStep.PlayerInput;
		return PhaseResult.WaitInput;
	}

	// === Enemy Phase Functions ===

	/// 處理敵人意圖生成：查詢 AI 策略表，決定敵人行動
	public static PhaseResult HandleEnemyAI(ref CombatState state)
	{
		// 檢查是否已有 Intent
		if (state.PhaseCtx.HasPendingIntent)
		{
			state.PhaseCtx.Step = PhaseStep.EnemyPlanning;
			return PhaseResult.Continue;
		}

		// 查詢 AI 策略表，生成敵人意圖
		var intent = CombatPipeline.GenerateEnemyIntent(state);

		// 設定 Intent 並推進到計劃階段
		state.PhaseCtx.SetIntent(intent);
		state.PhaseCtx.Step = PhaseStep.EnemyPlanning;
		return PhaseResult.Continue;
	}

	/// 處理敵人管線處理：將意圖轉換為執行計劃
	public static PhaseResult HandleEnemyPipelineProcessing(ref CombatState state)
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

		// 保存轉換結果並推進到執行階段
		state.PhaseCtx.SetTranslation(translationResult);
		state.PhaseCtx.Step = PhaseStep.EnemyExecInstant;
		return PhaseResult.Continue;
	}

	/// 處理敵人執行階段：命令執行
	public static PhaseResult HandleEnemyExecution(ref CombatState state)
	{
		if (!state.PhaseCtx.TryConsumeTranslation(out var translation))
		{
			state.PhaseCtx.Step = PhaseStep.PlayerInit;
			return PhaseResult.Continue;
		}

		var execResult = CombatPipeline.ExecuteCommands(state, translation.Commands, translation.OriginalIntent);

		state.PhaseCtx.Step = PhaseStep.PlayerInit;
		return PhaseResult.Continue;
	}

	/// 提交玩家行動結果到遊戲狀態（從 CombatPipeline.CommitAction 移入）
	private static void CommitPlayerAction(CombatState state, HLAIntent intent, ExecutionResult execResult)
	{
		// Memory 管理：Basic 動作需要寫入記憶
		if (intent is BasicIntent basicIntent && PhaseRunner.IsPlayerPhase(ref state))
		{
			state.Mem?.Push(basicIntent.Act, state.PhaseCtx.TurnNum);
		}

		// Recall 標記：標記本回合已使用 Recall
		if (intent is RecallIntent)
		{
			state.PhaseCtx.MarkRecallUsed();
		}
	}

}
