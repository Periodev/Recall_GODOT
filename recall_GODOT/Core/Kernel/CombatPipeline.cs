using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CombatCore;
using CombatCore.Command;
using CombatCore.InterOp;
using CombatCore.Recall;
using CombatCore.UI;

namespace CombatCore.Kernel
{
	public static class CombatPipeline
	{
		public static PhaseQueue EnemyMarkQueue { get; } = new();
		public static PhaseQueue PlayerQueue { get; } = new();
		public static PhaseQueue EnemyActionQueue { get; } = new();
		public static PhaseQueue TurnEndQueue { get; } = new();

		/// 階段1：將 HLA Intent 轉換為 AtomicCmd 陣列
		/// 使用時機：PlayerPlanning, EnemyPlanning 階段
		/// 介入點：轉換完成後，執行前（預測型反應）

		/// <param name="state">戰鬥狀態</param>
		/// <param name="actor">執行動作的角色</param>
		/// <param name="intent">高階行動意圖</param>
		/// <returns>轉換結果，包含命令陣列或錯誤碼</returns>
		public static PipelineResult TranslateIntent(CombatState state, Actor actor, Intent intent)
		{
			var translationResult = Translator.TryTranslate(intent, state, actor);

			if (!translationResult.Success)
			{
				SignalHub.NotifyError(translationResult.ErrorCode);
				return PipelineResult.Fail(translationResult.ErrorCode);
			}

			return PipelineResult.Pass(translationResult.Commands, translationResult.OriginalIntent);
		}


		/// 階段2：執行 AtomicCmd 陣列並提交狀態變更
		/// 使用時機：PlayerExecute, EnemyExecMark 階段
		/// 介入點：執行完成後（結果型反應）

		/// <param name="state">戰鬥狀態</param>
		/// <param name="commands">要執行的命令陣列</param>
		/// <param name="originalIntent">原始意圖（用於提交階段）</param>
		/// <returns>執行結果，包含實際效果日誌</returns>
		public static ExecutionResult ExecuteCommands(CombatState state, AtomicCmd[] commands, Intent originalIntent)
		{
			// 執行階段
			var execResult = CmdExecutor.ExecuteOrDiscard(commands);
			if (!execResult.Ok)
				return ExecutionResult.Fail(execResult.Code);

			return ExecutionResult.Pass(execResult.Log);
		}


		/// phase queue API 
		public static void EnqueuePlayerAction(Actor actor, Intent intent, string reason = "Player action")
		{
			PlayerQueue.Enqueue(actor, intent, reason);
		}

		public static void EnqueueEnemyMark(Actor enemy, Intent intent, string reason = "Enemy mark")
		{
			EnemyMarkQueue.Enqueue(enemy, intent, reason);
		}

		public static void EnqueueEnemyAction(Actor enemy, Intent intent, string reason = "Enemy action")
		{
			EnemyActionQueue.Enqueue(enemy, intent, reason);
		}

		public static ExecutionResult ProcessPlayerQueue(CombatState state)
		{
			var results = new List<ExecutionResult>();

			while (PlayerQueue.TryDequeue(out var queuedIntent))
			{
				var pipelineResult = TranslateIntent(state, queuedIntent.Actor, queuedIntent.Intent);

				if (!pipelineResult.Success)
				{
#if DEBUG
					Debug.Print($"[Pipeline] failed: {pipelineResult.ErrorCode}");
#endif
					continue;
				}

				var execResult = ExecuteCommands(state, pipelineResult.Commands, queuedIntent.Intent);
				if (execResult.Success)
				{
					CommitPlayerAction(state, queuedIntent.Intent, execResult);
					results.Add(execResult);
				}
			}

			return results.Count > 0 ? results[0] : ExecutionResult.Fail(FailCode.None);
		}

