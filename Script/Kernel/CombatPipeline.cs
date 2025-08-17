using System;
using CombatCore;
using CombatCore.Abstractions;
using CombatCore.Command;
using CombatCore.InterOp;
using CombatCore.Memory;

namespace CombatCore
{
	public static class CombatPipeline
	{
		// === 靜態實例，避免重複創建無狀態對象 ===
		private static readonly HLATranslator Translator = new();
		private static readonly InterOps InterOps = new();
		private static readonly CmdExecutor Executor = new();


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
				state, 
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

		/// AI 支援：生成敵人行動意圖
		/// 使用時機：EnemyIntent 階段
		public static HLAIntent GenerateEnemyIntent(CombatState state)
		{
			// 簡單 AI 邏輯：血量低時防禦，否則攻擊
			var enemy = state.Enemy;
			var healthRatio = (double)enemy.HP.Value / enemy.HP.Max.Value;

			if (healthRatio < 0.3 && enemy.HasAP(1))
			{
				// 血量低於30%時選擇防禦
				return new BasicIntent(ActionType.B, null);
			}
			else if (enemy.HasAP(1))
			{
				// 否則攻擊玩家（假設玩家 ID 為 0）
				return new BasicIntent(ActionType.A, 0);
			}
			else
			{
				// 沒有 AP 時選擇充能
				return new BasicIntent(ActionType.C, null);
			}
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
