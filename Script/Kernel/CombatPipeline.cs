using System;
using System.Collections.Generic;
using CombatCore;
using CombatCore.Command;
using CombatCore.InterOp;
using CombatCore.Memory;

#if DEBUG
using Godot;
#endif

namespace CombatCore
{
	public static class CombatPipeline
	{
		// === 靜態實例，避免重複創建無狀態對象 ===
		private static readonly HLATranslator Translator = new();
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
		public static TranslationResult TranslateIntent(CombatState state, Actor actor, HLAIntent intent)
		{
			// 驗證階段
			var failCode = Translator.TryTranslate(
				intent, 
				state.PhaseCtx, 
				state.GetRecallView(), 
				state.TryGetActor, 
				actor, 
				out var basicPlan, 
				out var recallPlan
			);

			if (failCode != FailCode.None)
				return TranslationResult.Fail(failCode);

			// 轉換階段：根據 Intent 類型建構命令
			var commands = intent switch
			{
				BasicIntent => InterOps.BuildBasic(basicPlan),
				RecallIntent => InterOps.BuildRecall(recallPlan),
				_ => Array.Empty<AtomicCmd>()
			};

			return TranslationResult.Pass(commands, intent);
		}


		/// 階段2：執行 AtomicCmd 陣列並提交狀態變更
		/// 使用時機：PlayerExecute, EnemyExecInstant 階段
		/// 介入點：執行完成後（結果型反應）

		/// <param name="state">戰鬥狀態</param>
		/// <param name="commands">要執行的命令陣列</param>
		/// <param name="originalIntent">原始意圖（用於提交階段）</param>
		/// <returns>執行結果，包含實際效果日誌</returns>
		public static ExecutionResult ExecuteCommands(CombatState state, AtomicCmd[] commands, HLAIntent originalIntent)
		{
			// 執行階段
			var execResult = Executor.ExecuteOrDiscard(commands);
			if (!execResult.Ok)
				return ExecutionResult.Fail(execResult.Code);

			return ExecutionResult.Pass(execResult.Log);
		}

		/// Player Queue 管理
		public static void EnqueuePlayerAction(Actor actor, HLAIntent intent, string reason = "Player action")
		{
			PlayerQueue.Enqueue(actor, intent, reason);
		}

		/// Enemy Instant Queue 管理
		public static void EnqueueEnemyInstantAction(Actor enemy, HLAIntent intent, string reason = "Enemy instant")
		{
			EnemyInstantQueue.Enqueue(enemy, intent, reason);
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
					GD.Print($"[Pipeline] Translation failed: {translationResult.ErrorCode}");
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
#if DEBUG
					GD.Print($"[Pipeline] Enemy instant translation failed: {translationResult.ErrorCode}");
#endif
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

		private static void CommitPlayerAction(CombatState state, HLAIntent intent, ExecutionResult execResult)
		{
			if (intent is BasicIntent basicIntent)
			{
				state.Mem?.Push(basicIntent.Act, state.PhaseCtx.TurnNum);
			}
			
			if (intent is RecallIntent)
			{
				state.PhaseCtx.MarkRecallUsed();
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
#if DEBUG
					GD.Print($"[Pipeline] Enemy delayed translation failed: {translationResult.ErrorCode}");
#endif
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
		private static bool IsInstantAction(HLAIntent intent)
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

			// 生成基本攻擊行為 (ActionType.A - 延遲執行)
			var attackIntent = new BasicIntent(ActionType.A, 0);
			EnemyDelayedQueue.Enqueue(enemy, attackIntent, "Enemy AI attack");

			// 根據條件可能生成即時行為 (ActionType.B 或 C)
			if (enemy.HP.Value < enemy.HP.Max / 2)
			{
				var blockIntent = new BasicIntent(ActionType.B, 0);
				if (IsInstantAction(blockIntent))
				{
					EnqueueEnemyInstantAction(enemy, blockIntent, "Enemy AI block");
				}
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
	}


	/// Intent 轉換結果 - 包含即將執行的指令序列
	/// 用於預測型反應：其他系統可以分析 Commands 並提前應對
	public readonly struct TranslationResult
	{
		public bool Success { get; }
		public FailCode ErrorCode { get; }
		public AtomicCmd[] Commands { get; }
		public HLAIntent OriginalIntent { get; }

		private TranslationResult(bool success, FailCode errorCode, AtomicCmd[] commands, HLAIntent originalIntent)
		{
			Success = success;
			ErrorCode = errorCode;
			Commands = commands ?? Array.Empty<AtomicCmd>();
			OriginalIntent = originalIntent;
		}

		public static TranslationResult Pass(AtomicCmd[] commands, HLAIntent intent) =>
			new(true, FailCode.None, commands, intent);

		public static TranslationResult Fail(FailCode code) =>
			new(false, code, Array.Empty<AtomicCmd>(), null);
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
