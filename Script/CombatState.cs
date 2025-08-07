using Godot;
using System;
using CombatCore;

public partial class CombatState : Node
{
	public PhaseContext PhaseCtx;
	public Actor Player { get; private set; }

	public CombatState()
	{
		// ✅ 這裡執行在 new 時，無法 GetNode 或操作場景
		PhaseCtx.Init();
		Player = new Actor(100);
	}


	public override void _Ready()
	{
		GD.Print("Combat state is ready");
	}


}
