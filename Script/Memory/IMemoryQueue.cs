
using System;
using System.Collections.Generic;
using CombatCore;

namespace CombatCore.Memory
{
	public enum ActionType { A, B, C }

	public interface IMemoryQueue
	{
		int Capacity { get; }
		int Count { get; }

		// 只接受 A/B/C。枚舉限定，不存在其他值
		void PushBasic(ActionType a);

		// 0 = 最舊；越界拋 ArgumentOutOfRangeException
		ActionType Peek(int idxFromOldest);

		// 由舊到新快照（唯讀）
		IReadOnlyList<ActionType> Snapshot();
	}
}
