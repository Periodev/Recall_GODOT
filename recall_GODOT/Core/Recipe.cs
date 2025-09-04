using System;

namespace CombatCore
{
	/// 配方結構，定義動作序列與對應的操作
	public readonly struct Recipe
	{
		/// 配方唯一標識符
		public readonly int Id;

		/// 配置標識符
		public readonly int ConfigId;

		/// 動作序列模式
		public readonly ActionType[] Pattern;

		/// 高階操作代號
		public readonly HLAop Op;

		/// 目標類型
		public readonly TargetType TargetType;

		/// 配方描述
		public readonly string Description;

		/// <summary>
		/// 建構 Recipe 實例
		/// <param name="id">配方 ID</param>
		/// <param name="configId">配置 ID</param>
		/// <param name="pattern">動作序列模式</param>
		/// <param name="op">高階操作</param>
		/// <param name="targetType">目標類型</param>
		/// <param name="description">描述</param>
		public Recipe(int id, int configId, ActionType[] pattern, HLAop op, TargetType targetType, string description)
		{
			Id = id;
			ConfigId = configId;
			Pattern = pattern;
			Op = op;
			TargetType = targetType;
			Description = description;
		}
	}
}