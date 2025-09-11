using System.Collections.Generic;
using CombatCore;

namespace CombatCore.UI
{
    public static class ErrorMessage
    {
        public static readonly IReadOnlyDictionary<FailCode, string> Map =
            new Dictionary<FailCode, string>
            {
                [FailCode.NoAP] = "行動點不足",
                [FailCode.NoCharge] = "充能不足",
                [FailCode.BadTarget] = "無效目標",
                [FailCode.RecallUsed] = "本回合已使用召回",
                [FailCode.IndexNotContiguous] = "序列必須連續",
                [FailCode.NoRecipe] = "無效序列",
                [FailCode.EchoCooldown] = "Echo冷卻中",
                [FailCode.EchoSlotsFull] = "Echo槽已滿",
                [FailCode.PhaseLocked] = "當前階段無法操作",
            };

        public static string Get(FailCode code)
            => Map.TryGetValue(code, out var s) ? s : "Unknown";
    }
}
