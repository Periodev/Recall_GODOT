
using System;
using CombatCore.Component;
using static CombatCore.Result;

// SelfOp is a static class that provides operations for the Actor class.
// It includes methods for manipulating the actor's shield, HP, charge, and AP.
// This allows for a clean and organized way to perform operations on the actor without directly modifying its properties.

namespace CombatCore.ActorOp
{
	public static class SelfOp
	{

		// Shield
		public static int AddShield(Actor self, int amount) => 
			(self == null || amount <= 0) ? 0 : self.Shield.Add(amount);

		public static int CutShield(Actor self, int amount) => 
			(self == null || amount <= 0) ? 0 : self.Shield.Cut(amount);

		public static void ClearShield(Actor self) => self?.Shield?.Clear();

		// HP
		public static int RestoreHP(Actor self, int amount) => 
			(self == null || amount <= 0) ? 0 : self.HP.Add(amount);

		public static int CutHP(Actor self, int amount) => 
			(self == null || amount <= 0) ? 0 : self.HP.Cut(amount);

		// Charge
		public static int GainCharge(Actor self, int amount) => 
			(self?.Charge == null || amount <= 0) ? 0 : self.Charge.Add(amount);

		public static bool ConsumeCharge(Actor self, int amount)
		{
			if (self?.Charge == null || amount <= 0) return FAIL;
			return self.Charge.Use(amount);
		}

		public static void ClearCharge(Actor self) => self?.Charge?.Clear();

		// Copy
		public static int GainCopy(Actor self, int amount) => 
			(self?.Copy == null || amount <= 0) ? 0 : self.Copy.Add(amount);

		public static bool ConsumeCopy(Actor self, int amount = 1)
		{
			if (self?.Copy == null || amount <= 0) return FAIL;
			return self.Copy.Use(amount);
		}

		public static void ClearCopy(Actor self) => self?.Copy?.Clear();

		// AP
		public static int GainAP(Actor self, int amount) => 
			(self == null || amount <= 0) ? 0 : self.AP.Add(amount);

		public static bool ConsumeAP(Actor self, int amount)
		{
			if (self == null || amount <= 0) return FAIL;
			return self.AP.Use(amount) ? PASS : FAIL;
		}

		public static int RefillAP(Actor self) => self?.AP?.Refill() ?? 0;
	}
}
