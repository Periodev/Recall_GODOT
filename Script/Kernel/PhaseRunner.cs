#if DEBUG
using Godot;
#endif

using System;
using CombatCore;
using CombatCore.Command;
using CombatCore.InterOp;


/// Phase 流程控制器 - 基於回調模式的設計
/// 根據 PhaseStep + PhaseResult 組合提供對應服務

public static class PhaseRunner
{
	// === 公開接口：UI 層調用 ===

	/// 嘗試執行玩家動作（帶完整保護）
	/// 這是 Combat UI 應該使用的唯一入口
	public static PhaseResult TryExecutePlayerAction(ref CombatState state, HLAIntent intent)
	{
		// 🔒 階段保護：只有在正確階段才能執行
		if (!CanPlayerAct(state.PhaseCtx))
		{
#if DEBUG
			GD.Print($"[PhaseRunner] Player action blocked: Wrong phase ({state.PhaseCtx.Step})");
#endif
			return PhaseResult.PhaseLocked;
		}

		// 🔒 重複動作保護：檢查是否已有未處理的 Intent
		if (state.PhaseCtx.HasPendingIntent)
		{
#if DEBUG
			GD.Print($"[PhaseRunner] Player action blocked: Previous action still pending");
#endif
			return PhaseResult.Pending;
		}

		// 🔒 戰鬥狀態保護
		if (state.PhaseCtx.Step == PhaseStep.CombatEnd)
		{
			return PhaseResult.CombatEnd;
		}
#if DEBUG
		GD.Print($"[PhaseRunner] Accepting player action: {intent}");
#endif

		// ✅ 保護檢查通過，設定 Intent 並推進流程
		state.PhaseCtx.SetIntent(intent);
		return AdvanceUntilInput(ref state);
	}


	/// 嘗試結束玩家回合（帶完整保護）
	public static PhaseResult TryEndPlayerTurn(ref CombatState state)
	{
		// 🔒 階段保護：只有在玩家輸入階段才能結束回合
		if (!CanPlayerAct(state.PhaseCtx))
		{
#if DEBUG
			GD.Print($"[PhaseRunner] End turn blocked: Wrong phase ({state.PhaseCtx.Step})");
#endif
			return PhaseResult.PhaseLocked;
		}

		// 🔒 重複動作保護：檢查是否已有未處理的 Intent
		if (state.PhaseCtx.HasPendingIntent)
		{
#if DEBUG
			GD.Print($"[PhaseRunner] End turn blocked: Previous action still pending");
#endif
			return PhaseResult.Pending;
		}

#if DEBUG
		GD.Print($"[PhaseRunner] Player ending turn");
#endif

		// ✅ 直接跳到敵人延遲執行階段
		state.PhaseCtx.Step = PhaseStep.EnemyExecDelayed;
		return AdvanceUntilInput(ref state);
	}

	/// 初始化戰鬥流程（遊戲開始時調用）
	public static PhaseResult InitializeCombat(ref CombatState state)
	{
#if DEBUG
		GD.Print($"[PhaseRunner] Initializing combat, starting phase: {state.PhaseCtx.Step}");
#endif

		return AdvanceUntilInput(ref state);
	}

	public static bool IsPlayerPhase(ref CombatState state)
	{
		return ((byte)state.PhaseCtx.Step & 0xF0) == 0x00;
	}



	// === 內部邏輯：狀態保護 ===

	/// 執行單個 Phase 步驟並提供服務調度
	public static PhaseResult Run(ref CombatState state)
	{
		var step = state.PhaseCtx.Step;

		// 先執行標準 Phase 邏輯
		if (PhaseMap.StepFuncs.TryGetValue(step, out var stepFunc))
		{
			var result = stepFunc(ref state.PhaseCtx);

			// 🎯 根據 Step + Result 組合決定服務調度
			return DispatchService(ref state, step, result);
		}

#if DEBUG
		GD.PrintErr($"[PhaseRunner] Unknown phase step: {step}. Halting execution to prevent infinite loop.");
#endif
		// 未知的 Phase Step，這通常是一個錯誤（例如 PhaseMap 沒定義）。
		// 返回 Interrupt 而不是 Continue，以防止無窮迴圈。
		// 讓上層調用者決定如何處理這個中斷。
		return PhaseResult.Interrupt;
	}


	/// 推進直到需要輸入並提供服務調度
	public static PhaseResult AdvanceUntilInput(ref CombatState state)
	{
		PhaseResult result = PhaseResult.Continue;
		int maxIterations = 100; // 安全保護
		int iterations = 0;

		while (result == PhaseResult.Continue && iterations < maxIterations)
		{
			iterations++;

#if DEBUG
			GD.Print($"[PhaseRunner] Iteration {iterations}: Step={state.PhaseCtx.Step}");
#endif

			result = Run(ref state);

			if (IsStoppingResult(result))
				break;
		}

#if DEBUG
		if (iterations >= maxIterations)
		{
			GD.PrintErr($"[PhaseRunner] Max iterations reached! Current step: {state.PhaseCtx.Step}");
			return PhaseResult.Interrupt;
		}
#endif

		return result;
	}



	// === 服務調度核心 ===

	/// 根據 PhaseStep + PhaseResult 組合調度對應服務

