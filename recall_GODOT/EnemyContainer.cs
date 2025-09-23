using Godot;
using System;
using CombatCore;
using CombatCore.UI;

public partial class EnemyContainer : Control
{
	private EnemySlot[] slots = new EnemySlot[6];
	private int? selectedSlotIndex = null;
	
	public override void _Ready()
	{
		var gridContainer = GetNode<GridContainer>("GridContainer");
		
		for (int i = 0; i < 6; i++)
		{
			slots[i] = gridContainer.GetNode<EnemySlot>($"EnemySlot{i}");
			slots[i].SlotIndex = i;
		}

		SignalHub.OnEnemySlotClicked += OnSlotClicked;
	}
	
	private void OnSlotClicked(int slotIndex, int? enemyId)
	{

		selectedSlotIndex = slotIndex;
		//TODO 	UpdateSelection();
		SignalHub.NotifyEnemySelected(enemyId);
	}
}
