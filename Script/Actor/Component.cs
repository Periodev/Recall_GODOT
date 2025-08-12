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

		public virtual int Add(int amount)
		{
			int lastValue = Value;

			if (amount <= 0) return 0;
			if (Max.HasValue)
				Value = Math.Min(Value + amount, Max.Value);
			else
				Value += amount;

			return Value - lastValue; // 返回實際增加的值
		}

		public virtual int Cut(int amount)
		{
			int lastValue = Value;

			if (amount <= 0) return 0;
			Value = Math.Max(0, Value - amount);
			return lastValue - Value;      // 返回實際減少的值
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

		public int Refill()
		{
			int lastValue = Value;
			Value = PerTurn;
			return Value - lastValue;  // 返回實際恢復量		
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
