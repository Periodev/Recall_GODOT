
using System;
using System.Collections.Generic;

namespace CombatCore.InterOp
{
	public enum InterOpCode 
	{ 
		BasicA, 
		BasicB, 
		BasicC, 
		RecallEcho 
	}

	public readonly struct InterOpCall
	{
		public InterOpCode Op { get; }
		public int SourceId { get; }
		public int TargetId { get; }
		public IReadOnlyList<int> Indices { get; } // for RecallEcho

		public InterOpCall(InterOpCode op, int src, int dst, IReadOnlyList<int> idx = null)
		{ 
			Op = op; 
			SourceId = src; 
			TargetId = dst; 
			Indices = idx; 
		}

		// 便利工廠方法
		public static InterOpCall BasicA(int src, int dst) => 
			new InterOpCall(InterOpCode.BasicA, src, dst);
		
		public static InterOpCall BasicB(int src, int dst) => 
			new InterOpCall(InterOpCode.BasicB, src, dst);
		
		public static InterOpCall BasicC(int src) => 
			new InterOpCall(InterOpCode.BasicC, src, src);
		
		public static InterOpCall RecallEcho(int src, int dst, IReadOnlyList<int> indices) => 
			new InterOpCall(InterOpCode.RecallEcho, src, dst, indices);
	}

}
