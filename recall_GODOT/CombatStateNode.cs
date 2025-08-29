using Godot;
using System;
using CombatCore; // 放 CombatState 的命名空間

public partial class CombatStateNode : Node
{
	public CombatState State { get; private set; } = new CombatState();

	public override void _Ready()
	{
		// 若需要，這裡訂閱 UISignalHub 以做場景轉發或初始化場景綁定
		// UISignalHub.OnHPChanged += (id,hp) => { /* 可選：集中處理 */ };
	}

	public void ResetState(CombatState? newState = null)
	{
		State = newState ?? new CombatState();
		// 可選：廣播全量刷新訊號
		// UISignalHub.NotifyFullRefresh(State.Snapshot());
	}

}
