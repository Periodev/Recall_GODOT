using CombatCore;

namespace CombatCore.AI
{
    public enum ExecuteTiming { Mark, Action }

    public static class EnemyStrategy
    {
        /// <summary>
        /// 決定 Enemy Intent 的執行時機
        /// A (攻擊) → Action
        /// B (防禦), C (充能) → Mark
        /// </summary>
        public static ExecuteTiming DetermineTiming(Intent intent)
        {
            return intent switch
            {
                ActIntent ai when ai.Act.Op == HLAop.Attack => ExecuteTiming.Action,
                ActIntent ai when ai.Act.Op == HLAop.Block => ExecuteTiming.Mark,
                ActIntent ai when ai.Act.Op == HLAop.Charge => ExecuteTiming.Mark,
                _ => ExecuteTiming.Mark
            };
        }
    }
}