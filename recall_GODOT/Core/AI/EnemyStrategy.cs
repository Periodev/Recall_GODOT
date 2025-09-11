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
                EchoIntent ei when ei.Echo.Op == HLAop.Attack => ExecuteTiming.Action,
                EchoIntent ei when ei.Echo.Op == HLAop.Block => ExecuteTiming.Mark,
                EchoIntent ei when ei.Echo.Op == HLAop.Charge => ExecuteTiming.Mark,
                _ => ExecuteTiming.Mark
            };
        }
    }
}