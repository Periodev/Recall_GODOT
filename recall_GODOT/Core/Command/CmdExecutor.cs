
using System;
using System.Collections.Generic;

namespace CombatCore.Command
{
	public sealed class CmdExecutor
	{
		public static ExecResult ExecuteAll(IEnumerable<AtomicCmd> cmds)
		{
			ArgumentNullException.ThrowIfNull(cmds);

			var log = new CmdLog();

			foreach (var c in cmds)
			{
				int delta = c.Execute();
				log.Record(c, delta);
			}

			return ExecResult.Pass(log);
		}

		public static ExecResult ExecuteOrDiscard(IEnumerable<AtomicCmd> cmds)
		{
			ArgumentNullException.ThrowIfNull(cmds);
			var batch = new List<AtomicCmd>(cmds);

			// check if cmd actor is dead
			foreach (var c in batch)
			{
				if (c.Source != null && !c.Source.IsAlive)
				{
					return ExecResult.Fail(FailCode.SelfDead);
				}
			}

			// 聚合 AP 需求，避免逐條通過但總量不足
			var apNeed = new Dictionary<Actor, int>();
			foreach (var c in batch)
			{
				if (c.Type == CmdType.ConsumeAP && c.Source != null && c.Value > 0)
				{
					apNeed.TryGetValue(c.Source, out int cur);
					apNeed[c.Source] = cur + c.Value;
				}
			}
			foreach (var kv in apNeed)
			{
				var actor = kv.Key;
				var need  = kv.Value;
				if (actor?.AP == null || actor.AP.Value < need)
					return ExecResult.Fail(FailCode.NoAP);
			}

			// 執行
			var log = new CmdLog();
			foreach (var c in batch)
			{
				int delta = c.Execute();
				log.Record(c, delta);
			}
			return ExecResult.Pass(log);
		}
	}

	public readonly struct ExecResult
	{
		public bool Ok { get; }
		public CmdLog Log { get; }
		public FailCode Code { get; }

		private ExecResult(bool ok, CmdLog log, FailCode code)
		{ Ok = ok; Log = log; Code = code; }

		public static ExecResult Pass(CmdLog log) => new(true, log, FailCode.None);
		public static ExecResult Fail(FailCode code) => new(false, new CmdLog(), code);
	}

	public sealed class CmdLog
	{
		public readonly struct Entry
		{
			public Entry(AtomicCmd cmd, int delta) { Cmd = cmd; Delta = delta; }
			public AtomicCmd Cmd { get; }
			public int Delta { get; }
		}
		private readonly List<Entry> _items = new();
		public void Record(AtomicCmd cmd, int delta) => _items.Add(new Entry(cmd, delta));
		public IReadOnlyList<Entry> Items => _items;
		public int Count => _items.Count;
	}
}

