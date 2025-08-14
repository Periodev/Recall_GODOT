
using System;
using System.Collections.Generic;
using CombatCore.Command;

namespace CombatCore.InterOp
{
	public enum BasicKind { A, B, C }

	public readonly struct BasicPlan
	{
		public BasicPlan(BasicKind kind, Actor src, Actor dst,
			int damage = 0, int block = 0, int chargeCost = 0, int gainAmount = 0, int apCost = 1)
		{
			Kind = kind; Source = src; Target = dst;
			Damage = damage; Block = block; 
			ChargeCost = chargeCost; GainAmount = gainAmount;
			APCost = apCost;
		}
		public BasicKind Kind { get; }
		public Actor Source { get; }
		public Actor Target { get; }
		public int Damage { get; }       // A
		public int Block { get; }        // B
		public int ChargeCost { get; }   // A/B 嘗試消耗
		public int GainAmount { get; }   // C 獲得
		public int APCost { get; }       // 消耗 AP 
	}

	public enum EchoOp { Attack, Block, GainCharge }

	public readonly struct RecallItemPlan
	{
		public RecallItemPlan(EchoOp op, int damage = 0, int block = 0, int chargeCost = 0, int gainAmount = 0)
		{
			Op = op; Damage = damage; Block = block; 
			ChargeCost = chargeCost; GainAmount = gainAmount;
		}
		public EchoOp Op { get; }
		public int Damage { get; }       // Attack
		public int Block { get; }        // Block
		public int ChargeCost { get; }   // Attack/Block 嘗試消耗
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
		public int BatchChargeCost { get; } // 若規則為整批只扣一次，Translator 設定 >0
		public int APCost { get; }          
	}

	public sealed class InterOps
	{
		public AtomicCmd[] BuildBasic(in BasicPlan plan)
		{
			var list = new List<AtomicCmd>(capacity: 4);
			list.Add(AtomicCmd.ConsumeAP(plan.Source, plan.APCost));   // 即使 0 也加入

			switch (plan.Kind)
			{
				case BasicKind.A:
					if (plan.ChargeCost > 0)
						list.Add(AtomicCmd.ConsumeCharge(plan.Source, plan.ChargeCost));
					if (plan.Damage > 0)
						list.Add(AtomicCmd.DealDamage(plan.Source, plan.Target, plan.Damage));
					break;

				case BasicKind.B:
					if (plan.ChargeCost > 0)
						list.Add(AtomicCmd.ConsumeCharge(plan.Source, plan.ChargeCost));
					if (plan.Block > 0)
						list.Add(AtomicCmd.AddShield(plan.Target, plan.Block));
					break;

				case BasicKind.C:
					if (plan.GainAmount > 0)
						list.Add(AtomicCmd.GainCharge(plan.Source, plan.GainAmount));
					break;
			}

			return list.ToArray();
		}

		public AtomicCmd[] BuildRecall(in RecallPlan plan)
		{
			// 預估：一次性扣費 + N 個項目
			var list = new List<AtomicCmd>(capacity: (plan.BatchChargeCost > 0 ? 1 : 0) + plan.Items.Count + 2);
			list.Add(AtomicCmd.ConsumeAP(plan.Source, plan.APCost));   // 即使 0 也加入

			// 批次一次性嘗試扣費（若規則需要）
			if (plan.BatchChargeCost > 0)
				list.Add(AtomicCmd.ConsumeCharge(plan.Source, plan.BatchChargeCost));

			foreach (var item in plan.Items)
			{
				switch (item.Op)
				{
					case EchoOp.Attack:
						if (item.ChargeCost > 0)
							list.Add(AtomicCmd.ConsumeCharge(plan.Source, item.ChargeCost));
						if (item.Damage > 0)
							list.Add(AtomicCmd.DealDamage(plan.Source, plan.Target, item.Damage));
						break;

					case EchoOp.Block:
						if (item.ChargeCost > 0)
							list.Add(AtomicCmd.ConsumeCharge(plan.Source, item.ChargeCost));
						if (item.Block > 0)
							list.Add(AtomicCmd.AddShield(plan.Target, item.Block));
						break;

					case EchoOp.GainCharge:
						if (item.GainAmount > 0)
							list.Add(AtomicCmd.GainCharge(plan.Source, item.GainAmount));
						break;
				}
			}

			return list.ToArray();
		}
	}
}
