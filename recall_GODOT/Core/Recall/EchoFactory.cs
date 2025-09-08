using System;
using CombatCore;

namespace CombatCore.Recall
{
	public static class EchoFactory
	{
		/// <summary>
		/// Creates an Echo from a recipe ID using pure lookup table approach.
		/// Returns Echo with only execution semantic fields (Id, RecipeId, Op, TargetType, CostAP).
		/// Does not include name/summary - UI should get display strings via RecipeRegistry.
		/// </summary>
		public static Echo BuildFromRecipe(int recipeId, int turn, int runSeed = 0)
		{
			if (!RecipeRegistry.TryGetRecipe(recipeId, out var recipe))
			{
				throw new ArgumentException($"Recipe with ID {recipeId} not found", nameof(recipeId));
			}
			
			// Generate deterministic ID: Hash(recipeId, turn, runSeed)
			var id = StableHash($"{recipeId}:{turn}:{runSeed}");
			
			return new Echo
			{
				Id = id,
				RecipeId = recipeId,
				Op = recipe.Op,
				TargetType = recipe.TargetType,
				CostAP = recipe.CostAP,
				
				// Empty display fields - UI should use RecipeRegistry for these
				Name = "",
				RecipeLabel = "",
				Summary = ""
			};
		}
		
		// Simple, deterministic stable hash (avoid DateTime.Now for replay consistency)
		private static int StableHash(string s)
		{
			unchecked
			{
				int h = 23;
				for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
				return h;
			}
		}
	}
}