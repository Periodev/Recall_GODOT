
using System;

namespace CombatCore.Abstractions
{
	/// <summary>
	/// Actor 查找介面 - 核心抽象，不依賴任何引擎
	/// 由 CombatState (Godot) 和測試 Fake 共同實作
	/// </summary>
	public interface IActorLookup
	{
		Actor GetActor(int id);
	}
}
