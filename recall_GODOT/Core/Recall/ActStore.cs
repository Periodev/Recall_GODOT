using System;
using System.Linq;
using CombatCore;
using CombatCore.InterOp;

namespace CombatCore.Recall
{
	public partial class ActStore
	{
		public const int Capacity = 5;
		private readonly List<Act> _acts = new(Capacity);
		private int _nextId = 1; // Session-wide unique ID counter

		public int Count => _acts.Count;
		public bool IsFull => _acts.Count >= Capacity;

		public IReadOnlyList<Act> Items => _acts; // 不特別保護；需要時再改 AsReadOnly()

		public FailCode TryAdd(Act act)
		{
			if (IsFull) return FailCode.ActSlotsFull; // 滿了不 pop，直接 fail

			// Assign unique incremental ID - never reuse removed IDs
			if (act.Id == 0)
			{
				act.Id = _nextId++;
			}
			else if (_acts.Any(e => e.Id == act.Id))
			{
				// Safety: if non-zero ID already exists, find next available ID
				do
				{
					act.Id = _nextId++;
				} while (_acts.Any(e => e.Id == act.Id));
			}

			_acts.Add(act);
			return FailCode.None;
		}

		public FailCode TryRemoveAt(int index)
		{
			if ((uint)index >= (uint)_acts.Count) return FailCode.BadIndex;
			_acts.RemoveAt(index); // 自動左移：前密後空
			return FailCode.None;
		}

		public FailCode TryRemove(Act act)
		{
			for (int i = 0; i < _acts.Count; i++)
			{
				if (_acts[i].Id == act.Id)
				{
					_acts.RemoveAt(i);
					return FailCode.None;
				}
			}
			return FailCode.BadIndex; // Act not found
		}

		public void Clear() => _acts.Clear();

		/// 產生固定 5 格的槽位陣列：前段為 Act 實例，後段為 null 代表空槽。
		/// UI 直接渲染 5 格即可，無洞。
		public Act?[] ToSlots()
		{
			var slots = new Act?[Capacity];
			for (int i = 0; i < _acts.Count; i++)
				slots[i] = _acts[i];
			// 其餘維持 null 作為 Empty
			return slots;
		}

		// 可選安全讀取
		public bool TryGet(int index, out Act? act)
		{
			if ((uint)index >= (uint)_acts.Count) { act =null; return false; }
			act =_acts[index]; return true;
		}
	}
}
