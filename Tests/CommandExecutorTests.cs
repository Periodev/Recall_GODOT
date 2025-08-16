using CombatCore.Command;

namespace Tests
{
    [TestFixture]
    public class CommandExecutorTests
    {
        private CmdExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            _executor = new CmdExecutor();
        }

        #region B1: ExecuteAll - 按輸入順序影響狀態

        [Test]
        public void ExecuteAll_SequentialExecution_ShouldAffectStateInOrder()
        {
            // Arrange
            var target = ActorFactory.Create(hp: 100);
            
            var commands = new List<AtomicCmd>
            {
                AtomicCmd.AddShield(target, 20),        // 先加護盾
                AtomicCmd.DealDamage(target, target, 15), // 再攻擊（應被護盾吸收）
                AtomicCmd.GainCharge(target, 2)         // 最後加充能
            };

            // Act
            CmdLog log = _executor.ExecuteAll(commands);

            // Assert
            Assert.That(log.Count, Is.EqualTo(3), "Should execute all 3 commands");
            
            // 驗證最終狀態：HP應該不變（被護盾吸收），護盾剩5，充能為2
            ActorAssert.HasHP(target, 100, "HP should remain full (damage absorbed by shield)");
            ActorAssert.HasShield(target, 5, "Shield should remain after absorbing damage (20-15=5)");
            ActorAssert.HasCharge(target, 2, "Charge should be gained");
        }

        [Test]
        public void ExecuteAll_OrderMatters_DifferentOrderDifferentResult()
        {
            // Arrange - 兩個相同的目標
            var target1 = ActorFactory.Create(hp: 50);
            var target2 = ActorFactory.Create(hp: 50);

            // 順序1: 先攻擊再加護盾
            var commands1 = new List<AtomicCmd>
            {
                AtomicCmd.DealDamage(target1, target1, 20), // 直接扣血
                AtomicCmd.AddShield(target1, 15)           // 後加護盾
            };

            // 順序2: 先加護盾再攻擊
            var commands2 = new List<AtomicCmd>
            {
                AtomicCmd.AddShield(target2, 15),          // 先加護盾
                AtomicCmd.DealDamage(target2, target2, 20) // 護盾吸收部分傷害
            };

            // Act
            _executor.ExecuteAll(commands1);
            _executor.ExecuteAll(commands2);

            // Assert - 兩者的最終狀態應該不同
            ActorAssert.HasHP(target1, 30, "Target1: HP should decrease by full damage (no shield protection)");
            ActorAssert.HasShield(target1, 15, "Target1: Shield should be added after damage");

            ActorAssert.HasHP(target2, 45, "Target2: HP should decrease by only penetrating damage (20-15=5)");
            ActorAssert.HasShield(target2, 0, "Target2: Shield should be depleted by damage");
        }

        [Test]
        public void ExecuteAll_MultipleCommandsOnSameTarget_ShouldAccumulate()
        {
            // Arrange
            var target = ActorFactory.Create(hp: 100);
            
            var commands = new List<AtomicCmd>
            {
                AtomicCmd.AddShield(target, 10),
                AtomicCmd.AddShield(target, 15), // 累積護盾
                AtomicCmd.GainCharge(target, 1),
                AtomicCmd.GainCharge(target, 1), // 累積充能
                AtomicCmd.DealDamage(target, target, 30) // 攻擊累積的護盾
            };

            // Act
            CmdLog log = _executor.ExecuteAll(commands);

            // Assert
            Assert.That(log.Count, Is.EqualTo(5), "Should execute all 5 commands");
            ActorAssert.HasHP(target, 95, "HP should decrease by penetrating damage (30-25=5)");
            ActorAssert.HasShield(target, 0, "Shield should be depleted (25 absorbed 30 damage)");
            ActorAssert.HasCharge(target, 2, "Charge should accumulate (1+1=2)");
        }

        #endregion

        #region B2: ExecuteAll - 記錄筆數與各Entry.Delta與狀態一致

        [Test]
        public void ExecuteAll_LogCountShouldMatchCommandCount()
        {
            // Arrange
            var target = ActorFactory.Create();
            var commands = new List<AtomicCmd>
            {
                AtomicCmd.AddShield(target, 10),
                AtomicCmd.GainCharge(target, 2),
                AtomicCmd.DealDamage(target, target, 5)
            };

            // Act
            CmdLog log = _executor.ExecuteAll(commands);

            // Assert
            Assert.That(log.Count, Is.EqualTo(3), "Log count should match command count");
            Assert.That(log.Items.Count, Is.EqualTo(3), "Items count should match command count");
        }

