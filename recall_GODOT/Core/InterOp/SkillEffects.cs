using System;
using CombatCore;
using CombatCore.Command;
using static CombatCore.GameConst;

namespace CombatCore.InterOp
{
    public static class SkillEffects
    {
        /// <summary>
        /// 根據 RecipeId 建構技能效果命令
        /// </summary>
        public static AtomicCmd[] BuildSkillCommands(int recipeId, Actor source, Actor target)
        {
            return recipeId switch
            {
                111 => DoubleStrike(source, target),
                _ => throw new NotImplementedException($"Skill effect for RecipeId {recipeId} not implemented")
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