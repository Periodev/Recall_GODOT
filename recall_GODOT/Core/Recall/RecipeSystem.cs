using System;
using System.Collections.Generic;
using System.Linq;

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

    public static class RecipeSystem
	{
		public static int Encode(TokenType[] actions)
		{
			int result = 0;
			foreach (var action in actions)
			{
				result = result * 10 + ActionToDigit(action);
			}
			return result;
		}

		private static int ActionToDigit(TokenType action)
		{
			return action switch
			{
				TokenType.A => 1,
				TokenType.B => 2,
				TokenType.C => 3,
				_ => throw new ArgumentException($"Unsupported action: {action}")
			};
		}

		public static List<int> FilterRecipesByPattern(int pattern)
		{
			var matchingRecipes = new List<int>();

			foreach (var recipeId in RecipeRegistry.GetAllRecipeIds())
			{
				int recipePattern = recipeId / 10;
				if (recipePattern == pattern)
				{
					matchingRecipes.Add(recipeId);
				}
			}

			matchingRecipes.Sort(); // 確保穩定排序
			return matchingRecipes;
		}
	}
}
