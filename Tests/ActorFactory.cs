using CombatCore;

namespace Tests
{
    /// <summary>
    /// 測試用 Actor 工廠，提供快速創建各種狀態的 Actor
    /// 純 C# 實現，不依賴 Godot
    /// </summary>
    internal static class ActorFactory
    {
        /// <summary>
        /// 創建標準測試 Actor
        /// </summary>
        public static Actor Create(int hp = 100, int apPerTurn = 3, bool withCharge = true)
        {
            return new Actor(hp, apPerTurn, withCharge);
        }

        /// <summary>
        /// 創建有護盾的 Actor
        /// </summary>
        public static Actor CreateWithShield(int hp = 100, int shield = 10, bool withCharge = true)
        {
            var actor = new Actor(hp, 3, withCharge);
            actor.Shield.Add(shield);
            return actor;
        }

        /// <summary>
        /// 創建有充能的 Actor
        /// </summary>
        public static Actor CreateWithCharge(int hp = 100, int charge = 2)
        {
            var actor = new Actor(hp, 3, true);
            actor.Charge?.Add(charge);
            return actor;
        }

        /// <summary>
        /// 創建無充能的 Actor（如敵人）
        /// </summary>
        public static Actor CreateEnemy(int hp = 50)
        {
            return new Actor(hp, 2, false);
        }

        /// <summary>
        /// 創建低血量 Actor
        /// </summary>
        public static Actor CreateLowHP(int hp = 5)
        {
            return new Actor(hp, 3, true);
        }

        /// <summary>
        /// 創建滿充能 Actor
        /// </summary>
        public static Actor CreateMaxCharge(int hp = 100)
        {
            var actor = new Actor(hp, 3, true);
            actor.Charge?.Add(3); // 填滿到上限
            return actor;
        }
    }

    /// <summary>
    /// 測試用斷言輔助工具
    /// </summary>
    internal static class ActorAssert
    {
        /// <summary>
        /// 驗證 Actor 的 HP 值
        /// </summary>
        public static void HasHP(Actor actor, int expectedHP, string message = "")
        {
            Assert.That(actor.HP.Value, Is.EqualTo(expectedHP), 
                $"Expected HP {expectedHP}, but was {actor.HP.Value}. {message}");
        }

        /// <summary>
        /// 驗證 Actor 的護盾值
        /// </summary>
        public static void HasShield(Actor actor, int expectedShield, string message = "")
        {
            Assert.That(actor.Shield.Value, Is.EqualTo(expectedShield), 
                $"Expected Shield {expectedShield}, but was {actor.Shield.Value}. {message}");
        }

        /// <summary>
        /// 驗證 Actor 的充能值
        /// </summary>
        public static void HasCharge(Actor actor, int expectedCharge, string message = "")
        {
            Assert.That(actor.Charge?.Value ?? 0, Is.EqualTo(expectedCharge), 
                $"Expected Charge {expectedCharge}, but was {actor.Charge?.Value ?? 0}. {message}");
        }

        /// <summary>
        /// 驗證 Actor 的 AP 值
        /// </summary>
        public static void HasAP(Actor actor, int expectedAP, string message = "")
        {
            Assert.That(actor.AP.Value, Is.EqualTo(expectedAP), 
                $"Expected AP {expectedAP}, but was {actor.AP.Value}. {message}");
        }

        /// <summary>
        /// 驗證 Actor 的完整狀態
        /// </summary>
        public static void HasState(Actor actor, int hp, int shield, int charge, int ap, string message = "")
        {
            HasHP(actor, hp, $"{message} - HP");
            HasShield(actor, shield, $"{message} - Shield");
            HasCharge(actor, charge, $"{message} - Charge");
            HasAP(actor, ap, $"{message} - AP");
        }
    }
}
