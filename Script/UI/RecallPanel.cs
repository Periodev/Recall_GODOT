using Godot;
using System;
using System.Collections.Generic;
using CombatCore; // 為了 ActionType

public partial class RecallPanel : Control
{
	public enum RecallState { EnemyPhase, PlayerPhase, Selecting }

	[Export] public Button BtnRecall;
	[Export] public Button BtnConfirm;
	[Export] public Button BtnCancel;
	[Export] public Control Timeline;       // 內含 5 顆 BtnSlot*

	[Signal] public delegate void ConfirmPressedEventHandler(int[] indices);

	private readonly List<Button> _slots = new();
	private RecallState _state = RecallState.EnemyPhase;
	private List<int> _selected = new();
	private bool[] _currentTurnSlots = Array.Empty<bool>(); // 本回合的槽位（不可選）

	public override void _Ready()
	{
		// 綁定按鈕事件
		if (BtnRecall != null) BtnRecall.Pressed += OnRecallPressed;
		if (BtnConfirm != null) BtnConfirm.Pressed += OnConfirmPressed;
		if (BtnCancel != null) BtnCancel.Pressed += OnCancelPressed;

		// 收集 Timeline 內的所有 Button 作為槽位
		_slots.Clear();
		if (Timeline != null)
			CollectButtonsRecursive(Timeline, _slots);

		// 綁定槽位點擊事件
		for (int i = 0; i < _slots.Count; i++)
		{
			int idx = i; // 閉包捕獲
			_slots[i].Pressed += () => OnSlotPressed(idx);
		}

		// 初始化
		_currentTurnSlots = new bool[_slots.Count];
		
		// 初始狀態
		SetState(RecallState.EnemyPhase);
		//SetState(RecallState.PlayerPhase);
		
		GD.Print($"[RecallPanel] Ready - {_slots.Count} slots, state: {_state}");
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

		_currentTurnSlots = new bool[_slots.Count];

		for (int i = 0; i < _slots.Count; i++)
		{
			var b = _slots[i];
			if (i < ops.Count)
			{
				b.Text = OpToChar(ops[i]);
				bool isCur = (i < turns.Count && turns[i] == currentTurn);
				_currentTurnSlots[i] = isCur;  // 本回合的槽位標記
				b.Visible = true;
			}
			else
			{
				b.Text = "-";
				_currentTurnSlots[i] = false;  // 空槽位不是本回合
				b.Visible = true;
			}
		}

		// 重新應用當前狀態的 UI
		ApplyCurrentStateUI();
	}

	private static string OpToChar(ActionType op) => op switch
	{
		ActionType.A => "A",
		ActionType.B => "B",
		ActionType.C => "C",
		_ => "-"
	};

	// === 狀態管理 === //

	public void SetState(RecallState newState)
	{
		GD.Print($"[RecallPanel] State: {_state} → {newState}");
		_state = newState;
		
		// 清除選取（除非是在 Selecting 狀態內操作）
		if (newState != RecallState.Selecting)
		{
			_selected.Clear();
		}
		
		ApplyCurrentStateUI();
	}

	private void ApplyCurrentStateUI()
	{
		switch (_state)
		{
			case RecallState.EnemyPhase:
				SetEnemyPhaseUI();
				break;
			case RecallState.PlayerPhase:
				SetPlayerPhaseUI();
				break;
			case RecallState.Selecting:
				SetSelectingUI();
				break;
		}
	}

	// === 輔助方法 === //

	private void SetButtonStates(bool recallEnabled, bool confirmEnabled, bool cancelEnabled)
	{
		if (BtnRecall != null) BtnRecall.Disabled = !recallEnabled;
		if (BtnConfirm != null) BtnConfirm.Disabled = !confirmEnabled;
		if (BtnCancel != null) BtnCancel.Disabled = !cancelEnabled;
	}

	private void SetEnemyPhaseUI()
	{
		// 所有按鈕禁用
		SetButtonStates(false, false, false);
		
		// 所有槽位禁用，恢復原色
		foreach (var slot in _slots)
		{
			slot.Disabled = true;
			slot.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		}
	}

	private void SetPlayerPhaseUI()
	{
		// Recall 可用，其他禁用
		SetButtonStates(true, false, false);
		
		// 槽位純顯示，不可點擊，恢復原色
		foreach (var slot in _slots)
		{
			slot.Disabled = true;
			slot.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		}
	}

	private void SetSelectingUI()
	{
		// Recall 禁用，Cancel 可用，Confirm 動態
		SetButtonStates(false, _selected.Count > 0, true);
		
		// 槽位根據條件設定
		for (int i = 0; i < _slots.Count; i++)
		{
			var slot = _slots[i];
			
			// 空槽位或本回合的槽位不可選
			bool isEmptySlot = (i >= _currentTurnSlots.Length) || (slot.Text == "-");
			bool isCurrentTurn = (i < _currentTurnSlots.Length) && _currentTurnSlots[i];
			
			slot.Disabled = isEmptySlot || isCurrentTurn;
			
			// 更新選取外觀
			UpdateSlotAppearance(i, _selected.Contains(i));
		}
	}

	// === 按鈕事件處理 === //

	private void OnSlotPressed(int idx)
	{
		if (_state != RecallState.Selecting) return;
		if (_slots[idx].Disabled) return;
		
		// 切換選取狀態
		bool wasSelected = _selected.Contains(idx);
		if (wasSelected)
		{
			_selected.Remove(idx);
		}
		else
		{
			_selected.Add(idx);
		}
		
		// 更新視覺外觀
		UpdateSlotAppearance(idx, !wasSelected);
		
		GD.Print($"[RecallPanel] Slot {idx} {(wasSelected ? "deselected" : "selected")}, total: {_selected.Count}");
		
		// 動態更新 Confirm 按鈕
		SetButtonStates(false, _selected.Count > 0, true);
	}

	private void UpdateSlotAppearance(int idx, bool selected)
	{
		var slot = _slots[idx];
		if (selected)
		{
			// 選中：藍色背景
			slot.Modulate = new Color(0.3f, 0.5f, 1.0f, 1.0f);
		}
		else
		{
			// 未選中：恢復原色
			slot.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		}
	}

	private void OnRecallPressed()
	{
		if (_state != RecallState.PlayerPhase) return;
		SetState(RecallState.Selecting);
	}

	private void OnConfirmPressed()
	{
		if (_state != RecallState.Selecting || _selected.Count == 0) return;
		
		GD.Print($"[RecallPanel] Confirm with selection: [{string.Join(", ", _selected)}]");
		EmitSignal(SignalName.ConfirmPressed, _selected.ToArray());
		
		SetState(RecallState.PlayerPhase);
	}

	private void OnCancelPressed()
	{
		if (_state != RecallState.Selecting) return;
		SetState(RecallState.PlayerPhase);
	}

	// === 公開介面 === //

	/// <summary>
	/// 外部 Phase 系統呼叫：進入敵人回合
	/// </summary>
	public void EnterEnemyPhase() => SetState(RecallState.EnemyPhase);

	/// <summary>
	/// 外部 Phase 系統呼叫：進入玩家回合
	/// </summary>
	public void EnterPlayerPhase() => SetState(RecallState.PlayerPhase);

	/// <summary>
	/// 取得當前狀態
	/// </summary>
	public RecallState CurrentState => _state;

	/// <summary>
	/// 取得當前選取的索引
	/// </summary>
	public IReadOnlyList<int> SelectedIndices => _selected.AsReadOnly();
}
