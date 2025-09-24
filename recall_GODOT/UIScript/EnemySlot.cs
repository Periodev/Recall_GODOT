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
		if (actor != null && actor.IsAlive)
		{
			IDLabel.Text = $"ID:{actor.Id}";
			HPLabel.Text = $"HP:{actor.HP.Value}";
			ShieldLabel.Text = $"Shield:{actor.Shield.Value}";
			Disabled = false;
			Visible = true;
		}
		else
		{
			Unbind();
		}
	}

	public void Unbind()
	{
		IDLabel.Text = "";
		IntentLabel.Text = "";
		HPLabel.Text = "";
		ShieldLabel.Text = "";
		Disabled = true;
		//Visible = false;
		// 清除選擇高亮
		SetSelected(false);
		ReleaseFocus();
	}

	public void SetSelected(bool selected)
	{
		Modulate = selected ? Colors.LightBlue : Colors.White;
	}
}
