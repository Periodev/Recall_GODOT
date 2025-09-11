using CombatCore;

namespace CombatCore.AI
{
    public enum ExecuteTiming { Instant, Delayed }

    public static class EnemyStrategy
    {
        /// <summary>
        /// 決定 Enemy Intent 的執行時機
        /// A (攻擊) → Delayed
        /// B (防禦), C (充能) → Instant
        /// </summary>
        public static ExecuteTiming DetermineTiming(Intent intent)
        {
            return intent switch
            {
                EchoIntent ei when ei.Echo.Op == HLAop.Attack => ExecuteTiming.Delayed,
                EchoIntent ei when ei.Echo.Op == HLAop.Block => ExecuteTiming.Instant,
                EchoIntent ei when ei.Echo.Op == HLAop.Charge => ExecuteTiming.Instant,
                _ => ExecuteTiming.Instant
            };
        }
    }
}