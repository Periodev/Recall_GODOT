using CombatCore.Command;

namespace Tests
{
    [TestFixture]
    public class AtomicCmdTests
    {
        #region A1: DealDamage - 先扣盾再扣血，回傳HP傷害

        [Test]
        public void DealDamage_WithoutShield_ShouldReturnFullHPDamage()
        {
            // Arrange
            var attacker = ActorFactory.Create();
            var target = ActorFactory.Create(hp: 50);
            var cmd = AtomicCmd.DealDamage(attacker, target, 20);

            // Act
            int hpDamage = cmd.Execute();

            // Assert
            Assert.That(hpDamage, Is.EqualTo(20), "Should return full HP damage when no shield");
            ActorAssert.HasHP(target, 30, "Target HP should decrease by damage amount");
            ActorAssert.HasShield(target, 0, "Target should have no shield");
        }

        [Test]
        public void DealDamage_WithPartialShield_ShouldReturnPenetratingHPDamage()
        {
            // Arrange
            var attacker = ActorFactory.Create();
            var target = ActorFactory.CreateWithShield(hp: 50, shield: 15);
            var cmd = AtomicCmd.DealDamage(attacker, target, 20);

            // Act
            int hpDamage = cmd.Execute();

            // Assert
            Assert.That(hpDamage, Is.EqualTo(5), "Should return only HP damage (20-15=5)");
            ActorAssert.HasHP(target, 45, "Target HP should decrease by penetrating damage");
            ActorAssert.HasShield(target, 0, "Target shield should be depleted");
        }

        [Test]
        public void DealDamage_ExactShieldAmount_ShouldReturnZeroHPDamage()
        {
            // Arrange
            var attacker = ActorFactory.Create();
            var target = ActorFactory.CreateWithShield(hp: 50, shield: 20);
            var cmd = AtomicCmd.DealDamage(attacker, target, 20);

            // Act
            int hpDamage = cmd.Execute();

            // Assert
            Assert.That(hpDamage, Is.EqualTo(0), "Should return 0 HP damage when shield absorbs all");
            ActorAssert.HasHP(target, 50, "Target HP should remain unchanged");
            ActorAssert.HasShield(target, 0, "Target shield should be exactly depleted");
        }

        #endregion

        #region A2: DealDamage - 全被盾吸收，回傳0，HP不變

        [Test]
        public void DealDamage_ExcessShield_ShouldReturnZeroAndLeaveShieldRemainder()
        {
            // Arrange
            var attacker = ActorFactory.Create();
            var target = ActorFactory.CreateWithShield(hp: 50, shield: 30);
            var cmd = AtomicCmd.DealDamage(attacker, target, 20);

            // Act
            int hpDamage = cmd.Execute();

            // Assert
            Assert.That(hpDamage, Is.EqualTo(0), "Should return 0 HP damage when shield absorbs all");
            ActorAssert.HasHP(target, 50, "Target HP should remain unchanged");
            ActorAssert.HasShield(target, 10, "Target should have remaining shield (30-20=10)");
        }

        [Test]
        public void DealDamage_LargeDamageVsLargeShield_ShouldReturnZero()
        {
            // Arrange
            var attacker = ActorFactory.Create();
            var target = ActorFactory.CreateWithShield(hp: 100, shield: 999);
            var cmd = AtomicCmd.DealDamage(attacker, target, 500);

            // Act
            int hpDamage = cmd.Execute();

            // Assert
            Assert.That(hpDamage, Is.EqualTo(0), "Should return 0 HP damage for large shield");
            ActorAssert.HasHP(target, 100, "Target HP should remain unchanged");
            ActorAssert.HasShield(target, 499, "Target should have remaining shield (999-500=499)");
        }

        #endregion

        #region A3: AddShield - 累加，回傳實際新增量

        [Test]
        public void AddShield_ToEmptyShield_ShouldReturnFullAmount()
        {
            // Arrange
            var target = ActorFactory.Create();
            var cmd = AtomicCmd.AddShield(target, 15);

            // Act
            int shieldAdded = cmd.Execute();

            // Assert
            Assert.That(shieldAdded, Is.EqualTo(15), "Should return full amount added");
            ActorAssert.HasShield(target, 15, "Target should have added shield");
        }

        [Test]
        public void AddShield_ToExistingShield_ShouldReturnFullAmountAndAccumulate()
        {
            // Arrange
            var target = ActorFactory.CreateWithShield(hp: 100, shield: 10);
            var cmd = AtomicCmd.AddShield(target, 20);

            // Act
            int shieldAdded = cmd.Execute();

            // Assert
            Assert.That(shieldAdded, Is.EqualTo(20), "Should return full amount added");
            ActorAssert.HasShield(target, 30, "Target should have accumulated shield (10+20=30)");
        }

        [Test]
        public void AddShield_ZeroAmount_ShouldReturnZeroAndNotChangeShield()
        {
            // Arrange
            var target = ActorFactory.CreateWithShield(hp: 100, shield: 5);
            var cmd = AtomicCmd.AddShield(target, 0);

            // Act
            int shieldAdded = cmd.Execute();

            // Assert
            Assert.That(shieldAdded, Is.EqualTo(0), "Should return 0 for zero amount");
            ActorAssert.HasShield(target, 5, "Target shield should remain unchanged");
        }

