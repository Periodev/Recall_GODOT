
using System;
using CombatCore;
using CombatCore.Component;

public class Actor
{
	public HP HP { get; }
	public Shield Shield { get; }
	public AP AP { get; } = null!;
	public Charge? Charge { get; }
	
	public string DebugName { get; set; } = "Actor";


	public Actor(int maxHP, bool withAP = true, int apPerTurn = 3, bool withCharge = true)
	{
		HP = new HP(maxHP);
		Shield = new Shield();
		AP = withAP ? new AP(apPerTurn) : new AP(0);
		if (withCharge) Charge = new Charge();
	}

	public bool IsAlive => HP.Value > 0;
	public bool HasAP(int cost) => AP.Value >= cost;
	public bool HasCharge(int cost) => Charge?.Value >= cost;

	public T? Get<T>() where T : Component
	{
		if (typeof(T) == typeof(HP)) return HP as T;
		if (typeof(T) == typeof(Shield)) return Shield as T;
		if (typeof(T) == typeof(Charge)) return Charge as T;
		return null;
	}
}
