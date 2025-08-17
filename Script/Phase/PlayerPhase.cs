#if DEBUG
using Godot;
#endif
using System;
using CombatCore;


/// 玩家階段處理 - 純狀態轉換邏輯
/// 移除了所有 CombatPipeline 調用，只負責 Phase 狀態轉換

public static class PlayerPhase
{

	/// 玩家初始化階段
	public static PhaseResult Init(ref PhaseContext ctx)
	{
		ctx.Step = PhaseStep.PlayerInit;
		return PhaseResult.RequiresSysInit;
	}


	/// 玩家抽牌階段
	public static PhaseResult Draw(ref PhaseContext ctx)
	{
		// 觸發 UI 事件通知
		UISignalHub.NotifyPlayerDrawComplete();

		ctx.Step = PhaseStep.PlayerInput;
		return PhaseResult.Continue;
	}


	/// 玩家輸入階段 - 等待玩家操作
	public static PhaseResult Input(ref PhaseContext ctx)
	{
		// 檢查是否有 Intent 需要處理
		if (ctx.HasPendingIntent)
		{
#if DEBUG
			GD.Print($"Intent available: {ctx.PendingIntent}");
#endif

			ctx.Step = PhaseStep.PlayerPlanning;
			return PhaseResult.Continue;
		}

		// 沒有 Intent，繼續等待輸入
		ctx.Step = PhaseStep.PlayerInput;
		return PhaseResult.WaitInput;
	}

	/// 玩家計劃階段 - 請求 CombatPipeline 服務
	public static PhaseResult Planning(ref PhaseContext ctx)
	{
		return PhaseResult.RequiresPipeline;
	}

	/// 玩家執行階段 - 請求命令執行服務
	public static PhaseResult Execute(ref PhaseContext ctx)
	{
		return PhaseResult.RequiresExecution;
	}
}
