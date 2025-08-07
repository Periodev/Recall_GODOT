using Godot;
using System;

public partial class PlayerView : Node2D
{
	[Export] public Label HpLabel;

	private Actor _actor;
	
	public void BindActor(Actor actor)
	{
		_actor = actor;
		UpdateVisual();
	}

	public void UpdateVisual()
	{
		if (_actor == null) return;
		HpLabel.Text = $"HP: {_actor.HP}/{_actor.MaxHP}";
	}

	
}
