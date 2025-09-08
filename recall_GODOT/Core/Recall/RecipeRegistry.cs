using System;
using System.Collections.Generic;
using CombatCore;

namespace CombatCore.Recall
{
	public readonly struct RecipeData
	{
		public int Id { get; init; }
		public HLAop Op { get; init; }
		public TargetType TargetType { get; init; }
		public int CostAP { get; init; }
		
		// UI display strings (not used in Echo itself)
		public string Name { get; init; }
		public string Label { get; init; }
		public string Summary { get; init; }
	}

	public static class RecipeRegistry
	{
		private static readonly Dictionary<int, RecipeData> _recipes = new();
		
		static RecipeRegistry()
		{
			InitializeRecipes();
		}
		
		public static bool TryGetRecipe(int recipeId, out RecipeData recipe)
		{
			return _recipes.TryGetValue(recipeId, out recipe);
		}
		
		public static RecipeData GetRecipe(int recipeId)
		{
			if (_recipes.TryGetValue(recipeId, out var recipe))
				return recipe;
			
			throw new ArgumentException($"Recipe with ID {recipeId} not found", nameof(recipeId));
		}
		
		public static bool ContainsRecipe(int recipeId)
		{
			return _recipes.ContainsKey(recipeId);
		}
		
		private static void InitializeRecipes()
		{
			// Sample recipes - these would be populated from actual game data
			AddRecipe(1, HLAop.Attack, TargetType.Target, 1, "Attack", "A", "Basic attack");
			AddRecipe(2, HLAop.Block, TargetType.Self, 1, "Block", "B", "Basic defense");
			AddRecipe(3, HLAop.Charge, TargetType.Self, 1, "Charge", "C", "Gain charge/copy");
			AddRecipe(4, HLAop.Attack, TargetType.Target, 1, "Double Attack", "A+A", "Attack twice");
			AddRecipe(5, HLAop.Charge, TargetType.Self, 1, "Combo Charge", "A+C", "Attack then charge");
		}
		
		private static void AddRecipe(int id, HLAop op, TargetType targetType, int costAP, string name, string label, string summary)
		{
			var recipe = new RecipeData
			{
				Id = id,
				Op = op,
				TargetType = targetType,
				CostAP = costAP,
				Name = name,
				Label = label,
				Summary = summary
			};
			_recipes[id] = recipe;
		}
	}
}