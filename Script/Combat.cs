using Godot;
using System;
using CombatCore;
using CombatCore.Component;

public partial class Combat : Control
{
	[Export] public CombatState CombatState;    // manually bind model.cs in inspector
	[Export] public PlayerView PlayerView;      // manually bind view.tscn instance in inspector

	public override void _Ready()
	{

		GD.Print("Combat is ready");

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

		ActorOp.Add<Charge>(CombatState.Player, 1); // 初始充能
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
}
