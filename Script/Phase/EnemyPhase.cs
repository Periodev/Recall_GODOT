
using System;
using CombatCore;

/// <summary>
/// 敵人階段處理 - 純狀態轉換邏輯
/// 移除了所有 CombatPipeline 調用，只負責 Phase 狀態轉換
/// </summary>
public static class EnemyPhase
{
	/// <summary>
	/// 敵人初始化階段
	/// </summary>
	public static PhaseResult Init(ref PhaseContext ctx)
	{
		ctx.Step = PhaseStep.EnemyIntent;
		return PhaseResult.Continue;
	}

	/// <summary>
	/// 敵人意圖階段 - 展示敵人下一步行動
	/// </summary>
	public static PhaseResult Intent(ref PhaseContext ctx)
	{
		// 推進到敵人計劃階段（由 PhaseRunner 處理）
		ctx.Step = PhaseStep.EnemyPlanning;
		return PhaseResult.Continue;
	}

	/// <summary>
	/// 敵人延遲執行階段 - 處理需要延後執行的效果
	/// </summary>
	public static PhaseResult ExecDelayed(ref PhaseContext ctx)
	{
		// TODO: 處理延遲效果（如持續傷害、狀態過期等）
		
		ctx.Step = PhaseStep.TurnEnd;
		return PhaseResult.Continue;
	}

	// 注意：EnemyPlanning 和 EnemyExecInstant 方法已移除
	// 這些複雜邏輯現在由 PhaseRunner.ExecuteEnemyPlanning() 和 
	// PhaseRunner.ExecuteEnemyExecInstant() 處理
}
