using Godot;
using System;
using CombatCore;

public partial class Combat : Control
{
	[Export] public CombatState CombatState;
	
	public override void _Ready()
	{
		GD.Print("Combat is ready");
		// Initialize combat state
		if (CombatState == null)
		{
			//AddChild(CombatState);
			GD.Print("need to create CombatState");		
		}
		if (CombatState != null)
		{
			GD.Print("read CombatState success");
		}

		CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx);

	}
	
}
