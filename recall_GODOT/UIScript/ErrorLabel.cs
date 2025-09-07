using Godot;
using System;
using System.Collections.Generic;
using CombatCore;
using CombatCore.UI;


public partial class ErrorLabel : Control
{
	[Export] public Label Label;

	public void ShowError(FailCode failCode)
	{
		Label.Text = ErrorMessage.Get(failCode);
		Label.Modulate = new Color(1f, 0.3f, 0.3f); // 紅色
		GetTree().CreateTimer(3.0f).Timeout += () => Label.Text = "";
	}
}
