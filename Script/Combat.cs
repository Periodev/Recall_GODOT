using Godot;
using System;
using CombatCore;

public partial class Combat : Control
{
	[Export] public CombatState CombatState;	// manually bind model.cs in inspector
	[Export] public PlayerView PlayerView;		// manually bind view.tscn instance in inspector

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

		PlayerView.BindActor(CombatState.Player);


		CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx);

	}
	
}
