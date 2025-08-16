
using System;
using CombatCore;
using CombatCore.Abstractions;
using CombatCore.Command;
using CombatCore.InterOp;
using CombatCore.Memory;

public sealed class CombatEngine
{
    // 靜態管線
    private readonly HLATranslator _translator = new();
    private readonly InterOps _interops = new();
    private readonly CmdExecutor _exec = new();

    // Basic：A/B/C
    public ExecResult RunBasic(CombatState s, ActionType act, int? targetId)
    {
        if (s is null) return ExecResult.Fail(FailCode.PhaseLocked);

        var phase = s.PhaseCtx;
        var self  = s.Player;

        // Basic 不用記憶，但簽名需要
        var emptyView = new RecallView(Array.Empty<ActionType>(), Array.Empty<int>());

        var fc = _translator.TryTranslate(
            new BasicIntent(act, targetId),
            phase, emptyView, s, self,
            out var basic, out _);

        if (fc != FailCode.None) return ExecResult.Fail(fc);

        var cmds = _interops.BuildBasic(basic);
        var res  = _exec.ExecuteOrDiscard(cmds);

        if (!res.Ok) return res;

        // 記憶僅在 Basic 寫入
        s.Mem?.Push(act, phase.TurnNum);

        // 推到 Execute，交由外部 Runner
        s.PhaseCtx.Step = PhaseStep.PlayerExecute;
        return res;
    }

    // Recall：indices 來自 UI，targetId 只有包含 A 時才需要
    public ExecResult RunRecall(CombatState s, int[] indices, int? targetId)
    {
        if (s is null) return ExecResult.Fail(FailCode.PhaseLocked);
        if (indices is null || indices.Length == 0) return ExecResult.Fail(FailCode.BadIndex);

        var phase = s.PhaseCtx;
        var self  = s.Player;

        // 由 MemoryQueue 建 recall view
        var view = new RecallView(s.Mem.SnapshotOps(), s.Mem.SnapshotTurns());

        var fc = _translator.TryTranslate(
            new RecallIntent(indices, targetId),
            phase, view, s, self,
            out _, out var recall);

        if (fc != FailCode.None) return ExecResult.Fail(fc);

        var cmds = _interops.BuildRecall(recall);
        var res  = _exec.ExecuteOrDiscard(cmds);

        if (!res.Ok) return res;

        // Recall 僅標記一次/回合
        s.PhaseCtx.MarkRecallUsed();
        s.PhaseCtx.Step = PhaseStep.PlayerExecute;

        return res;
    }

    // EndTurn：僅改 Phase，讓 Runner 進一步處理
    public void EndTurn(CombatState s)
    {
        if (s is null) return;
        s.PhaseCtx.Step = PhaseStep.TurnEnd;
    }
}
