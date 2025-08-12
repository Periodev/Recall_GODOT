# 🧠 HLA 系統架構與 InterOp 設計規範

## 📌 目標

將玩家或 AI 的高階行為（HLA）轉譯為可執行的最小命令序列（AtomicCmd），並透過清晰分層達成以下目標：

- 支援 Replay、Undo、AI、自動化測試
- 可擴充的技能與邏輯層
- 明確的命令執行流程與檢查機制

---

## 🧱 層級職責總覽

| 層級            | 職責說明                             | 輸入範例                                 | 輸出範例                     |
|------------------|----------------------------------------|--------------------------------------------|------------------------------|
| `HLA`            | 玩家意圖的參數化描述（非可執行）       | `Attack(player, enemy, 20)`                | `HLA struct`                 |
| `HLATranslator`  | 規則解釋與策略翻譯 → 行動中繼封裝       | `HLA`                                      | `InterOpCall`                |
| `InterOpCall`    | 包含 InterOps 方法與必要參數            | 內部封裝為 Method + Args                  | 可執行的 `.Execute()` 呼叫  |
| `InterOps`       | 核心邏輯調度，含事務檢查與命令組裝       | `InterOpCall.Execute()`                    | `bool success`               |
| `SelfOp`         | 角色操作封裝，轉 AtomicCmd 呼叫         | `ConsumeAP(actor, 1)`                      | `true/false`（或 Cmd 發出）  |
| `AtomicCmd`      | 最終狀態操作指令                       | `CutHP(target, 15)`                        | 遞交執行器                   |

---

## 🔁 執行流程範例

```csharp
// 1. UI 產生 HLA
var action = new HLA(HLAType.Attack, player, enemy, 20);

// 2. Translator 解析策略
var interOpCall = HLATranslator.Translate(action);

// 3. InterOps 執行事務邏輯
bool success = interOpCall.Execute();

// 4. 回饋結果給 UI / 戰鬥流程
if (success) { Animate(); } else { ShowError(); }
```

---

## ⚙️ InterOps 中的行為流程

以 `Attack` 為例：

```csharp
public static bool Attack(Actor attacker, Actor target, int damage, int apCost = 1)
{
    if (!SelfOp.ConsumeAP(attacker, apCost)) return false;
    DealDamage(attacker, target, damage);
    return true;
}
```

DealDamage 可進一步拆解為：

- `CutShieldCommand`
- `CutHPCommand`
- `RecordReverbCommand`

---

## ✅ 命名與語意選擇

| 名稱         | 原因與語意說明                           |
|--------------|--------------------------------------------|
| `InterOp`    | 表示「跨對象操作中介」，語意精確，與 Operation 系統一致 |
| `Interact`   | 語意偏 UX／輸入事件，與內核執行邏輯不符         |
| `InterOpCall`| 表示中繼封裝的行為呼叫（資料結構）                |
| `SelfOp`     | 表示針對單一 Actor 執行的內部操作封裝             |

---

## 🔖 補充建議

- `InterOpCall` 可序列化，便於 Replay 與檢查
- `AtomicCmd` 可插入 Reaction / Trigger 系統監聽
- `HLATranslator` 應保持 stateless，僅轉換規則 → 呼叫結構

---

## ✅ 最終語意層級圖

```
UI / AI
  ↓
HLA
  ↓
HLATranslator
  ↓
InterOpCall (封裝 method + args)
  ↓
InterOps (檢查 + 指令生成)
  ↓
AtomicCmd[]
  ↓
CommandExecutor
  ↓
Component 操作
```

---
