#if DEBUG
using Godot;
#endif
using System;
using CombatCore;


/// 敵人階段處理 - 純狀態轉換邏輯
/// 移除了所有 CombatPipeline 調用，只負責 Phase 狀態轉換

public static class EnemyPhase
{
    
    /// 敵人初始化階段
    public static PhaseResult Init(ref PhaseContext ctx)
    {
        ctx.Step = PhaseStep.EnemyIntent;
        return PhaseResult.Continue;
    }

    
    /// 敵人意圖階段 - 引用 AI 策略表，展示敵人下一步行動
    public static PhaseResult Intent(ref PhaseContext ctx)
    {
        // 檢查是否已從策略表生成 Intent
        if (ctx.HasPendingIntent)
        {
            // 已有 Intent（從策略表獲取），推進到計劃階段
            ctx.Step = PhaseStep.EnemyPlanning;
            return PhaseResult.Continue;
        }
        else
        {
            // 沒有 Intent，請求 AI 策略表查詢服務
            return PhaseResult.RequiresAI;
        }
    }

    
    /// 敵人計劃階段 - 將意圖轉換為具體執行計劃
    public static PhaseResult Planning(ref PhaseContext ctx)
    {
        // 此時應該已經有 Intent（從 Intent 階段獲得）
        if (ctx.HasPendingIntent)
        {
            // 有 Intent，請求 Pipeline 處理轉換
            return PhaseResult.RequiresPipeline;
        }
        else
        {
            // 異常狀況：沒有 Intent 就進入了 Planning
            // 回退到 Intent 階段重新生成
            ctx.Step = PhaseStep.EnemyIntent;
            return PhaseResult.Continue;
        }
    }

    
    /// 敵人即時執行階段 - 請求命令執行服務
    public static PhaseResult ExecInstant(ref PhaseContext ctx)
    {
        return PhaseResult.RequiresExecution;
    }
    
    /// 敵人延遲執行階段 - 處理延後效果
    public static PhaseResult ExecDelayed(ref PhaseContext ctx)
    {
        // TODO: 處理延遲效果（如持續傷害、狀態過期等）
        
        ctx.Step = PhaseStep.TurnEnd;
        return PhaseResult.Continue;
    }
}
