using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CombatCore;
using CombatCore.Command;
using CombatCore.Memory;

namespace CombatCore.InterOp
{
	public readonly struct BasicPlan
	{
		public BasicPlan(ActionType act, Actor src, Actor dst,
			int damage = 0, int block = 0, int chargeCost = 0, int gainAmount = 0, int apCost = 1)
		{
			Act = act; Source = src; Target = dst;
			Damage = damage; Block = block; 
			ChargeCost = chargeCost; GainAmount = gainAmount;
			APCost = apCost;
		}
		public ActionType Act { get; }
		public Actor Source { get; }
		public Actor Target { get; }
		public int Damage { get; }       // A
		public int Block { get; }        // B
		public int ChargeCost { get; }   // A/B charge consumption attempt
		public int GainAmount { get; }   // C gain amount
		public int APCost { get; }       // AP consumption
	}

	public readonly struct RecallPlan
	{
		public RecallPlan(Actor src, ActionType[] sequence, int apCost = 1)
		{ Source = src; ActionSequence = sequence ?? Array.Empty<ActionType>(); APCost = apCost; }
		
		public Actor Source { get; }
		public ActionType[] ActionSequence { get; } // display only
		public int APCost { get; }          
	}

	public sealed class InterOps
	{
		public AtomicCmd[] BuildBasic(in BasicPlan plan)
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

		public AtomicCmd[] BuildRecall(in RecallPlan plan)
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
