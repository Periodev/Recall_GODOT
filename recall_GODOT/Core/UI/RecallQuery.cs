using System;
using System.Linq;
using System.Diagnostics;

using CombatCore;

namespace CombatCore.UI
{
    public readonly struct RecallValidationResult
    {
        public bool IsValid { get; }
        public FailCode ErrorCode { get; }
        public int RecipeId { get; }
        
        private RecallValidationResult(bool isValid, FailCode errorCode, int recipeId)
        {
            IsValid = isValid;
            ErrorCode = errorCode;
            RecipeId = recipeId;
        }
        
        public static RecallValidationResult Pass(int recipeId) => 
            new(true, FailCode.None, recipeId);

        public static RecallValidationResult Fail(FailCode code)
        {
            SignalHub.NotifyError(code);
            return new(false, code, -1);
        }
    }

    public static class RecallQuery
    {
        /// <summary>
        /// Godot UI 的統一入口：驗證索引並選擇 Recipe
        /// </summary>
        public static RecallValidationResult ValidateAndSelectRecipe(
            int[] indices, 
            RecallView memory, 
            int currentTurn,
            bool isEchoStoreFull)
        {
			// EchoStore full check
			if (isEchoStoreFull)
				return RecallValidationResult.Fail(FailCode.EchoSlotsFull);

            // 索引驗證（從 Translator 移出的邏輯）
            var indexValidation = ValidateIndices(indices, memory, currentTurn);
            if (indexValidation != FailCode.None)
                return RecallValidationResult.Fail(indexValidation);

            // 內部調用 RecipeSystem 獲取配方
            var sequence = indices.Select(idx => memory.Ops[idx]).ToArray();
            
            // TODO: 等待 RecipeSystem 實現，暫時返回固定 recipeId
            int recipeId = 1; // 臨時實現
            
            Debug.Print($"[Recipe]: {recipeId}");

            return RecallValidationResult.Pass(recipeId);
        }

        /// <summary>
        /// 私有方法：索引合法性檢查（從 Translator 搬移）
        /// </summary>
        private static FailCode ValidateIndices(int[] indices, RecallView memory, int currentTurn)
        {
            // 空索引防呆
            if (indices.Length == 0) return FailCode.BadIndex;
            
            // 檢查索引範圍和重複
            if (indices.Any(idx => idx < 0 || idx >= memory.Count) || 
                indices.Distinct().Count() != indices.Length)
            {
                return FailCode.IndexOutOfBound;
            }

            // 排除本回合：檢查是否引用當前回合的記憶
            if (indices.Any(idx => memory.Turns[idx] == currentTurn))
            {
                return FailCode.IndexLimited;
            }

			// 連續性檢查：去重 + 由小到大排序 → 相鄰索引必須差 1（預留給未來 2L/3L）
			var span = indices.Distinct().OrderBy(x => x).ToArray();
			for (int i = 1; i < span.Length; i++)
			{
				if (span[i] != span[i - 1] + 1)
					return FailCode.IndixNotContiguous;
			}

            return FailCode.None;
        }
    }
}