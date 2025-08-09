using Godot;
using System;
using CombatCore;

public partial class PlayerView : Node2D
{
	[Export] public Combat Control;

	[Export] public Label HpLabel;
	[Export] public Label ChargeLabel;

	private Actor _actor;
	
	public void BindActor(Actor actor)
	{
		_actor = actor;
		UpdateVisual();
	}

	public override void _Ready()
	{
	}


	public void UpdateVisual()
	{
		if (_actor == null) return;
		HpLabel.Text = $"HP: {_actor.HP.Value}/{_actor.HP.Max}";
		ChargeLabel.Text = $"Charge: {_actor.Charge?.Value ?? 0}/3";
	}
}
