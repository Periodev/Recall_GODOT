using System;
using System.Collections.Generic;
using CombatCore;

namespace CombatCore.Memory
{
	public sealed class MemoryQueue : IMemoryQueue
	{
		private readonly ActionType[] _buf;
		private int _head;   // 指向最舊元素位置
		private int _count;  // 目前元素數

		public int Capacity { get; }
		public int Count => _count;

		public MemoryQueue(int capacity)
		{
			if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
			Capacity = capacity;
			_buf = new ActionType[capacity];
			_head = 0;
			_count = 0;
		}

		public void PushBasic(ActionType a)
		{
			if (_count < Capacity)
			{
				int tail = (_head + _count) % Capacity;
				_buf[tail] = a;
				_count++;
			}
			else
			{
				// 滿則淘汰最舊：覆寫 _head 並前移 _head
				_buf[_head] = a;
				_head = (_head + 1) % Capacity;
			}
		}

		public ActionType Peek(int idxFromOldest)
		{
			if (idxFromOldest < 0 || idxFromOldest >= _count)
				throw new ArgumentOutOfRangeException(nameof(idxFromOldest));
			int idx = (_head + idxFromOldest) % Capacity;
			return _buf[idx];
		}

		public IReadOnlyList<ActionType> Snapshot()
		{
			var list = new List<ActionType>(_count);
			for (int i = 0; i < _count; i++)
			{
				int idx = (_head + i) % Capacity;
				list.Add(_buf[idx]);
			}
			return list.AsReadOnly();
		}
	}
}
