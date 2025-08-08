using Godot;
using System;

public static partial class ActorOp
{
	public static void Clear(ref int field)
	{
		// shield, charge, and other fields can be cleared similarly
		field = 0;
	}

	public static void Add(ref int field, int value)
	{
		if (value < 0) { return; }
		field += value;
	}

	public static void Cut(ref int field, int value)
	{
		if (value < 0) { return; }
		field = (field < value) ? 0 : field - value;
	}

	public static void Use(ref int field, int value)
	{
		if (value < 0) { return; }
		field = (field < value) ? 0 : field - value;
	}

}
