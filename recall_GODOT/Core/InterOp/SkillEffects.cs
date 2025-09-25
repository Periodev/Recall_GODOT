using System;
using CombatCore;
using CombatCore.Command;
using static CombatCore.GameConst;

namespace CombatCore.InterOp
{
    /// <summary>
    /// 專門處理非基本操作的技能效果系統
    /// 基本操作 (Attack, Block, Charge) 在 Translator 中直接處理
    /// </summary>
    public static class SkillEffects
    {
        /// <summary>
        /// 根據 HLAop 建構技能效果命令
        /// </summary>
        public static AtomicCmd[] BuildSkillCommands(HLAop op, Actor source, Actor target)
        {
            return op switch
            {
                HLAop.DoubleStrike => DoubleStrike(source, target),
                _ => throw new NotImplementedException($"Skill effect for HLAop {op} not implemented")
            };
        }

        /// <summary>
        /// Double Strike: 連續攻擊兩次
        /// </summary>
        private static AtomicCmd[] DoubleStrike(Actor source, Actor target)
        {
            int damage = A_BASE_DMG;
            return new AtomicCmd[]
            {
                AtomicCmd.DealDamage(source, target, damage),
                AtomicCmd.DealDamage(source, target, damage)
            };
        }
    }
}