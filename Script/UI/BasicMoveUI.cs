using Godot;
using System;
using CombatCore;

public partial class BasicMoveUI : Control
{
	// manually bind in inspector
	[Export] private Button ButtonA;
	[Export] private Button ButtonB;
	[Export] private Button ButtonC;

	public override void _Ready()
	{
		ButtonA.Pressed += () => OnBasicMovePressed("A");
		ButtonB.Pressed += () => OnBasicMovePressed("B");
		ButtonC.Pressed += () => OnBasicMovePressed("C");
	}


	private void OnBasicMovePressed(string moveId)
	{
		GD.Print($"[UI] Basic Move {moveId} pressed");
		// 🔹 這裡可以直接呼叫 CombatState 或 Kernel 的 API
		// CombatControl.Instance.QueueBasicMove(moveId);
	}

	
}
