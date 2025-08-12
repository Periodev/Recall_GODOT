🏗️ Recall 專案架構更新總結
📋 更新概述
從早期 C 風格設計演進到 OOP + Command 混合架構

🔄 主要變更
1. 移除 ActorOp 中介層設計
diff- // 之前：所有操作都通過 ActorOp
- ActorOp.Add<Shield>(actor, amount);
- ActorOp.Add<Charge>(actor, amount);

+ // 現在：區分 Self OP 和 Interact OP
+ actor.Shield.Add(amount);              // Self OP → Component
+ ActorOp.DealDamage(target, damage);    // Interact OP → ActorOp
2. 確定 AtomicCmd 最終架構
csharppublic struct AtomicCmd  
{
    // 數據層：完整的執行參數
    public CmdType Type { get; }
    public Actor Source { get; }
    public Actor Target { get; }
    public int Value { get; }
    
    // 執行層：自包含邏輯
    public void Execute()
    {
        // 根據 Type 解析參數並執行
        switch (Type) { /* ... */ }
    }
}
3. 建立清晰的操作分層
┌─────────────────┐
│   AtomicCmd     │ ← 命令層：類型識別 + 參數解析 + 邏輯分派
├─────────────────┤
│   ActorOp       │ ← 交互層：跨Actor複雜業務邏輯 (DealDamage等)
├─────────────────┤  
│   Component     │ ← 數據層：單Actor自身狀態操作 (Add/Cut/Clear)
└─────────────────┘

✅ 設計決策
Command 設計模式選擇

❌ 純 Function-based (序列化困難，類型辨識不足)
❌ 純 Data-based (靈活性不足)
✅ Data + Self-Execute (數據可追蹤 + 邏輯內聚)

類型辨識方案

❌ String-based (運行時錯誤風險)
❌ 複雜 OpCode (過度設計)
✅ 簡單 CmdType enum (類型安全 + 足夠表達力)

操作執行責任

❌ cmd.Execute() vs Phase.Execute(cmd) 二選一
✅ cmd 自執行 + 分層操作 (責任清晰 + 邏輯內聚)


🎯 核心原則確立
1. 最小複雜度原則

只實現當前需求，避免過度設計
Memory Timeline 用於流程追蹤，不需要精確重現

2. 職責分離原則

Self OP: Component 自己管理 (shield.Add())
Interact OP: ActorOp 處理交互 (DealDamage())

3. 實用主義原則

架構服務於遊戲需求，不追求理論完美
優先代碼清晰度和開發效率


🚀 後續發展
短期目標

 完成基礎 CmdType (Attack/Block/Charge/Heal)
 實現 Memory Timeline 基本記錄功能
 整合 Phase 系統執行命令

中期目標

 擴展更多命令類型
 實現 Echo 系統基礎版本
 完善戰鬥流程

長期架構

 卡牌系統整合
 複雜狀態效果支援
 Memory Timeline 深度分析功能