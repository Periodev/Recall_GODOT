using System;
using System.Collections.Generic;
using CombatCore;

namespace CombatCore.Recall
{
	public sealed class MemoryQueue
	{
		private readonly int _capacity;
		private readonly List<ActionType> _ops = new();
		private readonly List<int> _turns = new();          // 每格寫入時的回合號，用於 Translator 過濾「本回合」

		public MemoryQueue(int capacity)
		{
			if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
			_capacity = capacity;
		}

		public int Capacity => _capacity;
		public int Count => _ops.Count;

		public void Push(ActionType op, int turnNumber)
		{
			if (_ops.Count == _capacity) { _ops.RemoveAt(0); _turns.RemoveAt(0); } // 滿了丟最舊
			_ops.Add(op);
			_turns.Add(turnNumber);
		}

		public void Clear()
		{
			_ops.Clear();
			_turns.Clear();
		}

		// —— 唯讀快照 —— //
		public IReadOnlyList<ActionType> SnapshotOps()   => _ops;
		public IReadOnlyList<int>        SnapshotTurns() => _turns;
	}

}
