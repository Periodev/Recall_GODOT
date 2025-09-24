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
		SignalHub.NotifyEnemySlotClicked(SlotIndex, EnemyId);
	}

	public void BindActor(Actor actor)
	{
		if (actor != null)
		{
			IDLabel.Text = $"ID:{actor.Id}";
			HPLabel.Text = $"HP:{actor.HP.Value}";
			ShieldLabel.Text = $"Shield:{actor.Shield.Value}";
			Disabled = false;
		}
		else
		{
			IDLabel.Text = "";
			HPLabel.Text = "";
			ShieldLabel.Text = "";
			Disabled = true;
		}
	}

	public void SetSelected(bool selected)
	{
		Modulate = selected ? Colors.LightBlue : Colors.White;
	}
}
