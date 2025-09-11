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
		BtnA.Pressed += () => CombatCtrl.TryRunBasic(TokenType.A, 1);
		BtnB.Pressed += () => CombatCtrl.TryRunBasic(TokenType.B, null);
		BtnC.Pressed += () => CombatCtrl.TryRunBasic(TokenType.C, null);
		BtnEnd.Pressed += () => CombatCtrl.TryEndTurn();

	}

}