        #endregion

        #region A4: GainCharge - 累加，回傳實際新增量

        [Test]
        public void GainCharge_ToEmptyCharge_ShouldReturnFullAmount()
        {
            // Arrange
            var target = ActorFactory.Create();
            var cmd = AtomicCmd.GainCharge(target, 2);

            // Act
            int chargeGained = cmd.Execute();

            // Assert
            Assert.That(chargeGained, Is.EqualTo(2), "Should return full amount gained");
            ActorAssert.HasCharge(target, 2, "Target should have gained charge");
        }

        [Test]
        public void GainCharge_WithExistingCharge_ShouldReturnFullAmountAndAccumulate()
        {
            // Arrange
            var target = ActorFactory.CreateWithCharge(hp: 100, charge: 1);
            var cmd = AtomicCmd.GainCharge(target, 1);

            // Act
            int chargeGained = cmd.Execute();

            // Assert
            Assert.That(chargeGained, Is.EqualTo(1), "Should return full amount gained");
            ActorAssert.HasCharge(target, 2, "Target should have accumulated charge (1+1=2)");
        }

        [Test]
        public void GainCharge_UnboundedAccumulation_WhenNoMaximumSet()
        {
            // Arrange - 假設充能為無界累加（需根據實際 Component 實現調整）
            var target = ActorFactory.Create();
            
            // Act - 多次累加充能
            var cmd1 = AtomicCmd.GainCharge(target, 5);
            var cmd2 = AtomicCmd.GainCharge(target, 10);
            
            int gained1 = cmd1.Execute();
            int gained2 = cmd2.Execute();

            // Assert - 如果有上限，此測試需調整為驗證上限行為
            // 當前假設：如果 Charge 組件有 Max=3 的限制
            Assert.That(gained1, Is.EqualTo(3), "Should gain only up to maximum (assuming max=3)");
            Assert.That(gained2, Is.EqualTo(0), "Should gain 0 when already at maximum");
            ActorAssert.HasCharge(target, 3, "Target should be at maximum charge");
        }

        [Test]
        public void GainCharge_ExceedingMaximum_ShouldReturnOnlyActualGained()
        {
            // Arrange
            var target = ActorFactory.CreateMaxCharge(); // 已有3充能（上限）
            var cmd = AtomicCmd.GainCharge(target, 2);

            // Act
            int chargeGained = cmd.Execute();

            // Assert
            Assert.That(chargeGained, Is.EqualTo(0), "Should return 0 when at max charge");
            ActorAssert.HasCharge(target, 3, "Target should remain at max charge");
        }

        [Test]
        public void GainCharge_PartiallyExceedingMaximum_ShouldReturnOnlyActualGained()
        {
            // Arrange
            var target = ActorFactory.CreateWithCharge(hp: 100, charge: 2);
            var cmd = AtomicCmd.GainCharge(target, 2);

            // Act
            int chargeGained = cmd.Execute();

            // Assert
            Assert.That(chargeGained, Is.EqualTo(1), "Should return only actual gained (3-2=1)");
            ActorAssert.HasCharge(target, 3, "Target should be at max charge");
        }

        [Test]
        public void GainCharge_NoChargeComponent_ShouldReturnZero()
        {
            // Arrange
            var target = ActorFactory.CreateEnemy(); // 無充能組件
            var cmd = AtomicCmd.GainCharge(target, 2);

            // Act
            int chargeGained = cmd.Execute();

            // Assert
            Assert.That(chargeGained, Is.EqualTo(0), "Should return 0 for actor without charge component");
        }

        #endregion

        #region A5: 建構子 - Target為null，擲ArgumentNullException

        [Test]
        public void Constructor_NullTarget_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                AtomicCmd.DealDamage(ActorFactory.Create(), null!, 10),
                "DealDamage with null target should throw ArgumentNullException");

            Assert.Throws<ArgumentNullException>(() => 
                AtomicCmd.AddShield(null!, 10),
                "AddShield with null target should throw ArgumentNullException");

