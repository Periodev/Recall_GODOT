using System;
using System.Linq;
using CombatCore;

namespace CombatCore.Recall
{
	public enum PatternKey : int
	{
		// 1L (1-Length) - 單動作
		A = 1,
		B = 2,

		// 2L (2-Length) - 雙動作組合  
		AA = 11,
		AB = 12,
		BA = 21,
		BB = 22,

		// 3L (3-Length) - 三動作組合
		AAA = 111,
		AAB = 112,
		ABA = 121,
		ABB = 122,
		BAA = 211,
		BAB = 212,
		BBA = 221,
		BBB = 222
	}

	public static class PatternExtractor
	{
		/// <summary>
		/// 從動作序列轉換為 PatternKey (支援 1L~3L)
		/// 只處理 AB-only，忽略 C
		/// </summary>

		public static int Encode(ActionType[] actions)
		{
			int result = 0;
			foreach (var action in actions)
			{
				result = result * 10 + ActionToDigit(action);
			}
			return result;
		}

		private static int ActionToDigit(ActionType action)
		{
			return action switch
			{
				ActionType.A => 1,
				ActionType.B => 2,
				ActionType.C => 3,
				_ => throw new ArgumentException($"Unsupported action: {action}")
			};
		}
	}
}
