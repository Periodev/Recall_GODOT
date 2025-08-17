using System;
using CombatCore;
using CombatCore.Command;
using CombatCore.InterOp;

/// <summary>
/// Phase 流程控制器 - 統一管理戰鬥階段推進
/// 核心原則："Phase 決定何時與誰，Pipeline 決定做什麼；State 是唯一事實來源"
/// </summary>
public static class PhaseRunner
{
	// === 保留原有簽名的方法（向後相容） ===
	
	/// <summary>
	/// 執行單個 Phase 步驟（原有方法，保持向後相容）
	/// </summary>
	public static PhaseResult Run(ref PhaseContext ctx)
	{
		// 對於簡單 Phase，直接查表執行
		if (PhaseMap.StepFuncs.TryGetValue(ctx.Step, out var stepFunc))
		{
			return stepFunc(ref ctx);
		}
		
		// 對於複雜 Phase，需要 CombatState 支援
		// 這裡只能返回 Pending，要求外部使用 Run(CombatState) 版本
		return PhaseResult.Pending;
	}

	/// <summary>
	/// 推進直到需要輸入（原有方法，保持向後相容）
	/// </summary>
	public static PhaseResult AdvanceUntilInput(ref PhaseContext ctx)
	{
		PhaseResult result = PhaseResult.Continue;
		
		while (result == PhaseResult.Continue)
		{
			result = Run(ref ctx);
			
			if (result == PhaseResult.WaitInput || 
				result == PhaseResult.Pending || 
				result == PhaseResult.Interrupt ||
				result == PhaseResult.CombatEnd)
			{
				break;
			}
		}

		return result;
	}

	// === 新增的 CombatState 版本（推薦使用） ===

	/// <summary>
	/// 執行單個 Phase 步驟（新版本，支援複雜 Phase）
	/// </summary>
	public static PhaseResult Run(CombatState state)
	{
		var step = state.PhaseCtx.Step;
		
		// 先嘗試使用傳統 Phase 處理
		if (PhaseMap.StepFuncs.TryGetValue(step, out var stepFunc))
		{
			return stepFunc(ref state.PhaseCtx);
		}
		
		// 處理需要 CombatPipeline 的複雜 Phase
		return step switch
		{
			PhaseStep.PlayerPlanning => ExecutePlayerPlanning(state),
			PhaseStep.PlayerExecute => ExecutePlayerExecute(state),
			PhaseStep.EnemyPlanning => ExecuteEnemyPlanning(state),
			PhaseStep.EnemyExecInstant => ExecuteEnemyExecInstant(state),
			_ => PhaseResult.Continue // 未知 Phase，直接繼續
		};
	}

	/// <summary>
	/// 推進直到需要輸入（新版本，支援複雜 Phase）
	/// </summary>
	public static PhaseResult AdvanceUntilInput(CombatState state)
	{
		PhaseResult result = PhaseResult.Continue;
		
		while (result == PhaseResult.Continue)
		{
			result = Run(state);
			
			if (result == PhaseResult.WaitInput || 
				result == PhaseResult.Pending || 
				result == PhaseResult.Interrupt ||
				result == PhaseResult.CombatEnd)
			{
				break;
			}
		}

		return result;
	}

	// === 複雜 Phase 處理方法 ===

	/// <summary>
	/// 處理玩家計劃階段：將 Intent 轉換為 Commands
	/// </summary>
	private static PhaseResult ExecutePlayerPlanning(CombatState state)
	{
		// 檢查是否有待處理的 Intent
		if (!state.PhaseCtx.TryConsumeIntent(out var intent))
		{
			// 沒有 Intent，回到輸入階段
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 使用 CombatPipeline 轉換 Intent
		var translationResult = CombatPipeline.TranslateIntent(state, state.Player, intent);
		
		if (!translationResult.Success)
		{
			// 轉換失敗，回到輸入階段
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 保存轉換結果並推進到執行階段
		state.PhaseCtx.SetTranslation(translationResult);
		state.PhaseCtx.Step = PhaseStep.PlayerExecute;
		return PhaseResult.Continue;
	}

	/// <summary>
	/// 處理玩家執行階段：執行已轉換的 Commands
	/// </summary>
	private static PhaseResult ExecutePlayerExecute(CombatState state)
	{
		// 檢查是否有待執行的轉換結果
		if (!state.PhaseCtx.TryConsumeTranslation(out var translation))
		{
			// 沒有待執行的命令，回到輸入階段
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 執行命令
		var execResult = CombatPipeline.ExecuteCommands(state, translation.Commands, translation.OriginalIntent);
		
		if (!execResult.Success)
		{
			// 執行失敗，回到輸入階段
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.WaitInput;
		}

		// 執行成功，推進到敵人階段
		state.PhaseCtx.Step = PhaseStep.EnemyExecDelayed;
		return PhaseResult.Continue;
	}

	/// <summary>
	/// 處理敵人計劃階段：生成 AI Intent 並轉換為 Commands
	/// </summary>
	private static PhaseResult ExecuteEnemyPlanning(CombatState state)
	{
		// 生成敵人 AI 意圖
		var intent = CombatPipeline.GenerateEnemyIntent(state);
		
		// 轉換為命令
		var translationResult = CombatPipeline.TranslateIntent(state, state.Enemy, intent);
		
		if (!translationResult.Success)
		{
			// AI 轉換失敗，跳過敵人行動
			state.PhaseCtx.Step = PhaseStep.EnemyExecDelayed;
			return PhaseResult.Continue;
		}

		// 保存轉換結果並推進到執行階段
		state.PhaseCtx.SetTranslation(translationResult);
		state.PhaseCtx.Step = PhaseStep.EnemyExecInstant;
		return PhaseResult.Continue;
	}

	/// <summary>
	/// 處理敵人即時執行階段：執行已轉換的 Commands
	/// </summary>
	private static PhaseResult ExecuteEnemyExecInstant(CombatState state)
	{
		// 檢查是否有待執行的轉換結果
		if (!state.PhaseCtx.TryConsumeTranslation(out var translation))
		{
			// 沒有待執行的命令，跳過
			state.PhaseCtx.Step = PhaseStep.PlayerInit;
			return PhaseResult.Continue;
		}

		// 執行命令
		var execResult = CombatPipeline.ExecuteCommands(state, translation.Commands, translation.OriginalIntent);
		
		// 無論成功與否，都推進到玩家階段
		state.PhaseCtx.Step = PhaseStep.PlayerInit;
		return PhaseResult.Continue;
	}
}