	private static PhaseResult DispatchService(ref CombatState state, PhaseStep step, PhaseResult result)
	{

#if DEBUG
		GD.Print($"step {step}, result {result}");
#endif

		// 如果不是服務請求，直接返回原結果
		if (!result.IsServiceRequest())
			return result;

		// 根據 Step + Result 組合調度服務
		return (step, result) switch
		{
			// === Player Phase 服務 ===
			(PhaseStep.PlayerInit, PhaseResult.RequiresSysInit)
				=> ExecutePlayerInitSystem(ref state),

			(PhaseStep.PlayerPlanning, PhaseResult.RequiresPipeline)
				=> ExecutePlayerPlanning(ref state),

			(PhaseStep.PlayerExecute, PhaseResult.RequiresExecution)
				=> ExecutePlayerExecution(ref state),

			// === Enemy Phase 服務 ===
			(PhaseStep.EnemyIntent, PhaseResult.RequiresAI)
				=> ExecuteEnemyIntentGeneration(ref state),

			(PhaseStep.EnemyPlanning, PhaseResult.RequiresPipeline)
				=> ExecuteEnemyPipelineProcessing(ref state),

			(PhaseStep.EnemyExecInstant, PhaseResult.RequiresExecution)
				=> ExecuteEnemyExecution(ref state),


			// === 未知組合，返回錯誤 ===
			_ => HandleUnknownServiceRequest(ref state, step, result)
		};
	}

	// === Player 服務實現 ===

	/// 處理玩家初始化系統服務：AP 恢復等系統操作
	private static PhaseResult ExecutePlayerInitSystem(ref CombatState state)
	{
#if DEBUG
		GD.Print($"[PhaseRunner] Executing player init system services");
		GD.Print($"[PhaseRunner] Player AP before refill: {state.Player.AP.Value}/{state.Player.AP.PerTurn}");
#endif

		// 🎯 恢復玩家 AP 到每回合最大值
		state.Player.AP.Refill();

#if DEBUG
		GD.Print($"[PhaseRunner] Player AP after refill: {state.Player.AP.Value}/{state.Player.AP.PerTurn}");
#endif

		// 🎯 清理其他需要重置的狀態（如果有的話）

		// 🎯 推進到下一階段
		state.PhaseCtx.Step = PhaseStep.PlayerDraw;
		return PhaseResult.Continue;
	}

	private static PhaseResult ExecutePlayerPlanning(ref CombatState state)
	{
		// 檢查是否有待處理的 Intent
		if (!state.PhaseCtx.TryConsumeIntent(out var intent))
		{
#if DEBUG
			GD.Print($"No pending Intent.");
#endif
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 使用 CombatPipeline 轉換 Intent
		var translationResult = CombatPipeline.TranslateIntent(state, state.Player, intent);

#if DEBUG
		GD.Print($"Translating.");
#endif


		if (!translationResult.Success)
		{
#if DEBUG
			GD.Print($"Intent translation failed, code: {translationResult.ErrorCode}.");
#endif
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 保存轉換結果並推進
		state.PhaseCtx.SetTranslation(translationResult);
		state.PhaseCtx.Step = PhaseStep.PlayerExecute;
		return PhaseResult.Continue;
	}

	private static PhaseResult ExecutePlayerExecution(ref CombatState state)
	{
		if (!state.PhaseCtx.TryConsumeTranslation(out var translation))
		{
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		var execResult = CombatPipeline.ExecuteCommands(state, translation.Commands, translation.OriginalIntent);

		state.PhaseCtx.Step = PhaseStep.PlayerInput;
		return PhaseResult.WaitInput;
	}

	// === Enemy 服務實現 ===


	/// 處理敵人意圖生成：查詢 AI 策略表，決定敵人行動

	private static PhaseResult ExecuteEnemyIntentGeneration(ref CombatState state)
	{
		// 查詢 AI 策略表，生成敵人意圖
		var intent = CombatPipeline.GenerateEnemyIntent(state);

		// 設定 Intent 並推進到計劃階段
		state.PhaseCtx.SetIntent(intent);
		state.PhaseCtx.Step = PhaseStep.EnemyPlanning;
		return PhaseResult.Continue;
	}


	/// 處理敵人管線處理：將意圖轉換為執行計劃

	private static PhaseResult ExecuteEnemyPipelineProcessing(ref CombatState state)
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
			GD.PrintErr($"[PhaseRunner] No enemy Intent.");
#endif
			return PhaseResult.Continue;
		}

		// 保存轉換結果並推進到執行階段
		state.PhaseCtx.SetTranslation(translationResult);
		state.PhaseCtx.Step = PhaseStep.EnemyExecInstant;
		return PhaseResult.Continue;
	}

	private static PhaseResult ExecuteEnemyExecution(ref CombatState state)
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

	// === 錯誤處理 ===

	private static PhaseResult HandleUnknownServiceRequest(ref CombatState state, PhaseStep step, PhaseResult result)
	{
		// 這是個嚴重的邏輯錯誤，因為一個 Phase Function 請求了一項服務，
		// 但 Dispatcher 不知道如何處理。
		// 如果我們返回 Continue，將會導致無窮迴圈，因為 Step 沒有改變。
#if DEBUG
		GD.PrintErr($"[PhaseRunner] Unknown service request combination: Step={step}, Result={result}. Halting execution.");
#endif

		// 返回 Interrupt 來中斷流程，防止無窮迴圈。
		// 上層邏輯可以捕獲這個狀態並進行錯誤處理或日誌記錄。
		return PhaseResult.Interrupt;
	}

	// === 輔助方法 ===

	/// 檢查玩家是否可以執行動作
	private static bool CanPlayerAct(PhaseContext ctx)
	{
		return ctx.Step == PhaseStep.PlayerInput;
	}

	/// 檢查敵人是否可以執行動作
	private static bool CanEnemyAct(PhaseContext ctx)
	{
		return ctx.Step == PhaseStep.EnemyIntent ||
			   ctx.Step == PhaseStep.EnemyPlanning;
	}

	private static bool IsStoppingResult(PhaseResult result)
	{
		return result == PhaseResult.WaitInput ||
			   result == PhaseResult.Pending ||
			   result == PhaseResult.Interrupt ||
			   result == PhaseResult.CombatEnd;
	}
	
}
