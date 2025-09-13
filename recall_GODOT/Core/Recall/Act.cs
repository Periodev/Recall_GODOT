using System;
using CombatCore;

namespace CombatCore.Recall
{
	[Flags]
	public enum ActionType { Basic = 1, Echo = 2 }

	public partial class Act
	{
		// === Core Spec (不可變) ===
		public HLAop Op { get; init; }
		public TargetType TargetType { get; init; }
		
		// === Changeable Spec ===
		public int CostAP { get; init; } = 1;
		public TokenType? PushMemory { get; init; } = null;
		public bool ConsumeOnPlay { get; init; } = true;
		public int CooldownTurns { get; init; } = 0;
		
		// === 建立時確定的識別符 ===
		public ActionType ActionFlags { get; init; } = ActionType.Echo;
		public int Id { get; set; }
		
		// === Runtime 變數 ===
		public int CooldownCounter { get; set; } = 0;
		public bool IsReady => CooldownCounter <= 0;
		
		// === UI 顯示 ===
		public int RecipeId { get; init; }  // debug only
		public string Name { get; init; } = "";
		public string RecipeLabel { get; init; } = "";
		public string Summary { get; init; } = "";
	}

	public static class ActFactory
	{
		/// <summary>
		/// Creates an Act from a recipe ID using pure lookup table approach.
		/// Returns Act with all fields populated from RecipeRegistry lookup.
		/// Initial ID will be updated during actStore.TryAdd to ensure uniqueness.
		/// </summary>

		public static Act BuildFromRecipe(int recipeId)
		{
			var template = RecipeRegistry.GetRecipe(recipeId);
			
			// 手動複製模板並重置 ID
			return new Act
			{
				Id = 0, // 待 ActStore 分配
				RecipeId = template.RecipeId,
				Op = template.Op,
				TargetType = template.TargetType,
				CostAP = template.CostAP,
				ActionFlags = template.ActionFlags,
				PushMemory = template.PushMemory,
				ConsumeOnPlay = template.ConsumeOnPlay,
				CooldownTurns = template.CooldownTurns,
				CooldownCounter = 0, // 重置運行時狀態
				Name = template.Name,
				RecipeLabel = template.RecipeLabel,
				Summary = template.Summary
			};
		}
	}
}
