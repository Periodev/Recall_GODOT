using Godot;
using System;
using CombatCore;
using CombatCore.Kernel;
using CombatCore.ActorOp;
using CombatCore.InterOp;
using CombatCore.Recall;
using CombatCore.Command;
using CombatCore.UI;

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

		CreateDebugEchos();


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

	/// <summary>
	/// 處理基本動作按鈕點擊
	/// </summary>
	public void TryRunBasic(ActionType act, int? targetId)
	{
		GD.Print($"[CombatUI] TryRunBasic: {act}, target: {targetId}");

		// 設定玩家意圖
		var intent = new BasicIntent(act, targetId);
		var result = PhaseRunner.TryExecutePlayerAction(State, intent);

		GD.Print($"[CombatUI] Basic action result: {result}, Current step: {State.PhaseCtx.Step}");

		// 刷新 UI
		RefreshAllUI();
	}

	public void TryEndTurn()
	{
		GD.Print("[CombatUI] TryEndTurn");

		// 🎯 直接調用 PhaseRunner 的保護接口
		var result = PhaseRunner.TryEndPlayerTurn(State);

		GD.Print($"End turn result: {result}, Current step: {State.PhaseCtx.Step}");

		RefreshAllUI();

	}

	public void TryRunEcho(Echo echo, int? targetId)
	{
		GD.Print($"[Combat] TryRunEcho: {echo.Name}, target: {targetId}");

		// 找到選中的槽位索引
		var slots = State.echoStore.ToSlots();
		int slotIndex = -1;
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i]?.Id == echo.Id)
			{
				slotIndex = i;
				break;
			}
		}

		if (slotIndex == -1)
		{
			GD.Print("[Combat] Echo not found in store");
			return;
		}

		var intent = new EchoIntent(echo, targetId, slotIndex);
		var result = PhaseRunner.TryExecutePlayerAction(State, intent);

		GD.Print($"[Combat] Echo result: {result}");
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

	}

	private void CleanupUIListeners()
	{
		SignalHub.OnChargeChanged -= OnStatusChanged;
		SignalHub.OnHPChanged -= OnStatusChanged;
		SignalHub.OnShieldChanged -= OnStatusChanged;
		SignalHub.OnAPChanged -= OnStatusChanged;
		SignalHub.OnPlayerDrawComplete -= OnPlayerDrawComplete;
		SignalHub.OnErrorOccurred -= ShowError;
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


	// debug function
	private void CreateDebugEchos()
	{
		// Echo 1: 攻擊類 (對敵人)
		var attackEcho = new Echo
		{
			Id = 1,
			RecipeId = 1,
			Name = "Attack",
			RecipeLabel = "A",
			Summary = "[Test] Basic attack",
			CostAP = 1,
			Op = HLAop.Attack,
			TargetType = TargetType.Target
		};

		// Echo 2: 防禦類 (對自己)
		var shieldEcho = new Echo
		{
			Id = 2,
			RecipeId = 102,
			Name = "Block",
			RecipeLabel = "B",
			Summary = "[Test] Basic Block",
			CostAP = 1,
			Op = HLAop.Block,
			TargetType = TargetType.Self
		};

		// 加入到 EchoStore
		var result1 = State.echoStore.TryAdd(attackEcho);
		var result2 = State.echoStore.TryAdd(shieldEcho);

		GD.Print($"[Combat] Created debug Echos: {State.echoStore.Count}/5");
		GD.Print($"[Combat] - {attackEcho.Name} ({attackEcho.TargetType}) - {result1}");
		GD.Print($"[Combat] - {shieldEcho.Name} ({shieldEcho.TargetType}) - {result2}");
	}

}
