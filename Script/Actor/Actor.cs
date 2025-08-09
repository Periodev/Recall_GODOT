using Godot;
using System;
using CombatCore;
using CombatCore.Component;

public class Actor
{
	public Component[] _components;

	public Actor(int maxHP)
	{
		_components = new Component[(int)ComponentType.MaxNum];  
		_components[(int)ComponentType.HP]     = new HP(maxHP);
		_components[(int)ComponentType.Shield] = new Shield();
		_components[(int)ComponentType.Charge] = new Charge();
	}

	// 型別安全的存取器：回傳物件本身（class），直接改它的 Value 就行
	public HP HP         => (HP)_components[(int)ComponentType.HP];
	public Shield Shield => (Shield)_components[(int)ComponentType.Shield];
	public Charge Charge => (Charge)_components[(int)ComponentType.Charge];
	
	// 內部用的 slot 存取
	public T Get<T>(ComponentType type) where T : Component
		=> (T)_components[(int)type];	// 這樣可以直接用 ActorOp 的方法來操作


}
