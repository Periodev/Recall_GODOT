using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CombatCore;
using CombatCore.Command;
using CombatCore.Memory;

namespace CombatCore.InterOp
{
	public abstract record Plan(Actor Source, int APCost);

	public sealed record BasicPlan(
		ActionType Act,
		Actor Source,
		Actor Target,
		int Damage,       // A
		int Block,        // B
		int ChargeCost,   // A/B charge consumption attempt
		int GainAmount,   // C gain amount
		int APCost        // AP consumption
	) : Plan(Source, APCost);

	public sealed record RecallPlan(
		Actor Source,
		ActionType[] ActionSequence, // display only
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
			var list = new List<AtomicCmd>(capacity: 4);
			list.Add(AtomicCmd.ConsumeAP(plan.Source, plan.APCost));   // Add even if 0

			switch (plan.Act)
			{
				case ActionType.A:
					if (plan.ChargeCost > 0)
						list.Add(AtomicCmd.ConsumeCharge(plan.Source, plan.ChargeCost));
					if (plan.Damage > 0)
						list.Add(AtomicCmd.DealDamage(plan.Source, plan.Target, plan.Damage));
					break;

				case ActionType.B:
					if (plan.ChargeCost > 0)
						list.Add(AtomicCmd.ConsumeCharge(plan.Source, plan.ChargeCost));
					if (plan.Block > 0)
						list.Add(AtomicCmd.AddShield(plan.Target, plan.Block));
					break;

				case ActionType.C:
					if (plan.GainAmount > 0)
						list.Add(AtomicCmd.GainCharge(plan.Source, plan.GainAmount));
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
			var pattern = string.Join("", plan.ActionSequence.Select(x => x.ToString()));
			Debug.WriteLine($"Player recalled [{pattern}]");

			return list.ToArray();
		}
	}
}
