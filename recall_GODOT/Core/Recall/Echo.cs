using System;
using CombatCore;

namespace CombatCore.Recall
{
	public partial class Echo
	{
		// UI view
		public int Id { get; set; }
		public int RecipeId { get; init; }

		public string Name { get; init; } = "";
		public string RecipeLabel { get; init; } = "";
		public string Summary { get; init; } = "";

		// HLA behavior
		public int CostAP { get; init; } = 1;
		public HLAop Op { get; init; }
		public TargetType TargetType { get; init; }

	}

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
