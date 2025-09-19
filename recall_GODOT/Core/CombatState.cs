
using System;
using System.Collections.Generic;
using System.Linq;
using CombatCore;
using CombatCore.Recall;

namespace CombatCore
{
	public partial class CombatState
	{
		public PhaseContext PhaseCtx;
		public Actor Player { get; private set; }

		// 添加敵人列表支援
		private List<Actor> _enemies = new();

		// 保持向後兼容
		public Actor Enemy => _enemies.FirstOrDefault() ?? throw new InvalidOperationException("No enemies");

		public MemoryQueue Mem { get; private set; } = new MemoryQueue(5);

		public ActStore actStore { get; private set; } = new ActStore();
		public bool IsActStoreFull => actStore.IsFull;

		// 添加 ID 映射
		private Dictionary<int, Actor> _actorById = new();

		public IReadOnlyList<Actor> GetAllEnemies() => _enemies;


		public CombatState()
		{
			PhaseCtx.Init();

			Player = new Actor(maxHP: 20, apPerTurn: 3, withCharge: false, withCopy: true);
			Player.Id = 0;
			Player.DebugName = "Player";
			_actorById[Player.Id] = Player;

			// 測試：創建兩個敵人
			var enemy1 = new Actor(maxHP: 8, withAP: false, withCharge: false);
			var enemy2 = new Actor(maxHP: 6, withAP: false, withCharge: false);

			enemy1.Id = 1;
			enemy2.Id = 2;
			enemy1.DebugName = "Enemy1";
			enemy2.DebugName = "Enemy2";

			_enemies.Add(enemy1);
			_enemies.Add(enemy2);

			_actorById[enemy1.Id] = enemy1;
			_actorById[enemy2.Id] = enemy2;
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
