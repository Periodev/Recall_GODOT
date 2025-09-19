
using System;
using System.Collections.Generic;
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

		public ActStore actStore { get; private set; } = new ActStore();
		public bool IsActStoreFull => actStore.IsFull;

		// 添加 ID 映射
		private Dictionary<int, Actor> _actorById = new();


		public CombatState()
		{
			PhaseCtx.Init();

			// 使用不同的 Component 組合來定義 Player 和 Enemy
			// Player: 擁有 HP, Shield, AP, 和 Charge 元件
			Player = new Actor(maxHP: 20, apPerTurn: 3, withCharge: false, withCopy: true);
			// Enemy: 只有基礎的 HP 和 Shield 元件
			Enemy = new Actor(maxHP: 10, withAP: false, withCharge: false);

			// 指派 ID 並建立映射
			Player.Id = 0;
			Enemy.Id = 1;
			_actorById[Player.Id] = Player;
			_actorById[Enemy.Id] = Enemy;
		}


		public RecallView GetRecallView() =>
			new RecallView(Mem.SnapshotOps(), Mem.SnapshotTurns());

		// 添加查找方法
		public bool TryGetActorById(int id, out Actor actor)
		{
			return _actorById.TryGetValue(id, out actor);
		}

		// 更新現有的 TryGetActor 方法使用新映射
		public bool TryGetActor(int id, out Actor actor)
		{
			return TryGetActorById(id, out actor);
		}
	}
}