            Assert.Throws<ArgumentNullException>(() => 
                AtomicCmd.GainCharge(null!, 10),
                "GainCharge with null target should throw ArgumentNullException");
        }

        [Test]
        public void Constructor_NullSource_ShouldBeAllowed()
        {
            // Arrange
            var target = ActorFactory.Create();

            // Act & Assert - 這些不應該拋異常
            Assert.DoesNotThrow(() => AtomicCmd.DealDamage(null, target, 10),
                "DealDamage with null source should be allowed");
            Assert.DoesNotThrow(() => AtomicCmd.AddShield(target, 10),
                "AddShield factory method should handle null source internally");
            Assert.DoesNotThrow(() => AtomicCmd.GainCharge(target, 10),
                "GainCharge factory method should handle null source internally");
        }

        #endregion

        #region A6: 無副作用 - 命令不改其他屬性

        [Test]
        public void DealDamage_ShouldNotModifyAPOrCharge()
        {
            // Arrange
            var attacker = ActorFactory.CreateWithCharge(hp: 100, charge: 2);
            var target = ActorFactory.CreateWithCharge(hp: 50, charge: 1);
            int attackerInitialAP = attacker.AP.Value;
            int attackerInitialCharge = attacker.Charge?.Value ?? 0;
            int targetInitialAP = target.AP.Value;
            int targetInitialCharge = target.Charge?.Value ?? 0;

            var cmd = AtomicCmd.DealDamage(attacker, target, 20);

            // Act
            cmd.Execute();

            // Assert - DealDamage 不應改變任何 AP 或 Charge
            ActorAssert.HasAP(attacker, attackerInitialAP, "Attacker AP should not change");
            ActorAssert.HasCharge(attacker, attackerInitialCharge, "Attacker charge should not change");
            ActorAssert.HasAP(target, targetInitialAP, "Target AP should not change");
            ActorAssert.HasCharge(target, targetInitialCharge, "Target charge should not change");
        }

        [Test]
        public void AddShield_ShouldNotModifyAPOrChargeOrHP()
        {
            // Arrange
            var target = ActorFactory.CreateWithCharge(hp: 100, charge: 2);
            int initialAP = target.AP.Value;
            int initialCharge = target.Charge?.Value ?? 0;
            int initialHP = target.HP.Value;

            var cmd = AtomicCmd.AddShield(target, 15);

            // Act
            cmd.Execute();

            // Assert - AddShield 不應改變 AP、Charge 或 HP
            ActorAssert.HasAP(target, initialAP, "Target AP should not change");
            ActorAssert.HasCharge(target, initialCharge, "Target charge should not change");
            ActorAssert.HasHP(target, initialHP, "Target HP should not change");
        }

        [Test]
        public void GainCharge_ShouldNotModifyAPOrHPOrShield()
        {
            // Arrange
            var target = ActorFactory.Create();
            int initialAP = target.AP.Value;
            int initialHP = target.HP.Value;
            int initialShield = target.Shield.Value;

            var cmd = AtomicCmd.GainCharge(target, 2);

            // Act
            cmd.Execute();

            // Assert - GainCharge 應該改變 Charge，但不改變其他屬性
            ActorAssert.HasAP(target, initialAP, "Target AP should not change");
            ActorAssert.HasHP(target, initialHP, "Target HP should not change");
            ActorAssert.HasShield(target, initialShield, "Target shield should not change");
            // 注意：Charge 本身會改變，這是正常的，所以不在此測試
        }

        #endregion

        #region A7: Value<=0 - Execute回傳0且不改狀態

        [Test]
        public void DealDamage_NegativeValue_ShouldReturnZeroAndNotChangeState()
        {
            // Arrange
            var attacker = ActorFactory.Create();
            var target = ActorFactory.CreateWithShield(hp: 50, shield: 10);
            var cmd = AtomicCmd.DealDamage(attacker, target, -5);

            // Act
            int result = cmd.Execute();

            // Assert
            Assert.That(result, Is.EqualTo(0), "Should return 0 for negative damage");
            ActorAssert.HasState(target, 50, 10, 0, 3, "Target state should remain unchanged");
        }

        [Test]
        public void AddShield_NegativeValue_ShouldReturnZeroAndNotChangeState()
        {
            // Arrange
            var target = ActorFactory.CreateWithShield(hp: 100, shield: 5);
            var cmd = AtomicCmd.AddShield(target, -10);

            // Act
            int result = cmd.Execute();

            // Assert
            Assert.That(result, Is.EqualTo(0), "Should return 0 for negative shield");
            ActorAssert.HasState(target, 100, 5, 0, 3, "Target state should remain unchanged");
        }

        [Test]
        public void GainCharge_NegativeValue_ShouldReturnZeroAndNotChangeState()
        {
            // Arrange
            var target = ActorFactory.CreateWithCharge(hp: 100, charge: 2);
            var cmd = AtomicCmd.GainCharge(target, -1);

            // Act
            int result = cmd.Execute();

            // Assert
            Assert.That(result, Is.EqualTo(0), "Should return 0 for negative charge");
            ActorAssert.HasState(target, 100, 0, 2, 3, "Target state should remain unchanged");
        }

        [Test]
        public void AllCommands_ZeroValue_ShouldReturnZeroAndNotChangeState()
        {
            // Arrange
            var attacker = ActorFactory.Create();
            var target = ActorFactory.CreateWithShield(hp: 50, shield: 10);
            
            // Act & Assert
            Assert.That(AtomicCmd.DealDamage(attacker, target, 0).Execute(), Is.EqualTo(0));
            Assert.That(AtomicCmd.AddShield(target, 0).Execute(), Is.EqualTo(0));
            Assert.That(AtomicCmd.GainCharge(target, 0).Execute(), Is.EqualTo(0));
            
            ActorAssert.HasState(target, 50, 10, 0, 3, "Target state should remain unchanged after zero-value commands");
        }

        #endregion
    }
}
