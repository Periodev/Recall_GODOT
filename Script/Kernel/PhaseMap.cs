
using System;
using System.Collections.Generic;
using CombatCore;

public static class PhaseMap
{
	public delegate PhaseResult StepFunc(ref PhaseContext ctx);        // typedef PhaseResult (*StepFunc)(PhaseContext* ctx);

	/// <summary>
	/// 簡單 Phase 的映射表
	/// 只包含純狀態轉換的 Phase，不包含需要業務邏輯的複雜 Phase
	/// </summary>
	public static readonly Dictionary<PhaseStep, StepFunc> StepFuncs = new()
	{
		// === Turn Phase ===
		{ PhaseStep.TurnStart, TurnStart },
		{ PhaseStep.TurnEnd, TurnEnd },
		{ PhaseStep.CombatEnd, CombatEnd },

		// === Enemy Phase - 簡單轉換 ===
		{ PhaseStep.EnemyInit, EnemyPhase.Init },
		{ PhaseStep.EnemyIntent, EnemyPhase.Intent },
		{ PhaseStep.EnemyExecDelayed, EnemyPhase.ExecDelayed },

		// === Player Phase - 簡單轉換 ===
		{ PhaseStep.PlayerInit, PlayerPhase.Init },
		{ PhaseStep.PlayerDraw, PlayerPhase.Draw },
		{ PhaseStep.PlayerInput, PlayerPhase.Input },

		// 注意：以下複雜 Phase 不在此表中，由 PhaseRunner 直接處理：
		// - PhaseStep.PlayerPlanning
		// - PhaseStep.PlayerExecute  
		// - PhaseStep.EnemyPlanning
		// - PhaseStep.EnemyExecInstant
	};

	// === 簡單 Phase 處理方法 ===

	private static PhaseResult TurnStart(ref PhaseContext ctx)
	{
		ctx.StartNewTurn(); // 使用 PhaseContext 的內建方法
		ctx.Step = PhaseStep.EnemyInit;
		return PhaseResult.Continue;
	}

	private static PhaseResult TurnEnd(ref PhaseContext ctx)
	{
		ctx.Step = PhaseStep.TurnStart;
		return PhaseResult.Continue;
	}

	private static PhaseResult CombatEnd(ref PhaseContext ctx)
	{
		ctx.Step = PhaseStep.CombatEnd;
		return PhaseResult.CombatEnd;
	}
}
