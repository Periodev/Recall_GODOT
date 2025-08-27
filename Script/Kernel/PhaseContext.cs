
using System;
using CombatCore;

namespace CombatCore
{
	public enum PhaseStep : byte
	{
		// === Player Phase (0x0X) ===
		PlayerInit = 0x00,
		PlayerDraw = 0x01,
		PlayerInput = 0x02,
		PlayerPlanning = 0x03,    // 🆕 新增
		PlayerExecute = 0x04,     // 🆕 新增

		// === Enemy Phase (0x1X) ===
		EnemyInit = 0x10,
		EnemyIntent = 0x11,
		EnemyPlanning = 0x12,     // 🆕 新增
		EnemyExecInstant = 0x13,  // 🆕 修改編號
		EnemyExecDelayed = 0x14,  // 🆕 修改編號

		// === Turn Phase (0xFX) ===
		TurnStart = 0xF0,
		TurnEnd = 0xF1,
		CombatEnd = 0xFF
	}

	public enum PhaseType
	{
		Player = 0x0,
		Enemy = 0x1,
		Turn = 0xF,
	}

	public enum PhaseResult
	{
		Continue = 0x0,
		WaitInput = 0x1,
		Pending = 0x2,
		Interrupt = 0x3,
		PhaseLocked = 0x4,
		CombatEnd = 0xF,
	}

	/// PhaseResult 的擴展方法
	public static class PhaseResultExtensions
	{
		/// 檢查是否為服務請求類型
		public static bool IsServiceRequest(this PhaseResult result)
		{
			return (byte)result >= 0x10 && (byte)result <= 0x2F;
		}

		/// 檢查是否為流程控制類型
		public static bool IsFlowControl(this PhaseResult result)
		{
			return (byte)result <= 0x0F;
		}
	}


	public struct PhaseContext
	{
		public PhaseStep Step;
		public int TurnNum;
		public bool RecallUsedThisTurn;
		public HLAIntent PendingIntent; // 保留給 Enemy 使用

		public void Init()
		{
			Step = PhaseStep.TurnStart;
			TurnNum = 0;
			RecallUsedThisTurn = false;
			PendingIntent = null;
		}

		public void StartNewTurn()
		{
			TurnNum++;
			RecallUsedThisTurn = false;
			PendingIntent = null;
		}

		public void MarkRecallUsed()
		{
			RecallUsedThisTurn = true;
		}

		// Enemy Intent 管理方法（保留給 Enemy 使用）
		public void SetIntent(HLAIntent intent) => PendingIntent = intent;

		public bool TryConsumeIntent(out HLAIntent intent)
		{
			if (PendingIntent is null) { intent = null; return false; }
			intent = PendingIntent; PendingIntent = null; return true;
		}

		public bool HasPendingIntent => PendingIntent is not null;
	}
}
