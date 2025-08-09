using Godot;
using System;
using CombatCore.Component;

public static class ActorOp
{
	public static void Clear<T>(Actor actor) where T : Component
	{
		var comp = actor.Get<T>();
		comp?.Clear();
	}

	public static void Add<T>(Actor actor, int amount) where T : Component
	{
		var comp = actor.Get<T>();
		comp?.Add(amount);
	}

	public static void Cut<T>(Actor actor, int amount) where T : Component
	{
		var comp = actor.Get<T>();
		comp?.Cut(amount);
	}

	public static bool Use<T>(Actor actor, int amount) where T : Component
	{
		var comp = actor.Get<T>();
		return comp != null && comp.Use(amount);
	}

	public static void RefillAP(Actor actor)
	{
		var ap = actor?.Get<AP>();
		if (ap == null) return;
		ap.Refill();
	}

}