		public static ExecutionResult ProcessEnemyMarkQueue(CombatState state)
		{
			var results = new List<ExecutionResult>();
			var processedEnemies = new HashSet<int>();

			while (EnemyMarkQueue.TryDequeue(out var queuedIntent))
			{
				var pipelineResult = TranslateIntent(state, queuedIntent.Actor, queuedIntent.Intent);

				if (!pipelineResult.Success)
				{
					Debug.Print($"[Pipeline] Enemy mark translation failed: {pipelineResult.ErrorCode}");
					continue;
				}

				var execResult = ExecuteCommands(state, pipelineResult.Commands, queuedIntent.Intent);
				if (execResult.Success)
				{
					results.Add(execResult);
					processedEnemies.Add(queuedIntent.Actor.Id);
				}
			}

			// 清除所有已處理敵人的意圖
			foreach (var enemyId in processedEnemies)
			{
				SignalHub.NotifyEnemyIntentCleared(enemyId);
			}

			return results.Count > 0 ? results[0] : ExecutionResult.Pass(new CmdLog());
		}

		private static void CommitPlayerAction(CombatState state, Intent intent, ExecutionResult execResult)
		{
			if (!execResult.Success) return;

			// 統一 Commit 處理
			switch (intent)
			{
				case ActIntent actIntent:
					CommitAct(actIntent.Act, state);
					break;
				case RecallIntent recallIntent:
					CommitRecall(recallIntent, state);
					break;
			}
		}

		private static void CommitAct(Act act, CombatState state)
		{
			// 冷卻起算
			if (act.CooldownTurns > 0)
				act.CooldownCounter = act.CooldownTurns;

			// 推入記憶（只有 Basic + 有 PushMemory）
			if (act.ActionFlags.HasFlag(ActionType.Basic) && act.PushMemory.HasValue)
			{
				state.Mem?.Push(act.PushMemory.Value, state.PhaseCtx.TurnNum);
			}

			// 移除消耗型 Act
			if (act.ConsumeOnPlay)
			{
				state.actStore.TryRemove(act);
			}
		}

		private static void CommitRecall(RecallIntent intent, CombatState state)
		{
			var act = ActFactory.BuildFromRecipe(intent.RecipeId);
			if (state.actStore.TryAdd(act) == FailCode.None)
			{
				state.PhaseCtx.MarkRecallUsed();
			}
		}

		/// <summary>
		/// 處理 Enemy ActionQueue 中的所有 Intent
		/// </summary>
		public static ExecutionResult ProcessEnemyActionQueue(CombatState state)
		{
			var results = new List<ExecutionResult>();
			var processedEnemies = new HashSet<int>();

			while (EnemyActionQueue.TryDequeue(out var queuedIntent))
			{
				var pipelineResult = TranslateIntent(state, queuedIntent.Actor, queuedIntent.Intent);

				if (!pipelineResult.Success)
				{
					Debug.Print($"[Pipeline] Enemy action translation failed: {pipelineResult.ErrorCode}");
					continue;
				}

				var execResult = ExecuteCommands(state, pipelineResult.Commands, queuedIntent.Intent);
				if (execResult.Success)
				{
					results.Add(execResult);
					processedEnemies.Add(queuedIntent.Actor.Id);
				}
			}

			// 清除所有已處理敵人的意圖
			foreach (var enemyId in processedEnemies)
			{
				SignalHub.NotifyEnemyIntentCleared(enemyId);
			}

			return results.Count > 0 ? results[0] : ExecutionResult.Pass(new CmdLog());
		}

		/// <summary>
		/// 判斷行為是否為即時執行
		/// </summary>
		private static bool IsMarkAction(Intent intent)
		{
			if (intent is ActIntent actIntent)
			{
				return actIntent.Act.Op == HLAop.Block || actIntent.Act.Op == HLAop.Charge;
			}
			return false;
		}

