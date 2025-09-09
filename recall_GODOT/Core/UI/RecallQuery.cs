using System;
using System.Linq;
using System.Diagnostics;

using CombatCore;
using CombatCore.Recall;

namespace CombatCore.UI
{
    public readonly struct RecallValidationResult
    {
        public bool IsValid { get; }
        public FailCode ErrorCode { get; }
        public List<int> CandidateRecipeIds { get; }
        public int SelectedRecipeId { get; }

        private RecallValidationResult(bool isValid, FailCode errorCode, 
            List<int> candidateRecipeIds, int selectedRecipeId)
        {
            IsValid = isValid;
            ErrorCode = errorCode;
            CandidateRecipeIds = candidateRecipeIds ?? new List<int>();
            SelectedRecipeId = selectedRecipeId;
        }

        public static RecallValidationResult Pass(List<int> candidateIds) =>
            new(true, FailCode.None, candidateIds, -1);

        public static RecallValidationResult Fail(FailCode code)
        {
            SignalHub.NotifyError(code);
            return new(false, code, new List<int>(), -1);
        }
        
        public RecallValidationResult WithSelection(int recipeId) =>
            new(IsValid, ErrorCode, CandidateRecipeIds, recipeId);
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

            int pattern = PatternExtractor.Encode(sequence);
            //Debug.Print($"[pattern]: {pattern}");

            var candidateRecipeIds = RecipeSystem.FilterRecipesByPattern(pattern);

            if (candidateRecipeIds.Count == 0)
                return RecallValidationResult.Fail(FailCode.NoRecipe);

            return RecallValidationResult.Pass(candidateRecipeIds);
        }

        /// <summary>
        /// 獲取 Recipe 顯示信息的方法
        /// </summary>
        public static bool TryGetRecipeDisplayInfo(int recipeId, 
            out string name, out string label, out string summary)
        {
            name = label = summary = string.Empty;
            
            if (!RecipeRegistry.TryGetRecipe(recipeId, out var recipe))
                return false;
                
            name = recipe.Name;
            label = recipe.Label;  
            summary = recipe.Summary;
            return true;
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
                    return FailCode.IndexNotContiguous;
            }

            return FailCode.None;
        }
    }
}