using Godot;
using System;
using System.Collections.Generic;
using CombatCore; // 為了 ActionType

public partial class RecallPanel : Control
{
	[Export] public Button BtnRecall;
	[Export] public Button BtnConfirm;
	[Export] public Button BtnCancel;
	[Export] public Control Timeline;       // 內含 5 顆 BtnSlot*

	[Signal] public delegate void RecallPressedEventHandler();
	[Signal] public delegate void ConfirmPressedEventHandler();
	[Signal] public delegate void CancelPressedEventHandler();
	[Signal] public delegate void SelectionChangedEventHandler(int[] indices);

	private readonly List<Button> _slots = new();
	private bool[] _baseDisabled = Array.Empty<bool>(); // 本回合禁用等基礎禁用
	private List<int> _selected = new();                // 目前選取索引
	private bool _selecting = false;

	public override void _Ready()
	{
		// 綁按鍵
		if (BtnRecall != null) BtnRecall.Pressed += () => EmitSignal(SignalName.RecallPressed);
		if (BtnConfirm != null) BtnConfirm.Pressed += () => EmitSignal(SignalName.ConfirmPressed);
		if (BtnCancel != null) BtnCancel.Pressed += () => EmitSignal(SignalName.CancelPressed);

		// 收集 Timeline 內的所有 Button 作為槽位（依場景順序）
		_slots.Clear();
		if (Timeline != null)
			CollectButtonsRecursive(Timeline, _slots);

	}

	private static void CollectButtonsRecursive(Node node, List<Button> into)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is Button b) into.Add(b);
			if (child is Node n) CollectButtonsRecursive(n, into);
		}
	}

	public void RefreshSnapshot(IReadOnlyList<ActionType> ops, IReadOnlyList<int> turns, int currentTurn)
	{
		if (_slots.Count == 0) return;

		_baseDisabled = new bool[_slots.Count];

		for (int i = 0; i < _slots.Count; i++)
		{
			var b = _slots[i];
			if (i < ops.Count)
			{
				b.Text = OpToChar(ops[i]);
				bool isCur = (i < turns.Count && turns[i] == currentTurn);
				_baseDisabled[i] = isCur;
				// 非選取模式：僅顯示，不可互動
				b.Disabled = true;
				b.Visible = true;
				b.ButtonPressed = false;
			}
			else
			{
				b.Text = "-";
				_baseDisabled[i] = true;
				b.Disabled = true;
				b.Visible = true;
				b.ButtonPressed = false;
			}
		}
	}

	private static string OpToChar(ActionType op) => op switch
	{
		ActionType.A => "A",
		ActionType.B => "B",
		ActionType.C => "C",
		_ => "-"
	};

}