		/// AI 支援：生成敵人行動意圖並分類到對應隊列
		/// 使用時機：EnemyIntent 階段
		public static void GenerateAndEnqueueEnemyActions(CombatState state)
		{
			var enemies = state.GetAllEnemies();
			if (enemies.Count == 0) return;

			// 處理所有活著的敵人
			foreach (var enemy in enemies.Where(e => e.IsAlive))
			{
				if (enemy.Id == 1) // 第一個敵人 (Enemy1): 交替攻擊/防禦
				{
					if (state.PhaseCtx.TurnNum % 2 == 1)
					{
						var blockAct = CreateEnemyBasicAct(HLAop.Block);
						var blockIntent = new ActIntent(blockAct, null);
						EnemyMarkQueue.Enqueue(enemy, blockIntent, "Enemy1 Block");

						var declare = new List<EnemyIntentUIItem>
						{
							new("🛡", "Block 1")
						};
						SignalHub.NotifyEnemyIntentUpdated(enemy.Id, declare);
					}
					else
					{
						var attackAct = CreateEnemyBasicAct(HLAop.Attack);
						var attackIntent = new ActIntent(attackAct, 0);
						EnemyActionQueue.Enqueue(enemy, attackIntent, "Enemy1 Attack");

						var declare = new List<EnemyIntentUIItem>
						{
							new("⚔", "Attack 2")
						};
						SignalHub.NotifyEnemyIntentUpdated(enemy.Id, declare);
					}
				}
				else if (enemy.Id == 2) // 第二個敵人 (Enemy2): 持續格檔
				{
					var blockAct = CreateEnemyBasicAct(HLAop.Block);
					var blockIntent = new ActIntent(blockAct, null);
					EnemyMarkQueue.Enqueue(enemy, blockIntent, "Enemy2 Block");

					var declare = new List<EnemyIntentUIItem>
					{
						new("🛡", "Defend")
					};
					SignalHub.NotifyEnemyIntentUpdated(enemy.Id, declare);
				}
			}
		}

		/// <summary>
		/// 輔助方法：建立敵人 Basic Act
		/// </summary>
		private static Act CreateEnemyBasicAct(HLAop op)
		{
			return new Act
			{
				ActionFlags = ActionType.Basic,
				ConsumeOnPlay = false,
				Op = op,
				TargetType = op == HLAop.Attack ? TargetType.Target : TargetType.Self,
				Name = op.ToString(),
				CostAP = 0  // 敵人不消耗 AP
			};
		}

		/// <summary>
		/// 處理 Turn End Queue 中的所有 Intent
		/// </summary>
		public static ExecutionResult ProcessTurnEndQueue(CombatState state)
		{
			// 目前只清空隊列並返回成功結果
			TurnEndQueue.Clear();
			return ExecutionResult.Pass(new CmdLog());
		}
	}


	/// Intent 轉換結果 - 包含即將執行的指令序列
	/// 用於預測型反應：其他系統可以分析 Commands 並提前應對
	public readonly struct PipelineResult
	{
		public bool Success { get; }
		public FailCode ErrorCode { get; }
		public AtomicCmd[] Commands { get; }
		public Intent OriginalIntent { get; }

		private PipelineResult(bool success, FailCode errorCode, AtomicCmd[] commands, Intent originalIntent)
		{
			Success = success;
			ErrorCode = errorCode;
			Commands = commands ?? Array.Empty<AtomicCmd>();
			OriginalIntent = originalIntent;
		}

		public static PipelineResult Pass(AtomicCmd[] commands, Intent intent) =>
				new(true, FailCode.None, commands, intent);

		public static PipelineResult Fail(FailCode code) =>
				new(false, code, Array.Empty<AtomicCmd>(), null!);
	}

	/// 命令執行結果 - 包含實際發生的效果記錄
	/// 用於結果型反應：其他系統可以分析 Log 並觸發連鎖反應
	public readonly struct ExecutionResult
	{
		public bool Success { get; }
		public FailCode ErrorCode { get; }
		public CmdLog Log { get; }

		private ExecutionResult(bool success, FailCode errorCode, CmdLog log)
		{
			Success = success;
			ErrorCode = errorCode;
			Log = log ?? new CmdLog();
		}

		public static ExecutionResult Pass(CmdLog log) =>
			new(true, FailCode.None, log);

		public static ExecutionResult Fail(FailCode code) =>
			new(false, code, new CmdLog());
	}
}
