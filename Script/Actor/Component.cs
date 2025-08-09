using Godot;
using System;
using CombatCore;

namespace CombatCore.Component
{
	public enum ComponentType { HP, Shield, Charge, MaxNum }

	// 統一的基類，但只有數據
	public class Component
	{
		public int Value;
		public int? Max;  // 統一的可選上限

		public Component(int? max = null)
		{
			Max = max;
		}
	}

	// 具體的 component 只是不同的實例
	public class Shield : Component
	{
		public Shield() : base(int.MaxValue) { }  // 無上限
	}

	public class Charge : Component  
	{
		public Charge() : base(3) { }     // 上限 3
	}

	public class HP : Component
	{
		public HP(int maxHP) : base(maxHP) 
		{
			Value = maxHP;  // 初始滿血
		}
	}
}
