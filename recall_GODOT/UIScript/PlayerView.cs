using Godot;
using System;
using CombatCore;

public partial class PlayerView : Node2D
{

	[Export] public Label HpLabel;
	[Export] public Label ShieldLabel;
	[Export] public Label CopyLabel;

	
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
		CopyLabel.Text = $"Copy: {_actor.Copy?.Value ?? 0}/2";
		ShieldLabel.Text = $"Shield: {_actor.Shield?.Value ?? 0}";
	}
}
