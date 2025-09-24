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
		try
		{
			var gridContainer = GetNode<GridContainer>("GridContainer");
			if (gridContainer == null)
			{
				GD.PrintErr("[EnemyContainer] GridContainer not found!");
				return;
			}

			for (int i = 0; i < 6; i++)
			{
				var slotNode = gridContainer.GetNode<EnemySlot>($"EnemySlot{i}");
				if (slotNode != null)
				{
					slots[i] = slotNode;
					slots[i].SlotIndex = i;
				}
				else
				{
					GD.PrintErr($"[EnemyContainer] EnemySlot{i} not found");
				}
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[EnemyContainer] Initialization failed: {ex.Message}");
		}

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
			slots[slotIndex].Unbind();          	// 清空文字、EnemyId=null、Disabled/Visible 狀態
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

	public void RefreshUI(System.Collections.Generic.IReadOnlyList<Actor> enemies)
	{
		bool hadSelection = selectedSlotIndex.HasValue;

		// 為每個槽位找到對應的敵人（根據 EnemyId 匹配）
		for (int i = 0; i < slots.Length; i++)
		{
			var enemy = enemies.ElementAtOrDefault(i);
			RefreshSlot(i, enemy);
		}

		// 如果之前有選擇但現在沒有了，立即觸發選擇同步
		if (hadSelection && !selectedSlotIndex.HasValue)
		{
			// 立即尋找新目標並同步高亮
			var targetId = GetDefaultTargetId(enemies);
			// GetDefaultTargetId 會自動處理選擇和高亮同步
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
		// 如果有選中的敵人且還活著
		if (selectedSlotIndex.HasValue && IsValidSlot(selectedSlotIndex.Value))
		{
			var selectedEnemyId = slots[selectedSlotIndex.Value].EnemyId;
			if (selectedEnemyId.HasValue)
			{
				var selectedEnemy = enemies.FirstOrDefault(e => e.Id == selectedEnemyId.Value);
				if (selectedEnemy?.IsAlive == true)
					return selectedEnemy.Id;
			}
		}

		// 當前選擇無效，尋找第一個活著的敵人並自動選擇它
		var firstAliveEnemy = enemies.FirstOrDefault(e => e.IsAlive);
		if (firstAliveEnemy != null)
		{
			// 找到對應的槽位索引並自動選擇
			for (int i = 0; i < slots.Length; i++)
			{
				if (IsValidSlot(i) && slots[i].EnemyId == firstAliveEnemy.Id)
				{
					selectedSlotIndex = i;
					UpdateSelection(); // 同步高亮狀態
					return firstAliveEnemy.Id;
				}
			}
		}

		// 沒有找到任何活著的敵人
		selectedSlotIndex = null;
		UpdateSelection();
		return null;
	}
}
