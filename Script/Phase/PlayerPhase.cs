
using System;
using CombatCore;

/// <summary>
/// 玩家階段處理 - 純狀態轉換邏輯
/// 移除了所有 CombatPipeline 調用，只負責 Phase 狀態轉換
/// </summary>
public static class PlayerPhase
{
	/// <summary>
	/// 玩家初始化階段
	/// </summary>
	public static PhaseResult Init(ref PhaseContext ctx)
	{
		ctx.Step = PhaseStep.PlayerDraw;
		return PhaseResult.Continue;
	}

	/// <summary>
	/// 玩家抽牌階段
	/// </summary>
	public static PhaseResult Draw(ref PhaseContext ctx)
	{
		// 觸發 UI 事件通知
		UISignalHub.NotifyPlayerDrawComplete();
		
		ctx.Step = PhaseStep.PlayerInput;
		return PhaseResult.Continue;
	}

	/// <summary>
	/// 玩家輸入階段 - 等待玩家操作
	/// </summary>
	public static PhaseResult Input(ref PhaseContext ctx)
	{
		// 檢查是否有 Intent 需要處理
		if (ctx.HasPendingIntent)
		{
			// 有 Intent，推進到計劃階段
			ctx.Step = PhaseStep.PlayerPlanning;
			return PhaseResult.Continue;
		}

		// 沒有 Intent，繼續等待輸入
		ctx.Step = PhaseStep.PlayerInput;
		return PhaseResult.WaitInput;
	}

	// 注意：PlayerPlanning 和 PlayerExecute 方法已移除
	// 這些複雜邏輯現在由 PhaseRunner.ExecutePlayerPlanning() 和 
	// PhaseRunner.ExecutePlayerExecute() 處理
}
