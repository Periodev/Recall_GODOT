using System;
using CombatCore;

namespace CombatCore.Echo
{
    public partial class Echo
    {
        // UI view
        public int Id { get; init; }
        public int RecipeId { get; init; }

        public string Name { get; init; } = "";
        public string RecipeLabel { get; init; } = "";
        public string Summary { get; init; } = "";

        // HLA behavior
        public int CostAP { get; init; } = 1;
        public HLAop Op { get; init; }
        public TargetType TargetType { get; init; }

    }
}
