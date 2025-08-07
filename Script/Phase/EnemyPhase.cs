using Godot;
using System;
using CombatCore;

public static class EnemyPhase
{
    public static PhaseResult Init(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.EnemyIntent;
        return PhaseResult.Continue;
    }

    public static PhaseResult Intent(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.EnemyExecInstant;
        return PhaseResult.Continue;
    }

    public static PhaseResult ExecInstant(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.PlayerInit;
        return PhaseResult.Continue;
    }

    public static PhaseResult ExecDelayed(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.TurnEnd;
        return PhaseResult.Continue;
    }
}
