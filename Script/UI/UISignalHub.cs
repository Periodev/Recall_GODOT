
using System;
using CombatCore;

// UISignalHub is a static class that serves as a central hub for UI-related signals in the game.
// It allows different parts of the UI to subscribe to changes in actor states like charge, HP, and shield.
public static class UISignalHub
{

	public static Action<int>? OnHPChanged;
	public static Action<int>? OnChargeChanged;
	public static Action<int>? OnShieldChanged;
	public static Action<int>? OnAPChanged;


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

}
