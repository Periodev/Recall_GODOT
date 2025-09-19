
using System;
using CombatCore;
using CombatCore.ActorOp;

namespace CombatCore
{
	public class Actor
	{
		public int Id { get; set; } = -1; // 未指派時為 -1
		public HP HP { get; }
		public Shield Shield { get; }
		public AP? AP { get; }
		public Charge? Charge { get; }
		public Copy? Copy { get; }

		public string DebugName { get; set; } = "Actor";


		public Actor(int maxHP, bool withAP = true, int apPerTurn = 3, bool withCharge = true, bool withCopy = false)
		{
			HP = new HP(maxHP);
			Shield = new Shield();
			if (withAP) AP = new AP(apPerTurn);
			if (withCharge) Charge = new Charge();
			if (withCopy) Copy = new Copy();
		}

		public bool IsAlive => HP.Value > 0;
		public bool HasAP(int cost) => AP?.Value >= cost;
		public bool HasCharge(int cost) => Charge?.Value >= cost;
	}

	public static class ActorExtensions
	{
		public static bool HasCopy(this Actor actor) =>
			(actor.Copy?.Value ?? 0) > 0;

		public static bool HasCharge(this Actor actor, int cost) =>
			(actor.Charge?.Value ?? 0) >= cost;

		public static bool HasAP(this Actor actor, int cost) =>
			(actor.AP?.Value ?? 0) >= cost;
	}
}