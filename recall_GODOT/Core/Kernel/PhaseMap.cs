
using System;
using System.Collections.Generic;
using CombatCore;
using CombatCore.UI;

namespace CombatCore.Kernel
{
	public static class PhaseMap
	{
		// 簽名改變：接收完整 CombatState
		public delegate PhaseResult StepMap(CombatState state);

		public static readonly Dictionary<PhaseStep, StepMap> StepMaps = new()
		{
			// === Turn Phase ===
			{ PhaseStep.TurnStart, (CombatState state) => {
				state.PhaseCtx.StartNewTurn();

				state.PhaseCtx.Step = PhaseStep.EnemyInit;
				return PhaseResult.Continue;
			}},

			{ PhaseStep.TurnEnd, (CombatState state) =>
				PhaseFunction.HandleTurnEnd(state) },

			{ PhaseStep.CombatEnd, (CombatState state) => {
				state.PhaseCtx.Step = PhaseStep.CombatEnd;
				return PhaseResult.CombatEnd;
			}},

			// === Player Phase ===
			// 不需要 Phase 觸發的直接調用 PhaseFunction
			{ PhaseStep.PlayerInit, (CombatState state) =>
				PhaseFunction.HandlePlayerInit(state) },

			{ PhaseStep.PlayerDraw, (CombatState state) => {
				SignalHub.NotifyPlayerDrawComplete();
				state.PhaseCtx.Step = PhaseStep.PlayerInput;
				return PhaseResult.Continue;
			}},

			{ PhaseStep.PlayerInput, (CombatState state) => {
				if (CombatPipeline.PlayerQueue.HasIntents)
				{
					state.PhaseCtx.Step = PhaseStep.PlayerExecute;
					return PhaseResult.Continue;
				}
				return PhaseResult.WaitInput;
			}},

			{ PhaseStep.PlayerExecute, (CombatState state) =>
				PhaseFunction.HandlePlayerExecution(state) },

			// === Enemy Phase ===
			{ PhaseStep.EnemyInit, (CombatState state) => {
				state.PhaseCtx.Step = PhaseStep.EnemyIntent;
				return PhaseResult.Continue;
			}},
			
			// 直接調用 PhaseFunction
			{ PhaseStep.EnemyIntent, (CombatState state) =>
				PhaseFunction.HandleEnemyAI(state) },

			{ PhaseStep.EnemyExecInstant, (CombatState state) =>
				PhaseFunction.HandleEnemyInstantExecution(state) },

			{ PhaseStep.EnemyExecDelayed, (CombatState state) =>
				PhaseFunction.HandleEnemyDelayed(state) }
		};
	}
}