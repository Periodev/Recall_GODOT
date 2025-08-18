
using System;
using CombatCore;
using CombatCore.Component;
using CombatCore.Memory;

public partial class CombatState
{
	public PhaseContext PhaseCtx;
	public Actor Player { get; private set; }
	public Actor Enemy { get; private set; }  // default enemy
	public MemoryQueue Mem { get; private set; } = new MemoryQueue(5);


	public CombatState()
	{
		PhaseCtx.Init();
		Player = new Actor(100);
		Enemy = new Actor(80);
	}


	public RecallView GetRecallView() =>
		new RecallView(Mem.SnapshotOps(), Mem.SnapshotTurns());

	// 供 Translator 綁定的委派實作
	public bool TryGetActor(int id, out Actor actor)
	{
		switch (id)
		{
			case 0: actor = Player; return true;
			case 1: actor = Enemy;  return true;
			default: actor = default!; return false;
		}
	}
}
