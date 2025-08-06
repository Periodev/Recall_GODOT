using Godot;
using System;
using CombatCore;

public static class CombatKernel
{
	public static PhaseResult Run(ref PhaseContext ctx)
	{
		PhaseResult rslt = PhaseMap.StepFuncs[ctx.Step](ref ctx);
		return rslt;
	}

	public static PhaseResult Run(ref CombatState state)
	{
		return Run(ref state.PhaseCtx);
	}

	public static PhaseResult AdvanceUntilInput(ref PhaseContext ctx)
	{
		PhaseResult rslt = PhaseResult.Continue;
		
		while (rslt == PhaseResult.Continue)
		{
			rslt = Run(ref ctx);

			if (rslt == PhaseResult.WaitInput)
			{
				GD.Print("Wait Input");
				break;
			}
			else if (rslt == PhaseResult.Pending)
			{
				GD.Print("Pending");
				break;
			}
			else if (rslt == PhaseResult.Interrupt)
			{
				GD.Print("Interrupt");
				break;
			}
			else
			{
				GD.Print("Continue to next step ", ctx.Step.ToString());
			}
		}

		return rslt;

	}

	public static PhaseResult AdvanceUntilInput(ref CombatState state)
	{
		return AdvanceUntilInput(ref state.PhaseCtx);
	}

}
