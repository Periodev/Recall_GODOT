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

	// Intent to command pipeline
	private readonly HLATranslator _translator = new();
	private readonly InterOps _interops = new();
	private readonly CmdExecutor _exec = new();

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


		CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx);

		GD.Print($"Step={CombatState.PhaseCtx.Step}"); // 應為 PlayerInput

		// debug test timeline
		//CombatState.Mem.Push(ActionType.A, 0);
		//CombatState.Mem.Push(ActionType.B, 1);

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



	// Move options
	public void TryRunBasic(ActionType act, int? targetId)
	{
		GD.Print($"[UI] Basic Intent {act} pressed");

		var phase = CombatState.PhaseCtx;
		var self = CombatState.Player;
		var memV = CombatState.GetRecallView();      // Basic 不用，但簽名需要
		var actors = CombatState;                      // IActorLookup

		BasicPlan basic;

		FailCode fail = _translator.TryTranslate(
			new BasicIntent(act, targetId),
			phase, memV, actors, self,
			out basic, out _);   // <- 丟掉 RecallPlan

		if (fail != FailCode.None) { GD.PrintErr($"[HLA] {fail}"); return; }
		else { GD.Print($"[HLA] success"); }


		try
		{
			var cmds = _interops.BuildBasic(basic);
			//_exec.ExecuteAll(cmds);
			_exec.ExecuteOrDiscard(cmds);

			CombatState.Mem.Push(act, phase.TurnNum);

			// 刷新
			PlayerView?.UpdateVisual();
			RecallPanel?.RefreshSnapshot(
				CombatState.Mem.SnapshotOps(),
				CombatState.Mem.SnapshotTurns(),
				CombatState.PhaseCtx.TurnNum);
			// process phase step
			CombatState.PhaseCtx.Step = PhaseStep.PlayerExecute;
			CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx); // 跑到下一次 WaitInput
		}
		catch (Exception e)
		{
			GD.PrintErr(e.Message); // 例如 no ap
		}	

	}

	public void EndTurn()
	{
		GD.Print($"[UI] End Turn Intent pressed");
		CombatState.PhaseCtx.Step = PhaseStep.TurnEnd;
		CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx);
	}

	public void TryRunRecall(int[] indices, int? targetId)
	{
		if (CombatState.PhaseCtx.Step != PhaseStep.PlayerInput) { GD.Print("[Recall] ignore: not PlayerInput"); return; }
		if (indices == null || indices.Length == 0) { GD.Print("[Recall] empty selection"); return; }

		var phase  = CombatState.PhaseCtx;
		var self   = CombatState.Player;
		var view   = CombatState.GetRecallView();   // Mem.SnapshotOps/Turns 封裝
		var actors = CombatState;                   // IActorLookup

		GD.Print($"[Recall] sel=[{string.Join(",", indices)}], tgt={(targetId?.ToString() ?? "null")}");

		FailCode fail = _translator.TryTranslate(
				new RecallIntent(indices, targetId),
				phase, view, actors, self,
				out _, out var plan);

		if (fail != FailCode.None) { GD.PrintErr($"[HLA] {fail}"); return; }


		try
		{
			var cmds = _interops.BuildRecall(plan);
			//_exec.ExecuteAll(cmds);
			_exec.ExecuteOrDiscard(cmds);

			CombatState.PhaseCtx.MarkRecallUsed();
			CombatState.PhaseCtx.Step = PhaseStep.PlayerExecute;
			CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx);

			// 刷新
			PlayerView?.UpdateVisual();
			RecallPanel?.RefreshSnapshot(
				CombatState.Mem.SnapshotOps(),
				CombatState.Mem.SnapshotTurns(),
				CombatState.PhaseCtx.TurnNum);
		}
		catch (Exception e)
		{
			GD.PrintErr(e.Message); // 例如 no ap
		}
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

		TryRunRecall(indices, targetId);
	}


}
