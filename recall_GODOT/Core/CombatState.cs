
using System;
using CombatCore;
using CombatCore.Recall;

namespace CombatCore
{
	public partial class CombatState
	{
		public PhaseContext PhaseCtx;
		public Actor Player { get; private set; }
		public Actor Enemy { get; private set; }  // default enemy
		public MemoryQueue Mem { get; private set; } = new MemoryQueue(5);

		public EchoStore echoStore { get; private set; } = new EchoStore();
		public bool IsEchoStoreFull => echoStore.IsFull;


		public CombatState()
		{
			PhaseCtx.Init();

			// 使用不同的 Component 組合來定義 Player 和 Enemy
			// Player: 擁有 HP, Shield, AP, 和 Charge 元件
			Player = new Actor(maxHP: 20, apPerTurn: 3, withCharge: false, withCopy: true);
			// Enemy: 只有基礎的 HP 和 Shield 元件
			Enemy = new Actor(maxHP: 10, withAP: false, withCharge: false);
		}


		public RecallView GetRecallView() =>
			new RecallView(Mem.SnapshotOps(), Mem.SnapshotTurns());

		// 供 Translator 綁定的委派實作
		public bool TryGetActor(int id, out Actor actor)
		{
			switch (id)
			{
				case 0: actor = Player; return true;
				case 1: actor = Enemy; return true;
				default: actor = default!; return false;
			}
		}
	}
}
