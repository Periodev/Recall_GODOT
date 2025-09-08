using System;
using CombatCore;

namespace CombatCore.Recall
{
	public static class EchoFactory
	{
		/// <summary>
		/// Creates an Echo from a recipe ID using pure lookup table approach.
		/// Returns Echo with all fields populated from RecipeRegistry lookup.
		/// Initial ID will be updated during echoStore.TryAdd to ensure uniqueness.
		/// </summary>
		public static Echo BuildFromRecipe(int recipeId, int turn)
		{
			if (!RecipeRegistry.TryGetRecipe(recipeId, out var recipe))
			{
				throw new ArgumentException($"Recipe with ID {recipeId} not found", nameof(recipeId));
			}
			
			// Temporary ID - will be replaced with unique random hash in echoStore.TryAdd
			var id = StableHash($"{recipeId}:{turn}");
			
			return new Echo
			{
				Id = id,
				RecipeId = recipeId,
				Op = recipe.Op,
				TargetType = recipe.TargetType,
				CostAP = recipe.CostAP,
				
				// Display fields filled from RecipeRegistry
				Name = recipe.Name,
				RecipeLabel = recipe.Label,
				Summary = recipe.Summary
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