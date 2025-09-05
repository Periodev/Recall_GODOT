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
                BasicIntent bi when bi.Act == ActionType.A => ExecuteTiming.Delayed,
                BasicIntent bi when bi.Act == ActionType.B => ExecuteTiming.Instant,
                BasicIntent bi when bi.Act == ActionType.C => ExecuteTiming.Instant,
                _ => ExecuteTiming.Instant
            };
        }
    }
}