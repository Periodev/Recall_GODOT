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
	// === 保留原有簽名的方法（向後相容） ===
	
	public static PhaseResult Run(ref PhaseContext ctx)
	{
		// 對於簡單 Phase，直接查表執行
		if (PhaseMap.StepFuncs.TryGetValue(ctx.Step, out var stepFunc))
		{
			return stepFunc(ref ctx);
		}
		
#if DEBUG
		GD.PrintErr($"[PhaseRunner] Unknown phase step: {ctx.Step}. Halting execution to prevent infinite loop.");
#endif
		// 未知的 Phase Step，這通常是一個錯誤（例如 PhaseMap 沒定義）。
		// 返回 Interrupt 而不是 Continue，以防止無窮迴圈。
		// 讓上層調用者決定如何處理這個中斷。
		return PhaseResult.Interrupt;
	}

	public static PhaseResult AdvanceUntilInput(ref PhaseContext ctx)
	{
		PhaseResult result = PhaseResult.Continue;
		int maxIterations = 100; // 🚨 緊急保護：最多執行 100 步
		int iterations = 0;

		while (result == PhaseResult.Continue)
		{
			iterations++;
			result = Run(ref ctx);

			if (IsStoppingResult(result))
				break;


			if (iterations >= maxIterations)
			{
				break;
			}

		}

#if DEBUG
		if (iterations >= maxIterations)
		{
			GD.PrintErr($"[PhaseRunner] Max iterations reached! Current step: {ctx.Step}");
		}
#endif


		return result;
	}

	// === 新增的 CombatState 版本（服務調度器） ===

	/// 執行單個 Phase 步驟並提供服務調度
	
	public static PhaseResult Run(CombatState state)
	{
		var step = state.PhaseCtx.Step;
		
		// 先執行標準 Phase 邏輯
		if (PhaseMap.StepFuncs.TryGetValue(step, out var stepFunc))
		{
			var result = stepFunc(ref state.PhaseCtx);
			
			// 🎯 根據 Step + Result 組合決定服務調度
			return DispatchService(state, step, result);
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
	
	public static PhaseResult AdvanceUntilInput(CombatState state)
	{
		int maxIterations = 100; // 🚨 緊急保護：最多執行 100 步
		int iterations = 0;

		PhaseResult result = PhaseResult.Continue;
		
		while (result == PhaseResult.Continue)
		{
			iterations++;
			if (iterations >= maxIterations)
			{
				break;
			}

			result = Run(state);
			
			if (IsStoppingResult(result))
				break;
		}
		
#if DEBUG
		if (iterations >= maxIterations)
		{
			GD.PrintErr($"[PhaseRunner] Max iterations reached! Current step: {state.PhaseCtx.Step}");
		}
#endif
		return result;
	}

	// === 服務調度核心 ===

	
	/// 根據 PhaseStep + PhaseResult 組合調度對應服務
	
	private static PhaseResult DispatchService(CombatState state, PhaseStep step, PhaseResult result)
	{
		// 如果不是服務請求，直接返回原結果
		if (!result.IsServiceRequest())
			return result;

		// 根據 Step + Result 組合調度服務
		return (step, result) switch
		{
			// === Player Phase 服務 ===
			(PhaseStep.PlayerPlanning, PhaseResult.RequiresPipeline) 
				=> ExecutePlayerPlanning(state),
				
			(PhaseStep.PlayerExecute, PhaseResult.RequiresExecution) 
				=> ExecutePlayerExecution(state),

			// === Enemy Phase 服務 ===
			(PhaseStep.EnemyIntent, PhaseResult.RequiresAI) 
				=> ExecuteEnemyIntentGeneration(state),
				
			(PhaseStep.EnemyPlanning, PhaseResult.RequiresPipeline) 
				=> ExecuteEnemyPipelineProcessing(state),
				
			(PhaseStep.EnemyExecInstant, PhaseResult.RequiresExecution) 
				=> ExecuteEnemyExecution(state),


			// === 未知組合，返回錯誤 ===
			_ => HandleUnknownServiceRequest(state, step, result)
		};
	}

	// === Player 服務實現 ===

	private static PhaseResult ExecutePlayerPlanning(CombatState state)
	{
		// 檢查是否有待處理的 Intent
		if (!state.PhaseCtx.TryConsumeIntent(out var intent))
		{
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 使用 CombatPipeline 轉換 Intent
		var translationResult = CombatPipeline.TranslateIntent(state, state.Player, intent);
		
		if (!translationResult.Success)
		{
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 保存轉換結果並推進
		state.PhaseCtx.SetTranslation(translationResult);
		state.PhaseCtx.Step = PhaseStep.PlayerExecute;
		return PhaseResult.Continue;
	}

	private static PhaseResult ExecutePlayerExecution(CombatState state)
	{
		if (!state.PhaseCtx.TryConsumeTranslation(out var translation))
		{
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		var execResult = CombatPipeline.ExecuteCommands(state, translation.Commands, translation.OriginalIntent);
		
		if (!execResult.Success)
		{
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		state.PhaseCtx.Step = PhaseStep.EnemyExecDelayed;
		return PhaseResult.Continue;
	}

	// === Enemy 服務實現 ===

	
	/// 處理敵人意圖生成：查詢 AI 策略表，決定敵人行動
	
	private static PhaseResult ExecuteEnemyIntentGeneration(CombatState state)
	{
		// 查詢 AI 策略表，生成敵人意圖
		var intent = CombatPipeline.GenerateEnemyIntent(state);
		
		// 設定 Intent 並推進到計劃階段
		state.PhaseCtx.SetIntent(intent);
		state.PhaseCtx.Step = PhaseStep.EnemyPlanning;
		return PhaseResult.Continue;
	}

	
	/// 處理敵人管線處理：將意圖轉換為執行計劃
	
	private static PhaseResult ExecuteEnemyPipelineProcessing(CombatState state)
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

	private static PhaseResult ExecuteEnemyExecution(CombatState state)
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

	private static PhaseResult HandleUnknownServiceRequest(CombatState state, PhaseStep step, PhaseResult result)
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

	private static bool IsStoppingResult(PhaseResult result)
	{
		return result == PhaseResult.WaitInput || 
			   result == PhaseResult.Pending || 
			   result == PhaseResult.Interrupt ||
			   result == PhaseResult.CombatEnd;
	}
}
