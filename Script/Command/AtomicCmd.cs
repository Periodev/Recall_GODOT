
using System;
using System.Diagnostics;
using CombatCore.ActorOp;

namespace CombatCore.Command
{
	public enum CmdType : byte
	{
		DealDamage = 0x01,
		AddShield = 0x02,
		GainCharge = 0x03, 
		ConsumeCharge = 0x04,
		ConsumeAP = 0x05     // ← 新增
	}

	/// <summary>
	/// 原子命令：最小不可分割的狀態操作單元
	/// 嚴格遵循「無隱性副作用」原則 - 不扣AP、不清Charge、不做致死觸發
	/// </summary>
	public readonly struct AtomicCmd
	{
		public readonly CmdType Type;
		public readonly Actor Source;
		public readonly Actor Target;
		public readonly int Value;

		public AtomicCmd(CmdType type, Actor source, Actor target, int value)
		{
			Type = type;
			Source = source;
			Target = target ?? throw new ArgumentNullException(nameof(target));
			Value = value;
		}

		// 便利工廠方法
		public static AtomicCmd DealDamage(Actor source, Actor target, int damage) =>
			new AtomicCmd(CmdType.DealDamage, source, target, damage);

		public static AtomicCmd AddShield(Actor target, int amount) =>
			new AtomicCmd(CmdType.AddShield, null, target, amount);

		public static AtomicCmd GainCharge(Actor target, int amount) =>
			new AtomicCmd(CmdType.GainCharge, null, target, amount);

		public static AtomicCmd ConsumeCharge(Actor target, int amount) =>
			new AtomicCmd(CmdType.ConsumeCharge, null, target, amount);

		public static AtomicCmd ConsumeAP(Actor src, int amount) =>
			new AtomicCmd(CmdType.ConsumeAP, src, src, amount);

		/// <summary>
		/// 執行命令並返回主要資源變動量
		/// DealDamage = HP損失量（不含護盾吸收）
		/// AddShield = 實際新增護盾量
		/// GainCharge = 實際新增充能量
		/// ConsumeCharge = 實際消耗的充能量
		/// ConsumeAP = 實際消耗的 AP
		/// </summary>
		/// <returns>主要資源的實際變動量</returns>
		public int Execute()
		{
			if (Value <= 0) return 0;

			return Type switch
			{
				CmdType.DealDamage => ExecuteDealDamage(),
				CmdType.AddShield => ExecuteAddShield(),
				CmdType.GainCharge => ExecuteGainCharge(),
				CmdType.ConsumeCharge => ExecuteConsumeCharge(),
				CmdType.ConsumeAP => ExecuteConsumeAP(),
				_ => 0
			};
		}


		// Self execute functions

		private int ExecuteDealDamage()
		{
			// 護盾優先吸收傷害
			int shieldAbsorbed = SelfOp.CutShield(Target, Value);
			int penetrating = Value - shieldAbsorbed;
			
			// 短路優化：如果護盾完全吸收，直接返回 0 HP傷害
			if (penetrating <= 0) return 0;
			
			// 計算實際 HP 傷害
			int hpDamage = SelfOp.CutHP(Target, penetrating);

#if DEBUG
			if (hpDamage > 0 || shieldAbsorbed > 0)
			{
				Debug.WriteLine($"[DealDamage] {GetSourceName()} → {GetTargetName()}: " +
							   $"shield absorbed {shieldAbsorbed}, HP damage {hpDamage}");
			}
#endif

			// 僅返回 HP 實際傷害量，語義清晰
			return hpDamage;
		}

		private int ExecuteAddShield()
		{
			int actualAdded = SelfOp.AddShield(Target, Value);

#if DEBUG
			if (actualAdded > 0)
			{
				Debug.WriteLine($"[AddShield] {GetTargetName()} gained {actualAdded} shield");
			}
#endif

			return actualAdded;
		}

		private int ExecuteGainCharge()
		{
			int actualGained = SelfOp.GainCharge(Target, Value);

#if DEBUG
			if (actualGained > 0)
			{
				Debug.WriteLine($"[GainCharge] {GetTargetName()} gained {actualGained} charge");
			}
#endif

			return actualGained;
		}

		private int ExecuteConsumeCharge()
		{
			int actualConsumed = SelfOp.ConsumeCharge(Target, Value) ? Value : 0 ;
#if DEBUG
			if (actualConsumed > 0)
			{
				Debug.WriteLine($"[ConsumeCharge] {GetTargetName()} consumed {actualConsumed} charge");
			}
#endif

			return actualConsumed;
		}

		private int ExecuteConsumeAP()
		{
			int actualConsumed = SelfOp.ConsumeAP(Source, Value) ? Value : throw new InvalidOperationException("AP not enough");
#if DEBUG
			if (actualConsumed > 0)
			{
				Debug.WriteLine($"[ConsumeAP] {GetSourceName()} consumed {actualConsumed} AP");
			}
#endif

			return actualConsumed;
		}


		// Debug functions
#if DEBUG
		private string GetSourceName() => Source?.GetType().Name ?? "Unknown";
		private string GetTargetName() => Target.GetType().Name;
#endif

		public override string ToString()
		{
#if DEBUG
			string sourceName = Source?.GetType().Name ?? "None";
			string targetName = Target.GetType().Name;
			return $"{Type}({sourceName} → {targetName}, {Value})";
#else
			return $"{Type}({Value})";
#endif
		}
	}
}
