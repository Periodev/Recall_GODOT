
using System;
using System.Collections.Generic;
using CombatCore;
using CombatCore.InterOp;
using CombatCore.Abstractions;
using CombatCore.Command;
using CombatCore.Memory;

public abstract record HLAIntent(int? TargetId);
public sealed record BasicIntent(ActionType Act, int? TargetId) : HLAIntent(TargetId);
public sealed record RecallIntent(int[] RecallIndices, int? TargetId) : HLAIntent(TargetId);

public sealed class HLATranslator
{
	// 單一入口：輸入抽象意圖，型別模式分派
	public bool TryTranslate(
		HLAIntent intent,
		PhaseContext phase,
		IReadOnlyList<ActionType> mem,
		IActorLookup actors,
		Actor self,
		out BasicPlan basicPlan,
		out RecallPlan recallPlan,
		out string fail)
	{
		basicPlan = default; recallPlan = default; fail = string.Empty;

		if (!self.IsAlive) { fail = "self dead"; return false; }

		return intent switch
		{
			BasicIntent bi  => TryBasic(bi, phase, self, actors, out basicPlan, out fail),
			RecallIntent ri => TryRecall(ri, phase, mem, self, actors, out recallPlan, out fail),
			_ => Fail("unknown intent", out fail)
		};
	}

	private static bool TryBasic(
		BasicIntent bi, PhaseContext phase, Actor self, IActorLookup actors,
		out BasicPlan plan, out string fail)
	{
		plan = default; fail = string.Empty;

		var tgt = ResolveTarget(bi.TargetId, actors) ?? self;
		// 規則：AP/Phase/Charge 檢查（僅檢查，不扣資源）
		// 例：if (!self.HasAP(1)) return Fail("no ap", out fail);

		// 規則：計算最終數值（含 Charge Bonus → Damage/Block，並給出 ChargeCost）
		int damage = bi.Act == ActionType.A ? 5 : 0;
		int block  = bi.Act == ActionType.B ? 6 : 0;
		int gain   = bi.Act == ActionType.C ? 2 : 0;
		int chargeCost = (bi.Act is ActionType.A or ActionType.B) ? 1 : 0;
		int apCost = 1; // 由 Translator 決定成本

		plan = new BasicPlan(bi.Act, self, tgt, damage, block, chargeCost, gain, apCost);
		return true;
	}

	private static bool TryRecall(
		RecallIntent ri, PhaseContext phase, IReadOnlyList<ActionType> mem, Actor self, IActorLookup actors,
		out RecallPlan plan, out string fail)
	{
		plan = default; fail = string.Empty;

		var tgt = ResolveTarget(ri.TargetId, actors) ?? self;

		// 規則：回合限一次、索引合法、排除本回合等（僅檢查）
		// 例：if (!IndicesValid(ri.RecallIndices, mem)) return Fail("bad indices", out fail);

		// 規則：將 mem 中的記憶轉為項目與數值
		var items = new List<RecallItemPlan>();
		foreach (var idx in ri.RecallIndices)
		{
			// 示例：從 mem 抽象化 → 先放一個攻擊 5
			items.Add(new RecallItemPlan(ActionType.A, damage: 5));
		}

		// 規則：是否批次扣費
		int batchChargeCost = 0;
		int apCost = 1;

		plan = new RecallPlan(self, tgt, items, batchChargeCost, apCost);
		return true;
	}

	private static Actor? ResolveTarget(int? id, IActorLookup actors) =>
		id.HasValue ? actors.GetById(id.Value) : null;

	private static bool Fail(string msg, out string fail) { fail = msg; return false; }
}
