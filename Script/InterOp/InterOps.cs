using System;
using System.Collections.Generic;
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

	public readonly struct RecallItemPlan
	{
		public RecallItemPlan(ActionType op, int damage = 0, int block = 0, int chargeCost = 0, int gainAmount = 0)
		{
			Op = op; Damage = damage; Block = block; 
			ChargeCost = chargeCost; GainAmount = gainAmount;
		}
		public ActionType Op { get; }
		public int Damage { get; }       // Attack
		public int Block { get; }        // Block
		public int ChargeCost { get; }   // Attack/Block charge consumption attempt
		public int GainAmount { get; }   // GainCharge
	}

	public readonly struct RecallPlan
	{
		public RecallPlan(Actor src, Actor dst, IReadOnlyList<RecallItemPlan> items, int batchChargeCost = 0, int apCost = 1)
		{
			Source = src; Target = dst; Items = items ?? Array.Empty<RecallItemPlan>();
			BatchChargeCost = batchChargeCost;
			APCost = apCost;
		}
		public Actor Source { get; }
		public Actor Target { get; }
		public IReadOnlyList<RecallItemPlan> Items { get; }
		public int BatchChargeCost { get; } // If rules require batch deduction only once, Translator sets >0
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
			// Estimate: one-time batch cost + N items
			var list = new List<AtomicCmd>(capacity: (plan.BatchChargeCost > 0 ? 1 : 0) + plan.Items.Count + 2);
			list.Add(AtomicCmd.ConsumeAP(plan.Source, plan.APCost));   // Add even if 0

			// Batch one-time charge cost (if rules require)
			if (plan.BatchChargeCost > 0)
				list.Add(AtomicCmd.ConsumeCharge(plan.Source, plan.BatchChargeCost));

			foreach (var item in plan.Items)
			{
				switch (item.Op)
				{
					case ActionType.A:
						if (item.ChargeCost > 0)
							list.Add(AtomicCmd.ConsumeCharge(plan.Source, item.ChargeCost));
						if (item.Damage > 0)
							list.Add(AtomicCmd.DealDamage(plan.Source, plan.Target, item.Damage));
						break;

					case ActionType.B:
						if (item.ChargeCost > 0)
							list.Add(AtomicCmd.ConsumeCharge(plan.Source, item.ChargeCost));
						if (item.Block > 0)
							list.Add(AtomicCmd.AddShield(plan.Source, item.Block)); // Fix: B always adds shield to source
						break;

					case ActionType.C:
						if (item.GainAmount > 0)
							list.Add(AtomicCmd.GainCharge(plan.Source, item.GainAmount));
						break;
				}
			}

			return list.ToArray();
		}
	}
}
