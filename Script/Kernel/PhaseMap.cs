
using System;
using System.Collections.Generic;
using CombatCore;

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
		
		{ PhaseStep.TurnEnd, (CombatState state) => {

			state.PhaseCtx.Step = PhaseStep.TurnStart;
			return PhaseResult.Continue;
		}},
		
		{ PhaseStep.CombatEnd, (CombatState state) => {
			state.PhaseCtx.Step = PhaseStep.CombatEnd;
			return PhaseResult.CombatEnd;
		}},

		// === Player Phase ===
		// 不需要 Phase 觸發的直接調用 PhaseFunction
		{ PhaseStep.PlayerInit, (CombatState state) => 
			PhaseFunction.HandlePlayerInit(state) },
		
		{ PhaseStep.PlayerDraw, (CombatState state) => {
			UISignalHub.NotifyPlayerDrawComplete();
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.Continue;
		}},
		
		// 需要 Phase 觸發的保留攔截邏輯
		{ PhaseStep.PlayerInput, (CombatState state) => {
			
			if (state.PhaseCtx.HasPendingIntent) {

				state.PhaseCtx.Step = PhaseStep.PlayerExecute;
				return PhaseResult.Continue;
			}
			return PhaseResult.WaitInput;
		}},
		
		// 直接調用合併的 PhaseFunction
		{ PhaseStep.PlayerExecute, (CombatState state) => {
			
			// 調用合併的 PhaseFunction
			return PhaseFunction.HandlePlayerPlanningAndExecution(state);
		}},

		// === Enemy Phase ===
		{ PhaseStep.EnemyInit, (CombatState state) => {
			state.PhaseCtx.Step = PhaseStep.EnemyIntent;
			return PhaseResult.Continue;
		}},
		
		// 直接調用 PhaseFunction
		{ PhaseStep.EnemyIntent, (CombatState state) => 
			PhaseFunction.HandleEnemyAI(state) },
		
		{ PhaseStep.EnemyExecInstant, (CombatState state) => 
			PhaseFunction.HandleEnemyPlanningAndExecution(state) },
		
		{ PhaseStep.EnemyExecDelayed, (CombatState state) => {
			state.PhaseCtx.Step = PhaseStep.TurnEnd;
			return PhaseResult.Continue;
		}}
	};
}
