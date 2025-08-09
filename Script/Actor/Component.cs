using Godot;
using System;
using CombatCore;

namespace CombatCore.Component
{
	public class Component
	{
		public int Value;
		public int? Max;

		public Component(int? max = null)
		{
			Max = max;
		}

		public virtual void Clear() => Value = 0;

		public virtual void Add(int amount)
		{
			if (amount <= 0) return;
			if (Max.HasValue)
				Value = Math.Min(Value + amount, Max.Value);
			else
				Value += amount;
		}

		public virtual void Cut(int amount)
		{
			if (amount <= 0) return;
			Value = Math.Max(0, Value - amount);
		}

		public virtual bool Use(int amount)
		{
			if (amount <= 0 || Value < amount) return false;
			Value -= amount;
			return true;
		}
	}

	public class HP : Component
	{
		public HP(int maxHP) : base(maxHP)
		{
			Value = maxHP;
		}
	}

	public class AP : Component
	{
		public int PerTurn => Max ?? 0;     // 每回合恢復的 AP

		public AP(int perTurn) : base(perTurn)
		{
			Value = perTurn;        // 初始 AP 設定為每回合恢復的值
		}

		public override void Add(int amount)
		{
			if (amount <= 0) return;
			Value += amount;        //  no Max limit for AP
		}

		public void Refill()
		{
			Value = PerTurn;        // 每回合恢復 AP
		}
	}


	public class Shield : Component
	{
		public Shield() : base(int.MaxValue) { }
	}

	public class Charge : Component
	{
		public Charge() : base(3) { }
	}
}
