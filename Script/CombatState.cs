using Godot;
using System;
using CombatCore;
using CombatCore.Component;
using CombatCore.Abstractions;
using CombatCore.Memory;


public partial class CombatState : Node, IActorLookup
{
	public PhaseContext PhaseCtx;
	public Actor Player { get; private set; }
	public Actor Enemy { get; private set; }  // default enemy
	public MemoryQueue Mem { get; private set; } = new MemoryQueue(5);


	public CombatState()
	{
		// ✅ 這裡執行在 new 時，無法 GetNode 或操作場景
		PhaseCtx.Init();
		Player = new Actor(100);
		Enemy = new Actor(80);
	}

	public override void _Ready()
	{
		GD.Print("Combat state is ready");
	}

    public RecallView GetRecallView() =>
        new RecallView(Mem.SnapshotOps(), Mem.SnapshotTurns());


	public Actor GetById(int actorId)
	{
		return actorId switch
		{
			0 => Player,
			1 => Enemy,
			_ => null  // 無效 ID
		};
	}
}
