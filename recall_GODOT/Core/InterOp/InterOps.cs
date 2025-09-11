using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CombatCore;
using CombatCore.Command;
using CombatCore.Recall;

namespace CombatCore.InterOp
{
	public abstract record Plan(Actor Source, int APCost);

	public sealed record BasicPlan(
		TokenType Act,
		Actor Source,
		Actor Target,
		int Damage,       // A
		int Block,        // B
		int ChargeCost,   // A/B charge consumption attempt
		int CopyCost,     // A/B copy consumption attempt  
		int GainAmount,   // C gain amount
		int APCost        // AP consumption
	) : Plan(Source, APCost);

	public sealed record RecallPlan(
		Actor Source,
		int RecipeId,
		int APCost
	) : Plan(Source, APCost);

	public sealed class InterOps
	{
		public static AtomicCmd[] Build(Plan plan)
		{
			return plan switch
			{
				BasicPlan bp => BuildBasic(bp),
				RecallPlan rp => BuildRecall(rp), 
				_ => Array.Empty<AtomicCmd>()
			};
		}

		public static AtomicCmd[] BuildBasic(BasicPlan plan)
		{
			var list = new List<AtomicCmd>(capacity: 6);
			list.Add(AtomicCmd.ConsumeAP(plan.Source, plan.APCost));   // Add even if 0

			switch (plan.Act)
			{
				case TokenType.A:
					// 玩家：先消耗 Copy，再執行攻擊
					if (plan.CopyCost > 0)
						list.Add(AtomicCmd.ConsumeCopy(plan.Source, plan.CopyCost));
					// 敵人：消耗 Charge 加成
					if (plan.ChargeCost > 0)
						list.Add(AtomicCmd.ConsumeCharge(plan.Source, plan.ChargeCost));
					
					// 根據 Copy 決定執行次數：Copy值+1
					int attackTimes = plan.CopyCost + 1;
					for (int i = 0; i < attackTimes; i++)
					{
						if (plan.Damage > 0)
							list.Add(AtomicCmd.DealDamage(plan.Source, plan.Target, plan.Damage));
					}
					break;

				case TokenType.B:
					// 類似 A 的邏輯
					if (plan.CopyCost > 0)
						list.Add(AtomicCmd.ConsumeCopy(plan.Source, plan.CopyCost));
					if (plan.ChargeCost > 0)
						list.Add(AtomicCmd.ConsumeCharge(plan.Source, plan.ChargeCost));
					
					int blockTimes = plan.CopyCost + 1;
					for (int i = 0; i < blockTimes; i++)
					{
						if (plan.Block > 0)
							list.Add(AtomicCmd.AddShield(plan.Target, plan.Block));
					}
					break;

				case TokenType.C:
					// 根據角色的組件類型決定獲得什麼
					// 有 Copy 組件的角色獲得 Copy，有 Charge 組件的角色獲得 Charge
					if (plan.GainAmount > 0)
					{
						if (plan.Source.Copy != null)
							list.Add(AtomicCmd.GainCopy(plan.Source, plan.GainAmount));
						else if (plan.Source.Charge != null)
							list.Add(AtomicCmd.GainCharge(plan.Source, plan.GainAmount));
					}
					break;
			}

			return list.ToArray();
		}

		public static AtomicCmd[] BuildRecall(RecallPlan plan)
		{
			// Minimal behavior for current milestone:
			// - Only consume AP
			// - Log the recalled pattern (no commas), e.g., [CA]
			var list = new List<AtomicCmd>(capacity: 1);
			list.Add(AtomicCmd.ConsumeAP(plan.Source, plan.APCost));

			// diagnostic message (non-invasive)
			Debug.WriteLine($"Player recalled RecipeId[{plan.RecipeId}]");

			return list.ToArray();
		}
	}
}
