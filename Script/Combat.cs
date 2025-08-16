using Godot;
using System;
using CombatCore;
using CombatCore.Component;
using CombatCore.InterOp;
using CombatCore.Abstractions;
using CombatCore.Memory;
using CombatCore.Command;


public partial class Combat : Control
{
	[Export] public CombatState CombatState;    // manually bind model.cs in inspector
	[Export] public PlayerView PlayerView;      // manually bind view.tscn instance in inspector
	[Export] public RecallPanel RecallPanel;
	[Export] public EnemyView EnemyView;

	public CombatEngine _engine = new();

	public override void _Ready()
	{

		// Initialize combat state
		if (CombatState == null)
		{
			//AddChild(CombatState);
			GD.Print("need to create CombatState");
		}
		if (CombatState != null)
		{
			GD.Print("read CombatState success");
			UISignalHub.OnHPChanged += OnStatusChanged;
			UISignalHub.OnChargeChanged += OnStatusChanged;
			UISignalHub.OnShieldChanged += OnStatusChanged;
			UISignalHub.OnAPChanged += OnStatusChanged;

		}

		PlayerView.BindActor(CombatState.Player);
		EnemyView.BindActor(CombatState.Enemy);
		CombatState.Player.DebugName = "Player";
		CombatState.Enemy.DebugName = "Enemy";


		PhaseRunner.AdvanceUntilInput(ref CombatState.PhaseCtx);

		GD.Print($"Step={CombatState.PhaseCtx.Step}"); // 應為 PlayerInput

		UISignalHub.OnPlayerDrawComplete += OnPlayerDrawComplete;

		RecallPanel.ConfirmPressed += OnRecallConfirm;

		RefreshTimelineSnapshot();
	}

	public override void _ExitTree()
	{
		// 記得取消訂閱避免記憶體洩漏
		UISignalHub.OnChargeChanged -= OnStatusChanged;
		UISignalHub.OnHPChanged -= OnStatusChanged;
		UISignalHub.OnShieldChanged -= OnStatusChanged;
		UISignalHub.OnAPChanged -= OnStatusChanged;
		UISignalHub.OnPlayerDrawComplete -= OnPlayerDrawComplete;
	}

	private void OnStatusChanged(int newSts)
	{
		PlayerView.UpdateVisual(); // 通知 View 更新
		EnemyView.UpdateVisual();

		GD.Print("[UI] OnStatusChanged");
	}

	private void RefreshTimelineSnapshot()
	{
		var ops = CombatState.Mem.SnapshotOps();
		var turns = CombatState.Mem.SnapshotTurns();
		var cur = CombatState.PhaseCtx.TurnNum;
		RecallPanel.RefreshSnapshot(ops, turns, cur);
	}

	private void OnPlayerDrawComplete()
	{
		var memOps = CombatState.Mem.SnapshotOps();
		if (memOps.Count > 0)
		{
			RecallPanel.EnterPlayerPhase();
		}
	}

	private void OnRecallConfirm(int[] indices)
	{
		// 檢查是否包含攻擊動作
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

		int? targetId = hasAttack ? 1 : null;  // 1 = enemy

		//TryRunRecall(indices, targetId);
	}


}
