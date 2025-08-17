
using System;
using CombatCore;

// Phase definition for combat phases in the game.
// Combat 系統用的 PhaseContext 定義，包含當前階段、回合數等資訊。
// Constant and context struct for managing combat phases.

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
		TurnEnd  = 0xF1,
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
		public bool RecallUsedThisTurn;
		public HLAIntent PendingIntent;
		public TranslationResult? PendingTranslation; // 🆕 新增

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
			PendingTranslation = null; // 🆕 清理轉換結果
		}

		// Method to mark recall as used
		public void MarkRecallUsed()
		{
			RecallUsedThisTurn = true;
		}

		// Intent 管理
		public void SetIntent(HLAIntent intent) => PendingIntent = intent;
		
		public bool TryConsumeIntent(out HLAIntent intent)
		{
			if (PendingIntent is null) { intent = null; return false; }
			intent = PendingIntent; PendingIntent = null; return true;
		}

		// 🆕 TranslationResult 管理
		public void SetTranslation(TranslationResult translation)
		{
			PendingTranslation = translation;
		}

		public bool TryConsumeTranslation(out TranslationResult translation)
		{
			if (PendingTranslation is null) 
			{ 
				translation = default; 
				return false; 
			}
			translation = PendingTranslation.Value; 
			PendingTranslation = null; 
			return true;
		}

		public bool HasPendingTranslation => PendingTranslation.HasValue;
		public bool HasPendingIntent => PendingIntent is not null;
	}
}