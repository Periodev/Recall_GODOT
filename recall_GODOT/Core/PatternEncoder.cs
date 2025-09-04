using System;

namespace CombatCore
{
	/// 靜態類別，用於將 ActionType 序列編碼為整數
	/// 使用三進制編碼：A=1, B=2, C=3
	public static class PatternEncoder
	{
		/// 將 ActionType 序列編碼為整數
		/// <param name="sequence">ActionType 序列</param>
		/// <returns>編碼後的整數</returns>
		public static int Encode(ActionType[] sequence)
		{
			int result = 0;
			foreach (ActionType action in sequence)
			{
				result = result * 3 + (int)action + 1;
			}
			return result;
		}
	}
}