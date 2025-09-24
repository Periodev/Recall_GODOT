using Godot;
using System;
using System.Linq;
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

		// 只有當敵人ID改變時才重新綁定
		if (slots[slotIndex].EnemyId != enemy?.Id)
		{
			BindEnemyToSlot(slotIndex, enemy);
		}
		else if (enemy != null)
		{
			// 同一個敵人，只更新顯示數據
			slots[slotIndex].BindActor(enemy);
		}
	}

	private void UpdateSelection()
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (IsValidSlot(i))
			{
				slots[i].SetSelected(i == selectedSlotIndex);
			}
		}
	}

	public void RefreshUI(System.Collections.Generic.IReadOnlyList<Actor> enemies)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			var enemy = enemies.ElementAtOrDefault(i);
			RefreshSlot(i, enemy);
		}
	}

	private bool IsValidSlot(int slotIndex)
	{
		return slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex] != null;
	}

	/// <summary>
	/// 獲取默認目標ID，優先使用選中的敵人，否則使用第一個活著的敵人
	/// </summary>
	public int GetDefaultTargetId(System.Collections.Generic.IReadOnlyList<Actor> enemies)
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

		// 否則返回第一個活著的敵人
		return enemies.FirstOrDefault(e => e.IsAlive)?.Id ?? 1;
	}
}
