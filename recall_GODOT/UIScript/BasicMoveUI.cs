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
		BtnA.Pressed += () => TryRunBasicSlot(101, 1); // Basic Attack slot
		BtnB.Pressed += () => TryRunBasicSlot(102, null); // Basic Block slot
		BtnC.Pressed += () => TryRunBasicSlot(103, null); // Basic Charge slot
		BtnEnd.Pressed += () => CombatCtrl.TryEndTurn();
	}

	private void TryRunBasicSlot(int actId, int? targetId)
	{
		// Find the Basic Action Slot Act by ID
		var slots = CombatCtrl.State.actStore.ToSlots();
		foreach (var act in slots)
		{
			if (act?.Id == actId)
			{
				CombatCtrl.TryRunAct(act, targetId);
				break;
			}
		}
	}

}
