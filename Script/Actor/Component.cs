using Godot;
using System;
using CombatCore;

namespace CombatCore.Component
{
	public class Charge
	{
		public const int MaxCharge = 3;
		public int Value;
		public void Add(int amount)
		{
			Value = Math.Min(Value + amount, MaxCharge);
			UISignalHub.NotifyChargeChanged(Value);
		}

		public void Cut(int amount)
		{
			Value = Math.Max(0, Value - amount);
			UISignalHub.NotifyChargeChanged(Value);
		}

		public bool Use(int amount)
		{
			if (Value < amount) return false;
			Value -= amount;
			UISignalHub.NotifyChargeChanged(Value);
			return true;
		}

		public bool UseAll()
		{
			if (Value == 0) return false;
			Value = 0;
			UISignalHub.NotifyChargeChanged(Value);
			return true;
		}

		public void Clear()
		{
			Value = 0;
			UISignalHub.NotifyChargeChanged(Value);
		}

	}

}
