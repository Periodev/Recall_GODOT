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
			AddRecipe(11, HLAop.Attack, TargetType.Target, 1, "Attack", "A", "Basic attack");
			AddRecipe(21, HLAop.Block, TargetType.Self, 1, "Block", "B", "Basic defense");
			AddRecipe(31, HLAop.Charge, TargetType.Self, 1, "Charge", "C", "Gain charge/copy");

			// ----------------------------------------------------
			// Echo (2L) — 對應 PatternKey: AA=11, AB=12, BA=21, BB=22
			// ID 規劃：11x/12x = 2L Echo（採用直觀數字對應）
			// TargetType 原則：只要含 A（攻擊）就需要 Target；純 B 給 Self
			// ----------------------------------------------------
			AddRecipe(111, HLAop.Attack, TargetType.Target, 1, "Echo: AA — Double Strike", "A+A", "double hit)");
			AddRecipe(121, HLAop.Attack, TargetType.Target, 1, "Echo: AB — Strike + Guard", "A+B", "(attack then block)");
			AddRecipe(211, HLAop.Attack, TargetType.Target, 1, "Echo: BA — Guard + Strike", "B+A", "(block then attack)");
			AddRecipe(221, HLAop.Block, TargetType.Self, 1, "Echo: BB — Fortify", "B+B", "(reinforce shield)");

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