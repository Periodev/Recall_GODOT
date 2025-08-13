
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
		EnemyExecInstant = 0x12,

		// === Player Phase (0x0X) ===
		PlayerInit = 0x00,
		PlayerDraw = 0x01,
		PlayerInput = 0x02,
		PlayerExecute = 0x03,

		// === Enemy action Phase (0x1X) ===
		EnemyExecDelayed = 0x13,

		// === Turn Phase (0xFX) ===
		TurnStart = 0xF0,           // do nothing, just a marker
		TurnEnd  = 0xF1,

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

		public void Init()
		{
			Step = PhaseStep.TurnStart;
			TurnNum = 0;
		}
	}
}
