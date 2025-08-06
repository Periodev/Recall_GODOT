using Godot;
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


/*
	public static PhaseResult RunPhase(ref PhaseContext ctx)
	{
		var rslt = PhaseResult.Continue;

		switch (ctx.Step)
		{
			case PhaseStep.TurnStart:
				ctx.TurnNum++;
				ctx.Step = PhaseStep.EnemyInit;
				break;

			case PhaseStep.EnemyInit:
				ctx.Step = PhaseStep.EnemyIntent;
				break; 

			case PhaseStep.EnemyIntent:
				ctx.Step = PhaseStep.EnemyExecInstant;
				break;

			case PhaseStep.EnemyExecInstant:
				ctx.Step = PhaseStep.PlayerInit;
				break;
		
			case PhaseStep.PlayerInit:
				ctx.Step = PhaseStep.PlayerDraw;
				break;

			case PhaseStep.PlayerDraw:
				ctx.Step = PhaseStep.PlayerInput;
				break;
				
			case PhaseStep.PlayerInput:
				rslt = PhaseResult.WaitInput;
				break;

			case PhaseStep.PlayerExecute:
				ctx.Step = PhaseStep.PlayerExecute;
				break;

			case PhaseStep.EnemyExecDelayed:
				ctx.Step = PhaseStep.TurnEnd;
				break;

			case PhaseStep.TurnEnd:
				ctx.Step = PhaseStep.TurnStart;
				break;

			case PhaseStep.CombatEnd:
			default:
				ctx.Step = PhaseStep.CombatEnd;
				break;
		}

		return rslt;
	}

	*/
}
