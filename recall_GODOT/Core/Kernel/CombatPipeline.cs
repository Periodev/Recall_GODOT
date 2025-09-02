using System;
using System.Collections.Generic;
using System.Diagnostics;
using CombatCore;
using CombatCore.Command;
using CombatCore.InterOp;
using CombatCore.Recall;

namespace CombatCore
{
	public static class CombatPipeline
	{
		// === 靜態實例，避免重複創建無狀態對象 ===
		private static readonly Translator Translator = new();
		private static readonly InterOps InterOps = new();
		private static readonly CmdExecutor Executor = new();

		public static PhaseQueue EnemyInstantQueue { get; } = new();
		public static PhaseQueue PlayerQueue { get; } = new();
		public static PhaseQueue EnemyDelayedQueue { get; } = new();
		public static PhaseQueue TurnEndQueue { get; } = new();



		/// 階段1：將 HLA Intent 轉換為 AtomicCmd 陣列
		/// 使用時機：PlayerPlanning, EnemyPlanning 階段
		/// 介入點：轉換完成後，執行前（預測型反應）

		/// <param name="state">戰鬥狀態</param>
		/// <param name="actor">執行動作的角色</param>
		/// <param name="intent">高階行動意圖</param>
		/// <returns>轉換結果，包含命令陣列或錯誤碼</returns>
		public static TranslationResult TranslateIntent(CombatState state, Actor actor, Intent intent)
		{
			var translationResult = Translator.TryTranslate(intent, state, actor);
				
			if (!translationResult.Success)
				return TranslationResult.Fail(translationResult.ErrorCode);

			var commands = InterOps.Build(translationResult.Plan);
			return TranslationResult.Pass(commands, translationResult.OriginalIntent);
		}


		/// 階段2：執行 AtomicCmd 陣列並提交狀態變更
		/// 使用時機：PlayerExecute, EnemyExecInstant 階段
		/// 介入點：執行完成後（結果型反應）

		/// <param name="state">戰鬥狀態</param>
		/// <param name="commands">要執行的命令陣列</param>
		/// <param name="originalIntent">原始意圖（用於提交階段）</param>
		/// <returns>執行結果，包含實際效果日誌</returns>
		public static ExecutionResult ExecuteCommands(CombatState state, AtomicCmd[] commands, Intent originalIntent)
		{
			// 執行階段
			var execResult = Executor.ExecuteOrDiscard(commands);
			if (!execResult.Ok)
				return ExecutionResult.Fail(execResult.Code);

			return ExecutionResult.Pass(execResult.Log);
		}


		/// phase queue API 
		public static void EnqueuePlayerAction(Actor actor, Intent intent, string reason = "Player action")
		{
			PlayerQueue.Enqueue(actor, intent, reason);
		}

		public static void EnqueueEnemyInstantAction(Actor enemy, Intent intent, string reason = "Enemy instant")
		{
			EnemyInstantQueue.Enqueue(enemy, intent, reason);
		}

