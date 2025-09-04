using System;
using System.Collections.Generic;

namespace CombatCore
{
	/// <summary>
	/// 配方元資料記錄結構
	/// </summary>
	public readonly record struct RecipeMeta(
		HLAop Op,
		TargetType Target,
		string Description,
		int CostAP
	);

	/// <summary>
	/// 配方系統靜態類別，管理配方映射和註冊表
	/// </summary>
	public static class RecipeSystem
	{
		/// <summary>
		/// 配方映射表：(patternKey, configKey) -> recipeId
		/// </summary>
		private static readonly Dictionary<(int patternKey, int configKey), int> Map = new()
		{
			{ (1, 1), 101 },  // A pattern with config 1 -> recipe 101
			{ (6, 1), 201 }   // AC pattern with config 1 -> recipe 201
		};

		/// <summary>
		/// 配方註冊表：recipeId -> RecipeMeta
		/// </summary>
		private static readonly Dictionary<int, RecipeMeta> Registry = new()
		{
			{ 101, new RecipeMeta(HLAop.Attack, TargetType.Target, "Basic Attack", 1) },
			{ 201, new RecipeMeta(HLAop.Charge, TargetType.Self, "Attack then Charge", 2) }
		};

		/// <summary>
		/// 檢查是否存在指定的配方
		/// </summary>
		/// <param name="patternKey">模式編碼</param>
		/// <param name="configKey">配置編碼</param>
		/// <returns>配方是否存在</returns>
		public static bool HasRecipe(int patternKey, int configKey)
		{
			return Map.ContainsKey((patternKey, configKey));
		}

		/// <summary>
		/// 獲取指定的配方
		/// </summary>
		/// <param name="patternKey">模式編碼</param>
		/// <param name="configKey">配置編碼</param>
		/// <returns>配方元資料，如果不存在則返回 null</returns>
		public static RecipeMeta? GetRecipe(int patternKey, int configKey)
		{
			if (Map.TryGetValue((patternKey, configKey), out int recipeId))
			{
				if (Registry.TryGetValue(recipeId, out RecipeMeta recipeMeta))
				{
					return recipeMeta;
				}
			}
			return null;
		}
	}
}