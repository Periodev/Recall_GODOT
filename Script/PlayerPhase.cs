using Godot;
using System;
using CombatCore;

public static class PlayerPhase
{
    public static PhaseResult Init(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.PlayerDraw;
        return PhaseResult.Continue;
    }

    public static PhaseResult Draw(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.PlayerInput;
        return PhaseResult.Continue;
    }

    public static PhaseResult Input(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.PlayerExecute;
        return PhaseResult.WaitInput;
    }

    public static PhaseResult Execute(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.EnemyExecDelayed;
        return PhaseResult.Continue;
    }

}
