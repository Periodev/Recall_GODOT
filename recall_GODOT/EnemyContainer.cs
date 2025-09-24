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
		GD.Print("[EnemyContainer] _Ready() called");

		try
		{
			var gridContainer = GetNode<GridContainer>("GridContainer");
			//GD.Print($"[EnemyContainer] GridContainer found: {gridContainer != null}");

			if (gridContainer == null)
			{
				GD.PrintErr("[EnemyContainer] GridContainer not found!");
				return;
			}

			// 列出 GridContainer 的所有子節點
			var children = gridContainer.GetChildren();
			//GD.Print($"[EnemyContainer] GridContainer has {children.Count} children");
			for (int i = 0; i < children.Count; i++)
			{
				//GD.Print($"[EnemyContainer] Child {i}: {children[i].Name} ({children[i].GetType().Name})");
			}

			for (int i = 0; i < 6; i++)
			{
				try
				{
					var slotNode = gridContainer.GetNode<EnemySlot>($"EnemySlot{i}");
					if (slotNode != null)
					{
						slots[i] = slotNode;
						slots[i].SlotIndex = i;
						GD.Print($"[EnemyContainer] EnemySlot{i} successfully initialized");
					}
					else
					{
						GD.PrintErr($"[EnemyContainer] EnemySlot{i} not found in GridContainer");
					}
				}
				catch (Exception slotEx)
				{
					GD.PrintErr($"[EnemyContainer] Error getting EnemySlot{i}: {slotEx.Message}");
				}
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[EnemyContainer] Error in _Ready: {ex.Message}");
		}

		SignalHub.OnEnemySlotClicked += OnSlotClicked;
		//GD.Print("[EnemyContainer] _Ready() completed");
	}
	
	private void OnSlotClicked(int slotIndex, int? enemyId)
	{
		selectedSlotIndex = slotIndex;
		UpdateSelection();
		SignalHub.NotifyEnemySelected(enemyId);
	}

	public void BindEnemyToSlot(int slotIndex, Actor enemy)
	{
		if (slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex] != null)
		{
			slots[slotIndex].EnemyId = enemy?.Id;
			slots[slotIndex].BindActor(enemy);
		}
		else
		{
			GD.PrintErr($"[EnemyContainer] Cannot bind enemy to slot {slotIndex}: slot is null or invalid");
		}
	}

	private void UpdateSelection()
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i] != null)
			{
				slots[i].SetSelected(i == selectedSlotIndex);
			}
		}
	}

	public void RefreshUI(System.Collections.Generic.IReadOnlyList<Actor> enemies)
	{
		// 重新綁定所有已綁定的敵人以刷新顯示
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i] != null && slots[i].EnemyId.HasValue)
			{
				// 根據 EnemyId 找到對應的敵人並重新綁定
				var enemyId = slots[i].EnemyId.Value;
				var enemy = enemies.FirstOrDefault(e => e.Id == enemyId);
				if (enemy != null)
				{
					slots[i].BindActor(enemy);
				}
			}
		}
	}
}
