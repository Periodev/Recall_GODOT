
using System;
using System.Collections.Generic;
using CombatCore;

public static class PhaseMap
{
	// 簽名改變：接收完整 CombatState
	public delegate PhaseResult StepMap(ref CombatState state);

	public static readonly Dictionary<PhaseStep, StepMap> StepMaps = new()
	{
		// === Turn Phase ===
		{ PhaseStep.TurnStart, (ref CombatState state) => {
			state.PhaseCtx.StartNewTurn();
			
			state.PhaseCtx.Step = PhaseStep.EnemyInit;
			return PhaseResult.Continue;
		}},
		
		{ PhaseStep.TurnEnd, (ref CombatState state) => {

			state.PhaseCtx.Step = PhaseStep.TurnStart;
			return PhaseResult.Continue;
		}},
		
		{ PhaseStep.CombatEnd, (ref CombatState state) => {
			state.PhaseCtx.Step = PhaseStep.CombatEnd;
			return PhaseResult.CombatEnd;
		}},

		// === Player Phase ===
		// 不需要 Phase 觸發的直接調用 PhaseFunction
		{ PhaseStep.PlayerInit, (ref CombatState state) => 
			PhaseFunction.HandlePlayerInit(ref state) },
		
		{ PhaseStep.PlayerDraw, (ref CombatState state) => {
			UISignalHub.NotifyPlayerDrawComplete();
			state.PhaseCtx.Step = PhaseStep.PlayerInput;
			return PhaseResult.Continue;
		}},
		
		// 需要 Phase 觸發的保留攔截邏輯
		{ PhaseStep.PlayerInput, (ref CombatState state) => {
			
			if (state.PhaseCtx.HasPendingIntent) {

				state.PhaseCtx.Step = PhaseStep.PlayerPlanning;
				return PhaseResult.Continue;
			}
			return PhaseResult.WaitInput;
		}},
		
		// 直接調用 PhaseFunction
		{ PhaseStep.PlayerPlanning, (ref CombatState state) =>
			PhaseFunction.HandlePlayerPlanning(ref state) },
		
		// 需要 Phase 觸發的
		{ PhaseStep.PlayerExecute, (ref CombatState state) => {
			
			// 然後調用 PhaseFunction
			return PhaseFunction.HandlePlayerExecution(ref state);
		}},

		// === Enemy Phase ===
		{ PhaseStep.EnemyInit, (ref CombatState state) => {
			state.PhaseCtx.Step = PhaseStep.EnemyIntent;
			return PhaseResult.Continue;
		}},
		
		// 直接調用 PhaseFunction
		{ PhaseStep.EnemyIntent, (ref CombatState state) => 
			PhaseFunction.HandleEnemyAI(ref state) },
		
		{ PhaseStep.EnemyPlanning, (ref CombatState state) =>
			PhaseFunction.HandleEnemyPipelineProcessing(ref state) },
		
		{ PhaseStep.EnemyExecInstant, (ref CombatState state) => 
			PhaseFunction.HandleEnemyExecution(ref state) },
		
		{ PhaseStep.EnemyExecDelayed, (ref CombatState state) => {
			state.PhaseCtx.Step = PhaseStep.TurnEnd;
			return PhaseResult.Continue;
		}}
	};
}
