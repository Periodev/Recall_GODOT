using Godot;
using System;
using CombatCore;
using CombatCore.Component;

public class Actor
{
	public int HP { get; set; }
	public int MaxHP { get; set; }
	public int Shield;
	public Charge Charge = new ();


	public Actor(int maxHp)
	{
		MaxHP = maxHp;
		HP = maxHp;
		Shield = 0;

	}
}
