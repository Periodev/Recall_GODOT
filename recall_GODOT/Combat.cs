using Godot;
using System;
using System.Collections.Generic;

using CombatCore;
using CombatCore.Kernel;
using CombatCore.ActorOp;
using CombatCore.InterOp;
using CombatCore.Recall;
using CombatCore.Command;
using CombatCore.UI;
using System.Linq;

/// <summary>
/// Combat 控制器 - 負責 UI 與戰鬥系統的整合
/// 使用重構後的 PhaseRunner 進行流程控制
/// </summary>
public partial class Combat : Control
{
	[Export] public CombatStateNode CombatNode;
	[Export] public PlayerView PlayerView;
	[Export] public EnemyView EnemyView;
	[Export] public RecallPanel RecallPanel;
	[Export] public EchoPanel EchoPanel;
	[Export] public ErrorLabel ErrorLabel;


	public CombatState State => CombatNode!.State;

	public override void _Ready()
	{
		// 驗證必要組件
		if (CombatNode is null)
		{
			GD.PushError("CombatNode not bound in Inspector.");
			SetProcess(false);
			return;
		}

		GD.Print("Combat system initializing...");

		CreateDebugActs();


		// 設定 UI 事件監聽
		SetupUIListeners();

		// 綁定角色到 UI
		BindActorsToUI();

		// 使用新的 PhaseRunner API 推進遊戲流程

		var result = PhaseRunner.AdvanceUntilInput(State);

		// 初始化 UI 顯示
		RefreshAllUI();
	}

	public override void _ExitTree()
	{
		// 清理 UI 事件訂閱
		CleanupUIListeners();
	}

	// === UI 事件處理 ===


	public void TryEndTurn()
	{
		GD.Print("[CombatUI] TryEndTurn");

		// 🎯 直接調用 PhaseRunner 的保護接口
		var result = PhaseRunner.TryEndPlayerTurn(State);

		GD.Print($"End turn result: {result}, Current step: {State.PhaseCtx.Step}");

		RefreshAllUI();

	}

