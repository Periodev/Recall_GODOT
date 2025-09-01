using Godot;
using System;
using System.Collections.Generic;
using CombatCore;
using CombatCore.Echo;

public partial class EchoPanel : Control
{
	[Export] public Combat CombatCtrl;

	// UI 組件 - 手動在 Inspector 綁定
	[Export] private Button BtnEchoSlot0;
	[Export] private Button BtnEchoSlot1;
	[Export] private Button BtnEchoSlot2;
	[Export] private Button BtnEchoSlot3;
	[Export] private Button BtnEchoSlot4;
	[Export] private Label EchoName;
	[Export] private Label Recipe;
	[Export] private RichTextLabel Summary;
	[Export] private Button BtnPlay;
	[Export] private Button BtnCancel;
	[Export] private Label Reason;


	private readonly List<Button> _slotButtons = new();
	private int _selectedSlotIndex = -1;
	private Echo? _selectedEcho = null;

	public override void _Ready()
	{
		// 收集槽位按鈕
		_slotButtons.AddRange(new[] {
			BtnEchoSlot0, BtnEchoSlot1, BtnEchoSlot2, BtnEchoSlot3, BtnEchoSlot4
		});

		// 綁定槽位事件（閉包捕獲索引）
		for (int i = 0; i < _slotButtons.Count; i++)
		{
			int slotIndex = i; // 閉包捕獲
			_slotButtons[i].Pressed += () => OnSlotPressed(slotIndex);
		}

		// 綁定操作按鈕事件
		if (BtnPlay != null) BtnPlay.Pressed += OnPlayPressed;
		if (BtnCancel != null) BtnCancel.Pressed += OnCancelPressed;

		// 初始化 UI
		RefreshEchoSlots();
		ClearSelection();

		GD.Print("[EchoPanel] Ready - UI components bound");
	}

	public override void _ExitTree()
	{
		// 清理事件綁定
		if (BtnPlay != null) BtnPlay.Pressed -= OnPlayPressed;
		if (BtnCancel != null) BtnCancel.Pressed -= OnCancelPressed;
	}

	// === 槽位管理 ===

	/// 刷新 Echo 槽位顯示
	public void RefreshEchoSlots()
	{
		if (CombatCtrl?.State?.echoStore == null)
		{
			// 無資料時顯示空槽
			for (int i = 0; i < _slotButtons.Count; i++)
			{
				UpdateSlotButton(i, null, false);
			}
			return;
		}

		var echoSlots = CombatCtrl.State.echoStore.ToSlots();

		for (int i = 0; i < _slotButtons.Count; i++)
		{
			var echo = echoSlots[i];
			bool isSelected = (i == _selectedSlotIndex);
			UpdateSlotButton(i, echo, isSelected);
		}
	}

