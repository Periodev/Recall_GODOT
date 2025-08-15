
using System;
using System.Collections.Generic;
using CombatCore;

public static class PhaseMap
{
	public delegate PhaseResult StepFunc(ref PhaseContext ctx);        // typedef PhaseResult (*StepFunc)(PhaseContext* ctx);

	public static readonly Dictionary<PhaseStep, StepFunc> StepFuncs = new()
	{
		{ PhaseStep.TurnStart, TurnStart },
		{ PhaseStep.EnemyInit, EnemyPhase.Init },
		{ PhaseStep.EnemyIntent, EnemyPhase.Intent },
		{ PhaseStep.EnemyExecInstant, EnemyPhase.ExecInstant },
		{ PhaseStep.EnemyExecDelayed, EnemyPhase.ExecDelayed },
		{ PhaseStep.PlayerInit, PlayerPhase.Init },
		{ PhaseStep.PlayerDraw, PlayerPhase.Draw },
		{ PhaseStep.PlayerInput, PlayerPhase.Input },
		{ PhaseStep.PlayerExecute, PlayerPhase.Execute },
		{ PhaseStep.TurnEnd, TurnEnd },
		{ PhaseStep.CombatEnd, CombatEnd } 
	};

	private static PhaseResult TurnStart(ref PhaseContext ctx)
	{
		ctx.TurnNum++;
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
