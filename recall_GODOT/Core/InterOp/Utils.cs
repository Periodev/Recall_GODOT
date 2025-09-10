
using System;
using CombatCore;

namespace CombatCore
{
	public enum TokenType { A, B, C };     // Attack, Block, GainCharge

	/// <summary>
	/// 高階意圖操作代號 - 備用的抽象操作代號
	/// </summary>
	public enum HLAop : byte
	{
		Attack = 0x01,      // 對應 BasicIntent(TokenType.A)
		Block = 0x02,       // 對應 BasicIntent(TokenType.B)  
		Charge = 0x03,      // 對應 BasicIntent(TokenType.C)
		Recall = 0x10,      // 對應 RecallIntent
		EndTurn = 0xFF      // 結束回合意圖
	}

	public enum TargetType { None, Self, Target, All };
}

namespace Recall.InterOp
{
	// 確認 Actor 是否存在
	public delegate bool HasActorById(int id);

	// 嘗試取回 Actor 實體
	public delegate bool TryGetActorById(int id, out Actor actor);
}
