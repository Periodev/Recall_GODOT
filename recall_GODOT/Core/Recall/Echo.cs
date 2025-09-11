using System;
using CombatCore;

namespace CombatCore.Recall
{
	[Flags]
	public enum ActionType { Basic = 1, Echo = 2 }

	public partial class Echo
	{
		// === 原有 UI 屬性 ===
		public int Id { get; set; }
		public int RecipeId { get; init; }

		public string Name { get; init; } = "";
		public string RecipeLabel { get; init; } = "";
		public string Summary { get; init; } = "";

		// === 原有 HLA 屬性 ===
		public int CostAP { get; init; } = 1;
		public HLAop Op { get; init; }
		public TargetType TargetType { get; init; }

		// === 新增：行為分類與執行規則 ===
		public ActionType ActionFlags { get; init; } = ActionType.Echo;
		public TokenType? PushMemory { get; init; } = null;      // A/B/C or null
		public bool ConsumeOnPlay { get; init; } = true;         // Echo=true, Basic=false
		public int CooldownTurns { get; init; } = 0;             // 未來擴展用
		
		// === 冷卻計數器 ===
		public int CooldownCounter { get; set; } = 0;
		public bool IsReady => CooldownCounter <= 0;

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
