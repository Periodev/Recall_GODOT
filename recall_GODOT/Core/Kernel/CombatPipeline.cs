using System;
using System.Collections.Generic;
using System.Diagnostics;
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

			var commands = InterOps.Build(translationResult.Plan);
			return PipelineResult.Pass(commands, translationResult.OriginalIntent);
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
				var translationResult = TranslateIntent(state, queuedIntent.Actor, queuedIntent.Intent);

				if (!translationResult.Success)
				{
#if DEBUG
					Debug.Print($"[Pipeline] Translation failed: {translationResult.ErrorCode}");
#endif
					continue;
				}

				var execResult = ExecuteCommands(state, translationResult.Commands, queuedIntent.Intent);
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

			while (EnemyMarkQueue.TryDequeue(out var queuedIntent))
			{
				var translationResult = TranslateIntent(state, queuedIntent.Actor, queuedIntent.Intent);

				if (!translationResult.Success)
				{
					Debug.Print($"[Pipeline] Enemy mark translation failed: {translationResult.ErrorCode}");
					continue;
				}

				var execResult = ExecuteCommands(state, translationResult.Commands, queuedIntent.Intent);
				if (execResult.Success)
				{
					results.Add(execResult);
				}
			}

			SignalHub.NotifyEnemyIntentCleared(1);

			return results.Count > 0 ? results[0] : ExecutionResult.Pass(new CmdLog());
		}

		private static void CommitPlayerAction(CombatState state, Intent intent, ExecutionResult execResult)
		{
			if (!execResult.Success) return;

			// 處理 Echo 行為（統一邏輯）
			if (intent is ActIntent actIntent)
			{
				var act = actIntent.Act;

				// 觸發冷卻
				if (act.CooldownTurns > 0)
					act.CooldownCounter = act.CooldownTurns;

				// 推入記憶
				if (act.ActionFlags.HasFlag(ActionType.Basic) && act.PushMemory.HasValue)
				{
					state.Mem?.Push(act.PushMemory.Value, state.PhaseCtx.TurnNum);
				}

				// 移除消耗型 Echo
				if (act.ConsumeOnPlay)
				{
					state.actStore.TryRemove(act);
				}
			}

			if (intent is RecallIntent recallIntent)
			{
				// Use RecipeId lookup to build Act directly
				var act = ActFactory.BuildFromRecipe(recallIntent.RecipeId);

				// Only mark RecallUsed if successfully added to store
				if (state.actStore.TryAdd(act) == FailCode.None)
				{
					state.PhaseCtx.MarkRecallUsed();
				}
				else
				{
					// Echo slot full or add failed → don't mark RecallUsed, don't write to Memory
					// (If AP already consumed during execution, consider AP restoration logic here)
					return;
				}
				// RecallIntent does not write to Memory
			}

		}

		/// <summary>
		/// 處理 Enemy ActionQueue 中的所有 Intent
		/// </summary>
		public static ExecutionResult ProcessEnemyActionQueue(CombatState state)
		{
			var results = new List<ExecutionResult>();

			while (EnemyActionQueue.TryDequeue(out var queuedIntent))
			{
				var translationResult = TranslateIntent(state, queuedIntent.Actor, queuedIntent.Intent);

				if (!translationResult.Success)
				{
					Debug.Print($"[Pipeline] Enemy action translation failed: {translationResult.ErrorCode}");
					continue;
				}

				var execResult = ExecuteCommands(state, translationResult.Commands, queuedIntent.Intent);
				if (execResult.Success)
				{
					results.Add(execResult);
				}
			}

			SignalHub.NotifyEnemyIntentCleared(1);

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
			// 簡單 AI 邏輯：生成多個敵人行為
			var enemy = state.Enemy;

			// 偶數回合防禦(mark)，奇數回合攻擊(delay)
			if (state.PhaseCtx.TurnNum % 2 == 1)
			{
				// B = mark
				var blockAct = CreateEnemyBasicAct(HLAop.Block, TokenType.B);
				var blockIntent = new ActIntent(blockAct, null);
				EnemyMarkQueue.Enqueue(enemy, blockIntent, "Block");


				var Declare = new List<CombatCore.UI.EnemyIntentUIItem>
				{
					new CombatCore.UI.EnemyIntentUIItem("🛡", "Block 1"),  // Block(1) → 下回合開始會套上
				};

				SignalHub.NotifyEnemyIntentUpdated(1, Declare);
			}
			else
			{
				// A = delay  
				var attackAct = CreateEnemyBasicAct(HLAop.Attack, TokenType.A);
				var attackIntent = new ActIntent(attackAct, 0);
				EnemyActionQueue.Enqueue(enemy, attackIntent, "Attack");

				var Declare = new List<CombatCore.UI.EnemyIntentUIItem>
				{
					new CombatCore.UI.EnemyIntentUIItem("⚔", "Attack 2"),
				};

				SignalHub.NotifyEnemyIntentUpdated(1, Declare);
			}

		}

		/// <summary>
		/// 輔助方法：建立敵人 Basic Act
		/// </summary>
		private static Act CreateEnemyBasicAct(HLAop op, TokenType pushToken)
		{
			return new Act
			{
				ActionFlags = ActionType.Basic,
				PushMemory = pushToken,
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