	public void TryRunAct(Act act, int? targetId)
	{
		GD.Print($"[Combat] TryRunAct: {act.Name}, target: {targetId}");

		// 找到選中的槽位索引
		var slots = State.actStore.ToSlots();
		int slotIndex = -1;
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i]?.Id == act.Id)
			{
				slotIndex = i;
				break;
			}
		}

		if (slotIndex == -1)
		{
			GD.Print("[Combat] Actnot found in store");
			return;
		}

		var intent = new ActIntent(act, targetId);
		var result = PhaseRunner.TryExecutePlayerAction(State, intent);

		GD.Print($"[Combat] Actresult: {result}");
		RefreshAllUI();
	}


	/// <summary>
	/// 處理 Recall 確認
	/// </summary>
	public void OnRecallConfirm(int recipeId)
	{
		GD.Print($"[Combat] OnRecallConfirm: recipeId={recipeId}");

		// ✅ RecipeId 已通過 UI 層驗證，直接提交到 Translator
		var intent = new RecallIntent(recipeId);
		var phaseResult = PhaseRunner.TryExecutePlayerAction(State, intent);

		GD.Print($"[Combat] Recall result: {phaseResult}, Step: {State.PhaseCtx.Step}");
		RefreshAllUI();
	}

	// === UI 管理 ===

	private void SetupUIListeners()
	{
		// 狀態變化監聽
		SignalHub.OnHPChanged += OnStatusChanged;
		SignalHub.OnChargeChanged += OnStatusChanged;
		SignalHub.OnShieldChanged += OnStatusChanged;
		SignalHub.OnAPChanged += OnStatusChanged;

		// Phase 事件監聽
		SignalHub.OnPlayerDrawComplete += OnPlayerDrawComplete;

		// Error message
		SignalHub.OnErrorOccurred += ShowError;

		SignalHub.OnEnemyIntentUpdated += UpdateEnemyIntent;
		SignalHub.OnEnemyIntentCleared += ClearEnemyIntent;
	}

	private void CleanupUIListeners()
	{
		SignalHub.OnChargeChanged -= OnStatusChanged;
		SignalHub.OnHPChanged -= OnStatusChanged;
		SignalHub.OnShieldChanged -= OnStatusChanged;
		SignalHub.OnAPChanged -= OnStatusChanged;
		SignalHub.OnPlayerDrawComplete -= OnPlayerDrawComplete;
		SignalHub.OnErrorOccurred -= ShowError;
		SignalHub.OnEnemyIntentUpdated -= UpdateEnemyIntent;
		SignalHub.OnEnemyIntentCleared -= ClearEnemyIntent;
	}

	private void BindActorsToUI()
	{
		if (PlayerView != null && State.Player != null)
		{
			PlayerView.BindActor(State.Player);
			State.Player.DebugName = "Player";
		}

		if (EnemyView != null && State.Enemy != null)
		{
			EnemyView.BindActor(State.Enemy);
			State.Enemy.DebugName = "Enemy";
		}
	}

	private void RefreshAllUI()
	{
		// 刷新角色狀態顯示
		PlayerView?.UpdateVisual();
		EnemyView?.UpdateVisual();

		// 刷新記憶時間線
		RefreshTimelineSnapshot();

		// 根據當前 Phase 更新 RecallPanel 狀態
		UpdateRecallPanelState();

		EchoPanel.RefreshPanel();

	}

	private void RefreshTimelineSnapshot()
	{
		if (RecallPanel == null) return;

		var ops = State.Mem.SnapshotOps();
		var turns = State.Mem.SnapshotTurns();
		var currentTurn = State.PhaseCtx.TurnNum;

		RecallPanel.RefreshSnapshot(ops, turns, currentTurn);
	}

	private void UpdateRecallPanelState()
	{
		if (RecallPanel == null) return;

		var currentStep = State.PhaseCtx.Step;

		// 根據當前 Phase 設定 RecallPanel 狀態
		switch (currentStep)
		{
			case PhaseStep.PlayerInput:
			case PhaseStep.PlayerPlanning:
			case PhaseStep.PlayerExecute:
				RecallPanel.EnterPlayerPhase();
				break;

			case PhaseStep.EnemyInit:
			case PhaseStep.EnemyIntent:
			case PhaseStep.EnemyPlanning:
			case PhaseStep.EnemyExecMark:
			case PhaseStep.EnemyExec:
				RecallPanel.EnterEnemyPhase();
				break;

			default:
				// 其他階段保持當前狀態
				break;
		}
	}

	// === 事件回調 ===

	private void OnStatusChanged(int newValue)
	{
		// 延遲到下一幀刷新，避免同一幀內多次更新
		CallDeferred(nameof(RefreshAllUI));
	}

	private void OnPlayerDrawComplete()
	{
		GD.Print("[Combat] Player draw complete");

		// 檢查是否有記憶可以使用 Recall
		var memOps = State.Mem.SnapshotOps();
		if (memOps.Count > 0)
		{
			RecallPanel?.EnterPlayerPhase();
		}
	}

	private void ShowError(FailCode code)
	{
		ErrorLabel.ShowError(code);
	}

	private void UpdateEnemyIntent(int ActorID, IReadOnlyList<EnemyIntentUIItem> items)
	{
		EnemyView.UpdateIntent(items[0].Icon, items[0].Text);
	}

	private void ClearEnemyIntent(int ActorID)
	{
		EnemyView.ClearIntent();
	}


	// debug function
	private void CreateDebugActs()
	{
		var basicActs = new[]
		{
			ActFactory.BuildFromRecipe(1),  // Attack
			ActFactory.BuildFromRecipe(2),  // Block  
			ActFactory.BuildFromRecipe(3),  // Copy
		};


		foreach (var act in basicActs)
			State.actStore.TryAdd(act);


		GD.Print("[Combat] - Basic Action Slots added");
	}

}
