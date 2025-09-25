using System;
using CombatCore;
using CombatCore.InterOp;

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
		DoubleStrike = 0x04, // Double Strike 技能
		Recall = 0x10,      // 對應 RecallIntent
		EndTurn = 0xFF      // 結束回合意圖
	}

	public enum TargetType { None, Self, Target, All };
}

namespace CombatCore
{
	public delegate bool TryGetActorById(int id, out Actor actor);
	public abstract record Intent(int? TargetId);
	public sealed record RecallIntent(int RecipeId) : Intent((int?)null);
	public sealed record ActIntent(Act Act, int? TargetId) : Intent(TargetId);

	public readonly struct RecallView
	{
		public RecallView(IReadOnlyList<TokenType> ops, IReadOnlyList<int> turns)
		{
			Ops = ops ?? Array.Empty<TokenType>();
			Turns = turns ?? Array.Empty<int>();
		}

		public IReadOnlyList<TokenType> Ops { get; }
		public IReadOnlyList<int> Turns { get; }
		public int Count => Ops.Count;
	}
}

namespace Recall.InterOp
{
	// 確認 Actor 是否存在
	public delegate bool HasActorById(int id);

	// 嘗試取回 Actor 實體
	public delegate bool TryGetActorById(int id, out Actor actor);
}
