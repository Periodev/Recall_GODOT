using Godot;
using System;
using CombatCore;

public partial class BasicMoveUI : Control
{
	[Export] public Combat CombatCtrl;

	// manually bind in inspector
	[Export] private Button BtnA;
	[Export] private Button BtnB;
	[Export] private Button BtnC;
	[Export] private Button BtnEnd;


	public override void _Ready()
	{
		/*
		BtnA.Pressed += () => OnBasicMovePressed("A");
		BtnB.Pressed += () => OnBasicMovePressed("B");
		BtnC.Pressed += () => OnBasicMovePressed("C");
		*/
		BtnA.Pressed += () => CombatCtrl.TryRunBasic(ActionType.A, 1);
		BtnB.Pressed += () => CombatCtrl.TryRunBasic(ActionType.B, null);
		BtnC.Pressed += () => CombatCtrl.TryRunBasic(ActionType.C, null);
		BtnEnd.Pressed += () => CombatCtrl.TryEndTurn();

	}

}
