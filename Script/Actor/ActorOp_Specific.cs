using Godot;
using System;

public static partial class ActorOp
{
    private const int MaxCharge = 3;
    public static void ClearShield(Actor actor)
    {
        if (actor == null) { return; }
        actor.Shield = 0;
        GD.Print("Cleared shield from actor.");
    }

    public static void AddShield(Actor actor, int value)
    {
        if (actor == null) { return; }
        actor.Shield += value;
        GD.Print($"Added {value} shield to actor. New shield value: {actor.Shield}");
    }

    public static void CutShield(Actor actor, int value)
    {
        if (actor == null || value < 0) { return; }

        int blocked = Math.Min(actor.Shield, value);

        actor.Shield -= blocked;
        GD.Print($"Cut {blocked} shield from actor. New shield value: {actor.Shield}");
    }


}
