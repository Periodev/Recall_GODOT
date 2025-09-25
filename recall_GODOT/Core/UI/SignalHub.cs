using System;
using System.Collections.Generic;
using CombatCore;

namespace CombatCore.UI
{
	/// <summary>
	/// UI 顯示用的敵方意圖項目（最小：無時點標籤）
	/// </summary>
	public readonly record struct EnemyIntentUIItem(
		string Icon,   // 例："⚔", "🛡", "🧪", "➕" 或你的貼圖鍵
		string Text    // 例："15", "12", "Weak(2)"（UI 直接印這段）
	);

	/// <summary>
	/// SignalHub：集中管理 UI 事件的靜態樞紐
	/// </summary>
	public static class SignalHub
	{
		// ===== 既有 UI 狀態事件（保持不動） =====
		public static Action<int>? OnHPChanged { get; set; }
		public static Action<int>? OnChargeChanged { get; set; }
		public static Action<int>? OnShieldChanged { get; set; }
		public static Action<int>? OnAPChanged { get; set; }
		public static Action<FailCode>? OnErrorOccurred { get; set; }
		public static Action? OnPlayerDrawComplete { get; set; }

		// SignalHub.cs 新增
		public static Action<int, int?>? OnEnemySlotClicked { get; set; }




		// ===== 新增：敵方意圖（簡化版） =====
		/// <summary>
		/// 覆寫指定敵人的意圖清單。Queue 在「宣告」或「結算後」自行決定何時呼叫。
		/// - 宣告後：推送當前意圖（可能 1~N 個）
		/// - 結算後：推送移除已結算項目的剩餘清單（或空清單）
		/// </summary>
		public static Action<int, IReadOnlyList<EnemyIntentUIItem>>? OnEnemyIntentUpdated { get; set; }
		public static Action<int>? OnEnemyIntentCleared { get; set; }

		// ===== Notify 包裝（含既有） =====
		public static void NotifyHPChanged(int value) => OnHPChanged?.Invoke(value);
		public static void NotifyChargeChanged(int value) => OnChargeChanged?.Invoke(value);
		public static void NotifyShieldChanged(int value) => OnShieldChanged?.Invoke(value);
		public static void NotifyAPChanged(int value) => OnAPChanged?.Invoke(value);
		public static void NotifyPlayerDrawComplete() => OnPlayerDrawComplete?.Invoke();
		public static void NotifyError(FailCode failCode) => OnErrorOccurred?.Invoke(failCode);

		public static void NotifyEnemyIntentUpdated(int enemyId, IReadOnlyList<EnemyIntentUIItem> items)
			=> OnEnemyIntentUpdated?.Invoke(enemyId, items);

		public static void NotifyEnemyIntentCleared(int enemyId) => OnEnemyIntentCleared(enemyId);


		public static void NotifyEnemySlotClicked(int slotIndex, int? enemyId)
			=> OnEnemySlotClicked?.Invoke(slotIndex, enemyId);

	}
}
