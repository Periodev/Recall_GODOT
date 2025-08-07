using Godot;
using System;
using CombatCore;

public class Actor
{
	public int HP { get; set; }
	public int MaxHP { get; set; }
	
	public Actor(int maxHp)
	{
		MaxHP = maxHp;
		HP = maxHp;
	}

}
