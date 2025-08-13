
using System;
using CombatCore.Abstractions;
using CombatCore.Memory;
using CombatCore.Command;

namespace CombatCore.InterOp
{
	public interface IInterOps
	{
		string LastError { get; } // 空字串代表無錯
		AtomicCmd[] Translate(InterOpCall call, IActorLookup lookup, IMemoryQueue queue);
	}
}
