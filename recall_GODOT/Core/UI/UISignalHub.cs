
using System;
using CombatCore;

// SignalHub is a static class that serves as a central hub for UI-related signals in the game.
// It allows different parts of the UI to subscribe to changes in actor states like charge, HP, and shield.
namespace CombatCore.UI
{
	public static class SignalHub
	{

		// 改為屬性提供控制存取
		public static Action<int>? OnHPChanged { get; set; }
		public static Action<int>? OnChargeChanged { get; set; }
		public static Action<int>? OnShieldChanged { get; set; }
		public static Action<int>? OnAPChanged { get; set; }

		public static Action? OnPlayerDrawComplete { get; set; }

		public static void NotifyHPChanged(int value)
		{
			OnHPChanged?.Invoke(value);
		}

		public static void NotifyChargeChanged(int value)
		{
			OnChargeChanged?.Invoke(value);
		}

		public static void NotifyShieldChanged(int value)
		{
			OnShieldChanged?.Invoke(value);
		}

		public static void NotifyAPChanged(int value)
		{
			OnAPChanged?.Invoke(value);
		}

		public static void NotifyPlayerDrawComplete()
		{
			OnPlayerDrawComplete?.Invoke();
		}

	}
}