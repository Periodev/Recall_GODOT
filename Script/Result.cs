using Godot;
using System;
using CombatCore;

// Result is a static class that defines constants for the result of operations in the game.
// It provides a simple way to represent success or failure without using magic numbers or strings.

// This can be used in various parts of the codebase to indicate the outcome of actions, such as combat results or ability usage.
namespace CombatCore
{
	public static class Result
	{
		public const bool PASS = true;
		public const bool FAIL = false;
	}
}
