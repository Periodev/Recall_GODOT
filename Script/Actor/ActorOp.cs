using Godot;
using System;
using CombatCore.Component;

public static partial class ActorOp
{
	public static void Clear(Actor actor, ComponentType type)
	{
		if (actor == null) return;
		ref int value = ref GetComponentValue(actor, type);
		value = 0;
		GetNotifyAction(type)?.Invoke(value);
	}

	public static void Add(Actor actor, ComponentType type, int amount)
	{
		if (actor == null || amount < 0) return;
		ref int value = ref GetComponentValue(actor, type);
		int max = GetComponentMax(actor, type);

		value = Math.Min(value + amount, max);
		GetNotifyAction(type)?.Invoke(value);
	}

	public static void Cut(Actor actor, ComponentType type, int amount)
	{
		if (actor == null || amount < 0) { return; }
		ref int value = ref GetComponentValue(actor, type);
		value = (value < amount) ? 0 : value - amount;
		GetNotifyAction(type)?.Invoke(value);
	}

	public static bool Use(Actor actor, ComponentType type, int amount)
	{
		if (actor == null || amount < 0) { return false; }
		ref int value = ref GetComponentValue(actor, type);
		if (value < amount) { return false; }

		Cut(actor, type, amount);  // 直接呼叫 Cut
		return true;
	}

	public static ref int GetComponentValue(Actor actor, ComponentType type)
	{
		return ref actor._components[(int)type].Value;
	}

	public static int GetComponentMax(Actor actor, ComponentType type)
	{
		return actor._components[(int)type].Max.Value;
	}

	static Action<int> GetNotifyAction(ComponentType type)
	{
		return type switch
		{
			ComponentType.Shield => UISignalHub.NotifyShieldChanged,
			ComponentType.Charge => UISignalHub.NotifyChargeChanged,
			ComponentType.HP => UISignalHub.NotifyHPChanged,
			_ => null
		};
	}

}
