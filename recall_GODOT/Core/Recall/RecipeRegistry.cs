using System;
using System.Collections.Generic;
using CombatCore;

namespace CombatCore.Recall
{
	public static class RecipeRegistry
	{
		private static readonly Dictionary<int, Act> _recipes = new();

		static RecipeRegistry()
		{
			InitializeRecipes();
		}

		public static bool TryGetRecipe(int recipeId, out Act recipe)
		{
			return _recipes.TryGetValue(recipeId, out recipe);
		}

		public static Act GetRecipe(int recipeId)
		{
			if (_recipes.TryGetValue(recipeId, out var recipe))
				return recipe;

			throw new ArgumentException($"Recipe with ID {recipeId} not found", nameof(recipeId));
		}

		public static bool ContainsRecipe(int recipeId)
		{
			return _recipes.ContainsKey(recipeId);
		}

		// 新增：用於 RecipeSystem 的模式過濾
		public static IEnumerable<Act> GetAllRecipes()
		{
			return _recipes.Values;
		}

		public static IEnumerable<int> GetAllRecipeIds()
		{
			return _recipes.Keys;
		}

		private static void InitializeRecipes()
		{
			// Basic recipes (1L)
			AddRecipe(11, new Act
			{
				RecipeId = 11,
				Op = HLAop.Attack,
				TargetType = TargetType.Target,
				CostAP = 1,
				ActionFlags = ActionType.Basic,
				PushMemory = TokenType.A,
				ConsumeOnPlay = false,
				CooldownTurns = 0,
				Name = "Attack",
				RecipeLabel = "A",
				Summary = "Basic attack"
			});

			AddRecipe(21, new Act
			{
				RecipeId = 21,
				Op = HLAop.Block,
				TargetType = TargetType.Self,
				CostAP = 1,
				ActionFlags = ActionType.Basic,
				PushMemory = TokenType.B,
				ConsumeOnPlay = false,
				CooldownTurns = 0,
				Name = "Block",
				RecipeLabel = "B",
				Summary = "Basic defense"
			});

			// Echo recipes (2L)
			AddRecipe(111, new Act
			{
				RecipeId = 111,
				Op = HLAop.Attack,
				TargetType = TargetType.Target,
				CostAP = 1,
				ActionFlags = ActionType.Echo,
				PushMemory = null,
				ConsumeOnPlay = true,
				CooldownTurns = 0,
				Name = "Double Strike",
				RecipeLabel = "A+A",
				Summary = "double hit"
			});

			AddRecipe(112, new Act
			{
				RecipeId = 112,
				Op = HLAop.Attack,
				TargetType = TargetType.Target,
				CostAP = 1,
				ActionFlags = ActionType.Echo,
				PushMemory = null,
				ConsumeOnPlay = true,
				CooldownTurns = 0,
				Name = "Heavy Strike",
				RecipeLabel = "A+A",
				Summary = "Heavy Strike"
			});

			AddRecipe(121, new Act
			{
				RecipeId = 121,
				Op = HLAop.Attack,
				TargetType = TargetType.Target,
				CostAP = 1,
				ActionFlags = ActionType.Echo,
				PushMemory = null,
				ConsumeOnPlay = true,
				CooldownTurns = 0,
				Name = "Strike + Guard",
				RecipeLabel = "A+B",
				Summary = "attack then block"
			});

			AddRecipe(211, new Act
			{
				RecipeId = 211,
				Op = HLAop.Attack,
				TargetType = TargetType.Target,
				CostAP = 1,
				ActionFlags = ActionType.Echo,
				PushMemory = null,
				ConsumeOnPlay = true,
				CooldownTurns = 0,
				Name = "Guard + Strike",
				RecipeLabel = "B+A",
				Summary = "block then attack"
			});

			AddRecipe(212, new Act
			{
				RecipeId = 212,
				Op = HLAop.Attack,
				TargetType = TargetType.Target,
				CostAP = 1,
				ActionFlags = ActionType.Echo,
				PushMemory = null,
				ConsumeOnPlay = true,
				CooldownTurns = 0,
				Name = "Bash Attack",
				RecipeLabel = "B+A",
				Summary = "Bash Attack"
			});

			AddRecipe(221, new Act
			{
				RecipeId = 221,
				Op = HLAop.Block,
				TargetType = TargetType.Self,
				CostAP = 1,
				ActionFlags = ActionType.Echo,
				PushMemory = null,
				ConsumeOnPlay = true,
				CooldownTurns = 0,
				Name = "Fortify",
				RecipeLabel = "B+B",
				Summary = "reinforce shield"
			});
		}

		private static void AddRecipe(int id, Act recipe)
		{
			_recipes[id] = recipe;
		}
	}
}
