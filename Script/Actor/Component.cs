using Godot;
using System;
using CombatCore;

namespace CombatCore.Component
{
	public class Component
	{
		// Interface, only for type checking
		public interface IBasic { }
		public interface IAbility { }
		public interface IBuff { }

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

	public class HP : Component, Component.IBasic
	{
		public HP(int maxHP) : base(maxHP)
		{
			Value = maxHP;
		}
	}

	public class AP : Component, Component.IBasic
	{
		public int PerTurn { get; private set; }

		public AP(int perTurn) : base(null)
		{
			PerTurn = perTurn;      // 設定每回合恢復量
			Value = perTurn;        // 初始 AP 設定為每回合恢復的值
		}

		public void Refill()
		{
			Value = PerTurn;        // 每回合恢復 AP
		}
	}


	public class Shield : Component, Component.IBasic
	{
		public Shield() : base(null) { }
	}

	public class Charge : Component, Component.IBasic
	{
		public Charge() : base(3) { }
	}
}