	/// 更新單個槽位按鈕的顯示
	private void UpdateSlotButton(int index, Echo? echo, bool isSelected)
	{
		if (index >= _slotButtons.Count) return;

		var btn = _slotButtons[index];

		if (echo == null)
		{
			// 空槽位
			btn.Text = "-";
			btn.Disabled = true;
			btn.Modulate = new Color(0.7f, 0.7f, 0.7f, 1.0f); // 灰色
		}
		else
		{
			// 有 Echo 的槽位
			btn.Text = echo.Name;
			btn.Disabled = false;

			if (isSelected)
			{
				btn.Modulate = new Color(0.3f, 0.5f, 1.0f, 1.0f); // 藍色高亮
			}
			else
			{
				btn.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f); // 原色
			}
		}
	}

	/// 處理槽位點擊事件
	private void OnSlotPressed(int slotIndex)
	{
		if (CombatCtrl?.State?.echoStore == null) return;

		// 檢查槽位是否有 Echo
		if (!CombatCtrl.State.echoStore.TryGet(slotIndex, out var echo) || echo == null)
		{
			GD.Print($"[EchoPanel] Slot {slotIndex} is empty");
			return;
		}

		// 更新選擇狀態
		_selectedSlotIndex = slotIndex;
		_selectedEcho = echo;

		GD.Print($"[EchoPanel] Selected slot {slotIndex}: {echo.Name}");

		// 刷新 UI
		RefreshEchoSlots();
		UpdateEchoInfo(echo);
		UpdateActionButtons();
	}

	// === 資訊顯示 ===

	/// 更新右側 Echo 資訊顯示
	private void UpdateEchoInfo(Echo echo)
	{
		if (EchoName != null) EchoName.Text = echo.Name;
		if (Recipe != null) Recipe.Text = echo.RecipeLabel;
		if (Summary != null) Summary.Text = echo.Summary;

		GD.Print($"[EchoPanel] Updated info for: {echo.Name}");
	}

	/// 清空 Echo 資訊顯示
	private void ClearEchoInfo()
	{
		if (EchoName != null) EchoName.Text = "-";
		if (Recipe != null) Recipe.Text = "-";
		if (Summary != null) Summary.Text = "Select an Echo to view details";
	}

	/// 更新操作按鈕狀態
	private void UpdateActionButtons()
	{
		bool hasSelection = _selectedEcho != null;

		if (BtnPlay != null)
		{
			BtnPlay.Disabled = !hasSelection;
		}

		if (BtnCancel != null)
		{
			BtnCancel.Disabled = false; // Cancel 隨時可按
		}
	}

	// === 操作處理 ===

	/// 處理 Play 按鈕點擊
	private void OnPlayPressed()
	{
		if (_selectedEcho == null)
		{
			ShowReason("No Echo selected");
			return;
		}

		// 檢查階段（可選，Translator 會再檢）
		if (CombatCtrl?.State?.PhaseCtx.Step != PhaseStep.PlayerInput)
		{
			ShowReason("Can only use Echo during player input phase");
			return;
		}

		GD.Print($"[EchoPanel] Playing Echo: {_selectedEcho.Name}");

		int? targetId = DetermineTargetId(_selectedEcho.TargetType);
		
		// 呼叫 Combat 執行
		CombatCtrl?.TryRunEcho(_selectedEcho, targetId);

		// 清空選擇（Echo 使用後會被移除）
		ClearSelection();
		CallDeferred(nameof(RefreshEchoSlots));
	}

	/// 處理 Cancel 按鈕點擊
	private void OnCancelPressed()
	{
		GD.Print("[EchoPanel] Selection cancelled");
		ClearSelection();
	}

	/// 清空選擇狀態
	private void ClearSelection()
	{
		_selectedSlotIndex = -1;
		_selectedEcho = null;

		RefreshEchoSlots();
		ClearEchoInfo();
		UpdateActionButtons();
		ClearReason();
	}

	// === 工具方法 ===

	/// 根據 TargetType 決定目標 ID
	private int? DetermineTargetId(TargetType targetType)
	{
		return targetType switch
		{
			TargetType.None => null,
			TargetType.Self => 0,        // Player ID = 0
			TargetType.Target => 1,      // Enemy ID = 1
			TargetType.All => null,      // 全體攻擊，暫時用 null
			_ => null
		};
	}

	/// 顯示錯誤或狀態訊息
	private void ShowReason(string message)
	{
		if (Reason != null)
		{
			Reason.Text = message;
			Reason.Modulate = new Color(1.0f, 0.3f, 0.3f, 1.0f); // 紅色文字
		}

		GD.Print($"[EchoPanel] Reason: {message}");
	}

	/// 清空狀態訊息
	private void ClearReason()
	{
		if (Reason != null)
		{
			Reason.Text = "";
			Reason.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f); // 原色
		}
	}

	// === 公開介面 ===

	/// 外部呼叫：刷新整個面板（當 EchoStore 變更時）
	public void RefreshPanel()
	{
		// 檢查當前選擇是否仍有效
		if (_selectedSlotIndex >= 0 && _selectedEcho != null)
		{
			if (CombatCtrl?.State?.echoStore != null &&
				CombatCtrl.State.echoStore.TryGet(_selectedSlotIndex, out var currentEcho) &&
				currentEcho?.Id == _selectedEcho.Id)
			{
				// 選擇仍有效，保持選中狀態
				RefreshEchoSlots();
				return;
			}
		}

		// 選擇無效，清空選擇
		ClearSelection();
	}


}
