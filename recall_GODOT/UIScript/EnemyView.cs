using Godot;
using System;
using CombatCore;

public partial class EnemyView : Node2D
{
	[Export] public Label HpLabel;
	[Export] public Label ShieldLabel;

	private Actor _actor;

	public void BindActor(Actor actor)
	{
		_actor = actor;
		UpdateVisual();
	}

	public void UpdateVisual()
	{
		if (_actor == null) return;
		HpLabel.Text = $"HP: {_actor.HP.Value}/{_actor.HP.Max}";
		//ChargeLabel.Text = $"Charge: {_actor.Charge?.Value ?? 0}/3";
		ShieldLabel.Text = $"Shield: {_actor.Shield?.Value ?? 0}";

	}

}