        [Test]
        public void ExecuteAll_EntryDeltasShouldMatchActualEffects()
        {
            // Arrange
            var target = ActorFactory.CreateWithShield(hp: 100, shield: 10);
            
            var commands = new List<AtomicCmd>
            {
                AtomicCmd.AddShield(target, 15),        // 應該+15護盾
                AtomicCmd.DealDamage(target, target, 30), // 應該造成5點HP傷害 (30-25=5)
                AtomicCmd.GainCharge(target, 2)         // 應該+2充能
            };

            // Act
            CmdLog log = _executor.ExecuteAll(commands);

            // Assert
            var entries = log.Items.ToList();
            
            Assert.That(entries[0].Delta, Is.EqualTo(15), "First entry should show +15 shield");
            Assert.That(entries[1].Delta, Is.EqualTo(5), "Second entry should show 5 HP damage");
            Assert.That(entries[2].Delta, Is.EqualTo(2), "Third entry should show +2 charge");

            // 驗證命令記錄
            Assert.That(entries[0].Cmd.Type, Is.EqualTo(CmdType.AddShield));
            Assert.That(entries[1].Cmd.Type, Is.EqualTo(CmdType.DealDamage));
            Assert.That(entries[2].Cmd.Type, Is.EqualTo(CmdType.GainCharge));
        }

        [Test]
        public void ExecuteAll_ZeroEffectCommands_ShouldRecordZeroDelta()
        {
            // Arrange
            var target = ActorFactory.CreateMaxCharge(); // 滿充能
            
            var commands = new List<AtomicCmd>
            {
                AtomicCmd.GainCharge(target, 5),        // 無法增加（已滿）
                AtomicCmd.DealDamage(target, target, 0), // 零傷害
                AtomicCmd.AddShield(target, -5)         // 負數（無效）
            };

            // Act
            CmdLog log = _executor.ExecuteAll(commands);

            // Assert
            var entries = log.Items.ToList();
            Assert.That(entries[0].Delta, Is.EqualTo(0), "Max charge command should record 0 delta");
            Assert.That(entries[1].Delta, Is.EqualTo(0), "Zero damage command should record 0 delta");
            Assert.That(entries[2].Delta, Is.EqualTo(0), "Negative shield command should record 0 delta");
        }

        [Test]
        public void ExecuteAll_ComplexScenario_LogShouldMatchStateChanges()
        {
            // Arrange
            var player = ActorFactory.Create(hp: 50);
            var enemy = ActorFactory.CreateEnemy(hp: 30);
            
            var commands = new List<AtomicCmd>
            {
                AtomicCmd.AddShield(player, 20),        // Player +20 shield
                AtomicCmd.GainCharge(player, 3),        // Player +3 charge (limited to max)
                AtomicCmd.DealDamage(player, enemy, 25), // Enemy -25 HP
                AtomicCmd.DealDamage(enemy, player, 15), // Player damage absorbed by shield
                AtomicCmd.GainCharge(enemy, 1)          // Enemy has no charge component
            };

            // Act
            CmdLog log = _executor.ExecuteAll(commands);

            // Assert
            var entries = log.Items.ToList();
            Assert.That(entries[0].Delta, Is.EqualTo(20), "Player should gain 20 shield");
            Assert.That(entries[1].Delta, Is.EqualTo(3), "Player should gain 3 charge (up to max)");
            Assert.That(entries[2].Delta, Is.EqualTo(25), "Enemy should lose 25 HP");
            Assert.That(entries[3].Delta, Is.EqualTo(0), "Player should take 0 HP damage (shield absorbed)");
            Assert.That(entries[4].Delta, Is.EqualTo(0), "Enemy should gain 0 charge (no component)");

            // Verify final states match log implications
            ActorAssert.HasHP(player, 50, "Player HP should be unchanged");
            ActorAssert.HasShield(player, 5, "Player shield should have 5 remaining (20-15=5)");
            ActorAssert.HasCharge(player, 3, "Player should have max charge");
            ActorAssert.HasHP(enemy, 5, "Enemy HP should be 5 (30-25=5)");
        }

        #endregion

        #region B3: ExecuteAll - cmds為null，擲ArgumentNullException

