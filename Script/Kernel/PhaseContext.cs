
using System;
using CombatCore;

// Phase definition for combat phases in the game.
// Combat 系統用的 PhaseContext 定義，包含當前階段、回合數等資訊。
// Constant and context struct for managing combat phases.

namespace CombatCore
{
	public enum PhaseStep : byte
	{
		// === Enemy Phase (0x1X) ===
		EnemyInit = 0x10,
		EnemyIntent = 0x11,
		EnemyPlanning = 0x12,     // 🆕 新增：AI Intent → Commands 轉換階段
		EnemyExecInstant = 0x13,

		// === Player Phase (0x0X) ===
		PlayerInit = 0x00,
		PlayerDraw = 0x01,
		PlayerInput = 0x02,
		PlayerPlanning = 0x03,    // 🆕 新增：Intent → Commands 轉換階段
		PlayerExecute = 0x04,     // 🆕 修改：Commands → Execute 執行階段

		// === Enemy action Phase (0x1X) ===
		EnemyExecDelayed = 0x14,

		// === Turn Phase (0xFX) ===
		TurnStart = 0xF0,           // do nothing, just a marker
		TurnEnd = 0xF1,

		// === Default ===
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
		CombatEnd = 0xF
	}

	public struct PhaseContext
	{
		public PhaseStep Step;
		public int TurnNum;
		public bool RecallUsedThisTurn; // Track if Recall has been used this turn

		public HLAIntent PendingIntent;
		public TranslationResult? PendingTranslation; // 🆕 等待執行的轉換結果

		public void Init()
		{
			Step = PhaseStep.TurnStart;
			TurnNum = 0;
			RecallUsedThisTurn = false;
			PendingIntent = null;
			PendingTranslation = null;
		}

		// Method to reset turn-specific flags
		public void StartNewTurn()
		{
			TurnNum++;
			RecallUsedThisTurn = false;
			PendingIntent = null;
			PendingTranslation = null;
		}

		// Method to mark recall as used
		public void MarkRecallUsed()
		{
			RecallUsedThisTurn = true;
		}

		// HLA
		public void SetIntent(HLAIntent intent) => PendingIntent = intent;

		public bool TryConsumeIntent(out HLAIntent intent)
		{
			if (PendingIntent is null) { intent = null; return false; }
			intent = PendingIntent; PendingIntent = null; return true;
		}

		public bool HasPendingIntent => PendingIntent is not null;

		// InterOP
		public void SetTranslation(TranslationResult translation) => PendingTranslation = translation;

		public bool TryConsumeTranslation(out TranslationResult translation)
		{
			if (PendingTranslation is null) { translation = default; return false; }
			translation = PendingTranslation.Value; PendingTranslation = null; return true;
		}

		public bool HasPendingTranslation => PendingTranslation is not null;


		public void ClearPendingStates()
		{
			PendingIntent = null;
			PendingTranslation = null;
		}

	}
}
