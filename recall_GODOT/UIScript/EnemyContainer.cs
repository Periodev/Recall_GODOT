using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using CombatCore;
using CombatCore.UI;

public partial class EnemyContainer : Control
{
	private EnemySlot[] slots = new EnemySlot[6];
	private int? selectedSlotIndex = null;

	public override void _Ready()
	{
		InitializeSlots();
		SignalHub.OnEnemySlotClicked += OnSlotClicked;
	}


	public override void _ExitTree()
	{
		SignalHub.OnEnemySlotClicked -= OnSlotClicked;
	}

	private void OnSlotClicked(int slotIndex, int? enemyId)
	{
		selectedSlotIndex = slotIndex;
		UpdateSelection();
	}

	public void BindEnemyToSlot(int slotIndex, Actor enemy)
	{
		if (!IsValidSlot(slotIndex))
		{
			GD.PrintErr($"[EnemyContainer] Invalid slot: {slotIndex}");
			return;
		}

		slots[slotIndex].EnemyId = enemy?.Id;
		slots[slotIndex].BindActor(enemy);
	}

	public void RefreshSlot(int slotIndex, Actor enemy)
	{
		if (!IsValidSlot(slotIndex)) return;

		// 死亡或不存在 ⇒ 立即清槽，並處理選取回退
		if (enemy == null || !enemy.IsAlive)
		{
			slots[slotIndex].Unbind();              // 清空文字、EnemyId=null、Disabled/Visible 狀態
			return;                               // 結束：不要再往下刷新文字了
		}

		// 活著 ⇒ ID 改變就 Rebind；相同 ID 就只更新數值
		if (slots[slotIndex].EnemyId != enemy.Id)
		{
			BindEnemyToSlot(slotIndex, enemy);   // 內部會設定 Visible=true 等
		}
		else
		{
			slots[slotIndex].BindActor(enemy);    // 同一敵人：刷新 HP/Shield/Intent 文案
		}
	}

	private void UpdateSelection()
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (IsValidSlot(i))
			{
				// 只有活著的敵人且為選中槽位才高亮
				bool isSelected = i == selectedSlotIndex && slots[i].EnemyId.HasValue;
				slots[i].SetSelected(isSelected);
			}
		}
	}

	public void RefreshUI(IReadOnlyList<Actor> enemies)
	{
		bool hadSelection = selectedSlotIndex.HasValue;

		// 遍歷每個槽位，保持原有綁定關係
		for (int i = 0; i < slots.Length; i++)
		{
			if (!IsValidSlot(i)) continue;

			// 檢查該槽位是否已綁定敵人
			if (slots[i].EnemyId.HasValue)
			{
				// 在敵人列表中尋找對應ID的敵人
				var boundEnemy = enemies.FirstOrDefault(e => e.Id == slots[i].EnemyId.Value);
				RefreshSlot(i, boundEnemy); // 如果敵人死亡或不存在，RefreshSlot會自動Unbind
			}
			// 如果槽位沒有綁定敵人，保持空狀態（不做任何操作）
		}

		// 2. 為新敵人分配空槽位
		var unboundEnemies = enemies.Where(e => !slots.Any(s => s?.EnemyId == e.Id)).ToList();
		foreach (var newEnemy in unboundEnemies)
		{
			for (int i = 0; i < slots.Length; i++)
			{
				if (IsValidSlot(i) && !slots[i].EnemyId.HasValue)
				{
					BindEnemyToSlot(i, newEnemy);
					GD.Print($"[EnemyContainer] Auto-bound {newEnemy.DebugName} to slot {i}");
					break;
				}
			}
		}

		// 如果之前有選擇但現在沒有了，立即觸發選擇同步
		if (hadSelection && (!selectedSlotIndex.HasValue || !IsSlotOccupied(selectedSlotIndex.Value)))
		{
			// 立即尋找新目標並同步高亮
			var targetId = GetDefaultTargetId(enemies);
		}
	}

	private bool IsValidSlot(int slotIndex)
	{
		return slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex] != null;
	}

	/// <summary>
	/// 獲取默認目標ID，優先使用選中的敵人，否則自動選擇第一個活著的敵人並同步高亮狀態
	/// </summary>
	/// <returns>目標敵人ID，如果沒有有效目標則返回 null</returns>
	public int? GetDefaultTargetId(System.Collections.Generic.IReadOnlyList<Actor> enemies)
	{
		// 如果有選中的敵人且該槽位仍被占用
		if (selectedSlotIndex.HasValue && IsSlotOccupied(selectedSlotIndex.Value))
		{
			var selectedEnemyId = slots[selectedSlotIndex.Value].EnemyId;
			if (selectedEnemyId.HasValue)
			{
				var selectedEnemy = enemies.FirstOrDefault(e => e.Id == selectedEnemyId.Value);
				if (selectedEnemy?.IsAlive == true)
					return selectedEnemy.Id;
			}
		}

		// 當前選擇無效，遍歷所有槽位尋找第一個活著的敵人
		for (int i = 0; i < slots.Length; i++)
		{
			if (IsSlotOccupied(i))
			{
				var enemyId = slots[i].EnemyId.Value;
				var enemy = enemies.FirstOrDefault(e => e.Id == enemyId);
				if (enemy?.IsAlive == true)
				{
					selectedSlotIndex = i;
					UpdateSelection(); // 同步高亮狀態
					return enemy.Id;
				}
			}
		}

		// 沒有找到任何活著的敵人
		selectedSlotIndex = null;
		UpdateSelection();
		return null;
	}

	/// <summary>
	/// 檢查指定槽位是否被占用（有綁定活著的敵人）
	/// </summary>
	private bool IsSlotOccupied(int slotIndex)
	{
		return IsValidSlot(slotIndex) &&
			slots[slotIndex].EnemyId.HasValue &&
			!slots[slotIndex].Disabled;
	}

	/// <summary>
	/// 為新敵人分配第一個可用的空槽位
	/// </summary>
	/// <param name="enemy">要綁定的敵人</param>
	/// <returns>成功綁定返回true，無可用槽位返回false</returns>
	public bool TryBindEnemyToEmptySlot(Actor enemy)
	{
		if (enemy == null || !enemy.IsAlive) return false;

		// 尋找第一個空槽位
		for (int i = 0; i < slots.Length; i++)
		{
			if (IsValidSlot(i) && !slots[i].EnemyId.HasValue)
			{
				BindEnemyToSlot(i, enemy);
				return true;
			}
		}

		GD.PrintErr("[EnemyContainer] No empty slots available for enemy binding");
		return false;
	}

	private void InitializeSlots()
	{
		var gridContainer = GetNode<GridContainer>("GridContainer");
		if (gridContainer == null)
		{
			GD.PrintErr("[EnemyContainer] GridContainer not found!");
			return;
		}

		for (int i = 0; i < 6; i++)
		{
			var slotNode = gridContainer.GetChild(i) as EnemySlot;
			if (slotNode != null)
			{
				slots[i] = slotNode;
				slots[i].SlotIndex = i;
			}
		}
	}


}
