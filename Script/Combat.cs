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

			UISignalHub.OnChargeChanged += OnChargeChanged;

		}

		PlayerView.BindActor(CombatState.Player);


		CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx);

		GD.Print($"Step={CombatState.PhaseCtx.Step}"); // 應為 PlayerInput

		// debug test timeline
		//CombatState.Mem.Push(ActionType.A, 0);
		//CombatState.Mem.Push(ActionType.B, 1);


		RecallPanel.RecallPressed += OnRecallStart;
		RecallPanel.SelectionChanged += OnRecallSelectionChanged;
		RecallPanel.ConfirmPressed += OnRecallConfirm;
		RecallPanel.CancelPressed += OnRecallCancel;

		RefreshTimelineSnapshot();
	}

	public override void _ExitTree()
	{
		// 記得取消訂閱避免記憶體洩漏
		UISignalHub.OnChargeChanged -= OnChargeChanged;
	}

	private void OnChargeChanged(int newCharge)
	{
		GD.Print($"Combat received charge changed: {newCharge}");
		PlayerView.UpdateVisual(); // 通知 View 更新
	}

	public void TryRunBasic(ActionType act, int? targetId)
	{
		GD.Print($"[UI] Basic Intent {act} pressed");

		var phase = CombatState.PhaseCtx;
		var self = CombatState.Player;
		var memV = CombatState.GetRecallView();      // Basic 不用，但簽名需要
		var actors = CombatState;                      // IActorLookup

		BasicPlan basic;
		string fail;

		bool ok = _translator.TryTranslate(
			new BasicIntent(act, targetId),
			phase, memV, actors, self,
			out basic, out _, out fail);   // <- 丟掉 RecallPlan

		if (!ok) { GD.PrintErr($"[HLA] {fail}"); return; }
		else { GD.Print($"[HLA] success"); }

		//_exec.ExecuteAll(cmds);
		CombatState.Mem.Push(act, phase.TurnNum);
		RefreshTimelineSnapshot();


		// process phase step
		CombatState.PhaseCtx.Step = PhaseStep.PlayerExecute;
		CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx); // 跑到下一次 WaitInput

	}

	public void EndTurn()
	{
		GD.Print($"[UI] End Turn Intent pressed");
		CombatState.PhaseCtx.Step = PhaseStep.TurnEnd;
		CombatKernel.AdvanceUntilInput(ref CombatState.PhaseCtx);

	}


	// Recall events
	void OnRecallStart()
	{ 
		GD.Print($"[UI] Recall Intent pressed");

	}

	private void RefreshTimelineSnapshot()
	{
		var ops = CombatState.Mem.SnapshotOps();
		var turns = CombatState.Mem.SnapshotTurns();
		var cur = CombatState.PhaseCtx.TurnNum;
		RecallPanel.RefreshSnapshot(ops, turns, cur);
	}


	void OnRecallSelectionChanged(int[] indices)
	{ /* 記錄索引 + 決定是否可 Confirm */
		GD.Print($"[UI] Recall Selection Changed");

	}

	void OnRecallConfirm()
	{ /* TryRunRecall + 關閉選取 */
		GD.Print($"[UI] Recall Confirm");

	}

	void OnRecallCancel()
	{ /* 關閉選取 + 清狀態 */
		GD.Print($"[UI] Recall Cancel");

	}



}
