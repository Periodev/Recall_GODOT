// File: Tests/Factories/CombatStateFactory.cs
using CombatCore.Abstractions;

namespace Tests
{
    /// <summary>
    /// 測試用的 FakeCombatState，不依賴 Godot Node
    /// </summary>
    public sealed class FakeCombatState : IActorLookup
    {
        public Actor Player { get; private set; }
        public Actor Enemy { get; private set; }

        public FakeCombatState()
        {
            Player = new Actor(100);
            Enemy = new Actor(80);
        }

        /// <summary>
        /// 根據 ID 獲取 Actor
        /// </summary>
        public Actor GetActor(int id)
        {
            return id switch
            {
                0 => Player,
                1 => Enemy,
                _ => null
            };
        }
    }

    public static class CombatStateFactory
    {
        /// <summary>
        /// 建立可測的 FakeCombatState，並初始化 Player/Enemy 數值。
        /// </summary>
        public static FakeCombatState Create(
            int playerHP = 100, int playerAP = 3, int playerCharge = 0, int playerShield = 0,
            int enemyHP = 80, int enemyAP = 2, int enemyShield = 0)
        {
            var s = new FakeCombatState();
            
            // Player 設定
            s.Player.HP.Value = playerHP;
            s.Player.AP.Value = playerAP;
            s.Player.Shield.Value = playerShield;
            if (s.Player.Charge != null)
            {
                s.Player.Charge.Clear();
                if (playerCharge > 0) s.Player.Charge.Add(playerCharge);
            }
            
            // Enemy 設定
            s.Enemy.HP.Value = enemyHP;
            s.Enemy.AP.Value = enemyAP;
            s.Enemy.Shield.Value = enemyShield;
            if (s.Enemy.Charge != null) s.Enemy.Charge.Clear();
            
            return s;
        }
    }
}