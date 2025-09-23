using Godot;
using System;
using CombatCore;
using CombatCore.UI;

public partial class EnemySlot : Button
{
	[Export] public Label IDLabel;
	[Export] public Label IntentLabel;
	[Export] public Label HPLabel;
	[Export] public Label ShieldLabel;
	
	public int SlotIndex { get; set; }
	public int? EnemyId { get; set; }
	
	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private void OnPressed()
	{
		//TODO SignalHub.NotifyEnemySlotClicked(SlotIndex, EnemyId);
	}
}