		public static void EnqueueEnemyDelayedAction(Actor enemy, Intent intent, string reason = "Enemy delayed")
		{
			EnemyDelayedQueue.Enqueue(enemy, intent, reason);
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

		public static ExecutionResult ProcessEnemyInstantQueue(CombatState state)
		{
			var results = new List<ExecutionResult>();

			while (EnemyInstantQueue.TryDequeue(out var queuedIntent))
			{
				var translationResult = TranslateIntent(state, queuedIntent.Actor, queuedIntent.Intent);

				if (!translationResult.Success)
				{
					Debug.Print($"[Pipeline] Enemy instant translation failed: {translationResult.ErrorCode}");
					continue;
				}

				var execResult = ExecuteCommands(state, translationResult.Commands, queuedIntent.Intent);
				if (execResult.Success)
				{
					results.Add(execResult);
				}
			}

			return results.Count > 0 ? results[0] : ExecutionResult.Pass(new CmdLog());
		}

		private static void CommitPlayerAction(CombatState state, Intent intent, ExecutionResult execResult)
		{
			if (!execResult.Success) return;

			if (intent is BasicIntent basicIntent)
			{
				state.Mem?.Push(basicIntent.Act, state.PhaseCtx.TurnNum);
			}

			if (intent is RecallIntent recallIntent)
			{
				state.PhaseCtx.MarkRecallUsed();

				var sequence = RebuildMemSeq(state.GetRecallView(), recallIntent);
				var echo = Echo.Build(sequence, state.PhaseCtx.TurnNum);
				state.echoStore.TryAdd(echo);
			}

			// 新增 Echo 處理
			if (intent is EchoIntent echoIntent)
			{
				state.echoStore.TryRemoveAt(echoIntent.SlotIndex);
				// Echo 不寫入 Memory
			}
		}

		/// <summary>
		/// 處理 Enemy DelayedQueue 中的所有 Intent
		/// </summary>
		public static ExecutionResult ProcessEnemyDelayedQueue(CombatState state)
		{
			var results = new List<ExecutionResult>();

			while (EnemyDelayedQueue.TryDequeue(out var queuedIntent))
			{
				var translationResult = TranslateIntent(state, queuedIntent.Actor, queuedIntent.Intent);

				if (!translationResult.Success)
				{
					Debug.Print($"[Pipeline] Enemy delayed translation failed: {translationResult.ErrorCode}");
					continue;
				}

				var execResult = ExecuteCommands(state, translationResult.Commands, queuedIntent.Intent);
				if (execResult.Success)
				{
					results.Add(execResult);
				}
			}

			return results.Count > 0 ? results[0] : ExecutionResult.Pass(new CmdLog());
		}

		/// <summary>
		/// 判斷行為是否為即時執行
		/// </summary>
		private static bool IsInstantAction(Intent intent)
		{
			if (intent is BasicIntent basicIntent)
			{
				return basicIntent.Act == ActionType.B || basicIntent.Act == ActionType.C;
			}
			return false;
		}

		/// AI 支援：生成敵人行動意圖並分類到對應隊列
		/// 使用時機：EnemyIntent 階段
		public static void GenerateAndEnqueueEnemyActions(CombatState state)
		{
			// 簡單 AI 邏輯：生成多個敵人行為
			var enemy = state.Enemy;

			// 偶數回合防禦(instant)，奇數回合攻擊(delay)
			if (state.PhaseCtx.TurnNum % 2 == 1)
			{
				// B = instant
				var blockIntent = new BasicIntent(ActionType.B, null);
				EnemyInstantQueue.Enqueue(enemy, blockIntent, "Block");
			}


			else
			{
				// A = delay  
				var attackIntent = new BasicIntent(ActionType.A, 0);
				EnemyDelayedQueue.Enqueue(enemy, attackIntent, "Attack");
			}

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

		/// 從 RecallIntent 的索引，在指定的 MemoryView 中重建出行為序列。
		/// <param name="memory">目前回合的記憶視圖</param>
		/// <param name="intent">RecallIntent，內含索引</param>
		/// <returns>對應的 ActionType 序列</returns>
		public static ActionType[] RebuildMemSeq(RecallView memory, RecallIntent intent)
		{
			return intent.RecallIndices
						 .Select(i => memory.Ops[i])
						 .ToArray();
		}

	}


	/// Intent 轉換結果 - 包含即將執行的指令序列
	/// 用於預測型反應：其他系統可以分析 Commands 並提前應對
	public readonly struct TranslationResult
	{
		public bool Success { get; }
		public FailCode ErrorCode { get; }
		public AtomicCmd[] Commands { get; }
		public Intent OriginalIntent { get; }

		private TranslationResult(bool success, FailCode errorCode, AtomicCmd[] commands, Intent originalIntent)
		{
			Success = success;
			ErrorCode = errorCode;
			Commands = commands ?? Array.Empty<AtomicCmd>();
			OriginalIntent = originalIntent;
		}

		public static TranslationResult Pass(AtomicCmd[] commands, Intent intent) =>
			new(true, FailCode.None, commands, intent);

		public static TranslationResult Fail(FailCode code) =>
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
