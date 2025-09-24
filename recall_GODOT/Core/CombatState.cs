
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
		// 每場戰鬥私有的配號器（0 保留給 Player）
		private int _nextActorId = 1;


		public IReadOnlyList<Actor> GetAllEnemies() => _enemies;
		public IReadOnlyList<Actor> GetAliveEnemies() => _enemies.Where(e => e.IsAlive).ToList();
		
		/// <summary>依 Id 移除敵人；成功回傳 true。</summary>
		public bool RemoveEnemyById(int id)
		{
			var idx = _enemies.FindIndex(e => e.Id == id);
			if (idx < 0) return false;
			_enemies.RemoveAt(idx);
			_actorById.Remove(id);
			return true;
		}

		/// <summary>剔除死亡敵人並回傳被移除的 Id 清單（純資料操作，不發事件）。</summary>
		public List<int> PurgeDeadEnemies()
		{
			var deadIds = _enemies.Where(e => !e.IsAlive).Select(e => e.Id).ToList();
			if (deadIds.Count == 0) return deadIds;
			_enemies.RemoveAll(e => deadIds.Contains(e.Id));
			foreach (var id in deadIds) _actorById.Remove(id);
			return deadIds;
		}

		/// <summary>配發新 ActorId（本場戰鬥內唯一且不重用）。</summary>
		private int AllocateActorId()
		{
			var id = _nextActorId;
			_nextActorId += 1;
			return id;
		}

		/// <summary>加入敵人（初始/增援皆走此路徑），回傳配發的 Id。</summary>
		public int AddEnemy(Actor enemy)
		{
			if (enemy == null) throw new ArgumentNullException(nameof(enemy));
			// 若這個實例已經在名冊中，直接回傳既有 Id（避免重複加入）
			if (_actorById.TryGetValue(enemy.Id, out var existing) && ReferenceEquals(existing, enemy))
				return enemy.Id;

			var id = AllocateActorId();
			enemy.Id = id;
			_enemies.Add(enemy);
			_actorById[id] = enemy;
			return id;
		}


		public CombatState()
		{
			PhaseCtx.Init();

			Player = new Actor(maxHP: 20, apPerTurn: 3, withCharge: false, withCopy: true);
			Player.Id = 0;
			Player.DebugName = "Player";
			_actorById[Player.Id] = Player;

			// 測試：創建兩個敵人（改用 AddEnemy 分配 Id）
			var enemy1 = new Actor(maxHP: 8, withAP: false, withCharge: false) { DebugName = "Enemy1" };
			var enemy2 = new Actor(maxHP: 3, withAP: false, withCharge: false) { DebugName = "Enemy2" };
			AddEnemy(enemy1);
			AddEnemy(enemy2);
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
