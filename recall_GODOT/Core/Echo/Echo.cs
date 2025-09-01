using System;
using CombatCore;

namespace CombatCore.Echo
{
    public partial class Echo
    {
        public int Id { get; init; }
        public int RecipeId { get; init; }

        public string Name { get; init; } = "";
        public string RecipeLabel { get; init; } = "";
        public string Summary { get; init; } = "";

        public int CostAP { get; init; } = 1;
        public HLAop Op { get; init; }

    }
}
