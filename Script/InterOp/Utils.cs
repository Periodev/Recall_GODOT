
using System;

namespace CombatCore
{
	public enum ActionType { A, B, C };		// Attack, Block, GainCharge
}

namespace Recall.InterOp
{
	// 確認 Actor 是否存在
	public delegate bool HasActorById(int id);

	// 嘗試取回 Actor 實體
	public delegate bool TryGetActorById(int id, out Actor actor);
}
