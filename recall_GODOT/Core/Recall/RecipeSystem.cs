using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CombatCore.Recall
{
	public static class RecipeSystem
	{
		public static void FilterRecipesByPattern(int pattern)
		{
			var matchingRecipes = new List<int>();
			
			foreach (int recipeId in GetAllRecipeIds())
			{
				int recipePattern = recipeId / 10;
				if (recipePattern == pattern)
				{
					matchingRecipes.Add(recipeId);
				}
			}
			
			foreach (int id in matchingRecipes)
			{
				Debug.Print($"Recipe ID: {id}");
			}
		}
		
		private static IEnumerable<int> GetAllRecipeIds()
		{
			var recipeIds = new List<int>();
			
			for (int id = 1; id <= 9999; id++)
			{
				if (RecipeRegistry.ContainsRecipe(id))
				{
					recipeIds.Add(id);
				}
			}
			
			return recipeIds;
		}
	}
}
