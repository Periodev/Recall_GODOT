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
		public static Echo BuildFromRecipe(int recipeId)
		{
			if (!RecipeRegistry.TryGetRecipe(recipeId, out var recipe))
			{
				throw new ArgumentException($"Recipe with ID {recipeId} not found", nameof(recipeId));
			}
			
			return new Echo
			{
				Id = 0,		// unassigned
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
	}
}