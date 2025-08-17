using Godot;
using System;
using CombatCore;
using CombatCore.Component;
using CombatCore.InterOp;
using CombatCore.Abstractions;
using CombatCore.Memory;
using CombatCore.Command;

/// <summary>
/// Combat 控制器 - 負責 UI 與戰鬥系統的整合
/// 使用重構後的 PhaseRunner 進行流程控制
/// </summary>
public partial class Combat : Control
{
	[Export] public CombatState CombatState;
	[Export] public PlayerView PlayerView;
	[Export] public RecallPanel RecallPanel;
	[Export] public EnemyView EnemyView;

	public override void _Ready()
	{
		// 驗證必要組件
		if (CombatState == null)
		{
			GD.PrintErr("CombatState is null! Please bind it in the inspector.");
			return;
		}

		GD.Print("Combat system initializing...");

		// 設定 UI 事件監聽
		SetupUIListeners();

		// 綁定角色到 UI
		BindActorsToUI();

		// 使用新的 PhaseRunner API 推進遊戲流程
	
		var result = PhaseRunner.AdvanceUntilInput(CombatState);
		GD.Print($"Initial phase advance result: {result}, Current step: {CombatState.PhaseCtx.Step}");

		// 初始化 UI 顯示
		RefreshAllUI();
	}

	public override void _ExitTree()
	{
		// 清理 UI 事件訂閱
		CleanupUIListeners();
	}

	// === UI 事件處理 ===

	/// <summary>
	/// 處理基本動作按鈕點擊
	/// </summary>
	public void TryRunBasic(ActionType act, int? targetId)
	{
		GD.Print($"[Combat] TryRunBasic: {act}, target: {targetId}");

		// 設定玩家意圖
		var intent = new BasicIntent(act, targetId);
		CombatState.PhaseCtx.SetIntent(intent);

		// 使用新的 PhaseRunner API 推進流程
		var result = PhaseRunner.AdvanceUntilInput(CombatState);
		GD.Print($"Basic action result: {result}, Current step: {CombatState.PhaseCtx.Step}");

		// 刷新 UI
		RefreshAllUI();
	}

	/// <summary>
	/// 處理 Recall 確認
	/// </summary>
	private void OnRecallConfirm(int[] indices)
	{
		GD.Print($"[Combat] OnRecallConfirm: [{string.Join(", ", indices)}]");

		// 檢查是否包含攻擊動作以決定目標
		bool hasAttack = false;
		var memOps = CombatState.Mem.SnapshotOps();

		foreach (int idx in indices)
		{
			if (idx < memOps.Count && memOps[idx] == ActionType.A)
			{
				hasAttack = true;
				break;
			}
		}

		int? targetId = hasAttack ? 1 : null; // 1 = enemy, null = self

		// 設定 Recall 意圖
		var intent = new RecallIntent(indices, targetId);
		CombatState.PhaseCtx.SetIntent(intent);

		// 推進流程
		var result = PhaseRunner.AdvanceUntilInput(CombatState);
		GD.Print($"Recall action result: {result}, Current step: {CombatState.PhaseCtx.Step}");

		// 刷新 UI
		RefreshAllUI();
	}

	// === UI 管理 ===

	private void SetupUIListeners()
	{
		// 狀態變化監聽
		UISignalHub.OnHPChanged += OnStatusChanged;
		UISignalHub.OnChargeChanged += OnStatusChanged;
		UISignalHub.OnShieldChanged += OnStatusChanged;
		UISignalHub.OnAPChanged += OnStatusChanged;

		// Phase 事件監聽
		UISignalHub.OnPlayerDrawComplete += OnPlayerDrawComplete;

		// Recall 事件監聽
		RecallPanel.ConfirmPressed += OnRecallConfirm;
	}

	private void CleanupUIListeners()
	{
		UISignalHub.OnChargeChanged -= OnStatusChanged;
		UISignalHub.OnHPChanged -= OnStatusChanged;
		UISignalHub.OnShieldChanged -= OnStatusChanged;
		UISignalHub.OnAPChanged -= OnStatusChanged;
		UISignalHub.OnPlayerDrawComplete -= OnPlayerDrawComplete;
		
		if (RecallPanel != null)
		{
			RecallPanel.ConfirmPressed -= OnRecallConfirm;
		}
	}

	private void BindActorsToUI()
	{
		if (PlayerView != null && CombatState.Player != null)
		{
			PlayerView.BindActor(CombatState.Player);
			CombatState.Player.DebugName = "Player";
		}

		if (EnemyView != null && CombatState.Enemy != null)
		{
			EnemyView.BindActor(CombatState.Enemy);
			CombatState.Enemy.DebugName = "Enemy";
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
	}

	private void RefreshTimelineSnapshot()
	{
		if (RecallPanel == null) return;

		var ops = CombatState.Mem.SnapshotOps();
		var turns = CombatState.Mem.SnapshotTurns();
		var currentTurn = CombatState.PhaseCtx.TurnNum;
		
		RecallPanel.RefreshSnapshot(ops, turns, currentTurn);
	}

	private void UpdateRecallPanelState()
	{
		if (RecallPanel == null) return;

		var currentStep = CombatState.PhaseCtx.Step;
		
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
			case PhaseStep.EnemyExecInstant:
			case PhaseStep.EnemyExecDelayed:
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
		var memOps = CombatState.Mem.SnapshotOps();
		if (memOps.Count > 0)
		{
			RecallPanel?.EnterPlayerPhase();
		}
	}
}