        [Test]
        public void ExecuteAll_NullCommands_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _executor.ExecuteAll(null!), 
                "ExecuteAll with null commands should throw ArgumentNullException");
        }

        [Test]
        public void ExecuteAll_EmptyCommands_ShouldReturnEmptyLog()
        {
            // Arrange
            var emptyCommands = new List<AtomicCmd>();

            // Act
            CmdLog log = _executor.ExecuteAll(emptyCommands);

            // Assert
            Assert.That(log.Count, Is.EqualTo(0), "Empty command list should result in empty log");
            Assert.That(log.Items, Is.Empty, "Log items should be empty");
        }

        #endregion

        #region B4: Log - ToString或簡易Inspect不觸發副作用

        [Test]
        public void Log_MultipleReadsAndInspections_ShouldNotAffectState()
        {
            // Arrange
            var target = ActorFactory.Create(hp: 100);
            var commands = new List<AtomicCmd>
            {
                AtomicCmd.AddShield(target, 10),
                AtomicCmd.DealDamage(target, target, 5)
            };

            CmdLog log = _executor.ExecuteAll(commands);
            
            // 記錄執行後的狀態
            int hpAfterExecution = target.HP.Value;
            int shieldAfterExecution = target.Shield.Value;
            int chargeAfterExecution = target.Charge?.Value ?? 0;
            int apAfterExecution = target.AP.Value;

            // Act - 多次讀取和檢查日誌（驗證重複讀取無副作用）
            for (int i = 0; i < 5; i++)
            {
                _ = log.Count;
                _ = log.Items.Count;
                
                foreach (var entry in log.Items)
                {
                    _ = entry.Cmd.ToString();
                    _ = entry.Delta;
                    _ = entry.Cmd.Type;
                }
            }

            // Assert - 狀態應該完全不變
            ActorAssert.HasHP(target, hpAfterExecution, "HP should not change after log inspections");
            ActorAssert.HasShield(target, shieldAfterExecution, "Shield should not change after log inspections");
            ActorAssert.HasCharge(target, chargeAfterExecution, "Charge should not change after log inspections");
            ActorAssert.HasAP(target, apAfterExecution, "AP should not change after log inspections");
        }

        [Test]
        public void Log_EntriesAreReadOnly_CannotModifyOriginalCommands()
        {
            // Arrange
            var target = ActorFactory.Create();
            var originalCommand = AtomicCmd.AddShield(target, 15);
            var commands = new List<AtomicCmd> { originalCommand };

            // Act
            CmdLog log = _executor.ExecuteAll(commands);
            var loggedEntry = log.Items.First();

            // Assert - 日誌提供唯讀視圖，不會影響原始狀態
            Assert.That(loggedEntry.Cmd.Type, Is.EqualTo(originalCommand.Type));
            Assert.That(loggedEntry.Cmd.Value, Is.EqualTo(originalCommand.Value));
            Assert.That(loggedEntry.Cmd.Target, Is.EqualTo(originalCommand.Target));
            
            // 多次訪問同一日誌項目不會產生副作用
            for (int i = 0; i < 3; i++)
            {
                Assert.That(loggedEntry.Delta, Is.EqualTo(15), "Delta should remain consistent across reads");
            }
        }

        #endregion

        #region 額外的邊界值測試

        [Test]
        public void ExecuteAll_LargeCommandSequence_ShouldHandleEfficiently()
        {
            // Arrange
            var target = ActorFactory.Create(hp: 1000);
            var commands = new List<AtomicCmd>();

            // 創建大量命令 - 修正邏輯：先累積護盾再攻擊
            for (int i = 0; i < 50; i++)
            {
                commands.Add(AtomicCmd.AddShield(target, 2)); // +2 護盾
                commands.Add(AtomicCmd.GainCharge(target, 1)); // +1 充能（有上限）
            }
            // 最後一次攻擊測試累積的護盾
            commands.Add(AtomicCmd.DealDamage(target, target, 50)); // 攻擊累積的護盾

            // Act
            CmdLog log = _executor.ExecuteAll(commands);

            // Assert
            Assert.That(log.Count, Is.EqualTo(101), "Should process all 101 commands");
            
            // 驗證最終狀態：50次+2護盾=100護盾，最後50點攻擊被完全吸收
            ActorAssert.HasHP(target, 1000, "HP should remain unchanged (all damage absorbed)");
            ActorAssert.HasShield(target, 50, "Shield should have remainder (100-50=50)");
            ActorAssert.HasCharge(target, 3, "Charge should be at maximum");
        }

        [Test]
        public void ExecuteAll_MixedValidAndInvalidCommands_ShouldProcessAll()
        {
            // Arrange
            var target = ActorFactory.CreateMaxCharge(hp: 50);
            
            var commands = new List<AtomicCmd>
            {
                AtomicCmd.AddShield(target, 10),        // 有效：+10護盾
                AtomicCmd.DealDamage(target, target, 0), // 無效：零傷害
                AtomicCmd.GainCharge(target, 5),        // 無效：已滿充能
                AtomicCmd.AddShield(target, -5),        // 無效：負數
                AtomicCmd.DealDamage(target, target, 15) // 有效：15傷害，10被護盾吸收，5穿透到HP
            };

            // Act
            CmdLog log = _executor.ExecuteAll(commands);

            // Assert
            Assert.That(log.Count, Is.EqualTo(5), "Should process all commands including invalid ones");
            
            var deltas = log.Items.Select(e => e.Delta).ToList();
            // 修正期望值：DealDamage 只返回 HP 實際傷害（不含護盾吸收）
            Assert.That(deltas, Is.EqualTo(new[] { 10, 0, 0, 0, 5 }), 
                "Should record correct deltas: +10 shield, 0 damage, 0 charge, 0 shield, 5 HP damage");
                
            // 驗證最終狀態
            ActorAssert.HasHP(target, 45, "HP should decrease by penetrating damage (50-5=45)");
            ActorAssert.HasShield(target, 0, "Shield should be depleted by damage");
        }

        #endregion
    }
}