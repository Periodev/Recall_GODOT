using System;
using CombatCore;

namespace CombatCore.Recall
{
	public partial class Echo
	{
		// UI view
		public int Id { get; init; }
		public int RecipeId { get; init; }

		public string Name { get; init; } = "";
		public string RecipeLabel { get; init; } = "";
		public string Summary { get; init; } = "";

		// HLA behavior
		public int CostAP { get; init; } = 1;
		public HLAop Op { get; init; }
		public TargetType TargetType { get; init; }


		/// 最小建構：由記憶序列組出一張 Echo（決定論 Id / RecipeId）。
		/// 預設命名： "Recall " + 連成字串；Summary 預設空字串。
		public static Echo Build(
			ActionType[] sequence,
			int turn,
			int runSeed = 0,
			Func<ActionType[], string>? nameFactory = null,
			Func<ActionType[], string>? summaryFactory = null)
		{
			// 1) Recipe 標籤與 Id（決定論）
			var recipeLabel = string.Join("+", sequence);
			var recipeId = StableHash(recipeLabel);

			// 2) Heuristics（與你目前規則一致）
			bool hasA = sequence.Contains(ActionType.A);

			var opKind = (sequence.Length == 1 && hasA) ? HLAop.Attack : HLAop.Charge;
			var targetType = hasA ? TargetType.Target : TargetType.Self;

			// 3) Name / Summary（可注入工廠；未提供則使用預設/空字串）
			var name = nameFactory?.Invoke(sequence) ?? $"Recall {string.Concat(sequence)}";
			var summary = summaryFactory?.Invoke(sequence) ?? string.Empty;

			// 4) Echo 物件（Id 以 sequence+turn+seed 產生決定論雜湊）
			return new Echo
			{
				Id = StableHash($"{recipeId}:{turn}:{runSeed}"),
				RecipeId = recipeId,
				Name = name,
				RecipeLabel = recipeLabel,
				Summary = summary,
				CostAP = 1,
				Op = opKind,
				TargetType = targetType,
			};
		}

		// 簡單、決定論的穩定雜湊（避免 DateTime.Now 引發重播不一致）
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
